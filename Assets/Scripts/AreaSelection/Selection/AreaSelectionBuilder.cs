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

            districtBoard = DescriptionBoard.Create(viewportRect, theme);

            lastScreenSize = new Vector2(Screen.width, Screen.height);
            appliedGrabZoom = -1f;

            currentDistrictIndex = 0;
            phase = SelectionPhase.District;
            ApplyPhase(SelectionPhase.District);
            RefreshDistrictChrome();

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

        private IEnumerator LayoutThenFrameFirstDistrict()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutMap();

            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutMap();

            FocusCurrentDistrict(instant: true);

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

        private static DistrictMapArt[] BuildPlaceholderArt()
        {
            string[] order = DavaoDistrictService.ProductiveDistrictOrder;
            var rows = new DistrictMapArt[order.Length];

            for (int i = 0; i < order.Length; i++)
                rows[i] = new DistrictMapArt { districtName = order[i] };

            return rows;
        }

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

        private Canvas EnsureCanvas()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f;

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
            canvasScaler.matchWidthOrHeight = 1f;

            return canvas;
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

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

            mapFrameImage.preserveAspect = false;
            mapFrameImage.raycastTarget = false;
        }

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

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, parent.rect.height);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            map.preserveAspect = false;
            map.raycastTarget = true;

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

            zoomPan.allowUserPan = allowMapDragging;
            zoomPan.allowUserZoom = true;

            return rect;
        }

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

            float clearHeight = Mathf.Max(160f, screenHeight - uiTopMargin - uiBottomMargin);
            float clearCentreY = (uiBottomMargin - uiTopMargin) * 0.5f;

            float openingHeight = clearHeight * mapFrameScale;
            float openingWidth = openingHeight / aspect;

            Vector2 inset = FrameInset();
            float frameWidth = openingWidth / Mathf.Max(0.1f, 1f - inset.x * 2f);
            float frameHeight = openingHeight / Mathf.Max(0.1f, 1f - inset.y * 2f);

            if (frameGroupRect != null)
            {
                frameGroupRect.sizeDelta = new Vector2(frameWidth, frameHeight);
                frameGroupRect.anchoredPosition = new Vector2(0f, clearCentreY);
            }

            mapWindowRect.sizeDelta = new Vector2(openingWidth, openingHeight);

            mapRect.sizeDelta = new Vector2(0f, openingWidth * aspect);

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

            return 2950f / 2500f;
        }

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

            districtOverlayImage.enabled = sprite != null;

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

            if (zoomPan != null && zoomPan.IsAwayFromFocus)
                zoomPan.GlideBackToFocus(districtGlideSeconds);

            if (TryGetDistrictFrame(CurrentDistrictName(), out _, out Vector2 center))
                MoveBoxToNormalizedCenter(center);
            else
                MoveBoxToNormalizedCenter(new Vector2(startX, startY));

            ShowAreaTutorialIfDue();
        }

        public void OnBackPressed()
        {
            if (phase == SelectionPhase.Area)
            {
                ApplyPhase(SelectionPhase.District);
                return;
            }

            SceneManager.LoadScene(backSceneName);
        }

        private bool TryGetDistrictFrame(string districtName, out Rect bounds, out Vector2 center)
        {
            bounds = new Rect(0f, 0f, 1f, 1f);
            center = new Vector2(0.5f, 0.5f);

            if (string.IsNullOrEmpty(districtName))
                return false;

            DistrictMapArt art = CurrentArt();
            if (art == null)
                return false;

            return DistrictAreaSpot.TryGetFootprint(art.agriculturalAreaSpot, out bounds, out center);
        }

        private void FocusCurrentDistrict(bool instant)
        {
            if (zoomPan == null || mapRect == null || viewportRect == null || mapWindowRect == null)
                return;

            string districtName = CurrentDistrictName();

            if (!TryGetDistrictFrame(districtName, out Rect bounds, out Vector2 center))
            {
                bounds = new Rect(0f, 0f, 1f, 1f);
                center = new Vector2(0.5f, 0.5f);
            }

            float fitZoom = ComputeFitZoom(bounds);

            zoomPan.clampInsetTop = 0f;
            zoomPan.clampInsetBottom = 0f;
            zoomPan.focusViewportOffset = Vector2.zero;

            zoomPan.minZoom = fitZoom;

            Vector2 framingPoint = bounds.center;

            if (instant || districtGlideSeconds <= 0.02f)
                zoomPan.FocusNormalizedPoint(framingPoint, fitZoom);
            else
                zoomPan.GlideToNormalizedPoint(framingPoint, fitZoom, districtGlideSeconds);

            MoveBoxToNormalizedCenter(center);
        }

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

            float needed = Mathf.Min(
                viewHeight * 0.96f / districtHeight,
                viewWidth * 0.96f / districtWidth);

            float floor = needed >= 1f ? 1f : Mathf.Max(minimumDistrictZoom, needed);

            return Mathf.Clamp(fit, floor, maxZoom);
        }

        private void BuildTutorial(Transform canvasTransform)
        {
            var layerGo = new GameObject(TutorialRootName, typeof(RectTransform));
            layerGo.transform.SetParent(canvasTransform, false);

            tutorialLayer = layerGo.GetComponent<RectTransform>();
            Stretch(tutorialLayer);

            tutorialPromptBlocker = new GameObject("PromptBlocker", typeof(RectTransform), typeof(Image));
            tutorialPromptBlocker.transform.SetParent(tutorialLayer, false);
            Stretch(tutorialPromptBlocker.GetComponent<RectTransform>());

            Image blocker = tutorialPromptBlocker.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.55f);
            blocker.raycastTarget = true;
            tutorialPromptBlocker.SetActive(false);

            tutorialBoard = TutorialSlideBoard.Create(tutorialLayer, theme);
        }

        private void OfferTutorial()
        {
            FarmConfirmPopup popup = FarmConfirmPopup.Instance;
            if (popup == null)
                popup = new GameObject("FarmConfirmPopup").AddComponent<FarmConfirmPopup>();

            if (popup == null || tutorialLayer == null)
                return;

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

        private void ShowAreaTutorialIfDue()
        {
            if (!tutorialAccepted || areaTutorialShown || tutorialBoard == null)
                return;

            areaTutorialShown = true;
            tutorialLayer.SetAsLastSibling();
            tutorialBoard.Show(SlidesOrDefault(areaTutorialSlides, DefaultAreaTutorialSlides), null);
        }

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
                    "and the crops you can plant there.")
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

        private void CreateInfoText(RectTransform parent)
        {
            Sprite plank = theme?.infoPlank;

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
                plankImage.type = Image.Type.Sliced;
                plankImage.raycastTarget = false;

                textParent = plankGo.transform;
            }

            var go = new GameObject("InfoText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(textParent, false);

            var rect = go.GetComponent<RectTransform>();

            if (plank != null)
            {
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
                bg.color = new Color(0f, 0f, 0f, 0.80f);
            }

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(popupPanel.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            Stretch(textRect);

            float padX = board != null ? size.x * 0.12f : 20f;
            float padY = board != null ? size.y * 0.24f : 20f;
            textRect.offsetMin = new Vector2(padX, padY);
            textRect.offsetMax = new Vector2(-padX, -padY);

            popupText = textGo.GetComponent<Text>();
            popupText.font = GameFonts.Primary;
            popupText.fontSize = 28;
            popupText.fontStyle = FontStyle.Bold;
            popupText.alignment = TextAnchor.MiddleCenter;
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
