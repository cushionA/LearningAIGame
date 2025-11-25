using LearningAIGame.CombatSystem.Core;
using LearningAIGame.CombatSystem.Data;
using LearningAIGame.CombatSystem.Systems;
using LitMotion;
using LitMotion.Extensions;
using NaughtyAttributes;
using R3;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static LearningAIGame.CombatSystem.Core.StateSystem;

namespace LearningAIGame.UI
{
    /// <summary>
    /// 3方向インジケーターUIの管理（R3 + LitMotion）
    /// 自分の攻撃検知、ガード成功、構え方向などをリアクティブに表示
    /// </summary>
    public class DirectionIndicatorUI : MonoBehaviour
    {
        #region Inspector設定

        [Header("各方向のSpriteRenderer")]
        [Required]
        [SerializeField] private SpriteRenderer _upIndicator;

        [Required]
        [SerializeField] private SpriteRenderer _leftIndicator;

        [Required]
        [SerializeField] private SpriteRenderer _rightIndicator;

        [Header("色設定")]
        [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0.4f);              // 半透明白
        [SerializeField] private Color _activeColor = new Color(1f, 1f, 1f, 1f);                // 白（構え方向）
        [SerializeField] private Color _enemyAttackWarningColor = new Color(1f, 0.5f, 0f, 1f);  // オレンジ（攻撃開始警告）
        [SerializeField] private Color _enemyAttackHitColor = new Color(1f, 0.2f, 0.2f, 1f);    // 赤（攻撃判定発生）
        [SerializeField] private Color _guardSuccessColor = new Color(0.3f, 0.6f, 1f, 1f);      // 青（成功）
        [SerializeField] private Color _highlightColor = new Color(1.5f, 1.5f, 1.5f, 1f);       // 白ハイライト

        [Header("アニメーション設定")]
        [SerializeField] private float _stanceScale = 1.15f;                               // 構え方向の通常時スケール
        [SerializeField] private float _highlightScale = 1.3f;                             // ハイライト時の拡大率
        [SerializeField] private float _highlightDuration = 0.15f;                         // ハイライト持続時間
        [SerializeField] private float _scaleBackDuration = 0.2f;                          // 縮小時間
        [SerializeField] private Ease _highlightEase = Ease.OutBack;                       // イージング
        [SerializeField] private float _scaleTransitionDuration = 0.2f;                    // スケール変更時の遷移時間

        [Header("ガード方向変更時のスケール")]
        [SerializeField] private float _activeStanceScale = 1.15f;                         // 現在の構え方向の拡大率
        [SerializeField] private float _stanceChangeDuration = 0.15f;                      // スケール変更時間

        #endregion

        #region 内部データ

        /// <summary>
        /// 各方向の現在の状態
        /// </summary>
        private IndicatorState _upState = IndicatorState.Normal;
        private IndicatorState _leftState = IndicatorState.Normal;
        private IndicatorState _rightState = IndicatorState.Normal;

        /// <summary>
        /// 現在の構え方向
        /// </summary>
        private StanceType _currentStanceDirection = StanceType.Up;

        /// <summary>
        /// ブロッキング受付中かどうか
        /// </summary>
        private bool _wasInBlockingWindow = false;

        /// <summary>
        /// R3の購読を管理
        /// </summary>
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        /// <summary>
        /// アニメーション用のMotionHandle
        /// </summary>
        private MotionHandle _upScaleHandle;
        private MotionHandle _leftScaleHandle;
        private MotionHandle _rightScaleHandle;

        /// <summary>
        /// ベーススケール遷移用のMotionHandle
        /// </summary>
        private MotionHandle _upBaseScaleHandle;
        private MotionHandle _leftBaseScaleHandle;
        private MotionHandle _rightBaseScaleHandle;

        #endregion

        #region 初期化・破棄

        /// <summary>
        /// 初期化
        /// </summary>
        private void Start()
        {
            // 初期状態を設定
            ResetAll();

            StateSystem myStateSystem = GetComponentInParent<StateSystem>();
            AttackSystem myAttackSystem = GetComponentInParent<AttackSystem>();

            Setup(myStateSystem, myAttackSystem);
        }

        /// <summary>
        /// 破棄時にR3の購読を解除
        /// </summary>
        private void OnDestroy()
        {
            _disposables.Dispose();

            // ハイライトアニメーションをキャンセル
            if (_upScaleHandle.IsActive())
                _upScaleHandle.Cancel();
            if (_leftScaleHandle.IsActive())
                _leftScaleHandle.Cancel();
            if (_rightScaleHandle.IsActive())
                _rightScaleHandle.Cancel();

            // ベーススケールアニメーションをキャンセル
            if (_upBaseScaleHandle.IsActive())
                _upBaseScaleHandle.Cancel();
            if (_leftBaseScaleHandle.IsActive())
                _leftBaseScaleHandle.Cancel();
            if (_rightBaseScaleHandle.IsActive())
                _rightBaseScaleHandle.Cancel();
        }

        #endregion

        #region セットアップ（外部から呼び出し）

        /// <summary>
        /// 各システムと接続してリアクティブに購読開始
        /// </summary>
        /// <param name="stateSystem">状態管理システム</param>
        /// <param name="opponentAttackSystem">自分の攻撃システム</param>
        public void Setup(
            StateSystem stateSystem,
            AttackSystem opponentAttackSystem)
        {
            // 既存の購読をクリア
            _disposables.Clear();

            // 1. 構え方向の変更を購読（ReactiveProperty）
            if (stateSystem != null)
            {
                stateSystem.CurrentStance
                    .Subscribe(newStance => OnStanceDirectionChanged(newStance))
                    .AddTo(_disposables);

                // 2. 防御成功状態を監視（ガード成功・ブロッキング成功）
                stateSystem.CurrentState
                    .Subscribe(newState => OnDefenseStateChanged(newState, stateSystem.CurrentStance.CurrentValue))
                    .AddTo(_disposables);
            }

            // 3. 自分の攻撃システムを購読
            if (opponentAttackSystem != null)
            {
                opponentAttackSystem.Observable
                    .Subscribe(attackReport => OnEnemyAttack(attackReport))
                    .AddTo(_disposables);
            }
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// 構え方向が変更された
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnStanceDirectionChanged(StanceType newDirection)
        {
            Debug.Log($"DirectionIndicatorUI: 構え方向が変更されました: {newDirection}");
            StanceType oldDirection = _currentStanceDirection;
            _currentStanceDirection = newDirection;

            // 前の構え方向を通常サイズに戻す
            if (oldDirection != newDirection)
            {
                UpdateIndicatorBaseScale(oldDirection, 1.0f);
            }

            // 新しい構え方向を大きくする
            UpdateIndicatorBaseScale(newDirection, _stanceScale);

            UpdateAllIndicators();
        }

        /// <summary>
        /// 防御状態が変更された（StateSystemのCurrentStateから）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnDefenseStateChanged(ActionState newState, StanceType currentStance)
        {
            Debug.Log($"DirectionIndicatorUI: 防御状態が変更されました: {newState} (構え方向: {currentStance})");

            // ガード成功
            if (newState == ActionState.ガード成功)
            {
                OnGuardSuccess(currentStance);
            }
            // ブロッキング成功
            else if (newState == ActionState.ブロッキング成功)
            {
                OnBlockSuccess(currentStance);
            }
            // ブロッキング中
            else if (newState == ActionState.ブロッキング)
            {
                if (!_wasInBlockingWindow)
                {
                    OnBlockingWindowStarted(currentStance);
                    _wasInBlockingWindow = true;
                }
            }
            // ブロッキング終了
            else
            {
                if (_wasInBlockingWindow)
                {
                    OnBlockingWindowEnded(currentStance);
                    _wasInBlockingWindow = false;
                }
            }
        }

        /// <summary>
        /// 自分の攻撃報告を処理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnEnemyAttack(AttackReportInfo attackReport)
        {
            Debug.Log($"DirectionIndicatorUI: 攻撃報告を受信: {attackReport.reportType} (方向: {attackReport.stance}, 遅延: {attackReport.hitDelayFrame}フレーム)");

            // 攻撃開始時
            if (attackReport.reportType == AttackReportType.WeakAttackStart ||
                attackReport.reportType == AttackReportType.HeavyAttackStart)
            {
                // 攻撃方向を警告表示
                OnEnemyAttackDetected(attackReport.stance);

                // hitDelayFrame分待ってから攻撃判定発生のハイライト
                if (attackReport.hitDelayFrame > 0)
                {
                    Observable.Timer(TimeSpan.FromSeconds(attackReport.hitDelayFrame * Time.fixedDeltaTime))
                        .Subscribe(_ => OnEnemyAttackHit(attackReport.stance))
                        .AddTo(_disposables);
                }
                else
                {
                    // 遅延がない場合は即座にハイライト
                    OnEnemyAttackHit(attackReport.stance);
                }
            }
        }

        /// <summary>
        /// 自分の攻撃方向を検知（オレンジ色警告）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnEnemyAttackDetected(StanceType direction)
        {
            SetIndicatorState(direction, IndicatorState.EnemyAttackWarning);
            UpdateIndicator(direction);

            // 一定時間後に通常状態に戻す（攻撃ヒットしなかった場合用）
            Observable.Timer(TimeSpan.FromSeconds(2.0f))
                .Subscribe(_ =>
                {
                    if (GetIndicatorState(direction) == IndicatorState.EnemyAttackWarning)
                    {
                        ResetIndicatorState(direction);
                        UpdateIndicator(direction);
                    }
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 自分の攻撃がヒットする瞬間（赤ハイライト・拡大）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnEnemyAttackHit(StanceType direction)
        {
            SetIndicatorState(direction, IndicatorState.EnemyAttackHit);
            UpdateIndicator(direction);
            PlayHighlightAnimation(direction, _enemyAttackHitColor);

            // 一定時間後に通常状態に戻す（警告状態ではなく通常に戻す）
            Observable.Timer(TimeSpan.FromSeconds(_highlightDuration + 0.3f))
                .Subscribe(_ =>
                {
                    if (GetIndicatorState(direction) == IndicatorState.EnemyAttackHit)
                    {
                        // 通常状態に戻す
                        ResetIndicatorState(direction);
                        UpdateIndicator(direction);
                    }
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// ブロッキング受付開始（白ハイライト・拡大）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnBlockingWindowStarted(StanceType direction)
        {
            SetIndicatorState(direction, IndicatorState.BlockingWindow);
            UpdateIndicator(direction);
            PlayHighlightAnimation(direction, _highlightColor);
        }

        /// <summary>
        /// ブロッキング受付終了
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnBlockingWindowEnded(StanceType direction)
        {
            // 通常状態に戻す
            if (GetIndicatorState(direction) == IndicatorState.BlockingWindow)
            {
                ResetIndicatorState(direction);
                UpdateIndicator(direction);
            }
        }

        /// <summary>
        /// ガード成功（青色）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnGuardSuccess(StanceType direction)
        {
            SetIndicatorState(direction, IndicatorState.GuardSuccess);
            UpdateIndicator(direction);

            // 0.5秒後に通常状態に戻す
            Observable.Timer(TimeSpan.FromSeconds(0.5f))
                .Subscribe(_ =>
                {
                    ResetIndicatorState(direction);
                    UpdateIndicator(direction);
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// ブロッキング成功（青色）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnBlockSuccess(StanceType direction)
        {
            SetIndicatorState(direction, IndicatorState.BlockSuccess);
            UpdateIndicator(direction);
            PlayHighlightAnimation(direction, _guardSuccessColor);

            // 0.5秒後に通常状態に戻す
            Observable.Timer(TimeSpan.FromSeconds(0.5f))
                .Subscribe(_ =>
                {
                    ResetIndicatorState(direction);
                    UpdateIndicator(direction);
                })
                .AddTo(_disposables);
        }

        #endregion

        #region 状態管理

        /// <summary>
        /// インジケーターの状態を設定
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetIndicatorState(StanceType direction, IndicatorState state)
        {
            switch (direction)
            {
                case StanceType.Up:
                    _upState = state;
                    break;
                case StanceType.Left:
                    _leftState = state;
                    break;
                case StanceType.Right:
                    _rightState = state;
                    break;
            }
        }

        /// <summary>
        /// インジケーターの状態を取得
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IndicatorState GetIndicatorState(StanceType direction)
        {
            return direction switch
            {
                StanceType.Up => _upState,
                StanceType.Left => _leftState,
                StanceType.Right => _rightState,
                _ => IndicatorState.Normal
            };
        }

        /// <summary>
        /// インジケーターの状態をリセット
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetIndicatorState(StanceType direction)
        {
            SetIndicatorState(direction, IndicatorState.Normal);
        }

        #endregion

        #region スケール管理

        /// <summary>
        /// インジケーターのベーススケールを変更
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateIndicatorBaseScale(StanceType direction, float targetScale)
        {
            Transform targetTransform = GetIndicatorRenderer(direction).transform;
            ref MotionHandle handle = ref GetBaseScaleMotionHandle(direction);

            // 既存のアニメーションをキャンセル
            if (handle.IsActive())
            {
                handle.Cancel();
            }

            // 現在のスケールから目標スケールへスムーズに遷移
            Vector3 currentScale = targetTransform.localScale;
            Vector3 targetScaleVec = Vector3.one * targetScale;

            handle = LMotion.Create(currentScale, targetScaleVec, _scaleTransitionDuration)
                .WithEase(Ease.OutQuad)
                .BindToLocalScale(targetTransform);
        }

        /// <summary>
        /// 方向に対応するベーススケール用MotionHandleを取得
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref MotionHandle GetBaseScaleMotionHandle(StanceType direction)
        {
            switch (direction)
            {
                case StanceType.Up:
                    return ref _upBaseScaleHandle;
                case StanceType.Left:
                    return ref _leftBaseScaleHandle;
                case StanceType.Right:
                    return ref _rightBaseScaleHandle;
                default:
                    return ref _upBaseScaleHandle;
            }
        }

        #endregion

        #region 表示更新

        /// <summary>
        /// すべてのインジケーターを更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateAllIndicators()
        {
            UpdateIndicator(StanceType.Up);
            UpdateIndicator(StanceType.Left);
            UpdateIndicator(StanceType.Right);
        }

        /// <summary>
        /// 指定方向のインジケーターを更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateIndicator(StanceType direction)
        {
            SpriteRenderer targetRenderer = GetIndicatorRenderer(direction);
            IndicatorState state = GetIndicatorState(direction);

            // 状態に応じた色を設定
            Color targetColor = GetColorForState(state, direction);
            targetRenderer.color = targetColor;
        }

        /// <summary>
        /// 状態に応じた色を取得
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Color GetColorForState(IndicatorState state, StanceType direction)
        {
            return state switch
            {
                IndicatorState.EnemyAttackWarning => _enemyAttackWarningColor,  // オレンジ（攻撃開始）
                IndicatorState.EnemyAttackHit => _enemyAttackHitColor,          // 赤（攻撃判定発生）
                IndicatorState.BlockingWindow => _highlightColor,
                IndicatorState.GuardSuccess => _guardSuccessColor,
                IndicatorState.BlockSuccess => _guardSuccessColor,
                IndicatorState.Normal => direction == _currentStanceDirection ? _activeColor : _normalColor,
                _ => _normalColor
            };
        }

        #endregion

        #region アニメーション

        /// <summary>
        /// ハイライトアニメーションを再生
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PlayHighlightAnimation(StanceType direction, Color highlightColor)
        {
            Transform targetTransform = GetIndicatorRenderer(direction).transform;
            ref MotionHandle handle = ref GetMotionHandle(direction);

            // 既存のアニメーションをキャンセル
            if (handle.IsActive())
            {
                handle.Cancel();
            }

            // 現在のスケールを取得（構え方向なら_stanceScale、それ以外は1.0）
            float baseScale = (direction == _currentStanceDirection) ? _stanceScale : 1.0f;
            Vector3 startScale = Vector3.one * baseScale;
            Vector3 highlightScaleVec = Vector3.one * baseScale * _highlightScale;

            // スケールアニメーション
            handle = LMotion.Create(startScale, highlightScaleVec, _highlightDuration)
                .WithEase(_highlightEase)
                .WithOnComplete(() =>
                {
                    // 元のベーススケールに戻す
                    LMotion.Create(highlightScaleVec, startScale, _scaleBackDuration)
                        .WithEase(Ease.OutQuad)
                        .BindToLocalScale(targetTransform);
                })
                .BindToLocalScale(targetTransform);
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// 方向に対応するSpriteRendererを取得
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SpriteRenderer GetIndicatorRenderer(StanceType direction)
        {
            return direction switch
            {
                StanceType.Up => _upIndicator,
                StanceType.Left => _leftIndicator,
                StanceType.Right => _rightIndicator,
                _ => _upIndicator
            };
        }

        /// <summary>
        /// 方向に対応するMotionHandleを取得
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref MotionHandle GetMotionHandle(StanceType direction)
        {
            switch (direction)
            {
                case StanceType.Up:
                    return ref _upScaleHandle;
                case StanceType.Left:
                    return ref _leftScaleHandle;
                case StanceType.Right:
                    return ref _rightScaleHandle;
                default:
                    return ref _upScaleHandle;
            }
        }

        /// <summary>
        /// すべてをリセット
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetAll()
        {
            _upState = IndicatorState.Normal;
            _leftState = IndicatorState.Normal;
            _rightState = IndicatorState.Normal;
            UpdateAllIndicators();
        }

        #endregion

        #region Odinデバッグボタン

        [Button("構え方向: 上")]
        private void DebugStanceUp()
        {
            OnStanceDirectionChanged(StanceType.Up);
        }

        [Button("構え方向: 左")]
        private void DebugStanceLeft()
        {
            OnStanceDirectionChanged(StanceType.Left);
        }

        [Button("敵攻撃警告: 上")]
        private void DebugEnemyAttackUp()
        {
            OnEnemyAttackDetected(StanceType.Up);
        }

        [Button("敵攻撃ヒット: 左")]
        private void DebugEnemyHitLeft()
        {
            OnEnemyAttackHit(StanceType.Left);
        }

        [Button("ブロッキング受付: 右")]
        private void DebugBlockingRight()
        {
            OnBlockingWindowStarted(StanceType.Right);
        }

        [Button("ガード成功: 上")]
        private void DebugGuardSuccessUp()
        {
            OnGuardSuccess(StanceType.Up);
        }

        [Button("ブロッキング成功: 左")]
        private void DebugBlockSuccessLeft()
        {
            OnBlockSuccess(StanceType.Left);
        }

        [Button("リセット")]
        private void DebugReset()
        {
            ResetAll();
        }

        #endregion

        #region 内部定義

        /// <summary>
        /// インジケーターの状態
        /// </summary>
        private enum IndicatorState
        {
            Normal,              // 通常（構え方向なら白、それ以外は半透明）
            EnemyAttackWarning,  // 自分の攻撃警告（赤）
            EnemyAttackHit,      // 自分の攻撃ヒット瞬間（赤ハイライト・拡大）
            BlockingWindow,      // ブロッキング受付中（白ハイライト・拡大）
            GuardSuccess,        // ガード成功（青）
            BlockSuccess         // ブロッキング成功（青・拡大）
        }

        #endregion
    }
}