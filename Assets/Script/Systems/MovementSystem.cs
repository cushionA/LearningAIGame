using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using UnityEngine;

//==============================================ファイルヘッダ===========================================================
// MovementSystem
// 
// 概要: キャラクターの移動行動を管理するシステムクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 通常の歩行移動と回避ステップ(前後左右)の実行を行い、
// 移動情報をStateSystemに通知する。
// 
// 入力元クラス:BattleCharacterController
// 出力先クラス:StateSystem
// 
// その他:
// BaseSystem<MoveReportInfo>を継承
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Systems
{
    public class MovementSystem : BaseSystem<MoveReportInfo>
    {
        /// <summary>
        /// 移動アクションの報告用データ
        /// </summary>
        private MoveReportInfo _info;

        /// <summary>
        /// 歩行移動を開始するメソッド
        /// </summary>
        public void Move(Vector3 newVector)
        {
            // 移動を開始してStateSystemに報告
            moveController.MoveStart(newVector);
            _info.SetInfo(newVector, MovementReportType.NormalMove);
            NotifyObservers(_info);
        }

        /// <summary>
        /// 回避実行メソッド
        /// </summary>
        public void Avoid(MovementReportType stepType, float speed, float duration)
        {
            // 回避移動を開始する
            Vector3 moveVector = stepType switch
            {
                MovementReportType.FrontStep => Vector3.forward,
                MovementReportType.LeftStep => Vector3.left,
                MovementReportType.RightStep => Vector3.right,
                MovementReportType.BackStep => Vector3.back,
                _ => Vector3.zero  // defaultの場合
            };
            moveController.AddForce(moveVector * speed, duration);

            // 移動情報を報告する
            _info.SetInfo(Vector3.zero, stepType);
            NotifyObservers(_info);
        }
    }
}