namespace AgriDabao3D
{
    public class CollectiblePomelo : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Pomelo;
            itemType = InventoryItemType.Pomelo;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Pomelo;
            itemType = InventoryItemType.Pomelo;
            amount = 1;
            base.Reset();
        }
    }
}
