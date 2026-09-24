using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public static class DistrictCropPools
    {
        public const int StartingSeedKinds = 3;

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

        private static readonly FarmCropType[] FallbackPool =
        {
            FarmCropType.Coconut,
            FarmCropType.Banana,
            FarmCropType.Cacao,
            FarmCropType.Corn
        };

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

        public static bool IsDistrictRestricted(InventoryItemType item)
        {
            return TryGetCrop(item, out _);
        }

        public static bool IsAvailableIn(InventoryItemType item, string districtName)
        {
            if (!TryGetCrop(item, out FarmCropType crop))
                return true;

            return System.Array.IndexOf(CropsFor(districtName), crop) >= 0;
        }

        public static bool IsAvailableToPlayer(InventoryItemType item)
        {
            return IsAvailableIn(item, SelectedAreaState.SelectedDistrictName);
        }

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

        public static FarmCropType[] CropsFor(string districtName)
        {
            if (!string.IsNullOrWhiteSpace(districtName) &&
                Pools.TryGetValue(districtName.Trim(), out FarmCropType[] pool))
            {
                return pool;
            }

            return FallbackPool;
        }

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

        public static List<InventoryItemType> RollStartingSeeds(string districtName)
        {
            List<FarmCropType> crops = new List<FarmCropType>(CropsFor(districtName));

            for (int i = crops.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (crops[i], crops[j]) = (crops[j], crops[i]);
            }

            List<InventoryItemType> picked = new List<InventoryItemType>();
            HashSet<FarmCropType> usedCrops = new HashSet<FarmCropType>();

            if (!TryPick(crops, usedCrops, picked, direct: true, allowMulch: false))
                TryPick(crops, usedCrops, picked, direct: true, allowMulch: true);

            TryPick(crops, usedCrops, picked, direct: false, allowMulch: true);

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

                picked.Add(options[Random.Range(0, options.Count)]);
                usedCrops.Add(crop);
                return true;
            }

            return false;
        }

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
