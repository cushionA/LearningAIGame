using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

namespace LLMDataArchitect
{
    /// <summary>
    /// AIキャラクターの各アクションの実行確率を管理するクラス
    /// プロンプトで使用される確率名に対応
    /// </summary>
    public class ActionProbabilityManager
    {
        #region フィールド

        // --- 各行動の実行回数 ---
        private int _backwardDodgeCount;
        private int _horizontalDodgeCount;
        private int _forwardDodgeCount;
        private int _blockingCount;
        private int _lightAttackCount;
        private int _strongAttackCount;
        private int _strongAttackCancelCount;
        private int _horizontalDodgeAttackCount;
        private int _forwardDodgeAttackCount;

        /// <summary>
        /// 全行動の合計回数
        /// </summary>
        private int _totalActionCount;

        #endregion

        #region プロパティ

        /// <summary>
        /// 後ろ回避の実行確率
        /// </summary>
        public float BackwardDodgePercentage => CalculatePercentage(_backwardDodgeCount);

        /// <summary>
        /// 横回避の実行確率(左右統合)
        /// </summary>
        public float HorizontalDodgePercentage => CalculatePercentage(_horizontalDodgeCount);

        /// <summary>
        /// 前回避の実行確率
        /// </summary>
        public float ForwardDodgePercentage => CalculatePercentage(_forwardDodgeCount);

        /// <summary>
        /// ブロッキングの実行確率
        /// </summary>
        public float BlockingPercentage => CalculatePercentage(_blockingCount);

        /// <summary>
        /// 弱攻撃の実行確率
        /// </summary>
        public float LightAttackPercentage => CalculatePercentage(_lightAttackCount);

        /// <summary>
        /// 強攻撃の実行確率
        /// </summary>
        public float StrongAttackPercentage => CalculatePercentage(_strongAttackCount);

        /// <summary>
        /// 強攻撃キャンセルの実行確率
        /// </summary>
        public float StrongAttackCancelPercentage => CalculatePercentage(_strongAttackCancelCount);

        /// <summary>
        /// 横回避攻撃の実行確率
        /// </summary>
        public float HorizontalDodgeAttackPercentage => CalculatePercentage(_horizontalDodgeAttackCount);

        /// <summary>
        /// 前回避攻撃の実行確率
        /// </summary>
        public float ForwardDodgeAttackPercentage => CalculatePercentage(_forwardDodgeAttackCount);

        #endregion

        #region コンストラクタ

        /// <summary>
        /// コンストラクタ。基本確率で初期化
        /// </summary>
        public ActionProbabilityManager()
        {
            InitializeBasicProbabilities();
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 全ての実行回数をリセット
        /// </summary>
        public void ResetCounts()
        {
            _backwardDodgeCount = 0;
            _horizontalDodgeCount = 0;
            _forwardDodgeCount = 0;
            _blockingCount = 0;
            _lightAttackCount = 0;
            _strongAttackCount = 0;
            _strongAttackCancelCount = 0;
            _horizontalDodgeAttackCount = 0;
            _forwardDodgeAttackCount = 0;
            _totalActionCount = 0;
        }

        /// <summary>
        /// 基本的な確率で初期化
        /// </summary>
        public void InitializeBasicProbabilities()
        {
            // 基準となる合計回数(100回で計算すると整数値で扱いやすい)
            int baseTotal = 100;

            _backwardDodgeCount = Mathf.RoundToInt(baseTotal * 0.05f);          // 5
            _horizontalDodgeCount = Mathf.RoundToInt(baseTotal * 0.05f);        // 5
            _forwardDodgeCount = Mathf.RoundToInt(baseTotal * 0.15f);           // 15
            _blockingCount = Mathf.RoundToInt(baseTotal * 0.06f);               // 6
            _lightAttackCount = Mathf.RoundToInt(baseTotal * 0.25f);            // 25
            _strongAttackCount = Mathf.RoundToInt(baseTotal * 0.22f);           // 22
            _strongAttackCancelCount = Mathf.RoundToInt(baseTotal * 0.05f);     // 5
            _horizontalDodgeAttackCount = Mathf.RoundToInt(baseTotal * 0.11f);  // 11
            _forwardDodgeAttackCount = Mathf.RoundToInt(baseTotal * 0.11f);     // 11

            // 合計を再計算
            _totalActionCount = _backwardDodgeCount + _horizontalDodgeCount + _forwardDodgeCount +
                             _blockingCount + _lightAttackCount + _strongAttackCount +
                             _strongAttackCancelCount + _horizontalDodgeAttackCount + _forwardDodgeAttackCount;
        }

        /// <summary>
        /// ActionStateに基づいて対応する行動の実行回数を増やす
        /// </summary>
        /// <param name="state">実行されたActionState</param>
        public void AddAction(ActionState state)
        {
            switch (state)
            {
                case ActionState.後ろ回避:
                    _backwardDodgeCount++;
                    break;
                case ActionState.横回避:
                    _horizontalDodgeCount++;
                    break;
                case ActionState.前回避:
                    _forwardDodgeCount++;
                    break;
                case ActionState.ブロッキング:
                    _blockingCount++;
                    break;
                case ActionState.弱攻撃:
                    _lightAttackCount++;
                    break;
                case ActionState.強攻撃:
                    _strongAttackCount++;
                    break;
                case ActionState.強攻撃キャンセル:
                    _strongAttackCancelCount++;
                    break;
                case ActionState.横回避攻撃:
                    _horizontalDodgeAttackCount++;
                    break;
                case ActionState.前回避攻撃:
                    _forwardDodgeAttackCount++;
                    break;
                default:
                    Debug.LogWarning($"ActionState '{state}' は確率管理対象外です。");
                    return;
            }
            _totalActionCount++;
        }

        /// <summary>
        /// ActionStateに対応する実行回数を取得
        /// </summary>
        /// <param name="state">取得対象のActionState</param>
        /// <returns>実行回数</returns>
        public int GetActionCount(ActionState state)
        {
            switch (state)
            {
                case ActionState.後ろ回避:
                    return _backwardDodgeCount;
                case ActionState.横回避:
                    return _horizontalDodgeCount;
                case ActionState.前回避:
                    return _forwardDodgeCount;
                case ActionState.ブロッキング:
                    return _blockingCount;
                case ActionState.弱攻撃:
                    return _lightAttackCount;
                case ActionState.強攻撃:
                    return _strongAttackCount;
                case ActionState.強攻撃キャンセル:
                    return _strongAttackCancelCount;
                case ActionState.横回避攻撃:
                    return _horizontalDodgeAttackCount;
                case ActionState.前回避攻撃:
                    return _forwardDodgeAttackCount;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 全行動の合計回数を取得
        /// </summary>
        public int GetTotalActionCount()
        {
            return _totalActionCount;
        }

        #endregion

        #region テスト用メソッド

        /// <summary>
        /// テスト用に戦況パターンに応じた行動データを設定
        /// </summary>
        /// <param name="situationType">テスト戦況の種類</param>
        public void SetTestData(TestSituationType situationType)
        {
            ResetCounts();

            switch (situationType)
            {
                case TestSituationType.優勢:
                    SetAdvantageTestData();
                    break;

                case TestSituationType.拮抗:
                    SetBalanceTestData();
                    break;

                case TestSituationType.劣勢:
                    SetDisadvantageTestData();
                    break;

                case TestSituationType.エネルギー不足:
                    SetEnergyShortageTestData();
                    break;

                case TestSituationType.体力危険:
                    SetHealthCriticalTestData();
                    break;
            }
        }

        /// <summary>
        /// 優勢状態のテストデータを設定
        /// 特徴: 攻撃行動が多く、積極的なプレイスタイル
        /// </summary>
        private void SetAdvantageTestData()
        {
            // 攻撃重視: 弱攻撃30%, 強攻撃25%, 回避攻撃20%, 回避15%, ブロッキング10%
            _lightAttackCount = 30;
            _strongAttackCount = 25;
            _forwardDodgeAttackCount = 12;
            _horizontalDodgeAttackCount = 8;
            _forwardDodgeCount = 10;
            _horizontalDodgeCount = 3;
            _backwardDodgeCount = 2;
            _blockingCount = 7;
            _strongAttackCancelCount = 3;

            _totalActionCount = _backwardDodgeCount + _horizontalDodgeCount + _forwardDodgeCount +
                             _blockingCount + _lightAttackCount + _strongAttackCount +
                             _strongAttackCancelCount + _horizontalDodgeAttackCount + _forwardDodgeAttackCount;
        }

        /// <summary>
        /// 拮抗状態のテストデータを設定
        /// 特徴: 攻撃と防御のバランスが取れた行動パターン
        /// </summary>
        private void SetBalanceTestData()
        {
            // バランス型: 攻撃40%, 回避30%, 防御30%
            _lightAttackCount = 25;
            _strongAttackCount = 15;
            _forwardDodgeCount = 12;
            _horizontalDodgeCount = 10;
            _backwardDodgeCount = 8;
            _forwardDodgeAttackCount = 10;
            _horizontalDodgeAttackCount = 8;
            _blockingCount = 10;
            _strongAttackCancelCount = 2;

            _totalActionCount = _backwardDodgeCount + _horizontalDodgeCount + _forwardDodgeCount +
                             _blockingCount + _lightAttackCount + _strongAttackCount +
                             _strongAttackCancelCount + _horizontalDodgeAttackCount + _forwardDodgeAttackCount;
        }

        /// <summary>
        /// 劣勢状態のテストデータを設定
        /// 特徴: 防御行動が多く、慎重なプレイスタイル
        /// </summary>
        private void SetDisadvantageTestData()
        {
            // 防御重視: 回避40%, 防御20%, 攻撃40%
            _backwardDodgeCount = 20;
            _horizontalDodgeCount = 12;
            _forwardDodgeCount = 8;
            _blockingCount = 20;
            _lightAttackCount = 25;
            _strongAttackCount = 10;
            _forwardDodgeAttackCount = 3;
            _horizontalDodgeAttackCount = 2;
            _strongAttackCancelCount = 0;

            _totalActionCount = _backwardDodgeCount + _horizontalDodgeCount + _forwardDodgeCount +
                             _blockingCount + _lightAttackCount + _strongAttackCount +
                             _strongAttackCancelCount + _horizontalDodgeAttackCount + _forwardDodgeAttackCount;
        }

        /// <summary>
        /// エネルギー不足状態のテストデータを設定
        /// 特徴: エネルギー消費の少ない行動が中心
        /// </summary>
        private void SetEnergyShortageTestData()
        {
            // 低コスト行動中心: 弱攻撃50%, 回避30%, ブロッキング20%, 強攻撃ほぼなし
            _lightAttackCount = 50;
            _backwardDodgeCount = 12;
            _horizontalDodgeCount = 10;
            _forwardDodgeCount = 8;
            _blockingCount = 15;
            _strongAttackCount = 3;
            _forwardDodgeAttackCount = 1;
            _horizontalDodgeAttackCount = 1;
            _strongAttackCancelCount = 0;

            _totalActionCount = _backwardDodgeCount + _horizontalDodgeCount + _forwardDodgeCount +
                             _blockingCount + _lightAttackCount + _strongAttackCount +
                             _strongAttackCancelCount + _horizontalDodgeAttackCount + _forwardDodgeAttackCount;
        }

        /// <summary>
        /// 体力危険状態のテストデータを設定
        /// 特徴: 回避とブロッキングを多用する超慎重な行動
        /// </summary>
        private void SetHealthCriticalTestData()
        {
            // 超防御重視: 回避50%, ブロッキング30%, 弱攻撃のみ20%
            _backwardDodgeCount = 30;
            _horizontalDodgeCount = 15;
            _forwardDodgeCount = 5;
            _blockingCount = 30;
            _lightAttackCount = 20;
            _strongAttackCount = 0;
            _forwardDodgeAttackCount = 0;
            _horizontalDodgeAttackCount = 0;
            _strongAttackCancelCount = 0;

            _totalActionCount = _backwardDodgeCount + _horizontalDodgeCount + _forwardDodgeCount +
                             _blockingCount + _lightAttackCount + _strongAttackCount +
                             _strongAttackCancelCount + _horizontalDodgeAttackCount + _forwardDodgeAttackCount;
        }

        #endregion

        #region プライベートメソッド

        /// <summary>
        /// 実行回数から確率を計算
        /// </summary>
        /// <param name="count">実行回数</param>
        /// <returns>確率(0.0～1.0)</returns>
        private float CalculatePercentage(int count)
        {
            if (_totalActionCount == 0)
                return 0f;

            return (float)count / _totalActionCount;
        }

        #endregion

        #region デバッグ用メソッド

        /// <summary>
        /// 現在の確率情報をデバッグログに出力
        /// </summary>
        public void DebugLogProbabilities()
        {
            Debug.Log($"=== Action Probabilities (Total: {_totalActionCount}) ===");
            Debug.Log($"後ろ回避: {BackwardDodgePercentage:P2} ({_backwardDodgeCount}回)");
            Debug.Log($"横回避: {HorizontalDodgePercentage:P2} ({_horizontalDodgeCount}回)");
            Debug.Log($"前回避: {ForwardDodgePercentage:P2} ({_forwardDodgeCount}回)");
            Debug.Log($"ブロッキング: {BlockingPercentage:P2} ({_blockingCount}回)");
            Debug.Log($"弱攻撃: {LightAttackPercentage:P2} ({_lightAttackCount}回)");
            Debug.Log($"強攻撃: {StrongAttackPercentage:P2} ({_strongAttackCount}回)");
            Debug.Log($"強攻撃キャンセル: {StrongAttackCancelPercentage:P2} ({_strongAttackCancelCount}回)");
            Debug.Log($"横回避攻撃: {HorizontalDodgeAttackPercentage:P2} ({_horizontalDodgeAttackCount}回)");
            Debug.Log($"前回避攻撃: {ForwardDodgeAttackPercentage:P2} ({_forwardDodgeAttackCount}回)");
        }

        #endregion
    }
}