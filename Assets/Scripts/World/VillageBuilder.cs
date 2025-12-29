using System.Collections.Generic;
using UnityEngine;
using Hearthbound.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hearthbound.World
{
    /// <summary>
    /// Village Builder
    /// Generates procedural villages with buildings, roads, and props
    /// Uses seed for reproducible village placement
    /// </summary>
    public class VillageBuilder : MonoBehaviour
    {
        #region Building Prefabs
        [Header("Building Prefabs")]
        [SerializeField] private GameObject[] housePrefabs;
        [SerializeField] private GameObject[] farmPrefabs;
        [SerializeField] private GameObject tavernPrefab;
        [SerializeField] private GameObject[] propPrefabs;
        #endregion

        #region Village Settings
        [Header("Village Generation")]
        [SerializeField] private int numberOfVillages = 3;
        [SerializeField] private int buildingsPerVillage = 10;
        [SerializeField] private float villageRadius = 50f;
        [SerializeField] private float minBuildingDistance = 10f;
        [SerializeField] private float minVillageDistance = 200f;
        
        [Header("Placement Rules")]
        [SerializeField] private float maxSlope = 30f;
        [SerializeField] private float minHeightForVillage = 10f;
        [SerializeField] private float maxHeightForVillage = 100f;
        #endregion

        #region Terrain Reference
        [Header("Terrain")]
        [SerializeField] private TerrainGenerator terrainGenerator;
        #endregion

        #region Generated Data
        private List<Vector3> villagePositions = new List<Vector3>();
        private List<GameObject> generatedBuildings = new List<GameObject>();
        private Transform villagesContainer;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (terrainGenerator == null)
            {
                terrainGenerator = FindObjectOfType<TerrainGenerator>();
            }

            // Find or create container for villages
            Transform existingContainer = transform.Find("Villages");
            if (existingContainer == null)
            {
                GameObject containerObj = new GameObject("Villages");
                villagesContainer = containerObj.transform;
                villagesContainer.SetParent(transform);
            }
            else
            {
                villagesContainer = existingContainer;
            }
        }
        #endregion

        #region Village Generation
        public void GenerateVillages(int seed)
        {
            Debug.Log($"🏘️ Generating {numberOfVillages} villages with seed: {seed}");
            
            // Always clear existing villages first
            ClearVillages();
            
            Random.InitState(seed);
            
            // Find village locations
            FindVillageLocations(seed);
            
            // Ensure container exists before building
            if (villagesContainer == null)
            {
                Transform existingContainer = transform.Find("Villages");
                if (existingContainer != null)
                {
                    villagesContainer = existingContainer;
                }
                else
                {
                    GameObject containerObj = new GameObject("Villages");
                    villagesContainer = containerObj.transform;
                    villagesContainer.SetParent(transform);
                }
            }
            
            // Build each village
            for (int i = 0; i < villagePositions.Count; i++)
            {
                BuildVillage(villagePositions[i], seed + i);
            }
            
            Debug.Log($"✅ Generated {villagePositions.Count} villages with {generatedBuildings.Count} buildings");
        }

        private void FindVillageLocations(int seed)
        {
            villagePositions.Clear();
            
            if (terrainGenerator == null)
            {
                Debug.LogError("❌ TerrainGenerator not found!");
                return;
            }

            int attempts = 0;
            int maxAttempts = numberOfVillages * 10;

            while (villagePositions.Count < numberOfVillages && attempts < maxAttempts)
            {
                attempts++;

                // Generate random position
                float x = Random.Range(50f, terrainGenerator.GetComponent<Terrain>().terrainData.size.x - 50f);
                float z = Random.Range(50f, terrainGenerator.GetComponent<Terrain>().terrainData.size.z - 50f);
                Vector3 position = new Vector3(x, 0, z);

                // Get terrain height
                float height = terrainGenerator.GetHeightAtPosition(position);
                position.y = height;

                // Check if position is valid
                if (!IsValidVillagePosition(position))
                    continue;

                // Check distance from other villages
                bool tooClose = false;
                foreach (Vector3 existingVillage in villagePositions)
                {
                    if (Vector3.Distance(position, existingVillage) < minVillageDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    villagePositions.Add(position);
                    Debug.Log($"  📍 Village {villagePositions.Count} at {position}");
                }
            }

            if (villagePositions.Count < numberOfVillages)
            {
                Debug.LogWarning($"⚠️ Only found {villagePositions.Count}/{numberOfVillages} valid village locations");
            }
        }

        private bool IsValidVillagePosition(Vector3 position)
        {
            // Check height range
            if (position.y < minHeightForVillage || position.y > maxHeightForVillage)
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

        private void BuildVillage(Vector3 centerPosition, int villageSeed)
        {
            Random.InitState(villageSeed);

            // Ensure container exists
            if (villagesContainer == null)
            {
                Transform existingContainer = transform.Find("Villages");
                if (existingContainer != null)
                {
                    villagesContainer = existingContainer;
                }
                else
                {
                    GameObject containerObj = new GameObject("Villages");
                    villagesContainer = containerObj.transform;
                    villagesContainer.SetParent(transform);
                }
            }

            // Create village container
            GameObject villageObj = new GameObject($"Village_{villagePositions.IndexOf(centerPosition)}");
            villageObj.transform.SetParent(villagesContainer);
            villageObj.transform.position = centerPosition;

            List<Vector3> buildingPositions = new List<Vector3>();

            // Place tavern at center (if prefab exists)
            if (tavernPrefab != null)
            {
                PlaceBuilding(tavernPrefab, centerPosition, villageObj.transform);
                buildingPositions.Add(centerPosition);
            }

            // Place houses around center
            int housesPlaced = 0;
            int attempts = 0;
            int maxAttempts = buildingsPerVillage * 5;

            while (housesPlaced < buildingsPerVillage && attempts < maxAttempts)
            {
                attempts++;

                // Generate random position within village radius
                Vector2 randomCircle = Random.insideUnitCircle * villageRadius;
                Vector3 buildingPos = centerPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Snap to terrain height
                if (terrainGenerator != null)
                {
                    buildingPos.y = terrainGenerator.GetHeightAtPosition(buildingPos);
                }

                // Check if position is valid
                if (!IsValidBuildingPosition(buildingPos, buildingPositions))
                    continue;

                // Choose random building type
                GameObject prefab = GetRandomBuildingPrefab();
                if (prefab != null)
                {
                    PlaceBuilding(prefab, buildingPos, villageObj.transform);
                    buildingPositions.Add(buildingPos);
                    housesPlaced++;
                }
            }

            // Place props (fences, carts, etc.)
            PlaceVillageProps(centerPosition, villageObj.transform, villageSeed);

            Debug.Log($"  🏠 Built village with {housesPlaced} buildings");
        }

        private bool IsValidBuildingPosition(Vector3 position, List<Vector3> existingBuildings)
        {
            // Check slope
            if (terrainGenerator != null)
            {
                float slope = terrainGenerator.GetSlopeAtPosition(position);
                if (slope > maxSlope)
                    return false;
            }

            // Check distance from other buildings
            foreach (Vector3 existingPos in existingBuildings)
            {
                if (Vector3.Distance(position, existingPos) < minBuildingDistance)
                    return false;
            }

            return true;
        }

        private GameObject GetRandomBuildingPrefab()
        {
            // 70% houses, 30% farms
            if (Random.value < 0.7f && housePrefabs != null && housePrefabs.Length > 0)
            {
                return housePrefabs[Random.Range(0, housePrefabs.Length)];
            }
            else if (farmPrefabs != null && farmPrefabs.Length > 0)
            {
                return farmPrefabs[Random.Range(0, farmPrefabs.Length)];
            }
            
            return null;
        }

        private void PlaceBuilding(GameObject prefab, Vector3 position, Transform parent)
        {
            GameObject building = Instantiate(prefab, position, Quaternion.identity, parent);
            
            // Random rotation (snap to 90 degree angles)
            building.transform.rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);
            
            // Align to terrain normal if available
            if (terrainGenerator != null)
            {
                Vector3 normal = terrainGenerator.GetNormalAtPosition(position);
                building.transform.up = normal;
            }

            generatedBuildings.Add(building);
        }

        private void PlaceVillageProps(Vector3 centerPosition, Transform parent, int seed)
        {
            if (propPrefabs == null || propPrefabs.Length == 0)
                return;

            Random.InitState(seed + 1000);

            int propCount = Random.Range(5, 15);
            for (int i = 0; i < propCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * villageRadius;
                Vector3 propPos = centerPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

                if (terrainGenerator != null)
                {
                    propPos.y = terrainGenerator.GetHeightAtPosition(propPos);
                }

                GameObject prop = Instantiate(
                    propPrefabs[Random.Range(0, propPrefabs.Length)],
                    propPos,
                    Quaternion.Euler(0, Random.Range(0f, 360f), 0),
                    parent
                );

                generatedBuildings.Add(prop);
            }
        }
        #endregion

        #region Clear Villages
        public void ClearVillages()
        {
            Debug.Log("🗑️ Clearing villages...");
            
            // Find container if not already found
            if (villagesContainer == null)
            {
                villagesContainer = transform.Find("Villages");
            }

            // Clear village containers first (this will destroy all children)
            if (villagesContainer != null)
            {
                // Create a list of children to destroy (to avoid modifying collection during iteration)
                List<Transform> childrenToDestroy = new List<Transform>();
                foreach (Transform child in villagesContainer)
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
            
            // Clear buildings list (may contain references to already-destroyed objects)
            generatedBuildings.Clear();
            villagePositions.Clear();
            
            Debug.Log($"✅ Villages cleared (container: {villagesContainer != null})");
        }
        #endregion

        #region Utilities
        public List<Vector3> GetVillagePositions()
        {
            return new List<Vector3>(villagePositions);
        }

        public Vector3 GetNearestVillage(Vector3 position)
        {
            if (villagePositions.Count == 0)
                return Vector3.zero;

            Vector3 nearest = villagePositions[0];
            float nearestDistance = Vector3.Distance(position, nearest);

            foreach (Vector3 villagePos in villagePositions)
            {
                float distance = Vector3.Distance(position, villagePos);
                if (distance < nearestDistance)
                {
                    nearest = villagePos;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
        #endregion

        #region Debug
        [ContextMenu("Generate Villages (Test Seed)")]
        private void DebugGenerateVillages()
        {
            GenerateVillages(12345);
        }

        [ContextMenu("Clear Villages")]
        private void DebugClearVillages()
        {
            ClearVillages();
        }

        private void OnDrawGizmos()
        {
            // Draw village positions
            Gizmos.color = Color.yellow;
            foreach (Vector3 pos in villagePositions)
            {
                Gizmos.DrawWireSphere(pos, villageRadius);
                Gizmos.DrawSphere(pos, 2f);
            }
        }
        #endregion
    }
}
