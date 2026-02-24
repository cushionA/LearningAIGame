using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Setting;
using LearningAIGame.CombatSystem.Systems;
using R3;
using System;
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
    public class BattleCharacterController : MonoBehaviour, ITargetSet
    {
        /// <summary>
        /// アクション設定データ
        /// </summary>
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
        /// 物理演算用Rigidbody
        /// </summary>
        [SerializeField]
        private Rigidbody _rb;

        /// <summary>
        /// 敵のTransform
        /// </summary>
        [SerializeField]
        private Transform _enemyTransform;

        /// <summary>
        /// 敵との最小距離
        /// これ以上は近づかない
        /// </summary>
        [SerializeField]
        private float _minDistanceToEnemy = 1.5f;

        /// <summary>
        /// 自分の位置のキャッシュ
        /// </summary>
        private PositionCache _myPosition;

        /// <summary>
        /// 敵の位置のキャッシュ
        /// </summary>
        private PositionCache _enemyPosition;

        /// <summary>
        /// これが真の間は敵の方向を向く
        /// </summary>
        private bool _lookEnemyEnabled = true;

        private void Update()
        {
            LookAtEnemy();

            // 敵に向き直れる状態であればフラグを立てる
            // フラグを折ることはしない
            if ((_stateSystem.CurrentState.CurrentValue & ActionState.敵への自動転回可能) > 0)
            {
                _lookEnemyEnabled = true;
            }
        }

        private void FixedUpdate()
        {
            //   PushAwayFromEnemy();
        }

        /// <summary>
        /// 即座に敵の方向を向く
        /// </summary>
        private void LookAtEnemy()
        {
            if (_enemyTransform == null)
                return;

            // 敵に向き直れない状態であれば何もしない
            if (!_lookEnemyEnabled)
            {
                return;
            }

            Vector3 direction = _enemyPosition.Position - _myPosition.Position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>
        /// 敵と近すぎる場合に押し出す（Rigidbody経由）
        /// 壁があれば自然に止まる
        /// </summary>
        private void PushAwayFromEnemy()
        {
            if (_enemyTransform == null || _rb == null)
                return;

            Vector3 toEnemy = _enemyPosition.Position - _myPosition.Position;
            toEnemy.y = 0f;

            float distanceSqr = toEnemy.sqrMagnitude;
            float minDistanceSqr = _minDistanceToEnemy * _minDistanceToEnemy;

            if (distanceSqr < minDistanceSqr && distanceSqr > 0.001f)
            {
                float distance = Mathf.Sqrt(distanceSqr);
                float pushDistance = _minDistanceToEnemy - distance;
                Vector3 pushDirection = -toEnemy / distance;

                // Rigidbody経由で押し出す（壁があれば自然に止まる）
                _rb.MovePosition(_myPosition.Position + pushDirection * pushDistance);
            }
        }

        /// <summary>
        /// 弱攻撃実行
        /// </summary>
        public async UniTaskVoid LightAttackAct(StanceType stance)
        {
            Debug.Log($"LightAttackAct called - CanAttack: {_stateSystem.CanAttack}, CanAvoidAttack: {_stateSystem.CanAvoidAttack}");

            if (!_stateSystem.CanAttack && !_stateSystem.CanAvoidAttack)
            {
                return;
            }

            stance = stance == StanceType.None ? _stateSystem.CurrentStance.CurrentValue : stance;

            int cost = _actionSetting.WeakAttackEnergyCost;
            _stateSystem.UseEnergy(cost);

            int damage = _actionSetting.WeakAttackDamage;
            Vector3 stepVector = Vector3.forward * _actionSetting.WeakAttackStepSpeed;
            float stepDuration = _actionSetting.WeakAttackStepDuration;

            _attackSystem.WeakAttack(damage, stance, stepVector, stepDuration, _actionSetting.WeakAttackStartFrame);

            bool isCancel = await UniTask.DelayFrame(_actionSetting.WeakAttackStartFrame, PlayerLoopTiming.FixedUpdate, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
            _lookEnemyEnabled = false;// 攻撃判定中は敵の方向を向かない
            if (!isCancel && (_stateSystem.CurrentState.CurrentValue & ActionState.弱攻撃系統) > 0)
            {
                _hitSystem.DamageStart(_stateSystem.CurrentAttackInfo, _actionSetting.WeakAttackDurationFrame);
            }
        }

        /// <summary>
        /// 強攻撃実行
        /// </summary>
        public async UniTaskVoid HeavyAttackAct(StanceType stance)
        {

            if (!_stateSystem.CanAttack)
            {
                return;
            }

            stance = stance == StanceType.None ? _stateSystem.CurrentStance.CurrentValue : stance;

            int cost = _actionSetting.HeavyAttackEnergyCost;
            _stateSystem.UseEnergy(cost);

            int damage = _actionSetting.HeavyAttackDamage;
            Vector3 stepVector = Vector3.forward * _actionSetting.HeavyAttackStepSpeed;
            float stepDuration = _actionSetting.HeavyAttackStepDuration;

            _attackSystem.HeavyAttack(damage, stance, stepVector, stepDuration, _actionSetting.HeavyAttackStartFrame);

            bool isCancel = await UniTask.DelayFrame(_actionSetting.HeavyAttackStartFrame, PlayerLoopTiming.FixedUpdate, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
            _lookEnemyEnabled = false;// 攻撃判定中は敵の方向を向かない

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
            if (_stateSystem.CurrentState.CurrentValue != ActionState.強攻撃 || !_stateSystem.CanCancelHeavyAttack)
            {
                return;
            }

            int cost = _actionSetting.HeavyAttackCancelEnergyCost;
            _stateSystem.UseEnergy(cost);

            _attackSystem.HeavyAttackCancel();
            _hitSystem.DamageStop(true);
        }

        /// <summary>
        /// 強攻撃フェイント（AI用）
        /// </summary>
        public async UniTaskVoid HeavyAttackFeint(StanceType stance)
        {
            HeavyAttackAct(stance).Forget();

            bool IsCancel = await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: destroyCancellationToken).SuppressCancellationThrow();

            if (!IsCancel)
            {
                HeavyAttackCancel();
            }
        }

        /// <summary>
        /// ガード方向変更
        /// </summary>
        public void GuardDirectionChange(StanceType stance)
        {
            if (!_stateSystem.CanChangeGuardDirection || stance == StanceType.None)
            {
                return;
            }

            _defenseSystem.GuardStanceChange(stance);
        }

        /// <summary>
        /// ブロッキング実行
        /// </summary>
        public void BlockingAct(StanceType stance)
        {
            if (!_stateSystem.CanBlock)
            {
                return;
            }

            stance = stance == StanceType.None ? _stateSystem.CurrentStance.CurrentValue : stance;

            int cost = _actionSetting.BlockingEnergyCost;
            _stateSystem.UseEnergy(cost);

            _defenseSystem.BlockingStart(stance);
        }

        /// <summary>
        /// AI用の弱攻撃ブロッキング実行
        /// </summary>
        public void LightBlocking(StanceType stance)
        {
            BlockingReleaseAfterDelay(Math.Max(_actionSetting.WeakAttackStartFrame - 1, 0), stance).Forget();
        }

        /// <summary>
        /// AI用の強攻撃ブロッキング実行
        /// </summary>
        public void HeavyBlocking(StanceType stance)
        {
            BlockingReleaseAfterDelay(Math.Max(_actionSetting.HeavyAttackStartFrame - 1, 0), stance).Forget();
        }

        /// <summary>
        /// 指定フレームだけ遅らせてからブロッキング実行
        /// </summary>
        private async UniTaskVoid BlockingReleaseAfterDelay(int delay, StanceType stance)
        {
            bool isCancel = await UniTask.DelayFrame(delay, PlayerLoopTiming.FixedUpdate, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();

            if (!isCancel)
            {
                BlockingAct(stance);
            }
        }

        /// <summary>
        /// 回避実行
        /// 後ろ回避は強すぎるので制限多め
        /// </summary>
        public void AvoidAct(MovementReportType type)
        {
            if (!_stateSystem.CanAvoid)
            {
                return;
            }

            // バックステップはエネルギー消費増大
            // エネルギー切れ時は性能大幅低下
            if (type == MovementReportType.BackStep)
            {
                if (_stateSystem.Energy == 0)
                {
                    _movementSystem.Avoid(type, _actionSetting.BackAvoidSpeed * 0.7f, _actionSetting.BackAvoidDuration * 0.7f);
                }
                else
                {
                    _movementSystem.Avoid(type, _actionSetting.BackAvoidSpeed, _actionSetting.BackAvoidDuration);
                    _stateSystem.UseEnergy(_actionSetting.BackAvoidEnergyCost);
                }
            }
            else
            {
                if (_stateSystem.Energy == 0)
                {
                    _movementSystem.Avoid(type, _actionSetting.AvoidSpeed * 0.7f, _actionSetting.AvoidDuration * 0.7f);
                }
                else
                {
                    _movementSystem.Avoid(type, _actionSetting.AvoidSpeed, _actionSetting.AvoidDuration);
                    _stateSystem.UseEnergy(_actionSetting.AvoidEnergyCost);
                }
            }
        }

        /// <summary>
        /// 回避攻撃（AI用）
        /// </summary>
        public async UniTaskVoid AvoidAttackAct(MovementReportType type)
        {
            if (!_stateSystem.CanAvoid && type == MovementReportType.BackStep)
            {
                return;
            }

            _stateSystem.UseEnergy(_actionSetting.AvoidEnergyCost);
            _movementSystem.Avoid(type, _actionSetting.AvoidSpeed, _actionSetting.AvoidDuration);

            float waitTime = _actionSetting.AvoidAttackInputDuration * 0.7f;

            bool isCancel = await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: destroyCancellationToken).SuppressCancellationThrow();

            if (!isCancel)
            {
                LightAttackAct(_stateSystem.CurrentStance.CurrentValue).Forget();
            }
        }

        /// <summary>
        /// 移動実行
        /// 後方ベクトルへの移動は減速する
        /// </summary>
        public void MoveAct(Vector3 moveVector)
        {
            if (!_stateSystem.CanMove || _enemyTransform == null)
            {
                return;
            }

            // 移動ベクトルを正規化して方向を取得
            Vector3 normalizedMove = moveVector.normalized;

            // 速度減衰係数を計算
            // 後ろ移動は速度を落とす
            float speedFactor;
            Debug.Log($"ヌルチェック{_actionSetting == null}、{_stateSystem == null}");
            float backSpeedFactor = _actionSetting.BackMoveMultiplier;
            const float forwardSpeedFactor = 1.0f;

            if (normalizedMove.z >= 0)
            {
                // Zが 0 または 正 (前、横、斜め前) の場合は、速度係数は常に 1.0
                speedFactor = forwardSpeedFactor; // 1.0f
            }
            else
            {
                // Zが 負 (後ろ、斜め後ろ) の場合、滑らかに減速する

                // Zの負の成分を 0〜1 の範囲に変換する（例: -1 -> 1, -0.5 -> 0.5, 0 -> 0）
                float backRatio = Mathf.Abs(normalizedMove.z);

                // Lerpで補間 (tが0で1.0倍速、tが1で0.7倍速)
                // Lerp(1.0, 0.7, backRatio)
                // - 真後ろ (Z=-1, backRatio=1) の時: 0.7倍速
                // - 斜め後ろ (Z=-0.5, backRatio=0.5) の時: 0.85倍速
                // - 真横 (Z=0, backRatio=0) の時: 1.0倍速 (厳密にはこのブロックに入らないが、連続性を持たせるため)
                speedFactor = Mathf.Lerp(forwardSpeedFactor, backSpeedFactor, backRatio);
            }

            // 最終速度を計算
            float speed = _actionSetting.MoveSpeed * speedFactor;

            // 移動実行
            _movementSystem.Move(moveVector * speed);
        }

        /// <summary>
        /// 怯み等で行動キャンセルされた場合にコールバックされる
        /// </summary>
        private void OnStunCancel()
        {
            ActionState last = _stateSystem.LastState;
            Debug.Log($"[{gameObject.name}]スタン解除: {_stateSystem.CurrentState.CurrentValue} 前回{last}");

            if ((last & ActionState.攻撃) > 0)
            {
                _hitSystem.DamageStop();
            }
        }

        /// <summary>
        /// targetを設定する
        /// </summary>
        /// <param name="target"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void SetTarget(GameObject target)
        {
            _enemyTransform = target.transform;

            _enemyPosition = _enemyTransform.GetComponent<PositionCache>();
            _myPosition = GetComponent<PositionCache>();

            // Rigidbodyの取得（未設定の場合）
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody>();
            }

            _actionSetting = _stateSystem.actionSetting;

            // 被ダメージによる行動キャンセルイベント
            _stateSystem.CurrentState
                .Where(state => (state & ActionState.強制行動キャンセル) > 0)
                .Subscribe(_ => OnStunCancel())
                .AddTo(this);
        }
    }
}