using UnityEditor;
using UnityEngine;
using System.IO;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Quick script to organize Fantasy Worlds assets into project prefab folders
    /// </summary>
    public class OrganizeFantasyWorldsAssets
    {
        [MenuItem("Hearthbound/Organize Fantasy Worlds Assets")]
        public static void OrganizeAssets()
        {
            int copied = 0;
            int skipped = 0;

            // Trees
            string[] treeAssets = new string[]
            {
                "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_Tree_M_2.prefab",
                "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_TreeSapling_02B.prefab"
            };

            // Bushes/Plants
            string[] bushAssets = new string[]
            {
                "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_ForestPlant_B_02.prefab",
                "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_Grass_M_1.prefab"
            };

            // Rocks
            string[] rockAssets = new string[]
            {
                "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_Rock_01.prefab",
                "Assets/TriForge Assets/Fantasy Worlds - DEMO Content/Prefabs/P_fwOF_Stone_01.prefab"
            };

            // Copy trees
            foreach (string assetPath in treeAssets)
            {
                if (File.Exists(assetPath))
                {
                    string fileName = Path.GetFileName(assetPath);
                    string targetPath = $"Assets/Prefabs/Trees/{fileName}";
                    
                    if (!File.Exists(targetPath))
                    {
                        // Ensure directory exists
                        string directory = Path.GetDirectoryName(targetPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        AssetDatabase.CopyAsset(assetPath, targetPath);
                        copied++;
                        Debug.Log($"✅ Copied: {fileName} → Prefabs/Trees/");
                    }
                    else
                    {
                        skipped++;
                        Debug.Log($"⏭️ Skipped (already exists): {fileName}");
                    }
                }
            }

            // Copy bushes
            foreach (string assetPath in bushAssets)
            {
                if (File.Exists(assetPath))
                {
                    string fileName = Path.GetFileName(assetPath);
                    string targetPath = $"Assets/Prefabs/Props/{fileName}";
                    
                    if (!File.Exists(targetPath))
                    {
                        // Ensure directory exists
                        string directory = Path.GetDirectoryName(targetPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        AssetDatabase.CopyAsset(assetPath, targetPath);
                        copied++;
                        Debug.Log($"✅ Copied: {fileName} → Prefabs/Props/");
                    }
                    else
                    {
                        skipped++;
                        Debug.Log($"⏭️ Skipped (already exists): {fileName}");
                    }
                }
            }

            // Copy rocks
            foreach (string assetPath in rockAssets)
            {
                if (File.Exists(assetPath))
                {
                    string fileName = Path.GetFileName(assetPath);
                    string targetPath = $"Assets/Prefabs/Props/{fileName}";
                    
                    if (!File.Exists(targetPath))
                    {
                        // Ensure directory exists
                        string directory = Path.GetDirectoryName(targetPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        AssetDatabase.CopyAsset(assetPath, targetPath);
                        copied++;
                        Debug.Log($"✅ Copied: {fileName} → Prefabs/Props/");
                    }
                    else
                    {
                        skipped++;
                        Debug.Log($"⏭️ Skipped (already exists): {fileName}");
                    }
                }
            }

            AssetDatabase.Refresh();
            
            Debug.Log($"\n📦 Organization Complete!");
            Debug.Log($"   ✅ Copied: {copied} prefabs");
            Debug.Log($"   ⏭️ Skipped: {skipped} prefabs");
            Debug.Log($"\n💡 Next: Select [WORLD] GameObject and assign these prefabs to ForestGenerator component!");

            EditorUtility.DisplayDialog(
                "Assets Organized!",
                $"Copied {copied} prefabs to your Prefabs folders!\n\n" +
                $"Trees: Assets/Prefabs/Trees/\n" +
                $"Bushes & Rocks: Assets/Prefabs/Props/\n\n" +
                $"Next: Assign them to ForestGenerator component.",
                "OK"
            );
        }
    }
}

