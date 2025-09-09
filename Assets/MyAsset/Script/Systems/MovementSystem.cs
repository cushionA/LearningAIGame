using Sirenix.OdinInspector;
using System;
using System.Runtime.CompilerServices;
using UniRx;
using UnityEngine;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 移動データの構造体
    /// </summary>
    [System.Serializable]
    public struct MovementData
    {
        public Vector3 velocity;
        public bool isGrounded;
        public bool isBoosting;
        public bool isAirborne;
        public bool isInAerialFloat;
        public float speed;
        public float airTime;
        public ActionState movementState;
    }

    /// <summary>
    /// 移動システム - 歩行、ジャンプ、ブースト、回避、空中制御、踏み込みなどの移動を管理
    /// For Honorライクな方向システムとの連携を含む
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MovementSystem : BaseSystem<MovementData>
    {
        // コンポーネント
        private Rigidbody rigidBody;
        private StateSystem stateSystem;
        private EnergySystem energySystem;
        private AttackSystem attackSystem;
        private DirectionSystem directionSystem;
        private PositionCache positionCache;

        // 移動状態
        private MovementData currentMovementData;

        [Title("現在の状態")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("地面に接触しているかどうか")]
        public bool IsGrounded { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = true;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在ブースト中かどうか")]
        public bool IsBoosting { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("空中にいるかどうか")]
        public bool IsAirborne { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("空中滞空中かどうか")]
        public bool IsInAerialFloat { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の移動速度")]
        public float CurrentSpeed { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 0f;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("空中時間の累計")]
        public float TotalAirTime { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 0f;

        // 内部状態
        private Vector3 currentMoveDirection = Vector3.zero;
        private bool isChargingJump = false;
        private float jumpChargeStartTime = 0f;
        private float lastDodgeTime = 0f;
        private bool canDoubleDodge = false;

        // 空中制御
        private bool isChargingInAir = false;
        private float airChargeStartTime = 0f;
        private float aerialFloatTimer = 0f;
        private bool hasUsedDoubleJump = false;

        // 踏み込み制御
        private bool isLunging = false;
        private Vector3 lungeDirection;
        private float lungeSpeed;
        private float lungeDistance;
        private float lungeTravelDistance;

        // 地面判定
        [Title("地面判定設定")]
        [PropertyTooltip("地面判定のレイヤーマスク")]
        [SerializeField] private LayerMask groundLayerMask = 1;

        [PropertyTooltip("地面判定の距離")]
        [Range(0.1f, 2f)]
        [SerializeField] private float groundCheckDistance = 1.1f;

        /// <summary>
        /// 初期化処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
        }

        protected override void OnInitialized()
        {
            // 他のシステムの参照取得
            stateSystem = GetComponent<StateSystem>();
            energySystem = GetComponent<EnergySystem>();
            attackSystem = GetComponent<AttackSystem>();
            directionSystem = GetComponent<DirectionSystem>();
            positionCache = GetComponent<PositionCache>();

            if ( Settings?.movement == null )
            {
                DebugLogError("MovementSettingsが設定されていません");
                return;
            }

            // 移動データの設定
            UpdateMovementData();
        }

        protected override void SetupObservables()
        {
            // フレーム毎の移動データ更新をObservableで通知
            UniRx.Observable.EveryFixedUpdate()
                .Subscribe(_ => UpdateAndNotifyMovementData())
                .AddTo(disposables);

            // 空中時間の更新
            UniRx.Observable.EveryUpdate()
                .Subscribe(_ => UpdateAirTime())
                .AddTo(disposables);

            // 空中滞空の更新
            UniRx.Observable.EveryUpdate()
                .Subscribe(_ => UpdateAerialFloat())
                .AddTo(disposables);
        }

        private void UpdateAndNotifyMovementData()
        {
            UpdateMovementData();
            NotifyObservers(currentMovementData);
        }

        private void UpdateMovementData()
        {
            currentMovementData = new MovementData
            {
                velocity = rigidBody != null ? rigidBody.linearVelocity : Vector3.zero,
                isGrounded = IsGrounded,
                isBoosting = IsBoosting,
                isAirborne = IsAirborne,
                isInAerialFloat = IsInAerialFloat,
                speed = CurrentSpeed,
                airTime = TotalAirTime,
                movementState = DetermineMovementState()
            };
        }

        private ActionState DetermineMovementState()
        {
            if ( isLunging )
                return ActionState.Attacking; // 踏み込み中

            if ( !IsGrounded )
            {
                if ( IsInAerialFloat )
                    return ActionState.AirCharge;
                return rigidBody.linearVelocity.y > 0 ? ActionState.Jumping : ActionState.Falling;
            }

            if ( IsBoosting )
                return ActionState.Boosting;

            if ( rigidBody.linearVelocity.magnitude > 0.1f )
                return ActionState.Walking;

            return ActionState.Idle;
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            UpdateGroundCheck();
            UpdateMovementState();
            UpdateCurrentSpeed();
            UpdateAirborneState();
        }

        /// <summary>
        /// 物理更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FixedUpdate()
        {
            ProcessMovement();
            ProcessLunge();
        }

        #region Public Movement Methods

        /// <summary>
        /// 移動を実行
        /// </summary>
        /// <param name="direction">移動方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Move(Vector3 direction)
        {
            currentMoveDirection = direction.normalized;

            if ( currentMoveDirection.magnitude > 0.1f )
            {
                stateSystem.ReportActionStateChange(ActionState.Walking);
            }
            else
            {
                stateSystem.ReportActionStateChange(ActionState.Idle);
            }
        }

        /// <summary>
        /// ジャンプを実行
        /// </summary>
        /// <param name="charged">チャージジャンプかどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Jump(bool charged = false)
        {
            if ( !CanJump() )
                return;

            float jumpForce = charged ? Settings.movement.chargedJumpForce : Settings.movement.jumpForce;

            if ( !IsGrounded && !hasUsedDoubleJump )
            {
                // 二段ジャンプ
                if ( energySystem.UseEnergy(Settings.movement.airJumpEnergyCost) )
                {
                    rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, jumpForce, rigidBody.linearVelocity.z);
                    hasUsedDoubleJump = true;
                }
            }
            else if ( IsGrounded )
            {
                // 通常ジャンプ
                rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, jumpForce, rigidBody.linearVelocity.z);
                hasUsedDoubleJump = false; // 地上ジャンプ時にリセット
            }

            stateSystem.ReportActionStateChange(ActionState.Jumping);
        }

        /// <summary>
        /// ジャンプチャージを開始
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartJumpCharge()
        {
            if ( IsGrounded && !isChargingJump )
            {
                isChargingJump = true;
                jumpChargeStartTime = Time.time;
            }
        }

        /// <summary>
        /// ジャンプチャージを終了してジャンプ実行
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseJumpCharge()
        {
            if ( isChargingJump )
            {
                float chargeTime = Time.time - jumpChargeStartTime;
                bool isCharged = chargeTime >= Settings.movement.chargeTime;

                isChargingJump = false;
                Jump(isCharged);
            }
        }

        /// <summary>
        /// ブーストを実行
        /// </summary>
        /// <param name="direction">ブースト方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Boost(Vector3 direction)
        {
            if ( !CanBoost() )
                return;

            IsBoosting = true;
            currentMoveDirection = direction.normalized;

            // ブースト方向に基づく攻撃方向をDirectionSystemに設定
            if ( directionSystem != null )
            {
                directionSystem.DeriveDirectionFromMovement(direction, 0.2f);
            }

            stateSystem.ReportActionStateChange(ActionState.Boosting);
        }

        /// <summary>
        /// ブースト停止
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopBoost()
        {
            IsBoosting = false;
            stateSystem.ReportActionStateChange(ActionState.Idle);
        }

        /// <summary>
        /// 回避を実行（For Honorライクな方向システム連携）
        /// </summary>
        /// <param name="direction">回避方向（空でバックステップ）</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dodge(Vector3 direction)
        {
            if ( !CanDodge() )
                return;

            // 二段回避の判定
            bool isDoubleDodge = canDoubleDodge && (Time.time - lastDodgeTime) <= 0.2f;

            float energyCost = isDoubleDodge ? Settings.movement.doubleDodgeEnergyCost : Settings.movement.dodgeEnergyCost;
            float dodgeDistance = isDoubleDodge ? Settings.movement.dodgeDistance * 1.5f : Settings.movement.dodgeDistance;

            if ( !energySystem.UseEnergy(energyCost) )
                return;

            // 回避方向の決定
            Vector3 dodgeDirection = direction.magnitude > 0.1f ? direction.normalized : -transform.forward;

            // DirectionSystemに回避方向に基づく攻撃方向を設定
            if ( directionSystem != null )
            {
                directionSystem.DeriveDirectionFromMovement(dodgeDirection, 0.5f);
            }

            // 回避実行
            rigidBody.linearVelocity = dodgeDirection * (dodgeDistance / 0.3f); // 0.3秒で移動完了

            // 無敵フレーム設定
            stateSystem.HealthData.isInvincible = true;
            stateSystem.HealthData.invincibilityTimer = 0.2f;

            stateSystem.ReportActionStateChange(ActionState.Dodging);

            // AttackSystemに回避実行を通知（回避攻撃のため）
            if ( attackSystem != null )
            {
                attackSystem.OnDodgeExecuted(dodgeDirection);
            }

            // 回避の状態設定
            UniRx.Observable.Timer(TimeSpan.FromSeconds(0.3f))
                .Subscribe(_ => OnDodgeComplete())
                .AddTo(disposables);

            lastDodgeTime = Time.time;
            canDoubleDodge = !isDoubleDodge; // 二段後はリセット
        }

        /// <summary>
        /// クイックターンを実行
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QuickTurn()
        {
            if ( energySystem.UseEnergy(5f) ) // 軽微なエネルギー消費
            {
                transform.Rotate(0, 180f, 0);

                // クイックターン時にDirectionSystemの方向ロックを一時的に解除
                if ( directionSystem != null )
                {
                    // ターン後の新しい方向を設定
                    directionSystem.ForceDirection(directionSystem.CurrentDirection, 0.1f);
                }
            }
        }

        /// <summary>
        /// 踏み込みを実行
        /// </summary>
        /// <param name="direction">踏み込み方向</param>
        /// <param name="distance">踏み込み距離</param>
        /// <param name="speed">踏み込み速度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteLunge(Vector3 direction, float distance, float speed)
        {
            if ( isLunging )
                return; // 既に踏み込み中

            lungeDirection = direction.normalized;
            lungeDistance = distance;
            lungeSpeed = speed;
            lungeTravelDistance = 0f;
            isLunging = true;

            // 踏み込み中は他の移動を一時停止
            currentMoveDirection = Vector3.zero;

            // DirectionSystemに踏み込み方向を設定
            if ( directionSystem != null )
            {
                directionSystem.DeriveDirectionFromMovement(direction, 0.3f);
            }

            stateSystem.ReportActionStateChange(ActionState.Attacking);
        }

        /// <summary>
        /// 空中滞空を開始
        /// </summary>
        /// <param name="floatTime">滞空時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartAerialFloat(float floatTime)
        {
            if ( IsAirborne )
            {
                IsInAerialFloat = true;
                aerialFloatTimer = floatTime;

                // 重力を一時的に無効化
                rigidBody.useGravity = false;
                rigidBody.linearVelocity = new Vector3(rigidBody.linearVelocity.x, 0, rigidBody.linearVelocity.z);

                stateSystem.AnalysisData.isInAerialCombo = true;
                stateSystem.AnalysisData.aerialFloatTimeRemaining = floatTime;
            }
        }

        /// <summary>
        /// 空中滞空を終了
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndAerialFloat()
        {
            if ( IsInAerialFloat )
            {
                IsInAerialFloat = false;
                aerialFloatTimer = 0f;
                rigidBody.useGravity = true;

                stateSystem.AnalysisData.isInAerialCombo = false;
                stateSystem.AnalysisData.aerialFloatTimeRemaining = 0f;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 地面判定の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateGroundCheck()
        {
            bool wasGrounded = IsGrounded;
            Vector3 position = positionCache != null ? positionCache.Position : transform.position;
            IsGrounded = Physics.Raycast(position, Vector3.down, groundCheckDistance, groundLayerMask);

            // 着地時の処理
            if ( !wasGrounded && IsGrounded )
            {
                OnLanded();
            }

            // 落下状態の判定
            if ( !IsGrounded && rigidBody.linearVelocity.y < -0.1f &&
                stateSystem.CurrentActionState != ActionState.Dodging &&
                !IsInAerialFloat )
            {
                stateSystem.ReportActionStateChange(ActionState.Falling);
            }
            else if ( IsGrounded && stateSystem.CurrentActionState == ActionState.Falling )
            {
                stateSystem.ReportActionStateChange(ActionState.Idle);
            }
        }

        /// <summary>
        /// 空中状態の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAirborneState()
        {
            IsAirborne = !IsGrounded;
            stateSystem.AnalysisData.isAirborne = IsAirborne;
        }

        /// <summary>
        /// 空中時間の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAirTime()
        {
            if ( IsAirborne )
            {
                TotalAirTime += Time.deltaTime;

                // 最大空中時間チェック
                if ( TotalAirTime > Settings.movement.maxAirTime )
                {
                    // 強制的に降下開始
                    if ( IsInAerialFloat )
                    {
                        EndAerialFloat();
                    }
                }
            }
            else
            {
                TotalAirTime = 0f;
            }

            stateSystem.AnalysisData.totalAirTime = TotalAirTime;
        }

        /// <summary>
        /// 空中滞空の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAerialFloat()
        {
            if ( IsInAerialFloat )
            {
                aerialFloatTimer -= Time.deltaTime;
                stateSystem.AnalysisData.aerialFloatTimeRemaining = Mathf.Max(0f, aerialFloatTimer);

                if ( aerialFloatTimer <= 0f )
                {
                    EndAerialFloat();
                }
            }
        }

        /// <summary>
        /// 移動状態の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateMovementState()
        {
            // ブーストのエネルギー消費
            if ( IsBoosting )
            {
                if ( !energySystem.UseEnergy(Settings.movement.boostEnergyConsumption * Time.deltaTime) )
                {
                    StopBoost();
                }
            }
        }

        /// <summary>
        /// 現在速度の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateCurrentSpeed()
        {
            CurrentSpeed = new Vector3(rigidBody.linearVelocity.x, 0, rigidBody.linearVelocity.z).magnitude;
        }

        /// <summary>
        /// 移動処理の実行
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessMovement()
        {
            if ( isLunging || currentMoveDirection.magnitude < 0.1f )
                return;

            float moveSpeed = IsBoosting ? Settings.movement.boostSpeed : Settings.movement.walkSpeed;

            // 空中での移動速度調整
            if ( IsAirborne && !IsInAerialFloat )
            {
                moveSpeed *= Settings.movement.airMobilityMultiplier;
            }

            Vector3 targetVelocity = currentMoveDirection * moveSpeed;

            // Y軸の速度は維持（重力の影響を維持）
            if ( !IsInAerialFloat )
            {
                targetVelocity.y = rigidBody.linearVelocity.y;
            }

            rigidBody.linearVelocity = targetVelocity;
        }

        /// <summary>
        /// 踏み込み処理の実行
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessLunge()
        {
            if ( !isLunging )
                return;

            float deltaDistance = lungeSpeed * Time.fixedDeltaTime;
            lungeTravelDistance += deltaDistance;

            // 踏み込み移動の実行
            Vector3 lungeVelocity = lungeDirection * lungeSpeed;

            // 空中踏み込みの場合、Y軸速度も制御
            if ( IsAirborne )
            {
                rigidBody.linearVelocity = lungeVelocity;
            }
            else
            {
                lungeVelocity.y = rigidBody.linearVelocity.y;
                rigidBody.linearVelocity = lungeVelocity;
            }

            // 踏み込み完了チェック
            if ( lungeTravelDistance >= lungeDistance )
            {
                EndLunge();
            }
        }

        /// <summary>
        /// 踏み込み終了
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EndLunge()
        {
            isLunging = false;
            lungeTravelDistance = 0f;

            // 通常の移動状態に戻る
            stateSystem.ReportActionStateChange(ActionState.Idle);
        }

        /// <summary>
        /// 着地時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnLanded()
        {
            // 空中関連の状態をリセット
            hasUsedDoubleJump = false;

            if ( IsInAerialFloat )
            {
                EndAerialFloat();
            }

            TotalAirTime = 0f;
        }

        /// <summary>
        /// 回避完了時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDodgeComplete()
        {
            stateSystem.ReportActionStateChange(ActionState.Idle);

            // 回避の脆弱時間設定
            UniRx.Observable.Timer(TimeSpan.FromSeconds(Settings.movement.postDodgeVulnerabilityTime))
                .Subscribe(_ => canDoubleDodge = true)
                .AddTo(disposables);
        }

        /// <summary>
        /// ジャンプ可能かどうか
        /// </summary>
        /// <returns>ジャンプ可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanJump()
        {
            if ( stateSystem.CurrentActionState == ActionState.Dodging ||
                stateSystem.CurrentActionState == ActionState.Stunned )
                return false;

            // 地上または二段ジャンプ可
            return IsGrounded || (!hasUsedDoubleJump && energySystem.CanUseEnergy(Settings.movement.airJumpEnergyCost));
        }

        /// <summary>
        /// ブースト可能かどうか
        /// </summary>
        /// <returns>ブースト可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanBoost()
        {
            return !IsBoosting &&
                   !isLunging &&
                   stateSystem.CurrentActionState != ActionState.Dodging &&
                   stateSystem.CurrentActionState != ActionState.Stunned &&
                   energySystem.CanUseEnergy(Settings.movement.boostEnergyConsumption * 0.1f);
        }

        /// <summary>
        /// 回避可能かどうか
        /// </summary>
        /// <returns>回避可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanDodge()
        {
            return !isLunging &&
                   stateSystem.CurrentActionState != ActionState.Dodging &&
                   stateSystem.CurrentActionState != ActionState.Stunned &&
                   energySystem.CanUseEnergy(Settings.movement.dodgeEnergyCost);
        }

        #endregion

        #region Debug Methods

        [Title("デバッグ機能")]
        [Button("通常ジャンプ", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugJump()
        {
            Jump(false);
        }

        [Button("チャージジャンプ", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugChargedJump()
        {
            Jump(true);
        }

        [Button("前回避", ButtonSizes.Medium)]
        [GUIColor(1f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugDodgeForward()
        {
            Dodge(transform.forward);
        }

        [Button("踏み込みテスト", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugLunge()
        {
            ExecuteLunge(transform.forward, 3f, 10f);
        }

        /// <summary>
        /// Gizmo描画
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDrawGizmos()
        {
            // 地面判定の可視化
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 position = positionCache != null ? positionCache.Position : transform.position;
            Gizmos.DrawRay(position, Vector3.down * groundCheckDistance);

            // 移動方向の可視化
            if ( currentMoveDirection.magnitude > 0.1f )
            {
                Gizmos.color = IsBoosting ? Color.blue : Color.yellow;
                Gizmos.DrawRay(position, currentMoveDirection * 2f);
            }

            // 踏み込み方向の可視化
            if ( isLunging )
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(position, lungeDirection * lungeDistance);
            }

            // 空中状態の可視化
            if ( IsAirborne )
            {
                Gizmos.color = IsInAerialFloat ? Color.cyan : Color.magenta;
                Gizmos.DrawWireSphere(position, 0.5f);
            }
        }

        #endregion
    }
}