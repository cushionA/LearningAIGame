#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LearningAIGame.UI.Title.Editor
{
    /// <summary>
    /// タイトル画面UIをヒエラルキー上に自動生成するエディタースクリプト
    /// </summary>
    public static class TitleScreenUICreator
    {
        #region Menu Items

        [MenuItem("GameObject/UI/LearningAIGame/Title Screen UI", false, 11)]
        public static void CreateTitleScreenUI()
        {
            var root = CreateCanvasRoot();
            var background = CreateBackground(root.transform);
            var titleRoot = CreateTitleSection(root.transform);
            var buttonRoot = CreateButtonSection(root.transform);
            CreateFooter(root.transform);

            // コントローラーをアタッチして参照を設定
            SetupController(root, background, titleRoot, buttonRoot);

            // 選択状態にする
            Selection.activeGameObject = root;
            Undo.RegisterCreatedObjectUndo(root, "Create Title Screen UI");

            Debug.Log("[TitleScreenUICreator] Title Screen UI を作成しました");
        }

        #endregion

        #region Canvas Setup

        private static GameObject CreateCanvasRoot()
        {
            var canvasObj = new GameObject("TitleScreenUI");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = 50;

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                canvas.worldCamera = mainCamera;
                canvas.planeDistance = 1f;
            }
            else
            {
                Debug.LogWarning("[TitleScreenUICreator] Main Camera が見つかりません。Canvas の Render Camera を手動で設定してください。");
            }

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            var canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            return canvasObj;
        }

        #endregion

        #region UI Elements

        private static Image CreateBackground(Transform parent)
        {
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(parent, false);

            var rect = bgObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = bgObj.AddComponent<Image>();
            image.color = new Color(0.06f, 0.06f, 0.1f); // ダークブルー

            return image;
        }

        private static TitleReferences CreateTitleSection(Transform parent)
        {
            // Title Root
            var titleRoot = new GameObject("TitleRoot");
            titleRoot.transform.SetParent(parent, false);

            var rootRect = titleRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.6f);
            rootRect.anchorMax = new Vector2(0.5f, 0.6f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(1200f, 300f);

            // Main Title
            var mainTitleObj = new GameObject("MainTitle");
            mainTitleObj.transform.SetParent(titleRoot.transform, false);

            var mainTitleRect = mainTitleObj.AddComponent<RectTransform>();
            mainTitleRect.anchorMin = new Vector2(0.5f, 0.6f);
            mainTitleRect.anchorMax = new Vector2(0.5f, 0.6f);
            mainTitleRect.pivot = new Vector2(0.5f, 0.5f);
            mainTitleRect.anchoredPosition = Vector2.zero;
            mainTitleRect.sizeDelta = new Vector2(1200f, 150f);

            var mainTitle = mainTitleObj.AddComponent<TextMeshProUGUI>();
            mainTitle.text = "AIわからせバトル";
            mainTitle.fontSize = 100;
            mainTitle.fontStyle = FontStyles.Bold;
            mainTitle.alignment = TextAlignmentOptions.Center;
            mainTitle.enableWordWrapping = false;
            mainTitle.enableVertexGradient = true;
            mainTitle.colorGradient = new VertexGradient(
                new Color(1f, 0.88f, 0.4f),    // Top: ゴールド
                new Color(1f, 0.88f, 0.4f),
                new Color(1f, 0.27f, 0.27f),   // Bottom: レッド
                new Color(1f, 0.27f, 0.27f)
            );

            // Sub Title
            var subTitleObj = new GameObject("SubTitle");
            subTitleObj.transform.SetParent(titleRoot.transform, false);

            var subTitleRect = subTitleObj.AddComponent<RectTransform>();
            subTitleRect.anchorMin = new Vector2(0.5f, 0.3f);
            subTitleRect.anchorMax = new Vector2(0.5f, 0.3f);
            subTitleRect.pivot = new Vector2(0.5f, 0.5f);
            subTitleRect.anchoredPosition = Vector2.zero;
            subTitleRect.sizeDelta = new Vector2(600f, 60f);

            var subTitle = subTitleObj.AddComponent<TextMeshProUGUI>();
            subTitle.text = "～職を取り戻せ～";
            subTitle.fontSize = 40;
            subTitle.alignment = TextAlignmentOptions.Center;
            subTitle.enableWordWrapping = false;
            subTitle.color = new Color(0.49f, 0.83f, 0.99f); // シアン

            return new TitleReferences
            {
                Root = rootRect,
                MainTitle = mainTitle,
                SubTitle = subTitle
            };
        }

        private static ButtonReferences CreateButtonSection(Transform parent)
        {
            // Button Root
            var buttonRoot = new GameObject("ButtonRoot");
            buttonRoot.transform.SetParent(parent, false);

            var rootRect = buttonRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.3f);
            rootRect.anchorMax = new Vector2(0.5f, 0.3f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(400f, 200f);

            // Vertical Layout
            var verticalLayout = buttonRoot.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 20f;
            verticalLayout.childAlignment = TextAnchor.MiddleCenter;
            verticalLayout.childControlWidth = false;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = false;
            verticalLayout.childForceExpandHeight = false;

            // Start Button
            var startButton = CreateButton(buttonRoot.transform, "StartButton", "ゲーム開始",
                new Color(1f, 0.42f, 0.21f), new Vector2(300f, 70f));

            // Exit Button
            var exitButton = CreateButton(buttonRoot.transform, "ExitButton", "終了",
                new Color(0.35f, 0.35f, 0.45f), new Vector2(300f, 70f));

            return new ButtonReferences
            {
                Root = rootRect,
                StartButton = startButton.Button,
                StartButtonText = startButton.Text,
                ExitButton = exitButton.Button,
                ExitButtonText = exitButton.Text
            };
        }

        private static (Button Button, TextMeshProUGUI Text) CreateButton(Transform parent, string name, string label, Color bgColor, Vector2 size)
        {
            var buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            var rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            var image = buttonObj.AddComponent<Image>();
            image.color = bgColor;
            image.type = Image.Type.Sliced;

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;

            // ボタンのTransition設定
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            // テキスト
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 36;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return (button, text);
        }

        private static void CreateFooter(Transform parent)
        {
            var footerObj = new GameObject("Footer");
            footerObj.transform.SetParent(parent, false);

            var rect = footerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 30f);
            rect.sizeDelta = new Vector2(400f, 30f);

            var text = footerObj.AddComponent<TextMeshProUGUI>();
            text.text = "© 2025 LearningAIGame";
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.4f, 0.4f, 0.5f, 0.5f);
        }

        #endregion

        #region Controller Setup

        private static void SetupController(
            GameObject canvasRoot,
            Image background,
            TitleReferences title,
            ButtonReferences buttons)
        {
            var controller = canvasRoot.AddComponent<TitleScreenUIController>();

            var serializedObject = new SerializedObject(controller);

            // Canvas Group & Background
            serializedObject.FindProperty("_canvasGroup").objectReferenceValue = canvasRoot.GetComponent<CanvasGroup>();
            serializedObject.FindProperty("_backgroundImage").objectReferenceValue = background;

            // Title
            serializedObject.FindProperty("_titleRoot").objectReferenceValue = title.Root;
            serializedObject.FindProperty("_mainTitle").objectReferenceValue = title.MainTitle;
            serializedObject.FindProperty("_subTitle").objectReferenceValue = title.SubTitle;

            // Buttons
            serializedObject.FindProperty("_buttonRoot").objectReferenceValue = buttons.Root;
            serializedObject.FindProperty("_startButton").objectReferenceValue = buttons.StartButton;
            serializedObject.FindProperty("_startButtonText").objectReferenceValue = buttons.StartButtonText;
            serializedObject.FindProperty("_exitButton").objectReferenceValue = buttons.ExitButton;
            serializedObject.FindProperty("_exitButtonText").objectReferenceValue = buttons.ExitButtonText;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region Reference Structs

        private struct TitleReferences
        {
            public RectTransform Root;
            public TextMeshProUGUI MainTitle;
            public TextMeshProUGUI SubTitle;
        }

        private struct ButtonReferences
        {
            public RectTransform Root;
            public Button StartButton;
            public TextMeshProUGUI StartButtonText;
            public Button ExitButton;
            public TextMeshProUGUI ExitButtonText;
        }

        #endregion
    }
}
#endif