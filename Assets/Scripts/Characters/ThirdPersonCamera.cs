using UnityEngine;

namespace Hearthbound.Characters
{
    /// <summary>
    /// Third-person camera controller
    /// Handles camera follow, mouse look, and smooth camera movement
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        #region Target Reference
        [Header("Target")]
        [SerializeField] private Transform target; // Character to follow
        #endregion

        #region Camera Settings
        [Header("Camera Position")]
        [SerializeField] private float distance = 5f;
        [SerializeField] private float heightOffset = 2f;
        [SerializeField] private Vector3 cameraOffset = Vector3.zero;
        #endregion

        #region Mouse Look Settings
        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivityX = 2f;
        [SerializeField] private float mouseSensitivityY = 2f;
        [SerializeField] private float minPitchAngle = -30f;
        [SerializeField] private float maxPitchAngle = 60f;
        [SerializeField] private bool invertY = false;
        #endregion

        #region Smoothing Settings
        [Header("Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.1f;
        [SerializeField] private float rotationSmoothTime = 0.1f;
        #endregion

        #region Cursor Settings
        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;
        [SerializeField] private KeyCode unlockCursorKey = KeyCode.Escape;
        #endregion

        #region Private Variables
        private float currentYaw;
        private float currentPitch;
        private Vector3 currentVelocity;
        private bool cursorLocked = true;
        #endregion

        void Start()
        {
            // Initialize camera rotation based on current rotation
            Vector3 euler = transform.eulerAngles;
            currentYaw = euler.y;
            currentPitch = euler.x;

            // Normalize pitch to -180 to 180 range
            if (currentPitch > 180f)
            {
                currentPitch -= 360f;
            }

            // Lock cursor
            if (lockCursor)
            {
                LockCursor();
            }

            // Auto-find target if not assigned
            if (target == null)
            {
                ThirdPersonController controller = FindObjectOfType<ThirdPersonController>();
                if (controller != null)
                {
                    target = controller.transform;
                }
            }
        }

        void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandleCursorLock();
            HandleMouseLook();
            UpdateCameraPosition();
        }

        /// <summary>
        /// Handles cursor lock/unlock
        /// </summary>
        private void HandleCursorLock()
        {
            if (Input.GetKeyDown(unlockCursorKey))
            {
                cursorLocked = false;
                UnlockCursor();
            }

            if (Input.GetMouseButtonDown(0) && !cursorLocked)
            {
                cursorLocked = true;
                LockCursor();
            }
        }

        /// <summary>
        /// Handles mouse look input
        /// </summary>
        private void HandleMouseLook()
        {
            if (!cursorLocked)
            {
                return;
            }

            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

            // Invert Y if needed
            if (invertY)
            {
                mouseY = -mouseY;
            }

            // Update yaw (horizontal rotation)
            currentYaw += mouseX;

            // Update pitch (vertical rotation) with clamping
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minPitchAngle, maxPitchAngle);
        }

        /// <summary>
        /// Updates camera position and rotation
        /// </summary>
        private void UpdateCameraPosition()
        {
            // Calculate target position (character position + height offset)
            Vector3 targetPosition = target.position + Vector3.up * heightOffset;

            // Calculate camera direction based on yaw and pitch
            float yawRad = currentYaw * Mathf.Deg2Rad;
            float pitchRad = currentPitch * Mathf.Deg2Rad;

            // Calculate offset direction (spherical coordinates)
            Vector3 offsetDirection = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            );

            // Calculate desired camera position
            Vector3 desiredPosition = targetPosition + offsetDirection * distance + cameraOffset;

            // Smoothly move camera to desired position
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);

            // Look at target
            Vector3 lookDirection = (targetPosition - transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime / rotationSmoothTime);
            }
        }

        /// <summary>
        /// Locks cursor to center of screen
        /// </summary>
        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Unlocks cursor
        /// </summary>
        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Sets the target to follow
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Gets the current yaw rotation
        /// </summary>
        public float GetYaw()
        {
            return currentYaw;
        }

        /// <summary>
        /// Gets the current pitch rotation
        /// </summary>
        public float GetPitch()
        {
            return currentPitch;
        }
    }
}

