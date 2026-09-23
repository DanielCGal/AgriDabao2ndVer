using UnityEngine;
using UnityEngine.EventSystems;

namespace AgriDabao3D
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private RectTransform root;
        private RectTransform handle;
        private MobileHudBuilder hud;
        private float radius;

        public void Setup(RectTransform rootRect, RectTransform handleRect, MobileHudBuilder hudBuilder)
        {
            root = rootRect;
            handle = handleRect;
            hud = hudBuilder;
            radius = root.sizeDelta.x * 0.35f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, eventData.pressEventCamera, out var local))
                return;

            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            handle.anchoredPosition = clamped;

            Vector2 normalized = clamped / radius;
            hud.SetMoveInput(normalized);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            handle.anchoredPosition = Vector2.zero;
            hud.SetMoveInput(Vector2.zero);
        }
    }
}
