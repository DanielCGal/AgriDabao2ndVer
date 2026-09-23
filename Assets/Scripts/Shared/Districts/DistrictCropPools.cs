using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Which crops each Davao district actually grows, and the starting planting
    /// materials a new farm is given because of it.
    ///
    /// The game used to hand every new player all thirteen seed types at once,
    /// which made the district choice cosmetic and left nothing to discover in the
    /// shop. A farm now starts with three kinds of planting material drawn from its
    /// own district's crops.
    ///
    /// The list is also the limit. A farm can only get the planting materials of
    /// crops its district grows - every material of that crop, so a Calinan farm
    /// can have both the banana plantlet and the banana sucker. The shop greys the
    /// rest out and will not sell them, the marketplace does not show them, and a
    /// trade cannot hand one over. The server holds the same table
    /// (DistrictSeedPools in the backend) and refuses marketplace purchases and
    /// trades on its side too, so the two must be kept identical.
    ///
    /// The five old seed items (banana, coconut, mango, pineapple and strawberry
    /// seed) are no longer sold, but they are still limited by their crop, so an
    /// older save or listing cannot move them to a district that does not grow it.
    /// </summary>
    public static class DistrictCropPools
    {
        /// <summary>How many different planting materials a new farm starts with.</summary>
        public const int StartingSeedKinds = 3;

        /// <summary>How many of each.</summary>
        public const int SeedsPerKind = 3;

        private static readonly Dictionary<string, FarmCropType[]> Pools =
            new Dictionary<string, FarmCropType[]>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["Calinan"] = new[]
                {
                    FarmCropType.Pineapple,
                    FarmCropType.Pomelo,
                    FarmCropType.Mango,
                    FarmCropType.Durian,
                    FarmCropType.Banana,
                    FarmCropType.Coconut,
                    FarmCropType.Cacao,
                    FarmCropType.Mangosteen
                },
                ["Toril"] = new[]
                {
                    FarmCropType.Coconut,
                    FarmCropType.Banana,
                    FarmCropType.Cacao,
                    FarmCropType.Mango,
                    FarmCropType.Pomelo,
                    FarmCropType.Durian,
                    FarmCropType.Pineapple
                },
                ["Baguio"] = new[]
                {
                    FarmCropType.Coconut,
                    FarmCropType.Mangosteen,
                    FarmCropType.Banana,
                    FarmCropType.Cacao,
                    FarmCropType.Durian,
                    FarmCropType.Corn
                },
                ["Paquibato"] = new[]
                {
                    FarmCropType.Corn,
                    FarmCropType.Banana,
                    FarmCropType.Coconut,
                    FarmCropType.Cacao
                },
                ["Marilog"] = new[]
                {
                    FarmCropType.Tomato,
                    FarmCropType.Squash,
                    FarmCropType.Eggplant,
                    FarmCropType.Strawberry,
                    FarmCropType.Mangosteen
                },
                ["Buhangin"] = new[]
                {
                    FarmCropType.Coconut,
                    FarmCropType.Cacao,
                    FarmCropType.Banana,
                    FarmCropType.Corn
                },
                ["Tugbok"] = new[]
                {
                    FarmCropType.Banana,
                    FarmCropType.Cacao,
                    FarmCropType.Coconut,
                    FarmCropType.Mangosteen,
                    FarmCropType.Corn,
                    FarmCropType.Durian,
                    FarmCropType.Mango
                }
            };

        /// <summary>
        /// Fallback for a farm whose district is missing or unrecognised - an
        /// older save, or a district renamed later. Four hardy crops that appear in
        /// most of the districts, so such a farm is still playable.
        /// </summary>
        private static readonly FarmCropType[] FallbackPool =
        {
            FarmCropType.Coconut,
            FarmCropType.Banana,
            FarmCropType.Cacao,
            FarmCropType.Corn
        };

        /// <summary>The crop an item is the planting material of, old seed items included.</summary>
        public static bool TryGetCrop(InventoryItemType item, out FarmCropType crop)
        {
            crop = FarmCropType.Coconut;
            if (!PlantingMaterialCatalog.TryGet(PlantingMaterialCatalog.UpgradeLegacy(item),
                    out PlantingMaterialInfo info))
            {
                return false;
            }

            crop = info.Crop;
            return true;
        }

        /// <summary>
        /// Whether the item is planting material, and so limited to districts at
        /// all. Produce, tools and kits are not - anyone can buy and trade those.
        /// </summary>
        public static bool IsDistrictRestricted(InventoryItemType item)
        {
            return TryGetCrop(item, out _);
        }

        /// <summary>Whether a farm in <paramref name="districtName"/> may obtain the item.</summary>
        public static bool IsAvailableIn(InventoryItemType item, string districtName)
        {
            if (!TryGetCrop(item, out FarmCropType crop))
                return true;

            return System.Array.IndexOf(CropsFor(districtName), crop) >= 0;
        }

        /// <summary>Whether the farm being played may obtain the item.</summary>
        public static bool IsAvailableToPlayer(InventoryItemType item)
        {
            return IsAvailableIn(item, SelectedAreaState.SelectedDistrictName);
        }

        /// <summary>The districts that grow an item's crop, in the order the game lists districts.</summary>
        public static List<string> DistrictsGrowing(InventoryItemType item)
        {
            var names = new List<string>();
            if (!TryGetCrop(item, out FarmCropType crop))
                return names;

            foreach (string district in DavaoDistrictService.ProductiveDistrictOrder)
            {
                if (Pools.TryGetValue(district, out FarmCropType[] pool) &&
                    System.Array.IndexOf(pool, crop) >= 0)
                {
                    names.Add(district);
                }
            }

            return names;
        }

        /// <summary>"Calinan, Toril and Baguio".</summary>
        public static string JoinNames(IReadOnlyList<string> names)
        {
            if (names == null || names.Count == 0)
                return string.Empty;
            if (names.Count == 1)
                return names[0];

            var head = new List<string>(names.Count - 1);
            for (int i = 0; i < names.Count - 1; i++)
                head.Add(names[i]);

            return string.Join(", ", head) + " and " + names[names.Count - 1];
        }

        public static bool IsKnownDistrict(string districtName)
        {
            return !string.IsNullOrWhiteSpace(districtName) &&
                   Pools.ContainsKey(districtName.Trim());
        }

        /// <summary>Every crop the named district grows, or the fallback list.</summary>
        public static FarmCropType[] CropsFor(string districtName)
        {
            if (!string.IsNullOrWhiteSpace(districtName) &&
                Pools.TryGetValue(districtName.Trim(), out FarmCropType[] pool))
            {
                return pool;
            }

            return FallbackPool;
        }

        /// <summary>Every planting material the named district's farms can get, in shelf order.</summary>
        public static IReadOnlyList<InventoryItemType> PoolFor(string districtName)
        {
            FarmCropType[] crops = CropsFor(districtName);
            List<InventoryItemType> materials = new List<InventoryItemType>();

            foreach (InventoryItemType item in PlantingMaterialCatalog.AllMaterials)
            {
                if (TryGetCrop(item, out FarmCropType crop) && System.Array.IndexOf(crops, crop) >= 0)
                    materials.Add(item);
            }

            return materials;
        }

        /// <summary>
        /// Picks the planting materials a new farm in this district starts with:
        /// three kinds from three different crops.
        ///
        /// The draw always includes one material that goes straight into the ground
        /// and one raised in the Seedling Tent, so a new player has something to
        /// plant on day one and something to learn the tent with. Every district
        /// grows both kinds. The first pick avoids strawberry where it can, since a
        /// runner needs a mulched bed before it can go in. Ready-grown squash
        /// seedlings are never dealt - they are the expensive shortcut.
        /// </summary>
        public static List<InventoryItemType> RollStartingSeeds(string districtName)
        {
            List<FarmCropType> crops = new List<FarmCropType>(CropsFor(districtName));

            // Copy before shuffling: the pool arrays are shared and must not be
            // reordered, or every later roll would inherit this one's shuffle.
            for (int i = crops.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (crops[i], crops[j]) = (crops[j], crops[i]);
            }

            List<InventoryItemType> picked = new List<InventoryItemType>();
            HashSet<FarmCropType> usedCrops = new HashSet<FarmCropType>();

            // 1. Something to plant today, preferring a crop that needs no mulch.
            if (!TryPick(crops, usedCrops, picked, direct: true, allowMulch: false))
                TryPick(crops, usedCrops, picked, direct: true, allowMulch: true);

            // 2. Something for the Seedling Tent.
            TryPick(crops, usedCrops, picked, direct: false, allowMulch: true);

            // 3. Anything else the district grows, either way.
            while (picked.Count < StartingSeedKinds &&
                   (TryPick(crops, usedCrops, picked, direct: null, allowMulch: true)))
            {
            }

            return picked;
        }

        private static bool TryPick(
            List<FarmCropType> crops,
            HashSet<FarmCropType> usedCrops,
            List<InventoryItemType> picked,
            bool? direct,
            bool allowMulch)
        {
            if (picked.Count >= StartingSeedKinds)
                return false;

            foreach (FarmCropType crop in crops)
            {
                if (usedCrops.Contains(crop))
                    continue;

                List<InventoryItemType> options = new List<InventoryItemType>();
                foreach (InventoryItemType item in PlantingMaterialCatalog.MaterialsFor(crop))
                {
                    if (!PlantingMaterialCatalog.TryGet(item, out PlantingMaterialInfo info) || info.ReadySeedling)
                        continue;
                    if (!allowMulch && info.NeedsMulchedBed)
                        continue;
                    if (direct == true && !info.PlantDirect)
                        continue;
                    if (direct == false && !info.SowInBag)
                        continue;
                    options.Add(item);
                }

                if (options.Count == 0)
                    continue;

                // Squash seed can go either way; picked as the tent material it
                // teaches the tent, picked first it is planted straight away.
                picked.Add(options[Random.Range(0, options.Count)]);
                usedCrops.Add(crop);
                return true;
            }

            return false;
        }

        /// <summary>"Durian Seed, Banana Sucker and Corn Seed" - for Antonio's dialogue and the log.</summary>
        public static string Describe(IReadOnlyList<InventoryItemType> seeds)
        {
            if (seeds == null || seeds.Count == 0)
                return "nothing";

            List<string> names = new List<string>(seeds.Count);
            foreach (InventoryItemType seed in seeds)
                names.Add(PlantingMaterialCatalog.NameOf(seed));

            if (names.Count == 1)
                return names[0];

            string last = names[names.Count - 1];
            names.RemoveAt(names.Count - 1);
            return string.Join(", ", names) + " and " + last;
        }
    }
}
