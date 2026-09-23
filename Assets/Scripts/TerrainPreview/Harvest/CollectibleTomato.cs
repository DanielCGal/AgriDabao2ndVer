namespace AgriDabao3D
{
    public class CollectibleTomato : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Tomato;
            itemType = InventoryItemType.Tomato;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Tomato;
            itemType = InventoryItemType.Tomato;
            amount = 1;
            base.Reset();
        }
    }
}
