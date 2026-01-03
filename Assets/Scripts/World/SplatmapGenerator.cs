using UnityEngine;
using System.Collections.Generic;
using Hearthbound.Utilities;

namespace Hearthbound.World
{
    /// <summary>
    /// Generates terrain texture splatmaps based on biome systems
    /// </summary>
    public class SplatmapGenerator
    {
        // Biome height thresholds
        public float WaterHeight { get; set; } = 0.05f;
        public float GrassHeight { get; set; } = 0.3f;
        public float RockHeight { get; set; } = 0.6f;
        public float SnowHeight { get; set; } = 0.7f;
        public float SteepSlope { get; set; } = 45f;

        // Biome system configuration
        public BiomeCollection BiomeCollection { get; set; }
        public bool UseScriptableObjectBiomes { get; set; } = true;
        public bool UseAdvancedBiomes { get; set; } = true;
        public float BiomeBlendDistance { get; set; } = 0.1f;

        // Noise parameters for temperature/moisture
        public float MoistureFrequency { get; set; } = 0.003f;
        public float TemperatureFrequency { get; set; } = 0.002f;

        // Forest thresholds
        public float ForestMoistureMin { get; set; } = 0.5f;
        public float ForestMoistureMax { get; set; } = 1.0f;
        public float ForestTemperatureMin { get; set; } = 0.3f;
        public float ForestTemperatureMax { get; set; } = 0.7f;

        // Water biome control
        public bool DisableWaterBiomes { get; set; } = true;

        // Terrain dimensions (needed for noise generation)
        public int TerrainWidth { get; set; } = 1000;
        public int TerrainLength { get; set; } = 1000;
        public int TerrainHeight { get; set; } = 600;

        /// <summary>
        /// Generate and apply splatmap to terrain data
        /// </summary>
        public void GenerateSplatmap(int seed, TerrainData terrainData)
        {
            Debug.Log("  Generating texture splatmap...");

            // Use ScriptableObject biome system if available
            if (UseScriptableObjectBiomes && BiomeCollection != null && BiomeCollection.biomes != null && BiomeCollection.biomes.Length > 0)
            {
                GenerateSplatmapFromBiomeCollection(seed, terrainData);
                return;
            }

            // Fall back to legacy system
            Debug.LogWarning("BiomeCollection not assigned or empty! Using legacy biome system. Please assign a BiomeCollection asset or create one via: Hearthbound > Create Default Biome Collection");
            GenerateSplatmapLegacy(seed, terrainData);
        }

        private void GenerateSplatmapFromBiomeCollection(int seed, TerrainData terrainData)
        {
            Debug.Log($"  Using BiomeCollection for texture splatmap... (Blend Factor: {BiomeCollection.globalBlendFactor}, Biomes: {BiomeCollection.biomes.Length})");

            // Get terrain layers from biome collection
            var terrainLayers = BiomeCollection.GetAllTerrainLayers();
            if (terrainLayers == null || terrainLayers.Count == 0)
            {
                Debug.LogWarning("BiomeCollection has no terrain layers! Falling back to default.");
                CreateDefaultTerrainLayers(terrainData);
                GenerateSplatmapLegacy(seed, terrainData);
                return;
            }

            // Build mapping from biomes to terrain layer indices
            var biomeToLayerIndex = BiomeCollection.GetBiomeToLayerIndexMap();

            Debug.Log($"  Generated {terrainLayers.Count} terrain layers from {BiomeCollection.biomes.Length} biomes");

            // Set terrain layers
            terrainData.terrainLayers = terrainLayers.ToArray();

            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            int numTextures = terrainLayers.Count;

            float[,,] splatmapData = new float[alphamapWidth, alphamapHeight, numTextures];

            // Generate temperature and humidity maps ONCE for the entire terrain
            Debug.Log("  Generating temperature map (latitude-based, NOT height-based)...");
            float[,] temperatureMap = GenerateTemperatureMap(seed, terrainData, alphamapWidth, alphamapHeight);
            Debug.Log("  Generating humidity map (noise-based, NOT temperature-coupled)...");
            float[,] humidityMap = GenerateHumidityMap(seed, terrainData, alphamapWidth, alphamapHeight);

            // Log sample temperature/humidity values to verify they vary correctly
            int sampleX = alphamapWidth / 2;
            int sampleZ1 = alphamapHeight / 4;  // Edge (should be cooler)
            int sampleZ2 = alphamapHeight / 2;  // Center (should be warmer)
            int sampleZ3 = alphamapHeight * 3 / 4; // Edge (should be cooler)

            float height1 = terrainData.GetHeight(Mathf.RoundToInt(sampleX / (float)alphamapWidth * terrainData.heightmapResolution),
                                                  Mathf.RoundToInt(sampleZ1 / (float)alphamapHeight * terrainData.heightmapResolution)) / TerrainHeight;
            float height2 = terrainData.GetHeight(Mathf.RoundToInt(sampleX / (float)alphamapWidth * terrainData.heightmapResolution),
                                                  Mathf.RoundToInt(sampleZ2 / (float)alphamapHeight * terrainData.heightmapResolution)) / TerrainHeight;
            float height3 = terrainData.GetHeight(Mathf.RoundToInt(sampleX / (float)alphamapWidth * terrainData.heightmapResolution),
                                                  Mathf.RoundToInt(sampleZ3 / (float)alphamapHeight * terrainData.heightmapResolution)) / TerrainHeight;

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
                    ) / TerrainHeight;

                    // Get slope at this position
                    float slope = terrainData.GetSteepness(normX, normZ);

                    // Use pre-generated temperature and humidity maps
                    float temperature = temperatureMap[x, z];
                    float moisture = humidityMap[x, z];

                    // Calculate biome weights using BiomeCollection
                    var biomeWeights = BiomeCollection.CalculateBiomeWeights(moisture, temperature, height, slope);

                    // Check if this is a mountain area OR elevated area
                    bool isMountainArea = slope > SteepSlope || height > RockHeight || height > 0.2f;

                    // Remove water biome from mountain areas
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
                    else if (height < WaterHeight && !DisableWaterBiomes)
                    {
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
                            biomeWeights[waterBiome] = 1.0f;
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
                                biomeWeights[otherBiome] *= 0.1f;
                            }
                        }
                    }
                    // If water biomes are disabled, remove water biome from low areas too
                    else if (height < WaterHeight && DisableWaterBiomes)
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

                    // Debug logging: Sample a few pixels
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

            terrainData.SetAlphamaps(0, 0, splatmapData);
            Debug.Log("  Splatmap applied from BiomeCollection");
        }

        /// <summary>
        /// Generate temperature map independently from height
        /// Uses latitude-like gradient + noise for variation
        /// </summary>
        public float[,] GenerateTemperatureMap(int seed, TerrainData terrainData, int width, int height)
        {
            float[,] tempMap = new float[width, height];

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normX = x / (float)width;
                    float normZ = z / (float)height;
                    float worldX = normX * TerrainWidth;
                    float worldZ = normZ * TerrainLength;

                    // Latitude-like gradient: warmer at center (equator), cooler at edges (poles)
                    float latitudeGradient = 1f - Mathf.Abs((normZ - 0.5f) * 2f);
                    latitudeGradient = Mathf.Pow(latitudeGradient, 1.5f);

                    // Add noise variation for local temperature variations
                    float temperatureNoise = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed + 10000, TemperatureFrequency);

                    // Combine latitude gradient (70%) and noise (30%)
                    float temperature = latitudeGradient * 0.7f + temperatureNoise * 0.3f;

                    // Optional: Slight altitude influence (higher = cooler, but not dominant)
                    float terrainHeight = terrainData.GetHeight(
                        Mathf.RoundToInt(normX * terrainData.heightmapResolution),
                        Mathf.RoundToInt(normZ * terrainData.heightmapResolution)
                    ) / TerrainHeight;

                    // Reduce temperature slightly at high elevations (max 20% reduction)
                    float altitudeEffect = terrainHeight * 0.2f;
                    temperature = Mathf.Clamp01(temperature - altitudeEffect);

                    tempMap[x, z] = temperature;
                }
            }

            return tempMap;
        }

        /// <summary>
        /// Generate humidity map independently from height
        /// Uses noise patterns for rainfall + slight height influence
        /// </summary>
        public float[,] GenerateHumidityMap(int seed, TerrainData terrainData, int width, int height)
        {
            float[,] humidMap = new float[width, height];

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normX = x / (float)width;
                    float normZ = z / (float)height;
                    float worldX = normX * TerrainWidth;
                    float worldZ = normZ * TerrainLength;

                    // Base humidity from noise (rainfall patterns)
                    float baseHumidity = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed + 20000, MoistureFrequency);

                    // Add second noise layer for more complex patterns
                    float humidityDetail = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed + 30000, MoistureFrequency * 2f) * 0.3f;

                    // Combine base and detail
                    float humidity = baseHumidity * 0.7f + humidityDetail;

                    // Optional: Slight height influence (lower elevations slightly more humid)
                    float terrainHeight = terrainData.GetHeight(
                        Mathf.RoundToInt(normX * terrainData.heightmapResolution),
                        Mathf.RoundToInt(normZ * terrainData.heightmapResolution)
                    ) / TerrainHeight;

                    // Boost humidity slightly at low elevations (max 15% boost)
                    float heightBoost = (1f - terrainHeight) * 0.15f;
                    humidity = Mathf.Clamp01(humidity + heightBoost);

                    humidMap[x, z] = humidity;
                }
            }

            return humidMap;
        }

        private void GenerateSplatmapLegacy(int seed, TerrainData terrainData)
        {
            Debug.Log("  Using legacy biome system for texture splatmap...");

            // Ensure we have terrain layers
            if (terrainData.terrainLayers == null || terrainData.terrainLayers.Length == 0)
            {
                CreateDefaultTerrainLayers(terrainData);
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
                    float worldX = normX * TerrainWidth;
                    float worldZ = normZ * TerrainLength;

                    // Get height at this position (normalized 0-1)
                    float height = terrainData.GetHeight(
                        Mathf.RoundToInt(normX * terrainData.heightmapResolution),
                        Mathf.RoundToInt(normZ * terrainData.heightmapResolution)
                    ) / TerrainHeight;

                    // Get slope at this position
                    float slope = terrainData.GetSteepness(normX, normZ);

                    // Calculate texture weights
                    float[] weights = new float[numTextures];

                    if (UseAdvancedBiomes)
                    {
                        weights = CalculateAdvancedBiomeWeights(worldX, worldZ, height, slope, seed);
                    }
                    else
                    {
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

        private float[] CalculateSimpleBiomeWeights(float height, float slope)
        {
            float[] weights = new float[6];

            // Layer 0: Water - only if water biomes are enabled
            if (height < WaterHeight && !DisableWaterBiomes)
                weights[0] = 1f;

            // Layer 1: Plains/Grass
            else if (height < GrassHeight && slope < SteepSlope)
                weights[1] = 1f;

            // Layer 3: Rock/Mountains
            else if (slope > SteepSlope || (height >= RockHeight && height < SnowHeight))
                weights[3] = 1f;

            // Layer 4: Snow
            else if (height >= SnowHeight)
                weights[4] = 1f;

            // Layer 5: Dirt (transition areas)
            else
                weights[5] = 0.5f;

            return weights;
        }

        private float[] CalculateAdvancedBiomeWeights(float worldX, float worldZ, float height, float slope, int seed)
        {
            float[] weights = new float[6];

            // Get moisture and temperature values
            float baseTemperature = 1f - height;
            float temperatureNoise = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed + 10000, TemperatureFrequency) * 0.2f - 0.1f;
            float temperature = Mathf.Clamp01(baseTemperature + temperatureNoise);

            float baseHumidity = NoiseGenerator.GetBiomeValue(worldX, worldZ, seed, MoistureFrequency);
            float heightHumidityBoost = Mathf.Pow(1f - height, 2f) * 0.4f;
            float rawHumidity = Mathf.Clamp01(baseHumidity + heightHumidityBoost);

            // Apply temperature-based humidity multiplier
            float gammaOffset = 0.2f;
            float gammaValue = 1.0f;
            float tempHumidityMultiplier = gammaOffset + (1f - gammaOffset) * Mathf.Pow(temperature, gammaValue);
            float moisture = Mathf.Clamp01(rawHumidity * tempHumidityMultiplier);

            // Layer 0: Water - only if water biomes are enabled
            if (height < WaterHeight && !DisableWaterBiomes)
            {
                weights[0] = 1f;
                return weights;
            }

            // Layer 4: Snow
            if (height >= SnowHeight)
            {
                float snowWeight = CalculateBiomeWeight(height, slope, moisture, temperature,
                    heightRange: new Vector2(SnowHeight, 1f),
                    slopeRange: new Vector2(0f, 90f),
                    moistureRange: new Vector2(0f, 1f),
                    temperatureRange: new Vector2(0f, 0.3f));
                weights[4] = snowWeight;

                if (snowWeight < 0.9f && slope > SteepSlope)
                    weights[3] = (1f - snowWeight) * 0.5f;
            }
            // Layer 3: Rock/Mountains
            else if (slope > SteepSlope || height >= RockHeight)
            {
                float rockWeight = CalculateBiomeWeight(height, slope, moisture, temperature,
                    heightRange: new Vector2(RockHeight, SnowHeight),
                    slopeRange: new Vector2(SteepSlope, 90f),
                    moistureRange: new Vector2(0f, 0.5f),
                    temperatureRange: new Vector2(0.2f, 0.8f));

                if (slope > SteepSlope)
                    rockWeight = Mathf.Max(rockWeight, 0.8f);

                weights[3] = rockWeight;

                if (rockWeight < 0.9f)
                    weights[5] = (1f - rockWeight) * 0.3f;
            }
            // Layer 2: Forest
            else if (moisture >= ForestMoistureMin && moisture <= ForestMoistureMax &&
                     temperature >= ForestTemperatureMin && temperature <= ForestTemperatureMax &&
                     height >= WaterHeight && height < RockHeight && slope < SteepSlope)
            {
                float forestWeight = CalculateBiomeWeight(height, slope, moisture, temperature,
                    heightRange: new Vector2(WaterHeight, RockHeight),
                    slopeRange: new Vector2(0f, SteepSlope * 0.7f),
                    moistureRange: new Vector2(ForestMoistureMin, ForestMoistureMax),
                    temperatureRange: new Vector2(ForestTemperatureMin, ForestTemperatureMax));
                weights[2] = forestWeight;

                weights[1] = (1f - forestWeight) * 0.5f;
            }
            // Layer 1: Plains/Grass
            else if (height < GrassHeight && slope < SteepSlope)
            {
                float plainsWeight = CalculateBiomeWeight(height, slope, moisture, temperature,
                    heightRange: new Vector2(WaterHeight, GrassHeight),
                    slopeRange: new Vector2(0f, SteepSlope),
                    moistureRange: new Vector2(0.2f, 0.7f),
                    temperatureRange: new Vector2(0.4f, 0.8f));
                weights[1] = plainsWeight;

                weights[5] = (1f - plainsWeight) * 0.3f;
            }
            // Layer 5: Dirt
            else
            {
                weights[5] = 0.7f;
                if (height < RockHeight)
                    weights[1] = 0.3f;
                else
                    weights[3] = 0.3f;
            }

            return weights;
        }

        private float CalculateBiomeWeight(float height, float slope, float moisture, float temperature,
            Vector2 heightRange, Vector2 slopeRange, Vector2 moistureRange, Vector2 temperatureRange)
        {
            float heightMatch = GetFactorMatch(height, heightRange.x, heightRange.y);
            float slopeMatch = GetFactorMatch(slope / 90f, slopeRange.x / 90f, slopeRange.y / 90f);
            float moistureMatch = GetFactorMatch(moisture, moistureRange.x, moistureRange.y);
            float temperatureMatch = GetFactorMatch(temperature, temperatureRange.x, temperatureRange.y);

            float combinedMatch = heightMatch * slopeMatch * moistureMatch * temperatureMatch;

            return Mathf.SmoothStep(0f, 1f, combinedMatch);
        }

        private float GetFactorMatch(float value, float min, float max)
        {
            if (value < min)
            {
                float distance = min - value;
                float falloffRange = BiomeBlendDistance;
                return Mathf.Clamp01(1f - (distance / falloffRange));
            }
            else if (value > max)
            {
                float distance = value - max;
                float falloffRange = BiomeBlendDistance;
                return Mathf.Clamp01(1f - (distance / falloffRange));
            }
            else
            {
                return 1f;
            }
        }

        private void CreateDefaultTerrainLayers(TerrainData terrainData)
        {
            Debug.Log("  Creating default terrain layers with distinct biome colors...");

            TerrainLayer[] layers = new TerrainLayer[6];

            // Layer 0: Water/Beach - Bright Cyan/Blue
            layers[0] = new TerrainLayer();
            layers[0].diffuseTexture = CreateColoredTexture(new Color(0.1f, 0.5f, 0.9f), "Water");
            layers[0].tileSize = new Vector2(15, 15);
            layers[0].diffuseTexture.name = "WaterTexture";

            // Layer 1: Plains/Grass - Yellow-Green
            layers[1] = new TerrainLayer();
            layers[1].diffuseTexture = CreateColoredTexture(new Color(0.6f, 0.9f, 0.3f), "Plains");
            layers[1].tileSize = new Vector2(15, 15);
            layers[1].diffuseTexture.name = "PlainsTexture";

            // Layer 2: Forest - Deep Saturated Green
            layers[2] = new TerrainLayer();
            layers[2].diffuseTexture = CreateColoredTexture(new Color(0.05f, 0.5f, 0.15f), "Forest");
            layers[2].tileSize = new Vector2(15, 15);
            layers[2].diffuseTexture.name = "ForestTexture";

            // Layer 3: Rock/Mountains - Tan/Brown
            layers[3] = new TerrainLayer();
            layers[3].diffuseTexture = CreateColoredTexture(new Color(0.7f, 0.6f, 0.4f), "Rock");
            layers[3].tileSize = new Vector2(15, 15);
            layers[3].diffuseTexture.name = "RockTexture";

            // Layer 4: Snow - Pure White
            layers[4] = new TerrainLayer();
            layers[4].diffuseTexture = CreateColoredTexture(new Color(1.0f, 1.0f, 1.0f), "Snow");
            layers[4].tileSize = new Vector2(15, 15);
            layers[4].diffuseTexture.name = "SnowTexture";

            // Layer 5: Dirt - Dark Brown
            layers[5] = new TerrainLayer();
            layers[5].diffuseTexture = CreateColoredTexture(new Color(0.3f, 0.2f, 0.1f), "Dirt");
            layers[5].tileSize = new Vector2(15, 15);
            layers[5].diffuseTexture.name = "DirtTexture";

            terrainData.terrainLayers = layers;
            Debug.Log("  Terrain layers created with distinct colors: Water (Cyan), Plains (Yellow-Green), Forest (Deep Green), Rock (Tan), Snow (White), Dirt (Dark Brown)");
        }

        private Texture2D CreateColoredTexture(Color color, string name)
        {
            int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

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
                    float variation = (noise - 0.5f) * 0.1f;
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
    }
}
