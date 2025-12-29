using UnityEngine;

namespace Hearthbound.World
{
    /// <summary>
    /// World Seed Manager
    /// Manages seed-based world generation for reproducible worlds
    /// Same seed = same world every time
    /// </summary>
    public class WorldSeedManager : MonoBehaviour
    {
        #region Singleton
        private static WorldSeedManager _instance;
        public static WorldSeedManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<WorldSeedManager>();
                }
                return _instance;
            }
        }
        #endregion

        #region Seed Configuration
        [Header("World Seed")]
        [SerializeField] private int worldSeed = 12345;
        [SerializeField] private bool useRandomSeed = false;
        [SerializeField] private bool generateOnStart = false; // Changed default to false to preserve existing terrain
        [SerializeField] private bool preserveExistingTerrain = true; // If true, won't regenerate if terrain already has data

        public int CurrentSeed => worldSeed;
        #endregion

        #region Seed History
        [Header("Seed History")]
        [SerializeField] private int[] recentSeeds = new int[10];
        private int seedHistoryIndex = 0;
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

            InitializeSeed();
        }

        private void Start()
        {
            if (generateOnStart)
            {
                // Check if we should preserve existing terrain
                if (preserveExistingTerrain && TerrainAlreadyExists())
                {
                    Debug.Log("🌍 Terrain already exists - preserving it (set 'Generate On Start' or 'Preserve Existing Terrain' to false to regenerate)");
                    return; // Don't regenerate
                }
                
                GenerateWorld();
            }
            else
            {
                Debug.Log("🌍 Generate On Start is disabled - terrain will not be regenerated automatically");
            }
        }
        
        /// <summary>
        /// Check if terrain already has generated data
        /// </summary>
        private bool TerrainAlreadyExists()
        {
            TerrainGenerator terrainGen = FindObjectOfType<TerrainGenerator>();
            if (terrainGen == null) return false;
            
            Terrain terrain = terrainGen.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null) return false;
            
            // Check if terrain has been generated (has heightmap data)
            int heightmapWidth = terrain.terrainData.heightmapResolution;
            int heightmapHeight = terrain.terrainData.heightmapResolution;
            
            if (heightmapWidth <= 0 || heightmapHeight <= 0) return false;
            
            // Sample multiple points to see if terrain has actual data (not flat/default)
            int samples = Mathf.Min(20, heightmapWidth);
            bool hasVariation = false;
            float firstHeight = -1f;
            
            for (int i = 0; i < samples; i++)
            {
                int x = (int)(heightmapWidth * (float)(i + 1) / (samples + 1));
                int z = (int)(heightmapHeight * (float)(i + 1) / (samples + 1));
                
                if (x >= heightmapWidth) x = heightmapWidth - 1;
                if (z >= heightmapHeight) z = heightmapHeight - 1;
                
                float height = terrain.terrainData.GetHeight(x, z);
                
                if (firstHeight < 0)
                {
                    firstHeight = height;
                }
                else if (Mathf.Abs(height - firstHeight) > 0.01f) // Has height variation
                {
                    hasVariation = true;
                    break;
                }
            }
            
            return hasVariation || firstHeight > 0.1f; // Has data if there's variation or any significant height
        }
        #endregion

        #region Seed Management
        private void InitializeSeed()
        {
            if (useRandomSeed)
            {
                GenerateRandomSeed();
            }
            else
            {
                SetSeed(worldSeed);
            }
        }

        public void SetSeed(int seed)
        {
            worldSeed = seed;
            Random.InitState(seed);
            AddToHistory(seed);
            Debug.Log($"🌍 World seed set to: {seed}");
        }

        public void GenerateRandomSeed()
        {
            // Generate a positive random seed (0 to int.MaxValue)
            // Using a large range for variety, but keeping it positive for clarity
            worldSeed = Random.Range(0, int.MaxValue);
            Random.InitState(worldSeed);
            AddToHistory(worldSeed);
            Debug.Log($"🎲 Generated random world seed: {worldSeed}");
        }

        public void SetSeedFromString(string seedString)
        {
            // Try to parse as int
            if (int.TryParse(seedString, out int seed))
            {
                SetSeed(seed);
            }
            else
            {
                // Use string hash as seed
                seed = seedString.GetHashCode();
                SetSeed(seed);
                Debug.Log($"🔤 Converted string '{seedString}' to seed: {seed}");
            }
        }

        private void AddToHistory(int seed)
        {
            recentSeeds[seedHistoryIndex] = seed;
            seedHistoryIndex = (seedHistoryIndex + 1) % recentSeeds.Length;
        }

        public int[] GetSeedHistory()
        {
            return recentSeeds;
        }
        #endregion

        #region World Generation
        public void GenerateWorld()
        {
            Debug.Log($"🌍 Generating world with seed: {worldSeed}");
            
            // Initialize random with seed
            Random.InitState(worldSeed);
            
            // Find and trigger all world generators
            TerrainGenerator terrainGen = FindObjectOfType<TerrainGenerator>();
            if (terrainGen != null)
            {
                terrainGen.GenerateTerrain(worldSeed);
            }
            
            VillageBuilder villageBuilder = FindObjectOfType<VillageBuilder>();
            if (villageBuilder != null)
            {
                villageBuilder.GenerateVillages(worldSeed);
            }
            
            ForestGenerator forestGen = FindObjectOfType<ForestGenerator>();
            if (forestGen != null)
            {
                forestGen.GenerateForests(worldSeed);
            }
            
            WaterGenerator waterGen = FindObjectOfType<WaterGenerator>();
            if (waterGen != null)
            {
                waterGen.GenerateWater();
            }
            
            Debug.Log("✅ World generation complete!");
        }

        public void RegenerateWorld()
        {
            Debug.Log("🔄 Regenerating world...");
            
            // Clear existing world
            ClearWorld();
            
            // Generate new world with current seed
            GenerateWorld();
        }

        public void GenerateNewRandomWorld()
        {
            GenerateRandomSeed();
            ClearWorld();
            GenerateWorld();
        }

        private void ClearWorld()
        {
            // Find and clear all generated content
            TerrainGenerator terrainGen = FindObjectOfType<TerrainGenerator>();
            if (terrainGen != null)
            {
                terrainGen.ClearTerrain();
            }
            
            VillageBuilder villageBuilder = FindObjectOfType<VillageBuilder>();
            if (villageBuilder != null)
            {
                villageBuilder.ClearVillages();
            }
            
            ForestGenerator forestGen = FindObjectOfType<ForestGenerator>();
            if (forestGen != null)
            {
                forestGen.ClearForests();
            }
            
            WaterGenerator waterGen = FindObjectOfType<WaterGenerator>();
            if (waterGen != null)
            {
                waterGen.ClearWater();
            }
        }
        #endregion

        #region Seed Utilities
        /// <summary>
        /// Get a derived seed for a specific system
        /// Allows different systems to have different but deterministic seeds
        /// </summary>
        public int GetDerivedSeed(string systemName)
        {
            int hash = systemName.GetHashCode();
            return worldSeed + hash;
        }

        /// <summary>
        /// Get a seed for a specific position in the world
        /// Useful for chunk-based generation
        /// </summary>
        public int GetPositionSeed(int x, int z)
        {
            // Combine world seed with position using prime numbers
            return worldSeed + x * 73856093 + z * 19349663;
        }

        /// <summary>
        /// Check if two seeds would generate the same world
        /// </summary>
        public bool SeedsMatch(int seed1, int seed2)
        {
            return seed1 == seed2;
        }
        #endregion

        #region Save/Load
        public string ExportSeed()
        {
            return worldSeed.ToString();
        }

        public void ImportSeed(string seedString)
        {
            SetSeedFromString(seedString);
        }

        public void SaveSeedToPlayerPrefs(string key = "WorldSeed")
        {
            PlayerPrefs.SetInt(key, worldSeed);
            PlayerPrefs.Save();
            Debug.Log($"💾 Saved seed {worldSeed} to PlayerPrefs");
        }

        public void LoadSeedFromPlayerPrefs(string key = "WorldSeed")
        {
            if (PlayerPrefs.HasKey(key))
            {
                int savedSeed = PlayerPrefs.GetInt(key);
                SetSeed(savedSeed);
                Debug.Log($"📂 Loaded seed {savedSeed} from PlayerPrefs");
            }
            else
            {
                Debug.LogWarning($"⚠️ No saved seed found in PlayerPrefs with key '{key}'");
            }
        }
        #endregion

        #region Debug
        [ContextMenu("Generate Random Seed")]
        private void DebugGenerateRandomSeed()
        {
            GenerateRandomSeed();
        }

        [ContextMenu("Regenerate World")]
        private void DebugRegenerateWorld()
        {
            RegenerateWorld();
        }

        [ContextMenu("Print Seed Info")]
        private void DebugPrintSeedInfo()
        {
            Debug.Log("=== World Seed Info ===");
            Debug.Log($"Current Seed: {worldSeed}");
            Debug.Log($"Use Random: {useRandomSeed}");
            Debug.Log($"Recent Seeds: {string.Join(", ", recentSeeds)}");
        }

        [ContextMenu("Test Seed Reproducibility")]
        private void DebugTestReproducibility()
        {
            Debug.Log("Testing seed reproducibility...");
            
            int testSeed = 12345;
            Random.InitState(testSeed);
            float value1 = Random.value;
            
            Random.InitState(testSeed);
            float value2 = Random.value;
            
            Debug.Log($"Seed {testSeed}: First={value1}, Second={value2}, Match={value1 == value2}");
        }
        #endregion
    }
}
