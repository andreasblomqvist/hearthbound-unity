using UnityEngine;

namespace Hearthbound.World
{
    /// <summary>
    /// Terrain Style Preset ScriptableObject
    /// Defines a set of terrain generation parameters for different terrain styles
    /// </summary>
    [CreateAssetMenu(fileName = "NewTerrainStyle", menuName = "Hearthbound/Terrain Style Preset")]
    public class TerrainStylePreset : ScriptableObject
    {
        [Header("Preset Info")]
        [Tooltip("Name of this terrain style (e.g., 'Alpine Mountains', 'Rocky Mountains')")]
        public string styleName = "New Terrain Style";

        [Tooltip("Description of what this style creates")]
        [TextArea(2, 4)]
        public string description = "Description of terrain style";

        [Header("Terrain Size")]
        [Tooltip("Terrain width in world units")]
        public float terrainWidth = 8000f;

        [Tooltip("Terrain length in world units")]
        public float terrainLength = 8000f;

        [Tooltip("Maximum terrain height")]
        public float terrainHeight = 1000f;

        [Tooltip("Heightmap resolution (257, 513, 1025, 2049)")]
        public int heightmapResolution = 513;

        [Header("Height Generation")]
        [Tooltip("Base terrain height (plains/lowlands)")]
        public float baseHeight = 50f;

        [Tooltip("Gentle rolling hills")]
        public float hillHeight = 100f;

        [Tooltip("Tall dramatic mountains")]
        public float mountainHeight = 800f;
        
        [Header("Height Curve")]
        [Tooltip("Height curve for terrain distribution")]
        public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Noise Parameters (Advanced)")]
        [Tooltip("Continental mask threshold - controls where mountains appear (0.4-0.6 recommended)")]
        [Range(0.3f, 0.7f)]
        public float continentalThreshold = 0.5f;
        
        [Tooltip("Continental mask frequency - controls size of mountain regions (0.00005 for vast plains, lower = larger regions)")]
        [Range(0.00001f, 0.001f)]
        public float continentalMaskFrequency = 0.00005f;
        
        [Tooltip("Domain warp strength for mountain ranges (100-200 recommended)")]
        [Range(100f, 250f)]
        public float warpStrength = 150f;
        
        [Tooltip("Mountain range frequency (0.0005-0.001 recommended)")]
        [Range(0.0005f, 0.002f)]
        public float mountainFrequency = 0.0008f;
        
        [Tooltip("Power curve exponent for peak sharpness (1.0-1.5 recommended)")]
        [Range(1.0f, 2.0f)]
        public float peakSharpness = 1.3f;

        [Header("Cliff Generation")]
        [Range(0f, 1f)]
        [Tooltip("Strength of cliff features (0 = none, 1 = dramatic)")]
        public float cliffStrength = 0.4f;

        [Range(0.005f, 0.02f)]
        [Tooltip("Frequency of cliff patterns (lower = larger cliffs)")]
        public float cliffFrequency = 0.01f;

        [Range(0.5f, 0.8f)]
        [Tooltip("Minimum continental mask value for cliff generation")]
        public float cliffThreshold = 0.6f;

        [Header("Hydraulic Erosion")]
        [Tooltip("Apply hydraulic erosion simulation")]
        public bool applyErosion = true;

        [Range(1000, 50000)]
        [Tooltip("Number of water droplets to simulate (more = more erosion)")]
        public int erosionIterations = 10000;

        [Range(0.1f, 1f)]
        [Tooltip("How aggressively terrain is eroded")]
        public float erosionStrength = 0.3f;

        [Range(1f, 10f)]
        [Tooltip("Amount of sediment water can carry")]
        public float sedimentCapacity = 4f;

        [Range(0.01f, 0.2f)]
        [Tooltip("Water evaporation rate per step")]
        public float evaporationRate = 0.05f;

        [Header("Biome Height Thresholds")]
        [Range(0f, 1f)]
        [Tooltip("Normalized height for water (0-1)")]
        public float waterHeight = 0.05f;

        [Range(0f, 1f)]
        [Tooltip("Normalized height for grass (0-1)")]
        public float grassHeight = 0.3f;

        [Range(0f, 1f)]
        [Tooltip("Normalized height for rock (0-1)")]
        public float rockHeight = 0.6f;

        [Range(0f, 1f)]
        [Tooltip("Normalized height for snow (0-1)")]
        public float snowHeight = 0.7f;

        /// <summary>
        /// Apply this preset to a TerrainGenerator component
        /// </summary>
        public void ApplyTo(TerrainGenerator terrainGenerator)
        {
            if (terrainGenerator == null)
            {
                Debug.LogError("Cannot apply preset: TerrainGenerator is null!");
                return;
            }
            
            terrainGenerator.SetBaseHeight(baseHeight);
            terrainGenerator.SetHillHeight(hillHeight);
            terrainGenerator.SetMountainHeight(mountainHeight);
            terrainGenerator.SetHeightCurve(heightCurve);
            terrainGenerator.SetContinentalThreshold(continentalThreshold);
            terrainGenerator.SetContinentalMaskFrequency(continentalMaskFrequency);
            terrainGenerator.SetWarpStrength(warpStrength);
            terrainGenerator.SetMountainFrequency(mountainFrequency);
            terrainGenerator.SetPeakSharpness(peakSharpness);

            // Apply cliff parameters
            terrainGenerator.SetCliffStrength(cliffStrength);
            terrainGenerator.SetCliffFrequency(cliffFrequency);
            terrainGenerator.SetCliffThreshold(cliffThreshold);

            // Apply terrain size
            terrainGenerator.SetTerrainSize(terrainWidth, terrainLength, terrainHeight);
            terrainGenerator.SetHeightmapResolution(heightmapResolution);

            // Apply biome heights
            terrainGenerator.SetWaterHeight(waterHeight);
            terrainGenerator.SetGrassHeight(grassHeight);
            terrainGenerator.SetRockHeight(rockHeight);
            terrainGenerator.SetSnowHeight(snowHeight);

            Debug.Log($"✅ Applied terrain style preset: {styleName}");
        }
    }
}

