# Fantasy Worlds: Forest Asset Setup Guide

This guide will help you set up the **Fantasy Worlds: Forest FREE** asset pack with your Hearthbound Unity project.

## Step 1: Import the Package

Since you're using **Built-in Rendering Pipeline (BiRP)**, you need to import the BiRP package:

1. In Unity Editor, open the **Project** window
2. Navigate to: `Assets/TriForge Assets/`
3. Find: `_BiRP Content - Fantasy Worlds - Old Forest DEMO.unitypackage`
4. **Double-click** the `.unitypackage` file
5. Unity will show an import dialog - click **"Import All"**
6. Wait for the import to complete (this may take a minute or two)

> **Note:** If you're using URP instead, use `_URP Content - Fantasy Worlds - Old Forest DEMO.unitypackage`

## Step 2: Find Your Vegetation Assets

After importing, use the **Vegetation Asset Finder** tool to automatically scan and categorize all your vegetation assets:

1. In Unity Editor menu bar: **Hearthbound > Find Vegetation Assets**
2. The window will open and automatically scan for:
   - **Trees** (anything with "tree" in the name/path)
   - **Bushes/Plants** (anything with "bush", "plant", "grass", "vegetation")
   - **Rocks** (anything with "rock" or "stone")
   - **Other assets** (everything else)

3. Click **"Scan Assets"** to refresh the list if needed

## Step 3: Create Prefabs

The tool allows you to create prefab copies directly:

1. For each tree/bush/rock you want to use:
   - Click the **"Create Prefab"** button next to the asset
   - This will copy it to the appropriate folder:
     - Trees → `Assets/Prefabs/Trees/`
     - Bushes/Rocks → `Assets/Prefabs/Props/`

2. **Or manually:**
   - Drag assets from `TriForge Assets` folders into your scene
   - Position and configure them as needed
   - Drag from Scene Hierarchy into `Assets/Prefabs/Trees/` or `Assets/Prefabs/Props/`

## Step 4: Assign to ForestGenerator

1. Select the `[WORLD]` GameObject in your scene
2. Find the **ForestGenerator** component in the Inspector
3. Expand the prefab arrays:
   - **Tree Prefabs**: Set the size (number of tree types), then drag your tree prefabs into the slots
   - **Bush Prefabs**: Set the size, drag bush/plant prefabs into slots
   - **Rock Prefabs**: Set the size, drag rock prefabs into slots

## Step 5: Test and Adjust

1. Generate your world (press Play or use WorldSeedManager)
2. Check if forests appear with your new assets
3. Adjust settings in ForestGenerator:
   - **Tree Density**: 0.3-0.5 is a good starting point
   - **Bush Density**: 0.5-0.7 for lush forests
   - **Rock Density**: 0.1-0.2 for natural scatter
   - **Scale Variation**: 0.2-0.4 for natural size differences
   - **Number of Forests**: Adjust based on terrain size

## Tips

### Performance Optimization

- **LOD Groups**: Some assets may already have LOD groups. Keep them!
- **Polygon Count**: Fantasy Worlds assets are optimized, but if you notice performance issues:
  - Reduce `Tree Density` or `Number of Forests`
  - Use fewer different tree types
  - Consider using terrain detail grass instead of bush prefabs for small vegetation

### Asset Organization

- Keep the original assets in `TriForge Assets` folder (don't delete them)
- Your prefab copies in `Assets/Prefabs/` are what the ForestGenerator uses
- You can create multiple variations (different scales) of the same asset

### Pivot Points

- Make sure tree prefabs have their pivot at the base (ground level)
- If trees are floating or clipping into terrain, check the pivot point
- You may need to adjust prefabs or use a pivot editor tool

### Wind Animation

The asset pack includes a **TriForge Wind Controller** prefab (`Assets/TriForge Assets/Fantasy Worlds - DEMO Common Files/TriForge Wind Controller.prefab`). You can add this to your scene for wind animation on vegetation.

## Troubleshooting

**"No tree prefabs assigned!" warning:**
- Make sure you've assigned at least one prefab to the Tree Prefabs array in ForestGenerator

**Trees don't appear:**
- Check Console for errors
- Verify prefabs are assigned (arrays are not empty)
- Check that `numberOfForests` is > 0
- Verify terrain has been generated first

**Trees floating or clipping:**
- Check pivot points are at base of trees
- Adjust `minHeightForForest` and `maxHeightForForest` if trees are in wrong elevation zones

**Performance issues:**
- Reduce density values
- Reduce number of different prefab types
- Check polygon counts in asset inspector
- Use LOD groups if available

## Next Steps

Once your basic vegetation is working:

1. **Add Variety**: Use multiple tree types per forest
2. **Biome-Specific**: Modify ForestGenerator to use different vegetation per biome
3. **Grass Details**: Set up Unity's terrain detail system for grass (see VEGETATION_ASSETS_GUIDE.md)
4. **Optimization**: Add object pooling for dynamic placement
5. **LOD System**: Ensure all trees have LOD groups for better performance

---

**Happy forest building! 🌲🌿**

