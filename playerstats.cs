using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gerencia todas as estatísticas do jogador - VERSÃO CORRIGIDA
/// Correções principais: Integração com EventManager para recompensas e referências de StatModifier
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int level = 1;
    public int experience = 0;
    public int experienceToNextLevel = 100;
    
    [Header("Health and Mana")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float maxMana = 50f;
    public float currentMana = 50f;
    public float healthRegenRate = 1f;
    public float manaRegenRate = 2f;
    
    [Header("Primary Attributes")]
    public int strength = 10;
    public int dexterity = 10;
    public int intelligence = 10;
    public int vitality = 10;
    
    [Header("Combat Stats")]
    public int damage = 10;
    public int armor = 0;
    public float criticalChance = 5f;
    public float criticalDamage = 150f;
    public float attackSpeed = 1f;
    public float movementSpeed = 5f;
    
    [Header("Resistances")]
    public float fireResistance = 0f;
    public float coldResistance = 0f;
    public float lightningResistance = 0f;
    public float poisonResistance = 0f;
    
    [Header("Level Up")]
    public int availableStatPoints = 0;
    public int statPointsPerLevel = 5;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Bônus de equipamentos
    private Dictionary<string, float> equipmentBonuses = new Dictionary<string, float>();
    
    // Buffs temporários - CORREÇÃO: Usar StatModifier do próprio PlayerStats
    private List<StatModifier> activeModifiers = new List<StatModifier>();
    
    // Componentes relacionados
    private StatusEffectManager statusEffectManager;
    private PlayerController playerController;
    private PlayerInventory playerInventory;
    
    // Propriedades calculadas
    public int FinalStrength => Mathf.RoundToInt(strength + GetEquipmentBonus("strength") + GetModifierBonus("strength"));
    public int FinalDexterity => Mathf.RoundToInt(dexterity + GetEquipmentBonus("dexterity") + GetModifierBonus("dexterity"));
    public int FinalIntelligence => Mathf.RoundToInt(intelligence + GetEquipmentBonus("intelligence") + GetModifierBonus("intelligence"));
    public int FinalVitality => Mathf.RoundToInt(vitality + GetEquipmentBonus("vitality") + GetModifierBonus("vitality"));
    
    public float FinalMaxHealth => CalculateMaxHealth();
    public float FinalMaxMana => CalculateMaxMana();
    public int FinalDamage => Mathf.RoundToInt(damage + GetEquipmentBonus("damage") + GetModifierBonus("damage"));
    public int FinalArmor => Mathf.RoundToInt(armor + GetEquipmentBonus("armor") + GetModifierBonus("armor"));
    public float FinalCriticalChance => criticalChance + GetEquipmentBonus("criticalChance") + GetModifierBonus("criticalChance");
    public float FinalCriticalDamage => criticalDamage + GetEquipmentBonus("criticalDamage") + GetModifierBonus("criticalDamage");
    public float FinalAttackSpeed => attackSpeed + (GetEquipmentBonus("attackSpeed") + GetModifierBonus("attackSpeed")) / 100f;
    public float FinalMovementSpeed => movementSpeed * (1f + (GetEquipmentBonus("movementSpeed") + GetModifierBonus("movementSpeed")) / 100f);
    
    // Propriedades públicas para acesso
    public float CurrentHealth => currentHealth;
    public float MaxHealth => FinalMaxHealth;
    public float CurrentMana => currentMana;
    public float MaxMana => FinalMaxMana;
    public int Level => level;
    
    // Eventos
    public System.Action OnStatsChanged;
    public System.Action OnLevelUp;
    
    // Singleton instance para acesso fácil
    private static PlayerStats _instance;
    public static PlayerStats Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlayerStats>();
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Obter componentes
        statusEffectManager = GetComponent<StatusEffectManager>();
        playerController = GetComponent<PlayerController>();
        playerInventory = GetComponent<PlayerInventory>();
    }
    
    private void Start()
    {
        InitializeStats();
        InvokeRepeating(nameof(RegenerateHealthAndMana), 1f, 1f);
        
        // CORREÇÃO: Inscrever nos eventos do EventManager
        SubscribeToEventManager();
    }
    
    /// <summary>
    /// NOVO: Inscrever nos eventos do EventManager para receber recompensas
    /// </summary>
    private void SubscribeToEventManager()
    {
        EventManager.OnPlayerExperienceGained += HandleExperienceGained;
        EventManager.OnGoldChanged += HandleGoldChanged;
        
        if (enableDebugLogs)
            Debug.Log("✅ PlayerStats inscrito nos eventos do EventManager");
    }
    
    private void InitializeStats()
    {
        // Calcular stats baseados no nível inicial
        RecalculateStats();
        
        // Setar vida e mana para o máximo
        currentHealth = FinalMaxHealth;
        currentMana = FinalMaxMana;
        
        // Disparar eventos iniciais
        EventManager.TriggerPlayerHealthChanged(currentHealth, FinalMaxHealth);
        EventManager.TriggerPlayerManaChanged(currentMana, FinalMaxMana);
        
        if (enableDebugLogs)
            Debug.Log($"✅ PlayerStats inicializado - Level: {level}, HP: {currentHealth:F1}/{FinalMaxHealth:F1}, MP: {currentMana:F1}/{FinalMaxMana:F1}");
    }
    
    private void Update()
    {
        UpdateModifiers();
    }
    
    #region Event Handlers - NOVO
    
    /// <summary>
    /// CORREÇÃO: Handler para receber experiência via EventManager
    /// </summary>
    private void HandleExperienceGained(int xpAmount)
    {
        if (enableDebugLogs)
            Debug.Log($"🎯 Recebendo {xpAmount} XP via EventManager");
        
        GainExperience(xpAmount);
    }
    
    /// <summary>
    /// CORREÇÃO: Handler para receber ouro via EventManager
    /// </summary>
    private void HandleGoldChanged(int goldAmount)
    {
        if (enableDebugLogs)
            Debug.Log($"💰 Recebendo {goldAmount} ouro via EventManager");
        
        // Se tem inventário, adicionar ouro lá
        if (playerInventory != null)
        {
            playerInventory.AddGold(goldAmount);
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("⚠️ PlayerInventory não encontrado - ouro não foi adicionado");
        }
    }
    
    #endregion
    
    #region Health and Mana Management
    
    public void TakeDamage(float damageAmount, DamageType damageType = DamageType.Physical)
    {
        if (damageAmount <= 0) return;
        
        // Verificar invulnerabilidade
        if (statusEffectManager != null && statusEffectManager.IsInvulnerable())
        {
            if (enableDebugLogs)
                Debug.Log("🛡️ Player está invulnerável - dano ignorado");
            return;
        }
        
        // Aplicar resistência baseada no tipo de dano
        float resistance = GetResistanceForDamageType(damageType);
        float finalDamage = damageAmount * (1f - resistance / 100f);
        
        // Aplicar redução de armor para dano físico
        if (damageType == DamageType.Physical)
        {
            finalDamage = CalculatePhysicalDamageReduction(finalDamage);
        }
        
        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        
        if (enableDebugLogs)
        {
            Debug.Log($"💥 Player recebeu {finalDamage:F1} de dano ({damageType})");
            Debug.Log($"   Vida: {previousHealth:F1} → {currentHealth:F1}");
        }
        
        // Trigger eventos
        EventManager.TriggerPlayerHealthChanged(currentHealth, FinalMaxHealth);
        EventManager.TriggerDamageDealt(finalDamage, transform.position);
        
        // Verificar morte
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float healAmount)
    {
        if (healAmount <= 0) return;
        
        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(FinalMaxHealth, currentHealth + healAmount);
        
        if (enableDebugLogs && currentHealth != previousHealth)
            Debug.Log($"💚 Player curado: {previousHealth:F1} → {currentHealth:F1} (+{currentHealth - previousHealth:F1})");
        
        EventManager.TriggerPlayerHealthChanged(currentHealth, FinalMaxHealth);
    }
    
    public void UseMana(float manaAmount)
    {
        if (manaAmount <= 0) return;
        
        float previousMana = currentMana;
        currentMana = Mathf.Max(0, currentMana - manaAmount);
        
        if (enableDebugLogs && currentMana != previousMana)
            Debug.Log($"🔵 Mana usada: {previousMana:F1} → {currentMana:F1} (-{previousMana - currentMana:F1})");
        
        EventManager.TriggerPlayerManaChanged(currentMana, FinalMaxMana);
    }
    
    public void RestoreMana(float manaAmount)
    {
        if (manaAmount <= 0) return;
        
        float previousMana = currentMana;
        currentMana = Mathf.Min(FinalMaxMana, currentMana + manaAmount);
        
        if (enableDebugLogs && currentMana != previousMana)
            Debug.Log($"🔷 Mana restaurada: {previousMana:F1} → {currentMana:F1} (+{currentMana - previousMana:F1})");
        
        EventManager.TriggerPlayerManaChanged(currentMana, FinalMaxMana);
    }
    
    public bool HasEnoughMana(float requiredMana)
    {
        return currentMana >= requiredMana;
    }
    
    private void RegenerateHealthAndMana()
    {
        bool healthChanged = false;
        bool manaChanged = false;
        
        // Regeneração de vida
        if (currentHealth < FinalMaxHealth)
        {
            float healthRegen = healthRegenRate + (FinalVitality * 0.1f);
            
            // Aplicar bônus de regeneração de status effects
            if (statusEffectManager != null)
            {
                healthRegen += statusEffectManager.GetStatModifier(StatusEffectManager.StatType.HealthRegeneration);
            }
            
            if (healthRegen > 0)
            {
                Heal(healthRegen);
                healthChanged = true;
            }
        }
        
        // Regeneração de mana
        if (currentMana < FinalMaxMana)
        {
            float manaRegen = manaRegenRate + (FinalIntelligence * 0.1f);
            
            // Aplicar bônus de regeneração de status effects
            if (statusEffectManager != null)
            {
                manaRegen += statusEffectManager.GetStatModifier(StatusEffectManager.StatType.ManaRegeneration);
            }
            
            if (manaRegen > 0)
            {
                RestoreMana(manaRegen);
                manaChanged = true;
            }
        }
    }
    
    private void Die()
    {
        if (enableDebugLogs)
            Debug.Log("💀 Player morreu!");
        
        // Parar movimento se tiver controller
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }
        
        EventManager.TriggerPlayerDeath();
    }
    
    #endregion
    
    #region Experience and Leveling - CORRIGIDO
    
    /// <summary>
    /// CORREÇÃO: Método principal para ganhar experiência
    /// </summary>
    public void GainExperience(int xpAmount)
    {
        if (xpAmount <= 0) return;
        
        int previousExperience = experience;
        experience += xpAmount;
        
        if (enableDebugLogs)
            Debug.Log($"📈 XP ganho: {previousExperience} → {experience} (+{xpAmount})");
        
        // Verificar level up
        while (experience >= experienceToNextLevel)
        {
            LevelUp();
        }
        
        OnStatsChanged?.Invoke();
    }
    
    /// <summary>
    /// MANTIDO: Método alternativo para compatibilidade
    /// </summary>
    public void AddExperience(int xpAmount)
    {
        GainExperience(xpAmount);
    }
    
    /// <summary>
    /// CORRIGIDO: Método para adicionar ouro removido (agora via EventManager)
    /// </summary>
    public void AddGold(int goldAmount)
    {
        // Este método agora só dispara o evento, o ouro é adicionado via EventManager
        if (goldAmount > 0)
        {
            EventManager.TriggerGoldChanged(goldAmount);
        }
    }
    
    private void LevelUp()
    {
        experience -= experienceToNextLevel;
        level++;
        
        // Calcular XP para próximo nível
        experienceToNextLevel = CalculateExperienceForNextLevel();
        
        // Ganhar pontos de atributo
        availableStatPoints += statPointsPerLevel;
        
        // Recalcular stats
        RecalculateStats();
        
        // Curar completamente ao subir de nível
        currentHealth = FinalMaxHealth;
        currentMana = FinalMaxMana;
        
        // Disparar eventos
        EventManager.TriggerPlayerLevelUp(level);
        EventManager.TriggerPlayerHealthChanged(currentHealth, FinalMaxHealth);
        EventManager.TriggerPlayerManaChanged(currentMana, FinalMaxMana);
        
        OnLevelUp?.Invoke();
        
        if (enableDebugLogs)
            Debug.Log($"🎉 LEVEL UP! Novo nível: {level} | Pontos disponíveis: {availableStatPoints}");
    }
    
    private int CalculateExperienceForNextLevel()
    {
        return Mathf.RoundToInt(100 * Mathf.Pow(1.5f, level - 1));
    }
    
    #endregion
    
    #region Stat Point Distribution
    
    public bool AddStatPoint(string statName, int points = 1)
    {
        if (availableStatPoints < points) return false;
        
        switch (statName.ToLower())
        {
            case "strength":
                strength += points;
                break;
            case "dexterity":
                dexterity += points;
                break;
            case "intelligence":
                intelligence += points;
                break;
            case "vitality":
                vitality += points;
                break;
            default:
                return false;
        }
        
        availableStatPoints -= points;
        RecalculateStats();
        OnStatsChanged?.Invoke();
        
        if (enableDebugLogs)
            Debug.Log($"📊 {statName} +{points} | Pontos restantes: {availableStatPoints}");
        
        return true;
    }
    
    #endregion
    
    #region Equipment Bonuses
    
    public void AddEquipmentBonus(string statName, float bonus)
    {
        if (!equipmentBonuses.ContainsKey(statName))
        {
            equipmentBonuses[statName] = 0f;
        }
        
        equipmentBonuses[statName] += bonus;
        RecalculateStats();
        OnStatsChanged?.Invoke();
        
        if (enableDebugLogs && bonus != 0)
            Debug.Log($"⚔️ Bônus de equipamento: {statName} {(bonus > 0 ? "+" : "")}{bonus}");
    }
    
    public void RemoveEquipmentBonus(string statName, float bonus)
    {
        AddEquipmentBonus(statName, -bonus);
    }
    
    public float GetEquipmentBonus(string statName)
    {
        return equipmentBonuses.ContainsKey(statName) ? equipmentBonuses[statName] : 0f;
    }
    
    #endregion
    
    #region Temporary Modifiers (Buffs/Debuffs)
    
    public void AddModifier(StatModifier modifier)
    {
        activeModifiers.Add(modifier);
        RecalculateStats();
        OnStatsChanged?.Invoke();
        
        if (enableDebugLogs)
            Debug.Log($"✨ Modificador adicionado: {modifier.statName} {(modifier.value > 0 ? "+" : "")}{modifier.value} por {modifier.duration}s");
    }
    
    public void RemoveModifier(StatModifier modifier)
    {
        activeModifiers.Remove(modifier);
        RecalculateStats();
        OnStatsChanged?.Invoke();
    }
    
    public void RemoveModifiersBySource(string source)
    {
        int removed = activeModifiers.RemoveAll(m => m.source == source);
        if (removed > 0)
        {
            RecalculateStats();
            OnStatsChanged?.Invoke();
            
            if (enableDebugLogs)
                Debug.Log($"🗑️ {removed} modificadores removidos da fonte: {source}");
        }
    }
    
    private void UpdateModifiers()
    {
        bool removedAny = false;
        for (int i = activeModifiers.Count - 1; i >= 0; i--)
        {
            if (activeModifiers[i].HasExpired())
            {
                activeModifiers.RemoveAt(i);
                removedAny = true;
            }
        }
        
        if (removedAny)
        {
            RecalculateStats();
            OnStatsChanged?.Invoke();
        }
    }
    
    private float GetModifierBonus(string statName)
    {
        float totalBonus = 0f;
        
        foreach (var modifier in activeModifiers)
        {
            if (modifier.statName == statName)
            {
                totalBonus += modifier.GetCurrentValue();
            }
        }
        
        // Adicionar bônus de status effects se disponível
        if (statusEffectManager != null)
        {
            StatusEffectManager.StatType statType = ConvertStringToStatType(statName);
            totalBonus += statusEffectManager.GetStatModifier(statType);
        }
        
        return totalBonus;
    }
    
    private StatusEffectManager.StatType ConvertStringToStatType(string statName)
    {
        switch (statName.ToLower())
        {
            case "strength": return StatusEffectManager.StatType.Strength;
            case "health": return StatusEffectManager.StatType.Health;
            case "mana": return StatusEffectManager.StatType.Mana;
            case "movementspeed": return StatusEffectManager.StatType.MovementSpeed;
            case "attackspeed": return StatusEffectManager.StatType.AttackSpeed;
            case "armor": return StatusEffectManager.StatType.Armor;
            case "criticalchance": return StatusEffectManager.StatType.CriticalChance;
            default: return StatusEffectManager.StatType.Health;
        }
    }
    
    #endregion
    
    #region Calculations
    
    public void RecalculateStats()
    {
        // Recalcular vida e mana máximas
        float newMaxHealth = FinalMaxHealth;
        float newMaxMana = FinalMaxMana;
        
        // Ajustar vida e mana atuais proporcionalmente
        if (maxHealth > 0 && currentHealth > 0)
        {
            float healthRatio = currentHealth / maxHealth;
            currentHealth = newMaxHealth * healthRatio;
        }
        
        if (maxMana > 0 && currentMana > 0)
        {
            float manaRatio = currentMana / maxMana;
            currentMana = newMaxMana * manaRatio;
        }
        
        // Atualizar eventos
        EventManager.TriggerPlayerHealthChanged(currentHealth, newMaxHealth);
        EventManager.TriggerPlayerManaChanged(currentMana, newMaxMana);
    }
    
    private float CalculateMaxHealth()
    {
        float baseHealth = maxHealth;
        float vitalityBonus = FinalVitality * 5f; // 5 HP por ponto de vitalidade
        float equipmentBonus = GetEquipmentBonus("maxHealth");
        float modifierBonus = GetModifierBonus("maxHealth");
        
        return baseHealth + vitalityBonus + equipmentBonus + modifierBonus;
    }
    
    private float CalculateMaxMana()
    {
        float baseMana = maxMana;
        float intelligenceBonus = FinalIntelligence * 3f; // 3 MP por ponto de inteligência
        float equipmentBonus = GetEquipmentBonus("maxMana");
        float modifierBonus = GetModifierBonus("maxMana");
        
        return baseMana + intelligenceBonus + equipmentBonus + modifierBonus;
    }
    
    private float CalculatePhysicalDamageReduction(float incomingDamage)
    {
        float damageReduction = FinalArmor / (FinalArmor + 100f);
        return incomingDamage * (1f - damageReduction);
    }
    
    private float GetResistanceForDamageType(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Fire: return fireResistance;
            case DamageType.Cold: return coldResistance;
            case DamageType.Lightning: return lightningResistance;
            case DamageType.Poison: return poisonResistance;
            case DamageType.Physical: return 0f; // Resistência física é handled pelo armor
            default: return 0f;
        }
    }
    
    #endregion
    
    #region Public Properties
    
    public float HealthPercentage => FinalMaxHealth > 0 ? currentHealth / FinalMaxHealth : 0f;
    public float ManaPercentage => FinalMaxMana > 0 ? currentMana / FinalMaxMana : 0f;
    public bool IsAlive => currentHealth > 0;
    public bool IsAtFullHealth => currentHealth >= FinalMaxHealth;
    public bool IsAtFullMana => currentMana >= FinalMaxMana;
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Debug Player Stats")]
    public void DebugPlayerStats()
    {
        Debug.Log($"=== PLAYER STATS DEBUG ===");
        Debug.Log($"Level: {level} (XP: {experience}/{experienceToNextLevel})");
        Debug.Log($"Health: {currentHealth:F1}/{FinalMaxHealth:F1} ({HealthPercentage:P})");
        Debug.Log($"Mana: {currentMana:F1}/{FinalMaxMana:F1} ({ManaPercentage:P})");
        Debug.Log($"Attributes: STR={FinalStrength}, DEX={FinalDexterity}, INT={FinalIntelligence}, VIT={FinalVitality}");
        Debug.Log($"Combat: DMG={FinalDamage}, ARM={FinalArmor}, CRIT={FinalCriticalChance:F1}%");
        Debug.Log($"Available Stat Points: {availableStatPoints}");
        Debug.Log($"Equipment Bonuses: {equipmentBonuses.Count}");
        Debug.Log($"Active Modifiers: {activeModifiers.Count}");
        Debug.Log("==========================");
    }
    
    [ContextMenu("Test Gain Experience")]
    public void TestGainExperience()
    {
        GainExperience(50);
    }
    
    [ContextMenu("Test Take Damage")]
    public void TestTakeDamage()
    {
        TakeDamage(25f);
    }
    
    [ContextMenu("Test Level Up")]
    public void TestLevelUp()
    {
        experience = experienceToNextLevel;
        GainExperience(1);
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Desinscrever dos eventos do EventManager
        EventManager.OnPlayerExperienceGained -= HandleExperienceGained;
        EventManager.OnGoldChanged -= HandleGoldChanged;
        
        if (_instance == this)
        {
            _instance = null;
        }
        
        if (enableDebugLogs)
            Debug.Log("🗑️ PlayerStats destruído e desinscrito dos eventos");
    }
}
