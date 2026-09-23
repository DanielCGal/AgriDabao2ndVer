using UnityEngine;
namespace AgriDabao3D
{
    public enum InventoryItemType
    {
        None,
        Machete,
        CoconutSeed,
        Coconut,
        BananaSeed,
        Banana,
        WateringCan,
        AphidTrap,
        SprayerPump,
        InsecticideLiter,
        DisinfectantLiter,
        DurianSeed,
        Durian,
        PomeloSeed,
        Pomelo,
        CacaoSeed,
        Cacao,
        PineappleSeed,
        Pineapple,
        MangosteenSeed,
        Mangosteen,
        MangoSeed,
        Mango,
        CornSeed,
        Corn,
        EggplantSeed,
        Eggplant,
        SquashSeed,
        Squash,
        StrawberrySeed,
        Strawberry,
        TomatoSeed,
        Tomato,
        // Always add new enum items at the end.
        Shovel,
        NeemSoapLiter,
        BtBioInsecticideLiter,
        CopperFungicideLiter,
        PheromoneTrap,
        FruitBag,
        DrainageKit,
        TermiteBaitStation,
        // Climate mitigation and crop-maintenance items. Keep new values at the end.
        MulchBag,
        OrganicCompostBag,
        PruningShears,
        SupportStakeKit,
        TrellisKit,
        RaisedBedKit,
        IrrigationSystemKit,
        WaterStorageTankKit,
        ShadeNetKit,
        WindbreakKit,
        GreenhouseKit,
        DrainageCanalKit,
        // Planting materials for the planting system. Keep new values at the end:
        // saves store items by name, but the scene and Inspector store the number.
        //
        // BananaSeed, CoconutSeed, MangoSeed, PineappleSeed and StrawberrySeed above
        // are no longer sold. They stay only so older saves and marketplace data
        // still parse; PlantingMaterialCatalog turns them into the material that
        // replaced them the moment they are loaded.
        BananaPlantlet,
        BananaSucker,
        MangoGraftedSeedling,
        MangoLiso,
        CoconutSeednut,
        PineappleSucker,
        StrawberryRunner,
        SquashSeedling
    }
}
