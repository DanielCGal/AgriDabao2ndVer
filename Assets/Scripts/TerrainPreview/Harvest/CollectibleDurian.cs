namespace AgriDabao3D
{
    public class CollectibleDurian : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Durian;
            itemType = InventoryItemType.Durian;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Durian;
            itemType = InventoryItemType.Durian;
            amount = 1;
            base.Reset();
        }
    }
}
