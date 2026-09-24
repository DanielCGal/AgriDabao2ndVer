using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class FarmConfirmPopup : MonoBehaviour
    {
        public static FarmConfirmPopup Instance { get; private set; }

        private Canvas canvas;
        private GameObject panel;
        private Text messageText;
        private Action yesAction;
        private Action noAction;

        private GameObject titleSign;
        private Image titleSignImage;

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

        public void Show(string message, Action onYes, Sprite titleArt = null, Action onNo = null)
        {
            yesAction = onYes;
            noAction = onNo;

            if (titleSign != null)
            {
                titleSign.SetActive(titleArt != null);
                if (titleArt != null)
                    titleSignImage.sprite = titleArt;
            }

            HudRegistry.CloseOtherPanels(HudPiece.ConfirmPopup);

            messageText.text = message;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            yesAction = null;
            noAction = null;

            if (panel != null)
                panel.SetActive(false);
        }

        private void ConfirmNo()
        {
            Action action = noAction;

            Hide();

            action?.Invoke();
        }

        private void ConfirmYes()
        {
            Action action = yesAction;

            Hide();

            action?.Invoke();
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void EnsureCanvas()
        {
            canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();

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
            UIThemeSprites theme = UIThemeSprites.Instance;
            Sprite board = theme?.saveFarmBoard;

            panel = new GameObject("FarmConfirmPopup", typeof(RectTransform), typeof(Image));
            HudRegistry.RegisterPiece(HudPiece.ConfirmPopup, panel);
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = board != null ? new Vector2(900f, 300f) : new Vector2(800f, 260f);
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
                bg.color = new Color(0f, 0f, 0f, 0.82f);
            }

            titleSign = new GameObject("TitleSign", typeof(RectTransform), typeof(Image));
            titleSign.transform.SetParent(panel.transform, false);

            RectTransform signRect = titleSign.GetComponent<RectTransform>();
            signRect.anchorMin = signRect.anchorMax = new Vector2(0.5f, 1f);
            signRect.pivot = new Vector2(0.5f, 0.5f);
            signRect.sizeDelta = new Vector2(520f, 140f);
            signRect.anchoredPosition = new Vector2(0f, 10f);

            titleSignImage = titleSign.GetComponent<Image>();
            titleSignImage.preserveAspect = true;
            titleSignImage.raycastTarget = false;
            titleSign.SetActive(false);

            GameObject textGo = new GameObject("Message", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.38f);
            textRect.anchorMax = new Vector2(1f, 0.86f);
            textRect.offsetMin = new Vector2(board != null ? 90f : 40f, 0f);
            textRect.offsetMax = new Vector2(board != null ? -90f : -40f, 0f);

            messageText = textGo.GetComponent<Text>();
            messageText.font = GameFonts.Primary;
            messageText.fontSize = 25;
            messageText.fontStyle = FontStyle.Bold;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.color = Color.white;

            float buttonY = board != null ? -70f : -62f;
            Button yes = CreateButton(
                panel.transform, "Yes", new Vector2(-120f, buttonY),
                new Color(0.20f, 0.55f, 0.20f, 0.95f), theme?.confirmYesButton);

            Button no = CreateButton(
                panel.transform, "No", new Vector2(120f, buttonY),
                new Color(0.55f, 0.16f, 0.16f, 0.95f), theme?.confirmNoButton);

            yes.onClick.AddListener(ConfirmYes);
            no.onClick.AddListener(ConfirmNo);
        }

        private Button CreateButton(Transform parent, string label, Vector2 pos, Color color, Sprite art)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = art != null ? new Vector2(190f, 62f) : new Vector2(180f, 58f);
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

            bg.color = color;

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
