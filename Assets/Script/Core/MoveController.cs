using System.Runtime.CompilerServices;
using UnityEngine;

namespace LearningAIGame.CombatSystem.Core
{
    /// <summary>
    /// 移動種類の区分
    /// </summary>
    public enum MoveType : byte
    {
        Velocity,
        AddForce,
        None
    }

    /// <summary>
    /// 移動速度管理を担当するコントローラー
    /// </summary>
    public class MoveController : MonoBehaviour
    {
        /// <summary>
        /// 移動処理対象
        /// </summary>
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveStart(Vector3 moveVector)
        {

        }

        /// <summary>
        /// 加速度付き移動開始（AddForce設定）
        /// </summary>
        /// <param name="moveVector">移動ベクトル</param>
        /// <param name="moveDuration">移動継続時間</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddForce(Vector3 moveVector, float moveDuration)
        {

        }

        /// <summary>
        /// 停止処理（MoveTypeをNoneに設定）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stop()
        {

        }

        /// <summary>
        /// 毎フレーム速度計算。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalCalcSpeed()
        {

        }
    }
}