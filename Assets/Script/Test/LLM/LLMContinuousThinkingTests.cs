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

namespace LLMDataArchitect.Test
{
    /// <summary>
    /// プロンプト生成タイプ
    /// </summary>
    public enum PromptGeneratorType
    {
        Japanese,
        English,
        Experimental
    }

    /// <summary>
    /// LLM for Unityを使用した連続的な思考テスト用コンポーネント（RAG統合版）
    /// </summary>
    public class LLMContinuousThinkingTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private int _testIterations = 5;
        [SerializeField] private float _delayBetweenTests = 2f;
        [SerializeField] private bool _autoStartOnPlay = true;
        [SerializeField] private bool _generateRandomDataEachIteration = true;

        [Header("プロンプト生成設定")]
        [SerializeField] private PromptGeneratorType _generatorType = PromptGeneratorType.Japanese;

        [Header("ファイル出力設定")]
        [SerializeField] private string _outputDirectoryPath = "Assets/LLMTestResults";
        [SerializeField] private string _filePrefix = "LLMTest";

        [Header("LLM設定")]
        [SerializeField] private LLMCharacter _llmCharacter;
        [SerializeField] private float _timeoutSeconds = 50f;

        [Header("RAG設定")]
        [SerializeField] private RAG _rag;
        [SerializeField] private bool _useRAG = true;
        [SerializeField] private int _ragTopK = 3;
        [SerializeField] private float _ragDistanceThreshold = 0.8f;
        [SerializeField] private bool _showRAGResults = true;

        [Header("LLM最適化設定")]
        [SerializeField] private bool _autoConfigureLLM = true;
        [SerializeField] private bool _useGrammar = true;

        [Header("戦況設定")]
        [SerializeField] private TestSituationType _situationType = TestSituationType.拮抗;
        [SerializeField] private bool _useMixedSituations = true;

        [Header("デバッグ表示")]
        [SerializeField] private bool _showProgressInConsole = true;
        [SerializeField] private bool _showDetailedTiming = true;
        [SerializeField] private bool _showPromptSummary = true;
        [SerializeField] private bool _validateResponses = true;
        [SerializeField] private bool _showPerformanceMetrics = true;

        // プロンプト生成インターフェイス
        private PromptGeneratorBase _promptGenerator;

        // テストデータ保持
        private Dictionary<TestSituationType, LLMInputData> _baseTestData;

        // 共通フィールド
        private List<TestResult> _testResults;
        private bool _isTestRunning;
        private int _currentIteration;
        private Stopwatch _totalStopwatch;
        private StringBuilder _logBuilder;
        private string _currentSessionId;

        // パフォーマンスメトリクス
        private PerformanceMetrics _performanceMetrics;

        // 現在のテストデータ（LastStrategyを更新していく）
        private LLMInputData _currentTestData;

        // RAG統計
        private RAGStatistics _ragStatistics;

        // パフォーマンスメトリクス
        private class PerformanceMetrics
        {
            public double TotalTokensGenerated;
            public List<double> TokensPerSecondHistory = new List<double>();
        }

        // RAG統計
        private class RAGStatistics
        {
            public int TotalSearches;
            public int SuccessfulSearches;
            public List<double> SearchTimes = new List<double>();
            public Dictionary<string, int> CategoryUsage = new Dictionary<string, int>();
            public List<int> ResultCounts = new List<int>();
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
            public string grammar;
            public string error;
            public DateTime timestamp;
            public bool isSuccessful;
            public bool isValidJson;
            public string calculationSummary;
            public string tacticsType;
            public double tokensPerSecond;
            public int responseTokenCount;

            // RAG関連
            public bool usedRAG;
            public int ragResultCount;
            public double ragSearchTimeMs;
            public string[] ragResults;
            public float[] ragDistances;

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
            public List<TestResult> testResults;
            public List<string> errors;
            public Dictionary<string, int> situationTypeCounts;
            public Dictionary<string, int> tacticTypeCounts;
            public string promptGeneratorType;

            // RAG統計
            public bool ragEnabled;
            public int totalRAGSearches;
            public int successfulRAGSearches;
            public double averageRAGSearchTimeMs;
            public Dictionary<string, int> ragCategoryUsage;

            public IntegratedTestResults()
            {
                testResults = new List<TestResult>();
                errors = new List<string>();
                situationTypeCounts = new Dictionary<string, int>();
                tacticTypeCounts = new Dictionary<string, int>();
                ragCategoryUsage = new Dictionary<string, int>();
            }
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
            _performanceMetrics = new PerformanceMetrics();
            _ragStatistics = new RAGStatistics();

            // 出力ディレクトリの作成
            if (!Directory.Exists(_outputDirectoryPath))
            {
                Directory.CreateDirectory(_outputDirectoryPath);
            }

            // RAGの検証
            if (_useRAG && _rag == null)
            {
                UnityEngine.Debug.LogWarning("RAGが有効ですが、RAGコンポーネントが設定されていません。RAG機能を無効化します。");
                _useRAG = false;
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
            SetupSystemPrompt();

            // プロンプト生成器を初期化（依存性注入）
            _promptGenerator = CreatePromptGenerator(_generatorType);

            // テストデータ生成
            GenerateTestData();

            if (_showProgressInConsole)
            {
                string generatorName = _generatorType.ToString();
                UnityEngine.Debug.Log($"LLM連続思考テスト初期化完了 - セッションID: {_currentSessionId}");
                UnityEngine.Debug.Log($"プロンプト生成器: {generatorName}");
                UnityEngine.Debug.Log($"RAG機能: {(_useRAG ? "有効" : "無効")}");
                UnityEngine.Debug.Log($"基本テストデータ生成完了: {_baseTestData.Count}種類の戦況");
            }
        }

        /// <summary>
        /// プロンプト生成器を生成（依存性注入）
        /// </summary>
        private PromptGeneratorBase CreatePromptGenerator(PromptGeneratorType type)
        {
            return type switch
            {
                PromptGeneratorType.Japanese => new JapanesePromptGenerator(),
                PromptGeneratorType.English => new EnglishPromptGenerator(),
                _ => new JapanesePromptGenerator()
            };
        }

        /// <summary>
        /// システムプロンプトを設定（RAG対応）
        /// </summary>
        private void SetupSystemPrompt()
        {
            if (_generatorType == PromptGeneratorType.English)
            {
                _llmCharacter.prompt = @"You are a tactical combat AI assistant with access to a comprehensive game rules knowledge base.
Analyze battle data and the provided game rules to make strategic decisions in strict JSON format.
Always respond with ONLY valid JSON, no markdown, no explanations.
Use the provided game rules context to inform your tactical decisions.";
            }
            else
            {
                _llmCharacter.prompt = @"あなたは包括的なゲームルール知識ベースにアクセスできる戦術的な戦闘AIアシスタントです。
戦闘データと提供されたゲームルールを分析し、厳密なJSON形式で戦略的判断を提供してください。
常に有効なJSONのみで応答し、マークダウンや説明文は含めないでください。
提供されたゲームルールの文脈を使用して、戦術的判断を行ってください。";
            }

            _llmCharacter.playerName = "User";
            _llmCharacter.AIName = "TacticAI";
        }

        /// <summary>
        /// LLM Characterの最適設定を適用
        /// </summary>
        private void ConfigureLLMOptimal()
        {
            _llmCharacter.stream = true;
            _llmCharacter.cachePrompt = true;

            UnityEngine.Debug.Log("LLM最適設定を適用しました");
        }

        /// <summary>
        /// Grammar設定（新プロンプト形式用JSON Schema）
        /// </summary>
        private void SetupGrammar()
        {
            if (_generatorType == PromptGeneratorType.English)
            {
                _llmCharacter.grammarJSONString = @"{
  ""type"": ""object"",
  ""properties"": {
    ""analysis_result"": {
      ""type"": ""string""
    },
    ""basic_tactics"": {
      ""type"": ""string"",
      ""enum"": [""Aggressive"", ""Defensive"", ""Adaptive"", ""Disruptive"", ""Endurance""]
    },
    ""attack_judgment_criteria"": {
      ""type"": ""string"",
      ""enum"": [""Cumulative Probability Focus"", ""Recent Pattern Focus"", ""Speed Focus"", ""Return Focus"", ""Feint Focus"", ""Distribution Focus"", ""Energy Efficiency Focus""]
    },
    ""continuous_attack_judgment_criteria"": {
      ""type"": ""string"",
      ""enum"": [""Cumulative Probability Focus"", ""Recent Pattern Focus"", ""Speed Focus"", ""Return Focus"", ""Feint Focus"", ""Distribution Focus"", ""Energy Efficiency Focus""]
    },
    ""defense_judgment_criteria"": {
      ""type"": ""string"",
      ""enum"": [""Cumulative Probability Focus"", ""Recent Pattern Focus"", ""Counterattack Focus"", ""Return Focus"", ""Risk Avoidance Focus"", ""Counter Focus"", ""Distribution Focus""]
    },
    ""continuous_defense_judgment_criteria"": {
      ""type"": ""string"",
      ""enum"": [""Cumulative Probability Focus"", ""Recent Pattern Focus"", ""Counterattack Focus"", ""Return Focus"", ""Risk Avoidance Focus"", ""Counter Focus"", ""Distribution Focus""]
    }
  },
  ""required"": [""analysis_result"", ""basic_tactics"", ""attack_judgment_criteria"", ""continuous_attack_judgment_criteria"", ""defense_judgment_criteria"", ""continuous_defense_judgment_criteria""]
}";
            }
            else
            {
                _llmCharacter.grammarJSONString = @"{
  ""type"": ""object"",
  ""properties"": {
    ""分析結果"": {
      ""type"": ""string""
    },
    ""基本戦術"": {
      ""type"": ""string"",
      ""enum"": [""攻撃型"", ""防御型"", ""対応型"", ""攪乱型"", ""持久型""]
    },
    ""攻撃時判断基準"": {
      ""type"": ""string"",
      ""enum"": [""累積確率重視"", ""直近パターン重視"", ""速度重視"", ""リターン重視"", ""フェイント重視"", ""分散重視"", ""エネルギー効率重視""]
    },
    ""連続攻撃時判断基準"": {
      ""type"": ""string"",
      ""enum"": [""累積確率重視"", ""直近パターン重視"", ""速度重視"", ""リターン重視"", ""フェイント重視"", ""分散重視"", ""エネルギー効率重視""]
    },
    ""防御時判断基準"": {
      ""type"": ""string"",
      ""enum"": [""累積確率重視"", ""直近パターン重視"", ""反撃重視"", ""リターン重視"", ""リスク回避重視"", ""カウンター重視"", ""分散重視""]
    },
    ""連続防御時判断基準"": {
      ""type"": ""string"",
      ""enum"": [""累積確率重視"", ""直近パターン重視"", ""反撃重視"", ""リターン重視"", ""リスク回避重視"", ""カウンター重視"", ""分散重視""]
    }
  },
  ""required"": [""分析結果"", ""基本戦術"", ""攻撃時判断基準"", ""連続攻撃時判断基準"", ""防御時判断基準"", ""連続防御時判断基準""]
}";
            }

            UnityEngine.Debug.Log($"JSON Schema Grammar設定完了 ({_generatorType})");
        }

        private void GenerateTestData()
        {
            _baseTestData = new Dictionary<TestSituationType, LLMInputData>();
            foreach (TestSituationType situationType in Enum.GetValues(typeof(TestSituationType)))
            {
                _baseTestData[situationType] = LLMInputData.CreateForTestSituation(situationType);
            }

            // 初回テストデータを設定
            _currentTestData = _baseTestData[_situationType];
        }

        /// <summary>
        /// RAGで関連ルールを検索
        /// </summary>
        private async System.Threading.Tasks.Task<(string[] results, float[] distances, double searchTimeMs)> SearchRAGKnowledge(string query, string category = null)
        {
            if (!_useRAG || _rag == null)
            {
                return (new string[0], new float[0], 0);
            }

            var searchStopwatch = Stopwatch.StartNew();

            try
            {
                _ragStatistics.TotalSearches++;

                // カテゴリ指定がある場合とない場合で検索
                (string[] results, float[] distances) = category != null
                    ? await _rag.Search(query, _ragTopK, category)
                    : await _rag.Search(query, _ragTopK);

                searchStopwatch.Stop();
                double searchTimeMs = searchStopwatch.ElapsedMilliseconds;

                // 距離閾値でフィルタリング
                List<string> filteredResults = new List<string>();
                List<float> filteredDistances = new List<float>();

                for (int i = 0; i < results.Length && i < distances.Length; i++)
                {
                    if (distances[i] < _ragDistanceThreshold)
                    {
                        filteredResults.Add(results[i]);
                        filteredDistances.Add(distances[i]);
                    }
                }

                if (filteredResults.Count > 0)
                {
                    _ragStatistics.SuccessfulSearches++;
                }

                _ragStatistics.SearchTimes.Add(searchTimeMs);
                _ragStatistics.ResultCounts.Add(filteredResults.Count);

                if (!string.IsNullOrEmpty(category) && filteredResults.Count > 0)
                {
                    if (!_ragStatistics.CategoryUsage.ContainsKey(category))
                    {
                        _ragStatistics.CategoryUsage[category] = 0;
                    }
                    _ragStatistics.CategoryUsage[category]++;
                }

                if (_showRAGResults && filteredResults.Count > 0)
                {
                    UnityEngine.Debug.Log($"RAG検索完了: {filteredResults.Count}件の関連ルールを発見 ({searchTimeMs:F2}ms)");
                    for (int i = 0; i < filteredResults.Count; i++)
                    {
                        UnityEngine.Debug.Log($"  [{i + 1}] 距離: {filteredDistances[i]:F3} - {filteredResults[i].Substring(0, Math.Min(100, filteredResults[i].Length))}...");
                    }
                }

                return (filteredResults.ToArray(), filteredDistances.ToArray(), searchTimeMs);
            }
            catch (Exception ex)
            {
                searchStopwatch.Stop();
                UnityEngine.Debug.LogError($"RAG検索エラー: {ex.Message}");
                return (new string[0], new float[0], searchStopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// RAG結果をプロンプトに統合
        /// </summary>
        private string IntegrateRAGContext(string basePrompt, string[] ragResults)
        {
            if (ragResults == null || ragResults.Length == 0)
            {
                return basePrompt;
            }

            StringBuilder contextBuilder = new StringBuilder();

            if (_generatorType == PromptGeneratorType.English)
            {
                contextBuilder.AppendLine("=== RELEVANT GAME RULES ===");
                for (int i = 0; i < ragResults.Length; i++)
                {
                    contextBuilder.AppendLine($"\n[Rule {i + 1}]");
                    contextBuilder.AppendLine(ragResults[i]);
                }
                contextBuilder.AppendLine("\n=== END OF GAME RULES ===\n");
                contextBuilder.AppendLine("Based on the above game rules and the battle data below, provide your tactical judgment:\n");
            }
            else
            {
                contextBuilder.AppendLine("=== 関連ゲームルール ===");
                for (int i = 0; i < ragResults.Length; i++)
                {
                    contextBuilder.AppendLine($"\n[ルール{i + 1}]");
                    contextBuilder.AppendLine(ragResults[i]);
                }
                contextBuilder.AppendLine("\n=== ゲームルール終了 ===\n");
                contextBuilder.AppendLine("上記のゲームルールと以下の戦闘データに基づいて、戦術的判断を提供してください:\n");
            }

            contextBuilder.Append(basePrompt);

            return contextBuilder.ToString();
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
            var sampleData = LLMInputData.CreateForTestSituation(_situationType);
            var samplePrompt = _promptGenerator.GeneratePromptByData(sampleData);
            UnityEngine.Debug.Log($"Sample Prompt ({_situationType}):\n{samplePrompt}");
        }

        [ContextMenu("RAG検索テスト")]
        public async void TestRAGSearch()
        {
            if (!_useRAG || _rag == null)
            {
                UnityEngine.Debug.LogWarning("RAGが有効ではありません");
                return;
            }

            string testQuery = _generatorType == PromptGeneratorType.English
                ? "What should I do when attacking consecutively?"
                : "連続で攻撃している時はどうすればいいですか？";

            UnityEngine.Debug.Log($"RAG検索テスト実行: {testQuery}");

            var (results, distances, searchTime) = await SearchRAGKnowledge(testQuery);

            UnityEngine.Debug.Log($"検索完了: {results.Length}件 ({searchTime:F2}ms)");
            for (int i = 0; i < results.Length; i++)
            {
                UnityEngine.Debug.Log($"結果{i + 1} (距離: {distances[i]:F3}):\n{results[i]}\n");
            }
        }

        private IEnumerator RunContinuousTest()
        {
            _isTestRunning = true;
            _totalStopwatch.Start();
            _currentIteration = 0;

            if (_showProgressInConsole)
            {
                UnityEngine.Debug.Log($"連続思考テスト開始 - {_testIterations}回実行予定 ({_generatorType}, RAG: {(_useRAG ? "有効" : "無効")})");
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
                    // 新しい戦況データを生成するが、LastStrategyは保持
                    var newSituation = GetCurrentSituationType(i);
                    var newData = LLMInputData.CreateForTestSituation(newSituation);
                    newData.LastStrategy = _currentTestData.LastStrategy;
                    _currentTestData = newData;
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
                usedRAG = _useRAG
            };

            var stopwatch = Stopwatch.StartNew();

            // 基本プロンプト生成
            string basePrompt = _promptGenerator.GeneratePromptByData(_currentTestData);

            // RAG検索（非同期処理を同期的に待つ）
            string[] ragResults = new string[0];
            float[] ragDistances = new float[0];
            double ragSearchTimeMs = 0;

            if (_useRAG && _rag != null)
            {
                // 戦況に応じた検索クエリを生成
                string searchQuery = GenerateRAGSearchQuery(_currentTestData);

                var ragTask = SearchRAGKnowledge(searchQuery);
                yield return new WaitUntil(() => ragTask.IsCompleted);

                var ragResult = ragTask.Result;
                ragResults = ragResult.results;
                ragDistances = ragResult.distances;
                ragSearchTimeMs = ragResult.searchTimeMs;

                testResult.ragResultCount = ragResults.Length;
                testResult.ragSearchTimeMs = ragSearchTimeMs;
                testResult.ragResults = ragResults;
                testResult.ragDistances = ragDistances;
            }

            // RAG結果をプロンプトに統合
            string fullPrompt = IntegrateRAGContext(basePrompt, ragResults);

            testResult.prompt = fullPrompt;
            testResult.situationType = GetCurrentSituationType(iteration).ToString();

            if (_showDetailedTiming)
            {
                UnityEngine.Debug.Log($"プロンプト生成完了: {fullPrompt.Length}文字 {EstimateTokenCount(fullPrompt)}トークン");
                if (_useRAG)
                {
                    UnityEngine.Debug.Log($"RAG: {ragResults.Length}件の関連ルール追加 ({ragSearchTimeMs:F2}ms)");
                }
            }

            // LLMに完全なプロンプトを送信
            yield return StartCoroutine(SendToLLM(fullPrompt, testResult));

            // 応答の検証
            if (_validateResponses && !string.IsNullOrEmpty(testResult.response))
            {
                ValidateResponse(testResult);

                // LLMの応答からLastStrategyを更新
                UpdateLastStrategyFromResponse(testResult.response);
            }

            stopwatch.Stop();
            testResult.responseTimeSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
            testResult.isSuccessful = !string.IsNullOrEmpty(testResult.response);

            // トークン数と速度を計算
            if (testResult.isSuccessful)
            {
                testResult.responseTokenCount = EstimateTokenCount(testResult.response);
                testResult.tokensPerSecond = testResult.responseTokenCount / testResult.responseTimeSeconds;

                _performanceMetrics.TokensPerSecondHistory.Add(testResult.tokensPerSecond);
                _performanceMetrics.TotalTokensGenerated += testResult.responseTokenCount;
            }

            if (_showDetailedTiming)
            {
                UnityEngine.Debug.Log($"テスト {testResult.iteration} 完了: {testResult.responseTimeSeconds:F2}秒");

                if (_showPerformanceMetrics)
                {
                    UnityEngine.Debug.Log($"  トークン数: {testResult.responseTokenCount}, 速度: {testResult.tokensPerSecond:F1} tokens/秒");
                }
            }

            _testResults.Add(testResult);
        }

        /// <summary>
        /// RAG検索用のクエリを生成
        /// </summary>
        private string GenerateRAGSearchQuery(LLMInputData data)
        {
            // 戦況データから適切な検索クエリを生成
            StringBuilder queryBuilder = new StringBuilder();

            // 前回の戦術があれば参照
            if (data.LastStrategy != null)
            {
                if (_generatorType == PromptGeneratorType.English)
                {
                    queryBuilder.Append($"Previous tactics: {data.LastStrategy.基本戦術}. ");
                }
                else
                {
                    queryBuilder.Append($"前回の戦術: {data.LastStrategy.基本戦術}。");
                }
            }

            // 体力・エネルギー状況に応じたクエリ
            float healthRatio = data.MyData.Hp / 100f;
            float energyRatio = data.MyData.Energy / 100f;

            if (_generatorType == PromptGeneratorType.English)
            {
                if (healthRatio < 0.3f)
                {
                    queryBuilder.Append("Low health defensive tactics. ");
                }
                if (energyRatio < 0.3f)
                {
                    queryBuilder.Append("Energy efficiency tactics. ");
                }
                if (healthRatio > 0.7f && energyRatio > 0.7f)
                {
                    queryBuilder.Append("Aggressive attack tactics. ");
                }

                queryBuilder.Append("Tactical judgment criteria.");
            }
            else
            {
                if (healthRatio < 0.3f)
                {
                    queryBuilder.Append("体力が少ない時の防御戦術。");
                }
                if (energyRatio < 0.3f)
                {
                    queryBuilder.Append("エネルギー効率を重視した戦術。");
                }
                if (healthRatio > 0.7f && energyRatio > 0.7f)
                {
                    queryBuilder.Append("攻撃的な戦術。");
                }

                queryBuilder.Append("戦術判断基準。");
            }

            return queryBuilder.ToString();
        }

        /// <summary>
        /// LLMの応答からLastStrategyを更新
        /// </summary>
        private void UpdateLastStrategyFromResponse(string jsonResponse)
        {
            try
            {
                StrategyData strategy;

                if (_generatorType == PromptGeneratorType.English)
                {
                    strategy = StrategyData.FromJsonEnglish(jsonResponse);
                }
                else
                {
                    strategy = StrategyData.FromJson(jsonResponse);
                }

                // LastStrategyを更新（次のイテレーションで使用）
                _currentTestData.LastStrategy = strategy;

                if (_showProgressInConsole)
                {
                    UnityEngine.Debug.Log($"LastStrategy更新: 基本戦術={strategy.基本戦術}");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"LastStrategy更新失敗: {ex.Message}");
            }
        }

        private int EstimateTokenCount(string text)
        {
            if (_generatorType == PromptGeneratorType.English)
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

            UnityEngine.Debug.Log($"LLM応答: {response}");
        }

        private void ValidateResponse(TestResult testResult)
        {
            try
            {
                var parsedResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(testResult.response);
                testResult.isValidJson = true;

                bool hasRequiredFields;

                if (_generatorType == PromptGeneratorType.English)
                {
                    hasRequiredFields =
                        parsedResponse.ContainsKey("analysis_result") &&
                        parsedResponse.ContainsKey("basic_tactics") &&
                        parsedResponse.ContainsKey("attack_judgment_criteria") &&
                        parsedResponse.ContainsKey("continuous_attack_judgment_criteria") &&
                        parsedResponse.ContainsKey("defense_judgment_criteria") &&
                        parsedResponse.ContainsKey("continuous_defense_judgment_criteria");
                }
                else
                {
                    hasRequiredFields =
                        parsedResponse.ContainsKey("分析結果") &&
                        parsedResponse.ContainsKey("基本戦術") &&
                        parsedResponse.ContainsKey("攻撃時判断基準") &&
                        parsedResponse.ContainsKey("連続攻撃時判断基準") &&
                        parsedResponse.ContainsKey("防御時判断基準") &&
                        parsedResponse.ContainsKey("連続防御時判断基準");
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
            // 現在のテストデータを返す（LastStrategyが更新されている）
            return _currentTestData;
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

        private void SaveIntegratedResults()
        {
            try
            {
                var integratedResults = CreateIntegratedResults();

                string generatorTag = _generatorType.ToString();
                string ragTag = _useRAG ? "_RAG" : "_NoRAG";

                // JSONファイルとして保存
                string jsonFileName = $"{_filePrefix}_{generatorTag}{ragTag}_AllResults_{_currentSessionId}.json";
                string jsonFilePath = Path.Combine(_outputDirectoryPath, jsonFileName);
                string json = JsonConvert.SerializeObject(integratedResults, Formatting.Indented);
                File.WriteAllText(jsonFilePath, json, Encoding.UTF8);

                // 人間が読みやすいテキスト形式でも保存
                string txtFileName = $"{_filePrefix}_{generatorTag}{ragTag}_Report_{_currentSessionId}.txt";
                string txtFilePath = Path.Combine(_outputDirectoryPath, txtFileName);
                string textReport = CreateReadableReport(integratedResults);
                File.WriteAllText(txtFilePath, textReport, Encoding.UTF8);

                // CSV形式での統計データ保存
                string csvFileName = $"{_filePrefix}_{generatorTag}{ragTag}_Stats_{_currentSessionId}.csv";
                string csvFilePath = Path.Combine(_outputDirectoryPath, csvFileName);
                string csvData = CreateCsvReport(integratedResults);
                File.WriteAllText(csvFilePath, csvData, Encoding.UTF8);

                if (_showProgressInConsole)
                {
                    UnityEngine.Debug.Log($"テスト結果を保存しました:\n- JSON: {jsonFileName}\n- レポート: {txtFileName}\n- CSV: {csvFileName}");
                }

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
                promptGeneratorType = _generatorType.ToString(),
                ragEnabled = _useRAG,
                totalRAGSearches = _ragStatistics.TotalSearches,
                successfulRAGSearches = _ragStatistics.SuccessfulSearches,
                ragCategoryUsage = _ragStatistics.CategoryUsage
            };

            if (_ragStatistics.SearchTimes.Count > 0)
            {
                results.averageRAGSearchTimeMs = _ragStatistics.SearchTimes.Average();
            }

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

                if (!results.situationTypeCounts.ContainsKey(result.situationType))
                    results.situationTypeCounts[result.situationType] = 0;
                results.situationTypeCounts[result.situationType]++;

                if (!string.IsNullOrEmpty(result.tacticsType))
                {
                    if (!results.tacticTypeCounts.ContainsKey(result.tacticsType))
                        results.tacticTypeCounts[result.tacticsType] = 0;
                    results.tacticTypeCounts[result.tacticsType]++;
                }
            }

            if (results.totalTests > 0)
            {
                results.successRate = (double)results.successfulTests / results.totalTests;
                results.jsonValidRate = (double)results.validJsonResponses / results.totalTests;
            }

            var successfulResults = _testResults.FindAll(r => r.isSuccessful);
            if (successfulResults.Count > 0)
            {
                var responseTimes = successfulResults.ConvertAll(r => r.responseTimeSeconds);
                results.averageResponseTimeSeconds = responseTimes.Average();
                results.minResponseTimeSeconds = responseTimes.Min();
                results.maxResponseTimeSeconds = responseTimes.Max();
            }

            if (_performanceMetrics.TokensPerSecondHistory.Count > 0)
            {
                results.averageTokensPerSecond = _performanceMetrics.TokensPerSecondHistory.Average();
            }

            return results;
        }

        private string CreateReadableReport(IntegratedTestResults results)
        {
            var report = new StringBuilder();

            report.AppendLine("================================================================================");
            report.AppendLine($"        LLM連続思考テスト詳細レポート（{results.promptGeneratorType}）");
            report.AppendLine("================================================================================");
            report.AppendLine();
            report.AppendLine($"セッションID: {results.sessionId}");
            report.AppendLine($"プロンプト生成器: {results.promptGeneratorType}");
            report.AppendLine($"RAG機能: {(results.ragEnabled ? "有効" : "無効")}");
            report.AppendLine($"開始時刻: {results.startTime:yyyy/MM/dd HH:mm:ss}");
            report.AppendLine($"終了時刻: {results.endTime:yyyy/MM/dd HH:mm:ss}");
            report.AppendLine($"総実行時間: {results.totalTimeSeconds:F2}秒");
            report.AppendLine();

            report.AppendLine("【テスト結果サマリー】");
            report.AppendLine($"総テスト数: {results.totalTests}");
            report.AppendLine($"成功: {results.successfulTests}");
            report.AppendLine($"失敗: {results.failedTests}");
            report.AppendLine($"成功率: {results.successRate:P1}");
            report.AppendLine($"有効なJSON応答: {results.validJsonResponses}");
            report.AppendLine($"JSON有効率: {results.jsonValidRate:P1}");
            report.AppendLine();

            if (results.ragEnabled)
            {
                report.AppendLine("【RAG統計】");
                report.AppendLine($"総検索回数: {results.totalRAGSearches}");
                report.AppendLine($"成功検索回数: {results.successfulRAGSearches}");
                report.AppendLine($"平均検索時間: {results.averageRAGSearchTimeMs:F2}ms");

                if (results.ragCategoryUsage.Count > 0)
                {
                    report.AppendLine("カテゴリ別使用回数:");
                    foreach (var kvp in results.ragCategoryUsage)
                    {
                        report.AppendLine($"  {kvp.Key}: {kvp.Value}回");
                    }
                }
                report.AppendLine();
            }

            if (results.successfulTests > 0)
            {
                report.AppendLine("【応答時間統計】");
                report.AppendLine($"平均応答時間: {results.averageResponseTimeSeconds:F2}秒");
                report.AppendLine($"最短応答時間: {results.minResponseTimeSeconds:F2}秒");
                report.AppendLine($"最長応答時間: {results.maxResponseTimeSeconds:F2}秒");
                report.AppendLine();
            }

            if (results.averageTokensPerSecond > 0)
            {
                report.AppendLine("【パフォーマンスメトリクス】");
                report.AppendLine($"平均生成速度: {results.averageTokensPerSecond:F1} tokens/秒");
                report.AppendLine();
            }

            if (results.situationTypeCounts.Count > 0)
            {
                report.AppendLine("【戦況タイプ分布】");
                foreach (var kvp in results.situationTypeCounts)
                {
                    report.AppendLine($"  {kvp.Key}: {kvp.Value}回");
                }
                report.AppendLine();
            }

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
                report.AppendLine($"■ テスト {result.iteration}");
                report.AppendLine($"  時刻: {result.timestamp:HH:mm:ss}");
                report.AppendLine($"  状況: {result.situationType}");
                report.AppendLine($"  結果: {(result.isSuccessful ? "成功" : "失敗")}");
                report.AppendLine($"  JSON有効: {(result.isValidJson ? "有効" : "無効")}");
                report.AppendLine($"  応答時間: {result.responseTimeSeconds:F2}秒");

                if (result.usedRAG)
                {
                    report.AppendLine($"  RAG検索: {result.ragResultCount}件 ({result.ragSearchTimeMs:F2}ms)");
                }

                if (result.tokensPerSecond > 0)
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
                    report.AppendLine($"  レスポンス: {result.response}");
                }

                report.AppendLine("--------------------------------------------------------------------------------");
            }

            return report.ToString();
        }

        private string CreateCsvReport(IntegratedTestResults results)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Iteration,Timestamp,SituationType,IsSuccessful,IsValidJson,ResponseTimeSeconds,TokensPerSecond,TokenCount,PromptLength,ResponseLength,UsedRAG,RAGResultCount,RAGSearchTimeMs,Error");

            foreach (var result in results.testResults)
            {
                csv.AppendLine($"{result.iteration}," +
                              $"{result.timestamp:yyyy-MM-dd HH:mm:ss}," +
                              $"{result.situationType}," +
                              $"{result.isSuccessful}," +
                              $"{result.isValidJson}," +
                              $"{result.responseTimeSeconds:F2}," +
                              $"{result.tokensPerSecond:F1}," +
                              $"{result.responseTokenCount}," +
                              $"{result.prompt?.Length ?? 0}," +
                              $"{result.response?.Length ?? 0}," +
                              $"{result.usedRAG}," +
                              $"{result.ragResultCount}," +
                              $"{result.ragSearchTimeMs:F2}," +
                              $"\"{result.error ?? ""}\"");
            }

            return csv.ToString();
        }

        private void DisplaySummaryInConsole(IntegratedTestResults results)
        {
            var summary = new StringBuilder();

            summary.AppendLine($"=== LLMテスト完了（{results.promptGeneratorType}, RAG: {(results.ragEnabled ? "有効" : "無効")}） ===");
            summary.AppendLine($"総テスト数: {results.totalTests}");
            summary.AppendLine($"成功: {results.successfulTests} / 失敗: {results.failedTests}");
            summary.AppendLine($"成功率: {results.successRate:P1}");
            summary.AppendLine($"JSON有効率: {results.jsonValidRate:P1}");

            if (results.ragEnabled)
            {
                summary.AppendLine($"RAG検索: {results.totalRAGSearches}回 (成功: {results.successfulRAGSearches}回)");
                summary.AppendLine($"平均RAG検索時間: {results.averageRAGSearchTimeMs:F2}ms");
            }

            if (results.successfulTests > 0)
            {
                summary.AppendLine($"平均応答時間: {results.averageResponseTimeSeconds:F2}秒");
                summary.AppendLine($"最短/最長: {results.minResponseTimeSeconds:F2}秒 / {results.maxResponseTimeSeconds:F2}秒");
            }

            if (results.averageTokensPerSecond > 0)
            {
                summary.AppendLine($"平均生成速度: {results.averageTokensPerSecond:F1} tokens/秒");
            }

            summary.AppendLine($"総実行時間: {results.totalTimeSeconds:F2}秒");

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
        /// RAG統計をリセット
        /// </summary>
        [ContextMenu("RAG統計をリセット")]
        public void ResetRAGStatistics()
        {
            _ragStatistics = new RAGStatistics();
            UnityEngine.Debug.Log("RAG統計をリセットしました");
        }

        /// <summary>
        /// RAG統計を表示
        /// </summary>
        [ContextMenu("RAG統計を表示")]
        public void ShowRAGStatistics()
        {
            if (!_useRAG)
            {
                UnityEngine.Debug.Log("RAG機能は無効です");
                return;
            }

            var stats = new StringBuilder();
            stats.AppendLine("=== RAG統計 ===");
            stats.AppendLine($"総検索回数: {_ragStatistics.TotalSearches}");
            stats.AppendLine($"成功検索回数: {_ragStatistics.SuccessfulSearches}");

            if (_ragStatistics.SearchTimes.Count > 0)
            {
                stats.AppendLine($"平均検索時間: {_ragStatistics.SearchTimes.Average():F2}ms");
                stats.AppendLine($"最速: {_ragStatistics.SearchTimes.Min():F2}ms");
                stats.AppendLine($"最遅: {_ragStatistics.SearchTimes.Max():F2}ms");
            }

            if (_ragStatistics.ResultCounts.Count > 0)
            {
                stats.AppendLine($"平均結果数: {_ragStatistics.ResultCounts.Average():F1}件");
            }

            if (_ragStatistics.CategoryUsage.Count > 0)
            {
                stats.AppendLine("カテゴリ別使用回数:");
                foreach (var kvp in _ragStatistics.CategoryUsage)
                {
                    stats.AppendLine($"  {kvp.Key}: {kvp.Value}回");
                }
            }

            UnityEngine.Debug.Log(stats.ToString());
        }

        /// <summary>
        /// RAG機能の有効/無効を切り替え
        /// </summary>
        [ContextMenu("RAG機能を切り替え")]
        public void ToggleRAG()
        {
            _useRAG = !_useRAG;

            if (_useRAG && _rag == null)
            {
                UnityEngine.Debug.LogWarning("RAGコンポーネントが設定されていません");
                _useRAG = false;
            }
            else
            {
                UnityEngine.Debug.Log($"RAG機能を{(_useRAG ? "有効" : "無効")}化しました");
            }
        }

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

        /// <summary>
        /// プロンプト生成器を変更
        /// </summary>
        [ContextMenu("プロンプト生成器を変更")]
        public void ChangePromptGenerator()
        {
            // 次のタイプに切り替え
            int nextType = ((int)_generatorType + 1) % 3;
            _generatorType = (PromptGeneratorType)nextType;

            _promptGenerator = CreatePromptGenerator(_generatorType);
            SetupSystemPrompt();
            SetupGrammar();

            UnityEngine.Debug.Log($"プロンプト生成器を {_generatorType} に変更しました");
        }

        #endregion
    }
}