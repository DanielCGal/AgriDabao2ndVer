using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace AgriDabao3D
{
    /// <summary>Which portrait Antonio wears for a line.</summary>
    public enum AntonioExpression
    {
        Hello,
        Teaching,
        Thinking,
        Surprise
    }

    /// <summary>
    /// The board Antonio talks from during the beginner guide: his portrait in a
    /// frame on the left, his words on the plank beside it.
    ///
    /// It shows one line at a time, typed out, and advances on a tap. While the
    /// guide is waiting for the player to actually do something, the board stays
    /// up with the instruction still readable and swaps the "tap to continue"
    /// caret for a short objective hint - so nobody is ever left looking at an
    /// empty screen wondering what the game wants.
    ///
    /// Built in code like the rest of the game's UI. With no art assigned it
    /// falls back to plain planks and still works, so the guide can be played and
    /// tested before Antonio's portraits exist.
    /// </summary>
    public class TutorialDialogueUI : MonoBehaviour
    {
        /// <summary>Characters revealed per second while a line is typing.</summary>
        private const float TypeSpeed = 38f;

        /// <summary>
        /// One expression: its looping video, the texture it draws into, and the
        /// image showing that texture.
        ///
        /// Each expression gets its own player rather than one player swapping
        /// clips, because Antonio changes expression on almost every line and a
        /// clip swap has to Prepare() first - which shows a black frame each time.
        /// Four players prepared once up front swap instantly; only the visible one
        /// is ever playing, the rest sit paused.
        /// </summary>
        private sealed class Portrait
        {
            public VideoPlayer Player;
            public RenderTexture Texture;
            public RawImage Image;
        }

        private readonly Dictionary<AntonioExpression, Portrait> portraits =
            new Dictionary<AntonioExpression, Portrait>();

        private AntonioExpression? currentExpression;
        private float nextPortraitCheck;

        /// <summary>Portrait render size. Square, and larger than it is ever drawn.</summary>
        private const int PortraitTextureSize = 512;

        private Canvas canvas;
        private GameObject root;
        private CanvasGroup boardGroup;
        private GameObject objectiveRoot;
        private Text objectiveText;
        private Image boardImage;
        private RectTransform portraitRect;
        private Text speakerText;
        private Text bodyText;
        private Text hintText;
        private Button advanceButton;

        private string fullLine = string.Empty;
        private float revealed;
        private bool waitingForGate;
        private Action onAdvance;

        public bool IsVisible => root != null && root.activeSelf;

        /// <summary>True once the whole line is on screen and a tap would advance.</summary>
        public bool LineFinished => revealed >= fullLine.Length;

        private UIThemeSprites Theme => UIThemeSprites.Instance;

        private void Awake()
        {
            EnsureCanvas();
            Build();
            root.SetActive(false);
        }

        private void Update()
        {
            // Runs even while the board is hidden or the line has finished typing:
            // the surface can be lost at any moment, including between lines.
            TickPortraitWatchdog();

            // Panels bring themselves to the front as they open, so the plank has
            // to keep reclaiming it - otherwise the objectives book covers the very
            // instruction telling the player what to do with it.
            if (objectiveRoot != null && objectiveRoot.activeSelf)
                objectiveRoot.transform.SetAsLastSibling();

            if (!IsVisible || LineFinished)
                return;

            revealed += TypeSpeed * Time.unscaledDeltaTime;
            int count = Mathf.Clamp(Mathf.FloorToInt(revealed), 0, fullLine.Length);
            bodyText.text = fullLine.Substring(0, count);

            if (LineFinished)
                RefreshCaret();
        }

        /// <summary>
        /// Shows one line. <paramref name="speaker"/> null or empty makes it a
        /// narration beat: italic, no name, and the portrait keeps whatever
        /// expression it was already wearing so his face does not flicker.
        /// </summary>
        public void ShowLine(string speaker, string line, AntonioExpression? expression, Action advanceCallback)
        {
            objectiveRoot.SetActive(false);

            root.SetActive(true);
            root.transform.SetAsLastSibling();
            SetBoardShown(true);

            // Coming back from a gate the board was only faded, not rebuilt, but a
            // pause can survive that - make sure he is moving again.
            if (currentExpression.HasValue &&
                portraits.TryGetValue(currentExpression.Value, out Portrait showing) &&
                !showing.Player.isPlaying)
            {
                showing.Player.Play();
            }

            // Prepare on the first line rather than in Awake: nothing under an
            // inactive root prepares, and the board is hidden until now. The intro
            // runs fourteen lines before Antonio changes expression, which is far
            // more than enough time for all four to finish decoding their opener.
            EnsurePortraitsPrepared();

            waitingForGate = false;
            onAdvance = advanceCallback;

            bool isNarration = string.IsNullOrEmpty(speaker);
            speakerText.text = isNarration ? string.Empty : speaker;
            speakerText.gameObject.SetActive(!isNarration);

            // Bold black on both: the old browns were close enough to the plank's
            // own wood tone that the words sank into the grain and were hard to read.
            bodyText.fontStyle = isNarration ? FontStyle.BoldAndItalic : FontStyle.Bold;
            bodyText.color = Color.black;

            if (expression.HasValue)
                ApplyExpression(expression.Value);

            fullLine = line ?? string.Empty;
            revealed = 0f;
            bodyText.text = string.Empty;
            RefreshCaret();
        }

        /// <summary>
        /// Hands the screen back to the player.
        ///
        /// The dialogue board is most of the lower half of the screen, so leaving
        /// it up while the player is meant to be digging or opening the shop hides
        /// the very thing they are being asked to use. It steps aside, and a narrow
        /// plank at the top carries the instruction instead. The board returns the
        /// moment the job is done.
        /// </summary>
        public void ShowObjective(string hint)
        {
            waitingForGate = true;

            // Snap the line fully open before stepping aside, so the board is not
            // mid-typewriter when it comes back.
            revealed = fullLine.Length;
            bodyText.text = fullLine;
            hintText.text = string.Empty;

            SetBoardShown(false);

            objectiveText.text = string.IsNullOrWhiteSpace(hint) ? string.Empty : hint;
            objectiveRoot.SetActive(true);
            objectiveRoot.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (objectiveRoot != null)
                objectiveRoot.SetActive(false);

            if (root != null)
                root.SetActive(false);
        }

        /// <summary>
        /// Fades the board rather than deactivating it: the portrait videos live
        /// underneath, and a VideoPlayer on an inactive object stops. Alpha keeps
        /// them running so Antonio is still mid-gesture when he comes back.
        /// </summary>
        private void SetBoardShown(bool shown)
        {
            boardGroup.alpha = shown ? 1f : 0f;
            boardGroup.blocksRaycasts = shown;
            boardGroup.interactable = shown;
        }

        private void RefreshCaret()
        {
            hintText.text = LineFinished && !waitingForGate ? "tap to continue" : string.Empty;
            hintText.color = new Color(0.38f, 0.30f, 0.18f, 1f);
        }

        private void OnBoardTapped()
        {
            // While a gate is open the board is only a sign, not a button.
            if (waitingForGate)
                return;

            if (!LineFinished)
            {
                // First tap completes the line rather than skipping it, so a fast
                // tapper cannot blow through dialogue without seeing it.
                revealed = fullLine.Length;
                bodyText.text = fullLine;
                RefreshCaret();
                return;
            }

            onAdvance?.Invoke();
        }

        private void EnsurePortraitsPrepared()
        {
            foreach (KeyValuePair<AntonioExpression, Portrait> entry in portraits)
            {
                VideoPlayer player = entry.Value.Player;
                if (player != null && !player.isPrepared && !player.isPlaying)
                    player.Prepare();
            }
        }

        private void ApplyExpression(AntonioExpression expression)
        {
            if (currentExpression == expression)
                return;

            if (!portraits.TryGetValue(expression, out Portrait next))
            {
                // No clip assigned for this expression yet. Leave whatever is
                // already showing rather than blanking the frame mid-conversation.
                return;
            }

            if (currentExpression.HasValue &&
                portraits.TryGetValue(currentExpression.Value, out Portrait previous))
            {
                previous.Player.Pause();
                previous.Image.gameObject.SetActive(false);
            }

            next.Image.gameObject.SetActive(true);
            next.Player.Play();
            currentExpression = expression;
        }

        /// <summary>
        /// Repairs the portrait textures after the graphics surface has been
        /// recreated - which Android does when the app is paused, or when the soft
        /// keyboard opens. The contents are discarded, the texture reports itself
        /// as uncreated, and the RawImage draws nothing. This is the same failure
        /// that turned the main menu white, guarded the same way.
        /// </summary>
        private void TickPortraitWatchdog()
        {
            if (Time.unscaledTime < nextPortraitCheck)
                return;

            nextPortraitCheck = Time.unscaledTime + 0.5f;

            foreach (KeyValuePair<AntonioExpression, Portrait> entry in portraits)
            {
                Portrait portrait = entry.Value;
                if (portrait.Texture == null || portrait.Texture.IsCreated())
                    continue;

                portrait.Texture.Release();
                portrait.Texture.Create();

                // The old native handle died with the surface, so the player has to
                // be pointed at the texture again or its frames go nowhere.
                portrait.Player.targetTexture = portrait.Texture;

                if (currentExpression == entry.Key)
                    portrait.Player.Play();
            }
        }

        // -------------------------------------------------------------- building

        private void Build()
        {
            Vector2 boardSize = Theme != null
                ? Theme.tutorialDialogueSize
                : new Vector2(1240f, 510f);

            root = new GameObject("TutorialDialogue",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
            root.transform.SetParent(canvas.transform, false);

            boardGroup = root.GetComponent<CanvasGroup>();

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = boardSize;
            rect.anchoredPosition = new Vector2(0f, 24f);

            boardImage = root.GetComponent<Image>();
            Sprite board = Theme?.tutorialDialogueBoard;

            if (board != null)
            {
                boardImage.sprite = board;
                boardImage.preserveAspect = true;
                boardImage.color = Color.white;
            }
            else
            {
                boardImage.color = new Color(0.72f, 0.55f, 0.30f, 0.97f);
            }

            advanceButton = root.GetComponent<Button>();
            advanceButton.targetGraphic = boardImage;
            advanceButton.transition = Selectable.Transition.None;
            advanceButton.onClick.AddListener(OnBoardTapped);

            BuildPortrait(boardSize);
            BuildText(boardSize);
            BuildObjectivePlank();
        }

        /// <summary>
        /// The narrow plank at the top that carries the current instruction while
        /// the board is out of the way.
        /// </summary>
        private void BuildObjectivePlank()
        {
            Vector2 size = Theme != null
                ? Theme.tutorialObjectiveSize
                : new Vector2(700f, 150f);

            objectiveRoot = new GameObject("TutorialObjective", typeof(RectTransform), typeof(Image));
            objectiveRoot.transform.SetParent(canvas.transform, false);

            RectTransform rect = objectiveRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;

            // Below the round button row, which starts 20 down and is 90 tall.
            rect.anchoredPosition = new Vector2(0f, -(20f + HudIconButton.Size + 16f));

            Image image = objectiveRoot.GetComponent<Image>();
            Sprite plank = Theme?.tutorialObjectiveBoard;

            if (plank != null)
            {
                image.sprite = plank;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.42f, 0.28f, 0.13f, 0.94f);
            }

            image.raycastTarget = false;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(objectiveRoot.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(size.x * 0.12f, size.y * 0.20f);
            textRect.offsetMax = new Vector2(-size.x * 0.12f, -size.y * 0.20f);

            objectiveText = textGo.GetComponent<Text>();
            objectiveText.font = GameFonts.Primary;
            objectiveText.fontSize = 30;
            objectiveText.fontStyle = FontStyle.Bold;
            objectiveText.alignment = TextAnchor.MiddleCenter;
            objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Shrinks a long objective to fit the plank instead of cutting it off,
            // the same fix as the shop's status row. The plank holds one line at
            // this size, so "Buy anything, then close the shop" wrapped and its
            // second line was never drawn - and the Text Size setting, which
            // raises the font but not the plank, made more objectives wrap. Best
            // fit never goes above the font size, and the text size setting scales
            // both limits, so short objectives still grow with that setting.
            objectiveText.verticalOverflow = VerticalWrapMode.Truncate;
            objectiveText.resizeTextForBestFit = true;
            objectiveText.resizeTextMaxSize = objectiveText.fontSize;
            objectiveText.resizeTextMinSize = 14;
            // White rather than the dark brown this used to be. The plank art is
            // mid-brown, so brown-on-brown left the current objective - the one
            // line telling the player what the tutorial is waiting for - the least
            // readable text on screen.
            objectiveText.color = Color.white;
            objectiveText.raycastTarget = false;

            objectiveRoot.SetActive(false);
        }

        private void BuildPortrait(Vector2 boardSize)
        {
            Vector2 size = Theme != null ? Theme.tutorialPortraitSize : new Vector2(240f, 240f);
            Vector2 offset = Theme != null ? Theme.tutorialPortraitOffset : new Vector2(96f, 40f);

            // RectMask2D clips every portrait to the frame's painted opening, so a
            // clip whose shape does not match can never spill over the woodwork.
            GameObject go = new GameObject("Portrait", typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(root.transform, false);

            // Anchored to the board's top-left so the offset reads the way it is
            // described in the theme: X runs right, Y runs down.
            portraitRect = go.GetComponent<RectTransform>();
            portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 1f);
            portraitRect.pivot = new Vector2(0f, 1f);
            portraitRect.sizeDelta = size;
            portraitRect.anchoredPosition = new Vector2(offset.x, -offset.y);

            // Printed because these two values live in the UITheme asset, and an
            // asset that already holds numbers ignores any later change to the C#
            // defaults. If Antonio sits wrong in his frame, this line says exactly
            // what rectangle is being used so it can be compared with the art.
            Debug.Log("[Tutorial] Portrait opening: size " + size + " at offset " + offset +
                      " (DialogueBoard.png's painted opening is 269 x 380 at 37, 68 " +
                      "when the board is drawn at 1240 x 510).");

            CreatePortrait(AntonioExpression.Hello, Theme?.antonioHello);
            CreatePortrait(AntonioExpression.Teaching, Theme?.antonioTeaching);
            CreatePortrait(AntonioExpression.Thinking, Theme?.antonioThinking);
            CreatePortrait(AntonioExpression.Surprise, Theme?.antonioSurprise);
        }

        private void CreatePortrait(AntonioExpression expression, VideoClip clip)
        {
            // An expression with no clip assigned simply does not exist, and
            // ApplyExpression leaves the previous one showing instead.
            if (clip == null)
                return;

            // The clip's own shape, so nothing is stretched. A square texture would
            // squash a portrait-shaped video into a square and then stretch it back
            // out across the opening, which is what made Antonio look wrong and
            // pushed him past the frame.
            float clipAspect = clip.height > 0
                ? (float)clip.width / clip.height
                : 1f;

            int textureWidth = clipAspect >= 1f
                ? PortraitTextureSize
                : Mathf.Max(1, Mathf.RoundToInt(PortraitTextureSize * clipAspect));
            int textureHeight = clipAspect >= 1f
                ? Mathf.Max(1, Mathf.RoundToInt(PortraitTextureSize / clipAspect))
                : PortraitTextureSize;

            GameObject go = new GameObject(expression.ToString(), typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(portraitRect, false);

            // Fitted inside the opening rather than filling it: the whole of
            // Antonio stays visible, and because the videos are on black and the
            // opening is black, the letterboxing is invisible.
            Vector2 opening = portraitRect.sizeDelta;
            float openingAspect = opening.y > 0f ? opening.x / opening.y : 1f;
            Vector2 fitted = clipAspect > openingAspect
                ? new Vector2(opening.x, opening.x / clipAspect)
                : new Vector2(opening.y * clipAspect, opening.y);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = fitted;
            rect.anchoredPosition = Vector2.zero;

            RenderTexture texture = new RenderTexture(textureWidth, textureHeight, 0)
            {
                name = "AntonioPortrait_" + expression
            };
            texture.Create();

            RawImage image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;

            // Parented to the portrait holder, NOT to the RawImage above: that image
            // is switched off whenever its expression is not showing, and a
            // VideoPlayer on an inactive object will not prepare or play. Keeping
            // the players on an always-active parent is what lets all four sit
            // ready so swapping expressions is instant.
            GameObject playerGo = new GameObject("Player_" + expression, typeof(VideoPlayer));
            playerGo.transform.SetParent(portraitRect, false);

            VideoPlayer player = playerGo.GetComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = texture;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.clip = clip;
            player.waitForFirstFrame = true;

            go.SetActive(false);

            portraits[expression] = new Portrait
            {
                Player = player,
                Texture = texture,
                Image = image
            };
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<AntonioExpression, Portrait> entry in portraits)
            {
                if (entry.Value.Texture == null)
                    continue;

                entry.Value.Texture.Release();
                Destroy(entry.Value.Texture);
            }

            portraits.Clear();
        }

        private void BuildText(Vector2 boardSize)
        {
            // The plank the words sit on starts to the right of the portrait frame
            // and stops short of the board's painted edges.
            Vector2 portraitSize = Theme != null ? Theme.tutorialPortraitSize : new Vector2(240f, 240f);
            Vector2 portraitOffset = Theme != null ? Theme.tutorialPortraitOffset : new Vector2(96f, 40f);

            float left = portraitOffset.x + portraitSize.x + 46f;
            float right = 70f;
            float top = 120f;
            float bottom = 70f;

            speakerText = CreateText("Speaker", 26, FontStyle.Bold, Color.black);
            // Font 26 in the 34-tall strip below gives only 1.31x the font size of
            // vertical room, which is under Playpen Sans's line box - and Unity
            // discards a line that does not fit rather than clipping it, so
            // Antonio's name disappeared while his sentence stayed. The body text
            // below already opts out for the same reason; the name needs it too.
            speakerText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform speakerRect = speakerText.rectTransform;
            speakerRect.anchorMin = new Vector2(0f, 1f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.pivot = new Vector2(0.5f, 1f);
            speakerRect.offsetMin = new Vector2(left, 0f);
            speakerRect.offsetMax = new Vector2(-right, 0f);
            speakerRect.sizeDelta = new Vector2(speakerRect.sizeDelta.x, 34f);
            speakerRect.anchoredPosition = new Vector2(speakerRect.anchoredPosition.x, -(top - 42f));

            bodyText = CreateText("Body", 30, FontStyle.Bold, Color.black);
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(left, bottom);
            bodyRect.offsetMax = new Vector2(-right, -top);

            hintText = CreateText("Hint", 22, FontStyle.Bold,
                new Color(0.38f, 0.30f, 0.18f, 1f));
            hintText.alignment = TextAnchor.LowerRight;
            // 1.55x here, so "tap to continue" still fits today - but it is the
            // prompt telling the player how to advance the tutorial at all, so it
            // is not left one text-size notch away from vanishing.
            hintText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform hintRect = hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.offsetMin = new Vector2(left, 0f);
            hintRect.offsetMax = new Vector2(-right, 0f);
            hintRect.sizeDelta = new Vector2(hintRect.sizeDelta.x, 34f);
            hintRect.anchoredPosition = new Vector2(hintRect.anchoredPosition.x, bottom - 42f);
        }

        private Text CreateText(string name, int fontSize, FontStyle style, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(root.transform, false);

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = color;

            // Nothing here is a raycast target: the whole board is one button, and
            // a label swallowing the tap would make the dialogue feel broken.
            text.raycastTarget = false;

            return text;
        }

        private void EnsureCanvas()
        {
            canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f; // landscape: scale by height
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }
        }
    }
}
