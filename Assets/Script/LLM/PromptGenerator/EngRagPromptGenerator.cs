using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;
using static LLMDataArchitect.ActionTable;

namespace LLMDataArchitect.Test
{
    public class EngRagPromptGenerator : PromptGeneratorBase
    {
        public override string GeneratePromptByData(LLMInputData inputData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# Combat AI Analysis");
            prompt.AppendLine("Analyze the following battle data and output the optimal tactics in the specified JSON format.");
            prompt.AppendLine();

            // === Current Situation ===
            prompt.AppendLine("## Current Situation");

            var myHp = inputData.PlayerData.Hp;
            var enemyHp = inputData.NPCData.Hp;
            var hpDiff = myHp - enemyHp;
            var myEnergy = inputData.PlayerData.Energy;
            var enemyEnergy = inputData.NPCData.Energy;
            var energyDiff = myEnergy - enemyEnergy;

            prompt.AppendLine($"- **HP Status** Self: {myHp} Enemy: {enemyHp} (HP is {Math.Abs(hpDiff)} {(hpDiff >= 0 ? "higher" : "lower")} than enemy)");
            prompt.AppendLine($"- **Energy Status** Self: {myEnergy} Enemy: {enemyEnergy} (Energy is {Math.Abs(energyDiff)} {(energyDiff >= 0 ? "higher" : "lower")} than enemy)");
            prompt.AppendLine();

            // === Enemy Attack Pattern ===
            prompt.AppendLine("## Enemy Attack Pattern");

            // Recent pattern (extract attacks only from RecentActionArray)
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
                        case ActionState.é„çUåÇ:
                            lightAttackCount++;
                            break;
                        case ActionState.ã≠çUåÇ:
                            strongAttackCount++;
                            break;
                        case ActionState.ã≠çUåÇÉLÉÉÉìÉZÉã:
                            strongAttackCancelCount++;
                            break;
                    }
                }

                prompt.AppendLine($"**Recent Attack Pattern** Light Attacks: {lightAttackCount}, Strong Attacks: {strongAttackCount}, Feints: {strongAttackCancelCount}");
            }
            else
            {
                prompt.AppendLine("**Recent** No data available");
            }

            // Cumulative pattern
            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"**Cumulative** Strong Attacks: {inputData.ActionLog.StrongAttackPercentage * 100:F0}%, " +
                                 $"Light Attacks: {inputData.ActionLog.LightAttackPercentage * 100:F0}%, " +
                                 $"Feints: {inputData.ActionLog.StrongAttackCancelPercentage * 100:F0}%");
            }
            else
            {
                prompt.AppendLine("**Cumulative** No data available");
            }
            prompt.AppendLine();

            // === Enemy Defense Pattern ===
            prompt.AppendLine("## Enemy Defense Pattern");

            // Recent defense pattern
            if (inputData.PlayerLog.DamageSituations != null && inputData.PlayerLog.DamageSituations.Count > 0)
            {
                Span<HitSituation> damageSpan = inputData.PlayerLog.DamageSituations.AsSpan();

                int horizontalDodgeCount = 0; // Horizontal dodge
                int backwardDodgeCount = 0;   // Backward dodge
                int blockingCount = 0;        // Successful blocking
                int guardCount = 0;           // Successful guard

                foreach (var situation in damageSpan)
                {
                    switch (situation.DamageState)
                    {
                        case ActionState.â°âÒî:
                            horizontalDodgeCount++;
                            break;
                        case ActionState.å„ÇÎâÒî:
                            backwardDodgeCount++;
                            break;
                        case ActionState.ÉuÉçÉbÉLÉìÉOê¨å˜:
                            blockingCount++;
                            break;
                        case ActionState.ÉKÅ[Éhê¨å˜:
                            guardCount++;
                            break;
                    }
                }

                prompt.AppendLine($"**Recent Defense Pattern** Horizontal Dodges: {horizontalDodgeCount}, Backward Dodges: {backwardDodgeCount}, Blocks: {blockingCount}, Guards: {guardCount}");
            }
            else
            {
                prompt.AppendLine("**Recent** No data available");
            }

            // Cumulative defense pattern
            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"**Cumulative** Counters: {inputData.ActionLog.HorizontalDodgeAttackPercentage * 100:F0}%, " +
                                 $"Blocking: {inputData.ActionLog.BlockingPercentage * 100:F0}%, " +
                                 $"Dodges: {(inputData.ActionLog.BackwardDodgePercentage + inputData.ActionLog.HorizontalDodgePercentage) * 100:F0}%");
            }
            else
            {
                prompt.AppendLine("**Cumulative** No data available");
            }
            prompt.AppendLine();

            // === Combat Results Since Last Decision ===
            prompt.AppendLine("## Combat Results Since Last Decision");

            float dealtDmg = inputData.NPCLog.HitDamage;
            float takenDmg = inputData.NPCLog.TakeDamage;
            float balance = dealtDmg - takenDmg;

            prompt.AppendLine($"- **Damage** Dealt: {dealtDmg:F0} Taken: {takenDmg:F0} Balance: {balance:+0;-0;0}");
            prompt.AppendLine();

            // === Feedback on Previous Decision Criteria ===
            prompt.AppendLine("## Feedback on Previous Decision Criteria");

            if (inputData.CurrentStrategy != null)
            {
                // Evaluation of each decision criterion (use actual data if available, placeholder otherwise)
                prompt.AppendLine(inputData.StrategyResult.GetAllConditionEvaluationsEnglish());
            }
            else
            {
                prompt.AppendLine("No previous decision data (initial state)");
            }
            prompt.AppendLine();

            // === Evaluation of Previous Tactics ===
            prompt.AppendLine("### Evaluation of Previous Tactics");
            prompt.AppendLine("Compare the previously selected tactics with the combat results and evaluate their effectiveness.");
            prompt.AppendLine();
            prompt.AppendLine("**If the situation has worsened:**");
            prompt.AppendLine("- Change tactics based on the cause of deterioration");
            prompt.AppendLine("- Avoid continuing the same approach");
            prompt.AppendLine();
            prompt.AppendLine("**If the situation has improved:**");
            prompt.AppendLine("- Decide whether to continue current tactics or advance to the next phase");
            prompt.AppendLine("- Consider options that leverage your lead (resource recovery priority, further offensive, stabilization, etc.)");
            prompt.AppendLine();
            prompt.AppendLine("**If the situation hasn't changed:**");
            prompt.AppendLine("- Analyze the cause of stalemate (both sides adapted, lack of decisive moves, etc.)");
            prompt.AppendLine("- Consider tactical changes to create momentum");
            prompt.AppendLine();
            prompt.AppendLine(GenerateFixedSection());

            return prompt.ToString();
        }

        public override string GenerateRandomPrompt()
        {
            var randomSituation = (TestSituationType)UnityEngine.Random.Range(0, 5);
            var inputData = LLMInputData.CreateForTestSituation(randomSituation);
            return GeneratePromptByData(inputData);
        }

        /// <summary>
        /// Generate English version of fixed section (system prompt)
        /// </summary>
        public override string GenerateFixedSection()
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("---");
            prompt.AppendLine("## System Prompt");
            prompt.AppendLine("- You must respond ONLY in valid JSON format");
            prompt.AppendLine("- Do not include any explanations, comments, or markdown formatting");
            prompt.AppendLine("- Do not use code blocks (```json) or any other decorations");
            prompt.AppendLine("- The entire response must be parseable JSON that begins with { and ends with }");
            prompt.AppendLine("- All string values must be properly escaped and enclosed in quotes");
            prompt.AppendLine("- If any property in the output JSON does not contain a valid value, it will be treated as an error");
            prompt.AppendLine();
            prompt.AppendLine("## Output Format");
            prompt.AppendLine("Fill in the values for all properties in the following JSON structure and output it as a string.");
            prompt.AppendLine("For each property, select the most appropriate option from the basic tactic types or decision criteria mentioned above.");
            prompt.AppendLine("There must be absolutely no missing property keys or values.");
            prompt.AppendLine();
            prompt.AppendLine("{");
            prompt.AppendLine("  \"AnalysisResult\": \"\",");
            prompt.AppendLine("  \"BasicTactic\": \"<Select from basic tactics>\",");
            prompt.AppendLine("  \"AttackCriteria\": \"<Select from attack criteria>\",");
            prompt.AppendLine("  \"ContinuousAttackCriteria\": \"<Select from attack criteria>\",");
            prompt.AppendLine("  \"DefenseCriteria\": \"<Select from defense criteria>\",");
            prompt.AppendLine("  \"ContinuousDefenseCriteria\": \"<Select from defense criteria>\"");
            prompt.AppendLine("}");
            prompt.AppendLine();
            return prompt.ToString();
        }

        /// <summary>
        /// Generate grammar (same as Japanese version but with English property names)
        /// </summary>
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