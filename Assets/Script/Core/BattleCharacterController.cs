using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Setting;
using LearningAIGame.CombatSystem.Systems;
using System;
using System.Threading.Tasks;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

//==============================================ファイルヘッダ===========================================================
// BattleCharacterController
// 
// 概要: バトルキャラクターの行動制御を統括するコントローラークラス
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// プレイヤーまたはAIからの入力を受け付け、各種システム(攻撃/防御/移動)に処理を委譲する。
// 状態管理システムと連携し、行動可否の判定やエネルギー管理を行う。
// 攻撃時の踏み込み方向計算など、敵との位置関係に基づく処理も担当。
// 
// 入力元クラス:プレイヤーの入力コントローラー / キャラAI
// 出力先クラス:AttackSystem, DefenseSystem, MovementSystem, StateSystem
// 
// その他:
// UniTask使用(HeavyAttackFeint)
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
        /// アクション設定データ
        /// </summary>
        [SerializeField]
        private ActionSetting _actionSetting;

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
        /// 自分の位置のキャッシュ
        /// </summary>
        [SerializeField]
        private Vector3 _myPosition;

        /// <summary>
        /// 敵の位置のキャッシュ
        /// </summary>
        private Vector3 _enemyPosition;

        private void Update()
        {
            // 自分の位置を毎フレームキャッシュ
            _myPosition = transform.position;

            // 敵の位置を毎フレームキャッシュ
            _enemyPosition = _enemyTransform.position;
        }

        /// <summary>
        /// 弱攻撃実行
        /// </summary>
        public void WeakAttackAct(StanceType stance)
        {
            // 攻撃可能な状態かを確認
            if (!_stateSystem.CanAttack)
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
            _attackSystem.WeakAttack(damage, stance, stepVector, stepDuration);
        }

        /// <summary>
        /// 強攻撃実行
        /// </summary>
        public void HeavyAttackAct(StanceType stance)
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
            _attackSystem.HeavyAttack(damage, stance, stepVector, stepDuration);
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
        }

        /// <summary>
        /// 強攻撃フェイント（AI用）
        /// </summary>
        public async UniTaskVoid HeavyAttackFeint(StanceType stance)
        {
            // 強攻撃を開始
            HeavyAttackAct(stance);

            // 1秒待機
            bool isSuccess = await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow();

            // 正常に待機が終わればキャンセル実行
            if (isSuccess)
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

            // コスト消費
            _stateSystem.UseEnergy(_actionSetting.AvoidEnergyCost);

            // 回避実行
            _movementSystem.Avoid(type, _actionSetting.AvoidSpeed, _actionSetting.AvoidDuration);
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
            bool isSuccess = await UniTask.Delay(TimeSpan.FromSeconds(waitTime), cancellationToken: this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow();

            // 正常に待機が終わればキャンセル実行
            if (isSuccess)
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
            _movementSystem.Move(moveVector);
        }

        /// <summary>
        /// エネルギー消費処理
        /// </summary>
        /// <param name="useAmount">消費量</param>
        private void InternalEnergyUse(int useAmount)
        {
            // 状態管理システムのエネルギーを減らす
            _stateSystem.UseEnergy(useAmount);
        }

        /// <summary>
        /// 敵への法線ベクトルを取得
        /// </summary>
        /// <returns>正規化された敵への方向ベクトル</returns>
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
    }
}