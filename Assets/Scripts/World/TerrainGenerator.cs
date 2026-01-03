using System.Collections.Generic;
using UnityEngine;
using Hearthbound.Utilities;

namespace Hearthbound.World
{
    /// <summary>
    /// How the river path is determined
    /// </summary>
    public enum RiverPathMode
    {
        Auto,    // Automatically generate path from mountains to plains
        Manual   // Use custom path points provided by user
    }

    /// <summary>
    /// Terrain Generator - Orchestrator Pattern
    /// Coordinates specialized subsystems for terrain generation
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    public class TerrainGenerator : MonoBehaviour
    {
        #region Subsystems (Private)
        private TerrainInitializer terrainInitializer;
        private HeightmapGenerator heightmapGenerator;
        private SplatmapGenerator splatmapGenerator;
        private RiverWaterSystem riverWaterSystem;
        private WaterPlaneManager waterPlaneManager;
        private TerrainQueryService terrainQuery;
        private BiomeQueryService biomeQuery;
        #endregion

        #region Components
        private Terrain terrain;
        private TerrainData terrainData;
        private List<Vector2> lastGeneratedRiverPath;
        #endregion

        #region Configuration Fields (SerializeField)
        [Header("Terrain Preset")]
        [Tooltip("Apply a terrain style preset to set all parameters at once")]
        [SerializeField] private TerrainStylePreset currentPreset;

        [Header("Terrain Size")]
        [SerializeField] private int terrainWidth = 1000;
        [SerializeField] private int terrainLength = 1000;
        [SerializeField] private int terrainHeight = 600;
        [SerializeField] private int heightmapResolution = 513;

        [Header("Height Generation")]
        [SerializeField] private float baseHeight = 150f;
        [SerializeField] private float hillHeight = 100f;
        [SerializeField] private float mountainHeight = 300f;
        [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Noise Parameters")]
        [Tooltip("Continental mask threshold - controls where mountains appear")]
        [Range(0.2f, 0.7f)]
        [SerializeField] private float continentalThreshold = 0.3f;

        [Tooltip("Continental mask frequency - controls size of mountain regions")]
        [Range(0.0001f, 0.001f)]
        [SerializeField] private float continentalMaskFrequency = 0.0003f;

        [Tooltip("Domain warp strength for mountain ranges")]
        [Range(100f, 250f)]
        [SerializeField] private float warpStrength = 150f;

        [Tooltip("Mountain range frequency")]
        [Range(0.0005f, 0.002f)]
        [SerializeField] private float mountainFrequency = 0.0008f;

        [Tooltip("Power curve exponent for peak sharpness")]
        [Range(1.0f, 2.0f)]
        [SerializeField] private float peakSharpness = 1.3f;

        [Header("Cliff Generation (Voronoi)")]
        [Tooltip("Cliff strength - controls prominence of Voronoi cliffs")]
        [Range(0f, 1f)]
        [SerializeField] private float cliffStrength = 0.3f;

        [Tooltip("Cliff frequency - controls scale of Voronoi cells")]
        [Range(0.005f, 0.02f)]
        [SerializeField] private float cliffFrequency = 0.01f;

        [Tooltip("Cliff threshold - minimum continental mask value for cliffs to appear")]
        [Range(0.4f, 0.8f)]
        [SerializeField] private float cliffThreshold = 0.6f;

        [Header("Rivers and Lakes")]
        [Tooltip("How to generate river path: Auto or Manual")]
        [SerializeField] private RiverPathMode riverPathMode = RiverPathMode.Auto;

        [Tooltip("Disable WaterGenerator component")]
        [SerializeField] private bool disableWaterGenerator = true;

        [Tooltip("Disable water biome textures")]
        [SerializeField] private bool disableWaterBiomes = true;

        [Tooltip("Generate water GameObjects along river paths")]
        [SerializeField] private bool generateRiverWater = true;

        [Tooltip("Water material for river/lake water")]
        [SerializeField] private Material riverWaterMaterial;

        [Tooltip("Manual river path points")]
        [SerializeField] private List<Vector2> customRiverPath = new List<Vector2>();

        [Tooltip("Manual river source point")]
        [SerializeField] private Vector2 manualRiverSource;

        [Tooltip("Manual lake center point")]
        [SerializeField] private Vector2 manualLakeCenter;

        [Tooltip("River width in world units")]
        [Range(5f, 100f)]
        [SerializeField] private float riverWidth = 40f;

        [Tooltip("River depth in world units")]
        [Range(50f, 600f)]
        [SerializeField] private float riverDepth = 200f;

        [Tooltip("Lake radius in world units")]
        [Range(100f, 300f)]
        [SerializeField] private float lakeRadius = 150f;

        [Tooltip("Lake depth in world units")]
        [Range(50f, 600f)]
        [SerializeField] private float lakeDepth = 250f;

        [Header("Texture Splatting - Height Thresholds")]
        [SerializeField] private float waterHeight = 0.05f;
        [SerializeField] private float grassHeight = 0.3f;
        [SerializeField] private float rockHeight = 0.6f;
        [SerializeField] private float snowHeight = 0.7f;
        [SerializeField] private float steepSlope = 45f;

        [Header("Biome Settings")]
        [SerializeField] private float forestMoistureMin = 0.5f;
        [SerializeField] private float forestMoistureMax = 1.0f;
        [SerializeField] private float forestTemperatureMin = 0.3f;
        [SerializeField] private float forestTemperatureMax = 0.7f;

        [Header("Biome System")]
        [SerializeField] private BiomeCollection biomeCollection;
        [SerializeField] private bool useScriptableObjectBiomes = true;
        [SerializeField] private bool useAdvancedBiomes = true;
        [SerializeField] private float biomeBlendDistance = 0.1f;
        [SerializeField] private float moistureFrequency = 0.003f;
        [SerializeField] private float temperatureFrequency = 0.002f;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = false;

        [Header("Vegetation")]
        [Tooltip("ForestGenerator component to place trees/bushes/rocks")]
        [SerializeField] private ForestGenerator forestGenerator;

        [Tooltip("Generate forests and vegetation automatically")]
        [SerializeField] private bool generateVegetation = true;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            terrain = GetComponent<Terrain>();
            InitializeSubsystems();
        }

        private void Start()
        {
            if (generateOnStart)
            {
                WorldSeedManager seedManager = FindObjectOfType<WorldSeedManager>();
                int seed = seedManager != null ? seedManager.CurrentSeed : 12345;
                GenerateTerrain(seed);
            }
        }
        #endregion

        #region Subsystem Initialization
        private void InitializeSubsystems()
        {
            // Initialize terrain initializer
            terrainInitializer = new TerrainInitializer(this);
            ConfigureTerrainInitializer();
            terrainInitializer.InitializeTerrainData();

            // Get terrain references
            terrain = terrainInitializer.Terrain;
            terrainData = terrainInitializer.TerrainData;

            // Initialize subsystems
            heightmapGenerator = new HeightmapGenerator();
            ConfigureHeightmapGenerator();

            splatmapGenerator = new SplatmapGenerator();
            ConfigureSplatmapGenerator();

            riverWaterSystem = new RiverWaterSystem(transform);
            ConfigureRiverWaterSystem();

            waterPlaneManager = new WaterPlaneManager(this);
            ConfigureWaterPlaneManager();

            terrainQuery = new TerrainQueryService(terrain, terrainData);

            biomeQuery = new BiomeQueryService(terrainQuery, biomeCollection);
            ConfigureBiomeQueryService();
        }

        private void ConfigureTerrainInitializer()
        {
            terrainInitializer.TerrainWidth = terrainWidth;
            terrainInitializer.TerrainLength = terrainLength;
            terrainInitializer.TerrainHeight = terrainHeight;
            terrainInitializer.HeightmapResolution = heightmapResolution;
        }

        private void ConfigureHeightmapGenerator()
        {
            heightmapGenerator.BaseHeight = baseHeight;
            heightmapGenerator.HillHeight = hillHeight;
            heightmapGenerator.MountainHeight = mountainHeight;
            heightmapGenerator.HeightCurve = heightCurve;
            heightmapGenerator.ContinentalThreshold = continentalThreshold;
            heightmapGenerator.ContinentalMaskFrequency = continentalMaskFrequency;
            heightmapGenerator.WarpStrength = warpStrength;
            heightmapGenerator.MountainFrequency = mountainFrequency;
            heightmapGenerator.PeakSharpness = peakSharpness;
            heightmapGenerator.CliffStrength = cliffStrength;
            heightmapGenerator.CliffFrequency = cliffFrequency;
            heightmapGenerator.CliffThreshold = cliffThreshold;
            heightmapGenerator.RiverWidth = riverWidth;
            heightmapGenerator.RiverDepth = riverDepth;
            heightmapGenerator.LakeRadius = lakeRadius;
            heightmapGenerator.LakeDepth = lakeDepth;
            heightmapGenerator.TerrainWidth = terrainWidth;
            heightmapGenerator.TerrainLength = terrainLength;
            heightmapGenerator.TerrainHeight = terrainHeight;
            heightmapGenerator.HeightmapResolution = heightmapResolution;
        }

        private void ConfigureSplatmapGenerator()
        {
            splatmapGenerator.WaterHeight = waterHeight;
            splatmapGenerator.GrassHeight = grassHeight;
            splatmapGenerator.RockHeight = rockHeight;
            splatmapGenerator.SnowHeight = snowHeight;
            splatmapGenerator.SteepSlope = steepSlope;
            splatmapGenerator.BiomeCollection = biomeCollection;
            splatmapGenerator.UseScriptableObjectBiomes = useScriptableObjectBiomes;
            splatmapGenerator.UseAdvancedBiomes = useAdvancedBiomes;
            splatmapGenerator.BiomeBlendDistance = biomeBlendDistance;
            splatmapGenerator.MoistureFrequency = moistureFrequency;
            splatmapGenerator.TemperatureFrequency = temperatureFrequency;
            splatmapGenerator.ForestMoistureMin = forestMoistureMin;
            splatmapGenerator.ForestMoistureMax = forestMoistureMax;
            splatmapGenerator.ForestTemperatureMin = forestTemperatureMin;
            splatmapGenerator.ForestTemperatureMax = forestTemperatureMax;
            splatmapGenerator.DisableWaterBiomes = disableWaterBiomes;
            splatmapGenerator.TerrainWidth = terrainWidth;
            splatmapGenerator.TerrainLength = terrainLength;
            splatmapGenerator.TerrainHeight = terrainHeight;
        }

        private void ConfigureRiverWaterSystem()
        {
            riverWaterSystem.GenerateWater = generateRiverWater;
            riverWaterSystem.WaterMaterial = riverWaterMaterial;
            riverWaterSystem.RiverWidth = riverWidth;
            riverWaterSystem.LakeRadius = lakeRadius;
        }

        private void ConfigureWaterPlaneManager()
        {
            waterPlaneManager.DisableWaterGenerator = disableWaterGenerator;
            waterPlaneManager.DisableWaterBiomes = disableWaterBiomes;
        }

        private void ConfigureBiomeQueryService()
        {
            biomeQuery.WaterHeight = waterHeight;
            biomeQuery.GrassHeight = grassHeight;
            biomeQuery.RockHeight = rockHeight;
            biomeQuery.SnowHeight = snowHeight;
            biomeQuery.SteepSlope = steepSlope;
            biomeQuery.ForestMoistureMin = forestMoistureMin;
            biomeQuery.ForestMoistureMax = forestMoistureMax;
            biomeQuery.ForestTemperatureMin = forestTemperatureMin;
            biomeQuery.ForestTemperatureMax = forestTemperatureMax;
            biomeQuery.MoistureFrequency = moistureFrequency;
            biomeQuery.TemperatureFrequency = temperatureFrequency;
            biomeQuery.TerrainHeight = terrainHeight;
            biomeQuery.UseScriptableObjectBiomes = useScriptableObjectBiomes;
        }
        #endregion

        #region Main Generation
        public void GenerateTerrain(int seed)
        {
            // Ensure subsystems are initialized (important for Edit mode)
            if (heightmapGenerator == null)
            {
                InitializeSubsystems();
            }

            // Ensure terrain is initialized
            if (terrain == null || terrainData == null)
            {
                terrainInitializer.InitializeTerrainData();
                terrain = terrainInitializer.Terrain;
                terrainData = terrainInitializer.TerrainData;
            }

            Debug.Log($"Generating terrain with seed: {seed}");

            Random.InitState(seed);

            // Handle WaterGenerator
            waterPlaneManager.HandleWaterGenerator();

            // Clear existing river water
            riverWaterSystem.ClearRiverWater();

            // Determine river path based on mode
            List<Vector2> riverPath = DetermineRiverPath(seed);
            lastGeneratedRiverPath = riverPath;

            // Generate heightmap
            heightmapGenerator.GenerateHeightmap(seed, terrainData, riverPath);

            // Apply texture splatmap
            splatmapGenerator.GenerateSplatmap(seed, terrainData);

            // Generate water along river paths if enabled
            if (generateRiverWater && riverPath != null && riverPath.Count > 0)
            {
                riverWaterSystem.GenerateRiverWater(riverPath, terrain, terrainData);
            }

            terrain.Flush();

            // Generate vegetation (forests, bushes, rocks)
            if (generateVegetation && forestGenerator != null)
            {
                Debug.Log("Generating vegetation...");
                forestGenerator.GenerateForests(seed);
            }
            else if (generateVegetation && forestGenerator == null)
            {
                Debug.LogWarning("⚠️ Vegetation generation enabled but ForestGenerator not assigned!");
            }

            Debug.Log("Terrain generation complete!");
        }

        private List<Vector2> DetermineRiverPath(int seed)
        {
            if (riverPathMode == RiverPathMode.Manual)
            {
                if (customRiverPath != null && customRiverPath.Count > 0)
                {
                    Debug.Log($"🌊 Using manual river path with {customRiverPath.Count} points");
                    return new List<Vector2>(customRiverPath);
                }
                else if (manualRiverSource != Vector2.zero && manualLakeCenter != Vector2.zero)
                {
                    Debug.Log($"🌊 Generating river path from manual points");
                    var path = RiverPathGenerator.GenerateMeanderingPath(manualRiverSource, manualLakeCenter, seed);
                    if (path != null && path.Count > 0)
                    {
                        Debug.Log($"✅ Generated river path with {path.Count} points");
                        return path;
                    }
                    Debug.LogError("❌ River path generation failed!");
                    return new List<Vector2>();
                }
                else
                {
                    Debug.LogWarning("⚠️ Manual mode selected but points not set. Falling back to Auto mode.");
                }
            }

            // Auto mode or fallback
            System.Func<float, float, float> getContinentalMaskFunc = (x, z) =>
                NoiseGenerator.GetContinentalMask(x, z, seed, continentalMaskFrequency);

            var autoPath = RiverPathGenerator.GenerateRiverPath(seed, terrainWidth, terrainLength, getContinentalMaskFunc);
            Debug.Log($"🌊 Auto-generated river path with {autoPath.Count} points");
            return autoPath;
        }

        public void ClearTerrain()
        {
            if (terrainData == null)
                return;

            int width = terrainData.heightmapResolution;
            int height = terrainData.heightmapResolution;
            float[,] heights = new float[width, height];

            terrainData.SetHeights(0, 0, heights);
            Debug.Log("Terrain cleared to flat");
        }
        #endregion

        #region Query Methods (Delegate to Services)
        public float GetHeightAtPosition(Vector3 worldPosition)
        {
            return terrainQuery.GetHeightAtPosition(worldPosition);
        }

        public Vector3 GetNormalAtPosition(Vector3 worldPosition)
        {
            return terrainQuery.GetNormalAtPosition(worldPosition);
        }

        public float GetSlopeAtPosition(Vector3 worldPosition)
        {
            return terrainQuery.GetSlopeAtPosition(worldPosition);
        }

        public bool IsValidPlacementPosition(Vector3 worldPosition, float maxSlope = 45f)
        {
            return terrainQuery.IsValidPlacementPosition(worldPosition, maxSlope);
        }

        public string GetBiomeAtPosition(Vector3 worldPosition, int seed)
        {
            return biomeQuery.GetBiomeAtPosition(worldPosition, seed);
        }

        public BiomeInfo GetBiomeInfoAtPosition(Vector3 worldPosition, int seed)
        {
            return biomeQuery.GetBiomeInfoAtPosition(worldPosition, seed);
        }

        public BiomeData GetBiomeDataAtPosition(Vector3 worldPosition, int seed)
        {
            return biomeQuery.GetBiomeDataAtPosition(worldPosition, seed);
        }
        #endregion

        #region Setter Methods (For Preset System)
        public void SetBaseHeight(float value)
        {
            baseHeight = value;
            if (heightmapGenerator != null)
                heightmapGenerator.BaseHeight = value;
        }

        public void SetHillHeight(float value)
        {
            hillHeight = value;
            if (heightmapGenerator != null)
                heightmapGenerator.HillHeight = value;
        }

        public void SetMountainHeight(float value)
        {
            mountainHeight = value;
            if (heightmapGenerator != null)
                heightmapGenerator.MountainHeight = value;
        }

        public void SetHeightCurve(AnimationCurve value)
        {
            heightCurve = value;
            if (heightmapGenerator != null)
                heightmapGenerator.HeightCurve = value;
        }

        public void SetContinentalThreshold(float value)
        {
            continentalThreshold = value;
            if (heightmapGenerator != null)
                heightmapGenerator.ContinentalThreshold = value;
        }

        public void SetContinentalMaskFrequency(float value)
        {
            continentalMaskFrequency = value;
            if (heightmapGenerator != null)
                heightmapGenerator.ContinentalMaskFrequency = value;
        }

        public void SetWarpStrength(float value)
        {
            warpStrength = value;
            if (heightmapGenerator != null)
                heightmapGenerator.WarpStrength = value;
        }

        public void SetMountainFrequency(float value)
        {
            mountainFrequency = value;
            if (heightmapGenerator != null)
                heightmapGenerator.MountainFrequency = value;
        }

        public void SetPeakSharpness(float value)
        {
            peakSharpness = value;
            if (heightmapGenerator != null)
                heightmapGenerator.PeakSharpness = value;
        }

        public void SetCliffStrength(float value)
        {
            cliffStrength = value;
            if (heightmapGenerator != null)
                heightmapGenerator.CliffStrength = value;
        }

        public void SetCliffFrequency(float value)
        {
            cliffFrequency = value;
            if (heightmapGenerator != null)
                heightmapGenerator.CliffFrequency = value;
        }

        public void SetCliffThreshold(float value)
        {
            cliffThreshold = value;
            if (heightmapGenerator != null)
                heightmapGenerator.CliffThreshold = value;
        }

        public void SetTerrainSize(float width, float length, float height)
        {
            terrainWidth = (int)width;
            terrainLength = (int)length;
            terrainHeight = (int)height;

            if (terrainData != null)
            {
                terrainData.size = new Vector3(width, height, length);
            }

            if (terrainInitializer != null)
            {
                terrainInitializer.TerrainWidth = terrainWidth;
                terrainInitializer.TerrainLength = terrainLength;
                terrainInitializer.TerrainHeight = terrainHeight;
            }

            if (heightmapGenerator != null)
            {
                heightmapGenerator.TerrainWidth = terrainWidth;
                heightmapGenerator.TerrainLength = terrainLength;
                heightmapGenerator.TerrainHeight = terrainHeight;
            }

            if (splatmapGenerator != null)
            {
                splatmapGenerator.TerrainWidth = terrainWidth;
                splatmapGenerator.TerrainLength = terrainLength;
                splatmapGenerator.TerrainHeight = terrainHeight;
            }

            if (biomeQuery != null)
            {
                biomeQuery.TerrainHeight = terrainHeight;
            }
        }

        public void SetHeightmapResolution(int resolution)
        {
            heightmapResolution = resolution;
            if (terrainData != null)
            {
                terrainData.heightmapResolution = resolution;
            }
            if (terrainInitializer != null)
            {
                terrainInitializer.HeightmapResolution = resolution;
            }
            if (heightmapGenerator != null)
            {
                heightmapGenerator.HeightmapResolution = resolution;
            }
        }

        public void SetWaterHeight(float value)
        {
            waterHeight = value;
            if (splatmapGenerator != null)
                splatmapGenerator.WaterHeight = value;
            if (biomeQuery != null)
                biomeQuery.WaterHeight = value;
        }

        public void SetGrassHeight(float value)
        {
            grassHeight = value;
            if (splatmapGenerator != null)
                splatmapGenerator.GrassHeight = value;
            if (biomeQuery != null)
                biomeQuery.GrassHeight = value;
        }

        public void SetRockHeight(float value)
        {
            rockHeight = value;
            if (splatmapGenerator != null)
                splatmapGenerator.RockHeight = value;
            if (biomeQuery != null)
                biomeQuery.RockHeight = value;
        }

        public void SetSnowHeight(float value)
        {
            snowHeight = value;
            if (splatmapGenerator != null)
                splatmapGenerator.SnowHeight = value;
            if (biomeQuery != null)
                biomeQuery.SnowHeight = value;
        }
        #endregion

        #region Water Plane Management (Public API)
        public void ClearAllWaterPlanes()
        {
            waterPlaneManager?.ClearAllWaterPlanes();
        }
        #endregion
    }

    #region Biome Data Structures
    [System.Serializable]
    public struct BiomeInfo
    {
        public float height;
        public float slope;
        public float moisture;
        public float temperature;
        public string biomeName;
    }
    #endregion
}
