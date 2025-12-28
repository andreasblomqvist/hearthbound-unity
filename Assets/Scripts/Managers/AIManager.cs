using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Hearthbound.Managers
{
    /// <summary>
    /// Claude API Manager for NPC Dialogue
    /// Handles all communication with Anthropic Claude API
    /// Singleton - Access via AIManager.Instance
    /// </summary>
    public class AIManager : MonoBehaviour
    {
        #region Singleton
        private static AIManager _instance;
        public static AIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AIManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AIManager");
                        _instance = go.AddComponent<AIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Configuration
        private const string API_URL = "https://api.anthropic.com/v1/messages";
        private const string MODEL = "claude-sonnet-4-20250514";
        private const int MAX_TOKENS = 1000;
        #endregion

        #region Rate Limiting
        private int callCount = 0;
        private float lastResetTime = 0f;
        private const int MAX_CALLS_PER_PERIOD = 3;
        private const float RESET_PERIOD = 10f; // seconds
        #endregion

        #region API Key
        private string apiKey = "";
        #endregion

        #region Response Cache
        private Dictionary<string, string> responseCache = new Dictionary<string, string>();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadAPIKey();
        }

        private void Update()
        {
            // Reset rate limit counter every RESET_PERIOD seconds
            if (Time.time - lastResetTime > RESET_PERIOD)
            {
                callCount = 0;
                lastResetTime = Time.time;
            }
        }
        #endregion

        #region API Key Loading
        private void LoadAPIKey()
        {
            // Try to load from environment variable
            apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_ANTHROPIC_API_KEY_HERE")
            {
                Debug.LogWarning("⚠️ Anthropic API key not set! Please set ANTHROPIC_API_KEY environment variable");
            }
            else
            {
                Debug.Log("✅ Anthropic API key loaded from environment");
            }
        }
        #endregion

        #region Main API Functions
        /// <summary>
        /// Main function to call Claude API
        /// </summary>
        /// <param name="prompt">The question or request for Claude</param>
        /// <param name="context">Optional dictionary with additional context</param>
        /// <param name="callback">Callback with Claude's response</param>
        public void AskClaude(string prompt, Dictionary<string, object> context, Action<string> callback)
        {
            StartCoroutine(AskClaudeCoroutine(prompt, context, callback));
        }

        private IEnumerator AskClaudeCoroutine(string prompt, Dictionary<string, object> context, Action<string> callback)
        {
            // Check rate limit
            if (callCount >= MAX_CALLS_PER_PERIOD)
            {
                Debug.LogWarning("AI rate limit reached, using cached response");
                callback?.Invoke(GetFallbackResponse(context));
                yield break;
            }

            // Check cache
            string cacheKey = GenerateCacheKey(prompt, context);
            if (responseCache.ContainsKey(cacheKey))
            {
                Debug.Log("📦 Using cached AI response");
                callback?.Invoke(responseCache[cacheKey]);
                yield break;
            }

            // Validate API key
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_ANTHROPIC_API_KEY_HERE")
            {
                Debug.LogError("❌ No API key configured!");
                callback?.Invoke(GetFallbackResponse(context));
                yield break;
            }

            // Build full prompt with context
            string fullPrompt = BuildPrompt(prompt, context);

            // Prepare API request
            string jsonBody = CreateRequestBody(fullPrompt);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-api-key", apiKey);
                request.SetRequestHeader("anthropic-version", "2023-06-01");

                // Increment call counter
                callCount++;
                Debug.Log($"🤖 Claude API call #{callCount}/{MAX_CALLS_PER_PERIOD}");

                yield return request.SendWebRequest();

                string result;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    result = ParseResponse(request.downloadHandler.text);
                    // Cache the response
                    responseCache[cacheKey] = result;
                }
                else
                {
                    Debug.LogError($"❌ API Error: {request.error}");
                    result = GetFallbackResponse(context);
                }

                callback?.Invoke(result);
            }
        }

        /// <summary>
        /// Convenience function for NPC dialogue
        /// </summary>
        public void NPCDialogue(string npcName, string personality, string playerMessage, Action<string> callback)
        {
            Dictionary<string, object> context = new Dictionary<string, object>
            {
                { "npc_name", npcName },
                { "personality", personality },
                { "time", TimeManager.Instance != null ? TimeManager.Instance.GetFormattedTime() : "Unknown" }
            };

            AskClaude(playerMessage, context, callback);
        }
        #endregion

        #region Helper Functions
        private string BuildPrompt(string prompt, Dictionary<string, object> context)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("You are a helpful NPC in a fantasy RPG game.");
            sb.AppendLine();

            // Add NPC context
            if (context.ContainsKey("npc_name"))
                sb.AppendLine($"NPC: {context["npc_name"]}");
            if (context.ContainsKey("personality"))
                sb.AppendLine($"Personality: {context["personality"]}");
            if (context.ContainsKey("time"))
                sb.AppendLine($"Time: {context["time"]}");
            if (context.ContainsKey("activity"))
                sb.AppendLine($"Current activity: {context["activity"]}");
            if (context.ContainsKey("mood"))
                sb.AppendLine($"Mood: {context["mood"]}/100");

            sb.AppendLine();
            sb.AppendLine($"Player: \"{prompt}\"");
            sb.AppendLine();
            sb.AppendLine("Respond naturally in 2-3 sentences as this character. Be helpful and stay in character.");

            return sb.ToString();
        }

        private string CreateRequestBody(string prompt)
        {
            var requestData = new
            {
                model = MODEL,
                max_tokens = MAX_TOKENS,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            return JsonUtility.ToJson(requestData);
        }

        private string ParseResponse(string jsonResponse)
        {
            try
            {
                // Simple JSON parsing (for production, use a proper JSON library)
                ClaudeResponse response = JsonUtility.FromJson<ClaudeResponse>(jsonResponse);
                if (response != null && response.content != null && response.content.Length > 0)
                {
                    return response.content[0].text;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ JSON Parse Error: {e.Message}");
            }

            return "I'm having trouble thinking right now...";
        }

        private string GenerateCacheKey(string prompt, Dictionary<string, object> context)
        {
            string key = "";
            if (context.ContainsKey("npc_name"))
                key += context["npc_name"] + "_";
            
            // First 20 chars of prompt
            key += prompt.Length > 20 ? prompt.Substring(0, 20) : prompt;
            return key;
        }

        private string GetFallbackResponse(Dictionary<string, object> context)
        {
            string[] fallbacks = new string[]
            {
                "Let me think about that...",
                "Interesting question.",
                "I'm not sure right now.",
                "Come back and talk to me later."
            };
            return fallbacks[UnityEngine.Random.Range(0, fallbacks.Length)];
        }

        /// <summary>
        /// Clear the response cache
        /// </summary>
        public void ClearCache()
        {
            responseCache.Clear();
            Debug.Log("🗑️ AI response cache cleared");
        }
        #endregion

        #region Data Classes
        [Serializable]
        private class ClaudeResponse
        {
            public ContentItem[] content;
        }

        [Serializable]
        private class ContentItem
        {
            public string text;
        }
        #endregion
    }
}
