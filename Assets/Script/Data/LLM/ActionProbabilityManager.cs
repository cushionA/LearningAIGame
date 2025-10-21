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
        #region 定数

        /// <summary>
        /// 行動記録数の上限
        /// この数を超えたら圧縮が実行される
        /// </summary>
        private const int k_MaxActionCount = 100;

        /// <summary>
        /// 圧縮後の目標行動記録数
        /// 上限を超えた際にこの数まで圧縮される
        /// </summary>
        private const int k_CompressedActionCount = 10;

        #endregion

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
        public float HeavyAttackPercentage => CalculatePercentage(_strongAttackCount);

        /// <summary>
        /// 強攻撃キャンセルの実行確率
        /// </summary>
        public float HeavyAttackCancelPercentage => CalculatePercentage(_strongAttackCancelCount);

        /// <summary>
        /// 横回避攻撃の実行確率
        /// </summary>
        public float HorizontalDodgeAttackPercentage => CalculatePercentage(_horizontalDodgeAttackCount);

        /// <summary>
        /// 前回避攻撃の実行確率
        /// </summary>
        public float ForwardDodgeAttackPercentage => CalculatePercentage(_forwardDodgeAttackCount);

        /// <summary>
        /// 最も実行されている攻撃アクション
        /// </summary>
        public ActionState MostUsedAttack
        {
            get
            {
                int maxCount = 0;
                ActionState mostUsed = ActionState.弱攻撃;

                // 攻撃行動のみを比較
                if (_lightAttackCount > maxCount)
                {
                    maxCount = _lightAttackCount;
                    mostUsed = ActionState.弱攻撃;
                }
                if (_strongAttackCount > maxCount)
                {
                    maxCount = _strongAttackCount;
                    mostUsed = ActionState.強攻撃;
                }
                if (_strongAttackCancelCount > maxCount)
                {
                    maxCount = _strongAttackCancelCount;
                    mostUsed = ActionState.強攻撃キャンセル;
                }
                if (_horizontalDodgeAttackCount > maxCount)
                {
                    maxCount = _horizontalDodgeAttackCount;
                    mostUsed = ActionState.横回避攻撃;
                }
                if (_forwardDodgeAttackCount > maxCount)
                {
                    mostUsed = ActionState.前回避攻撃;
                }

                return mostUsed;
            }
        }

        /// <summary>
        /// 最も実行されていない攻撃アクション
        /// </summary>
        public ActionState LeastUsedAttack
        {
            get
            {
                int minCount = int.MaxValue;
                ActionState leastUsed = ActionState.弱攻撃;

                // 攻撃行動のみを比較
                if (_lightAttackCount < minCount)
                {
                    minCount = _lightAttackCount;
                    leastUsed = ActionState.弱攻撃;
                }
                if (_strongAttackCount < minCount)
                {
                    minCount = _strongAttackCount;
                    leastUsed = ActionState.強攻撃;
                }
                if (_strongAttackCancelCount < minCount)
                {
                    minCount = _strongAttackCancelCount;
                    leastUsed = ActionState.強攻撃キャンセル;
                }
                if (_horizontalDodgeAttackCount < minCount)
                {
                    minCount = _horizontalDodgeAttackCount;
                    leastUsed = ActionState.横回避攻撃;
                }
                if (_forwardDodgeAttackCount < minCount)
                {
                    leastUsed = ActionState.前回避攻撃;
                }

                return leastUsed;
            }
        }

        /// <summary>
        /// 最も実行されている防御アクション
        /// </summary>
        public ActionState MostUsedDefense
        {
            get
            {
                int maxCount = 0;
                ActionState mostUsed = ActionState.後ろ回避;

                // 防御行動のみを比較
                if (_backwardDodgeCount > maxCount)
                {
                    maxCount = _backwardDodgeCount;
                    mostUsed = ActionState.後ろ回避;
                }
                if (_horizontalDodgeCount > maxCount)
                {
                    maxCount = _horizontalDodgeCount;
                    mostUsed = ActionState.横回避;
                }
                if (_forwardDodgeCount > maxCount)
                {
                    maxCount = _forwardDodgeCount;
                    mostUsed = ActionState.前回避;
                }
                if (_blockingCount > maxCount)
                {
                    mostUsed = ActionState.ブロッキング;
                }

                return mostUsed;
            }
        }

        /// <summary>
        /// 最も実行されていない防御アクション
        /// </summary>
        public ActionState LeastUsedDefense
        {
            get
            {
                int minCount = int.MaxValue;
                ActionState leastUsed = ActionState.後ろ回避;

                // 防御行動のみを比較
                if (_backwardDodgeCount < minCount)
                {
                    minCount = _backwardDodgeCount;
                    leastUsed = ActionState.後ろ回避;
                }
                if (_horizontalDodgeCount < minCount)
                {
                    minCount = _horizontalDodgeCount;
                    leastUsed = ActionState.横回避;
                }
                if (_forwardDodgeCount < minCount)
                {
                    minCount = _forwardDodgeCount;
                    leastUsed = ActionState.前回避;
                }
                if (_blockingCount < minCount)
                {
                    leastUsed = ActionState.ブロッキング;
                }

                return leastUsed;
            }
        }

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

            // 上限チェックと圧縮処理
            CheckAndCompressIfNeeded();
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

        /// <summary>
        /// 行動記録数が上限を超えていないかチェックし、必要に応じて圧縮する
        /// </summary>
        private void CheckAndCompressIfNeeded()
        {
            if (_totalActionCount > k_MaxActionCount)
            {
                CompressActionCounts();
            }
        }

        /// <summary>
        /// 各行動の実行回数を圧縮する
        /// 確率の比率を保ったまま、合計をk_CompressedActionCountに近づける
        /// </summary>
        private void CompressActionCounts()
        {
            // 圧縮率を計算
            float compressionRatio = (float)k_CompressedActionCount / _totalActionCount;

            // 各カウントを圧縮（最低1は保証し、0の場合は0のまま）
            _backwardDodgeCount = CompressCount(_backwardDodgeCount, compressionRatio);
            _horizontalDodgeCount = CompressCount(_horizontalDodgeCount, compressionRatio);
            _forwardDodgeCount = CompressCount(_forwardDodgeCount, compressionRatio);
            _blockingCount = CompressCount(_blockingCount, compressionRatio);
            _lightAttackCount = CompressCount(_lightAttackCount, compressionRatio);
            _strongAttackCount = CompressCount(_strongAttackCount, compressionRatio);
            _strongAttackCancelCount = CompressCount(_strongAttackCancelCount, compressionRatio);
            _horizontalDodgeAttackCount = CompressCount(_horizontalDodgeAttackCount, compressionRatio);
            _forwardDodgeAttackCount = CompressCount(_forwardDodgeAttackCount, compressionRatio);

            // 合計を再計算
            _totalActionCount = _backwardDodgeCount + _horizontalDodgeCount + _forwardDodgeCount +
                             _blockingCount + _lightAttackCount + _strongAttackCount +
                             _strongAttackCancelCount + _horizontalDodgeAttackCount + _forwardDodgeAttackCount;

            Debug.Log($"行動記録を圧縮しました。新しい合計回数: {_totalActionCount}");
        }

        /// <summary>
        /// 単一のカウント値を圧縮する
        /// </summary>
        /// <param name="count">元のカウント</param>
        /// <param name="ratio">圧縮率</param>
        /// <returns>圧縮後のカウント</returns>
        private int CompressCount(int count, float ratio)
        {
            if (count == 0)
                return 0;

            // 圧縮後の値を計算（最低1は保証）
            int compressed = Mathf.Max(1, Mathf.RoundToInt(count * ratio));
            return compressed;
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
            Debug.Log($"強攻撃: {HeavyAttackPercentage:P2} ({_strongAttackCount}回)");
            Debug.Log($"強攻撃キャンセル: {HeavyAttackCancelPercentage:P2} ({_strongAttackCancelCount}回)");
            Debug.Log($"横回避攻撃: {HorizontalDodgeAttackPercentage:P2} ({_horizontalDodgeAttackCount}回)");
            Debug.Log($"前回避攻撃: {ForwardDodgeAttackPercentage:P2} ({_forwardDodgeAttackCount}回)");
            Debug.Log($"\n=== 攻撃パターン分析 ===");
            Debug.Log($"最も使用した攻撃: {MostUsedAttack}");
            Debug.Log($"最も使用しない攻撃: {LeastUsedAttack}");
            Debug.Log($"\n=== 防御パターン分析 ===");
            Debug.Log($"最も使用した防御: {MostUsedDefense}");
            Debug.Log($"最も使用しない防御: {LeastUsedDefense}");
        }

        #endregion
    }
}