using UnityEngine;

namespace AgriDabao3D
{
    public class CollectibleTropicalCrop : MonoBehaviour
    {
        public TropicalCropKind cropKind = TropicalCropKind.Durian;
        public InventoryItemType itemType = InventoryItemType.None;
        public int amount = 1;

        protected virtual void Awake()
        {
            if (itemType == InventoryItemType.None)
                itemType = TropicalCropCatalog.GetFruitItem(cropKind);

            amount = Mathf.Max(1, amount);
        }

        protected virtual void Reset()
        {
            itemType = TropicalCropCatalog.GetFruitItem(cropKind);
            amount = Mathf.Max(1, amount);
        }
    }
}
