using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class UIScaleService : MonoBehaviour
    {
        private const string RuntimeObjectName = "AgriDabao_UIScale";

        private const float PollInterval = 0.1f;

        private static UIScaleService instance;

        private sealed class CanvasRecord
        {
            public CanvasScaler scaler;
            public Vector2 baseResolution;
            public Vector2 appliedResolution;
        }

        private sealed class TextRecord
        {
            public Text text;
            public int baseFontSize;
            public int appliedFontSize;
            public int baseBestFitMin;
            public int baseBestFitMax;
            public int appliedBestFitMin;
            public int appliedBestFitMax;
        }

        private readonly Dictionary<int, CanvasRecord> canvases = new Dictionary<int, CanvasRecord>();
        private readonly Dictionary<int, TextRecord> texts = new Dictionary<int, TextRecord>();

        private readonly List<int> deadKeys = new List<int>();

        private float nextPollTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (instance != null || GameObject.Find(RuntimeObjectName) != null)
                return;

            GameObject go = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(go);
            go.AddComponent<UIScaleService>();
        }

        public static void ApplyNow()
        {
            if (instance != null)
                instance.Apply();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            GameSettings.Changed += Apply;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            GameSettings.Changed -= Apply;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }

        private void Start()
        {
            Apply();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            canvases.Clear();
            texts.Clear();
            Apply();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextPollTime)
                return;

            nextPollTime = Time.unscaledTime + PollInterval;
            Apply();
        }

        private void Apply()
        {
            ApplyCanvasScale(GameSettings.UiScale);
            ApplyTextScale(GameSettings.TextScale);
        }

        private void ApplyCanvasScale(float uiScale)
        {
            if (uiScale <= 0f)
                return;

            CanvasScaler[] found = FindObjectsByType<CanvasScaler>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (CanvasScaler scaler in found)
            {
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    continue;

                int key = scaler.GetInstanceID();
                if (!canvases.TryGetValue(key, out CanvasRecord record))
                {
                    record = new CanvasRecord
                    {
                        scaler = scaler,
                        baseResolution = scaler.referenceResolution,
                        appliedResolution = scaler.referenceResolution
                    };
                    canvases.Add(key, record);
                }
                else if (scaler.referenceResolution != record.appliedResolution)
                {
                    record.baseResolution = scaler.referenceResolution;
                }

                Vector2 target = record.baseResolution / uiScale;
                if (scaler.referenceResolution != target)
                    scaler.referenceResolution = target;

                record.appliedResolution = target;
            }

            Prune(canvases, record => record.scaler == null);
        }

        private void ApplyTextScale(float textScale)
        {
            if (textScale <= 0f)
                return;

            Text[] found = FindObjectsByType<Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Text text in found)
            {
                if (text == null)
                    continue;

                int key = text.GetInstanceID();
                if (!texts.TryGetValue(key, out TextRecord record))
                {
                    record = new TextRecord
                    {
                        text = text,
                        baseFontSize = text.fontSize,
                        appliedFontSize = text.fontSize,
                        baseBestFitMin = text.resizeTextMinSize,
                        baseBestFitMax = text.resizeTextMaxSize,
                        appliedBestFitMin = text.resizeTextMinSize,
                        appliedBestFitMax = text.resizeTextMaxSize
                    };
                    texts.Add(key, record);
                }
                else
                {
                    if (text.fontSize != record.appliedFontSize)
                        record.baseFontSize = text.fontSize;
                    if (text.resizeTextMinSize != record.appliedBestFitMin)
                        record.baseBestFitMin = text.resizeTextMinSize;
                    if (text.resizeTextMaxSize != record.appliedBestFitMax)
                        record.baseBestFitMax = text.resizeTextMaxSize;
                }

                int targetSize = Scaled(record.baseFontSize, textScale);
                if (text.fontSize != targetSize)
                    text.fontSize = targetSize;
                record.appliedFontSize = targetSize;

                RescueFromTruncation(text, targetSize);

                if (text.resizeTextForBestFit)
                {
                    int targetMin = Scaled(record.baseBestFitMin, textScale);
                    int targetMax = Scaled(record.baseBestFitMax, textScale);

                    if (text.resizeTextMinSize != targetMin)
                        text.resizeTextMinSize = targetMin;
                    if (text.resizeTextMaxSize != targetMax)
                        text.resizeTextMaxSize = targetMax;

                    record.appliedBestFitMin = targetMin;
                    record.appliedBestFitMax = targetMax;
                }
            }

            Prune(texts, record => record.text == null);
        }

        private static void RescueFromTruncation(Text text, int fontSize)
        {
            if (text.verticalOverflow != VerticalWrapMode.Truncate)
                return;

            RectTransform rect = text.rectTransform;
            if (rect == null)
                return;

            float available = rect.rect.height;

            if (available <= 0f)
                return;

            if (available < fontSize * MinLineBoxRatio)
                text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private const float MinLineBoxRatio = 1.85f;

        private static int Scaled(int baseValue, float scale)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseValue * scale));
        }

        private void Prune<T>(Dictionary<int, T> table, System.Func<T, bool> isDead)
        {
            deadKeys.Clear();
            foreach (KeyValuePair<int, T> pair in table)
            {
                if (isDead(pair.Value))
                    deadKeys.Add(pair.Key);
            }

            foreach (int key in deadKeys)
                table.Remove(key);
        }
    }
}
