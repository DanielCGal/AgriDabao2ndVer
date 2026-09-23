using UnityEngine;
using UnityEngine.Video;

namespace AgriDabao3D
{
    /// <summary>
    /// Drag-and-drop home for every Main Menu / Auth / Settings UI sprite.
    ///
    /// The Login, Sign-up, Verify and Settings panels are all built at runtime,
    /// so those components have no Inspector of their own to drop sprites into.
    /// This asset solves that: it lives in Assets/Resources as a single
    /// "UITheme" asset, the builders load it by name, and every slot below shows
    /// up in one Inspector.
    ///
    /// Every slot is optional. A slot left empty simply falls back to the old
    /// flat-colour look for that one element, so the UI never breaks while the
    /// art is still being made.
    ///
    /// SETUP: right-click in Assets/Resources -> Create -> AgriDabao -> UI Theme,
    /// and name it exactly "UITheme".
    /// </summary>
    /// <summary>One district's painted name sign, paired with the district it belongs to.</summary>
    [System.Serializable]
    public class DistrictSign
    {
        public string districtName;
        public Sprite sign;
    }

    [CreateAssetMenu(fileName = "UITheme", menuName = "AgriDabao/UI Theme")]
    public class UIThemeSprites : ScriptableObject
    {
        public const string ResourceName = "UITheme";

        private static UIThemeSprites cached;
        private static bool lookedUp;

        /// <summary>The one theme asset, or null if it has not been created yet.</summary>
        public static UIThemeSprites Instance
        {
            get
            {
                if (lookedUp)
                    return cached;

                lookedUp = true;
                cached = Resources.Load<UIThemeSprites>(ResourceName);

                // Normalise Unity's "fake null" to a real null so callers can
                // safely use the ?. operator against this reference.
                if (cached == null)
                {
                    cached = null;
                    Debug.LogWarning(
                        "UIThemeSprites: no 'UITheme' asset found in a Resources folder. " +
                        "UI will use the plain fallback style. Create one via " +
                        "Assets/Resources -> right-click -> Create -> AgriDabao -> UI Theme.");
                }

                return cached;
            }
        }

        [Header("Shared Board (9-sliced)")]
        [Tooltip("The wooden board behind every panel. Set its Border in the Sprite Editor " +
                 "so the rolled log ends never stretch.")]
        public Sprite panelBoard;

        [Header("Player Login")]
        public Sprite loginLabel;
        public Sprite loginButton;
        public Sprite signUpButton;

        [Header("Create Player Account")]
        public Sprite createAccountLabel;
        public Sprite createButton;
        public Sprite createBackButton;

        [Header("Enter Code")]
        public Sprite enterCodeLabel;
        public Sprite verifyButton;
        public Sprite verifyBackButton;
        public Sprite resendCodeButton;

        [Header("Settings")]
        public Sprite settingsLabel;
        public Sprite closeButton;
        [Tooltip("The grooved wooden bar the slider knob runs along.")]
        public Sprite sliderBar;
        [Tooltip("The round log cross-section used as the slider knob.")]
        public Sprite sliderKnob;

        [Header("Credits")]
        [Tooltip("Round or wide button on the main menu that opens the credits.")]
        public Sprite creditsButton;
        [Tooltip("Board behind the rolling credits. 9-sliced, same style as panelBoard. " +
                 "Leave empty to reuse panelBoard.")]
        public Sprite creditsBoard;
        [Tooltip("Game logo shown at the very top of the roll.")]
        public Sprite gameLogo;
        [Tooltip("Team logo shown at the very bottom of the roll.")]
        public Sprite teamLogo;

        [Header("Area Selection")]
        [Tooltip("Plank behind the \"you selected outside the district\" warning. " +
                 "Not 9-sliced. Export 1800 x 600.")]
        public Sprite areaSelectionPopupBoard;
        [Tooltip("On-screen size of that plank. Keep it on your art's aspect ratio.")]
        public Vector2 areaSelectionPopupSize = new Vector2(900f, 300f);
        public Sprite prevDistrictButton;
        public Sprite nextDistrictButton;
        public Sprite areaBackButton;
        public Sprite generateButton;
        [Tooltip("Locks in the district the player is looking at and moves on to "
                 + "picking an area inside it. Sits exactly where Generate sits - the "
                 + "two are never on screen at the same time. Defaults to the Trade "
                 + "screen's ENTER plank; drop a painted NEXT here to replace it.")]
        public Sprite districtNextButton;
        [Tooltip("The plank that sits behind the \"Drag the box inside ...\" hint. 9-sliced.")]
        public Sprite infoPlank;

        [Tooltip("The draggable selection box's frame, drawn on the map. 9-sliced.")]
        public Sprite selectionBoxFrame;

        [Tooltip("One painted sign per district. The name must match the district " +
                 "exactly (Calinan, Toril, Baguio, Paquibato, Marilog, Buhangin, Tugbok); " +
                 "an entry left empty falls back to plain text for that district only.")]
        public DistrictSign[] districtSigns = new DistrictSign[]
        {
            new DistrictSign { districtName = "Calinan" },
            new DistrictSign { districtName = "Toril" },
            new DistrictSign { districtName = "Baguio" },
            new DistrictSign { districtName = "Paquibato" },
            new DistrictSign { districtName = "Marilog" },
            new DistrictSign { districtName = "Buhangin" },
            new DistrictSign { districtName = "Tugbok" }
        };

        /// <summary>The painted sign for a district, or null to fall back to text.</summary>
        public Sprite GetDistrictSign(string districtName)
        {
            if (districtSigns == null || string.IsNullOrWhiteSpace(districtName))
                return null;

            for (int i = 0; i < districtSigns.Length; i++)
            {
                DistrictSign entry = districtSigns[i];
                if (entry != null &&
                    string.Equals(entry.districtName, districtName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return entry.sign;
                }
            }

            return null;
        }

        [Header("Farm HUD - Pause")]
        [Tooltip("Round wooden pause button in the farm's top-left corner.")]
        public Sprite pauseButton;
        [Tooltip("Decorated board behind the Paused menu. Not 9-sliced - the panel is " +
                 "sized to this sprite's own aspect so the corner art never stretches.")]
        public Sprite pauseBoard;
        public Sprite pauseSettingsButton;
        public Sprite pauseMainMenuButton;
        public Sprite pauseResumeButton;

        [Header("Farm HUD - Leave Prompt")]
        [Tooltip("Board behind \"Save your farm before leaving?\". Falls back to panelBoard when empty.")]
        public Sprite leavePromptBoard;
        public Sprite yesButton;
        public Sprite noButton;
        public Sprite cancelButton;

        [Header("Farm HUD - AI Adviser")]
        [Tooltip("Round wooden button that opens the AI-Adviser chat, beside the pause button.")]
        public Sprite chatbotButton;
        [Tooltip("Board behind the whole AI-Adviser chat panel.")]
        public Sprite chatbotBoard;
        public Sprite askQuestionsLabel;
        [Tooltip("Semi-transparent panel the chat transcript scrolls inside. 9-sliced.")]
        public Sprite chatWindowPanel;
        [Tooltip("Wooden bar behind the type-here field. 9-sliced.")]
        public Sprite chatInputBar;
        public Sprite chatSendButton;
        [Tooltip("Wooden frame around Antonio's animated portrait.")]
        public Sprite portraitFrame;

        [Header("Farm HUD - Inventory")]
        [Tooltip("Wooden bar drawn behind the hotbar's six slots plus the bag slot.")]
        public Sprite hotbarBar;
        [Tooltip("On-screen size of that bar. Nudge until its painted cells line up " +
                 "with the slots sitting on top of it.")]
        public Vector2 hotbarBarSize = new Vector2(1180f, 197f);
        [Tooltip("The woven basket shown in the locked 7th (bag) slot.")]
        public Sprite bagButton;
        [Tooltip("Grid frame behind the 6x6 backpack. Not 9-sliced - the panel is " +
                 "sized to this sprite's own aspect so the cells stay square.")]
        public Sprite backpackBoard;
        [Tooltip("On-screen size of that board. Match your sprite's aspect ratio " +
                 "or the frame will stretch.")]
        public Vector2 backpackBoardSize = new Vector2(880f, 642f);
        [Tooltip("Size of the painted 6x6 cell area inside the frame. Item slots are " +
                 "spread evenly across this, so grow or shrink it until they sit on " +
                 "the cells.")]
        public Vector2 backpackGridSize = new Vector2(800f, 580f);
        [Tooltip("Nudge the whole grid relative to the board's centre.")]
        public Vector2 backpackGridOffset = Vector2.zero;
        [Tooltip("Item size within each cell, as a fraction of the cell. Lower values " +
                 "leave more of the painted cell visible around each item.")]
        [Range(0.4f, 1f)] public float backpackSlotFill = 0.86f;

        public Sprite backpackLabel;
        [Tooltip("The round X that closes the backpack.")]
        public Sprite backpackCloseButton;

        [Header("Farm HUD - Weather / Time")]
        [Tooltip("Clear weather, daytime.")]
        public Sprite weatherSunny;
        [Tooltip("Clear weather, night-time.")]
        public Sprite weatherNight;
        public Sprite weatherRain;
        public Sprite weatherTyphoon;
        public Sprite weatherDrought;
        [Tooltip("On-screen size of the weather panel. All five sprites must share " +
                 "one canvas size with the plank in the same place, or the text will " +
                 "drift when the weather changes.")]
        public Vector2 weatherPanelSize = new Vector2(560f, 280f);

        [Header("Farm HUD - Round Action Buttons")]
        public Sprite shopButton;
        public Sprite farmObjectivesButton;
        public Sprite searchPlayersButton;
        public Sprite marketplaceButton;
        public Sprite saveFarmButton;

        [Header("Farm HUD - Money")]
        [Tooltip("Plank the money amount is printed on, coins hanging off the left.")]
        public Sprite moneyPlank;
        [Tooltip("On-screen size of the money plank.")]
        public Vector2 moneyPlankSize = new Vector2(360f, 150f);

        [Header("Tutorial - Antonio")]
        [Tooltip("Dialogue board: the plank with the portrait frame on its left. " +
                 "Not 9-sliced. Export 2048 x 843.")]
        public Sprite tutorialDialogueBoard;
        [Tooltip("On-screen size of the dialogue board.")]
        public Vector2 tutorialDialogueSize = new Vector2(1240f, 510f);
        [Tooltip("The frame's dark opening, in on-screen units. Measured from " +
                 "DialogueBoard.png: the opening is 445 x 629 of its 2048 x 843, " +
                 "which is 269 x 380 once drawn at 1240 x 510.")]
        public Vector2 tutorialPortraitSize = new Vector2(269f, 380f);
        [Tooltip("Top-left corner of that opening, measured from the board's own " +
                 "top-left. X runs right, Y runs down. Tune to your art rather " +
                 "than editing code; the video is clipped to this rectangle.")]
        public Vector2 tutorialPortraitOffset = new Vector2(37f, 68f);
        // These four are VideoClips, not Sprites, so Antonio's portrait moves while
        // he talks. Drop the .mp4 files straight in. Note that MP4 carries no alpha
        // channel: whatever is behind him in the video is what will show inside the
        // frame, so keep all four on the same background.
        [Tooltip("Waving. Greetings, farewells, praise.")]
        public VideoClip antonioHello;
        [Tooltip("Pointing. Instructions and explanations - the most used one.")]
        public VideoClip antonioTeaching;
        [Tooltip("Hand on chin. Warnings and the reflective beats.")]
        public VideoClip antonioThinking;
        [Tooltip("Hands on cheeks. Shock and delight.")]
        public VideoClip antonioSurprise;
        [Tooltip("Board behind the \"do you want the tutorial?\" question. Export " +
                 "the plank BARE - the shared confirmYesButton and confirmNoButton " +
                 "are placed on top, exactly as the Save Farm popup does it.")]
        public Sprite tutorialPromptBoard;
        [Tooltip("Optional painted sign for that prompt.")]
        public Sprite tutorialPromptLabel;
        [Tooltip("Narrow plank shown at the top of the screen while the guide is " +
                 "waiting for the player to do something. The big dialogue board " +
                 "steps aside for it so the farm is not covered up. Export 1400 x 300.")]
        public Sprite tutorialObjectiveBoard;
        [Tooltip("On-screen size of that plank.")]
        public Vector2 tutorialObjectiveSize = new Vector2(700f, 150f);

        [Header("Farm HUD - Map")]
        [Tooltip("Wooden frame around the map. Not 9-sliced: the one sprite is " +
                 "drawn at both sizes below, so keep those two on the same aspect " +
                 "ratio as the art or the frame will stretch. Leave the middle " +
                 "transparent - the field colour below is drawn behind it.")]
        public Sprite mapFrame;
        [Tooltip("On-screen size of the small map that sits under the money plank.")]
        public Vector2 mapSmallSize = new Vector2(400f, 300f);
        [Tooltip("On-screen size of the map once it has been opened.")]
        public Vector2 mapExpandedSize = new Vector2(1040f, 780f);
        [Tooltip("How far the field is inset from each edge of the frame, as a " +
                 "fraction of the frame's size. Tune to your art so the dots stay " +
                 "inside the painted opening. X and Y are separate because the " +
                 "border is rarely the same thickness on both axes.")]
        public Vector2 mapFieldInsetFraction = new Vector2(0.105f, 0.125f);
        [Tooltip("Colour of the field the dots sit on. Set the alpha to 0 if your " +
                 "frame art already paints its own field.")]
        public Color mapFieldColor = new Color(0.42f, 0.60f, 0.16f, 1f);
        [Tooltip("Closes the opened map. Optional - a plain button is drawn if empty.")]
        public Sprite mapCloseButton;

        [Header("Farm HUD - Farm Objectives Book")]
        [Tooltip("The open book behind the objectives. Not 9-sliced.")]
        public Sprite objectivesBook;
        [Tooltip("On-screen size of the book. Match your sprite's aspect ratio.")]
        public Vector2 objectivesBookSize = new Vector2(1200f, 662f);
        public Sprite dailyObjectivesLabel;
        public Sprite antonioObjectivesLabel;
        public Sprite collectRewardButton;
        public Sprite skipNextDayButton;
        public Sprite callAntonioButton;
        public Sprite skipTaskButton;
        public Sprite checkTaskButton;

        [Header("Social - Shared")]
        [Tooltip("Rolled-log board behind the social panels (search, profile). 9-sliced.")]
        public Sprite socialBoard;
        [Tooltip("Wooden bar behind each player / request row in a list. 9-sliced.")]
        public Sprite socialListRow;
        [Tooltip("Generic small wooden button used where no dedicated art is set.")]
        public Sprite socialButton;

        [Header("Social - Search / Friends")]
        public Sprite searchFriendsLabel;
        public Sprite searchButton;
        public Sprite requestButton;
        public Sprite viewButton;
        public Sprite acceptButton;
        public Sprite declineButton;
        [Tooltip("Close button for the social panels. Separate from Settings' " +
                 "closeButton so the two can use different art if you want.")]
        public Sprite socialCloseButton;

        [Header("Social - Player Information")]
        public Sprite playerInfoLabel;
        public Sprite backButton;
        [Tooltip("Shown when the two players are not friends yet.")]
        public Sprite addFriendButton;
        [Tooltip("Shown after a request has been sent and is still pending.")]
        public Sprite requestSentButton;
        [Tooltip("Shown once the two players are friends (button is disabled).")]
        public Sprite friendsButton;
        public Sprite tradePlayerButton;
        public Sprite chatPlayerButton;

        [Header("Farm Task Popup")]
        [Tooltip("Board behind the Farm Task popup. 9-sliced.")]
        public Sprite farmTaskBoard;
        public Sprite farmTaskLabel;
        public Sprite farmTaskCloseButton;

        [Header("Pest & Disease Popup")]
        [Tooltip("Wide plank behind the pest/disease announcement. 9-sliced.")]
        public Sprite pestDiseaseBoard;
        public Sprite pestDiseaseLabel;
        [Tooltip("Leave empty to reuse the Climate Event's Okay button art.")]
        public Sprite pestDiseaseOkayButton;

        [Header("Climate Maintenance Toast")]
        [Tooltip("Plain plank behind the placement confirmation toast. 9-sliced. " +
                 "No button - the toast hides itself after a few seconds.")]
        public Sprite maintenanceToastPlank;

        [Header("World Loading Screen")]
        [Tooltip("Plank behind \"Generating Farm\". 9-sliced.")]
        public Sprite loadingBoard;
        public Sprite loadingLabel;

        [Header("Mobile HUD")]
        [Tooltip("Square wooden frame behind the movement joystick.")]
        public Sprite joystickBase;
        [Tooltip("Round log the player drags.")]
        public Sprite joystickHandle;
        public Sprite jumpButton;

        [Header("Shop")]
        [Tooltip("Board behind the whole shop. 9-sliced.")]
        public Sprite shopBoard;
        public Sprite shopLabel;
        [Tooltip("The 4x4 shelf grid the stock is drawn in. 9-sliced.")]
        public Sprite shopShelfGrid;
        [Tooltip("Framed preview box showing the selected item.")]
        public Sprite shopPreviewFrame;
        public Sprite shopPrevPageButton;
        public Sprite shopNextPageButton;
        public Sprite shopBuyButton;

        [Header("Climate Event Popup")]
        [Tooltip("Wide plank behind the \"a typhoon is here\" announcement. 9-sliced.")]
        public Sprite climateEventBoard;
        public Sprite climateEventLabel;

        [Header("Climate Evaluation Popup")]
        [Tooltip("Board behind the end-of-event mitigation score. 9-sliced.")]
        public Sprite climateEvaluationBoard;
        [Tooltip("Shared by both climate popups.")]
        public Sprite climateOkayButton;

        [Header("Confirm Popups - Save Farm")]
        [Tooltip("Wide plank behind the Save Farm confirmation. 9-sliced.")]
        public Sprite saveFarmBoard;
        public Sprite saveFarmLabel;

        [Header("Confirm Popups - Save Settings")]
        [Tooltip("Wide plank behind the Save Settings confirmation. 9-sliced.")]
        public Sprite saveSettingsBoard;
        public Sprite saveSettingsLabel;

        [Header("Confirm Popups - Shared Buttons")]
        [Tooltip("Shared by both confirmation popups.")]
        public Sprite confirmYesButton;
        public Sprite confirmNoButton;

        [Header("Social - Marketplace")]
        public Sprite marketplaceLabel;
        [Tooltip("The << and >> filter arrows.")]
        public Sprite filterPrevButton;
        public Sprite filterNextButton;
        public Sprite refreshButton;
        public Sprite sellItemsButton;
        public Sprite marketplaceCloseButton;
        [Tooltip("Cancels the player's own listing and returns the items.")]
        public Sprite cancelListingButton;
        [Tooltip("Buys another player's listing.")]
        public Sprite buyListingButton;

        [Header("Social - Sell Items")]
        public Sprite sellItemsLabel;
        public Sprite createListingButton;
        public Sprite sellCancelButton;

        [Header("Social - Sale Notification")]
        [Tooltip("Board behind \"<name> bought your items!\".")]
        public Sprite saleNotifyBoard;
        public Sprite saleNotifyLabel;
        public Sprite okayButton;

        [Header("Social - Player Chat")]
        [Tooltip("Board behind the whole chat panel. 9-sliced.")]
        public Sprite chatBoard;
        [Tooltip("Rope-ended plank behind a single chat message. 9-sliced, so it " +
                 "stretches taller on its own when a message wraps to several lines.")]
        public Sprite chatBubble;
        [Tooltip("Separate from the AI-Adviser chat's chatSendButton so the two " +
                 "panels can use different art if you want.")]
        public Sprite playerChatSendButton;
        public Sprite playerChatCloseButton;

        [Header("Social - Trade Request Popup")]
        [Tooltip("Plank behind \"<name> wants to trade with you!\".")]
        public Sprite tradeRequestBoard;
        public Sprite tradeRequestLabel;

        [Header("Social - Trade Screen")]
        public Sprite tradingLabel;
        [Tooltip("Large 6x6 grid the player's own inventory is drawn in. 9-sliced.")]
        public Sprite tradeInventoryGrid;
        [Tooltip("The 2x2 grid one player's offered items sit in. 9-sliced.")]
        public Sprite tradeOfferGrid;
        [Tooltip("Name plank above each player's 2x2 offer area.")]
        public Sprite tradeNamePlank;
        [Tooltip("Money plank shown beside each offer area.")]
        public Sprite tradeMoneyPlank;
        [Tooltip("Spinning log shown while that player has not locked their offer.")]
        public Sprite tradeLogIcon;
        [Tooltip("Padlock shown once that player has pressed Set Trade.")]
        public Sprite tradeLockIcon;
        public Sprite addMoneyPlank;
        public Sprite enterButton;
        public Sprite setTradeButton;
        [Tooltip("Cancels an active trade; also doubles as Close once the trade " +
                 "has already ended (completed/cancelled/declined).")]
        public Sprite cancelTradeButton;

        [Header("Social - Trade Confirmation")]
        [Tooltip("Board behind the final \"you give / you get\" summary.")]
        public Sprite tradeConfirmBoard;
        public Sprite tradeConfirmLabel;
        public Sprite confirmAcceptButton;
        public Sprite confirmDeclineButton;

        [Tooltip("Degrees per second the log icon spins while a player is still deciding.")]
        public float tradeLogSpinSpeed = 90f;

        [Header("Farm HUD - Crop Info")]
        [Tooltip("Board behind the crop inspection readout. 9-sliced.")]
        public Sprite cropInfoBoard;

        [Header("Layout Tuning")]
        [Tooltip("On-screen height of every hanging label sign. Width follows the " +
                 "sprite's own aspect ratio automatically.")]
        public float labelHeight = 150f;

        [Tooltip("Vertical nudge of the label sign relative to the board's top edge. " +
                 "Negative sits lower, positive sits higher.")]
        public float labelOffsetY = -12f;

        [Tooltip("On-screen size of the paired buttons (Login/Sign Up, Create/Back, Verify/Back).")]
        public Vector2 buttonSize = new Vector2(280f, 95f);

        [Tooltip("On-screen size of the single wide buttons (Resend code, Close).")]
        public Vector2 wideButtonSize = new Vector2(320f, 95f);

        [Tooltip("How much horizontal space each rolled log end of the board takes up. " +
                 "Panel content is inset by this much on both sides.")]
        public float panelPaddingX = 170f;
    }
}
