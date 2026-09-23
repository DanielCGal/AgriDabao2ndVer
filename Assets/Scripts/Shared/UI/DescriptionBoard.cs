using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// The wooden board that shows a long description, centred over whatever
    /// opened it, with a CLOSE plank along the bottom.
    ///
    /// Shared rather than copied because two screens use it - the shop's item
    /// descriptions and the area selection's district descriptions - and they are
    /// meant to be the same board. Kept as a plain class rather than a
    /// MonoBehaviour: it owns its GameObjects and needs no update loop, so the
    /// caller holds one of these as a field and forgets about it.
    ///
    /// It parents itself to whatever transform it is given, which is what makes it
    /// safe to leave lying around: hiding the parent panel hides this too, without
    /// every close path having to know it exists.
    /// </summary>
    public class DescriptionBoard
    {
        /// <summary>
        /// TradeConfirmBoard.png is 1760x1320 with fixed 130px 9-slice corners, and
        /// its painted logs run to x=280 and resume at x=1458. Because slicing
        /// holds the corners at true size, those logs eat a near-constant amount
        /// off the board however wide it is drawn - at 380 wide only about 96 units
        /// of plank would remain to write on. At 1200 the text column is about 730,
        /// which is what makes this readable rather than a ribbon.
        /// </summary>
        private const float BoardWidth = 1200f;
        private const float BoardHeight = 800f;
        private const float BoardArtWidth = 1760f;
        private const float LogEndsAtPx = 280f;
        private const float LogResumesAtPx = 1458f;

        /// <summary>CloseButton.png is 640x190, so preserveAspect keeps this true.</summary>
        private const float CloseWidth = 230f;
        private const float CloseHeight = 68f;

        /// <summary>Room left under the text for the close plank.</summary>
        private const float Footer = 104f;

        private GameObject root;
        private Text titleText;
        private Text bodyText;
        private ScrollRect scroll;

        public bool IsOpen => root != null && root.activeSelf;

        /// <summary>Builds the board hidden. Call <see cref="Show"/> to open it.</summary>
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

            // Left on so the board swallows clicks meant for whatever is behind it.
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

        /// <summary>
        /// A scrolling text column.
        ///
        /// The label IS the scroll content rather than a child of a layout group.
        /// Wrapping it in one left the label sized by the group instead of by the
        /// viewport, and the mask then cut characters off both edges. Stretched
        /// horizontal anchors with a zero sizeDelta give it exactly the viewport's
        /// width, so nothing can fall underneath the mask.
        ///
        /// The text also overflows vertically rather than truncating: a truncating
        /// label silently drops whole lines once the player raises the text size,
        /// so the words would vanish rather than scroll.
        /// </summary>
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

        /// <summary>
        /// The plank button that opens a board.
        ///
        /// Shared for the same reason the board itself is: two screens put one of
        /// these on screen and they are meant to be the same button. The caller
        /// positions the returned button; only its size is passed in, because the
        /// two screens have very different space to give it.
        /// </summary>
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
                // TradeRequestBoard.png has no 9-slice border, so it can only be
                // stretched, never sliced. Its grain runs lengthwise, which is why
                // a wider-than-native plank still reads as a plank.
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

        /// <summary>Fills the board and brings it to the front.</summary>
        public void Show(string title, string body)
        {
            if (root == null || string.IsNullOrEmpty(body))
                return;

            if (titleText != null)
                titleText.text = title;
            if (bodyText != null)
                bodyText.text = body;

            root.SetActive(true);

            // Drawn last so it covers everything its parent panel holds.
            root.transform.SetAsLastSibling();

            // A new subject starts at the top rather than wherever the last one was
            // left scrolled to.
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        /// <summary>
        /// How far in from each edge the plank wall begins.
        ///
        /// Computed from the width rather than fixed, because 9-slicing keeps the
        /// corners at true size and stretches only the middle, so the painted log
        /// moves inward as the board narrows. Laying text out against the sprite's
        /// 9-slice border instead would put the first eighty-odd units of every
        /// line on top of the log.
        /// </summary>
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

        /// <summary>
        /// A label styled for the board's planks. The dark browns used elsewhere
        /// are near-invisible against this art, so it uses a light fill over a hard
        /// outline, which stays readable over both plank and log.
        /// </summary>
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
