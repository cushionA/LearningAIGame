using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LearningAIGame.CombatSystem.Core
{
    /// <summary>
    /// 移動速度管理を担当するコントローラー
    /// </summary>
    public class MoveController : MonoBehaviour
    {
        /// <summary>
        /// 移動種類の区分
        /// </summary>
        private enum MoveType : byte
        {
            Velocity,
            AddForce,
            None
        }

        /// <summary>
        /// 移動処理対象
        /// </summary>
        [SerializeField]
        private Rigidbody _rb;

        /// <summary>
        /// 地面検知用のレイキャスト距離
        /// </summary>
        [SerializeField]
        private float _groundCheckDistance = 0.1f;

        /// <summary>
        /// 地面として認識するレイヤー
        /// </summary>
        [SerializeField]
        private LayerMask _groundLayer = -1;

        /// <summary>
        /// ローカル座標系で移動するか（true: キャラの向きに沿う、false: ワールド座標）
        /// </summary>
        [SerializeField]
        private bool _useLocalDirection = true;

        /// <summary>
        /// 移動開始時間
        /// </summary>
        private float _moveStartTime;

        /// <summary>
        /// 移動継続時間
        /// </summary>
        private float _moveDuration;

        /// <summary>
        /// 移動ベクトル(ローカル座標系またはワールド座標系)
        /// </summary>
        private Vector3 _moveDirection;

        /// <summary>
        /// 移動種類
        /// </summary>
        private MoveType _moveType;

        /// <summary>
        /// 通常移動開始(Velocity設定)
        /// </summary>
        /// <param name="moveVector">移動ベクトル(ローカル座標系の場合は自分の向き基準)</param>
        public void MoveStart(Vector3 moveVector)
        {
            // Y軸成分は無視して水平方向のみ保持
            Vector3 localDirection = new Vector3(moveVector.x, 0f, moveVector.z);

            // ローカル座標系の場合、ワールド座標に変換
            _moveDirection = _useLocalDirection
                ? transform.TransformDirection(localDirection)
                : localDirection;

            _moveType = MoveType.Velocity;
            _moveDuration = 0f;
        }

        /// <summary>
        /// 加速度付き移動開始(AddForce設定)
        /// </summary>
        /// <param name="moveVector">移動ベクトル(ローカル座標系の場合は自分の向き基準)</param>
        /// <param name="moveDuration">移動継続時間</param>
        public void AddForce(Vector3 moveVector, float moveDuration)
        {
            // 移動時間が0以下の場合は即座に停止
            if (moveDuration <= 0f)
            {
                Debug.LogWarning($"[MoveController] Invalid moveDuration: {moveDuration}. Movement will be ignored.");
                _moveType = MoveType.None;
                return;
            }

            // Y軸成分は無視して水平方向のみ保持
            Vector3 localDirection = new Vector3(moveVector.x, 0f, moveVector.z);

            // ローカル座標系の場合、ワールド座標に変換
            _moveDirection = _useLocalDirection
                ? transform.TransformDirection(localDirection)
                : localDirection;

            _moveStartTime = Time.time;
            _moveDuration = moveDuration;
            _moveType = MoveType.AddForce;
        }

        /// <summary>
        /// 停止処理(MoveTypeをNoneに設定)
        /// </summary>
        public void Stop()
        {
            _moveType = MoveType.None;
        }

        private void FixedUpdate()
        {
            InternalCalcSpeed();
        }

        /// <summary>
        /// 毎フレーム速度計算。FixedUpdateで呼び出す
        /// </summary>
        private void InternalCalcSpeed()
        {
            // 移動タイプがNoneの場合は早期リターン
            if (_moveType == MoveType.None)
            {
                return;
            }

            // 現在のY軸速度(重力による落下速度)を保持
            float verticalVelocity = _rb.linearVelocity.y;

            // 移動タイプによって処理を分岐
            switch (_moveType)
            {
                case MoveType.Velocity:
                    // 等速運動:水平方向の速度のみ設定、Y軸は保持
                    _rb.linearVelocity = new Vector3(
                        _moveDirection.x,
                        verticalVelocity,
                        _moveDirection.z
                    );
                    break;

                case MoveType.AddForce:
                    // 加速度付き運動:指数関数的な減衰で自然な力の減衰を表現
                    float elapsedTime = Time.time - _moveStartTime;
                    float normalizedTime = elapsedTime / _moveDuration;

                    // 1 - t^2 の減衰カーブ(開始時最大、滑らかに減速)
                    float decayFactor = 1f - (normalizedTime * normalizedTime);

                    Vector3 horizontalVelocity = _moveDirection * decayFactor;

                    // 水平方向の速度のみ適用、Y軸は保持
                    _rb.linearVelocity = new Vector3(
                        horizontalVelocity.x,
                        verticalVelocity,
                        horizontalVelocity.z
                    );

                    // 移動継続時間を超えたら停止
                    if (elapsedTime >= _moveDuration)
                    {
                        _moveDirection = Vector3.zero;
                        _moveType = MoveType.None;
                    }
                    break;
            }
        }
    }
}