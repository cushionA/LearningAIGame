using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ=========================================================
// AttackReportInfo
// 
// 概要: 攻撃システムからStateSystemへの報告用データ構造
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [構造体メンバ]
// - stance: 攻撃方向（上・左・右）
// - damage: ダメージ値
// - reportType: 報告種類（弱攻撃開始・強攻撃開始・強攻撃キャンセル）
// 
// [AttackReportType enum]
// - WeakAttackStart: 弱攻撃開始の報告
// - HeavyAttackStart: 強攻撃開始の報告
// - HeavyAttackCancel: 強攻撃キャンセルの報告
// 
// 
// 入力元クラス: AttackSystem
// 出力先クラス: StateSystem
// 
// その他:
// 判定結果ではなく行動開始/キャンセルの報告に特化した設計
// 攻撃の成功・失敗判定（ヒット・ガード・ブロック）はDamageSystemが担当するため、
// この構造体は攻撃行動の「開始・キャンセル」のみを報告する責務を持つ。
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Data
{
    /// <summary>
    /// 状態管理システムへの報告用構造体
    /// 攻撃の開始とキャンセルを報告する
    /// 成功や失敗の報告はダメージシステムの責任（ヒットや防御が実際に行われ、成功や失敗が評価可能になるから）
    /// </summary>
    public struct AttackReportInfo
    {
        public StanceType stance;           // 上、左、右の攻撃方向
        public int damage;                // ダメージの値
        public AttackReportType reportType; // 開始、キャンセル、のどれか

        /// <summary>
        /// 報告用の情報を設定する
        /// インスタンスを使いまわすために後からセットできるように
        /// </summary>
        public void SetInfo(StanceType newStance, int nextDamage, AttackReportType type)
        {
            stance = newStance;
            damage = nextDamage;
            reportType = type;
        }
    }

    /// <summary>
    /// 攻撃関連の報告のタイプ
    /// </summary>
    public enum AttackReportType : byte
    {
        WeakAttackStart,// 弱攻撃開始
        HeavyAttackStart,// 強攻撃開始
        HeavyAttackCancel// 強攻撃キャンセル
    }
}