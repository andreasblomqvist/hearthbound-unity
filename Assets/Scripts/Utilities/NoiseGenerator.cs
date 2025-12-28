using UnityEngine;

namespace Hearthbound.Utilities
{
    /// <summary>
    /// Noise Generator using Unity's Perlin Noise
    /// Provides various noise functions for procedural generation
    /// </summary>
    public static class NoiseGenerator
    {
        /// <summary>
        /// Generate 2D Perlin noise with seed support
        /// </summary>
        public static float GetNoise2D(float x, float y, int seed, float frequency = 0.01f)
        {
            // Offset based on seed for reproducibility
            float offsetX = seed * 0.1f;
            float offsetY = seed * 0.2f;
            
            return Mathf.PerlinNoise((x + offsetX) * frequency, (y + offsetY) * frequency);
        }

        /// <summary>
        /// Generate fractal/layered noise (Fractal Brownian Motion)
        /// </summary>
        public static float GetFractalNoise(float x, float y, int seed, int octaves = 4, float frequency = 0.01f, float lacunarity = 2.0f, float gain = 0.5f)
        {
            float total = 0f;
            float amplitude = 1f;
            float maxValue = 0f;
            float freq = frequency;

            for (int i = 0; i < octaves; i++)
            {
                total += GetNoise2D(x, y, seed + i, freq) * amplitude;
                maxValue += amplitude;
                
                amplitude *= gain;
                freq *= lacunarity;
            }

            return total / maxValue; // Normalize to 0-1
        }

        /// <summary>
        /// Generate height value for terrain
        /// Combines multiple noise layers for realistic terrain
        /// </summary>
        public static float GetTerrainHeight(float x, float y, int seed, float baseHeight = 50f, float hillHeight = 30f, float mountainHeight = 100f)
        {
            // Base terrain (large features)
            float baseNoise = GetFractalNoise(x, y, seed, 4, 0.001f, 2.0f, 0.5f);
            
            // Hills (medium features)
            float hillNoise = GetFractalNoise(x, y, seed + 1000, 3, 0.005f, 2.0f, 0.5f);
            
            // Mountains (sharp peaks)
            float mountainNoise = GetFractalNoise(x, y, seed + 2000, 2, 0.002f, 2.5f, 0.6f);
            mountainNoise = Mathf.Pow(mountainNoise, 2f); // Square for sharper peaks
            
            // Combine layers
            float height = baseNoise * baseHeight;
            height += hillNoise * hillHeight;
            height += mountainNoise * mountainHeight;
            
            return height;
        }

        /// <summary>
        /// Generate moisture/temperature value for biomes
        /// </summary>
        public static float GetBiomeValue(float x, float y, int seed, float frequency = 0.003f)
        {
            return GetFractalNoise(x, y, seed + 5000, 3, frequency, 2.0f, 0.5f);
        }

        /// <summary>
        /// Generate value for detail placement (trees, rocks, etc.)
        /// </summary>
        public static float GetDetailNoise(float x, float y, int seed, float frequency = 0.1f)
        {
            return GetNoise2D(x, y, seed + 10000, frequency);
        }

        /// <summary>
        /// Check if a position should have a detail object based on density
        /// </summary>
        public static bool ShouldPlaceDetail(float x, float y, int seed, float density = 0.5f)
        {
            float noise = GetDetailNoise(x, y, seed);
            return noise > (1f - density);
        }

        /// <summary>
        /// Generate Voronoi-like noise for cellular patterns
        /// Useful for village placement, biome boundaries, etc.
        /// </summary>
        public static float GetVoronoiNoise(float x, float y, int seed, float cellSize = 100f)
        {
            // Simple Voronoi approximation using multiple noise layers
            float noise1 = GetNoise2D(x, y, seed, 1f / cellSize);
            float noise2 = GetNoise2D(x + 100f, y + 100f, seed, 1f / cellSize);
            
            return Mathf.Abs(noise1 - noise2);
        }

        /// <summary>
        /// Generate ridge noise (inverted valleys)
        /// Good for mountain ridges and rivers
        /// </summary>
        public static float GetRidgeNoise(float x, float y, int seed, float frequency = 0.01f)
        {
            float noise = GetNoise2D(x, y, seed, frequency);
            return 1f - Mathf.Abs(noise * 2f - 1f); // Create ridges
        }

        /// <summary>
        /// Generate billow noise (puffy clouds effect)
        /// </summary>
        public static float GetBillowNoise(float x, float y, int seed, float frequency = 0.01f)
        {
            float noise = GetNoise2D(x, y, seed, frequency);
            return Mathf.Abs(noise * 2f - 1f);
        }

        /// <summary>
        /// Generate domain warped noise (more organic/flowing)
        /// </summary>
        public static float GetWarpedNoise(float x, float y, int seed, float frequency = 0.01f, float warpStrength = 10f)
        {
            // Get warp offsets
            float warpX = GetNoise2D(x, y, seed + 100, frequency) * warpStrength;
            float warpY = GetNoise2D(x, y, seed + 200, frequency) * warpStrength;
            
            // Apply warp
            return GetNoise2D(x + warpX, y + warpY, seed, frequency);
        }

        /// <summary>
        /// Remap value from one range to another
        /// </summary>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        /// <summary>
        /// Apply falloff from center (useful for island generation)
        /// </summary>
        public static float ApplyRadialFalloff(float value, float x, float y, float centerX, float centerY, float radius)
        {
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
            float falloff = Mathf.Clamp01(1f - (distance / radius));
            return value * falloff;
        }
    }
}
