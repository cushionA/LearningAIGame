using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// AttackSystem
// 
// 概要: キャラクターの攻撃行動を管理するシステムクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 弱攻撃・強攻撃の実行、踏み込み移動の制御、攻撃のキャンセル処理を行い、
// 攻撃の開始・キャンセル情報をStateSystemに通知する。
// 
// 入力元クラス:BattleCharacterController
// 出力先クラス:StateSystem
// 
// その他:
// BaseSystem<AttackReportInfo>を継承
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Systems
{
    public class AttackSystem : BaseSystem<AttackReportInfo>
    {
        /// <summary>
        /// 攻撃処理の報告のデータ
        /// </summary>
        private AttackReportInfo _info;

        /// <summary>
        /// 弱攻撃を実行し、踏み込み移動を開始し、攻撃開始を報告します。
        /// </summary>
        /// <param name="damage">攻撃のダメージ。</param>
        /// <param name="stance">攻撃の方向。</param>
        /// <param name="moveVector">踏み込みの移動ベクトル。</param>
        /// <param name="moveDuration">踏み込みの継続時間。</param>
        public void WeakAttack(int damage, StanceType stance, Vector3 moveVector, float moveDuration)
        {
            // 踏み込み開始
            moveController.AddForce(moveVector, moveDuration);

            // 報告用データをセット
            _info.SetInfo(stance, damage, AttackReportType.WeakAttackStart);

            // 攻撃開始をNotifyObservers()で状態管理クラスに通知
            NotifyObservers(_info);
        }

        /// <summary>
        /// 強攻撃を実行し、踏み込み移動を開始し、攻撃開始を報告します。
        /// </summary>
        /// <param name="damage">攻撃のダメージ。</param>
        /// <param name="stance">攻撃の方向。</param>
        /// <param name="moveVector">踏み込みの移動ベクトル。</param>
        /// <param name="moveDuration">踏み込みの継続時間。</param>
        public void HeavyAttack(int damage, StanceType stance, Vector3 moveVector, float moveDuration)
        {
            // 踏み込み開始
            moveController.AddForce(moveVector, moveDuration);

            // 報告用データをセット
            _info.SetInfo(stance, damage, AttackReportType.HeavyAttackStart);

            // 攻撃開始をNotifyObservers()で状態管理クラスに通知
            NotifyObservers(_info);
        }

        /// <summary>
        /// 強攻撃をキャンセルし、移動を停止し、攻撃キャンセルを報告します。
        /// </summary>
        public void HeavyAttackCancel()
        {
            // 踏み込み停止
            moveController.Stop();

            // 報告用データをセット
            _info.SetInfo(StanceType.None, 0, AttackReportType.HeavyAttackCancel);

            // 攻撃キャンセルを状態管理クラスに通知
            NotifyObservers(_info);
        }
    }
}