using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hearthbound.World
{
    /// <summary>
    /// Manages existing water planes in the scene, removing them when the river system is used
    /// </summary>
    public class WaterPlaneManager
    {
        private MonoBehaviour context;

        // Configuration
        public bool DisableWaterGenerator { get; set; } = true;
        public bool DisableWaterBiomes { get; set; } = true;

        public WaterPlaneManager(MonoBehaviour context)
        {
            this.context = context;
        }

        /// <summary>
        /// Handle WaterGenerator component - disable it and clear water planes
        /// </summary>
        public void HandleWaterGenerator()
        {
            if (!DisableWaterGenerator)
                return;

            // First, find and destroy any existing water planes in the scene
            ClearAllWaterPlanes();

            // Find WaterGenerator component
            WaterGenerator waterGen = context.GetComponent<WaterGenerator>();
            if (waterGen == null)
            {
                waterGen = context.GetComponentInParent<WaterGenerator>();
                if (waterGen == null)
                {
                    waterGen = context.GetComponentInChildren<WaterGenerator>();
                }
            }

            // Also check in the scene
            if (waterGen == null)
            {
                waterGen = Object.FindObjectOfType<WaterGenerator>();
            }

            if (waterGen != null)
            {
                waterGen.enabled = false;
                waterGen.ClearWater();
                Debug.Log("🌊 WaterGenerator disabled (river system is handling water)");
            }
            else
            {
                Debug.Log("ℹ️ No WaterGenerator found to disable");
            }
        }

        /// <summary>
        /// Clear all water plane GameObjects from the scene
        /// </summary>
        public void ClearAllWaterPlanes()
        {
            List<GameObject> waterObjects = new List<GameObject>();
            WaterGenerator[] waterGens = new WaterGenerator[0];

            #if UNITY_EDITOR
            // Method 1: Search ALL GameObjects in the scene (including inactive)
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                // Skip prefabs (assets, not scene instances)
                if (PrefabUtility.IsPartOfPrefabAsset(obj))
                    continue;

                // Skip if not in a scene
                if (obj.scene.name == null)
                    continue;

                // Check by name
                if (obj != null && (obj.name == "Water" || obj.name.StartsWith("Water")))
                {
                    if (!waterObjects.Contains(obj))
                    {
                        waterObjects.Add(obj);
                        Debug.Log($"Found water object by name: {obj.name} at path: {GetGameObjectPath(obj)}");
                    }
                }
            }

            // Method 2: Find all WaterGenerator components (including inactive)
            waterGens = Object.FindObjectsOfType<WaterGenerator>(true);
            foreach (WaterGenerator waterGen in waterGens)
            {
                if (waterGen != null)
                {
                    Debug.Log($"Found WaterGenerator component, calling ClearWater()");
                    waterGen.ClearWater();

                    // Also check children of WaterGenerator for water objects
                    Transform waterGenTransform = waterGen.transform;
                    for (int i = waterGenTransform.childCount - 1; i >= 0; i--)
                    {
                        Transform child = waterGenTransform.GetChild(i);
                        if (child != null && child.gameObject != null)
                        {
                            if (child.name == "Water" || child.name.StartsWith("Water"))
                            {
                                if (!waterObjects.Contains(child.gameObject))
                                {
                                    waterObjects.Add(child.gameObject);
                                    Debug.Log($"Found water object as child of WaterGenerator: {GetGameObjectPath(child.gameObject)}");
                                }
                            }
                        }
                    }
                }
            }

            // Method 3: Find all Plane meshes that look like water (aggressive search)
            MeshFilter[] meshFilters = Object.FindObjectsOfType<MeshFilter>(true);
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null)
                    continue;

                string meshName = mf.sharedMesh.name;
                if (meshName.Contains("Plane") || meshName == "Quad" || meshName.StartsWith("Plane"))
                {
                    MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        bool isWaterLike = false;

                        // Check material
                        if (mr.sharedMaterial != null)
                        {
                            Material mat = mr.sharedMaterial;
                            Color matColor = mat.color;

                            // Blue materials
                            if (matColor.b > 0.3f && matColor.b > matColor.r && matColor.b > matColor.g)
                                isWaterLike = true;
                            // Material name contains water or blue
                            string matNameLower = mat.name.ToLower();
                            if (matNameLower.Contains("water") || matNameLower.Contains("blue") || matNameLower.Contains("aqua"))
                                isWaterLike = true;
                        }

                        // Large scale (water planes are scaled large)
                        Vector3 scale = mf.transform.lossyScale;
                        if (scale.x > 30f || scale.z > 30f)
                        {
                            isWaterLike = true;
                            Debug.Log($"Found large plane (potential water): {mf.gameObject.name} at {GetGameObjectPath(mf.gameObject)}, scale: {scale.x:F1}x{scale.z:F1}");
                        }

                        // Check if it's at a low Y position (sea level)
                        float yPos = mf.transform.position.y;
                        if (yPos < 100f)
                            isWaterLike = true;

                        // If GameObject name suggests water
                        if (mf.gameObject.name.ToLower().Contains("water"))
                            isWaterLike = true;

                        if (isWaterLike && !waterObjects.Contains(mf.gameObject))
                        {
                            waterObjects.Add(mf.gameObject);
                            Debug.Log($"Found water plane by mesh/material: {mf.gameObject.name} at path: {GetGameObjectPath(mf.gameObject)}");
                        }
                    }
                }
            }
            #else
            // Runtime fallback
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj != null && (obj.name == "Water" || obj.name.StartsWith("Water")))
                {
                    if (!waterObjects.Contains(obj))
                        waterObjects.Add(obj);
                }
            }
            waterGens = Object.FindObjectsOfType<WaterGenerator>(true);
            foreach (WaterGenerator waterGen in waterGens)
            {
                if (waterGen != null)
                    waterGen.ClearWater();
            }
            #endif

            // Destroy all found water objects
            int destroyedCount = 0;
            foreach (GameObject waterObj in waterObjects)
            {
                if (waterObj != null)
                {
                    Debug.Log($"🗑️ Destroying water object: {GetGameObjectPath(waterObj)}");
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        Object.DestroyImmediate(waterObj);
                        destroyedCount++;
                    }
                    else
                    #endif
                    {
                        Object.Destroy(waterObj);
                        destroyedCount++;
                    }
                }
            }

            if (destroyedCount > 0 || waterGens.Length > 0)
            {
                Debug.Log($"🗑️ Successfully removed {destroyedCount} water plane(s) from scene");
            }
            else
            {
                Debug.LogWarning("⚠️ No water planes found to remove. Check Console for search details.");
            }
        }

        /// <summary>
        /// Get full hierarchy path for a GameObject (for debugging)
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null)
                return "null";

            string path = obj.name;
            Transform current = obj.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }
    }
}
