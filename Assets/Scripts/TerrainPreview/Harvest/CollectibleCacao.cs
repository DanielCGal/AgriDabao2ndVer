namespace AgriDabao3D
{
    public class CollectibleCacao : CollectibleTropicalCrop
    {
        protected override void Awake()
        {
            cropKind = TropicalCropKind.Cacao;
            itemType = InventoryItemType.Cacao;
            base.Awake();
        }

        protected override void Reset()
        {
            cropKind = TropicalCropKind.Cacao;
            itemType = InventoryItemType.Cacao;
            amount = 1;
            base.Reset();
        }
    }
}
