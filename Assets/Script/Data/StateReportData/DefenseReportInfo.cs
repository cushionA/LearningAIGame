using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ=========================================================
// DefenseReportInfo
// 
// 概要: 防御システムからStateSystemへの報告用データ構造
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [構造体メンバ]
// - stance: 防御方向（上・左・右）
// - reportType: 報告種類（ガード方向変更 or ブロッキング開始）
// 
// [DefenseReportType enum]
// - StanceChange: ガード方向変更の報告
// - BlockingStart: ブロッキング開始の報告
// 
// 
// 入力元クラス: DefenseSystem
// 出力先クラス: StateSystem
// 
// その他:
// 判定結果ではなく行動開始の報告に特化した設計
// 防御の成功・失敗判定はDamageSystemが担当するため、
// この構造体は防御行動の「開始」のみを報告する責務を持つ。
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Data
{
    /// <summary>
    /// 状態管理システムへの報告用構造体
    /// 防御行動の開始とキャンセルを報告する
    /// 成功や失敗の報告はダメージシステムの責任（ヒットや防御が実際に行われ、成功や失敗が評価可能になるから）
    /// </summary>
    public struct DefenseReportInfo
    {
        /// <summary>
        /// 防御方向
        /// </summary>
        public StanceType stance; // 上、左、右の攻撃方向

        /// <summary>
        /// 報告内容
        /// </summary>
        public DefenseReportType reportType;
    }

    /// <summary>
    /// 防御関連の報告のタイプ
    /// </summary>
    public enum DefenseReportType : byte
    {
        StanceChange,// ガード方向変更
        BlockingStart,// ブロッキング開始
    }
}