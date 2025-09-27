using System;
using System.Text;
using System.Linq;

namespace LLMDataArchitectTest
{
    /// <summary>
    /// 最終版プロンプト形式でのSystemPrompt生成クラス
    /// </summary>
    public class SystemPromptGenerator
    {
        /// <summary>
        /// 日本語版の動的部分（状況判定・履歴分析・入力データ）を生成
        /// </summary>
        public string GenerateDynamicSectionJapanese(LLMInputData inputData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# 戦闘AI分析");
            prompt.AppendLine();
            prompt.AppendLine("入力データを分析し、戦略をJSON形式で出力せよ。");
            prompt.AppendLine("なお出力は指定した構造のJsonデータのみとし、それ以外のデータは絶対に一切含めないこと。");
            prompt.AppendLine();

            // 状況判定セクション
            prompt.AppendLine("## 必須：状況判定");
            prompt.AppendLine();
            prompt.AppendLine("以下を計算し記載：");

            var myHpPercent = (float)inputData.MyData.Hp / inputData.MyData.MaxHp * 100f;
            var enemyHpPercent = (float)inputData.EnemyData.Hp / inputData.EnemyData.MaxHp * 100f;
            var hpDiff = myHpPercent - enemyHpPercent;
            var myEnergyPercent = (float)inputData.MyData.Energy / inputData.MyData.MaxEnergy * 100f;
            var enemyEnergyPercent = (float)inputData.EnemyData.Energy / inputData.EnemyData.MaxEnergy * 100f;

            prompt.AppendLine($"- 自分HP割合 {myHpPercent:F0}%");
            prompt.AppendLine($"- 敵HP割合 {enemyHpPercent:F0}%");
            prompt.AppendLine($"- 体力差 {hpDiff:+0;-0;0}");
            prompt.AppendLine($"- 自分エネルギー割合 {myEnergyPercent:F0}%");
            prompt.AppendLine($"- 敵エネルギー割合 {enemyEnergyPercent:F0}%");
            prompt.AppendLine();

            // 履歴分析セクション
            prompt.AppendLine("## 必須：履歴分析");
            prompt.AppendLine();
            prompt.AppendLine("以下を記載：");

            if (inputData.HitSituations != null && inputData.HitSituations.Length > 0)
            {
                var maxHit = inputData.HitSituations.OrderByDescending(h => h.GetDamage).First();
                prompt.AppendLine($"- 効果的だった自分の攻撃: {maxHit.HitState}（敵{maxHit.HitType}時に{maxHit.GetDamage:F0}ダメージ）");
            }
            else
            {
                prompt.AppendLine("- 効果的だった自分の攻撃: データなし");
            }

            if (inputData.EnemyHitSituations != null && inputData.EnemyHitSituations.Length > 0)
            {
                var maxDamageReceived = inputData.EnemyHitSituations.OrderByDescending(h => h.GetDamage).First();
                prompt.AppendLine($"- 失敗した自分の防御: {maxDamageReceived.HitState}（敵{maxDamageReceived.HitType}時に{maxDamageReceived.GetDamage:F0}被ダメージ）");

                var totalEnemyDamage = inputData.EnemyHitSituations.Sum(h => h.GetDamage);
                prompt.AppendLine($"- 自分の直近の被ダメージ合計{totalEnemyDamage:F0}ダメージ");
            }
            else
            {
                prompt.AppendLine("- 失敗した自分の防御: データなし");
                prompt.AppendLine("- 自分の直近の被ダメージ合計0ダメージ");
            }

            if (inputData.HitSituations != null && inputData.HitSituations.Length > 0)
            {
                var totalMyDamage = inputData.HitSituations.Sum(h => h.GetDamage);
                prompt.AppendLine($"- 敵の直近の被ダメージ合計{totalMyDamage:F0}ダメージ");
            }
            else
            {
                prompt.AppendLine("- 敵の直近の被ダメージ合計0ダメージ");
            }
            prompt.AppendLine();

            // 入力データ
            prompt.AppendLine("## 入力データ");
            prompt.AppendLine();
            prompt.AppendLine("```json");
            prompt.Append(LLMInputData.ToJson(inputData, true));
            prompt.AppendLine("```");
            prompt.AppendLine();

            return prompt.ToString();
        }

        /// <summary>
        /// 日本語版の固定部分（システムプロンプト以降）を生成
        /// </summary>
        public string GenerateFixedSectionJapanese()
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("---");
            prompt.AppendLine("## 出力指示");
            prompt.AppendLine("- 必ず有効なJSON形式のみで応答してください");
            prompt.AppendLine("- 説明文、コメント、マークダウン形式は一切含めないでください");
            prompt.AppendLine("- コードブロック（```json）やその他の装飾は使用しないでください");
            prompt.AppendLine("- 応答全体が{で始まり}で終わる解析可能なJSONである必要があります");
            prompt.AppendLine("- すべての文字列値は適切にエスケープし、クォートで囲んでください");
            prompt.AppendLine("- 出力するJsonのプロパティに有効な値が入っていなければエラーとみなします");
            prompt.AppendLine();

            prompt.AppendLine("## 分析観点");
            prompt.AppendLine("1. 敵の行動パターン（攻撃頻度、防御頻度、移動傾向）");
            prompt.AppendLine("2. 体力・エネルギー効率（持久戦か短期決戦か）");
            prompt.AppendLine("3. 戦術履歴の成功・失敗（効果的な行動、被弾要因）");
            prompt.AppendLine();

            prompt.AppendLine("## 判断優先順位");
            prompt.AppendLine("1. 生存確保（体力・エネルギー管理、致命傷回避）");
            prompt.AppendLine("2. 有効打創出（敵の行動パターン活用）");
            prompt.AppendLine("3. 戦術優位確立（長期的勝利条件整備）");
            prompt.AppendLine();

            prompt.AppendLine("## 基本戦術の一覧");
            prompt.AppendLine("**「防御型」**:自エネルギー減少、自分が低HP、直近の被ダメージが多い等の理由で不利");
            prompt.AppendLine("**「攻撃型」**:防御型と同じ観点で自分が優位");
            prompt.AppendLine("**「対応型」**:デフォルト");
            prompt.AppendLine("**「攪乱型」**:敵の攻撃頻度が低く、守りを固めている");
            prompt.AppendLine();

            prompt.AppendLine("## 出力形式");
            prompt.AppendLine("以下の構造のJsonデータの全てのプロパティの値を埋めて、文字列として出力する。");
            prompt.AppendLine("行動テーブルは状況をキーとして（例：敵攻撃体勢）、対応する行動（例：ガード）を文字列の値として記入する。");
            prompt.AppendLine("プロパティのキーと値に絶対に抜けがあってはいけない。");
            prompt.AppendLine();
            prompt.AppendLine("{");
            prompt.AppendLine("  \"結論\": \"\",");
            prompt.AppendLine("  \"理由\": \"\",");
            prompt.AppendLine("  \"基本戦術\": \"<基本戦術の値>\",");
            prompt.AppendLine("  \"行動テーブル\": {");
            prompt.AppendLine("    \"敵攻撃体勢\": \"<防御択の値>\",");
            prompt.AppendLine("    \"敵待機状態\": \"<攻撃択の値>\",");
            prompt.AppendLine("    \"自分微有利状況\": \"<攻撃択の値>\",");
            prompt.AppendLine("    \"自分有利状況\": \"<攻撃択の値>\",");
            prompt.AppendLine("    \"自分微不利状況\": \"<防御択の値>\",");
            prompt.AppendLine("    \"自分不利状況\": \"<防御択の値>\",");
            prompt.AppendLine("    \"自分強攻撃ヒット\": \"<攻撃択の値>\",");
            prompt.AppendLine("    \"敵強攻撃ヒット\": \"<防御択の値>\"");
            prompt.AppendLine("  }");
            prompt.AppendLine("}");
            prompt.AppendLine();

            prompt.AppendLine("## 行動テーブルのキー（状況）");
            prompt.AppendLine("- **敵攻撃体勢**: 敵が攻撃（弱攻撃or強攻撃or攻撃キャンセル）した時の対応行動。防御択から一つ選択。");
            prompt.AppendLine("- **敵待機状態**: 敵が守りを固めている時の行動。攻撃択から一つ選択");
            prompt.AppendLine("- **自分微有利状況**: わずかに有利フレームを持った状況の追撃。攻撃択から一つ選択");
            prompt.AppendLine("- **自分有利状況**: 攻撃択から一つ選択。弱攻撃は確定行動。");
            prompt.AppendLine("- **自分微不利状況**: わずかに不利フレームを持った状況の行動。防御択から一つ選択。");
            prompt.AppendLine("- **自分不利状況**: 自分がフレーム不利な状況での対応行動。防御択から一つ選択。");
            prompt.AppendLine("- **自分強攻撃ヒット**: 敵行動パターンを根拠に攻撃択から一つ選択。");
            prompt.AppendLine("- **敵強攻撃ヒット**: 敵行動パターンを根拠に防御択から一つ選択。");
            prompt.AppendLine();

            prompt.AppendLine("## 行動テーブルの値（行動）");
            prompt.AppendLine("以下の攻撃択と防御択、いずれかに属する値のみを必ず使用する。");
            prompt.AppendLine();

            prompt.AppendLine("### 攻撃択（有利・待機時に使用）");
            prompt.AppendLine();
            prompt.AppendLine("- **「弱攻撃」**: 基本攻撃、確実にダメージ");
            prompt.AppendLine("- **「強攻撃」**: 高威力、敵待機時に有効");
            prompt.AppendLine("- **「強攻撃キャンセル」**: フェイント、守備的な敵を崩す");
            prompt.AppendLine("- **「弱攻撃ブロッキング」**: 敵の反撃を予想してカウンター");
            prompt.AppendLine("- **「前回避」**: 接近、敵が守備的な時");
            prompt.AppendLine("- **「ガード」**: 慎重に様子見、エネルギー不足時");
            prompt.AppendLine();

            prompt.AppendLine("### 防御択（不利・敵攻撃時に使用）");
            prompt.AppendLine();
            prompt.AppendLine("- **「ガード」**: 最も安全、エネルギー回復");
            prompt.AppendLine("- **「後ろ回避」**: 強力な回避だが成功時リターンがない、不利状況の緊急回避のみ");
            prompt.AppendLine("- **「横回避攻撃」**: 攻撃的防御、回避しつつ反撃");
            prompt.AppendLine("- **「横回避」**: 攻撃回避、安全重視");
            prompt.AppendLine("- **「強攻撃ブロッキング」**: 敵強攻撃を予想");
            prompt.AppendLine("- **「弱攻撃ブロッキング」**: 敵弱攻撃を予想");
            prompt.AppendLine("- **「弱攻撃」**: 素早く主導権奪取");
            prompt.AppendLine();

            return prompt.ToString();
        }

        /// <summary>
        /// 日本語版の完全なプロンプトを生成
        /// </summary>
        public string GenerateFullPromptJapanese(LLMInputData inputData)
        {
            return GenerateDynamicSectionJapanese(inputData) + GenerateFixedSectionJapanese();
        }

        /// <summary>
        /// 英語版の動的部分（状況判定・履歴分析・入力データ）を生成
        /// </summary>
        public string GenerateDynamicSectionEnglish(LLMInputDataEnglish inputData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# Battle AI Analysis");
            prompt.AppendLine();
            prompt.AppendLine("Analyze the input data and output the strategy in JSON format.");
            prompt.AppendLine("The output must be ONLY the JSON data with the specified structure, and absolutely no other data must be included.");
            prompt.AppendLine();

            // Situation Assessment Section
            prompt.AppendLine("## Required: Situation Assessment");
            prompt.AppendLine();
            prompt.AppendLine("Calculate and record the following:");

            var myHpPercent = (float)inputData.MyData.Hp / inputData.MyData.MaxHp * 100f;
            var enemyHpPercent = (float)inputData.EnemyData.Hp / inputData.EnemyData.MaxHp * 100f;
            var hpDiff = myHpPercent - enemyHpPercent;
            var myEnergyPercent = (float)inputData.MyData.Energy / inputData.MyData.MaxEnergy * 100f;
            var enemyEnergyPercent = (float)inputData.EnemyData.Energy / inputData.EnemyData.MaxEnergy * 100f;

            prompt.AppendLine($"- My HP Percentage {myHpPercent:F0}%");
            prompt.AppendLine($"- Enemy HP Percentage {enemyHpPercent:F0}%");
            prompt.AppendLine($"- HP Difference {hpDiff:+0;-0;0}");
            prompt.AppendLine($"- My Energy Percentage {myEnergyPercent:F0}%");
            prompt.AppendLine($"- Enemy Energy Percentage {enemyEnergyPercent:F0}%");
            prompt.AppendLine();

            // History Analysis Section
            prompt.AppendLine("## Required: History Analysis");
            prompt.AppendLine();
            prompt.AppendLine("Record the following:");

            if (inputData.HitSituations != null && inputData.HitSituations.Length > 0)
            {
                var maxHit = inputData.HitSituations.OrderByDescending(h => h.GetDamage).First();
                prompt.AppendLine($"- Effective attack: {maxHit.SituationType} (dealt {maxHit.GetDamage:F0} damage when enemy {maxHit.EnemyActionType})");
            }
            else
            {
                prompt.AppendLine("- Effective attack: No data");
            }

            if (inputData.EnemyHitSituations != null && inputData.EnemyHitSituations.Length > 0)
            {
                var maxDamageReceived = inputData.EnemyHitSituations.OrderByDescending(h => h.GetDamage).First();
                prompt.AppendLine($"- Failed defense: {maxDamageReceived.SituationType} (took {maxDamageReceived.GetDamage:F0} damage when enemy {maxDamageReceived.EnemyActionType})");

                var totalEnemyDamage = inputData.EnemyHitSituations.Sum(h => h.GetDamage);
                prompt.AppendLine($"- Total recent damage taken: {totalEnemyDamage:F0} damage");
            }
            else
            {
                prompt.AppendLine("- Failed defense: No data");
                prompt.AppendLine("- Total recent damage taken: 0 damage");
            }

            if (inputData.HitSituations != null && inputData.HitSituations.Length > 0)
            {
                var totalMyDamage = inputData.HitSituations.Sum(h => h.GetDamage);
                prompt.AppendLine($"- Total recent damage dealt to enemy: {totalMyDamage:F0} damage");
            }
            else
            {
                prompt.AppendLine("- Total recent damage dealt to enemy: 0 damage");
            }
            prompt.AppendLine();

            // Input Data
            prompt.AppendLine("## Input Data");
            prompt.AppendLine();
            prompt.AppendLine("```json");
            prompt.Append(LLMInputDataEnglish.ToJson(inputData, true));
            prompt.AppendLine("```");
            prompt.AppendLine();

            return prompt.ToString();
        }

        /// <summary>
        /// 英語版の固定部分（システムプロンプト以降）を生成
        /// </summary>
        /// <summary>
        /// 英語版の固定部分（システムプロンプト以降）を生成 - 改善版
        /// </summary>
        public string GenerateFixedSectionEnglish()
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("---");
            prompt.AppendLine("## Output Instructions");
            prompt.AppendLine("- You MUST respond ONLY with valid JSON format");
            prompt.AppendLine("- Do NOT include any explanatory text, comments, or markdown formatting");
            prompt.AppendLine("- Do NOT use code blocks (```json) or any other decorations");
            prompt.AppendLine("- Your entire response must be parseable JSON starting with { and ending with }");
            prompt.AppendLine("- All string values must be properly escaped and quoted");
            prompt.AppendLine("- If the output JSON properties do not contain valid values, it will be considered an error");
            prompt.AppendLine();

            prompt.AppendLine("## Analysis Perspectives");
            prompt.AppendLine("1. Enemy behavior patterns (attack frequency, defense frequency, movement tendencies)");
            prompt.AppendLine("2. HP/energy efficiency (war of attrition vs. short-term decisive battle)");
            prompt.AppendLine("3. Tactical history successes/failures (effective actions, hit damage factors)");
            prompt.AppendLine();

            prompt.AppendLine("## Judgment Priority");
            prompt.AppendLine("1. Survival assurance (HP/energy management, fatal damage avoidance)");
            prompt.AppendLine("2. Creating effective hits (utilizing enemy behavior patterns)");
            prompt.AppendLine("3. Establishing tactical advantage (preparing long-term victory conditions)");
            prompt.AppendLine();

            prompt.AppendLine("## List of Basic Tactics");
            prompt.AppendLine("**\"Defensive\"**: Disadvantaged due to reasons such as my energy decrease, my low HP, high recent damage taken");
            prompt.AppendLine("**\"Aggressive\"**: Advantageous from the same perspective as defensive");
            prompt.AppendLine("**\"Adaptive\"**: Default");
            prompt.AppendLine("**\"Disruptive\"**: Enemy has low attack frequency and is solidifying their defense");
            prompt.AppendLine();

            prompt.AppendLine("## Output Format");
            prompt.AppendLine("Fill in all property values of the JSON data with the following structure and output as a string.");
            prompt.AppendLine("The action table uses situations as keys (e.g.: enemy_attack_stance) and enters corresponding actions (e.g.: Guard) as string values.");
            prompt.AppendLine("There must absolutely never be any missing property keys or values.");
            prompt.AppendLine();
            prompt.AppendLine("{");
            prompt.AppendLine("  \"conclusion\": \"\",");
            prompt.AppendLine("  \"reasoning\": \"\",");
            prompt.AppendLine("  \"basic_tactics\": \"<basic_tactics value>\",");
            prompt.AppendLine("  \"action_table\": {");
            prompt.AppendLine("    \"enemy_attack_stance\": \"<defensive option value>\",");
            prompt.AppendLine("    \"enemy_standby_state\": \"<offensive option value>\",");
            prompt.AppendLine("    \"my_slight_advantage\": \"<offensive option value>\",");
            prompt.AppendLine("    \"my_advantage\": \"<offensive option value>\",");
            prompt.AppendLine("    \"my_slight_disadvantage\": \"<defensive option value>\",");
            prompt.AppendLine("    \"my_disadvantage\": \"<defensive option value>\",");
            prompt.AppendLine("    \"my_heavy_attack_hit\": \"<offensive option value>\",");
            prompt.AppendLine("    \"enemy_heavy_attack_hit\": \"<defensive option value>\"");
            prompt.AppendLine("  }");
            prompt.AppendLine("}");
            prompt.AppendLine();

            prompt.AppendLine("## Action Table Keys (Situations)");
            prompt.AppendLine("- **enemy_attack_stance**: Response action when enemy attacks (light attack or heavy attack or attack cancel). Select one from defensive options.");
            prompt.AppendLine("- **enemy_standby_state**: Action when enemy is solidifying their defense. Select one from offensive options.");
            prompt.AppendLine("- **my_slight_advantage**: Follow-up attack in situation with slight advantage frames. Select one from offensive options.");
            prompt.AppendLine("- **my_advantage**: Select one from offensive options. Light attack is a guaranteed action.");
            prompt.AppendLine("- **my_slight_disadvantage**: Action in situation with slight disadvantage frames. Select one from defensive options.");
            prompt.AppendLine("- **my_disadvantage**: Response action in situation where I have frame disadvantage. Select one from defensive options.");
            prompt.AppendLine("- **my_heavy_attack_hit**: Select one from offensive options based on enemy behavior pattern.");
            prompt.AppendLine("- **enemy_heavy_attack_hit**: Select one from defensive options based on enemy behavior pattern.");
            prompt.AppendLine();

            prompt.AppendLine("## Action Table Values (Actions)");
            prompt.AppendLine("You must always use only values that belong to either the offensive options or defensive options below.");
            prompt.AppendLine();

            prompt.AppendLine("### Offensive Options (use when advantageous/standby)");
            prompt.AppendLine();
            prompt.AppendLine("- **\"Light Attack\"**: Basic attack, reliably deals damage");
            prompt.AppendLine("- **\"Heavy Attack\"**: High power, effective when enemy is on standby");
            prompt.AppendLine("- **\"Heavy Attack Cancel\"**: Feint, breaks down defensive enemies");
            prompt.AppendLine("- **\"Light Attack Blocking\"**: Counter while anticipating enemy's counterattack");
            prompt.AppendLine("- **\"Forward Dodge\"**: Approach, when enemy is defensive");
            prompt.AppendLine("- **\"Guard\"**: Cautiously observe situation, when energy is insufficient");
            prompt.AppendLine();

            prompt.AppendLine("### Defensive Options (use when disadvantaged/enemy attacking)");
            prompt.AppendLine();
            prompt.AppendLine("- **\"Guard\"**: Safest option, recovers energy");
            prompt.AppendLine("- **\"Backward Dodge\"**: Powerful evasion but no return on success, only for emergency evasion in disadvantaged situations");
            prompt.AppendLine("- **\"Horizontal Dodge Attack\"**: Aggressive defense, evade while counterattacking");
            prompt.AppendLine("- **\"Horizontal Dodge\"**: Attack evasion, safety-focused");
            prompt.AppendLine("- **\"Heavy Attack Blocking\"**: Anticipate enemy heavy attack");
            prompt.AppendLine("- **\"Light Attack Blocking\"**: Anticipate enemy light attack");
            prompt.AppendLine("- **\"Light Attack\"**: Quickly seize initiative");
            prompt.AppendLine();

            return prompt.ToString();
        }

        /// <summary>
        /// 英語版の完全なプロンプトを生成
        /// </summary>
        public string GenerateFullPromptEnglish(LLMInputDataEnglish inputData)
        {
            return GenerateDynamicSectionEnglish(inputData) + GenerateFixedSectionEnglish();
        }
    }
}