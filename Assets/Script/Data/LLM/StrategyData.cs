using NaughtyAttributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LLMDataArchitect
{
    /// <summary>
    /// LLMにより作成される戦略データ
    /// </summary>
    [Serializable]
    public class StrategyData
    {
        #region 静的配列と列挙型

        /// <summary>
        /// 攻撃行動の基準をマッピングするDictionary
        /// キー: プロンプトの行動基準文字列
        /// 値: ActionCriteriaType (Attack_*)
        /// </summary>
        public static readonly Dictionary<string, ActionCriteriaType> AttackCriteriaDictionary = new()
        {
            { "Cumulative Probability", ActionCriteriaType.Attack_CumulativeProbability },
            { "Recent Pattern Focus", ActionCriteriaType.Attack_RecentPatternFocus },
            { "Speed Priority", ActionCriteriaType.Attack_SpeedPriority },
            { "Return Priority", ActionCriteriaType.Attack_ReturnPriority },
            { "Feint Focus", ActionCriteriaType.Attack_FeintFocus },
            { "Dispersion Focus", ActionCriteriaType.Attack_DispersionFocus },
            { "Energy Efficiency", ActionCriteriaType.Attack_EnergyEfficiency }
        };

        /// <summary>
        /// 防御行動の基準をマッピングするDictionary
        /// キー: プロンプトの行動基準文字列
        /// 値: ActionCriteriaType (Defense_*)
        /// </summary>
        public static readonly Dictionary<string, ActionCriteriaType> DefenseCriteriaDictionary = new()
        {
            { "Cumulative Probability", ActionCriteriaType.Defense_CumulativeProbability },
            { "Recent Pattern Focus", ActionCriteriaType.Defense_RecentPatternFocus },
            { "Counterattack Focus", ActionCriteriaType.Defense_CounterattackFocus },
            { "Return Priority", ActionCriteriaType.Defense_ReturnPriority },
            { "Risk Avoidance", ActionCriteriaType.Defense_RiskAvoidance },
            { "Evasive Counter Priority", ActionCriteriaType.Defense_EvasiveCounterPriority },
            { "Dispersion Focus", ActionCriteriaType.Defense_DispersionFocus }
        };

        /// <summary>
        /// 戦闘AIが選択できる基準に対応する列挙型。
        /// 各メンバーのXMLコメントに元のプロンプトの文字列を記述しています。
        /// </summary>
        public enum ActionCriteriaType : byte
        {
            /// <summary> 累積確率 </summary>
            Attack_CumulativeProbability,
            /// <summary> 最近のパターン重視 </summary>
            Attack_RecentPatternFocus,
            /// <summary> スピード優先 </summary>
            Attack_SpeedPriority,
            /// <summary> リターン優先 </summary>
            Attack_ReturnPriority,
            /// <summary> フェイント重視 </summary>
            Attack_FeintFocus,
            /// <summary> 分散重視 </summary>
            Attack_DispersionFocus,
            /// <summary> エネルギー効率 </summary>
            Attack_EnergyEfficiency,
            // 
            /// <summary> 累積確率 </summary>
            Defense_CumulativeProbability,
            /// <summary> 最近のパターン重視 </summary>
            Defense_RecentPatternFocus,
            /// <summary> 反撃重視 </summary>
            Defense_CounterattackFocus,
            /// <summary> リターン優先 </summary>
            Defense_ReturnPriority,
            /// <summary> リスク回避 </summary>
            Defense_RiskAvoidance,
            /// <summary> カウンター優先 </summary>
            Defense_EvasiveCounterPriority,
            /// <summary> 分散重視 </summary>
            Defense_DispersionFocus
        }

        #endregion

        #region ValueDropdown用メソッド

        /// <summary>
        /// 基本戦術のドロップダウンリスト
        /// </summary>
        private static DropdownList<string> GetBasicTactics()
        {
            return new DropdownList<string>
            {
                { "攻撃型", "Aggressive" },
                { "防御型", "Defensive" },
                { "対応型", "Adaptive" },
                { "攪乱型", "Disruptive" },
                { "持久型", "Endurance" }
            };
        }

        /// <summary>
        /// 攻撃時判断基準のドロップダウンリスト
        /// </summary>
        private static DropdownList<string> GetAttackCriteria()
        {
            return new DropdownList<string>
            {
                { "累積確率重視", "Cumulative Probability" },
                { "直近パターン重視", "Recent Pattern Focus" },
                { "速度重視", "Speed Priority" },
                { "リターン重視", "Return Priority" },
                { "フェイント重視", "Feint Focus" },
                { "分散重視", "Dispersion Focus" },
                { "エネルギー効率重視", "Energy Efficiency" }
            };
        }

        /// <summary>
        /// 防御時判断基準のドロップダウンリスト
        /// </summary>
        private static DropdownList<string> GetDefenseCriteria()
        {
            return new DropdownList<string>
            {
                { "累積確率重視", "Cumulative Probability" },
                { "直近パターン重視", "Recent Pattern Focus" },
                { "エネルギー重視", "Energy Efficiency" },
                { "反撃", "Counterattack Focus" },
                { "カウンター", "Evasive Counter Priority" },
                { "生存重視", "Risk Avoidance" },
                { "分散重視", "Dispersion Focus" }
            };
        }

        #endregion

        #region フィールドとプロパティ

        /// <summary>
        /// 分析結果
        /// </summary>
        [SerializeField]
        [ResizableTextArea]
        private string _analysisResult = "No analysis due to default tactics.";
        public string AnalysisResult
        {
            get => _analysisResult;
            set => _analysisResult = value;
        }

        /// <summary>
        /// 基本戦術
        /// </summary>
        [SerializeField]
        [Dropdown("GetBasicTactics")]
        private string _basicTactic;
        public string BasicTactic
        {
            get => _basicTactic;
            set => _basicTactic = value;
        }

        /// <summary>
        /// 攻撃時判断基準
        /// </summary>
        [SerializeField]
        [Dropdown("GetAttackCriteria")]
        private string _attackCriteria;
        public string AttackCriteria
        {
            get => _attackCriteria;
            set => _attackCriteria = value;
        }

        /// <summary>
        /// 攻撃継続時判断基準
        /// </summary>
        [SerializeField]
        [Dropdown("GetAttackCriteria")]
        private string _continuousAttackCriteria;
        public string ContinuousAttackCriteria
        {
            get => _continuousAttackCriteria;
            set => _continuousAttackCriteria = value;
        }

        /// <summary>
        /// 防御時判断基準
        /// </summary>
        [SerializeField]
        [Dropdown("GetDefenseCriteria")]
        private string _defenseCriteria;
        public string DefenseCriteria
        {
            get => _defenseCriteria;
            set => _defenseCriteria = value;
        }

        /// <summary>
        /// 連続防御時判断基準
        /// </summary>
        [SerializeField]
        [Dropdown("GetDefenseCriteria")]
        private string _continuousDefenseCriteria;
        public string ContinuousDefenseCriteria
        {
            get => _continuousDefenseCriteria;
            set => _continuousDefenseCriteria = value;
        }

        #endregion

        /// <summary>
        /// 文字列からAttackCriteriaTypeを取得
        /// </summary>
        /// <param name="criteria">プロンプトの基準文字列</param>
        /// <returns>対応するActionCriteriaType</returns>
        public static ActionCriteriaType GetAttackCriteria(string criteria)
        {
            if (AttackCriteriaDictionary.TryGetValue(criteria, out var result))
                return result;

            return ActionCriteriaType.Attack_CumulativeProbability; // デフォルト値
        }

        /// <summary>
        /// 文字列からDefenseCriteriaTypeを取得
        /// </summary>
        /// <param name="criteria">プロンプトの基準文字列</param>
        /// <returns>対応するActionCriteriaType</returns>
        public static ActionCriteriaType GetDefenseCriteria(string criteria)
        {
            if (DefenseCriteriaDictionary.TryGetValue(criteria, out var result))
                return result;

            return ActionCriteriaType.Defense_CumulativeProbability; // デフォルト値
        }

        /// <summary>
        /// 文字列(列挙型のメンバー名)を ActionCriteriaType 列挙型に変換。
        /// </summary>
        /// <param name="result">変換結果の ActionCriteriaType</param>
        /// <param name="ignoreCase">大文字と小文字を区別しない場合は true(デフォルトは true)</param>
        /// <returns>変換に成功した場合は true、それ以外は false</returns>
        public static bool TryGetActionCriteriaTypeFromString(string criteriaMemberName, out ActionCriteriaType result, bool ignoreCase = true)
        {
            // C# 7.3 以降で利用可能なジェネリック版 Enum.TryParse。
            // これが最も高速かつ低オーバーヘッドな方法です。
            return Enum.TryParse<ActionCriteriaType>(criteriaMemberName, ignoreCase, out result);
        }

        #region Json変換

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
        /// JSON文字列から戦略データを解析(日本語版)
        /// </summary>
        public static StrategyData FromJson(string json)
        {
            // 簡易的なJSON解析(実際にはNewtonsoft.Jsonなどを使用することを推奨)
            var strategy = new StrategyData();
            // プロパティの抽出(簡易実装)
            strategy.AnalysisResult = ExtractJsonValue(json, "分析結果");
            strategy.BasicTactic = ExtractJsonValue(json, "基本戦術");
            strategy.AttackCriteria = ExtractJsonValue(json, "攻撃時判断基準");
            strategy.ContinuousAttackCriteria = ExtractJsonValue(json, "連続攻撃時判断基準\"");
            strategy.DefenseCriteria = ExtractJsonValue(json, "防御時判断基準");
            strategy.ContinuousDefenseCriteria = ExtractJsonValue(json, "連続防御時判断基準");
            return strategy;
        }

        /// <summary>
        /// JSON文字列から戦略データを安全に解析(英語版)
        /// 例外を発生させず、成功/失敗を返します。
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>(解析成功, 戦略データ)のタプル。失敗時はstrategyはnull</returns>
        public static (bool isSuccess, StrategyData strategy) TryFromJsonEnglish(string json)
        {
            try
            {
                // 簡易的なJSON解析(実際にはNewtonsoft.Jsonなどを使用することを推奨)
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
                // 例外が発生した場合もfalseを返す(デバッグ用にログ出力)
                Debug.LogWarning($"JSON解析中に例外が発生しました: {ex.Message}");
                return (false, null);
            }
        }

        /// <summary>
        /// JSON文字列から戦略データを解析(英語版)
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

        #endregion

        #region テスト用

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

        #endregion
    }
}