using UnityEngine;
using System.Collections;

/// <summary>
/// Gerenciador principal do jogo - versão corrigida com melhor integração
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("Game Settings")]
    public GameState currentGameState = GameState.MainMenu;
    public int currentDifficulty = 1; // 0=Easy, 1=Normal, 2=Hard
    public bool isPaused = false;
    public float gameTime = 0f;
    
    [Header("Player References")]
    public GameObject playerPrefab;
    public Transform playerSpawnPoint;
    private GameObject currentPlayer;
    
    [Header("Camera")]
    public Camera mainCamera;
    private CameraController cameraController;
    
    [Header("UI References")]
    public GameObject pauseMenu;
    public GameObject gameOverScreen;
    public GameObject victoryScreen;
    public GameObject loadingScreen;
    
    [Header("Audio")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip victoryMusic;
    public AudioClip gameOverMusic;
    public AudioClip buttonClickSound;
    
    [Header("Game Events")]
    public bool autoSaveEnabled = true;
    public float autoSaveInterval = 300f; // 5 minutos
    private float lastAutoSaveTime;
    
    // Public property for CurrentPlayer
    public GameObject CurrentPlayer => currentPlayer;
    
    protected override void OnSingletonAwake()
    {
        InitializeGame();
    }
    
    private void Start()
    {
        SetupEventListeners();
        SetGameState(GameState.MainMenu);
        lastAutoSaveTime = Time.time;
    }
    
    private void Update()
    {
        if (currentGameState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
            
            // Auto save
            if (autoSaveEnabled && Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                AutoSave();
                lastAutoSaveTime = Time.time;
            }
        }
    }
    
    private void InitializeGame()
    {
        Time.timeScale = 1f;
        
        // Configurar referências principais
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Criar câmera se não existir
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }
        }
        
        // Configurar spawn point se não definido
        if (playerSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("PlayerSpawnPoint");
            spawnPoint.transform.position = Vector3.zero;
            playerSpawnPoint = spawnPoint.transform;
        }
        
        // Garantir que outros managers estejam inicializados
        EnsureManagersInitialized();
    }
    
    private void EnsureManagersInitialized()
    {
        // Força inicialização de managers críticos
        AudioManager.EnsureInstance();
        UIManager.EnsureInstance();
        InputManager.EnsureInstance();
        EventManager.EnsureInstance();
        SaveManager.EnsureInstance();
        
        Debug.Log("GameManager: Todos os managers inicializados");
    }
    
    private void SetupEventListeners()
    {
        EventManager.OnPlayerDeath += HandlePlayerDeath;
        EventManager.OnPauseToggled += TogglePause;
        EventManager.OnGameOver += HandleGameOver;
        EventManager.OnSceneLoaded += HandleSceneLoaded;
    }
    
    #region Game State Management
    
    public void SetGameState(GameState newState)
    {
        GameState previousState = currentGameState;
        currentGameState = newState;
        
        HandleStateTransition(previousState, newState);
    }
    
    private void HandleStateTransition(GameState from, GameState to)
    {
        // Log da transição
        Debug.Log($"Game State: {from} -> {to}");
        
        // Desativar UI do estado anterior
        DeactivateStateUI(from);
        
        // Ativar UI do novo estado
        ActivateStateUI(to);
        
        // Lógica específica para cada transição
        switch (to)
        {
            case GameState.MainMenu:
                HandleMainMenuState();
                break;
            case GameState.Playing:
                HandlePlayingState();
                break;
            case GameState.Paused:
                HandlePausedState();
                break;
            case GameState.GameOver:
                HandleGameOverState();
                break;
            case GameState.Victory:
                HandleVictoryState();
                break;
            case GameState.Loading:
                HandleLoadingState();
                break;
        }
    }
    
    private void DeactivateStateUI(GameState state)
    {
        if (UIManager.Instance == null) return;
        
        switch (state)
        {
            case GameState.MainMenu:
                // UI do main menu será handled pelo UIManager
                break;
            case GameState.Paused:
                UIManager.Instance.HidePauseMenu();
                break;
            case GameState.GameOver:
                UIManager.Instance.HidePanel("GameOver");
                break;
            case GameState.Victory:
                UIManager.Instance.HidePanel("Victory");
                break;
            case GameState.Loading:
                UIManager.Instance.HideLoadingScreen();
                break;
        }
    }
    
    private void ActivateStateUI(GameState state)
    {
        if (UIManager.Instance == null) return;
        
        switch (state)
        {
            case GameState.MainMenu:
                UIManager.Instance.ShowMainMenu();
                break;
            case GameState.Playing:
                UIManager.Instance.ShowGameplayUI();
                break;
            case GameState.Paused:
                UIManager.Instance.ShowPauseMenu();
                break;
            case GameState.GameOver:
                UIManager.Instance.ShowGameOverScreen();
                break;
            case GameState.Victory:
                UIManager.Instance.ShowVictoryScreen();
                break;
            case GameState.Loading:
                UIManager.Instance.ShowLoadingScreen();
                break;
        }
    }
    
    #endregion
    
    #region State Handlers
    
    private void HandleMainMenuState()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        PlayMusic(mainMenuMusic);
        
        // Destruir player atual se existir
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }
    }
    
    private void HandlePlayingState()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        PlayMusic(gameplayMusic);
        
        // Garantir que o player existe
        if (currentPlayer == null)
        {
            SpawnPlayer();
        }
        
        // Configurar câmera
        SetupCameraForPlayer();
    }
    
    private void HandlePausedState()
    {
        Time.timeScale = 0f;
        isPaused = true;
        
        // Pausar sistemas de áudio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseAllSounds();
        }
    }
    
    private void HandleGameOverState()
    {
        Time.timeScale = 0.1f; // Slow motion para efeito dramático
        isPaused = true;
        
        PlayMusic(gameOverMusic);
        
        // Mostrar tela de game over após delay
        StartCoroutine(ShowGameOverAfterDelay(2f));
    }
    
    private void HandleVictoryState()
    {
        Time.timeScale = 0f;
        isPaused = true;
        
        PlayMusic(victoryMusic);
    }
    
    private void HandleLoadingState()
    {
        // Manter time scale normal durante loading
        Time.timeScale = 1f;
    }
    
    private IEnumerator ShowGameOverAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        
        Time.timeScale = 0f;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverScreen();
        }
    }
    
    #endregion
    
    #region Player Management
    
    public void StartNewGame()
    {
        SetGameState(GameState.Loading);
        StartCoroutine(StartNewGameCoroutine());
    }
    
    private IEnumerator StartNewGameCoroutine()
    {
        yield return new WaitForSeconds(1f);
        
        gameTime = 0f;
        SpawnPlayer();
        SetGameState(GameState.Playing);
    }
    
    public void SpawnPlayer()
    {
        if (playerPrefab != null && playerSpawnPoint != null)
        {
            if (currentPlayer != null)
            {
                Destroy(currentPlayer);
            }
            
            currentPlayer = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
            
            // Configurar componentes do player
            SetupPlayerComponents();
            
            // Configurar câmera
            SetupCameraForPlayer();
            
            // Configurar UI
            SetupUIForPlayer();
            
            Debug.Log("Player spawned successfully");
        }
        else
        {
            Debug.LogError("PlayerPrefab ou PlayerSpawnPoint não configurados!");
        }
    }
    
    private void SetupPlayerComponents()
    {
        if (currentPlayer == null) return;
        
        // Garantir que o player tem todos os componentes necessários
        PlayerStats playerStats = currentPlayer.GetComponent<PlayerStats>();
        PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
        PlayerCombat playerCombat = currentPlayer.GetComponent<PlayerCombat>();
        PlayerInventory playerInventory = currentPlayer.GetComponent<PlayerInventory>();
        PlayerEquipment playerEquipment = currentPlayer.GetComponent<PlayerEquipment>();
        StatusEffectManager statusEffectManager = currentPlayer.GetComponent<StatusEffectManager>();
        
        // Adicionar componentes faltantes
        if (playerStats == null)
            playerStats = currentPlayer.AddComponent<PlayerStats>();
        
        if (statusEffectManager == null)
            statusEffectManager = currentPlayer.AddComponent<StatusEffectManager>();
        
        if (playerInventory == null)
            playerInventory = currentPlayer.AddComponent<PlayerInventory>();
        
        if (playerEquipment == null)
            playerEquipment = currentPlayer.AddComponent<PlayerEquipment>();
        
        Debug.Log("Player components configured");
    }
    
    private void SetupCameraForPlayer()
    {
        if (currentPlayer == null) return;
        
        // Configurar câmera para seguir o player
        if (cameraController == null)
        {
            cameraController = FindFirstObjectByType<CameraController>();
            
            if (cameraController == null && mainCamera != null)
            {
                cameraController = mainCamera.gameObject.AddComponent<CameraController>();
            }
        }
        
        if (cameraController != null)
        {
            cameraController.SetTarget(currentPlayer.transform);
        }
    }
    
    private void SetupUIForPlayer()
    {
        if (currentPlayer == null || UIManager.Instance == null) return;
        
        // Configurar health bar UI
        if (UIManager.Instance.healthBarUI != null)
        {
            PlayerStats playerStats = currentPlayer.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                UIManager.Instance.healthBarUI.SetPlayer(playerStats);
            }
        }
    }
    
    #endregion
    
    #region Game Flow Control
    
    public void ResumeGame()
    {
        if (currentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
            
            // Retomar sistemas de áudio
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResumeAllSounds();
            }
        }
    }
    
    public void TogglePause()
    {
        if (currentGameState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
            EventManager.TriggerGamePaused();
        }
        else if (currentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
            EventManager.TriggerGameResumed();
        }
    }
    
    public void HandleLevelComplete()
    {
        SetGameState(GameState.Victory);
    }
    
    public void RestartGame()
    {
        SetGameState(GameState.Loading);
        StartCoroutine(RestartGameCoroutine());
    }
    
    private IEnumerator RestartGameCoroutine()
    {
        yield return new WaitForSeconds(1f);
        
        gameTime = 0f;
        SpawnPlayer();
        SetGameState(GameState.Playing);
    }
    
    public void ReturnToMainMenu()
    {
        SetGameState(GameState.Loading);
        StartCoroutine(ReturnToMainMenuCoroutine());
    }
    
    private IEnumerator ReturnToMainMenuCoroutine()
    {
        yield return new WaitForSeconds(1f);
        
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }
        
        gameTime = 0f;
        SetGameState(GameState.MainMenu);
    }
    
    public void QuitGame()
    {
        // Salvar jogo antes de sair
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveSettings();
        }
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    #endregion
    
    #region Event Handlers
    
    private void HandlePlayerDeath()
    {
        StartCoroutine(DelayedGameOver());
    }
    
    private IEnumerator DelayedGameOver()
    {
        yield return new WaitForSeconds(2f);
        SetGameState(GameState.GameOver);
        EventManager.TriggerGameOver();
    }
    
    private void HandleGameOver()
    {
        SetGameState(GameState.GameOver);
    }
    
    private void HandleSceneLoaded()
    {
        // Reconfigurar jogo após carregar nova cena
        if (currentGameState == GameState.Playing && currentPlayer != null)
        {
            SetupCameraForPlayer();
            SetupUIForPlayer();
        }
    }
    
    #endregion
    
    #region Audio Management
    
    private void PlayMusic(AudioClip musicClip)
    {
        if (musicClip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(musicClip);
        }
    }
    
    public void PlayButtonClick()
    {
        if (buttonClickSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSound);
        }
    }
    
    #endregion
    
    #region Save/Load
    
    private void AutoSave()
    {
        if (SaveManager.Instance != null && currentGameState == GameState.Playing)
        {
            SaveManager.Instance.AutoSave();
            Debug.Log("Auto save completed");
        }
    }
    
    public void SaveGame(int slot = 0)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame(slot);
        }
    }
    
    public void LoadGame(int slot = 0)
    {
        if (SaveManager.Instance != null)
        {
            SetGameState(GameState.Loading);
            StartCoroutine(LoadGameCoroutine(slot));
        }
    }
    
    private IEnumerator LoadGameCoroutine(int slot)
    {
        yield return new WaitForSeconds(0.5f);
        
        if (SaveManager.Instance.LoadGame(slot))
        {
            SetGameState(GameState.Playing);
        }
        else
        {
            Debug.LogError("Failed to load game");
            SetGameState(GameState.MainMenu);
        }
    }
    
    #endregion
    
    #region Public Properties and Methods
    
    public bool IsGamePlaying()
    {
        return currentGameState == GameState.Playing;
    }
    
    public bool IsGamePaused()
    {
        return isPaused;
    }
    
    public float GetGameTime()
    {
        return gameTime;
    }
    
    public GameObject GetCurrentPlayer()
    {
        return currentPlayer;
    }
    
    public GameState GetCurrentGameState()
    {
        return currentGameState;
    }
    
    /// <summary>
    /// Força respawn do player (para debug ou casos especiais)
    /// </summary>
    public void ForceRespawnPlayer()
    {
        SpawnPlayer();
    }
    
    /// <summary>
    /// Teleporta o player para uma posição específica
    /// </summary>
    public void TeleportPlayer(Vector3 position)
    {
        if (currentPlayer != null)
        {
            PlayerController controller = currentPlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.TeleportTo(position);
            }
            else
            {
                currentPlayer.transform.position = position;
            }
        }
    }
    
    /// <summary>
    /// Obtém as estatísticas do player atual
    /// </summary>
    public PlayerStats GetPlayerStats()
    {
        return currentPlayer?.GetComponent<PlayerStats>();
    }
    
    #endregion
    
    #region Debug Methods
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugGameState()
    {
        Debug.Log($"=== GAME STATE DEBUG ===");
        Debug.Log($"Current State: {currentGameState}");
        Debug.Log($"Is Paused: {isPaused}");
        Debug.Log($"Game Time: {gameTime:F1}s");
        Debug.Log($"Player Exists: {currentPlayer != null}");
        Debug.Log($"Time Scale: {Time.timeScale}");
        Debug.Log("=======================");
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugKillPlayer()
    {
        if (currentPlayer != null)
        {
            PlayerStats stats = currentPlayer.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(9999f);
            }
        }
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugCompleteLevel()
    {
        HandleLevelComplete();
    }
    
    #endregion
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // Desinscrever dos eventos
        EventManager.OnPlayerDeath -= HandlePlayerDeath;
        EventManager.OnPauseToggled -= TogglePause;
        EventManager.OnGameOver -= HandleGameOver;
        EventManager.OnSceneLoaded -= HandleSceneLoaded;
    }
}