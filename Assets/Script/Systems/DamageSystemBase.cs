using UnityEngine;
using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// DamageSystem
// 
// 概要: キャラクターの被ダメージ処理を管理するシステムクラスの基底クラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 敵からの攻撃を受け、自身の防御状態（ガード、ブロッキング、回避など）に基づいて
// 攻撃結果（ヒット、ガード成功、ブロッキング成功、回避成功など）を判定するクラスの基底クラス。
// テスト時のスタブを作成するための基底クラスとして設計。
// 
// 入力元クラス:HitSystem (敵の攻撃システム)
// 出力先クラス:StateSystem
// 
// その他:
// BaseSystem<DamageReportInfo>を継承
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Systems
{
    public abstract class DamageSystemBase : BaseSystem<DamageReportInfo>
    {
        #region フィールド

        /// <summary>
        /// ダメージ報告
        /// </summary>
        protected DamageReportInfo _info;

        #endregion

        #region 被ダメージ処理

        /// <summary>
        /// 敵からの攻撃を受け、攻撃結果を返す
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>攻撃結果（ヒット、ガード、ブロッキング、回避など）</returns>
        public abstract HitResultType Damage(in AttackInfo attackInfo);

        /// <summary>
        /// 敵の攻撃の最終結果を受け取り、オブザーバーに通知する
        /// HitSystemから攻撃判定が完全に終了した際に呼ばれる
        /// </summary>
        /// <param name="hitReport">敵の攻撃結果情報</param>
        public abstract void DamageReport(HitReportInfo hitReport);

        #endregion
    }
}