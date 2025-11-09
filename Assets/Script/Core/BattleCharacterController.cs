using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Setting;
using LearningAIGame.CombatSystem.Systems;
using R3;
using System;
using System.Threading.Tasks;
using Unity.Burst;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// StateSystemDebug
// 
// 概要: StateSystemのデバッグ情報表示機能を提供するpartialクラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// StateSystemのデバッグ情報をGUI表示およびログ出力する。
// リアルタイムで状態遷移、エネルギー、行動可能フラグ、硬直時間などを可視化。
// 回避攻撃の実行可否判定と詳細なバッファ受付状況を表示する。
// エディタ上でのデバッグ作業を効率化するための開発支援ツール。
// 
// 入力元クラス: StateSystem(自クラス)
// 出力先クラス: なし(デバッグ表示専用)
// 
// その他:
// UNITY_EDITORディレクティブによりエディタ環境でのみ動作
// ContextMenuによる手動デバッグ機能も提供
//=====================================================================================================================
namespace LearningAIGame.CombatSystem.Core
{
    /// <summary>
    /// バトルキャラクターコントローラー
    /// 責任範囲：入力受付、入力処理、移動処理
    /// </summary>
    public class BattleCharacterController : MonoBehaviour
    {
        /// <summary>
        /// アクション設定データ
        /// </summary>
        [SerializeField]
        private ActionSetting _actionSetting;

        /// <summary>
        /// 攻撃実行クラス
        /// </summary>
        [SerializeField]
        private AttackSystem _attackSystem;

        /// <summary>
        /// 防御実行クラス
        /// </summary>
        [SerializeField]
        private DefenseSystem _defenseSystem;

        /// <summary>
        /// 移動実行クラス
        /// </summary>
        [SerializeField]
        private MovementSystem _movementSystem;

        /// <summary>
        /// ダメージ判定の管理クラス
        /// </summary>
        [SerializeField]
        private HitSystem _hitSystem;

        /// <summary>
        /// 状態管理システム
        /// </summary>
        [SerializeField]
        private StateSystem _stateSystem;

        /// <summary>
        /// 敵のTransform
        /// </summary>
        [SerializeField]
        private Transform _enemyTransform;

        /// <summary>
        /// 回転速度（度/秒）
        /// </summary>
        [SerializeField]
        private float _rotationSpeed = 360f;

        /// <summary>
        /// 回転を停止する角度の閾値（度）
        /// </summary>
        [SerializeField]
        private float _rotationThreshold = 1f;

        /// <summary>
        /// 自分の位置のキャッシュ
        /// </summary>
        [SerializeField]
        private Vector3 _myPosition;

        /// <summary>
        /// 敵の位置のキャッシュ
        /// </summary>
        private Vector3 _enemyPosition;

        private void Start()
        {
            // 被ダメージによる行動キャンセルイベント
            _stateSystem.CurrentState
                .Where(state => (state & ActionState.強制行動キャンセル) > 0)
                .Subscribe(_ => OnStunCancel())
                .AddTo(this);
        }

        private void Update()
        {
            // 自分の位置を毎フレームキャッシュ
            _myPosition = transform.position;

            // 敵の位置を毎フレームキャッシュ
            _enemyPosition = _enemyTransform.position;

            // 敵方向に回転して向き直る。
            RotateTowardsEnemy();
        }

        /// <summary>
        /// 弱攻撃実行
        /// </summary>
        public async UniTaskVoid LightAttackAct(StanceType stance)
        {

            // デバッグログ
            Debug.Log($"LightAttackAct called - CanAttack: {_stateSystem.CanAttack}, CanAvoidAttack: {_stateSystem.CanAvoidAttack}");

            // 攻撃可能な状態かを確認
            if (!_stateSystem.CanAttack && !_stateSystem.CanAvoidAttack)
            {
                return;
            }

            // 指定がなければ現在の構え方向を使う
            stance = stance == StanceType.None ? _stateSystem.CurrentStance.CurrentValue : stance;

            // コストエネルギーを消費
            int cost = _actionSetting.WeakAttackEnergyCost;
            _stateSystem.UseEnergy(cost);

            // 攻撃パラメータの取得
            int damage = _actionSetting.WeakAttackDamage;
            Vector3 stepVector = GetNormalToEnemy() * _actionSetting.WeakAttackStepSpeed;
            float stepDuration = _actionSetting.WeakAttackStepDuration;

            // 攻撃実行
            _attackSystem.WeakAttack(damage, stance, stepVector, stepDuration, _actionSetting.WeakAttackStartFrame);

            // 攻撃判定発生フレームまで待機
            bool isCancel = await UniTask.DelayFrame(_actionSetting.WeakAttackStartFrame, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();

            // 攻撃継続中であれば判定を出す
            if (!isCancel && (_stateSystem.CurrentState.CurrentValue & ActionState.弱攻撃系統) > 0)
            {
                _hitSystem.DamageStart(_stateSystem.CurrentAttackInfo, _actionSetting.WeakAttackDurationFrame);
            }
        }

        /// <summary>
        /// 強攻撃実行
        /// 攻撃判定発生フレーム消化後に判定を発生させる
        /// </summary>
        public async UniTaskVoid HeavyAttackAct(StanceType stance)
        {
            // 攻撃可能な状態かを確認
            if (!_stateSystem.CanAttack)
            {
                return;
            }

            // 指定がなければ現在の構え方向を使う
            stance = stance == StanceType.None ? _stateSystem.CurrentStance.CurrentValue : stance;

            // コストエネルギーを消費
            int cost = _actionSetting.HeavyAttackEnergyCost;
            _stateSystem.UseEnergy(cost);

            // 攻撃パラメータの取得
            int damage = _actionSetting.HeavyAttackDamage;
            Vector3 stepVector = GetNormalToEnemy() * _actionSetting.HeavyAttackStepSpeed;
            float stepDuration = _actionSetting.HeavyAttackStepDuration;

            // 攻撃実行
            _attackSystem.HeavyAttack(damage, stance, stepVector, stepDuration, _actionSetting.HeavyAttackStartFrame);

            // 攻撃判定発生フレームまで待機
            bool isCancel = await UniTask.DelayFrame(_actionSetting.HeavyAttackStartFrame, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();

            // 攻撃継続中であれば判定を出す
            if (!isCancel && _stateSystem.CurrentState.CurrentValue == ActionState.強攻撃)
            {
                _hitSystem.DamageStart(_stateSystem.CurrentAttackInfo, _actionSetting.HeavyAttackDurationFrame);
            }
        }

        /// <summary>
        /// 強攻撃キャンセル
        /// </summary>
        public void HeavyAttackCancel()
        {
            // 強攻撃実行中、かつキャンセル可能状態かを確認
            if (_stateSystem.CurrentState.CurrentValue != ActionState.強攻撃 || !_stateSystem.CanCancelHeavyAttack)
            {
                return;
            }

            // キャンセルコストを消費
            int cost = _actionSetting.HeavyAttackCancelEnergyCost;
            _stateSystem.UseEnergy(cost);

            // キャンセル実行
            _attackSystem.HeavyAttackCancel();

            // 攻撃判定の終了を行う
            _hitSystem.DamageStop(true);
        }

        /// <summary>
        /// 強攻撃フェイント（AI用）
        /// </summary>
        public async UniTaskVoid HeavyAttackFeint(StanceType stance)
        {
            // 強攻撃を開始
            HeavyAttackAct(stance).Forget();

            // 1秒待機
            bool IsCancel = await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: destroyCancellationToken).SuppressCancellationThrow();

            // 正常に待機が終わればキャンセル実行
            if (!IsCancel)
            {
                HeavyAttackCancel();
            }
        }

        /// <summary>
        /// ガード方向変更
        /// </summary>
        /// <param name="guardDirection">ガード方向ベクトル</param>
        public void GuardDirectionChange(StanceType stance)
        {
            // ガード方向変更可能な状態か、指定が有効かを確認
            if (!_stateSystem.CanChangeGuardDirection || stance == StanceType.None)
            {
                return;
            }

            // ガード方向変更実行
            _defenseSystem.GuardStanceChange(stance);
        }

        /// <summary>
        /// ブロッキング実行
        /// </summary>
        public void BlockingAct(StanceType stance)
        {
            // ブロッキング可能な状態かを確認
            if (!_stateSystem.CanBlock)
            {
                return;
            }

            // 指定がなければ現在の構え方向を使う
            stance = stance == StanceType.None ? _stateSystem.CurrentStance.CurrentValue : stance;

            // コストエネルギーを消費
            int cost = _actionSetting.BlockingEnergyCost;
            _stateSystem.UseEnergy(cost);

            // ブロッキング実行
            _defenseSystem.BlockingStart(stance);
        }

        /// <summary>
        /// AI用の弱攻撃ブロッキング実行
        /// </summary>
        public void LightBlocking(StanceType stance)
        {
            BlockingReleaseAfterDelay(_actionSetting.WeakAttackStartFrame - 2, stance).Forget();
        }

        /// <summary>
        /// AI用の強攻撃ブロッキング実行
        /// </summary>
        public void HeavyBlocking(StanceType stance)
        {
            BlockingReleaseAfterDelay(_actionSetting.HeavyAttackStartFrame - 2, stance).Forget();
        }

        /// <summary>
        /// 指定フレームだけ遅らせてからブロッキング実行
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="stance"></param>
        /// <returns></returns>
        private async UniTaskVoid BlockingReleaseAfterDelay(int delay, StanceType stance)
        {
            // 指定時間待機
            bool isCancel = await UniTask.DelayFrame(delay, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();

            // 正常に待機が終わればブロッキング実行
            if (!isCancel)
            {
                BlockingAct(stance);
            }
        }

        /// <summary>
        /// 回避実行
        /// </summary>
        /// <param name="moveVector">移動方向ベクトル</param>
        public void AvoidAct(MovementReportType type)
        {
            // 回避可能な状態かを確認
            if (!_stateSystem.CanAvoid)
            {
                return;
            }

            // エネルギーが0なら回避性能が落ちる
            if (_stateSystem.Energy == 0)
            {
                // 回避実行
                _movementSystem.Avoid(type, _actionSetting.AvoidSpeed * 0.7f, _actionSetting.AvoidDuration * 0.7f);
            }
            else
            {
                // 回避実行
                _movementSystem.Avoid(type, _actionSetting.AvoidSpeed, _actionSetting.AvoidDuration);

                // コスト消費
                _stateSystem.UseEnergy(_actionSetting.AvoidEnergyCost);
            }
        }

        /// <summary>
        /// 回避攻撃（AI用）
        /// </summary>
        /// <param name="moveVector">移動方向ベクトル</param>
        public async UniTaskVoid AvoidAttackAct(MovementReportType type)
        {
            // 回避可能な状態かを確認
            if (!_stateSystem.CanAvoid && type == MovementReportType.BackStep)
            {
                return;
            }

            // コスト消費
            _stateSystem.UseEnergy(_actionSetting.AvoidEnergyCost);

            // 回避実行
            _movementSystem.Avoid(type, _actionSetting.AvoidSpeed, _actionSetting.AvoidDuration);

            // 回避攻撃の実行猶予を入れる
            float waitTime = _actionSetting.AvoidAttackInputDuration * 0.7f;

            // 実行猶予分待機
            bool isCancel = await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: destroyCancellationToken).SuppressCancellationThrow();

            // 正常に待機が終わればキャンセル実行
            if (!isCancel)
            {
                HeavyAttackCancel();
            }
        }

        /// <summary>
        /// 移動実行
        /// </summary>
        /// <param name="moveVector">移動方向ベクトル</param>
        public void MoveAct(Vector3 moveVector)
        {
            // 移動可能な状態かを確認
            if (!_stateSystem.CanMove)
            {
                return;
            }

            // 移動開始
            // 敵基準の相対座標系に変換してから移動
            _movementSystem.Move(moveVector * _actionSetting.MoveSpeed);
        }

        /// <summary>
        /// 外部から敵のTransformを設定する
        /// </summary>
        /// <param name="enemyTransform"></param>
        public void SetTargetTransform(Transform enemyTransform)
        {
            _enemyTransform = enemyTransform;
        }

        #region 購読用メソッド

        /// <summary>
        /// 怯み等で行動キャンセルされた場合にコールバックされる
        /// 実行中だった行動の影響をキャンセルする
        /// </summary>
        private void OnStunCancel()
        {
            // 強制キャンセル時の行動を確認。
            ActionState last = _stateSystem.LastState;

            if ((last & ActionState.攻撃) > 0)
            {
                _hitSystem.DamageStop();
            }
        }

        #endregion

        /// <summary>
        /// 敵への法線ベクトルを取得
        /// </summary>
        /// <returns>正規化された敵への方向ベクトル</returns>
        [BurstCompile]
        private Vector3 GetNormalToEnemy()
        {
            if (_enemyTransform == null)
            {
                return transform.forward;
            }

            Vector3 direction = (_enemyPosition - _myPosition);
            direction.y = 0f; // Y軸は無視
            return direction.normalized;
        }

        /// <summary>
        /// 敵の方向に向き直る処理
        /// </summary>
        private void RotateTowardsEnemy()
        {
            if (_enemyTransform == null)
                return;

            // 敵への方向ベクトル（Y軸無視）
            Vector3 direction = _enemyPosition - _myPosition;
            direction.y = 0f;

            // ゼロベクトルチェック
            if (direction.sqrMagnitude < 0.001f)
                return;

            // 目標角度
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.y;

            // 角度差（-180～180）
            float delta = Mathf.DeltaAngle(currentAngle, targetAngle);

            // 閾値チェック
            if (Mathf.Abs(delta) < _rotationThreshold)
                return;

            // 回転（最大速度制限付き）
            float rotation = Mathf.Clamp(delta, -_rotationSpeed * Time.deltaTime, _rotationSpeed * Time.deltaTime);
            transform.Rotate(0f, rotation, 0f, Space.Self);
        }

    }
}