using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AgriDabao3D
{
    public class DevToolsUIBuilder : MonoBehaviour
    {
        [Header("Toggle")]
        public KeyCode toggleKey = KeyCode.F1;
        public bool startHidden = true;

        /// <summary>
        /// Above every panel the game builds, below the on-screen typing bar,
        /// which sits at 32000 and has to stay above everything.
        /// </summary>
        private const int DevPanelSortingOrder = 30000;

        /// <summary>
        /// On a desktop or in the editor the panel simply exists, toggled with
        /// F1. There is no account check because there is no account worth
        /// checking: anyone running the editor already has the project.
        /// </summary>
        private static bool DesktopToolsAllowed => !Application.isMobilePlatform;

        /// <summary>
        /// Set only once the server has confirmed both that this account is on
        /// the admin list and that the deployment currently allows the tools on
        /// a phone. Never decided here: the list and the switch both live on the
        /// server, so a phone has to ask, and an ordinary player is told no.
        ///
        /// Worth being honest about the limit of this. The panel's local actions
        /// are simulated on the device, so a rebuilt APK could perform them
        /// whatever the server says. What the gate buys is that the ordinary
        /// build has no button at all. The actions that reach other people -
        /// deleting an account, sending an event to another player - are checked
        /// against the admin list on the server every time and do not depend on
        /// this at all.
        /// </summary>
        private bool mobileToolsGranted;

        /// <summary>True when the panel is being drawn for a touch screen.</summary>
        private bool MobileLayout => !DesktopToolsAllowed;

        private Canvas canvas;
        private GameObject panelRoot;
        private RectTransform contentRect;
        private Text dayCounterText;
        private Text weatherText;

        private UIThemeSprites theme;
        private Image weatherPanelImage;
        private Text clockText;

        [Header("Day / Night Boundary")]
        [Tooltip("Hour the sunny panel takes over from the night panel.")]
        [Range(0f, 24f)] public float dayStartHour = 6f;
        [Tooltip("Hour the night panel takes over from the sunny panel.")]
        [Range(0f, 24f)] public float nightStartHour = 18f;

        private InputField seedInput;
        private InputField moneyInput;
        private InputField passDaysInput;
        private InputField passHoursInput;
        private InputField weatherDurationInput;

        private InputField sendTargetEmailInput;
        private InputField sendDurationInput;
        private InputField sendMoneyInput;
        private InputField sendTimeInput;
        private Text sendCommandStatus;

        // ---- crop info edit ----
        private Text cropEditTitle;
        private InputField cropEditStressInput;
        private InputField cropEditWaterInput;
        private InputField cropEditDrainageInput;
        private InputField cropEditFertilityInput;
        private InputField cropEditSuitabilityInput;
        private Text cropEditStatus;

        /// <summary>
        /// Every object the crop-editor section owns, so the whole block can be
        /// hidden when no crop is selected. Collected as a range of the content's
        /// children rather than tracked one by one, which keeps the flow-layout
        /// helpers unchanged.
        /// </summary>
        private readonly List<GameObject> cropEditBlock = new List<GameObject>();
        private float cropEditBlockHeight;
        private float contentFullHeight;
        private GameObject cropEditBoundCrop;
        private Text tankStatus;
        private bool sendCommandBusy;

        private Text sendEventLabel;
        private Text sendDurationLabel;
        private GameObject eventPicker;
        private int selectedEventIndex;

        /// <summary>
        /// How much vertical room the duration label and field occupy, so hiding
        /// them can pull everything below back up by exactly that much.
        /// </summary>
        private float durationBlockHeight;
        private bool durationVisible = true;

        /// <summary>
        /// Every option the picker offers: what to send, and how to name it on
        /// screen. The wording is carried alongside rather than parsed back out
        /// of the label, so a row can be reworded for readability without
        /// changing what pressing it triggers.
        /// </summary>
        private readonly System.Collections.Generic.List<(string type, string payload, string label)>
            sendEventOptions = new System.Collections.Generic.List<(string, string, string)>();

        private InputField deleteAccountEmailInput;
        private Text deleteAccountStatus;
        private GameObject deleteAccountPrompt;
        private Text deleteAccountPromptText;

        /// <summary>The address the open confirmation is about.</summary>
        private string pendingDeleteEmail;

        /// <summary>One admin request at a time; deleting is not something to fire twice.</summary>
        private bool deleteAccountBusy;

        private float nextY;

        private void Start()
        {
            EnsureEventSystem();
            EnsureCanvas();
            theme = UIThemeSprites.Instance;

            // These must also appear on Android.
            if (HasWeatherPanelArt())
            {
                BuildWeatherPanel();
            }
            else
            {
                BuildDayCounter();
                BuildWeatherText();
            }

            if (DesktopToolsAllowed)
            {
                BuildDevPanel();

                if (panelRoot != null)
                    panelRoot.SetActive(!startHidden);
            }
            else
            {
                // A phone builds nothing yet. The panel appears only if the
                // server says this account may have it, which takes a round trip
                // and cannot be answered here.
                StartCoroutine(RequestMobileTools());
            }

            if (GameTimeSystem.Instance != null)
            {
                GameTimeSystem.Instance.OnTimeChanged +=
                    RefreshDayCounter;
            }

            // Weather changes do not tick the clock, so the panel needs its own
            // signal to swap plates the moment a storm starts or clears.
            if (WeatherSystem.Instance != null)
                WeatherSystem.Instance.OnWeatherChanged += RefreshDayCounter;

            RefreshDayCounter();
        }

        private void OnDestroy()
        {
            if (GameTimeSystem.Instance != null)
                GameTimeSystem.Instance.OnTimeChanged -= RefreshDayCounter;

            if (WeatherSystem.Instance != null)
                WeatherSystem.Instance.OnWeatherChanged -= RefreshDayCounter;
        }

        private void Update()
        {
            // The crop editor follows whatever crop is on the info panel, and the
            // two can be opened in either order. Opening the dev panel refreshes
            // it, but tapping a crop while the panel is already open cannot - the
            // toggle never runs. Comparing the bound crop each frame covers both
            // orders, and costs one reference comparison.
            //
            // Deliberately above the desktop guard: on a phone Update returns
            // early, and the section has to work there too.
            if (panelRoot != null &&
                panelRoot.activeSelf &&
                SoilAwareTerrainGenerator.LastInspectedCrop != cropEditBoundCrop)
            {
                RefreshCropEditSection();
            }

            // F1 belongs to the desktop build. A phone toggles with the on-screen
            // button instead, so a keyboard plugged into a tablet still cannot
            // open anything the server did not grant.
            if (!DesktopToolsAllowed)
                return;

            if (Input.GetKeyDown(toggleKey) &&
                panelRoot != null)
            {
                // Through the shared toggle rather than SetActive, so opening with
                // the key does everything opening with the button does - it used
                // to skip the crop-editor refresh entirely.
                ToggleDevPanel();
            }
        }

        // ----------------------------------------------- tools on a phone

        /// <summary>
        /// Asks the server whether this account may open the tools here, and
        /// builds them if so.
        ///
        /// The wait exists because a saved login is restored asynchronously: on a
        /// cold launch this runs before AuthSession has a token, and asking then
        /// would get an unauthenticated refusal and never retry.
        /// </summary>
        private IEnumerator RequestMobileTools()
        {
            float waited = 0f;
            while (waited < 20f &&
                   (AuthSession.Instance == null ||
                    !AuthSession.Instance.IsAuthenticated ||
                    ApiClient.Instance == null))
            {
                waited += 0.5f;
                yield return new WaitForSecondsRealtime(0.5f);
            }

            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
                yield break;

            AdminCapabilityDto capability = null;
            yield return AuthSession.Instance.GetAdminCapability(
                value => capability = value,
                // Silent on purpose. Every ordinary player runs this, and a
                // failed check should leave the game exactly as it was.
                _ => { });

            if (capability == null || !capability.mobileToolsEnabled)
                yield break;

            mobileToolsGranted = true;

            BuildDevPanel();
            BuildMobileToolsButton();

            if (panelRoot != null)
                panelRoot.SetActive(false);

            Debug.Log("[DevTools] Mobile developer tools granted by the server.");
        }

        /// <summary>
        /// The plain button that opens the panel on a phone, tucked under the
        /// map on the right edge where the farm HUD leaves a gap.
        /// </summary>
        private void BuildMobileToolsButton()
        {
            GameObject go = new GameObject("MobileDevToolsButton",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(150f, 78f);

            // Below the small map, derived rather than eyeballed. The map hangs
            // from the same top-right corner at -(14 + moneyPlankSize.y + 12),
            // which is -176 with the stock 150-tall plank, and it is 300 tall -
            // so its bottom edge is at -476. A first attempt at -430 put this
            // button behind that last 46 units of it.
            const float moneyPlankTop = 14f;
            const float mapGap = 12f;
            float plankHeight = theme != null ? theme.moneyPlankSize.y : 150f;
            float mapHeight = theme != null ? theme.mapSmallSize.y : 300f;
            float mapBottom = moneyPlankTop + plankHeight + mapGap + mapHeight;

            rect.anchoredPosition = new Vector2(-24f, -(mapBottom + 24f));

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.09f, 0.11f, 0.13f, 0.94f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(ToggleDevPanel);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text label = textGo.GetComponent<Text>();
            label.font = GameFonts.Primary;
            label.fontSize = 24;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = "DEV";
        }

        /// <summary>
        /// The X in the panel's own corner. A phone has no F1, and although the
        /// DEV button toggles, a panel that covers a third of the screen wants a
        /// close where the eye already is.
        /// </summary>
        private void BuildDevPanelCloseButton(RectTransform parent)
        {
            GameObject go = new GameObject("DevPanelClose",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(56f, 44f);
            rect.anchoredPosition = new Vector2(-6f, -6f);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.48f, 0.16f, 0.14f, 0.96f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => panelRoot.SetActive(false));

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text label = textGo.GetComponent<Text>();
            label.font = GameFonts.Primary;
            label.fontSize = 22;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = "X";
        }

        public void ToggleDevPanel()
        {
            if (panelRoot == null)
                return;

            // Second guard, after the one that decided to build at all. Nothing
            // else calls this on a phone, but the panel is worth two checks.
            if (MobileLayout && !mobileToolsGranted)
                return;

            panelRoot.SetActive(!panelRoot.activeSelf);

            if (panelRoot.activeSelf)
            {
                panelRoot.transform.SetAsLastSibling();

                // Picks up whatever crop was tapped since the panel was last open,
                // and hides the block again if none was.
                RefreshCropEditSection();
            }
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

            if (canvas == null)
            {
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
                scaler.matchWidthOrHeight = 1f; // landscape: scale by height
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        private bool HasWeatherPanelArt()
        {
            return theme != null &&
                   (theme.weatherSunny != null || theme.weatherNight != null ||
                    theme.weatherRain != null || theme.weatherTyphoon != null ||
                    theme.weatherDrought != null);
        }

        /// <summary>
        /// The wooden weather panel: one Image whose sprite is swapped per weather
        /// state, with the clock, date and temperature drawn on the plank.
        ///
        /// All five sprites must share one canvas size with the plank in the same
        /// place - the text positions below are fixed, so a sprite whose plank sits
        /// elsewhere would make the text drift when the weather changes.
        /// </summary>
        private void BuildWeatherPanel()
        {
            Vector2 panelSize = theme.weatherPanelSize;

            GameObject go = new GameObject("WeatherTimePanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            HudRegistry.RegisterPiece(HudPiece.WeatherPanel, go);

            // Top-left, tucked under the pause and AI-Adviser buttons.
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = panelSize;
            rect.anchoredPosition = new Vector2(
                10f,
                -(20f + PauseMenuBuilder.CornerButtonSize + 14f));

            weatherPanelImage = go.GetComponent<Image>();
            weatherPanelImage.preserveAspect = true;
            weatherPanelImage.raycastTarget = false;

            // Offsets are expressed as fractions of the panel so tweaking
            // weatherPanelSize keeps the text on the planks.
            float w = panelSize.x;
            float h = panelSize.y;

            clockText = CreatePanelText(go.transform, "ClockText", 30, TextAnchor.MiddleCenter);
            RectTransform clockRect = clockText.rectTransform;
            clockRect.anchorMin = clockRect.anchorMax = new Vector2(0.5f, 1f);
            clockRect.pivot = new Vector2(0.5f, 0.5f);
            clockRect.sizeDelta = new Vector2(w * 0.54f, h * 0.20f);
            // Shifted left from 0.16: the clock sat right of its plank's centre
            // because the sun disc on the left was not accounted for.
            clockRect.anchoredPosition = new Vector2(w * 0.11f, -h * 0.275f);

            weatherText = CreatePanelText(go.transform, "DateTempText", 20, TextAnchor.UpperCenter);
            // White, unlike the clock above it. The clock sits on the pale top
            // plank where dark brown reads well, but the date and temperature sit
            // on the shaded lower plank, where the same brown all but disappeared.
            weatherText.color = Color.white;
            RectTransform infoRect = weatherText.rectTransform;
            infoRect.anchorMin = infoRect.anchorMax = new Vector2(0.5f, 1f);
            infoRect.pivot = new Vector2(0.5f, 0.5f);
            infoRect.sizeDelta = new Vector2(w * 0.60f, h * 0.30f);
            // Dropped from 0.66 so the date and temperature sit low on their plank
            // rather than riding its upper edge.
            infoRect.anchoredPosition = new Vector2(w * 0.16f, -h * 0.70f);

            RefreshWeatherPanelSprite();
        }

        private Text CreatePanelText(Transform parent, string name, int size, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.16f, 0.10f, 0.04f, 1f);
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        /// <summary>
        /// Clear weather follows the clock (sunny by day, night after dark). Rain,
        /// typhoon and drought hold their own panel regardless of the hour, and only
        /// hand back to sunny/night once the weather returns to clear.
        /// </summary>
        private void RefreshWeatherPanelSprite()
        {
            if (weatherPanelImage == null)
                return;

            WeatherSystem weather = WeatherSystem.Instance;
            WeatherEventType current = weather != null ? weather.currentEvent : WeatherEventType.Clear;

            Sprite chosen;
            switch (current)
            {
                case WeatherEventType.Rain:
                    chosen = theme.weatherRain;
                    break;
                case WeatherEventType.Typhoon:
                    chosen = theme.weatherTyphoon;
                    break;
                case WeatherEventType.ExtremeDrought:
                    chosen = theme.weatherDrought;
                    break;
                default:
                    chosen = IsDaytime() ? theme.weatherSunny : theme.weatherNight;
                    break;
            }

            // Fall back to the sunny plate rather than blanking the HUD if a slot
            // has not been filled in yet.
            if (chosen == null)
                chosen = theme.weatherSunny != null ? theme.weatherSunny : theme.weatherNight;

            if (chosen != null && weatherPanelImage.sprite != chosen)
                weatherPanelImage.sprite = chosen;

            weatherPanelImage.enabled = weatherPanelImage.sprite != null;
        }

        private bool IsDaytime()
        {
            if (GameTimeSystem.Instance == null)
                return true;

            float hour = GameTimeSystem.Instance.TimeOfDay01 * 24f;
            return hour >= dayStartHour && hour < nightStartHour;
        }

        private void BuildDayCounter()
        {
            GameObject go = new GameObject("DayCounterText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(canvas.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(800f, 60f);
            rect.anchoredPosition = new Vector2(0f, -18f);

            dayCounterText = go.GetComponent<Text>();
            dayCounterText.font = GameFonts.Primary;
            dayCounterText.fontSize = 28;
            dayCounterText.alignment = TextAnchor.MiddleCenter;
            dayCounterText.color = Color.white;
            dayCounterText.text = "Year 1 Month 1 Day 1";
        }

        private void BuildWeatherText()
        {
            GameObject go = new GameObject("WeatherText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(canvas.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(800f, 50f);
            rect.anchoredPosition = new Vector2(0f, -62f);

            weatherText = go.GetComponent<Text>();
            weatherText.font = GameFonts.Primary;
            weatherText.fontSize = 24;
            weatherText.alignment = TextAnchor.MiddleCenter;
            weatherText.color = new Color(0.92f, 0.92f, 0.92f);
            weatherText.text = "Weather: --";
        }

        private void BuildDevPanel()
        {
            panelRoot = new GameObject("DevToolsPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvas.transform, false);

            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(460f, 760f);
            rect.anchoredPosition = new Vector2(-20f, 0f);

            Image bg = panelRoot.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.82f);

            // Draw above every other panel on the shared canvas.
            //
            // Without this the panel is ordered by when it was created, and the
            // farm HUD's map is built afterwards and lands on top of it - which
            // is exactly what it did, hiding half the seed buttons behind the
            // map. Its own sorting layer settles that no matter what is built
            // later, so a new panel added next month cannot bury it again.
            //
            // Below MobileTextEntryOverlay's 32000: the on-screen typing bar has
            // to stay above everything, this one included.
            Canvas overlay = panelRoot.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = DevPanelSortingOrder;

            // A nested canvas needs its own raycaster or nothing inside it can be
            // clicked; the parent's does not reach through.
            panelRoot.AddComponent<GraphicRaycaster>();

            if (MobileLayout)
            {
                // Scaled as a whole rather than re-laid-out. Every row, field and
                // button grows together, so a panel designed for a mouse becomes
                // comfortable under a thumb without a second set of sizes to keep
                // in step. 1.25 is the most that keeps a 760-tall board inside a
                // 1080 canvas.
                panelRoot.transform.localScale = Vector3.one * 1.25f;
                BuildDevPanelCloseButton(rect);
            }

            GameObject scrollGo = new GameObject(
                "DevToolsScrollView",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect)
            );

            scrollGo.transform.SetParent(panelRoot.transform, false);

            RectTransform scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = Vector2.zero;
            scrollRectTransform.anchorMax = Vector2.one;
            scrollRectTransform.offsetMin = new Vector2(18f, 18f);
            scrollRectTransform.offsetMax = new Vector2(-18f, -18f);

            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(1f, 1f, 1f, 0.02f);

            ScrollRect scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 45f;

            GameObject viewportGo = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask)
            );

            viewportGo.transform.SetParent(scrollGo.transform, false);

            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;

            Mask mask = viewportGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);

            contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(400f, 1280f);
            contentRect.anchoredPosition = Vector2.zero;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            nextY = -12f;

            CreateLabel(contentRect, "DEV TOOLS (F1)", 28, 44f);

            CreateLabel(contentRect, "Money Amount", 21, 34f);
            moneyInput = CreateInputField(contentRect, "1000", 150f, 50f);
            CreateButton(contentRect, "Add Money", OnAddMoneyPressed);

            CreateSpacer(14f);

            CreateLabel(contentRect, "Planting Material Amount", 21, 34f);
            seedInput = CreateInputField(contentRect, "10", 150f, 50f);

            // One button per planting material, from the catalog, so a material
            // added later gets its button without touching this list.
            foreach (InventoryItemType material in PlantingMaterialCatalog.AllMaterials)
            {
                InventoryItemType captured = material;
                CreateButton(contentRect, "Add " + PlantingMaterialCatalog.NameOf(material),
                    () => AddSeeds(captured));
            }

            CreateSpacer(14f);
            CreateButton(contentRect, "Next Growth Stage", OnForceNextStagePressed, 250f, 58f);
            CreateButton(contentRect, "Harvest", OnForceHarvestPressed, 250f, 58f);
            CreateButton(contentRect, "Seedlings Ready", OnSeedlingsReadyPressed, 250f, 58f);

            CreateSpacer(14f);
            CreateLabel(contentRect, "Tutorial / HUD", 21, 34f);
            CreateButton(contentRect, "Replay Tutorial", OnReplayTutorialPressed, 250f, 58f);
            CreateButton(contentRect, "Skip Tutorial", OnSkipTutorialPressed, 250f, 58f);
            CreateButton(contentRect, "Toggle Joystick", OnToggleJoystickPressed, 250f, 58f);
            CreateButton(contentRect, "Show Whole HUD", OnShowWholeHudPressed, 250f, 58f);

            CreateSpacer(14f);
            CreateLabel(contentRect, "Pass Days", 21, 34f);
            passDaysInput = CreateInputField(contentRect, "20", 150f, 50f);
            CreateButton(contentRect, "Pass Days", OnPassDaysPressed);

            CreateLabel(contentRect, "Pass Hours", 21, 34f);
            passHoursInput = CreateInputField(contentRect, "2", 150f, 50f);
            CreateButton(contentRect, "Pass Hours", OnPassHoursPressed);

            CreateSpacer(14f);
            CreateLabel(contentRect, "Weather Duration Days", 21, 34f);
            weatherDurationInput = CreateInputField(contentRect, "3", 150f, 50f);

            CreateButton(contentRect, "Force Rain", OnForceRainPressed);
            CreateButton(contentRect, "Force Typhoon", OnForceTyphoonPressed);
            CreateButton(contentRect, "Force Drought", OnForceDroughtPressed);

            CreateSpacer(14f);
            CreateLabel(contentRect, "Pest & Disease Events", 21, 34f);

            CreateButton(contentRect, "Force Bacterial Wilt", () =>
            {
                PestDiseaseSystem.Instance?.ForceBacterialWilt();
            });

            CreateButton(contentRect, "Force Aphids", () =>
            {
                PestDiseaseSystem.Instance?.ForceAphids();
            });

            CreateButton(contentRect, "Force Armyworms", () =>
            {
                PestDiseaseSystem.Instance?.ForceArmyworms();
            });

            CreateButton(contentRect, "Force Mites", () =>
            {
                PestDiseaseSystem.Instance?.ForceMites();
            });

            CreateSpacer(14f);
            CreateLabel(contentRect, "Pest Tools", 21, 34f);

            CreateButton(contentRect, "Add Aphid Traps", () =>
            {
                AddSeeds(InventoryItemType.AphidTrap);
            });

            CreateButton(contentRect, "Add Sprayer Pump", () =>
            {
                PlayerInventory.Instance?.AddItem(InventoryItemType.SprayerPump, 1);
            });

            CreateButton(contentRect, "Add Insecticide", () =>
            {
                AddSeeds(InventoryItemType.InsecticideLiter);
            });

            CreateButton(contentRect, "Add Disinfectant", () =>
            {
                AddSeeds(InventoryItemType.DisinfectantLiter);
            });

            CreateLabel(contentRect, "SEND TO A PLAYER", 22, 40f);
            CreateLabel(contentRect,
                "The player must be online and in their farm. Arrives within ~15s.", 14, 40f);
            sendTargetEmailInput = CreateInputField(contentRect, "email@example.com", 340f, 50f);

            CreateLabel(contentRect, "Event", 16, 26f);
            CreateEventSelector(contentRect, 340f, 46f);
            // Height of the label plus the field plus their spacing, so hiding
            // the pair can close the gap they leave behind rather than leaving a
            // hole in the middle of the panel.
            float beforeDuration = nextY;
            sendDurationLabel = CreateLabel(contentRect, "Weather duration (days)", 16, 26f);
            sendDurationInput = CreateInputField(contentRect, "3", 340f, 46f);
            durationBlockHeight = beforeDuration - nextY;

            CreateButton(contentRect, "Trigger Event", OnSendEventPressed, 340f);

            // Same picker, applied to this farm instead of someone else's. The
            // four hard-coded buttons above only reach four of the forty-odd
            // conditions in the database; this reaches all of them without a
            // second list to keep in step.
            CreateButton(contentRect, "Force Here (my farm)", OnForceEventHerePressed, 340f);

            CreateLabel(contentRect, "Money to send", 16, 26f);
            sendMoneyInput = CreateInputField(contentRect, "1000", 340f, 46f);
            CreateButton(contentRect, "Send Money", OnSendMoneyPressed, 340f);

            // One field feeds both buttons: the number means days or hours
            // depending on which is pressed, so there is no second box to keep
            // in step and no way to send a value typed into the wrong one.
            CreateLabel(contentRect, "Time to pass", 16, 26f);
            sendTimeInput = CreateInputField(contentRect, "1", 340f, 46f);
            CreateButton(contentRect, "Pass Days on Player", OnSendPassDaysPressed, 340f);
            CreateButton(contentRect, "Pass Hours on Player", OnSendPassHoursPressed, 340f);

            sendCommandStatus = CreateStatusLabel(contentRect, 90f);

            CreateLabel(contentRect, "DELETE ACCOUNT", 22, 40f);
            CreateLabel(contentRect,
                "Wipes the account and its farm. The email becomes free again.", 14, 40f);
            deleteAccountEmailInput = CreateInputField(contentRect, "email@example.com", 340f, 50f);
            CreateButton(contentRect, "Delete Account", OnDeleteAccountPressed, 340f);
            deleteAccountStatus = CreateStatusLabel(contentRect, 100f);

            BuildTankSection();
            BuildCropEditSection();

            contentFullHeight = Mathf.Abs(nextY) + 40f;
            contentRect.sizeDelta = new Vector2(400f, contentFullHeight);
            scrollRect.verticalNormalizedPosition = 1f;

            // Nothing is selected when the panel is first built.
            SetCropEditVisible(false);
        }

        // ----------------------------------------------------------- water tank

        /// <summary>
        /// Fills the water tank the player last clicked.
        ///
        /// A tank gathers 45 a day in rain and 85 in a typhoon against a capacity
        /// of 500, so filling one honestly costs six game days of storm. That is
        /// fine to watch once, when the collection itself is what is being tested,
        /// and pure waiting every other time - the irrigation tests only need a
        /// tank that already has water in it.
        /// </summary>
        private void BuildTankSection()
        {
            CreateLabel(contentRect, "WATER TANK", 22, 40f);
            CreateLabel(contentRect,
                "Click a tank in the world first, then fill it here.", 14, 30f);
            CreateButton(contentRect, "Fill Water", OnFillTankPressed, 340f);
            tankStatus = CreateStatusLabel(contentRect, 60f);
        }

        private void OnFillTankPressed()
        {
            ClimateMitigationWorldObject structure =
                ClimateMitigationWorldObject.LastInspected;

            if (structure == null)
            {
                if (tankStatus != null)
                    tankStatus.text = "Click a structure in the world first.";
                return;
            }

            if (structure.mitigationType != ClimateWorldMitigationType.WaterStorageTank)
            {
                if (tankStatus != null)
                    tankStatus.text = structure.displayName + " is not a water tank.";
                return;
            }

            structure.DevFillResource();

            if (tankStatus != null)
            {
                tankStatus.text = string.Format(
                    "{0} filled to {1:F0}.",
                    structure.displayName, structure.storedResource);
            }

            Debug.Log("[DevTools] Water tank filled to " + structure.storedResource);
        }

        // ------------------------------------------------------- crop info edit

        /// <summary>
        /// Edits the readings of the crop currently shown on the info panel.
        ///
        /// Built last on purpose. The panel lays its contents out in a single
        /// downward flow, so a block anywhere else could only be hidden by
        /// shifting everything below it; at the end, hiding it is just a shorter
        /// content rect.
        /// </summary>
        private void BuildCropEditSection()
        {
            int firstChild = contentRect.childCount;
            float startY = nextY;

            CreateLabel(contentRect, "CROP INFO EDIT", 22, 40f);
            cropEditTitle = CreateLabel(contentRect, "No crop selected", 16, 30f);
            CreateLabel(contentRect,
                "Drainage, fertility and suitability hold. Water and stress are "
                + "simulated, so they set a starting point and then drift.", 13, 54f);

            CreateLabel(contentRect, "Stress (0-100)", 16, 26f);
            cropEditStressInput = CreateInputField(contentRect, "0", 340f, 46f);

            CreateLabel(contentRect, "Water %", 16, 26f);
            cropEditWaterInput = CreateInputField(contentRect, "60", 340f, 46f);

            CreateLabel(contentRect, "Drainage %", 16, 26f);
            cropEditDrainageInput = CreateInputField(contentRect, "50", 340f, 46f);

            CreateLabel(contentRect, "Fertility %", 16, 26f);
            cropEditFertilityInput = CreateInputField(contentRect, "50", 340f, 46f);

            CreateLabel(contentRect, "Soil Suitability %", 16, 26f);
            cropEditSuitabilityInput = CreateInputField(contentRect, "50", 340f, 46f);

            CreateButton(contentRect, "Read From Crop", RefreshCropEditSection, 340f);
            CreateButton(contentRect, "Apply To Crop", OnApplyCropVitalsPressed, 340f);
            cropEditStatus = CreateStatusLabel(contentRect, 70f);

            cropEditBlockHeight = startY - nextY;

            for (int i = firstChild; i < contentRect.childCount; i++)
                cropEditBlock.Add(contentRect.GetChild(i).gameObject);
        }

        private void SetCropEditVisible(bool visible)
        {
            foreach (GameObject go in cropEditBlock)
            {
                if (go != null)
                    go.SetActive(visible);
            }

            if (contentRect != null)
            {
                contentRect.sizeDelta = new Vector2(
                    400f,
                    visible
                        ? contentFullHeight
                        : Mathf.Max(40f, contentFullHeight - cropEditBlockHeight));
            }
        }

        /// <summary>
        /// Names the selected crop and fills the boxes with its current numbers,
        /// so one value can be changed without retyping the rest - and so a pair
        /// of plants can be matched by reading one and typing those figures into
        /// the other.
        /// </summary>
        private void RefreshCropEditSection()
        {
            GameObject crop = SoilAwareTerrainGenerator.LastInspectedCrop;
            cropEditBoundCrop = crop;

            if (crop == null || !DevCropVitals.TryRead(crop, out DevCropVitals.Vitals v))
            {
                SetCropEditVisible(false);
                return;
            }

            SetCropEditVisible(true);

            if (cropEditTitle != null)
                cropEditTitle.text = "Editing: " + v.cropName;

            if (cropEditStressInput != null)
                cropEditStressInput.text = v.stress.ToString("F1");
            if (cropEditWaterInput != null)
                cropEditWaterInput.text = (v.water * 100f).ToString("F0");
            if (cropEditDrainageInput != null)
                cropEditDrainageInput.text = (v.drainage * 100f).ToString("F0");
            if (cropEditFertilityInput != null)
                cropEditFertilityInput.text = (v.fertility * 100f).ToString("F0");
            if (cropEditSuitabilityInput != null)
                cropEditSuitabilityInput.text = (v.suitability * 100f).ToString("F0");

            if (cropEditStatus != null)
                cropEditStatus.text = "";
        }

        private void OnApplyCropVitalsPressed()
        {
            GameObject crop = SoilAwareTerrainGenerator.LastInspectedCrop;

            if (crop == null || !DevCropVitals.TryRead(crop, out DevCropVitals.Vitals current))
            {
                SetCropEditVisible(false);
                return;
            }

            // A box left empty or holding something unparseable keeps the crop's
            // present value, so a slip of the keyboard cannot silently zero a
            // reading the developer never meant to touch.
            DevCropVitals.Vitals next = new DevCropVitals.Vitals
            {
                cropName = current.cropName,
                stress = ParseOr(cropEditStressInput, current.stress),
                water = ParseOr(cropEditWaterInput, current.water * 100f) / 100f,
                drainage = ParseOr(cropEditDrainageInput, current.drainage * 100f) / 100f,
                fertility = ParseOr(cropEditFertilityInput, current.fertility * 100f) / 100f,
                suitability = ParseOr(cropEditSuitabilityInput, current.suitability * 100f) / 100f
            };

            if (!DevCropVitals.TryApply(crop, next))
            {
                if (cropEditStatus != null)
                    cropEditStatus.text = "That object is not a crop.";
                return;
            }

            if (cropEditStatus != null)
            {
                cropEditStatus.text = string.Format(
                    "{0}: stress {1:F1}, water {2:F0}%, drainage {3:F0}%, "
                    + "fertility {4:F0}%, suitability {5:F0}%",
                    next.cropName, next.stress, next.water * 100f,
                    next.drainage * 100f, next.fertility * 100f,
                    next.suitability * 100f);
            }

            Debug.Log("[DevTools] Crop vitals set on " + next.cropName);

            // Redraw the info panel so the board shows the new numbers rather than
            // the ones it was opened with.
            SoilAwareTerrainGenerator terrain =
                Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();
            if (terrain != null)
                terrain.ShowFullCropInfo(crop);
        }

        private static float ParseOr(InputField field, float fallback)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.text))
                return fallback;

            return float.TryParse(field.text, out float parsed) ? parsed : fallback;
        }

        // --------------------------------------------- send to another player

        /// <summary>
        /// Fills the option list and shows the current pick on a button.
        ///
        /// Not a Unity Dropdown. That control opens its list as a child of
        /// itself, and this panel lives inside a masked ScrollRect, so the list
        /// was clipped away by the mask with half an entry escaping over the
        /// label above it. The picker below opens on the canvas root instead,
        /// clear of the scroll view entirely.
        /// </summary>
        private void CreateEventSelector(Transform parent, float width, float height)
        {
            sendEventOptions.Clear();

            foreach (WeatherEventType weather in new[]
                     {
                         WeatherEventType.Rain,
                         WeatherEventType.Typhoon,
                         WeatherEventType.ExtremeDrought,
                         WeatherEventType.Clear
                     })
            {
                sendEventOptions.Add(("FORCE_WEATHER", weather.ToString(), "Weather - " + weather));
            }

            // Read from the enum, so a condition added later shows up here
            // without this list being touched.
            foreach (PestDiseaseType pest in
                     (PestDiseaseType[])System.Enum.GetValues(typeof(PestDiseaseType)))
            {
                if (pest == PestDiseaseType.None)
                    continue;

                sendEventOptions.Add(("FORCE_PEST_DISEASE", pest.ToString(), "Pest - " + pest));
            }

            GameObject go = new GameObject("EventSelector",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, nextY);
            nextY -= height + 8f;

            Image background = go.GetComponent<Image>();
            background.color = new Color(0.16f, 0.18f, 0.22f, 1f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(ShowEventPicker);

            sendEventLabel = CreateSelectorText(go.transform, 16, TextAnchor.MiddleLeft, 12f);
            selectedEventIndex = 0;
            RefreshEventLabel();
        }

        private void RefreshEventLabel()
        {
            if (sendEventLabel == null)
                return;

            sendEventLabel.text =
                selectedEventIndex >= 0 && selectedEventIndex < sendEventOptions.Count
                    ? sendEventOptions[selectedEventIndex].label + "   (tap to change)"
                    : "(choose an event)";

            // Duration only means anything to a weather event. The server drops
            // it for a pest command, so leaving the box on screen would invite a
            // developer to set a number that quietly does nothing.
            SetDurationVisible(
                selectedEventIndex >= 0 &&
                selectedEventIndex < sendEventOptions.Count &&
                sendEventOptions[selectedEventIndex].type == "FORCE_WEATHER");
        }

        /// <summary>
        /// Shows or hides the duration pair, closing the gap behind it.
        ///
        /// This panel is laid out once with fixed positions rather than by a
        /// layout group, so hiding a row does not reflow anything on its own -
        /// everything below has to be moved by hand, and the scroll content
        /// resized to match, or the panel is left with a hole and a stretch of
        /// dead scroll at the bottom.
        /// </summary>
        private void SetDurationVisible(bool visible)
        {
            if (sendDurationLabel == null || sendDurationInput == null)
                return;

            if (durationVisible == visible)
                return;

            durationVisible = visible;
            sendDurationLabel.gameObject.SetActive(visible);
            sendDurationInput.gameObject.SetActive(visible);

            // Rows sit at negative y, so moving up means adding.
            float shift = visible ? -durationBlockHeight : durationBlockHeight;

            int firstBelow = sendDurationInput.transform.GetSiblingIndex() + 1;
            for (int i = firstBelow; i < contentRect.childCount; i++)
            {
                if (contentRect.GetChild(i) is RectTransform child)
                    child.anchoredPosition += new Vector2(0f, shift);
            }

            contentRect.sizeDelta = new Vector2(
                contentRect.sizeDelta.x,
                Mathf.Max(0f, contentRect.sizeDelta.y - shift));
        }

        /// <summary>
        /// The option list, on the canvas root above the dev panel so no mask or
        /// sibling order can hide it. Built once and reused.
        /// </summary>
        private void ShowEventPicker()
        {
            if (eventPicker == null)
                BuildEventPicker();

            eventPicker.SetActive(true);
            eventPicker.transform.SetAsLastSibling();
        }

        private void BuildEventPicker()
        {
            eventPicker = new GameObject("DevEventPicker",
                typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
            eventPicker.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = eventPicker.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image blocker = eventPicker.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.80f);
            blocker.raycastTarget = true;

            // Above the dev panel own 30000, below the typing bar 32000.
            Canvas overlay = eventPicker.GetComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = DevPanelSortingOrder + 500;

            GameObject scrollGo = new GameObject("Scroll",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
            scrollGo.transform.SetParent(eventPicker.transform, false);

            RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = scrollRect.anchorMax = scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = new Vector2(560f, 620f);
            scrollRect.anchoredPosition = new Vector2(0f, 40f);
            scrollGo.GetComponent<Image>().color = new Color(0.10f, 0.11f, 0.14f, 0.99f);
            scrollGo.GetComponent<Mask>().showMaskGraphic = true;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(scrollGo.transform, false);
            RectTransform contentArea = content.GetComponent<RectTransform>();
            contentArea.anchorMin = new Vector2(0f, 1f);
            contentArea.anchorMax = new Vector2(1f, 1f);
            contentArea.pivot = new Vector2(0.5f, 1f);

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = contentArea;
            scroll.viewport = scrollRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            const float rowHeight = 40f;
            float y = -6f;

            for (int i = 0; i < sendEventOptions.Count; i++)
            {
                // Captured per row. One shared variable would leave every button
                // selecting whatever the loop finished on.
                int index = i;

                GameObject row = new GameObject(sendEventOptions[i].label,
                    typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(content.transform, false);

                RectTransform rowRect = row.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.offsetMin = new Vector2(8f, 0f);
                rowRect.offsetMax = new Vector2(-8f, 0f);
                rowRect.sizeDelta = new Vector2(-16f, rowHeight - 4f);
                rowRect.anchoredPosition = new Vector2(0f, y);
                y -= rowHeight;

                Image rowImage = row.GetComponent<Image>();
                rowImage.color = sendEventOptions[index].type == "FORCE_WEATHER"
                    ? new Color(0.18f, 0.32f, 0.45f, 1f)
                    : new Color(0.22f, 0.40f, 0.24f, 1f);

                Button rowButton = row.GetComponent<Button>();
                rowButton.targetGraphic = rowImage;
                rowButton.onClick.AddListener(() =>
                {
                    selectedEventIndex = index;
                    RefreshEventLabel();
                    eventPicker.SetActive(false);
                });

                Text rowLabel = CreateSelectorText(row.transform, 15, TextAnchor.MiddleLeft, 12f);
                rowLabel.text = sendEventOptions[index].label;
            }

            contentArea.sizeDelta = new Vector2(0f, Mathf.Abs(y) + 8f);

            GameObject closeGo = new GameObject("Close",
                typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(eventPicker.transform, false);

            RectTransform closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.sizeDelta = new Vector2(200f, 50f);
            closeRect.anchoredPosition = new Vector2(0f, -320f);
            closeGo.GetComponent<Image>().color = new Color(0.30f, 0.32f, 0.36f, 1f);

            Button closeButton = closeGo.GetComponent<Button>();
            closeButton.targetGraphic = closeGo.GetComponent<Image>();
            closeButton.onClick.AddListener(() => eventPicker.SetActive(false));

            Text closeLabel = CreateSelectorText(closeGo.transform, 16, TextAnchor.MiddleCenter, 0f);
            closeLabel.text = "Cancel";

            eventPicker.SetActive(false);
        }

        private static Text CreateSelectorText(Transform parent, int size,
            TextAnchor alignment, float inset)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, 2f);
            rect.offsetMax = new Vector2(-inset, -2f);

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>
        /// Forces the picked pest on this farm.
        ///
        /// Weather is deliberately refused: the farm already has Force Rain,
        /// Typhoon and Drought buttons of its own, and those take the duration
        /// field, which this path has no way to pass.
        /// </summary>
        private void OnForceEventHerePressed()
        {
            if (selectedEventIndex < 0 || selectedEventIndex >= sendEventOptions.Count)
                return;

            (string type, string payload, string label) chosen =
                sendEventOptions[selectedEventIndex];

            if (chosen.type != "FORCE_PEST_DISEASE")
            {
                if (sendCommandStatus != null)
                {
                    sendCommandStatus.text =
                        "Pick a pest. Weather on your own farm has its own "
                        + "buttons further up, which also take a duration.";
                }
                return;
            }

            if (!System.Enum.TryParse(chosen.payload, out PestDiseaseType pest) ||
                pest == PestDiseaseType.None)
            {
                if (sendCommandStatus != null)
                    sendCommandStatus.text = "Unknown pest: " + chosen.payload;
                return;
            }

            if (PestDiseaseSystem.Instance == null)
            {
                if (sendCommandStatus != null)
                    sendCommandStatus.text = "The pest system is not running.";
                return;
            }

            PestDiseaseSystem.Instance.ForceEvent(pest);

            if (sendCommandStatus != null)
            {
                sendCommandStatus.text =
                    "Forced " + pest + " here. If nothing appears, no planted crop "
                    + "hosts it - check the console for Valid crops=0.";
            }

            Debug.Log("[DevTools] Forced " + pest + " on the local farm.");
        }

        private void OnSendEventPressed()
        {
            if (selectedEventIndex < 0 || selectedEventIndex >= sendEventOptions.Count)
            {
                SetSendStatus("Choose an event first.");
                return;
            }

            (string type, string payload, string label) chosen = sendEventOptions[selectedEventIndex];

            SendCommand(new AdminCommandRequestDto
            {
                commandType = chosen.type,
                payload = chosen.payload,
                durationDays = ParsePositiveInt(sendDurationInput, 3)
            });
        }

        private void OnSendMoneyPressed()
        {
            int amount = ParsePositiveInt(sendMoneyInput, 0);
            if (amount <= 0)
            {
                SetSendStatus("Enter an amount of money greater than zero.");
                return;
            }

            SendCommand(new AdminCommandRequestDto
            {
                commandType = "GRANT_MONEY",
                amount = amount
            });
        }

        private void OnSendPassDaysPressed()
        {
            SendTimeSkip("PASS_DAYS", "days");
        }

        private void OnSendPassHoursPressed()
        {
            SendTimeSkip("PASS_HOURS", "hours");
        }

        /// <summary>
        /// Sends a clock skip to the target player.
        ///
        /// The server holds the real ceilings and refuses anything past them;
        /// this only catches an empty or zero box so the obvious mistake does
        /// not cost a round trip. Skipping time is not undoable on the receiving
        /// farm - crops age, weather rerolls, objectives regenerate - so neither
        /// side quietly rounds a number down.
        /// </summary>
        private void SendTimeSkip(string commandType, string unit)
        {
            int amount = ParsePositiveInt(sendTimeInput, 0);
            if (amount <= 0)
            {
                SetSendStatus("Enter how many " + unit + " to pass.");
                return;
            }

            SendCommand(new AdminCommandRequestDto
            {
                commandType = commandType,
                amount = amount
            });
        }

        /// <summary>Fills in the target and posts, with the shared guards.</summary>
        private void SendCommand(AdminCommandRequestDto request)
        {
            if (sendCommandBusy)
                return;

            string email = sendTargetEmailInput != null
                ? sendTargetEmailInput.text.Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                SetSendStatus("Enter the player's email address.");
                return;
            }

            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
            {
                SetSendStatus("Sign in first - this uses your own account's admin rights.");
                return;
            }

            request.email = email;
            sendCommandBusy = true;
            SetSendStatus("Sending to " + email + "...");

            StartCoroutine(AuthSession.Instance.SendAdminCommand(
                request,
                message =>
                {
                    sendCommandBusy = false;
                    SetSendStatus(message);
                },
                message =>
                {
                    sendCommandBusy = false;
                    SetSendStatus(message);
                }));
        }

        private void SetSendStatus(string message)
        {
            if (sendCommandStatus != null)
                sendCommandStatus.text = message;
        }

        // ------------------------------------------------- delete an account

        /// <summary>
        /// Looks the address up first, so the confirmation can name a real
        /// account rather than asking the developer to trust their own typing.
        /// </summary>
        private void OnDeleteAccountPressed()
        {
            if (deleteAccountBusy)
                return;

            string email = deleteAccountEmailInput != null
                ? deleteAccountEmailInput.text.Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                SetDeleteStatus("Enter an email address.");
                return;
            }

            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
            {
                SetDeleteStatus("Sign in first - this uses your own account's admin rights.");
                return;
            }

            deleteAccountBusy = true;
            SetDeleteStatus("Looking up " + email + "...");

            StartCoroutine(AuthSession.Instance.LookupAccount(
                email,
                result =>
                {
                    deleteAccountBusy = false;

                    if (result == null || !result.exists)
                    {
                        SetDeleteStatus("No account exists with " + email + ".");
                        return;
                    }

                    pendingDeleteEmail = result.email;
                    ShowDeletePrompt(result);
                },
                message =>
                {
                    deleteAccountBusy = false;
                    SetDeleteStatus(message);
                }));
        }

        private void OnDeleteAccountConfirmed()
        {
            if (deleteAccountBusy || string.IsNullOrWhiteSpace(pendingDeleteEmail))
                return;

            deleteAccountBusy = true;
            HideDeletePrompt();
            SetDeleteStatus("Deleting " + pendingDeleteEmail + "...");

            StartCoroutine(AuthSession.Instance.DeleteAccount(
                pendingDeleteEmail,
                message =>
                {
                    deleteAccountBusy = false;
                    pendingDeleteEmail = null;
                    if (deleteAccountEmailInput != null)
                        deleteAccountEmailInput.text = string.Empty;
                    SetDeleteStatus(message);
                },
                message =>
                {
                    deleteAccountBusy = false;
                    pendingDeleteEmail = null;
                    SetDeleteStatus(message);
                }));
        }

        private void SetDeleteStatus(string message)
        {
            if (deleteAccountStatus != null)
                deleteAccountStatus.text = message;
        }

        /// <summary>
        /// A plain dark box over the screen. Deliberately unstyled - no player
        /// ever sees this, and dressing it in wooden planks would only make it
        /// look like something they are meant to.
        /// </summary>
        private void ShowDeletePrompt(AdminAccountLookupDto account)
        {
            if (deleteAccountPrompt == null)
                BuildDeletePrompt();

            deleteAccountPromptText.text =
                "You want to delete this " + account.email + " account?\n\n" +
                "Display name: " +
                (string.IsNullOrWhiteSpace(account.displayName) ? "(none)" : account.displayName) +
                "\nHas a farm: " + (account.hasFarm ? "yes" : "no") +
                "\n\nThis erases the account, its farm, its settings, its friends, " +
                "its chats and its trades. It cannot be undone.";

            deleteAccountPrompt.SetActive(true);
            deleteAccountPrompt.transform.SetAsLastSibling();
        }

        private void HideDeletePrompt()
        {
            if (deleteAccountPrompt != null)
                deleteAccountPrompt.SetActive(false);
        }

        private void BuildDeletePrompt()
        {
            deleteAccountPrompt = new GameObject(
                "DevDeleteAccountPrompt", typeof(RectTransform), typeof(Image));
            deleteAccountPrompt.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = deleteAccountPrompt.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image blocker = deleteAccountPrompt.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.75f);
            blocker.raycastTarget = true;

            GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(deleteAccountPrompt.transform, false);

            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(720f, 380f);
            box.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.98f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(box.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.30f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(28f, 0f);
            textRect.offsetMax = new Vector2(-28f, -24f);

            deleteAccountPromptText = textGo.GetComponent<Text>();
            deleteAccountPromptText.font = GameFonts.Primary;
            deleteAccountPromptText.fontSize = 20;
            deleteAccountPromptText.alignment = TextAnchor.UpperLeft;
            deleteAccountPromptText.color = Color.white;
            deleteAccountPromptText.horizontalOverflow = HorizontalWrapMode.Wrap;
            deleteAccountPromptText.verticalOverflow = VerticalWrapMode.Overflow;

            CreatePromptButton(box.transform, "YES, DELETE", new Vector2(-150f, 40f),
                new Color(0.60f, 0.16f, 0.16f, 1f), OnDeleteAccountConfirmed);
            CreatePromptButton(box.transform, "NO", new Vector2(150f, 40f),
                new Color(0.25f, 0.28f, 0.32f, 1f), () =>
                {
                    pendingDeleteEmail = null;
                    HideDeletePrompt();
                    SetDeleteStatus("Cancelled. Nothing was deleted.");
                });

            deleteAccountPrompt.SetActive(false);
        }

        private void CreatePromptButton(Transform parent, string label, Vector2 position,
            Color colour, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(240f, 56f);
            rect.anchoredPosition = position;

            Image image = go.GetComponent<Image>();
            image.color = colour;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGo.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = label;
        }

        /// <summary>A wrapping line for results, unlike CreateLabel's fixed row.</summary>
        private Text CreateStatusLabel(Transform parent, float height)
        {
            GameObject go = new GameObject("Status", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(340f, height);
            rect.anchoredPosition = new Vector2(0f, nextY);
            nextY -= height + 8f;

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = 15;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(0.85f, 0.88f, 0.92f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = string.Empty;
            return text;
        }

        private void RefreshDayCounter()
        {
            GameTimeSystem time = GameTimeSystem.Instance;
            WeatherSystem weather = WeatherSystem.Instance;

            // Themed panel: clock on the top plank, date and temperature below,
            // and the plate itself swapped to match the weather.
            if (weatherPanelImage != null)
            {
                RefreshWeatherPanelSprite();

                if (clockText != null)
                    clockText.text = time != null ? time.GetClockText() : "--:--";

                if (weatherText != null)
                {
                    string date = time != null
                        ? $"Year {time.CurrentYearNumber} | Month {time.CurrentMonthNumber} | Day {time.CurrentDayInMonth}"
                        : "Year - | Month - | Day -";

                    string temp = weather != null
                        ? $"Temperature: {weather.currentTemperatureC:F1}C"
                        : "Temperature: --C";

                    weatherText.text = date + "\n" + temp;
                }

                return;
            }

            if (dayCounterText == null || time == null)
                return;

            dayCounterText.text =
                $"Year {time.CurrentYearNumber}  " +
                $"Month {time.CurrentMonthNumber}  " +
                $"Day {time.CurrentDayInMonth}  |  " +
                $"{time.GetClockText()}";

            if (weatherText == null)
                return;

            if (weather == null)
            {
                weatherText.text =
                    "Weather: --  |  Temperature: --C";

                return;
            }

            weatherText.text =
                $"Weather: {weather.currentEvent}  |  " +
                $"Temperature: {weather.currentTemperatureC:F1}C";
        }

        private void OnForceNextStagePressed()
        {
            FarmingInteractionSystem farming = Object.FindFirstObjectByType<FarmingInteractionSystem>();

            if (farming != null)
                farming.ForceAllPlantsNextStageForDev();
        }

        /// <summary>Every sown bag in the Seedling Tent skips to ready to transplant.</summary>
        private void OnSeedlingsReadyPressed()
        {
            int changed = NurserySystem.Instance != null ? NurserySystem.Instance.DevMakeAllReady() : 0;
            Debug.Log("[DevTools] Seedling Tent bags made ready: " + changed);
        }

        private void AddSeeds(InventoryItemType item)
        {
            if (PlayerInventory.Instance == null)
                return;

            int amount = ParsePositiveInt(seedInput, 0);

            if (amount <= 0)
                return;

            PlayerInventory.Instance.AddItem(item, amount);
        }

        private void OnAddMoneyPressed()
        {
            if (PlayerInventory.Instance == null)
                return;

            int amount = ParsePositiveInt(moneyInput, 0);

            if (amount <= 0)
                return;

            PlayerInventory.Instance.AddMoney(amount);

            Debug.Log($"[DevTools] Added money: P{amount}");
        }

        private void OnForceHarvestPressed()
        {
            FarmingInteractionSystem farming = Object.FindFirstObjectByType<FarmingInteractionSystem>();

            if (farming != null)
                farming.ForceAllPlantsToProduceForDev();
        }

        /// <summary>
        /// Runs the beginner guide again on this farm. Testing it otherwise means
        /// creating a whole new account every time, since it only ever runs once.
        /// </summary>
        private void OnReplayTutorialPressed()
        {
            TutorialState.ResetForNewFarm();
            TutorialState.EnsureStartingSeedsRolled(SelectedAreaState.SelectedDistrictName);

            TutorialDirector existing = Object.FindFirstObjectByType<TutorialDirector>();
            if (existing != null)
                Destroy(existing.gameObject);

            new GameObject("TutorialDirector").AddComponent<TutorialDirector>();
            Debug.Log("[DevTools] Beginner guide restarted.");
        }

        /// <summary>Ends the guide immediately and hands back the full HUD.</summary>
        private void OnSkipTutorialPressed()
        {
            TutorialDirector existing = Object.FindFirstObjectByType<TutorialDirector>();
            if (existing != null)
                Destroy(existing.gameObject);

            TutorialState.Completed = true;
            TutorialState.CurrentStep = -1;
            TutorialState.CropInspectionLocked = false;
            HudRegistry.EndTutorialControl();
            Debug.Log("[DevTools] Beginner guide skipped; HUD restored.");
        }

        private void OnToggleJoystickPressed()
        {
            if (!HudRegistry.TryGetPiece(HudPiece.Joystick, out GameObject go))
            {
                // Saying "shown" when there is nothing to show sends you hunting
                // for a rendering bug that does not exist.
                Debug.LogWarning(
                    "[DevTools] There is no joystick to toggle. MobileHudBuilder only " +
                    "builds the on-screen controls on a phone. To use them in the " +
                    "Editor, tick 'Also Build In Editor' on the MobileHudBuilder " +
                    "component and press Play again.");
                return;
            }

            bool visible = go.activeSelf;
            HudRegistry.SetPieceVisible(HudPiece.Joystick, !visible);
            HudRegistry.SetPieceVisible(HudPiece.JumpButton, !visible);
            Debug.Log("[DevTools] Joystick and jump button " + (visible ? "hidden." : "shown."));
        }

        private void OnShowWholeHudPressed()
        {
            HudRegistry.EndTutorialControl();
            Debug.Log("[DevTools] Whole HUD shown.");
        }

        private void OnPassDaysPressed()
        {
            if (GameTimeSystem.Instance == null)
                return;

            float days = ParsePositiveFloat(passDaysInput, 0f);

            if (days > 0f)
                GameTimeSystem.Instance.AdvanceDays(days);
        }

        private void OnPassHoursPressed()
        {
            if (GameTimeSystem.Instance == null)
                return;

            float hours = ParsePositiveFloat(passHoursInput, 0f);

            if (hours > 0f)
                GameTimeSystem.Instance.AdvanceHours(hours);
        }

        private void OnForceRainPressed()
        {
            if (WeatherSystem.Instance == null)
                return;

            int duration = ParsePositiveInt(weatherDurationInput, 1);
            WeatherSystem.Instance.ForceWeatherEvent(WeatherEventType.Rain, duration);
            RefreshDayCounter();
        }

        private void OnForceTyphoonPressed()
        {
            if (WeatherSystem.Instance == null)
                return;

            int duration = ParsePositiveInt(weatherDurationInput, 4);
            WeatherSystem.Instance.ForceWeatherEvent(WeatherEventType.Typhoon, duration);
            RefreshDayCounter();
        }

        private void OnForceDroughtPressed()
        {
            if (WeatherSystem.Instance == null)
                return;

            int duration = ParsePositiveInt(weatherDurationInput, 4);
            WeatherSystem.Instance.ForceWeatherEvent(WeatherEventType.ExtremeDrought, duration);
            RefreshDayCounter();
        }

        private int ParsePositiveInt(InputField field, int fallback)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.text))
                return fallback;

            return int.TryParse(field.text, out int value)
                ? Mathf.Max(0, value)
                : fallback;
        }

        private float ParsePositiveFloat(InputField field, float fallback)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.text))
                return fallback;

            return float.TryParse(field.text, out float value)
                ? Mathf.Max(0f, value)
                : fallback;
        }

        /// <summary>
        /// Returns the label so a caller can hide or reword it later. Most
        /// callers ignore it.
        /// </summary>
        private Text CreateLabel(Transform parent, string textValue, int fontSize, float height)
        {
            GameObject go = new GameObject(textValue, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(380f, height);
            rect.anchoredPosition = new Vector2(0f, nextY);
            nextY -= height;

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = textValue;
            return text;
        }

        private InputField CreateInputField(Transform parent, string defaultValue, float width, float height)
        {
            GameObject go = new GameObject(
                "InputField",
                typeof(RectTransform),
                typeof(Image),
                typeof(InputField)
            );

            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, nextY);
            nextY -= height + 10f;

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.92f);

            InputField input = go.GetComponent<InputField>();

            GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(go.transform, false);
            Stretch(placeholderGo.GetComponent<RectTransform>(), 10f, 6f);

            Text placeholder = placeholderGo.GetComponent<Text>();
            placeholder.font = GameFonts.Primary;
            placeholder.fontSize = 22;
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.color = new Color(0f, 0f, 0f, 0.35f);
            placeholder.text = defaultValue;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>(), 10f, 6f);

            Text text = textGo.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.black;
            text.text = defaultValue;

            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = defaultValue;

            return input;
        }

        private void CreateButton(
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction onClick,
            float width = 260f,
            float height = 52f)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, nextY);
            nextY -= height + 8f;

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.2f, 0.55f, 0.2f, 0.95f);

            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);

            RectTransform tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            Text text = textGo.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = 21;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
        }

        private void CreateSpacer(float height)
        {
            nextY -= height;
        }

        private void Stretch(RectTransform rect, float padX, float padY)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padX, padY);
            rect.offsetMax = new Vector2(-padX, -padY);
        }
    }
}