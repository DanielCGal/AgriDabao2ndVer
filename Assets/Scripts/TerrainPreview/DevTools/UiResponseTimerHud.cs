using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// Times how long each farm screen takes to appear after the tap that opens it,
    /// for the device performance tests (NFR-02: visible feedback within 2 seconds).
    /// A developer tool, drawn plainly like the Dev Tools panel, under the FPS counter.
    ///
    /// The clock starts at the frame in which the tap is processed and stops at the
    /// end of the frame in which the screen is first drawn, so it includes whatever
    /// work opening the screen does - building rows, laying out text - as well as
    /// rendering it. It measures the screen appearing, not data arriving from the
    /// server afterwards; the marketplace, farmer list and AI Adviser still fill in
    /// once their requests return.
    ///
    /// Nothing in the builders is changed. The timer watches each screen's own
    /// open state - the panels registered with HudRegistry, the map's expanded
    /// state, and the pause and settings roots - and credits a screen that opens to
    /// the most recent tap. One tap is credited to one screen, and a screen that
    /// opens more than 5 seconds after the last tap (a notification, say) is not
    /// counted.
    ///
    /// Planting, watering and harvesting are timed the same way, from the tap to
    /// the end of the frame the result is drawn in. They are credited when the
    /// farming system reports them, which is inside the tap's own frame, so the
    /// crop information panel an action refreshes is not also timed as opened.
    ///
    /// Each result is also written to the device log as a [UI] line. Tap the box to
    /// list every screen timed so far with its last, average and slowest time.
    /// Switched on and off with Show Ui Response Timer on the Inventory UI Builder in
    /// the TerrainPreview scene.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public class UiResponseTimerHud : MonoBehaviour, IPointerClickHandler
    {
        private const double CreditWindowSeconds = 5.0;
        private const float ResolveEverySeconds = 1f;

        /// <summary>NFR-02's limit, in milliseconds.</summary>
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

        /// <summary>
        /// Builds the box under <paramref name="fpsCounter"/>, or where the FPS counter
        /// would be when that is switched off.
        /// </summary>
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

            // The Dev Tools panel's background. The box is the button that shows
            // and hides the full list.
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
            // The event is static, so a destroyed box must not stay subscribed.
            FarmingInteractionSystem.FarmActionApplied -= OnFarmActionApplied;
        }

        /// <summary>
        /// Called inside the tap's frame, after this box's Update and before its
        /// LateUpdate, so the tap is taken here - before the crop information panel
        /// the action refreshed could be credited with it.
        /// </summary>
        private void OnFarmActionApplied(string action)
        {
            if (double.IsNaN(lastTapTime) || frameStart - lastTapTime > CreditWindowSeconds)
                return;

            StartCoroutine(RecordAtEndOfFrame(action, lastTapTime));
            lastTapTime = double.NaN;
        }

        private void Update()
        {
            // Runs before the EventSystem (see the execution order above), so this is
            // the moment the frame that handles the tap began.
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

                    // One tap opens one screen; anything opening later is not its doing.
                    lastTapTime = double.NaN;
                }

                screen.wasOpen = open;
            }

            primed = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            expanded = !expanded;

            // Tapping this box opens no screen, so the tap is not left waiting to be
            // credited to something that opens a few seconds later.
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

        /// <summary>
        /// The pause and settings screens are not in HudRegistry and the map is built
        /// by its own bootstrap, so they are looked up by name until they exist. Their
        /// open state is taken as it is when found, so a screen found already open is
        /// not mistaken for one that just opened.
        /// </summary>
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
