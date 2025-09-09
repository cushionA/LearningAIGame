using System;
using UnityEngine;
using Sirenix.OdinInspector;
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
        [Title("基本設定")]
        [PropertyTooltip("攻撃名")]
        public string attackName = "Basic Attack";

        [PropertyTooltip("ダメージ")]
        [Range(5f, 200f)]
        public float damage = 25f;

        [PropertyTooltip("発生フレーム（秒）")]
        [Range(0.1f, 2f)]
        public float startupTime = 0.2f;

        [PropertyTooltip("持続フレーム（秒）")]
        [Range(0.1f, 1f)]
        public float activeTime = 0.2f;

        [PropertyTooltip("硬直時間（秒）")]
        [Range(0.1f, 1.5f)]
        public float recoveryTime = 0.3f;

        [Title("踏み込み設定")]
        [PropertyTooltip("踏み込み距離")]
        [Range(0f, 10f)]
        public float lungeDistance = 2f;

        [PropertyTooltip("踏み込み速度")]
        [Range(1f, 20f)]
        public float lungeSpeed = 8f;

        [PropertyTooltip("初段攻撃時のみ踏み込み")]
        public bool lungeOnlyOnFirstHit = true;

        [Title("空中攻撃設定")]
        [PropertyTooltip("空中での威力倍率")]
        [Range(1f, 3f)]
        public float aerialDamageMultiplier = 2f;

        [PropertyTooltip("空中での踏み込み距離倍率")]
        [Range(1f, 2f)]
        public float aerialLungeMultiplier = 1.3f;

        [PropertyTooltip("空中コンボ中の滞空時間")]
        [Range(0.1f, 1f)]
        public float aerialFloatTime = 0.5f;

        [Title("エネルギー")]
        [PropertyTooltip("エネルギー消費量")]
        [Range(0f, 50f)]
        public float energyCost = 5f;

        [Title("特殊効果")]
        [PropertyTooltip("スーパーアーマー付与（初段のみ）")]
        public bool hasSuperArmor = false;

        [PropertyTooltip("ガード不可攻撃")]
        public bool isUnguardable = false;

        [PropertyTooltip("ブロッキング不可攻撃")]
        public bool isUnblockable = false;

        [PropertyTooltip("スタンゲージ蓄積量")]
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
        [Title("コンボ基本設定")]
        [PropertyTooltip("最大連撃数")]
        [Range(1, 10)]
        public int maxComboCount = 3;

        [PropertyTooltip("コンボ受付時間（秒）")]
        [Range(0.1f, 2f)]
        public float comboWindow = 0.8f;

        [PropertyTooltip("コンボリセット時間（秒）")]
        [Range(0.5f, 3f)]
        public float comboResetTime = 2f;

        [Title("弱攻撃チェーン")]
        [PropertyTooltip("弱攻撃のモーション設定")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "attackName")]
        public AttackMotionData[] weakAttackChain = new AttackMotionData[3];

        [Title("強攻撃フィニッシュ")]
        [PropertyTooltip("強攻撃フィニッシュのモーション設定")]
        public AttackMotionData strongFinisher = new AttackMotionData();

        [Title("空中コンボ")]
        [PropertyTooltip("空中コンボのモーション設定")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "attackName")]
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

            if ( comboIndex < 0 || comboIndex >= chain.Length )
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
        [Title("基本設定")]
        [PropertyTooltip("回避攻撃のモーション設定")]
        public AttackMotionData dodgeAttackMotion = new AttackMotionData();

        [Title("踏み込み強化")]
        [PropertyTooltip("基本踏み込み強化倍率")]
        [Range(1f, 3f)]
        public float baseLungeMultiplier = 1.5f;

        [PropertyTooltip("最大踏み込み強化倍率（同方向回避時）")]
        [Range(1.5f, 5f)]
        public float maxLungeMultiplier = 3f;

        [PropertyTooltip("回避攻撃の受付時間（回避後）")]
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
        [Title("武器基本情報")]
        [PropertyTooltip("武器名")]
        public string weaponName = "Basic Weapon";

        [PropertyTooltip("武器カテゴリ")]
        public WeaponCategory category = WeaponCategory.Balanced;

        [PropertyTooltip("武器の説明")]
        [TextArea(2, 4)]
        public string description = "";

        [Title("コンボシステム")]
        [InlineEditor(InlineEditorModes.LargePreview)]
        [PropertyTooltip("コンボ設定")]
        public ComboSettings comboSettings = new ComboSettings();

        [Title("回避攻撃")]
        [InlineEditor(InlineEditorModes.LargePreview)]
        [PropertyTooltip("回避攻撃設定")]
        public DodgeAttackSettings dodgeAttackSettings = new DodgeAttackSettings();

        [Title("特殊攻撃")]
        [PropertyTooltip("初段強攻撃のモーション設定")]
        public AttackMotionData initialStrongAttack = new AttackMotionData();

        [Title("武器固有設定")]
        [PropertyTooltip("射程距離")]
        [Range(1f, 8f)]
        public float weaponRange = 3f;

        [PropertyTooltip("武器重量（アニメーション速度に影響）")]
        [Range(0.5f, 2f)]
        public float weaponWeight = 1f;

        [PropertyTooltip("クリティカル率")]
        [Range(0f, 0.3f)]
        public float criticalRate = 0.05f;

        [PropertyTooltip("カウンター攻撃ボーナス")]
        [Range(1f, 2f)]
        public float counterDamageMultiplier = 1.5f;

        /// <summary>
        /// 武器重量に基づくアニメーション速度倍率を取得
        /// </summary>
        /// <returns>アニメーション速度倍率</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetAnimationSpeedMultiplier()
        {
            return 1f / weaponWeight;
        }

        /// <summary>
        /// カテゴリに基づくデフォルトコンボ設定を生成
        /// </summary>
        [Button("カテゴリ別デフォルト設定", ButtonSizes.Large)]
        [GUIColor(0.7f, 1f, 0.7f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateDefaultSettings()
        {
            switch ( category )
            {
                case WeaponCategory.Fast:
                    SetupFastWeaponDefaults();
                    break;
                case WeaponCategory.Balanced:
                    SetupBalancedWeaponDefaults();
                    break;
                case WeaponCategory.Power:
                    SetupPowerWeaponDefaults();
                    break;
                case WeaponCategory.Reach:
                    SetupReachWeaponDefaults();
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupFastWeaponDefaults()
        {
            comboSettings.maxComboCount = 5;
            weaponWeight = 0.7f;
            weaponRange = 2.5f;
            criticalRate = 0.15f;

            // 高速連撃用の設定
            for ( int i = 0; i < comboSettings.weakAttackChain.Length; i++ )
            {
                if ( comboSettings.weakAttackChain[i] == null )
                    comboSettings.weakAttackChain[i] = new AttackMotionData();

                comboSettings.weakAttackChain[i].damage = 20f + i * 5f;
                comboSettings.weakAttackChain[i].startupTime = 0.15f;
                comboSettings.weakAttackChain[i].lungeDistance = 1.5f;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupBalancedWeaponDefaults()
        {
            comboSettings.maxComboCount = 3;
            weaponWeight = 1f;
            weaponRange = 3f;
            criticalRate = 0.08f;

            // バランス型の設定
            for ( int i = 0; i < comboSettings.weakAttackChain.Length; i++ )
            {
                if ( comboSettings.weakAttackChain[i] == null )
                    comboSettings.weakAttackChain[i] = new AttackMotionData();

                comboSettings.weakAttackChain[i].damage = 25f + i * 8f;
                comboSettings.weakAttackChain[i].startupTime = 0.2f;
                comboSettings.weakAttackChain[i].lungeDistance = 2f;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupPowerWeaponDefaults()
        {
            comboSettings.maxComboCount = 2;
            weaponWeight = 1.5f;
            weaponRange = 3.5f;
            criticalRate = 0.12f;

            // パワー型の設定
            for ( int i = 0; i < comboSettings.weakAttackChain.Length; i++ )
            {
                if ( comboSettings.weakAttackChain[i] == null )
                    comboSettings.weakAttackChain[i] = new AttackMotionData();

                comboSettings.weakAttackChain[i].damage = 35f + i * 15f;
                comboSettings.weakAttackChain[i].startupTime = 0.3f;
                comboSettings.weakAttackChain[i].lungeDistance = 2.5f;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetupReachWeaponDefaults()
        {
            comboSettings.maxComboCount = 3;
            weaponWeight = 1.2f;
            weaponRange = 5f;
            criticalRate = 0.06f;

            // リーチ型の設定
            for ( int i = 0; i < comboSettings.weakAttackChain.Length; i++ )
            {
                if ( comboSettings.weakAttackChain[i] == null )
                    comboSettings.weakAttackChain[i] = new AttackMotionData();

                comboSettings.weakAttackChain[i].damage = 30f + i * 10f;
                comboSettings.weakAttackChain[i].startupTime = 0.25f;
                comboSettings.weakAttackChain[i].lungeDistance = 3.5f;
            }
        }

        [Title("設定検証")]
        [Button("設定検証実行", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.8f, 1f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateSettings()
        {
            var report = new System.Text.StringBuilder();
            var isValid = true;

            // コンボ設定の検証
            if ( comboSettings.maxComboCount <= 0 )
            {
                report.AppendLine("❌ 最大コンボ数が0以下です");
                isValid = false;
            }

            if ( comboSettings.weakAttackChain == null || comboSettings.weakAttackChain.Length == 0 )
            {
                report.AppendLine("❌ 弱攻撃チェーンが設定されていません");
                isValid = false;
            }

            // 武器範囲の検証
            if ( weaponRange <= 0 )
            {
                report.AppendLine("❌ 武器射程が0以下です");
                isValid = false;
            }

            if ( isValid )
            {
                report.AppendLine("✅ 全ての設定が妥当です");
            }

            Debug.Log($"武器設定検証結果 [{weaponName}]:\n{report}");
        }
    }
}
