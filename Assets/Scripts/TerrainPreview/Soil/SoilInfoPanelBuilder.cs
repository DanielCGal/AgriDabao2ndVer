using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// The crop inspection readout on the left of the farm HUD.
    ///
    /// It stays hidden until the player actually inspects a crop - players can no
    /// longer read raw soil chemistry off the bare ground, so there is nothing to
    /// show until a plant is clicked. Art comes from the shared
    /// <see cref="UIThemeSprites"/> asset and is optional.
    ///
    /// The readout scrolls. It used to be a single truncating label sized for
    /// eight lines, which meant that raising the text size silently dropped
    /// whatever no longer fitted - fertility and soil suitability simply vanished
    /// off the bottom, with nothing to tell the player they existed. It is now a
    /// scroll view whose text overflows rather than truncates, so a longer readout
    /// or a larger font makes it scrollable instead of making it incomplete.
    /// </summary>
    public class SoilInfoPanelBuilder : MonoBehaviour
    {
        public Text createdText;

        private GameObject panelRoot;
        private ScrollRect scroll;
        private DescriptionBoard fieldGuide;

        /// <summary>
        /// Tall enough for most of the readout without scrolling, and short enough
        /// that the panel plus its button still clear the bottom of the screen at
        /// the largest UI scale, where the canvas is only 900 units high.
        /// </summary>
        private const float PanelHeight = 310f;
        private const float PanelWidth = 540f;
        private const float ButtonWidth = 420f;
        private const float ButtonHeight = 58f;

        private void Awake()
        {
            UIThemeSprites theme = UIThemeSprites.Instance;

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f; // landscape: scale by height
            }

            Sprite board = theme?.cropInfoBoard;

            GameObject panel = new GameObject("CropInfoPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            panelRoot = panel;
            HudRegistry.RegisterPiece(HudPiece.CropInfoPanel, panel);

            RectTransform pRect = panel.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0f, 0.5f);
            pRect.anchorMax = new Vector2(0f, 0.5f);
            pRect.pivot = new Vector2(0f, 0.5f);
            // The board is 9-sliced, so any aspect is fine - only the middle
            // stretches, the logs stay correct.
            pRect.sizeDelta = board != null
                ? new Vector2(PanelWidth, PanelHeight)
                : new Vector2(360f, PanelHeight);
            pRect.anchoredPosition = new Vector2(20f, -30f);

            Image bg = panel.GetComponent<Image>();
            if (board != null)
            {
                bg.sprite = board;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0f, 0f, 0f, 0.55f);
            }

            // Must clear the sprite's 9-slice border, which renders at its native
            // pixel size (L/R 95, T/B 45) no matter how the panel is scaled - a
            // smaller inset than that would run the text onto the rolled logs.
            float padX = board != null ? 110f : 16f;
            float padY = board != null ? 55f : 16f;

            BuildScrollingReadout(panel.transform, padX, padY);
            BuildFieldGuideButton(panel.transform, theme);

            fieldGuide = DescriptionBoard.Create(canvas.transform, theme);

            // Nothing to show until a crop is inspected.
            panelRoot.SetActive(false);
        }

        /// <summary>
        /// The readout, as scroll content rather than a fixed label.
        ///
        /// The label IS the content: stretched horizontal anchors give it exactly
        /// the viewport's width, and the size fitter reports its real height to the
        /// scroll rect. Overflow rather than Truncate is the part that matters -
        /// a truncating label drops whole lines once the font grows, and the
        /// player has no way to know anything is missing.
        /// </summary>
        private void BuildScrollingReadout(Transform parent, float padX, float padY)
        {
            GameObject viewport = new GameObject("Viewport",
                typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(parent, false);

            RectTransform viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.pivot = new Vector2(0.5f, 0.5f);
            viewRect.offsetMin = new Vector2(padX, padY);
            viewRect.offsetMax = new Vector2(-padX, -padY);

            GameObject txt = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txt.transform.SetParent(viewport.transform, false);

            RectTransform tRect = txt.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = Vector2.zero;
            tRect.sizeDelta = Vector2.zero;

            createdText = txt.GetComponent<Text>();
            createdText.font = GameFonts.Primary;
            createdText.fontSize = 19;
            createdText.alignment = TextAnchor.UpperLeft;
            createdText.color = Color.white;
            createdText.fontStyle = FontStyle.Bold;
            createdText.horizontalOverflow = HorizontalWrapMode.Wrap;
            createdText.verticalOverflow = VerticalWrapMode.Overflow;
            createdText.text = "";

            ContentSizeFitter fitter = txt.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll = parent.gameObject.AddComponent<ScrollRect>();
            scroll.content = tRect;
            scroll.viewport = viewRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.scrollSensitivity = 28f;
            scroll.inertia = true;
        }

        /// <summary>
        /// The button under the board that explains what the numbers mean. Built
        /// through <see cref="DescriptionBoard"/> so it is the same plank the shop
        /// and the district map use.
        /// </summary>
        private void BuildFieldGuideButton(Transform parent, UIThemeSprites theme)
        {
            Button button = DescriptionBoard.CreateOpenButton(
                parent, theme, "CROP INFORMATION DESCRIPTION",
                new Vector2(ButtonWidth, ButtonHeight),
                ShowFieldGuide);

            // Hangs just under the board's bottom edge, centred on it.
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f);
        }

        private void ShowFieldGuide()
        {
            if (fieldGuide != null)
                fieldGuide.Show(CropFieldDescriptions.Title, CropFieldDescriptions.Body);
        }

        /// <summary>Fills the panel and reveals it.</summary>
        public void ShowInfo(string text)
        {
            // The beginner guide keeps this shut until it teaches inspection.
            if (TutorialState.CropInspectionLocked)
                return;

            if (createdText != null)
                createdText.text = text;

            if (panelRoot != null)
                panelRoot.SetActive(true);

            // A new crop starts at the top rather than wherever the last readout
            // was left scrolled to.
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
        }

        /// <summary>Hides the panel until the next crop inspection.</summary>
        public void HideInfo()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            // The field guide is parented to the canvas rather than the panel, so
            // that it can centre on the screen instead of on a panel pinned to the
            // left edge. That means it does not inherit the panel's hiding, and
            // has to be closed here or it would outlive the board it explains.
            if (fieldGuide != null)
                fieldGuide.Hide();
        }
    }
}
