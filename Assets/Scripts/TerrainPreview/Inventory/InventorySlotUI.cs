using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class InventorySlotUI : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        public int slotIndex = -1;
        public bool isBackpackButton;

        public InventoryItemType itemType;
        public Image background;
        public Image iconImage;
        public Text countText;
        public Text labelText;

        private InventoryUIBuilder owner;
        private Canvas canvas;
        private GameObject dragGhost;
        private bool forwardingScrollDrag;

        public void Setup(InventoryUIBuilder builder, int index, bool backpackButton = false)
        {
            owner = builder;
            slotIndex = index;
            isBackpackButton = backpackButton;
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner == null)
                return;

            if (isBackpackButton)
            {
                owner.ToggleBackpack();
                return;
            }

            if (owner.IsTrashMode)
            {
                owner.HandleTrashSlotClicked(slotIndex);
                return;
            }

            owner.HandleSlotClicked(slotIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            forwardingScrollDrag = false;

            if (isBackpackButton || owner == null || PlayerInventory.Instance == null)
                return;

            if (owner.IsTrashMode)
                return;

            InventorySlotData slot = PlayerInventory.Instance.GetSlot(slotIndex);

            if ((slot == null || slot.IsEmpty) &&
                owner.IsBackpackOpen &&
                owner.BackpackScrollRect != null)
            {
                forwardingScrollDrag = true;
                owner.BackpackScrollRect.OnBeginDrag(eventData);
                return;
            }

            if (slot == null || slot.IsEmpty || iconImage == null || iconImage.sprite == null)
                return;

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            dragGhost = new GameObject("InventoryDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            dragGhost.transform.SetParent(canvas.transform, false);
            dragGhost.transform.SetAsLastSibling();

            RectTransform ghostRect = dragGhost.GetComponent<RectTransform>();
            ghostRect.sizeDelta = new Vector2(90f, 90f);
            ghostRect.position = eventData.position;

            Image ghostImage = dragGhost.GetComponent<Image>();
            ghostImage.sprite = iconImage.sprite;
            ghostImage.preserveAspect = true;
            ghostImage.raycastTarget = false;

            CanvasGroup group = dragGhost.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0.85f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (forwardingScrollDrag && owner != null && owner.BackpackScrollRect != null)
            {
                owner.BackpackScrollRect.OnDrag(eventData);
                return;
            }

            if (dragGhost != null)
                dragGhost.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (forwardingScrollDrag && owner != null && owner.BackpackScrollRect != null)
            {
                owner.BackpackScrollRect.OnEndDrag(eventData);
                forwardingScrollDrag = false;
                return;
            }

            if (dragGhost != null)
                Destroy(dragGhost);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (isBackpackButton || owner == null || owner.IsTrashMode)
                return;

            InventorySlotUI source = null;

            if (eventData.pointerDrag != null)
                source = eventData.pointerDrag.GetComponent<InventorySlotUI>();

            if (source == null || source.isBackpackButton)
                return;

            owner.HandleSlotDropped(source.slotIndex, slotIndex);
        }
    }
}

