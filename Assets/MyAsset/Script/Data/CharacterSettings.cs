using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using Sirenix.OdinInspector;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 移動システムの設定
    /// </summary>
    [Serializable]
    public class MovementSettings
    {
        [Title("基本移動")]
        [PropertyTooltip("歩行速度")]
        [Range(1f, 10f)]
        public float walkSpeed = 5f;

        [PropertyTooltip("安全距離（この距離以上離れていると安全と判断）")]
        [Range(5f, 20f)]
        public float safeDistance = 10f;

        [Title("ジャンプ")]
        [PropertyTooltip("通常ジャンプ力")]
        [Range(5f, 15f)]
        public float jumpForce = 8f;

        [PropertyTooltip("チャージジャンプ力")]
        [Range(10f, 25f)]
        public float chargedJumpForce = 12f;

        [PropertyTooltip("チャージ時間")]
        [Range(0.5f, 3f)]
        public float chargeTime = 1.5f;

        [Title("ブースト")]
        [PropertyTooltip("ブースト速度")]
        [Range(10f, 30f)]
        public float boostSpeed = 20f;

        [PropertyTooltip("ブーストエネルギー消費率（秒あたり）")]
        [Range(10f, 50f)]
        public float boostEnergyConsumption = 25f;

        [Title("回避")]
        [PropertyTooltip("回避距離")]
        [Range(2f, 8f)]
        public float dodgeDistance = 5f;

        [PropertyTooltip("回避エネルギー消費")]
        [Range(5f, 25f)]
        public float dodgeEnergyCost = 15f;

        [PropertyTooltip("二段回避エネルギー消費")]
        [Range(15f, 40f)]
        public float doubleDodgeEnergyCost = 30f;

        [PropertyTooltip("回避後のガード不可時間")]
        [Range(0.1f, 1f)]
        public float postDodgeVulnerabilityTime = 0.5f;

        [Title("空中制御")]
        [PropertyTooltip("二段ジャンプエネルギー消費")]
        [Range(5f, 20f)]
        public float airJumpEnergyCost = 10f;

        [PropertyTooltip("空中チャージエネルギー消費率")]
        [Range(10f, 30f)]
        public float airChargeEnergyConsumption = 20f;

        [PropertyTooltip("空中での最大滞空時間")]
        [Range(2f, 10f)]
        public float maxAirTime = 5f;

        [PropertyTooltip("空中移動速度倍率")]
        [Range(0.5f, 1.5f)]
        public float airMobilityMultiplier = 0.8f;
    }

    /// <summary>
    /// 攻撃システムの設定
    /// </summary>
    [Serializable]
    public class AttackSettings
    {
        [Title("近接攻撃範囲")]
        [PropertyTooltip("近接攻撃が有効な距離")]
        [Range(1f, 5f)]
        public float meleeRange = 3f;

        [Title("基本攻撃")]
        [PropertyTooltip("弱攻撃のダメージ")]
        [Range(10f, 50f)]
        public float weakAttackDamage = 25f;

        [PropertyTooltip("弱攻撃の発生フレーム")]
        [Range(0.1f, 0.5f)]
        public float weakAttackStartup = 0.2f;

        [PropertyTooltip("弱攻撃のエネルギー消費")]
        [Range(0f, 10f)]
        public float weakAttackEnergyCost = 5f;

        [PropertyTooltip("強攻撃のダメージ")]
        [Range(30f, 100f)]
        public float strongAttackDamage = 60f;

        [PropertyTooltip("強攻撃の発生フレーム")]
        [Range(0.3f, 1f)]
        public float strongAttackStartup = 0.5f;

        [PropertyTooltip("強攻撃のエネルギー消費")]
        [Range(10f, 30f)]
        public float strongAttackEnergyCost = 20f;

        [PropertyTooltip("強攻撃キャンセル時の追加消費")]
        [Range(5f, 15f)]
        public float strongAttackCancelCost = 10f;

        [Title("空中攻撃")]
        [PropertyTooltip("空中攻撃の威力倍率")]
        [Range(1.5f, 3f)]
        public float aerialDamageMultiplier = 2f;

        [PropertyTooltip("空中攻撃の踏み込み距離倍率")]
        [Range(1f, 2f)]
        public float aerialLungeMultiplier = 1.3f;

        [PropertyTooltip("空中コンボ中の滞空時間")]
        [Range(0.2f, 2f)]
        public float aerialComboFloatTime = 0.8f;

        [Title("踏み込み設定")]
        [PropertyTooltip("基本踏み込み距離")]
        [Range(0.5f, 5f)]
        public float baseLungeDistance = 2f;

        [PropertyTooltip("強攻撃踏み込み距離倍率")]
        [Range(1f, 3f)]
        public float strongLungeMultiplier = 1.5f;

        [PropertyTooltip("初段のみ踏み込み")]
        public bool lungeOnlyOnFirstHit = true;

        [Title("コンボ設定")]
        [PropertyTooltip("コンボ受付時間")]
        [Range(0.2f, 2f)]
        public float comboWindow = 0.8f;

        [PropertyTooltip("コンボリセット時間")]
        [Range(1f, 5f)]
        public float comboResetTime = 2f;

        [Title("回避攻撃")]
        [PropertyTooltip("回避攻撃の基本踏み込み倍率")]
        [Range(1.2f, 3f)]
        public float dodgeAttackBaseLunge = 1.5f;

        [PropertyTooltip("回避攻撃の最大踏み込み倍率")]
        [Range(2f, 5f)]
        public float dodgeAttackMaxLunge = 3f;

        [PropertyTooltip("回避攻撃の受付時間")]
        [Range(0.1f, 1f)]
        public float dodgeAttackWindow = 0.5f;

        [Title("射撃")]
        [PropertyTooltip("弱射撃のダメージ")]
        [Range(5f, 25f)]
        public float weakShootDamage = 15f;

        [PropertyTooltip("強射撃のダメージ")]
        [Range(40f, 120f)]
        public float strongShootDamage = 80f;

        [PropertyTooltip("射撃スキルのエネルギー消費")]
        [Range(15f, 40f)]
        public float shootSkillEnergyCost = 25f;

        [Title("偏差射撃")]
        [PropertyTooltip("最大精度到達時間")]
        [Range(0.5f, 3f)]
        public float maxAccuracyTime = 1.5f;

        [PropertyTooltip("精度向上速度")]
        [Range(0.5f, 2f)]
        public float accuracyGainRate = 1f;
    }

    /// <summary>
    /// 防御システムの設定
    /// </summary>
    [Serializable]
    public class DefenseSettings
    {
        [Title("ガード")]
        [PropertyTooltip("ガード成功時のエネルギー回復ボーナス時間")]
        [Range(1f, 5f)]
        public float guardEnergyBonusTime = 3f;

        [PropertyTooltip("ガード成功時のエネルギー回復倍率")]
        [Range(1.5f, 3f)]
        public float guardEnergyBonusMultiplier = 2f;

        [Title("ブロッキング")]
        [PropertyTooltip("ブロッキング成功時のエネルギー回復量")]
        [Range(10f, 30f)]
        public float blockEnergyRecovery = 20f;

        [PropertyTooltip("ブロッキング失敗時のダメージ増加率")]
        [Range(1.2f, 2f)]
        public float blockFailDamageMultiplier = 1.5f;

        [PropertyTooltip("ブロッキング失敗時のエネルギー消費")]
        [Range(5f, 15f)]
        public float blockFailEnergyCost = 10f;

        [PropertyTooltip("ブロッキング成功時の移動距離（射撃に対して）")]
        [Range(3f, 10f)]
        public float blockMoveDistance = 6f;

        [Title("ジャスト回避")]
        [PropertyTooltip("ジャスト回避の判定ウィンドウ")]
        [Range(0.05f, 0.3f)]
        public float justDodgeWindow = 0.15f;
    }

    /// <summary>
    /// エネルギーシステムの設定
    /// </summary>
    [Serializable]
    public class EnergySettings
    {
        [Title("基本設定")]
        [PropertyTooltip("最大エネルギー量")]
        [Range(50f, 200f)]
        public float maxEnergy = 100f;

        [PropertyTooltip("通常時のエネルギー回復速度")]
        [Range(10f, 40f)]
        public float normalRecoveryRate = 25f;

        [PropertyTooltip("エネルギー切れ時の高速回復速度")]
        [Range(30f, 80f)]
        public float fastRecoveryRate = 50f;

        [Title("回復阻害")]
        [PropertyTooltip("弱攻撃被弾時の回復停止時間")]
        [Range(0.1f, 1f)]
        public float weakHitRecoveryPause = 0.3f;

        [PropertyTooltip("強攻撃被弾時の回復停止時間")]
        [Range(1f, 3f)]
        public float strongHitRecoveryPause = 2f;
    }

    /// <summary>
    /// キャラクター全体の設定を管理するScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterSettings", menuName = "LearningAIGame/Character Settings")]
    public class CharacterSettings : ScriptableObject
    {
        [Title("基本パラメータ")]
        [ValidateInput("ValidateHealth", "体力は0より大きい必要があります")]
        [Range(100f, 1000f)]
        [PropertyTooltip("最大体力")]
        public float maxHealth = 500f;

        [Title("武器設定")]
        [PropertyTooltip("装備する武器の設定")]
        [InlineEditor(InlineEditorModes.LargePreview)]
        public WeaponSettings weaponSettings;

        [Title("システム設定")]
        [InlineEditor(InlineEditorModes.LargePreview)]
        [PropertyTooltip("移動システムの設定")]
        public MovementSettings movement = new MovementSettings();

        [InlineEditor(InlineEditorModes.LargePreview)]
        [PropertyTooltip("攻撃システムの設定")]
        public AttackSettings attack = new AttackSettings();

        [InlineEditor(InlineEditorModes.LargePreview)]
        [PropertyTooltip("防御システムの設定")]
        public DefenseSettings defense = new DefenseSettings();

        [InlineEditor(InlineEditorModes.LargePreview)]
        [PropertyTooltip("エネルギーシステムの設定")]
        public EnergySettings energy = new EnergySettings();

        [Title("エネルギーバリアモード")]
        [PropertyTooltip("スタンゲージの最大値")]
        [Range(50f, 150f)]
        public float maxStunGauge = 100f;

        [PropertyTooltip("スタンゲージの回復速度")]
        [Range(10f, 40f)]
        public float stunGaugeRecoveryRate = 25f;

        /// <summary>
        /// 最大体力の妥当性を検証
        /// </summary>
        /// <param name="health">検証する体力値</param>
        /// <returns>妥当性</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ValidateHealth(float health) => health > 0;

        /// <summary>
        /// 武器設定を含む総合的な攻撃力を計算
        /// </summary>
        /// <param name="baseAttackType">基本攻撃タイプ</param>
        /// <param name="comboIndex">コンボインデックス</param>
        /// <param name="isAerial">空中攻撃かどうか</param>
        /// <returns>最終攻撃力</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalculateFinalDamage(AttackType baseAttackType, int comboIndex = 0, bool isAerial = false)
        {
            float baseDamage = baseAttackType switch
            {
                AttackType.WeakMelee => attack.weakAttackDamage,
                AttackType.StrongMelee => attack.strongAttackDamage,
                _ => attack.weakAttackDamage
            };

            // 武器設定からのダメージ補正
            if (weaponSettings?.comboSettings != null)
            {
                var attackData = weaponSettings.comboSettings.GetAttackData(comboIndex, isAerial);
                if (attackData != null)
                {
                    baseDamage = attackData.damage;
                }
            }

            // 空中攻撃補正
            if (isAerial)
            {
                baseDamage *= attack.aerialDamageMultiplier;
            }

            return baseDamage;
        }

        /// <summary>
        /// 踏み込み距離を計算
        /// </summary>
        /// <param name="attackType">攻撃タイプ</param>
        /// <param name="comboIndex">コンボインデックス</param>
        /// <param name="isAerial">空中攻撃かどうか</param>
        /// <param name="isDodgeAttack">回避攻撃かどうか</param>
        /// <param name="dodgeDirection">回避方向（回避攻撃時）</param>
        /// <param name="toEnemyDirection">敵への方向（回避攻撃時）</param>
        /// <returns>最終踏み込み距離</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float CalculateLungeDistance(AttackType attackType, int comboIndex = 0, bool isAerial = false, 
            bool isDodgeAttack = false, Vector3 dodgeDirection = default, Vector3 toEnemyDirection = default)
        {
            float baseLunge = attack.baseLungeDistance;

            // 武器設定からの踏み込み距離
            if (weaponSettings?.comboSettings != null)
            {
                var attackData = weaponSettings.comboSettings.GetAttackData(comboIndex, isAerial);
                if (attackData != null)
                {
                    baseLunge = attackData.lungeDistance;
                    
                    // 初段のみ踏み込み設定のチェック
                    if (attackData.lungeOnlyOnFirstHit && comboIndex > 0)
                    {
                        return 0f;
                    }
                }
            }

            // 強攻撃の踏み込み倍率
            if (attackType == AttackType.StrongMelee)
            {
                baseLunge *= attack.strongLungeMultiplier;
            }

            // 空中攻撃の踏み込み倍率
            if (isAerial)
            {
                baseLunge *= attack.aerialLungeMultiplier;
            }

            // 回避攻撃の踏み込み強化
            if (isDodgeAttack && weaponSettings?.dodgeAttackSettings != null)
            {
                float multiplier = weaponSettings.dodgeAttackSettings.CalculateLungeMultiplier(dodgeDirection, toEnemyDirection);
                baseLunge *= multiplier;
            }

            return baseLunge;
        }

        /// <summary>
        /// 設定の妥当性を検証
        /// </summary>
        /// <returns>妥当性</returns>
        [Button("設定検証実行", ButtonSizes.Large)]
        [GUIColor(0.7f, 1f, 0.7f)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ValidateSettings()
        {
            var isValid = true;
            var report = new System.Text.StringBuilder();

            // 基本設定の検証
            if (this.maxHealth <= 0)
            {
                report.AppendLine("❌ 最大体力が0以下です");
                isValid = false;
            }

            // エネルギー設定の検証
            if (this.energy.maxEnergy < 50f)
            {
                report.AppendLine("❌ 最大エネルギーが50未満です");
                isValid = false;
            }

            if (this.energy.fastRecoveryRate <= this.energy.normalRecoveryRate)
            {
                report.AppendLine("⚠️ 高速回復速度が通常回復速度以下です");
            }

            // 攻撃設定の検証
            if (this.attack.strongAttackDamage <= this.attack.weakAttackDamage)
            {
                report.AppendLine("⚠️ 強攻撃ダメージが弱攻撃以下です");
            }

            // 武器設定の検証
            if (this.weaponSettings == null)
            {
                report.AppendLine("⚠️ 武器設定が未設定です");
            }

            // 空中攻撃設定の検証
            if (this.attack.aerialDamageMultiplier < 1f)
            {
                report.AppendLine("⚠️ 空中攻撃倍率が1未満です");
            }

            if (isValid)
            {
                report.AppendLine("✅ 全ての設定が妥当です");
            }

            Debug.Log($"設定検証結果:\n{report}");
            return isValid;
        }

        [Title("プリセット機能")]
        [HorizontalGroup("プリセット")]
        [Button("攻撃特化")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetAttackPreset()
        {
            this.attack.weakAttackDamage *= 1.3f;
            this.attack.strongAttackDamage *= 1.3f;
            this.attack.weakAttackStartup *= 0.8f;
            this.attack.strongAttackStartup *= 0.8f;
            this.attack.aerialDamageMultiplier *= 1.2f;
            this.energy.maxEnergy *= 0.9f;
        }

        [HorizontalGroup("プリセット")]
        [Button("防御特化")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetDefensePreset()
        {
            this.maxHealth *= 1.3f;
            this.defense.guardEnergyBonusMultiplier *= 1.5f;
            this.defense.blockEnergyRecovery *= 1.3f;
            this.energy.normalRecoveryRate *= 1.2f;
        }

        [HorizontalGroup("プリセット")]
        [Button("機動特化")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetMobilityPreset()
        {
            this.movement.boostSpeed *= 1.3f;
            this.movement.dodgeDistance *= 1.2f;
            this.movement.boostEnergyConsumption *= 0.8f;
            this.movement.dodgeEnergyCost *= 0.8f;
            this.movement.airMobilityMultiplier *= 1.2f;
            this.energy.maxEnergy *= 1.2f;
        }

        [HorizontalGroup("プリセット")]
        [Button("空中特化")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetAerialPreset()
        {
            this.attack.aerialDamageMultiplier *= 1.3f;
            this.attack.aerialLungeMultiplier *= 1.2f;
            this.attack.aerialComboFloatTime *= 1.5f;
            this.movement.airJumpEnergyCost *= 0.7f;
            this.movement.maxAirTime *= 1.5f;
        }

        [HorizontalGroup("プリセット")]
        [Button("バランス")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetBalancedPreset()
        {
            this.movement = new MovementSettings();
            this.attack = new AttackSettings();
            this.defense = new DefenseSettings();
            this.energy = new EnergySettings();
        }
    }
}
