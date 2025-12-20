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
using Debug = UnityEngine.Debug;

namespace LLMDataArchitect.Test
{
    /// <summary>
    /// プロンプト生成タイプ
    /// </summary>
    public enum PromptGeneratorType
    {
        Japanese,
        English,
        Jap_Rag,
        Eng_Rag,
        Fixed_Eng,
        Main,
        Cache,
        Tuned,
        Experimental,

        // === ルールベース系NLI ===
        Cache_NLI_AggressiveFinisher,
        Cache_NLI_AggressiveDisruptor,
        Cache_NLI_DefensiveSurvivor,
        Cache_NLI_DefensiveCounter,
        Cache_NLI_BalancedAdaptive,
        Cache_NLI_AnalyticalLearner,
        Cache_NLI_EnduranceManager,

        // === 自然言語系NLI ===
        Cache_NLI_CorneredBeast,    // 追い詰められるほど攻撃的に
        Cache_NLI_Finisher,          // 敵HPが減るほど攻撃的に
        Cache_NLI_FrontRunner,       // リード時は安全に
        Cache_NLI_PatternBreaker,    // 予測不能に動く
        Cache_NLI_MomentumRider,     // 流れに乗る/変える
        Cache_NLI_StaminaManager,    // エネルギー意識
        Cache_NLI_CounterPuncher,    // 反撃重視
        Cache_NLI_Berserker,         // 常時攻撃的
        Cache_NLI_Tactician,         // 慎重・確実
        Cache_NLI_WaterFlow,         // 状況適応（柔軟）

        // === ランダム選択 ===
        Cache_NLI_Random,            // ランダムにNLIタイプを選択
        Cache_NLI_Random_RuleBased,  // ルールベース系からランダム
        Cache_NLI_Random_Natural     // 自然言語系からランダム
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

        [Header("RAG初期化設定")]
        [SerializeField] private bool _autoInitializeRAG = true;
        [SerializeField] private bool _loadFromResources = true;
        [SerializeField] private string _resourcesPath = "GameRules";
        [SerializeField] private TextAsset[] _manualRuleFiles;
        [SerializeField] private string _ragFolderPath = "Assets/Resources/GameRules";

        private string[] _defaultRuleNames = new string[]
        {
            "tactical_types",
            "attack_criteria",
            "defense_criteria",
            "continuous_actions",
            "analysis_format",
            "faq"
        };

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
        [SerializeField] private bool _verboseRAGLogging = false;

        // プロンプト生成インターフェイス
        private PromptGeneratorBase _promptGenerator;

        // NLI付きジェネレーター（Cache_NLI系の場合に使用）
        private CachePromptGeneratorWithNLI _nliGenerator;

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
            public string response;
            public string error;
            public DateTime timestamp;
            public bool isSuccessful;
            public bool isValidJson;
            public string tacticsType;
            public double tokensPerSecond;
            public int responseTokenCount;

            // NLI関連
            public string nliType;

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

            // NLI統計
            public Dictionary<string, int> nliTypeCounts;

            public IntegratedTestResults()
            {
                testResults = new List<TestResult>();
                errors = new List<string>();
                situationTypeCounts = new Dictionary<string, int>();
                tacticTypeCounts = new Dictionary<string, int>();
                ragCategoryUsage = new Dictionary<string, int>();
                nliTypeCounts = new Dictionary<string, int>();
            }
        }

        private void Start()
        {
            InitializeTest();

            if (_autoInitializeRAG && _useRAG && _rag != null)
            {
                StartCoroutine(InitializeRAGCoroutine());
            }

            if (_autoStartOnPlay)
            {
                StartCoroutine(RunContinuousTest());
            }
        }

        /// <summary>
        /// RAGを初期化（非同期処理をコルーチンでラップ）
        /// </summary>
        private IEnumerator InitializeRAGCoroutine()
        {
            var initTask = InitializeRAG();
            yield return new WaitUntil(() => initTask.IsCompleted);

            if (initTask.Exception != null)
            {
                UnityEngine.Debug.LogError($"RAG初期化エラー: {initTask.Exception.Message}");
            }
        }

        /// <summary>
        /// RAGデータベースを初期化
        /// </summary>
        private async System.Threading.Tasks.Task InitializeRAG()
        {
            if (_rag == null)
            {
                UnityEngine.Debug.LogWarning("RAGコンポーネントが設定されていません");
                return;
            }

            // 既存データベースの確認
            string dbPath = System.IO.Path.Combine(Application.streamingAssetsPath, "tactical_knowledge.zip");

            if (System.IO.File.Exists(dbPath))
            {
                UnityEngine.Debug.Log("既存のRAGデータベースをロード中...");
                try
                {
                    await _rag.Load("tactical_knowledge.zip");
                    UnityEngine.Debug.Log("✅ RAGデータベースロード完了");
                    return;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"既存データベースのロードに失敗: {ex.Message}");
                    UnityEngine.Debug.Log("新規データベースを作成します...");
                }
            }

            // 新規作成
            UnityEngine.Debug.Log("新規RAGデータベースを作成中...");

            int loadedCount = 0;

            if (_loadFromResources)
            {
                loadedCount = await LoadRulesFromResources();
            }
            else if (_manualRuleFiles != null && _manualRuleFiles.Length > 0)
            {
                loadedCount = await LoadRulesFromTextAssets();
            }
            else
            {
                UnityEngine.Debug.LogError("ルールファイルが設定されていません");
                return;
            }

            if (loadedCount > 0)
            {
                // 保存
                _rag.Save("tactical_knowledge.zip");
                UnityEngine.Debug.Log($"✅ RAGデータベース作成・保存完了 ({loadedCount}ファイル)");
            }
            else
            {
                UnityEngine.Debug.LogError("ルールファイルの読み込みに失敗しました");
            }
        }

        /// <summary>
        /// Resourcesフォルダからルールを読み込み
        /// </summary>
        private async System.Threading.Tasks.Task<int> LoadRulesFromResources()
        {
            int loadedCount = 0;

            foreach (string ruleName in _defaultRuleNames)
            {
                TextAsset asset = Resources.Load<TextAsset>($"{_resourcesPath}/{ruleName}");

                if (asset != null)
                {
                    try
                    {
                        await _rag.Add(asset.text, ruleName);
                        loadedCount++;
                        UnityEngine.Debug.Log($"✅ {ruleName} を追加");
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"❌ {ruleName} の追加に失敗: {ex.Message}");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"⚠️ Resources/{_resourcesPath}/{ruleName}.txt が見つかりません");
                }
            }

            UnityEngine.Debug.Log($"{loadedCount}/{_defaultRuleNames.Length} 個のルールファイルを読み込みました");
            return loadedCount;
        }

        /// <summary>
        /// 手動設定のTextAssetからルールを読み込み
        /// </summary>
        private async System.Threading.Tasks.Task<int> LoadRulesFromTextAssets()
        {
            int loadedCount = 0;

            foreach (var file in _manualRuleFiles)
            {
                if (file != null)
                {
                    try
                    {
                        await _rag.Add(file.text, file.name);
                        loadedCount++;
                        UnityEngine.Debug.Log($"✅ {file.name} を追加");
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"❌ {file.name} の追加に失敗: {ex.Message}");
                    }
                }
            }

            UnityEngine.Debug.Log($"{loadedCount}/{_manualRuleFiles.Length} 個のルールファイルを読み込みました");
            return loadedCount;
        }

        private void InitializeTest()
        {
            _testResults = new List<TestResult>();
            _totalStopwatch = new Stopwatch();
            _logBuilder = new StringBuilder();
            _currentSessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _performanceMetrics = new PerformanceMetrics();
            _ragStatistics = new RAGStatistics();

            // プロンプト生成器を初期化（依存性注入）
            _promptGenerator = CreatePromptGenerator(_generatorType);

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



            // テストデータ生成
            GenerateTestData();

            if (_showProgressInConsole)
            {
                string generatorName = _generatorType.ToString();
                UnityEngine.Debug.Log($"LLM連続思考テスト初期化完了 - セッションID: {_currentSessionId}");
                UnityEngine.Debug.Log($"プロンプト生成器: {generatorName}");
                UnityEngine.Debug.Log($"RAG機能: {(_useRAG ? "有効" : "無効")}");

                // NLI情報を表示
                if (_nliGenerator != null)
                {
                    string nliInfo = IsRandomNLIType(_generatorType)
                        ? "ランダム（各イテレーションで変更）"
                        : CachePromptGeneratorWithNLI.GetInstructionDescription(_nliGenerator.CurrentInstructionType);
                    UnityEngine.Debug.Log($"自然言語指示: {nliInfo}");
                }

                UnityEngine.Debug.Log($"基本テストデータ生成完了: {_baseTestData.Count}種類の戦況");
            }
        }

        /// <summary>
        /// プロンプト生成器を生成（依存性注入）
        /// ★ここを編集して好きなインスタンスを生成可能 ★
        /// </summary>
        private PromptGeneratorBase CreatePromptGenerator(PromptGeneratorType type)
        {
            Debug.Log($"プロンプト生成を初期化: {type}");

            // NLI付きタイプの場合
            if (IsNLIType(type))
            {
                var nliType = GetNLITypeFromGeneratorType(type);
                _nliGenerator = new CachePromptGeneratorWithNLI(nliType);
                Debug.Log($"  → CachePromptGeneratorWithNLI ({nliType}) を生成");
                return _nliGenerator;
            }

            // 通常タイプの場合
            _nliGenerator = null;

            return type switch
            {
                PromptGeneratorType.Japanese => new JapanesePromptGenerator(),
                PromptGeneratorType.English => new EnglishPromptGenerator(),
                PromptGeneratorType.Jap_Rag => new JapRagPromptGenerator(),
                PromptGeneratorType.Eng_Rag => new EngRagPromptGenerator(),
                PromptGeneratorType.Fixed_Eng => new FixedEnglishGenerator(),
                PromptGeneratorType.Main => new MainPromptGenerator(),
                PromptGeneratorType.Cache => new CachePromptGenerator(),
                PromptGeneratorType.Tuned => new TunedPromptGenerator(),
                _ => new JapanesePromptGenerator()
            };
        }

        /// <summary>
        /// 指定タイプがNLI付きかどうかを判定
        /// </summary>
        private bool IsNLIType(PromptGeneratorType type)
        {
            return type switch
            {
                // ルールベース系
                PromptGeneratorType.Cache_NLI_AggressiveFinisher => true,
                PromptGeneratorType.Cache_NLI_AggressiveDisruptor => true,
                PromptGeneratorType.Cache_NLI_DefensiveSurvivor => true,
                PromptGeneratorType.Cache_NLI_DefensiveCounter => true,
                PromptGeneratorType.Cache_NLI_BalancedAdaptive => true,
                PromptGeneratorType.Cache_NLI_AnalyticalLearner => true,
                PromptGeneratorType.Cache_NLI_EnduranceManager => true,

                // 自然言語系
                PromptGeneratorType.Cache_NLI_CorneredBeast => true,
                PromptGeneratorType.Cache_NLI_Finisher => true,
                PromptGeneratorType.Cache_NLI_FrontRunner => true,
                PromptGeneratorType.Cache_NLI_PatternBreaker => true,
                PromptGeneratorType.Cache_NLI_MomentumRider => true,
                PromptGeneratorType.Cache_NLI_StaminaManager => true,
                PromptGeneratorType.Cache_NLI_CounterPuncher => true,
                PromptGeneratorType.Cache_NLI_Berserker => true,
                PromptGeneratorType.Cache_NLI_Tactician => true,
                PromptGeneratorType.Cache_NLI_WaterFlow => true,

                // ランダム系
                PromptGeneratorType.Cache_NLI_Random => true,
                PromptGeneratorType.Cache_NLI_Random_RuleBased => true,
                PromptGeneratorType.Cache_NLI_Random_Natural => true,

                _ => false
            };
        }

        /// <summary>
        /// 指定タイプがランダムNLIかどうかを判定
        /// </summary>
        private bool IsRandomNLIType(PromptGeneratorType type)
        {
            return type == PromptGeneratorType.Cache_NLI_Random ||
                   type == PromptGeneratorType.Cache_NLI_Random_RuleBased ||
                   type == PromptGeneratorType.Cache_NLI_Random_Natural;
        }

        /// <summary>
        /// PromptGeneratorTypeからNaturalLanguageInstructionTypeを取得
        /// </summary>
        private NaturalLanguageInstructionType GetNLITypeFromGeneratorType(PromptGeneratorType type)
        {
            return type switch
            {
                // ルールベース系
                PromptGeneratorType.Cache_NLI_AggressiveFinisher => NaturalLanguageInstructionType.AggressiveFinisher,
                PromptGeneratorType.Cache_NLI_AggressiveDisruptor => NaturalLanguageInstructionType.AggressiveDisruptor,
                PromptGeneratorType.Cache_NLI_DefensiveSurvivor => NaturalLanguageInstructionType.DefensiveSurvivor,
                PromptGeneratorType.Cache_NLI_DefensiveCounter => NaturalLanguageInstructionType.DefensiveCounter,
                PromptGeneratorType.Cache_NLI_BalancedAdaptive => NaturalLanguageInstructionType.BalancedAdaptive,
                PromptGeneratorType.Cache_NLI_AnalyticalLearner => NaturalLanguageInstructionType.AnalyticalLearner,
                PromptGeneratorType.Cache_NLI_EnduranceManager => NaturalLanguageInstructionType.EnduranceManager,

                // 自然言語系
                PromptGeneratorType.Cache_NLI_CorneredBeast => NaturalLanguageInstructionType.CorneredBeast,
                PromptGeneratorType.Cache_NLI_Finisher => NaturalLanguageInstructionType.Finisher,
                PromptGeneratorType.Cache_NLI_FrontRunner => NaturalLanguageInstructionType.FrontRunner,
                PromptGeneratorType.Cache_NLI_PatternBreaker => NaturalLanguageInstructionType.PatternBreaker,
                PromptGeneratorType.Cache_NLI_MomentumRider => NaturalLanguageInstructionType.MomentumRider,
                PromptGeneratorType.Cache_NLI_StaminaManager => NaturalLanguageInstructionType.StaminaManager,
                PromptGeneratorType.Cache_NLI_CounterPuncher => NaturalLanguageInstructionType.CounterPuncher,
                PromptGeneratorType.Cache_NLI_Berserker => NaturalLanguageInstructionType.Berserker,
                PromptGeneratorType.Cache_NLI_Tactician => NaturalLanguageInstructionType.Tactician,
                PromptGeneratorType.Cache_NLI_WaterFlow => NaturalLanguageInstructionType.WaterFlow,

                // ランダム系は初期値（後でランダム選択）
                PromptGeneratorType.Cache_NLI_Random => NaturalLanguageInstructionType.None,
                PromptGeneratorType.Cache_NLI_Random_RuleBased => NaturalLanguageInstructionType.None,
                PromptGeneratorType.Cache_NLI_Random_Natural => NaturalLanguageInstructionType.None,

                _ => NaturalLanguageInstructionType.None
            };
        }

        /// <summary>
        /// ランダムなNLIタイプを取得
        /// </summary>
        private NaturalLanguageInstructionType GetRandomNLIType()
        {
            return GetRandomNLIType(_generatorType);
        }

        /// <summary>
        /// 指定されたランダムタイプに応じたNLIタイプを取得
        /// </summary>
        private NaturalLanguageInstructionType GetRandomNLIType(PromptGeneratorType randomType)
        {
            NaturalLanguageInstructionType[] candidates;

            switch (randomType)
            {
                case PromptGeneratorType.Cache_NLI_Random_RuleBased:
                    candidates = CachePromptGeneratorWithNLI.GetRuleBasedTypes();
                    break;
                case PromptGeneratorType.Cache_NLI_Random_Natural:
                    candidates = CachePromptGeneratorWithNLI.GetNaturalLanguageTypes();
                    break;
                default: // Cache_NLI_Random
                    candidates = CachePromptGeneratorWithNLI.GetActiveInstructionTypes();
                    break;
            }

            int randomIndex = UnityEngine.Random.Range(0, candidates.Length);
            return candidates[randomIndex];
        }

        /// <summary>
        /// システムプロンプトを設定（RAG対応）
        /// </summary>
        private void SetupSystemPrompt()
        {
            if (_generatorType == PromptGeneratorType.English || _generatorType == PromptGeneratorType.Main)
            {
                _llmCharacter.prompt = @"You are a tactical combat AI assistant with access to a comprehensive game rules knowledge base.
Analyze battle data and the provided game rules to make strategic decisions in strict JSON format.
Always respond with ONLY valid JSON, no markdown, no explanations.
Use the provided game rules context to inform your tactical decisions.";
            }
            else if (_generatorType == PromptGeneratorType.Cache || IsNLIType(_generatorType))
            {
                // Cache系（NLI含む）は共通のFixedSectionを使用
                _llmCharacter.SetPrompt(_promptGenerator.GenerateFixedSection());
            }
            else if (_generatorType == PromptGeneratorType.Tuned)
            {
                _llmCharacter.SetPrompt(_promptGenerator.GenerateFixedSection());
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
        /// LLM の最適設定を適用（戦術判断タスク用）
        /// </summary>
        private void ConfigureLLMOptimal()
        {
            // === ストリーミングとキャッシュ ===
            _llmCharacter.llm.contextSize = 4096;  // 重要！
            _llmCharacter.numPredict = 512;        // 重要！
            _llmCharacter.temperature = 0.0f;
            _llmCharacter.topK = 1;
            _llmCharacter.topP = 1.0f;
            _llmCharacter.seed = 42;               // 0 ではなく 42
            _llmCharacter.cachePrompt = true;

            UnityEngine.Debug.Log("LLM戦術判断用最適設定を適用しました");
            UnityEngine.Debug.Log($"- Temperature: {_llmCharacter.temperature} (決定論的)");
            UnityEngine.Debug.Log($"- Context Size: {_llmCharacter.llm.contextSize}");
            UnityEngine.Debug.Log($"- Max Tokens: {_llmCharacter.numPredict}");
        }

        /// <summary>
        /// Grammar設定（新プロンプト形式用JSON Schema）
        /// </summary>
        private void SetupGrammar()
        {

            _llmCharacter.grammarJSONString = _promptGenerator.GenerateGrammar();


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
            _promptGenerator = CreatePromptGenerator(_generatorType);
            var sampleData = LLMInputData.CreateForTestSituation(_situationType);
            var samplePrompt = _promptGenerator.GeneratePromptByData(sampleData);
            UnityEngine.Debug.Log($"Sample Prompt ({_situationType}):\n{_promptGenerator.GenerateFixedSection()}\n{samplePrompt}");
        }


        [ContextMenu("サンプルGrammar表示")]
        public void ShowSampleGrammar()
        {
            _promptGenerator = CreatePromptGenerator(_generatorType);
            var sampleGrammar = _promptGenerator.GenerateGrammar();
            UnityEngine.Debug.Log($"Sample Grammar:\n{sampleGrammar}");
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

        [ContextMenu("NLIタイプ一覧を表示")]
        public void ShowNLITypes()
        {
            var allTypes = CachePromptGeneratorWithNLI.GetAllInstructionTypes();
            var sb = new StringBuilder();
            sb.AppendLine("=== 利用可能なNLIタイプ ===");
            foreach (var nliType in allTypes)
            {
                string shortName = CachePromptGeneratorWithNLI.GetInstructionShortName(nliType);
                string description = CachePromptGeneratorWithNLI.GetInstructionDescription(nliType);
                sb.AppendLine($"  {shortName}: {description}");
            }
            UnityEngine.Debug.Log(sb.ToString());
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
                    newData.CurrentStrategy = _currentTestData.CurrentStrategy;
                    _currentTestData = newData;
                }

                // ランダムNLIモードの場合、各イテレーションでNLIタイプを変更
                if (IsRandomNLIType(_generatorType) && _nliGenerator != null)
                {
                    var randomNLI = GetRandomNLIType();
                    _nliGenerator.CurrentInstructionType = randomNLI;

                    if (_showProgressInConsole)
                    {
                        UnityEngine.Debug.Log($"  NLIタイプ: {CachePromptGeneratorWithNLI.GetInstructionShortName(randomNLI)}");
                    }
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
                iteration = iteration + 1
            };

            // NLIタイプを記録
            if (_nliGenerator != null)
            {
                testResult.nliType = CachePromptGeneratorWithNLI.GetInstructionShortName(_nliGenerator.CurrentInstructionType);
            }

            var stopwatch = Stopwatch.StartNew();

            // 基本プロンプト生成
            string basePrompt = _promptGenerator.GeneratePromptByData(_currentTestData);

            // RAG検索（非同期処理を同期的に待つ）
            string[] ragResults = new string[0];

            if (_useRAG && _rag != null)
            {
                // 戦況に応じた検索クエリを生成
                string searchQuery = GenerateRAGSearchQuery(_currentTestData);

                var ragTask = SearchRAGKnowledge(searchQuery);
                yield return new WaitUntil(() => ragTask.IsCompleted);

                var ragResult = ragTask.Result;
                ragResults = ragResult.results;
            }

            // RAG結果をプロンプトに統合
            string fullPrompt = IntegrateRAGContext(basePrompt, ragResults);

            testResult.prompt = fullPrompt;
            testResult.situationType = GetCurrentSituationType(iteration).ToString();

            if (_showDetailedTiming)
            {
                UnityEngine.Debug.Log($"プロンプト生成完了: {fullPrompt.Length}文字 {EstimateTokenCount(fullPrompt)}トークン");
            }
            if (iteration == 0)
            {
                // _llmCharacter.saveCache = true;
                // _llmCharacter.save = "domain_session";
                // Debug.Log($"LLMキャッシュ保存を有効化{_llmCharacter.GetCacheSavePath("domain_session")}");


                _llmCharacter.Warmup(_llmCharacter.prompt);
                //   _llmCharacter.cachePrompt = false;
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
            if (data.CurrentStrategy != null)
            {
                if (_generatorType == PromptGeneratorType.English)
                {
                    queryBuilder.Append($"Previous tactics: {data.CurrentStrategy.BasicTactic}. ");
                }
                else
                {
                    queryBuilder.Append($"前回の戦術: {data.CurrentStrategy.BasicTactic}。");
                }
            }

            // 体力・エネルギー状況に応じたクエリ
            float healthRatio = data.PlayerData.Hp / 100f;
            float energyRatio = data.PlayerData.Energy / 100f;

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

                strategy = StrategyData.FromJsonEnglish(jsonResponse);


                // LastStrategyを更新（次のイテレーションで使用）
                _currentTestData.CurrentStrategy = strategy;

                if (_showProgressInConsole)
                {
                    UnityEngine.Debug.Log($"LastStrategy更新: 基本戦術={strategy.BasicTactic}");
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
            _llmCharacter.ClearChat();
        }

        private void ValidateResponse(TestResult testResult)
        {
            try
            {
                var parsedResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(testResult.response);
                testResult.isValidJson = true;

                bool hasRequiredFields;

                // Cache系（NLI含む）の場合はPascalCase形式をチェック
                if (_generatorType == PromptGeneratorType.Cache ||
                    _generatorType == PromptGeneratorType.Tuned ||
                    IsNLIType(_generatorType))
                {
                    hasRequiredFields =
                        parsedResponse.ContainsKey("AnalysisResult") &&
                        parsedResponse.ContainsKey("BasicTactic") &&
                        parsedResponse.ContainsKey("AttackCriteria") &&
                        parsedResponse.ContainsKey("ContinuousAttackCriteria") &&
                        parsedResponse.ContainsKey("DefenseCriteria") &&
                        parsedResponse.ContainsKey("ContinuousDefenseCriteria");

                    // 戦術タイプを記録
                    if (parsedResponse.ContainsKey("BasicTactic"))
                    {
                        testResult.tacticsType = parsedResponse["BasicTactic"]?.ToString();
                    }
                }
                else if (_generatorType == PromptGeneratorType.English ||
                         _generatorType == PromptGeneratorType.Main ||
                         _generatorType == PromptGeneratorType.Fixed_Eng ||
                         _generatorType == PromptGeneratorType.Eng_Rag)
                {
                    hasRequiredFields =
                        parsedResponse.ContainsKey("analysis_result") &&
                        parsedResponse.ContainsKey("basic_tactics") &&
                        parsedResponse.ContainsKey("attack_judgment_criteria") &&
                        parsedResponse.ContainsKey("continuous_attack_judgment_criteria") &&
                        parsedResponse.ContainsKey("defense_judgment_criteria") &&
                        parsedResponse.ContainsKey("continuous_defense_judgment_criteria");

                    // 戦術タイプを記録
                    if (parsedResponse.ContainsKey("basic_tactics"))
                    {
                        testResult.tacticsType = parsedResponse["basic_tactics"]?.ToString();
                    }
                }
                else
                {
                    // 日本語形式
                    hasRequiredFields =
                        parsedResponse.ContainsKey("分析結果") &&
                        parsedResponse.ContainsKey("基本戦術") &&
                        parsedResponse.ContainsKey("攻撃時判断基準") &&
                        parsedResponse.ContainsKey("連続攻撃時判断基準") &&
                        parsedResponse.ContainsKey("防御時判断基準") &&
                        parsedResponse.ContainsKey("連続防御時判断基準");

                    // 戦術タイプを記録
                    if (parsedResponse.ContainsKey("基本戦術"))
                    {
                        testResult.tacticsType = parsedResponse["基本戦術"]?.ToString();
                    }
                }

                if (!hasRequiredFields)
                {
                    testResult.error = "必要なフィールドが不足しています";
                }

                if (_showProgressInConsole)
                {
                    UnityEngine.Debug.Log($"応答検証: JSON形式={testResult.isValidJson}, 必須フィールド={hasRequiredFields}, 戦術={testResult.tacticsType}");
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

                // NLI統計
                if (!string.IsNullOrEmpty(result.nliType))
                {
                    if (!results.nliTypeCounts.ContainsKey(result.nliType))
                        results.nliTypeCounts[result.nliType] = 0;
                    results.nliTypeCounts[result.nliType]++;
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

            // NLI統計
            if (results.nliTypeCounts.Count > 0)
            {
                report.AppendLine("【自然言語指示(NLI)統計】");
                foreach (var kvp in results.nliTypeCounts)
                {
                    report.AppendLine($"  {kvp.Key}: {kvp.Value}回");
                }
                report.AppendLine();
            }

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

                // 戦術タイプ
                if (!string.IsNullOrEmpty(result.tacticsType))
                {
                    report.AppendLine($"  戦術タイプ: {result.tacticsType}");
                }

                // NLIタイプ
                if (!string.IsNullOrEmpty(result.nliType))
                {
                    report.AppendLine($"  NLIタイプ: {result.nliType}");
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
            csv.AppendLine("Iteration,Timestamp,SituationType,IsSuccessful,IsValidJson,ResponseTimeSeconds,TokensPerSecond,TokenCount,PromptLength,ResponseLength,TacticsType,NLIType,Error");

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
                              $"\"{result.tacticsType ?? ""}\"," +
                              $"\"{result.nliType ?? ""}\"," +
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

            // NLI統計
            if (results.nliTypeCounts.Count > 0)
            {
                summary.AppendLine($"NLIタイプ使用: {string.Join(", ", results.nliTypeCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
            }

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
        /// 指定フォルダのすべてのtxtファイルを読み込んでRAGを構築（エディター専用）
        /// </summary>
        [ContextMenu("指定フォルダからRAG構築")]
        public void BuildRAGFromFolder()
        {
            if (_rag == null)
            {
                UnityEngine.Debug.LogError("RAGコンポーネントが設定されていません");
                return;
            }

            if (string.IsNullOrEmpty(_ragFolderPath))
            {
                UnityEngine.Debug.LogError("RAGフォルダパスが設定されていません");
                return;
            }

            if (!System.IO.Directory.Exists(_ragFolderPath))
            {
                UnityEngine.Debug.LogError($"フォルダが存在しません: {_ragFolderPath}");
                return;
            }

            StartCoroutine(BuildRAGFromFolderCoroutine());
        }

        private IEnumerator BuildRAGFromFolderCoroutine()
        {
            UnityEngine.Debug.Log($"RAG構築開始: {_ragFolderPath}");

            var buildTask = BuildRAGFromFolderAsync();
            yield return new WaitUntil(() => buildTask.IsCompleted);

            if (buildTask.Exception != null)
            {
                UnityEngine.Debug.LogError($"RAG構築エラー: {buildTask.Exception.Message}");
            }
            else
            {
                int loadedCount = buildTask.Result;
                UnityEngine.Debug.Log($"✅ RAG構築完了: {loadedCount}ファイル");
            }
        }

        /// <summary>
        /// フォルダ内のすべてのtxtファイルを非同期で読み込み
        /// </summary>
        private async System.Threading.Tasks.Task<int> BuildRAGFromFolderAsync()
        {
            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // すべての.txtファイルを取得
            string[] txtFiles = System.IO.Directory.GetFiles(_ragFolderPath, "*.txt", System.IO.SearchOption.AllDirectories);

            if (txtFiles.Length == 0)
            {
                UnityEngine.Debug.LogWarning($"txtファイルが見つかりません: {_ragFolderPath}");
                return 0;
            }

            UnityEngine.Debug.Log($"📂 {txtFiles.Length}個のtxtファイルを発見");

            int loadedCount = 0;
            int errorCount = 0;
            int totalChunks = 0;

            for (int fileIndex = 0; fileIndex < txtFiles.Length; fileIndex++)
            {
                string filePath = txtFiles[fileIndex];
                var fileStopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    // ファイル名を取得（拡張子なし）
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                    UnityEngine.Debug.Log($"⏳ [{fileIndex + 1}/{txtFiles.Length}] {fileName} を処理中...");

                    // ファイル内容を読み込み
                    string content = System.IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8);

                    if (string.IsNullOrEmpty(content))
                    {
                        UnityEngine.Debug.LogWarning($"⚠️ 空のファイル: {fileName}");
                        continue;
                    }

                    // チャンク数を推定
                    int estimatedChunks = EstimateChunkCount(content);
                    UnityEngine.Debug.Log($"  📄 ファイルサイズ: {content.Length}文字 (推定{estimatedChunks}チャンク)");

                    // RAGに追加（カテゴリ名はファイル名）
                    UnityEngine.Debug.Log($"  🔄 埋め込み生成中... (これには時間がかかります)");
                    await _rag.Add(content, fileName);

                    fileStopwatch.Stop();
                    totalChunks += estimatedChunks;
                    loadedCount++;

                    UnityEngine.Debug.Log($"  ✅ 完了 ({fileStopwatch.ElapsedMilliseconds / 1000.0:F1}秒)");

                    // 進捗サマリー
                    float progress = (float)(fileIndex + 1) / txtFiles.Length * 100f;
                    float elapsedMinutes = totalStopwatch.ElapsedMilliseconds / 60000.0f;
                    float estimatedTotalMinutes = elapsedMinutes / progress * 100f;
                    float remainingMinutes = estimatedTotalMinutes - elapsedMinutes;

                    UnityEngine.Debug.Log($"📊 進捗: {progress:F1}% ({fileIndex + 1}/{txtFiles.Length}) - 経過: {elapsedMinutes:F1}分 / 残り約: {remainingMinutes:F1}分");
                }
                catch (System.Exception ex)
                {
                    errorCount++;
                    UnityEngine.Debug.LogError($"❌ {System.IO.Path.GetFileName(filePath)} の読み込みに失敗: {ex.Message}");
                }
            }

            totalStopwatch.Stop();

            if (loadedCount > 0)
            {
                // データベースを保存
                try
                {
                    UnityEngine.Debug.Log($"💾 RAGデータベースを保存中...");
                    _rag.Save("tactical_knowledge.zip");
                    UnityEngine.Debug.Log($"✅ データベース保存完了: tactical_knowledge.zip");
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"データベース保存エラー: {ex.Message}");
                }
            }

            UnityEngine.Debug.Log($"\n{'='} RAG構築完了 {'='}\n");
            UnityEngine.Debug.Log($"⏱️  総処理時間: {totalStopwatch.ElapsedMilliseconds / 60000.0:F2}分 ({totalStopwatch.ElapsedMilliseconds / 1000.0:F1}秒)");
            UnityEngine.Debug.Log($"✅ 成功: {loadedCount}ファイル");
            UnityEngine.Debug.Log($"❌ 失敗: {errorCount}ファイル");
            UnityEngine.Debug.Log($"📦 推定総チャンク数: {totalChunks}");
            UnityEngine.Debug.Log($"⚡ 平均処理時間: {(totalStopwatch.ElapsedMilliseconds / 1000.0) / loadedCount:F1}秒/ファイル");

            return loadedCount;
        }

        /// <summary>
        /// テキストから推定チャンク数を計算
        /// </summary>
        private int EstimateChunkCount(string text)
        {
            // SentenceSplitterのデフォルト設定を想定
            // 512トークン/チャンク、オーバーラップ100として推定
            int estimatedTokens = text.Length / 2; // 日本語は約2文字/トークン
            int chunkSize = 512;
            int overlap = 100;

            if (estimatedTokens <= chunkSize)
            {
                return 1;
            }

            // オーバーラップを考慮したチャンク数
            int effectiveChunkSize = chunkSize - overlap;
            return (int)Math.Ceiling((double)(estimatedTokens - chunkSize) / effectiveChunkSize) + 1;
        }

        /// <summary>
        /// RAGデータベースの内容を確認（エディター専用）
        /// </summary>
        [ContextMenu("RAGデータベース内容を確認")]
        public void InspectRAGDatabase()
        {
            if (_rag == null)
            {
                UnityEngine.Debug.LogError("RAGコンポーネントが設定されていません");
                return;
            }

            StartCoroutine(InspectRAGCoroutine());
        }

        private IEnumerator InspectRAGCoroutine()
        {
            string dbPath = System.IO.Path.Combine(Application.streamingAssetsPath, "tactical_knowledge.zip");

            if (!System.IO.File.Exists(dbPath))
            {
                UnityEngine.Debug.LogWarning("RAGデータベースが存在しません。先に構築してください。");
                yield break;
            }

            UnityEngine.Debug.Log($"=== RAGデータベース情報 ===");
            UnityEngine.Debug.Log($"パス: {dbPath}");

            System.IO.FileInfo fileInfo = new System.IO.FileInfo(dbPath);
            UnityEngine.Debug.Log($"サイズ: {fileInfo.Length / 1024.0:F2} KB");
            UnityEngine.Debug.Log($"作成日時: {fileInfo.CreationTime:yyyy/MM/dd HH:mm:ss}");
            UnityEngine.Debug.Log($"最終更新: {fileInfo.LastWriteTime:yyyy/MM/dd HH:mm:ss}");

            // テスト検索
            string[] testQueries = new string[]
            {
                "攻撃型",
                "防御",
                "エネルギー",
                "連続攻撃"
            };

            UnityEngine.Debug.Log($"\n=== テスト検索実行 ===");

            foreach (string query in testQueries)
            {
                var searchTask = _rag.Search(query, 2);
                yield return new WaitUntil(() => searchTask.IsCompleted);

                var (results, distances) = searchTask.Result;

                UnityEngine.Debug.Log($"\nクエリ: 「{query}」");
                UnityEngine.Debug.Log($"結果数: {results.Length}件");

                for (int i = 0; i < results.Length; i++)
                {
                    string preview = results[i].Length > 100
                        ? results[i].Substring(0, 100) + "..."
                        : results[i];
                    UnityEngine.Debug.Log($"  [{i + 1}] 距離: {distances[i]:F3} - {preview}");
                }
            }
        }

        /// <summary>
        /// 指定フォルダ内のtxtファイル一覧を表示（エディター専用）
        /// </summary>
        [ContextMenu("フォルダ内のtxtファイル一覧を表示")]
        public void ShowTxtFilesInFolder()
        {
            if (string.IsNullOrEmpty(_ragFolderPath))
            {
                UnityEngine.Debug.LogError("RAGフォルダパスが設定されていません");
                return;
            }

            if (!System.IO.Directory.Exists(_ragFolderPath))
            {
                UnityEngine.Debug.LogError($"フォルダが存在しません: {_ragFolderPath}");
                return;
            }

            string[] txtFiles = System.IO.Directory.GetFiles(_ragFolderPath, "*.txt", System.IO.SearchOption.AllDirectories);

            UnityEngine.Debug.Log($"=== {_ragFolderPath} 内のtxtファイル ===");
            UnityEngine.Debug.Log($"合計: {txtFiles.Length}ファイル\n");

            if (txtFiles.Length == 0)
            {
                UnityEngine.Debug.LogWarning("txtファイルが見つかりません");
                return;
            }

            long totalSize = 0;

            for (int i = 0; i < txtFiles.Length; i++)
            {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(txtFiles[i]);
                totalSize += fileInfo.Length;

                string relativePath = txtFiles[i].Replace(_ragFolderPath, "").TrimStart('\\', '/');
                UnityEngine.Debug.Log($"[{i + 1}] {relativePath} ({fileInfo.Length / 1024.0:F2} KB)");
            }

            UnityEngine.Debug.Log($"\n合計サイズ: {totalSize / 1024.0:F2} KB");
        }

        /// <summary>
        /// RAGデータベースを手動で再構築
        /// </summary>
        [ContextMenu("RAGデータベースを再構築")]
        public void RebuildRAGDatabase()
        {
            if (_rag == null)
            {
                UnityEngine.Debug.LogError("RAGコンポーネントが設定されていません");
                return;
            }

            StartCoroutine(RebuildRAGCoroutine());
        }

        private IEnumerator RebuildRAGCoroutine()
        {
            UnityEngine.Debug.Log("RAGデータベースを再構築中...");

            // 既存データベースを削除
            string dbPath = System.IO.Path.Combine(Application.streamingAssetsPath, "tactical_knowledge.zip");
            if (System.IO.File.Exists(dbPath))
            {
                System.IO.File.Delete(dbPath);
                UnityEngine.Debug.Log("既存データベースを削除しました");
            }

            // 再初期化
            var initTask = InitializeRAG();
            yield return new WaitUntil(() => initTask.IsCompleted);

            if (initTask.Exception != null)
            {
                UnityEngine.Debug.LogError($"RAG再構築エラー: {initTask.Exception.Message}");
            }
            else
            {
                UnityEngine.Debug.Log("✅ RAGデータベース再構築完了");
            }
        }

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
            int nextType = ((int)_generatorType + 1) % Enum.GetValues(typeof(PromptGeneratorType)).Length;
            _generatorType = (PromptGeneratorType)nextType;

            _promptGenerator = CreatePromptGenerator(_generatorType);
            SetupSystemPrompt();
            SetupGrammar();

            UnityEngine.Debug.Log($"プロンプト生成器を {_generatorType} に変更しました");
        }

        #endregion
    }
}