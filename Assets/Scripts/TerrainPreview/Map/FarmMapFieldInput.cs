using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AgriDabao3D
{
    /// <summary>
    /// Pinch, scroll wheel and drag on the opened farm map, handed to
    /// FarmMapUIBuilder to turn into zoom and panning.
    ///
    /// It sits on the green field inside the frame and is enabled only while the
    /// map is open. The small corner map is a button that opens it, and has to
    /// stay one.
    ///
    /// Pointer down, up and click are answered here too, and do nothing. Unity
    /// otherwise passes them up to the nearest parent that wants them - the
    /// frame's button - and a held button tints its art, so the whole board would
    /// darken for as long as a finger was dragging the map.
    /// </summary>
    public class FarmMapFieldInput : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IDragHandler, IScrollHandler
    {
        /// <summary>Zoom change for one notch of the wheel.</summary>
        private const float WheelStep = 1.2f;

        public FarmMapUIBuilder map;

        private bool pinching;
        private float lastPinchDistance;
        private Vector2 lastPinchCentre;

        public void OnPointerDown(PointerEventData eventData) { }
        public void OnPointerUp(PointerEventData eventData) { }
        public void OnPointerClick(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            // Two fingers are a pinch, handled in Update from both at once. Each
            // finger also reports a drag of its own, and following those as well
            // would move the map twice. The right mouse button turns the camera,
            // so it is left to do that.
            if (map == null || TouchesDown() >= 2 ||
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            map.PanByScreenDelta(eventData.position, eventData.delta);
        }

        public void OnScroll(PointerEventData eventData)
        {
            float wheel = eventData.scrollDelta.y;
            if (map == null || Mathf.Abs(wheel) < 0.01f)
                return;

            // One step per notch whatever size of delta is reported, which varies
            // between mice, trackpads and platforms.
            map.ZoomAtScreenPoint(eventData.position, wheel > 0f ? WheelStep : 1f / WheelStep);
        }

        private void Update()
        {
            if (map == null || !TryReadTwoTouches(out Vector2 first, out Vector2 second))
            {
                pinching = false;
                return;
            }

            float distance = Vector2.Distance(first, second);
            Vector2 centre = (first + second) * 0.5f;

            // Measured against the previous frame rather than where the fingers
            // started, so the map follows them exactly: spreading them twice as
            // far apart doubles the zoom, and moving both carries the map along.
            if (pinching && lastPinchDistance > 1f)
            {
                map.PanByScreenDelta(centre, centre - lastPinchCentre);
                map.ZoomAtScreenPoint(centre, distance / lastPinchDistance);
            }

            pinching = true;
            lastPinchDistance = distance;
            lastPinchCentre = centre;
        }

        private void OnDisable()
        {
            pinching = false;
        }

        private static bool TryReadTwoTouches(out Vector2 first, out Vector2 second)
        {
            first = Vector2.zero;
            second = Vector2.zero;

            Touchscreen screen = Touchscreen.current;
            if (screen == null)
                return false;

            int found = 0;
            var touches = screen.touches;

            for (int i = 0; i < touches.Count && found < 2; i++)
            {
                if (!touches[i].press.isPressed)
                    continue;

                if (found == 0)
                    first = touches[i].position.ReadValue();
                else
                    second = touches[i].position.ReadValue();

                found++;
            }

            return found == 2;
        }

        private static int TouchesDown()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null)
                return 0;

            int count = 0;
            var touches = screen.touches;

            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].press.isPressed)
                    count++;
            }

            return count;
        }
    }
}
