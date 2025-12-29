using UnityEngine;
using Hearthbound.Managers;

/// <summary>
/// Simple test script for AIManager
/// Add this to any GameObject and press Play to test AI integration
/// </summary>
public class AITest : MonoBehaviour
{
    void Start()
    {
        // Wait a moment for AIManager to initialize
        Invoke(nameof(TestNPC), 1f);
    }

    void TestNPC()
    {
        Debug.Log("🤖 Testing AI Integration...");
        
        // Test NPC dialogue
        AIManager.Instance.NPCDialogue(
            "Guard", 
            "Suspicious and brief", 
            "What's happening here?", 
            (response) => {
                Debug.Log("✅ NPC Response: " + response);
            }
        );
    }
}

