using System.Runtime.CompilerServices;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// キャラクターの行動モードを定義する列挙型
    /// </summary>
    public enum ActionMode : byte
    {
        /// <summary>
        /// 近接戦闘モード - 剣による攻撃とガード・ブロッキングが可能
        /// </summary>
        Melee,

        /// <summary>
        /// 射撃戦闘モード - 銃火器による攻撃とエクステンション使用が可能
        /// </summary>
        Ranged,

        /// <summary>
        /// エネルギー切れ時の特殊バリアモード - 強力シールドとスタンゲージシステムが有効
        /// </summary>
        EnergyBarrier
    }
}
