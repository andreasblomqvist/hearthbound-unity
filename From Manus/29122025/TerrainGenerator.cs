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