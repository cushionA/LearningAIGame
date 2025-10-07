using System;
using System.Linq;
using System.Text;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;
using static LLMDataArchitect.ActionTable;

namespace LLMDataArchitect.Test
{
    /// <summary>
    /// 英語版プロンプト生成クラス
    /// </summary>
    public class EnglishPromptGenerator : PromptGeneratorBase
    {
        public override string GeneratePromptByData(LLMInputData inputData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# Combat AI Analysis");
            prompt.AppendLine("Analyze the battle situation from the following data and output the optimal tactics in the specified JSON format.");
            prompt.AppendLine();

            // === 現在の状況 ===
            prompt.AppendLine("## Current Situation");

            var myHp = inputData.MyData.Hp;
            var enemyHp = inputData.NPCData.Hp;
            var hpDiff = myHp - enemyHp;
            var myEnergy = inputData.MyData.Energy;
            var enemyEnergy = inputData.NPCData.Energy;
            var energyDiff = myEnergy - enemyEnergy;

            prompt.AppendLine($"- **HP Status** You:{myHp} Enemy:{enemyHp} (You have {Math.Abs(hpDiff)} {(hpDiff >= 0 ? "more" : "less")} HP than enemy)");
            prompt.AppendLine($"- **Energy Status** You:{myEnergy} Enemy:{enemyEnergy} (You have {Math.Abs(energyDiff)} {(energyDiff >= 0 ? "more" : "less")} energy than enemy)");
            prompt.AppendLine();

            // === 敵攻撃パターン ===
            prompt.AppendLine("## Enemy Attack Patterns");

            // 直近攻撃パターン
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

                prompt.AppendLine($"**Recent Attack Pattern** Light Attack: {lightAttackCount} times, Heavy Attack: {strongAttackCount} times, Feint: {strongAttackCancelCount} times");
            }
            else
            {
                prompt.AppendLine("**Recent:** No data");
            }

            // 累積パターン
            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"**Cumulative:** Heavy Attack:{inputData.ActionLog.StrongAttackPercentage * 100:F0}%, " +
                                 $"Light Attack:{inputData.ActionLog.LightAttackPercentage * 100:F0}%, " +
                                 $"Feint:{inputData.ActionLog.StrongAttackCancelPercentage * 100:F0}%");
            }
            else
            {
                prompt.AppendLine("**Cumulative:** No data");
            }
            prompt.AppendLine();

            // === 敵防御パターン ===
            prompt.AppendLine("## Enemy Defense Patterns");

            // 直近防御パターン
            if (inputData.PlayerLog.DamageSituations != null && inputData.PlayerLog.DamageSituations.Count > 0)
            {
                Span<HitSituation> damageSpan = inputData.PlayerLog.DamageSituations.AsSpan();

                int horizontalDodgeCount = 0; // 横回避
                int backwardDodgeCount = 0;   // 後ろ回避
                int blockingCount = 0;        // ブロッキング成功
                int guardCount = 0;           // ガード成功

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

                prompt.AppendLine($"**Recent Defense Pattern** SideDodge: {horizontalDodgeCount},BackDodge: {backwardDodgeCount}, Parry: {blockingCount} times, Guard: {guardCount} times");
            }
            else
            {
                prompt.AppendLine("**Recent:** No data");
            }

            // 累積防御パターン
            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"**Cumulative:** Counter:{inputData.ActionLog.HorizontalDodgeAttackPercentage * 100:F0}%, " +
                                 $"Parry:{inputData.ActionLog.BlockingPercentage * 100:F0}%, " +
                                 $"Dodge:{(inputData.ActionLog.BackwardDodgePercentage + inputData.ActionLog.HorizontalDodgePercentage) * 100:F0}%");
            }
            else
            {
                prompt.AppendLine("**Cumulative:** No data");
            }
            prompt.AppendLine();

            // === 前回判断後の戦闘結果 ===
            prompt.AppendLine("## Combat Results Since Last Decision");

            float dealtDmg = inputData.NPCLog.HitDamage;
            float takenDmg = inputData.NPCLog.TakeDamage;
            float balance = dealtDmg - takenDmg;

            prompt.AppendLine($"- **Damage** Dealt: {dealtDmg:F0} Taken: {takenDmg:F0} Balance: {balance:+0;-0;0}");
            prompt.AppendLine();

            // === 前回判断基準のフィードバック ===
            prompt.AppendLine("## Feedback on Previous Decision Criteria");

            if (inputData.LastStrategy != null)
            {
                // StrategyResultから全評価を取得
                prompt.AppendLine(inputData.StrategyResult.GetAllConditionEvaluationsEnglish(
                    inputData.LastStrategy.攻撃時判断基準,
                    inputData.LastStrategy.攻撃継続時判断基準,
                    inputData.LastStrategy.防御時判断基準,
                    inputData.LastStrategy.連続防御時判断基準
                ));
            }
            else
            {
                prompt.AppendLine("No previous decision data (first turn)");
            }
            prompt.AppendLine();

            // === 前回戦術の評価 ===
            prompt.AppendLine("### Evaluation of Previous Tactics");
            prompt.AppendLine("Compare the previously selected tactics with the combat results and evaluate their effectiveness.");
            prompt.AppendLine();
            prompt.AppendLine("**If the situation has worsened:**");
            prompt.AppendLine("- Change tactics according to the cause of deterioration");
            prompt.AppendLine("- Avoid continuing the same approach");
            prompt.AppendLine();
            prompt.AppendLine("**If the situation has improved:**");
            prompt.AppendLine("- Decide whether to continue current tactics or proceed to the next phase");
            prompt.AppendLine("- Consider options that leverage your lead (prioritize resource recovery, further offensive, stabilization, etc.)");
            prompt.AppendLine();
            prompt.AppendLine("**If the situation remains unchanged:**");
            prompt.AppendLine("- Analyze the cause of stalemate (both have countered each other, lack of decisive moves, etc.)");
            prompt.AppendLine("- Consider tactical changes to create momentum");
            prompt.AppendLine();
            prompt.AppendLine(GenerateFixedSectionEnglish());
            return prompt.ToString();
        }

        public override string GenerateRandomPrompt()
        {
            var randomSituation = (TestSituationType)UnityEngine.Random.Range(0, 5);
            var inputData = LLMInputData.CreateForTestSituation(randomSituation);
            return GeneratePromptByData(inputData);
        }

        /// <summary>
        /// 英語版の固定部分(システムプロンプト)を生成
        /// </summary>
        public string GenerateFixedSectionEnglish()
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("---");
            prompt.AppendLine("## System Prompt");
            prompt.AppendLine("- You MUST respond with valid JSON format only");
            prompt.AppendLine("- Do NOT include any explanatory text, comments, or markdown formatting");
            prompt.AppendLine("- Do NOT use code blocks (```json) or any other decorations");
            prompt.AppendLine("- The entire response must be parsable JSON starting with { and ending with }");
            prompt.AppendLine("- All string values must be properly escaped and enclosed in quotes");
            prompt.AppendLine("- If any property in the output JSON does not contain a valid value, it will be considered an error");
            prompt.AppendLine();
            prompt.AppendLine("### Basic Tactic Types");
            prompt.AppendLine("- **Aggressive**: High risk high return, high attack frequency");
            prompt.AppendLine("- **Defensive**: Low risk low return, survival priority, only attack when hit is guaranteed");
            prompt.AppendLine("- **Adaptive**: Balance priority, flexible based on situation");
            prompt.AppendLine("- **Disruptive**: Unexpected movements, tactics to break down defensive opponents");
            prompt.AppendLine("- **Endurance**: Energy management is top priority, energy-saving actions for long battles");
            prompt.AppendLine();
            prompt.AppendLine("## Attack Decision Criteria (Attack Decision Indicators)");
            prompt.AppendLine("- Cumulative Probability: Attack with highest success rate from all action history");
            prompt.AppendLine("- Recent Pattern Focus: Select attacks with high success probability based on enemy's recent attack patterns");
            prompt.AppendLine("- Speed Priority: Low risk, low return, high turnover rate, effective against feints");
            prompt.AppendLine("- Return Priority: High return, high risk");
            prompt.AppendLine("- Feint Focus: Minimum risk, no return, observe enemy reaction");
            prompt.AppendLine("- Dispersion Focus: Scatter actions to avoid pattern recognition");
            prompt.AppendLine("- Energy Efficiency: Prioritize energy recovery, avoid actions");
            prompt.AppendLine();
            prompt.AppendLine("## Defense Decision Criteria (Defense Decision Indicators)");
            prompt.AppendLine("- Cumulative Probability: Defense with highest success rate from all action history");
            prompt.AppendLine("- Recent Pattern Focus: Select actions with high success probability based on enemy's recent attack patterns");
            prompt.AppendLine("- Counterattack Focus: Medium risk, medium return, seize attack initiative");
            prompt.AppendLine("- Return Priority: Emphasize return on success");
            prompt.AppendLine("- Risk Avoidance: Focus on defending against enemy's most powerful attacks");
            prompt.AppendLine("- Counter Priority: Meta against high attack frequency opponents, high risk on failure");
            prompt.AppendLine("- Dispersion Focus: Scatter defense patterns to avoid recognition");
            prompt.AppendLine();
            prompt.AppendLine("## Output Format");
            prompt.AppendLine("Fill in all property values of the following Json data structure and output as a string.");
            prompt.AppendLine("For each property, select and enter one most appropriate option from the basic tactic types or decision criteria mentioned above.");
            prompt.AppendLine("There must be absolutely no missing property keys or values.");
            prompt.AppendLine();
            prompt.AppendLine("{");
            prompt.AppendLine("  \"AnalysisResult\": \"\",");
            prompt.AppendLine("  \"BasicTactic\": \"<Select from Basic Tactic Types>\",");
            prompt.AppendLine("  \"AttackCriteria\": \"<Select from Attack Decision Indicators>\",");
            prompt.AppendLine("  \"ContinuousAttackCriteria\": \"<Select from Attack Decision Indicators>\",");
            prompt.AppendLine("  \"DefenseCriteria\": \"<Select from Defense Decision Indicators>\",");
            prompt.AppendLine("  \"ContinuousDefenseCriteria\": \"<Select from Defense Decision Indicators>\"");
            prompt.AppendLine("}");
            prompt.AppendLine();
            prompt.AppendLine("## Key Descriptions");
            prompt.AppendLine("- AnalysisResult: Concisely describe the reasoning, necessary responses, and reflected content within 30 characters");
            prompt.AppendLine("- AttackCriteria: Decision criteria when attacking");
            prompt.AppendLine("- ContinuousAttackCriteria: Decision criteria when attacks continue two or more times");
            prompt.AppendLine("- DefenseCriteria: Decision criteria when defending against enemy attacks");
            prompt.AppendLine("- ContinuousDefenseCriteria: Decision criteria when enemy attacks continue two or more times");
            prompt.AppendLine();
            return prompt.ToString();
        }

        /// <summary>
        /// ActionStateを英語に変換
        /// </summary>
        private string TranslateActionState(ActionState state)
        {
            return state switch
            {
                ActionState.弱攻撃 => "Light Attack",
                ActionState.強攻撃 => "Heavy Attack",
                ActionState.強攻撃キャンセル => "Feint",
                ActionState.ガード => "Guard",
                ActionState.弱攻撃ブロッキング => "Light Parry",
                ActionState.強攻撃ブロッキング => "Heavy Parry",
                ActionState.後ろ回避 => "Backward Dodge",
                ActionState.横回避 => "Side Dodge",
                ActionState.前回避 => "Forward Dodge",
                _ => state.ToString()
            };
        }

        /// <summary>
        /// グラマーを作成する(英語版)
        /// </summary>
        /// <returns></returns>
        public override string GenerateGrammar()
        {
            return @"
root ::= object
object ::= ""{"" 
  ws ""\"" ""AnalysisResult"" ""\"" "":"" ws string "",""
  ws ""\"" ""BasicTactic"" ""\"" "":"" ws basic_tactic_value "",""
  ws ""\"" ""AttackCriteria"" ""\"" "":"" ws attack_criteria_value "",""
  ws ""\"" ""ContinuousAttackCriteria"" ""\"" "":"" ws attack_criteria_value "",""
  ws ""\"" ""DefenseCriteria"" ""\"" "":"" ws defense_criteria_value "",""
  ws ""\"" ""ContinuousDefenseCriteria"" ""\"" "":"" ws defense_criteria_value
  ws ""}""

basic_tactic_value ::= ""\"" (""Aggressive"" | ""Defensive"" | ""Adaptive"" | ""Disruptive"" | ""Endurance"") ""\""

attack_criteria_value ::= ""\"" (""Cumulative Probability"" | ""Recent Pattern Focus"" | ""Speed Priority"" | ""Return Priority"" | ""Feint Focus"" | ""Dispersion Focus"" | ""Energy Efficiency"") ""\""

defense_criteria_value ::= ""\"" (""Cumulative Probability"" | ""Recent Pattern Focus"" | ""Counterattack Focus"" | ""Return Priority"" | ""Risk Avoidance"" | ""Counter Priority"" | ""Dispersion Focus"") ""\""

string ::= ""\"" ([^""\\\x00-\x1f] | ""\\"" ([""\\/bfnrt] | ""u"" [0-9a-fA-F]{4})) * ""\""
ws ::= [ \t\n\r]*
";
        }

    }
}