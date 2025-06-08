using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    [Header("Input Settings")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode inventoryKey = KeyCode.I;
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode skillKey1 = KeyCode.Alpha1;
    public KeyCode skillKey2 = KeyCode.Alpha2;
    public KeyCode skillKey3 = KeyCode.Alpha3;
    public KeyCode skillKey4 = KeyCode.Alpha4;
    
    [Header("Movement")]
    public float horizontalInput;
    public float verticalInput;
    public Vector2 movementInput;
    
    [Header("Mouse")]
    public Vector2 mousePosition;
    public bool leftMouseDown;
    public bool rightMouseDown;
    public bool leftMousePressed;
    public bool rightMousePressed;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Eventos de input - CORRIGIDOS para serem estáticos
    public static System.Action<Vector2> OnMovementInput;
    public static System.Action<Vector3> OnMousePositionInput;
    public static System.Action OnRunInput;
    public static System.Action OnRunInputReleased;
    public static System.Action OnPrimaryAttackInput;
    public static System.Action OnSecondaryAttackInput;
    public static System.Action<int> OnSkillInput;
    public static System.Action OnInventoryInput;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (enableDebugLogs)
            Debug.Log("InputManager inicializado");
    }
    
    private void Update()
    {
        HandleMovementInput();
        HandleMouseInput();
        HandleKeyboardInput();
    }
    
    private void HandleMovementInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        movementInput = new Vector2(horizontalInput, verticalInput).normalized;
        
        // Trigger movement event
        OnMovementInput?.Invoke(movementInput);
        
        // Run input
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            OnRunInput?.Invoke();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            OnRunInputReleased?.Invoke();
        }
    }
    
    private void HandleMouseInput()
    {
        // CORREÇÃO: Melhor detecção da posição do mouse
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCamera.nearClipPlane));
            mousePosition = mouseWorldPos;
            
            // Trigger mouse position event
            OnMousePositionInput?.Invoke(mouseWorldPos);
        }
        
        // CORREÇÃO: Detecção mais robusta dos cliques do mouse
        leftMouseDown = Input.GetMouseButtonDown(0);
        rightMouseDown = Input.GetMouseButtonDown(1);
        leftMousePressed = Input.GetMouseButton(0);
        rightMousePressed = Input.GetMouseButton(1);
        
        // CORREÇÃO: Debug dos cliques
        if (leftMouseDown && enableDebugLogs)
        {
            Debug.Log("Mouse esquerdo clicado - disparando OnPrimaryAttackInput");
        }
        
        if (rightMouseDown && enableDebugLogs)
        {
            Debug.Log("Mouse direito clicado - disparando OnSecondaryAttackInput");
        }
        
        // Attack inputs - CORREÇÃO: Verificar se os eventos existem antes de disparar
        if (leftMouseDown)
        {
            OnPrimaryAttackInput?.Invoke();
        }
        
        if (rightMouseDown)
        {
            OnSecondaryAttackInput?.Invoke();
        }
    }
    
    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(interactKey))
        {
            EventManager.TriggerInteractPressed();
        }
        
        if (Input.GetKeyDown(inventoryKey))
        {
            EventManager.TriggerInventoryToggled();
            OnInventoryInput?.Invoke();
        }
        
        if (Input.GetKeyDown(pauseKey))
        {
            EventManager.TriggerPauseToggled();
        }
        
        // CORREÇÃO: Skills com verificação de eventos
        if (Input.GetKeyDown(skillKey1))
        {
            EventManager.TriggerSkillUsed(1);
            OnSkillInput?.Invoke(1);
        }
        
        if (Input.GetKeyDown(skillKey2))
        {
            EventManager.TriggerSkillUsed(2);
            OnSkillInput?.Invoke(2);
        }
        
        if (Input.GetKeyDown(skillKey3))
        {
            EventManager.TriggerSkillUsed(3);
            OnSkillInput?.Invoke(3);
        }
        
        if (Input.GetKeyDown(skillKey4))
        {
            EventManager.TriggerSkillUsed(4);
            OnSkillInput?.Invoke(4);
        }
        
        // ADICIONADO: Tecla de debug para testar ataque
        if (Input.GetKeyDown(KeyCode.T) && enableDebugLogs)
        {
            Debug.Log("Tecla T pressionada - testando ataque manual");
            OnPrimaryAttackInput?.Invoke();
        }
    }
    
    public Vector2 GetMovementInput()
    {
        return movementInput;
    }
    
    public Vector2 GetMouseWorldPosition()
    {
        return mousePosition;
    }
    
    public bool IsLeftMousePressed()
    {
        return leftMousePressed;
    }
    
    public bool IsRightMousePressed()
    {
        return rightMousePressed;
    }
    
    public bool IsLeftMouseDown()
    {
        return leftMouseDown;
    }
    
    public bool IsRightMouseDown()
    {
        return rightMouseDown;
    }
    
    // ADICIONADO: Métodos de debug
    [ContextMenu("Debug Input System")]
    public void DebugInputSystem()
    {
        Debug.Log("=== INPUT SYSTEM DEBUG ===");
        Debug.Log($"Mouse Position: {mousePosition}");
        Debug.Log($"Left Mouse Down: {leftMouseDown}");
        Debug.Log($"Right Mouse Down: {rightMouseDown}");
        Debug.Log($"Movement Input: {movementInput}");
        Debug.Log($"OnPrimaryAttackInput subscribers: {OnPrimaryAttackInput?.GetInvocationList()?.Length ?? 0}");
        Debug.Log($"OnSecondaryAttackInput subscribers: {OnSecondaryAttackInput?.GetInvocationList()?.Length ?? 0}");
        Debug.Log("==========================");
    }
    
    [ContextMenu("Test Primary Attack Input")]
    public void TestPrimaryAttackInput()
    {
        Debug.Log("Testando input de ataque primário manualmente");
        OnPrimaryAttackInput?.Invoke();
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Limpar eventos estáticos
        OnMovementInput = null;
        OnMousePositionInput = null;
        OnRunInput = null;
        OnRunInputReleased = null;
        OnPrimaryAttackInput = null;
        OnSecondaryAttackInput = null;
        OnSkillInput = null;
        OnInventoryInput = null;
    }
}