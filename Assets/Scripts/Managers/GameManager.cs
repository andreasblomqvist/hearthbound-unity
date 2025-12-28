using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound.Managers
{
    /// <summary>
    /// Main Game Manager
    /// Handles game state, initialization, and coordination between systems
    /// Singleton pattern
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Game State
        public enum GameState
        {
            MainMenu,
            Loading,
            Playing,
            Paused,
            GameOver
        }

        [SerializeField] private GameState currentState = GameState.MainMenu;
        public GameState CurrentState => currentState;
        #endregion

        #region World Settings
        [Header("World Generation")]
        [SerializeField] private int worldSeed = 12345;
        [SerializeField] private bool useRandomSeed = false;
        
        public int WorldSeed => worldSeed;
        #endregion

        #region Player Data
        [Header("Player")]
        [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
        [SerializeField] private GameObject playerPrefab;
        
        private GameObject playerInstance;
        public GameObject Player => playerInstance;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGame();
        }

        private void Start()
        {
            // Initialize world seed
            if (useRandomSeed)
            {
                worldSeed = Random.Range(int.MinValue, int.MaxValue);
                Debug.Log($"🎲 Generated random world seed: {worldSeed}");
            }
            else
            {
                Debug.Log($"🌍 Using world seed: {worldSeed}");
            }

            Random.InitState(worldSeed);
        }
        #endregion

        #region Initialization
        private void InitializeGame()
        {
            Debug.Log("🎮 Initializing Hearthbound...");
            
            // Ensure all managers exist
            EnsureManagerExists<AIManager>();
            EnsureManagerExists<TimeManager>();
            
            Debug.Log("✅ Game initialized");
        }

        private void EnsureManagerExists<T>() where T : MonoBehaviour
        {
            if (FindObjectOfType<T>() == null)
            {
                GameObject go = new GameObject(typeof(T).Name);
                go.AddComponent<T>();
                DontDestroyOnLoad(go);
            }
        }
        #endregion

        #region Game State Management
        public void SetGameState(GameState newState)
        {
            if (currentState == newState) return;

            Debug.Log($"Game State: {currentState} → {newState}");
            currentState = newState;

            switch (newState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.Loading:
                    // Show loading screen
                    break;
            }
        }

        public void StartNewGame()
        {
            SetGameState(GameState.Loading);
            
            // Generate new world seed if random
            if (useRandomSeed)
            {
                worldSeed = Random.Range(int.MinValue, int.MaxValue);
                Random.InitState(worldSeed);
            }
            
            Debug.Log($"🎮 Starting new game with seed: {worldSeed}");
            
            // Spawn player
            SpawnPlayer();
            
            SetGameState(GameState.Playing);
        }

        public void PauseGame()
        {
            SetGameState(GameState.Paused);
        }

        public void ResumeGame()
        {
            SetGameState(GameState.Playing);
        }
        #endregion

        #region Player Management
        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("⚠️ Player prefab not assigned!");
                return;
            }

            if (playerInstance != null)
            {
                Destroy(playerInstance);
            }

            playerInstance = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
            playerInstance.name = "Player";
            Debug.Log("✅ Player spawned");
        }

        public void SetPlayerSpawnPosition(Vector3 position)
        {
            playerSpawnPosition = position;
        }
        #endregion

        #region World Seed Management
        public void SetWorldSeed(int seed)
        {
            worldSeed = seed;
            Random.InitState(seed);
            Debug.Log($"🌍 World seed set to: {seed}");
        }

        public void SetRandomSeed(bool random)
        {
            useRandomSeed = random;
        }
        #endregion

        #region Debug
        [ContextMenu("Print Game Info")]
        private void PrintGameInfo()
        {
            Debug.Log("=== Game Info ===");
            Debug.Log($"State: {currentState}");
            Debug.Log($"World Seed: {worldSeed}");
            Debug.Log($"Player: {(playerInstance != null ? "Active" : "Inactive")}");
        }
        #endregion
    }
}
