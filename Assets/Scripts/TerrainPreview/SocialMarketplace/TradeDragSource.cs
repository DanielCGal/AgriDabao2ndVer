using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// An inventory entry inside the trade screen that can be dragged onto one of
    /// the player's own trade slots. Dropping is handled by <see cref="TradeDropSlot"/>;
    /// this side only carries the item identity and draws the drag ghost.
    /// </summary>
    public class TradeDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public InventoryItemType itemType;
        public Sprite icon;

        /// <summary>The item currently being dragged, or None when nothing is.</summary>
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

            // Cleared after the frame's drop handlers have run.
            Dragging = InventoryItemType.None;
        }
    }

    /// <summary>
    /// One cell of a player's 2x2 trade area. Accepts a dragged inventory item,
    /// and a plain click removes one unit again.
    /// </summary>
    public class TradeDropSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        /// <summary>Raised with the dropped item; the panel decides whether it fits.</summary>
        public Action<InventoryItemType> onItemDropped;

        /// <summary>Raised when a filled slot is clicked, to remove one unit.</summary>
        public Action<InventoryItemType> onSlotClicked;

        /// <summary>What this cell currently shows, or None when empty.</summary>
        public InventoryItemType occupant = InventoryItemType.None;

        /// <summary>False for the other player's slots, which are display-only.</summary>
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
