using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Centraliza a ativação/desativação de painéis de UI
/// Atualizado com sistema de criação de personagem
/// </summary>
public class UIManager : Singleton<UIManager>
{
    [Header("Main UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject gameplayUIPanel;
    public GameObject pauseMenuPanel;
    public GameObject inventoryPanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject settingsPanel;
    public GameObject questPanel;
    
    [Header("Menu Panels")]
    public GameObject characterSelectionPanel;
    public GameObject characterCreationPanel;
    
    [Header("HUD Elements")]
    public PlayerHealthBarUI healthBarUI;
    public GameObject miniMapPanel;
    public GameObject skillBarPanel;
    public GameObject chatPanel;
    
    [Header("Interactive UI")]
    public GameObject tooltipPanel;
    public Text tooltipText;
    public GameObject lootWindowPanel;
    public GameObject dialoguePanel;
    
    [Header("Loading")]
    public GameObject loadingScreen;
    public Slider loadingProgressBar;
    public Text loadingText;
    
    [Header("Transition Effects")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeSpeed = 2f;
    
    // Estados de UI
    private Dictionary<string, GameObject> uiPanels = new Dictionary<string, GameObject>();
    private List<GameObject> activeOverlays = new List<GameObject>();
    private bool isInventoryOpen = false;
    private bool isPauseMenuOpen = false;
    
    protected override void OnSingletonAwake()
    {
        // Registrar painéis no dicionário
        RegisterUIPanels();
        
        // Inscrever nos eventos
        SubscribeToEvents();
        
        // Configurar estado inicial
        InitializeUI();
    }
    
    private void RegisterUIPanels()
    {
        if (mainMenuPanel != null)
            uiPanels["MainMenu"] = mainMenuPanel;
        if (gameplayUIPanel != null)
            uiPanels["GameplayUI"] = gameplayUIPanel;
        if (pauseMenuPanel != null)
            uiPanels["PauseMenu"] = pauseMenuPanel;
        if (inventoryPanel != null)
            uiPanels["Inventory"] = inventoryPanel;
        if (gameOverPanel != null)
            uiPanels["GameOver"] = gameOverPanel;
        if (victoryPanel != null)
            uiPanels["Victory"] = victoryPanel;
        if (settingsPanel != null)
            uiPanels["Settings"] = settingsPanel;
        if (questPanel != null)
            uiPanels["Quest"] = questPanel;
        if (tooltipPanel != null)
            uiPanels["Tooltip"] = tooltipPanel;
        if (lootWindowPanel != null)
            uiPanels["LootWindow"] = lootWindowPanel;
        if (dialoguePanel != null)
            uiPanels["Dialogue"] = dialoguePanel;
        if (loadingScreen != null)
            uiPanels["Loading"] = loadingScreen;
        
        // NOVO: Painéis de menu
        if (characterSelectionPanel != null)
            uiPanels["CharacterSelection"] = characterSelectionPanel;
        if (characterCreationPanel != null)
            uiPanels["CharacterCreation"] = characterCreationPanel;
    }
    
    private void SubscribeToEvents()
    {
        EventManager.OnInventoryToggle += HandleInventoryToggle;
        EventManager.OnShowTooltip += ShowTooltip;
        EventManager.OnHideTooltip += HideTooltip;
        EventManager.OnGamePaused += ShowPauseMenu;
        EventManager.OnGameResumed += HidePauseMenu;
        EventManager.OnGameOver += ShowGameOverScreen;
    }
    
    private void InitializeUI()
    {
        // Esconder todos os painéis exceto o necessário
        HideAllPanels();
        
        // Mostrar painel apropriado baseado no GameManager
        if (GameManager.Instance != null)
        {
            switch (GameManager.Instance.currentGameState)
            {
                case GameState.MainMenu:
                    ShowPanel("MainMenu");
                    break;
                case GameState.Playing:
                    ShowGameplayUI();
                    break;
            }
        }
    }
    
    // Métodos de controle de painéis
    public void ShowPanel(string panelName)
    {
        if (uiPanels.ContainsKey(panelName))
        {
            uiPanels[panelName].SetActive(true);
            
            if (!activeOverlays.Contains(uiPanels[panelName]))
            {
                activeOverlays.Add(uiPanels[panelName]);
            }
        }
        else
        {
            Debug.LogWarning($"UI Panel '{panelName}' não encontrado!");
        }
    }
    
    public void HidePanel(string panelName)
    {
        if (uiPanels.ContainsKey(panelName))
        {
            uiPanels[panelName].SetActive(false);
            activeOverlays.Remove(uiPanels[panelName]);
        }
    }
    
    public void TogglePanel(string panelName)
    {
        if (uiPanels.ContainsKey(panelName))
        {
            bool isActive = uiPanels[panelName].activeSelf;
            if (isActive)
                HidePanel(panelName);
            else
                ShowPanel(panelName);
        }
    }
    
    public void HideAllPanels()
    {
        foreach (var panel in uiPanels.Values)
        {
            if (panel != null)
                panel.SetActive(false);
        }
        activeOverlays.Clear();
    }
    
    // Métodos específicos de UI
    public void ShowGameplayUI()
    {
        HideAllPanels();
        ShowPanel("GameplayUI");
        
        // Ativar elementos do HUD
        if (healthBarUI != null)
            healthBarUI.gameObject.SetActive(true);
        if (miniMapPanel != null)
            miniMapPanel.SetActive(true);
        if (skillBarPanel != null)
            skillBarPanel.SetActive(true);
    }
    
    public void ShowMainMenu()
    {
        HideAllPanels();
        ShowPanel("MainMenu");
    }
    
    /// <summary>
    /// Mostra a tela de seleção de personagem
    /// </summary>
    public void ShowCharacterSelection()
    {
        HideAllPanels();
        ShowPanel("CharacterSelection");
    }
    
    /// <summary>
    /// Mostra a tela de criação de personagem
    /// </summary>
    public void ShowCharacterCreation()
    {
        HideAllPanels();
        ShowPanel("CharacterCreation");
    }
    
    /// <summary>
    /// Esconde a tela de criação de personagem
    /// </summary>
    public void HideCharacterCreation()
    {
        HidePanel("CharacterCreation");
    }
    
    public void ShowPauseMenu()
    {
        ShowPanel("PauseMenu");
        isPauseMenuOpen = true;
    }
    
    public void HidePauseMenu()
    {
        HidePanel("PauseMenu");
        isPauseMenuOpen = false;
    }
    
    public void ShowGameOverScreen()
    {
        ShowPanel("GameOver");
    }
    
    public void ShowVictoryScreen()
    {
        ShowPanel("Victory");
    }
    
    public void ShowSettings()
    {
        ShowPanel("Settings");
    }
    
    public void HideSettings()
    {
        HidePanel("Settings");
    }
    
    // Inventário
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        
        if (isInventoryOpen)
            ShowPanel("Inventory");
        else
            HidePanel("Inventory");
            
        EventManager.TriggerInventoryToggle(isInventoryOpen);
    }
    
    private void HandleInventoryToggle(bool isOpen)
    {
        isInventoryOpen = isOpen;
    }
    
    // Tooltip
    public void ShowTooltip(string text)
    {
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipText.text = text;
            ShowPanel("Tooltip");
            
            // Posicionar tooltip perto do mouse
            Vector3 mousePos = Input.mousePosition;
            tooltipPanel.transform.position = mousePos + new Vector3(10, -10, 0);
        }
    }
    
    public void HideTooltip()
    {
        HidePanel("Tooltip");
    }
    
    // Loading Screen
    public void ShowLoadingScreen(string text = "Carregando...")
    {
        if (loadingText != null)
            loadingText.text = text;
        
        ShowPanel("Loading");
    }
    
    public void HideLoadingScreen()
    {
        HidePanel("Loading");
    }
    
    public void UpdateLoadingProgress(float progress)
    {
        if (loadingProgressBar != null)
        {
            loadingProgressBar.value = progress;
        }
    }
    
    /// <summary>
    /// Transição suave do menu principal para o jogo
    /// </summary>
    public void TransitionToGameplay()
    {
        // Fade out do menu
        StartCoroutine(FadeToGameplay());
    }
    
    private System.Collections.IEnumerator FadeToGameplay()
    {
        // Mostrar loading screen
        ShowLoadingScreen("Entrando no mundo...");
        
        // Fade effect se disponível
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeIn());
        }
        
        // Aguardar um pouco
        yield return new WaitForSeconds(2f);
        
        // Fade out
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeOut());
        }
        
        // Esconder loading e mostrar gameplay
        HideLoadingScreen();
        ShowGameplayUI();
    }
    
    private System.Collections.IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;
        
        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = 0f;
        
        while (fadeCanvasGroup.alpha < 1f)
        {
            fadeCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 1f;
    }
    
    private System.Collections.IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;
        
        fadeCanvasGroup.alpha = 1f;
        
        while (fadeCanvasGroup.alpha > 0f)
        {
            fadeCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.gameObject.SetActive(false);
    }
    
    // Dialogue System
    public void ShowDialogue()
    {
        ShowPanel("Dialogue");
    }
    
    public void HideDialogue()
    {
        HidePanel("Dialogue");
    }
    
    // Loot Window
    public void ShowLootWindow()
    {
        ShowPanel("LootWindow");
    }
    
    public void HideLootWindow()
    {
        HidePanel("LootWindow");
    }
    
    // Quest Panel
    public void ShowQuestPanel()
    {
        ShowPanel("Quest");
    }
    
    public void HideQuestPanel()
    {
        HidePanel("Quest");
    }
    
    #region Button Event Methods
    
    // Métodos de botão (para conectar na UI)
    public void OnPlayButtonClicked()
    {
        PlayUISound("ButtonClick");
        GameManager.Instance?.StartNewGame();
    }
    
    public void OnResumeButtonClicked()
    {
        PlayUISound("ButtonClick");
        GameManager.Instance?.ResumeGame();
    }
    
    public void OnRestartButtonClicked()
    {
        PlayUISound("ButtonClick");
        GameManager.Instance?.RestartGame();
    }
    
    public void OnMainMenuButtonClicked()
    {
        PlayUISound("ButtonClick");
        GameManager.Instance?.ReturnToMainMenu();
    }
    
    public void OnQuitButtonClicked()
    {
        PlayUISound("ButtonClick");
        GameManager.Instance?.QuitGame();
    }
    
    public void OnSettingsButtonClicked()
    {
        PlayUISound("ButtonClick");
        ShowSettings();
    }
    
    public void OnCloseSettingsButtonClicked()
    {
        PlayUISound("ButtonClick");
        HideSettings();
    }
    
    // NOVOS: Métodos para o sistema de criação de personagem
    public void OnCharacterSelectionClicked()
    {
        PlayUISound("ButtonClick");
        ShowCharacterSelection();
    }
    
    public void OnCreateCharacterClicked()
    {
        PlayUISound("ButtonClick");
        ShowCharacterCreation();
    }
    
    public void OnBackToMainMenuClicked()
    {
        PlayUISound("ButtonClick");
        ShowMainMenu();
    }
    
    public void OnLoadCharacterClicked()
    {
        PlayUISound("ButtonClick");
        // Este método será chamado pelo MainMenuManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
    }
    
    private void PlayUISound(string soundName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }
    
    #endregion
    
    #region Public Helper Methods
    
    /// <summary>
    /// Verifica se um painel específico está ativo
    /// </summary>
    public bool IsPanelActive(string panelName)
    {
        if (uiPanels.ContainsKey(panelName))
        {
            return uiPanels[panelName].activeSelf;
        }
        return false;
    }
    
    /// <summary>
    /// Obtém o painel por nome
    /// </summary>
    public GameObject GetPanel(string panelName)
    {
        if (uiPanels.ContainsKey(panelName))
        {
            return uiPanels[panelName];
        }
        return null;
    }
    
    /// <summary>
    /// Adiciona um painel ao sistema
    /// </summary>
    public void RegisterPanel(string panelName, GameObject panel)
    {
        if (panel != null)
        {
            uiPanels[panelName] = panel;
        }
    }
    
    /// <summary>
    /// Remove um painel do sistema
    /// </summary>
    public void UnregisterPanel(string panelName)
    {
        if (uiPanels.ContainsKey(panelName))
        {
            uiPanels.Remove(panelName);
        }
    }
    
    /// <summary>
    /// Mostra uma mensagem temporária na tela
    /// </summary>
    public void ShowTemporaryMessage(string message, float duration = 3f)
    {
        StartCoroutine(TemporaryMessageCoroutine(message, duration));
    }
    
    private System.Collections.IEnumerator TemporaryMessageCoroutine(string message, float duration)
    {
        // Implementar sistema de mensagem temporária se necessário
        Debug.Log($"Mensagem temporária: {message}");
        yield return new WaitForSeconds(duration);
    }
    
    /// <summary>
    /// Força atualização da health bar
    /// </summary>
    public void UpdateHealthBar()
    {
        if (healthBarUI != null)
        {
            healthBarUI.ForceUpdateDisplay();
        }
    }
    
    #endregion
    
    // Propriedades públicas
    public bool IsInventoryOpen => isInventoryOpen;
    public bool IsPauseMenuOpen => isPauseMenuOpen;
    public bool HasActiveOverlays => activeOverlays.Count > 0;
    
    // Propriedades para o sistema de menu
    public bool IsMainMenuActive => IsPanelActive("MainMenu");
    public bool IsCharacterSelectionActive => IsPanelActive("CharacterSelection");
    public bool IsCharacterCreationActive => IsPanelActive("CharacterCreation");
    public bool IsGameplayUIActive => IsPanelActive("GameplayUI");
    
    // Método para verificar se alguma UI está bloqueando input
    public bool IsUIBlockingInput()
    {
        return isPauseMenuOpen || isInventoryOpen || HasActiveOverlays || 
               IsCharacterSelectionActive || IsCharacterCreationActive;
    }
    
    /// <summary>
    /// Verifica se está em estado de menu (qualquer tela de menu)
    /// </summary>
    public bool IsInMenuState()
    {
        return IsMainMenuActive || IsCharacterSelectionActive || IsCharacterCreationActive;
    }
    
    #region Debug Methods
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugUIState()
    {
        Debug.Log("=== UI MANAGER DEBUG ===");
        Debug.Log($"Active Panels: {activeOverlays.Count}");
        Debug.Log($"Inventory Open: {isInventoryOpen}");
        Debug.Log($"Pause Menu Open: {isPauseMenuOpen}");
        Debug.Log($"Main Menu Active: {IsMainMenuActive}");
        Debug.Log($"Character Selection Active: {IsCharacterSelectionActive}");
        Debug.Log($"Character Creation Active: {IsCharacterCreationActive}");
        Debug.Log($"Gameplay UI Active: {IsGameplayUIActive}");
        Debug.Log($"UI Blocking Input: {IsUIBlockingInput()}");
        Debug.Log("========================");
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugShowAllPanels()
    {
        Debug.Log("Showing all registered panels:");
        foreach (var panel in uiPanels)
        {
            Debug.Log($"- {panel.Key}: {(panel.Value != null ? "exists" : "null")}");
        }
    }
    
    #endregion
    
    protected override void OnDestroy()
    {
        // Desinscrever dos eventos
        EventManager.OnInventoryToggle -= HandleInventoryToggle;
        EventManager.OnShowTooltip -= ShowTooltip;
        EventManager.OnHideTooltip -= HideTooltip;
        EventManager.OnGamePaused -= ShowPauseMenu;
        EventManager.OnGameResumed -= HidePauseMenu;
        EventManager.OnGameOver -= ShowGameOverScreen;
        
        // Chamar o OnDestroy da classe base
        base.OnDestroy();
    }
}