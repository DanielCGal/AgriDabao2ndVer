using System;
using System.Collections.Generic;

namespace AgriDabao3D
{
    [Serializable]
    public class EmptyRequestDto
    {
    }

    [Serializable]
    public class PlayerCardDto
    {
        public string id;
        public string displayName;
        public string districtName;
        public bool online;
        public string relationship;
    }

    [Serializable]
    public class PlayerProfileDto
    {
        public string id;
        public string displayName;
        public string districtName;
        public int money;
        public bool online;
        public string relationship;
    }

    [Serializable]
    public class FriendRequestDto
    {
        public string requestId;
        public string playerId;
        public string displayName;
        public string districtName;
        public bool online;
        public string createdAt;
    }

    [Serializable]
    public class SendMessageRequestDto
    {
        public string body;
    }

    [Serializable]
    public class ChatMessageDto
    {
        public string id;
        public string senderId;
        public string receiverId;
        public string body;
        public string sentAt;
        public string readAt;
    }

    [Serializable]
    public class UnreadCountDto
    {
        public long count;
    }

    [Serializable]
    public class CreateListingRequestDto
    {
        public string itemType;
        public int quantity;
        public int askingPrice;
    }

    [Serializable]
    public class ListingFeeDto
    {
        public int itemBaseValue;
        public int itemValueTotal;
        public int askingPrice;
        public int listingFee;
    }

    [Serializable]
    public class MarketplaceListingDto
    {
        public string id;
        public string sellerId;
        public string sellerDisplayName;
        public string itemType;
        public int quantity;
        public int askingPrice;
        public int listingFee;
        public string status;
        public string createdAt;
        public string expiresAt;
        public bool mine;
    }

    [Serializable]
    public class MarketplacePurchaseDto
    {
        public MarketplaceListingDto listing;
        public long buyerFarmRevision;
        public int buyerMoney;
    }

    [Serializable]
    public class TradeInviteRequestDto
    {
        public string targetPlayerId;
    }

    /// <summary>One "your listing sold" notification for the seller.</summary>
    [Serializable]
    public class MarketplaceSaleDto
    {
        public string listingId;
        public string buyerDisplayName;
        public string itemType;
        public int quantity;
        public int moneyReceived;
        public string soldAt;
    }

    [Serializable]
    public class TradeItemDto
    {
        public string itemType;
        public int quantity;
    }

    [Serializable]
    public class UpdateTradeOfferRequestDto
    {
        public int money;
        public List<TradeItemDto> items = new List<TradeItemDto>();
    }

    [Serializable]
    public class TradeOfferDto
    {
        public int money;
        public List<TradeItemDto> items = new List<TradeItemDto>();
    }

    [Serializable]
    public class ActiveTradeResponseDto
    {
        public TradeViewDto trade;
    }

    [Serializable]
    public class TradeViewDto
    {
        public string id;
        public string requesterId;
        public string requesterDisplayName;
        public string targetId;
        public string targetDisplayName;
        public string status;
        public TradeOfferDto requesterOffer;
        public TradeOfferDto targetOffer;
        public bool requesterAgreed;
        public bool targetAgreed;
        public bool requesterConfirmed;
        public bool targetConfirmed;
        public bool requesterOnline;
        public bool targetOnline;
        public string createdAt;
        public string expiresAt;
        public long? currentPlayerFarmRevision;

        // Each player's farm district, so a seed can be refused the moment it is
        // dragged in. Null from a server older than the district rule; an empty
        // string for a farm with no district recorded, which uses the fallback pool.
        public string requesterDistrictName;
        public string targetDistrictName;
    }
}
