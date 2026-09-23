using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// Runtime Settings overlay opened from the Main Menu and the in-farm pause
    /// menu. Six horizontal sliders (BG music, SFX, weather ambience, render
    /// distance, interface size, text size) drive <see cref="GameSettings"/> live.
    /// Closing after a change prompts to save; Save persists locally and (if
    /// logged in) to the backend account, No reverts to the last saved values.
    /// </summary>
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

        // Widened from 1000 to pay for the larger knob. The knob has to stop half
        // its own width before each end of the groove, so a bigger knob costs travel
        // unless the bar grows with it. BackgroundUI.png's caps are 130, and content
        // is inset by panelPaddingX (170), so this still leaves 40 of clearance.
        private const float PanelWidth = 1200f;

        /// <summary>
        /// Panel height is unchanged at 740, and the rows were tightened from 90 to
        /// 72 apart to seat six of them instead of four rather than growing the
        /// board. That is a hard constraint, not a preference: the interface-size
        /// slider shrinks every canvas's reference resolution, so at the 1.20
        /// ceiling the usable height falls to 900. A 740-tall board centred there
        /// puts the top of its hanging "Settings" sign at 433 of the 450 available,
        /// and a taller board would push that sign off the screen - on the one
        /// panel the player needs in order to undo the setting.
        /// </summary>
        private const float PanelHeight = 740f;
        private const float FirstRowY = -150f;

        /// <summary>
        /// Tightened again from 72 to seat a seventh row. The board deliberately
        /// does not grow to make space: the height above is what keeps its hanging
        /// sign on screen at the 1.20 interface ceiling, so rows have to fit within
        /// it rather than the other way round.
        /// </summary>
        private const float RowSpacing = 64f;
        private const float CloseButtonY = -625f;

        /// <summary>Row index of the AI Summarization toggle - always the last one.</summary>
        private const int AiRowIndex = 6;

        private static float RowY(int index)
        {
            return FirstRowY - RowSpacing * index;
        }

        // Measured from SliderBar.png (840x72, border {40,0,40,0}): the grooved
        // channel runs from x=40 to x=796, i.e. exactly between the two rounded
        // caps. Because 9-slice caps render at native size, these insets hold at
        // any bar width.
        private const float SliderGrooveLeftInset = 40f;
        private const float SliderGrooveRightInset = 44f;

        /// <summary>
        /// Knob diameter. Sized as a phone touch target rather than to the art:
        /// 48 clears the ~44dp minimum most mobile guidelines use. SliderBarHeight
        /// must stay above this or the knob pokes out above and below the wood.
        /// </summary>
        private const float SliderKnobSize = 48f;

        /// <summary>
        /// The AI Summarization knob. Larger than the slider knob because it
        /// carries a word, and held under RowSpacing so it cannot reach the row
        /// above it.
        /// </summary>
        private const float ToggleButtonSize = 60f;
        private const float SliderBarHeight = 56f;

        /// <summary>
        /// Width of the label column on every row, and of the value column at the
        /// right end of the slider rows.
        ///
        /// Sized for the largest Text Size (115%), where these labels are drawn at
        /// 28 instead of 24. "BG SFX (Weather)" and "AI Summarization" measure about
        /// 250 there, and "100%" and "60 m" about 80, so at the old 240 and 70 they
        /// wrapped onto a second line the 60-tall row cannot show and the board
        /// read "BG SFX", "AI" and "100". The slider gives up the difference and
        /// still has over 300 of knob travel.
        /// </summary>
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
            // Snapshot the current values so we can detect changes on close.
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

            // The board is about to be looked at closely, so make sure it is already
            // at the player's chosen sizes rather than being corrected by the next
            // poll a fraction of a second later.
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

            // Both scale rows apply live. UIScaleService listens to GameSettings
            // and re-applies on the same frame, so the board under the player's
            // thumb resizes as the knob moves and they can judge the result on the
            // real interface rather than on a preview.
            uiScaleSlider = CreateSliderRow(panel.transform, "UI Size",
                RowY(4), GameSettings.UiScaleMin, GameSettings.UiScaleMax,
                GameSettings.UiScale, out uiScaleValue,
                v => { GameSettings.SetUiScale(v); RefreshValueLabels(); });

            textScaleSlider = CreateSliderRow(panel.transform, "Text Size",
                RowY(5), GameSettings.TextScaleMin, GameSettings.TextScaleMax,
                GameSettings.TextScale, out textScaleValue,
                v => { GameSettings.SetTextScale(v); RefreshValueLabels(); });

            // A toggle rather than a slider, so it borrows the slider row's geometry
            // but puts a single round knob button where the track would be.
            CreateToggleRow(panel.transform, "AI Summarization", RowY(AiRowIndex),
                out aiSummarizationValue, OnAiSummarizationPressed);

            CreateButton(panel.transform, "Close", new Vector2(0f, CloseButtonY), WideButtonSize,
                theme?.closeButton, OnClosePressed);

            BuildSavePrompt(panelRect);
            BuildAiConfirmPopup(panelRect);
        }

        /// <summary>The hanging "Settings" sign, or a plain Text when no sprite is set.</summary>
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

            // Hanging "Save Settings?" sign above the plank.
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
            // The confirm board sits over the panel; closing underneath it would
            // strand it on screen with nothing behind it.
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

            // Handed to AuthSession, which outlives this panel. Starting the request
            // here meant it died the moment the player left the scene, leaving the
            // account on its old values.
            AuthSession.Instance?.SaveSettingsToAccount();

            root.SetActive(false);
        }

        private void OnSaveNo()
        {
            // Discard the edits rather than merely skipping the write.
            //
            // The sliders apply their value live as they are dragged, so declining to
            // save previously left the new values sitting in GameSettings for the rest
            // of the session. Show() reads its slider positions from GameSettings, so
            // re-opening the panel showed the rejected values as though they had been
            // saved. Restoring the snapshot taken in Show() puts the audio, render
            // distance and slider positions back to the last saved state.
            RestoreBaseline();
            root.SetActive(false);
        }

        /// <summary>
        /// Returns every setting to the values captured when the panel was opened,
        /// which is the last saved state. Assigning through the GameSettings setters
        /// raises its Changed event, so the audio mixer and culling distance follow
        /// the revert immediately instead of waiting for a restart.
        /// </summary>
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

        // ---------------- UI helpers ----------------

        /// <summary>
        /// A label with a single round button where a slider's track would be.
        ///
        /// Laid out from the same measurements as <see cref="CreateSliderRow"/> so
        /// the label column lines up with the six rows above it, and the button
        /// lands on the centre of the control column rather than floating.
        /// </summary>
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
            // Kept inside the row spacing so the knob never overlaps the slider on
            // the row above it.
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

            // The word sits on the knob, so it is dark to read against the wood.
            stateText = CreateText(go.transform, "State", 22, TextAnchor.MiddleCenter);
            stateText.color = new Color(0.20f, 0.12f, 0.04f, 1f);
            stateText.fontStyle = FontStyle.Bold;
            stateText.raycastTarget = false;
            Stretch(stateText.rectTransform);
        }

        /// <summary>
        /// The "Do you want to turn ... on/off?" board, built from the same trade
        /// request art the social panel uses and the shared Yes/No buttons.
        /// </summary>
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

        /// <summary>Asks before flipping, so a mis-tap never silently changes it.</summary>
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
            // Applied straight away like the sliders. Closing the board still asks
            // whether to keep it, and answering No puts it back.
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
            // Lay the row out between the board's two log ends: label on the
            // left, then the grooved bar, then the live value on the right.
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
            // -2 rather than -10 so the taller bar's centre still lines up with the
            // 60-tall label beside it.
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
            // The wooden groove art already reads as the track, so the green
            // progress fill is hidden once the bar sprite is in use.
            fillImage.color = barSprite != null
                ? new Color(0f, 0f, 0f, 0f)
                : new Color(0.20f, 0.70f, 0.30f, 1f);

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            Stretch(handleAreaRect);

            // This rect is the knob's travel range, and Unity places the knob's
            // CENTRE on its edges - so at the old inset of 10 a 44-wide knob hung
            // 12 units past each end of the bar.
            //
            // SliderBar.png is 840x72 with a {40,0,40,0} border. Those caps are the
            // rounded wooden ends and are drawn at native size whatever the bar's
            // width, so the grooved channel between them always begins 40 in from
            // the left and 44 from the right. Insetting by that plus half the knob
            // keeps the knob inside the groove at both extremes.
            float knobHalf = (knobSprite != null ? SliderKnobSize : 28f) * 0.5f;
            handleAreaRect.offsetMin = new Vector2(SliderGrooveLeftInset + knobHalf + 2f, 0f);
            handleAreaRect.offsetMax = new Vector2(-(SliderGrooveRightInset + knobHalf + 2f), 0f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            Image handleImage = handle.GetComponent<Image>();
            if (knobSprite != null)
            {
                // Kept below SliderBarHeight so the knob never pokes out above or
                // below the wood, as it did when 44 sat in a 40-tall bar.
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
                // The word is painted into the art, so no Text child is added.
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
                // Sliced so the rolled log ends keep their true size.
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

        /// <summary>
        /// Lets a row's label or value shrink to fit its column instead of losing
        /// its second line, the same fix as the shop's status row and the tutorial's
        /// objective plank. The columns are sized so every current label fits at the
        /// largest text size, so this is only a safety net - best fit never draws
        /// above the font size, and the text size setting scales both limits.
        /// </summary>
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
            scaler.matchWidthOrHeight = 1f; // landscape: scale by height
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
