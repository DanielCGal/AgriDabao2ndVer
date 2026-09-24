using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace AgriDabao3D
{
    public class SocialMarketplaceApi : MonoBehaviour
    {
        private string Token => AuthSession.Instance != null ? AuthSession.Instance.AccessToken : null;

        public IEnumerator Heartbeat(Action onSuccess = null, Action<string> onError = null,
            Action<string> onBusy = null, Action<string> onSignedOut = null)
        {
            yield return ApiClient.Instance.PostJson<PresenceHeartbeatRequestDto, object>(
                "/api/presence/heartbeat",
                new PresenceHeartbeatRequestDto { deviceId = DeviceIdentity.Current },
                Token,
                _ => onSuccess?.Invoke(),
                (message, code) =>
                {
                    if (code == 409)
                        onBusy?.Invoke(message);
                    else if (code == 401 || code == 403)
                        onSignedOut?.Invoke(message);
                    else
                        onError?.Invoke(message);
                });
        }

        public IEnumerator MarkOffline()
        {
            if (ApiClient.Instance == null || AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
                yield break;

            yield return ApiClient.Instance.PostJson<PresenceHeartbeatRequestDto, object>(
                "/api/presence/offline",
                new PresenceHeartbeatRequestDto { deviceId = DeviceIdentity.Current },
                Token,
                _ => { },
                (_, __) => { });
        }

        public IEnumerator SearchPlayers(string displayName,
            Action<List<PlayerCardDto>> onSuccess, Action<string> onError)
        {
            string query = UnityWebRequest.EscapeURL(displayName ?? "");
            yield return Get("/api/players/search?displayName=" + query, onSuccess, onError);
        }

        public IEnumerator GetPlayer(string playerId,
            Action<PlayerProfileDto> onSuccess, Action<string> onError)
        {
            yield return Get("/api/players/" + playerId, onSuccess, onError);
        }

        public IEnumerator GetFriends(Action<List<PlayerCardDto>> onSuccess, Action<string> onError)
        {
            yield return Get("/api/friends", onSuccess, onError);
        }

        public IEnumerator GetFriendRequests(Action<List<FriendRequestDto>> onSuccess, Action<string> onError)
        {
            yield return Get("/api/friends/requests", onSuccess, onError);
        }

        public IEnumerator SendFriendRequest(string playerId, Action onSuccess, Action<string> onError)
        {
            yield return PostNoBody("/api/friends/requests/" + playerId, onSuccess, onError);
        }

        public IEnumerator AcceptFriendRequest(string requestId, Action onSuccess, Action<string> onError)
        {
            yield return PostNoBody("/api/friends/requests/" + requestId + "/accept", onSuccess, onError);
        }

        public IEnumerator DeclineFriendRequest(string requestId, Action onSuccess, Action<string> onError)
        {
            yield return PostNoBody("/api/friends/requests/" + requestId + "/decline", onSuccess, onError);
        }

        public IEnumerator GetMessages(string playerId, string after,
            Action<List<ChatMessageDto>> onSuccess, Action<string> onError)
        {
            string path = "/api/chat/" + playerId + "/messages";
            if (!string.IsNullOrWhiteSpace(after))
                path += "?after=" + UnityWebRequest.EscapeURL(after);
            yield return Get(path, onSuccess, onError);
        }

        public IEnumerator SendMessage(string playerId, string body,
            Action<ChatMessageDto> onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<SendMessageRequestDto, ChatMessageDto>(
                "/api/chat/" + playerId + "/messages",
                new SendMessageRequestDto { body = body }, Token,
                onSuccess, (message, _) => onError?.Invoke(message));
        }

        public IEnumerator GetUnreadCount(Action<UnreadCountDto> onSuccess, Action<string> onError)
        {
            yield return Get("/api/chat/unread-count", onSuccess, onError);
        }

        public IEnumerator GetListings(string itemType,
            Action<List<MarketplaceListingDto>> onSuccess, Action<string> onError)
        {
            string path = "/api/marketplace/listings";
            if (!string.IsNullOrWhiteSpace(itemType) && itemType != "ALL")
                path += "?itemType=" + UnityWebRequest.EscapeURL(itemType);
            yield return Get(path, onSuccess, onError);
        }

        public IEnumerator GetListingFee(string itemType, int quantity, int askingPrice,
            Action<ListingFeeDto> onSuccess, Action<string> onError)
        {
            string path = "/api/marketplace/fee?itemType=" + UnityWebRequest.EscapeURL(itemType) +
                          "&quantity=" + quantity + "&askingPrice=" + askingPrice;
            yield return Get(path, onSuccess, onError);
        }

        public IEnumerator CreateListing(CreateListingRequestDto request,
            Action<MarketplaceListingDto> onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<CreateListingRequestDto, MarketplaceListingDto>(
                "/api/marketplace/listings", request, Token,
                onSuccess, (message, _) => onError?.Invoke(message));
        }

        public IEnumerator BuyListing(string listingId,
            Action<MarketplacePurchaseDto> onSuccess, Action<string> onError)
        {
            yield return PostEmpty<MarketplacePurchaseDto>(
                "/api/marketplace/listings/" + listingId + "/buy", onSuccess, onError);
        }

        public IEnumerator CancelListing(string listingId,
            Action<MarketplaceListingDto> onSuccess, Action<string> onError)
        {
            yield return PostEmpty<MarketplaceListingDto>(
                "/api/marketplace/listings/" + listingId + "/cancel", onSuccess, onError);
        }

        public IEnumerator GetUnseenSales(
            Action<List<MarketplaceSaleDto>> onSuccess, Action<string> onError)
        {
            yield return Get("/api/marketplace/sales/unseen", onSuccess, onError);
        }

        public IEnumerator AcknowledgeSale(string listingId,
            Action onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.PostWithoutBody(
                "/api/marketplace/sales/" + listingId + "/acknowledge", Token,
                onSuccess, (message, _) => onError?.Invoke(message));
        }

        public IEnumerator GetActiveTrade(Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            ActiveTradeResponseDto wrapper = null;
            string error = null;
            yield return Get<ActiveTradeResponseDto>(
                "/api/trades/active",
                value => wrapper = value,
                message => error = message);
            if (!string.IsNullOrWhiteSpace(error)) onError?.Invoke(error);
            else onSuccess?.Invoke(wrapper != null ? wrapper.trade : null);
        }

        public IEnumerator GetTrade(string tradeId, Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            yield return Get("/api/trades/" + tradeId, onSuccess, onError);
        }

        public IEnumerator GetIncomingTrades(Action<List<TradeViewDto>> onSuccess, Action<string> onError)
        {
            yield return Get("/api/trades/incoming", onSuccess, onError);
        }

        public IEnumerator InviteTrade(string targetPlayerId,
            Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<TradeInviteRequestDto, TradeViewDto>(
                "/api/trades/invite",
                new TradeInviteRequestDto { targetPlayerId = targetPlayerId }, Token,
                onSuccess, (message, _) => onError?.Invoke(message));
        }

        public IEnumerator AcceptTrade(string tradeId, Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            yield return PostEmpty("/api/trades/" + tradeId + "/accept", onSuccess, onError);
        }

        public IEnumerator DeclineTrade(string tradeId, Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            yield return PostEmpty("/api/trades/" + tradeId + "/decline", onSuccess, onError);
        }

        public IEnumerator UpdateTradeOffer(string tradeId, UpdateTradeOfferRequestDto request,
            Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.PutJson<UpdateTradeOfferRequestDto, TradeViewDto>(
                "/api/trades/" + tradeId + "/offer", request, Token,
                onSuccess, (message, _) => onError?.Invoke(message));
        }

        public IEnumerator AgreeTrade(string tradeId, Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            yield return PostEmpty("/api/trades/" + tradeId + "/agree", onSuccess, onError);
        }

        public IEnumerator ConfirmTrade(string tradeId, Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            yield return PostEmpty("/api/trades/" + tradeId + "/confirm", onSuccess, onError);
        }

        public IEnumerator CancelTrade(string tradeId, Action<TradeViewDto> onSuccess, Action<string> onError)
        {
            yield return PostEmpty("/api/trades/" + tradeId + "/cancel", onSuccess, onError);
        }

        private IEnumerator Get<T>(string path, Action<T> onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.GetJson<T>(
                path, Token, onSuccess, (message, _) => onError?.Invoke(message));
        }

        private IEnumerator PostNoBody(string path, Action onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.PostWithoutBody(
                path, Token, onSuccess, (message, _) => onError?.Invoke(message));
        }

        private IEnumerator PostEmpty<T>(string path, Action<T> onSuccess, Action<string> onError)
        {
            yield return ApiClient.Instance.PostJson<EmptyRequestDto, T>(
                path, new EmptyRequestDto(), Token, onSuccess, (message, _) => onError?.Invoke(message));
        }
    }
}
