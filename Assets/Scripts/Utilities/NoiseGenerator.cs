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
        /// Returns values in 0-1 range (Unity's PerlinNoise already returns 0-1)
        /// </summary>
        public static float GetNoise2D(float x, float y, int seed, float frequency = 0.01f)
        {
            // Offset based on seed for reproducibility
            float offsetX = seed * 0.1f;
            float offsetY = seed * 0.2f;
            
            // Unity's PerlinNoise already returns 0-1, no remapping needed
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
        /// Generate mountain range noise using domain warping and ridged noise
        /// Creates elongated mountain ranges instead of circular blobs
        /// </summary>
        public static float GetMountainRangeNoise(float x, float y, int seed, float frequency = 0.0008f, float warpStrength = 150f)
        {
            // Domain warping to elongate mountains into ranges
            // Warp more in one direction to create linear ranges
            float warpX = GetNoise2D(x, y, seed + 100, frequency * 0.5f) * warpStrength;
            float warpY = GetNoise2D(x, y, seed + 200, frequency * 0.3f) * (warpStrength * 0.6f); // Less warping in Y for elongation
            
            // Apply asymmetric warp (more in X direction)
            float warpedX = x + warpX;
            float warpedY = y + warpY;
            
            // Generate base noise at warped coordinates
            float noise = GetFractalNoise(warpedX, warpedY, seed + 3000, 4, frequency, 2.5f, 0.6f);
            
            // Apply ridged noise for sharp peaks: abs(noise * 2 - 1)
            // This creates upward peaks (not inverted/hanging)
            float ridgedNoise = Mathf.Abs(noise * 2f - 1f);
            
            // Return value 0-1
            return Mathf.Clamp01(ridgedNoise);
        }

        /// <summary>
        /// Generate continental mask that defines where mountain ranges should appear
        /// Uses very low frequency noise for large-scale features
        /// </summary>
        public static float GetContinentalMask(float x, float y, int seed)
        {
            // Use very low frequency noise (0.0003) for large-scale features
            // Use fractal noise with 2 octaves for smooth transitions
            float mask = GetFractalNoise(x, y, seed + 4000, 2, 0.0003f, 2.0f, 0.5f);
            
            // Return value 0-1 where high values = mountain regions, low values = plains
            return mask;
        }

        /// <summary>
        /// Generate height value for terrain
        /// Creates realistic mountain ranges using continental mask and domain warping
        /// </summary>
        public static float GetTerrainHeight(float x, float y, int seed, 
            float baseHeight = 50f, float hillHeight = 30f, float mountainHeight = 100f,
            float continentalThreshold = 0.5f, float warpStrength = 150f, 
            float mountainFrequency = 0.0008f, float peakSharpness = 1.3f)
        {
            // 1. Generate continental mask (very low frequency) to define where mountains appear
            float continentalMask = GetContinentalMask(x, y, seed);
            
            // 2. Generate base plains using low frequency noise
            float baseNoise = GetNoise2D(x, y, seed, 0.001f);
            // FIXED: Use full baseHeight instead of 0.3 multiplier (was making terrain too flat)
            // baseNoise should now be guaranteed 0-1 range after GetNoise2D fix
            float plainsHeight = baseNoise * baseHeight; // Base plains everywhere
            
            // 3. Generate rolling hills using medium frequency fractal noise
            float hillNoise = GetFractalNoise(x, y, seed + 1000, 3, 0.003f, 2.2f, 0.5f);
            float hillsHeight = 0f;
            float hillThreshold = Mathf.Max(0f, continentalThreshold - 0.2f); // Hills appear before mountains
            if (continentalMask > hillThreshold)
            {
                // Add hills where continental mask > threshold
                // Use full hillHeight (removed 0.5 multiplier) to get proper height variation
                hillsHeight = hillNoise * hillHeight;
            }
            
            // 4. Generate mountain ranges using domain warping and ridged noise
            float mountainRangeNoise = GetMountainRangeNoise(x, y, seed, mountainFrequency, warpStrength);
            
            // DEBUG: Log noise values to diagnose issues (only log once per terrain generation)
            // Check if this is a sample point (away from origin to avoid edge cases)
            if (x > 100 && y > 100 && (x % 200 == 0 && y % 200 == 0))
            {
                Debug.Log($"🔍 Noise Values Debug - baseNoise={baseNoise:F3}, hillNoise={hillNoise:F3}, mountainNoise={mountainRangeNoise:F3}, continentalMask={continentalMask:F3}");
                Debug.Log($"🔍 Expected: All values 0.0-1.0. If negative or near 0.0, that's the problem.");
            }
            
            float mountainsHeight = 0f;
            // Add mountains where continental mask > threshold
            // NOTE: If continentalThreshold is too high (e.g., 0.5), mountains may not appear
            // Lower it to 0.3-0.4 in TerrainGenerator Inspector for more mountains
            if (continentalMask > continentalThreshold)
            {
                // Mountains should be MUCH taller than hills
                mountainsHeight = mountainRangeNoise * mountainHeight * 1.5f;
            }
            
            // 5. Combine layers - RAW height values (not normalized)
            float height = plainsHeight + hillsHeight + mountainsHeight;
            
            // 6. Apply power curve to make peaks sharper
            // Calculate max possible height for curve application
            // Updated max calculation: baseHeight (full) + hillHeight (full) + mountainHeight (1.5x)
            float maxPossibleHeight = baseHeight + hillHeight + mountainHeight * 1.5f;
            if (maxPossibleHeight > 0f)
            {
                // Apply power curve: normalize internally, apply curve, scale back to raw
                float normalizedHeight = height / maxPossibleHeight;
                normalizedHeight = Mathf.Pow(normalizedHeight, peakSharpness);
                height = normalizedHeight * maxPossibleHeight;
            }
            
            // Return RAW height (0 to maxPossibleHeight range, e.g., 0-600)
            // TerrainGenerator will normalize this to 0-1 for Unity terrain
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
