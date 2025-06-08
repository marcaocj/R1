using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("Health Bar Components")]
    public Slider healthSlider;
    public Slider healthSliderBackground;
    public Image healthFill;
    public Image healthBackground;
    public Text healthText; // Texto para mostrar valores numéricos
    
    [Header("Mana Bar Components")]
    public Slider manaSlider;
    public Image manaFill;
    public Text manaText; // Texto para mostrar valores numéricos
    
    [Header("Colors")]
    public Color healthColor = Color.green;
    public Color lowHealthColor = new Color(0.8f, 0.4f, 0f, 1f); // Laranja
    public Color criticalHealthColor = Color.red;
    public Color manaColor = new Color(0f, 0.4f, 0.8f, 1f); // Azul
    public Color backgroundColorNormal = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color backgroundColorDamaged = new Color(0.8f, 0.2f, 0.2f, 0.8f);
    
    [Header("Animation Settings")]
    public float smoothSpeed = 2f;
    public float damageFlashDuration = 0.2f;
    public bool animateHealthChanges = true;
    
    [Header("Thresholds")]
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.3f;
    [Range(0f, 1f)]
    public float criticalHealthThreshold = 0.15f;
    
    private PlayerStats playerStats;
    private float targetHealthPercentage;
    private float currentDisplayedHealth;
    private float targetManaPercentage;
    private float currentDisplayedMana;
    private bool isFlashing = false;
    
    private Color originalBackgroundColor;
    
    private void Awake()
    {
        InitializeHealthBar();
    }
    
    private void Start()
    {
        // Procurar PlayerStats automaticamente se não estiver configurado
        if (playerStats == null)
        {
            FindPlayerStats();
        }
        
        // Configurar valores iniciais
        if (playerStats != null)
        {
            UpdateHealthDisplay(true); // Force update inicial
            UpdateManaDisplay(true);   // Force update inicial
        }
    }
    
    private void InitializeHealthBar()
    {
        // Configurar referências automáticas se não estiverem definidas
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();
            
        if (healthFill == null && healthSlider != null)
            healthFill = healthSlider.fillRect.GetComponent<Image>();
            
        if (healthBackground == null && healthSliderBackground != null)
            healthBackground = healthSliderBackground.fillRect.GetComponent<Image>();
        
        // Configurar valores dos sliders
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }
        
        if (manaSlider != null)
        {
            manaSlider.minValue = 0f;
            manaSlider.maxValue = 1f;
            manaSlider.value = 1f;
        }
        
        // Configurar cores iniciais
        if (healthFill != null)
            healthFill.color = healthColor;
            
        if (manaFill != null)
            manaFill.color = manaColor;
        
        // Salvar cor original do background
        if (healthBackground != null)
            originalBackgroundColor = healthBackground.color;
        
        currentDisplayedHealth = 1f;
        currentDisplayedMana = 1f;
        targetHealthPercentage = 1f;
        targetManaPercentage = 1f;
    }
    
    private void FindPlayerStats()
    {
        if (playerStats == null)
        {
            // Tentar encontrar através do GameManager primeiro
            if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
            {
                playerStats = GameManager.Instance.CurrentPlayer.GetComponent<PlayerStats>();
            }
            
            // Se não encontrou, procurar na cena
            if (playerStats == null)
            {
                playerStats = FindAnyObjectByType<PlayerStats>();
            }
            
            // Se ainda não encontrou, procurar por tag
            if (playerStats == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerStats = player.GetComponent<PlayerStats>();
                }
            }
        }
        
        // Se encontrou, inscrever nos eventos
        if (playerStats != null)
        {
            Debug.Log("PlayerHealthBarUI: PlayerStats encontrado!");
            
            // Desinscrever primeiro para evitar duplicatas
            playerStats.OnStatsChanged -= OnStatsChanged;
            
            // Inscrever nos eventos
            playerStats.OnStatsChanged += OnStatsChanged;
            
            // Força update inicial
            ForceUpdateDisplay();
        }
        else
        {
            Debug.LogWarning("PlayerHealthBarUI: PlayerStats não encontrado!");
        }
    }
    
    private void Update()
    {
        if (playerStats == null)
        {
            FindPlayerStats();
            return;
        }
        
        UpdateHealthDisplay();
        UpdateManaDisplay();
    }
    
    private void UpdateHealthDisplay(bool forceUpdate = false)
    {
        if (playerStats == null) return;
        
        // Calcular percentage atual
        float newTargetHealth = playerStats.MaxHealth > 0 ? playerStats.CurrentHealth / playerStats.MaxHealth : 0f;
        
        // Verificar se mudou
        if (Mathf.Abs(newTargetHealth - targetHealthPercentage) > 0.001f || forceUpdate)
        {
            targetHealthPercentage = newTargetHealth;
            
            if (!animateHealthChanges || forceUpdate)
            {
                currentDisplayedHealth = targetHealthPercentage;
            }
        }
        
        // Animar mudanças
        if (animateHealthChanges && !forceUpdate)
        {
            currentDisplayedHealth = Mathf.Lerp(currentDisplayedHealth, targetHealthPercentage, Time.deltaTime * smoothSpeed);
        }
        
        // Atualizar slider
        if (healthSlider != null)
        {
            healthSlider.value = currentDisplayedHealth;
        }
        
        // Atualizar cor
        UpdateHealthColor();
        
        // Atualizar texto
        UpdateHealthText();
        
        // Verificar flash de dano
        if (!isFlashing && targetHealthPercentage < currentDisplayedHealth - 0.01f)
        {
            StartCoroutine(FlashDamage());
        }
    }
    
    private void UpdateManaDisplay(bool forceUpdate = false)
    {
        if (manaSlider == null || playerStats == null || playerStats.MaxMana <= 0) return;
        
        // Calcular percentage atual
        float newTargetMana = playerStats.CurrentMana / playerStats.MaxMana;
        
        // Verificar se mudou
        if (Mathf.Abs(newTargetMana - targetManaPercentage) > 0.001f || forceUpdate)
        {
            targetManaPercentage = newTargetMana;
            
            if (!animateHealthChanges || forceUpdate)
            {
                currentDisplayedMana = targetManaPercentage;
            }
        }
        
        // Animar mudanças
        if (animateHealthChanges && !forceUpdate)
        {
            currentDisplayedMana = Mathf.Lerp(currentDisplayedMana, targetManaPercentage, Time.deltaTime * smoothSpeed);
        }
        
        // Atualizar slider
        manaSlider.value = currentDisplayedMana;
        
        // Atualizar cor
        if (manaFill != null)
        {
            manaFill.color = manaColor;
        }
        
        // Atualizar texto
        UpdateManaText();
    }
    
    private void UpdateHealthColor()
    {
        if (healthFill == null) return;
        
        Color targetColor;
        
        if (currentDisplayedHealth <= criticalHealthThreshold)
        {
            targetColor = criticalHealthColor;
        }
        else if (currentDisplayedHealth <= lowHealthThreshold)
        {
            // Interpolação entre cor baixa e crítica
            float t = (currentDisplayedHealth - criticalHealthThreshold) / (lowHealthThreshold - criticalHealthThreshold);
            targetColor = Color.Lerp(criticalHealthColor, lowHealthColor, t);
        }
        else
        {
            // Interpolação entre cor normal e baixa
            float t = (currentDisplayedHealth - lowHealthThreshold) / (1f - lowHealthThreshold);
            targetColor = Color.Lerp(lowHealthColor, healthColor, t);
        }
        
        healthFill.color = targetColor;
    }
    
    private void UpdateHealthText()
    {
        if (healthText != null && playerStats != null)
        {
            healthText.text = $"{Mathf.Ceil(playerStats.CurrentHealth)}/{Mathf.Ceil(playerStats.MaxHealth)}";
        }
    }
    
    private void UpdateManaText()
    {
        if (manaText != null && playerStats != null)
        {
            manaText.text = $"{Mathf.Ceil(playerStats.CurrentMana)}/{Mathf.Ceil(playerStats.MaxMana)}";
        }
    }
    
    private IEnumerator FlashDamage()
    {
        isFlashing = true;
        
        if (healthBackground != null)
        {
            // Flash do background
            healthBackground.color = backgroundColorDamaged;
            yield return new WaitForSeconds(damageFlashDuration);
            
            // Voltar cor original gradualmente
            float elapsed = 0f;
            while (elapsed < damageFlashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / damageFlashDuration;
                healthBackground.color = Color.Lerp(backgroundColorDamaged, originalBackgroundColor, t);
                yield return null;
            }
            
            healthBackground.color = originalBackgroundColor;
        }
        
        isFlashing = false;
    }
    
    // Método chamado quando os stats do player mudam
    private void OnStatsChanged()
    {
        ForceUpdateDisplay();
    }
    
    public void SetPlayer(PlayerStats newPlayerStats)
    {
        // Desinscrever do player anterior
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= OnStatsChanged;
        }
        
        playerStats = newPlayerStats;
        
        if (playerStats != null)
        {
            // Inscrever nos eventos do novo player
            playerStats.OnStatsChanged += OnStatsChanged;
            
            // Atualizar display imediatamente
            ForceUpdateDisplay();
            
            Debug.Log("PlayerHealthBarUI: Novo player configurado!");
        }
    }
    
    public void ForceUpdateDisplay()
    {
        if (playerStats == null) return;
        
        // Forçar update imediato
        UpdateHealthDisplay(true);
        UpdateManaDisplay(true);
        
        Debug.Log($"Health UI Updated: {playerStats.CurrentHealth}/{playerStats.MaxHealth} | Mana: {playerStats.CurrentMana}/{playerStats.MaxMana}");
    }
    
    public void SetHealthBarVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
    
    public void SetAnimationEnabled(bool enabled)
    {
        animateHealthChanges = enabled;
    }
    
    // Método público para testar a UI
    [ContextMenu("Test Health UI")]
    public void TestHealthUI()
    {
        if (playerStats != null)
        {
            Debug.Log($"Testing Health UI - Current: {playerStats.CurrentHealth}/{playerStats.MaxHealth}");
            ForceUpdateDisplay();
        }
        else
        {
            Debug.LogWarning("PlayerStats não configurado!");
        }
    }
    
    private void OnValidate()
    {
        // Garantir que os thresholds estejam em ordem correta
        if (criticalHealthThreshold > lowHealthThreshold)
        {
            criticalHealthThreshold = lowHealthThreshold;
        }
    }
    
    private void OnDestroy()
    {
        // Desinscrever dos eventos
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= OnStatsChanged;
        }
    }
}