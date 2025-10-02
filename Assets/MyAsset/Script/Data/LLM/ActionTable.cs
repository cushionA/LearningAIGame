using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace LLMDataArchitect
{
    /// <summary>
    /// 新プロンプト対応の行動テーブル
    /// </summary>
    public class ActionTable
    {
        [JsonProperty("敵攻撃体勢")]
        public string? 敵攻撃体勢 { get; set; }

        [JsonProperty("敵待機状態")]
        public string? 敵待機状態 { get; set; }

        [JsonProperty("自分微有利状況")]
        public string? 自分微有利状況 { get; set; }

        [JsonProperty("自分有利状況")]
        public string? 自分有利状況 { get; set; }

        [JsonProperty("自分微不利状況")]
        public string? 自分微不利状況 { get; set; }

        [JsonProperty("自分不利状況")]
        public string? 自分不利状況 { get; set; }

        [JsonProperty("自分強攻撃ヒット")]
        public string? 自分強攻撃ヒット { get; set; }

        [JsonProperty("敵強攻撃ヒット")]
        public string? 敵強攻撃ヒット { get; set; }

        /// <summary>
        /// デフォルトの行動テーブルを作成
        /// </summary>
        public static ActionTable CreateDefault()
        {
            return new ActionTable
            {
                敵攻撃体勢 = "ガード",
                敵待機状態 = "弱攻撃",
                自分微有利状況 = "弱攻撃",
                自分有利状況 = "強攻撃",
                自分微不利状況 = "ガード",
                自分不利状況 = "後ろ回避",
                自分強攻撃ヒット = "弱攻撃",
                敵強攻撃ヒット = "後ろ回避"
            };
        }

        /// <summary>
        /// 攻撃的な行動テーブルを作成（優勢時用）
        /// </summary>
        public static ActionTable CreateAggressive()
        {
            return new ActionTable
            {
                敵攻撃体勢 = "強攻撃",
                敵待機状態 = "強攻撃",
                自分微有利状況 = "強攻撃",
                自分有利状況 = "強攻撃",
                自分微不利状況 = "弱攻撃",
                自分不利状況 = "弱攻撃",
                自分強攻撃ヒット = "強攻撃",
                敵強攻撃ヒット = "強攻撃"
            };
        }

        /// <summary>
        /// 守備的な行動テーブルを作成（劣勢時用）
        /// </summary>
        public static ActionTable CreateDefensive()
        {
            return new ActionTable
            {
                敵攻撃体勢 = "後ろ回避",
                敵待機状態 = "ガード",
                自分微有利状況 = "ガード",
                自分有利状況 = "弱攻撃",
                自分微不利状況 = "後ろ回避",
                自分不利状況 = "後ろ回避",
                自分強攻撃ヒット = "ガード",
                敵強攻撃ヒット = "後ろ回避"
            };
        }

        /// <summary>
        /// エネルギー節約重視の行動テーブルを作成（エネルギー不足時用）
        /// </summary>
        public static ActionTable CreateEnergySaving()
        {
            return new ActionTable
            {
                敵攻撃体勢 = "ガード",
                敵待機状態 = "ガード",
                自分微有利状況 = "ガード",
                自分有利状況 = "弱攻撃",
                自分微不利状況 = "ガード",
                自分不利状況 = "ガード",
                自分強攻撃ヒット = "ガード",
                敵強攻撃ヒット = "ガード"
            };
        }

        /// <summary>
        /// 回避重視の行動テーブルを作成（体力危険時用）
        /// </summary>
        public static ActionTable CreateEvasive()
        {
            return new ActionTable
            {
                敵攻撃体勢 = "後ろ回避",
                敵待機状態 = "後ろ回避",
                自分微有利状況 = "後ろ回避",
                自分有利状況 = "横回避",
                自分微不利状況 = "後ろ回避",
                自分不利状況 = "後ろ回避",
                自分強攻撃ヒット = "後ろ回避",
                敵強攻撃ヒット = "後ろ回避"
            };
        }

        /// <summary>
        /// 状況に応じた行動テーブルを作成
        /// </summary>
        /// <param name="situationType">テスト状況の種類</param>
        /// <returns>適切な行動テーブル</returns>
        public static ActionTable CreateForSituation(TestSituationType situationType)
        {
            return situationType switch
            {
                TestSituationType.優勢 => CreateAggressive(),
                TestSituationType.劣勢 => CreateDefensive(),
                TestSituationType.エネルギー不足 => CreateEnergySaving(),
                TestSituationType.体力危険 => CreateEvasive(),
                _ => CreateDefault()
            };
        }

        /// <summary>
        /// TestSituationType列挙型
        /// </summary>
        public enum TestSituationType
        {
            優勢,      // 自分有利
            拮抗,      // 互角
            劣勢,      // 敵有利
            エネルギー不足, // エネルギー危機
            体力危険    // 体力危機
        }

        /// <summary>
        /// 行動テーブルを検証（全ての行動が有効な選択肢かチェック）
        /// </summary>
        /// <returns>検証結果のメッセージ</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var validActions = new HashSet<string>
        {
            "後ろ回避", "横回避", "前回避", "ガード", "ブロッキング",
            "弱攻撃", "強攻撃", "強攻撃キャンセル", "横回避攻撃", "前回避攻撃",
            "弱攻撃ブロッキング", "強攻撃ブロッキング"
        };

            CheckAction(nameof(敵攻撃体勢), 敵攻撃体勢, validActions, errors);
            CheckAction(nameof(敵待機状態), 敵待機状態, validActions, errors);
            CheckAction(nameof(自分微有利状況), 自分微有利状況, validActions, errors);
            CheckAction(nameof(自分有利状況), 自分有利状況, validActions, errors);
            CheckAction(nameof(自分微不利状況), 自分微不利状況, validActions, errors);
            CheckAction(nameof(自分不利状況), 自分不利状況, validActions, errors);
            CheckAction(nameof(自分強攻撃ヒット), 自分強攻撃ヒット, validActions, errors);
            CheckAction(nameof(敵強攻撃ヒット), 敵強攻撃ヒット, validActions, errors);

            return errors;
        }

        private void CheckAction(string fieldName, string? action, HashSet<string> validActions, List<string> errors)
        {
            if (string.IsNullOrEmpty(action))
            {
                errors.Add($"{fieldName} が設定されていません。");
            }
            else if (!validActions.Contains(action))
            {
                errors.Add($"{fieldName} の値 '{action}' は有効な行動ではありません。");
            }
        }

        /// <summary>
        /// 行動テーブルの統計情報を取得
        /// </summary>
        /// <returns>統計情報</returns>
        public ActionTableStats GetStats()
        {
            var actions = new[] { 敵攻撃体勢, 敵待機状態, 自分微有利状況, 自分有利状況,
                             自分微不利状況, 自分不利状況, 自分強攻撃ヒット, 敵強攻撃ヒット };

            var stats = new ActionTableStats();

            foreach (var action in actions.Where(a => !string.IsNullOrEmpty(a)))
            {
                switch (action)
                {
                    case "弱攻撃":
                    case "強攻撃":
                    case "強攻撃キャンセル":
                    case "横回避攻撃":
                    case "前回避攻撃":
                    case "弱攻撃ブロッキング":
                    case "強攻撃ブロッキング":
                        stats.AttackActionsCount++;
                        break;
                    case "後ろ回避":
                    case "横回避":
                    case "前回避":
                    case "ガード":
                    case "ブロッキング":
                        stats.DefenseActionsCount++;
                        break;
                }
            }

            stats.TotalActions = actions.Count(a => !string.IsNullOrEmpty(a));
            stats.AttackRatio = stats.TotalActions > 0 ? (float)stats.AttackActionsCount / stats.TotalActions : 0f;
            stats.DefenseRatio = stats.TotalActions > 0 ? (float)stats.DefenseActionsCount / stats.TotalActions : 0f;

            return stats;
        }
    }
}