using System.Collections.Generic;
using UnityEngine;
using Hearthbound.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    /// Terrain Generator
    /// Generates procedural terrain using seed-based noise
    /// Creates mountains, valleys, plains, and applies textures
    /// </summary>
    [RequireComponent(typeof(Terrain))]
    public class TerrainGenerator : MonoBehaviour
    {
        #region Components
        private Terrain terrain;
        private TerrainData terrainData;
        private List<Vector2> lastGeneratedRiverPath; // Store river path for water generation
        private List<GameObject> riverWaterObjects = new List<GameObject>(); // Store generated water objects
        #endregion

        #region Terrain Settings
        [Header("Terrain Size")]
        [SerializeField] private int terrainWidth = 1000;
        [SerializeField] private int terrainLength = 1000;
        [SerializeField] private int terrainHeight = 600;
        [SerializeField] private int heightmapResolution = 513;
        
        [Header("Height Generation")]
        [SerializeField] private float baseHeight = 150f; // Base terrain height (plains/lowlands) - increased significantly to prevent flat terrain
        [SerializeField] private float hillHeight = 100f; // Gentle hills for variation - increased for more visible hills
        [SerializeField] private float mountainHeight = 300f; // Clustered mountains - increased for more prominent mountains
        [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Noise Parameters")]
        [Tooltip("Continental mask threshold - controls where mountains appear (0.3 recommended for better terrain distribution)")]
        [Range(0.2f, 0.7f)]
        [SerializeField] private float continentalThreshold = 0.3f;
        
        [Tooltip("Continental mask frequency - controls size of mountain regions (0.0002-0.0005 recommended, lower = larger regions)")]
        [Range(0.0001f, 0.001f)]
        [SerializeField] private float continentalMaskFrequency = 0.0003f;
        
        [Tooltip("Domain warp strength for mountain ranges (100-200 recommended)")]
        [Range(100f, 250f)]
        [SerializeField] private float warpStrength = 150f;
        
        [Tooltip("Mountain range frequency (0.0005-0.001 recommended)")]
        [Range(0.0005f, 0.002f)]
        [SerializeField] private float mountainFrequency = 0.0008f;
        
        [Tooltip("Power curve exponent for peak sharpness (1.0-1.5 recommended)")]
        [Range(1.0f, 2.0f)]
        [SerializeField] private float peakSharpness = 1.3f;
        
        [Header("Rivers and Lakes")]
        [Tooltip("How to generate river path: Auto = automatic from mountains to plains, Manual = use custom points below")]
        [SerializeField] private RiverPathMode riverPathMode = RiverPathMode.Auto;
        
        [Tooltip("Disable WaterGenerator component when using river system (prevents flat water plane from covering carved rivers)")]
        [SerializeField] private bool disableWaterGenerator = true;
        
        [Tooltip("Disable water biome textures when using river system (prevents blue water texture from being painted on terrain based on height)")]
        [SerializeField] private bool disableWaterBiomes = true;
        
        [Tooltip("Generate water GameObjects along river paths and in lakes (creates visible water in carved areas)")]
        [SerializeField] private bool generateRiverWater = true;
        
        [Tooltip("Water material to use for river/lake water (if null, creates default blue material)")]
        [SerializeField] private Material riverWaterMaterial;
        
        [Tooltip("Manual river path points (in world coordinates). Only used if River Path Mode is set to Manual.")]
        [SerializeField] private List<Vector2> customRiverPath = new List<Vector2>();
        
        [Tooltip("Manual river source point (for Manual mode). If empty, will use first point in customRiverPath")]
        [SerializeField] private Vector2 manualRiverSource;
        
        [Tooltip("Manual lake center point (for Manual mode). If empty, will use last point in customRiverPath")]
        [SerializeField] private Vector2 manualLakeCenter;
        
        [Tooltip("River width in world units")]
        [Range(5f, 100f)]
        [SerializeField] private float riverWidth = 40f;
        
        [Tooltip("How deep rivers carve into terrain (in world units). Higher values create deeper valleys.")]
        [Range(50f, 600f)]
        [SerializeField] private float riverDepth = 200f;
        
        [Tooltip("Lake radius in world units")]
        [Range(100f, 300f)]
        [SerializeField] private float lakeRadius = 150f;
        
        [Tooltip("How deep lake basins are (in world units). Higher values create deeper lake beds.")]
        [Range(50f, 600f)]
        [SerializeField] private float lakeDepth = 250f;
        
        [Header("Texture Splatting - Height Thresholds")]
        [SerializeField] private float waterHeight = 0.05f; // Below this = water/beach (lowered from 0.1 for less water coverage)
        [SerializeField] private float grassHeight = 0.3f; // Plains/grass biome
        [SerializeField] private float rockHeight = 0.6f; // Mountains start
        [SerializeField] private float snowHeight = 0.7f; // Snow on peaks (lowered from 0.8 for more snow coverage)
        [SerializeField] private float steepSlope = 45f;
        
        [Header("Biome Settings")]
        [SerializeField] private float forestMoistureMin = 0.5f; // Forests need high moisture
        [SerializeField] private float forestMoistureMax = 1.0f;
        [SerializeField] private float forestTemperatureMin = 0.3f; // Forests prefer moderate temps
        [SerializeField] private float forestTemperatureMax = 0.7f;

        [Header("Biome System")]
        [SerializeField] private BiomeCollection biomeCollection;
        [SerializeField] private bool useScriptableObjectBiomes = true; // Use BiomeCollection, otherwise use legacy system
        [SerializeField] private bool useAdvancedBiomes = true; // Legacy: Use advanced biome system
        [SerializeField] private float biomeBlendDistance = 0.1f; // Legacy: How smoothly biomes blend
        [SerializeField] private float moistureFrequency = 0.003f;
        [SerializeField] private float temperatureFrequency = 0.002f;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart = false;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            terrain = GetComponent<Terrain>();
            InitializeTerrainData();
        }

        private void Start()
        {
            if (generateOnStart && WorldSeedManager.Instance != null)
            {
                // Check if terrain already has data before regenerating
                if (terrain != null && terrain.terrainData != null)
                {
                    // Sample terrain to see if it has actual data
                    int width = terrain.terrainData.heightmapResolution;
                    int height = terrain.terrainData.heightmapResolution;
                    
                    if (width > 0 && height > 0)
                    {
                        // Check a few sample points
                        bool hasData = false;
                        for (int i = 0; i < 10 && !hasData; i++)
                        {
                            int x = (int)(width * 0.1f * (i + 1));
                            int z = (int)(height * 0.1f * (i + 1));
                            if (x >= width) x = width - 1;
                            if (z >= height) z = height - 1;
                            
                            float sampleHeight = terrain.terrainData.GetHeight(x, z);
                            if (sampleHeight > 0.1f) // Has some height
                            {
                                hasData = true;
                            }
                        }
                        
                        if (hasData)
                        {
                            Debug.Log("TerrainGenerator: Terrain already has data - skipping generation (uncheck 'Generate On Start' to prevent this check)");
                            return;
                        }
                    }
                }
                
                GenerateTerrain(WorldSeedManager.Instance.CurrentSeed);
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Ensure terrain component is initialized (called before using terrain)
        /// </summary>
        private void EnsureTerrainInitialized()
        {
            if (terrain == null)
            {
                terrain = GetComponent<Terrain>();
                if (terrain == null)
                {
                    Debug.LogError("TerrainGenerator requires a Terrain component!");
                    return;
                }
            }
        }

        private void InitializeTerrainData()
        {
            // Ensure terrain component is initialized
            EnsureTerrainInitialized();
            
            if (terrain == null)
            {
                Debug.LogError("Cannot initialize terrain data: Terrain component is missing!");
                return;
            }

            if (terrain.terrainData == null)
            {
                terrainData = new TerrainData();
                terrain.terrainData = terrainData;
                // Only set size for new terrain
                terrainData.heightmapResolution = heightmapResolution;
                terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
                Debug.Log($"Terrain initialized (new): {terrainWidth}x{terrainLength}, Height: {terrainHeight}");
            }
            else
            {
                terrainData = terrain.terrainData;
                // Preserve existing terrain size to prevent flattening
                Vector3 existingSize = terrainData.size;
                int existingResolution = terrainData.heightmapResolution;
                
                // Only update if significantly different (to allow manual adjustments)
                bool sizeChanged = Mathf.Abs(existingSize.x - terrainWidth) > 1f || 
                                  Mathf.Abs(existingSize.z - terrainLength) > 1f;
                bool resolutionChanged = existingResolution != heightmapResolution;
                
                if (sizeChanged || resolutionChanged)
                {
                    Debug.Log($"Terrain size/resolution changed - updating from {existingSize} (res: {existingResolution}) to {terrainWidth}x{terrainLength} (res: {heightmapResolution})");
                    terrainData.heightmapResolution = heightmapResolution;
                    // IMPORTANT: Preserve Y (height) to prevent flattening mountains!
                    terrainData.size = new Vector3(terrainWidth, existingSize.y, terrainLength);
                }
                else
                {
                    // Preserve everything - don't modify existing terrain
                    Debug.Log($"Terrain already initialized - preserving size: {existingSize} (res: {existingResolution})");
                }
            }
            
            // Ensure TerrainCollider uses the same TerrainData
            TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
            if (terrainCollider != null && terrainCollider.terrainData != terrainData)
            {
                terrainCollider.terrainData = terrainData;
                Debug.Log("  Synced TerrainCollider with TerrainData");
            }
        }
        #endregion

        #region Terrain Generation
        public void GenerateTerrain(int seed)
        {
            // Ensure terrain is initialized
            if (terrain == null || terrainData == null)
            {
                InitializeTerrainData();
            }

            Debug.Log($"Generating terrain with seed: {seed}");
            
            Random.InitState(seed);
            
            // Handle WaterGenerator based on river system settings
            HandleWaterGenerator();
            
            // Clear any existing river water
            ClearRiverWater();
            
            // Generate heightmap
            GenerateHeightmap(seed);
            
            // Apply texture splatmap
            GenerateSplatmap(seed);
            
            // Add detail layers (grass, rocks)
            GenerateDetailLayers(seed);
            
            // Generate water along river paths if enabled
            if (generateRiverWater && lastGeneratedRiverPath != null && lastGeneratedRiverPath.Count > 0)
            {
                GenerateRiverWater();
            }
            
            Debug.Log("Terrain generation complete!");
        }
        
        /// <summary>
        /// Enable/disable WaterGenerator based on river system settings
        /// </summary>
        private void HandleWaterGenerator()
        {
            if (!disableWaterGenerator)
                return;
            
            // First, find and destroy any existing water planes in the scene
            ClearAllWaterPlanes();
            
            // Find WaterGenerator component (could be on this GameObject or any parent/child)
            WaterGenerator waterGen = GetComponent<WaterGenerator>();
            if (waterGen == null)
            {
                // Also check parent/children
                waterGen = GetComponentInParent<WaterGenerator>();
                if (waterGen == null)
                {
                    waterGen = GetComponentInChildren<WaterGenerator>();
                }
            }
            
            // Also check in the scene (in case it's on a sibling GameObject)
            if (waterGen == null)
            {
                waterGen = FindObjectOfType<WaterGenerator>();
            }
            
            if (waterGen != null)
            {
                waterGen.enabled = false;
                // Also clear any existing water
                waterGen.ClearWater();
                Debug.Log("🌊 WaterGenerator disabled (river system is handling water)");
            }
            else
            {
                // WaterGenerator not found - that's okay, just log it
                Debug.Log("ℹ️ No WaterGenerator found to disable");
            }
        }
        
        /// <summary>
        /// Clear all water plane GameObjects from the scene
        /// </summary>
        public void ClearAllWaterPlanes()
        {
            List<GameObject> waterObjects = new List<GameObject>();
            WaterGenerator[] waterGens = new WaterGenerator[0];
            
            #if UNITY_EDITOR
            // Method 1: Search ALL GameObjects in the scene (including inactive)
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            
            foreach (GameObject obj in allObjects)
            {
                // Skip prefabs (assets, not scene instances)
                if (PrefabUtility.IsPartOfPrefabAsset(obj))
                    continue;
                
                // Skip if not in a scene
                if (obj.scene.name == null)
                    continue;
                
                // Check by name
                if (obj != null && (obj.name == "Water" || obj.name.StartsWith("Water")))
                {
                    if (!waterObjects.Contains(obj))
                    {
                        waterObjects.Add(obj);
                        Debug.Log($"Found water object by name: {obj.name} at path: {GetGameObjectPath(obj)}");
                    }
                }
            }
            
            // Method 2: Find all WaterGenerator components (including inactive)
            waterGens = FindObjectsOfType<WaterGenerator>(true);
            foreach (WaterGenerator waterGen in waterGens)
            {
                if (waterGen != null)
                {
                    Debug.Log($"Found WaterGenerator component, calling ClearWater()");
                    waterGen.ClearWater();
                    
                    // Also check children of WaterGenerator for water objects
                    Transform waterGenTransform = waterGen.transform;
                    for (int i = waterGenTransform.childCount - 1; i >= 0; i--)
                    {
                        Transform child = waterGenTransform.GetChild(i);
                        if (child != null && child.gameObject != null)
                        {
                            if (child.name == "Water" || child.name.StartsWith("Water"))
                            {
                                if (!waterObjects.Contains(child.gameObject))
                                {
                                    waterObjects.Add(child.gameObject);
                                    Debug.Log($"Found water object as child of WaterGenerator: {GetGameObjectPath(child.gameObject)}");
                                }
                            }
                        }
                    }
                }
            }
            
            // Method 3: Find all Plane meshes that look like water (more aggressive search)
            MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>(true);
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null)
                    continue;
                
                string meshName = mf.sharedMesh.name;
                if (meshName.Contains("Plane") || meshName == "Quad" || meshName.StartsWith("Plane"))
                {
                    MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        bool isWaterLike = false;
                        
                        // Check material
                        if (mr.sharedMaterial != null)
                        {
                            Material mat = mr.sharedMaterial;
                            Color matColor = mat.color;
                            
                            // Blue materials (lower threshold to catch more)
                            if (matColor.b > 0.3f && matColor.b > matColor.r && matColor.b > matColor.g)
                                isWaterLike = true;
                            // Material name contains water or blue
                            string matNameLower = mat.name.ToLower();
                            if (matNameLower.Contains("water") || matNameLower.Contains("blue") || matNameLower.Contains("aqua"))
                                isWaterLike = true;
                        }
                        
                        // Large scale (water planes are scaled large - 100x100+ for 1000x1000 terrain)
                        Vector3 scale = mf.transform.lossyScale;
                        if (scale.x > 30f || scale.z > 30f)
                        {
                            isWaterLike = true;
                            Debug.Log($"Found large plane (potential water): {mf.gameObject.name} at {GetGameObjectPath(mf.gameObject)}, scale: {scale.x:F1}x{scale.z:F1}");
                        }
                        
                        // Check if it's at a low Y position (sea level)
                        float yPos = mf.transform.position.y;
                        if (yPos < 100f) // Water is usually at low elevation
                            isWaterLike = true;
                        
                        // If GameObject name suggests water
                        if (mf.gameObject.name.ToLower().Contains("water"))
                            isWaterLike = true;
                        
                        if (isWaterLike && !waterObjects.Contains(mf.gameObject))
                        {
                            waterObjects.Add(mf.gameObject);
                            Debug.Log($"Found water plane by mesh/material: {mf.gameObject.name} at path: {GetGameObjectPath(mf.gameObject)}");
                        }
                    }
                }
            }
            #else
            // Runtime fallback
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj != null && (obj.name == "Water" || obj.name.StartsWith("Water")))
                {
                    if (!waterObjects.Contains(obj))
                        waterObjects.Add(obj);
                }
            }
            waterGens = FindObjectsOfType<WaterGenerator>(true);
            foreach (WaterGenerator waterGen in waterGens)
            {
                if (waterGen != null)
                    waterGen.ClearWater();
            }
            #endif
            
            // Destroy all found water objects
            int destroyedCount = 0;
            foreach (GameObject waterObj in waterObjects)
            {
                if (waterObj != null)
                {
                    Debug.Log($"🗑️ Destroying water object: {GetGameObjectPath(waterObj)}");
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(waterObj);
                        destroyedCount++;
                    }
                    else
                    #endif
                    {
                        Destroy(waterObj);
                        destroyedCount++;
                    }
                }
            }
            
            if (destroyedCount > 0 || waterGens.Length > 0)
            {
                Debug.Log($"🗑️ Successfully removed {destroyedCount} water plane(s) from scene");
            }
            else
            {
                Debug.LogWarning("⚠️ No water planes found to remove. Check Console for search details.");
            }
        }
        
        /// <summary>
        /// Get full hierarchy path for a GameObject (for debugging)
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null)
                return "null";
            
            string path = obj.name;
            Transform current = obj.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private void GenerateHeightmap(int seed)
        {
            Debug.Log("  Generating heightmap...");
            Debug.Log($"  Water Carving Params: riverWidth={riverWidth:F1}, riverDepth={riverDepth:F1}, lakeRadius={lakeRadius:F1}, lakeDepth={lakeDepth:F1}");
            
            int width = terrainData.heightmapResolution;
            int height = terrainData.heightmapResolution;
            float[,] heights = new float[width, height];

            // Calculate world space size per heightmap pixel
            float scaleX = terrainWidth / (float)width;
            float scaleZ = terrainLength / (float)height;

            // Generate or use custom river path
            List<Vector2> riverPath = null;
            
            if (riverPathMode == RiverPathMode.Manual)
            {
                if (customRiverPath != null && customRiverPath.Count > 0)
                {
                    // Use manually specified path points
                    riverPath = new List<Vector2>(customRiverPath);
                    Debug.Log($"🌊 Using manual river path with {riverPath.Count} points");
                }
                else if (manualRiverSource != Vector2.zero && manualLakeCenter != Vector2.zero)
                {
                    // Generate path between manual source and lake center
                    Debug.Log($"🌊 Generating river path from manual points: Source=({manualRiverSource.x:F1},{manualRiverSource.y:F1}), Lake=({manualLakeCenter.x:F1},{manualLakeCenter.y:F1})");
                    riverPath = RiverPathGenerator.GenerateMeanderingPath(manualRiverSource, manualLakeCenter, seed);
                    Debug.Log($"✅ Generated river path with {riverPath.Count} points from ({manualRiverSource.x:F1},{manualRiverSource.y:F1}) to ({manualLakeCenter.x:F1},{manualLakeCenter.y:F1})");
                    if (riverPath == null || riverPath.Count == 0)
                    {
                        Debug.LogError("❌ River path generation returned empty/null path!");
                    }
                    else
                    {
                        Debug.Log($"   First point: ({riverPath[0].x:F1}, {riverPath[0].y:F1}), Last point: ({riverPath[riverPath.Count - 1].x:F1}, {riverPath[riverPath.Count - 1].y:F1})");
                    }
                }
                else
                {
                    // Manual mode selected but points not set - fall back to auto mode
                    float sourceX = manualRiverSource.x;
                    float sourceY = manualRiverSource.y;
                    float lakeX = manualLakeCenter.x;
                    float lakeY = manualLakeCenter.y;
                    Debug.LogWarning($"⚠️ Manual mode selected but points not set. Source=({sourceX:F1},{sourceY:F1}), Lake=({lakeX:F1},{lakeY:F1}). Falling back to Auto mode.");
                    
                    System.Func<float, float, float> getContinentalMaskFunc = (x, z) => 
                        NoiseGenerator.GetContinentalMask(x, z, seed, continentalMaskFrequency);

                    riverPath = RiverPathGenerator.GenerateRiverPath(
                        seed, 
                        terrainWidth, 
                        terrainLength, 
                        getContinentalMaskFunc
                    );
                    Debug.Log($"🌊 Auto-generated river path (fallback) with {riverPath.Count} points");
                }
            }
            else // Auto mode
            {
                // Auto mode: automatically generate path from mountains to plains
                System.Func<float, float, float> getContinentalMaskFunc = (x, z) => 
                    NoiseGenerator.GetContinentalMask(x, z, seed, continentalMaskFrequency);

                riverPath = RiverPathGenerator.GenerateRiverPath(
                    seed, 
                    terrainWidth, 
                    terrainLength, 
                    getContinentalMaskFunc
                );

                Debug.Log($"🌊 Auto-generated river path with {riverPath.Count} points");
            }

            // Ensure riverPath is never null (fallback to empty list)
            if (riverPath == null)
            {
                Debug.LogError("❌ River path is null! Creating empty path.");
                riverPath = new List<Vector2>();
            }
            
            // Store river path for water generation
            lastGeneratedRiverPath = riverPath;

            // Calculate theoretical max possible height (before water carving)
            // Updated to match the new height formula in GetTerrainHeight
            // Formula: baseHeight (full) + hillHeight (full) + mountainHeight (1.5x)
            float theoreticalMaxHeight = (baseHeight + hillHeight + mountainHeight * 1.5f) * 0.65f;
            
            // Account for maximum possible water carving in maxPossibleHeight calculation
            // This ensures normalization doesn't change when river/lake depth changes
            float maxWaterCarving = Mathf.Max(riverDepth, lakeDepth);
            float maxPossibleHeight = theoreticalMaxHeight; // Use theoretical max, carving happens after
            
            // Debug: Log sample values to verify heights are correct
            bool loggedSample = false;
            
            // Track actual min/max for debugging
            float actualMinHeight = float.MaxValue;
            float actualMaxHeight = float.MinValue;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Convert to world space coordinates
                    float worldX = x * scaleX;
                    float worldZ = z * scaleZ;

                    // Generate height using noise - returns RAW height values (0 to maxPossibleHeight)
                    float heightValue = NoiseGenerator.GetTerrainHeight(
                        worldX, worldZ, seed,
                        baseHeight, hillHeight, mountainHeight,
                        continentalThreshold, warpStrength, mountainFrequency, peakSharpness, continentalMaskFrequency,
                        riverPath,
                        riverWidth, riverDepth, lakeRadius, lakeDepth
                    );
                    
                    // Track actual min/max
                    actualMinHeight = Mathf.Min(actualMinHeight, heightValue);
                    actualMaxHeight = Mathf.Max(actualMaxHeight, heightValue);

                    // Normalize to 0-1 range using theoretical max height (before carving)
                    // This ensures mountains don't change height when river depth changes
                    // IMPORTANT: Only normalize ONCE here, GetTerrainHeight returns raw values
                    float normalizedHeight = 0f;
                    if (maxPossibleHeight > 0f)
                    {
                        normalizedHeight = heightValue / maxPossibleHeight;
                        // NOTE: Power curve removed - it was reducing already-low heights
                        // For values < 1.0, raising to power > 1.0 makes them smaller
                        // Example: 0.117^1.45 = 0.044 (makes terrain flatter, not sharper)
                        // If you want sharper peaks, increase raw heights instead
                        // normalizedHeight = Mathf.Pow(normalizedHeight, peakSharpness);
                    }
                    
                    // Debug logging for first few samples
                    if (!loggedSample && (x == width / 4 && z == height / 4))
                    {
                        // Get continental mask for this point to debug
                        float sampleWorldX = (width / 4) * scaleX;
                        float sampleWorldZ = (height / 4) * scaleZ;
                        float sampleContinentalMask = NoiseGenerator.GetContinentalMask(sampleWorldX, sampleWorldZ, seed, continentalMaskFrequency);
                        Debug.Log($"Height Debug - Raw: {heightValue:F2}, Max: {maxPossibleHeight:F2}, Normalized: {normalizedHeight:F3}");
                        Debug.Log($"Continental Mask: {sampleContinentalMask:F3}, Threshold: {continentalThreshold:F3}");
                        
                        // Call GetTerrainHeight again with debug to see what's happening
                        // We'll manually break down the height calculation to match what GetTerrainHeight does
                        float testBaseNoise = NoiseGenerator.GetNoise2D(sampleWorldX, sampleWorldZ, seed, 0.001f);
                        float testHillNoise = NoiseGenerator.GetFractalNoise(sampleWorldX, sampleWorldZ, seed + 1000, 3, 0.003f, 2.2f, 0.5f);
                        float testMountainNoise = NoiseGenerator.GetMountainRangeNoise(sampleWorldX, sampleWorldZ, seed, mountainFrequency, warpStrength);
                        float testHillMask = Mathf.Max(0.5f, sampleContinentalMask);
                        float testMountainMask = Mathf.Max(0.3f, sampleContinentalMask * 0.8f);
                        float testPlainsHeight = Mathf.Max(testBaseNoise * baseHeight, baseHeight * 0.3f);
                        float testHillsHeight = testHillNoise * hillHeight * testHillMask;
                        float testMountainsHeight = testMountainNoise * mountainHeight * 1.5f * testMountainMask;
                        float testTotal = testPlainsHeight + testHillsHeight + testMountainsHeight;
                        
                        Debug.Log($"[NoiseGenerator] Called with world ({sampleWorldX:F1}, {sampleWorldZ:F1}):");
                        Debug.Log($"   baseNoise={testBaseNoise:F3}, hillNoise={testHillNoise:F3}, mountainNoise={testMountainNoise:F3}");
                        Debug.Log($"   continentalMask={sampleContinentalMask:F3}, hillMask={testHillMask:F3}, mountainMask={testMountainMask:F3}");
                        Debug.Log($"   plains={testPlainsHeight:F2}, hills={testHillsHeight:F2}, mountains={testMountainsHeight:F2}, TOTAL={testTotal:F2}");
                        
                        Debug.Log($"[TerrainGenerator] DETAILED Height Breakdown at pixel ({x}, {z}) / world ({sampleWorldX:F1}, {sampleWorldZ:F1}):");
                        Debug.Log($"   baseNoise={testBaseNoise:F3}, hillNoise={testHillNoise:F3}, mountainNoise={testMountainNoise:F3}");
                        Debug.Log($"   hillMask={testHillMask:F3}, mountainMask={testMountainMask:F3}");
                        Debug.Log($"   plains={testPlainsHeight:F2}, hills={testHillsHeight:F2}, mountains={testMountainsHeight:F2}");
                        // Add river and lake information
                        float sampleWaterCarving = NoiseGenerator.GetWaterCarvingFromPath(sampleWorldX, sampleWorldZ, riverPath, riverWidth, riverDepth, lakeRadius, lakeDepth);
                        float testTotalAfterCarving = Mathf.Max(0f, testTotal - sampleWaterCarving);
                        
                        Debug.Log($"   Manual TOTAL (before carving)={testTotal:F2}, Manual TOTAL (after carving)={testTotalAfterCarving:F2}");
                        Debug.Log($"   GetTerrainHeight Raw (after carving)={heightValue:F2} (should match Manual TOTAL after carving)");
                        Debug.Log($"   After normalization: {normalizedHeight:F3} (before heightCurve)");
                        Debug.Log($"   Height Params: baseHeight={baseHeight:F1}, hillHeight={hillHeight:F1}, mountainHeight={mountainHeight:F1}, peakSharpness={peakSharpness:F2}");
                        
                        Debug.Log($"[TerrainGenerator] Rivers and Lakes at world ({sampleWorldX:F1}, {sampleWorldZ:F1}):");
                        Debug.Log($"   waterCarving={sampleWaterCarving:F2}, heightBeforeCarving={testTotal:F2}, heightAfterCarving={testTotalAfterCarving:F2}");
                        Debug.Log($"   Water Params: riverWidth={riverWidth:F1}, riverDepth={riverDepth:F1}, lakeRadius={lakeRadius:F1}, lakeDepth={lakeDepth:F1}");
                        
                        loggedSample = true;
                    }
                    
                    // Apply height curve for more control
                    normalizedHeight = heightCurve.Evaluate(normalizedHeight);
                    
                    // Clamp to valid range (Unity terrain expects 0-1)
                    normalizedHeight = Mathf.Clamp01(normalizedHeight);

                    heights[z, x] = normalizedHeight;
                }
            }
            
            // Log actual height range for debugging
            Debug.Log($"  Actual Height Range: Min={actualMinHeight:F2}, Max={actualMaxHeight:F2}, Theoretical Max={maxPossibleHeight:F2}");

            // Count terrain distribution for verification
            // Adjusted thresholds to match actual height distribution (normalized heights are typically 0.15-0.3)
            int plainsCount = 0;
            int hillsCount = 0;
            int mountainsCount = 0;
            int peaksCount = 0;
            int totalPixels = width * height;
            
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float h = heights[z, x];
                    if (h < 0.15f) // Plains: below 15%
                        plainsCount++;
                    else if (h < 0.4f) // Hills: 15% - 40%
                        hillsCount++;
                    else if (h < 0.6f) // Mountains: 40% - 60%
                        mountainsCount++;
                    else // High mountains: 60%+
                        mountainsCount++;
                    
                    if (h > 0.6f) // Peaks: above 60%
                        peaksCount++;
                }
            }
            
            float plainsPercent = (plainsCount / (float)totalPixels) * 100f;
            float hillsPercent = (hillsCount / (float)totalPixels) * 100f;
            float mountainsPercent = (mountainsCount / (float)totalPixels) * 100f;
            float peaksPercent = (peaksCount / (float)totalPixels) * 100f;
            
            Debug.Log($"  Terrain distribution: Plains={plainsPercent:F0}%, Hills={hillsPercent:F0}%, Mountains={mountainsPercent:F0}%, Peaks={peaksPercent:F0}%");

            terrainData.SetHeights(0, 0, heights);
            Debug.Log("  Heightmap generated");
        }

        private void GenerateSplatmap(int seed)
        {
            Debug.Log("  Generating texture splatmap...");
            
            // Use ScriptableObject biome system if available
            if (useScriptableObjectBiomes && biomeCollection != null && biomeCollection.biomes != null && biomeCollection.biomes.Length > 0)
            {
                GenerateSplatmapFromBiomeCollection(seed);
                return;
            }
            
            // Fall back to legacy system
            Debug.LogWarning("BiomeCollection not assigned or empty! Using legacy biome system. Please assign a BiomeCollection asset or create one via: Hearthbound > Create Default Biome Collection");
            GenerateSplatmapLegacy(seed);
        }

        private void GenerateSplatmapFromBiomeCollection(int seed)
        {
            Debug.Log($"  Using BiomeCollection for texture splatmap... (Blend Factor: {biomeCollection.globalBlendFactor}, Biomes: {biomeCollection.biomes.Length})");
            
            // Get terrain layers from biome collection
            var terrainLayers = biomeCollection.GetAllTerrainLayers();
            if (terrainLayers == null || terrainLayers.Count == 0)
            {
                Debug.LogWarning("BiomeCollection has no terrain layers! Falling back to default.");
                CreateDefaultTerrainLayers();
                GenerateSplatmapLegacy(seed);
                return;
            }

            // Build mapping from biomes to terrain layer indices
            var biomeToLayerIndex = biomeCollection.GetBiomeToLayerIndexMap();
            
            Debug.Log($"  Generated {terrainLayers.Count} terrain layers from {biomeCollection.biomes.Length} biomes");
            
            // Set terrain layers
            terrainData.terrainLayers = terrainLayers.ToArray();

            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            int numTextures = terrainLayers.Count;

            float[,,] splatmapData = new float[alphamapWidth, alphamapHeight, numTextures];

            // FIXED: Generate temperature and humidity maps ONCE for the entire terrain
            // This ensures consistency and allows for proper biome zones
            Debug.Log("  Generating temperature map (latitude-based, NOT height-based)...");
            float[,] temperatureMap = GenerateTemperatureMap(alphamapWidth, alphamapHeight, seed);
            Debug.Log("  Generating humidity map (noise-based, NOT temperature-coupled)...");
            float[,] humidityMap = GenerateHumidityMap(alphamapWidth, alphamapHeight, seed);
            
            // Log sample temperature/humidity values to verify they vary correctly
            int sampleX = alphamapWidth / 2;
            int sampleZ1 = alphamapHeight / 4;  // Edge (should be cooler)
            int sampleZ2 = alphamapHeight / 2;  // Center (should be warmer)
            int sampleZ3 = alphamapHeight * 3 / 4; // Edge (should be cooler)
            
            float height1 = terrainData.GetHeight(Mathf.RoundToInt(sampleX / (float)alphamapWidth * terrainData.heightmapResolution), 
                                                  Mathf.RoundToInt(sampleZ1 / (float)alphamapHeight * terrainData.heightmapResolution)) / terrainHeight;
            float height2 = terrainData.GetHeight(Mathf.RoundToInt(sampleX / (float)alphamapWidth * terrainData.heightmapResolution), 
                                                  Mathf.RoundToInt(sampleZ2 / (float)alphamapHeight * terrainData.heightmapResolution)) / terrainHeight;
            float height3 = terrainData.GetHeight(Mathf.RoundToInt(sampleX / (float)alphamapWidth * terrainData.heightmapResolution), 
                                                  Mathf.RoundToInt(sampleZ3 / (float)alphamapHeight * terrainData.heightmapResolution)) / terrainHeight;
            
            Debug.Log($"  Temperature samples (same X={sampleX}, different Z positions):");
            Debug.Log($"     Z={sampleZ1} (edge): height={height1:F3}, temp={temperatureMap[sampleX, sampleZ1]:F3}, humid={humidityMap[sampleX, sampleZ1]:F3}");
            Debug.Log($"     Z={sampleZ2} (center): height={height2:F3}, temp={temperatureMap[sampleX, sampleZ2]:F3}, humid={humidityMap[sampleX, sampleZ2]:F3}");
            Debug.Log($"     Z={sampleZ3} (edge): height={height3:F3}, temp={temperatureMap[sampleX, sampleZ3]:F3}, humid={humidityMap[sampleX, sampleZ3]:F3}");
            Debug.Log($"  Notice: Temperature varies by Z position (latitude), not just height!");

            for (int z = 0; z < alphamapHeight; z++)
            {
                for (int x = 0; x < alphamapWidth; x++)
                {
                    // Get world position for noise sampling
                    float normX = x / (float)alphamapWidth;
                    float normZ = z / (float)alphamapHeight;
                    
                    // Get height at this position (normalized 0-1)
                    float height = terrainData.GetHeight(
                        Mathf.RoundToInt(normX * terrainData.heightmapResolution),
                        Mathf.RoundToInt(normZ * terrainData.heightmapResolution)
                    ) / terrainHeight;

                    // Get slope at this position
                    float slope = terrainData.GetSteepness(normX, normZ);

                    // FIXED: Use pre-generated temperature and humidity maps
                    float temperature = temperatureMap[x, z];
                    float moisture = humidityMap[x, z];

                    // Calculate biome weights using BiomeCollection
                    var biomeWeights = biomeCollection.CalculateBiomeWeights(moisture, temperature, height, slope);

                    // Check if this is a mountain area OR elevated area
                    // Prevent water on ANY elevated terrain (not just mountains)
                    bool isMountainArea = slope > steepSlope || height > rockHeight || height > 0.2f;
                    
                    // Remove water biome from mountain areas (even if it was calculated by biome system)
                    if (isMountainArea)
                    {
                        BiomeData waterBiomeToRemove = null;
                        foreach (var kvp in biomeWeights)
                        {
                            if (kvp.Key != null && kvp.Key.biomeName == "Water")
                            {
                                waterBiomeToRemove = kvp.Key;
                                break;
                            }
                        }
                        if (waterBiomeToRemove != null)
                        {
                            biomeWeights.Remove(waterBiomeToRemove);
                        }
                    }
                    // Force water biome for areas below water threshold (rivers/lakes)
                    // BUT only if NOT a mountain area AND water biomes are enabled
                    else if (height < waterHeight && !disableWaterBiomes)
                    {
                        // Find water biome and give it high priority
                        BiomeData waterBiome = null;
                        foreach (var kvp in biomeWeights)
                        {
                            if (kvp.Key != null && kvp.Key.biomeName == "Water")
                            {
                                waterBiome = kvp.Key;
                                break;
                            }
                        }
                        
                        if (waterBiome != null)
                        {
                            // Set water biome weight to 1.0 for areas below water threshold (but not mountains)
                            biomeWeights[waterBiome] = 1.0f;
                            // Reduce other biome weights
                            var keysToReduce = new List<BiomeData>();
                            foreach (var kvp in biomeWeights)
                            {
                                if (kvp.Key != waterBiome)
                                {
                                    keysToReduce.Add(kvp.Key);
                                }
                            }
                            foreach (var otherBiome in keysToReduce)
                            {
                                biomeWeights[otherBiome] *= 0.1f; // Reduce other biomes
                            }
                        }
                    }
                    // If water biomes are disabled, remove water biome from low areas too
                    else if (height < waterHeight && disableWaterBiomes)
                    {
                        BiomeData waterBiomeToRemove = null;
                        foreach (var kvp in biomeWeights)
                        {
                            if (kvp.Key != null && kvp.Key.biomeName == "Water")
                            {
                                waterBiomeToRemove = kvp.Key;
                                break;
                            }
                        }
                        if (waterBiomeToRemove != null)
                        {
                            biomeWeights.Remove(waterBiomeToRemove);
                        }
                    }

                    // Convert biome weights to texture weights using the mapping
                    float[] weights = new float[numTextures];
                    float totalWeight = 0f;

                    // Map each biome's weight to its corresponding terrain layer index
                    foreach (var kvp in biomeWeights)
                    {
                        BiomeData biome = kvp.Key;
                        float weight = kvp.Value;
                        
                        if (biomeToLayerIndex.ContainsKey(biome))
                        {
                            int layerIndex = biomeToLayerIndex[biome];
                            if (layerIndex >= 0 && layerIndex < numTextures)
                            {
                                weights[layerIndex] = weight;
                                totalWeight += weight;
                            }
                        }
                    }

                    // Debug logging: Sample a few pixels to see what's happening
                    bool isSamplePoint = (x == alphamapWidth / 4 && z == alphamapHeight / 4) || 
                                        (x == alphamapWidth / 2 && z == alphamapHeight / 2) ||
                                        (x == alphamapWidth * 3 / 4 && z == alphamapHeight * 3 / 4);
                    
                    if (isSamplePoint)
                    {
                        string biomeInfo = $"Sample at ({x},{z}): height={height:F3}, temp={temperature:F3}, moisture={moisture:F3}\n";
                        biomeInfo += $"  Biome weights: ";
                        foreach (var kvp in biomeWeights)
                        {
                            if (biomeToLayerIndex.ContainsKey(kvp.Key))
                            {
                                float normalizedWeight = totalWeight > 0.001f ? kvp.Value / totalWeight : 0f;
                                if (normalizedWeight > 0.05f)
                                {
                                    biomeInfo += $"{kvp.Key.biomeName}={normalizedWeight:P0} ";
                                }
                            }
                        }
                        Debug.Log(biomeInfo);
                    }

                    // Normalize weights (Unity needs weights to sum to 1)
                    if (totalWeight > 0.001f)
                    {
                        for (int i = 0; i < numTextures; i++)
                        {
                            weights[i] /= totalWeight;
                        }
                    }
                    else
                    {
                        // No biome weights calculated - fall back to first texture
                        if (numTextures > 0)
                            weights[0] = 1f;
                    }

                    // Assign weights to splatmap
                    for (int i = 0; i < numTextures; i++)
                    {
                        splatmapData[z, x, i] = weights[i];
                    }
                }
            }

            // Apply the splatmap to terrain
            terrainData.SetAlphamaps(0, 0, splatmapData);
            Debug.Log("  Splatmap applied from BiomeCollection");
        }

        /// <summary>
        /// FIXED: Generate temperature map independently from height
        /// Uses latitude-like gradient + noise for variation
        /// </summary>
        private float[,] GenerateTemperatureMap(int width, int height, int seed)
        {
            float[,] tempMap = new float[width, height];
            
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normX = x / (float)width;
                    float normZ = z / (float)height;
                    float worldX = normX * terrainWidth;
                    float worldZ = normZ * terrainLength;
                    
                    // Latitude-like gradient: warmer at center (equator), cooler at edges (poles)
                    // This creates horizontal temperature bands
                    float latitudeGradient = 1f - Mathf.Abs((normZ - 0.5f) * 2f); // 0 at edges, 1 at center
                    latitudeGradient = Mathf.Pow(latitudeGradient, 1.5f); // Make gradient less linear
                    
                    // Add noise variation for local temperature variations
                    float temperatureNoise = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed + 10000, temperatureFrequency);
                    
                    // Combine latitude gradient (70%) and noise (30%)
                    float temperature = latitudeGradient * 0.7f + temperatureNoise * 0.3f;
                    
                    // Optional: Slight altitude influence (higher = cooler, but not dominant)
                    float terrainHeight = terrainData.GetHeight(
                        Mathf.RoundToInt(normX * terrainData.heightmapResolution),
                        Mathf.RoundToInt(normZ * terrainData.heightmapResolution)
                    ) / this.terrainHeight;
                    
                    // Reduce temperature slightly at high elevations (max 20% reduction)
                    float altitudeEffect = terrainHeight * 0.2f;
                    temperature = Mathf.Clamp01(temperature - altitudeEffect);
                    
                    tempMap[x, z] = temperature;
                }
            }
            
            return tempMap;
        }

        /// <summary>
        /// FIXED: Generate humidity map independently from height
        /// Uses noise patterns for rainfall + slight height influence
        /// </summary>
        private float[,] GenerateHumidityMap(int width, int height, int seed)
        {
            float[,] humidMap = new float[width, height];
            
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normX = x / (float)width;
                    float normZ = z / (float)height;
                    float worldX = normX * terrainWidth;
                    float worldZ = normZ * terrainLength;
                    
                    // Base humidity from noise (rainfall patterns)
                    // Use different frequency for more varied patterns
                    float baseHumidity = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed + 20000, moistureFrequency);
                    
                    // Add second noise layer for more complex patterns
                    float humidityDetail = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed + 30000, moistureFrequency * 2f) * 0.3f;
                    
                    // Combine base and detail
                    float humidity = baseHumidity * 0.7f + humidityDetail;
                    
                    // Optional: Slight height influence (lower elevations slightly more humid)
                    float terrainHeight = terrainData.GetHeight(
                        Mathf.RoundToInt(normX * terrainData.heightmapResolution),
                        Mathf.RoundToInt(normZ * terrainData.heightmapResolution)
                    ) / this.terrainHeight;
                    
                    // Boost humidity slightly at low elevations (max 15% boost)
                    float heightBoost = (1f - terrainHeight) * 0.15f;
                    humidity = Mathf.Clamp01(humidity + heightBoost);
                    
                    humidMap[x, z] = humidity;
                }
            }
            
            return humidMap;
        }

        private void GenerateSplatmapLegacy(int seed)
        {
            Debug.Log("  Using legacy biome system for texture splatmap...");
            
            // Ensure we have terrain layers
            if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length == 0)
            {
                CreateDefaultTerrainLayers();
            }

            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            int numTextures = terrainData.terrainLayers.Length;

            float[,,] splatmapData = new float[alphamapWidth, alphamapHeight, numTextures];

            for (int z = 0; z < alphamapHeight; z++)
            {
                for (int x = 0; x < alphamapWidth; x++)
                {
                    // Get world position for noise sampling
                    float normX = x / (float)alphamapWidth;
                    float normZ = z / (float)alphamapHeight;
                    float worldX = normX * terrainWidth;
                    float worldZ = normZ * terrainLength;
                    
                    // Get height at this position (normalized 0-1)
                    float height = terrainData.GetHeight(
                        Mathf.RoundToInt(normX * terrainData.heightmapResolution),
                        Mathf.RoundToInt(normZ * terrainData.heightmapResolution)
                    ) / terrainHeight;

                    // Get slope at this position
                    float slope = terrainData.GetSteepness(normX, normZ);

                    // Calculate texture weights
                    float[] weights = new float[numTextures];
                    
                    if (useAdvancedBiomes)
                    {
                        // Advanced biome system with moisture/temperature
                        weights = CalculateAdvancedBiomeWeights(worldX, worldZ, height, slope, seed);
                    }
                    else
                    {
                        // Simple biome system (original)
                        weights = CalculateSimpleBiomeWeights(height, slope);
                    }

                    // Normalize weights
                    float totalWeight = 0f;
                    for (int i = 0; i < numTextures; i++)
                        totalWeight += weights[i];
                    
                    if (totalWeight > 0f)
                    {
                        for (int i = 0; i < numTextures; i++)
                            weights[i] /= totalWeight;
                    }
                    else
                    {
                        weights[0] = 1f; // Default to grass
                    }

                    // Apply weights
                    for (int i = 0; i < numTextures; i++)
                    {
                        splatmapData[x, z, i] = weights[i];
                    }
                }
            }

            terrainData.SetAlphamaps(0, 0, splatmapData);
            Debug.Log("  Splatmap generated (legacy system)");
        }

        /// <summary>
        /// Calculate biome weights using simple height/slope system
        /// </summary>
        private float[] CalculateSimpleBiomeWeights(float height, float slope)
        {
            float[] weights = new float[6];
            
            // Layer 0: Water (very low elevation) - only if water biomes are enabled
            if (height < waterHeight && !disableWaterBiomes)
                weights[0] = 1f;
            
            // Layer 1: Plains/Grass (low elevation, gentle slope)
            else if (height < grassHeight && slope < steepSlope)
                weights[1] = 1f;
            
            // Layer 3: Rock/Mountains (steep slopes or medium-high elevation)
            else if (slope > steepSlope || (height >= rockHeight && height < snowHeight))
                weights[3] = 1f;
            
            // Layer 4: Snow (very high elevation)
            else if (height >= snowHeight)
                weights[4] = 1f;
            
            // Layer 5: Dirt (transition areas)
            else
                weights[5] = 0.5f;

            return weights;
        }

        /// <summary>
        /// Calculate biome weights using advanced moisture/temperature system
        /// Creates smoother transitions and more biome variety
        /// </summary>
        private float[] CalculateAdvancedBiomeWeights(float worldX, float worldZ, float height, float slope, int seed)
        {
            float[] weights = new float[6];
            
            // Get moisture and temperature values (0-1 range)
            // Temperature decreases with height (realistic: higher elevation = lower temperature)
            float baseTemperature = 1f - height;
            float temperatureNoise = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed + 10000, temperatureFrequency) * 0.2f - 0.1f;
            float temperature = baseTemperature + temperatureNoise;
            temperature = Mathf.Clamp01(temperature);
            
            // Humidity: Per Worldengine/Holdridge model - temperature affects humidity capacity
            float baseHumidity = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed, moistureFrequency);
            float heightHumidityBoost = Mathf.Pow(1f - height, 2f) * 0.4f;
            float rawHumidity = baseHumidity + heightHumidityBoost;
            rawHumidity = Mathf.Clamp01(rawHumidity);
            
            // Apply temperature-based humidity multiplier (colder = less moisture capacity)
            float gammaOffset = 0.2f;
            float gammaValue = 1.0f;
            float tempHumidityMultiplier = gammaOffset + (1f - gammaOffset) * Mathf.Pow(temperature, gammaValue);
            float moisture = rawHumidity * tempHumidityMultiplier;
            moisture = Mathf.Clamp01(moisture);

            // Layer 0: Water (very low elevation, high moisture areas) - only if water biomes are enabled
            if (height < waterHeight && !disableWaterBiomes)
            {
                weights[0] = 1f;
                return weights; // Water takes priority
            }

            // Layer 4: Snow (very high elevation, low temperature)
            if (height >= snowHeight)
            {
                float snowWeight = CalculateBiomeWeight(height, slope, moisture, temperature,
                    heightRange: new Vector2(snowHeight, 1f),
                    slopeRange: new Vector2(0f, 90f),
                    moistureRange: new Vector2(0f, 1f),
                    temperatureRange: new Vector2(0f, 0.3f));
                weights[4] = snowWeight;
                
                // If not fully snow, add some rock for variety
                if (snowWeight < 0.9f && slope > steepSlope)
                    weights[3] = (1f - snowWeight) * 0.5f; // Rock on steep snowy slopes
            }
            // Layer 3: Rock/Mountains (steep slopes or medium-high elevation)
            else if (slope > steepSlope || height >= rockHeight)
            {
                float rockWeight = CalculateBiomeWeight(height, slope, moisture, temperature,
                    heightRange: new Vector2(rockHeight, snowHeight),
                    slopeRange: new Vector2(steepSlope, 90f),
                    moistureRange: new Vector2(0f, 0.5f),
                    temperatureRange: new Vector2(0.2f, 0.8f));
                
                // Steep slopes are always rocky
                if (slope > steepSlope)
                    rockWeight = Mathf.Max(rockWeight, 0.8f);
                
                weights[3] = rockWeight;
                
                // Add some dirt for transitions
                if (rockWeight < 0.9f)
                    weights[5] = (1f - rockWeight) * 0.3f;
            }
            // Layer 2: Forest (moderate elevation, high moisture, moderate temperature)
            else if (moisture >= forestMoistureMin && moisture <= forestMoistureMax &&
                     temperature >= forestTemperatureMin && temperature <= forestTemperatureMax &&
                     height >= waterHeight && height < rockHeight && slope < steepSlope)
            {
                float forestWeight = CalculateBiomeWeight(height, slope, moisture, temperature,
                    heightRange: new Vector2(waterHeight, rockHeight),
                    slopeRange: new Vector2(0f, steepSlope * 0.7f),
                    moistureRange: new Vector2(forestMoistureMin, forestMoistureMax),
                    temperatureRange: new Vector2(forestTemperatureMin, forestTemperatureMax));
                weights[2] = forestWeight;
                
                // Blend with plains
                weights[1] = (1f - forestWeight) * 0.5f;
            }
            // Layer 1: Plains/Grass (low-moderate elevation, gentle slope)
            else if (height < grassHeight && slope < steepSlope)
            {
                float plainsWeight = CalculateBiomeWeight(height, slope, moisture, temperature,
                    heightRange: new Vector2(waterHeight, grassHeight),
                    slopeRange: new Vector2(0f, steepSlope),
                    moistureRange: new Vector2(0.2f, 0.7f),
                    temperatureRange: new Vector2(0.4f, 0.8f));
                weights[1] = plainsWeight;
                
                // Add some dirt for variety
                weights[5] = (1f - plainsWeight) * 0.3f;
            }
            // Layer 5: Dirt (transition areas)
            else
            {
                weights[5] = 0.7f;
                // Blend with adjacent biomes
                if (height < rockHeight)
                    weights[1] = 0.3f; // Some plains
                else
                    weights[3] = 0.3f; // Some rock
            }

            return weights;
        }

        /// <summary>
        /// Calculate weight for a specific biome based on multiple factors
        /// Uses smooth falloff for natural transitions
        /// </summary>
        private float CalculateBiomeWeight(float height, float slope, float moisture, float temperature,
            Vector2 heightRange, Vector2 slopeRange, Vector2 moistureRange, Vector2 temperatureRange)
        {
            // Calculate how well this position matches each biome factor
            float heightMatch = GetFactorMatch(height, heightRange.x, heightRange.y);
            float slopeMatch = GetFactorMatch(slope / 90f, slopeRange.x / 90f, slopeRange.y / 90f); // Normalize slope
            float moistureMatch = GetFactorMatch(moisture, moistureRange.x, moistureRange.y);
            float temperatureMatch = GetFactorMatch(temperature, temperatureRange.x, temperatureRange.y);

            // Combine factors (all must be somewhat favorable)
            float combinedMatch = heightMatch * slopeMatch * moistureMatch * temperatureMatch;
            
            // Apply smooth falloff for blending
            return Mathf.SmoothStep(0f, 1f, combinedMatch);
        }

        /// <summary>
        /// Get how well a value matches a range (0-1, with smooth falloff)
        /// </summary>
        private float GetFactorMatch(float value, float min, float max)
        {
            if (value < min)
            {
                // Below range - smooth falloff
                float distance = min - value;
                float falloffRange = biomeBlendDistance;
                return Mathf.Clamp01(1f - (distance / falloffRange));
            }
            else if (value > max)
            {
                // Above range - smooth falloff
                float distance = value - max;
                float falloffRange = biomeBlendDistance;
                return Mathf.Clamp01(1f - (distance / falloffRange));
            }
            else
            {
                // Within range - full match
                return 1f;
            }
        }

        private void GenerateDetailLayers(int seed)
        {
            Debug.Log("  Generating detail layers (grass, rocks)...");
            
            // This is a placeholder - detail layers require DetailPrototype setup
            // You would add grass, rocks, etc. here based on terrain height/slope
            
            // Example structure:
            // DetailPrototype[] details = new DetailPrototype[2];
            // details[0] = new DetailPrototype { prototypeTexture = grassTexture };
            // details[1] = new DetailPrototype { prototypeTexture = rockTexture };
            // terrainData.detailPrototypes = details;
            
            Debug.Log("  Detail layers not yet implemented (add grass/rock prefabs)");
        }

        private void CreateDefaultTerrainLayers()
        {
            Debug.Log("  Creating default terrain layers with distinct biome colors...");
            
            // Create 6 terrain layers for different biomes
            // Using highly distinct colors for better visibility
            TerrainLayer[] layers = new TerrainLayer[6];
            
            // Layer 0: Water/Beach - Bright Cyan/Blue (more saturated)
            layers[0] = new TerrainLayer();
            layers[0].diffuseTexture = CreateColoredTexture(new Color(0.1f, 0.5f, 0.9f), "Water");
            layers[0].tileSize = new Vector2(15, 15);
            layers[0].diffuseTexture.name = "WaterTexture";
            
            // Layer 1: Plains/Grass - Yellow-Green (light, distinct from forest)
            layers[1] = new TerrainLayer();
            layers[1].diffuseTexture = CreateColoredTexture(new Color(0.6f, 0.9f, 0.3f), "Plains");
            layers[1].tileSize = new Vector2(15, 15);
            layers[1].diffuseTexture.name = "PlainsTexture";
            
            // Layer 2: Forest - Deep Saturated Green (very dark, distinct from plains)
            layers[2] = new TerrainLayer();
            layers[2].diffuseTexture = CreateColoredTexture(new Color(0.05f, 0.5f, 0.15f), "Forest");
            layers[2].tileSize = new Vector2(15, 15);
            layers[2].diffuseTexture.name = "ForestTexture";
            
            // Layer 3: Rock/Mountains - Tan/Brown (instead of grey, more visible)
            layers[3] = new TerrainLayer();
            layers[3].diffuseTexture = CreateColoredTexture(new Color(0.7f, 0.6f, 0.4f), "Rock");
            layers[3].tileSize = new Vector2(15, 15);
            layers[3].diffuseTexture.name = "RockTexture";
            
            // Layer 4: Snow - Pure White (maximum visibility)
            layers[4] = new TerrainLayer();
            layers[4].diffuseTexture = CreateColoredTexture(new Color(1.0f, 1.0f, 1.0f), "Snow");
            layers[4].tileSize = new Vector2(15, 15);
            layers[4].diffuseTexture.name = "SnowTexture";
            
            // Layer 5: Dirt - Dark Brown (distinct transition color)
            layers[5] = new TerrainLayer();
            layers[5].diffuseTexture = CreateColoredTexture(new Color(0.3f, 0.2f, 0.1f), "Dirt");
            layers[5].tileSize = new Vector2(15, 15);
            layers[5].diffuseTexture.name = "DirtTexture";

            terrainData.terrainLayers = layers;
            Debug.Log("  Terrain layers created with distinct colors: Water (Cyan), Plains (Yellow-Green), Forest (Deep Green), Rock (Tan), Snow (White), Dirt (Dark Brown)");
        }

        /// <summary>
        /// Create a simple colored texture for placeholder biomes
        /// </summary>
        private Texture2D CreateColoredTexture(Color color, string name)
        {
            int size = 64; // Small texture size for placeholders
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            // Fill with base color
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            // Add some simple noise for texture variation
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                    float variation = (noise - 0.5f) * 0.1f; // ±10% variation
                    Color pixelColor = color;
                    pixelColor.r = Mathf.Clamp01(pixelColor.r + variation);
                    pixelColor.g = Mathf.Clamp01(pixelColor.g + variation);
                    pixelColor.b = Mathf.Clamp01(pixelColor.b + variation);
                    pixels[y * size + x] = pixelColor;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            texture.name = name;
            
            return texture;
        }
        #endregion

        #region Clear Terrain
        public void ClearTerrain()
        {
            Debug.Log("Clearing terrain...");
            
            // Clear water planes first
            ClearAllWaterPlanes();
            
            // Clear river water
            ClearRiverWater();
            
            // Ensure terrain is initialized
            EnsureTerrainInitialized();
            if (terrain == null)
            {
                Debug.LogError("Cannot clear terrain: Terrain component is missing!");
                return;
            }
            
            // Ensure terrain data is initialized
            if (terrainData == null)
            {
                InitializeTerrainData();
            }
            
            if (terrainData == null)
            {
                Debug.LogError("Cannot clear terrain: TerrainData is null!");
                return;
            }
            
            // Reset heightmap to flat
            int width = terrainData.heightmapResolution;
            int height = terrainData.heightmapResolution;
            float[,] heights = new float[width, height];
            
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    heights[z, x] = 0f;
                }
            }
            
            terrainData.SetHeights(0, 0, heights);
            Debug.Log("Terrain cleared");
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Get terrain height at world position
        /// </summary>
        public float GetHeightAtPosition(Vector3 worldPosition)
        {
            Vector3 terrainPosition = worldPosition - terrain.transform.position;
            Vector3 normalizedPosition = new Vector3(
                terrainPosition.x / terrainData.size.x,
                0,
                terrainPosition.z / terrainData.size.z
            );
            
            return terrainData.GetInterpolatedHeight(normalizedPosition.x, normalizedPosition.z);
        }

        /// <summary>
        /// Get terrain normal at world position
        /// </summary>
        public Vector3 GetNormalAtPosition(Vector3 worldPosition)
        {
            Vector3 terrainPosition = worldPosition - terrain.transform.position;
            Vector3 normalizedPosition = new Vector3(
                terrainPosition.x / terrainData.size.x,
                0,
                terrainPosition.z / terrainData.size.z
            );
            
            return terrainData.GetInterpolatedNormal(normalizedPosition.x, normalizedPosition.z);
        }

        /// <summary>
        /// Check if position is valid for placement (not too steep)
        /// </summary>
        public bool IsValidPlacementPosition(Vector3 worldPosition, float maxSlope = 45f)
        {
            float slope = GetSlopeAtPosition(worldPosition);
            return slope <= maxSlope;
        }

        /// <summary>
        /// Get slope in degrees at world position
        /// </summary>
        public float GetSlopeAtPosition(Vector3 worldPosition)
        {
            Vector3 terrainPosition = worldPosition - terrain.transform.position;
            Vector3 normalizedPosition = new Vector3(
                terrainPosition.x / terrainData.size.x,
                0,
                terrainPosition.z / terrainData.size.z
            );
            
            return terrainData.GetSteepness(normalizedPosition.x, normalizedPosition.z);
        }

        /// <summary>
        /// Get biome information at world position
        /// Returns a string describing the primary biome
        /// </summary>
        public string GetBiomeAtPosition(Vector3 worldPosition, int seed)
        {
            float height = GetHeightAtPosition(worldPosition) / terrainHeight;
            float slope = GetSlopeAtPosition(worldPosition);
            // Temperature decreases with height (realistic: higher elevation = lower temperature)
            float baseTemperature = 1f - height;
            float temperatureNoise = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed + 10000, temperatureFrequency) * 0.2f - 0.1f;
            float temperature = baseTemperature + temperatureNoise;
            temperature = Mathf.Clamp01(temperature);
            
            // Humidity: Increases near water/low altitude, affected by temperature
            float baseHumidity = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed, moistureFrequency);
            float heightHumidityBoost = (1f - height) * 0.3f; // Higher at low elevations
            float tempHumidityInfluence = temperature * 0.2f;
            float moisture = baseHumidity + heightHumidityBoost + tempHumidityInfluence;
            moisture = Mathf.Clamp01(moisture);

            // Determine primary biome (matching the advanced biome system)
            if (height < waterHeight)
                return "Water";
            else if (height >= snowHeight)
                return "Snow";
            else if (slope > steepSlope || height >= rockHeight)
                return "Rock";
            else if (moisture >= forestMoistureMin && moisture <= forestMoistureMax &&
                     temperature >= forestTemperatureMin && temperature <= forestTemperatureMax &&
                     height >= waterHeight && height < rockHeight && slope < steepSlope)
                return "Forest";
            else if (height < grassHeight && slope < steepSlope)
                return "Plains";
            else
                return "Dirt";
        }

        /// <summary>
        /// Get detailed biome data at world position
        /// </summary>
        public BiomeInfo GetBiomeInfoAtPosition(Vector3 worldPosition, int seed)
        {
            float height = GetHeightAtPosition(worldPosition) / terrainHeight;
            float slope = GetSlopeAtPosition(worldPosition);
            // Temperature decreases with height (realistic: higher elevation = lower temperature)
            float baseTemperature = 1f - height;
            float temperatureNoise = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed + 10000, temperatureFrequency) * 0.2f - 0.1f;
            float temperature = baseTemperature + temperatureNoise;
            temperature = Mathf.Clamp01(temperature);
            
            // Humidity: Increases near water/low altitude, affected by temperature
            float baseHumidity = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed, moistureFrequency);
            float heightHumidityBoost = (1f - height) * 0.3f; // Higher at low elevations
            float tempHumidityInfluence = temperature * 0.2f;
            float moisture = baseHumidity + heightHumidityBoost + tempHumidityInfluence;
            moisture = Mathf.Clamp01(moisture);

            return new BiomeInfo
            {
                height = height,
                slope = slope,
                moisture = moisture,
                temperature = temperature,
                biomeName = GetBiomeAtPosition(worldPosition, seed)
            };
        }
        
        /// <summary>
        /// Get BiomeData ScriptableObject at world position (if using ScriptableObject system)
        /// </summary>
        public BiomeData GetBiomeDataAtPosition(Vector3 worldPosition, int seed)
        {
            if (useScriptableObjectBiomes && biomeCollection != null)
            {
                float height = GetHeightAtPosition(worldPosition) / terrainHeight;
                float slope = GetSlopeAtPosition(worldPosition);
                // Matches GitHub: Pure Perlin noise (not height-based)
                float moisture = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed, moistureFrequency);
                float temperature = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed + 10000, temperatureFrequency);
                temperature = Mathf.Clamp01(temperature);
                
                return biomeCollection.GetPrimaryBiome(moisture, temperature, height, slope);
            }
            return null;
        }
        #endregion

        #region Biome Data Structure (Legacy)
        [System.Serializable]
        public class BiomeInfo
        {
            public float height;
            public float slope;
            public float moisture;
            public float temperature;
            public string biomeName;
        }
        #endregion

        #region Public Setters (for Preset System)
        /// <summary>
        /// Set terrain generation parameters programmatically (for preset system)
        /// </summary>
        public void SetBaseHeight(float value) => baseHeight = value;
        public void SetHillHeight(float value) => hillHeight = value;
        public void SetMountainHeight(float value) => mountainHeight = value;
        public void SetHeightCurve(AnimationCurve curve) => heightCurve = curve;
        public void SetWaterHeight(float value) => waterHeight = value;
        public void SetGrassHeight(float value) => grassHeight = value;
        public void SetRockHeight(float value) => rockHeight = value;
        public void SetSnowHeight(float value) => snowHeight = value;
        public void SetContinentalThreshold(float value) => continentalThreshold = value;
        public void SetContinentalMaskFrequency(float value) => continentalMaskFrequency = value;
        public void SetWarpStrength(float value) => warpStrength = value;
        public void SetMountainFrequency(float value) => mountainFrequency = value;
        public void SetPeakSharpness(float value) => peakSharpness = value;

        public void SetTerrainSize(float width, float length, float height)
        {
            terrainWidth = width;
            terrainLength = length;
            terrainHeight = height;
            if (terrainData != null)
            {
                terrainData.size = new Vector3(width, height, length);
            }
        }

        public void SetHeightmapResolution(int resolution)
        {
            heightmapResolution = resolution;
            if (terrainData != null)
            {
                terrainData.heightmapResolution = resolution;
            }
        }
        #endregion

        /// <summary>
        /// Generate water GameObjects along the river path and in the lake
        /// </summary>
        private void GenerateRiverWater()
        {
            if (lastGeneratedRiverPath == null || lastGeneratedRiverPath.Count < 2)
            {
                Debug.LogWarning("⚠️ Cannot generate river water: No valid river path");
                return;
            }
            
            EnsureTerrainInitialized();
            if (terrain == null || terrainData == null)
                return;
            
            Vector3 terrainPos = terrain.transform.position;
            
            // Generate a single continuous mesh for the entire river
            GameObject riverWaterMesh = CreateRiverWaterMesh(terrainPos);
            if (riverWaterMesh != null)
            {
                riverWaterObjects.Add(riverWaterMesh);
            }
            
            // Generate circular lake at the end
            if (lastGeneratedRiverPath.Count > 0)
            {
                Vector2 lakeCenter = RiverPathGenerator.GetLakeCenter(lastGeneratedRiverPath);
                Vector3 lakeCenterWorld = new Vector3(lakeCenter.x, 0, lakeCenter.y) + terrainPos;
                // Note: terrain.SampleHeight returns the height AFTER carving, so it's already at the bottom of the lake
                float terrainLakeHeight = terrain.SampleHeight(lakeCenterWorld);
                
                // Raise water higher to be more visible in the carved basin
                float lakeHeight = terrainLakeHeight + 2.0f;
                
                // Make lake slightly larger for better visual coverage
                GameObject lakeWater = CreateCircularLake(lakeCenterWorld + Vector3.up * lakeHeight, lakeRadius * 1.1f);
                if (lakeWater != null)
                {
                    riverWaterObjects.Add(lakeWater);
                }
            }
            
            Debug.Log($"💧 Generated {riverWaterObjects.Count} water objects along river path (1 continuous mesh + lake)");
        }
        
        /// <summary>
        /// Create a single continuous mesh for the entire river path
        /// </summary>
        private GameObject CreateRiverWaterMesh(Vector3 terrainPos)
        {
            if (lastGeneratedRiverPath == null || lastGeneratedRiverPath.Count < 2)
                return null;
            
            GameObject riverWater = new GameObject("RiverWater");
            riverWater.transform.SetParent(transform);
            
            MeshFilter mf = riverWater.AddComponent<MeshFilter>();
            MeshRenderer mr = riverWater.AddComponent<MeshRenderer>();
            
            // Build vertices and triangles for the entire river
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            
            float halfWidth = riverWidth * 0.6f; // Slightly wider for better coverage
            
            // Generate vertices for each segment
            for (int i = 0; i < lastGeneratedRiverPath.Count; i++)
            {
                Vector2 pathPoint = lastGeneratedRiverPath[i];
                Vector3 worldPos = new Vector3(pathPoint.x, 0, pathPoint.y) + terrainPos;
                
                // Sample terrain height at this point
                float terrainHeight = terrain.SampleHeight(worldPos);
                // Raise water higher to be more visible in the carved valley
                float waterHeight = terrainHeight + 2.0f;
                
                Vector3 centerVertex = worldPos + Vector3.up * waterHeight;
                
                // Calculate direction for this point
                Vector3 direction = Vector3.forward;
                if (i < lastGeneratedRiverPath.Count - 1)
                {
                    Vector2 nextPoint = lastGeneratedRiverPath[i + 1];
                    Vector3 nextWorld = new Vector3(nextPoint.x, 0, nextPoint.y) + terrainPos;
                    direction = (nextWorld - worldPos).normalized;
                }
                else if (i > 0)
                {
                    Vector2 prevPoint = lastGeneratedRiverPath[i - 1];
                    Vector3 prevWorld = new Vector3(prevPoint.x, 0, prevPoint.y) + terrainPos;
                    direction = (worldPos - prevWorld).normalized;
                }
                
                // Calculate perpendicular for width
                Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized * halfWidth;
                
                // Add two vertices (left and right edge)
                vertices.Add(centerVertex + perpendicular);
                vertices.Add(centerVertex - perpendicular);
                
                // UV coordinates
                float u = i / (float)(lastGeneratedRiverPath.Count - 1);
                uvs.Add(new Vector2(u, 0));
                uvs.Add(new Vector2(u, 1));
            }
            
            // Build triangles connecting segments
            for (int i = 0; i < lastGeneratedRiverPath.Count - 1; i++)
            {
                int baseIndex = i * 2;
                
                // First triangle: baseIndex, baseIndex+1, baseIndex+2
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                
                // Second triangle: baseIndex+1, baseIndex+3, baseIndex+2
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 2);
            }
            
            // Create and assign mesh
            Mesh mesh = new Mesh();
            mesh.name = "RiverWaterMesh";
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            mf.mesh = mesh;
            
            // Apply material
            if (riverWaterMaterial != null)
            {
                mr.material = riverWaterMaterial;
            }
            else
            {
                Material waterMat = new Material(Shader.Find("Standard"));
                waterMat.color = new Color(0.2f, 0.4f, 0.7f, 0.8f);
                waterMat.SetFloat("_Metallic", 0f);
                waterMat.SetFloat("_Glossiness", 0.8f);
                waterMat.SetFloat("_Mode", 3);
                waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                waterMat.SetInt("_ZWrite", 0);
                waterMat.DisableKeyword("_ALPHATEST_ON");
                waterMat.EnableKeyword("_ALPHABLEND_ON");
                waterMat.renderQueue = 3000;
                mr.material = waterMat;
            }
            
            return riverWater;
        }
        
        /// <summary>
        /// Create a water quad between two points (deprecated - use CreateRiverWaterMesh instead)
        /// </summary>
        private GameObject CreateWaterQuad(Vector3 start, Vector3 end, float width)
        {
            GameObject quad = new GameObject("RiverWater");
            quad.transform.SetParent(transform);
            
            MeshFilter mf = quad.AddComponent<MeshFilter>();
            MeshRenderer mr = quad.AddComponent<MeshRenderer>();
            
            // Create quad mesh
            Mesh mesh = new Mesh();
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized * width * 0.5f;
            
            Vector3[] vertices = new Vector3[4];
            vertices[0] = start + perpendicular;
            vertices[1] = start - perpendicular;
            vertices[2] = end + perpendicular;
            vertices[3] = end - perpendicular;
            
            int[] triangles = new int[] { 0, 1, 2, 2, 1, 3 };
            Vector2[] uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.name = "RiverWaterMesh";
            
            mf.mesh = mesh;
            
            // Apply material
            if (riverWaterMaterial != null)
            {
                mr.material = riverWaterMaterial;
            }
            else
            {
                Material waterMat = new Material(Shader.Find("Standard"));
                waterMat.color = new Color(0.2f, 0.4f, 0.7f, 0.8f);
                waterMat.SetFloat("_Metallic", 0f);
                waterMat.SetFloat("_Glossiness", 0.8f);
                waterMat.SetFloat("_Mode", 3);
                waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                waterMat.SetInt("_ZWrite", 0);
                waterMat.DisableKeyword("_ALPHATEST_ON");
                waterMat.EnableKeyword("_ALPHABLEND_ON");
                waterMat.renderQueue = 3000;
                mr.material = waterMat;
            }
            
            return quad;
        }
        
        /// <summary>
        /// Create a circular lake
        /// </summary>
        private GameObject CreateCircularLake(Vector3 center, float radius)
        {
            GameObject lake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lake.name = "LakeWater";
            lake.transform.SetParent(transform);
            lake.transform.position = center;
            lake.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
            
            // Remove collider (or keep it if you want water physics)
            Collider col = lake.GetComponent<Collider>();
            if (col != null)
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(col);
                }
                else
                #endif
                {
                    Destroy(col);
                }
            }
            
            MeshRenderer mr = lake.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                if (riverWaterMaterial != null)
                {
                    mr.material = riverWaterMaterial;
                }
                else
                {
                    Material waterMat = new Material(Shader.Find("Standard"));
                    waterMat.color = new Color(0.2f, 0.4f, 0.7f, 0.8f);
                    waterMat.SetFloat("_Metallic", 0f);
                    waterMat.SetFloat("_Glossiness", 0.8f);
                    waterMat.SetFloat("_Mode", 3);
                    waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    waterMat.SetInt("_ZWrite", 0);
                    waterMat.DisableKeyword("_ALPHATEST_ON");
                    waterMat.EnableKeyword("_ALPHABLEND_ON");
                    waterMat.renderQueue = 3000;
                    mr.material = waterMat;
                }
            }
            
            return lake;
        }
        
        /// <summary>
        /// Clear all generated river water objects
        /// </summary>
        private void ClearRiverWater()
        {
            foreach (GameObject waterObj in riverWaterObjects)
            {
                if (waterObj != null)
                {
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(waterObj);
                    }
                    else
                    #endif
                    {
                        Destroy(waterObj);
                    }
                }
            }
            riverWaterObjects.Clear();
        }
        
        #region Debug
        [ContextMenu("Generate Terrain (Test Seed)")]
        private void DebugGenerateTerrain()
        {
            GenerateTerrain(12345);
        }

        [ContextMenu("Clear Terrain")]
        private void DebugClearTerrain()
        {
            ClearTerrain();
        }
        
        [ContextMenu("Clear All Water Planes")]
        private void DebugClearWaterPlanes()
        {
            ClearAllWaterPlanes();
        }

        [ContextMenu("Print Terrain Info")]
        private void DebugPrintTerrainInfo()
        {
            Debug.Log("=== Terrain Info ===");
            Debug.Log($"Size: {terrainData.size}");
            Debug.Log($"Heightmap Resolution: {terrainData.heightmapResolution}");
            Debug.Log($"Alphamap Resolution: {terrainData.alphamapResolution}");
            Debug.Log($"Terrain Layers: {terrainData.terrainLayers?.Length ?? 0}");
        }
        #endregion
    }
}
