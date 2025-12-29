using UnityEngine;
using UnityEditor;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Tool to explain how humidity works in the biome system
    /// </summary>
    public class ExplainHumiditySystem : EditorWindow
    {
        [MenuItem("Hearthbound/Explain: How Humidity Works")]
        public static void ShowWindow()
        {
            EditorUtility.DisplayDialog("How Humidity Works in Biome System",
                "HUMIDITY is generated using noise patterns across the terrain.\n\n" +
                "HOW IT WORKS:\n" +
                "• Humidity = moisture/rainfall patterns (0.0 = dry, 1.0 = wet)\n" +
                "• Generated using noise (random but consistent patterns)\n" +
                "• Slightly higher near low elevations (rain collects in valleys)\n" +
                "• Creates 'wet zones' and 'dry zones' across the terrain\n\n" +
                "HOW IT AFFECTS BIOMES:\n" +
                "• Plains: LOW humidity (0.0-0.5) = dry grasslands\n" +
                "• Forest: HIGH humidity (0.5-1.0) = wet areas with trees\n" +
                "• Rock: LOW humidity (0.0-0.4) = dry mountains\n" +
                "• Water: VERY HIGH humidity (0.8-1.0) = water bodies\n\n" +
                "RESULT:\n" +
                "At the same height (plains level), you get:\n" +
                "• Wet areas → Forests (green)\n" +
                "• Dry areas → Plains (yellow-green)\n\n" +
                "This creates natural variation - forests appear in wetter regions, plains in drier regions, even at the same elevation!",
                "Got it!");
        }
    }
}

