using LLMUnity;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace NPC
{
    public class LLMUnitySample : MonoBehaviour
    {
        [SerializeField] private LLMCharacter llmCharacter;
        private bool isInitialized = false;
        private Stopwatch stopwatch = new Stopwatch();
        private float firstTokenTime = 0f;
        private int tokenCount = 0;

        private void Start()
        {
            // Initialize and send a test message when the game starts
            if (llmCharacter != null)
            {
                isInitialized = true;
                Debug.Log("[LLMUnitySample] Starting LLM interaction...");
                Game();
            }
            else
            {
                Debug.LogError("[LLMUnitySample] LLMCharacter is not assigned! Please assign it in the Inspector.");
            }
        }

        private void HandleReply(string reply)
        {
            // Handle the reply from the model with better logging
            if (string.IsNullOrEmpty(reply))
            {
                Debug.LogWarning("[LLMUnitySample] Received empty reply from LLM");
                return;
            }

            // Record first token time
            if (tokenCount == 0 && stopwatch.IsRunning)
            {
                firstTokenTime = (float)stopwatch.Elapsed.TotalSeconds;
                Debug.Log($"[LLMUnitySample] Time to first token: {firstTokenTime:F3} seconds");
            }

            tokenCount++;
            Debug.Log($"[LLMUnitySample] Token {tokenCount}: {reply}");
        }

        private void ReplyCompleted()
        {
            // Called when the reply is completed
            stopwatch.Stop();
            float totalTime = (float)stopwatch.Elapsed.TotalSeconds;
            float tokensPerSecond = tokenCount > 0 ? tokenCount / totalTime : 0;

            Debug.Log("[LLMUnitySample] ===== Response Metrics =====");
            Debug.Log($"[LLMUnitySample] Reply completed");
            Debug.Log($"[LLMUnitySample] Total response time: {totalTime:F3} seconds");
            Debug.Log($"[LLMUnitySample] Time to first token: {firstTokenTime:F3} seconds");
            Debug.Log($"[LLMUnitySample] Total tokens generated: {tokenCount}");
            Debug.Log($"[LLMUnitySample] Tokens per second: {tokensPerSecond:F2}");
            Debug.Log($"[LLMUnitySample] Average time per token: {(tokenCount > 0 ? totalTime / tokenCount : 0):F3} seconds");
            Debug.Log("[LLMUnitySample] =========================");
        }

        private void Game()
        {
            if (!isInitialized || llmCharacter == null)
            {
                Debug.LogError("[LLMUnitySample] Cannot send message - LLMCharacter not initialized");
                return;
            }

            // Reset metrics
            stopwatch.Reset();
            firstTokenTime = 0f;
            tokenCount = 0;

            string message = "Hello bot! How are you today?";
            Debug.Log($"[LLMUnitySample] Sending message: {message}");

            // Start timing
            stopwatch.Start();
            _ = llmCharacter.Chat(message, HandleReply, ReplyCompleted);
        }

        // Optional: Allow testing with a key press
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && isInitialized)
            {
                Debug.Log("[LLMUnitySample] Space key pressed - sending another message");
                Game();
            }
        }
    }
}