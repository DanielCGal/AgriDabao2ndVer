using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Video;
using System.Collections;

namespace AgriDabao3D
{
    public enum AdvisorMood
    {
        Hello,
        Thinking,
        Teaching
    }

    public class FarmAdvisorUIBuilder : MonoBehaviour
    {
        [Header("Antonio Portrait (looping videos)")]
        [Tooltip("Plays when the chat panel is opened.")]
        public VideoClip helloClip;
        [Tooltip("Plays while waiting for the AI reply.")]
        public VideoClip thinkingClip;
        [Tooltip("Plays once a reply arrives.")]
        public VideoClip teachingClip;

        [Tooltip("Render size of the portrait video texture. 512x512 suits the frame art.")]
        public int portraitVideoSize = 512;

        public RectTransform panelRect;
        public Text chatOutputText;
        public InputField questionInput;
        public Button sendButton;
        public ScrollRect scrollRect;

        private Canvas canvas;
        private UIThemeSprites theme;

        private GameObject chatPanel;
        private VideoPlayer portraitPlayer;
        private RenderTexture portraitTexture;
        private AdvisorMood currentMood = AdvisorMood.Hello;

        private const float PanelWidth = 1400f;
        private const float PanelHeight = 849f;

        private void Awake()
        {
            theme = UIThemeSprites.Instance;
            EnsureEventSystem();
            EnsureCanvas();
            BuildUI();
            CreateToggleButton();

            chatPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (portraitTexture != null)
            {
                portraitTexture.Release();
                portraitTexture = null;
            }
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void EnsureCanvas()
        {
            canvas = Object.FindFirstObjectByType<Canvas>();

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
        }

        private void CreateToggleButton()
        {
            GameObject go = new GameObject("AdvisorChatButton", typeof(RectTransform), typeof(Image), typeof(Button));
            HudRegistry.RegisterIconButton(HudIconButton.SlotAdviserChat, go);
            go.transform.SetParent(canvas.transform, false);

            Sprite art = theme?.chatbotButton;
            float size = PauseMenuBuilder.CornerButtonSize;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = art != null ? new Vector2(size, size) : new Vector2(120f, 56f);
            rect.anchoredPosition = new Vector2(art != null ? 20f + size + 12f : 152f, -20f);

            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(ToggleChat);

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
                return;
            }

            image.color = new Color(0.08f, 0.10f, 0.09f, 0.92f);
            Text label = CreateText(go.transform, "Text", 22, TextAnchor.MiddleCenter);
            label.text = "Adviser";
            Stretch(label.rectTransform);
        }

        public void ToggleChat()
        {
            if (chatPanel == null)
                return;

            if (chatPanel.activeSelf)
            {
                CloseChat();
                return;
            }

            OpenChat();
        }

        public void OpenChat()
        {
            if (chatPanel == null)
                return;

            HudRegistry.RegisterPiece(HudPiece.AdviserChatPanel, chatPanel);
            HudRegistry.CloseOtherPanels(HudPiece.AdviserChatPanel);

            chatPanel.SetActive(true);
            chatPanel.transform.SetAsLastSibling();

            SetMood(AdvisorMood.Hello, force: true);
        }

        public void CloseChat()
        {
            if (chatPanel == null)
                return;

            chatPanel.SetActive(false);

            if (portraitPlayer != null)
                portraitPlayer.Stop();
        }

        public void SetMood(AdvisorMood mood, bool force = false)
        {
            if (!force && currentMood == mood && portraitPlayer != null && portraitPlayer.isPlaying)
                return;

            currentMood = mood;

            if (portraitPlayer == null)
                return;

            VideoClip clip = mood switch
            {
                AdvisorMood.Thinking => thinkingClip,
                AdvisorMood.Teaching => teachingClip,
                _ => helloClip
            };

            if (clip == null)
                return;

            portraitPlayer.Stop();
            portraitPlayer.clip = clip;
            portraitPlayer.isLooping = true;
            portraitPlayer.Play();
        }

        private void BuildPortrait(Transform parent, Vector2 anchoredPos, float frameSize)
        {
            Sprite frameArt = theme?.portraitFrame;

            GameObject frame = new GameObject("AntonioPortrait", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(parent, false);

            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.sizeDelta = new Vector2(frameSize, frameSize);
            frameRect.anchoredPosition = anchoredPos;

            Image frameImage = frame.GetComponent<Image>();
            if (frameArt != null)
            {
                frameImage.sprite = frameArt;
                frameImage.preserveAspect = true;
                frameImage.color = Color.white;
            }
            else
            {
                frameImage.color = new Color(0.10f, 0.08f, 0.05f, 0.9f);
            }

            GameObject videoGo = new GameObject("PortraitVideo", typeof(RectTransform), typeof(RawImage));
            videoGo.transform.SetParent(frame.transform, false);

            RectTransform videoRect = videoGo.GetComponent<RectTransform>();
            Stretch(videoRect);
            float inset = frameSize * 0.14f;
            videoRect.offsetMin = new Vector2(inset, inset);
            videoRect.offsetMax = new Vector2(-inset, -inset);

            RawImage raw = videoGo.GetComponent<RawImage>();
            raw.color = Color.white;
            raw.raycastTarget = false;

            int size = Mathf.Max(64, portraitVideoSize);
            portraitTexture = new RenderTexture(size, size, 0);
            portraitTexture.name = "AntonioPortrait_RT";
            portraitTexture.Create();
            raw.texture = portraitTexture;

            GameObject playerGo = new GameObject("PortraitVideoPlayer", typeof(VideoPlayer));
            playerGo.transform.SetParent(frame.transform, false);

            portraitPlayer = playerGo.GetComponent<VideoPlayer>();
            portraitPlayer.playOnAwake = false;
            portraitPlayer.isLooping = true;
            portraitPlayer.renderMode = VideoRenderMode.RenderTexture;
            portraitPlayer.targetTexture = portraitTexture;
            portraitPlayer.audioOutputMode = VideoAudioOutputMode.None;
            portraitPlayer.waitForFirstFrame = true;
        }

        private void BuildUI()
        {
            Sprite board = theme?.chatbotBoard;

            GameObject panel = new GameObject("FarmAdvisorPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            chatPanel = panel;

            panelRect = panel.GetComponent<RectTransform>();
            Image panelBg = panel.GetComponent<Image>();

            if (board != null)
            {
                panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
                panelRect.anchoredPosition = Vector2.zero;

                panelBg.sprite = board;
                panelBg.color = Color.white;

                BuildThemedPanel(panel.transform);
                return;
            }

            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(520f, 360f);
            panelRect.anchoredPosition = new Vector2(20f, -120f);
            panelBg.color = new Color(0f, 0f, 0f, 0.45f);

            BuildChatArea(panel.transform, new Vector2(20f, 90f), new Vector2(-20f, -70f), null);
            BuildInputArea(panel.transform);
        }

        private void BuildThemedPanel(Transform parent)
        {
            Sprite labelArt = theme?.askQuestionsLabel;
            if (labelArt != null)
            {
                GameObject signGo = new GameObject("AskQuestionsLabel", typeof(RectTransform), typeof(Image));
                signGo.transform.SetParent(parent, false);

                RectTransform signRect = signGo.GetComponent<RectTransform>();
                signRect.anchorMin = signRect.anchorMax = new Vector2(0.5f, 1f);
                signRect.pivot = new Vector2(0.5f, 0.5f);
                signRect.sizeDelta = new Vector2(980f, 220f);
                signRect.anchoredPosition = new Vector2(-170f, -95f);

                Image signImage = signGo.GetComponent<Image>();
                signImage.sprite = labelArt;
                signImage.preserveAspect = true;
                signImage.raycastTarget = false;
            }

            BuildPortrait(parent, new Vector2(-355f, 75f), 340f);

            RectTransform window = BuildChatWindow(parent, new Vector2(175f, 75f), new Vector2(720f, 340f));
            BuildChatArea(window, Vector2.zero, Vector2.zero, theme?.chatWindowPanel);

            BuildThemedInputRow(parent);
        }

        private RectTransform BuildChatWindow(Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject("ChatWindow", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            return rect;
        }

        private void BuildThemedInputRow(Transform parent)
        {
            GameObject row = new GameObject("InputRow", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(1040f, 80f);
            rowRect.anchoredPosition = new Vector2(0f, -250f);

            Image rowImage = row.GetComponent<Image>();
            Sprite barArt = theme?.chatInputBar;
            if (barArt != null)
            {
                rowImage.sprite = barArt;
                rowImage.type = Image.Type.Sliced;
                rowImage.color = Color.white;
            }
            else
            {
                rowImage.color = new Color(0f, 0f, 0f, 0.35f);
            }

            BuildInputField(row.transform, new Vector2(-110f, 0f), new Vector2(780f, 52f));
            BuildSendButton(row.transform, new Vector2(400f, 0f), new Vector2(200f, 64f), theme?.chatSendButton);
        }

        private void BuildChatArea(Transform parent, Vector2 offsetMin, Vector2 offsetMax, Sprite windowSprite)
        {
            GameObject scrollGo = new GameObject(
                "ChatScrollView",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect)
            );
            scrollGo.transform.SetParent(parent, false);

            RectTransform scrollRectTransform = scrollGo.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = offsetMin;
            scrollRectTransform.offsetMax = offsetMax;

            Image scrollBg = scrollGo.GetComponent<Image>();
            if (windowSprite != null)
            {
                scrollBg.sprite = windowSprite;
                scrollBg.type = Image.Type.Sliced;
                scrollBg.color = Color.white;
            }
            else
            {
                scrollBg.color = new Color(1f, 1f, 1f, 0.05f);
            }

            scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject viewportGo = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask)
            );
            viewportGo.transform.SetParent(scrollGo.transform, false);

            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            float pad = windowSprite != null ? 52f : 0f;
            viewportRect.offsetMin = new Vector2(pad, pad);
            viewportRect.offsetMax = new Vector2(-pad, -pad);

            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;

            Mask viewportMask = viewportGo.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentGo = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(ContentSizeFitter),
                typeof(VerticalLayoutGroup)
            );
            contentGo.transform.SetParent(viewportGo.transform, false);

            RectTransform contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0f;
            layout.padding = new RectOffset(0, 0, 0, 0);

            ContentSizeFitter contentFitter = contentGo.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject textGo = new GameObject(
                "ChatText",
                typeof(RectTransform),
                typeof(Text),
                typeof(ContentSizeFitter)
            );
            textGo.transform.SetParent(contentGo.transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.offsetMin = new Vector2(0f, 0f);
            textRect.offsetMax = new Vector2(0f, 0f);

            chatOutputText = textGo.GetComponent<Text>();
            chatOutputText.font = GameFonts.Primary;
            chatOutputText.fontSize = 20;
            chatOutputText.alignment = TextAnchor.UpperLeft;
            chatOutputText.color = Color.white;
            chatOutputText.horizontalOverflow = HorizontalWrapMode.Wrap;
            chatOutputText.verticalOverflow = VerticalWrapMode.Overflow;
            chatOutputText.text = "AI Adviser: Ask about crop management, soil suitability, weather timing, or your current crops.";

            ContentSizeFitter textFitter = textGo.GetComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
        }

        private void BuildInputField(Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            GameObject inputGo = new GameObject(
                "QuestionInput",
                typeof(RectTransform),
                typeof(Image),
                typeof(InputField)
            );
            inputGo.transform.SetParent(parent, false);

            RectTransform inputRect = inputGo.GetComponent<RectTransform>();
            inputRect.anchorMin = inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.sizeDelta = size;
            inputRect.anchoredPosition = anchoredPos;

            Image inputBg = inputGo.GetComponent<Image>();
            inputBg.color = new Color(1f, 1f, 1f, 0.92f);

            questionInput = inputGo.GetComponent<InputField>();

            questionInput.characterLimit = 400;

            GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(inputGo.transform, false);

            RectTransform placeholderRect = placeholderGo.GetComponent<RectTransform>();
            Stretch(placeholderRect);
            placeholderRect.offsetMin = new Vector2(12f, 6f);
            placeholderRect.offsetMax = new Vector2(-12f, -6f);

            Text placeholderText = placeholderGo.GetComponent<Text>();
            placeholderText.font = GameFonts.Primary;
            placeholderText.fontSize = 18;
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.color = new Color(0f, 0f, 0f, 0.35f);
            placeholderText.text = "Ask a farming question...";

            GameObject inputTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            inputTextGo.transform.SetParent(inputGo.transform, false);

            RectTransform inputTextRect = inputTextGo.GetComponent<RectTransform>();
            Stretch(inputTextRect);
            inputTextRect.offsetMin = new Vector2(12f, 6f);
            inputTextRect.offsetMax = new Vector2(-12f, -6f);

            Text inputText = inputTextGo.GetComponent<Text>();
            inputText.font = GameFonts.Primary;
            inputText.fontSize = 18;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.color = Color.black;
            inputText.text = "";

            placeholderText.verticalOverflow = VerticalWrapMode.Overflow;
            inputText.verticalOverflow = VerticalWrapMode.Overflow;

            questionInput.textComponent = inputText;
            questionInput.placeholder = placeholderText;
        }

        private void BuildSendButton(Transform parent, Vector2 anchoredPos, Vector2 size, Sprite sprite)
        {
            GameObject buttonGo = new GameObject(
                "SendButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );
            buttonGo.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = size;
            buttonRect.anchoredPosition = anchoredPos;

            Image buttonBg = buttonGo.GetComponent<Image>();
            sendButton = buttonGo.GetComponent<Button>();
            sendButton.targetGraphic = buttonBg;

            if (sprite != null)
            {
                buttonBg.sprite = sprite;
                buttonBg.preserveAspect = true;
                buttonBg.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(sendButton);
                return;
            }

            buttonBg.color = new Color(0.2f, 0.7f, 0.2f, 0.95f);

            Text buttonText = CreateText(buttonGo.transform, "Text", 18, TextAnchor.MiddleCenter);
            buttonText.text = "Send";
            Stretch(buttonText.rectTransform);
        }

        private void BuildInputArea(Transform parent)
        {
            GameObject holder = new GameObject("InputRow", typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            RectTransform holderRect = holder.GetComponent<RectTransform>();
            holderRect.anchorMin = new Vector2(0f, 0f);
            holderRect.anchorMax = new Vector2(1f, 0f);
            holderRect.pivot = new Vector2(0.5f, 0f);
            holderRect.offsetMin = new Vector2(20f, 20f);
            holderRect.offsetMax = new Vector2(-20f, 60f);

            BuildInputField(holder.transform, new Vector2(-50f, 0f), new Vector2(360f, 40f));
            BuildSendButton(holder.transform, new Vector2(190f, 0f), new Vector2(90f, 40f), null);
        }

        public void AppendMessage(string speaker, string message)
        {
            if (chatOutputText == null)
                return;

            if (!string.IsNullOrEmpty(chatOutputText.text))
                chatOutputText.text += "\n\n";

            chatOutputText.text += speaker + ": " + message;
            StartCoroutine(ScrollToBottomNextFrame());
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (scrollRect != null && scrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

                if (chatOutputText != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatOutputText.rectTransform);

                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public string GetInputText()
        {
            return questionInput != null ? questionInput.text : "";
        }

        public void ClearInput()
        {
            if (questionInput != null)
                questionInput.text = "";
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
    }
}
