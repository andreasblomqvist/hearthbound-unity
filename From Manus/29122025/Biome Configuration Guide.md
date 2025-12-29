# Biome Configuration Guide

## How to Set Up Your Biomes

With the fixed code, your biomes need properly configured ranges. Here are recommended settings:

### Example Biome Configurations

#### 1. Water (Ocean/Lake)
```
Height Range: 0.0 - 0.15
Temperature Range: 0.0 - 1.0 (water can exist at any temperature)
Humidity Range: 0.0 - 1.0 (water creates its own humidity)
Blend Strength: 5 (sharp boundaries with land)
Color: Blue (#2E5C8A)
```

#### 2. Beach/Sand
```
Height Range: 0.12 - 0.25
Temperature Range: 0.4 - 1.0 (warm areas)
Humidity Range: 0.0 - 0.4 (dry)
Blend Strength: 3 (moderate blending)
Color: Tan (#D4C4A8)
```

#### 3. Plains/Grassland
```
Height Range: 0.15 - 0.5
Temperature Range: 0.3 - 0.8 (temperate)
Humidity Range: 0.3 - 0.7 (moderate moisture)
Blend Strength: 2 (soft blending with other biomes)
Color: Green (#6B8E23)
```

#### 4. Forest
```
Height Range: 0.2 - 0.6
Temperature Range: 0.3 - 0.7 (temperate)
Humidity Range: 0.5 - 1.0 (high moisture)
Blend Strength: 3 (moderate blending)
Color: Dark Green (#2D5016)
```

#### 5. Desert
```
Height Range: 0.15 - 0.5
Temperature Range: 0.6 - 1.0 (hot)
Humidity Range: 0.0 - 0.3 (very dry)
Blend Strength: 3 (moderate blending)
Color: Sandy Yellow (#EDC9AF)
```

#### 6. Tundra
```
Height Range: 0.2 - 0.6
Temperature Range: 0.0 - 0.3 (cold)
Humidity Range: 0.0 - 0.5 (low to moderate)
Blend Strength: 3 (moderate blending)
Color: Pale Green (#A8C090)
```

#### 7. Mountain/Rock
```
Height Range: 0.5 - 0.9
Temperature Range: 0.0 - 1.0 (any temperature at high altitude)
Humidity Range: 0.0 - 1.0 (any humidity)
Blend Strength: 4 (fairly sharp)
Color: Gray (#808080)
```

#### 8. Snow/Ice
```
Height Range: 0.7 - 1.0
Temperature Range: 0.0 - 0.3 (very cold)
Humidity Range: 0.0 - 1.0 (any humidity)
Blend Strength: 4 (fairly sharp)
Color: White (#F0F0F0)
```

## Key Principles

### 1. Overlapping Ranges Create Blending
Biomes with overlapping ranges will blend together. For example:
- Plains (height 0.15-0.5) and Forest (height 0.2-0.6) overlap at height 0.2-0.5
- At height 0.3, both biomes can appear based on temperature and humidity

### 2. Temperature and Humidity Create Variety
At the same height, different temp/humidity combinations create different biomes:
- Height 0.3, Temp 0.7, Humidity 0.2 = Desert
- Height 0.3, Temp 0.5, Humidity 0.6 = Forest
- Height 0.3, Temp 0.2, Humidity 0.3 = Tundra

### 3. Blend Strength Controls Boundaries
- **Low (1-2)**: Soft, gradual transitions (good for similar biomes like Plains/Forest)
- **Medium (3-4)**: Moderate transitions (most biomes)
- **High (5-10)**: Sharp boundaries (good for Water/Land or Snow/Rock)

### 4. Height Ranges Should Overlap Slightly
Don't create strict height bands! Allow overlap:
- ❌ Bad: Water 0-0.1, Plains 0.1-0.5, Rock 0.5-1.0 (no overlap = hard bands)
- ✅ Good: Water 0-0.15, Plains 0.15-0.5, Rock 0.5-0.9 (overlap = natural transitions)

## How the Fixed System Works

### Temperature Generation
- **Latitude gradient**: Warmer at center (equator), cooler at edges (poles)
- **Noise variation**: Local temperature variations
- **Slight altitude effect**: High elevations are slightly cooler (max 20% reduction)

### Humidity Generation
- **Noise patterns**: Rainfall patterns across the map
- **Detail layer**: Additional variation for complex patterns
- **Slight height boost**: Low elevations are slightly more humid (max 15% boost)

### Biome Matching
1. For each biome, calculate match score (0-1) based on how well height, temp, humidity fit within ranges
2. Apply blend strength to sharpen or soften boundaries
3. Normalize weights so they sum to 1.0
4. Apply to terrain splatmap

## Testing Your Configuration

### Step 1: Start Simple
Create just 3 biomes first:
- Water (low height)
- Plains (mid height)
- Rock (high height)

Make sure these work before adding more complexity.

### Step 2: Add Temperature Variation
Add Desert (hot, dry) and Tundra (cold, dry) to see temperature effects.

### Step 3: Add Humidity Variation
Add Forest (humid) to see humidity effects.

### Step 4: Fine-Tune
Adjust blend strengths and ranges until you get the look you want.

## Debug Tips

### Enable Debug Logging
In your BiomeCollection asset:
1. Check "Debug Logging"
2. Generate terrain
3. Check Console for biome weight calculations at sample points

### Visualize Maps Separately
Create a debug script to visualize:
- Temperature map (red = hot, blue = cold)
- Humidity map (blue = wet, yellow = dry)
- Height map (white = high, black = low)

This helps you understand what values your biomes are actually seeing.

### Common Issues

**Issue**: Everything is one biome
**Fix**: Ranges are too narrow or blend strength too high. Widen ranges and lower blend strength.

**Issue**: Biomes change too abruptly
**Fix**: Increase range overlap and lower blend strength.

**Issue**: Expected biome doesn't appear
**Fix**: Check if temp/humidity generation actually produces values in that biome's range. Use debug logging.

**Issue**: Water appears on mountains
**Fix**: Make water's height range stricter (0.0-0.1) and increase blend strength.

## Code-First Workflow

You can create biome assets via code instead of the Unity Editor:

```csharp
// Example: Create a Forest biome via code
BiomeData forest = ScriptableObject.CreateInstance<BiomeData>();
forest.biomeName = "Forest";
forest.heightRange = new Vector2(0.2f, 0.6f);
forest.temperatureRange = new Vector2(0.3f, 0.7f);
forest.humidityRange = new Vector2(0.5f, 1.0f);
forest.blendStrength = 3f;
forest.terrainLayers = new TerrainLayerData[1];
forest.terrainLayers[0] = new TerrainLayerData();
forest.terrainLayers[0].color = new Color(0.18f, 0.31f, 0.09f); // Dark green

// Save as asset
#if UNITY_EDITOR
UnityEditor.AssetDatabase.CreateAsset(forest, "Assets/Biomes/Forest.asset");
UnityEditor.AssetDatabase.SaveAssets();
#endif
```

You can create an editor script to generate all biomes at once with proper configurations.
