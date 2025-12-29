using UnityEngine;
using UnityEditor;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Quick check to see what biomes exist and their colors
    /// </summary>
    public class QuickBiomeCheck
    {
        [MenuItem("Hearthbound/Quick Check - What Biomes Exist?")]
        public static void CheckBiomes()
        {
            TerrainGenerator terrainGen = Object.FindObjectOfType<TerrainGenerator>();
            if (terrainGen == null)
            {
                EditorUtility.DisplayDialog("Not Found", "No TerrainGenerator found in scene!", "OK");
                return;
            }

            System.Reflection.FieldInfo biomeCollectionField = typeof(TerrainGenerator).GetField("biomeCollection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            BiomeCollection collection = biomeCollectionField?.GetValue(terrainGen) as BiomeCollection;

            if (collection == null)
            {
                EditorUtility.DisplayDialog("Info", "TerrainGenerator is using DEFAULT biome colors (not BiomeCollection).\n\nRegenerate terrain to see new default colors!", "OK");
                return;
            }

            string message = $"Found {collection.biomes.Length} biomes in {collection.name}:\n\n";
            foreach (BiomeData biome in collection.biomes)
            {
                if (biome == null) continue;
                
                string colorInfo = "No layers";
                if (biome.terrainLayers != null && biome.terrainLayers.Length > 0)
                {
                    var layer = biome.terrainLayers[0];
                    if (layer != null)
                    {
                        Color c = layer.color;
                        if (layer.diffuseTexture != null)
                        {
                            colorInfo = $"Has texture: {layer.diffuseTexture.name} (color backup: RGB({c.r:F2}, {c.g:F2}, {c.b:F2}))";
                        }
                        else
                        {
                            colorInfo = $"Color: RGB({c.r:F2}, {c.g:F2}, {c.b:F2})";
                        }
                    }
                }
                message += $"• {biome.biomeName}: {colorInfo}\n";
            }
            
            message += "\n⚠️ IMPORTANT: You must REGENERATE the terrain for color changes to appear!";
            
            EditorUtility.DisplayDialog("Biome Colors Check", message, "OK");
        }
    }
}

