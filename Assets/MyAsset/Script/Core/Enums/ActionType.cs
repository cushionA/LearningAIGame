using System.Runtime.CompilerServices;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// アクションの種類を定義する列挙型
    /// </summary>
    public enum ActionType : byte
    {
        /// <summary>
        /// 歩行移動
        /// </summary>
        Walk,

        /// <summary>
        /// ジャンプ
        /// </summary>
        Jump,

        /// <summary>
        /// ブースト移動
        /// </summary>
        Boost,

        /// <summary>
        /// 回避行動
        /// </summary>

        Dodge,

        /// <summary>
        /// 弱攻撃
        /// </summary>
        WeakAttack,

        /// <summary>
        /// 強攻撃
        /// </summary>
        StrongAttack,

        /// <summary>
        /// スキル攻撃
        /// </summary>
        SkillAttack,

        /// <summary>
        /// 弱射撃
        /// </summary>
        WeakShoot,

        /// <summary>
        /// 強射撃
        /// </summary>
        StrongShoot,

        /// <summary>
        /// ガード
        /// </summary>
        Guard,

        /// <summary>
        /// ブロッキング
        /// </summary>
        Block,

        /// <summary>
        /// エクステンション兵器使用
        /// </summary>
        Extension,

        /// <summary>
        /// モード切り替え
        /// </summary>
        ModeSwitch,

        /// <summary>
        /// 回避攻撃
        /// </summary>
        DodgeAttack,

        /// <summary>
        /// 空中攻撃
        /// </summary>
        AerialAttack,

        /// <summary>
        /// コンボ攻撃
        /// </summary>
        ComboAttack,

        /// <summary>
        /// 空中チャージ
        /// </summary>
        AirCharge,

        /// <summary>
        /// 二段ジャンプ
        /// </summary>
        DoubleJump,
        Maneuver
    }

    /// <summary>
    /// 攻撃の種類を定義する列挙型
    /// </summary>
    public enum AttackType : byte
    {
        /// <summary>
        /// 攻撃なし
        /// </summary>
        None,

        /// <summary>
        /// 弱近接攻撃
        /// </summary>
        WeakMelee,

        /// <summary>
        /// 強近接攻撃
        /// </summary>
        StrongMelee,

        /// <summary>
        /// 近接スキル攻撃
        /// </summary>
        MeleeSkill,

        /// <summary>
        /// 弱射撃攻撃
        /// </summary>
        WeakRanged,

        /// <summary>
        /// 強射撃攻撃
        /// </summary>
        StrongRanged,

        /// <summary>
        /// 射撃スキル攻撃
        /// </summary>
        RangedSkill,

        /// <summary>
        /// 空中弱攻撃
        /// </summary>
        AerialWeakMelee,

        /// <summary>
        /// 空中強攻撃
        /// </summary>
        AerialStrongMelee,

        /// <summary>
        /// 回避攻撃
        /// </summary>
        DodgeAttack
    }

    /// <summary>
    /// 防御の種類を定義する列挙型
    /// </summary>
    public enum DefenseType : byte
    {
        /// <summary>
        /// 通常ガード
        /// </summary>
        Guard,

        /// <summary>
        /// ジャストブロッキング
        /// </summary>
        Block,

        /// <summary>
        /// 回避
        /// </summary>
        Dodge,

        /// <summary>
        /// エネルギーバリア
        /// </summary>
        EnergyBarrier
    }

    /// <summary>
    /// 武器カテゴリを定義する列挙型
    /// </summary>
    public enum WeaponCategory : byte
    {
        /// <summary>
        /// 高速型武器（双剣、短剣など）
        /// </summary>
        Fast,

        /// <summary>
        /// バランス型武器（長剣、刀など）
        /// </summary>
        Balanced,

        /// <summary>
        /// パワー型武器（大剣、斧など）
        /// </summary>
        Power,

        /// <summary>
        /// リーチ型武器（槍、薙刀など）
        /// </summary>
        Reach
    }
}
