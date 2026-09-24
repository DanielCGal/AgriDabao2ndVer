using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public partial class SocialMarketplaceUIBuilder
    {
        private void BuildMarketplacePanel()
        {
            marketplacePanel = CreatePanel("PlayerMarketplacePanel", new Vector2(1400f, 800f));
            HudRegistry.RegisterPiece(HudPiece.MarketplacePanel, marketplacePanel);
            CreatePanelTitle(marketplacePanel.transform, "PLAYER MARKETPLACE",
                Theme?.marketplaceLabel, 480f, 150f, -13f);

            CreateButton(marketplacePanel.transform, "<", new Vector2(0f, 1f),
                new Vector2(80f, 50f), new Vector2(SocialBoardLogInset, -150f),
                PreviousMarketplaceFilter, out _, art: Theme?.filterPrevButton);
            marketplaceFilterText = CreateText(marketplacePanel.transform, "Filter", 21, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(245f, -200f), new Vector2(505f, -150f));
            marketplaceFilterText.text = "Filter: ALL ITEMS";
            CreateButton(marketplacePanel.transform, ">", new Vector2(0f, 1f),
                new Vector2(80f, 50f), new Vector2(515f, -150f), NextMarketplaceFilter, out _,
                art: Theme?.filterNextButton);

            CreateButton(marketplacePanel.transform, "Refresh", new Vector2(1f, 1f),
                new Vector2(160f, 50f), new Vector2(-495f, -150f),
                () => StartCoroutine(RefreshMarketplace()), out _, art: Theme?.refreshButton);
            sellItemsButton = CreateButton(marketplacePanel.transform, "Sell Items", new Vector2(1f, 1f),
                new Vector2(175f, 50f), new Vector2(-310f, -150f), OpenSellPanel, out _,
                art: Theme?.sellItemsButton);
            CreateButton(marketplacePanel.transform, "Close", new Vector2(1f, 1f),
                new Vector2(150f, 50f), new Vector2(-SocialBoardLogInset, -150f),
                () => marketplacePanel.SetActive(false), out _,
                art: Theme?.marketplaceCloseButton);

            marketplaceContent = CreateScrollContent(marketplacePanel.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(SocialBoardLogInset, 100f),
                new Vector2(-SocialBoardLogInset, -215f));

            marketplaceStatusText = CreateText(marketplacePanel.transform, "Status", 20,
                TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(SocialBoardLogInset, 30f), new Vector2(-SocialBoardLogInset, 85f));
            marketplaceStatusText.text = "Browse listings from other players.";
            marketplacePanel.SetActive(false);
        }

        private void BuildSellPanel()
        {
            sellPanel = CreatePanel("SellItemsPanel", new Vector2(1000f, 680f), 0.97f);
            CreatePanelTitle(sellPanel.transform, "SELL ITEMS", Theme?.sellItemsLabel, 360f, 150f, -12f);

            CreateButton(sellPanel.transform, "<", new Vector2(0f, 1f),
                new Vector2(80f, 50f), new Vector2(SocialBoardLogInset, -80f), PreviousSellItem, out _,
                art: Theme?.filterPrevButton);
            sellItemText = CreateText(sellPanel.transform, "SelectedItem", 24, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(250f, -130f), new Vector2(-250f, -80f));
            CreateButton(sellPanel.transform, ">", new Vector2(1f, 1f),
                new Vector2(80f, 50f), new Vector2(-SocialBoardLogInset, -80f), NextSellItem, out _,
                art: Theme?.filterNextButton);

            sellInventoryText = CreateText(sellPanel.transform, "Inventory", 21, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(SocialBoardLogInset, -224f), new Vector2(-SocialBoardLogInset, -174f));

            Text qtyLabel = CreateText(sellPanel.transform, "QuantityLabel", 21, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -318f), new Vector2(400f, -268f));
            qtyLabel.text = "Quantity:";
            sellQuantityInput = CreateInput(sellPanel.transform, "1",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(420f, -318f), new Vector2(-SocialBoardLogInset, -268f), true);
            sellQuantityInput.text = "1";

            Text priceLabel = CreateText(sellPanel.transform, "PriceLabel", 21, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -412f), new Vector2(400f, -362f));
            priceLabel.text = "Selling Price:";
            sellPriceInput = CreateInput(sellPanel.transform, "100",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(420f, -412f), new Vector2(-SocialBoardLogInset, -362f), true);
            sellPriceInput.text = "100";

            sellFeeText = CreateText(sellPanel.transform, "Fee", 19, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(SocialBoardLogInset, -506f), new Vector2(-SocialBoardLogInset, -456f));

            sellQuantityInput.onValueChanged.AddListener(_ => RefreshSellSelection());
            sellPriceInput.onValueChanged.AddListener(_ => RefreshSellSelection());

            CreateButton(sellPanel.transform, "Create Listing", new Vector2(0.5f, 0f),
                new Vector2(240f, 60f), new Vector2(-130f, 70f), OnCreateListingPressed, out _,
                art: Theme?.createListingButton);
            CreateButton(sellPanel.transform, "Cancel", new Vector2(0.5f, 0f),
                new Vector2(200f, 60f), new Vector2(130f, 70f),
                () =>
                {
                    sellPanel.SetActive(false);
                    marketplacePanel.SetActive(true);
                }, out _, new Color(0.40f, 0.40f, 0.40f, 0.98f),
                art: Theme?.sellCancelButton);
            sellPanel.SetActive(false);
        }

        private void BuildSaleNotificationPanel()
        {
            saleNotifyPanel = CreatePanel("MarketplaceSalePanel", new Vector2(720f, 560f), 0.98f,
                boardOverride: Theme?.saleNotifyBoard);
            CreatePanelTitle(saleNotifyPanel.transform, "PLAYER MARKETPLACE",
                Theme?.saleNotifyLabel, 560f, 150f);

            saleNotifyText = CreateText(saleNotifyPanel.transform, "SaleInfo", 23, TextAnchor.UpperCenter,
                new Vector2(0f, 0.22f), new Vector2(1f, 0.78f), new Vector2(110f, 0f), new Vector2(-110f, 0f));
            saleNotifyText.fontStyle = FontStyle.Bold;

            CreateButton(saleNotifyPanel.transform, "Okay!", new Vector2(0.5f, 0f),
                new Vector2(230f, 66f), new Vector2(0f, 55f),
                ShowNextSaleNotification, out _, art: Theme?.okayButton);

            saleNotifyPanel.SetActive(false);
        }

        private void OnSaleNotified(MarketplaceSaleDto sale)
        {
            if (sale == null) return;
            pendingSaleNotifications.Enqueue(sale);

            if (saleNotifyPanel != null && !saleNotifyPanel.activeSelf)
                ShowNextSaleNotification();
        }

        private void ShowNextSaleNotification()
        {
            if (saleNotifyPanel == null)
                return;

            if (pendingSaleNotifications.Count == 0)
            {
                saleNotifyPanel.SetActive(false);
                return;
            }

            MarketplaceSaleDto sale = pendingSaleNotifications.Dequeue();
            saleNotifyText.text =
                sale.buyerDisplayName + " bought your items in the\nmarketplace!\n\n" +
                "Item Listed:\n" + sale.quantity + " x " + FriendlyItem(sale.itemType) + "\n\n" +
                "Total Money Received:\nP" + sale.moneyReceived;

            saleNotifyPanel.SetActive(true);
            saleNotifyPanel.transform.SetAsLastSibling();

            if (GameAudioManager.HasInstance)
                GameAudioManager.Instance.PlayReward();
        }

        private void PreviousMarketplaceFilter()
        {
            marketplaceFilterIndex = StepMarketplaceFilter(marketplaceFilterIndex, -1);
            RefreshMarketplaceFilterLabel();
            StartCoroutine(RefreshMarketplace());
        }

        private void NextMarketplaceFilter()
        {
            marketplaceFilterIndex = StepMarketplaceFilter(marketplaceFilterIndex, 1);
            RefreshMarketplaceFilterLabel();
            StartCoroutine(RefreshMarketplace());
        }

        private static int StepMarketplaceFilter(int index, int direction)
        {
            InventoryItemType[] items = SocialMarketplaceCatalog.TradableItems;
            int count = items.Length;

            for (int guard = 0; guard <= count + 1; guard++)
            {
                index += direction;
                if (index >= count) index = -1;
                if (index < -1) index = count - 1;

                if (index < 0 || DistrictCropPools.IsAvailableToPlayer(items[index]))
                    return index;
            }

            return -1;
        }

        private void RefreshMarketplaceFilterLabel()
        {
            marketplaceFilterText.text = marketplaceFilterIndex < 0
                ? "Filter: ALL ITEMS"
                : "Filter: " + SocialMarketplaceCatalog.FriendlyName(
                    SocialMarketplaceCatalog.TradableItems[marketplaceFilterIndex]);
        }

        private IEnumerator RefreshMarketplace()
        {
            if (TutorialState.IsRunning)
            {
                ClearContent(marketplaceContent);
                marketplaceStatusText.text =
                    "Listings are hidden until you finish the beginner guide.";
                yield break;
            }

            marketplaceStatusText.text = "Synchronizing money and inventory...";
            string syncError = null;
            yield return controller.SyncRemoteEconomyPreservingLocalChanges(
                null, message => syncError = message);
            if (!string.IsNullOrWhiteSpace(syncError))
                marketplaceStatusText.text = "Economy sync warning: " + syncError;
            else
                marketplaceStatusText.text = "Loading marketplace listings...";

            string filter = marketplaceFilterIndex < 0
                ? "ALL"
                : SocialMarketplaceCatalog.TradableItems[marketplaceFilterIndex].ToString();
            List<MarketplaceListingDto> listings = null;
            string error = null;
            yield return controller.Api.GetListings(filter,
                value => listings = value, message => error = message);
            if (listings == null)
            {
                marketplaceStatusText.text = error;
                yield break;
            }

            listings.RemoveAll(listing =>
                !listing.mine &&
                System.Enum.TryParse(listing.itemType, out InventoryItemType listedItem) &&
                !DistrictCropPools.IsAvailableToPlayer(listedItem));

            ClearContent(marketplaceContent);
            foreach (MarketplaceListingDto listing in listings)
            {
                MarketplaceListingDto captured = listing;
                GameObject row = CreateListRow(marketplaceContent, 115f);
                Text info = CreateText(row.transform, "Info", 20, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 1f),
                    new Vector2(RowRopeInset, 8f), new Vector2(-310f, -8f));
                string special = captured.status == "RETURN_PENDING"
                    ? "\nRETURN PENDING - free inventory space"
                    : "";
                info.text = captured.sellerDisplayName + " sells " + captured.quantity + " x " +
                            FriendlyItem(captured.itemType) + "\nPrice: P" + captured.askingPrice +
                            " | Expires: " + FormatDate(captured.expiresAt) + special;

                string buttonLabel = captured.mine
                    ? (captured.status == "RETURN_PENDING" ? "Claim Return" : "Cancel")
                    : "Buy Seller Items";
                Sprite buttonArt = captured.mine
                    ? Theme?.cancelListingButton
                    : Theme?.buyListingButton;
                CreateButton(row.transform, buttonLabel, new Vector2(1f, 0.5f),
                    new Vector2(185f, 58f), new Vector2(-RowRopeInset, 0f),
                    () =>
                    {
                        if (captured.mine)
                            StartCoroutine(CancelOrClaimListing(captured));
                        else
                            StartCoroutine(BuyListing(captured));
                    }, out _, captured.mine
                        ? new Color(0.50f, 0.30f, 0.10f, 0.98f)
                        : (Color?)null,
                    art: buttonArt);
            }
            marketplaceStatusText.text = listings.Count == 0
                ? "No active listings match this filter."
                : listings.Count + " marketplace listing(s).";
        }

        private void OpenSellPanel()
        {
            if (TutorialState.IsRunning)
                return;

            marketplacePanel.SetActive(false);
            sellPanel.SetActive(true);
            RefreshSellSelection();
        }

        private void PreviousSellItem()
        {
            sellItemIndex--;
            if (sellItemIndex < 0)
                sellItemIndex = SocialMarketplaceCatalog.TradableItems.Length - 1;
            RefreshSellSelection();
        }

        private void NextSellItem()
        {
            sellItemIndex++;
            if (sellItemIndex >= SocialMarketplaceCatalog.TradableItems.Length)
                sellItemIndex = 0;
            RefreshSellSelection();
        }

        private void RefreshSellSelection()
        {
            InventoryItemType item = SocialMarketplaceCatalog.TradableItems[sellItemIndex];
            int owned = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetCount(item) : 0;
            int quantity = ParsePositive(sellQuantityInput, 1);
            int price = ParsePositive(sellPriceInput, 0);
            int fee = SocialMarketplaceCatalog.CalculateListingFee(item, quantity, price);
            sellItemText.text = SocialMarketplaceCatalog.FriendlyName(item);
            sellInventoryText.text = "Owned: " + owned + " | Base Value: " +
                                     PesoPrice.Label(SocialMarketplaceCatalog.GetBaseValueCentavos(item)) + " each";
            sellFeeText.text = "Listing fee: P" + fee;
        }

        private void OnCreateListingPressed()
        {
            InventoryItemType item = SocialMarketplaceCatalog.TradableItems[sellItemIndex];
            int quantity = ParsePositive(sellQuantityInput, 0);
            int price = ParsePositive(sellPriceInput, 0);
            if (quantity <= 0)
            {
                sellFeeText.text = "Quantity must be greater than zero.";
                return;
            }
            if (PlayerInventory.Instance == null || PlayerInventory.Instance.GetCount(item) < quantity)
            {
                sellFeeText.text = "You do not have enough of this item.";
                return;
            }
            StartCoroutine(CreateListing(item, quantity, price));
        }

        private IEnumerator CreateListing(InventoryItemType item, int quantity, int price)
        {
            sellFeeText.text = "Saving farm and creating listing...";
            MarketplaceListingDto created = null;
            string error = null;
            yield return controller.CreateListing(
                new CreateListingRequestDto
                {
                    itemType = item.ToString(),
                    quantity = quantity,
                    askingPrice = price
                },
                value => created = value,
                message => error = message);
            if (created == null)
            {
                sellFeeText.text = error;
                yield break;
            }
            sellPanel.SetActive(false);
            marketplacePanel.SetActive(true);
            marketplaceStatusText.text = "Listing created. Fee paid: P" + created.listingFee + ".";
            yield return RefreshMarketplace();
        }

        private IEnumerator BuyListing(MarketplaceListingDto listing)
        {
            marketplaceStatusText.text = "Saving farm and purchasing listing...";
            MarketplacePurchaseDto purchase = null;
            string error = null;
            yield return controller.BuyListing(listing.id,
                value => purchase = value, message => error = message);
            if (purchase == null)
            {
                marketplaceStatusText.text = error;
                yield break;
            }
            GameAudioManager.Instance.PlayReward();
            marketplaceStatusText.text = "Purchase complete. Remaining money: P" + purchase.buyerMoney + ".";
            yield return RefreshMarketplace();
        }

        private IEnumerator CancelOrClaimListing(MarketplaceListingDto listing)
        {
            marketplaceStatusText.text = listing.status == "RETURN_PENDING"
                ? "Claiming returned item..."
                : "Cancelling listing...";
            MarketplaceListingDto result = null;
            string error = null;
            yield return controller.CancelListing(listing.id,
                value => result = value, message => error = message);
            marketplaceStatusText.text = result != null
                ? "Item returned to your inventory."
                : error;
            yield return RefreshMarketplace();
        }

        private static string FriendlyItem(string itemType)
        {
            return PlantingMaterialCatalog.TryParseItem(itemType, out InventoryItemType item)
                ? SocialMarketplaceCatalog.FriendlyName(item)
                : itemType;
        }

        private static string FormatDate(string iso)
        {
            return System.DateTime.TryParse(iso, out System.DateTime value)
                ? value.ToLocalTime().ToString("MMM d, h:mm tt")
                : iso;
        }
    }
}
