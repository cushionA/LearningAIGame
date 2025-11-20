using UnityEngine;
using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// DamageSystem
// 
// 概要: キャラクターの被ダメージ処理を管理するシステムクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 敵からの攻撃を受け、自身の防御状態（ガード、ブロッキング、回避など）に基づいて
// 攻撃結果（ヒット、ガード成功、ブロッキング成功、回避成功など）を判定する。
// 判定結果をStateSystemに通知し、キャラクターの状態遷移を促す。
// HitSystemと対になる、1v1戦闘における被攻撃側の処理を担当。
// 
// 入力元クラス:HitSystem (敵の攻撃システム)
// 出力先クラス:StateSystem
// 
// その他:
// DamageSystemBaseを継承
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Systems
{
    public class DamageSystemMock : DamageSystemBase
    {
        #region フィールド

        /// <summary>
        /// Guard,Blocking,Avoidの防御情報を切り替えるためのモック情報
        /// </summary>
        public DefenseInfo _defenseMockInfo;

        /// <summary>
        /// 防御方向を切り替えるためのモック情報
        /// </summary>
        public StanceType _mockStance;

        /// <summary>
        /// 防御情報取得のための参照
        /// </summary>
        [SerializeField]
        private StateSystem _stateSystem;

        /// <summary>
        /// 攻撃を受けた瞬間の自分のアクション状態のスナップショット
        /// ブロッキングやガードのタイミング判定に使用
        /// </summary>
        private ActionState _lastHitAction;

        #endregion

        #region 初期化

        private void Awake()
        {
            if (_stateSystem == null)
            {
                Debug.LogError($"[{nameof(DamageSystem)}] StateSystemが設定されていません！");
            }
        }

        #endregion

        /// <summary>
        /// モックに情報を設定する。
        /// </summary>
        /// <param name="type"></param>
        /// <param name="stance"></param>
        public void MockSetting(DefenseType type, StanceType stance)
        {
            _defenseMockInfo.SetInfo(type, Time.time - 1, 999);
            _mockStance = stance;
        }

        /// <summary>
        /// モックに情報を設定する。
        /// 継続時間まで指定するオーバーロード
        /// </summary>
        /// <param name="type"></param>
        /// <param name="stance"></param>
        public void MockSetting(DefenseType type, StanceType stance, float duration)
        {
            _defenseMockInfo.SetInfo(type, Time.time - 1, duration);
            _mockStance = stance;
        }

        /// <summary>
        /// モックに情報を設定する。
        /// 行動タイプで指定するオーバーロード
        /// </summary>
        /// <param name="type"></param>
        /// <param name="stance"></param>
        public void MockSetting(ActionState actionState, StanceType stance, float duration = 999)
        {
            DefenseType type = actionState switch
            {
                ActionState.ガード => DefenseType.Guard,
                ActionState.ブロッキング => DefenseType.Blocking,
                ActionState.回避 => DefenseType.Avoid,
                _ => DefenseType.None,
            };

            if (type == DefenseType.None)
            {
                Debug.LogWarning($"[{nameof(DamageSystemMock)}] 無効なActionStateが指定されました: {actionState}");
                return;
            }

            _defenseMockInfo.SetInfo(type, Time.time - 1, duration);
            _mockStance = stance;
            Debug.Log($"[{nameof(DamageSystemMock)}] モック設定: 防御タイプ={type}, 防御方向={stance}, 継続時間={duration}");
        }

        #region 被ダメージ処理

        /// <summary>
        /// 敵からの攻撃を受け、攻撃結果を返す
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <returns>攻撃結果（ヒット、ガード、ブロッキング、回避など）</returns>
        public override HitResultType Damage(in AttackInfo attackInfo)
        {
            // 攻撃を受けた瞬間の防御状態をスナップショット
            switch (_defenseMockInfo.CurrentDefense)
            {
                case DefenseType.Guard:
                    _lastHitAction = ActionState.ガード;
                    break;
                case DefenseType.Blocking:
                    _lastHitAction = ActionState.ブロッキング;
                    break;
                case DefenseType.Avoid:
                    _lastHitAction = ActionState.回避;
                    break;
                default:
                    _lastHitAction = _stateSystem.CurrentState.CurrentValue;
                    break;
            }

            Debug.Log($"[{nameof(DamageSystem)}] 攻撃を受けました。 " +
                $"攻撃タイプ: {attackInfo.attackType}, " +
                $"攻撃ダメージ: {attackInfo.damage}, " +
                $"防御状態: {_lastHitAction}, " +
                $"防御方向: {_mockStance}");

            // 攻撃結果を問い合わせる
            return _defenseMockInfo.IsDefenseSuccess(attackInfo, _mockStance);
        }

        /// <summary>
        /// 敵の攻撃の最終結果を受け取り、オブザーバーに通知する
        /// HitSystemから攻撃判定が完全に終了した際に呼ばれる
        /// </summary>
        /// <param name="hitReport">敵の攻撃結果情報</param>
        public override void DamageReport(HitReportInfo hitReport)
        {
            // 被ダメージ情報を設定
            _info.SetInfo(hitReport, _lastHitAction);

            // StateSystemに通知
            NotifyObservers(_info);

            Debug.Log($"[{nameof(DamageSystem)}] 攻撃結果をStateSystemに通知しました。 " +
                $"攻撃結果: {hitReport.hitResultType}, " +
                $"被ダメージ: {hitReport.damage}, " +
                $"防御状態: {_lastHitAction}");
        }

        #endregion

        #region デバッグ用

#if UNITY_EDITOR
        /// <summary>
        /// 現在の被ダメージ状態をデバッグ表示
        /// </summary>
        [ContextMenu("被ダメージ状態を表示")]
        private void DebugPrintState()
        {
            Debug.Log($"[{nameof(DamageSystem)}] " +
                $"最後に受けた攻撃時の状態: {_lastHitAction}, " +
                $"StateSystem参照: {(_stateSystem != null ? "OK" : "NULL")}");
        }
#endif

        #endregion
    }
}