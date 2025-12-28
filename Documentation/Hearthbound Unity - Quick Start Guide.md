# Hearthbound Unity - Quick Start Guide

## What You're Getting

A complete Unity project for **seed-based procedural world generation** with:

- **Terrain Generation** - Mountains, valleys, and plains from a single seed
- **Village System** - Procedurally placed villages with buildings
- **Forest System** - Trees, bushes, and rocks scattered naturally
- **AI-Powered NPCs** - Claude API integration for dynamic dialogue
- **Code-First Workflow** - Designed for Cursor IDE with Unity MCP
- **Reproducible Worlds** - Same seed = same world every time

## Files Included

- **`HearthboundUnity.zip`** - The complete Unity project
- **`godot_to_unity_conversion.md`** - Detailed conversion analysis
- **`hearthbound_analysis.md`** - Original Godot project analysis
- **`unity_mcp_findings.md`** - Unity MCP integration research

## Installation Steps

### 1. Extract the Project

Unzip `HearthboundUnity.zip` to your desired location.

### 2. Open in Unity

1. Open **Unity Hub**
2. Click **Add** and navigate to the extracted `HearthboundUnity` folder
3. Open the project with **Unity 2022.3 LTS** or newer

### 3. Follow the Setup Guide

Inside the project, open `Documentation/SETUP.md` for detailed setup instructions.

## Key Features

### Seed-Based Generation

Every aspect of world generation is controlled by a single integer seed. This means:

- **Reproducibility**: Same seed always generates the same world
- **Shareability**: Players can share seeds to explore identical worlds
- **Testing**: Easy to test specific world configurations
- **Debugging**: Consistent results make debugging easier

### Code-First Workflow

The project is designed to work with **Cursor IDE** and **Unity MCP**:

- Write all code in Cursor
- Control Unity Editor from Cursor
- AI-assisted script generation
- Minimal manual editor work

See `Documentation/UNITY_MCP_GUIDE.md` for setup instructions.

### Procedural Systems

#### Terrain Generator
- Noise-based heightmap generation
- Automatic texture splatting (grass, rock, snow)
- Configurable size, resolution, and height
- Biome-aware terrain features

#### Village Builder
- Seed-based village placement
- Multiple building types (houses, farms, taverns)
- Automatic terrain snapping
- Slope and height validation
- Prop placement (carts, fences, etc.)

#### Forest Generator
- Natural tree clustering using noise
- Multiple tree and bush types
- Rock placement
- Density control
- Terrain-aware placement

### AI Integration

The `AIManager` provides Claude API integration for NPC dialogue:

```csharp
AIManager.Instance.NPCDialogue(
    "Guard", 
    "Suspicious and brief", 
    "What's happening here?", 
    (response) => {
        Debug.Log("NPC Response: " + response);
    }
);
```

## Project Structure

```
HearthboundUnity/
├── Assets/
│   ├── Scripts/
│   │   ├── Managers/          # Core game systems
│   │   │   ├── AIManager.cs
│   │   │   ├── GameManager.cs
│   │   │   └── TimeManager.cs
│   │   ├── World/             # Procedural generation
│   │   │   ├── WorldSeedManager.cs
│   │   │   ├── TerrainGenerator.cs
│   │   │   ├── VillageBuilder.cs
│   │   │   └── ForestGenerator.cs
│   │   └── Utilities/         # Helper functions
│   │       └── NoiseGenerator.cs
│   ├── Models/                # 3D models (add yours here)
│   ├── Prefabs/               # Building/tree prefabs
│   └── Scenes/                # Unity scenes
└── Documentation/
    ├── SETUP.md               # Full setup guide
    ├── UNITY_MCP_GUIDE.md     # Cursor integration
    └── CONVERSION_NOTES.md    # Godot → Unity details
```

## Next Steps

1. **Read the Setup Guide**: `Documentation/SETUP.md`
2. **Configure Unity MCP**: `Documentation/UNITY_MCP_GUIDE.md`
3. **Import Your Assets**: Add your 3D models to `Assets/Models/`
4. **Create Prefabs**: Make building and tree prefabs
5. **Generate Your First World**: Press Play!

## Customization

All generation parameters are exposed in the Unity Inspector:

- **Terrain Size**: Change world dimensions
- **Height Settings**: Control mountain height
- **Village Count**: Number of villages to generate
- **Forest Density**: Tree and vegetation density
- **Seed**: Change the world seed for different worlds

## Converting from Godot

If you're coming from the original Godot project, see `Documentation/CONVERSION_NOTES.md` for:

- GDScript to C# mapping
- System architecture changes
- New features in Unity version
- Migration checklist

## Support

For questions or issues:

1. Check the documentation in the `Documentation/` folder
2. Review the code comments in each script
3. Use the Unity MCP integration to ask Cursor for help

## What's Missing (To Add)

The project provides the framework, but you need to add:

- **3D Models**: Buildings, trees, rocks, props
- **Textures**: Terrain textures (grass, rock, snow, dirt)
- **Player Controller**: Basic movement and camera
- **NPC Characters**: Character models and animations
- **UI**: Menus, HUD, dialogue boxes

All systems are ready to accept these assets - just create prefabs and assign them in the Inspector!

## Performance Tips

- Start with smaller terrain sizes for testing
- Reduce forest/village density if performance is slow
- Use Unity's Profiler to identify bottlenecks
- Consider LOD (Level of Detail) for distant objects
- Use object pooling for frequently spawned objects

## Example Seeds to Try

- **12345**: Balanced world with good variety
- **99999**: Mountain-heavy terrain
- **54321**: More plains and gentle hills
- **11111**: Dense forests
- **77777**: Scattered villages

Try different seeds to see the variety of worlds you can generate!
