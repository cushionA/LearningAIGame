using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ=========================================================
// DamageReportInfo
// 
// 概要: 被弾システムの報告・判定に関するデータ構造を定義
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// [DamageReportInfo]
// - 被ダメージ状況の報告用構造体
// - ダメージ量、実行した防御種類、受けた攻撃種類を記録
// 
// [DefenseInfo]
// - 防御判定の内部処理用構造体
// - 防御開始時刻、継続時間、防御タイプを管理
// - SetInfo: DefenseReportInfoまたはMoveReportInfoから防御情報を設定
// - IsDefenseSuccess: 攻撃に対する防御成否を判定（時間・方向・タイプを考慮）
// 
// [DefenseType enum]
// - Guard: ガード（弱攻撃のみ防御）
// - Blocking: ブロッキング（タイミング判定あり）
// - Avoid: 回避（無敵時間判定）
// - None: 防御不能状態
// 
// 入力元クラス: DefenseSystem, DamageSystem
// 出力先クラス: StateSystem, DamageSystem
// 
// その他:
// 防御判定のロジックはIsDefenseSuccessメソッドに集約
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Data
{
    /// <summary>
    /// 状態管理システムへの報告用構造体
    /// 攻撃被弾時の状況を報告する
    /// </summary>
    public struct DamageReportInfo
    {
        /// <summary>
        /// 受けたダメージ
        /// </summary>
        private int _damage;

        /// <summary>
        /// ヒット時の自分の行動
        /// </summary>
        private ActionState _defenseAction;

        /// <summary>
        /// 受けた攻撃の種類
        /// </summary>
        private AttackType _attackType;

        /// <summary>
        /// 受けたダメージ
        /// 0以外であれば防御失敗
        /// </summary>
        public int Damage => _damage;

        /// <summary>
        /// 実行した防御の種類
        /// </summary>
        public ActionState DefenseAction => _defenseAction;

        /// <summary>
        /// 受けた攻撃の種類
        /// </summary>
        public AttackType AttackType => _attackType;

        /// <summary>
        /// 報告用の被弾データをセットする
        /// </summary>
        public void SetInfo(in HitReportInfo hitReport, ActionState defenseAction)
        {
            _damage = hitReport.hitResultType == HitResultType.Hit ? hitReport.damage : 0;
            _attackType = hitReport.attackType;
            _defenseAction = defenseAction;
        }
    }

    /// <summary>
    /// 被弾時の自分の防御状況を表す列挙体
    /// </summary>
    public enum DefenseType : byte
    {
        Guard,// ガード
        Blocking,// ブロッキング
        Avoid,// 回避
        None // 防御不能状態
    }

    /// <summary>
    /// 防御開始報告を受けて作成する情報
    /// ダメージシステムで参照
    /// </summary>
    public struct DefenseInfo
    {
        /// <summary>
        /// 防御タイプ
        /// </summary>
        private DefenseType _defenseType;

        /// <summary>
        /// 現在の防御状態の判定が始まる時間
        /// </summary>
        private float _defenseStartTime;

        /// <summary>
        /// 現在の防御状態の判定が継続する時間
        /// </summary>
        private float _defenseDuration;

        /// <summary>
        /// 現在の防御状態を取得するための読み取り専用プロパティ
        /// </summary>
        public DefenseType CurrentDefense { get { return _defenseType; } }

        /// <summary>
        /// 報告内容に従い現在の防御情報を作成する
        /// </summary>
        /// <param name="reportInfo">報告データ</param>
        public void SetInfo(in DefenseReportInfo reportInfo, float startTime, float duration)
        {
            // ガードとブロッキングで処理を分ける
            // ブロッキング
            if (reportInfo.reportType == DefenseReportType.BlockingStart)
            {
                _defenseType = DefenseType.Blocking;
                _defenseStartTime = Time.time + startTime;
                _defenseDuration = duration;
            }

            // ガード
            else
            {
                _defenseType = DefenseType.Guard;

                // ガードは判定発生時間と継続時間が不要
                _defenseStartTime = -1;
                _defenseDuration = 0;
            }
        }

        /// <summary>
        /// 報告内容に従い現在の防御情報を作成する
        /// 回避アクションの情報を受け付けるオーバーロード
        /// </summary>
        /// <param name="reportInfo">報告データ</param>
        public void SetInfo(in MoveReportInfo reportInfo, float startTime, float duration)
        {
            // 通常移動なら戻る
            if (reportInfo.reportType == MovementReportType.NormalMove)
            {
                return;
            }

            // 回避情報を入れる
            _defenseType = DefenseType.Blocking;
            _defenseStartTime = Time.time + startTime;
            _defenseDuration = duration;
        }

        /// <summary>
        /// 入力に基づいて防御情報を作成する
        /// </summary>
        /// <param name="reportInfo">報告データ</param>
        public void SetInfo(DefenseType type, float startTime, float duration)
        {
            _defenseType = type;
            _defenseStartTime = Time.time + startTime;
            _defenseDuration = duration;
        }

        /// <summary>
        /// 攻撃に対する防御が成功したかを返すメソッド
        /// </summary>
        /// <param name="attackInfo"></param>
        /// <param name="defenseStance"></param>
        /// <param name="attackType"></param>
        /// <returns>攻撃の実行結果</returns>
        public HitResultType IsDefenseSuccess(in AttackInfo attackInfo, StanceType defenseStance)
        {
            // ガード方向か防御タイプがない場合は無条件で被弾
            if (defenseStance == StanceType.None || _defenseType == DefenseType.None)
            {
                Debug.Log($"[DefenseInfo] 防御なし");
                return HitResultType.Hit;
            }

            // 攻撃結果
            HitResultType result = HitResultType.Hit;

            // 効果時間内であるかを確認する
            bool hasEffect = (Time.time >= _defenseStartTime) && (Time.time <= _defenseStartTime + _defenseDuration);
            Debug.Log($"[DefenseInfo] 防御判定: hasEffect={hasEffect}, Time={Time.time}, Start={_defenseStartTime}, Duration={_defenseDuration}");

            // 防御方向があっているかを確認する
            // 左右の防御方向は対応が逆になるので注意
            bool matchStance = ((defenseStance != attackInfo.stance) && (defenseStance != StanceType.Up && attackInfo.stance != StanceType.Up)) ||
                ((defenseStance == attackInfo.stance) && (defenseStance == StanceType.Up));

            // 防御タイプごとに結果を設定
            switch (_defenseType)
            {
                // ガード時
                case DefenseType.Guard:
                    result = (attackInfo.attackType == AttackType.WeakAttack && matchStance) ? HitResultType.Guard : result;
                    Debug.Log($"[DefenseInfo] ガード判定: hasEffect={hasEffect}, matchStance={matchStance}, Result={result}");
                    break;
                // 回避時
                case DefenseType.Avoid:
                    result = hasEffect ? HitResultType.Avoid : result;
                    Debug.Log($"[DefenseInfo] 回避判定: hasEffect={hasEffect}, Result={result}");
                    break;
                // ブロッキング時
                case DefenseType.Blocking:
                    result = hasEffect && matchStance ? HitResultType.Block : result;
                    break;
            }

            return result;
        }
    }
}