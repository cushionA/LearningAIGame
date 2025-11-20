using LearningAIGame.CombatSystem.Data;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

namespace LLMDataArchitect
{
    /// <summary>
    /// 攻撃ヒット時の状況をまとめる
    /// </summary>
    public struct HitSituation
    {
        /// <summary>
        /// ヒットした時の与ダメージ側状態
        /// </summary>
        public ActionState HitState { get; set; }

        /// <summary>
        /// ヒット時の被ダメージ側行動
        /// </summary>
        public ActionState DamageState { get; set; }

        /// <summary>
        /// 与えた/受けたダメージ
        /// </summary>
        // 2. GetDamage の型を float に修正し、より正確なダメージ計算に対応
        public int GetDamage { get; set; }

        // 3. コンストラクタを完成させる
        public HitSituation(ActionState hitState, ActionState attackType, int damage)
        {
            // プロパティに引数を代入
            HitState = hitState;
            DamageState = attackType;
            GetDamage = damage;
        }


        /// <summary>
        /// ヒット時情報からヒット情報を作成する
        /// こちらをメインで使う
        /// </summary>
        /// <param name="reportInfo"></param>
        public HitSituation(in HitReportInfo reportInfo)
        {
            HitState = reportInfo.attackType == AttackType.WeakAttack ? ActionState.弱攻撃 : ActionState.強攻撃;
            // プロパティに引数を代入
            switch (reportInfo.hitResultType)
            {
                case HitResultType.Block:
                    DamageState = ActionState.ブロッキング;
                    break;
                case HitResultType.Avoid:
                    DamageState = ActionState.回避;
                    break;
                case HitResultType.Stun:
                    DamageState = ActionState.弱攻撃;
                    break;
                case HitResultType.Cancel:
                    DamageState = ActionState.ガード;
                    HitState = ActionState.強攻撃キャンセル;
                    break;
                default:
                    DamageState = ActionState.ガード;
                    break;
            }


            GetDamage = reportInfo.damage;
        }

        /// <summary>
        /// ダメージ報告情報からヒット情報を作成する
        /// こちらをメインで使う
        /// </summary>
        /// <param name="reportInfo"></param>
        public HitSituation(in DamageReportInfo reportInfo)
        {
            // プロパティに引数を代入
            HitState = reportInfo.AttackType == AttackType.WeakAttack ? ActionState.弱攻撃 : ActionState.強攻撃;
            DamageState = reportInfo.DefenseAction;
            GetDamage = reportInfo.Damage;
        }
    }
}