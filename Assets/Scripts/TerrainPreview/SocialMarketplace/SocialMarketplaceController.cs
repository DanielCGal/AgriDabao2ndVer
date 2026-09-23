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

        /// <summary>Raised when one of this player's listings has been bought.</summary>
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
            // Coming back from the background is the likeliest moment to discover a
            // takeover: heartbeats stopped while paused, which is exactly what lets
            // another device claim the account.
            if (!paused && isActiveAndEnabled)
                StartCoroutine(Api.Heartbeat(onBusy: SessionTakeover.Raise));
        }

        private void OnApplicationQuit()
        {
            // The backend also treats a player as offline after 45 seconds without a heartbeat.
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
                        // Another device holds the account. Leave the farm rather
                        // than play on beside it: this device can no longer save,
                        // so every minute spent here is a minute thrown away.
                        SessionTakeover.Raise(takenOver);
                        yield break;
                    }

                    if (signedOut != null)
                    {
                        // The account's password was changed somewhere else, which
                        // ends every session it had. Same reasoning as above - this
                        // device cannot save any more - but permanent, so the saved
                        // login goes with it and the player lands on the sign-in form.
                        //
                        // The wording is ours, not the server's: a rejected token
                        // comes back as a bare 401 with the reason in a header the
                        // client does not read, so quoting it would put "HTTP/1.1
                        // 401 Unauthorized" on a wooden plank in front of a player.
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

                // A live trade needs quick updates; the rest of the time poll
                // slowly so the radio is not woken every few seconds on mobile.
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

        /// <summary>
        /// Looks for listings of this player's that someone bought while they were
        /// elsewhere, raises one notification each, and refreshes the wallet so the
        /// sale money is visible immediately rather than after the next reload.
        /// </summary>
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

                // Acknowledged only after the event is raised, so a failure here
                // means the player sees it again rather than never seeing it.
                yield return Api.AcknowledgeSale(sale.listingId, () => { }, _ => { });
            }

            // Merged, not copied: the sale only added money on the server, and a
            // straight copy of the server's backpack also undid everything the seller
            // had harvested, bought or used since their last save.
            yield return MergeServerEconomy();
        }

        /// <summary>
        /// Applies anything a developer has sent this player - a weather event,
        /// an outbreak, or money.
        ///
        /// Applied here on the client rather than written into the stored farm on
        /// the server, because while a farm is being played this game holds the
        /// authoritative copy of it: a change made to the saved snapshot would be
        /// overwritten by this client's next save. The weather and pest systems
        /// are client-side simulations in any case, so there is nothing on the
        /// server that could start a typhoon.
        ///
        /// Errors are swallowed. The endpoint is a developer convenience and a
        /// failed poll must never interrupt someone's game.
        /// </summary>
        public IEnumerator RefreshAdminCommands()
        {
            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
                yield break;

            List<AdminCommandDto> commands = null;
            string error = null;
            yield return AuthSession.Instance.CollectAdminCommands(
                value => commands = value,
                message => error = message);

            // Logged rather than swallowed. This used to fail silently, which
            // meant a command that never arrived left no trace anywhere - no way
            // to tell a refused request from an empty queue from a build that
            // predates this code. The poll still never interrupts play; it just
            // says so in the console now.
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
                        // AddMoney raises OnInventoryChanged, which is what the
                        // money plank listens to - so the wallet updates on screen
                        // without anything here having to touch the HUD.
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
                    // A newer server sending a command this build predates. Ignored
                    // rather than logged as an error: it is already marked delivered,
                    // so it will not come back and nothing is stuck.
                    Debug.LogWarning("[Admin] Ignored unknown command " + command.commandType + ".");
                    break;
            }
        }

        public IEnumerator RefreshActiveTrade()
        {
            TradeViewDto trade = null;
            bool requestFailed = false;
            yield return Api.GetActiveTrade(value => trade = value, _ => requestFailed = true);

            // A poll that never reached the server is not the same as "there is no
            // trade" - both used to leave `trade` null. Since the trade screen now
            // closes itself when the session disappears, treating a dropped packet
            // as a vanished trade would eject a player mid-negotiation. Keep the
            // last known state and try again on the next tick instead.
            if (requestFailed)
                yield break;

            TradeViewDto previous = ActiveTrade;

            // The player who confirms FIRST gets a response that is not yet
            // COMPLETED, so ConfirmTrade's own refresh never runs for them - the
            // server carries out the exchange when the OTHER player confirms. This
            // poll is how they find out, but /api/trades/active only reports PENDING
            // and ACTIVE trades, so the finished trade never came back here as
            // COMPLETED: it simply stopped being listed. Their backpack stayed as it
            // was, and every save they made was refused as a revision conflict,
            // until reopening the trade screen happened to merge the server's copy
            // in. So when an open trade drops out of the list, ask how it ended.
            if (trade == null && previous != null && IsOpen(previous))
            {
                TradeViewDto ended = null;
                yield return Api.GetTrade(previous.id, value => ended = value, _ => { });

                bool completed = ended != null && ended.status == "COMPLETED";

                // An unknown ending - a server without that call, or a dropped
                // request - is merged as well. The merge only applies what changed
                // on the server, so after a cancelled trade it changes nothing.
                if (completed || ended == null)
                    yield return MergeServerEconomy();

                if (completed)
                {
                    // Raised before the trade is cleared below, so the trade board
                    // shows the success message and closes itself, exactly as it
                    // does for the player who confirmed last.
                    ActiveTrade = ended;
                    ActiveTradeChanged?.Invoke(ended);
                    previous = ended;
                }
            }

            bool changed = HasTradeChanged(previous, trade);

            // For a server that does list a trade as COMPLETED here.
            bool justCompleted =
                trade != null &&
                trade.status == "COMPLETED" &&
                (previous == null || previous.status != "COMPLETED");

            ActiveTrade = trade;

            // Merged before the board is told, so it never reports an updated
            // backpack that has not been updated yet.
            if (justCompleted)
                yield return MergeServerEconomy();

            if (changed)
                ActiveTradeChanged?.Invoke(ActiveTrade);
        }

        private static bool IsOpen(TradeViewDto trade)
        {
            return trade.status == "PENDING" || trade.status == "ACTIVE";
        }

        /// <summary>
        /// Set while the server holds a change to this player's farm - a finished
        /// trade, a sold listing - that has not reached the backpack yet. Retried on
        /// every poll until it does, because the change only announces itself once:
        /// a trade that finished while the connection dropped would otherwise never
        /// be brought in, and every save would keep being refused.
        /// </summary>
        private bool serverEconomyMergePending;

        /// <summary>
        /// Brings a change the server made to this player's saved farm into the farm
        /// being played.
        ///
        /// Merged rather than copied over. The server's copy is only as new as the
        /// player's last save, so replacing the backpack with it threw away whatever
        /// they had harvested, bought or used since. The merge adds only what the
        /// server changed on top of what is in the backpack now.
        /// </summary>
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
                // Nothing was replaced, so the next poll can simply try again.
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
                // The farm revision has already moved on to the server's, so leaving
                // the backpack as it is would let the next save write the old items
                // back over the exchange. The server's copy wins instead, at the cost
                // of anything unsaved in the backpack.
                Debug.LogWarning("[Economy] " + mergeError +
                                 " Using the server's backpack instead.");
                PlayerInventory.Instance.RestoreSaveData(refreshed.snapshot.inventory);
            }

            serverEconomyMergePending = false;
        }

        /// <summary>
        /// True when anything the trade UI displays differs, including the offer
        /// contents. Comparing only status/agreed/confirmed meant a player editing
        /// their money or items produced no event, so the other side's panel never
        /// redrew until it was closed and reopened.
        /// </summary>
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

        /// <summary>
        /// Pushes an offer without first re-saving the whole farm.
        ///
        /// The server validates offers against the SAVED farm, so a save has to
        /// happen at least once - but only once, via <see cref="PrepareTradeSession"/>
        /// when the trade opens. Inventory cannot change while the player is sitting
        /// in the trade screen, so repeating a full farm save on every drag would
        /// just make the UI crawl. The exchange itself is re-validated server-side
        /// at confirm time regardless.
        /// </summary>
        public IEnumerator UpdateTradeOfferLive(string tradeId, UpdateTradeOfferRequestDto request,
            Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            yield return Api.UpdateTradeOffer(tradeId, request,
                value => SetTrade(value, onSuccess), onError);
        }

        /// <summary>
        /// Saves the farm once so the server's copy matches local inventory before
        /// live offer edits begin.
        /// </summary>
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

            // The same merge the other player gets from the poll. A failure is not
            // reported as a failed trade - the exchange has already happened on the
            // server - it is retried on the next poll instead.
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
