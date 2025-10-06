using Newtonsoft.Json;
using System.Linq;
using UnityEngine;

namespace LLMDataArchitect
{
    /// <summary>
    /// 新プロンプト対応の戦略データ
    /// </summary>
    /// <summary>
    /// 新プロンプト形式用の戦略データ構造
    /// </summary>
    public class StrategyData
    {
        /// <summary>
        /// 基本戦術
        /// </summary>
        public string 分析結果 { get; set; }

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
  ""分析結果"": ""{分析結果}"",
  ""基本戦術"": ""{基本戦術}"",
  ""攻撃時判断基準"": ""{攻撃時判断基準}"",
  ""攻撃継続時判断基準"": ""{攻撃継続時判断基準}"",
  ""防御時判断基準"": ""{防御時判断基準}"",
  ""連続防御時判断基準"": ""{連続防御時判断基準}""
}}";
        }
        /// <summary>
        /// JSON文字列から戦略データを解析（日本語版）
        /// </summary>
        public static StrategyData FromJson(string json)
        {
            // 簡易的なJSON解析（実際にはNewtonsoft.Jsonなどを使用することを推奨）
            var strategy = new StrategyData();
            // プロパティの抽出（簡易実装）
            strategy.分析結果 = ExtractJsonValue(json, "分析結果");
            strategy.基本戦術 = ExtractJsonValue(json, "基本戦術");
            strategy.攻撃時判断基準 = ExtractJsonValue(json, "攻撃時判断基準");
            strategy.攻撃継続時判断基準 = ExtractJsonValue(json, "連続攻撃時判断基準");
            strategy.防御時判断基準 = ExtractJsonValue(json, "防御時判断基準");
            strategy.連続防御時判断基準 = ExtractJsonValue(json, "連続防御時判断基準");
            return strategy;
        }

        /// <summary>
        /// JSON文字列から戦略データを解析（英語版）
        /// </summary>
        public static StrategyData FromJsonEnglish(string json)
        {
            // 簡易的なJSON解析（実際にはNewtonsoft.Jsonなどを使用することを推奨）
            var strategy = new StrategyData();
            // プロパティの抽出（簡易実装）
            strategy.分析結果 = ExtractJsonValue(json, "AnalysisResult");
            strategy.基本戦術 = ExtractJsonValue(json, "BasicTactic");
            strategy.攻撃時判断基準 = ExtractJsonValue(json, "AttackCriteria");
            strategy.攻撃継続時判断基準 = ExtractJsonValue(json, "ContinuousAttackCriteria");
            strategy.防御時判断基準 = ExtractJsonValue(json, "DefenseCriteria");
            strategy.連続防御時判断基準 = ExtractJsonValue(json, "ContinuousDefenseCriteria");
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
        /// デフォルトの戦略データを生成
        /// </summary>
        public static StrategyData CreateDefault()
        {
            return new StrategyData
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
        public static StrategyData CreateAggressive()
        {
            return new StrategyData
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
        public static StrategyData CreateDefensive()
        {
            return new StrategyData
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
        public static StrategyData CreateEnergyEfficient()
        {
            return new StrategyData
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
