using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using UnityEngine;
using LLMUnity;

namespace LLMDataArchitectTest.Tests
{
    /// <summary>
    /// Remote LLM (Ollama)を使用するテスト
    /// ローカルのネイティブライブラリ問題を回避
    /// </summary>
    public class RemoteLLMTest
    {
        private LLM _llm;
        private LLMCharacter _testCharacter;
        private GameObject _llmGameObject;
        private GameObject _characterGameObject;

        [SetUp]
        public void SetUp()
        {
            Debug.Log("=== Remote LLM Test Setup ===");

            try
            {
                // Option 1: プレハブからLLMを読み込み
                GameObject llmPrefab = Resources.Load<GameObject>("RemoteLLM");
                if (llmPrefab != null)
                {
                    Debug.Log("Found RemoteLLM prefab, instantiating...");
                    _llmGameObject = GameObject.Instantiate(llmPrefab);
                    _llm = _llmGameObject.GetComponent<LLM>();

                    if (_llm != null)
                    {
                        Debug.Log("✓ Remote LLM component loaded from prefab");
                        VerifyRemoteSettings(); // プレハブの設定確認
                    }
                    else
                    {
                        Debug.LogError("LLM component not found in RemoteLLM prefab");
                    }
                }
                else
                {
                    Debug.LogWarning("RemoteLLM prefab not found in Resources folder");
                    Debug.LogWarning("Creating Remote LLM programmatically...");

                    // Option 2: プログラム的にRemote LLMを作成
                    CreateRemoteLLM();
                }

                if (_llm == null)
                {
                    Assert.Fail(@"Remote LLM setup failed! Try this:
1. Create empty GameObject → Add LLM component
2. Set LLM to Remote mode in Inspector
3. Set Host: localhost, Port: 11434, Model: gemma2:2b
4. Create prefab: Drag to Assets/Resources/RemoteLLM.prefab
5. Make sure Ollama is running: ollama serve
6. Run test again");
                    return;
                }

                CreateTestCharacter();

                Debug.Log("Remote LLM test setup completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Setup failed: {ex.Message}");
                Assert.Fail($"Setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Remote LLM (Ollama)を作成
        /// </summary>
        private void CreateRemoteLLM()
        {
            Debug.Log("Creating Remote LLM for Ollama...");

            _llmGameObject = new GameObject("RemoteLLM");

            // LLMコンポーネント追加前にRemote設定を準備
            // Awakeが実行される前に設定を完了させる
            try
            {
                _llm = _llmGameObject.AddComponent<LLM>();

                // AddComponent直後、Awakeが完了する前に設定
                SetLLMRemoteConfigurationBeforeAwake();

                Debug.Log("✓ Remote LLM created");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create LLM component: {ex.Message}");
                Debug.LogError("This is likely due to the 'No model file provided' error in Remote mode");

                // フォールバック: LLMClient のみを使用
                CreateLLMClientFallback();
            }
        }

        /// <summary>
        /// Awake前にRemote設定を行う
        /// </summary>
        private void SetLLMRemoteConfigurationBeforeAwake()
        {
            if (_llm == null)
                return;

            Debug.Log("Setting Remote configuration before Awake...");

            try
            {
                var llmType = typeof(LLM);

                // Remote フラグを最初に設定（最重要）
                var remoteField = llmType.GetField("remote",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (remoteField != null)
                {
                    remoteField.SetValue(_llm, true);
                    Debug.Log("✓ Remote mode enabled BEFORE Awake");
                }

                // model フィールドを空文字に設定（ローカルモデル要求を回避）
                var modelField = llmType.GetField("model",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (modelField != null)
                {
                    modelField.SetValue(_llm, ""); // 空文字でローカルモデル要求を回避
                    Debug.Log("✓ Model field cleared to avoid local file requirement");
                }

                // その他のRemote設定
                SetRemoteConnectionSettings();

            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set remote configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Remote接続設定
        /// </summary>
        private void SetRemoteConnectionSettings()
        {
            var llmType = typeof(LLM);

            // ホスト設定
            var hostField = llmType.GetField("host",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            if (hostField != null)
            {
                hostField.SetValue(_llm, "localhost");
                Debug.Log("✓ Host set to localhost");
            }

            // ポート設定
            var portField = llmType.GetField("port",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            if (portField != null)
            {
                portField.SetValue(_llm, 11434);
                Debug.Log("✓ Port set to 11434");
            }

            // Remote用モデル名設定
            var remoteModelField = llmType.GetField("remoteModel",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            if (remoteModelField != null)
            {
                remoteModelField.SetValue(_llm, "gemma2:2b");
                Debug.Log("✓ Remote model set to gemma2:2b");
            }
            else
            {
                // remoteModelフィールドがない場合、modelフィールドにリモートモデル名を設定
                var modelField = llmType.GetField("model",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (modelField != null)
                {
                    modelField.SetValue(_llm, "gemma2:2b");
                    Debug.Log("✓ Model field set to gemma2:2b for remote");
                }
            }
        }

        /// <summary>
        /// LLMClientフォールバック（LLMが使えない場合）
        /// </summary>
        private void CreateLLMClientFallback()
        {
            Debug.LogWarning("Creating LLMClient as fallback...");

            try
            {
                // LLMClientコンポーネントを試す（LLMより軽量）
                var llmClientType = System.Type.GetType("LLMUnity.LLMClient");
                if (llmClientType != null)
                {
                    var llmClient = _llmGameObject.AddComponent(llmClientType);
                    Debug.Log("✓ LLMClient created as fallback");

                    // LLMClientに同様の設定を適用
                    SetRemoteConfigurationForComponent(llmClient);

                    // LLMフィールドにLLMClientを設定（互換性のため）
                    _llm = llmClient as LLM;
                }
                else
                {
                    Debug.LogError("LLMClient type not found");
                    throw new Exception("Neither LLM nor LLMClient could be created");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LLMClient fallback also failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 任意のコンポーネントにRemote設定を適用
        /// </summary>
        private void SetRemoteConfigurationForComponent(Component component)
        {
            var componentType = component.GetType();

            try
            {
                var remoteField = componentType.GetField("remote",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                if (remoteField != null)
                {
                    remoteField.SetValue(component, true);
                }

                var hostField = componentType.GetField("host",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                if (hostField != null)
                {
                    hostField.SetValue(component, "localhost");
                }

                var portField = componentType.GetField("port",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                if (portField != null)
                {
                    portField.SetValue(component, 11434);
                }

                Debug.Log($"✓ Remote configuration applied to {componentType.Name}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not configure {componentType.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// LLMのRemote設定
        /// </summary>
        private void SetLLMRemoteConfiguration()
        {
            try
            {
                Debug.Log("Configuring LLM for remote connection...");

                // Reflectionで設定（LLM for Unityのバージョンに応じて調整）
                var llmType = typeof(LLM);

                // Remote モードを有効化
                var remoteField = llmType.GetField("remote",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (remoteField != null)
                {
                    remoteField.SetValue(_llm, true);
                    Debug.Log("✓ Remote mode enabled");
                }

                // ホスト設定
                var hostField = llmType.GetField("host",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (hostField != null)
                {
                    hostField.SetValue(_llm, "localhost");
                    Debug.Log("✓ Host set to localhost");
                }

                // ポート設定
                var portField = llmType.GetField("port",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (portField != null)
                {
                    portField.SetValue(_llm, 11434);
                    Debug.Log("✓ Port set to 11434");
                }

                // モデル名設定
                var modelField = llmType.GetField("model",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);

                if (modelField != null)
                {
                    modelField.SetValue(_llm, "gemma2:2b");
                    Debug.Log("✓ Model set to gemma2:2b");
                }

                // 設定確認
                Debug.Log("=== LLM Configuration ===");
                Debug.Log($"Remote: {remoteField?.GetValue(_llm)}");
                Debug.Log($"Host: {hostField?.GetValue(_llm)}");
                Debug.Log($"Port: {portField?.GetValue(_llm)}");
                Debug.Log($"Model: {modelField?.GetValue(_llm)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to configure remote LLM: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// テスト用LLMCharacterを作成（Awakeタイミング問題を回避）
        /// </summary>
        private void CreateTestCharacter()
        {
            Debug.Log("Creating LLMCharacter with proper initialization order...");

            _characterGameObject = new GameObject("RemoteLLMCharacter");

            // LLMCharacterコンポーネントを追加前にLLMを事前設定
            // これでAwakeでのエラーを回避
            _testCharacter = _characterGameObject.AddComponent<LLMCharacter>();

            // Awakeが呼ばれる前にLLMを設定（重要！）
            if (_llm != null)
            {
                _testCharacter.llm = _llm;
                Debug.Log("✓ LLM assigned to LLMCharacter before Awake");
            }
            else
            {
                Debug.LogError("LLM is null when creating LLMCharacter");
                throw new Exception("LLM must be created before LLMCharacter");
            }

            // 基本設定
            _testCharacter.playerName = "Player";
            _testCharacter.AIName = "GemmaAI";
            _testCharacter.temperature = 0.8f;
            _testCharacter.topP = 0.9f;
            _testCharacter.numPredict = 100;
            _testCharacter.stream = true;

            Debug.Log("✓ Remote LLMCharacter created and configured");
        }

        /// <summary>
        /// プレハブのRemote設定を確認
        /// </summary>
        private void VerifyRemoteSettings()
        {
            if (_llm == null)
                return;

            Debug.Log("Verifying prefab remote settings...");

            var llmType = typeof(LLM);

            try
            {
                var remoteField = llmType.GetField("remote", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var hostField = llmType.GetField("host", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var portField = llmType.GetField("port", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var modelField = llmType.GetField("model", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                bool isRemote = (bool)(remoteField?.GetValue(_llm) ?? false);
                string host = hostField?.GetValue(_llm)?.ToString() ?? "";
                int port = (int)(portField?.GetValue(_llm) ?? 0);
                string model = modelField?.GetValue(_llm)?.ToString() ?? "";

                Debug.Log($"Prefab settings - Remote: {isRemote}, Host: {host}, Port: {port}, Model: {model}");

                if (!isRemote)
                {
                    Debug.LogWarning("Prefab LLM is not in remote mode. Switching to remote...");
                    SetLLMRemoteConfiguration();
                }
                else
                {
                    Debug.Log("✓ Prefab LLM is correctly configured for remote");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not verify prefab settings: {ex.Message}");
                Debug.LogWarning("Applying remote configuration...");
                SetLLMRemoteConfiguration();
            }
        }

        [Test]
        public async UniTask TestRemoteLLMConnection()
        {
            Debug.Log("Testing remote LLM connection...");

            // Ollamaサーバーの稼働確認
            bool ollamaRunning = await CheckOllamaServer();

            if (!ollamaRunning)
            {
                Assert.Inconclusive(@"Ollama server not running. Please:
1. Install Ollama from https://ollama.ai
2. Run: ollama pull gemma2:2b
3. Run: ollama serve
4. Run test again");
                return;
            }

            Debug.Log("✓ Ollama server is running");
            Assert.Pass("Remote LLM connection test passed");
        }

        [Test]
        public async UniTask TestRemoteLLMResponse()
        {
            if (_testCharacter == null)
            {
                Assert.Fail("LLMCharacter not initialized");
                return;
            }

            // Ollamaサーバーの確認
            bool ollamaRunning = await CheckOllamaServer();
            if (!ollamaRunning)
            {
                Assert.Inconclusive("Ollama server not running");
                return;
            }

            Debug.Log("Starting remote LLM response test...");

            string testPrompt = "Hello! Please respond with a brief greeting.";
            string response = "";
            bool completed = false;

            try
            {
                // Remote LLMとチャット
                await _testCharacter.Chat(testPrompt,
                    callback: (string partialResponse) =>
                    {
                        response = partialResponse;
                        Debug.Log($"Partial response: {partialResponse}");
                    },
                    completionCallback: () =>
                    {
                        completed = true;
                        Debug.Log($"Response completed: {response}");
                    }
                );

                // 最大45秒待機（Remote LLMは時間がかかる）
                float timeout = 45f;
                float elapsed = 0f;

                while (!completed && elapsed < timeout)
                {
                    await UniTask.Delay(100);
                    elapsed += 0.1f;

                    if (elapsed % 10f < 0.1f) // 10秒ごとにログ
                    {
                        Debug.Log($"Waiting for remote response... ({elapsed:F1}s)");
                    }
                }

                if (!completed)
                {
                    Assert.Fail("Remote LLM response timed out after 45 seconds");
                    return;
                }

                // 結果の検証
                Assert.IsTrue(!string.IsNullOrEmpty(response), "Remote LLM response is empty");
                Assert.IsTrue(response.Length > 1, "Remote LLM response too short");

                Debug.Log($"✓ Remote LLM test passed! Response: {response}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Remote LLM test failed: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Ollamaサーバーの稼働確認
        /// </summary>
        private async UniTask<bool> CheckOllamaServer()
        {
            try
            {
                Debug.Log("Checking Ollama server at localhost:11434...");

                using (var www = UnityEngine.Networking.UnityWebRequest.Get("http://localhost:11434/api/tags"))
                {
                    await www.SendWebRequest();

                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        Debug.Log("✓ Ollama server is responding");
                        Debug.Log($"Response: {www.downloadHandler.text}");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"Ollama server check failed: {www.error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to check Ollama server: {ex.Message}");
                return false;
            }
        }

        [Test]
        public void TestLLMRemoteSettings()
        {
            if (_llm == null)
            {
                Assert.Fail("Remote LLM not initialized");
                return;
            }

            Debug.Log("=== Remote LLM Settings ===");

            var llmType = typeof(LLM);

            try
            {
                var remoteField = llmType.GetField("remote", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var hostField = llmType.GetField("host", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var portField = llmType.GetField("port", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var modelField = llmType.GetField("model", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                Debug.Log($"Remote: {remoteField?.GetValue(_llm)}");
                Debug.Log($"Host: {hostField?.GetValue(_llm)}");
                Debug.Log($"Port: {portField?.GetValue(_llm)}");
                Debug.Log($"Model: {modelField?.GetValue(_llm)}");

                // Remote設定の検証
                Assert.IsTrue((bool)(remoteField?.GetValue(_llm) ?? false), "Remote mode should be enabled");
                Assert.AreEqual("localhost", hostField?.GetValue(_llm)?.ToString(), "Host should be localhost");
                Assert.AreEqual(11434, (int)(portField?.GetValue(_llm) ?? 0), "Port should be 11434");
                Assert.AreEqual("gemma2:2b", modelField?.GetValue(_llm)?.ToString(), "Model should be gemma2:2b");

                Debug.Log("✓ Remote LLM settings validation passed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Settings validation failed: {ex.Message}");
                throw;
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