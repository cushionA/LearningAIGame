using UnityEngine;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// For Honorライクな方向入力システム（右スティック）
    /// 実際の位置関係ではなく、システマティックな3方向切り替え
    /// </summary>
    public class DirectionSystem : MonoBehaviour
    {
        #region 内部データ

        /// <summary>
        /// 現在の攻撃・防御方向
        /// </summary>
        private AttackDirection _currentDirection = AttackDirection.Up;

        /// <summary>
        /// 右スティック入力の閾値
        /// </summary>
        [SerializeField] private float stickThreshold = 0.5f;

        /// <summary>
        /// 方向切り替えの無効時間（システムが強制的に方向を変更した直後など）
        /// </summary>
        private float _directionLockTime = 0f;

        /// <summary>
        /// 前フレームの右スティック入力（連続切り替え防止用）
        /// </summary>
        private Vector2 _previousStickInput = Vector2.zero;

        #endregion

        #region 公開プロパティ

        /// <summary>
        /// 現在の攻撃・防御方向
        /// </summary>
        public AttackDirection CurrentDirection => _currentDirection;

        /// <summary>
        /// 方向変更が可能かどうか
        /// </summary>
        public bool CanChangeDirection => _directionLockTime <= 0f;

        #endregion

        #region 初期化・更新

        private void Update()
        {
            // 方向ロック時間の更新
            if (_directionLockTime > 0f)
            {
                _directionLockTime -= Time.deltaTime;
            }
        }

        #endregion

        #region 方向入力処理

        /// <summary>
        /// 右スティック入力から攻撃・防御方向を更新
        /// </summary>
        /// <param name="rightStickInput">右スティックの入力値（-1.0～1.0）</param>
        public void UpdateDirectionFromStick(Vector2 rightStickInput)
        {
            // 方向変更がロックされている場合は無視
            if (!CanChangeDirection)
                return;

            // 閾値未満の入力は無視
            if (rightStickInput.magnitude < stickThreshold)
                return;

            // 前フレームと同じ入力の場合は無視（連続切り替え防止）
            if (Vector2.Distance(rightStickInput, _previousStickInput) < 0.1f)
                return;

            _previousStickInput = rightStickInput;

            // スティック入力から方向を決定
            AttackDirection newDirection = DetermineDirectionFromStick(rightStickInput);

            // 方向が変化した場合のみ更新
            if (newDirection != _currentDirection)
            {
                _currentDirection = newDirection;
                OnDirectionChanged?.Invoke(_currentDirection);
            }
        }

        /// <summary>
        /// 右スティック入力から攻撃方向を決定
        /// </summary>
        /// <param name="stickInput">正規化されたスティック入力</param>
        /// <returns>対応する攻撃方向</returns>
        private AttackDirection DetermineDirectionFromStick(Vector2 stickInput)
        {
            // 角度を計算（-180度～180度）
            float angle = Mathf.Atan2(stickInput.y, stickInput.x) * Mathf.Rad2Deg;

            // 角度を0度～360度に正規化
            if (angle < 0f)
                angle += 360f;

            // 角度から方向を決定（120度ずつの3分割）
            if (angle >= 315f || angle < 45f)
                return AttackDirection.Right;   // 右方向（-45度～45度）
            else if (angle >= 45f && angle < 165f)
                return AttackDirection.Up;      // 上方向（45度～165度）
            else if (angle >= 165f && angle < 285f)
                return AttackDirection.Left;    // 左方向（165度～285度）
            else
                return AttackDirection.Up;      // 下方向（285度～315度）→上として扱う
        }

        #endregion

        #region 強制方向変更（システム用）

        /// <summary>
        /// システムが強制的に方向を変更（回避・ブースト等）
        /// </summary>
        /// <param name="direction">強制する方向</param>
        /// <param name="lockTime">方向変更をロックする時間</param>
        public void ForceDirection(AttackDirection direction, float lockTime = 0.2f)
        {
            _currentDirection = direction;
            _directionLockTime = lockTime;
            OnDirectionChanged?.Invoke(_currentDirection);
        }

        /// <summary>
        /// 移動方向から攻撃方向を自動決定（回避攻撃・ブースト攻撃用）
        /// </summary>
        /// <param name="movementVector">移動方向ベクトル</param>
        /// <param name="lockTime">方向変更をロックする時間</param>
        public void DeriveDirectionFromMovement(Vector3 movementVector, float lockTime = 0.2f)
        {
            if (movementVector.magnitude < 0.1f)
                return;

            // 移動方向から攻撃方向を決定
            Vector2 movementInput = new Vector2(movementVector.x, movementVector.z).normalized;
            AttackDirection derivedDirection = DetermineDirectionFromStick(movementInput);

            ForceDirection(derivedDirection, lockTime);
        }

        #endregion

        #region イベント

        /// <summary>
        /// 方向が変更された時のイベント
        /// </summary>
        public System.Action<AttackDirection> OnDirectionChanged;

        #endregion

        #region デバッグ

        /// <summary>
        /// デバッグ用：現在の方向を文字列で取得
        /// </summary>
        public string GetDirectionDebugString()
        {
            string lockStatus = CanChangeDirection ? "" : " (LOCKED)";
            return $"Direction: {_currentDirection}{lockStatus}";
        }

        #endregion
    }
}
