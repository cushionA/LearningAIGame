using NaughtyAttributes;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LearningAIGame.CombatSystem.Core
{
    /// <summary>
    /// 敵を中心とした移動速度管理を担当するコントローラー
    /// </summary>
    /// <remarks>
    /// 移動は常にターゲット相対座標系で計算される:
    /// - 前方(Z+): ターゲットに接近
    /// - 後方(Z-): ターゲットから離脱
    /// - 左右(X): ターゲットを中心とした円周移動
    /// </remarks>
    public class MoveController : MonoBehaviour, ITargetSet
    {
        #region 定数

        /// <summary>
        /// ターゲットとの最小距離（ゼロ除算防止）
        /// </summary>
        private const float k_MIN_TARGET_DISTANCE = 0.1f;

        #endregion

        #region 列挙型

        /// <summary>
        /// 移動種類の区分
        /// </summary>
        private enum MoveType : byte
        {
            /// <summary>等速移動（Velocity直接設定）</summary>
            Velocity,
            /// <summary>減速移動（AddForce的な挙動）</summary>
            Decelerate,
            /// <summary>移動なし</summary>
            None
        }

        #endregion

        #region シリアライズフィールド

        /// <summary>
        /// 移動処理対象のRigidbody
        /// </summary>
        [SerializeField]
        private Rigidbody _rb;

        #endregion

        #region プライベートフィールド

        /// <summary>
        /// ターゲット（敵）のTransform
        /// </summary>
        [SerializeField]
        [ReadOnly]
        private Transform _target;

        /// <summary>
        /// 現在の移動種類
        /// </summary>
        private MoveType _moveType = MoveType.None;

        /// <summary>
        /// 入力された移動ベクトル（ローカル座標系: X=左右, Z=前後）
        /// </summary>
        private Vector3 _inputVector;

        /// <summary>
        /// AddForce用: 移動開始時のワールド方向ベクトル
        /// </summary>
        private Vector3 _decelerateWorldDirection;

        /// <summary>
        /// AddForce用: 初期速度
        /// </summary>
        private float _decelerateInitialSpeed;

        /// <summary>
        /// AddForce用: 移動開始時間
        /// </summary>
        private float _decelerateStartTime;

        /// <summary>
        /// AddForce用: 移動継続時間
        /// </summary>
        private float _decelerateDuration;

        #endregion

        #region パブリックAPI

        /// <summary>
        /// ターゲット（敵）を設定する
        /// </summary>
        /// <param name="target">ターゲットのTransform（nullで解除）</param>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        /// <summary>
        /// 通常移動開始（等速移動）
        /// </summary>
        /// <param name="moveVector">移動ベクトル（X=左右, Z=前後）</param>
        /// <remarks>
        /// ターゲット相対座標系で解釈される:
        /// - Z+: ターゲットに接近
        /// - Z-: ターゲットから離脱
        /// - X+: ターゲットを中心に時計回り
        /// - X-: ターゲットを中心に反時計回り
        /// </remarks>
        public void MoveStart(Vector3 moveVector)
        {
            // ターゲット未設定時は移動しない
            if (_target == null)
            {
                _moveType = MoveType.None;
                return;
            }

            // Y軸成分は無視
            _inputVector = new Vector3(moveVector.x, 0f, moveVector.z);
            _moveType = MoveType.Velocity;
        }

        /// <summary>
        /// 減速付き移動開始（AddForce的挙動）
        /// </summary>
        /// <param name="moveVector">移動ベクトル（X=左右, Z=前後）</param>
        /// <param name="moveDuration">移動継続時間</param>
        /// <remarks>
        /// 移動開始時点のターゲット位置を基準にワールド方向を計算し、
        /// その方向へ減速しながら移動する
        /// </remarks>
        public void AddForce(Vector3 moveVector, float moveDuration)
        {
            // ターゲット未設定時は移動しない
            if (_target == null)
            {
                _moveType = MoveType.None;
                return;
            }

            // 移動時間が0以下の場合は無視
            if (moveDuration <= 0f)
            {
                Debug.LogWarning($"[MoveController] Invalid moveDuration: {moveDuration}. Movement will be ignored.");
                _moveType = MoveType.None;
                return;
            }

            // Y軸成分は無視
            Vector3 inputXZ = new Vector3(moveVector.x, 0f, moveVector.z);

            // 移動開始時点でワールド方向を計算・固定
            _decelerateWorldDirection = CalculateWorldDirection(inputXZ, out float speed);
            _decelerateInitialSpeed = speed;
            _decelerateStartTime = Time.time;
            _decelerateDuration = moveDuration;
            _moveType = MoveType.Decelerate;
        }

        /// <summary>
        /// 停止処理
        /// </summary>
        public void Stop()
        {
            _rb.linearVelocity = Vector3.zero;
            _moveType = MoveType.None;
        }

        #endregion

        #region Unityイベント

        private void FixedUpdate()
        {
            UpdateMovement();
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 毎フレームの移動更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateMovement()
        {
            if (_moveType == MoveType.None)
            {
                return;
            }

            // 現在のY軸速度（重力）を保持
            float verticalVelocity = _rb.linearVelocity.y;

            switch (_moveType)
            {
                case MoveType.Velocity:
                    UpdateVelocityMovement(verticalVelocity);
                    break;

                case MoveType.Decelerate:
                    UpdateDecelerateMovement(verticalVelocity);
                    break;
            }
        }

        /// <summary>
        /// 等速移動の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateVelocityMovement(float verticalVelocity)
        {
            // ターゲットが消えた場合は停止
            if (_target == null)
            {
                _moveType = MoveType.None;
                return;
            }

            // 毎フレーム、現在のターゲット位置を基準にワールド方向を計算
            Vector3 worldDirection = CalculateWorldDirection(_inputVector, out float speed);

            _rb.linearVelocity = new Vector3(
                worldDirection.x * speed,
                verticalVelocity,
                worldDirection.z * speed
            );
        }

        /// <summary>
        /// 減速移動の更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateDecelerateMovement(float verticalVelocity)
        {
            float elapsedTime = Time.time - _decelerateStartTime;

            // 移動時間終了チェック
            if (elapsedTime >= _decelerateDuration)
            {
                _moveType = MoveType.None;
                return;
            }

            // 減衰係数: 1 - t^2 のカーブ
            float normalizedTime = elapsedTime / _decelerateDuration;
            float decayFactor = 1f - (normalizedTime * normalizedTime);

            float currentSpeed = _decelerateInitialSpeed * decayFactor;

            _rb.linearVelocity = new Vector3(
                _decelerateWorldDirection.x * currentSpeed,
                verticalVelocity,
                _decelerateWorldDirection.z * currentSpeed
            );
        }

        /// <summary>
        /// 入力ベクトルからワールド方向と速度を計算
        /// </summary>
        /// <param name="input">入力ベクトル（X=左右, Z=前後）</param>
        /// <param name="speed">出力: 移動速度</param>
        /// <returns>正規化されたワールド方向ベクトル</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 CalculateWorldDirection(Vector3 input, out float speed)
        {
            // 自分からターゲットへのベクトル
            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;

            float distanceToTarget = toTarget.magnitude;

            // 最小距離以下の場合は移動しない
            if (distanceToTarget < k_MIN_TARGET_DISTANCE)
            {
                speed = 0f;
                return Vector3.zero;
            }

            // ターゲット方向の正規化ベクトル
            Vector3 forwardDir = toTarget / distanceToTarget;

            // 右方向（時計回り方向）
            Vector3 rightDir = new Vector3(forwardDir.z, 0f, -forwardDir.x);

            // 入力を分解
            float forwardInput = input.z;  // 前後（+で接近、-で離脱）
            float strafeInput = input.x;   // 左右（+で時計回り、-で反時計回り）

            // ワールド方向を合成
            Vector3 worldDirection = (forwardDir * forwardInput) + (rightDir * strafeInput);

            // 速度は入力の大きさ
            speed = worldDirection.magnitude;

            // 正規化して返す（速度0の場合はゼロベクトル）
            if (speed > 0.001f)
            {
                return worldDirection / speed;
            }

            speed = 0f;
            return Vector3.zero;
        }

        public void SetTarget(GameObject target)
        {
            _target = target.transform;
        }

        #endregion
    }
}