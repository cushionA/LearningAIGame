using UnityEngine;
using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// DamageSystem
// 
// 概要: キャラクターの被ダメージ処理を管理するシステムクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 敵からの攻撃を受け、自身の防御状態（ガード、ブロッキング、回避など）に基づいて
// 攻撃結果（ヒット、ガード成功、ブロッキング成功、回避成功など）を判定する。
// 判定結果をStateSystemに通知し、キャラクターの状態遷移を促す。
// HitSystemと対になる、1v1戦闘における被攻撃側の処理を担当。
// 
// 入力元クラス:HitSystem (敵の攻撃システム)
// 出力先クラス:StateSystem
// 
// その他:
// DamageSystemBaseを継承
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Systems
{
    public class DamageSystemStab : DamageSystemBase
    {
        #region フィールド

        /// <summary>
        /// 設定したヒット結果
        /// スタブのパラメータ
        /// </summary>
        public HitResultType hitResult;

        /// <summary>
        /// 攻撃を受けた瞬間の自分のアクション状態
        /// スタブのパラメータ
        /// </summary>
        public ActionState damageTimeAction;

        #endregion

        #region 被ダメージ処理

        /// <summary>
        /// 敵からの攻撃を受け、攻撃結果を返す
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>攻撃結果（ヒット、ガード、ブロッキング、回避など）</returns>
        public override HitResultType Damage(in AttackInfo attackInfo)
        {
            return hitResult;
        }

        /// <summary>
        /// 敵の攻撃の最終結果を受け取り、オブザーバーに通知する
        /// HitSystemから攻撃判定が完全に終了した際に呼ばれる
        /// </summary>
        /// <param name="hitReport">敵の攻撃結果情報</param>
        public override void DamageReport(HitReportInfo hitReport)
        {
            // 被ダメージ情報を設定
            _info.SetInfo(hitReport, damageTimeAction);

            // StateSystemに通知
            NotifyObservers(_info);
        }

        #endregion
    }
}