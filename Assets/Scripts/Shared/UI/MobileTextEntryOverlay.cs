using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// Shows what the player is typing in a bar across the top of the screen,
    /// because on a phone in landscape the soft keyboard covers most of the game
    /// and usually the text box itself with it.
    ///
    /// Android has its own version of this - the fullscreen "extract" editor -
    /// but it cannot be relied on. uGUI already asks for it: InputField sets
    /// TouchScreenKeyboard.hideInput from shouldHideMobileInput, which defaults to
    /// false on Android, so the native editor is enabled and still does not
    /// appear. Whether it shows is up to the device's keyboard app and the
    /// activity's window flags, neither of which the game controls. Drawing it
    /// ourselves gives every phone the same behaviour.
    ///
    /// This watches the EventSystem instead of being wired into each panel, so it
    /// covers every text box in the game - login, sign-up, the verification code,
    /// the AI adviser, player chat, marketplace search, the shop - without any of
    /// those builders knowing it exists.
    ///
    /// It is deliberately inert: its canvas has no GraphicRaycaster and nothing on
    /// it is a raycast target, so it cannot take focus from the field being typed
    /// into, swallow a tap, or change any existing behaviour. It only reads.
    /// </summary>
    public class MobileTextEntryOverlay : MonoBehaviour
    {
        private static MobileTextEntryOverlay instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject go = new GameObject("MobileTextEntryOverlay");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MobileTextEntryOverlay>();
        }

        [Tooltip("Also show the bar in the Editor, where there is no soft keyboard " +
                 "to hide anything. Off by default: the PC is for testing, and the " +
                 "bar would only sit on top of the Game view.")]
        public bool showInEditor;

        /// <summary>Longest tail of text kept on screen; older characters scroll off the front.</summary>
        private const int MaxVisibleCharacters = 240;

        private const float BarHeight = 190f;

        /// <summary>
        /// The overlay's own canvas, kept switched OFF whenever nobody is typing.
        ///
        /// This is load-bearing, not tidiness. Every UI builder in the game finds
        /// its canvas with FindObjectOfType&lt;Canvas&gt;() and assumes the scene has
        /// exactly one, so a second permanent canvas makes two builders pick
        /// different ones and whichever has the lower sorting order disappears
        /// behind the other. FindObjectOfType skips inactive objects, so while
        /// this is off it cannot be found, and nothing is built while the player
        /// is mid-keystroke.
        /// </summary>
        private GameObject canvasRoot;

        private GameObject bar;
        private Text captionText;
        private Text valueText;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Build();
            canvasRoot.SetActive(false);
        }

        private void LateUpdate()
        {
            InputField field = ResolveFocusedField();

            if (field == null)
            {
                if (canvasRoot.activeSelf)
                    canvasRoot.SetActive(false);
                return;
            }

            if (!canvasRoot.activeSelf)
                canvasRoot.SetActive(true);

            captionText.text = PlaceholderOf(field);
            valueText.text = BuildDisplayText(field);
        }

        /// <summary>The InputField the keyboard is currently typing into, if any.</summary>
        private InputField ResolveFocusedField()
        {
            if (!ShouldRun())
                return null;

            EventSystem events = EventSystem.current;
            if (events == null)
                return null;

            GameObject selected = events.currentSelectedGameObject;
            if (selected == null)
                return null;

            InputField field = selected.GetComponent<InputField>();
            if (field == null || !field.isFocused)
                return null;

            return field;
        }

        private bool ShouldRun()
        {
            if (Application.isMobilePlatform)
                return true;

#if UNITY_EDITOR
            return showInEditor;
#else
            return false;
#endif
        }

        /// <summary>
        /// The typed text, trimmed to the most recent characters, with a blinking
        /// caret on the end.
        ///
        /// Passwords are shown here in the clear, deliberately. The field itself
        /// still masks them - that is Unity drawing its own Password contentType,
        /// and it is untouched. This bar exists only because the soft keyboard
        /// covers the box being typed into, so repeating the mask up here made it
        /// useless for exactly the fields where a typo is hardest to notice and
        /// most annoying to recover from: the password, its confirmation, and the
        /// sign-up password.
        ///
        /// The trade is that a password is briefly readable over the player's
        /// shoulder while they type it. That is the same exposure as any "show
        /// password" eye toggle, it lasts only while the field has focus, and the
        /// bar is already showing every other credential they type.
        /// </summary>
        private string BuildDisplayText(InputField field)
        {
            string value = field.text ?? string.Empty;

            if (value.Length > MaxVisibleCharacters)
                value = "…" + value.Substring(value.Length - MaxVisibleCharacters);

            bool caretOn = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f;
            return caretOn ? value + "|" : value;
        }

        /// <summary>
        /// Names the box being typed into, so the bar is not just a floating line
        /// of text. Falls back to the object's name when a field has no
        /// placeholder of its own.
        /// </summary>
        private static string PlaceholderOf(InputField field)
        {
            if (field.placeholder is Text placeholder &&
                !string.IsNullOrWhiteSpace(placeholder.text))
            {
                return placeholder.text;
            }

            return field.gameObject.name;
        }

        private void Build()
        {
            canvasRoot = new GameObject(
                "MobileTextEntryCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasRoot.transform.SetParent(transform, false);

            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Above every other canvas in the game. No GraphicRaycaster is added,
            // which is what keeps this bar from ever intercepting a touch.
            canvas.sortingOrder = 32000;

            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // landscape: scale by height

            bar = new GameObject("EntryBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(canvasRoot.transform, false);

            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.sizeDelta = new Vector2(0f, BarHeight);
            barRect.anchoredPosition = Vector2.zero;

            Image background = bar.GetComponent<Image>();
            background.color = new Color(0.97f, 0.97f, 0.96f, 1f);
            background.raycastTarget = false;

            captionText = CreateLabel("Caption", 24, new Color(0.35f, 0.35f, 0.35f, 1f));
            RectTransform captionRect = captionText.rectTransform;
            captionRect.anchorMin = new Vector2(0f, 1f);
            captionRect.anchorMax = new Vector2(1f, 1f);
            captionRect.pivot = new Vector2(0.5f, 1f);
            captionRect.sizeDelta = new Vector2(-96f, 34f);
            captionRect.anchoredPosition = new Vector2(0f, -12f);

            valueText = CreateLabel("Value", 40, new Color(0.08f, 0.08f, 0.08f, 1f));
            RectTransform valueRect = valueText.rectTransform;
            valueRect.anchorMin = new Vector2(0f, 0f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.offsetMin = new Vector2(48f, 14f);
            valueRect.offsetMax = new Vector2(-48f, -52f);
            valueText.alignment = TextAnchor.UpperLeft;
            valueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            valueText.verticalOverflow = VerticalWrapMode.Truncate;

            CreateUnderline();
        }

        private Text CreateLabel(string name, int fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(bar.transform, false);

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = color;
            text.raycastTarget = false;

            // Geometry is left to the caller; both of them position their label
            // differently and would only overwrite anything set here.
            return text;
        }

        /// <summary>The green rule along the bottom edge, echoing a text field's underline.</summary>
        private void CreateUnderline()
        {
            GameObject go = new GameObject("Underline", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(bar.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-96f, 5f);
            rect.anchoredPosition = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.15f, 0.45f, 0.18f, 1f);
            image.raycastTarget = false;
        }
    }
}
