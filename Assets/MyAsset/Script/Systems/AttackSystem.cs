
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UniRx;
using UnityEngine;

//=====================================================================================================================
// LearningAIGame
// 
// 概要: 攻撃システムの実装クラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 攻撃実行メソッドの呼び出しをトリガーに起動
// 
// 入力元クラス:BattleCharacterController
// 出力先クラス:StateSystem
// 
// その他:
// 特記事項や注意点があれば記述
/// 回避攻撃は回避中の攻撃方向変えられないタイミングで
/// このゲーム後ろ回避が強すぎない？　近接択全拒否できるじゃん
/// やっぱ銃器ごとに強射撃実装するか。弾倉全弾撃ち尽くすけど超強力な奴
/// というよりフルバーストって名前で全弾撃つか
/// ガトリングの場合、三発ずつ撃てるようになる（威力は二倍くらい）
/// 弾持ちが悪くなるけどDPSが跳ね上がる
/// 銃はブロッキング不可で
/// 後ろ回避は他ムーブで硬直キャンセル不可
//=====================================================================================================================
namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// StateSystemへの報告用攻撃データの構造体
    /// 
    /// </summary>
    [System.Serializable]
    public struct AttackData
    {
        /// <summary>
        /// 攻撃のタイプ
        /// </summary>
        public AttackType attackType;
        public float damage;
        public AttackDirection direction;
        public ActionState lastState;
        public int comboCount;
    }

    /// <summary>
    /// 実行した攻撃の結果を記録するためのデータ
    /// モーション終了、中断時にStateSystemに報告する
    /// </summary>
    public enum AttackResult : Byte
    {
        キャンセル,
        ヒット,
        ガード,
        ブロッキング,
        回避
    }


    /// <summary>
    /// 攻撃システム - 近接攻撃、射撃攻撃、スキル攻撃、コンボシステムを管理
    /// 
    /// 武器の仕様
    /// 基本武器を一つ選ぶ。近接武器。弱強を出せる
    /// 射撃武器を一つ選ぶ。Lで射撃。
    /// スキルを一つ選ぶ。遠距離や近距離がある。ミサイルとか水平に薙ぎ払う防禦できないレーザーとか、特殊モーション突進とか、出が早い斬撃とか（キャンセル後に使う）
    /// エクステンションを選ぶ。パリィや罠設置
    /// ShootSystemを導入しないとね
    /// 
    /// マニューバオミットすればパリィとエクステンション分けれる
    /// エクステンションオミットしてマニューバもあり
    /// マニューバはいったん削る
    /// 
    /// ここで攻撃データは持たない。
    /// StateSystemで管理している攻撃状態に基づき、コントローターのステータス～攻撃データを取って使う
    /// 攻撃アニメが終了・中断した瞬間にStateSystemに報告して、コンボ状態とかを調整しよう
    /// このクラスの責任範囲は以下
    /// ・アニメを再生
    /// ・攻撃判定の発生
    /// ・StateSystemへの報告
    /// ・攻撃結果の確認
    /// ・フェイントの受付
    /// </summary>
    public class AttackSystem : BaseSystem<AttackData>
    {
        // 攻撃状態
        private AttackData _currentAttackData;

        [Header("攻撃判定")]
        [Tooltip("近接攻撃の判定コライダー")]
        [SerializeField] private AttackCollider _meleeAttackCollider;

        protected override void OnInitialized()
        {

            InitializeAttackCollider();

            // 初期データの設定
            UpdateAttackData();
        }

        private void UpdateAndNotifyAttackData()
        {
            UpdateAttackData();
            NotifyObservers(_currentAttackData);
        }

        private void UpdateAttackData()
        {
            //currentAttackData = new AttackData
            //{
            //    attackType = GetCurrentAttackType(),
            //    damage = GetCurrentDamage(),
            //    direction = currentAttackDirection,
            //    isExecuting = IsAttacking,
            //    isAiming = isAiming,
            //    aimingAccuracy = CurrentAimingAccuracy,
            //    comboCount = CurrentComboCount,
            //    isAerialAttack = IsAerialCombo,
            //    isDodgeAttack = canDodgeAttack
            //};
        }

        #region Public Attack Methods

        /// <summary>
        /// 弱攻撃を実行
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteWeakAttack(AttackDirection direction)
        {

        }

        /// <summary>
        /// 強攻撃を実行
        /// </summary>
        /// <param name="direction">攻撃方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteStrongAttack(AttackDirection direction)
        {

        }

        /// <summary>
        /// スキル攻撃を実行
        /// </summary>
        /// <param name="skillIndex">スキルインデックス</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteSkill(int skillIndex)
        {

        }

        /// <summary>
        /// 攻撃キャンセル
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CancelAttack()
        {

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 攻撃を開始
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <param name="startupTime">発生時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StartAttack(AttackData attackInfo, float startupTime)
        {


            // 攻撃終了時間
            UniRx.Observable.Timer(TimeSpan.FromSeconds(startupTime + 0.2f)).Subscribe(_ => StopAttack()).AddTo(disposables);
        }

        /// <summary>
        /// 攻撃を停止
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void StopAttack()
        {
            //IsAttacking = false;
            //stateSystem.ReportActionStateChange(ActionState.Idle);

            if (_meleeAttackCollider != null)
            {
                _meleeAttackCollider.DeactivateAttack();
            }
        }

        /// <summary>
        /// 敵への方向を取得
        /// </summary>
        /// <returns>敵への方向ベクトル</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 GetDirectionToEnemy()
        {
            return transform.forward;
        }

        #region Initialization Methods

        /// <summary>
        /// 攻撃コライダーの初期化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeAttackCollider()
        {
            if (_meleeAttackCollider == null)
            {
                // 攻撃コライダーを動的作成
                var colliderObject = new GameObject("MeleeAttackCollider");
                colliderObject.transform.SetParent(transform);
                colliderObject.transform.localPosition = Vector3.forward;

                var collider = colliderObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 1.5f;

                _meleeAttackCollider = colliderObject.AddComponent<AttackCollider>();
            }
        }

        #endregion
    }

    /// <summary>
    /// 攻撃判定コライダー
    /// </summary>
    public class AttackCollider : MonoBehaviour
    {
        private AttackData _currentAttackData;
        private BattleCharacterController _attackerController;
        private bool _isActive = false;

        /// <summary>
        /// 攻撃を有効化
        /// </summary>
        /// <param name="attackInfo">攻撃情報</param>
        /// <param name="attacker">攻撃者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ActivateAttack(AttackData attackInfo, BattleCharacterController attacker)
        {
            _currentAttackData = attackInfo;
            _attackerController = attacker;
            _isActive = true;
        }

        /// <summary>
        /// 攻撃を無効化
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DeactivateAttack()
        {
            _isActive = false;
            _attackerController = null;
        }

        /// <summary>
        /// トリガー判定
        /// </summary>
        /// <param name="other">衝突オブジェクト</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnTriggerEnter(Collider other)
        {
            if (!_isActive || _attackerController == null)
                return;

            //var defender = other.GetComponent<BattleCharacterController>();
            //if (defender != null && defender != _attackerController)
            //{
            //    var result = CombatUtilities.CalculateHit(_currentAttackData, defender);
            //    defender.ReceiveAttack(result);
            //    _attackerController.OnAttackResult(result);
            //}
        }
    }

    /// <summary>
    /// 弾丸コントローラー（簡易版）
    /// </summary>
    public class BulletController : MonoBehaviour
    {
        private AttackData _attackInfo;
        private BattleCharacterController _attackerController;
        private float _accuracy;
        private float _speed = 20f;
        private float _lifeTime = 5f;

        /// <summary>
        /// 弾丸を初期化
        /// </summary>
        /// <param name="info">攻撃情報</param>
        /// <param name="attacker">攻撃者</param>
        /// <param name="aimAccuracy">射撃精度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(AttackData info, BattleCharacterController attacker, float aimAccuracy)
        {
            _attackInfo = info;
            _attackerController = attacker;
            _accuracy = aimAccuracy;

            // 一定時間後に自動削除
            Destroy(gameObject, _lifeTime);
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            transform.Translate(Vector3.forward * _speed * Time.deltaTime);
        }

        /// <summary>
        /// トリガー判定
        /// </summary>
        /// <param name="other">衝突オブジェクト</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnTriggerEnter(Collider other)
        {
            var defender = other.GetComponent<BattleCharacterController>();
            if (defender != null && defender != _attackerController)
            {
                ////var result = CombatUtilities.CalculateHit(_attackInfo, defender);
                //defender.ReceiveAttack(result);
                //_attackerController.OnAttackResult(result);

                //Destroy(gameObject);
            }
        }

        #endregion private methods
    }
}