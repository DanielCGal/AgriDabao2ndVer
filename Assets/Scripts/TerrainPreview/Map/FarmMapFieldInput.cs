using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AgriDabao3D
{
    public class FarmMapFieldInput : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IDragHandler, IScrollHandler
    {
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
