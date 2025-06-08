using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    public AIState currentState = AIState.Idle;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float followRange = 15f;
    public float patrolRadius = 8f;
    public float stateChangeDelay = 0.5f;
    
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float runSpeed = 6f;
    public float rotationSpeed = 120f;
    public float stoppingDistance = 1.5f;
    
    [Header("Combat")]
    public float attackDamage = 25f;
    public float attackCooldown = 2f;
    public float combatTimeout = 8f;
    public bool canBlockAttacks = false;
    public float blockChance = 0.3f;
    
    [Header("AI Behavior")]
    public bool shouldFlee = false;
    public float fleeHealthThreshold = 0.3f;
    public bool shouldPatrol = true;
    public float patrolRange = 8f;
    public bool canCallForHelp = false;
    
    [Header("Patrol Settings")]
    public List<Transform> patrolPoints = new List<Transform>();
    public float waitTimeAtPatrol = 3f;
    public bool randomPatrol = false;
    
    [Header("Visual Feedback")]
    public GameObject alertIcon;
    public Color idleColor = Color.green;
    public Color alertColor = Color.yellow;
    public Color aggroColor = new Color(1f, 0.5f, 0f, 1f);
    public Color combatColor = Color.red;
    
    [Header("Audio")]
    public AudioClip alertSound;
    public AudioClip attackSound;
    public AudioClip deathSound;
    
    [Header("References")]
    public Animator animator;
    public NavMeshAgent navAgent;
    public Rigidbody rb;
    public Collider mainCollider;
    
    // Private variables
    private Transform player;
    private PlayerStats playerStats;
    private EnemyStats enemyStats;
    private EnemyController enemyController;
    private AudioSource audioSource;
    private Renderer enemyRenderer;
    
    private Vector3 startPosition;
    private Vector3 lastKnownPlayerPosition;
    private int currentPatrolIndex = 0;
    private float lastAttackTime;
    private float lastStateChangeTime;
    private float combatTimer;
    private bool isDead = false; // ADICIONADO: Flag para controlar estado de morte
    
    // Coroutines
    private Coroutine patrolCoroutine;
    private Coroutine searchCoroutine;
    private Coroutine combatCoroutine;
    
    private void Start()
    {
        InitializeComponents();
        SetupInitialState();
    }
    
    private void InitializeComponents()
    {
        // Get required components
        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();
            
        if (rb == null)
            rb = GetComponent<Rigidbody>();
            
        if (mainCollider == null)
            mainCollider = GetComponent<Collider>();
            
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
            
        enemyStats = GetComponent<EnemyStats>();
        enemyController = GetComponent<EnemyController>();
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer == null)
            enemyRenderer = GetComponent<Renderer>();
        
        // Find player
        GameObject playerObj = FindPlayerObject();
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<PlayerStats>();
        }
        
        // Configure NavMeshAgent
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.angularSpeed = rotationSpeed;
        }
        
        startPosition = transform.position;
        
        // Subscribe to events
        if (enemyStats != null)
        {
            enemyStats.OnDeath += HandleDeath;
            enemyStats.OnDamageTaken += HandleDamageTaken;
        }
    }
    
    private GameObject FindPlayerObject()
    {
        // Tentar múltiplas maneiras de encontrar o player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) return player;
        
        // Se não encontrou por tag, tentar pelo GameManager
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
            return GameManager.Instance.CurrentPlayer;
        
        // Se ainda não encontrou, procurar por PlayerStats
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
            return playerStats.gameObject;
        
        return null;
    }
    
    private void SetupInitialState()
    {
        if (patrolPoints.Count > 0)
        {
            ChangeState(AIState.Patrol);
        }
        else
        {
            ChangeState(AIState.Idle);
        }
        
        if (alertIcon != null)
            alertIcon.SetActive(false);
            
        UpdateVisualFeedback();
    }
    
    private void Update()
    {
        // CORREÇÃO: Não processar se estiver morto
        if (isDead) return;
        
        // Revalidar player se perdeu a referência
        if (player == null)
        {
            GameObject playerObj = FindPlayerObject();
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerStats = playerObj.GetComponent<PlayerStats>();
            }
        }
        
        CheckForPlayer();
        UpdateCurrentState();
        UpdateAnimations();
    }
    
    private void CheckForPlayer()
    {
        if (player == null || isDead) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool canSeePlayer = CanSeePlayer();
        
        // State transitions based on player proximity and visibility
        switch (currentState)
        {
            case AIState.Idle:
            case AIState.Patrol:
                if (canSeePlayer && distanceToPlayer <= detectionRange)
                {
                    ChangeState(AIState.Alert);
                }
                break;
                
            case AIState.Alert:
                if (!canSeePlayer || distanceToPlayer > detectionRange)
                {
                    ChangeState(AIState.Search);
                }
                else if (distanceToPlayer <= attackRange)
                {
                    ChangeState(AIState.Combat);
                }
                else if (distanceToPlayer <= followRange)
                {
                    ChangeState(AIState.Chase);
                }
                break;
                
            case AIState.Chase:
                if (!canSeePlayer && distanceToPlayer > followRange)
                {
                    ChangeState(AIState.Search);
                }
                else if (distanceToPlayer <= attackRange)
                {
                    ChangeState(AIState.Combat);
                }
                else if (distanceToPlayer > followRange)
                {
                    ChangeState(AIState.Return);
                }
                break;
                
            case AIState.Combat:
                if (distanceToPlayer > attackRange * 1.5f)
                {
                    if (canSeePlayer && distanceToPlayer <= followRange)
                    {
                        ChangeState(AIState.Chase);
                    }
                    else
                    {
                        ChangeState(AIState.Search);
                    }
                }
                
                // Check flee condition
                if (shouldFlee && enemyStats != null && enemyStats.HealthPercentage <= fleeHealthThreshold)
                {
                    ChangeState(AIState.Return);
                }
                break;
                
            case AIState.Search:
                if (canSeePlayer && distanceToPlayer <= detectionRange)
                {
                    ChangeState(AIState.Alert);
                }
                break;
        }
    }
    
    private bool CanSeePlayer()
    {
        if (player == null || isDead) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Raycast to check for obstacles
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayStart, directionToPlayer, out hit, distanceToPlayer))
        {
            return hit.collider.CompareTag("Player");
        }
        
        return false;
    }
    
    private void UpdateCurrentState()
    {
        // CORREÇÃO: Não processar estados se estiver morto
        if (isDead) return;
        
        switch (currentState)
        {
            case AIState.Idle:
                HandleIdleState();
                break;
            case AIState.Patrol:
                HandlePatrolState();
                break;
            case AIState.Alert:
                HandleAlertState();
                break;
            case AIState.Chase:
                HandleChaseState();
                break;
            case AIState.Combat:
                HandleCombatState();
                break;
            case AIState.Search:
                HandleSearchState();
                break;
            case AIState.Return:
                HandleReturnState();
                break;
            case AIState.Stunned:
                HandleStunnedState();
                break;
        }
    }
    
    private void HandleIdleState()
    {
        // CORREÇÃO: Verificar se NavAgent está ativo antes de usar
        if (navAgent != null && navAgent.enabled && !isDead)
        {
            navAgent.isStopped = true;
        }
    }
    
    private void HandlePatrolState()
    {
        if (patrolPoints.Count == 0 || isDead) return;
        
        if (patrolCoroutine == null)
        {
            patrolCoroutine = StartCoroutine(PatrolRoutine());
        }
    }
    
    private void HandleAlertState()
    {
        if (player == null || isDead) return;
        
        // Look at player
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0;
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime / 60f);
        }
        
        // Wait for a moment before chasing
        if (Time.time - lastStateChangeTime > 1f)
        {
            ChangeState(AIState.Chase);
        }
    }
    
    private void HandleChaseState()
    {
        if (player == null || isDead) return;
        
        // CORREÇÃO: Verificar se NavAgent está ativo antes de usar
        if (navAgent != null && navAgent.enabled && !isDead)
        {
            navAgent.isStopped = false;
            navAgent.speed = runSpeed;
            navAgent.SetDestination(player.position);
        }
        
        lastKnownPlayerPosition = player.position;
        
        // Use enemy controller if available
        if (enemyController != null && !isDead)
        {
            enemyController.MoveToTarget(player.gameObject);
            enemyController.SetRunning(true);
        }
    }
    
    private void HandleCombatState()
    {
        if (player == null || isDead) return;
        
        // CORREÇÃO: Verificar se NavAgent está ativo antes de usar
        if (navAgent != null && navAgent.enabled && !isDead)
        {
            navAgent.isStopped = true;
        }
        
        // Use enemy controller to stop movement
        if (enemyController != null && !isDead)
        {
            enemyController.StopMovement();
            enemyController.LookAtTarget(player.position);
        }
        else
        {
            // Face the player manually
            Vector3 lookDirection = (player.position - transform.position).normalized;
            lookDirection.y = 0;
            
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime / 60f);
            }
        }
        
        // Attack logic
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
        }
        
        // Combat timeout
        combatTimer += Time.deltaTime;
        if (combatTimer >= combatTimeout)
        {
            ChangeState(AIState.Search);
        }
    }
    
    private void HandleSearchState()
    {
        if (isDead) return;
        
        if (searchCoroutine == null)
        {
            searchCoroutine = StartCoroutine(SearchRoutine());
        }
    }
    
    private void HandleReturnState()
    {
        if (isDead) return;
        
        // CORREÇÃO: Verificar se NavAgent está ativo antes de usar
        if (navAgent != null && navAgent.enabled && !isDead)
        {
            navAgent.isStopped = false;
            navAgent.speed = moveSpeed;
            navAgent.SetDestination(startPosition);
        }
        
        // Use enemy controller if available
        if (enemyController != null && !isDead)
        {
            enemyController.MoveTo(startPosition);
            enemyController.SetRunning(false);
        }
        
        if (Vector3.Distance(transform.position, startPosition) < 2f)
        {
            if (patrolPoints.Count > 0)
            {
                ChangeState(AIState.Patrol);
            }
            else
            {
                ChangeState(AIState.Idle);
            }
        }
    }
    
    private void HandleStunnedState()
    {
        if (isDead) return;
        
        // CORREÇÃO: Verificar se NavAgent está ativo antes de usar
        if (navAgent != null && navAgent.enabled && !isDead)
        {
            navAgent.isStopped = true;
        }
        
        if (enemyController != null && !isDead)
        {
            enemyController.StopMovement();
        }
    }
    
    #region Coroutines
    
    private IEnumerator PatrolRoutine()
    {
        while (currentState == AIState.Patrol && patrolPoints.Count > 0 && !isDead)
        {
            Transform targetPoint = patrolPoints[currentPatrolIndex];
            
            if (targetPoint != null)
            {
                if (enemyController != null && !isDead)
                {
                    enemyController.MoveTo(targetPoint.position);
                    enemyController.SetRunning(false);
                }
                else if (navAgent != null && navAgent.enabled && !isDead)
                {
                    navAgent.isStopped = false;
                    navAgent.speed = moveSpeed;
                    navAgent.SetDestination(targetPoint.position);
                }
                
                // Wait until we reach the patrol point
                while (Vector3.Distance(transform.position, targetPoint.position) > stoppingDistance && !isDead)
                {
                    if (currentState != AIState.Patrol || isDead) yield break;
                    yield return null;
                }
                
                // Wait at patrol point
                yield return new WaitForSeconds(waitTimeAtPatrol);
                
                // Check if still alive and patrolling
                if (isDead || currentState != AIState.Patrol) yield break;
                
                // Move to next patrol point
                if (randomPatrol)
                {
                    currentPatrolIndex = Random.Range(0, patrolPoints.Count);
                }
                else
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
                }
            }
            else
            {
                yield break;
            }
        }
        
        patrolCoroutine = null;
    }
    
    private IEnumerator SearchRoutine()
    {
        if (isDead) yield break;
        
        if (enemyController != null && !isDead)
        {
            enemyController.MoveTo(lastKnownPlayerPosition);
            enemyController.SetRunning(false);
        }
        else if (navAgent != null && navAgent.enabled && !isDead)
        {
            navAgent.isStopped = false;
            navAgent.speed = moveSpeed;
            navAgent.SetDestination(lastKnownPlayerPosition);
        }
        
        // Go to last known position
        while (Vector3.Distance(transform.position, lastKnownPlayerPosition) > stoppingDistance && !isDead)
        {
            if (currentState != AIState.Search || isDead) 
            {
                yield break;
            }
            yield return null;
        }
        
        // Look around
        for (int i = 0; i < 4; i++)
        {
            if (currentState != AIState.Search || isDead) 
            {
                yield break;
            }
            
            Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y + 90f, 0);
            
            float rotateTime = 0f;
            while (rotateTime < 1f && !isDead)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateTime);
                rotateTime += Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        // Return to patrol or idle
        if (!isDead)
        {
            ChangeState(AIState.Return);
        }
        
        searchCoroutine = null;
    }
    
    #endregion
    
    #region Combat
    
    private void PerformAttack()
    {
        if (isDead) return;
        
        lastAttackTime = Time.time;
        
        if (animator != null && !isDead)
        {
            animator.SetTrigger("Attack");
        }
        
        if (audioSource != null && attackSound != null && !isDead)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        // Apply damage after a short delay
        StartCoroutine(ApplyDamageAfterDelay(0.5f));
    }
    
    private IEnumerator ApplyDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // CORREÇÃO: Verificar se ainda está vivo antes de aplicar dano
        if (isDead || !enemyStats.IsAlive) yield break;
        
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            if (playerStats != null)
            {
                playerStats.TakeDamage(attackDamage);
            }
        }
    }
    
    #endregion
    
    #region State Management
    
    public void ChangeState(AIState newState)
    {
        // CORREÇÃO: Não mudar estado se estiver morto
        if (isDead) return;
        
        if (currentState == newState) return;
        if (Time.time - lastStateChangeTime < stateChangeDelay) return;
        
        // Exit current state
        ExitState(currentState);
        
        // Enter new state
        currentState = newState;
        lastStateChangeTime = Time.time;
        combatTimer = 0f;
        
        EnterState(newState);
        UpdateVisualFeedback();
    }
    
    private void ExitState(AIState state)
    {
        switch (state)
        {
            case AIState.Patrol:
                if (patrolCoroutine != null)
                {
                    StopCoroutine(patrolCoroutine);
                    patrolCoroutine = null;
                }
                break;
            case AIState.Search:
                if (searchCoroutine != null)
                {
                    StopCoroutine(searchCoroutine);
                    searchCoroutine = null;
                }
                break;
            case AIState.Combat:
                if (combatCoroutine != null)
                {
                    StopCoroutine(combatCoroutine);
                    combatCoroutine = null;
                }
                break;
        }
    }
    
    private void EnterState(AIState state)
    {
        if (isDead) return;
        
        switch (state)
        {
            case AIState.Alert:
                if (audioSource != null && alertSound != null)
                {
                    audioSource.PlayOneShot(alertSound);
                }
                if (alertIcon != null)
                {
                    alertIcon.SetActive(true);
                    StartCoroutine(HideAlertIcon(2f));
                }
                break;
        }
    }
    
    private IEnumerator HideAlertIcon(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (alertIcon != null && !isDead)
        {
            alertIcon.SetActive(false);
        }
    }
    
    #endregion
    
    #region Visual and Audio
    
    private void UpdateVisualFeedback()
    {
        if (enemyRenderer == null || isDead) return;
        
        Color targetColor = idleColor;
        
        switch (currentState)
        {
            case AIState.Idle:
            case AIState.Patrol:
            case AIState.Return:
                targetColor = idleColor;
                break;
            case AIState.Alert:
            case AIState.Search:
                targetColor = alertColor;
                break;
            case AIState.Chase:
                targetColor = aggroColor;
                break;
            case AIState.Combat:
                targetColor = combatColor;
                break;
        }
        
        if (enemyRenderer.material.HasProperty("_Color"))
        {
            enemyRenderer.material.color = targetColor;
        }
    }
    
    private void UpdateAnimations()
    {
        if (animator == null || isDead) return;
        
        float speed = 0f;
        bool isInCombat = currentState == AIState.Combat;
        bool isAlert = currentState == AIState.Alert || currentState == AIState.Search;
        
        if (navAgent != null && navAgent.enabled && !isDead)
        {
            speed = navAgent.velocity.magnitude / navAgent.speed;
        }
        else if (enemyController != null && !isDead)
        {
            speed = enemyController.CurrentSpeed / moveSpeed;
        }
        
        animator.SetFloat("Speed", speed);
        animator.SetBool("InCombat", isInCombat);
        animator.SetBool("Alert", isAlert);
    }
    
    #endregion
    
    #region Event Handlers
    
    private void HandleDeath()
    {
        Die();
    }
    
    private void HandleDamageTaken(float damage, Vector3 damagePosition)
    {
        if (isDead) return;
        
        // Enter combat state if damaged
        if (currentState != AIState.Combat && currentState != AIState.Dead)
        {
            lastKnownPlayerPosition = damagePosition;
            ChangeState(AIState.Combat);
        }
        
        // Call for help if enabled
        if (canCallForHelp)
        {
            CallForHelp();
        }
    }
    
    public void Die()
    {
        if (isDead) return;
        
        // CORREÇÃO: Marcar como morto primeiro
        isDead = true;
        ChangeState(AIState.Dead);
        
        // CORREÇÃO: Parar todas as coroutines primeiro
        StopAllCoroutines();
        
        // CORREÇÃO: Desabilitar NavMeshAgent de forma segura
        if (navAgent != null && navAgent.enabled)
        {
            try
            {
                if (navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                {
                    navAgent.ResetPath();
                }
                navAgent.enabled = false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Erro ao desabilitar NavMeshAgent no EnemyAI: {e.Message}");
            }
        }
        
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }
        
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Disable movement controller
        if (enemyController != null)
        {
            enemyController.SetMovementEnabled(false);
        }
        
        // Disable AI components
        this.enabled = false;
    }
    
    public void Stun(float duration)
    {
        if (isDead) return;
        
        StartCoroutine(StunCoroutine(duration));
    }
    
    private IEnumerator StunCoroutine(float duration)
    {
        if (isDead) yield break;
        
        AIState previousState = currentState;
        ChangeState(AIState.Stunned);
        
        yield return new WaitForSeconds(duration);
        
        if (!isDead)
        {
            ChangeState(previousState);
        }
    }
    
    public void OnTakeDamage(float damage, Vector3 damagePosition)
    {
        HandleDamageTaken(damage, damagePosition);
    }
    
    private void CallForHelp()
    {
        if (isDead) return;
        
        // Find nearby enemies and alert them
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, detectionRange * 2f);
        
        foreach (Collider col in nearbyEnemies)
        {
            EnemyAI otherEnemy = col.GetComponent<EnemyAI>();
            if (otherEnemy != null && otherEnemy != this && !otherEnemy.isDead)
            {
                if (otherEnemy.currentState == AIState.Idle || otherEnemy.currentState == AIState.Patrol)
                {
                    otherEnemy.lastKnownPlayerPosition = player != null ? player.position : transform.position;
                    otherEnemy.ChangeState(AIState.Alert);
                }
            }
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public void OnReachedDestination()
    {
        // Called by EnemyController when reaching destination
        if (!isDead)
        {
            Debug.Log($"{gameObject.name} reached destination");
        }
    }
    
    public void DebugAIInfo()
    {
        Debug.Log($"=== AI INFO for {gameObject.name} ===");
        Debug.Log($"Current State: {currentState}");
        Debug.Log($"Is Dead: {isDead}");
        Debug.Log($"Player Distance: {(player ? Vector3.Distance(transform.position, player.position) : -1)}");
        Debug.Log($"Can See Player: {CanSeePlayer()}");
        Debug.Log("============================");
    }
    
    #endregion
    
    #region Properties
    
    public bool IsDead => isDead; // ADICIONADO: Propriedade pública para verificar se está morto
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        if (isDead) return;
        
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Follow range
        Gizmos.color = aggroColor;
        Gizmos.DrawWireSphere(transform.position, followRange);
        
        // Patrol points
        if (patrolPoints.Count > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 1f);
                    
                    if (i < patrolPoints.Count - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                }
            }
            
            // Line back to first point
            if (patrolPoints.Count > 2 && patrolPoints[0] != null && patrolPoints[patrolPoints.Count - 1] != null)
            {
                Gizmos.DrawLine(patrolPoints[patrolPoints.Count - 1].position, patrolPoints[0].position);
            }
        }
        
        // Line of sight to player
        if (player != null)
        {
            Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, player.position + Vector3.up * 0.5f);
        }
        
        // State indicator
        Vector3 statePos = transform.position + Vector3.up * 3f;
        switch (currentState)
        {
            case AIState.Idle:
                Gizmos.color = idleColor;
                break;
            case AIState.Alert:
                Gizmos.color = alertColor;
                break;
            case AIState.Combat:
                Gizmos.color = combatColor;
                break;
            case AIState.Chase:
                Gizmos.color = aggroColor;
                break;
        }
        Gizmos.DrawWireCube(statePos, Vector3.one * 0.5f);
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (enemyStats != null)
        {
            enemyStats.OnDeath -= HandleDeath;
            enemyStats.OnDamageTaken -= HandleDamageTaken;
        }
    }
}