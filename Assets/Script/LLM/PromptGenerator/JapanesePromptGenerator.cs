using System;
using System.Linq;
using System.Text;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;
using static LLMDataArchitect.ActionTable;

namespace LLMDataArchitect.Test
{
    public class JapanesePromptGenerator : PromptGeneratorBase
    {
        public override string GeneratePromptByData(LLMInputData inputData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# 戦闘AI分析");
            prompt.AppendLine("以下のデータから戦況を分析し、最適な戦術を指定のJSON形式で出力。");
            prompt.AppendLine();

            // === 現在の状況 ===
            prompt.AppendLine("## 現在の状況");

            var myHp = inputData.MyData.Hp;
            var enemyHp = inputData.NPCData.Hp;
            var hpDiff = myHp - enemyHp;
            var myEnergy = inputData.MyData.Energy;
            var enemyEnergy = inputData.NPCData.Energy;
            var energyDiff = myEnergy - enemyEnergy;

            prompt.AppendLine($"- 【HP状況】自分:{myHp} 敵:{enemyHp} (HPは敵より{Math.Abs(hpDiff)}{(hpDiff >= 0 ? "多い" : "少ない")})");
            prompt.AppendLine($"- 【エネルギー状況】自分:{myEnergy} 敵:{enemyEnergy}（エネルギーは敵より{Math.Abs(energyDiff)}{(energyDiff >= 0 ? "多い" : "少ない")}）");
            prompt.AppendLine();

            // === 敵攻撃パターン ===
            prompt.AppendLine("## 敵攻撃パターン");

            // 直近パターン（RecentActionArrayから攻撃のみ抽出）
            if (inputData.RecentActionArray != null && inputData.RecentActionArray.Length > 0)
            {
                var attackActions = new[] { ActionState.弱攻撃, ActionState.強攻撃, ActionState.強攻撃キャンセル };
                var recentAttacks = inputData.RecentActionArray.Where(a => attackActions.Contains(a)).ToList();

                var attackCounts = recentAttacks.GroupBy(a => a)
                    .Select(g => $"{g.Key}: {g.Count()}回")
                    .ToList();

                prompt.AppendLine($"【直近】{string.Join(", ", attackCounts)}");
            }
            else
            {
                prompt.AppendLine("【直近】データなし");
            }

            // 累積パターン
            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"【累積】強攻撃:{inputData.ActionLog.StrongAttackPercentage * 100:F0}%, " +
                                 $"弱攻撃:{inputData.ActionLog.LightAttackPercentage * 100:F0}%, " +
                                 $"フェイント:{inputData.ActionLog.StrongAttackCancelPercentage * 100:F0}%");
            }
            else
            {
                prompt.AppendLine("【累積】データなし");
            }
            prompt.AppendLine();

            // === 敵防御パターン ===
            prompt.AppendLine("## 敵防御パターン");

            // 直近防御パターン
            if (inputData.RecentActionArray != null && inputData.RecentActionArray.Length > 0)
            {
                var defenseActions = new[] {
            ActionState.ガード, ActionState.弱攻撃ブロッキング, ActionState.強攻撃ブロッキング,
            ActionState.後ろ回避, ActionState.横回避, ActionState.前回避
        };
                var recentDefenses = inputData.RecentActionArray.Where(a => defenseActions.Contains(a)).ToList();

                var defenseCounts = recentDefenses.GroupBy(a => a)
                    .Select(g => $"{g.Key}: {g.Count()}回")
                    .ToList();

                prompt.AppendLine($"【直近】{string.Join(", ", defenseCounts)}");
            }
            else
            {
                prompt.AppendLine("【直近】データなし");
            }

            // 累積防御パターン
            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"【累積】ガード:{inputData.ActionLog.GuardPercentage * 100:F0}%, " +
                                 $"ブロッキング:{inputData.ActionLog.BlockingPercentage * 100:F0}%, " +
                                 $"回避:{(inputData.ActionLog.BackwardDodgePercentage + inputData.ActionLog.HorizontalDodgePercentage) * 100:F0}%");
            }
            else
            {
                prompt.AppendLine("【累積】データなし");
            }
            prompt.AppendLine();

            // === 前回判断後の戦闘結果 ===
            prompt.AppendLine("## 前回判断後の戦闘結果");

            float dealtDmg = inputData.HitSituations?.Sum(h => h.GetDamage) ?? 0;
            float takenDmg = inputData.EnemyHitSituations?.Sum(h => h.GetDamage) ?? 0;
            float balance = dealtDmg - takenDmg;

            prompt.AppendLine($"- 【ダメージ】 与ダメージ: {dealtDmg:F0} 被ダメージ: {takenDmg:F0} 収支: {balance:+0;-0;0}");
            prompt.AppendLine();

            // === 前回判断基準のフィードバック ===
            prompt.AppendLine("## 前回判断基準のフィードバック");

            if (inputData.LastStrategy != null)
            {
                // 各判断基準の評価（実際のデータがあれば使用、なければプレースホルダー）
                // 攻撃時判断基準
                string attackEval = EvaluateAttackCriteria(dealtDmg, "{攻撃成功回数}", "{攻撃失敗回数}", "{攻撃与ダメージ}");
                prompt.AppendLine($"- 【攻撃時判断基準】「{inputData.LastStrategy.攻撃時判断基準}」{attackEval}");

                // 連続攻撃時判断基準
                string continuousAttackEval = EvaluateAttackCriteria(dealtDmg, "{連続攻撃成功回数}", "{連続攻撃失敗回数}", "{連続攻撃与ダメージ}");
                prompt.AppendLine($"- 【連続攻撃時判断基準】「{inputData.LastStrategy.攻撃継続時判断基準}」{continuousAttackEval}");

                // 防御時判断基準
                string defenseEval = EvaluateDefenseCriteria(takenDmg, "{防御成功回数}", "{防御失敗回数}", "{防御被ダメージ}");
                prompt.AppendLine($"- 【防御時判断基準】「{inputData.LastStrategy.防御時判断基準}」{defenseEval}");

                // 連続防御時判断基準
                string continuousDefenseEval = EvaluateDefenseCriteria(takenDmg, "{連続防御成功回数}", "{連続防御失敗回数}", "{連続防御被ダメージ}");
                prompt.AppendLine($"- 【連続防御時判断基準】「{inputData.LastStrategy.連続防御時判断基準}」{continuousDefenseEval}");
            }
            else
            {
                prompt.AppendLine("前回判断データなし（初回）");
            }
            prompt.AppendLine();

            // === 前回戦術の評価 ===
            prompt.AppendLine("### 前回戦術の評価");
            prompt.AppendLine("前回選択した戦術と戦闘結果を照らし合わせ、効果を評価してください。");
            prompt.AppendLine();
            prompt.AppendLine("**戦況が悪化している場合:**");
            prompt.AppendLine("- 悪化の原因に応じて戦術を変更する");
            prompt.AppendLine("- 同じアプローチを続けることは避ける");
            prompt.AppendLine();
            prompt.AppendLine("**戦況が好転している場合:**");
            prompt.AppendLine("- 現在の戦術を継続するか、次の段階に進むかを判断");
            prompt.AppendLine("- リードを活かした選択肢を検討（リソース回復優先、さらなる攻勢、安定化など）");
            prompt.AppendLine();
            prompt.AppendLine("**戦況が変化していない場合:**");
            prompt.AppendLine("- 膠着の原因を分析する（互いに対策済み、決定打の不足など）");
            prompt.AppendLine("- 変化を生むための戦術変更を検討");
            prompt.AppendLine();
            prompt.AppendLine(GenerateFixedSectionJapanese());

            return prompt.ToString();
        }

        public override string GenerateRandomPrompt()
        {
            var randomSituation = (TestSituationType)UnityEngine.Random.Range(0, 5);
            var inputData = LLMInputData.CreateForTestSituation(randomSituation);
            return GeneratePromptByData(inputData);
        }

        /// <summary>
        /// 攻撃判断基準の評価テキストを生成
        /// </summary>
        private string EvaluateAttackCriteria(float totalDamage, string successCount, string failCount, string damage)
        {
            // 実際のデータで置き換え
            string evaluation;

            // プレースホルダーとして実装（実データがあれば条件分岐）
            if (damage == "{攻撃与ダメージ}" || damage == "{連続攻撃与ダメージ}")
            {
                // データがない場合はプレースホルダーのまま
                return $"成功:{successCount} 失敗:{failCount} 与ダメージ{damage} → {{評価}}";
            }

            float dmg = float.Parse(damage);
            if (dmg > 20)
                evaluation = "効果的";
            else if (dmg > 10)
                evaluation = "標準的";
            else
                evaluation = "効果薄い";

            return $"成功:{successCount} 失敗:{failCount} 与ダメージ{dmg:F0} → {evaluation}";
        }

        /// <summary>
        /// 防御判断基準の評価テキストを生成
        /// </summary>
        private string EvaluateDefenseCriteria(float totalDamage, string successCount, string failCount, string damage)
        {
            // 実際のデータで置き換え
            string evaluation;

            // プレースホルダーとして実装（実データがあれば条件分岐）
            if (damage == "{防御被ダメージ}" || damage == "{連続防御被ダメージ}")
            {
                // データがない場合はプレースホルダーのまま
                return $"成功:{successCount} 失敗:{failCount} 被ダメージ{damage} → {{評価}}";
            }

            float dmg = float.Parse(damage);
            if (dmg > 30)
                evaluation = "変更必須";
            else if (dmg > 15)
                evaluation = "改善余地あり";
            else
                evaluation = "許容範囲";

            return $"成功:{successCount} 失敗:{failCount} 被ダメージ{dmg:F0} → {evaluation}";
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
            prompt.AppendLine("- 速度重視: 低リスク、低リターン、回転率が高い、フェイントに有効");
            prompt.AppendLine("- リターン重視: 高リターン、高リスク");
            prompt.AppendLine("- フェイント重視: リスク最小、リターン無し、敵の反応を見る");
            prompt.AppendLine("- 分散重視: パターンを読まれないよう行動を散らす");
            prompt.AppendLine("- エネルギー効率重視: エネルギー回復を優先して行動しない");
            prompt.AppendLine();
            prompt.AppendLine("## 防御時判断基準(防御時判断指標)");
            prompt.AppendLine("- 累積確率重視: 全ての行動履歴から最も成功率の高い防御");
            prompt.AppendLine("- 直近パターン重視: 敵の直近の攻撃パターンから成功確率の高い行動を選択");
            prompt.AppendLine("- 反撃重視: 中リスク、中リターン、攻撃の主導権を奪う");
            prompt.AppendLine("- リターン重視: 成功時のリターンを重視");
            prompt.AppendLine("- リスク回避重視: 敵の最も攻撃力が大きい攻撃に重点的に対応");
            prompt.AppendLine("- カウンター重視: 攻撃頻度が高い相手へのメタ、失敗時リスク高");
            prompt.AppendLine("- 分散重視: 防御パターンを読まれないように散らす");
            prompt.AppendLine();
            prompt.AppendLine("## 出力形式");
            prompt.AppendLine("以下の構造のJsonデータの全てのプロパティの値を埋めて、文字列として出力する。");
            prompt.AppendLine("各プロパティには、前述の基本戦術タイプまたは判断基準から、状況に最も適した一つを選択して記入する。");
            prompt.AppendLine("プロパティのキーと値に絶対に抜けがあってはいけない。");
            prompt.AppendLine();
            prompt.AppendLine("{");
            prompt.AppendLine("  \"分析結果\": \"\",");
            prompt.AppendLine("  \"基本戦術\": \"<基本戦術から選択>\",");
            prompt.AppendLine("  \"攻撃時判断基準\": \"<攻撃時判断指標から選択>\",");
            prompt.AppendLine("  \"連続攻撃時判断基準\": \"<攻撃時判断指標から選択>\",");
            prompt.AppendLine("  \"防御時判断基準\": \"<防御時判断指標から選択>\",");
            prompt.AppendLine("  \"連続防御時判断基準\": \"<防御時判断指標から選択>\"");
            prompt.AppendLine("}");
            prompt.AppendLine();
            prompt.AppendLine("## キー説明");
            prompt.AppendLine("- 分析結果: 判断理由、必要な対応、反映内容を30文字以内で簡潔に記載");
            prompt.AppendLine("- 攻撃時判断基準: 攻撃の際の判断基準");
            prompt.AppendLine("- 連続攻撃時判断基準: 攻撃が二回以上連続した際の判断基準");
            prompt.AppendLine("- 防御時判断基準: 敵攻撃への防御の際の判断基準");
            prompt.AppendLine("- 連続防御時判断基準: 敵攻撃が二回以上連続した際の防御の判断基準");
            prompt.AppendLine();
            return prompt.ToString();
        }

    }
}