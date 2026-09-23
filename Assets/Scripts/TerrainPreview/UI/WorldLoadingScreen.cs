using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class WorldLoadingScreen : MonoBehaviour
    {
        public static WorldLoadingScreen Instance { get; private set; }

        [Header("Background")]
        public Sprite backgroundSprite;

        private Canvas canvas;
        private GameObject root;
        private Text statusText;

        private bool isShowing;
        private readonly List<GameObject> hiddenGameplayUI = new List<GameObject>();

        private void Awake()
        {
            Instance = this;
            Build();
            Show("Preparing selected area...");
        }

        private void Update()
        {
            // This catches UI that gets created after the loading screen starts,
            // like InventoryUIBuilder and DevToolsUIBuilder.
            if (isShowing)
                HideGameplayUI();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Build()
        {
            canvas = Object.FindFirstObjectByType<Canvas>();

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

            root = new GameObject("WorldLoadingScreen_UI", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(root.transform, false);

            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            Stretch(bgRect);

            Image bg = bgGo.GetComponent<Image>();
            bg.sprite = backgroundSprite;
            bg.preserveAspect = false;
            bg.color = Color.white;

            GameObject dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(root.transform, false);

            RectTransform dimRect = dimGo.GetComponent<RectTransform>();
            Stretch(dimRect);

            Image dim = dimGo.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.45f);

            GameObject panelGo = new GameObject("LoadingPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(root.transform, false);

            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            UIThemeSprites theme = UIThemeSprites.Instance;
            Sprite board = theme?.loadingBoard;

            panelRect.sizeDelta = board != null ? new Vector2(980f, 300f) : new Vector2(900f, 260f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panel = panelGo.GetComponent<Image>();
            if (board != null)
            {
                panel.sprite = board;
                panel.type = Image.Type.Sliced;
                panel.color = Color.white;
            }
            else
            {
                panel.color = new Color(0f, 0f, 0f, 0.65f);
            }

            // Hanging "Generating Farm" sign; falls back to plain title text.
            if (theme?.loadingLabel != null)
            {
                GameObject signGo = new GameObject("TitleSign", typeof(RectTransform), typeof(Image));
                signGo.transform.SetParent(panelGo.transform, false);

                RectTransform signRect = signGo.GetComponent<RectTransform>();
                signRect.anchorMin = signRect.anchorMax = new Vector2(0.5f, 1f);
                signRect.pivot = new Vector2(0.5f, 0.5f);
                signRect.sizeDelta = new Vector2(560f, 150f);
                signRect.anchoredPosition = new Vector2(0f, 8f);

                Image signImage = signGo.GetComponent<Image>();
                signImage.sprite = theme.loadingLabel;
                signImage.preserveAspect = true;
                signImage.raycastTarget = false;
            }
            else
            {
                GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
                titleGo.transform.SetParent(panelGo.transform, false);

                RectTransform titleRect = titleGo.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.sizeDelta = new Vector2(0f, 80f);
                titleRect.anchoredPosition = new Vector2(0f, -30f);

                Text title = titleGo.GetComponent<Text>();
                title.font = GameFonts.Primary;
                title.fontSize = 42;
                title.alignment = TextAnchor.MiddleCenter;
                title.color = Color.white;
                title.text = "Generating Your Farm";
            }

            GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
            statusGo.transform.SetParent(panelGo.transform, false);

            RectTransform statusRect = statusGo.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.offsetMin = new Vector2(board != null ? 90f : 40f, 35f);
            statusRect.offsetMax = new Vector2(board != null ? -90f : -40f, board != null ? -95f : -110f);

            statusText = statusGo.GetComponent<Text>();
            statusText.font = GameFonts.Primary;
            statusText.fontSize = board != null ? 26 : 30;
            statusText.fontStyle = board != null ? FontStyle.Bold : FontStyle.Normal;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = board != null
                ? new Color(0.20f, 0.12f, 0.04f, 1f)
                : new Color(0.95f, 0.95f, 0.95f, 1f);
            statusText.text = "Loading...";
        }

        public void Show(string message)
        {
            isShowing = true;

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }

            HideGameplayUI();
            SetStatus(message);
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;

            if (root != null)
                root.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            isShowing = false;

            ShowGameplayUI();

            if (root != null)
                root.SetActive(false);
        }

        private void HideGameplayUI()
        {
            HideByName("InventoryUI");
            HideByName("MoneyText");
            HideByName("DayCounterText");
            HideByName("WeatherText");
            HideByName("WeatherUI");
            HideByName("MobileHud");
            HideByName("MoveJoystick");
            HideByName("JumpButton");
            HideByName("SoilInfoPanel");
        }

        private void HideByName(string objectName)
        {
            GameObject go = GameObject.Find(objectName);

            if (go == null)
                return;

            if (root != null && go.transform.IsChildOf(root.transform))
                return;

            if (!go.activeSelf)
                return;

            if (!hiddenGameplayUI.Contains(go))
                hiddenGameplayUI.Add(go);

            go.SetActive(false);
        }

        private void ShowGameplayUI()
        {
            for (int i = 0; i < hiddenGameplayUI.Count; i++)
            {
                if (hiddenGameplayUI[i] != null)
                    hiddenGameplayUI[i].SetActive(true);
            }

            hiddenGameplayUI.Clear();
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