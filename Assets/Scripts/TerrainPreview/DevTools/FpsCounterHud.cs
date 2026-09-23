using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// Frame rate readout for the device performance tests, in a small box to the
    /// left of the money plank. A developer tool, so it is drawn plainly in the same
    /// dark style as the Dev Tools panel rather than with the game's wooden art.
    ///
    /// One live number cannot answer what those tests ask. The requirement (NFR-01)
    /// is an average of at least 30 frames per second over a continuous ten-minute
    /// session, so the box shows the live rate, the average since measuring began,
    /// the lowest one-second rate in that time - the stutters a player actually
    /// notices - and how long it has been measuring. Tapping the box starts a new
    /// measurement, so a run can begin from the same moment on every phone.
    ///
    /// Measuring waits until the farm has finished loading, because loading-screen
    /// frames say nothing about gameplay, and a phone coming back from the
    /// background does not count the time it was away as one enormous frame. The
    /// game caps itself at 60 frames per second (MobilePerformanceBootstrap), so no
    /// phone reads higher than that.
    ///
    /// The same figures are written to the device log once a minute, so a run can
    /// also be read back with adb logcat rather than only from screenshots.
    ///
    /// Switched on and off with Show Fps Counter on the Inventory UI Builder in the
    /// TerrainPreview scene. It is a testing aid; switch it off for release builds.
    /// </summary>
    public class FpsCounterHud : MonoBehaviour, IPointerClickHandler
    {
        private const float RefreshSeconds = 0.5f;
        private const float WarmUpSeconds = 2f;
        private const float LogEverySeconds = 60f;

        /// <summary>NFR-01's floor. The live rate turns red below it.</summary>
        private const float TargetFps = 30f;

        private static readonly Color TextColor = new Color(0.85f, 0.88f, 0.92f, 1f);
        private static readonly Color Warning = new Color(1f, 0.38f, 0.32f, 1f);

        private Text rateText;
        private Text statsText;

        private float liveTime;
        private int liveFrames;

        private bool measuring;
        private float warmUpLeft = WarmUpSeconds;
        private float measuredTime;
        private int measuredFrames;
        private float secondTime;
        private int secondFrames;
        private float lowestSecond = float.MaxValue;
        private float nextLogAt = LogEverySeconds;
        private bool skipNextFrame;

        public static FpsCounterHud Create(Transform canvas, RectTransform moneyPlank)
        {
            GameObject go = new GameObject("FpsCounter", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(260f, 70f);

            // Level with the amount printed on the money plank, which sits a little
            // below that plank's middle, and clear of the coins on its left end.
            rect.anchoredPosition = moneyPlank != null
                ? new Vector2(moneyPlank.anchoredPosition.x - moneyPlank.sizeDelta.x - 12f,
                              moneyPlank.anchoredPosition.y - moneyPlank.sizeDelta.y * 0.56f)
                : new Vector2(-400f, -70f);

            // The Dev Tools panel's background.
            Image image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.82f);

            // The box is the button that restarts the measurement.
            image.raycastTarget = true;

            FpsCounterHud hud = go.AddComponent<FpsCounterHud>();
            hud.rateText = CreateText(go.transform, "Rate", 24, FontStyle.Bold,
                new Vector2(0f, 0.45f), Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, -4f));
            hud.statsText = CreateText(go.transform, "Stats", 16, FontStyle.Normal,
                Vector2.zero, new Vector2(1f, 0.5f), new Vector2(10f, 6f), new Vector2(-10f, 0f));

            hud.rateText.text = "-- FPS";
            hud.statsText.text = "Waiting for the farm";
            return hud;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // The first frame back from the background spans the whole time away.
            if (skipNextFrame)
            {
                skipNextFrame = false;
                return;
            }

            liveTime += dt;
            liveFrames++;

            if (measuring)
            {
                Measure(dt);
            }
            else if (WorldReady())
            {
                warmUpLeft -= dt;
                if (warmUpLeft <= 0f)
                    StartMeasuring("farm loaded");
            }

            if (liveTime >= RefreshSeconds)
            {
                Redraw(liveFrames / liveTime);
                liveTime = 0f;
                liveFrames = 0;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
                skipNextFrame = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (measuring)
                Debug.Log("[FPS] Run ended by a tap: " + Summary());

            StartMeasuring("tapped");
            statsText.text = "Avg -- | Low -- | 00:00";
        }

        private void Measure(float dt)
        {
            measuredTime += dt;
            measuredFrames++;
            secondTime += dt;
            secondFrames++;

            if (secondTime >= 1f)
            {
                lowestSecond = Mathf.Min(lowestSecond, secondFrames / secondTime);
                secondTime = 0f;
                secondFrames = 0;
            }

            if (measuredTime >= nextLogAt)
            {
                nextLogAt += LogEverySeconds;
                Debug.Log("[FPS] " + Summary());
            }
        }

        private void StartMeasuring(string reason)
        {
            measuring = true;
            measuredTime = 0f;
            measuredFrames = 0;
            secondTime = 0f;
            secondFrames = 0;
            lowestSecond = float.MaxValue;
            nextLogAt = LogEverySeconds;

            Debug.Log("[FPS] Measuring from now (" + reason + ") on " + SystemInfo.deviceModel +
                      " | " + SystemInfo.operatingSystem +
                      " | " + SystemInfo.processorType +
                      " | " + SystemInfo.systemMemorySize + " MB RAM" +
                      " | " + SystemInfo.graphicsDeviceName +
                      " | " + Screen.width + "x" + Screen.height +
                      " | cap " + Application.targetFrameRate + " fps");
        }

        private void Redraw(float liveFps)
        {
            rateText.text = Mathf.RoundToInt(liveFps) + " FPS";
            rateText.color = liveFps < TargetFps ? Warning : TextColor;

            statsText.text = measuring
                ? "Avg " + Mathf.RoundToInt(Average()) + " | Low " + LowestText("F0") +
                  " | " + Clock(measuredTime)
                : "Waiting for the farm";
        }

        /// <summary>
        /// Whether gameplay has started. No persistence manager means a scene opened
        /// straight in the Editor, which has no farm load to wait for.
        /// </summary>
        private static bool WorldReady()
        {
            return FarmPersistenceManager.Instance == null || FarmPersistenceManager.Instance.IsWorldReady;
        }

        private float Average()
        {
            return measuredTime > 0f ? measuredFrames / measuredTime : 0f;
        }

        private string LowestText(string format)
        {
            return lowestSecond < float.MaxValue ? lowestSecond.ToString(format) : "--";
        }

        private string Summary()
        {
            return Clock(measuredTime) + " measured | avg " + Average().ToString("F1") +
                   " | low " + LowestText("F1") + " | " + SystemInfo.deviceModel;
        }

        private static string Clock(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        private static Text CreateText(Transform parent, string name, int size, FontStyle style,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Text text = go.GetComponent<Text>();
            text.font = GameFonts.Primary;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextColor;
            text.raycastTarget = false;

            // One line each, kept on screen at any text size instead of being dropped
            // when the Text Size setting grows the font past its row.
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
