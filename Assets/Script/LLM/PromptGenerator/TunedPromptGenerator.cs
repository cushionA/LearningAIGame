using System;
using System.Text;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// TunedPromptGenerator
// 
//
// 概要: LLM用の戦術判断プロンプトを生成するメインクラス（英語版・最適化済み）
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 戦闘データを解析し、LLMが戦術判断を行うための構造化プロンプトを生成する。
// JSON Schema Grammarでパース失敗を防止。
// 
// **重要な変更点:**
// - フィードバックセクションに「前回の BasicTactic」を明示
// - これにより LLM が前回の決定を正確に把握し、適切な継続/変更判断が可能になる
// 
// 基底クラス: PromptGeneratorBase
// 入力データ: LLMInputData (StateSystem, ActionLog, StrategyResult)
// 出力形式: 構造化された英語プロンプト + JSON Schema
// 
// プロンプト構成:
// 1. Current Battle State - HP/Energy差分と状況評価
// 2. Enemy Attack Patterns - 直近ターン+累積データ
// 3. Enemy Defense Patterns - 直近ターン+累積データ
// 4. Previous Turn Results - ダメージ収支とパフォーマンス評価
// 5. Performance Feedback - 前回戦術の詳細フィードバック（★前回のBasicTacticを含む）
// 6. Tactical Guidelines - 状況別推奨戦術
// 7. Output Format - JSON形式の厳密な指定
// 
// その他:
// バランス調整用の定数で動作調整可能
//=====================================================================================================================

namespace LLMDataArchitect.Test
{
    /// <summary>
    /// 英語版プロンプト生成クラス（最適化版・Cache8互換）
    /// </summary>
    public class TunedPromptGenerator : PromptGeneratorBase
    {
        #region バランス調整用定数

        // === 状況評価の閾値 ===
        /// <summary>圧倒的優位と判定するHP差</summary>
        private const float k_HpDominantThreshold = 50f;

        /// <summary>優位と判定するHP差</summary>
        private const float k_HpAdvantageThreshold = 20f;

        /// <summary>劣勢と判定するHP差</summary>
        private const float k_HpDisadvantageThreshold = -20f;

        /// <summary>危機的状況と判定するHP差</summary>
        private const float k_HpCriticalThreshold = -50f;

        /// <summary>圧倒的優位と判定するエネルギー差</summary>
        private const float k_EnergyDominantThreshold = 20f;

        /// <summary>優位と判定するエネルギー差</summary>
        private const float k_EnergyAdvantageThreshold = 30f;

        /// <summary>劣勢と判定するエネルギー差</summary>
        private const float k_EnergyDisadvantageThreshold = -30f;

        /// <summary>危機的状況と判定するエネルギー差</summary>
        private const float k_EnergyCriticalThreshold = -20f;

        /// <summary>互角と判定するHP差の上限</summary>
        private const float k_HpEvenThreshold = 20f;

        /// <summary>互角と判定するエネルギー差の上限</summary>
        private const float k_EnergyEvenThreshold = 20f;

        // === パフォーマンス評価の閾値 ===
        /// <summary>大成功と判定するダメージ収支</summary>
        private const float k_PerformanceHighlySuccessfulThreshold = 50f;

        /// <summary>成功と判定するダメージ収支</summary>
        private const float k_PerformanceSuccessfulThreshold = 20f;

        /// <summary>大失敗と判定するダメージ収支</summary>
        private const float k_PerformanceMajorFailureThreshold = -50f;

        /// <summary>失敗と判定するダメージ収支</summary>
        private const float k_PerformanceFailureThreshold = -20f;

        #endregion

        #region キャッシュされた固定プロンプト

        /// <summary>
        /// 固定プロンプトセクション（Output Format Requirements）のキャッシュ
        /// 初回生成後は再利用する
        /// </summary>
        private string _cachedFixedSection = null;

        #endregion

        /// <summary>
        /// 実際の戦闘データからプロンプトを生成する
        /// </summary>
        /// <param name="inputData"></param>
        /// <returns></returns>
        public override string GeneratePromptByData(LLMInputData inputData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# Combat AI Tactical Decision");
            prompt.AppendLine("Analyze the current battle state and select optimal tactics.");
            prompt.AppendLine();

            //// === 戦術ガイドライン ===
            //prompt.AppendLine("# Tactical Decision Guidelines");
            //prompt.AppendLine();

            //prompt.AppendLine("## When DOMINANT/ADVANTAGE:");
            //prompt.AppendLine("- 'Aggressive': Use for offensive strategy to maximize damage output");
            //prompt.AppendLine("- 'Disruptive': Use to break through strong defenses or disrupt enemy tactics");
            //prompt.AppendLine("- Goal: Finish the battle quickly with high-damage attacks");
            //prompt.AppendLine();

            //prompt.AppendLine("## When EVENLY MATCHED:");
            //prompt.AppendLine("- 'Adaptive': Remain flexible and adjust tactics based on situation");
            //prompt.AppendLine("- Goal: Create opportunities through varied tactics and analysis");
            //prompt.AppendLine();

            //prompt.AppendLine("## When DISADVANTAGE/CRITICAL:");
            //prompt.AppendLine("- 'Defensive': Use when HP is critically low (survival priority)");
            //prompt.AppendLine("- 'Endurance': Use when energy is depleted (conservation strategy)");
            //prompt.AppendLine("- Goal: Survive and recover until conditions improve");

            // === 現在の状況 ===
            prompt.AppendLine("## 1. Current Battle State");

            var myHp = inputData.PlayerData.Hp;
            var enemyHp = inputData.NPCData.Hp;
            var hpDiff = myHp - enemyHp;
            var myEnergy = inputData.PlayerData.Energy;
            var enemyEnergy = inputData.NPCData.Energy;
            var energyDiff = myEnergy - enemyEnergy;

            // HP差を明確に表現
            string hpComparison = hpDiff > 0
                ? $"(You have {hpDiff} more)"
                : hpDiff < 0
                    ? $"(Enemy has {Math.Abs(hpDiff)} more)"
                    : "(Equal)";

            // エネルギー差を明確に表現
            string energyComparison = energyDiff > 0
                ? $"(You have {energyDiff} more)"
                : energyDiff < 0
                    ? $"(Enemy has {Math.Abs(energyDiff)} more)"
                    : "(Equal)";

            prompt.AppendLine($"**Your HP**: {myHp}  |  **Enemy HP**: {enemyHp}  {hpComparison}");
            prompt.AppendLine($"**Your Energy**: {myEnergy}  |  **Enemy Energy**: {enemyEnergy}  {energyComparison}");

            // 状況評価
            string situationTag = EvaluateSituation(hpDiff, energyDiff);
            prompt.AppendLine();
            prompt.AppendLine($"**Situation Assessment**: {situationTag}");
            prompt.AppendLine();

            // === 敵攻撃パターン ===
            prompt.AppendLine("## 2. Enemy Attack Patterns");

            bool hasRecentAttackData = false;
            if (inputData.PlayerLog.HitSituations != null && inputData.PlayerLog.HitSituations.Count > 0)
            {
                Span<HitSituation> hitSpan = inputData.PlayerLog.HitSituations.AsSpan();

                int lightAttackCount = 0;
                int strongAttackCount = 0;
                int strongAttackCancelCount = 0;

                foreach (var situation in hitSpan)
                {
                    switch (situation.HitState)
                    {
                        case ActionState.弱攻撃:
                            lightAttackCount++;
                            break;
                        case ActionState.強攻撃:
                            strongAttackCount++;
                            break;
                        case ActionState.強攻撃キャンセル:
                            strongAttackCancelCount++;
                            break;
                    }
                }

                hasRecentAttackData = (lightAttackCount + strongAttackCount + strongAttackCancelCount) > 0;

                if (hasRecentAttackData)
                {
                    prompt.AppendLine($"**Recent Turns**: Heavy {strongAttackCount}×, Light {lightAttackCount}×, Feint {strongAttackCancelCount}×");
                }
            }

            if (!hasRecentAttackData)
            {
                prompt.AppendLine("**Recent Turns**: No attacks (refer to historical data)");
            }

            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"**Historical Pattern**: Heavy {inputData.ActionLog.HeavyAttackPercentage * 100:F0}%, Light {inputData.ActionLog.LightAttackPercentage * 100:F0}%, Feint {inputData.ActionLog.HeavyAttackCancelPercentage * 100:F0}%");
            }

            prompt.AppendLine();

            // === 敵防御パターン ===
            prompt.AppendLine("## 3. Enemy Defense Patterns");

            bool hasRecentDefenseData = false;
            if (inputData.PlayerLog.DamageSituations != null && inputData.PlayerLog.DamageSituations.Count > 0)
            {
                Span<HitSituation> damageSpan = inputData.PlayerLog.DamageSituations.AsSpan();

                int horizontalDodgeCount = 0;
                int backwardDodgeCount = 0;
                int blockingCount = 0;
                int guardCount = 0;

                foreach (var situation in damageSpan)
                {
                    switch (situation.DamageState)
                    {
                        case ActionState.横回避:
                            horizontalDodgeCount++;
                            break;
                        case ActionState.後ろ回避:
                            backwardDodgeCount++;
                            break;
                        case ActionState.ブロッキング成功:
                            blockingCount++;
                            break;
                        case ActionState.ガード成功:
                            guardCount++;
                            break;
                    }
                }

                hasRecentDefenseData = (horizontalDodgeCount + backwardDodgeCount + blockingCount + guardCount) > 0;

                if (hasRecentDefenseData)
                {
                    prompt.AppendLine($"**Recent Turns**: Parry {blockingCount}×, Guard {guardCount}×, Counter {horizontalDodgeCount}×, Dodge {backwardDodgeCount}×");
                }
            }

            if (!hasRecentDefenseData)
            {
                prompt.AppendLine("**Recent Turns**: No defenses (refer to historical data)");
            }

            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"**Historical Pattern**: Counter {inputData.ActionLog.HorizontalDodgeAttackPercentage * 100:F0}%, Parry {inputData.ActionLog.BlockingPercentage * 100:F0}%, Dodge {(inputData.ActionLog.BackwardDodgePercentage + inputData.ActionLog.HorizontalDodgePercentage) * 100:F0}%");
            }
            prompt.AppendLine();

            // === 前回結果 ===
            prompt.AppendLine("## 4. Previous Turn Results");

            float dealtDmg = inputData.NPCLog.HitDamage;
            float takenDmg = inputData.NPCLog.TakeDamage;
            float balance = dealtDmg - takenDmg;

            string performanceTag = EvaluatePerformance(balance);

            prompt.AppendLine($"**Damage Dealt**: {dealtDmg:F0}  |  **Damage Taken**: {takenDmg:F0}  |  **Net Balance**: {balance:+0;-0;0}");
            prompt.AppendLine($"**Performance**: {performanceTag}");
            prompt.AppendLine();

            // === フィードバック（★Cache8互換：前回のBasicTacticを含む） ===
            prompt.AppendLine("## 5. Performance Feedback on Last Decision");
            prompt.AppendLine();

            if (inputData.CurrentStrategy != null)
            {
                var result = inputData.StrategyResult;

                // ★重要: 前回の決定を明示（Cache8の成功要因）
                prompt.AppendLine("**Previous Turn Decision:**");
                prompt.AppendLine($"- BasicTactic: {inputData.CurrentStrategy.BasicTactic}");
                prompt.AppendLine($"- AttackCriteria: {inputData.CurrentStrategy.AttackCriteria}");
                prompt.AppendLine($"- ContinuousAttackCriteria: {inputData.CurrentStrategy.ContinuousAttackCriteria}");
                prompt.AppendLine($"- DefenseCriteria: {inputData.CurrentStrategy.DefenseCriteria}");
                prompt.AppendLine($"- ContinuousDefenseCriteria: {inputData.CurrentStrategy.ContinuousDefenseCriteria}");
                prompt.AppendLine();

                // パフォーマンス評価（前回の基準名を明示）
                prompt.AppendLine("**Performance Results:**");
                var evaluations = result.GetAllConditionEvaluationsEnglish(
                    inputData.CurrentStrategy.AttackCriteria,
                    inputData.CurrentStrategy.ContinuousAttackCriteria,
                    inputData.CurrentStrategy.DefenseCriteria,
                    inputData.CurrentStrategy.ContinuousDefenseCriteria
                );

                prompt.AppendLine(evaluations);
                prompt.AppendLine();


                // === 戦術ガイドライン ===
                prompt.AppendLine("### Tactical Decision Guidelines");
                prompt.AppendLine();

                prompt.AppendLine("#### When DOMINANT/ADVANTAGE:");
                prompt.AppendLine("- 'Aggressive': Use for offensive strategy to maximize damage output");
                prompt.AppendLine("- 'Disruptive': Use to break through strong defenses or disrupt enemy tactics");
                prompt.AppendLine("- Goal: Finish the battle quickly with high-damage attacks");
                prompt.AppendLine();

                prompt.AppendLine("#### When EVENLY MATCHED:");
                prompt.AppendLine("- 'Adaptive': Remain flexible and adjust tactics based on situation");
                prompt.AppendLine("- Goal: Create opportunities through varied tactics and analysis");
                prompt.AppendLine();

                prompt.AppendLine("#### When DISADVANTAGE/CRITICAL:");
                prompt.AppendLine("- 'Defensive': Use when HP is critically low (survival priority)");
                prompt.AppendLine("- 'Endurance': Use when energy is depleted (conservation strategy)");
                prompt.AppendLine("- **Priority: When HP is low, prioritize 'Defensive' over 'Endurance'**");
                prompt.AppendLine("- Goal: Survive and recover until conditions improve");

                // フィードバックルール
                prompt.AppendLine("### [IMPORTANT] MANDATORY FEEDBACK RULES");
                prompt.AppendLine();
                prompt.AppendLine("**Rule 1: Keep What Works**");
                prompt.AppendLine("If any criteria shows \"Highly Effective\" (Success >> Failure):");
                prompt.AppendLine("→ You MUST use the EXACT SAME criteria again");
                prompt.AppendLine("→ Example: Last turn 'DefenseCriteria: Cumulative Probability' = Highly Effective");
                prompt.AppendLine("→ This turn 'DefenseCriteria: Cumulative Probability' ← KEEP IT");
                prompt.AppendLine();
                prompt.AppendLine("**Rule 2: Change What Fails**");
                prompt.AppendLine("If any criteria shows \"Must Change\" (Failure > Success):");
                prompt.AppendLine("→ You MUST select a DIFFERENT criteria from the available options");
                prompt.AppendLine("→ Example: Last turn 'AttackCriteria: Speed Priority' = Must Change");
                prompt.AppendLine("→ This turn 'AttackCriteria: Return Priority' ← MUST BE DIFFERENT");
                prompt.AppendLine();
                prompt.AppendLine("**Rule 3: Slightly Adjust Weak Effects**");
                prompt.AppendLine("If criteria shows \"Weak Effect\" or \"Acceptable\":");
                prompt.AppendLine("→ You MAY keep or change based on overall situation");
            }
            else
            {
                prompt.AppendLine("(First turn - no previous feedback available)");
            }
            prompt.AppendLine();

            return prompt.ToString();
        }

        /// <summary>
        /// ランダムなテストプロンプトを生成
        /// </summary>
        /// <returns></returns>
        public override string GenerateRandomPrompt()
        {
            var randomSituation = (TestSituationType)UnityEngine.Random.Range(0, 5);
            var inputData = LLMInputData.CreateForTestSituation(randomSituation);
            return GeneratePromptByData(inputData);
        }

        #region プライベートヘルパーメソッド

        /// <summary>
        /// HP差とエネルギー差から戦況を評価
        /// </summary>
        /// <param name="hpDiff">HP差（自分 - 敵）</param>
        /// <param name="energyDiff">エネルギー差（自分 - 敵）</param>
        /// <returns>状況評価タグ</returns>
        private string EvaluateSituation(float hpDiff, float energyDiff)
        {
            if (hpDiff > k_HpDominantThreshold && energyDiff > k_EnergyDominantThreshold)
                return "【DOMINANT POSITION】";
            else if (hpDiff > k_HpAdvantageThreshold || energyDiff > k_EnergyAdvantageThreshold)
                return "【ADVANTAGE】";
            else if (hpDiff < k_HpCriticalThreshold && energyDiff < k_EnergyCriticalThreshold)
                return "【CRITICAL DANGER】";
            else if (hpDiff < k_HpDisadvantageThreshold || energyDiff < k_EnergyDisadvantageThreshold)
                return "【DISADVANTAGE】";
            else if (Math.Abs(hpDiff) <= k_HpEvenThreshold && Math.Abs(energyDiff) <= k_EnergyEvenThreshold)
                return "【EVENLY MATCHED】";

            return "【ADVANTAGE】"; // デフォルト
        }

        /// <summary>
        /// ダメージ収支からパフォーマンスを評価
        /// </summary>
        /// <param name="balance">ダメージ収支（与ダメージ - 被ダメージ）</param>
        /// <returns>パフォーマンス評価タグ</returns>
        private string EvaluatePerformance(float balance)
        {
            if (balance > k_PerformanceHighlySuccessfulThreshold)
                return "【HIGHLY SUCCESSFUL】";
            else if (balance > k_PerformanceSuccessfulThreshold)
                return "【SUCCESSFUL】";
            else if (balance < k_PerformanceMajorFailureThreshold)
                return "【MAJOR FAILURE】";
            else if (balance < k_PerformanceFailureThreshold)
                return "【FAILURE】";
            else
                return "【STALEMATE】";
        }

        #endregion

        /// <summary>
        /// 固定プロンプトセクション（Output Format Requirements）を取得
        /// キャッシュが存在する場合はそれを返し、存在しない場合は生成してキャッシュする
        /// </summary>
        /// <returns>固定プロンプトセクション</returns>
        private string GetFixedSectionEnglish()
        {
            // キャッシュがあればそれを返す
            if (_cachedFixedSection != null)
            {
                return _cachedFixedSection;
            }

            // キャッシュがない場合は生成してキャッシュ
            _cachedFixedSection = GenerateFixedSection();
            return _cachedFixedSection;
        }

        /// <summary>
        /// 固定プロンプトセクション（Output Format Requirements）を生成
        /// ファインチューニング済みモデル用の最小限システムプロンプト
        /// モデルに焼き付けた戦術知識を活用し、出力形式とルールのみを指定
        /// このメソッドは初回のみ実行され、結果はキャッシュされる
        /// </summary>
        /// <returns>生成された固定プロンプトセクション</returns>
        public override string GenerateFixedSection()
        {
            var prompt = new StringBuilder();

            // === システムプロンプトとしてのロール定義 ===
            prompt.AppendLine("You are a tactical combat AI advisor. Analyze battle situations and provide strategic decisions in strict JSON format.");
            prompt.AppendLine();

            // === ファインチューニング済み知識の活用指示 ===
            prompt.AppendLine("You have been fine-tuned with comprehensive combat tactics knowledge. Apply your trained understanding of:");
            prompt.AppendLine("- BasicTactic types and their strategic applications");
            prompt.AppendLine("- Attack/Defense criteria and their optimal usage contexts");
            prompt.AppendLine("- Situational decision-making (dominant/even/disadvantaged scenarios)");
            prompt.AppendLine("- Performance feedback interpretation and adaptation");
            prompt.AppendLine();
            prompt.AppendLine("USE YOUR TRAINED KNOWLEDGE - do not ask for clarification on tactical terminology.");
            prompt.AppendLine();


            return prompt.ToString();
        }

        /// <summary>
        /// グラマーを返すメソッド
        /// </summary>
        /// <returns></returns>
        public override string GenerateGrammar()
        {
            return @"{
  ""type"": ""object"",
  ""properties"": {
    ""AnalysisResult"": {
      ""type"": ""string"",
      ""maxLength"": 200
    },
    ""BasicTactic"": {
      ""type"": ""string"",
      ""enum"": [""Aggressive"", ""Defensive"", ""Adaptive"", ""Disruptive"", ""Endurance""]
    },
    ""AttackCriteria"": {
      ""type"": ""string"",
      ""enum"": [""Cumulative Probability"", ""Recent Pattern Focus"", ""Speed Priority"", ""Return Priority"", ""Feint Focus"", ""Dispersion Focus"", ""Energy Efficiency""]
    },
    ""ContinuousAttackCriteria"": {
      ""type"": ""string"",
      ""enum"": [""Cumulative Probability"", ""Recent Pattern Focus"", ""Speed Priority"", ""Return Priority"", ""Feint Focus"", ""Dispersion Focus"", ""Energy Efficiency""]
    },
    ""DefenseCriteria"": {
      ""type"": ""string"",
      ""enum"": [""Cumulative Probability"", ""Recent Pattern Focus"", ""Counterattack Focus"", ""Return Priority"", ""Risk Avoidance"", ""Evasive Counter Priority"", ""Dispersion Focus""]
    },
    ""ContinuousDefenseCriteria"": {
      ""type"": ""string"",
      ""enum"": [""Cumulative Probability"", ""Recent Pattern Focus"", ""Counterattack Focus"", ""Return Priority"", ""Risk Avoidance"", ""Evasive Counter Priority"", ""Dispersion Focus""]
    }
  },
  ""required"": [""AnalysisResult"", ""BasicTactic"", ""AttackCriteria"", ""ContinuousAttackCriteria"", ""DefenseCriteria"", ""ContinuousDefenseCriteria""],
  ""additionalProperties"": false
}";
        }
    }
}