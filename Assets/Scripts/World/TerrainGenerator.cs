using UnityEngine;
using Hearthbound.Utilities;

namespace Hearthbound.World
{
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
        #endregion

        #region Terrain Settings
        [Header("Terrain Size")]
        [SerializeField] private int terrainWidth = 1000;
        [SerializeField] private int terrainLength = 1000;
        [SerializeField] private int terrainHeight = 600;
        [SerializeField] private int heightmapResolution = 513;
        
        [Header("Height Generation")]
        [SerializeField] private float baseHeight = 50f;
        [SerializeField] private float hillHeight = 100f;
        [SerializeField] private float mountainHeight = 200f;
        [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Texture Splatting")]
        [SerializeField] private float grassHeight = 0.3f;
        [SerializeField] private float rockHeight = 0.6f;
        [SerializeField] private float snowHeight = 0.8f;
        [SerializeField] private float steepSlope = 45f;
        
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
                GenerateTerrain(WorldSeedManager.Instance.CurrentSeed);
            }
        }
        #endregion

        #region Initialization
        private void InitializeTerrainData()
        {
            if (terrain.terrainData == null)
            {
                terrainData = new TerrainData();
                terrain.terrainData = terrainData;
            }
            else
            {
                terrainData = terrain.terrainData;
            }

            // Set terrain size
            terrainData.heightmapResolution = heightmapResolution;
            terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
            
            Debug.Log($"🗻 Terrain initialized: {terrainWidth}x{terrainLength}, Height: {terrainHeight}");
        }
        #endregion

        #region Terrain Generation
        public void GenerateTerrain(int seed)
        {
            Debug.Log($"🗻 Generating terrain with seed: {seed}");
            
            Random.InitState(seed);
            
            // Generate heightmap
            GenerateHeightmap(seed);
            
            // Apply texture splatmap
            GenerateSplatmap(seed);
            
            // Add detail layers (grass, rocks)
            GenerateDetailLayers(seed);
            
            Debug.Log("✅ Terrain generation complete!");
        }

        private void GenerateHeightmap(int seed)
        {
            Debug.Log("  Generating heightmap...");
            
            int width = terrainData.heightmapResolution;
            int height = terrainData.heightmapResolution;
            float[,] heights = new float[width, height];

            // Calculate world space size per heightmap pixel
            float scaleX = terrainWidth / (float)width;
            float scaleZ = terrainLength / (float)height;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Convert to world space coordinates
                    float worldX = x * scaleX;
                    float worldZ = z * scaleZ;

                    // Generate height using noise
                    float heightValue = NoiseGenerator.GetTerrainHeight(
                        worldX, worldZ, seed,
                        baseHeight, hillHeight, mountainHeight
                    );

                    // Normalize to 0-1 range for terrain
                    heightValue = heightValue / terrainHeight;
                    
                    // Apply height curve for more control
                    heightValue = heightCurve.Evaluate(heightValue);
                    
                    // Clamp to valid range
                    heightValue = Mathf.Clamp01(heightValue);

                    heights[z, x] = heightValue;
                }
            }

            terrainData.SetHeights(0, 0, heights);
            Debug.Log("  ✅ Heightmap generated");
        }

        private void GenerateSplatmap(int seed)
        {
            Debug.Log("  Generating texture splatmap...");
            
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
                    // Get height at this position
                    float normX = x / (float)alphamapWidth;
                    float normZ = z / (float)alphamapHeight;
                    float height = terrainData.GetHeight(
                        Mathf.RoundToInt(normX * terrainData.heightmapResolution),
                        Mathf.RoundToInt(normZ * terrainData.heightmapResolution)
                    ) / terrainHeight;

                    // Get slope at this position
                    float slope = terrainData.GetSteepness(normX, normZ);

                    // Calculate texture weights based on height and slope
                    float[] weights = new float[numTextures];
                    
                    // Texture 0: Grass (low elevation, gentle slope)
                    if (height < grassHeight && slope < steepSlope)
                        weights[0] = 1f;
                    
                    // Texture 1: Rock (steep slopes or medium elevation)
                    if (slope > steepSlope || (height >= grassHeight && height < rockHeight))
                        weights[1] = 1f;
                    
                    // Texture 2: Snow (high elevation)
                    if (height >= snowHeight)
                        weights[2] = 1f;
                    
                    // Texture 3: Dirt (transition areas)
                    if (height >= grassHeight && height < rockHeight && slope < steepSlope)
                        weights[3] = 0.5f;

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
            Debug.Log("  ✅ Splatmap generated");
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
            
            Debug.Log("  ⚠️ Detail layers not yet implemented (add grass/rock prefabs)");
        }

        private void CreateDefaultTerrainLayers()
        {
            Debug.Log("  Creating default terrain layers...");
            
            // Create 4 basic terrain layers
            TerrainLayer[] layers = new TerrainLayer[4];
            
            // Layer 0: Grass
            layers[0] = new TerrainLayer();
            layers[0].diffuseTexture = Texture2D.whiteTexture; // Replace with actual grass texture
            layers[0].tileSize = new Vector2(15, 15);
            
            // Layer 1: Rock
            layers[1] = new TerrainLayer();
            layers[1].diffuseTexture = Texture2D.grayTexture; // Replace with actual rock texture
            layers[1].tileSize = new Vector2(15, 15);
            
            // Layer 2: Snow
            layers[2] = new TerrainLayer();
            layers[2].diffuseTexture = Texture2D.whiteTexture; // Replace with actual snow texture
            layers[2].tileSize = new Vector2(15, 15);
            
            // Layer 3: Dirt
            layers[3] = new TerrainLayer();
            layers[3].diffuseTexture = Texture2D.grayTexture; // Replace with actual dirt texture
            layers[3].tileSize = new Vector2(15, 15);

            terrainData.terrainLayers = layers;
            Debug.Log("  ✅ Default terrain layers created (replace with actual textures)");
        }
        #endregion

        #region Clear Terrain
        public void ClearTerrain()
        {
            Debug.Log("🗑️ Clearing terrain...");
            
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
            Debug.Log("✅ Terrain cleared");
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
        #endregion

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
