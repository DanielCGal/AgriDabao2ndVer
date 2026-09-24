using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class FarmChoicePopup : MonoBehaviour
    {
        public struct Choice
        {
            public string Label;
            public string Hint;
            public Action OnChoose;

            public Choice(string label, string hint, Action onChoose)
            {
                Label = label;
                Hint = hint;
                OnChoose = onChoose;
            }
        }

        private static FarmChoicePopup instance;

        public static FarmChoicePopup Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = FindFirstObjectByType<FarmChoicePopup>();
                if (instance == null)
                    instance = new GameObject("FarmChoicePopup").AddComponent<FarmChoicePopup>();

                return instance;
            }
        }

        public bool IsOpen => panel != null && panel.activeSelf;

        private const int MaxChoices = 3;
        private const float PanelWidth = 1040f;
        private const float PanelHeight = 500f;

        private Canvas canvas;
        private GameObject panel;
        private Text messageText;
        private readonly List<Button> choiceButtons = new List<Button>();
        private readonly List<Text> choiceLabels = new List<Text>();
        private readonly List<Text> choiceHints = new List<Text>();
        private readonly List<Action> choiceActions = new List<Action>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureEventSystem();
            EnsureCanvas();
            Build();
            Hide();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void Show(string message, IList<Choice> choices)
        {
            if (panel == null)
                return;

            messageText.text = message;
            choiceActions.Clear();

            int count = choices != null ? Mathf.Min(choices.Count, MaxChoices) : 0;
            float spacing = 320f;
            float startX = -(count - 1) * spacing * 0.5f;

            for (int i = 0; i < MaxChoices; i++)
            {
                bool used = i < count;
                choiceButtons[i].gameObject.SetActive(used);
                choiceHints[i].gameObject.SetActive(used);

                if (!used)
                {
                    choiceActions.Add(null);
                    continue;
                }

                Choice choice = choices[i];
                choiceLabels[i].text = choice.Label;
                choiceHints[i].text = choice.Hint ?? string.Empty;
                choiceActions.Add(choice.OnChoose);

                float x = startX + i * spacing;
                ((RectTransform)choiceButtons[i].transform).anchoredPosition = new Vector2(x, 6f);
                choiceHints[i].rectTransform.anchoredPosition = new Vector2(x, -78f);
            }

            HudRegistry.CloseOtherPanels(HudPiece.ConfirmPopup);
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            choiceActions.Clear();

            if (panel != null)
                panel.SetActive(false);
        }

        private void Choose(int index)
        {
            Action action = index >= 0 && index < choiceActions.Count ? choiceActions[index] : null;
            Hide();
            action?.Invoke();
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void EnsureCanvas()
        {
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
                return;

            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

            panel = new GameObject("FarmChoicePopup", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
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
                background.color = new Color(0f, 0f, 0f, 0.82f);
            }

            GameObject textGo = new GameObject("Message", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.66f);
            textRect.anchorMax = new Vector2(1f, 0.9f);
            textRect.offsetMin = new Vector2(board != null ? 110f : 40f, 0f);
            textRect.offsetMax = new Vector2(board != null ? -110f : -40f, 0f);

            messageText = textGo.GetComponent<Text>();
            messageText.font = GameFonts.Primary;
            messageText.fontSize = 26;
            messageText.fontStyle = FontStyle.Bold;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.color = Color.white;
            messageText.resizeTextForBestFit = true;
            messageText.resizeTextMinSize = 16;
            messageText.resizeTextMaxSize = 26;

            Sprite plank = theme?.tradeRequestBoard;
            for (int i = 0; i < MaxChoices; i++)
            {
                int index = i;
                Button button = UIPlank.CreateButton(panel.transform, "Choice" + i, plank, true,
                    new Vector2(290f, 96f), Vector2.zero, string.Empty, 24, () => Choose(index));

                RectTransform buttonRect = (RectTransform)button.transform;
                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);

                choiceButtons.Add(button);
                choiceLabels.Add(button.GetComponentInChildren<Text>());

                GameObject hintGo = new GameObject("Hint" + i, typeof(RectTransform), typeof(Text));
                hintGo.transform.SetParent(panel.transform, false);
                RectTransform hintRect = hintGo.GetComponent<RectTransform>();
                hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0.5f);
                hintRect.pivot = new Vector2(0.5f, 0.5f);
                hintRect.sizeDelta = new Vector2(300f, 50f);

                Text hint = hintGo.GetComponent<Text>();
                hint.font = GameFonts.Primary;
                hint.fontSize = 18;
                hint.alignment = TextAnchor.UpperCenter;
                hint.horizontalOverflow = HorizontalWrapMode.Wrap;
                hint.verticalOverflow = VerticalWrapMode.Truncate;
                hint.resizeTextForBestFit = true;
                hint.resizeTextMinSize = 13;
                hint.resizeTextMaxSize = 18;
                hint.color = new Color(1f, 0.95f, 0.82f, 1f);
                hint.raycastTarget = false;
                choiceHints.Add(hint);
            }

            Sprite backArt = theme?.verifyBackButton;
            GameObject backGo = new GameObject("Back", typeof(RectTransform), typeof(Image), typeof(Button));
            backGo.transform.SetParent(panel.transform, false);
            RectTransform backRect = backGo.GetComponent<RectTransform>();
            backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.sizeDelta = new Vector2(220f, 76f);
            backRect.anchoredPosition = new Vector2(0f, 30f);

            Image backImage = backGo.GetComponent<Image>();
            Button back = backGo.GetComponent<Button>();
            back.targetGraphic = backImage;
            back.onClick.AddListener(Hide);

            if (backArt != null)
            {
                backImage.sprite = backArt;
                backImage.preserveAspect = true;
                backImage.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(back);
            }
            else
            {
                backImage.color = new Color(0.55f, 0.16f, 0.16f, 0.95f);
                GameObject labelGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(backGo.transform, false);
                RectTransform labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                Text label = labelGo.GetComponent<Text>();
                label.font = GameFonts.Primary;
                label.fontSize = 24;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.text = "Back";
            }
        }
    }
}
