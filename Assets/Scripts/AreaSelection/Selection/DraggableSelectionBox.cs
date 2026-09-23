using UnityEngine;
using UnityEngine.EventSystems;

namespace AgriDabao3D
{
    public class DraggableSelectionBox : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
    {
        public RectTransform dragArea;
        public RectTransform boxRect;

        [Tooltip("Optional. The box cannot be dragged past the edges of this rectangle - " +
                 "the window the map is seen through - so it can never be pushed under " +
                 "the frame to somewhere the player cannot see or reach it.")]
        public RectTransform visibleArea;

        private static readonly Vector3[] Corners = new Vector3[4];

        private Vector2 grabOffset;
        private bool dragging;
        private int dragPointerId;

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public Rect GetNormalizedRect()
        {
            if (dragArea == null || boxRect == null)
                return new Rect(0.3f, 0.47f, 0.02f, 0.02f);

            float parentWidth = dragArea.rect.width;
            float parentHeight = dragArea.rect.height;

            float x = boxRect.anchoredPosition.x / parentWidth;
            float y = boxRect.anchoredPosition.y / parentHeight;
            float w = boxRect.rect.width / parentWidth;
            float h = boxRect.rect.height / parentHeight;

            return new Rect(x, y, w, h);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // A second finger landing mid-drag must not take the box over.
            if (dragging && eventData.pointerId != dragPointerId)
                return;

            dragging = false;

            if (dragArea == null || boxRect == null)
                return;

            if (!TryPointerInArea(eventData, out Vector2 pointer))
                return;

            // Kept for the whole drag, so the box travels with the pointer from
            // wherever it was grabbed. The version before this measured the grab
            // from the box's centre while positioning it by its corner, which made
            // the box jump half its own size down and left the moment a drag began.
            grabOffset = boxRect.anchoredPosition - pointer;
            dragging = true;
            dragPointerId = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || eventData.pointerId != dragPointerId || dragArea == null || boxRect == null)
                return;

            if (!TryPointerInArea(eventData, out Vector2 pointer))
                return;

            float boxWidth = boxRect.rect.width;
            float boxHeight = boxRect.rect.height;

            float minX = 0f;
            float minY = 0f;
            float maxX = dragArea.rect.width - boxWidth;
            float maxY = dragArea.rect.height - boxHeight;

            // Held to what can actually be seen. The map is larger than the frame's
            // window once it is zoomed, and a box pushed past the window's edge
            // carries on across the hidden part of the map where the player has no
            // way to get it back.
            if (visibleArea != null && TryGetVisibleBounds(out Rect visible))
            {
                minX = Mathf.Max(minX, visible.xMin);
                minY = Mathf.Max(minY, visible.yMin);
                maxX = Mathf.Min(maxX, visible.xMax - boxWidth);
                maxY = Mathf.Min(maxY, visible.yMax - boxHeight);
            }

            // A window narrower than the box leaves no valid range at all; centre it
            // rather than letting the clamp pick one edge.
            if (maxX < minX) minX = maxX = (minX + maxX) * 0.5f;
            if (maxY < minY) minY = maxY = (minY + maxY) * 0.5f;

            Vector2 target = pointer + grabOffset;

            boxRect.anchorMin = Vector2.zero;
            boxRect.anchorMax = Vector2.zero;
            boxRect.pivot = Vector2.zero;
            boxRect.anchoredPosition = new Vector2(
                Mathf.Clamp(target.x, minX, maxX),
                Mathf.Clamp(target.y, minY, maxY));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == dragPointerId)
                dragging = false;
        }

        private void OnDisable()
        {
            dragging = false;
        }

        /// <summary>
        /// The pointer measured from the drag area's bottom-left corner - the same
        /// space the box's anchoredPosition is in, since the box is anchored there.
        /// </summary>
        private bool TryPointerInArea(PointerEventData eventData, out Vector2 point)
        {
            point = Vector2.zero;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragArea,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 local))
            {
                return false;
            }

            point = local - dragArea.rect.min;
            return true;
        }

        /// <summary>
        /// The part of the drag area currently showing through
        /// <see cref="visibleArea"/>, in the same bottom-left space. Worked out from
        /// world corners, so the map's zoom and pan are already accounted for.
        /// </summary>
        private bool TryGetVisibleBounds(out Rect bounds)
        {
            bounds = default;
            visibleArea.GetWorldCorners(Corners);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < Corners.Length; i++)
            {
                Vector2 local = dragArea.InverseTransformPoint(Corners[i]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            Vector2 origin = dragArea.rect.min;
            bounds = Rect.MinMaxRect(min.x - origin.x, min.y - origin.y, max.x - origin.x, max.y - origin.y);
            return bounds.width > 0.5f && bounds.height > 0.5f;
        }
    }
}
