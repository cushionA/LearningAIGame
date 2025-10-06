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
// - テスト用に優勢/劣勢/均衡の状態データを自動生成可能
// 
// [主要プロパティ]
// - MyData: 自分のキャラクターデータ
// - RecentActionArray: 直近実行したアクションの配列
// - ActionLog: 行動ログの累積割合
// - HitSituations: 直近実行した攻撃ログ
// - DamageSituations: 直近実行した防御ログ
// 
// [主要メソッド]
// - AddActionLog: 行動履歴を追加
// - AddHitSituationLog: 攻撃状況を記録
// - AddDamageSituationLog: 被ダメージ状況を記録
// - SetTestData: テスト用に優勢/劣勢/均衡の状態データを設定
// 
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

        /// <summary>
        /// 与えたダメージ
        /// </summary>
        public int HitDamage { get; private set; }

        /// <summary>
        /// 受けたダメージ
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

        // ===== コンストラクタ =====

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

        // ===== 公開メソッド =====

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

        // ===== デバッグ用メソッド =====

        /// <summary>
        /// 現在のログデータをデバッグ出力
        /// </summary>
        public void DebugLogAllData()
        {
            Debug.Log("=== LLM Log Data ===");
            Debug.Log($"Recent Actions Count: {_recentActions.Count}");
            Debug.Log($"Hit Situations Count: {_hitSituations.Count}");
            Debug.Log($"Damage Situations Count: {_damageSituations.Count}");

            // 行動ログの確率を出力
            ActionLog.DebugLogProbabilities();
        }
    }
}