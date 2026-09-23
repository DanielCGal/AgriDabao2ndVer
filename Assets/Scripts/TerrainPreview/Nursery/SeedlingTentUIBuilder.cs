using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// The Seedling Tent panel: a round HUD button, a board with the tent's eight
    /// seedling bags, and the actions for the bag picked - fill it with soil, sow,
    /// prick out, transplant or empty it.
    ///
    /// Built in code like every other farm panel, on the shop's board art, with
    /// the painted bag sprites for the bags themselves. Transplanting closes the
    /// panel and shows a banner along the top of the screen until the seedling is
    /// in the ground or the player cancels.
    /// </summary>
    public class SeedlingTentUIBuilder : MonoBehaviour
    {
        public static SeedlingTentUIBuilder Instance { get; private set; }

        private const float PanelWidth = 1300f;
        private const float PanelHeight = 700f;
        private const int Columns = 4;
        private const int Rows = 2;

        private static readonly Color CellColor = new Color(0.16f, 0.10f, 0.05f, 0.55f);
        private static readonly Color CellSelectedColor = new Color(0.36f, 0.62f, 0.26f, 0.85f);
        private static readonly Color CellReadyColor = new Color(0.62f, 0.52f, 0.12f, 0.75f);
        private static readonly Color TextLight = new Color(1f, 0.97f, 0.88f, 1f);

        private Canvas canvas;
        private UIThemeSprites theme;
        private GameObject panel;
        private GameObject heading;
        private RectTransform gridRoot;
        private Image previewBag;
        private Image previewMaterial;
        private Text previewName;
        private Text detailText;
        private Text statusText;
        private Text summaryText;
        private RectTransform actionRow;
        private GameObject detailArea;
        private GameObject sowPicker;
        private RectTransform sowGrid;
        private DescriptionBoard descriptionBoard;
        private Button descriptionButton;

        private GameObject banner;
        private Text bannerText;

        private readonly List<CellView> cells = new List<CellView>();
        private int selectedSlot;
        private string shownActionKey;
        private float nextLiveRefresh;
        private InventoryUIBuilder inventoryUI;

        private sealed class CellView
        {
            public Image background;
            public Image bag;
            public Image material;
            public Text label;
        }

        public bool IsOpen => panel != null && panel.activeSelf;

        /// <summary>Opens the panel, creating it first if the scene has not yet.</summary>
        public static void OpenPanel()
        {
            SeedlingTentUIBuilder builder = Instance != null
                ? Instance
                : FindFirstObjectByType<SeedlingTentUIBuilder>();

            if (builder != null)
                builder.Open();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            theme = UIThemeSprites.Instance;
            EnsureEventSystem();
            EnsureCanvas();
            BuildHudButton();
            BuildPanel();
            BuildBanner();
            panel.SetActive(false);
            banner.SetActive(false);

            if (NurserySystem.Instance != null)
                NurserySystem.Instance.Changed += OnNurseryChanged;
        }

        private void OnDestroy()
        {
            if (NurserySystem.Instance != null)
                NurserySystem.Instance.Changed -= OnNurseryChanged;

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            NurserySystem nursery = NurserySystem.Instance;

            bool transplanting = nursery != null && nursery.IsTransplanting;
            if (banner != null && banner.activeSelf != transplanting)
            {
                banner.SetActive(transplanting);
                if (transplanting)
                    RefreshBanner();
            }

            if (!IsOpen)
                return;

            // The tutorial's objective plank covers the top centre of the
            // screen, where this heading sits. It is the same plank art, so
            // while it is up it stands in for the heading rather than half
            // hiding it.
            bool showHeading = !TutorialDialogueUI.ObjectiveShowing;
            if (heading != null && heading.activeSelf != showHeading)
                heading.SetActive(showHeading);

            // Days-left figures tick down while the panel is open.
            if (Time.unscaledTime >= nextLiveRefresh)
            {
                nextLiveRefresh = Time.unscaledTime + 1f;
                Redraw();
            }
        }

        private void OnNurseryChanged()
        {
            if (IsOpen)
                Redraw();

            if (banner != null && banner.activeSelf)
                RefreshBanner();
        }

        // ---------------------------------------------------------- open/close

        public void Open()
        {
            if (panel == null)
                return;

            HudRegistry.CloseOtherPanels(HudPiece.SeedlingTentPanel);
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            ShowDetail();
            SetStatus(string.Empty);
            Redraw();
        }

        public void Close()
        {
            if (descriptionBoard != null)
                descriptionBoard.Hide();

            if (panel != null)
                panel.SetActive(false);
        }

        private void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        // ------------------------------------------------------------- building

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

        private void BuildHudButton()
        {
            HudIconButton.Create(
                canvas.transform,
                "SeedlingTentButton",
                theme?.seedlingTentButton,
                HudIconButton.SlotSeedlingTent,
                "TENT",
                Toggle,
                out _,
                out _);
        }

        private void BuildPanel()
        {
            Sprite board = theme?.shopBoard;

            panel = new GameObject("SeedlingTentPanel", typeof(RectTransform), typeof(Image));
            HudRegistry.RegisterPiece(HudPiece.SeedlingTentPanel, panel);
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rect.anchoredPosition = Vector2.zero;

            Image background = panel.GetComponent<Image>();
            background.raycastTarget = true;
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

            // Heading on a blank plank, as the account screens do it: there is no
            // painted "Seedling Tent" sign, and this keeps it in the same style.
            Sprite plank = theme?.tutorialObjectiveBoard;
            heading = UIPlank.Create(panel.transform, "Heading", plank, false,
                UIPlank.SizeFor(plank, 420f, 96f), new Vector2(0f, 30f), "SEEDLING TENT", 30, out _);

            summaryText = CreateText(panel.transform, "Summary", 20, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(-250f, -92f), new Vector2(560f, 36f));

            BuildGrid();
            BuildDetailArea();
            BuildSowPicker();

            statusText = CreateText(panel.transform, "Status", 20, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(1000f, 44f));
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 14;
            statusText.resizeTextMaxSize = 20;

            BuildCloseButton();

            descriptionBoard = DescriptionBoard.Create(panel.transform, theme);
        }

        private void BuildGrid()
        {
            GameObject gridGo = new GameObject("BagGrid", typeof(RectTransform));
            gridGo.transform.SetParent(panel.transform, false);
            gridRoot = gridGo.GetComponent<RectTransform>();
            gridRoot.anchorMin = gridRoot.anchorMax = new Vector2(0.5f, 0.5f);
            gridRoot.pivot = new Vector2(0.5f, 0.5f);
            gridRoot.sizeDelta = new Vector2(620f, 420f);
            gridRoot.anchoredPosition = new Vector2(-250f, -10f);

            float cellW = gridRoot.sizeDelta.x / Columns;
            float cellH = gridRoot.sizeDelta.y / Rows;

            for (int slot = 0; slot < NurserySystem.BagCount; slot++)
            {
                int row = slot / Columns;
                int col = slot % Columns;
                Vector2 position = new Vector2(
                    -gridRoot.sizeDelta.x * 0.5f + cellW * (col + 0.5f),
                    gridRoot.sizeDelta.y * 0.5f - cellH * (row + 0.5f));

                cells.Add(BuildCell(slot, position, new Vector2(cellW - 14f, cellH - 14f)));
            }
        }

        private CellView BuildCell(int slot, Vector2 position, Vector2 size)
        {
            GameObject cellGo = new GameObject("Bag" + (slot + 1), typeof(RectTransform), typeof(Image), typeof(Button));
            cellGo.transform.SetParent(gridRoot, false);

            RectTransform rect = cellGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            CellView view = new CellView { background = cellGo.GetComponent<Image>() };
            view.background.color = CellColor;

            Button button = cellGo.GetComponent<Button>();
            button.targetGraphic = view.background;
            int captured = slot;
            button.onClick.AddListener(() => SelectSlot(captured));

            view.bag = CreateImage(cellGo.transform, "Bag", new Vector2(0f, 16f), new Vector2(size.x * 0.72f, size.y * 0.62f));
            view.material = CreateImage(cellGo.transform, "Material", new Vector2(size.x * 0.24f, size.y * 0.24f),
                new Vector2(size.x * 0.42f, size.x * 0.42f));

            view.label = CreateText(cellGo.transform, "Label", 16, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(size.x - 8f, 44f));
            view.label.resizeTextForBestFit = true;
            view.label.resizeTextMinSize = 11;
            view.label.resizeTextMaxSize = 16;

            return view;
        }

        private void BuildDetailArea()
        {
            detailArea = new GameObject("Detail", typeof(RectTransform));
            detailArea.transform.SetParent(panel.transform, false);
            RectTransform areaRect = detailArea.GetComponent<RectTransform>();
            areaRect.anchorMin = areaRect.anchorMax = new Vector2(0.5f, 0.5f);
            areaRect.pivot = new Vector2(0.5f, 0.5f);
            areaRect.sizeDelta = new Vector2(420f, 520f);
            areaRect.anchoredPosition = new Vector2(300f, 0f);

            GameObject frame = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(detailArea.transform, false);
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 1f);
            frameRect.pivot = new Vector2(0.5f, 1f);
            frameRect.sizeDelta = new Vector2(250f, 250f);
            frameRect.anchoredPosition = new Vector2(0f, -10f);

            Image frameImage = frame.GetComponent<Image>();
            if (theme?.shopPreviewFrame != null)
            {
                frameImage.sprite = theme.shopPreviewFrame;
                frameImage.color = Color.white;
            }
            else
            {
                frameImage.color = new Color(0f, 0f, 0f, 0.35f);
            }

            previewBag = CreateImage(frame.transform, "Bag", new Vector2(0f, -6f), new Vector2(120f, 120f));
            previewMaterial = CreateImage(frame.transform, "Material", new Vector2(58f, 40f), new Vector2(72f, 72f));

            previewName = CreateText(frame.transform, "Name", 18, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(220f, 40f));
            previewName.resizeTextForBestFit = true;
            previewName.resizeTextMinSize = 11;
            previewName.resizeTextMaxSize = 18;

            // Below the frame, which runs 10 to 260 down, and above the action
            // buttons, whose tops reach about 380 down.
            detailText = CreateText(detailArea.transform, "DetailText", 18, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -326f), new Vector2(420f, 104f));
            detailText.resizeTextForBestFit = true;
            detailText.resizeTextMinSize = 12;
            detailText.resizeTextMaxSize = 18;

            GameObject rowGo = new GameObject("Actions", typeof(RectTransform));
            rowGo.transform.SetParent(detailArea.transform, false);
            actionRow = rowGo.GetComponent<RectTransform>();
            actionRow.anchorMin = actionRow.anchorMax = new Vector2(0.5f, 0f);
            actionRow.pivot = new Vector2(0.5f, 0f);
            actionRow.sizeDelta = new Vector2(420f, 90f);
            actionRow.anchoredPosition = new Vector2(0f, 44f);

            descriptionButton = DescriptionBoard.CreateOpenButton(
                detailArea.transform, theme, "CHECK DESCRIPTION", new Vector2(300f, 54f), ShowDescription);
            RectTransform descriptionRect = descriptionButton.GetComponent<RectTransform>();
            descriptionRect.anchorMin = descriptionRect.anchorMax = new Vector2(0.5f, 0f);
            descriptionRect.pivot = new Vector2(0.5f, 0f);
            descriptionRect.anchoredPosition = new Vector2(0f, -14f);
        }

        private void BuildSowPicker()
        {
            sowPicker = new GameObject("SowPicker", typeof(RectTransform));
            sowPicker.transform.SetParent(panel.transform, false);
            RectTransform pickerRect = sowPicker.GetComponent<RectTransform>();
            pickerRect.anchorMin = pickerRect.anchorMax = new Vector2(0.5f, 0.5f);
            pickerRect.pivot = new Vector2(0.5f, 0.5f);
            pickerRect.sizeDelta = new Vector2(440f, 520f);
            pickerRect.anchoredPosition = new Vector2(300f, 0f);

            CreateText(sowPicker.transform, "Heading", 22, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(420f, 44f)).text =
                "What goes in this bag?";

            GameObject gridGo = new GameObject("Choices", typeof(RectTransform));
            gridGo.transform.SetParent(sowPicker.transform, false);
            sowGrid = gridGo.GetComponent<RectTransform>();
            sowGrid.anchorMin = sowGrid.anchorMax = new Vector2(0.5f, 1f);
            sowGrid.pivot = new Vector2(0.5f, 1f);
            sowGrid.sizeDelta = new Vector2(420f, 360f);
            sowGrid.anchoredPosition = new Vector2(0f, -60f);

            Button back = UIPlank.CreateButton(sowPicker.transform, "Back", theme?.tradeRequestBoard, true,
                new Vector2(200f, 70f), Vector2.zero, "Back", 22, ShowDetail);
            RectTransform backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 14f);

            sowPicker.SetActive(false);
        }

        private void BuildCloseButton()
        {
            Sprite art = theme?.backpackCloseButton != null ? theme.backpackCloseButton : theme?.closeButton;

            GameObject go = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel.transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = art != null ? new Vector2(96f, 96f) : new Vector2(80f, 60f);
            rect.anchoredPosition = new Vector2(-40f, -26f);

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Close);

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
            }
            else
            {
                image.color = new Color(0.55f, 0.16f, 0.16f, 0.95f);
                Text label = CreateText(go.transform, "Text", 22, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), Vector2.zero, rect.sizeDelta);
                label.text = "X";
            }
        }

        private void BuildBanner()
        {
            Sprite plank = theme?.tradeRequestBoard;

            banner = new GameObject("TransplantBanner", typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(canvas.transform, false);
            RectTransform rect = banner.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(820f, 110f);
            rect.anchoredPosition = new Vector2(0f, -130f);

            Image image = banner.GetComponent<Image>();
            if (plank != null)
            {
                image.sprite = plank;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.25f, 0.16f, 0.07f, 0.92f);
            }

            bannerText = CreateText(banner.transform, "Text", 20, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(560f, 90f));
            RectTransform textRect = bannerText.rectTransform;
            textRect.pivot = new Vector2(0f, 0.5f);
            bannerText.resizeTextForBestFit = true;
            bannerText.resizeTextMinSize = 13;
            bannerText.resizeTextMaxSize = 20;

            Sprite cancelArt = theme?.cancelButton;
            GameObject cancelGo = new GameObject("Cancel", typeof(RectTransform), typeof(Image), typeof(Button));
            cancelGo.transform.SetParent(banner.transform, false);
            RectTransform cancelRect = cancelGo.GetComponent<RectTransform>();
            cancelRect.anchorMin = cancelRect.anchorMax = new Vector2(1f, 0.5f);
            cancelRect.pivot = new Vector2(1f, 0.5f);
            cancelRect.sizeDelta = new Vector2(170f, 62f);
            cancelRect.anchoredPosition = new Vector2(-46f, 0f);

            Image cancelImage = cancelGo.GetComponent<Image>();
            Button cancel = cancelGo.GetComponent<Button>();
            cancel.targetGraphic = cancelImage;
            cancel.onClick.AddListener(() => NurserySystem.Instance?.CancelTransplant());

            if (cancelArt != null)
            {
                cancelImage.sprite = cancelArt;
                cancelImage.preserveAspect = true;
                cancelImage.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(cancel);
            }
            else
            {
                cancelImage.color = new Color(0.55f, 0.16f, 0.16f, 0.95f);
                Text label = CreateText(cancelGo.transform, "Text", 20, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 0.5f), Vector2.zero, cancelRect.sizeDelta);
                label.text = "Cancel";
            }
        }

        // ------------------------------------------------------------- drawing

        private void Redraw()
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null || panel == null)
                return;

            int ready = nursery.CountReady();
            int used = NurserySystem.BagCount - nursery.CountBags(SeedlingBagStatus.Empty);
            summaryText.text = used + " of " + NurserySystem.BagCount + " bags in use" +
                               (ready > 0 ? "  -  " + ready + " ready to transplant" : string.Empty);

            for (int slot = 0; slot < cells.Count; slot++)
                DrawCell(nursery, slot, cells[slot]);

            DrawDetail(nursery);
        }

        private void DrawCell(NurserySystem nursery, int slot, CellView view)
        {
            SeedlingBagStatus status = nursery.GetStatus(slot);

            view.background.color = slot == selectedSlot
                ? CellSelectedColor
                : status == SeedlingBagStatus.Ready || status == SeedlingBagStatus.NeedsPricking
                    ? CellReadyColor
                    : CellColor;

            SetSprite(view.bag, status == SeedlingBagStatus.Empty ? theme?.seedlingBagNoSoil : theme?.seedlingBagWithSoil);

            bool sown = nursery.TryGetMaterial(slot, out PlantingMaterialInfo info);
            SetSprite(view.material, sown ? ItemIcon(info.Item) : null);

            view.label.text = ShortStatus(nursery, slot, status, info);
        }

        private static string ShortStatus(NurserySystem nursery, int slot, SeedlingBagStatus status, PlantingMaterialInfo info)
        {
            switch (status)
            {
                case SeedlingBagStatus.Empty: return "Empty";
                case SeedlingBagStatus.Filled: return "Soil - ready to sow";
                case SeedlingBagStatus.Germinating:
                    return CropName(info) + "\nGerminating " + DaysShort(nursery.DaysUntilNextStep(slot));
                case SeedlingBagStatus.NeedsPricking: return CropName(info) + "\nPrick out!";
                case SeedlingBagStatus.Growing:
                    return CropName(info) + "\n" + DaysShort(nursery.DaysUntilNextStep(slot)) + " left";
                case SeedlingBagStatus.Ready: return CropName(info) + "\nReady!";
                default: return string.Empty;
            }
        }

        private static string CropName(PlantingMaterialInfo info)
        {
            return info != null ? info.Crop.ToString() : string.Empty;
        }

        /// <summary>The seed while it is still in the seed tray, the seedling after that.</summary>
        private static string BagContentName(SeedlingBagStatus status, PlantingMaterialInfo info)
        {
            return status == SeedlingBagStatus.Germinating
                ? info.Name
                : PlantingMaterialCatalog.SeedlingName(info);
        }

        private static string DaysShort(float days)
        {
            return days.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " d";
        }

        private void DrawDetail(NurserySystem nursery)
        {
            if (sowPicker != null && sowPicker.activeSelf)
                return;

            SeedlingBagStatus status = nursery.GetStatus(selectedSlot);
            bool sown = nursery.TryGetMaterial(selectedSlot, out PlantingMaterialInfo info);

            SetSprite(previewBag, status == SeedlingBagStatus.Empty ? theme?.seedlingBagNoSoil : theme?.seedlingBagWithSoil);
            SetSprite(previewMaterial, sown ? ItemIcon(info.Item) : null);

            previewName.text = sown ? BagContentName(status, info) : "Bag " + (selectedSlot + 1);
            detailText.text = DetailText(nursery, status, info);

            if (descriptionButton != null)
                descriptionButton.gameObject.SetActive(sown);

            // The panel redraws every second for the countdowns; the buttons are
            // only rebuilt when they would change, so a tap is never lost to a
            // button being replaced under the finger.
            string actionKey = selectedSlot + ":" + status + ":" + sown;
            if (actionKey != shownActionKey)
            {
                shownActionKey = actionKey;
                RebuildActions(status, sown);
            }
        }

        private string DetailText(NurserySystem nursery, SeedlingBagStatus status, PlantingMaterialInfo info)
        {
            float next = nursery.DaysUntilNextStep(selectedSlot);

            switch (status)
            {
                case SeedlingBagStatus.Empty:
                    return "An empty seedling bag. Fill it with soil to use it.";
                case SeedlingBagStatus.Filled:
                    return "Filled with soil. Sow a seed, or set a plantlet or grafted seedling, in it.";
                case SeedlingBagStatus.Germinating:
                    return "Germinating in the seed tray. Prick it into this bag in " +
                           PlantingMaterialCatalog.FormatDays(next) + ".";
                case SeedlingBagStatus.NeedsPricking:
                    return "It has germinated. Prick the seedling into this bag so it can keep growing.";
                case SeedlingBagStatus.Growing:
                    bool hardening = info != null &&
                                     (info.Item == InventoryItemType.BananaPlantlet ||
                                      info.Item == InventoryItemType.MangoGraftedSeedling);
                    return (hardening ? "Hardening. " : "Growing. ") + "Ready to transplant in " +
                           PlantingMaterialCatalog.FormatDays(next) + ".";
                case SeedlingBagStatus.Ready:
                    return "Ready to transplant into a " + PlantingMaterialCatalog.PlotName(info.Plot) +
                           (info.NeedsMulchedBed ? " (mulched)" : string.Empty) +
                           ". It has grown " + PlantingMaterialCatalog.FormatDays(nursery.DaysInNursery(selectedSlot)) +
                           " here, and that time counts toward its age in the field.";
                default:
                    return string.Empty;
            }
        }

        private void RebuildActions(SeedlingBagStatus status, bool sown)
        {
            for (int i = actionRow.childCount - 1; i >= 0; i--)
                Destroy(actionRow.GetChild(i).gameObject);

            List<(string label, Sprite art, UnityEngine.Events.UnityAction action)> actions =
                new List<(string, Sprite, UnityEngine.Events.UnityAction)>();

            switch (status)
            {
                case SeedlingBagStatus.Empty:
                    actions.Add(("Fill with Soil", theme?.seedlingBagEmptyIcon, OnFill));
                    break;
                case SeedlingBagStatus.Filled:
                    actions.Add(("Sow", theme?.seedlingBagSproutIcon, OpenSowPicker));
                    actions.Add(("Empty Bag", null, OnEmpty));
                    break;
                case SeedlingBagStatus.NeedsPricking:
                    actions.Add(("Prick Out", null, OnPrick));
                    actions.Add(("Empty Bag", null, OnEmpty));
                    break;
                case SeedlingBagStatus.Ready:
                    actions.Add(("Transplant", null, OnTransplant));
                    actions.Add(("Empty Bag", null, OnEmpty));
                    break;
                default:
                    if (sown)
                        actions.Add(("Empty Bag", null, OnEmpty));
                    break;
            }

            float width = 190f;
            float spacing = 14f;
            float start = -(actions.Count - 1) * (width + spacing) * 0.5f;

            for (int i = 0; i < actions.Count; i++)
            {
                (string label, Sprite art, UnityEngine.Events.UnityAction action) = actions[i];
                Vector2 position = new Vector2(start + i * (width + spacing), 0f);
                CreateActionButton(label, art, action, position, width);
            }
        }

        /// <summary>
        /// A round painted button with its caption under it when the art exists,
        /// otherwise a written plank - so the painted bag icons get used, and the
        /// actions without art still read clearly.
        /// </summary>
        private void CreateActionButton(string label, Sprite art, UnityEngine.Events.UnityAction action,
            Vector2 position, float width)
        {
            if (art == null)
            {
                Button plankButton = UIPlank.CreateButton(actionRow, label, theme?.tradeRequestBoard, true,
                    new Vector2(width, 76f), Vector2.zero, label, 21, action);
                RectTransform plankRect = (RectTransform)plankButton.transform;
                plankRect.anchorMin = plankRect.anchorMax = new Vector2(0.5f, 0.5f);
                plankRect.pivot = new Vector2(0.5f, 0.5f);
                plankRect.anchoredPosition = position;
                return;
            }

            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(actionRow, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(78f, 78f);
            rect.anchoredPosition = position + new Vector2(0f, 10f);

            Image image = go.GetComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            PauseMenuBuilder.ApplySpriteTint(button);

            Text caption = CreateText(go.transform, "Caption", 18, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0f), new Vector2(0f, -2f), new Vector2(width, 30f));
            caption.rectTransform.pivot = new Vector2(0.5f, 1f);
            caption.text = label;
        }

        private void RefreshBanner()
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null || bannerText == null)
                return;

            if (nursery.TryGetTransplant(out PlantingMaterialInfo info, out _))
            {
                bannerText.text = "Carrying the " + PlantingMaterialCatalog.SeedlingName(info) + ". Tap a prepared " +
                                  PlantingMaterialCatalog.PlotName(info.Plot) +
                                  (info.NeedsMulchedBed ? " that is mulched" : string.Empty) +
                                  " to transplant it.";
            }
        }

        // ------------------------------------------------------------- actions

        private void SelectSlot(int slot)
        {
            selectedSlot = slot;
            ShowDetail();
            SetStatus(string.Empty);
            Redraw();
        }

        private void ShowDetail()
        {
            if (sowPicker != null)
                sowPicker.SetActive(false);
            if (detailArea != null)
                detailArea.SetActive(true);

            if (IsOpen)
                Redraw();
        }

        private void OnFill()
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null)
                return;

            nursery.FillBag(selectedSlot, out string message);
            SetStatus(message);
        }

        private void OpenSowPicker()
        {
            for (int i = sowGrid.childCount - 1; i >= 0; i--)
                Destroy(sowGrid.GetChild(i).gameObject);

            List<InventoryItemType> held = new List<InventoryItemType>();
            if (PlayerInventory.Instance != null)
            {
                foreach (InventoryItemType item in PlantingMaterialCatalog.AllMaterials)
                {
                    if (PlantingMaterialCatalog.IsNurseryMaterial(item) && PlayerInventory.Instance.GetCount(item) > 0)
                        held.Add(item);
                }
            }

            if (held.Count == 0)
            {
                SetStatus("You have nothing to sow in a bag. Seeds, banana plantlets and grafted " +
                          "mango seedlings are sold at the shop.");
                return;
            }

            const int pickerColumns = 3;
            float cellW = sowGrid.sizeDelta.x / pickerColumns;
            float cellH = 118f;

            for (int i = 0; i < held.Count; i++)
            {
                InventoryItemType item = held[i];
                int row = i / pickerColumns;
                int col = i % pickerColumns;

                // Each row is centred, so one or two choices do not sit off to the left.
                int inRow = Mathf.Min(pickerColumns, held.Count - row * pickerColumns);
                Vector2 position = new Vector2(-cellW * inRow * 0.5f + cellW * (col + 0.5f), -cellH * (row + 0.5f));
                CreateSowChoice(item, position, new Vector2(cellW - 12f, cellH - 10f));
            }

            detailArea.SetActive(false);
            sowPicker.SetActive(true);
            SetStatus("Pick what to sow in bag " + (selectedSlot + 1) + ".");
        }

        private void CreateSowChoice(InventoryItemType item, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject("Sow_" + item, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(sowGrid, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image background = go.GetComponent<Image>();
            background.color = CellColor;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => OnSow(item));

            Image icon = CreateImage(go.transform, "Icon", new Vector2(0f, 16f), new Vector2(size.y * 0.55f, size.y * 0.55f));
            SetSprite(icon, ItemIcon(item));

            int count = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetCount(item) : 0;
            Text label = CreateText(go.transform, "Label", 15, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(size.x - 6f, 40f));
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 15;
            label.text = PlantingMaterialCatalog.NameOf(item) + " x" + count;
        }

        private void OnSow(InventoryItemType item)
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null)
                return;

            bool sown = nursery.Sow(selectedSlot, item, out string message);
            ShowDetail();
            SetStatus(message);

            if (sown)
                Redraw();
        }

        private void OnPrick()
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null)
                return;

            nursery.PrickOut(selectedSlot, out string message);
            SetStatus(message);
        }

        private void OnTransplant()
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null)
                return;

            if (nursery.BeginTransplant(selectedSlot, out string message))
            {
                Close();
                banner.SetActive(true);
                RefreshBanner();
                banner.transform.SetAsLastSibling();
            }
            else
            {
                SetStatus(message);
            }
        }

        private void OnEmpty()
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null)
                return;

            int slot = selectedSlot;
            bool sown = nursery.TryGetMaterial(slot, out PlantingMaterialInfo info);

            if (!sown)
            {
                nursery.EmptyBag(slot, out string emptied);
                SetStatus(emptied);
                return;
            }

            // Throwing away a seedling cannot be undone, so it is asked first -
            // on the shared Yes/No board, which closes this panel while it asks.
            if (FarmConfirmPopup.Instance != null)
            {
                FarmConfirmPopup.Instance.Show(
                    "Throw away the " + BagContentName(nursery.GetStatus(slot), info) + " in bag " + (slot + 1) +
                    "? It cannot be recovered.",
                    () =>
                    {
                        nursery.EmptyBag(slot, out _);
                        Open();
                        SetStatus("Bag " + (slot + 1) + " was emptied.");
                    },
                    null,
                    Open);
            }
            else
            {
                nursery.EmptyBag(slot, out string emptied);
                SetStatus(emptied);
            }
        }

        private void ShowDescription()
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null || descriptionBoard == null ||
                !nursery.TryGetMaterial(selectedSlot, out PlantingMaterialInfo info))
            {
                return;
            }

            descriptionBoard.Show(info.Name, ShopItemDescriptions.For(info.Item));
        }

        // -------------------------------------------------------------- helpers

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message ?? string.Empty;
        }

        private Sprite ItemIcon(InventoryItemType item)
        {
            if (inventoryUI == null)
                inventoryUI = FindFirstObjectByType<InventoryUIBuilder>();
            return inventoryUI != null ? inventoryUI.GetSpriteFor(item) : null;
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = TextLight;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.10f, 0.06f, 0.02f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }
    }
}
