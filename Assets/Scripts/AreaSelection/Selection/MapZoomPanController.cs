using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AgriDabao3D
{
    public class MapZoomPanController : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("References")]
        public RectTransform viewport;
        public RectTransform content;

        [Header("Zoom")]
        public float minZoom = 1f;
        public float maxZoom = 5f;
        public float mouseWheelZoomSpeed = 0.18f;
        public float pinchZoomSpeed = 0.008f;

        [Header("Player Input")]
        [Tooltip("Let the player drag the map around (mouse and touch). The drag is " +
                 "held inside the view the current district opened at, so it only has " +
                 "room to move once the player has zoomed in. Off = the view only moves " +
                 "when district switching moves it.")]
        public bool allowUserPan = true;

        [Tooltip("Let the player zoom with the mouse wheel or a pinch gesture.")]
        public bool allowUserZoom = true;

        [Tooltip("Shifts where on screen a focus call puts its target, in viewport " +
                 "pixels. Positive Y aims higher. Used to sit a district in the gap " +
                 "between the hint plank and the button row rather than dead centre, " +
                 "since the two are not the same height.")]
        public Vector2 focusViewportOffset = Vector2.zero;

        [Tooltip("Height of opaque UI along the top and bottom of the screen. The map " +
                 "is not required to reach behind those bands, which lets a district " +
                 "at the very edge of the picture still be placed in the clear strip " +
                 "between them. Leave at zero to make the map cover the whole screen.")]
        public float clampInsetTop;
        public float clampInsetBottom;

        private Vector2 lastDragLocalPoint;
        private bool draggingMap;
        private int dragPointerId;

        private float lastPinchDistance;
        private bool pinching;

        // The view a district opened at, captured by FocusNormalizedPoint. Zooming
        // toward a point moves the content as well as scaling it, so without an
        // anchor to measure against, zooming in on one corner and back out on
        // another leaves the map somewhere it never started - the floor returns the
        // right scale but not the right place.
        private bool hasFocusAnchor;
        private float focusZoom;
        private Vector2 focusPosition;

        // A district switch travels to the next framing instead of cutting to it.
        // While that is running the focus anchor is dropped, because the anchor
        // holds the view at the district it was set for and would drag the glide
        // backwards; it is re-armed on the last frame at the district arrived at.
        private bool gliding;
        private float glideElapsed;
        private float glideDuration;
        private float glideFromZoom;
        private float glideToZoom;
        private Vector2 glideFromPosition;
        private Vector2 glideToPosition;

        /// <summary>True while the view is travelling to a new district.</summary>
        public bool IsGliding => gliding;

        private void Awake()
        {
            if (content == null)
                content = transform as RectTransform;
        }

        private void Update()
        {
            if (UpdateGlide())
                return;

            HandleTouchZoomAndPan();
        }

        public void OnScroll(PointerEventData eventData)
        {
            // Input is ignored mid-glide rather than cancelling it: a wheel notch
            // during the travel would fight the tween for the same two values.
            if (gliding || !allowUserZoom || viewport == null || content == null)
                return;

            float wheel = eventData.scrollDelta.y;
            if (Mathf.Abs(wheel) < 0.01f)
                return;

            float zoomFactor = 1f + wheel * mouseWheelZoomSpeed;
            ZoomAtScreenPoint(eventData.position, zoomFactor, eventData.pressEventCamera);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // A second finger joining a drag is the start of a pinch, which Update
            // handles. It must not take the drag over.
            if (draggingMap && eventData.pointerId != dragPointerId)
                return;

            draggingMap = false;

            if (gliding || pinching || !allowUserPan || viewport == null || content == null)
                return;

            // Every button pans, including the left one. It used to be kept for the
            // selection box, but that was never needed: the event system hands a
            // drag to whichever object it started on, and the box is its own drag
            // handler drawn over the map. A drag that starts on the box moves the
            // box; one that starts on bare map moves the map.
            //
            // Measured from where the press began rather than where the drag
            // threshold was crossed, so the map stays under the finger instead of
            // lagging behind it by the threshold distance.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport,
                eventData.pressPosition,
                eventData.pressEventCamera,
                out lastDragLocalPoint))
            {
                return;
            }

            draggingMap = true;
            dragPointerId = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!draggingMap || eventData.pointerId != dragPointerId || viewport == null || content == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                return;
            }

            Vector2 delta = localPoint - lastDragLocalPoint;
            lastDragLocalPoint = localPoint;

            // Tracked but not applied while a pinch or a glide owns the view, so the
            // map does not leap by the whole distance moved when either one ends.
            if (gliding || pinching)
                return;

            content.anchoredPosition += delta;

            // This is what keeps the drag inside the district: the same clamp that
            // stops zooming out past the district's framing also stops a drag from
            // carrying the view past it.
            ClampContentToViewport();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == dragPointerId)
                draggingMap = false;
        }

        private void OnDisable()
        {
            draggingMap = false;
            pinching = false;
        }

        private void HandleTouchZoomAndPan()
        {
            if (gliding || !allowUserZoom || Touchscreen.current == null || viewport == null || content == null)
                return;

            var touches = Touchscreen.current.touches;

            int pressedCount = 0;
            Vector2 p0 = Vector2.zero;
            Vector2 p1 = Vector2.zero;

            for (int i = 0; i < touches.Count; i++)
            {
                if (!touches[i].press.isPressed)
                    continue;

                if (pressedCount == 0)
                    p0 = touches[i].position.ReadValue();
                else if (pressedCount == 1)
                    p1 = touches[i].position.ReadValue();

                pressedCount++;
                if (pressedCount >= 2)
                    break;
            }

            if (pressedCount >= 2)
            {
                float currentDistance = Vector2.Distance(p0, p1);
                Vector2 center = (p0 + p1) * 0.5f;

                if (!pinching)
                {
                    pinching = true;
                    lastPinchDistance = currentDistance;
                    return;
                }

                float delta = currentDistance - lastPinchDistance;
                lastPinchDistance = currentDistance;

                float zoomFactor = 1f + delta * pinchZoomSpeed;
                ZoomAtScreenPoint(center, zoomFactor, null);
                return;
            }

            pinching = false;
        }

        private void ZoomAtScreenPoint(Vector2 screenPoint, float zoomFactor, Camera eventCamera)
        {
            float oldScale = content.localScale.x;
            float newScale = Mathf.Clamp(oldScale * zoomFactor, minZoom, maxZoom);

            if (Mathf.Approximately(oldScale, newScale))
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                screenPoint,
                eventCamera,
                out Vector2 localPointBefore
            );

            content.localScale = new Vector3(newScale, newScale, 1f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                screenPoint,
                eventCamera,
                out Vector2 localPointAfter
            );

            Vector2 localDelta = localPointAfter - localPointBefore;
            content.anchoredPosition += localDelta * newScale;

            ClampContentToViewport();
        }

        private void ClampContentToViewport()
        {
            if (viewport == null || content == null)
                return;

            float scale = content.localScale.x;

            float viewportWidth = viewport.rect.width;
            float viewportHeight = viewport.rect.height;

            float contentWidth = content.rect.width * scale;
            float contentHeight = content.rect.height * scale;

            if (viewportWidth < 1f || viewportHeight < 1f || contentWidth < 1f || contentHeight < 1f)
                return;

            Vector2 pos = content.anchoredPosition;

            if (contentWidth <= viewportWidth)
            {
                pos.x = 0f;
            }
            else
            {
                float maxX = (contentWidth - viewportWidth) * 0.5f;
                pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            }

            // The map has to cover the strip the player can actually see the map
            // through, not the whole screen. With both insets at zero that is the
            // whole screen and this behaves exactly as it did before.
            float requiredHeight = Mathf.Max(120f, viewportHeight - clampInsetTop - clampInsetBottom);
            float requiredCentreY = (clampInsetBottom - clampInsetTop) * 0.5f;

            if (contentHeight <= requiredHeight)
            {
                pos.y = requiredCentreY;
            }
            else
            {
                float maxY = (contentHeight - requiredHeight) * 0.5f;
                pos.y = Mathf.Clamp(pos.y, requiredCentreY - maxY, requiredCentreY + maxY);
            }

            // Hold the view inside the region the district opened showing.
            //
            // At the focus zoom the viewport framed a rectangle of the map; at any
            // higher zoom it frames a smaller one, and the rule here is simply that
            // the smaller rectangle must stay inside the original. Writing the
            // viewport centre in content-local units as c = -pos / scale, that reads
            //
            //     |c - c0|  <=  (V / 2) * (1 / focusZoom - 1 / scale)
            //
            // which rearranges into the position clamp below with k = scale/focusZoom.
            // The allowance is exactly zero at k = 1, so the map is not merely
            // nudged back toward the district framing as it zooms out - it arrives
            // at precisely the position it started from, from any path in or out.
            // Above the floor the allowance opens up smoothly, so zooming in still
            // travels toward whatever the player pointed at.
            if (hasFocusAnchor && focusZoom > 0.0001f)
            {
                float k = scale / focusZoom;

                if (k <= 1f)
                {
                    pos = focusPosition;
                }
                else
                {
                    Vector2 anchor = focusPosition * k;
                    float allowedX = viewportWidth * 0.5f * (k - 1f);
                    float allowedY = viewportHeight * 0.5f * (k - 1f);

                    pos.x = Mathf.Clamp(pos.x, anchor.x - allowedX, anchor.x + allowedX);
                    pos.y = Mathf.Clamp(pos.y, anchor.y - allowedY, anchor.y + allowedY);
                }
            }

            content.anchoredPosition = pos;
        }

        public void ResetZoom()
        {
            if (content == null)
                return;

            content.localScale = Vector3.one;
            content.anchoredPosition = Vector2.zero;

            // No district is framed any more, so nothing to hold the view to, and
            // a glide still in flight would immediately undo this.
            hasFocusAnchor = false;
            gliding = false;
        }

        /// <summary>
        /// Zooms to <paramref name="zoom"/> and pans so the given normalized content
        /// point (0..1, origin bottom-left) sits at the centre of the viewport.
        /// The result is clamped to the viewport just like interactive panning.
        /// </summary>
        public void FocusNormalizedPoint(Vector2 uv, float zoom)
        {
            if (content == null || viewport == null)
                return;

            // Dropped before positioning: the previous district's anchor would
            // otherwise clamp this one's framing back toward the old view.
            hasFocusAnchor = false;

            float z = Mathf.Clamp(zoom, minZoom, maxZoom);
            content.localScale = new Vector3(z, z, 1f);

            float w = content.rect.width;
            float h = content.rect.height;
            if (w < 1f) w = viewport.rect.width;
            if (h < 1f) h = viewport.rect.height;
            if (w < 1f || h < 1f)
                return;

            Vector2 local = new Vector2((uv.x - 0.5f) * w, (uv.y - 0.5f) * h);
            content.anchoredPosition = -z * local + focusViewportOffset;

            ClampContentToViewport();

            // Recorded after the clamp, not before. A district near the edge of the
            // map cannot actually be centred - the clamp slides it back so the map
            // still covers the viewport - and the anchor has to be the view the
            // player really sees, or zooming out would return them to a framing that
            // was never on screen.
            focusZoom = z;
            focusPosition = content.anchoredPosition;
            hasFocusAnchor = true;
        }

        /// <summary>
        /// The same framing as <see cref="FocusNormalizedPoint"/>, but travelled to
        /// over <paramref name="seconds"/> instead of jumped to, so stepping through
        /// districts reads as the map moving rather than as a cut.
        /// </summary>
        public void GlideToNormalizedPoint(Vector2 uv, float zoom, float seconds)
        {
            if (content == null || viewport == null)
                return;

            if (seconds <= 0.02f)
            {
                FocusNormalizedPoint(uv, zoom);
                return;
            }

            float fromZoom = content.localScale.x;
            Vector2 fromPosition = content.anchoredPosition;

            // The destination is worked out by actually going there and reading the
            // result back, because where the map ends up is decided by the viewport
            // clamp - a district near an edge cannot be centred - and that clamp
            // only ever runs against the live transform.
            FocusNormalizedPoint(uv, zoom);
            glideToZoom = focusZoom;
            glideToPosition = focusPosition;

            content.localScale = new Vector3(fromZoom, fromZoom, 1f);
            content.anchoredPosition = fromPosition;

            glideFromZoom = fromZoom;
            glideFromPosition = fromPosition;
            glideElapsed = 0f;
            glideDuration = seconds;
            gliding = true;
            hasFocusAnchor = false;
        }

        /// <summary>
        /// True when the player has zoomed in or dragged away from the view the
        /// current district opened at.
        /// </summary>
        public bool IsAwayFromFocus
        {
            get
            {
                if (!hasFocusAnchor || content == null)
                    return false;

                return content.localScale.x > focusZoom * 1.0005f ||
                       (content.anchoredPosition - focusPosition).sqrMagnitude > 0.25f;
            }
        }

        /// <summary>
        /// Travels back to the view the current district opened at, from wherever
        /// the player has zoomed or dragged to.
        /// </summary>
        public void GlideBackToFocus(float seconds)
        {
            if (!hasFocusAnchor || content == null)
                return;

            if (seconds <= 0.02f)
            {
                content.localScale = new Vector3(focusZoom, focusZoom, 1f);
                content.anchoredPosition = focusPosition;
                return;
            }

            glideFromZoom = content.localScale.x;
            glideFromPosition = content.anchoredPosition;
            glideToZoom = focusZoom;
            glideToPosition = focusPosition;
            glideElapsed = 0f;
            glideDuration = seconds;
            gliding = true;

            // Re-armed on the glide's last frame, at these same values.
            hasFocusAnchor = false;
        }

        /// <summary>Stops a glide where it stands. Nothing happens if none is running.</summary>
        public void CancelGlide()
        {
            gliding = false;
        }

        private bool UpdateGlide()
        {
            if (!gliding)
                return false;

            if (content == null)
            {
                gliding = false;
                return false;
            }

            glideElapsed += Time.unscaledDeltaTime;
            float t = glideDuration <= 0f ? 1f : Mathf.Clamp01(glideElapsed / glideDuration);

            // Smoothstep, so the map pulls away and settles instead of starting and
            // stopping at full speed.
            float eased = t * t * (3f - 2f * t);

            float zoom = Mathf.Lerp(glideFromZoom, glideToZoom, eased);
            content.localScale = new Vector3(zoom, zoom, 1f);
            content.anchoredPosition = Vector2.Lerp(glideFromPosition, glideToPosition, eased);

            if (t < 1f)
                return true;

            gliding = false;
            content.localScale = new Vector3(glideToZoom, glideToZoom, 1f);
            content.anchoredPosition = glideToPosition;

            focusZoom = glideToZoom;
            focusPosition = glideToPosition;
            hasFocusAnchor = true;
            return true;
        }
    }
}
