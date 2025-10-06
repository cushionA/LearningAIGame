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

            // 直近パターン（RecentActionArrayから攻撃のみ抽出）
            if (inputData.RecentActionArray != null && inputData.RecentActionArray.Length > 0)
            {
                var attackActions = new[] { ActionState.弱攻撃, ActionState.強攻撃, ActionState.強攻撃キャンセル };
                var recentAttacks = inputData.RecentActionArray.Where(a => attackActions.Contains(a)).ToList();

                var attackCounts = recentAttacks.GroupBy(a => a)
                    .Select(g => $"{TranslateActionState(g.Key)}: {g.Count()} times")
                    .ToList();

                prompt.AppendLine($"**Recent:** {string.Join(", ", attackCounts)}");
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
            if (inputData.RecentActionArray != null && inputData.RecentActionArray.Length > 0)
            {
                var defenseActions = new[] {
                    ActionState.ガード, ActionState.弱攻撃ブロッキング, ActionState.強攻撃ブロッキング,
                    ActionState.後ろ回避, ActionState.横回避, ActionState.前回避
                };
                var recentDefenses = inputData.RecentActionArray.Where(a => defenseActions.Contains(a)).ToList();

                var defenseCounts = recentDefenses.GroupBy(a => a)
                    .Select(g => $"{TranslateActionState(g.Key)}: {g.Count()} times")
                    .ToList();

                prompt.AppendLine($"**Recent:** {string.Join(", ", defenseCounts)}");
            }
            else
            {
                prompt.AppendLine("**Recent:** No data");
            }

            // 累積防御パターン
            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"**Cumulative:** Guard:{inputData.ActionLog.GuardPercentage * 100:F0}%, " +
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

            float dealtDmg = inputData.HitSituations?.Sum(h => h.GetDamage) ?? 0;
            float takenDmg = inputData.EnemyHitSituations?.Sum(h => h.GetDamage) ?? 0;
            float balance = dealtDmg - takenDmg;

            prompt.AppendLine($"- **Damage** Dealt: {dealtDmg:F0} Taken: {takenDmg:F0} Balance: {balance:+0;-0;0}");
            prompt.AppendLine();

            // === 前回判断基準のフィードバック ===
            prompt.AppendLine("## Feedback on Previous Decision Criteria");

            if (inputData.LastStrategy != null)
            {
                // 各判断基準の評価（実際のデータがあれば使用、なければプレースホルダー）
                // 攻撃時判断基準
                string attackEval = EvaluateAttackCriteria(dealtDmg, "{attack_success_count}", "{attack_fail_count}", "{attack_damage}");
                prompt.AppendLine($"- **Attack Criteria** \"{inputData.LastStrategy.攻撃時判断基準}\" {attackEval}");

                // 連続攻撃時判断基準
                string continuousAttackEval = EvaluateAttackCriteria(dealtDmg, "{continuous_attack_success}", "{continuous_attack_fail}", "{continuous_attack_damage}");
                prompt.AppendLine($"- **Continuous Attack Criteria** \"{inputData.LastStrategy.攻撃継続時判断基準}\" {continuousAttackEval}");

                // 防御時判断基準
                string defenseEval = EvaluateDefenseCriteria(takenDmg, "{defense_success_count}", "{defense_fail_count}", "{defense_damage}");
                prompt.AppendLine($"- **Defense Criteria** \"{inputData.LastStrategy.防御時判断基準}\" {defenseEval}");

                // 連続防御時判断基準
                string continuousDefenseEval = EvaluateDefenseCriteria(takenDmg, "{continuous_defense_success}", "{continuous_defense_fail}", "{continuous_defense_damage}");
                prompt.AppendLine($"- **Continuous Defense Criteria** \"{inputData.LastStrategy.連続防御時判断基準}\" {continuousDefenseEval}");
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
        /// 攻撃判断基準の評価テキストを生成
        /// </summary>
        private string EvaluateAttackCriteria(float totalDamage, string successCount, string failCount, string damage)
        {
            string evaluation;

            // プレースホルダーとして実装（実データがあれば条件分岐）
            if (damage == "{attack_damage}" || damage == "{continuous_attack_damage}")
            {
                // データがない場合はプレースホルダーのまま
                return $"Success:{successCount} Fail:{failCount} Damage:{damage} → {{evaluation}}";
            }

            float dmg = float.Parse(damage);
            if (dmg > 20)
                evaluation = "Effective";
            else if (dmg > 10)
                evaluation = "Standard";
            else
                evaluation = "Ineffective";

            return $"Success:{successCount} Fail:{failCount} Damage:{dmg:F0} → {evaluation}";
        }

        /// <summary>
        /// 防御判断基準の評価テキストを生成
        /// </summary>
        private string EvaluateDefenseCriteria(float totalDamage, string successCount, string failCount, string damage)
        {
            string evaluation;

            // プレースホルダーとして実装（実データがあれば条件分岐）
            if (damage == "{defense_damage}" || damage == "{continuous_defense_damage}")
            {
                // データがない場合はプレースホルダーのまま
                return $"Success:{successCount} Fail:{failCount} Damage:{damage} → {{evaluation}}";
            }

            float dmg = float.Parse(damage);
            if (dmg > 30)
                evaluation = "Must Change";
            else if (dmg > 15)
                evaluation = "Room for Improvement";
            else
                evaluation = "Acceptable";

            return $"Success:{successCount} Fail:{failCount} Damage:{dmg:F0} → {evaluation}";
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
    }
}