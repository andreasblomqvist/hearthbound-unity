// This is a PARTIAL file showing only the fixed GenerateSplatmapFromBiomeCollection method
// Replace the existing method in your TerrainGenerator.cs with this version

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
    float[,] temperatureMap = GenerateTemperatureMap(alphamapWidth, alphamapHeight, seed);
    float[,] humidityMap = GenerateHumidityMap(alphamapWidth, alphamapHeight, seed);

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
    Debug.Log("  ✅ Splatmap applied from BiomeCollection");
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
