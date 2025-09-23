using LearningAIGame.CombatSystem.Core.StateReportData;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UniRx;
using UnityEngine;
using NaughtyAttributes;

namespace LearningAIGame.CombatSystem.Core
{
    /// <summary>
    /// BaseControllerに送信する通知の種類
    /// 各通知は「BaseControllerが何らかのアクションを取る必要がある状況」を表す
    /// </summary>
    public enum StateNotificationTrigger
    {
        // エネルギー関連の重要な変化
        EnergyDepleted,              // エネルギー切れ状態に突入
        EnergyRecovered,             // エネルギー切れから完全回復
        EnergyRecoveryPaused,        // エネルギー回復が停止された

        // 体力・生存関連の重要な変化  
        HealthCritical,              // 体力が危険レベルに到達
        StunOccurred,                // スタン状態に突入
        StunRecovered,               // スタン状態から回復
        InvincibilityStarted,        // 無敵状態開始
        InvincibilityEnded,          // 無敵状態終了

        // 戦闘状況の変化
        GuardBroken,                 // ガードが破られた
        BlockingSucceeded,           // ブロッキング成功
        BlockingFailed,              // ブロッキング失敗
        ComboInterrupted,            // コンボが中断された

        // アクション可能性の変化
        SkillBecameAvailable,        // スキルが使用可能になった
        ManeuverBecameAvailable,     // マニューバが使用可能になった
        ActionRestricted,            // アクション実行が制限された
        ActionUnrestricted,          // アクション制限が解除された

        // モード・状態の重要な変化
        CombatModeChanged,           // 戦闘モードが変更された
        DirectionChangeBlocked,      // 方向変更が阻止された
        MovementRestricted,          // 移動が制限された

        // 射撃システム関連
        AimingMaxAccuracy,           // 射撃精度が最大に到達
        ReloadCompleted,             // リロード完了
        WeaponOverheated,            // 武器オーバーヒート

        // 回避システム関連
        DodgeIntervalReset,          // 回避インターバルがリセットされた
        DodgeBecameUnavailable,      // 回避が使用不可になった
    }

    /// <summary>
    /// BaseControllerへの通知データ構造
    /// Object型を避け、必要最小限のデータで構成
    /// </summary>
    [System.Serializable]
    public struct StateNotification
    {
        public StateNotificationTrigger trigger;    // 通知の種類
        public float severity;                       // 重要度・緊急度 (0.0-1.0)
        public float timestamp;                      // 発生タイムスタンプ

        // 文脈情報（型安全性を保つため個別フィールド）
        public int relatedIndex;                     // 関連インデックス（スキル番号等）
        public float relatedValue;                   // 関連値（エネルギー量、体力等）
        public AttackDirection relatedDirection;     // 関連方向
        public bool relatedFlag;                     // 関連フラグ

        /// <summary>
        /// 基本通知の作成
        /// </summary>
        public StateNotification(StateNotificationTrigger notificationTrigger, float notificationSeverity = 0.5f)
        {
            trigger = notificationTrigger;
            severity = Mathf.Clamp01(notificationSeverity);
            timestamp = Time.time;
            relatedIndex = -1;
            relatedValue = 0f;
            relatedDirection = AttackDirection.Up;
            relatedFlag = false;
        }

        /// <summary>
        /// インデックス付き通知の作成（スキル・マニューバ等）
        /// </summary>
        public StateNotification(StateNotificationTrigger notificationTrigger, int index, float notificationSeverity = 0.5f)
            : this(notificationTrigger, notificationSeverity)
        {
            relatedIndex = index;
        }

        /// <summary>
        /// 値付き通知の作成（エネルギー・体力等）
        /// </summary>
        public StateNotification(StateNotificationTrigger notificationTrigger, float value, float notificationSeverity = 0.5f)
            : this(notificationTrigger, notificationSeverity)
        {
            relatedValue = value;
        }

        /// <summary>
        /// 方向付き通知の作成（攻撃・防御方向等）
        /// </summary>
        public StateNotification(StateNotificationTrigger notificationTrigger, AttackDirection direction, float notificationSeverity = 0.5f)
            : this(notificationTrigger, notificationSeverity)
        {
            relatedDirection = direction;
        }

        /// <summary>
        /// フラグ付き通知の作成（成功・失敗等）
        /// </summary>
        public StateNotification(StateNotificationTrigger notificationTrigger, bool flag, float notificationSeverity = 0.5f)
            : this(notificationTrigger, notificationSeverity)
        {
            relatedFlag = flag;
        }
    }

    /// <summary>
    /// 各システムからの報告データを保持する内部構造体群
    /// StateSystemが受信した報告を一時的に保持し、必要に応じて通知判定を行う
    /// </summary>
    namespace StateReportData
    {
        /// <summary>
        /// 移動システムからの報告データ
        /// </summary>
        [System.Serializable]
        public struct MovementReport
        {
            public bool isMoving;
            public float speed;
            public Vector3 direction;
            public bool isGrounded;
            public float verticalVelocity;
            public bool isBoosting;
            public bool dodgeExecuted;
            public float lastDodgeTime;

            public MovementReport(bool moving, float currentSpeed, Vector3 moveDirection)
            {
                isMoving = moving;
                speed = currentSpeed;
                direction = moveDirection;
                isGrounded = true;
                verticalVelocity = 0f;
                isBoosting = false;
                dodgeExecuted = false;
                lastDodgeTime = 0f;
            }
        }

        /// <summary>
        /// 攻撃システムからの報告データ
        /// </summary>
        [System.Serializable]
        public struct AttackReport
        {
            public bool isAttacking;
            public AttackType attackType;
            public AttackDirection direction;
            public int comboCount;
            public bool isAirAttack;
            public bool attackHit;
            public bool attackBlocked;
            public int skillIndex;
            public float skillCooldown;

            public AttackReport(bool attacking, AttackType type, AttackDirection attackDirection)
            {
                isAttacking = attacking;
                attackType = type;
                direction = attackDirection;
                comboCount = 0;
                isAirAttack = false;
                attackHit = false;
                attackBlocked = false;
                skillIndex = -1;
                skillCooldown = 0f;
            }
        }

        /// <summary>
        /// 防御システムからの報告データ
        /// </summary>
        [System.Serializable]
        public struct DefenseReport
        {
            public bool isGuarding;
            public AttackDirection guardDirection;
            public bool isBlocking;
            public bool blockingSuccess;
            public bool guardBroken;
            public float blockingTimestamp;
            public AttackType blockedAttackType;

            public DefenseReport(bool guarding, AttackDirection direction)
            {
                isGuarding = guarding;
                guardDirection = direction;
                isBlocking = false;
                blockingSuccess = false;
                guardBroken = false;
                blockingTimestamp = 0f;
                blockedAttackType = AttackType.None;
            }
        }

        /// <summary>
        /// エネルギーシステムからの報告データ
        /// </summary>
        [System.Serializable]
        public struct EnergyReport
        {
            public float currentPercentage;
            public float previousPercentage;
            public bool isDepleted;
            public bool wasDepletedLastFrame;
            public bool isRecoveryPaused;
            public float recoveryPauseEndTime;
            public float consumptionAmount;

            public EnergyReport(float percentage)
            {
                currentPercentage = percentage;
                previousPercentage = percentage;
                isDepleted = percentage <= 0f;
                wasDepletedLastFrame = false;
                isRecoveryPaused = false;
                recoveryPauseEndTime = 0f;
                consumptionAmount = 0f;
            }
        }

        /// <summary>
        /// 体力システムからの報告データ
        /// </summary>
        [System.Serializable]
        public struct HealthReport
        {
            public float currentHealth;
            public float previousHealth;
            public float maxHealth;
            public bool isStunned;
            public bool wasStunnedLastFrame;
            public float stunGauge;
            public bool isInvincible;
            public float invincibilityTimeRemaining;
            public AttackDirection lastHitDirection;
            public bool lastHitWasCritical;
            public float lastDamageAmount;

            public HealthReport(float health, float maximum)
            {
                currentHealth = health;
                previousHealth = health;
                maxHealth = maximum;
                isStunned = false;
                wasStunnedLastFrame = false;
                stunGauge = 0f;
                isInvincible = false;
                invincibilityTimeRemaining = 0f;
                lastHitDirection = AttackDirection.Up;
                lastHitWasCritical = false;
                lastDamageAmount = 0f;
            }
        }

        /// <summary>
        /// 方向システムからの報告データ
        /// </summary>
        [System.Serializable]
        public struct DirectionReport
        {
            public AttackDirection currentDirection;
            public AttackDirection previousDirection;
            public bool canChangeDirection;
            public bool directionChangeBlocked;
            public float lastChangeTime;

            public DirectionReport(AttackDirection direction)
            {
                currentDirection = direction;
                previousDirection = direction;
                canChangeDirection = true;
                directionChangeBlocked = false;
                lastChangeTime = Time.time;
            }
        }

        /// <summary>
        /// 射撃システムからの報告データ
        /// </summary>
        [System.Serializable]
        public struct ShootingReport
        {
            public bool isAiming;
            public float aimAccuracy;
            public Vector3 aimDirection;
            public bool isReloading;
            public float reloadProgress;
            public bool hasMaxAccuracy;
            public float weaponHeat;
            public bool isOverheated;

            public ShootingReport(bool aiming)
            {
                isAiming = aiming;
                aimAccuracy = 0f;
                aimDirection = Vector3.forward;
                isReloading = false;
                reloadProgress = 0f;
                hasMaxAccuracy = false;
                weaponHeat = 0f;
                isOverheated = false;
            }
        }
    }

    /// <summary>
    /// StateSystemの通知機能拡張部分
    /// BaseSystemから受信した報告を処理し、必要に応じてBaseControllerに通知する
    /// </summary>
    public partial class StateSystem : BaseSystem<StateNotification>
    {
        // 各システムからの報告データ保持
        [Header("報告データ保持")]
        [SerializeField, ReadOnly]
        private StateReportData.MovementReport _movementReport;

        [SerializeField, ReadOnly]
        private StateReportData.AttackReport _attackReport;

        [SerializeField, ReadOnly]
        private StateReportData.DefenseReport _defenseReport;

        [SerializeField, ReadOnly]
        private StateReportData.EnergyReport _energyReport;

        [SerializeField, ReadOnly]
        private StateReportData.HealthReport _healthReport;

        [SerializeField, ReadOnly]
        private StateReportData.DirectionReport _directionReport;

        // 射撃関連は存在しないため削除
        // [SerializeField, ReadOnly]
        // private StateReportData.ShootingReport shootingReport;

        // BaseController向け通知用Subject
        private readonly Subject<StateNotification> _controllerNotificationSubject = new Subject<StateNotification>();

        /// <summary>
        /// BaseControllerが購読するための通知Observable
        /// </summary>
        public IObservable<StateNotification> ControllerNotifications => _controllerNotificationSubject.AsObservable();

        #region 各システムからの報告受信メソッド

        /// <summary>
        /// MovementSystemからの移動状態報告
        /// </summary>
        /// <param name="isMoving">移動中かどうか</param>
        /// <param name="speed">現在の速度</param>
        /// <param name="direction">移動方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveMovementStateReport(bool isMoving, float speed, Vector3 direction)
        {
            _movementReport = new StateReportData.MovementReport(isMoving, speed, direction);

            DebugLog($"移動状態報告受信 - 移動中: {isMoving}, 速度: {speed:F1}");

            // 状態更新
            ProcessMovementStateChange(isMoving, speed, direction);

            // 通知判定（後で実装予定地点）
            // EvaluateMovementNotifications(previousReport, movementReport);
        }

        /// <summary>
        /// MovementSystemからの地面接触状態報告
        /// </summary>
        /// <param name="isGrounded">接地しているかどうか</param>
        /// <param name="verticalVelocity">垂直速度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveGroundStateReport(bool isGrounded, float verticalVelocity)
        {
            _movementReport.isGrounded = isGrounded;
            _movementReport.verticalVelocity = verticalVelocity;

            DebugLog($"接地状態報告受信 - 接地: {isGrounded}, 垂直速度: {verticalVelocity:F1}");

            // 状態更新
            ProcessGroundStateChange(isGrounded, verticalVelocity);
        }

        /// <summary>
        /// MovementSystemからのブースト状態報告
        /// </summary>
        /// <param name="isBoosting">ブースト中かどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveBoostStateReport(bool isBoosting)
        {
            _movementReport.isBoosting = isBoosting;

            DebugLog($"ブースト状態報告受信 - ブースト中: {isBoosting}");

            // 状態更新
            ProcessBoostStateChange(isBoosting);
        }

        /// <summary>
        /// MovementSystemからの回避実行報告
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveDodgeExecutedReport()
        {
            _movementReport.dodgeExecuted = true;
            _movementReport.lastDodgeTime = Time.time;

            DebugLog("回避実行報告受信");

            // 状態更新
            ProcessDodgeExecuted();

            // 通知: 回避が使用不可になった
            SendControllerNotification(new StateNotification(StateNotificationTrigger.DodgeBecameUnavailable, 0.3f));
        }

        /// <summary>
        /// AttackSystemからの攻撃状態報告
        /// </summary>
        /// <param name="isAttacking">攻撃中かどうか</param>
        /// <param name="attackType">攻撃タイプ</param>
        /// <param name="direction">攻撃方向</param>
        /// <param name="comboCount">コンボ数</param>
        /// <param name="isAirAttack">空中攻撃かどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveAttackStateReport(bool isAttacking, AttackType attackType, AttackDirection direction, int comboCount, bool isAirAttack)
        {
            var previousComboCount = _attackReport.comboCount;

            _attackReport = new StateReportData.AttackReport(isAttacking, attackType, direction)
            {
                comboCount = comboCount,
                isAirAttack = isAirAttack
            };

            DebugLog($"攻撃状態報告受信 - 攻撃中: {isAttacking}, タイプ: {attackType}, コンボ: {comboCount}");

            // 状態更新
            ProcessAttackStateChange(isAttacking, comboCount, isAirAttack);

            // コンボ中断の通知判定
            if (previousComboCount > 0 && comboCount == 0 && !isAttacking)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.ComboInterrupted, previousComboCount, 0.4f));
            }
        }

        /// <summary>
        /// AttackSystemからの攻撃結果報告
        /// </summary>
        /// <param name="attackHit">攻撃が命中したかどうか</param>
        /// <param name="wasBlocked">攻撃がブロックされたかどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveAttackResultReport(bool attackHit, bool wasBlocked)
        {
            _attackReport.attackHit = attackHit;
            _attackReport.attackBlocked = wasBlocked;

            DebugLog($"攻撃結果報告受信 - 命中: {attackHit}, ブロック: {wasBlocked}");
        }

        /// <summary>
        /// AttackSystemからのスキルクールダウン報告
        /// </summary>
        /// <param name="skillIndex">スキルインデックス</param>
        /// <param name="cooldownTime">クールダウン時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveSkillCooldownReport(int skillIndex, float cooldownTime)
        {
            _attackReport.skillIndex = skillIndex;
            _attackReport.skillCooldown = cooldownTime;

            DebugLog($"スキルクールダウン報告受信 - スキル{skillIndex}: {cooldownTime:F1}秒");

            // 状態更新
            ProcessSkillCooldownChange(skillIndex, cooldownTime);

            // スキル使用可能通知判定
            if (cooldownTime <= 0f)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.SkillBecameAvailable, skillIndex, 0.6f));
            }
        }

        /// <summary>
        /// DefenseSystemからのガード状態報告
        /// </summary>
        /// <param name="isGuarding">ガード中かどうか</param>
        /// <param name="direction">ガード方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveGuardStateReport(bool isGuarding, AttackDirection direction)
        {
            _defenseReport = new StateReportData.DefenseReport(isGuarding, direction);

            DebugLog($"ガード状態報告受信 - ガード中: {isGuarding}, 方向: {direction}");

            // 状態更新
            ProcessGuardStateChange(isGuarding, direction);
        }

        /// <summary>
        /// DefenseSystemからのブロッキング結果報告
        /// </summary>
        /// <param name="isBlocking">ブロッキング中かどうか</param>
        /// <param name="success">ブロッキング成功かどうか</param>
        /// <param name="blockedAttackType">ブロックした攻撃タイプ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveBlockingResultReport(bool isBlocking, bool success, AttackType blockedAttackType)
        {
            _defenseReport.isBlocking = isBlocking;
            _defenseReport.blockingSuccess = success;
            _defenseReport.blockingTimestamp = Time.time;
            _defenseReport.blockedAttackType = blockedAttackType;

            DebugLog($"ブロッキング結果報告受信 - ブロッキング中: {isBlocking}, 成功: {success}");

            // 状態更新
            ProcessBlockingStateChange(isBlocking, success);

            // ブロッキング結果通知
            if (isBlocking)
            {
                if (success)
                {
                    SendControllerNotification(new StateNotification(StateNotificationTrigger.BlockingSucceeded, 0.7f));
                }
                else
                {
                    SendControllerNotification(new StateNotification(StateNotificationTrigger.BlockingFailed, 0.5f));
                }
            }
        }

        /// <summary>
        /// DefenseSystemからのガード破壊報告
        /// </summary>
        /// <param name="isGuardBroken">ガードが破られたかどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveGuardBrokenReport(bool isGuardBroken)
        {
            _defenseReport.guardBroken = isGuardBroken;

            DebugLog($"ガード破壊報告受信 - ガード破壊: {isGuardBroken}");

            // 状態更新
            ProcessGuardBrokenChange(isGuardBroken);

            // ガード破壊通知
            if (isGuardBroken)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.GuardBroken, 0.8f));
            }
        }

        /// <summary>
        /// EnergySystemからのエネルギー変更報告
        /// </summary>
        /// <param name="energyPercentage">エネルギー割合</param>
        /// <param name="consumptionAmount">消費量（正の値）</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveEnergyChangeReport(float energyPercentage, float consumptionAmount = 0f)
        {
            var previousPercentage = _energyReport.currentPercentage;
            var wasDepletedBefore = _energyReport.isDepleted;

            _energyReport.previousPercentage = previousPercentage;
            _energyReport.currentPercentage = energyPercentage;
            _energyReport.isDepleted = energyPercentage <= 0f;
            _energyReport.consumptionAmount = consumptionAmount;

            DebugLog($"エネルギー変更報告受信 - {previousPercentage:P1} → {energyPercentage:P1}");

            // 状態更新
            ProcessEnergyChange(energyPercentage);

            // エネルギー切れ突入通知
            if (!wasDepletedBefore && _energyReport.isDepleted)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.EnergyDepleted, energyPercentage, 1.0f));
            }
            // エネルギー完全回復通知
            else if (wasDepletedBefore && energyPercentage >= 1f)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.EnergyRecovered, energyPercentage, 0.8f));
            }
        }

        /// <summary>
        /// EnergySystemからのエネルギー回復停止報告
        /// </summary>
        /// <param name="pauseDuration">停止期間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveEnergyRecoveryPauseReport(float pauseDuration)
        {
            _energyReport.isRecoveryPaused = true;
            _energyReport.recoveryPauseEndTime = Time.time + pauseDuration;

            DebugLog($"エネルギー回復停止報告受信 - 停止期間: {pauseDuration:F1}秒");

            // 状態更新
            ProcessEnergyRecoveryPauseChange(pauseDuration);

            // 回復停止通知（重要度は停止期間に応じて）
            float severity = Mathf.Clamp01(pauseDuration / 5f);
            SendControllerNotification(new StateNotification(StateNotificationTrigger.EnergyRecoveryPaused, pauseDuration, severity));
        }

        /// <summary>
        /// HealthSystemからのダメージ報告
        /// </summary>
        /// <param name="currentHealth">現在の体力</param>
        /// <param name="damageAmount">ダメージ量</param>
        /// <param name="causesStun">スタンを引き起こすかどうか</param>
        /// <param name="isCritical">クリティカルヒットかどうか</param>
        /// <param name="hitDirection">被弾方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveDamageReport(float currentHealth, float damageAmount, bool causesStun, bool isCritical, AttackDirection hitDirection)
        {
            _healthReport.previousHealth = _healthReport.currentHealth;
            _healthReport.currentHealth = currentHealth;
            _healthReport.lastDamageAmount = damageAmount;
            _healthReport.lastHitWasCritical = isCritical;
            _healthReport.lastHitDirection = hitDirection;

            DebugLog($"ダメージ報告受信 - 体力: {currentHealth:F0}, ダメージ: {damageAmount:F0}, スタン: {causesStun}");

            // 状態更新
            ProcessDamageReceived(damageAmount, causesStun);

            // 体力危険レベル通知
            float healthPercentage = currentHealth / _healthReport.maxHealth;
            if (healthPercentage <= 0.25f) // 25%以下で危険
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.HealthCritical, healthPercentage, 0.9f));
            }

            // スタン発生通知
            if (causesStun)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.StunOccurred, 1.0f));
            }
        }

        /// <summary>
        /// HealthSystemからのスタン状態報告
        /// </summary>
        /// <param name="isStunned">スタン中かどうか</param>
        /// <param name="stunGauge">スタンゲージ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveStunStateReport(bool isStunned, float stunGauge)
        {
            bool wasStunnedBefore = _healthReport.isStunned;
            _healthReport.isStunned = isStunned;
            _healthReport.stunGauge = stunGauge;

            DebugLog($"スタン状態報告受信 - スタン中: {isStunned}, ゲージ: {stunGauge:F1}");

            // スタン回復通知
            if (wasStunnedBefore && !isStunned)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.StunRecovered, 0.6f));
            }
        }

        /// <summary>
        /// HealthSystemからの無敵状態報告
        /// </summary>
        /// <param name="isInvincible">無敵状態かどうか</param>
        /// <param name="timeRemaining">残り時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveInvincibilityReport(bool isInvincible, float timeRemaining)
        {
            bool wasInvincibleBefore = _healthReport.isInvincible;
            _healthReport.isInvincible = isInvincible;
            _healthReport.invincibilityTimeRemaining = timeRemaining;

            DebugLog($"無敵状態報告受信 - 無敵: {isInvincible}, 残り時間: {timeRemaining:F1}");

            // 無敵状態開始通知
            if (!wasInvincibleBefore && isInvincible)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.InvincibilityStarted, timeRemaining, 0.4f));
            }
            // 無敵状態終了通知
            else if (wasInvincibleBefore && !isInvincible)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.InvincibilityEnded, 0.5f));
            }
        }

        /// <summary>
        /// DirectionSystemからの方向変更報告
        /// </summary>
        /// <param name="newDirection">新しい方向</param>
        /// <param name="changeBlocked">方向変更が阻止されたかどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveDirectionChangeReport(AttackDirection newDirection, bool changeBlocked)
        {
            _directionReport.previousDirection = _directionReport.currentDirection;
            _directionReport.currentDirection = newDirection;
            _directionReport.directionChangeBlocked = changeBlocked;
            _directionReport.lastChangeTime = Time.time;

            DebugLog($"方向変更報告受信 - 方向: {newDirection}, 変更阻止: {changeBlocked}");

            // 状態更新
            ProcessDirectionChange(newDirection);

            // 方向変更阻止通知
            if (changeBlocked)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.DirectionChangeBlocked, newDirection, 0.3f));
            }
        }

        /// <summary>
        /// DirectionSystemからの方向変更可能性報告
        /// </summary>
        /// <param name="canChange">方向変更可能かどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveDirectionAvailabilityReport(bool canChange)
        {
            _directionReport.canChangeDirection = canChange;

            DebugLog($"方向変更可能性報告受信 - 変更可能: {canChange}");
        }

        /// <summary>
        /// アクション実行制限の報告（汎用）
        /// </summary>
        /// <param name="actionType">制限されたアクションタイプ</param>
        /// <param name="isRestricted">制限されているかどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReceiveActionRestrictionReport(ActionType actionType, bool isRestricted)
        {
            DebugLog($"アクション制限報告受信 - {actionType}: {(isRestricted ? "制限" : "解除")}");

            // 制限・解除通知
            if (isRestricted)
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.ActionRestricted, (int)actionType, 0.4f));
            }
            else
            {
                SendControllerNotification(new StateNotification(StateNotificationTrigger.ActionUnrestricted, (int)actionType, 0.3f));
            }
        }

        #endregion

        #region 内部通知送信メソッド

        /// <summary>
        /// BaseControllerに通知を送信
        /// </summary>
        /// <param name="notification">通知データ</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendControllerNotification(StateNotification notification)
        {
            _controllerNotificationSubject.OnNext(notification);
            DebugLog($"Controller通知送信: {notification.trigger} (重要度: {notification.severity:F1})");
        }

        /// <summary>
        /// 複数の通知を一度に送信（バッチ処理用）
        /// </summary>
        /// <param name="notifications">通知データ配列</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SendControllerNotifications(params StateNotification[] notifications)
        {
            foreach (var notification in notifications)
            {
                SendControllerNotification(notification);
            }
        }

        #endregion

        #region 既存の状態処理メソッドとの統合

        // 注意: 以下のメソッドは既存のStateSystemの処理メソッドです
        // 実際の実装では、これらが既に存在することを前提としています

        /// <summary>
        /// 移動状態変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessMovementStateChange(bool isMoving, float speed, Vector3 movementDirection)
        {
            // 既存のStateSystemの処理を呼び出し
            // 実装は既存コードに依存
        }

        /// <summary>
        /// 地面接触状態変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessGroundStateChange(bool isGrounded, float verticalVelocity)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// ブースト状態変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessBoostStateChange(bool isBoosting)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// 回避実行の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessDodgeExecuted()
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// 攻撃状態変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessAttackStateChange(bool isAttacking, int comboCount, bool isAirAttack)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// スキルクールダウン変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessSkillCooldownChange(int skillIndex, float cooldownTime)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// ガード状態変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessGuardStateChange(bool isGuarding, AttackDirection direction)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// ブロッキング状態変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessBlockingStateChange(bool isBlocking, bool success)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// ガード破壊の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessGuardBrokenChange(bool isGuardBroken)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// エネルギー変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessEnergyChange(float energyPercentage)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// エネルギー回復停止の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessEnergyRecoveryPauseChange(float pauseDuration)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// ダメージ受信の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessDamageReceived(float damage, bool causesStun)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// 方向変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessDirectionChange(AttackDirection direction)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// マニューバクールダウン変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessManeuverCooldownChange(int maneuverIndex, float cooldownTime)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// エイム状態変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessAimingStateChange(bool isAiming, float accuracy, float weaponPower)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        /// <summary>
        /// リロード状態変更の処理（既存メソッドの呼び出し）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReloadStateChange(bool isReloading)
        {
            // 既存のStateSystemの処理を呼び出し
        }

        #endregion

        #region 初期化処理

        /// <summary>
        /// 通知システムの初期化
        /// </summary>
        private void InitializeNotificationSystem()
        {
            // 報告データの初期化
            _movementReport = new StateReportData.MovementReport(false, 0f, Vector3.zero);
            _attackReport = new StateReportData.AttackReport(false, AttackType.None, AttackDirection.Up);
            _defenseReport = new StateReportData.DefenseReport(false, AttackDirection.Up);
            _energyReport = new StateReportData.EnergyReport(1f);
            _healthReport = new StateReportData.HealthReport(100f, 100f); // デフォルト値
            _directionReport = new StateReportData.DirectionReport(AttackDirection.Up);

            DebugLog("通知システム初期化完了");
        }

        #endregion
    }
}