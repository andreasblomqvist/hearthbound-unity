# Third-Person Character Controller Setup Guide

## Overview

The third-person character controller system allows players to navigate the procedurally generated terrain using WASD movement and mouse look controls.

## Components

### Scripts

1. **ThirdPersonController.cs** - Handles character movement, walk/run speeds, and rotation
2. **ThirdPersonCamera.cs** - Handles camera follow, mouse look, and smooth camera movement
3. **PlayerSetup.cs** - Helper script to automatically set up the character and camera in the scene

## Quick Setup

### Method 1: Automatic Setup (Recommended)

1. In Unity, create an empty GameObject in your scene
2. Add the `PlayerSetup` component to it
3. Configure the settings:
   - **Spawn Position**: Where you want the player to start (default: 0, 0, 0)
   - **Find Terrain Height**: Check this to automatically position player on terrain
   - **Setup Camera**: Check this to automatically configure the camera
4. Right-click on the `PlayerSetup` component and select "Setup Player Character"
   - OR check "Setup On Start" to automatically set up when entering play mode

### Method 2: Manual Setup

1. **Create Character:**
   - Create a Capsule GameObject (GameObject > 3D Object > Capsule)
   - Name it "Player"
   - Remove the default Capsule Collider component
   - Add a CharacterController component:
     - Height: 2
     - Radius: 0.5
     - Center: (0, 1, 0)
   - Add the `ThirdPersonController` script

2. **Setup Camera:**
   - Select your Main Camera (or create one)
   - Add the `ThirdPersonCamera` script
   - In the Inspector, assign the Player transform to the "Target" field
   - Configure camera settings:
     - Distance: 5
     - Height Offset: 2
     - Mouse Sensitivity X/Y: 2

3. **Connect Camera to Controller:**
   - Select the Player GameObject
   - In the `ThirdPersonController` component, the camera should auto-detect
   - If not, you can manually set it via code: `controller.SetCameraTransform(camera.transform)`

4. **Position Player:**
   - Position the player at a good starting location on your terrain
   - Make sure the player is above the terrain surface

## Controls

- **W/A/S/D** - Move character (forward/left/backward/right)
- **Left Shift** - Hold to run (faster movement)
- **Mouse** - Look around / Rotate camera
- **Escape** - Unlock cursor
- **Left Click** - Lock cursor (when unlocked)

## Configuration

### ThirdPersonController Settings

- **Walk Speed**: Default movement speed (default: 5)
- **Run Speed**: Speed when holding Shift (default: 8)
- **Rotation Smooth Time**: How quickly character rotates to face movement direction (default: 0.1)
- **Acceleration**: How quickly character speeds up (default: 10)
- **Deceleration**: How quickly character slows down (default: 15)
- **Gravity**: Gravity strength (default: -9.81)

### ThirdPersonCamera Settings

- **Target**: The character transform to follow
- **Distance**: How far behind the character the camera stays (default: 5)
- **Height Offset**: Height above character center (default: 2)
- **Mouse Sensitivity X**: Horizontal mouse sensitivity (default: 2)
- **Mouse Sensitivity Y**: Vertical mouse sensitivity (default: 2)
- **Min/Max Pitch Angle**: Vertical look limits (default: -30° to 60°)
- **Position Smooth Time**: Camera follow smoothness (default: 0.1)
- **Lock Cursor**: Whether to lock cursor on start (default: true)

## Integration with Terrain

The character controller automatically works with Unity's Terrain system. The CharacterController component handles:
- Walking on slopes
- Collision with terrain
- Ground detection
- Gravity

Make sure your terrain has colliders enabled (Unity Terrain has this by default).

## Troubleshooting

### Character falls through terrain
- Make sure the Terrain has a Terrain Collider component
- Check that the CharacterController is properly configured
- Verify the character is positioned above the terrain

### Camera doesn't follow
- Make sure the Target is assigned in ThirdPersonCamera
- Check that the camera script is enabled
- Verify the camera GameObject is active

### Movement feels wrong
- Adjust the Walk Speed and Run Speed values
- Tune the Acceleration and Deceleration values
- Check that the camera is properly connected to the controller

### Mouse look doesn't work
- Press Escape to unlock cursor, then click to lock it again
- Check Mouse Sensitivity settings
- Verify cursor is locked (should be invisible and centered)

## Next Steps

- Add jumping mechanics
- Add character animations
- Add camera collision detection
- Add camera zoom
- Add interaction system

