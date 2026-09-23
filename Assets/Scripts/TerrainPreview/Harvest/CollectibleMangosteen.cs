namespace AgriDabao3D
{
    public class CollectibleMangosteen : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Mangosteen;
            itemType = InventoryItemType.Mangosteen;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Mangosteen;
            itemType = InventoryItemType.Mangosteen;
            amount = 1;
            base.Reset();
        }
    }
}
