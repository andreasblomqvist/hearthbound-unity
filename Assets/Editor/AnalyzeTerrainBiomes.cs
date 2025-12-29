using UnityEngine;
using UnityEditor;
using Hearthbound.World;
using System.Collections.Generic;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Editor utility to analyze biome distribution on the terrain
    /// </summary>
    public class AnalyzeTerrainBiomes : EditorWindow
    {
        private TerrainGenerator terrainGenerator;
        private int sampleCount = 1000;
        private Dictionary<string, int> biomeCounts = new Dictionary<string, int>();

        [MenuItem("Hearthbound/Analyze Terrain Biomes")]
        public static void ShowWindow()
        {
            GetWindow<AnalyzeTerrainBiomes>("Analyze Terrain Biomes");
        }

        private void OnGUI()
        {
            GUILayout.Label("Analyze Terrain Biomes", EditorStyles.boldLabel);
            GUILayout.Space(10);

            terrainGenerator = (TerrainGenerator)EditorGUILayout.ObjectField("Terrain Generator", terrainGenerator, typeof(TerrainGenerator), true);

            if (terrainGenerator == null)
            {
                if (GUILayout.Button("Find TerrainGenerator in Scene"))
                {
                    terrainGenerator = Object.FindObjectOfType<TerrainGenerator>();
                }
            }

            GUILayout.Space(10);
            sampleCount = EditorGUILayout.IntField("Sample Count", sampleCount);
            EditorGUILayout.HelpBox("This will sample random points on the terrain to see what biomes exist.", MessageType.Info);

            GUILayout.Space(10);
            if (GUILayout.Button("Analyze Biomes", GUILayout.Height(30)))
            {
                if (terrainGenerator == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign a TerrainGenerator component first!", "OK");
                    return;
                }

                AnalyzeBiomes();
            }

            GUILayout.Space(20);
            if (biomeCounts.Count > 0)
            {
                GUILayout.Label("Biome Distribution:", EditorStyles.boldLabel);
                GUILayout.Space(5);

                float total = 0f;
                foreach (var count in biomeCounts.Values)
                {
                    total += count;
                }

                // Sort by count (descending)
                var sorted = new List<KeyValuePair<string, int>>(biomeCounts);
                sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

                foreach (var kvp in sorted)
                {
                    float percentage = (kvp.Value / total) * 100f;
                    string bar = "";
                    int barLength = Mathf.RoundToInt(percentage / 2f);
                    for (int i = 0; i < barLength; i++)
                    {
                        bar += "█";
                    }
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{kvp.Key}:", GUILayout.Width(80));
                    GUILayout.Label(bar, GUILayout.Width(150));
                    GUILayout.Label($"{percentage:F1}% ({kvp.Value} samples)", GUILayout.Width(150));
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(10);
                EditorGUILayout.HelpBox($"Total samples: {total} biomes found: {biomeCounts.Count}", MessageType.Info);
            }
        }

        private void AnalyzeBiomes()
        {
            biomeCounts.Clear();

            Terrain terrain = terrainGenerator.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                EditorUtility.DisplayDialog("Error", "TerrainGenerator has no Terrain component or TerrainData!", "OK");
                return;
            }

            Vector3 terrainSize = terrain.terrainData.size;
            Vector3 terrainPos = terrain.transform.position;
            int seed = 12345; // Use a consistent seed for analysis

            Debug.Log($"🔍 Analyzing biome distribution (sampling {sampleCount} random points)...");

            for (int i = 0; i < sampleCount; i++)
            {
                float x = Random.Range(0f, terrainSize.x) + terrainPos.x;
                float z = Random.Range(0f, terrainSize.z) + terrainPos.z;
                Vector3 worldPos = new Vector3(x, 0, z);

                // Get terrain height
                float height = terrainGenerator.GetHeightAtPosition(worldPos);
                worldPos.y = height;

                // Get biome
                string biomeName = "Unknown";
                BiomeData biomeData = terrainGenerator.GetBiomeDataAtPosition(worldPos, seed);
                
                if (biomeData != null)
                {
                    biomeName = biomeData.biomeName;
                }
                else
                {
                    biomeName = terrainGenerator.GetBiomeAtPosition(worldPos, seed);
                }

                if (!biomeCounts.ContainsKey(biomeName))
                {
                    biomeCounts[biomeName] = 0;
                }
                biomeCounts[biomeName]++;
            }

            Debug.Log($"✅ Biome analysis complete! Found {biomeCounts.Count} different biomes.");
            foreach (var kvp in biomeCounts)
            {
                float percentage = (kvp.Value / (float)sampleCount) * 100f;
                Debug.Log($"  {kvp.Key}: {percentage:F1}% ({kvp.Value} samples)");
            }
        }
    }
}

