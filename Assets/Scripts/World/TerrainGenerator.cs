using System.Collections.Generic;
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
        [SerializeField] private float baseHeight = 80f; // Base terrain height (plains/lowlands) - increased to prevent everything being underwater
        [SerializeField] private float hillHeight = 60f; // Gentle hills for variation
        [SerializeField] private float mountainHeight = 200f; // Clustered mountains
        [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Texture Splatting - Height Thresholds")]
        [SerializeField] private float waterHeight = 0.1f; // Below this = water/beach
        [SerializeField] private float grassHeight = 0.3f; // Plains/grass biome
        [SerializeField] private float rockHeight = 0.6f; // Mountains start
        [SerializeField] private float snowHeight = 0.8f; // Snow on peaks
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
            }
            else
            {
                terrainData = terrain.terrainData;
            }

            // Set terrain size
            terrainData.heightmapResolution = heightmapResolution;
            terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);
            
            // Ensure TerrainCollider uses the same TerrainData
            TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
            if (terrainCollider != null && terrainCollider.terrainData != terrainData)
            {
                terrainCollider.terrainData = terrainData;
                Debug.Log("  ✅ Synced TerrainCollider with TerrainData");
            }
            
            Debug.Log($"🗻 Terrain initialized: {terrainWidth}x{terrainLength}, Height: {terrainHeight}");
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
            
            // Use ScriptableObject biome system if available
            if (useScriptableObjectBiomes && biomeCollection != null && biomeCollection.biomes != null && biomeCollection.biomes.Length > 0)
            {
                GenerateSplatmapFromBiomeCollection(seed);
                return;
            }
            
            // Fall back to legacy system
            Debug.LogWarning("⚠️ BiomeCollection not assigned or empty! Using legacy biome system. Please assign a BiomeCollection asset or create one via: Hearthbound > Create Default Biome Collection");
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
            // This is crucial because GetAllTerrainLayers() only includes biomes with valid terrain layers
            var biomeToLayerIndex = biomeCollection.GetBiomeToLayerIndexMap();
            
            Debug.Log($"  Generated {terrainLayers.Count} terrain layers from {biomeCollection.biomes.Length} biomes");
            
            // Set terrain layers
            terrainData.terrainLayers = terrainLayers.ToArray();

            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            int numTextures = terrainLayers.Count;

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

                    // Get moisture and temperature values
                    // Per best practices: Temperature drops with altitude, humidity rises near water/low altitude
                    
                    // Temperature decreases with height (realistic: higher elevation = lower temperature)
                    // Base temperature from height: 1.0 at sea level, 0.0 at max height
                    // Add noise variation for localized temperature variations
                    float baseTemperature = 1f - height; // Lower height = higher temp, higher height = lower temp
                    float temperatureNoise = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed + 10000, temperatureFrequency) * 0.2f - 0.1f; // ±0.1 variation
                    float temperature = baseTemperature + temperatureNoise;
                    temperature = Mathf.Clamp01(temperature);
                    
                    // Humidity: Increases near water/low altitude, affected by temperature and noise patterns
                    // Per Worldengine/Holdridge model: Humidity capacity decreases with temperature (colder = less moisture capacity)
                    // Base humidity from noise (rainfall patterns)
                    float baseHumidity = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed, moistureFrequency);
                    // Increase humidity at low elevations (near water/sea level) - stronger at very low elevations
                    float heightHumidityBoost = Mathf.Pow(1f - height, 2f) * 0.4f; // Stronger boost at very low elevations
                    
                    // Combine base humidity and height boost
                    float rawHumidity = baseHumidity + heightHumidityBoost;
                    rawHumidity = Mathf.Clamp01(rawHumidity);
                    
                    // Apply temperature-based humidity transformation (per Worldengine model)
                    // Colder regions can't hold as much moisture - use gamma-like function
                    // Function ranges from offset (0.2) to 1.0 as temperature goes from 0 to 1
                    float gammaOffset = 0.2f; // Minimum humidity multiplier at coldest temperatures
                    float gammaValue = 1.0f; // Linear curve (can be adjusted for different curves)
                    
                    // Calculate temperature-based humidity multiplier
                    // f(T) = offset + (1 - offset) * T^gamma
                    // This ensures cold regions have lower max humidity capacity
                    float tempHumidityMultiplier = gammaOffset + (1f - gammaOffset) * Mathf.Pow(temperature, gammaValue);
                    
                    // Apply the temperature-based multiplier to humidity
                    // This prevents unrealistic combinations like "Polar" + "Superhumid"
                    float moisture = rawHumidity * tempHumidityMultiplier;
                    moisture = Mathf.Clamp01(moisture);

                    // Calculate biome weights using BiomeCollection (with slope)
                    var biomeWeights = biomeCollection.CalculateBiomeWeights(moisture, temperature, height, slope);

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
                    // Include both low and high elevation samples
                    bool isSamplePoint = (x == alphamapWidth / 4 && z == alphamapHeight / 4) || 
                                        (x == alphamapWidth / 2 && z == alphamapHeight / 2) ||
                                        (x == alphamapWidth * 3 / 4 && z == alphamapHeight * 3 / 4);
                    
                    // Also sample high elevation points to check for Rock/Snow
                    bool isHighElevationSample = height > 0.5f && (x % (alphamapWidth / 8) == 0 && z % (alphamapHeight / 8) == 0);
                    
                    if (isSamplePoint || isHighElevationSample)
                    {
                        string biomeInfo = $"Sample at ({x},{z}): height={height:F3}, temp={temperature:F3}, moisture={moisture:F3}\n";
                        biomeInfo += $"  Biome weights calculated: {biomeWeights.Count}\n";
                        foreach (var kvp in biomeWeights)
                        {
                            if (biomeToLayerIndex.ContainsKey(kvp.Key))
                            {
                                int idx = biomeToLayerIndex[kvp.Key];
                                float normalizedWeight = totalWeight > 0.001f ? kvp.Value / totalWeight : 0f;
                                biomeInfo += $"  {kvp.Key.biomeName} (idx {idx}): raw={kvp.Value:F6}, normalized={normalizedWeight:F3}, temp={kvp.Key.temperature:F2}, humid={kvp.Key.humidity:F2}\n";
                            }
                        }
                        biomeInfo += $"  Total weight: {totalWeight:F6}\n";
                        biomeInfo += $"  Final normalized weights: ";
                        if (totalWeight > 0.001f)
                        {
                            for (int i = 0; i < numTextures; i++)
                            {
                                if (weights[i] / totalWeight > 0.01f) // Only show weights > 1%
                                    biomeInfo += $"layer[{i}]={weights[i]/totalWeight:F3} ";
                            }
                        }
                        else
                        {
                            biomeInfo += "NONE (using fallback)";
                        }
                        // Split into multiple log lines for better readability
                        Debug.Log(biomeInfo);
                        // Also log a summary line for quick reference
                        string summary = $"  Summary: ";
                        foreach (var kvp in biomeWeights)
                        {
                            if (biomeToLayerIndex.ContainsKey(kvp.Key))
                            {
                                float normalizedWeight = totalWeight > 0.001f ? kvp.Value / totalWeight : 0f;
                                if (normalizedWeight > 0.05f) // Only show biomes with >5% weight
                                {
                                    summary += $"{kvp.Key.biomeName}={normalizedWeight:P0} ";
                                }
                            }
                        }
                        Debug.Log(summary);
                    }

                    // Normalize weights (as per article: weights don't necessarily sum to 1, but Unity needs normalized)
                    if (totalWeight > 0.001f)
                    {
                        for (int i = 0; i < numTextures; i++)
                        {
                            weights[i] /= totalWeight;
                        }
                    }
                    else
                    {
                        // No biome weights calculated - fall back to height-based biome selection
                        if (numTextures >= 5)
                        {
                            // Distribute based on height: water at bottom, snow at top
                            if (height < 0.1f)
                                weights[0] = 1f; // Water
                            else if (height < 0.3f)
                                weights[1] = 1f; // Plains
                            else if (height < 0.6f)
                                weights[2] = 1f; // Forest
                            else if (height < 0.8f)
                                weights[3] = 1f; // Rock
                            else
                                weights[4] = 1f; // Snow
                        }
                        else if (numTextures > 0)
                        {
                            weights[0] = 1f; // Default to first biome
                        }
                    }

                    // Apply weights
                    for (int i = 0; i < numTextures; i++)
                    {
                        splatmapData[x, z, i] = weights[i];
                    }
                }
            }

            terrainData.SetAlphamaps(0, 0, splatmapData);
            Debug.Log($"  ✅ Splatmap generated using {numTextures} biomes from BiomeCollection");
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
            Debug.Log("  ✅ Splatmap generated (legacy system)");
        }

        /// <summary>
        /// Calculate biome weights using simple height/slope system
        /// </summary>
        private float[] CalculateSimpleBiomeWeights(float height, float slope)
        {
            float[] weights = new float[6];
            
            // Layer 0: Water (very low elevation)
            if (height < waterHeight)
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

            // Layer 0: Water (very low elevation, high moisture areas)
            if (height < waterHeight)
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
            
            Debug.Log("  ⚠️ Detail layers not yet implemented (add grass/rock prefabs)");
        }

        private void CreateDefaultTerrainLayers()
        {
            Debug.Log("  Creating default terrain layers with biome colors...");
            
            // Create 6 terrain layers for different biomes
            TerrainLayer[] layers = new TerrainLayer[6];
            
            // Layer 0: Water/Beach (blue/sand)
            layers[0] = new TerrainLayer();
            layers[0].diffuseTexture = CreateColoredTexture(new Color(0.2f, 0.4f, 0.7f), "Water"); // Blue
            layers[0].tileSize = new Vector2(15, 15);
            layers[0].diffuseTexture.name = "WaterTexture";
            
            // Layer 1: Plains/Grass (green)
            layers[1] = new TerrainLayer();
            layers[1].diffuseTexture = CreateColoredTexture(new Color(0.3f, 0.7f, 0.3f), "Plains"); // Bright green
            layers[1].tileSize = new Vector2(15, 15);
            layers[1].diffuseTexture.name = "PlainsTexture";
            
            // Layer 2: Forest (dark green)
            layers[2] = new TerrainLayer();
            layers[2].diffuseTexture = CreateColoredTexture(new Color(0.1f, 0.4f, 0.1f), "Forest"); // Dark green
            layers[2].tileSize = new Vector2(15, 15);
            layers[2].diffuseTexture.name = "ForestTexture";
            
            // Layer 3: Rock/Mountains (grey)
            layers[3] = new TerrainLayer();
            layers[3].diffuseTexture = CreateColoredTexture(new Color(0.5f, 0.5f, 0.5f), "Rock"); // Grey
            layers[3].tileSize = new Vector2(15, 15);
            layers[3].diffuseTexture.name = "RockTexture";
            
            // Layer 4: Snow (white/light blue)
            layers[4] = new TerrainLayer();
            layers[4].diffuseTexture = CreateColoredTexture(new Color(0.9f, 0.95f, 1f), "Snow"); // Light blue-white
            layers[4].tileSize = new Vector2(15, 15);
            layers[4].diffuseTexture.name = "SnowTexture";
            
            // Layer 5: Dirt (brown - transition)
            layers[5] = new TerrainLayer();
            layers[5].diffuseTexture = CreateColoredTexture(new Color(0.4f, 0.3f, 0.2f), "Dirt"); // Brown
            layers[5].tileSize = new Vector2(15, 15);
            layers[5].diffuseTexture.name = "DirtTexture";

            terrainData.terrainLayers = layers;
            Debug.Log("  ✅ Terrain layers created: Water, Plains, Forest, Rock, Snow, Dirt");
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
            Debug.Log("🗑️ Clearing terrain...");
            
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
