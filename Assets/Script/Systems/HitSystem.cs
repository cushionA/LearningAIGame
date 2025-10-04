using UnityEngine;
using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using static LearningAIGame.CombatSystem.Core.StateSystem;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;

//==============================================ファイルヘッダ===========================================================
// HitSystem
// 
// 概要: キャラクターの攻撃ヒット検出を管理するシステムクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 攻撃判定の開始・終了を管理し、敵との衝突を検出してヒット結果を通知する。
// 攻撃判定の持続時間やキャンセル処理も制御する。
// 1v1のゲームに特化した実装で、当たり判定のレイヤーをヒット検出に専用化し、
// 敵の参照も最初から保持している状態で動作する。
// DamageSystemと対になる、1v1戦闘における攻撃側の処理を担当。
// 
// 入力元クラス:BattleCharacterController
// 出力先クラス:StateSystem, DamageSystem (敵)
// 
// その他:
// BaseSystem<HitReportInfo>を継承
// 攻撃IDベースでキャンセル管理を行い、GCアロケーションを最小化
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Systems
{
    public class HitSystem : BaseSystem<HitReportInfo>
    {
        /// <summary>
        /// 攻撃実行情報
        /// </summary>
        private HitReportInfo _info;

        /// <summary>
        /// 実行中の攻撃情報
        /// </summary>
        private AttackInfo _currentAttack;

        /// <summary>
        /// 敵の防御システム
        /// 1v1のゲームなので直接参照を持つ
        /// 当たり判定にヒットしたら攻撃結果判定メソッドを呼ぶ
        /// </summary>
        [SerializeField]
        private DamageSystem _enemyDamageSystem;

        /// <summary>
        /// 自分の攻撃判定のコライダー
        /// </summary>
        [SerializeField]
        private Collider _collider;

        /// <summary>
        /// 攻撃の一意なID（新しい攻撃ごとにインクリメント）
        /// </summary>
        private int _currentAttackId;

        /// <summary>
        /// 現在攻撃判定が有効かどうか
        /// </summary>
        public bool IsAttacking => _collider != null && _collider.enabled;

        #region 初期化・破棄

        private void Awake()
        {
            _currentAttackId = 0;

            if (_collider != null)
            {
                _collider.enabled = false;
            }
            else
            {
                Debug.LogError($"[HitSystem] Colliderが設定されていません！");
            }
        }

        #endregion

        #region 攻撃判定制御

        /// <summary>
        /// 攻撃を開始するメソッド
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <param name="attackDurationFrame">攻撃判定の持続フレーム数</param>
        public void DamageStart(in AttackInfo attackInfo, int attackDurationFrame)
        {
            // 攻撃IDをインクリメント（既存の攻撃を自動的に無効化）
            int attackId = ++_currentAttackId;

            // 当たり判定を有効化
            _collider.enabled = true;

            // 攻撃結果の初期化
            _info.SetResult(HitResultType.Miss);

            // 攻撃情報を保存
            _currentAttack = attackInfo;

            // 判定持続フレーム消化後に当たり判定を消す
            AttackFrameWaitAsync(attackDurationFrame, attackId).Forget();
        }

        /// <summary>
        /// 攻撃判定を強制終了
        /// 判定終了の他、キャンセルや怯みで発生
        /// </summary>
        /// <param name="isSelfStop">自分でキャンセルした場合</param>
        public void DamageStop(bool isSelfStop = false)
        {
            // 攻撃IDをインクリメントして既存の攻撃を無効化
            _currentAttackId++;

            // 当たり判定を無効化
            _collider.enabled = false;

            // ヒット結果を中断にして報告
            _info.SetResult(isSelfStop ? HitResultType.Cancel : HitResultType.Stun);
            AttackResultReport();
        }

        /// <summary>
        /// 攻撃の持続フレームを待ち、当たり判定を消失させる
        /// </summary>
        /// <param name="waitFrame">待機フレーム数</param>
        /// <param name="attackId">この攻撃の一意なID</param>
        private async UniTaskVoid AttackFrameWaitAsync(int waitFrame, int attackId)
        {

            // フレーム数で正確に待機
            bool isCancel = await UniTask.DelayFrame(waitFrame, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();

            // この攻撃がまだ有効かチェック（新しい攻撃が始まっていないか）
            // キャンセルもチェック
            if (attackId != _currentAttackId || isCancel)
            {
                return; // 既に別の攻撃が始まっているので何もしない
            }

            // 当たり判定を消して通知
            _collider.enabled = false;

            AttackResultReport();
        }

        #endregion

        #region 衝突判定

        /// <summary>
        /// 攻撃ヒット時の処理
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            AttackHit();
        }

        /// <summary>
        /// 攻撃ヒット時の処理
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            AttackHit();
        }

        /// <summary>
        /// ヒット時の処理
        /// </summary>
        private void AttackHit()
        {
            // すでにヒット済み（Miss/Avoid以外）なら処理しない
            if (!IsFirstHit())
            {
                return;
            }

            // 敵のダメージシステムに攻撃を伝え、結果を取得
            HitResultType result = _enemyDamageSystem.Damage(_currentAttack);

            // ヒット結果を更新
            _info.SetResult(result);

            // 結果に応じた処理
            ProcessHitResult(result);
        }

        /// <summary>
        /// 初回ヒット判定（まだヒットしていないか）
        /// </summary>
        private bool IsFirstHit()
        {
            return _info.hitResultType == HitResultType.Miss ||
                   _info.hitResultType == HitResultType.Avoid;
        }

        /// <summary>
        /// ヒット結果に応じた処理
        /// </summary>
        private void ProcessHitResult(HitResultType result)
        {
            switch (result)
            {
                case HitResultType.Block:
                case HitResultType.Guard:
                    // 防御成功。判定を即座に終了
                    _collider.enabled = false;
                    AttackResultReport();
                    break;

                case HitResultType.Hit:
                    // ヒット成功。判定を終了（通知は持続フレーム終了時にAttackFrameWaitAsyncで）
                    _collider.enabled = false;
                    break;
            }
        }

        #endregion

        /// <summary>
        /// 自分の攻撃結果を自分のStateSystemと敵のDamageSystemに通知する
        /// </summary>
        private void AttackResultReport()
        {
            NotifyObservers(_info);

            // 空振り以外なら敵にも通知する
            if (_info.hitResultType != HitResultType.Miss)
            {
                _enemyDamageSystem.DamageReport(_info);
            }
        }

        #region デバッグ用

#if UNITY_EDITOR
        [ContextMenu("攻撃判定の状態を表示")]
        private void DebugPrintState()
        {
            Debug.Log($"[HitSystem] 攻撃中: {IsAttacking}, 攻撃ID: {_currentAttackId}, 結果: {_info.hitResultType}");
        }
#endif

        #endregion
    }
}