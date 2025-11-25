//==============================================ファイルヘッダ===========================================================
// LockOnCameraSystem
// 
// 概要: 速度予測型ロックオンカメラシステム
// 
// 制作者: 小さな座布団
// 
// 機能説明:
// 自キャラの背後に追従し、敵をフォーカスするカメラシステム。
// ロックオン時は自キャラと敵の中間点を注視し、敵との距離に応じてカメラ距離を自動調整する。
// PositionCacheから取得した速度情報を使用して移動を予測し、スムーズながくつきのない追従を実現。
// 1v1戦闘に特化し、インスペクタで指定した単一の敵をターゲットとする。
// 
// 入力元クラス: 外部からSetEnemy()で制御、PositionCacheから速度取得
// 出力先クラス: なし(カメラ制御のみ)
// 
// その他:
// 独立したカメラシステムとして動作し、既存のBaseSystemとの連携は不要
// 速度ベース予測により、横移動時のガクつきを解消
//=====================================================================================================================

using UnityEngine;
using LearningAIGame.CombatSystem;

/// <summary>
/// 速度予測型ロックオンカメラシステム
/// 自キャラの背後に追従し、敵をフォーカスする
/// </summary>
public class LockOnCameraSystem : MonoBehaviour
{
    #region ターゲット設定

    [Header("ターゲット設定")]
    [Tooltip("追従する自キャラ")]
    [SerializeField] private Transform _player;

    [Tooltip("ロックオンする敵キャラ")]
    [SerializeField] private Transform _enemy;

    #endregion

    #region 速度ベース追従設定

    [Header("速度ベース追従設定")]
    [Tooltip("プレイヤーのPositionCache(nullの場合は自動取得)")]
    [SerializeField] private PositionCache _playerCache;

    [Tooltip("敵のPositionCache(nullの場合は自動取得)")]
    [SerializeField] private PositionCache _enemyCache;

    [Tooltip("速度予測の係数(0=予測なし, 1=1秒先を予測)")]
    [Range(0f, 1f)]
    [SerializeField] private float _velocityPrediction = 0.3f;

    [Tooltip("速度予測の最大距離")]
    [SerializeField] private float _maxPredictionDistance = 3f;

    #endregion

    #region カメラ距離設定

    [Header("カメラ距離設定")]
    [Tooltip("基本距離")]
    [SerializeField] private float _baseDistance = 5f;

    [Tooltip("最小距離")]
    [SerializeField] private float _minDistance = 3f;

    [Tooltip("最大距離")]
    [SerializeField] private float _maxDistance = 10f;

    [Tooltip("カメラの高さオフセット")]
    [SerializeField] private float _heightOffset = 2f;

    #endregion

    #region ロックオン設定

    [Header("ロックオン設定")]
    [Tooltip("敵を画面内に収めるための距離調整係数")]
    [SerializeField] private float _distanceAdjustFactor = 0.5f;

    [Tooltip("注視点の自キャラ寄り割合 (0=敵, 1=自キャラ)")]
    [Range(0f, 1f)]
    [SerializeField] private float _lookAtPlayerWeight = 0.3f;

    #endregion

    #region 水平オフセット設定

    [Header("水平オフセット設定")]
    [Tooltip("水平オフセット量")]
    [SerializeField] private float _horizontalOffsetAmount = 1.5f;

    [Tooltip("左側にオフセットする(falseで右側)")]
    [SerializeField] private bool _offsetToLeft = false;

    #endregion

    #region スムージング設定

    [Header("スムージング設定")]
    [Tooltip("位置補間速度")]
    [SerializeField] private float _positionSmoothSpeed = 8f;

    [Tooltip("回転補間速度")]
    [SerializeField] private float _rotationSmoothSpeed = 10f;

    [Tooltip("距離補間速度")]
    [SerializeField] private float _distanceSmoothSpeed = 5f;

    #endregion

    #region 内部状態

    /// <summary>
    /// 現在の目標距離
    /// </summary>
    private float _currentDistance;

    /// <summary>
    /// 目標距離(補間用)
    /// </summary>
    private float _targetDistance;

    #endregion

    #region Unity ライフサイクル

    private void Start()
    {
        _currentDistance = _baseDistance;
        _targetDistance = _baseDistance;

        // PositionCacheの自動取得
        if (_player != null && _playerCache == null)
        {
            _playerCache = _player.GetComponent<PositionCache>();
            if (_playerCache == null)
            {
                Debug.LogWarning($"[LockOnCameraSystem] プレイヤーにPositionCacheコンポーネントがありません: {_player.name}");
            }
        }

        if (_enemy != null && _enemyCache == null)
        {
            _enemyCache = _enemy.GetComponent<PositionCache>();
        }

        // 初期位置を即座に設定
        if (_player != null)
        {
            UpdateCameraPosition(immediate: true);
        }
    }

    private void LateUpdate()
    {
        if (_player == null)
            return;

        UpdateCameraPosition(immediate: false);
    }

    #endregion

    #region カメラ更新処理

    /// <summary>
    /// カメラ位置・回転を更新
    /// </summary>
    /// <param name="immediate">即座に反映するか</param>
    private void UpdateCameraPosition(bool immediate)
    {
        Vector3 targetPosition;
        Quaternion targetRotation;

        if (_enemy != null)
        {
            // ロックオンモード
            CalculateLockOnCamera(out targetPosition, out targetRotation);
        }
        else
        {
            // 通常追従モード
            CalculateFollowCamera(out targetPosition, out targetRotation);
        }

        // 距離の補間
        _currentDistance = immediate
            ? _targetDistance
            : Mathf.Lerp(_currentDistance, _targetDistance, _distanceSmoothSpeed * Time.deltaTime);

        // 位置・回転の適用
        if (immediate)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                _positionSmoothSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _rotationSmoothSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// ロックオンモードのカメラ位置・回転を計算
    /// </summary>
    private void CalculateLockOnCamera(out Vector3 position, out Quaternion rotation)
    {
        // 速度予測を考慮した位置を取得
        Vector3 playerPos = GetPredictedPosition(_player, _playerCache);
        Vector3 enemyPos = GetPredictedPosition(_enemy, _enemyCache);

        // 自キャラと敵の距離
        float playerToEnemyDistance = Vector3.Distance(playerPos, enemyPos);

        // 距離に応じてカメラ距離を調整(敵が遠いほどカメラも離れる)
        _targetDistance = Mathf.Clamp(
            _baseDistance + playerToEnemyDistance * _distanceAdjustFactor,
            _minDistance,
            _maxDistance
        );

        // 敵から自キャラへの方向(カメラの配置方向)
        Vector3 directionFromEnemy = (playerPos - enemyPos).normalized;

        // 水平方向のみ考慮(Y軸を0に)
        Vector3 horizontalDirection = new Vector3(directionFromEnemy.x, 0f, directionFromEnemy.z).normalized;

        // 右方向ベクトル
        Vector3 rightDirection = Vector3.Cross(Vector3.up, horizontalDirection).normalized;

        // 固定水平オフセット(左右切り替え可能)
        float offsetSign = _offsetToLeft ? -1f : 1f;
        Vector3 horizontalOffset = rightDirection * _horizontalOffsetAmount * offsetSign;

        // カメラ位置(自キャラの背後 + 水平オフセット)
        position = playerPos
            + horizontalDirection * _currentDistance
            + horizontalOffset
            + Vector3.up * _heightOffset;

        // 注視点(自キャラと敵の中間点、重み付き)
        Vector3 lookAtPoint = Vector3.Lerp(enemyPos, playerPos, _lookAtPlayerWeight);
        lookAtPoint.y += _heightOffset * 0.5f; // 注視点を少し上げる

        // 回転
        rotation = Quaternion.LookRotation(lookAtPoint - position);
    }

    /// <summary>
    /// 通常追従モードのカメラ位置・回転を計算
    /// </summary>
    private void CalculateFollowCamera(out Vector3 position, out Quaternion rotation)
    {
        _targetDistance = _baseDistance;

        // 速度予測を考慮した位置を取得
        Vector3 playerPos = GetPredictedPosition(_player, _playerCache);

        // 自キャラの背後に配置
        Vector3 backDirection = -_player.forward;

        // 右方向ベクトル
        Vector3 rightDirection = _player.right;

        // 固定水平オフセット
        float offsetSign = _offsetToLeft ? -1f : 1f;
        Vector3 horizontalOffset = rightDirection * _horizontalOffsetAmount * offsetSign;

        position = playerPos
            + backDirection * _currentDistance
            + horizontalOffset
            + Vector3.up * _heightOffset;

        // 自キャラを注視
        Vector3 lookAtPoint = playerPos + Vector3.up * _heightOffset * 0.5f;
        rotation = Quaternion.LookRotation(lookAtPoint - position);
    }

    /// <summary>
    /// 速度予測を考慮した位置を取得
    /// </summary>
    /// <param name="target">対象Transform</param>
    /// <param name="cache">PositionCache(nullの場合は予測なし)</param>
    /// <returns>予測された位置</returns>
    private Vector3 GetPredictedPosition(Transform target, PositionCache cache)
    {
        if (target == null)
            return Vector3.zero;

        Vector3 currentPos = target.position;

        // PositionCacheがない、または速度予測が無効の場合は現在位置を返す
        if (cache == null || _velocityPrediction <= 0f)
            return currentPos;

        // 速度が十分に小さい場合は予測不要
        if (!cache.IsMoving)
            return currentPos;

        // 速度ベクトルから予測位置を計算
        Vector3 velocityOffset = cache.Velocity * _velocityPrediction;

        // 予測距離を制限
        if (velocityOffset.magnitude > _maxPredictionDistance)
        {
            velocityOffset = velocityOffset.normalized * _maxPredictionDistance;
        }

        return currentPos + velocityOffset;
    }

    #endregion

    #region 公開API

    /// <summary>
    /// 自キャラを設定
    /// </summary>
    public void SetPlayer(Transform player)
    {
        _player = player;

        // PositionCacheの自動取得
        if (_player != null)
        {
            _playerCache = _player.GetComponent<PositionCache>();
        }
        else
        {
            _playerCache = null;
        }
    }

    /// <summary>
    /// 敵をロックオン(nullで解除)
    /// </summary>
    public void SetEnemy(Transform enemy)
    {
        _enemy = enemy;

        // PositionCacheの自動取得
        if (_enemy != null)
        {
            _enemyCache = _enemy.GetComponent<PositionCache>();
        }
        else
        {
            _enemyCache = null;
        }
    }

    /// <summary>
    /// ロックオン中かどうか
    /// </summary>
    public bool IsLockedOn => _enemy != null;

    /// <summary>
    /// 現在のカメラ距離
    /// </summary>
    public float CurrentDistance => _currentDistance;

    /// <summary>
    /// カメラ位置を即座に更新
    /// </summary>
    public void SnapToTarget()
    {
        if (_player != null)
        {
            UpdateCameraPosition(immediate: true);
        }
    }

    /// <summary>
    /// 基本距離を動的に変更
    /// </summary>
    public void SetBaseDistance(float distance)
    {
        _baseDistance = Mathf.Clamp(distance, _minDistance, _maxDistance);
    }

    /// <summary>
    /// 水平オフセット量を設定
    /// </summary>
    /// <param name="amount">オフセット量</param>
    public void SetHorizontalOffsetAmount(float amount)
    {
        _horizontalOffsetAmount = Mathf.Max(0f, amount);
    }

    /// <summary>
    /// オフセット方向を設定
    /// </summary>
    /// <param name="toLeft">trueで左、falseで右</param>
    public void SetOffsetDirection(bool toLeft)
    {
        _offsetToLeft = toLeft;
    }

    /// <summary>
    /// オフセット方向を切り替え
    /// </summary>
    public void ToggleOffsetDirection()
    {
        _offsetToLeft = !_offsetToLeft;
    }

    /// <summary>
    /// 速度予測係数を設定
    /// </summary>
    /// <param name="prediction">予測係数(0-1)</param>
    public void SetVelocityPrediction(float prediction)
    {
        _velocityPrediction = Mathf.Clamp01(prediction);
    }

    /// <summary>
    /// 現在の水平オフセット量
    /// </summary>
    public float HorizontalOffsetAmount => _horizontalOffsetAmount;

    /// <summary>
    /// 現在左側にオフセットしているか
    /// </summary>
    public bool IsOffsetToLeft => _offsetToLeft;

    /// <summary>
    /// 現在の速度予測係数
    /// </summary>
    public float VelocityPrediction => _velocityPrediction;

    #endregion

    #region デバッグ

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_player == null)
            return;

        // カメラ距離の範囲を表示
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_player.position, _minDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(_player.position, _maxDistance);

        // 速度予測の可視化
        if (_playerCache != null && _playerCache.IsMoving)
        {
            Vector3 predictedPos = GetPredictedPosition(_player, _playerCache);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(_player.position, predictedPos);
            Gizmos.DrawWireSphere(predictedPos, 0.2f);
        }

        // 注視点を表示
        if (_enemy != null)
        {
            Vector3 playerPos = GetPredictedPosition(_player, _playerCache);
            Vector3 enemyPos = GetPredictedPosition(_enemy, _enemyCache);
            Vector3 lookAtPoint = Vector3.Lerp(enemyPos, playerPos, _lookAtPlayerWeight);
            lookAtPoint.y += _heightOffset * 0.5f;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lookAtPoint, 0.3f);

            // 自キャラと敵を結ぶ線
            Gizmos.color = Color.red;
            Gizmos.DrawLine(playerPos, enemyPos);

            // 敵の速度予測も表示
            if (_enemyCache != null && _enemyCache.IsMoving)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(_enemy.position, enemyPos);
                Gizmos.DrawWireSphere(enemyPos, 0.2f);
            }
        }
    }
#endif

    #endregion
}