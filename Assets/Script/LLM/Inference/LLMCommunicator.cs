using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.AI;
using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LLMDataArchitect.Test;
using LLMUnity;
using System;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

//==============================================ファイルヘッダ===========================================================
// LLMCommunicator (UniTask 統合版)
// 
// 概要: LLMと通信し、戦術を生成する通信管理クラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// LLMUnityライブラリを使用してLLMと非同期通信を行い、戦闘AIの戦術判断をリアルタイムで取得。
// 一定間隔での自動更新機能により、戦闘時間に応じて動的な戦術変更。
// publicメソッドで外部から制御することも可能。
// プロンプト生成、JSON Schema Grammarによる出力制御、タイムアウト/キャンセル処理を統合管理。
// StateSystemから取得した戦闘データをLLMInputDataに変換し、戦術判断の精度を向上。
// 
// **重要な変更点（UniTask 統合）:**
// - async void を廃止（SetupSystemPrompt, Initialize）
// - async UniTask に統一
// - Warmup 完了まで確実に待機
// - 初期化完了後に AI 稼働開始
// 
// 依存ライブラリ: LLMUnity, UniTask
// 入力元クラス: StateSystem(プレイヤー/NPC), PromptGeneratorBase
// 出力先クラス: StrategyData(AIControllerが参照)、RuleBaseInjection（戦術を注入されるルールベースAIの基底クラス）
// 
// 設計思想:
// - 全ての非同期処理は例外を発生させず、ログ出力のみで継続動作
// - 自動更新ループは任意のタイミングで開始/停止可能
// - LLM設定の動的変更に対応し、実験的な調整が容易
// - 初期化の完了を確実に待機（async UniTask による安全な初期化）
// 
// 通信フロー:
// 1. LLMInputDataから戦闘データ取得
// 2. PromptGeneratorでプロンプト生成
// 3. LLMCharacterへ非同期リクエスト送信
// 4. JSON形式で戦術データを受信・解析
// 5. StrategyDataを更新してルールベースAIへ反映
// 
// その他:
// JSON Schema Grammarによる構造化出力を強制
// タイムアウト時間、更新間隔は実行時に動的変更可能
// 初回遅延設定により、ゲーム開始直後の不要なリクエストを回避
//=====================================================================================================================
namespace LLMDataArchitect
{
    /// <summary>
    /// プロンプト生成タイプ
    /// </summary>
    public enum PromptGeneratorType
    {
        Debug,
        Cache,
        Cache_NLI
    }

    /// <summary>
    /// LLM for Unityを使用し、戦術的な思考を生成するための通信コンポーネント
    /// 一定間隔でLLMに戦術判断をリクエストし、結果を更新
    /// </summary>
    public class LLMCommunicator : MonoBehaviour
    {
        #region Inspector設定

        [Header("LLM設定")]
        [SerializeField]
        [Tooltip("LLM通信に使用するLLMCharacterコンポーネント")]
        protected LLMCharacter _llmCharacter;

        [SerializeField]
        [Tooltip("LLMリクエストのタイムアウト時間（秒）")]
        protected float _timeoutSeconds = 30f;

        [SerializeField]
        [Tooltip("LLMの最適設定を自動適用するか")]
        protected bool _autoConfigureLLM = true;

        [SerializeField]
        [Tooltip("JSON Schema Grammarを使用するか")]
        protected bool _useGrammar = true;

        [Header("プロンプト生成設定")]
        [SerializeField]
        [Tooltip("使用するプロンプト生成器のタイプ")]
        protected PromptGeneratorType _generatorType = PromptGeneratorType.Cache;

        [SerializeField]
        [Tooltip("自然言語指示タイプ（Cache_NLI選択時のみ有効）")]
        protected NaturalLanguageInstructionType _nliType = NaturalLanguageInstructionType.None;

        [Header("更新設定")]
        [SerializeField]
        [Tooltip("戦術判断を自動更新するか")]
        protected bool _autoUpdate = true;

        [SerializeField]
        [Tooltip("戦術判断の更新間隔（秒）")]
        [Range(1f, 60f)]
        protected float _updateInterval = 5f;

        [SerializeField]
        [Tooltip("自動更新を開始するまでの遅延時間（秒）")]
        protected float _startDelay = 2f;

        //[Header("データソース設定")]
        //[SerializeField]
        //[Tooltip("プレイヤーの状態システムへの参照")]
        protected StateSystem _playerStateSystem;

        //[SerializeField]
        //[Tooltip("NPCの状態システムへの参照")]
        protected StateSystem _npcStateSystem;

        //[SerializeField]
        //[Tooltip("NPCのAI")]
        protected RuleBaseInjection _ruleBaseAI;

        #endregion

        #region Private Fields

        protected LLMInputData _inputData;
        protected PromptGeneratorBase _promptGenerator;
        protected bool _isInitialized = false;
        protected bool _isProcessing = false;
        protected CancellationTokenSource _updateLoopCts;

        #endregion

        #region Properties

        /// <summary>
        /// 自動更新が実行中かどうかを取得
        /// </summary>
        public bool IsAutoUpdateRunning => _updateLoopCts != null && !_updateLoopCts.IsCancellationRequested;

        /// <summary>
        /// 初期化済みかどうかを取得
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 現在処理中かどうかを取得
        /// </summary>
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// 現在のNLIタイプを取得
        /// </summary>
        public NaturalLanguageInstructionType CurrentNLIType => _nliType;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 破棄時に更新ループをキャンセル
        /// </summary>
        private void OnDestroy()
        {
            StopAutoUpdate();
        }

        /// <summary>
        /// 無効化時に更新ループを停止
        /// </summary>
        private void OnDisable()
        {
            StopAutoUpdate();
        }

        #endregion

        #region Public API - 初期化・注入

        /// <summary>
        /// 注入と初期化を同時に実行
        /// </summary>
        /// <param name="player">プレイヤーのStateSystem</param>
        /// <param name="npc">NPCのStateSystem</param>
        public void InitializeWithInjection(StateSystem player, StateSystem npc)
        {
            _playerStateSystem = player;
            _npcStateSystem = npc;
            _ruleBaseAI = npc.GetComponent<RuleBaseInjection>();

            _inputData = new LLMInputData(_playerStateSystem, _npcStateSystem, new StrategyResult());

            if (_ruleBaseAI != null)
            {
                _ruleBaseAI.InjectionData(_inputData);
            }

            InitializeAsync().Forget();
        }

        /// <summary>
        /// 新規バトル用にデータを注入（バトル継続時）
        /// </summary>
        /// <param name="player">プレイヤーのStateSystem</param>
        /// <param name="npc">NPCのStateSystem</param>
        public void InjectionNewBattle(StateSystem player, StateSystem npc)
        {
            _playerStateSystem = player;
            _npcStateSystem = npc;
            _ruleBaseAI = npc.GetComponent<RuleBaseInjection>();
            _inputData.InjectionNewBattle(player, npc);

            if (_ruleBaseAI != null)
            {
                _ruleBaseAI.InjectionData(_inputData);
            }
        }

        /// <summary>
        /// 現在の入力データを取得（デバッグ用）
        /// </summary>
        /// <returns>現在のLLMInputData</returns>
        public LLMInputData GetCurrentInputData() => _inputData;

        #endregion

        #region Public API - 動的設定変更

        /// <summary>
        /// 更新間隔を動的に変更
        /// </summary>
        /// <param name="newInterval">新しい更新間隔（秒）</param>
        public void SetUpdateInterval(float newInterval)
        {
            if (newInterval < 1f)
            {
                Debug.LogWarning("更新間隔は1秒以上である必要があります。");
                return;
            }

            _updateInterval = newInterval;
            Debug.Log($"更新間隔を {newInterval}秒 に変更しました。");

            if (IsAutoUpdateRunning)
            {
                StartAutoUpdate();
            }
        }

        /// <summary>
        /// NLIタイプを動的に変更
        /// </summary>
        /// <param name="newNLIType">新しいNLIタイプ</param>
        public void SetNLIType(NaturalLanguageInstructionType newNLIType)
        {
            _nliType = newNLIType;

            if (_promptGenerator is CachePromptGeneratorWithNLI nliGenerator)
            {
                nliGenerator.CurrentInstructionType = newNLIType;
            }

            Debug.Log($"NLIタイプを {CachePromptGeneratorWithNLI.GetInstructionDescription(newNLIType)} に変更しました。");
        }

        #endregion

        #region Public API - 自動更新制御

        /// <summary>
        /// 自動更新ループを開始
        /// </summary>
        public void StartAutoUpdate()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("初期化されていません。先に初期化を待ってください。");
                return;
            }

            if (_updateLoopCts != null)
            {
                StopAutoUpdate();
            }

            _updateLoopCts = new CancellationTokenSource();
            AutoUpdateLoopAsync(_updateLoopCts.Token).Forget();

            Debug.Log($"自動更新を開始しました (間隔: {_updateInterval}秒, 初回遅延: {_startDelay}秒)");
        }

        /// <summary>
        /// 自動更新ループを停止
        /// </summary>
        public void StopAutoUpdate()
        {
            if (_updateLoopCts != null)
            {
                _updateLoopCts.Cancel();
                _updateLoopCts.Dispose();
                _updateLoopCts = null;

                Debug.Log("自動更新を停止しました。");
            }
        }

        /// <summary>
        /// 手動で戦術判断をリクエスト（自動更新が無効な場合のみ動作）
        /// </summary>
        public async UniTask RequestTacticalDecisionManualAsync()
        {
            if (_autoUpdate)
            {
                Debug.LogWarning("自動更新が有効です。手動リクエストはスキップされます。");
                return;
            }

            await RequestTacticalDecisionInternalAsync(this.GetCancellationTokenOnDestroy());
        }

        #endregion

        #region Private Methods - 初期化

        /// <summary>
        /// コミュニケーターを非同期で初期化
        /// </summary>
        protected virtual async UniTaskVoid InitializeAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    Debug.LogWarning("LLM Communicatorは既に初期化されています。");
                    return;
                }

                _promptGenerator = CreatePromptGenerator();
                Debug.Log($"プロンプト生成器を初期化: {_generatorType}");

                if (_llmCharacter == null)
                {
                    Debug.LogError("LLMCharacterが設定されていません。Inspectorで設定してください。");
                    return;
                }

                if (_autoConfigureLLM)
                {
                    ConfigureLLMOptimal();
                }

                if (_useGrammar)
                {
                    SetupGrammar();
                }

                await SetupSystemPromptAsync();

                _isInitialized = true;
                Debug.Log("LLM Communicatorの初期化が完全に完了しました。");

                if (_autoUpdate)
                {
                    StartAutoUpdate();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"LLM Communicator の初期化中に予期しないエラーが発生しました: {ex.Message}\n{ex.StackTrace}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// プロンプト生成器のインスタンスを作成
        /// </summary>
        /// <returns>選択されたタイプに応じたプロンプト生成器</returns>
        private PromptGeneratorBase CreatePromptGenerator()
        {
            return _generatorType switch
            {
                PromptGeneratorType.Debug => new DebugPromptGenerator(),
                PromptGeneratorType.Cache => new CachePromptGenerator(),
                PromptGeneratorType.Cache_NLI => new CachePromptGeneratorWithNLI(_nliType),
                _ => new CachePromptGenerator()
            };
        }

        /// <summary>
        /// LLMのシステムプロンプトを設定（Warmup完了を待機）
        /// </summary>
        private async UniTask SetupSystemPromptAsync()
        {
            try
            {
                _llmCharacter.SetPrompt(_promptGenerator.GenerateFixedSection());
                _llmCharacter.playerName = "User";
                _llmCharacter.AIName = "TacticAI";

                await _llmCharacter.Warmup(_promptGenerator.GenerateFixedSection());

                Debug.Log("システムプロンプトを設定しました（Warmup完了）。");
            }
            catch (Exception ex)
            {
                Debug.LogError($"システムプロンプト設定中にエラーが発生しました: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// LLMの最適な設定を適用
        /// </summary>
        private void ConfigureLLMOptimal()
        {
            _llmCharacter.cachePrompt = true;
            _llmCharacter.llm.contextSize = 2048;
            _llmCharacter.seed = 0;

            Debug.Log("LLM最適設定を適用しました (cachePrompt: true)");
        }

        /// <summary>
        /// JSON Schema Grammarを設定
        /// </summary>
        private void SetupGrammar()
        {
            _llmCharacter.grammarJSONString = _promptGenerator.GenerateGrammar();

            Debug.Log("JSON Schema Grammar設定完了");
        }

        #endregion

        #region Private Methods - LLM通信

        /// <summary>
        /// 自動更新ループの非同期処理
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        private async UniTaskVoid AutoUpdateLoopAsync(CancellationToken cancellationToken)
        {
            if (_startDelay > 0)
            {
                bool isCanceled = await UniTask.Delay(
                    TimeSpan.FromSeconds(_startDelay),
                    cancellationToken: cancellationToken
                ).SuppressCancellationThrow();

                if (isCanceled)
                {
                    Debug.Log("自動更新ループが初回遅延中にキャンセルされました。");
                    return;
                }
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await RequestTacticalDecisionInternalAsync(cancellationToken);

                bool isCanceled = await UniTask.Delay(
                    TimeSpan.FromSeconds(_updateInterval),
                    cancellationToken: cancellationToken
                ).SuppressCancellationThrow();

                if (isCanceled)
                {
                    Debug.Log("自動更新ループがキャンセルされました。");
                    return;
                }
            }
        }

        /// <summary>
        /// LLMに戦術判断をリクエスト（内部用）
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        protected virtual async UniTask RequestTacticalDecisionInternalAsync(CancellationToken cancellationToken)
        {
            if (_isProcessing)
            {
                Debug.LogWarning("前回のリクエストがまだ処理中です。スキップ");
                return;
            }

            _isProcessing = true;

            try
            {
                var (isCanceled, strategy) = await SendLLMRequestAsync(cancellationToken).SuppressCancellationThrow();

                if (isCanceled)
                {
                    Debug.Log("戦術判断リクエストがキャンセルされました。");
                    return;
                }

                if (strategy == null)
                {
                    Debug.LogError("LLMからの応答が無効(Null)でした。");
                    return;
                }

                _inputData.UpdateStrategy(strategy);

                if (_ruleBaseAI != null)
                {
                    _ruleBaseAI.UpdateStrategy();
                }

                Debug.Log($"戦術を更新しました: {strategy.BasicTactic}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"戦術判断リクエストで予期しないエラーが発生しました: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// LLMに非同期でリクエストを送信し、戦術データを返す
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>LLMが生成した戦術データ。失敗時はnull</returns>
        protected virtual async UniTask<StrategyData> SendLLMRequestAsync(CancellationToken cancellationToken = default)
        {
            string prompt = _promptGenerator.GeneratePromptByData(_inputData);
            Debug.Log($"生成されたプロンプト (文字数: {prompt.Length}):\n{prompt}");

            DateTime start = DateTime.Now;

            var result = await _llmCharacter.Chat(prompt)
                .AsUniTask()
                .Timeout(TimeSpan.FromSeconds(_timeoutSeconds))
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();

            if (result.IsCanceled)
            {
                Debug.LogWarning($"LLMリクエストがキャンセルまたはタイムアウトしました ({_timeoutSeconds}秒)。");
                return null;
            }

            string output = result.Result;

            if (string.IsNullOrEmpty(output))
            {
                Debug.LogError("LLMから空の応答が返されました。");
                return null;
            }

            Debug.Log($"LLM応答を受信 \n文字数: {output.Length} 処理時間：{(DateTime.Now - start).TotalSeconds}秒 \nプロンプト：{prompt} \n応答：{output}");

            var (isSuccess, strategy) = StrategyData.TryFromJsonEnglish(output);

            if (!isSuccess)
            {
                Debug.LogError($"JSON解析に失敗しました。応答内容:\n{output}");
                return null;
            }

            return strategy;
        }

        #endregion
    }
}