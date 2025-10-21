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
    public class FixedEnglishGenerator : PromptGeneratorBase
    {
        public override string GeneratePromptByData(LLMInputData inputData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# Combat AI Analysis");
            prompt.AppendLine("Analyze the battle situation and output optimal tactics in JSON format.");
            prompt.AppendLine();

            // === 現在の状況 ===
            prompt.AppendLine("## Current Situation");

            var myHp = inputData.PlayerData.Hp;
            var enemyHp = inputData.NPCData.Hp;
            var hpDiff = myHp - enemyHp;
            var myEnergy = inputData.PlayerData.Energy;
            var enemyEnergy = inputData.NPCData.Energy;
            var energyDiff = myEnergy - enemyEnergy;

            // 状況を明確に評価
            string situationAssessment = "";
            if (hpDiff > 50 && energyDiff > 20)
                situationAssessment = " → **You have STRONG advantage**";
            else if (hpDiff > 20 || energyDiff > 30)
                situationAssessment = " → **You have advantage**";
            else if (hpDiff < -50 && energyDiff < -20)
                situationAssessment = " → **You are in CRITICAL danger**";
            else if (hpDiff < -20 || energyDiff < -30)
                situationAssessment = " → **You are at disadvantage**";
            else if (Math.Abs(hpDiff) <= 20 && Math.Abs(energyDiff) <= 20)
                situationAssessment = " → **Evenly matched**";

            prompt.AppendLine($"- **HP**: You {myHp} vs Enemy {enemyHp} (Difference: {hpDiff:+0;-0;0})");
            prompt.AppendLine($"- **Energy**: You {myEnergy} vs Enemy {enemyEnergy} (Difference: {energyDiff:+0;-0;0})");
            prompt.AppendLine(situationAssessment);
            prompt.AppendLine();

            // === 敵攻撃パターン ===
            prompt.AppendLine("## Enemy Attack Patterns");

            // 直近攻撃パターン
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
                    prompt.AppendLine($"**Recent** (Last few turns): Light {lightAttackCount}, Heavy {strongAttackCount}, Feint {strongAttackCancelCount}");
                }
            }

            if (!hasRecentAttackData)
            {
                prompt.AppendLine("**Recent**: No attacks observed (Use cumulative data below)");
            }

            // 累積パターン
            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"**Overall History**: Heavy {inputData.ActionLog.HeavyAttackPercentage * 100:F0}%, " +
                                 $"Light {inputData.ActionLog.LightAttackPercentage * 100:F0}%, " +
                                 $"Feint {inputData.ActionLog.HeavyAttackCancelPercentage * 100:F0}%");
            }
            prompt.AppendLine();

            // === 敵防御パターン ===
            prompt.AppendLine("## Enemy Defense Patterns");

            // 直近防御パターン
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
                    prompt.AppendLine($"**Recent** (Last few turns): SideDodge {horizontalDodgeCount}, BackDodge {backwardDodgeCount}, Parry {blockingCount}, Guard {guardCount}");
                }
            }

            if (!hasRecentDefenseData)
            {
                prompt.AppendLine("**Recent**: No defenses observed (Use cumulative data below)");
            }

            // 累積防御パターン
            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"**Overall History**: Counter {inputData.ActionLog.HorizontalDodgeAttackPercentage * 100:F0}%, " +
                                 $"Parry {inputData.ActionLog.BlockingPercentage * 100:F0}%, " +
                                 $"Dodge {(inputData.ActionLog.BackwardDodgePercentage + inputData.ActionLog.HorizontalDodgePercentage) * 100:F0}%");
            }
            prompt.AppendLine();

            // === 前回判断後の戦闘結果 ===
            prompt.AppendLine("## Combat Results Since Last Decision");

            float dealtDmg = inputData.NPCLog.HitDamage;
            float takenDmg = inputData.NPCLog.TakeDamage;
            float balance = dealtDmg - takenDmg;

            string performanceNote;
            if (balance > 50)
                performanceNote = " → Previous tactics were VERY effective";
            else if (balance > 20)
                performanceNote = " → Previous tactics were effective";
            else if (balance < -50)
                performanceNote = " → Previous tactics FAILED badly";
            else if (balance < -20)
                performanceNote = " → Previous tactics were ineffective";
            else
                performanceNote = " → Stalemate";

            prompt.AppendLine($"**Damage**: Dealt {dealtDmg:F0}, Taken {takenDmg:F0}, Net {balance:+0;-0;0}{performanceNote}");
            prompt.AppendLine();

            // === 前回判断基準のフィードバック ===
            prompt.AppendLine("## Feedback on Previous Decision");

            if (inputData.CurrentStrategy != null)
            {
                prompt.AppendLine("**IMPORTANT**: The following shows success/failure of your LAST decision:");
                prompt.AppendLine(inputData.StrategyResult.GetAllConditionEvaluationsEnglish(
                    inputData.CurrentStrategy.AttackCriteria,
                    inputData.CurrentStrategy.ContinuousAttackCriteria,
                    inputData.CurrentStrategy.DefenseCriteria,
                    inputData.CurrentStrategy.ContinuousDefenseCriteria
                ));
                prompt.AppendLine("→ If \"Highly Effective\": CONTINUE same criteria");
                prompt.AppendLine("→ If \"Must Change\": MUST select different criteria");
            }
            else
            {
                prompt.AppendLine("(First turn - no previous data)");
            }
            prompt.AppendLine();

            // === 戦術指針の簡潔化 ===
            prompt.AppendLine("## Tactical Guidelines");
            prompt.AppendLine("**If you have advantage**: Use Aggressive/Disruptive to finish quickly");
            prompt.AppendLine("**If evenly matched**: Use Adaptive, focus on creating opportunities");
            prompt.AppendLine("**If at disadvantage**: Use Defensive/Endurance, prioritize survival and energy management");
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
        /// 英語版の固定部分(システムプロンプト)を生成
        /// </summary>
        public override string GenerateFixedSection()
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("---");
            prompt.AppendLine("## Output Instructions");
            prompt.AppendLine("**CRITICAL RULES**:");
            prompt.AppendLine("1. Output ONLY raw JSON. No markdown, no code blocks, no explanations");
            prompt.AppendLine("2. Your response MUST start with { and end with }");
            prompt.AppendLine("3. NEVER write ```json or ``` or any decorators");
            prompt.AppendLine("4. ALL properties must have valid values from the lists below");
            prompt.AppendLine("5. NEVER use 'None' or empty strings - always select a valid option");
            prompt.AppendLine();
            prompt.AppendLine("### Basic Tactic Types");
            prompt.AppendLine("- **Aggressive**: High risk, high damage, fast victory");
            prompt.AppendLine("- **Defensive**: Low risk, survival first, attack only when safe");
            prompt.AppendLine("- **Adaptive**: Balanced, flexible response to situation");
            prompt.AppendLine("- **Disruptive**: Unpredictable moves to break enemy patterns");
            prompt.AppendLine("- **Endurance**: Energy conservation for long battle");
            prompt.AppendLine();
            prompt.AppendLine("### Attack Decision Criteria");
            prompt.AppendLine("(IMPORTANT: Even in dire situations, you MUST select ONE valid option - do NOT use 'None')");
            prompt.AppendLine("- **Cumulative Probability**: Use historically most successful attack");
            prompt.AppendLine("- **Recent Pattern Focus**: Counter enemy's recent attack patterns");
            prompt.AppendLine("- **Speed Priority**: Fast, low-risk attacks");
            prompt.AppendLine("- **Return Priority**: High-damage, high-risk attacks");
            prompt.AppendLine("- **Feint Focus**: Use feints to study enemy");
            prompt.AppendLine("- **Dispersion Focus**: Vary attacks to avoid patterns");
            prompt.AppendLine("- **Energy Efficiency**: Minimal energy attacks, maximum conservation (select this in desperate situations)");
            prompt.AppendLine();
            prompt.AppendLine("### Defense Decision Criteria");
            prompt.AppendLine("- **Cumulative Probability**: Use historically most successful defense");
            prompt.AppendLine("- **Recent Pattern Focus**: Counter enemy's recent attack patterns");
            prompt.AppendLine("- **Counterattack Focus**: Risky counters for initiative");
            prompt.AppendLine("- **Return Priority**: High-reward defenses");
            prompt.AppendLine("- **Risk Avoidance**: Defend against strongest enemy attacks");
            prompt.AppendLine("- **Counter Priority**: Counter-heavy style (risky if wrong)");
            prompt.AppendLine("- **Dispersion Focus**: Vary defense to avoid patterns");
            prompt.AppendLine();
            prompt.AppendLine("## Required JSON Format");
            prompt.AppendLine("Your entire response must be exactly in this format:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"AnalysisResult\": \"Brief reason (max 50 chars)\",");
            prompt.AppendLine("  \"BasicTactic\": \"Aggressive\",");
            prompt.AppendLine("  \"AttackCriteria\": \"Speed Priority\",");
            prompt.AppendLine("  \"ContinuousAttackCriteria\": \"Cumulative Probability\",");
            prompt.AppendLine("  \"DefenseCriteria\": \"Risk Avoidance\",");
            prompt.AppendLine("  \"ContinuousDefenseCriteria\": \"Cumulative Probability\"");
            prompt.AppendLine("}");
            prompt.AppendLine();
            prompt.AppendLine("CRITICAL: Start your response with { (not with any other text or markers)");
            prompt.AppendLine();
            return prompt.ToString();
        }

        public override string GenerateGrammar()
        {
            // JSON Schema形式で返す
            return @"{
  ""type"": ""object"",
  ""properties"": {
    ""AnalysisResult"": {
      ""type"": ""string"",
      ""maxLength"": 100
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
      ""enum"": [""Cumulative Probability"", ""Recent Pattern Focus"", ""Counterattack Focus"", ""Return Priority"", ""Risk Avoidance"", ""Counter Priority"", ""Dispersion Focus""]
    },
    ""ContinuousDefenseCriteria"": {
      ""type"": ""string"",
      ""enum"": [""Cumulative Probability"", ""Recent Pattern Focus"", ""Counterattack Focus"", ""Return Priority"", ""Risk Avoidance"", ""Counter Priority"", ""Dispersion Focus""]
    }
  },
  ""required"": [""AnalysisResult"", ""BasicTactic"", ""AttackCriteria"", ""ContinuousAttackCriteria"", ""DefenseCriteria"", ""ContinuousDefenseCriteria""],
  ""additionalProperties"": false
}";
        }
    }
}