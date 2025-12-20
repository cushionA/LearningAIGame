using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LearningAIGame.CombatSystem.Singleton;

namespace LearningAIGame.UI.Title
{
    /// <summary>
    /// タイトル画面UIコントローラー
    /// GameManagerの既存メソッドを直接使用してボタンイベントを登録する
    /// </summary>
    public class TitleScreenUIController : MonoBehaviour
    {
        #region Inspector設定

        [Header("=== UI References ===")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundImage;

        [Header("--- Title ---")]
        [SerializeField] private RectTransform _titleRoot;
        [SerializeField] private TextMeshProUGUI _mainTitle;
        [SerializeField] private TextMeshProUGUI _subTitle;

        [Header("--- Buttons ---")]
        [SerializeField] private RectTransform _buttonRoot;
        [SerializeField] private Button _startButton;
        [SerializeField] private TextMeshProUGUI _startButtonText;
        [SerializeField] private Button _exitButton;
        [SerializeField] private TextMeshProUGUI _exitButtonText;

        [Header("=== Title Settings ===")]
        [SerializeField] private string _mainTitleText = "AIわからせバトル";
        [SerializeField] private string _subTitleText = "～職を取り戻せ～";
        [SerializeField] private string _startButtonLabel = "ゲーム開始";
        [SerializeField] private string _exitButtonLabel = "終了";

        [Header("=== Color Settings ===")]
        [SerializeField] private Color _mainTitleTopColor = new Color(1f, 0.88f, 0.4f);
        [SerializeField] private Color _mainTitleBottomColor = new Color(1f, 0.27f, 0.27f);
        [SerializeField] private Color _subTitleColor = new Color(0.49f, 0.83f, 0.99f);
        [SerializeField] private Color _startButtonColor = new Color(1f, 0.42f, 0.21f);
        [SerializeField] private Color _startButtonHoverColor = new Color(1f, 0.27f, 0.27f);
        [SerializeField] private Color _exitButtonColor = new Color(0.4f, 0.4f, 0.5f);
        [SerializeField] private Color _exitButtonHoverColor = new Color(0.6f, 0.6f, 0.7f);

        [Header("=== Font Size Settings ===")]
        [SerializeField] private float _mainTitleFontSize = 100f;
        [SerializeField] private float _subTitleFontSize = 40f;
        [SerializeField] private float _buttonFontSize = 36f;

        [Header("=== Animation Settings ===")]
        [SerializeField] private float _fadeInDuration = 0.5f;
        [SerializeField] private float _titleEnterDelay = 0.3f;
        [SerializeField] private float _subTitleEnterDelay = 0.5f;
        [SerializeField] private float _buttonEnterDelay = 0.8f;
        [SerializeField] private float _elementEnterDuration = 0.6f;

        [Header("=== Audio ===")]
        [SerializeField] private AudioSource _bgmAudioSource;
        [SerializeField] private AudioSource _seAudioSource;
        [SerializeField] private AudioClip _titleBGM;
        [SerializeField] private AudioClip _titleAppearSE;
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _seVolume = 1f;
        [SerializeField] private float _bgmFadeInDuration = 1.5f;

        #endregion

        #region Private Fields

        private CancellationTokenSource _animationCts;
        private Image _startButtonImage;
        private Image _exitButtonImage;
        private Color _startButtonOriginalColor;
        private Color _exitButtonOriginalColor;
        private Vector2 _titleOriginalPosition;
        private Vector2 _buttonOriginalPosition;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            CacheOriginalPositions();
            InitializeUI();
            SetupButtonEvents();
        }

        /// <summary>
        /// 開始処理
        /// </summary>
        private void Start()
        {
            ShowAsync().Forget();
        }

        /// <summary>
        /// 破棄時処理
        /// </summary>
        private void OnDestroy()
        {
            CancelAnimation();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 元の位置をキャッシュ
        /// </summary>
        private void CacheOriginalPositions()
        {
            _titleOriginalPosition = _titleRoot.anchoredPosition;
            _buttonOriginalPosition = _buttonRoot.anchoredPosition;
        }

        /// <summary>
        /// UIを初期化
        /// </summary>
        private void InitializeUI()
        {
            _canvasGroup.alpha = 0f;

            SetupMainTitle();
            SetupSubTitle();
            SetupButtonVisuals();

            _titleRoot.anchoredPosition = _titleOriginalPosition + new Vector2(0f, -30f);
            _buttonRoot.anchoredPosition = _buttonOriginalPosition + new Vector2(0f, 30f);
        }

        /// <summary>
        /// メインタイトルの設定
        /// </summary>
        private void SetupMainTitle()
        {
            _mainTitle.text = _mainTitleText;
            _mainTitle.fontSize = _mainTitleFontSize;
            _mainTitle.fontStyle = FontStyles.Bold;
            _mainTitle.enableVertexGradient = true;
            _mainTitle.colorGradient = new VertexGradient(
                _mainTitleTopColor,
                _mainTitleTopColor,
                _mainTitleBottomColor,
                _mainTitleBottomColor
            );
        }

        /// <summary>
        /// サブタイトルの設定
        /// </summary>
        private void SetupSubTitle()
        {
            _subTitle.text = _subTitleText;
            _subTitle.fontSize = _subTitleFontSize;
            _subTitle.color = _subTitleColor;
        }

        /// <summary>
        /// ボタンのビジュアル設定
        /// </summary>
        private void SetupButtonVisuals()
        {
            _startButtonText.text = _startButtonLabel;
            _startButtonText.fontSize = _buttonFontSize;
            _startButtonImage = _startButton.GetComponent<Image>();
            if (_startButtonImage != null)
            {
                _startButtonImage.color = _startButtonColor;
                _startButtonOriginalColor = _startButtonColor;
            }

            _exitButtonText.text = _exitButtonLabel;
            _exitButtonText.fontSize = _buttonFontSize;
            _exitButtonImage = _exitButton.GetComponent<Image>();
            if (_exitButtonImage != null)
            {
                _exitButtonImage.color = _exitButtonColor;
                _exitButtonOriginalColor = _exitButtonColor;
            }
        }

        /// <summary>
        /// ボタンイベントを設定
        /// GameManagerの既存メソッドを直接使用
        /// </summary>
        private void SetupButtonEvents()
        {
            if (!GameManager.HasInstance)
            {
                Debug.LogError("[TitleScreenUI] GameManagerが存在しません。BaseSceneを先にロードしてください。");
                return;
            }

            var gameManager = GameManager.Instance;

            // ボタンクリックイベント登録
            _startButton.onClick.AddListener(gameManager.StartGame);
            _exitButton.onClick.AddListener(gameManager.ExitGame);

            // ホバーエフェクト設定
            SetupButtonHoverEffect(_startButton, _startButtonImage, _startButtonOriginalColor, _startButtonHoverColor);
            SetupButtonHoverEffect(_exitButton, _exitButtonImage, _exitButtonOriginalColor, _exitButtonHoverColor);

            Debug.Log("[TitleScreenUI] GameManagerにボタンイベントを登録しました");
        }

        /// <summary>
        /// ボタンホバーエフェクトを設定
        /// </summary>
        /// <param name="button">対象のボタン</param>
        /// <param name="buttonImage">ボタンのImage</param>
        /// <param name="normalColor">通常時の色</param>
        /// <param name="hoverColor">ホバー時の色</param>
        /// <summary>
        /// ボタンホバーエフェクトを設定
        /// </summary>
        /// <param name="button">対象のボタン</param>
        /// <param name="buttonImage">ボタンのImage</param>
        /// <param name="normalColor">通常時の色</param>
        /// <param name="hoverColor">ホバー時の色</param>
        private void SetupButtonHoverEffect(Button button, Image buttonImage, Color normalColor, Color hoverColor)
        {
            if (button == null || buttonImage == null)
                return;

            var eventTrigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            // Pointer Enter
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            enterEntry.callback.AddListener(_ =>
            {
                // ボタンやImageが破棄されていないかチェック
                if (button == null || buttonImage == null)
                    return;

                if (GameManager.HasInstance)
                {
                    GameManager.Instance.PlayButtonHoverSE();
                }

                LMotion.Create(buttonImage.color, hoverColor, 0.15f)
                    .WithEase(Ease.OutQuad)
                    .Bind(c =>
                    {
                        if (buttonImage != null)
                            buttonImage.color = c;
                    });

                LMotion.Create(button.transform.localScale, Vector3.one * 1.05f, 0.15f)
                    .WithEase(Ease.OutQuad)
                    .Bind(s =>
                    {
                        if (button != null)
                            button.transform.localScale = s;
                    });
            });
            eventTrigger.triggers.Add(enterEntry);

            // Pointer Exit
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener(_ =>
            {
                // ボタンやImageが破棄されていないかチェック
                if (button == null || buttonImage == null)
                    return;

                LMotion.Create(buttonImage.color, normalColor, 0.15f)
                    .WithEase(Ease.OutQuad)
                    .Bind(c =>
                    {
                        if (buttonImage != null)
                            buttonImage.color = c;
                    });

                LMotion.Create(button.transform.localScale, Vector3.one, 0.15f)
                    .WithEase(Ease.OutQuad)
                    .Bind(s =>
                    {
                        if (button != null)
                            button.transform.localScale = s;
                    });
            });
            eventTrigger.triggers.Add(exitEntry);
        }

        #endregion

        #region Public API - 表示制御

        /// <summary>
        /// タイトル画面を表示（アニメーション付き）
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async UniTask ShowAsync(CancellationToken cancellationToken = default)
        {
            CancelAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            gameObject.SetActive(true);

            _titleRoot.anchoredPosition = _titleOriginalPosition + new Vector2(0f, -30f);
            _buttonRoot.anchoredPosition = _buttonOriginalPosition + new Vector2(0f, 30f);

            StartBGM();

            try
            {
                await LMotion.Create(0f, 1f, _fadeInDuration)
                    .WithEase(Ease.OutQuad)
                    .Bind(a => _canvasGroup.alpha = a)
                    .ToUniTask(_animationCts.Token);

                await UniTask.Delay(TimeSpan.FromSeconds(_titleEnterDelay), cancellationToken: _animationCts.Token);

                PlayTitleAppearSE();

                await LMotion.Create(_titleRoot.anchoredPosition, _titleOriginalPosition, _elementEnterDuration)
                    .WithEase(Ease.OutBack)
                    .Bind(p => _titleRoot.anchoredPosition = p)
                    .ToUniTask(_animationCts.Token);

                await UniTask.Delay(TimeSpan.FromSeconds(_buttonEnterDelay - _titleEnterDelay - _elementEnterDuration), cancellationToken: _animationCts.Token);

                await LMotion.Create(_buttonRoot.anchoredPosition, _buttonOriginalPosition, _elementEnterDuration)
                    .WithEase(Ease.OutBack)
                    .Bind(p => _buttonRoot.anchoredPosition = p)
                    .ToUniTask(_animationCts.Token);
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は静かに終了
            }
        }

        /// <summary>
        /// タイトル画面を非表示（アニメーション付き）
        /// </summary>
        /// <param name="cancellationToken">キャンセルトークン</param>
        public async UniTask HideAsync(CancellationToken cancellationToken = default)
        {
            CancelAnimation();
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            StopBGM();

            try
            {
                await LMotion.Create(1f, 0f, _fadeInDuration)
                    .WithEase(Ease.InQuad)
                    .Bind(a => _canvasGroup.alpha = a)
                    .ToUniTask(_animationCts.Token);

                gameObject.SetActive(false);
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は静かに終了
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
            _titleRoot.anchoredPosition = _titleOriginalPosition;
            _buttonRoot.anchoredPosition = _buttonOriginalPosition;
        }

        /// <summary>
        /// 即座に非表示
        /// </summary>
        public void HideImmediate()
        {
            CancelAnimation();
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// アニメーションをキャンセル
        /// </summary>
        private void CancelAnimation()
        {
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;
        }

        #endregion

        #region Private Methods - Audio

        /// <summary>
        /// BGM開始（フェードイン）
        /// </summary>
        private void StartBGM()
        {
            if (_bgmAudioSource == null || _titleBGM == null)
                return;

            _bgmAudioSource.clip = _titleBGM;
            _bgmAudioSource.volume = 0f;
            _bgmAudioSource.loop = true;
            _bgmAudioSource.Play();

            LMotion.Create(0f, _bgmVolume, _bgmFadeInDuration)
                .WithEase(Ease.OutQuad)
                .Bind(v => _bgmAudioSource.volume = v);
        }

        /// <summary>
        /// BGM停止（フェードアウト）
        /// </summary>
        private void StopBGM()
        {
            if (_bgmAudioSource == null || !_bgmAudioSource.isPlaying)
                return;

            LMotion.Create(_bgmAudioSource.volume, 0f, _fadeInDuration)
                .WithEase(Ease.InQuad)
                .WithOnComplete(() => _bgmAudioSource.Stop())
                .Bind(v => _bgmAudioSource.volume = v);
        }

        /// <summary>
        /// タイトル登場SE再生
        /// </summary>
        private void PlayTitleAppearSE()
        {
            if (_seAudioSource == null || _titleAppearSE == null)
                return;
            _seAudioSource.PlayOneShot(_titleAppearSE, _seVolume);
        }

        #endregion

        #region Test Methods (Editor Only)

        [Button("Test: Show Title")]
        private void TestShow()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TitleScreenUI] Play Mode でのみテスト可能です");
                return;
            }

            ShowAsync().Forget();
        }

        [Button("Test: Hide Title")]
        private void TestHide()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TitleScreenUI] Play Mode でのみテスト可能です");
                return;
            }
            HideAsync().Forget();
        }

        #endregion
    }
}