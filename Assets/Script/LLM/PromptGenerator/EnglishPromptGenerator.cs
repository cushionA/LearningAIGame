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

            var myHp = inputData.PlayerData.Hp;
            var enemyHp = inputData.NPCData.Hp;
            var hpDiff = myHp - enemyHp;
            var myEnergy = inputData.PlayerData.Energy;
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

            if (inputData.CurrentStrategy != null)
            {
                // StrategyResultから全評価を取得
                prompt.AppendLine(inputData.StrategyResult.GetAllConditionEvaluationsEnglish(
                    inputData.CurrentStrategy.AttackCriteria,
                    inputData.CurrentStrategy.ContinuousAttackCriteria,
                    inputData.CurrentStrategy.DefenseCriteria,
                    inputData.CurrentStrategy.ContinuousDefenseCriteria
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
        /// 固定プロンプトセクション（Output Format Requirements）を生成
        /// LLMへのJSON出力形式、利用可能な戦術オプション、プロパティ説明を含む
        /// このメソッドは初回のみ実行され、結果はキャッシュされる
        /// </summary>
        /// <returns>生成された固定プロンプトセクション</returns>
        public override string GenerateFixedSection()
        {
            var prompt = new StringBuilder();

            // === システムプロンプトとしてのロール定義 ===
            prompt.AppendLine("You are a tactical combat AI advisor. Your role is to analyze battle situations and provide strategic decisions in strict JSON format.");
            prompt.AppendLine();

            // === 戦術ガイドライン ===
            prompt.AppendLine("# Tactical Decision Guidelines");
            prompt.AppendLine();
            prompt.AppendLine("## When DOMINANT/ADVANTAGE:");
            prompt.AppendLine("- Use 'Aggressive' or 'Disruptive' to secure victory quickly");
            prompt.AppendLine("- Focus on high-damage attacks to finish the battle");
            prompt.AppendLine();
            prompt.AppendLine("## When EVENLY MATCHED:");
            prompt.AppendLine("- Use 'Adaptive' to remain flexible");
            prompt.AppendLine("- Create opportunities through varied tactics");
            prompt.AppendLine();
            prompt.AppendLine("## When DISADVANTAGE/CRITICAL:");
            prompt.AppendLine("- Use 'Defensive' or 'Endurance' to survive");
            prompt.AppendLine("- Prioritize energy management and damage avoidance");
            prompt.AppendLine("- Use 'Energy Efficiency' for attacks, 'Risk Avoidance' for defense");
            prompt.AppendLine();

            // === 出力形式要件 ===
            prompt.AppendLine("# Output Format Requirements");
            prompt.AppendLine();

            // JSON出力の厳密なルール定義
            prompt.AppendLine("## Critical JSON Rules");
            prompt.AppendLine("1. Output ONLY pure JSON - no markdown, no code blocks, no explanations");
            prompt.AppendLine("2. Start directly with { and end with }");
            prompt.AppendLine("3. NEVER write ```json or ``` markers");
            prompt.AppendLine("4. ALL properties must contain valid values from the options below");
            prompt.AppendLine("5. NEVER use 'None' or leave any field empty");
            prompt.AppendLine();

            // === 利用可能な戦術オプション ===
            prompt.AppendLine("# Available Tactical Options");
            prompt.AppendLine();

            // 基本戦術タイプ（5種類）
            prompt.AppendLine("## BasicTactic (Choose ONE):");
            prompt.AppendLine("- **Aggressive**: High risk, high damage, fast victory");
            prompt.AppendLine("- **Defensive**: Low risk, survival priority, attack only when safe");
            prompt.AppendLine("- **Adaptive**: Balanced approach, flexible to situation");
            prompt.AppendLine("- **Disruptive**: Unpredictable moves to break enemy rhythm");
            prompt.AppendLine("- **Endurance**: Energy conservation for prolonged battle");
            prompt.AppendLine();

            // 攻撃判断基準（7種類）
            prompt.AppendLine("## AttackCriteria & ContinuousAttackCriteria (Choose ONE for each):");
            prompt.AppendLine("- **Cumulative Probability**: Use historically most successful attacks");
            prompt.AppendLine("- **Recent Pattern Focus**: Counter enemy's recent attack patterns");
            prompt.AppendLine("- **Speed Priority**: Fast, low-risk attacks");
            prompt.AppendLine("- **Return Priority**: High-damage, high-risk attacks");
            prompt.AppendLine("- **Feint Focus**: Feints to observe enemy reactions");
            prompt.AppendLine("- **Dispersion Focus**: Vary attacks to avoid predictability");
            prompt.AppendLine("- **Energy Efficiency**: Minimal attacks to conserve energy (use in desperate situations)");
            prompt.AppendLine();

            // 防御判断基準（7種類）
            prompt.AppendLine("## DefenseCriteria & ContinuousDefenseCriteria (Choose ONE for each):");
            prompt.AppendLine("- **Cumulative Probability**: Use historically most successful defenses");
            prompt.AppendLine("- **Recent Pattern Focus**: Counter enemy's recent attack patterns");
            prompt.AppendLine("- **Counterattack Focus**: Risky counters to seize initiative");
            prompt.AppendLine("- **Return Priority**: High-reward defensive moves");
            prompt.AppendLine("- **Risk Avoidance**: Defend against enemy's strongest attacks");
            prompt.AppendLine("- **Counter Priority**: Counter-heavy style (high risk if timed wrong)");
            prompt.AppendLine("- **Dispersion Focus**: Vary defenses to avoid predictability");
            prompt.AppendLine();

            // === 必須JSON構造の例示 ===
            prompt.AppendLine("# Required JSON Structure");
            prompt.AppendLine();
            prompt.AppendLine("You must always respond with this exact structure:");
            prompt.AppendLine();
            prompt.AppendLine("{");
            prompt.AppendLine("  \"AnalysisResult\": \"Your tactical reasoning in max 50 characters\",");
            prompt.AppendLine("  \"BasicTactic\": \"One of: Aggressive, Defensive, Adaptive, Disruptive, Endurance\",");
            prompt.AppendLine("  \"AttackCriteria\": \"One attack criteria from list above\",");
            prompt.AppendLine("  \"ContinuousAttackCriteria\": \"One attack criteria from list above\",");
            prompt.AppendLine("  \"DefenseCriteria\": \"One defense criteria from list above\",");
            prompt.AppendLine("  \"ContinuousDefenseCriteria\": \"One defense criteria from list above\"");
            prompt.AppendLine("}");
            prompt.AppendLine();

            // === 各プロパティの説明 ===
            prompt.AppendLine("# Property Descriptions");
            prompt.AppendLine();
            prompt.AppendLine("- **AnalysisResult**: Brief explanation of your tactical decision (max 50 characters)");
            prompt.AppendLine("- **BasicTactic**: Overall combat approach for this turn");
            prompt.AppendLine("- **AttackCriteria**: Decision logic for initiating attacks");
            prompt.AppendLine("- **ContinuousAttackCriteria**: Decision logic when attacks chain (2+ consecutive attacks)");
            prompt.AppendLine("- **DefenseCriteria**: Decision logic for initial defensive response");
            prompt.AppendLine("- **ContinuousDefenseCriteria**: Decision logic when defending chains (2+ consecutive enemy attacks)");
            prompt.AppendLine();

            // 最終リマインダー
            prompt.AppendLine("# CRITICAL REMINDER");
            prompt.AppendLine();
            prompt.AppendLine("Your response MUST be valid JSON starting with { and ending with } - absolutely nothing else!");
            prompt.AppendLine("No explanations, no markdown formatting, no code blocks - ONLY the JSON object.");
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