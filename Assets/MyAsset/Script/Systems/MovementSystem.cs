using Cysharp.Threading.Tasks;
using LearningAIGame.CombatSystem.Core;
using System;
using System.Runtime.CompilerServices;
using UniRx;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using static LearningAIGame.CombatSystem.CharacterSettings;
using NaughtyAttributes;

namespace LearningAIGame.CombatSystem
{
    /// <summary>
    /// 移動データの構造体
    /// 共通設定かなんかで各移動始動時のリセット設定をビットフラグにまとめるか
    /// </summary>
    [System.Serializable]
    public struct MovementData
    {
        /// <summary>
        /// 現在の行動を開始した時間
        /// </summary>
        public float moveStartTime;

        /// <summary>
        /// 現在の移動状態
        /// </summary>
        public ActionState movementState;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="nowTime"></param>
        public MovementData(float nowTime)
        {
            moveStartTime = nowTime;
            movementState = ActionState.Idle;
        }

        /// <summary>
        /// 移動の更新
        /// </summary>
        /// <param name="vel"></param>
        /// <param name="grounded"></param>
        /// <param name="spd"></param>
        /// <param name="airT"></param>
        /// <param name="state"></param>
        public void UpdateMovementData(ActionState state)
        {
            movementState = state;
        }

    }

    /// <summary>
    /// 速度変化パターンの種類
    /// </summary>
    public enum SpeedBoostPattern : byte
    {
        /// <summary>山型：0→最大→0（滑らかな加速・減速）</summary>
        Mountain,
        /// <summary>減衰型：最大→0（最初が最速、徐々に減速）</summary>
        Decay,
        /// <summary>加速型：0→最大（徐々に加速）</summary>
        Acceleration,
        /// <summary>一定型：最大を維持</summary>
        Constant,
        /// <summary>急減衰型：最大→0（急激な減速）</summary>
        SharpDecay,
        /// <summary>弾性型：オーバーシュート後に安定</summary>
        Elastic
    }

    /// <summary>
    /// 改修版移動システム - アクション別ベクトル管理
    /// シンプルなベクトルブレンドシステム
    ///
    /// 改修版移動システム - 汎用移動タイプシステム
    /// 様々な移動パターンを統一的に管理
    ///
    /// 基礎的な移動コードを内包しつつ、ジャンプや吹っ飛ばしなどを行う窓口を持つ。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MovementSystem : BaseSystem<ActionState>
    {
        // コンポーネント
        private Rigidbody _rigidBody;

        ///// <summary>
        ///// 移動状態管理
        ///// </summary>
        //private MovementData currentMovementData;

        [Tooltip("移動設定")]
        public MovementSettings moveSetting;

        [Tooltip("空中時間の累計")]
        public float TotalAirTime { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 0f;

        // 移動状態タイマー（減衰処理用）
        private float _jumpDecayRate = 0f;
        private float _dodgeTimer = 0f;
        private float _lungeTimer = 0f;
        private float _lungeDistance = 0f;
        private float _lungeTravelDistance = 0f;
        private float _knockbackDecayRate = 0f;
        private bool _hasUsedDoubleJump = false;

        // 移動速度修正システム
        [Header("移動速度修正システム")]
        [Tooltip("現在適用中の移動速度修正")]
        private System.Collections.Generic.Dictionary<string, float> _speedModifiers = new System.Collections.Generic.Dictionary<string, float>();

        [Tooltip("最終的な移動速度倍率")]
        public float FinalSpeedMultiplier { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = 1f;

        // 地面・壁検知
        [Header("地面・壁検知設定")]
        [Tooltip("地面検知のレイヤーマスク")]
        [SerializeField] private LayerMask _groundLayerMask = 1;

        [Tooltip("壁検知のレイヤーマスク")]
        [SerializeField] private LayerMask _wallLayerMask = 1;

        [Tooltip("地面検知の距離")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _groundCheckDistance = 1.1f;

        [Tooltip("壁検知の距離")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _wallCheckDistance = 0.6f;

        // === 統合機能：重力・ジャンプシステム ===
        [Header("重力・ジャンプ設定")]
        [Tooltip("重力値")]
        [SerializeField] private float _gravity = -20f;

        [Tooltip("最大落下速度")]
        [SerializeField] private float _maxFallSpeed = -25f;

        [Tooltip("ジャンプ力")]
        [SerializeField] private float _jumpForce = 10f;

        [Tooltip("ジャンプ早期終了時の速度倍率")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _jumpCutMultiplier = 0.5f;

        // === 統合機能：コヨーテタイム・ジャンプバッファ ===
        [Header("コヨーテタイム設定")]
        //[Tooltip("地面を離れてもジャンプ可能な時間")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _coyoteTime = 0.2f;

        //[Tooltip("ジャンプ入力を受け付ける猶予時間")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _jumpBufferTime = 0.1f;

        // === 統合機能：オーディオ・エフェクト ===
        [Header("オーディオ・エフェクト")]
        [Tooltip("AudioSourceコンポーネント")]
        [SerializeField] private AudioSource _audioSource;

        [Tooltip("足音配列")]
        [SerializeField] private AudioClip[] _footstepSounds;

        [Tooltip("ジャンプ音")]
        [SerializeField] private AudioClip _jumpSound;

        [Tooltip("着地音")]
        [SerializeField] private AudioClip _landSound;

        [Tooltip("着地エフェクト")]
        [SerializeField] private ParticleSystem _landingParticles;

        [Tooltip("ダッシュエフェクト")]
        [SerializeField] private ParticleSystem _dashParticles;

        // === 統合機能：速度加算システム ===
        [Header("速度加算システム")]
        [Tooltip("最大水平速度")]
        [SerializeField] private float _maxHorizontalSpeed = 15f;

        [Tooltip("最小速度閾値")]
        [SerializeField] private float _minHorizontalSpeed = 0.1f;

        // === プライベート変数（統合機能用） ===
        private bool _wasGroundedLastFrame;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private float _verticalVelocity;
        private float _coyoteTimeCounter;
        private float _jumpBufferCounter;
        private float _footstepTimer;
        private float _footstepInterval = 0.5f;

        // 速度加算システム
        private Vector3 _boostVelocity;
        private Vector3 _baseVelocity;
        private float _boostStartTime;
        private float _boostDuration;
        private SpeedBoostPattern _boostPattern;
        private bool _isSpeedBoostActive = false;

        /// <summary>
        /// 初期化処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Awake()
        {
            _rigidBody = GetComponent<Rigidbody>();
        }

        protected override void OnInitialized()
        {
            if (Settings.movement == null)
            {
                DebugLogError("MovementSettings が設定されていません");
                return;
            }

            //  currentMovementData = new MovementData(Time.time);
        }

        /// <summary>
        /// 物理更新処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FixedUpdate()
        {
            ApplyFinalMovement();
        }

        #region 基本移動アクション制御

        /// <summary>
        /// 停止する
        /// </summary>
        public void Stop()
        {
            _baseVelocity.Set(0, _baseVelocity.y, 0);
            _boostVelocity = Vector3.zero;
            _isSpeedBoostActive = false;

            NotifyObservers(ActionState.Idle);
        }

        /// <summary>
        /// 歩行移動を設定
        /// プレイヤーの基本的な移動を制御します
        /// 移動方向については有力を方向に変換する処理をプレイヤー側に入れる
        ///
        /// 他の移動に関しても言えるが、水平移動の入力で軌道制御を行う
        /// ブーストと移動だけが一瞬の加速ではない
        /// </summary>
        /// <param name="direction">移動方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveStart(Vector2 direction)
        {
            // 移動ベクトル設定
            SetBaseVelocity(direction * moveSetting.moveSpeed);

            // 移動開始
            NotifyObservers(ActionState.Moving);
        }

        /// <summary>
        /// 通常移動の方向を更新する
        /// 具体的には落下か歩行中に移動入力があった場合の処理
        /// キャラクターコントローラーが呼び出す
        /// </summary>
        /// <param name="direction">移動方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveUpdate(Vector2 direction)
        {

            // 移動ベクトル設定
            // SetBaseVelocity(direction * useSpeed);
        }

        /// <summary>
        /// ジャンプを実行
        /// チャージジャンプ廃止
        /// 入力がニュートラルなら真上に飛ぶ
        /// directionとboostSpeed分の真上ベクトルをブレンドして飛ぶ
        ///
        /// ジャンプボタン離した段階で終わろう
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Jump(Vector3 direction)
        {
            // 移動開始
            AddSpeedBoost(direction * moveSetting.jumpForce, moveSetting.jumpTime, SpeedBoostPattern.Decay);

            NotifyObservers(ActionState.Jumping);
        }

        /// <summary>
        /// ブーストを設定
        /// エネルギーを消費して高速移動を実行します
        ///
        /// 入力がなければ向いてる方向に移動する
        /// ブースト中ジャンプボタンを押すと真上に飛んでいく
        /// しかし水平入力をするとその方向に角度がつく
        ///
        /// やっぱやめた。
        /// ジャンプみたいに力が加わるだけにしよう
        /// 飛び回って撃つゲームじゃないので
        /// </summary>
        /// <param name="direction">ブースト方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBoost(Vector2 direction)
        {
            // 移動ベクトル設定
            SetBaseVelocity(direction * moveSetting.boostSpeed);

            // 移動開始
            NotifyObservers(ActionState.Boosting);
        }

        /// <summary>
        /// 回避を実行
        /// 無敵フレーム付きの緊急回避を実行します
        /// ブースト中に回避するとブーストがキャンセルされる
        ///
        /// 時間終了したらステートを戻すためにUnitaskを使用
        /// </summary>
        /// <param name="direction">回避方向（空白時はバックステップ）</param>
        /// <returns>アクション中に別のステートに変わっていないか。偽なら変わってる</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask Dodge(Vector3 direction)
        {
            // 移動開始
            AddSpeedBoost(direction * moveSetting.dodgeSpeed, moveSetting.dodgeTime, SpeedBoostPattern.Decay);
            NotifyObservers(ActionState.Dodging);

            await UniTask.Delay(TimeSpan.FromSeconds(moveSetting.dodgeTime));

            if (characterController.CurrentState.state == ActionState.Dodging)
            {
                NotifyObservers(ActionState.Idle);
            }
        }


        /// <summary>
        /// 二段回避を実行
        /// 無敵フレーム付きの緊急回避を実行します
        ///
        /// 時間終了したらステートを戻すためにUnitaskを使用
        /// </summary>
        /// <param name="direction">回避方向（空白時はバックステップ）</param>
        /// <returns>アクション中に別のステートに変わっていないか。偽なら変わってる</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTaskVoid DoubleDodge(Vector3 direction)
        {

            // 移動開始
            AddSpeedBoost(direction * moveSetting.dodgeSpeed, moveSetting.dodgeTime * 1.8f, SpeedBoostPattern.Decay);
            NotifyObservers(ActionState.DoubleDodging);

            await UniTask.Delay(TimeSpan.FromSeconds(moveSetting.dodgeTime * 1.8f));

            if (characterController.CurrentState.state == ActionState.DoubleDodging)
            {
                NotifyObservers(ActionState.Idle);
            }
        }

        /// <summary>
        /// クイックターンを実行
        /// 即座に180度振り向きを実行します
        ///
        /// アニメーションだけでやろう
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QuickTurn()
        {
            Stop();
            // 移動開始
            NotifyObservers(ActionState.QuickTurn);
        }

        #endregion

        #region 戦闘関連移動アクション制御

        /// <summary>
        /// 攻撃の踏み込みを実行
        /// 攻撃時の前進動作を制御します
        /// </summary>
        /// <param name="direction">踏み込み方向</param>
        /// <param name="distance">踏み込み距離</param>
        /// <param name="speed">踏み込み速度</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteLunge(Vector3 direction, float distance, float speed)
        {
            // 移動開始
            Stop();
            AddSpeedBoost(direction * speed, distance / speed, SpeedBoostPattern.Mountain);
        }

        /// <summary>
        /// ノックバック（被弾時の吹き飛ばし）を適用
        /// 被弾時の強制的な押し戻し効果を実行します
        ///
        /// これに関しては時間を設定しない
        /// </summary>
        /// <param name="direction">吹き飛ばし方向</param>
        /// <param name="force">吹き飛ばし力</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ApplyKnockback(Vector3 direction, float force)
        {
            Stop();
            _rigidBody.AddForce(direction.normalized * force, ForceMode.VelocityChange);
        }

        #endregion

        #region 統合機能：速度加算システム

        /// <summary>
        /// 基礎速度にxz平面の移動速度を設定します
        /// 常に一定の速度を加算したい場合に使用します
        /// </summary>
        /// <param name="velocity"></param>
        private void SetBaseVelocity(Vector2 velocity)
        {
            _baseVelocity.Set(velocity.x, _baseVelocity.y, velocity.y);
        }

        /// <summary>
        /// 速度加算効果を開始（ダッシュエフェクト付き）
        /// 時間内で様々なパターンで速度変化をする
        ///
        /// 一定時間の加速的挙動に使用
        /// </summary>
        /// <param name="velocity">追加速度ベクトル</param>
        /// <param name="duration">持続時間</param>
        /// <param name="pattern">速度変化パターン</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddSpeedBoost(Vector3 velocity, float duration, SpeedBoostPattern pattern = SpeedBoostPattern.Mountain)
        {
            _boostVelocity = velocity;
            _boostDuration = duration;
            _boostStartTime = Time.time;
            _boostPattern = pattern;
            _isSpeedBoostActive = true;

            // ダッシュエフェクト再生
            if (_dashParticles != null)
            {
                _dashParticles.Play();
            }
        }

        /// <summary>
        /// 現在の追加速度を取得
        /// </summary>
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 GetCurrentAdditionalVelocity()
        {
            if (!_isSpeedBoostActive)
                return Vector3.zero;

            float progress = (Time.time - _boostStartTime) / _boostDuration;
            if (progress >= 1f)
            {
                _isSpeedBoostActive = false;
                return Vector3.zero;
            }

            float curveValue = GetSpeedCurveValue(progress, _boostPattern);
            return _boostVelocity * curveValue;
        }

        /// <summary>
        /// パターンに応じた速度カーブ値を取得
        /// </summary>
        /// <param name="progress">進行度（0-1）</param>
        /// <param name="pattern">速度パターン</param>
        /// <returns>速度倍率（0-1）</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        private float GetSpeedCurveValue(float progress, SpeedBoostPattern pattern)
        {
            switch (pattern)
            {
                case SpeedBoostPattern.Mountain:
                    // 0→1→0の山型（Sin波）
                    return math.sin(progress * math.PI);

                case SpeedBoostPattern.Decay:
                    // 1→0の減衰（指数関数的）
                    return math.exp(-progress * 3f);

                case SpeedBoostPattern.Acceleration:
                    // 0→1の加速（二次関数）
                    return progress * progress;

                case SpeedBoostPattern.Constant:
                    // 一定速度
                    return 1f;

                case SpeedBoostPattern.SharpDecay:
                    // 1→0の急激な減衰（三次関数）
                    return math.pow(1f - progress, 3f);

                case SpeedBoostPattern.Elastic:
                    // 弾性効果（オーバーシュート後に安定）
                    if (progress < 0.5f)
                    {
                        // 前半：オーバーシュート
                        return math.sin(progress * 2f * math.PI) * 0.2f + 1f;
                    }
                    else
                    {
                        // 後半：安定化
                        float t = (progress - 0.5f) * 2f;
                        return math.lerp(1f, 0f, t * t);
                    }

                default:
                    return math.sin(progress * math.PI);
            }
        }

        /// <summary>
        /// 最終的な速度を反映
        /// 毎フレーム呼び出し、rigidbodyに最終速度を適用します
        /// </summary>
        private void ApplyFinalMovement()
        {
            _baseVelocity.y = SetVerticalVelocity();
            _rigidBody.linearVelocity = _baseVelocity + GetCurrentAdditionalVelocity();
        }

        #endregion

        #region 統合機能：重力・ジャンプシステム

        /// <summary>
        /// ジャンプ可能かチェック
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CanJump()
        {
            return _coyoteTimeCounter > 0f && _jumpBufferCounter > 0f;
        }

        /// <summary>
        /// 垂直速度の更新（重力適用）
        ///
        /// 常にy速度が0.5なのでそれ以上の場合に落下にしないとね
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float SetVerticalVelocity()
        {
            // 縦の追加速度がある場合は重力を切る
            if (_boostVelocity.y != 0)
            {
                _verticalVelocity = 0;
                return 0;
            }

            //if (currentMovementData.isGrounded && _verticalVelocity <= 0)
            //{
            //    return -0.5f; // 地面に軽く押し付ける
            //}
            //else
            //{
            //    // 重力適用
            //    _verticalVelocity += _gravity * Time.fixedDeltaTime;
            //    // 最大落下速度制限
            //    return Mathf.Max(_gravity * Time.fixedDeltaTime, _maxFallSpeed);
            //}
            return 0;
        }

        #endregion

        #region 統合機能：速度制限

        /// <summary>
        /// 速度制限を適用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplySpeedLimits(ref Vector3 velocity)
        {
            float horizontalSpeed = velocity.magnitude;

            if (horizontalSpeed > _maxHorizontalSpeed)
            {
                // 最大速度制限
                velocity = velocity.normalized * _maxHorizontalSpeed;
            }
            else if (horizontalSpeed > 0 && horizontalSpeed < _minHorizontalSpeed)
            {
                // 最小速度制限（微小な動きを無効化）
                velocity = Vector3.zero;
            }
        }

        #endregion
    }
}
