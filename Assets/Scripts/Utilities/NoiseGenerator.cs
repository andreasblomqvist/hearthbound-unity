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
        /// Creates flatter terrain with clustered mountains
        /// </summary>
        public static float GetTerrainHeight(float x, float y, int seed, float baseHeight = 50f, float hillHeight = 30f, float mountainHeight = 100f)
        {
            // Use different seed offsets for each layer to avoid correlation
            // Add position-based variation to break up patterns
            float offsetX = (seed % 1000) * 0.01f;
            float offsetY = ((seed / 1000) % 1000) * 0.01f;
            
            // Base terrain (large, flat features) - very low frequency for broad, flat areas
            float baseNoise = GetFractalNoise(x + offsetX, y + offsetY, seed, 4, 0.0005f, 2.0f, 0.5f);
            
            // Hills (gentle rolling hills) - reduced contribution for flatter terrain
            float hillNoise = GetFractalNoise(x - offsetX * 2, y - offsetY * 2, seed + 1000, 3, 0.002f, 2.2f, 0.5f);
            
            // Mountains (clustered mountain regions) - MUCH lower frequency to create large mountain groups
            // Use lower frequency (0.0003f instead of 0.0015f) to create larger, clustered mountain regions
            float rotatedX = x * 0.707f - y * 0.707f; // 45 degree rotation
            float rotatedY = x * 0.707f + y * 0.707f;
            float mountainNoise = GetFractalNoise(rotatedX + offsetX, rotatedY + offsetY, seed + 2000, 4, 0.0003f, 2.5f, 0.6f);
            
            // Create mountain clusters: only apply mountains where noise is above a threshold
            // This creates distinct mountain regions rather than scattered peaks
            float mountainThreshold = 0.6f; // Only create mountains where noise > 0.6
            float mountainMask = Mathf.Clamp01((mountainNoise - mountainThreshold) / (1f - mountainThreshold));
            mountainMask = Mathf.Pow(mountainMask, 1.5f); // Sharpen the transition
            mountainNoise = mountainMask * mountainNoise; // Apply mask
            
            // Add subtle variation for organic feel (reduced)
            float warpX = GetNoise2D(x, y, seed + 5000, 0.001f) * 30f;
            float warpY = GetNoise2D(x, y, seed + 6000, 0.001f) * 30f;
            float warpedNoise = GetFractalNoise(x + warpX, y + warpY, seed + 3000, 2, 0.003f, 2.0f, 0.5f);
            
            // Combine layers with varied weights
            float height = baseNoise * baseHeight;
            height += hillNoise * hillHeight * 0.7f; // Reduced hill contribution for more plains
            height += mountainNoise * mountainHeight; // Mountains only in clusters
            height += warpedNoise * (hillHeight * 0.5f); // Subtle variation
            
            // Apply very mild flattening curve: only slightly reduces low areas
            // This creates more lowlands without making everything disappear
            float maxPossibleHeight = baseHeight + hillHeight + mountainHeight;
            float normalizedHeight = height / maxPossibleHeight;
            
            // Very gentle curve: only slightly flattens low areas (1.05 instead of 1.2)
            // This preserves most height variation while creating slightly more lowlands
            normalizedHeight = Mathf.Pow(normalizedHeight, 1.05f);
            height = normalizedHeight * maxPossibleHeight;
            
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
