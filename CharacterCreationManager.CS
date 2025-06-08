using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gerencia a criação de personagem na tela inicial
/// </summary>
public class CharacterCreationManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject characterCreationPanel;
    public TMP_InputField playerNameInput;
    public Button createCharacterButton;
    public Button cancelButton;
    public Text errorMessageText;
    
    [Header("Character Preview")]
    public GameObject characterPreviewContainer;
    public GameObject[] characterModels; // Diferentes modelos de personagem
    public Button previousCharacterButton;
    public Button nextCharacterButton;
    
    [Header("Character Stats")]
    public Slider strengthSlider;
    public Slider dexteritySlider;
    public Slider intelligenceSlider;
    public Slider vitalitySlider;
    public Text availablePointsText;
    public Button resetStatsButton;
    
    [Header("Audio")]
    public AudioClip buttonClickSound;
    public AudioClip characterSelectSound;
    public AudioClip errorSound;
    
    [Header("Settings")]
    public int maxNameLength = 20;
    public int minNameLength = 3;
    public int totalStatPoints = 20;
    public string gameplaySceneName = "GameplayScene";
    
    // Estado atual
    private int selectedCharacterIndex = 0;
    private int availableStatPoints;
    private bool isCreatingCharacter = false;
    
    // Stats base
    private int baseStrength = 10;
    private int baseDexterity = 10;
    private int baseIntelligence = 10;
    private int baseVitality = 10;
    
    private void Awake()
    {
        // Verificar referências obrigatórias
        if (characterCreationPanel == null)
        {
            Debug.LogError("CharacterCreationManager: characterCreationPanel não está configurado!");
        }
        
        if (playerNameInput == null)
        {
            Debug.LogError("CharacterCreationManager: playerNameInput não está configurado!");
        }
    }
    
    private void Start()
    {
        SetupUI();
        SetupEventListeners();
        
        // Esconder painel inicialmente
        if (characterCreationPanel != null)
        {
            characterCreationPanel.SetActive(false);
        }
    }
    
    private void SetupUI()
    {
        // Configurar pontos de atributo disponíveis
        availableStatPoints = totalStatPoints;
        UpdateStatsUI();
        
        // Configurar modelo inicial
        UpdateCharacterPreview();
        
        // Configurar input field
        if (playerNameInput != null)
        {
            playerNameInput.characterLimit = maxNameLength;
            playerNameInput.onValueChanged.AddListener(OnNameInputChanged);
        }
        
        // Esconder mensagem de erro
        if (errorMessageText != null)
        {
            errorMessageText.gameObject.SetActive(false);
        }
    }
    
    private void SetupEventListeners()
    {
        // Botão de criar personagem
        if (createCharacterButton != null)
        {
            createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);
        }
        
        // Botão de cancelar
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
        
        // Botões de seleção de personagem
        if (previousCharacterButton != null)
        {
            previousCharacterButton.onClick.AddListener(OnPreviousCharacterClicked);
        }
        
        if (nextCharacterButton != null)
        {
            nextCharacterButton.onClick.AddListener(OnNextCharacterClicked);
        }
        
        // Sliders de atributos
        if (strengthSlider != null)
        {
            strengthSlider.onValueChanged.AddListener(OnStrengthChanged);
        }
        
        if (dexteritySlider != null)
        {
            dexteritySlider.onValueChanged.AddListener(OnDexterityChanged);
        }
        
        if (intelligenceSlider != null)
        {
            intelligenceSlider.onValueChanged.AddListener(OnIntelligenceChanged);
        }
        
        if (vitalitySlider != null)
        {
            vitalitySlider.onValueChanged.AddListener(OnVitalityChanged);
        }
        
        // Botão de reset de stats
        if (resetStatsButton != null)
        {
            resetStatsButton.onClick.AddListener(OnResetStatsClicked);
        }
    }
    
    #region Public Methods
    
    /// <summary>
    /// Abre a tela de criação de personagem
    /// </summary>
    public void ShowCharacterCreation()
    {
        if (characterCreationPanel != null)
        {
            characterCreationPanel.SetActive(true);
            
            // Reset do estado
            ResetCharacterCreation();
            
            // Focar no input de nome
            if (playerNameInput != null)
            {
                playerNameInput.Select();
                playerNameInput.ActivateInputField();
            }
            
            PlaySound(characterSelectSound);
            
            Debug.Log("Tela de criação de personagem aberta");
        }
    }
    
    /// <summary>
    /// Fecha a tela de criação de personagem
    /// </summary>
    public void HideCharacterCreation()
    {
        if (characterCreationPanel != null)
        {
            characterCreationPanel.SetActive(false);
            Debug.Log("Tela de criação de personagem fechada");
        }
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnCreateCharacterClicked()
    {
        PlaySound(buttonClickSound);
        
        if (isCreatingCharacter) return;
        
        string playerName = playerNameInput != null ? playerNameInput.text.Trim() : "";
        
        if (ValidateCharacterData(playerName))
        {
            CreateCharacter(playerName);
        }
    }
    
    private void OnCancelClicked()
    {
        PlaySound(buttonClickSound);
        HideCharacterCreation();
    }
    
    private void OnPreviousCharacterClicked()
    {
        PlaySound(buttonClickSound);
        
        if (characterModels != null && characterModels.Length > 0)
        {
            selectedCharacterIndex--;
            if (selectedCharacterIndex < 0)
            {
                selectedCharacterIndex = characterModels.Length - 1;
            }
            
            UpdateCharacterPreview();
        }
    }
    
    private void OnNextCharacterClicked()
    {
        PlaySound(buttonClickSound);
        
        if (characterModels != null && characterModels.Length > 0)
        {
            selectedCharacterIndex++;
            if (selectedCharacterIndex >= characterModels.Length)
            {
                selectedCharacterIndex = 0;
            }
            
            UpdateCharacterPreview();
        }
    }
    
    private void OnNameInputChanged(string newName)
    {
        // Validação em tempo real
        if (errorMessageText != null && errorMessageText.gameObject.activeSelf)
        {
            HideErrorMessage();
        }
    }
    
    private void OnStrengthChanged(float value)
    {
        UpdateStatAllocation();
    }
    
    private void OnDexterityChanged(float value)
    {
        UpdateStatAllocation();
    }
    
    private void OnIntelligenceChanged(float value)
    {
        UpdateStatAllocation();
    }
    
    private void OnVitalityChanged(float value)
    {
        UpdateStatAllocation();
    }
    
    private void OnResetStatsClicked()
    {
        PlaySound(buttonClickSound);
        ResetStats();
    }
    
    #endregion
    
    #region Character Validation and Creation
    
    private bool ValidateCharacterData(string playerName)
    {
        // Validar nome
        if (string.IsNullOrEmpty(playerName))
        {
            ShowErrorMessage("Nome do personagem não pode estar vazio!");
            return false;
        }
        
        if (playerName.Length < minNameLength)
        {
            ShowErrorMessage($"Nome deve ter pelo menos {minNameLength} caracteres!");
            return false;
        }
        
        if (playerName.Length > maxNameLength)
        {
            ShowErrorMessage($"Nome deve ter no máximo {maxNameLength} caracteres!");
            return false;
        }
        
        // Validar caracteres permitidos
        if (!IsValidName(playerName))
        {
            ShowErrorMessage("Nome contém caracteres inválidos!");
            return false;
        }
        
        // Validar se todos os pontos foram distribuídos
        if (availableStatPoints > 0)
        {
            ShowErrorMessage("Distribua todos os pontos de atributo!");
            return false;
        }
        
        return true;
    }
    
    private bool IsValidName(string name)
    {
        // Permitir apenas letras, números e alguns caracteres especiais
        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != ' ' && c != '-' && c != '_')
            {
                return false;
            }
        }
        return true;
    }
    
    private void CreateCharacter(string playerName)
    {
        isCreatingCharacter = true;
        
        Debug.Log($"Criando personagem: {playerName}");
        
        // Salvar dados do personagem
        CharacterData characterData = new CharacterData
        {
            playerName = playerName,
            characterModelIndex = selectedCharacterIndex,
            strength = GetStatValue(strengthSlider, baseStrength),
            dexterity = GetStatValue(dexteritySlider, baseDexterity),
            intelligence = GetStatValue(intelligenceSlider, baseIntelligence),
            vitality = GetStatValue(vitalitySlider, baseVitality)
        };
        
        // Salvar nos PlayerPrefs temporariamente
        SaveCharacterData(characterData);
        
        // Transição para o jogo
        StartGameWithCharacter(characterData);
    }
    
    private void SaveCharacterData(CharacterData data)
    {
        PlayerPrefs.SetString("PlayerName", data.playerName);
        PlayerPrefs.SetInt("CharacterModelIndex", data.characterModelIndex);
        PlayerPrefs.SetInt("PlayerStrength", data.strength);
        PlayerPrefs.SetInt("PlayerDexterity", data.dexterity);
        PlayerPrefs.SetInt("PlayerIntelligence", data.intelligence);
        PlayerPrefs.SetInt("PlayerVitality", data.vitality);
        PlayerPrefs.SetInt("IsNewCharacter", 1);
        
        PlayerPrefs.Save();
        
        Debug.Log("Dados do personagem salvos");
    }
    
    private void StartGameWithCharacter(CharacterData characterData)
    {
        // Mostrar loading se disponível
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLoadingScreen("Iniciando jogo...");
        }
        
        // Carregar cena do jogo
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(gameplaySceneName, true);
        }
        else
        {
            // Fallback usando SceneManager diretamente
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
        }
        
        Debug.Log($"Carregando cena: {gameplaySceneName}");
    }
    
    #endregion
    
    #region UI Updates
    
    private void UpdateCharacterPreview()
    {
        if (characterModels == null || characterModels.Length == 0) return;
        
        // Esconder todos os modelos
        foreach (GameObject model in characterModels)
        {
            if (model != null)
            {
                model.SetActive(false);
            }
        }
        
        // Mostrar modelo selecionado
        if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterModels.Length)
        {
            if (characterModels[selectedCharacterIndex] != null)
            {
                characterModels[selectedCharacterIndex].SetActive(true);
            }
        }
    }
    
    private void UpdateStatsUI()
    {
        if (availablePointsText != null)
        {
            availablePointsText.text = $"Pontos Disponíveis: {availableStatPoints}";
        }
        
        // Atualizar cor do texto baseado nos pontos disponíveis
        if (availablePointsText != null)
        {
            availablePointsText.color = availableStatPoints == 0 ? Color.green : Color.white;
        }
    }
    
    private void UpdateStatAllocation()
    {
        // Calcular pontos usados
        int usedPoints = 0;
        
        if (strengthSlider != null)
            usedPoints += Mathf.RoundToInt(strengthSlider.value);
        if (dexteritySlider != null)
            usedPoints += Mathf.RoundToInt(dexteritySlider.value);
        if (intelligenceSlider != null)
            usedPoints += Mathf.RoundToInt(intelligenceSlider.value);
        if (vitalitySlider != null)
            usedPoints += Mathf.RoundToInt(vitalitySlider.value);
        
        availableStatPoints = totalStatPoints - usedPoints;
        
        // Impedir valores negativos
        if (availableStatPoints < 0)
        {
            availableStatPoints = 0;
            // Ajustar sliders se necessário
        }
        
        UpdateStatsUI();
    }
    
    private void ResetStats()
    {
        if (strengthSlider != null)
            strengthSlider.value = 0;
        if (dexteritySlider != null)
            dexteritySlider.value = 0;
        if (intelligenceSlider != null)
            intelligenceSlider.value = 0;
        if (vitalitySlider != null)
            vitalitySlider.value = 0;
        
        availableStatPoints = totalStatPoints;
        UpdateStatsUI();
    }
    
    private void ResetCharacterCreation()
    {
        // Reset nome
        if (playerNameInput != null)
        {
            playerNameInput.text = "";
        }
        
        // Reset stats
        ResetStats();
        
        // Reset modelo selecionado
        selectedCharacterIndex = 0;
        UpdateCharacterPreview();
        
        // Esconder mensagem de erro
        HideErrorMessage();
        
        // Reset flag
        isCreatingCharacter = false;
    }
    
    private int GetStatValue(Slider slider, int baseStat)
    {
        if (slider != null)
        {
            return baseStat + Mathf.RoundToInt(slider.value);
        }
        return baseStat;
    }
    
    #endregion
    
    #region Error Handling
    
    private void ShowErrorMessage(string message)
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = message;
            errorMessageText.gameObject.SetActive(true);
            
            PlaySound(errorSound);
            
            // Auto-esconder após alguns segundos
            Invoke(nameof(HideErrorMessage), 3f);
        }
        
        Debug.LogWarning($"Erro na criação do personagem: {message}");
    }
    
    private void HideErrorMessage()
    {
        if (errorMessageText != null)
        {
            errorMessageText.gameObject.SetActive(false);
        }
        
        // Cancelar auto-hide se ainda estiver agendado
        CancelInvoke(nameof(HideErrorMessage));
    }
    
    #endregion
    
    #region Audio
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
    
    #endregion
    
    #region Data Classes
    
    [System.Serializable]
    public class CharacterData
    {
        public string playerName;
        public int characterModelIndex;
        public int strength;
        public int dexterity;
        public int intelligence;
        public int vitality;
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Limpar listeners para evitar vazamentos de memória
        if (playerNameInput != null)
        {
            playerNameInput.onValueChanged.RemoveAllListeners();
        }
        
        if (createCharacterButton != null)
        {
            createCharacterButton.onClick.RemoveAllListeners();
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
        }
        
        // Cancelar invokes pendentes
        CancelInvoke();
    }
}