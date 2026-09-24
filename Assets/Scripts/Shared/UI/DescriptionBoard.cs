using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class DescriptionBoard
    {
        private const float BoardWidth = 1200f;
        private const float BoardHeight = 800f;
        private const float BoardArtWidth = 1760f;
        private const float LogEndsAtPx = 280f;
        private const float LogResumesAtPx = 1458f;

        private const float CloseWidth = 230f;
        private const float CloseHeight = 68f;

        private const float Footer = 104f;

        private GameObject root;
        private Text titleText;
        private Text bodyText;
        private ScrollRect scroll;

        public bool IsOpen => root != null && root.activeSelf;

        public static DescriptionBoard Create(Transform parent, UIThemeSprites theme)
        {
            DescriptionBoard board = new DescriptionBoard();
            board.Build(parent, theme);
            return board;
        }

        private void Build(Transform parent, UIThemeSprites theme)
        {
            root = new GameObject("DescriptionBoard", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(BoardWidth, BoardHeight);
            rect.anchoredPosition = Vector2.zero;

            Image boardImage = root.GetComponent<Image>();
            Sprite art = theme != null ? theme.tradeConfirmBoard : null;
            if (art != null)
            {
                boardImage.sprite = art;
                boardImage.type = Image.Type.Sliced;
                boardImage.color = Color.white;
            }
            else
            {
                boardImage.color = new Color(0f, 0f, 0f, 0.82f);
            }

            boardImage.raycastTarget = true;

            float inset = ContentInset(theme, BoardWidth);

            titleText = CreateLabel(root.transform, 26, BoardWidth - inset * 2f, 46f);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -52f);
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 14;
            titleText.resizeTextMaxSize = 26;

            BuildScroll(inset);
            BuildCloseButton(theme);

            root.SetActive(false);
        }

        private void BuildScroll(float inset)
        {
            GameObject viewport = new GameObject("Viewport",
                typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);

            RectTransform viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.pivot = new Vector2(0.5f, 0.5f);
            viewRect.offsetMin = new Vector2(inset, Footer);
            viewRect.offsetMax = new Vector2(-inset, -110f);

            GameObject content = new GameObject("Content",
                typeof(RectTransform), typeof(Text));
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            bodyText = content.GetComponent<Text>();
            bodyText.font = GameFonts.Primary;
            bodyText.fontSize = 19;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.color = new Color(1f, 0.97f, 0.88f, 1f);
            bodyText.raycastTarget = false;
            bodyText.text = string.Empty;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            AddOutline(content, 1.5f);

            scroll = root.AddComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.scrollSensitivity = 28f;
            scroll.inertia = true;
        }

        private void BuildCloseButton(UIThemeSprites theme)
        {
            GameObject go = new GameObject("CloseDescription",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(CloseWidth, CloseHeight);
            rect.anchoredPosition = new Vector2(0f, 54f);

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Hide);

            Sprite art = theme != null ? theme.closeButton : null;
            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
                return;
            }

            image.color = new Color(0.55f, 0.18f, 0.12f, 0.95f);
            Text fallback = CreateLabel(go.transform, 22, CloseWidth, CloseHeight);
            fallback.text = "CLOSE";
            RectTransform fallbackRect = fallback.rectTransform;
            fallbackRect.anchorMin = Vector2.zero;
            fallbackRect.anchorMax = Vector2.one;
            fallbackRect.offsetMin = Vector2.zero;
            fallbackRect.offsetMax = Vector2.zero;
        }

        public static Button CreateOpenButton(Transform parent, UIThemeSprites theme,
            string label, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject("CheckDescriptionButton",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            go.GetComponent<RectTransform>().sizeDelta = size;

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            Sprite art = theme != null ? theme.tradeRequestBoard : null;
            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = false;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
            }
            else
            {
                image.color = new Color(0.45f, 0.30f, 0.14f, 0.95f);
            }

            Text text = CreateLabel(go.transform, 20, size.x - 46f, size.y - 22f);
            text.text = label;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = 20;

            return button;
        }

        public void Show(string title, string body)
        {
            if (root == null || string.IsNullOrEmpty(body))
                return;

            if (titleText != null)
                titleText.text = title;
            if (bodyText != null)
                bodyText.text = body;

            root.SetActive(true);

            root.transform.SetAsLastSibling();

            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        internal static float ContentInset(UIThemeSprites theme, float width)
        {
            Sprite art = theme != null ? theme.tradeConfirmBoard : null;
            if (art == null || art.rect.width <= 0f)
                return 40f;

            Vector4 border = art.border;
            float stretched = BoardArtWidth - border.x - border.z;
            if (stretched <= 0f)
                return 40f;

            float scale = (width - border.x - border.z) / stretched;
            float left = border.x + (LogEndsAtPx - border.x) * scale;
            float right = border.z +
                (BoardArtWidth - LogResumesAtPx - border.z) * scale;

            return Mathf.Max(left, right) + 10f;
        }

        internal static Text CreateLabel(Transform parent, int fontSize,
            float width, float height)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.97f, 0.88f, 1f);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;

            AddOutline(go, 2f);
            return text;
        }

        internal static void AddOutline(GameObject go, float distance)
        {
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.10f, 0.06f, 0.02f, 0.95f);
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = false;
        }
    }
}
