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
        
        [Header("Height Multiplier")]
        [Tooltip("Multiplier applied to height noise values in this biome")]
        [Range(0.1f, 2.0f)]
        public float heightMultiplier = 1.0f;
        
        [Header("Terrain Layers")]
        [Tooltip("Multiple layers can be used for biome texture blending")]
        public TerrainLayerData[] terrainLayers = new TerrainLayerData[1];
        
        [Header("Blend Factors")]
        [Tooltip("How strongly this biome 'claims' its territory - higher = sharper boundaries")]
        [Range(1f, 10f)]
        public float blendStrength = 3f;

        /// <summary>
        /// Calculate match score for this biome at given conditions
        /// Returns a score (higher = better match)
        /// Uses smooth falloff outside ranges for natural blending
        /// </summary>
        public float CalculateMatchScore(float height, float temperature, float humidity)
        {
            // Calculate how well each parameter matches its range
            float heightScore = GetRangeMatchScore(height, heightRange);
            float tempScore = GetRangeMatchScore(temperature, temperatureRange);
            float humidScore = GetRangeMatchScore(humidity, humidityRange);
            
            // Combine scores multiplicatively (all must match reasonably well)
            float combinedScore = heightScore * tempScore * humidScore;
            
            // Apply blend strength to make boundaries sharper or softer
            // Higher blend strength = sharper boundaries (biome "claims" its territory more strongly)
            combinedScore = Mathf.Pow(combinedScore, blendStrength);
            
            return combinedScore;
        }
        
        /// <summary>
        /// Calculate how well a value matches a range
        /// Returns 1.0 if within range, smoothly falls off outside range
        /// </summary>
        private float GetRangeMatchScore(float value, Vector2 range)
        {
            float min = range.x;
            float max = range.y;
            
            // Perfect match if within range
            if (value >= min && value <= max)
            {
                return 1f;
            }
            
            // Calculate distance outside range
            float distance;
            if (value < min)
            {
                distance = min - value;
            }
            else // value > max
            {
                distance = value - max;
            }
            
            // Smooth exponential falloff
            // The falloff rate determines how quickly the score drops outside the range
            float falloffRate = 5f; // Higher = faster falloff
            float score = Mathf.Exp(-distance * falloffRate);
            
            return score;
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
