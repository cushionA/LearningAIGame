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
// - 成功率とダメージ量から戦略の有効性を評価
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
// - GetConditionEvaluationJapanese: 日本語フォーマットで評価を取得
// - GetConditionEvaluationEnglish: 英語フォーマットで評価を取得
// - SetTestData: テスト用に優勢/劣勢/膠着の状態データを設定
// 
// 評価基準:
// [攻撃系]
// - 非常に効果的: 成功率70%以上 かつ ダメージ100以上
// - 効果的: 成功率50%以上 かつ ダメージ50以上
// - 効果薄い: 成功率30%以上 または ダメージ30以上
// - 変更必須: 上記以外
// 
// [防御系]
// - 非常に効果的: 成功率70%以上 かつ ダメージ20以下
// - 効果的: 成功率50%以上 かつ ダメージ40以下
// - 許容範囲: 成功率30%以上 または ダメージ60以下
// - 変更必須: 上記以外
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

        /// <summary>
        /// テスト用の戦況パターン
        /// </summary>
        public enum TestBattleState
        {
            /// <summary>
            /// 優勢状態：高い成功率、有利なダメージ比率
            /// </summary>
            Advantage,
            /// <summary>
            /// 劣勢状態：低い成功率、不利なダメージ比率
            /// </summary>
            Disadvantage,
            /// <summary>
            /// 膠着状態：中程度の成功率、拮抗したダメージ比率
            /// </summary>
            Stalemate
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
        /// 条件タイプの評価を日本語でフォーマットして取得
        /// 例: 【攻撃時判断基準】成功:5 失敗:3 与ダメージ120 → 効果的
        /// </summary>
        /// <param name="conditionType">条件の種類</param>
        /// <param name="damage">ダメージ量</param>
        /// <returns>フォーマットされた評価文字列</returns>
        public string GetConditionEvaluationJapanese(ConditionType conditionType, int damage)
        {
            int success = GetResult(conditionType, true);
            int fail = GetResult(conditionType, false);

            string conditionName = GetConditionNameJapanese(conditionType);
            string damageLabel = IsAttackCondition(conditionType) ? "与ダメージ" : "被ダメージ";
            string evaluation = EvaluateConditionJapanese(conditionType, success, fail, damage);

            return $"【{conditionName}】成功:{success} 失敗:{fail} {damageLabel}{damage} → {evaluation}";
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
        /// 条件の評価を日本語で取得
        /// </summary>
        private string EvaluateConditionJapanese(ConditionType conditionType, int success, int fail, int damage)
        {
            int total = success + fail;

            // データが不足している場合
            if (total == 0)
                return "データ不足";

            float successRate = (float)success / total;

            // 攻撃系の評価
            if (IsAttackCondition(conditionType))
            {
                if (successRate >= 0.7f && damage >= 100)
                    return "非常に効果的";
                else if (successRate >= 0.5f && damage >= 50)
                    return "効果的";
                else if (successRate >= 0.3f || damage >= 30)
                    return "効果薄い";
                else
                    return "変更必須";
            }
            // 防御系の評価
            else
            {
                if (successRate >= 0.7f && damage <= 20)
                    return "非常に効果的";
                else if (successRate >= 0.5f && damage <= 40)
                    return "効果的";
                else if (successRate >= 0.3f || damage <= 60)
                    return "許容範囲";
                else
                    return "変更必須";
            }
        }

        // ===== 英語フォーマット =====

        /// <summary>
        /// 条件タイプの評価を英語でフォーマットして取得
        /// 例: **Attack Criteria** Success:5 Fail:3 Damage:120 → Effective
        /// </summary>
        /// <param name="conditionType">条件の種類</param>
        /// <param name="damage">ダメージ量</param>
        /// <param name="criteriaText">判断基準のテキスト(任意)</param>
        /// <returns>フォーマットされた評価文字列</returns>
        public string GetConditionEvaluationEnglish(ConditionType conditionType, int damage, string criteriaText = "")
        {
            int success = GetResult(conditionType, true);
            int fail = GetResult(conditionType, false);

            string conditionName = GetConditionNameEnglish(conditionType);
            string damageLabel = IsAttackCondition(conditionType) ? "Dealt" : "Taken";
            string evaluation = EvaluateConditionEnglish(conditionType, success, fail, damage);

            string criteriaDisplay = string.IsNullOrEmpty(criteriaText) ? "" : $" \"{criteriaText}\"";

            return $"- **{conditionName}**{criteriaDisplay} Success:{success} Fail:{fail} {damageLabel} Damage:{damage} → {evaluation}";
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
        /// 条件の評価を英語で取得
        /// </summary>
        private string EvaluateConditionEnglish(ConditionType conditionType, int success, int fail, int damage)
        {
            int total = success + fail;

            // データが不足している場合
            if (total == 0)
                return "Insufficient Data";

            float successRate = (float)success / total;

            // 攻撃系の評価
            if (IsAttackCondition(conditionType))
            {
                if (successRate >= 0.7f && damage >= 100)
                    return "Highly Effective";
                else if (successRate >= 0.5f && damage >= 50)
                    return "Effective";
                else if (successRate >= 0.3f || damage >= 30)
                    return "Weak Effect";
                else
                    return "Must Change";
            }
            // 防御系の評価
            else
            {
                if (successRate >= 0.7f && damage <= 20)
                    return "Highly Effective";
                else if (successRate >= 0.5f && damage <= 40)
                    return "Effective";
                else if (successRate >= 0.3f || damage <= 60)
                    return "Acceptable";
                else
                    return "Must Change";
            }
        }

        // ===== テスト用メソッド =====

        /// <summary>
        /// テスト用に戦況パターンに応じたデータを設定
        /// </summary>
        /// <param name="battleState">戦況パターン(優勢/劣勢/膠着)</param>
        public void SetTestData(TestBattleState battleState)
        {
            Clear();

            switch (battleState)
            {
                case TestBattleState.Advantage:
                    SetAdvantageData();
                    break;

                case TestBattleState.Disadvantage:
                    SetDisadvantageData();
                    break;

                case TestBattleState.Stalemate:
                    SetStalemateData();
                    break;
            }
        }

        /// <summary>
        /// 優勢状態のテストデータを設定
        /// 攻撃: 高成功率・高ダメージ、防御: 高成功率・低被ダメージ
        /// </summary>
        private void SetAdvantageData()
        {
            // 攻撃時判断基準: 成功率75% (15成功/5失敗)
            AttackConditionSuccess = 15;
            AttackConditionFail = 5;

            // 連続攻撃時判断基準: 成功率80% (16成功/4失敗)
            SequentialAttackConditionSuccess = 16;
            SequentialAttackConditionFail = 4;

            // 防御時判断基準: 成功率80% (16成功/4失敗)
            DefenseConditionSuccess = 16;
            DefenseConditionFail = 4;

            // 連続防御時判断基準: 成功率85% (17成功/3失敗)
            SequentialDefenseConditionSuccess = 17;
            SequentialDefenseConditionFail = 3;
        }

        /// <summary>
        /// 劣勢状態のテストデータを設定
        /// 攻撃: 低成功率・低ダメージ、防御: 低成功率・高被ダメージ
        /// </summary>
        private void SetDisadvantageData()
        {
            // 攻撃時判断基準: 成功率20% (4成功/16失敗)
            AttackConditionSuccess = 4;
            AttackConditionFail = 16;

            // 連続攻撃時判断基準: 成功率15% (3成功/17失敗)
            SequentialAttackConditionSuccess = 3;
            SequentialAttackConditionFail = 17;

            // 防御時判断基準: 成功率25% (5成功/15失敗)
            DefenseConditionSuccess = 5;
            DefenseConditionFail = 15;

            // 連続防御時判断基準: 成功率10% (2成功/18失敗)
            SequentialDefenseConditionSuccess = 2;
            SequentialDefenseConditionFail = 18;
        }

        /// <summary>
        /// 膠着状態のテストデータを設定
        /// 攻撃: 中成功率・中ダメージ、防御: 中成功率・中被ダメージ
        /// </summary>
        private void SetStalemateData()
        {
            // 攻撃時判断基準: 成功率50% (10成功/10失敗)
            AttackConditionSuccess = 10;
            AttackConditionFail = 10;

            // 連続攻撃時判断基準: 成功率45% (9成功/11失敗)
            SequentialAttackConditionSuccess = 9;
            SequentialAttackConditionFail = 11;

            // 防御時判断基準: 成功率55% (11成功/9失敗)
            DefenseConditionSuccess = 11;
            DefenseConditionFail = 9;

            // 連続防御時判断基準: 成功率50% (10成功/10失敗)
            SequentialDefenseConditionSuccess = 10;
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
            Debug.Log(GetConditionEvaluationJapanese(ConditionType.Attack, 0));
            Debug.Log(GetConditionEvaluationJapanese(ConditionType.SequentialAttack, 0));
            Debug.Log(GetConditionEvaluationJapanese(ConditionType.Defense, 0));
            Debug.Log(GetConditionEvaluationJapanese(ConditionType.SequentialDefense, 0));
        }

        /// <summary>
        /// 現在の戦略結果をデバッグログに出力(英語)
        /// </summary>
        public void DebugLogResultsEnglish()
        {
            Debug.Log("=== Strategy Results ===");
            Debug.Log(GetConditionEvaluationEnglish(ConditionType.Attack, 0));
            Debug.Log(GetConditionEvaluationEnglish(ConditionType.SequentialAttack, 0));
            Debug.Log(GetConditionEvaluationEnglish(ConditionType.Defense, 0));
            Debug.Log(GetConditionEvaluationEnglish(ConditionType.SequentialDefense, 0));
        }
    }
}