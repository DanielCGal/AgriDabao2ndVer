using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace AgriDabao3D
{
    /// <summary>
    /// Picking where the farm will be, in two steps.
    ///
    /// STEP ONE - the district. Only the district being looked at is tinted on the
    /// map, and the camera travels to it as the player steps through with the
    /// arrows. There is no selection box yet: choosing a district and choosing a
    /// plot inside it are different questions, and asking both at once was what
    /// left beta testers unsure which district they were even looking at.
    ///
    /// STEP TWO - the area. Pressing Next locks the district in and swaps the map
    /// to that district's barangays, drawn green where farming is allowed and red
    /// where it is not. The box appears, Generate replaces Next, and Generate
    /// checks the box against the district's agricultural area spot picture. Being
    /// inside a production district is no longer enough on its own - a barangay
    /// inside one can still be off limits, and the red areas are where.
    /// </summary>
    public class AreaSelectionBuilder : MonoBehaviour
    {
        [Header("Sprites")]
        [Tooltip("The satellite map every district picture is drawn on top of. Its " +
                 "corners are the coordinates in DavaoGeoReference, so the two are " +
                 "replaced together or the farm is generated somewhere else.")]
        public Sprite davaoMapSprite;

        [Header("District Map Art")]
        [Tooltip("Three pictures per district. Rows are filled in with the district " +
                 "names automatically; drop the art into the slots. A district whose " +
                 "art is missing still appears in the selector, but says so rather " +
                 "than generating a farm on land nobody has checked.")]
        public DistrictMapArt[] districtArt = BuildPlaceholderArt();

        [Header("Selection Box Size (normalized 0 to 1)")]
        [Range(0.01f, 1f)] public float areaWidth = 0.05f;
        [Range(0.01f, 1f)] public float areaHeight = 0.05f;

        [Tooltip("Where the box sits when a district has no art to centre it on.")]
        [Range(0f, 1f)] public float startX = 0.30f;
        [Range(0f, 1f)] public float startY = 0.47f;

        [Tooltip("Points sampled across the box, per axis, when checking it against " +
                 "the agricultural area spot picture. 7 means 49 checks.")]
        [Range(3, 15)] public int areaSampleResolution = 7;

        [Tooltip("Smallest size the selection box can be grabbed by on screen, in canvas " +
                 "units. 0 means only the box itself can be grabbed, at every zoom, and a " +
                 "drag anywhere else moves the map. The box is only 14 to 39 units across " +
                 "at the zoom a district opens at, which is small for a fingertip - raise " +
                 "this to around 40 if it proves hard to pick up on a phone. Either way it " +
                 "never makes the grab area bigger than the box once the box is larger " +
                 "than this on screen.")]
        [Min(0f)] public float selectionBoxMinGrabSize = 0f;

        [Header("Scene")]
        public string terrainPreviewSceneName = "TerrainPreview";
        public string backSceneName = "MainMenu";

        [Header("Map Zoom")]
        public float minZoom = 1f;
        public float maxZoom = 5f;
        public float mouseWheelZoomSpeed = 0.18f;
        public float pinchZoomSpeed = 0.008f;

        // Replaces lockMapView under a new name on purpose: the scene saved that one
        // as on, and carrying it across would keep dragging switched off.
        [Tooltip("Let the player drag the map inside the frame. The drag is held to the " +
                 "view each district opens at - at that zoom there is nowhere further to " +
                 "go, and after zooming in the player can move around inside it but " +
                 "never out onto a neighbouring district.")]
        public bool allowMapDragging = true;

        [Header("District Framing")]
        [Tooltip("Fraction of the view a district should fill when focused (higher = tighter zoom).")]
        [Range(0.3f, 1f)] public float districtFillFraction = 0.7f;

        [Tooltip("How long the map takes to travel from one district to the next. " +
                 "Set to 0 to cut straight there instead of gliding.")]
        [Range(0f, 2f)] public float districtGlideSeconds = 0.55f;

        [Header("Keeping Districts Clear of the UI")]
        [Tooltip("Height of the hint plank along the top, in canvas units. Districts " +
                 "are framed below it.")]
        public float uiTopMargin = 165f;

        [Tooltip("Height of the district sign and button row along the bottom. " +
                 "Districts are framed above it.")]
        public float uiBottomMargin = 215f;

        [Tooltip("How far out the view may pull to get a tall district clear of the " +
                 "UI. Below 1 the map stops reaching the left and right edges of the " +
                 "screen and the backdrop shows beside it, so this is the trade: 1 " +
                 "keeps the map edge to edge always and lets the tallest districts run " +
                 "under the UI, lower values show those districts whole on a narrower " +
                 "map. Districts that already fit are never pulled back below 1.")]
        [Range(0.4f, 1f)] public float minimumDistrictZoom = 0.5f;

        [Header("Background")]
        [Tooltip("Looping video behind the framed map. Drop a clip in and it plays; " +
                 "leave it empty and the dimmed Davao map is used instead. The clip " +
                 "is played with its own audio switched off, so a clip with music in " +
                 "it stays silent.")]
        public VideoClip backgroundVideoClip;

        [Tooltip("Size of the texture the video is drawn into. Match the clip.")]
        public int backgroundVideoWidth = 1920;
        public int backgroundVideoHeight = 1080;

        [Tooltip("How far to darken the background so the framed map reads in front " +
                 "of it. 0 leaves the video at full brightness.")]
        [Range(0f, 0.9f)] public float backgroundDim = 0.45f;

        [Tooltip("Tint of the dimmed Davao map used as the background when no video " +
                 "clip is set, and shown behind the video until its first frame.")]
        public Color backdropTint = new Color(0.30f, 0.33f, 0.30f, 1f);

        [Header("Map Frame")]
        [Tooltip("Hangs the wooden Map Frame from the UI theme around the map. The " +
                 "art is a landscape frame and is stretched to the map's upright " +
                 "shape, so its top and bottom rails come out thicker than its sides.")]
        public bool showMapFrame = true;

        [Tooltip("Scales the whole framed picture. 1 sits the opening exactly in the " +
                 "gap between the hint plank and the button row.")]
        [Range(0.5f, 1.4f)] public float mapFrameScale = 1f;

        [Header("Area Selection Tutorial")]
        [Tooltip("Ask whether the player wants the tutorial each time this screen opens. " +
                 "The answer is deliberately not remembered: a player who leaves partway " +
                 "through picking an area is asked again when they come back.")]
        public bool offerTutorial = true;

        [Tooltip("Pages shown after the player says Yes, before they choose a district. " +
                 "Drop each page's screenshot into its Picture slot; the text is editable too.")]
        public TutorialSlide[] districtTutorialSlides = DefaultDistrictTutorialSlides();

        [Tooltip("Pages shown the first time the player presses ENTER on a district, " +
                 "before they drag the box.")]
        public TutorialSlide[] areaTutorialSlides = DefaultAreaTutorialSlides();

        private const string RootName = "AreaSelection_Runtime";

        /// <summary>Which of the two questions the screen is asking right now.</summary>
        private enum SelectionPhase
        {
            District,
            Area
        }

        private RectTransform viewportRect;
        private RectTransform backdropRect;
        private Image backdropImage;
        private RectTransform frameGroupRect;
        private RectTransform mapWindowRect;
        private Image mapFrameImage;
        private RectTransform mapRect;
        private Image districtOverlayImage;
        private RectTransform selectionRect;
        private Image selectionImage;
        private DraggableSelectionBox draggableBox;
        private MapZoomPanController zoomPan;

        private Text infoText;
        private Text districtLabel;
        private Image districtSignImage;
        private int currentDistrictIndex;
        private SelectionPhase phase = SelectionPhase.District;

        private UIThemeSprites theme;

        private GameObject generateButtonGo;
        private GameObject confirmDistrictButtonGo;
        private GameObject prevDistrictButtonGo;
        private GameObject nextDistrictButtonGo;

        private GameObject popupPanel;
        private Text popupText;
        private Coroutine layoutRoutine;
        private DescriptionBoard districtBoard;

        private const string TutorialRootName = "AreaSelectionTutorial_Runtime";
        private RectTransform tutorialLayer;
        private GameObject tutorialPromptBlocker;
        private TutorialSlideBoard tutorialBoard;
        private bool tutorialAccepted;
        private bool areaTutorialShown;
        private Vector2 lastScreenSize;

        // The map zoom the box's grab area was last sized for; -1 forces a resize.
        private float appliedGrabZoom = -1f;

        private void Awake()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            EnsureDistrictArt();
        }

        private void OnValidate()
        {
            EnsureDistrictArt();
        }

        private void Start()
        {
            Build();
        }

        [ContextMenu("Build Area Selection")]
        public void Build()
        {
            var oldRoot = GameObject.Find(RootName);
            if (oldRoot != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(oldRoot);
#else
                Destroy(oldRoot);
#endif
            }

            // Built under the canvas rather than under the screen's own root, so it
            // sits above everything on it; cleared here for the same reason the root
            // is, so building the screen twice does not stack two tutorials.
            var oldTutorial = GameObject.Find(TutorialRootName);
            if (oldTutorial != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(oldTutorial);
#else
                Destroy(oldTutorial);
#endif
            }

            theme = UIThemeSprites.Instance;
            EnsureDistrictArt();
            ReportArtProblems();

            EnsureEventSystem();
            var canvas = EnsureCanvas();

            var root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            viewportRect = root.GetComponent<RectTransform>();
            Stretch(viewportRect);

            CreateBackdrop(viewportRect);
            CreateVideoBackground(viewportRect);
            CreateBackgroundDim(viewportRect);
            CreateFrameGroup(viewportRect);
            mapRect = CreateMap(mapWindowRect);
            selectionRect = CreateSelectionBox(mapRect);
            CreateInfoText(viewportRect);
            CreateGenerateButton(viewportRect);
            CreateConfirmDistrictButton(viewportRect);
            CreateBackButton(viewportRect);
            CreateDistrictSelector(viewportRect);
            CreateCheckDescriptionButton(viewportRect);
            CreatePopup(viewportRect);

            // Built last so it is the newest sibling and already draws over the
            // map and the selector; Show also re-raises it each time.
            districtBoard = DescriptionBoard.Create(viewportRect, theme);

            lastScreenSize = new Vector2(Screen.width, Screen.height);
            appliedGrabZoom = -1f;

            currentDistrictIndex = 0;
            phase = SelectionPhase.District;
            ApplyPhase(SelectionPhase.District);
            RefreshDistrictChrome();

            // Sized and framed once here and again from the coroutine below. The
            // map's height is worked out from the canvas width, and if that is
            // still zero on this frame the map would be a zero-height strip until
            // the coroutine catches it - a blank screen for the first frame or two.
            Canvas.ForceUpdateCanvases();
            LayoutMap();
            FocusCurrentDistrict(instant: true);

            if (layoutRoutine != null)
                StopCoroutine(layoutRoutine);
            layoutRoutine = StartCoroutine(LayoutThenFrameFirstDistrict());

            BuildTutorial(canvas.transform);
            tutorialAccepted = false;
            areaTutorialShown = false;

            if (offerTutorial)
                OfferTutorial();
        }

        /// <summary>
        /// The map's height depends on the canvas width, which is not known until
        /// the layout has run, so the very first framing waits for it. Later
        /// district switches do not - by then the sizes are settled, and waiting
        /// would put a two frame stutter at the start of every glide.
        /// </summary>
        private IEnumerator LayoutThenFrameFirstDistrict()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutMap();

            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutMap();

            FocusCurrentDistrict(instant: true);

            // The remaining districts are decoded one per frame while the player is
            // still reading the first screen, so pressing >> never waits on a
            // picture being read for the first time.
            for (int i = 0; i < DistrictCount(); i++)
            {
                DistrictMapArt art = districtArt[i];
                if (art != null && art.agriculturalAreaSpot != null)
                    DistrictAreaSpot.Prewarm(art.agriculturalAreaSpot);

                yield return null;
            }

            layoutRoutine = null;
        }

        private void OnDestroy()
        {
            // The decoded area spot maps are only of use on this screen, and the
            // cache is static, so it would otherwise follow the player into the farm.
            DistrictAreaSpot.ClearCache();
        }

        private void Update()
        {
            UpdateSelectionGrabArea();

            Vector2 screen = new Vector2(Screen.width, Screen.height);
            if (screen == lastScreenSize)
                return;

            lastScreenSize = screen;
            LayoutMap();
            FocusCurrentDistrict(instant: true);
        }

        // ------------------------------------------------------------------
        // District art
        // ------------------------------------------------------------------

        private static DistrictMapArt[] BuildPlaceholderArt()
        {
            string[] order = DavaoDistrictService.ProductiveDistrictOrder;
            var rows = new DistrictMapArt[order.Length];

            for (int i = 0; i < order.Length; i++)
                rows[i] = new DistrictMapArt { districtName = order[i] };

            return rows;
        }

        /// <summary>
        /// Keeps the art list usable no matter what state the Inspector is in: a
        /// list that has never been filled in gets every production district as
        /// empty rows, a row added by hand gets a name so it is not a blank entry in
        /// the selector, and a district the game knows but this list does not is
        /// added at the end. Without that last step a district added in code would
        /// be missing from the selector while the shop and the seed pools already
        /// name it. Its row starts empty, and the screen refuses to generate on it
        /// until the three pictures are dropped in.
        /// </summary>
        private void EnsureDistrictArt()
        {
            if (districtArt == null || districtArt.Length == 0)
            {
                districtArt = BuildPlaceholderArt();
                return;
            }

            string[] order = DavaoDistrictService.ProductiveDistrictOrder;

            for (int i = 0; i < districtArt.Length; i++)
            {
                if (districtArt[i] == null)
                    districtArt[i] = new DistrictMapArt();

                if (string.IsNullOrWhiteSpace(districtArt[i].districtName) && i < order.Length)
                    districtArt[i].districtName = order[i];
            }

            var rows = new List<DistrictMapArt>(districtArt);

            foreach (string district in order)
            {
                bool listed = rows.Exists(row => string.Equals(
                    row.districtName != null ? row.districtName.Trim() : "",
                    district,
                    System.StringComparison.OrdinalIgnoreCase));

                if (!listed)
                    rows.Add(new DistrictMapArt { districtName = district });
            }

            if (rows.Count != districtArt.Length)
                districtArt = rows.ToArray();
        }

        private void ReportArtProblems()
        {
            if (districtArt == null)
                return;

            var missing = new StringBuilder();

            for (int i = 0; i < districtArt.Length; i++)
            {
                DistrictMapArt art = districtArt[i];
                if (art == null || art.IsComplete)
                    continue;

                missing.Append("\n  ").Append(DisplayName(art.districtName)).Append(": ");

                if (art.districtHighlight == null) missing.Append("district highlight, ");
                if (art.barangayHighlight == null) missing.Append("barangay highlight, ");
                if (art.agriculturalAreaSpot == null) missing.Append("agricultural area spot, ");

                missing.Length -= 2;
            }

            if (missing.Length > 0)
            {
                Debug.LogWarning(
                    "Area selection: district art is still missing. Those districts can be " +
                    "browsed but not generated on." + missing);
            }

            WarnAboutMismatchedArt();
        }

        /// <summary>
        /// Every picture is stretched over the same rectangle, so one exported at a
        /// different shape would put its barangays somewhere the satellite does not
        /// agree with. Cheaper to say so here than to hunt it as a visual bug.
        /// </summary>
        private void WarnAboutMismatchedArt()
        {
            float baseAspect = SpriteAspect(davaoMapSprite);
            if (baseAspect <= 0f || districtArt == null)
                return;

            for (int i = 0; i < districtArt.Length; i++)
            {
                DistrictMapArt art = districtArt[i];
                if (art == null)
                    continue;

                WarnIfAspectDiffers(art.districtHighlight, baseAspect, art.districtName, "district highlight");
                WarnIfAspectDiffers(art.barangayHighlight, baseAspect, art.districtName, "barangay highlight");
                WarnIfAspectDiffers(art.agriculturalAreaSpot, baseAspect, art.districtName, "agricultural area spot");
            }
        }

        private static void WarnIfAspectDiffers(Sprite sprite, float baseAspect, string district, string slot)
        {
            float aspect = SpriteAspect(sprite);
            if (aspect <= 0f)
                return;

            if (Mathf.Abs(aspect - baseAspect) / baseAspect <= 0.01f)
                return;

            Debug.LogWarning(
                "Area selection: " + DisplayName(district) + "'s " + slot + " (\"" + sprite.name +
                "\") is a different shape from the satellite map, so it will not line up " +
                "with it. Export every district picture on the same canvas size.");
        }

        private static float SpriteAspect(Sprite sprite)
        {
            if (sprite == null || sprite.rect.width < 0.5f)
                return 0f;

            return sprite.rect.height / sprite.rect.width;
        }

        private static string DisplayName(string districtName)
        {
            return string.IsNullOrWhiteSpace(districtName) ? "(unnamed district)" : districtName.Trim();
        }

        private int DistrictCount()
        {
            return districtArt == null ? 0 : districtArt.Length;
        }

        private DistrictMapArt CurrentArt()
        {
            int count = DistrictCount();
            if (count == 0)
                return null;

            int i = ((currentDistrictIndex % count) + count) % count;
            return districtArt[i];
        }

        private string CurrentDistrictName()
        {
            DistrictMapArt art = CurrentArt();
            return art == null || art.districtName == null ? "" : art.districtName.Trim();
        }

        // ------------------------------------------------------------------
        // Scene plumbing
        // ------------------------------------------------------------------

        private Canvas EnsureCanvas()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f; // landscape: scale by height

                if (canvas.GetComponent<GraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<GraphicRaycaster>();

                return canvas;
            }

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasScaler = canvasGo.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 1f; // landscape: scale by height

            return canvas;
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // ------------------------------------------------------------------
        // The map
        // ------------------------------------------------------------------

        /// <summary>
        /// The same picture as the map, drawn behind it at the size it would have if
        /// it were not zoomed at all, and dimmed.
        ///
        /// It only shows where the framed map does not reach, which happens when a
        /// district has been pulled back to get clear of the UI. A flat panel there
        /// reads as a hole in the screen; the same view dimmed reads as the map
        /// carrying on past the edge of what is in focus, which is what it is.
        /// </summary>
        private void CreateBackdrop(RectTransform parent)
        {
            var go = new GameObject("MapBackdrop", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            backdropRect = go.GetComponent<RectTransform>();
            backdropRect.anchorMin = backdropRect.anchorMax = new Vector2(0.5f, 0.5f);
            backdropRect.pivot = new Vector2(0.5f, 0.5f);
            backdropRect.anchoredPosition = Vector2.zero;

            backdropImage = go.GetComponent<Image>();
            backdropImage.color = backdropTint;
            backdropImage.preserveAspect = false;
            backdropImage.raycastTarget = false;
        }

        private void CreateVideoBackground(RectTransform parent)
        {
            LoopingVideoBackground.Create(
                "AreaSelectionVideoBackground", parent, backgroundVideoClip,
                backgroundVideoWidth, backgroundVideoHeight);
        }

        private void CreateBackgroundDim(RectTransform parent)
        {
            if (backgroundDim <= 0.001f)
                return;

            var go = new GameObject("BackgroundDim", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());

            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, backgroundDim);
            image.raycastTarget = false;
        }

        /// <summary>
        /// The wooden frame and the window it holds.
        ///
        /// The window is what the map is now seen through, so it - not the whole
        /// screen - is what the district framing measures against, and it is masked
        /// so a zoomed map is cut off at the frame rather than spilling across the
        /// screen. The frame itself is drawn after the window so its inner edge
        /// laps over the map, the way a real frame sits on a photograph.
        /// </summary>
        private void CreateFrameGroup(RectTransform parent)
        {
            var groupGo = new GameObject("MapFrameGroup", typeof(RectTransform));
            groupGo.transform.SetParent(parent, false);

            frameGroupRect = groupGo.GetComponent<RectTransform>();
            frameGroupRect.anchorMin = frameGroupRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameGroupRect.pivot = new Vector2(0.5f, 0.5f);

            var windowGo = new GameObject("MapWindow", typeof(RectTransform), typeof(RectMask2D));
            windowGo.transform.SetParent(groupGo.transform, false);

            mapWindowRect = windowGo.GetComponent<RectTransform>();
            mapWindowRect.anchorMin = mapWindowRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapWindowRect.pivot = new Vector2(0.5f, 0.5f);
            mapWindowRect.anchoredPosition = Vector2.zero;

            Sprite frame = showMapFrame ? theme?.mapFrame : null;
            if (frame == null)
                return;

            var frameGo = new GameObject("MapFrame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(groupGo.transform, false);
            Stretch(frameGo.GetComponent<RectTransform>());

            mapFrameImage = frameGo.GetComponent<Image>();
            mapFrameImage.sprite = frame;

            // Stretched rather than kept to its own proportions - the art is a
            // landscape frame and the map is upright, which is the warp the frame
            // was asked to take.
            mapFrameImage.preserveAspect = false;
            mapFrameImage.raycastTarget = false;
        }

        /// <summary>How far the frame art insets its opening, as a fraction of its size.</summary>
        private Vector2 FrameInset()
        {
            if (mapFrameImage == null || theme == null)
                return Vector2.zero;

            Vector2 inset = theme.mapFieldInsetFraction;
            return new Vector2(
                Mathf.Clamp(inset.x, 0f, 0.45f),
                Mathf.Clamp(inset.y, 0f, 0.45f));
        }

        private RectTransform CreateMap(RectTransform parent)
        {
            var map = CreateImage("DavaoMap", parent, davaoMapSprite);
            var rect = map.rectTransform;

            // Pinned to the left and right edges of the screen and centred
            // vertically, with the height worked out from the picture's own shape
            // in LayoutMap. The map used to be stretched to the screen on both
            // axes, which squashed it; now it keeps its proportions and is simply
            // taller than the screen, which is what the view scrolls through.
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, parent.rect.height);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            map.preserveAspect = false;
            map.raycastTarget = true;

            // The per-district picture rides on the map as a child, so zooming and
            // panning move the two together and the barangays stay where the
            // satellite puts them. It never takes clicks - the box and the map's own
            // zoom handler are underneath it.
            var overlay = CreateImage("DistrictHighlight", rect, null);
            Stretch(overlay.rectTransform);
            overlay.preserveAspect = false;
            overlay.raycastTarget = false;
            overlay.enabled = false;
            districtOverlayImage = overlay;

            zoomPan = map.gameObject.AddComponent<MapZoomPanController>();
            zoomPan.viewport = parent;
            zoomPan.content = rect;
            zoomPan.minZoom = minZoom;
            zoomPan.maxZoom = maxZoom;
            zoomPan.mouseWheelZoomSpeed = mouseWheelZoomSpeed;
            zoomPan.pinchZoomSpeed = pinchZoomSpeed;

            // Both are held to the district. FocusCurrentDistrict raises the zoom floor
            // to the district's own framing and records that view as the anchor, and
            // the controller keeps every later view - zoomed or dragged - inside it.
            // So zooming out stops where the district opened, and a drag can explore
            // a zoomed-in district without ever reaching the one next door.
            zoomPan.allowUserPan = allowMapDragging;
            zoomPan.allowUserZoom = true;

            return rect;
        }

        /// <summary>
        /// Gives the map its true shape at the current screen width. Called once the
        /// canvas has laid out, and again whenever the window changes size.
        /// </summary>
        /// <summary>
        /// Hangs the framed picture in the gap the UI leaves and sizes the map to
        /// the opening, so that at zoom 1 the whole of Davao sits in the frame and
        /// zooming in frames one district of it.
        /// </summary>
        private void LayoutMap()
        {
            if (mapRect == null || viewportRect == null || mapWindowRect == null)
                return;

            float screenWidth = viewportRect.rect.width;
            float screenHeight = viewportRect.rect.height;
            if (screenWidth < 1f || screenHeight < 1f)
                return;

            float aspect = MapAspect();
            if (aspect <= 0f)
                return;

            // The picture hangs in the strip between the hint plank and the button
            // row, centred on that strip rather than on the screen - the two bands
            // are not the same height.
            float clearHeight = Mathf.Max(160f, screenHeight - uiTopMargin - uiBottomMargin);
            float clearCentreY = (uiBottomMargin - uiTopMargin) * 0.5f;

            float openingHeight = clearHeight * mapFrameScale;
            float openingWidth = openingHeight / aspect;

            // The frame is bigger than its opening by however much of the art is
            // rail, so the opening lands exactly on the strip and the woodwork
            // reaches out past it, behind the plank and the sign.
            Vector2 inset = FrameInset();
            float frameWidth = openingWidth / Mathf.Max(0.1f, 1f - inset.x * 2f);
            float frameHeight = openingHeight / Mathf.Max(0.1f, 1f - inset.y * 2f);

            if (frameGroupRect != null)
            {
                frameGroupRect.sizeDelta = new Vector2(frameWidth, frameHeight);
                frameGroupRect.anchoredPosition = new Vector2(0f, clearCentreY);
            }

            mapWindowRect.sizeDelta = new Vector2(openingWidth, openingHeight);

            // Width comes from the window through the stretched anchors; the height
            // is the map's own shape, which makes it exactly fill the opening.
            mapRect.sizeDelta = new Vector2(0f, openingWidth * aspect);

            // The dimmed copy is the screen background now, so it covers the screen
            // rather than the opening.
            if (backdropRect != null)
                backdropRect.sizeDelta = new Vector2(screenWidth, screenWidth * aspect);

            LayoutSelectionBox();
        }

        private float MapAspect()
        {
            float aspect = SpriteAspect(davaoMapSprite);
            if (aspect > 0f)
                return aspect;

            if (districtArt != null)
            {
                for (int i = 0; i < districtArt.Length; i++)
                {
                    DistrictMapArt art = districtArt[i];
                    if (art == null)
                        continue;

                    aspect = SpriteAspect(art.districtHighlight);
                    if (aspect > 0f) return aspect;

                    aspect = SpriteAspect(art.barangayHighlight);
                    if (aspect > 0f) return aspect;
                }
            }

            // Nothing assigned yet: fall back to the shape the Davao maps are drawn
            // at, so the screen still looks right while the art is being made.
            return 2950f / 2500f;
        }

        // ------------------------------------------------------------------
        // The selection box
        // ------------------------------------------------------------------

        private RectTransform CreateSelectionBox(RectTransform parent)
        {
            var go = new GameObject("SelectionBox", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(DraggableSelectionBox));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(parent.rect.width * areaWidth, parent.rect.height * areaHeight);
            rect.anchoredPosition = new Vector2(parent.rect.width * startX, parent.rect.height * startY);

            var image = go.GetComponent<Image>();
            var outline = go.GetComponent<Outline>();

            Sprite frame = theme?.selectionBoxFrame;
            if (frame != null)
            {
                // The frame art already reads as a border, so the plain outline
                // used by the flat-colour fallback is switched off.
                image.sprite = frame;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                image.raycastTarget = true;
                outline.enabled = false;
            }
            else
            {
                image.color = new Color(1f, 1f, 1f, 0.12f);
                image.raycastTarget = true;

                outline.effectColor = new Color(1f, 1f, 1f, 0.95f);
                outline.effectDistance = new Vector2(3f, -3f);
            }

            selectionImage = image;

            draggableBox = go.GetComponent<DraggableSelectionBox>();
            draggableBox.dragArea = parent;
            draggableBox.boxRect = rect;
            draggableBox.visibleArea = mapWindowRect;

            appliedGrabZoom = -1f;

            return rect;
        }

        /// <summary>
        /// Keeps the box the same slice of the map at any screen size. The size is
        /// a fraction of the picture, not of the screen, so the patch of Davao it
        /// covers - and therefore the coordinates handed to the terrain generator -
        /// does not change when the window does.
        /// </summary>
        private void LayoutSelectionBox()
        {
            if (mapRect == null || selectionRect == null)
                return;

            float width = mapRect.rect.width * areaWidth;
            float height = mapRect.rect.height * areaHeight;
            if (width < 1f || height < 1f)
                return;

            selectionRect.sizeDelta = new Vector2(width, height);
            appliedGrabZoom = -1f;
            UpdateSelectionGrabArea();
        }

        /// <summary>
        /// Sizes the area the selection box can be grabbed by.
        ///
        /// It used to be padded out to at least 140 units with 56 more on every side,
        /// which suited the old full-screen map where the box could not be zoomed.
        /// But the padding lives on the box, and the box lives on the zoomed map, so
        /// zooming in multiplied it - at four times zoom the grab area was around 600
        /// units across, and a drag meant for the map kept picking up the box.
        ///
        /// Now the grab area is the box as drawn. The optional minimum is worked out
        /// in screen units and converted back into the box's own units at the current
        /// zoom, so it stays the same size on screen however far in the player goes,
        /// and stops mattering entirely once the box itself is bigger.
        /// </summary>
        private void UpdateSelectionGrabArea()
        {
            if (selectionImage == null || selectionRect == null || mapRect == null)
                return;

            float zoom = Mathf.Max(0.0001f, mapRect.localScale.x);
            if (Mathf.Approximately(zoom, appliedGrabZoom))
                return;

            appliedGrabZoom = zoom;

            float minLocal = selectionBoxMinGrabSize / zoom;
            float padX = Mathf.Max(0f, (minLocal - selectionRect.rect.width) * 0.5f);
            float padY = Mathf.Max(0f, (minLocal - selectionRect.rect.height) * 0.5f);

            // Negative padding grows the raycast area; zero leaves it exactly the box.
            selectionImage.raycastPadding = new Vector4(-padX, -padY, -padX, -padY);
        }

        private void MoveBoxToNormalizedCenter(Vector2 center)
        {
            if (mapRect == null || selectionRect == null)
                return;

            float mapW = mapRect.rect.width;
            float mapH = mapRect.rect.height;
            float boxW = selectionRect.rect.width;
            float boxH = selectionRect.rect.height;

            float x = Mathf.Clamp(center.x * mapW - boxW * 0.5f, 0f, Mathf.Max(0f, mapW - boxW));
            float y = Mathf.Clamp(center.y * mapH - boxH * 0.5f, 0f, Mathf.Max(0f, mapH - boxH));

            selectionRect.anchorMin = new Vector2(0f, 0f);
            selectionRect.anchorMax = new Vector2(0f, 0f);
            selectionRect.pivot = new Vector2(0f, 0f);
            selectionRect.anchoredPosition = new Vector2(x, y);
        }

        // ------------------------------------------------------------------
        // Buttons
        // ------------------------------------------------------------------

        private void CreateGenerateButton(RectTransform parent)
        {
            var button = CreateThemedButton(
                "GenerateButton", parent, "Generate", theme?.generateButton, new Vector2(300f, 95f));

            PlaceBottomRight(button.GetComponent<RectTransform>());
            button.onClick.AddListener(OnGeneratePressed);
            generateButtonGo = button.gameObject;
        }

        private void CreateConfirmDistrictButton(RectTransform parent)
        {
            // Deliberately the same corner as Generate. The two are never on screen
            // together - one asks for a district, the other for a plot inside it -
            // so the player presses the same place twice to go forward.
            var button = CreateThemedButton(
                "ConfirmDistrictButton", parent, "Enter", theme?.districtNextButton, new Vector2(300f, 95f));

            PlaceBottomRight(button.GetComponent<RectTransform>());
            button.onClick.AddListener(OnConfirmDistrictPressed);
            confirmDistrictButtonGo = button.gameObject;
        }

        private static void PlaceBottomRight(RectTransform rect)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-30f, 30f);
        }

        private void CreateBackButton(RectTransform parent)
        {
            var button = CreateThemedButton(
                "BackButton", parent, "Back", theme?.areaBackButton, new Vector2(260f, 95f));
            var rect = button.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(30f, 30f);

            button.onClick.AddListener(OnBackPressed);
        }

        private void CreateDistrictSelector(RectTransform parent)
        {
            Button prev = CreateThemedButton(
                "PrevDistrictButton", parent, "<<", theme?.prevDistrictButton, new Vector2(200f, 90f));
            var prevRect = prev.GetComponent<RectTransform>();
            prevRect.anchorMin = prevRect.anchorMax = new Vector2(0.5f, 0f);
            prevRect.pivot = new Vector2(0.5f, 0.5f);
            prevRect.anchoredPosition = new Vector2(-380f, 150f);
            prev.onClick.AddListener(OnPrevDistrict);
            prevDistrictButtonGo = prev.gameObject;

            Button next = CreateThemedButton(
                "NextDistrictButton", parent, ">>", theme?.nextDistrictButton, new Vector2(200f, 90f));
            var nextRect = next.GetComponent<RectTransform>();
            nextRect.anchorMin = nextRect.anchorMax = new Vector2(0.5f, 0f);
            nextRect.pivot = new Vector2(0.5f, 0.5f);
            nextRect.anchoredPosition = new Vector2(380f, 150f);
            next.onClick.AddListener(OnNextDistrict);
            nextDistrictButtonGo = next.gameObject;

            // A painted sign per district when the art is present, otherwise the
            // original plain text label. Both are built once any sign exists, so a
            // district whose own sign has not been painted yet still shows its name
            // instead of an empty slot. RefreshDistrictChrome picks which one shows.
            if (theme != null && HasAnyDistrictSign())
            {
                var signGo = new GameObject("DistrictSign", typeof(RectTransform), typeof(Image));
                signGo.transform.SetParent(parent, false);

                var signRect = signGo.GetComponent<RectTransform>();
                signRect.anchorMin = signRect.anchorMax = new Vector2(0.5f, 0f);
                signRect.pivot = new Vector2(0.5f, 0.5f);
                signRect.sizeDelta = new Vector2(520f, 130f);
                signRect.anchoredPosition = new Vector2(0f, 150f);

                districtSignImage = signGo.GetComponent<Image>();
                districtSignImage.preserveAspect = true;
                districtSignImage.raycastTarget = false;
            }

            var labelGo = new GameObject("DistrictLabel", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(parent, false);

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(380f, 80f);
            labelRect.anchoredPosition = new Vector2(0f, 150f);

            districtLabel = labelGo.GetComponent<Text>();
            districtLabel.font = GameFonts.Primary;
            districtLabel.fontSize = 34;
            districtLabel.alignment = TextAnchor.MiddleCenter;
            districtLabel.color = Color.white;
            districtLabel.text = "(District Name)";
        }

        private bool HasAnyDistrictSign()
        {
            if (districtArt == null)
                return false;

            for (int i = 0; i < districtArt.Length; i++)
            {
                DistrictMapArt art = districtArt[i];
                if (art != null && theme.GetDistrictSign(art.districtName) != null)
                    return true;
            }

            return false;
        }

        private void CreateCheckDescriptionButton(RectTransform parent)
        {
            Button button = DescriptionBoard.CreateOpenButton(
                parent, theme, "CHECK DESCRIPTION", new Vector2(320f, 58f),
                OnCheckDescriptionPressed);

            // Under the district sign. The sign is drawn 130 tall centred at
            // y=150, so its painted edge stops around y=85; Back and Generate sit
            // out at the screen corners, which leaves the middle of this strip
            // free. It stays available after the district is locked in, because
            // what it describes - the district - has not changed.
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 52f);
        }

        private void OnCheckDescriptionPressed()
        {
            if (districtBoard == null)
                return;

            string district = CurrentDistrictName();
            string body = DistrictDescriptions.For(district);
            if (string.IsNullOrEmpty(body))
                return;

            districtBoard.Show(district, body);
        }

        // ------------------------------------------------------------------
        // Phases
        // ------------------------------------------------------------------

        private void ApplyPhase(SelectionPhase newPhase)
        {
            phase = newPhase;
            bool choosingDistrict = phase == SelectionPhase.District;

            if (selectionRect != null) selectionRect.gameObject.SetActive(!choosingDistrict);
            if (generateButtonGo != null) generateButtonGo.SetActive(!choosingDistrict);
            if (confirmDistrictButtonGo != null) confirmDistrictButtonGo.SetActive(choosingDistrict);
            if (prevDistrictButtonGo != null) prevDistrictButtonGo.SetActive(choosingDistrict);
            if (nextDistrictButtonGo != null) nextDistrictButtonGo.SetActive(choosingDistrict);

            RefreshMapOverlay();
            RefreshInfoText();
        }

        private void RefreshMapOverlay()
        {
            if (districtOverlayImage == null)
                return;

            DistrictMapArt art = CurrentArt();
            Sprite sprite = art == null
                ? null
                : (phase == SelectionPhase.District ? art.districtHighlight : art.barangayHighlight);

            districtOverlayImage.sprite = sprite;

            // Hidden rather than left as a blank white rectangle over the map when
            // a district's picture has not been drawn yet.
            districtOverlayImage.enabled = sprite != null;

            // The dimmed copy follows whatever the map is showing, so the two never
            // disagree about which district is highlighted.
            if (backdropImage != null)
            {
                backdropImage.sprite = sprite != null ? sprite : davaoMapSprite;
                backdropImage.color = backdropTint;
            }
        }

        private void RefreshInfoText()
        {
            if (infoText == null)
                return;

            string district = DisplayName(CurrentDistrictName());

            // The button's wording is painted into its art, so this names the same
            // word the player is looking at. Change both together if that art is
            // ever replaced.
            infoText.text = phase == SelectionPhase.District
                ? "Choose your district, then press ENTER to pick an area inside it"
                : "Drag the box inside " + district + ", then press Generate";
        }

        private void RefreshDistrictChrome()
        {
            string districtName = CurrentDistrictName();
            Sprite sign = districtSignImage != null ? theme?.GetDistrictSign(districtName) : null;

            if (districtSignImage != null)
            {
                districtSignImage.sprite = sign;
                // Hide the slot entirely if this one district's sign is missing,
                // rather than showing a blank white box. The text label takes its
                // place.
                districtSignImage.enabled = sign != null;
            }

            if (districtLabel != null)
            {
                districtLabel.text = DisplayName(districtName);
                districtLabel.enabled = sign == null;
            }

            if (districtBoard != null && districtBoard.IsOpen)
                OnCheckDescriptionPressed();

            RefreshMapOverlay();
            RefreshInfoText();
        }

        private void OnPrevDistrict()
        {
            SwitchToDistrict(currentDistrictIndex - 1);
        }

        private void OnNextDistrict()
        {
            SwitchToDistrict(currentDistrictIndex + 1);
        }

        private void SwitchToDistrict(int index)
        {
            int count = DistrictCount();
            if (count == 0)
                return;

            currentDistrictIndex = ((index % count) + count) % count;

            RefreshDistrictChrome();
            FocusCurrentDistrict(instant: false);
        }

        private void OnConfirmDistrictPressed()
        {
            if (phase != SelectionPhase.District)
                return;

            ApplyPhase(SelectionPhase.Area);

            // Picking an area starts from the district's own framing. Had the player
            // zoomed in to look around first, the barangay map would open partly
            // hidden, and the box - placed at the middle of the district - could land
            // outside the part they are looking at.
            if (zoomPan != null && zoomPan.IsAwayFromFocus)
                zoomPan.GlideBackToFocus(districtGlideSeconds);

            // The box starts in the middle of the district rather than wherever it
            // was left, so it is always somewhere the player can see it.
            if (TryGetDistrictFrame(CurrentDistrictName(), out _, out Vector2 center))
                MoveBoxToNormalizedCenter(center);
            else
                MoveBoxToNormalizedCenter(new Vector2(startX, startY));

            ShowAreaTutorialIfDue();
        }

        public void OnBackPressed()
        {
            // Back steps out of the area, then out of the screen. Without this the
            // only way to change a district after pressing Next would be to leave
            // for the main menu and come in again.
            if (phase == SelectionPhase.Area)
            {
                ApplyPhase(SelectionPhase.District);
                return;
            }

            SceneManager.LoadScene(backSceneName);
        }

        // ------------------------------------------------------------------
        // Framing
        // ------------------------------------------------------------------

        private bool TryGetDistrictFrame(string districtName, out Rect bounds, out Vector2 center)
        {
            bounds = new Rect(0f, 0f, 1f, 1f);
            center = new Vector2(0.5f, 0.5f);

            if (string.IsNullOrEmpty(districtName))
                return false;

            DistrictMapArt art = CurrentArt();
            if (art == null)
                return false;

            // The district's extent is read off the spot picture: green and red
            // together are exactly the barangays that belong to it, which is a
            // tighter and more honest frame than any hand-entered rectangle.
            return DistrictAreaSpot.TryGetFootprint(art.agriculturalAreaSpot, out bounds, out center);
        }

        private void FocusCurrentDistrict(bool instant)
        {
            if (zoomPan == null || mapRect == null || viewportRect == null || mapWindowRect == null)
                return;

            string districtName = CurrentDistrictName();

            if (!TryGetDistrictFrame(districtName, out Rect bounds, out Vector2 center))
            {
                // No art to aim at: show the whole map rather than guessing.
                bounds = new Rect(0f, 0f, 1f, 1f);
                center = new Vector2(0.5f, 0.5f);
            }

            float fitZoom = ComputeFitZoom(bounds);

            // Nothing covers the opening, so the map simply has to fill it and the
            // district simply has to sit in the middle of it. The offsets that used
            // to dodge the plank and the sign belong to the frame's placement now.
            zoomPan.clampInsetTop = 0f;
            zoomPan.clampInsetBottom = 0f;
            zoomPan.focusViewportOffset = Vector2.zero;

            // The district's own framing becomes the zoom-out limit, so the player
            // can move in closer but never back out past the view this district
            // opened at. Set before focusing, since both focus calls clamp the zoom
            // they are given to this same range.
            zoomPan.minZoom = fitZoom;

            // The camera aims at the middle of the district's extent rather than at
            // its centre of mass. A district shaped like Paquibato - wide at one end,
            // tapering at the other - has a centroid well off the middle of its
            // outline, and aiming there pushes the far end back under the UI even
            // though the whole thing would have fitted. The selection box still
            // starts at the centroid, which is somewhere solidly inside the district
            // rather than possibly in a notch of its bounding box.
            Vector2 framingPoint = bounds.center;

            if (instant || districtGlideSeconds <= 0.02f)
                zoomPan.FocusNormalizedPoint(framingPoint, fitZoom);
            else
                zoomPan.GlideToNormalizedPoint(framingPoint, fitZoom, districtGlideSeconds);

            MoveBoxToNormalizedCenter(center);
        }

        /// <summary>
        /// The zoom to frame a district at, measured against the frame's opening -
        /// the map is only seen through that, so the screen and the UI on it no
        /// longer come into this at all.
        /// </summary>
        private float ComputeFitZoom(Rect bounds)
        {
            float viewWidth = mapWindowRect.rect.width;
            float viewHeight = mapWindowRect.rect.height;
            float contentWidth = mapRect.rect.width;
            float contentHeight = mapRect.rect.height;

            if (viewWidth < 1f || viewHeight < 1f || contentWidth < 1f || contentHeight < 1f)
                return 1f;

            float districtWidth = Mathf.Max(1f, bounds.width * contentWidth);
            float districtHeight = Mathf.Max(1f, bounds.height * contentHeight);

            float fit = Mathf.Min(
                districtFillFraction * viewWidth / districtWidth,
                districtFillFraction * viewHeight / districtHeight);

            // The zoom this district just fits the opening at, with a little to
            // spare so it does not sit flush against the frame.
            float needed = Mathf.Min(
                viewHeight * 0.96f / districtHeight,
                viewWidth * 0.96f / districtWidth);

            // The map fills the opening at zoom 1, so that is normally the floor and
            // no gap can open inside the frame. A district larger than the opening -
            // only possible if the frame is scaled down hard - is allowed to pull
            // back just far enough to be seen whole.
            float floor = needed >= 1f ? 1f : Mathf.Max(minimumDistrictZoom, needed);

            return Mathf.Clamp(fit, floor, maxZoom);
        }

        // ------------------------------------------------------------------
        // Tutorial
        // ------------------------------------------------------------------

        private void BuildTutorial(Transform canvasTransform)
        {
            var layerGo = new GameObject(TutorialRootName, typeof(RectTransform));
            layerGo.transform.SetParent(canvasTransform, false);

            tutorialLayer = layerGo.GetComponent<RectTransform>();
            Stretch(tutorialLayer);

            // Only needed behind the question. The question's plank takes clicks on
            // itself, but ENTER, << and >> are still showing around it and would
            // otherwise work straight through it.
            tutorialPromptBlocker = new GameObject("PromptBlocker", typeof(RectTransform), typeof(Image));
            tutorialPromptBlocker.transform.SetParent(tutorialLayer, false);
            Stretch(tutorialPromptBlocker.GetComponent<RectTransform>());

            Image blocker = tutorialPromptBlocker.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.55f);
            blocker.raycastTarget = true;
            tutorialPromptBlocker.SetActive(false);

            tutorialBoard = TutorialSlideBoard.Create(tutorialLayer, theme);
        }

        /// <summary>
        /// Asks whether to run the tutorial, with the very same prompt the farm uses
        /// when it asks about its own. The farm builds that popup in its scene and
        /// this screen has none, so one is made here - the same class, which is what
        /// keeps the two questions looking identical.
        /// </summary>
        private void OfferTutorial()
        {
            FarmConfirmPopup popup = FarmConfirmPopup.Instance;
            if (popup == null)
                popup = new GameObject("FarmConfirmPopup").AddComponent<FarmConfirmPopup>();

            if (popup == null || tutorialLayer == null)
                return;

            // Layer first, then the popup, so the popup lands above the blocker.
            tutorialLayer.SetAsLastSibling();
            tutorialPromptBlocker.SetActive(true);

            popup.Show(
                "Do you want to have an Area Selection Tutorial? (Recommended for new players)",
                AcceptTutorial,
                null,
                DeclineTutorial);
        }

        private void AcceptTutorial()
        {
            tutorialAccepted = true;

            if (tutorialPromptBlocker != null)
                tutorialPromptBlocker.SetActive(false);

            if (tutorialBoard != null)
            {
                tutorialLayer.SetAsLastSibling();
                tutorialBoard.Show(SlidesOrDefault(districtTutorialSlides, DefaultDistrictTutorialSlides), null);
            }
        }

        private void DeclineTutorial()
        {
            tutorialAccepted = false;

            if (tutorialPromptBlocker != null)
                tutorialPromptBlocker.SetActive(false);
        }

        /// <summary>
        /// The second half of the tutorial, the first time a district is locked in.
        /// Once only per visit, so stepping back out and pressing ENTER again does not
        /// make the player read it twice.
        /// </summary>
        private void ShowAreaTutorialIfDue()
        {
            if (!tutorialAccepted || areaTutorialShown || tutorialBoard == null)
                return;

            areaTutorialShown = true;
            tutorialLayer.SetAsLastSibling();
            tutorialBoard.Show(SlidesOrDefault(areaTutorialSlides, DefaultAreaTutorialSlides), null);
        }

        /// <summary>
        /// The Inspector list, or the built-in pages if someone has emptied it, so
        /// a Yes never opens onto nothing.
        /// </summary>
        private static TutorialSlide[] SlidesOrDefault(TutorialSlide[] slides, System.Func<TutorialSlide[]> fallback)
        {
            return slides != null && slides.Length > 0 ? slides : fallback();
        }

        private static TutorialSlide[] DefaultDistrictTutorialSlides()
        {
            return new[]
            {
                new TutorialSlide(
                    "This is the Davao City Area Selection. This is where you pick an area in " +
                    "Davao City to generate a terrain and get soil information of that area."),
                new TutorialSlide(
                    "First, you have to select a district that you want your farm to be in, click " +
                    "the \">>\" and \"<<\" buttons to switch districts and press \"ENTER\" to select " +
                    "the chosen district"),
                new TutorialSlide(
                    "You can also check the district description to learn more about the district " +
                    "and the possible seeds that you will get in the game.")
            };
        }

        private static TutorialSlide[] DefaultAreaTutorialSlides()
        {
            return new[]
            {
                new TutorialSlide(
                    "There are some parts of the district that are Agricultural Area and some are " +
                    "not Agricultural Area. The indication between is the color, Red is the " +
                    "Non-Agricultural Area and Green is the Agricultural Area, you can only create " +
                    "your farm in the Agricultural Area only!"),
                new TutorialSlide(
                    "You can pinch and zoom in and out of the map, and also drag to move the map to " +
                    "see the area better. Select the wooden box that is pointed by the white arrow " +
                    "and drag the box to select the area you want to generate your farm. This tiny " +
                    "box represents your farm and the game will generate your farm based on that " +
                    "specific area in Davao city Soil and Terrain.")
            };
        }

        // ------------------------------------------------------------------
        // Generate
        // ------------------------------------------------------------------

        public void OnGeneratePressed()
        {
            if (phase != SelectionPhase.Area || draggableBox == null)
                return;

            string districtName = CurrentDistrictName();
            DistrictMapArt art = CurrentArt();

            if (art == null || art.agriculturalAreaSpot == null)
            {
                ShowPopup(
                    "The agricultural area map for " + DisplayName(districtName) +
                    " has not been set up yet, so this district cannot be farmed.");
                return;
            }

            Rect selectedRect = draggableBox.GetNormalizedRect();

            switch (DistrictAreaSpot.Validate(art.agriculturalAreaSpot, selectedRect, areaSampleResolution))
            {
                case AreaSelectionVerdict.OutsideDistrict:
                    ShowPopup(
                        "You selected outside the " + DisplayName(districtName) +
                        " district. Please select an area inside the district.");
                    return;

                case AreaSelectionVerdict.NonAgricultural:
                    ShowPopup(
                        "You selected a Non-Agricultural area of the District, please " +
                        "select another area that is part of the Agricultural Area");
                    return;

                case AreaSelectionVerdict.SpotMapMissing:
                case AreaSelectionVerdict.SpotMapUnreadable:
                    ShowPopup(
                        "The agricultural area map for " + DisplayName(districtName) +
                        " could not be read. Turn on Read/Write Enabled on that picture.");
                    return;
            }

            SelectedAreaState.NormalizedRect = selectedRect;
            SelectedAreaState.SelectedDistrictName = districtName;

            SceneManager.LoadScene(terrainPreviewSceneName);
        }

        // ------------------------------------------------------------------
        // Text, popup and small helpers
        // ------------------------------------------------------------------

        private void CreateInfoText(RectTransform parent)
        {
            Sprite plank = theme?.infoPlank;

            // With art, the hint sits on a wooden plank; without it, the text
            // floats on its own exactly as before.
            Transform textParent = parent;

            if (plank != null)
            {
                var plankGo = new GameObject("InfoPlank", typeof(RectTransform), typeof(Image));
                plankGo.transform.SetParent(parent, false);

                var plankRect = plankGo.GetComponent<RectTransform>();
                plankRect.anchorMin = plankRect.anchorMax = new Vector2(0.5f, 1f);
                plankRect.pivot = new Vector2(0.5f, 1f);
                plankRect.sizeDelta = new Vector2(1150f, 140f);
                plankRect.anchoredPosition = new Vector2(0f, -25f);

                var plankImage = plankGo.GetComponent<Image>();
                plankImage.sprite = plank;
                // Sliced so the notched ends keep their shape while the middle
                // stretches to fit the longest district name.
                plankImage.type = Image.Type.Sliced;
                plankImage.raycastTarget = false;

                textParent = plankGo.transform;
            }

            var go = new GameObject("InfoText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(textParent, false);

            var rect = go.GetComponent<RectTransform>();

            if (plank != null)
            {
                // Fill the plank, inset so the words stay off the wooden edges.
                Stretch(rect);
                rect.offsetMin = new Vector2(70f, 22f);
                rect.offsetMax = new Vector2(-70f, -22f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(1100f, 80f);
                rect.anchoredPosition = new Vector2(0f, -30f);
            }

            infoText = go.GetComponent<Text>();
            infoText.text = "Choose your district, then press ENTER to pick an area inside it";
            infoText.alignment = TextAnchor.MiddleCenter;
            infoText.font = GameFonts.Primary;
            infoText.fontSize = 32;
            infoText.fontStyle = FontStyle.Bold;
            infoText.color = Color.white;
            infoText.raycastTarget = false;
        }

        private void CreatePopup(RectTransform parent)
        {
            popupPanel = new GameObject("PopupPanel", typeof(RectTransform), typeof(Image));
            popupPanel.transform.SetParent(parent, false);

            Sprite board = theme?.areaSelectionPopupBoard;
            Vector2 size = board != null && theme != null
                ? theme.areaSelectionPopupSize
                : new Vector2(950f, 180f);

            var rect = popupPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            var bg = popupPanel.GetComponent<Image>();
            if (board != null)
            {
                bg.sprite = board;
                bg.preserveAspect = true;
                bg.color = Color.white;
            }
            else
            {
                // Unchanged fallback, so the warning still reads before the art exists.
                bg.color = new Color(0f, 0f, 0f, 0.80f);
            }

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(popupPanel.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            Stretch(textRect);

            // Inset off the plank's painted edges when themed; the flat box needs
            // only a small margin.
            float padX = board != null ? size.x * 0.12f : 20f;
            float padY = board != null ? size.y * 0.24f : 20f;
            textRect.offsetMin = new Vector2(padX, padY);
            textRect.offsetMax = new Vector2(-padX, -padY);

            popupText = textGo.GetComponent<Text>();
            popupText.font = GameFonts.Primary;
            popupText.fontSize = 28;
            popupText.fontStyle = FontStyle.Bold;
            popupText.alignment = TextAnchor.MiddleCenter;
            // Dark ink on wood, white on the flat fallback.
            popupText.color = board != null ? Color.black : Color.white;
            popupText.text = "";

            popupPanel.SetActive(false);
        }

        private void ShowPopup(string message)
        {
            if (popupPanel == null || popupText == null)
                return;

            popupText.text = message;
            popupPanel.SetActive(true);

            CancelInvoke(nameof(HidePopup));

            // The non-agricultural warning is twice the length of the old one, and
            // 2.6 seconds is not enough to read it, so longer messages stay up
            // longer instead of every player having to trigger it twice.
            float seconds = Mathf.Clamp(1.6f + message.Length * 0.035f, 2.6f, 6f);
            Invoke(nameof(HidePopup), seconds);
        }

        private void HidePopup()
        {
            if (popupPanel != null)
                popupPanel.SetActive(false);
        }

        private Image CreateImage(string objectName, RectTransform parent, Sprite sprite)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        /// <summary>
        /// A button that uses its painted sprite when one is set (the wording is
        /// in the art, so no Text child is added), and otherwise falls back to
        /// the original dark box with a text label.
        /// </summary>
        private Button CreateThemedButton(
            string objectName, RectTransform parent, string label, Sprite sprite, Vector2 size)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            go.GetComponent<RectTransform>().sizeDelta = size;

            var image = go.GetComponent<Image>();
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.color = Color.white;

                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f, 1f);
                colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
                colors.selectedColor = Color.white;
                button.colors = colors;

                return button;
            }

            image.color = new Color(0f, 0f, 0f, 0.6f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            Stretch(textRect);

            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = GameFonts.Primary;
            text.fontSize = 30;
            text.color = Color.white;

            return button;
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
