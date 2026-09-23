using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// The Account board, opened from the Main Menu.
    ///
    /// Shows the account's display name and email, and offers the three things a
    /// signed-in player can change about it. Each change is its own small board,
    /// and every one of them ends back here with the new value already on the
    /// planks - the panel reads AuthSession.CurrentUser, which the session
    /// updates as soon as the server confirms a change.
    ///
    /// Built at runtime like every other panel in the game, out of the planks the
    /// tutorial and trade screens already ship: there are no UI prefabs to edit.
    /// </summary>
    public class AccountUIBuilder : MonoBehaviour
    {
        public static AccountUIBuilder Instance { get; private set; }

        /// <summary>
        /// Wider than the login board, for two reasons. The rows have to be tall
        /// enough to seat a caption plank and a value under it, and these planks
        /// are drawn at their own proportions rather than squashed - ObjectivePlank
        /// is a wide 1400x300 and TradeRequestBoard a squat 1520x600, neither
        /// carrying 9-slice borders - so a row only gets taller by getting wider.
        /// And the board's rolled logs eat a fixed amount off each side, so a
        /// wider board is the only way to buy usable width between them.
        /// </summary>
        private const float PanelWidth = 1200f;

        // Where TradeConfirmBoard.png actually stops being a rolled log and
        // starts being the plank wall, measured off the 1760x1320 art: the left
        // log runs to x=280 and the right one begins again at x=1458.
        //
        // These are NOT the sprite's 9-slice border, which is only 130 either
        // side. That border is set to the least that can be repeated without the
        // art distorting, not to the edge of the painted log, so laying content
        // out against it puts the first 80-odd units of every row on top of the
        // log. Which is exactly what it did.
        private const float BoardArtWidth = 1760f;
        private const float BoardLogEndsAtPx = 280f;
        private const float BoardLogResumesAtPx = 1458f;

        /// <summary>
        /// How far in from each edge the plank wall begins, for a board of this
        /// width.
        ///
        /// It depends on the width because of how 9-slicing draws: the corners
        /// keep their true size and everything between them is stretched, so the
        /// part of the log that falls outside the border region moves inward as
        /// the board grows. Computed rather than fixed so that changing a panel's
        /// width cannot silently push its contents back onto the wood.
        /// </summary>
        private float BoardContentInset(float panelWidth)
        {
            Sprite board = theme?.tradeConfirmBoard;
            if (board == null || board.rect.width <= 0f)
                return PaddingX;

            Vector4 border = board.border;
            float stretched = BoardArtWidth - border.x - border.z;
            if (stretched <= 0f)
                return PaddingX;

            float scale = (panelWidth - border.x - border.z) / stretched;
            float left = border.x + (BoardLogEndsAtPx - border.x) * scale;
            float right = border.z + (BoardArtWidth - BoardLogResumesAtPx - border.z) * scale;

            // The wider of the two, so the layout stays centred and clears both.
            return Mathf.Max(left, right);
        }

        /// <summary>Usable width between the two logs on a board this wide.</summary>
        private float BoardContentWidth(float panelWidth)
        {
            return panelWidth - BoardContentInset(panelWidth) * 2f;
        }

        /// <summary>
        /// Every board is kept well under 900 tall on purpose. The interface-size
        /// setting shrinks each canvas's reference resolution, so at its 1.20
        /// ceiling only 900 units of height are left: a taller board would push
        /// its own heading off the top of the screen, and one that reached the
        /// full 900 would sit under the status line along the bottom.
        /// </summary>
        private const float DetailsHeight = 800f;

        private const float RowWidth = 680f;
        private const float HeadingWidth = 460f;

        private static readonly Vector2 PairButtonSize = new Vector2(280f, 95f);
        private static readonly Vector2 WideButtonSize = new Vector2(320f, 95f);

        private UIThemeSprites theme;
        private Canvas canvas;

        private GameObject root;
        private GameObject detailsPanel;
        private GameObject emailPanel;
        private GameObject verifyPanel;
        private GameObject passwordPanel;
        private GameObject displayNamePanel;

        private Text statusText;
        private Text displayNameValue;
        private Text emailValue;
        private Text verifyInfo;

        private InputField newEmailInput;
        private InputField verifyCodeInput;
        private InputField currentPasswordInput;
        private InputField newPasswordInput;
        private InputField confirmPasswordInput;
        private InputField displayNameInput;

        /// <summary>
        /// The address a pending email change is moving to. Kept only to word the
        /// code board; the server reads the real target from its own pending
        /// record, so nothing here can redirect a change.
        /// </summary>
        private string pendingNewEmail;

        /// <summary>
        /// One request at a time. Without it a double tap on Enter sends two
        /// code requests, and the second is refused by the resend cooldown -
        /// which the player reads as the button being broken.
        /// </summary>
        private bool busy;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            EnsureEventSystem();
            EnsureCanvas();
            Build();
            root.SetActive(false);
        }

        private void OnEnable()
        {
            if (AuthSession.Instance != null)
                AuthSession.Instance.SessionChanged += RefreshDetails;
        }

        private void OnDisable()
        {
            if (AuthSession.Instance != null)
                AuthSession.Instance.SessionChanged -= RefreshDetails;
        }

        // --------------------------------------------------------- showing

        public void Show()
        {
            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
                return;

            root.SetActive(true);
            root.transform.SetAsLastSibling();

            statusText.text = "";
            busy = false;
            ShowDetails();
        }

        public void Hide()
        {
            root.SetActive(false);
            ClearInputs();
        }

        private void ShowDetails()
        {
            RefreshDetails();
            OnlyShow(detailsPanel);
        }

        private void OnlyShow(GameObject panel)
        {
            detailsPanel.SetActive(panel == detailsPanel);
            emailPanel.SetActive(panel == emailPanel);
            verifyPanel.SetActive(panel == verifyPanel);
            passwordPanel.SetActive(panel == passwordPanel);
            displayNamePanel.SetActive(panel == displayNamePanel);
        }

        /// <summary>
        /// Repaints the two planks from the live session. Also runs on every
        /// session change, so a name or address edited here lands on the board
        /// without this panel having to be told which change happened.
        /// </summary>
        private void RefreshDetails()
        {
            UserResponseDto user = AuthSession.Instance != null ? AuthSession.Instance.CurrentUser : null;

            displayNameValue.text = user != null && !string.IsNullOrWhiteSpace(user.displayName)
                ? user.displayName
                : "Unnamed Farmer";

            emailValue.text = user != null && !string.IsNullOrWhiteSpace(user.email)
                ? user.email
                : "-";
        }

        private void ClearInputs()
        {
            newEmailInput.text = "";
            verifyCodeInput.text = "";
            currentPasswordInput.text = "";
            newPasswordInput.text = "";
            confirmPasswordInput.text = "";
            displayNameInput.text = "";
            pendingNewEmail = null;
        }

        private void ShowStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        /// <summary>
        /// Ends a request: clears the busy latch and reports what went wrong.
        /// Every failure path goes through here so none of them can leave the
        /// panel stuck refusing to send anything again.
        /// </summary>
        private void Fail(string message)
        {
            busy = false;
            ShowStatus(string.IsNullOrWhiteSpace(message) ? "That did not work. Please try again." : message);
        }

        // ---------------------------------------------------- change email

        private void OnChangeEmailPressed()
        {
            statusText.text = "";
            newEmailInput.text = "";
            OnlyShow(emailPanel);
        }

        private void OnSubmitNewEmail()
        {
            if (busy)
                return;

            string newEmail = newEmailInput.text.Trim();

            // Only the form of the address is checked here, by the rules the
            // server applies to any address. Whether it is free is the server's
            // call, and whether it can receive mail is settled by the code
            // arriving at it.
            string problem = AccountRules.ChangeEmailProblem(newEmail);
            if (problem != null)
            {
                ShowStatus(problem);
                return;
            }

            busy = true;
            ShowStatus("Sending a code to " + newEmail + "...");

            StartCoroutine(AuthSession.Instance.RequestEmailChange(
                newEmail,
                response =>
                {
                    busy = false;
                    pendingNewEmail = newEmail;
                    ShowVerify("We emailed a code to\n" + newEmail + "\nEnter it below to continue.");
                    ShowStatus(CodeSentStatus(response));
                },
                Fail));
        }

        private void ShowVerify(string info)
        {
            verifyInfo.text = info;
            verifyCodeInput.text = "";
            OnlyShow(verifyPanel);
        }

        private void OnVerifyEmailChange()
        {
            if (busy)
                return;

            string code = verifyCodeInput.text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowStatus("Enter the code from your email.");
                return;
            }

            busy = true;
            ShowStatus("Verifying...");

            StartCoroutine(AuthSession.Instance.ConfirmEmailChange(
                code,
                () =>
                {
                    busy = false;
                    pendingNewEmail = null;
                    ClearInputs();
                    ShowDetails();
                    ShowStatus("Your email has been changed.");
                },
                Fail));
        }

        private void OnResendEmailCode()
        {
            if (busy || string.IsNullOrWhiteSpace(pendingNewEmail))
                return;

            busy = true;
            ShowStatus("Resending code...");

            StartCoroutine(AuthSession.Instance.RequestEmailChange(
                pendingNewEmail,
                response =>
                {
                    busy = false;
                    ShowStatus(CodeSentStatus(response));
                },
                Fail));
        }

        private void OnVerifyBack()
        {
            pendingNewEmail = null;
            statusText.text = "";
            OnlyShow(emailPanel);
        }

        // ------------------------------------------------- change password

        private void OnChangePasswordPressed()
        {
            statusText.text = "";
            currentPasswordInput.text = "";
            newPasswordInput.text = "";
            confirmPasswordInput.text = "";
            OnlyShow(passwordPanel);
        }

        private void OnSubmitPassword()
        {
            if (busy)
                return;

            string current = currentPasswordInput.text;
            string next = newPasswordInput.text;
            string confirm = confirmPasswordInput.text;

            // Checked here as well as on the server so a mismatch, or a new
            // password the server would refuse, is caught before the current
            // password is sent anywhere. Whether the current one is right is
            // only the server's to say.
            string problem = AccountRules.ChangePasswordProblem(current, next, confirm);
            if (problem != null)
            {
                ShowStatus(problem);
                return;
            }

            busy = true;
            ShowStatus("Changing your password...");

            StartCoroutine(AuthSession.Instance.ChangePassword(
                current, next, confirm,
                message =>
                {
                    busy = false;
                    ClearInputs();
                    ShowDetails();
                    ShowStatus(message);
                },
                Fail));
        }

        // --------------------------------------------- change display name

        private void OnChangeDisplayNamePressed()
        {
            statusText.text = "";

            UserResponseDto user = AuthSession.Instance != null ? AuthSession.Instance.CurrentUser : null;
            displayNameInput.text = user != null && user.displayName != null ? user.displayName : "";

            OnlyShow(displayNamePanel);
        }

        private void OnSubmitDisplayName()
        {
            if (busy)
                return;

            string displayName = displayNameInput.text.Trim();

            // AccountRules copies the server's display-name rules, including why
            // an @ sign is refused. Whether the name is taken, or already yours,
            // is the server's call.
            string problem = AccountRules.ChangeDisplayNameProblem(displayName);
            if (problem != null)
            {
                ShowStatus(problem);
                return;
            }

            busy = true;
            ShowStatus("Changing your display name...");

            StartCoroutine(AuthSession.Instance.ChangeDisplayName(
                displayName,
                () =>
                {
                    busy = false;
                    ClearInputs();
                    ShowDetails();
                    ShowStatus("Your display name has been changed.");
                },
                Fail));
        }

        private void OnSubPanelBack()
        {
            statusText.text = "";
            ClearInputs();
            ShowDetails();
        }

        private static string CodeSentStatus(CodeRequestResponseDto response)
        {
            string message = response != null && !string.IsNullOrWhiteSpace(response.message)
                ? response.message
                : "Code sent. Check your email.";

            // Only ever filled in when the server was deliberately started with
            // APP_VERIFICATION_EXPOSE_CODE=true; a normal deployment never sends it.
            if (response != null && !string.IsNullOrWhiteSpace(response.devCode))
                message += "  (dev code: " + response.devCode + ")";

            return message;
        }

        // ---------------------------------------------------------- building

        private void Build()
        {
            theme = UIThemeSprites.Instance;

            root = new GameObject("AccountUI_Runtime", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);
            // Swallows every press behind the board, so the menu underneath
            // cannot be driven while an account change is in progress.
            blocker.raycastTarget = true;

            detailsPanel = CreatePanel("AccountDetailsPanel", new Vector2(PanelWidth, DetailsHeight));
            BuildDetails(detailsPanel.transform);

            emailPanel = CreatePanel("ChangeEmailPanel", new Vector2(PanelWidth, 520f));
            BuildChangeEmail(emailPanel.transform);

            verifyPanel = CreatePanel("AccountVerifyPanel", new Vector2(PanelWidth, 600f));
            BuildVerify(verifyPanel.transform);

            passwordPanel = CreatePanel("ChangePasswordPanel", new Vector2(PanelWidth, 680f));
            BuildChangePassword(passwordPanel.transform);

            displayNamePanel = CreatePanel("ChangeDisplayNamePanel", new Vector2(PanelWidth, 520f));
            BuildChangeDisplayName(displayNamePanel.transform);

            // Built last so it draws over the boards rather than under them.
            // Sibling order is what decides that here, and a server message that
            // ends up behind a plank is a message the player never reads.
            BuildStatusLine();

            OnlyShow(detailsPanel);
        }

        /// <summary>
        /// The one line that carries every server message, stretched to the
        /// screen rather than given a fixed width: these are the longest strings
        /// on the panel and they must not lose their tail at any text size.
        /// </summary>
        private void BuildStatusLine()
        {
            statusText = CreateText(root.transform, "Status", 24, TextAnchor.MiddleCenter);
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = statusText.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            // Pinned low and kept shallow so that at the 1.20 interface ceiling,
            // where only 900 units of height exist, it still clears the bottom of
            // the tallest board rather than sitting across its Enter button.
            rect.sizeDelta = new Vector2(-120f, 100f);
            rect.anchoredPosition = new Vector2(0f, 12f);
        }

        private void BuildDetails(Transform parent)
        {
            CreateHeading(parent, "Account Details");

            CreateDetailRow(parent, "DisplayNameRow", "Display Name", -126f, out displayNameValue);
            CreateDetailRow(parent, "EmailRow", "Email", -282f, out emailValue);

            // 96 tall against a 290 width is a 3.0 ratio to the plank's own 2.53,
            // near enough that the wood does not read as stretched. Sliced rather
            // than aspect-preserved because a button has to be the size the row
            // gives it, and with no border regions on this sprite the two modes
            // differ only in whether that size is honoured.
            Vector2 halfButton = new Vector2(300f, 100f);
            UIPlank.CreateButton(parent, "ChangeEmailButton", theme?.tradeRequestBoard, true,
                halfButton, new Vector2(-165f, -446f), "Change Email", 24, OnChangeEmailPressed);
            UIPlank.CreateButton(parent, "ChangePasswordButton", theme?.tradeRequestBoard, true,
                halfButton, new Vector2(165f, -446f), "Change Password", 24, OnChangePasswordPressed);
            UIPlank.CreateButton(parent, "ChangeDisplayNameButton", theme?.tradeRequestBoard, true,
                new Vector2(380f, 100f), new Vector2(0f, -554f), "Change Display Name", 24,
                OnChangeDisplayNamePressed);

            // Back rather than Enter: this board only reports and dispatches, so
            // there is nothing on it to confirm - leaving is all the button does.
            CreateButton(parent, "AccountClose", new Vector2(0f, -668f),
                theme?.verifyBackButton, new Vector2(280f, 95f), Hide);
        }

        /// <summary>
        /// One labelled value: the wide tutorial plank with a small trade plank
        /// resting on it for the caption and the value written underneath.
        /// </summary>
        private void CreateDetailRow(Transform parent, string name, string caption, float y, out Text value)
        {
            Vector2 rowSize = UIPlank.SizeFor(theme?.tutorialObjectiveBoard, RowWidth, 130f);

            GameObject row = new GameObject(name, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);

            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = rowSize;
            rect.anchoredPosition = new Vector2(0f, y);

            Image image = row.GetComponent<Image>();
            if (theme?.tutorialObjectiveBoard != null)
            {
                image.sprite = theme.tutorialObjectiveBoard;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.30f, 0.19f, 0.09f, 0.96f);
            }
            image.raycastTarget = false;

            // Caption plank on top, value written on the bare wood below it - the
            // arrangement the mock-up uses. Fonts are kept small deliberately:
            // this is the tightest pair of text boxes on the panel, and at the
            // 1.15 text ceiling a line of Playpen Sans is about 45 units tall.
            UIPlank.Create(row.transform, "Caption", theme?.tradeRequestBoard, true,
                new Vector2(250f, 84f), new Vector2(0f, -6f), caption, 21, out _);

            value = CreateText(row.transform, "Value", 22, TextAnchor.UpperCenter);
            RectTransform valueRect = value.rectTransform;
            valueRect.anchorMin = valueRect.anchorMax = new Vector2(0.5f, 1f);
            valueRect.pivot = new Vector2(0.5f, 1f);
            valueRect.sizeDelta = new Vector2(rowSize.x * 0.88f, rowSize.y - 96f);
            valueRect.anchoredPosition = new Vector2(0f, -94f);
            value.horizontalOverflow = HorizontalWrapMode.Wrap;
            value.verticalOverflow = VerticalWrapMode.Overflow;
            value.fontStyle = FontStyle.Bold;
        }

        private void BuildChangeEmail(Transform parent)
        {
            CreateHeading(parent, "Change Email");
            // Each hint on these boards states the rule the server enforces, as the
            // sign-up board does; AccountRules holds the rules themselves. These
            // fields leave about 663 units for text, and every hint was measured to
            // stay on one line at the largest text size - a hint that wraps spills
            // its second line out of the field.
            newEmailInput = CreateInput(parent, "New email (e.g. name@gmail.com)", false, -170f);

            CreateButton(parent, "NewEmailEnter", new Vector2(-150f, -300f),
                theme?.enterButton, PairButtonSize, OnSubmitNewEmail);
            CreateButton(parent, "NewEmailBack", new Vector2(150f, -300f),
                theme?.verifyBackButton, PairButtonSize, OnSubPanelBack);
        }

        /// <summary>
        /// The code board for an email change. No hanging sign: the painted one
        /// reads "Enter Login Code", and the line below says where the code went
        /// anyway.
        /// </summary>
        private void BuildVerify(Transform parent)
        {
            verifyInfo = CreateText(parent, "Info", 24, TextAnchor.UpperCenter);
            RectTransform infoRect = verifyInfo.rectTransform;
            infoRect.anchorMin = infoRect.anchorMax = new Vector2(0.5f, 1f);
            infoRect.pivot = new Vector2(0.5f, 1f);
            infoRect.sizeDelta = new Vector2(BoardContentWidth(PanelWidth) * 0.96f, 140f);
            infoRect.anchoredPosition = new Vector2(0f, -60f);
            verifyInfo.horizontalOverflow = HorizontalWrapMode.Wrap;
            verifyInfo.verticalOverflow = VerticalWrapMode.Overflow;
            verifyInfo.text = "";

            verifyCodeInput = CreateInput(parent, "Enter code", false, -220f);

            CreateButton(parent, "AccountVerify", new Vector2(-150f, -330f),
                theme?.verifyButton, PairButtonSize, OnVerifyEmailChange);
            CreateButton(parent, "AccountVerifyBack", new Vector2(150f, -330f),
                theme?.verifyBackButton, PairButtonSize, OnVerifyBack);
            CreateButton(parent, "AccountResend", new Vector2(0f, -445f),
                theme?.resendCodeButton, WideButtonSize, OnResendEmailCode);
        }

        private void BuildChangePassword(Transform parent)
        {
            CreateHeading(parent, "Change Password");
            // The current password has no rule to show - the server only needs it
            // filled in - so its hint just says which password it wants. The new
            // one reads the same as on the forgotten-password reset board.
            currentPasswordInput = CreateInput(parent, "Current password", true, -160f);
            newPasswordInput = CreateInput(parent, "New password (min. 8 characters)", true, -250f);
            confirmPasswordInput = CreateInput(parent, "Confirm new password (must match)", true, -340f);

            CreateButton(parent, "PasswordEnter", new Vector2(-150f, -460f),
                theme?.enterButton, PairButtonSize, OnSubmitPassword);
            CreateButton(parent, "PasswordBack", new Vector2(150f, -460f),
                theme?.verifyBackButton, PairButtonSize, OnSubPanelBack);
        }

        private void BuildChangeDisplayName(Transform parent)
        {
            CreateHeading(parent, "Change Display Name");
            displayNameInput = CreateInput(parent, "New display name (3-80, no @)", false, -170f);

            CreateButton(parent, "DisplayNameEnter", new Vector2(-150f, -300f),
                theme?.enterButton, PairButtonSize, OnSubmitDisplayName);
            CreateButton(parent, "DisplayNameBack", new Vector2(150f, -300f),
                theme?.verifyBackButton, PairButtonSize, OnSubPanelBack);
        }

        private void CreateHeading(Transform parent, string text)
        {
            UIPlank.Create(parent, "Heading", theme?.tutorialObjectiveBoard, false,
                UIPlank.SizeFor(theme?.tutorialObjectiveBoard, HeadingWidth, 100f),
                new Vector2(0f, -20f), text, 28, out _);
        }

        // ------------------------------------------------------- primitives

        private float PaddingX => theme != null ? theme.panelPaddingX : 170f;

        private GameObject CreatePanel(string name, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            Sprite board = theme?.tradeConfirmBoard != null ? theme.tradeConfirmBoard : theme?.panelBoard;

            if (board != null)
            {
                // Sliced so the rolled log ends keep their true size at every
                // board height; only the middle planks stretch.
                image.sprite = board;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.03f, 0.08f, 0.04f, 0.96f);
            }

            return panel;
        }

        private InputField CreateInput(Transform parent, string placeholderText, bool password, float y)
        {
            GameObject go = new GameObject(placeholderText,
                typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            // Measured off the board rather than assumed, and pulled in a little
            // further so the white field never touches the wood beside it.
            rect.sizeDelta = new Vector2(BoardContentWidth(PanelWidth) * 0.96f, 70f);
            rect.anchoredPosition = new Vector2(0f, y);
            go.GetComponent<Image>().color = Color.white;

            // Pinned to Overflow, never Truncate: Unity discards a line that does
            // not fit rather than clipping it, so at the largest text size the
            // field would draw nothing while still taking keystrokes.
            Text text = CreateText(go.transform, "Text", 25, TextAnchor.MiddleLeft);
            text.color = Color.black;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            StretchWithPadding(text.rectTransform, 16f, 8f);

            Text placeholder = CreateText(go.transform, "Placeholder", 25, TextAnchor.MiddleLeft);
            placeholder.color = new Color(0f, 0f, 0f, 0.45f);
            placeholder.text = placeholderText;
            placeholder.verticalOverflow = VerticalWrapMode.Overflow;
            StretchWithPadding(placeholder.rectTransform, 16f, 8f);

            InputField input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            return input;
        }

        private void CreateButton(Transform parent, string label, Vector2 position, Sprite sprite,
            Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            if (sprite != null)
            {
                // The word is painted into the art, so no Text child is added.
                image.sprite = sprite;
                image.preserveAspect = true;
                image.color = Color.white;

                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
                colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
                colors.selectedColor = Color.white;
                button.colors = colors;
                return;
            }

            image.color = new Color(0.12f, 0.55f, 0.20f, 1f);
            Text text = CreateText(go.transform, "Text", 27, TextAnchor.MiddleCenter);
            text.text = label;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(text.rectTransform);
        }

        private static Text CreateText(Transform parent, string name, int size, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private void EnsureCanvas()
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
                return;

            GameObject go = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchWithPadding(RectTransform rect, float x, float y)
        {
            Stretch(rect);
            rect.offsetMin = new Vector2(x, y);
            rect.offsetMax = new Vector2(-x, -y);
        }
    }
}
