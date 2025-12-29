using UnityEngine;
using UnityEditor;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Force regenerate all biome textures to ensure color updates take effect
    /// </summary>
    public class ForceRegenerateBiomeTextures
    {
        [MenuItem("Hearthbound/Force Regenerate Biome Textures")]
        public static void Regenerate()
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

            int clearedCount = 0;
            foreach (BiomeData biome in collection.biomes)
            {
                if (biome == null || biome.terrainLayers == null)
                    continue;

                foreach (var layer in biome.terrainLayers)
                {
                    if (layer != null)
                    {
                        // Destroy old texture if it exists
                        if (layer.diffuseTexture != null)
                        {
                            Object.DestroyImmediate(layer.diffuseTexture, true);
                            layer.diffuseTexture = null;
                            clearedCount++;
                        }
                    }
                }
                
                EditorUtility.SetDirty(biome);
            }

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", $"Cleared {clearedCount} old textures.\n\nNow:\n1. Regenerate your terrain\n2. New colored textures will be created from the color values", "OK");
        }
    }
}

