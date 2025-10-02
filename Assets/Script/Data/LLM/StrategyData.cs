using Newtonsoft.Json;
using UnityEngine;

namespace LLMDataArchitect
{
    /// <summary>
    /// 新プロンプト対応の戦略データ
    /// </summary>
    public class StrategyData
    {
        /// <summary>
        /// 戦略的な結論（行動方針）
        /// </summary>
        [JsonProperty("結論")]
        public string? 結論 { get; set; }

        /// <summary>
        /// 結論に至った理由
        /// </summary>
        [JsonProperty("理由")]
        public string? 理由 { get; set; }

        /// <summary>
        /// 基本戦術
        /// </summary>
        [JsonProperty("基本戦術")]
        public string? 基本戦術 { get; set; }

        /// <summary>
        /// 状況ごとの行動テーブル
        /// </summary>
        [JsonProperty("行動テーブル")]
        public ActionTable? 行動テーブル { get; set; }

        /// <summary>
        /// サンプルデータを生成するファクトリーメソッド
        /// </summary>
        public static StrategyData CreateSample()
        {
            return new StrategyData
            {
                結論 = "軸ずらしと防御を優先し、隙が出たら弱攻撃で反撃する戦術を取る。",
                理由 = "敵が強攻撃を狙うと隙が大きいため、そのタイミングを見て反撃できる。また、無理に前進するとリスクが高いため、防御と回避を基本とする。",
                基本戦術 = "対応型",
                行動テーブル = new ActionTable
                {
                    敵攻撃体勢 = "ガード",
                    敵待機状態 = "弱攻撃",
                    自分微有利状況 = "弱攻撃",
                    自分有利状況 = "強攻撃",
                    自分微不利状況 = "ガード",
                    自分不利状況 = "後ろ回避",
                    自分強攻撃ヒット = "弱攻撃",
                    敵強攻撃ヒット = "後ろ回避"
                }
            };
        }
    }
}
