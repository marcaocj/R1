using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gerencia o menu principal do jogo
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    public Button createCharacterButton;
    public Button loadGameButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button quitButton;
    
    [Header("Sub Menus")]
    public GameObject mainMenuPanel;
    public GameObject creditsPanel;
    
    [Header("Character Creation")]
    public CharacterCreationManager characterCreationManager;
    
    [Header("Audio")]
    public AudioClip buttonHoverSound;
    public AudioClip buttonClickSound;
    
    [Header("Version Info")]
    public Text versionText;
    public string gameVersion = "v1.0.0";
    
    private void Start()
    {
        InitializeMainMenu();
        SetupEventListeners();
        SetupAudio();
    }
    
    private void InitializeMainMenu()
    {
        // Mostrar painel principal
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        
        // Esconder outros painéis
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
        
        // Configurar versão
        if (versionText != null)
        {
            versionText.text = gameVersion;
        }
        
        // Configurar estado do jogo
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.MainMenu);
        }
        
        // Tocar música do menu principal
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMainMenuMusic();
        }
        
        Debug.Log("Menu principal inicializado");
    }
    
    private void SetupEventListeners()
    {
        // Botão Criar Personagem
        if (createCharacterButton != null)
        {
            createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);
            AddHoverSound(createCharacterButton);
        }
        
        // Botão Carregar Jogo
        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
            AddHoverSound(loadGameButton);
            
            // Verificar se há saves disponíveis
            bool hasSaves = CheckForSavedGames();
            loadGameButton.interactable = hasSaves;
        }
        
        // Botão Configurações
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            AddHoverSound(settingsButton);
        }
        
        // Botão Créditos
        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(OnCreditsClicked);
            AddHoverSound(creditsButton);
        }
        
        // Botão Sair
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
            AddHoverSound(quitButton);
        }
    }
    
    private void SetupAudio()
    {
        // Configurar volume do menu se necessário
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(0.7f);
        }
    }
    
    #region Button Event Handlers
    
    private void OnCreateCharacterClicked()
    {
        PlayButtonSound();
        
        Debug.Log("Botão Criar Personagem clicado");
        
        // Mostrar tela de criação de personagem
        if (characterCreationManager != null)
        {
            characterCreationManager.ShowCharacterCreation();
        }
        else
        {
            Debug.LogError("CharacterCreationManager não encontrado!");
            
            // Fallback: ir direto para o jogo
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
            }
        }
    }
    
    private void OnLoadGameClicked()
    {
        PlayButtonSound();
        
        Debug.Log("Botão Carregar Jogo clicado");
        
        // Implementar carregamento de save
        if (SaveManager.Instance != null)
        {
            bool loaded = SaveManager.Instance.LoadGame(0); // Carregar slot 0
            
            if (loaded)
            {
                Debug.Log("Jogo carregado com sucesso");
            }
            else
            {
                Debug.LogWarning("Falha ao carregar o jogo");
                ShowMessage("Erro ao carregar o jogo!");
            }
        }
        else
        {
            Debug.LogWarning("SaveManager não encontrado");
            ShowMessage("Sistema de save não disponível!");
        }
    }
    
    private void OnSettingsClicked()
    {
        PlayButtonSound();
        
        Debug.Log("Botão Configurações clicado");
        
        // Abrir menu de configurações
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowSettings();
        }
        else
        {
            Debug.LogWarning("UIManager não encontrado");
        }
    }
    
    private void OnCreditsClicked()
    {
        PlayButtonSound();
        
        Debug.Log("Botão Créditos clicado");
        
        // Mostrar créditos
        ShowCredits();
    }
    
    private void OnQuitClicked()
    {
        PlayButtonSound();
        
        Debug.Log("Botão Sair clicado");
        
        // Confirmar saída
        ConfirmQuit();
    }
    
    #endregion
    
    #region Utility Methods
    
    private bool CheckForSavedGames()
    {
        if (SaveManager.Instance != null)
        {
            // Verificar se existe pelo menos um save
            return SaveManager.Instance.HasSave(0);
        }
        
        // Fallback: verificar PlayerPrefs
        return PlayerPrefs.HasKey("PlayerName");
    }
    
    private void ShowCredits()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
        
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }
    }
    
    public void HideCredits()
    {
        PlayButtonSound();
        
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
        
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }
    
    private void ConfirmQuit()
    {
        // Por simplicidade, sair diretamente
        // Em um jogo completo, você mostraria uma caixa de diálogo de confirmação
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
            // Fallback
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
    
    private void ShowMessage(string message)
    {
        // Implementar sistema de mensagens se necessário
        Debug.Log($"Mensagem para o usuário: {message}");
        
        // Por enquanto, apenas log
        // Em um jogo completo, você mostraria uma UI de mensagem
    }
    
    #endregion
    
    #region Audio Methods
    
    private void PlayButtonSound()
    {
        if (buttonClickSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSound);
        }
    }
    
    private void PlayHoverSound()
    {
        if (buttonHoverSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonHoverSound, 0.5f); // Volume mais baixo para hover
        }
    }
    
    private void AddHoverSound(Button button)
    {
        if (button != null)
        {
            // Adicionar evento de hover
            var eventTrigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            // Evento de enter (hover)
            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((eventData) => PlayHoverSound());
            eventTrigger.triggers.Add(pointerEnter);
        }
    }
    
    #endregion
    
    #region Public Methods for UI
    
    /// <summary>
    /// Método público para botões da UI chamarem diretamente
    /// </summary>
    public void CreateCharacterButtonClicked()
    {
        OnCreateCharacterClicked();
    }
    
    public void LoadGameButtonClicked()
    {
        OnLoadGameClicked();
    }
    
    public void SettingsButtonClicked()
    {
        OnSettingsClicked();
    }
    
    public void CreditsButtonClicked()
    {
        OnCreditsClicked();
    }
    
    public void QuitButtonClicked()
    {
        OnQuitClicked();
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Limpar listeners para evitar vazamentos de memória
        if (createCharacterButton != null)
        {
            createCharacterButton.onClick.RemoveAllListeners();
        }
        
        if (loadGameButton != null)
        {
            loadGameButton.onClick.RemoveAllListeners();
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
        }
        
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
        }
    }
}