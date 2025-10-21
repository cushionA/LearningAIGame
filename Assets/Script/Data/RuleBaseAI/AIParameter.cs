using UnityEngine;
using NaughtyAttributes;
using System;

namespace LearningAIGame.CombatSystem.Data
{
    /// <summary>
    /// AI戦術パラメーターを定義するScriptableObject
    /// 基本戦術の種類ごとに攻撃頻度、エネルギー管理、移動パターンなどを設定
    /// </summary>
    public class AIParameter
    {
        #region 基本戦術タイプ

        /// <summary>
        /// 基本戦術タイプの列挙型
        /// </summary>
        public enum StrategyType : byte
        {
            Aggressive,      // 攻撃的
            Defensive,       // 防御的
            Adaptive,        // バランス型
            Disturbance,     // 攪乱
            Endurance        // エネルギー重視持久戦
        }

        /// <summary>
        /// 移動タイプ列挙型
        /// </summary>
        public enum MovementType
        {
            None,
            Forward,    // 前進
            Backward,   // 後退
            Left,    // 左移動
            Right   // 右移動
        }

        #endregion

        #region 攻撃パラメーター

        [Header("=== 攻撃設定 ===")]
        [InfoBox("攻撃に関する動作パラメーターを設定します", EInfoBoxType.Normal)]

        [Tooltip("攻撃頻度 (0: 消極的 ~ 1: 非常に積極的)")]
        [Range(0f, 1f)]
        [OnValueChanged("ValidateAttackFrequency")]
        public float attackFrequency = 0.5f;

        [Tooltip("連続攻撃を行う確率 (0: 単発のみ ~ 1: 必ず連続攻撃)")]
        [Range(0f, 1f)]
        [ShowIf("IsAggressiveOrAdaptive")]
        public float comboAttackProbability = 0.3f;

        [Space(10)]
        [Header("攻撃タイミング制御")]

        [Tooltip("攻撃判断の最小間隔（秒）")]
        [MinValue(0.1f)]
        public float minAttackInterval = 0.5f;

        [Tooltip("攻撃判断の最大間隔（秒）")]
        [MinValue(0.1f)]
        [ValidateInput("ValidateMaxAttackInterval", "最大間隔は最小間隔より大きい必要があります")]
        public float maxAttackInterval = 2.0f;

        [Tooltip("敵の隙を見つけたら攻撃をする確率")]
        public float opportunityAttackRate = 0f;

        #endregion

        #region 防御パラメーター

        [Header("=== 防御設定 ===")]
        [InfoBox("防御に関する動作パラメーターを設定します", EInfoBoxType.Normal)]

        [Tooltip("回避の使用率 (0: 使わない ~ 1: 回避優先)")]
        [Range(0f, 1f)]
        public float stepUsageRate = 0.5f;

        [Tooltip("確定反撃率 (0: 反撃しない ~ 1: 反撃優先)")]
        [Range(0f, 1f)]
        public float punishRate = 0.5f;

        #endregion

        #region エネルギー管理

        [Header("=== エネルギー管理 ===")]
        [InfoBox("エネルギーの使用と回復に関する設定", EInfoBoxType.Normal)]

        [Tooltip("維持するエネルギー率の下限 (0 ~ 1)")]
        [Range(0f, 1f)]
        [ProgressBar("エネルギー下限", 1f, EColor.Blue)]
        public float minEnergyRatio = 0.3f;

        [Space(10)]
        [Header("エネルギー消費判断")]

        [Tooltip("弱攻撃を使用する最低エネルギー率")]
        [Range(0.2f, 0.8f)]
        public float lightAttackMinEnergy = 0.5f;

        [Tooltip("強攻撃を使用する最低エネルギー率")]
        [Range(0.2f, 0.8f)]
        public float heavyAttackMinEnergy = 0.5f;

        [Tooltip("前回避行動を使用する最低エネルギー率")]
        [Range(0.1f, 0.5f)]
        public float rushMinEnergy = 0.2f;

        [Tooltip("連続攻撃を行う最低エネルギー率")]
        [Range(0.3f, 0.9f)]
        [ShowIf("IsAggressiveOrAdaptive")]
        public float comboMinEnergy = 0.6f;

        #endregion

        #region 移動パターン

        [Header("=== 移動パターン ===")]
        [InfoBox("AIの移動戦略と距離管理の設定", EInfoBoxType.Normal)]

        [Tooltip("移動の積極性 (0: ほとんど動かない ~ 1: 常に動き回る)")]
        [Range(0f, 1f)]
        public float movementAggressiveness = 0.5f;

        [Tooltip("好む交戦距離（メートル）")]
        [MinMaxSlider(1f, 15f)]
        public Vector2 preferredCombatDistanceRange = new Vector2(4f, 7f);

        [Tooltip("危険距離の閾値（メートル）この距離より近いと離脱行動を取る")]
        [Range(0.5f, 5f)]
        [ShowIf("IsDefensiveOrEndurance")]
        public float dangerDistanceThreshold = 2f;

        [Space(10)]
        [Header("移動パターン詳細")]

        [Tooltip("前進移動の頻度 (0: 使わない ~ 1: 頻繁に使う)")]
        [Range(0f, 1f)]
        [ShowIf("IsAggressiveOrDisturbance")]
        public float forwardMovementFrequency = 0.6f;

        [Tooltip("後退移動の頻度 (0: 使わない ~ 1: 頻繁に使う)")]
        [Range(0f, 1f)]
        [ShowIf("IsDefensiveOrEndurance")]
        public float backwardMovementFrequency = 0.7f;

        [Tooltip("左移動の頻度 (0: 使わない ~ 1: 頻繁に使う)")]
        [Range(0f, 1f)]
        [ShowIf("IsDisturbanceOrAdaptive")]
        public float leftMovementFrequency = 0.5f;

        [Tooltip("右の頻度 (0: 使わない ~ 1: 頻繁に使う)")]
        [Range(0f, 1f)]
        [ShowIf("IsDisturbanceOrAdaptive")]
        public float rightMovementFrequency = 0.3f;

        [Space(10)]
        [Header("スタンス変更")]

        [Tooltip("構え変更の頻度 (0: 固定 ~ 1: 頻繁に変更)")]
        [Range(0f, 1f)]
        public float stanceChangeFrequency = 0.4f;

        [Tooltip("構え変更の最小間隔（秒）")]
        [MinValue(0.5f)]
        public float minStanceChangeInterval = 1.5f;

        #endregion

        #region 戦術調整パラメーター

        [Header("=== 戦術調整 ===")]
        [InfoBox("試合状況に応じた戦術調整の設定", EInfoBoxType.Warning)]

        [Tooltip("体力劣勢時の攻撃性変化 (-1: 守備的 ~ 1: 攻撃的)")]
        [Range(-1f, 1f)]
        public float lowHealthAggressivenessModifier = -0.3f;

        [Tooltip("体力優勢時の攻撃性変化 (-1: 守備的 ~ 1: 攻撃的)")]
        [Range(-1f, 1f)]
        public float highHealthAggressivenessModifier = 0.2f;

        [Tooltip("エネルギー劣勢時の行動変化率 (0: 変化なし ~ 1: 大きく変化)")]
        [Range(0f, 1f)]
        [ShowIf("IsEnduranceOrAdaptive")]
        public float lowEnergyBehaviorChangeRate = 0.5f;

        [Space(10)]
        [Header("適応性設定")]

        [Tooltip("連続でダメージを受けた後の戦術変更（何回連続で被弾したら戦術を変えるか）")]
        [Range(2, 10)]
        public int damageThresholdForTacticChange = 3;

        [Tooltip("戦術パターンのランダム性 (0: 固定 ~ 1: 完全ランダム)")]
        [Range(0f, 1f)]
        public float tacticRandomness = 0.2f;

        [Tooltip("敵の行動パターンへの適応速度 (0: 適応しない ~ 1: 即座に適応)")]
        [Range(0f, 1f)]
        [ShowIf("IsAdaptive")]
        public float adaptationSpeed = 0.6f;

        #endregion

        #region バリデーション

        /// <summary>
        /// 最大攻撃間隔の妥当性をチェック
        /// </summary>
        private bool ValidateMaxAttackInterval(float value)
        {
            return value > minAttackInterval;
        }

        /// <summary>
        /// Unity標準のバリデーション
        /// </summary>
        private void OnValidate()
        {
            // 最小間隔が最大間隔より大きくならないように調整
            if (minAttackInterval > maxAttackInterval)
            {
                maxAttackInterval = minAttackInterval + 0.1f;
            }

            // 距離範囲の妥当性チェック
            if (preferredCombatDistanceRange.x > preferredCombatDistanceRange.y)
            {
                preferredCombatDistanceRange.y = preferredCombatDistanceRange.x + 1f;
            }
        }

        #endregion

        #region ゲッタープロパティ・判定メソッド

        /// <summary>
        /// 次回の攻撃判断までの時間を取得（秒）
        /// </summary>
        public float GetNextAttackDelay()
        {
            return UnityEngine.Random.Range(minAttackInterval, maxAttackInterval);
        }

        /// <summary>
        /// 現在の設定で攻撃を実行すべきかランダム判定
        /// </summary>
        public bool ShouldAttack()
        {
            return UnityEngine.Random.value < attackFrequency;
        }

        /// <summary>
        /// 連続攻撃を実行すべきかランダム判定
        /// </summary>
        public bool ShouldComboAttack()
        {
            return UnityEngine.Random.value < comboAttackProbability;
        }

        /// <summary>
        /// 移動を実行すべきかランダム判定
        /// </summary>
        public bool ShouldMove()
        {
            return UnityEngine.Random.value < movementAggressiveness;
        }

        /// <summary>
        /// 回避を実行すべきかランダム判定
        /// </summary>
        public bool ShouldStep()
        {
            return UnityEngine.Random.value < stepUsageRate;
        }

        /// <summary>
        /// 敵の隙に攻撃を実行すべきかランダム判定
        /// </summary>
        public bool ShouldOpportunityAttack()
        {
            return UnityEngine.Random.value < opportunityAttackRate;
        }

        /// <summary>
        /// 確定反撃を実行すべきかランダム判定
        /// </summary>
        public bool ShouldPunish()
        {
            return UnityEngine.Random.value < punishRate;
        }

        /// <summary>
        /// 現在の距離が好む交戦距離範囲内かチェック
        /// </summary>
        /// <param name="currentDistance">現在の敵との距離</param>
        /// <returns>範囲内ならtrue</returns>
        public bool IsInPreferredRange(float currentDistance)
        {
            return currentDistance >= preferredCombatDistanceRange.x &&
                   currentDistance <= preferredCombatDistanceRange.y;
        }

        /// <summary>
        /// 現在の二乗距離が好む交戦距離範囲かチェック（最適化版）
        /// </summary>
        /// <param name="currentDistanceSqr">現在の敵との二乗距離</param>
        /// <param name="result">距離の状態 (適正:0 / 近すぎ:1 / 遠すぎ:-1)</param>
        /// <returns>適正範囲内ならtrue、範囲外ならfalse</returns>
        public bool CheckPreferredRangeSqr(float currentDistanceSqr, out int result)
        {
            float minSqr = preferredCombatDistanceRange.x * preferredCombatDistanceRange.x;
            float maxSqr = preferredCombatDistanceRange.y * preferredCombatDistanceRange.y;

            if (currentDistanceSqr < minSqr)
            {
                result = 1;   // 近すぎる
                return false;
            }
            else if (currentDistanceSqr > maxSqr)
            {
                result = -1;  // 遠すぎる
                return false;
            }
            else
            {
                result = 0;   // 適正範囲内
                return true;
            }
        }

        /// <summary>
        /// 現在の距離が危険距離以下かチェック
        /// </summary>
        /// <param name="currentDistance">現在の敵との距離</param>
        /// <returns>危険距離以下ならtrue</returns>
        public bool IsInDangerRange(float currentDistance)
        {
            return currentDistance <= dangerDistanceThreshold;
        }

        /// <summary>
        /// 現在の二乗距離が危険距離以下かチェック（最適化版）
        /// </summary>
        /// <param name="currentDistanceSqr">現在の敵との二乗距離</param>
        /// <returns>危険距離以下ならtrue</returns>
        public bool IsInDangerRangeSqr(float currentDistanceSqr)
        {
            float dangerSqr = dangerDistanceThreshold * dangerDistanceThreshold;
            return currentDistanceSqr <= dangerSqr;
        }

        /// <summary>
        /// 好む交戦距離の最大値の二乗を取得（最適化版）
        /// </summary>
        /// <returns>最大距離の二乗</returns>
        public float GetPreferredMaxDistanceSqr()
        {
            return preferredCombatDistanceRange.y * preferredCombatDistanceRange.y;
        }

        /// <summary>
        /// 移動方向を決定（前進/後退/横移動/円運動）
        /// </summary>
        /// <returns>決定された移動タイプ</returns>
        public MovementType DecideMovementType()
        {
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;

            cumulative += forwardMovementFrequency;
            if (roll < cumulative)
                return MovementType.Forward;

            cumulative += backwardMovementFrequency;
            if (roll < cumulative)
                return MovementType.Backward;

            cumulative += leftMovementFrequency;
            if (roll < cumulative)
                return MovementType.Left;

            cumulative += rightMovementFrequency;
            if (roll < cumulative)
                return MovementType.Right;

            return MovementType.None;
        }

        #endregion

        #region デバッグ機能

        [Button("設定値の整合性チェック", EButtonEnableMode.Always)]
        private void ValidateAllSettings()
        {
            bool isValid = true;
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("=== 設定値検証レポート ===");

            // 距離設定チェック
            if (preferredCombatDistanceRange.x > preferredCombatDistanceRange.y)
            {
                report.AppendLine("⚠ エラー: 好む交戦距離の設定が不正です");
                isValid = false;
            }

            // 攻撃間隔チェック
            if (minAttackInterval > maxAttackInterval)
            {
                report.AppendLine("⚠ エラー: 攻撃間隔の設定が不正です");
                isValid = false;
            }

            if (isValid)
            {
                report.AppendLine("✓ すべての設定が正常です");
            }

            Debug.Log(report.ToString());
        }

        [Button("現在の設定をログ出力", EButtonEnableMode.Always)]
        private void LogCurrentSettings()
        {
            Debug.Log($@"
=== AI戦術パラメーター ===
攻撃頻度: {attackFrequency:F2}
最低エネルギー率: {minEnergyRatio:F2}
好む交戦距離: {preferredCombatDistanceRange.x:F1}m ~ {preferredCombatDistanceRange.y:F1}m
移動積極性: {movementAggressiveness:F2}
            ");
        }

        #endregion
    }
}