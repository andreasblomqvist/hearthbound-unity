using System.Collections.Generic;
using UnityEngine;
using Hearthbound.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hearthbound.World
{
    /// <summary>
    /// Forest Generator
    /// Generates procedural forests with trees, bushes, and rocks
    /// Uses seed-based noise for natural-looking distribution
    /// </summary>
    public class ForestGenerator : MonoBehaviour
    {
        #region Prefabs
        [Header("Tree Prefabs")]
        [SerializeField] private GameObject[] treePrefabs;
        [SerializeField] private GameObject[] bushPrefabs;
        [SerializeField] private GameObject[] rockPrefabs;
        #endregion

        #region Generation Settings
        [Header("Forest Generation")]
        [SerializeField] private int numberOfForests = 5;
        [SerializeField] private float forestRadius = 100f;
        [SerializeField] private float treeDensity = 0.3f; // 0-1
        [SerializeField] private float bushDensity = 0.5f;
        [SerializeField] private float rockDensity = 0.1f;
        
        [Header("Placement Rules")]
        [SerializeField] private float minTreeDistance = 3f;
        [SerializeField] private float maxSlope = 45f;
        [SerializeField] private float minHeightForForest = 20f;
        [SerializeField] private float maxHeightForForest = 300f;
        
        [Header("Variation")]
        [SerializeField] private float scaleVariation = 0.3f; // ±30%
        [SerializeField] private bool randomRotation = true;
        #endregion

        #region Terrain Reference
        [Header("Terrain")]
        [SerializeField] private TerrainGenerator terrainGenerator;
        #endregion

        #region Generated Data
        private List<Vector3> forestCenters = new List<Vector3>();
        private List<GameObject> generatedObjects = new List<GameObject>();
        private Transform forestsContainer;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (terrainGenerator == null)
            {
                terrainGenerator = FindObjectOfType<TerrainGenerator>();
            }

            // Find or create container for forests
            Transform existingContainer = transform.Find("Forests");
            if (existingContainer == null)
            {
                GameObject containerObj = new GameObject("Forests");
                forestsContainer = containerObj.transform;
                forestsContainer.SetParent(transform);
            }
            else
            {
                forestsContainer = existingContainer;
            }
        }
        #endregion

        #region Forest Generation
        public void GenerateForests(int seed)
        {
            Debug.Log($"🌲 Generating {numberOfForests} forests with seed: {seed}");
            
            // Always clear existing forests first
            ClearForests();
            
            Random.InitState(seed);
            
            // Find forest locations
            FindForestLocations(seed);
            
            // Ensure container exists before generating
            if (forestsContainer == null)
            {
                Transform existingContainer = transform.Find("Forests");
                if (existingContainer != null)
                {
                    forestsContainer = existingContainer;
                }
                else
                {
                    GameObject containerObj = new GameObject("Forests");
                    forestsContainer = containerObj.transform;
                    forestsContainer.SetParent(transform);
                }
            }
            
            // Generate each forest
            for (int i = 0; i < forestCenters.Count; i++)
            {
                GenerateForest(forestCenters[i], seed + i * 1000);
            }
            
            Debug.Log($"✅ Generated {forestCenters.Count} forests with {generatedObjects.Count} objects");
        }

        private void FindForestLocations(int seed)
        {
            forestCenters.Clear();
            
            if (terrainGenerator == null)
            {
                Debug.LogError("❌ TerrainGenerator not found!");
                return;
            }

            Terrain terrain = terrainGenerator.GetComponent<Terrain>();
            if (terrain == null) return;

            int attempts = 0;
            int maxAttempts = numberOfForests * 10;

            while (forestCenters.Count < numberOfForests && attempts < maxAttempts)
            {
                attempts++;

                // Generate random position
                float x = Random.Range(forestRadius, terrain.terrainData.size.x - forestRadius);
                float z = Random.Range(forestRadius, terrain.terrainData.size.z - forestRadius);
                Vector3 position = new Vector3(x, 0, z);

                // Get terrain height
                float height = terrainGenerator.GetHeightAtPosition(position);
                position.y = height;

                // Check if position is valid
                if (!IsValidForestPosition(position))
                    continue;

                // Check distance from other forests (allow some overlap)
                bool tooClose = false;
                foreach (Vector3 existingForest in forestCenters)
                {
                    if (Vector3.Distance(position, existingForest) < forestRadius * 0.5f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    forestCenters.Add(position);
                    Debug.Log($"  🌲 Forest {forestCenters.Count} at {position}");
                }
            }

            if (forestCenters.Count < numberOfForests)
            {
                Debug.LogWarning($"⚠️ Only found {forestCenters.Count}/{numberOfForests} valid forest locations");
            }
        }

        private bool IsValidForestPosition(Vector3 position)
        {
            // Check height range
            if (position.y < minHeightForForest || position.y > maxHeightForForest)
                return false;

            // Check slope
            if (terrainGenerator != null)
            {
                float slope = terrainGenerator.GetSlopeAtPosition(position);
                if (slope > maxSlope)
                    return false;
            }

            return true;
        }

        private void GenerateForest(Vector3 centerPosition, int forestSeed)
        {
            Random.InitState(forestSeed);

            // Ensure container exists
            if (forestsContainer == null)
            {
                Transform existingContainer = transform.Find("Forests");
                if (existingContainer != null)
                {
                    forestsContainer = existingContainer;
                }
                else
                {
                    GameObject containerObj = new GameObject("Forests");
                    forestsContainer = containerObj.transform;
                    forestsContainer.SetParent(transform);
                }
            }

            // Create forest container
            GameObject forestObj = new GameObject($"Forest_{forestCenters.IndexOf(centerPosition)}");
            forestObj.transform.SetParent(forestsContainer);
            forestObj.transform.position = centerPosition;

            List<Vector3> treePositions = new List<Vector3>();

            // Calculate number of objects based on density
            int treeCount = Mathf.RoundToInt((forestRadius * forestRadius * Mathf.PI) * treeDensity / 10f);
            int bushCount = Mathf.RoundToInt((forestRadius * forestRadius * Mathf.PI) * bushDensity / 5f);
            int rockCount = Mathf.RoundToInt((forestRadius * forestRadius * Mathf.PI) * rockDensity / 20f);

            // Place trees
            PlaceTrees(centerPosition, forestObj.transform, treeCount, forestSeed, treePositions);

            // Place bushes
            PlaceBushes(centerPosition, forestObj.transform, bushCount, forestSeed + 100);

            // Place rocks
            PlaceRocks(centerPosition, forestObj.transform, rockCount, forestSeed + 200);

            Debug.Log($"  🌲 Forest generated: {treeCount} trees, {bushCount} bushes, {rockCount} rocks");
        }

        private void PlaceTrees(Vector3 center, Transform parent, int count, int seed, List<Vector3> positions)
        {
            if (treePrefabs == null || treePrefabs.Length == 0)
            {
                Debug.LogWarning("⚠️ No tree prefabs assigned!");
                return;
            }

            Random.InitState(seed);

            int placed = 0;
            int attempts = 0;
            int maxAttempts = count * 5;

            while (placed < count && attempts < maxAttempts)
            {
                attempts++;

                // Use noise-based distribution for more natural clustering
                Vector2 randomCircle = Random.insideUnitCircle * forestRadius;
                Vector3 position = center + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Check noise value for placement probability
                float noiseValue = NoiseGenerator.GetDetailNoise(position.x, position.z, seed, 0.05f);
                if (noiseValue < 0.3f) // Only place in certain noise regions
                    continue;

                // Snap to terrain height
                if (terrainGenerator != null)
                {
                    position.y = terrainGenerator.GetHeightAtPosition(position);
                }

                // Check if position is valid
                if (!IsValidTreePosition(position, positions))
                    continue;

                // Place tree
                PlaceObject(treePrefabs[Random.Range(0, treePrefabs.Length)], position, parent);
                positions.Add(position);
                placed++;
            }
        }

        private void PlaceBushes(Vector3 center, Transform parent, int count, int seed)
        {
            if (bushPrefabs == null || bushPrefabs.Length == 0)
                return;

            Random.InitState(seed);

            for (int i = 0; i < count; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * forestRadius;
                Vector3 position = center + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Snap to terrain height
                if (terrainGenerator != null)
                {
                    position.y = terrainGenerator.GetHeightAtPosition(position);
                    
                    // Check slope
                    float slope = terrainGenerator.GetSlopeAtPosition(position);
                    if (slope > maxSlope)
                        continue;
                }

                PlaceObject(bushPrefabs[Random.Range(0, bushPrefabs.Length)], position, parent);
            }
        }

        private void PlaceRocks(Vector3 center, Transform parent, int count, int seed)
        {
            if (rockPrefabs == null || rockPrefabs.Length == 0)
                return;

            Random.InitState(seed);

            for (int i = 0; i < count; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * forestRadius;
                Vector3 position = center + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Snap to terrain height
                if (terrainGenerator != null)
                {
                    position.y = terrainGenerator.GetHeightAtPosition(position);
                }

                PlaceObject(rockPrefabs[Random.Range(0, rockPrefabs.Length)], position, parent);
            }
        }

        private bool IsValidTreePosition(Vector3 position, List<Vector3> existingTrees)
        {
            // Check slope
            if (terrainGenerator != null)
            {
                float slope = terrainGenerator.GetSlopeAtPosition(position);
                if (slope > maxSlope)
                    return false;
            }

            // Check distance from other trees
            foreach (Vector3 existingPos in existingTrees)
            {
                if (Vector3.Distance(position, existingPos) < minTreeDistance)
                    return false;
            }

            return true;
        }

        private void PlaceObject(GameObject prefab, Vector3 position, Transform parent)
        {
            GameObject obj = Instantiate(prefab, position, Quaternion.identity, parent);
            
            // Random rotation
            if (randomRotation)
            {
                obj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }
            
            // Scale variation
            float scale = 1f + Random.Range(-scaleVariation, scaleVariation);
            obj.transform.localScale = Vector3.one * scale;
            
            // Align to terrain normal if available
            if (terrainGenerator != null)
            {
                Vector3 normal = terrainGenerator.GetNormalAtPosition(position);
                obj.transform.up = Vector3.Lerp(Vector3.up, normal, 0.5f); // Blend for more natural look
            }

            generatedObjects.Add(obj);
        }
        #endregion

        #region Clear Forests
        public void ClearForests()
        {
            Debug.Log("🗑️ Clearing forests...");
            
            // Find container if not already found
            if (forestsContainer == null)
            {
                forestsContainer = transform.Find("Forests");
            }

            // Clear forest containers first (this will destroy all children)
            if (forestsContainer != null)
            {
                // Create a list of children to destroy (to avoid modifying collection during iteration)
                List<Transform> childrenToDestroy = new List<Transform>();
                foreach (Transform child in forestsContainer)
                {
                    if (child != null)
                    {
                        childrenToDestroy.Add(child);
                    }
                }
                
                foreach (Transform child in childrenToDestroy)
                {
                    if (child != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(child.gameObject);
                        }
                        else
                        {
#if UNITY_EDITOR
                            DestroyImmediate(child.gameObject);
                            EditorUtility.SetDirty(gameObject);
#endif
                        }
                    }
                }
            }
            
            // Clear objects list (may contain references to already-destroyed objects)
            generatedObjects.Clear();
            forestCenters.Clear();
            
            Debug.Log($"✅ Forests cleared (container: {forestsContainer != null})");
        }
        #endregion

        #region Utilities
        public List<Vector3> GetForestCenters()
        {
            return new List<Vector3>(forestCenters);
        }

        public bool IsInForest(Vector3 position)
        {
            foreach (Vector3 center in forestCenters)
            {
                if (Vector3.Distance(position, center) < forestRadius)
                    return true;
            }
            return false;
        }
        #endregion

        #region Debug
        [ContextMenu("Generate Forests (Test Seed)")]
        private void DebugGenerateForests()
        {
            GenerateForests(12345);
        }

        [ContextMenu("Clear Forests")]
        private void DebugClearForests()
        {
            ClearForests();
        }

        private void OnDrawGizmos()
        {
            // Draw forest centers
            Gizmos.color = Color.green;
            foreach (Vector3 center in forestCenters)
            {
                Gizmos.DrawWireSphere(center, forestRadius);
                Gizmos.DrawSphere(center, 3f);
            }
        }
        #endregion
    }
}
