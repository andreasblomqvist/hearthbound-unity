using UnityEngine;
using UnityEditor;
using Hearthbound.World;

namespace Hearthbound.Editor
{
    /// <summary>
    /// Editor helper to create terrain style presets from the Quick Reference Guide
    /// </summary>
    public class TerrainStylePresetCreator : EditorWindow
    {
        [MenuItem("Hearthbound/Create Terrain Style Presets")]
        public static void ShowWindow()
        {
            GetWindow<TerrainStylePresetCreator>("Terrain Style Presets");
        }

        private void OnGUI()
        {
            GUILayout.Label("Create Terrain Style Presets", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Create All Presets", GUILayout.Height(30)))
            {
                CreateAllPresets();
            }

            GUILayout.Space(20);
            GUILayout.Label("Individual Presets:", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (GUILayout.Button("Alpine Mountains"))
                CreateAlpineMountains();

            if (GUILayout.Button("Rocky Mountains"))
                CreateRockyMountains();

            if (GUILayout.Button("Appalachian Style"))
                CreateAppalachianStyle();

            if (GUILayout.Button("Plains with Distant Mountains"))
                CreatePlainsWithDistantMountains();

            if (GUILayout.Button("Himalayan Style"))
                CreateHimalayanStyle();

            if (GUILayout.Button("Fantasy RPG (Recommended)"))
                CreateFantasyRPG();
        }

        private static void CreateAllPresets()
        {
            CreateAlpineMountains();
            CreateRockyMountains();
            CreateAppalachianStyle();
            CreatePlainsWithDistantMountains();
            CreateHimalayanStyle();
            CreateFantasyRPG();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("✅ Created all terrain style presets!");
        }

        private static string GetPresetFolder()
        {
            string folder = "Assets/Resources/TerrainStyles";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string resourcesFolder = "Assets/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateFolder("Assets/Resources", "TerrainStyles");
            }
            return folder;
        }

        private static TerrainStylePreset CreatePreset(string name, string description, 
            float baseHeight, float hillHeight, float mountainHeight,
            float snowHeight, float continentalThreshold, float warpStrength,
            float mountainFrequency = 0.0008f, float peakSharpness = 1.3f)
        {
            TerrainStylePreset preset = ScriptableObject.CreateInstance<TerrainStylePreset>();
            preset.styleName = name;
            preset.description = description;
            preset.baseHeight = baseHeight;
            preset.hillHeight = hillHeight;
            preset.mountainHeight = mountainHeight;
            preset.snowHeight = snowHeight;
            preset.continentalThreshold = continentalThreshold;
            preset.warpStrength = warpStrength;
            preset.mountainFrequency = mountainFrequency;
            preset.peakSharpness = peakSharpness;
            
            // Create exponential height curve
            preset.heightCurve = new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.5f, 0.3f),
                new Keyframe(1, 1)
            );
            preset.heightCurve.SmoothTangents(1, 0.5f);
            
            string folder = GetPresetFolder();
            string path = $"{folder}/{name.Replace(" ", "")}.asset";
            AssetDatabase.CreateAsset(preset, path);
            
            Debug.Log($"✅ Created preset: {name}");
            return preset;
        }

        private static void CreateAlpineMountains()
        {
            CreatePreset(
                "Alpine Mountains",
                "Tall, sharp peaks with snow, valleys between ranges. Like Swiss Alps.",
                50f, 120f, 600f, 0.70f, 0.5f, 150f
            );
        }

        private static void CreateRockyMountains()
        {
            CreatePreset(
                "Rocky Mountains",
                "Extensive mountain ranges, high elevation overall. Like Colorado.",
                80f, 180f, 550f, 0.75f, 0.45f, 180f
            );
        }

        private static void CreateAppalachianStyle()
        {
            CreatePreset(
                "Appalachian Style",
                "Older, more eroded mountains, less dramatic peaks.",
                60f, 200f, 400f, 0.85f, 0.6f, 120f
            );
        }

        private static void CreatePlainsWithDistantMountains()
        {
            CreatePreset(
                "Plains with Distant Mountains",
                "Mostly flat with dramatic mountain ranges on horizon.",
                40f, 100f, 650f, 0.68f, 0.65f, 200f
            );
        }

        private static void CreateHimalayanStyle()
        {
            CreatePreset(
                "Himalayan Style",
                "Very high elevation overall, extreme peaks, lots of snow.",
                100f, 200f, 800f, 0.60f, 0.55f, 140f
            );
        }

        private static void CreateFantasyRPG()
        {
            CreatePreset(
                "Fantasy RPG (Recommended)",
                "Varied, playable terrain for open world RPG. Balanced distribution.",
                60f, 140f, 550f, 0.70f, 0.5f, 150f
            );
        }
    }
}

