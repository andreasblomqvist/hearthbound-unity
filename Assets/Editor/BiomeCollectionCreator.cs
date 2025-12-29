using UnityEngine;
using UnityEditor;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Editor helper to create default biome collection with all standard biomes
    /// </summary>
    public class BiomeCollectionCreator : EditorWindow
    {
        [MenuItem("Hearthbound/Create Default Biome Collection")]
        public static void CreateDefaultBiomeCollection()
        {
            // Create folder for biome assets if it doesn't exist
            string biomeFolder = "Assets/Resources/Biomes";
            if (!AssetDatabase.IsValidFolder(biomeFolder))
            {
                string resourcesFolder = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "Biomes");
            }
            
            // Delete existing biome assets to recreate with updated ranges
            string[] biomeNames = { "Water", "Plains", "Forest", "Rock", "Snow" };
            int deletedCount = 0;
            foreach (string biomeName in biomeNames)
            {
                string biomePath = $"{biomeFolder}/{biomeName}Biome.asset";
                if (AssetDatabase.LoadAssetAtPath<BiomeData>(biomePath) != null)
                {
                    AssetDatabase.DeleteAsset(biomePath);
                    deletedCount++;
                    Debug.Log($"🗑️ Deleted existing biome: {biomePath}");
                }
            }
            
            string collectionPath = $"{biomeFolder}/DefaultBiomeCollection.asset";
            if (AssetDatabase.LoadAssetAtPath<BiomeCollection>(collectionPath) != null)
            {
                AssetDatabase.DeleteAsset(collectionPath);
                Debug.Log($"🗑️ Deleted existing collection: {collectionPath}");
            }
            
            // Refresh database to ensure deletions are processed
            AssetDatabase.Refresh();
            
            Debug.Log($"Deleted {deletedCount} existing biome assets. Will recreate with lookup table ranges.");

            // Create individual biome ScriptableObjects
            // Using Lookup Table approach: Each biome has Height, Temperature, and Humidity ranges
            // Temperature = 1 - height, so:
            //   High temperature (0.7-1.0) = Low elevation (0.0-0.3) = Water/Plains
            //   Medium temperature (0.4-0.7) = Medium elevation (0.3-0.6) = Forest/Plains
            //   Low temperature (0.0-0.4) = High elevation (0.6-1.0) = Rock/Snow
            
            // Water: Very low elevation ONLY, very high temperature, high humidity
            BiomeData waterBiome = CreateBiomeWithRanges("Water", 
                new Vector2(0f, 0.05f),    // Height: sea level only (stricter: 5% max to prevent water on mountains)
                new Vector2(0.9f, 1.0f),   // Temperature: very high (low elevation)
                new Vector2(0.7f, 1.0f),   // Humidity: high (near water)
                new Color(0.2f, 0.4f, 0.7f), 0.5f, biomeFolder);
            
            // Plains: Low-medium elevation, high-medium temperature, medium humidity
            // More yellow/golden color for plains (grassland)
            BiomeData plainsBiome = CreateBiomeWithRanges("Plains",
                new Vector2(0.1f, 0.5f),  // Height: low to medium
                new Vector2(0.5f, 0.9f),  // Temperature: medium-high
                new Vector2(0.3f, 0.7f),   // Humidity: medium
                new Color(0.6f, 0.7f, 0.2f), 1.0f, biomeFolder); // Yellow-green/golden color
            
            // Forest: Medium elevation, medium temperature, high humidity
            // Darker, richer green for forest
            BiomeData forestBiome = CreateBiomeWithRanges("Forest",
                new Vector2(0.2f, 0.6f),  // Height: medium
                new Vector2(0.4f, 0.8f),  // Temperature: medium
                new Vector2(0.6f, 1.0f),  // Humidity: high
                new Color(0.05f, 0.3f, 0.05f), 1.0f, biomeFolder); // Dark green
            
            // Rock: High elevation, low temperature, low humidity
            BiomeData rockBiome = CreateBiomeWithRanges("Rock",
                new Vector2(0.5f, 1.0f),  // Height: high (peaks)
                new Vector2(0.0f, 0.5f),  // Temperature: low (high elevation)
                new Vector2(0.0f, 0.5f),  // Humidity: low
                new Color(0.4f, 0.4f, 0.45f), 1.5f, biomeFolder);
            
            // Snow: Very high elevation, very low temperature, any humidity
            BiomeData snowBiome = CreateBiomeWithRanges("Snow",
                new Vector2(0.7f, 1.0f), // Height: very high (mountain peaks)
                new Vector2(0.0f, 0.3f), // Temperature: very low (very high elevation)
                new Vector2(0.0f, 1.0f), // Humidity: any
                new Color(0.9f, 0.95f, 1f), 1.2f, biomeFolder);

            // Create BiomeCollection
            BiomeCollection collection = ScriptableObject.CreateInstance<BiomeCollection>();
            collection.biomes = new BiomeData[] { waterBiome, plainsBiome, forestBiome, rockBiome, snowBiome };
            collection.globalBlendFactor = 3f; // Default blend factor (range 1-10)
            collection.useGlobalBlendFactor = true;

            AssetDatabase.CreateAsset(collection, collectionPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"✅ Created default biome collection at: {collectionPath}");
            Debug.Log($"   Biomes: {collection.biomes.Length}");
            Debug.Log($"   Global Blend Factor: {collection.globalBlendFactor}");
            
            // Verify the biomes have lookup table ranges set
            foreach (BiomeData biome in collection.biomes)
            {
                if (biome != null)
                {
                    Debug.Log($"   {biome.biomeName}: Height=[{biome.heightRange.x:F2}-{biome.heightRange.y:F2}], Temp=[{biome.temperatureRange.x:F2}-{biome.temperatureRange.y:F2}], Humid=[{biome.humidityRange.x:F2}-{biome.humidityRange.y:F2}]");
                }
            }
            
            // Select the created asset
            Selection.activeObject = collection;
            EditorUtility.FocusProjectWindow();
        }

        private static BiomeData CreateBiomeWithRanges(string name, Vector2 heightRange, Vector2 temperatureRange, Vector2 humidityRange,
            Color color, float heightMultiplier, string folder)
        {
            BiomeData biome = ScriptableObject.CreateInstance<BiomeData>();
            biome.biomeName = name;
            
            // Set lookup table ranges
            biome.heightRange = heightRange;
            biome.temperatureRange = temperatureRange;
            biome.humidityRange = humidityRange;
            
            // Set legacy distance-based values (for compatibility)
            biome.humidity = (humidityRange.x + humidityRange.y) * 0.5f; // Center of range
            biome.temperature = (temperatureRange.x + temperatureRange.y) * 0.5f; // Center of range
            
            biome.heightMultiplier = heightMultiplier;
            biome.blendStrength = 3f; // Default blend strength (replaces the three separate blend factors)

            // Create terrain layer data
            TerrainLayerData layerData = new TerrainLayerData();
            layerData.color = color;
            layerData.tileSize = new Vector2(15, 15);
            biome.terrainLayers = new TerrainLayerData[] { layerData };

            string path = $"{folder}/{name}Biome.asset";
            AssetDatabase.CreateAsset(biome, path);
            
            return biome;
        }
        
        // Legacy method for compatibility
        private static BiomeData CreateBiome(string name, float humidity, float temperature, 
            Color color, float heightMultiplier, string folder)
        {
            // Use default ranges centered on the point values
            return CreateBiomeWithRanges(name, 
                new Vector2(0f, 1f), // Full height range
                new Vector2(Mathf.Max(0f, temperature - 0.2f), Mathf.Min(1f, temperature + 0.2f)), // ±0.2 around temp
                new Vector2(Mathf.Max(0f, humidity - 0.2f), Mathf.Min(1f, humidity + 0.2f)), // ±0.2 around humidity
                color, heightMultiplier, folder);
        }
    }
}
