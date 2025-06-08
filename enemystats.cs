using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gerencia as estatísticas e estado de saúde dos inimigos - VERSÃO CORRIGIDA para NavMeshAgent
/// Correções principais: Ordem correta de desabilitação de componentes na morte
/// </summary>
public class EnemyStats : MonoBehaviour
{
    [Header("Basic Stats")]
    public int enemyLevel = 1;
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public int damage = 15;
    public int armor = 5;
    public float movementSpeed = 3f;
    
    [Header("Combat Stats")]
    public float attackSpeed = 1f;
    public float criticalChance = 2f;
    public float criticalDamage = 150f;
    public float attackRange = 2f;
    
    [Header("Resistances")]
    public float fireResistance = 0f;
    public float coldResistance = 0f;
    public float lightningResistance = 0f;
    public float poisonResistance = 0f;
    public float physicalResistance = 0f;
    
    [Header("Experience and Loot")]
    public int experienceReward = 25;
    public int goldReward = 10;
    public List<LootDrop> lootTable = new List<LootDrop>();
    public float lootDropChance = 0.3f;
    
    [Header("Status")]
    public bool isAlive = true;
    public bool isInvulnerable = false;
    public float invulnerabilityDuration = 0f;
    
    [Header("Death Effects")]
    public GameObject deathEffect;
    public AudioClip deathSound;
    public AudioClip attackSound;
    public AudioClip hitSound;
    public float corpseLifetime = 10f;
    
    [Header("Death Physics Control")]
    public bool freezeCorpsePosition = true;
    public bool useRagdollPhysics = false;
    public LayerMask groundLayer = 1;
    public float groundCheckDistance = 1f;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool showHealthBar = true;
    
    // Eventos
    public System.Action<float, float> OnHealthChanged;
    public System.Action OnDeath;
    public System.Action<float, Vector3> OnDamageTaken;
    
    // Componentes relacionados
    private StatusEffectManager statusEffectManager;
    private EnemyController enemyController;
    private EnemyAI enemyAI;
    
    // Estado interno
    private float lastDamageTime;
    private GameObject lastAttacker;
    private float originalMaxHealth;
    private bool isDead = false; // ADICIONADO: Flag para controlar estado de morte
    
    // UI de vida
    private GameObject healthBarUI;
    
    // Controle de física após morte
    private Rigidbody enemyRigidbody;
    private Collider[] enemyColliders;
    private bool deathPhysicsApplied = false;
    
    private void Awake()
    {
        GetRequiredComponents();
        SetupInitialStats();
    }
    
    private void GetRequiredComponents()
    {
        statusEffectManager = GetComponent<StatusEffectManager>();
        enemyController = GetComponent<EnemyController>();
        enemyAI = GetComponent<EnemyAI>();
        
        enemyRigidbody = GetComponent<Rigidbody>();
        enemyColliders = GetComponents<Collider>();
        
        if (statusEffectManager == null)
        {
            statusEffectManager = gameObject.AddComponent<StatusEffectManager>();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ EnemyStats inicializado para {gameObject.name}: Vida={maxHealth}, Dano={damage}");
        }
    }
    
    private void SetupInitialStats()
    {
        originalMaxHealth = maxHealth;
        currentHealth = maxHealth;
        
        ScaleStatsWithLevel();
        currentHealth = maxHealth;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (!gameObject.CompareTag("Enemy"))
        {
            gameObject.tag = "Enemy";
            if (enableDebugLogs)
                Debug.Log($"🏷️ Tag 'Enemy' definida para {gameObject.name}");
        }
        
        if (showHealthBar)
        {
            CreateHealthBar();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ {gameObject.name} pronto - Vida: {currentHealth}/{maxHealth}, Vivo: {isAlive}");
        }
    }
    
    private void Update()
    {
        if (isInvulnerable && invulnerabilityDuration > 0f)
        {
            invulnerabilityDuration -= Time.deltaTime;
            if (invulnerabilityDuration <= 0f)
            {
                isInvulnerable = false;
                if (enableDebugLogs)
                    Debug.Log($"🛡️ {gameObject.name} não está mais invulnerável");
            }
        }
        
        UpdateHealthBar();
        
        if (!isAlive && !deathPhysicsApplied)
        {
            CheckAndFixCorpsePosition();
        }
    }
    
    #region Health Management
    
    /// <summary>
    /// TakeDamage com atacante - método principal
    /// </summary>
    public void TakeDamage(float damageAmount, GameObject attacker, DamageType damageType = DamageType.Physical)
    {
        if (!isAlive || isDead)
        {
            if (enableDebugLogs)
                Debug.Log($"❌ {gameObject.name} já está morto, ignorando dano");
            return;
        }
        
        if (isInvulnerable)
        {
            if (enableDebugLogs)
                Debug.Log($"🛡️ {gameObject.name} está invulnerável, ignorando dano");
            return;
        }
        
        if (damageAmount <= 0)
        {
            if (enableDebugLogs)
                Debug.Log($"❌ Dano inválido: {damageAmount}");
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"💥 {gameObject.name} recebendo {damageAmount:F1} de dano ({damageType})");
            Debug.Log($"   Vida antes: {currentHealth:F1}/{maxHealth:F1}");
            Debug.Log($"   Atacante: {(attacker != null ? attacker.name : "NULL")}");
        }
        
        // Definir atacante
        if (attacker != null)
        {
            lastAttacker = attacker;
            if (enableDebugLogs)
                Debug.Log($"🎯 Atacante registrado: {attacker.name}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"⚠️ Nenhum atacante fornecido!");
        }
        
        // Calcular resistência
        float resistance = GetResistance(damageType);
        float finalDamage = damageAmount * (1f - resistance / 100f);
        
        // Aplicar redução de armor para dano físico
        if (damageType == DamageType.Physical)
        {
            finalDamage = CalculatePhysicalDamageReduction(finalDamage);
        }
        
        finalDamage = Mathf.Max(1f, finalDamage);
        
        // Aplicar dano
        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        lastDamageTime = Time.time;
        
        float actualDamage = previousHealth - currentHealth;
        
        if (enableDebugLogs)
        {
            Debug.Log($"   Dano final aplicado: {actualDamage:F1}");
            Debug.Log($"   Vida depois: {currentHealth:F1}/{maxHealth:F1}");
            Debug.Log($"   Resistência {damageType}: {resistance:F1}%");
        }
        
        // Disparar eventos
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke(actualDamage, transform.position);
        EventManager.TriggerDamageDealt(actualDamage, transform.position);
        
        // Efeitos visuais e sonoros
        PlayHitEffects(attacker?.transform.position ?? transform.position, actualDamage);
        
        // Alertar IA sobre o dano
        if (enemyAI != null)
        {
            enemyAI.OnTakeDamage(actualDamage, attacker?.transform.position ?? transform.position);
        }
        
        // Verificar morte
        if (currentHealth <= 0f && isAlive)
        {
            if (enableDebugLogs)
                Debug.Log($"💀 {gameObject.name} foi morto por {(lastAttacker != null ? lastAttacker.name : "desconhecido")}!");
            Die();
        }
    }
    
    /// <summary>
    /// Sobrecargas para compatibilidade
    /// </summary>
    public void TakeDamage(float damageAmount, Vector3 damageSource, DamageType damageType = DamageType.Physical)
    {
        GameObject attacker = FindAttackerFromPosition(damageSource);
        TakeDamage(damageAmount, attacker, damageType);
    }
    
    public void TakeDamage(float damageAmount)
    {
        TakeDamage(damageAmount, (GameObject)null, DamageType.Physical);
    }
    
    public void Heal(float healAmount)
    {
        if (!isAlive || healAmount <= 0 || isDead) return;
        
        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        float actualHeal = currentHealth - previousHealth;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (enableDebugLogs)
            Debug.Log($"💚 {gameObject.name} foi curado em {actualHeal:F1}. Vida atual: {currentHealth:F1}");
    }
    
    public void SetInvulnerable(float duration)
    {
        if (isDead) return;
        
        isInvulnerable = true;
        invulnerabilityDuration = duration;
        
        if (enableDebugLogs)
            Debug.Log($"🛡️ {gameObject.name} ficou invulnerável por {duration:F1}s");
    }
    
    #endregion
    
    #region Death System - CORRIGIDO
    
    private void Die()
    {
        if (!isAlive || isDead) return;
        
        // CORREÇÃO: Marcar como morto primeiro para evitar chamadas recursivas
        isDead = true;
        isAlive = false;
        currentHealth = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"💀 {gameObject.name} morreu! Atacante: {(lastAttacker != null ? lastAttacker.name : "desconhecido")}");
        
        // Dar recompensas via EventManager
        GiveRewardsViaEventManager();
        
        // Disparar eventos
        OnDeath?.Invoke();
        EventManager.TriggerEnemyDeath(gameObject);
        EventManager.TriggerEnemyKilled(gameObject.name);
        
        // Efeitos de morte
        PlayDeathEffects();
        
        // CORREÇÃO: Desabilitar componentes na ordem correta
        DisableComponentsInOrder();
        
        // Controlar física do corpo
        HandleDeathPhysics();
        
        // Agendar destruição
        Destroy(gameObject, corpseLifetime);
    }
    
    /// <summary>
    /// NOVO: Desabilita componentes na ordem correta para evitar erros
    /// </summary>
    private void DisableComponentsInOrder()
    {
        // 1. Primeiro: Notificar EnemyController para parar movimento de forma segura
        if (enemyController != null)
        {
            enemyController.SetMovementEnabled(false);
        }
        
        // 2. Segundo: Desabilitar EnemyAI
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }
        
        // 3. Terceiro: Trigger animação de morte
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        
        // 4. Quarto: Remover health bar UI
        if (healthBarUI != null)
        {
            Destroy(healthBarUI);
        }
    }
    
    /// <summary>
    /// DEPRECIADO: Método antigo mantido para compatibilidade mas não usado
    /// </summary>
    private void DisableComponents()
    {
        // Este método foi substituído por DisableComponentsInOrder()
        // Mantido apenas para compatibilidade
        DisableComponentsInOrder();
    }
    
    /// <summary>
    /// Sistema de recompensas via EventManager
    /// </summary>
    private void GiveRewardsViaEventManager()
    {
        if (lastAttacker != null && lastAttacker.CompareTag("Player"))
        {
            if (enableDebugLogs)
                Debug.Log($"🎁 Dando recompensas para {lastAttacker.name}:");
            
            // Dar experiência via EventManager
            if (experienceReward > 0)
            {
                EventManager.TriggerPlayerExperienceGained(experienceReward);
                if (enableDebugLogs)
                    Debug.Log($"   ✅ XP: {experienceReward}");
            }
            
            // Dar ouro via EventManager
            if (goldReward > 0)
            {
                EventManager.TriggerGoldChanged(goldReward);
                if (enableDebugLogs)
                    Debug.Log($"   ✅ Ouro: {goldReward}");
            }
            
            if (enableDebugLogs)
                Debug.Log($"✅ Recompensas entregues via EventManager!");
        }
        else
        {
            if (enableDebugLogs)
            {
                if (lastAttacker == null)
                    Debug.Log($"❌ Nenhum atacante registrado - sem recompensas");
                else if (!lastAttacker.CompareTag("Player"))
                    Debug.Log($"❌ Atacante {lastAttacker.name} não é player (tag: {lastAttacker.tag}) - sem recompensas");
            }
        }
        
        // Dropar loot independentemente
        DropLoot();
    }
    
    /// <summary>
    /// Sistema de detecção de atacante como fallback
    /// </summary>
    private GameObject FindAttackerFromPosition(Vector3 damageSource)
    {
        if (enableDebugLogs)
            Debug.Log($"🔍 Procurando atacante na posição: {damageSource}");
        
        // 1. Tentar GameManager primeiro
        if (GameManager.Instance?.CurrentPlayer != null)
        {
            GameObject player = GameManager.Instance.CurrentPlayer;
            float distanceToPlayer = Vector3.Distance(damageSource, player.transform.position);
            
            if (distanceToPlayer < 20f)
            {
                if (enableDebugLogs)
                    Debug.Log($"🎯 Player encontrado via GameManager: {player.name} (distância: {distanceToPlayer:F1}m)");
                return player;
            }
        }
        
        // 2. Procurar por tag Player
        GameObject playerByTag = GameObject.FindGameObjectWithTag("Player");
        if (playerByTag != null)
        {
            float distanceToPlayer = Vector3.Distance(damageSource, playerByTag.transform.position);
            if (distanceToPlayer < 20f)
            {
                if (enableDebugLogs)
                    Debug.Log($"🎯 Player encontrado por tag: {playerByTag.name} (distância: {distanceToPlayer:F1}m)");
                return playerByTag;
            }
        }
        
        // 3. Procurar PlayerStats na cena
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            float distanceToPlayer = Vector3.Distance(damageSource, playerStats.transform.position);
            if (distanceToPlayer < 20f)
            {
                if (enableDebugLogs)
                    Debug.Log($"🎯 Player encontrado por PlayerStats: {playerStats.name} (distância: {distanceToPlayer:F1}m)");
                return playerStats.gameObject;
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"❌ Nenhum atacante encontrado na posição: {damageSource}");
        
        return null;
    }
    
    private void DropLoot()
    {
        if (isDead) // Verificar se já está processando morte
        {
            foreach (LootDrop lootDrop in lootTable)
            {
                if (lootDrop != null && lootDrop.ShouldDrop(lastAttacker))
                {
                    Vector3 dropPosition = transform.position + Vector3.up * 0.5f + Random.insideUnitSphere * 2f;
                    dropPosition.y = transform.position.y + 0.5f;
                    
                    GameObject lootObject = lootDrop.CreateWorldItem(dropPosition);
                    
                    if (lootObject != null && enableDebugLogs)
                    {
                        Debug.Log($"📦 {gameObject.name} dropou {lootDrop.item.itemName} x{lootDrop.quantity}");
                    }
                }
            }
        }
    }
    
    private void HandleDeathPhysics()
    {
        if (freezeCorpsePosition)
        {
            if (enemyRigidbody != null)
            {
                enemyRigidbody.isKinematic = true;
                enemyRigidbody.useGravity = false;
            }
            
            foreach (Collider col in enemyColliders)
            {
                if (col != null && !col.isTrigger)
                {
                    col.enabled = false;
                }
            }
        }
        else if (useRagdollPhysics && enemyRigidbody != null)
        {
            enemyRigidbody.isKinematic = false;
            enemyRigidbody.useGravity = true;
            
            Vector3 deathForce = Random.insideUnitSphere * 2f;
            deathForce.y = Mathf.Abs(deathForce.y);
            enemyRigidbody.AddForce(deathForce, ForceMode.Impulse);
        }
        
        deathPhysicsApplied = true;
    }
    
    private void CheckAndFixCorpsePosition()
    {
        if (isDead) // Só verificar se já está morto
        {
            RaycastHit hit;
            bool isOnGround = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer);
            
            if (!isOnGround)
            {
                if (enableDebugLogs)
                    Debug.Log($"⚠️ {gameObject.name} caiu do mapa! Reposicionando...");
                
                Vector3 safePosition = FindNearestGroundPosition();
                
                if (safePosition != Vector3.zero)
                {
                    transform.position = safePosition;
                    
                    if (enemyRigidbody != null)
                    {
                        enemyRigidbody.linearVelocity = Vector3.zero;
                        enemyRigidbody.angularVelocity = Vector3.zero;
                    }
                    
                    if (enableDebugLogs)
                        Debug.Log($"✅ {gameObject.name} reposicionado em: {safePosition}");
                }
            }
        }
    }
    
    private Vector3 FindNearestGroundPosition()
    {
        Vector3 originalPosition = transform.position;
        
        Vector3[] directions = {
            Vector3.zero,
            Vector3.forward * 5f,
            Vector3.back * 5f,
            Vector3.left * 5f,
            Vector3.right * 5f
        };
        
        foreach (Vector3 offset in directions)
        {
            Vector3 testPosition = originalPosition + offset;
            testPosition.y += 10f;
            
            RaycastHit hit;
            if (Physics.Raycast(testPosition, Vector3.down, out hit, 50f, groundLayer))
            {
                return hit.point + Vector3.up * 0.1f;
            }
        }
        
        return new Vector3(originalPosition.x, 0f, originalPosition.z);
    }
    
    #endregion
    
    #region Helper Methods
    
    private void ScaleStatsWithLevel()
    {
        if (enemyLevel <= 1) return;
        
        float levelMultiplier = 1f + (enemyLevel - 1) * 0.2f;
        
        maxHealth = originalMaxHealth * levelMultiplier;
        damage = Mathf.RoundToInt(damage * levelMultiplier);
        armor = Mathf.RoundToInt(armor * levelMultiplier);
        
        experienceReward = Mathf.RoundToInt(experienceReward * levelMultiplier);
        goldReward = Mathf.RoundToInt(goldReward * levelMultiplier);
        
        if (enableDebugLogs)
        {
            Debug.Log($"📊 {gameObject.name} escalado para nível {enemyLevel}:");
            Debug.Log($"   Vida: {maxHealth:F1} (x{levelMultiplier:F2})");
            Debug.Log($"   Dano: {damage} (x{levelMultiplier:F2})");
            Debug.Log($"   XP: {experienceReward} | Ouro: {goldReward}");
        }
    }
    
    private float CalculatePhysicalDamageReduction(float damage)
    {
        float reduction = armor / (armor + 100f);
        return damage * (1f - reduction);
    }
    
    private float GetResistance(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Fire: return fireResistance;
            case DamageType.Cold: return coldResistance;
            case DamageType.Lightning: return lightningResistance;
            case DamageType.Poison: return poisonResistance;
            case DamageType.Physical: return physicalResistance;
            default: return 0f;
        }
    }
    
    private void PlayDeathEffects()
    {
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, transform.rotation);
            Destroy(effect, 5f);
        }
        
        if (deathSound != null)
        {
            AudioManager.Instance?.PlaySFXAtPosition(deathSound, transform.position);
        }
    }
    
    private void PlayHitEffects(Vector3 damageSource, float damage)
    {
        if (hitSound != null)
        {
            AudioManager.Instance?.PlaySFXAtPosition(hitSound, transform.position);
        }
    }
    
    private void CreateHealthBar()
    {
        GameObject healthBarPrefab = Resources.Load<GameObject>("UI/EnemyHealthBar");
        if (healthBarPrefab != null)
        {
            healthBarUI = Instantiate(healthBarPrefab, transform);
            healthBarUI.transform.localPosition = Vector3.up * 2f;
        }
    }
    
    private void UpdateHealthBar()
    {
        if (healthBarUI == null || isDead) return;
        
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            healthBarUI.transform.LookAt(mainCamera.transform);
            healthBarUI.transform.Rotate(0, 180, 0);
        }
    }
    
    #endregion
    
    #region Public Properties
    
    public bool IsAlive => isAlive && !isDead;
    public bool IsInvulnerable => isInvulnerable && !isDead;
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsAtFullHealth => currentHealth >= maxHealth && !isDead;
    public bool IsLowHealth => HealthPercentage <= 0.25f;
    public float TimeSinceLastDamage => Time.time - lastDamageTime;
    public GameObject LastAttacker => lastAttacker;
    public bool IsDead => isDead; // ADICIONADO: Propriedade pública para verificar se está morto
    
    #endregion
    
    #region Public Methods
    
    public void ForceKill()
    {
        if (enableDebugLogs)
            Debug.Log($"💀 Forçando morte de {gameObject.name}");
        
        currentHealth = 0f;
        Die();
    }
    
    public void ResetStats()
    {
        if (isDead) return; // Não resetar se já estiver morto
        
        currentHealth = maxHealth;
        isAlive = true;
        isInvulnerable = false;
        invulnerabilityDuration = 0f;
        lastAttacker = null;
        deathPhysicsApplied = false;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (enableDebugLogs)
            Debug.Log($"🔄 Stats de {gameObject.name} resetados");
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Debug Enemy Stats")]
    public void DebugEnemyStats()
    {
        Debug.Log($"=== {gameObject.name} STATS ===");
        Debug.Log($"Vida: {currentHealth:F1}/{maxHealth:F1} ({HealthPercentage:P})");
        Debug.Log($"Vivo: {isAlive}");
        Debug.Log($"Morto (flag): {isDead}");
        Debug.Log($"Invulnerável: {isInvulnerable}");
        Debug.Log($"Nível: {enemyLevel}");
        Debug.Log($"XP Reward: {experienceReward}");
        Debug.Log($"Gold Reward: {goldReward}");
        Debug.Log($"Last Attacker: {(lastAttacker != null ? lastAttacker.name : "None")}");
        Debug.Log("========================");
    }
    
    [ContextMenu("Test Take Damage")]
    public void TestTakeDamage()
    {
        GameObject player = GameManager.Instance?.CurrentPlayer ?? GameObject.FindGameObjectWithTag("Player");
        TakeDamage(25f, player);
    }
    
    [ContextMenu("Test Force Kill")]
    public void TestForceKill()
    {
        ForceKill();
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        if (isDead) return;
        
        // Desenhar range de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Indicador de saúde
        if (isAlive)
        {
            Gizmos.color = Color.Lerp(Color.red, Color.green, HealthPercentage);
            Vector3 healthBarPos = transform.position + Vector3.up * 2f;
            Vector3 healthBarEnd = healthBarPos + Vector3.right * (HealthPercentage * 2f);
            Gizmos.DrawLine(healthBarPos, healthBarEnd);
            
            Gizmos.color = Color.white;
            Gizmos.DrawLine(healthBarPos, healthBarPos + Vector3.right * 2f);
        }
        
        // Indicador de ground check
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);
        
        // Indicador de invulnerabilidade
        if (isInvulnerable)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
        
        // Indicador do último atacante
        if (lastAttacker != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, lastAttacker.transform.position);
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        if (healthBarUI != null)
        {
            Destroy(healthBarUI);
        }
    }
}