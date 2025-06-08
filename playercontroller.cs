using UnityEngine;

/// <summary>
/// Controla o movimento, rotação e ações básicas do jogador - Unity 6 Corrigido
/// </summary>
[RequireComponent(typeof(CharacterController), typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseMovementSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public float rotationSpeed = 10f;
    
    [Header("Physics")]
    public float gravity = -9.81f;
    public float jumpHeight = 1f;
    public LayerMask groundMask = 1;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    
    [Header("Mouse Controls")]
    public bool useMouseForMovement = true;
    public bool useMouseForRotation = true;
    public LayerMask mouseTargetLayer = 1;
    
    [Header("Status Effects")]
    [SerializeField] private bool isStunned = false;
    [SerializeField] private bool isSilenced = false;
    [SerializeField] private bool isRooted = false;
    
    // Componentes
    private CharacterController characterController;
    private PlayerStats playerStats;
    private PlayerAnimationController animationController;
    private StatusEffectManager statusEffectManager;
    
    // Estado do movimento
    private Vector3 velocity;
    private Vector3 moveDirection;
    private Vector3 mouseWorldPosition;
    private bool isGrounded;
    private bool isRunning;
    private bool isMoving;
    private bool movementEnabled = true;
    
    // Input
    private Vector2 movementInput;
    
    // Cache para performance
    private Camera playerCamera;
    private bool hasCamera = false;
    
    private void Awake()
    {
        // Obter componentes obrigatórios
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("PlayerController: CharacterController não encontrado!");
        }
        
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerController: PlayerStats não encontrado!");
        }
        
        // Obter componentes opcionais
        animationController = GetComponent<PlayerAnimationController>();
        statusEffectManager = GetComponent<StatusEffectManager>();
        
        // Cache da câmera
        playerCamera = Camera.main;
        hasCamera = playerCamera != null;
        
        // Configurar groundCheck se não estiver definido
        SetupGroundCheck();
    }
    
    private void Start()
    {
        // Inscrever nos eventos de input
        SubscribeToInputEvents();
        
        // Inscrever nos eventos de status effects
        SubscribeToStatusEvents();
        
        // Configuração inicial
        InitializeController();
    }
    
    private void Update()
    {
        CheckGrounded();
        UpdateStatusEffects();
        HandleMovement();
        HandleRotation();
        HandleGravity();
        ApplyMovement();
        UpdateAnimations();
    }
    
    #region Initialization
    
    private void SetupGroundCheck()
    {
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }
    
    private void InitializeController()
    {
        // Configurações iniciais
        velocity = Vector3.zero;
        moveDirection = Vector3.zero;
        mouseWorldPosition = transform.position;
        
        // Verificar se há câmera disponível
        if (!hasCamera)
        {
            Debug.LogWarning("PlayerController: Câmera principal não encontrada! Algumas funcionalidades podem não funcionar.");
        }
    }
    
    #endregion
    
    #region Event Subscription
    
    private void SubscribeToInputEvents()
    {
        if (InputManager.Instance != null)
        {
            InputManager.OnMovementInput += HandleMovementInput;
            InputManager.OnMousePositionInput += HandleMousePositionInput;
            InputManager.OnRunInput += StartRunning;
            InputManager.OnRunInputReleased += StopRunning;
        }
        else
        {
            Debug.LogWarning("PlayerController: InputManager não encontrado!");
        }
    }
    
    private void SubscribeToStatusEvents()
    {
        if (statusEffectManager != null)
        {
            statusEffectManager.OnEffectAdded += HandleStatusEffectAdded;
            statusEffectManager.OnEffectRemoved += HandleStatusEffectRemoved;
        }
    }
    
    #endregion
    
    #region Input Handlers
    
    private void HandleMovementInput(Vector2 input)
    {
        movementInput = input;
    }
    
    private void HandleMousePositionInput(Vector3 worldPos)
    {
        mouseWorldPosition = worldPos;
    }
    
    private void StartRunning()
    {
        if (!isStunned && !isRooted && movementEnabled)
        {
            isRunning = true;
        }
    }
    
    private void StopRunning()
    {
        isRunning = false;
    }
    
    #endregion
    
    #region Status Effect Handlers
    
    private void HandleStatusEffectAdded(StatusEffectManager.StatusEffectType effectType)
    {
        switch (effectType)
        {
            case StatusEffectManager.StatusEffectType.Stun:
                SetStunned(true);
                break;
            case StatusEffectManager.StatusEffectType.Silence:
                SetSilenced(true);
                break;
            case StatusEffectManager.StatusEffectType.Slow:
                // O slow é handled automaticamente pelo sistema de stats
                break;
            case StatusEffectManager.StatusEffectType.Freeze:
                SetRooted(true);
                break;
        }
    }
    
    private void HandleStatusEffectRemoved(StatusEffectManager.StatusEffectType effectType)
    {
        switch (effectType)
        {
            case StatusEffectManager.StatusEffectType.Stun:
                SetStunned(false);
                break;
            case StatusEffectManager.StatusEffectType.Silence:
                SetSilenced(false);
                break;
            case StatusEffectManager.StatusEffectType.Freeze:
                SetRooted(false);
                break;
        }
    }
    
    #endregion
    
    #region Movement
    
    private void CheckGrounded()
    {
        if (groundCheck == null) return;
        
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Pequeno valor negativo para manter no chão
        }
    }
    
    private void UpdateStatusEffects()
    {
        // Verificar status effects que afetam movimento
        if (statusEffectManager != null)
        {
            isStunned = statusEffectManager.IsStunned();
            isSilenced = statusEffectManager.IsSilenced();
            isRooted = statusEffectManager.HasEffect(StatusEffectManager.StatusEffectType.Freeze);
        }
    }
    
    private void HandleMovement()
    {
        // Verificar se movimento está habilitado e não está impedido por status effects
        if (!movementEnabled || isStunned || isRooted)
        {
            moveDirection = Vector3.zero;
            isMoving = false;
            return;
        }
        
        // Calcular direção do movimento
        if (useMouseForMovement && movementInput.magnitude > 0.1f)
        {
            // Movimento híbrido: WASD + direção do mouse
            Vector3 keyboardDirection = new Vector3(movementInput.x, 0, movementInput.y).normalized;
            moveDirection = keyboardDirection;
        }
        else if (movementInput.magnitude > 0.1f)
        {
            // Movimento só com teclado
            moveDirection = new Vector3(movementInput.x, 0, movementInput.y).normalized;
        }
        else
        {
            moveDirection = Vector3.zero;
        }
        
        // Verificar se está se movendo
        isMoving = moveDirection.magnitude > 0.1f;
    }
    
    private void HandleRotation()
    {
        if (!useMouseForRotation || isStunned || !hasCamera) return;
        
        // Calcular direção para o mouse usando raycast
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 directionToMouse = (mouseWorldPos - transform.position).normalized;
        directionToMouse.y = 0; // Manter rotação apenas no plano horizontal
        
        if (directionToMouse.magnitude > 0.1f)
        {
            // Rotacionar suavemente para a direção do mouse
            Quaternion targetRotation = Quaternion.LookRotation(directionToMouse);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        if (!hasCamera) return mouseWorldPosition;
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f, mouseTargetLayer))
        {
            return hit.point;
        }
        
        // Fallback: projetar na altura atual do player
        float distance = 10f;
        Vector3 worldPoint = ray.origin + ray.direction * distance;
        worldPoint.y = transform.position.y;
        return worldPoint;
    }
    
    private void HandleGravity()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }
    
    private void ApplyMovement()
    {
        if (characterController == null) return;
        
        // Calcular velocidade final baseada nos stats
        float finalSpeed = CalculateFinalMovementSpeed();
        
        // Aplicar movimento horizontal
        Vector3 horizontalMovement = moveDirection * finalSpeed;
        
        // Combinar movimento horizontal com velocidade vertical
        Vector3 finalMovement = horizontalMovement + new Vector3(0, velocity.y, 0);
        
        // Aplicar movimento
        characterController.Move(finalMovement * Time.deltaTime);
    }
    
    private float CalculateFinalMovementSpeed()
    {
        float speed = baseMovementSpeed;
        
        // Aplicar modificador de corrida
        if (isRunning && !isRooted)
        {
            speed *= runSpeedMultiplier;
        }
        
        // Aplicar bônus de velocidade dos stats do player
        if (playerStats != null)
        {
            speed = playerStats.FinalMovementSpeed;
            
            // Aplicar modificador de corrida aos stats finais
            if (isRunning && !isRooted)
            {
                speed *= runSpeedMultiplier;
            }
        }
        
        // Aplicar efeitos de slow se houver
        if (statusEffectManager != null)
        {
            float speedModifier = statusEffectManager.GetStatModifier(StatusEffectManager.StatType.MovementSpeed);
            speed += speedModifier;
            speed = Mathf.Max(0.1f, speed); // Garantir velocidade mínima
        }
        
        return speed;
    }
    
    #endregion
    
    #region Animation Updates
    
    private void UpdateAnimations()
    {
        if (animationController == null) return;
        
        // Atualizar parâmetros de animação
        animationController.SetMoving(isMoving && !isStunned);
        animationController.SetRunning(isRunning && !isRooted);
        animationController.SetMovementSpeed(isStunned ? 0f : moveDirection.magnitude);
        animationController.SetGrounded(isGrounded);
    }
    
    #endregion
    
    #region Status Effects Control
    
    /// <summary>
    /// Define se o jogador está stunado
    /// </summary>
    public void SetStunned(bool stunned)
    {
        isStunned = stunned;
        
        if (isStunned)
        {
            // Parar movimento quando stunado
            moveDirection = Vector3.zero;
            velocity.x = 0;
            velocity.z = 0;
            isRunning = false;
            isMoving = false;
        }
    }
    
    /// <summary>
    /// Define se o jogador está silenciado
    /// </summary>
    public void SetSilenced(bool silenced)
    {
        isSilenced = silenced;
    }
    
    /// <summary>
    /// Define se o jogador está enraizado (não pode se mover)
    /// </summary>
    public void SetRooted(bool rooted)
    {
        isRooted = rooted;
        
        if (isRooted)
        {
            moveDirection = Vector3.zero;
            velocity.x = 0;
            velocity.z = 0;
            isRunning = false;
            isMoving = false;
        }
    }
    
    /// <summary>
    /// Verifica se está stunado
    /// </summary>
    public bool IsStunned => isStunned;
    
    /// <summary>
    /// Verifica se está silenciado
    /// </summary>
    public bool IsSilenced => isSilenced;
    
    /// <summary>
    /// Verifica se está enraizado
    /// </summary>
    public bool IsRooted => isRooted;
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Move o jogador para uma posição específica (para cutscenes, teleporte, etc.)
    /// </summary>
    public void TeleportTo(Vector3 position)
    {
        if (characterController == null) return;
        
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
        velocity = Vector3.zero;
        
        Debug.Log($"PlayerController: Teleportado para {position}");
    }
    
    /// <summary>
    /// Aplica um impulso de knockback
    /// </summary>
    public void ApplyKnockback(Vector3 force, float duration = 0.3f)
    {
        if (!isStunned && characterController != null) // Não aplicar knockback se estiver stunado
        {
            StartCoroutine(KnockbackCoroutine(force, duration));
        }
    }
    
    private System.Collections.IEnumerator KnockbackCoroutine(Vector3 force, float duration)
    {
        float timer = 0f;
        Vector3 knockbackVelocity = force;
        bool wasMovementEnabled = movementEnabled;
        
        // Temporariamente desabilitar controle de movimento
        SetMovementEnabled(false);
        
        while (timer < duration && characterController != null)
        {
            // Aplicar knockback diminuindo ao longo do tempo
            float factor = (duration - timer) / duration;
            Vector3 knockbackMovement = knockbackVelocity * factor;
            
            characterController.Move(knockbackMovement * Time.deltaTime);
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Restaurar controle de movimento
        SetMovementEnabled(wasMovementEnabled);
    }
    
    /// <summary>
    /// Permite/impede o movimento do jogador
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        
        if (!enabled)
        {
            moveDirection = Vector3.zero;
            velocity.x = 0;
            velocity.z = 0;
            isRunning = false;
            isMoving = false;
        }
    }
    
    /// <summary>
    /// Faz o jogador pular (se estiver no chão)
    /// </summary>
    public void Jump()
    {
        if (isGrounded && !isStunned && !isRooted && movementEnabled)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            
            // Trigger animação de pulo
            if (animationController != null)
            {
                animationController.TriggerJump();
            }
        }
    }
    
    /// <summary>
    /// Força parada imediata
    /// </summary>
    public void StopImmediately()
    {
        moveDirection = Vector3.zero;
        velocity = Vector3.zero;
        isRunning = false;
        isMoving = false;
    }
    
    /// <summary>
    /// Atualiza a referência da câmera
    /// </summary>
    public void UpdateCameraReference()
    {
        playerCamera = Camera.main;
        hasCamera = playerCamera != null;
        
        if (!hasCamera)
        {
            Debug.LogWarning("PlayerController: Câmera principal não encontrada!");
        }
    }
    
    #endregion
    
    #region Collision Detection
    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Detectar colisões com objetos específicos
        if (hit.gameObject.CompareTag("Interactable"))
        {
            // Lógica de interação pode ser adicionada aqui
        }
        
        // Empurrar objetos Rigidbody (Corrigido para Unity 6)
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body != null && !body.isKinematic && body.mass < 50f)
        {
            Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            // Corrigido para Unity 6: velocity -> linearVelocity
            body.linearVelocity = pushDirection * 3f;
        }
    }
    
    #endregion
    
    #region Properties
    
    public bool IsMoving => isMoving && !isStunned && movementEnabled;
    public bool IsRunning => isRunning && !isRooted && movementEnabled;
    public bool IsGrounded => isGrounded;
    public Vector3 MoveDirection => moveDirection;
    public Vector3 Velocity => velocity;
    public Vector3 MouseWorldPosition => mouseWorldPosition;
    public float CurrentSpeed => CalculateFinalMovementSpeed();
    public bool MovementEnabled => movementEnabled && !isStunned && !isRooted;
    public bool HasCamera => hasCamera;
    
    #endregion
    
    #region Debug
    
    private void OnDrawGizmosSelected()
    {
        // Desenhar ground check
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
        
        // Desenhar direção do movimento
        if (isMoving)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, moveDirection * 2f);
        }
        
        // Desenhar posição do mouse
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(mouseWorldPosition, 0.5f);
        
        // Indicador de status effects
        Vector3 statusPos = transform.position + Vector3.up * 2.5f;
        
        if (isStunned)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(statusPos, Vector3.one * 0.5f);
        }
        
        if (isRooted)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(statusPos + Vector3.up * 0.5f, Vector3.one * 0.3f);
        }
        
        if (isSilenced)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(statusPos + Vector3.up * 1f, Vector3.one * 0.3f);
        }
        
        // Desenhar range de detecção do mouse
        if (hasCamera)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, GetMouseWorldPosition());
        }
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugPlayerState()
    {
        Debug.Log($"=== PLAYER CONTROLLER DEBUG ===");
        Debug.Log($"Movement Enabled: {movementEnabled}");
        Debug.Log($"Is Moving: {isMoving}");
        Debug.Log($"Is Running: {isRunning}");
        Debug.Log($"Is Grounded: {isGrounded}");
        Debug.Log($"Is Stunned: {isStunned}");
        Debug.Log($"Is Rooted: {isRooted}");
        Debug.Log($"Is Silenced: {isSilenced}");
        Debug.Log($"Current Speed: {CurrentSpeed:F2}");
        Debug.Log($"Move Direction: {moveDirection}");
        Debug.Log($"Velocity: {velocity}");
        Debug.Log($"Has Camera: {hasCamera}");
        Debug.Log("===============================");
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Desinscrever dos eventos
        if (InputManager.Instance != null)
        {
            InputManager.OnMovementInput -= HandleMovementInput;
            InputManager.OnMousePositionInput -= HandleMousePositionInput;
            InputManager.OnRunInput -= StartRunning;
            InputManager.OnRunInputReleased -= StopRunning;
        }
        
        if (statusEffectManager != null)
        {
            statusEffectManager.OnEffectAdded -= HandleStatusEffectAdded;
            statusEffectManager.OnEffectRemoved -= HandleStatusEffectRemoved;
        }
    }
}