using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AgriDabao3D
{
    /// <summary>
    /// One planting material: what it grows into, how it reaches the field, and
    /// the real-world timing the game compresses.
    /// </summary>
    public sealed class PlantingMaterialInfo
    {
        public InventoryItemType Item;
        public FarmCropType Crop;
        public string Name;

        /// <summary>Raised in a seedling bag in the Seedling Tent before it goes to the field.</summary>
        public bool SowInBag;

        /// <summary>Goes straight into prepared ground in the field.</summary>
        public bool PlantDirect;

        /// <summary>Bought already raised, so it skips the tent and is transplanted straight away.</summary>
        public bool ReadySeedling;

        /// <summary>The ground this crop needs in the field.</summary>
        public PreparedPlotKind Plot;

        /// <summary>The raised bed must be mulched before this goes in.</summary>
        public bool NeedsMulchedBed;

        /// <summary>Game days from sowing in a bag to ready for transplanting.</summary>
        public float NurseryDays;

        /// <summary>
        /// Above zero for seeds that germinate in a seed tray first and are then
        /// pricked into their bag by hand. The bag waits for the player after this
        /// many days, and the rest of <see cref="NurseryDays"/> starts from the
        /// pricking.
        /// </summary>
        public float PrickAfterDays;

        /// <summary>
        /// Extra age given on top of the nursery time. Used for the grafted mango,
        /// which the notes say flowers sooner than a seed-grown tree.
        /// </summary>
        public float ExtraAgeDays;

        /// <summary>How old a bought ready seedling already is, in game days.</summary>
        public float ReadySeedlingAgeDays;

        /// <summary>The real figure from the user's crop notes, shown beside the game one.</summary>
        public string RealWorld;

        /// <summary>A material that can go into the field today, without the tent.</summary>
        public bool FieldReady => PlantDirect || ReadySeedling;
    }

    /// <summary>
    /// The sixteen planting materials and the route each one takes to the field.
    ///
    /// The planting material, not the crop, decides the route. The same crop can
    /// come from different material - a banana from a tissue-cultured plantlet or
    /// a sword sucker, a mango from a grafted seedling or a liso seed - and squash
    /// seed can go either way, or be bought as a ready seedling. Every real-world
    /// figure below is taken from the user's own notes in the Handoff folder
    /// (crop_materials.txt and crop_production_notes.txt).
    ///
    /// Nursery waits are compressed into a few game days on purpose. A game day
    /// lasts fifteen real minutes, so even a five-month cacao nursery would take
    /// tens of hours of play; the real figure stays visible in the text instead.
    ///
    /// The five old seed items (banana, coconut, mango, pineapple and strawberry
    /// seed) are kept in the item list only so that older saves, trades and
    /// listings still read. They are turned into the material that replaced them
    /// wherever they are loaded.
    /// </summary>
    public static class PlantingMaterialCatalog
    {
        /// <summary>
        /// A seedling left in its bag long after it was ready only counts up to
        /// this multiple of its nursery time, so a forgotten bag does not turn into
        /// a tree that is already bearing.
        /// </summary>
        public const float MaxNurseryAgeMultiplier = 2f;

        private static readonly Dictionary<InventoryItemType, PlantingMaterialInfo> Materials =
            new Dictionary<InventoryItemType, PlantingMaterialInfo>();

        private static readonly Dictionary<InventoryItemType, InventoryItemType> LegacyUpgrades =
            new Dictionary<InventoryItemType, InventoryItemType>
            {
                { InventoryItemType.BananaSeed, InventoryItemType.BananaSucker },
                { InventoryItemType.CoconutSeed, InventoryItemType.CoconutSeednut },
                { InventoryItemType.MangoSeed, InventoryItemType.MangoLiso },
                { InventoryItemType.PineappleSeed, InventoryItemType.PineappleSucker },
                { InventoryItemType.StrawberrySeed, InventoryItemType.StrawberryRunner }
            };

        /// <summary>The order the shop shelf and the developer tools list them in, grouped by crop.</summary>
        public static readonly InventoryItemType[] AllMaterials =
        {
            InventoryItemType.CacaoSeed,
            InventoryItemType.DurianSeed,
            InventoryItemType.MangosteenSeed,
            InventoryItemType.PomeloSeed,
            InventoryItemType.BananaPlantlet,
            InventoryItemType.BananaSucker,
            InventoryItemType.MangoGraftedSeedling,
            InventoryItemType.MangoLiso,
            InventoryItemType.CoconutSeednut,
            InventoryItemType.PineappleSucker,
            InventoryItemType.StrawberryRunner,
            InventoryItemType.TomatoSeed,
            InventoryItemType.EggplantSeed,
            InventoryItemType.SquashSeed,
            InventoryItemType.SquashSeedling,
            InventoryItemType.CornSeed
        };

        static PlantingMaterialCatalog()
        {
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.CacaoSeed, Crop = FarmCropType.Cacao, Name = "Cacao Seed",
                SowInBag = true, Plot = PreparedPlotKind.Hole, NurseryDays = 6f,
                RealWorld = "Sprouts 14-21 days after sowing in a polybag and moves to the field 5-12 " +
                            "months after germination. Young cacao needs shade."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.DurianSeed, Crop = FarmCropType.Durian, Name = "Durian Seed",
                SowInBag = true, Plot = PreparedPlotKind.Hole, NurseryDays = 6f, PrickAfterDays = 2f,
                RealWorld = "Germinates 3-14 days after sowing. Seedlings are pricked into polybags " +
                            "9-12 days after germination and stay about 6-12 months in the nursery."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.MangosteenSeed, Crop = FarmCropType.Mangosteen, Name = "Mangosteen Seed",
                SowInBag = true, Plot = PreparedPlotKind.Hole, NurseryDays = 8f,
                RealWorld = "Needs fresh seed heavier than 1 g. Germinates about 30 days after sowing " +
                            "and is transplanted 18-24 months after planting - the longest nursery here."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.PomeloSeed, Crop = FarmCropType.Pomelo, Name = "Pomelo Seed",
                SowInBag = true, Plot = PreparedPlotKind.Hole, NurseryDays = 6f, PrickAfterDays = 2f,
                RealWorld = "Germinates in 2-4 weeks. Seedlings are potted into polybags 21-28 days " +
                            "after germination and kept 6-12 months before field planting."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.BananaPlantlet, Crop = FarmCropType.Banana, Name = "Banana Plantlet",
                SowInBag = true, Plot = PreparedPlotKind.Hole, NurseryDays = 4f,
                RealWorld = "A tissue-cultured plantlet, hardened in a polybag and moved to the field " +
                            "within several weeks to a few months. Clean, low-cost planting material."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.BananaSucker, Crop = FarmCropType.Banana, Name = "Banana Sucker",
                PlantDirect = true, Plot = PreparedPlotKind.Hole,
                RealWorld = "A sword sucker - about 20 cm tall in the Cardava study, 3-4 ft for Saba - " +
                            "planted straight into a hole about 60-80 cm deep."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.MangoGraftedSeedling, Crop = FarmCropType.Mango, Name = "Grafted Mango Seedling",
                SowInBag = true, Plot = PreparedPlotKind.Hole, NurseryDays = 2f, ExtraAgeDays = 35f,
                RealWorld = "Hardened in its polybag, then planted at the same depth it grew at in the " +
                            "nursery. Grafted trees may flower about 2-4 years after planting."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.MangoLiso, Crop = FarmCropType.Mango, Name = "Mango Liso",
                PlantDirect = true, Plot = PreparedPlotKind.Hole,
                RealWorld = "The seed-grown route: planted straight into a hole, and slower to flower " +
                            "than a grafted tree."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.CoconutSeednut, Crop = FarmCropType.Coconut, Name = "Coconut Seednut",
                PlantDirect = true, Plot = PreparedPlotKind.Hole,
                RealWorld = "A mature seednut, stored 3-4 weeks or soaked about 2 weeks to hasten " +
                            "sprouting. The first shoot takes about 3-4 months; sold here already sprouted."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.PineappleSucker, Crop = FarmCropType.Pineapple, Name = "Pineapple Sucker",
                PlantDirect = true, Plot = PreparedPlotKind.RaisedBed,
                RealWorld = "A young shoot with good roots and leaves, planted straight into prepared " +
                            "soil. The first fruit comes about 10-15 months after planting."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.StrawberryRunner, Crop = FarmCropType.Strawberry, Name = "Strawberry Runner",
                PlantDirect = true, Plot = PreparedPlotKind.RaisedBed, NeedsMulchedBed = true,
                RealWorld = "A runner rooted for about 4-6 weeks and cut from its mother plant, then " +
                            "planted through holes in black plastic mulch on a raised bed."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.TomatoSeed, Crop = FarmCropType.Tomato, Name = "Tomato Seed",
                SowInBag = true, Plot = PreparedPlotKind.RaisedBed, NurseryDays = 3f,
                RealWorld = "Grown about 14 days in a seed box, then transplanted at 3-5 true leaves, " +
                            "about 25-45 days after sowing."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.EggplantSeed, Crop = FarmCropType.Eggplant, Name = "Eggplant Seed",
                SowInBag = true, Plot = PreparedPlotKind.RaisedBed, NurseryDays = 4f,
                RealWorld = "Germinates in about 7-10 days and is ready for transplanting 4-6 weeks " +
                            "after sowing."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.SquashSeed, Crop = FarmCropType.Squash, Name = "Squash Seed",
                SowInBag = true, PlantDirect = true, Plot = PreparedPlotKind.RaisedBed, NurseryDays = 2f,
                RealWorld = "Germinates 4-10 days after planting. Sown straight into the field, or raised " +
                            "in a container and transplanted about 2 weeks after it comes up."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.SquashSeedling, Crop = FarmCropType.Squash, Name = "Squash Seedling",
                ReadySeedling = true, Plot = PreparedPlotKind.RaisedBed, ReadySeedlingAgeDays = 2f,
                RealWorld = "Already raised in a nursery tray and ready to transplant. It skips the wait " +
                            "in the tent, at a higher price than seed."
            });
            Add(new PlantingMaterialInfo
            {
                Item = InventoryItemType.CornSeed, Crop = FarmCropType.Corn, Name = "Corn Seed",
                PlantDirect = true, Plot = PreparedPlotKind.Furrow,
                RealWorld = "Sown in furrows about 8 cm deep. Germinates 4-10 days after planting and is " +
                            "harvested 70-120 days after planting."
            });
        }

        private static void Add(PlantingMaterialInfo info)
        {
            Materials[info.Item] = info;
        }

        // ------------------------------------------------------------- lookups

        public static bool TryGet(InventoryItemType item, out PlantingMaterialInfo info)
        {
            return Materials.TryGetValue(item, out info);
        }

        /// <summary>By saved name, upgrading an old seed name first.</summary>
        public static bool TryGet(string itemName, out PlantingMaterialInfo info)
        {
            info = null;
            return TryParseItem(itemName, out InventoryItemType item) &&
                   Materials.TryGetValue(UpgradeLegacy(item), out info);
        }

        public static bool IsPlantingMaterial(InventoryItemType item)
        {
            return Materials.ContainsKey(item);
        }

        public static bool IsNurseryMaterial(InventoryItemType item)
        {
            return TryGet(item, out PlantingMaterialInfo info) && info.SowInBag;
        }

        /// <summary>Can go into prepared ground today: a direct material or a bought seedling.</summary>
        public static bool IsFieldReadyMaterial(InventoryItemType item)
        {
            return TryGet(item, out PlantingMaterialInfo info) && info.FieldReady;
        }

        /// <summary>Every material that grows into this crop, in shelf order.</summary>
        public static List<InventoryItemType> MaterialsFor(FarmCropType crop)
        {
            List<InventoryItemType> result = new List<InventoryItemType>();
            foreach (InventoryItemType item in AllMaterials)
            {
                if (Materials[item].Crop == crop)
                    result.Add(item);
            }
            return result;
        }

        /// <summary>
        /// The material assumed for a crop that was planted before materials were
        /// recorded - what its old seed would be called today.
        /// </summary>
        public static InventoryItemType DefaultMaterialFor(FarmCropType crop)
        {
            switch (crop)
            {
                case FarmCropType.Coconut: return InventoryItemType.CoconutSeednut;
                case FarmCropType.Banana: return InventoryItemType.BananaSucker;
                case FarmCropType.Durian: return InventoryItemType.DurianSeed;
                case FarmCropType.Pomelo: return InventoryItemType.PomeloSeed;
                case FarmCropType.Cacao: return InventoryItemType.CacaoSeed;
                case FarmCropType.Pineapple: return InventoryItemType.PineappleSucker;
                case FarmCropType.Mangosteen: return InventoryItemType.MangosteenSeed;
                case FarmCropType.Mango: return InventoryItemType.MangoLiso;
                case FarmCropType.Corn: return InventoryItemType.CornSeed;
                case FarmCropType.Eggplant: return InventoryItemType.EggplantSeed;
                case FarmCropType.Squash: return InventoryItemType.SquashSeed;
                case FarmCropType.Strawberry: return InventoryItemType.StrawberryRunner;
                case FarmCropType.Tomato: return InventoryItemType.TomatoSeed;
                default: return InventoryItemType.None;
            }
        }

        /// <summary>The ground a crop needs in the field, whatever it was grown from.</summary>
        public static PreparedPlotKind PlotFor(FarmCropType crop)
        {
            switch (crop)
            {
                case FarmCropType.Tomato:
                case FarmCropType.Eggplant:
                case FarmCropType.Strawberry:
                case FarmCropType.Pineapple:
                case FarmCropType.Squash:
                    return PreparedPlotKind.RaisedBed;
                case FarmCropType.Corn:
                    return PreparedPlotKind.Furrow;
                default:
                    return PreparedPlotKind.Hole;
            }
        }

        public static bool NeedsMulchedBed(FarmCropType crop)
        {
            return crop == FarmCropType.Strawberry;
        }

        public static string PlotName(PreparedPlotKind kind)
        {
            switch (kind)
            {
                case PreparedPlotKind.Tilled: return "tilled ground";
                case PreparedPlotKind.Hole: return "planting hole";
                case PreparedPlotKind.RaisedBed: return "raised bed";
                case PreparedPlotKind.Furrow: return "furrow";
                default: return "prepared ground";
            }
        }

        public static string NameOf(InventoryItemType item)
        {
            if (TryGet(UpgradeLegacy(item), out PlantingMaterialInfo info))
                return info.Name;
            return SocialMarketplaceCatalog.FriendlyName(item);
        }

        public static string NameOf(string itemName)
        {
            return TryParseItem(itemName, out InventoryItemType item)
                ? NameOf(item)
                : itemName ?? string.Empty;
        }

        /// <summary>
        /// What a nursery material is called once it is growing in its bag: a seed
        /// becomes "Cacao Seedling", while a banana plantlet or a grafted mango
        /// seedling keeps its own name. Tacking "seedling" onto the material's
        /// name gave "Cacao Seed seedling".
        /// </summary>
        public static string SeedlingName(PlantingMaterialInfo info)
        {
            if (info == null)
                return "Seedling";

            return info.Item.ToString().EndsWith("Seed", StringComparison.Ordinal)
                ? info.Crop + " Seedling"
                : info.Name;
        }

        /// <summary>"Banana" to the crop; false for anything that is not a crop name.</summary>
        public static bool TryGetCropType(string cropName, out FarmCropType crop)
        {
            crop = FarmCropType.Coconut;
            return !string.IsNullOrWhiteSpace(cropName) &&
                   Enum.TryParse(cropName.Trim(), true, out crop) &&
                   Enum.IsDefined(typeof(FarmCropType), crop);
        }

        public static bool TryParseItem(string itemName, out InventoryItemType item)
        {
            item = InventoryItemType.None;
            return !string.IsNullOrWhiteSpace(itemName) &&
                   Enum.TryParse(itemName.Trim(), true, out item) &&
                   Enum.IsDefined(typeof(InventoryItemType), item) &&
                   item != InventoryItemType.None;
        }

        // ------------------------------------------------------- old seed items

        /// <summary>One of the five seed items the new materials replaced.</summary>
        public static bool IsLegacySeed(InventoryItemType item)
        {
            return LegacyUpgrades.ContainsKey(item);
        }

        public static IEnumerable<InventoryItemType> LegacySeeds => LegacyUpgrades.Keys;

        /// <summary>The replacement for an old seed item, or the item itself.</summary>
        public static InventoryItemType UpgradeLegacy(InventoryItemType item)
        {
            return LegacyUpgrades.TryGetValue(item, out InventoryItemType upgraded) ? upgraded : item;
        }

        /// <summary>The same, for an item saved by name. Unknown names are returned as they were.</summary>
        public static string UpgradeLegacyName(string itemName)
        {
            if (!TryParseItem(itemName, out InventoryItemType item))
                return itemName;

            return LegacyUpgrades.TryGetValue(item, out InventoryItemType upgraded)
                ? upgraded.ToString()
                : itemName;
        }

        /// <summary>
        /// Upgrades every old seed name inside an "A|B|C" alternative list, as the
        /// daily tasks and the adviser's objectives store them, keeping the rest.
        /// </summary>
        public static string UpgradeLegacyList(string alternatives)
        {
            if (string.IsNullOrWhiteSpace(alternatives))
                return alternatives;

            string[] parts = alternatives.Split('|');
            List<string> result = new List<string>(parts.Length);
            foreach (string part in parts)
            {
                string upgraded = UpgradeLegacyName(part.Trim());
                if (!result.Contains(upgraded))
                    result.Add(upgraded);
            }

            return string.Join("|", result);
        }

        // -------------------------------------------------------------- timing

        /// <summary>
        /// Age, in game days, a crop starts at when this material goes into the
        /// field. The user decided nursery days count toward the crop's age; a
        /// seedling left in its bag long after it was ready only counts up to
        /// <see cref="MaxNurseryAgeMultiplier"/> times its nursery time.
        /// </summary>
        public static float StartingAgeDays(PlantingMaterialInfo info, float daysInNursery)
        {
            if (info == null)
                return 0f;

            if (info.ReadySeedling)
                return Math.Max(0f, info.ReadySeedlingAgeDays + info.ExtraAgeDays);

            if (!info.SowInBag || daysInNursery <= 0f)
                return 0f;

            float nursery = Math.Min(daysInNursery, info.NurseryDays * MaxNurseryAgeMultiplier);
            return Math.Max(0f, nursery) + Math.Max(0f, info.ExtraAgeDays);
        }

        public static string FormatDays(float days)
        {
            if (days < 0f)
                days = 0f;

            int rounded = (int)Math.Round(days);
            if (Math.Abs(days - rounded) < 0.05f)
                return rounded == 1 ? "1 game day" : rounded + " game days";

            return days.ToString("0.0", CultureInfo.InvariantCulture) + " game days";
        }

        // ---------------------------------------------------------------- text

        /// <summary>
        /// The planting route in plain words, for the shop, the Seedling Tent and
        /// the adviser.
        /// </summary>
        public static string DescribeRoute(PlantingMaterialInfo info)
        {
            if (info == null)
                return string.Empty;

            string plot = PlotName(info.Plot);
            StringBuilder text = new StringBuilder();

            if (info.ReadySeedling)
            {
                text.Append("Till the ground with the shovel, build a ").Append(plot)
                    .Append(" and plant the seedling straight in - no wait in the Seedling Tent.");
                return text.ToString();
            }

            if (info.SowInBag && info.PlantDirect)
            {
                text.Append("Two ways. Till the ground, build a ").Append(plot)
                    .Append(" and sow it straight in; or fill a seedling bag in the Seedling Tent, sow it there, ")
                    .Append("wait about ").Append(FormatDays(info.NurseryDays))
                    .Append(" and transplant the seedling into a ").Append(plot).Append('.');
                return text.ToString();
            }

            if (info.SowInBag)
            {
                bool isPlant = info.Item == InventoryItemType.BananaPlantlet ||
                               info.Item == InventoryItemType.MangoGraftedSeedling;

                text.Append("Fill a seedling bag with soil in the Seedling Tent and ")
                    .Append(isPlant ? "set the plant in it" : "sow the seed in it");

                if (info.PrickAfterDays > 0f)
                {
                    text.Append(". It germinates after about ").Append(FormatDays(info.PrickAfterDays))
                        .Append("; prick it into the bag then, and it is ready about ")
                        .Append(FormatDays(info.NurseryDays - info.PrickAfterDays)).Append(" later");
                }
                else
                {
                    text.Append(isPlant ? ". It hardens for about " : ". It grows for about ")
                        .Append(FormatDays(info.NurseryDays));
                }

                text.Append(". Then till the ground, prepare a ").Append(plot).Append(" and transplant it.");
                return text.ToString();
            }

            text.Append("Till the ground with the shovel, ")
                .Append(info.Plot == PreparedPlotKind.Furrow ? "open a " : info.Plot == PreparedPlotKind.Hole ? "dig a " : "build a ")
                .Append(plot);
            if (info.NeedsMulchedBed)
                text.Append(", cover the bed with a Mulch Bag");
            text.Append(" and plant it straight in.");
            return text.ToString();
        }

        /// <summary>
        /// "Planted From" line for the crop board: the material, and how long it
        /// spent in the tent.
        /// </summary>
        public static string CropBoardLine(string materialName, float nurseryDays)
        {
            if (!TryGet(materialName, out PlantingMaterialInfo info))
                return string.Empty;

            string route;
            if (info.ReadySeedling)
                route = "bought as a ready seedling";
            else if (nurseryDays > 0.01f)
                route = "raised " + FormatDays(nurseryDays) + " in the Seedling Tent";
            else
                route = "planted directly";

            return "Planted From: " + info.Name + " (" + route + ")";
        }

        /// <summary>"In real farms: ..." for the crop board.</summary>
        public static string RealWorldLine(string materialName)
        {
            return TryGet(materialName, out PlantingMaterialInfo info) && !string.IsNullOrEmpty(info.RealWorld)
                ? "In real farms: " + info.RealWorld
                : string.Empty;
        }
    }

    /// <summary>
    /// The two planting lines every crop board shows, formatted to drop straight
    /// into the three crop classes' readouts: nothing at all for a crop planted
    /// before materials were recorded.
    /// </summary>
    public static class CropPlantingText
    {
        /// <summary>"Planted From: ..." plus its line break, or nothing.</summary>
        public static string PlantedFromLine(string materialName, float nurseryDays)
        {
            string line = PlantingMaterialCatalog.CropBoardLine(materialName, nurseryDays);
            return string.IsNullOrEmpty(line) ? string.Empty : line + "\n";
        }

        /// <summary>
        /// A line break plus "In real farms: ...". A crop planted before materials
        /// were recorded uses the material its old seed became, so every crop on
        /// the farm shows its real-world timing.
        /// </summary>
        public static string RealWorldLine(string materialName, FarmCropType crop)
        {
            if (string.IsNullOrWhiteSpace(materialName))
                materialName = PlantingMaterialCatalog.DefaultMaterialFor(crop).ToString();

            string line = PlantingMaterialCatalog.RealWorldLine(materialName);
            return string.IsNullOrEmpty(line) ? string.Empty : "\n" + line;
        }
    }
}
