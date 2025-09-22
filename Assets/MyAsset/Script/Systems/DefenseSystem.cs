using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;
using System;
using NaughtyAttributes;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 防御データの構造体
    /// </summary>
    [System.Serializable]
    public struct DefenseData
    {
        public bool isGuarding;
        public bool isBlocking;
        public AttackDirection guardDirection;
        public float lastBlockTime;
        public bool blockSuccess;
        public bool hasEnergyBonus;
        public bool isEnergyShieldActive;
        public float energyShieldDurability;
    }

    /// <summary>
    /// 防御システム - ガード、ブロッキング、防御状態を管理
    /// </summary>
    public class DefenseSystem : BaseSystem<DefenseData>
    {

        // 防御状態
        private DefenseData _currentDefenseData;

        [Header("現在の状態")]

        [Tooltip("現在ガード中かどうか")]
        public bool IsGuarding { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [Tooltip("現在のガード方向")]
        public AttackDirection GuardDirection { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = AttackDirection.Up;

        [Tooltip("ブロッキング判定ウィンドウ中かどうか")]
        public bool IsInBlockWindow { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [Tooltip("ガード成功によるエネルギーボーナス中かどうか")]
        public bool HasGuardEnergyBonus { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        // 内部状態
        private float _blockWindowStartTime = 0f;
        private float _guardEnergyBonusEndTime = 0f;
        private AttackDirection _lastBlockDirection = AttackDirection.Up;

        /// <summary>
        /// エネルギー切れ時のシールド関連フィールド
        /// </summary>
        [Header("エネルギー切れシールド設定")]

        [Tooltip("エネルギー切れシールドが展開中かどうか")]
        private bool _isEnergyShieldActive = false;

        [Tooltip("エネルギー切れシールドの現在耐久値")]
        private float _energyShieldDurability = 100f;

        [Tooltip("エネルギー切れシールドの最大耐久値")]
        private const float k_MAX_ENERGY_SHIELD_DURABILITY = 100f;

        [Tooltip("エネルギー切れシールド展開中の移動速度減少率")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _energyShieldMovementSpeedReduction = 0.5f;

        protected override void OnInitialized()
        {

            // 初期データの設定
            UpdateDefenseData();
        }

        protected override void SetupObservables()
        {

        }

        private void UpdateAndNotifyDefenseData()
        {
            UpdateDefenseData();
            NotifyObservers(_currentDefenseData);
        }

        private void UpdateDefenseData()
        {
            _currentDefenseData = new DefenseData
            {
                isGuarding = IsGuarding,
                isBlocking = IsInBlockWindow,
                guardDirection = GuardDirection,
                lastBlockTime = _blockWindowStartTime,
                blockSuccess = false, // 実際のブロッキング成功時に設定
                hasEnergyBonus = HasGuardEnergyBonus,
                isEnergyShieldActive = _isEnergyShieldActive,
                energyShieldDurability = _energyShieldDurability
            };
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            UpdateBlockWindow();
            UpdateGuardEnergyBonus();
            UpdateEnergyShieldDurability(); // エネルギーシールド耐久値更新
        }

        #region Public Defense Methods

        /// <summary>
        /// ガードを開始
        /// </summary>
        /// <param name="direction">ガード方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartGuard(AttackDirection direction)
        {
            IsGuarding = true;
            GuardDirection = direction;
        }

        /// <summary>
        /// ガードを停止
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopGuard()
        {
            IsGuarding = false;
        }

        /// <summary>
        /// ブロッキングを試行（修正版）
        /// ブースト中はブロッキングできない、近接モード時のみ有効
        /// </summary>
        /// <param name="direction">ブロッキング方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AttemptBlock(AttackDirection direction)
        {
            _lastBlockDirection = direction;
            IsInBlockWindow = true;
            _blockWindowStartTime = Time.time;

        }

        /// <summary>
        /// 攻撃を受けた時の防御判定（修正版）
        /// エネルギー切れ状態の正しい判定を追加
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>防御結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DamageResult ProcessDefense(AttackInfo attackInfo)
        {
            // ブロッキング判定（最優先）
            if (IsInBlockWindow && CanBlockAttack(attackInfo))
            {
                return ProcessBlockingSuccess(attackInfo);
            }

            // ガード判定
            if (IsGuarding && CanGuardAttack(attackInfo))
            {
                return ProcessGuardSuccess(attackInfo);
            }

            // 防御失敗
            return ProcessDefenseFailure(attackInfo);
        }

        #region エネルギー切れシールド管理（修正版）

        /// <summary>
        /// エネルギー切れシールドを開始（修正版）
        /// StateSystemのエネルギー切れ状態を参照
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartEnergyShield()
        {

            _isEnergyShieldActive = true;

            Debug.Log("エネルギー切れシールドを展開しました");
        }

        /// <summary>
        /// エネルギー切れシールドを停止
        /// L1ボタンを離すか、回避行動で中断される
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopEnergyShield()
        {
            if (!_isEnergyShieldActive)
                return;

            _isEnergyShieldActive = false;

            // 移動速度を元に戻す
            //if (movementSystem != null)
            //{
            //    movementSystem.RemoveMovementSpeedModifier("EnergyShield");
            //}

            Debug.Log("エネルギー切れシールドを解除しました");
        }

        /// <summary>
        /// エネルギー切れシールドが展開中かどうかを取得
        /// </summary>
        /// <returns>シールド展開中かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnergyShieldActive()
        {
            return _isEnergyShieldActive;
        }

        /// <summary>
        /// エネルギー切れシールドの耐久値を更新
        /// シールドが展開されていない時は自動回復
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateEnergyShieldDurability()
        {
            // シールドが展開されていない時のみ耐久値が回復
            if (!_isEnergyShieldActive && _energyShieldDurability < k_MAX_ENERGY_SHIELD_DURABILITY)
            {
                // 毎秒50ポイント回復
                _energyShieldDurability += 50f * Time.deltaTime;
                _energyShieldDurability = Mathf.Min(_energyShieldDurability, k_MAX_ENERGY_SHIELD_DURABILITY);
            }
        }


        #endregion エネルギー切れシールド管理

        #endregion

        #region Private Defense Processing Methods

        /// <summary>
        /// ブロッキング成功処理
        /// エネルギー回復はStateSystemでする
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>ダメージ結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DamageResult ProcessBlockingSuccess(AttackInfo attackInfo)
        {
            IsInBlockWindow = false;

            // 射撃攻撃に対するブロッキングは移動効果
            if (IsRangedAttack(attackInfo.attackType))
            {
                ExecuteBlockMovement();
            }

            return new DamageResult
            {
                actualDamage = 0f,
                stunAccumulation = 0f,
                energyDamage = 0f,
                wasHit = false,
                wasGuarded = false,
                wasBlocked = true,
                wasJustDodged = false,
                causedStun = false,
                hitPosition = transform.position,
                hitDirection = Vector3.zero
            };
        }

        /// <summary>
        /// ガード成功処理
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>ダメージ結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DamageResult ProcessGuardSuccess(AttackInfo attackInfo)
        {
            float damage = 0f;
            float stunAccumulation = 0f;

            // 強攻撃はガードしても怯み発生
            if (attackInfo.attackType == AttackType.StrongMelee || attackInfo.attackType == AttackType.StrongShoot)
            {
                damage = attackInfo.baseDamage * 0.3f; // 軽減ダメージ
                stunAccumulation = attackInfo.stunAccumulation * 0.5f;
            }
            else
            {
                // 弱攻撃は完全ガード + エネルギーボーナス
                ApplyGuardEnergyBonus();
            }

            return new DamageResult
            {
                actualDamage = damage,
                stunAccumulation = stunAccumulation,
                energyDamage = 0f,
                wasHit = damage > 0f,
                wasGuarded = true,
                wasBlocked = false,
                wasJustDodged = false,
                causedStun = false,
                hitPosition = transform.position,
                hitDirection = Vector3.zero
            };
        }

        /// <summary>
        /// 防御失敗処理
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>ダメージ結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DamageResult ProcessDefenseFailure(AttackInfo attackInfo)
        {
            float damage = attackInfo.baseDamage;
            float stunAccumulation = attackInfo.stunAccumulation;

            // ブロッキング失敗ペナルティ
            if (IsInBlockWindow)
            {
                damage *= Settings.defense.blockFailDamageMultiplier;
                IsInBlockWindow = false;
            }

            return new DamageResult
            {
                actualDamage = damage,
                stunAccumulation = stunAccumulation,
                energyDamage = attackInfo.energyDamage,
                wasHit = true,
                wasGuarded = false,
                wasBlocked = false,
                wasJustDodged = false,
                hitPosition = transform.position,
                hitDirection = Vector3.forward // 簡易的な方向
            };
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// 攻撃をブロッキングできるかどうか
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>ブロッキング可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanBlockAttack(AttackInfo attackInfo)
        {
            return attackInfo.canBeBlocked &&
                   _lastBlockDirection == attackInfo.direction &&
                   (Time.time - _blockWindowStartTime) <= 0.2f;
        }

        /// <summary>
        /// 攻撃をガードできるかどうか
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>ガード可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanGuardAttack(AttackInfo attackInfo)
        {
            return attackInfo.canBeGuarded &&
                   GuardDirection == attackInfo.direction;
        }

        /// <summary>
        /// 遠距離攻撃かどうか
        /// </summary>
        /// <param name="attackType">攻撃タイプ</param>
        /// <returns>遠距離攻撃かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsRangedAttack(AttackType attackType)
        {
            return attackType == AttackType.WeakShoot ||
                   attackType == AttackType.StrongShoot;
        }

        /// <summary>
        /// ガードエネルギーボーナスを適用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyGuardEnergyBonus()
        {
            HasGuardEnergyBonus = true;
            _guardEnergyBonusEndTime = Time.time + Settings.defense.guardEnergyBonusTime;
        }

        /// <summary>
        /// ブロッキング移動を実行
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteBlockMovement()
        {
            Vector3 moveDirection = GetBlockMoveDirection();
            transform.position += moveDirection * Settings.defense.blockMoveDistance;
        }

        /// <summary>
        /// ブロッキング移動方向を取得
        /// </summary>
        /// <returns>移動方向</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 GetBlockMoveDirection()
        {
            return _lastBlockDirection switch
            {
                AttackDirection.Up => transform.forward,
                AttackDirection.Left => -transform.right,
                AttackDirection.Right => transform.right,
                _ => transform.forward
            };
        }

        /// <summary>
        /// ブロッキングウィンドウの更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateBlockWindow()
        {
            if (IsInBlockWindow && (Time.time - _blockWindowStartTime) > 0.2f)
            {
                OnBlockWindowEnd();
            }
        }

        /// <summary>
        /// ブロッキングウィンドウ終了時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnBlockWindowEnd()
        {
            if (IsInBlockWindow)
            {
                IsInBlockWindow = false;
                // ブロッキング失敗ペナルティは実際の被弾時に適用
            }
        }

        /// <summary>
        /// ガードエネルギーボーナスの更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateGuardEnergyBonus()
        {
            if (HasGuardEnergyBonus && Time.time >= _guardEnergyBonusEndTime)
            {
                HasGuardEnergyBonus = false;
            }
        }

        #endregion
    }
}