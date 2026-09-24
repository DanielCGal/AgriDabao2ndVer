using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public partial class SocialMarketplaceUIBuilder
    {
        private const float ChatPollMinInterval = 2f;
        private const float ChatPollMaxInterval = 8f;

        private const float SocialBoardLogInset = 150f;

        private const float RowRopeInset = 105f;

        private const float ChatBoardLogInset = 210f;

        private void BuildPlayerSearchPanel()
        {
            searchPanel = CreatePanel("PlayerSearchPanel", new Vector2(1400f, 760f));
            HudRegistry.RegisterPiece(HudPiece.SearchPlayersPanel, searchPanel);
            CreatePanelTitle(searchPanel.transform, "SEARCH PLAYERS / FRIENDS",
                Theme?.searchFriendsLabel, 470f, 150f, -13f);

            playerSearchInput = CreateInput(searchPanel.transform, "Enter player display name...",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(SocialBoardLogInset, -190f), new Vector2(-500f, -140f));

            CreateButton(searchPanel.transform, "Search", new Vector2(1f, 1f),
                new Vector2(160f, 56f), new Vector2(-320f, -140f), OnSearchPressed, out _,
                art: Theme?.searchButton);
            CreateButton(searchPanel.transform, "Requests", new Vector2(1f, 1f),
                new Vector2(160f, 56f), new Vector2(-SocialBoardLogInset, -140f),
                OnRequestsPressed, out _, art: Theme?.requestButton);

            playerListContent = CreateScrollContent(searchPanel.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(SocialBoardLogInset, 110f),
                new Vector2(-SocialBoardLogInset, -210f));

            playerSearchStatus = CreateText(searchPanel.transform, "Status", 20, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(SocialBoardLogInset, 40f), new Vector2(-380f, 95f));
            playerSearchStatus.text = "Search by display name or view your friends and incoming requests.";

            CreateButton(searchPanel.transform, "Close", new Vector2(1f, 0f),
                new Vector2(170f, 58f), new Vector2(-SocialBoardLogInset, 36f),
                () => searchPanel.SetActive(false), out _,
                art: Theme?.socialCloseButton);
            searchPanel.SetActive(false);
        }

        private void BuildProfilePanel()
        {
            profilePanel = CreatePanel("PlayerProfilePanel", new Vector2(1100f, 620f));
            CreatePanelTitle(profilePanel.transform, "PLAYER INFORMATION",
                Theme?.playerInfoLabel, 420f, 150f, -26f);

            profileInfoText = CreateText(profilePanel.transform, "ProfileInfo", 26, TextAnchor.UpperCenter,
                new Vector2(0f, 0.30f), new Vector2(1f, 0.80f),
                new Vector2(SocialBoardLogInset, 10f), new Vector2(-SocialBoardLogInset, -45f));

            profileAddFriendButton = CreateButton(profilePanel.transform, "Add Friend",
                new Vector2(0f, 0f), new Vector2(210f, 62f), new Vector2(170f, 90f),
                OnAddFriendPressed, out profileAddFriendText,
                art: Theme?.addFriendButton);
            profileAddFriendImage = profileAddFriendButton.GetComponent<Image>();

            profileTradeButton = CreateButton(profilePanel.transform, "Trade Player",
                new Vector2(0.5f, 0f), new Vector2(210f, 62f), new Vector2(0f, 90f),
                OnTradePlayerPressed, out _, art: Theme?.tradePlayerButton);
            profileChatButton = CreateButton(profilePanel.transform, "Chat Player",
                new Vector2(1f, 0f), new Vector2(210f, 62f), new Vector2(-170f, 90f),
                OnChatPlayerPressed, out _, art: Theme?.chatPlayerButton);

            CreateButton(profilePanel.transform, "Back", new Vector2(1f, 1f),
                new Vector2(150f, 52f), new Vector2(-SocialBoardLogInset, -90f),
                () =>
                {
                    profilePanel.SetActive(false);
                    searchPanel.SetActive(true);
                }, out _, art: Theme?.backButton);
            profilePanel.SetActive(false);
        }

        private void BuildChatPanel()
        {
            chatPanel = CreatePanel("PlayerChatPanel", new Vector2(1350f, 720f),
                boardOverride: Theme?.chatBoard);

            chatTitleText = CreateText(chatPanel.transform, "Title", 28, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(ChatBoardLogInset, -110f), new Vector2(-390f, -55f));
            chatTitleText.fontStyle = FontStyle.Bold;
            chatTitleText.text = "PLAYER CHAT";

            chatContent = CreateScrollContent(chatPanel.transform,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(ChatBoardLogInset, 140f),
                new Vector2(-ChatBoardLogInset, -120f));

            chatInput = CreateInput(chatPanel.transform, "Write a message...",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(ChatBoardLogInset, 62f), new Vector2(-400f, 112f));
            CreateButton(chatPanel.transform, "Send", new Vector2(1f, 0f),
                new Vector2(160f, 56f), new Vector2(-ChatBoardLogInset, 60f),
                OnSendChatPressed, out _, art: Theme?.playerChatSendButton);
            CreateButton(chatPanel.transform, "Close", new Vector2(1f, 1f),
                new Vector2(150f, 52f), new Vector2(-ChatBoardLogInset, -55f),
                CloseChatPanel, out _, art: Theme?.playerChatCloseButton);

            chatStatusText = CreateText(chatPanel.transform, "Status", 18, TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(ChatBoardLogInset, 10f), new Vector2(-ChatBoardLogInset, 45f));
            chatPanel.SetActive(false);
        }

        private void OnSearchPressed()
        {
            if (TutorialState.IsRunning)
            {
                playerSearchStatus.text =
                    "Other farmers are hidden until you finish the beginner guide.";
                return;
            }

            string query = playerSearchInput.text.Trim();
            if (query.Length < 2)
            {
                playerSearchStatus.text = "Enter at least two characters.";
                return;
            }
            StartCoroutine(SearchPlayers(query));
        }

        private IEnumerator SearchPlayers(string query)
        {
            playerSearchStatus.text = "Searching...";
            List<PlayerCardDto> players = null;
            string error = null;
            yield return controller.Api.SearchPlayers(query,
                value => players = value, message => error = message);
            if (players == null)
            {
                playerSearchStatus.text = error;
                yield break;
            }
            RenderPlayerCards(players);
            playerSearchStatus.text = players.Count == 0
                ? "No player matched that display name."
                : players.Count + " player(s) found.";
        }

        private IEnumerator LoadFriendsAndRequests()
        {
            List<PlayerCardDto> friends = null;
            string error = null;
            yield return controller.Api.GetFriends(value => friends = value, message => error = message);
            if (friends == null)
            {
                playerSearchStatus.text = error;
                yield break;
            }
            RenderPlayerCards(friends);
            playerSearchStatus.text = friends.Count == 0
                ? "You have no friends yet. Search for another farmer above."
                : "Your friends are listed below.";
        }

        private void OnRequestsPressed()
        {
            StartCoroutine(LoadFriendRequests());
        }

        private IEnumerator LoadFriendRequests()
        {
            List<FriendRequestDto> requests = null;
            string error = null;
            yield return controller.Api.GetFriendRequests(value => requests = value, message => error = message);
            if (requests == null)
            {
                playerSearchStatus.text = error;
                yield break;
            }

            ClearContent(playerListContent);
            foreach (FriendRequestDto request in requests)
            {
                FriendRequestDto captured = request;
                GameObject row = CreateListRow(playerListContent, 110f);
                Text info = CreateText(row.transform, "Info", 21, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 1f),
                    new Vector2(RowRopeInset, 8f), new Vector2(-430f, -8f));
                info.text = captured.displayName + "\n" + captured.districtName +
                            " | " + (captured.online ? "ONLINE" : "OFFLINE");

                CreateButton(row.transform, "Accept", new Vector2(1f, 0.5f),
                    new Vector2(150f, 54f), new Vector2(-265f, 0f),
                    () => StartCoroutine(AnswerRequest(captured.requestId, true)), out _,
                    art: Theme?.acceptButton);
                CreateButton(row.transform, "Decline", new Vector2(1f, 0.5f),
                    new Vector2(150f, 54f), new Vector2(-RowRopeInset, 0f),
                    () => StartCoroutine(AnswerRequest(captured.requestId, false)), out _,
                    new Color(0.62f, 0.18f, 0.18f, 0.98f),
                    art: Theme?.declineButton);
            }
            playerSearchStatus.text = requests.Count == 0
                ? "No incoming friend requests."
                : requests.Count + " incoming friend request(s).";
        }

        private IEnumerator AnswerRequest(string requestId, bool accept)
        {
            string error = null;
            if (accept)
                yield return controller.Api.AcceptFriendRequest(requestId, () => { }, message => error = message);
            else
                yield return controller.Api.DeclineFriendRequest(requestId, () => { }, message => error = message);

            playerSearchStatus.text = string.IsNullOrWhiteSpace(error)
                ? (accept ? "Friend request accepted." : "Friend request declined.")
                : error;
            yield return controller.RefreshNotifications();
            yield return LoadFriendRequests();
        }

        private void RenderPlayerCards(List<PlayerCardDto> players)
        {
            ClearContent(playerListContent);
            foreach (PlayerCardDto player in players)
            {
                PlayerCardDto captured = player;
                GameObject row = CreateListRow(playerListContent, 92f);
                Text info = CreateText(row.transform, "Info", 21, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0f), new Vector2(1f, 1f),
                    new Vector2(RowRopeInset, 8f), new Vector2(-270f, -8f));
                info.text = captured.displayName + "\n" + captured.districtName +
                            " | " + (captured.online ? "ONLINE" : "OFFLINE") +
                            " | " + RelationshipLabel(captured.relationship);
                CreateButton(row.transform, "View", new Vector2(1f, 0.5f),
                    new Vector2(150f, 54f), new Vector2(-RowRopeInset, 0f),
                    () => StartCoroutine(OpenPlayerProfile(captured.id)), out _,
                    art: Theme?.viewButton);
            }
        }

        private IEnumerator OpenPlayerProfile(string playerId)
        {
            playerSearchStatus.text = "Loading player information...";
            PlayerProfileDto profile = null;
            string error = null;
            yield return controller.Api.GetPlayer(playerId,
                value => profile = value, message => error = message);
            if (profile == null)
            {
                playerSearchStatus.text = error;
                yield break;
            }
            selectedProfile = profile;
            profileInfoText.text =
                "Display Name: " + profile.displayName + "\n\n" +
                "Farm District: " + profile.districtName + "\n\n" +
                "Money: P" + profile.money + "\n\n" +
                "Status: " + (profile.online ? "ONLINE" : "OFFLINE") + "\n\n" +
                "Relationship: " + RelationshipLabel(profile.relationship);

            bool friend = profile.relationship == "FRIENDS";
            ApplyFriendButtonState(profile.relationship);
            profileAddFriendButton.interactable = profile.relationship == "NONE" ||
                                                   profile.relationship == "INCOMING_PENDING";
            profileChatButton.interactable = friend;
            profileTradeButton.interactable = profile.online;
            searchPanel.SetActive(false);
            profilePanel.SetActive(true);
        }

        private void ApplyFriendButtonState(string relationship)
        {
            string label =
                relationship == "FRIENDS" ? "Friends" :
                relationship == "OUTGOING_PENDING" ? "Request Sent" :
                relationship == "INCOMING_PENDING" ? "Open Requests" : "Add Friend";

            Sprite art =
                relationship == "FRIENDS" ? Theme?.friendsButton :
                relationship == "OUTGOING_PENDING" ? Theme?.requestSentButton :
                Theme?.addFriendButton;

            if (profileAddFriendImage != null && art != null)
            {
                profileAddFriendImage.sprite = art;
                if (profileAddFriendText != null)
                    profileAddFriendText.text = "";
                return;
            }

            if (profileAddFriendText != null)
                profileAddFriendText.text = label;
        }

        private void OnAddFriendPressed()
        {
            if (selectedProfile == null) return;
            if (selectedProfile.relationship == "INCOMING_PENDING")
            {
                profilePanel.SetActive(false);
                searchPanel.SetActive(true);
                StartCoroutine(LoadFriendRequests());
                return;
            }
            StartCoroutine(SendFriendRequest());
        }

        private IEnumerator SendFriendRequest()
        {
            string error = null;
            yield return controller.Api.SendFriendRequest(selectedProfile.id,
                () => { }, message => error = message);
            if (string.IsNullOrWhiteSpace(error))
            {
                selectedProfile.relationship = "OUTGOING_PENDING";
                ApplyFriendButtonState(selectedProfile.relationship);
                profileAddFriendButton.interactable = false;
            }
            else
            {
                profileInfoText.text += "\n\nError: " + error;
            }
        }

        private void OnTradePlayerPressed()
        {
            if (selectedProfile == null) return;
            if (!selectedProfile.online)
            {
                profileInfoText.text += "\n\nBoth players must be online to trade.";
                return;
            }
            StartCoroutine(controller.InviteTrade(selectedProfile.id,
                ShowTradePanel,
                message => profileInfoText.text += "\n\nTrade error: " + message));
        }

        private void OnChatPlayerPressed()
        {
            if (selectedProfile == null || selectedProfile.relationship != "FRIENDS")
                return;
            OpenChatPanel(selectedProfile);
        }

        private void OpenChatPanel(PlayerProfileDto profile)
        {
            HideMainPanels();

            chatPlayer = profile;

            chatTitleText.text =
                profile.displayName.ToUpperInvariant() +
                (profile.online ? " (ONLINE)" : " (OFFLINE)");

            chatStatusText.text = "";

            chatCursor = null;
            chatPollInterval = ChatPollMinInterval;
            ClearContent(chatContent);

            chatPanel.SetActive(true);

            if (chatPolling != null)
            {
                StopCoroutine(chatPolling);
            }

            chatPolling = StartCoroutine(ChatPollLoop());
        }

        private void CloseChatPanel()
        {
            if (chatPolling != null)
            {
                StopCoroutine(chatPolling);
                chatPolling = null;
            }
            chatPanel?.SetActive(false);
            chatPlayer = null;
            chatCursor = null;
        }

        private IEnumerator ChatPollLoop()
        {
            while (chatPanel != null && chatPanel.activeSelf && chatPlayer != null)
            {
                yield return LoadConversation();
                yield return new WaitForSecondsRealtime(chatPollInterval);
            }
        }

        private IEnumerator LoadConversation()
        {
            string cursor = chatCursor;

            List<ChatMessageDto> messages = null;
            string error = null;
            yield return controller.Api.GetMessages(chatPlayer.id, cursor,
                value => messages = value, message => error = message);

            if (messages == null)
            {
                chatStatusText.text = error;
                yield break;
            }

            if (messages.Count == 0)
            {
                if (cursor == null)
                    chatStatusText.text = "No messages yet. Start the conversation.";

                chatPollInterval = Mathf.Min(chatPollInterval * 2f, ChatPollMaxInterval);
                yield break;
            }

            chatPollInterval = ChatPollMinInterval;

            foreach (ChatMessageDto message in messages)
            {
                AppendChatRow(message);
                chatCursor = message.sentAt;
            }

            chatStatusText.text = "";

            yield return controller.RefreshNotifications();
        }

        private void AppendChatRow(ChatMessageDto message)
        {
            bool mine = message.senderId == CurrentUserId;
            Sprite bubbleArt = Theme?.chatBubble;

            GameObject row = new GameObject("Bubble",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement),
                typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
            row.transform.SetParent(chatContent, false);

            VerticalLayoutGroup layout = row.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = bubbleArt != null
                ? new RectOffset(105, 105, 32, 32)
                : new RectOffset(16, 16, 10, 10);

            ContentSizeFitter fitter = row.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement element = row.GetComponent<LayoutElement>();
            element.minHeight = bubbleArt != null ? 132f : 60f;

            Image background = row.GetComponent<Image>();
            if (bubbleArt != null)
            {
                background.sprite = bubbleArt;
                background.type = Image.Type.Sliced;
                background.color = Color.white;
            }
            else
            {
                background.color = mine
                    ? new Color(0.10f, 0.45f, 0.20f, 0.40f)
                    : new Color(0.10f, 0.30f, 0.55f, 0.40f);
            }

            GameObject textGo = new GameObject("Message",
                typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
            textGo.transform.SetParent(row.transform, false);

            Text text = textGo.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = 19;
            text.alignment = mine ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = (mine ? "You: " : chatPlayer.displayName + ": ") + message.body;

            ContentSizeFitter textFitter = textGo.GetComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void OnSendChatPressed()
        {
            if (chatPlayer == null) return;
            string body = chatInput.text.Trim();
            if (body.Length == 0) return;
            StartCoroutine(SendChat(body));
        }

        private IEnumerator SendChat(string body)
        {
            ChatMessageDto sent = null;
            string error = null;
            yield return controller.Api.SendMessage(chatPlayer.id, body,
                value => sent = value, message => error = message);
            if (sent == null)
            {
                chatStatusText.text = error;
                yield break;
            }
            chatInput.text = "";

            chatPollInterval = ChatPollMinInterval;
            yield return LoadConversation();
        }

        private static string RelationshipLabel(string relationship)
        {
            switch (relationship)
            {
                case "FRIENDS": return "Friend";
                case "OUTGOING_PENDING": return "Friend request sent";
                case "INCOMING_PENDING": return "Friend request received";
                case "SELF": return "You";
                default: return "Not friends";
            }
        }
    }
}
