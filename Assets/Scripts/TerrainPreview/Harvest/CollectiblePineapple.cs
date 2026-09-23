namespace AgriDabao3D
{
    public class CollectiblePineapple : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Pineapple;
            itemType = InventoryItemType.Pineapple;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Pineapple;
            itemType = InventoryItemType.Pineapple;
            amount = 1;
            base.Reset();
        }
    }
}
