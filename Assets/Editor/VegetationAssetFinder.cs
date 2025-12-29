using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Helper tool to find and organize vegetation assets from imported packages
    /// </summary>
    public class VegetationAssetFinder : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<GameObject> treePrefabs = new List<GameObject>();
        private List<GameObject> bushPrefabs = new List<GameObject>();
        private List<GameObject> rockPrefabs = new List<GameObject>();
        private List<GameObject> otherPrefabs = new List<GameObject>();

        [MenuItem("Hearthbound/Find Vegetation Assets")]
        public static void ShowWindow()
        {
            GetWindow<VegetationAssetFinder>("Vegetation Asset Finder");
        }

        private void OnEnable()
        {
            ScanForAssets();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Vegetation Asset Finder", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("This tool scans your project for tree, bush, and rock assets.\nClick 'Scan Assets' to refresh the list.", MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("Scan Assets", GUILayout.Height(30)))
            {
                ScanForAssets();
            }

            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Trees
            EditorGUILayout.LabelField($"Trees Found: {treePrefabs.Count}", EditorStyles.boldLabel);
            foreach (var prefab in treePrefabs)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                if (GUILayout.Button("Create Prefab", GUILayout.Width(100)))
                {
                    CreatePrefabCopy(prefab, "Trees");
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Bushes
            EditorGUILayout.LabelField($"Bushes/Plants Found: {bushPrefabs.Count}", EditorStyles.boldLabel);
            foreach (var prefab in bushPrefabs)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                if (GUILayout.Button("Create Prefab", GUILayout.Width(100)))
                {
                    CreatePrefabCopy(prefab, "Props");
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Rocks
            EditorGUILayout.LabelField($"Rocks Found: {rockPrefabs.Count}", EditorStyles.boldLabel);
            foreach (var prefab in rockPrefabs)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                if (GUILayout.Button("Create Prefab", GUILayout.Width(100)))
                {
                    CreatePrefabCopy(prefab, "Props");
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // Other
            if (otherPrefabs.Count > 0)
            {
                EditorGUILayout.LabelField($"Other Assets: {otherPrefabs.Count}", EditorStyles.boldLabel);
                foreach (var prefab in otherPrefabs.Take(10)) // Limit display
                {
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                }
                if (otherPrefabs.Count > 10)
                {
                    EditorGUILayout.LabelField($"... and {otherPrefabs.Count - 10} more", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void ScanForAssets()
        {
            treePrefabs.Clear();
            bushPrefabs.Clear();
            rockPrefabs.Clear();
            otherPrefabs.Clear();

            // Find all prefabs in the project
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/TriForge Assets" });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                string nameLower = prefab.name.ToLower();
                string pathLower = path.ToLower();

                // Categorize based on name and path
                if (nameLower.Contains("tree") || pathLower.Contains("tree"))
                {
                    treePrefabs.Add(prefab);
                }
                else if (nameLower.Contains("bush") || nameLower.Contains("plant") || 
                         nameLower.Contains("grass") || pathLower.Contains("bush") || 
                         pathLower.Contains("plant") || pathLower.Contains("vegetation"))
                {
                    bushPrefabs.Add(prefab);
                }
                else if (nameLower.Contains("rock") || nameLower.Contains("stone") || 
                         pathLower.Contains("rock") || pathLower.Contains("stone"))
                {
                    rockPrefabs.Add(prefab);
                }
                else
                {
                    otherPrefabs.Add(prefab);
                }
            }

            // Also check for FBX/Mesh files that could be used as prefabs
            guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/TriForge Assets" });
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (model == null) continue;

                string nameLower = model.name.ToLower();
                string pathLower = path.ToLower();

                // Only add if we don't already have a prefab version
                if (nameLower.Contains("tree") || pathLower.Contains("tree"))
                {
                    if (!treePrefabs.Any(p => p.name == model.name))
                    {
                        treePrefabs.Add(model);
                    }
                }
                else if (nameLower.Contains("bush") || nameLower.Contains("plant") || 
                         nameLower.Contains("grass") || pathLower.Contains("bush") || 
                         pathLower.Contains("plant") || pathLower.Contains("vegetation"))
                {
                    if (!bushPrefabs.Any(p => p.name == model.name))
                    {
                        bushPrefabs.Add(model);
                    }
                }
                else if (nameLower.Contains("rock") || nameLower.Contains("stone") || 
                         pathLower.Contains("rock") || pathLower.Contains("stone"))
                {
                    if (!rockPrefabs.Any(p => p.name == model.name))
                    {
                        rockPrefabs.Add(model);
                    }
                }
            }

            Debug.Log($"✅ Scan complete: {treePrefabs.Count} trees, {bushPrefabs.Count} bushes, {rockPrefabs.Count} rocks, {otherPrefabs.Count} other assets");
        }

        private void CreatePrefabCopy(GameObject source, string folder)
        {
            string targetPath = $"Assets/Prefabs/{folder}/{source.name}.prefab";
            
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(targetPath);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            // Check if prefab already exists
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            if (existing != null)
            {
                Debug.LogWarning($"Prefab already exists at {targetPath}. Skipping.");
                return;
            }

            // Create prefab variant or copy
            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(prefabInstance, targetPath);
            DestroyImmediate(prefabInstance);

            Debug.Log($"✅ Created prefab: {targetPath}");
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(newPrefab);
        }
    }
}

