using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public partial class SocialMarketplaceUIBuilder
    {
        private const int OfferSlotCount = 4;   // the 2x2 grid
        private const int InventoryColumns = 6;
        private const int InventoryRows = 6;

        private Button tradeSetOfferButton;
        private Button tradeCancelButton;
        private Button tradeConfirmAcceptButton;
        private bool tradeClosingAfterCompletion;
        private Text tradeMyNameText;
        private Text tradeOtherNameText;
        private Text tradeMyMoneyText;
        private Text tradeOtherMoneyText;
        private Image tradeMyStateIcon;
        private Image tradeOtherStateIcon;
        private RectTransform tradeInventoryContent;

        private readonly TradeDropSlot[] mySlots = new TradeDropSlot[OfferSlotCount];
        private readonly Image[] mySlotIcons = new Image[OfferSlotCount];
        private readonly Text[] mySlotCounts = new Text[OfferSlotCount];

        private readonly Image[] otherSlotIcons = new Image[OfferSlotCount];
        private readonly Text[] otherSlotCounts = new Text[OfferSlotCount];

        // The trade-request popup shown to the receiving player.
        private GameObject tradeRequestPopup;
        private Text tradeRequestText;
        private string tradeRequestShownForId;

        private bool tradeSessionPrepared;
        private Coroutine offerPushLoop;
        private bool offerDirty;
        private bool preparingTradeSession;
        private string tradePrepareError;

        // The popup for offering a seed the other player's district does not grow.
        private GameObject tradeSeedNoticePanel;

        private const string SeedUnavailableNotice =
            "The seed that you are offering isn't available to the other person you're trading with, please select another seed that is available to the other player.";

        /// <summary>
        /// Part of the server's refusal wording, which is the notice word for word.
        /// Matching on it lets a refusal from the server open the same popup.
        /// </summary>
        private const string SeedUnavailableMarker = "available to the other player";

        // ---------------------------------------------------------------- build

        /// <summary>
        /// Board inset for the trade panel. It uses socialBoard, whose logs sit
        /// clear of the plank, so the declared 130px border plus a 20px margin is
        /// enough - unlike ChatBoard, which needs the deeper ChatBoardLogInset.
        /// </summary>
        private const float TradeBoardInset = 150f;

        private void BuildTradePanel()
        {
            // Taller and wider than before: the old 1450x820 could not fit the
            // inventory grid, the money row and the buttons without them stacking
            // on top of each other, which is why everything was crushed downward.
            tradePanel = CreatePanel("PlayerTradePanel", new Vector2(1600f, 980f),
                boardOverride: Theme?.socialBoard);
            CreatePanelTitle(tradePanel.transform, "TRADING", Theme?.tradingLabel,
                520f, 150f, -18f);

            tradeTitleText = CreateText(tradePanel.transform, "Title", 1, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            tradeTitleText.gameObject.SetActive(false);

            BuildTradeInventoryGrid();
            // Both offer areas start just under the board's top log cap so the
            // column fills the space that used to sit empty above them.
            BuildOfferArea(isMine: true, anchoredY: -70f);
            BuildOfferArea(isMine: false, anchoredY: -452f);
            BuildMoneyRow();

            tradeSetOfferButton = CreateButton(tradePanel.transform, "Set Trade",
                new Vector2(1f, 0f), new Vector2(260f, 70f), new Vector2(-TradeBoardInset, 70f),
                OnSetTradePressed, out _, art: Theme?.setTradeButton);

            tradeCancelButton = CreateButton(tradePanel.transform, "Cancel Trade",
                new Vector2(0f, 0f), new Vector2(220f, 60f), new Vector2(TradeBoardInset, 70f),
                OnCancelTradePressed, out _,
                new Color(0.50f, 0.25f, 0.12f, 0.98f),
                art: Theme?.cancelTradeButton);

            // Raised into the gap between Cancel and Set Trade instead of sitting
            // under both, where the hotbar clipped it.
            tradeStatusText = CreateText(tradePanel.transform, "Status", 18, TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(390f, 78f), new Vector2(-430f, 118f));

            // Unused by the new layout but kept so older code paths still compile.
            tradeSummaryText = tradeStatusText;
            tradePendingOfferText = tradeStatusText;

            tradePanel.SetActive(false);
        }

        private void BuildTradeInventoryGrid()
        {
            GameObject grid = new GameObject("TradeInventory", typeof(RectTransform), typeof(Image));
            grid.transform.SetParent(tradePanel.transform, false);

            RectTransform rect = grid.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(700f, 560f);
            // Raised to sit directly under the top log cap; it used to start 180
            // down, which pushed its lower half over the Add Money plank.
            rect.anchoredPosition = new Vector2(TradeBoardInset, -70f);

            Image image = grid.GetComponent<Image>();
            Sprite art = Theme?.tradeInventoryGrid;
            if (art != null)
            {
                image.sprite = art;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0f, 0f, 0f, 0.35f);
            }

            GameObject content = new GameObject("Cells", typeof(RectTransform));
            content.transform.SetParent(grid.transform, false);
            tradeInventoryContent = content.GetComponent<RectTransform>();
            tradeInventoryContent.anchorMin = tradeInventoryContent.anchorMax = new Vector2(0.5f, 0.5f);
            tradeInventoryContent.pivot = new Vector2(0.5f, 0.5f);
            // Measured against the painted compartments in TradeInventoryUI.png:
            // six columns at ~96.7 and six rows at ~73.3, i.e. the region inside the
            // sprite's 60px border, sitting ~9px above the rect centre. The old
            // 620x490 centred gave the wrong pitch, so cells drifted off their boxes.
            tradeInventoryContent.sizeDelta = new Vector2(580f, 440f);
            tradeInventoryContent.anchoredPosition = new Vector2(0f, 9.5f);
        }

        /// <summary>
        /// One player's offer area: name plank, 2x2 item grid, money plank and the
        /// log/lock state icon. The bottom copy mirrors the other player and is
        /// display-only.
        /// </summary>
        private void BuildOfferArea(bool isMine, float anchoredY)
        {
            string prefix = isMine ? "My" : "Other";

            GameObject namePlank = new GameObject(prefix + "NamePlank", typeof(RectTransform), typeof(Image));
            namePlank.transform.SetParent(tradePanel.transform, false);
            RectTransform nameRect = namePlank.GetComponent<RectTransform>();
            nameRect.anchorMin = nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(1f, 1f);
            nameRect.sizeDelta = new Vector2(260f, 60f);
            nameRect.anchoredPosition = new Vector2(-470f, anchoredY);
            ApplySprite(namePlank.GetComponent<Image>(), Theme?.tradeNamePlank,
                new Color(0.35f, 0.22f, 0.10f, 0.9f), sliced: true);

            Text nameText = CreateText(namePlank.transform, "Name", 22, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -4f));
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = Color.white;
            if (isMine) tradeMyNameText = nameText; else tradeOtherNameText = nameText;

            // 2x2 grid.
            GameObject offerGrid = new GameObject(prefix + "OfferGrid", typeof(RectTransform), typeof(Image));
            offerGrid.transform.SetParent(tradePanel.transform, false);
            RectTransform gridRect = offerGrid.GetComponent<RectTransform>();
            gridRect.anchorMin = gridRect.anchorMax = new Vector2(1f, 1f);
            gridRect.pivot = new Vector2(1f, 1f);
            // TradeOffer*.png is 440x440 9-sliced {60,60,60,60}. Those caps are drawn
            // at native size, so at the old 220 they ate most of the rect and the
            // painted 2x2 boxes were tiny. At 280 the inner region is 160, putting
            // the compartment centres at +/-40 - which is what the cell maths below
            // now uses, instead of the old 96/55 that overshot the boxes entirely.
            gridRect.sizeDelta = new Vector2(280f, 280f);
            gridRect.anchoredPosition = new Vector2(-375f, anchoredY - 72f);
            ApplySprite(offerGrid.GetComponent<Image>(), Theme?.tradeOfferGrid,
                new Color(0f, 0f, 0f, 0.35f), sliced: true);

            const float cell = 80f;
            const float cellHalf = 40f;
            const float slotSize = 62f;
            for (int i = 0; i < OfferSlotCount; i++)
            {
                int row = i / 2;
                int col = i % 2;
                Vector2 pos = new Vector2(-cellHalf + col * cell, cellHalf - row * cell);
                BuildOfferSlot(offerGrid.transform, isMine, i, pos, slotSize);
            }

            // Money plank.
            GameObject moneyPlank = new GameObject(prefix + "MoneyPlank", typeof(RectTransform), typeof(Image));
            moneyPlank.transform.SetParent(tradePanel.transform, false);
            RectTransform moneyRect = moneyPlank.GetComponent<RectTransform>();
            moneyRect.anchorMin = moneyRect.anchorMax = new Vector2(1f, 1f);
            moneyRect.pivot = new Vector2(1f, 1f);
            // TradeMoney*.png is 460x112 with an asymmetric border {200,30,30,40}:
            // the money-bag art fills the fixed 200px left cap. Sliced at 230x56 the
            // horizontal middle collapsed to 30px while the caps stayed full size, so
            // the bag swallowed the plank - and 56 was below the 70px of vertical
            // border, squashing it further. Drawn Simple with preserveAspect instead,
            // the whole sprite scales evenly and keeps its shape at any size.
            moneyRect.sizeDelta = new Vector2(270f, 66f);
            moneyRect.anchoredPosition = new Vector2(-170f, anchoredY);
            ApplySprite(moneyPlank.GetComponent<Image>(), Theme?.tradeMoneyPlank,
                new Color(0.30f, 0.20f, 0.08f, 0.9f), sliced: false, preserveAspect: true);

            // The bag occupies the left ~44% of the art, so the amount is placed over
            // the plank to its right rather than centred across the whole sprite.
            Text moneyText = CreateText(moneyPlank.transform, "Money", 22, TextAnchor.MiddleCenter,
                new Vector2(0.44f, 0f), new Vector2(1f, 1f),
                new Vector2(6f, 6f), new Vector2(-12f, -8f));
            moneyText.fontStyle = FontStyle.Bold;
            moneyText.color = Color.white;
            moneyText.text = "P0";
            if (isMine) tradeMyMoneyText = moneyText; else tradeOtherMoneyText = moneyText;

            // Log / lock state icon.
            GameObject icon = new GameObject(prefix + "StateIcon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(tradePanel.transform, false);
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(1f, 1f);
            // Centre pivot: TradeSpinner rotates the transform, and rotation happens
            // about the pivot. At the old (1,1) the log swung around its own top-right
            // corner instead of turning on the spot. anchoredPosition is now the
            // icon's centre rather than its corner.
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(80f, 80f);
            // Grid and icon are treated as one 410-wide group centred under the name
            // and money planks above them, rather than being pushed to the outer
            // edges with a wide gap between.
            iconRect.anchoredPosition = new Vector2(-285f, anchoredY - 212f);

            Image iconImage = icon.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            if (isMine) tradeMyStateIcon = iconImage; else tradeOtherStateIcon = iconImage;
        }

        private void BuildOfferSlot(Transform parent, bool isMine, int index, Vector2 pos, float size)
        {
            GameObject slot = new GameObject("Slot" + index, typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(parent, false);

            RectTransform rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = pos;

            // Transparent but still raycastable, so it can receive drops.
            Image background = slot.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.001f);
            background.raycastTarget = true;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(slot.transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(size * 0.7f, size * 0.7f);
            iconRect.anchoredPosition = Vector2.zero;
            Image icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;

            Text count = CreateText(slot.transform, "Count", 18, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-4f, -2f));
            count.fontStyle = FontStyle.Bold;
            count.raycastTarget = false;
            count.text = "";

            if (isMine)
            {
                TradeDropSlot drop = slot.AddComponent<TradeDropSlot>();
                drop.interactable = true;
                drop.onItemDropped = OnItemDroppedIntoOffer;
                drop.onSlotClicked = OnOfferSlotClicked;
                mySlots[index] = drop;
                mySlotIcons[index] = icon;
                mySlotCounts[index] = count;
            }
            else
            {
                otherSlotIcons[index] = icon;
                otherSlotCounts[index] = count;
            }
        }

        private void BuildMoneyRow()
        {
            GameObject plank = new GameObject("AddMoneyPlank", typeof(RectTransform), typeof(Image));
            plank.transform.SetParent(tradePanel.transform, false);
            RectTransform rect = plank.GetComponent<RectTransform>();
            // Anchored to the panel's top-left like the inventory grid above it, so
            // the two stack predictably instead of being measured from opposite
            // edges - which is how they ended up overlapping.
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            // TradeAddmoneyPlank.png is 1400x148 (9.46:1). Its declared 9-slice border
            // is only 60, but the rope loops sit at x89-111 and x1304-1325 - outside
            // the caps, in the stretchable middle. Slicing therefore squeezed the
            // ropes along with the wood and moved them unpredictably. Drawn Simple
            // with preserveAspect the whole plank scales evenly and the ropes land at
            // a known place: at 600 wide (scale 0.4286) they occupy 38-48 and 559-568.
            rect.sizeDelta = new Vector2(600f, 64f);
            rect.anchoredPosition = new Vector2(TradeBoardInset, -670f);
            ApplySprite(plank.GetComponent<Image>(), Theme?.addMoneyPlank,
                new Color(0.30f, 0.20f, 0.08f, 0.9f), sliced: false, preserveAspect: true);

            Text label = CreateText(plank.transform, "Label", 22, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(70f, 0f), new Vector2(250f, 0f));
            label.text = "Add Money:";
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;

            tradeMoneyInput = CreateInput(plank.transform, "0",
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(258f, -20f), new Vector2(-58f, 20f), true);
            tradeMoneyInput.text = "0";

            // Enter sits beside the plank, not on it - inside the plank its right
            // edge landed on the rope. Placed clear of the lower offer grid's column.
            CreateButton(tradePanel.transform, "Enter", new Vector2(0f, 1f),
                new Vector2(150f, 60f), new Vector2(TradeBoardInset + 620f, -672f),
                OnEnterMoneyPressed, out _, art: Theme?.enterButton);
        }

        private void BuildTradeConfirmationPanel()
        {
            tradeConfirmPanel = CreatePanel("TradeFinalConfirmationPanel", new Vector2(880f, 660f), 0.98f,
                boardOverride: Theme?.tradeConfirmBoard);
            // Sunk into the board like the other signs; at the default +6 it sat too
            // high and left a strip of the top log showing beneath it.
            CreatePanelTitle(tradeConfirmPanel.transform, "TRADING",
                Theme?.tradeConfirmLabel, 520f, 150f, -20f);

            tradeConfirmSummary = CreateText(tradeConfirmPanel.transform, "Summary", 22, TextAnchor.UpperCenter,
                new Vector2(0f, 0.26f), new Vector2(1f, 0.78f), new Vector2(150f, 10f), new Vector2(-150f, -10f));

            // TradeConfirmBoard.png is 9-sliced {130,70,130,70}, so on an 880x660
            // panel the plank floor is y=-260. At the old y=55 the buttons reached
            // -275 and hung over the bottom log; 90 puts them 20 clear of it.
            tradeConfirmAcceptButton = CreateButton(tradeConfirmPanel.transform, "Accept",
                new Vector2(0.5f, 0f), new Vector2(200f, 62f), new Vector2(120f, 90f),
                OnFinalTradeConfirmPressed, out _,
                art: Theme?.confirmAcceptButton);
            CreateButton(tradeConfirmPanel.transform, "Decline", new Vector2(0.5f, 0f),
                new Vector2(200f, 62f), new Vector2(-120f, 90f), OnDeclineFinalPressed, out _,
                new Color(0.62f, 0.18f, 0.18f, 0.98f), art: Theme?.confirmDeclineButton);

            tradeConfirmPanel.SetActive(false);
        }

        /// <summary>The incoming "X wants to trade with you!" popup.</summary>
        private void BuildTradeRequestPopup()
        {
            tradeRequestPopup = CreatePanel("TradeRequestPopup", new Vector2(760f, 300f), 0.98f,
                boardOverride: Theme?.tradeRequestBoard);
            // TradeRequestLabel.png is 920x240 (3.83:1). At 460 it spanned 60% of the
            // 760-wide popup, which is what read as stretched; the negative offsetY
            // keeps the smaller sign seated on the board.
            CreatePanelTitle(tradeRequestPopup.transform, "TRADE REQUEST",
                Theme?.tradeRequestLabel, 340f, 120f, -12f);

            tradeRequestText = CreateText(tradeRequestPopup.transform, "Message", 24, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.30f), new Vector2(1f, 0.72f), new Vector2(80f, 0f), new Vector2(-80f, 0f));
            tradeRequestText.fontStyle = FontStyle.Bold;
            tradeRequestText.color = new Color(0.20f, 0.12f, 0.04f, 1f);

            CreateButton(tradeRequestPopup.transform, "Accept", new Vector2(0.5f, 0f),
                new Vector2(190f, 58f), new Vector2(-110f, 40f), OnAcceptTradePressed, out _,
                art: Theme?.acceptButton);
            CreateButton(tradeRequestPopup.transform, "Decline", new Vector2(0.5f, 0f),
                new Vector2(190f, 58f), new Vector2(110f, 40f), OnDeclineTradePressed, out _,
                new Color(0.62f, 0.18f, 0.18f, 0.98f), art: Theme?.declineButton);

            tradeRequestPopup.SetActive(false);
        }

        private static void ApplySprite(Image image, Sprite art, Color fallback, bool sliced,
            bool preserveAspect = false)
        {
            if (art != null)
            {
                image.sprite = art;
                if (sliced)
                    image.type = Image.Type.Sliced;
                image.preserveAspect = preserveAspect;
                image.color = Color.white;
                return;
            }
            image.color = fallback;
        }

        // ------------------------------------------------------------- lifecycle

        private void OnActiveTradeChanged(TradeViewDto trade)
        {
            TradeViewDto previous = displayedTrade;
            displayedTrade = trade;

            if (trade == null)
            {
                HideTradeRequestPopup();

                // The session is gone, so nothing may keep pushing offers at it.
                StopOfferPushLoop();
                tradeSessionPrepared = false;
                if (tradeConfirmPanel != null)
                    tradeConfirmPanel.SetActive(false);

                if (tradePanel != null && tradePanel.activeSelf)
                {
                    // /api/trades/active only reports PENDING and ACTIVE sessions,
                    // so the moment the other player cancels it answers null rather
                    // than a CANCELLED view - which meant the CANCELLED branch below
                    // never ran for them and they were left staring at a dead panel
                    // with no way out. Close it here instead.
                    //
                    // A trade that finished normally is the exception: its result is
                    // left on screen for the player to read and dismiss themselves.
                    bool finishedNormally = previous != null && previous.status == "COMPLETED";
                    if (finishedNormally)
                    {
                        // This is the first player to have accepted: their own
                        // request came back with the trade still active, and it is
                        // this poll that discovers the other side finished it.
                        FinishAndCloseTrade();
                    }
                    else
                    {
                        tradePanel.SetActive(false);
                    }
                }
                return;
            }

            // A PENDING trade the current player did not start is an invitation.
            bool amTarget = trade.targetId == CurrentUserId;
            if (trade.status == "PENDING" && amTarget)
                ShowTradeRequestPopup(trade);
            else
                HideTradeRequestPopup();

            if (trade.status == "ACTIVE" && (tradePanel == null || !tradePanel.activeSelf))
                ShowTradePanel(trade);
            else if (tradePanel != null && tradePanel.activeSelf)
                RefreshTradePanel();

            if (trade.status == "COMPLETED" || trade.status == "CANCELLED" || trade.status == "DECLINED")
            {
                StopOfferPushLoop();
                tradeSessionPrepared = false;
                if (tradeConfirmPanel != null)
                    tradeConfirmPanel.SetActive(false);
            }

            // A cancel/decline from either side leaves nothing to look at, so the
            // screen closes for BOTH players - this handler runs on every device
            // via the poll, not just the one that pressed the button. COMPLETED is
            // left open on purpose: the presser needs to see the success message,
            // and Cancel Trade below now closes it safely once there is nothing
            // left to cancel.
            if ((trade.status == "CANCELLED" || trade.status == "DECLINED") &&
                tradePanel != null && tradePanel.activeSelf)
            {
                tradePanel.SetActive(false);
            }
        }

        private void ShowTradeRequestPopup(TradeViewDto trade)
        {
            if (tradeRequestPopup == null || trade == null)
                return;

            // Do not re-open the popup the player just dismissed for this trade.
            if (tradeRequestShownForId == trade.id && tradeRequestPopup.activeSelf)
                return;

            tradeRequestShownForId = trade.id;
            tradeRequestText.text = trade.requesterDisplayName + " wants to trade\nwith you!";
            tradeRequestPopup.SetActive(true);
            tradeRequestPopup.transform.SetAsLastSibling();
        }

        private void HideTradeRequestPopup()
        {
            if (tradeRequestPopup != null)
                tradeRequestPopup.SetActive(false);
        }

        private void ShowTradePanel(TradeViewDto trade)
        {
            if (trade == null) return;
            displayedTrade = trade;
            HideMainPanels();
            tradePanel.SetActive(true);
            // The hotbar is built by a different script and can land later in the
            // canvas hierarchy, drawing over the Cancel / Set Trade row. Re-parenting
            // to last on open keeps the trade board above it.
            tradePanel.transform.SetAsLastSibling();

            pendingTradeItems.Clear();
            SeedPendingItemsFromOffer(MyOffer(trade));
            RefreshTradePanel();
            RenderTradeInventory();

            if (trade.status == "ACTIVE")
                StartCoroutine(PrepareThenStartLiveOffers());
        }

        /// <summary>
        /// Saves the farm once so the server can validate live offer edits, then
        /// starts the debounced push loop.
        /// </summary>
        private IEnumerator PrepareThenStartLiveOffers()
        {
            if (tradeSessionPrepared || preparingTradeSession)
                yield break;

            preparingTradeSession = true;
            tradeStatusText.text = "Preparing trade...";
            bool ready = false;
            string error = null;
            yield return controller.PrepareTradeSession(() => ready = true, message => error = message);

            if (!ready)
            {
                // Remembered so a later drag can explain itself instead of
                // reporting success it did not achieve, and left un-prepared so the
                // next drag retries. Without the retry a single failed sync killed
                // trading for the whole session: the push loop never started, so
                // drags only ever updated the local dictionary and the offer box
                // stayed empty no matter what the player did.
                tradePrepareError = string.IsNullOrWhiteSpace(error)
                    ? "Could not sync your farm. Tap an item again to retry."
                    : error;
                tradeStatusText.text = tradePrepareError;
                preparingTradeSession = false;
                yield break;
            }

            tradePrepareError = null;
            preparingTradeSession = false;
            tradeSessionPrepared = true;
            tradeStatusText.text = "Drag items from your backpack into your trade box.";
            StartOfferPushLoop();
        }

        private void StartOfferPushLoop()
        {
            if (offerPushLoop == null)
                offerPushLoop = StartCoroutine(OfferPushLoop());
        }

        private void StopOfferPushLoop()
        {
            if (offerPushLoop != null)
            {
                StopCoroutine(offerPushLoop);
                offerPushLoop = null;
            }
            offerDirty = false;
        }

        /// <summary>
        /// Pushes the local offer shortly after it changes. Debounced so a burst of
        /// drags becomes one request instead of several, while still reaching the
        /// other player quickly.
        /// </summary>
        private IEnumerator OfferPushLoop()
        {
            while (tradePanel != null && tradePanel.activeSelf)
            {
                if (offerDirty)
                {
                    offerDirty = false;
                    yield return PushOffer();
                }
                yield return new WaitForSecondsRealtime(0.35f);
            }
            offerPushLoop = null;
        }

        private IEnumerator PushOffer()
        {
            if (displayedTrade == null || displayedTrade.status != "ACTIVE")
                yield break;

            UpdateTradeOfferRequestDto request = new UpdateTradeOfferRequestDto
            {
                money = ParsePositive(tradeMoneyInput, 0)
            };
            foreach (KeyValuePair<InventoryItemType, int> pair in pendingTradeItems)
            {
                request.items.Add(new TradeItemDto
                {
                    itemType = pair.Key.ToString(),
                    quantity = pair.Value
                });
            }

            yield return controller.UpdateTradeOfferLive(displayedTrade.id, request,
                value =>
                {
                    displayedTrade = value;
                    RefreshTradePanel();
                },
                message =>
                {
                    if (!string.IsNullOrEmpty(message) &&
                        message.IndexOf(SeedUnavailableMarker, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // The server refused a seed this device could not check - an
                        // older server that did not send the other player's district.
                        // Put the box back to what the server last accepted, so the
                        // seed is out of the offer here as well.
                        pendingTradeItems.Clear();
                        SeedPendingItemsFromOffer(MyOffer(displayedTrade));
                        RefreshTradePanel();
                        ShowTradeSeedNotice();
                        return;
                    }

                    tradeStatusText.text = message;
                });
        }

        // ------------------------------------------------------------- rendering

        private TradeOfferDto MyOffer(TradeViewDto trade)
        {
            if (trade == null) return null;
            return trade.requesterId == CurrentUserId ? trade.requesterOffer : trade.targetOffer;
        }

        private TradeOfferDto TheirOffer(TradeViewDto trade)
        {
            if (trade == null) return null;
            return trade.requesterId == CurrentUserId ? trade.targetOffer : trade.requesterOffer;
        }

        private void SeedPendingItemsFromOffer(TradeOfferDto offer)
        {
            if (offer?.items == null) return;
            foreach (TradeItemDto item in offer.items)
            {
                if (System.Enum.TryParse(item.itemType, out InventoryItemType parsed))
                    pendingTradeItems[parsed] = item.quantity;
            }
            if (tradeMoneyInput != null)
                tradeMoneyInput.text = offer.money.ToString();
        }

        private void RefreshTradePanel()
        {
            if (displayedTrade == null) return;

            bool currentRequester = displayedTrade.requesterId == CurrentUserId;
            string myName = currentRequester
                ? displayedTrade.requesterDisplayName
                : displayedTrade.targetDisplayName;
            string otherName = currentRequester
                ? displayedTrade.targetDisplayName
                : displayedTrade.requesterDisplayName;

            if (tradeMyNameText != null) tradeMyNameText.text = myName;
            if (tradeOtherNameText != null) tradeOtherNameText.text = otherName;

            TradeOfferDto mine = MyOffer(displayedTrade);
            TradeOfferDto theirs = TheirOffer(displayedTrade);

            if (tradeMyMoneyText != null)
                tradeMyMoneyText.text = "P" + (mine != null ? mine.money : 0);
            if (tradeOtherMoneyText != null)
                tradeOtherMoneyText.text = "P" + (theirs != null ? theirs.money : 0);

            RenderOfferSlots(mine, mySlotIcons, mySlotCounts, mySlots);
            RenderOfferSlots(theirs, otherSlotIcons, otherSlotCounts, null);

            bool myAgreed = currentRequester
                ? displayedTrade.requesterAgreed
                : displayedTrade.targetAgreed;
            bool otherAgreed = currentRequester
                ? displayedTrade.targetAgreed
                : displayedTrade.requesterAgreed;

            ApplyStateIcon(tradeMyStateIcon, myAgreed);
            ApplyStateIcon(tradeOtherStateIcon, otherAgreed);

            bool active = displayedTrade.status == "ACTIVE";
            tradeSetOfferButton.gameObject.SetActive(active);
            tradeSetOfferButton.interactable = active && !myAgreed;

            // Both locked in: time for the final give/get confirmation.
            if (active && myAgreed && otherAgreed &&
                tradeConfirmPanel != null && !tradeConfirmPanel.activeSelf)
            {
                OpenTradeConfirmation();
            }

            if (displayedTrade.status == "COMPLETED")
                tradeStatusText.text = "Trade completed. Both backpacks were updated.";
            else if (!active)
                tradeStatusText.text = "Trade " + displayedTrade.status.ToLowerInvariant() + ".";
        }

        private void RenderOfferSlots(TradeOfferDto offer, Image[] icons, Text[] counts, TradeDropSlot[] slots)
        {
            int index = 0;
            if (offer?.items != null)
            {
                foreach (TradeItemDto item in offer.items)
                {
                    if (index >= OfferSlotCount) break;
                    if (!System.Enum.TryParse(item.itemType, out InventoryItemType parsed))
                        continue;

                    icons[index].sprite = InventoryIcon(parsed);
                    icons[index].enabled = icons[index].sprite != null;
                    counts[index].text = "x" + item.quantity;
                    if (slots != null) slots[index].occupant = parsed;
                    index++;
                }
            }

            for (; index < OfferSlotCount; index++)
            {
                icons[index].enabled = false;
                counts[index].text = "";
                if (slots != null) slots[index].occupant = InventoryItemType.None;
            }
        }

        private void ApplyStateIcon(Image image, bool locked)
        {
            if (image == null) return;

            Sprite art = locked ? Theme?.tradeLockIcon : Theme?.tradeLogIcon;
            image.sprite = art;
            image.enabled = art != null;

            // Only the unlocked log spins; a padlock stays upright.
            TradeSpinner spinner = image.GetComponent<TradeSpinner>();
            if (!locked && art != null)
            {
                if (spinner == null)
                    spinner = image.gameObject.AddComponent<TradeSpinner>();
                spinner.degreesPerSecond = Theme != null ? Theme.tradeLogSpinSpeed : 90f;
                spinner.enabled = true;
            }
            else if (spinner != null)
            {
                spinner.enabled = false;
                image.rectTransform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>Draws the player's own backpack as drag sources.</summary>
        private void RenderTradeInventory()
        {
            if (tradeInventoryContent == null || PlayerInventory.Instance == null)
                return;

            for (int i = tradeInventoryContent.childCount - 1; i >= 0; i--)
                Destroy(tradeInventoryContent.GetChild(i).gameObject);

            Vector2 size = tradeInventoryContent.sizeDelta;
            float cellW = size.x / InventoryColumns;
            float cellH = size.y / InventoryRows;
            float slotSize = Mathf.Min(cellW, cellH) * 0.82f;

            int index = 0;
            for (int slot = 0; slot < PlayerInventory.InventorySlotCount &&
                               index < InventoryColumns * InventoryRows; slot++)
            {
                InventorySlotData data = PlayerInventory.Instance.GetSlot(slot);
                if (data == null || data.IsEmpty)
                    continue;
                if (!SocialMarketplaceCatalog.IsTradable(data.itemType))
                    continue;

                int row = index / InventoryColumns;
                int col = index % InventoryColumns;
                Vector2 pos = new Vector2(
                    -size.x * 0.5f + cellW * (col + 0.5f),
                    size.y * 0.5f - cellH * (row + 0.5f));

                BuildInventoryCell(data, pos, slotSize);
                index++;
            }
        }

        private void BuildInventoryCell(InventorySlotData data, Vector2 pos, float size)
        {
            GameObject cell = new GameObject("Item_" + data.itemType,
                typeof(RectTransform), typeof(Image), typeof(TradeDragSource));
            cell.transform.SetParent(tradeInventoryContent, false);

            RectTransform rect = cell.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = pos;

            Sprite icon = InventoryIcon(data.itemType);
            Image image = cell.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
            image.raycastTarget = true;

            Text count = CreateText(cell.transform, "Count", 16, TextAnchor.LowerRight,
                Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            count.text = data.amount > 1 ? data.amount.ToString() : "";
            count.raycastTarget = false;

            TradeDragSource drag = cell.GetComponent<TradeDragSource>();
            drag.itemType = data.itemType;
            drag.icon = icon;
        }

        private InventoryUIBuilder cachedInventoryUI;

        /// <summary>
        /// Borrows the backpack's icon table. Cached because this is called once per
        /// visible cell, and a scene-wide search per item would be wasteful.
        /// </summary>
        private Sprite InventoryIcon(InventoryItemType item)
        {
            if (cachedInventoryUI == null)
                cachedInventoryUI = Object.FindFirstObjectByType<InventoryUIBuilder>();

            return cachedInventoryUI != null ? cachedInventoryUI.GetSpriteFor(item) : null;
        }

        // ---------------------------------------------------------- interactions

        private void OnItemDroppedIntoOffer(InventoryItemType item)
        {
            if (displayedTrade == null || displayedTrade.status != "ACTIVE")
                return;

            // A seed the other player's district does not grow never goes into the
            // box - it would hand them a seed they have no other way to get. It stays
            // in the backpack, which is where it was all along.
            if (!IsSeedAvailableToPartner(item))
            {
                ShowTradeSeedNotice();
                return;
            }

            int owned = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetCount(item) : 0;
            int already = pendingTradeItems.TryGetValue(item, out int current) ? current : 0;

            if (already >= owned)
            {
                tradeStatusText.text = "You do not own any more " +
                                       SocialMarketplaceCatalog.FriendlyName(item) + ".";
                return;
            }

            // A new item needs a free cell; the 2x2 grid only holds four stacks.
            if (already == 0 && pendingTradeItems.Count >= OfferSlotCount)
            {
                tradeStatusText.text = "Your trade box is full (4 item types).";
                return;
            }

            pendingTradeItems[item] = already + 1;
            offerDirty = true;

            if (!tradeSessionPrepared)
            {
                // Nothing is pushing offers yet, so saying "offer updated" would be
                // a lie - the other player would never see it. Say what is actually
                // wrong and try the sync again.
                tradeStatusText.text = tradePrepareError ?? "Preparing trade...";
                StartCoroutine(PrepareThenStartLiveOffers());
                return;
            }

            tradeStatusText.text = "Offer updated.";
        }

        /// <summary>
        /// Whether the other player's district grows the item. Anything that is not a
        /// crop seed is always allowed. When the server did not say which district
        /// the other player is in, this does not guess: the server still checks, and
        /// its refusal comes back through PushOffer.
        /// </summary>
        private bool IsSeedAvailableToPartner(InventoryItemType item)
        {
            if (!DistrictCropPools.IsDistrictRestricted(item) || displayedTrade == null)
                return true;

            string partnerDistrict = displayedTrade.requesterId == CurrentUserId
                ? displayedTrade.targetDistrictName
                : displayedTrade.requesterDistrictName;

            if (partnerDistrict == null)
                return true;

            return DistrictCropPools.IsAvailableIn(item, partnerDistrict);
        }

        private void ShowTradeSeedNotice()
        {
            if (tradeSeedNoticePanel == null)
                BuildTradeSeedNotice();

            tradeSeedNoticePanel.SetActive(true);
            tradeSeedNoticePanel.transform.SetAsLastSibling();

            if (tradeStatusText != null)
                tradeStatusText.text = "That seed cannot go to the other player.";
        }

        /// <summary>
        /// Built the first time it is needed rather than with the rest of the trade
        /// screen, so a player who never offers the wrong seed never pays for it.
        /// </summary>
        private void BuildTradeSeedNotice()
        {
            tradeSeedNoticePanel = CreatePanel("TradeSeedUnavailableNotice", new Vector2(900f, 300f), 0.98f,
                boardOverride: Theme?.saveFarmBoard);

            Text message = CreateText(tradeSeedNoticePanel.transform, "Message", 25, TextAnchor.MiddleCenter,
                new Vector2(0f, 0.36f), new Vector2(1f, 0.86f), new Vector2(90f, 0f), new Vector2(-90f, 0f));
            message.fontStyle = FontStyle.Bold;
            message.color = Color.white;
            message.text = SeedUnavailableNotice;

            CreateButton(tradeSeedNoticePanel.transform, "Okay!", new Vector2(0.5f, 0f),
                new Vector2(200f, 62f), new Vector2(0f, 62f),
                () => tradeSeedNoticePanel.SetActive(false), out _, art: Theme?.okayButton);

            tradeSeedNoticePanel.SetActive(false);
        }

        private void OnOfferSlotClicked(InventoryItemType item)
        {
            if (displayedTrade == null || displayedTrade.status != "ACTIVE")
                return;
            if (!pendingTradeItems.TryGetValue(item, out int current))
                return;

            if (current <= 1)
                pendingTradeItems.Remove(item);
            else
                pendingTradeItems[item] = current - 1;

            offerDirty = true;
            tradeStatusText.text = "Removed one " + SocialMarketplaceCatalog.FriendlyName(item) + ".";
        }

        private void OnEnterMoneyPressed()
        {
            offerDirty = true;
            tradeStatusText.text = "Money offer updated.";
        }

        private void OnSetTradePressed()
        {
            if (displayedTrade == null) return;

            // Flush any queued edits before locking, so the agreed offer is current.
            StartCoroutine(SetTradeSequence());
        }

        private IEnumerator SetTradeSequence()
        {
            if (offerDirty)
            {
                offerDirty = false;
                yield return PushOffer();
            }

            yield return controller.AgreeTrade(displayedTrade.id,
                value =>
                {
                    displayedTrade = value;
                    RefreshTradePanel();
                },
                message => tradeStatusText.text = message);
        }

        private void OpenTradeConfirmation()
        {
            if (displayedTrade == null) return;

            TradeOfferDto give = MyOffer(displayedTrade);
            TradeOfferDto get = TheirOffer(displayedTrade);
            tradeConfirmSummary.text =
                "You will give:\n" + FormatOffer(give) + "\n\n" +
                "You will get:\n" + FormatOffer(get);

            // Pressable again each time the board opens, so a declined-and-reopened
            // confirmation is not stuck showing the previous attempt's state - but
            // not once this player has accepted. The first player to accept gets the
            // trade back still ACTIVE, which closes and reopens this board, and it
            // used to reopen with Accept lit again while they waited on the other.
            bool alreadyAccepted = displayedTrade.requesterId == CurrentUserId
                ? displayedTrade.requesterConfirmed
                : displayedTrade.targetConfirmed;
            if (tradeConfirmAcceptButton != null)
                tradeConfirmAcceptButton.interactable = !alreadyAccepted;

            tradeConfirmPanel.SetActive(true);
            tradeConfirmPanel.transform.SetAsLastSibling();
        }

        private void OnFinalTradeConfirmPressed()
        {
            if (displayedTrade == null) return;

            // Greyed the moment it is pressed. The first player to accept then
            // waits on the second, and with the button still lit there was no way
            // to tell a press that registered from one that did not - so players
            // pressed it again while the first request was still in flight.
            if (tradeConfirmAcceptButton != null)
                tradeConfirmAcceptButton.interactable = false;

            StartCoroutine(controller.ConfirmTrade(displayedTrade.id,
                value =>
                {
                    displayedTrade = value;
                    tradeConfirmPanel.SetActive(false);
                    RefreshTradePanel();
                    RenderTradeInventory();

                    // The second player to accept gets COMPLETED back from their
                    // own request rather than from a later poll, and nothing here
                    // used to close the trade board for them - so they were left
                    // sitting on a finished trade while the other player's board
                    // had already closed.
                    if (value != null && value.status == "COMPLETED")
                        FinishAndCloseTrade();
                },
                message =>
                {
                    // Pressable again, or a failed accept leaves the board dead.
                    if (tradeConfirmAcceptButton != null)
                        tradeConfirmAcceptButton.interactable = true;
                    tradeConfirmSummary.text += "\n\nError: " + message;
                }));
        }

        /// <summary>
        /// Shows the result for a moment, then closes the trade board.
        ///
        /// Both players end up here - one from their own confirm response, the
        /// other from the poll that finds the session gone - so both boards close
        /// on their own. The board used to be left open for the player to dismiss
        /// with Cancel, which technically worked but asked somebody to press
        /// "cancel" on a trade that had already succeeded.
        /// </summary>
        private void FinishAndCloseTrade()
        {
            if (tradeClosingAfterCompletion)
                return;

            tradeClosingAfterCompletion = true;

            if (tradeConfirmPanel != null)
                tradeConfirmPanel.SetActive(false);

            if (tradeStatusText != null)
                tradeStatusText.text = "Trade completed. Both backpacks were updated.";

            // The backpack column still showed the items traded away while the
            // result was on screen, for the player whose trade finished by poll.
            RenderTradeInventory();

            StopOfferPushLoop();
            tradeSessionPrepared = false;

            StartCoroutine(CloseTradePanelAfterResult());
        }

        private IEnumerator CloseTradePanelAfterResult()
        {
            // Long enough to read the line above, short enough that neither
            // player is left waiting on the other's screen.
            yield return new WaitForSecondsRealtime(2.5f);

            if (tradePanel != null)
                tradePanel.SetActive(false);

            displayedTrade = null;
            tradeClosingAfterCompletion = false;
        }

        private void OnDeclineFinalPressed()
        {
            tradeConfirmPanel.SetActive(false);
            OnCancelTradePressed();
        }

        private void OnAcceptTradePressed()
        {
            if (displayedTrade == null) return;
            HideTradeRequestPopup();
            StartCoroutine(controller.Api.AcceptTrade(displayedTrade.id,
                value =>
                {
                    displayedTrade = value;
                    ShowTradePanel(value);
                }, message => tradeStatusText.text = message));
        }

        private void OnDeclineTradePressed()
        {
            if (displayedTrade == null) return;
            HideTradeRequestPopup();
            StartCoroutine(controller.Api.DeclineTrade(displayedTrade.id,
                value => displayedTrade = value,
                message => tradeStatusText.text = message));
        }

        private void OnCancelTradePressed()
        {
            if (displayedTrade == null) return;

            StopOfferPushLoop();
            tradeSessionPrepared = false;

            // COMPLETED here means the presser is just dismissing the success
            // screen - the trade already exists on the server as done, so hitting
            // the cancel endpoint would only bounce back "this trade is already
            // closed." This button doubles as the close action whenever there is
            // nothing left to actually cancel.
            if (displayedTrade.status != "ACTIVE" && displayedTrade.status != "PENDING")
            {
                tradePanel.SetActive(false);
                return;
            }

            StartCoroutine(controller.Api.CancelTrade(displayedTrade.id,
                value =>
                {
                    displayedTrade = value;
                    tradePanel.SetActive(false);
                }, message => tradeStatusText.text = message));
        }

        private static string FormatOffer(TradeOfferDto offer)
        {
            if (offer == null) return "Nothing";
            string text = "Money: P" + offer.money;
            if (offer.items == null || offer.items.Count == 0)
                return text + "\nItems: none";
            foreach (TradeItemDto item in offer.items)
                text += "\n" + item.quantity + " x " + FriendlyItem(item.itemType);
            return text;
        }
    }

    /// <summary>Spins a RectTransform; used for the unlocked "log" trade state.</summary>
    public class TradeSpinner : MonoBehaviour
    {
        public float degreesPerSecond = 90f;

        private void Update()
        {
            transform.Rotate(0f, 0f, -degreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
