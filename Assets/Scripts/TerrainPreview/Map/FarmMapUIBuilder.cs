using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class FarmMapUIBuilder : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "TerrainPreview" &&
                Object.FindFirstObjectByType<FarmMapUIBuilder>() == null)
            {
                new GameObject("FarmMapUI").AddComponent<FarmMapUIBuilder>();
            }
        }

        [Header("Zoom")]
        [Tooltip("How many metres of farm the small map shows top to bottom. " +
                 "Lower zooms in closer around the player.")]
        [Min(10f)]
        public float smallMapSpanMetres = 140f;

        [Tooltip("How far the opened map zooms in. 1 is the whole farm, which is " +
                 "also as far out as it goes.")]
        [Min(1f)]
        public float maxZoom = 10f;

        [Header("Refresh")]
        [Tooltip("Seconds between rescans for new crops and placed objects. The " +
                 "markers themselves move every frame; this is only how often the " +
                 "list of things to draw is rebuilt.")]
        [Min(0.1f)]
        public float rescanIntervalSeconds = 1.5f;

        [Header("Dots")]
        [Tooltip("The player and the shipping bin are always dots. The crop and " +
                 "mitigation sizes are only used for anything without a picture.")]
        public float playerDotSize = 11f;
        public float cropDotSize = 9f;
        public float shippingBinDotSize = 11f;
        public float mitigationDotSize = 9f;

        [Header("Icons")]
        [Tooltip("On-screen size of a crop's fruit, with the map zoomed out.")]
        public float cropIconSize = 26f;
        [Tooltip("On-screen size of a placed mitigation item, with the map zoomed out.")]
        public float mitigationIconSize = 30f;
        [Tooltip("The most an icon grows as the opened map zooms in. Icons grow with " +
                 "the square root of the zoom, far less than the distances between " +
                 "them, so zooming in pulls apart crops planted close together.")]
        [Min(1f)]
        public float maxIconGrowth = 2f;

        [Header("Compass")]
        [Tooltip("Diameter of the N, E, S and W balls on the small map.")]
        public float compassBallSize = 40f;

        private static readonly Color PlayerColor = Color.white;
        private static readonly Color CropColor = new Color(0.42f, 0.24f, 0.09f, 1f);
        private static readonly Color ShippingBinColor = new Color(0.96f, 0.89f, 0.71f, 1f);
        private static readonly Color MitigationColor = new Color(0.12f, 0.10f, 0.08f, 1f);

        private static readonly Color CompassBallFill = new Color(0.96f, 0.67f, 0.35f, 1f);
        private static readonly Color CompassBallRim = new Color(0.36f, 0.20f, 0.07f, 1f);
        private static readonly Color CompassLetterColor = new Color(0.14f, 0.08f, 0.03f, 1f);

        private const float CompassBallMargin = 4f;

        private const float CompassPlankWidth = 200f;

        private Canvas canvas;
        private GameObject root;
        private Image blocker;
        private RectTransform frameRect;
        private Button frameButton;
        private RectTransform fieldRect;
        private Image fieldImage;
        private FarmMapFieldInput fieldInput;
        private RectTransform markersRoot;
        private GameObject closeButton;
        private GameObject compassPlanks;
        private GameObject compassBallsRoot;
        private RectTransform[] compassBalls;

        private Sprite dotSprite;
        private readonly List<Image> markerPool = new List<Image>();

        private Terrain terrain;
        private Transform player;
        private bool expanded;

        private float zoom = 1f;

        private Vector2 panMetres;

        public bool IsExpanded => expanded;

        public void Collapse()
        {
            if (expanded)
                SetExpanded(false);
        }
        private float nextRescanTime;

        private readonly List<MapMarker> markers = new List<MapMarker>();

        private struct MapMarker
        {
            public Transform target;
            public Color color;
            public float size;

            public Sprite icon;

            public int order;
        }

        private static readonly System.Comparison<MapMarker> ByOrder =
            (a, b) => a.order.CompareTo(b.order);

        private UIThemeSprites Theme => UIThemeSprites.Instance;

        private Vector2 SmallSize =>
            Theme != null ? Theme.mapSmallSize : new Vector2(400f, 300f);

        private Vector2 ExpandedSize =>
            Theme != null ? Theme.mapExpandedSize : new Vector2(1040f, 780f);

        private Vector2 InsetFraction =>
            Theme != null ? Theme.mapFieldInsetFraction : new Vector2(0.105f, 0.125f);

        private void Start()
        {
            EnsureEventSystem();
            EnsureCanvas();
            Build();
            root.SetActive(false);
            StartCoroutine(WaitForTerrainThenShow());
        }

        private IEnumerator WaitForTerrainThenShow()
        {
            while (terrain == null)
            {
                terrain = ResolveTerrain();
                if (terrain == null)
                    yield return null;
            }

            SetExpanded(false);
            root.SetActive(true);
        }

        private static Terrain ResolveTerrain()
        {
            TemporaryTerrainGenerator generator =
                Object.FindFirstObjectByType<TemporaryTerrainGenerator>();

            if (generator != null && generator.targetTerrain != null)
                return generator.targetTerrain;

            if (Terrain.activeTerrain != null)
                return Terrain.activeTerrain;

            return Object.FindFirstObjectByType<Terrain>();
        }

        private void LateUpdate()
        {
            if (terrain == null || root == null || !root.activeSelf)
                return;

            if (Time.unscaledTime >= nextRescanTime)
            {
                nextRescanTime = Time.unscaledTime + rescanIntervalSeconds;
                RebuildMarkers();
            }

            LayoutMarkers();
        }

        private void RebuildMarkers()
        {
            markers.Clear();

            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                if (crop == null || crop.Transform == null)
                    continue;

                FarmCropType type = crop.CropType;
                markers.Add(IconOrDot(crop.Transform, FarmMapIcons.ForCrop(type),
                    cropIconSize, CropColor, cropDotSize, (int)type));
            }

            foreach (ClimateMitigationWorldObject item in
                     Object.FindObjectsByType<ClimateMitigationWorldObject>(FindObjectsSortMode.None))
            {
                if (item == null)
                    continue;

                markers.Add(IconOrDot(item.transform, FarmMapIcons.ForClimateObject(item.mitigationType),
                    mitigationIconSize, MitigationColor, mitigationDotSize, 100 + (int)item.mitigationType));
            }

            foreach (AreaMitigationTrapInstance trap in
                     Object.FindObjectsByType<AreaMitigationTrapInstance>(FindObjectsSortMode.None))
            {
                if (trap == null)
                    continue;

                markers.Add(IconOrDot(trap.transform, FarmMapIcons.ForAreaTrap(trap.mitigation),
                    mitigationIconSize, MitigationColor, mitigationDotSize, 200 + (int)trap.mitigation));
            }

            foreach (AphidTrapInstance trap in
                     Object.FindObjectsByType<AphidTrapInstance>(FindObjectsSortMode.None))
            {
                if (trap == null)
                    continue;

                markers.Add(IconOrDot(trap.transform, FarmMapIcons.ForAphidTrap(),
                    mitigationIconSize, MitigationColor, mitigationDotSize, 300));
            }

            SeedlingTentInstance tent = Object.FindFirstObjectByType<SeedlingTentInstance>();
            if (tent != null)
            {
                markers.Add(IconOrDot(tent.transform, UIThemeSprites.Instance?.seedlingTentButton,
                    mitigationIconSize * 1.25f, MitigationColor, mitigationDotSize, 400));
            }

            markers.Sort(ByOrder);

            ShippingBinSeller bin = Object.FindFirstObjectByType<ShippingBinSeller>();
            if (bin != null)
            {
                markers.Add(new MapMarker
                {
                    target = bin.transform,
                    color = ShippingBinColor,
                    size = shippingBinDotSize
                });
            }

            if (player == null)
            {
                FirstPersonTerrainController controller =
                    Object.FindFirstObjectByType<FirstPersonTerrainController>();
                if (controller != null)
                    player = controller.transform;
            }

            if (player != null)
            {
                markers.Add(new MapMarker
                {
                    target = player,
                    color = PlayerColor,
                    size = playerDotSize
                });
            }
        }

        private static MapMarker IconOrDot(Transform target, Sprite icon, float iconSize,
            Color dotColor, float dotSize, int order)
        {
            return new MapMarker
            {
                target = target,
                icon = icon,
                size = icon != null ? iconSize : dotSize,
                color = icon != null ? Color.white : dotColor,
                order = order
            };
        }

        private Vector2 FieldSize
        {
            get
            {
                Vector2 frame = expanded ? ExpandedSize : SmallSize;
                Vector2 inset = InsetFraction;
                return new Vector2(
                    Mathf.Max(1f, frame.x * (1f - inset.x * 2f)),
                    Mathf.Max(1f, frame.y * (1f - inset.y * 2f)));
            }
        }

        private float PixelsPerMetre()
        {
            Vector2 field = FieldSize;

            if (!expanded)
                return field.y / Mathf.Max(1f, smallMapSpanMetres);

            Vector3 size = terrain.terrainData.size;
            float span = Mathf.Max(1f, Mathf.Max(size.x, size.z));
            return Mathf.Min(field.x, field.y) / span * zoom;
        }

        private Vector2 FarmCentreXZ()
        {
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            return new Vector2(origin.x + size.x * 0.5f, origin.z + size.z * 0.5f);
        }

        private Vector2 ViewCentreWorldXZ()
        {
            if (!expanded && player != null)
                return new Vector2(player.position.x, player.position.z);

            return FarmCentreXZ() + panMetres;
        }

        private float PlayerHeadingDegrees()
        {
            return player != null ? player.eulerAngles.y : 0f;
        }

        private void LayoutMarkers()
        {
            float pixelsPerMetre = PixelsPerMetre();
            Vector2 centre = ViewCentreWorldXZ();
            Vector2 halfField = FieldSize * 0.5f;

            float heading = expanded ? 0f : PlayerHeadingDegrees() * Mathf.Deg2Rad;
            float cos = Mathf.Cos(heading);
            float sin = Mathf.Sin(heading);

            float growth = expanded ? Mathf.Clamp(Mathf.Sqrt(zoom), 1f, maxIconGrowth) : 1f;

            int used = 0;

            for (int i = 0; i < markers.Count; i++)
            {
                MapMarker marker = markers[i];
                if (marker.target == null)
                    continue;

                Vector3 world = marker.target.position;

                float east = (world.x - centre.x) * pixelsPerMetre;
                float north = (world.z - centre.y) * pixelsPerMetre;

                Vector2 point = new Vector2(
                    east * cos - north * sin,
                    east * sin + north * cos);

                float size = marker.icon != null ? marker.size * growth : marker.size;

                if (Mathf.Abs(point.x) > halfField.x + size ||
                    Mathf.Abs(point.y) > halfField.y + size)
                {
                    continue;
                }

                Image image = GetMarkerImage(used++);
                RectTransform rect = image.rectTransform;
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = point;

                image.sprite = marker.icon != null ? marker.icon : dotSprite;
                image.preserveAspect = marker.icon != null;
                image.color = marker.color;
            }

            for (int i = used; i < markerPool.Count; i++)
            {
                if (markerPool[i].gameObject.activeSelf)
                    markerPool[i].gameObject.SetActive(false);
            }

            if (!expanded)
                LayoutCompassBalls(cos, sin, halfField);
        }

        private Image GetMarkerImage(int index)
        {
            while (markerPool.Count <= index)
            {
                GameObject go = new GameObject("Marker", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(markersRoot, false);

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                Image image = go.GetComponent<Image>();
                image.sprite = dotSprite;
                image.raycastTarget = false;

                markerPool.Add(image);
            }

            Image marker = markerPool[index];
            if (!marker.gameObject.activeSelf)
                marker.gameObject.SetActive(true);

            return marker;
        }

        private void LayoutCompassBalls(float cos, float sin, Vector2 halfField)
        {
            if (compassBalls == null)
                return;

            PlaceCompassBall(compassBalls[0], new Vector2(-sin, cos), halfField);
            PlaceCompassBall(compassBalls[1], new Vector2(cos, sin), halfField);
            PlaceCompassBall(compassBalls[2], new Vector2(sin, -cos), halfField);
            PlaceCompassBall(compassBalls[3], new Vector2(-cos, -sin), halfField);
        }

        private static void PlaceCompassBall(RectTransform ball, Vector2 direction, Vector2 halfField)
        {
            float radius = ball.sizeDelta.x * 0.5f;
            float roomX = Mathf.Max(0f, halfField.x - radius - CompassBallMargin);
            float roomY = Mathf.Max(0f, halfField.y - radius - CompassBallMargin);

            float toSide = Mathf.Abs(direction.x) > 0.0001f ? roomX / Mathf.Abs(direction.x) : float.MaxValue;
            float toTop = Mathf.Abs(direction.y) > 0.0001f ? roomY / Mathf.Abs(direction.y) : float.MaxValue;

            ball.anchoredPosition = direction * Mathf.Min(toSide, toTop);
        }

        public void ZoomAtScreenPoint(Vector2 screenPoint, float factor)
        {
            if (!expanded || terrain == null || factor <= 0f)
                return;

            float next = Mathf.Clamp(zoom * factor, 1f, Mathf.Max(1f, maxZoom));
            if (Mathf.Approximately(next, zoom))
                return;

            if (TryFieldPoint(screenPoint, out Vector2 local))
            {
                Vector2 underPointer = ViewCentreWorldXZ() + local / PixelsPerMetre();
                zoom = next;
                panMetres = underPointer - local / PixelsPerMetre() - FarmCentreXZ();
            }
            else
            {
                zoom = next;
            }

            ClampPan();
        }

        public void PanByScreenDelta(Vector2 screenPosition, Vector2 screenDelta)
        {
            if (!expanded || terrain == null)
                return;

            if (!TryFieldPoint(screenPosition, out Vector2 now) ||
                !TryFieldPoint(screenPosition - screenDelta, out Vector2 before))
            {
                return;
            }

            panMetres -= (now - before) / PixelsPerMetre();
            ClampPan();
        }

        private void ClampPan()
        {
            Vector3 size = terrain.terrainData.size;
            float pixelsPerMetre = PixelsPerMetre();
            Vector2 halfField = FieldSize * 0.5f;

            float roomX = Mathf.Max(0f, size.x * 0.5f - halfField.x / pixelsPerMetre);
            float roomZ = Mathf.Max(0f, size.z * 0.5f - halfField.y / pixelsPerMetre);

            panMetres.x = Mathf.Clamp(panMetres.x, -roomX, roomX);
            panMetres.y = Mathf.Clamp(panMetres.y, -roomZ, roomZ);
        }

        private bool TryFieldPoint(Vector2 screenPoint, out Vector2 local)
        {
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                fieldRect, screenPoint, eventCamera, out local);
        }

        private void SetExpanded(bool value)
        {
            expanded = value;

            zoom = 1f;
            panMetres = Vector2.zero;

            Vector2 size = expanded ? ExpandedSize : SmallSize;
            frameRect.sizeDelta = size;

            if (expanded)
            {
                HudRegistry.CloseOtherPanels(HudPiece.Map);

                frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
                frameRect.pivot = new Vector2(0.5f, 0.5f);
                frameRect.anchoredPosition = Vector2.zero;

                root.transform.SetAsLastSibling();
            }
            else
            {
                frameRect.anchorMin = frameRect.anchorMax = new Vector2(1f, 1f);
                frameRect.pivot = new Vector2(1f, 1f);
                frameRect.anchoredPosition = SmallAnchoredPosition();
            }

            Vector2 inset = InsetFraction;
            Vector2 insetPixels = new Vector2(size.x * inset.x, size.y * inset.y);
            fieldRect.offsetMin = insetPixels;
            fieldRect.offsetMax = -insetPixels;

            blocker.gameObject.SetActive(expanded);
            closeButton.SetActive(expanded);
            compassPlanks.SetActive(expanded);
            compassBallsRoot.SetActive(!expanded);

            fieldImage.raycastTarget = expanded;
            fieldInput.enabled = expanded;

            LayoutMarkers();
        }

        private Vector2 SmallAnchoredPosition()
        {
            float plankHeight = Theme != null ? Theme.moneyPlankSize.y : 150f;

            const float plankTop = 14f;
            const float gap = 12f;

            return new Vector2(-20f, -(plankTop + plankHeight + gap));
        }

        private void Build()
        {
            dotSprite = CreateCircleSprite();

            root = new GameObject("FarmMapRoot", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            HudRegistry.RegisterPiece(HudPiece.Map, root);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            BuildBlocker();
            BuildFrame();
            BuildField();
            BuildCompassBalls();
            BuildCloseButton();
            BuildCompassPlanks();
        }

        private void BuildBlocker()
        {
            GameObject go = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            blocker = go.GetComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.55f);
            blocker.raycastTarget = true;

            go.SetActive(false);
        }

        private void BuildFrame()
        {
            GameObject go = new GameObject("MapFrame", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root.transform, false);

            frameRect = go.GetComponent<RectTransform>();

            Image image = go.GetComponent<Image>();
            Sprite frame = Theme?.mapFrame;

            if (frame != null)
            {
                image.sprite = frame;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.40f, 0.26f, 0.13f, 0.95f);
            }

            frameButton = go.GetComponent<Button>();
            frameButton.targetGraphic = image;

            frameButton.onClick.AddListener(() =>
            {
                if (!expanded)
                    SetExpanded(true);
            });
        }

        private void BuildField()
        {
            GameObject go = new GameObject("MapField", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            go.transform.SetParent(frameRect, false);

            fieldRect = go.GetComponent<RectTransform>();
            fieldRect.anchorMin = Vector2.zero;
            fieldRect.anchorMax = Vector2.one;

            fieldImage = go.GetComponent<Image>();
            fieldImage.color = Theme != null
                ? Theme.mapFieldColor
                : new Color(0.42f, 0.60f, 0.16f, 1f);

            fieldImage.raycastTarget = false;

            fieldInput = go.AddComponent<FarmMapFieldInput>();
            fieldInput.map = this;
            fieldInput.enabled = false;

            GameObject markersGo = new GameObject("Markers", typeof(RectTransform));
            markersGo.transform.SetParent(go.transform, false);

            markersRoot = markersGo.GetComponent<RectTransform>();
            markersRoot.anchorMin = markersRoot.anchorMax = new Vector2(0.5f, 0.5f);
            markersRoot.pivot = new Vector2(0.5f, 0.5f);
            markersRoot.sizeDelta = Vector2.zero;
        }

        private void BuildCompassBalls()
        {
            compassBallsRoot = new GameObject("CompassBalls", typeof(RectTransform));
            compassBallsRoot.transform.SetParent(fieldRect, false);

            RectTransform rootRect = compassBallsRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = Vector2.zero;

            Sprite ball = CreateCompassBallSprite();

            string[] letters = { "N", "E", "S", "W" };
            compassBalls = new RectTransform[letters.Length];

            for (int i = 0; i < letters.Length; i++)
            {
                GameObject go = new GameObject("Compass_" + letters[i], typeof(RectTransform), typeof(Image));
                go.transform.SetParent(rootRect, false);

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(compassBallSize, compassBallSize);

                Image image = go.GetComponent<Image>();
                image.sprite = ball;
                image.raycastTarget = false;

                GameObject textGo = new GameObject("Letter", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(go.transform, false);

                RectTransform textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                Text text = textGo.GetComponent<Text>();
                text.font = GameFonts.Primary;
                text.fontSize = 22;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = CompassLetterColor;
                text.raycastTarget = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.text = letters[i];

                compassBalls[i] = rect;
            }
        }

        private void BuildCompassPlanks()
        {
            compassPlanks = new GameObject("CompassPlanks", typeof(RectTransform));
            compassPlanks.transform.SetParent(frameRect, false);

            RectTransform rootRect = compassPlanks.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Vector2 inset = InsetFraction;
            Vector2 border = new Vector2(ExpandedSize.x * inset.x, ExpandedSize.y * inset.y);

            CreateCompassPlank("North", new Vector2(0.5f, 1f), new Vector2(0f, -border.y * 0.5f));
            CreateCompassPlank("East", new Vector2(1f, 0.5f), new Vector2(-border.x * 0.5f, 0f));
            CreateCompassPlank("South", new Vector2(0.5f, 0f), new Vector2(0f, border.y * 0.5f));
            CreateCompassPlank("West", new Vector2(0f, 0.5f), new Vector2(border.x * 0.5f, 0f));

            compassPlanks.SetActive(false);
        }

        private void CreateCompassPlank(string word, Vector2 anchor, Vector2 position)
        {
            Sprite art = Theme?.tradeRequestBoard;

            Vector2 size = UIPlank.SizeFor(art, CompassPlankWidth, CompassPlankWidth * 0.4f);

            GameObject plank = UIPlank.Create(compassPlanks.transform, "Compass_" + word, art, true,
                size, Vector2.zero, word, 26, out _);

            RectTransform rect = plank.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;

            plank.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildCloseButton()
        {
            Sprite art = Theme?.mapCloseButton;

            closeButton = new GameObject("Button_Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeButton.transform.SetParent(frameRect, false);

            RectTransform rect = closeButton.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(170f, 58f);
            rect.anchoredPosition = new Vector2(-18f, -12f);

            Image image = closeButton.GetComponent<Image>();
            Button button = closeButton.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SetExpanded(false));

            if (art != null)
            {
                image.sprite = art;
                image.preserveAspect = true;
                image.color = Color.white;
                PauseMenuBuilder.ApplySpriteTint(button);
            }
            else
            {
                image.color = new Color(0.55f, 0.30f, 0.12f, 0.95f);

                GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(closeButton.transform, false);

                RectTransform textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                Text text = textGo.GetComponent<Text>();
                text.font = GameFonts.Primary;
                text.fontSize = 24;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.text = "Close";
            }

            closeButton.SetActive(false);
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            const float radius = size * 0.5f;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = Mathf.Clamp01(radius - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateCompassBallSprite()
        {
            const int size = 128;
            const float radius = size * 0.5f;
            const float rim = 9f;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    Color color = Color.Lerp(CompassBallRim, CompassBallFill,
                        Mathf.Clamp01(radius - rim - distance + 0.5f));
                    color.a = Mathf.Clamp01(radius - distance);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(true);

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f));
        }

        private void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private void EnsureCanvas()
        {
            canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f;
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
