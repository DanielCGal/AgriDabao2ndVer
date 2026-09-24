using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public partial class SocialMarketplaceUIBuilder : MonoBehaviour
    {
        private Canvas canvas;
        private SocialMarketplaceController controller;

        private Button searchPlayersButton;
        private Text searchPlayersButtonText;
        private Button marketplaceButton;
        private Text marketplaceButtonText;

        private GameObject searchPanel;
        private InputField playerSearchInput;
        private RectTransform playerListContent;
        private Text playerSearchStatus;

        private GameObject profilePanel;
        private Text profileInfoText;
        private Button profileAddFriendButton;
        private Text profileAddFriendText;
        private Image profileAddFriendImage;
        private Button profileTradeButton;
        private Button profileChatButton;
        private PlayerProfileDto selectedProfile;

        private GameObject chatPanel;
        private Text chatTitleText;
        private RectTransform chatContent;
        private InputField chatInput;
        private Text chatStatusText;
        private PlayerProfileDto chatPlayer;
        private Coroutine chatPolling;

        private string chatCursor;
        private float chatPollInterval;

        private GameObject marketplacePanel;
        private Button sellItemsButton;
        private RectTransform marketplaceContent;
        private Text marketplaceFilterText;
        private Text marketplaceStatusText;
        private int marketplaceFilterIndex = -1;

        private GameObject sellPanel;
        private Text sellItemText;
        private Text sellInventoryText;
        private Text sellFeeText;
        private InputField sellQuantityInput;
        private InputField sellPriceInput;
        private int sellItemIndex;

        private GameObject tradePanel;
        private Text tradeTitleText;
        private Text tradeSummaryText;
        private Text tradeStatusText;
        private Text tradeItemText;
        private Text tradePendingOfferText;
        private InputField tradeQuantityInput;
        private InputField tradeMoneyInput;
        private int tradeItemIndex;
        private readonly Dictionary<InventoryItemType, int> pendingTradeItems =
            new Dictionary<InventoryItemType, int>();
        private TradeViewDto displayedTrade;

        private GameObject tradeConfirmPanel;
        private Text tradeConfirmSummary;

        private GameObject saleNotifyPanel;
        private Text saleNotifyText;
        private readonly Queue<MarketplaceSaleDto> pendingSaleNotifications =
            new Queue<MarketplaceSaleDto>();

        private IEnumerator Start()
        {
            EnsureEventSystem();
            for (int i = 0; i < 180; i++)
            {
                canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                controller = SocialMarketplaceController.Instance;
                if (canvas != null && controller != null)
                    break;
                yield return null;
            }

            if (canvas == null || controller == null)
            {
                Debug.LogError("SocialMarketplaceUIBuilder: Canvas or controller was not found.");
                yield break;
            }

            BuildTopButtons();
            BuildPlayerSearchPanel();
            BuildProfilePanel();
            BuildChatPanel();
            BuildMarketplacePanel();
            BuildSellPanel();
            BuildTradePanel();
            BuildTradeConfirmationPanel();
            BuildTradeRequestPopup();
            BuildSaleNotificationPanel();

            controller.NotificationStateChanged += RefreshTopButtonLabels;
            controller.ActiveTradeChanged += OnActiveTradeChanged;
            controller.SaleNotified += OnSaleNotified;
            RefreshTopButtonLabels();
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.NotificationStateChanged -= RefreshTopButtonLabels;
                controller.ActiveTradeChanged -= OnActiveTradeChanged;
                controller.SaleNotified -= OnSaleNotified;
            }
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
                return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void BuildTopButtons()
        {
            searchPlayersButton = HudIconButton.Create(
                canvas.transform, "SearchPlayersButton",
                UIThemeSprites.Instance?.searchPlayersButton,
                HudIconButton.SlotSearchPlayers,
                "Search Players", ToggleSearchPanel,
                out _, out searchPlayersButtonText);

            marketplaceButton = HudIconButton.Create(
                canvas.transform, "PlayerMarketplaceButton",
                UIThemeSprites.Instance?.marketplaceButton,
                HudIconButton.SlotMarketplace,
                "Marketplace", ToggleMarketplacePanel,
                out _, out marketplaceButtonText);

            searchBadgeRoot = CreateNotificationBadge(searchPlayersButton, out searchBadgeText);
        }

        private GameObject searchBadgeRoot;
        private Text searchBadgeText;

        private GameObject CreateNotificationBadge(Button host, out Text countText)
        {
            countText = null;
            if (host == null)
                return null;

            GameObject go = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(host.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(34f, 34f);
            rect.anchoredPosition = new Vector2(-6f, -6f);

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.80f, 0.14f, 0.12f, 1f);
            bg.raycastTarget = false;

            GameObject textGo = new GameObject("Count", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            countText = textGo.GetComponent<Text>();
            countText.font = GameFonts.Primary;
            countText.fontSize = 20;
            countText.fontStyle = FontStyle.Bold;
            countText.alignment = TextAnchor.MiddleCenter;
            countText.color = Color.white;
            countText.raycastTarget = false;

            go.SetActive(false);
            return go;
        }

        private void RefreshTopButtonLabels()
        {
            int pending = controller.PendingFriendRequests;
            long unread = controller.UnreadMessages;
            bool trade = controller.ActiveTrade != null && controller.ActiveTrade.status == "PENDING";
            long total = Mathf.Max(0, pending) + System.Math.Max(0L, unread) + (trade ? 1L : 0L);

            if (searchBadgeRoot != null)
            {
                searchBadgeRoot.SetActive(total > 0);
                if (searchBadgeText != null)
                    searchBadgeText.text = total > 99 ? "99+" : total.ToString();
            }

            if (searchPlayersButtonText != null)
            {
                string badge = pending > 0 ? " [" + pending + " request]" : "";
                if (unread > 0)
                    badge += " [" + unread + " chat]";
                if (trade)
                    badge += " [trade]";
                searchPlayersButtonText.text = "Search Players" + badge;
            }
        }

        private void ToggleSearchPanel()
        {
            bool open = !searchPanel.activeSelf;
            HideMainPanels();
            if (open)
                HudRegistry.CloseOtherPanels(HudPiece.SearchPlayersPanel);

            searchPanel.SetActive(open);
            if (open)
            {
                searchPanel.transform.SetAsLastSibling();
                StartCoroutine(LoadFriendsAndRequests());
            }
        }

        private void ToggleMarketplacePanel()
        {
            bool open = !marketplacePanel.activeSelf;
            HideMainPanels();
            if (open)
                HudRegistry.CloseOtherPanels(HudPiece.MarketplacePanel);

            marketplacePanel.SetActive(open);
            if (open)
            {
                marketplacePanel.transform.SetAsLastSibling();

                if (sellItemsButton != null)
                    sellItemsButton.interactable = !TutorialState.IsRunning;

                StartCoroutine(RefreshMarketplace());
            }
        }

        private void HideMainPanels()
        {
            searchPanel?.SetActive(false);
            profilePanel?.SetActive(false);
            marketplacePanel?.SetActive(false);
            sellPanel?.SetActive(false);
            tradePanel?.SetActive(false);
            tradeConfirmPanel?.SetActive(false);
            CloseChatPanel();
        }

        private UIThemeSprites Theme => UIThemeSprites.Instance;

        private GameObject CreatePanel(string name, Vector2 size, float alpha = 0.92f,
            Sprite boardOverride = null)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            Sprite board = boardOverride != null ? boardOverride : Theme?.socialBoard;
            if (board != null)
            {
                image.sprite = board;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.04f, 0.10f, 0.16f, alpha);
            }

            panel.transform.SetAsLastSibling();
            return panel;
        }

        private void CreatePanelTitle(Transform parent, string fallbackText, Sprite art,
            float width = 620f, float height = 130f, float offsetY = 6f)
        {
            if (art != null)
            {
                GameObject go = new GameObject("TitleSign", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = new Vector2(0f, offsetY);

                Image image = go.GetComponent<Image>();
                image.sprite = art;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return;
            }

            Text title = CreateText(parent, "Title", 32, TextAnchor.MiddleCenter,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(20f, -70f), new Vector2(-20f, -10f));
            title.text = fallbackText;
        }

        private Text CreateText(Transform parent, string name, int size,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Button CreateButton(Transform parent, string label, Vector2 anchor,
            Vector2 size, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick,
            out Text labelText, Color? color = null, Sprite art = null)
        {
            GameObject go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            if (art == null)
                art = Theme?.socialButton;

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);

                labelText = CreateText(go.transform, "Text", 21, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                labelText.text = "";
                labelText.raycastTarget = false;
                return button;
            }

            image.color = color ?? new Color(0.10f, 0.55f, 0.24f, 0.98f);
            labelText = CreateText(go.transform, "Text", 21, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            labelText.text = label;
            labelText.raycastTarget = false;
            return button;
        }

        private InputField CreateInput(Transform parent, string placeholder,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            bool numeric = false)
        {
            GameObject go = new GameObject("Input_" + placeholder,
                typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);

            InputField input = go.GetComponent<InputField>();
            input.contentType = numeric ? InputField.ContentType.IntegerNumber : InputField.ContentType.Standard;

            Text placeholderText = CreateText(go.transform, "Placeholder", 20,
                TextAnchor.MiddleLeft, Vector2.zero, Vector2.one,
                new Vector2(12f, 4f), new Vector2(-12f, -4f));
            placeholderText.text = placeholder;
            placeholderText.color = new Color(0f, 0f, 0f, 0.40f);

            Text valueText = CreateText(go.transform, "Text", 20,
                TextAnchor.MiddleLeft, Vector2.zero, Vector2.one,
                new Vector2(12f, 4f), new Vector2(-12f, -4f));
            valueText.color = Color.black;
            valueText.text = "";

            placeholderText.verticalOverflow = VerticalWrapMode.Overflow;
            valueText.verticalOverflow = VerticalWrapMode.Overflow;
            input.placeholder = placeholderText;
            input.textComponent = valueText;
            return input;
        }

        private RectTransform CreateScrollContent(Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject scrollGo = new GameObject("ScrollView",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            RectTransform scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = anchorMin;
            scrollRectTransform.anchorMax = anchorMax;
            scrollRectTransform.offsetMin = offsetMin;
            scrollRectTransform.offsetMax = offsetMax;
            scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewport = viewportGo.GetComponent<RectTransform>();
            Stretch(viewport);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;
            return content;
        }

        private GameObject CreateListRow(RectTransform content, float height = 96f)
        {
            GameObject row = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(content, false);

            Image image = row.GetComponent<Image>();
            Sprite rowArt = Theme?.socialListRow;
            if (rowArt != null)
            {
                image.sprite = rowArt;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(1f, 1f, 1f, 0.08f);
            }

            row.GetComponent<LayoutElement>().preferredHeight = height;
            row.GetComponent<LayoutElement>().minHeight = height;
            return row;
        }

        private static void ClearContent(RectTransform content)
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        private static int ParsePositive(InputField input, int fallback = 0)
        {
            return input != null && int.TryParse(input.text, out int value)
                ? Mathf.Max(0, value)
                : fallback;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static string CurrentUserId
        {
            get
            {
                return AuthSession.Instance != null && AuthSession.Instance.CurrentUser != null
                    ? AuthSession.Instance.CurrentUser.id
                    : "";
            }
        }
    }
}
