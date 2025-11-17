using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.AI;
using LearningAIGame.CombatSystem.Core;
using LLMDataArchitect.Test;
using LLMUnity;
using System;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

//==============================================ファイルヘッダ===========================================================
// LLMCommunicator
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
// 依存ライブラリ: LLMUnity, UniTask
// 入力元クラス: StateSystem(プレイヤー/NPC), PromptGeneratorBase
// 出力先クラス: StrategyData(AIControllerが参照)、RuleBaseInjection（戦術を注入されるルールベースAIの基底クラス）
// 
// 設計思想:
// - 全ての非同期処理は例外を発生させず、ログ出力のみで継続動作
// - 自動更新ループは任意のタイミングで開始/停止可能
// - LLM設定の動的変更に対応し、実験的な調整が容易
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
    /// LLM for Unityを使用し、戦術的な思考を生成するための通信コンポーネント 
    /// 一定間隔でLLMに戦術判断をリクエストし、結果を更新
    /// </summary>
    public class LLMCommunicator : MonoBehaviour
    {
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

        [Header("データソース設定")]
        [SerializeField]
        [Tooltip("プレイヤーの状態システムへの参照")]
        protected StateSystem _playerStateSystem;

        [SerializeField]
        [Tooltip("NPCの状態システムへの参照")]
        protected StateSystem _npcStateSystem;

        [SerializeField]
        [Tooltip("NPCのAI")]
        protected RuleBaseInjection _ruleBaseAI;

        // 内部状態
        protected LLMInputData _inputData;
        protected PromptGeneratorBase _promptGenerator;
        protected bool _isInitialized = false;
        protected bool _isProcessing = false;
        protected CancellationTokenSource _updateLoopCts;

        #region パブリックプロパティ

        /// <summary>
        /// 自動更新が実行中かどうかを取得
        /// </summary>
        public bool IsAutoUpdateRunning => _updateLoopCts != null && !_updateLoopCts.IsCancellationRequested;

        /// <summary>
        /// 現在の入力データを取得（デバッグ用）。
        /// </summary>
        public LLMInputData GetCurrentInputData() => _inputData;

        /// <summary>
        /// 初期化済みかどうかを取得
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 現在処理中かどうかを取得
        /// </summary>
        public bool IsProcessing => _isProcessing;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
            _ruleBaseAI.InjectionData(_inputData);
        }

        private void OnDestroy()
        {
            // 更新ループをキャンセル
            StopAutoUpdate();
        }

        private void OnDisable()
        {
            // 無効化時も更新ループを停止
            StopAutoUpdate();
        }

        #endregion

        #region Publicメソッド

        #region 動的設定変更

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

            // 自動更新中の場合は再起動
            if (IsAutoUpdateRunning)
            {
                StartAutoUpdate();
            }
        }

        /// <summary>
        /// LLMの応答タイムアウト時間を動的に変更
        /// </summary>
        /// <param name="newTimeout">新しいタイムアウト時間（秒）</param>
        public void SetTimeout(float newTimeout)
        {
            if (newTimeout < 1f)
            {
                Debug.LogWarning("タイムアウト時間は1秒以上である必要があります。");
                return;
            }

            _timeoutSeconds = newTimeout;
            Debug.Log($"タイムアウト時間を {newTimeout}秒 に変更しました。");
        }


        #endregion

        #region 自動更新制御

        /// <summary>
        /// 自動更新ループを開始
        /// 指定された間隔で戦術判断をリクエストし続けます。
        /// </summary>
        public void StartAutoUpdate()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("初期化されていません。先にInitialize()を呼び出してください。");
                return;
            }

            // 既に実行中の場合は停止してから再開
            if (_updateLoopCts != null)
            {
                StopAutoUpdate();
            }

            _updateLoopCts = new CancellationTokenSource();
            AutoUpdateLoopAsync(_updateLoopCts.Token).Forget();

            Debug.Log($"自動更新を開始しました (間隔: {_updateInterval}秒, 初回遅延: {_startDelay}秒)");
        }

        /// <summary>
        /// LLMに戦術判断をリクエスト（手動呼び出し用）
        /// 自動更新が無効な場合や、即座に判断が必要な場合に使用
        /// </summary>
        public async UniTask RequestTacticalDecisionAsync()
        {
            // 自動更新中はスキップ
            if (_autoUpdate)
            {
                return;
            }
            await RequestTacticalDecisionAsync(this.GetCancellationTokenOnDestroy());
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

        #endregion

        #endregion

        #region privateメソッド

        #region 初期化

        /// <summary>
        /// コミュニケーターを初期化
        /// プロンプト生成器、LLM設定、データソースを設定
        /// </summary>
        protected virtual void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("LLM Communicatorは既に初期化されています。");
                return;
            }

            // プロンプト生成器を初期化
            _promptGenerator = CreatePromptGenerator();
            Debug.Log($"プロンプト生成器を初期化");

            // LLMCharacterの存在チェック
            if (_llmCharacter == null)
            {
                Debug.LogError("LLMCharacterが設定されていません。Inspectorで設定してください。");
                return;
            }

            // LLMの最適設定を適用
            if (_autoConfigureLLM)
                ConfigureLLMOptimal();

            // JSON Schema Grammarを設定
            if (_useGrammar)
                SetupGrammar();

            // システムプロンプトを設定
            SetupSystemPrompt();

            // 入力データの初期化
            // TODO: 戦術結果はAI完成時にAIへの参照に書き換える
            _inputData = new LLMInputData(_playerStateSystem, _npcStateSystem, new StrategyResult());

            _isInitialized = true;
            Debug.Log("LLM Communicatorの初期化が完了しました。");

            // 自動更新が有効な場合、更新ループを開始
            if (_autoUpdate)
            {
                StartAutoUpdate();
            }
        }

        /// <summary>
        /// プロンプト生成器のインスタンスを作成
        /// </summary>
        /// <returns>選択されたタイプに応じたプロンプト生成器</returns>
        private PromptGeneratorBase CreatePromptGenerator()
        {
            // 現在はMainPromptGeneratorを使用
            // 必要に応じて他の生成器に切り替え可能
            return new CachePromptGenerator();

            // 将来的にタイプ選択を有効化する場合:
            //return _generatorType switch
            //{
            //    PromptGeneratorType.Japanese => new JapanesePromptGenerator(),
            //    PromptGeneratorType.English => new EnglishPromptGenerator(),
            //    PromptGeneratorType.Fixed_Eng => new FixedEnglishGenerator(),
            //    PromptGeneratorType.Main => new MainPromptGenerator(),
            //    _ => new JapanesePromptGenerator()
            //};
        }

        /// <summary>
        /// LLMのシステムプロンプトを設定
        /// AIの役割と応答形式を定義
        /// </summary>
        private void SetupSystemPrompt()
        {
            _llmCharacter.SetPrompt(_promptGenerator.GenerateFixedSection());
            _llmCharacter.playerName = "User";
            _llmCharacter.AIName = "TacticAI";

            _llmCharacter.Warmup(_promptGenerator.GenerateFixedSection());

            Debug.Log("システムプロンプトを設定しました。");
        }

        /// <summary>
        /// LLMの最適な設定を適用
        /// ストリーミングとプロンプトキャッシュを有効化
        /// 今回処理するタスクに最適化
        /// </summary>
        private void ConfigureLLMOptimal()
        {
            _llmCharacter.stream = true;       // ストリーミングレスポンスを有効化
            _llmCharacter.cachePrompt = true;  // プロンプトキャッシュを有効化
            _llmCharacter.llm.contextSize = 2048; // コンテキストサイズを2048に設定
            _llmCharacter.seed = 0; // シード値を0に設定（ランダム性をなくす）

            Debug.Log("LLM最適設定を適用しました (cachePrompt: true)");
        }

        /// <summary>
        /// JSON Schema Grammarを設定
        /// LLMの出力形式を厳密に制御
        /// </summary>
        private void SetupGrammar()
        {
            _llmCharacter.grammarJSONString = _promptGenerator.GenerateGrammar();
            ;

            Debug.Log($"JSON Schema Grammar設定完了");
        }

        #endregion

        #region LLM通信

        /// <summary>
        /// 自動更新ループの非同期処理
        /// 指定間隔で戦術判断を繰り返しリクエスト
        /// 例外は全てSuppressされ、ログ出力のみ行う
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン。NPC死亡イベントで起動</param>
        private async UniTaskVoid AutoUpdateLoopAsync(CancellationToken cancellationToken)
        {
            // 初回遅延（キャンセルチェック付き）
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

            // メインループ
            while (!cancellationToken.IsCancellationRequested)
            {
                // 戦術判断をリクエスト（例外は内部で処理）
                await RequestTacticalDecisionAsync(cancellationToken);

                // 次の更新まで待機（キャンセルチェック付き）
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
        /// LLMに戦術判断をリクエスト
        /// 全ての例外はSuppressされ、適切にログ出力される
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        protected virtual async UniTask RequestTacticalDecisionAsync(CancellationToken cancellationToken)
        {
            // 処理中の場合はスキップ
            if (_isProcessing)
            {
                Debug.LogWarning("前回のリクエストがまだ処理中です。スキップ");
                return;
            }

            _isProcessing = true;

            try
            {
                // LLMに非同期リクエスト（キャンセルチェック付き）
                var (isCanceled, strategy) = await RequestAsync(cancellationToken).SuppressCancellationThrow();

                // キャンセルされた場合
                if (isCanceled)
                {
                    Debug.Log("戦術判断リクエストがキャンセルされました。");
                    return;
                }

                // 応答が無効な場合
                if (strategy == null)
                {
                    Debug.LogError("LLMからの応答が無効でした。");
                    return;
                }

                // 応答が有効な場合、入力データを更新
                _inputData.UpdateStrategy(strategy);
                _ruleBaseAI.UpdateStrategy();

                Debug.Log($"戦術を更新しました: {strategy.BasicTactic}");
            }
            catch (Exception ex)
            {
                // 予期しない例外のみキャッチ（SuppressCancellationThrowで例外は基本的に発生しない）
                Debug.LogError($"戦術判断リクエストで予期しないエラーが発生しました: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// LLMに非同期でリクエストを送信し、戦術データを返す
        /// タイムアウト、キャンセル、JSON解析失敗に対応
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>LLMが生成した戦術データ。失敗時はnull</returns>
        protected virtual async UniTask<StrategyData> RequestAsync(CancellationToken cancellationToken = default)
        {
            // プロンプト生成
            string prompt = _promptGenerator.GeneratePromptByData(_inputData);
            Debug.Log($"生成されたプロンプト (文字数: {prompt.Length}):\n{prompt}");

            // LLMにリクエスト送信（タイムアウト＋キャンセル対応、例外抑制）
            var result = await _llmCharacter.Chat(prompt)
                .AsUniTask()
                .Timeout(TimeSpan.FromSeconds(_timeoutSeconds))
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();

            // キャンセルまたはタイムアウトのチェック
            if (result.IsCanceled)
            {
                Debug.LogWarning($"LLMリクエストがキャンセルまたはタイムアウトしました ({_timeoutSeconds}秒)。");
                return null;
            }

            // 応答の取得
            string output = result.Result;

            if (string.IsNullOrEmpty(output))
            {
                Debug.LogError("LLMから空の応答が返されました。");
                return null;
            }

            Debug.Log($"LLM応答を受信 (文字数: {output.Length}):\n{output}");

            // JSON解析（失敗時はnullを返す）
            var (isSuccess, strategy) = StrategyData.TryFromJsonEnglish(output);

            if (!isSuccess)
            {
                Debug.LogError($"JSON解析に失敗しました。応答内容:\n{output}");
                return null;
            }

            return strategy;
        }

        #endregion

        #endregion
    }
}