using LLMDataArchitectTest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using R3;
using System;
using System.Collections.Generic;
using UnityEngine;

//=====================================================================================================================
// LearningAIGame
// 
// 概要: 各アクションから情報を受け取り、状態を管理してLLMプロンプト用のデータを作成するクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// Publicのメンバには以下のものがある：
// 
// [プロパティ]
// - Energy: 現在のエネルギー量（枯渇中は0を返す）
// - CurrentAttackInfo: 実行中の攻撃情報（ダメージシステムが参照）
// - CurrentState: 現在の行動状態（リアクティブプロパティ、アニメーション制御用）
// - MoveVector: 移動方向ベクトル（リアクティブプロパティ）
// - CurrentStance: 現在の構え方向（リアクティブプロパティ）
// - CanAttack, CanChangeGuardDirection, CanBlock, CanAvoid, CanCancelHeavyAttack: 各行動の実行可否
// 
// [メソッド]
// - UseEnergy(int): エネルギーを消費する
// - SetNeutral(): ニュートラル状態に戻す（アニメーション完了時に呼ぶ）
// - GetAttackResult(AttackInfo): 攻撃の結果判定（ダメージシステムから呼ぶ）
// - OnDamage(DamageReportInfo): 被ダメージ報告を受け付ける
// - Deconstruct(...): LLM出力用のデータを取得する
// 
// 入力元クラス:各アクションのクラス
// 出力先クラス:キャラコントローラー（主に）
// 
// その他:
// 各アクション実装時は「報告用データ定義」や「購読用メソッド」のRegionで定義・使用されているデータを投げてください
//
// 残対応について
// - 購読処理は結合時に行う予定
// - 各リテラル部分は設定データに置き換え予定（ブロッキング時EN回復量・毎秒EN回復量・ブロッキングと回避判定の発生や持続など）
//=====================================================================================================================
public class StateSystem : MonoBehaviour
{
    #region 報告用データ定義

    /// <summary>
    /// キャラクターの基礎データ
    /// LLMの判断で使用する情報をまとめる
    /// </summary>
    public class CharacterData
    {
        /// <summary>
        /// 現在の体力
        /// </summary>
        public int Hp { get; set; }

        /// <summary>
        /// 最大体力
        /// </summary>
        public int MaxHp { get; set; }

        /// <summary>
        /// 現在のエネルギー
        /// </summary>
        public int Energy { get; set; }

        /// <summary>
        /// 最大エネルギー
        /// </summary>
        public int MaxEnergy { get; set; }

        /// <summary>
        /// エネルギー切れかどうかを返すプロパティ
        /// JsonIgnore属性を付与して、シリアライズ時に無視されるようにする
        /// </summary>
        [JsonIgnore]
        public bool IsEnergyExhaust { get; private set; }

        /// <summary>
        /// 死んでいるかを返すプロパティ
        /// 真なら死亡
        /// </summary>
        [JsonIgnore]
        public bool IsDead { get { return Hp <= 0; } }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public CharacterData()
        {

        }

        /// <summary>
        /// 最大体力と最大エネルギーを指定するコンストラクタ
        /// </summary>
        /// <param name="maxHp"></param>
        /// <param name="maxEnergy"></param>
        public CharacterData(int maxHp, int maxEnergy)
        {
            MaxHp = maxHp;
            Hp = maxHp;
            MaxEnergy = maxEnergy;
            Energy = maxEnergy;
        }

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        public void TakeDamage(int amount)
        {
            Hp = Math.Max(0, Hp - amount);
        }

        /// <summary>
        /// エネルギーを消費する
        /// </summary>
        public void ConsumeEnergy(int amount)
        {
            Energy = Math.Max(0, Energy - amount);
            if (Energy <= 0)
            {
                IsEnergyExhaust = true;
            }
        }

        /// <summary>
        /// 割合でエネルギーを回復する
        /// </summary>
        public void RecoverEnergyByRate(float ratio)
        {
            int recoverAmount = (int)(MaxEnergy * (ratio * 0.01));
            Energy = Math.Min(MaxEnergy, Energy + recoverAmount);

            // エネルギー枯渇時、最大まで回復すれば枯渇解除
            if (Energy >= MaxEnergy)
            {
                Energy = MaxEnergy;
                IsEnergyExhaust = false;
            }
        }
    }

    #region 攻撃用報告データ定義

    /// <summary>
    /// 状態管理システムへの報告用構造体
    /// 攻撃の開始とキャンセルを報告する
    /// 成功や失敗の報告はダメージシステムの責任（ヒットや防御が実際に行われ、成功や失敗が評価可能になるから）
    /// </summary>
    public struct AttackReportInfo
    {
        public StanceType stance;           // 上、左、右の攻撃方向
        public int damage;                // ダメージの値
        public AttackReportType reportType; // 開始、キャンセル、のどれか
    }

    /// <summary>
    /// 攻撃関連の報告のタイプ
    /// </summary>
    public enum AttackReportType : byte
    {
        WeakAttackStart,// 弱攻撃開始
        HeavyAttackStart,// 強攻撃開始
        HeavyAttackCancel// 強攻撃キャンセル
    }

    #endregion

    #region 防御用報告データ定義

    /// <summary>
    /// 状態管理システムへの報告用構造体
    /// 防御行動の開始とキャンセルを報告する
    /// 成功や失敗の報告はダメージシステムの責任（ヒットや防御が実際に行われ、成功や失敗が評価可能になるから）
    /// </summary>
    public struct DefenseReportInfo
    {
        /// <summary>
        /// 防御方向
        /// </summary>
        public StanceType stance;           // 上、左、右の攻撃方向

        /// <summary>
        /// 報告内容
        /// </summary>
        public DefenseReportType reportType;
    }

    /// <summary>
    /// 防御関連の報告のタイプ
    /// </summary>
    public enum DefenseReportType : byte
    {
        StanceChange,// ガード方向変更
        BlockingStart,// ブロッキング開始
    }

    #endregion

    #region 移動アクション用報告データ定義

    /// <summary>
    /// 状態管理システムへの報告用構造体
    /// 移動の開始を報告する
    /// 回避の成功や失敗の報告はダメージシステムの責任
    /// （ヒットや防御が実際に行われ、成功や失敗が評価可能になるから）
    /// </summary>
    public class MoveReportInfo
    {
        /// <summary>
        /// 移動ベクトル
        /// </summary>
        public Vector3 moveVector;

        /// <summary>
        /// 移動報告の区分
        /// </summary>
        public MovementReportType reportType;
    }

    /// <summary>
    /// 移動アクション関連の報告のタイプ
    /// </summary>
    public enum MovementReportType : byte
    {
        NormalMove,// 通常移動
        FrontStep,// 前回避
        LeftStep,// 左回避
        RightStep,// 右回避
        BackStep// 後ろ回避
    }

    #endregion

    #region 被ダメージ状況記録用データ定義

    /// <summary>
    /// 状態管理システムへの報告用構造体
    /// 攻撃被弾時の状況を報告する
    /// </summary>
    public struct DamageReportInfo
    {
        /// <summary>
        /// 受けたダメージ
        /// 0以外であれば防御失敗
        /// </summary>
        public int damage;

        /// <summary>
        /// 実行した防御の種類
        /// </summary>
        public DefenseType defenseType;

        /// <summary>
        /// 受けた攻撃の種類
        /// </summary>
        public AttackType attackType;
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

    #endregion

    #region 与ダメージ状況記録用データ定義

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
    }

    /// <summary>
    /// ヒットさせた時の自分の攻撃状況を表す列挙体
    /// </summary>
    public enum AttackType : byte
    {
        WeakAttack,
        HeavyAttack
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
        Miss// 空振り。初期値
    }

    #endregion

    #endregion

    #region クラス定義

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
        /// 報告内容に従い現在の攻撃情報を作成する
        /// </summary>
        /// <param name="reportInfo">報告データ</param>
        public void SetInfo(in DefenseReportInfo reportInfo)
        {
            // ガードとブロッキングで処理を分ける
            // ブロッキング
            if (reportInfo.reportType == DefenseReportType.BlockingStart)
            {
                _defenseType = DefenseType.Blocking;

                // とりあえずリテラルで入れておきますが、最終的には設定データに置き換えます
                _defenseStartTime = Time.time + 0.1f;
                _defenseDuration = 0.4f;
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
        /// 報告内容に従い現在の攻撃情報を作成する
        /// 回避アクションの情報を受け付けるオーバーロード
        /// </summary>
        /// <param name="reportInfo">報告データ</param>
        public void SetInfo(in MoveReportInfo reportInfo)
        {
            // 通常移動なら戻る
            if (reportInfo.reportType == MovementReportType.NormalMove)
            {
                return;
            }

            // 回避情報を入れる
            _defenseType = DefenseType.Blocking;

            // とりあえずリテラルで入れますが、最終的には設定データに置き換えます
            _defenseStartTime = Time.time + 0.5f;
            _defenseDuration = 0.8f;
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
            // ガード方向ない場合は無条件で被弾
            if (defenseStance != StanceType.None)
            {
                return HitResultType.Hit;
            }

            // 攻撃結果
            HitResultType result = HitResultType.Hit;

            // 効果時間内であるかを確認する
            bool hasEffect = (Time.time >= _defenseStartTime) && (Time.time <= _defenseStartTime + _defenseDuration);

            // 防御方向があっているかを確認する
            // 左右の防御方向は対応が逆になるので注意
            bool matchStance = ((defenseStance != attackInfo.stance) && (defenseStance != StanceType.Up)) ||
                ((defenseStance == attackInfo.stance) && (defenseStance == StanceType.Up));

            // 防御タイプごとに結果を設定
            switch (_defenseType)
            {
                // ガード時
                case DefenseType.Guard:
                    result = (attackInfo.attackType == AttackType.WeakAttack && matchStance) ? HitResultType.Guard : result;
                    break;
                // 回避時
                case DefenseType.Avoid:
                    result = hasEffect ? HitResultType.Avoid : result;
                    break;
                // ブロッキング時
                case DefenseType.Blocking:
                    result = hasEffect && matchStance ? HitResultType.Block : result;
                    break;
            }

            return result;
        }
    }

    #endregion

    #region 列挙型定義

    /// <summary>
    /// 現在の行動状況と行動履歴を管理するための列挙体
    /// 各アクションの始動・結果報告を受けて切り替わる
    /// アニメーション制御にも使用する
    /// </summary>
    [System.Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ActionState
    {
        // --- 固有アクション/状態の定義 ---
        ガード = 1 << 0,  // デフォルト状態。歩行中もガード
        ブロッキング = 1 << 1,
        前回避 = 1 << 2,
        横回避 = 1 << 3,
        後ろ回避 = 1 << 4,
        前回避攻撃 = 1 << 5,
        横回避攻撃 = 1 << 6,
        弱攻撃 = 1 << 7,
        強攻撃 = 1 << 8,
        ブロッキング成功 = 1 << 9,
        ガード成功 = 1 << 10,
        強攻撃キャンセル = 1 << 11,

        // --- 攻防関連時の状態（モーション差分は行動不能時間の長さ） ---
        小怯み = 1 << 12, // 弱攻撃被弾モーション
        大怯み = 1 << 13,// 強攻撃被弾モーション
        弱攻撃ブロッキング = 1 << 14,// 弱攻撃をブロッキングされた時のモーション。
        強攻撃ブロッキング = 1 << 15,// 強攻撃をブロッキングされた時のモーション
        弱攻撃ガード = 1 << 16,// 弱攻撃をガードされた時のモーション。

        // --- 特殊状態フラグ ---
        死亡 = 1 << 17,

        // --- 複合フラグ（論理和 '|' を使用） ---
        ガード方向切り替え可能 = ガード | ブロッキング成功 | ガード成功,
        ブロッキング可能 = ガード | ブロッキング成功 | ガード成功,
        回避可能 = ガード | ブロッキング成功 | ガード成功,
        攻撃可能 = ガード | ブロッキング成功 | ガード成功,
        強攻撃キャンセル可能 = 強攻撃,
        行動履歴に記録しない = ブロッキング成功 | ガード成功 | 小怯み | 大怯み | 弱攻撃ブロッキング | 強攻撃ブロッキング | 弱攻撃ガード | 死亡
    }

    /// <summary>
    /// 攻撃/防御の方向
    /// </summary>
    public enum StanceType : byte
    {
        Up,     // 上
        Left,   // 左
        Right,  // 右
        None    // ガード無し
    }

    #endregion

    #region フィールド

    /// <summary>
    /// キャラの基礎データ
    /// LLMへの報告用
    /// </summary>
    private CharacterData _characterData;

    /// <summary>
    /// 攻撃を受けた際の状況を記録する
    /// LLMへの報告用
    /// </summary>
    private List<HitSituation> _damageSituation;

    /// <summary>
    /// 自分の行動を記録するリスト
    /// LLMへの報告用
    /// </summary>
    private List<ActionState> _actHistory;

    /// <summary>
    /// 攻撃の実行情報
    /// コールバックとプロパティを通じてアクセス
    /// </summary>
    private AttackInfo _attackInfo;

    /// <summary>
    /// 防御の実行情報
    /// コールバックとメソッドを通じてのみアクセス
    /// </summary>
    private DefenseInfo _defenseInfo;

    #endregion

    #region Publicプロパティ

    /// <summary>
    /// 読み取り専用の残りエネルギー（枯渇中は常に 0）
    ///</summary>
    public int Energy { get { return _characterData.IsEnergyExhaust ? 0 : _characterData.Energy; } }

    /// <summary>
    /// 読み取り専用の攻撃情報
    /// </summary>
    public AttackInfo CurrentAttackInfo { get { return _attackInfo; } }

    #region アニメーション管理リアクティブプロパティ

    /// <summary>
    /// 現在の行動状態のリアクティブプロパティ
    /// </summary>
    public ReactiveProperty<ActionState> CurrentState;

    /// <summary>
    /// 現在の歩行方向を示すリアクティブプロパティ
    /// </summary>
    public ReactiveProperty<Vector3> MoveVector;

    /// <summary>
    /// 現在の構え方向のリアクティブプロパティ
    /// </summary>
    public ReactiveProperty<StanceType> CurrentStance;

    #endregion

    #region 行動実行可能管理プロパティ

    /// <summary>
    /// 攻撃可能かどうか
    /// </summary>
    public bool CanAttack { get { return !_characterData.IsEnergyExhaust && (CurrentState.CurrentValue & ActionState.攻撃可能) > 0; } }

    /// <summary>
    /// ガード方向切り替え可能かどうか
    /// </summary>
    public bool CanChangeGuardDirection { get { return (CurrentState.CurrentValue & ActionState.ガード方向切り替え可能) > 0; } }

    /// <summary>
    /// ブロッキング可能かどうか
    /// </summary>
    public bool CanBlock { get { return !_characterData.IsEnergyExhaust && (CurrentState.CurrentValue & ActionState.ブロッキング可能) > 0; } }

    /// <summary>
    /// 回避可能かどうか
    /// </summary>
    public bool CanAvoid { get { return (CurrentState.CurrentValue & ActionState.回避可能) > 0; } }

    /// <summary>
    /// 強攻撃をキャンセル可能かどうか
    /// </summary>
    public bool CanCancelHeavyAttack { get { return !_characterData.IsEnergyExhaust && (CurrentState.CurrentValue & ActionState.強攻撃キャンセル可能) > 0; } }

    #endregion

    #endregion

    /// <summary>
    /// 内部状態を更新する（フレーム更新時などに呼ばれる）
    /// </summary>
    public void Update()
    {
        // 毎フレームエネルギーの回復
        // リテラルは設定データに置き換え予定
        _characterData.RecoverEnergyByRate(Time.deltaTime * 3); // 1秒に3%回復
    }

    #region Publicメソッド

    /// <summary>
    /// エネルギーを使用する
    /// 各アクションでエネルギーを使用する際に使う
    /// </summary>
    /// <param name="amount">使用量</param>
    public void UseEnergy(int amount)
    {
        _characterData.ConsumeEnergy(amount);
    }

    /// <summary>
    /// アニメ完了を待ってニュートラル状態に戻りガードを開始する
    /// アニメ管理クラスから各モーション終了時に呼ぶ想定
    /// </summary>
    public void SetNeutral()
    {
        ChangeState(ActionState.ガード);
    }

    /// <summary>
    /// ある攻撃がヒットした際の結果を返すメソッド
    /// ダメージシステムから使用する
    /// </summary>
    /// <param name="attackInfo">攻撃の情報</param>
    /// <returns>ヒット、回避、ガード、の中のどの結果であるかを返す</returns>
    public HitResultType GetAttackResult(in AttackInfo attackInfo)
    {
        return _defenseInfo.IsDefenseSuccess(attackInfo, CurrentStance.CurrentValue);
    }

    /// <summary>
    /// LLMへの出力データ作成用のデコンストラクタ
    /// </summary>
    /// <param name="hitSituations"></param>
    /// <param name="actionHistory"></param>
    /// <param name="characterData"></param>
    public void Deconstruct(out HitSituation[] hitSituations, out ActionState[] actionHistory, out CharacterData characterData)
    {
        hitSituations = _damageSituation.ToArray();
        actionHistory = _actHistory.ToArray();
        characterData = _characterData;
    }

    #endregion

    #region privateメソッド

    #region 購読用メソッド

    /// <summary>
    /// ダメージを受けた状況を記録する
    /// </summary>
    private void OnHit(HitReportInfo hitReport)
    {
        // 攻撃結果がブロッキングとガードならリアクションをする
        // ブロッキングされた場合
        if (hitReport.hitResultType == HitResultType.Block)
        {
            ChangeState(hitReport.attackType == AttackType.WeakAttack ? ActionState.弱攻撃ブロッキング : ActionState.強攻撃キャンセル);
        }

        // ガードされた場合
        else if (hitReport.hitResultType == HitResultType.Guard)
        {
            ChangeState(ActionState.弱攻撃ガード);
        }
    }

    /// <summary>
    /// ダメージを受けた状況を記録する
    /// </summary>
    public void OnDamage(DamageReportInfo damageReport)
    {
        // 被弾している場合
        if (damageReport.damage != 0)
        {
            _characterData.TakeDamage(damageReport.damage);

            // 死んでいたら死亡状態に移行
            if (_characterData.IsDead)
            {
                ChangeState(ActionState.死亡);
            }

            // 弱攻撃の場合
            if (damageReport.attackType == AttackType.WeakAttack)
            {
                ChangeState(ActionState.小怯み);
            }
            else
            {
                ChangeState(ActionState.大怯み);
            }

            // 被弾状況を追加
            _damageSituation.Add(new HitSituation(damageReport, CurrentState.CurrentValue));
        }
        // 防御成功した場合
        else
        {
            // ガード成功
            if (damageReport.defenseType == DefenseType.Guard)
            {
                ChangeState(ActionState.ガード成功);
            }

            // ブロッキング成功
            else
            {
                ChangeState(ActionState.ブロッキング成功);

                // ブロッキング成功時10パーセントエネルギー回復
                // リテラルは設定データに置き換え予定
                _characterData.RecoverEnergyByRate(20);
            }
        }
    }

    /// <summary>
    /// 防御関連のコールバックで呼ばれるメソッド
    /// 構え方向の変更とブロッキング開始を報告する
    /// ガード切り替え受け付け
    /// </summary>
    private void OnDefense(DefenseReportInfo defenseReport)
    {
        // 構え方向の変更
        CurrentStance.Value = defenseReport.stance;

        // 行動切り替え
        ChangeState(defenseReport.reportType == DefenseReportType.StanceChange ? ActionState.ガード : ActionState.ブロッキング);

        // 防御データの設定
        _defenseInfo.SetInfo(defenseReport);
    }

    /// <summary>
    /// 攻撃関連のコールバックで呼ばれるメソッド
    /// 弱攻撃と強攻撃の開始、強攻撃キャンセルを報告する
    /// </summary>
    private void OnAttack(AttackReportInfo attackReport)
    {
        // 切り替える行動状態
        ActionState useState;
        // キャンセル行動であるかどうか
        bool isCancel = false;

        // 現在の行動と報告内容に応じて処理を切り替え
        switch (attackReport.reportType)
        {
            case AttackReportType.WeakAttackStart:
                // 回避攻撃の場合もモーションは全て同じ弱攻撃でいい
                // 前回避攻撃になるかをチェック
                if (CurrentState.CurrentValue == ActionState.前回避)
                {
                    useState = ActionState.前回避攻撃;
                    isCancel = true;
                }
                // 横回避攻撃になるかをチェック
                else if (CurrentState.CurrentValue == ActionState.横回避)
                {
                    useState = ActionState.横回避攻撃;
                    isCancel = true;
                }
                else
                {
                    useState = ActionState.弱攻撃;
                }
                break;

            // 強攻撃開始
            case AttackReportType.HeavyAttackStart:
                // 強攻撃に状態切り替え
                useState = ActionState.強攻撃;
                break;

            // 強攻撃キャンセル
            case AttackReportType.HeavyAttackCancel:
                useState = ActionState.強攻撃キャンセル;
                isCancel = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // 攻撃方向切り替え
        CurrentStance.Value = attackReport.stance;

        // 行動切り替え
        ChangeState(useState, isCancel);

        // 攻撃データを設定
        _attackInfo.SetInfo(attackReport);
    }

    /// <summary>
    /// 攻撃関連のコールバックで呼ばれるメソッド
    /// 弱攻撃と強攻撃の開始、強攻撃キャンセルを報告する
    /// </summary>
    private void OnMovement(MoveReportInfo moveReport)
    {
        switch (moveReport.reportType)
        {
            // 通常移動
            case MovementReportType.NormalMove:
                MoveVector.Value = moveReport.moveVector;
                ChangeState(ActionState.ガード);
                break;
            case MovementReportType.FrontStep:
                ChangeState(ActionState.前回避);
                break;
            case MovementReportType.LeftStep:
                ChangeState(ActionState.横回避);
                break;
            case MovementReportType.RightStep:
                ChangeState(ActionState.横回避);
                break;
            case MovementReportType.BackStep:
                ChangeState(ActionState.後ろ回避);
                break;
            default:
                break;
        }

        _defenseInfo.SetInfo(moveReport);
    }

    #endregion

    /// <summary>
    /// 行動切り替えに使用するメソッド
    /// </summary>
    /// <param name="newState">切り替え先の行動</param>
    /// <param name="isCnancel">強攻撃キャンセルなどのキャンセル行動であるか</param>
    private void ChangeState(ActionState newState, bool isCancel = false)
    {
        // 同じ状態への変更は無視
        if (newState == CurrentState.CurrentValue)
            return;

        // キャンセル行動の場合記録せず切り替える
        if (isCancel)
        {
            // 行動切り替え
            CurrentState.Value = newState;
        }
        else
        {
            // 行動履歴への記録対象外でなければ
            if ((CurrentState.CurrentValue & ActionState.行動履歴に記録しない) == 0)
            {
                // 行動切り替え前に現在の行動を履歴に保存
                this._actHistory.Add(CurrentState.CurrentValue);
            }

            // 行動切り替え
            CurrentState.Value = newState;
        }

        // アクションが切り替わった際に移動フラグは一度消す
        MoveVector.Value = Vector3.zero;
    }

    #endregion

}
