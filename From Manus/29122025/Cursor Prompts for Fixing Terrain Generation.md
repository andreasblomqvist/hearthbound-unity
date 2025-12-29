# Cursor Prompts for Fixing Terrain Generation

Copy and paste these prompts into Cursor one at a time. The Unity MCP will help you make the changes.

---

## Prompt 1: Add Mountain Range Noise Function

```
In Assets/Scripts/World/NoiseGenerator.cs, add a new function called GetMountainRangeNoise that creates elongated mountain ranges instead of circular blobs.

The function should:
1. Use domain warping to elongate mountains into ranges
2. Use ridged noise for sharp peaks
3. Take parameters: x, y, seed, frequency (default 0.0008), warpStrength (default 150)
4. Warp coordinates in one direction more than the other to create linear ranges
5. Apply ridged noise: 1.0 - abs(noise * 2 - 1) for sharp peaks
6. Return value 0-1

Add this function after the GetTerrainHeight function.
```

---

## Prompt 2: Add Continental Mask Function

```
In Assets/Scripts/World/NoiseGenerator.cs, add a new function called GetContinentalMask that defines where mountain ranges should appear.

The function should:
1. Use very low frequency noise (0.0003) for large-scale features
2. Take parameters: x, y, seed
3. Return value 0-1 where high values = mountain regions, low values = plains
4. Use fractal noise with 2 octaves for smooth transitions

Add this function after GetMountainRangeNoise.
```

---

## Prompt 3: Rewrite GetTerrainHeight Function

```
In Assets/Scripts/World/NoiseGenerator.cs, completely rewrite the GetTerrainHeight function to create realistic mountain ranges.

The new function should:
1. Generate a continental mask using GetContinentalMask (very low frequency)
2. Generate mountain ranges using GetMountainRangeNoise where continental mask > 0.5
3. Generate rolling hills using GetFractalNoise with medium frequency (0.003)
4. Generate base plains using GetNoise2D with low frequency (0.001)
5. Combine layers:
   - Base plains everywhere
   - Hills added where continental mask > 0.3
   - Mountains added where continental mask > 0.5
   - Mountains should be MUCH taller than hills
6. Use these height multipliers:
   - Plains: baseHeight * 0.3
   - Hills: hillHeight * 0.5
   - Mountains: mountainHeight * 1.5 (make mountains TALL)
7. Apply a power curve (exponent 1.3) to make peaks sharper
8. Return final height value

Replace the entire existing GetTerrainHeight function with this new implementation.
```

---

## Prompt 4: Update TerrainGenerator Settings

```
In the Unity scene, select the Terrain GameObject and update the TerrainGenerator component settings:

Set these values in the Inspector:
- Base Height: 50
- Hill Height: 150
- Mountain Height: 500 (increased from 800 for better proportions)
- Height Curve: Make it steeper at the high end (adjust the curve to be exponential)

Then click "Generate Terrain" to regenerate with the new algorithm.
```

---

## Prompt 5: Fix Snow Height Threshold

```
In Assets/Scripts/World/TerrainGenerator.cs, find the GenerateSplatmap function.

Update the snow height threshold:
- Change Snow Height from 0.8 to 0.7
- This will make snow appear on more mountain peaks

Also ensure the texture splatting uses the correct height thresholds:
- Water: 0-0.1
- Grass: 0.1-0.3
- Rock: 0.6-0.7
- Snow: 0.7-1.0

Save the file.
```

---

## Prompt 6: Add Mountain Range Visualization

```
In Assets/Scripts/World/TerrainGenerator.cs, add debug logging to the GenerateHeightmap function.

After generating the heightmap, add code to:
1. Count how many heightmap pixels are in each range:
   - Plains: height < 0.3
   - Hills: height 0.3-0.6
   - Mountains: height > 0.6
   - Peaks: height > 0.8
2. Log the percentages to console
3. This helps verify the terrain distribution

Add this logging right before the "Heightmap generated" debug log.
```

---

## Prompt 7: Regenerate Terrain

```
In Unity, select the Terrain GameObject in the hierarchy.

In the TerrainGenerator component Inspector:
1. Change the seed to a new value (try 54321)
2. Click the "Generate Terrain" button
3. Check the Console for the terrain distribution percentages
4. Verify you see distinct mountain ranges in the Scene view

If mountains are still too small, increase Mountain Height to 600 or 700.
```

---

## Testing Prompts (Use After Implementation)

### Test Prompt 1: Verify Mountain Ranges
```
In Unity Scene view, zoom out to see the entire terrain. 

Verify that you see:
- Clear mountain ranges (elongated, not circular)
- Snow on the highest peaks
- Valleys between mountain ranges
- Plains in lowland areas
- Smooth transitions from plains → hills → mountains

Take a screenshot and show me if it looks correct.
```

### Test Prompt 2: Adjust Mountain Height
```
If mountains are too tall or too short, adjust the Mountain Height parameter in the TerrainGenerator Inspector:
- Too flat: Increase to 600-700
- Too spiky: Decrease to 400-450

Regenerate terrain after each change until it looks right.
```

### Test Prompt 3: Adjust Mountain Coverage
```
If there are too many or too few mountains, modify the GetTerrainHeight function:
- Too many mountains: Change continental mask threshold from 0.5 to 0.6
- Too few mountains: Change continental mask threshold from 0.5 to 0.4

This controls what percentage of the terrain has mountain ranges.
```

---

## Expected Results

After completing all prompts, you should see:

✅ **Distinct mountain ranges** - elongated chains, not random bumps  
✅ **Snow-capped peaks** - white snow on the highest mountains  
✅ **Varied terrain** - plains, hills, and mountains in different areas  
✅ **Natural transitions** - smooth gradients between terrain types  
✅ **Realistic proportions** - ~30% plains, ~40% hills, ~30% mountains  

Console output should show something like:
```
🗻 Generating terrain with seed: 54321
  Generating heightmap...
  Terrain distribution: Plains=32%, Hills=41%, Mountains=27%, Peaks=8%
  ✅ Heightmap generated
```

---

## Troubleshooting Prompts

### If terrain is still too flat:
```
Increase the Mountain Height parameter to 700 or 800 in the TerrainGenerator Inspector, then regenerate.
```

### If there are no mountain ranges (just random bumps):
```
Check that GetMountainRangeNoise is using domain warping with warpStrength=150. The warp should be asymmetric (more in X than Z) to create elongated ranges.
```

### If mountains are everywhere:
```
In GetTerrainHeight, increase the continental mask threshold from 0.5 to 0.6. This makes mountains appear in fewer areas.
```

### If there's no snow on peaks:
```
Lower the Snow Height threshold in GenerateSplatmap from 0.7 to 0.65, then regenerate the splatmap.
```

---

## Quick Reference: Key Parameters

**For more mountains:**
- Continental mask threshold: 0.4 (more area)
- Mountain Height: 600-800 (taller)

**For fewer mountains:**
- Continental mask threshold: 0.6 (less area)
- Mountain Height: 400-500 (shorter)

**For sharper peaks:**
- Height curve exponent: 1.5 (sharper)
- Ridge sharpness in GetMountainRangeNoise: increase

**For more snow:**
- Snow Height threshold: 0.65 (lower = more snow)

**For realistic ranges:**
- Domain warp strength: 150-200 (more elongated)
- Mountain frequency: 0.0008 (larger features)

---

## Final Prompt: Verify Everything Works

```
Generate 3 different terrains with seeds 12345, 54321, and 99999.

For each terrain, verify:
1. Mountain ranges are visible and elongated
2. Snow appears on peaks
3. There are distinct plains, hills, and mountain areas
4. The terrain looks natural and varied

Show me screenshots of all three terrains so I can confirm the fix worked.
```

---

That's it! Use these prompts in order with Cursor + Unity MCP to fix your terrain generation.
