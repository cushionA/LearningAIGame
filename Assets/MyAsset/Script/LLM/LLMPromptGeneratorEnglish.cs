using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LLMDataArchitectTest.ActionTableEnglish;

namespace LLMDataArchitectTest
{
    /// <summary>
    /// 英語版LLM戦略分析プロンプトジェネレーター
    /// </summary>
    public class LLMPromptGeneratorEnglish
    {
        /// <summary>
        /// 入力データから英語形式の完全なプロンプトを生成
        /// </summary>
        /// <param name="inputData">分析対象の入力データ</param>
        /// <returns>LLMに送信する完全なプロンプト</returns>
        public string GeneratePrompt(LLMInputDataEnglish inputData)
        {
            var analysis = BattleAnalysisResultEnglish.AnalyzeFromInputData(inputData);
            var jsonData = LLMInputDataEnglish.ToJson(inputData, true);

            var prompt = new StringBuilder();

            // プロンプトヘッダー
            prompt.AppendLine("【BATTLE AI ANALYSIS】");
            prompt.AppendLine("Analyze the input data and output strategy in JSON format.");
            prompt.AppendLine("The output must consist solely of JSON data in the specified structure and must absolutely not contain any other data whatsoever.");
            prompt.AppendLine();

            // 必須：状況判定（空欄を埋めた形）
            prompt.AppendLine("【REQUIRED: SITUATION ASSESSMENT】");
            prompt.AppendLine("Calculate and record the following:");
            prompt.AppendLine($"My HP percentage = ({inputData.MyData.Hp} ÷ {inputData.MyData.MaxHp}) × 100 = {analysis.MyHpPercentage:F0}%");
            prompt.AppendLine($"Enemy HP percentage = ({inputData.EnemyData.Hp} ÷ {inputData.EnemyData.MaxHp}) × 100 = {analysis.EnemyHpPercentage:F0}%");
            prompt.AppendLine($"HP difference = My% - Enemy% = {analysis.HpDifference:+0;-0;0} points");
            prompt.AppendLine($"My energy percentage = ({inputData.MyData.Energy} ÷ {inputData.MyData.MaxEnergy}) × 100 = {analysis.MyEnergyPercentage:F0}%");
            prompt.AppendLine();

            // 必須：履歴分析（空欄を埋めた形）
            prompt.AppendLine("【REQUIRED: HISTORY ANALYSIS】");
            prompt.AppendLine("Record the following:");
            prompt.AppendLine($"Effective attack: {analysis.EffectiveAttack}");
            prompt.AppendLine($"Dangerous defense: {analysis.DangerousDefense}");
            prompt.AppendLine($"Enemy attack tendency: {analysis.EnemyAttackTendency}");
            prompt.AppendLine();

            // 戦術判定ルール
            prompt.AppendLine("【TACTICAL JUDGMENT RULES】");
            prompt.AppendLine("HP difference +20 or more AND Energy 50% or more → \"Aggressive\"");
            prompt.AppendLine("HP difference -20 or less OR Energy 30% or less → \"Defensive\"");
            prompt.AppendLine("Others → \"Adaptive\"");
            prompt.AppendLine();

            // 分析観点
            prompt.AppendLine("【ANALYSIS PERSPECTIVES】");
            prompt.AppendLine("1. Enemy behavior patterns (attack frequency, defense frequency, movement tendency)");
            prompt.AppendLine("2. HP/Energy efficiency (endurance vs quick battle)");
            prompt.AppendLine("3. Tactical history success/failure (effective actions, damage causes)");
            prompt.AppendLine();

            // 判断優先順位
            prompt.AppendLine("【DECISION PRIORITIES】");
            prompt.AppendLine("1. Survival assurance (HP/Energy management, fatal damage avoidance)");
            prompt.AppendLine("2. Effective hit creation (utilizing enemy gaps and behavior patterns)");
            prompt.AppendLine("3. Tactical advantage establishment (long-term victory conditions)");
            prompt.AppendLine();

            // 出力形式
            prompt.AppendLine("【OUTPUT FORMAT】");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"conclusion\": \"\",");
            prompt.AppendLine("  \"reasoning\": \"\",");
            prompt.AppendLine("  \"basic_tactics\": \"\",");
            prompt.AppendLine("  \"action_table\": {");
            prompt.AppendLine("    \"enemy_attack_stance\": \"\",");
            prompt.AppendLine("    \"enemy_standby_state\": \"\",");
            prompt.AppendLine("    \"my_slight_advantage\": \"\",");
            prompt.AppendLine("    \"my_advantage\": \"\",");
            prompt.AppendLine("    \"my_slight_disadvantage\": \"\",");
            prompt.AppendLine("    \"my_disadvantage\": \"\",");
            prompt.AppendLine("    \"my_heavy_attack_hit\": \"\",");
            prompt.AppendLine("    \"enemy_heavy_attack_hit\": \"\"");
            prompt.AppendLine("  }");
            prompt.AppendLine("}");
            prompt.AppendLine();

            // 行動選択基準
            prompt.AppendLine("【ACTION SELECTION CRITERIA】");
            prompt.AppendLine("Offensive options (use when advantageous/standby):");
            prompt.AppendLine("- \"Light Attack\": Basic attack, reliable damage");
            prompt.AppendLine("- \"Heavy Attack\": High power, effective when enemy is idle");
            prompt.AppendLine("- \"Heavy Attack Cancel\": Feint, breaks defensive enemies");
            prompt.AppendLine("- \"Light Attack Blocking\": Counter expecting enemy retaliation");
            prompt.AppendLine("- \"Forward Dodge\": Approach, when enemy is defensive");
            prompt.AppendLine("- \"Guard\": Cautious observation, when energy insufficient");
            prompt.AppendLine("Defensive options (use when disadvantageous/enemy attacking):");
            prompt.AppendLine("- \"Guard\": Safest, energy recovery");
            prompt.AppendLine("- \"Backward Dodge\": Danger avoidance, when HP disadvantaged");
            prompt.AppendLine("- \"Horizontal Dodge Attack\": Dodge while counterattacking");
            prompt.AppendLine("- \"Horizontal Dodge\": Attack avoidance, safety priority");
            prompt.AppendLine("- \"Heavy Attack Blocking\": Expecting enemy heavy attack");
            prompt.AppendLine("- \"Light Attack Blocking\": Expecting enemy light attack");
            prompt.AppendLine("- \"Light Attack\": Quick initiative seizure");
            prompt.AppendLine();

            // 状況別選択指針
            prompt.AppendLine("【SITUATION-SPECIFIC GUIDELINES】");
            prompt.AppendLine("Enemy Attack Stance → Select from defensive options");
            prompt.AppendLine("Enemy Standby State → Select from offensive options");
            prompt.AppendLine("My Advantage Situation → Select from offensive options");
            prompt.AppendLine("My Disadvantage Situation → Select from defensive options");
            prompt.AppendLine("HP Disadvantage → Prioritize \"Guard\" or \"Backward Dodge\"");
            prompt.AppendLine("Energy Shortage → Prioritize \"Guard\"");
            prompt.AppendLine();

            // 出力前確認
            prompt.AppendLine("【PRE-OUTPUT CHECKLIST】");
            prompt.AppendLine("□ Are numerical calculations accurate?");
            prompt.AppendLine("□ Did you follow tactical judgment rules?");
            prompt.AppendLine("□ Did you fill in all 8 actions?");
            prompt.AppendLine("□ Did you select actions from the options list?");
            prompt.AppendLine();

            // 入力データ
            prompt.AppendLine("【INPUT DATA】");
            prompt.AppendLine(jsonData);

            return prompt.ToString();
        }

        public string GenerateSystemPrompt()
        {
            var prompt = new StringBuilder();
            // 出力形式
            // JSON専用出力の指示
            prompt.AppendLine("【IMPORTANT INSTRUCTIONS】");
            prompt.AppendLine("- You MUST respond ONLY with valid JSON format");
            prompt.AppendLine("- Do NOT include any explanatory text, comments, or markdown formatting");
            prompt.AppendLine("- Do NOT use code blocks (```json) or any other formatting");
            prompt.AppendLine("- Your entire response must be parseable JSON starting with { and ending with }");
            prompt.AppendLine("- All string values must be properly escaped and quoted");
            prompt.AppendLine("- Do NOT add any text before or after the JSON object");
            prompt.AppendLine();
            // 最終確認
            prompt.AppendLine("【FINAL REMINDER】");
            prompt.AppendLine("Your response must start with { and end with }. Nothing else.");
            prompt.AppendLine();
            return prompt.ToString();
        }

        /// <summary>
        /// サンプルデータでテストプロンプトを生成
        /// </summary>
        /// <param name="situationType">テスト状況の種類</param>
        /// <returns>テスト用プロンプト</returns>
        public string GenerateTestPrompt(TestSituationTypeEnglish situationType = TestSituationTypeEnglish.Even)
        {
            var testData = LLMInputDataEnglish.CreateForTestSituation(situationType);
            return GeneratePrompt(testData);
        }

        /// <summary>
        /// 複数の戦況タイプのテストデータを生成
        /// </summary>
        /// <returns>戦況タイプごとのテストデータセット</returns>
        public Dictionary<TestSituationTypeEnglish, LLMInputDataEnglish> GenerateMultipleTestData()
        {
            var testDataSet = new Dictionary<TestSituationTypeEnglish, LLMInputDataEnglish>();

            foreach (TestSituationTypeEnglish situationType in Enum.GetValues(typeof(TestSituationTypeEnglish)))
            {
                testDataSet[situationType] = LLMInputDataEnglish.CreateForTestSituation(situationType);
            }

            return testDataSet;
        }

        /// <summary>
        /// 複数の戦況タイプのプロンプトを生成
        /// </summary>
        /// <returns>戦況タイプごとのプロンプトセット</returns>
        public Dictionary<TestSituationTypeEnglish, string> GenerateMultipleTestPrompts()
        {
            var testDataSet = GenerateMultipleTestData();
            var promptSet = new Dictionary<TestSituationTypeEnglish, string>();

            foreach (var kvp in testDataSet)
            {
                promptSet[kvp.Key] = GeneratePrompt(kvp.Value);
            }

            return promptSet;
        }

        /// <summary>
        /// 特定の戦況に基づいたカスタムプロンプトを生成
        /// </summary>
        /// <param name="myHp">自分の体力</param>
        /// <param name="myMaxHp">自分の最大体力</param>
        /// <param name="enemyHp">敵の体力</param>
        /// <param name="enemyMaxHp">敵の最大体力</param>
        /// <param name="myEnergy">自分のエネルギー</param>
        /// <param name="myMaxEnergy">自分の最大エネルギー</param>
        /// <param name="enemyEnergy">敵のエネルギー</param>
        /// <param name="enemyMaxEnergy">敵の最大エネルギー</param>
        /// <param name="recentEnemyActions">敵の最近の行動</param>
        /// <returns>カスタマイズされたプロンプト</returns>
        public string GenerateCustomPrompt(
            int myHp, int myMaxHp, int enemyHp, int enemyMaxHp,
            int myEnergy, int myMaxEnergy, int enemyEnergy, int enemyMaxEnergy,
            ActionListEnglish[] recentEnemyActions = null)
        {
            // LLMInputDataEnglishのCreateCustomメソッドを使用
            var customData = LLMInputDataEnglish.CreateCustom(
                myHp, myMaxHp, enemyHp, enemyMaxHp,
                myEnergy, myMaxEnergy, enemyEnergy, enemyMaxEnergy,
                recentEnemyActions);
            return GeneratePrompt(customData);
        }

        /// <summary>
        /// サンプル応答JSONを生成
        /// </summary>
        /// <param name="inputData">入力データ</param>
        /// <returns>期待される応答のサンプルJSON</returns>
        public string GenerateSampleResponse(LLMInputDataEnglish inputData)
        {
            var analysis = BattleAnalysisResultEnglish.AnalyzeFromInputData(inputData);

            // 戦況判定
            var situationType = DetermineSituationType(analysis);

            var response = new
            {
                conclusion = GenerateConclusion(situationType, analysis),
                reasoning = GenerateReason(situationType, analysis),
                basic_tactics = analysis.TacticType,
                action_table = GenerateActionTable(situationType)
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented);
        }

        private TestSituationTypeEnglish DetermineSituationType(BattleAnalysisResultEnglish analysis)
        {
            if (analysis.MyHpPercentage < 30f)
                return TestSituationTypeEnglish.CriticalHP;
            if (analysis.MyEnergyPercentage < 30f)
                return TestSituationTypeEnglish.LowEnergy;
            if (analysis.HpDifference >= 20f)
                return TestSituationTypeEnglish.Advantage;
            if (analysis.HpDifference <= -20f)
                return TestSituationTypeEnglish.Disadvantage;
            return TestSituationTypeEnglish.Even;
        }

        private string GenerateConclusion(TestSituationTypeEnglish situationType, BattleAnalysisResultEnglish analysis)
        {
            return situationType switch
            {
                TestSituationTypeEnglish.Advantage => "Leverage significant HP advantage to aggressively attack and secure victory.",
                TestSituationTypeEnglish.Disadvantage => "Prioritize safety-first defensive tactics while seeking counterattack opportunities to overcome HP disadvantage.",
                TestSituationTypeEnglish.LowEnergy => "Prioritize energy recovery with guard-centered defensive tactics.",
                TestSituationTypeEnglish.CriticalHP => "Prioritize survival with dodge-centered ultra-defensive tactics for prolonged battle.",
                _ => "Maintain current status and respond flexibly according to opponent's movements."
            };
        }

        private string GenerateReason(TestSituationTypeEnglish situationType, BattleAnalysisResultEnglish analysis)
        {
            return situationType switch
            {
                TestSituationTypeEnglish.Advantage => $"With HP difference of {analysis.HpDifference:+0;-0;0} points giving significant advantage, can afford risks to press forward. Energy at {analysis.MyEnergyPercentage:F0}% is also sufficient.",
                TestSituationTypeEnglish.Disadvantage => $"HP difference of {analysis.HpDifference:+0;-0;0} points is disadvantageous; reckless attacks would be fatal. Must solidify defense and induce opponent mistakes.",
                TestSituationTypeEnglish.LowEnergy => $"Energy at {analysis.MyEnergyPercentage:F0}% is critical. Heavy attacks and blocking unavailable, making guard-based recovery urgent.",
                TestSituationTypeEnglish.CriticalHP => $"HP at {analysis.MyHpPercentage:F0}% is dangerous. Even one hit could lead to defeat, making dodge-based survival top priority.",
                _ => $"HP difference of {analysis.HpDifference:+0;-0;0} points, energy at {analysis.MyEnergyPercentage:F0}% represents balanced state. Respond appropriately to opponent's moves."
            };
        }

        private object GenerateActionTable(TestSituationTypeEnglish situationType)
        {
            var actionTable = ActionTableEnglish.CreateForSituation(situationType);
            return new
            {
                enemy_attack_stance = actionTable.EnemyAttackStance,
                enemy_standby_state = actionTable.EnemyStandbyState,
                my_slight_advantage = actionTable.MySlightAdvantage,
                my_advantage = actionTable.MyAdvantage,
                my_slight_disadvantage = actionTable.MySlightDisadvantage,
                my_disadvantage = actionTable.MyDisadvantage,
                my_heavy_attack_hit = actionTable.MyHeavyAttackHit,
                enemy_heavy_attack_hit = actionTable.EnemyHeavyAttackHit
            };
        }
    }

    /// <summary>
    /// 英語版プロンプト生成のユーティリティメソッドを提供する静的クラス
    /// </summary>
    public static class PromptUtilitiesEnglish
    {
        /// <summary>
        /// 戦況に応じた推奨戦術を取得
        /// </summary>
        /// <param name="myHpRatio">自分の体力比率（0.0-1.0）</param>
        /// <param name="enemyHpRatio">敵の体力比率（0.0-1.0）</param>
        /// <param name="myEnergyRatio">自分のエネルギー比率（0.0-1.0）</param>
        /// <returns>推奨戦術</returns>
        public static string GetRecommendedTactic(float myHpRatio, float enemyHpRatio, float myEnergyRatio)
        {
            var hpDiff = (myHpRatio - enemyHpRatio) * 100f;

            if (hpDiff >= 20f && myEnergyRatio >= 0.5f)
                return "Aggressive";

            if (hpDiff <= -20f || myEnergyRatio <= 0.3f)
                return "Defensive";

            return "Adaptive";
        }

        /// <summary>
        /// 行動パターンから敵の傾向を分析
        /// </summary>
        /// <param name="recentActions">最近の敵の行動</param>
        /// <returns>敵の行動傾向</returns>
        public static string AnalyzeEnemyTendency(ActionListEnglish[] recentActions)
        {
            if (recentActions == null || recentActions.Length == 0)
                return "Unknown";

            var attackCount = recentActions.Count(a =>
                a == ActionListEnglish.LightAttack || a == ActionListEnglish.HeavyAttack ||
                a == ActionListEnglish.ForwardDodgeAttack || a == ActionListEnglish.HorizontalDodgeAttack);

            var dodgeCount = recentActions.Count(a =>
                a == ActionListEnglish.BackwardDodge || a == ActionListEnglish.HorizontalDodge || a == ActionListEnglish.ForwardDodge);

            var guardCount = recentActions.Count(a =>
                a == ActionListEnglish.Guard || a.ToString().Contains("Blocking"));

            var total = recentActions.Length;

            if (attackCount > total * 0.5f)
                return "Aggressive type (prefers active attacks)";

            if (dodgeCount > total * 0.4f)
                return "Evasive type (mobility-focused)";

            if (guardCount > total * 0.3f)
                return "Defensive type (emphasizes defense)";

            return "Balanced type (balances offense and defense)";
        }
    }
}