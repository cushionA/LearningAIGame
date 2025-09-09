using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 外部解析用データ - AIが参照する戦闘状況データ
    /// </summary>
    [Serializable]
    public class AnalysisData
    {
        [Title("移動関連")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の移動ベクトル")]
        public Vector3 currentVelocity;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の移動速度")]
        public float currentSpeed;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("最後のアクションからの経過時間")]
        public float timeSinceLastAction;

        [Title("戦闘状態")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在リロード中かどうか")]
        public bool isReloading;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("各スキルのクールタイム残り時間")]
        public float[] skillCooldowns = new float[5];

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("各マニューバのクールタイム残り時間")]
        public float[] maneuverCooldowns = new float[3];

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("スキルが使用可能かどうか")]
        public bool canUseSkills;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("マニューバが使用可能かどうか")]
        public bool canUseManeuvers;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("最後に実行した攻撃方向")]
        public AttackDirection lastAttackDirection;

        [Title("射撃関連")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の射撃精度（0.0-1.0）")]
        public float aimingAccuracy;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の狙い方向")]
        public Vector3 aimDirection;

        [Title("コンボ情報")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在のコンボ段数")]
        public int currentComboCount;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("最大コンボ数")]
        public int maxComboCount;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("コンボ実行中かどうか")]
        public bool isInCombo;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("コンボ受付時間残り")]
        public float comboWindowRemaining;

        [Title("空中戦闘")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("空中にいるかどうか")]
        public bool isAirborne;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("空中コンボ中かどうか")]
        public bool isInAerialCombo;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("空中滞空時間残り")]
        public float aerialFloatTimeRemaining;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("空中時間の合計")]
        public float totalAirTime;

        [Title("回避攻撃")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("回避攻撃が可能かどうか")]
        public bool canDodgeAttack;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("最後の回避方向")]
        public Vector3 lastDodgeDirection;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("回避攻撃受付時間残り")]
        public float dodgeAttackWindowRemaining;

        /// <summary>
        /// データをリセットする
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            this.currentVelocity = Vector3.zero;
            this.currentSpeed = 0f;
            this.timeSinceLastAction = 0f;
            this.isReloading = false;
            this.canUseSkills = true;
            this.canUseManeuvers = true;
            this.lastAttackDirection = AttackDirection.Up;
            this.aimingAccuracy = 0f;
            this.aimDirection = Vector3.forward;

            // コンボ情報のリセット
            this.currentComboCount = 0;
            this.maxComboCount = 3;
            this.isInCombo = false;
            this.comboWindowRemaining = 0f;

            // 空中戦闘情報のリセット
            this.isAirborne = false;
            this.isInAerialCombo = false;
            this.aerialFloatTimeRemaining = 0f;
            this.totalAirTime = 0f;

            // 回避攻撃情報のリセット
            this.canDodgeAttack = false;
            this.lastDodgeDirection = Vector3.zero;
            this.dodgeAttackWindowRemaining = 0f;

            Array.Clear(this.skillCooldowns, 0, this.skillCooldowns.Length);
            Array.Clear(this.maneuverCooldowns, 0, this.maneuverCooldowns.Length);
        }
    }

    /// <summary>
    /// ヘルス関連データ - 体力・スタン・無敵状態の管理
    /// </summary>
    [Serializable]
    public class HealthData
    {
        [Title("体力管理")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("現在の体力")]
        public float currentHealth;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("最大体力")]
        public float maxHealth;

        [ShowInInspector, ReadOnly]
        [ProgressBar(0, 1)]
        [PropertyTooltip("体力割合")]
        public float healthPercentage;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("生存しているかどうか")]
        public bool isAlive;

        [Title("状態管理")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("スタン状態かどうか")]
        public bool isStunned;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("怯み状態かどうか")]
        public bool isFlinching;

        [Title("スタンゲージ")]
        [ShowInInspector, ReadOnly]
        [ProgressBar(0, 100)]
        [PropertyTooltip("現在のスタンゲージ蓄積量")]
        public float stunGauge;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("スタンゲージの回復速度")]
        public float stunRecoveryRate = 20f;

        [Title("無敵関連")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("無敵状態かどうか")]
        public bool isInvincible;

        [ShowInInspector, ReadOnly]
        [PropertyTooltip("無敵時間の残り時間")]
        public float invincibilityTimer;

        [Title("生存状態")]
        [ShowInInspector, ReadOnly]
        [PropertyTooltip("死亡状態かどうか")]
        public bool isDead;

        /// <summary>
        /// データをリセットする
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            this.currentHealth = 0f;
            this.maxHealth = 0f;
            this.healthPercentage = 0f;
            this.isAlive = true;
            this.isStunned = false;
            this.isFlinching = false;
            this.stunGauge = 0f;
            this.isInvincible = false;
            this.invincibilityTimer = 0f;
            this.isDead = false;
        }
    }

    /// <summary>
    /// 攻撃情報データ - ダメージ計算に使用される情報
    /// </summary>
    [Serializable]
    public class AttackInfo
    {
        [Title("基本情報")]
        [PropertyTooltip("攻撃の種類")]
        public AttackType attackType;

        [PropertyTooltip("攻撃方向")]
        public AttackDirection direction;

        [PropertyTooltip("攻撃位置")]
        public Vector3 attackerPosition;

        [PropertyTooltip("基本ダメージ量")]
        [MinValue(0)]
        public float baseDamage;

        [Title("コンボ情報")]
        [PropertyTooltip("コンボ段数")]
        [MinValue(0)]
        public int comboIndex;

        [PropertyTooltip("空中攻撃かどうか")]
        public bool isAerialAttack;

        [PropertyTooltip("回避攻撃かどうか")]
        public bool isDodgeAttack;

        [PropertyTooltip("コンボフィニッシュかどうか")]
        public bool isComboFinisher;

        [Title("踏み込み情報")]
        [PropertyTooltip("踏み込み距離")]
        [MinValue(0)]
        public float lungeDistance;

        [PropertyTooltip("踏み込み速度")]
        [MinValue(0)]
        public float lungeSpeed;

        [PropertyTooltip("踏み込み実行するかどうか")]
        public bool shouldLunge;

        [Title("特殊効果")]
        [PropertyTooltip("スタンゲージに与える蓄積量")]
        [MinValue(0)]
        public float stunAccumulation;

        [PropertyTooltip("ガード可能かどうか")]
        public bool canBeGuarded = true;

        [PropertyTooltip("ブロッキング可能かどうか")]
        public bool canBeBlocked = true;

        [PropertyTooltip("カウンター攻撃かどうか")]
        public bool isCounterAttack = false;

        [PropertyTooltip("スーパーアーマー付きかどうか")]
        public bool hasSuperArmor = false;

        [Title("エネルギー")]
        [PropertyTooltip("エネルギーダメージ")]
        [MinValue(0)]
        public float energyDamage;

        /// <summary>
        /// 攻撃情報を作成する
        /// </summary>
        /// <param name="type">攻撃種類</param>
        /// <param name="direction">攻撃方向</param>
        /// <param name="damage">ダメージ量</param>
        /// <returns>攻撃情報</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AttackInfo Create(AttackType type, AttackDirection direction, float damage, Vector3 position)
        {
            return new AttackInfo
            {
                attackType = type,
                direction = direction,
                attackerPosition = position,
                baseDamage = damage,
                comboIndex = 0,
                isAerialAttack = false,
                isDodgeAttack = false,
                isComboFinisher = false,
                lungeDistance = 2f,
                lungeSpeed = 8f,
                shouldLunge = true,
                stunAccumulation = damage * 0.5f,
                canBeGuarded = true,
                canBeBlocked = true,
                isCounterAttack = false,
                hasSuperArmor = false,
                energyDamage = 0f
            };
        }

        /// <summary>
        /// コンボ攻撃情報を作成する
        /// </summary>
        /// <param name="type">攻撃種類</param>
        /// <param name="direction">攻撃方向</param>
        /// <param name="damage">ダメージ量</param>
        /// <param name="comboIndex">コンボ段数</param>
        /// <param name="isAerial">空中攻撃かどうか</param>
        /// <returns>攻撃情報</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AttackInfo CreateComboAttack(AttackType type, AttackDirection direction, float damage,
            int comboIndex, Vector3 position, bool isAerial = false)
        {
            var info = Create(type, direction, damage, position);
            info.comboIndex = comboIndex;
            info.isAerialAttack = isAerial;
            info.shouldLunge = comboIndex == 0; // 初段のみ踏み込み

            if ( isAerial )
            {
                info.baseDamage *= 2f; // 空中攻撃は威力2倍
                info.lungeDistance *= 1.3f; // 空中では踏み込み距離延長
            }

            return info;
        }

        /// <summary>
        /// 回避攻撃情報を作成する
        /// </summary>
        /// <param name="type">攻撃種類</param>
        /// <param name="direction">攻撃方向</param>
        /// <param name="damage">ダメージ量</param>
        /// <param name="lungeMultiplier">踏み込み倍率</param>
        /// <returns>攻撃情報</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AttackInfo CreateDodgeAttack(AttackType type, AttackDirection direction, float damage,
            Vector3 position, float lungeMultiplier = 2f)
        {
            var info = Create(type, direction, damage, position);
            info.isDodgeAttack = true;
            info.lungeDistance *= lungeMultiplier; // 回避攻撃は踏み込み強化
            info.shouldLunge = true;

            return info;
        }
    }

    /// <summary>
    /// ダメージ結果データ
    /// </summary>
    [Serializable]
    public class DamageResult
    {
        [Title("ダメージ情報")]
        [PropertyTooltip("実際に与えられたダメージ")]
        public float actualDamage;

        [PropertyTooltip("スタンゲージ蓄積量")]
        public float stunAccumulation;

        [PropertyTooltip("エネルギーダメージ")]
        public float energyDamage;

        [Title("結果フラグ")]
        [PropertyTooltip("攻撃がヒットしたかどうか")]
        public bool wasHit;

        [PropertyTooltip("ガードされたかどうか")]
        public bool wasGuarded;

        [PropertyTooltip("ブロッキングされたかどうか")]
        public bool wasBlocked;

        [PropertyTooltip("ジャスト回避されたかどうか")]
        public bool wasJustDodged;

        [PropertyTooltip("スタンを引き起こしたかどうか")]
        public bool causedStun;

        [PropertyTooltip("コンボが中断されたかどうか")]
        public bool brokeCombo;

        [Title("位置情報")]
        [PropertyTooltip("ヒット位置")]
        public Vector3 hitPosition;

        [PropertyTooltip("ヒット方向")]
        public Vector3 hitDirection;

        [Title("特殊情報")]
        [PropertyTooltip("空中攻撃だったかどうか")]
        public bool wasAerialAttack;

        [PropertyTooltip("カウンター攻撃だったかどうか")]
        public bool wasCounterAttack;

        [PropertyTooltip("クリティカルヒットだったかどうか")]
        public bool wasCriticalHit;

        /// <summary>
        /// 完全回避の結果を作成
        /// </summary>
        /// <returns>回避結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DamageResult CreateMiss()
        {
            return new DamageResult
            {
                actualDamage = 0f,
                stunAccumulation = 0f,
                energyDamage = 0f,
                wasHit = false,
                wasGuarded = false,
                wasBlocked = false,
                wasJustDodged = false,
                causedStun = false,
                brokeCombo = false,
                hitPosition = Vector3.zero,
                hitDirection = Vector3.zero,
                wasAerialAttack = false,
                wasCounterAttack = false,
                wasCriticalHit = false
            };
        }

        /// <summary>
        /// ヒット結果を作成
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <param name="stunValue">スタン蓄積</param>
        /// <param name="hitPos">ヒット位置</param>
        /// <param name="hitDir">ヒット方向</param>
        /// <returns>ヒット結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DamageResult CreateHit(float damage, float stunValue, Vector3 hitPos, Vector3 hitDir)
        {
            return new DamageResult
            {
                actualDamage = damage,
                stunAccumulation = stunValue,
                energyDamage = 0f,
                wasHit = true,
                wasGuarded = false,
                wasBlocked = false,
                wasJustDodged = false,
                causedStun = false,
                brokeCombo = false,
                hitPosition = hitPos,
                hitDirection = hitDir,
                wasAerialAttack = false,
                wasCounterAttack = false,
                wasCriticalHit = false
            };
        }

        /// <summary>
        /// ガード結果を作成
        /// </summary>
        /// <param name="reducedDamage">軽減後ダメージ</param>
        /// <returns>ガード結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DamageResult CreateGuard(float reducedDamage = 0f)
        {
            return new DamageResult
            {
                actualDamage = reducedDamage,
                stunAccumulation = 0f,
                energyDamage = 0f,
                wasHit = true,
                wasGuarded = true,
                wasBlocked = false,
                wasJustDodged = false,
                causedStun = false,
                brokeCombo = false,
                hitPosition = Vector3.zero,
                hitDirection = Vector3.zero,
                wasAerialAttack = false,
                wasCounterAttack = false,
                wasCriticalHit = false
            };
        }

        /// <summary>
        /// ブロッキング結果を作成
        /// </summary>
        /// <returns>ブロッキング結果</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DamageResult CreateBlock()
        {
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
                brokeCombo = true, // ブロッキングはコンボを中断
                hitPosition = Vector3.zero,
                hitDirection = Vector3.zero,
                wasAerialAttack = false,
                wasCounterAttack = false,
                wasCriticalHit = false
            };
        }
    }

    /// <summary>
    /// コンボ状態データ
    /// </summary>
    [Serializable]
    public class ComboStateData
    {
        [Title("基本情報")]
        [PropertyTooltip("現在のコンボ段数")]
        public int currentCount;

        [PropertyTooltip("最大コンボ数")]
        public int maxCount;

        [PropertyTooltip("コンボ実行中かどうか")]
        public bool isActive;

        [Title("タイミング")]
        [PropertyTooltip("コンボ開始時刻")]
        public float startTime;

        [PropertyTooltip("最後の攻撃時刻")]
        public float lastAttackTime;

        [PropertyTooltip("コンボ受付時間")]
        public float comboWindow;

        [PropertyTooltip("コンボリセット時間")]
        public float resetTime;

        [Title("状態")]
        [PropertyTooltip("空中コンボかどうか")]
        public bool isAerialCombo;

        [PropertyTooltip("強攻撃フィニッシュ可能かどうか")]
        public bool canFinishWithStrong;

        [PropertyTooltip("次の攻撃受付中かどうか")]
        public bool isAcceptingInput;

        /// <summary>
        /// コンボをリセット
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            currentCount = 0;
            isActive = false;
            startTime = 0f;
            lastAttackTime = 0f;
            isAerialCombo = false;
            canFinishWithStrong = false;
            isAcceptingInput = false;
        }

        /// <summary>
        /// コンボ受付時間が残っているかチェック
        /// </summary>
        /// <param name="currentTime">現在時刻</param>
        /// <returns>受付可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWithinWindow(float currentTime)
        {
            return isActive && (currentTime - lastAttackTime) <= comboWindow;
        }

        /// <summary>
        /// コンボがタイムアウトしたかチェック
        /// </summary>
        /// <param name="currentTime">現在時刻</param>
        /// <returns>タイムアウトしたかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTimedOut(float currentTime)
        {
            return isActive && (currentTime - lastAttackTime) > resetTime;
        }
    }
}
