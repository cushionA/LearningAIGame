using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using NaughtyAttributes;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 外部解析用データ - AIが参照する戦闘状況データ
    /// </summary>
    [Serializable]
    public class AnalysisData
    {
        [Header("移動関連")]
        [SerializeField, ReadOnly]
        [Tooltip("現在の移動ベクトル")]
        public Vector3 currentVelocity;

        [SerializeField, ReadOnly]
        [Tooltip("現在の移動速度")]
        public float currentSpeed;

        [SerializeField, ReadOnly]
        [Tooltip("最後のアクションからの経過時間")]
        public float timeSinceLastAction;

        [Header("戦闘状態")]
        [SerializeField, ReadOnly]
        [Tooltip("現在リロード中かどうか")]
        public bool isReloading;

        [SerializeField, ReadOnly]
        [Tooltip("各スキルのクールタイム残り時間")]
        public float[] skillCooldowns = new float[5];

        [SerializeField, ReadOnly]
        [Tooltip("各マニューバのクールタイム残り時間")]
        public float[] maneuverCooldowns = new float[3];

        [SerializeField, ReadOnly]
        [Tooltip("スキルが使用可能かどうか")]
        public bool canUseSkills;

        [SerializeField, ReadOnly]
        [Tooltip("マニューバが使用可能かどうか")]
        public bool canUseManeuvers;

        [SerializeField, ReadOnly]
        [Tooltip("最後に実行した攻撃方向")]
        public AttackDirection lastAttackDirection;

        [Header("射撃関連")]
        [SerializeField, ReadOnly]
        [Tooltip("現在の射撃精度（0.0-1.0）")]
        public float aimingAccuracy;

        [SerializeField, ReadOnly]
        [Tooltip("現在の狙い方向")]
        public Vector3 aimDirection;

        [Header("コンボ情報")]
        [SerializeField, ReadOnly]
        [Tooltip("現在のコンボ段数")]
        public int currentComboCount;

        [SerializeField, ReadOnly]
        [Tooltip("最大コンボ数")]
        public int maxComboCount;

        [SerializeField, ReadOnly]
        [Tooltip("コンボ実行中かどうか")]
        public bool isInCombo;

        [SerializeField, ReadOnly]
        [Tooltip("コンボ受付時間残り")]
        public float comboWindowRemaining;

        [Header("空中戦闘")]
        [SerializeField, ReadOnly]
        [Tooltip("空中にいるかどうか")]
        public bool isAirborne;

        [SerializeField, ReadOnly]
        [Tooltip("空中コンボ中かどうか")]
        public bool isInAerialCombo;

        [SerializeField, ReadOnly]
        [Tooltip("空中滞空時間残り")]
        public float aerialFloatTimeRemaining;

        [SerializeField, ReadOnly]
        [Tooltip("空中時間の合計")]
        public float totalAirTime;

        [Header("回避攻撃")]
        [SerializeField, ReadOnly]
        [Tooltip("回避攻撃が可能かどうか")]
        public bool canDodgeAttack;

        [SerializeField, ReadOnly]
        [Tooltip("最後の回避方向")]
        public Vector3 lastDodgeDirection;

        [SerializeField, ReadOnly]
        [Tooltip("回避攻撃受付時間残り")]
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
        [Header("体力管理")]
        [SerializeField, ReadOnly]
        [Tooltip("現在の体力")]
        public float currentHealth;

        [SerializeField, ReadOnly]
        [Tooltip("最大体力")]
        public float maxHealth;

        [SerializeField, ReadOnly]
        [Tooltip("体力割合")]
        public float healthPercentage;

        [SerializeField, ReadOnly]
        [Tooltip("生存しているかどうか")]
        public bool isAlive;

        [Header("状態管理")]
        [SerializeField, ReadOnly]
        [Tooltip("スタン状態かどうか")]
        public bool isStunned;

        [SerializeField, ReadOnly]
        [Tooltip("怯み状態かどうか")]
        public bool isFlinching;

        [Header("スタンゲージ")]
        [SerializeField, ReadOnly]
        [Tooltip("現在のスタンゲージ蓄積量")]
        public float stunGauge;

        [SerializeField, ReadOnly]
        [Tooltip("スタンゲージの回復速度")]
        public float stunRecoveryRate = 20f;

        [Header("無敵関連")]
        [SerializeField, ReadOnly]
        [Tooltip("無敵状態かどうか")]
        public bool isInvincible;

        [SerializeField, ReadOnly]
        [Tooltip("無敵時間の残り時間")]
        public float invincibilityTimer;

        [Header("生存状態")]
        [SerializeField, ReadOnly]
        [Tooltip("死亡状態かどうか")]
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
        [Header("基本情報")]
        [Tooltip("攻撃の種類")]
        public AttackType attackType;

        [Tooltip("攻撃方向")]
        public AttackDirection direction;

        [Tooltip("攻撃位置")]
        public Vector3 attackerPosition;

        [Tooltip("基本ダメージ量")]

        public float baseDamage;

        [Header("コンボ情報")]
        [Tooltip("コンボ段数")]

        public int comboIndex;

        [Tooltip("空中攻撃かどうか")]
        public bool isAerialAttack;

        [Tooltip("回避攻撃かどうか")]
        public bool isDodgeAttack;

        [Tooltip("コンボフィニッシュかどうか")]
        public bool isComboFinisher;

        [Header("踏み込み情報")]
        [Tooltip("踏み込み距離")]

        public float lungeDistance;

        [Tooltip("踏み込み速度")]

        public float lungeSpeed;

        [Tooltip("踏み込み実行するかどうか")]
        public bool shouldLunge;

        [Header("特殊効果")]
        [Tooltip("スタンゲージに与える蓄積量")]

        public float stunAccumulation;

        [Tooltip("ガード可能かどうか")]
        public bool canBeGuarded = true;

        [Tooltip("ブロッキング可能かどうか")]
        public bool canBeBlocked = true;

        [Tooltip("カウンター攻撃かどうか")]
        public bool isCounterAttack = false;

        [Tooltip("スーパーアーマー付きかどうか")]
        public bool hasSuperArmor = false;

        [Header("エネルギー")]
        [Tooltip("エネルギーダメージ")]

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

            if (isAerial)
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
        [Header("ダメージ情報")]
        [Tooltip("実際に与えられたダメージ")]
        public float actualDamage;

        [Tooltip("スタンゲージ蓄積量")]
        public float stunAccumulation;

        [Tooltip("エネルギーダメージ")]
        public float energyDamage;

        [Header("結果フラグ")]
        [Tooltip("攻撃がヒットしたかどうか")]
        public bool wasHit;

        [Tooltip("ガードされたかどうか")]
        public bool wasGuarded;

        [Tooltip("ブロッキングされたかどうか")]
        public bool wasBlocked;

        [Tooltip("ジャスト回避されたかどうか")]
        public bool wasJustDodged;

        [Tooltip("スタンを引き起こしたかどうか")]
        public bool causedStun;

        [Tooltip("コンボが中断されたかどうか")]
        public bool brokeCombo;

        [Header("位置情報")]
        [Tooltip("ヒット位置")]
        public Vector3 hitPosition;

        [Tooltip("ヒット方向")]
        public Vector3 hitDirection;

        [Header("特殊情報")]
        [Tooltip("空中攻撃だったかどうか")]
        public bool wasAerialAttack;

        [Tooltip("カウンター攻撃だったかどうか")]
        public bool wasCounterAttack;

        [Tooltip("クリティカルヒットだったかどうか")]
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

}
