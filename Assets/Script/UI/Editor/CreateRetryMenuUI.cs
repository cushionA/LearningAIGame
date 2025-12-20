#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LearningAIGame.UI.Common.Editor
{
    /// <summary>
    /// リトライメニューUIをヒエラルキー上に自動生成するエディタースクリプト
    /// </summary>
    public static class RetryMenuUICreator
    {
        [MenuItem("GameObject/UI/LearningAIGame/Retry Menu UI", false, 12)]
        public static void CreateRetryMenuUI()
        {
            var root = CreateCanvasRoot();
            var panel = CreatePanel(root.transform);
            var buttons = CreateButtons(panel.PanelRect);

            SetupController(root, panel, buttons);

            Selection.activeGameObject = root;
            Undo.RegisterCreatedObjectUndo(root, "Create Retry Menu UI");

            Debug.Log("[RetryMenuUICreator] Retry Menu UI を作成しました");
        }

        #region Canvas Setup

        private static GameObject CreateCanvasRoot()
        {
            var canvasObj = new GameObject("RetryMenuUI");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = 150; // GameProgressUIより上

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                canvas.worldCamera = mainCamera;
                canvas.planeDistance = 1f;
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

        private static PanelReferences CreatePanel(Transform parent)
        {
            // Panel Root
            var panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(parent, false);

            var panelRect = panelObj.AddComponent<RectTransform>();
            // 画面下部に配置
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 60f);
            panelRect.sizeDelta = new Vector2(450f, 80f);

            // 背景
            var bgImage = panelObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            // 角丸風にするためにスプライトがあればSlicedに
            // ない場合はそのまま

            return new PanelReferences
            {
                PanelRect = panelRect,
                BackgroundImage = bgImage
            };
        }

        private static ButtonReferences CreateButtons(RectTransform panel)
        {
            // Horizontal Layout
            var layoutObj = new GameObject("ButtonLayout");
            layoutObj.transform.SetParent(panel, false);

            var layoutRect = layoutObj.AddComponent<RectTransform>();
            layoutRect.anchorMin = Vector2.zero;
            layoutRect.anchorMax = Vector2.one;
            layoutRect.offsetMin = new Vector2(20f, 15f);
            layoutRect.offsetMax = new Vector2(-20f, -15f);

            var layout = layoutObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // リトライボタン
            var retryButton = CreateButton(layoutObj.transform, "RetryButton", "リトライ",
                new Color(0.2f, 0.6f, 0.9f));

            // タイトルへボタン
            var titleButton = CreateButton(layoutObj.transform, "TitleButton", "タイトルへ",
                new Color(0.4f, 0.4f, 0.5f));

            return new ButtonReferences
            {
                RetryButton = retryButton.Button,
                RetryButtonText = retryButton.Text,
                TitleButton = titleButton.Button,
                TitleButtonText = titleButton.Text
            };
        }

        private static (Button Button, TextMeshProUGUI Text) CreateButton(
            Transform parent, string name, string label, Color bgColor)
        {
            var buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            var rect = buttonObj.AddComponent<RectTransform>();

            var image = buttonObj.AddComponent<Image>();
            image.color = bgColor;

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;

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
            text.fontSize = 28;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return (button, text);
        }

        #endregion

        #region Controller Setup

        private static void SetupController(GameObject root, PanelReferences panel, ButtonReferences buttons)
        {
            var controller = root.AddComponent<RetryMenuUIController>();

            var so = new SerializedObject(controller);

            so.FindProperty("_canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
            so.FindProperty("_panelRoot").objectReferenceValue = panel.PanelRect;
            so.FindProperty("_backgroundImage").objectReferenceValue = panel.BackgroundImage;
            so.FindProperty("_retryButton").objectReferenceValue = buttons.RetryButton;
            so.FindProperty("_retryButtonText").objectReferenceValue = buttons.RetryButtonText;
            so.FindProperty("_titleButton").objectReferenceValue = buttons.TitleButton;
            so.FindProperty("_titleButtonText").objectReferenceValue = buttons.TitleButtonText;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region Reference Structs

        private struct PanelReferences
        {
            public RectTransform PanelRect;
            public Image BackgroundImage;
        }

        private struct ButtonReferences
        {
            public Button RetryButton;
            public TextMeshProUGUI RetryButtonText;
            public Button TitleButton;
            public TextMeshProUGUI TitleButtonText;
        }

        #endregion
    }
}
#endif