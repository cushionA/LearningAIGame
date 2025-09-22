using System;
using UnityEngine;

using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 個別攻撃のモーション設定
    /// </summary>
    [Serializable]
    public class AttackMotionData
    {
        [Header("基本設定")]
        [Tooltip("攻撃名")]
        public string attackName = "Basic Attack";

        [Tooltip("ダメージ")]
        [Range(5f, 200f)]
        public float damage = 25f;

        [Tooltip("発生フレーム（秒）")]
        [Range(0.1f, 2f)]
        public float startupTime = 0.2f;

        [Tooltip("持続フレーム（秒）")]
        [Range(0.1f, 1f)]
        public float activeTime = 0.2f;

        [Tooltip("硬直時間（秒）")]
        [Range(0.1f, 1.5f)]
        public float recoveryTime = 0.3f;

        [Header("踏み込み設定")]
        [Tooltip("踏み込み距離")]
        [Range(0f, 10f)]
        public float lungeDistance = 2f;

        [Tooltip("踏み込み速度")]
        [Range(1f, 20f)]
        public float lungeSpeed = 8f;

        [Tooltip("初段攻撃時のみ踏み込み")]
        public bool lungeOnlyOnFirstHit = true;

        [Header("空中攻撃設定")]
        [Tooltip("空中での威力倍率")]
        [Range(1f, 3f)]
        public float aerialDamageMultiplier = 2f;

        [Tooltip("空中での踏み込み距離倍率")]
        [Range(1f, 2f)]
        public float aerialLungeMultiplier = 1.3f;

        [Tooltip("空中コンボ中の滞空時間")]
        [Range(0.1f, 1f)]
        public float aerialFloatTime = 0.5f;

        [Header("エネルギー")]
        [Tooltip("エネルギー消費量")]
        [Range(0f, 50f)]
        public float energyCost = 5f;

        [Header("特殊効果")]
        [Tooltip("スーパーアーマー付与（初段のみ）")]
        public bool hasSuperArmor = false;

        [Tooltip("ガード不可攻撃")]
        public bool isUnguardable = false;

        [Tooltip("ブロッキング不可攻撃")]
        public bool isUnblockable = false;

        [Tooltip("スタンゲージ蓄積量")]
        [Range(0f, 100f)]
        public float stunAccumulation = 15f;

        /// <summary>
        /// 踏み込みがあるかを示すプロパティ
        /// </summary>
        public bool ShouldLunge
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [Pure]
            get { return lungeDistance > 0f; }
        }

    }

    /// <summary>
    /// 武器のコンボ設定
    /// </summary>
    [Serializable]
    public class ComboSettings
    {
        [Header("コンボ基本設定")]
        [Tooltip("最大連撃数")]
        [Range(1, 10)]
        public int maxComboCount = 3;

        [Tooltip("コンボ受付時間（秒）")]
        [Range(0.1f, 2f)]
        public float comboWindow = 0.8f;

        [Tooltip("コンボリセット時間（秒）")]
        [Range(0.5f, 3f)]
        public float comboResetTime = 2f;

        [Header("弱攻撃チェーン")]
        [Tooltip("弱攻撃のモーション設定")]
        public AttackMotionData[] weakAttackChain = new AttackMotionData[3];

        [Header("強攻撃フィニッシュ")]
        [Tooltip("強攻撃フィニッシュのモーション設定")]
        public AttackMotionData strongFinisher = new AttackMotionData();

        [Header("空中コンボ")]
        [Tooltip("空中コンボのモーション設定")]
        public AttackMotionData[] aerialComboChain = new AttackMotionData[3];

        /// <summary>
        /// 指定したコンボ段数の攻撃データを取得
        /// </summary>
        /// <param name="comboIndex">コンボインデックス</param>
        /// <param name="isAerial">空中攻撃かどうか</param>
        /// <returns>攻撃モーションデータ</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AttackMotionData GetAttackData(int comboIndex, bool isAerial = false)
        {
            var chain = isAerial ? aerialComboChain : weakAttackChain;

            if (comboIndex < 0 || comboIndex >= chain.Length)
                return null;

            return chain[comboIndex];
        }

        /// <summary>
        /// 強攻撃フィニッシュが可能かどうか
        /// </summary>
        /// <param name="currentComboIndex">現在のコンボインデックス</param>
        /// <returns>フィニッシュ可能かどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanFinishWithStrong(int currentComboIndex)
        {
            return currentComboIndex > 0 && strongFinisher != null;
        }
    }

    /// <summary>
    /// 回避攻撃の設定
    /// </summary>
    [Serializable]
    public class DodgeAttackSettings
    {
        [Header("基本設定")]
        [Tooltip("回避攻撃のモーション設定")]
        public AttackMotionData dodgeAttackMotion = new AttackMotionData();

        [Header("踏み込み強化")]
        [Tooltip("基本踏み込み強化倍率")]
        [Range(1f, 3f)]
        public float baseLungeMultiplier = 1.5f;

        [Tooltip("最大踏み込み強化倍率（同方向回避時）")]
        [Range(1.5f, 5f)]
        public float maxLungeMultiplier = 3f;

        [Tooltip("回避攻撃の受付時間（回避後）")]
        [Range(0.1f, 1f)]
        public float dodgeAttackWindow = 0.5f;

        /// <summary>
        /// 回避方向に基づく踏み込み強化度を計算
        /// </summary>
        /// <param name="dodgeDirection">回避方向</param>
        /// <param name="toEnemyDirection">敵への方向</param>
        /// <returns>踏み込み強化倍率</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalculateLungeMultiplier(Vector3 dodgeDirection, Vector3 toEnemyDirection)
        {
            float dot = Vector3.Dot(dodgeDirection.normalized, toEnemyDirection.normalized);
            // cosの値（-1 to 1）を（baseLungeMultiplier to maxLungeMultiplier）にマッピング
            float t = (dot + 1f) * 0.5f; // 0 to 1 の範囲に変換
            return Mathf.Lerp(baseLungeMultiplier, maxLungeMultiplier, t);
        }
    }

    /// <summary>
    /// 武器設定のScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponSettings", menuName = "LearningAIGame/Weapon Settings")]
    public class WeaponSettings : ScriptableObject
    {
        [Header("武器基本情報")]
        [Tooltip("武器名")]
        public string weaponName = "Basic Weapon";

        [Tooltip("武器の説明")]
        [TextArea(2, 4)]
        public string description = "";

        [Header("コンボシステム")]
        [Tooltip("コンボ設定")]
        public ComboSettings comboSettings = new ComboSettings();

        [Header("回避攻撃")]
        [Tooltip("回避攻撃設定")]
        public DodgeAttackSettings dodgeAttackSettings = new DodgeAttackSettings();

        [Header("特殊攻撃")]
        [Tooltip("初段強攻撃のモーション設定")]
        public AttackMotionData initialStrongAttack = new AttackMotionData();

        [Header("武器固有設定")]
        [Tooltip("射程距離")]
        [Range(1f, 8f)]
        public float weaponRange = 3f;

        [Tooltip("武器重量（アニメーション速度に影響）")]
        [Range(0.5f, 2f)]
        public float weaponWeight = 1f;

        [Tooltip("クリティカル率")]
        [Range(0f, 0.3f)]
        public float criticalRate = 0.05f;

        [Tooltip("カウンター攻撃ボーナス")]
        [Range(1f, 2f)]
        public float counterDamageMultiplier = 1.5f;

    }
}
