using UnityEngine;
using UnityEditor;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Verify what ranges are actually set on biomes
    /// </summary>
    public class VerifyBiomeRanges
    {
        [MenuItem("Hearthbound/Verify Biome Ranges (Check Current Settings)")]
        public static void Verify()
        {
            string[] guids = AssetDatabase.FindAssets("DefaultBiomeCollection t:BiomeCollection");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Not Found", "DefaultBiomeCollection not found!", "OK");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            BiomeCollection collection = AssetDatabase.LoadAssetAtPath<BiomeCollection>(path);
            
            if (collection == null || collection.biomes == null)
            {
                EditorUtility.DisplayDialog("Error", "BiomeCollection has no biomes!", "OK");
                return;
            }

            string message = "CURRENT BIOME RANGES:\n\n";
            foreach (BiomeData biome in collection.biomes)
            {
                if (biome == null) continue;
                
                message += $"• {biome.biomeName}:\n";
                message += $"  Height: {biome.heightRange.x:F2} - {biome.heightRange.y:F2}\n";
                message += $"  Temperature: {biome.temperatureRange.x:F2} - {biome.temperatureRange.y:F2}\n";
                message += $"  Humidity: {biome.humidityRange.x:F2} - {biome.humidityRange.y:F2}\n\n";
                
                // Check if Forest is too high
                if (biome.biomeName.ToLower().Contains("forest"))
                {
                    if (biome.heightRange.y > 0.25f)
                    {
                        message += "⚠️ WARNING: Forest max height is > 0.25! This allows forests on mountains!\n\n";
                    }
                }
                
                // Check if Rock starts too late
                if (biome.biomeName.ToLower().Contains("rock") || biome.biomeName.ToLower().Contains("mountain"))
                {
                    if (biome.heightRange.x > 0.25f)
                    {
                        message += "⚠️ WARNING: Rock starts too late (> 0.25)! This creates a gap where Forest can appear!\n\n";
                    }
                }
            }
            
            EditorUtility.DisplayDialog("Current Biome Ranges", message, "OK");
        }
    }
}

