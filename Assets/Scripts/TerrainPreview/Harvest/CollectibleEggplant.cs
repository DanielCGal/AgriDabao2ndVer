namespace AgriDabao3D
{
    public class CollectibleEggplant : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Eggplant;
            itemType = InventoryItemType.Eggplant;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Eggplant;
            itemType = InventoryItemType.Eggplant;
            amount = 1;
            base.Reset();
        }
    }
}
