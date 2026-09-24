using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class SoilInfoPanelBuilder : MonoBehaviour
    {
        public Text createdText;

        private GameObject panelRoot;
        private ScrollRect scroll;
        private DescriptionBoard fieldGuide;

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
                scaler.matchWidthOrHeight = 1f;
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

            float padX = board != null ? 110f : 16f;
            float padY = board != null ? 55f : 16f;

            BuildScrollingReadout(panel.transform, padX, padY);
            BuildFieldGuideButton(panel.transform, theme);

            fieldGuide = DescriptionBoard.Create(canvas.transform, theme);

            panelRoot.SetActive(false);
        }

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

        private void BuildFieldGuideButton(Transform parent, UIThemeSprites theme)
        {
            Button button = DescriptionBoard.CreateOpenButton(
                parent, theme, "CROP INFORMATION DESCRIPTION",
                new Vector2(ButtonWidth, ButtonHeight),
                ShowFieldGuide);

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

        public void ShowInfo(string text)
        {
            if (TutorialState.CropInspectionLocked)
                return;

            if (createdText != null)
                createdText.text = text;

            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
        }

        public void HideInfo()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (fieldGuide != null)
                fieldGuide.Hide();
        }
    }
}
