using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AgriDabao3D
{
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

        private const int MaxVisibleCharacters = 240;

        private const float BarHeight = 190f;

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

        private string BuildDisplayText(InputField field)
        {
            string value = field.text ?? string.Empty;

            if (value.Length > MaxVisibleCharacters)
                value = "…" + value.Substring(value.Length - MaxVisibleCharacters);

            bool caretOn = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f;
            return caretOn ? value + "|" : value;
        }

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

            canvas.sortingOrder = 32000;

            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

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

            return text;
        }

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
