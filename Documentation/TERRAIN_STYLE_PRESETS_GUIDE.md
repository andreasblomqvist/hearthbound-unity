# Terrain Style Presets Guide

## Overview

The terrain style preset system allows you to quickly switch between different terrain generation styles (like "Alpine Mountains", "Rocky Mountains", etc.) without manually adjusting all the parameters in the Inspector.

## Quick Start

### Step 1: Create Presets

1. In Unity, go to **Hearthbound > Create Terrain Style Presets**
2. Click **"Create All Presets"** to create all available styles, or click individual buttons to create specific ones
3. Presets will be saved in `Assets/Resources/TerrainStyles/`

### Step 2: Apply a Preset

**Method 1: Using TerrainStyleApplier Component**
1. Select your Terrain GameObject
2. Add the `TerrainStyleApplier` component
3. Assign your `TerrainGenerator` component (or it will auto-find)
4. Assign a `TerrainStylePreset` asset
5. Right-click the component → **"Apply Preset"** (or check "Apply On Start" to auto-apply)

**Method 2: Using Code**
```csharp
TerrainStylePreset preset = Resources.Load<TerrainStylePreset>("TerrainStyles/AlpineMountains");
TerrainGenerator terrainGen = GetComponent<TerrainGenerator>();
preset.ApplyTo(terrainGen);
```

**Method 3: Direct in Inspector**
1. Select your Terrain GameObject
2. In the `TerrainGenerator` component, manually adjust values to match a preset
3. Or use the preset values as reference

## Available Presets

### Alpine Mountains
- **Base Height**: 50
- **Hill Height**: 120
- **Mountain Height**: 600
- **Snow Height**: 0.70
- **Continental Threshold**: 0.5
- **Warp Strength**: 150
- **Result**: Tall, sharp peaks with snow, valleys between ranges

### Rocky Mountains
- **Base Height**: 80
- **Hill Height**: 180
- **Mountain Height**: 550
- **Snow Height**: 0.75
- **Continental Threshold**: 0.45
- **Warp Strength**: 180
- **Result**: Extensive mountain ranges, high elevation overall

### Appalachian Style
- **Base Height**: 60
- **Hill Height**: 200
- **Mountain Height**: 400
- **Snow Height**: 0.85
- **Continental Threshold**: 0.6
- **Warp Strength**: 120
- **Result**: Older, more eroded mountains, less dramatic peaks

### Plains with Distant Mountains
- **Base Height**: 40
- **Hill Height**: 100
- **Mountain Height**: 650
- **Snow Height**: 0.68
- **Continental Threshold**: 0.65
- **Warp Strength**: 200
- **Result**: Mostly flat with dramatic mountain ranges on horizon

### Himalayan Style
- **Base Height**: 100
- **Hill Height**: 200
- **Mountain Height**: 800
- **Snow Height**: 0.60
- **Continental Threshold**: 0.55
- **Warp Strength**: 140
- **Result**: Very high elevation overall, extreme peaks, lots of snow

### Fantasy RPG (Recommended)
- **Base Height**: 60
- **Hill Height**: 140
- **Mountain Height**: 550
- **Snow Height**: 0.70
- **Continental Threshold**: 0.5
- **Warp Strength**: 150
- **Result**: Varied, playable terrain for open world RPG

## Creating Custom Presets

### Method 1: Using Unity Editor

1. Right-click in Project window → **Create > Hearthbound > Terrain Style Preset**
2. Name your preset (e.g., "MyCustomStyle")
3. Adjust all parameters in the Inspector:
   - Height Generation (Base, Hill, Mountain)
   - Height Curve
   - Texture Splatting Thresholds
   - Noise Parameters (Continental Threshold, Warp Strength, etc.)
4. Save the asset

### Method 2: Using Code

```csharp
TerrainStylePreset preset = ScriptableObject.CreateInstance<TerrainStylePreset>();
preset.styleName = "My Custom Style";
preset.description = "Description of my style";
preset.baseHeight = 60f;
preset.hillHeight = 140f;
preset.mountainHeight = 550f;
preset.snowHeight = 0.7f;
preset.continentalThreshold = 0.5f;
preset.warpStrength = 150f;
preset.mountainFrequency = 0.0008f;
preset.peakSharpness = 1.3f;

// Create exponential height curve
preset.heightCurve = new AnimationCurve(
    new Keyframe(0, 0),
    new Keyframe(0.5f, 0.3f),
    new Keyframe(1, 1)
);

// Save as asset
#if UNITY_EDITOR
UnityEditor.AssetDatabase.CreateAsset(preset, "Assets/Resources/TerrainStyles/MyCustomStyle.asset");
UnityEditor.AssetDatabase.SaveAssets();
#endif
```

## Preset Parameters Explained

### Height Generation
- **Base Height**: Elevation of plains/lowlands (30-100)
- **Hill Height**: Elevation of rolling hills (100-250)
- **Mountain Height**: Elevation of mountain peaks (400-800)

### Height Curve
Controls how heights are distributed across the terrain:
- **Linear**: Equal distribution (straight diagonal line)
- **Exponential**: More low areas, fewer high areas (curved upward) - Recommended

### Texture Splatting Thresholds
- **Water Height**: Below this = water/beach (0.0-0.2)
- **Grass Height**: Plains/grass biome (0.1-0.4)
- **Rock Height**: Mountains start (0.5-0.7)
- **Snow Height**: Snow on peaks (0.6-0.9)

### Noise Parameters (Advanced)
- **Continental Threshold**: Controls where mountains appear (0.3-0.7)
  - Lower = more mountains (0.4 = ~40% coverage)
  - Higher = fewer mountains (0.6 = ~20% coverage)
- **Warp Strength**: Controls mountain range elongation (100-250)
  - Lower = slightly elongated (100)
  - Higher = very long, narrow ranges (200)
- **Mountain Frequency**: Controls mountain chain detail (0.0005-0.002)
  - Lower = smoother ranges
  - Higher = more variation
- **Peak Sharpness**: Controls how sharp peaks are (1.0-2.0)
  - Lower = rounded peaks (1.0)
  - Higher = very sharp peaks (1.5)

## Workflow Example

1. **Create presets** using the editor window
2. **Apply a preset** to your TerrainGenerator
3. **Generate terrain** with a test seed
4. **Check console** for terrain distribution
5. **Adjust preset** if needed (or create a new one)
6. **Regenerate** until satisfied
7. **Save preset** for future use

## Tips

- **Start with "Fantasy RPG"** preset as a baseline
- **Test with different seeds** to see variety
- **Check terrain distribution** in console logs
- **Fine-tune individual parameters** if preset is close but not perfect
- **Create variations** of presets for different regions of your world
- **Save presets** you like for reuse

## Troubleshooting

### Preset doesn't apply
- Check that TerrainGenerator component is assigned
- Verify preset asset is not null
- Check console for error messages

### Terrain looks different than expected
- Regenerate terrain after applying preset (presets don't auto-regenerate)
- Check that all parameters were applied correctly
- Verify seed hasn't changed

### Can't find presets
- Check `Assets/Resources/TerrainStyles/` folder
- Use "Create Terrain Style Presets" menu to create them
- Refresh Unity's asset database

## Example: Switching Styles at Runtime

```csharp
public class TerrainStyleManager : MonoBehaviour
{
    public TerrainGenerator terrainGenerator;
    public TerrainStylePreset[] availableStyles;
    private int currentStyleIndex = 0;

    public void NextStyle()
    {
        currentStyleIndex = (currentStyleIndex + 1) % availableStyles.Length;
        availableStyles[currentStyleIndex].ApplyTo(terrainGenerator);
        terrainGenerator.GenerateTerrain(UnityEngine.Random.Range(0, 99999));
    }
}
```

This allows players or developers to cycle through different terrain styles and see them instantly!

---

**Happy terrain generating!** 🏔️

