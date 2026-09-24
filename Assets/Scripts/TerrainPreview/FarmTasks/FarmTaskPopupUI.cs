using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class FarmTaskPopupUI : MonoBehaviour
    {
        public static FarmTaskPopupUI Instance { get; private set; }

        private GameObject panel;
        private Text titleText;
        private Text bodyText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureEventSystem();
            Build();
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Show(string title, string body)
        {
            if (panel == null)
                Build();

            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(title)
                    ? "Farm Task"
                    : title;
            if (bodyText != null)
                bodyText.text = body ?? string.Empty;
            if (panel != null)
            {
                panel.transform.SetAsLastSibling();
                panel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        private void Build()
        {
            Canvas canvas = FindOrCreateCanvas();
            Transform existing = canvas.transform.Find("FarmTaskPopup");
            if (existing != null)
            {
                panel = existing.gameObject;
                titleText = existing.Find("Title")?.GetComponent<Text>();
                bodyText = existing.Find("BodyScroll/Viewport/Content/Body")?.GetComponent<Text>();
                return;
            }

            panel = new GameObject(
                "FarmTaskPopup",
                typeof(RectTransform),
                typeof(Image));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            UIThemeSprites theme = UIThemeSprites.Instance;
            Sprite board = theme?.farmTaskBoard;

            rect.sizeDelta = board != null ? new Vector2(1000f, 620f) : new Vector2(900f, 520f);
            rect.anchoredPosition = Vector2.zero;

            Image background = panel.GetComponent<Image>();
            if (board != null)
            {
                background.sprite = board;
                background.type = Image.Type.Sliced;
                background.color = Color.white;
            }
            else
            {
                background.color = new Color(0.025f, 0.10f, 0.15f, 0.97f);
            }

            titleText = CreateText(
                "Title",
                panel.transform,
                34,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(35f, -80f);
            titleRect.offsetMax = new Vector2(-35f, -20f);
            titleText.text = "Farm Task";

            if (theme?.farmTaskLabel != null)
            {
                titleText.gameObject.SetActive(false);

                GameObject signGo = new GameObject("TitleSign", typeof(RectTransform), typeof(Image));
                signGo.transform.SetParent(panel.transform, false);

                RectTransform signRect = signGo.GetComponent<RectTransform>();
                signRect.anchorMin = signRect.anchorMax = new Vector2(0.5f, 1f);
                signRect.pivot = new Vector2(0.5f, 0.5f);
                signRect.sizeDelta = new Vector2(520f, 150f);
                signRect.anchoredPosition = new Vector2(0f, 8f);

                Image signImage = signGo.GetComponent<Image>();
                signImage.sprite = theme.farmTaskLabel;
                signImage.preserveAspect = true;
                signImage.raycastTarget = false;
            }

            ScrollRect scroll = CreateScrollArea(panel.transform, board != null);
            bodyText = scroll.content.Find("Body").GetComponent<Text>();

            Button close = CreateButton(
                "CloseButton",
                panel.transform,
                "Close",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                board != null ? new Vector2(230f, 66f) : new Vector2(220f, 62f),
                new Vector2(0f, board != null ? 34f : 24f),
                theme?.farmTaskCloseButton);
            close.onClick.AddListener(Hide);
        }

        private static ScrollRect CreateScrollArea(Transform parent, bool themedBoard = false)
        {
            GameObject scrollGo = new GameObject(
                "BodyScroll",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(themedBoard ? 120f : 40f, themedBoard ? 125f : 105f);
            scrollRect.offsetMax = new Vector2(themedBoard ? -120f : -40f, themedBoard ? -170f : -90f);
            scrollGo.GetComponent<Image>().color = themedBoard
                ? new Color(1f, 1f, 1f, 0f)
                : new Color(1f, 1f, 1f, 0.045f);

            GameObject viewportGo = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewport = viewportGo.GetComponent<RectTransform>();
            Stretch(viewport);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 16, 16);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = contentGo.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text body = CreateText(
                "Body",
                contentGo.transform,
                25,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.text = string.Empty;
            ContentSizeFitter bodyFitter = body.gameObject.AddComponent<ContentSizeFitter>();
            bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            int size,
            FontStyle style,
            TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 size,
            Vector2 position,
            Sprite art = null)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
                return button;
            }

            image.color = new Color(0.08f, 0.58f, 0.24f, 0.98f);

            Text text = CreateText(
                "Text",
                go.transform,
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.text = label;
            return button;
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject(
                    "Canvas",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
