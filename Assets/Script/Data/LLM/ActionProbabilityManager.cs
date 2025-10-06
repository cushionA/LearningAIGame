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
        private int _guardCount;
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
        /// ガードの実行確率
        /// </summary>
        public float GuardPercentage => CalculatePercentage(_guardCount);

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

        /// <summary>
        /// コンストラクタ。基本確率で初期化
        /// </summary>
        public ActionProbabilityManager()
        {
            InitializeBasicProbabilities();
        }

        /// <summary>
        /// 全ての実行回数をリセット
        /// </summary>
        public void ResetCounts()
        {

            _backwardDodgeCount = 0;
            _horizontalDodgeCount = 0;
            _forwardDodgeCount = 0;
            _guardCount = 0;
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
            _guardCount = Mathf.RoundToInt(baseTotal * 0.05f);                  // 5
            _blockingCount = Mathf.RoundToInt(baseTotal * 0.05f);               // 5
            _lightAttackCount = Mathf.RoundToInt(baseTotal * 0.25f);            // 25
            _strongAttackCount = Mathf.RoundToInt(baseTotal * 0.20f);           // 20
            _strongAttackCancelCount = Mathf.RoundToInt(baseTotal * 0.05f);     // 5
            _horizontalDodgeAttackCount = Mathf.RoundToInt(baseTotal * 0.10f);  // 10
            _forwardDodgeAttackCount = Mathf.RoundToInt(baseTotal * 0.10f);     // 10

            // 合計を再計算
            _totalActionCount = _backwardDodgeCount + _horizontalDodgeCount + _forwardDodgeCount +
                             _guardCount + _blockingCount + _lightAttackCount + _strongAttackCount +
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
                case ActionState.ガード:
                    _guardCount++;
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
                case ActionState.ガード:
                    return _guardCount;
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

        /// <summary>
        /// 現在の確率情報をデバッグログに出力
        /// </summary>
        public void DebugLogProbabilities()
        {
            Debug.Log($"=== Action Probabilities (Total: {_totalActionCount}) ===");
            Debug.Log($"後ろ回避: {BackwardDodgePercentage:P2} ({_backwardDodgeCount}回)");
            Debug.Log($"横回避: {HorizontalDodgePercentage:P2} ({_horizontalDodgeCount}回)");
            Debug.Log($"前回避: {ForwardDodgePercentage:P2} ({_forwardDodgeCount}回)");
            Debug.Log($"ガード: {GuardPercentage:P2} ({_guardCount}回)");
            Debug.Log($"ブロッキング: {BlockingPercentage:P2} ({_blockingCount}回)");
            Debug.Log($"弱攻撃: {LightAttackPercentage:P2} ({_lightAttackCount}回)");
            Debug.Log($"強攻撃: {StrongAttackPercentage:P2} ({_strongAttackCount}回)");
            Debug.Log($"強攻撃キャンセル: {StrongAttackCancelPercentage:P2} ({_strongAttackCancelCount}回)");
            Debug.Log($"横回避攻撃: {HorizontalDodgeAttackPercentage:P2} ({_horizontalDodgeAttackCount}回)");
            Debug.Log($"前回避攻撃: {ForwardDodgeAttackPercentage:P2} ({_forwardDodgeAttackCount}回)");
        }
    }
}