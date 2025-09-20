using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using UniRx;
using System;

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
        private DefenseData currentDefenseData;

        [Title("現在の状態")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在ガード中かどうか")]
        public bool IsGuarding { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在のガード方向")]
        public AttackDirection GuardDirection { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = AttackDirection.Up;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("ブロッキング判定ウィンドウ中かどうか")]
        public bool IsInBlockWindow { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("ガード成功によるエネルギーボーナス中かどうか")]
        public bool HasGuardEnergyBonus { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        // 内部状態
        private float blockWindowStartTime = 0f;
        private float guardEnergyBonusEndTime = 0f;
        private AttackDirection lastBlockDirection = AttackDirection.Up;

        /// <summary>
        /// エネルギー切れ時のシールド関連フィールド
        /// </summary>
        [Title("エネルギー切れシールド設定")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("エネルギー切れシールドが展開中かどうか")]
        private bool isEnergyShieldActive = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("エネルギー切れシールドの現在耐久値")]
        private float energyShieldDurability = 100f;

        [PropertyTooltip("エネルギー切れシールドの最大耐久値")]
        private const float MAX_ENERGY_SHIELD_DURABILITY = 100f;

        [PropertyTooltip("エネルギー切れシールド展開中の移動速度減少率")]
        [Range(0.1f, 1f)]
        [SerializeField] private float energyShieldMovementSpeedReduction = 0.5f;

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
            energySystem = GetComponent<EnergySystem>();
            movementSystem = GetComponent<MovementSystem>();

            if ( Settings?.defense == null )
            {
                DebugLogError("DefenseSettingsが見つかりません");
                return;
            }

            // 初期データの設定
            UpdateDefenseData();
        }

        protected override void SetupObservables()
        {
            // 防御状態の更新をObservableで通知
            UniRx.Observable.EveryUpdate()
                .Subscribe(_ => UpdateAndNotifyDefenseData())
                .AddTo(disposables);
        }

        private void UpdateAndNotifyDefenseData()
        {
            UpdateDefenseData();
            NotifyObservers(currentDefenseData);
        }

        private void UpdateDefenseData()
        {
            currentDefenseData = new DefenseData
            {
                isGuarding = IsGuarding,
                isBlocking = IsInBlockWindow,
                guardDirection = GuardDirection,
                lastBlockTime = blockWindowStartTime,
                blockSuccess = false, // 実際のブロッキング成功時に設定
                hasEnergyBonus = HasGuardEnergyBonus,
                isEnergyShieldActive = isEnergyShieldActive,
                energyShieldDurability = energyShieldDurability
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
            if ( !CanGuard() )
                return;

            IsGuarding = true;
            GuardDirection = direction;
            stateSystem.ReportActionStateChange(ActionState.Guarding);
        }

        /// <summary>
        /// ガードを停止
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopGuard()
        {
            IsGuarding = false;
            stateSystem.ReportActionStateChange(ActionState.Idle);
        }

        /// <summary>
        /// ブロッキングを試行（修正版）
        /// ブースト中はブロッキングできない、近接モード時のみ有効
        /// </summary>
        /// <param name="direction">ブロッキング方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AttemptBlock(AttackDirection direction)
        {
            if ( !CanBlock() )
                return;

            lastBlockDirection = direction;
            IsInBlockWindow = true;
            blockWindowStartTime = Time.time;

            // ブロッキング失敗時のペナルティ予約
            UniRx.Observable.Timer(TimeSpan.FromSeconds(0.2f))
                .Subscribe(_ => OnBlockWindowEnd())
                .AddTo(disposables);
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
            if ( IsInBlockWindow && CanBlockAttack(attackInfo) )
            {
                return ProcessBlockingSuccess(attackInfo);
            }

            // ガード判定
            if ( IsGuarding && CanGuardAttack(attackInfo) )
            {
                return ProcessGuardSuccess(attackInfo);
            }

            // エネルギー切れシールド判定（手動展開かつエネルギー切れ状態）
            if ( isEnergyShieldActive && stateSystem.IsEnergyDepleted )
            {
                return ProcessEnergyShieldDefense(attackInfo);
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
            // エネルギー切れ状態でない場合は使用不可
            if ( !stateSystem.IsEnergyDepleted )
            {
                Debug.LogWarning("エネルギーが切れていないため、エネルギーシールドは使用できません");
                return;
            }

            isEnergyShieldActive = true;
            stateSystem.ReportActionStateChange(ActionState.EnergyShielding);

            // 移動速度を減少させる
            if ( movementSystem != null )
            {
                movementSystem.ApplyMovementSpeedModifier(energyShieldMovementSpeedReduction, "EnergyShield");
            }

            Debug.Log("エネルギー切れシールドを展開しました");
        }

        /// <summary>
        /// エネルギー切れシールドを停止
        /// L1ボタンを離すか、回避行動で中断される
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopEnergyShield()
        {
            if ( !isEnergyShieldActive )
                return;

            isEnergyShieldActive = false;
            stateSystem.ReportActionStateChange(ActionState.Idle);

            // 移動速度を元に戻す
            if ( movementSystem != null )
            {
                movementSystem.RemoveMovementSpeedModifier("EnergyShield");
            }

            Debug.Log("エネルギー切れシールドを解除しました");
        }

        /// <summary>
        /// エネルギー切れシールドが展開中かどうかを取得
        /// </summary>
        /// <returns>シールド展開中かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnergyShieldActive()
        {
            return isEnergyShieldActive;
        }

        /// <summary>
        /// エネルギー切れシールドの耐久値を更新
        /// シールドが展開されていない時は自動回復
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateEnergyShieldDurability()
        {
            // シールドが展開されていない時のみ耐久値が回復
            if ( !isEnergyShieldActive && energyShieldDurability < MAX_ENERGY_SHIELD_DURABILITY )
            {
                // 毎秒50ポイント回復
                energyShieldDurability += 50f * Time.deltaTime;
                energyShieldDurability = Mathf.Min(energyShieldDurability, MAX_ENERGY_SHIELD_DURABILITY);
            }
        }

        /// <summary>
        /// エネルギー切れシールド防御処理（修正版）
        /// 手動展開されたシールドのみが防御効果を発揮
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>ダメージ結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DamageResult ProcessEnergyShieldDefense(AttackInfo attackInfo)
        {
            // 攻撃方向とシールド方向が合っていれば防御可能
            if ( stateSystem.CurrentDirection == attackInfo.direction )
            {
                // 強攻撃はシールドを貫通
                if ( attackInfo.attackType == AttackType.StrongMelee || attackInfo.attackType == AttackType.StrongShoot )
                {
                    Debug.Log("強攻撃：エネルギーシールドを貫通");
                    return ProcessDefenseFailure(attackInfo);
                }

                // シールド耐久値をチェック
                float damage = attackInfo.baseDamage;
                if ( damage > energyShieldDurability )
                {
                    // 耐久値を超えるダメージでスタン発生
                    energyShieldDurability = 0f;
                    StopEnergyShield();
                    stateSystem.ForceStun(2f); // 2秒間スタン

                    Debug.Log($"シールド耐久値超過：スタン発生 (ダメージ{damage} > 耐久値{energyShieldDurability})");

                    return new DamageResult
                    {
                        actualDamage = damage * 0.5f, // 軽減ダメージ
                        stunAccumulation = 100f, // 強制スタン
                        energyDamage = 0f,
                        wasHit = true,
                        wasGuarded = false,
                        wasBlocked = false,
                        wasJustDodged = false,
                        causedStun = true,
                        hitPosition = transform.position,
                        hitDirection = Vector3.zero
                    };
                }
                else
                {
                    // 耐久値内なら防御成功、耐久値を減らす
                    energyShieldDurability -= damage;

                    Debug.Log($"エネルギーシールド防御成功：耐久値{energyShieldDurability}/{MAX_ENERGY_SHIELD_DURABILITY}");

                    return new DamageResult
                    {
                        actualDamage = 0f,
                        stunAccumulation = 0f,
                        energyDamage = 0f,
                        wasHit = false,
                        wasGuarded = true,
                        wasBlocked = false,
                        wasJustDodged = false,
                        causedStun = false,
                        hitPosition = transform.position,
                        hitDirection = Vector3.zero
                    };
                }
            }

            // 方向が合わない場合は防御失敗
            Debug.Log("攻撃方向とシールド方向が不一致：防御失敗");
            return ProcessDefenseFailure(attackInfo);
        }

        #endregion エネルギー切れシールド管理

        #endregion

        #region Private Defense Processing Methods

        /// <summary>
        /// ブロッキング成功処理
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>ダメージ結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DamageResult ProcessBlockingSuccess(AttackInfo attackInfo)
        {
            IsInBlockWindow = false;

            // エネルギー回復
            energySystem.RecoverEnergy(Settings.defense.blockEnergyRecovery);

            // 射撃攻撃に対するブロッキングは移動効果
            if ( IsRangedAttack(attackInfo.attackType) )
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
            if ( attackInfo.attackType == AttackType.StrongMelee || attackInfo.attackType == AttackType.StrongShoot )
            {
                damage = attackInfo.baseDamage * 0.3f; // 軽減ダメージ
                stunAccumulation = attackInfo.stunAccumulation * 0.5f;
                stateSystem.HealthData.isFlinching = true;

                // 怯み時間設定
                UniRx.Observable.Timer(TimeSpan.FromSeconds(0.5f))
                    .Subscribe(_ => stateSystem.HealthData.isFlinching = false)
                    .AddTo(disposables);
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
            if ( IsInBlockWindow )
            {
                damage *= Settings.defense.blockFailDamageMultiplier;
                energySystem.UseEnergy(Settings.defense.blockFailEnergyCost);
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
                causedStun = stateSystem.HealthData.stunGauge + stunAccumulation >= 100f,
                hitPosition = transform.position,
                hitDirection = Vector3.forward // 簡易的な方向
            };
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// ガードが可能かどうか
        /// </summary>
        /// <returns>ガード可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanGuard()
        {
            return stateSystem.CurrentActionMode == ActionMode.Melee &&
                   stateSystem.CanExecuteAction(ActionType.Guard);
        }

        /// <summary>
        /// ブロッキングが可能かどうかを判定（修正版）
        /// ブースト中は無効、近接モード時のみ有効
        /// </summary>
        /// <returns>ブロッキング可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanBlock()
        {
            // ブースト中はブロッキングできない
            return stateSystem.CurrentActionMode == ActionMode.Melee &&
                   stateSystem.CurrentActionState != ActionState.Boosting && // ブースト中は無効
                   stateSystem.CanExecuteAction(ActionType.Block) &&
                   !IsInBlockWindow;
        }

        /// <summary>
        /// 攻撃をブロッキングできるかどうか
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>ブロッキング可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanBlockAttack(AttackInfo attackInfo)
        {
            return attackInfo.canBeBlocked &&
                   lastBlockDirection == attackInfo.direction &&
                   (Time.time - blockWindowStartTime) <= 0.2f;
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
                   attackType == AttackType.StrongShoot ||
                   attackType == AttackType.RangedSkill;
        }

        /// <summary>
        /// ガードエネルギーボーナスを適用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyGuardEnergyBonus()
        {
            HasGuardEnergyBonus = true;
            guardEnergyBonusEndTime = Time.time + Settings.defense.guardEnergyBonusTime;
            energySystem.ApplyEnergyRecoveryBonus(Settings.defense.guardEnergyBonusMultiplier, Settings.defense.guardEnergyBonusTime);
        }

        /// <summary>
        /// ブロッキング移動を実行
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteBlockMovement()
        {
            Vector3 moveDirection = GetBlockMoveDirection();
            transform.position += moveDirection * Settings.defense.blockMoveDistance;

            // 無敵時間付与
            stateSystem.HealthData.isInvincible = true;
            stateSystem.HealthData.invincibilityTimer = 0.5f;
        }

        /// <summary>
        /// ブロッキング移動方向を取得
        /// </summary>
        /// <returns>移動方向</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 GetBlockMoveDirection()
        {
            return lastBlockDirection switch
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
            if ( IsInBlockWindow && (Time.time - blockWindowStartTime) > 0.2f )
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
            if ( IsInBlockWindow )
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
            if ( HasGuardEnergyBonus && Time.time >= guardEnergyBonusEndTime )
            {
                HasGuardEnergyBonus = false;
            }
        }

        #endregion

        #region Debug Methods

        [Title("デバッグ機能")]
        [Button("ガード開始（上）", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugStartGuardUp()
        {
            StartGuard(AttackDirection.Up);
        }

        [Button("ブロッキング試行（上）", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugAttemptBlockUp()
        {
            AttemptBlock(AttackDirection.Up);
        }

        [Button("ガード停止", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugStopGuard()
        {
            StopGuard();
        }

        [Button("エネルギーシールド開始", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugStartEnergyShield()
        {
            StartEnergyShield();
        }

        [Button("エネルギーシールド停止", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugStopEnergyShield()
        {
            StopEnergyShield();
        }

        [Button("テスト攻撃受け", ButtonSizes.Medium)]
        [GUIColor(1f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugTestDefense()
        {
            var testAttack = new AttackInfo
            {
                attackType = AttackType.WeakMelee,
                direction = AttackDirection.Up,
                baseDamage = 25f,
                stunAccumulation = 12.5f,
                canBeGuarded = true,
                canBeBlocked = true
            };

            var result = ProcessDefense(testAttack);
            Debug.Log($"防御テスト結果: ダメージ{result.actualDamage}, ガード{result.wasGuarded}, ブロック{result.wasBlocked}");
        }

        #endregion

        #region SRDebugger Integration

        [System.ComponentModel.Category("SRDebugger - 防御")]
        public bool DebugIsGuarding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsGuarding;
        }

        [System.ComponentModel.Category("SRDebugger - 防御")]
        public bool DebugIsInBlockWindow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsInBlockWindow;
        }

        [System.ComponentModel.Category("SRDebugger - 防御")]
        public string DebugGuardDirection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GuardDirection.ToString();
        }

        [System.ComponentModel.Category("SRDebugger - 防御")]
        public bool DebugIsEnergyShieldActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => isEnergyShieldActive;
        }

        [System.ComponentModel.Category("SRDebugger - 防御")]
        public float DebugEnergyShieldDurability
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => energyShieldDurability;
        }

        [System.ComponentModel.Category("SRDebugger - 防御")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugForceGuard() => StartGuard(AttackDirection.Up);

        [System.ComponentModel.Category("SRDebugger - 防御")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugForceBlock() => AttemptBlock(AttackDirection.Up);

        [System.ComponentModel.Category("SRDebugger - 防御")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugForceEnergyShield() => StartEnergyShield();

        #endregion
    }
}