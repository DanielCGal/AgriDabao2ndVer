using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class TradeDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public InventoryItemType itemType;
        public Sprite icon;

        public static InventoryItemType Dragging { get; private set; } = InventoryItemType.None;

        private GameObject ghost;
        private Canvas rootCanvas;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (itemType == InventoryItemType.None)
                return;

            Dragging = itemType;

            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
                return;

            ghost = new GameObject("TradeDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            ghost.transform.SetParent(rootCanvas.transform, false);
            ghost.transform.SetAsLastSibling();

            RectTransform rect = ghost.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80f, 80f);
            rect.position = eventData.position;

            Image image = ghost.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = icon != null;

            CanvasGroup group = ghost.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0.85f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost != null)
                ghost.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ghost != null)
            {
                Destroy(ghost);
                ghost = null;
            }

            Dragging = InventoryItemType.None;
        }
    }

    public class TradeDropSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public Action<InventoryItemType> onItemDropped;

        public Action<InventoryItemType> onSlotClicked;

        public InventoryItemType occupant = InventoryItemType.None;

        public bool interactable = true;

        public void OnDrop(PointerEventData eventData)
        {
            if (!interactable)
                return;

            InventoryItemType dragged = TradeDragSource.Dragging;
            if (dragged == InventoryItemType.None)
                return;

            onItemDropped?.Invoke(dragged);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable || occupant == InventoryItemType.None)
                return;

            onSlotClicked?.Invoke(occupant);
        }
    }
}
