using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using UnityEngine;
using LLMUnity;

namespace LLMDataArchitectTest.Tests
{
    /// <summary>
    /// 事前設定済みプレハブを使用するシンプルなRemote LLMテスト
    /// プログラム的な設定は一切行わず、プレハブの設定に依存
    /// </summary>
    public class SimplePrefabRemoteTest
    {
        private LLM _llm;
        private LLMCharacter _testCharacter;
        private GameObject _llmGameObject;
        private GameObject _characterGameObject;

        [SetUp]
        public void SetUp()
        {
            Debug.Log("=== Simple Prefab Remote Test Setup ===");

            // プレハブからRemoteLLMを読み込み（設定済み前提）
            GameObject llmPrefab = Resources.Load<GameObject>("TestLLM");

            if (llmPrefab == null)
            {
                Assert.Inconclusive(@"RemoteLLM prefab not found! Please create it manually:

1. Create new Scene
2. Hierarchy → Create Empty → 'RemoteLLM'  
3. RemoteLLM → Add Component → LLM
4. LLM Inspector:
   - Remote: ✓ Check
   - Host: localhost
   - Port: 11434  
   - Model: gemma2:2b
5. Drag RemoteLLM to Assets/Resources/RemoteLLM.prefab
6. Start Ollama: ollama serve
7. Run test again");
                return;
            }

            Debug.Log("Found RemoteLLM prefab, instantiating...");
            _llmGameObject = GameObject.Instantiate(llmPrefab);
            _llm = _llmGameObject.GetComponent<LLM>();

            if (_llm == null)
            {
                Assert.Fail("LLM component not found in RemoteLLM prefab");
                return;
            }

            Debug.Log("✓ Remote LLM loaded from prefab");

            // プレハブ設定の確認
            LogPrefabSettings();

            // LLMCharacterを作成
            CreateTestCharacter();

            Debug.Log("Simple prefab remote test setup completed!");
        }

        /// <summary>
        /// プレハブの設定を確認
        /// </summary>
        private void LogPrefabSettings()
        {
            Debug.Log("=== Prefab Settings ===");

            try
            {
                var llmType = typeof(LLM);

                var remoteField = llmType.GetField("remote", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var hostField = llmType.GetField("host", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var portField = llmType.GetField("port", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var modelField = llmType.GetField("model", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                Debug.Log($"Remote: {remoteField?.GetValue(_llm)}");
                Debug.Log($"Host: {hostField?.GetValue(_llm)}");
                Debug.Log($"Port: {portField?.GetValue(_llm)}");
                Debug.Log($"Model: {modelField?.GetValue(_llm)}");

                bool isRemote = (bool)(remoteField?.GetValue(_llm) ?? false);
                if (!isRemote)
                {
                    Debug.LogWarning("⚠️ Prefab is not configured for Remote mode!");
                    Debug.LogWarning("Please check the RemoteLLM prefab settings in Inspector");
                }
                else
                {
                    Debug.Log("✓ Prefab is correctly configured for Remote mode");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not read prefab settings: {ex.Message}");
            }
        }

        /// <summary>
        /// テスト用LLMCharacterを作成
        /// </summary>
        private void CreateTestCharacter()
        {
            _characterGameObject = new GameObject("TestLLMCharacter");
            _testCharacter = _characterGameObject.AddComponent<LLMCharacter>();

            // LLMCharacterにLLMを設定
            _testCharacter.llm = _llm;

            // 基本設定
            _testCharacter.playerName = "Player";
            _testCharacter.AIName = "GemmaAI";
            _testCharacter.temperature = 0.8f;
            _testCharacter.topP = 0.9f;
            _testCharacter.numPredict = 100;
            _testCharacter.stream = true;

            Debug.Log("✓ LLMCharacter created");
        }

        [Test]
        public async UniTask TestOllamaConnection()
        {
            Debug.Log("Testing Ollama server connection...");

            bool ollamaRunning = await CheckOllamaServer();

            if (!ollamaRunning)
            {
                Assert.Inconclusive(@"Ollama server not running. Please:
1. Install Ollama from https://ollama.ai
2. Run: ollama pull gemma2:2b  
3. Run: ollama serve
4. Verify at http://localhost:11434
5. Run test again");
                return;
            }

            Debug.Log("✓ Ollama server is running");
            Assert.Pass("Ollama connection test passed");
        }

        [Test]
        public async UniTask TestPrefabRemoteLLM()
        {
            if (_testCharacter == null)
            {
                Assert.Fail("LLMCharacter not initialized");
                return;
            }

            bool ollamaRunning = await CheckOllamaServer();
            if (!ollamaRunning)
            {
                Assert.Inconclusive("Ollama server not running");
                return;
            }

            Debug.Log("Starting prefab remote LLM test...");

            string testPrompt = "Say hello";
            string response = "";
            bool completed = false;
            Exception testException = null;

            try
            {
                await _testCharacter.Chat(testPrompt,
                    callback: (string partialResponse) =>
                    {
                        response = partialResponse;
                        Debug.Log($"Response: {partialResponse}");
                    },
                    completionCallback: () =>
                    {
                        completed = true;
                        Debug.Log($"Completed: {response}");
                    }
                );

                // 60秒待機
                float timeout = 60f;
                float elapsed = 0f;

                while (!completed && elapsed < timeout)
                {
                    await UniTask.Delay(1000);
                    elapsed += 1f;

                    if (elapsed % 10f < 1f)
                    {
                        Debug.Log($"Waiting... ({elapsed:F0}s/{timeout:F0}s)");
                    }
                }

                if (!completed)
                {
                    Assert.Fail($"Test timed out after {timeout} seconds");
                    return;
                }

                Assert.IsTrue(!string.IsNullOrEmpty(response), "Response is empty");
                Assert.IsTrue(response.Length > 1, "Response too short");

                Debug.Log($"✓ Prefab Remote LLM test passed! Response: {response}");
            }
            catch (Exception ex)
            {
                testException = ex;
                Debug.LogError($"Test failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Ollamaサーバーの確認
        /// </summary>
        private async UniTask<bool> CheckOllamaServer()
        {
            try
            {
                using (var www = UnityEngine.Networking.UnityWebRequest.Get("http://localhost:11434/api/tags"))
                {
                    www.timeout = 5; // 5秒タイムアウト
                    await www.SendWebRequest();

                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        Debug.Log("✓ Ollama server responding");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"Ollama server not responding: {www.error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Ollama server check failed: {ex.Message}");
                return false;
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_characterGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_characterGameObject);
                _characterGameObject = null;
                _testCharacter = null;
            }

            if (_llmGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_llmGameObject);
                _llmGameObject = null;
                _llm = null;
            }
        }
    }
}