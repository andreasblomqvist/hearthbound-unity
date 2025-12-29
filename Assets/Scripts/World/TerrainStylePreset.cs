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
        
        [Header("Height Generation")]
        [Tooltip("Base terrain height (plains/lowlands)")]
        public float baseHeight = 60f;
        
        [Tooltip("Gentle hills for variation")]
        public float hillHeight = 140f;
        
        [Tooltip("Clustered mountains")]
        public float mountainHeight = 550f;
        
        [Header("Height Curve")]
        [Tooltip("Height curve for terrain distribution")]
        public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Texture Splatting - Height Thresholds")]
        [Tooltip("Below this = water/beach")]
        [Range(0f, 0.2f)]
        public float waterHeight = 0.1f;
        
        [Tooltip("Plains/grass biome")]
        [Range(0.1f, 0.4f)]
        public float grassHeight = 0.3f;
        
        [Tooltip("Mountains start")]
        [Range(0.5f, 0.7f)]
        public float rockHeight = 0.6f;
        
        [Tooltip("Snow on peaks")]
        [Range(0.6f, 0.9f)]
        public float snowHeight = 0.7f;
        
        [Header("Noise Parameters (Advanced)")]
        [Tooltip("Continental mask threshold - controls where mountains appear (0.4-0.6 recommended)")]
        [Range(0.3f, 0.7f)]
        public float continentalThreshold = 0.5f;
        
        [Tooltip("Continental mask frequency - controls size of mountain regions (0.0002-0.0005 recommended, lower = larger regions)")]
        [Range(0.0001f, 0.001f)]
        public float continentalMaskFrequency = 0.0003f;
        
        [Tooltip("Domain warp strength for mountain ranges (100-200 recommended)")]
        [Range(100f, 250f)]
        public float warpStrength = 150f;
        
        [Tooltip("Mountain range frequency (0.0005-0.001 recommended)")]
        [Range(0.0005f, 0.002f)]
        public float mountainFrequency = 0.0008f;
        
        [Tooltip("Power curve exponent for peak sharpness (1.0-1.5 recommended)")]
        [Range(1.0f, 2.0f)]
        public float peakSharpness = 1.3f;
        
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
            terrainGenerator.SetWaterHeight(waterHeight);
            terrainGenerator.SetGrassHeight(grassHeight);
            terrainGenerator.SetRockHeight(rockHeight);
            terrainGenerator.SetSnowHeight(snowHeight);
            terrainGenerator.SetContinentalThreshold(continentalThreshold);
            terrainGenerator.SetContinentalMaskFrequency(continentalMaskFrequency);
            terrainGenerator.SetWarpStrength(warpStrength);
            terrainGenerator.SetMountainFrequency(mountainFrequency);
            terrainGenerator.SetPeakSharpness(peakSharpness);
            
            Debug.Log($"✅ Applied terrain style preset: {styleName}");
        }
    }
}

