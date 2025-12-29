using UnityEngine;
using UnityEditor;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Editor utility to update biome colors in BiomeCollection ScriptableObjects
    /// </summary>
    public class UpdateBiomeColors : EditorWindow
    {
        private BiomeCollection biomeCollection;
        private bool useUpdatedColors = true;

        [MenuItem("Hearthbound/Update Biome Colors")]
        public static void ShowWindow()
        {
            GetWindow<UpdateBiomeColors>("Update Biome Colors");
        }

        private void OnGUI()
        {
            GUILayout.Label("Update Biome Colors", EditorStyles.boldLabel);
            GUILayout.Space(10);

            biomeCollection = (BiomeCollection)EditorGUILayout.ObjectField("Biome Collection", biomeCollection, typeof(BiomeCollection), false);

            GUILayout.Space(10);
            EditorGUILayout.HelpBox("This will update all biome colors to be more distinct and visible:", MessageType.Info);
            
            EditorGUILayout.LabelField("Water: Cyan/Blue (0.1, 0.5, 0.9)");
            EditorGUILayout.LabelField("Plains: Yellow-Green (0.6, 0.9, 0.3)");
            EditorGUILayout.LabelField("Forest: Deep Green (0.05, 0.5, 0.15)");
            EditorGUILayout.LabelField("Rock: Tan (0.7, 0.6, 0.4)");
            EditorGUILayout.LabelField("Snow: White (1.0, 1.0, 1.0)");
            EditorGUILayout.LabelField("Dirt: Dark Brown (0.3, 0.2, 0.1)");

            GUILayout.Space(10);
            if (GUILayout.Button("Update Colors", GUILayout.Height(30)))
            {
                if (biomeCollection == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign a BiomeCollection asset first!", "OK");
                    return;
                }

                UpdateColors();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Find DefaultBiomeCollection", GUILayout.Height(25)))
            {
                string[] guids = AssetDatabase.FindAssets("DefaultBiomeCollection t:BiomeCollection");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    biomeCollection = AssetDatabase.LoadAssetAtPath<BiomeCollection>(path);
                    EditorGUIUtility.PingObject(biomeCollection);
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found", "DefaultBiomeCollection not found in Assets/Resources/Biomes/", "OK");
                }
            }
        }

        private void UpdateColors()
        {
            if (biomeCollection == null || biomeCollection.biomes == null)
            {
                EditorUtility.DisplayDialog("Error", "BiomeCollection has no biomes defined!", "OK");
                return;
            }

            int updatedCount = 0;
            
            foreach (BiomeData biome in biomeCollection.biomes)
            {
                if (biome == null || biome.terrainLayers == null || biome.terrainLayers.Length == 0)
                    continue;

                Color newColor = GetColorForBiome(biome.biomeName);
                
                // Update all terrain layers for this biome
                foreach (var layer in biome.terrainLayers)
                {
                    if (layer != null)
                    {
                        layer.color = newColor;
                        // Clear existing texture so it will be recreated with new color
                        layer.diffuseTexture = null;
                        updatedCount++;
                    }
                }
                
                // Mark biome asset as dirty
                EditorUtility.SetDirty(biome);
            }

            EditorUtility.SetDirty(biomeCollection);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", $"Updated colors for {updatedCount} terrain layers in {biomeCollection.biomes.Length} biomes!\n\nYou'll need to regenerate the terrain for changes to take effect.", "OK");
        }

        private Color GetColorForBiome(string biomeName)
        {
            string nameLower = biomeName.ToLower();
            
            if (nameLower.Contains("water"))
                return new Color(0.1f, 0.5f, 0.9f); // Cyan/Blue
            else if (nameLower.Contains("plains") || nameLower.Contains("grass"))
                return new Color(0.6f, 0.9f, 0.3f); // Yellow-Green
            else if (nameLower.Contains("forest"))
                return new Color(0.05f, 0.5f, 0.15f); // Deep Green
            else if (nameLower.Contains("rock") || nameLower.Contains("mountain"))
                return new Color(0.7f, 0.6f, 0.4f); // Tan
            else if (nameLower.Contains("snow"))
                return new Color(1.0f, 1.0f, 1.0f); // White
            else if (nameLower.Contains("dirt"))
                return new Color(0.3f, 0.2f, 0.1f); // Dark Brown
            else
                return Color.white; // Default
        }
    }
}

