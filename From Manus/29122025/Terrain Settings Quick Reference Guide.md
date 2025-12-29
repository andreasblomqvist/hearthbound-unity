# Terrain Settings Quick Reference Guide

## Overview

This guide helps you adjust terrain generation parameters to get the exact look you want for your open world RPG.

## Key Concepts

### Continental Mask
Controls **where** mountain ranges appear (not everywhere, just in certain regions like real-world mountain chains).

- **Threshold 0.4**: Mountains cover ~40% of terrain (lots of mountains)
- **Threshold 0.5**: Mountains cover ~30% of terrain (balanced)
- **Threshold 0.6**: Mountains cover ~20% of terrain (mostly plains/hills)

### Domain Warping
Makes mountains form in **elongated ranges** instead of circular blobs.

- **Warp Strength 100**: Slightly elongated
- **Warp Strength 150**: Natural-looking ranges (recommended)
- **Warp Strength 200**: Very long, narrow ranges

### Ridged Noise
Creates **sharp peaks** instead of rounded hills.

- Applied to mountain ranges only
- Formula: `1.0 - abs(noise * 2 - 1)`
- Makes peaks look like real mountains (Alps, Rockies)

## TerrainGenerator Inspector Settings

### Height Settings

**Base Height** (Lowland elevation)
- **30-50**: Very flat plains
- **50-80**: Gentle rolling plains (recommended)
- **80-100**: Elevated plains

**Hill Height** (Rolling hills elevation)
- **100-150**: Gentle hills (recommended)
- **150-200**: Prominent hills
- **200-250**: Very tall hills

**Mountain Height** (Peak elevation)
- **400-500**: Moderate mountains
- **500-600**: Tall mountains (recommended)
- **600-800**: Very tall mountains
- **800+**: Extreme peaks (may look unrealistic)

### Recommended Starting Values
```
Base Height: 60
Hill Height: 140
Mountain Height: 550
```

### Noise Frequencies

**Continental** (0.0003)
- Controls large-scale terrain zones
- Lower = larger mountain ranges
- Higher = smaller, more scattered mountains

**Mountain Range** (0.0008)
- Controls mountain chain detail
- Lower = smoother ranges
- Higher = more variation in ranges

**Hills** (0.003)
- Controls rolling hill size
- Lower = larger, smoother hills
- Higher = smaller, bumpier hills

**Detail** (0.01)
- Controls small bumps and texture
- Lower = smoother terrain
- Higher = more detailed/rough

## Biome Height Thresholds

These control what textures/biomes appear at different elevations:

```
Water:  0.00 - 0.10  (10% of height range)
Beach:  0.10 - 0.15  (5% of height range)
Grass:  0.15 - 0.35  (20% of height range)
Forest: 0.25 - 0.55  (30% of height range, overlaps grass)
Rock:   0.60 - 0.75  (15% of height range)
Snow:   0.70 - 1.00  (30% of height range, overlaps rock)
```

### Adjusting Snow Coverage

**More snow on peaks:**
- Lower Snow Height threshold to 0.65 or 0.60

**Less snow (only highest peaks):**
- Raise Snow Height threshold to 0.75 or 0.80

**No snow:**
- Set Snow Height threshold to 1.0

## Common Terrain Styles

### Alpine Mountains (Like Swiss Alps)
```
Base Height: 50
Hill Height: 120
Mountain Height: 600
Continental Threshold: 0.5
Warp Strength: 150
Snow Height: 0.70
```
Result: Tall, sharp peaks with snow, valleys between ranges

### Rocky Mountains (Like Colorado)
```
Base Height: 80
Hill Height: 180
Mountain Height: 550
Continental Threshold: 0.45
Warp Strength: 180
Snow Height: 0.75
```
Result: Extensive mountain ranges, high elevation overall

### Appalachian Style (Rolling Mountains)
```
Base Height: 60
Hill Height: 200
Mountain Height: 400
Continental Threshold: 0.6
Warp Strength: 120
Snow Height: 0.85
```
Result: Older, more eroded mountains, less dramatic peaks

### Plains with Distant Mountains
```
Base Height: 40
Hill Height: 100
Mountain Height: 650
Continental Threshold: 0.65
Warp Strength: 200
Snow Height: 0.68
```
Result: Mostly flat with dramatic mountain ranges on horizon

### Himalayan Style (Extreme Peaks)
```
Base Height: 100
Hill Height: 200
Mountain Height: 800
Continental Threshold: 0.55
Warp Strength: 140
Snow Height: 0.60
```
Result: Very high elevation overall, extreme peaks, lots of snow

## Terrain Distribution Goals

For a balanced open world RPG:

**Ideal Distribution:**
- **Plains**: 25-35% (settlements, farms, travel)
- **Hills**: 35-45% (forests, villages, variety)
- **Mountains**: 20-30% (challenges, dungeons, landmarks)
- **Peaks with Snow**: 5-10% (rare, special locations)

**Check Your Distribution:**
After generating terrain, check Console logs:
```
Terrain distribution: Plains=32%, Hills=41%, Mountains=27%, Peaks=8%
```

If distribution is off:
- **Too flat**: Increase Mountain Height or lower Continental Threshold
- **Too mountainous**: Decrease Mountain Height or raise Continental Threshold
- **No variety**: Adjust Hill Height to be between Base and Mountain

## Height Curve Adjustment

The Height Curve in TerrainGenerator controls how heights are distributed.

**Linear Curve** (straight diagonal line)
- Equal distribution of all heights
- Result: Gradual slopes, no dramatic features

**Exponential Curve** (curved upward)
- More low areas, fewer high areas
- Sharp peaks when they do appear
- Recommended for realistic terrain

**How to adjust in Unity:**
1. Select Terrain GameObject
2. Find TerrainGenerator component
3. Click Height Curve
4. Adjust curve shape:
   - Start: (0, 0)
   - Middle: (0.5, 0.3) - keeps lowlands low
   - End: (1, 1)
   - This creates exponential growth

## Troubleshooting

### Problem: No snow on peaks
**Solution:** Lower Snow Height threshold to 0.65 or 0.60

### Problem: Mountains are too small
**Solution:** Increase Mountain Height to 600-800

### Problem: Mountains everywhere (no plains)
**Solution:** Increase Continental Threshold to 0.6 or 0.65

### Problem: Terrain is too flat
**Solution:** 
- Increase Mountain Height
- Decrease Continental Threshold
- Check that GetMountainRangeNoise is being used correctly

### Problem: Mountains are circular blobs, not ranges
**Solution:** 
- Increase Warp Strength to 150-200
- Ensure domain warping is asymmetric (more in one direction)

### Problem: No distinct biomes
**Solution:**
- Check biome height thresholds
- Ensure temperature/moisture noise is working
- Verify BiomeCollection has correct biome definitions

### Problem: Terrain looks too uniform
**Solution:**
- Add more noise octaves for detail
- Increase detail frequency
- Add subtle warping to base terrain

## Performance Considerations

### For Large Terrains (2000x2000+)

**Heightmap Resolution:**
- 513: Fast generation, lower detail
- 1025: Balanced (recommended)
- 2049: High detail, slower generation

**Detail Layers:**
- Reduce detail density for better performance
- Use LOD for grass/rocks
- Only generate details near player

### For Multiple Terrains (Chunked World)

- Generate terrains on separate thread
- Cache terrain data
- Use same seed + offset for consistency
- Stream terrains as player moves

## Advanced Techniques

### Island Generation
Add radial falloff to create islands:
```csharp
float distanceFromCenter = Vector2.Distance(pos, center);
float falloff = Mathf.Clamp01(1 - distanceFromCenter / radius);
height *= falloff;
```

### River Valleys
Use inverted ridged noise for river paths:
```csharp
float riverNoise = GetRidgeNoise(x, z, seed, 0.002f);
riverNoise = 1.0f - riverNoise; // Invert for valleys
height -= riverNoise * riverDepth;
```

### Plateaus
Use stepped noise for flat-topped mountains:
```csharp
float plateauNoise = Mathf.Floor(noise * steps) / steps;
```

### Volcanic Peaks
Use cone-shaped falloff from peak points:
```csharp
float distanceToPeak = Vector2.Distance(pos, peakPos);
float coneHeight = Mathf.Max(0, peakHeight - distanceToPeak * slope);
```

## Testing Checklist

After adjusting settings, verify:

- [ ] Mountain ranges are visible and elongated
- [ ] Snow appears on highest peaks (not everywhere)
- [ ] Plains exist for settlements and travel
- [ ] Hills provide transition between plains and mountains
- [ ] Terrain distribution is balanced (check console logs)
- [ ] Different seeds produce different but similar terrain
- [ ] Biomes appear in logical locations (snow on peaks, grass in valleys)
- [ ] Terrain looks natural (no obvious patterns or artifacts)

## Recommended Workflow

1. **Start with default settings** (Base: 60, Hill: 140, Mountain: 550)
2. **Generate terrain** and check distribution
3. **Adjust Mountain Height** if peaks are too small/large
4. **Adjust Continental Threshold** if too many/few mountains
5. **Adjust Snow Height** for desired snow coverage
6. **Fine-tune Warp Strength** for range shape
7. **Test with 3-5 different seeds** to ensure variety
8. **Adjust biome thresholds** for logical biome placement
9. **Add creatures/settlements** to verify terrain is playable
10. **Iterate** based on gameplay testing

## Quick Settings for Different World Types

### Fantasy RPG (Varied, Playable)
```
Base: 60, Hill: 140, Mountain: 550
Continental: 0.5, Warp: 150, Snow: 0.70
Distribution: 30% plains, 40% hills, 30% mountains
```

### Survival Game (Challenging Terrain)
```
Base: 40, Hill: 160, Mountain: 600
Continental: 0.45, Warp: 170, Snow: 0.65
Distribution: 25% plains, 35% hills, 40% mountains
```

### Peaceful Exploration (Gentle Terrain)
```
Base: 70, Hill: 120, Mountain: 450
Continental: 0.6, Warp: 130, Snow: 0.80
Distribution: 40% plains, 45% hills, 15% mountains
```

### Epic Adventure (Dramatic Landscapes)
```
Base: 50, Hill: 180, Mountain: 700
Continental: 0.5, Warp: 180, Snow: 0.65
Distribution: 25% plains, 35% hills, 40% mountains
```

---

Use this guide as a reference while adjusting your terrain in Unity. Experiment with different values to find the perfect look for your game!
