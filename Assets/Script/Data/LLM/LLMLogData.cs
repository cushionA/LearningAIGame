//==============================================ファイルヘッダ=========================================================
// LLMLogData
// 
// 概要: LLMへの入力用に戦闘データを記録・管理するクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [LLMLogData]
// - AI戦略判断のためのログデータを一元管理
// - 直近の行動履歴、攻撃・防御の状況、行動の累積割合を記録
// - テスト用に優勢/劣勢/拮抗/エネルギー不足/体力危険の状態データを自動生成可能
// 
// [主要プロパティ]
// - MyData: 自分のキャラクターデータ
// - RecentActionArray: 直近実行したアクションの配列
// - ActionLog: 行動ログの累積割合
// - HitSituations: 直近実行した攻撃ログ
// - DamageSituations: 直近実行した防御ログ
// - HitDamage: 与えたダメージの累積値
// - TakeDamage: 受けたダメージの累積値
// 
// [主要メソッド]
// - AddActionLog: 行動履歴を追加
// - AddHitSituationLog: 攻撃状況を記録
// - AddDamageSituationLog: 被ダメージ状況を記録
// - SetTestData: テスト用に各状況の戦闘データを設定
// 
// 入力元クラス: StateSystem, DamageSystem
// 出力先クラス: LLMプロンプト生成システム
// 
// その他:
// FixedLengthListを使用して固定長の履歴を管理し、メモリ効率を確保
// 戦闘中のリアルタイムデータ収集とLLMへのフィードバック情報生成を担当
//=====================================================================================================================

using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Utilities;
using LLMDataArchitect;
using System;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

namespace LLMDataArchitect
{
    /// <summary>
    /// LLMへの入力用に戦闘データを記録・管理するクラス
    /// </summary>
    public class LLMLogData
    {
        #region プロパティ

        /// <summary>
        /// 直近実行したアクションの配列
        /// FixedLengthListで固定長管理
        /// </summary>
        public FixedLengthList<ActionState> RecentActionArray
        {
            get => _recentActions;
        }

        /// <summary>
        /// 行動ログの累積割合
        /// 各アクションの実行頻度を管理
        /// </summary>
        public ActionProbabilityManager ActionLog { get; set; }

        /// <summary>
        /// 直近実行した攻撃ログ
        /// 自分の攻撃がヒット/ガード/ブロック等の結果を記録
        /// </summary>
        public FixedLengthList<HitSituation> HitSituations
        {
            get => _hitSituations;
        }

        /// <summary>
        /// 直近実行した防御ログ
        /// 敵の攻撃による被ダメージ履歴（防御成功も含む）
        /// </summary>
        public FixedLengthList<HitSituation> DamageSituations
        {
            get => _damageSituations;
        }

        #region 直近集計

        /// <summary>
        /// 直近の行動履歴から最も実行している攻撃アクション
        /// </summary>
        public ActionState RecentMostUsedAttack
        {
            get
            {
                if (_recentActions.Count == 0)
                    return ActionState.デフォルト攻撃;

                ActionState mostUsed = ActionState.デフォルト攻撃;
                int maxCount = 0;

                var recentActions = _recentActions.GetInOrder();

                // 攻撃行動のみをカウント
                Span<ActionState> attackActions = stackalloc[]
                {
                    ActionState.弱攻撃,
                    ActionState.強攻撃,
                    ActionState.強攻撃キャンセル,
                    ActionState.横回避攻撃,
                    ActionState.前回避攻撃
                };

                foreach (var attackAction in attackActions)
                {
                    int count = 0;
                    foreach (var action in recentActions)
                    {
                        if (action == attackAction)
                            count++;
                    }

                    if (count > maxCount)
                    {
                        maxCount = count;
                        mostUsed = attackAction;
                    }
                }

                return mostUsed;
            }
        }

        /// <summary>
        /// 直近の行動履歴から最も実行している防御アクション
        /// </summary>
        public ActionState RecentMostUsedDefense
        {
            get
            {
                if (_recentActions.Count == 0)
                    return ActionState.デフォルト防御;

                ActionState mostUsed = ActionState.デフォルト防御;
                int maxCount = 0;

                var recentActions = _recentActions.GetInOrder();

                // 防御行動のみをカウント
                Span<ActionState> defenseActions = stackalloc[]
                {
                    ActionState.後ろ回避,
                    ActionState.横回避,
                    ActionState.前回避,
                    ActionState.ブロッキング,
                    ActionState.ガード
                };

                foreach (var defenseAction in defenseActions)
                {
                    int count = 0;
                    foreach (var action in recentActions)
                    {
                        if (action == defenseAction)
                            count++;
                    }

                    if (count > maxCount)
                    {
                        maxCount = count;
                        mostUsed = defenseAction;
                    }
                }

                return mostUsed;
            }
        }

        #endregion

        /// <summary>
        /// 与えたダメージの累積値
        /// </summary>
        public int HitDamage { get; private set; }

        /// <summary>
        /// 受けたダメージの累積値
        /// </summary>
        public int TakeDamage { get; private set; }

        #endregion

        #region フィールド

        /// <summary>
        /// 内部で行動履歴を記録するためのフィールド
        /// </summary>
        private FixedLengthList<ActionState> _recentActions;

        /// <summary>
        /// 自分の攻撃履歴
        /// </summary>
        private FixedLengthList<HitSituation> _hitSituations;

        /// <summary>
        /// 敵の攻撃による被ダメージ履歴（防御成功も含む）
        /// </summary>
        private FixedLengthList<HitSituation> _damageSituations;

        #endregion

        #region コンストラクタ

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="actionHistorySize">行動履歴の最大記録数</param>
        /// <param name="hitSituationSize">攻撃状況の最大記録数</param>
        /// <param name="damageSituationSize">被ダメージ状況の最大記録数</param>
        public LLMLogData(int actionHistorySize = 7, int hitSituationSize = 7, int damageSituationSize = 7)
        {
            _recentActions = new FixedLengthList<ActionState>(actionHistorySize);
            _hitSituations = new FixedLengthList<HitSituation>(hitSituationSize);
            _damageSituations = new FixedLengthList<HitSituation>(damageSituationSize);
            ActionLog = new ActionProbabilityManager();
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 行動履歴を追加する
        /// RecentActionArrayとActionLogの両方に記録
        /// </summary>
        /// <param name="action">実行したアクション</param>
        public void AddActionLog(ActionState action)
        {
            _recentActions.Add(action);
            ActionLog.AddAction(action);
        }

        /// <summary>
        /// 攻撃状況を記録する
        /// 自分の攻撃結果（ヒット/ガード/ブロック等）を記録
        /// </summary>
        /// <param name="situation">攻撃状況</param>
        public void AddHitSituationLog(HitSituation situation)
        {
            _hitSituations.Add(situation);
            HitDamage += situation.GetDamage;
        }

        /// <summary>
        /// 被ダメージ状況を記録する
        /// 敵の攻撃を受けた結果を記録
        /// </summary>
        /// <param name="situation">被ダメージ状況</param>
        public void AddDamageSituationLog(HitSituation situation)
        {
            _damageSituations.Add(situation);
            TakeDamage += situation.GetDamage;
        }

        /// <summary>
        /// 全ての直近ログデータをクリア
        /// 累積ログは残す
        /// </summary>
        public void ClearAllRecentLogs()
        {
            _recentActions.Clear();
            _hitSituations.Clear();
            _damageSituations.Clear();
            HitDamage = 0;
            TakeDamage = 0;
        }

        #endregion

        #region テスト用メソッド

        /// <summary>
        /// テスト用に戦況パターンに応じたデータを設定
        /// TestSituationType列挙型に基づいて各状況のデータを生成
        /// </summary>
        /// <param name="situationType">テスト戦況の種類</param>
        public void SetTestData(TestSituationType situationType)
        {
            ClearAllRecentLogs();

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
        /// 特徴: 攻撃成功率高、被ダメージ少、積極的な攻撃行動
        /// </summary>
        private void SetAdvantageTestData()
        {
            // 行動履歴: 攻撃を多めに設定
            AddActionLog(ActionState.弱攻撃);
            AddActionLog(ActionState.弱攻撃);
            AddActionLog(ActionState.強攻撃);
            AddActionLog(ActionState.前回避攻撃);
            AddActionLog(ActionState.弱攻撃);
            AddActionLog(ActionState.横回避攻撃);
            AddActionLog(ActionState.強攻撃);

            // 攻撃状況: 高い成功率（敵は小怯み・大怯みが多い）
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 30));
            AddHitSituationLog(new HitSituation(ActionState.大怯み, ActionState.強攻撃, 45));
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 28));
            AddHitSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 10));
            AddHitSituationLog(new HitSituation(ActionState.大怯み, ActionState.強攻撃, 50));
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 32));
            AddHitSituationLog(new HitSituation(ActionState.後ろ回避, ActionState.弱攻撃, 0));

            // 被ダメージ状況: 防御成功が多い
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 8));
            AddDamageSituationLog(new HitSituation(ActionState.横回避, ActionState.弱攻撃, 0));
            AddDamageSituationLog(new HitSituation(ActionState.ブロッキング成功, ActionState.強攻撃, 0));
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 10));
            AddDamageSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 15));
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 7));
            AddDamageSituationLog(new HitSituation(ActionState.前回避, ActionState.強攻撃, 0));
        }

        /// <summary>
        /// 拮抗状態のテストデータを設定
        /// 特徴: 攻防バランスの取れた状況、成功率約50%
        /// </summary>
        private void SetBalanceTestData()
        {
            // 行動履歴: 攻撃と防御のバランス
            AddActionLog(ActionState.弱攻撃);
            AddActionLog(ActionState.ガード);
            AddActionLog(ActionState.前回避);
            AddActionLog(ActionState.強攻撃);
            AddActionLog(ActionState.横回避);
            AddActionLog(ActionState.弱攻撃);
            AddActionLog(ActionState.ブロッキング);

            // 攻撃状況: 成功と失敗が半々
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 28));
            AddHitSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 10));
            AddHitSituationLog(new HitSituation(ActionState.大怯み, ActionState.強攻撃, 42));
            AddHitSituationLog(new HitSituation(ActionState.横回避, ActionState.弱攻撃, 0));
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 25));
            AddHitSituationLog(new HitSituation(ActionState.ブロッキング成功, ActionState.強攻撃, 0));
            AddHitSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 12));

            // 被ダメージ状況: 中程度のダメージ
            AddDamageSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 25));
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 12));
            AddDamageSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 28));
            AddDamageSituationLog(new HitSituation(ActionState.前回避, ActionState.強攻撃, 0));
            AddDamageSituationLog(new HitSituation(ActionState.大怯み, ActionState.強攻撃, 40));
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 10));
            AddDamageSituationLog(new HitSituation(ActionState.ブロッキング成功, ActionState.弱攻撃, 0));
        }

        /// <summary>
        /// 劣勢状態のテストデータを設定
        /// 特徴: 攻撃成功率低、被ダメージ多、防御的な行動
        /// </summary>
        private void SetDisadvantageTestData()
        {
            // 行動履歴: 防御を多めに設定
            AddActionLog(ActionState.ガード);
            AddActionLog(ActionState.後ろ回避);
            AddActionLog(ActionState.弱攻撃);
            AddActionLog(ActionState.ガード);
            AddActionLog(ActionState.横回避);
            AddActionLog(ActionState.ブロッキング);
            AddActionLog(ActionState.後ろ回避);

            // 攻撃状況: 低い成功率（防御・回避されることが多い）
            AddHitSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 8));
            AddHitSituationLog(new HitSituation(ActionState.ブロッキング成功, ActionState.強攻撃, 0));
            AddHitSituationLog(new HitSituation(ActionState.横回避, ActionState.弱攻撃, 0));
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 20));
            AddHitSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 10));
            AddHitSituationLog(new HitSituation(ActionState.ガード, ActionState.弱攻撃, 0)); // 空振り
            AddHitSituationLog(new HitSituation(ActionState.前回避, ActionState.強攻撃, 0));

            // 被ダメージ状況: 多いダメージ、防御失敗多め
            AddDamageSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 38));
            AddDamageSituationLog(new HitSituation(ActionState.大怯み, ActionState.強攻撃, 52));
            AddDamageSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 35));
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 15));
            AddDamageSituationLog(new HitSituation(ActionState.大怯み, ActionState.強攻撃, 48));
            AddDamageSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 37));
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.強攻撃, 18));
        }

        /// <summary>
        /// エネルギー不足状態のテストデータを設定
        /// 特徴: 強攻撃や回避攻撃が少なく、エネルギー消費の少ない行動が多い
        /// </summary>
        private void SetEnergyShortageTestData()
        {
            // 行動履歴: エネルギー消費の少ない行動中心（弱攻撃、ガード、回避）
            AddActionLog(ActionState.弱攻撃);
            AddActionLog(ActionState.ガード);
            AddActionLog(ActionState.弱攻撃);
            AddActionLog(ActionState.横回避);
            AddActionLog(ActionState.弱攻撃);
            AddActionLog(ActionState.ガード);
            AddActionLog(ActionState.後ろ回避);

            // 攻撃状況: 弱攻撃のみで低ダメージ
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 18));
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 20));
            AddHitSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 8));
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 22));
            AddHitSituationLog(new HitSituation(ActionState.横回避, ActionState.弱攻撃, 0));
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 19));
            AddHitSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 9));

            // 被ダメージ状況: 防御成功も混在するが中程度のダメージ
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 12));
            AddDamageSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 28));
            AddDamageSituationLog(new HitSituation(ActionState.前回避, ActionState.弱攻撃, 0));
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 10));
            AddDamageSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 30));
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 11));
            AddDamageSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 26));
        }

        /// <summary>
        /// 体力危険状態のテストデータを設定
        /// 特徴: 防御行動が多く、被弾を避けようとする慎重な行動パターン
        /// </summary>
        private void SetHealthCriticalTestData()
        {
            // 行動履歴: 防御と回避を中心とした慎重な行動
            AddActionLog(ActionState.後ろ回避);
            AddActionLog(ActionState.ガード);
            AddActionLog(ActionState.後ろ回避);
            AddActionLog(ActionState.ブロッキング);
            AddActionLog(ActionState.横回避);
            AddActionLog(ActionState.ガード);
            AddActionLog(ActionState.弱攻撃);

            // 攻撃状況: 攻撃機会が少なく、慎重な攻撃
            AddHitSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 10));
            AddHitSituationLog(new HitSituation(ActionState.後ろ回避, ActionState.弱攻撃, 0));
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 22));
            AddHitSituationLog(new HitSituation(ActionState.ガード, ActionState.弱攻撃, 0)); // 空振り
            AddHitSituationLog(new HitSituation(ActionState.横回避, ActionState.弱攻撃, 0));
            AddHitSituationLog(new HitSituation(ActionState.小怯み, ActionState.弱攻撃, 25));
            AddHitSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 9));

            // 被ダメージ状況: 防御成功が多いが、被弾すると大きなダメージ
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 10));
            AddDamageSituationLog(new HitSituation(ActionState.前回避, ActionState.弱攻撃, 0));
            AddDamageSituationLog(new HitSituation(ActionState.ブロッキング成功, ActionState.強攻撃, 0));
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 8));
            AddDamageSituationLog(new HitSituation(ActionState.大怯み, ActionState.強攻撃, 55));
            AddDamageSituationLog(new HitSituation(ActionState.ガード成功, ActionState.弱攻撃, 9));
            AddDamageSituationLog(new HitSituation(ActionState.後ろ回避, ActionState.強攻撃, 0));
        }

        #endregion

        #region デバッグ用メソッド

        /// <summary>
        /// 現在のログデータをデバッグ出力
        /// </summary>
        public void DebugLogAllData()
        {
            Debug.Log("=== LLM Log Data ===");
            Debug.Log($"Recent Actions Count: {_recentActions.Count}");
            Debug.Log($"Hit Situations Count: {_hitSituations.Count}");
            Debug.Log($"Damage Situations Count: {_damageSituations.Count}");
            Debug.Log($"Total Hit Damage: {HitDamage}");
            Debug.Log($"Total Take Damage: {TakeDamage}");

            // 行動ログの確率を出力
            ActionLog.DebugLogProbabilities();
        }

        #endregion
    }
}