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
        /// Improved seed hashing function for better distribution across Perlin noise space
        /// Uses prime numbers and bit manipulation for better randomization
        /// </summary>
        private static float HashSeed(int seed, int offset)
        {
            // Use prime numbers and bit operations for better distribution
            uint hash = (uint)(seed + offset);
            hash ^= hash << 13;
            hash ^= hash >> 17;
            hash ^= hash << 5;
            // Convert to float in a good range for Perlin noise offsets
            return (hash % 1000000) / 10000f; // Range: 0-100
        }

        /// <summary>
        /// Generate 2D Perlin noise with seed support
        /// Returns values in 0-1 range (Unity's PerlinNoise already returns 0-1)
        /// </summary>
        public static float GetNoise2D(float x, float y, int seed, float frequency = 0.01f)
        {
            // Use improved seed hashing for better distribution across Perlin noise space
            // This ensures different seeds sample different regions, avoiding "bad" regions
            float offsetX = HashSeed(seed, 0);
            float offsetY = HashSeed(seed, 1);
            
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
            
            // Apply ridged noise for sharp peaks: 1 - abs(noise * 2 - 1)
            // This inverts it so peaks are HIGH (1.0) and valleys are LOW (0.0)
            float ridgedNoise = 1f - Mathf.Abs(noise * 2f - 1f);
            
            // Add power curve for sharper peaks
            ridgedNoise = Mathf.Pow(ridgedNoise, 1.5f);
            
            // Return value 0-1
            return Mathf.Clamp01(ridgedNoise);
        }

        /// <summary>
        /// Generate continental mask that defines where mountain ranges should appear
        /// Uses very low frequency noise for large-scale features
        /// </summary>
        public static float GetContinentalMask(float x, float y, int seed, float frequency = 0.0003f)
        {
            // Use very low frequency noise for large-scale features (default 0.0003)
            // Use fractal noise with 2 octaves for smooth transitions
            float mask = GetFractalNoise(x, y, seed + 4000, 2, frequency, 2.0f, 0.5f);
            
            // Return value 0-1 where high values = mountain regions, low values = plains
            return mask;
        }

        /// <summary>
        /// Generate height value for terrain
        /// Creates realistic mountain ranges using continental mask and domain warping
        /// </summary>
        public static float GetTerrainHeight(float x, float y, int seed, 
            float baseHeight, float hillHeight, float mountainHeight,
            float continentalThreshold, float warpStrength, 
            float mountainFrequency, float peakSharpness, float continentalMaskFrequency,
            float riverWidth, float riverDepth, float lakeThreshold, float lakeDepth)
        {
            // 1. Generate continental mask (very low frequency) to define where mountains appear
            float continentalMask = GetContinentalMask(x, y, seed, continentalMaskFrequency);
            
            // 2. Generate base plains using low frequency noise
            float baseNoise = GetNoise2D(x, y, seed, 0.001f);
            // FIXED: Use full baseHeight and ensure minimum height
            // Ensure base terrain has minimum height to prevent everything being underwater
            // Use higher multiplier to get more visible terrain
            float plainsHeight = Mathf.Max(baseNoise * baseHeight, baseHeight * 0.5f); // Minimum 50% of baseHeight everywhere
            
            // DEBUG: Log noise values early to diagnose (only at one specific point)
            if (x > 100 && y > 100 && x < 250 && y < 250 && (x == 200 && y == 200))
            {
                Debug.Log($"EARLY Noise Debug - baseNoise={baseNoise:F3}, continentalMask={continentalMask:F3}");
            }
            
            // 3. Generate rolling hills using medium frequency fractal noise
            float hillNoise = GetFractalNoise(x, y, seed + 1000, 3, 0.003f, 2.2f, 0.5f);
            // Use smooth multipliers based on continental mask, respecting thresholds
            // Hills appear gradually starting at mask 0.3, full strength at mask 0.7
            float hillMask = Mathf.Clamp01((continentalMask - 0.3f) * 2.5f);
            float hillsHeight = hillNoise * hillHeight * hillMask;
            
            // 4. Generate mountain ranges using domain warping and ridged noise
            float mountainRangeNoise = GetMountainRangeNoise(x, y, seed, mountainFrequency, warpStrength);
            
            // Use smooth multipliers based on continental mask, respecting continental threshold
            // Mountains appear gradually starting at threshold, full strength at threshold + 0.33
            float mountainMask = Mathf.Clamp01((continentalMask - continentalThreshold) * 3.0f);
            float mountainsHeight = mountainRangeNoise * mountainHeight * 1.5f * mountainMask;
            
            // 5. Combine layers - RAW height values (not normalized)
            float height = plainsHeight + hillsHeight + mountainsHeight;
            
            // 6. Apply water carving (rivers and lakes)
            // Reduce carving in mountain areas to prevent affecting mountain heights
            float riverNoise = GetRiverNoise(x, y, seed);
            float lakeNoise = GetLakeNoise(x, y, seed);
            float waterCarving = GetWaterCarving(x, y, seed, riverWidth, riverDepth, lakeThreshold, lakeDepth);
            
            // Reduce water carving in mountain areas (mountains shouldn't have rivers/lakes carved into them)
            // Use mountainMask to reduce carving - full carving in plains, reduced in hills, minimal in mountains
            float mountainCarvingReduction = 1f - (mountainMask * 0.9f); // Reduce carving by up to 90% in mountain areas
            waterCarving *= mountainCarvingReduction;
            
            float heightBeforeCarving = height;
            height = Mathf.Max(0f, height - waterCarving); // Subtract carving, but don't go below 0
            
            // DEBUG: Log detailed height breakdown (only log once per terrain generation)
            // Use a simple coordinate check that will definitely match during terrain generation
            // Check for a specific world coordinate that corresponds to the sample point in TerrainGenerator
            // TerrainGenerator samples at (width/4, height/4), which for 513x513 = (128, 128)
            // World coordinates: 128 * (1000/513) ≈ 249.5
            if (Mathf.Abs(x - 250f) < 2f && Mathf.Abs(y - 250f) < 2f)
            {
                Debug.Log($"[NoiseGenerator] FULL Noise Values Debug at world ({x:F1}, {y:F1}):");
                Debug.Log($"   baseNoise={baseNoise:F3}, hillNoise={hillNoise:F3}, mountainNoise={mountainRangeNoise:F3}");
                Debug.Log($"   continentalMask={continentalMask:F3}, hillMask={hillMask:F3}, mountainMask={mountainMask:F3}");
                Debug.Log($"[NoiseGenerator] Height Components:");
                Debug.Log($"   plains={plainsHeight:F2}, hills={hillsHeight:F2}, mountains={mountainsHeight:F2}, BEFORE_CARVING={heightBeforeCarving:F2}");
                Debug.Log($"   Height Params: baseHeight={baseHeight:F1}, hillHeight={hillHeight:F1}, mountainHeight={mountainHeight:F1}");
                Debug.Log($"[Rivers and Lakes] Water Features:");
                Debug.Log($"   riverNoise={riverNoise:F3} (threshold: {riverWidth:F3}, {(riverNoise < riverWidth ? "RIVER" : "no river")})");
                Debug.Log($"   lakeNoise={lakeNoise:F3} (threshold: {lakeThreshold:F3}, {(lakeNoise < lakeThreshold ? "LAKE" : "no lake")})");
                Debug.Log($"   waterCarving={waterCarving:F2}, heightAfterCarving={height:F2}");
                Debug.Log($"   Water Params: riverWidth={riverWidth:F3}, riverDepth={riverDepth:F1}, lakeThreshold={lakeThreshold:F3}, lakeDepth={lakeDepth:F1}");
            }
            
            // NOTE: Power curve (peakSharpness) is now applied in TerrainGenerator after normalization
            // This prevents the power curve from reducing already-low height values
            // The power curve should be applied to normalized 0-1 values, not raw height values
            
            // Return RAW height (0 to maxPossibleHeight range, e.g., 0-1450)
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

        /// <summary>
        /// Generate river path noise - creates winding river channels
        /// Returns 0-1 where values close to 0 indicate river paths
        /// </summary>
        public static float GetRiverNoise(float x, float y, int seed, float frequency = 0.0005f)
        {
            // Use domain warping to create meandering river paths
            float warpX = GetNoise2D(x, y, seed + 300, frequency * 0.3f) * 200f;
            float warpY = GetNoise2D(x, y, seed + 400, frequency * 0.3f) * 200f;
            
            // Generate base river noise at warped coordinates
            float riverNoise = GetNoise2D(x + warpX, y + warpY, seed + 6000, frequency);
            
            // Create river channels using distance from 0.5
            // Values close to 0.5 become rivers (narrow channels)
            float riverMask = Mathf.Abs(riverNoise - 0.5f) * 2f; // Remap to 0-1
            
            return riverMask;
        }

        /// <summary>
        /// Generate lake depression noise - creates natural lake basins
        /// Returns 0-1 where low values indicate lake depressions
        /// </summary>
        public static float GetLakeNoise(float x, float y, int seed, float frequency = 0.0003f)
        {
            // Use multiple noise layers for organic lake shapes
            float noise1 = GetNoise2D(x, y, seed + 7000, frequency);
            float noise2 = GetNoise2D(x, y, seed + 7100, frequency * 2f);
            
            // Combine for more interesting shapes
            float combined = noise1 * 0.7f + noise2 * 0.3f;
            
            // Create circular depressions using power curve
            float lakeMask = Mathf.Pow(combined, 2.0f);
            
            return lakeMask;
        }

        /// <summary>
        /// Calculate water carving effect - how much to lower terrain for water features
        /// </summary>
        public static float GetWaterCarving(float x, float y, int seed, float riverWidth = 0.15f, float riverDepth = 40f, float lakeThreshold = 0.3f, float lakeDepth = 30f)
        {
            float riverNoise = GetRiverNoise(x, y, seed);
            float lakeNoise = GetLakeNoise(x, y, seed);
            
            // Rivers: carve where riverNoise is below threshold
            float riverCarving = 0f;
            if (riverNoise < riverWidth)
            {
                // Smooth carving based on distance from river center
                float riverStrength = 1f - (riverNoise / riverWidth);
                riverCarving = riverStrength * riverDepth;
            }
            
            // Lakes: carve where lakeNoise is below threshold
            float lakeCarving = 0f;
            if (lakeNoise < lakeThreshold)
            {
                // Smooth carving for lake basins
                float lakeStrength = 1f - (lakeNoise / lakeThreshold);
                lakeCarving = lakeStrength * lakeDepth;
            }
            
            // Return the maximum carving (rivers and lakes don't stack)
            return Mathf.Max(riverCarving, lakeCarving);
        }
    }
}
