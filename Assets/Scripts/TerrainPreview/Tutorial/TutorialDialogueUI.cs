using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace AgriDabao3D
{
    public enum AntonioExpression
    {
        Hello,
        Teaching,
        Thinking,
        Surprise
    }

    public class TutorialDialogueUI : MonoBehaviour
    {
        private const float TypeSpeed = 38f;

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

        public bool LineFinished => revealed >= fullLine.Length;

        private UIThemeSprites Theme => UIThemeSprites.Instance;

        private static TutorialDialogueUI active;

        public static bool ObjectiveShowing =>
            active != null && active.objectiveRoot != null && active.objectiveRoot.activeInHierarchy;

        private void Awake()
        {
            active = this;
            EnsureCanvas();
            Build();
            root.SetActive(false);
        }

        private void Update()
        {
            TickPortraitWatchdog();

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

        public void ShowLine(string speaker, string line, AntonioExpression? expression, Action advanceCallback)
        {
            objectiveRoot.SetActive(false);

            root.SetActive(true);
            root.transform.SetAsLastSibling();
            SetBoardShown(true);

            if (currentExpression.HasValue &&
                portraits.TryGetValue(currentExpression.Value, out Portrait showing) &&
                !showing.Player.isPlaying)
            {
                showing.Player.Play();
            }

            EnsurePortraitsPrepared();

            waitingForGate = false;
            onAdvance = advanceCallback;

            bool isNarration = string.IsNullOrEmpty(speaker);
            speakerText.text = isNarration ? string.Empty : speaker;
            speakerText.gameObject.SetActive(!isNarration);

            bodyText.fontStyle = isNarration ? FontStyle.BoldAndItalic : FontStyle.Bold;
            bodyText.color = Color.black;

            if (expression.HasValue)
                ApplyExpression(expression.Value);

            fullLine = line ?? string.Empty;
            revealed = 0f;
            bodyText.text = string.Empty;
            RefreshCaret();
        }

        public void ShowObjective(string hint)
        {
            waitingForGate = true;

            revealed = fullLine.Length;
            bodyText.text = fullLine;
            hintText.text = string.Empty;

            SetBoardShown(false);

            objectiveText.text = string.IsNullOrWhiteSpace(hint) ? string.Empty : hint;
            objectiveRoot.SetActive(true);
            objectiveRoot.transform.SetAsLastSibling();
        }

        public void SetObjectiveText(string hint)
        {
            if (objectiveText == null || objectiveRoot == null || !objectiveRoot.activeSelf)
                return;

            string text = string.IsNullOrWhiteSpace(hint) ? string.Empty : hint;
            if (objectiveText.text != text)
                objectiveText.text = text;
        }

        public void Hide()
        {
            if (objectiveRoot != null)
                objectiveRoot.SetActive(false);

            if (root != null)
                root.SetActive(false);
        }

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
            if (waitingForGate)
                return;

            if (!LineFinished)
            {
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

                portrait.Player.targetTexture = portrait.Texture;

                if (currentExpression == entry.Key)
                    portrait.Player.Play();
            }
        }

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

            objectiveText.verticalOverflow = VerticalWrapMode.Truncate;
            objectiveText.resizeTextForBestFit = true;
            objectiveText.resizeTextMaxSize = objectiveText.fontSize;
            objectiveText.resizeTextMinSize = 14;
            objectiveText.color = Color.white;
            objectiveText.raycastTarget = false;

            objectiveRoot.SetActive(false);
        }

        private void BuildPortrait(Vector2 boardSize)
        {
            Vector2 size = Theme != null ? Theme.tutorialPortraitSize : new Vector2(240f, 240f);
            Vector2 offset = Theme != null ? Theme.tutorialPortraitOffset : new Vector2(96f, 40f);

            GameObject go = new GameObject("Portrait", typeof(RectTransform), typeof(RectMask2D));
            go.transform.SetParent(root.transform, false);

            portraitRect = go.GetComponent<RectTransform>();
            portraitRect.anchorMin = portraitRect.anchorMax = new Vector2(0f, 1f);
            portraitRect.pivot = new Vector2(0f, 1f);
            portraitRect.sizeDelta = size;
            portraitRect.anchoredPosition = new Vector2(offset.x, -offset.y);

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
            if (clip == null)
                return;

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
            if (active == this)
                active = null;

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
            Vector2 portraitSize = Theme != null ? Theme.tutorialPortraitSize : new Vector2(240f, 240f);
            Vector2 portraitOffset = Theme != null ? Theme.tutorialPortraitOffset : new Vector2(96f, 40f);

            float left = portraitOffset.x + portraitSize.x + 46f;
            float right = 70f;
            float top = 120f;
            float bottom = 70f;

            speakerText = CreateText("Speaker", 26, FontStyle.Bold, Color.black);
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
                scaler.matchWidthOrHeight = 1f;
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
