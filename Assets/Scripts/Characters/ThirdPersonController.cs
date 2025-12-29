using UnityEngine;

namespace Hearthbound.Characters
{
    /// <summary>
    /// Third-person character controller
    /// Handles WASD movement, walk/run speeds, and character rotation
    /// Uses Unity's CharacterController for terrain interaction
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        #region Components
        private CharacterController characterController;
        private Transform cameraTransform;
        #endregion

        #region Movement Settings
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float rotationSmoothTime = 0.1f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 15f;
        #endregion

        #region Gravity Settings
        [Header("Gravity")]
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundLayerMask = -1;
        [SerializeField] private bool useRaycastGroundCheck = true;
        [SerializeField] private bool followTerrainSurface = true; // Keep character on terrain surface
        [SerializeField] private float terrainFollowStrength = 10f; // How strongly to follow terrain (higher = smoother)
        [SerializeField] private float terrainFixThreshold = 0.5f; // How far below terrain before fixing position
        #endregion

        #region Private Variables
        private Vector3 velocity;
        private float currentSpeed;
        private float targetSpeed;
        private float rotationVelocity;
        private bool isGrounded;
        private float groundDistance;
        private float lastTerrainCheckTime;
        private Terrain cachedTerrain;
        private int snapCount = 0;
        private float lastSnapTime = 0f;
        #endregion

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            
            // Ensure CharacterController is properly configured
            if (characterController != null)
            {
                // Set min move distance to 0 for better collision detection
                characterController.minMoveDistance = 0f;
                
                // Ensure skin width is reasonable (prevents getting stuck but allows detection)
                if (characterController.skinWidth < 0.01f)
                {
                    characterController.skinWidth = 0.01f;
                }
                
                // Ensure step offset is reasonable
                characterController.stepOffset = 0.3f;
                
                // Ensure slope limit allows walking on terrain (higher = can walk on steeper slopes)
                characterController.slopeLimit = 60f; // Increased to handle mountain slopes
            }
        }

        void Start()
        {
            // Find main camera if not assigned
            if (cameraTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    cameraTransform = mainCam.transform;
                }
                else
                {
                    // Try to find ThirdPersonCamera component
                    ThirdPersonCamera thirdPersonCam = FindObjectOfType<ThirdPersonCamera>();
                    if (thirdPersonCam != null)
                    {
                        cameraTransform = thirdPersonCam.transform;
                    }
                }
            }

            // Cache terrain reference
            cachedTerrain = FindObjectOfType<Terrain>();

            // Snap to terrain on start with delay to ensure terrain is ready
            Invoke(nameof(SnapToTerrain), 0.1f);
        }

        void Update()
        {
            HandleGravity();
            
            // Follow terrain surface if enabled (must be before movement)
            if (followTerrainSurface)
            {
                FollowTerrainSurface();
            }
            
            HandleMovement();
            
            // Safety check: if we're falling too fast or too far, snap back to terrain
            // Prevent infinite snapping loop by limiting snap frequency
            if (Time.time - lastSnapTime > 0.5f) // Only snap once per 0.5 seconds max
            {
                if (velocity.y < -50f || transform.position.y < -100f)
                {
                    Debug.LogWarning($"Character falling through world! Y: {transform.position.y}, Velocity Y: {velocity.y}, Grounded: {isGrounded}. Snapping back to terrain.");
                    SnapToTerrain();
                    lastSnapTime = Time.time;
                }
            }
        }

        /// <summary>
        /// Handles character movement based on input
        /// </summary>
        private void HandleMovement()
        {
            // Get input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            bool isRunning = Input.GetKey(KeyCode.LeftShift);

            // Calculate movement direction relative to camera
            Vector3 moveDirection = Vector3.zero;
            
            if (cameraTransform != null)
            {
                // Get camera forward and right vectors (flattened on Y axis)
                Vector3 cameraForward = cameraTransform.forward;
                Vector3 cameraRight = cameraTransform.right;
                
                cameraForward.y = 0f;
                cameraRight.y = 0f;
                
                cameraForward.Normalize();
                cameraRight.Normalize();
                
                // Calculate movement direction relative to camera
                moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
            }
            else
            {
                // Fallback to world space if no camera
                moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
            }

            // Determine target speed
            if (moveDirection.magnitude > 0.1f)
            {
                targetSpeed = isRunning ? runSpeed : walkSpeed;
            }
            else
            {
                targetSpeed = 0f;
            }

            // Smoothly interpolate current speed
            if (targetSpeed > currentSpeed)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, deceleration * Time.deltaTime);
            }

            // Calculate horizontal movement
            Vector3 horizontalMovement = moveDirection * currentSpeed;
            
            // If following terrain, check terrain height at destination
            if (followTerrainSurface && cachedTerrain != null && horizontalMovement.magnitude > 0.01f)
            {
                // Calculate where we'll be after horizontal movement
                Vector3 futurePos = transform.position + horizontalMovement * Time.deltaTime;
                float futureTerrainHeight = cachedTerrain.SampleHeight(futurePos);
                float futureTerrainWorldY = cachedTerrain.transform.position.y + futureTerrainHeight;
                float currentTerrainHeight = cachedTerrain.SampleHeight(transform.position);
                float currentTerrainWorldY = cachedTerrain.transform.position.y + currentTerrainHeight;
                
                // Calculate terrain slope (height difference)
                float terrainSlope = futureTerrainWorldY - currentTerrainWorldY;
                
                // Adjust vertical velocity to follow terrain slope
                if (isGrounded)
                {
                    // When grounded, adjust velocity to match terrain slope
                    velocity.y = terrainSlope / Time.deltaTime;
                    // Clamp to reasonable values
                    velocity.y = Mathf.Clamp(velocity.y, -10f, 10f);
                }
            }
            
            // Apply gravity to velocity (will be adjusted by FollowTerrainSurface if enabled)
            if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
            }

            // Combine horizontal movement with vertical velocity
            Vector3 movement = horizontalMovement * Time.deltaTime;
            movement.y = velocity.y * Time.deltaTime;

            // Move character - CharacterController will automatically handle terrain slopes
            CollisionFlags collisionFlags = characterController.Move(movement);
            
            // Update grounded state after movement
            isGrounded = (collisionFlags & CollisionFlags.Below) != 0 || characterController.isGrounded;
            
            // If we just landed, reset velocity
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small downward force to keep grounded
            }

            // Rotate character to face movement direction
            if (moveDirection.magnitude > 0.1f)
            {
                float targetRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }
        }

        /// <summary>
        /// Handles gravity and ground detection
        /// </summary>
        private void HandleGravity()
        {
            // Check if grounded using multiple methods
            isGrounded = CheckGrounded();
        }
        
        /// <summary>
        /// Makes the character follow the terrain surface smoothly
        /// </summary>
        private void FollowTerrainSurface()
        {
            if (characterController == null) return;
            
            // Refresh terrain cache if needed
            if (cachedTerrain == null)
            {
                cachedTerrain = FindObjectOfType<Terrain>();
                if (cachedTerrain == null) return;
            }
            
            Vector3 currentPos = transform.position;
            float terrainHeight = cachedTerrain.SampleHeight(currentPos);
            float terrainWorldY = cachedTerrain.transform.position.y + terrainHeight;
            float characterBottom = currentPos.y - characterController.height / 2f;
            float targetY = terrainWorldY + characterController.height / 2f + 0.1f;
            
            // Calculate height difference
            float heightDifference = targetY - currentPos.y;
            
            // Always apply terrain following when grounded or close to ground
            if (isGrounded || characterBottom < terrainWorldY + 1f)
            {
                // If we're above terrain, pull down strongly
                if (heightDifference < -0.1f)
                {
                    // Calculate velocity needed to reach target height
                    float requiredVelocity = heightDifference / Time.deltaTime;
                    // Smoothly adjust velocity
                    velocity.y = Mathf.Lerp(velocity.y, requiredVelocity * 0.5f, terrainFollowStrength * Time.deltaTime);
                    // Ensure we're pulling down
                    velocity.y = Mathf.Min(velocity.y, -1f);
                }
                // If we're slightly above, apply gentle downward force
                else if (heightDifference < -0.02f)
                {
                    velocity.y = -3f; // Gentle pull down
                }
                // If we're below terrain, push up
                else if (heightDifference > 0.1f)
                {
                    // Push up to reach terrain
                    float requiredVelocity = heightDifference / Time.deltaTime;
                    velocity.y = Mathf.Lerp(velocity.y, requiredVelocity * 0.3f, terrainFollowStrength * Time.deltaTime);
                }
                // If at correct height, maintain small downward force
                else
                {
                    velocity.y = -2f; // Small downward force to keep grounded
                }
            }
            // If falling and above terrain, apply extra gravity
            else if (heightDifference < -0.2f && velocity.y < 0)
            {
                // Apply extra gravity when above terrain
                velocity.y += gravity * 1.5f * Time.deltaTime;
            }
        }

        /// <summary>
        /// Checks if the character is grounded using CharacterController and raycast
        /// </summary>
        private bool CheckGrounded()
        {
            // First check CharacterController
            if (characterController.isGrounded)
            {
                return true;
            }

            // Also use raycast for more reliable detection
            if (useRaycastGroundCheck)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
                float rayDistance = characterController.height / 2f + groundCheckDistance;
                
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance, groundLayerMask))
                {
                    groundDistance = hit.distance;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks and fixes character position relative to terrain
        /// </summary>
        private void CheckAndFixTerrainPosition()
        {
            if (characterController == null) return;
            
            // Don't fix if we're grounded and not falling - this prevents bouncing
            if (isGrounded && velocity.y >= -1f)
            {
                return; // Character is fine, don't adjust
            }

            // Refresh terrain cache if needed
            if (cachedTerrain == null)
            {
                cachedTerrain = FindObjectOfType<Terrain>();
            }

            if (cachedTerrain != null)
            {
                Vector3 worldPos = transform.position;
                float terrainHeight = cachedTerrain.SampleHeight(worldPos);
                
                // Account for terrain's world position
                float terrainWorldY = cachedTerrain.transform.position.y + terrainHeight;
                float characterBottom = transform.position.y - characterController.height / 2f;
                float targetY = terrainWorldY + characterController.height / 2f + 0.1f;
                
                // Only fix if character is significantly below terrain (using threshold)
                // And prevent infinite loops by checking time since last snap
                float distanceBelow = terrainWorldY - characterBottom;
                if (distanceBelow > terrainFixThreshold && Time.time - lastSnapTime > 0.5f)
                {
                    // Disable controller to set position directly
                    characterController.enabled = false;
                    transform.position = new Vector3(worldPos.x, targetY, worldPos.z);
                    characterController.enabled = true;
                    
                    if (velocity.y < 0)
                    {
                        velocity.y = 0f;
                    }
                    
                    lastSnapTime = Time.time;
                }
            }
        }

        /// <summary>
        /// Snaps the character to the terrain surface
        /// </summary>
        public void SnapToTerrain()
        {
            if (characterController == null) return;

            // Refresh terrain cache if needed
            if (cachedTerrain == null)
            {
                cachedTerrain = FindObjectOfType<Terrain>();
            }

            // Ensure terrain has a collider
            if (cachedTerrain != null)
            {
                TerrainCollider terrainCollider = cachedTerrain.GetComponent<TerrainCollider>();
                if (terrainCollider == null)
                {
                    terrainCollider = cachedTerrain.gameObject.AddComponent<TerrainCollider>();
                    terrainCollider.terrainData = cachedTerrain.terrainData;
                    Debug.Log("Added TerrainCollider to terrain");
                }
                else if (!terrainCollider.enabled)
                {
                    terrainCollider.enabled = true;
                    Debug.Log("Enabled TerrainCollider on terrain");
                }
            }

            // Try to find terrain first
            if (cachedTerrain != null)
            {
                Vector3 worldPos = transform.position;
                float terrainHeight = cachedTerrain.SampleHeight(worldPos);
                
                // Account for terrain's world position (terrain might not be at origin)
                float terrainWorldY = cachedTerrain.transform.position.y + terrainHeight;
                float targetY = terrainWorldY + characterController.height / 2f + 0.1f;
                
                // Always snap if we're significantly below terrain or falling
                if (transform.position.y < targetY - 1f || velocity.y < -10f || !isGrounded)
                {
                    // Disable character controller temporarily to set position directly
                    characterController.enabled = false;
                    transform.position = new Vector3(worldPos.x, targetY, worldPos.z);
                    characterController.enabled = true;
                    velocity.y = 0f;
                    Debug.Log($"Snapped player to terrain at height: {targetY} (terrain world Y: {terrainWorldY}, terrain height: {terrainHeight})");
                }
                return;
            }

            // Fallback: use raycast
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + Vector3.up * 100f;
            float maxDistance = 200f;
            
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, maxDistance, groundLayerMask))
            {
                float targetY = hit.point.y + characterController.height / 2f + 0.1f;
                
                // Always snap if we're significantly below ground or falling
                if (transform.position.y < targetY - 1f || velocity.y < -10f || !isGrounded)
                {
                    // Disable character controller temporarily to set position directly
                    characterController.enabled = false;
                    transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
                    characterController.enabled = true;
                    velocity.y = 0f;
                    Debug.Log($"Snapped player to ground using raycast at height: {targetY}");
                }
            }
            else
            {
                Debug.LogWarning("SnapToTerrain: Could not find terrain or ground!");
            }
        }

        /// <summary>
        /// Sets the camera transform reference for movement calculations
        /// </summary>
        public void SetCameraTransform(Transform camera)
        {
            cameraTransform = camera;
        }

        /// <summary>
        /// Gets the current movement speed
        /// </summary>
        public float GetCurrentSpeed()
        {
            return currentSpeed;
        }

        /// <summary>
        /// Gets whether the character is grounded
        /// </summary>
        public bool IsGrounded()
        {
            return isGrounded;
        }

        /// <summary>
        /// Context menu option to manually snap to terrain
        /// </summary>
        [ContextMenu("Snap To Terrain")]
        private void SnapToTerrainContextMenu()
        {
            SnapToTerrain();
        }
    }
}

