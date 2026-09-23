namespace AgriDabao3D
{
    public class CollectibleMango : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Mango;
            itemType = InventoryItemType.Mango;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Mango;
            itemType = InventoryItemType.Mango;
            amount = 1;
            base.Reset();
        }
    }
}
