using System.Runtime.CompilerServices;
using UnityEngine;
using UniRx;
using System;
using Unity.Mathematics;
using Unity.Burst;
using NaughtyAttributes;

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
        private readonly float _maxEnergy;

        /// <summary>
        /// エネルギーの更新
        /// </summary>
        /// <param name="changeEnergy"></param>
        [BurstCompile]
        public void UpdateEnergy(float changeEnergy)
        {
            lastEnergy = currentEnergy;
            currentEnergy = math.min(_maxEnergy, currentEnergy + changeEnergy);
            energyPercentage = (currentEnergy / _maxEnergy);

            // エネルギー枯渇状態の更新
            // すでに枯渇状態の場合は最大エネルギーに達するまで枯渇状態を維持
            isEnergyDepleted = isEnergyDepleted ? currentEnergy <= _maxEnergy : currentEnergy <= 0;
        }

        public void SetEnergy(float changeEnergy)
        {
            currentEnergy = math.min(_maxEnergy, changeEnergy);
            energyPercentage = (currentEnergy / _maxEnergy);

            // エネルギー枯渇状態の更新
            // すでに枯渇状態の場合は最大エネルギーに達するまで枯渇状態を維持
            isEnergyDepleted = isEnergyDepleted ? currentEnergy <= _maxEnergy : currentEnergy <= 0;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="maxEnergy"></param>
        public EnergyData(float maxEnergy)
        {
            lastEnergy = maxEnergy;
            currentEnergy = maxEnergy;
            this._maxEnergy = maxEnergy;
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
        private EnergyData _currentEnergyData;

        [Tooltip("最大エネルギー量")]
        public float MaxEnergy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        [Tooltip("現在の回復速度")]
        public float CurrentRecoveryRate { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; }

        // 内部状態
        private bool _wasEnergyDepleted = false;
        private float _energyRecoveryMultiplier = 1;
        private float _energyRecoveryBonusEndTime = 0;


        protected override void OnInitialized()
        {
            InitializeEnergy();
            _currentEnergyData = new EnergyData(MaxEnergy);
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
            if (!_currentEnergyData.isEnergyDepleted)
                return false;

            _currentEnergyData.UpdateEnergy(-1 * amount);
            NotifyObservers(_currentEnergyData);
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
            _currentEnergyData.UpdateEnergy(amount);
            NotifyObservers(_currentEnergyData);
            return true;
        }

        /// <summary>
        /// エネルギーを強制的に設定（デバッグ用）
        /// </summary>
        /// <param name="amount">設定量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEnergy(float amount)
        {
            _currentEnergyData.SetEnergy(Mathf.Clamp(amount, 0f, MaxEnergy));
            NotifyObservers(_currentEnergyData);
        }

        /// <summary>
        /// エネルギー回復ボーナスを適用
        /// </summary>
        /// <param name="multiplier">回復倍率</param>
        /// <param name="duration">持続時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyEnergyRecoveryBonus(float multiplier, float duration)
        {
            _energyRecoveryMultiplier = multiplier;
            _energyRecoveryBonusEndTime = Time.time + duration;
        }

        /// <summary>
        /// 通常エネルギー回復処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateEnergyRecovery()
        {
            // 回復速度の決定
            float baseRecoveryRate = _currentEnergyData.isEnergyDepleted ?
                Settings.energy.fastRecoveryRate : Settings.energy.normalRecoveryRate;

            // ボーナス倍率の適用
            if (Time.time < _energyRecoveryBonusEndTime)
            {
                CurrentRecoveryRate = baseRecoveryRate * _energyRecoveryMultiplier;
            }
            else
            {
                CurrentRecoveryRate = baseRecoveryRate;
                _energyRecoveryMultiplier = 1f;
            }

            // エネルギー回復実行
            if (_currentEnergyData.currentEnergy < MaxEnergy)
            {
                _currentEnergyData.UpdateEnergy(CurrentRecoveryRate * Time.deltaTime);
                NotifyObservers(_currentEnergyData);
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
            _currentEnergyData = new EnergyData(MaxEnergy);
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
    }
}
