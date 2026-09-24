using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class SettingsUIBuilder : MonoBehaviour
    {
        public static SettingsUIBuilder Instance { get; private set; }

        private Canvas canvas;
        private GameObject root;
        private GameObject savePrompt;

        private Slider musicSlider;
        private Slider sfxSlider;
        private Slider ambienceSlider;
        private Slider renderSlider;
        private Slider uiScaleSlider;
        private Slider textScaleSlider;

        private Text musicValue;
        private Text sfxValue;
        private Text ambienceValue;
        private Text renderValue;
        private Text uiScaleValue;
        private Text textScaleValue;

        private Text aiSummarizationValue;
        private GameObject aiConfirmPopup;
        private Text aiConfirmMessage;

        private float baseMusic, baseSfx, baseAmbience, baseRender;
        private float baseUiScale, baseTextScale;
        private bool baseAiSummarization;

        private UIThemeSprites theme;

        private const float PanelWidth = 1200f;

        private const float PanelHeight = 740f;
        private const float FirstRowY = -150f;

        private const float RowSpacing = 64f;
        private const float CloseButtonY = -625f;

        private const int AiRowIndex = 6;

        private static float RowY(int index)
        {
            return FirstRowY - RowSpacing * index;
        }

        private const float SliderGrooveLeftInset = 40f;
        private const float SliderGrooveRightInset = 44f;

        private const float SliderKnobSize = 48f;

        private const float ToggleButtonSize = 60f;
        private const float SliderBarHeight = 56f;

        private const float LabelColumnWidth = 280f;
        private const float ValueColumnWidth = 95f;
        private float PaddingX => theme != null ? theme.panelPaddingX : 120f;
        private float LabelHeight => theme != null ? theme.labelHeight : 150f;
        private float LabelOffsetY => theme != null ? theme.labelOffsetY : -12f;
        private Vector2 WideButtonSize => theme != null ? theme.wideButtonSize : new Vector2(320f, 95f);

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
            baseMusic = GameSettings.MusicVolume;
            baseSfx = GameSettings.SfxVolume;
            baseAmbience = GameSettings.AmbienceVolume;
            baseRender = GameSettings.RenderDistance;
            baseUiScale = GameSettings.UiScale;
            baseTextScale = GameSettings.TextScale;
            baseAiSummarization = GameSettings.AiSummarization;

            musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            ambienceSlider.SetValueWithoutNotify(GameSettings.AmbienceVolume);
            renderSlider.SetValueWithoutNotify(GameSettings.RenderDistance);
            uiScaleSlider.SetValueWithoutNotify(GameSettings.UiScale);
            textScaleSlider.SetValueWithoutNotify(GameSettings.TextScale);
            RefreshValueLabels();

            savePrompt.SetActive(false);
            aiConfirmPopup.SetActive(false);
            root.SetActive(true);
            root.transform.SetAsLastSibling();

            UIScaleService.ApplyNow();
        }

        private void Build()
        {
            theme = UIThemeSprites.Instance;

            root = new GameObject("Settings_Root", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.GetComponent<RectTransform>());
            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);
            blocker.raycastTarget = true;

            GameObject panel = CreatePanel(root.transform, "SettingsPanel", new Vector2(PanelWidth, PanelHeight));
            RectTransform panelRect = panel.GetComponent<RectTransform>();

            CreateHeading(panel.transform, "Settings", theme?.settingsLabel);

            musicSlider = CreateSliderRow(panel.transform, "BG Music", RowY(0), 0f, 1f,
                GameSettings.MusicVolume, out musicValue,
                v => { GameSettings.SetMusicVolume(v); RefreshValueLabels(); });

            sfxSlider = CreateSliderRow(panel.transform, "SFX", RowY(1), 0f, 1f,
                GameSettings.SfxVolume, out sfxValue,
                v => { GameSettings.SetSfxVolume(v); RefreshValueLabels(); });

            ambienceSlider = CreateSliderRow(panel.transform, "BG SFX (Weather)", RowY(2), 0f, 1f,
                GameSettings.AmbienceVolume, out ambienceValue,
                v => { GameSettings.SetAmbienceVolume(v); RefreshValueLabels(); });

            renderSlider = CreateSliderRow(panel.transform, "Render Distance",
                RowY(3), GameSettings.RenderMin, GameSettings.RenderMax,
                GameSettings.RenderDistance, out renderValue,
                v => { GameSettings.SetRenderDistance(v); RefreshValueLabels(); });

            uiScaleSlider = CreateSliderRow(panel.transform, "UI Size",
                RowY(4), GameSettings.UiScaleMin, GameSettings.UiScaleMax,
                GameSettings.UiScale, out uiScaleValue,
                v => { GameSettings.SetUiScale(v); RefreshValueLabels(); });

            textScaleSlider = CreateSliderRow(panel.transform, "Text Size",
                RowY(5), GameSettings.TextScaleMin, GameSettings.TextScaleMax,
                GameSettings.TextScale, out textScaleValue,
                v => { GameSettings.SetTextScale(v); RefreshValueLabels(); });

            CreateToggleRow(panel.transform, "AI Summarization", RowY(AiRowIndex),
                out aiSummarizationValue, OnAiSummarizationPressed);

            CreateButton(panel.transform, "Close", new Vector2(0f, CloseButtonY), WideButtonSize,
                theme?.closeButton, OnClosePressed);

            BuildSavePrompt(panelRect);
            BuildAiConfirmPopup(panelRect);
        }

        private void CreateHeading(Transform parent, string value, Sprite sprite)
        {
            if (sprite != null)
            {
                GameObject go = new GameObject("HeadingLabel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);

                RectTransform signRect = go.GetComponent<RectTransform>();
                signRect.anchorMin = signRect.anchorMax = new Vector2(0.5f, 1f);
                signRect.pivot = new Vector2(0.5f, 0.5f);
                signRect.sizeDelta = new Vector2(PanelWidth - 60f, LabelHeight);
                signRect.anchoredPosition = new Vector2(0f, LabelOffsetY);

                Image image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return;
            }

            Text title = CreateText(parent, "Title", 40, TextAnchor.MiddleCenter);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(700f, 80f);
            titleRect.anchoredPosition = new Vector2(0f, -30f);
            title.text = value;
        }

        private void BuildSavePrompt(RectTransform parent)
        {
            Sprite board = theme?.saveSettingsBoard;

            savePrompt = new GameObject("SavePrompt", typeof(RectTransform), typeof(Image));
            savePrompt.transform.SetParent(parent, false);
            RectTransform rect = savePrompt.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = board != null ? new Vector2(700f, 280f) : new Vector2(560f, 240f);
            rect.anchoredPosition = Vector2.zero;

            Image promptBg = savePrompt.GetComponent<Image>();
            if (board != null)
            {
                promptBg.sprite = board;
                promptBg.type = Image.Type.Sliced;
                promptBg.color = Color.white;
            }
            else
            {
                promptBg.color = new Color(0.05f, 0.1f, 0.06f, 0.99f);
            }

            if (theme?.saveSettingsLabel != null)
            {
                GameObject signGo = new GameObject("TitleSign", typeof(RectTransform), typeof(Image));
                signGo.transform.SetParent(savePrompt.transform, false);

                RectTransform signRect = signGo.GetComponent<RectTransform>();
                signRect.anchorMin = signRect.anchorMax = new Vector2(0.5f, 1f);
                signRect.pivot = new Vector2(0.5f, 0.5f);
                signRect.sizeDelta = new Vector2(480f, 130f);
                signRect.anchoredPosition = new Vector2(0f, 8f);

                Image signImage = signGo.GetComponent<Image>();
                signImage.sprite = theme.saveSettingsLabel;
                signImage.preserveAspect = true;
                signImage.raycastTarget = false;
            }

            Text message = CreateText(savePrompt.transform, "Message", 26, TextAnchor.MiddleCenter);
            RectTransform msgRect = message.rectTransform;
            msgRect.anchorMin = new Vector2(0f, 1f);
            msgRect.anchorMax = new Vector2(1f, 1f);
            msgRect.pivot = new Vector2(0.5f, 1f);
            msgRect.sizeDelta = new Vector2(board != null ? -140f : -40f, 90f);
            msgRect.anchoredPosition = new Vector2(0f, board != null ? -95f : -30f);
            message.fontStyle = FontStyle.Bold;
            message.text = "Save Setting Changes?";

            CreateButton(savePrompt.transform, "Yes", new Vector2(-130f, 55f), new Vector2(200f, 70f),
                theme?.confirmYesButton, OnSaveYes);
            CreateButton(savePrompt.transform, "No", new Vector2(130f, 55f), new Vector2(200f, 70f),
                theme?.confirmNoButton, OnSaveNo);
            SetButtonAnchorBottom(savePrompt.transform.Find("Yes") as RectTransform);
            SetButtonAnchorBottom(savePrompt.transform.Find("No") as RectTransform);

            savePrompt.SetActive(false);
        }

        private void OnClosePressed()
        {
            if (aiConfirmPopup != null && aiConfirmPopup.activeSelf)
                return;

            if (HasChanges())
                savePrompt.SetActive(true);
            else
                root.SetActive(false);
        }

        private void OnSaveYes()
        {
            GameSettings.SaveLocal();

            AuthSession.Instance?.SaveSettingsToAccount();

            root.SetActive(false);
        }

        private void OnSaveNo()
        {
            RestoreBaseline();
            root.SetActive(false);
        }

        private void RestoreBaseline()
        {
            GameSettings.SetMusicVolume(baseMusic);
            GameSettings.SetSfxVolume(baseSfx);
            GameSettings.SetAmbienceVolume(baseAmbience);
            GameSettings.SetRenderDistance(baseRender);
            GameSettings.SetUiScale(baseUiScale);
            GameSettings.SetTextScale(baseTextScale);
            GameSettings.SetAiSummarization(baseAiSummarization);

            musicSlider.SetValueWithoutNotify(baseMusic);
            sfxSlider.SetValueWithoutNotify(baseSfx);
            ambienceSlider.SetValueWithoutNotify(baseAmbience);
            renderSlider.SetValueWithoutNotify(baseRender);
            uiScaleSlider.SetValueWithoutNotify(baseUiScale);
            textScaleSlider.SetValueWithoutNotify(baseTextScale);
            RefreshValueLabels();
        }

        private bool HasChanges()
        {
            return !Mathf.Approximately(baseMusic, GameSettings.MusicVolume)
                || !Mathf.Approximately(baseSfx, GameSettings.SfxVolume)
                || !Mathf.Approximately(baseAmbience, GameSettings.AmbienceVolume)
                || !Mathf.Approximately(baseRender, GameSettings.RenderDistance)
                || !Mathf.Approximately(baseUiScale, GameSettings.UiScale)
                || !Mathf.Approximately(baseTextScale, GameSettings.TextScale)
                || baseAiSummarization != GameSettings.AiSummarization;
        }

        private void RefreshValueLabels()
        {
            musicValue.text = Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%";
            sfxValue.text = Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%";
            ambienceValue.text = Mathf.RoundToInt(GameSettings.AmbienceVolume * 100f) + "%";
            renderValue.text = Mathf.RoundToInt(GameSettings.RenderDistance) + " m";
            uiScaleValue.text = Mathf.RoundToInt(GameSettings.UiScale * 100f) + "%";
            textScaleValue.text = Mathf.RoundToInt(GameSettings.TextScale * 100f) + "%";
            aiSummarizationValue.text = GameSettings.AiSummarization ? "On" : "Off";
        }

        private void CreateToggleRow(Transform parent, string label, float y,
            out Text stateText, UnityEngine.Events.UnityAction onPressed)
        {
            const float labelWidth = LabelColumnWidth;
            const float valueWidth = ValueColumnWidth;
            float innerLeft = -PanelWidth * 0.5f + PaddingX;
            float innerRight = PanelWidth * 0.5f - PaddingX;

            float controlLeft = innerLeft + labelWidth + 10f;
            float controlRight = innerRight - valueWidth - 20f;
            float controlCentre = controlLeft + (controlRight - controlLeft) * 0.5f;

            Text labelText = CreateText(parent, label + "_Label", 24, TextAnchor.MiddleLeft);
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(labelWidth, 60f);
            labelRect.anchoredPosition = new Vector2(innerLeft + labelWidth * 0.5f, y);
            labelText.text = label;
            FitToRow(labelText);

            GameObject go = new GameObject(label + "_Toggle",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(ToggleButtonSize, ToggleButtonSize);
            rect.anchoredPosition = new Vector2(controlCentre, y);

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onPressed);

            Sprite knob = theme?.sliderKnob;
            if (knob != null)
            {
                image.sprite = knob;
                image.preserveAspect = true;
                image.color = Color.white;

                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
                colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
                colors.selectedColor = Color.white;
                button.colors = colors;
            }
            else
            {
                image.color = new Color(0.12f, 0.55f, 0.20f, 1f);
            }

            stateText = CreateText(go.transform, "State", 22, TextAnchor.MiddleCenter);
            stateText.color = new Color(0.20f, 0.12f, 0.04f, 1f);
            stateText.fontStyle = FontStyle.Bold;
            stateText.raycastTarget = false;
            Stretch(stateText.rectTransform);
        }

        private void BuildAiConfirmPopup(RectTransform parent)
        {
            Sprite board = theme?.tradeRequestBoard;

            aiConfirmPopup = new GameObject("AiSummarizationPrompt",
                typeof(RectTransform), typeof(Image));
            aiConfirmPopup.transform.SetParent(parent, false);

            RectTransform rect = aiConfirmPopup.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = board != null ? new Vector2(760f, 300f) : new Vector2(560f, 240f);
            rect.anchoredPosition = Vector2.zero;

            Image background = aiConfirmPopup.GetComponent<Image>();
            if (board != null)
            {
                background.sprite = board;
                background.type = Image.Type.Sliced;
                background.color = Color.white;
            }
            else
            {
                background.color = new Color(0.05f, 0.1f, 0.06f, 0.99f);
            }

            aiConfirmMessage = CreateText(aiConfirmPopup.transform, "Message", 26,
                TextAnchor.MiddleCenter);
            RectTransform messageRect = aiConfirmMessage.rectTransform;
            messageRect.anchorMin = new Vector2(0f, 0.34f);
            messageRect.anchorMax = new Vector2(1f, 0.86f);
            messageRect.offsetMin = new Vector2(70f, 0f);
            messageRect.offsetMax = new Vector2(-70f, 0f);
            aiConfirmMessage.fontStyle = FontStyle.Bold;
            aiConfirmMessage.color = new Color(0.20f, 0.12f, 0.04f, 1f);
            aiConfirmMessage.horizontalOverflow = HorizontalWrapMode.Wrap;
            aiConfirmMessage.verticalOverflow = VerticalWrapMode.Overflow;

            CreateButton(aiConfirmPopup.transform, "AiYes", new Vector2(-130f, 55f),
                new Vector2(200f, 70f), theme?.confirmYesButton, OnAiSummarizationConfirmed);
            CreateButton(aiConfirmPopup.transform, "AiNo", new Vector2(130f, 55f),
                new Vector2(200f, 70f), theme?.confirmNoButton, OnAiSummarizationDeclined);
            SetButtonAnchorBottom(aiConfirmPopup.transform.Find("AiYes") as RectTransform);
            SetButtonAnchorBottom(aiConfirmPopup.transform.Find("AiNo") as RectTransform);

            aiConfirmPopup.SetActive(false);
        }

        private void OnAiSummarizationPressed()
        {
            aiConfirmMessage.text = GameSettings.AiSummarization
                ? "Do you want to turn off “AI Summarization”?"
                : "Do you want to turn on “AI Summarization”?";

            aiConfirmPopup.SetActive(true);
            aiConfirmPopup.transform.SetAsLastSibling();
        }

        private void OnAiSummarizationConfirmed()
        {
            GameSettings.SetAiSummarization(!GameSettings.AiSummarization);
            aiConfirmPopup.SetActive(false);
            RefreshValueLabels();
        }

        private void OnAiSummarizationDeclined()
        {
            aiConfirmPopup.SetActive(false);
        }

        private Slider CreateSliderRow(Transform parent, string label, float y,
            float min, float max, float value, out Text valueText,
            UnityEngine.Events.UnityAction<float> onChanged)
        {
            const float labelWidth = LabelColumnWidth;
            const float valueWidth = ValueColumnWidth;
            float innerLeft = -PanelWidth * 0.5f + PaddingX;
            float innerRight = PanelWidth * 0.5f - PaddingX;

            float sliderLeft = innerLeft + labelWidth + 10f;
            float sliderRight = innerRight - valueWidth - 20f;
            float sliderWidth = sliderRight - sliderLeft;

            Text labelText = CreateText(parent, label + "_Label", 24, TextAnchor.MiddleLeft);
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(labelWidth, 60f);
            labelRect.anchoredPosition = new Vector2(innerLeft + labelWidth * 0.5f, y);
            labelText.text = label;
            FitToRow(labelText);

            valueText = CreateText(parent, label + "_Value", 24, TextAnchor.MiddleRight);
            RectTransform valRect = valueText.rectTransform;
            valRect.anchorMin = valRect.anchorMax = new Vector2(0.5f, 1f);
            valRect.pivot = new Vector2(0.5f, 1f);
            valRect.sizeDelta = new Vector2(valueWidth, 60f);
            valRect.anchoredPosition = new Vector2(innerRight - valueWidth * 0.5f, y);
            FitToRow(valueText);

            Slider slider = CreateSlider(parent, label + "_Slider", min, max, value);
            RectTransform sliderRect = slider.GetComponent<RectTransform>();
            sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 1f);
            sliderRect.sizeDelta = new Vector2(sliderWidth, SliderBarHeight);
            sliderRect.anchoredPosition = new Vector2(sliderLeft + sliderWidth * 0.5f, y - 2f);
            slider.onValueChanged.AddListener(onChanged);

            return slider;
        }

        private Slider CreateSlider(Transform parent, string name, float min, float max, float value)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);

            Sprite barSprite = theme?.sliderBar;
            Sprite knobSprite = theme?.sliderKnob;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(go.transform, false);
            Stretch(background.GetComponent<RectTransform>());
            Image backgroundImage = background.GetComponent<Image>();
            if (barSprite != null)
            {
                backgroundImage.sprite = barSprite;
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.color = Color.white;
            }
            else
            {
                backgroundImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            }

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(8f, 0f);
            fillAreaRect.offsetMax = new Vector2(-8f, 0f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(10f, 0f);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = barSprite != null
                ? new Color(0f, 0f, 0f, 0f)
                : new Color(0.20f, 0.70f, 0.30f, 1f);

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            Stretch(handleAreaRect);

            float knobHalf = (knobSprite != null ? SliderKnobSize : 28f) * 0.5f;
            handleAreaRect.offsetMin = new Vector2(SliderGrooveLeftInset + knobHalf + 2f, 0f);
            handleAreaRect.offsetMax = new Vector2(-(SliderGrooveRightInset + knobHalf + 2f), 0f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            Image handleImage = handle.GetComponent<Image>();
            if (knobSprite != null)
            {
                handleRect.sizeDelta = new Vector2(SliderKnobSize, SliderKnobSize);
                handleImage.sprite = knobSprite;
                handleImage.preserveAspect = true;
                handleImage.color = Color.white;
            }
            else
            {
                handleRect.sizeDelta = new Vector2(28f, 40f);
                handleImage.color = Color.white;
            }

            Slider slider = go.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.value = value;

            return slider;
        }

        private void CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size,
            Sprite sprite, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.color = Color.white;

                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
                colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
                colors.selectedColor = Color.white;
                button.colors = colors;
                return;
            }

            image.color = new Color(0.12f, 0.55f, 0.20f, 1f);
            Text text = CreateText(go.transform, "Text", 26, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.rectTransform);
        }

        private static void SetButtonAnchorBottom(RectTransform rect)
        {
            if (rect == null)
                return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            Sprite board = theme?.panelBoard;
            if (board != null)
            {
                image.sprite = board;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.03f, 0.08f, 0.04f, 0.97f);
            }

            return panel;
        }

        private static void FitToRow(Text text)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = text.fontSize;
            text.resizeTextMinSize = 14;
        }

        private static Text CreateText(Transform parent, string name, int size, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private void EnsureCanvas()
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
                return;

            GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
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
