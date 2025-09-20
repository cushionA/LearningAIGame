using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using UniRx;
using System;
using Unity.Mathematics;
using Unity.Burst;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// エネルギーデータの構造体
    /// </summary>
    [System.Serializable]
    public struct EnergyData
    {
        /// <summary>
        /// エネルギー量
        /// </summary>
        public float currentEnergy;

        /// <summary>
        /// 前回の変更前のエネルギー量
        /// </summary>
        public float lastEnergy;

        /// <summary>
        /// エネルギー割合
        /// </summary>
        public float energyPercentage;

        /// <summary>
        /// エネルギー枯渇中か
        /// </summary>
        public bool isEnergyDepleted;

        /// <summary>
        /// 最大エネルギー量
        /// </summary>
        private readonly float maxEnergy;

        /// <summary>
        /// エネルギーの更新
        /// </summary>
        /// <param name="changeEnergy"></param>
        [BurstCompile]
        public void UpdateEnergy(float changeEnergy)
        {
            lastEnergy = currentEnergy;
            currentEnergy = math.min(maxEnergy, currentEnergy + changeEnergy);
            energyPercentage = (currentEnergy / maxEnergy);

            // エネルギー枯渇状態の更新
            // すでに枯渇状態の場合は最大エネルギーに達するまで枯渇状態を維持
            isEnergyDepleted = isEnergyDepleted ? currentEnergy <= maxEnergy : currentEnergy <= 0;
        }

        public void SetEnergy(float changeEnergy)
        {
            currentEnergy = math.min(maxEnergy, changeEnergy);
            energyPercentage = (currentEnergy / maxEnergy);

            // エネルギー枯渇状態の更新
            // すでに枯渇状態の場合は最大エネルギーに達するまで枯渇状態を維持
            isEnergyDepleted = isEnergyDepleted ? currentEnergy <= maxEnergy : currentEnergy <= 0;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="maxEnergy"></param>
        public EnergyData(float maxEnergy)
        {
            lastEnergy = maxEnergy;
            currentEnergy = maxEnergy;
            this.maxEnergy = maxEnergy;
            isEnergyDepleted = false;
            energyPercentage = 1;
        }
    }

    /// <summary>
    /// エネルギーシステム - エネルギーの管理と回復を行う
    /// 
    /// ここで必要になった全体情報
    /// エネルギー回復可能か（＝消費アクションをしていないか）
    /// モード？
    /// </summary>
    public class EnergySystem : BaseSystem<EnergyData>
    {
        /// <summary>
        /// エネルギー状態
        /// </summary>
        private EnergyData currentEnergyData;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("最大エネルギー量")]
        public float MaxEnergy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の回復速度")]
        public float CurrentRecoveryRate { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        // 内部状態
        private bool wasEnergyDepleted = false;
        private float energyRecoveryMultiplier = 1;
        private float energyRecoveryBonusEndTime = 0;

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

            if ( Settings?.energy == null )
            {
                DebugLogError("EnergySettingsが見つかりません");
                return;
            }

            InitializeEnergy();
            currentEnergyData = new EnergyData(MaxEnergy);
        }

        #region Public Methods

        /// <summary>
        /// エネルギーを使用（修正版）
        /// エネルギー切れ中は使用不可
        /// </summary>
        /// <param name="amount">使用量</param>
        /// <returns>使用に成功したかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool UseEnergy(float amount)
        {
            if ( !currentEnergyData.isEnergyDepleted )
                return false;

            currentEnergyData.UpdateEnergy(-1 * amount);
            NotifyObservers(currentEnergyData);
            return true;
        }

        /// <summary>
        /// エネルギーを回復
        /// </summary>
        /// <param name="amount">回復量</param>
        /// <returns>使用に成功したかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RecoverEnergy(float amount)
        {
            currentEnergyData.UpdateEnergy(amount);
            NotifyObservers(currentEnergyData);
            return true;
        }

        /// <summary>
        /// エネルギーを強制的に設定（デバッグ用）
        /// </summary>
        /// <param name="amount">設定量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEnergy(float amount)
        {
            currentEnergyData.SetEnergy(Mathf.Clamp(amount, 0f, MaxEnergy));
            NotifyObservers(currentEnergyData);
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
            float baseRecoveryRate = currentEnergyData.isEnergyDepleted ?
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
            if ( currentEnergyData.currentEnergy < MaxEnergy )
            {
                currentEnergyData.UpdateEnergy(CurrentRecoveryRate * Time.deltaTime);
                NotifyObservers(currentEnergyData);
            }
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
            currentEnergyData = new EnergyData(MaxEnergy);
            CurrentRecoveryRate = Settings.energy.normalRecoveryRate;
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
            get => currentEnergyData.currentEnergy;
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
            get => currentEnergyData.isEnergyDepleted;
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
