using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controla projéteis criados por skills (fireballs, flechas, etc.) - Unity 6 Corrigido
/// </summary>
public class SkillProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float lifetime = 5f;
    public float damage = 25f;
    public LayerMask targetLayers = -1;
    public bool piercing = false;
    public int maxPierceTargets = 3;
    
    [Header("Homing")]
    public bool isHoming = false;
    public float homingStrength = 2f;
    public float homingRange = 10f;
    public GameObject homingTarget;
    
    [Header("Area of Effect")]
    public bool hasAOE = false;
    public float aoeRadius = 3f;
    public GameObject aoeEffectPrefab;
    
    [Header("Visual Effects")]
    public TrailRenderer trail;
    public ParticleSystem particles;
    public GameObject hitEffectPrefab;
    
    [Header("Audio")]
    public AudioClip launchSound;
    public AudioClip hitSound;
    public AudioClip flyingSound;
    
    [Header("Physics")]
    public bool useGravity = false;
    public float gravityMultiplier = 1f;
    
    // Dados da skill que criou este projétil
    private Skill sourceSkill;
    private GameObject caster;
    private Vector3 targetPosition;
    
    // Estado do projétil
    private Vector3 direction;
    private List<GameObject> hitTargets = new List<GameObject>();
    private bool hasHit = false;
    private float timeAlive = 0f;
    private bool isInitialized = false;
    
    // Componentes
    private Rigidbody rb;
    private Collider projectileCollider;
    private AudioSource audioSource;
    
    // Cache para performance
    private Vector3 lastPosition;
    private float distanceTraveled = 0f;
    
    private void Awake()
    {
        // Obter componentes
        rb = GetComponent<Rigidbody>();
        projectileCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        
        // Configurar Rigidbody
        SetupRigidbody();
        
        // Configurar collider
        SetupCollider();
        
        // Cache posição inicial
        lastPosition = transform.position;
    }
    
    private void Start()
    {
        // Tocar som de lançamento
        PlayLaunchSound();
        
        // Som contínuo de voo
        PlayFlyingSound();
        
        // Agendar destruição após lifetime
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }
    
    private void Update()
    {
        timeAlive += Time.deltaTime;
        
        if (!hasHit && isInitialized)
        {
            UpdateMovement();
            UpdateHoming();
            UpdateDistanceTraveled();
        }
    }
    
    #region Setup
    
    private void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.useGravity = useGravity;
            rb.freezeRotation = true;
            
            // Unity 6: Configurar interpolação para movimento suave
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }
    
    private void SetupCollider()
    {
        if (projectileCollider != null)
        {
            projectileCollider.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("SkillProjectile: Nenhum collider encontrado! Adicionando SphereCollider.");
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.1f;
            sphere.isTrigger = true;
            projectileCollider = sphere;
        }
    }
    
    #endregion
    
    #region Audio
    
    private void PlayLaunchSound()
    {
        if (launchSound != null)
        {
            AudioManager.Instance?.PlaySFXAtPosition(launchSound, transform.position);
        }
    }
    
    private void PlayFlyingSound()
    {
        if (flyingSound != null && audioSource != null)
        {
            audioSource.clip = flyingSound;
            audioSource.loop = true;
            audioSource.volume = 0.3f; // Volume mais baixo para som contínuo
            audioSource.Play();
        }
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Inicializa o projétil com dados da skill
    /// </summary>
    public void Initialize(Skill skill, GameObject skillCaster, Vector3 target)
    {
        if (skillCaster == null)
        {
            Debug.LogError("SkillProjectile: Caster não pode ser nulo!");
            Destroy(gameObject);
            return;
        }
        
        sourceSkill = skill;
        caster = skillCaster;
        targetPosition = target;
        
        if (skill != null)
        {
            // Configurar propriedades baseadas na skill
            damage = skill.baseDamage;
            speed = 15f; // Velocidade padrão mais alta
            
            // Configurar AOE se a skill tiver
            if (skill.areaOfEffect > 0)
            {
                hasAOE = true;
                aoeRadius = skill.areaOfEffect;
            }
        }
        
        // Calcular direção inicial
        direction = (targetPosition - transform.position).normalized;
        
        // Validar direção
        if (direction.magnitude < 0.1f)
        {
            Debug.LogWarning("SkillProjectile: Direção muito pequena, usando direção forward do caster");
            direction = skillCaster.transform.forward;
        }
        
        // Orientar projétil na direção do movimento
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        isInitialized = true;
        
        Debug.Log($"SkillProjectile inicializado: {skill?.skillName ?? "Unknown"} | Direção: {direction} | Velocidade: {speed}");
    }
    
    /// <summary>
    /// Configura projétil como homing
    /// </summary>
    public void SetHoming(GameObject target, float strength = 2f, float range = 10f)
    {
        if (target == null)
        {
            Debug.LogWarning("SkillProjectile: Target para homing não pode ser nulo!");
            return;
        }
        
        isHoming = true;
        homingTarget = target;
        homingStrength = Mathf.Max(0.1f, strength);
        homingRange = Mathf.Max(1f, range);
    }
    
    #endregion
    
    #region Movement
    
    private void UpdateMovement()
    {
        if (rb != null)
        {
            // Unity 6: Usar linearVelocity ao invés de velocity
            Vector3 targetVelocity = direction * speed;
            
            // Aplicar gravidade customizada se habilitada
            if (useGravity)
            {
                targetVelocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            }
            
            rb.linearVelocity = targetVelocity;
        }
        else
        {
            // Movimento manual se não há Rigidbody
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }
    }
    
    private void UpdateHoming()
    {
        if (!isHoming || homingTarget == null) return;
        
        float distanceToTarget = Vector3.Distance(transform.position, homingTarget.transform.position);
        
        // Só fazer homing se o alvo estiver dentro do range
        if (distanceToTarget <= homingRange)
        {
            Vector3 targetDirection = (homingTarget.transform.position - transform.position).normalized;
            
            // Lerp suave para a nova direção
            direction = Vector3.Lerp(direction, targetDirection, homingStrength * Time.deltaTime).normalized;
            
            // Atualizar rotação
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, homingStrength * 2f * Time.deltaTime);
            }
        }
    }
    
    private void UpdateDistanceTraveled()
    {
        distanceTraveled += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
    }
    
    #endregion
    
    #region Collision Detection
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit && !piercing) return;
        
        // Ignorar o caster
        if (other.gameObject == caster) return;
        
        // Ignorar triggers desnecessários
        if (other.isTrigger && !IsValidTargetCollider(other)) return;
        
        // Verificar se é um alvo válido
        if (!IsValidTarget(other.gameObject)) return;
        
        // Verificar se já atingiu este alvo (para projéteis piercing)
        if (hitTargets.Contains(other.gameObject)) return;
        
        // Processar hit
        ProcessHit(other.gameObject);
        
        // Adicionar à lista de alvos atingidos
        hitTargets.Add(other.gameObject);
        
        // Verificar se deve parar
        if (!piercing || hitTargets.Count >= maxPierceTargets)
        {
            StopProjectile();
        }
    }
    
    private bool IsValidTargetCollider(Collider collider)
    {
        // Verificar se é um collider que devemos considerar para alvos
        return collider.CompareTag("Enemy") || collider.CompareTag("Player") || collider.CompareTag("Destructible");
    }
    
    private bool IsValidTarget(GameObject target)
    {
        if (target == null) return false;
        
        // Verificar layer
        int targetLayer = 1 << target.layer;
        if ((targetLayers & targetLayer) == 0) return false;
        
        // Verificar se é um alvo apropriado baseado na skill
        if (sourceSkill != null)
        {
            switch (sourceSkill.targetType)
            {
                case TargetType.Enemy:
                    return target.CompareTag("Enemy");
                case TargetType.Ally:
                    return target.CompareTag("Player") || target.CompareTag("Ally");
                case TargetType.Single:
                case TargetType.Multiple:
                    return target.CompareTag("Enemy") || target.CompareTag("Player") || target.CompareTag("Ally");
                default:
                    return true;
            }
        }
        
        return true;
    }
    
    #endregion
    
    #region Hit Processing
    
    private void ProcessHit(GameObject target)
    {
        // Calcular dano final
        float finalDamage = CalculateDamage();
        
        // Aplicar dano ao alvo
        ApplyDamageToTarget(target, finalDamage);
        
        // Aplicar efeitos da skill
        ApplySkillEffects(target);
        
        // Efeitos visuais e sonoros
        CreateHitEffects(target.transform.position);
        
        // Processar AOE se habilitado
        if (hasAOE)
        {
            ProcessAOEDamage(target.transform.position);
        }
        
        Debug.Log($"Projétil atingiu {target.name} causando {finalDamage:F1} de dano");
    }
    
    private float CalculateDamage()
    {
        float calculatedDamage = damage;
        
        // Aplicar scaling da skill se disponível
        if (sourceSkill != null && caster != null)
        {
            PlayerStats casterStats = caster.GetComponent<PlayerStats>();
            if (casterStats != null)
            {
                // Adicionar bônus baseado nos atributos do caster
                switch (sourceSkill.damageType)
                {
                    case DamageType.Physical:
                        calculatedDamage += casterStats.FinalStrength * sourceSkill.damageScaling;
                        break;
                    case DamageType.Magic:
                    case DamageType.Fire:
                    case DamageType.Cold:
                    case DamageType.Lightning:
                        calculatedDamage += casterStats.FinalIntelligence * sourceSkill.damageScaling;
                        break;
                }
                
                // Verificar crítico
                bool isCritical = Random.Range(0f, 100f) <= casterStats.FinalCriticalChance;
                if (isCritical)
                {
                    calculatedDamage *= (casterStats.FinalCriticalDamage / 100f);
                }
            }
        }
        
        // Redução de dano por distância (opcional)
        if (distanceTraveled > 10f)
        {
            float falloff = Mathf.Clamp01(1f - (distanceTraveled - 10f) / 20f);
            calculatedDamage *= falloff;
        }
        
        return calculatedDamage;
    }
    
    private void ApplyDamageToTarget(GameObject target, float damage)
    {
        // Aplicar dano a inimigos
        if (target.CompareTag("Enemy"))
        {
            EnemyStats enemyStats = target.GetComponent<EnemyStats>();
            if (enemyStats != null)
            {
                DamageType damageType = sourceSkill?.damageType ?? DamageType.Magic;
                enemyStats.TakeDamage(damage, transform.position, damageType);
            }
        }
        // Aplicar dano/cura ao player (se for skill de cura)
        else if (target.CompareTag("Player"))
        {
            PlayerStats playerStats = target.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                if (sourceSkill?.skillType == SkillType.Heal)
                {
                    playerStats.Heal(damage);
                }
                else
                {
                    playerStats.TakeDamage(damage);
                }
            }
        }
        
        // Disparar evento de dano
        EventManager.TriggerDamageDealt(damage, target.transform.position);
    }
    
    private void ApplySkillEffects(GameObject target)
    {
        if (sourceSkill?.skillEffects == null) return;
        
        foreach (SkillEffect effect in sourceSkill.skillEffects)
        {
            if (effect != null)
            {
                effect.ApplyEffect(target, caster);
            }
        }
    }
    
    private void ProcessAOEDamage(Vector3 centerPosition)
    {
        // Encontrar todos os alvos na área
        Collider[] colliders = Physics.OverlapSphere(centerPosition, aoeRadius, targetLayers);
        
        foreach (Collider col in colliders)
        {
            if (col.gameObject != caster && !hitTargets.Contains(col.gameObject))
            {
                if (IsValidTarget(col.gameObject))
                {
                    // Aplicar dano reduzido para AOE
                    float aoeDamage = CalculateDamage() * 0.7f; // 70% do dano para AOE
                    ApplyDamageToTarget(col.gameObject, aoeDamage);
                    ApplySkillEffects(col.gameObject);
                }
            }
        }
        
        // Criar efeito visual de AOE
        if (aoeEffectPrefab != null)
        {
            GameObject aoeEffect = Instantiate(aoeEffectPrefab, centerPosition, Quaternion.identity);
            Destroy(aoeEffect, 3f);
        }
    }
    
    #endregion
    
    #region Visual and Audio Effects
    
    private void CreateHitEffects(Vector3 position)
    {
        // Efeito visual de impacto
        if (hitEffectPrefab != null)
        {
            GameObject hitEffect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(hitEffect, 2f);
        }
        
        // Som de impacto
        if (hitSound != null)
        {
            AudioManager.Instance?.PlaySFXAtPosition(hitSound, position);
        }
        
        // Parar efeitos do projétil
        StopVisualEffects();
    }
    
    private void StopVisualEffects()
    {
        if (particles != null)
        {
            // Unity 6: Usar Stop(false) para parar emissão mas permitir partículas existentes terminarem
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        
        if (trail != null)
        {
            trail.enabled = false;
        }
    }
    
    #endregion
    
    #region Projectile Control
    
    private void StopProjectile()
    {
        hasHit = true;
        
        // Parar movimento
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Unity 6: linearVelocity
            rb.isKinematic = true;
        }
        
        // Parar som de voo
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // Desativar collider
        if (projectileCollider != null)
        {
            projectileCollider.enabled = false;
        }
        
        // Agendar destruição
        Destroy(gameObject, 1f);
    }
    
    /// <summary>
    /// Força a explosão do projétil na posição atual
    /// </summary>
    public void ExplodeAtCurrentPosition()
    {
        if (hasAOE)
        {
            ProcessAOEDamage(transform.position);
        }
        
        CreateHitEffects(transform.position);
        StopProjectile();
    }
    
    /// <summary>
    /// Muda a direção do projétil
    /// </summary>
    public void SetDirection(Vector3 newDirection)
    {
        if (newDirection.magnitude < 0.1f)
        {
            Debug.LogWarning("SkillProjectile: Nova direção muito pequena!");
            return;
        }
        
        direction = newDirection.normalized;
        
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    /// <summary>
    /// Modifica a velocidade do projétil
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0.1f, newSpeed);
    }
    
    /// <summary>
    /// Adiciona um alvo aos já atingidos (para evitar hit múltiplo)
    /// </summary>
    public void AddHitTarget(GameObject target)
    {
        if (target != null && !hitTargets.Contains(target))
        {
            hitTargets.Add(target);
        }
    }
    
    #endregion
    
    #region Public Properties
    
    public bool HasHit => hasHit;
    public float TimeAlive => timeAlive;
    public float DistanceTraveled => distanceTraveled;
    public int HitTargetCount => hitTargets.Count;
    public Vector3 CurrentDirection => direction;
    public bool IsInitialized => isInitialized;
    
    #endregion
    
    #region Validation
    
    private void OnValidate()
    {
        speed = Mathf.Max(0.1f, speed);
        lifetime = Mathf.Max(0.1f, lifetime);
        damage = Mathf.Max(0f, damage);
        maxPierceTargets = Mathf.Max(0, maxPierceTargets);
        homingStrength = Mathf.Max(0.1f, homingStrength);
        homingRange = Mathf.Max(1f, homingRange);
        aoeRadius = Mathf.Max(0f, aoeRadius);
        gravityMultiplier = Mathf.Clamp(gravityMultiplier, -5f, 5f);
    }
    
    #endregion
    
    #region Gizmos
    
    private void OnDrawGizmosSelected()
    {
        // Desenhar área de AOE
        if (hasAOE)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aoeRadius);
        }
        
        // Desenhar range de homing
        if (isHoming)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, homingRange);
            
            // Linha para o alvo de homing
            if (homingTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, homingTarget.transform.position);
            }
        }
        
        // Direção do movimento
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, direction * 2f);
        
        // Desenhar targets já atingidos
        Gizmos.color = Color.gray;
        foreach (GameObject target in hitTargets)
        {
            if (target != null)
            {
                Gizmos.DrawWireSphere(target.transform.position, 0.5f);
            }
        }
    }
    
    #endregion
    
    #region Debug
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugProjectileInfo()
    {
        Debug.Log($"=== PROJECTILE DEBUG ===");
        Debug.Log($"Has Hit: {hasHit}");
        Debug.Log($"Time Alive: {timeAlive:F2}s");
        Debug.Log($"Distance Traveled: {distanceTraveled:F2}");
        Debug.Log($"Speed: {speed}");
        Debug.Log($"Direction: {direction}");
        Debug.Log($"Targets Hit: {hitTargets.Count}");
        Debug.Log($"Is Homing: {isHoming}");
        Debug.Log($"Has AOE: {hasAOE}");
        Debug.Log("========================");
    }
    
    #endregion
}