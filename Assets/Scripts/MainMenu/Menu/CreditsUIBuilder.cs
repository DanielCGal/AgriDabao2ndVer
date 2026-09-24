using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class CreditsUIBuilder : MonoBehaviour
    {
        public static CreditsUIBuilder Instance { get; private set; }

        private const float ScrollSpeed = 45f;

        private const float ResumeDelay = 1.2f;

        private const float PanelWidth = 1100f;
        private const float PanelHeight = 820f;

        private const float BoardInset = 160f;

        private Canvas canvas;
        private GameObject root;
        private ScrollRect scroll;
        private RectTransform content;
        private UIThemeSprites theme;

        private float resumeAt;
        private bool reachedEnd;
        private bool dragging;

        private void Awake()
        {
            Instance = this;
            EnsureEventSystem();
            EnsureCanvas();
            Build();
            root.SetActive(false);
        }

        public void Show()
        {
            reachedEnd = false;
            dragging = false;
            resumeAt = 0f;

            Canvas.ForceUpdateCanvases();
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;

            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        private void Update()
        {
            if (root == null || !root.activeSelf || scroll == null || reachedEnd)
                return;

            if (dragging || Time.unscaledTime < resumeAt)
                return;

            float viewportHeight = scroll.viewport != null ? scroll.viewport.rect.height : 0f;
            float scrollable = content.rect.height - viewportHeight;
            if (scrollable <= 1f)
                return;

            float step = ScrollSpeed / scrollable * Time.unscaledDeltaTime;
            float next = scroll.verticalNormalizedPosition - step;

            if (next <= 0f)
            {
                scroll.verticalNormalizedPosition = 0f;
                reachedEnd = true;
                return;
            }

            scroll.verticalNormalizedPosition = next;
        }

        private void Build()
        {
            theme = UIThemeSprites.Instance;

            root = new GameObject("Credits_Root", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.GetComponent<RectTransform>());
            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.55f);
            blocker.raycastTarget = true;

            GameObject panel = new GameObject("CreditsPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panelRect.anchoredPosition = Vector2.zero;

            Image board = panel.GetComponent<Image>();
            Sprite boardArt = theme?.creditsBoard != null ? theme.creditsBoard : theme?.panelBoard;
            if (boardArt != null)
            {
                board.sprite = boardArt;
                board.type = Image.Type.Sliced;
                board.color = Color.white;
            }
            else
            {
                board.color = new Color(0.05f, 0.10f, 0.06f, 0.97f);
            }

            BuildScrollArea(panelRect);
            BuildCloseButton(panelRect);
        }

        private void BuildScrollArea(RectTransform parent)
        {
            GameObject scrollGo = new GameObject("CreditsScroll",
                typeof(RectTransform), typeof(ScrollRect), typeof(CreditsDragWatcher));
            scrollGo.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(BoardInset, 120f);
            scrollRect.offsetMax = new Vector2(-BoardInset, -90f);

            GameObject viewportGo = new GameObject("Viewport",
                typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewport = viewportGo.GetComponent<RectTransform>();
            Stretch(viewport);
            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.001f);

            GameObject contentGo = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 40, 40);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildCreditsContent(contentGo.transform);

            scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.horizontalScrollbar = null;
            scroll.verticalScrollbar = null;

            CreditsDragWatcher watcher = scrollGo.GetComponent<CreditsDragWatcher>();
            watcher.owner = this;
        }

        internal void NotifyDragging(bool isDragging)
        {
            dragging = isDragging;
            if (!isDragging)
            {
                resumeAt = Time.unscaledTime + ResumeDelay;
                reachedEnd = false;
            }
        }

        private void BuildCloseButton(RectTransform parent)
        {
            GameObject go = new GameObject("CreditsClose",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(300f, 90f);
            rect.anchoredPosition = new Vector2(0f, 30f);

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Hide);

            if (theme?.closeButton != null)
            {
                image.sprite = theme.closeButton;
                image.preserveAspect = true;
                image.color = Color.white;
                return;
            }

            image.color = new Color(0.20f, 0.55f, 0.20f, 0.95f);
            Text label = CreateText(go.transform as RectTransform, "Text", 24,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform);
            label.text = "CLOSE";
        }

        private void BuildCreditsContent(Transform parent)
        {
            AddLogo(parent, theme?.gameLogo, 520f, 200f);
            AddGap(parent, 20f);

            AddHeading(parent, "PROPONENTS");
            AddLine(parent, "Full Stack Developer: Daniel C. Galam");
            AddLine(parent, "Technical Writer: Jessica Mae Suello");
            AddGap(parent, 30f);

            AddHeading(parent, "GAME ENGINE");
            AddLine(parent, "Unity 6.3 LTS - Universal Render Pipeline");
            AddSubtle(parent, "Unity EULA / Unity Companion License");
            AddGap(parent, 24f);

            AddHeading(parent, "BACKEND");
            AddLine(parent, "Spring Boot - Web, Security, OAuth2 Resource Server,");
            AddLine(parent, "Data JPA, Validation, Actuator, Mail");
            AddSubtle(parent, "Apache License 2.0");
            AddLine(parent, "Flyway (Community Edition) - database migrations");
            AddSubtle(parent, "Apache License 2.0");
            AddLine(parent, "PostgreSQL JDBC Driver");
            AddSubtle(parent, "BSD 2-Clause License");
            AddGap(parent, 24f);

            AddHeading(parent, "HOSTING");
            AddLine(parent, "Railway - application and PostgreSQL hosting");
            AddLine(parent, "Resend - transactional email delivery");
            AddGap(parent, 24f);

            AddHeading(parent, "LIBRARIES");
            AddLine(parent, "Json.NET for Unity (com.unity.nuget.newtonsoft-json)");
            AddSubtle(parent, "Copyright (c) 2007 James Newton-King");
            AddSubtle(parent, "Licensed under the MIT License");
            AddGap(parent, 12f);

            AddLine(parent, "UnityMeshSimplifier (com.whinarn.unitymeshsimplifier)");
            AddLine(parent, "Used to reduce the polygon count of the 3D models");
            AddLine(parent, "so the game runs smoothly on mobile devices.");
            AddSubtle(parent, "Copyright (c) 2017-2021 Mattias Edlund");
            AddSubtle(parent, "Licensed under the MIT License");
            AddGap(parent, 24f);

            AddHeading(parent, "ARTIFICIAL INTELLIGENCE");
            AddLine(parent, "Google Gemini API (gemini-2.5-flash)");
            AddLine(parent, "Powers the AI Adviser chatbot, task generator");
            AddLine(parent, "and climate resilience evaluation.");
            AddSubtle(parent, "In-game farming advice is AI-generated and may be");
            AddSubtle(parent, "inaccurate. Always verify with a real agriculturist.");
            AddGap(parent, 24f);

            AddHeading(parent, "SCIENTIFIC DATA");
            AddLine(parent, "Soil data (c) ISRIC - World Soil Information");
            AddLine(parent, "SoilGrids REST API");
            AddSubtle(parent, "Licensed under CC BY 4.0");
            AddSubtle(parent, "rest.isric.org/soilgrids/v2.0");
            AddGap(parent, 12f);

            AddLine(parent, "Davao City climate calendar based on monthly");
            AddLine(parent, "rainfall, temperature and sunshine averages from");
            AddLine(parent, "Weather Atlas and Weather and Climate.");
            AddSubtle(parent, "weather-atlas.com | weather-and-climate.com");
            AddGap(parent, 12f);

            AddLine(parent, "Seed, planting material and crop prices based on");
            AddLine(parent, "Department of Agriculture administrative");
            AddLine(parent, "issuances, Philippine Statistics Authority");
            AddLine(parent, "reports, DOST-PCAARRD and Bankerohan Public");
            AddLine(parent, "Market reports by 93.9 IFM News Davao.");
            AddGap(parent, 12f);

            AddLine(parent, "Pest and disease profiles based on Bureau of");
            AddLine(parent, "Plant Industry crop production guides and");
            AddLine(parent, "Region 11 crop pest reports, Department of");
            AddLine(parent, "Agriculture.");
            AddGap(parent, 24f);

            AddHeading(parent, "MAPS");
            AddLine(parent, "Davao City map imagery, district and barangay");
            AddLine(parent, "boundaries (c) City Government of Davao");
            AddSubtle(parent, "gismap.davaocity.gov.ph/Davaomap");
            AddGap(parent, 24f);

            AddHeading(parent, "ART AND ASSETS");
            AddLine(parent, "Meshy AI (Pro Tier) - 3D crop and objects models");
            AddLine(parent, "Artlist AI (AI Starter Tier) - for the Game User");
            AddLine(parent, "Interface Artwork");
            AddLine(parent, "Playpen Sans by TypeTogether");
            AddSubtle(parent, "(c) 2023 The Playpen Sans Project Authors");
            AddSubtle(parent, "Licensed under the SIL Open Font License 1.1");
            AddGap(parent, 24f);

            AddHeading(parent, "SPECIAL THANKS");
            AddLine(parent, "To the farmers of Davao City, whose work inspired");
            AddLine(parent, "this project.");
            AddGap(parent, 40f);

            AddLogo(parent, theme?.teamLogo, 420f, 170f);
            AddGap(parent, 30f);
        }

        private void AddHeading(Transform parent, string value)
        {
            Text text = CreateText(parent as RectTransform, "Heading", 30,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            text.text = value;
            text.color = new Color(1f, 0.92f, 0.62f, 1f);
            AddFitter(text.gameObject);
        }

        private void AddLine(Transform parent, string value)
        {
            Text text = CreateText(parent as RectTransform, "Line", 24,
                TextAnchor.MiddleCenter, FontStyle.Normal);
            text.text = value;
            AddFitter(text.gameObject);
        }

        private void AddSubtle(Transform parent, string value)
        {
            Text text = CreateText(parent as RectTransform, "Note", 19,
                TextAnchor.MiddleCenter, FontStyle.Italic);
            text.text = value;
            text.color = new Color(0.88f, 0.88f, 0.82f, 0.85f);
            AddFitter(text.gameObject);
        }

        private void AddGap(Transform parent, float height)
        {
            GameObject go = new GameObject("Gap", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
        }

        private void AddLogo(Transform parent, Sprite sprite, float width, float height)
        {
            if (sprite == null)
                return;

            GameObject go = new GameObject("Logo", typeof(RectTransform),
                typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            LayoutElement element = go.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
        }

        private static void AddFitter(GameObject go)
        {
            ContentSizeFitter fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static Text CreateText(RectTransform parent, string name, int size,
            TextAnchor alignment, FontStyle style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void EnsureCanvas()
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject go = new GameObject("Canvas",
                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
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
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            new GameObject("EventSystem", typeof(EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public class CreditsDragWatcher : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        internal CreditsUIBuilder owner;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner != null)
                owner.NotifyDragging(true);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner != null)
                owner.NotifyDragging(false);
        }
    }
}
