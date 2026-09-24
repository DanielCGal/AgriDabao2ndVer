using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public static class SocialMarketplaceCatalog
    {
        public static readonly InventoryItemType[] TradableItems =
        {
            InventoryItemType.CoconutSeednut, InventoryItemType.Coconut,
            InventoryItemType.BananaPlantlet, InventoryItemType.BananaSucker, InventoryItemType.Banana,
            InventoryItemType.DurianSeed, InventoryItemType.Durian,
            InventoryItemType.PomeloSeed, InventoryItemType.Pomelo,
            InventoryItemType.CacaoSeed, InventoryItemType.Cacao,
            InventoryItemType.PineappleSucker, InventoryItemType.Pineapple,
            InventoryItemType.MangosteenSeed, InventoryItemType.Mangosteen,
            InventoryItemType.MangoGraftedSeedling, InventoryItemType.MangoLiso, InventoryItemType.Mango,
            InventoryItemType.CornSeed, InventoryItemType.Corn,
            InventoryItemType.EggplantSeed, InventoryItemType.Eggplant,
            InventoryItemType.SquashSeed, InventoryItemType.SquashSeedling, InventoryItemType.Squash,
            InventoryItemType.StrawberryRunner, InventoryItemType.Strawberry,
            InventoryItemType.TomatoSeed, InventoryItemType.Tomato,
            InventoryItemType.AphidTrap,
            InventoryItemType.InsecticideLiter,
            InventoryItemType.DisinfectantLiter,
            InventoryItemType.NeemSoapLiter,
            InventoryItemType.BtBioInsecticideLiter,
            InventoryItemType.CopperFungicideLiter,
            InventoryItemType.PheromoneTrap,
            InventoryItemType.FruitBag,
            InventoryItemType.DrainageKit,
            InventoryItemType.TermiteBaitStation,

            InventoryItemType.MulchBag,
            InventoryItemType.OrganicCompostBag,
            InventoryItemType.SupportStakeKit,
            InventoryItemType.TrellisKit,
            InventoryItemType.RaisedBedKit,
            InventoryItemType.IrrigationSystemKit,
            InventoryItemType.WaterStorageTankKit,
            InventoryItemType.ShadeNetKit,
            InventoryItemType.WindbreakKit,
            InventoryItemType.GreenhouseKit,
            InventoryItemType.DrainageCanalKit
        };

        public static readonly InventoryItemType[] MergeItems = BuildMergeItems();

        private static InventoryItemType[] BuildMergeItems()
        {
            List<InventoryItemType> items = new List<InventoryItemType>(TradableItems);
            foreach (InventoryItemType legacy in PlantingMaterialCatalog.LegacySeeds)
            {
                if (!items.Contains(legacy))
                    items.Add(legacy);
            }
            return items.ToArray();
        }

        private static readonly Dictionary<InventoryItemType, int> BaseValues =
            new Dictionary<InventoryItemType, int>
            {
                { InventoryItemType.CacaoSeed, PesoPrice.Centavos(25m) },
                { InventoryItemType.DurianSeed, PesoPrice.Centavos(60m) },
                { InventoryItemType.MangosteenSeed, PesoPrice.Centavos(75m) },
                { InventoryItemType.PomeloSeed, PesoPrice.Centavos(50m) },
                { InventoryItemType.BananaPlantlet, PesoPrice.Centavos(30m) },
                { InventoryItemType.BananaSucker, PesoPrice.Centavos(15m) },
                { InventoryItemType.MangoGraftedSeedling, PesoPrice.Centavos(80m) },
                { InventoryItemType.MangoLiso, PesoPrice.Centavos(30m) },
                { InventoryItemType.CoconutSeednut, PesoPrice.Centavos(15m) },
                { InventoryItemType.PineappleSucker, PesoPrice.Centavos(10m) },
                { InventoryItemType.StrawberryRunner, PesoPrice.Centavos(3m) },
                { InventoryItemType.TomatoSeed, PesoPrice.Centavos(9500m) },
                { InventoryItemType.EggplantSeed, PesoPrice.Centavos(8200m) },
                { InventoryItemType.SquashSeed, PesoPrice.Centavos(3000m) },
                { InventoryItemType.SquashSeedling, PesoPrice.Centavos(3300m) },
                { InventoryItemType.CornSeed, PesoPrice.Centavos(388.89m) },
                { InventoryItemType.PineappleSeed, PesoPrice.Centavos(10m) },
                { InventoryItemType.BananaSeed, PesoPrice.Centavos(15m) },
                { InventoryItemType.CoconutSeed, PesoPrice.Centavos(15m) },
                { InventoryItemType.MangoSeed, PesoPrice.Centavos(30m) },
                { InventoryItemType.StrawberrySeed, PesoPrice.Centavos(3m) },
                { InventoryItemType.Coconut, PesoPrice.Centavos(16.89m) },
                { InventoryItemType.Banana, PesoPrice.Centavos(51m) },
                { InventoryItemType.Durian, PesoPrice.Centavos(150m) },
                { InventoryItemType.Pomelo, PesoPrice.Centavos(200m) },
                { InventoryItemType.Cacao, PesoPrice.Centavos(275.27m) },
                { InventoryItemType.Pineapple, PesoPrice.Centavos(50m) },
                { InventoryItemType.Mangosteen, PesoPrice.Centavos(50m) },
                { InventoryItemType.Mango, PesoPrice.Centavos(118m) },
                { InventoryItemType.Corn, PesoPrice.Centavos(15m) },
                { InventoryItemType.Eggplant, PesoPrice.Centavos(62m) },
                { InventoryItemType.Squash, PesoPrice.Centavos(42m) },
                { InventoryItemType.Strawberry, PesoPrice.Centavos(350m) },
                { InventoryItemType.Tomato, PesoPrice.Centavos(156m) },
                { InventoryItemType.AphidTrap, PesoPrice.Centavos(35m) },
                { InventoryItemType.InsecticideLiter, PesoPrice.Centavos(90m) },
                { InventoryItemType.DisinfectantLiter, PesoPrice.Centavos(80m) },
                { InventoryItemType.NeemSoapLiter, PesoPrice.Centavos(100m) },
                { InventoryItemType.BtBioInsecticideLiter, PesoPrice.Centavos(130m) },
                { InventoryItemType.CopperFungicideLiter, PesoPrice.Centavos(120m) },
                { InventoryItemType.PheromoneTrap, PesoPrice.Centavos(90m) },
                { InventoryItemType.FruitBag, PesoPrice.Centavos(25m) },
                { InventoryItemType.DrainageKit, PesoPrice.Centavos(150m) },
                { InventoryItemType.TermiteBaitStation, PesoPrice.Centavos(110m) },

                { InventoryItemType.MulchBag, PesoPrice.Centavos(25m) },
                { InventoryItemType.OrganicCompostBag, PesoPrice.Centavos(45m) },
                { InventoryItemType.SupportStakeKit, PesoPrice.Centavos(60m) },
                { InventoryItemType.TrellisKit, PesoPrice.Centavos(75m) },
                { InventoryItemType.RaisedBedKit, PesoPrice.Centavos(140m) },
                { InventoryItemType.IrrigationSystemKit, PesoPrice.Centavos(450m) },
                { InventoryItemType.WaterStorageTankKit, PesoPrice.Centavos(600m) },
                { InventoryItemType.ShadeNetKit, PesoPrice.Centavos(260m) },
                { InventoryItemType.WindbreakKit, PesoPrice.Centavos(180m) },
                { InventoryItemType.GreenhouseKit, PesoPrice.Centavos(800m) },
                { InventoryItemType.DrainageCanalKit, PesoPrice.Centavos(320m) }
            };

        public static bool IsTradable(InventoryItemType item)
        {
            return Array.IndexOf(TradableItems, item) >= 0;
        }

        public static int GetBaseValueCentavos(InventoryItemType item)
        {
            return BaseValues.TryGetValue(item, out int centavos) ? centavos : 100;
        }

        public static int CalculateListingFee(InventoryItemType item, int quantity, int askingPrice)
        {
            long result = (long)PesoPrice.TotalPesos(GetBaseValueCentavos(item), Mathf.Max(0, quantity))
                          + Mathf.Max(0, askingPrice);
            return result > int.MaxValue ? int.MaxValue : (int)result;
        }

        public static string FriendlyName(InventoryItemType item)
        {
            if (PlantingMaterialCatalog.TryGet(item, out PlantingMaterialInfo material))
                return material.Name;

            switch (item)
            {
                case InventoryItemType.NeemSoapLiter: return "Neem Soap Liter";
                case InventoryItemType.BtBioInsecticideLiter: return "Bt Bio-Insecticide Liter";
                case InventoryItemType.CopperFungicideLiter: return "Copper Fungicide Liter";
                case InventoryItemType.PheromoneTrap: return "Pheromone Trap";
                case InventoryItemType.FruitBag: return "Fruit Bag";
                case InventoryItemType.DrainageKit: return "Drainage Kit";
                case InventoryItemType.TermiteBaitStation: return "Termite Bait Station";
                case InventoryItemType.MulchBag: return "Mulch Bag";
                case InventoryItemType.OrganicCompostBag: return "Organic Compost Bag";
                case InventoryItemType.SupportStakeKit: return "Support Stake Kit";
                case InventoryItemType.TrellisKit: return "Trellis Kit";
                case InventoryItemType.RaisedBedKit: return "Raised Bed Kit";
                case InventoryItemType.IrrigationSystemKit: return "Irrigation System Kit";
                case InventoryItemType.WaterStorageTankKit: return "Water Storage Tank Kit";
                case InventoryItemType.ShadeNetKit: return "Shade Net Kit";
                case InventoryItemType.WindbreakKit: return "Windbreak Kit";
                case InventoryItemType.GreenhouseKit: return "Greenhouse Kit";
                case InventoryItemType.DrainageCanalKit: return "Drainage Canal Kit";
                default:
                    string raw = item.ToString();
                    return raw.Replace("Seed", " Seed").Replace("Liter", " Liter");
            }
        }

        public static bool TryParseItem(string value, out InventoryItemType item)
        {
            return Enum.TryParse(value, out item) && IsTradable(item);
        }
    }
}

