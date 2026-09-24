using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public class SocialMarketplaceController : MonoBehaviour
    {
        public static SocialMarketplaceController Instance { get; private set; }

        public SocialMarketplaceApi Api { get; private set; }
        public int PendingFriendRequests { get; private set; }
        public long UnreadMessages { get; private set; }
        public TradeViewDto ActiveTrade { get; private set; }

        public event Action NotificationStateChanged;
        public event Action<TradeViewDto> ActiveTradeChanged;

        public event Action<MarketplaceSaleDto> SaleNotified;

        [Header("Polling")]
        [Tooltip("Seconds between notification/trade checks during normal play. Higher = less mobile battery use.")]
        public float notificationIntervalSeconds = 15f;

        [Tooltip("Faster interval used only while a trade is in progress, so trade updates stay responsive.")]
        public float activeTradeIntervalSeconds = 5f;

        private Coroutine heartbeatLoop;
        private Coroutine notificationLoop;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Api = GetComponent<SocialMarketplaceApi>();
            if (Api == null)
                Api = gameObject.AddComponent<SocialMarketplaceApi>();
        }

        private void Start()
        {
            heartbeatLoop = StartCoroutine(HeartbeatLoop());
            notificationLoop = StartCoroutine(NotificationLoop());
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused && isActiveAndEnabled)
                StartCoroutine(Api.Heartbeat(onBusy: SessionTakeover.Raise));
        }

        private void OnApplicationQuit()
        {
            if (isActiveAndEnabled)
                StartCoroutine(Api.MarkOffline());
        }

        private IEnumerator HeartbeatLoop()
        {
            while (true)
            {
                if (IsReady())
                {
                    string takenOver = null;
                    string signedOut = null;
                    yield return Api.Heartbeat(
                        onBusy: message => takenOver = message,
                        onSignedOut: message => signedOut = message);

                    if (takenOver != null)
                    {
                        SessionTakeover.Raise(takenOver);
                        yield break;
                    }

                    if (signedOut != null)
                    {
                        Debug.LogWarning("[Session] Heartbeat refused: " + signedOut);
                        SessionTakeover.RaiseSignedOut(
                            "You have been signed out. This account's password may have been " +
                            "changed on another device, or the account may no longer exist. Please sign in again.");
                        yield break;
                    }
                }

                yield return new WaitForSecondsRealtime(15f);
            }
        }

        private IEnumerator NotificationLoop()
        {
            while (true)
            {
                if (IsReady())
                {
                    if (serverEconomyMergePending)
                        yield return MergeServerEconomy();

                    yield return RefreshNotifications();
                    yield return RefreshActiveTrade();
                    yield return RefreshUnseenSales();
                    yield return RefreshAdminCommands();
                }

                yield return new WaitForSecondsRealtime(
                    ActiveTrade != null ? activeTradeIntervalSeconds : notificationIntervalSeconds);
            }
        }

        public IEnumerator RefreshNotifications()
        {
            List<FriendRequestDto> requests = null;
            UnreadCountDto unread = null;

            yield return Api.GetFriendRequests(value => requests = value, _ => { });
            yield return Api.GetUnreadCount(value => unread = value, _ => { });

            int newRequestCount = requests != null ? requests.Count : 0;
            long newUnreadCount = unread != null ? unread.count : 0;
            if (newRequestCount != PendingFriendRequests || newUnreadCount != UnreadMessages)
            {
                PendingFriendRequests = newRequestCount;
                UnreadMessages = newUnreadCount;
                NotificationStateChanged?.Invoke();
            }
        }

        public IEnumerator RefreshUnseenSales()
        {
            List<MarketplaceSaleDto> sales = null;
            yield return Api.GetUnseenSales(value => sales = value, _ => { });

            if (sales == null || sales.Count == 0)
                yield break;

            foreach (MarketplaceSaleDto sale in sales)
            {
                if (sale == null || string.IsNullOrWhiteSpace(sale.listingId))
                    continue;

                SaleNotified?.Invoke(sale);

                yield return Api.AcknowledgeSale(sale.listingId, () => { }, _ => { });
            }

            yield return MergeServerEconomy();
        }

        public IEnumerator RefreshAdminCommands()
        {
            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
                yield break;

            List<AdminCommandDto> commands = null;
            string error = null;
            yield return AuthSession.Instance.CollectAdminCommands(
                value => commands = value,
                message => error = message);

            if (error != null)
            {
                Debug.LogWarning("[Admin] Could not collect commands: " + error);
                yield break;
            }

            if (commands == null || commands.Count == 0)
                yield break;

            Debug.Log("[Admin] Collected " + commands.Count + " command(s) from the server.");

            foreach (AdminCommandDto command in commands)
            {
                if (command != null)
                    ApplyAdminCommand(command);
            }
        }

        private void ApplyAdminCommand(AdminCommandDto command)
        {
            switch (command.commandType)
            {
                case "GRANT_MONEY":
                    int granted = command.amount.GetValueOrDefault();
                    if (PlayerInventory.Instance != null && granted > 0)
                    {
                        PlayerInventory.Instance.AddMoney(granted);
                        Debug.Log("[Admin] Received P" + granted + " from a developer.");
                    }
                    break;

                case "FORCE_WEATHER":
                    if (WeatherSystem.Instance != null &&
                        Enum.TryParse(command.payload, true, out WeatherEventType weather))
                    {
                        int days = Mathf.Max(1, command.durationDays.GetValueOrDefault(1));
                        WeatherSystem.Instance.ForceWeatherEvent(weather, days);
                        Debug.Log("[Admin] Weather forced to " + weather +
                                  " for " + days + " day(s).");
                    }
                    break;

                case "PASS_DAYS":
                    if (GameTimeSystem.Instance != null && command.amount.GetValueOrDefault() > 0)
                    {
                        int days = command.amount.GetValueOrDefault();
                        GameTimeSystem.Instance.AdvanceDays(days);
                        Debug.Log("[Admin] Clock advanced " + days + " day(s) by a developer.");
                    }
                    break;

                case "PASS_HOURS":
                    if (GameTimeSystem.Instance != null && command.amount.GetValueOrDefault() > 0)
                    {
                        int hours = command.amount.GetValueOrDefault();
                        GameTimeSystem.Instance.AdvanceHours(hours);
                        Debug.Log("[Admin] Clock advanced " + hours + " hour(s) by a developer.");
                    }
                    break;

                case "FORCE_PEST_DISEASE":
                    if (PestDiseaseSystem.Instance != null &&
                        Enum.TryParse(command.payload, true, out PestDiseaseType pest) &&
                        pest != PestDiseaseType.None)
                    {
                        PestDiseaseSystem.Instance.ForceEvent(pest);
                        Debug.Log("[Admin] Outbreak forced: " + pest + ".");
                    }
                    break;

                default:
                    Debug.LogWarning("[Admin] Ignored unknown command " + command.commandType + ".");
                    break;
            }
        }

        public IEnumerator RefreshActiveTrade()
        {
            TradeViewDto trade = null;
            bool requestFailed = false;
            yield return Api.GetActiveTrade(value => trade = value, _ => requestFailed = true);

            if (requestFailed)
                yield break;

            TradeViewDto previous = ActiveTrade;

            if (trade == null && previous != null && IsOpen(previous))
            {
                TradeViewDto ended = null;
                yield return Api.GetTrade(previous.id, value => ended = value, _ => { });

                bool completed = ended != null && ended.status == "COMPLETED";

                if (completed || ended == null)
                    yield return MergeServerEconomy();

                if (completed)
                {
                    ActiveTrade = ended;
                    ActiveTradeChanged?.Invoke(ended);
                    previous = ended;
                }
            }

            bool changed = HasTradeChanged(previous, trade);

            bool justCompleted =
                trade != null &&
                trade.status == "COMPLETED" &&
                (previous == null || previous.status != "COMPLETED");

            ActiveTrade = trade;

            if (justCompleted)
                yield return MergeServerEconomy();

            if (changed)
                ActiveTradeChanged?.Invoke(ActiveTrade);
        }

        private static bool IsOpen(TradeViewDto trade)
        {
            return trade.status == "PENDING" || trade.status == "ACTIVE";
        }

        private bool serverEconomyMergePending;

        private IEnumerator MergeServerEconomy()
        {
            serverEconomyMergePending = true;

            if (!IsReady() || PlayerInventory.Instance == null)
                yield break;

            InventorySaveDto knownBase = EconomyRevisionMerge.Clone(
                AuthSession.Instance.LoadedFarm != null &&
                AuthSession.Instance.LoadedFarm.snapshot != null
                    ? AuthSession.Instance.LoadedFarm.snapshot.inventory
                    : null);
            InventorySaveDto localCurrent = PlayerInventory.Instance.CaptureSaveData();

            FarmSaveResponseDto refreshed = null;
            string refreshError = null;
            yield return AuthSession.Instance.RefreshFarm(
                value => refreshed = value,
                (message, _) => refreshError = message);

            if (refreshed == null || refreshed.snapshot == null)
            {
                Debug.LogWarning("[Economy] Could not fetch the server farm to merge: " + refreshError);
                yield break;
            }

            if (EconomyRevisionMerge.TryMerge(
                    knownBase,
                    localCurrent,
                    refreshed.snapshot.inventory,
                    out InventorySaveDto merged,
                    out string mergeError))
            {
                PlayerInventory.Instance.RestoreSaveData(merged);
            }
            else
            {
                Debug.LogWarning("[Economy] " + mergeError +
                                 " Using the server's backpack instead.");
                PlayerInventory.Instance.RestoreSaveData(refreshed.snapshot.inventory);
            }

            serverEconomyMergePending = false;
        }

        private static bool HasTradeChanged(TradeViewDto a, TradeViewDto b)
        {
            if (a == null && b == null)
                return false;
            if (a == null || b == null)
                return true;

            return a.id != b.id ||
                   a.status != b.status ||
                   a.requesterAgreed != b.requesterAgreed ||
                   a.targetAgreed != b.targetAgreed ||
                   a.requesterConfirmed != b.requesterConfirmed ||
                   a.targetConfirmed != b.targetConfirmed ||
                   a.requesterOnline != b.requesterOnline ||
                   a.targetOnline != b.targetOnline ||
                   HasOfferChanged(a.requesterOffer, b.requesterOffer) ||
                   HasOfferChanged(a.targetOffer, b.targetOffer);
        }

        private static bool HasOfferChanged(TradeOfferDto a, TradeOfferDto b)
        {
            if (a == null && b == null)
                return false;
            if (a == null || b == null)
                return true;
            if (a.money != b.money)
                return true;

            int countA = a.items != null ? a.items.Count : 0;
            int countB = b.items != null ? b.items.Count : 0;
            if (countA != countB)
                return true;

            for (int i = 0; i < countA; i++)
            {
                TradeItemDto itemA = a.items[i];
                TradeItemDto itemB = b.items[i];

                if (itemA == null || itemB == null)
                {
                    if (itemA != itemB)
                        return true;
                    continue;
                }

                if (itemA.itemType != itemB.itemType || itemA.quantity != itemB.quantity)
                    return true;
            }

            return false;
        }

        public IEnumerator SaveBeforeEconomy(Action onReady, Action<string> onError)
        {
            if (!IsReady())
            {
                onError?.Invoke("Log in and load a saved farm before using the marketplace or trading.");
                yield break;
            }

            FarmPersistenceManager persistence = FarmPersistenceManager.Instance;
            if (persistence == null || !persistence.IsWorldReady)
            {
                onError?.Invoke("The farm is still loading. Try again after the world is ready.");
                yield break;
            }

            InventorySaveDto knownBase = EconomyRevisionMerge.Clone(
                AuthSession.Instance.LoadedFarm != null &&
                AuthSession.Instance.LoadedFarm.snapshot != null
                    ? AuthSession.Instance.LoadedFarm.snapshot.inventory
                    : null);
            InventorySaveDto localCurrent = PlayerInventory.Instance != null
                ? PlayerInventory.Instance.CaptureSaveData()
                : null;

            yield return persistence.SaveFarm(allowDuringTutorial: true);
            while (persistence.IsSaving)
                yield return null;

            if (persistence.LastStatus == "Farm saved successfully.")
            {
                onReady?.Invoke();
                yield break;
            }

            bool revisionConflict = !string.IsNullOrWhiteSpace(persistence.LastStatus) &&
                                    persistence.LastStatus.StartsWith("This farm was changed");
            if (!revisionConflict || PlayerInventory.Instance == null)
            {
                onError?.Invoke(string.IsNullOrWhiteSpace(persistence.LastStatus)
                    ? "The farm could not be saved before the economy action."
                    : persistence.LastStatus);
                yield break;
            }

            FarmSaveResponseDto refreshed = null;
            string refreshError = null;
            yield return AuthSession.Instance.RefreshFarm(
                value => refreshed = value,
                (message, _) => refreshError = message);
            if (refreshed == null || refreshed.snapshot == null)
            {
                onError?.Invoke(string.IsNullOrWhiteSpace(refreshError)
                    ? "The server farm changed and could not be refreshed."
                    : refreshError);
                yield break;
            }

            if (!EconomyRevisionMerge.TryMerge(
                    knownBase,
                    localCurrent,
                    refreshed.snapshot.inventory,
                    out InventorySaveDto merged,
                    out string mergeError))
            {
                onError?.Invoke(mergeError + " Reload the farm before continuing.");
                yield break;
            }

            PlayerInventory.Instance.RestoreSaveData(merged);
            yield return persistence.SaveFarm(allowDuringTutorial: true);
            while (persistence.IsSaving)
                yield return null;

            if (persistence.LastStatus == "Farm saved successfully.")
                onReady?.Invoke();
            else
                onError?.Invoke(persistence.LastStatus);
        }

        public IEnumerator SyncRemoteEconomyPreservingLocalChanges(
            Action onSuccess = null,
            Action<string> onError = null)
        {
            if (!IsReady() || PlayerInventory.Instance == null)
            {
                onError?.Invoke("The player economy is not ready.");
                yield break;
            }

            InventorySaveDto knownBase = EconomyRevisionMerge.Clone(
                AuthSession.Instance.LoadedFarm != null &&
                AuthSession.Instance.LoadedFarm.snapshot != null
                    ? AuthSession.Instance.LoadedFarm.snapshot.inventory
                    : null);
            InventorySaveDto localCurrent = PlayerInventory.Instance.CaptureSaveData();

            FarmSaveResponseDto refreshed = null;
            string error = null;
            yield return AuthSession.Instance.RefreshFarm(
                value => refreshed = value,
                (message, _) => error = message);
            if (refreshed == null || refreshed.snapshot == null)
            {
                onError?.Invoke(string.IsNullOrWhiteSpace(error)
                    ? "Could not synchronize the remote economy."
                    : error);
                yield break;
            }

            if (!EconomyRevisionMerge.TryMerge(
                    knownBase,
                    localCurrent,
                    refreshed.snapshot.inventory,
                    out InventorySaveDto merged,
                    out string mergeError))
            {
                onError?.Invoke(mergeError);
                yield break;
            }

            PlayerInventory.Instance.RestoreSaveData(merged);
            onSuccess?.Invoke();
        }

        public IEnumerator RefreshFarmInventory(Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsReady())
            {
                onError?.Invoke("The player session is not ready.");
                yield break;
            }

            FarmSaveResponseDto response = null;
            string error = null;
            yield return AuthSession.Instance.RefreshFarm(
                value => response = value,
                (message, _) => error = message);

            if (response == null)
            {
                onError?.Invoke(string.IsNullOrWhiteSpace(error)
                    ? "Could not refresh the server farm economy."
                    : error);
                yield break;
            }

            if (PlayerInventory.Instance != null && response.snapshot != null)
                PlayerInventory.Instance.RestoreSaveData(response.snapshot.inventory);

            onSuccess?.Invoke();
        }

        public IEnumerator CreateListing(CreateListingRequestDto request,
            Action<MarketplaceListingDto> onSuccess, Action<string> onError)
        {
            bool ready = false;
            string saveError = null;
            yield return SaveBeforeEconomy(() => ready = true, message => saveError = message);
            if (!ready)
            {
                onError?.Invoke(saveError);
                yield break;
            }

            MarketplaceListingDto listing = null;
            string error = null;
            yield return Api.CreateListing(request, value => listing = value, message => error = message);
            if (listing == null)
            {
                onError?.Invoke(error);
                yield break;
            }

            yield return RefreshFarmInventory(null, message => error = message);
            if (!string.IsNullOrWhiteSpace(error))
            {
                onError?.Invoke(error);
                yield break;
            }
            onSuccess?.Invoke(listing);
        }

        public IEnumerator BuyListing(string listingId,
            Action<MarketplacePurchaseDto> onSuccess, Action<string> onError)
        {
            bool ready = false;
            string saveError = null;
            yield return SaveBeforeEconomy(() => ready = true, message => saveError = message);
            if (!ready)
            {
                onError?.Invoke(saveError);
                yield break;
            }

            MarketplacePurchaseDto purchase = null;
            string error = null;
            yield return Api.BuyListing(listingId, value => purchase = value, message => error = message);
            if (purchase == null)
            {
                onError?.Invoke(error);
                yield break;
            }
            yield return RefreshFarmInventory(null, message => error = message);
            if (!string.IsNullOrWhiteSpace(error))
            {
                onError?.Invoke(error);
                yield break;
            }
            onSuccess?.Invoke(purchase);
        }

        public IEnumerator CancelListing(string listingId,
            Action<MarketplaceListingDto> onSuccess, Action<string> onError)
        {
            bool ready = false;
            string saveError = null;
            yield return SaveBeforeEconomy(() => ready = true, message => saveError = message);
            if (!ready)
            {
                onError?.Invoke(saveError);
                yield break;
            }

            MarketplaceListingDto listing = null;
            string error = null;
            yield return Api.CancelListing(listingId, value => listing = value, message => error = message);
            if (listing == null)
            {
                onError?.Invoke(error);
                yield break;
            }
            yield return RefreshFarmInventory(null, message => error = message);
            if (!string.IsNullOrWhiteSpace(error))
            {
                onError?.Invoke(error);
                yield break;
            }
            onSuccess?.Invoke(listing);
        }

        public IEnumerator InviteTrade(string targetPlayerId,
            Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            bool ready = false;
            string saveError = null;
            yield return SaveBeforeEconomy(() => ready = true, message => saveError = message);
            if (!ready)
            {
                onError?.Invoke(saveError);
                yield break;
            }

            yield return Api.InviteTrade(targetPlayerId,
                trade =>
                {
                    ActiveTrade = trade;
                    ActiveTradeChanged?.Invoke(trade);
                    onSuccess?.Invoke(trade);
                }, onError);
        }

        public IEnumerator UpdateTradeOffer(string tradeId, UpdateTradeOfferRequestDto request,
            Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            bool ready = false;
            string saveError = null;
            yield return SaveBeforeEconomy(() => ready = true, message => saveError = message);
            if (!ready)
            {
                onError?.Invoke(saveError);
                yield break;
            }
            yield return Api.UpdateTradeOffer(tradeId, request,
                value => SetTrade(value, onSuccess), onError);
        }

        public IEnumerator UpdateTradeOfferLive(string tradeId, UpdateTradeOfferRequestDto request,
            Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            yield return Api.UpdateTradeOffer(tradeId, request,
                value => SetTrade(value, onSuccess), onError);
        }

        public IEnumerator PrepareTradeSession(Action onReady, Action<string> onError)
        {
            yield return SaveBeforeEconomy(onReady, onError);
        }

        public IEnumerator AgreeTrade(string tradeId,
            Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            bool ready = false;
            string saveError = null;
            yield return SaveBeforeEconomy(() => ready = true, message => saveError = message);
            if (!ready)
            {
                onError?.Invoke(saveError);
                yield break;
            }
            yield return Api.AgreeTrade(tradeId, value => SetTrade(value, onSuccess), onError);
        }

        public IEnumerator ConfirmTrade(string tradeId,
            Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            bool ready = false;
            string saveError = null;
            yield return SaveBeforeEconomy(() => ready = true, message => saveError = message);
            if (!ready)
            {
                onError?.Invoke(saveError);
                yield break;
            }

            TradeViewDto result = null;
            string error = null;
            yield return Api.ConfirmTrade(tradeId, value => result = value, message => error = message);
            if (result == null)
            {
                onError?.Invoke(error);
                yield break;
            }

            SetTrade(result, null);

            if (result.status == "COMPLETED")
                yield return MergeServerEconomy();

            onSuccess?.Invoke(result);
        }

        private void SetTrade(TradeViewDto trade, Action<TradeViewDto> callback)
        {
            ActiveTrade = trade;
            ActiveTradeChanged?.Invoke(trade);
            callback?.Invoke(trade);
        }

        private static bool IsReady()
        {
            return AuthSession.Instance != null &&
                   AuthSession.Instance.IsAuthenticated &&
                   ApiClient.Instance != null;
        }
    }
}
