using UnityEngine;
using UnityEditor;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Custom inspector for BiomeData to make range editing easier
    /// </summary>
    [CustomEditor(typeof(BiomeData))]
    public class BiomeDataEditor : UnityEditor.Editor
    {
        private bool showRangeHelpers = true;

        public override void OnInspectorGUI()
        {
            // Draw default inspector first
            DrawDefaultInspector();

            BiomeData biomeData = (BiomeData)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(5);

            // Helper section for adjusting ranges
            showRangeHelpers = EditorGUILayout.Foldout(showRangeHelpers, "Range Adjustment Helpers", true);
            if (showRangeHelpers)
            {
                EditorGUILayout.HelpBox("Use these helpers to quickly adjust biome ranges. Remember to regenerate terrain after changes!", MessageType.Info);

                EditorGUILayout.Space(5);
                
                // Height Range
                EditorGUILayout.LabelField("Height Range (0.0 = low, 1.0 = high elevation)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                biomeData.heightRange.x = EditorGUILayout.Slider("Min", biomeData.heightRange.x, 0f, 1f);
                biomeData.heightRange.y = EditorGUILayout.Slider("Max", biomeData.heightRange.y, 0f, 1f);
                EditorGUILayout.EndHorizontal();
                
                // Ensure min <= max
                if (biomeData.heightRange.x > biomeData.heightRange.y)
                {
                    biomeData.heightRange.y = biomeData.heightRange.x;
                }
                
                EditorGUILayout.Space(5);
                
                // Temperature Range
                EditorGUILayout.LabelField("Temperature Range (0.0 = cold, 1.0 = hot)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                biomeData.temperatureRange.x = EditorGUILayout.Slider("Min", biomeData.temperatureRange.x, 0f, 1f);
                biomeData.temperatureRange.y = EditorGUILayout.Slider("Max", biomeData.temperatureRange.y, 0f, 1f);
                EditorGUILayout.EndHorizontal();
                
                if (biomeData.temperatureRange.x > biomeData.temperatureRange.y)
                {
                    biomeData.temperatureRange.y = biomeData.temperatureRange.x;
                }
                
                EditorGUILayout.Space(5);
                
                // Humidity Range
                EditorGUILayout.LabelField("Humidity Range (0.0 = dry, 1.0 = wet)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                biomeData.humidityRange.x = EditorGUILayout.Slider("Min", biomeData.humidityRange.x, 0f, 1f);
                biomeData.humidityRange.y = EditorGUILayout.Slider("Max", biomeData.humidityRange.y, 0f, 1f);
                EditorGUILayout.EndHorizontal();
                
                if (biomeData.humidityRange.x > biomeData.humidityRange.y)
                {
                    biomeData.humidityRange.y = biomeData.humidityRange.x;
                }
                
                EditorGUILayout.Space(5);
                
                // Blend Strength
                EditorGUILayout.LabelField("Blend Strength (higher = sharper boundaries)", EditorStyles.boldLabel);
                biomeData.blendStrength = EditorGUILayout.Slider("Strength", biomeData.blendStrength, 1f, 10f);
                
                EditorGUILayout.Space(5);
                
                // Preset buttons
                EditorGUILayout.LabelField("Quick Presets:", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                string nameLower = biomeData.biomeName.ToLower();
                if (nameLower.Contains("plains") || nameLower.Contains("grass"))
                {
                    if (GUILayout.Button("Set: Low Elevation, Dry"))
                    {
                        biomeData.heightRange = new Vector2(0.0f, 0.20f);
                        biomeData.temperatureRange = new Vector2(0.4f, 0.8f);
                        biomeData.humidityRange = new Vector2(0.0f, 0.5f);
                    }
                }
                else if (nameLower.Contains("forest"))
                {
                    if (GUILayout.Button("Set: Low Elevation, Wet"))
                    {
                        biomeData.heightRange = new Vector2(0.0f, 0.20f);
                        biomeData.temperatureRange = new Vector2(0.3f, 0.7f);
                        biomeData.humidityRange = new Vector2(0.5f, 1.0f);
                        biomeData.blendStrength = 5f;
                    }
                }
                else if (nameLower.Contains("rock") || nameLower.Contains("mountain"))
                {
                    if (GUILayout.Button("Set: Mountains (High Elevation)"))
                    {
                        biomeData.heightRange = new Vector2(0.20f, 0.75f);
                        biomeData.temperatureRange = new Vector2(0.2f, 0.8f);
                        biomeData.humidityRange = new Vector2(0.0f, 0.4f);
                        biomeData.blendStrength = 5f;
                    }
                }
                else if (nameLower.Contains("snow"))
                {
                    if (GUILayout.Button("Set: High Peaks, Cold"))
                    {
                        biomeData.heightRange = new Vector2(0.7f, 1.0f);
                        biomeData.temperatureRange = new Vector2(0.0f, 0.3f);
                        biomeData.humidityRange = new Vector2(0.0f, 1.0f);
                    }
                }
                else if (nameLower.Contains("water"))
                {
                    if (GUILayout.Button("Set: Low Elevation, Very Wet"))
                    {
                        biomeData.heightRange = new Vector2(0.0f, 0.15f);
                        biomeData.temperatureRange = new Vector2(0.0f, 1.0f);
                        biomeData.humidityRange = new Vector2(0.8f, 1.0f);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Mark as dirty if any changes were made
                if (GUI.changed)
                {
                    EditorUtility.SetDirty(biomeData);
                }
            }
        }
    }
}

