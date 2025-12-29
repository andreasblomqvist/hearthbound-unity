using UnityEditor;
using UnityEngine;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Automatically assigns vegetation prefabs to ForestGenerator component
    /// </summary>
    public class AssignVegetationPrefabs
    {
        [MenuItem("Hearthbound/Assign Vegetation Prefabs to ForestGenerator")]
        public static void AssignPrefabs()
        {
            // Find ForestGenerator in the scene
            ForestGenerator forestGenerator = Object.FindObjectOfType<ForestGenerator>();
            
            if (forestGenerator == null)
            {
                EditorUtility.DisplayDialog(
                    "ForestGenerator Not Found",
                    "Could not find ForestGenerator component in the scene.\n\n" +
                    "Make sure:\n" +
                    "1. A GameObject with ForestGenerator component exists\n" +
                    "2. The scene is open and active",
                    "OK"
                );
                Debug.LogWarning("⚠️ ForestGenerator component not found in scene!");
                return;
            }

            // Load prefabs
            GameObject[] treePrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Trees/P_fwOF_Tree_M_2.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Trees/P_fwOF_TreeSapling_02B.prefab")
            };

            GameObject[] bushPrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/P_fwOF_ForestPlant_B_02.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/P_fwOF_Grass_M_1.prefab")
            };

            GameObject[] rockPrefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/P_fwOF_Rock_01.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/P_fwOF_Stone_01.prefab")
            };

            // Use SerializedObject to modify the private fields
            SerializedObject serializedObject = new SerializedObject(forestGenerator);
            
            // Assign tree prefabs (main array - used as fallback)
            SerializedProperty treePrefabsProp = serializedObject.FindProperty("treePrefabs");
            treePrefabsProp.arraySize = treePrefabs.Length;
            for (int i = 0; i < treePrefabs.Length; i++)
            {
                if (treePrefabs[i] != null)
                {
                    treePrefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = treePrefabs[i];
                }
            }

            // Assign biome-specific tree arrays (for biome-based placement)
            // Forest biome: Use all trees (full forest)
            SerializedProperty forestBiomeTreesProp = serializedObject.FindProperty("forestBiomeTrees");
            forestBiomeTreesProp.arraySize = treePrefabs.Length;
            for (int i = 0; i < treePrefabs.Length; i++)
            {
                if (treePrefabs[i] != null)
                {
                    forestBiomeTreesProp.GetArrayElementAtIndex(i).objectReferenceValue = treePrefabs[i];
                }
            }

            // Plains biome: Use sapling (smaller, sparse trees)
            SerializedProperty plainsBiomeTreesProp = serializedObject.FindProperty("plainsBiomeTrees");
            plainsBiomeTreesProp.arraySize = 1;
            plainsBiomeTreesProp.GetArrayElementAtIndex(0).objectReferenceValue = treePrefabs[1]; // TreeSapling

            // Default biome trees: Use all trees as fallback
            SerializedProperty defaultBiomeTreesProp = serializedObject.FindProperty("defaultBiomeTrees");
            defaultBiomeTreesProp.arraySize = treePrefabs.Length;
            for (int i = 0; i < treePrefabs.Length; i++)
            {
                if (treePrefabs[i] != null)
                {
                    defaultBiomeTreesProp.GetArrayElementAtIndex(i).objectReferenceValue = treePrefabs[i];
                }
            }

            // Enable biome-based trees
            serializedObject.FindProperty("useBiomeBasedTrees").boolValue = true;

            // Assign bush prefabs
            SerializedProperty bushPrefabsProp = serializedObject.FindProperty("bushPrefabs");
            bushPrefabsProp.arraySize = bushPrefabs.Length;
            for (int i = 0; i < bushPrefabs.Length; i++)
            {
                if (bushPrefabs[i] != null)
                {
                    bushPrefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = bushPrefabs[i];
                }
            }

            // Assign rock prefabs
            SerializedProperty rockPrefabsProp = serializedObject.FindProperty("rockPrefabs");
            rockPrefabsProp.arraySize = rockPrefabs.Length;
            for (int i = 0; i < rockPrefabs.Length; i++)
            {
                if (rockPrefabs[i] != null)
                {
                    rockPrefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = rockPrefabs[i];
                }
            }

            // Apply changes
            serializedObject.ApplyModifiedProperties();
            
            // Mark scene as dirty
            if (forestGenerator.gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(forestGenerator.gameObject.scene);
            }

            Debug.Log("✅ Successfully assigned vegetation prefabs to ForestGenerator!");
            Debug.Log($"   Trees: {treePrefabs.Length} prefabs (main array)");
            Debug.Log($"   Forest Biome Trees: {treePrefabs.Length} prefabs");
            Debug.Log($"   Plains Biome Trees: 1 prefab (sapling)");
            Debug.Log($"   Bushes: {bushPrefabs.Length} prefabs");
            Debug.Log($"   Rocks: {rockPrefabs.Length} prefabs");
            Debug.Log($"   Biome-based tree placement: ENABLED");
            
            // Select the GameObject in the Inspector so user can see the changes
            Selection.activeGameObject = forestGenerator.gameObject;

            EditorUtility.DisplayDialog(
                "Prefabs Assigned!",
                $"Successfully assigned vegetation prefabs to ForestGenerator!\n\n" +
                $"Trees: {treePrefabs.Length} (main array)\n" +
                $"Forest Biome: {treePrefabs.Length} trees\n" +
                $"Plains Biome: 1 tree (sapling)\n" +
                $"Bushes: {bushPrefabs.Length}\n" +
                $"Rocks: {rockPrefabs.Length}\n\n" +
                $"✅ Biome-based tree placement: ENABLED\n" +
                $"Trees will now vary by biome location!\n\n" +
                $"You can customize biome trees in the Inspector.",
                "OK"
            );
        }
    }
}

