using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class GameAccessibility : MonoBehaviour
    {
        public const string SurfaceLabel =
            "AgriDabao 3D. A farming simulation game set in Davao City.";

        private const string RuntimeObjectName = "AgriDabao_Accessibility";

        private const float RefreshInterval = 0.75f;

        private AccessibilityHierarchy hierarchy;
        private float nextRefreshTime;
        private int lastSignature;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject androidActivity;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (GameObject.Find(RuntimeObjectName) != null)
                return;

            GameObject go = new GameObject(RuntimeObjectName);
            go.AddComponent<GameAccessibility>();
            DontDestroyOnLoad(go);
        }

        private void Start()
        {
            ApplySurfaceLabel();
            SceneManager.sceneLoaded += OnSceneLoaded;
            Rebuild();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (AssistiveSupport.activeHierarchy == hierarchy)
                AssistiveSupport.activeHierarchy = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            lastSignature = 0;
            Rebuild();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = Time.unscaledTime + RefreshInterval;

            int signature = BuildSignature();
            if (signature == lastSignature)
                return;

            lastSignature = signature;
            Rebuild();
        }

        private struct Entry
        {
            public string label;
            public string hint;
            public AccessibilityRole role;
            public RectTransform rect;
            public Canvas canvas;
            public Rect screen;
        }

        private void Rebuild()
        {
            if (hierarchy == null)
                hierarchy = new AccessibilityHierarchy();
            else
                hierarchy.Clear();

            List<Entry> entries = new List<Entry>();
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas == null || !canvas.isActiveAndEnabled)
                    continue;
                if (canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                Collect(canvas, canvas.transform, entries);
            }

            entries.Sort((a, b) =>
            {
                int byRow = b.screen.y.CompareTo(a.screen.y);
                return byRow != 0 ? byRow : a.screen.x.CompareTo(b.screen.x);
            });

            foreach (Entry entry in entries)
            {
                AccessibilityNode node = hierarchy.AddNode(entry.label, null);
                if (node == null)
                    continue;

                node.role = entry.role;
                node.hint = entry.hint;

                RectTransform rect = entry.rect;
                Canvas canvas = entry.canvas;
                node.frameGetter = () => ScreenFrameOf(rect, canvas);
            }

            AssistiveSupport.activeHierarchy = hierarchy;
        }

        private static void Collect(Canvas canvas, Transform parent, List<Entry> entries)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                    continue;

                if (TryDescribe(child.gameObject, out string label,
                        out AccessibilityRole role, out string hint))
                {
                    RectTransform rect = child as RectTransform;
                    if (rect != null)
                    {
                        entries.Add(new Entry
                        {
                            label = label,
                            role = role,
                            hint = hint,
                            rect = rect,
                            canvas = canvas,
                            screen = ScreenFrameOf(rect, canvas)
                        });
                    }

                    continue;
                }

                Collect(canvas, child, entries);
            }
        }

        private static bool TryDescribe(GameObject go, out string label,
            out AccessibilityRole role, out string hint)
        {
            label = null;
            role = AccessibilityRole.None;
            hint = null;

            if (go.TryGetComponent(out Button button))
            {
                label = LabelForControl(go);
                role = AccessibilityRole.Button;
                hint = button.interactable ? "Double tap to activate." : null;
                if (!button.interactable)
                    label += ", unavailable";
                return !string.IsNullOrEmpty(label);
            }

            if (go.TryGetComponent(out InputField input))
            {
                string value = string.IsNullOrEmpty(input.text)
                    ? PlaceholderOf(input)
                    : input.text;
                label = Humanize(go.name) + (string.IsNullOrEmpty(value) ? "" : ", " + value);
                role = AccessibilityRole.TextField;
                hint = "Double tap to edit.";
                return true;
            }

            if (go.TryGetComponent(out Slider slider))
            {
                label = Humanize(go.name) + ", " + Mathf.RoundToInt(slider.value * 100f) + " percent";
                role = AccessibilityRole.Slider;
                hint = "Swipe up or down to adjust.";
                return true;
            }

            if (go.TryGetComponent(out Toggle _))
            {
                label = LabelForControl(go);
                role = AccessibilityRole.Toggle;
                return !string.IsNullOrEmpty(label);
            }

            if (go.TryGetComponent(out Text text))
            {
                string value = text.text;
                if (string.IsNullOrWhiteSpace(value))
                    return false;

                label = Clean(value);
                role = AccessibilityRole.StaticText;
                return !string.IsNullOrEmpty(label);
            }

            return false;
        }

        private static string LabelForControl(GameObject go)
        {
            Text caption = go.GetComponentInChildren<Text>(false);
            if (caption != null && !string.IsNullOrWhiteSpace(caption.text))
                return Clean(caption.text);

            return Humanize(go.name);
        }

        private static string PlaceholderOf(InputField input)
        {
            Text placeholder = input.placeholder as Text;
            return placeholder != null ? Clean(placeholder.text) : string.Empty;
        }

        private static string Humanize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            string value = raw;

            if (value.StartsWith("Button_"))
                value = value.Substring("Button_".Length);
            value = value.Replace("_Runtime", string.Empty)
                         .Replace("_Slider", string.Empty)
                         .Replace("_Label", string.Empty)
                         .Replace('_', ' ');
            if (value.EndsWith("Button") && value.Length > "Button".Length)
                value = value.Substring(0, value.Length - "Button".Length);

            StringBuilder builder = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1]) && value[i - 1] != ' ')
                    builder.Append(' ');
                builder.Append(c);
            }

            return builder.ToString().Trim();
        }

        private static string Clean(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace('\n', ' ').Replace("  ", " ").Trim();
        }

        private static Rect ScreenFrameOf(RectTransform rect, Canvas canvas)
        {
            if (rect == null)
                return Rect.zero;

            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);

            float width = Mathf.Abs(topRight.x - bottomLeft.x);
            float height = Mathf.Abs(topRight.y - bottomLeft.y);
            float x = Mathf.Min(bottomLeft.x, topRight.x);
            float y = Screen.height - Mathf.Max(bottomLeft.y, topRight.y);

            return new Rect(x, y, width, height);
        }

        private int BuildSignature()
        {
            unchecked
            {
                int hash = 17;
                foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (canvas == null || !canvas.isActiveAndEnabled)
                        continue;

                    foreach (Transform child in canvas.transform)
                    {
                        if (!child.gameObject.activeInHierarchy)
                            continue;
                        hash = hash * 31 + child.name.GetHashCode();
                    }
                }
                return hash;
            }
        }

        private void ApplySurfaceLabel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                androidActivity = player.GetStatic<AndroidJavaObject>("currentActivity");
                if (androidActivity == null)
                    return;

                androidActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        using AndroidJavaObject window = androidActivity.Call<AndroidJavaObject>("getWindow");
                        using AndroidJavaObject decor = window.Call<AndroidJavaObject>("getDecorView");
                        LabelViews(decor, 0);
                    }
                    catch (System.Exception inner)
                    {
                        Debug.LogWarning("[Accessibility] Could not label the Unity view: " + inner.Message);
                    }
                }));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Accessibility] Android view labelling skipped: " + ex.Message);
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void LabelViews(AndroidJavaObject view, int depth)
        {
            if (view == null || depth > 6)
                return;

            using (AndroidJavaObject type = view.Call<AndroidJavaObject>("getClass"))
            {
                string name = type.Call<string>("getName");
                if (!string.IsNullOrEmpty(name) &&
                    (name.Contains("SurfaceView") || name.Contains("UnityPlayer")))
                {
                    view.Call("setContentDescription", SurfaceLabel);
                    view.Call("setImportantForAccessibility", 1);
                }
            }

            int count;
            try
            {
                count = view.Call<int>("getChildCount");
            }
            catch (System.Exception)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                using AndroidJavaObject child = view.Call<AndroidJavaObject>("getChildAt", i);
                LabelViews(child, depth + 1);
            }
        }
#endif
    }
}
