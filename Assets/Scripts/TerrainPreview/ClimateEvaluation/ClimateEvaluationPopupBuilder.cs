using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AgriDabao3D
{
    public class ClimateEvaluationPopupBuilder : MonoBehaviour
    {
        [Header("Runtime References")]
        public Canvas canvas;

        private GameObject startPopup;
        private Text startPopupText;

        private GameObject resultPopup;
        private Text resultTitleText;
        private Text resultScoreText;
        private ScrollRect resultScrollRect;
        private RectTransform resultContentRect;
        private Text resultBodyText;

        private void Start()
        {
            EnsureEventSystem();
            EnsureCanvas();
            BuildStartPopup();
            BuildResultPopup();
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void EnsureCanvas()
        {
            if (canvas == null)
                canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f;
            }
        }

        private void BuildStartPopup()
        {
            bool themed = Theme?.climateEventBoard != null;
            startPopup = CreatePanel("ClimateEventStartPopup",
                themed ? new Vector2(1120f, 380f) : new Vector2(820f, 300f),
                Theme?.climateEventBoard);
            startPopup.SetActive(false);

            CreateSign(startPopup.transform, Theme?.climateEventLabel, new Vector2(520f, 150f), 8f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(startPopup.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.30f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(themed ? 90f : 24f, 24f);
            textRect.offsetMax = new Vector2(themed ? -90f : -24f, themed ? -90f : -24f);

            startPopupText = textGo.GetComponent<Text>();
            startPopupText.font = GameFonts.Primary;
            startPopupText.fontSize = themed ? 24 : 28;
            startPopupText.fontStyle = themed ? FontStyle.Bold : FontStyle.Normal;
            startPopupText.alignment = TextAnchor.MiddleCenter;
            startPopupText.color = themed ? new Color(0.20f, 0.12f, 0.04f, 1f) : Color.white;

            CreateButton(
                startPopup.transform,
                "Okay!",
                new Vector2(0.5f, 0f),
                new Vector2(themed ? 230f : 180f, themed ? 66f : 60f),
                new Vector2(0f, themed ? 55f : 30f),
                () => startPopup.SetActive(false),
                Theme?.climateOkayButton
            );
        }

        private void BuildResultPopup()
        {
            bool themed = Theme?.climateEvaluationBoard != null;
            resultPopup = CreatePanel("ClimateEventResultPopup",
                themed ? new Vector2(1100f, 720f) : new Vector2(980f, 700f),
                Theme?.climateEvaluationBoard);
            resultPopup.SetActive(false);

            CreateTitleText();
            CreateScoreText();
            CreateScrollableBody();
            CreateButton(
                resultPopup.transform,
                "Okay!",
                new Vector2(0.5f, 0f),
                new Vector2(themed ? 230f : 180f, themed ? 66f : 60f),
                new Vector2(0f, themed ? 60f : 24f),
                () => resultPopup.SetActive(false),
                Theme?.climateOkayButton
            );
        }

        private void CreateTitleText()
        {
            GameObject go = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(resultPopup.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 60f);
            rect.anchoredPosition = new Vector2(0f, -44f);

            resultTitleText = go.GetComponent<Text>();
            resultTitleText.font = GameFonts.Primary;
            resultTitleText.fontSize = 32;
            resultTitleText.alignment = TextAnchor.MiddleCenter;
            resultTitleText.color = Color.white;
            resultTitleText.text = "Climate Event Ended";
        }

        private void CreateScoreText()
        {
            GameObject go = new GameObject("ScoreText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(resultPopup.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 50f);
            rect.anchoredPosition = new Vector2(0f, -100f);

            resultScoreText = go.GetComponent<Text>();
            resultScoreText.font = GameFonts.Primary;
            resultScoreText.fontSize = 26;
            resultScoreText.alignment = TextAnchor.MiddleCenter;
            resultScoreText.color = new Color(1f, 0.9f, 0.4f);
            resultScoreText.text = "Mitigation Score: 0%";
        }

        private void CreateScrollableBody()
        {
            GameObject scrollView = new GameObject(
                "Scroll View",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect)
            );
            scrollView.transform.SetParent(resultPopup.transform, false);

            bool themedBoard = Theme?.climateEvaluationBoard != null;
            RectTransform scrollRectTransform = scrollView.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(themedBoard ? 158f : 30f, themedBoard ? 140f : 100f);
            scrollRectTransform.offsetMax = new Vector2(themedBoard ? -174f : -30f, themedBoard ? -210f : -140f);

            Image scrollBg = scrollView.GetComponent<Image>();
            scrollBg.color = themedBoard
                ? new Color(1f, 1f, 1f, 0f)
                : new Color(1f, 1f, 1f, 0.06f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollView.transform, false);

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewport.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

            Mask viewportMask = viewport.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)
            );
            content.transform.SetParent(viewport.transform, false);

            resultContentRect = content.GetComponent<RectTransform>();
            resultContentRect.anchorMin = new Vector2(0f, 1f);
            resultContentRect.anchorMax = new Vector2(1f, 1f);
            resultContentRect.pivot = new Vector2(0.5f, 1f);
            resultContentRect.anchoredPosition = Vector2.zero;
            resultContentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = content.GetComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            GameObject textGo = new GameObject("EvaluationText", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
            textGo.transform.SetParent(content.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.sizeDelta = new Vector2(0f, 0f);

            resultBodyText = textGo.GetComponent<Text>();
            resultBodyText.font = GameFonts.Primary;
            resultBodyText.fontSize = 24;
            resultBodyText.alignment = TextAnchor.UpperLeft;
            resultBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            resultBodyText.verticalOverflow = VerticalWrapMode.Truncate;
            resultBodyText.color = Color.white;
            resultBodyText.text = "";

            ContentSizeFitter textFitter = textGo.GetComponent<ContentSizeFitter>();
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            resultScrollRect = scrollView.GetComponent<ScrollRect>();
            resultScrollRect.viewport = viewportRect;
            resultScrollRect.content = resultContentRect;
            resultScrollRect.horizontal = false;
            resultScrollRect.vertical = true;
            resultScrollRect.movementType = ScrollRect.MovementType.Clamped;
            resultScrollRect.scrollSensitivity = 30f;
        }

        private UIThemeSprites Theme => UIThemeSprites.Instance;

        private GameObject CreatePanel(string name, Vector2 size, Sprite board = null)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            Image bg = panel.GetComponent<Image>();
            if (board != null)
            {
                bg.sprite = board;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0f, 0f, 0f, 0.85f);
            }

            return panel;
        }

        private void CreateSign(Transform parent, Sprite art, Vector2 size, float offsetY)
        {
            if (art == null)
                return;

            GameObject go = new GameObject("TitleSign", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0f, offsetY);

            Image image = go.GetComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void CreateButton(Transform parent, string label, Vector2 anchor, Vector2 size,
            Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick, Sprite art = null)
        {
            GameObject go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Image bg = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(onClick);

            if (art != null)
            {
                bg.sprite = art;
                bg.preserveAspect = true;
                bg.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
                return;
            }

            bg.color = new Color(0.2f, 0.6f, 0.2f, 0.95f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGo.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
        }

        public void ShowEventStarted(string eventName, int durationDays)
        {
            if (startPopup == null)
                return;

            startPopupText.text =
                $"{eventName} is currently in your area right now and it will take {durationDays} day(s)!\n\n" +
                "Mitigate and strategize what you should do in this current weather!";

            startPopup.transform.SetAsLastSibling();
            startPopup.SetActive(true);
        }

        public void ShowEventEnded(string eventName, float scorePercent, string evaluationText)
        {
            if (resultPopup == null)
                return;

            float clampedScore = Mathf.Clamp(scorePercent, 0f, 100f);

            resultTitleText.text = $"{eventName} has ended";
            resultScoreText.text = $"Mitigation Score: {Mathf.RoundToInt(clampedScore)}%";
            resultBodyText.text = CropNaming.Humanize(AiText.StripMarkdown(evaluationText));

            resultPopup.transform.SetAsLastSibling();
            resultPopup.SetActive(true);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(resultContentRect);
            resultScrollRect.verticalNormalizedPosition = 1f;
        }

        public void HideAll()
        {
            if (startPopup != null) startPopup.SetActive(false);
            if (resultPopup != null) resultPopup.SetActive(false);
        }
    }
}
