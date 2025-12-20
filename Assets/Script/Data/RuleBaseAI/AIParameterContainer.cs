using UnityEngine;
using NaughtyAttributes;
using System;
using LLMDataArchitect;

namespace LearningAIGame.CombatSystem.Data
{
    /// <summary>
    /// AI戦術プリセットコレクション
    /// 各戦術タイプごとにパラメーターセットを保持
    /// </summary>
    [CreateAssetMenu(fileName = "AIParameters", menuName = "LearningAIGame/AIParameterContainer")]
    public class AIParameterContainer : ScriptableObject
    {
        [Header("=== 戦術プリセットコレクション ===")]
        [InfoBox("各戦術タイプに対応するパラメーターセットを管理します", EInfoBoxType.Normal)]

        [BoxGroup("攻撃的戦術")]
        [Tooltip("攻撃的戦術のパラメーター")]
        public AIParameter aggressive;

        [BoxGroup("防御的戦術")]
        [Tooltip("防御的戦術のパラメーター")]
        public AIParameter defensive;

        [BoxGroup("バランス型戦術")]
        [Tooltip("バランス型戦術のパラメーター")]
        public AIParameter adaptive;

        [BoxGroup("攪乱型戦術")]
        [Tooltip("攪乱型戦術のパラメーター")]
        public AIParameter disruptive;

        [BoxGroup("持久戦型戦術")]
        [Tooltip("持久戦型戦術のパラメーター")]
        public AIParameter endurance;

        [Header("=== デフォルト戦術データ ===")]
        public StrategyData defaultStrategyData;

        #region プリセット初期化

        [Button("全プリセットを初期化", EButtonEnableMode.Editor)]
        private void InitializeAllPresets()
        {
            InitializeAggressivePreset();
            InitializeDefensivePreset();
            InitializeAdaptivePreset();
            InitializeDisruptivePreset();
            InitializeEndurancePreset();

            Debug.Log("全ての戦術プリセットを初期化しました");
        }

        [Button("攻撃的プリセット初期化")]
        private void InitializeAggressivePreset()
        {
            // 攻撃設定
            aggressive.attackFrequency = 0.8f;
            aggressive.comboAttackProbability = 0.6f;
            aggressive.minAttackInterval = 0.3f;
            aggressive.maxAttackInterval = 1.5f;
            aggressive.opportunityAttackRate = 0.7f;

            // 防御設定
            aggressive.stepUsageRate = 0.6f;
            aggressive.punishRate = 0.5f;

            // エネルギー管理
            aggressive.minEnergyRatio = 0.2f;
            aggressive.lightAttackMinEnergy = 0.3f;
            aggressive.heavyAttackMinEnergy = 0.4f;
            aggressive.rushMinEnergy = 0.15f;
            aggressive.comboMinEnergy = 0.5f;

            // 移動パターン
            aggressive.movementAggressiveness = 0.8f;
            aggressive.preferredCombatDistanceRange = new Vector2(2f, 5f);
            aggressive.dangerDistanceThreshold = 1.5f;
            aggressive.forwardMovementFrequency = 0.7f;
            aggressive.backwardMovementFrequency = 0.1f;
            aggressive.leftMovementFrequency = 0.4f;
            aggressive.rightMovementFrequency = 0.4f;
            aggressive.stanceChangeFrequency = 0.5f;
            aggressive.minStanceChangeInterval = 1.0f;

            Debug.Log("攻撃的プリセットを初期化しました");
        }

        [Button("防御的プリセット初期化")]
        private void InitializeDefensivePreset()
        {
            // 攻撃設定
            defensive.attackFrequency = 0.3f;
            defensive.comboAttackProbability = 0.2f;
            defensive.minAttackInterval = 0.8f;
            defensive.maxAttackInterval = 2.5f;
            defensive.opportunityAttackRate = 0.5f;

            // 防御設定
            defensive.stepUsageRate = 0.7f;
            defensive.punishRate = 0.8f;

            // エネルギー管理
            defensive.minEnergyRatio = 0.4f;
            defensive.lightAttackMinEnergy = 0.5f;
            defensive.heavyAttackMinEnergy = 0.6f;
            defensive.rushMinEnergy = 0.25f;
            defensive.comboMinEnergy = 0.7f;

            // 移動パターン
            defensive.movementAggressiveness = 0.3f;
            defensive.preferredCombatDistanceRange = new Vector2(6f, 10f);
            defensive.dangerDistanceThreshold = 3f;
            defensive.forwardMovementFrequency = 0.2f;
            defensive.backwardMovementFrequency = 0.6f;
            defensive.leftMovementFrequency = 0.3f;
            defensive.rightMovementFrequency = 0.3f;
            defensive.stanceChangeFrequency = 0.3f;
            defensive.minStanceChangeInterval = 2.0f;

            Debug.Log("防御的プリセットを初期化しました");
        }

        [Button("バランス型プリセット初期化")]
        private void InitializeAdaptivePreset()
        {
            // 攻撃設定
            adaptive.attackFrequency = 0.5f;
            adaptive.comboAttackProbability = 0.4f;
            adaptive.minAttackInterval = 0.5f;
            adaptive.maxAttackInterval = 2.0f;
            adaptive.opportunityAttackRate = 0.6f;

            // 防御設定
            adaptive.stepUsageRate = 0.5f;
            adaptive.punishRate = 0.6f;

            // エネルギー管理
            adaptive.minEnergyRatio = 0.3f;
            adaptive.lightAttackMinEnergy = 0.4f;
            adaptive.heavyAttackMinEnergy = 0.5f;
            adaptive.rushMinEnergy = 0.2f;
            adaptive.comboMinEnergy = 0.6f;

            // 移動パターン
            adaptive.movementAggressiveness = 0.5f;
            adaptive.preferredCombatDistanceRange = new Vector2(4f, 7f);
            adaptive.dangerDistanceThreshold = 2f;
            adaptive.forwardMovementFrequency = 0.4f;
            adaptive.backwardMovementFrequency = 0.4f;
            adaptive.leftMovementFrequency = 0.4f;
            adaptive.rightMovementFrequency = 0.4f;
            adaptive.stanceChangeFrequency = 0.5f;
            adaptive.minStanceChangeInterval = 1.5f;

            Debug.Log("バランス型プリセットを初期化しました");
        }

        [Button("攪乱型プリセット初期化")]
        private void InitializeDisruptivePreset()
        {
            // 攻撃設定
            disruptive.attackFrequency = 0.6f;
            disruptive.comboAttackProbability = 0.5f;
            disruptive.minAttackInterval = 0.4f;
            disruptive.maxAttackInterval = 1.8f;
            disruptive.opportunityAttackRate = 0.4f;

            // 防御設定
            disruptive.stepUsageRate = 0.7f;
            disruptive.punishRate = 0.4f;

            // エネルギー管理
            disruptive.minEnergyRatio = 0.25f;
            disruptive.lightAttackMinEnergy = 0.35f;
            disruptive.heavyAttackMinEnergy = 0.45f;
            disruptive.rushMinEnergy = 0.18f;
            disruptive.comboMinEnergy = 0.55f;

            // 移動パターン
            disruptive.movementAggressiveness = 0.8f;
            disruptive.preferredCombatDistanceRange = new Vector2(3f, 8f);
            disruptive.dangerDistanceThreshold = 2f;
            disruptive.forwardMovementFrequency = 0.4f;
            disruptive.backwardMovementFrequency = 0.3f;
            disruptive.leftMovementFrequency = 0.5f;
            disruptive.rightMovementFrequency = 0.5f;
            disruptive.stanceChangeFrequency = 0.7f;
            disruptive.minStanceChangeInterval = 1.0f;

            Debug.Log("攪乱型プリセットを初期化しました");
        }

        [Button("持久戦型プリセット初期化")]
        private void InitializeEndurancePreset()
        {
            // 攻撃設定
            endurance.attackFrequency = 0.4f;
            endurance.comboAttackProbability = 0.25f;
            endurance.minAttackInterval = 0.7f;
            endurance.maxAttackInterval = 2.5f;
            endurance.opportunityAttackRate = 0.6f;

            // 防御設定
            endurance.stepUsageRate = 0.3f;
            endurance.punishRate = 0.7f;

            // エネルギー管理
            endurance.minEnergyRatio = 0.5f;
            endurance.lightAttackMinEnergy = 0.6f;
            endurance.heavyAttackMinEnergy = 0.7f;
            endurance.rushMinEnergy = 0.3f;
            endurance.comboMinEnergy = 0.8f;

            // 移動パターン
            endurance.movementAggressiveness = 0.4f;
            endurance.preferredCombatDistanceRange = new Vector2(5f, 9f);
            endurance.dangerDistanceThreshold = 2.5f;
            endurance.forwardMovementFrequency = 0.3f;
            endurance.backwardMovementFrequency = 0.5f;
            endurance.leftMovementFrequency = 0.35f;
            endurance.rightMovementFrequency = 0.35f;
            endurance.stanceChangeFrequency = 0.3f;
            endurance.minStanceChangeInterval = 2.0f;

            Debug.Log("持久戦型プリセットを初期化しました");
        }

        #region 移動比率正規化

        /// <summary>
        /// 全プリセットの移動方向比率を正規化
        /// </summary>
        [Button("全プリセットの移動比率を正規化", EButtonEnableMode.Editor)]
        private void NormalizeAllMovementFrequencies()
        {
            NormalizeMovementFrequency(aggressive, "攻撃的");
            NormalizeMovementFrequency(defensive, "防御的");
            NormalizeMovementFrequency(adaptive, "バランス型");
            NormalizeMovementFrequency(disruptive, "攪乱型");
            NormalizeMovementFrequency(endurance, "持久戦型");

            Debug.Log("全プリセットの移動方向比率を正規化しました");
        }

        /// <summary>
        /// 単一プリセットの移動方向比率を正規化（合計1.0）
        /// </summary>
        /// <param name="param">対象のAIParameter</param>
        /// <param name="presetName">ログ用プリセット名</param>
        private void NormalizeMovementFrequency(AIParameter param, string presetName)
        {
            if (param == null)
            {
                Debug.LogWarning($"{presetName}プリセットがnullです");
                return;
            }

            float total = param.forwardMovementFrequency
                        + param.backwardMovementFrequency
                        + param.leftMovementFrequency
                        + param.rightMovementFrequency;

            if (total <= 0f)
            {
                Debug.LogWarning($"{presetName}プリセット: 移動比率の合計が0以下です。均等配分に設定します。");
                param.forwardMovementFrequency = 0.25f;
                param.backwardMovementFrequency = 0.25f;
                param.leftMovementFrequency = 0.25f;
                param.rightMovementFrequency = 0.25f;
                return;
            }

            // 正規化（小数3桁で丸め）
            param.forwardMovementFrequency = RoundToThreeDecimals(param.forwardMovementFrequency / total);
            param.backwardMovementFrequency = RoundToThreeDecimals(param.backwardMovementFrequency / total);
            param.leftMovementFrequency = RoundToThreeDecimals(param.leftMovementFrequency / total);
            param.rightMovementFrequency = RoundToThreeDecimals(param.rightMovementFrequency / total);

            // 丸め誤差補正（合計が1.0になるよう最大値で調整）
            float newTotal = param.forwardMovementFrequency
                           + param.backwardMovementFrequency
                           + param.leftMovementFrequency
                           + param.rightMovementFrequency;

            float diff = 1f - newTotal;
            if (Mathf.Abs(diff) > 0.0001f)
            {
                // 最大の要素に誤差を加算
                AdjustLargestFrequency(param, diff);
            }

            Debug.Log($"{presetName}プリセット: 移動比率を正規化 " +
                      $"(前:{param.forwardMovementFrequency:F3} 後:{param.backwardMovementFrequency:F3} " +
                      $"左:{param.leftMovementFrequency:F3} 右:{param.rightMovementFrequency:F3})");
        }

        /// <summary>
        /// 小数3桁で丸める
        /// </summary>
        private float RoundToThreeDecimals(float value)
        {
            return Mathf.Round(value * 1000f) / 1000f;
        }

        /// <summary>
        /// 丸め誤差を最大要素に加算して合計1.0に調整
        /// </summary>
        private void AdjustLargestFrequency(AIParameter param, float diff)
        {
            float max = Mathf.Max(
                param.forwardMovementFrequency,
                param.backwardMovementFrequency,
                param.leftMovementFrequency,
                param.rightMovementFrequency
            );

            if (param.forwardMovementFrequency >= max)
                param.forwardMovementFrequency = RoundToThreeDecimals(param.forwardMovementFrequency + diff);
            else if (param.backwardMovementFrequency >= max)
                param.backwardMovementFrequency = RoundToThreeDecimals(param.backwardMovementFrequency + diff);
            else if (param.leftMovementFrequency >= max)
                param.leftMovementFrequency = RoundToThreeDecimals(param.leftMovementFrequency + diff);
            else
                param.rightMovementFrequency = RoundToThreeDecimals(param.rightMovementFrequency + diff);
        }

        #endregion

        #endregion

        #region ユーティリティメソッド

        /// <summary>
        /// 戦術タイプに対応するパラメーターを取得
        /// </summary>
        public AIParameter GetStrategyParameters(AIParameter.StrategyType type)
        {
            return type switch
            {
                AIParameter.StrategyType.Aggressive => aggressive,
                AIParameter.StrategyType.Defensive => defensive,
                AIParameter.StrategyType.Adaptive => adaptive,
                AIParameter.StrategyType.Disruptive => disruptive,
                AIParameter.StrategyType.Endurance => endurance,
                _ => adaptive // デフォルトはバランス型
            };
        }

        /// <summary>
        /// 戦術タイプ名(文字列)に対応するパラメーターを取得
        /// </summary>
        /// <param name="typeName">戦術タイプ名 ("Aggressive", "Defensive", "Adaptive", "Disruptive", "Endurance")</param>
        /// <returns>対応するAIパラメーター。無効な名前の場合はAdaptive(バランス型)を返す</returns>
        public AIParameter GetStrategyParameters(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                Debug.LogWarning("戦術タイプ名が空です。デフォルト(Adaptive)を返します。");
                return adaptive;
            }

            // 大文字小文字を区別しない比較
            return typeName.ToLower() switch
            {
                "aggressive" => aggressive,
                "defensive" => defensive,
                "adaptive" => adaptive,
                "disruptive" => disruptive,
                "endurance" => endurance,
                _ => HandleInvalidTypeName(typeName)
            };
        }

        /// <summary>
        /// 無効な戦術タイプ名が指定された場合の処理
        /// </summary>
        private AIParameter HandleInvalidTypeName(string typeName)
        {
            Debug.LogWarning($"無効な戦術タイプ名: '{typeName}'。デフォルト(Adaptive)を返します。" +
                           $"\n有効な値: Aggressive, Defensive, Adaptive, Disruptive, Endurance");
            return adaptive;
        }

        [Button("全プリセット情報を出力", EButtonEnableMode.Always)]
        private void LogAllPresets()
        {
            Debug.Log($@"
=== AI戦術プリセットコレクション ===

【攻撃的】
  攻撃頻度: {aggressive.attackFrequency:F2} | 回避使用率: {aggressive.stepUsageRate:F2} | 反撃率: {aggressive.punishRate:F2}
  エネルギー下限: {aggressive.minEnergyRatio:F2} | 好む距離: {aggressive.preferredCombatDistanceRange.x:F1}m～{aggressive.preferredCombatDistanceRange.y:F1}m
  隙攻撃率: {aggressive.opportunityAttackRate:F2}

【防御的】
  攻撃頻度: {defensive.attackFrequency:F2} | 回避使用率: {defensive.stepUsageRate:F2} | 反撃率: {defensive.punishRate:F2}
  エネルギー下限: {defensive.minEnergyRatio:F2} | 好む距離: {defensive.preferredCombatDistanceRange.x:F1}m～{defensive.preferredCombatDistanceRange.y:F1}m
  隙攻撃率: {defensive.opportunityAttackRate:F2}

【バランス型】
  攻撃頻度: {adaptive.attackFrequency:F2} | 回避使用率: {adaptive.stepUsageRate:F2} | 反撃率: {adaptive.punishRate:F2}
  エネルギー下限: {adaptive.minEnergyRatio:F2} | 好む距離: {adaptive.preferredCombatDistanceRange.x:F1}m～{adaptive.preferredCombatDistanceRange.y:F1}m
  隙攻撃率: {adaptive.opportunityAttackRate:F2}

【攪乱型】
  攻撃頻度: {disruptive.attackFrequency:F2} | 回避使用率: {disruptive.stepUsageRate:F2} | 反撃率: {disruptive.punishRate:F2}
  エネルギー下限: {disruptive.minEnergyRatio:F2} | 好む距離: {disruptive.preferredCombatDistanceRange.x:F1}m～{disruptive.preferredCombatDistanceRange.y:F1}m
  隙攻撃率: {disruptive.opportunityAttackRate:F2}

【持久戦型】
  攻撃頻度: {endurance.attackFrequency:F2} | 回避使用率: {endurance.stepUsageRate:F2} | 反撃率: {endurance.punishRate:F2}
  エネルギー下限: {endurance.minEnergyRatio:F2} | 好む距離: {endurance.preferredCombatDistanceRange.x:F1}m～{endurance.preferredCombatDistanceRange.y:F1}m
  隙攻撃率: {endurance.opportunityAttackRate:F2}
            ");
        }

        #endregion
    }
}