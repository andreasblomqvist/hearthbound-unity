using UnityEngine;
using System.Collections.Generic;

namespace Hearthbound.World
{
    /// <summary>
    /// Generates water GameObject meshes for rivers and lakes
    /// </summary>
    public class RiverWaterSystem
    {
        private List<GameObject> riverWaterObjects = new List<GameObject>();
        private Transform parentTransform;

        // Configuration
        public bool GenerateWater { get; set; } = true;
        public Material WaterMaterial { get; set; }
        public float RiverWidth { get; set; } = 40f;
        public float LakeRadius { get; set; } = 150f;

        public RiverWaterSystem(Transform parent)
        {
            parentTransform = parent;
        }

        /// <summary>
        /// Generate river water GameObjects along the river path
        /// </summary>
        public void GenerateRiverWater(List<Vector2> riverPath, Terrain terrain, TerrainData terrainData)
        {
            if (riverPath == null || riverPath.Count < 2)
            {
                Debug.LogWarning("⚠️ Cannot generate river water: No valid river path");
                return;
            }

            if (terrain == null || terrainData == null)
            {
                Debug.LogWarning("⚠️ Cannot generate river water: Terrain or TerrainData is null");
                return;
            }

            Vector3 terrainPos = terrain.transform.position;

            // Generate a single continuous mesh for the entire river
            GameObject riverWaterMesh = CreateRiverWaterMesh(riverPath, terrain, terrainPos);
            if (riverWaterMesh != null)
            {
                riverWaterObjects.Add(riverWaterMesh);
            }

            // Generate circular lake at the end
            if (riverPath.Count > 0)
            {
                Vector2 lakeCenter = RiverPathGenerator.GetLakeCenter(riverPath);
                Vector3 lakeCenterWorld = new Vector3(lakeCenter.x, 0, lakeCenter.y) + terrainPos;
                float terrainLakeHeight = terrain.SampleHeight(lakeCenterWorld);

                // Raise water higher to be more visible in the carved basin
                float lakeHeight = terrainLakeHeight + 2.0f;

                // Make lake slightly larger for better visual coverage
                GameObject lakeWater = CreateCircularLake(lakeCenterWorld + Vector3.up * lakeHeight, LakeRadius * 1.1f);
                if (lakeWater != null)
                {
                    riverWaterObjects.Add(lakeWater);
                }
            }

            Debug.Log($"💧 Generated {riverWaterObjects.Count} water objects along river path (1 continuous mesh + lake)");
        }

        /// <summary>
        /// Create a single continuous mesh for the entire river path
        /// </summary>
        private GameObject CreateRiverWaterMesh(List<Vector2> riverPath, Terrain terrain, Vector3 terrainPos)
        {
            if (riverPath == null || riverPath.Count < 2)
                return null;

            GameObject riverWater = new GameObject("RiverWater");
            riverWater.transform.SetParent(parentTransform);

            MeshFilter mf = riverWater.AddComponent<MeshFilter>();
            MeshRenderer mr = riverWater.AddComponent<MeshRenderer>();

            // Build vertices and triangles for the entire river
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float halfWidth = RiverWidth * 0.6f; // Slightly wider for better coverage

            // Generate vertices for each segment
            for (int i = 0; i < riverPath.Count; i++)
            {
                Vector2 pathPoint = riverPath[i];
                Vector3 worldPos = new Vector3(pathPoint.x, 0, pathPoint.y) + terrainPos;

                // Sample terrain height at this point
                float terrainHeight = terrain.SampleHeight(worldPos);
                // Raise water higher to be more visible in the carved valley
                float waterHeight = terrainHeight + 2.0f;

                Vector3 centerVertex = worldPos + Vector3.up * waterHeight;

                // Calculate direction for this point
                Vector3 direction = Vector3.forward;
                if (i < riverPath.Count - 1)
                {
                    Vector2 nextPoint = riverPath[i + 1];
                    Vector3 nextWorld = new Vector3(nextPoint.x, 0, nextPoint.y) + terrainPos;
                    direction = (nextWorld - worldPos).normalized;
                }
                else if (i > 0)
                {
                    Vector2 prevPoint = riverPath[i - 1];
                    Vector3 prevWorld = new Vector3(prevPoint.x, 0, prevPoint.y) + terrainPos;
                    direction = (worldPos - prevWorld).normalized;
                }

                // Calculate perpendicular for width
                Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized * halfWidth;

                // Add two vertices (left and right edge)
                vertices.Add(centerVertex + perpendicular);
                vertices.Add(centerVertex - perpendicular);

                // UV coordinates
                float u = i / (float)(riverPath.Count - 1);
                uvs.Add(new Vector2(u, 0));
                uvs.Add(new Vector2(u, 1));
            }

            // Build triangles connecting segments
            for (int i = 0; i < riverPath.Count - 1; i++)
            {
                int baseIndex = i * 2;

                // First triangle
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);

                // Second triangle
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 2);
            }

            // Create and assign mesh
            Mesh mesh = new Mesh();
            mesh.name = "RiverWaterMesh";
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.mesh = mesh;

            // Apply material
            ApplyWaterMaterial(mr);

            return riverWater;
        }

        /// <summary>
        /// Create a circular lake
        /// </summary>
        private GameObject CreateCircularLake(Vector3 center, float radius)
        {
            GameObject lake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lake.name = "LakeWater";
            lake.transform.SetParent(parentTransform);
            lake.transform.position = center;
            lake.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);

            // Remove collider
            Collider col = lake.GetComponent<Collider>();
            if (col != null)
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Object.DestroyImmediate(col);
                }
                else
                #endif
                {
                    Object.Destroy(col);
                }
            }

            MeshRenderer mr = lake.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                ApplyWaterMaterial(mr);
            }

            return lake;
        }

        /// <summary>
        /// Apply water material to a mesh renderer
        /// </summary>
        private void ApplyWaterMaterial(MeshRenderer mr)
        {
            if (WaterMaterial != null)
            {
                mr.material = WaterMaterial;
            }
            else
            {
                Material waterMat = new Material(Shader.Find("Standard"));
                waterMat.color = new Color(0.2f, 0.4f, 0.7f, 0.8f);
                waterMat.SetFloat("_Metallic", 0f);
                waterMat.SetFloat("_Glossiness", 0.8f);
                waterMat.SetFloat("_Mode", 3);
                waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                waterMat.SetInt("_ZWrite", 0);
                waterMat.DisableKeyword("_ALPHATEST_ON");
                waterMat.EnableKeyword("_ALPHABLEND_ON");
                waterMat.renderQueue = 3000;
                mr.material = waterMat;
            }
        }

        /// <summary>
        /// Clear all generated river water objects
        /// </summary>
        public void ClearRiverWater()
        {
            foreach (GameObject waterObj in riverWaterObjects)
            {
                if (waterObj != null)
                {
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        Object.DestroyImmediate(waterObj);
                    }
                    else
                    #endif
                    {
                        Object.Destroy(waterObj);
                    }
                }
            }
            riverWaterObjects.Clear();
        }
    }
}
