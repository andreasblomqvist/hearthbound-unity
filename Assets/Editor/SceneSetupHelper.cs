using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Hearthbound.Managers;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Helper script to set up the WorldGeneration scene according to SETUP.md Step 3
    /// </summary>
    public class SceneSetupHelper : EditorWindow
    {
        [MenuItem("Hearthbound/Setup Scene (Step 3)")]
        public static void SetupScene()
        {
            // Step 3.1: Create new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Step 3.2: Create three empty GameObjects
            GameObject managersGO = new GameObject("[MANAGERS]");
            GameObject worldGO = new GameObject("[WORLD]");
            GameObject playerGO = new GameObject("[PLAYER]");
            
            // Step 3.3: Add scripts to [MANAGERS]
            managersGO.AddComponent<GameManager>();
            managersGO.AddComponent<TimeManager>();
            managersGO.AddComponent<AIManager>();
            
            // Step 3.4: Add scripts to [WORLD]
            worldGO.AddComponent<WorldSeedManager>();
            worldGO.AddComponent<VillageBuilder>();
            worldGO.AddComponent<ForestGenerator>();
            
            // Step 3.5: Create Terrain and parent it under [WORLD]
            // Note: TerrainGenerator requires Terrain component on same GameObject,
            // so we add TerrainGenerator to the Terrain GameObject itself
            GameObject terrainGO = Terrain.CreateTerrainGameObject(null);
            terrainGO.name = "Terrain";
            terrainGO.transform.SetParent(worldGO.transform);
            
            // Add TerrainGenerator to the Terrain GameObject (since it requires Terrain component)
            terrainGO.AddComponent<TerrainGenerator>();
            
            // Save the scene
            string scenePath = "Assets/Scenes/WorldGeneration.unity";
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            
            bool saved = EditorSceneManager.SaveScene(newScene, scenePath);
            if (saved)
            {
                Debug.Log($"✅ Scene setup complete! Scene saved to: {scenePath}");
                Debug.Log("✅ Created [MANAGERS] with GameManager, TimeManager, AIManager");
                Debug.Log("✅ Created [WORLD] with WorldSeedManager, TerrainGenerator, VillageBuilder, ForestGenerator");
                Debug.Log("✅ Created [PLAYER] GameObject");
                Debug.Log("✅ Created Terrain object and assigned to TerrainGenerator");
            }
            else
            {
                Debug.LogError("❌ Failed to save scene!");
            }
            
            // Mark scene as dirty to ensure changes are saved
            EditorSceneManager.MarkSceneDirty(newScene);
        }
    }
}

