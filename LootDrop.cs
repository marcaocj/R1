using UnityEngine;

/// <summary>
/// Configuração de loot drop para inimigos
/// </summary>
[System.Serializable]
public class LootDrop
{
    [Header("Item Info")]
    public Item item;
    public int quantity = 1;
    
    [Header("Drop Chance")]
    [Range(0f, 1f)]
    public float dropChance = 0.1f;
    
    [Header("Conditional Drops")]
    public int minPlayerLevel = 1;
    public int maxPlayerLevel = 999;
    public bool onlyDropIfKilledByPlayer = true;
    
    [Header("Special Conditions")]
    public bool isRareDrop = false;
    public float rareBonusChance = 0f; // Chance extra para drops raros
    
    /// <summary>
    /// Construtor padrão
    /// </summary>
    public LootDrop()
    {
        quantity = 1;
        dropChance = 0.1f;
        minPlayerLevel = 1;
        maxPlayerLevel = 999;
        onlyDropIfKilledByPlayer = true;
    }
    
    /// <summary>
    /// Construtor com parâmetros
    /// </summary>
    public LootDrop(Item dropItem, int dropQuantity, float chance)
    {
        item = dropItem;
        quantity = dropQuantity;
        dropChance = Mathf.Clamp01(chance);
        minPlayerLevel = 1;
        maxPlayerLevel = 999;
        onlyDropIfKilledByPlayer = true;
    }
    
    /// <summary>
    /// Verifica se o item deve ser dropado baseado nas condições
    /// </summary>
    public bool ShouldDrop(GameObject killer = null)
    {
        // Verificar se tem item configurado
        if (item == null)
        {
            return false;
        }
        
        // Verificar se foi morto por player (se necessário)
        if (onlyDropIfKilledByPlayer && killer != null && !killer.CompareTag("Player"))
        {
            return false;
        }
        
        // Verificar nível do player
        if (killer != null && killer.CompareTag("Player"))
        {
            PlayerStats playerStats = killer.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                int playerLevel = playerStats.Level;
                if (playerLevel < minPlayerLevel || playerLevel > maxPlayerLevel)
                {
                    return false;
                }
            }
        }
        
        // Calcular chance final
        float finalChance = dropChance;
        
        // Aplicar bônus para drops raros
        if (isRareDrop)
        {
            finalChance += rareBonusChance;
        }
        
        // Aplicar modificadores baseados no nível do player ou outros fatores
        finalChance = ApplyDropModifiers(finalChance, killer);
        
        // Teste de probabilidade
        return Random.Range(0f, 1f) <= finalChance;
    }
    
    /// <summary>
    /// Aplica modificadores de drop baseado em condições externas
    /// </summary>
    private float ApplyDropModifiers(float baseChance, GameObject killer)
    {
        float modifiedChance = baseChance;
        
        // Modificador de magic find do player
        if (killer != null && killer.CompareTag("Player"))
        {
            PlayerStats playerStats = killer.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                // Se o player tiver magic find, aplicar bônus
                // Isso dependeria da implementação do sistema de magic find
                // modifiedChance *= (1f + magicFind);
            }
        }
        
        // Outros modificadores podem ser adicionados aqui
        // Como eventos especiais, dificuldade, etc.
        
        return Mathf.Clamp01(modifiedChance);
    }
    
    /// <summary>
    /// Obtém a quantidade final a ser dropada (pode incluir variação)
    /// </summary>
    public int GetDropQuantity()
    {
        // Por enquanto retorna a quantidade fixa
        // Pode ser expandido para incluir variação aleatória
        return quantity;
    }
    
    /// <summary>
    /// Cria uma instância do item no mundo
    /// </summary>
    public GameObject CreateWorldItem(Vector3 position)
    {
        if (item == null || item.worldModel == null)
        {
            Debug.LogWarning("LootDrop: Item ou worldModel é null!");
            return null;
        }
        
        // Instanciar o modelo do item no mundo
        GameObject worldItem = Object.Instantiate(item.worldModel, position, Quaternion.identity);
        
        // Adicionar componente LootItem se não existir
        LootItem lootComponent = worldItem.GetComponent<LootItem>();
        if (lootComponent == null)
        {
            lootComponent = worldItem.AddComponent<LootItem>();
        }
        
        // Configurar o loot item
        lootComponent.Initialize(item, GetDropQuantity());
        
        return worldItem;
    }
    
    /// <summary>
    /// Cria texto descritivo do drop para UI
    /// </summary>
    public string GetDropDescription()
    {
        if (item == null)
        {
            return "Drop inválido";
        }
        
        string description = $"{item.itemName}";
        
        if (quantity > 1)
        {
            description += $" x{quantity}";
        }
        
        description += $" ({dropChance:P} chance)";
        
        if (isRareDrop)
        {
            description += " [RARO]";
        }
        
        return description;
    }
    
    /// <summary>
    /// Valida se o drop está configurado corretamente
    /// </summary>
    public bool IsValid()
    {
        return item != null && 
               quantity > 0 && 
               dropChance >= 0f && 
               dropChance <= 1f &&
               minPlayerLevel <= maxPlayerLevel;
    }
}

/// <summary>
/// Classe helper para criar drops comuns facilmente
/// </summary>
public static class LootDropHelper
{
    /// <summary>
    /// Cria um drop comum (baixa chance)
    /// </summary>
    public static LootDrop CreateCommonDrop(Item item, int quantity = 1)
    {
        return new LootDrop(item, quantity, 0.3f); // 30% chance
    }
    
    /// <summary>
    /// Cria um drop incomum
    /// </summary>
    public static LootDrop CreateUncommonDrop(Item item, int quantity = 1)
    {
        return new LootDrop(item, quantity, 0.15f); // 15% chance
    }
    
    /// <summary>
    /// Cria um drop raro
    /// </summary>
    public static LootDrop CreateRareDrop(Item item, int quantity = 1)
    {
        LootDrop drop = new LootDrop(item, quantity, 0.05f); // 5% chance
        drop.isRareDrop = true;
        return drop;
    }
    
    /// <summary>
    /// Cria um drop épico
    /// </summary>
    public static LootDrop CreateEpicDrop(Item item, int quantity = 1)
    {
        LootDrop drop = new LootDrop(item, quantity, 0.01f); // 1% chance
        drop.isRareDrop = true;
        drop.rareBonusChance = 0.02f; // +2% de bônus
        return drop;
    }
    
    /// <summary>
    /// Cria um drop garantido (sempre dropa)
    /// </summary>
    public static LootDrop CreateGuaranteedDrop(Item item, int quantity = 1)
    {
        return new LootDrop(item, quantity, 1f); // 100% chance
    }
    
    /// <summary>
    /// Cria um drop condicional por nível
    /// </summary>
    public static LootDrop CreateLevelBasedDrop(Item item, int minLevel, int maxLevel, float chance = 0.2f)
    {
        LootDrop drop = new LootDrop(item, 1, chance);
        drop.minPlayerLevel = minLevel;
        drop.maxPlayerLevel = maxLevel;
        return drop;
    }
}