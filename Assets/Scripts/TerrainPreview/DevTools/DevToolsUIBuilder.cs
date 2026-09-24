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

        private const int DevPanelSortingOrder = 30000;

        private static bool DesktopToolsAllowed => !Application.isMobilePlatform;

        private bool mobileToolsGranted;

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

        private Text cropEditTitle;
        private InputField cropEditStressInput;
        private InputField cropEditWaterInput;
        private InputField cropEditDrainageInput;
        private InputField cropEditFertilityInput;
        private InputField cropEditSuitabilityInput;
        private Text cropEditStatus;

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

        private float durationBlockHeight;
        private bool durationVisible = true;

        private readonly System.Collections.Generic.List<(string type, string payload, string label)>
            sendEventOptions = new System.Collections.Generic.List<(string, string, string)>();

        private InputField deleteAccountEmailInput;
        private Text deleteAccountStatus;
        private GameObject deleteAccountPrompt;
        private Text deleteAccountPromptText;

        private string pendingDeleteEmail;

        private bool deleteAccountBusy;

        private float nextY;

        private void Start()
        {
            EnsureEventSystem();
            EnsureCanvas();
            theme = UIThemeSprites.Instance;

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
                StartCoroutine(RequestMobileTools());
            }

            if (GameTimeSystem.Instance != null)
            {
                GameTimeSystem.Instance.OnTimeChanged +=
                    RefreshDayCounter;
            }

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
            if (panelRoot != null &&
                panelRoot.activeSelf &&
                SoilAwareTerrainGenerator.LastInspectedCrop != cropEditBoundCrop)
            {
                RefreshCropEditSection();
            }

            if (!DesktopToolsAllowed)
                return;

            if (Input.GetKeyDown(toggleKey) &&
                panelRoot != null)
            {
                ToggleDevPanel();
            }
        }

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

        private void BuildMobileToolsButton()
        {
            GameObject go = new GameObject("MobileDevToolsButton",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(150f, 78f);

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

            if (MobileLayout && !mobileToolsGranted)
                return;

            panelRoot.SetActive(!panelRoot.activeSelf);

            if (panelRoot.activeSelf)
            {
                panelRoot.transform.SetAsLastSibling();

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
                scaler.matchWidthOrHeight = 1f;
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

        private void BuildWeatherPanel()
        {
            Vector2 panelSize = theme.weatherPanelSize;

            GameObject go = new GameObject("WeatherTimePanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            HudRegistry.RegisterPiece(HudPiece.WeatherPanel, go);

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

            float w = panelSize.x;
            float h = panelSize.y;

            clockText = CreatePanelText(go.transform, "ClockText", 30, TextAnchor.MiddleCenter);
            RectTransform clockRect = clockText.rectTransform;
            clockRect.anchorMin = clockRect.anchorMax = new Vector2(0.5f, 1f);
            clockRect.pivot = new Vector2(0.5f, 0.5f);
            clockRect.sizeDelta = new Vector2(w * 0.54f, h * 0.20f);
            clockRect.anchoredPosition = new Vector2(w * 0.11f, -h * 0.275f);

            weatherText = CreatePanelText(go.transform, "DateTempText", 20, TextAnchor.UpperCenter);
            weatherText.color = Color.white;
            RectTransform infoRect = weatherText.rectTransform;
            infoRect.anchorMin = infoRect.anchorMax = new Vector2(0.5f, 1f);
            infoRect.pivot = new Vector2(0.5f, 0.5f);
            infoRect.sizeDelta = new Vector2(w * 0.60f, h * 0.30f);
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

            Canvas overlay = panelRoot.AddComponent<Canvas>();
            overlay.overrideSorting = true;
            overlay.sortingOrder = DevPanelSortingOrder;

            panelRoot.AddComponent<GraphicRaycaster>();

            if (MobileLayout)
            {
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
            float beforeDuration = nextY;
            sendDurationLabel = CreateLabel(contentRect, "Weather duration (days)", 16, 26f);
            sendDurationInput = CreateInputField(contentRect, "3", 340f, 46f);
            durationBlockHeight = beforeDuration - nextY;

            CreateButton(contentRect, "Trigger Event", OnSendEventPressed, 340f);

            CreateButton(contentRect, "Force Here (my farm)", OnForceEventHerePressed, 340f);

            CreateLabel(contentRect, "Money to send", 16, 26f);
            sendMoneyInput = CreateInputField(contentRect, "1000", 340f, 46f);
            CreateButton(contentRect, "Send Money", OnSendMoneyPressed, 340f);

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

            SetCropEditVisible(false);
        }

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

            SetDurationVisible(
                selectedEventIndex >= 0 &&
                selectedEventIndex < sendEventOptions.Count &&
                sendEventOptions[selectedEventIndex].type == "FORCE_WEATHER");
        }

        private void SetDurationVisible(bool visible)
        {
            if (sendDurationLabel == null || sendDurationInput == null)
                return;

            if (durationVisible == visible)
                return;

            durationVisible = visible;
            sendDurationLabel.gameObject.SetActive(visible);
            sendDurationInput.gameObject.SetActive(visible);

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