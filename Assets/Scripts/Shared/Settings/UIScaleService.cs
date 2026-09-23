using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// Applies the player's interface and text size settings to every canvas and
    /// every label in the game, in every scene.
    ///
    /// All of AgriDabaw's UI is constructed in C# at runtime by a couple of dozen
    /// <c>*UIBuilder</c> classes, and panels are created, shown and destroyed
    /// continuously. There is no single place to hook, so this follows the same
    /// approach <see cref="GameAccessibility"/> already uses for the same reason:
    /// re-scan on a short interval rather than trying to intercept every builder.
    /// That keeps the feature entirely additive - no builder is modified, no
    /// RectTransform is repositioned, and nothing breaks if a new panel is added
    /// later, because it is picked up by the next sweep.
    ///
    /// Two different mechanisms, because the two sliders mean different things:
    ///
    ///   * <b>Interface size</b> divides each canvas's reference resolution. Every
    ///     canvas in the game is ScaleWithScreenSize at 1920x1080 matched to
    ///     height, so halving the reference height doubles everything drawn on it -
    ///     panels, buttons, icons and the text inside them - with no layout maths
    ///     of our own. Nothing in the project reads referenceResolution back, so
    ///     rewriting it has no other effect.
    ///
    ///   * <b>Text size</b> multiplies each label's font size on top of that, for
    ///     players who want readable text without giving up screen space.
    ///
    /// Both default to 1.0, which reproduces today's appearance exactly.
    /// </summary>
    public class UIScaleService : MonoBehaviour
    {
        private const string RuntimeObjectName = "AgriDabao_UIScale";

        /// <summary>
        /// How often newly built UI is picked up.
        ///
        /// Panels that are built and shown in the same instant - a marketplace
        /// listing row, a chat line, a task entry - would otherwise be drawn at
        /// their authored size for one interval before being corrected, which
        /// reads as a flicker. At 10 Hz that window is short enough not to be
        /// seen, and the scan itself is two native type queries over a few hundred
        /// components, which costs far less than the panel it is correcting.
        ///
        /// Neither settings changes nor opening the Settings board wait for this;
        /// both apply immediately.
        /// </summary>
        private const float PollInterval = 0.1f;

        private static UIScaleService instance;

        /// <summary>
        /// A canvas's authored reference resolution, plus what we last wrote, so a
        /// builder re-setting it on an existing canvas is recognised as a new
        /// baseline instead of being treated as our own scaled value.
        /// </summary>
        private sealed class CanvasRecord
        {
            public CanvasScaler scaler;
            public Vector2 baseResolution;
            public Vector2 appliedResolution;
        }

        /// <summary>The same idea for a label's authored font size.</summary>
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

        /// <summary>
        /// Re-applies both scales right now. Called after a scene loads and after
        /// any settings change; safe to call at any time.
        /// </summary>
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
            // The previous scene's canvases and labels are gone; their records would
            // only be pruned lazily, and their instance ids are never reused.
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

        // ------------------------------------------------------------ interface

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
                    // A builder wrote its own reference resolution after we scaled
                    // this canvas. That value is the new authored baseline.
                    record.baseResolution = scaler.referenceResolution;
                }

                Vector2 target = record.baseResolution / uiScale;
                if (scaler.referenceResolution != target)
                    scaler.referenceResolution = target;

                record.appliedResolution = target;
            }

            Prune(canvases, record => record.scaler == null);
        }

        // ----------------------------------------------------------------- text

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
                    // Some panels rewrite a label's size after it is built - the
                    // shop's item name is one. Whatever they set is the new baseline.
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

                // Best-fit labels ignore fontSize and clamp themselves between these
                // two, so they need the same treatment or they would not scale.
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

        /// <summary>
        /// Stops a one-line label from vanishing when it outgrows its own box.
        ///
        /// Unity's legacy Text does not clip a line that is too tall for its rect -
        /// under the default Truncate it discards the line outright, so the label
        /// renders as nothing at all. Playpen Sans has a line box around 1.45x its
        /// font size where the font the UI was laid out against sat near 1.15x, and
        /// a good number of labels live in rects sized as a fixed multiple of their
        /// font. Those flipped from comfortable to invisible on the font change,
        /// and the text setting pushes more of them over the line, because it grows
        /// the font without growing the rect.
        ///
        /// The test below is deliberately narrow. A rect shorter than
        /// <see cref="MinLineBoxRatio"/> times its font size cannot fit even one
        /// line, so Truncate there can only ever produce an empty label - switching
        /// it to Overflow strictly improves matters. Anything with a taller rect is
        /// left alone, which is what keeps the deliberate Truncate on genuinely
        /// multi-line text intact: the climate evaluation body and the social and
        /// marketplace rows all sit in tall containers and use truncation to stay
        /// inside them, and they never match this condition.
        /// </summary>
        private static void RescueFromTruncation(Text text, int fontSize)
        {
            if (text.verticalOverflow != VerticalWrapMode.Truncate)
                return;

            RectTransform rect = text.rectTransform;
            if (rect == null)
                return;

            float available = rect.rect.height;

            // A rect that has not been laid out yet reports zero; leave it for a
            // later sweep rather than acting on a height that is not real.
            if (available <= 0f)
                return;

            if (available < fontSize * MinLineBoxRatio)
                text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        /// <summary>
        /// Height, as a multiple of font size, below which a rect cannot hold one
        /// line of text.
        ///
        /// Read out of PlaypenSans.ttf rather than estimated. With unitsPerEm 1000
        /// the font declares hhea ascender 1170 and descender -340 with no line gap
        /// (1.510 em), and OS/2 winAscent 1294 with winDescent 502 (1.796 em).
        /// FreeType, which is what Unity's dynamic font rendering sits on, normally
        /// takes the larger OS/2 pair, so the threshold is set just above 1.796
        /// rather than between the two - being too high only converts a few labels
        /// that would have been fine to Overflow, which costs nothing on a
        /// single-line label, while being too low leaves them invisible.
        ///
        /// For contrast, the built-in font this UI was originally laid out against
        /// sits near 1.15 em. That 56% jump is why so many boxes that were
        /// comfortable became too short the moment the typeface changed.
        /// </summary>
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
