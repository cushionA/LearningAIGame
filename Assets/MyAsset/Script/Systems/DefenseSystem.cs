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
    }
    
    /// <summary>
    /// 防御システム - ガード、ブロッキング、防御状態を管理
    /// </summary>
    public class DefenseSystem : BaseSystem<DefenseData>
    {
        // コンポーネント
        private StateSystem stateSystem;
        private EnergySystem energySystem;
        
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
            
            if (Settings?.defense == null)
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
                hasEnergyBonus = HasGuardEnergyBonus
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
        }

        #region Public Defense Methods

        /// <summary>
        /// ガードを開始
        /// </summary>
        /// <param name="direction">ガード方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartGuard(AttackDirection direction)
        {
            if (!CanGuard())
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
        /// ブロッキングを試行
        /// </summary>
        /// <param name="direction">ブロッキング方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AttemptBlock(AttackDirection direction)
        {
            if (!CanBlock())
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
        /// 攻撃を受けた時の防御判定
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

            // エネルギーバリア判定（エネルギー切れ時）
            if (stateSystem.CurrentActionMode == ActionMode.EnergyBarrier)
            {
                return ProcessEnergyBarrierDefense(attackInfo);
            }

            // 防御失敗
            return ProcessDefenseFailure(attackInfo);
        }

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
            if (attackInfo.attackType == AttackType.StrongMelee || attackInfo.attackType == AttackType.StrongRanged)
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
        /// エネルギーバリア防御処理
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>ダメージ結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DamageResult ProcessEnergyBarrierDefense(AttackInfo attackInfo)
        {
            // 方向が合っていれば防御可能
            if (stateSystem.CurrentDirection == attackInfo.direction)
            {
                // 強攻撃はバリアを貫通
                if (attackInfo.attackType == AttackType.StrongMelee || attackInfo.attackType == AttackType.StrongRanged)
                {
                    return ProcessDefenseFailure(attackInfo);
                }

                // スタンゲージ蓄積はあるが無ダメージ
                float stunAccumulation = attackInfo.stunAccumulation;
                stateSystem.HealthData.stunGauge += stunAccumulation;

                return new DamageResult
                {
                    actualDamage = 0f,
                    stunAccumulation = stunAccumulation,
                    energyDamage = 0f,
                    wasHit = false,
                    wasGuarded = true,
                    wasBlocked = false,
                    wasJustDodged = false,
                    causedStun = stateSystem.HealthData.stunGauge >= 100f,
                    hitPosition = transform.position,
                    hitDirection = Vector3.zero
                };
            }

            return ProcessDefenseFailure(attackInfo);
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
        /// ブロッキングが可能かどうか
        /// </summary>
        /// <returns>ブロッキング可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanBlock()
        {
            return stateSystem.CurrentActionMode == ActionMode.Melee &&
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
            return attackType == AttackType.WeakRanged ||
                   attackType == AttackType.StrongRanged ||
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
            if (IsInBlockWindow && (Time.time - blockWindowStartTime) > 0.2f)
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
            if (HasGuardEnergyBonus && Time.time >= guardEnergyBonusEndTime)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugForceGuard() => StartGuard(AttackDirection.Up);

        [System.ComponentModel.Category("SRDebugger - 防御")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DebugForceBlock() => AttemptBlock(AttackDirection.Up);

        #endregion
    }
}
