using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace AgriDabao3D
{
    public class MainMenuBuilder : MonoBehaviour
    {
        [Header("Sprites - Fallback / UI")]
        public Sprite backgroundSprite;
        public Sprite titleSprite;
        public Sprite startSprite;
        public Sprite settingsSprite;
        public Sprite exitSprite;

        [Header("Videos")]
        public VideoClip introVideoClip;
        public VideoClip mainMenuBackgroundVideoClip;

        [Tooltip("If true, the GreenScape intro only plays once per app session.")]
        public bool playIntroOnlyOnce = true;

        [Header("Startup Fade")]
        [Tooltip("Seconds to hold on solid white after the intro, before the fade begins.")]
        public float whiteHoldSeconds = 0.3f;

        [Tooltip("Seconds for the white cover to fade away and reveal the background art.")]
        public float whiteFadeSeconds = 1.6f;

        [Tooltip("Seconds for the logo and buttons to fade in once the player has logged in.")]
        public float menuFadeInSeconds = 0.7f;

        [Header("Video Render Size")]
        public int videoWidth = 1920;
        public int videoHeight = 1080;

        [Header("Layout")]
        [Range(0.1f, 1f)] public float backgroundFillPercent = 1f;
        [Range(0.1f, 1f)] public float titleWidthPercent = 0.58f;
        [Range(0.1f, 1f)] public float buttonWidthPercent = 0.22f;
        [Range(0.05f, 0.5f)] public float titleTopPercent = 0.14f;
        [Range(0.2f, 0.8f)] public float firstButtonYPercent = 0.52f;
        [Range(0.05f, 0.25f)] public float buttonSpacingPercent = 0.105f;

        [Header("Title Floating Animation")]
        public bool animateTitle = true;
        public float titleFloatAmplitude = 18f;
        public float titleFloatSpeed = 1.25f;

        [Header("Buttons")]
        public bool rebuildOnStart = true;

        private const string RootName = "MainMenu_Runtime";
        private static bool introAlreadyPlayed;

        private Canvas canvas;
        private RectTransform rootRect;
        private RectTransform titleRect;
        private Vector2 titleBasePosition;

        private GameObject backgroundRoot;
        private GameObject menuContentRoot;
        private CanvasGroup menuContentGroup;
        private GameObject whiteCoverRoot;
        private Image whiteCoverImage;
        private GameObject introOverlayRoot;

        private bool menuRevealed;
        private bool revealStarted;

        private VideoPlayer introVideoPlayer;
        private VideoPlayer backgroundVideoPlayer;
        private RawImage backgroundRawImage;

        private RenderTexture introRenderTexture;
        private RenderTexture backgroundRenderTexture;
        private float nextBackgroundVideoCheck;
        private bool backgroundVideoShouldRun;
        private bool backgroundNeedsRestart;
        private bool keyboardWasVisible;
        private long lastBackgroundFrame = -1;
        private float lastBackgroundFrameTime;

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
        }

        private void Start()
        {
            if (rebuildOnStart)
            {
                Build();
            }
        }

        private void Update()
        {
            AnimateTitleCard();
            TickBackgroundVideoWatchdog();
        }

        [ContextMenu("Build Main Menu")]
        public void Build()
        {
            var existing = transform.Find(RootName);
            if (existing != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(existing.gameObject);
#else
                Destroy(existing.gameObject);
#endif
            }

            EnsureEventSystem();
            canvas = EnsureCanvas();

            GameObject root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            backgroundRoot = new GameObject("BackgroundRoot", typeof(RectTransform));
            backgroundRoot.transform.SetParent(rootRect, false);
            Stretch(backgroundRoot.GetComponent<RectTransform>());

            menuContentRoot = new GameObject("MenuContentRoot", typeof(RectTransform), typeof(CanvasGroup));
            menuContentRoot.transform.SetParent(rootRect, false);
            Stretch(menuContentRoot.GetComponent<RectTransform>());
            menuContentGroup = menuContentRoot.GetComponent<CanvasGroup>();

            CreateBackground(backgroundRoot.GetComponent<RectTransform>());
            CreateDim(backgroundRoot.GetComponent<RectTransform>());
            CreateTitle(menuContentRoot.GetComponent<RectTransform>());
            CreateButtons(menuContentRoot.GetComponent<RectTransform>());

            CreateWhiteCover(rootRect);
            SubscribeToSession();

            revealStarted = false;

            bool shouldPlayIntro =
                introVideoClip != null &&
                (!playIntroOnlyOnce || !introAlreadyPlayed);

            if (!shouldPlayIntro &&
                AuthSession.Instance != null && AuthSession.Instance.IsAuthenticated)
            {
                whiteCoverRoot.SetActive(false);
                menuRevealed = true;
                menuContentGroup.alpha = 1f;
                menuContentRoot.SetActive(true);
                StartMenuBackgroundVideo();

                if (SessionTakeover.Consume(out string takeoverMessage))
                    ShowAccountBusyPrompt(takeoverMessage);

                return;
            }

            menuRevealed = false;
            menuContentGroup.alpha = 0f;
            menuContentRoot.SetActive(false);
            ShowWhiteCover();

            if (shouldPlayIntro)
            {
                CreateIntroOverlay(rootRect);
                PlayIntroVideo();
            }
            else
            {
                StartCoroutine(RevealBackgroundThenLogin());
            }
        }

        private void CreateWhiteCover(RectTransform parent)
        {
            whiteCoverRoot = new GameObject("WhiteCover", typeof(RectTransform), typeof(Image));
            whiteCoverRoot.transform.SetParent(parent, false);
            Stretch(whiteCoverRoot.GetComponent<RectTransform>());

            whiteCoverImage = whiteCoverRoot.GetComponent<Image>();
            whiteCoverImage.color = Color.white;
            whiteCoverImage.raycastTarget = true;

            whiteCoverRoot.SetActive(false);
        }

        private void ShowWhiteCover()
        {
            if (whiteCoverRoot == null)
                return;

            whiteCoverRoot.SetActive(true);
            whiteCoverRoot.transform.SetAsLastSibling();
            SetWhiteAlpha(1f);
        }

        private void SetWhiteAlpha(float alpha)
        {
            if (whiteCoverImage == null)
                return;

            Color color = whiteCoverImage.color;
            color.a = alpha;
            whiteCoverImage.color = color;

            whiteCoverImage.raycastTarget = alpha > 0.01f;
        }

        private IEnumerator RevealBackgroundThenLogin()
        {
            if (revealStarted)
                yield break;
            revealStarted = true;

            StartMenuBackgroundVideo();

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, whiteHoldSeconds));

            float duration = Mathf.Max(0.01f, whiteFadeSeconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetWhiteAlpha(1f - Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            SetWhiteAlpha(0f);
            if (whiteCoverRoot != null)
                whiteCoverRoot.SetActive(false);

            ShowAuthenticationIfNeeded();
        }

        private void SubscribeToSession()
        {
            if (AuthSession.Instance == null)
                return;

            AuthSession.Instance.SessionChanged -= HandleSessionChanged;
            AuthSession.Instance.SessionChanged += HandleSessionChanged;
        }

        private void HandleSessionChanged()
        {
            bool authenticated = AuthSession.Instance != null && AuthSession.Instance.IsAuthenticated;

            if (!authenticated)
            {
                if (!menuRevealed)
                    return;

                menuRevealed = false;
                if (menuContentGroup != null)
                    menuContentGroup.alpha = 0f;
                if (menuContentRoot != null)
                    menuContentRoot.SetActive(false);
                return;
            }

            if (menuRevealed)
                return;

            menuRevealed = true;
            StartCoroutine(FadeInMenuContent());
        }

        private IEnumerator FadeInMenuContent()
        {
            if (menuContentRoot == null)
                yield break;

            menuContentRoot.SetActive(true);

            float duration = Mathf.Max(0.01f, menuFadeInSeconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                menuContentGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            menuContentGroup.alpha = 1f;
        }

        private Canvas EnsureCanvas()
        {
            var foundCanvas = FindObjectOfType<Canvas>();
            if (foundCanvas != null)
            {
                var scaler = foundCanvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                    scaler = foundCanvas.gameObject.AddComponent<CanvasScaler>();

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f;

                if (foundCanvas.GetComponent<GraphicRaycaster>() == null)
                    foundCanvas.gameObject.AddComponent<GraphicRaycaster>();

                return foundCanvas;
            }

            GameObject canvasGo = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            Canvas newCanvas = canvasGo.GetComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler canvasScaler = canvasGo.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 1f;

            return newCanvas;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void CreateBackground(RectTransform parent)
        {
            if (mainMenuBackgroundVideoClip != null)
            {
                CreateVideoBackground(parent);
                return;
            }

            Image bg = CreateImage("Background", parent, backgroundSprite);
            RectTransform rect = bg.rectTransform;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            bg.preserveAspect = false;
        }

        private void CreateVideoBackground(RectTransform parent)
        {
            CreateBackgroundFallback(parent);

            GameObject rawGo = new GameObject("MainMenuVideoBackground", typeof(RectTransform), typeof(RawImage));
            rawGo.transform.SetParent(parent, false);

            RectTransform rawRect = rawGo.GetComponent<RectTransform>();
            Stretch(rawRect);

            backgroundRawImage = rawGo.GetComponent<RawImage>();
            backgroundRawImage.color = Color.white;
            backgroundRawImage.raycastTarget = false;

            backgroundRenderTexture = new RenderTexture(videoWidth, videoHeight, 0);
            backgroundRenderTexture.name = "MainMenuBackground_RT";
            backgroundRenderTexture.Create();

            backgroundRawImage.texture = backgroundRenderTexture;

            GameObject playerGo = new GameObject("MainMenuBackgroundVideoPlayer", typeof(VideoPlayer));
            playerGo.transform.SetParent(transform, false);

            backgroundVideoPlayer = playerGo.GetComponent<VideoPlayer>();
            backgroundVideoPlayer.playOnAwake = false;
            backgroundVideoPlayer.isLooping = true;
            backgroundVideoPlayer.skipOnDrop = false;
            backgroundVideoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            backgroundVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            backgroundVideoPlayer.targetTexture = backgroundRenderTexture;
            backgroundVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            backgroundVideoPlayer.clip = mainMenuBackgroundVideoClip;
            backgroundVideoPlayer.waitForFirstFrame = true;
            backgroundVideoPlayer.loopPointReached += OnBackgroundVideoLoopPoint;
            backgroundVideoPlayer.errorReceived += OnBackgroundVideoError;
            backgroundVideoPlayer.started += OnBackgroundVideoStarted;
        }

        private void CreateBackgroundFallback(RectTransform parent)
        {
            Image fallback = CreateImage("BackgroundFallback", parent, backgroundSprite);
            Stretch(fallback.rectTransform);
            fallback.preserveAspect = false;
            fallback.raycastTarget = false;
            if (backgroundSprite == null)
                fallback.color = new Color(0.45f, 0.62f, 0.28f, 1f);
        }

        private void StartMenuBackgroundVideo()
        {
            backgroundVideoShouldRun = true;
            RestoreMenuBackgroundVideo(true);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                backgroundNeedsRestart = true;
            else
                RestoreMenuBackgroundVideo(true);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                backgroundNeedsRestart = true;
            else
                RestoreMenuBackgroundVideo(true);
        }

        private void TickBackgroundVideoWatchdog()
        {
            if (!backgroundVideoShouldRun)
                return;

            bool keyboardVisible = TouchScreenKeyboard.visible;
            if (keyboardWasVisible && !keyboardVisible)
                backgroundNeedsRestart = true;
            keyboardWasVisible = keyboardVisible;

            if (Time.unscaledTime < nextBackgroundVideoCheck && !backgroundNeedsRestart)
                return;

            nextBackgroundVideoCheck = Time.unscaledTime + 0.35f;
            RestoreMenuBackgroundVideo(backgroundNeedsRestart);
        }

        private void RestoreMenuBackgroundVideo(bool force = false)
        {
            if (!backgroundVideoShouldRun)
                return;

            if (backgroundVideoPlayer == null || mainMenuBackgroundVideoClip == null)
                return;

            if (introOverlayRoot != null && introOverlayRoot.activeSelf)
                return;

            backgroundVideoPlayer.isLooping = true;
            backgroundVideoPlayer.skipOnDrop = false;

            bool rtLost = backgroundRenderTexture == null || !backgroundRenderTexture.IsCreated();
            bool notPlaying = !backgroundVideoPlayer.isPlaying;
            bool stalled = IsBackgroundVideoStalled();

            if (!force && !backgroundNeedsRestart && !rtLost && !notPlaying && !stalled)
                return;

            backgroundNeedsRestart = false;

            bool hardRestart = force || rtLost || stalled;
            if (rtLost || stalled)
                RecreateBackgroundRenderTexture();

            backgroundVideoPlayer.clip = mainMenuBackgroundVideoClip;
            backgroundVideoPlayer.isLooping = true;
            backgroundVideoPlayer.skipOnDrop = false;
            backgroundVideoPlayer.targetTexture = backgroundRenderTexture;
            if (backgroundRawImage != null)
            {
                backgroundRawImage.texture = backgroundRenderTexture;
                if (rtLost)
                    backgroundRawImage.enabled = false;
            }

            if (hardRestart && backgroundVideoPlayer.isPlaying)
                backgroundVideoPlayer.Stop();

            backgroundVideoPlayer.Play();
            lastBackgroundFrame = backgroundVideoPlayer.frame;
            lastBackgroundFrameTime = Time.unscaledTime;
        }

        private void RecreateBackgroundRenderTexture()
        {
            if (backgroundRenderTexture == null)
            {
                backgroundRenderTexture = new RenderTexture(videoWidth, videoHeight, 0);
                backgroundRenderTexture.name = "MainMenuBackground_RT";
            }
            else
            {
                backgroundRenderTexture.Release();
            }

            backgroundRenderTexture.Create();
        }

        private bool IsBackgroundVideoStalled()
        {
            if (backgroundVideoPlayer == null || !backgroundVideoPlayer.isPlaying)
                return false;

            long frame = backgroundVideoPlayer.frame;
            if (frame != lastBackgroundFrame)
            {
                lastBackgroundFrame = frame;
                lastBackgroundFrameTime = Time.unscaledTime;
                if (backgroundRawImage != null)
                    backgroundRawImage.enabled = true;
                return false;
            }

            return Time.unscaledTime - lastBackgroundFrameTime > 1.5f;
        }

        private void OnBackgroundVideoLoopPoint(VideoPlayer source)
        {
            source.isLooping = true;
            if (!source.isPlaying)
            {
                source.time = 0d;
                source.Play();
            }
        }

        private void OnBackgroundVideoError(VideoPlayer source, string message)
        {
            backgroundNeedsRestart = true;
        }

        private void OnBackgroundVideoStarted(VideoPlayer source)
        {
            if (backgroundRawImage != null)
                backgroundRawImage.enabled = true;

            lastBackgroundFrame = source.frame;
            lastBackgroundFrameTime = Time.unscaledTime;
        }

        private void CreateIntroOverlay(RectTransform parent)
        {
            introOverlayRoot = new GameObject("IntroVideoOverlay", typeof(RectTransform), typeof(Image));
            introOverlayRoot.transform.SetParent(parent, false);

            RectTransform overlayRect = introOverlayRoot.GetComponent<RectTransform>();
            Stretch(overlayRect);

            Image overlayBg = introOverlayRoot.GetComponent<Image>();
            overlayBg.color = Color.black;
            overlayBg.raycastTarget = true;

            GameObject rawGo = new GameObject("IntroVideoRawImage", typeof(RectTransform), typeof(RawImage));
            rawGo.transform.SetParent(introOverlayRoot.transform, false);

            RectTransform rawRect = rawGo.GetComponent<RectTransform>();
            Stretch(rawRect);

            RawImage rawImage = rawGo.GetComponent<RawImage>();
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;

            introRenderTexture = new RenderTexture(videoWidth, videoHeight, 0);
            introRenderTexture.name = "GreenScapeIntro_RT";
            introRenderTexture.Create();

            rawImage.texture = introRenderTexture;

            GameObject playerGo = new GameObject("IntroVideoPlayer", typeof(VideoPlayer));
            playerGo.transform.SetParent(introOverlayRoot.transform, false);

            introVideoPlayer = playerGo.GetComponent<VideoPlayer>();
            introVideoPlayer.playOnAwake = false;
            introVideoPlayer.isLooping = false;
            introVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            introVideoPlayer.targetTexture = introRenderTexture;
            introVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            introVideoPlayer.clip = introVideoClip;
            introVideoPlayer.waitForFirstFrame = true;

            introVideoPlayer.loopPointReached += OnIntroVideoFinished;
        }

        private void PlayIntroVideo()
        {
            if (introVideoPlayer == null || introVideoClip == null)
            {
                FinishIntroAndShowMenu();
                return;
            }

            introAlreadyPlayed = true;

            introVideoPlayer.Stop();
            introVideoPlayer.clip = introVideoClip;
            introVideoPlayer.Play();

            StartCoroutine(IntroSafetyFallback());
        }

        private IEnumerator IntroSafetyFallback()
        {
            if (introVideoClip == null)
                yield break;

            float maxWait = (float)introVideoClip.length + 1.5f;

            if (maxWait <= 1.5f)
                maxWait = 6f;

            yield return new WaitForSecondsRealtime(maxWait);

            if (introOverlayRoot != null && introOverlayRoot.activeSelf)
            {
                FinishIntroAndShowMenu();
            }
        }

        private void OnIntroVideoFinished(VideoPlayer source)
        {
            FinishIntroAndShowMenu();
        }

        private void FinishIntroAndShowMenu()
        {
            if (introVideoPlayer != null)
            {
                introVideoPlayer.loopPointReached -= OnIntroVideoFinished;
                introVideoPlayer.Stop();
            }

            if (introOverlayRoot != null)
                introOverlayRoot.SetActive(false);

            StartCoroutine(RevealBackgroundThenLogin());
        }

        private void ShowAuthenticationIfNeeded()
        {
            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
            {
                AuthUIBuilder.Instance?.ShowLogin();
            }
            else if (!menuRevealed)
            {
                menuRevealed = true;
                StartCoroutine(FadeInMenuContent());
            }

            if (SessionTakeover.Consume(out string takeoverMessage))
                ShowAccountBusyPrompt(takeoverMessage);
        }

        private void CreateDim(RectTransform parent)
        {
            GameObject dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(parent, false);

            RectTransform rect = dimGo.GetComponent<RectTransform>();
            Stretch(rect);

            Image image = dimGo.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.28f);
            image.raycastTarget = false;
        }

        private void CreateTitle(RectTransform parent)
        {
            Image title = CreateImage("Title", parent, titleSprite);

            titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(1920f * titleWidthPercent, 420f);
            titleRect.anchoredPosition = new Vector2(0f, -1080f * titleTopPercent);

            titleBasePosition = titleRect.anchoredPosition;

            title.preserveAspect = true;
            title.raycastTarget = false;
        }

        private void AnimateTitleCard()
        {
            if (!animateTitle || titleRect == null)
                return;

            float y = Mathf.Sin(Time.unscaledTime * titleFloatSpeed) * titleFloatAmplitude;
            titleRect.anchoredPosition = titleBasePosition + new Vector2(0f, y);
        }

        private void CreateButtons(RectTransform parent)
        {
            float startY = 1080f * (0.5f - firstButtonYPercent);
            float spacing = 1080f * buttonSpacingPercent;

            CreateButton("StartButton", parent, startSprite, new Vector2(0f, startY), OnStartPressed);
            CreateButton("SettingsButton", parent, settingsSprite, new Vector2(0f, startY - spacing), OnSettingsPressed);
            CreateButton("ExitButton", parent, exitSprite, new Vector2(0f, startY - spacing * 2f), OnExitPressed);

            CreateCreditsButton(parent);
            CreateLogoutButton(parent);
            CreateAccountButton(parent);
        }

        private void CreateCreditsButton(RectTransform parent)
        {
            GameObject go = new GameObject("CreditsButton",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Sprite art = UIThemeSprites.Instance?.creditsButton;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = art != null ? new Vector2(240f, 100f) : new Vector2(200f, 70f);
            rect.anchoredPosition = new Vector2(-40f, 40f);

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnCreditsPressed);

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                return;
            }

            image.color = new Color(0.20f, 0.45f, 0.22f, 0.95f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());

            Text label = textGo.GetComponent<Text>();
            label.font = GameFonts.Primary;
            label.fontSize = 26;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = "CREDITS";
        }

        public void OnCreditsPressed()
        {
            if (CreditsUIBuilder.Instance == null)
                new GameObject("CreditsUI_Runtime").AddComponent<CreditsUIBuilder>();

            CreditsUIBuilder.Instance?.Show();
        }

        private GameObject logoutPrompt;
        private GameObject accountBusyPrompt;
        private Text accountBusyMessage;
        private Text logoutPromptMessage;

        private void CreateLogoutButton(RectTransform parent)
        {
            CreateCornerDiscButton(parent, "LogoutButton", "Logout", 40f, OnLogoutPressed);
        }

        private void CreateAccountButton(RectTransform parent)
        {
            CreateCornerDiscButton(parent, "AccountButton", "Account", 210f, OnAccountPressed);
        }

        private void CreateCornerDiscButton(RectTransform parent, string name, string text,
            float x, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Sprite art = UIThemeSprites.Instance?.sliderKnob;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = art != null ? new Vector2(150f, 150f) : new Vector2(200f, 70f);
            rect.anchoredPosition = new Vector2(x, 40f);

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.45f, 0.20f, 0.18f, 0.95f);
            }

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());

            Text label = textGo.GetComponent<Text>();
            label.font = GameFonts.Primary;
            label.fontSize = 26;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = art != null ? new Color(0.20f, 0.12f, 0.04f, 1f) : Color.white;
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = text;
        }

        public void OnAccountPressed()
        {
            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
                return;

            if (AccountUIBuilder.Instance == null)
                new GameObject("AccountUI_Runtime").AddComponent<AccountUIBuilder>();

            AccountUIBuilder.Instance?.Show();
        }

        public void OnLogoutPressed()
        {
            if (logoutPrompt == null)
                BuildLogoutPrompt();

            string email = AuthSession.Instance?.CurrentUser?.email;
            if (string.IsNullOrWhiteSpace(email))
                email = SavedSession.RememberedEmail();

            logoutPromptMessage.text = string.IsNullOrWhiteSpace(email)
                ? "Do you want to logout your current account?"
                : "Do you want to logout your current account?\n(" + email + ")";

            logoutPrompt.SetActive(true);
            logoutPrompt.transform.SetAsLastSibling();
        }

        private void OnLogoutConfirmed()
        {
            logoutPrompt.SetActive(false);

            AuthSession.Instance?.Logout();

            menuRevealed = false;
            if (menuContentGroup != null)
                menuContentGroup.alpha = 0f;
            if (menuContentRoot != null)
                menuContentRoot.SetActive(false);

            AuthUIBuilder.Instance?.ShowLogin();
        }

        private void OnLogoutCancelled()
        {
            logoutPrompt.SetActive(false);
        }

        private void BuildLogoutPrompt()
        {
            logoutPrompt = CreatePromptBoard("LogoutPrompt", out logoutPromptMessage);

            CreatePromptButton(logoutPrompt.transform, "LogoutYes",
                UIThemeSprites.Instance?.confirmYesButton, "YES",
                new Vector2(-130f, 55f), new Vector2(200f, 70f), OnLogoutConfirmed);
            CreatePromptButton(logoutPrompt.transform, "LogoutNo",
                UIThemeSprites.Instance?.confirmNoButton, "NO",
                new Vector2(130f, 55f), new Vector2(200f, 70f), OnLogoutCancelled);

            logoutPrompt.SetActive(false);
        }

        private void ShowAccountBusyPrompt(string message)
        {
            if (accountBusyPrompt == null)
            {
                accountBusyPrompt = CreatePromptBoard("AccountBusyPrompt", out accountBusyMessage);

                CreatePromptButton(accountBusyPrompt.transform, "AccountBusyOkay",
                    UIThemeSprites.Instance?.okayButton, "OKAY!",
                    new Vector2(0f, 55f), new Vector2(220f, 70f),
                    () => accountBusyPrompt.SetActive(false));

                accountBusyPrompt.SetActive(false);
            }

            accountBusyMessage.text = string.IsNullOrWhiteSpace(message)
                ? "The account you are logged in are currently using in a different " +
                  "device. If you didn't share your login information, contact at " +
                  "greenscapeacd@gmail.com for Account Help"
                : message;

            accountBusyPrompt.SetActive(true);
            accountBusyPrompt.transform.SetAsLastSibling();
        }

        private GameObject CreatePromptBoard(string name, out Text message)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            Image blocker = root.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.45f);
            blocker.raycastTarget = true;

            Sprite board = UIThemeSprites.Instance?.tradeRequestBoard;

            GameObject panel = new GameObject("Board", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = board != null ? new Vector2(900f, 340f) : new Vector2(700f, 280f);

            Image image = panel.GetComponent<Image>();
            if (board != null)
            {
                image.sprite = board;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.05f, 0.1f, 0.06f, 0.99f);
            }

            GameObject textGo = new GameObject("Message", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.34f);
            textRect.anchorMax = new Vector2(1f, 0.88f);
            textRect.offsetMin = new Vector2(80f, 0f);
            textRect.offsetMax = new Vector2(-80f, 0f);

            message = textGo.GetComponent<Text>();
            message.font = GameFonts.Primary;
            message.fontSize = 24;
            message.fontStyle = FontStyle.Bold;
            message.alignment = TextAnchor.MiddleCenter;
            message.color = new Color(0.20f, 0.12f, 0.04f, 1f);
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            message.verticalOverflow = VerticalWrapMode.Overflow;
            message.raycastTarget = false;

            return root;
        }

        private void CreatePromptButton(Transform parent, string name, Sprite art,
            string fallbackLabel, Vector2 anchoredPosition, Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            Transform board = parent.Find("Board");
            if (board == null)
                board = parent;

            GameObject go = new GameObject(name,
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(board, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                return;
            }

            image.color = new Color(0.20f, 0.45f, 0.22f, 0.95f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());

            Text label = textGo.GetComponent<Text>();
            label.font = GameFonts.Primary;
            label.fontSize = 24;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.text = fallbackLabel;
        }

        private void CreateButton(
            string objectName,
            RectTransform parent,
            Sprite sprite,
            Vector2 anchoredPosition,
            UnityEngine.Events.UnityAction clickAction)
        {
            GameObject buttonGo = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);

            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1920f * buttonWidthPercent, 190f);
            rect.anchoredPosition = anchoredPosition;

            Image image = buttonGo.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = true;

            Button button = buttonGo.GetComponent<Button>();

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);

            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = image;
            button.onClick.AddListener(clickAction);
        }

        private Image CreateImage(string objectName, RectTransform parent, Sprite sprite)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;

            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public void OnStartPressed()
        {
            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
            {
                AuthUIBuilder.Instance?.ShowLogin();
                return;
            }

            if (claimingSession)
                return;

            StartCoroutine(ClaimThenStart());
        }

        private bool claimingSession;

        private IEnumerator ClaimThenStart()
        {
            claimingSession = true;

            bool blocked = false;
            string blockedMessage = null;

            yield return AuthSession.Instance.ClaimPlaySession(
                onSuccess: () => { },
                onBusy: message =>
                {
                    blocked = true;
                    blockedMessage = message;
                },
                onError: _ => { });

            claimingSession = false;

            if (blocked)
            {
                ShowAccountBusyPrompt(blockedMessage);
                yield break;
            }

            AuthSession.Instance.StartGame();
        }

        public void OnSettingsPressed()
        {
            if (SettingsUIBuilder.Instance == null)
                new GameObject("SettingsUIBuilder").AddComponent<SettingsUIBuilder>();

            SettingsUIBuilder.Instance.Show();
        }

        public void OnExitPressed()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnDestroy()
        {
            if (AuthSession.Instance != null)
                AuthSession.Instance.SessionChanged -= HandleSessionChanged;

            if (introVideoPlayer != null)
                introVideoPlayer.loopPointReached -= OnIntroVideoFinished;

            if (backgroundVideoPlayer != null)
            {
                backgroundVideoPlayer.loopPointReached -= OnBackgroundVideoLoopPoint;
                backgroundVideoPlayer.errorReceived -= OnBackgroundVideoError;
                backgroundVideoPlayer.started -= OnBackgroundVideoStarted;
                backgroundVideoPlayer.Stop();
                backgroundVideoPlayer.targetTexture = null;
            }

            if (introRenderTexture != null)
            {
                introRenderTexture.Release();
                introRenderTexture = null;
            }

            if (backgroundRenderTexture != null)
            {
                backgroundRenderTexture.Release();
                backgroundRenderTexture = null;
            }

            if (backgroundRawImage != null)
                backgroundRawImage.texture = null;
        }
    }
}