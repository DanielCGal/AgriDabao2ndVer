using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace AgriDabao3D
{
    public class ShopUIBuilder : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject shopPanel;
        private Text statusText;
        private InputField amountInput;
        private RectTransform shopButtonsContent;
        // Static so the daily-objective system can price an item without owning
        // a shop panel. It is the only price list in the game, and it stays the
        // only one: a task that asks the player to buy something has to work out
        // the cost from the same numbers the shop will actually charge.
        //
        // Held in centavos. The seeds use the Davao City government prices, and
        // corn has centavos in it; PesoPrice explains how a price in centavos
        // becomes a charge in whole pesos.
        private static readonly Dictionary<InventoryItemType, int> seedPriceCentavos = new Dictionary<InventoryItemType, int>
        {
            // Seeds - Davao City government prices
            { InventoryItemType.PineappleSeed, PesoPrice.Centavos(10m) },
            { InventoryItemType.BananaSeed, PesoPrice.Centavos(15m) },
            { InventoryItemType.CacaoSeed, PesoPrice.Centavos(25m) },
            { InventoryItemType.CoconutSeed, PesoPrice.Centavos(15m) },
            { InventoryItemType.PomeloSeed, PesoPrice.Centavos(50m) },
            { InventoryItemType.MangoSeed, PesoPrice.Centavos(30m) },
            { InventoryItemType.MangosteenSeed, PesoPrice.Centavos(75m) },
            { InventoryItemType.DurianSeed, PesoPrice.Centavos(60m) },
            { InventoryItemType.CornSeed, PesoPrice.Centavos(388.89m) },
            { InventoryItemType.EggplantSeed, PesoPrice.Centavos(8200m) },
            { InventoryItemType.SquashSeed, PesoPrice.Centavos(3000m) },
            { InventoryItemType.StrawberrySeed, PesoPrice.Centavos(3m) },
            { InventoryItemType.TomatoSeed, PesoPrice.Centavos(9500m) },
            // Equipment / pest tools
            { InventoryItemType.AphidTrap, PesoPrice.Centavos(35m) },
            { InventoryItemType.SprayerPump, PesoPrice.Centavos(250m) },
            { InventoryItemType.InsecticideLiter, PesoPrice.Centavos(90m) },
            { InventoryItemType.DisinfectantLiter, PesoPrice.Centavos(80m) },
            { InventoryItemType.NeemSoapLiter, PesoPrice.Centavos(100m) },
            { InventoryItemType.BtBioInsecticideLiter, PesoPrice.Centavos(130m) },
            { InventoryItemType.CopperFungicideLiter, PesoPrice.Centavos(120m) },
            { InventoryItemType.PheromoneTrap, PesoPrice.Centavos(90m) },
            { InventoryItemType.FruitBag, PesoPrice.Centavos(25m) },
            { InventoryItemType.DrainageKit, PesoPrice.Centavos(150m) },
            { InventoryItemType.TermiteBaitStation, PesoPrice.Centavos(110m) },
            // Climate mitigation and crop maintenance
            { InventoryItemType.MulchBag, PesoPrice.Centavos(25m) },
            { InventoryItemType.OrganicCompostBag, PesoPrice.Centavos(45m) },
            { InventoryItemType.PruningShears, PesoPrice.Centavos(180m) },
            { InventoryItemType.SupportStakeKit, PesoPrice.Centavos(60m) },
            { InventoryItemType.TrellisKit, PesoPrice.Centavos(75m) },
            { InventoryItemType.RaisedBedKit, PesoPrice.Centavos(140m) },
            { InventoryItemType.IrrigationSystemKit, PesoPrice.Centavos(450m) },
            { InventoryItemType.WaterStorageTankKit, PesoPrice.Centavos(600m) },
            { InventoryItemType.ShadeNetKit, PesoPrice.Centavos(260m) },
            { InventoryItemType.WindbreakKit, PesoPrice.Centavos(180m) },
            { InventoryItemType.GreenhouseKit, PesoPrice.Centavos(800m) },
            { InventoryItemType.DrainageCanalKit, PesoPrice.Centavos(320m) }
        };
        private void Start()
        {
            EnsureEventSystem();
            EnsureCanvas();
            BuildShopButton();
            BuildShopPanel();
            SetShopOpen(false);
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
        private void BuildShopButton()
        {
            // Part of the shared round-icon row along the HUD's top-left edge.
            HudIconButton.Create(
                canvas.transform,
                "ShopButton",
                UIThemeSprites.Instance?.shopButton,
                HudIconButton.SlotShop,
                "SHOP",
                ToggleShop,
                out _,
                out _);
        }
        private void BuildShopPanel()
        {
            UIThemeSprites theme = UIThemeSprites.Instance;
            Sprite board = theme?.shopBoard;

            shopPanel = new GameObject("SeedEquipmentShopPanel", typeof(RectTransform), typeof(Image));
            HudRegistry.RegisterPiece(HudPiece.ShopPanel, shopPanel);
            shopPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = shopPanel.GetComponent<RectTransform>();

            if (board != null)
            {
                // Centred board layout: shelves on the left, preview on the right.
                panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
                panelRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                panelRect.anchorMin = new Vector2(1f, 1f);
                panelRect.anchorMax = new Vector2(1f, 1f);
                panelRect.pivot = new Vector2(1f, 1f);
                panelRect.sizeDelta = new Vector2(620f, 760f);
                panelRect.anchoredPosition = new Vector2(-30f, -100f);
            }

            Image panelBg = shopPanel.GetComponent<Image>();
            if (board != null)
            {
                panelBg.sprite = board;
                panelBg.type = Image.Type.Sliced;
                panelBg.color = Color.white;
            }
            else
            {
                panelBg.color = new Color(0f, 0f, 0f, 0.72f);
            }
            panelBg.raycastTarget = true;

            if (theme?.shopLabel != null)
            {
                GameObject signGo = new GameObject("ShopSign", typeof(RectTransform), typeof(Image));
                signGo.transform.SetParent(shopPanel.transform, false);
                RectTransform signRect = signGo.GetComponent<RectTransform>();
                signRect.anchorMin = signRect.anchorMax = new Vector2(0.5f, 1f);
                signRect.pivot = new Vector2(0.5f, 0.5f);
                // ShopLabel.png is 1040x300 (3.47:1), so preserveAspect keeps it
                // undistorted and this width sets the sign's overall size. Narrowed
                // from 520 so the sign stops dominating the top of the board.
                signRect.sizeDelta = new Vector2(440f, 150f);
                // Lowered from +6 so the sign overlaps the board's top edge and
                // reads as mounted on it rather than floating above it.
                signRect.anchoredPosition = new Vector2(0f, -16f);
                Image signImage = signGo.GetComponent<Image>();
                signImage.sprite = theme.shopLabel;
                signImage.preserveAspect = true;
                signImage.raycastTarget = false;
            }
            else
            {
                CreateLabel(shopPanel.transform, "SEED/EQUIPMENT SHOP", 34,
                    new Vector2(0f, -38f), 560f, 55f);
            }

            if (board != null)
            {
                BuildShelfArea(theme);
                BuildPreviewArea(theme);
                BuildCheckDescriptionButton(theme);
                descriptionBoard = DescriptionBoard.Create(shopPanel.transform, theme);
                // Starts blank rather than carrying a standing "Select an item to
                // buy." prompt. The row still exists so buy results and errors
                // ("Not enough money", "Inventory is full") have somewhere to show.
                // Back on the plank at the position it held before the Check
                // Description button existed; the shelf and that button were lifted
                // to make room rather than pushing this line off the board.
                statusText = CreateLabel(shopPanel.transform, "", 20,
                    new Vector2(-230f, -640f + ContentLift), 520f, 44f);
                FitStatusText(statusText);
            }
            else
            {
                CreateLabel(shopPanel.transform, "Amount to Buy", 22,
                    new Vector2(-70f, -105f), 260f, 40f);
                amountInput = CreateInputField(shopPanel.transform, "1",
                    new Vector2(180f, -105f), 130f, 48f);
                BuildScrollableButtonArea();
                statusText = CreateLabel(shopPanel.transform, "Select an item to buy.", 22,
                    new Vector2(0f, -690f), 560f, 80f);
                FitStatusText(statusText);
            }
        }

        // ---------------- Themed shelf layout ----------------

        private const int ShelfColumns = 4;
        private const int ShelfRows = 4;
        private int ShelfPageSize => ShelfColumns * ShelfRows;

        private RectTransform shelfGrid;
        private int shopPage;
        private InventoryItemType selectedItem = InventoryItemType.None;
        private Image previewIcon;
        private Text previewNameText;
        private Text previewPriceText;
        private GameObject prevPageButton;
        private GameObject nextPageButton;
        private InventoryUIBuilder cachedInventoryUI;

        // ---------------- Item description board ----------------

        private DescriptionBoard descriptionBoard;

        private const float DescriptionBoardArtWidth = 1760f;
        private const float DescriptionLogEndsAtPx = 280f;
        private const float DescriptionLogResumesAtPx = 1458f;

        /// <summary>Centre of the 4x4 shelf, in the panel's own coordinates.</summary>
        private const float ShelfCentreX = 160f + ShelfShiftX + 250f - PanelHalfWidth;
        private const float CheckButtonWidth = 320f;
        private const float CheckButtonHeight = 58f;

        /// <summary>Every item the shop sells, in the order they appear on the shelf.</summary>
        private static readonly InventoryItemType[] ShopStock =
        {
            InventoryItemType.PineappleSeed, InventoryItemType.BananaSeed,
            InventoryItemType.CacaoSeed, InventoryItemType.CoconutSeed,
            InventoryItemType.PomeloSeed, InventoryItemType.MangoSeed,
            InventoryItemType.MangosteenSeed, InventoryItemType.DurianSeed,
            InventoryItemType.CornSeed, InventoryItemType.EggplantSeed,
            InventoryItemType.SquashSeed, InventoryItemType.StrawberrySeed,
            InventoryItemType.TomatoSeed, InventoryItemType.AphidTrap,
            InventoryItemType.SprayerPump, InventoryItemType.InsecticideLiter,
            InventoryItemType.DisinfectantLiter, InventoryItemType.NeemSoapLiter,
            InventoryItemType.BtBioInsecticideLiter, InventoryItemType.CopperFungicideLiter,
            InventoryItemType.PheromoneTrap, InventoryItemType.FruitBag,
            InventoryItemType.DrainageKit, InventoryItemType.TermiteBaitStation,
            InventoryItemType.MulchBag, InventoryItemType.OrganicCompostBag,
            InventoryItemType.PruningShears, InventoryItemType.SupportStakeKit,
            InventoryItemType.TrellisKit, InventoryItemType.RaisedBedKit,
            InventoryItemType.IrrigationSystemKit, InventoryItemType.WaterStorageTankKit,
            InventoryItemType.ShadeNetKit, InventoryItemType.WindbreakKit,
            InventoryItemType.GreenhouseKit, InventoryItemType.DrainageCanalKit
        };

        private void BuildShelfArea(UIThemeSprites theme)
        {
            GameObject shelf = new GameObject("ShopShelf", typeof(RectTransform), typeof(Image));
            shelf.transform.SetParent(shopPanel.transform, false);

            RectTransform rect = shelf.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(500f, 440f);
            rect.anchoredPosition = new Vector2(160f + ShelfShiftX, -135f + ContentLift);

            Image image = shelf.GetComponent<Image>();
            if (theme?.shopShelfGrid != null)
            {
                image.sprite = theme.shopShelfGrid;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0f, 0f, 0f, 0.3f);
            }

            GameObject cells = new GameObject("Cells", typeof(RectTransform));
            cells.transform.SetParent(shelf.transform, false);
            shelfGrid = cells.GetComponent<RectTransform>();
            shelfGrid.anchorMin = shelfGrid.anchorMax = new Vector2(0.5f, 0.5f);
            shelfGrid.pivot = new Vector2(0.5f, 0.5f);
            // Rows are spaced by sizeDelta.y / 4, so this has to match the painted
            // compartments in ShopShelve.png. At the old 380 the spacing came out
            // ~4px short per row, and the error accumulated downward until the
            // bottom row straddled the shelf plank instead of sitting in the cell.
            // 398 with a 3px drop matches the art across all four rows.
            shelfGrid.sizeDelta = new Vector2(430f, 398f);
            shelfGrid.anchoredPosition = new Vector2(0f, -3f);

            // Paging arrows sit either side of the shelf; hidden when everything fits.
            prevPageButton = CreateIconButton(theme?.shopPrevPageButton, "<<",
                new Vector2(0f, 1f), new Vector2(120f, 46f),
                new Vector2(105f + ShelfShiftX, -345f + ContentLift),
                () => ChangeShopPage(-1));
            nextPageButton = CreateIconButton(theme?.shopNextPageButton, ">>",
                new Vector2(0f, 1f), new Vector2(120f, 46f),
                new Vector2(620f + ShelfShiftX, -345f + ContentLift),
                () => ChangeShopPage(1));
        }

        // ---- Board layout tuning ----
        // ShopBoard.png is natively 1120x700; the extra width here stretches it.
        // The shelf stays a fixed distance from the left edge and the preview a
        // fixed distance from the right, so widening the board opens up the gap
        // between them rather than moving either one.
        private const float PanelWidth = 1300f;
        private const float PanelHeight = 700f;
        private const float PanelHalfWidth = PanelWidth * 0.5f;

        /// <summary>
        /// Raises every control inside the board by this many pixels. The contents
        /// were laid out bottom-heavy, leaving a band of bare wood along the top;
        /// lifting them all by one shared value keeps their spacing intact while
        /// centring the group. Increase to lift further, decrease to drop back.
        /// </summary>
        private const float ContentLift = 50f;

        /// <summary>
        /// Shifts the shelf and its two paging arrows right as one group. At the
        /// shelf's original X the prev arrow sat partly on the board's left rolled
        /// log; this moves the group onto the flat plank area while preserving the
        /// arrows' alignment against the shelf edges.
        /// </summary>
        private const float ShelfShiftX = 80f;

        // The preview frame and the Amount / Buy controls under it are all derived
        // from these so the column stays aligned if the frame is moved or resized.
        private const float PreviewWidth = 300f;
        private const float PreviewHeight = 300f;
        private const float PreviewRightInset = 170f;
        private const float BuyWidth = 220f;

        /// <summary>Centre of the preview frame, relative to the panel's centre.</summary>
        private const float PreviewCentreX =
            PanelHalfWidth - PreviewRightInset - PreviewWidth * 0.5f;

        private void BuildPreviewArea(UIThemeSprites theme)
        {
            GameObject frame = new GameObject("ShopPreview", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(shopPanel.transform, false);

            RectTransform rect = frame.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(PreviewWidth, PreviewHeight);
            rect.anchoredPosition = new Vector2(-PreviewRightInset, -150f + ContentLift);

            Image frameImage = frame.GetComponent<Image>();
            if (theme?.shopPreviewFrame != null)
            {
                frameImage.sprite = theme.shopPreviewFrame;
                // ShopItemFrame.png carries no 9-slice border, so preserveAspect
                // would lock it to its native 0.87:1 and ignore the wider box.
                // Turning it off is what actually widens the frame; give the sprite
                // a border in the Sprite Editor if the stretch ever reads too far.
                frameImage.preserveAspect = false;
                frameImage.color = Color.white;
            }
            else
            {
                frameImage.color = new Color(0f, 0f, 0f, 0.35f);
            }

            // Name plank across the top of the frame art.
            previewNameText = CreateLabel(frame.transform, "", 19, Vector2.zero, 270f, 44f);
            RectTransform nameRect = previewNameText.rectTransform;
            nameRect.anchorMin = nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0f, -14f);
            // Long names such as "Water Storage Tank Kit" overran the plank at a
            // fixed size; best-fit shrinks only those that need it.
            previewNameText.resizeTextForBestFit = true;
            previewNameText.resizeTextMinSize = 12;
            previewNameText.resizeTextMaxSize = 19;
            ApplyPlankTextStyle(previewNameText);

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(frame.transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(120f, 120f);
            iconRect.anchoredPosition = new Vector2(0f, 6f);
            previewIcon = iconGo.GetComponent<Image>();
            previewIcon.preserveAspect = true;
            previewIcon.raycastTarget = false;
            previewIcon.enabled = false;

            // Price plank across the bottom.
            previewPriceText = CreateLabel(frame.transform, "", 20, Vector2.zero, 230f, 44f);
            RectTransform priceRect = previewPriceText.rectTransform;
            priceRect.anchorMin = priceRect.anchorMax = new Vector2(0.5f, 0f);
            priceRect.pivot = new Vector2(0.5f, 0f);
            // Raised off the frame's bottom edge so the price sits on the plank art
            // rather than below it.
            priceRect.anchoredPosition = new Vector2(0f, 46f);
            ApplyPlankTextStyle(previewPriceText);

            // Amount and Buy form one column directly beneath the preview frame.
            // These labels anchor to the panel's top-centre, so X is the offset from
            // centre and Y is the drop from the top edge.
            CreateLabel(shopPanel.transform, "Amount:", 22,
                new Vector2(PreviewCentreX, -462f + ContentLift), 200f, 40f);
            amountInput = CreateInputField(shopPanel.transform, "1",
                new Vector2(PreviewCentreX, -508f + ContentLift), 240f, 48f);

            // Digits only, and at most two of them.
            //
            // The field took any text before. Letters were not dangerous -
            // ParseAmount falls back to 1 - but the fallback was silent, so a
            // player who typed something odd got one seed and no explanation.
            // Two characters also makes the 1-99 ceiling visible while typing,
            // instead of quietly clamping a 500 down to 99 after the fact. On a
            // phone IntegerNumber opens the number pad rather than the full
            // keyboard, which is the same reason the sign-up date boxes use it.
            amountInput.contentType = InputField.ContentType.IntegerNumber;
            amountInput.characterLimit = 2;

            // Buy anchors to the panel's top-right, so its X is converted from the
            // shared centre line to keep it under the same column.
            CreateIconButton(theme?.shopBuyButton, "BUY",
                new Vector2(1f, 1f), new Vector2(BuyWidth, 62f),
                new Vector2(PreviewCentreX + BuyWidth * 0.5f - PanelHalfWidth,
                    -595f + ContentLift),
                OnBuySelectedPressed);
        }

        /// <summary>
        /// Styles a label that sits on the preview frame's wooden planks. The
        /// previous dark brown was near-invisible against that art, so this uses a
        /// light fill with a hard outline, which stays readable over both the plank
        /// and the item icon behind it.
        /// </summary>
        private static void ApplyPlankTextStyle(Text label)
        {
            if (label == null)
                return;

            label.color = new Color(1f, 0.97f, 0.88f, 1f);
            label.fontStyle = FontStyle.Bold;

            Outline outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.10f, 0.06f, 0.02f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
        }

        private GameObject CreateIconButton(Sprite art, string fallbackLabel, Vector2 anchor,
            Vector2 size, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject("Button_" + fallbackLabel,
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(shopPanel.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
                return go;
            }

            image.color = new Color(0.20f, 0.55f, 0.20f, 0.95f);
            Text text = CreateLabel(go.transform, fallbackLabel, 20, Vector2.zero, size.x, size.y);
            Stretch(text.rectTransform);
            return go;
        }

        /// <summary>Draws one page of stock onto the 4x4 shelf.</summary>
        private void RefreshShelf()
        {
            if (shelfGrid == null)
                return;

            for (int i = shelfGrid.childCount - 1; i >= 0; i--)
                Destroy(shelfGrid.GetChild(i).gameObject);

            int pageCount = Mathf.CeilToInt(ShopStock.Length / (float)ShelfPageSize);
            shopPage = Mathf.Clamp(shopPage, 0, Mathf.Max(0, pageCount - 1));

            // Arrows only appear when the stock actually needs more than one page.
            bool paged = pageCount > 1;
            if (prevPageButton != null) prevPageButton.SetActive(paged);
            if (nextPageButton != null) nextPageButton.SetActive(paged);

            Vector2 size = shelfGrid.sizeDelta;
            float cellW = size.x / ShelfColumns;
            float cellH = size.y / ShelfRows;
            float slot = Mathf.Min(cellW, cellH) * 0.78f;

            int start = shopPage * ShelfPageSize;
            for (int i = 0; i < ShelfPageSize; i++)
            {
                int index = start + i;
                if (index >= ShopStock.Length)
                    break;

                InventoryItemType item = ShopStock[index];
                int row = i / ShelfColumns;
                int col = i % ShelfColumns;
                Vector2 pos = new Vector2(
                    -size.x * 0.5f + cellW * (col + 0.5f),
                    size.y * 0.5f - cellH * (row + 0.5f));

                CreateShelfCell(item, pos, slot);
            }
        }

        private void CreateShelfCell(InventoryItemType item, Vector2 pos, float size)
        {
            GameObject cell = new GameObject("Stock_" + item,
                typeof(RectTransform), typeof(Image), typeof(Button));
            cell.transform.SetParent(shelfGrid, false);

            RectTransform rect = cell.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = pos;

            Sprite icon = ItemIcon(item);
            Image image = cell.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            // Selected stock is tinted; the shelf art already draws the cell itself.
            // Seeds this farm's district does not grow stay on the shelf, greyed out,
            // so they can still be picked and read about - they just cannot be bought.
            bool offered = DistrictCropPools.IsAvailableToPlayer(item);
            image.color = icon == null
                ? new Color(1f, 1f, 1f, 0.15f)
                : !offered
                    ? (item == selectedItem ? UnavailableSelectedTint : UnavailableTint)
                    : (item == selectedItem ? new Color(0.75f, 1f, 0.75f, 1f) : Color.white);

            Button button = cell.GetComponent<Button>();
            button.targetGraphic = image;
            InventoryItemType captured = item;
            button.onClick.AddListener(() => SelectShopItem(captured));
        }

        private void SelectShopItem(InventoryItemType item)
        {
            selectedItem = item;

            if (previewIcon != null)
            {
                previewIcon.sprite = ItemIcon(item);
                previewIcon.enabled = previewIcon.sprite != null;
                previewIcon.color = DistrictCropPools.IsAvailableToPlayer(item)
                    ? Color.white
                    : UnavailableTint;
            }
            if (previewNameText != null)
                previewNameText.text = FriendlyShopName(item);
            if (previewPriceText != null)
                previewPriceText.text = PesoPrice.Label(GetSeedPriceCentavos(item));

            // Opening is the Check Description button's job. Picking a different
            // item while the board is already open refreshes it in place rather
            // than making the player close and reopen it.
            if (descriptionBoard != null && descriptionBoard.IsOpen)
                ShowDescriptionFor(item);

            // No "Selected ..." line: the preview frame already names the item and
            // shows its price, so the status row is left for buy results and errors.
            RefreshShelf();
        }

        /// <summary>The button under the shelf that opens the description board.</summary>
        private void BuildCheckDescriptionButton(UIThemeSprites theme)
        {
            Button button = DescriptionBoard.CreateOpenButton(
                shopPanel.transform, theme, "CHECK DESCRIPTION",
                new Vector2(CheckButtonWidth, CheckButtonHeight),
                OnCheckDescriptionPressed);

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(ShelfCentreX, -579f + ContentLift);
        }

        private void OnCheckDescriptionPressed()
        {
            if (selectedItem == InventoryItemType.None)
            {
                SetStatus("Select an item from the shelf first.");
                return;
            }

            ShowDescriptionFor(selectedItem);
        }

        /// <summary>Fills the shared board for one item and opens it.</summary>
        private void ShowDescriptionFor(InventoryItemType item)
        {
            if (descriptionBoard == null)
                return;

            string body = ShopItemDescriptions.For(item);
            if (string.IsNullOrEmpty(body))
            {
                descriptionBoard.Hide();
                return;
            }

            descriptionBoard.Show(FriendlyShopName(item), body);
        }

        private void HideDescription()
        {
            if (descriptionBoard != null)
                descriptionBoard.Hide();
        }


        private void ChangeShopPage(int delta)
        {
            shopPage += delta;
            int pageCount = Mathf.CeilToInt(ShopStock.Length / (float)ShelfPageSize);
            if (shopPage < 0) shopPage = pageCount - 1;
            if (shopPage >= pageCount) shopPage = 0;
            RefreshShelf();
        }

        private void OnBuySelectedPressed()
        {
            if (selectedItem == InventoryItemType.None)
            {
                SetStatus("Select an item from the shelf first.");
                return;
            }
            TryBuySeed(selectedItem, FriendlyShopName(selectedItem));
        }

        /// <summary>Reuses the backpack's icon table so the shop shows the same art.</summary>
        private Sprite ItemIcon(InventoryItemType item)
        {
            if (cachedInventoryUI == null)
                cachedInventoryUI = Object.FindFirstObjectByType<InventoryUIBuilder>();
            return cachedInventoryUI != null ? cachedInventoryUI.GetSpriteFor(item) : null;
        }

        private static string FriendlyShopName(InventoryItemType item)
        {
            return SocialMarketplaceCatalog.FriendlyName(item);
        }
        private void BuildScrollableButtonArea()
        {
            GameObject scrollGo = new GameObject(
                "ShopScrollView",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect)
            );
            scrollGo.transform.SetParent(shopPanel.transform, false);
            RectTransform scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(45f, 135f);
            scrollRectTransform.offsetMax = new Vector2(-45f, -175f);
            Image scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.color = new Color(1f, 1f, 1f, 0.03f);
            scrollBg.raycastTarget = true;
            ScrollRect scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 45f;
            GameObject viewportGo = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask)
            );
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            Stretch(viewportRect);
            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;
            Mask mask = viewportGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;
            GameObject contentGo = new GameObject("ShopContent", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            shopButtonsContent = contentGo.GetComponent<RectTransform>();
            shopButtonsContent.anchorMin = new Vector2(0.5f, 1f);
            shopButtonsContent.anchorMax = new Vector2(0.5f, 1f);
            shopButtonsContent.pivot = new Vector2(0.5f, 1f);
            shopButtonsContent.anchoredPosition = Vector2.zero;
            shopButtonsContent.sizeDelta = new Vector2(520f, 790f);
            scrollRect.viewport = viewportRect;
            scrollRect.content = shopButtonsContent;
            float y = -10f;
            float step = 62f;
            CreateSeedButton(InventoryItemType.PineappleSeed, "Pineapple Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.BananaSeed, "Banana Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.CacaoSeed, "Cacao Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.CoconutSeed, "Coconut Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.PomeloSeed, "Pomelo Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.MangoSeed, "Mango Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.MangosteenSeed, "Mangosteen Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.DurianSeed, "Durian Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.CornSeed, "Corn Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.EggplantSeed, "Eggplant Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.SquashSeed, "Squash Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.StrawberrySeed, "Strawberry Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.TomatoSeed, "Tomato Seed", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.AphidTrap, "Aphid Trap", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.SprayerPump, "Sprayer Pump", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.InsecticideLiter, "Insecticide Liter", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.DisinfectantLiter, "Disinfectant Liter", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.NeemSoapLiter, "Neem Soap Liter", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.BtBioInsecticideLiter, "Bt Bio-Insecticide Liter", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.CopperFungicideLiter, "Copper Fungicide Liter", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.PheromoneTrap, "Pheromone Trap", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.FruitBag, "Fruit Bag", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.DrainageKit, "Drainage Kit", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.TermiteBaitStation, "Termite Bait Station", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.MulchBag, "Mulch Bag", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.OrganicCompostBag, "Organic Compost Bag", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.PruningShears, "Pruning Shears", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.SupportStakeKit, "Support Stake Kit", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.TrellisKit, "Trellis Kit", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.RaisedBedKit, "Raised Bed Kit", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.IrrigationSystemKit, "Irrigation System Kit", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.WaterStorageTankKit, "Water Storage Tank Kit", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.ShadeNetKit, "Shade Net Kit", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.WindbreakKit, "Windbreak Kit", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.GreenhouseKit, "Greenhouse Kit", new Vector2(0f, y));
            y -= step;
            CreateSeedButton(InventoryItemType.DrainageCanalKit, "Drainage Canal Kit", new Vector2(0f, y));
            shopButtonsContent.sizeDelta = new Vector2(520f, Mathf.Abs(y) + 80f);
            scrollRect.verticalNormalizedPosition = 1f;
        }
        private void CreateSeedButton(InventoryItemType seedType, string displayName, Vector2 pos)
        {
            string label = $"{displayName} - {PesoPrice.Label(GetSeedPriceCentavos(seedType))}";
            CreateSimpleButton(
                shopButtonsContent,
                label,
                pos,
                500f,
                52f,
                () => TryBuySeed(seedType, displayName),
                new Color(0.20f, 0.55f, 0.20f, 0.95f)
            );
        }
        private void TryBuySeed(InventoryItemType seedType, string displayName)
        {
            if (PlayerInventory.Instance == null)
            {
                SetStatus("Inventory system not found.");
                return;
            }
            if (!DistrictCropPools.IsAvailableToPlayer(seedType))
            {
                string here = SelectedAreaState.SelectedDistrictName;
                SetStatus(string.IsNullOrWhiteSpace(here)
                    ? $"{displayName} is not sold on your farm. Check its description."
                    : $"{displayName} is not sold in {here.Trim()}. Check its description.");
                return;
            }
            int amount = ParseAmount();
            if (amount <= 0)
            {
                SetStatus("Enter a valid amount.");
                return;
            }
            if (PlayerInventory.IsTool(seedType))
            {
                amount = 1;
                if (PlayerInventory.Instance.HasItem(seedType, 1))
                {
                    SetStatus($"You already own {displayName}.");
                    return;
                }
            }
            // Priced in centavos, paid in whole pesos: it is the total that gets
            // rounded, never the price of each one.
            int totalCost = PesoPrice.TotalPesos(GetSeedPriceCentavos(seedType), amount);
            if (PlayerInventory.Instance.money < totalCost)
            {
                SetStatus($"Not enough money. Need P{totalCost} for {amount} {displayName}(s).");
                return;
            }
            if (!PlayerInventory.Instance.CanAddItem(seedType, amount))
            {
                SetStatus("Inventory is full. Free up space first.");
                return;
            }
            bool paid = PlayerInventory.Instance.SpendMoney(totalCost);
            if (!paid)
            {
                SetStatus("Not enough money.");
                return;
            }
            bool added = PlayerInventory.Instance.AddItem(seedType, amount);
            if (!added)
            {
                PlayerInventory.Instance.AddMoney(totalCost);
                SetStatus("Could not add item. Money refunded.");
                return;
            }
            GameAudioManager.Instance.PlayReward();
            SetStatus($"Bought {amount} {displayName}(s) for P{totalCost}.");
        }
        private int GetSeedPriceCentavos(InventoryItemType seedType)
        {
            // P999 rather than free for anything unlisted. A missing entry must
            // never make an item free; StockedPriceCentavos below is the query that
            // is allowed to answer "not sold", and only because nothing buys with it.
            int centavos = StockedPriceCentavos(seedType);
            return centavos > 0 ? centavos : PesoPrice.Centavos(999m);
        }

        /// <summary>
        /// What the shop charges for one of an item, in centavos, or 0 when it does
        /// not stock it at all. Zero means the item cannot be bought at any price,
        /// which is a different answer from "it is expensive" and has to stay
        /// distinguishable.
        /// </summary>
        public static int StockedPriceCentavos(InventoryItemType item)
        {
            return seedPriceCentavos.TryGetValue(item, out int centavos) ? centavos : 0;
        }

        /// <summary>
        /// What buying <paramref name="quantity"/> of an item costs, in whole pesos,
        /// rounded exactly as the shop rounds it when it charges.
        ///
        /// Read by the daily objectives to decide whether a task is affordable
        /// before offering it, so a task is never judged affordable by a peso the
        /// shop then asks for. Check StockedPriceCentavos first when "not sold"
        /// matters - this returns 0 for that too.
        /// </summary>
        public static int CostFor(InventoryItemType item, int quantity)
        {
            return PesoPrice.TotalPesos(StockedPriceCentavos(item), quantity);
        }

        /// <summary>
        /// Whether the shop will sell the item to this farm: it is stocked, and if it
        /// is a seed, the farm's district grows it.
        /// </summary>
        public static bool SellsToPlayer(InventoryItemType item)
        {
            return StockedPriceCentavos(item) > 0 && DistrictCropPools.IsAvailableToPlayer(item);
        }

        /// <summary>A dim grey that still lets the seed's art read, for stock this farm cannot buy.</summary>
        private static readonly Color UnavailableTint = new Color(0.42f, 0.42f, 0.42f, 0.8f);
        private static readonly Color UnavailableSelectedTint = new Color(0.62f, 0.66f, 0.62f, 0.9f);
        private int ParseAmount()
        {
            if (amountInput == null || string.IsNullOrWhiteSpace(amountInput.text))
                return 1;
            if (!int.TryParse(amountInput.text, out int value))
                return 1;
            return Mathf.Clamp(value, 1, 99);
        }
        private void ToggleShop()
        {
            SetShopOpen(shopPanel == null || !shopPanel.activeSelf);
        }
        private void SetShopOpen(bool open)
        {
            if (shopPanel != null)
            {
                if (open)
                    HudRegistry.CloseOtherPanels(HudPiece.ShopPanel);

                shopPanel.SetActive(open);

                // Siblings on a canvas draw in hierarchy order, and the weather/time
                // panel is built by a different script, so creation order alone
                // decides who covers whom. Re-parenting to last on open keeps the
                // shop above the HUD regardless of which script ran first.
                if (open)
                    shopPanel.transform.SetAsLastSibling();
            }

            // The description board is deliberately not restored with the rest of
            // the panel: reopening the shop should show the shelf, not whatever
            // was last read. It comes back on the next selection.
            if (open)
                HideDescription();

            // Redrawn on open so the shelf reflects the current page and selection.
            if (open && shelfGrid != null)
                RefreshShelf();
        }
        /// <summary>
        /// Lets the status row shrink a long message to fit instead of cutting it off.
        /// On the themed board the row is one line tall, and a result such as a
        /// refused seed or a large purchase runs past one line - the second line was
        /// simply never drawn.
        /// </summary>
        private static void FitStatusText(Text text)
        {
            if (text == null)
                return;

            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = text.fontSize;
            text.resizeTextMinSize = 12;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
            Debug.Log("[Shop] " + message);
        }
        private Text CreateLabel(
            Transform parent,
            string textValue,
            int fontSize,
            Vector2 anchoredPosition,
            float width,
            float height)
        {
            GameObject go = new GameObject(textValue, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = anchoredPosition;
            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = textValue;
            text.raycastTarget = false;
            return text;
        }
        private InputField CreateInputField(
            Transform parent,
            string defaultValue,
            Vector2 anchoredPosition,
            float width,
            float height)
        {
            GameObject go = new GameObject(
                "AmountInput",
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
            rect.anchoredPosition = anchoredPosition;
            Image bg = go.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.95f);
            InputField input = go.GetComponent<InputField>();
            GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(go.transform, false);
            StretchWithPadding(placeholderGo.GetComponent<RectTransform>(), 10f, 6f);
            Text placeholder = placeholderGo.GetComponent<Text>();
            placeholder.font = GameFonts.Primary;
            placeholder.fontSize = 22;
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.color = new Color(0f, 0f, 0f, 0.35f);
            placeholder.text = defaultValue;
            // A 48-tall field leaves 36 for its text after padding. Playpen Sans at
            // font 22 needs about 32 of that, so raising the text size ran out of
            // room - and Unity's default Truncate drops the line rather than
            // clipping it, which made the field look like it was refusing input
            // when it was actually accepting it and drawing nothing.
            //
            // Only the vertical mode is touched: InputField drives horizontal
            // overflow itself to scroll a single line, so overriding that would
            // break long values.
            placeholder.verticalOverflow = VerticalWrapMode.Overflow;
            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            StretchWithPadding(textGo.GetComponent<RectTransform>(), 10f, 6f);
            Text text = textGo.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.black;
            text.text = defaultValue;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = defaultValue;
            return input;
        }
        private void CreateSimpleButton(
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            float width,
            float height,
            UnityEngine.Events.UnityAction onClick,
            Color color)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = anchoredPosition;
            Image bg = go.GetComponent<Image>();
            bg.color = color;
            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);
            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            Stretch(textRect);
            Text text = textGo.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        private static void StretchWithPadding(RectTransform rect, float padX, float padY)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padX, padY);
            rect.offsetMax = new Vector2(-padX, -padY);
        }
    }
}
