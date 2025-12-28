# Hearthbound - Unity Version

This is the Unity conversion of the Hearthbound project, focused on **code-first, seed-based procedural world generation**.

## Project Overview

This project provides a complete framework for generating open worlds in Unity programmatically. It includes systems for:

-   **Seed-Based Terrain Generation**: Create mountains, valleys, and plains from a single integer seed.
-   **Procedural Biomes**: Automatically place forests, fields, and other biomes.
-   **Village Generation**: Spawn villages with buildings and props.
-   **Forest Generation**: Populate the world with trees, bushes, and rocks.
-   **AI-Powered NPCs**: Integrated with Anthropic's Claude API for dynamic dialogue.
-   **Day/Night Cycle**: A full time-of-day system with lighting changes.

## Getting Started

To get started, follow the setup guide:

**[➡️ Full Setup Guide](./Documentation/SETUP.md)**

## Code-First Workflow with Cursor

This project is designed to be used with **Cursor IDE** and the **Unity MCP integration**. This allows you to write code and control the Unity Editor directly from your text editor.

For instructions on how to set this up, see:

**[➡️ Unity MCP Integration Guide](./Documentation/UNITY_MCP_GUIDE.md)**

## How to Generate a World

1.  Open the `WorldGeneration` scene.
2.  Select the `[WORLD]` GameObject.
3.  In the **World Seed Manager** component, enter a seed or check `Use Random Seed`.
4.  Press **Play**.

## Key Scripts

-   **`Assets/Scripts/World/WorldSeedManager.cs`**: The main controller for seed-based generation.
-   **`Assets/Scripts/World/TerrainGenerator.cs`**: Generates the terrain heightmap and textures.
-   **`Assets/Scripts/World/VillageBuilder.cs`**: Procedurally places villages.
-   **`Assets/Scripts/World/ForestGenerator.cs`**: Spawns trees and other vegetation.
-   **`Assets/Scripts/Managers/AIManager.cs`**: Handles all communication with the Claude API for NPC dialogue.

## Customization

All generation parameters can be tweaked in the Inspector by selecting the `[WORLD]` GameObject. You can control:

-   Terrain size and height
-   Number of villages and buildings
-   Forest density and size
-   And much more!

## Asset Integration

The project uses placeholder assets by default. To use your own models:

1.  Import your 3D models into the `Assets/Models` folder.
2.  Create prefabs in the `Assets/Prefabs` folder.
3.  Assign these prefabs to the `VillageBuilder` and `ForestGenerator` components in the Inspector.

## Godot to Unity Conversion

For a detailed breakdown of how this project was converted from the original Godot version, see the conversion notes:

**[➡️ Godot to Unity Conversion Notes](./Documentation/CONVERSION_NOTES.md)**
