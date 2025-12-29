using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hearthbound.World
{
    /// <summary>
    /// Generates a flat water plane at sea level
    /// Water fills low-lying areas of the terrain
    /// </summary>
    public class WaterGenerator : MonoBehaviour
    {
        [Header("Water Settings")]
        [SerializeField] private float seaLevel = 0f; // Sea level height (in world units, typically 0 or slightly above)
        [SerializeField] [Range(0f, 0.2f)] private float seaLevelHeightRatio = 0.05f; // Sea level as ratio of terrain height (0.05 = 5% of max height)
        [SerializeField] private Material waterMaterial;
        [SerializeField] private bool generateOnTerrainGenerate = true;
        
        [Header("References")]
        [SerializeField] private TerrainGenerator terrainGenerator;
        
        private GameObject waterPlane;

        private void Start()
        {
            if (terrainGenerator == null)
            {
                terrainGenerator = FindObjectOfType<TerrainGenerator>();
            }
        }

        /// <summary>
        /// Generate water plane at sea level
        /// </summary>
        public void GenerateWater()
        {
            ClearWater();
            
            if (terrainGenerator == null)
            {
                terrainGenerator = FindObjectOfType<TerrainGenerator>();
                if (terrainGenerator == null)
                {
                    Debug.LogWarning("WaterGenerator: No TerrainGenerator found!");
                    return;
                }
            }

            var terrain = terrainGenerator.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogWarning("WaterGenerator: Terrain data not available!");
                return;
            }

                   // Get terrain dimensions
                   Vector3 terrainSize = terrain.terrainData.size;
                   Vector3 terrainPosition = terrain.transform.position;

                   // Calculate sea level based on terrain height
                   // Use either explicit seaLevel or calculate from terrain height ratio
                   float actualSeaLevel = seaLevel;
                   if (seaLevelHeightRatio > 0f)
                   {
                       // Calculate sea level as a percentage of terrain height
                       actualSeaLevel = terrainPosition.y + (terrainSize.y * seaLevelHeightRatio);
                   }
                   else
                   {
                       // Use explicit sea level value
                       actualSeaLevel = terrainPosition.y + seaLevel;
                   }

                   // Create water plane
                   waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                   waterPlane.name = "Water";
                   waterPlane.transform.SetParent(transform);
                   
                   // Position water plane at sea level, centered on terrain
                   // Scale plane to match terrain size (Plane is 10x10 units by default)
                   float scaleX = terrainSize.x / 10f;
                   float scaleZ = terrainSize.z / 10f;
                   waterPlane.transform.localScale = new Vector3(scaleX, 1f, scaleZ);
                   
                   // Position at terrain center X/Z, but at calculated sea level Y
                   waterPlane.transform.position = new Vector3(
                       terrainPosition.x + terrainSize.x * 0.5f,
                       actualSeaLevel,
                       terrainPosition.z + terrainSize.z * 0.5f
                   );
            
            // Rotate 180 degrees because Unity's plane faces down by default
            waterPlane.transform.rotation = Quaternion.Euler(0, 0, 0);

            // Apply water material if provided
            if (waterMaterial != null)
            {
                var renderer = waterPlane.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = waterMaterial;
                }
            }
            else
            {
                // Create a simple blue water material
                CreateDefaultWaterMaterial();
            }

                   Debug.Log($"💧 Water plane generated at sea level {actualSeaLevel:F2} (ratio: {seaLevelHeightRatio:P0}, terrain height: {terrainSize.y}), size: {terrainSize.x}x{terrainSize.z}");
        }

        /// <summary>
        /// Create a simple default water material
        /// </summary>
        private void CreateDefaultWaterMaterial()
        {
            var renderer = waterPlane.GetComponent<Renderer>();
            if (renderer == null) return;

            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.2f, 0.4f, 0.7f, 0.8f); // Blue with transparency
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0.8f);
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            renderer.material = mat;
        }

        /// <summary>
        /// Clear/remove water plane
        /// </summary>
        public void ClearWater()
        {
            // Clear the tracked water plane
            if (waterPlane != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(waterPlane);
                }
                else
#endif
                {
                    Destroy(waterPlane);
                }
                waterPlane = null;
            }
            
            // Also clear any child objects named "Water" (in case of duplicates)
            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child.name == "Water" || child.name.StartsWith("Water"))
                {
                    childrenToDestroy.Add(child.gameObject);
                }
            }
            
            foreach (GameObject obj in childrenToDestroy)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(obj);
                }
                else
#endif
                {
                    Destroy(obj);
                }
            }
        }

        /// <summary>
        /// Context menu to generate water
        /// </summary>
        [ContextMenu("Generate Water")]
        private void GenerateWaterContextMenu()
        {
            GenerateWater();
        }

        /// <summary>
        /// Context menu to clear water
        /// </summary>
        [ContextMenu("Clear Water")]
        private void ClearWaterContextMenu()
        {
            ClearWater();
        }
    }
}
