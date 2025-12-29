using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound.World
{
    /// <summary>
    /// Biome Collection ScriptableObject
    /// Defines which biomes exist in the world and how they blend together
    /// Based on concepts from: https://medium.com/@mrrsff/procedural-world-generation-with-biomes-in-unity-a474e11ff0b7
    /// </summary>
    [CreateAssetMenu(fileName = "NewBiomeCollection", menuName = "Hearthbound/Biome Collection")]
    public class BiomeCollection : ScriptableObject
    {
        [Header("Biome Collection")]
        [Tooltip("List of biomes available in this world")]
        public BiomeData[] biomes = new BiomeData[0];
        
        [Header("Global Blend Settings")]
        [Tooltip("Global biome blend factor - higher values create sharper transitions (1-20)")]
        [Range(1f, 20f)]
        public float globalBlendFactor = 5f;
        
        [Tooltip("If true, uses global blend factor. If false, uses individual biome blend factors")]
        public bool useGlobalBlendFactor = true;

        [Header("Matching Method")]
        [Tooltip("If true, uses lookup table approach (Height/Temp/Humidity ranges). If false, uses distance-based matching.")]
        public bool useLookupTable = true;
        
        /// <summary>
        /// Calculate biome weights for a given point
        /// Returns dictionary of biome -> weight (weights are NOT normalized to 1.0)
        /// 
        /// Lookup Table Approach (recommended):
        /// - Each biome has Height, Temperature, and Humidity ranges
        /// - Calculates match score (0-1) based on how well all factors fit within ranges
        /// - Uses smooth blending between biomes
        /// 
        /// Distance-Based Approach (legacy):
        /// 1. Calculate match value (distance): temperatureMatch + humidityMatch (lower is better)
        /// 2. Convert to weight: 1 / (match + 0.1f)
        /// 3. Apply blend factor: Pow(match + 1, biomeBlendingFactor)
        /// </summary>
        public Dictionary<BiomeData, float> CalculateBiomeWeights(float humidity, float temperature, float height, float slope = 0f)
        {
            Dictionary<BiomeData, float> weights = new Dictionary<BiomeData, float>();
            
            if (biomes == null || biomes.Length == 0)
            {
                Debug.LogWarning("BiomeCollection has no biomes defined!");
                return weights;
            }
            
            // Identify biomes by name for height-based adjustments
            BiomeData waterBiome = null;
            BiomeData snowBiome = null;
            BiomeData rockBiome = null;
            
            foreach (BiomeData biome in biomes)
            {
                if (biome == null) continue;
                string nameLower = biome.biomeName.ToLower();
                if (nameLower.Contains("water")) waterBiome = biome;
                if (nameLower.Contains("snow")) snowBiome = biome;
                if (nameLower.Contains("rock") || nameLower.Contains("mountain")) rockBiome = biome;
            }
            
            // Calculate weight for each biome
            foreach (BiomeData biome in biomes)
            {
                if (biome == null) continue;

                // CRITICAL: Hard exclusion for water at high elevations - check FIRST before any calculations
                if (biome == waterBiome && height > 0.05f) // Stricter: 5% instead of 8%
                {
                    continue; // Skip water completely at high elevations
                }

                float match;
                
                // SPECIAL HANDLING: For rock and snow, prioritize height over temperature/humidity
                // This ensures they appear at high elevations even if temp/humidity aren't perfect matches
                if (biome == rockBiome && height >= 0.5f)
                {
                    // Rock at high elevations: base match on height, temp/humidity as modifier
                    // Height-based match: perfect at height 0.5+, improves up to height 1.0
                    float heightMatch = Mathf.Clamp01((height - 0.5f) / 0.5f); // 0 at height 0.5, 1 at height 1.0
                    float baseMatch = 0.8f + (heightMatch * 0.2f); // 0.8 to 1.0 based on height
                    
                    // Apply temp/humidity as a modifier (reduce match slightly if way off)
                    float lookupMatch = biome.GetLookupTableMatch(height, temperature, humidity);
                    float modifier = Mathf.Lerp(0.7f, 1.0f, lookupMatch); // Reduce by up to 30% if temp/humid way off
                    
                    match = baseMatch * modifier;
                    match = 1f - match; // Convert to "distance" (lower is better)
                }
                else if (biome == snowBiome && height >= 0.7f)
                {
                    // Snow at very high elevations: base match on height, temp/humidity as modifier
                    float heightMatch = Mathf.Clamp01((height - 0.7f) / 0.3f); // 0 at height 0.7, 1 at height 1.0
                    float baseMatch = 0.8f + (heightMatch * 0.2f); // 0.8 to 1.0 based on height
                    
                    // Apply temp/humidity as a modifier
                    float lookupMatch = biome.GetLookupTableMatch(height, temperature, humidity);
                    float modifier = Mathf.Lerp(0.7f, 1.0f, lookupMatch);
                    
                    match = baseMatch * modifier;
                    match = 1f - match; // Convert to "distance"
                }
                else if (useLookupTable)
                {
                    // Lookup Table Approach: Use Height, Temperature, Humidity ranges
                    match = biome.GetLookupTableMatch(height, temperature, humidity);
                    
                    // Additional safety check: if lookup table returned 0 (hard cutoff), skip this biome
                    if (match <= 0.0001f)
                    {
                        continue; // Skip biomes that don't match at all
                    }
                    
                    // Invert: higher match score (0-1) = better, but we need "distance" (lower is better)
                    match = 1f - match; // Convert to "distance" (0 = perfect match, 1 = worst match)
                }
                else
                {
                    // Distance-Based Approach: Use single temperature/humidity points
                    match = biome.GetBiomeMatchValue(humidity, temperature);
                }
                
                // Apply height-based adjustments to match value (for both approaches)
                // This ensures snow/rock appear at high elevations, water at low elevations
                float heightAdjustment = 0f;
                
                if (biome == rockBiome)
                {
                    // Rock should be the base for all peaks/mountains
                    // Improve match (reduce match value) at high elevations
                    if (height < 0.4f)
                    {
                        // Strongly penalize rock at low elevations
                        heightAdjustment = (0.4f - height) * 10f; // Up to 4.0 penalty at height 0
                    }
                    else if (height < 0.6f)
                    {
                        // Medium elevations: slight boost
                        float normalizedHeight = (height - 0.4f) / 0.2f; // 0 at height 0.4, 1 at height 0.6
                        heightAdjustment = -normalizedHeight * 1.5f; // 0 at height 0.4, -1.5 at height 0.6
                    }
                    else
                    {
                        // High elevations (peaks): very strongly improve match
                        float normalizedHeight = (height - 0.6f) / 0.4f; // 0 at height 0.6, 1 at height 1.0
                        heightAdjustment = -normalizedHeight * 2f; // 0 at height 0.6, -2.0 at height 1.0
                    }
                }
                else if (biome == snowBiome)
                {
                    // Snow should layer on top of rock at the highest elevations
                    // Improve match (reduce match value) at very high elevations
                    if (height < 0.65f)
                    {
                        // Very strongly penalize snow at low-medium elevations
                        heightAdjustment = (0.65f - height) * 20f; // Up to 13.0 penalty at height 0
                    }
                    else if (height < 0.8f)
                    {
                        // High elevations: improve match
                        float normalizedHeight = (height - 0.65f) / 0.15f; // 0 at height 0.65, 1 at height 0.8
                        heightAdjustment = -normalizedHeight * 2f; // 0 at height 0.65, -2.0 at height 0.8
                    }
                    else
                    {
                        // Very high elevations (peaks): very strongly improve match
                        float normalizedHeight = (height - 0.8f) / 0.2f; // 0 at height 0.8, 1 at height 1.0
                        heightAdjustment = -normalizedHeight * 2f; // 0 at height 0.8, -2.0 at height 1.0
                    }
                }
                else if (biome == waterBiome)
                {
                    // Water should ONLY appear at very low elevations (valleys, sea level)
                    // Boost water at very low elevations only (should only reach here if height <= 0.05)
                    heightAdjustment = -(1f - (height / 0.05f)) * 3f; // -3.0 at height 0, 0 at height 0.05
                }
                
                // Apply height adjustment to match value (lower match = better)
                match = match + heightAdjustment;
                match = Mathf.Max(0.001f, match); // Ensure match is never exactly 0 to avoid division issues
                
                // Step 3: Convert match to weight (inverse relationship)
                // Matches GitHub: weight = 1 / (match + 0.1f)
                float baseWeight = 1f / (match + 0.1f);
                
                // Step 4: Apply blend factor using power function
                // Matches GitHub: weight = Pow(weight + 1, biomeBlendingFactor)
                float blendFactor = useGlobalBlendFactor ? globalBlendFactor : 
                    (biome.humidityBlendFactor + biome.temperatureBlendFactor) * 0.5f;
                float weight = Mathf.Pow(baseWeight + 1f, blendFactor);
                
                // Only add biome if it has some weight (matches GitHub threshold of 0.001f)
                if (weight > 0.001f)
                {
                    weights[biome] = weight;
                }
            }

            return weights;
        }

        /// <summary>
        /// Get the primary biome (highest weight) for a given point
        /// </summary>
        public BiomeData GetPrimaryBiome(float humidity, float temperature, float height, float slope = 0f)
        {
            var weights = CalculateBiomeWeights(humidity, temperature, height, slope);
            
            BiomeData primaryBiome = null;
            float maxWeight = 0f;
            
            foreach (var kvp in weights)
            {
                if (kvp.Value > maxWeight)
                {
                    maxWeight = kvp.Value;
                    primaryBiome = kvp.Key;
                }
            }
            
            return primaryBiome;
        }

        /// <summary>
        /// Get all terrain layers from all biomes in this collection
        /// Returns list of TerrainLayers in order (one per biome)
        /// </summary>
        public List<TerrainLayer> GetAllTerrainLayers()
        {
            List<TerrainLayer> layers = new List<TerrainLayer>();
            
            if (biomes == null) return layers;
            
            foreach (BiomeData biome in biomes)
            {
                if (biome != null && biome.terrainLayers != null && biome.terrainLayers.Length > 0)
                {
                    TerrainLayer layer = biome.CreateTerrainLayer();
                    if (layer != null)
                    {
                        layers.Add(layer);
                    }
                }
            }
            
            return layers;
        }

        /// <summary>
        /// Get a dictionary mapping each biome to its terrain layer index
        /// This ensures correct mapping when some biomes might not have terrain layers
        /// </summary>
        public Dictionary<BiomeData, int> GetBiomeToLayerIndexMap()
        {
            Dictionary<BiomeData, int> map = new Dictionary<BiomeData, int>();
            
            if (biomes == null) return map;
            
            int layerIndex = 0;
            foreach (BiomeData biome in biomes)
            {
                if (biome != null && biome.terrainLayers != null && biome.terrainLayers.Length > 0)
                {
                    TerrainLayer layer = biome.CreateTerrainLayer();
                    if (layer != null)
                    {
                        map[biome] = layerIndex;
                        layerIndex++;
                    }
                }
            }
            
            return map;
        }

        /// <summary>
        /// Validate biome collection - check for errors
        /// </summary>
        public void Validate()
        {
            if (biomes == null || biomes.Length == 0)
            {
                Debug.LogWarning($"BiomeCollection '{name}' has no biomes!");
                return;
            }

            foreach (BiomeData biome in biomes)
            {
                if (biome == null)
                {
                    Debug.LogWarning($"BiomeCollection '{name}' contains null biome!");
                    continue;
                }

                if (biome.terrainLayers == null || biome.terrainLayers.Length == 0)
                {
                    Debug.LogWarning($"Biome '{biome.biomeName}' has no terrain layers!");
                }
            }
        }
    }
}
