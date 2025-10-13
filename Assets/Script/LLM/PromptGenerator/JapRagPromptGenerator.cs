using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;
using static LLMDataArchitect.ActionTable;

namespace LLMDataArchitect.Test
{
    public class JapRagPromptGenerator : PromptGeneratorBase
    {
        public override string GeneratePromptByData(LLMInputData inputData)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# 戦闘AI分析");
            prompt.AppendLine("以下のデータから戦況を分析し、最適な戦術を指定のJSON形式で出力。");
            prompt.AppendLine();

            // === 現在の状況 ===
            prompt.AppendLine("## 現在の状況");

            var myHp = inputData.PlayerData.Hp;
            var enemyHp = inputData.NPCData.Hp;
            var hpDiff = myHp - enemyHp;
            var myEnergy = inputData.PlayerData.Energy;
            var enemyEnergy = inputData.NPCData.Energy;
            var energyDiff = myEnergy - enemyEnergy;

            prompt.AppendLine($"- 【HP状況】自分:{myHp} 敵:{enemyHp} (HPは敵より{Math.Abs(hpDiff)}{(hpDiff >= 0 ? "多い" : "少ない")})");
            prompt.AppendLine($"- 【エネルギー状況】自分:{myEnergy} 敵:{enemyEnergy}（エネルギーは敵より{Math.Abs(energyDiff)}{(energyDiff >= 0 ? "多い" : "少ない")}）");
            prompt.AppendLine();

            // === 敵攻撃パターン ===
            prompt.AppendLine("## 敵攻撃パターン");

            // 直近パターン（RecentActionArrayから攻撃のみ抽出）
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

                prompt.AppendLine($"【直近攻撃パターン】弱攻撃: {lightAttackCount}回, 強攻撃: {strongAttackCount}回, 強攻撃キャンセル: {strongAttackCancelCount}回");
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

                prompt.AppendLine($"【直近防御パターン】横回避: {horizontalDodgeCount},後ろ回避: {backwardDodgeCount}, ブロッキング: {blockingCount}回, ガード: {guardCount}回");
            }
            else
            {
                prompt.AppendLine("【直近】データなし");
            }

            // 累積防御パターン
            if (inputData.ActionLog != null)
            {
                prompt.AppendLine($"【累積】カウンター:{inputData.ActionLog.HorizontalDodgeAttackPercentage * 100:F0}%, " +
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

            float dealtDmg = inputData.NPCLog.HitDamage;
            float takenDmg = inputData.NPCLog.TakeDamage;
            float balance = dealtDmg - takenDmg;

            prompt.AppendLine($"- 【ダメージ】 与ダメージ: {dealtDmg:F0} 被ダメージ: {takenDmg:F0} 収支: {balance:+0;-0;0}");
            prompt.AppendLine();

            // === 前回判断基準のフィードバック ===
            prompt.AppendLine("## 前回判断基準のフィードバック");

            if (inputData.CurrentStrategy != null)
            {
                // 各判断基準の評価（実際のデータがあれば使用、なければプレースホルダー）
                // 攻撃時判断基準
                prompt.AppendLine(inputData.StrategyResult.GetAllConditionEvaluationsJapanese());
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
            return prompt.ToString();
        }

        /// <summary>
        /// グラマーを生成する
        /// </summary>
        /// <returns></returns>
        public override string GenerateGrammar()
        {
            return @"
root ::= object
object ::= ""{"" 
  ws ""\"" ""分析結果"" ""\"" "":"" ws string "",""
  ws ""\"" ""基本戦術"" ""\"" "":"" ws basic_tactic_value "",""
  ws ""\"" ""攻撃時判断基準"" ""\"" "":"" ws attack_criteria_value "",""
  ws ""\"" ""連続攻撃時判断基準"" ""\"" "":"" ws attack_criteria_value "",""
  ws ""\"" ""防御時判断基準"" ""\"" "":"" ws defense_criteria_value "",""
  ws ""\"" ""連続防御時判断基準"" ""\"" "":"" ws defense_criteria_value
  ws ""}""

basic_tactic_value ::= ""\"" (""攻撃型"" | ""防御型"" | ""対応型"" | ""攪乱型"" | ""持久型"") ""\""

attack_criteria_value ::= ""\"" (""累積確率重視"" | ""直近パターン重視"" | ""速度重視"" | ""リターン重視"" | ""フェイント重視"" | ""分散重視"" | ""エネルギー効率重視"") ""\""

defense_criteria_value ::= ""\"" (""累積確率重視"" | ""直近パターン重視"" | ""反撃重視"" | ""リターン重視"" | ""リスク回避重視"" | ""カウンター重視"" | ""分散重視"") ""\""

string ::= ""\"" ([^""\\\x00-\x1f] | ""\\"" ([""\\/bfnrt] | ""u"" [0-9a-fA-F]{4})) * ""\""
ws ::= [ \t\n\r]*
";
        }

    }
}