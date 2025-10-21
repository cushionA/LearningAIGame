using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ=========================================================
// HitReportInfo
// 
// 概要: 攻撃システムの結果報告と実行情報に関するデータ構造を定義
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [HitReportInfo]
// - 攻撃ヒット時の結果報告用構造体
// - 与えたダメージ量、攻撃種類、実行結果（ヒット/ガード/ブロック/回避/空振り）を記録
// 
// [AttackInfo]
// - 攻撃実行中の情報を保持する構造体
// - ダメージ、攻撃種類、攻撃方向を管理
// - SetInfo: AttackReportInfoから攻撃情報を設定
// - DamageSystemが防御判定時に参照
// 
// [AttackType enum]
// - WeakAttack: 弱攻撃
// - HeavyAttack: 強攻撃
// 
// [HitResultType enum]
// - Hit: 被弾成功
// - Guard: ガードされた
// - Block: ブロッキングされた
// - Avoid: 回避された
// - Miss: 空振り（初期値）
// 
// 入力元クラス: DamageSystem
// 出力先クラス: StateSystem
// 
// その他:
// AttackInfoは攻撃実行中の情報、HitReportInfoは結果報告という責務の違いがある
//=====================================================================================================================

namespace LearningAIGame.CombatSystem.Data
{
    /// <summary>
    /// 状態管理システムへの報告用構造体
    /// 攻撃ヒット時の状況を報告する
    /// </summary>
    public struct HitReportInfo
    {
        /// <summary>
        /// 与えたダメージ
        /// 0であれば攻撃失敗
        /// </summary>
        public int damage;

        /// <summary>
        /// 実行した攻撃の種類
        /// </summary>
        public AttackType attackType;

        /// <summary>
        /// 攻撃の実行結果
        /// </summary>
        public HitResultType hitResultType;

        /// <summary>
        /// 攻撃開始時にダメージと攻撃の種類を設定する
        /// </summary>
        public void InitializeDamage(in AttackInfo info)
        {
            damage = info.damage;
            attackType = info.attackType;
        }

        /// <summary>
        /// 結果を設定する
        /// </summary>
        /// <param name="result"></param>
        public void SetResult(HitResultType result)
        {
            hitResultType = result;
        }
    }

    /// <summary>
    /// ヒットさせた時の自分の攻撃状況を表す列挙体
    /// </summary>
    public enum AttackType : byte
    {
        WeakAttack,
        HeavyAttack,
        NoAttack
    }

    /// <summary>
    /// ヒットさせた時の自分の攻撃状況を表す列挙体
    /// </summary>
    public enum HitResultType : byte
    {
        Block,
        Guard,
        Avoid,
        Hit,
        Stun,// 敵攻撃により中断
        Cancel,// 自分でキャンセルした
        Miss // 空振り。初期値
    }

    /// <summary>
    /// 攻撃の実行時情報
    /// ダメージシステムが参照する
    /// </summary>
    public struct AttackInfo
    {
        /// <summary>
        /// 現在実行中の攻撃のダメージ
        /// </summary>
        public int damage;

        /// <summary>
        /// 現在実行中の攻撃の種類
        /// </summary>
        public AttackType attackType;

        /// <summary>
        /// 現在実行中の攻撃の攻撃方向
        /// </summary>
        public StanceType stance;

        /// <summary>
        /// 報告内容に従い現在の攻撃情報を作成する
        /// </summary>
        /// <param name="reportInfo">報告データ</param>
        public void SetInfo(AttackReportInfo reportInfo)
        {
            damage = reportInfo.damage;
            stance = reportInfo.stance;
            attackType = reportInfo.reportType == AttackReportType.WeakAttackStart ? AttackType.WeakAttack : AttackType.HeavyAttack;
        }
    }

}