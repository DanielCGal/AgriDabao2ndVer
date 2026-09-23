namespace AgriDabao3D
{
    public class CollectibleSquash : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Squash;
            itemType = InventoryItemType.Squash;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Squash;
            itemType = InventoryItemType.Squash;
            amount = 1;
            base.Reset();
        }
    }
}
