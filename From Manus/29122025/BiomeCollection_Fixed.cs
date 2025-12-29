using System.Collections.Generic;
using UnityEngine;

namespace Hearthbound.World
{
    /// <summary>
    /// Biome Collection ScriptableObject
    /// Defines which biomes exist in the world and how they blend together
    /// Based on concepts from: https://medium.com/@mrrsff/procedural-world-generation-with-biomes-in-unity-a474e11ff0b7
    /// 
    /// SIMPLIFIED VERSION - Removed complex height adjustments that were overriding the lookup table
    /// </summary>
    [CreateAssetMenu(fileName = "NewBiomeCollection", menuName = "Hearthbound/Biome Collection")]
    public class BiomeCollection : ScriptableObject
    {
        [Header("Biome Collection")]
        [Tooltip("List of biomes available in this world")]
        public BiomeData[] biomes = new BiomeData[0];
        
        [Header("Global Blend Settings")]
        [Tooltip("Global biome blend factor - higher values create sharper transitions (1-10)")]
        [Range(1f, 10f)]
        public float globalBlendFactor = 3f;
        
        [Tooltip("If true, uses global blend factor. If false, uses individual biome blend factors")]
        public bool useGlobalBlendFactor = true;
        
        [Header("Debug")]
        [Tooltip("Enable detailed logging for biome weight calculations")]
        public bool debugLogging = false;

        /// <summary>
        /// Calculate biome weights for a given point
        /// Returns dictionary of biome -> weight (weights are NOT normalized to 1.0)
        /// 
        /// Simplified approach:
        /// 1. Each biome calculates its match score (0-1) based on height, temp, humidity ranges
        /// 2. Apply blend factor to sharpen or soften boundaries
        /// 3. Return weights (Unity will normalize them when applying to splatmap)
        /// </summary>
        public Dictionary<BiomeData, float> CalculateBiomeWeights(float humidity, float temperature, float height, float slope = 0f)
        {
            Dictionary<BiomeData, float> weights = new Dictionary<BiomeData, float>();
            
            if (biomes == null || biomes.Length == 0)
            {
                Debug.LogWarning("BiomeCollection has no biomes defined!");
                return weights;
            }
            
            // Calculate weight for each biome
            foreach (BiomeData biome in biomes)
            {
                if (biome == null) continue;

                // Get match score from biome (0-1, higher = better match)
                float matchScore = biome.CalculateMatchScore(height, temperature, humidity);
                
                // Skip biomes with negligible match scores
                if (matchScore < 0.001f)
                    continue;
                
                // Apply global or individual blend factor
                float blendFactor = useGlobalBlendFactor ? globalBlendFactor : biome.blendStrength;
                
                // Apply blend factor to create sharper or softer boundaries
                // Higher blend factor = sharper boundaries (biome dominates its territory)
                float weight = Mathf.Pow(matchScore, 1f / blendFactor);
                
                // Only add biome if it has meaningful weight
                if (weight > 0.001f)
                {
                    weights[biome] = weight;
                }
            }

            // Debug logging if enabled
            if (debugLogging && weights.Count > 0)
            {
                string log = $"Biome weights at h={height:F2}, t={temperature:F2}, m={humidity:F2}:\n";
                float totalWeight = 0f;
                foreach (var kvp in weights)
                {
                    totalWeight += kvp.Value;
                }
                foreach (var kvp in weights)
                {
                    float normalizedWeight = kvp.Value / totalWeight;
                    log += $"  {kvp.Key.biomeName}: {normalizedWeight:P0}\n";
                }
                Debug.Log(log);
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
                if (biome == null) continue;
                
                TerrainLayer layer = biome.CreateTerrainLayer();
                if (layer != null)
                {
                    layers.Add(layer);
                }
            }
            
            return layers;
        }

        /// <summary>
        /// Get mapping from BiomeData to terrain layer index
        /// This is needed because not all biomes may have valid terrain layers
        /// </summary>
        public Dictionary<BiomeData, int> GetBiomeToLayerIndexMap()
        {
            Dictionary<BiomeData, int> map = new Dictionary<BiomeData, int>();
            
            if (biomes == null) return map;
            
            int layerIndex = 0;
            foreach (BiomeData biome in biomes)
            {
                if (biome == null) continue;
                
                // Only include biomes that have valid terrain layers
                if (biome.terrainLayers != null && biome.terrainLayers.Length > 0)
                {
                    map[biome] = layerIndex;
                    layerIndex++;
                }
            }
            
            return map;
        }
    }
}
