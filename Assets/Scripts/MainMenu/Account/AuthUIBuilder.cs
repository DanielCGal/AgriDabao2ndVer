using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class AuthUIBuilder : MonoBehaviour
    {
        public static AuthUIBuilder Instance { get; private set; }

        private enum AuthMode { Signup, Login, ForgotPassword }

        private UIThemeSprites theme;

        private float PaddingX => theme != null ? theme.panelPaddingX : 90f;
        private float LabelHeight => theme != null ? theme.labelHeight : 150f;
        private float LabelOffsetY => theme != null ? theme.labelOffsetY : -12f;
        private Vector2 ButtonSize => theme != null ? theme.buttonSize : new Vector2(280f, 95f);
        private Vector2 WideButtonSize => theme != null ? theme.wideButtonSize : new Vector2(320f, 95f);

        private Canvas canvas;
        private GameObject authRoot;
        private GameObject loginPanel;
        private GameObject signupPanel;
        private GameObject verifyPanel;
        private Text statusText;

        private InputField loginEmail;
        private InputField loginPassword;

        private InputField signupEmail;
        private InputField signupPassword;
        private InputField signupConfirmPassword;
        private InputField signupDisplayName;

        // Date of birth is three number boxes rather than one text box: typing
        // "2004-03-12" by hand on a phone keyboard is slow and easy to get wrong,
        // and the dashes were a common cause of the format error.
        private InputField signupBirthYear;
        private InputField signupBirthMonth;
        private InputField signupBirthDay;

        private Text verifyHeading;
        private GameObject verifyHeadingSign;
        private GameObject verifySignupHeading;
        private Text verifyInfo;
        private InputField verifyCode;

        private GameObject forgotPanel;
        private InputField forgotIdentifier;

        private GameObject resetPanel;
        private InputField resetPassword;
        private InputField resetConfirmPassword;

        // Pending flow the code-entry panel is confirming.
        private AuthMode pendingMode;
        private string pendingEmail;
        private RegisterRequestDto pendingRegister;
        private LoginRequestDto pendingLogin;

        // Recovery keeps the typed identifier rather than an address: a player
        // who started from their display name is never told the email, so that
        // is the only thing both the confirm and resend calls can be keyed on.
        private string pendingResetIdentifier;
        private string resetTicket;

        /// <summary>
        /// One recovery request in flight at a time. Sign-up and login lean on
        /// AuthSession.IsBusy for this; recovery keeps its own flag so a request
        /// that never comes back cannot leave that shared one raised, which would
        /// stop the player opening their farm afterwards.
        /// </summary>
        private bool recovering;

        private void Awake()
        {
            Instance = this;
            EnsureEventSystem();
            EnsureCanvas();
            Build();
            HideAll();
        }

        public void ShowLogin()
        {
            if (AuthSession.Instance != null &&
                AuthSession.Instance.IsAuthenticated)
            {
                return;
            }

            authRoot.SetActive(true);
            authRoot.transform.SetAsLastSibling();

            statusText.text = "";
            recovering = false;
            OnlyShow(loginPanel);
        }

        public void ShowSignup()
        {
            authRoot.SetActive(true);
            authRoot.transform.SetAsLastSibling();

            statusText.text = "";
            OnlyShow(signupPanel);
        }

        /// <summary>
        /// The "enter your email or display name" board, reached from Forgot
        /// Password on the login board.
        /// </summary>
        private void ShowForgot()
        {
            authRoot.SetActive(true);
            authRoot.transform.SetAsLastSibling();

            statusText.text = "";

            // Whatever is in the box is left alone, so stepping back here from
            // the code board does not wipe the identifier that got you there.
            resetTicket = null;
            recovering = false;
            OnlyShow(forgotPanel);
        }

        /// <summary>
        /// The code board. <paramref name="info"/> is what the server said about
        /// where the code went - the full address for sign-up and login, a masked
        /// one for recovery, which is why it is quoted rather than rebuilt here.
        /// </summary>
        private void ShowVerify(AuthMode mode, string email, string info)
        {
            pendingMode = mode;
            pendingEmail = email;

            authRoot.SetActive(true);
            authRoot.transform.SetAsLastSibling();

            OnlyShow(verifyPanel);

            // The painted sign says "Enter Login Code", which is wrong for a
            // password reset, so recovery takes it down and lets the line below
            // carry the wording on its own.
            // Named apart from the "recovering" field, which is the in-flight
            // latch and a different thing entirely.
            bool isRecovery = mode == AuthMode.ForgotPassword;
            bool isSignup = mode == AuthMode.Signup;

            // The sign is wrong for sign-up as well, so sign-up swaps it for a
            // plank that says "Enter Sign-up Code" (ISS-16). Login keeps the sign.
            if (verifyHeadingSign != null)
                verifyHeadingSign.SetActive(!isRecovery && !isSignup);
            if (verifySignupHeading != null)
                verifySignupHeading.SetActive(isSignup);

            // Null once the painted sign is in use - the sign carries the
            // wording, and the info line below still names the flow.
            if (verifyHeading != null)
            {
                verifyHeading.gameObject.SetActive(!isRecovery);
                verifyHeading.text = mode == AuthMode.Signup ? "Enter Sign-up Code" : "Enter Login Code";
            }

            verifyInfo.text = info;
            verifyCode.text = "";
        }

        private static string CodeSentTo(string email)
        {
            return "We emailed a code to\n" + email + "\nEnter it below to continue.";
        }

        /// <summary>Brings one board to the front and puts every other one away.</summary>
        private void OnlyShow(GameObject panel)
        {
            loginPanel.SetActive(panel == loginPanel);
            signupPanel.SetActive(panel == signupPanel);
            verifyPanel.SetActive(panel == verifyPanel);
            forgotPanel.SetActive(panel == forgotPanel);
            resetPanel.SetActive(panel == resetPanel);
        }

        public void ShowStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        public void HideAll()
        {
            loginPanel?.SetActive(false);
            signupPanel?.SetActive(false);
            verifyPanel?.SetActive(false);
            forgotPanel?.SetActive(false);
            resetPanel?.SetActive(false);
            authRoot?.SetActive(false);

            // The reset ticket only ever authorises the one password change it
            // was issued for, so it is dropped the moment this board closes.
            resetTicket = null;
            recovering = false;
        }

        private void OnLoginPressed()
        {
            LoginRequestDto request = new LoginRequestDto
            {
                identifier = loginEmail.text.Trim(),
                password = loginPassword.text
            };

            if (string.IsNullOrWhiteSpace(request.identifier) || string.IsNullOrWhiteSpace(request.password))
            {
                ShowStatus("Enter your email or display name, and your password.");
                return;
            }

            pendingLogin = request;
            ShowStatus("Sending login code...");
            StartCoroutine(AuthSession.Instance.RequestLogin(
                request,
                response =>
                {
                    // The account's real address, which the server returns once
                    // the password has checked out. A player who signed in with
                    // their display name has never seen it, and the confirm call
                    // is keyed on it, so falling back to what they typed would
                    // leave them unable to finish.
                    string email = response != null && !string.IsNullOrWhiteSpace(response.email)
                        ? response.email
                        : request.identifier;

                    ShowVerify(AuthMode.Login, email, CodeSentTo(email));
                    ShowStatus(CodeSentStatus(response));
                },
                ShowStatus));
        }

        // ------------------------------------------------- password recovery

        private void OnForgotPasswordPressed()
        {
            // Carried over so a player who typed their email and then realised
            // they have forgotten the password does not type it again.
            string typed = loginEmail.text.Trim();
            ShowForgot();
            forgotIdentifier.text = typed;
        }

        private void OnForgotSubmitPressed()
        {
            if (recovering)
                return;

            string identifier = forgotIdentifier.text.Trim();
            if (string.IsNullOrWhiteSpace(identifier))
            {
                ShowStatus("Enter your email or display name.");
                return;
            }

            recovering = true;
            pendingResetIdentifier = identifier;
            ShowStatus("Sending reset code...");
            StartCoroutine(AuthSession.Instance.RequestPasswordReset(
                identifier,
                response =>
                {
                    recovering = false;
                    // The server's own wording, because it names the account in
                    // masked form and only it knows the address behind a display
                    // name. Rebuilding the sentence here would either print the
                    // display name back at the player or leak the address.
                    string info = response != null && !string.IsNullOrWhiteSpace(response.message)
                        ? response.message + "\nEnter it below to continue."
                        : "We emailed you a code.\nEnter it below to continue.";

                    ShowVerify(AuthMode.ForgotPassword, null, info);
                    ShowStatus(CodeSentStatus(response));
                },
                FailRecovery));
        }

        /// <summary>Clears the recovery latch, then reports why it stopped.</summary>
        private void FailRecovery(string message)
        {
            recovering = false;
            ShowStatus(message);
        }

        private void OnForgotBackPressed()
        {
            ShowLogin();
        }

        /// <summary>
        /// Opened once the reset code is accepted. The ticket earned there is
        /// what authorises the change, so this board is never reachable without
        /// having passed the code first.
        /// </summary>
        private void ShowResetPassword()
        {
            authRoot.SetActive(true);
            authRoot.transform.SetAsLastSibling();

            resetPassword.text = "";
            resetConfirmPassword.text = "";
            OnlyShow(resetPanel);
        }

        private void OnResetSubmitPressed()
        {
            if (recovering)
                return;

            string password = resetPassword.text;
            string confirm = resetConfirmPassword.text;

            // The same password rules as sign-up, checked before the round trip.
            string problem = AccountRules.ResetPasswordProblem(password, confirm);
            if (problem != null)
            {
                ShowStatus(problem);
                return;
            }

            if (string.IsNullOrWhiteSpace(resetTicket))
            {
                ShowStatus("Your password reset has expired. Please start again.");
                ShowLogin();
                return;
            }

            recovering = true;
            ShowStatus("Changing your password...");
            StartCoroutine(AuthSession.Instance.CompletePasswordReset(
                resetTicket,
                password,
                confirm,
                () =>
                {
                    recovering = false;
                    // Spent. Keeping it would leave a usable ticket lying in
                    // memory for the rest of the session.
                    resetTicket = null;
                    ShowStatus("Password changed. You are signed in.");
                    HideAll();
                },
                FailRecovery));
        }

        private void OnResetBackPressed()
        {
            resetTicket = null;
            ShowLogin();
        }

        private void OnSignupPressed()
        {
            string email = signupEmail.text.Trim();
            string password = signupPassword.text;
            string confirmPassword = signupConfirmPassword.text;
            string displayName = signupDisplayName.text.Trim();

            // Checked here first so a bad value is caught the moment Create is
            // pressed, rather than after a trip to the server. The server still
            // checks everything again; AccountRules explains why the two have to
            // stay in step.
            string problem = AccountRules.SignupProblem(
                email, password, confirmPassword, displayName);
            if (problem != null)
            {
                ShowStatus(problem);
                return;
            }

            // The date row sits below the other fields, so it is checked after
            // them: the first message a player sees always belongs to the highest
            // field that needs fixing.
            if (!TryBuildDateOfBirth(out string dateOfBirth, out string dateError))
            {
                ShowStatus(dateError);
                return;
            }

            RegisterRequestDto request = new RegisterRequestDto
            {
                email = email,
                password = password,
                confirmPassword = confirmPassword,
                displayName = displayName,
                dateOfBirth = dateOfBirth
            };

            pendingRegister = request;
            ShowStatus("Sending sign-up code...");
            StartCoroutine(AuthSession.Instance.RequestRegister(
                request,
                response =>
                {
                    ShowVerify(AuthMode.Signup, request.email, CodeSentTo(request.email));
                    ShowStatus(CodeSentStatus(response));
                },
                ShowStatus));
        }

        private void OnVerifyPressed()
        {
            string code = verifyCode.text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowStatus("Enter the code from your email.");
                return;
            }

            ShowStatus("Verifying...");
            if (pendingMode == AuthMode.Signup)
            {
                StartCoroutine(AuthSession.Instance.VerifyRegister(
                    pendingEmail,
                    code,
                    () =>
                    {
                        ShowStatus("Account created and signed in.");
                        HideAll();
                    },
                    ShowStatus));
            }
            else if (pendingMode == AuthMode.ForgotPassword)
            {
                if (recovering)
                    return;

                recovering = true;
                StartCoroutine(AuthSession.Instance.VerifyPasswordReset(
                    pendingResetIdentifier,
                    code,
                    ticket =>
                    {
                        recovering = false;
                        resetTicket = ticket != null ? ticket.resetToken : null;
                        ShowStatus("Code accepted. Choose a new password.");
                        ShowResetPassword();
                    },
                    FailRecovery));
            }
            else
            {
                StartCoroutine(AuthSession.Instance.VerifyLogin(
                    pendingEmail,
                    code,
                    () =>
                    {
                        ShowStatus("Login successful.");
                        HideAll();
                    },
                    ShowStatus));
            }
        }

        private void OnResendPressed()
        {
            ShowStatus("Resending code...");
            if (pendingMode == AuthMode.Signup && pendingRegister != null)
            {
                StartCoroutine(AuthSession.Instance.RequestRegister(
                    pendingRegister,
                    response => ShowStatus(CodeSentStatus(response)),
                    ShowStatus));
            }
            else if (pendingMode == AuthMode.ForgotPassword &&
                     !string.IsNullOrWhiteSpace(pendingResetIdentifier))
            {
                if (recovering)
                    return;

                recovering = true;
                StartCoroutine(AuthSession.Instance.RequestPasswordReset(
                    pendingResetIdentifier,
                    response =>
                    {
                        recovering = false;
                        ShowStatus(CodeSentStatus(response));
                    },
                    FailRecovery));
            }
            else if (pendingMode == AuthMode.Login && pendingLogin != null)
            {
                StartCoroutine(AuthSession.Instance.RequestLogin(
                    pendingLogin,
                    response => ShowStatus(CodeSentStatus(response)),
                    ShowStatus));
            }
        }

        private void OnVerifyBackPressed()
        {
            if (pendingMode == AuthMode.Signup)
                ShowSignup();
            else if (pendingMode == AuthMode.ForgotPassword)
                ShowForgot();
            else
                ShowLogin();
        }

        private static string CodeSentStatus(CodeRequestResponseDto response)
        {
            string message = response != null && !string.IsNullOrWhiteSpace(response.message)
                ? response.message
                : "Code sent. Check your email.";

            // devCode is only ever non-empty when the server was deliberately started
            // with APP_VERIFICATION_EXPOSE_CODE=true (demo/dev mode) - the server is
            // the real gate here, so showing it on every platform (not just the
            // Editor) is safe: a normally-configured deployment never sends this field.
            if (response != null && !string.IsNullOrWhiteSpace(response.devCode))
                message += "  (dev code: " + response.devCode + ")";

            return message;
        }

        private void Build()
        {
            theme = UIThemeSprites.Instance;

            authRoot = new GameObject("AuthUI_Runtime", typeof(RectTransform), typeof(Image));
            authRoot.transform.SetParent(canvas.transform, false);
            Stretch(authRoot.GetComponent<RectTransform>());

            Image blocker = authRoot.GetComponent<Image>();
            // Lighter than before so the painted farm background still reads
            // behind the wooden boards.
            blocker.color = new Color(0f, 0f, 0f, 0.45f);
            blocker.raycastTarget = true;

            statusText = CreateText(authRoot.transform, "Status", 24, TextAnchor.MiddleCenter);
            // This line carries the longest strings in the game - a sent-code
            // confirmation with the address in it, the dev code appended after it,
            // and every backend error message - so it is the one label that must
            // never lose its tail.
            //
            // It used to be a fixed 900x80 box on Unity's default Truncate, which
            // silently ate whatever did not fit. Stretching it to the screen width
            // instead of pinning a width keeps it correct on a 4:3 tablet and at
            // any interface scale, where a fixed 900 could be wider than the canvas
            // itself. Overflow then guarantees a long message grows upward into
            // empty background rather than being cut off.
            statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            statusText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform statusRect = statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0f);
            // Negative width = inset from both screen edges, so this is a 60-unit
            // margin each side rather than a fixed 900.
            statusRect.sizeDelta = new Vector2(-120f, 120f);
            statusRect.anchoredPosition = new Vector2(0f, 25f);

            loginPanel = CreatePanel(authRoot.transform, "LoginPanel", new Vector2(LoginWidth, 600f));
            BuildLogin(loginPanel.transform);

            signupPanel = CreatePanel(authRoot.transform, "SignupPanel", new Vector2(SignupWidth, 880f));
            BuildSignup(signupPanel.transform);

            verifyPanel = CreatePanel(authRoot.transform, "VerifyPanel", new Vector2(VerifyWidth, 720f));
            BuildVerify(verifyPanel.transform);

            forgotPanel = CreatePanel(authRoot.transform, "ForgotPasswordPanel", new Vector2(ForgotWidth, 620f));
            BuildForgot(forgotPanel.transform);

            resetPanel = CreatePanel(authRoot.transform, "ResetPasswordPanel", new Vector2(ResetWidth, 620f));
            BuildReset(resetPanel.transform);
        }

        private const float LoginWidth = 900f;
        private const float SignupWidth = 900f;
        private const float VerifyWidth = 900f;
        private const float ForgotWidth = 900f;
        private const float ResetWidth = 900f;

        /// <summary>
        /// Width of the blank planks the recovery screens write on. They are the
        /// tutorial objective plank, which has no wording painted in, so the text
        /// is drawn live on top - see <see cref="UIPlank"/>.
        ///
        /// Held to the same width as the input boxes below it, which is what
        /// keeps it off the board's rolled logs. BackgroundUI.png is 1024 wide
        /// and its painted log runs to x=157; drawn 9-sliced at 900 that puts the
        /// plank wall's left edge 153 units in, so panelPaddingX of 170 leaves
        /// room to spare and anything wider than 560 climbs onto the wood.
        /// </summary>
        private const float PlankHeadingWidth = 540f;

        private void BuildVerify(Transform parent)
        {
            verifyHeading = CreateHeading(parent, "Enter Code", theme?.enterCodeLabel, VerifyWidth,
                out verifyHeadingSign);

            // Only needed when the painted "Enter Login Code" sign is in use; the
            // plain text heading already says "Enter Sign-up Code" by itself. The
            // same blank plank and position as the recovery boards' headings, and
            // hidden until ShowVerify opens the board for sign-up.
            if (verifyHeadingSign != null)
            {
                verifySignupHeading = UIPlank.Create(parent, "SignupCodeHeading", theme?.tutorialObjectiveBoard, false,
                    UIPlank.SizeFor(theme?.tutorialObjectiveBoard, PlankHeadingWidth, 110f),
                    new Vector2(0f, -24f), "Enter Sign-up Code", 24, out _);
                verifySignupHeading.SetActive(false);
            }

            verifyInfo = CreateText(parent, "Info", 24, TextAnchor.UpperCenter);
            RectTransform infoRect = verifyInfo.rectTransform;
            infoRect.anchorMin = infoRect.anchorMax = new Vector2(0.5f, 1f);
            infoRect.pivot = new Vector2(0.5f, 1f);
            infoRect.sizeDelta = new Vector2(VerifyWidth - PaddingX * 2f, 110f);
            infoRect.anchoredPosition = new Vector2(0f, -180f);
            verifyInfo.text = "";

            verifyCode = CreateInput(parent, "Enter code", false, -320f, VerifyWidth);

            CreateButton(parent, "Verify", new Vector2(-150f, -440f), theme?.verifyButton, ButtonSize, OnVerifyPressed);
            CreateButton(parent, "Back", new Vector2(150f, -440f), theme?.verifyBackButton, ButtonSize, OnVerifyBackPressed);
            CreateButton(parent, "Resend code", new Vector2(0f, -555f), theme?.resendCodeButton, WideButtonSize, OnResendPressed);
        }

        private void BuildLogin(Transform parent)
        {
            CreateHeading(parent, "Player Login", theme?.loginLabel, LoginWidth);
            loginEmail = CreateInput(parent, "Email or Display Name", false, -185f, LoginWidth);
            loginPassword = CreateInput(parent, "Password", true, -275f, LoginWidth);

            // Tucked under the password box on the left, the way it is written on
            // a real sign-in form. Left-aligned rather than centred so it reads as
            // a footnote to the field above it and not as a third button.
            float contentLeft = -(LoginWidth * 0.5f - PaddingX);
            Vector2 forgotSize = UIPlank.SizeFor(theme?.tutorialObjectiveBoard, 300f, 66f);
            UIPlank.CreateButton(parent, "ForgotPassword", theme?.tutorialObjectiveBoard, false,
                forgotSize, new Vector2(contentLeft + forgotSize.x * 0.5f, -358f),
                "Forgot Password?", 22, OnForgotPasswordPressed);

            CreateButton(parent, "Login", new Vector2(-150f, -440f), theme?.loginButton, ButtonSize, OnLoginPressed);
            CreateButton(parent, "Sign Up", new Vector2(150f, -440f), theme?.signUpButton, ButtonSize, ShowSignup);
        }

        /// <summary>
        /// "Enter Email or Display Name" - the first step of recovery.
        ///
        /// A player who has forgotten the address as well as the password is
        /// pointed at the support mailbox rather than left with nowhere to go:
        /// there is no automatic way to prove an account is yours once you can
        /// name neither of the two things that identify it.
        /// </summary>
        private void BuildForgot(Transform parent)
        {
            UIPlank.Create(parent, "ForgotHeading", theme?.tutorialObjectiveBoard, false,
                UIPlank.SizeFor(theme?.tutorialObjectiveBoard, PlankHeadingWidth, 110f),
                new Vector2(0f, -24f), "Enter Email or Display Name", 24, out _);

            forgotIdentifier = CreateInput(parent, "Email or Display Name", false, -200f, ForgotWidth);

            Text help = CreateText(parent, "SupportHint", 21, TextAnchor.UpperCenter);
            RectTransform helpRect = help.rectTransform;
            helpRect.anchorMin = helpRect.anchorMax = new Vector2(0.5f, 1f);
            helpRect.pivot = new Vector2(0.5f, 1f);
            helpRect.sizeDelta = new Vector2(ForgotWidth - PaddingX * 2f, 90f);
            helpRect.anchoredPosition = new Vector2(0f, -290f);
            help.horizontalOverflow = HorizontalWrapMode.Wrap;
            help.verticalOverflow = VerticalWrapMode.Overflow;
            help.text = "Forget Email? Please Contact greenscapeacd@gmail.com\nfor Account Support";

            CreateButton(parent, "ForgotEnter", new Vector2(-150f, -420f),
                theme?.enterButton, ButtonSize, OnForgotSubmitPressed);
            CreateButton(parent, "ForgotBack", new Vector2(150f, -420f),
                theme?.verifyBackButton, ButtonSize, OnForgotBackPressed);
        }

        /// <summary>The new-password board, reached only after the code is accepted.</summary>
        private void BuildReset(Transform parent)
        {
            UIPlank.Create(parent, "ResetHeading", theme?.tutorialObjectiveBoard, false,
                UIPlank.SizeFor(theme?.tutorialObjectiveBoard, PlankHeadingWidth, 110f),
                new Vector2(0f, -24f), "Change Password", 24, out _);

            // Hints state the server's rules, as on the sign-up board, and are
            // measured the same way against the same 528 units of text width.
            // "New password (at least 8 characters)" ran past that at the largest
            // text size, which is why this one says "min." instead.
            resetPassword = CreateInput(parent, "New password (min. 8 characters)", true, -200f, ResetWidth);
            resetConfirmPassword = CreateInput(parent, "Confirm password (must match)", true, -290f, ResetWidth);

            CreateButton(parent, "ResetEnter", new Vector2(-150f, -420f),
                theme?.enterButton, ButtonSize, OnResetSubmitPressed);
            CreateButton(parent, "ResetBack", new Vector2(150f, -420f),
                theme?.verifyBackButton, ButtonSize, OnResetBackPressed);
        }

        private void BuildSignup(Transform parent)
        {
            CreateHeading(parent, "Create Player Account", theme?.createAccountLabel, SignupWidth);
            // Each hint states the rule the server will actually enforce, so a
            // player learns it before pressing Create rather than from a rejection
            // afterwards. The rules come from RegisterRequest and
            // AccountFields.validateDisplayName: email must be a valid address,
            // password 8 to 72 characters, display name 3 to 80 with no @ sign.
            //
            // Wording is measured, not guessed. These fields leave 528 units for
            // text, and the placeholder wraps rather than clipping, with vertical
            // overflow on - so a hint too long for one line spills its second line
            // onto the field below. Every string here stays under that width at the
            // largest text setting. The fuller "Display name (optional, 3-80, no @)"
            // did not, which is why "optional" is left to the fact that a blank
            // name is simply accepted.
            signupEmail = CreateInput(parent, "Email (e.g. name@gmail.com)", false, -175f, SignupWidth);
            signupPassword = CreateInput(parent, "Password (at least 8 characters)", true, -260f, SignupWidth);
            signupConfirmPassword = CreateInput(parent, "Confirm password (must match)", true, -345f, SignupWidth);
            signupDisplayName = CreateInput(parent, "Display name (3-80, no @)", false, -430f, SignupWidth);
            CreateDateOfBirthRow(parent, -515f, SignupWidth);
            CreateButton(parent, "Create", new Vector2(-150f, -650f), theme?.createButton, ButtonSize, OnSignupPressed);
            CreateButton(parent, "Back", new Vector2(150f, -650f), theme?.createBackButton, ButtonSize, ShowLogin);
        }

        private void EnsureCanvas()
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
                return;

            GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // landscape: scale by height
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            Image image = panel.GetComponent<Image>();
            Sprite board = theme?.panelBoard;
            if (board != null)
            {
                // Sliced so the rolled log ends keep their true size at every
                // panel height - only the middle planks stretch.
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

        /// <summary>
        /// The hanging sign above a panel. With a sprite it is a picture (the
        /// wording is painted in) and null is returned; without one it falls
        /// back to a live Text, which is returned so callers can retitle it.
        /// </summary>
        private Text CreateHeading(Transform parent, string value, Sprite sprite, float panelWidth)
        {
            return CreateHeading(parent, value, sprite, panelWidth, out _);
        }

        /// <summary>
        /// As above, and hands back the sign itself so a caller can take it down.
        /// The code board needs that: its sign says "Enter Login Code", which is
        /// the wrong words when the same board is confirming a password reset.
        /// </summary>
        private Text CreateHeading(Transform parent, string value, Sprite sprite, float panelWidth,
            out GameObject signObject)
        {
            signObject = null;

            if (sprite != null)
            {
                GameObject go = new GameObject("HeadingLabel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);

                RectTransform signRect = go.GetComponent<RectTransform>();
                signRect.anchorMin = signRect.anchorMax = new Vector2(0.5f, 1f);
                signRect.pivot = new Vector2(0.5f, 0.5f);
                // preserveAspect fits the sign inside this box, so each sign
                // keeps its own width and they all share one height.
                signRect.sizeDelta = new Vector2(panelWidth - 60f, LabelHeight);
                signRect.anchoredPosition = new Vector2(0f, LabelOffsetY);

                Image image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;

                signObject = go;
                return null;
            }

            Text heading = CreateText(parent, "Heading", 38, TextAnchor.MiddleCenter);
            RectTransform rect = heading.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(700f, 70f);
            rect.anchoredPosition = new Vector2(0f, -25f);
            heading.text = value;
            return heading;
        }

        /// <summary>
        /// Joins the three birth boxes into the "yyyy-MM-dd" the backend expects.
        ///
        /// The field stays optional, exactly as it was when it was one text box:
        /// all three blank returns null and no error.
        /// </summary>
        private bool TryBuildDateOfBirth(out string isoDate, out string error)
        {
            isoDate = null;
            error = null;

            string year = signupBirthYear.text.Trim();
            string month = signupBirthMonth.text.Trim();
            string day = signupBirthDay.text.Trim();

            if (year.Length == 0 && month.Length == 0 && day.Length == 0)
                return true;

            if (year.Length == 0 || month.Length == 0 || day.Length == 0)
            {
                error = "Fill in the year, month and day.";
                return false;
            }

            string candidate =
                year.PadLeft(4, '0') + "-" +
                month.PadLeft(2, '0') + "-" +
                day.PadLeft(2, '0');

            // TryParseExact also rejects dates that do not exist, such as
            // 2004-02-31, which the old single text box would have accepted.
            if (!System.DateTime.TryParseExact(
                    candidate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out _))
            {
                error = "That date does not exist. Check the year, month and day.";
                return false;
            }

            isoDate = candidate;
            return true;
        }

        /// <summary>
        /// Year / month / day side by side on one row, in place of the single
        /// text box. The row spans the same width the old field did, so nothing
        /// above or below it has to move.
        /// </summary>
        private void CreateDateOfBirthRow(Transform parent, float y, float panelWidth)
        {
            const float gap = 16f;

            float inner = panelWidth - PaddingX * 2f;
            float usable = inner - gap * 2f;
            // Month and day take whatever the year leaves, so the three boxes and
            // the two gaps add up to exactly the old field's width - rounding both
            // independently left the day box hanging a pixel past the edge.
            float yearWidth = Mathf.Round(usable * 0.44f);
            float partWidth = (usable - yearWidth) * 0.5f;

            float left = -inner * 0.5f;
            float yearX = left + yearWidth * 0.5f;
            float monthX = left + yearWidth + gap + partWidth * 0.5f;
            float dayX = monthX + partWidth + gap;

            signupBirthYear = CreateNumberInput(parent, "Birth year", "YYYY", yearX, y, yearWidth, 4);
            signupBirthMonth = CreateNumberInput(parent, "Birth month", "MM", monthX, y, partWidth, 2);
            signupBirthDay = CreateNumberInput(parent, "Birth day", "DD", dayX, y, partWidth, 2);
        }

        private InputField CreateNumberInput(Transform parent, string name, string placeholderText,
            float x, float y, float width, int characterLimit)
        {
            InputField input = CreateInputAt(parent, name, placeholderText, x, y, width);

            // IntegerNumber opens the phone's number pad instead of the full
            // keyboard, and refuses anything that is not a digit.
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = characterLimit;

            input.textComponent.alignment = TextAnchor.MiddleCenter;
            if (input.placeholder is Text placeholder)
                placeholder.alignment = TextAnchor.MiddleCenter;

            return input;
        }

        private InputField CreateInput(Transform parent, string placeholderText, bool password, float y, float panelWidth)
        {
            // Inset so the field sits between the board's two log ends.
            InputField input = CreateInputAt(
                parent, placeholderText, placeholderText, 0f, y, panelWidth - PaddingX * 2f);

            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            return input;
        }

        private InputField CreateInputAt(Transform parent, string name, string placeholderText,
            float x, float y, float width)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, 70f);
            rect.anchoredPosition = new Vector2(x, y);
            go.GetComponent<Image>().color = Color.white;

            // Font 25 in the 54 units left after padding has room to spare even at
            // the largest text setting, so unlike the shop's amount box these do not
            // currently run out. They are pinned to Overflow anyway: Unity's default
            // Truncate discards a line that does not fit rather than clipping it, so
            // the failure mode here is a field that silently draws nothing while
            // still accepting keystrokes - and on the login screen that is a player
            // who cannot sign in at all.
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
