using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace AgriDabao3D
{
    public class InventoryUIBuilder : MonoBehaviour
    {
        [Header("Performance Testing")]
        [Tooltip("Shows the frame rate in a small dark box left of the money: the live rate, " +
                 "and the average and lowest one-second rate since measuring began, " +
                 "with how long it has measured. Tap it to start a new measurement. A " +
                 "testing aid for the device performance tests - switch it off for " +
                 "release builds.")]
        public bool showFpsCounter = true;

        [Tooltip("Times how long each farm screen takes to appear after the tap that " +
                 "opens it - shop, map, backpack, AI Adviser and the rest - and lists " +
                 "the last, average and slowest time per screen. Shown under the FPS " +
                 "counter; tap it to list every screen. A testing aid for NFR-02 - " +
                 "switch it off for release builds.")]
        public bool showUiResponseTimer = true;

        [Header("Sprites")]
        public Sprite macheteSprite;
        public Sprite shovelSprite;
        public Sprite coconutSeedSprite;
        public Sprite coconutSprite;
        public Sprite wateringCanSprite;
        public Sprite bananaSeedSprite;
        public Sprite bananaSprite;
        public Sprite aphidTrapSprite;
        public Sprite sprayerPumpSprite;
        public Sprite insecticideLiterSprite;
        public Sprite disinfectantLiterSprite;
        public Sprite durianSeedSprite;
        public Sprite durianSprite;
        public Sprite pomeloSeedSprite;
        public Sprite pomeloSprite;
        public Sprite cacaoSeedSprite;
        public Sprite cacaoSprite;
        public Sprite pineappleSeedSprite;
        public Sprite pineappleSprite;
        public Sprite mangosteenSeedSprite;
        public Sprite mangosteenSprite;
        public Sprite mangoSeedSprite;
        public Sprite mangoSprite;
        [Header("New Crop Sprites")]
        public Sprite cornSeedSprite;
        public Sprite cornSprite;
        public Sprite eggplantSeedSprite;
        public Sprite eggplantSprite;
        public Sprite squashSeedSprite;
        public Sprite squashSprite;
        public Sprite strawberrySeedSprite;
        public Sprite strawberrySprite;
        public Sprite tomatoSeedSprite;
        public Sprite tomatoSprite;
        [Header("Pest and Disease Item Sprites")]
        public Sprite neemSoapLiterSprite;
        public Sprite btBioInsecticideLiterSprite;
        public Sprite copperFungicideLiterSprite;
        public Sprite pheromoneTrapSprite;
        public Sprite fruitBagSprite;
        public Sprite drainageKitSprite;
        public Sprite termiteBaitStationSprite;

        [Header("Climate Mitigation and Maintenance Item Sprites")]
        public Sprite mulchBagSprite;
        public Sprite organicCompostBagSprite;
        public Sprite pruningShearsSprite;
        public Sprite supportStakeKitSprite;
        public Sprite trellisKitSprite;
        public Sprite raisedBedKitSprite;
        public Sprite irrigationSystemKitSprite;
        public Sprite waterStorageTankKitSprite;
        public Sprite shadeNetKitSprite;
        public Sprite windbreakKitSprite;
        public Sprite greenhouseKitSprite;
        public Sprite drainageCanalKitSprite;

        [Header("Planting Material Sprites")]
        public Sprite bananaPlantletSprite;
        public Sprite bananaSuckerSprite;
        public Sprite mangoGraftedSeedlingSprite;
        public Sprite mangoLisoSprite;
        public Sprite coconutSeednutSprite;
        public Sprite pineappleSuckerSprite;
        public Sprite strawberryRunnerSprite;
        public Sprite squashSeedlingSprite;

        [Header("Layout")]
        public float hotbarSlotSize = 130f;
        public float backpackSlotSize = 110f;
        private Canvas canvas;
        private Text moneyText;
        private RectTransform moneyPlankRect;
        private readonly InventorySlotUI[] hotbarSlots = new InventorySlotUI[PlayerInventory.HotbarSlotCount];
        private readonly InventorySlotUI[] backpackSlots = new InventorySlotUI[PlayerInventory.InventorySlotCount];
        private InventorySlotUI backpackButton;
        private GameObject backpackPanel;
        private ScrollRect backpackScrollRect;
        private bool backpackOpen;
        private UIThemeSprites theme;
        public ScrollRect BackpackScrollRect => backpackScrollRect;
        public bool IsBackpackOpen => backpackOpen;

        private bool trashMode;
        private Image trashButtonImage;
        private Text trashButtonLabel;
        private InventoryTrashUI trashUI;

        public bool IsTrashMode => trashMode;
        private void Start()
        {
            EnsureEventSystem();
            EnsureCanvas();
            BuildUI();
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += Refresh;
                Refresh();
            }
        }
        private void OnDestroy()
        {
            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.OnInventoryChanged -= Refresh;
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
                GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
        private void BuildUI()
        {
            theme = UIThemeSprites.Instance;
            BuildMoneyUI();

            FpsCounterHud fpsCounter = showFpsCounter
                ? FpsCounterHud.Create(canvas.transform, moneyPlankRect)
                : null;

            if (showUiResponseTimer)
            {
                UiResponseTimerHud.Create(canvas.transform,
                    fpsCounter != null ? (RectTransform)fpsCounter.transform : null,
                    moneyPlankRect);
            }

            BuildHotbarUI();
            BuildBackpackPanel();
            SetBackpackOpen(false);
        }
        private void BuildMoneyUI()
        {
            Sprite plank = theme?.moneyPlank;

            if (plank != null)
            {
                GameObject plankGo = new GameObject("MoneyPlank", typeof(RectTransform), typeof(Image));
                plankGo.transform.SetParent(canvas.transform, false);
                HudRegistry.RegisterPiece(HudPiece.Money, plankGo);

                RectTransform plankRect = plankGo.GetComponent<RectTransform>();
                plankRect.anchorMin = plankRect.anchorMax = new Vector2(1f, 1f);
                plankRect.pivot = new Vector2(1f, 1f);
                plankRect.sizeDelta = theme.moneyPlankSize;
                plankRect.anchoredPosition = new Vector2(-20f, -14f);
                moneyPlankRect = plankRect;

                Image plankImage = plankGo.GetComponent<Image>();
                plankImage.sprite = plank;
                plankImage.preserveAspect = true;
                plankImage.raycastTarget = false;

                GameObject textGo = new GameObject("MoneyText", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(plankGo.transform, false);

                RectTransform textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.sizeDelta = new Vector2(theme.moneyPlankSize.x * 0.46f,
                                                 theme.moneyPlankSize.y * 0.30f);
                textRect.anchoredPosition = new Vector2(theme.moneyPlankSize.x * 0.18f,
                                                        -theme.moneyPlankSize.y * 0.06f);

                moneyText = textGo.GetComponent<Text>();
                moneyText.font = GameFonts.Primary;
                moneyText.fontSize = 30;
                moneyText.fontStyle = FontStyle.Bold;
                moneyText.alignment = TextAnchor.MiddleCenter;
                moneyText.color = new Color(0.20f, 0.12f, 0.04f, 1f);
                moneyText.raycastTarget = false;
                moneyText.horizontalOverflow = HorizontalWrapMode.Overflow;
                moneyText.verticalOverflow = VerticalWrapMode.Overflow;
                moneyText.text = "P0";
                return;
            }

            GameObject go = new GameObject("MoneyText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(canvas.transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(360f, 80f);
            rect.anchoredPosition = new Vector2(-30f, -30f);
            moneyText = go.GetComponent<Text>();
            moneyText.font = GameFonts.Primary;
            moneyText.fontSize = 28;
            moneyText.alignment = TextAnchor.MiddleRight;
            moneyText.color = new Color(1f, 0.95f, 0.4f);
            moneyText.raycastTarget = false;
            moneyText.text = "P0";
        }
        private void BuildHotbarUI()
        {
            GameObject root = new GameObject("InventoryHotbar", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            HudRegistry.RegisterPiece(HudPiece.Hotbar, root);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.sizeDelta = new Vector2(1220f, 150f);
            rootRect.anchoredPosition = new Vector2(0f, 25f);

            Sprite barArt = theme?.hotbarBar;
            if (barArt != null)
            {
                GameObject barGo = new GameObject("HotbarBar", typeof(RectTransform), typeof(Image));
                barGo.transform.SetParent(rootRect, false);

                RectTransform barRect = barGo.GetComponent<RectTransform>();
                barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0.5f);
                barRect.pivot = new Vector2(0.5f, 0.5f);
                barRect.sizeDelta = theme.hotbarBarSize;
                barRect.anchoredPosition = Vector2.zero;

                Image barImage = barGo.GetComponent<Image>();
                barImage.sprite = barArt;
                barImage.preserveAspect = true;
                barImage.raycastTarget = false;
            }

            float spacing = 28f;
            float step = hotbarSlotSize + spacing;
            float startX = -(step * 3f);
            for (int i = 0; i < PlayerInventory.HotbarSlotCount; i++)
            {
                Vector2 pos = new Vector2(startX + step * i, 0f);
                hotbarSlots[i] = CreateInventorySlot(rootRect, "HotbarSlot_" + i, i, pos, hotbarSlotSize);
            }
            Vector2 bagPos = new Vector2(startX + step * 6f, 0f);
            backpackButton = CreateBackpackButton(rootRect, bagPos, hotbarSlotSize);
        }
        private void BuildBackpackPanel()
        {
            backpackPanel = new GameObject("BackpackPanel", typeof(RectTransform), typeof(Image));
            backpackPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = backpackPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            Sprite board = theme?.backpackBoard;

            panelRect.sizeDelta = board != null
                ? theme.backpackBoardSize
                : new Vector2(920f, 700f);
            panelRect.anchoredPosition = new Vector2(0f, 45f);

            Image panelBg = backpackPanel.GetComponent<Image>();
            panelBg.raycastTarget = true;

            if (board != null)
            {
                panelBg.sprite = board;
                panelBg.color = Color.white;

                BuildBackpackLabel(panelRect);
                BuildBackpackCloseButton(panelRect);
                BuildTrashButton(panelRect);
            }
            else
            {
                panelBg.color = new Color(0f, 0f, 0f, 0.82f);
            }

            GameObject gridGo = new GameObject("BackpackGrid", typeof(RectTransform));
            gridGo.transform.SetParent(backpackPanel.transform, false);
            RectTransform contentRect = gridGo.GetComponent<RectTransform>();
            contentRect.anchorMin = contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);

            int columns = 6;
            int total = PlayerInventory.InventorySlotCount;
            int rows = Mathf.CeilToInt(total / (float)columns);

            if (board != null)
            {
                Vector2 gridSize = theme.backpackGridSize;
                contentRect.sizeDelta = gridSize;
                contentRect.anchoredPosition = theme.backpackGridOffset;

                float cellW = gridSize.x / columns;
                float cellH = gridSize.y / rows;
                float slotSize = Mathf.Min(cellW, cellH) * Mathf.Clamp(theme.backpackSlotFill, 0.4f, 1f);

                float firstX = -gridSize.x * 0.5f + cellW * 0.5f;
                float firstY = gridSize.y * 0.5f - cellH * 0.5f;

                for (int i = 0; i < total; i++)
                {
                    int row = i / columns;
                    int col = i % columns;
                    Vector2 pos = new Vector2(firstX + col * cellW, firstY - row * cellH);
                    backpackSlots[i] = CreateInventorySlot(contentRect, "BackpackSlot_" + i, i, pos, slotSize);
                }

                return;
            }

            float spacing = 16f;
            float step = backpackSlotSize + spacing;
            float totalW = columns * backpackSlotSize + (columns - 1) * spacing;
            float totalH = rows * backpackSlotSize + (rows - 1) * spacing;
            contentRect.sizeDelta = new Vector2(totalW, totalH);
            contentRect.anchoredPosition = Vector2.zero;

            float startX = -totalW * 0.5f + backpackSlotSize * 0.5f;
            float startY = totalH * 0.5f - backpackSlotSize * 0.5f;
            for (int i = 0; i < total; i++)
            {
                int row = i / columns;
                int col = i % columns;
                Vector2 pos = new Vector2(startX + col * step, startY - row * step);
                backpackSlots[i] = CreateInventorySlot(contentRect, "BackpackSlot_" + i, i, pos, backpackSlotSize);
            }
        }

        private void BuildBackpackLabel(RectTransform parent)
        {
            Sprite labelArt = theme?.backpackLabel;
            if (labelArt == null)
                return;

            GameObject go = new GameObject("BackpackLabel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 110f);
            rect.anchoredPosition = new Vector2(0f, 4f);

            Image image = go.GetComponent<Image>();
            image.sprite = labelArt;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void BuildBackpackCloseButton(RectTransform parent)
        {
            GameObject go = new GameObject("BackpackCloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(86f, 86f);
            rect.anchoredPosition = new Vector2(-14f, 6f);

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(CloseBackpack);

            Sprite closeArt = theme?.backpackCloseButton;
            if (closeArt != null)
            {
                image.sprite = closeArt;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
                return;
            }

            image.color = new Color(0.55f, 0.16f, 0.16f, 0.95f);
            Text text = CreateSlotText(go.transform);
            text.text = "X";
        }

        public void CloseBackpack()
        {
            SetBackpackOpen(false);
        }

        private void BuildTrashButton(RectTransform parent)
        {
            GameObject go = new GameObject("TrashItemButton",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(112f, 112f);
            rect.anchoredPosition = new Vector2(22f, 4f);

            trashButtonImage = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = trashButtonImage;
            button.onClick.AddListener(ToggleTrashMode);

            trashButtonImage.preserveAspect = true;
            trashButtonImage.color = Color.white;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 14f);
            textRect.offsetMax = new Vector2(-10f, -14f);

            trashButtonLabel = textGo.GetComponent<Text>();
            trashButtonLabel.font = GameFonts.Primary;
            trashButtonLabel.fontSize = 19;
            trashButtonLabel.fontStyle = FontStyle.Bold;
            trashButtonLabel.alignment = TextAnchor.MiddleCenter;
            trashButtonLabel.color = new Color(0.20f, 0.12f, 0.04f, 1f);
            trashButtonLabel.raycastTarget = false;
            trashButtonLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            trashButtonLabel.verticalOverflow = VerticalWrapMode.Overflow;

            PauseMenuBuilder.ApplySpriteTint(button);
            ApplyTrashButtonLook();
        }

        public void ToggleTrashMode()
        {
            SetTrashMode(!trashMode);
        }

        private void SetTrashMode(bool active)
        {
            trashMode = active;

            if (!trashMode)
                trashUI?.Hide();

            ApplyTrashButtonLook();
            Refresh();
        }

        private void ApplyTrashButtonLook()
        {
            if (trashButtonImage == null)
                return;

            Sprite art = trashMode ? theme?.backpackCloseButton : theme?.sliderKnob;
            if (art != null)
            {
                trashButtonImage.sprite = art;
                trashButtonImage.color = Color.white;
            }
            else
            {
                trashButtonImage.color = trashMode
                    ? new Color(0.55f, 0.16f, 0.16f, 0.95f)
                    : new Color(0.45f, 0.30f, 0.14f, 0.95f);
            }

            if (trashButtonLabel != null)
            {
                trashButtonLabel.text = trashMode
                    ? (theme?.backpackCloseButton != null ? string.Empty : "X")
                    : "Trash Item";
            }
        }

        public void HandleTrashSlotClicked(int slotIndex)
        {
            if (PlayerInventory.Instance == null)
                return;

            InventorySlotData slot = PlayerInventory.Instance.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty || PlayerInventory.IsTool(slot.itemType))
                return;

            if (trashUI == null)
            {
                trashUI = InventoryTrashUI.Create(canvas);
                trashUI.Discarded = Refresh;
            }

            trashUI.Begin(slotIndex);
        }
        private InventorySlotUI CreateInventorySlot(RectTransform parent, string objectName, int slotIndex, Vector2 pos, float size)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(InventorySlotUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = pos;
            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = true;
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(size * 0.56f, size * 0.56f);
            iconRect.anchoredPosition = new Vector2(0f, size * 0.08f);
            Image iconImage = iconGo.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            GameObject countGo = new GameObject("CountText", typeof(RectTransform), typeof(Text));
            countGo.transform.SetParent(go.transform, false);
            RectTransform countRect = countGo.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0f, 0f);
            countRect.anchorMax = new Vector2(1f, 0f);
            countRect.pivot = new Vector2(0.5f, 0f);
            countRect.sizeDelta = new Vector2(0f, size * 0.25f);
            countRect.anchoredPosition = new Vector2(0f, 8f);
            Text countText = countGo.GetComponent<Text>();
            countText.font = GameFonts.Primary;
            countText.fontSize = Mathf.RoundToInt(size * 0.18f);
            countText.alignment = TextAnchor.MiddleCenter;
            countText.color = Color.white;
            countText.raycastTarget = false;
            countText.verticalOverflow = VerticalWrapMode.Overflow;
            countText.horizontalOverflow = HorizontalWrapMode.Overflow;
            GameObject labelGo = new GameObject("LabelText", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(0f, size * 0.22f);
            labelRect.anchoredPosition = new Vector2(0f, -4f);
            Text labelText = labelGo.GetComponent<Text>();
            labelText.font = GameFonts.Primary;
            labelText.fontSize = Mathf.RoundToInt(size * 0.13f);
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(1f, 1f, 1f, 0.65f);
            labelText.raycastTarget = false;
            labelText.verticalOverflow = VerticalWrapMode.Overflow;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelText.text = slotIndex < PlayerInventory.HotbarSlotCount ? (slotIndex + 1).ToString() : "";
            InventorySlotUI slot = go.GetComponent<InventorySlotUI>();
            slot.Setup(this, slotIndex, false);
            slot.background = bg;
            slot.iconImage = iconImage;
            slot.countText = countText;
            slot.labelText = labelText;
            return slot;
        }
        private InventorySlotUI CreateBackpackButton(RectTransform parent, Vector2 pos, float size)
        {
            GameObject go = new GameObject("BackpackButton", typeof(RectTransform), typeof(Image), typeof(InventorySlotUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = pos;

            Image bg = go.GetComponent<Image>();
            bg.raycastTarget = true;

            Sprite basket = theme?.bagButton;
            Text text;

            if (basket != null)
            {
                bg.color = new Color(1f, 1f, 1f, 0f);

                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(size * 0.82f, size * 0.82f);
                iconRect.anchoredPosition = Vector2.zero;

                Image iconImage = iconGo.GetComponent<Image>();
                iconImage.sprite = basket;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                text = CreateSlotText(go.transform);
                text.text = "";
            }
            else
            {
                bg.color = new Color(0.10f, 0.18f, 0.28f, 0.90f);
                text = CreateSlotText(go.transform);
                text.text = "BAG";
            }

            InventorySlotUI slot = go.GetComponent<InventorySlotUI>();
            slot.Setup(this, -1, true);
            slot.background = bg;
            slot.countText = text;
            return slot;
        }
        private static Text CreateSlotText(Transform parent)
        {
            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(parent, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text text = textGo.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private void ApplyTutorialTrashGate()
        {
            if (trashButtonImage == null)
                return;

            bool allowed = !TutorialState.IsRunning;

            if (!allowed && trashMode)
                SetTrashMode(false);

            trashButtonImage.gameObject.SetActive(allowed);
        }

        public void ToggleBackpack()
        {
            SetBackpackOpen(!backpackOpen);
        }
        private void SetBackpackOpen(bool open)
        {
            backpackOpen = open;

            if (!open && trashMode)
                SetTrashMode(false);

            ApplyTutorialTrashGate();

            if (backpackPanel != null)
            {
                HudRegistry.RegisterPiece(HudPiece.BackpackPanel, backpackPanel);
                if (backpackOpen)
                    HudRegistry.CloseOtherPanels(HudPiece.BackpackPanel);

                backpackPanel.SetActive(backpackOpen);
            }
            Refresh();
        }
        public void HandleSlotClicked(int slotIndex)
        {
            if (PlayerInventory.Instance == null)
                return;
            if (PlayerInventory.Instance.IsHotbarSlot(slotIndex))
                PlayerInventory.Instance.SelectSlot(slotIndex);
        }
        public void HandleSlotDropped(int fromSlotIndex, int toSlotIndex)
        {
            if (PlayerInventory.Instance == null)
                return;
            InventorySlotData from = PlayerInventory.Instance.GetSlot(fromSlotIndex);
            InventorySlotData to = PlayerInventory.Instance.GetSlot(toSlotIndex);
            if (from != null &&
                to != null &&
                PlayerInventory.IsLiquidItem(from.itemType) &&
                to.itemType == InventoryItemType.SprayerPump)
            {
                PlayerInventory.Instance.TryLoadSprayerFromSlot(fromSlotIndex, toSlotIndex);
                return;
            }
            PlayerInventory.Instance.MoveOrSwapSlots(fromSlotIndex, toSlotIndex);
        }
        private void Refresh()
        {
            ApplyTutorialTrashGate();

            if (PlayerInventory.Instance == null)
                return;
            for (int i = 0; i < hotbarSlots.Length; i++)
                UpdateSlotVisual(hotbarSlots[i]);
            for (int i = 0; i < backpackSlots.Length; i++)
                UpdateSlotVisual(backpackSlots[i]);
            if (moneyText != null)
                moneyText.text = $"P{PlayerInventory.Instance.money}";
            if (backpackButton != null && backpackButton.background != null &&
                (theme == null || theme.bagButton == null))
            {
                backpackButton.background.color = backpackOpen
                    ? new Color(0.25f, 0.55f, 0.25f, 0.95f)
                    : new Color(0.10f, 0.18f, 0.28f, 0.90f);
            }
        }
        private void UpdateSlotVisual(InventorySlotUI slotUI)
        {
            if (slotUI == null || PlayerInventory.Instance == null)
                return;
            InventorySlotData slot = PlayerInventory.Instance.GetSlot(slotUI.slotIndex);
            if (slot == null || slot.IsEmpty)
            {
                slotUI.itemType = InventoryItemType.None;
                if (slotUI.iconImage != null)
                {
                    slotUI.iconImage.enabled = false;
                    slotUI.iconImage.sprite = null;
                }
                if (slotUI.countText != null)
                    slotUI.countText.text = "";
                ApplySlotColor(slotUI);
                return;
            }
            slotUI.itemType = slot.itemType;
            if (slotUI.iconImage != null)
            {
                slotUI.iconImage.sprite = GetSprite(slot.itemType);
                slotUI.iconImage.enabled = slotUI.iconImage.sprite != null;
            }
            if (slotUI.countText != null)
            {
                if (slot.itemType == InventoryItemType.SprayerPump)
                {
                    slotUI.countText.text = slot.sprayerLiquid == SprayerLiquidType.None
                        ? "Empty"
                        : $"{slot.sprayerLiquid}\n{slot.liquidMl / 1000f:F1}L";
                }
                else if (PlayerInventory.IsLiquidItem(slot.itemType))
                {
                    slotUI.countText.text = $"{slot.liquidMl / 1000f:F1}L";
                }
                else if (PlayerInventory.IsTool(slot.itemType))
                {
                    slotUI.countText.text = "Owned";
                }
                else
                {
                    slotUI.countText.text = slot.amount.ToString();
                }
            }

            if (slotUI.iconImage != null)
            {
                bool untrashable = trashMode && PlayerInventory.IsTool(slot.itemType);
                slotUI.iconImage.color = untrashable
                    ? new Color(1f, 1f, 1f, 0.35f)
                    : Color.white;
            }

            ApplySlotColor(slotUI);
        }
        private void ApplySlotColor(InventorySlotUI slotUI)
        {
            if (slotUI == null || slotUI.background == null || PlayerInventory.Instance == null)
                return;
            bool selected = PlayerInventory.Instance.selectedSlotIndex == slotUI.slotIndex &&
                            PlayerInventory.Instance.IsHotbarSlot(slotUI.slotIndex);

            bool themed = theme != null &&
                          (theme.hotbarBar != null || theme.backpackBoard != null);

            if (themed)
            {
                slotUI.background.color = selected
                    ? new Color(0.35f, 0.85f, 0.35f, 0.42f)
                    : new Color(1f, 1f, 1f, 0f);
                return;
            }

            if (selected)
            {
                slotUI.background.color = new Color(0.25f, 0.60f, 0.25f, 0.88f);
                return;
            }
            if (slotUI.slotIndex >= 0 && slotUI.slotIndex < PlayerInventory.HotbarSlotCount)
            {
                slotUI.background.color = new Color(0.05f, 0.10f, 0.12f, 0.68f);
                return;
            }
            slotUI.background.color = new Color(0f, 0f, 0f, 0.55f);
        }
        public Sprite GetSpriteFor(InventoryItemType item) => GetSprite(item);

        private Sprite GetSprite(InventoryItemType item)
        {
            switch (item)
            {
                case InventoryItemType.Machete:
                    return macheteSprite;
                case InventoryItemType.Shovel:
                    return shovelSprite;
                case InventoryItemType.CoconutSeed:
                    return coconutSeedSprite;
                case InventoryItemType.Coconut:
                    return coconutSprite;
                case InventoryItemType.BananaSeed:
                    return bananaSeedSprite;
                case InventoryItemType.Banana:
                    return bananaSprite;
                case InventoryItemType.WateringCan:
                    return wateringCanSprite;
                case InventoryItemType.DurianSeed:
                    return durianSeedSprite;
                case InventoryItemType.Durian:
                    return durianSprite;
                case InventoryItemType.PomeloSeed:
                    return pomeloSeedSprite;
                case InventoryItemType.Pomelo:
                    return pomeloSprite;
                case InventoryItemType.CacaoSeed:
                    return cacaoSeedSprite;
                case InventoryItemType.Cacao:
                    return cacaoSprite;
                case InventoryItemType.PineappleSeed:
                    return pineappleSeedSprite;
                case InventoryItemType.Pineapple:
                    return pineappleSprite;
                case InventoryItemType.MangosteenSeed:
                    return mangosteenSeedSprite;
                case InventoryItemType.Mangosteen:
                    return mangosteenSprite;
                case InventoryItemType.MangoSeed:
                    return mangoSeedSprite;
                case InventoryItemType.Mango:
                    return mangoSprite;
                case InventoryItemType.AphidTrap:
                    return aphidTrapSprite;
                case InventoryItemType.SprayerPump:
                    return sprayerPumpSprite;
                case InventoryItemType.InsecticideLiter:
                    return insecticideLiterSprite;
                case InventoryItemType.DisinfectantLiter:
                    return disinfectantLiterSprite;
                case InventoryItemType.CornSeed:
                    return cornSeedSprite;
                case InventoryItemType.Corn:
                    return cornSprite;
                case InventoryItemType.EggplantSeed:
                    return eggplantSeedSprite;
                case InventoryItemType.Eggplant:
                    return eggplantSprite;
                case InventoryItemType.SquashSeed:
                    return squashSeedSprite;
                case InventoryItemType.Squash:
                    return squashSprite;
                case InventoryItemType.StrawberrySeed:
                    return strawberrySeedSprite;
                case InventoryItemType.Strawberry:
                    return strawberrySprite;
                case InventoryItemType.TomatoSeed:
                    return tomatoSeedSprite;
                case InventoryItemType.Tomato:
                    return tomatoSprite;
                case InventoryItemType.NeemSoapLiter:
                    return neemSoapLiterSprite;
                case InventoryItemType.BtBioInsecticideLiter:
                    return btBioInsecticideLiterSprite;
                case InventoryItemType.CopperFungicideLiter:
                    return copperFungicideLiterSprite;
                case InventoryItemType.PheromoneTrap:
                    return pheromoneTrapSprite;
                case InventoryItemType.FruitBag:
                    return fruitBagSprite;
                case InventoryItemType.DrainageKit:
                    return drainageKitSprite;
                case InventoryItemType.TermiteBaitStation:
                    return termiteBaitStationSprite;
                case InventoryItemType.MulchBag:
                    return mulchBagSprite;
                case InventoryItemType.OrganicCompostBag:
                    return organicCompostBagSprite;
                case InventoryItemType.PruningShears:
                    return pruningShearsSprite;
                case InventoryItemType.SupportStakeKit:
                    return supportStakeKitSprite;
                case InventoryItemType.TrellisKit:
                    return trellisKitSprite;
                case InventoryItemType.RaisedBedKit:
                    return raisedBedKitSprite;
                case InventoryItemType.IrrigationSystemKit:
                    return irrigationSystemKitSprite;
                case InventoryItemType.WaterStorageTankKit:
                    return waterStorageTankKitSprite;
                case InventoryItemType.ShadeNetKit:
                    return shadeNetKitSprite;
                case InventoryItemType.WindbreakKit:
                    return windbreakKitSprite;
                case InventoryItemType.GreenhouseKit:
                    return greenhouseKitSprite;
                case InventoryItemType.DrainageCanalKit:
                    return drainageCanalKitSprite;
                case InventoryItemType.BananaPlantlet:
                    return bananaPlantletSprite;
                case InventoryItemType.BananaSucker:
                    return bananaSuckerSprite;
                case InventoryItemType.MangoGraftedSeedling:
                    return mangoGraftedSeedlingSprite;
                case InventoryItemType.MangoLiso:
                    return mangoLisoSprite;
                case InventoryItemType.CoconutSeednut:
                    return coconutSeednutSprite;
                case InventoryItemType.PineappleSucker:
                    return pineappleSuckerSprite;
                case InventoryItemType.StrawberryRunner:
                    return strawberryRunnerSprite;
                case InventoryItemType.SquashSeedling:
                    return squashSeedlingSprite;
                default:
                    return null;
            }
        }
    }
}

