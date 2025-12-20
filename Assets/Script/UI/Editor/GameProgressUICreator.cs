#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LearningAIGame.UI.Battle.Editor
{
    /// <summary>
    /// ゲーム進行UIをヒエラルキー上に自動生成するエディタースクリプト
    /// </summary>
    public static class GameProgressUICreator
    {
        #region Menu Items

        [MenuItem("GameObject/UI/LearningAIGame/Game Progress UI", false, 10)]
        public static void CreateGameProgressUI()
        {
            var root = CreateCanvasRoot();
            var blackoutImage = CreateBlackoutImage(root.transform);
            var contentRoot = CreateContentRoot(root.transform);

            // 各表示パネルを作成
            var roundDisplay = CreateRoundDisplay(contentRoot);
            var fightDisplay = CreateFightDisplay(contentRoot);
            var resultDisplay = CreateResultDisplay(contentRoot);
            var gameSetDisplay = CreateGameSetDisplay(contentRoot);

            // コントローラーをアタッチして参照を設定
            SetupController(root, contentRoot, blackoutImage, roundDisplay, fightDisplay, resultDisplay, gameSetDisplay);

            // 選択状態にする
            Selection.activeGameObject = root;
            Undo.RegisterCreatedObjectUndo(root, "Create Game Progress UI");

            Debug.Log("[GameProgressUICreator] Game Progress UI を作成しました");
        }

        #endregion

        #region Canvas Setup

        private static GameObject CreateCanvasRoot()
        {
            // Canvas
            var canvasObj = new GameObject("GameProgressUI");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.sortingOrder = 100;

            // メインカメラを自動設定（見つからなければ警告）
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                canvas.worldCamera = mainCamera;
                canvas.planeDistance = 1f;
            }
            else
            {
                Debug.LogWarning("[GameProgressUICreator] Main Camera が見つかりません。Canvas の Render Camera を手動で設定してください。");
            }

            // CanvasScaler
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();

            // CanvasGroup
            var canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            return canvasObj;
        }

        private static Image CreateBlackoutImage(Transform parent)
        {
            var blackoutObj = new GameObject("BlackoutImage");
            blackoutObj.transform.SetParent(parent, false);
            blackoutObj.transform.SetAsFirstSibling(); // 最背面に配置

            var rect = blackoutObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = blackoutObj.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f); // 透明で初期化
            image.raycastTarget = false;

            return image;
        }

        private static RectTransform CreateContentRoot(Transform parent)
        {
            var contentObj = new GameObject("ContentRoot");
            contentObj.transform.SetParent(parent, false);

            var rectTransform = contentObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 50f);
            rectTransform.sizeDelta = new Vector2(800f, 300f);
            rectTransform.localScale = Vector3.zero; // 初期状態

            return rectTransform;
        }

        #endregion

        #region Display Panels

        private static RoundDisplayReferences CreateRoundDisplay(RectTransform parent)
        {
            // Root
            var root = CreateDisplayRoot("RoundDisplay", parent);
            root.SetActive(false);

            // Round Label
            var labelObj = CreateTextObject("RoundLabel", root.transform);
            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0f, 80f);

            var labelText = labelObj.GetComponent<TextMeshProUGUI>();
            labelText.text = "ROUND";
            labelText.fontSize = 48;
            labelText.fontStyle = FontStyles.Normal;
            labelText.color = new Color(0.9f, 0.85f, 0.7f); // ウォームクリーム
            labelText.characterSpacing = 20f;

            // Round Number
            var numberObj = CreateTextObject("RoundNumber", root.transform);
            var numberRect = numberObj.GetComponent<RectTransform>();
            numberRect.anchoredPosition = new Vector2(0f, -40f);

            var numberText = numberObj.GetComponent<TextMeshProUGUI>();
            numberText.text = "1";
            numberText.fontSize = 200;
            numberText.fontStyle = FontStyles.Bold;
            numberText.enableVertexGradient = true;
            numberText.colorGradient = new VertexGradient(
                new Color(1f, 0.95f, 0.8f),    // Top: ライトクリーム
                new Color(1f, 0.95f, 0.8f),
                new Color(0.95f, 0.75f, 0.4f), // Bottom: ゴールデンオレンジ
                new Color(0.95f, 0.75f, 0.4f)
            );

            return new RoundDisplayReferences
            {
                Root = root,
                Label = labelText,
                Number = numberText
            };
        }

        private static FightDisplayReferences CreateFightDisplay(RectTransform parent)
        {
            // Root
            var root = CreateDisplayRoot("FightDisplay", parent);
            root.SetActive(false);

            // Fight Text
            var textObj = CreateTextObject("FightText", root.transform);
            var text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = "FIGHT!";
            text.fontSize = 140;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 0.75f, 0.15f); // オレンジゴールド
            text.characterSpacing = 10f;

            return new FightDisplayReferences
            {
                Root = root,
                Text = text
            };
        }

        private static ResultDisplayReferences CreateResultDisplay(RectTransform parent)
        {
            // Root
            var root = CreateDisplayRoot("ResultDisplay", parent);
            root.SetActive(false);

            // Result Text
            var textObj = CreateTextObject("ResultText", root.transform);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0f, 20f);

            var text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = "PLAYER WIN!";
            text.fontSize = 110;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.38f, 0.65f, 0.98f); // ブルー
            text.characterSpacing = 5f;

            // Sub Text
            var subTextObj = CreateTextObject("ResultSubText", root.transform);
            var subTextRect = subTextObj.GetComponent<RectTransform>();
            subTextRect.anchoredPosition = new Vector2(0f, -60f);

            var subText = subTextObj.GetComponent<TextMeshProUGUI>();
            subText.text = "— DEFEATED —";
            subText.fontSize = 36;
            subText.fontStyle = FontStyles.Normal;
            subText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            subText.characterSpacing = 15f;
            subTextObj.SetActive(false);

            return new ResultDisplayReferences
            {
                Root = root,
                Text = text,
                SubText = subText
            };
        }

        private static GameSetDisplayReferences CreateGameSetDisplay(RectTransform parent)
        {
            // Root
            var root = CreateDisplayRoot("GameSetDisplay", parent);
            root.SetActive(false);

            // Game Set Text
            var textObj = CreateTextObject("GameSetText", root.transform);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0f, 20f);

            var text = textObj.GetComponent<TextMeshProUGUI>();
            text.text = "GAME SET!";
            text.fontSize = 130;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.65f, 0.55f, 0.98f); // パープル
            text.characterSpacing = 10f;

            // Decorative Line
            var lineObj = new GameObject("GameSetLine");
            lineObj.transform.SetParent(root.transform, false);

            var lineRect = lineObj.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = new Vector2(0f, -70f);
            lineRect.sizeDelta = new Vector2(450f, 4f);
            lineRect.localScale = new Vector3(0f, 1f, 1f); // 初期状態

            var lineImage = lineObj.AddComponent<Image>();
            lineImage.color = new Color(0.65f, 0.55f, 0.98f, 0f);

            return new GameSetDisplayReferences
            {
                Root = root,
                Text = text,
                Line = lineImage
            };
        }

        #endregion

        #region Helper Methods

        private static GameObject CreateDisplayRoot(string name, RectTransform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return obj;
        }

        private static GameObject CreateTextObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 200f);

            var text = obj.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;

            return obj;
        }

        private static void SetupController(
            GameObject canvasRoot,
            RectTransform contentRoot,
            Image blackoutImage,
            RoundDisplayReferences round,
            FightDisplayReferences fight,
            ResultDisplayReferences result,
            GameSetDisplayReferences gameSet)
        {
            var controller = canvasRoot.AddComponent<GameProgressUIController>();

            // SerializedObjectを使って private フィールドに値を設定
            var serializedObject = new SerializedObject(controller);

            // CanvasGroup & ContentRoot & Blackout
            serializedObject.FindProperty("_canvasGroup").objectReferenceValue = canvasRoot.GetComponent<CanvasGroup>();
            serializedObject.FindProperty("_contentRoot").objectReferenceValue = contentRoot;
            serializedObject.FindProperty("_blackoutImage").objectReferenceValue = blackoutImage;

            // Round Display
            serializedObject.FindProperty("_roundDisplay").objectReferenceValue = round.Root;
            serializedObject.FindProperty("_roundLabel").objectReferenceValue = round.Label;
            serializedObject.FindProperty("_roundNumber").objectReferenceValue = round.Number;

            // Fight Display
            serializedObject.FindProperty("_fightDisplay").objectReferenceValue = fight.Root;
            serializedObject.FindProperty("_fightText").objectReferenceValue = fight.Text;

            // Result Display
            serializedObject.FindProperty("_resultDisplay").objectReferenceValue = result.Root;
            serializedObject.FindProperty("_resultText").objectReferenceValue = result.Text;
            serializedObject.FindProperty("_resultSubText").objectReferenceValue = result.SubText;

            // Game Set Display
            serializedObject.FindProperty("_gameSetDisplay").objectReferenceValue = gameSet.Root;
            serializedObject.FindProperty("_gameSetText").objectReferenceValue = gameSet.Text;
            serializedObject.FindProperty("_gameSetLine").objectReferenceValue = gameSet.Line;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region Reference Structs

        private struct RoundDisplayReferences
        {
            public GameObject Root;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Number;
        }

        private struct FightDisplayReferences
        {
            public GameObject Root;
            public TextMeshProUGUI Text;
        }

        private struct ResultDisplayReferences
        {
            public GameObject Root;
            public TextMeshProUGUI Text;
            public TextMeshProUGUI SubText;
        }

        private struct GameSetDisplayReferences
        {
            public GameObject Root;
            public TextMeshProUGUI Text;
            public Image Line;
        }

        #endregion
    }
}
#endif