using UnityEngine;

/// <summary>
/// Enums globais do jogo - versão corrigida e unificada
/// </summary>

// ===== EQUIPMENT ENUMS =====
public enum EquipmentSlot
{
    None,
    MainHand,
    OffHand,
    TwoHanded,
    Helmet,
    Chest,
    Legs,
    Boots,
    Gloves,
    Ring1,
    Ring2,
    Necklace,
    Belt
}

public enum EquipmentType
{
    Weapon,
    Armor,
    Accessory,
    Shield
}

// ===== SKILL ENUMS =====
public enum SkillType
{
    Instant,        // Efeito instantâneo
    Projectile,     // Projétil
    AreaOfEffect,   // Área de efeito (AOE)
    Buff,           // Melhoria
    Heal,           // Cura
    Channel,        // Canalização
    Toggle          // Liga/desliga
}

public enum TargetType
{
    Self,       // Próprio caster
    Single,     // Alvo único
    Multiple,   // Múltiplos alvos
    Enemy,      // Apenas inimigos
    Ally,       // Apenas aliados
    Ground      // Posição no chão
}

// ===== DAMAGE ENUMS =====
public enum DamageType
{
    Physical,
    Magic,
    Fire,
    Cold,
    Lightning,
    Poison,
    True // Ignora resistências
}

// ===== ITEM ENUMS =====
public enum ItemType
{
    Consumable,     // Poções, comida
    Equipment,      // Armas, armaduras
    Material,       // Materiais de craft
    Quest,          // Itens de quest
    Key,           // Chaves
    Currency,      // Moedas, gemas
    Misc           // Outros
}

public enum ItemRarity
{
    Common,        // Branco
    Uncommon,      // Verde
    Rare,          // Azul
    Epic,          // Roxo
    Legendary,     // Laranja
    Mythic         // Vermelho
}

// ===== STAT ENUMS =====
public enum StatType
{
    // Atributos primários
    Strength,
    Dexterity,
    Intelligence,
    Vitality,
    
    // Stats de combate
    Damage,
    Armor,
    CriticalChance,
    CriticalDamage,
    AttackSpeed,
    
    // Stats de movimento
    MovementSpeed,
    
    // Stats de vida e mana
    MaxHealth,
    MaxMana,
    HealthRegeneration,
    ManaRegeneration,
    
    // Resistências
    FireResistance,
    ColdResistance,
    LightningResistance,
    PoisonResistance,
    PhysicalResistance,
    
    // Stats especiais
    ExperienceGain,
    GoldFind,
    MagicFind,
    SkillCooldownReduction
}

// ===== QUEST ENUMS =====
public enum QuestType
{
    Kill,
    Collect,
    Deliver,
    GoTo,
    Interact,
    Survive,
    Escort,
    Custom
}

public enum QuestStatus
{
    NotStarted,
    Active,
    Completed,
    Failed,
    TurnedIn
}

// ===== STATUS EFFECT ENUMS =====
public enum StatusEffectType
{
    Poison,
    Regeneration,
    Strength,
    Weakness,
    Speed,
    Slow,
    Shield,
    Burn,
    Freeze,
    Stun,
    Silence,
    Invisibility,
    Invulnerability,
    CriticalBoost,
    ArmorBoost,
    DamageReduction,
    HealthBoost,
    ManaBoost,
    ExperienceBoost,
    Heal
}

public enum StackBehavior
{
    None,           // Não empilha
    Stack,          // Empilha efeitos
    RefreshDuration, // Renova duração
    Replace         // Substitui efeito anterior
}

// ===== EFFECT ENUMS =====
public enum EffectType
{
    Instant,
    DamageOverTime,
    HealOverTime,
    Buff,
    Debuff,
    Root,
    Slow,
    Stun,
    Silence,
    ManaRegeneration,
    Heal,
    Custom
}

public enum EffectTarget
{
    Self,       // Próprio caster
    Enemy,      // Inimigos
    Ally,       // Aliados
    Anyone      // Qualquer alvo
}

public enum ModifierType
{
    Flat,        // +10 damage
    Percentage   // +10% damage
}

// ===== GAME STATE ENUMS =====
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Victory,
    Loading
}

// ===== AI ENUMS =====
public enum AIState
{
    Idle,
    Patrol,
    Alert,
    Chase,
    Combat,
    Search,
    Return,
    Dead,
    Stunned
}