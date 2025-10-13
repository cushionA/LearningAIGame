using UnityEngine;

//==============================================ファイルヘッダ=========================================================
// MoveReportInfo
// 
// 概要: 移動システムからStateSystemへの報告用データ構造
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [クラスメンバ]
// - moveVector: 移動方向ベクトル（通常移動時に使用）
// - reportType: 移動種類（通常移動・前後左右回避）
// 
// [MovementReportType enum]
// - NormalMove: 通常移動の報告
// - FrontStep: 前回避の報告
// - LeftStep: 左回避の報告
// - RightStep: 右回避の報告
// - BackStep: 後ろ回避の報告
// 
// 
// 入力元クラス: MovementSystem
// 出力先クラス: StateSystem
// 
// その他:
// 回避行動は防御の一種として扱われ、DefenseInfoでも判定処理される
// 回避の成功・失敗判定（無敵時間による被弾回避）はDamageSystemが担当するため、
// この構造体は移動・回避行動の「開始」のみを報告する責務を持つ。
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Data
{
    /// <summary>
    /// 状態管理システムへの報告用構造体
    /// 移動の開始を報告する
    /// 回避の成功や失敗の報告はダメージシステムの責任
    /// （ヒットや防御が実際に行われ、成功や失敗が評価可能になるから）
    /// </summary>
    public struct MoveReportInfo
    {
        /// <summary>
        /// 移動ベクトル
        /// </summary>
        public Vector3 moveVector;

        /// <summary>
        /// 移動報告の区分
        /// </summary>
        public MovementReportType reportType;

        /// <summary>
        /// 報告用の情報を設定する
        /// インスタンスを使いまわすために後からセットできるように
        /// </summary>
        /// <param name="newVector"></param>
        /// <param name="type"></param>
        public void SetInfo(Vector3 newVector, MovementReportType type)
        {
            moveVector = newVector;
            reportType = type;
        }
    }

    /// <summary>
    /// 移動アクション関連の報告のタイプ
    /// </summary>
    public enum MovementReportType : byte
    {
        NormalMove,// 通常移動
        FrontStep,// 前回避
        LeftStep,// 左回避
        RightStep,// 右回避
        BackStep// 後ろ回避
    }
}