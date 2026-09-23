using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// Top-down farm map on a wooden board.
    ///
    /// The farm is 700m across, so a player who planted a durian on the far side
    /// has no way to find it again. This shows where everything is: the player,
    /// every crop, the shipping bin, and every placed mitigation object. Crops are
    /// drawn as their fruit and placed items as their kit, so a durian reads
    /// differently from a coconut and a greenhouse from a canal; the player and the
    /// shipping bin stay plain dots.
    ///
    /// Two forms, one panel. Small, it lives under the money plank, zoomed in on
    /// the player and turning with them, so the way they face is always up and the
    /// N, E, S and W balls round its edge show which way that is. Tapped, it grows
    /// to the middle of the screen, north-up with the four directions on planks
    /// round the frame, and fits the whole farm. Pinching or scrolling zooms it,
    /// dragging moves it, and zooming all the way back out returns it to exactly
    /// that whole-farm view. Closing it returns it to the corner.
    ///
    /// NOTHING HERE IS SAVED, and nothing needs to be. Every marker is read live
    /// from the scene, and the farm save already restores all of it - crops, the
    /// shipping bin's exact transform, mitigation objects and traps. Load the same
    /// farm on another phone and the map redraws itself identically, because the
    /// world it is reading has itself been restored.
    ///
    /// Built entirely in code like the rest of the game's UI, and it creates
    /// itself in TerrainPreview, so there is no scene wiring and no prefab. It uses
    /// the sprites in UITheme and the item pictures already on the inventory; with
    /// none assigned it still works, drawn in plain colours.
    /// </summary>
    public class FarmMapUIBuilder : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // -= then += so we never double-subscribe if the domain isn't reloaded.
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

        /// <summary>Gap kept between a compass ball and the edge of the green.</summary>
        private const float CompassBallMargin = 4f;

        /// <summary>Width of the direction planks round the opened map.</summary>
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

        /// <summary>Zoom of the opened map. 1 fits the whole farm.</summary>
        private float zoom = 1f;

        /// <summary>How far the opened map's view has been dragged from the middle of the farm, in metres.</summary>
        private Vector2 panMetres;

        /// <summary>True while the map is open full-size. Read by the beginner guide.</summary>
        public bool IsExpanded => expanded;

        /// <summary>Folds the map back to its corner size. Used when another panel opens.</summary>
        public void Collapse()
        {
            if (expanded)
                SetExpanded(false);
        }
        private float nextRescanTime;

        /// <summary>Everything drawn on the map, rebuilt on the rescan tick.</summary>
        private readonly List<MapMarker> markers = new List<MapMarker>();

        private struct MapMarker
        {
            public Transform target;
            public Color color;
            public float size;

            /// <summary>The picture drawn in place of a dot, or null for a plain dot.</summary>
            public Sprite icon;

            /// <summary>Sort key that keeps identical pictures together.</summary>
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

        /// <summary>Mirrors MapBoundarySystem so both agree on which terrain is the farm.</summary>
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

        // ---------------------------------------------------------------- data

        /// <summary>
        /// Rescans the scene for everything the map draws.
        ///
        /// Safe against distance culling: DistanceCullable only toggles renderers,
        /// leaving the GameObjects active, so crops far from the player are still
        /// returned here and still appear on the map.
        /// </summary>
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

            // The six placeable climate prefabs all share this component. Mulch,
            // trellises, stakes and compost are crop-attached or consumed, carry no
            // world object of their own, and so correctly never appear here.
            foreach (ClimateMitigationWorldObject item in
                     Object.FindObjectsByType<ClimateMitigationWorldObject>(FindObjectsSortMode.None))
            {
                if (item == null)
                    continue;

                markers.Add(IconOrDot(item.transform, FarmMapIcons.ForClimateObject(item.mitigationType),
                    mitigationIconSize, MitigationColor, mitigationDotSize, 100 + (int)item.mitigationType));
            }

            // Termite bait stations and pheromone traps: also standalone prefabs
            // the player drops and then has to find again.
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

            // Grouped by kind. Every kind of picture is its own texture, and where
            // two different pictures overlap the canvas has to split its batch
            // between them; in scene order the kinds interleave, so a dense planting
            // could cost a draw call per crop.
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

            // Added last so it is the last sibling, and so draws over any crop or
            // object the player happens to be standing on.
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

        /// <summary>The picture when there is one, and the old coloured dot when there is not.</summary>
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

        // -------------------------------------------------------------- drawing

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

        /// <summary>
        /// UI pixels per world metre. Uniform on both axes so the farm is never
        /// stretched: expanded fits the whole terrain inside the shorter side of
        /// the field and multiplies by the zoom, small works back from how many
        /// metres it should show.
        /// </summary>
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

        /// <summary>
        /// The world point the middle of the field represents: the player on the
        /// small map, and on the opened one the middle of the farm, moved by however
        /// far the player has dragged it.
        /// </summary>
        private Vector2 ViewCentreWorldXZ()
        {
            if (!expanded && player != null)
                return new Vector2(player.position.x, player.position.z);

            return FarmCentreXZ() + panMetres;
        }

        /// <summary>
        /// Which way the player is looking, in degrees clockwise from north. The
        /// camera only ever pitches underneath the player, so the player's own turn
        /// is the camera's heading.
        /// </summary>
        private float PlayerHeadingDegrees()
        {
            return player != null ? player.eulerAngles.y : 0f;
        }

        private void LayoutMarkers()
        {
            float pixelsPerMetre = PixelsPerMetre();
            Vector2 centre = ViewCentreWorldXZ();
            Vector2 halfField = FieldSize * 0.5f;

            // The small map turns so the way the player faces is up. The opened map
            // stays north-up, which is what the planks round its frame say.
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

                // Terrain +Z is north and UI +Y is up, so Z maps straight onto Y
                // and, unturned, the map reads north-up.
                float east = (world.x - centre.x) * pixelsPerMetre;
                float north = (world.z - centre.y) * pixelsPerMetre;

                // Turned anticlockwise by the heading, which lands anything lying
                // straight ahead of the player straight up the map.
                Vector2 point = new Vector2(
                    east * cos - north * sin,
                    east * sin + north * cos);

                float size = marker.icon != null ? marker.size * growth : marker.size;

                // RectMask2D clips anything straying over the edge, but skipping
                // markers that are far outside keeps the pool small when zoomed in.
                if (Mathf.Abs(point.x) > halfField.x + size ||
                    Mathf.Abs(point.y) > halfField.y + size)
                {
                    continue;
                }

                Image image = GetMarkerImage(used++);
                RectTransform rect = image.rectTransform;
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = point;

                // A picture keeps its own shape inside the square; a dot is the
                // square. Icons are never turned with the small map, so the fruit
                // always stands upright.
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

        /// <summary>
        /// Puts each ball where its direction meets the edge of the green.
        ///
        /// North, east, south and west are turned by the same rotation as the
        /// markers, and each ball travels out from the middle that way until it
        /// would touch the edge. As the player turns, the balls run round the
        /// inside of the field and never onto the wood.
        /// </summary>
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

        // --------------------------------------------------------- zoom and pan

        /// <summary>
        /// Zooms the opened map by <paramref name="factor"/>, keeping the spot
        /// under <paramref name="screenPoint"/> - the cursor, or the middle of a
        /// pinch - where it is.
        /// </summary>
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

        /// <summary>Moves the opened map by a finger or mouse movement given in screen pixels.</summary>
        public void PanByScreenDelta(Vector2 screenPosition, Vector2 screenDelta)
        {
            if (!expanded || terrain == null)
                return;

            if (!TryFieldPoint(screenPosition, out Vector2 now) ||
                !TryFieldPoint(screenPosition - screenDelta, out Vector2 before))
            {
                return;
            }

            // The farm moves with the finger, so the middle of the view moves the
            // other way.
            panMetres -= (now - before) / PixelsPerMetre();
            ClampPan();
        }

        /// <summary>
        /// Keeps the view on the farm: the edge of what is on screen may travel as
        /// far as the edge of the terrain and no further.
        ///
        /// At a zoom of 1 the whole farm already fits, so there is no room at all
        /// and the view is pinned to the middle. That is what brings a player who
        /// zooms back out to exactly the view the map opened on, from wherever they
        /// had dragged it: the room shrinks as they zoom out and reaches nothing
        /// just as they arrive.
        /// </summary>
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

        /// <summary>A screen point as a position relative to the middle of the field, in the units the markers are laid out in.</summary>
        private bool TryFieldPoint(Vector2 screenPoint, out Vector2 local)
        {
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                fieldRect, screenPoint, eventCamera, out local);
        }

        // ---------------------------------------------------------------- modes

        private void SetExpanded(bool value)
        {
            expanded = value;

            // The opened map always starts on the whole farm, however it was left.
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

                // Above whatever else is on screen while it is open.
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

            // Only the opened map takes drags and pinches. Small, the field lets
            // taps fall through to the frame, which is the button that opens it.
            fieldImage.raycastTarget = expanded;
            fieldInput.enabled = expanded;

            // The zoom changed, so redraw before the next frame renders.
            LayoutMarkers();
        }

        /// <summary>
        /// Tucked under the money plank, using the plank's own themed height so
        /// the two never overlap if that art is resized.
        /// </summary>
        private Vector2 SmallAnchoredPosition()
        {
            float plankHeight = Theme != null ? Theme.moneyPlankSize.y : 150f;

            // The plank sits at (-20, -14) with a top-right pivot.
            const float plankTop = 14f;
            const float gap = 12f;

            return new Vector2(-20f, -(plankTop + plankHeight + gap));
        }

        // -------------------------------------------------------------- building

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

        /// <summary>Dims the game and swallows taps meant for the map, not the joystick.</summary>
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

            // Guarded rather than made non-interactable while open: Button's
            // disabled state tints its target graphic to a half-transparent grey,
            // which would wash the whole board out the moment it was opened.
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

            // RectMask2D rather than Mask: it clips by rectangle and needs no
            // graphic of its own, so the field colour can be anything - including
            // fully transparent, if the frame art already paints its own field.
            // A stencil Mask would clip every marker away at alpha 0.
            //
            // Not a raycast target until the map is opened; SetExpanded switches
            // it on so the field can take drags.
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

        /// <summary>
        /// The N, E, S and W balls on the small map. Inside the field and after the
        /// markers, so they draw over a crop at the edge and are clipped to the
        /// green like everything else in it.
        /// </summary>
        private void BuildCompassBalls()
        {
            compassBallsRoot = new GameObject("CompassBalls", typeof(RectTransform));
            compassBallsRoot.transform.SetParent(fieldRect, false);

            RectTransform rootRect = compassBallsRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = Vector2.zero;

            Sprite ball = CreateCompassBallSprite();

            // In the order LayoutCompassBalls places them.
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
                // Overflow both ways. This font's line box is taller than the ball
                // at the largest text setting, and Truncate would then draw nothing.
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.text = letters[i];

                compassBalls[i] = rect;
            }
        }

        /// <summary>
        /// North, East, South and West on planks at the middle of each side of the
        /// opened map's frame. The opened map is always north-up, so they never
        /// move.
        /// </summary>
        private void BuildCompassPlanks()
        {
            compassPlanks = new GameObject("CompassPlanks", typeof(RectTransform));
            compassPlanks.transform.SetParent(frameRect, false);

            RectTransform rootRect = compassPlanks.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Centred on the wood of the frame, half the border's thickness in from
            // each edge, which is only ever drawn at the opened size.
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

            // The board art's own proportions, so the wood is never squashed.
            // TradeRequestBoard has no 9-slice border, so "sliced" simply stretches
            // it to this box.
            Vector2 size = UIPlank.SizeFor(art, CompassPlankWidth, CompassPlankWidth * 0.4f);

            GameObject plank = UIPlank.Create(compassPlanks.transform, "Compass_" + word, art, true,
                size, Vector2.zero, word, 26, out _);

            RectTransform rect = plank.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;

            // The side planks reach over the edge of the field, and a drag that
            // starts on one should still move the map.
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
                // The word is painted into the art, so no Text child is added.
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

        /// <summary>
        /// One soft-edged white circle, tinted per dot. A single shared sprite lets
        /// every dot batch into one draw call however many are on screen.
        /// </summary>
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

                    // One pixel of feather, so the dot keeps a clean edge when it
                    // is drawn far smaller than this texture.
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

        /// <summary>
        /// An orange ball with a dark rim, behind the small map's direction letters.
        /// Mipmapped, unlike the dot: the rim is a few pixels wide once drawn, and
        /// without smaller copies to sample from it would break up into specks.
        /// </summary>
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
                scaler.matchWidthOrHeight = 1f; // landscape: scale by height
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
