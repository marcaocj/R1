using UnityEngine;

/// <summary>
/// Dados de configuração para um ataque específico
/// </summary>
[System.Serializable]
public class AttackData
{
    [Header("Basic Info")]
    public string attackName = "Basic Attack";
    
    [Header("Damage")]
    public float damageMultiplier = 1f;
    public DamageType damageType = DamageType.Physical;
    
    [Header("Range and Area")]
    public float range = 2f;
    public float angle = 90f;
    
    [Header("Costs")]
    public float manaCost = 0f;
    public float staminaCost = 0f;
    
    [Header("Effects")]
    public float knockbackForce = 0f;
    public float stunDuration = 0f;
    
    [Header("Animation")]
    public string animationTrigger = "Attack";
    public float animationSpeed = 1f;
    
    [Header("Audio")]
    public AudioClip attackSound;
    public AudioClip hitSound;
    
    [Header("Visual Effects")]
    public GameObject hitEffectPrefab;
    public GameObject trailEffectPrefab;
    
    /// <summary>
    /// Construtor padrão
    /// </summary>
    public AttackData()
    {
        attackName = "Basic Attack";
        damageMultiplier = 1f;
        range = 2f;
        angle = 90f;
        manaCost = 0f;
        damageType = DamageType.Physical;
        knockbackForce = 0f;
    }
    
    /// <summary>
    /// Construtor com parâmetros básicos
    /// </summary>
    public AttackData(string name, float damage, float attackRange, float attackAngle)
    {
        attackName = name;
        damageMultiplier = damage;
        range = attackRange;
        angle = attackAngle;
        manaCost = 0f;
        damageType = DamageType.Physical;
        knockbackForce = 0f;
    }
    
    /// <summary>
    /// Cria dados de ataque básico
    /// </summary>
    public static AttackData CreateBasicAttack()
    {
        return new AttackData
        {
            attackName = "Basic Attack",
            damageMultiplier = 1f,
            range = 2f,
            angle = 90f,
            manaCost = 0f,
            damageType = DamageType.Physical,
            knockbackForce = 0f,
            animationTrigger = "Attack",
            animationSpeed = 1f
        };
    }
    
    /// <summary>
    /// Cria dados de ataque pesado
    /// </summary>
    public static AttackData CreateHeavyAttack()
    {
        return new AttackData
        {
            attackName = "Heavy Attack",
            damageMultiplier = 2f,
            range = 2.5f,
            angle = 120f,
            manaCost = 10f,
            damageType = DamageType.Physical,
            knockbackForce = 10f,
            animationTrigger = "HeavyAttack",
            animationSpeed = 0.8f
        };
    }
    
    /// <summary>
    /// Cria dados de ataque rápido
    /// </summary>
    public static AttackData CreateQuickAttack()
    {
        return new AttackData
        {
            attackName = "Quick Attack",
            damageMultiplier = 0.7f,
            range = 1.5f,
            angle = 60f,
            manaCost = 5f,
            damageType = DamageType.Physical,
            knockbackForce = 2f,
            animationTrigger = "QuickAttack",
            animationSpeed = 1.5f
        };
    }
}