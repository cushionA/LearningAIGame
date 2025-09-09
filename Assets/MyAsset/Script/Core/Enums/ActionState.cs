using System.Runtime.CompilerServices;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// キャラクターの行動状態を定義する列挙型
    /// </summary>
    public enum ActionState : byte
    {
        /// <summary>
        /// 待機状態 - 何もアクションを行っていない
        /// </summary>
        Idle,

        /// <summary>
        /// 歩行中 - 通常の移動を行っている
        /// </summary>
        Walking,

        /// <summary>
        /// ジャンプ中 - 上昇または落下中
        /// </summary>
        Jumping,

        /// <summary>
        /// 落下中 - 重力により下降している
        /// </summary>
        Falling,

        /// <summary>
        /// ブースト中 - エネルギーを消費した高速移動
        /// </summary>
        Boosting,

        /// <summary>
        /// 回避中 - 無敵フレーム付きの回避行動
        /// </summary>
        Dodging,

        /// <summary>
        /// 攻撃中 - 近接攻撃または射撃攻撃を実行中
        /// </summary>
        Attacking,

        /// <summary>
        /// ガード中 - 防御態勢を取っている
        /// </summary>
        Guarding,

        /// <summary>
        /// マニューバ実行中 - 事前記録した移動パターンを実行中
        /// </summary>
        UsingManeuver,

        /// <summary>
        /// スタン中 - 行動不能状態
        /// </summary>
        Stunned,

        /// <summary>
        /// 怯み中 - 短時間の行動制限状態
        /// </summary>
        Flinching,
        /// <summary>
        /// 空中でのチャージ中 - 空中でジャンプを溜めている状態
        /// </summary>
        AirCharge
    }
}
