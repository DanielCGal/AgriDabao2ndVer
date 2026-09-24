using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class FpsCounterHud : MonoBehaviour, IPointerClickHandler
    {
        private const float RefreshSeconds = 0.5f;
        private const float WarmUpSeconds = 2f;
        private const float LogEverySeconds = 60f;

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

            rect.anchoredPosition = moneyPlank != null
                ? new Vector2(moneyPlank.anchoredPosition.x - moneyPlank.sizeDelta.x - 12f,
                              moneyPlank.anchoredPosition.y - moneyPlank.sizeDelta.y * 0.56f)
                : new Vector2(-400f, -70f);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.82f);

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

            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
