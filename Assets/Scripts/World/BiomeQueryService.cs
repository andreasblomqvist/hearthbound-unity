using UnityEngine;
using Hearthbound.Utilities;

namespace Hearthbound.World
{
    /// <summary>
    /// Provides query methods for biome information at world positions
    /// </summary>
    public class BiomeQueryService
    {
        private TerrainQueryService terrainQuery;
        private BiomeCollection biomeCollection;

        // Biome height thresholds
        public float WaterHeight { get; set; } = 0.05f;
        public float GrassHeight { get; set; } = 0.3f;
        public float RockHeight { get; set; } = 0.6f;
        public float SnowHeight { get; set; } = 0.7f;
        public float SteepSlope { get; set; } = 45f;

        // Forest thresholds
        public float ForestMoistureMin { get; set; } = 0.5f;
        public float ForestMoistureMax { get; set; } = 1.0f;
        public float ForestTemperatureMin { get; set; } = 0.3f;
        public float ForestTemperatureMax { get; set; } = 0.7f;

        // Noise parameters
        public float MoistureFrequency { get; set; } = 0.003f;
        public float TemperatureFrequency { get; set; } = 0.002f;

        // Configuration
        public int TerrainHeight { get; set; } = 600;
        public bool UseScriptableObjectBiomes { get; set; } = true;

        public BiomeQueryService(TerrainQueryService terrainQuery, BiomeCollection biomeCollection)
        {
            this.terrainQuery = terrainQuery;
            this.biomeCollection = biomeCollection;
        }

        /// <summary>
        /// Get biome information at world position
        /// Returns a string describing the primary biome
        /// </summary>
        public string GetBiomeAtPosition(Vector3 worldPosition, int seed)
        {
            float height = terrainQuery.GetHeightAtPosition(worldPosition) / TerrainHeight;
            float slope = terrainQuery.GetSlopeAtPosition(worldPosition);

            // Temperature decreases with height
            float baseTemperature = 1f - height;
            float temperatureNoise = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed + 10000, TemperatureFrequency) * 0.2f - 0.1f;
            float temperature = baseTemperature + temperatureNoise;
            temperature = Mathf.Clamp01(temperature);

            // Humidity: Increases near water/low altitude, affected by temperature
            float baseHumidity = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed, MoistureFrequency);
            float heightHumidityBoost = (1f - height) * 0.3f;
            float tempHumidityInfluence = temperature * 0.2f;
            float moisture = baseHumidity + heightHumidityBoost + tempHumidityInfluence;
            moisture = Mathf.Clamp01(moisture);

            // Determine primary biome
            if (height < WaterHeight)
                return "Water";
            else if (height >= SnowHeight)
                return "Snow";
            else if (slope > SteepSlope || height >= RockHeight)
                return "Rock";
            else if (moisture >= ForestMoistureMin && moisture <= ForestMoistureMax &&
                     temperature >= ForestTemperatureMin && temperature <= ForestTemperatureMax &&
                     height >= WaterHeight && height < RockHeight && slope < SteepSlope)
                return "Forest";
            else if (height < GrassHeight && slope < SteepSlope)
                return "Plains";
            else
                return "Dirt";
        }

        /// <summary>
        /// Get detailed biome data at world position
        /// </summary>
        public BiomeInfo GetBiomeInfoAtPosition(Vector3 worldPosition, int seed)
        {
            float height = terrainQuery.GetHeightAtPosition(worldPosition) / TerrainHeight;
            float slope = terrainQuery.GetSlopeAtPosition(worldPosition);

            // Temperature decreases with height
            float baseTemperature = 1f - height;
            float temperatureNoise = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed + 10000, TemperatureFrequency) * 0.2f - 0.1f;
            float temperature = baseTemperature + temperatureNoise;
            temperature = Mathf.Clamp01(temperature);

            // Humidity
            float baseHumidity = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed, MoistureFrequency);
            float heightHumidityBoost = (1f - height) * 0.3f;
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
            if (UseScriptableObjectBiomes && biomeCollection != null)
            {
                float height = terrainQuery.GetHeightAtPosition(worldPosition) / TerrainHeight;
                float slope = terrainQuery.GetSlopeAtPosition(worldPosition);

                // Pure Perlin noise (not height-based)
                float moisture = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed, MoistureFrequency);
                float temperature = NoiseGenerator.GetBiomeValue(worldPosition.x, worldPosition.z, seed + 10000, TemperatureFrequency);
                temperature = Mathf.Clamp01(temperature);

                return biomeCollection.GetPrimaryBiome(moisture, temperature, height, slope);
            }
            return null;
        }
    }
}
