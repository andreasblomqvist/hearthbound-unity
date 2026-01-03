using UnityEngine;

namespace Hearthbound.World
{
    /// <summary>
    /// Provides query methods for terrain properties at world positions
    /// </summary>
    public class TerrainQueryService
    {
        private Terrain terrain;
        private TerrainData terrainData;

        public TerrainQueryService(Terrain terrain, TerrainData terrainData)
        {
            this.terrain = terrain;
            this.terrainData = terrainData;
        }

        /// <summary>
        /// Get terrain height at world position
        /// </summary>
        public float GetHeightAtPosition(Vector3 worldPosition)
        {
            Vector3 terrainPosition = worldPosition - terrain.transform.position;
            Vector3 normalizedPosition = new Vector3(
                terrainPosition.x / terrainData.size.x,
                0,
                terrainPosition.z / terrainData.size.z
            );

            // Get terrain-relative height and convert to world Y coordinate
            float terrainRelativeHeight = terrainData.GetInterpolatedHeight(normalizedPosition.x, normalizedPosition.z);
            return terrain.transform.position.y + terrainRelativeHeight;
        }

        /// <summary>
        /// Get terrain normal at world position
        /// </summary>
        public Vector3 GetNormalAtPosition(Vector3 worldPosition)
        {
            Vector3 terrainPosition = worldPosition - terrain.transform.position;
            Vector3 normalizedPosition = new Vector3(
                terrainPosition.x / terrainData.size.x,
                0,
                terrainPosition.z / terrainData.size.z
            );

            return terrainData.GetInterpolatedNormal(normalizedPosition.x, normalizedPosition.z);
        }

        /// <summary>
        /// Get slope in degrees at world position
        /// </summary>
        public float GetSlopeAtPosition(Vector3 worldPosition)
        {
            Vector3 terrainPosition = worldPosition - terrain.transform.position;
            Vector3 normalizedPosition = new Vector3(
                terrainPosition.x / terrainData.size.x,
                0,
                terrainPosition.z / terrainData.size.z
            );

            return terrainData.GetSteepness(normalizedPosition.x, normalizedPosition.z);
        }

        /// <summary>
        /// Check if position is valid for placement (not too steep)
        /// </summary>
        public bool IsValidPlacementPosition(Vector3 worldPosition, float maxSlope = 45f)
        {
            float slope = GetSlopeAtPosition(worldPosition);
            return slope <= maxSlope;
        }
    }
}
