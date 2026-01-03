using UnityEngine;
using System.Collections.Generic;
using Hearthbound.Utilities;

namespace Hearthbound.World
{
    /// <summary>
    /// Generates terrain heightmaps using noise functions and water carving
    /// </summary>
    public class HeightmapGenerator
    {
        // Height generation parameters
        public float BaseHeight { get; set; } = 150f;
        public float HillHeight { get; set; } = 100f;
        public float MountainHeight { get; set; } = 300f;
        public AnimationCurve HeightCurve { get; set; } = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Noise parameters
        public float ContinentalThreshold { get; set; } = 0.3f;
        public float ContinentalMaskFrequency { get; set; } = 0.0003f;
        public float WarpStrength { get; set; } = 150f;
        public float MountainFrequency { get; set; } = 0.0008f;
        public float PeakSharpness { get; set; } = 1.3f;

        // Cliff parameters (Voronoi-based dramatic cliffs)
        public float CliffStrength { get; set; } = 0.3f;
        public float CliffFrequency { get; set; } = 0.01f;
        public float CliffThreshold { get; set; } = 0.6f;

        // Water carving parameters
        public float RiverWidth { get; set; } = 40f;
        public float RiverDepth { get; set; } = 200f;
        public float LakeRadius { get; set; } = 150f;
        public float LakeDepth { get; set; } = 250f;

        // Terrain dimensions
        public int TerrainWidth { get; set; } = 1000;
        public int TerrainLength { get; set; } = 1000;
        public int TerrainHeight { get; set; } = 600;
        public int HeightmapResolution { get; set; } = 513;

        /// <summary>
        /// Generate heightmap and apply to terrain data
        /// </summary>
        /// <param name="seed">Random seed for noise generation</param>
        /// <param name="terrainData">Terrain data to modify</param>
        /// <param name="riverPath">River path for water carving (can be null)</param>
        public void GenerateHeightmap(int seed, TerrainData terrainData, List<Vector2> riverPath)
        {
            Debug.Log("  Generating heightmap...");
            Debug.Log($"  Water Carving Params: riverWidth={RiverWidth:F1}, riverDepth={RiverDepth:F1}, lakeRadius={LakeRadius:F1}, lakeDepth={LakeDepth:F1}");

            int width = terrainData.heightmapResolution;
            int height = terrainData.heightmapResolution;
            float[,] heights = new float[width, height];

            // Calculate world space size per heightmap pixel
            float scaleX = TerrainWidth / (float)width;
            float scaleZ = TerrainLength / (float)height;

            // Ensure riverPath is never null (fallback to empty list)
            if (riverPath == null)
            {
                Debug.LogWarning("River path is null! Creating empty path.");
                riverPath = new List<Vector2>();
            }

            // Calculate theoretical max possible height (before water carving)
            // Formula: baseHeight (full) + hillHeight (full) + mountainHeight (1.5x)
            float theoreticalMaxHeight = (BaseHeight + HillHeight + MountainHeight * 1.5f) * 0.65f;

            // Account for maximum possible water carving in maxPossibleHeight calculation
            float maxWaterCarving = Mathf.Max(RiverDepth, LakeDepth);
            float maxPossibleHeight = theoreticalMaxHeight; // Use theoretical max, carving happens after

            // Debug: Log sample values to verify heights are correct
            bool loggedSample = false;

            // Track actual min/max for debugging
            float actualMinHeight = float.MaxValue;
            float actualMaxHeight = float.MinValue;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Convert to world space coordinates
                    float worldX = x * scaleX;
                    float worldZ = z * scaleZ;

                    // Generate height using noise - returns RAW height values (0 to maxPossibleHeight)
                    float heightValue = NoiseGenerator.GetTerrainHeight(
                        worldX, worldZ, seed,
                        BaseHeight, HillHeight, MountainHeight,
                        ContinentalThreshold, WarpStrength, MountainFrequency, PeakSharpness, ContinentalMaskFrequency,
                        riverPath,
                        RiverWidth, RiverDepth, LakeRadius, LakeDepth,
                        CliffStrength, CliffFrequency, CliffThreshold
                    );

                    // Track actual min/max
                    actualMinHeight = Mathf.Min(actualMinHeight, heightValue);
                    actualMaxHeight = Mathf.Max(actualMaxHeight, heightValue);

                    // Normalize to 0-1 range using theoretical max height (before carving)
                    float normalizedHeight = 0f;
                    if (maxPossibleHeight > 0f)
                    {
                        normalizedHeight = heightValue / maxPossibleHeight;
                        // NOTE: Power curve removed - it was reducing already-low heights
                        // For values < 1.0, raising to power > 1.0 makes them smaller
                    }

                    // Debug logging for first few samples
                    if (!loggedSample && (x == width / 4 && z == height / 4))
                    {
                        LogHeightDebug(seed, x, z, scaleX, scaleZ, heightValue, normalizedHeight, maxPossibleHeight, riverPath);
                        loggedSample = true;
                    }

                    // Apply height curve for more control
                    normalizedHeight = HeightCurve.Evaluate(normalizedHeight);

                    // Clamp to valid range (Unity terrain expects 0-1)
                    normalizedHeight = Mathf.Clamp01(normalizedHeight);

                    heights[z, x] = normalizedHeight;
                }
            }

            // Log actual height range for debugging
            Debug.Log($"  Actual Height Range: Min={actualMinHeight:F2}, Max={actualMaxHeight:F2}, Theoretical Max={maxPossibleHeight:F2}");

            // Count terrain distribution for verification
            LogTerrainDistribution(heights, width, height);

            terrainData.SetHeights(0, 0, heights);
            Debug.Log("  Heightmap generated");
        }

        private void LogHeightDebug(int seed, int x, int z, float scaleX, float scaleZ, float heightValue, float normalizedHeight, float maxPossibleHeight, List<Vector2> riverPath)
        {
            // Get continental mask for this point to debug
            float sampleWorldX = (x) * scaleX;
            float sampleWorldZ = (z) * scaleZ;
            float sampleContinentalMask = NoiseGenerator.GetContinentalMask(sampleWorldX, sampleWorldZ, seed, ContinentalMaskFrequency);
            Debug.Log($"Height Debug - Raw: {heightValue:F2}, Max: {maxPossibleHeight:F2}, Normalized: {normalizedHeight:F3}");
            Debug.Log($"Continental Mask: {sampleContinentalMask:F3}, Threshold: {ContinentalThreshold:F3}");

            // Manual breakdown of height calculation to match what GetTerrainHeight does
            float testBaseNoise = NoiseGenerator.GetNoise2D(sampleWorldX, sampleWorldZ, seed, 0.001f);
            float testHillNoise = NoiseGenerator.GetFractalNoise(sampleWorldX, sampleWorldZ, seed + 1000, 3, 0.003f, 2.2f, 0.5f);
            float testMountainNoise = NoiseGenerator.GetMountainRangeNoise(sampleWorldX, sampleWorldZ, seed, MountainFrequency, WarpStrength);
            float testHillMask = Mathf.Max(0.5f, sampleContinentalMask);
            float testMountainMask = Mathf.Max(0.3f, sampleContinentalMask * 0.8f);
            float testPlainsHeight = Mathf.Max(testBaseNoise * BaseHeight, BaseHeight * 0.3f);
            float testHillsHeight = testHillNoise * HillHeight * testHillMask;
            float testMountainsHeight = testMountainNoise * MountainHeight * 1.5f * testMountainMask;
            float testTotal = testPlainsHeight + testHillsHeight + testMountainsHeight;

            Debug.Log($"[NoiseGenerator] Called with world ({sampleWorldX:F1}, {sampleWorldZ:F1}):");
            Debug.Log($"   baseNoise={testBaseNoise:F3}, hillNoise={testHillNoise:F3}, mountainNoise={testMountainNoise:F3}");
            Debug.Log($"   continentalMask={sampleContinentalMask:F3}, hillMask={testHillMask:F3}, mountainMask={testMountainMask:F3}");
            Debug.Log($"   plains={testPlainsHeight:F2}, hills={testHillsHeight:F2}, mountains={testMountainsHeight:F2}, TOTAL={testTotal:F2}");

            Debug.Log($"[TerrainGenerator] DETAILED Height Breakdown at pixel ({x}, {z}) / world ({sampleWorldX:F1}, {sampleWorldZ:F1}):");
            Debug.Log($"   baseNoise={testBaseNoise:F3}, hillNoise={testHillNoise:F3}, mountainNoise={testMountainNoise:F3}");
            Debug.Log($"   hillMask={testHillMask:F3}, mountainMask={testMountainMask:F3}");
            Debug.Log($"   plains={testPlainsHeight:F2}, hills={testHillsHeight:F2}, mountains={testMountainsHeight:F2}");

            // Add river and lake information
            float sampleWaterCarving = NoiseGenerator.GetWaterCarvingFromPath(sampleWorldX, sampleWorldZ, riverPath, RiverWidth, RiverDepth, LakeRadius, LakeDepth);
            float testTotalAfterCarving = Mathf.Max(0f, testTotal - sampleWaterCarving);

            Debug.Log($"   Manual TOTAL (before carving)={testTotal:F2}, Manual TOTAL (after carving)={testTotalAfterCarving:F2}");
            Debug.Log($"   GetTerrainHeight Raw (after carving)={heightValue:F2} (should match Manual TOTAL after carving)");
            Debug.Log($"   After normalization: {normalizedHeight:F3} (before heightCurve)");
            Debug.Log($"   Height Params: baseHeight={BaseHeight:F1}, hillHeight={HillHeight:F1}, mountainHeight={MountainHeight:F1}, peakSharpness={PeakSharpness:F2}");

            Debug.Log($"[TerrainGenerator] Rivers and Lakes at world ({sampleWorldX:F1}, {sampleWorldZ:F1}):");
            Debug.Log($"   waterCarving={sampleWaterCarving:F2}, heightBeforeCarving={testTotal:F2}, heightAfterCarving={testTotalAfterCarving:F2}");
            Debug.Log($"   Water Params: riverWidth={RiverWidth:F1}, riverDepth={RiverDepth:F1}, lakeRadius={LakeRadius:F1}, lakeDepth={LakeDepth:F1}");
        }

        private void LogTerrainDistribution(float[,] heights, int width, int height)
        {
            int plainsCount = 0;
            int hillsCount = 0;
            int mountainsCount = 0;
            int peaksCount = 0;
            int totalPixels = width * height;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float h = heights[z, x];
                    if (h < 0.15f) // Plains: below 15%
                        plainsCount++;
                    else if (h < 0.4f) // Hills: 15% - 40%
                        hillsCount++;
                    else if (h < 0.6f) // Mountains: 40% - 60%
                        mountainsCount++;
                    else // High mountains: 60%+
                        mountainsCount++;

                    if (h > 0.6f) // Peaks: above 60%
                        peaksCount++;
                }
            }

            float plainsPercent = (plainsCount / (float)totalPixels) * 100f;
            float hillsPercent = (hillsCount / (float)totalPixels) * 100f;
            float mountainsPercent = (mountainsCount / (float)totalPixels) * 100f;
            float peaksPercent = (peaksCount / (float)totalPixels) * 100f;

            Debug.Log($"  Terrain distribution: Plains={plainsPercent:F0}%, Hills={hillsPercent:F0}%, Mountains={mountainsPercent:F0}%, Peaks={peaksPercent:F0}%");
        }
    }
}
