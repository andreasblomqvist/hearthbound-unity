using UnityEngine;
using Hearthbound.World;

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
            // REDESIGNED: Sharp, angular mountains like reference image

            // Reduced domain warping for less smooth deformation
            float warpX = GetNoise2D(x, y, seed + 100, frequency * 0.5f) * (warpStrength * 0.5f);
            float warpY = GetNoise2D(x, y, seed + 200, frequency * 0.3f) * (warpStrength * 0.3f);

            float warpedX = x + warpX;
            float warpedY = y + warpY;

            // Generate base ridged noise with fewer octaves for sharper features
            float baseNoise = GetFractalNoise(warpedX, warpedY, seed + 3000, 4, frequency, 2.0f, 0.5f);

            // SHARP ridged peaks - more aggressive formula
            float ridgedNoise = 1f - Mathf.Abs(baseNoise * 2f - 1f);

            // Sharp power curve for angular peaks (balanced sharpness)
            ridgedNoise = Mathf.Pow(ridgedNoise, 2.2f);

            // Add second layer of sharp ridges at different frequency for variation
            float ridgedDetail = GetFractalNoise(x, y, seed + 3500, 3, frequency * 2.5f, 2.0f, 0.5f);
            ridgedDetail = 1f - Mathf.Abs(ridgedDetail * 2f - 1f);
            ridgedDetail = Mathf.Pow(ridgedDetail, 2.8f) * 0.5f;

            // Combine for sharp, varied peaks
            float finalNoise = ridgedNoise + ridgedDetail;

            return Mathf.Clamp01(finalNoise);
        }

        /// <summary>
        /// Generate cliff noise using Voronoi cells for dramatic, angular rock faces
        /// Blends Voronoi cellular patterns with ridged noise for natural-looking sharp cliffs
        /// </summary>
        public static float GetCliffNoise(float x, float y, int seed, float frequency = 0.01f, float cliffStrength = 0.5f)
        {
            // Generate Voronoi cells for sharp boundaries
            float voronoi = GetVoronoiNoise(x, y, seed + 5000, frequency);

            // Invert Voronoi to make cell boundaries high (cliffs) and centers low (flat areas)
            float cliffPattern = 1f - voronoi;

            // Apply power curve to sharpen the cliffs
            cliffPattern = Mathf.Pow(cliffPattern, 2.5f);

            // Add some variation with ridged noise so cliffs aren't too uniform
            float ridgedVariation = GetRidgeNoise(x, y, seed + 5500, frequency * 0.7f);
            ridgedVariation = Mathf.Pow(ridgedVariation, 1.5f);

            // Blend cliff pattern with ridged variation
            float finalCliff = cliffPattern * 0.7f + ridgedVariation * 0.3f;

            // Apply cliff strength
            return finalCliff * cliffStrength;
        }

        /// <summary>
        /// Generate continental mask that defines where mountain ranges should appear
        /// Uses very low frequency noise for large-scale features
        /// </summary>
        public static float GetContinentalMask(float x, float y, int seed, float frequency = 0.0003f)
        {
            // Use very low frequency noise for large-scale features (default 0.0003)
            // IMPROVED: Use 3 octaves instead of 2 for more natural variation
            float mask = GetFractalNoise(x, y, seed + 4000, 3, frequency, 2.0f, 0.5f);

            // IMPROVED: Add slight high-frequency variation to break up uniformity
            float detail = GetNoise2D(x, y, seed + 4500, frequency * 10f) * 0.1f;
            mask = Mathf.Clamp01(mask + detail - 0.05f);

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
            System.Collections.Generic.List<Vector2> riverPath,
            float riverWidth, float riverDepth, float lakeRadius, float lakeDepth,
            float cliffStrength = 0.3f, float cliffFrequency = 0.01f, float cliffThreshold = 0.6f)
        {
            // 1. Generate continental mask (very low frequency) to define where mountains appear
            float continentalMask = GetContinentalMask(x, y, seed, continentalMaskFrequency);
            
            // 2. Generate base plains (relatively flat with gentle variation)
            float baseNoise = GetNoise2D(x, y, seed, 0.001f);
            // REDESIGNED: Flatter plains with gentle variation
            // Use mostly flat base with gentle noise variation (40%)
            float plainsHeight = baseHeight * 0.5f + (baseNoise * baseHeight * 0.4f);
            
            
            // 3. Generate gentle hills (reduced for flatter valleys)
            // REDESIGNED: Fewer octaves (3) for less rolling but some variation
            float hillNoise = GetFractalNoise(x, y, seed + 1000, 3, 0.003f, 2.0f, 0.5f);
            // Hills appear gradually (0.35-0.7 range) for smoother transition
            float hillMask = Mathf.Clamp01((continentalMask - 0.35f) * 2.86f);
            float hillsHeight = hillNoise * hillHeight * hillMask * 0.7f;
            
            // 4. Generate mountain ranges using domain warping and ridged noise
            float mountainRangeNoise = GetMountainRangeNoise(x, y, seed, mountainFrequency, warpStrength);

            // Use smooth multipliers based on continental mask, respecting continental threshold
            // Mountains appear gradually starting at threshold, full strength at threshold + 0.33
            float mountainMask = Mathf.Clamp01((continentalMask - continentalThreshold) * 3.0f);

            // 4b. Add Voronoi cliffs for dramatic, angular rock faces in high mountain areas (OPTIONAL)
            // Cliffs are additive - they enhance mountains without replacing the base ridged noise
            float cliffNoise = 0f;
            if (cliffStrength > 0.01f) // Only calculate if cliff strength is significant
            {
                cliffNoise = GetCliffNoise(x, y, seed, cliffFrequency, cliffStrength);

                // Cliffs only appear in very high mountain regions (above cliffThreshold)
                float cliffMask = Mathf.Clamp01((continentalMask - cliffThreshold) * 5.0f);

                // Add cliff noise on top of mountains (additive, not replacement)
                cliffNoise = cliffNoise * cliffMask * 0.3f; // Scale down to prevent overwhelming
            }

            // Combine ridged mountains with cliff enhancement
            float combinedMountainNoise = mountainRangeNoise + cliffNoise;

            // REDESIGNED: Increased multiplier (1.5 → 2.0) for dramatic peaks
            float mountainsHeight = combinedMountainNoise * mountainHeight * 2.0f * mountainMask;
            
            // 5. Combine layers - RAW height values (not normalized)
            float height = plainsHeight + hillsHeight + mountainsHeight;
            
            // 6. Apply water carving using river path
            float waterCarving = GetWaterCarvingFromPath(x, y, riverPath, riverWidth, riverDepth, lakeRadius, lakeDepth);
            
            
            // Reduce water carving in mountain areas to prevent rivers cutting through peaks
            float mountainCarvingReduction = 1f - (mountainMask * 0.9f);
            waterCarving *= mountainCarvingReduction;
            
            float heightBeforeCarving = height;
            height = Mathf.Max(0f, height - waterCarving);
            
            
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
        /// Generate Voronoi/Cellular noise for sharp, dramatic patterns
        /// Creates cellular patterns with sharp boundaries - perfect for cliffs and angular terrain
        /// Returns 0-1 where low values = cell boundaries (sharp edges), high values = cell centers
        /// </summary>
        public static float GetVoronoiNoise(float x, float y, int seed, float frequency = 0.01f)
        {
            // Scale coordinates by frequency
            x *= frequency;
            y *= frequency;

            // Determine base cell coordinates
            int cellX = Mathf.FloorToInt(x);
            int cellY = Mathf.FloorToInt(y);

            float minDistance = float.MaxValue;

            // Check 3x3 grid of cells around the point
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    int neighborX = cellX + offsetX;
                    int neighborY = cellY + offsetY;

                    // Generate pseudo-random feature point within this cell
                    // Use seed-based hashing for deterministic random positions
                    uint hash = (uint)(seed + neighborX * 374761393 + neighborY * 668265263);
                    hash ^= hash << 13;
                    hash ^= hash >> 17;
                    hash ^= hash << 5;

                    float featureX = neighborX + ((hash % 1000000) / 1000000f);
                    hash = hash * 1103515245 + 12345; // Next random
                    float featureY = neighborY + ((hash % 1000000) / 1000000f);

                    // Calculate distance to this feature point
                    float dx = x - featureX;
                    float dy = y - featureY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    minDistance = Mathf.Min(minDistance, distance);
                }
            }

            // Normalize distance to roughly 0-1 range
            // Typical max distance in a cell is ~0.707 (corner to corner)
            return Mathf.Clamp01(minDistance * 1.4f);
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

        /// <summary>
        /// Calculate water carving effect using a deterministic river path
        /// Carves terrain along the river path and creates a circular lake at the destination
        /// </summary>
        public static float GetWaterCarvingFromPath(float x, float y, 
            System.Collections.Generic.List<Vector2> riverPath,
            float riverWidth, float riverDepth, float lakeRadius, float lakeDepth)
        {
            if (riverPath == null || riverPath.Count == 0)
                return 0f;
            
            float riverCarving = 0f;
            float lakeCarving = 0f;
            Vector2 point = new Vector2(x, y);
            
            // Calculate river carving based on distance to river path
            float distanceToRiver = RiverPathGenerator.DistanceToRiverPath(point, riverPath);
            
            // Debug: Log river path info at sample points near the path
            if (distanceToRiver < riverWidth * 3f && Random.value < 0.001f) // Sample 0.1% of points near river
            {
                Debug.Log($"[River Carving] At ({x:F1}, {y:F1}): distToRiver={distanceToRiver:F1}, riverWidth={riverWidth:F1}");
            }
            
            if (distanceToRiver < riverWidth)
            {
                // Smooth carving based on distance from river center
                float riverStrength = 1f - (distanceToRiver / riverWidth);
                riverCarving = riverStrength * riverDepth;
            }
            
            // Calculate lake carving (circular lake at end of river path)
            Vector2 lakeCenter = RiverPathGenerator.GetLakeCenter(riverPath);
            float distanceToLake = Vector2.Distance(point, lakeCenter);
            if (distanceToLake < lakeRadius)
            {
                // Smooth carving for lake basin
                float lakeStrength = 1f - (distanceToLake / lakeRadius);
                lakeCarving = lakeStrength * lakeDepth;
            }
            
            // Return the maximum carving (rivers and lakes don't stack)
            return Mathf.Max(riverCarving, lakeCarving);
        }
    }
}
