using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Which crops each Davao district actually grows, and the starting seeds a
    /// new farm is given because of it.
    ///
    /// The game used to hand every new player all thirteen seed types at once,
    /// which made the district choice cosmetic and left nothing to discover in the
    /// shop. A farm now starts with three seed kinds drawn from its own district's
    /// list.
    ///
    /// The list is also the limit. A farm can only get the seeds its district grows:
    /// the shop greys the rest out and will not sell them, the marketplace does not
    /// show them, and a trade cannot hand one over. The server holds the same table
    /// (DistrictSeedPools in the backend) and refuses marketplace purchases and
    /// trades on its side too, so the two must be kept identical.
    /// </summary>
    public static class DistrictCropPools
    {
        /// <summary>How many different seed kinds a new farm starts with.</summary>
        public const int StartingSeedKinds = 3;

        /// <summary>How many seeds of each kind.</summary>
        public const int SeedsPerKind = 3;

        private static readonly Dictionary<string, InventoryItemType[]> Pools =
            new Dictionary<string, InventoryItemType[]>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["Calinan"] = new[]
                {
                    InventoryItemType.PineappleSeed,
                    InventoryItemType.PomeloSeed,
                    InventoryItemType.MangoSeed,
                    InventoryItemType.DurianSeed,
                    InventoryItemType.BananaSeed,
                    InventoryItemType.CoconutSeed,
                    InventoryItemType.CacaoSeed,
                    InventoryItemType.MangosteenSeed
                },
                ["Toril"] = new[]
                {
                    InventoryItemType.CoconutSeed,
                    InventoryItemType.BananaSeed,
                    InventoryItemType.CacaoSeed,
                    InventoryItemType.MangoSeed,
                    InventoryItemType.PomeloSeed,
                    InventoryItemType.DurianSeed,
                    InventoryItemType.PineappleSeed
                },
                ["Baguio"] = new[]
                {
                    InventoryItemType.CoconutSeed,
                    InventoryItemType.MangosteenSeed,
                    InventoryItemType.BananaSeed,
                    InventoryItemType.CacaoSeed,
                    InventoryItemType.DurianSeed,
                    InventoryItemType.CornSeed
                },
                ["Paquibato"] = new[]
                {
                    InventoryItemType.CornSeed,
                    InventoryItemType.BananaSeed,
                    InventoryItemType.CoconutSeed,
                    InventoryItemType.CacaoSeed
                },
                ["Marilog"] = new[]
                {
                    InventoryItemType.TomatoSeed,
                    InventoryItemType.SquashSeed,
                    InventoryItemType.EggplantSeed,
                    InventoryItemType.StrawberrySeed,
                    InventoryItemType.MangosteenSeed
                },
                ["Buhangin"] = new[]
                {
                    InventoryItemType.CoconutSeed,
                    InventoryItemType.CacaoSeed,
                    InventoryItemType.BananaSeed,
                    InventoryItemType.CornSeed
                },
                ["Tugbok"] = new[]
                {
                    InventoryItemType.BananaSeed,
                    InventoryItemType.CacaoSeed,
                    InventoryItemType.CoconutSeed,
                    InventoryItemType.MangosteenSeed,
                    InventoryItemType.CornSeed,
                    InventoryItemType.DurianSeed,
                    InventoryItemType.MangoSeed
                }
            };

        /// <summary>
        /// Fallback pool for a farm whose district is missing or unrecognised - an
        /// older save, or a district renamed later. Four hardy crops that appear in
        /// most of the districts, so such a farm is still playable.
        /// </summary>
        private static readonly InventoryItemType[] FallbackPool =
        {
            InventoryItemType.CoconutSeed,
            InventoryItemType.BananaSeed,
            InventoryItemType.CacaoSeed,
            InventoryItemType.CornSeed
        };

        /// <summary>Every seed some district grows, which is every crop seed.</summary>
        private static readonly HashSet<InventoryItemType> RestrictedSeeds = CollectRestrictedSeeds();

        private static HashSet<InventoryItemType> CollectRestrictedSeeds()
        {
            var all = new HashSet<InventoryItemType>(FallbackPool);
            foreach (InventoryItemType[] pool in Pools.Values)
                all.UnionWith(pool);
            return all;
        }

        /// <summary>
        /// Whether the item is a crop seed, and so limited to districts at all.
        /// Produce, tools and kits are not - anyone can buy and trade those.
        /// </summary>
        public static bool IsDistrictRestricted(InventoryItemType item)
        {
            return RestrictedSeeds.Contains(item);
        }

        /// <summary>Whether a farm in <paramref name="districtName"/> may obtain the item.</summary>
        public static bool IsAvailableIn(InventoryItemType item, string districtName)
        {
            if (!IsDistrictRestricted(item))
                return true;

            IReadOnlyList<InventoryItemType> pool = PoolFor(districtName);
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] == item)
                    return true;
            }

            return false;
        }

        /// <summary>Whether the farm being played may obtain the item.</summary>
        public static bool IsAvailableToPlayer(InventoryItemType item)
        {
            return IsAvailableIn(item, SelectedAreaState.SelectedDistrictName);
        }

        /// <summary>The districts that grow a seed, in the order the game lists districts.</summary>
        public static List<string> DistrictsGrowing(InventoryItemType item)
        {
            var names = new List<string>();
            foreach (string district in DavaoDistrictService.ProductiveDistrictOrder)
            {
                if (Pools.TryGetValue(district, out InventoryItemType[] pool) &&
                    System.Array.IndexOf(pool, item) >= 0)
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

        /// <summary>Every crop the named district grows, or the fallback pool.</summary>
        public static IReadOnlyList<InventoryItemType> PoolFor(string districtName)
        {
            if (!string.IsNullOrWhiteSpace(districtName) &&
                Pools.TryGetValue(districtName.Trim(), out InventoryItemType[] pool))
            {
                return pool;
            }

            return FallbackPool;
        }

        /// <summary>
        /// Picks the seed kinds a new farm in this district starts with: three
        /// different kinds, never three of the same. Paquibato and Buhangin only
        /// grow four crops each, so there the draw is most of the pool - which is
        /// correct, those districts genuinely grow less variety.
        /// </summary>
        public static List<InventoryItemType> RollStartingSeeds(string districtName)
        {
            IReadOnlyList<InventoryItemType> pool = PoolFor(districtName);

            // Copy before shuffling: the pool arrays are shared and must not be
            // reordered, or every later roll would inherit this one's shuffle.
            List<InventoryItemType> shuffled = new List<InventoryItemType>(pool);

            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            int take = Mathf.Min(StartingSeedKinds, shuffled.Count);
            return shuffled.GetRange(0, take);
        }

        /// <summary>"Durian, Banana and Corn" - for Antonio's dialogue and the log.</summary>
        public static string Describe(IReadOnlyList<InventoryItemType> seeds)
        {
            if (seeds == null || seeds.Count == 0)
                return "nothing";

            List<string> names = new List<string>(seeds.Count);
            foreach (InventoryItemType seed in seeds)
                names.Add(seed.ToString().Replace("Seed", string.Empty));

            if (names.Count == 1)
                return names[0];

            string last = names[names.Count - 1];
            names.RemoveAt(names.Count - 1);
            return string.Join(", ", names) + " and " + last;
        }
    }
}
