using UnityEngine;
using Hearthbound.World;

namespace Hearthbound.Characters
{
    /// <summary>
    /// Helper script to set up the third-person player character and camera
    /// Can be attached to a GameObject or used via context menu
    /// </summary>
    public class PlayerSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool setupOnStart = false;
        
        public enum SpawnMode
        {
            UseExistingTransform,  // Use the existing GameObject's Transform position (if player exists)
            Custom,                // Use custom spawn position
            TerrainCenter,         // Spawn at terrain center
            TerrainRandom,         // Spawn at random location on terrain
            WorldOrigin            // Spawn at world origin (0,0,0)
        }
        
        [Header("Spawn Position")]
        [SerializeField] private SpawnMode spawnMode = SpawnMode.UseExistingTransform;
        [SerializeField] private Vector3 customSpawnPosition = Vector3.zero;
        [SerializeField] private bool findTerrainHeight = true;
        [SerializeField] private bool overrideExistingPosition = false; // If true, will reposition even existing players

        [Header("Character Settings")]
        [SerializeField] private float characterHeight = 2f;
        [SerializeField] private float characterRadius = 0.5f;

        [Header("Camera Settings")]
        [SerializeField] private bool setupCamera = true;
        [SerializeField] private float cameraDistance = 5f;
        [SerializeField] private float cameraHeight = 2f;

        private GameObject playerObject;
        private GameObject cameraObject;

        void Start()
        {
            if (setupOnStart)
            {
                SetupPlayer();
            }
        }

        [ContextMenu("Setup Player Character")]
        public void SetupPlayer()
        {
            // Find or create player
            ThirdPersonController existingController = FindObjectOfType<ThirdPersonController>();
            bool playerExisted = existingController != null;
            
            if (existingController != null)
            {
                playerObject = existingController.gameObject;
                Debug.Log("Found existing player character");
            }
            else
            {
                playerObject = CreatePlayerCharacter();
            }

            // Position player (only if not using existing transform, or if override is enabled)
            if (!playerExisted || overrideExistingPosition || spawnMode != SpawnMode.UseExistingTransform)
            {
                PositionPlayer();
            }
            else
            {
                // Just ensure player is on terrain if findTerrainHeight is enabled
                if (findTerrainHeight)
                {
                    AdjustToTerrainHeight();
                }
            }

            // Setup camera
            if (setupCamera)
            {
                SetupCamera();
            }

            // Connect camera to controller
            ConnectCameraToController();
        }

        /// <summary>
        /// Creates the player character GameObject
        /// </summary>
        private GameObject CreatePlayerCharacter()
        {
            // Create player GameObject
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            // Position will be set by PositionPlayer() method
            player.transform.position = Vector3.zero;

            // Remove default collider (CharacterController will handle collision)
            Collider defaultCollider = player.GetComponent<Collider>();
            if (defaultCollider != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(defaultCollider);
#else
                Destroy(defaultCollider);
#endif
            }

            // Add CharacterController
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = characterHeight;
            controller.radius = characterRadius;
            controller.center = new Vector3(0, characterHeight / 2f, 0);
            controller.minMoveDistance = 0f; // Better collision detection
            controller.skinWidth = 0.01f; // Prevents getting stuck but allows detection

            // Add ThirdPersonController script
            ThirdPersonController thirdPersonController = player.AddComponent<ThirdPersonController>();

            // Add a simple material to make it visible
            Renderer renderer = player.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.6f, 1f); // Light blue color
                renderer.material = mat;
            }

            Debug.Log("Created player character");
            return player;
        }

        /// <summary>
        /// Gets the spawn position based on spawn mode
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            // If using existing transform and player exists, use its current position
            if (spawnMode == SpawnMode.UseExistingTransform && playerObject != null)
            {
                return playerObject.transform.position;
            }
            
            Terrain terrain = FindObjectOfType<Terrain>();
            
            switch (spawnMode)
            {
                case SpawnMode.TerrainCenter:
                    if (terrain != null)
                    {
                        Vector3 terrainSize = terrain.terrainData.size;
                        Vector3 terrainCenter = terrain.transform.position + terrainSize * 0.5f;
                        return new Vector3(terrainCenter.x, 0, terrainCenter.z); // Y will be set by terrain height
                    }
                    return Vector3.zero;
                    
                case SpawnMode.TerrainRandom:
                    if (terrain != null)
                    {
                        Vector3 terrainSize = terrain.terrainData.size;
                        Vector3 terrainPos = terrain.transform.position;
                        float randomX = Random.Range(terrainPos.x + terrainSize.x * 0.1f, terrainPos.x + terrainSize.x * 0.9f);
                        float randomZ = Random.Range(terrainPos.z + terrainSize.z * 0.1f, terrainPos.z + terrainSize.z * 0.9f);
                        return new Vector3(randomX, 0, randomZ); // Y will be set by terrain height
                    }
                    return Vector3.zero;
                    
                case SpawnMode.WorldOrigin:
                    return Vector3.zero;
                    
                case SpawnMode.Custom:
                default:
                    return customSpawnPosition;
            }
        }
        
        /// <summary>
        /// Gets the water level at a position (checks WaterGenerator)
        /// </summary>
        private float GetWaterLevel(Vector3 position)
        {
            WaterGenerator waterGen = FindObjectOfType<WaterGenerator>();
            if (waterGen != null)
            {
                // Try to get the water plane's Y position
                Transform waterTransform = waterGen.transform.Find("Water");
                if (waterTransform == null)
                {
                    // Try to find by name
                    GameObject waterObj = GameObject.Find("Water");
                    if (waterObj != null)
                    {
                        waterTransform = waterObj.transform;
                    }
                }
                
                if (waterTransform != null)
                {
                    return waterTransform.position.y;
                }
            }
            
            // Fallback: check if position is in a low area (below water threshold)
            Terrain terrain = FindObjectOfType<Terrain>();
            if (terrain != null)
            {
                // Sample terrain height to see if it's very low (likely water)
                float terrainHeight = terrain.SampleHeight(position);
                float normalizedHeight = terrainHeight / terrain.terrainData.size.y;
                
                // If normalized height is very low (< 0.1), it's likely water
                if (normalizedHeight < 0.1f)
                {
                    // Estimate water level as a small percentage of terrain height
                    float estimatedWaterLevel = terrain.transform.position.y + terrain.terrainData.size.y * 0.02f;
                    return estimatedWaterLevel;
                }
            }
            
            return float.MinValue; // No water detected
        }

        /// <summary>
        /// Adjusts existing player's Y position to terrain height or water level without changing X/Z
        /// </summary>
        private void AdjustToTerrainHeight()
        {
            if (playerObject == null) return;
            
            Terrain terrain = FindObjectOfType<Terrain>();
            if (terrain != null)
            {
                Vector3 currentPos = playerObject.transform.position;
                float terrainHeight = terrain.SampleHeight(currentPos);
                float terrainWorldY = terrain.transform.position.y + terrainHeight;
                
                // Check if we're in a water area
                float waterLevel = GetWaterLevel(currentPos);
                bool isInWater = waterLevel != float.MinValue && terrainWorldY < waterLevel + 1f;
                
                // Get CharacterController if it exists to use its actual height
                CharacterController cc = playerObject.GetComponent<CharacterController>();
                float playerHeight = cc != null ? cc.height : characterHeight;
                
                float targetY;
                if (isInWater)
                {
                    // Position on water surface
                    targetY = waterLevel + playerHeight / 2f + 0.1f;
                    Debug.Log($"Player is in water area - positioning at water level: {waterLevel}");
                }
                else
                {
                    // Position on terrain
                    targetY = terrainWorldY + playerHeight / 2f + 0.1f;
                }
                
                // Disable CharacterController temporarily to set position
                if (cc != null)
                {
                    cc.enabled = false;
                }
                
                playerObject.transform.position = new Vector3(currentPos.x, targetY, currentPos.z);
                
                if (cc != null)
                {
                    cc.enabled = true;
                }
                
                Debug.Log($"Adjusted player Y to {(isInWater ? "water" : "terrain")} height: {targetY} (terrain: {terrainWorldY}, water: {waterLevel}, kept X: {currentPos.x}, Z: {currentPos.z})");
            }
        }

        /// <summary>
        /// Positions the player on the terrain
        /// </summary>
        private void PositionPlayer()
        {
            if (playerObject == null) return;

            Vector3 position = GetSpawnPosition();

            if (findTerrainHeight)
            {
                // Try to find terrain and get height at spawn position
                Terrain terrain = FindObjectOfType<Terrain>();
                if (terrain != null)
                {
                    // Wait a frame to ensure terrain is fully loaded
                    float terrainHeight = terrain.SampleHeight(position);
                    // Account for terrain's world position (terrain might not be at origin)
                    float terrainWorldY = terrain.transform.position.y + terrainHeight;
                    
                    // Check if we're in a water area (lake)
                    float waterLevel = GetWaterLevel(position);
                    bool isInWater = waterLevel != float.MinValue && terrainWorldY < waterLevel + 1f;
                    
                    // Get CharacterController if it exists to use its actual height
                    CharacterController cc = playerObject.GetComponent<CharacterController>();
                    float playerHeight = cc != null ? cc.height : characterHeight;
                    
                    if (isInWater)
                    {
                        // Position on water surface (lake)
                        position.y = waterLevel + playerHeight / 2f + 0.1f;
                        Debug.Log($"Positioned player on WATER at height: {position.y} (water level: {waterLevel}, terrain was: {terrainWorldY})");
                    }
                    else
                    {
                        // Position on terrain
                        position.y = terrainWorldY + playerHeight / 2f + 0.1f; // Slightly above terrain
                        Debug.Log($"Positioned player on TERRAIN at height: {position.y} (terrain world Y: {terrainWorldY}, terrain height: {terrainHeight}, player height: {playerHeight})");
                    }
                    
                    // Disable CharacterController temporarily to set position
                    if (cc != null)
                    {
                        cc.enabled = false;
                    }
                    
                    playerObject.transform.position = position;
                    
                    if (cc != null)
                    {
                        cc.enabled = true;
                    }
                }
                else
                {
                    // Fallback: raycast down to find ground
                    RaycastHit hit;
                    Vector3 rayOrigin = position + Vector3.up * 100f;
                    if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 200f))
                    {
                        position.y = hit.point.y + characterHeight / 2f + 0.1f;
                        Debug.Log($"Positioned player using raycast at height: {position.y}");
                    }
                    else
                    {
                        Debug.LogWarning("Could not find terrain or ground! Player may fall through world.");
                    }
                }
            }
            else
            {
                // Use the Y from the spawn position if not finding terrain height
                Vector3 spawnPos = GetSpawnPosition();
                position.y = spawnPos.y;
            }

            playerObject.transform.position = position;

            // Also call SnapToTerrain on the controller to ensure proper positioning
            ThirdPersonController controller = playerObject.GetComponent<ThirdPersonController>();
            if (controller != null)
            {
                controller.SnapToTerrain();
            }
        }

        /// <summary>
        /// Sets up the camera
        /// </summary>
        private void SetupCamera()
        {
            // Find existing camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraObject = mainCam.gameObject;
                
                // Check if it already has ThirdPersonCamera
                ThirdPersonCamera existingCam = cameraObject.GetComponent<ThirdPersonCamera>();
                if (existingCam == null)
                {
                    ThirdPersonCamera thirdPersonCam = cameraObject.AddComponent<ThirdPersonCamera>();
                    thirdPersonCam.SetTarget(playerObject.transform);
                    Debug.Log("Added ThirdPersonCamera to existing camera");
                }
                else
                {
                    existingCam.SetTarget(playerObject.transform);
                    Debug.Log("Updated existing ThirdPersonCamera target");
                }
            }
            else
            {
                // Create new camera
                cameraObject = new GameObject("Main Camera");
                Camera cam = cameraObject.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.fieldOfView = 60f;
                
                ThirdPersonCamera thirdPersonCam = cameraObject.AddComponent<ThirdPersonCamera>();
                thirdPersonCam.SetTarget(playerObject.transform);
                
                Debug.Log("Created new camera with ThirdPersonCamera");
            }
        }

        /// <summary>
        /// Connects the camera to the controller
        /// </summary>
        private void ConnectCameraToController()
        {
            if (playerObject == null || cameraObject == null) return;

            ThirdPersonController controller = playerObject.GetComponent<ThirdPersonController>();
            if (controller != null && cameraObject != null)
            {
                controller.SetCameraTransform(cameraObject.transform);
                Debug.Log("Connected camera to controller");
            }
        }

        /// <summary>
        /// Gets the player GameObject
        /// </summary>
        public GameObject GetPlayer()
        {
            return playerObject;
        }

        /// <summary>
        /// Gets the camera GameObject
        /// </summary>
        public GameObject GetCamera()
        {
            return cameraObject;
        }
    }
}

