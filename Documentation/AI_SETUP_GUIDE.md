# AI Integration Setup Guide

## Step 6: AI Integration

This guide will help you set up the Anthropic Claude API for NPC dialogue in your Hearthbound Unity project.

## Prerequisites

- An Anthropic API key (get one at https://console.anthropic.com/)
- Windows 10/11 (this guide is for Windows)

## Setting Up the API Key

### Option 1: System Environment Variables (Recommended)

1. **Open System Properties:**
   - Press `Win + R` to open Run dialog
   - Type `sysdm.cpl` and press Enter
   - OR: Right-click "This PC" > Properties > Advanced system settings

2. **Set Environment Variable:**
   - Click "Environment Variables..." button
   - Under "User variables" (or "System variables" if you want it for all users), click "New..."
   - Variable name: `ANTHROPIC_API_KEY`
   - Variable value: Your Anthropic API key (starts with `sk-ant-...`)
   - Click OK on all dialogs

3. **Restart Unity:**
   - Close Unity Editor completely
   - Reopen Unity Editor
   - The AIManager will automatically load the API key on startup

### Option 2: PowerShell (Temporary - Current Session Only)

Open PowerShell and run:
```powershell
[System.Environment]::SetEnvironmentVariable("ANTHROPIC_API_KEY", "your-api-key-here", "User")
```

Then restart Unity.

### Option 3: Command Prompt (Permanent)

Open Command Prompt as Administrator and run:
```cmd
setx ANTHROPIC_API_KEY "your-api-key-here"
```

Then restart Unity.

## Verifying the Setup

1. **Check Unity Console:**
   - After restarting Unity, look for one of these messages:
     - ✅ "Anthropic API key loaded from environment" (success!)
     - ⚠️ "Anthropic API key not set!" (not set correctly)

2. **Test the AI:**
   - Create a test script or use the example below
   - The AIManager will use the API key automatically

## Testing the AIManager

### Example Test Script

Create a new C# script called `AITest.cs` in `Assets/Scripts/`:

```csharp
using UnityEngine;
using Hearthbound.Managers;

public class AITest : MonoBehaviour
{
    void Start()
    {
        // Test NPC dialogue
        AIManager.Instance.NPCDialogue(
            "Guard", 
            "Suspicious and brief", 
            "What's happening here?", 
            (response) => {
                Debug.Log("NPC Response: " + response);
            }
        );
    }
}
```

Add this script to any GameObject in your scene and press Play. Check the Console for the NPC response.

## Troubleshooting

- **"API key not set" warning:** Make sure you restarted Unity after setting the environment variable
- **API errors:** Check that your API key is valid and has credits
- **Rate limiting:** The AIManager has built-in rate limiting (3 calls per 10 seconds)

## Next Steps

Once the API key is set up, you can use AIManager from any script:

```csharp
AIManager.Instance.NPCDialogue(npcName, personality, playerMessage, callback);
```

The AIManager will handle:
- API communication
- Rate limiting
- Response caching
- Error handling

