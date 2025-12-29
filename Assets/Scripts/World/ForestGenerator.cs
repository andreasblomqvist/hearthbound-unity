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
        
        [Header("Biome-Specific Trees (Optional)")]
        [Tooltip("If enabled, trees will be selected based on the biome at each forest location")]
        [SerializeField] private bool useBiomeBasedTrees = true;
        
        [Tooltip("Trees for Forest biome (medium trees, saplings)")]
        [SerializeField] private GameObject[] forestBiomeTrees;
        
        [Tooltip("Trees for Plains biome (smaller trees, sparse)")]
        [SerializeField] private GameObject[] plainsBiomeTrees;
        
        [Tooltip("Trees for Snow biome (if any)")]
        [SerializeField] private GameObject[] snowBiomeTrees;
        
        [Tooltip("Fallback: Trees used when biome-specific trees are not assigned")]
        [SerializeField] private GameObject[] defaultBiomeTrees;
        #endregion

        #region Generation Settings
        [Header("Forest Generation")]
        [SerializeField] private int numberOfForests = 5;
        [SerializeField] private float forestRadius = 100f;
        [SerializeField] private float treeDensity = 0.3f; // 0-1
        [SerializeField] private float bushDensity = 0.5f;
        [SerializeField] private float rockDensity = 0.1f;
        
        [Header("Plains Vegetation")]
        [SerializeField] private int numberOfPlainsPatches = 10;
        [SerializeField] private float plainsPatchRadius = 50f;
        [SerializeField] private float plainsGrassDensity = 0.4f; // 0-1
        
        [Header("Mountain Rocks")]
        [SerializeField] private int numberOfRockPatches = 8;
        [SerializeField] private float rockPatchRadius = 80f;
        [SerializeField] private float mountainRockDensity = 0.3f; // 0-1
        
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
        private List<Vector3> plainsPatches = new List<Vector3>();
        private List<Vector3> rockPatches = new List<Vector3>();
        private List<GameObject> generatedObjects = new List<GameObject>();
        private Transform forestsContainer;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            EnsureTerrainGenerator();
            
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
        
        /// <summary>
        /// Ensures terrainGenerator reference is found (needed for editor context menu execution)
        /// </summary>
        private void EnsureTerrainGenerator()
        {
            if (terrainGenerator == null)
            {
                terrainGenerator = FindObjectOfType<TerrainGenerator>();
                if (terrainGenerator == null)
                {
                    Debug.LogError("❌ TerrainGenerator not found! Make sure there's a TerrainGenerator component in the scene.");
                }
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
            
            // Find forest locations (only in Forest biomes)
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
            
            // Also generate plains vegetation (grass/bushes in Plains biomes)
            GeneratePlainsVegetation(seed);
            
            // Also generate mountain rocks (rocks in Rock/Mountain biomes)
            GenerateMountainRocks(seed);
            
            Debug.Log($"✅ Generated {forestCenters.Count} forests, {plainsPatches.Count} plains patches, {rockPatches.Count} rock patches with {generatedObjects.Count} objects");
        }

        private void FindForestLocations(int seed)
        {
            forestCenters.Clear();
            
            EnsureTerrainGenerator();
            if (terrainGenerator == null) return;

            Terrain terrain = terrainGenerator.GetComponent<Terrain>();
            if (terrain == null) return;

            int attempts = 0;
            int maxAttempts = numberOfForests * 50; // Increased attempts for better coverage

            // Debug: Sample a few random positions to see what biomes exist
            Debug.Log($"🔍 Sampling biomes (attempting to find {numberOfForests} forests)...");
            int sampleCount = 0;
            for (int i = 0; i < 20 && sampleCount < 5; i++)
            {
                float sampleX = Random.Range(forestRadius, terrain.terrainData.size.x - forestRadius);
                float sampleZ = Random.Range(forestRadius, terrain.terrainData.size.z - forestRadius);
                Vector3 samplePos = new Vector3(sampleX, 0, sampleZ);
                float sampleHeight = terrainGenerator.GetHeightAtPosition(samplePos);
                samplePos.y = sampleHeight;
                
                BiomeData biomeData = terrainGenerator.GetBiomeDataAtPosition(samplePos, seed);
                string biomeName = biomeData != null ? biomeData.biomeName : terrainGenerator.GetBiomeAtPosition(samplePos, seed);
                Debug.Log($"  Sample {sampleCount + 1}: pos=({samplePos.x:F0},{samplePos.z:F0}) height={sampleHeight:F1}, biome={biomeName}");
                sampleCount++;
            }

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

                // Check if position is valid (including biome check)
                if (!IsValidForestPosition(position, seed))
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

        private bool IsValidForestPosition(Vector3 position, int seed = 0)
        {
            if (terrainGenerator == null)
                return false;

            // Check slope first (fast check)
            float slope = terrainGenerator.GetSlopeAtPosition(position);
            if (slope > maxSlope)
                return false;
            
            // IMPORTANT: Check biome FIRST (most restrictive condition)
            // Try using BiomeCollection system first (more accurate)
            BiomeData biomeData = terrainGenerator.GetBiomeDataAtPosition(position, seed);
            string biomeName = null;
            
            if (biomeData != null)
            {
                // Use ScriptableObject biome system
                biomeName = biomeData.biomeName;
            }
            else
            {
                // Fall back to legacy string-based biome detection
                biomeName = terrainGenerator.GetBiomeAtPosition(position, seed);
            }
            
            if (biomeName != "Forest")
            {
                return false; // Not a Forest biome - skip this location
            }
            
            // Biome check passed - height check as safety (but biome system should handle height)
            // Note: Commenting out strict height check to trust biome system
            // if (position.y < minHeightForForest || position.y > maxHeightForForest)
            //     return false;

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

            // Get biome at forest center for biome-based tree selection
            string biomeName = "Forest"; // Default
            if (terrainGenerator != null)
            {
                biomeName = terrainGenerator.GetBiomeAtPosition(centerPosition, forestSeed);
            }

            // Create forest container
            GameObject forestObj = new GameObject($"Forest_{forestCenters.IndexOf(centerPosition)}_{biomeName}");
            forestObj.transform.SetParent(forestsContainer);
            forestObj.transform.position = centerPosition;

            List<Vector3> treePositions = new List<Vector3>();

            // Calculate number of objects based on density
            int treeCount = Mathf.RoundToInt((forestRadius * forestRadius * Mathf.PI) * treeDensity / 10f);
            int bushCount = Mathf.RoundToInt((forestRadius * forestRadius * Mathf.PI) * bushDensity / 5f);
            int rockCount = Mathf.RoundToInt((forestRadius * forestRadius * Mathf.PI) * rockDensity / 20f);

            // Place trees (with biome-based selection)
            PlaceTrees(centerPosition, forestObj.transform, treeCount, forestSeed, treePositions, biomeName);

            // Place bushes
            PlaceBushes(centerPosition, forestObj.transform, bushCount, forestSeed + 100);

            // Place rocks
            PlaceRocks(centerPosition, forestObj.transform, rockCount, forestSeed + 200);

            Debug.Log($"  🌲 Forest generated in {biomeName} biome: {treeCount} trees, {bushCount} bushes, {rockCount} rocks");
        }

        private void PlaceTrees(Vector3 center, Transform parent, int count, int seed, List<Vector3> positions, string biomeName = "Forest")
        {
            // Get appropriate tree prefabs for this biome
            GameObject[] treesToUse = GetTreesForBiome(biomeName);
            
            if (treesToUse == null || treesToUse.Length == 0)
            {
                Debug.LogWarning($"⚠️ No tree prefabs available for biome '{biomeName}'!");
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

                // Place tree from biome-appropriate list
                PlaceObject(treesToUse[Random.Range(0, treesToUse.Length)], position, parent);
                positions.Add(position);
                placed++;
            }
        }

        /// <summary>
        /// Get tree prefabs appropriate for the given biome
        /// </summary>
        private GameObject[] GetTreesForBiome(string biomeName)
        {
            // If biome-based trees are disabled, use all trees
            if (!useBiomeBasedTrees)
            {
                return treePrefabs.Length > 0 ? treePrefabs : defaultBiomeTrees;
            }

            // Select trees based on biome name
            GameObject[] selectedTrees = null;
            
            switch (biomeName.ToLower())
            {
                case "forest":
                    selectedTrees = forestBiomeTrees.Length > 0 ? forestBiomeTrees : treePrefabs;
                    break;
                case "plains":
                    selectedTrees = plainsBiomeTrees.Length > 0 ? plainsBiomeTrees : treePrefabs;
                    break;
                case "snow":
                    selectedTrees = snowBiomeTrees.Length > 0 ? snowBiomeTrees : defaultBiomeTrees;
                    break;
                default:
                    // For other biomes (Rock, Water, Dirt), use default or all trees
                    selectedTrees = defaultBiomeTrees.Length > 0 ? defaultBiomeTrees : treePrefabs;
                    break;
            }

            // Fallback: if selected array is empty, use main treePrefabs array
            if (selectedTrees == null || selectedTrees.Length == 0)
            {
                selectedTrees = treePrefabs.Length > 0 ? treePrefabs : defaultBiomeTrees;
            }

            return selectedTrees;
        }

        /// <summary>
        /// Generate grass/bushes in Plains biomes
        /// </summary>
        private void GeneratePlainsVegetation(int seed)
        {
            if (bushPrefabs == null || bushPrefabs.Length == 0)
            {
                Debug.LogWarning("⚠️ No bush prefabs assigned - skipping plains vegetation!");
                return;
            }

            Debug.Log($"🌾 Generating {numberOfPlainsPatches} plains vegetation patches with seed: {seed}");
            
            // Find plains locations
            FindPlainsLocations(seed);
            
            // Generate each plains patch
            for (int i = 0; i < plainsPatches.Count; i++)
            {
                GeneratePlainsPatch(plainsPatches[i], seed + i * 2000);
            }
            
            Debug.Log($"  ✅ Generated {plainsPatches.Count} plains patches");
        }

        private void FindPlainsLocations(int seed)
        {
            plainsPatches.Clear();
            
            EnsureTerrainGenerator();
            if (terrainGenerator == null) return;

            Terrain terrain = terrainGenerator.GetComponent<Terrain>();
            if (terrain == null) return;

            int attempts = 0;
            int maxAttempts = numberOfPlainsPatches * 10;

            while (plainsPatches.Count < numberOfPlainsPatches && attempts < maxAttempts)
            {
                attempts++;

                // Generate random position
                float x = Random.Range(plainsPatchRadius, terrain.terrainData.size.x - plainsPatchRadius);
                float z = Random.Range(plainsPatchRadius, terrain.terrainData.size.z - plainsPatchRadius);
                Vector3 position = new Vector3(x, 0, z);

                // Get terrain height
                float height = terrainGenerator.GetHeightAtPosition(position);
                position.y = height;

                // Check if position is valid (must be Plains biome)
                if (!IsValidPlainsPosition(position, seed))
                    continue;

                // Check distance from other patches
                bool tooClose = false;
                foreach (Vector3 existingPatch in plainsPatches)
                {
                    if (Vector3.Distance(position, existingPatch) < plainsPatchRadius * 0.7f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    plainsPatches.Add(position);
                }
            }

            if (plainsPatches.Count < numberOfPlainsPatches)
            {
                Debug.LogWarning($"⚠️ Only found {plainsPatches.Count}/{numberOfPlainsPatches} valid plains locations");
            }
        }

        private bool IsValidPlainsPosition(Vector3 position, int seed)
        {
            // Check height range (plains are usually at lower elevations)
            if (position.y < minHeightForForest || position.y > maxHeightForForest)
                return false;

            // Check slope (plains should be relatively flat)
            if (terrainGenerator != null)
            {
                float slope = terrainGenerator.GetSlopeAtPosition(position);
                if (slope > maxSlope * 0.7f) // Plains should be flatter than forests
                    return false;
                
                // IMPORTANT: Only place grass in Plains biomes
                string biomeName = terrainGenerator.GetBiomeAtPosition(position, seed);
                if (biomeName != "Plains")
                {
                    return false; // Not a Plains biome - skip this location
                }
            }

            return true;
        }

        private void GeneratePlainsPatch(Vector3 centerPosition, int patchSeed)
        {
            Random.InitState(patchSeed);

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

            // Create plains patch container
            GameObject patchObj = new GameObject($"PlainsPatch_{plainsPatches.IndexOf(centerPosition)}");
            patchObj.transform.SetParent(forestsContainer);
            patchObj.transform.position = centerPosition;

            // Calculate number of grass/bushes based on density
            int grassCount = Mathf.RoundToInt((plainsPatchRadius * plainsPatchRadius * Mathf.PI) * plainsGrassDensity / 3f);

            // Place grass/bushes in plains
            PlacePlainsVegetation(centerPosition, patchObj.transform, grassCount, patchSeed);
        }

        private void PlacePlainsVegetation(Vector3 center, Transform parent, int count, int seed)
        {
            if (bushPrefabs == null || bushPrefabs.Length == 0)
                return;

            Random.InitState(seed);

            for (int i = 0; i < count; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * plainsPatchRadius;
                Vector3 position = center + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Snap to terrain height
                if (terrainGenerator != null)
                {
                    position.y = terrainGenerator.GetHeightAtPosition(position);
                    
                    // Check slope
                    float slope = terrainGenerator.GetSlopeAtPosition(position);
                    if (slope > maxSlope * 0.7f)
                        continue;
                    
                    // Double-check it's still in Plains biome
                    string biomeName = terrainGenerator.GetBiomeAtPosition(position, seed);
                    if (biomeName != "Plains")
                        continue;
                }

                // Place grass/bush (prefer grass prefabs for plains)
                GameObject prefabToPlace = bushPrefabs[Random.Range(0, bushPrefabs.Length)];
                PlaceObject(prefabToPlace, position, parent);
            }
        }

        /// <summary>
        /// Generate rocks in Rock/Mountain biomes
        /// </summary>
        private void GenerateMountainRocks(int seed)
        {
            if (rockPrefabs == null || rockPrefabs.Length == 0)
            {
                Debug.LogWarning("⚠️ No rock prefabs assigned - skipping mountain rocks!");
                return;
            }

            Debug.Log($"⛰️ Generating {numberOfRockPatches} mountain rock patches with seed: {seed}");
            
            // Find rock/mountain locations
            FindRockLocations(seed);
            
            // Generate each rock patch
            for (int i = 0; i < rockPatches.Count; i++)
            {
                GenerateRockPatch(rockPatches[i], seed + i * 3000);
            }
            
            Debug.Log($"  ✅ Generated {rockPatches.Count} rock patches");
        }

        private void FindRockLocations(int seed)
        {
            rockPatches.Clear();
            
            EnsureTerrainGenerator();
            if (terrainGenerator == null) return;

            Terrain terrain = terrainGenerator.GetComponent<Terrain>();
            if (terrain == null) return;

            int attempts = 0;
            int maxAttempts = numberOfRockPatches * 10;

            while (rockPatches.Count < numberOfRockPatches && attempts < maxAttempts)
            {
                attempts++;

                // Generate random position
                float x = Random.Range(rockPatchRadius, terrain.terrainData.size.x - rockPatchRadius);
                float z = Random.Range(rockPatchRadius, terrain.terrainData.size.z - rockPatchRadius);
                Vector3 position = new Vector3(x, 0, z);

                // Get terrain height
                float height = terrainGenerator.GetHeightAtPosition(position);
                position.y = height;

                // Check if position is valid (must be Rock biome)
                if (!IsValidRockPosition(position, seed))
                    continue;

                // Check distance from other patches
                bool tooClose = false;
                foreach (Vector3 existingPatch in rockPatches)
                {
                    if (Vector3.Distance(position, existingPatch) < rockPatchRadius * 0.7f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    rockPatches.Add(position);
                }
            }

            if (rockPatches.Count < numberOfRockPatches)
            {
                Debug.LogWarning($"⚠️ Only found {rockPatches.Count}/{numberOfRockPatches} valid rock locations");
            }
        }

        private bool IsValidRockPosition(Vector3 position, int seed)
        {
            // Rocks can be at higher elevations
            if (terrainGenerator != null)
            {
                // Check slope - rocks are usually on steeper terrain
                float slope = terrainGenerator.GetSlopeAtPosition(position);
                
                // IMPORTANT: Only place rocks in Rock/Mountain biomes
                string biomeName = terrainGenerator.GetBiomeAtPosition(position, seed);
                if (biomeName != "Rock")
                {
                    return false; // Not a Rock biome - skip this location
                }
            }

            return true;
        }

        private void GenerateRockPatch(Vector3 centerPosition, int patchSeed)
        {
            Random.InitState(patchSeed);

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

            // Create rock patch container
            GameObject patchObj = new GameObject($"RockPatch_{rockPatches.IndexOf(centerPosition)}");
            patchObj.transform.SetParent(forestsContainer);
            patchObj.transform.position = centerPosition;

            // Calculate number of rocks based on density
            int rockCount = Mathf.RoundToInt((rockPatchRadius * rockPatchRadius * Mathf.PI) * mountainRockDensity / 15f);

            // Place rocks in mountain areas
            PlaceMountainRocks(centerPosition, patchObj.transform, rockCount, patchSeed);
        }

        private void PlaceMountainRocks(Vector3 center, Transform parent, int count, int seed)
        {
            if (rockPrefabs == null || rockPrefabs.Length == 0)
                return;

            Random.InitState(seed);

            for (int i = 0; i < count; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * rockPatchRadius;
                Vector3 position = center + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Snap to terrain height
                if (terrainGenerator != null)
                {
                    position.y = terrainGenerator.GetHeightAtPosition(position);
                    
                    // Double-check it's still in Rock biome
                    string biomeName = terrainGenerator.GetBiomeAtPosition(position, seed);
                    if (biomeName != "Rock")
                        continue;
                }

                // Place rock
                GameObject prefabToPlace = rockPrefabs[Random.Range(0, rockPrefabs.Length)];
                PlaceObject(prefabToPlace, position, parent);
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
            plainsPatches.Clear();
            rockPatches.Clear();
            
            Debug.Log($"✅ Forests, plains vegetation, and mountain rocks cleared (container: {forestsContainer != null})");
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
