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

        private bool hasFocusAnchor;
        private float focusZoom;
        private Vector2 focusPosition;

        private bool gliding;
        private float glideElapsed;
        private float glideDuration;
        private float glideFromZoom;
        private float glideToZoom;
        private Vector2 glideFromPosition;
        private Vector2 glideToPosition;

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
            if (draggingMap && eventData.pointerId != dragPointerId)
                return;

            draggingMap = false;

            if (gliding || pinching || !allowUserPan || viewport == null || content == null)
                return;

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

            if (gliding || pinching)
                return;

            content.anchoredPosition += delta;

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

            hasFocusAnchor = false;
            gliding = false;
        }

        public void FocusNormalizedPoint(Vector2 uv, float zoom)
        {
            if (content == null || viewport == null)
                return;

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

            focusZoom = z;
            focusPosition = content.anchoredPosition;
            hasFocusAnchor = true;
        }

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

            hasFocusAnchor = false;
        }

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
