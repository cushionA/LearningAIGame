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
        /// ヒットした時の状態（自身の行動）
        /// </summary>
        public ActionState HitState { get; set; }

        /// <summary>
        /// ヒット時の敵の行動（敵の攻撃・行動）
        /// </summary>
        public ActionState HitType { get; set; }

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
            HitType = attackType;
            GetDamage = damage;
        }

        /// <summary>
        /// ダメージ報告情報からヒット情報を作成する
        /// こちらをメインで使う
        /// </summary>
        /// <param name="reportInfo"></param>
        public HitSituation(in DamageReportInfo reportInfo)
        {
            // プロパティに引数を代入
            HitState = reportInfo.DefenseAction;
            HitType = reportInfo.AttackType == AttackType.WeakAttack ? ActionState.弱攻撃 : ActionState.強攻撃;
            GetDamage = reportInfo.Damage;
        }
    }
}