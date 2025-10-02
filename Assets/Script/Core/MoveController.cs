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
        /// 移動開始時間
        /// </summary>
        private float _moveStartTime;

        /// <summary>
        /// 移動継続時間
        /// </summary>
        private float _moveDuration;

        /// <summary>
        /// 移動ベクトル
        /// </summary>
        private Vector3 _moveDirection;

        /// <summary>
        /// 移動種類
        /// </summary>
        private MoveType _moveType;

        /// <summary>
        /// 通常移動開始（Velocity設定）
        /// </summary>
        /// <param name="moveVector">移動ベクトル</param>
        public void MoveStart(Vector3 moveVector)
        {
            _moveDirection = moveVector;
            _moveType = MoveType.Velocity;
            _moveDuration = 0f;
        }

        /// <summary>
        /// 加速度付き移動開始（AddForce設定）
        /// </summary>
        /// <param name="moveVector">移動ベクトル</param>
        /// <param name="moveDuration">移動継続時間</param>
        public void AddForce(Vector3 moveVector, float moveDuration)
        {
            _moveDirection = moveVector;
            _moveStartTime = Time.time;
            _moveDuration = moveDuration;
            _moveType = MoveType.AddForce;
        }

        /// <summary>
        /// 停止処理（MoveTypeをNoneに設定）
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

            // 移動タイプによって処理を分岐
            switch (_moveType)
            {
                case MoveType.Velocity:
                    // 等速運動：速度を直接設定
                    _rb.linearVelocity = _moveDirection;
                    break;

                case MoveType.AddForce:
                    // 加速度付き運動：指数関数的な減衰で自然な力の減衰を表現
                    float elapsedTime = Time.time - _moveStartTime;
                    float normalizedTime = elapsedTime / _moveDuration;

                    // 1 - t^2 の減衰カーブ（開始時最大、滑らかに減速）
                    float decayFactor = 1f - (normalizedTime * normalizedTime);
                    _rb.linearVelocity = _moveDirection * decayFactor;

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