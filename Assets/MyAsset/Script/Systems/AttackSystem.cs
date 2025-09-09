using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UniRx;
using UnityEngine;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 攻撃データの構造体
    /// </summary>
    [System.Serializable]
    public struct AttackData
    {
        public AttackType attackType;
        public float damage;
        public AttackDirection direction;
        public bool isExecuting;
        public bool isAiming;
        public float aimingAccuracy;
        public int comboCount;
        public bool isAerialAttack;
        public bool isDodgeAttack;
    }

    /// <summary>
    /// 攻撃システム - 近接攻撃、射撃攻撃、スキル攻撃、コンボシステムを管理
    /// </summary>
    public class AttackSystem : BaseSystem<AttackData>
    {
        // コンポーネント
        private StateSystem stateSystem;
        private EnergySystem energySystem;
        private MovementSystem movementSystem;

        // 攻撃状態
        private AttackData currentAttackData;

        // コンボ管理
        private ComboStateData comboState = new ComboStateData();

        // 回避攻撃管理
        private bool canDodgeAttack = false;
        private float dodgeAttackTimer = 0f;
        private Vector3 lastDodgeDirection;

        [Title("攻撃判定")]
        [PropertyTooltip("近接攻撃の判定コライダー")]
        [SerializeField] private AttackCollider meleeAttackCollider;

        [PropertyTooltip("射撃攻撃のプレハブ")]
        [SerializeField] private GameObject bulletPrefab;

        [PropertyTooltip("弾丸発射位置")]
        [SerializeField] private Transform bulletSpawnPoint;

        [Title("現在の状態")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在攻撃中かどうか")]
        public bool IsAttacking { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在リロード中かどうか")]
        public bool IsReloading { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = false;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の射撃精度")]
        public float CurrentAimingAccuracy { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 0f;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の狙い方向")]
        public Vector3 CurrentAimDirection { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = Vector3.forward;

        [Title("コンボ状態")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在のコンボ段数")]
        public int CurrentComboCount => comboState.currentCount;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("コンボ実行中かどうか")]
        public bool IsInCombo => comboState.isActive;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("空中コンボかどうか")]
        public bool IsAerialCombo => comboState.isAerialCombo;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("回避攻撃が可能かどうか")]
        public bool CanDodgeAttack => canDodgeAttack;

        // 内部状態
        private float lastAttackTime = 0f;
        private float aimingStartTime = 0f;
        private bool isAiming = false;
        private AttackDirection currentAttackDirection = AttackDirection.Up;

        // スキル管理
        [Title("スキル設定")]
        [PropertyTooltip("利用可能なスキル一覧")]
        [SerializeField] private List<SkillData> availableSkills = new List<SkillData>();

        /// <summary>
        /// スキルデータ
        /// </summary>
        [Serializable]
        public class SkillData
        {
            [PropertyTooltip("スキル名")]
            public string skillName;

            [PropertyTooltip("エネルギー消費量")]
            public float energyCost;

            [PropertyTooltip("クールタイム")]
            public float cooldownTime;

            [PropertyTooltip("ダメージ")]
            public float damage;

            [PropertyTooltip("攻撃タイプ")]
            public AttackType attackType;

            [PropertyTooltip("ガード可能かどうか")]
            public bool canBeGuarded = false;
        }

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

            if ( Settings?.attack == null )
            {
                DebugLogError("AttackSettingsが見つかりません");
                return;
            }

            InitializeAttackCollider();
            InitializeDefaultSkills();
            InitializeComboSystem();

            // 初期データの設定
            UpdateAttackData();
        }

        protected override void SetupObservables()
        {
            // 攻撃状態の更新をObservableで通知
            UniRx.Observable.EveryUpdate()
                .Subscribe(_ => UpdateAndNotifyAttackData())
                .AddTo(disposables);

            // コンボタイマーの更新
            UniRx.Observable.EveryUpdate()
                .Subscribe(_ => UpdateComboTimer())
                .AddTo(disposables);

            // 回避攻撃タイマーの更新
            UniRx.Observable.EveryUpdate()
                .Subscribe(_ => UpdateDodgeAttackTimer())
                .AddTo(disposables);
        }

        private void UpdateAndNotifyAttackData()
        {
            UpdateAttackData();
            NotifyObservers(currentAttackData);
        }

        private void UpdateAttackData()
        {
            currentAttackData = new AttackData
            {
                attackType = GetCurrentAttackType(),
                damage = GetCurrentDamage(),
                direction = currentAttackDirection,
                isExecuting = IsAttacking,
                isAiming = isAiming,
                aimingAccuracy = CurrentAimingAccuracy,
                comboCount = CurrentComboCount,
                isAerialAttack = IsAerialCombo,
                isDodgeAttack = canDodgeAttack
            };
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            UpdateAiming();
            UpdateAttackState();
        }

        #region Public Attack Methods

        /// <summary>
        /// 弱攻撃を実行
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteWeakAttack(AttackDirection direction)
        {
            if ( !CanExecuteWeakAttack() )
                return;

            bool isAerial = stateSystem.AnalysisData.isAirborne;
            AttackType attackType = isAerial ? AttackType.AerialWeakMelee : AttackType.WeakMelee;

            // コンボ状態の更新
            UpdateComboState(attackType, isAerial);

            // 武器設定からモーションデータを取得
            var motionData = GetAttackMotionData(comboState.currentCount - 1, isAerial);
            if ( motionData == null )
            {
                DebugLogError("攻撃モーションデータが見つかりません");
                return;
            }

            // エネルギー消費チェック
            if ( !energySystem.UseEnergy(motionData.energyCost) )
                return;

            currentAttackDirection = direction;
            stateSystem.AnalysisData.lastAttackDirection = direction;

            // 踏み込み実行
            if ( motionData.ShouldLunge )
            {
                ExecuteLunge(motionData, direction, isAerial);
            }

            // 攻撃情報作成
            var attackInfo = CreateComboAttackInfo(attackType, direction, motionData, comboState.currentCount - 1, isAerial);

            StartAttack(attackInfo, motionData.startupTime);
        }

        /// <summary>
        /// 強攻撃を実行
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteStrongAttack(AttackDirection direction)
        {
            if ( !CanExecuteStrongAttack() )
                return;

            bool isAerial = stateSystem.AnalysisData.isAirborne;
            AttackType attackType = isAerial ? AttackType.AerialStrongMelee : AttackType.StrongMelee;

            // 強攻撃の処理
            AttackMotionData motionData;

            if ( IsInCombo && Settings.weaponSettings?.comboSettings?.CanFinishWithStrong(comboState.currentCount) == true )
            {
                // コンボフィニッシュ
                motionData = Settings.weaponSettings.comboSettings.strongFinisher;
                comboState.isActive = false; // コンボ終了
            }
            else
            {
                // 初段強攻撃
                motionData = Settings.weaponSettings?.initialStrongAttack ?? GetDefaultStrongAttackData();
                comboState.Reset(); // コンボリセット
            }

            // エネルギー消費チェック
            if ( !energySystem.UseEnergy(motionData.energyCost) )
                return;

            currentAttackDirection = direction;
            stateSystem.AnalysisData.lastAttackDirection = direction;

            // 踏み込み実行
            if ( motionData.ShouldLunge )
            {
                ExecuteLunge(motionData, direction, isAerial);
            }

            // 攻撃情報作成
            var attackInfo = CreateAttackInfo(attackType, direction, motionData, isAerial);
            attackInfo.hasSuperArmor = motionData.hasSuperArmor; // 初段強攻撃はスーパーアーマー
            attackInfo.canBeGuarded = !motionData.isUnguardable;
            attackInfo.canBeBlocked = !motionData.isUnblockable;

            StartAttack(attackInfo, motionData.startupTime);
        }

        /// <summary>
        /// 回避攻撃を実行
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteDodgeAttack(AttackDirection direction)
        {
            if ( !CanExecuteDodgeAttack() )
                return;

            var dodgeAttackSettings = Settings.weaponSettings?.dodgeAttackSettings;
            if ( dodgeAttackSettings == null )
                return;

            var motionData = dodgeAttackSettings.dodgeAttackMotion;

            // エネルギー消費チェック
            if ( !energySystem.UseEnergy(motionData.energyCost) )
                return;

            currentAttackDirection = direction;

            // 回避攻撃の踏み込み強化計算
            Vector3 toEnemyDirection = GetDirectionToEnemy();
            float lungeMultiplier = dodgeAttackSettings.CalculateLungeMultiplier(lastDodgeDirection, toEnemyDirection);

            // 踏み込み実行
            ExecuteDodgeAttackLunge(motionData, direction, lungeMultiplier);

            // 攻撃情報作成
            var attackInfo = AttackInfo.CreateDodgeAttack(AttackType.DodgeAttack, direction, motionData.damage, characterController.Position, lungeMultiplier);

            StartAttack(attackInfo, motionData.startupTime);

            // 回避攻撃状態をリセット
            canDodgeAttack = false;
            dodgeAttackTimer = 0f;
        }

        /// <summary>
        /// スキル攻撃を実行
        /// </summary>
        /// <param name="skillIndex">スキルインデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteSkill(int skillIndex)
        {
            if ( !CanUseSkill(skillIndex) )
                return;

            var skill = availableSkills[skillIndex];

            if ( !energySystem.UseEnergy(skill.energyCost) )
                return;

            var attackInfo = CreateAttackInfo(skill.attackType, currentAttackDirection, skill.damage);
            attackInfo.canBeGuarded = skill.canBeGuarded;

            StartAttack(attackInfo, 0.3f); // スキルは標準発生時間

            // クールダウン設定
            stateSystem.ReportSkillCooldown(skillIndex, skill.cooldownTime);

            // コンボリセット
            comboState.Reset();
        }

        /// <summary>
        /// 弱射撃を実行
        /// </summary>
        /// <param name="direction">射撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteWeakShoot(AttackDirection direction)
        {
            if ( !CanShoot() )
                return;

            var attackInfo = CreateAttackInfo(AttackType.WeakRanged, direction, Settings.attack.weakShootDamage);
            attackInfo.canBeGuarded = true; // 弱射撃はガード可能

            FireBullet(attackInfo);
        }

        /// <summary>
        /// 強射撃を実行
        /// </summary>
        /// <param name="direction">射撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteStrongShoot(AttackDirection direction)
        {
            if ( !CanShoot() )
                return;

            var attackInfo = CreateAttackInfo(AttackType.StrongRanged, direction, Settings.attack.strongShootDamage);
            attackInfo.canBeGuarded = false; // 強射撃はガード不可

            FireBullet(attackInfo);
        }

        /// <summary>
        /// 射撃スキルを実行
        /// </summary>
        /// <param name="skillIndex">スキルインデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteShootSkill(int skillIndex)
        {
            if ( !CanUseSkill(skillIndex) )
                return;

            var skill = availableSkills[skillIndex];

            if ( !energySystem.UseEnergy(skill.energyCost) )
                return;

            var attackInfo = CreateAttackInfo(skill.attackType, currentAttackDirection, skill.damage);

            FireBullet(attackInfo);

            // クールダウン設定
            stateSystem.ReportSkillCooldown(skillIndex, skill.cooldownTime);
        }

        /// <summary>
        /// 攻撃キャンセル
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CancelAttack()
        {
            if ( IsAttacking && CanCancelAttack() )
            {
                energySystem.UseEnergy(Settings.attack.strongAttackCancelCost);
                StopAttack();
            }
        }

        /// <summary>
        /// 回避実行時のコールバック（MovementSystemから呼ばれる）
        /// </summary>
        /// <param name="dodgeDirection">回避方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnDodgeExecuted(Vector3 dodgeDirection)
        {
            lastDodgeDirection = dodgeDirection;
            canDodgeAttack = true;
            dodgeAttackTimer = Settings.attack.dodgeAttackWindow;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// コンボシステムの初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeComboSystem()
        {
            if ( Settings.weaponSettings?.comboSettings != null )
            {
                comboState.maxCount = Settings.weaponSettings.comboSettings.maxComboCount;
                comboState.comboWindow = Settings.weaponSettings.comboSettings.comboWindow;
                comboState.resetTime = Settings.weaponSettings.comboSettings.comboResetTime;
            }
            else
            {
                comboState.maxCount = 3;
                comboState.comboWindow = Settings.attack.comboWindow;
                comboState.resetTime = Settings.attack.comboResetTime;
            }
        }

        /// <summary>
        /// コンボ状態の更新
        /// </summary>
        /// <param name="attackType">攻撃タイプ</param>
        /// <param name="isAerial">空中攻撃かどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateComboState(AttackType attackType, bool isAerial)
        {
            float currentTime = Time.time;

            if ( !comboState.isActive || !comboState.IsWithinWindow(currentTime) )
            {
                // 新しいコンボ開始
                comboState.Reset();
                comboState.isActive = true;
                comboState.startTime = currentTime;
                comboState.isAerialCombo = isAerial;
            }

            comboState.currentCount++;
            comboState.lastAttackTime = currentTime;
            comboState.canFinishWithStrong = comboState.currentCount > 0;
            comboState.isAcceptingInput = true;

            // StateSystemに報告
            stateSystem.AnalysisData.currentComboCount = comboState.currentCount;
            stateSystem.AnalysisData.maxComboCount = comboState.maxCount;
            stateSystem.AnalysisData.isInCombo = comboState.isActive;
        }

        /// <summary>
        /// コンボタイマーの更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateComboTimer()
        {
            if ( !comboState.isActive )
                return;

            float currentTime = Time.time;

            // コンボタイムアウトチェック
            if ( comboState.IsTimedOut(currentTime) )
            {
                comboState.Reset();
                stateSystem.AnalysisData.isInCombo = false;
                stateSystem.AnalysisData.currentComboCount = 0;
            }
            else
            {
                // 受付時間の更新
                float remainingWindow = comboState.comboWindow - (currentTime - comboState.lastAttackTime);
                stateSystem.AnalysisData.comboWindowRemaining = Mathf.Max(0f, remainingWindow);
            }
        }

        /// <summary>
        /// 回避攻撃タイマーの更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateDodgeAttackTimer()
        {
            if ( canDodgeAttack )
            {
                dodgeAttackTimer -= Time.deltaTime;
                if ( dodgeAttackTimer <= 0f )
                {
                    canDodgeAttack = false;
                }

                stateSystem.AnalysisData.canDodgeAttack = canDodgeAttack;
                stateSystem.AnalysisData.dodgeAttackWindowRemaining = Mathf.Max(0f, dodgeAttackTimer);
            }
        }

        /// <summary>
        /// 踏み込み実行
        /// </summary>
        /// <param name="motionData">モーションデータ</param>
        /// <param name="direction">攻撃方向</param>
        /// <param name="isAerial">空中攻撃かどうか</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteLunge(AttackMotionData motionData, AttackDirection direction, bool isAerial)
        {
            if ( movementSystem == null )
                return;

            Vector3 lungeDirection = DirectionToVector(direction);
            float distance = motionData.lungeDistance;

            if ( isAerial )
            {
                distance *= motionData.aerialLungeMultiplier;
            }

            movementSystem.ExecuteLunge(lungeDirection, distance, motionData.lungeSpeed);
        }

        /// <summary>
        /// 回避攻撃の踏み込み実行
        /// </summary>
        /// <param name="motionData">モーションデータ</param>
        /// <param name="direction">攻撃方向</param>
        /// <param name="multiplier">踏み込み倍率</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteDodgeAttackLunge(AttackMotionData motionData, AttackDirection direction, float multiplier)
        {
            if ( movementSystem == null )
                return;

            Vector3 lungeDirection = DirectionToVector(direction);
            float distance = motionData.lungeDistance * multiplier;

            movementSystem.ExecuteLunge(lungeDirection, distance, motionData.lungeSpeed);
        }

        /// <summary>
        /// 攻撃モーションデータを取得
        /// </summary>
        /// <param name="comboIndex">コンボインデックス</param>
        /// <param name="isAerial">空中攻撃かどうか</param>
        /// <returns>モーションデータ</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AttackMotionData GetAttackMotionData(int comboIndex, bool isAerial)
        {
            if ( Settings.weaponSettings?.comboSettings != null )
            {
                return Settings.weaponSettings.comboSettings.GetAttackData(comboIndex, isAerial);
            }

            // デフォルトデータを返す
            return GetDefaultWeakAttackData();
        }

        /// <summary>
        /// デフォルト弱攻撃データを作成
        /// </summary>
        /// <returns>デフォルト攻撃データ</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AttackMotionData GetDefaultWeakAttackData()
        {
            return new AttackMotionData
            {
                attackName = "Default Weak",
                damage = Settings.attack.weakAttackDamage,
                startupTime = Settings.attack.weakAttackStartup,
                activeTime = 0.2f,
                recoveryTime = 0.3f,
                lungeDistance = Settings.attack.baseLungeDistance,
                lungeSpeed = 8f,
                lungeOnlyOnFirstHit = Settings.attack.lungeOnlyOnFirstHit,
                aerialDamageMultiplier = Settings.attack.aerialDamageMultiplier,
                aerialLungeMultiplier = Settings.attack.aerialLungeMultiplier,
                aerialFloatTime = Settings.attack.aerialComboFloatTime,
                energyCost = Settings.attack.weakAttackEnergyCost,
                hasSuperArmor = false,
                isUnguardable = false,
                isUnblockable = false,
                stunAccumulation = Settings.attack.weakAttackDamage * 0.5f
            };
        }

        /// <summary>
        /// デフォルト強攻撃データを作成
        /// </summary>
        /// <returns>デフォルト強攻撃データ</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AttackMotionData GetDefaultStrongAttackData()
        {
            return new AttackMotionData
            {
                attackName = "Default Strong",
                damage = Settings.attack.strongAttackDamage,
                startupTime = Settings.attack.strongAttackStartup,
                activeTime = 0.3f,
                recoveryTime = 0.5f,
                lungeDistance = Settings.attack.baseLungeDistance * Settings.attack.strongLungeMultiplier,
                lungeSpeed = 8f,
                lungeOnlyOnFirstHit = true,
                aerialDamageMultiplier = Settings.attack.aerialDamageMultiplier,
                aerialLungeMultiplier = Settings.attack.aerialLungeMultiplier,
                aerialFloatTime = Settings.attack.aerialComboFloatTime,
                energyCost = Settings.attack.strongAttackEnergyCost,
                hasSuperArmor = true,
                isUnguardable = true,
                isUnblockable = false,
                stunAccumulation = Settings.attack.strongAttackDamage * 0.7f
            };
        }

        /// <summary>
        /// 攻撃情報を作成（汎用）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AttackInfo CreateAttackInfo(AttackType attackType, AttackDirection direction, float damage, bool isAerial = false)
        {
            return new AttackInfo
            {
                attackType = attackType,
                direction = direction,
                baseDamage = damage,
                comboIndex = 0,
                isAerialAttack = isAerial,
                isDodgeAttack = false,
                isComboFinisher = false,
                lungeDistance = 0f,
                lungeSpeed = 0f,
                shouldLunge = false,
                stunAccumulation = damage * 0.5f,
                canBeGuarded = true,
                canBeBlocked = true,
                isCounterAttack = false,
                hasSuperArmor = false,
                energyDamage = 0f
            };
        }

        /// <summary>
        /// 攻撃情報を作成（モーションデータベース）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AttackInfo CreateAttackInfo(AttackType attackType, AttackDirection direction, AttackMotionData motionData, bool isAerial = false)
        {
            float finalDamage = motionData.damage;
            if ( isAerial )
            {
                finalDamage *= motionData.aerialDamageMultiplier;
            }

            return new AttackInfo
            {
                attackType = attackType,
                direction = direction,
                baseDamage = finalDamage,
                comboIndex = 0,
                isAerialAttack = isAerial,
                isDodgeAttack = false,
                isComboFinisher = false,
                lungeDistance = motionData.lungeDistance,
                lungeSpeed = motionData.lungeSpeed,
                shouldLunge = motionData.lungeOnlyOnFirstHit,
                stunAccumulation = motionData.stunAccumulation,
                canBeGuarded = !motionData.isUnguardable,
                canBeBlocked = !motionData.isUnblockable,
                isCounterAttack = false,
                hasSuperArmor = motionData.hasSuperArmor,
                energyDamage = 0f
            };
        }

        /// <summary>
        /// コンボ攻撃情報を作成
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AttackInfo CreateComboAttackInfo(AttackType attackType, AttackDirection direction, AttackMotionData motionData, int comboIndex, bool isAerial)
        {
            var info = CreateAttackInfo(attackType, direction, motionData, isAerial);
            info.comboIndex = comboIndex;
            info.shouldLunge = (comboIndex == 0) && motionData.lungeOnlyOnFirstHit;

            return info;
        }

        /// <summary>
        /// 攻撃を開始
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <param name="startupTime">発生時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartAttack(AttackInfo attackInfo, float startupTime)
        {
            IsAttacking = true;
            lastAttackTime = Time.time;
            stateSystem.ReportActionStateChange(ActionState.Attacking);

            // 空中コンボ中の滞空処理
            if ( attackInfo.isAerialAttack && movementSystem != null )
            {
                var motionData = GetAttackMotionData(attackInfo.comboIndex, true);
                if ( motionData != null )
                {
                    movementSystem.StartAerialFloat(motionData.aerialFloatTime);
                }
            }

            // 発生時間後に攻撃判定発生
            UniRx.Observable.Timer(TimeSpan.FromSeconds(startupTime))
                .Subscribe(_ => ExecuteAttackHitCheck(attackInfo))
                .AddTo(disposables);

            // 攻撃終了時間
            UniRx.Observable.Timer(TimeSpan.FromSeconds(startupTime + 0.2f)).Subscribe(_ => StopAttack()).AddTo(disposables);
        }

        /// <summary>
        /// 攻撃判定実行
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteAttackHitCheck(AttackInfo attackInfo)
        {
            if ( meleeAttackCollider != null )
            {
                meleeAttackCollider.ActivateAttack(attackInfo, characterController);
            }
        }

        /// <summary>
        /// 攻撃を停止
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StopAttack()
        {
            IsAttacking = false;
            stateSystem.ReportActionStateChange(ActionState.Idle);

            if ( meleeAttackCollider != null )
            {
                meleeAttackCollider.DeactivateAttack();
            }
        }

        /// <summary>
        /// 弾丸を発射
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FireBullet(AttackInfo attackInfo)
        {
            if ( bulletPrefab == null || bulletSpawnPoint == null )
                return;

            // 偏差射撃の精度を考慮
            Vector3 fireDirection = CalculateFireDirection(attackInfo.direction);

            var bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.LookRotation(fireDirection));
            var bulletController = bullet.GetComponent<BulletController>();

            if ( bulletController != null )
            {
                bulletController.Initialize(attackInfo, characterController, CurrentAimingAccuracy);
            }

            // リロード開始
            if ( attackInfo.attackType == AttackType.StrongRanged )
            {
                StartReload();
            }
        }

        /// <summary>
        /// 発射方向を計算（偏差射撃含む）
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        /// <returns>発射方向</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 CalculateFireDirection(AttackDirection direction)
        {
            Vector3 baseDirection = DirectionToVector(direction);

            // 偏差射撃の計算
            if ( characterController.OpponentData != null && CurrentAimingAccuracy > 0.7f )
            {
                Vector3 targetVelocity = characterController.OpponentData.Velocity;
                float bulletSpeed = 20f; // 弾丸速度（設定から取得すべき）
                float distance = Vector3.Distance(transform.position, characterController.OpponentData.Position);
                float timeToTarget = distance / bulletSpeed;

                Vector3 predictedPosition = characterController.OpponentData.Position + targetVelocity * timeToTarget;
                Vector3 leadDirection = (predictedPosition - bulletSpawnPoint.position).normalized;

                // 精度に応じて予測射撃を混合
                return Vector3.Lerp(baseDirection, leadDirection, CurrentAimingAccuracy);
            }

            return baseDirection;
        }

        /// <summary>
        /// 攻撃方向をベクトルに変換
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        /// <returns>方向ベクトル</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 DirectionToVector(AttackDirection direction)
        {
            return direction switch
            {
                AttackDirection.Up => transform.forward,
                AttackDirection.Left => -transform.right,
                AttackDirection.Right => transform.right,
                _ => transform.forward
            };
        }

        /// <summary>
        /// 敵への方向を取得
        /// </summary>
        /// <returns>敵への方向ベクトル</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 GetDirectionToEnemy()
        {
            if ( characterController.OpponentData != null )
            {
                return (characterController.OpponentData.Position - transform.position).normalized;
            }
            return transform.forward;
        }

        /// <summary>
        /// リロードを開始
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartReload()
        {
            IsReloading = true;
            stateSystem.ReportReloadState(true);

            // リロード完了
            UniRx.Observable.Timer(TimeSpan.FromSeconds(2f))
                .Subscribe(_ => CompleteReload())
                .AddTo(disposables);
        }

        /// <summary>
        /// リロードを完了
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CompleteReload()
        {
            IsReloading = false;
            stateSystem.ReportReloadState(false);
        }

        /// <summary>
        /// 狙いの更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAiming()
        {
            if ( isAiming )
            {
                float aimingTime = Time.time - aimingStartTime;
                CurrentAimingAccuracy = Mathf.Clamp01(aimingTime * Settings.attack.accuracyGainRate / Settings.attack.maxAccuracyTime);
                stateSystem.ReportAimingData(CurrentAimingAccuracy, CurrentAimDirection);
            }
        }

        /// <summary>
        /// 攻撃状態の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAttackState()
        {
            // 攻撃状態の自動終了チェックなど
        }

        /// <summary>
        /// 現在の攻撃タイプを取得
        /// </summary>
        /// <returns>攻撃タイプ</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AttackType GetCurrentAttackType()
        {
            if ( canDodgeAttack )
                return AttackType.DodgeAttack;
            if ( IsAerialCombo )
                return AttackType.AerialWeakMelee;
            if ( IsInCombo )
                return AttackType.WeakMelee;
            return AttackType.None;
        }

        /// <summary>
        /// 現在のダメージを取得
        /// </summary>
        /// <returns>ダメージ量</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetCurrentDamage()
        {
            if ( Settings.weaponSettings?.comboSettings != null && IsInCombo )
            {
                var motionData = GetAttackMotionData(comboState.currentCount - 1, IsAerialCombo);
                if ( motionData != null )
                {
                    float damage = motionData.damage;
                    if ( IsAerialCombo )
                    {
                        damage *= motionData.aerialDamageMultiplier;
                    }
                    return damage;
                }
            }
            return Settings.attack.weakAttackDamage;
        }

        #region Condition Checks

        /// <summary>
        /// 弱攻撃が実行可能かどうか
        /// </summary>
        /// <returns>実行可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanExecuteWeakAttack()
        {
            if ( IsAttacking )
                return false;

            if ( stateSystem.CurrentActionMode != ActionMode.Melee )
                return false;

            // コンボ制限チェック
            if ( IsInCombo && comboState.currentCount >= comboState.maxCount )
                return false;

            // コンボ受付時間チェック
            if ( IsInCombo && !comboState.IsWithinWindow(Time.time) )
                return false;

            return stateSystem.CanExecuteAction(ActionType.WeakAttack);
        }

        /// <summary>
        /// 強攻撃が実行可能かどうか
        /// </summary>
        /// <returns>実行可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanExecuteStrongAttack()
        {
            if ( IsAttacking )
                return false;

            if ( stateSystem.CurrentActionMode != ActionMode.Melee )
                return false;

            return stateSystem.CanExecuteAction(ActionType.StrongAttack);
        }

        /// <summary>
        /// 回避攻撃が実行可能かどうか
        /// </summary>
        /// <returns>実行可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanExecuteDodgeAttack()
        {
            if ( IsAttacking )
                return false;

            if ( !canDodgeAttack )
                return false;

            if ( stateSystem.CurrentActionMode != ActionMode.Melee )
                return false;

            return stateSystem.CanExecuteAction(ActionType.DodgeAttack);
        }

        /// <summary>
        /// 射撃が可能かどうか
        /// </summary>
        /// <returns>射撃可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanShoot()
        {
            return !IsReloading &&
                   stateSystem.CurrentActionMode == ActionMode.Ranged &&
                   stateSystem.CanExecuteAction(ActionType.WeakShoot);
        }

        /// <summary>
        /// スキルが使用可能かどうか
        /// </summary>
        /// <param name="skillIndex">スキルインデックス</param>
        /// <returns>使用可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanUseSkill(int skillIndex)
        {
            if ( skillIndex < 0 || skillIndex >= availableSkills.Count )
                return false;

            return stateSystem.AnalysisData.skillCooldowns[skillIndex] <= 0f &&
                   stateSystem.CanExecuteAction(ActionType.SkillAttack);
        }

        /// <summary>
        /// 攻撃キャンセルが可能かどうか
        /// </summary>
        /// <returns>キャンセル可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanCancelAttack()
        {
            return energySystem.CanUseEnergy(Settings.attack.strongAttackCancelCost);
        }

        #endregion

        #region Public Interface Methods

        /// <summary>
        /// リロード処理の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateReloading()
        {
            // 射撃モード時で近接モードに切り替わった場合は自動リロード
            if ( stateSystem.CurrentActionMode == ActionMode.Melee && IsReloading )
            {
                CompleteReload();
            }
        }

        /// <summary>
        /// 狙いを開始
        /// </summary>
        /// <param name="direction">狙い方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartAiming(Vector3 direction)
        {
            if ( !isAiming )
            {
                isAiming = true;
                aimingStartTime = Time.time;
            }

            CurrentAimDirection = direction.normalized;
            stateSystem.ReportAimingData(CurrentAimingAccuracy, CurrentAimDirection);
        }

        /// <summary>
        /// 狙いを停止
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StopAiming()
        {
            isAiming = false;
            CurrentAimingAccuracy = 0f;
            stateSystem.ReportAimingData(0f, CurrentAimDirection);
        }

        /// <summary>
        /// コンボを強制リセット
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetCombo()
        {
            comboState.Reset();
            stateSystem.AnalysisData.isInCombo = false;
            stateSystem.AnalysisData.currentComboCount = 0;
        }

        /// <summary>
        /// 攻撃結果のコールバック
        /// </summary>
        /// <param name="result">ダメージ結果</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnAttackResult(DamageResult result)
        {
            if ( result.brokeCombo )
            {
                ResetCombo();
            }
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// 攻撃コライダーの初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeAttackCollider()
        {
            if ( meleeAttackCollider == null )
            {
                // 攻撃コライダーを動的作成
                var colliderObject = new GameObject("MeleeAttackCollider");
                colliderObject.transform.SetParent(transform);
                colliderObject.transform.localPosition = Vector3.forward;

                var collider = colliderObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 1.5f;

                meleeAttackCollider = colliderObject.AddComponent<AttackCollider>();
            }
        }

        /// <summary>
        /// デフォルトスキルの初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeDefaultSkills()
        {
            if ( availableSkills.Count == 0 )
            {
                // デフォルトスキルを追加
                availableSkills.Add(new SkillData
                {
                    skillName = "突進斬り",
                    energyCost = 25f,
                    cooldownTime = 5f,
                    damage = 80f,
                    attackType = AttackType.MeleeSkill,
                    canBeGuarded = false
                });

                availableSkills.Add(new SkillData
                {
                    skillName = "追尾ミサイル",
                    energyCost = 30f,
                    cooldownTime = 8f,
                    damage = 90f,
                    attackType = AttackType.RangedSkill,
                    canBeGuarded = false
                });
            }
        }

        #endregion

        #region Debug Methods

        [Title("デバッグ機能")]
        [Button("強制弱攻撃", ButtonSizes.Medium)]
        [GUIColor(0.8f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugWeakAttack()
        {
            ExecuteWeakAttack(AttackDirection.Up);
        }

        [Button("強制強攻撃", ButtonSizes.Medium)]
        [GUIColor(1f, 0.8f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugStrongAttack()
        {
            ExecuteStrongAttack(AttackDirection.Up);
        }

        [Button("回避攻撃テスト", ButtonSizes.Medium)]
        [GUIColor(1f, 1f, 0.8f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugDodgeAttack()
        {
            // 回避攻撃状態をシミュレート
            OnDodgeExecuted(Vector3.forward);
            ExecuteDodgeAttack(AttackDirection.Up);
        }

        [Button("コンボリセット", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugResetCombo()
        {
            ResetCombo();
        }

        [Button("スキル1実行", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DebugSkill1()
        {
            ExecuteSkill(0);
        }

        #endregion
    }

    /// <summary>
    /// 攻撃判定コライダー
    /// </summary>
    public class AttackCollider : MonoBehaviour
    {
        private AttackInfo currentAttackInfo;
        private BattleCharacterController attackerController;
        private bool isActive = false;

        /// <summary>
        /// 攻撃を有効化
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <param name="attacker">攻撃者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ActivateAttack(AttackInfo attackInfo, BattleCharacterController attacker)
        {
            currentAttackInfo = attackInfo;
            attackerController = attacker;
            isActive = true;
        }

        /// <summary>
        /// 攻撃を無効化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DeactivateAttack()
        {
            isActive = false;
            currentAttackInfo = null;
            attackerController = null;
        }

        /// <summary>
        /// トリガー判定
        /// </summary>
        /// <param name="other">衝突オブジェクト</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnTriggerEnter(Collider other)
        {
            if ( !isActive || attackerController == null )
                return;

            var defender = other.GetComponent<BattleCharacterController>();
            if ( defender != null && defender != attackerController )
            {
                var result = CombatUtilities.CalculateHit(currentAttackInfo, defender);
                defender.ReceiveAttack(result);
                attackerController.OnAttackResult(result);
            }
        }
    }

    /// <summary>
    /// 弾丸コントローラー（簡易版）
    /// </summary>
    public class BulletController : MonoBehaviour
    {
        private AttackInfo attackInfo;
        private BattleCharacterController attackerController;
        private float accuracy;
        private float speed = 20f;
        private float lifeTime = 5f;

        /// <summary>
        /// 弾丸を初期化
        /// </summary>
        /// <param name="info">攻撃情報</param>
        /// <param name="attacker">攻撃者</param>
        /// <param name="aimAccuracy">射撃精度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(AttackInfo info, BattleCharacterController attacker, float aimAccuracy)
        {
            attackInfo = info;
            attackerController = attacker;
            accuracy = aimAccuracy;

            // 一定時間後に自動削除
            Destroy(gameObject, lifeTime);
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }

        /// <summary>
        /// トリガー判定
        /// </summary>
        /// <param name="other">衝突オブジェクト</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnTriggerEnter(Collider other)
        {
            var defender = other.GetComponent<BattleCharacterController>();
            if ( defender != null && defender != attackerController )
            {
                var result = CombatUtilities.CalculateHit(attackInfo, defender);
                defender.ReceiveAttack(result);
                attackerController.OnAttackResult(result);

                Destroy(gameObject);
            }
        }

        #endregion private methods
    }
}