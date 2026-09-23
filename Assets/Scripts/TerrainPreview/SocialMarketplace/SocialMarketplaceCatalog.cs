using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public static class SocialMarketplaceCatalog
    {
        public static readonly InventoryItemType[] TradableItems =
        {
            InventoryItemType.CoconutSeed, InventoryItemType.Coconut,
            InventoryItemType.BananaSeed, InventoryItemType.Banana,
            InventoryItemType.DurianSeed, InventoryItemType.Durian,
            InventoryItemType.PomeloSeed, InventoryItemType.Pomelo,
            InventoryItemType.CacaoSeed, InventoryItemType.Cacao,
            InventoryItemType.PineappleSeed, InventoryItemType.Pineapple,
            InventoryItemType.MangosteenSeed, InventoryItemType.Mangosteen,
            InventoryItemType.MangoSeed, InventoryItemType.Mango,
            InventoryItemType.CornSeed, InventoryItemType.Corn,
            InventoryItemType.EggplantSeed, InventoryItemType.Eggplant,
            InventoryItemType.SquashSeed, InventoryItemType.Squash,
            InventoryItemType.StrawberrySeed, InventoryItemType.Strawberry,
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

            // Weather/climate mitigation consumables and kits.
            // PruningShears remains excluded because it is an owned/reusable tool.
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

        // Per item, in centavos. Seeds mirror the shop's Davao City prices and
        // produce mirrors the shipping bin; the rest keep the NPC shop prices.
        // The server holds the same table (EconomyJsonService.buildBaseValues) and
        // the two must agree, or the listing fee shown is not the fee charged.
        private static readonly Dictionary<InventoryItemType, int> BaseValues =
            new Dictionary<InventoryItemType, int>
            {
                { InventoryItemType.PineappleSeed, PesoPrice.Centavos(10m) },
                { InventoryItemType.BananaSeed, PesoPrice.Centavos(15m) },
                { InventoryItemType.CacaoSeed, PesoPrice.Centavos(25m) },
                { InventoryItemType.CoconutSeed, PesoPrice.Centavos(15m) },
                { InventoryItemType.PomeloSeed, PesoPrice.Centavos(50m) },
                { InventoryItemType.MangoSeed, PesoPrice.Centavos(30m) },
                { InventoryItemType.MangosteenSeed, PesoPrice.Centavos(75m) },
                { InventoryItemType.DurianSeed, PesoPrice.Centavos(60m) },
                { InventoryItemType.CornSeed, PesoPrice.Centavos(388.89m) },
                { InventoryItemType.EggplantSeed, PesoPrice.Centavos(8200m) },
                { InventoryItemType.SquashSeed, PesoPrice.Centavos(3000m) },
                { InventoryItemType.StrawberrySeed, PesoPrice.Centavos(3m) },
                { InventoryItemType.TomatoSeed, PesoPrice.Centavos(9500m) },
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

                // Match the existing NPC shop prices.
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

        /// <summary>An item's marketplace base value, per item, in centavos.</summary>
        public static int GetBaseValueCentavos(InventoryItemType item)
        {
            return BaseValues.TryGetValue(item, out int centavos) ? centavos : 100;
        }

        /// <summary>
        /// The listing fee: the items' base value in whole pesos, plus the asking
        /// price. The base value total is rounded exactly as the server rounds it,
        /// so the fee shown here is the fee the server charges.
        /// </summary>
        public static int CalculateListingFee(InventoryItemType item, int quantity, int askingPrice)
        {
            long result = (long)PesoPrice.TotalPesos(GetBaseValueCentavos(item), Mathf.Max(0, quantity))
                          + Mathf.Max(0, askingPrice);
            return result > int.MaxValue ? int.MaxValue : (int)result;
        }

        public static string FriendlyName(InventoryItemType item)
        {
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

