using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AgriDabao3D
{
    [DefaultExecutionOrder(-32000)]
    public class UiResponseTimerHud : MonoBehaviour, IPointerClickHandler
    {
        private const double CreditWindowSeconds = 5.0;
        private const float ResolveEverySeconds = 1f;

        private const double LimitMs = 2000.0;

        private const float CollapsedWidth = 260f;
        private const float ExpandedWidth = 460f;
        private const float LineHeight = 22f;

        private static readonly Color TextColor = new Color(0.85f, 0.88f, 0.92f, 1f);

        private sealed class WatchedScreen
        {
            public string label;
            public System.Func<bool> isOpen;
            public bool wasOpen;
        }

        private sealed class Timing
        {
            public int count;
            public double totalMs;
            public double lastMs;
            public double maxMs;
        }

        private readonly List<WatchedScreen> screens = new List<WatchedScreen>();
        private readonly Dictionary<string, Timing> timings = new Dictionary<string, Timing>();
        private readonly List<string> timedOrder = new List<string>();

        private GameObject pauseRoot;
        private GameObject settingsRoot;
        private FarmMapUIBuilder map;
        private float nextResolveAt;
        private bool primed;

        private double frameStart;
        private double lastTapTime = double.NaN;

        private RectTransform rect;
        private Text text;
        private bool expanded;
        private string lastResult = "Open a screen to time it";

        public static UiResponseTimerHud Create(Transform canvas, RectTransform fpsCounter, RectTransform moneyPlank)
        {
            GameObject go = new GameObject("UiResponseTimer", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);

            if (fpsCounter != null)
            {
                rect.anchoredPosition = new Vector2(
                    fpsCounter.anchoredPosition.x,
                    fpsCounter.anchoredPosition.y - fpsCounter.sizeDelta.y * fpsCounter.pivot.y - 8f);
            }
            else if (moneyPlank != null)
            {
                rect.anchoredPosition = new Vector2(
                    moneyPlank.anchoredPosition.x - moneyPlank.sizeDelta.x - 12f,
                    moneyPlank.anchoredPosition.y - moneyPlank.sizeDelta.y * 0.3f);
            }
            else
            {
                rect.anchoredPosition = new Vector2(-400f, -40f);
            }

            Image image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.82f);
            image.raycastTarget = true;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 6f);
            textRect.offsetMax = new Vector2(-10f, -6f);

            Text label = textGo.GetComponent<Text>();
            label.font = GameFonts.Primary;
            label.fontSize = 16;
            label.color = TextColor;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;

            UiResponseTimerHud hud = go.AddComponent<UiResponseTimerHud>();
            hud.rect = rect;
            hud.text = label;
            hud.RegisterScreens();
            hud.Redraw();
            return hud;
        }

        private void RegisterScreens()
        {
            AddPanel("Pause menu", () => pauseRoot != null && pauseRoot.activeInHierarchy);
            AddPanel("Settings", () => settingsRoot != null && settingsRoot.activeInHierarchy);
            AddPiece("AI Adviser", HudPiece.AdviserChatPanel);
            AddPiece("Shop", HudPiece.ShopPanel);
            AddPiece("Objectives book", HudPiece.ObjectivesPanel);
            AddPiece("Farmer list", HudPiece.SearchPlayersPanel);
            AddPiece("Marketplace", HudPiece.MarketplacePanel);
            AddPiece("Save Farm prompt", HudPiece.ConfirmPopup);
            AddPanel("Map", () => map != null && map.IsExpanded);
            AddPiece("Backpack", HudPiece.BackpackPanel);
            AddPiece("Crop information", HudPiece.CropInfoPanel);
            AddPiece("Seedling Tent", HudPiece.SeedlingTentPanel);
        }

        private void AddPiece(string label, HudPiece piece)
        {
            AddPanel(label, () => HudRegistry.TryGetPiece(piece, out GameObject go) && go.activeInHierarchy);
        }

        private void AddPanel(string label, System.Func<bool> isOpen)
        {
            screens.Add(new WatchedScreen { label = label, isOpen = isOpen });
        }

        private void OnEnable()
        {
            FarmingInteractionSystem.FarmActionApplied += OnFarmActionApplied;
        }

        private void OnDisable()
        {
            FarmingInteractionSystem.FarmActionApplied -= OnFarmActionApplied;
        }

        private void OnFarmActionApplied(string action)
        {
            if (double.IsNaN(lastTapTime) || frameStart - lastTapTime > CreditWindowSeconds)
                return;

            StartCoroutine(RecordAtEndOfFrame(action, lastTapTime));
            lastTapTime = double.NaN;
        }

        private void Update()
        {
            frameStart = Time.realtimeSinceStartupAsDouble;

            if (TapHappenedThisFrame())
                lastTapTime = frameStart;

            if (Time.unscaledTime >= nextResolveAt)
            {
                nextResolveAt = Time.unscaledTime + ResolveEverySeconds;
                ResolveRoots();
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < screens.Count; i++)
            {
                WatchedScreen screen = screens[i];
                bool open = screen.isOpen();

                if (primed && open && !screen.wasOpen &&
                    !double.IsNaN(lastTapTime) && frameStart - lastTapTime <= CreditWindowSeconds)
                {
                    StartCoroutine(RecordAtEndOfFrame(screen.label, lastTapTime));

                    lastTapTime = double.NaN;
                }

                screen.wasOpen = open;
            }

            primed = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            expanded = !expanded;

            lastTapTime = double.NaN;
            Redraw();
        }

        private IEnumerator RecordAtEndOfFrame(string label, double tapTime)
        {
            yield return new WaitForEndOfFrame();
            Record(label, (Time.realtimeSinceStartupAsDouble - tapTime) * 1000.0);
        }

        private void Record(string label, double ms)
        {
            if (!timings.TryGetValue(label, out Timing timing))
            {
                timing = new Timing();
                timings[label] = timing;
                timedOrder.Add(label);
            }

            timing.count++;
            timing.totalMs += ms;
            timing.lastMs = ms;
            timing.maxMs = System.Math.Max(timing.maxMs, ms);

            lastResult = label + " " + Rounded(ms) + " ms" + (ms > LimitMs ? " (over 2 s)" : "");

            Debug.Log("[UI] " + label + ": " + Rounded(ms) + " ms | avg " +
                      Rounded(timing.totalMs / timing.count) + " | max " + Rounded(timing.maxMs) +
                      " | tries " + timing.count + " | " + SystemInfo.deviceModel);

            Redraw();
        }

        private void Redraw()
        {
            if (text == null || rect == null)
                return;

            if (!expanded)
            {
                text.alignment = TextAnchor.MiddleCenter;
                text.text = "Screen: " + lastResult + "\nTap to list all";
                rect.sizeDelta = new Vector2(CollapsedWidth, 12f + LineHeight * 2f);
                return;
            }

            StringBuilder builder = new StringBuilder("Screen and action times in ms (tap to hide)");
            if (timedOrder.Count == 0)
                builder.Append("\nNothing timed yet");

            foreach (string label in timedOrder)
            {
                Timing timing = timings[label];
                builder.Append('\n').Append(label)
                       .Append(": last ").Append(Rounded(timing.lastMs))
                       .Append(" | avg ").Append(Rounded(timing.totalMs / timing.count))
                       .Append(" | max ").Append(Rounded(timing.maxMs))
                       .Append(" | tries ").Append(timing.count);
            }

            int lines = 1 + Mathf.Max(1, timedOrder.Count);
            text.alignment = TextAnchor.UpperLeft;
            text.text = builder.ToString();
            rect.sizeDelta = new Vector2(ExpandedWidth, 12f + LineHeight * lines);
        }

        private void ResolveRoots()
        {
            if (pauseRoot == null)
                pauseRoot = FindCanvasChild("PauseMenu_Root");
            if (settingsRoot == null)
                settingsRoot = FindCanvasChild("Settings_Root");
            if (map == null)
                map = Object.FindFirstObjectByType<FarmMapUIBuilder>();
        }

        private static GameObject FindCanvasChild(string name)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                Transform child = canvas.transform.Find(name);
                if (child != null)
                    return child.gameObject;
            }

            return null;
        }

        private static bool TapHappenedThisFrame()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
                        return true;
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.leftButton.wasReleasedThisFrame))
                return true;

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }

        private static string Rounded(double ms)
        {
            return ((long)System.Math.Round(ms)).ToString();
        }
    }
}
