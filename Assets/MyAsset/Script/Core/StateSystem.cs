using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;
using UniRx;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// キャラクターの全状態を統合管理するシステム
    /// 各システムからの報告を受け取り、一元的に状態を管理する
    /// </summary>
    public class StateSystem : MonoBehaviour
    {
        [Title("現在の状態")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の行動モード")]
        public ActionMode CurrentActionMode { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = ActionMode.Melee;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の行動状態")]
        public ActionState CurrentActionState { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = ActionState.Idle;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の攻撃・防御方向（DirectionSystemから取得）")]
        public AttackDirection CurrentDirection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var directionSystem = GetComponent<DirectionSystem>();
                return directionSystem != null ? directionSystem.CurrentDirection : AttackDirection.Up;
            }
        }

        [Title("データ管理")]
        [ShowInInspector]
        [PropertyTooltip("外部解析用データ")]
        public AnalysisData AnalysisData { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = new AnalysisData();

        [ShowInInspector]
        [PropertyTooltip("ヘルス関連データ")]
        public HealthData HealthData { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = new HealthData();

        [Title("エネルギー管理")]
        [ShowInInspector, ReadOnly]
        [ProgressBar(0, 1)]
        [PropertyTooltip("現在のエネルギー割合")]
        public float EnergyPercentage { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 1f;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("エネルギー回復が停止中かどうか")]
        public bool IsEnergyRecoveryPaused { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        // 内部管理データ
        [Title("内部状態")]
        [ShowInInspector, ReadOnly]
        private Dictionary<ActionType, float> actionStartTimes = new Dictionary<ActionType, float>();

        [ShowInInspector, ReadOnly]
        private float energyRecoveryPauseEndTime = 0f;

        [ShowInInspector, ReadOnly]
        private ActionMode previousActionMode = ActionMode.Melee;

        // イベント通知用のSubject（UniRx）
        private readonly Subject<ActionMode> onActionModeChanged = new Subject<ActionMode>();
        private readonly Subject<ActionState> onActionStateChanged = new Subject<ActionState>();
        private readonly Subject<float> onHealthChanged = new Subject<float>();
        private readonly Subject<float> onEnergyChanged = new Subject<float>();
        private readonly Subject<AttackDirection> onDirectionChanged = new Subject<AttackDirection>();

        // 公開イベント
        public IObservable<ActionMode> OnActionModeChanged => this.onActionModeChanged.AsObservable();
        public IObservable<ActionState> OnActionStateChanged => this.onActionStateChanged.AsObservable();
        public IObservable<float> OnHealthChanged => this.onHealthChanged.AsObservable();
        public IObservable<float> OnEnergyChanged => this.onEnergyChanged.AsObservable();
        public IObservable<AttackDirection> OnDirectionChanged => this.onDirectionChanged.AsObservable();

        /// <summary>
        /// 初期化処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Awake()
        {
            ResetAllStates();
        }

        /// <summary>
        /// 状態の更新処理（毎フレーム）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateStates()
        {
            UpdateTimingData();
            UpdateHealthRecovery();
            UpdateCooldowns();
            UpdateAnalysisData();
        }

        #region 報告受付メソッド

        /// <summary>
        /// 行動モードの変更を報告
        /// </summary>
        /// <param name="newMode">新しい行動モード</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReportActionModeChange(ActionMode newMode)
        {
            if ( CurrentActionMode != newMode )
            {
                this.previousActionMode = CurrentActionMode;
                CurrentActionMode = newMode;
                this.onActionModeChanged.OnNext(newMode);
            }
        }

        /// <summary>
        /// 行動状態の変更を報告
        /// </summary>
        /// <param name="newState">新しい行動状態</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReportActionStateChange(ActionState newState)
        {
            if ( CurrentActionState != newState )
            {
                CurrentActionState = newState;
                this.onActionStateChanged.OnNext(newState);
                RecordActionStart(GetActionTypeFromState(newState));
            }
        }

        /// <summary>
        /// 攻撃・防御方向の変更を報告（DirectionSystem経由で呼び出される）
        /// </summary>
        /// <param name="newDirection">新しい方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReportDirectionChange(AttackDirection newDirection)
        {
            // DirectionSystemが管理しているため、イベントのみ発火
            this.onDirectionChanged.OnNext(newDirection);
        }

        /// <summary>
        /// スキルクールダウンの報告
        /// </summary>
        /// <param name="skillIndex">スキルインデックス</param>
        /// <param name="cooldownTime">クールダウン時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReportSkillCooldown(int skillIndex, float cooldownTime)
        {
            if ( skillIndex >= 0 && skillIndex < AnalysisData.skillCooldowns.Length )
            {
                AnalysisData.skillCooldowns[skillIndex] = cooldownTime;
                UpdateSkillAvailability();
            }
        }

        /// <summary>
        /// マニューバクールダウンの報告
        /// </summary>
        /// <param name="maneuverIndex">マニューバインデックス</param>
        /// <param name="cooldownTime">クールダウン時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReportManeuverCooldown(int maneuverIndex, float cooldownTime)
        {
            if ( maneuverIndex >= 0 && maneuverIndex < AnalysisData.maneuverCooldowns.Length )
            {
                AnalysisData.maneuverCooldowns[maneuverIndex] = cooldownTime;
                UpdateManeuverAvailability();
            }
        }

        /// <summary>
        /// ダメージ情報の報告
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <param name="causesStun">スタンを引き起こすかどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReportDamage(float damage, bool causesStun)
        {
            if ( causesStun )
            {
                HealthData.isStunned = true;
                HealthData.stunGauge = 100f;
            }

            this.onHealthChanged.OnNext(damage);
        }

        /// <summary>
        /// エネルギー情報の報告
        /// </summary>
        /// <param name="energyPercentage">エネルギー割合</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReportEnergyChange(float energyPercentage)
        {
            EnergyPercentage = Mathf.Clamp01(energyPercentage);
            this.onEnergyChanged.OnNext(EnergyPercentage);

            // エネルギー切れ時のモード変更
            if ( EnergyPercentage <= 0f && CurrentActionMode != ActionMode.EnergyBarrier )
            {
                ReportActionModeChange(ActionMode.EnergyBarrier);
            }
            else if ( EnergyPercentage > 0.1f && CurrentActionMode == ActionMode.EnergyBarrier )
            {
                ReportActionModeChange(this.previousActionMode);
            }
        }

        /// <summary>
        /// エネルギー回復停止の報告
        /// </summary>
        /// <param name="pauseDuration">停止時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReportEnergyRecoveryPause(float pauseDuration)
        {
            this.energyRecoveryPauseEndTime = Time.time + pauseDuration;
            IsEnergyRecoveryPaused = true;
        }

        /// <summary>
        /// リロード状態の報告
        /// </summary>
        /// <param name="isReloading">リロード中かどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReportReloadState(bool isReloading)
        {
            AnalysisData.isReloading = isReloading;
        }

        /// <summary>
        /// 射撃精度の報告
        /// </summary>
        /// <param name="accuracy">射撃精度</param>
        /// <param name="aimDirection">狙い方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReportAimingData(float accuracy, Vector3 aimDirection)
        {
            AnalysisData.aimingAccuracy = Mathf.Clamp01(accuracy);
            AnalysisData.aimDirection = aimDirection.normalized;
        }

        #endregion

        #region 状態判定メソッド

        /// <summary>
        /// 指定したアクションが実行可能かどうかを判定
        /// </summary>
        /// <param name="actionType">アクションタイプ</param>
        /// <returns>実行可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExecuteAction(ActionType actionType)
        {
            // 基本的な実行不可条件
            if ( HealthData.isStunned || HealthData.isDead )
                return false;

            // エネルギー系アクションの判定
            if ( IsEnergyAction(actionType) && EnergyPercentage <= 0f )
                return false;

            // 状態別の実行可能性判定
            return CurrentActionState switch
            {
                ActionState.Attacking => actionType == ActionType.ModeSwitch, // 攻撃中はモード切替のみ
                ActionState.Dodging => false, // 回避中は何もできない
                ActionState.UsingManeuver => false, // マニューバ中は何もできない
                ActionState.Stunned => false, // スタン中は何もできない
                _ => true
            };
        }

        /// <summary>
        /// 方向の変更が可能かどうかを判定（DirectionSystemと連携）
        /// </summary>
        /// <returns>変更可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanChangeDirection()
        {
            var directionSystem = GetComponent<DirectionSystem>();
            if ( directionSystem == null )
                return false;

            return directionSystem.CanChangeDirection &&
                   CurrentActionState != ActionState.Attacking &&
                   CurrentActionState != ActionState.UsingManeuver &&
                   CurrentActionState != ActionState.Stunned;
        }

        /// <summary>
        /// 方向の変更を試行（DirectionSystem経由）
        /// </summary>
        /// <param name="newDirection">新しい方向</param>
        /// <returns>変更が成功したかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TrySetDirection(AttackDirection newDirection)
        {
            var directionSystem = GetComponent<DirectionSystem>();
            if ( directionSystem == null )
                return false;

            if ( CanChangeDirection() )
            {
                directionSystem.ForceDirection(newDirection, 0.1f);
                return true;
            }
            return false;
        }

        /// <summary>
        /// カメラ移動が有効かどうかを取得
        /// </summary>
        /// <returns>カメラ移動可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetCameraMovementEnabled()
        {
            return CurrentActionState != ActionState.Stunned &&
                   CurrentActionState != ActionState.UsingManeuver;
        }

        #endregion

        #region 内部更新メソッド

        /// <summary>
        /// タイミングデータの更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTimingData()
        {
            AnalysisData.timeSinceLastAction += Time.deltaTime;
        }

        /// <summary>
        /// ヘルス関連の回復処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateHealthRecovery()
        {
            // スタン回復
            if ( HealthData.isStunned )
            {
                HealthData.stunGauge -= HealthData.stunRecoveryRate * Time.deltaTime;
                if ( HealthData.stunGauge <= 0f )
                {
                    HealthData.isStunned = false;
                    HealthData.stunGauge = 0f;
                }
            }

            // 無敵時間管理
            if ( HealthData.isInvincible )
            {
                HealthData.invincibilityTimer -= Time.deltaTime;
                if ( HealthData.invincibilityTimer <= 0f )
                {
                    HealthData.isInvincible = false;
                    HealthData.invincibilityTimer = 0f;
                }
            }

            // エネルギー回復停止の管理
            if ( IsEnergyRecoveryPaused && Time.time >= this.energyRecoveryPauseEndTime )
            {
                IsEnergyRecoveryPaused = false;
            }
        }

        /// <summary>
        /// クールダウンの更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateCooldowns()
        {
            // スキルクールダウン更新
            for ( int i = 0; i < AnalysisData.skillCooldowns.Length; i++ )
            {
                if ( AnalysisData.skillCooldowns[i] > 0f )
                {
                    AnalysisData.skillCooldowns[i] -= Time.deltaTime;
                    if ( AnalysisData.skillCooldowns[i] < 0f )
                    {
                        AnalysisData.skillCooldowns[i] = 0f;
                    }
                }
            }

            // マニューバクールダウン更新
            for ( int i = 0; i < AnalysisData.maneuverCooldowns.Length; i++ )
            {
                if ( AnalysisData.maneuverCooldowns[i] > 0f )
                {
                    AnalysisData.maneuverCooldowns[i] -= Time.deltaTime;
                    if ( AnalysisData.maneuverCooldowns[i] < 0f )
                    {
                        AnalysisData.maneuverCooldowns[i] = 0f;
                    }
                }
            }

            UpdateSkillAvailability();
            UpdateManeuverAvailability();
        }

        /// <summary>
        /// 解析データの更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAnalysisData()
        {
            // Rigidbodyから移動データを取得
            var rigidbody = GetComponent<Rigidbody>();
            if ( rigidbody != null )
            {
                AnalysisData.currentVelocity = rigidbody.linearVelocity;
                AnalysisData.currentSpeed = rigidbody.linearVelocity.magnitude;
            }
        }

        /// <summary>
        /// スキル使用可否の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateSkillAvailability()
        {
            AnalysisData.canUseSkills = EnergyPercentage > 0.1f && !HealthData.isStunned;
            for ( int i = 0; i < AnalysisData.skillCooldowns.Length; i++ )
            {
                if ( AnalysisData.skillCooldowns[i] > 0f )
                {
                    AnalysisData.canUseSkills = false;
                    break;
                }
            }
        }

        /// <summary>
        /// マニューバ使用可否の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateManeuverAvailability()
        {
            AnalysisData.canUseManeuvers = EnergyPercentage > 0.2f && !HealthData.isStunned;
            for ( int i = 0; i < AnalysisData.maneuverCooldowns.Length; i++ )
            {
                if ( AnalysisData.maneuverCooldowns[i] > 0f )
                {
                    AnalysisData.canUseManeuvers = false;
                    break;
                }
            }
        }

        /// <summary>
        /// アクションの開始時刻を記録
        /// </summary>
        /// <param name="actionType">アクションタイプ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordActionStart(ActionType actionType)
        {
            this.actionStartTimes[actionType] = Time.time;
            AnalysisData.timeSinceLastAction = 0f;
        }

        /// <summary>
        /// アクション状態からアクションタイプを取得
        /// </summary>
        /// <param name="state">アクション状態</param>
        /// <returns>アクションタイプ</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ActionType GetActionTypeFromState(ActionState state)
        {
            return state switch
            {
                ActionState.Walking => ActionType.Walk,
                ActionState.Jumping => ActionType.Jump,
                ActionState.Boosting => ActionType.Boost,
                ActionState.Dodging => ActionType.Dodge,
                ActionState.Attacking => ActionType.WeakAttack, // デフォルト
                ActionState.Guarding => ActionType.Guard,
                ActionState.UsingManeuver => ActionType.Maneuver,
                _ => ActionType.Walk
            };
        }

        /// <summary>
        /// エネルギーを消費するアクションかどうかを判定
        /// </summary>
        /// <param name="actionType">アクションタイプ</param>
        /// <returns>エネルギー消費アクションかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsEnergyAction(ActionType actionType)
        {
            return actionType switch
            {
                ActionType.Boost or ActionType.Dodge or ActionType.SkillAttack or
                ActionType.ShootSkill or ActionType.Maneuver => true,
                _ => false
            };
        }

        #endregion

        #region 公開ユーティリティメソッド

        /// <summary>
        /// アクションの経過時間を取得
        /// </summary>
        /// <param name="actionType">アクションタイプ</param>
        /// <returns>経過時間</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetActionElapsedTime(ActionType actionType)
        {
            if ( this.actionStartTimes.TryGetValue(actionType, out float startTime) )
            {
                return Time.time - startTime;
            }
            return 0f;
        }

        /// <summary>
        /// ジャスト判定ウィンドウ内かどうかを確認
        /// </summary>
        /// <param name="actionType">アクションタイプ</param>
        /// <param name="windowTime">判定ウィンドウ時間</param>
        /// <returns>ウィンドウ内かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWithinJustWindow(ActionType actionType, float windowTime)
        {
            var elapsedTime = GetActionElapsedTime(actionType);
            return elapsedTime <= windowTime;
        }

        /// <summary>
        /// スタンを強制的に発生させる（デバッグ用）
        /// </summary>
        /// <param name="duration">スタン継続時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForceStun(float duration)
        {
            HealthData.isStunned = true;
            HealthData.stunGauge = 100f;
            HealthData.stunRecoveryRate = 100f / duration; // 指定時間で回復
        }

        /// <summary>
        /// 全ての状態をリセット
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetAllStates()
        {
            CurrentActionMode = ActionMode.Melee;
            CurrentActionState = ActionState.Idle;
            EnergyPercentage = 1f;
            IsEnergyRecoveryPaused = false;

            // DirectionSystemをリセット
            var directionSystem = GetComponent<DirectionSystem>();
            if ( directionSystem != null )
            {
                directionSystem.ForceDirection(AttackDirection.Up, 0f);
            }

            AnalysisData.Reset();
            HealthData.Reset();

            this.actionStartTimes.Clear();
            this.energyRecoveryPauseEndTime = 0f;
            this.previousActionMode = ActionMode.Melee;
        }

        #endregion

        #region 開発者ツール

        [Title("開発者ツール")]
        [Button("状態リセット", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugResetStates()
        {
            ResetAllStates();
            Debug.Log("全ての状態をリセットしました");
        }

        [Button("状態情報出力", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugLogStates()
        {
            var info = $"=== 状態システム情報 ===\n" +
                      $"行動モード: {CurrentActionMode}\n" +
                      $"行動状態: {CurrentActionState}\n" +
                      $"方向: {CurrentDirection}\n" +
                      $"エネルギー: {EnergyPercentage:P1}\n" +
                      $"スタン: {HealthData.isStunned}\n" +
                      $"無敵: {HealthData.isInvincible}\n" +
                      $"スキル使用可: {AnalysisData.canUseSkills}\n" +
                      $"マニューバ使用可: {AnalysisData.canUseManeuvers}";

            Debug.Log(info);
        }

        #endregion

        /// <summary>
        /// 破棄時の処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDestroy()
        {
            this.onActionModeChanged?.Dispose();
            this.onActionStateChanged?.Dispose();
            this.onHealthChanged?.Dispose();
            this.onEnergyChanged?.Dispose();
            this.onDirectionChanged?.Dispose();
        }
    }
}
