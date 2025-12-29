using UnityEngine;

namespace Hearthbound.World
{
    /// <summary>
    /// Component to apply terrain style presets to TerrainGenerator
    /// Add this to a GameObject to easily switch between terrain styles
    /// </summary>
    public class TerrainStyleApplier : MonoBehaviour
    {
        [Header("Terrain Generator")]
        [Tooltip("The TerrainGenerator component to apply presets to")]
        public TerrainGenerator terrainGenerator;

        [Header("Preset")]
        [Tooltip("The terrain style preset to apply")]
        public TerrainStylePreset preset;

        [Header("Auto-Apply")]
        [Tooltip("Automatically apply preset when component starts")]
        public bool applyOnStart = false;

        private void Awake()
        {
            // Auto-find TerrainGenerator if not assigned
            if (terrainGenerator == null)
            {
                terrainGenerator = GetComponent<TerrainGenerator>();
                if (terrainGenerator == null)
                {
                    // Try to find it in parent or children
                    terrainGenerator = GetComponentInParent<TerrainGenerator>();
                    if (terrainGenerator == null)
                    {
                        terrainGenerator = GetComponentInChildren<TerrainGenerator>();
                    }
                }
            }
        }

        private void Start()
        {
            if (applyOnStart && preset != null && terrainGenerator != null)
            {
                ApplyPreset();
            }
            else if (applyOnStart && terrainGenerator == null)
            {
                Debug.LogWarning($"TerrainStyleApplier on {gameObject.name}: TerrainGenerator not found! Please assign it manually.");
            }
        }

        /// <summary>
        /// Apply the assigned preset to the terrain generator
        /// </summary>
        [ContextMenu("Apply Preset")]
        public void ApplyPreset()
        {
            if (preset == null)
            {
                Debug.LogError("Cannot apply preset: No preset assigned!");
                return;
            }

            // Try to find TerrainGenerator if not assigned
            if (terrainGenerator == null)
            {
                terrainGenerator = GetComponent<TerrainGenerator>();
                if (terrainGenerator == null)
                {
                    terrainGenerator = GetComponentInParent<TerrainGenerator>();
                    if (terrainGenerator == null)
                    {
                        terrainGenerator = GetComponentInChildren<TerrainGenerator>();
                    }
                }
                
                if (terrainGenerator == null)
                {
                    Debug.LogError($"Cannot apply preset: TerrainGenerator not found on {gameObject.name} or its parent/children! Please assign it manually in the Inspector.");
                    return;
                }
                
                Debug.Log($"Auto-found TerrainGenerator on {terrainGenerator.gameObject.name}");
            }

            preset.ApplyTo(terrainGenerator);
            Debug.Log($"✅ Applied terrain style: {preset.styleName}");
        }

        /// <summary>
        /// Apply a specific preset
        /// </summary>
        public void ApplyPreset(TerrainStylePreset newPreset)
        {
            preset = newPreset;
            ApplyPreset();
        }
    }
}

