using LLMUnity;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static LLMDataArchitect.ActionTable;
using static LLMDataArchitect.ActionTableEnglish;


namespace LLMDataArchitect.Test
{
    /// <summary>
    /// LLM for Unityを使用した連続的な思考テスト用コンポーネント（最適化版）
    /// </summary>
    public class LLMContinuousThinkingTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private int _testIterations = 5;
        [SerializeField] private float _delayBetweenTests = 2f;
        [SerializeField] private bool _autoStartOnPlay = true;
        [SerializeField] private bool _generateRandomDataEachIteration = true;

        [Header("言語設定")]
        [SerializeField] private bool _isUseEnglish = false;

        [Header("ファイル出力設定")]
        [SerializeField] private string _outputDirectoryPath = "Assets/LLMTestResults";
        [SerializeField] private string _filePrefix = "LLMTest";

        [Header("LLM設定")]
        [SerializeField] private LLMCharacter _llmCharacter;
        [SerializeField] private float _timeoutSeconds = 50f;

        [Header("LLM最適化設定")]
        [SerializeField] private bool _autoConfigureLLM = true;
        [SerializeField] private bool _useGrammar = true;
        [SerializeField] private int _optimalThreads = 6;
        [SerializeField] private int _optimalGpuLayers = 33;
        [SerializeField] private int _optimalContextSize = 2048;
        [SerializeField] private int _optimalBatchSize = 512;

        [Header("戦況設定")]
        [SerializeField] private TestSituationType _situationType = TestSituationType.拮抗;
        [SerializeField] private TestSituationTypeEnglish _situationTypeEnglish = TestSituationTypeEnglish.Even;
        [SerializeField] private bool _useMixedSituations = true;

        [Header("デバッグ表示")]
        [SerializeField] private bool _showProgressInConsole = true;
        [SerializeField] private bool _showDetailedTiming = true;
        [SerializeField] private bool _showPromptSummary = true;
        [SerializeField] private bool _validateResponses = true;
        [SerializeField] private bool _showPerformanceMetrics = true;

        [Header("キャッシング設定")]
        [SerializeField] private bool _enableResponseCache = true;
        [SerializeField] private int _maxCacheSize = 20;

        // 最新版のSystemPromptGenerator
        private SystemPromptGenerator _promptGenerator;

        // テストデータ保持
        private Dictionary<TestSituationType, LLMInputData> _baseTestData;
        private Dictionary<TestSituationTypeEnglish, LLMInputDataEnglish> _baseTestDataEnglish;

        // 共通フィールド
        private List<TestResult> _testResults;
        private bool _isTestRunning;
        private int _currentIteration;
        private Stopwatch _totalStopwatch;
        private StringBuilder _logBuilder;
        private string _currentSessionId;

        // 最適化関連
        private Dictionary<string, CachedResponse> _responseCache;
        private PerformanceMetrics _performanceMetrics;
        private string _currentGrammar;

        // キャッシュ用クラス
        private class CachedResponse
        {
            public string Response;
            public DateTime CachedAt;
            public int HitCount;
        }

        // パフォーマンスメトリクス
        private class PerformanceMetrics
        {
            public double TotalTokensGenerated;
            public double AverageTokensPerSecond;
            public List<double> TokensPerSecondHistory = new List<double>();
            public int CacheHits;
            public int CacheMisses;
            public double CacheHitRate => (CacheHits + CacheMisses) > 0
                ? (double)CacheHits / (CacheHits + CacheMisses)
                : 0;
        }

        // テスト結果格納用クラス
        [System.Serializable]
        private class TestResult
        {
            public int iteration;
            public double responseTimeSeconds;
            public string situationType;
            public string prompt;
            public string systemPrompt;
            public string response;
            public string grammer;
            public string error;
            public DateTime timestamp;
            public bool isSuccessful;
            public bool isValidJson;
            public string calculationSummary;
            public string tacticsType;
            public bool isEnglish;
            public double tokensPerSecond;
            public int responseTokenCount;
            public bool wasCached;

            public TestResult()
            {
                timestamp = DateTime.Now;
            }
        }

        // 統合テスト結果用クラス
        [System.Serializable]
        private class IntegratedTestResults
        {
            public string sessionId;
            public DateTime startTime;
            public DateTime endTime;
            public double totalTimeSeconds;
            public int totalTests;
            public int successfulTests;
            public int failedTests;
            public int validJsonResponses;
            public double successRate;
            public double jsonValidRate;
            public double averageResponseTimeSeconds;
            public double minResponseTimeSeconds;
            public double maxResponseTimeSeconds;
            public double averageTokensPerSecond;
            public int cacheHits;
            public int cacheMisses;
            public double cacheHitRate;
            public List<TestResult> testResults;
            public List<string> errors;
            public Dictionary<string, int> situationTypeCounts;
            public Dictionary<string, int> tacticTypeCounts;
            public bool isEnglishTest;
            public LLMConfiguration llmConfig;

            public IntegratedTestResults()
            {
                testResults = new List<TestResult>();
                errors = new List<string>();
                situationTypeCounts = new Dictionary<string, int>();
                tacticTypeCounts = new Dictionary<string, int>();
            }
        }

        [System.Serializable]
        private class LLMConfiguration
        {
            public int threads;
            public int gpuLayers;
            public int contextSize;
            public int batchSize;
            public string logLevel;
            public bool grammarEnabled;
            public bool cacheEnabled;
        }

        private void Start()
        {
            InitializeTest();

            if (_autoStartOnPlay)
            {
                StartCoroutine(RunContinuousTest());
            }
        }

        private void InitializeTest()
        {
            _testResults = new List<TestResult>();
            _totalStopwatch = new Stopwatch();
            _logBuilder = new StringBuilder();
            _currentSessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _responseCache = new Dictionary<string, CachedResponse>();
            _performanceMetrics = new PerformanceMetrics();

            // 出力ディレクトリの作成
            if (!Directory.Exists(_outputDirectoryPath))
            {
                Directory.CreateDirectory(_outputDirectoryPath);
            }

            // LLMの最適設定を適用
            if (_autoConfigureLLM && _llmCharacter != null)
            {
                ConfigureLLMOptimal();
            }

            // Grammarを設定
            if (_useGrammar && _llmCharacter != null)
            {
                SetupGrammar();
            }

            // システムプロンプトを設定
            if (_isUseEnglish)
            {
                _llmCharacter.prompt = @"You are a tactical combat AI assistant. 
Analyze battle data and provide strategic decisions in strict JSON format.
Always respond with ONLY valid JSON, no markdown, no explanations.";
            }
            else
            {
                _llmCharacter.prompt = @"あなたは戦術的な戦闘AI分析アシスタントです。
戦闘データを分析し、厳密なJSON形式で戦略的判断を提供してください。
常に有効なJSONのみで応答し、マークダウンや説明文は含めないでください。";
            }

            // プレイヤーとAIの名前も設定可能
            _llmCharacter.playerName = "User";
            _llmCharacter.AIName = "TacticAI";

            // 最新版のSystemPromptGeneratorを初期化
            _promptGenerator = new SystemPromptGenerator();

            // テストデータ生成
            GenerateTestData();

            if (_showProgressInConsole)
            {
                string language = _isUseEnglish ? "英語" : "日本語";
                UnityEngine.Debug.Log($"LLM連続思考テスト初期化完了 - セッションID: {_currentSessionId} ({language})");
                UnityEngine.Debug.Log($"LLM設定: Threads={_optimalThreads}, GPU Layers={_optimalGpuLayers}, Context={_optimalContextSize}, Grammar={_useGrammar}");

                if (_isUseEnglish)
                    UnityEngine.Debug.Log($"基本テストデータ生成完了: {_baseTestDataEnglish.Count}種類の戦況");
                else
                    UnityEngine.Debug.Log($"基本テストデータ生成完了: {_baseTestData.Count}種類の戦況");
            }
        }

        /// <summary>
        /// LLM Characterの最適設定を適用
        /// </summary>
        private void ConfigureLLMOptimal()
        {

            // ストリーミング有効化
            _llmCharacter.stream = true;

            // プロンプトキャッシング有効化
            _llmCharacter.cachePrompt = true;

            UnityEngine.Debug.Log("LLM最適設定を適用しました");
        }

        /// <summary>
        /// Grammar設定（JSON Schema版 - 推奨）
        /// </summary>
        private void SetupGrammar()
        {
            if (_isUseEnglish)
            {
                // 英語版JSON Schema
                _llmCharacter.grammarJSONString = @"{
  ""type"": ""object"",
  ""properties"": {
    ""conclusion"": {
      ""type"": ""string""
    },
    ""reasoning"": {
      ""type"": ""string""
    },
    ""basic_tactics"": {
      ""type"": ""string"",
      ""enum"": [""defensive"", ""offensive"", ""adaptive"", ""disruptive""]
    },
    ""action_table"": {
      ""type"": ""object"",
      ""properties"": {
        ""enemy_attack_stance"": {
          ""type"": ""string"",
          ""enum"": [""guard"", ""backward_dodge"", ""side_dodge_attack"", ""side_dodge"", ""heavy_attack_parry"", ""light_attack_parry"", ""light_attack""]
        },
        ""enemy_waiting"": {
          ""type"": ""string"",
          ""enum"": [""light_attack"", ""heavy_attack"", ""heavy_attack_cancel"", ""light_attack_parry"", ""forward_dodge"", ""guard""]
        },
        ""slight_advantage"": {
          ""type"": ""string"",
          ""enum"": [""light_attack"", ""heavy_attack"", ""heavy_attack_cancel"", ""light_attack_parry"", ""forward_dodge"", ""guard""]
        },
        ""advantage"": {
          ""type"": ""string"",
          ""enum"": [""light_attack"", ""heavy_attack"", ""heavy_attack_cancel"", ""light_attack_parry"", ""forward_dodge"", ""guard""]
        },
        ""slight_disadvantage"": {
          ""type"": ""string"",
          ""enum"": [""guard"", ""backward_dodge"", ""side_dodge_attack"", ""side_dodge"", ""heavy_attack_parry"", ""light_attack_parry"", ""light_attack""]
        },
        ""disadvantage"": {
          ""type"": ""string"",
          ""enum"": [""guard"", ""backward_dodge"", ""side_dodge_attack"", ""side_dodge"", ""heavy_attack_parry"", ""light_attack_parry"", ""light_attack""]
        },
        ""strong_attack_hit"": {
          ""type"": ""string"",
          ""enum"": [""light_attack"", ""heavy_attack"", ""heavy_attack_cancel"", ""light_attack_parry"", ""forward_dodge"", ""guard""]
        },
        ""enemy_strong_attack_hit"": {
          ""type"": ""string"",
          ""enum"": [""guard"", ""backward_dodge"", ""side_dodge_attack"", ""side_dodge"", ""heavy_attack_parry"", ""light_attack_parry"", ""light_attack""]
        }
      },
      ""required"": [""enemy_attack_stance"", ""enemy_waiting"", ""slight_advantage"", ""advantage"", ""slight_disadvantage"", ""disadvantage"", ""strong_attack_hit"", ""enemy_strong_attack_hit""]
    }
  },
  ""required"": [""conclusion"", ""reasoning"", ""basic_tactics"", ""action_table""]
}";
            }
            else
            {
                // 日本語版JSON Schema
                _llmCharacter.grammarJSONString = @"{
  ""type"": ""object"",
  ""properties"": {
    ""結論"": {
      ""type"": ""string""
    },
    ""理由"": {
      ""type"": ""string""
    },
    ""基本戦術"": {
      ""type"": ""string"",
      ""enum"": [""防御型"", ""攻撃型"", ""対応型"", ""攪乱型""]
    },
    ""行動テーブル"": {
      ""type"": ""object"",
      ""properties"": {
        ""敵攻撃体勢"": {
          ""type"": ""string"",
          ""enum"": [""ガード"", ""後ろ回避"", ""横回避攻撃"", ""横回避"", ""強攻撃ブロッキング"", ""弱攻撃ブロッキング"", ""弱攻撃""]
        },
        ""敵待機状態"": {
          ""type"": ""string"",
          ""enum"": [""弱攻撃"", ""強攻撃"", ""強攻撃キャンセル"", ""弱攻撃ブロッキング"", ""前回避"", ""ガード""]
        },
        ""自分微有利状況"": {
          ""type"": ""string"",
          ""enum"": [""弱攻撃"", ""強攻撃"", ""強攻撃キャンセル"", ""弱攻撃ブロッキング"", ""前回避"", ""ガード""]
        },
        ""自分有利状況"": {
          ""type"": ""string"",
          ""enum"": [""弱攻撃"", ""強攻撃"", ""強攻撃キャンセル"", ""弱攻撃ブロッキング"", ""前回避"", ""ガード""]
        },
        ""自分微不利状況"": {
          ""type"": ""string"",
          ""enum"": [""ガード"", ""後ろ回避"", ""横回避攻撃"", ""横回避"", ""強攻撃ブロッキング"", ""弱攻撃ブロッキング"", ""弱攻撃""]
        },
        ""自分不利状況"": {
          ""type"": ""string"",
          ""enum"": [""ガード"", ""後ろ回避"", ""横回避攻撃"", ""横回避"", ""強攻撃ブロッキング"", ""弱攻撃ブロッキング"", ""弱攻撃""]
        },
        ""自分強攻撃ヒット"": {
          ""type"": ""string"",
          ""enum"": [""弱攻撃"", ""強攻撃"", ""強攻撃キャンセル"", ""弱攻撃ブロッキング"", ""前回避"", ""ガード""]
        },
        ""敵強攻撃ヒット"": {
          ""type"": ""string"",
          ""enum"": [""ガード"", ""後ろ回避"", ""横回避攻撃"", ""横回避"", ""強攻撃ブロッキング"", ""弱攻撃ブロッキング"", ""弱攻撃""]
        }
      },
      ""required"": [""敵攻撃体勢"", ""敵待機状態"", ""自分微有利状況"", ""自分有利状況"", ""自分微不利状況"", ""自分不利状況"", ""自分強攻撃ヒット"", ""敵強攻撃ヒット""]
    }
  },
  ""required"": [""結論"", ""理由"", ""基本戦術"", ""行動テーブル""]
}";
            }

            UnityEngine.Debug.Log($"JSON Schema Grammar設定完了 ({(_isUseEnglish ? "English" : "Japanese")})");
            UnityEngine.Debug.Log(_llmCharacter.grammarJSONString);
        }

        private void GenerateTestData()
        {
            if (_isUseEnglish)
            {
                _baseTestDataEnglish = new Dictionary<TestSituationTypeEnglish, LLMInputDataEnglish>();
                foreach (TestSituationTypeEnglish situationType in Enum.GetValues(typeof(TestSituationTypeEnglish)))
                {
                    _baseTestDataEnglish[situationType] = LLMInputDataEnglish.CreateForTestSituation(situationType);
                }
            }
            else
            {
                _baseTestData = new Dictionary<TestSituationType, LLMInputData>();
                foreach (TestSituationType situationType in Enum.GetValues(typeof(TestSituationType)))
                {
                    _baseTestData[situationType] = LLMInputData.CreateForTestSituation(situationType);
                }
            }
        }

        [ContextMenu("テスト開始")]
        public void StartTest()
        {
            if (!_isTestRunning)
            {
                StartCoroutine(RunContinuousTest());
            }
            else
            {
                UnityEngine.Debug.LogWarning("テストは既に実行中です");
            }
        }

        [ContextMenu("テスト停止")]
        public void StopTest()
        {
            _isTestRunning = false;
            SaveIntegratedResults();
        }

        [ContextMenu("サンプルプロンプト表示")]
        public void ShowSamplePrompt()
        {
            if (_promptGenerator == null)
            {
                _promptGenerator = new SystemPromptGenerator();
            }

            if (_isUseEnglish)
            {
                var sampleData = LLMInputDataEnglish.CreateForTestSituation(_situationTypeEnglish);
                var samplePrompt = _promptGenerator.GenerateFullPromptEnglish(sampleData);
                UnityEngine.Debug.Log($"Sample Prompt ({_situationTypeEnglish}):\n{samplePrompt}");
            }
            else
            {
                var sampleData = LLMInputData.CreateForTestSituation(_situationType);
                var samplePrompt = _promptGenerator.GenerateFullPromptJapanese(sampleData);
                UnityEngine.Debug.Log($"サンプルプロンプト ({_situationType}):\n{samplePrompt}");
            }
        }

        [ContextMenu("LLM設定を表示")]
        public void ShowLLMConfiguration()
        {
            if (_llmCharacter == null)
            {
                UnityEngine.Debug.LogWarning("LLMCharacterが設定されていません");
                return;
            }

            var config = new StringBuilder();
            config.AppendLine("=== LLM Configuration ===");
            config.AppendLine($"Stream: {_llmCharacter.stream}");
            config.AppendLine($"Cache Prompt: {_llmCharacter.cachePrompt}");
            config.AppendLine($"Grammar Enabled: {!string.IsNullOrEmpty(_llmCharacter.grammar)}");
            UnityEngine.Debug.Log(config.ToString());
        }

        [ContextMenu("キャッシュをクリア")]
        public void ClearCache()
        {
            _responseCache?.Clear();
            UnityEngine.Debug.Log("レスポンスキャッシュをクリアしました");
        }

        private IEnumerator RunContinuousTest()
        {
            _isTestRunning = true;
            _totalStopwatch.Start();
            _currentIteration = 0;

            if (_showProgressInConsole)
            {
                string language = _isUseEnglish ? "English" : "日本語";
                UnityEngine.Debug.Log($"連続思考テスト開始 - {_testIterations}回実行予定 ({language})");
            }

            for (int i = 0; i < _testIterations && _isTestRunning; i++)
            {
                _currentIteration = i + 1;

                if (_showProgressInConsole)
                {
                    UnityEngine.Debug.Log($"テスト {_currentIteration}/{_testIterations} 開始");
                }

                // 新しいデータを生成するか既存データを使用するかの判定
                if (_generateRandomDataEachIteration && i > 0)
                {
                    GenerateTestData();
                }

                yield return StartCoroutine(ExecuteSingleTest(i));

                if (i < _testIterations - 1)
                {
                    yield return new WaitForSeconds(_delayBetweenTests);
                }
            }

            _totalStopwatch.Stop();
            _isTestRunning = false;

            if (_showProgressInConsole)
            {
                UnityEngine.Debug.Log("連続思考テスト完了");
            }

            SaveIntegratedResults();
        }

        private IEnumerator ExecuteSingleTest(int iteration)
        {
            var testResult = new TestResult
            {
                iteration = iteration + 1,
                isEnglish = _isUseEnglish
            };

            var stopwatch = Stopwatch.StartNew();

            string fullPrompt;
            string calculationSummary;
            string tacticsType;
            string situationType;
            string userPrompt;

            if (_isUseEnglish)
            {
                var inputData = GetTestDataForIterationEnglish(iteration);
                userPrompt = _promptGenerator.GenerateDynamicSectionEnglish(inputData);
                string fixedSection = _promptGenerator.GenerateFixedSectionEnglish();
                fullPrompt = userPrompt + fixedSection;

                var analysis = BattleAnalysisResultEnglish.AnalyzeFromInputData(inputData);
                calculationSummary = analysis.CalculationSummary;
                tacticsType = analysis.TacticType;
                situationType = GetCurrentSituationTypeEnglish(iteration).ToString();

                testResult.systemPrompt = fixedSection;
            }
            else
            {
                var inputData = GetTestDataForIteration(iteration);
                userPrompt = _promptGenerator.GenerateDynamicSectionJapanese(inputData);
                string fixedSection = _promptGenerator.GenerateFixedSectionJapanese();
                fullPrompt = fixedSection + userPrompt;

                var analysis = BattleAnalysisResult.AnalyzeFromInputData(inputData);
                calculationSummary = analysis.CalculationSummary;
                tacticsType = analysis.TacticType;
                situationType = GetCurrentSituationType(iteration).ToString();

                testResult.systemPrompt = fixedSection;
            }

            testResult.prompt = userPrompt;
            testResult.calculationSummary = calculationSummary;
            testResult.tacticsType = tacticsType;
            testResult.situationType = situationType;

            //_llmCharacter.prompt = testResult.systemPrompt;

            if (_showDetailedTiming)
            {
                UnityEngine.Debug.Log($"プロンプト生成完了: {fullPrompt.Length}文字 {EstimateTokenCount(fullPrompt)}トークン");
            }

            if (_showPromptSummary)
            {
                UnityEngine.Debug.Log($"計算結果: {calculationSummary}, 戦術: {tacticsType}");
            }

            // LLMに完全なプロンプトを送信
            yield return StartCoroutine(SendToLLM(fullPrompt, testResult));
            _performanceMetrics.CacheMisses++;

            // 応答の検証
            if (_validateResponses && !string.IsNullOrEmpty(testResult.response))
            {
                ValidateResponse(testResult);
            }

            stopwatch.Stop();
            testResult.responseTimeSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
            testResult.isSuccessful = !string.IsNullOrEmpty(testResult.response);

            // トークン数と速度を計算
            if (testResult.isSuccessful)
            {
                testResult.responseTokenCount = EstimateTokenCount(testResult.response);
                testResult.tokensPerSecond = testResult.wasCached
                    ? 0 // キャッシュヒットの場合はトークン生成なし
                    : testResult.responseTokenCount / testResult.responseTimeSeconds;

                if (!testResult.wasCached)
                {
                    _performanceMetrics.TokensPerSecondHistory.Add(testResult.tokensPerSecond);
                    _performanceMetrics.TotalTokensGenerated += testResult.responseTokenCount;
                }
            }

            if (_showDetailedTiming)
            {
                string cacheStatus = testResult.wasCached ? " (キャッシュ)" : "";
                UnityEngine.Debug.Log($"テスト {testResult.iteration} 完了: {testResult.responseTimeSeconds:F2}秒{cacheStatus}");

                if (_showPerformanceMetrics && !testResult.wasCached)
                {
                    UnityEngine.Debug.Log($"  トークン数: {testResult.responseTokenCount}, 速度: {testResult.tokensPerSecond:F1} tokens/秒");
                }
            }

            _testResults.Add(testResult);
        }

        private string GetCacheKey(string prompt)
        {
            // プロンプトのハッシュをキーとして使用
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(prompt));
                return Convert.ToBase64String(hash);
            }
        }

        private void SaveToCache(string key, string response)
        {
            // キャッシュサイズ制限
            if (_responseCache.Count >= _maxCacheSize)
            {
                // 最も古いエントリを削除
                var oldest = _responseCache.OrderBy(kvp => kvp.Value.CachedAt).First();
                _responseCache.Remove(oldest.Key);
            }

            _responseCache[key] = new CachedResponse
            {
                Response = response,
                CachedAt = DateTime.Now,
                HitCount = 0
            };
        }

        private int EstimateTokenCount(string text)
        {
            // 簡易的なトークン数推定（英語: 単語数 × 1.3、日本語: 文字数 / 2）
            if (_isUseEnglish)
            {
                return (int)(text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length * 1.3);
            }
            else
            {
                return text.Length / 2;
            }
        }

        private IEnumerator SendToLLM(string prompt, TestResult testResult)
        {
            if (_llmCharacter == null)
            {
                testResult.error = "LLMCharacterが設定されていません";
                yield break;
            }

            bool responseReceived = false;
            string response = "";

            // 最もシンプルで互換性の高い形式
            // 完全な応答を受け取るコールバックのみ使用
            _llmCharacter.Chat(prompt, (receivedResponse) =>
            {
                response = receivedResponse;
                responseReceived = true;
            });

            // タイムアウト付きで応答を待機
            float elapsedTime = 0f;
            while (!responseReceived && elapsedTime < _timeoutSeconds)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (!responseReceived)
            {
                testResult.error = $"タイムアウト ({_timeoutSeconds}秒)";
            }
            else
            {
                testResult.response = response;
            }
            UnityEngine.Debug.Log($"LLMプロンプト: {prompt}");

            UnityEngine.Debug.Log($"LLM応答: {response}");

        }

        private void ValidateResponse(TestResult testResult)
        {
            try
            {
                // JSONとして正しくパースできるかチェック
                var parsedResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(testResult.response);
                testResult.isValidJson = true;

                // 必要なフィールドが含まれているかチェック
                bool hasRequiredFields;

                if (_isUseEnglish)
                {
                    hasRequiredFields =
                        parsedResponse.ContainsKey("conclusion") &&
                        parsedResponse.ContainsKey("reasoning") &&
                        parsedResponse.ContainsKey("basic_tactics") &&
                        parsedResponse.ContainsKey("action_table");
                }
                else
                {
                    hasRequiredFields =
                        parsedResponse.ContainsKey("結論") &&
parsedResponse.ContainsKey("理由") &&
                        parsedResponse.ContainsKey("基本戦術") &&
                        parsedResponse.ContainsKey("行動テーブル");
                }

                if (!hasRequiredFields)
                {
                    testResult.error = "必要なフィールドが不足しています";
                }

                if (_showProgressInConsole)
                {
                    UnityEngine.Debug.Log($"応答検証: JSON形式={testResult.isValidJson}, 必須フィールド={hasRequiredFields}");
                }
            }
            catch (JsonException jsonEx)
            {
                testResult.isValidJson = false;
                testResult.error = $"JSON解析エラー: {jsonEx.Message}";
            }
            catch (Exception ex)
            {
                testResult.isValidJson = false;
                testResult.error = $"応答検証エラー: {ex.Message}";
            }
        }

        private LLMInputData GetTestDataForIteration(int iteration)
        {
            if (_useMixedSituations)
            {
                var situations = new List<TestSituationType>(_baseTestData.Keys);
                var situationType = situations[iteration % situations.Count];
                return _baseTestData[situationType];
            }
            else
            {
                return _baseTestData[_situationType];
            }
        }

        private LLMInputDataEnglish GetTestDataForIterationEnglish(int iteration)
        {
            if (_useMixedSituations)
            {
                var situations = new List<TestSituationTypeEnglish>(_baseTestDataEnglish.Keys);
                var situationType = situations[iteration % situations.Count];
                return _baseTestDataEnglish[situationType];
            }
            else
            {
                return _baseTestDataEnglish[_situationTypeEnglish];
            }
        }

        private TestSituationType GetCurrentSituationType(int iteration)
        {
            if (_useMixedSituations)
            {
                var situations = Enum.GetValues(typeof(TestSituationType));
                return (TestSituationType)situations.GetValue(iteration % situations.Length);
            }
            else
            {
                return _situationType;
            }
        }

        private TestSituationTypeEnglish GetCurrentSituationTypeEnglish(int iteration)
        {
            if (_useMixedSituations)
            {
                var situations = Enum.GetValues(typeof(TestSituationTypeEnglish));
                return (TestSituationTypeEnglish)situations.GetValue(iteration % situations.Length);
            }
            else
            {
                return _situationTypeEnglish;
            }
        }

        private void SaveIntegratedResults()
        {
            try
            {
                var integratedResults = CreateIntegratedResults();

                // ファイル名に言語識別子を追加
                string languageTag = _isUseEnglish ? "EN" : "JP";

                // JSONファイルとして保存
                string jsonFileName = $"{_filePrefix}_{languageTag}_AllResults_{_currentSessionId}.json";
                string jsonFilePath = Path.Combine(_outputDirectoryPath, jsonFileName);
                string json = JsonConvert.SerializeObject(integratedResults, Formatting.Indented);
                File.WriteAllText(jsonFilePath, json, Encoding.UTF8);

                // 人間が読みやすいテキスト形式でも保存
                string txtFileName = $"{_filePrefix}_{languageTag}_Report_{_currentSessionId}.txt";
                string txtFilePath = Path.Combine(_outputDirectoryPath, txtFileName);
                string textReport = CreateReadableReport(integratedResults);
                File.WriteAllText(txtFilePath, textReport, Encoding.UTF8);

                // CSV形式での統計データ保存
                string csvFileName = $"{_filePrefix}_{languageTag}_Stats_{_currentSessionId}.csv";
                string csvFilePath = Path.Combine(_outputDirectoryPath, csvFileName);
                string csvData = CreateCsvReport(integratedResults);
                File.WriteAllText(csvFilePath, csvData, Encoding.UTF8);

                if (_showProgressInConsole)
                {
                    UnityEngine.Debug.Log($"テスト結果を保存しました:\n- JSON: {jsonFileName}\n- レポート: {txtFileName}\n- CSV: {csvFileName}");
                }

                // コンソールにサマリー表示
                DisplaySummaryInConsole(integratedResults);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"テスト結果の保存に失敗: {ex.Message}");
            }
        }

        private IntegratedTestResults CreateIntegratedResults()
        {
            var results = new IntegratedTestResults
            {
                sessionId = _currentSessionId,
                startTime = DateTime.Now.AddMilliseconds(-_totalStopwatch.ElapsedMilliseconds),
                endTime = DateTime.Now,
                totalTimeSeconds = _totalStopwatch.ElapsedMilliseconds / 1000.0,
                totalTests = _testResults.Count,
                testResults = _testResults,
                isEnglishTest = _isUseEnglish,
                cacheHits = _performanceMetrics.CacheHits,
                cacheMisses = _performanceMetrics.CacheMisses,
                cacheHitRate = _performanceMetrics.CacheHitRate,
                llmConfig = new LLMConfiguration
                {
                    threads = _optimalThreads,
                    gpuLayers = _optimalGpuLayers,
                    contextSize = _optimalContextSize,
                    batchSize = _optimalBatchSize,
                    grammarEnabled = _useGrammar,
                    cacheEnabled = _enableResponseCache
                }
            };

            // 成功/失敗の集計
            foreach (var result in _testResults)
            {
                if (result.isSuccessful)
                {
                    results.successfulTests++;
                }
                else
                {
                    results.failedTests++;
                    if (!string.IsNullOrEmpty(result.error))
                    {
                        results.errors.Add($"Test{result.iteration}: {result.error}");
                    }
                }

                if (result.isValidJson)
                {
                    results.validJsonResponses++;
                }

                // 戦況タイプの集計
                if (!results.situationTypeCounts.ContainsKey(result.situationType))
                    results.situationTypeCounts[result.situationType] = 0;
                results.situationTypeCounts[result.situationType]++;

                // 戦術タイプの集計
                if (!string.IsNullOrEmpty(result.tacticsType))
                {
                    if (!results.tacticTypeCounts.ContainsKey(result.tacticsType))
                        results.tacticTypeCounts[result.tacticsType] = 0;
                    results.tacticTypeCounts[result.tacticsType]++;
                }
            }

            // 成功率とJSON有効率の計算
            if (results.totalTests > 0)
            {
                results.successRate = (double)results.successfulTests / results.totalTests;
                results.jsonValidRate = (double)results.validJsonResponses / results.totalTests;
            }

            // 応答時間の統計計算
            var successfulResults = _testResults.FindAll(r => r.isSuccessful);
            if (successfulResults.Count > 0)
            {
                var responseTimes = successfulResults.ConvertAll(r => r.responseTimeSeconds);
                results.averageResponseTimeSeconds = responseTimes.Average();
                results.minResponseTimeSeconds = responseTimes.Min();
                results.maxResponseTimeSeconds = responseTimes.Max();
            }

            // トークン/秒の平均計算
            if (_performanceMetrics.TokensPerSecondHistory.Count > 0)
            {
                results.averageTokensPerSecond = _performanceMetrics.TokensPerSecondHistory.Average();
            }

            return results;
        }

        private string CreateReadableReport(IntegratedTestResults results)
        {
            var report = new StringBuilder();
            string title = results.isEnglishTest ? "LLM Continuous Thinking Test Report (Optimized)" : "LLM連続思考テスト詳細レポート（最適化版）";

            report.AppendLine("================================================================================");
            report.AppendLine($"                        {title}");
            report.AppendLine("================================================================================");
            report.AppendLine();
            report.AppendLine($"セッションID: {results.sessionId}");
            report.AppendLine($"言語: {(results.isEnglishTest ? "English" : "日本語")}");
            report.AppendLine($"開始時刻: {results.startTime:yyyy/MM/dd HH:mm:ss}");
            report.AppendLine($"終了時刻: {results.endTime:yyyy/MM/dd HH:mm:ss}");
            report.AppendLine($"総実行時間: {results.totalTimeSeconds:F2}秒");
            report.AppendLine();

            // LLM設定情報
            report.AppendLine("【LLM設定】");
            report.AppendLine($"Threads: {results.llmConfig.threads}");
            report.AppendLine($"GPU Layers: {results.llmConfig.gpuLayers}");
            report.AppendLine($"Context Size: {results.llmConfig.contextSize}");
            report.AppendLine($"Batch Size: {results.llmConfig.batchSize}");
            report.AppendLine($"Log Level: {results.llmConfig.logLevel}");
            report.AppendLine($"Grammar: {(results.llmConfig.grammarEnabled ? "有効" : "無効")}");
            report.AppendLine($"Response Cache: {(results.llmConfig.cacheEnabled ? "有効" : "無効")}");
            report.AppendLine();

            report.AppendLine("【テスト結果サマリー】");
            report.AppendLine($"総テスト数: {results.totalTests}");
            report.AppendLine($"成功: {results.successfulTests}");
            report.AppendLine($"失敗: {results.failedTests}");
            report.AppendLine($"成功率: {results.successRate:P1}");
            report.AppendLine($"有効なJSON応答: {results.validJsonResponses}");
            report.AppendLine($"JSON有効率: {results.jsonValidRate:P1}");
            report.AppendLine();

            if (results.successfulTests > 0)
            {
                report.AppendLine("【応答時間統計】");
                report.AppendLine($"平均応答時間: {results.averageResponseTimeSeconds:F2}秒");
                report.AppendLine($"最短応答時間: {results.minResponseTimeSeconds:F2}秒");
                report.AppendLine($"最長応答時間: {results.maxResponseTimeSeconds:F2}秒");
                report.AppendLine();
            }

            // パフォーマンスメトリクス
            if (results.averageTokensPerSecond > 0)
            {
                report.AppendLine("【パフォーマンスメトリクス】");
                report.AppendLine($"平均生成速度: {results.averageTokensPerSecond:F1} tokens/秒");
                report.AppendLine($"キャッシュヒット数: {results.cacheHits}");
                report.AppendLine($"キャッシュミス数: {results.cacheMisses}");
                report.AppendLine($"キャッシュヒット率: {results.cacheHitRate:P1}");
                report.AppendLine();
            }

            // 戦況タイプ分布
            if (results.situationTypeCounts.Count > 0)
            {
                report.AppendLine("【戦況タイプ分布】");
                foreach (var kvp in results.situationTypeCounts)
                {
                    report.AppendLine($"  {kvp.Key}: {kvp.Value}回");
                }
                report.AppendLine();
            }

            // 戦術タイプ分布
            if (results.tacticTypeCounts.Count > 0)
            {
                report.AppendLine("【戦術タイプ分布】");
                foreach (var kvp in results.tacticTypeCounts)
                {
                    report.AppendLine($"  {kvp.Key}: {kvp.Value}回");
                }
                report.AppendLine();
            }

            if (results.errors.Count > 0)
            {
                report.AppendLine("【エラー詳細】");
                foreach (var error in results.errors)
                {
                    report.AppendLine($"  - {error}");
                }
                report.AppendLine();
            }

            report.AppendLine("【個別テスト結果】");
            report.AppendLine("--------------------------------------------------------------------------------");
            foreach (var result in results.testResults)
            {
                report.AppendLine($"■ テスト {result.iteration} ({(result.isEnglish ? "EN" : "JP")})");
                report.AppendLine($"  時刻: {result.timestamp:HH:mm:ss}");
                report.AppendLine($"  状況: {result.situationType}");
                report.AppendLine($"  計算結果: {result.calculationSummary}");
                report.AppendLine($"  戦術: {result.tacticsType}");
                report.AppendLine($"  結果: {(result.isSuccessful ? "成功" : "失敗")}");
                report.AppendLine($"  JSON有効: {(result.isValidJson ? "有効" : "無効")}");
                report.AppendLine($"  応答時間: {result.responseTimeSeconds:F2}秒");
                report.AppendLine($"  キャッシュ: {(result.wasCached ? "ヒット" : "ミス")}");

                if (!result.wasCached && result.tokensPerSecond > 0)
                {
                    report.AppendLine($"  生成速度: {result.tokensPerSecond:F1} tokens/秒");
                    report.AppendLine($"  トークン数: {result.responseTokenCount}");
                }

                if (!result.isSuccessful && !string.IsNullOrEmpty(result.error))
                {
                    report.AppendLine($"  エラー: {result.error}");
                }

                if (!string.IsNullOrEmpty(result.prompt))
                {
                    report.AppendLine($"  プロンプト文字数: {result.prompt.Length}文字");
                }

                if (!string.IsNullOrEmpty(result.response))
                {
                    report.AppendLine($"  レスポンス文字数: {result.response.Length}文字");

                    // 応答の最初の100文字をサンプルとして表示
                    var sampleResponse = result.response;
                    report.AppendLine($"  レスポンス: {sampleResponse}");
                }

                report.AppendLine("--------------------------------------------------------------------------------");
            }

            return report.ToString();
        }

        private string CreateCsvReport(IntegratedTestResults results)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Iteration,Timestamp,SituationType,TacticsType,CalculationSummary,IsSuccessful,IsValidJson,ResponseTimeSeconds,TokensPerSecond,TokenCount,WasCached,PromptLength,ResponseLength,IsEnglish,Error");

            foreach (var result in results.testResults)
            {
                csv.AppendLine($"{result.iteration}," +
                              $"{result.timestamp:yyyy-MM-dd HH:mm:ss}," +
                              $"{result.situationType}," +
                              $"{result.tacticsType ?? ""}," +
                              $"\"{result.calculationSummary ?? ""}\"," +
                              $"{result.isSuccessful}," +
                              $"{result.isValidJson}," +
                              $"{result.responseTimeSeconds:F2}," +
                              $"{result.tokensPerSecond:F1}," +
                              $"{result.responseTokenCount}," +
                              $"{result.wasCached}," +
                              $"{result.prompt?.Length ?? 0}," +
                              $"{result.response?.Length ?? 0}," +
                              $"{result.isEnglish}," +
                              $"\"{result.error ?? ""}\"");
            }

            return csv.ToString();
        }

        private void DisplaySummaryInConsole(IntegratedTestResults results)
        {
            var summary = new StringBuilder();
            string languageTag = results.isEnglishTest ? "(English)" : "(日本語)";

            summary.AppendLine($"=== LLMテスト完了（最適化版） {languageTag} ===");
            summary.AppendLine($"総テスト数: {results.totalTests}");
            summary.AppendLine($"成功: {results.successfulTests} / 失敗: {results.failedTests}");
            summary.AppendLine($"成功率: {results.successRate:P1}");
            summary.AppendLine($"JSON有効率: {results.jsonValidRate:P1}");

            if (results.successfulTests > 0)
            {
                summary.AppendLine($"平均応答時間: {results.averageResponseTimeSeconds:F2}秒");
                summary.AppendLine($"最短/最長: {results.minResponseTimeSeconds:F2}秒 / {results.maxResponseTimeSeconds:F2}秒");
            }

            if (results.averageTokensPerSecond > 0)
            {
                summary.AppendLine($"平均生成速度: {results.averageTokensPerSecond:F1} tokens/秒");
            }

            if (results.cacheHits + results.cacheMisses > 0)
            {
                summary.AppendLine($"キャッシュ効率: {results.cacheHitRate:P1} ({results.cacheHits}ヒット / {results.cacheMisses}ミス)");
            }

            summary.AppendLine($"総実行時間: {results.totalTimeSeconds:F2}秒");

            summary.AppendLine();
            summary.AppendLine("=== LLM設定 ===");
            summary.AppendLine($"Threads: {results.llmConfig.threads}, GPU Layers: {results.llmConfig.gpuLayers}");
            summary.AppendLine($"Context: {results.llmConfig.contextSize}, Batch: {results.llmConfig.batchSize}");
            summary.AppendLine($"Grammar: {results.llmConfig.grammarEnabled}, Cache: {results.llmConfig.cacheEnabled}");

            UnityEngine.Debug.Log(summary.ToString());
        }

        private void OnDestroy()
        {
            if (_isTestRunning)
            {
                StopTest();
            }
        }

        #region ユーティリティメソッド

        /// <summary>
        /// パフォーマンスメトリクスをリセット
        /// </summary>
        [ContextMenu("パフォーマンスメトリクスをリセット")]
        public void ResetPerformanceMetrics()
        {
            _performanceMetrics = new PerformanceMetrics();
            UnityEngine.Debug.Log("パフォーマンスメトリクスをリセットしました");
        }

        /// <summary>
        /// 現在のパフォーマンスメトリクスを表示
        /// </summary>
        [ContextMenu("パフォーマンスメトリクスを表示")]
        public void ShowPerformanceMetrics()
        {
            var metrics = new StringBuilder();
            metrics.AppendLine("=== パフォーマンスメトリクス ===");
            metrics.AppendLine($"総トークン生成数: {_performanceMetrics.TotalTokensGenerated:F0}");

            if (_performanceMetrics.TokensPerSecondHistory.Count > 0)
            {
                metrics.AppendLine($"平均生成速度: {_performanceMetrics.TokensPerSecondHistory.Average():F1} tokens/秒");
                metrics.AppendLine($"最速: {_performanceMetrics.TokensPerSecondHistory.Max():F1} tokens/秒");
                metrics.AppendLine($"最遅: {_performanceMetrics.TokensPerSecondHistory.Min():F1} tokens/秒");
            }

            metrics.AppendLine($"キャッシュヒット: {_performanceMetrics.CacheHits}");
            metrics.AppendLine($"キャッシュミス: {_performanceMetrics.CacheMisses}");
            metrics.AppendLine($"キャッシュヒット率: {_performanceMetrics.CacheHitRate:P1}");

            UnityEngine.Debug.Log(metrics.ToString());
        }

        /// <summary>
        /// Grammarの有効/無効を切り替え
        /// </summary>
        [ContextMenu("Grammarを切り替え")]
        public void ToggleGrammar()
        {
            _useGrammar = !_useGrammar;

            if (_llmCharacter != null)
            {
                if (_useGrammar)
                {
                    SetupGrammar();
                    UnityEngine.Debug.Log("Grammarを有効化しました");
                }
                else
                {
                    _llmCharacter.grammar = "";
                    UnityEngine.Debug.Log("Grammarを無効化しました");
                }
            }
        }

        /// <summary>
        /// LLM設定を最適化プリセットに変更
        /// </summary>
        [ContextMenu("LLM設定を最適化")]
        public void OptimizeLLMSettings()
        {
            if (_llmCharacter != null)
            {
                ConfigureLLMOptimal();
                UnityEngine.Debug.Log("LLM設定を最適化しました");
            }
            else
            {
                UnityEngine.Debug.LogWarning("LLMCharacterが設定されていません");
            }
        }

        #endregion
    }
}