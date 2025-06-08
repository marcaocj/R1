using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Sistema de combate do jogador - VERSÃO TOTALMENTE CORRIGIDA
/// Correções principais: Passagem correta do atacante, integração com EventManager
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackAngle = 90f;
    public LayerMask enemyLayerMask = -1;
    public Transform attackPoint;
    
    [Header("Attack Data")]
    public AttackData primaryAttack;
    public AttackData secondaryAttack;
    
    [Header("Combat Effects")]
    public GameObject hitEffectPrefab;
    public GameObject criticalHitEffectPrefab;
    
    [Header("Audio")]
    public AudioClip attackSound;
    public AudioClip hitSound;
    public AudioClip criticalHitSound;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool showAttackRange = true;
    public bool enableDebugGizmos = true;
    
    // Componentes
    private PlayerStats playerStats;
    private PlayerAnimationController animationController;
    private PlayerController playerController;
    
    // Estado do combate
    private bool canAttack = true;
    private float lastAttackTime;
    private int currentComboCount = 0;
    private int maxComboCount = 3;
    
    // Lista de inimigos já atingidos neste ataque
    private List<GameObject> hitEnemiesThisAttack = new List<GameObject>();
    
    // Cache da última AttackData usada
    private AttackData lastUsedAttackData;
    
    private void Awake()
    {
        GetRequiredComponents();
        SetupAttackPoint();
        SetupDefaultAttackData();
    }
    
    private void Start()
    {
        SubscribeToInputEvents();
        
        if (enableDebugLogs)
        {
            Debug.Log("✅ PlayerCombat inicializado e pronto!");
            DebugCombatSystem();
        }
    }
    
    private void Update()
    {
        UpdateCombatState();
    }
    
    #region Setup Methods
    
    private void GetRequiredComponents()
    {
        playerStats = GetComponent<PlayerStats>();
        animationController = GetComponent<PlayerAnimationController>();
        playerController = GetComponent<PlayerController>();
        
        if (enableDebugLogs)
        {
            Debug.Log($"Componentes - Stats: {playerStats != null}, Controller: {playerController != null}, Animation: {animationController != null}");
        }
    }
    
    private void SetupAttackPoint()
    {
        if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.SetParent(transform);
            attackPointObj.transform.localPosition = Vector3.forward * 1.5f;
            attackPoint = attackPointObj.transform;
            
            if (enableDebugLogs)
                Debug.Log("AttackPoint criado automaticamente");
        }
    }
    
    private void SetupDefaultAttackData()
    {
        if (primaryAttack == null)
        {
            primaryAttack = AttackData.CreateBasicAttack();
            primaryAttack.range = attackRange;
            primaryAttack.angle = attackAngle;
        }
        
        if (secondaryAttack == null)
        {
            secondaryAttack = AttackData.CreateHeavyAttack();
            secondaryAttack.range = attackRange;
            secondaryAttack.angle = attackAngle;
        }
    }
    
    private void SubscribeToInputEvents()
    {
        if (InputManager.Instance != null)
        {
            InputManager.OnPrimaryAttackInput += OnPrimaryAttackInput;
            InputManager.OnSecondaryAttackInput += OnSecondaryAttackInput;
            
            if (enableDebugLogs)
                Debug.Log("✅ Eventos de input registrados com sucesso!");
        }
        else
        {
            Debug.LogError("❌ InputManager.Instance é null! Sistema de ataque não funcionará.");
        }
    }
    
    #endregion
    
    #region Input Handlers
    
    private void OnPrimaryAttackInput()
    {
        if (enableDebugLogs)
            Debug.Log("🎯 Input de ataque primário recebido!");
            
        TryPrimaryAttack();
    }
    
    private void OnSecondaryAttackInput()
    {
        if (enableDebugLogs)
            Debug.Log("🎯 Input de ataque secundário recebido!");
            
        TrySecondaryAttack();
    }
    
    #endregion
    
    #region Attack System
    
    private void TryPrimaryAttack()
    {
        if (enableDebugLogs)
            Debug.Log("🗡️ Tentando ataque primário...");
            
        if (CanPerformAttack())
        {
            PerformAttack(primaryAttack, 0);
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("❌ Não pode atacar agora");
        }
    }
    
    private void TrySecondaryAttack()
    {
        if (enableDebugLogs)
            Debug.Log("⚔️ Tentando ataque secundário...");
            
        if (CanPerformAttack())
        {
            PerformAttack(secondaryAttack, 1);
        }
    }
    
    private bool CanPerformAttack()
    {
        if (!canAttack)
        {
            if (enableDebugLogs) Debug.Log("❌ canAttack = false");
            return false;
        }
        
        if (playerStats == null)
        {
            if (enableDebugLogs) Debug.Log("❌ playerStats = null");
            return false;
        }
        
        if (!playerStats.IsAlive)
        {
            if (enableDebugLogs) Debug.Log("❌ Player não está vivo");
            return false;
        }
        
        if (playerController != null && playerController.IsStunned)
        {
            if (enableDebugLogs) Debug.Log("❌ Player está stunado");
            return false;
        }
        
        if (animationController != null && animationController.IsPlayingSkill)
        {
            if (enableDebugLogs) Debug.Log("❌ Tocando animação de skill");
            return false;
        }
        
        if (GameManager.Instance != null && !GameManager.Instance.IsGamePlaying())
        {
            if (enableDebugLogs) Debug.Log("❌ Jogo não está em estado Playing");
            return false;
        }
        
        if (enableDebugLogs) Debug.Log("✅ Pode atacar!");
        return true;
    }
    
    private void PerformAttack(AttackData attackData, int attackType)
    {
        if (attackData == null)
        {
            Debug.LogError("❌ AttackData é null!");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"⚡ Executando ataque: {attackData.attackName}");
        
        // Verificar e consumir mana ANTES do ataque
        if (attackData.manaCost > 0)
        {
            if (playerStats != null && !playerStats.HasEnoughMana(attackData.manaCost))
            {
                if (enableDebugLogs)
                    Debug.Log("❌ Mana insuficiente!");
                return;
            }
            
            if (playerStats != null)
            {
                playerStats.UseMana(attackData.manaCost);
            }
        }
        
        // Configurar estado de ataque
        canAttack = false;
        lastAttackTime = Time.time;
        hitEnemiesThisAttack.Clear();
        lastUsedAttackData = attackData;
        
        // Atualizar combo
        UpdateComboCount();
        
        // Som de ataque
        PlayAttackSound(attackData);
        
        // CORREÇÃO: Executar ataque usando a AttackData fornecida
        ExecuteAttackWithData(attackData);
        
        // Se tem animação, tocar também
        if (animationController != null)
        {
            animationController.TriggerAttackAnimation(attackType);
        }
    }
    
    /// <summary>
    /// CORREÇÃO PRINCIPAL: Método que executa o ataque usando AttackData específica
    /// </summary>
    public void ExecuteAttackWithData(AttackData attackData)
    {
        if (enableDebugLogs)
            Debug.Log($"💥 ExecuteAttackWithData chamado - AttackData: {attackData.attackName}");
        
        if (attackData == null)
        {
            Debug.LogError("❌ AttackData é null em ExecuteAttackWithData!");
            return;
        }
        
        // Detectar inimigos usando dados da AttackData
        List<GameObject> enemiesInRange = DetectEnemiesInAttackRange(attackData);
        
        if (enableDebugLogs)
            Debug.Log($"🎯 Inimigos detectados: {enemiesInRange.Count}");
        
        // Aplicar dano
        int enemiesHit = 0;
        foreach (GameObject enemy in enemiesInRange)
        {
            if (!hitEnemiesThisAttack.Contains(enemy))
            {
                DealDamageToEnemy(enemy, attackData);
                hitEnemiesThisAttack.Add(enemy);
                enemiesHit++;
            }
        }
        
        if (enemiesHit == 0 && enableDebugLogs)
        {
            Debug.Log("⚠️ Nenhum inimigo foi atingido");
        }
        else if (enableDebugLogs)
        {
            Debug.Log($"✅ {enemiesHit} inimigos atingidos!");
        }
    }
    
    /// <summary>
    /// Método de compatibilidade para ExecuteAttack sem parâmetros
    /// </summary>
    public void ExecuteAttack()
    {
        if (lastUsedAttackData != null)
        {
            ExecuteAttackWithData(lastUsedAttackData);
        }
        else
        {
            ExecuteAttackWithData(primaryAttack);
        }
    }
    
    #endregion
    
    #region Enemy Detection - CORRIGIDO
    
    private List<GameObject> DetectEnemiesInAttackRange(AttackData attackData)
    {
        List<GameObject> detectedEnemies = new List<GameObject>();
        
        if (attackData == null || attackPoint == null)
        {
            if (enableDebugLogs)
                Debug.Log("❌ AttackData ou AttackPoint é null");
            return detectedEnemies;
        }
        
        float finalRange = attackData.range;
        Vector3 attackPosition = attackPoint.position;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🔍 Detectando inimigos...");
            Debug.Log($"   Range: {finalRange}");
            Debug.Log($"   Posição: {attackPosition}");
            Debug.Log($"   LayerMask: {enemyLayerMask.value}");
        }
        
        // Detecção melhorada
        Collider[] hitColliders = Physics.OverlapSphere(attackPosition, finalRange, enemyLayerMask);
        
        if (enableDebugLogs)
            Debug.Log($"📊 Colliders encontrados: {hitColliders.Length}");
        
        foreach (Collider col in hitColliders)
        {
            if (enableDebugLogs)
                Debug.Log($"   Analisando: {col.name}, tag: {col.tag}, layer: {col.gameObject.layer}");
            
            // Verificar se é um inimigo válido
            if (IsValidEnemy(col.gameObject))
            {
                // Verificar ângulo de ataque
                Vector3 directionToEnemy = (col.transform.position - transform.position).normalized;
                float angleToEnemy = Vector3.Angle(transform.forward, directionToEnemy);
                
                if (angleToEnemy <= attackData.angle / 2f)
                {
                    detectedEnemies.Add(col.gameObject);
                    
                    if (enableDebugLogs)
                        Debug.Log($"   ✅ Inimigo válido: {col.name} (ângulo: {angleToEnemy:F1}°)");
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.Log($"   ❌ Fora do ângulo: {angleToEnemy:F1}° > {attackData.angle / 2f:F1}°");
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"   ❌ Não é inimigo válido: {col.name}");
            }
        }
        
        return detectedEnemies;
    }
    
    private bool IsValidEnemy(GameObject target)
    {
        if (target == null) return false;
        
        // 1. Verificar se tem EnemyStats (mais importante)
        EnemyStats enemyStats = target.GetComponent<EnemyStats>();
        if (enemyStats != null && enemyStats.IsAlive)
        {
            if (enableDebugLogs)
                Debug.Log($"   ✅ {target.name} tem EnemyStats e está vivo");
            return true;
        }
        
        // 2. Verificar tag como fallback
        if (target.CompareTag("Enemy"))
        {
            if (enableDebugLogs)
                Debug.Log($"   ✅ {target.name} tem tag Enemy");
            return true;
        }
        
        // 3. Verificar se tem EnemyAI
        EnemyAI enemyAI = target.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            if (enableDebugLogs)
                Debug.Log($"   ✅ {target.name} tem EnemyAI");
            return true;
        }
        
        if (enableDebugLogs)
            Debug.Log($"   ❌ {target.name} não é um inimigo válido");
        return false;
    }
    
    #endregion
    
    #region Damage System - CORRIGIDO
    
    /// <summary>
    /// CORREÇÃO CRÍTICA: Agora passa o gameObject do player como atacante
    /// </summary>
    private void DealDamageToEnemy(GameObject enemy, AttackData attackData)
    {
        if (enemy == null || attackData == null) return;
        
        if (enableDebugLogs)
            Debug.Log($"💢 Aplicando dano a: {enemy.name}");
        
        EnemyStats enemyStats = enemy.GetComponent<EnemyStats>();
        if (enemyStats == null)
        {
            if (enableDebugLogs)
                Debug.Log($"❌ EnemyStats não encontrado em: {enemy.name}");
            return;
        }
        
        // Calcular dano usando AttackData
        float baseDamage = CalculateBaseDamage(attackData);
        bool isCritical = CalculateCriticalHit();
        float finalDamage = isCritical ? baseDamage * GetCriticalMultiplier() : baseDamage;
        
        // Aplicar knockback usando dados da AttackData
        if (attackData.knockbackForce > 0f)
        {
            ApplyKnockback(enemy, attackData.knockbackForce);
        }
        
        if (enableDebugLogs)
            Debug.Log($"💥 Dano: base={baseDamage:F1}, final={finalDamage:F1}, crítico={isCritical}");
        
        // CORREÇÃO CRÍTICA: Passar gameObject do player como atacante
        try
        {
            enemyStats.TakeDamage(finalDamage, gameObject, attackData.damageType);
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ Dano aplicado com sucesso! Atacante: {gameObject.name}");
                Debug.Log($"   Vida do inimigo após dano: {enemyStats.currentHealth}/{enemyStats.maxHealth}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Erro ao aplicar dano: {e.Message}");
        }
        
        // Efeitos visuais e sonoros
        ApplyAttackEffects(enemy, attackData, isCritical);
        
        // Disparar eventos
        EventManager.TriggerDamageDealt(finalDamage, enemy.transform.position);
    }
    
    private float CalculateBaseDamage(AttackData attackData)
    {
        float damage = 10f; // Dano base mínimo
        
        if (playerStats != null)
        {
            damage = playerStats.FinalDamage;
        }
        
        // Aplicar multiplicador da AttackData corretamente
        damage *= attackData.damageMultiplier;
        
        // Aplicar bônus de combo
        damage *= GetComboDamageMultiplier();
        
        // Aplicar bônus de atributos baseado no tipo de dano da AttackData
        damage += GetAttributeDamageBonus(attackData.damageType);
        
        return damage;
    }
    
    private bool CalculateCriticalHit()
    {
        if (playerStats == null) return false;
        
        float critChance = playerStats.FinalCriticalChance;
        return Random.Range(0f, 100f) <= critChance;
    }
    
    private float GetCriticalMultiplier()
    {
        if (playerStats != null)
        {
            return playerStats.FinalCriticalDamage / 100f;
        }
        return 2f;
    }
    
    private float GetComboDamageMultiplier()
    {
        return 1f + (currentComboCount * 0.1f);
    }
    
    private float GetAttributeDamageBonus(DamageType damageType)
    {
        if (playerStats == null) return 0f;
        
        switch (damageType)
        {
            case DamageType.Physical:
                return playerStats.FinalStrength * 0.5f;
            case DamageType.Magic:
            case DamageType.Fire:
            case DamageType.Cold:
            case DamageType.Lightning:
                return playerStats.FinalIntelligence * 0.5f;
            default:
                return 0f;
        }
    }
    
    private void ApplyAttackEffects(GameObject enemy, AttackData attackData, bool isCritical)
    {
        // Efeitos visuais
        SpawnHitEffect(enemy.transform.position, isCritical);
        
        // Som de hit
        PlayHitSound(isCritical);
        
        // Aplicar stun se a AttackData tiver
        if (attackData.stunDuration > 0f)
        {
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.Stun(attackData.stunDuration);
            }
        }
    }
    
    private void ApplyKnockback(GameObject enemy, float force)
    {
        Vector3 direction = (enemy.transform.position - transform.position).normalized;
        
        // Tentar usar EnemyController primeiro
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.ApplyKnockback(direction * force);
            return;
        }
        
        // Fallback para Rigidbody
        Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
        if (enemyRb != null)
        {
            enemyRb.AddForce(direction * force, ForceMode.Impulse);
        }
    }
    
    #endregion
    
    #region Audio and Visual Effects
    
    private void PlayAttackSound(AttackData attackData)
    {
        AudioClip soundToPlay = attackData.attackSound ?? attackSound;
        
        if (soundToPlay != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundToPlay);
        }
    }
    
    private void SpawnHitEffect(Vector3 position, bool isCritical)
    {
        GameObject effectPrefab = isCritical ? criticalHitEffectPrefab : hitEffectPrefab;
        
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    private void PlayHitSound(bool isCritical)
    {
        AudioClip soundToPlay = isCritical ? criticalHitSound : hitSound;
        
        if (soundToPlay != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundToPlay);
        }
    }
    
    #endregion
    
    #region Combat State Management
    
    private void UpdateComboCount()
    {
        float timeSinceLastAttack = Time.time - lastAttackTime;
        
        if (timeSinceLastAttack < 2f)
        {
            currentComboCount = Mathf.Min(currentComboCount + 1, maxComboCount);
        }
        else
        {
            currentComboCount = 1;
        }
    }
    
    private void UpdateCombatState()
    {
        if (playerStats == null) return;
        
        // Reset da capacidade de atacar
        if (!canAttack)
        {
            float attackCooldown = 1f / (playerStats.FinalAttackSpeed > 0 ? playerStats.FinalAttackSpeed : 1f);
            
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                canAttack = true;
                
                if (enableDebugLogs)
                    Debug.Log("🔄 Pode atacar novamente");
            }
        }
        
        // Reset do combo
        if (Time.time - lastAttackTime > 3f)
        {
            currentComboCount = 0;
        }
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Debug Combat System")]
    public void DebugCombatSystem()
    {
        Debug.Log("=== COMBAT SYSTEM DEBUG ===");
        Debug.Log($"InputManager exists: {InputManager.Instance != null}");
        Debug.Log($"PlayerStats exists: {playerStats != null}");
        Debug.Log($"AnimationController exists: {animationController != null}");
        Debug.Log($"Primary Attack configured: {primaryAttack != null}");
        Debug.Log($"Secondary Attack configured: {secondaryAttack != null}");
        Debug.Log($"Attack Range: {attackRange}");
        Debug.Log($"Can Attack: {canAttack}");
        Debug.Log($"AttackPoint exists: {attackPoint != null}");
        if (attackPoint != null)
            Debug.Log($"AttackPoint position: {attackPoint.position}");
        Debug.Log($"Enemy LayerMask: {enemyLayerMask.value}");
        Debug.Log("===========================");
    }
    
    [ContextMenu("Find Enemies in Scene")]
    public void DebugFindEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log($"🔍 Inimigos encontrados na cena por tag: {enemies.Length}");
        
        foreach (GameObject enemy in enemies)
        {
            EnemyStats stats = enemy.GetComponent<EnemyStats>();
            Debug.Log($"   - {enemy.name}: EnemyStats={stats != null}, Layer={enemy.layer}, Vivo={stats?.IsAlive ?? false}");
        }
        
        EnemyStats[] allEnemyStats = FindObjectsOfType<EnemyStats>();
        Debug.Log($"🔍 EnemyStats encontrados na cena: {allEnemyStats.Length}");
        
        foreach (EnemyStats stats in allEnemyStats)
        {
            Debug.Log($"   - {stats.name}: Vivo={stats.IsAlive}, Tag={stats.tag}, Layer={stats.gameObject.layer}");
        }
    }
    
    [ContextMenu("Test Attack Range")]
    public void DebugTestAttackRange()
    {
        if (attackPoint == null) return;
        
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayerMask);
        Debug.Log($"🎯 Teste de alcance: {hits.Length} objetos detectados");
        
        foreach (Collider hit in hits)
        {
            bool isValid = IsValidEnemy(hit.gameObject);
            Debug.Log($"   - {hit.name} (tag: {hit.tag}, layer: {hit.gameObject.layer}, válido: {isValid})");
        }
    }
    
    [ContextMenu("Force Attack Test")]
    public void DebugForceAttack()
    {
        Debug.Log("🧪 Forçando teste de ataque!");
        if (primaryAttack != null)
        {
            ExecuteAttackWithData(primaryAttack);
        }
        else
        {
            Debug.LogError("Primary attack é null!");
        }
    }
    
    #endregion
    
    #region Properties
    
    public bool CanAttack => canAttack;
    public int CurrentCombo => currentComboCount;
    public float LastAttackTime => lastAttackTime;
    public float CurrentAttackRange => primaryAttack?.range ?? attackRange;
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        if (!showAttackRange || attackPoint == null) return;
        
        float debugRange = primaryAttack?.range ?? attackRange;
        float debugAngle = primaryAttack?.angle ?? attackAngle;
        
        // Range de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, debugRange);
        
        // Ângulo de ataque
        Vector3 leftBoundary = Quaternion.AngleAxis(-debugAngle / 2f, Vector3.up) * transform.forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(debugAngle / 2f, Vector3.up) * transform.forward;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(attackPoint.position, leftBoundary * debugRange);
        Gizmos.DrawRay(attackPoint.position, rightBoundary * debugRange);
        
        // Centro do ataque
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(attackPoint.position, transform.forward * debugRange);
        
        // Indicador de combo
        if (currentComboCount > 0)
        {
            Gizmos.color = Color.green;
            Vector3 comboPos = transform.position + Vector3.up * 2.5f;
            Gizmos.DrawWireCube(comboPos, Vector3.one * (0.2f + currentComboCount * 0.1f));
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Desinscrever dos eventos
        if (InputManager.Instance != null)
        {
            InputManager.OnPrimaryAttackInput -= OnPrimaryAttackInput;
            InputManager.OnSecondaryAttackInput -= OnSecondaryAttackInput;
        }
    }
}