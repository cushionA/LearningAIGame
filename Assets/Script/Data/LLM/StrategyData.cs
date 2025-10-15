using Newtonsoft.Json;
using System;
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
        public string AnalysisResult { get; set; }

        /// <summary>
        /// 基本戦術
        /// </summary>
        public string BasicTactic { get; set; }

        /// <summary>
        /// 攻撃時判断基準
        /// </summary>
        public string AttackCriteria { get; set; }

        /// <summary>
        /// 攻撃継続時判断基準
        /// </summary>
        public string ContinuousAttackCriteria { get; set; }

        /// <summary>
        /// 防御時判断基準
        /// </summary>
        public string DefenseCriteria { get; set; }

        /// <summary>
        /// 連続防御時判断基準
        /// </summary>
        public string ContinuousDefenseCriteria { get; set; }

        /// <summary>
        /// JSON形式の文字列に変換
        /// </summary>
        public string ToJson()
        {
            return $@"{{
  ""AnalysisResult"": ""{AnalysisResult}"",
  ""BasicTactic"": ""{BasicTactic}"",
  ""AttackCriteria"": ""{AttackCriteria}"",
  ""ContinuousAttackCriteria"": ""{ContinuousAttackCriteria}"",
  ""DefenseCriteria"": ""{DefenseCriteria}"",
  ""ContinuousDefenseCriteria"": ""{ContinuousDefenseCriteria}""
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
            strategy.AnalysisResult = ExtractJsonValue(json, "分析結果");
            strategy.BasicTactic = ExtractJsonValue(json, "基本戦術");
            strategy.AttackCriteria = ExtractJsonValue(json, "攻撃時判断基準");
            strategy.ContinuousAttackCriteria = ExtractJsonValue(json, "連続攻撃時判断基準\"");
            strategy.DefenseCriteria = ExtractJsonValue(json, "防御時判断基準");
            strategy.ContinuousDefenseCriteria = ExtractJsonValue(json, "連続防御時判断基準");
            return strategy;
        }

        /// <summary>
        /// JSON文字列から戦略データを安全に解析（英語版）
        /// 例外を発生させず、成功/失敗を返します。
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>(解析成功, 戦略データ)のタプル。失敗時はstrategyはnull</returns>
        public static (bool isSuccess, StrategyData strategy) TryFromJsonEnglish(string json)
        {
            try
            {
                // 簡易的なJSON解析（実際にはNewtonsoft.Jsonなどを使用することを推奨）
                var strategy = new StrategyData();

                // プロパティの抽出
                strategy.AnalysisResult = ExtractJsonValue(json, "AnalysisResult");
                strategy.BasicTactic = ExtractJsonValue(json, "BasicTactic");
                strategy.AttackCriteria = ExtractJsonValue(json, "AttackCriteria");
                strategy.ContinuousAttackCriteria = ExtractJsonValue(json, "ContinuousAttackCriteria");
                strategy.DefenseCriteria = ExtractJsonValue(json, "DefenseCriteria");
                strategy.ContinuousDefenseCriteria = ExtractJsonValue(json, "ContinuousDefenseCriteria");

                // 必須フィールドの検証
                if (string.IsNullOrEmpty(strategy.BasicTactic) ||
                    string.IsNullOrEmpty(strategy.AttackCriteria) ||
                    string.IsNullOrEmpty(strategy.ContinuousAttackCriteria) ||
                    string.IsNullOrEmpty(strategy.DefenseCriteria) ||
                    string.IsNullOrEmpty(strategy.ContinuousDefenseCriteria))
                {
                    return (false, null);
                }

                return (true, strategy);
            }
            catch (Exception ex)
            {
                // 例外が発生した場合もfalseを返す（デバッグ用にログ出力）
                Debug.LogWarning($"JSON解析中に例外が発生しました: {ex.Message}");
                return (false, null);
            }
        }

        /// <summary>
        /// JSON文字列から戦略データを解析（英語版）
        /// 例外が発生する可能性があるため、TryFromJsonEnglishの使用を推奨
        /// </summary>
        public static StrategyData FromJsonEnglish(string json)
        {
            var (isSuccess, strategy) = TryFromJsonEnglish(json);

            if (!isSuccess)
            {
                throw new JsonException("JSON解析に失敗しました。");
            }

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
                BasicTactic = "対応型",
                AttackCriteria = "累積確率重視",
                ContinuousAttackCriteria = "直近パターン重視",
                DefenseCriteria = "累積確率重視",
                ContinuousDefenseCriteria = "反撃"
            };
        }

        /// <summary>
        /// 攻撃的な戦略データを生成
        /// </summary>
        public static StrategyData CreateAggressive()
        {
            return new StrategyData
            {
                BasicTactic = "攻撃型",
                AttackCriteria = "リターン重視",
                ContinuousAttackCriteria = "リターン重視",
                DefenseCriteria = "反撃",
                ContinuousDefenseCriteria = "カウンター"
            };
        }

        /// <summary>
        /// 防御的な戦略データを生成
        /// </summary>
        public static StrategyData CreateDefensive()
        {
            return new StrategyData
            {
                BasicTactic = "防御型",
                AttackCriteria = "速度重視",
                ContinuousAttackCriteria = "速度重視",
                DefenseCriteria = "生存重視",
                ContinuousDefenseCriteria = "回避重視"
            };
        }

        /// <summary>
        /// エネルギー効率重視の戦略データを生成
        /// </summary>
        public static StrategyData CreateEnergyEfficient()
        {
            return new StrategyData
            {
                BasicTactic = "持久型",
                AttackCriteria = "エネルギー効率重視",
                ContinuousAttackCriteria = "エネルギー効率重視",
                DefenseCriteria = "エネルギー重視",
                ContinuousDefenseCriteria = "エネルギー重視"
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

            if (!validBasicTactics.Contains(BasicTactic))
            {
                errorMessage = $"無効な基本戦術: {BasicTactic}";
                return false;
            }

            if (!validAttackCriteria.Contains(AttackCriteria))
            {
                errorMessage = $"無効な攻撃時判断基準: {AttackCriteria}";
                return false;
            }

            if (!validAttackCriteria.Contains(ContinuousAttackCriteria))
            {
                errorMessage = $"無効な攻撃継続時判断基準: {ContinuousAttackCriteria}";
                return false;
            }

            if (!validDefenseCriteria.Contains(DefenseCriteria))
            {
                errorMessage = $"無効な防御時判断基準: {DefenseCriteria}";
                return false;
            }

            if (!validDefenseCriteria.Contains(ContinuousDefenseCriteria))
            {
                errorMessage = $"無効な連続防御時判断基準: {ContinuousDefenseCriteria}";
                return false;
            }

            errorMessage = "";
            return true;
        }
    }
}
