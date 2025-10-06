using System;
using System.Linq;
using System.Text;
using static LearningAIGame.CombatSystem.Core.StateSystem;

namespace LLMDataArchitect
{
    /// <summary>
    /// 新プロンプト形式での戦闘AI判断用システムプロンプト生成クラス
    /// 単一のLLMInputDataから日本語/英語両方のプロンプトを生成可能
    /// </summary>
    public class SystemPromptGenerator
    {
        /// <summary>
        /// 言語指定列挙型
        /// </summary>
        public enum Language
        {
            Japanese,
            English
        }

        /// <summary>
        /// 完全なプロンプトを生成（言語を指定）
        /// </summary>
        public string GenerateFullPrompt(LLMInputData inputData, Language language = Language.Japanese)
        {
            return language == Language.Japanese
                ? GenerateFullPromptJapanese(inputData)
                : GenerateFullPromptEnglish(inputData);
        }

        #region 日本語プロンプト生成

        /// <summary>
        /// 日本語版の完全なプロンプトを生成
        /// </summary>
        public string GenerateFullPromptJapanese(LLMInputData inputData)
        {
            return GenerateDynamicSectionJapanese(inputData) + GenerateFixedSectionJapanese();
        }

        /// <summary>
        /// 日本語版の動的部分(状況分析・履歴分析・入力データ)を生成
        /// </summary>
        public string GenerateDynamicSectionJapanese(LLMInputData inputData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# 戦闘AI分析");
            prompt.AppendLine("入力データを分析し、戦略をJSON形式で出力せよ。");
            prompt.AppendLine("なお出力は指定した構造のJsonデータのみとし、それ以外のデータは絶対に一切含めないこと。");
            prompt.AppendLine();

            // 状況分析セクション
            AppendSituationAnalysisJapanese(prompt, inputData);

            // 直近の敵行動分析セクション
            AppendRecentEnemyActionAnalysisJapanese(prompt, inputData);

            // キャラクターの個性セクション
            AppendCharacterPersonalityJapanese(prompt);

            // 前回の行動方針セクション
            AppendPreviousStrategyJapanese(prompt, inputData);

            // 履歴分析セクション
            AppendHistoryAnalysisJapanese(prompt, inputData);

            return prompt.ToString();
        }

        private void AppendSituationAnalysisJapanese(StringBuilder prompt, LLMInputData inputData)
        {
            prompt.AppendLine("## 状況分析");

            var myHpPercent = (float)inputData.MyData.Hp / inputData.MyData.MaxHp * 100f;
            var enemyHpPercent = (float)inputData.NPCData.Hp / inputData.NPCData.MaxHp * 100f;
            var hpDiff = myHpPercent - enemyHpPercent;
            var myEnergyPercent = (float)inputData.MyData.Energy / inputData.MyData.MaxEnergy * 100f;
            var enemyEnergyPercent = (float)inputData.NPCData.Energy / inputData.NPCData.MaxEnergy * 100f;

            prompt.AppendLine($"- 自分HP割合 {myHpPercent:F0}%");
            prompt.AppendLine($"- 敵HP割合 {enemyHpPercent:F0}%");

            string advantageText = hpDiff > 20 ? "自分 優位"
                                 : hpDiff < -20 ? "敵 優位"
                                 : "拮抗";
            prompt.AppendLine($"- 体力差 {(hpDiff >= 0 ? "自分 +" : "")}{hpDiff:F0} {advantageText}");

            prompt.AppendLine($"- 自分エネルギー割合 {myEnergyPercent:F0}%");
            prompt.AppendLine($"- 敵エネルギー割合 {enemyEnergyPercent:F0}%");
            prompt.AppendLine();
        }

        private void AppendRecentEnemyActionAnalysisJapanese(StringBuilder prompt, LLMInputData inputData)
        {
            prompt.AppendLine("## 直近の敵行動分析結果");

            if (inputData.RecentActionArray != null && inputData.RecentActionArray.Length > 0)
            {
                var attackActions = new[] {
                    ActionState.弱攻撃,
                    ActionState.強攻撃,
                    ActionState.横回避攻撃,
                    ActionState.前回避攻撃
                };

                int attackCount = inputData.RecentActionArray.Count(a => attackActions.Contains(a));
                float attackFrequency = (float)attackCount / inputData.RecentActionArray.Length;

                string frequencyLevel = attackFrequency > 0.6f ? "高"
                                      : attackFrequency > 0.3f ? "中"
                                      : "低";

                prompt.AppendLine($"- 敵攻撃頻度:{frequencyLevel}");

                var actionGroups = inputData.RecentActionArray.GroupBy(a => a).OrderByDescending(g => g.Count());

                if (actionGroups.Any())
                {
                    var mostFrequent = actionGroups.First();
                    float bias = (float)mostFrequent.Count() / inputData.RecentActionArray.Length;

                    string biasLevel = bias > 0.6f ? "大"
                                     : bias > 0.4f ? "中"
                                     : "小";

                    prompt.AppendLine($"- 行動の偏り:{biasLevel}");
                }
                else
                {
                    prompt.AppendLine("- 行動の偏り:不明");
                }
            }
            else
            {
                prompt.AppendLine("- 敵攻撃頻度:データなし");
                prompt.AppendLine("- 行動の偏り:データなし");
            }

            prompt.AppendLine();
        }

        private void AppendCharacterPersonalityJapanese(StringBuilder prompt)
        {
            prompt.AppendLine("## キャラクターの判断における個性");
            prompt.AppendLine("- HPが減少するほどに攻撃的に");
            prompt.AppendLine("- 防御失敗後の反撃を重視");
            prompt.AppendLine();
        }

        private void AppendPreviousStrategyJapanese(StringBuilder prompt, LLMInputData inputData)
        {
            prompt.AppendLine("## 前回の行動方針");

            if (inputData.LastStrategy != null)
            {
                var strategy = inputData.LastStrategy;
                prompt.AppendLine("{");
                prompt.AppendLine($"  \"基本戦術\": \"{strategy.基本戦術 ?? "対応型"}\",");
                prompt.AppendLine($"  \"攻撃時判断基準\": \"累積確率重視\",");
                prompt.AppendLine($"  \"連続攻撃時判断基準\": \"直近パターン重視\",");
                prompt.AppendLine($"  \"防御時判断基準\": \"累積確率重視\",");
                prompt.AppendLine($"  \"防御失敗後の防御判断基準\": \"反撃\"");
                prompt.AppendLine("}");
            }
            else
            {
                prompt.AppendLine("{");
                prompt.AppendLine("  \"基本戦術\": \"対応型\",");
                prompt.AppendLine("  \"攻撃時判断基準\": \"累積確率重視\",");
                prompt.AppendLine("  \"連続攻撃時判断基準\": \"直近パターン重視\",");
                prompt.AppendLine("  \"防御時判断基準\": \"累積確率重視\",");
                prompt.AppendLine("  \"防御失敗後の防御判断基準\": \"反撃\"");
                prompt.AppendLine("}");
            }

            prompt.AppendLine();
        }

        private void AppendHistoryAnalysisJapanese(StringBuilder prompt, LLMInputData inputData)
        {
            prompt.AppendLine("## 履歴分析");
            prompt.AppendLine("前回の判断による戦闘の結果");

            float totalDamageReceived = 0;
            if (inputData.EnemyHitSituations != null && inputData.EnemyHitSituations.Length > 0)
            {
                totalDamageReceived = inputData.EnemyHitSituations.Sum(h => h.GetDamage);
            }
            prompt.AppendLine($"- 自分の直近の被ダメージ合計{totalDamageReceived:F0}ダメージ");

            float totalDamageDealt = 0;
            if (inputData.HitSituations != null && inputData.HitSituations.Length > 0)
            {
                totalDamageDealt = inputData.HitSituations.Sum(h => h.GetDamage);
            }
            prompt.AppendLine($"- 自分の直近の与ダメージ合計{totalDamageDealt:F0}ダメージ");

            prompt.AppendLine("- 自分の今までの攻撃成功率 32%");
            prompt.AppendLine("- 自分の今までの防御成功率 63%");
            prompt.AppendLine("- 敵の今までの攻撃成功率 32%");
            prompt.AppendLine("- 敵の今までの防御成功率 63%");

            prompt.AppendLine();
            prompt.AppendLine("### 前回戦術の評価");
            prompt.AppendLine("前回選択した戦術と戦闘結果を照らし合わせ、効果を評価してください。");
            prompt.AppendLine();
            prompt.AppendLine("**戦況が悪化している場合:**");
            prompt.AppendLine("- 悪化の原因に応じて戦術を変更する");
            prompt.AppendLine("- 同じアプローチを続けることは避ける");
            prompt.AppendLine();
            prompt.AppendLine("**戦況が好転している場合:**");
            prompt.AppendLine("- 現在の戦術を継続するか、次の段階に進むかを判断");
            prompt.AppendLine("- リードを活かした選択肢を検討(リソース回復優先、さらなる攻勢、安定化など)");
            prompt.AppendLine();
            prompt.AppendLine("**戦況が変化していない場合:**");
            prompt.AppendLine("- 膠着の原因を分析する(互いに対策済み、決定打の不足など)");
            prompt.AppendLine("- 変化を生むための戦術変更を検討");
            prompt.AppendLine();
        }

        /// <summary>
        /// 日本語版の固定部分(システムプロンプト)を生成
        /// </summary>
        public string GenerateFixedSectionJapanese()
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("---");
            prompt.AppendLine("## システムプロンプト");
            prompt.AppendLine("- 必ず有効なJSON形式のみで応答してください");
            prompt.AppendLine("- 説明文、コメント、マークダウン形式は一切含めないでください");
            prompt.AppendLine("- コードブロック(```json)やその他の装飾は使用しないでください");
            prompt.AppendLine("- 応答全体が{で始まり}で終わる解析可能なJSONである必要があります");
            prompt.AppendLine("- すべての文字列値は適切にエスケープし、クォートで囲んでください");
            prompt.AppendLine("- 出力するJsonのプロパティに有効な値が入っていなければエラーとみなします");
            prompt.AppendLine();

            prompt.AppendLine("### 基本戦術タイプ");
            prompt.AppendLine("- **攻撃型**: 高リスク高リターン、攻撃頻度が高い");
            prompt.AppendLine("- **防御型**: 低リスク低リターン、生存重視、ヒット確定の状況でのみ攻撃");
            prompt.AppendLine("- **対応型**: バランス重視、状況判断で柔軟に");
            prompt.AppendLine("- **攪乱型**: 意表を突く動き、守備的な相手を崩す作戦");
            prompt.AppendLine("- **持久型**: エネルギー管理が最優先、長期戦を見据えた省エネ行動");
            prompt.AppendLine();

            prompt.AppendLine("## 攻撃時判断基準(攻撃時判断指標)");
            prompt.AppendLine("- 累積確率重視: 全ての行動履歴から最も成功率の高い攻撃");
            prompt.AppendLine("- 直近パターン重視: 敵の直近の攻撃パターンから成功確率の高い攻撃を選択");
            prompt.AppendLine("- 速度重視: 低リスク、低リターン、回転率が高い");
            prompt.AppendLine("- リターン重視: 高リターン、高リスク");
            prompt.AppendLine("- フェイント重視: リスク最小、リターン無し、敵の反応を見る");
            prompt.AppendLine("- 分散重視: パターンを読まれないよう行動を散らす");
            prompt.AppendLine("- エネルギー効率重視: エネルギー回復を優先して行動しない");
            prompt.AppendLine();

            prompt.AppendLine("## 防御時判断基準(防御時判断指標)");
            prompt.AppendLine("- 累積確率重視: 全ての行動履歴から最も成功率の高い防御");
            prompt.AppendLine("- 直近パターン重視: 敵の直近の攻撃パターンから成功確率の高い行動を選択");
            prompt.AppendLine("- エネルギー重視: 防御よりエネルギー回復に専念");
            prompt.AppendLine("- 反撃: 中リスク、中リターン、攻撃の主導権を奪う");
            prompt.AppendLine("- カウンター: 攻撃頻度が高い相手へのメタ、失敗時リスク高");
            prompt.AppendLine("- 生存重視: 最小リスク、リターン無し、エネルギー消費");
            prompt.AppendLine("- 分散重視: 防御パターンを読まれないように散らす");
            prompt.AppendLine("- 回避重視: 低リスク、ノーリターン、布石としてカウンターが成功しやすくなる");
            prompt.AppendLine();

            prompt.AppendLine("## 出力形式");
            prompt.AppendLine("以下の構造のJsonデータの全てのプロパティの値を埋めて、文字列として出力する。");
            prompt.AppendLine("各プロパティには、前述の基本戦術タイプまたは判断基準から、状況に最も適した一つを選択して記入する。");
            prompt.AppendLine("プロパティのキーと値に絶対に抜けがあってはいけない。");
            prompt.AppendLine();
            prompt.AppendLine("{");
            prompt.AppendLine("  \"基本戦術\": \"<基本戦術から選択>\",");
            prompt.AppendLine("  \"攻撃時判断基準\": \"<攻撃時判断基準から選択>\",");
            prompt.AppendLine("  \"攻撃継続時判断基準\": \"<攻撃継続時判断基準から選択>\",");
            prompt.AppendLine("  \"防御時判断基準\": \"<防御時判断基準から選択>\",");
            prompt.AppendLine("  \"連続防御時判断基準\": \"<防御時判断基準から選択>\"");
            prompt.AppendLine("}");
            prompt.AppendLine();

            return prompt.ToString();
        }

        #endregion

        #region 英語プロンプト生成

        /// <summary>
        /// 英語版の完全なプロンプトを生成
        /// </summary>
        public string GenerateFullPromptEnglish(LLMInputData inputData)
        {
            return GenerateDynamicSectionEnglish(inputData) + GenerateFixedSectionEnglish();
        }

        /// <summary>
        /// 英語版の動的部分を生成
        /// </summary>
        public string GenerateDynamicSectionEnglish(LLMInputData inputData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# Battle AI Analysis");
            prompt.AppendLine("Analyze the input data and output the strategy in JSON format.");
            prompt.AppendLine("The output must be ONLY the JSON data with the specified structure, and absolutely no other data must be included.");
            prompt.AppendLine();

            AppendSituationAnalysisEnglish(prompt, inputData);
            AppendRecentEnemyActionAnalysisEnglish(prompt, inputData);
            AppendCharacterPersonalityEnglish(prompt);
            AppendPreviousStrategyEnglish(prompt, inputData);
            AppendHistoryAnalysisEnglish(prompt, inputData);

            return prompt.ToString();
        }

        private void AppendSituationAnalysisEnglish(StringBuilder prompt, LLMInputData inputData)
        {
            prompt.AppendLine("## Situation Analysis");

            var myHpPercent = (float)inputData.MyData.Hp / inputData.MyData.MaxHp * 100f;
            var enemyHpPercent = (float)inputData.NPCData.Hp / inputData.NPCData.MaxHp * 100f;
            var hpDiff = myHpPercent - enemyHpPercent;
            var myEnergyPercent = (float)inputData.MyData.Energy / inputData.MyData.MaxEnergy * 100f;
            var enemyEnergyPercent = (float)inputData.NPCData.Energy / inputData.NPCData.MaxEnergy * 100f;

            prompt.AppendLine($"- My HP Percentage {myHpPercent:F0}%");
            prompt.AppendLine($"- Enemy HP Percentage {enemyHpPercent:F0}%");

            string advantageText = hpDiff > 20 ? "My Advantage"
                                 : hpDiff < -20 ? "Enemy Advantage"
                                 : "Even";
            prompt.AppendLine($"- HP Difference {(hpDiff >= 0 ? "My +" : "")}{hpDiff:F0} {advantageText}");

            prompt.AppendLine($"- My Energy Percentage {myEnergyPercent:F0}%");
            prompt.AppendLine($"- Enemy Energy Percentage {enemyEnergyPercent:F0}%");
            prompt.AppendLine();
        }

        private void AppendRecentEnemyActionAnalysisEnglish(StringBuilder prompt, LLMInputData inputData)
        {
            prompt.AppendLine("## Recent Enemy Action Analysis Results");

            if (inputData.RecentActionArray != null && inputData.RecentActionArray.Length > 0)
            {
                var attackActions = new[] {
                    ActionState.弱攻撃, ActionState.強攻撃,
                    ActionState.横回避攻撃, ActionState.前回避攻撃
                };

                int attackCount = inputData.RecentActionArray.Count(a => attackActions.Contains(a));
                float attackFrequency = (float)attackCount / inputData.RecentActionArray.Length;

                string frequencyLevel = attackFrequency > 0.6f ? "High"
                                      : attackFrequency > 0.3f ? "Medium"
                                      : "Low";

                prompt.AppendLine($"- Enemy Attack Frequency: {frequencyLevel}");

                var actionGroups = inputData.RecentActionArray.GroupBy(a => a).OrderByDescending(g => g.Count());

                if (actionGroups.Any())
                {
                    var mostFrequent = actionGroups.First();
                    float bias = (float)mostFrequent.Count() / inputData.RecentActionArray.Length;

                    string biasLevel = bias > 0.6f ? "Large"
                                     : bias > 0.4f ? "Medium"
                                     : "Small";

                    prompt.AppendLine($"- Action Bias: {biasLevel}");
                }
                else
                {
                    prompt.AppendLine("- Action Bias: Unknown");
                }
            }
            else
            {
                prompt.AppendLine("- Enemy Attack Frequency: No Data");
                prompt.AppendLine("- Action Bias: No Data");
            }

            prompt.AppendLine();
        }

        private void AppendCharacterPersonalityEnglish(StringBuilder prompt)
        {
            prompt.AppendLine("## Character Decision-Making Personality");
            prompt.AppendLine("- Becomes more aggressive as HP decreases");
            prompt.AppendLine("- Prioritizes counterattacks after failed defense");
            prompt.AppendLine();
        }

        private void AppendPreviousStrategyEnglish(StringBuilder prompt, LLMInputData inputData)
        {
            prompt.AppendLine("## Previous Action Policy");

            string basicTactics = "Adaptive";
            if (inputData.LastStrategy != null)
            {
                basicTactics = ConvertTacticsToEnglish(inputData.LastStrategy.基本戦術 ?? "対応型");
            }

            prompt.AppendLine("{");
            prompt.AppendLine($"  \"basic_tactics\": \"{basicTactics}\",");
            prompt.AppendLine("  \"attack_judgment_criteria\": \"Cumulative Probability Focus\",");
            prompt.AppendLine("  \"continuous_attack_judgment_criteria\": \"Recent Pattern Focus\",");
            prompt.AppendLine("  \"defense_judgment_criteria\": \"Cumulative Probability Focus\",");
            prompt.AppendLine("  \"post_defense_failure_criteria\": \"Counterattack\"");
            prompt.AppendLine("}");
            prompt.AppendLine();
        }

        private void AppendHistoryAnalysisEnglish(StringBuilder prompt, LLMInputData inputData)
        {
            prompt.AppendLine("## History Analysis");
            prompt.AppendLine("Results of combat based on previous decisions");

            float totalDamageReceived = 0;
            if (inputData.EnemyHitSituations != null && inputData.EnemyHitSituations.Length > 0)
            {
                totalDamageReceived = inputData.EnemyHitSituations.Sum(h => h.GetDamage);
            }
            prompt.AppendLine($"- Total recent damage taken: {totalDamageReceived:F0} damage");

            float totalDamageDealt = 0;
            if (inputData.HitSituations != null && inputData.HitSituations.Length > 0)
            {
                totalDamageDealt = inputData.HitSituations.Sum(h => h.GetDamage);
            }
            prompt.AppendLine($"- Total recent damage dealt: {totalDamageDealt:F0} damage");

            prompt.AppendLine("- My overall attack success rate: 32%");
            prompt.AppendLine("- My overall defense success rate: 63%");
            prompt.AppendLine("- Enemy overall attack success rate: 32%");
            prompt.AppendLine("- Enemy overall defense success rate: 63%");

            prompt.AppendLine();
            prompt.AppendLine("### Evaluation of Previous Tactics");
            prompt.AppendLine("Evaluate the effectiveness by comparing the previously selected tactics with combat results.");
            prompt.AppendLine();
            prompt.AppendLine("**If the situation has worsened:**");
            prompt.AppendLine("- Change tactics according to the cause of deterioration");
            prompt.AppendLine("- Avoid continuing the same approach");
            prompt.AppendLine();
            prompt.AppendLine("**If the situation has improved:**");
            prompt.AppendLine("- Decide whether to continue current tactics or move to the next phase");
            prompt.AppendLine("- Consider options that leverage the lead (resource recovery priority, further offense, stabilization, etc.)");
            prompt.AppendLine();
            prompt.AppendLine("**If the situation has not changed:**");
            prompt.AppendLine("- Analyze the cause of stalemate (mutual countermeasures, lack of decisive action, etc.)");
            prompt.AppendLine("- Consider tactical changes to create change");
            prompt.AppendLine();
        }

        /// <summary>
        /// 英語版の固定部分を生成
        /// </summary>
        public string GenerateFixedSectionEnglish()
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("---");
            prompt.AppendLine("## System Prompt");
            prompt.AppendLine("- You MUST respond ONLY with valid JSON format");
            prompt.AppendLine("- Do NOT include any explanatory text, comments, or markdown formatting");
            prompt.AppendLine("- Do NOT use code blocks (```json) or any other decorations");
            prompt.AppendLine("- Your entire response must be parseable JSON starting with { and ending with }");
            prompt.AppendLine("- All string values must be properly escaped and quoted");
            prompt.AppendLine("- If the output JSON properties do not contain valid values, it will be considered an error");
            prompt.AppendLine();

            prompt.AppendLine("### Basic Tactics Types");
            prompt.AppendLine("- **Aggressive**: High risk high return, high attack frequency");
            prompt.AppendLine("- **Defensive**: Low risk low return, survival focus, only attack when hit is guaranteed");
            prompt.AppendLine("- **Adaptive**: Balance-focused, flexible based on situation");
            prompt.AppendLine("- **Disruptive**: Unexpected movements, tactics to break down defensive opponents");
            prompt.AppendLine("- **Endurance**: Energy management is top priority, energy-saving actions for long battles");
            prompt.AppendLine();

            prompt.AppendLine("## Attack Judgment Criteria (Attack Decision Indicators)");
            prompt.AppendLine("- Cumulative Probability Focus: Attack with highest success rate from all action history");
            prompt.AppendLine("- Recent Pattern Focus: Select attacks with high success probability from enemy's recent attack patterns");
            prompt.AppendLine("- Speed Focus: Low risk, low return, high turnover rate");
            prompt.AppendLine("- Return Focus: High return, high risk");
            prompt.AppendLine("- Feint Focus: Minimum risk, no return, observe enemy reaction");
            prompt.AppendLine("- Distribution Focus: Scatter actions to avoid pattern reading");
            prompt.AppendLine("- Energy Efficiency Focus: Prioritize energy recovery over action");
            prompt.AppendLine();

            prompt.AppendLine("## Defense Judgment Criteria (Defense Decision Indicators)");
            prompt.AppendLine("- Cumulative Probability Focus: Defense with highest success rate from all action history");
            prompt.AppendLine("- Recent Pattern Focus: Select actions with high success probability from enemy's recent attack patterns");
            prompt.AppendLine("- Energy Focus: Focus on energy recovery over defense");
            prompt.AppendLine("- Counterattack: Medium risk, medium return, seize attack initiative");
            prompt.AppendLine("- Counter: Meta against high-frequency attackers, high risk on failure");
            prompt.AppendLine("- Survival Focus: Minimum risk, no return, energy consumption");
            prompt.AppendLine("- Distribution Focus: Scatter defense patterns to avoid being read");
            prompt.AppendLine("- Evasion Focus: Low risk, no return, makes counters more likely to succeed as groundwork");
            prompt.AppendLine();

            prompt.AppendLine("## Output Format");
            prompt.AppendLine("Fill in all property values of the JSON data with the following structure and output as a string.");
            prompt.AppendLine("For each property, select the one most appropriate for the situation from the basic tactics types or judgment criteria mentioned above.");
            prompt.AppendLine("There must absolutely never be any missing property keys or values.");
            prompt.AppendLine();
            prompt.AppendLine("{");
            prompt.AppendLine("  \"basic_tactics\": \"<Select from basic tactics>\",");
            prompt.AppendLine("  \"attack_judgment_criteria\": \"<Select from attack judgment criteria>\",");
            prompt.AppendLine("  \"continuous_attack_judgment_criteria\": \"<Select from continuous attack judgment criteria>\",");
            prompt.AppendLine("  \"defense_judgment_criteria\": \"<Select from defense judgment criteria>\",");
            prompt.AppendLine("  \"continuous_defense_judgment_criteria\": \"<Select from defense judgment criteria>\"");
            prompt.AppendLine("}");
            prompt.AppendLine();

            return prompt.ToString();
        }

        #endregion

        #region ユーティリティメソッド

        /// <summary>
        /// 戦術名を英語に変換
        /// </summary>
        private string ConvertTacticsToEnglish(string japaneseTactics)
        {
            return japaneseTactics switch
            {
                "攻撃型" => "Aggressive",
                "防御型" => "Defensive",
                "対応型" => "Adaptive",
                "攪乱型" => "Disruptive",
                "持久型" => "Endurance",
                _ => "Adaptive"
            };
        }

        /// <summary>
        /// ActionStateを英語名に変換
        /// </summary>
        private string ConvertActionStateToEnglish(ActionState action)
        {
            return action switch
            {
                ActionState.弱攻撃 => "Light Attack",
                ActionState.強攻撃 => "Heavy Attack",
                ActionState.強攻撃キャンセル => "Heavy Attack Cancel",
                ActionState.後ろ回避 => "Backward Dodge",
                ActionState.横回避 => "Horizontal Dodge",
                ActionState.前回避 => "Forward Dodge",
                ActionState.横回避攻撃 => "Horizontal Dodge Attack",
                ActionState.前回避攻撃 => "Forward Dodge Attack",
                ActionState.ガード => "Guard",
                ActionState.弱攻撃ブロッキング => "Light Attack Blocking",
                ActionState.強攻撃ブロッキング => "Heavy Attack Blocking",
                _ => action.ToString()
            };
        }

        #endregion
    }

    /// <summary>
    /// 新プロンプト形式用の戦略データ構造
    /// </summary>
    public class NewStrategyData
    {
        /// <summary>
        /// 基本戦術
        /// </summary>
        public string 基本戦術 { get; set; }

        /// <summary>
        /// 攻撃時判断基準
        /// </summary>
        public string 攻撃時判断基準 { get; set; }

        /// <summary>
        /// 攻撃継続時判断基準
        /// </summary>
        public string 攻撃継続時判断基準 { get; set; }

        /// <summary>
        /// 防御時判断基準
        /// </summary>
        public string 防御時判断基準 { get; set; }

        /// <summary>
        /// 連続防御時判断基準
        /// </summary>
        public string 連続防御時判断基準 { get; set; }

        /// <summary>
        /// JSON形式の文字列に変換
        /// </summary>
        public string ToJson()
        {
            return $@"{{
  ""基本戦術"": ""{基本戦術}"",
  ""攻撃時判断基準"": ""{攻撃時判断基準}"",
  ""攻撃継続時判断基準"": ""{攻撃継続時判断基準}"",
  ""防御時判断基準"": ""{防御時判断基準}"",
  ""連続防御時判断基準"": ""{連続防御時判断基準}""
}}";
        }

        /// <summary>
        /// JSON文字列から戦略データを解析
        /// </summary>
        public static NewStrategyData FromJson(string json)
        {
            var strategy = new NewStrategyData();

            strategy.基本戦術 = ExtractJsonValue(json, "基本戦術");
            strategy.攻撃時判断基準 = ExtractJsonValue(json, "攻撃時判断基準");
            strategy.攻撃継続時判断基準 = ExtractJsonValue(json, "攻撃継続時判断基準");
            strategy.防御時判断基準 = ExtractJsonValue(json, "防御時判断基準");
            strategy.連続防御時判断基準 = ExtractJsonValue(json, "連続防御時判断基準");

            return strategy;
        }

        /// <summary>
        /// 英語版JSONから戦略データを解析
        /// </summary>
        public static NewStrategyData FromJsonEnglish(string json)
        {
            var strategy = new NewStrategyData();

            // 英語版のキーから値を抽出し、日本語に変換
            var basicTactics = ExtractJsonValue(json, "basic_tactics");
            var attackCriteria = ExtractJsonValue(json, "attack_judgment_criteria");
            var continuousAttackCriteria = ExtractJsonValue(json, "continuous_attack_judgment_criteria");
            var defenseCriteria = ExtractJsonValue(json, "defense_judgment_criteria");
            var continuousDefenseCriteria = ExtractJsonValue(json, "continuous_defense_judgment_criteria");

            strategy.基本戦術 = ConvertTacticsToJapanese(basicTactics);
            strategy.攻撃時判断基準 = ConvertAttackCriteriaToJapanese(attackCriteria);
            strategy.攻撃継続時判断基準 = ConvertAttackCriteriaToJapanese(continuousAttackCriteria);
            strategy.防御時判断基準 = ConvertDefenseCriteriaToJapanese(defenseCriteria);
            strategy.連続防御時判断基準 = ConvertDefenseCriteriaToJapanese(continuousDefenseCriteria);

            return strategy;
        }

        /// <summary>
        /// JSON文字列から指定されたキーの値を抽出
        /// </summary>
        private static string ExtractJsonValue(string json, string key)
        {
            var keyPattern = $"\"{key}\"\\s*:\\s*\"([^\"]+)\"";
            var match = System.Text.RegularExpressions.Regex.Match(json, keyPattern);
            return match.Success ? match.Groups[1].Value : "";
        }

        /// <summary>
        /// 英語の戦術名を日本語に変換
        /// </summary>
        private static string ConvertTacticsToJapanese(string englishTactics)
        {
            return englishTactics switch
            {
                "Aggressive" => "攻撃型",
                "Defensive" => "防御型",
                "Adaptive" => "対応型",
                "Disruptive" => "攪乱型",
                "Endurance" => "持久型",
                _ => "対応型"
            };
        }

        /// <summary>
        /// 英語の攻撃判断基準を日本語に変換
        /// </summary>
        private static string ConvertAttackCriteriaToJapanese(string englishCriteria)
        {
            return englishCriteria switch
            {
                "Cumulative Probability Focus" => "累積確率重視",
                "Recent Pattern Focus" => "直近パターン重視",
                "Speed Focus" => "速度重視",
                "Return Focus" => "リターン重視",
                "Feint Focus" => "フェイント重視",
                "Distribution Focus" => "分散重視",
                "Energy Efficiency Focus" => "エネルギー効率重視",
                _ => "累積確率重視"
            };
        }

        /// <summary>
        /// 英語の防御判断基準を日本語に変換
        /// </summary>
        private static string ConvertDefenseCriteriaToJapanese(string englishCriteria)
        {
            return englishCriteria switch
            {
                "Cumulative Probability Focus" => "累積確率重視",
                "Recent Pattern Focus" => "直近パターン重視",
                "Energy Focus" => "エネルギー重視",
                "Counterattack" => "反撃",
                "Counter" => "カウンター",
                "Survival Focus" => "生存重視",
                "Distribution Focus" => "分散重視",
                "Evasion Focus" => "回避重視",
                _ => "累積確率重視"
            };
        }

        /// <summary>
        /// デフォルトの戦略データを生成
        /// </summary>
        public static NewStrategyData CreateDefault()
        {
            return new NewStrategyData
            {
                基本戦術 = "対応型",
                攻撃時判断基準 = "累積確率重視",
                攻撃継続時判断基準 = "直近パターン重視",
                防御時判断基準 = "累積確率重視",
                連続防御時判断基準 = "反撃"
            };
        }

        /// <summary>
        /// 攻撃的な戦略データを生成
        /// </summary>
        public static NewStrategyData CreateAggressive()
        {
            return new NewStrategyData
            {
                基本戦術 = "攻撃型",
                攻撃時判断基準 = "リターン重視",
                攻撃継続時判断基準 = "リターン重視",
                防御時判断基準 = "反撃",
                連続防御時判断基準 = "カウンター"
            };
        }

        /// <summary>
        /// 防御的な戦略データを生成
        /// </summary>
        public static NewStrategyData CreateDefensive()
        {
            return new NewStrategyData
            {
                基本戦術 = "防御型",
                攻撃時判断基準 = "速度重視",
                攻撃継続時判断基準 = "速度重視",
                防御時判断基準 = "生存重視",
                連続防御時判断基準 = "回避重視"
            };
        }

        /// <summary>
        /// エネルギー効率重視の戦略データを生成
        /// </summary>
        public static NewStrategyData CreateEnergyEfficient()
        {
            return new NewStrategyData
            {
                基本戦術 = "持久型",
                攻撃時判断基準 = "エネルギー効率重視",
                攻撃継続時判断基準 = "エネルギー効率重視",
                防御時判断基準 = "エネルギー重視",
                連続防御時判断基準 = "エネルギー重視"
            };
        }

        /// <summary>
        /// 戦術の妥当性を検証
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            var validBasicTactics = new[] { "攻撃型", "防御型", "対応型", "攪乱型", "持久型" };
            var validAttackCriteria = new[] {
                "累積確率重視", "直近パターン重視", "速度重視",
                "リターン重視", "フェイント重視", "分散重視", "エネルギー効率重視"
            };
            var validDefenseCriteria = new[] {
                "累積確率重視", "直近パターン重視", "エネルギー重視",
                "反撃", "カウンター", "生存重視", "分散重視", "回避重視"
            };

            if (!validBasicTactics.Contains(基本戦術))
            {
                errorMessage = $"無効な基本戦術: {基本戦術}";
                return false;
            }

            if (!validAttackCriteria.Contains(攻撃時判断基準))
            {
                errorMessage = $"無効な攻撃時判断基準: {攻撃時判断基準}";
                return false;
            }

            if (!validAttackCriteria.Contains(攻撃継続時判断基準))
            {
                errorMessage = $"無効な攻撃継続時判断基準: {攻撃継続時判断基準}";
                return false;
            }

            if (!validDefenseCriteria.Contains(防御時判断基準))
            {
                errorMessage = $"無効な防御時判断基準: {防御時判断基準}";
                return false;
            }

            if (!validDefenseCriteria.Contains(連続防御時判断基準))
            {
                errorMessage = $"無効な連続防御時判断基準: {連続防御時判断基準}";
                return false;
            }

            errorMessage = "";
            return true;
        }
    }
}