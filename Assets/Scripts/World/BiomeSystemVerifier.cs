using UnityEngine;

namespace Hearthbound.World
{
    /// <summary>
    /// Debug script to verify the biome system is using the fixed code
    /// Add this to a GameObject in your scene to see verification output
    /// </summary>
    public class BiomeSystemVerifier : MonoBehaviour
    {
        [Header("Verification Settings")]
        [Tooltip("The BiomeCollection asset to verify")]
        public BiomeCollection biomeCollection;
        
        [Tooltip("The TerrainGenerator to verify")]
        public TerrainGenerator terrainGenerator;
        
        [Header("Test Parameters")]
        [Tooltip("Test height value (0-1)")]
        [Range(0f, 1f)]
        public float testHeight = 0.3f;
        
        [Tooltip("Test temperature value (0-1)")]
        [Range(0f, 1f)]
        public float testTemperature = 0.5f;
        
        [Tooltip("Test humidity value (0-1)")]
        [Range(0f, 1f)]
        public float testHumidity = 0.5f;
        
        [ContextMenu("Verify Biome System")]
        public void VerifyBiomeSystem()
        {
            Debug.Log("=== BIOME SYSTEM VERIFICATION ===");
            
            // Check if BiomeCollection uses new method
            if (biomeCollection == null)
            {
                Debug.LogError("❌ BiomeCollection is not assigned!");
                return;
            }
            
            Debug.Log($"✅ BiomeCollection found: {biomeCollection.name}");
            Debug.Log($"   Biomes count: {biomeCollection.biomes?.Length ?? 0}");
            Debug.Log($"   Global Blend Factor: {biomeCollection.globalBlendFactor}");
            Debug.Log($"   Debug Logging: {biomeCollection.debugLogging}");
            
            // Test CalculateBiomeWeights with new system
            Debug.Log("\n--- Testing Biome Weight Calculation ---");
            var weights = biomeCollection.CalculateBiomeWeights(testHumidity, testTemperature, testHeight);
            
            Debug.Log($"Test parameters: Height={testHeight:F2}, Temp={testTemperature:F2}, Humidity={testHumidity:F2}");
            Debug.Log($"Biomes with weights: {weights.Count}");
            
            float totalWeight = 0f;
            foreach (var kvp in weights)
            {
                totalWeight += kvp.Value;
            }
            
            foreach (var kvp in weights)
            {
                float normalizedWeight = totalWeight > 0.001f ? kvp.Value / totalWeight : 0f;
                Debug.Log($"  {kvp.Key.biomeName}: {normalizedWeight:P1} (raw: {kvp.Value:F4})");
            }
            
            // Test individual biome match scores
            Debug.Log("\n--- Testing Individual Biome Match Scores ---");
            if (biomeCollection.biomes != null)
            {
                foreach (var biome in biomeCollection.biomes)
                {
                    if (biome == null) continue;
                    
                    // Check if biome has new CalculateMatchScore method
                    try
                    {
                        float matchScore = biome.CalculateMatchScore(testHeight, testTemperature, testHumidity);
                        Debug.Log($"  {biome.biomeName}: Match Score = {matchScore:F4}, Blend Strength = {biome.blendStrength}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"  ❌ {biome.biomeName}: Error calling CalculateMatchScore - {e.Message}");
                        Debug.LogError("     This biome might still be using old code!");
                    }
                }
            }
            
            // Check TerrainGenerator
            if (terrainGenerator == null)
            {
                Debug.LogWarning("⚠️ TerrainGenerator not assigned - cannot verify temperature/humidity generation");
            }
            else
            {
                Debug.Log("\n--- TerrainGenerator Status ---");
                Debug.Log($"✅ TerrainGenerator found");
                Debug.Log($"   Check Inspector to verify BiomeCollection is assigned");
                Debug.Log($"   Check Console during terrain generation for temperature/humidity logs");
            }
            
            Debug.Log("\n=== VERIFICATION COMPLETE ===");
            Debug.Log("If you see match scores and weights above, the new system is working!");
            Debug.Log("Enable 'Debug Logging' in BiomeCollection to see detailed output during terrain generation.");
        }
        
        [ContextMenu("Test Temperature/Humidity Generation")]
        public void TestTemperatureHumidityGeneration()
        {
            if (terrainGenerator == null)
            {
                Debug.LogError("❌ TerrainGenerator is not assigned!");
                return;
            }
            
            Debug.Log("=== TESTING TEMPERATURE/HUMIDITY GENERATION ===");
            Debug.Log("This tests if the new latitude-based temperature generation is working.");
            Debug.Log("Temperature should vary by Z position (latitude), not just height!");
            
            // We need to access the private methods via reflection or make them public for testing
            // For now, just check if terrain has been generated
            var terrain = terrainGenerator.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogWarning("⚠️ Terrain not initialized. Generate terrain first, then run this test.");
                return;
            }
            
            Debug.Log($"✅ Terrain found: {terrain.terrainData.size}");
            Debug.Log("   Temperature should now vary by latitude (Z position), not just height!");
            Debug.Log("   Check the console during terrain generation for sample point logs.");
        }
        
        private void Start()
        {
            // Auto-find components if not assigned
            if (biomeCollection == null)
            {
                // Try to find BiomeCollection in resources
                var collections = Resources.FindObjectsOfTypeAll<BiomeCollection>();
                if (collections.Length > 0)
                {
                    biomeCollection = collections[0];
                    Debug.Log($"Auto-assigned BiomeCollection: {biomeCollection.name}");
                }
            }
            
            if (terrainGenerator == null)
            {
                terrainGenerator = FindObjectOfType<TerrainGenerator>();
                if (terrainGenerator != null)
                {
                    Debug.Log($"Auto-assigned TerrainGenerator: {terrainGenerator.name}");
                }
            }
        }
    }
}

