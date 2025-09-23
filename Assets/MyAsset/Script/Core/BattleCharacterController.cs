using System.Runtime.CompilerServices;
using UnityEngine;
using UniRx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LearningAIGame.CombatSystem.Core;
using NaughtyAttributes;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// バトルキャラクターの基底コントローラークラス
    /// 全システムを統合管理し、継承先で具体的な動作ロジックを実装する
    /// </summary>
    public abstract class BattleCharacterController : MonoBehaviour
    {
        [Header("システム参照")]
        [Tooltip("キャラクターの設定データ")]
        [SerializeField] protected CharacterSettings characterSettings;

        // システムコンポーネント（protected）
        protected MovementSystem movementSystem;
        protected AttackSystem attackSystem;
        protected DefenseSystem defenseSystem;
        protected EnergySystem energySystem;
        protected PositionCache positionCache;
        protected StateSystem stateSystem;

        [Header("現在のリソース")]
        [Tooltip("現在の体力割合")]
        public float CurrentHealthPercentage { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 1f;

        [Tooltip("現在のエネルギー割合")]
        public float CurrentEnergyPercentage { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 1f;

        // 公開プロパティ
        public float CurrentHealth { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }
        public float MaxHealth { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }
        public float CurrentEnergy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }
        public float MaxEnergy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }
        public CharacterState CurrentState { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        public Vector3 Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.positionCache.Position;
        }

        public CharacterSettings Settings { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        /// <summary>
        /// エネルギー切れ状態復帰時の復元システム
        /// </summary>
        private Dictionary<string, object> _energyDepletedStateBackup = new Dictionary<string, object>();

        /// <summary>
        /// キャラクターの総合状態
        /// </summary>
        public struct CharacterState
        {
            public ActionMode mode;
            public ActionState state;
            public AttackDirection direction;
            public bool canAct;
            public bool isAlive;
        }

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void Awake()
        {
            InitializeComponents();
            InitializeStats();
            SetupEventSubscriptions();
        }

        /// <summary>
        /// 開始処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void Start()
        {
            ValidateConfiguration();
        }

        /// <summary>
        /// 物理更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void FixedUpdate()
        {
            // 状態更新
            //   stateSystem.UpdateStates();

            // ActionDataのクールダウン更新（新規追加）
            if (Settings != null)
            {
                //   Settings.UpdateAllCooldowns(Time.fixedDeltaTime);
            }

            // 行動モード別処理
            //switch (stateSystem.CurrentActionMode)
            //{
            //    case ActionMode.Melee:
            //        ProcessMeleeMode();
            //        break;
            //    case ActionMode.Ranged:
            //        ProcessRangedMode();
            //        break;
            //    case ActionMode.EnergyBarrier:
            //        ProcessEnergyBarrierMode();
            //        break;
            //}

            // 継承先の決定処理
            DecideNextAction();
        }

        /// <summary>
        /// 破棄処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnDestroy()
        {

        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// 次の行動を決定する（継承先で実装必須）
        /// </summary>
        protected abstract void DecideNextAction();

        #endregion

        #region Virtual Methods (オーバーライド可能)

        /// <summary>
        /// エネルギーバリアモード時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void ProcessEnergyBarrierMode()
        {
            // 基本的なエネルギーバリアモード処理
            //       energySystem.ForceEnergyRecovery();
        }

        /// <summary>
        /// アクション成功時のコールバック
        /// </summary>
        /// <param name="actionType">成功したアクションタイプ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnActionSucceeded(ActionType actionType) { }

        /// <summary>
        /// アクション失敗時のコールバック
        /// </summary>
        /// <param name="actionType">失敗したアクションタイプ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnActionFailed(ActionType actionType) { }

        /// <summary>
        /// 対戦相手の状態変化時のコールバック
        /// </summary>
        /// <param name="newState">新しい状態</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnOpponentStateChanged(ActionState newState) { }

        /// <summary>
        /// 体力変化時のコールバック
        /// </summary>
        /// <param name="newHealthPercentage">新しい体力割合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnHealthChanged(float newHealthPercentage) { }

        /// <summary>
        /// エネルギー変化時のコールバック
        /// </summary>
        /// <param name="newEnergyPercentage">新しいエネルギー割合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnEnergyChanged(float newEnergyPercentage) { }

        /// <summary>
        /// エネルギー切れ状態に入る時のコールバック（新規追加）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnEnergyDepleted()
        {

        }

        /// <summary>
        /// エネルギー切れ状態から回復する時のコールバック（新規追加）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnEnergyRecovered()
        {

        }

        #endregion

        #region Public Execution Interface

        /// <summary>
        /// 移動を実行
        /// </summary>
        /// <param name="direction">移動方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteMovement(Vector3 direction)
        {
            if (CanExecuteAction(ActionType.Walk))
            {
                movementSystem.MoveStart(direction);
            }
        }

        /// <summary>
        /// ジャンプを実行
        /// </summary>
        /// <param name="charged">チャージジャンプかどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteJump(Vector3 direction)
        {
            if (CanExecuteAction(ActionType.Jump))
            {
                movementSystem.Jump(direction);
            }
        }

        /// <summary>
        /// ブーストを実行
        /// </summary>
        /// <param name="direction">ブースト方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteBoost(Vector3 direction)
        {
            if (CanExecuteAction(ActionType.Boost))
            {
                movementSystem.SetBoost(direction);
            }
        }

        /// <summary>
        /// 回避を実行（ActionDataシステム対応版）
        /// 回避インターバル機能を追加
        /// </summary>
        /// <param name="direction">回避方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task ExecuteDodge(Vector3 direction)
        {
            if (CanExecuteAction(ActionType.Dodge))
            {
                // エネルギー消費とActionData更新
                if (UseEnergy(Settings.movement.dodgeEnergyCost))
                {
                    await movementSystem.Dodge(direction);
                }
            }
        }

        /// <summary>
        /// 弱攻撃を実行（ActionDataシステム対応版）
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteWeakAttack(AttackDirection direction)
        {
            attackSystem.ExecuteWeakAttack(direction);
        }

        /// <summary>
        /// 強攻撃を実行（ActionDataシステム対応版）
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteStrongAttack(AttackDirection direction)
        {
            attackSystem.ExecuteStrongAttack(direction);
        }

        /// <summary>
        /// スキル攻撃を実行
        /// </summary>
        /// <param name="skillIndex">スキルインデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteSkill(int skillIndex)
        {
            if (CanExecuteAction(ActionType.SkillAttack))
            {
                attackSystem.ExecuteSkill(skillIndex);
            }
        }

        /// <summary>
        /// ガードを実行
        /// </summary>
        /// <param name="direction">ガード方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteGuard(AttackDirection direction)
        {
            if (CanExecuteAction(ActionType.Guard))
            {

                defenseSystem.StartGuard(direction);
            }
        }

        /// <summary>
        /// エネルギー切れシールドを開始（L1ボタン用）修正版
        /// StateSystemのエネルギー切れ状態を確認
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteEnergyShield()
        {
            // エネルギー切れ状態時のみ有効
            if (stateSystem.isActiveAndEnabled)
            {
                defenseSystem.StartEnergyShield();
            }
            else
            {
                Debug.LogWarning("エネルギーが切れていないため、エネルギーシールドは使用できません");
            }
        }

        /// <summary>
        /// エネルギー切れシールドを停止（L1ボタン離し用）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopEnergyShield()
        {
            defenseSystem.StopEnergyShield();
        }

        /// <summary>
        /// エネルギーバリアモードに手動で移行（修正版）
        /// StateSystemのエネルギー切れ状態を確認
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnterEnergyBarrierMode()
        {
            if (stateSystem.isActiveAndEnabled)
            {
                //stateSystem.ForceEnergyBarrierMode();
            }
            else
            {
                Debug.LogWarning("エネルギーが切れていないため、エネルギーバリアモードに移行できません");
            }
        }

        /// <summary>
        /// ブロッキングを実行（修正版）
        /// ブースト中は無効、近接モード時の○ボタンでのみ発動
        /// </summary>
        /// <param name="direction">ブロッキング方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteBlock(AttackDirection direction)
        {
            //// 近接モードかつブースト中でない場合のみ実行可能
            //if (stateSystem.CurrentActionMode == ActionMode.Melee &&
            //     stateSystem.CurrentActionState != ActionState.Boosting &&
            //     CanExecuteAction(ActionType.Block))
            //{
            //    directionSystem.ForceDirection(direction, 0.1f);
            //    defenseSystem.AttemptBlock(direction);
            //}
        }

        /// <summary>
        /// クイックターンを実行
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteQuickTurn()
        {
            movementSystem.QuickTurn();
        }

        #endregion

        #region Public Resource Management

        /// <summary>
        /// エネルギーを使用（修正版）
        /// StateSystemのエネルギー切れ状態も考慮
        /// </summary>
        /// <param name="amount">使用量</param>
        /// <returns>使用に成功したかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UseEnergy(float amount)
        {
            return energySystem.UseEnergy(amount);
        }

        #endregion

        #region Protected Helper Methods

        /// <summary>
        /// アクションが実行可能かどうか（ActionDataシステム対応版）
        /// </summary>
        /// <param name="actionType">アクションタイプ</param>
        /// <returns>実行可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CanExecuteAction(ActionType actionType)
        {
            // 基本的な状態チェック
            if (stateSystem == null || Settings == null)
                return false;

            return true;
            //// CharacterSettingsのActionDataシステムでチェック
            //return Settings.CanExecuteAction(actionType, CurrentEnergy) &&
            //       stateSystem.CanExecuteAction(actionType);
        }

        /// <summary>
        /// 対戦相手との距離を取得
        /// </summary>
        /// <returns>距離</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected float GetDistanceToOpponent()
        {
            return 0;
            //return positionCache.DistanceTo();
        }

        /// <summary>
        /// 対戦相手への方向を取得
        /// </summary>
        /// <returns>方向ベクトル</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector3 GetDirectionToOpponent()
        {
            return positionCache.DirectionTo(this.transform.position);
        }

        /// <summary>
        /// 最適な攻撃方向を取得（For Honorライクな3方向システム）
        /// </summary>
        /// <returns>攻撃方向</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected AttackDirection GetOptimalAttackDirection()
        {

            return AttackDirection.Up; // フォールバック
        }

        /// <summary>
        /// 指定した範囲内にいるかどうか
        /// </summary>
        /// <param name="range">範囲</param>
        /// <returns>範囲内かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsInRange(float range)
        {
            return GetDistanceToOpponent() <= range;
        }

        /// <summary>
        /// 対戦相手が脆弱な状態かどうか
        /// 怯みやスタン、空振り後など
        /// </summary>
        /// <returns>脆弱状態かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsOpponentVulnerable()
        {
            return false;
        }

        /// <summary>
        /// 対戦相手に向かって移動
        /// </summary>
        /// <param name="target">目標位置</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ExecuteMovementToward(Vector3 target)
        {
            Vector3 direction = positionCache.DirectionTo(target);
            ExecuteMovement(direction);
        }

        /// <summary>
        /// 最適な攻撃を実行
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ExecuteOptimalAttack()
        {
            var direction = GetOptimalAttackDirection();
            if (IsInRange(Settings.attack.meleeRange))
            {
                ExecuteStrongAttack(direction);
            }
            else
            {
                ExecuteWeakAttack(direction);
            }
        }

        /// <summary>
        /// 回避行動を実行
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ExecuteEvasiveAction()
        {
            Vector3 escapeDirection = -GetDirectionToOpponent();
            if (CanExecuteAction(ActionType.Dodge))
            {
                //ExecuteDodge(escapeDirection);
            }
            else
            {
                ExecuteMovement(escapeDirection);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// コンポーネントの初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeComponents()
        {
            // 必須コンポーネントの取得/追加
            //stateSystem = this.GetComponent<StateSystem>() ? gameObject.AddComponent<StateSystem>();
            //movementSystem = GetComponent<MovementSystem>() ? gameObject.AddComponent<MovementSystem>();
            //attackSystem = GetComponent<AttackSystem>() ? gameObject.AddComponent<AttackSystem>();
            //defenseSystem = GetComponent<DefenseSystem>() ? gameObject.AddComponent<DefenseSystem>();
            //energySystem = GetComponent<EnergySystem>() ? gameObject.AddComponent<EnergySystem>();
            //positionCache = GetComponent<PositionCache>() ? gameObject.AddComponent<PositionCache>();

            // 参照設定
            Settings = characterSettings;

            // 各システムの初期化
            InitializeSystemsWithSettings();
        }

        /// <summary>
        /// 各システムを設定で初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeSystemsWithSettings()
        {
            if (characterSettings == null)
            {
                Debug.LogError($"{name}: CharacterSettingsが設定されていません");
                return;
            }

        }

        /// <summary>
        /// ステータスの初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeStats()
        {
            if (Settings != null)
            {
                MaxHealth = Settings.maxHealth;
                MaxEnergy = Settings.energy.maxEnergy;
                CurrentHealth = MaxHealth;
                CurrentEnergy = MaxEnergy;
                CurrentHealthPercentage = 1f;
                CurrentEnergyPercentage = 1f;
            }
        }

        /// <summary>
        /// イベント購読の設定
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupEventSubscriptions()
        {
            if (stateSystem != null)
            {
                //stateSystem.OnHealthChanged.Subscribe(OnHealthChangedInternal).AddTo(this);
                //stateSystem.OnEnergyChanged.Subscribe(OnEnergyChangedInternal).AddTo(this);
                //stateSystem.OnActionStateChanged.Subscribe(OnOpponentStateChanged).AddTo(this);

                //// エネルギー切れ状態の変化を監視（新規追加）
                //stateSystem.OnEnergyDepletedStateChanged.Subscribe(OnEnergyDepletedStateChangedInternal).AddTo(this);
            }
        }

        /// <summary>
        /// 設定の妥当性チェック
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateConfiguration()
        {
            if (Settings == null)
            {
                Debug.LogError($"{gameObject.name}: CharacterSettingsが設定されていません");
            }
        }

        #endregion

        #region Internal Event Handlers

        /// <summary>
        /// 体力変化の内部処理
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnHealthChangedInternal(float damage)
        {
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            CurrentHealthPercentage = CurrentHealth / MaxHealth;
            OnHealthChanged(CurrentHealthPercentage);

            if (CurrentHealth <= 0f)
            {

            }
        }

        /// <summary>
        /// エネルギー変化の内部処理
        /// </summary>
        /// <param name="energyPercentage">エネルギー割合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnEnergyChangedInternal(float energyPercentage)
        {
            CurrentEnergyPercentage = energyPercentage;
            CurrentEnergy = energyPercentage * MaxEnergy;
            OnEnergyChanged(energyPercentage);
        }

        /// <summary>
        /// エネルギー切れ状態変化の内部処理（新規追加）
        /// </summary>
        /// <param name="isDepleted">エネルギー切れ状態かどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnEnergyDepletedStateChangedInternal(bool isDepleted)
        {
            if (isDepleted)
            {
                OnEnergyDepleted();
            }
            else
            {
                OnEnergyRecovered();
            }
        }

        ///// <summary>
        ///// 現在の状態を更新
        ///// </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private void UpdateCurrentState()
        //{
        //    CurrentState = new CharacterState
        //    {
        //        mode = stateSystem.CurrentActionMode,
        //        state = stateSystem.CurrentActionState,
        //        direction = stateSystem.CurrentDirection,
        //        canAct = CanAct(),
        //        isAlive = !stateSystem.HealthData.isDead
        //    };
        //}

        #endregion
    }
}