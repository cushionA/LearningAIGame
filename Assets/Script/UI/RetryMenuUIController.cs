using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LearningAIGame.UI.Common
{
    /// <summary>
    /// リトライ/タイトルへ戻るメニューUIコントローラー
    /// BaseSceneに配置し、敗北時やリザルト画面で表示する
    /// </summary>
    public class RetryMenuUIController : MonoBehaviour
    {
        #region Inspector設定

        [Header("=== UI References ===")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private Image _backgroundImage;

        [Header("--- Buttons ---")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private TextMeshProUGUI _retryButtonText;
        [SerializeField] private Button _titleButton;
        [SerializeField] private TextMeshProUGUI _titleButtonText;

        [Header("=== Label Settings ===")]
        [SerializeField] private string _retryLabel = "リトライ";
        [SerializeField] private string _titleLabel = "タイトルへ";

        [Header("=== Color Settings ===")]
        [SerializeField] private Color _panelBackgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        [SerializeField] private Color _retryButtonColor = new Color(0.2f, 0.6f, 0.9f);
        [SerializeField] private Color _retryButtonHoverColor = new Color(0.3f, 0.7f, 1f);
        [SerializeField] private Color _titleButtonColor = new Color(0.4f, 0.4f, 0.5f);
        [SerializeField] private Color _titleButtonHoverColor = new Color(0.5f, 0.5f, 0.6f);

        [Header("=== Animation Settings ===")]
        [SerializeField] private float _showDuration = 0.3f;
        [SerializeField] private float _hideDuration = 0.2f;

        [Header("=== GameManager連携 ===")]
        [SerializeField] private bool _autoRegisterToGameManager = true;
        [SerializeField] private bool _useRetryFromBeginning = false;

        #endregion

        #region Private Fields

        private CancellationTokenSource _animationCts;
        private Image _retryButtonImage;
        private Image _titleButtonImage;
        private Vector2 _panelOriginalPosition;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheReferences();
            InitializeUI();
            SetupButtonEvents();

            // 初期状態は非表示
            HideImmediate();
        }

        private void OnDestroy()
        {
            CancelAnimation();
        }

        #endregion

        #region Initialization

        private void CacheReferences()
        {
            _panelOriginalPosition = _panelRoot.anchoredPosition;
            _retryButtonImage = _retryButton.GetComponent<Image>();
            _titleButtonImage = _titleButton.GetComponent<Image>();
        }

        private void InitializeUI()
        {
            // テキスト設定
            _retryButtonText.text = _retryLabel;
            _titleButtonText.text = _titleLabel;

            // 色設定
            if (_backgroundImage != null)
            {
                _backgroundImage.color = _panelBackgroundColor;
            }
            if (_retryButtonImage != null)
            {
                _retryButtonImage.color = _retryButtonColor;
            }
            if (_titleButtonImage != null)
            {
                _titleButtonImage.color = _titleButtonColor;
            }
        }

        private void SetupButtonEvents()
        {
            if (_autoRegisterToGameManager)
            {
                if (CombatSystem.Singleton.GameManager.HasInstance)
                {
                    var gm = CombatSystem.Singleton.GameManager.Instance;

                    _retryButton.onClick.AddListener(() =>
                    {
                        // SE再生はGameManager側で行う
                        if (_useRetryFromBeginning)
                        {
                            gm.RetryFromBeginning();
                        }
                        else
                        {
                            gm.RetryCurrentBattle();
                        }
                    });

                    _titleButton.onClick.AddListener(() =>
                    {
                        // SE再生はGameManager側で行う
                        gm.ReturnToTitle();
                    });

                    Debug.Log("[RetryMenuUI] GameManagerにボタンイベントを登録しました");
                }
                else
                {
                    Debug.LogWarning("[RetryMenuUI] GameManagerが存在しません");
                }
            }

            // ホバーエフェクト
            SetupButtonHoverEffect(_retryButton, _retryButtonImage, _retryButtonColor, _retryButtonHoverColor);
            SetupButtonHoverEffect(_titleButton, _titleButtonImage, _titleButtonColor, _titleButtonHoverColor);
        }

        private void SetupButtonHoverEffect(Button button, Image buttonImage, Color normalColor, Color hoverColor)
        {
            if (buttonImage == null)
                return;

            var eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            // Pointer Enter
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            enterEntry.callback.AddListener(_ =>
            {
                // ホバー音
                if (CombatSystem.Singleton.GameManager.HasInstance)
                {
                    CombatSystem.Singleton.GameManager.Instance.PlayButtonHoverSE();
                }

                LMotion.Create(buttonImage.color, hoverColor, 0.1f)
                    .WithEase(Ease.OutQuad)
                    .Bind(c => buttonImage.color = c);

                LMotion.Create(button.transform.localScale, Vector3.one * 1.05f, 0.1f)
                    .WithEase(Ease.OutQuad)
                    .Bind(s => button.transform.localScale = s);
            });
            eventTrigger.triggers.Add(enterEntry);

            // Pointer Exit
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener(_ =>
            {
                LMotion.Create(buttonImage.color, normalColor, 0.1f)
                    .WithEase(Ease.OutQuad)
                    .Bind(c => buttonImage.color = c);

                LMotion.Create(button.transform.localScale, Vector3.one, 0.1f)
                    .WithEase(Ease.OutQuad)
                    .Bind(s => button.transform.localScale = s);
            });
            eventTrigger.triggers.Add(exitEntry);
        }

        #endregion

        #region Public API

        /// <summary>
        /// メニューを表示（アニメーション付き）
        /// </summary>
        public async UniTask ShowAsync(CancellationToken cancellationToken = default)
        {
            CancelAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            gameObject.SetActive(true);
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            // 初期位置（下からスライドイン）
            _panelRoot.anchoredPosition = _panelOriginalPosition + new Vector2(0f, -50f);

            try
            {
                // フェードイン + スライドイン
                await UniTask.WhenAll(
                    LMotion.Create(0f, 1f, _showDuration)
                        .WithEase(Ease.OutQuad)
                        .Bind(a => _canvasGroup.alpha = a)
                        .ToUniTask(_animationCts.Token),
                    LMotion.Create(_panelRoot.anchoredPosition, _panelOriginalPosition, _showDuration)
                        .WithEase(Ease.OutBack)
                        .Bind(p => _panelRoot.anchoredPosition = p)
                        .ToUniTask(_animationCts.Token)
                );

                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            catch (OperationCanceledException)
            {
                // キャンセル時
            }
        }

        /// <summary>
        /// メニューを非表示（アニメーション付き）
        /// </summary>
        public async UniTask HideAsync(CancellationToken cancellationToken = default)
        {
            CancelAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            try
            {
                await LMotion.Create(1f, 0f, _hideDuration)
                    .WithEase(Ease.InQuad)
                    .Bind(a => _canvasGroup.alpha = a)
                    .ToUniTask(_animationCts.Token);

                gameObject.SetActive(false);
            }
            catch (OperationCanceledException)
            {
                // キャンセル時
            }
        }

        /// <summary>
        /// 即座に表示
        /// </summary>
        public void ShowImmediate()
        {
            CancelAnimation();
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _panelRoot.anchoredPosition = _panelOriginalPosition;
        }

        /// <summary>
        /// 即座に非表示
        /// </summary>
        public void HideImmediate()
        {
            CancelAnimation();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// リトライボタンのラベルを変更
        /// </summary>
        public void SetRetryLabel(string label)
        {
            _retryLabel = label;
            _retryButtonText.text = label;
        }

        /// <summary>
        /// リトライモードを切り替え（現在のバトル or 最初から）
        /// </summary>
        public void SetRetryFromBeginning(bool fromBeginning)
        {
            _useRetryFromBeginning = fromBeginning;
        }

        #endregion

        #region Private Methods

        private void CancelAnimation()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;
        }

        #endregion

        #region Test Methods

        [Button("Test: Show")]
        private void TestShow()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[RetryMenuUI] Play Mode でのみテスト可能です");
                return;
            }
            ShowAsync().Forget();
        }

        [Button("Test: Hide")]
        private void TestHide()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[RetryMenuUI] Play Mode でのみテスト可能です");
                return;
            }
            HideAsync().Forget();
        }

        #endregion
    }
}