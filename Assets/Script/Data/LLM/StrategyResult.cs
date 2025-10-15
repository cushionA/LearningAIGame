//==============================================ファイルヘッダ=========================================================
// StrategyResult
// 
// 概要: AI戦略の実行結果を管理し、成功/失敗のカウントと評価を提供
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [StrategyResult]
// - 戦略結果を管理するクラス
// - 攻撃・防御の各判断基準における成功/失敗のカウントを記録
// - 成功と失敗の回数差から戦略の有効性を評価
// 
// [ConditionType enum]
// - Attack: 攻撃時判断基準
// - SequentialAttack: 連続攻撃時判断基準
// - Defense: 防御時判断基準
// - SequentialDefense: 連続防御時判断基準
// 
// 主要メソッド:
// - AddResult: 指定した条件タイプの成功/失敗カウントを追加
// - GetResult: 指定した条件タイプのカウントを取得
// - GetSuccessRate: 指定した条件タイプの成功率を計算
// - GetSuccessDifference: 成功と失敗の回数差を取得
// - GetConditionEvaluationJapanese: 日本語フォーマットで評価を取得
// - GetConditionEvaluationEnglish: 英語フォーマットで評価を取得
// - SetTestData: テスト用に優勢/劣勢/拮抗/エネルギー不足/体力危険の状態データを設定
// 
// 評価基準（成功-失敗の差分）:
// [攻撃系・防御系共通]
// - 非常に効果的: +3以上の差
// - 効果的: +2以上の差
// - 許容範囲: +1以上の差
// - 効果薄い: ±0（同数）
// - 変更必須: マイナス（失敗の方が多い）
// 
// 入力元クラス: ルールベースAI
// 出力先クラス: LLMプロンプト生成用データ
// 
// その他:
// 戦闘中の戦略判断の結果を蓄積し、LLMへのフィードバック情報として利用される
//=====================================================================================================================

using UnityEngine;

namespace LLMDataArchitect
{
    /// <summary>
    /// 戦略結果を管理するクラス
    /// 攻撃・防御の各判断基準における成功/失敗のカウントを記録
    /// </summary>
    public class StrategyResult
    {
        // ===== 評価基準定数 =====

        /// <summary>
        /// 「非常に効果的」と判定する最小差分
        /// </summary>
        private const int k_HIGHLY_EFFECTIVE_THRESHOLD = 3;

        /// <summary>
        /// 「効果的」と判定する最小差分
        /// </summary>
        private const int k_EFFECTIVE_THRESHOLD = 2;

        /// <summary>
        /// 「許容範囲」と判定する最小差分
        /// </summary>
        private const int k_ACCEPTABLE_THRESHOLD = 1;

        /// <summary>
        /// 判断基準の種類
        /// </summary>
        public enum ConditionType
        {
            /// <summary>
            /// 攻撃時判断基準
            /// </summary>
            Attack,
            /// <summary>
            /// 連続攻撃時判断基準
            /// </summary>
            SequentialAttack,
            /// <summary>
            /// 防御時判断基準
            /// </summary>
            Defense,
            /// <summary>
            /// 連続防御時判断基準
            /// </summary>
            SequentialDefense
        }

        // ===== プライベートフィールド =====

        /// <summary>
        /// 攻撃時判断基準(成功)
        /// </summary>
        public int AttackConditionSuccess { get; private set; }

        /// <summary>
        /// 攻撃時判断基準(失敗)
        /// </summary>
        public int AttackConditionFail { get; private set; }

        /// <summary>
        /// 連続攻撃時判断基準(成功)
        /// </summary>
        public int SequentialAttackConditionSuccess { get; private set; }

        /// <summary>
        /// 連続攻撃時判断基準(失敗)
        /// </summary>
        public int SequentialAttackConditionFail { get; private set; }

        /// <summary>
        /// 防御時判断基準(成功)
        /// </summary>
        public int DefenseConditionSuccess { get; private set; }

        /// <summary>
        /// 防御時判断基準(失敗)
        /// </summary>
        public int DefenseConditionFail { get; private set; }

        /// <summary>
        /// 連続防御時判断基準(成功)
        /// </summary>
        public int SequentialDefenseConditionSuccess { get; private set; }

        /// <summary>
        /// 連続防御時判断基準(失敗)
        /// </summary>
        public int SequentialDefenseConditionFail { get; private set; }

        // ===== 公開メソッド =====

        /// <summary>
        /// 指定した条件タイプと結果のカウントを追加
        /// </summary>
        /// <param name="conditionType">条件の種類</param>
        /// <param name="isSuccess">成功ならtrue、失敗ならfalse</param>
        /// <param name="count">追加する数(デフォルト1)</param>
        public void AddResult(ConditionType conditionType, bool isSuccess, int count = 1)
        {
            switch (conditionType)
            {
                case ConditionType.Attack:
                    if (isSuccess)
                        AttackConditionSuccess += count;
                    else
                        AttackConditionFail += count;
                    break;

                case ConditionType.SequentialAttack:
                    if (isSuccess)
                        SequentialAttackConditionSuccess += count;
                    else
                        SequentialAttackConditionFail += count;
                    break;

                case ConditionType.Defense:
                    if (isSuccess)
                        DefenseConditionSuccess += count;
                    else
                        DefenseConditionFail += count;
                    break;

                case ConditionType.SequentialDefense:
                    if (isSuccess)
                        SequentialDefenseConditionSuccess += count;
                    else
                        SequentialDefenseConditionFail += count;
                    break;
            }
        }

        /// <summary>
        /// 指定した条件タイプと結果のカウントを取得
        /// </summary>
        /// <param name="conditionType">条件の種類</param>
        /// <param name="isSuccess">成功ならtrue、失敗ならfalse</param>
        /// <returns>現在のカウント</returns>
        public int GetResult(ConditionType conditionType, bool isSuccess)
        {
            switch (conditionType)
            {
                case ConditionType.Attack:
                    return isSuccess ? AttackConditionSuccess : AttackConditionFail;

                case ConditionType.SequentialAttack:
                    return isSuccess ? SequentialAttackConditionSuccess : SequentialAttackConditionFail;

                case ConditionType.Defense:
                    return isSuccess ? DefenseConditionSuccess : DefenseConditionFail;

                case ConditionType.SequentialDefense:
                    return isSuccess ? SequentialDefenseConditionSuccess : SequentialDefenseConditionFail;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// 指定した条件タイプの成功率を取得
        /// </summary>
        /// <param name="conditionType">条件の種類</param>
        /// <returns>成功率(0.0～1.0)。試行回数が0の場合は0を返す</returns>
        public float GetSuccessRate(ConditionType conditionType)
        {
            int success = GetResult(conditionType, true);
            int fail = GetResult(conditionType, false);
            int total = success + fail;

            if (total == 0)
                return 0f;

            return (float)success / total;
        }

        /// <summary>
        /// 成功と失敗の回数差を取得（成功 - 失敗）
        /// </summary>
        /// <param name="conditionType">条件の種類</param>
        /// <returns>成功と失敗の差分。正の値なら成功が多く、負の値なら失敗が多い</returns>
        public int GetSuccessDifference(ConditionType conditionType)
        {
            int success = GetResult(conditionType, true);
            int fail = GetResult(conditionType, false);
            return success - fail;
        }

        /// <summary>
        /// 指定した条件タイプの合計試行回数を取得
        /// </summary>
        /// <param name="conditionType">条件の種類</param>
        /// <returns>合計試行回数</returns>
        public int GetTotalCount(ConditionType conditionType)
        {
            int success = GetResult(conditionType, true);
            int fail = GetResult(conditionType, false);
            return success + fail;
        }

        /// <summary>
        /// 全てのカウントをクリア
        /// </summary>
        public void Clear()
        {
            AttackConditionSuccess = 0;
            AttackConditionFail = 0;
            SequentialAttackConditionSuccess = 0;
            SequentialAttackConditionFail = 0;
            DefenseConditionSuccess = 0;
            DefenseConditionFail = 0;
            SequentialDefenseConditionSuccess = 0;
            SequentialDefenseConditionFail = 0;
        }

        // ===== 日本語フォーマット =====

        /// <summary>
        /// 全ての条件タイプの評価を日本語でまとめて取得
        /// </summary>
        /// <returns>全評価をまとめたフォーマット済み文字列</returns>
        public string GetAllConditionEvaluationsJapanese()
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine(GetConditionEvaluationJapanese(ConditionType.Attack));
            sb.AppendLine(GetConditionEvaluationJapanese(ConditionType.SequentialAttack));
            sb.AppendLine(GetConditionEvaluationJapanese(ConditionType.Defense));
            sb.Append(GetConditionEvaluationJapanese(ConditionType.SequentialDefense));

            return sb.ToString();
        }

        /// <summary>
        /// 条件タイプの評価を日本語でフォーマットして取得
        /// 例: 【攻撃時判断基準】成功:15 失敗:5 (差分:+10) → 非常に効果的
        /// </summary>
        /// <param name="conditionType">条件の種類</param>
        /// <returns>フォーマットされた評価文字列</returns>
        public string GetConditionEvaluationJapanese(ConditionType conditionType)
        {
            int success = GetResult(conditionType, true);
            int fail = GetResult(conditionType, false);
            int difference = GetSuccessDifference(conditionType);

            string conditionName = GetConditionNameJapanese(conditionType);
            string differenceStr = difference >= 0 ? $"+{difference}" : difference.ToString();
            string evaluation = EvaluateConditionByDifference(difference);

            return $"【{conditionName}】成功:{success} 失敗:{fail} (差分:{differenceStr}) → {evaluation}";
        }

        /// <summary>
        /// 条件タイプの日本語名を取得
        /// </summary>
        private string GetConditionNameJapanese(ConditionType conditionType)
        {
            switch (conditionType)
            {
                case ConditionType.Attack:
                    return "攻撃時判断基準";
                case ConditionType.SequentialAttack:
                    return "連続攻撃時判断基準";
                case ConditionType.Defense:
                    return "防御時判断基準";
                case ConditionType.SequentialDefense:
                    return "連続防御時判断基準";
                default:
                    return "不明";
            }
        }

        /// <summary>
        /// 成功と失敗の差分から評価を取得
        /// </summary>
        /// <param name="difference">成功 - 失敗の差分</param>
        /// <returns>評価文字列</returns>
        private string EvaluateConditionByDifference(int difference)
        {
            if (difference >= k_HIGHLY_EFFECTIVE_THRESHOLD)
                return "非常に効果的";
            else if (difference >= k_EFFECTIVE_THRESHOLD)
                return "効果的";
            else if (difference >= k_ACCEPTABLE_THRESHOLD)
                return "許容範囲";
            else if (difference == 0)
                return "効果薄い";
            else
                return "変更必須";
        }

        // ===== 英語フォーマット =====

        /// <summary>
        /// 全ての条件タイプの評価を英語でまとめて取得
        /// </summary>
        /// <param name="attackCriteria">攻撃時判断基準のテキスト(任意)</param>
        /// <param name="sequentialAttackCriteria">連続攻撃時判断基準のテキスト(任意)</param>
        /// <param name="defenseCriteria">防御時判断基準のテキスト(任意)</param>
        /// <param name="sequentialDefenseCriteria">連続防御時判断基準のテキスト(任意)</param>
        /// <returns>全評価をまとめたフォーマット済み文字列</returns>
        public string GetAllConditionEvaluationsEnglish(
            string attackCriteria = "",
            string sequentialAttackCriteria = "",
            string defenseCriteria = "",
            string sequentialDefenseCriteria = "")
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine(GetConditionEvaluationEnglish(ConditionType.Attack, attackCriteria));
            sb.AppendLine(GetConditionEvaluationEnglish(ConditionType.SequentialAttack, sequentialAttackCriteria));
            sb.AppendLine(GetConditionEvaluationEnglish(ConditionType.Defense, defenseCriteria));
            sb.Append(GetConditionEvaluationEnglish(ConditionType.SequentialDefense, sequentialDefenseCriteria));

            return sb.ToString();
        }


        /// <summary>
        /// 条件タイプの評価を英語でフォーマットして取得
        /// 例: **Attack Criteria** Success:15 Fail:5 (Diff:+10) → Highly Effective
        /// </summary>
        /// <param name="conditionType">条件の種類</param>
        /// <param name="criteriaText">判断基準のテキスト(任意)</param>
        /// <returns>フォーマットされた評価文字列</returns>
        public string GetConditionEvaluationEnglish(ConditionType conditionType, string criteriaText = "")
        {
            int success = GetResult(conditionType, true);
            int fail = GetResult(conditionType, false);
            int difference = GetSuccessDifference(conditionType);

            string conditionName = GetConditionNameEnglish(conditionType);
            string differenceStr = difference >= 0 ? $"+{difference}" : difference.ToString();
            string evaluation = EvaluateConditionByDifferenceEnglish(difference);

            string criteriaDisplay = string.IsNullOrEmpty(criteriaText) ? "" : $" \"{criteriaText}\"";

            return $"- **{conditionName}**{criteriaDisplay} Success:{success} Fail:{fail} (Diff:{differenceStr}) → {evaluation}";
        }

        /// <summary>
        /// 条件タイプの英語名を取得
        /// </summary>
        private string GetConditionNameEnglish(ConditionType conditionType)
        {
            switch (conditionType)
            {
                case ConditionType.Attack:
                    return "Attack Criteria";
                case ConditionType.SequentialAttack:
                    return "Continuous Attack Criteria";
                case ConditionType.Defense:
                    return "Defense Criteria";
                case ConditionType.SequentialDefense:
                    return "Continuous Defense Criteria";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// 成功と失敗の差分から評価を取得（英語）
        /// </summary>
        /// <param name="difference">成功 - 失敗の差分</param>
        /// <returns>評価文字列</returns>
        private string EvaluateConditionByDifferenceEnglish(int difference)
        {
            if (difference >= k_HIGHLY_EFFECTIVE_THRESHOLD)
                return "Highly Effective";
            else if (difference >= k_EFFECTIVE_THRESHOLD)
                return "Effective";
            else if (difference >= k_ACCEPTABLE_THRESHOLD)
                return "Acceptable";
            else if (difference == 0)
                return "Weak Effect";
            else
                return "Must Change";
        }

        // ===== テスト用メソッド =====

        /// <summary>
        /// テスト用に戦況パターンに応じたデータを設定
        /// TestSituationType列挙型に基づいて各状況のデータを生成
        /// </summary>
        /// <param name="situationType">テスト戦況の種類</param>
        public void SetTestData(TestSituationType situationType)
        {
            Clear();

            switch (situationType)
            {
                case TestSituationType.優勢:
                    SetAdvantageData();
                    break;

                case TestSituationType.拮抗:
                    SetBalanceData();
                    break;

                case TestSituationType.劣勢:
                    SetDisadvantageData();
                    break;

                case TestSituationType.エネルギー不足:
                    SetEnergyShortageData();
                    break;

                case TestSituationType.体力危険:
                    SetHealthCriticalData();
                    break;
            }
        }

        /// <summary>
        /// 優勢状態のテストデータを設定
        /// 攻撃: 高成功率、防御: 高成功率
        /// </summary>
        private void SetAdvantageData()
        {
            // 攻撃時判断基準: 差分+5 (10成功/5失敗)
            AttackConditionSuccess = 10;
            AttackConditionFail = 5;

            // 連続攻撃時判断基準: 差分+6 (11成功/5失敗)
            SequentialAttackConditionSuccess = 11;
            SequentialAttackConditionFail = 5;

            // 防御時判断基準: 差分+6 (11成功/5失敗)
            DefenseConditionSuccess = 11;
            DefenseConditionFail = 5;

            // 連続防御時判断基準: 差分+7 (12成功/5失敗)
            SequentialDefenseConditionSuccess = 12;
            SequentialDefenseConditionFail = 5;
        }

        /// <summary>
        /// 拮抗状態のテストデータを設定
        /// 攻撃: 中成功率、防御: 中成功率
        /// </summary>
        private void SetBalanceData()
        {
            // 攻撃時判断基準: 差分±0 (10成功/10失敗)
            AttackConditionSuccess = 10;
            AttackConditionFail = 10;

            // 連続攻撃時判断基準: 差分-1 (9成功/10失敗)
            SequentialAttackConditionSuccess = 9;
            SequentialAttackConditionFail = 10;

            // 防御時判断基準: 差分+1 (11成功/10失敗)
            DefenseConditionSuccess = 11;
            DefenseConditionFail = 10;

            // 連続防御時判断基準: 差分±0 (10成功/10失敗)
            SequentialDefenseConditionSuccess = 10;
            SequentialDefenseConditionFail = 10;
        }

        /// <summary>
        /// 劣勢状態のテストデータを設定
        /// 攻撃: 低成功率、防御: 低成功率
        /// </summary>
        private void SetDisadvantageData()
        {
            // 攻撃時判断基準: 差分-6 (4成功/10失敗)
            AttackConditionSuccess = 4;
            AttackConditionFail = 10;

            // 連続攻撃時判断基準: 差分-7 (3成功/10失敗)
            SequentialAttackConditionSuccess = 3;
            SequentialAttackConditionFail = 10;

            // 防御時判断基準: 差分-5 (5成功/10失敗)
            DefenseConditionSuccess = 5;
            DefenseConditionFail = 10;

            // 連続防御時判断基準: 差分-8 (2成功/10失敗)
            SequentialDefenseConditionSuccess = 2;
            SequentialDefenseConditionFail = 10;
        }

        /// <summary>
        /// エネルギー不足状態のテストデータを設定
        /// 攻撃: 低リスク重視、防御: 標準的な成功率
        /// </summary>
        private void SetEnergyShortageData()
        {
            // 攻撃時判断基準: 差分-2 (8成功/10失敗) - 慎重な攻撃
            AttackConditionSuccess = 8;
            AttackConditionFail = 10;

            // 連続攻撃時判断基準: 差分-4 (6成功/10失敗) - 連続攻撃はさらに控えめ
            SequentialAttackConditionSuccess = 6;
            SequentialAttackConditionFail = 10;

            // 防御時判断基準: 差分+2 (12成功/10失敗) - 防御に注力
            DefenseConditionSuccess = 12;
            DefenseConditionFail = 10;

            // 連続防御時判断基準: 差分+3 (13成功/10失敗)
            SequentialDefenseConditionSuccess = 13;
            SequentialDefenseConditionFail = 10;
        }

        /// <summary>
        /// 体力危険状態のテストデータを設定
        /// 攻撃: 慎重な攻撃、防御: 高成功率重視
        /// </summary>
        private void SetHealthCriticalData()
        {
            // 攻撃時判断基準: 差分-3 (7成功/10失敗) - 非常に慎重
            AttackConditionSuccess = 7;
            AttackConditionFail = 10;

            // 連続攻撃時判断基準: 差分-5 (5成功/10失敗) - 連続攻撃はほぼ避ける
            SequentialAttackConditionSuccess = 5;
            SequentialAttackConditionFail = 10;

            // 防御時判断基準: 差分+5 (15成功/10失敗) - 防御最優先
            DefenseConditionSuccess = 15;
            DefenseConditionFail = 10;

            // 連続防御時判断基準: 差分+6 (16成功/10失敗)
            SequentialDefenseConditionSuccess = 16;
            SequentialDefenseConditionFail = 10;
        }

        // ===== ユーティリティメソッド =====

        /// <summary>
        /// 攻撃系の条件タイプかどうかを判定
        /// </summary>
        private bool IsAttackCondition(ConditionType conditionType)
        {
            return conditionType == ConditionType.Attack || conditionType == ConditionType.SequentialAttack;
        }

        /// <summary>
        /// 現在の戦略結果をデバッグログに出力(日本語)
        /// </summary>
        public void DebugLogResultsJapanese()
        {
            Debug.Log("=== 戦略結果 ===");
            Debug.Log(GetConditionEvaluationJapanese(ConditionType.Attack));
            Debug.Log(GetConditionEvaluationJapanese(ConditionType.SequentialAttack));
            Debug.Log(GetConditionEvaluationJapanese(ConditionType.Defense));
            Debug.Log(GetConditionEvaluationJapanese(ConditionType.SequentialDefense));
        }

        /// <summary>
        /// 現在の戦略結果をデバッグログに出力(英語)
        /// </summary>
        public void DebugLogResultsEnglish()
        {
            Debug.Log("=== Strategy Results ===");
            Debug.Log(GetConditionEvaluationEnglish(ConditionType.Attack));
            Debug.Log(GetConditionEvaluationEnglish(ConditionType.SequentialAttack));
            Debug.Log(GetConditionEvaluationEnglish(ConditionType.Defense));
            Debug.Log(GetConditionEvaluationEnglish(ConditionType.SequentialDefense));
        }
    }
}