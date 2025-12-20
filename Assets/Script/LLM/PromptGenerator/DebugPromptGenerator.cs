using System;
using System.Text;
using static LearningAIGame.CombatSystem.Core.StateSystem;
using UnityEngine;

//==============================================ファイルヘッダ===========================================================
// DebugPromptGenerator
// 
// 概要: LLM用の戦術判断プロンプトを生成するデバッグ専用クラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 戦闘データを解析し、LLMが戦術判断を行うための構造化プロンプトを生成する。
// nullが検出された場合は Debug.LogWarning で出力し、処理を継続する。
// デバッグ用に特化したシンプルな設計。
// 
// **特徴:**
// - nullが検出されたら Warning ログに記録
// - 処理は途中で止めずに継続
// - 代替値でプロンプト生成を完了
// - 例外処理はなし（意図的に例外を出す）
// 
// 基底クラス: PromptGeneratorBase
// 入力データ: LLMInputData (StateSystem, ActionLog, StrategyResult)
// 出力形式: 構造化された英語プロンプト + JSON Schema
//=====================================================================================================================

namespace LLMDataArchitect.Test
{
    /// <summary>
    /// デバッグ用プロンプト生成クラス（ヌル検出時はWarningログのみ）
    /// </summary>
    public class DebugPromptGenerator : PromptGeneratorBase
    {
        #region バランス調整用定数

        private const float k_HpDominantThreshold = 30f;
        private const float k_HpAdvantageThreshold = 10f;
        private const float k_HpDisadvantageThreshold = -10f;
        private const float k_HpCriticalThreshold = -30f;
        private const float k_EnergyDominantThreshold = 40f;
        private const float k_EnergyAdvantageThreshold = 30f;
        private const float k_EnergyDisadvantageThreshold = -30f;
        private const float k_EnergyCriticalThreshold = -40f;
        private const float k_HpEvenThreshold = 20f;
        private const float k_EnergyEvenThreshold = 20f;

        private const float k_PerformanceHighlySuccessfulThreshold = 35f;
        private const float k_PerformanceSuccessfulThreshold = 15f;
        private const float k_PerformanceMajorFailureThreshold = -35f;
        private const float k_PerformanceFailureThreshold = -15f;

        #endregion

        #region キャッシュ

        private string _cachedFixedSection = null;

        #endregion

        /// <summary>
        /// 実際の戦闘データからプロンプトを生成（デバッグ版）
        /// nullが検出されたら Warning ログに記録し、処理は継続
        /// </summary>
        public override string GeneratePromptByData(LLMInputData inputData)
        {
            // 最初のnullチェック
            if (inputData == null)
            {
                Debug.LogWarning("[DebugPromptGenerator] InputData is null");
                inputData = new LLMInputData();  // デフォルト値で代替
            }

            var prompt = new StringBuilder();

            prompt.AppendLine("# Combat AI Tactical Decision");
            prompt.AppendLine("Analyze the current battle state and select optimal tactics.");
            prompt.AppendLine();

            // === 現在の状況 ===
            prompt.AppendLine("## 1. Current Battle State");

            // null合体演算子で安全に取得
            var myHp = inputData.PlayerData?.Hp ?? 100f;
            if (inputData.PlayerData == null)
                Debug.LogWarning("[DebugPromptGenerator] PlayerData is null - using default HP: 100");

            var enemyHp = inputData.NPCData?.Hp ?? 100f;
            if (inputData.NPCData == null)
                Debug.LogWarning("[DebugPromptGenerator] NPCData is null - using default HP: 100");

            var hpDiff = myHp - enemyHp;

            var myEnergy = inputData.PlayerData?.Energy ?? 100f;
            var enemyEnergy = inputData.NPCData?.Energy ?? 100f;
            var energyDiff = myEnergy - enemyEnergy;

            string hpComparison = hpDiff > 0
                ? $"(You have {hpDiff} more)"
                : hpDiff < 0
                    ? $"(Enemy has {Math.Abs(hpDiff)} more)"
                    : "(Equal)";

            string energyComparison = energyDiff > 0
                ? $"(You have {energyDiff} more)"
                : energyDiff < 0
                    ? $"(Enemy has {Math.Abs(energyDiff)} more)"
                    : "(Equal)";

            prompt.AppendLine($"**Your HP**: {myHp:F0}  |  **Enemy HP**: {enemyHp:F0}  {hpComparison}");
            prompt.AppendLine($"**Your Energy**: {myEnergy:F0}  |  **Enemy Energy**: {enemyEnergy:F0}  {energyComparison}");

            string situationTag = EvaluateSituation(hpDiff, energyDiff);
            prompt.AppendLine();
            prompt.AppendLine($"**Situation Assessment**: {situationTag}");
            prompt.AppendLine();

            // === 敵攻撃パターン ===
            prompt.AppendLine("## 2. Enemy Attack Patterns");

            bool hasRecentAttackData = false;

            if (inputData.PlayerLog?.HitSituations != null && inputData.PlayerLog.HitSituations.Count > 0)
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
                    prompt.AppendLine($"**Recent Turns**: HeavyAttackCount {strongAttackCount}, LightAttackCount {lightAttackCount}, FeintCount {strongAttackCancelCount}");
                }
            }
            else
            {
                if (inputData.PlayerLog == null)
                    Debug.LogWarning("[DebugPromptGenerator] PlayerLog is null");
            }

            if (!hasRecentAttackData)
            {
                prompt.AppendLine("**Recent Turns**: No attacks (refer to historical data)");
            }

            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"**Historical Pattern**: Heavy {inputData.ActionLog.HeavyAttackPercentage * 100:F0}%, Light {inputData.ActionLog.LightAttackPercentage * 100:F0}%, Feint {inputData.ActionLog.HeavyAttackCancelPercentage * 100:F0}%");
            }
            else
            {
                Debug.LogWarning("[DebugPromptGenerator] ActionLog is null - historical patterns unavailable");
                prompt.AppendLine("**Historical Pattern**: No historical data available");
            }
            prompt.AppendLine();

            // === 敵防御パターン ===
            prompt.AppendLine("## 3. Enemy Defense Patterns");

            bool hasRecentDefenseData = false;

            if (inputData.PlayerLog?.DamageSituations != null && inputData.PlayerLog.DamageSituations.Count > 0)
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
                        case ActionState.ブロッキング:
                            blockingCount++;
                            break;
                        case ActionState.ガード:
                            guardCount++;
                            break;
                    }
                }

                hasRecentDefenseData = (horizontalDodgeCount + backwardDodgeCount + blockingCount + guardCount) > 0;

                if (hasRecentDefenseData)
                {
                    prompt.AppendLine($"**Recent Turns**: BlockingCount {blockingCount}, GuardCount {guardCount}, CounterCount {horizontalDodgeCount}, DodgeCount {backwardDodgeCount}");
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

            float dealtDmg = inputData.NPCLog?.HitDamage ?? 0f;
            if (inputData.NPCLog == null)
                Debug.LogWarning("[DebugPromptGenerator] NPCLog is null - using default damage values");

            float takenDmg = inputData.NPCLog?.TakeDamage ?? 0f;
            float balance = dealtDmg - takenDmg;

            string performanceTag = EvaluatePerformance(balance);

            prompt.AppendLine($"**Damage Dealt**: {dealtDmg:F0}  |  **Damage Taken**: {takenDmg:F0}  |  **Net Balance**: {balance:+0;-0;0}");
            prompt.AppendLine($"**Performance**: {performanceTag}");
            prompt.AppendLine();

            // === フィードバック ===
            prompt.AppendLine("## 5. Performance Feedback on Last Decision");
            prompt.AppendLine();

            if (inputData.CurrentStrategy != null)
            {
                if (inputData.StrategyResult != null)
                {
                    prompt.AppendLine("**Previous Turn Decision:**");
                    prompt.AppendLine($"- BasicTactic: {inputData.CurrentStrategy.BasicTactic ?? "Unknown"}");
                    prompt.AppendLine($"- AttackCriteria: {inputData.CurrentStrategy.AttackCriteria ?? "Unknown"}");
                    prompt.AppendLine($"- ContinuousAttackCriteria: {inputData.CurrentStrategy.ContinuousAttackCriteria ?? "Unknown"}");
                    prompt.AppendLine($"- DefenseCriteria: {inputData.CurrentStrategy.DefenseCriteria ?? "Unknown"}");
                    prompt.AppendLine($"- ContinuousDefenseCriteria: {inputData.CurrentStrategy.ContinuousDefenseCriteria ?? "Unknown"}");
                    prompt.AppendLine();

                    prompt.AppendLine("**Performance Results:**");
                    var evaluations = inputData.StrategyResult.GetAllConditionEvaluationsEnglish(
                        inputData.CurrentStrategy.AttackCriteria ?? "Unknown",
                        inputData.CurrentStrategy.ContinuousAttackCriteria ?? "Unknown",
                        inputData.CurrentStrategy.DefenseCriteria ?? "Unknown",
                        inputData.CurrentStrategy.ContinuousDefenseCriteria ?? "Unknown"
                    );

                    prompt.AppendLine(evaluations ?? "(Evaluation data unavailable)");
                    prompt.AppendLine();
                }
                else
                {
                    Debug.LogWarning("[DebugPromptGenerator] StrategyResult is null");
                    prompt.AppendLine("(First turn or StrategyResult unavailable)");
                    prompt.AppendLine();
                }
            }
            else
            {
                Debug.LogWarning("[DebugPromptGenerator] CurrentStrategy is null");
                prompt.AppendLine("(First turn - no previous feedback available)");
                prompt.AppendLine();
            }

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
            prompt.AppendLine();

            // 固定セクションを追加
            prompt.Append(GetFixedSectionEnglish());

            Debug.Log($"[DebugPromptGenerator] Prompt generated - Size: {prompt.Length} chars");

            return prompt.ToString();
        }

        /// <summary>
        /// ランダムなテストプロンプトを生成
        /// </summary>
        public override string GenerateRandomPrompt()
        {
            var randomSituation = (TestSituationType)UnityEngine.Random.Range(0, 5);
            var inputData = LLMInputData.CreateForTestSituation(randomSituation);

            if (inputData == null)
            {
                Debug.LogWarning("[DebugPromptGenerator] LLMInputData.CreateForTestSituation returned null");
                inputData = new LLMInputData();
            }

            return GeneratePromptByData(inputData);
        }

        #region プライベートヘルパーメソッド

        /// <summary>
        /// HP差とエネルギー差から戦況を評価
        /// </summary>
        private string EvaluateSituation(float hpDiff, float energyDiff)
        {
            if (hpDiff > k_HpDominantThreshold && energyDiff > k_EnergyDominantThreshold)
                return "【DOMINANT POSITION】";

            if (hpDiff < k_HpCriticalThreshold && energyDiff < k_EnergyCriticalThreshold)
                return "【CRITICAL DANGER】";

            if (hpDiff > k_HpDominantThreshold)
                return "【ADVANTAGE】";

            if (hpDiff > k_HpAdvantageThreshold)
            {
                if (energyDiff >= 0)
                    return "【ADVANTAGE】";
                else
                    return "【SLIGHT ADVANTAGE】";
            }

            if (hpDiff < k_HpCriticalThreshold)
                return "【DISADVANTAGE】";

            if (hpDiff < k_HpDisadvantageThreshold)
            {
                if (energyDiff > k_EnergyAdvantageThreshold)
                    return "【SLIGHT DISADVANTAGE】";
                else
                    return "【DISADVANTAGE】";
            }

            if (Math.Abs(hpDiff) <= k_HpEvenThreshold)
            {
                if (Math.Abs(energyDiff) <= k_EnergyEvenThreshold)
                    return "【EVENLY MATCHED】";
                else if (energyDiff > k_EnergyAdvantageThreshold)
                    return "【SLIGHT ADVANTAGE】";
                else if (energyDiff < k_EnergyDisadvantageThreshold)
                    return "【SLIGHT DISADVANTAGE】";
                else
                    return "【EVENLY MATCHED】";
            }

            return "【EVENLY MATCHED】";
        }

        /// <summary>
        /// ダメージ収支からパフォーマンスを評価
        /// </summary>
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
        /// 固定プロンプトセクション取得（キャッシュ活用）
        /// </summary>
        private string GetFixedSectionEnglish()
        {
            if (_cachedFixedSection != null)
                return _cachedFixedSection;

            _cachedFixedSection = GenerateFixedSection();
            return _cachedFixedSection;
        }

        /// <summary>
        /// 固定プロンプトセクション生成
        /// </summary>
        public override string GenerateFixedSection()
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("You are a tactical combat AI advisor. Your role is to analyze battle situations and provide strategic decisions in strict JSON format.");
            prompt.AppendLine();

            prompt.AppendLine("## Tactical Decision Guidelines");
            prompt.AppendLine();

            prompt.AppendLine("## When DOMINANT/ADVANTAGE:");
            prompt.AppendLine("- 'Aggressive': Use for offensive strategy to maximize damage output");
            prompt.AppendLine("- 'Disruptive': Use to break through strong defenses or disrupt enemy tactics");
            prompt.AppendLine("- Goal: Finish the battle quickly with high-damage attacks");
            prompt.AppendLine();

            prompt.AppendLine("## When EVENLY MATCHED:");
            prompt.AppendLine("- 'Adaptive': Remain flexible and adjust tactics based on situation");
            prompt.AppendLine("- Goal: Create opportunities through varied tactics and analysis");
            prompt.AppendLine();

            prompt.AppendLine("## When DISADVANTAGE/CRITICAL:");
            prompt.AppendLine("- 'Defensive': Use when HP is critically low (survival priority)");
            prompt.AppendLine("- 'Endurance': Use when energy is depleted (conservation strategy)");
            prompt.AppendLine("- **Priority: When HP is low, prioritize 'Defensive' over 'Endurance'**");
            prompt.AppendLine("- Goal: Survive and recover until conditions improve");

            prompt.AppendLine("# Output Format Requirements");
            prompt.AppendLine();

            prompt.AppendLine("## Critical JSON Rules");
            prompt.AppendLine("1. Output ONLY pure JSON - no markdown, no code blocks, no explanations");
            prompt.AppendLine("2. Start directly with { and end with }");
            prompt.AppendLine("3. NEVER write ```json or ``` markers");
            prompt.AppendLine("4. ALL properties must contain valid values from the options below");
            prompt.AppendLine("5. NEVER use 'None' or leave any field empty");
            prompt.AppendLine();

            prompt.AppendLine("# Available Tactical Options");
            prompt.AppendLine();

            prompt.AppendLine("## BasicTactic (Choose ONE):");
            prompt.AppendLine("- **Aggressive**: High risk, high damage, fast victory");
            prompt.AppendLine("- **Defensive**: Low risk, survival priority, attack only when safe");
            prompt.AppendLine("- **Adaptive**: Balanced approach, flexible to situation");
            prompt.AppendLine("- **Disruptive**: Unpredictable moves to break enemy rhythm");
            prompt.AppendLine("- **Endurance**: Energy conservation for prolonged battle");
            prompt.AppendLine();

            prompt.AppendLine("## AttackCriteria & ContinuousAttackCriteria (Choose ONE for each):");
            prompt.AppendLine("- **Cumulative Probability**: Use all historical data to select most successful attacks");
            prompt.AppendLine("- **Recent Pattern Focus**: Focus only on last 3-5 turns to counter recent patterns");
            prompt.AppendLine("- **Speed Priority**: Fast, low-risk attacks");
            prompt.AppendLine("- **Return Priority**: High-damage, high-risk attacks");
            prompt.AppendLine("- **Feint Focus**: Feints to observe enemy reactions");
            prompt.AppendLine("- **Dispersion Focus**: Vary attacks to avoid predictability");
            prompt.AppendLine("- **Energy Efficiency**: Minimal attacks to conserve energy (use in desperate situations)");
            prompt.AppendLine();

            prompt.AppendLine("## DefenseCriteria & ContinuousDefenseCriteria (Choose ONE for each):");
            prompt.AppendLine("- **Cumulative Probability**: Use all historical data to select most successful defenses");
            prompt.AppendLine("- **Recent Pattern Focus**: Focus only on last 3-5 turns to counter recent patterns");
            prompt.AppendLine("- **Counterattack Focus**: Risky counters to seize initiative");
            prompt.AppendLine("- **Return Priority**: High-reward defensive moves");
            prompt.AppendLine("- **Risk Avoidance**: Defend against enemy's strongest attacks");
            prompt.AppendLine("- **Evasive Counter Priority**: Attack while dodging (high risk if failed)");
            prompt.AppendLine("- **Dispersion Focus**: Vary defenses to avoid predictability");
            prompt.AppendLine();

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

            prompt.AppendLine("# Property Descriptions");
            prompt.AppendLine();
            prompt.AppendLine("- **AnalysisResult**: Brief explanation of your tactical decision (max 50 characters)");
            prompt.AppendLine("- **BasicTactic**: Overall combat approach for this turn");
            prompt.AppendLine("- **AttackCriteria**: Decision logic for initiating attacks");
            prompt.AppendLine("- **ContinuousAttackCriteria**: Decision logic when attacks chain (2+ consecutive attacks)");
            prompt.AppendLine("- **DefenseCriteria**: Decision logic for initial defensive response");
            prompt.AppendLine("- **ContinuousDefenseCriteria**: Decision logic when defending chains (2+ consecutive enemy attacks)");
            prompt.AppendLine();

            prompt.AppendLine("# CRITICAL REMINDER");
            prompt.AppendLine();
            prompt.AppendLine("Your response MUST be valid JSON starting with { and ending with } - absolutely nothing else!");
            prompt.AppendLine("No explanations, no markdown formatting, no code blocks - ONLY the JSON object.");
            prompt.AppendLine();

            return prompt.ToString();
        }

        /// <summary>
        /// JSON Schema Grammar を返す
        /// </summary>
        public override string GenerateGrammar()
        {
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