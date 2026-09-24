using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class PestDiseasePopupUI : MonoBehaviour
    {
        public static PestDiseasePopupUI Instance { get; private set; }

        private Canvas canvas;
        private GameObject panel;
        private Text messageText;

        private void Awake()
        {
            Instance = this;

            EnsureEventSystem();
            EnsureCanvas();

            Build();
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Show(string message)
        {
            if (panel == null)
                Build();

            messageText.text = message;
            panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void EnsureCanvas()
        {
            canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas != null)
                return;

            GameObject canvasGo = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
        }

        private void Build()
        {
            panel = new GameObject("PestDiseasePopup", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            UIThemeSprites theme = UIThemeSprites.Instance;
            Sprite board = theme?.pestDiseaseBoard;

            rect.sizeDelta = board != null ? new Vector2(1120f, 380f) : new Vector2(920f, 290f);
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
                bg.color = new Color(0f, 0f, 0f, 0.78f);
            }

            if (theme?.pestDiseaseLabel != null)
            {
                GameObject signGo = new GameObject("TitleSign", typeof(RectTransform), typeof(Image));
                signGo.transform.SetParent(panel.transform, false);

                RectTransform signRect = signGo.GetComponent<RectTransform>();
                signRect.anchorMin = signRect.anchorMax = new Vector2(0.5f, 1f);
                signRect.pivot = new Vector2(0.5f, 0.5f);
                signRect.sizeDelta = new Vector2(620f, 150f);
                signRect.anchoredPosition = new Vector2(0f, 8f);

                Image signImage = signGo.GetComponent<Image>();
                signImage.sprite = theme.pestDiseaseLabel;
                signImage.preserveAspect = true;
                signImage.raycastTarget = false;
            }

            GameObject textGo = new GameObject("Message", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.32f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(board != null ? 90f : 40f, 8f);
            textRect.offsetMax = new Vector2(board != null ? -90f : -40f, board != null ? -95f : -24f);

            messageText = textGo.GetComponent<Text>();
            messageText.font = GameFonts.Primary;
            messageText.fontSize = board != null ? 24 : 28;
            messageText.fontStyle = board != null ? FontStyle.Bold : FontStyle.Normal;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.color = board != null ? new Color(0.20f, 0.12f, 0.04f, 1f) : Color.white;

            Sprite okArt = theme?.pestDiseaseOkayButton != null
                ? theme.pestDiseaseOkayButton
                : theme?.climateOkayButton;

            Button okButton = CreateButton(panel.transform, "Okay",
                new Vector2(0f, board != null ? -100f : -80f),
                board != null ? 230f : 200f, board != null ? 66f : 60f, okArt);
            okButton.onClick.AddListener(Hide);
        }

        private Button CreateButton(Transform parent, string label, Vector2 pos,
            float width, float height, Sprite art = null)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = pos;

            Image bg = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;

            if (art != null)
            {
                bg.sprite = art;
                bg.preserveAspect = true;
                bg.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
                return button;
            }

            bg.color = new Color(0.20f, 0.55f, 0.20f, 0.95f);

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

            return button;
        }
    }
}
