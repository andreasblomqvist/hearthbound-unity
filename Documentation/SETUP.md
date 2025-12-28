# Hearthbound Unity Project Setup Guide

Welcome to the Unity version of Hearthbound! This guide will walk you through setting up the project for code-first procedural world generation.

## Requirements

- **Unity Hub**
- **Unity 2022.3 LTS** or newer
- **Cursor IDE** (or any code editor, but this guide is optimized for Cursor)
- **Node.js 18+** (for Unity MCP integration)

## Step 1: Create a New Unity Project

1.  Open **Unity Hub**.
2.  Create a new project using the **3D (URP)** template. Universal Render Pipeline (URP) is recommended for better performance with large worlds.
3.  Name your project `HearthboundUnity` and choose a location.
4.  Click **Create Project**.

## Step 2: Import Project Files

1.  Once the project is open, close Unity Editor.
2.  Copy the `Assets` and `Documentation` folders from this delivery into your new Unity project folder.
3.  When prompted, choose to **Replace** any existing files.
4.  Re-open your Unity project. Unity will import all the scripts and assets.

## Step 3: Configure the Scene

1.  Create a new scene by going to **File > New Scene**.
2.  Save the scene as `WorldGeneration` inside the `Assets/Scenes` folder.
3.  In the Hierarchy, create three empty GameObjects:
    -   `[MANAGERS]`
    -   `[WORLD]`
    -   `[PLAYER]`

4.  **Configure Managers:**
    -   Select the `[MANAGERS]` GameObject.
    -   Add the following scripts as components:
        -   `GameManager` (`Assets/Scripts/Managers/GameManager.cs`)
        -   `TimeManager` (`Assets/Scripts/Managers/TimeManager.cs`)
        -   `AIManager` (`Assets/Scripts/Managers/AIManager.cs`)

5.  **Configure World Generation:**
    -   Select the `[WORLD]` GameObject.
    -   Add the following scripts as components:
        -   `WorldSeedManager` (`Assets/Scripts/World/WorldSeedManager.cs`)
        -   `TerrainGenerator` (`Assets/Scripts/World/TerrainGenerator.cs`)
        -   `VillageBuilder` (`Assets/Scripts/World/VillageBuilder.cs`)
        -   `ForestGenerator` (`Assets/Scripts/World/ForestGenerator.cs`)
    -   Create a new **Terrain** object (**GameObject > 3D Object > Terrain**).
    -   Parent the `Terrain` object under the `[WORLD]` GameObject.
    -   Drag the `Terrain` object onto the `Terrain` field of the `TerrainGenerator` component.

6.  **Review Inspector Settings:**
    -   Select the `[WORLD]` GameObject.
    -   In the Inspector, you can now see all the procedural generation parameters for `TerrainGenerator`, `VillageBuilder`, and `ForestGenerator`.
    -   You can leave the default values for now.

## Step 4: Generate Your First World

1.  Select the `[WORLD]` GameObject.
2.  In the Inspector, find the **World Seed Manager** component.
3.  You can either:
    -   Enter a specific integer in the `World Seed` field.
    -   Check the `Use Random Seed` box to get a different world every time.
4.  Press the **Play** button in the Unity Editor.

Your procedural world will be generated automatically! You should see a terrain with mountains, biomes, villages, and forests.

## Step 5: Asset Integration (Next Steps)

The project is currently using basic primitives and placeholder textures. To bring your world to life, you need to assign your 3D models and textures.

1.  **Import Your Assets:**
    -   Copy your Quaternius models into the `Assets/Models` folder.
    -   Copy your textures into a new `Assets/Textures` folder.

2.  **Create Prefabs:**
    -   Create prefabs for your buildings, trees, and props in the `Assets/Prefabs` folder.

3.  **Assign Prefabs:**
    -   Select the `[WORLD]` GameObject.
    -   In the **Village Builder** component, drag your building prefabs into the `House Prefabs`, `Farm Prefabs`, etc. arrays.
    -   In the **Forest Generator** component, drag your tree and rock prefabs into the `Tree Prefabs`, `Rock Prefabs`, etc. arrays.

4.  **Assign Terrain Textures:**
    -   Create **Terrain Layers** in the Project window (**Create > Terrain Layer**).
    -   Assign your grass, rock, and snow textures to these layers.
    -   In the `Terrain` component, go to the **Paint Terrain** tab and click **Edit Terrain Layers...**.
    -   Add your created terrain layers.
    -   The `TerrainGenerator` will automatically use these layers for splatting.

## Step 6: AI Integration

1.  **Set API Key:**
    -   You must set your Anthropic API key as an environment variable named `ANTHROPIC_API_KEY`.
    -   In Windows, you can set this via **System Properties > Environment Variables**.
    -   In macOS/Linux, you can add `export ANTHROPIC_API_KEY="your_key_here"` to your `.bashrc` or `.zshrc` file.
    -   You must **restart Unity and/or your computer** for the environment variable to be detected.

2.  **Test the AI:**
    -   The `AIManager` is a singleton and can be called from any script.
    -   Example usage:
        ```csharp
        AIManager.Instance.NPCDialogue("Guard", "Suspicious and brief", "What's happening here?", (response) => {
            Debug.Log("NPC Response: " + response);
        });
        ```

## Next Steps

Now that your project is set up, you can start building your game!

-   **Explore the code** in the `Assets/Scripts` folder.
-   **Tweak the generation parameters** in the Inspector to change how your world looks.
-   **Follow the `UNITY_MCP_GUIDE.md`** to connect your project to Cursor for a powerful code-first workflow.
