using UnityEngine;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// Transform.positionへの頻繁なアクセスを最適化するキャッシュシステム
    /// </summary>
    public class PositionCache : MonoBehaviour
    {
        #region キャッシュデータ

        /// <summary>
        /// キャッシュされた位置
        /// </summary>
        private Vector3 _cachedPosition;

        /// <summary>
        /// キャッシュされた回転
        /// </summary>
        private Quaternion _cachedRotation;

        /// <summary>
        /// キャッシュされた前方向ベクトル
        /// </summary>
        private Vector3 _cachedForward;

        /// <summary>
        /// キャッシュされた右方向ベクトル
        /// </summary>
        private Vector3 _cachedRight;

        /// <summary>
        /// キャッシュされた上方向ベクトル
        /// </summary>
        private Vector3 _cachedUp;

        /// <summary>
        /// 前フレームの位置（移動量計算用）
        /// </summary>
        private Vector3 _previousPosition;

        /// <summary>
        /// 計算された移動量
        /// </summary>
        private Vector3 _velocity;

        /// <summary>
        /// 計算された移動速度
        /// </summary>
        private float _speed;

        #endregion

        #region 公開プロパティ

        /// <summary>
        /// キャッシュされた位置（transform.positionの代替）
        /// </summary>
        public Vector3 Position => _cachedPosition;

        /// <summary>
        /// キャッシュされた回転（transform.rotationの代替）
        /// </summary>
        public Quaternion Rotation => _cachedRotation;

        /// <summary>
        /// キャッシュされた前方向（transform.forwardの代替）
        /// </summary>
        public Vector3 Forward => _cachedForward;

        /// <summary>
        /// キャッシュされた右方向（transform.rightの代替）
        /// </summary>
        public Vector3 Right => _cachedRight;

        /// <summary>
        /// キャッシュされた上方向（transform.upの代替）
        /// </summary>
        public Vector3 Up => _cachedUp;

        /// <summary>
        /// 現在フレームの移動量
        /// </summary>
        public Vector3 Velocity => _velocity;

        /// <summary>
        /// 現在の移動速度
        /// </summary>
        public float Speed => _speed;

        /// <summary>
        /// 前フレームからの移動距離
        /// </summary>
        public float MovementDistance => _velocity.magnitude;

        /// <summary>
        /// 移動中かどうか（閾値0.01f）
        /// </summary>
        public bool IsMoving => _speed > 0.01f;

        #endregion

        #region 初期化・更新

        private void Awake()
        {
            // 初期キャッシュ
            UpdateCache();
            _previousPosition = _cachedPosition;
        }

        private void Update()
        {
            UpdateCache();
        }

        /// <summary>
        /// キャッシュを更新（毎フレーム実行）
        /// </summary>
        private void UpdateCache()
        {
            // 前フレームの位置を保存
            _previousPosition = _cachedPosition;

            // Transform情報をキャッシュ
            _cachedPosition = transform.position;
            _cachedRotation = transform.rotation;
            _cachedForward = transform.forward;
            _cachedRight = transform.right;
            _cachedUp = transform.up;

            // 移動量・速度を計算
            _velocity = (_cachedPosition - _previousPosition) / Time.deltaTime;
            _speed = _velocity.magnitude;
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 指定した位置との距離を計算
        /// </summary>
        /// <param name="targetPosition">対象位置</param>
        /// <returns>距離</returns>
        public float DistanceTo(Vector3 targetPosition)
        {
            return Vector3.Distance(_cachedPosition, targetPosition);
        }

        /// <summary>
        /// 他のPositionCacheとの距離を計算
        /// </summary>
        /// <param name="other">他のPositionCache</param>
        /// <returns>距離</returns>
        public float DistanceTo(PositionCache other)
        {
            return Vector3.Distance(_cachedPosition, other._cachedPosition);
        }

        /// <summary>
        /// 指定した位置への方向ベクトルを計算
        /// </summary>
        /// <param name="targetPosition">対象位置</param>
        /// <returns>正規化された方向ベクトル</returns>
        public Vector3 DirectionTo(Vector3 targetPosition)
        {
            return (targetPosition - _cachedPosition).normalized;
        }

        /// <summary>
        /// 他のPositionCacheへの方向ベクトルを計算
        /// </summary>
        /// <param name="other">他のPositionCache</param>
        /// <returns>正規化された方向ベクトル</returns>
        public Vector3 DirectionTo(PositionCache other)
        {
            return (other._cachedPosition - _cachedPosition).normalized;
        }

        /// <summary>
        /// 指定した位置との相対位置を計算
        /// </summary>
        /// <param name="targetPosition">対象位置</param>
        /// <returns>相対位置ベクトル</returns>
        public Vector3 RelativePosition(Vector3 targetPosition)
        {
            return targetPosition - _cachedPosition;
        }

        /// <summary>
        /// 他のPositionCacheとの相対位置を計算
        /// </summary>
        /// <param name="other">他のPositionCache</param>
        /// <returns>相対位置ベクトル</returns>
        public Vector3 RelativePosition(PositionCache other)
        {
            return other._cachedPosition - _cachedPosition;
        }

        /// <summary>
        /// 指定した位置が特定の距離内にあるかチェック
        /// </summary>
        /// <param name="targetPosition">対象位置</param>
        /// <param name="range">距離</param>
        /// <returns>範囲内にある場合true</returns>
        public bool IsInRange(Vector3 targetPosition, float range)
        {
            return Vector3.SqrMagnitude(targetPosition - _cachedPosition) <= range * range;
        }

        /// <summary>
        /// 他のPositionCacheが特定の距離内にあるかチェック
        /// </summary>
        /// <param name="other">他のPositionCache</param>
        /// <param name="range">距離</param>
        /// <returns>範囲内にある場合true</returns>
        public bool IsInRange(PositionCache other, float range)
        {
            return Vector3.SqrMagnitude(other._cachedPosition - _cachedPosition) <= range * range;
        }

        #endregion

        #region 強制更新

        /// <summary>
        /// 即座にキャッシュを更新（Transform変更直後などに使用）
        /// </summary>
        public void ForceUpdateCache()
        {
            UpdateCache();
        }

        #endregion

        #region デバッグ

        /// <summary>
        /// デバッグ用：位置情報の文字列取得
        /// </summary>
        public string GetPositionDebugString()
        {
            return $"Pos: {_cachedPosition:F2}, Speed: {_speed:F2}, Moving: {IsMoving}";
        }

        #endregion
    }
}
