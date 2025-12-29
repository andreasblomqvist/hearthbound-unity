using UnityEngine;
using UnityEditor;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Tool to adjust biome ranges for better diversity
    /// </summary>
    public class AdjustBiomeRangesForDiversity : EditorWindow
    {
        private BiomeCollection biomeCollection;
        private bool showPreview = true;

        [MenuItem("Hearthbound/Adjust Biome Ranges for Diversity")]
        public static void ShowWindow()
        {
            GetWindow<AdjustBiomeRangesForDiversity>("Adjust Biome Ranges");
        }

        private void OnGUI()
        {
            GUILayout.Label("Adjust Biome Ranges for Diversity", EditorStyles.boldLabel);
            GUILayout.Space(10);

            biomeCollection = (BiomeCollection)EditorGUILayout.ObjectField("Biome Collection", biomeCollection, typeof(BiomeCollection), false);

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
                    EditorUtility.DisplayDialog("Not Found", "DefaultBiomeCollection not found!", "OK");
                }
            }

            GUILayout.Space(20);
            
            if (showPreview)
            {
                EditorGUILayout.HelpBox("This will adjust biome ranges to create better diversity:", MessageType.Info);
                GUILayout.Space(10);
                
                EditorGUILayout.LabelField("Plains: Height 0.0-0.20, Temp 0.4-0.8, Humidity 0.0-0.5", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("Forest: Height 0.0-0.20, Temp 0.3-0.7, Humidity 0.5-1.0 (VERY LOW + sharp boundaries)", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("Rock: Height 0.20-0.75, Temp 0.2-0.8, Humidity 0.0-0.4 (starts at 0.20, sharp boundaries)", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("Snow: Height 0.7-1.0, Temp 0.0-0.3, Humidity 0.0-1.0", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("Water: Height 0.0-0.15, Temp 0.0-1.0, Humidity 0.8-1.0", EditorStyles.wordWrappedLabel);
                
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("⚠️ This will modify your BiomeCollection asset. Make sure to back it up if needed!", MessageType.Warning);
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Apply Improved Biome Ranges", GUILayout.Height(40)))
            {
                if (biomeCollection == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign a BiomeCollection asset first!", "OK");
                    return;
                }

                bool confirmed = EditorUtility.DisplayDialog("Confirm", 
                    "This will modify the biome ranges in your BiomeCollection.\n\n" +
                    "This may change your terrain appearance significantly.\n\n" +
                    "Continue?", "Yes", "Cancel");
                
                if (confirmed)
                {
                    ApplyRanges();
                }
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Show Current Ranges", GUILayout.Height(30)))
            {
                ShowCurrentRanges();
            }
        }

        private void ApplyRanges()
        {
            if (biomeCollection == null || biomeCollection.biomes == null)
            {
                EditorUtility.DisplayDialog("Error", "BiomeCollection has no biomes defined!", "OK");
                return;
            }

            int updatedCount = 0;
            
            foreach (BiomeData biome in biomeCollection.biomes)
            {
                if (biome == null)
                    continue;

                string nameLower = biome.biomeName.ToLower();
                
                if (nameLower.Contains("water"))
                {
                    biome.heightRange = new Vector2(0.0f, 0.15f);
                    biome.temperatureRange = new Vector2(0.0f, 1.0f);
                    biome.humidityRange = new Vector2(0.8f, 1.0f);
                    updatedCount++;
                }
                else if (nameLower.Contains("plains") || nameLower.Contains("grass"))
                {
                    // Plains: low elevation only, moderate temp, LOW humidity (dry grasslands)
                    biome.heightRange = new Vector2(0.0f, 0.20f);
                    biome.temperatureRange = new Vector2(0.4f, 0.8f);
                    biome.humidityRange = new Vector2(0.0f, 0.5f);
                    updatedCount++;
                }
                else if (nameLower.Contains("forest"))
                {
                    // Forests: VERY LOW elevation ONLY - account for exponential falloff
                    // Make it even lower to prevent appearing on mountains due to smooth falloff
                    biome.heightRange = new Vector2(0.0f, 0.20f);
                    biome.temperatureRange = new Vector2(0.3f, 0.7f);
                    biome.humidityRange = new Vector2(0.5f, 1.0f);
                    // Increase blend strength to make boundaries sharper (less falloff)
                    biome.blendStrength = 5f; // Higher = sharper boundaries
                    updatedCount++;
                }
                else if (nameLower.Contains("rock") || nameLower.Contains("mountain"))
                {
                    // Rocks start EARLIER - create hard cutoff, no overlap with Forest
                    biome.heightRange = new Vector2(0.20f, 0.75f);
                    biome.temperatureRange = new Vector2(0.2f, 0.8f);
                    biome.humidityRange = new Vector2(0.0f, 0.4f);
                    // Increase blend strength to claim territory more strongly
                    biome.blendStrength = 5f;
                    updatedCount++;
                }
                else if (nameLower.Contains("snow"))
                {
                    biome.heightRange = new Vector2(0.7f, 1.0f);
                    biome.temperatureRange = new Vector2(0.0f, 0.3f);
                    biome.humidityRange = new Vector2(0.0f, 1.0f);
                    updatedCount++;
                }
                
                EditorUtility.SetDirty(biome);
            }

            EditorUtility.SetDirty(biomeCollection);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", 
                $"Updated ranges for {updatedCount} biomes!\n\n" +
                "⚠️ You MUST regenerate your terrain for changes to take effect.", "OK");
        }

        private void ShowCurrentRanges()
        {
            if (biomeCollection == null || biomeCollection.biomes == null)
            {
                EditorUtility.DisplayDialog("Error", "No BiomeCollection assigned!", "OK");
                return;
            }

            string message = "Current Biome Ranges:\n\n";
            foreach (BiomeData biome in biomeCollection.biomes)
            {
                if (biome == null) continue;
                
                message += $"• {biome.biomeName}:\n";
                message += $"  Height: {biome.heightRange.x:F2} - {biome.heightRange.y:F2}\n";
                message += $"  Temperature: {biome.temperatureRange.x:F2} - {biome.temperatureRange.y:F2}\n";
                message += $"  Humidity: {biome.humidityRange.x:F2} - {biome.humidityRange.y:F2}\n\n";
            }
            
            EditorUtility.DisplayDialog("Current Ranges", message, "OK");
        }
    }
}

