# Vegetation Assets Guide

This guide will help you find and integrate trees, grass, bushes, and other vegetation assets to bring your procedural world to life.

## Current Status

Your project has:
- ✅ **ForestGenerator.cs** - Ready to place trees, bushes, and rocks via prefabs
- ✅ **Folder structure** - `Assets/Prefabs/Trees/`, `Assets/Prefabs/Props/` folders exist (currently empty)
- ✅ **Terrain detail system** - Placeholder in TerrainGenerator.cs for grass (lines 1350-1362)
- ❌ **No prefabs yet** - All prefab arrays are empty and need to be populated

## What You Need

Your `ForestGenerator` component requires three types of prefabs:

1. **Tree Prefabs** (`treePrefabs[]`) - For forest placement
2. **Bush Prefabs** (`bushPrefabs[]`) - For undergrowth
3. **Rock Prefabs** (`rockPrefabs[]`) - For natural stone features

Additionally, you can add:
4. **Grass** - Via Unity's Terrain Detail System (billboard grass or mesh grass)

## Recommended Free Asset Sources

### Free Unity Assets (Itch.io - Best for Free Content)

1. **Free Forest Themed Assets by Lithuanian_Dude**
   - **Link**: https://lithuanian-dude.itch.io/free-forest-themed-assets
   - **Includes**: Tree and bush models, grass detail textures, ground materials, rock models
   - **Format**: Unity package or FBX files
   - **Perfect for**: Getting started quickly with a complete set

2. **FREE Remake Stylized Meadow and Forests by EmaceArt**
   - **Link**: https://emaceart.itch.io/free-low-poly-meadows
   - **Includes**: 30 base meshes, ~250 ready-to-use prefabs (trees, rocks, fences)
   - **Format**: Unity package
   - **Perfect for**: Low-poly stylized look

3. **3D Trees Pack by Yoo Game Art**
   - **Link**: https://yoogameart.itch.io/3d-trees-pack
   - **Includes**: 10 low-poly tree models in various sizes
   - **Perfect for**: Performance-optimized stylized trees

4. **Grasses 3D by ToffeeCraft**
   - **Link**: https://toffeecraft.itch.io/grass-3d
   - **Includes**: 10 different grass 3D models (FBX, GLB, OBJ)
   - **Perfect for**: Converting to Unity terrain detail grass

5. **Stylized Tree by Kostas3D**
   - **Link**: https://kostas3d.itch.io/stylized-tree-free-3d-model-game-asset
   - **Includes**: Beautiful low-poly stylized tree
   - **Perfect for**: Single high-quality tree asset

### Unity Asset Store Free Assets

1. **Nature Starter Kit 2** (Unity Asset Store)
   - Search for "Nature Starter Kit 2" in Unity Package Manager
   - Includes: Trees, grass textures, and basic terrain assets
   - **Perfect for**: Quick testing and prototyping

2. **Free Trees Pack** (various authors on Asset Store)
   - Search "free trees" in Unity Asset Store
   - Many free tree packs available

3. **Polygon Nature Pack** (if available as free/package)
   - Check Unity Package Manager > Visual Effects
   - Stylized low-poly nature assets

### Additional Resources (Paid but Affordable)

- **Vegetation 2 - Low-Poly 3D Models Pack by ITHappy Studios**
  - 504 low-poly vegetation assets
  - Very comprehensive for a reasonable price
  - Link: https://ithappystudios.com/environment/vegetation-2/

## How to Import and Set Up Assets

### Step 1: Download and Import Assets

1. **Download from Itch.io:**
   - Create a free account if needed
   - Download the Unity package (.unitypackage) if available
   - Or download FBX/OBJ files

2. **Import into Unity:**
   - For `.unitypackage`: Double-click it, Unity will open and show import dialog
   - For individual models: Drag FBX/OBJ files into `Assets/Models/Environment/`
   - Unity will auto-import and create materials/textures

### Step 2: Create Prefabs

1. **Create Tree Prefabs:**
   - Drag imported tree models from Project window into Scene
   - Position at origin (0, 0, 0) - this will be the pivot point
   - Adjust scale if needed (typical tree: 2-10 units tall)
   - Add components if needed:
     - `LOD Group` (optional, for performance with multiple detail levels)
     - `Mesh Collider` (optional, for physics)
   - Drag from Scene Hierarchy into `Assets/Prefabs/Trees/` folder
   - Delete from scene (prefab is saved)

2. **Create Bush/Rock Prefabs:**
   - Repeat same process in `Assets/Prefabs/Props/` folder

3. **Tips:**
   - Name prefabs clearly: `Tree_Pine_01`, `Tree_Oak_01`, `Bush_Small_01`, `Rock_Large_01`
   - Create multiple variations of same model with different scales
   - Keep pivot at base (feet) so placement on terrain works correctly

### Step 3: Assign to ForestGenerator

1. Select the `[WORLD]` GameObject in your scene
2. Find the `ForestGenerator` component in Inspector
3. Expand the prefab arrays:
   - **Tree Prefabs**: Set size, drag tree prefabs into slots
   - **Bush Prefabs**: Set size, drag bush prefabs into slots
   - **Rock Prefabs**: Set size, drag rock prefabs into slots
4. Adjust generation settings:
   - `Tree Density`: 0.3-0.5 is good starting point
   - `Bush Density`: 0.5-0.7 for lush forests
   - `Rock Density`: 0.1-0.2 for natural scatter

### Step 4: Set Up Grass (Terrain Detail System)

Grass is handled differently - it uses Unity's Terrain Detail Prototypes instead of prefabs.

1. **Prepare Grass Textures:**
   - Download grass texture(s) - look for billboard grass textures (2D cross-section)
   - Import into `Assets/Textures/` (create folder if needed)
   - Recommended: 256x256 or 512x512 PNG with transparency

2. **Set Up Detail Prototypes:**
   - The `TerrainGenerator.cs` has placeholder code for this (line ~1359)
   - You'll need to modify `ApplyTerrainDetails()` method to:
     - Create `DetailPrototype` objects
     - Assign grass textures
     - Set detail density maps based on biome/height/slope

3. **Quick Alternative for Testing:**
   - Select your Terrain object in scene
   - Inspector > Terrain component > Paint Details tab
   - Click "Edit Details..." > "Add Grass Texture"
   - Assign grass texture
   - Paint manually to test (or implement in code)

## Asset Requirements & Recommendations

### Tree Assets Should Have:
- ✅ Low to medium polygon count (500-2000 triangles per tree is good)
- ✅ Single mesh or LOD group (Level of Detail)
- ✅ Materials/textures included
- ✅ Pivot point at base (ground level)
- ✅ Reasonable scale (2-15 units tall is typical)

### Bush Assets Should Have:
- ✅ Even lower polygon count (100-500 triangles)
- ✅ Compact design (fills space efficiently)
- ✅ Pivot at base

### Rock Assets Should Have:
- ✅ Varied shapes and sizes
- ✅ Low polygon count (50-300 triangles)
- ✅ Natural, irregular shapes

### Grass Textures Should Have:
- ✅ Alpha channel (transparency) for natural edges
- ✅ 256x256 to 512x512 resolution
- ✅ Billboard/cross-section style (two crossed quads)

## Testing Your Assets

1. **Test Individual Prefabs:**
   - Drag prefab into scene
   - Check scale, rotation, materials look correct
   - Verify pivot is at base

2. **Test Forest Generation:**
   - Assign prefabs to ForestGenerator
   - Set forest count to 1-2 for testing
   - Generate world and check placement looks natural
   - Adjust density, scale variation, rotation settings

3. **Performance Check:**
   - Generate full world with forests
   - Check FPS in Game view stats
   - If performance drops:
     - Reduce tree/bush density
     - Use LOD groups on trees
     - Reduce number of forests
     - Consider using terrain detail grass instead of bush prefabs

## Quick Start Checklist

- [ ] Download at least one free asset pack (recommend "Free Forest Themed Assets")
- [ ] Import .unitypackage or FBX files into Unity
- [ ] Create at least 3-5 tree prefabs in `Assets/Prefabs/Trees/`
- [ ] Create at least 2-3 bush prefabs in `Assets/Prefabs/Props/`
- [ ] Create at least 2-3 rock prefabs in `Assets/Prefabs/Props/`
- [ ] Assign prefabs to ForestGenerator component arrays
- [ ] Test generation and adjust density settings
- [ ] (Optional) Set up terrain detail grass system

## Integration with Existing Code

Your `ForestGenerator.cs` is already set up to:
- ✅ Accept prefab arrays (trees, bushes, rocks)
- ✅ Place them procedurally based on noise
- ✅ Respect terrain height and slope
- ✅ Apply random rotation and scale variation
- ✅ Snap to terrain surface automatically

You just need to populate the prefab arrays!

## Next Steps After Adding Assets

Once you have basic vegetation working:

1. **Add Biome-Specific Vegetation:**
   - Modify `ForestGenerator` to use different tree types per biome
   - Check biome at placement position
   - Select appropriate prefab array based on biome

2. **Optimize Performance:**
   - Add LOD groups to trees
   - Use object pooling for dynamic placement
   - Implement culling for distant objects

3. **Add Variety:**
   - Multiple tree species per forest
   - Seasonal variations (if adding seasons system)
   - Dead trees, fallen logs, stumps

4. **Enhance Grass System:**
   - Multiple grass types (tall, short, flowers)
   - Biome-specific grass textures
   - Procedural grass placement via code

## Troubleshooting

**Prefabs don't appear:**
- Check prefabs are assigned in ForestGenerator inspector
- Verify prefab arrays are not empty
- Check Console for errors during generation

**Trees floating or clipping:**
- Verify pivot point is at base of tree model
- Check terrain height snapping is working
- Adjust `minHeightForForest` / `maxHeightForForest` if needed

**Too many/too few trees:**
- Adjust `treeDensity` (0.0-1.0 range)
- Adjust `numberOfForests`
- Adjust `forestRadius`

**Performance issues:**
- Reduce polygon count of assets
- Lower `treeDensity` or `numberOfForests`
- Use LOD groups
- Consider using terrain detail grass instead of prefabs for small vegetation

---

**Happy world building! 🌲🌿🌳**

