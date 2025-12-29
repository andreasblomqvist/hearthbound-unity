using UnityEngine;
using Hearthbound.World;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hearthbound.Utilities
{
    /// <summary>
    /// Automatically positions the camera for optimal terrain viewing
    /// Positions camera at a good angle to see the generated terrain
    /// </summary>
    public class CameraPositioner : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private bool positionOnStart = true;
        [SerializeField] private float heightMultiplier = 0.4f; // Camera height as fraction of terrain height
        [SerializeField] private float distanceMultiplier = 0.6f; // Distance from center as fraction of terrain size
        [SerializeField] private float pitchAngle = 45f; // Angle looking down (degrees) - increased for better view
        [SerializeField] private float yawAngle = 45f; // Rotation around terrain (degrees)
        
        [Header("Auto-Find")]
        [SerializeField] private bool findTerrainAutomatically = true;
        [SerializeField] private TerrainGenerator terrainGenerator;

        private Camera cam;

        void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogWarning("CameraPositioner: No Camera component found!");
                return;
            }

            if (positionOnStart)
            {
                PositionCamera();
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // In editor, position camera when terrain is available (non-destructive, won't override manual positioning in play mode)
            if (!Application.isPlaying && positionOnStart)
            {
                // Small delay to ensure terrain is generated
                EditorApplication.delayCall += () =>
                {
                    if (this != null && gameObject != null)
                    {
                        PositionCamera();
                    }
                };
            }
        }
#endif

        /// <summary>
        /// Position the camera based on terrain size
        /// </summary>
        public void PositionCamera()
        {
            if (findTerrainAutomatically && terrainGenerator == null)
            {
                terrainGenerator = FindObjectOfType<TerrainGenerator>();
            }

            if (terrainGenerator == null)
            {
                Debug.LogWarning("CameraPositioner: No TerrainGenerator found! Using default position.");
                SetDefaultPosition();
                return;
            }

            // Get terrain data
            var terrain = terrainGenerator.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogWarning("CameraPositioner: Terrain data not available! Using default position.");
                SetDefaultPosition();
                return;
            }

            Vector3 terrainSize = terrain.terrainData.size;
            Vector3 terrainCenter = terrain.transform.position + terrainSize * 0.5f;

            // Calculate camera position
            float terrainMaxSize = Mathf.Max(terrainSize.x, terrainSize.z);
            float cameraHeight = terrainSize.y * heightMultiplier;
            float cameraDistance = terrainMaxSize * distanceMultiplier;

            // Position camera at angle from center
            float angleRad = yawAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Sin(angleRad) * cameraDistance,
                cameraHeight,
                Mathf.Cos(angleRad) * cameraDistance
            );

            Vector3 cameraPosition = terrainCenter + offset;
            transform.position = cameraPosition;

            // Calculate rotation: look at terrain center with pitch
            Vector3 directionToCenter = (terrainCenter - cameraPosition).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToCenter);
            
            // Apply additional pitch to look more down
            transform.rotation = lookRotation * Quaternion.Euler(pitchAngle, 0, 0);

            Debug.Log($"📷 Camera positioned: {cameraPosition}, Looking at: {terrainCenter}, Rotation: {transform.rotation.eulerAngles}");
        }

        /// <summary>
        /// Set a default camera position if terrain is not available
        /// </summary>
        private void SetDefaultPosition()
        {
            transform.position = new Vector3(500, 300, 500);
            transform.rotation = Quaternion.Euler(30, 45, 0);
        }

        /// <summary>
        /// Context menu for manual positioning
        /// </summary>
        [ContextMenu("Reposition Camera")]
        private void RepositionCamera()
        {
            PositionCamera();
        }
    }
}

