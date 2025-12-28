# Hearthbound: Godot to Unity Conversion Plan

## Project Overview

**Current State**: Godot 4.3+ project with AI-powered NPCs and procedural terrain generation
**Target State**: Unity project with same functionality, enhanced with seed-based world generation and Unity MCP integration

## What You Already Have in Godot

### 1. AI System (Claude API Integration)
**File**: `scripts/managers/ai_manager.gd`
**Features**:
- Claude API integration for NPC dialogue
- Rate limiting and caching
- Context-aware responses
- Fallback responses when API unavailable

### 2. World Generation Scripts
**Files**:
- `scripts/world/procedural_terrain_generator.gd` - Noise-based terrain generation
- `scripts/world/village_builder.gd` - Procedural village placement
- `scripts/world/forest_generator.gd` - Tree and vegetation scattering
- `scripts/world/biome_world_generator.gd` - Biome system

**Features**:
- HTerrain plugin integration
- FastNoiseLite for terrain generation
- Vegetation placement with slope detection
- Modular building system

### 3. Game Managers
**Files**:
- `scripts/managers/game_manager.gd` - Game state management
- `scripts/managers/quest_manager.gd` - Quest system
- `scripts/managers/time_manager.gd` - Day/night cycle
- `scripts/managers/journey_manager.gd` - Journey tracking

### 4. Character Systems
**Files**:
- `scripts/player.gd` - Player controller
- `scripts/npc.gd` - NPC base class with AI integration

### 5. Assets
- Quaternius low-poly models (buildings, environment, props)
- Materials and shaders
- Terrain data

## Unity Conversion Architecture

### Project Structure

```
HearthboundUnity/
├── Assets/
│   ├── Scripts/
│   │   ├── Managers/
│   │   │   ├── AIManager.cs
│   │   │   ├── GameManager.cs
│   │   │   ├── QuestManager.cs
│   │   │   ├── TimeManager.cs
│   │   │   └── JourneyManager.cs
│   │   ├── World/
│   │   │   ├── TerrainGenerator.cs
│   │   │   ├── BiomeSystem.cs
│   │   │   ├── VillageBuilder.cs
│   │   │   ├── ForestGenerator.cs
│   │   │   └── WorldSeedManager.cs (NEW)
│   │   ├── Characters/
│   │   │   ├── PlayerController.cs
│   │   │   └── NPCController.cs
│   │   └── Utilities/
│   │       ├── NoiseGenerator.cs
│   │       └── EnvLoader.cs
│   ├── Models/
│   │   ├── Buildings/
│   │   ├── Environment/
│   │   └── Characters/
│   ├── Materials/
│   ├── Prefabs/
│   │   ├── Buildings/
│   │   ├── Trees/
│   │   └── Props/
│   ├── Scenes/
│   │   ├── Main.unity
│   │   ├── TestWorld.unity
│   │   └── Village.unity
│   └── Resources/
│       └── WorldSeeds/
└── Documentation/
    ├── SETUP.md
    ├── UNITY_MCP_GUIDE.md
    └── CONVERSION_NOTES.md
```

## Key Conversions

### 1. GDScript to C# Mapping

| Godot (GDScript) | Unity (C#) |
|------------------|------------|
| `extends Node` | `class : MonoBehaviour` |
| `@export var` | `[SerializeField] private` or `public` |
| `func _ready()` | `void Start()` or `void Awake()` |
| `func _process(delta)` | `void Update()` |
| `func _physics_process(delta)` | `void FixedUpdate()` |
| `Vector3(x, y, z)` | `new Vector3(x, y, z)` |
| `await` | `async/await` with `Task` |
| `signal` | `event` or `UnityEvent` |
| `get_node()` | `GetComponent<>()` or `Find()` |
| `preload()` | `Resources.Load<>()` |
| `PackedScene` | `GameObject` prefab |
| `FastNoiseLite` | Custom noise or Unity packages |

### 2. Terrain System Conversion

**Godot HTerrain** → **Unity Terrain System**

| Feature | Godot | Unity |
|---------|-------|-------|
| Terrain Creation | HTerrain plugin | `Terrain` component |
| Heightmap | `terrain_data.set_all()` | `TerrainData.SetHeights()` |
| Texture Splatting | HTerrain textures | `TerrainLayer` and `SetAlphamaps()` |
| Detail Objects | Manual placement | `DetailPrototype` and `SetDetailLayer()` |
| Trees | Manual instantiation | `TreePrototype` and `AddTreeInstance()` |

### 3. API Integration

**Claude API** (same in both):
- HTTP requests work similarly
- Unity uses `UnityWebRequest` instead of `HTTPRequest`
- Async/await pattern supported in both

### 4. Noise Generation

**Godot FastNoiseLite** → **Unity Options**:
1. Port FastNoiseLite to C# (recommended)
2. Use Unity's built-in Perlin noise (`Mathf.PerlinNoise`)
3. Use third-party noise libraries (LibNoise, FastNoiseLite C# port)

## New Features for Unity Version

### 1. Seed-Based World Generation

**WorldSeedManager.cs**:
```csharp
public class WorldSeedManager : MonoBehaviour
{
    [SerializeField] private int worldSeed = 0;
    [SerializeField] private bool useRandomSeed = false;
    
    public int GetSeed()
    {
        if (useRandomSeed)
            return Random.Range(int.MinValue, int.MaxValue);
        return worldSeed;
    }
    
    public void SetSeed(int seed)
    {
        worldSeed = seed;
        Random.InitState(seed);
    }
}
```

All procedural generation will use this seed to ensure reproducibility.

### 2. Enhanced Terrain Generation

**TerrainGenerator.cs** features:
- Multiple biomes (plains, forests, mountains, lakes)
- Seed-based noise generation
- Configurable parameters (size, resolution, height)
- Automatic texture splatting based on height/slope
- Detail layer placement (grass, rocks)

### 3. Improved Village System

**VillageBuilder.cs** features:
- Seed-based village placement
- Road generation between villages
- Building variety based on village type
- Population density control
- Snap-to-terrain placement

### 4. Unity MCP Integration

**Benefits**:
- Write all code in Cursor
- AI-assisted script generation
- Direct Unity Editor control from Cursor
- Test and iterate without leaving Cursor

## Conversion Steps

### Phase 1: Setup
1. Create new Unity project (Unity 2022.3 LTS or newer)
2. Install Unity MCP package
3. Configure Cursor with Unity MCP
4. Set up project structure

### Phase 2: Core Systems
1. Convert AIManager.cs (Claude API integration)
2. Convert GameManager.cs (singleton pattern)
3. Convert TimeManager.cs (day/night cycle)
4. Create EnvLoader.cs (environment variables)

### Phase 3: World Generation
1. Implement NoiseGenerator.cs (FastNoiseLite port or alternative)
2. Create WorldSeedManager.cs (NEW)
3. Convert TerrainGenerator.cs with seed support
4. Implement BiomeSystem.cs
5. Convert VillageBuilder.cs
6. Convert ForestGenerator.cs

### Phase 4: Characters
1. Convert PlayerController.cs
2. Convert NPCController.cs with AI integration
3. Implement dialogue system

### Phase 5: Assets
1. Import Quaternius models
2. Create prefabs for buildings, trees, props
3. Set up materials
4. Create terrain layers

### Phase 6: Testing & Polish
1. Test seed-based generation
2. Verify AI integration
3. Performance optimization
4. Documentation

## Key Advantages of Unity Version

### 1. Better Terrain Tools
- Unity's terrain system is more mature
- Better performance for large worlds
- Built-in LOD and culling
- Easier texture splatting

### 2. Code-First Workflow
- Unity MCP enables full Cursor integration
- Can create/modify GameObjects from code
- Less manual editor work required
- Better for procedural generation

### 3. Seed-Based Generation
- Reproducible worlds
- Shareable seeds
- Easier testing and debugging
- Consistent results

### 4. Asset Ecosystem
- Larger asset store
- More free assets available
- Better community support
- More tutorials and resources

### 5. Performance
- Better optimization for large worlds
- More efficient terrain rendering
- Better multithreading support
- Profiling tools

## Migration Checklist

- [ ] Create Unity project structure
- [ ] Set up Unity MCP in Cursor
- [ ] Convert AIManager (Claude API)
- [ ] Convert GameManager
- [ ] Convert TimeManager
- [ ] Implement WorldSeedManager (NEW)
- [ ] Implement NoiseGenerator
- [ ] Convert TerrainGenerator with seed support
- [ ] Convert BiomeSystem
- [ ] Convert VillageBuilder
- [ ] Convert ForestGenerator
- [ ] Convert PlayerController
- [ ] Convert NPCController
- [ ] Import 3D models
- [ ] Create prefabs
- [ ] Set up materials
- [ ] Create test scenes
- [ ] Test seed generation
- [ ] Test AI integration
- [ ] Write documentation
- [ ] Create setup guide

## Estimated Effort

- **Core Systems Conversion**: 2-3 hours
- **World Generation System**: 3-4 hours
- **Character Systems**: 1-2 hours
- **Asset Import & Setup**: 1-2 hours
- **Testing & Polish**: 2-3 hours
- **Documentation**: 1 hour

**Total**: ~10-15 hours of development time

## Next Steps

1. Create Unity project with proper structure
2. Implement core managers (AI, Game, Time)
3. Build seed-based terrain generation system
4. Add village and forest generation
5. Test with multiple seeds
6. Document everything for Cursor workflow
