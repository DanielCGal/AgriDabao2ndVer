namespace AgriDabao3D
{
    public class CollectibleCorn : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Corn;
            itemType = InventoryItemType.Corn;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Corn;
            itemType = InventoryItemType.Corn;
            amount = 1;
            base.Reset();
        }
    }
}
