using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public partial class SocialMarketplaceUIBuilder
    {
        // Chat polls fast while a conversation is active and backs off when idle.
        private const float ChatPollMinInterval = 2f;
        private const float ChatPollMaxInterval = 8f;

        /// <summary>
        /// SocialBoard.png is 9-sliced with 130px rolled-log caps, so the flat plank
        /// only begins this far in from either edge. Controls are inset past it
        /// instead of being drawn over the log art.
        /// </summary>
        private const float SocialBoardLogInset = 150f;

        /// <summary>
        /// SocialLisRow.png paints a rope loop near each end. This keeps a row's
        /// text and button between them rather than on top of them.
        /// </summary>
        private const float RowRopeInset = 105f;

        /// <summary>
        /// ChatBoard.png needs a deeper inset than SocialBoard.png despite both
        /// declaring a 130px border. On SocialBoard the logs sit clear of the plank,
        /// so 130 is honest. On ChatBoard the logs overlap the plank and their art
        /// runs on past the border into the stretchable region - about x179 once the
        /// board is stretched - so anything placed at 150 landed on the log.
        /// </summary>
        private const float ChatBoardLogInset = 210f;

        private void BuildPlayerSearchPanel()
        {
            // Widened from 1050: at that width the input plus both buttons did not
            // fit between the logs, which pushed Request Send onto the right log.
            searchPanel = CreatePanel("PlayerSearchPanel", new Vector2(1400f, 760f));
            HudRegistry.RegisterPiece(HudPiece.SearchPlayersPanel, searchPanel);
            // searchFriendLabel.png is 1400x300 (4.67:1); preserveAspect keeps it
            // undistorted, so this width just sets the sign's overall size. Kept
            // near a third of the board width, matching the shop sign's proportion.
            // The negative offsetY sinks it into the board: a smaller sign overlaps
            // the top edge less, so at the default +6 it floated free of the board.
            CreatePanelTitle(searchPanel.transform, "SEARCH PLAYERS / FRIENDS",
                Theme?.searchFriendsLabel, 470f, 150f, -13f);

            // Inset so the fields clear the board's rolled log edges.
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

            // The Trade button that sat here is gone. A PENDING trade now raises the
            // trade-request popup via OnActiveTradeChanged, which reaches the player
            // wherever they are rather than only while this panel is open.
            CreateButton(searchPanel.transform, "Close", new Vector2(1f, 0f),
                new Vector2(170f, 58f), new Vector2(-SocialBoardLogInset, 36f),
                () => searchPanel.SetActive(false), out _,
                art: Theme?.socialCloseButton);
            searchPanel.SetActive(false);
        }

        private void BuildProfilePanel()
        {
            // Widened from 820: the three action buttons are 210 wide each, which at
            // the old width left them overlapping rather than merely touching.
            profilePanel = CreatePanel("PlayerProfilePanel", new Vector2(1100f, 620f));
            // PlayerInformationLabel.png is 1400x300 (4.67:1), the same very wide art
            // as the search sign. At 620 on an 820 board it covered three quarters of
            // the width, which is what read as stretched.
            // Sunk further so the sign covers the board's 70px top log cap. The art
            // is opaque over almost its whole canvas, so at a shallower offset its
            // lower edge stopped short of the log and the sign read as floating.
            CreatePanelTitle(profilePanel.transform, "PLAYER INFORMATION",
                Theme?.playerInfoLabel, 420f, 150f, -26f);

            profileInfoText = CreateText(profilePanel.transform, "ProfileInfo", 26, TextAnchor.UpperCenter,
                new Vector2(0f, 0.30f), new Vector2(1f, 0.80f),
                new Vector2(SocialBoardLogInset, 10f), new Vector2(-SocialBoardLogInset, -45f));

            // Art for this one is swapped at runtime between Add Friend / Request
            // Sent / Friends, so the sprite is applied in OpenPlayerProfile instead
            // of being fixed here.
            // Three 210-wide buttons across the plank, with a 65px gap between each
            // and 40px clear of the log caps on either side. Raised from y=48, which
            // had them straddling the board's bottom log.
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

            // Tucked into the plank's top-right corner: 20px inside the right log cap
            // (130) and 20px below the top cap (70), so it sits on wood, not on log.
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
            // Widened again so the deeper ChatBoardLogInset does not cost content
            // width: 1350 - 2*210 leaves 930, slightly more than the previous 900.
            chatPanel = CreatePanel("PlayerChatPanel", new Vector2(1350f, 720f),
                boardOverride: Theme?.chatBoard);

            // Name and status only, sitting inside the board's left inset.
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
            // Other farmers stay hidden until the tour is over. Refused here
            // rather than after the request so no search for a real display name
            // ever leaves the device during the tutorial.
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

        /// <summary>
        /// Swaps the friend button between its three painted states. With no art
        /// set it falls back to changing the plain text label instead.
        /// </summary>
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
            // Hide the other panels first.
            // HideMainPanels calls CloseChatPanel, which clears chatPlayer.
            HideMainPanels();

            // Assign the player only after HideMainPanels finishes.
            chatPlayer = profile;

            // Name and status only.
            chatTitleText.text =
                profile.displayName.ToUpperInvariant() +
                (profile.online ? " (ONLINE)" : " (OFFLINE)");

            chatStatusText.text = "";

            // Fresh conversation view: no cursor yet, nothing rendered yet, poll fast.
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
            // Only the very first fetch pulls the whole history. After that the
            // cursor makes the backend return just the messages we do not have,
            // which is normally an empty list.
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

                // Idle conversation: gradually slow the poll down to save battery.
                chatPollInterval = Mathf.Min(chatPollInterval * 2f, ChatPollMaxInterval);
                yield break;
            }

            // New activity, so go back to the responsive interval.
            chatPollInterval = ChatPollMinInterval;

            foreach (ChatMessageDto message in messages)
            {
                AppendChatRow(message);
                chatCursor = message.sentAt;
            }

            // The status line only surfaces errors and the empty-conversation hint
            // now; the offline-storage note was removed from the design.
            chatStatusText.text = "";

            // Reading the conversation clears unread messages server-side, so only
            // refresh the badge when something actually arrived - not every tick.
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

            // The bubble grows with its text instead of using a fixed row height,
            // so a long message wraps to two or three lines and the plank stretches
            // to match rather than clipping.
            VerticalLayoutGroup layout = row.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            // ChatBubble.png is 9-sliced {90,40,90,40}. The rope loops live inside
            // the 90px left/right caps, so the text has to start past 90 - the old
            // 46 put it straight over them.
            layout.padding = bubbleArt != null
                ? new RectOffset(105, 105, 32, 32)   // clear the rope ends
                : new RectOffset(16, 16, 10, 10);

            ContentSizeFitter fitter = row.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement element = row.GetComponent<LayoutElement>();
            // The sprite is 172 tall with 40px top and bottom caps, leaving a 92px
            // stretchable band. At the old 86 that band was crushed to 6px, which is
            // what made the plank look squashed. 132 keeps it at a healthy 52px, and
            // because this is only a MINIMUM the bubble still grows for long text.
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

            // Let the normal incremental path pick the new message up, so it is
            // rendered once and the cursor advances with the stored timestamp.
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
