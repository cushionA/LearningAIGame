using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using UniRx;
using System;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// エネルギーデータの構造体
    /// </summary>
    [System.Serializable]
    public struct EnergyData
    {
        public float currentEnergy;
        public float maxEnergy;
        public float energyPercentage;
        public bool isRecovering;
        public bool isDepleted;
        public float recoveryRate;
        public float recoveryMultiplier;
    }

    /// <summary>
    /// エネルギーシステム - エネルギーの管理と回復を行う
    /// </summary>
    public class EnergySystem : BaseSystem<EnergyData>
    {
        // コンポーネント
        private StateSystem stateSystem;

        // エネルギー状態
        private EnergyData currentEnergyData;

        [Title("現在の状態")]
        [ShowInInspector, ReadOnly]
        [ProgressBar(0, 1)]
        [PropertyTooltip("現在のエネルギー割合")]
        public float CurrentEnergyPercentage { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 1f;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在のエネルギー量")]
        public float CurrentEnergy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("最大エネルギー量")]
        public float MaxEnergy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の回復速度")]
        public float CurrentRecoveryRate { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("エネルギー切れ状態かどうか")]
        public bool IsEnergyDepleted { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        // 内部状態
        private bool wasEnergyDepleted = false;
        private float energyRecoveryMultiplier = 1f;
        private float energyRecoveryBonusEndTime = 0f;

        /// <summary>
        /// 初期化処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Awake()
        {
            // 他のシステムの参照取得は OnInitialized で行う
        }

        protected override void OnInitialized()
        {
            // 他のシステムの参照取得
            stateSystem = GetComponent<StateSystem>();

            if ( Settings?.energy == null )
            {
                DebugLogError("EnergySettingsが見つかりません");
                return;
            }

            InitializeEnergy();
            UpdateEnergyData();
        }

        protected override void SetupObservables()
        {
            // エネルギー状態の更新をObservableで通知
            UniRx.Observable.EveryUpdate()
                .Subscribe(_ => UpdateAndNotifyEnergyData())
                .AddTo(disposables);
        }

        private void UpdateAndNotifyEnergyData()
        {
            UpdateEnergyData();
            NotifyObservers(currentEnergyData);
        }

        private void UpdateEnergyData()
        {
            currentEnergyData = new EnergyData
            {
                currentEnergy = CurrentEnergy,
                maxEnergy = MaxEnergy,
                energyPercentage = CurrentEnergyPercentage,
                isRecovering = CurrentEnergy < MaxEnergy,
                isDepleted = IsEnergyDepleted,
                recoveryRate = CurrentRecoveryRate,
                recoveryMultiplier = energyRecoveryMultiplier
            };
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            UpdateEnergyRecovery();
            UpdateEnergyState();
        }

        #region Public Methods

        /// <summary>
        /// エネルギーが使用可能かどうか（修正版）
        /// エネルギー切れ中は一切使用不可
        /// </summary>
        /// <param name="amount">使用予定量</param>
        /// <returns>使用可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanUseEnergy(float amount)
        {
            // エネルギー切れ中は一切使用不可
            if ( IsEnergyDepleted )
                return false;

            return CurrentEnergy >= amount;
        }

        /// <summary>
        /// エネルギーを使用（修正版）
        /// エネルギー切れ中は使用不可
        /// </summary>
        /// <param name="amount">使用量</param>
        /// <returns>使用に成功したかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UseEnergy(float amount)
        {
            if ( !CanUseEnergy(amount) )
                return false;

            CurrentEnergy = Mathf.Max(0f, CurrentEnergy - amount);
            UpdateEnergyPercentage();

            return true;
        }

        /// <summary>
        /// エネルギーを回復
        /// </summary>
        /// <param name="amount">回復量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecoverEnergy(float amount)
        {
            CurrentEnergy = Mathf.Min(MaxEnergy, CurrentEnergy + amount);
            UpdateEnergyPercentage();
        }

        /// <summary>
        /// エネルギーを強制的に設定（デバッグ用）
        /// </summary>
        /// <param name="amount">設定量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEnergy(float amount)
        {
            CurrentEnergy = Mathf.Clamp(amount, 0f, MaxEnergy);
            UpdateEnergyPercentage();
        }

        /// <summary>
        /// エネルギー回復ボーナスを適用
        /// </summary>
        /// <param name="multiplier">回復倍率</param>
        /// <param name="duration">持続時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyEnergyRecoveryBonus(float multiplier, float duration)
        {
            energyRecoveryMultiplier = multiplier;
            energyRecoveryBonusEndTime = Time.time + duration;
        }

        /// <summary>
        /// 通常エネルギー回復処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateEnergyRecovery()
        {
            if ( stateSystem.IsEnergyRecoveryPaused )
                return;

            // 回復速度の決定
            float baseRecoveryRate = IsEnergyDepleted ?
                Settings.energy.fastRecoveryRate : Settings.energy.normalRecoveryRate;

            // ボーナス倍率の適用
            if ( Time.time < energyRecoveryBonusEndTime )
            {
                CurrentRecoveryRate = baseRecoveryRate * energyRecoveryMultiplier;
            }
            else
            {
                CurrentRecoveryRate = baseRecoveryRate;
                energyRecoveryMultiplier = 1f;
            }

            // エネルギー回復実行
            if ( CurrentEnergy < MaxEnergy )
            {
                RecoverEnergy(CurrentRecoveryRate * Time.deltaTime);
            }
        }

        /// <summary>
        /// 強制エネルギー回復（エネルギーバリアモード用）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceEnergyRecovery()
        {
            RecoverEnergy(Settings.energy.fastRecoveryRate * 1.5f * Time.deltaTime);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// エネルギーの初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeEnergy()
        {
            MaxEnergy = Settings.energy.maxEnergy;
            CurrentEnergy = MaxEnergy;
            CurrentEnergyPercentage = 1f;
            CurrentRecoveryRate = Settings.energy.normalRecoveryRate;
        }

        /// <summary>
        /// エネルギー割合の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateEnergyPercentage()
        {
            CurrentEnergyPercentage = CurrentEnergy / MaxEnergy;
            stateSystem.ReportEnergyChange(CurrentEnergyPercentage);
        }

        /// <summary>
        /// エネルギー状態の更新（修正版）
        /// エネルギー切れ状態をStateSystemに正しく報告
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateEnergyState()
        {
            // エネルギー切れ状態の判定
            bool isCurrentlyAtZero = CurrentEnergyPercentage <= 0f;
            bool hasFullyRecovered = CurrentEnergyPercentage >= 1f;

            if ( isCurrentlyAtZero && !IsEnergyDepleted )
            {
                // エネルギーが0%になった瞬間
                IsEnergyDepleted = true;
                OnEnergyDepleted();

                // StateSystemにエネルギー切れ状態を報告
                stateSystem.ReportEnergyDepletedState(true);
            }
            else if ( hasFullyRecovered && IsEnergyDepleted )
            {
                // エネルギーが100%回復した瞬間
                IsEnergyDepleted = false;
                OnEnergyRecovered();

                // StateSystemにエネルギー切れ解除を報告
                stateSystem.ReportEnergyDepletedState(false);
            }

            wasEnergyDepleted = IsEnergyDepleted;
        }

        /// <summary>
        /// エネルギー切れ時の処理（修正版）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnEnergyDepleted()
        {
            Debug.Log($"{gameObject.name}: エネルギー切れ - 全回復まで制限モードが継続");
        }

        /// <summary>
        /// エネルギー回復時の処理（修正版）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnEnergyRecovered()
        {
            Debug.Log($"{gameObject.name}: エネルギー全回復 - 制限モード解除");
        }

        #endregion

        #region Debug Methods

        [Title("デバッグ機能")]
        [Button("エネルギー全回復", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugFullRecover()
        {
            SetEnergy(MaxEnergy);
            Debug.Log($"{gameObject.name}: エネルギーを全回復しました");
        }

        [Button("エネルギー枯渇", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugDepleteFully()
        {
            SetEnergy(0f);
            Debug.Log($"{gameObject.name}: エネルギーを枯渇させました");
        }

        [Button("回復ボーナス付与", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugApplyRecoveryBonus()
        {
            ApplyEnergyRecoveryBonus(2f, 5f);
            Debug.Log($"{gameObject.name}: 5秒間2倍回復ボーナスを適用しました");
        }

        [ShowInInspector, PropertyRange(0, 1)]
        [PropertyTooltip("デバッグ用エネルギー設定")]
        private float debugEnergyPercentage = 1f;

        [Button("デバッグエネルギー設定", ButtonSizes.Medium)]
        [GUIColor(1f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugSetEnergyPercentage()
        {
            SetEnergy(MaxEnergy * debugEnergyPercentage);
            Debug.Log($"{gameObject.name}: エネルギーを{debugEnergyPercentage:P0}に設定しました");
        }

        #endregion

        #region SRDebugger Integration

        [System.ComponentModel.Category("SRDebugger - エネルギー")]
        public float DebugCurrentEnergy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CurrentEnergy;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetEnergy(value);
        }

        [System.ComponentModel.Category("SRDebugger - エネルギー")]
        public float DebugRecoveryRate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CurrentRecoveryRate;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => CurrentRecoveryRate = value;
        }

        [System.ComponentModel.Category("SRDebugger - エネルギー")]
        public bool DebugIsEnergyDepleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsEnergyDepleted;
        }

        [System.ComponentModel.Category("SRDebugger - エネルギー")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugForceDepletion() => DebugDepleteFully();

        [System.ComponentModel.Category("SRDebugger - エネルギー")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugForceRecovery() => DebugFullRecover();

        #endregion
    }
}
