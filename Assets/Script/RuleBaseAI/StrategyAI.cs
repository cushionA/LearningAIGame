using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Systems;
using LLMDataArchitect;
using R3;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;
using static LLMDataArchitect.StrategyData;
using static LLMDataArchitect.StrategyResult;
using MovementType = LearningAIGame.CombatSystem.Data.AIParameter.MovementType;

//==============================================ファイルヘッダ=========================================================
// StrategyAI
// 
// 概要: LLMからの応答を利用し、頻度パラメータに基づいて戦術行動を実行するルールベースAI
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [StrategyAI]
// - LLM戦術データに基づくAI行動制御クラス(RuleBaseInjectionの派生)
// - 移動制御、攻撃/防御判断、構え変更、ステップ回避を頻度ベースで実行
// - 敵との距離管理と戦術パラメータに応じた動的な行動選択
// 
// [主要機能]
// 1. 移動制御
//    - 距離ベースの移動判断(前進/後退/横移動/停止)
//    - 危険距離・好む距離範囲に基づく位置取り
//    - 移動パターンのランダム化と継続時間制御
// 
// 2. 攻撃制御
//    - 頻度パラメータによる攻撃タイミング制御
//    - エネルギー管理に基づく攻撃可否判定
//    - 連続攻撃・初回攻撃の状況別判断
//    - ヒット時の追撃コンボ実行
// 
// 3. 防御制御
//    - 敵攻撃検出時の防御行動選択
//    - 防御成功時の確定反撃処理
//    - 回避成功時のチャンス攻撃
//    - 連続防御・初回防御の状況別判断
// 
// 4. ステップ制御
//    - 頻度ベースのステップ実行判定
//    - 最小間隔制御による連続ステップ制限
//    - 移動方向に応じたステップ種類選択
// 
// 5. 構え変更制御
//    - 頻度パラメータによる構え変更タイミング制御
//    - ランダムな構え選択(現在の構え以外から選択)
// 
// 
// [システム連携]
// 購読対象:
//  - AttackSystem(敵): 敵の攻撃検出
//  - HitSystem(敵): 敵の攻撃結果検知
//  - HitSystem(自己): 自分の攻撃結果確認
// 
// 参照システム:
//  - StateSystem(自己/敵): 状態・エネルギー・構え情報
//  - BattleCharacterController(自己): 行動実行制御
// 
// 主要メソッド:
// - InjectionData: LLM戦術データの注入
// - UpdateStrategy: 戦術パラメータの更新
// 
// 入力元クラス: 
//  - LLMInputData(戦術データ)
//  - AIParameterContainer(戦術パラメータ設定)
//  - AttackSystem, HitSystem(イベント通知)
// 
// 出力先クラス:
//  - BattleCharacterController(行動実行)
//  - StrategyResult(戦術結果記録)
// 
// その他:
// - 距離管理による位置取り自動化
// - エネルギー状態に応じた行動制限
// - LLM戦術とルールベース制御のハイブリッド実装
// - リアルタイム戦術パラメータ切り替え対応
// - 行動履歴をStrategyResultに記録しLLMフィードバックに活用
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.AI
{
    /// <summary>
    /// LLMからの応答を利用したルールベースAI
    /// 移動制御、頻度ベース攻撃、構え変更処理を追加
    /// </summary>
    public class StrategyAI : RuleBaseInjection
    {

        #region フィールド

        /// <summary>
        /// AI戦術パラメーター設定
        /// </summary>
        [Header("AI設定")]
        [SerializeField]
        private AIParameterContainer _strategyParameters;

        /// <summary>
        /// 現在使用中の戦術パラメーター
        /// </summary>
        private AIParameter _currentParameter;

        /// <summary>
        /// 戦術の実行結果
        /// </summary>
        private StrategyResult _strategyResult;

        /// <summary>
        /// 現在認識している状況
        /// </summary>
        private ConditionType _currentCondition;

        /// <summary>
        /// 現在使用している行動指針名
        /// </summary>
        private string _currentCriteria;

        /// <summary>
        /// 現在時間のキャッシュ
        /// </summary>
        private float _currentTime = 0f;

        [Header("自分のシステム参照")]
        [SerializeField]
        private StateSystem _myStateSystem;

        [Header("敵のシステム参照")]
        [SerializeField]
        private StateSystem _enemyStateSystem;

        [Header("自分のコントローラー参照")]
        [SerializeField]
        private BattleCharacterController _myController;


        #region 購読対象

        [Header("購読対象")]
        [Tooltip("敵攻撃検出")]
        [SerializeField]
        private AttackSystem _enemyAttackSystem;

        [Tooltip("敵攻撃結果検知")]
        [SerializeField]
        private HitSystem _enemyHitSystem;

        [Tooltip("自己攻撃結果確認")]
        [SerializeField]
        private HitSystem _myHitSystem;

        #endregion

        #region タイミング制御

        /// <summary>
        /// 前回攻撃した時間
        /// </summary>
        private float _lastAttackTime = -1f;

        /// <summary>
        /// 前回防御した時間
        /// </summary>
        private float _lastDefenseTime = -1f;

        /// <summary>
        /// 次回攻撃可能時間
        /// </summary>
        private float _nextAttackTime = 0f;

        /// <summary>
        /// 次回構え変更可能時間
        /// </summary>
        private float _nextStanceChangeTime = 0f;

        /// <summary>
        /// 次回ステップ可能時間
        /// </summary>
        private float _nextStepTime = 0f;
        #endregion

        #region 移動制御

        /// <summary>
        /// 現在の移動目標位置
        /// </summary>
        private Vector3 _currentMovementTarget;

        /// <summary>
        /// 移動実行中フラグ
        /// </summary>
        private bool _isMoving = false;

        /// <summary>
        /// 移動開始時間
        /// </summary>
        private float _movementStartTime;

        /// <summary>
        /// 移動継続時間（ランダムで決定）
        /// </summary>
        private float _movementDuration;

        /// <summary>
        /// AIの移動入力ベクトル
        /// </summary>
        private Vector3 _moveInputVector;

        #endregion

        #endregion

        #region 定数

        /// <summary>
        /// 連続攻撃判定になる時間の定数
        /// </summary>
        private const float k_SequenceAttackDuration = 3f;

        /// <summary>
        /// 連続防御判定になる時間の定数
        /// </summary>
        private const float k_SequenceDefenseDuration = 3f;

        /// <summary>
        /// ステップ判定の最小間隔（秒）
        /// </summary>
        private const float k_MinStepInterval = 3f;

        #endregion

        #region ライフサイクル

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {

            _myStateSystem = GetComponent<StateSystem>();

            // 各システムの購読を開始
            SubscribeSystems();
        }

        /// <summary>
        /// 毎フレーム更新処理
        /// AIの行動判断ループ
        /// </summary>
        private void Update()
        {
            if (_myController == null || _enemyStateSystem == null)
                return;

            _currentTime = Time.time;

            // 移動制御
            UpdateMovement();

            // 構え変更制御
            UpdateStanceChange();

            // 攻撃タイミング制御
            UpdateAttackTiming();

            // 移動入力の適用
            if (_moveInputVector != Vector3.zero)
            {
                _myController.MoveAct(_moveInputVector);
            }
        }

        #endregion

        #region Publicメソッド(戦術データ注入と更新)

        /// <summary>
        /// 戦術データの初期化
        /// </summary>
        public override void InjectionData(LLMInputData data)
        {
            _llmData = data;
            // 行動の結果を記録するためにLLMからインスタンスを受け取る
            _strategyResult = _llmData.StrategyResult;
            Debug.Log($"[{nameof(StrategyAI)}] 戦術データを注入しました");

            // 戦術結果の初期化
            UpdateStrategy();
        }

        /// <summary>
        /// 戦術更新時の処理
        /// </summary>
        public override void UpdateStrategy()
        {
            if (_llmData.CurrentStrategy == null)
            {
                Debug.LogWarning($"[{nameof(StrategyAI)}] 戦術データが注入されていません。デフォルトデータを使用します。");
                _llmData.CurrentStrategy = _strategyParameters.defaultStrategyData;
            }

            // 現在の戦術パラメーターを取得
            _currentParameter = _strategyParameters.GetStrategyParameters(_llmData.CurrentStrategy.BasicTactic);
        }

        #endregion

        #region コールバックメソッド

        /// <summary>
        /// 敵の攻撃を検出した際のコールバック
        /// </summary>
        private void OnEnemyAttack(AttackReportInfo attackReport)
        {
            // 防御出来ない状態なら戻る
            if ((_myStateSystem.CurrentState.CurrentValue & ActionState.防御可能) == 0)
            {
                return;
            }

            // 横回避攻撃は特別扱い
            // 直出しすればブロッキングされる
            if (_enemyStateSystem.CurrentState.CurrentValue == ActionState.横回避攻撃)
            {
                ActionAct(ActionState.弱ブロッキング, _enemyStateSystem.CurrentStance.CurrentValue);
                return;
            }

            // 連続防御
            if (_currentTime - _lastDefenseTime < k_SequenceDefenseDuration)
            {
                _currentCondition = ConditionType.SequentialDefense;
                DefenseJudge(StrategyData.GetDefenseCriteria(_llmData.CurrentStrategy.ContinuousDefenseCriteria));
            }
            // 初回防御
            else
            {
                _currentCondition = ConditionType.Defense;
                DefenseJudge(StrategyData.GetDefenseCriteria(_llmData.CurrentStrategy.DefenseCriteria));
            }
        }

        /// <summary>
        /// 自分の攻撃結果を受け取った際のコールバック
        /// </summary>
        private void OnMyAttackEnd(HitReportInfo hitReport)
        {
            // 攻撃結果の記録
            if (_currentCondition == ConditionType.Attack || _currentCondition == ConditionType.SequentialAttack)
            {
                _lastAttackTime = _currentTime;

                switch (hitReport.hitResultType)
                {
                    case HitResultType.Block:
                    case HitResultType.Guard:
                    case HitResultType.Avoid:
                    case HitResultType.Stun:
                    case HitResultType.Miss:
                        _strategyResult.AddResult(_currentCondition, false);
                        break;

                    case HitResultType.Hit:
                        _strategyResult.AddResult(_currentCondition, true);

                        // ヒット時の追撃行動
                        // 連続攻撃判定
                        if (_currentParameter.ShouldComboAttack() &&
                            _myStateSystem.EnergyRatio >= _currentParameter.comboMinEnergy)
                        {
                            AttackAct();

                            // 次回の攻撃間隔を設定
                            _nextAttackTime = _currentTime + _currentParameter.GetNextAttackDelay();
                        }

                        break;
                }
            }
            _currentCondition = ConditionType.None;
        }

        /// <summary>
        /// 敵の攻撃が完了した際のコールバック
        /// </summary>
        private void OnEnemyAttackEnd(HitReportInfo hitReport)
        {
            // 防御結果の記録
            if (_currentCondition != ConditionType.Attack && _currentCondition != ConditionType.SequentialAttack)
            {
                _lastDefenseTime = _currentTime;

                switch (hitReport.hitResultType)
                {
                    case HitResultType.Block:

                        // 結果を追加
                        _strategyResult.AddResult(_currentCondition, true);

                        // 確定反撃行動
                        if (_currentParameter.ShouldPunish())
                        {
                            // 弱ブロッキング成功 + エネルギー十分なら強攻撃で確定反撃
                            if (hitReport.attackType == AttackType.WeakAttack
                            && _myStateSystem.EnergyRatio >= _currentParameter.heavyAttackMinEnergy)
                            {
                                _myController.HeavyAttackAct(GetAttackStance(_myStateSystem.CurrentStance.CurrentValue)).Forget();
                            }

                            // 強攻撃ブロック時の確定反撃行動
                            else if (_myStateSystem.EnergyRatio >= _currentParameter.lightAttackMinEnergy)
                            {
                                _myController.LightAttackAct(GetAttackStance(_myStateSystem.CurrentStance.CurrentValue)).Forget();
                            }
                        }

                        break;
                    case HitResultType.Guard:

                        // 結果を追加
                        _strategyResult.AddResult(_currentCondition, true);

                        // ガード時の確定反撃行動
                        if (_myStateSystem.EnergyRatio >= _currentParameter.lightAttackMinEnergy && _currentParameter.ShouldPunish())
                        {
                            _myController.LightAttackAct(GetAttackStance(_myStateSystem.CurrentStance.CurrentValue)).Forget();
                        }
                        break;
                    case HitResultType.Avoid:

                        // 結果を追加
                        _strategyResult.AddResult(_currentCondition, true);

                        // エネルギー十分で乱数が噛み合えば敵の強攻撃空振りに攻撃を合わせる
                        if (hitReport.attackType == AttackType.HeavyAttack &&
                            _myStateSystem.EnergyRatio >= _currentParameter.rushMinEnergy &&
                            _currentParameter.ShouldOpportunityAttack())
                        {
                            _myController.AvoidAttackAct(MovementReportType.FrontStep).Forget();
                        }

                        break;
                    case HitResultType.Stun:
                        _strategyResult.AddResult(_currentCondition, true);
                        break;

                    case HitResultType.Cancel:
                        break;
                    case HitResultType.Miss:

                        // エネルギー十分で乱数が噛み合えば敵の空振りに攻撃を合わせる
                        if (_myStateSystem.EnergyRatio >= _currentParameter.rushMinEnergy &&
                            _currentParameter.ShouldOpportunityAttack())
                        {
                            _myController.AvoidAttackAct(MovementReportType.FrontStep).Forget();
                        }
                        break;
                    case HitResultType.Hit:
                        _strategyResult.AddResult(_currentCondition, false);
                        break;
                }
            }
            _currentCondition = ConditionType.None;
        }

        #endregion

        #region 購読処理

        /// <summary>
        /// 各システムからの通知を購読する
        /// </summary>
        private void SubscribeSystems()
        {
            // 敵の攻撃システムを購読
            if (_enemyAttackSystem != null)
            {
                _enemyAttackSystem.Observable
                    .Subscribe(OnEnemyAttack)
                    .AddTo(this);

                Debug.Log($"[{nameof(StrategyAI)}] 敵の攻撃システムの購読を開始しました。");
            }

            // 自分の攻撃結果システムを購読
            if (_myHitSystem != null)
            {
                _myHitSystem.Observable
                    .Subscribe(OnMyAttackEnd)
                    .AddTo(this);

                Debug.Log($"[{nameof(StrategyAI)}] 自分の攻撃結果システムの購読を開始しました。");
            }

            // 敵の攻撃結果システムを購読
            if (_enemyHitSystem != null)
            {
                _enemyHitSystem.Observable
                    .Subscribe(OnEnemyAttackEnd)
                    .AddTo(this);

                Debug.Log($"[{nameof(StrategyAI)}] 敵の攻撃結果システムの購読を開始しました。");
            }
        }

        #endregion

        #region 移動制御

        /// <summary>
        /// 移動処理の更新
        /// 距離管理と移動パターンに基づいて移動を制御
        /// </summary>
        private void UpdateMovement()
        {
            // 移動不可能状態であれば移動制御は行わない
            // キャラコントローラーで制御は行っているが、無駄を省くためにここでも確認
            if (!_myStateSystem.CanMove)
            {
                _myController.MoveAct(Vector3.zero); // 停止
                _moveInputVector = Vector3.zero;
                _isMoving = false;
                return;
            }


            // 移動切り替えタイミングの前なら判断は行わない
            if (_isMoving)
            {
                // 移動時間が経過したら終了
                if (_currentTime - _movementStartTime >= _movementDuration)
                {
                    _isMoving = false;
                    return;
                }
                return;
            }

            // 移動を実行すべきか判定
            // すべきでなければ停止
            if (!_currentParameter.ShouldMove())
            {
                _myController.MoveAct(Vector3.zero); // 停止
                _moveInputVector = Vector3.zero;
                return;
            }

            // 距離に基づいて移動を決定
            DecideMovementByDistance((_enemyStateSystem.transform.position - transform.position).sqrMagnitude);
        }

        /// <summary>
        /// 距離に基づいて移動を決定
        /// </summary>
        /// <param name="distanceToEnemy">敵との距離</param>
        private void DecideMovementByDistance(float distanceToEnemySqr)
        {
            MovementType moveDirection;

            // 危険距離の場合は後退優先
            if (_currentParameter.IsInDangerRangeSqr(distanceToEnemySqr))
            {
                moveDirection = MovementType.Backward;
            }
            // 好む距離範囲外の場合は距離調整
            else if (!_currentParameter.CheckPreferredRangeSqr(distanceToEnemySqr, out int result))
            {
                // 遠すぎる場合は接近
                if (result == -1)
                {
                    moveDirection = MovementType.Forward;
                }
                // 近すぎる場合は後退
                else
                {
                    moveDirection = MovementType.Backward;
                }
            }
            // 好む距離範囲内の場合はパターンに従う
            else
            {
                moveDirection = _currentParameter.DecideMovementType();
            }


            ExecuteMovement(moveDirection);

        }

        #region 移動実行

        /// <summary>
        /// 移動を実行
        /// 一定条件で移動をステップに置き換える
        /// </summary>
        private void ExecuteMovement(MovementType moveDirection)
        {
            switch (moveDirection)
            {
                // 停止
                case AIParameter.MovementType.None:
                    _myController.MoveAct(Vector3.zero);
                    _moveInputVector = Vector3.zero;
                    break;
                case AIParameter.MovementType.Forward:
                    if (CanActStep())
                    {
                        ExecuteStep(moveDirection);
                    }
                    _moveInputVector = Vector3.forward;
                    break;

                case AIParameter.MovementType.Backward:
                    if (CanActStep())
                    {
                        ExecuteStep(moveDirection);
                    }

                    _moveInputVector = Vector3.back;
                    break;

                case AIParameter.MovementType.Left:
                    if (CanActStep())
                    {
                        ExecuteStep(moveDirection);
                    }
                    _moveInputVector = Vector3.left;
                    break;

                case AIParameter.MovementType.Right:
                    if (CanActStep())
                    {
                        ExecuteStep(moveDirection);
                    }
                    _moveInputVector = Vector3.right;
                    break;
                default:
                    break;
            }

            StartMovement(UnityEngine.Random.Range(0.5f, 1.5f));
        }

        #endregion

        /// <summary>
        /// 移動を開始
        /// </summary>
        private void StartMovement(float duration)
        {
            _isMoving = true;
            _movementStartTime = _currentTime;
            _movementDuration = duration;
        }

        #endregion

        #region ステップ制御

        /// <summary>
        /// ステップタイミングの更新
        /// 頻度パラメーターに基づいて回避行動を実行
        /// </summary>
        private bool CanActStep()
        {
            if (_strategyParameters == null)
                return false;

            // 次回ステップ時間に達していない場合はスキップ
            if (Time.time < _nextStepTime)
                return false;

            // エネルギーチェック（ステップにもエネルギーが必要）
            if (_myStateSystem.EnergyRatio < _currentParameter.minEnergyRatio)
            {
                // 次回ステップ時間を延長
                _nextStepTime = Time.time + k_MinStepInterval;
                return false;
            }

            // ステップを実行すべきか判定（movementAggressivenessを使用）
            return _currentParameter.ShouldStep();
        }

        /// <summary>
        /// ステップを実行
        /// </summary>
        /// <param name="stepType">ステップの種類</param>
        private void ExecuteStep(MovementType stepDirection)
        {
            switch (stepDirection)
            {
                case MovementType.Forward:
                    // 前回避
                    _myController.AvoidAct(MovementReportType.FrontStep);
                    Debug.Log("[StrategyAI] 前回避を実行");
                    break;

                case MovementType.Left:
                    // 左回避
                    _myController.AvoidAct(MovementReportType.LeftStep);
                    Debug.Log("[StrategyAI] 左回避を実行");
                    break;

                case MovementType.Right:
                    // 右回避
                    _myController.AvoidAct(MovementReportType.RightStep);
                    Debug.Log("[StrategyAI] 右回避を実行");
                    break;

                case MovementType.None:
                default:
                    // 何もしない
                    break;
            }

            // 次回ステップ可能時間を設定
            _nextStepTime = Time.time + k_MinStepInterval;
        }

        #endregion

        #region 攻撃制御

        /// <summary>
        /// 攻撃タイミングの更新
        /// 頻度パラメーターに基づいて攻撃を実行
        /// </summary>
        private void UpdateAttackTiming()
        {
            // 攻撃出来ない状態なら戻る
            if ((_myStateSystem.CurrentState.CurrentValue & ActionState.攻撃可能) == 0)
            {
                return;
            }

            // 次回攻撃時間に達していない場合はスキップ
            if (_currentTime < _nextAttackTime)
                return;

            // エネルギーチェック
            if (_myStateSystem.EnergyRatio < _currentParameter.minEnergyRatio)
            {
                // 次回攻撃時間を延長
                _nextAttackTime = _currentTime + _currentParameter.GetNextAttackDelay();
                return;
            }

            // 攻撃を実行すべきか判定
            if (_currentParameter.ShouldAttack())
            {
                AttackAct();

                // 次回の攻撃間隔を設定
                _nextAttackTime = _currentTime + _currentParameter.GetNextAttackDelay();
            }
            else
            {
                // 攻撃しない場合も次回判定時間を設定
                _nextAttackTime = _currentTime + _currentParameter.GetNextAttackDelay() * 0.4f;
            }
        }

        /// <summary>
        /// 攻撃行動を実行
        /// </summary>
        private void AttackAct()
        {
            // 連続攻撃
            if (_currentTime - _lastAttackTime < k_SequenceAttackDuration)
            {
                _currentCondition = ConditionType.SequentialAttack;
                AttackJudge(StrategyData.GetAttackCriteria(_llmData.CurrentStrategy.ContinuousAttackCriteria));
            }
            // 初回攻撃
            else
            {
                _currentCondition = ConditionType.Attack;
                AttackJudge(StrategyData.GetAttackCriteria(_llmData.CurrentStrategy.AttackCriteria));
            }
        }

        #endregion

        #region 構え変更制御

        /// <summary>
        /// 構え変更の更新
        /// 頻度パラメーターに基づいて構えを変更
        /// </summary>
        private void UpdateStanceChange()
        {
            if (_strategyParameters == null || _myStateSystem == null)
                return;

            // 次回構え変更時間に達していない場合はスキップ
            if (_currentTime < _nextStanceChangeTime)
                return;

            // 構え変更を実行すべきか判定
            if (UnityEngine.Random.value < _currentParameter.stanceChangeFrequency)
            {
                ExecuteStanceChange();
            }

            // 次回構え変更可能時間を設定
            _nextStanceChangeTime = _currentTime + _currentParameter.minStanceChangeInterval;
        }

        /// <summary>
        /// 構え変更を実行
        /// </summary>
        private void ExecuteStanceChange()
        {
            // 現在の構えを取得
            StanceType currentStance = _myStateSystem.CurrentStance.CurrentValue;

            // ランダムに異なる構えを選択
            StanceType newStance = GetRandomStance(currentStance);

            // 構え変更を実行（実装はBattleCharacterControllerに依存）
            _myController.GuardDirectionChange(newStance);

            Debug.Log($"[StrategyAI] 構えを変更: {currentStance} → {newStance}");
        }

        /// <summary>
        /// ランダムな構えを取得（現在の構え以外）
        /// 現在の構え以外の2つからランダムに選択
        /// </summary>
        /// <param name="currentStance">現在の構え</param>
        /// <returns>新しい構え（現在の構えとは異なる）</returns>
        private StanceType GetRandomStance(StanceType currentStance)
        {
            // 現在の構えに応じて、残り2つのうちランダムに選択
            switch (currentStance)
            {
                case StanceType.Up:
                    // UpならLeftかRight
                    return UnityEngine.Random.value < 0.5f ? StanceType.Left : StanceType.Right;

                case StanceType.Left:
                    // LeftならUpかRight
                    return UnityEngine.Random.value < 0.5f ? StanceType.Up : StanceType.Right;

                case StanceType.Right:
                    // RightならUpかLeft
                    return UnityEngine.Random.value < 0.5f ? StanceType.Up : StanceType.Left;

                default:
                    // フォールバック（通常ここには来ない）
                    return StanceType.Up;
            }
        }

        #endregion

        #region 行動判断

        /// <summary>
        /// 判断基準に応じた攻撃行動を行うメソッド
        /// </summary>
        private void AttackJudge(ActionCriteriaType criteriaType)
        {
            LLMLogData enemyLog = _llmData.PlayerLog;
            StanceType enemyStance = _enemyStateSystem.CurrentStance.CurrentValue;

            switch (criteriaType)
            {
                case ActionCriteriaType.Attack_CumulativeProbability:
                    ActionState mostUseDefense = enemyLog.ActionLog.MostUsedDefense;
                    ActionAct(AntiActionSelect(mostUseDefense), enemyStance);
                    break;

                case ActionCriteriaType.Attack_RecentPatternFocus:
                    ActionState mostRecentUseDefense = enemyLog.RecentMostUsedDefense;
                    ActionAct(AntiActionSelect(mostRecentUseDefense), enemyStance);
                    break;

                case ActionCriteriaType.Attack_SpeedPriority:
                    ActionAct(ActionState.弱攻撃, enemyStance);
                    break;

                case ActionCriteriaType.Attack_ReturnPriority:
                    ActionAct(ActionState.強攻撃, enemyStance);
                    break;

                case ActionCriteriaType.Attack_FeintFocus:
                    ActionAct(ActionState.強攻撃キャンセル, enemyStance);
                    break;

                case ActionCriteriaType.Attack_DispersionFocus:
                    ActionAct(_llmData.NPCLog.ActionLog.LeastUsedDefense, enemyStance);
                    break;

                case ActionCriteriaType.Attack_EnergyEfficiency:
                    ActionAct(ActionState.弱攻撃, enemyStance);
                    break;
            }
        }

        /// <summary>
        /// 判断基準に応じた防御行動を行うメソッド
        /// </summary>
        private void DefenseJudge(ActionCriteriaType criteriaType)
        {
            LLMLogData enemyLog = _llmData.PlayerLog;
            StanceType enemyStance = _enemyStateSystem.CurrentStance.CurrentValue;

            switch (criteriaType)
            {
                case ActionCriteriaType.Defense_CumulativeProbability:
                    ActionState mostUseAttack = enemyLog.ActionLog.MostUsedAttack;
                    ActionAct(AntiActionSelect(mostUseAttack), enemyStance);
                    break;

                case ActionCriteriaType.Defense_RecentPatternFocus:
                    ActionState mostRecentUseAttack = enemyLog.RecentMostUsedAttack;
                    ActionAct(AntiActionSelect(mostRecentUseAttack), enemyStance);
                    break;

                case ActionCriteriaType.Defense_CounterattackFocus:
                    ActionAct(ActionState.弱攻撃, enemyStance);
                    break;

                case ActionCriteriaType.Defense_ReturnPriority:
                    if (enemyLog.ActionLog.LightAttackPercentage >= enemyLog.ActionLog.HeavyAttackPercentage)
                    {
                        ActionAct(ActionState.弱ブロッキング, enemyStance);
                    }
                    else
                    {
                        ActionAct(ActionState.強ブロッキング, enemyStance);
                    }
                    break;

                case ActionCriteriaType.Defense_RiskAvoidance:
                    ActionAct(ActionState.後ろ回避, enemyStance);
                    break;

                case ActionCriteriaType.Defense_EvasiveCounterPriority:
                    ActionAct(ActionState.横回避攻撃, enemyStance);
                    break;

                case ActionCriteriaType.Defense_DispersionFocus:
                    ActionAct(_llmData.NPCLog.ActionLog.LeastUsedDefense, enemyStance);
                    break;
            }
        }

        /// <summary>
        /// 入力された行動に対する最適行動を返すメソッド
        /// </summary>
        private ActionState AntiActionSelect(ActionState type) => type switch
        {
            ActionState.後ろ回避 => ActionState.前回避攻撃,
            ActionState.横回避 => ActionState.強攻撃,
            ActionState.前回避 => ActionState.強攻撃,
            ActionState.ブロッキング => ActionState.強攻撃キャンセル,
            ActionState.弱攻撃 => ActionState.弱ブロッキング,
            ActionState.強攻撃 => ActionState.強ブロッキング,
            ActionState.強攻撃キャンセル => ActionState.弱攻撃,
            ActionState.横回避攻撃 => ActionState.強攻撃キャンセル,
            ActionState.前回避攻撃 => ActionState.ブロッキング,
            ActionState.デフォルト攻撃 => ActionState.ガード,
            ActionState.デフォルト防御 => ActionState.弱攻撃,
            _ => ActionState.弱攻撃
        };

        /// <summary>
        /// 指定された行動を実行するメソッド
        /// </summary>
        private void ActionAct(ActionState useAction, StanceType enemyStance)
        {
            switch (useAction)
            {
                case ActionState.後ろ回避:
                    _myController.AvoidAct(MovementReportType.BackStep);
                    break;

                case ActionState.横回避:
                    if (enemyStance == StanceType.Left)
                    {
                        _myController.AvoidAct(MovementReportType.RightStep);
                    }
                    else if (enemyStance == StanceType.Right)
                    {
                        _myController.AvoidAct(MovementReportType.LeftStep);
                    }
                    else
                    {
                        MovementReportType randomStep = UnityEngine.Random.value < 0.5f ?
                            MovementReportType.LeftStep : MovementReportType.RightStep;
                        _myController.AvoidAct(randomStep);
                    }
                    break;

                case ActionState.前回避:
                    _myController.AvoidAct(MovementReportType.FrontStep);
                    break;

                case ActionState.ブロッキング:
                    _myController.BlockingAct(GetDefenseStance(enemyStance));
                    break;

                case ActionState.弱攻撃:
                    _myController.LightAttackAct(GetAttackStance(enemyStance)).Forget();
                    break;

                case ActionState.強攻撃:
                    _myController.HeavyAttackAct(GetAttackStance(enemyStance)).Forget();
                    break;

                case ActionState.強攻撃キャンセル:
                    _myController.HeavyAttackFeint(GetAttackStance(enemyStance)).Forget();
                    break;

                case ActionState.横回避攻撃:
                    if (enemyStance == StanceType.Left)
                    {
                        _myController.AvoidAttackAct(MovementReportType.RightStep).Forget();
                    }
                    else if (enemyStance == StanceType.Right)
                    {
                        _myController.AvoidAttackAct(MovementReportType.LeftStep).Forget();
                    }
                    else
                    {
                        MovementReportType randomStep = UnityEngine.Random.value < 0.5f ?
                            MovementReportType.LeftStep : MovementReportType.RightStep;
                        _myController.AvoidAttackAct(randomStep).Forget();
                    }
                    break;

                case ActionState.前回避攻撃:
                    _myController.AvoidAttackAct(MovementReportType.FrontStep).Forget();
                    break;

                case ActionState.弱ブロッキング:
                    _myController.LightBlocking(GetDefenseStance(enemyStance));
                    break;

                case ActionState.強ブロッキング:
                    _myController.HeavyBlocking(GetDefenseStance(enemyStance));
                    break;

                default:
                    Debug.LogWarning($"ActionState '{useAction}' は実行処理未定義です。");
                    break;
            }
        }

        /// <summary>
        /// 攻撃用方向を敵の防御方向から取得するメソッド
        /// </summary>
        private StanceType GetAttackStance(StanceType stance) => stance switch
        {
            StanceType.Up => UnityEngine.Random.value < 0.5f ? StanceType.Left : StanceType.Right,
            StanceType.Left => UnityEngine.Random.value < 0.5f ? StanceType.Up : StanceType.Right,
            StanceType.Right => UnityEngine.Random.value < 0.5f ? StanceType.Left : StanceType.Up,
            _ => StanceType.Up
        };

        /// <summary>
        /// 防御用方向を敵の攻撃方向から取得するメソッド
        /// </summary>
        private StanceType GetDefenseStance(StanceType stance) => stance switch
        {
            StanceType.Up => StanceType.Up,
            StanceType.Left => StanceType.Right,
            StanceType.Right => StanceType.Left,
            _ => StanceType.Up
        };

        #endregion

        #region デバッグ用

        /// <summary>
        /// デバッグ用の実行時情報を文字列で返す
        /// </summary>
        /// <returns>現在のAI実行状態</returns>
        public string GetDebugInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("=== StrategyAI 実行状態 ===");

            // 現在の状況
            sb.AppendLine($"状況: {_currentCondition} | 戦術: {(_llmData?.CurrentStrategy?.BasicTactic ?? "未設定")}");

            // 自分の状態
            if (_myStateSystem != null)
            {
                sb.AppendLine($"自分: {_myStateSystem.CurrentState.CurrentValue} | 構え: {_myStateSystem.CurrentStance.CurrentValue} | EN: {_myStateSystem.Energy}");
            }

            // 敵の状態
            if (_enemyStateSystem != null)
            {
                float distance = transform != null && _enemyStateSystem.transform != null
                    ? Vector3.Distance(transform.position, _enemyStateSystem.transform.position)
                    : 0f;
                sb.AppendLine($"敵: {_enemyStateSystem.CurrentState.CurrentValue} | 構え: {_enemyStateSystem.CurrentStance.CurrentValue} | 距離: {distance:F1}m");
            }

            // 移動状態
            sb.AppendLine($"移動: {(_isMoving ? $"実行中 {_moveInputVector} (残り{(_movementDuration - (_currentTime - _movementStartTime)):F1}秒)" : "停止")}");

            // 次回行動可能時刻
            float nextAttack = Mathf.Max(0, _nextAttackTime - _currentTime);
            float nextStep = Mathf.Max(0, _nextStepTime - _currentTime);
            float nextStance = Mathf.Max(0, _nextStanceChangeTime - _currentTime);
            sb.AppendLine($"待機時間: 攻撃 {nextAttack:F1}秒 | ステップ {nextStep:F1}秒 | 構え変更 {nextStance:F1}秒");

            // 戦術結果
            if (_strategyResult != null)
            {
                sb.AppendLine($"成功率: 攻撃 {_strategyResult.GetSuccessRate(ConditionType.Attack):P0} | 防御 {_strategyResult.GetSuccessRate(ConditionType.Defense):P0}");
            }

            sb.AppendLine("==========================");

            return sb.ToString();
        }

        #endregion
    }
}