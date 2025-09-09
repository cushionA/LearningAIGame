using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using UniRx;
using System;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// ヘルスシステム - 体力管理、被ダメージ処理、スタン管理を行う
    /// </summary>
    public class HealthSystem : BaseSystem<HealthData>
    {
        [Title("コンポーネント参照")]
        [Required, PropertyTooltip("状態システム")]
        [SerializeField] private StateSystem stateSystem;

        [Required, PropertyTooltip("防御システム")]
        [SerializeField] private DefenseSystem defenseSystem;

        [Title("現在の状態")]
        [ShowInInspector, ReadOnly]
        [ProgressBar(0, 1)]
        [PropertyTooltip("現在の体力割合")]
        public float CurrentHealthPercentage { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 1f;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の体力")]
        public float CurrentHealth { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("最大体力")]
        public float MaxHealth { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("死亡状態かどうか")]
        public bool IsDead { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [Title("スタンシステム")]
        [ShowInInspector, ReadOnly]
        [ProgressBar(0, 100)]
        [PropertyTooltip("現在のスタンゲージ")]
        public float CurrentStunGauge { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 0f;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("最大スタンゲージ")]
        public float MaxStunGauge { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 100f;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在スタン中かどうか")]
        public bool IsStunned { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在怯み中かどうか")]
        public bool IsFlinching { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在無敵状態かどうか")]
        public bool IsInvincible { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        // 内部状態
        private float lastDamageTime = 0f;
        private float stunRecoveryRate = 25f;

        /// <summary>
        /// 初期化処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Awake()
        {
            if ( characterController == null )
                characterController = GetComponent<BattleCharacterController>();

            if ( stateSystem == null )
                stateSystem = GetComponent<StateSystem>();

            if ( defenseSystem == null )
                defenseSystem = GetComponent<DefenseSystem>();
        }

        /// <summary>
        /// 開始処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Start()
        {
            InitializeHealth();
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            UpdateHealthState();
            UpdateStunRecovery();
        }

        #region Public Health Methods

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TakeDamage(float damage)
        {
            if ( IsInvincible || IsDead )
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            CurrentHealthPercentage = CurrentHealth / MaxHealth;
            lastDamageTime = Time.time;

            // 状態システムに報告
            stateSystem.ReportDamage(damage, false);

            // 死亡判定
            if ( CurrentHealth <= 0f )
            {
                OnDeath();
            }

            Debug.Log($"{gameObject.name}: {damage}ダメージ受ける。残り体力: {CurrentHealth}/{MaxHealth}");
        }

        /// <summary>
        /// 体力を回復
        /// </summary>
        /// <param name="amount">回復量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecoverHealth(float amount)
        {
            if ( IsDead )
                return;

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            CurrentHealthPercentage = CurrentHealth / MaxHealth;
        }

        /// <summary>
        /// 体力を設定（デバッグ用）
        /// </summary>
        /// <param name="health">設定する体力</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetHealth(float health)
        {
            CurrentHealth = Mathf.Clamp(health, 0f, MaxHealth);
            CurrentHealthPercentage = CurrentHealth / MaxHealth;

            if ( CurrentHealth <= 0f )
            {
                OnDeath();
            }
            else if ( IsDead && CurrentHealth > 0f )
            {
                OnRevive();
            }
        }

        /// <summary>
        /// ダメージ結果を処理
        /// </summary>
        /// <param name="result">ダメージ結果</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessDamageResult(DamageResult result)
        {
            if ( IsInvincible || IsDead )
            {
                result.actualDamage = 0f;
                result.stunAccumulation = 0f;
                result.wasHit = false;
                return;
            }

            // 防御システムによる処理
            if ( defenseSystem != null )
            {
                // 攻撃情報を再構築（簡易版）
                var attackInfo = new AttackInfo
                {
                    baseDamage = result.actualDamage,
                    stunAccumulation = result.stunAccumulation,
                    canBeGuarded = true,
                    canBeBlocked = true
                };

                result = defenseSystem.ProcessDefense(attackInfo);
            }

            // ダメージ適用
            if ( result.actualDamage > 0f )
            {
                TakeDamage(result.actualDamage);
            }

            // スタンゲージ蓄積
            if ( result.stunAccumulation > 0f )
            {
                AccumulateStun(result.stunAccumulation);
            }

            // スタン判定
            if ( result.causedStun || CurrentStunGauge >= MaxStunGauge )
            {
                TriggerStun();
            }
        }

        /// <summary>
        /// スタンゲージを蓄積
        /// </summary>
        /// <param name="amount">蓄積量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AccumulateStun(float amount)
        {
            if ( IsDead )
                return;

            CurrentStunGauge = Mathf.Min(MaxStunGauge, CurrentStunGauge + amount);

            // StateSystemのHealthDataも同期
            stateSystem.HealthData.stunGauge = CurrentStunGauge;

            if ( CurrentStunGauge >= MaxStunGauge )
            {
                TriggerStun();
            }
        }

        /// <summary>
        /// スタンを発生させる
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TriggerStun()
        {
            if ( IsStunned || IsDead )
                return;

            IsStunned = true;
            CurrentStunGauge = MaxStunGauge;

            // StateSystemに反映
            stateSystem.HealthData.isStunned = true;
            stateSystem.HealthData.stunGauge = CurrentStunGauge;
            stateSystem.ReportActionStateChange(ActionState.Stunned);

            Debug.Log($"{gameObject.name}: スタン状態になりました");
        }

        /// <summary>
        /// スタン回復の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateStunRecovery()
        {
            if ( IsStunned && CurrentStunGauge > 0f )
            {
                CurrentStunGauge -= stunRecoveryRate * Time.deltaTime;
                stateSystem.HealthData.stunGauge = CurrentStunGauge;

                if ( CurrentStunGauge <= 0f )
                {
                    CurrentStunGauge = 0f;
                    RecoverFromStun();
                }
            }
        }

        /// <summary>
        /// 無敵状態を設定
        /// </summary>
        /// <param name="duration">無敵時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetInvincible(float duration)
        {
            IsInvincible = true;
            stateSystem.HealthData.isInvincible = true;
            stateSystem.HealthData.invincibilityTimer = duration;

            UniRx.Observable.Timer(TimeSpan.FromSeconds(duration))
                .Subscribe(_ => RemoveInvincible())
                .AddTo(disposables);
        }

        /// <summary>
        /// 怯み状態を設定
        /// </summary>
        /// <param name="duration">怯み時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFlinching(float duration)
        {
            IsFlinching = true;
            stateSystem.HealthData.isFlinching = true;
            stateSystem.ReportActionStateChange(ActionState.Flinching);

            UniRx.Observable.Timer(TimeSpan.FromSeconds(duration))
                .Subscribe(_ => RecoverFromFlinching())
                .AddTo(disposables);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ヘルスの初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeHealth()
        {
            MaxHealth = characterController.Settings?.maxHealth ?? 500f;
            MaxStunGauge = characterController.Settings?.maxStunGauge ?? 100f;
            stunRecoveryRate = characterController.Settings?.stunGaugeRecoveryRate ?? 25f;

            CurrentHealth = MaxHealth;
            CurrentHealthPercentage = 1f;
            CurrentStunGauge = 0f;
            IsDead = false;
            IsStunned = false;
            IsFlinching = false;
            IsInvincible = false;
        }

        /// <summary>
        /// ヘルス状態の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateHealthState()
        {
            // StateSystemと同期
            IsStunned = stateSystem.HealthData.isStunned;
            IsFlinching = stateSystem.HealthData.isFlinching;
            IsInvincible = stateSystem.HealthData.isInvincible;
            IsDead = stateSystem.HealthData.isDead;
        }

        /// <summary>
        /// 死亡時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDeath()
        {
            IsDead = true;
            stateSystem.HealthData.isDead = true;

            Debug.Log($"{gameObject.name}: 死亡しました");

            // 死亡エフェクトや処理をここに追加
        }

        /// <summary>
        /// 復活時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnRevive()
        {
            IsDead = false;
            stateSystem.HealthData.isDead = false;
            stateSystem.ReportActionStateChange(ActionState.Idle);

            Debug.Log($"{gameObject.name}: 復活しました");
        }

        /// <summary>
        /// スタンからの回復
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecoverFromStun()
        {
            IsStunned = false;
            stateSystem.HealthData.isStunned = false;
            stateSystem.ReportActionStateChange(ActionState.Idle);

            Debug.Log($"{gameObject.name}: スタンから回復しました");
        }

        /// <summary>
        /// 怯みからの回復
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecoverFromFlinching()
        {
            IsFlinching = false;
            stateSystem.HealthData.isFlinching = false;
            stateSystem.ReportActionStateChange(ActionState.Idle);
        }

        /// <summary>
        /// 無敵状態の解除
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveInvincible()
        {
            IsInvincible = false;
            stateSystem.HealthData.isInvincible = false;
            stateSystem.HealthData.invincibilityTimer = 0f;
        }

        #endregion

        #region Debug Methods

        [Title("デバッグ機能")]
        [Button("ダメージテスト(50)", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugTakeDamage()
        {
            TakeDamage(50f);
        }

        [Button("体力全回復", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugFullHeal()
        {
            SetHealth(MaxHealth);
            Debug.Log($"{gameObject.name}: 体力を全回復しました");
        }

        [Button("スタン発生", ButtonSizes.Medium)]
        [GUIColor(1f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugTriggerStun()
        {
            TriggerStun();
        }

        [Button("スタンゲージリセット", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugResetStun()
        {
            CurrentStunGauge = 0f;
            IsStunned = false;
            stateSystem.HealthData.stunGauge = 0f;
            stateSystem.HealthData.isStunned = false;
            Debug.Log($"{gameObject.name}: スタンゲージをリセットしました");
        }

        [Button("無敵状態(3秒)", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugSetInvincible()
        {
            SetInvincible(3f);
            Debug.Log($"{gameObject.name}: 3秒間無敵状態になりました");
        }

        [ShowInInspector, PropertyRange(0, 1)]
        [PropertyTooltip("デバッグ用体力設定")]
        private float debugHealthPercentage = 1f;

        [Button("デバッグ体力設定", ButtonSizes.Medium)]
        [GUIColor(1f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugSetHealthPercentage()
        {
            SetHealth(MaxHealth * debugHealthPercentage);
            Debug.Log($"{gameObject.name}: 体力を{debugHealthPercentage:P0}に設定しました");
        }

        #endregion

        #region SRDebugger Integration

        [System.ComponentModel.Category("SRDebugger - ヘルス")]
        public float DebugCurrentHealth
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CurrentHealth;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetHealth(value);
        }

        [System.ComponentModel.Category("SRDebugger - ヘルス")]
        public float DebugCurrentStunGauge
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CurrentStunGauge;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => CurrentStunGauge = Mathf.Clamp(value, 0f, MaxStunGauge);
        }

        [System.ComponentModel.Category("SRDebugger - ヘルス")]
        public bool DebugIsStunned
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsStunned;
        }

        [System.ComponentModel.Category("SRDebugger - ヘルス")]
        public bool DebugIsInvincible
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsInvincible;
        }

        [System.ComponentModel.Category("SRDebugger - ヘルス")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugKill() => SetHealth(0f);

        [System.ComponentModel.Category("SRDebugger - ヘルス")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugFullHealSR() => DebugFullHeal();

        [System.ComponentModel.Category("SRDebugger - ヘルス")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugForceStun() => TriggerStun();

        #endregion
    }
}
