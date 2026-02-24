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
        Fixed_Eng,
        Main,
        Cache,
        Tuned,
        Experimental,

        // === ルールベース系NLI ===
        NLI_AggressiveFinisher,
        NLI_AggressiveDisruptor,
        NLI_DefensiveSurvivor,
        NLI_DefensiveCounter,
        NLI_BalancedAdaptive,
        NLI_AnalyticalLearner,
        NLI_EnduranceManager,

        // === 自然言語系NLI ===
        NLI_CorneredBeast,
        NLI_Finisher,
        NLI_FrontRunner,
        NLI_PatternBreaker,
        NLI_MomentumRider,
        NLI_StaminaManager,
        NLI_CounterPuncher,
        NLI_Berserker,
        NLI_Tactician,
        NLI_WaterFlow,

        // === ランダム選択 ===
        NLI_Random,
        NLI_Random_RuleBased,
        NLI_Random_Natural
    }

    /// <summary>
    /// LLM for Unityを使用した連続的な思考テスト用コンポーネント
    /// 分析観点: 応答速度 / 戦術タイプ分布 / NLI傾向 / 応答品質
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

        [Header("LLM最適化設定")]
        [SerializeField] private bool _autoConfigureLLM = true;
        [SerializeField] private bool _useGrammar = true;

        [Header("戦況設定")]
        [SerializeField] private TestSituationType _situationType = TestSituationType.拮抗;
        [SerializeField] private bool _useMixedSituations = true;

        [Header("デバッグ表示")]
        [SerializeField] private bool _showProgressInConsole = true;
        [SerializeField] private bool _showDetailedTiming = true;
        [SerializeField] private bool _validateResponses = true;
        [SerializeField] private bool _showPerformanceMetrics = true;

        // ----------------------------------------
        // プライベートフィールド
        // ----------------------------------------

        /// <summary>アクティブなプロンプト生成器（非NLI時はこちらを使う）</summary>
        private PromptGeneratorBase _promptGenerator;

        /// <summary>
        /// NLI用生成器。NLI系GeneratorTypeの場合のみ非null。
        /// ランダムNLIモードではイテレーションごとに再生成される。
        /// </summary>
        private NLIPromptGenerator _nliGenerator;

        private Dictionary<TestSituationType, LLMInputData> _baseTestData;
        private List<TestResult> _testResults;
        private bool _isTestRunning;
        private int _currentIteration;
        private Stopwatch _totalStopwatch;
        private string _currentSessionId;
        private PerformanceMetrics _performanceMetrics;
        private LLMInputData _currentTestData;

        // ----------------------------------------
        // データクラス
        // ----------------------------------------

        private class PerformanceMetrics
        {
            public double TotalTokensGenerated;
            public List<double> TokensPerSecondHistory = new List<double>();
        }

        /// <summary>1回のテスト結果</summary>
        [System.Serializable]
        private class TestResult
        {
            public int Iteration;
            public DateTime Timestamp;
            public string SituationType;

            // 【応答速度】
            public double ResponseTimeSeconds;
            public double TokensPerSecond;
            public int ResponseTokenCount;

            // 【戦術タイプ】
            public string BasicTactic;

            // 【NLI傾向】
            public string NLIType;

            // 【応答品質】
            public bool IsSuccessful;
            public bool IsValidJson;
            public bool HasRequiredFields;
            public string Error;

            // 生データ（詳細確認用）
            public string Prompt;
            public string Response;

            public TestResult() { Timestamp = DateTime.Now; }
        }

        /// <summary>セッション全体の集計結果</summary>
        [System.Serializable]
        private class IntegratedTestResults
        {
            public string SessionId;
            public string PromptGeneratorType;
            public DateTime StartTime;
            public DateTime EndTime;
            public double TotalTimeSeconds;

            // 【応答速度】
            public double AverageResponseTimeSeconds;
            public double MinResponseTimeSeconds;
            public double MaxResponseTimeSeconds;
            public double AverageTokensPerSecond;

            // 【戦術タイプ分布】
            public Dictionary<string, int> TacticTypeCounts;

            // 【NLI傾向】
            public Dictionary<string, int> NLITypeCounts;

            // 【応答品質】
            public int TotalTests;
            public int SuccessfulTests;
            public int FailedTests;
            public int ValidJsonCount;
            public int RequiredFieldsOkCount;
            public double SuccessRate;
            public double JsonValidRate;
            public double RequiredFieldsRate;
            public List<string> Errors;

            // 戦況分布（補助情報）
            public Dictionary<string, int> SituationTypeCounts;

            // 個別テスト生データ
            public List<TestResult> TestResults;

            public IntegratedTestResults()
            {
                TacticTypeCounts = new Dictionary<string, int>();
                NLITypeCounts = new Dictionary<string, int>();
                Errors = new List<string>();
                SituationTypeCounts = new Dictionary<string, int>();
                TestResults = new List<TestResult>();
            }
        }

        // ----------------------------------------
        // 初期化
        // ----------------------------------------

        private void Start()
        {
            InitializeTest();

            if (_autoStartOnPlay)
                StartCoroutine(RunContinuousTest());
        }

        private void InitializeTest()
        {
            _testResults = new List<TestResult>();
            _totalStopwatch = new Stopwatch();
            _currentSessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _performanceMetrics = new PerformanceMetrics();

            CreatePromptGenerator(_generatorType, out _promptGenerator, out _nliGenerator);

            if (!Directory.Exists(_outputDirectoryPath))
                Directory.CreateDirectory(_outputDirectoryPath);

            if (_autoConfigureLLM && _llmCharacter != null)
                ConfigureLLMOptimal();

            if (_useGrammar && _llmCharacter != null)
                SetupGrammar();

            SetupSystemPrompt();
            GenerateTestData();

            if (_showProgressInConsole)
            {
                Debug.Log($"テスト初期化完了 - セッションID: {_currentSessionId}");
                Debug.Log($"プロンプト生成器: {_generatorType}");

                if (_nliGenerator != null)
                {
                    string nliInfo = IsRandomNLIType(_generatorType)
                        ? "ランダム（各イテレーションで変更）"
                        : NLIPromptGenerator.GetInstructionDescription(_nliGenerator.InstructionType);
                    Debug.Log($"自然言語指示: {nliInfo}");
                }
            }
        }

        // ----------------------------------------
        // プロンプト生成器の構築
        // ----------------------------------------

        /// <summary>
        /// PromptGeneratorTypeからプロンプト生成器を構築する。
        /// NLI系の場合は _nliGenerator にも設定し、_promptGenerator と同じインスタンスを指す。
        /// ランダムNLIの場合はこのメソッドで初期NLIタイプも決定する。
        /// </summary>
        private void CreatePromptGenerator(
            PromptGeneratorType type,
            out PromptGeneratorBase promptGenerator,
            out NLIPromptGenerator nliGenerator)
        {
            nliGenerator = null;

            if (IsNLIType(type))
            {
                var nliType = IsRandomNLIType(type)
                    ? GetRandomNLITypeFor(type)
                    : GetNLITypeFromGeneratorType(type);

                nliGenerator = new NLIPromptGenerator(nliType);
                promptGenerator = nliGenerator;

                Debug.Log($"プロンプト生成器: NLIPromptGenerator ({NLIPromptGenerator.GetInstructionShortName(nliType)})");
                return;
            }

            promptGenerator = type switch
            {
                PromptGeneratorType.Japanese => new JapanesePromptGenerator(),
                PromptGeneratorType.English => new EnglishPromptGenerator(),
                PromptGeneratorType.Fixed_Eng => new FixedEnglishGenerator(),
                PromptGeneratorType.Main => new MainPromptGenerator(),
                PromptGeneratorType.Cache => new CachePromptGenerator(),
                PromptGeneratorType.Tuned => new TunedPromptGenerator(),
                _ => new JapanesePromptGenerator()
            };

            Debug.Log($"プロンプト生成器: {promptGenerator.GetType().Name}");
        }

        // ----------------------------------------
        // NLIタイプ判定・変換ヘルパー
        // ----------------------------------------

        private static bool IsNLIType(PromptGeneratorType type)
        {
            return type switch
            {
                PromptGeneratorType.NLI_AggressiveFinisher => true,
                PromptGeneratorType.NLI_AggressiveDisruptor => true,
                PromptGeneratorType.NLI_DefensiveSurvivor => true,
                PromptGeneratorType.NLI_DefensiveCounter => true,
                PromptGeneratorType.NLI_BalancedAdaptive => true,
                PromptGeneratorType.NLI_AnalyticalLearner => true,
                PromptGeneratorType.NLI_EnduranceManager => true,
                PromptGeneratorType.NLI_CorneredBeast => true,
                PromptGeneratorType.NLI_Finisher => true,
                PromptGeneratorType.NLI_FrontRunner => true,
                PromptGeneratorType.NLI_PatternBreaker => true,
                PromptGeneratorType.NLI_MomentumRider => true,
                PromptGeneratorType.NLI_StaminaManager => true,
                PromptGeneratorType.NLI_CounterPuncher => true,
                PromptGeneratorType.NLI_Berserker => true,
                PromptGeneratorType.NLI_Tactician => true,
                PromptGeneratorType.NLI_WaterFlow => true,
                PromptGeneratorType.NLI_Random => true,
                PromptGeneratorType.NLI_Random_RuleBased => true,
                PromptGeneratorType.NLI_Random_Natural => true,
                _ => false
            };
        }

        private static bool IsRandomNLIType(PromptGeneratorType type)
        {
            return type == PromptGeneratorType.NLI_Random ||
                   type == PromptGeneratorType.NLI_Random_RuleBased ||
                   type == PromptGeneratorType.NLI_Random_Natural;
        }

        private static NaturalLanguageInstructionType GetNLITypeFromGeneratorType(PromptGeneratorType type)
        {
            return type switch
            {
                PromptGeneratorType.NLI_AggressiveFinisher => NaturalLanguageInstructionType.AggressiveFinisher,
                PromptGeneratorType.NLI_AggressiveDisruptor => NaturalLanguageInstructionType.AggressiveDisruptor,
                PromptGeneratorType.NLI_DefensiveSurvivor => NaturalLanguageInstructionType.DefensiveSurvivor,
                PromptGeneratorType.NLI_DefensiveCounter => NaturalLanguageInstructionType.DefensiveCounter,
                PromptGeneratorType.NLI_BalancedAdaptive => NaturalLanguageInstructionType.BalancedAdaptive,
                PromptGeneratorType.NLI_AnalyticalLearner => NaturalLanguageInstructionType.AnalyticalLearner,
                PromptGeneratorType.NLI_EnduranceManager => NaturalLanguageInstructionType.EnduranceManager,
                PromptGeneratorType.NLI_CorneredBeast => NaturalLanguageInstructionType.CorneredBeast,
                PromptGeneratorType.NLI_Finisher => NaturalLanguageInstructionType.Finisher,
                PromptGeneratorType.NLI_FrontRunner => NaturalLanguageInstructionType.FrontRunner,
                PromptGeneratorType.NLI_PatternBreaker => NaturalLanguageInstructionType.PatternBreaker,
                PromptGeneratorType.NLI_MomentumRider => NaturalLanguageInstructionType.MomentumRider,
                PromptGeneratorType.NLI_StaminaManager => NaturalLanguageInstructionType.StaminaManager,
                PromptGeneratorType.NLI_CounterPuncher => NaturalLanguageInstructionType.CounterPuncher,
                PromptGeneratorType.NLI_Berserker => NaturalLanguageInstructionType.Berserker,
                PromptGeneratorType.NLI_Tactician => NaturalLanguageInstructionType.Tactician,
                PromptGeneratorType.NLI_WaterFlow => NaturalLanguageInstructionType.WaterFlow,
                _ => throw new ArgumentException($"NLIタイプへの変換不可: {type}")
            };
        }

        private NaturalLanguageInstructionType GetRandomNLITypeFor(PromptGeneratorType type)
        {
            NaturalLanguageInstructionType[] candidates = type switch
            {
                PromptGeneratorType.NLI_Random_RuleBased => NLIPromptGenerator.GetRuleBasedTypes(),
                PromptGeneratorType.NLI_Random_Natural => NLIPromptGenerator.GetNaturalLanguageTypes(),
                _ => NLIPromptGenerator.GetActiveInstructionTypes()
            };

            return candidates[UnityEngine.Random.Range(0, candidates.Length)];
        }

        // ----------------------------------------
        // LLM設定
        // ----------------------------------------

        private void SetupSystemPrompt()
        {
            if (_generatorType == PromptGeneratorType.English || _generatorType == PromptGeneratorType.Main)
            {
                _llmCharacter.prompt = @"You are a tactical combat AI assistant.
Analyze battle data and make strategic decisions in strict JSON format.
Always respond with ONLY valid JSON, no markdown, no explanations.";
            }
            else if (IsNLIType(_generatorType) ||
                     _generatorType == PromptGeneratorType.Cache ||
                     _generatorType == PromptGeneratorType.Tuned)
            {
                // NLIPromptGenerator.GenerateFixedSection() は絞り込み済み選択肢を含む
                _llmCharacter.SetPrompt(_promptGenerator.GenerateFixedSection());
            }
            else
            {
                _llmCharacter.prompt = @"あなたは戦術的な戦闘AIアシスタントです。
戦闘データを分析し、厳密なJSON形式で戦略的判断を提供してください。
常に有効なJSONのみで応答し、マークダウンや説明文は含めないでください。";
            }

            _llmCharacter.playerName = "User";
            _llmCharacter.AIName = "TacticAI";
        }

        private void ConfigureLLMOptimal()
        {
            _llmCharacter.llm.contextSize = 4096;
            _llmCharacter.numPredict = 512;
            _llmCharacter.temperature = 0.0f;
            _llmCharacter.topK = 1;
            _llmCharacter.topP = 1.0f;
            _llmCharacter.seed = 42;
            _llmCharacter.cachePrompt = true;

            Debug.Log("LLM最適設定を適用: Temperature=0 (決定論的), Context=4096, MaxTokens=512");
        }

        private void SetupGrammar()
        {
            _llmCharacter.grammarJSONString = _promptGenerator.GenerateGrammar();
            Debug.Log($"Grammar設定完了 ({_generatorType})");
        }

        // ----------------------------------------
        // テストデータ
        // ----------------------------------------

        private void GenerateTestData()
        {
            _baseTestData = new Dictionary<TestSituationType, LLMInputData>();
            foreach (TestSituationType sit in Enum.GetValues(typeof(TestSituationType)))
                _baseTestData[sit] = LLMInputData.CreateForTestSituation(sit);

            _currentTestData = _baseTestData[_situationType];
        }

        private TestSituationType GetCurrentSituationType(int iteration)
        {
            if (_useMixedSituations)
            {
                var situations = Enum.GetValues(typeof(TestSituationType));
                return (TestSituationType)situations.GetValue(iteration % situations.Length);
            }
            return _situationType;
        }

        // ----------------------------------------
        // テスト実行
        // ----------------------------------------

        [ContextMenu("テスト開始")]
        public void StartTest()
        {
            if (!_isTestRunning)
                StartCoroutine(RunContinuousTest());
            else
                Debug.LogWarning("テストは既に実行中です");
        }

        [ContextMenu("テスト停止")]
        public void StopTest()
        {
            _isTestRunning = false;
            SaveIntegratedResults();
        }

        private IEnumerator RunContinuousTest()
        {
            _isTestRunning = true;
            _totalStopwatch.Start();
            _currentIteration = 0;

            Debug.Log($"連続思考テスト開始 - {_testIterations}回実行予定 ({_generatorType})");

            for (int i = 0; i < _testIterations && _isTestRunning; i++)
            {
                _currentIteration = i + 1;

                if (_showProgressInConsole)
                    Debug.Log($"テスト {_currentIteration}/{_testIterations} 開始");

                // 戦況データを更新（LastStrategyは前ターンから引き継ぐ）
                if (_generateRandomDataEachIteration && i > 0)
                {
                    var newData = LLMInputData.CreateForTestSituation(GetCurrentSituationType(i));
                    newData.CurrentStrategy = _currentTestData.CurrentStrategy;
                    _currentTestData = newData;
                }

                // ランダムNLIモード:
                // NLIPromptGeneratorはNLIタイプ固定構築のため、毎回再生成してGrammarも更新する
                if (IsRandomNLIType(_generatorType))
                {
                    RebuildNLIGeneratorForRandom();
                }

                yield return StartCoroutine(ExecuteSingleTest(i));

                if (i < _testIterations - 1)
                    yield return new WaitForSeconds(_delayBetweenTests);
            }

            _totalStopwatch.Stop();
            _isTestRunning = false;

            Debug.Log("連続思考テスト完了");
            SaveIntegratedResults();
        }

        /// <summary>
        /// ランダムNLIモード用: NLIPromptGeneratorを再生成しGrammarとSystemPromptを更新する。
        /// NLIPromptGeneratorはNLIタイプ固定設計のため、タイプ変更時は再生成が必要。
        /// </summary>
        private void RebuildNLIGeneratorForRandom()
        {
            var newNLIType = GetRandomNLITypeFor(_generatorType);
            _nliGenerator = new NLIPromptGenerator(newNLIType);
            _promptGenerator = _nliGenerator;

            // Grammar・SystemPromptをLLMCharacterに反映
            if (_useGrammar && _llmCharacter != null)
                SetupGrammar();

            _llmCharacter.SetPrompt(_promptGenerator.GenerateFixedSection());

            if (_showProgressInConsole)
                Debug.Log($"  NLIタイプ切替: {NLIPromptGenerator.GetInstructionShortName(newNLIType)}");
        }

        private IEnumerator ExecuteSingleTest(int iteration)
        {
            var result = new TestResult
            {
                Iteration = iteration + 1,
                SituationType = GetCurrentSituationType(iteration).ToString(),
                NLIType = _nliGenerator != null
                    ? NLIPromptGenerator.GetInstructionShortName(_nliGenerator.InstructionType)
                    : ""
            };

            var stopwatch = Stopwatch.StartNew();

            string prompt = _promptGenerator.GeneratePromptByData(_currentTestData);
            result.Prompt = prompt;

            if (_showDetailedTiming)
                Debug.Log($"プロンプト生成完了: {prompt.Length}文字 / 約{EstimateTokenCount(prompt)}トークン");

            // 初回: Warmup
            if (iteration == 0)
                _llmCharacter.Warmup(_llmCharacter.prompt);

            yield return StartCoroutine(SendToLLM(prompt, result));

            stopwatch.Stop();
            result.ResponseTimeSeconds = stopwatch.ElapsedMilliseconds / 1000.0;
            result.IsSuccessful = !string.IsNullOrEmpty(result.Response);

            // 応答品質の検証 & LastStrategy更新
            if (_validateResponses && result.IsSuccessful)
            {
                ValidateResponse(result);
                UpdateLastStrategyFromResponse(result.Response);
            }

            // パフォーマンスメトリクス
            if (result.IsSuccessful)
            {
                result.ResponseTokenCount = EstimateTokenCount(result.Response);
                result.TokensPerSecond = result.ResponseTokenCount / result.ResponseTimeSeconds;
                _performanceMetrics.TokensPerSecondHistory.Add(result.TokensPerSecond);
                _performanceMetrics.TotalTokensGenerated += result.ResponseTokenCount;
            }

            if (_showDetailedTiming)
            {
                Debug.Log($"テスト {result.Iteration} 完了: {result.ResponseTimeSeconds:F2}秒");
                if (_showPerformanceMetrics)
                    Debug.Log($"  {result.ResponseTokenCount}トークン / {result.TokensPerSecond:F1} tokens/秒");
            }

            _testResults.Add(result);
        }

        private IEnumerator SendToLLM(string prompt, TestResult result)
        {
            if (_llmCharacter == null)
            {
                result.Error = "LLMCharacterが設定されていません";
                yield break;
            }

            bool responseReceived = false;
            string response = "";

            _llmCharacter.Chat(prompt, r => { response = r; responseReceived = true; });

            float elapsed = 0f;
            while (!responseReceived && elapsed < _timeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!responseReceived)
                result.Error = $"タイムアウト ({_timeoutSeconds}秒)";
            else
                result.Response = response;

            Debug.Log($"LLM応答: {response}");
            _llmCharacter.ClearChat();
        }

        // ----------------------------------------
        // 応答品質の検証
        // ----------------------------------------

        private void ValidateResponse(TestResult result)
        {
            try
            {
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Response);
                result.IsValidJson = true;

                bool hasFields;

                if (IsNLIType(_generatorType) ||
                    _generatorType == PromptGeneratorType.Cache ||
                    _generatorType == PromptGeneratorType.Tuned)
                {
                    hasFields =
                        parsed.ContainsKey("AnalysisResult") &&
                        parsed.ContainsKey("BasicTactic") &&
                        parsed.ContainsKey("AttackCriteria") &&
                        parsed.ContainsKey("ContinuousAttackCriteria") &&
                        parsed.ContainsKey("DefenseCriteria") &&
                        parsed.ContainsKey("ContinuousDefenseCriteria");

                    if (parsed.ContainsKey("BasicTactic"))
                        result.BasicTactic = parsed["BasicTactic"]?.ToString();
                }
                else if (_generatorType == PromptGeneratorType.English ||
                         _generatorType == PromptGeneratorType.Main ||
                         _generatorType == PromptGeneratorType.Fixed_Eng)
                {
                    hasFields =
                        parsed.ContainsKey("analysis_result") &&
                        parsed.ContainsKey("basic_tactics") &&
                        parsed.ContainsKey("attack_judgment_criteria") &&
                        parsed.ContainsKey("continuous_attack_judgment_criteria") &&
                        parsed.ContainsKey("defense_judgment_criteria") &&
                        parsed.ContainsKey("continuous_defense_judgment_criteria");

                    if (parsed.ContainsKey("basic_tactics"))
                        result.BasicTactic = parsed["basic_tactics"]?.ToString();
                }
                else
                {
                    hasFields =
                        parsed.ContainsKey("分析結果") &&
                        parsed.ContainsKey("基本戦術") &&
                        parsed.ContainsKey("攻撃時判断基準") &&
                        parsed.ContainsKey("連続攻撃時判断基準") &&
                        parsed.ContainsKey("防御時判断基準") &&
                        parsed.ContainsKey("連続防御時判断基準");

                    if (parsed.ContainsKey("基本戦術"))
                        result.BasicTactic = parsed["基本戦術"]?.ToString();
                }

                result.HasRequiredFields = hasFields;
                if (!hasFields)
                    result.Error = "必要なフィールドが不足しています";

                if (_showProgressInConsole)
                    Debug.Log($"応答品質: JSON={result.IsValidJson}, 必須フィールド={hasFields}, 戦術={result.BasicTactic}");
            }
            catch (JsonException ex)
            {
                result.IsValidJson = false;
                result.Error = $"JSON解析エラー: {ex.Message}";
            }
        }

        private void UpdateLastStrategyFromResponse(string jsonResponse)
        {
            try
            {
                var strategy = StrategyData.FromJsonEnglish(jsonResponse);
                _currentTestData.CurrentStrategy = strategy;

                if (_showProgressInConsole)
                    Debug.Log($"LastStrategy更新: 基本戦術={strategy.BasicTactic}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LastStrategy更新失敗: {ex.Message}");
            }
        }

        private int EstimateTokenCount(string text)
        {
            if (_generatorType == PromptGeneratorType.English ||
                _generatorType == PromptGeneratorType.Main ||
                _generatorType == PromptGeneratorType.Fixed_Eng)
            {
                return (int)(text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length * 1.3);
            }
            return text.Length / 2;
        }

        // ----------------------------------------
        // 結果の保存
        // ----------------------------------------

        private void SaveIntegratedResults()
        {
            try
            {
                var integrated = CreateIntegratedResults();
                string tag = _generatorType.ToString();

                string jsonPath = Path.Combine(_outputDirectoryPath, $"{_filePrefix}_{tag}_Results_{_currentSessionId}.json");
                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(integrated, Formatting.Indented), Encoding.UTF8);

                string txtPath = Path.Combine(_outputDirectoryPath, $"{_filePrefix}_{tag}_Report_{_currentSessionId}.txt");
                File.WriteAllText(txtPath, CreateReadableReport(integrated), Encoding.UTF8);

                string csvPath = Path.Combine(_outputDirectoryPath, $"{_filePrefix}_{tag}_Stats_{_currentSessionId}.csv");
                File.WriteAllText(csvPath, CreateCsvReport(integrated), Encoding.UTF8);

                Debug.Log($"結果保存完了:\n- {Path.GetFileName(jsonPath)}\n- {Path.GetFileName(txtPath)}\n- {Path.GetFileName(csvPath)}");
                DisplaySummaryInConsole(integrated);
            }
            catch (Exception ex)
            {
                Debug.LogError($"結果の保存に失敗: {ex.Message}");
            }
        }

        private IntegratedTestResults CreateIntegratedResults()
        {
            var r = new IntegratedTestResults
            {
                SessionId = _currentSessionId,
                PromptGeneratorType = _generatorType.ToString(),
                StartTime = DateTime.Now.AddMilliseconds(-_totalStopwatch.ElapsedMilliseconds),
                EndTime = DateTime.Now,
                TotalTimeSeconds = _totalStopwatch.ElapsedMilliseconds / 1000.0,
                TotalTests = _testResults.Count,
                TestResults = _testResults
            };

            foreach (var t in _testResults)
            {
                if (t.IsSuccessful)
                    r.SuccessfulTests++;
                else
                {
                    r.FailedTests++;
                    if (!string.IsNullOrEmpty(t.Error))
                        r.Errors.Add($"Test{t.Iteration}: {t.Error}");
                }
                if (t.IsValidJson)
                    r.ValidJsonCount++;
                if (t.HasRequiredFields)
                    r.RequiredFieldsOkCount++;

                if (!string.IsNullOrEmpty(t.BasicTactic))
                {
                    if (!r.TacticTypeCounts.ContainsKey(t.BasicTactic))
                        r.TacticTypeCounts[t.BasicTactic] = 0;
                    r.TacticTypeCounts[t.BasicTactic]++;
                }

                if (!string.IsNullOrEmpty(t.NLIType))
                {
                    if (!r.NLITypeCounts.ContainsKey(t.NLIType))
                        r.NLITypeCounts[t.NLIType] = 0;
                    r.NLITypeCounts[t.NLIType]++;
                }

                if (!r.SituationTypeCounts.ContainsKey(t.SituationType))
                    r.SituationTypeCounts[t.SituationType] = 0;
                r.SituationTypeCounts[t.SituationType]++;
            }

            var succeeded = _testResults.Where(t => t.IsSuccessful).ToList();
            if (succeeded.Count > 0)
            {
                var times = succeeded.Select(t => t.ResponseTimeSeconds).ToList();
                r.AverageResponseTimeSeconds = times.Average();
                r.MinResponseTimeSeconds = times.Min();
                r.MaxResponseTimeSeconds = times.Max();
            }

            if (r.TotalTests > 0)
            {
                r.SuccessRate = (double)r.SuccessfulTests / r.TotalTests;
                r.JsonValidRate = (double)r.ValidJsonCount / r.TotalTests;
                r.RequiredFieldsRate = (double)r.RequiredFieldsOkCount / r.TotalTests;
            }

            if (_performanceMetrics.TokensPerSecondHistory.Count > 0)
                r.AverageTokensPerSecond = _performanceMetrics.TokensPerSecondHistory.Average();

            return r;
        }

        private string CreateReadableReport(IntegratedTestResults r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"  LLMテストレポート  [{r.PromptGeneratorType}]  セッション: {r.SessionId}");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"開始: {r.StartTime:yyyy/MM/dd HH:mm:ss}  終了: {r.EndTime:yyyy/MM/dd HH:mm:ss}");
            sb.AppendLine($"総実行時間: {r.TotalTimeSeconds:F2}秒");
            sb.AppendLine();

            sb.AppendLine("【応答速度】");
            sb.AppendLine($"  平均: {r.AverageResponseTimeSeconds:F2}秒  最短: {r.MinResponseTimeSeconds:F2}秒  最長: {r.MaxResponseTimeSeconds:F2}秒");
            sb.AppendLine($"  平均生成速度: {r.AverageTokensPerSecond:F1} tokens/秒");
            sb.AppendLine();

            sb.AppendLine("【応答品質】");
            sb.AppendLine($"  成功率: {r.SuccessRate:P1} ({r.SuccessfulTests}/{r.TotalTests})");
            sb.AppendLine($"  JSON有効率: {r.JsonValidRate:P1} ({r.ValidJsonCount}/{r.TotalTests})");
            sb.AppendLine($"  必須フィールド充足率: {r.RequiredFieldsRate:P1} ({r.RequiredFieldsOkCount}/{r.TotalTests})");
            if (r.Errors.Count > 0)
            {
                sb.AppendLine("  エラー:");
                foreach (var e in r.Errors)
                    sb.AppendLine($"    - {e}");
            }
            sb.AppendLine();

            sb.AppendLine("【戦術タイプ分布】");
            foreach (var kv in r.TacticTypeCounts.OrderByDescending(x => x.Value))
                sb.AppendLine($"  {kv.Key}: {kv.Value}回");
            sb.AppendLine();

            if (r.NLITypeCounts.Count > 0)
            {
                sb.AppendLine("【NLI傾向】");
                foreach (var kv in r.NLITypeCounts.OrderByDescending(x => x.Value))
                    sb.AppendLine($"  {kv.Key}: {kv.Value}回");
                sb.AppendLine();
            }

            sb.AppendLine("【戦況分布（補助）】");
            foreach (var kv in r.SituationTypeCounts)
                sb.AppendLine($"  {kv.Key}: {kv.Value}回");
            sb.AppendLine();

            sb.AppendLine("【個別テスト結果】");
            sb.AppendLine("--------------------------------------------------------------------------------");
            foreach (var t in r.TestResults)
            {
                sb.AppendLine($"■ テスト {t.Iteration}  [{t.Timestamp:HH:mm:ss}]  {t.SituationType}");
                sb.AppendLine($"  応答速度: {t.ResponseTimeSeconds:F2}秒 / {t.TokensPerSecond:F1} tokens/秒 ({t.ResponseTokenCount}tokens)");
                sb.AppendLine($"  応答品質: 成功={t.IsSuccessful}, JSON={t.IsValidJson}, フィールド充足={t.HasRequiredFields}");
                sb.AppendLine($"  戦術タイプ: {t.BasicTactic ?? "-"}");
                if (!string.IsNullOrEmpty(t.NLIType))
                    sb.AppendLine($"  NLIタイプ: {t.NLIType}");
                if (!string.IsNullOrEmpty(t.Error))
                    sb.AppendLine($"  エラー: {t.Error}");
                if (!string.IsNullOrEmpty(t.Response))
                    sb.AppendLine($"  応答: {t.Response}");
                sb.AppendLine("--------------------------------------------------------------------------------");
            }

            return sb.ToString();
        }

        private string CreateCsvReport(IntegratedTestResults r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Iteration,Timestamp,SituationType," +
                          "ResponseTimeSeconds,TokensPerSecond,ResponseTokenCount," +
                          "BasicTactic," +
                          "NLIType," +
                          "IsSuccessful,IsValidJson,HasRequiredFields,Error");

            foreach (var t in r.TestResults)
            {
                sb.AppendLine(
                    $"{t.Iteration}," +
                    $"{t.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                    $"{t.SituationType}," +
                    $"{t.ResponseTimeSeconds:F2}," +
                    $"{t.TokensPerSecond:F1}," +
                    $"{t.ResponseTokenCount}," +
                    $"\"{t.BasicTactic ?? ""}\"," +
                    $"\"{t.NLIType ?? ""}\"," +
                    $"{t.IsSuccessful}," +
                    $"{t.IsValidJson}," +
                    $"{t.HasRequiredFields}," +
                    $"\"{t.Error ?? ""}\"");
            }

            return sb.ToString();
        }

        private void DisplaySummaryInConsole(IntegratedTestResults r)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== テスト完了 [{r.PromptGeneratorType}] ===");
            sb.AppendLine($"[応答速度] 平均 {r.AverageResponseTimeSeconds:F2}秒 / {r.AverageTokensPerSecond:F1} tokens/秒");
            sb.AppendLine($"[応答品質] 成功率={r.SuccessRate:P1} / JSON={r.JsonValidRate:P1} / フィールド充足={r.RequiredFieldsRate:P1}");
            sb.AppendLine($"[戦術タイプ] {string.Join(", ", r.TacticTypeCounts.Select(kv => $"{kv.Key}:{kv.Value}"))}");
            if (r.NLITypeCounts.Count > 0)
                sb.AppendLine($"[NLI傾向] {string.Join(", ", r.NLITypeCounts.Select(kv => $"{kv.Key}:{kv.Value}"))}");
            sb.AppendLine($"総実行時間: {r.TotalTimeSeconds:F2}秒");
            Debug.Log(sb.ToString());
        }

        private void OnDestroy()
        {
            if (_isTestRunning)
                StopTest();
        }

        // ----------------------------------------
        // ContextMenuユーティリティ
        // ----------------------------------------

        [ContextMenu("テスト開始")]
        public void StartTestMenu() => StartTest();

        [ContextMenu("サンプルプロンプト表示")]
        public void ShowSamplePrompt()
        {
            CreatePromptGenerator(_generatorType, out var gen, out _);
            var data = LLMInputData.CreateForTestSituation(_situationType);
            Debug.Log($"=== Fixed Section ({_generatorType}) ===\n{gen.GenerateFixedSection()}" +
                      $"\n=== Dynamic Section ({_situationType}) ===\n{gen.GeneratePromptByData(data)}");
        }

        [ContextMenu("サンプルGrammar表示")]
        public void ShowSampleGrammar()
        {
            CreatePromptGenerator(_generatorType, out var gen, out _);
            Debug.Log($"=== Grammar ({_generatorType}) ===\n{gen.GenerateGrammar()}");
        }

        [ContextMenu("NLIタイプ一覧を表示")]
        public void ShowNLITypes()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ルールベース系 ===");
            foreach (var t in NLIPromptGenerator.GetRuleBasedTypes())
                sb.AppendLine($"  {NLIPromptGenerator.GetInstructionShortName(t)}: {NLIPromptGenerator.GetInstructionDescription(t)}");

            sb.AppendLine("=== 自然言語系 ===");
            foreach (var t in NLIPromptGenerator.GetNaturalLanguageTypes())
                sb.AppendLine($"  {NLIPromptGenerator.GetInstructionShortName(t)}: {NLIPromptGenerator.GetInstructionDescription(t)}");

            Debug.Log(sb.ToString());
        }

        [ContextMenu("Grammarを切り替え")]
        public void ToggleGrammar()
        {
            _useGrammar = !_useGrammar;
            if (_llmCharacter != null)
            {
                if (_useGrammar)
                    SetupGrammar();
                else
                    _llmCharacter.grammar = "";
            }
            Debug.Log($"Grammar: {(_useGrammar ? "有効" : "無効")}");
        }

        [ContextMenu("LLM設定を最適化")]
        public void OptimizeLLMSettings()
        {
            if (_llmCharacter != null)
                ConfigureLLMOptimal();
            else
                Debug.LogWarning("LLMCharacterが設定されていません");
        }

        [ContextMenu("パフォーマンスメトリクスをリセット")]
        public void ResetPerformanceMetrics()
        {
            _performanceMetrics = new PerformanceMetrics();
            Debug.Log("パフォーマンスメトリクスをリセットしました");
        }

        [ContextMenu("パフォーマンスメトリクスを表示")]
        public void ShowPerformanceMetrics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== パフォーマンスメトリクス ===");
            sb.AppendLine($"総トークン生成数: {_performanceMetrics.TotalTokensGenerated:F0}");
            if (_performanceMetrics.TokensPerSecondHistory.Count > 0)
            {
                sb.AppendLine($"平均: {_performanceMetrics.TokensPerSecondHistory.Average():F1} tokens/秒");
                sb.AppendLine($"最速: {_performanceMetrics.TokensPerSecondHistory.Max():F1} / 最遅: {_performanceMetrics.TokensPerSecondHistory.Min():F1}");
            }
            Debug.Log(sb.ToString());
        }

        [ContextMenu("プロンプト生成器を変更（次へ）")]
        public void ChangePromptGenerator()
        {
            int next = ((int)_generatorType + 1) % Enum.GetValues(typeof(PromptGeneratorType)).Length;
            _generatorType = (PromptGeneratorType)next;

            CreatePromptGenerator(_generatorType, out _promptGenerator, out _nliGenerator);
            SetupSystemPrompt();
            if (_useGrammar && _llmCharacter != null)
                SetupGrammar();

            Debug.Log($"プロンプト生成器を {_generatorType} に変更しました");
        }
    }
}