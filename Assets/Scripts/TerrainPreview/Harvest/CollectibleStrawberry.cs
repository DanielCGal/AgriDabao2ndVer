namespace AgriDabao3D
{
    public class CollectibleStrawberry : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Strawberry;
            itemType = InventoryItemType.Strawberry;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Strawberry;
            itemType = InventoryItemType.Strawberry;
            amount = 1;
            base.Reset();
        }
    }
}
