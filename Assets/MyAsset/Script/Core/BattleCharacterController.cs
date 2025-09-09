using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using UniRx;
using System;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// バトルキャラクターの基底コントローラークラス
    /// 全システムを統合管理し、継承先で具体的な動作ロジックを実装する
    /// </summary>
    public abstract class BattleCharacterController : MonoBehaviour
    {
        [Title("システム参照")]
        [Required, PropertyTooltip("キャラクターの設定データ")]
        [SerializeField] protected CharacterSettings characterSettings;

        [Required, PropertyTooltip("対戦相手のデータプロバイダー")]
        [SerializeField] protected OpponentDataProvider opponentDataProvider;

        // システムコンポーネント（protected）
        protected MovementSystem movementSystem;
        protected AttackSystem attackSystem;
        protected DefenseSystem defenseSystem;
        protected EnergySystem energySystem;
        protected HealthSystem healthSystem;
        protected ManeuverSystem maneuverSystem;
        protected StateSystem stateSystem;
        protected DirectionSystem directionSystem;
        protected PositionCache positionCache;

        [Title("現在のリソース")]
        [ShowInInspector, ReadOnly]
        [ProgressBar(0, 1)]
        [PropertyTooltip("現在の体力割合")]
        public float CurrentHealthPercentage { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 1f;

        [ShowInInspector, ReadOnly]
        [ProgressBar(0, 1)]
        [PropertyTooltip("現在のエネルギー割合")]
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

        // 対戦相手情報へのアクセス
        public IOpponentData OpponentData { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }
        public CharacterSettings Settings { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

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
            stateSystem.UpdateStates();

            // 行動モード別処理
            switch ( stateSystem.CurrentActionMode )
            {
                case ActionMode.Melee:
                    ProcessMeleeMode();
                    break;
                case ActionMode.Ranged:
                    ProcessRangedMode();
                    break;
                case ActionMode.EnergyBarrier:
                    ProcessEnergyBarrierMode();
                    break;
            }

            // 継承先の決定処理
            DecideNextAction();

            // 状態の更新
            UpdateCurrentState();
        }

        /// <summary>
        /// 破棄処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void OnDestroy()
        {
            CleanupEventSubscriptions();
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
        /// 近接モード時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void ProcessMeleeMode()
        {
            // 基本的な近接モード処理
            energySystem.UpdateEnergyRecovery();
        }

        /// <summary>
        /// 射撃モード時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void ProcessRangedMode()
        {
            // 基本的な射撃モード処理
            energySystem.UpdateEnergyRecovery();
            attackSystem.UpdateReloading();
        }

        /// <summary>
        /// エネルギーバリアモード時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void ProcessEnergyBarrierMode()
        {
            // 基本的なエネルギーバリアモード処理
            energySystem.ForceEnergyRecovery();
            healthSystem.UpdateStunRecovery();
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

        #endregion

        #region Public Execution Interface

        /// <summary>
        /// 移動を実行
        /// </summary>
        /// <param name="direction">移動方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteMovement(Vector3 direction)
        {
            if ( CanExecuteAction(ActionType.Walk) )
            {
                movementSystem.Move(direction);
            }
        }

        /// <summary>
        /// ジャンプを実行
        /// </summary>
        /// <param name="charged">チャージジャンプかどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteJump(bool charged = false)
        {
            if ( CanExecuteAction(ActionType.Jump) )
            {
                movementSystem.Jump(charged);
            }
        }

        /// <summary>
        /// ブーストを実行
        /// </summary>
        /// <param name="direction">ブースト方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteBoost(Vector3 direction)
        {
            if ( CanExecuteAction(ActionType.Boost) )
            {
                movementSystem.Boost(direction);
            }
        }

        /// <summary>
        /// 回避を実行
        /// </summary>
        /// <param name="direction">回避方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteDodge(Vector3 direction)
        {
            if ( CanExecuteAction(ActionType.Dodge) )
            {
                movementSystem.Dodge(direction);
            }
        }

        /// <summary>
        /// 弱攻撃を実行
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteWeakAttack(AttackDirection direction)
        {
            if ( CanExecuteAction(ActionType.WeakAttack) )
            {
                directionSystem.ForceDirection(direction, 0.1f);
                attackSystem.ExecuteWeakAttack(direction);
            }
        }

        /// <summary>
        /// 強攻撃を実行
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteStrongAttack(AttackDirection direction)
        {
            if ( CanExecuteAction(ActionType.StrongAttack) )
            {
                directionSystem.ForceDirection(direction, 0.1f);
                attackSystem.ExecuteStrongAttack(direction);
            }
        }

        /// <summary>
        /// スキル攻撃を実行
        /// </summary>
        /// <param name="skillIndex">スキルインデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteSkill(int skillIndex)
        {
            if ( CanExecuteAction(ActionType.SkillAttack) )
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
            if ( CanExecuteAction(ActionType.Guard) )
            {
                directionSystem.ForceDirection(direction, 0.1f);
                defenseSystem.StartGuard(direction);
            }
        }

        /// <summary>
        /// ブロッキングを実行
        /// </summary>
        /// <param name="direction">ブロッキング方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteBlock(AttackDirection direction)
        {
            if ( CanExecuteAction(ActionType.Block) )
            {
                directionSystem.ForceDirection(direction, 0.1f);
                defenseSystem.AttemptBlock(direction);
            }
        }

        /// <summary>
        /// マニューバを実行
        /// </summary>
        /// <param name="maneuverIndex">マニューバインデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteManeuver(int maneuverIndex)
        {
            if ( CanExecuteAction(ActionType.Maneuver) )
            {
                maneuverSystem.ExecuteManeuver(maneuverIndex);
            }
        }

        /// <summary>
        /// 戦闘モードを切り替え
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SwitchCombatMode()
        {
            if ( CanExecuteAction(ActionType.ModeSwitch) )
            {
                var newMode = stateSystem.CurrentActionMode == ActionMode.Melee ?
                    ActionMode.Ranged : ActionMode.Melee;
                stateSystem.ReportActionModeChange(newMode);
            }
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
        /// エネルギーが使用可能かどうか
        /// </summary>
        /// <param name="amount">使用予定のエネルギー量</param>
        /// <returns>使用可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanUseEnergy(float amount)
        {
            return energySystem.CanUseEnergy(amount);
        }

        /// <summary>
        /// エネルギーを使用
        /// </summary>
        /// <param name="amount">使用量</param>
        /// <returns>使用に成功したかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UseEnergy(float amount)
        {
            return energySystem.UseEnergy(amount);
        }

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TakeDamage(float damage)
        {
            healthSystem.TakeDamage(damage);
        }

        /// <summary>
        /// 攻撃結果を受け取る
        /// </summary>
        /// <param name="result">攻撃結果</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveAttack(DamageResult result)
        {
            healthSystem.ProcessDamageResult(result);
        }

        /// <summary>
        /// 攻撃結果の通知を受け取る（攻撃側）
        /// </summary>
        /// <param name="result">攻撃結果</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnAttackResult(DamageResult result)
        {
            if ( result.wasHit )
            {
                OnActionSucceeded(ActionType.WeakAttack); // 基本的に成功扱い
            }
            else
            {
                OnActionFailed(ActionType.WeakAttack);
            }
        }

        #endregion

        #region Protected Helper Methods

        /// <summary>
        /// アクションが実行可能かどうか
        /// </summary>
        /// <param name="actionType">アクションタイプ</param>
        /// <returns>実行可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CanExecuteAction(ActionType actionType)
        {
            return stateSystem.CanExecuteAction(actionType) && CanAct();
        }

        /// <summary>
        /// 基本的な行動が可能かどうか
        /// </summary>
        /// <returns>行動可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CanAct()
        {
            return !stateSystem.HealthData.isDead && !stateSystem.HealthData.isStunned;
        }

        /// <summary>
        /// 対戦相手との距離を取得
        /// </summary>
        /// <returns>距離</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected float GetDistanceToOpponent()
        {
            return positionCache.DistanceTo(OpponentData.Position);
        }

        /// <summary>
        /// 対戦相手への方向を取得
        /// </summary>
        /// <returns>方向ベクトル</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector3 GetDirectionToOpponent()
        {
            return positionCache.DirectionTo(OpponentData.Position);
        }

        /// <summary>
        /// 最適な攻撃方向を取得（For Honorライクな3方向システム）
        /// </summary>
        /// <returns>攻撃方向</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected AttackDirection GetOptimalAttackDirection()
        {
            // 相手の防御方向と異なる方向を選択
            var opponentDirection = OpponentData.CurrentDirection;
            var possibleDirections = new[] { AttackDirection.Up, AttackDirection.Left, AttackDirection.Right };

            foreach ( var direction in possibleDirections )
            {
                if ( direction != opponentDirection )
                {
                    return direction;
                }
            }

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
        /// </summary>
        /// <returns>脆弱状態かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsOpponentVulnerable()
        {
            return OpponentData.IsStunned ||
                   OpponentData.CurrentState == ActionState.Attacking ||
                   OpponentData.EnergyPercentage < 0.1f;
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
            if ( IsInRange(Settings.attack.meleeRange) )
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
            if ( CanExecuteAction(ActionType.Dodge) )
            {
                ExecuteDodge(escapeDirection);
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
            stateSystem = GetComponent<StateSystem>() ?? gameObject.AddComponent<StateSystem>();
            movementSystem = GetComponent<MovementSystem>() ?? gameObject.AddComponent<MovementSystem>();
            attackSystem = GetComponent<AttackSystem>() ?? gameObject.AddComponent<AttackSystem>();
            defenseSystem = GetComponent<DefenseSystem>() ?? gameObject.AddComponent<DefenseSystem>();
            energySystem = GetComponent<EnergySystem>() ?? gameObject.AddComponent<EnergySystem>();
            healthSystem = GetComponent<HealthSystem>() ?? gameObject.AddComponent<HealthSystem>();
            maneuverSystem = GetComponent<ManeuverSystem>() ?? gameObject.AddComponent<ManeuverSystem>();
            directionSystem = GetComponent<DirectionSystem>() ?? gameObject.AddComponent<DirectionSystem>();
            positionCache = GetComponent<PositionCache>() ?? gameObject.AddComponent<PositionCache>();

            // 参照設定
            Settings = characterSettings;
            OpponentData = opponentDataProvider;

            // 各システムの初期化
            InitializeSystemsWithSettings();
        }

        /// <summary>
        /// 各システムを設定で初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeSystemsWithSettings()
        {
            if ( characterSettings == null )
            {
                Debug.LogError($"{name}: CharacterSettingsが設定されていません");
                return;
            }

            // BaseSystemを継承しているシステムを初期化
            var baseSystemComponents = GetComponents<BaseSystem>();
            foreach ( var system in baseSystemComponents )
            {
                system.Initialize(this, characterSettings);
            }
        }

        /// <summary>
        /// ステータスの初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeStats()
        {
            if ( Settings != null )
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
            if ( stateSystem != null )
            {
                stateSystem.OnHealthChanged.Subscribe(OnHealthChangedInternal).AddTo(this);
                stateSystem.OnEnergyChanged.Subscribe(OnEnergyChangedInternal).AddTo(this);
                stateSystem.OnActionStateChanged.Subscribe(OnOpponentStateChanged).AddTo(this);
            }

            if ( directionSystem != null )
            {
                directionSystem.OnDirectionChanged += OnDirectionChangedInternal;
            }
        }

        /// <summary>
        /// イベント購読のクリーンアップ
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CleanupEventSubscriptions()
        {
            // UniRxのAddTo(this)により自動的にクリーンアップされる

            if ( directionSystem != null )
            {
                directionSystem.OnDirectionChanged -= OnDirectionChangedInternal;
            }
        }

        /// <summary>
        /// 設定の妥当性チェック
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateConfiguration()
        {
            if ( Settings == null )
            {
                Debug.LogError($"{gameObject.name}: CharacterSettingsが設定されていません");
            }

            if ( OpponentData == null )
            {
                Debug.LogError($"{gameObject.name}: OpponentDataProviderが設定されていません");
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

            if ( CurrentHealth <= 0f )
            {
                stateSystem.HealthData.isDead = true;
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
        /// 方向変化の内部処理
        /// </summary>
        /// <param name="newDirection">新しい方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDirectionChangedInternal(AttackDirection newDirection)
        {
            // StateSystemに通知（イベント発火用）
            stateSystem?.ReportDirectionChange(newDirection);
        }

        /// <summary>
        /// 現在の状態を更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateCurrentState()
        {
            CurrentState = new CharacterState
            {
                mode = stateSystem.CurrentActionMode,
                state = stateSystem.CurrentActionState,
                direction = stateSystem.CurrentDirection,
                canAct = CanAct(),
                isAlive = !stateSystem.HealthData.isDead
            };
        }

        #endregion

        #region Debug & Tools

        [Title("デバッグ機能")]
        [Button("全クールダウンリセット", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAllCooldowns()
        {
            for ( int i = 0; i < stateSystem.AnalysisData.skillCooldowns.Length; i++ )
            {
                stateSystem.ReportSkillCooldown(i, 0f);
            }
            for ( int i = 0; i < stateSystem.AnalysisData.maneuverCooldowns.Length; i++ )
            {
                stateSystem.ReportManeuverCooldown(i, 0f);
            }
            Debug.Log($"{gameObject.name}: 全クールダウンをリセットしました");
        }

        [Button("体力・エネルギー全回復", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FullRestore()
        {
            CurrentHealth = MaxHealth;
            CurrentEnergy = MaxEnergy;
            stateSystem.ReportEnergyChange(1f);
            stateSystem.HealthData.Reset();
            Debug.Log($"{gameObject.name}: 体力・エネルギーを全回復しました");
        }

        #endregion
    }
}
