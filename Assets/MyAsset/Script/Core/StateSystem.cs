using LearningAIGame.CombatSystem.Data;
using LLMDataArchitect;
using LLMDataArchitectTest;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using R3;
using System;
using System.Collections.Generic;
using UnityEngine;

//==============================================ファイルヘッダ=======================================================================
// StateSystem
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
// [購読用メソッドとイベント構造体の対応]
// 1. 攻撃用: OnAttack(AttackReportInfo)
//    - stance: 攻撃方向（上/左/右）
//    - damage: ダメージ値
//    - reportType: WeakAttackStart(弱攻撃開始) / HeavyAttackStart(強攻撃開始) / HeavyAttackCancel(強攻撃キャンセル)
//
// 2. 防御用: OnDefense(DefenseReportInfo)
//    - stance: 防御方向（上/左/右）
//    - reportType: StanceChange(ガード方向変更) / BlockingStart(ブロッキング開始)
//
// 3. 移動アクション用: OnMovement(MoveReportInfo)
//    - moveVector: 移動ベクトル
//    - reportType: NormalMove(通常移動) / FrontStep(前回避) / LeftStep(左回避) / RightStep(右回避) / BackStep(後ろ回避)
//
// 4. 被ダメージ用: OnDamage(DamageReportInfo)
//    - damage: 受けたダメージ（0以外なら防御失敗）
//    - defenseType: Guard(ガード) / Blocking(ブロッキング) / Avoid(回避) / None(防御不能)
//    - attackType: WeakAttack(弱攻撃) / HeavyAttack(強攻撃)
//
// 5. 与ダメージ用: OnHit(HitReportInfo)
//    - damage: 与えたダメージ（0なら攻撃失敗）
//    - attackType: WeakAttack(弱攻撃) / HeavyAttack(強攻撃)
//    - hitResultType: Block(ブロッキングされた) / Guard(ガードされた) / Avoid(回避された) / Hit(命中) / Miss(空振り)
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

namespace LearningAIGame.CombatSystem.Core
{
    public class StateSystem : MonoBehaviour
    {

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
        private void OnDamage(DamageReportInfo damageReport)
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
}