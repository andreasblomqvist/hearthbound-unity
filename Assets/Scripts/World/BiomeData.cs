using UnityEngine;

namespace Hearthbound.World
{
    /// <summary>
    /// Represents a single terrain layer within a biome
    /// Used for multi-layer terrain textures
    /// </summary>
    [System.Serializable]
    public class TerrainLayerData
    {
        public Texture2D diffuseTexture;
        public Vector2 tileSize = new Vector2(15, 15);
        public Color color = Color.white; // Used if texture is null (placeholder)
    }

    /// <summary>
    /// Biome Data ScriptableObject
    /// Defines a biome with humidity/temperature ranges and terrain layers
    /// Based on concepts from: https://medium.com/@mrrsff/procedural-world-generation-with-biomes-in-unity-a474e11ff0b7
    /// </summary>
    [CreateAssetMenu(fileName = "NewBiomeData", menuName = "Hearthbound/Biome Data")]
    public class BiomeData : ScriptableObject
    {
        [Header("Biome Identification")]
        public string biomeName = "New Biome";
        
        [Header("Biome Criteria - Lookup Table")]
        [Tooltip("Height range (0-1) for this biome - biome appears within this height range")]
        public Vector2 heightRange = new Vector2(0f, 1f);
        
        [Tooltip("Temperature range (0-1) for this biome - biome appears within this temperature range")]
        public Vector2 temperatureRange = new Vector2(0f, 1f);
        
        [Tooltip("Humidity range (0-1) for this biome - biome appears within this humidity range")]
        public Vector2 humidityRange = new Vector2(0f, 1f);
        
        [Header("Legacy - Distance-Based Matching (for compatibility)")]
        [Tooltip("Humidity value (0-1) for distance-based matching - deprecated, use humidityRange instead")]
        [Range(0f, 1f)]
        public float humidity = 0.5f;
        
        [Tooltip("Temperature value (0-1) for distance-based matching - deprecated, use temperatureRange instead")]
        [Range(0f, 1f)]
        public float temperature = 0.5f;
        
        [Header("Height Multiplier")]
        [Tooltip("Multiplier applied to height noise values in this biome")]
        [Range(0.1f, 2.0f)]
        public float heightMultiplier = 1.0f;
        
        [Header("Terrain Layers")]
        [Tooltip("Multiple layers can be used for biome texture blending")]
        public TerrainLayerData[] terrainLayers = new TerrainLayerData[1];
        
        [Header("Blend Factors")]
        [Tooltip("Height blend factor - lower values create sharper transitions")]
        [Range(1f, 20f)]
        public float heightBlendFactor = 5f;
        
        [Tooltip("Humidity blend factor - controls how smoothly biome transitions based on humidity")]
        [Range(1f, 20f)]
        public float humidityBlendFactor = 5f;
        
        [Tooltip("Temperature blend factor - controls how smoothly biome transitions based on temperature")]
        [Range(1f, 20f)]
        public float temperatureBlendFactor = 5f;

        /// <summary>
        /// Check if this biome matches the given conditions using lookup table approach
        /// Returns match score (0-1) based on how well height, temperature, and humidity fit within ranges
        /// </summary>
        public float GetLookupTableMatch(float height, float temperature, float humidity)
        {
            // Hard cutoff for water biome: completely exclude if height is too high
            // This prevents water from appearing on mountains - use very strict threshold
            string biomeNameLower = biomeName.ToLower();
            if (biomeNameLower.Contains("water"))
            {
                // Water should ONLY appear at very low elevations - hard cutoff
                if (height > 0.08f) // Stricter than heightRange.y to account for any edge cases
                {
                    return 0f; // Completely exclude water above 8% height
                }
                // Also check if height is within the actual range
                if (height > heightRange.y)
                {
                    return 0f; // Double-check: exclude water above its max height range
                }
            }
            
            // Hard cutoff for snow/rock: completely exclude if height is too low
            if (biomeNameLower.Contains("snow") || biomeNameLower.Contains("rock") || biomeNameLower.Contains("mountain"))
            {
                if (height < heightRange.x)
                {
                    return 0f; // Completely exclude snow/rock below their min height
                }
            }
            
            // Check if all factors are within range
            bool heightMatch = height >= heightRange.x && height <= heightRange.y;
            bool tempMatch = temperature >= temperatureRange.x && temperature <= temperatureRange.y;
            bool humidMatch = humidity >= humidityRange.x && humidity <= humidityRange.y;
            
            // If all factors match, return perfect score
            if (heightMatch && tempMatch && humidMatch)
            {
                return 1f;
            }
            
            // Calculate how close each factor is to its range (0-1 for each)
            // For water, height must be perfect - no falloff outside range
            float heightScore;
            if (biomeNameLower.Contains("water"))
            {
                // Water: hard cutoff - no score if outside height range
                if (height < heightRange.x || height > heightRange.y)
                {
                    return 0f; // No match if outside height range
                }
                heightScore = 1f; // Perfect match if within range
            }
            else
            {
                heightScore = GetRangeScore(height, heightRange.x, heightRange.y);
            }
            
            float tempScore = GetRangeScore(temperature, temperatureRange.x, temperatureRange.y);
            float humidScore = GetRangeScore(humidity, humidityRange.x, humidityRange.y);
            
            // Combine scores (all factors should be favorable)
            return heightScore * tempScore * humidScore;
        }
        
        /// <summary>
        /// Get how well a value fits within a range (0-1)
        /// Returns 1.0 if within range, decreases smoothly outside range
        /// </summary>
        private float GetRangeScore(float value, float min, float max)
        {
            if (value >= min && value <= max)
            {
                return 1f; // Perfect match
            }
            
            // Calculate distance from range
            float distance;
            if (value < min)
            {
                distance = min - value;
            }
            else
            {
                distance = value - max;
            }
            
            // Smooth falloff outside range
            float rangeSize = max - min;
            float normalizedDistance = distance / Mathf.Max(0.1f, rangeSize);
            return Mathf.Exp(-normalizedDistance * 2f); // Exponential falloff
        }
        
        /// <summary>
        /// Calculate biome match value (distance-based, lower is better)
        /// Matches GitHub implementation: temperatureMatch + humidityMatch
        /// Legacy method for compatibility
        /// </summary>
        public float GetBiomeMatchValue(float sampleHumidity, float sampleTemperature)
        {
            float temperatureMatch = Mathf.Abs(temperature - sampleTemperature);
            float humidityMatch = Mathf.Abs(humidity - sampleHumidity);
            
            return temperatureMatch + humidityMatch;
        }

        /// <summary>
        /// Create a Unity TerrainLayer from this biome's first terrain layer data
        /// </summary>
        public TerrainLayer CreateTerrainLayer()
        {
            if (terrainLayers == null || terrainLayers.Length == 0)
            {
                Debug.LogWarning($"BiomeData {biomeName} has no terrain layers!");
                return null;
            }

            TerrainLayer layer = new TerrainLayer();
            TerrainLayerData layerData = terrainLayers[0];
            
            if (layerData.diffuseTexture != null)
            {
                layer.diffuseTexture = layerData.diffuseTexture;
            }
            else
            {
                // Create colored placeholder texture
                layer.diffuseTexture = CreateColoredTexture(layerData.color, biomeName);
            }
            
            layer.tileSize = layerData.tileSize;
            layer.diffuseTexture.name = $"{biomeName}Texture";
            
            return layer;
        }

        /// <summary>
        /// Create a colored placeholder texture
        /// </summary>
        private Texture2D CreateColoredTexture(Color color, string name)
        {
            Texture2D tex = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.name = name;
            return tex;
        }
    }
}
