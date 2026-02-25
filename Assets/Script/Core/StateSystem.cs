using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Setting;
using LearningAIGame.CombatSystem.Singleton;
using LearningAIGame.CombatSystem.Systems;
using LearningAIGame.CombatSystem.Utilities;
using LLMDataArchitect;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using R3;
using System;
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
// - CanAttack,CanAvoidAttack, CanChangeGuardDirection, CanBlock, CanAvoid, CanCancelHeavyAttack: 各行動の実行可否
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
    public partial class StateSystem : MonoBehaviour, IGameHelper
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

            // --- 行動指定用フラグ ---
            弱ブロッキング = 1 << 18,// 弱攻撃をブロッキング
            強ブロッキング = 1 << 19,// 強攻撃をブロッキング
            デフォルト攻撃 = 1 << 20,// 通常攻撃
            デフォルト防御 = 1 << 21,// 通常防御

            // --- 複合フラグ（論理和 '|' を使用） ---
            ガード方向切り替え可能 = ガード,
            敵への自動転回可能 = ガード | 回避 | ブロッキング成功 | ガード成功 | 攻撃 | ブロッキング | 強攻撃キャンセル | 小怯み | 大怯み | 弱攻撃ブロッキング | 強攻撃ブロッキング,
            ブロッキング可能 = ガード | ブロッキング成功 | ガード成功,
            回避可能 = ガード | ブロッキング成功 | ガード成功,
            防御可能 = ガード | ブロッキング成功 | ガード成功,
            攻撃可能 = ガード | ブロッキング成功 | ガード成功,
            回避攻撃可能 = 前回避 | 横回避,
            移動可能 = ガード | ブロッキング成功 | ガード成功,
            移動停止 = 小怯み | 大怯み | 死亡 | ブロッキング | 弱攻撃ブロッキング | 強攻撃ブロッキング | 強攻撃キャンセル | ブロッキング成功 | ガード成功,
            弱攻撃系統 = 弱攻撃 | 前回避攻撃 | 横回避攻撃,
            攻撃 = 強攻撃 | 弱攻撃 | 前回避攻撃 | 横回避攻撃,
            回避 = 前回避 | 横回避 | 後ろ回避,
            防御 = 回避 | ガード | ブロッキング,
            強制行動キャンセル = 小怯み | 大怯み | 死亡,
            行動履歴に記録しない = ブロッキング成功 | ガード成功 | 小怯み | 大怯み | 弱攻撃ブロッキング | 強攻撃ブロッキング | 弱攻撃ガード | 死亡,
            スタミナ回復可能 = ガード | ガード成功 | ブロッキング成功
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
        /// アクション設定データ
        /// </summary>
        public ActionSetting actionSetting;

        /// <summary>
        /// キャラの基礎データ
        /// LLMへの報告用
        /// </summary>
        protected CharacterData _characterData;

        /// <summary>
        /// 攻撃の実行情報
        /// コールバックとプロパティを通じてアクセス
        /// </summary>
        protected AttackInfo _attackInfo;

        /// <summary>
        /// 防御の実行情報
        /// コールバックとメソッドを通じてのみアクセス
        /// </summary>
        protected DefenseInfo _defenseInfo;

        /// <summary>
        /// LLMプロンプト生成用に行動や攻撃を記録するための履歴
        /// </summary>
        protected LLMLogData _llmLogData;

        /// <summary>
        /// 行動硬直を記録する変数
        /// ある行動のあと、硬直を経て次に行動できるようになる時間を設定する
        /// </summary>
        protected float _moveStunTime = -1f;

        /// <summary>
        /// 回避攻撃の受付可能時間
        /// </summary>
        protected float _avoidAttackBufferLimit = -1f;

        /// <summary>
        /// 強攻撃キャンセルを受け付けるフレームを記録する変数
        /// </summary>
        protected long _heavyCancelFrame;

        #region 購読対象

        /// <summary>
        /// 攻撃実行クラス
        /// </summary>
        [SerializeField]
        protected AttackSystem _attackSystem;

        /// <summary>
        /// 防御実行クラス
        /// </summary>
        [SerializeField]
        protected DefenseSystem _defenseSystem;

        /// <summary>
        /// 移動実行クラス
        /// </summary>
        [SerializeField]
        protected MovementSystem _movementSystem;

        /// <summary>
        /// 与ダメージ管理クラス
        /// </summary>
        [SerializeField]
        protected HitSystem _hitSystem;

        /// <summary>
        /// 被弾管理クラス
        /// </summary>
        [SerializeField]
        protected DamageSystemBase _damageSystem;

        /// <summary>
        /// 位置管理クラス
        /// </summary>
        [SerializeField]
        protected PositionCache _positionCache;

        /// <summary>
        /// 移動制御クラス
        /// 被弾時などにキャラクターを停止させるために使用
        /// </summary>
        protected MoveController _moveController;

        /// <summary>
        /// 最初のレイヤーを記録する変数
        /// バトル終了後のリセットなどで使用する想定
        /// </summary>
        protected int _initialLayer;

        #endregion

        #endregion

        #region Publicプロパティ

        /// <summary>
        /// HP
        ///</summary>
        public int Hp { get { return (_characterData.Hp >= 0 ? _characterData.Hp : 0); } set { _characterData.Hp = value; } }

        /// <summary>
        /// 読み取り専用の残りエネルギー（枯渇中は常に 0）
        ///</summary>
        public int Energy { get { return (int)(_characterData.IsEnergyExhaust ? 0 : _characterData.Energy); } set { _characterData.Energy = value; } }

        public float EnergyRatio { get { return _characterData.IsEnergyExhaust ? 0 : _characterData.Energy / _characterData.MaxEnergy; } }

        /// <summary>
        /// 読み取り専用の攻撃情報
        /// </summary>
        public AttackInfo CurrentAttackInfo { get { return _attackInfo; } }

        /// <summary>
        /// 読み取り専用の防御情報
        /// </summary>
        public DefenseInfo CurrentDefenseInfo { get { return _defenseInfo; } }

        /// <summary>
        /// 一つ前の行動状態
        /// </summary>
        public ActionState LastState { get; protected set; }

        public Vector3 Position { get { return _positionCache.Position; } }

        /// <summary>
        /// 位置情報を外部に公開する。
        /// </summary>
        public PositionCache PositionCache { get { return _positionCache; } }

        /// <summary>
        /// 攻撃可能な距離を比較用の二乗の値で渡すプロパティ
        /// </summary>
        public float AttackRangePow { get { return actionSetting.AttackableDistancePow; } }

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

        /// <summary>
        /// ヘルス情報
        /// </summary>
        public CharacterData CharacterData => _characterData;

        #endregion

        #region 行動実行可能管理プロパティ

        /// <summary>
        /// 行動ロック状態かどうか
        /// </summary>
        private bool _moveLocked;

        /// <summary>
        /// 攻撃可能かどうか
        /// </summary>
        public bool CanAttack => !_moveLocked && !_characterData.IsEnergyExhaust && Time.time >= _moveStunTime && (CurrentState.CurrentValue & ActionState.攻撃可能) > 0;

        public bool CanAvoidAttack => !_moveLocked && !_characterData.IsEnergyExhaust && ((CurrentState.CurrentValue & ActionState.回避攻撃可能) > 0) && Time.time <= _avoidAttackBufferLimit;

        /// <summary>
        /// ガード方向切り替え可能かどうか
        /// </summary>
        public bool CanChangeGuardDirection => !_moveLocked && (CurrentState.CurrentValue & ActionState.ガード方向切り替え可能) > 0 && Time.time >= _moveStunTime;

        /// <summary>
        /// ブロッキング可能かどうか
        /// </summary>
        public bool CanBlock => !_moveLocked && !_characterData.IsEnergyExhaust && (CurrentState.CurrentValue & ActionState.ブロッキング可能) > 0 && Time.time >= _moveStunTime;

        /// <summary>
        /// 回避可能かどうか
        /// </summary>
        public bool CanAvoid => !_moveLocked && (CurrentState.CurrentValue & ActionState.回避可能) > 0 && Time.time >= _moveStunTime;

        /// <summary>
        /// 強攻撃をキャンセル可能かどうか
        /// </summary>
        public bool CanCancelHeavyAttack => !_moveLocked && !_characterData.IsEnergyExhaust && (CurrentState.CurrentValue & ActionState.強攻撃) > 0 && Time.frameCount <= _heavyCancelFrame && Time.time >= _moveStunTime;

        /// <summary>
        /// 移動可能かどうか
        /// </summary>
        public bool CanMove => !_moveLocked && (CurrentState.CurrentValue & ActionState.移動可能) > 0 && Time.time >= _moveStunTime;

        #endregion

        #endregion

        /// <summary>
        /// 内部状態を更新する（フレーム更新時などに呼ばれる）
        /// </summary>
        public void Update()
        {
            if (_characterData == null)
            {
                return;
            }

            // 防御中のみエネルギー回復
            if ((CurrentState.Value & ActionState.スタミナ回復可能) == 0)
            {
                return;
            }

            // エネルギー切れ中は少し回復速度が速くなる
            else if (_characterData.IsEnergyExhaust)
            {
                // ActionSettingの値を使用
                _characterData.RecoverEnergyByRate(
                    Time.deltaTime * actionSetting.EnergyRecoveryRatePerSecond * actionSetting.EnergyRecoveryEmergencyMultiply
                );
            }
            else
            {
                // ActionSettingの値を使用
                _characterData.RecoverEnergyByRate(
                    Time.deltaTime * actionSetting.EnergyRecoveryRatePerSecond
                );
            }
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
        /// また、行動後硬直を設定する
        /// アニメ管理クラスから各モーション終了時に呼ぶ想定
        /// </summary>
        public void SetNeutral()
        {

            if (CurrentState.CurrentValue != ActionState.ガード)
            {
                _moveStunTime = Time.time + actionSetting[CurrentState.CurrentValue];
                Debug.Log($"[{nameof(StateSystem)}] 行動硬直時間が {_moveStunTime - Time.time} 秒に設定されました。");
            }
            // ニュートラル状態 はガード
            ChangeState(ActionState.ガード);
            // 防御データの設定
            _defenseInfo.SetInfo(DefenseType.Guard, 0, 0);
        }

        /// <summary>
        /// ある攻撃がヒットした際の結果を返すメソッド
        /// ダメージシステムから使用する
        /// </summary>
        /// <param name="attackInfo">攻撃の情報</param>
        /// <returns>ヒット、回避、ガード、の中のどの結果であるかを返す</returns>
        public HitResultType GetAttackResult(in AttackInfo attackInfo)
        {
            // 現在防御中でなければ判定も行わない
            if ((CurrentState.CurrentValue & ActionState.防御) == 0)
            {
                return HitResultType.Hit;
            }

            // ガード中ならつねにガード設定になるように
            if (CurrentState.CurrentValue == ActionState.ガード)
            {
                _defenseInfo.SetInfo(DefenseType.Guard, 0, 0);
            }
            // 防御状態以外であれば防御なしとする
            else if ((CurrentState.CurrentValue & ActionState.防御) == 0)
            {
                _defenseInfo.SetInfo(DefenseType.None, 0, 0);
            }

            return _defenseInfo.IsDefenseSuccess(attackInfo, CurrentStance.CurrentValue);
        }

        /// <summary>
        /// LLMへの出力データ作成用の処理
        /// データが作成されていなければ作成する
        /// ここからまず参照を取る
        /// </summary>
        /// <param name="characterData"></param>
        /// <param name="logData"></param>
        public void CreateLLMSourceData(CharacterData data, LLMLogData log)
        {
            _llmLogData = log;
            _characterData = data;
        }

        #endregion

        #region protectedメソッド

        #region 購読用メソッド

        /// <summary>
        /// ダメージを受けた状況を記録する
        /// </summary>
        protected void OnHit(HitReportInfo hitReport)
        {
            // 攻撃結果がブロッキングとガードならリアクションをする
            // ブロッキングされた場合
            if (hitReport.hitResultType == HitResultType.Block)
            {
                ChangeState(hitReport.attackType == AttackType.WeakAttack ? ActionState.弱攻撃ブロッキング : ActionState.強攻撃ブロッキング);
            }

            // ガードされた場合
            else if (hitReport.hitResultType == HitResultType.Guard)
            {
                ChangeState(ActionState.弱攻撃ガード);
            }

            // 被弾状況を追加
            _llmLogData.AddHitSituationLog(new HitSituation(hitReport));
        }

        /// <summary>
        /// ダメージを受けた状況を記録する
        /// </summary>
        protected void OnDamage(DamageReportInfo damageReport)
        {
            Debug.Log($"[{nameof(StateSystem)}] ダメージ報告を受け取りました。ダメージ量: {damageReport.Damage}, 防御行動: {damageReport.DefenseAction}, 攻撃タイプ: {damageReport.AttackType} 現在時{DateTime.Now}");
            // 被弾している場合
            if (damageReport.Damage != 0)
            {
                // ダメージを受ける処理
                _characterData.TakeDamage(damageReport.Damage);

                // 死んでいたら死亡状態に移行
                if (_characterData.IsDead)
                {
                    Debug.Log($"[{nameof(StateSystem)}] 死亡状態に移行しました。");
                    ChangeState(ActionState.死亡);
                    gameObject.layer = LayerMask.NameToLayer("Interval");// 死亡レイヤーに変更
                    GameManager.Instance.DefeatedReport(this.gameObject);
                    return;
                }

                // 弱攻撃の場合
                if (damageReport.AttackType == AttackType.WeakAttack)
                {
                    Debug.Log($"[{nameof(StateSystem)}] 小怯み状態に移行しました。");
                    ChangeState(ActionState.小怯み);
                }
                else
                {
                    Debug.Log($"[{nameof(StateSystem)}] 大怯み状態に移行しました。");
                    ChangeState(ActionState.大怯み);
                }
            }
            // 防御成功した場合
            else
            {

                // 被ダメージ状況を追加
                _llmLogData.AddDamageSituationLog(new HitSituation(damageReport));

                // 反撃成功時に防御成功イベントが呼ばれないように制御
                if ((CurrentState.CurrentValue & ActionState.防御) == 0)
                {
                    return;
                }

                // ガード成功
                if (damageReport.DefenseAction == ActionState.ガード)
                {
                    Debug.Log($"[{nameof(StateSystem)}] ガード成功状態に移行しました。");
                    ChangeState(ActionState.ガード成功);
                }

                // ブロッキング成功
                else if (damageReport.DefenseAction == ActionState.ブロッキング)
                {
                    Debug.Log($"[{nameof(StateSystem)}] ブロッキング成功状態に移行しました。");
                    ChangeState(ActionState.ブロッキング成功);

                    // ブロッキング成功時エネルギー回復
                    _characterData.RecoverEnergyByRate(actionSetting.BlockingSuccessEnergyRecovery);
                }
            }


        }

        /// <summary>
        /// 防御関連のコールバックで呼ばれるメソッド
        /// 構え方向の変更とブロッキング開始を報告する
        /// ガード切り替え受け付け
        /// </summary>
        protected void OnDefense(DefenseReportInfo defenseReport)
        {
            // 構え方向の変更
            CurrentStance.Value = defenseReport.stance;

            // 行動切り替え
            ChangeState(defenseReport.reportType == DefenseReportType.StanceChange ? ActionState.ガード : ActionState.ブロッキング);

            // 防御データの設定
            _defenseInfo.SetInfo(defenseReport, actionSetting.BlockingStartDelay, actionSetting.BlockingDuration);
        }

        /// <summary>
        /// 攻撃関連のコールバックで呼ばれるメソッド
        /// 弱攻撃と強攻撃の開始、強攻撃キャンセルを報告する
        /// </summary>
        protected void OnAttack(AttackReportInfo attackReport)
        {
            // 切り替える行動状態
            ActionState useState;
            // キャンセル行動であるかどうか
            bool isCancel = CurrentState.CurrentValue == ActionState.ブロッキング成功 ||
                CurrentState.CurrentValue == ActionState.ガード成功;

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

                    // 攻撃方向切り替え
                    CurrentStance.Value = attackReport.stance;
                    break;

                // 強攻撃開始
                case AttackReportType.HeavyAttackStart:
                    // 強攻撃に状態切り替え
                    useState = ActionState.強攻撃;

                    // 強攻撃キャンセルフレームを設定
                    _heavyCancelFrame = Time.frameCount + actionSetting.HeavyCancelInputFrame;

                    // 攻撃方向切り替え
                    CurrentStance.Value = attackReport.stance;
                    break;

                // 強攻撃キャンセル
                case AttackReportType.HeavyAttackCancel:
                    useState = ActionState.強攻撃キャンセル;
                    isCancel = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // 行動切り替え
            ChangeState(useState, isCancel);


            // 攻撃データを設定
            _attackInfo.SetInfo(attackReport);

            if (gameObject.name == "NPC")
            {

                Debug.Log($"[{nameof(StateSystem)}] NPCの攻撃報告を受け取りました。現在時間{DateTime.Now}, 現在の状態{CurrentState.CurrentValue}");
            }

        }

        /// <summary>
        /// 回避関連のコールバックで呼ばれるメソッド
        /// 弱攻撃と強攻撃の開始、強攻撃キャンセルを報告する
        /// </summary>
        protected void OnMovement(MoveReportInfo moveReport)
        {
            switch (moveReport.reportType)
            {
                // 通常移動
                case MovementReportType.NormalMove:
                    MoveVector.Value = moveReport.moveVector;
                    // 移動中はガード
                    ChangeState(ActionState.ガード);
                    break;
                case MovementReportType.FrontStep:
                    ChangeState(ActionState.前回避);
                    _avoidAttackBufferLimit = Time.time + actionSetting.AvoidAttackInputDuration;
                    break;
                case MovementReportType.LeftStep:
                    ChangeState(ActionState.横回避);
                    _avoidAttackBufferLimit = Time.time + actionSetting.AvoidAttackInputDuration;
                    break;
                case MovementReportType.RightStep:
                    ChangeState(ActionState.横回避);
                    _avoidAttackBufferLimit = Time.time + actionSetting.AvoidAttackInputDuration;
                    break;
                case MovementReportType.BackStep:
                    ChangeState(ActionState.後ろ回避);
                    break;
                default:
                    break;
            }

            _defenseInfo.SetInfo(moveReport, actionSetting.AvoidInvincibleStartDelay, actionSetting.AvoidInvincibleDuration);
        }

        #endregion

        //private Animator _animator;

        /// <summary>
        /// 行動切り替えに使用するメソッド
        /// </summary>
        /// <param name="newState">切り替え先の行動</param>
        /// <param name="isCnancel">強攻撃キャンセルなどのキャンセル行動であるか</param>
        protected void ChangeState(ActionState newState, bool isCancel = false)
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
                    _llmLogData.AddActionLog(CurrentState.CurrentValue);
                }

                // 履歴関係なく前回の行動を保存する
                LastState = CurrentState.CurrentValue;

                // 行動切り替え
                CurrentState.Value = newState;

                Debug.Log($"[{nameof(StateSystem)}] 行動状態が {LastState} から {CurrentState.CurrentValue} に切り替わりました。");
            }

            // アクションが切り替わった際に移動フラグは一度消す
            MoveVector.Value = Vector3.zero;

            // アクションがガード以外に切り替わった場合、ガード状態であれば解除する
            if (newState != ActionState.ガード && _defenseInfo.CurrentDefense == DefenseType.Guard)
            {
                // 防御データの設定
                _defenseInfo.SetInfo(DefenseType.None, 0, 0);
            }

            // 移動停止状態の場合は移動を止める
            if ((CurrentState.CurrentValue & ActionState.移動停止) > 0)
            {
                _moveController.Stop();
            }
        }

        #endregion

        #region ライフサイクル

        /// <summary>
        /// 初期化時にフィールド、ReactiveProperty、購読設定を行う
        /// </summary>
        protected void Awake()
        {

            // nullチェック
            if (actionSetting == null)
            {
                Debug.LogError($"[{nameof(StateSystem)}] ActionSettingが設定されていません！");
            }

            // リアクティブプロパティの初期化と破棄登録
            CurrentState = new ReactiveProperty<ActionState>(ActionState.ガード).AddTo(this);
            MoveVector = new ReactiveProperty<Vector3>(Vector3.zero).AddTo(this);
            CurrentStance = new ReactiveProperty<StanceType>(StanceType.Up).AddTo(this);
            _moveController = GetComponent<MoveController>();

            SubscribeSystems();
        }

        /// <summary>
        /// 各システムからの通知を購読する
        /// </summary>
        protected void SubscribeSystems()
        {
            // 各システムの購読設定
            if (_attackSystem != null)
            {
                _attackSystem.Observable.Subscribe(OnAttack).AddTo(this);
            }

            if (_defenseSystem != null)
            {
                _defenseSystem.Observable.Subscribe(OnDefense).AddTo(this);
            }

            if (_movementSystem != null)
            {
                _movementSystem.Observable.Subscribe(OnMovement).AddTo(this);
            }

            if (_hitSystem != null)
            {
                _hitSystem.Observable.Subscribe(OnHit).AddTo(this);
            }

            // 被弾は0.1秒に一回までに制限
            if (_damageSystem != null)
            {
                _damageSystem.Observable
                    //.ThrottleFirst(TimeSpan.FromSeconds(0.3f))
                    .Subscribe(OnDamage)
                    .AddTo(this);
            }

        }

        #endregion

        #region ゲームヘルパー（行動ロック変数の切り替え）

        public void Lock()
        {
            _moveLocked = true;
        }

        public void Unlock()
        {
            _moveLocked = false;
        }

        public void SetUp()
        {

        }

        public void RoundStart()
        {
            // 初期状態はガード。レイヤーも戻す
            ChangeState(ActionState.ガード);
            gameObject.layer = _initialLayer;
        }

        public void RoundEnd()
        {
            _characterData.Reset();
        }

        public void GameEnd()
        {

        }


        #endregion


#if UNITY_EDITOR
        /// <summary>
        /// コンテキストメニューからコンポーネントを設定
        /// </summary>
        [ContextMenu("Setup Components")]
        private void SetupComponents()
        {
            _attackSystem = GetComponent<AttackSystem>();
            _defenseSystem = GetComponent<DefenseSystem>();
            _movementSystem = GetComponent<MovementSystem>();
            _hitSystem = GetComponent<HitSystem>();
            _damageSystem = GetComponent<DamageSystemBase>();
            _positionCache = GetComponent<PositionCache>();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}