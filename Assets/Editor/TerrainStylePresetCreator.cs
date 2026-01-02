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

        private static TerrainStylePreset CreatePreset(
            string name,
            string description,
            float terrainWidth,
            float terrainLength,
            float terrainHeight,
            int heightmapResolution,
            float baseHeight,
            float hillHeight,
            float mountainHeight,
            float continentalThreshold,
            float continentalMaskFreq,
            float warpStrength,
            float mountainFrequency = 0.0008f,
            float peakSharpness = 1.0f)
        {
            TerrainStylePreset preset = ScriptableObject.CreateInstance<TerrainStylePreset>();
            preset.styleName = name;
            preset.description = description;

            // Terrain size
            preset.terrainWidth = terrainWidth;
            preset.terrainLength = terrainLength;
            preset.terrainHeight = terrainHeight;
            preset.heightmapResolution = heightmapResolution;

            // Heights
            preset.baseHeight = baseHeight;
            preset.hillHeight = hillHeight;
            preset.mountainHeight = mountainHeight;

            // Noise parameters
            preset.continentalThreshold = continentalThreshold;
            preset.continentalMaskFrequency = continentalMaskFreq;
            preset.warpStrength = warpStrength;
            preset.mountainFrequency = mountainFrequency;
            preset.peakSharpness = peakSharpness;

            // Biome heights (normalized 0-1)
            preset.waterHeight = 0.05f;
            preset.grassHeight = 0.3f;
            preset.rockHeight = 0.6f;
            preset.snowHeight = 0.7f;

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
                "Alpine Mountains 8K",
                "8000x8000 terrain with tall, dramatic mountain ranges and vast plains.",
                8000f, 8000f, 1200f, 513,  // Size
                50f, 120f, 700f,  // Heights
                0.55f, 0.00005f, 200f,  // Noise
                0.0005f, 1.0f  // Mountain freq, sharpness
            );
        }

        private static void CreateRockyMountains()
        {
            CreatePreset(
                "Rolling Hills 8K",
                "8000x8000 terrain with gentle rolling hills and occasional peaks.",
                8000f, 8000f, 800f, 513,  // Size
                100f, 150f, 400f,  // Heights
                0.6f, 0.00005f, 150f,  // Noise
                0.0008f, 1.0f  // Mountain freq, sharpness
            );
        }

        private static void CreateAppalachianStyle()
        {
            CreatePreset(
                "Vast Plains 8K",
                "8000x8000 mostly flat terrain with distant mountains on horizon.",
                8000f, 8000f, 1000f, 513,  // Size
                80f, 100f, 600f,  // Heights
                0.65f, 0.00003f, 250f,  // Noise
                0.0005f, 1.0f  // Mountain freq, sharpness
            );
        }

        private static void CreatePlainsWithDistantMountains()
        {
            CreatePreset(
                "Small Test Terrain",
                "1000x1000 quick test terrain for rapid iteration.",
                1000f, 1000f, 600f, 257,  // Size
                50f, 100f, 300f,  // Heights
                0.5f, 0.0003f, 150f,  // Noise
                0.0008f, 1.3f  // Mountain freq, sharpness
            );
        }

        private static void CreateHimalayanStyle()
        {
            CreatePreset(
                "Extreme Mountains 8K",
                "8000x8000 with very tall peaks and dramatic elevation changes.",
                8000f, 8000f, 1500f, 513,  // Size
                100f, 200f, 1000f,  // Heights
                0.5f, 0.00005f, 220f,  // Noise
                0.0005f, 1.0f  // Mountain freq, sharpness
            );
        }

        private static void CreateFantasyRPG()
        {
            CreatePreset(
                "Balanced Fantasy 8K (Recommended)",
                "8000x8000 balanced terrain perfect for open-world RPG exploration.",
                8000f, 8000f, 1000f, 513,  // Size
                80f, 140f, 600f,  // Heights
                0.55f, 0.00005f, 200f,  // Noise
                0.0005f, 1.0f  // Mountain freq, sharpness
            );
        }
    }
}

