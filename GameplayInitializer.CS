using UnityEngine;
using System.Collections;

/// <summary>
/// Inicializa o gameplay após transição do menu principal
/// Aplica os dados do personagem criado e configura o jogo
/// </summary>
public class GameplayInitializer : MonoBehaviour
{
    [Header("Player Setup")]
    public GameObject[] playerPrefabs; // Diferentes modelos de personagem
    public Transform playerSpawnPoint;
    
    [Header("Game World")]
    public GameObject[] enemySpawnPoints;
    public GameObject[] enemyPrefabs;
    public int initialEnemyCount = 5;
    
    [Header("UI Setup")]
    public GameObject gameplayUICanvas;
    public GameObject playerHUD;
    
    [Header("World Settings")]
    public bool spawnEnemiesOnStart = true;
    public float enemySpawnDelay = 2f;
    public int maxEnemiesInWorld = 10;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Estado interno
    private GameObject currentPlayer;
    private int currentEnemyCount = 0;
    private bool isInitialized = false;
    
    private void Start()
    {
        StartCoroutine(InitializeGameplayCoroutine());
    }
    
    private IEnumerator InitializeGameplayCoroutine()
    {
        // Aguardar um frame para todos os sistemas estarem prontos
        yield return null;
        
        if (enableDebugLogs)
            Debug.Log("🎮 Iniciando inicialização do gameplay...");
        
        // 1. Configurar estado do jogo
        SetupGameState();
        
        // 2. Configurar UI
        SetupGameplayUI();
        
        // 3. Criar player com dados salvos
        yield return StartCoroutine(SetupPlayerCoroutine());
        
        // 4. Configurar câmera
        SetupCamera();
        
        // 5. Configurar mundo do jogo
        SetupGameWorld();
        
        // 6. Spawnar inimigos
        if (spawnEnemiesOnStart)
        {
            yield return StartCoroutine(SpawnInitialEnemiesCoroutine());
        }
        
        // 7. Finalizar inicialização
        FinalizeInitialization();
        
        if (enableDebugLogs)
            Debug.Log("✅ Gameplay inicializado com sucesso!");
    }
    
    #region Setup Methods
    
    private void SetupGameState()
    {
        // Configurar GameManager para estado de jogo
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Playing);
            
            if (enableDebugLogs)
                Debug.Log("🎮 Estado do jogo definido para Playing");
        }
        
        // Configurar música do gameplay
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }
        
        // Configurar time scale normal
        Time.timeScale = 1f;
    }
    
    private void SetupGameplayUI()
    {
        // Ativar UI do gameplay
        if (gameplayUICanvas != null)
        {
            gameplayUICanvas.SetActive(true);
        }
        
        if (playerHUD != null)
        {
            playerHUD.SetActive(true);
        }
        
        // Configurar UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameplayUI();
        }
        
        if (enableDebugLogs)
            Debug.Log("🖥️ UI do gameplay configurada");
    }
    
    private IEnumerator SetupPlayerCoroutine()
    {
        // Verificar se existe dados de personagem salvos
        if (PlayerPrefs.GetInt("IsNewCharacter", 0) == 1)
        {
            // Carregar dados do personagem criado
            CharacterCreationManager.CharacterData characterData = LoadCharacterData();
            
            if (characterData != null)
            {
                yield return StartCoroutine(CreatePlayerFromData(characterData));
            }
            else
            {
                // Fallback: criar personagem padrão
                yield return StartCoroutine(CreateDefaultPlayer());
            }
            
            // Limpar flag de novo personagem
            PlayerPrefs.SetInt("IsNewCharacter", 0);
            PlayerPrefs.Save();
        }
        else
        {
            // Tentar carregar save existente ou criar padrão
            bool loadedFromSave = TryLoadExistingPlayer();
            
            if (!loadedFromSave)
            {
                yield return StartCoroutine(CreateDefaultPlayer());
            }
        }
    }
    
    private CharacterCreationManager.CharacterData LoadCharacterData()
    {
        if (!PlayerPrefs.HasKey("PlayerName"))
        {
            if (enableDebugLogs)
                Debug.LogWarning("⚠️ Dados de personagem não encontrados");
            return null;
        }
        
        CharacterCreationManager.CharacterData data = new CharacterCreationManager.CharacterData
        {
            playerName = PlayerPrefs.GetString("PlayerName", "Herói"),
            characterModelIndex = PlayerPrefs.GetInt("CharacterModelIndex", 0),
            strength = PlayerPrefs.GetInt("PlayerStrength", 10),
            dexterity = PlayerPrefs.GetInt("PlayerDexterity", 10),
            intelligence = PlayerPrefs.GetInt("PlayerIntelligence", 10),
            vitality = PlayerPrefs.GetInt("PlayerVitality", 10)
        };
        
        if (enableDebugLogs)
            Debug.Log($"📊 Dados carregados - Nome: {data.playerName}, STR: {data.strength}, DEX: {data.dexterity}, INT: {data.intelligence}, VIT: {data.vitality}");
        
        return data;
    }
    
    private IEnumerator CreatePlayerFromData(CharacterCreationManager.CharacterData data)
    {
        // Determinar qual prefab usar
        GameObject playerPrefab = GetPlayerPrefab(data.characterModelIndex);
        
        if (playerPrefab == null)
        {
            Debug.LogError("❌ Prefab do player não encontrado!");
            yield break;
        }
        
        // Spawnar player
        Vector3 spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = playerSpawnPoint != null ? playerSpawnPoint.rotation : Quaternion.identity;
        
        currentPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        currentPlayer.name = $"Player_{data.playerName}";
        
        // Aguardar um frame para componentes inicializarem
        yield return null;
        
        // Configurar stats do player
        ApplyCharacterDataToPlayer(data);
        
        // Registrar no GameManager
        if (GameManager.Instance != null)
        {
            // Se GameManager tem método para definir player atual
            typeof(GameManager).GetField("currentPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(GameManager.Instance, currentPlayer);
        }
        
        if (enableDebugLogs)
            Debug.Log($"👤 Player '{data.playerName}' criado com sucesso!");
    }
    
    private IEnumerator CreateDefaultPlayer()
    {
        GameObject playerPrefab = GetPlayerPrefab(0); // Usar primeiro modelo
        
        if (playerPrefab == null)
        {
            Debug.LogError("❌ Nenhum prefab de player configurado!");
            yield break;
        }
        
        Vector3 spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        currentPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        currentPlayer.name = "Player_Default";
        
        yield return null;
        
        // Aplicar stats padrão
        ApplyDefaultStatsToPlayer();
        
        if (enableDebugLogs)
            Debug.Log("👤 Player padrão criado");
    }
    
    private GameObject GetPlayerPrefab(int modelIndex)
    {
        if (playerPrefabs != null && playerPrefabs.Length > 0)
        {
            int safeIndex = Mathf.Clamp(modelIndex, 0, playerPrefabs.Length - 1);
            return playerPrefabs[safeIndex];
        }
        
        return null;
    }
    
    private void ApplyCharacterDataToPlayer(CharacterCreationManager.CharacterData data)
    {
        if (currentPlayer == null) return;
        
        PlayerStats playerStats = currentPlayer.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            // Aplicar atributos
            playerStats.strength = data.strength;
            playerStats.dexterity = data.dexterity;
            playerStats.intelligence = data.intelligence;
            playerStats.vitality = data.vitality;
            
            // Recalcular stats derivados
            playerStats.RecalculateStats();
            
            if (enableDebugLogs)
                Debug.Log($"📊 Stats aplicados - STR: {data.strength}, DEX: {data.dexterity}, INT: {data.intelligence}, VIT: {data.vitality}");
        }
        
        // Configurar nome se houver componente de nome
        var nameDisplay = currentPlayer.GetComponentInChildren<UnityEngine.UI.Text>();
        if (nameDisplay != null)
        {
            nameDisplay.text = data.playerName;
        }
    }
    
    private void ApplyDefaultStatsToPlayer()
    {
        if (currentPlayer == null) return;
        
        PlayerStats playerStats = currentPlayer.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            // Stats padrão balanceados
            playerStats.strength = 12;
            playerStats.dexterity = 12;
            playerStats.intelligence = 12;
            playerStats.vitality = 12;
            
            playerStats.RecalculateStats();
        }
    }
    
    private bool TryLoadExistingPlayer()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave(0))
        {
            return SaveManager.Instance.LoadGame(0);
        }
        
        return false;
    }
    
    private void SetupCamera()
    {
        if (currentPlayer == null) return;
        
        // Configurar câmera para seguir o player
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        
        if (cameraController != null)
        {
            cameraController.SetTarget(currentPlayer.transform);
            
            if (enableDebugLogs)
                Debug.Log("📷 Câmera configurada para seguir o player");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("⚠️ CameraController não encontrado na cena");
        }
    }
    
    private void SetupGameWorld()
    {
        // Configurar elementos do mundo do jogo
        
        // Ativar sistemas de quest se disponível
        if (QuestManager.Instance != null)
        {
            // Pode iniciar quests automáticas aqui
            if (enableDebugLogs)
                Debug.Log("📜 Sistema de quests inicializado");
        }
        
        // Configurar spawn points de inimigos
        if (enemySpawnPoints != null)
        {
            foreach (GameObject spawnPoint in enemySpawnPoints)
            {
                if (spawnPoint != null)
                {
                    spawnPoint.SetActive(true);
                }
            }
        }
        
        if (enableDebugLogs)
            Debug.Log("🌍 Mundo do jogo configurado");
    }
    
    private IEnumerator SpawnInitialEnemiesCoroutine()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning("⚠️ Nenhum prefab de inimigo configurado");
            yield break;
        }
        
        for (int i = 0; i < initialEnemyCount && currentEnemyCount < maxEnemiesInWorld; i++)
        {
            SpawnRandomEnemy();
            currentEnemyCount++;
            
            // Pequena pausa entre spawns para distribuir carga
            yield return new WaitForSeconds(enemySpawnDelay);
        }
        
        if (enableDebugLogs)
            Debug.Log($"👹 {currentEnemyCount} inimigos spawnadaos no mundo");
    }
    
    private void SpawnRandomEnemy()
    {
        // Escolher prefab aleatório
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        
        // Escolher posição de spawn
        Vector3 spawnPosition = GetRandomSpawnPosition();
        
        // Spawnar inimigo
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemy.name = $"Enemy_{currentEnemyCount}";
        
        // Inscrever no evento de morte para respawn
        EnemyStats enemyStats = enemy.GetComponent<EnemyStats>();
        if (enemyStats != null)
        {
            enemyStats.OnDeath += () => OnEnemyDied(enemy);
        }
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
        {
            // Usar spawn points definidos
            GameObject randomSpawnPoint = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
            return randomSpawnPoint.transform.position;
        }
        else if (currentPlayer != null)
        {
            // Spawnar ao redor do player
            Vector3 playerPos = currentPlayer.transform.position;
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(10f, 20f);
            return new Vector3(playerPos.x + randomCircle.x, playerPos.y, playerPos.z + randomCircle.y);
        }
        else
        {
            // Posição aleatória padrão
            return new Vector3(Random.Range(-20f, 20f), 0f, Random.Range(-20f, 20f));
        }
    }
    
    private void OnEnemyDied(GameObject enemy)
    {
        currentEnemyCount--;
        
        // Respawn após delay se ainda há espaço
        if (currentEnemyCount < maxEnemiesInWorld)
        {
            StartCoroutine(RespawnEnemyCoroutine());
        }
    }
    
    private IEnumerator RespawnEnemyCoroutine()
    {
        yield return new WaitForSeconds(5f); // Aguardar 5 segundos antes de respawn
        
        if (currentEnemyCount < maxEnemiesInWorld)
        {
            SpawnRandomEnemy();
            currentEnemyCount++;
        }
    }
    
    private void FinalizeInitialization()
    {
        isInitialized = true;
        
        // Configurar health bar UI para o player
        if (UIManager.Instance?.healthBarUI != null && currentPlayer != null)
        {
            PlayerStats playerStats = currentPlayer.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                UIManager.Instance.healthBarUI.SetPlayer(playerStats);
            }
        }
        
        // Esconder loading screen
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideLoadingScreen();
        }
        
        // Salvar estado inicial
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSave();
        }
        
        if (enableDebugLogs)
            Debug.Log("✨ Inicialização finalizada - Jogo pronto!");
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Força respawn de inimigos
    /// </summary>
    public void ForceRespawnEnemies()
    {
        if (isInitialized)
        {
            StartCoroutine(SpawnInitialEnemiesCoroutine());
        }
    }
    
    /// <summary>
    /// Obtém referência do player atual
    /// </summary>
    public GameObject GetCurrentPlayer()
    {
        return currentPlayer;
    }
    
    /// <summary>
    /// Verifica se a inicialização foi completada
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Debug - Spawn Enemy")]
    public void DebugSpawnEnemy()
    {
        if (Application.isPlaying && isInitialized)
        {
            SpawnRandomEnemy();
            currentEnemyCount++;
        }
    }
    
    [ContextMenu("Debug - Player Info")]
    public void DebugPlayerInfo()
    {
        if (currentPlayer != null)
        {
            PlayerStats stats = currentPlayer.GetComponent<PlayerStats>();
            if (stats != null)
            {
                Debug.Log($"Player Info - Nome: {currentPlayer.name}, Level: {stats.level}, HP: {stats.currentHealth}/{stats.maxHealth}");
            }
        }
    }
    
    #endregion
}