using UnityEngine;

namespace Hearthbound.World
{
    /// <summary>
    /// Handles terrain component initialization and configuration
    /// </summary>
    public class TerrainInitializer
    {
        private MonoBehaviour context;
        private Terrain terrain;
        private TerrainData terrainData;

        // Terrain dimensions
        public int TerrainWidth { get; set; } = 1000;
        public int TerrainLength { get; set; } = 1000;
        public int TerrainHeight { get; set; } = 600;
        public int HeightmapResolution { get; set; } = 513;

        public TerrainInitializer(MonoBehaviour context)
        {
            this.context = context;
        }

        public Terrain Terrain => terrain;
        public TerrainData TerrainData => terrainData;

        /// <summary>
        /// Ensure terrain component is initialized
        /// </summary>
        public void EnsureTerrainInitialized()
        {
            if (terrain == null)
            {
                terrain = context.GetComponent<Terrain>();
                if (terrain == null)
                {
                    Debug.LogError("TerrainGenerator requires a Terrain component!");
                    return;
                }
            }
        }

        /// <summary>
        /// Initialize terrain data with configured dimensions
        /// </summary>
        public void InitializeTerrainData()
        {
            // Ensure terrain component is initialized
            EnsureTerrainInitialized();

            if (terrain == null)
            {
                Debug.LogError("Cannot initialize terrain data: Terrain component is missing!");
                return;
            }

            if (terrain.terrainData == null)
            {
                terrainData = new TerrainData();
                terrain.terrainData = terrainData;
                // Only set size for new terrain
                terrainData.heightmapResolution = HeightmapResolution;
                terrainData.size = new Vector3(TerrainWidth, TerrainHeight, TerrainLength);
                Debug.Log($"Terrain initialized (new): {TerrainWidth}x{TerrainLength}, Height: {TerrainHeight}");
            }
            else
            {
                terrainData = terrain.terrainData;
                // Preserve existing terrain size to prevent flattening
                Vector3 existingSize = terrainData.size;
                int existingResolution = terrainData.heightmapResolution;

                // Only update if significantly different
                bool sizeChanged = Mathf.Abs(existingSize.x - TerrainWidth) > 1f ||
                                  Mathf.Abs(existingSize.z - TerrainLength) > 1f;
                bool resolutionChanged = existingResolution != HeightmapResolution;

                if (sizeChanged || resolutionChanged)
                {
                    Debug.Log($"Terrain size/resolution changed - updating from {existingSize} (res: {existingResolution}) to {TerrainWidth}x{TerrainLength} (res: {HeightmapResolution})");
                    terrainData.heightmapResolution = HeightmapResolution;
                    // IMPORTANT: Preserve Y (height) to prevent flattening mountains!
                    terrainData.size = new Vector3(TerrainWidth, existingSize.y, TerrainLength);
                }
                else
                {
                    // Preserve everything - don't modify existing terrain
                    Debug.Log($"Terrain already initialized - preserving size: {existingSize} (res: {existingResolution})");
                }
            }

            // Ensure TerrainCollider uses the same TerrainData
            TerrainCollider terrainCollider = context.GetComponent<TerrainCollider>();
            if (terrainCollider != null && terrainCollider.terrainData != terrainData)
            {
                terrainCollider.terrainData = terrainData;
                Debug.Log("  Synced TerrainCollider with TerrainData");
            }
        }
    }
}
