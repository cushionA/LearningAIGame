using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LearningAIGame.UI.Battle
{
    /// <summary>
    /// ゲーム進行状態を示すアニメーションUI表示システム
    /// </summary>
    public class GameProgressUIController : MonoBehaviour
    {
        #region Inspector設定

        [Header("=== UI References ===")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private Image _blackoutImage;

        [Header("--- Round Display ---")]
        [SerializeField] private GameObject _roundDisplay;
        [SerializeField] private TextMeshProUGUI _roundLabel;
        [SerializeField] private TextMeshProUGUI _roundNumber;

        [Header("--- Fight Display ---")]
        [SerializeField] private GameObject _fightDisplay;
        [SerializeField] private TextMeshProUGUI _fightText;

        [Header("--- Result Display ---")]
        [SerializeField] private GameObject _resultDisplay;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _resultSubText;

        [Header("--- Game Set Display ---")]
        [SerializeField] private GameObject _gameSetDisplay;
        [SerializeField] private TextMeshProUGUI _gameSetText;
        [SerializeField] private Image _gameSetLine;

        [Header("=== Animation Settings ===")]
        [SerializeField] private float _enterDuration = 0.3f;
        [SerializeField] private float _holdDuration = 1.2f;
        [SerializeField] private float _exitDuration = 0.3f;
        [SerializeField] private float _blackoutDuration = 0.5f;
        [SerializeField] private Ease _enterEase = Ease.OutBack;
        [SerializeField] private Ease _exitEase = Ease.InQuad;

        [Header("=== Color Settings ===")]
        [SerializeField] private Color _roundLabelColor = new Color(0.9f, 0.85f, 0.7f);
        [SerializeField] private Color _roundNumberTopColor = new Color(1f, 0.95f, 0.8f);
        [SerializeField] private Color _roundNumberBottomColor = new Color(0.95f, 0.75f, 0.4f);
        [SerializeField] private Color _fightColor = new Color(1f, 0.75f, 0.15f);
        [SerializeField] private Color _winColor = new Color(0.38f, 0.65f, 0.98f);
        [SerializeField] private Color _loseColor = new Color(0.97f, 0.27f, 0.27f);
        [SerializeField] private Color _gameSetColor = new Color(0.65f, 0.55f, 0.98f);

        [Header("=== Font Size Settings ===")]
        [SerializeField] private float _roundLabelFontSize = 48f;
        [SerializeField] private float _roundNumberFontSize = 200f;
        [SerializeField] private float _fightFontSize = 140f;
        [SerializeField] private float _resultFontSize = 110f;
        [SerializeField] private float _resultSubFontSize = 36f;
        [SerializeField] private float _gameSetFontSize = 130f;

        [Header("=== Test Settings ===")]
        [SerializeField, Range(1, 5)] private int _testRoundNumber = 1;

        #endregion

        #region Private Fields

        private CancellationTokenSource _animationCts;
        private CancellationTokenSource _blackoutCts;
        private MotionHandle _currentMotion;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUI();
        }

        private void OnDestroy()
        {
            CancelCurrentAnimation();
            CancelBlackout();
        }

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            Debug.Log($"[GameProgressUI] InitializeUI 呼び出し - Scene: {gameObject.scene.name}");

            // 全表示を非表示に初期化
            _canvasGroup.alpha = 0f;
            _roundDisplay.SetActive(false);
            _fightDisplay.SetActive(false);
            _resultDisplay.SetActive(false);
            _gameSetDisplay.SetActive(false);

            // ブラックアウト画像を透明に
            if (_blackoutImage != null)
            {
                _blackoutImage.color = new Color(0f, 0f, 0f, 0f);
                _blackoutImage.raycastTarget = false;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// ラウンド表示アニメーション
        /// </summary>
        /// <param name="roundNumber">ラウンド番号（1-5）</param>
        public async UniTask ShowRoundAsync(int roundNumber, CancellationToken cancellationToken = default)
        {
            CancelCurrentAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            PrepareDisplay(_roundDisplay);

            // ラベル設定
            _roundLabel.text = "ROUND";
            _roundLabel.fontSize = _roundLabelFontSize;
            _roundLabel.color = _roundLabelColor;

            // 番号設定（グラデーション）
            _roundNumber.text = roundNumber.ToString();
            _roundNumber.fontSize = _roundNumberFontSize;
            _roundNumber.enableVertexGradient = true;
            _roundNumber.colorGradient = new VertexGradient(
                _roundNumberTopColor,
                _roundNumberTopColor,
                _roundNumberBottomColor,
                _roundNumberBottomColor
            );

            await PlayAnimationSequenceAsync(_animationCts.Token);
        }

        /// <summary>
        /// 戦闘開始表示アニメーション
        /// </summary>
        public async UniTask ShowFightAsync(CancellationToken cancellationToken = default)
        {
            CancelCurrentAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            PrepareDisplay(_fightDisplay);
            _fightText.text = "FIGHT!";
            _fightText.fontSize = _fightFontSize;
            _fightText.color = _fightColor;

            await PlayAnimationSequenceAsync(_animationCts.Token, enablePulse: true);
        }

        /// <summary>
        /// プレイヤー勝利表示アニメーション
        /// </summary>
        public async UniTask ShowPlayerWinAsync(CancellationToken cancellationToken = default)
        {
            CancelCurrentAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            PrepareDisplay(_resultDisplay);
            _resultText.text = "PLAYER WIN!";
            _resultText.fontSize = _resultFontSize;
            _resultText.color = _winColor;
            _resultSubText.text = "";
            _resultSubText.gameObject.SetActive(false);

            await PlayAnimationSequenceAsync(_animationCts.Token);
        }

        /// <summary>
        /// プレイヤー敗北表示アニメーション
        /// </summary>
        public async UniTask ShowPlayerLoseAsync(CancellationToken cancellationToken = default)
        {
            CancelCurrentAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            PrepareDisplay(_resultDisplay);
            _resultText.text = "PLAYER LOSE";
            _resultText.fontSize = _resultFontSize;
            _resultText.color = _loseColor;
            _resultSubText.text = "— DEFEATED —";
            _resultSubText.fontSize = _resultSubFontSize;
            _resultSubText.gameObject.SetActive(true);

            await PlayAnimationSequenceAsync(_animationCts.Token);
        }

        /// <summary>
        /// ゲーム終了表示アニメーション
        /// </summary>
        public async UniTask ShowGameSetAsync(CancellationToken cancellationToken = default)
        {
            CancelCurrentAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            PrepareDisplay(_gameSetDisplay);
            _gameSetText.text = "GAME SET!";
            _gameSetText.fontSize = _gameSetFontSize;
            _gameSetText.color = _gameSetColor;
            _gameSetLine.color = _gameSetColor;

            await PlayAnimationSequenceWithLineAsync(_animationCts.Token);
        }

        /// <summary>
        /// 即座に非表示
        /// </summary>
        public void HideImmediate()
        {
            CancelCurrentAnimation();
            _canvasGroup.alpha = 0f;
            HideAllDisplays();
        }

        /// <summary>
        /// ブラックアウト（画面を徐々に暗くする）
        /// </summary>
        public async UniTask BlackoutAsync(CancellationToken cancellationToken = default)
        {
            if (_blackoutImage == null)
                return;

            CancelBlackout();
            _blackoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _blackoutImage.raycastTarget = true;

            try
            {
                await LMotion.Create(0f, 1f, _blackoutDuration)
                    .WithEase(Ease.InOutQuad)
                    .Bind(a => _blackoutImage.color = new Color(0f, 0f, 0f, a))
                    .ToUniTask(_blackoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は静かに終了
            }
        }

        /// <summary>
        /// ブラックアウト解除（画面を徐々に明るくする）
        /// </summary>
        public async UniTask BlackoutReleaseAsync(CancellationToken cancellationToken = default)
        {
            if (_blackoutImage == null)
                return;

            CancelBlackout();
            _blackoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                await LMotion.Create(1f, 0f, _blackoutDuration)
                    .WithEase(Ease.InOutQuad)
                    .Bind(a => _blackoutImage.color = new Color(0f, 0f, 0f, a))
                    .ToUniTask(_blackoutCts.Token);

                _blackoutImage.raycastTarget = false;
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は静かに終了
            }
        }

        /// <summary>
        /// 即座にブラックアウト
        /// </summary>
        public void BlackoutImmediate()
        {
            Debug.Log($"[GameProgressUI] BlackoutImmediate - _blackoutImage: {_blackoutImage != null}");

            if (_blackoutImage == null)
            {
                Debug.LogError("[GameProgressUI] _blackoutImage が null です！");
                return;
            }

            // Canvas情報を出力
            var canvas = _blackoutImage.canvas;
            Debug.Log($"[GameProgressUI] Canvas: {canvas.name}, RenderMode: {canvas.renderMode}, SortOrder: {canvas.sortingOrder}");

            // RectTransform情報を出力
            var rect = _blackoutImage.rectTransform;
            Debug.Log($"[GameProgressUI] RectTransform - Size: {rect.rect.size}, Position: {rect.position}, AnchoredPosition: {rect.anchoredPosition}");
            Debug.Log($"[GameProgressUI] RectTransform - AnchorMin: {rect.anchorMin}, AnchorMax: {rect.anchorMax}");

            // Image情報を出力
            Debug.Log($"[GameProgressUI] Image - enabled: {_blackoutImage.enabled}, raycastTarget: {_blackoutImage.raycastTarget}");
            Debug.Log($"[GameProgressUI] GameObject - activeSelf: {_blackoutImage.gameObject.activeSelf}, activeInHierarchy: {_blackoutImage.gameObject.activeInHierarchy}");

            // 親Canvasの情報
            Debug.Log($"[GameProgressUI] Canvas GameObject - activeSelf: {canvas.gameObject.activeSelf}, activeInHierarchy: {canvas.gameObject.activeInHierarchy}");
            Debug.Log($"[GameProgressUI] Canvas - enabled: {canvas.enabled}");

            CancelBlackout();
            _blackoutImage.color = Color.black;
            _blackoutImage.raycastTarget = true;

            Debug.Log($"[GameProgressUI] BlackoutImmediate完了 - color: {_blackoutImage.color}");
        }

        /// <summary>
        /// 即座にブラックアウト解除
        /// </summary>
        public void BlackoutReleaseImmediate()
        {
            if (_blackoutImage == null)
                return;

            CancelBlackout();
            _blackoutImage.color = new Color(0f, 0f, 0f, 0f);
            _blackoutImage.raycastTarget = false;
        }

        #endregion

        #region Animation Implementation

        private void PrepareDisplay(GameObject targetDisplay)
        {
            HideAllDisplays();
            targetDisplay.SetActive(true);
            _contentRoot.localScale = Vector3.zero;
            _canvasGroup.alpha = 0f;
        }

        private void HideAllDisplays()
        {
            _roundDisplay.SetActive(false);
            _fightDisplay.SetActive(false);
            _resultDisplay.SetActive(false);
            _gameSetDisplay.SetActive(false);
        }

        private async UniTask PlayAnimationSequenceAsync(CancellationToken cancellationToken, bool enablePulse = false)
        {
            try
            {
                // Enter Animation
                await PlayEnterAnimationAsync(cancellationToken);

                // Hold with optional pulse
                if (enablePulse)
                {
                    await PlayPulseAnimationAsync(cancellationToken);
                }
                else
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(_holdDuration), cancellationToken: cancellationToken);
                }

                // Exit Animation
                await PlayExitAnimationAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は静かに終了
            }
            finally
            {
                HideAllDisplays();
            }
        }

        private async UniTask PlayAnimationSequenceWithLineAsync(CancellationToken cancellationToken)
        {
            try
            {
                // ラインを初期状態に
                var lineTransform = _gameSetLine.rectTransform;
                lineTransform.localScale = new Vector3(0f, 1f, 1f);
                _gameSetLine.color = new Color(_gameSetColor.r, _gameSetColor.g, _gameSetColor.b, 0f);

                // Enter Animation
                await PlayEnterAnimationAsync(cancellationToken);

                // Line Animation（並列実行、完了を待たない形で開始）
                var lineScaleTask = LMotion.Create(0f, 1f, 0.4f)
                    .WithEase(Ease.OutCubic)
                    .Bind(x => lineTransform.localScale = new Vector3(x, 1f, 1f))
                    .ToUniTask(cancellationToken);

                var lineAlphaTask = LMotion.Create(0f, 1f, 0.3f)
                    .Bind(a => _gameSetLine.color = new Color(_gameSetColor.r, _gameSetColor.g, _gameSetColor.b, a))
                    .ToUniTask(cancellationToken);

                // Lineアニメーションを待機しつつHold時間も消化
                await UniTask.WhenAll(lineScaleTask, lineAlphaTask);
                await UniTask.Delay(TimeSpan.FromSeconds(_holdDuration - 0.4f), cancellationToken: cancellationToken);

                // Exit Animation
                await PlayExitAnimationAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は静かに終了
            }
            finally
            {
                HideAllDisplays();
            }
        }

        private async UniTask PlayEnterAnimationAsync(CancellationToken cancellationToken)
        {
            // Scale: 0 → 1.2 → 1
            var scaleTask = LMotion.Create(Vector3.zero, Vector3.one * 1.2f, _enterDuration * 0.6f)
                .WithEase(_enterEase)
                .Bind(s => _contentRoot.localScale = s)
                .AddTo(_contentRoot)
                .ToUniTask(cancellationToken)
                .ContinueWith(async () =>
                {
                    await LMotion.Create(Vector3.one * 1.2f, Vector3.one, _enterDuration * 0.4f)
                        .WithEase(Ease.OutCubic)
                        .Bind(s => _contentRoot.localScale = s)
                        .AddTo(_contentRoot)
                        .ToUniTask(cancellationToken);
                });

            // Alpha: 0 → 1
            var alphaTask = LMotion.Create(0f, 1f, _enterDuration)
                .WithEase(Ease.OutQuad)
                .Bind(a => _canvasGroup.alpha = a)
                .AddTo(_canvasGroup)
                .ToUniTask(cancellationToken);

            await UniTask.WhenAll(scaleTask, alphaTask);
        }

        private async UniTask PlayPulseAnimationAsync(CancellationToken cancellationToken)
        {
            var pulseCount = Mathf.FloorToInt(_holdDuration / 0.5f);

            for (int i = 0; i < pulseCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await LMotion.Create(Vector3.one, Vector3.one * 1.05f, 0.25f)
                    .WithEase(Ease.OutQuad)
                    .Bind(s => _contentRoot.localScale = s)
                    .AddTo(_contentRoot)
                    .ToUniTask()
                    .AttachExternalCancellation(cancellationToken);

                await LMotion.Create(Vector3.one * 1.05f, Vector3.one, 0.25f)
                    .WithEase(Ease.InQuad)
                    .Bind(s => _contentRoot.localScale = s)
                    .AddTo(_contentRoot)
                    .ToUniTask()
                    .AttachExternalCancellation(cancellationToken);
            }
        }

        private async UniTask PlayExitAnimationAsync(CancellationToken cancellationToken)
        {
            // Scale: 1 → 0.8
            var scaleTask = LMotion.Create(Vector3.one, Vector3.one * 0.8f, _exitDuration)
                .WithEase(_exitEase)
                .Bind(s => _contentRoot.localScale = s)
                .AddTo(_contentRoot)
                .ToUniTask(cancellationToken);

            // Alpha: 1 → 0
            var alphaTask = LMotion.Create(1f, 0f, _exitDuration)
                .WithEase(Ease.InQuad)
                .Bind(a => _canvasGroup.alpha = a)
                .AddTo(_canvasGroup)
                .ToUniTask(cancellationToken);

            await UniTask.WhenAll(scaleTask, alphaTask);
        }

        private void CancelCurrentAnimation()
        {
            if (_animationCts != null)
            {
                _animationCts.Cancel();
                _animationCts.Dispose();
                _animationCts = null;
            }

            if (_currentMotion.IsActive())
            {
                _currentMotion.Cancel();
            }
        }

        private void CancelBlackout()
        {
            if (_blackoutCts != null)
            {
                _blackoutCts.Cancel();
                _blackoutCts.Dispose();
                _blackoutCts = null;
            }
        }

        #endregion

        #region Test Methods (Editor Only)

        /// <summary>
        /// ラウンド表示テスト
        /// </summary>
        [Button("Test: Round")]
        private void TestShowRound()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameProgressUI] Play Mode でのみテスト可能です");
                return;
            }
            ShowRoundAsync(_testRoundNumber).Forget();
        }

        /// <summary>
        /// Fight表示テスト
        /// </summary>
        [Button("Test: Fight!")]
        private void TestShowFight()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameProgressUI] Play Mode でのみテスト可能です");
                return;
            }
            ShowFightAsync().Forget();
        }

        /// <summary>
        /// プレイヤー勝利表示テスト
        /// </summary>
        [Button("Test: Player Win")]
        private void TestShowPlayerWin()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameProgressUI] Play Mode でのみテスト可能です");
                return;
            }
            ShowPlayerWinAsync().Forget();
        }

        /// <summary>
        /// プレイヤー敗北表示テスト
        /// </summary>
        [Button("Test: Player Lose")]
        private void TestShowPlayerLose()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameProgressUI] Play Mode でのみテスト可能です");
                return;
            }
            ShowPlayerLoseAsync().Forget();
        }

        /// <summary>
        /// ゲームセット表示テスト
        /// </summary>
        [Button("Test: Game Set")]
        private void TestShowGameSet()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameProgressUI] Play Mode でのみテスト可能です");
                return;
            }
            ShowGameSetAsync().Forget();
        }

        /// <summary>
        /// ブラックアウトテスト
        /// </summary>
        [Button("Test: Blackout")]
        private void TestBlackout()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameProgressUI] Play Mode でのみテスト可能です");
                return;
            }
            BlackoutAsync().Forget();
        }

        /// <summary>
        /// ブラックアウト解除テスト
        /// </summary>
        [Button("Test: Blackout Release")]
        private void TestBlackoutRelease()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameProgressUI] Play Mode でのみテスト可能です");
                return;
            }
            BlackoutReleaseAsync().Forget();
        }

        /// <summary>
        /// 全演出連続テスト（ラウンド開始〜終了まで）
        /// </summary>
        [Button("Test: Full Sequence")]
        private void TestFullSequence()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GameProgressUI] Play Mode でのみテスト可能です");
                return;
            }
            PlayFullSequenceAsync().Forget();
        }

        private async UniTask PlayFullSequenceAsync()
        {
            await BlackoutAsync();
            await UniTask.Delay(200);
            await BlackoutReleaseAsync();

            await ShowRoundAsync(_testRoundNumber);
            await UniTask.Delay(300);
            await ShowFightAsync();
            await UniTask.Delay(1000);
            await ShowPlayerWinAsync();
            await UniTask.Delay(500);
            await ShowGameSetAsync();
        }

        #endregion
    }
}