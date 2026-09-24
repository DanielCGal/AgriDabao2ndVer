using System.Collections.Generic;

namespace AgriDabao3D
{
    public static class ShopItemDescriptions
    {
        public static string For(InventoryItemType item)
        {
            string body = BaseDescription(item);
            if (string.IsNullOrEmpty(body) || !DistrictCropPools.IsDistrictRestricted(item))
                return body;

            return body + "\n\n" + DistrictAvailability(item);
        }

        private static string DistrictAvailability(InventoryItemType item)
        {
            List<string> districts = DistrictCropPools.DistrictsGrowing(item);
            string text = "AVAILABLE ONLY IN\n" + DistrictCropPools.JoinNames(districts) + ".";

            if (!DistrictCropPools.IsAvailableToPlayer(item))
            {
                string here = SelectedAreaState.SelectedDistrictName;
                text += string.IsNullOrWhiteSpace(here)
                    ? " Not sold on your farm."
                    : " Not sold on your farm in " + here.Trim() + ".";
            }

            return text;
        }

        private static string BaseDescription(InventoryItemType item)
        {
            if (PlantingMaterialCatalog.TryGet(PlantingMaterialCatalog.UpgradeLegacy(item),
                    out PlantingMaterialInfo material))
            {
                return MaterialDescription(material);
            }

            switch (item)
            {
                case InventoryItemType.SprayerPump:
                    return
                        "A refillable back-mounted sprayer. A tool, not a treatment - it "
                        + "holds nothing on its own.\n\n"
                        + "USED FOR\n"
                        + "Carrying and applying the five liquids: insecticide, "
                        + "disinfectant, neem soap, Bt bio-insecticide and copper "
                        + "fungicide. Each bottle fills it with 5000 ml. Without the pump "
                        + "you cannot use any liquid treatment at all, so buy this before "
                        + "buying any of them.";

                case InventoryItemType.AphidTrap:
                    return
                        "A sticky card staked beside the crop. Catches flying insects "
                        + "before they settle.\n\n"
                        + "USED FOR\n"
                        + "Aphids, and squash mosaic virus - which aphids carry, so "
                        + "trapping them is what stops the virus spreading. The cheapest "
                        + "pest treatment in the shop, and it needs no sprayer.";

                case InventoryItemType.NeemSoapLiter:
                    return
                        "An organic soap pressed from neem seed. Gentle on the crop and "
                        + "safe to use repeatedly.\n\n"
                        + "USED FOR\n"
                        + "Load into the Sprayer Pump. Treats the widest range of insects "
                        + "of any liquid: aphids, mealy bug, mites, 28-spotted lady beetle, "
                        + "coconut brontispa, mango flower beetle and squash mosaic virus.";

                case InventoryItemType.BtBioInsecticideLiter:
                    return
                        "A biological spray using Bacillus thuringiensis, which affects "
                        + "caterpillars and grubs but leaves other insects alone.\n\n"
                        + "USED FOR\n"
                        + "Load into the Sprayer Pump. The answer to anything that bores "
                        + "into fruit or stems: fall armyworm, corn borer, corn earworm, "
                        + "tomato fruit worm, and the cacao, citrus, durian, mango and "
                        + "pineapple borers.";

                case InventoryItemType.CopperFungicideLiter:
                    return
                        "A copper-based fungicide. The standard treatment for fungal "
                        + "disease across every crop.\n\n"
                        + "USED FOR\n"
                        + "Load into the Sprayer Pump. Treats anthracnose, cacao black pod "
                        + "rot, phytophthora, mango black spot, strawberry leaf spot, the "
                        + "corn ear rots, and pineapple core and root rot.";

                case InventoryItemType.DisinfectantLiter:
                    return
                        "A soil and tool disinfectant for bacterial infections that no "
                        + "insecticide or fungicide will touch.\n\n"
                        + "USED FOR\n"
                        + "Load into the Sprayer Pump. Treats bacterial wilt, banana bugtok "
                        + "and Panama disease. These three live in the soil, so badly "
                        + "infected plants may still need removing.";

                case InventoryItemType.InsecticideLiter:
                    return
                        "A broad chemical insecticide. Faster acting than neem soap but "
                        + "far less selective.\n\n"
                        + "USED FOR\n"
                        + "Load into the Sprayer Pump. A general-purpose insect spray for "
                        + "when a targeted treatment is not to hand.";

                case InventoryItemType.PheromoneTrap:
                    return
                        "A lure that draws in adult moths and flies before they can lay "
                        + "eggs on the crop.\n\n"
                        + "USED FOR\n"
                        + "Prevention rather than cure. Works on fall armyworm, corn borer, "
                        + "corn earworm, fruit fly, tomato fruit worm and the cacao, "
                        + "citrus, durian and pineapple borers. Needs no sprayer.";

                case InventoryItemType.FruitBag:
                    return
                        "A paper sleeve tied over developing fruit so nothing can reach "
                        + "it.\n\n"
                        + "USED FOR\n"
                        + "Protecting the fruit itself. Stops fruit fly, anthracnose, mango "
                        + "black spot, banana bugtok, cacao black pod rot and the fruit "
                        + "borers. Best fitted before damage appears, not after.";

                case InventoryItemType.TermiteBaitStation:
                    return
                        "A baited station sunk into the ground near the trunk.\n\n"
                        + "USED FOR\n"
                        + "Cacao termites, and nothing else. Termites nest below ground "
                        + "where a spray cannot reach, so this is the only thing that "
                        + "works on them.";

                case InventoryItemType.DrainageKit:
                    return
                        "A simple drainage improvement dug in around one crop.\n\n"
                        + "USED FOR\n"
                        + "Getting standing water away from the roots. Prevents the rots "
                        + "that start in waterlogged soil - phytophthora, bacterial wilt, "
                        + "Panama disease, and pineapple core, root and dieback rot.";

                case InventoryItemType.PruningShears:
                    return
                        "Shears for cutting away damaged and crowded growth.\n\n"
                        + "USED FOR\n"
                        + "Pruning a crop, which raises its health and lowers its stress. "
                        + "Pruning before a typhoon also gives the wind less to catch. Not "
                        + "every crop can be pruned - corn, squash and pineapple cannot.";

                case InventoryItemType.MulchBag:
                    return
                        "A bag of wood chips spread over the soil around a crop.\n\n"
                        + "USED FOR\n"
                        + "Holding moisture in the soil and lowering plant stress. The best "
                        + "single thing you can do for a crop during a drought, and it "
                        + "works on every crop in the game. Of little help against a "
                        + "typhoon. It can also cover a raised bed before planting - "
                        + "strawberry runners are planted through it, and whatever is "
                        + "planted in a mulched bed keeps the mulch.";

                case InventoryItemType.OrganicCompostBag:
                    return
                        "Composted organic matter worked into the soil.\n\n"
                        + "USED FOR\n"
                        + "Raising soil fertility and helping a crop recover health and "
                        + "shed stress. Accepted by every crop. It can only be applied "
                        + "again after about twelve days.";

                case InventoryItemType.SupportStakeKit:
                    return
                        "A stake driven in beside the crop and tied to its stem.\n\n"
                        + "USED FOR\n"
                        + "Holding a crop upright in strong wind. One of the two strongest "
                        + "typhoon defences for a single plant. Most valuable on banana, "
                        + "which is the crop wind damages most. Not every crop takes a "
                        + "stake - coconut, cacao, corn, squash and pineapple do not.";

                case InventoryItemType.TrellisKit:
                    return
                        "A frame that lifts a sprawling vine off the ground.\n\n"
                        + "USED FOR\n"
                        + "Supporting squash, the only crop that takes one. As effective "
                        + "against typhoon damage as a support stake, and it keeps the "
                        + "fruit off wet soil.";

                case InventoryItemType.RaisedBedKit:
                    return
                        "A raised planting bed that lifts the roots above the surrounding "
                        + "ground.\n\n"
                        + "USED FOR\n"
                        + "Keeping roots out of standing water in heavy rain, and a strong "
                        + "typhoon defence. Taken by pineapple, tomato, eggplant, squash "
                        + "and strawberry - the crops most at risk from waterlogged soil. "
                        + "Those crops are now planted on a raised bed built with the "
                        + "shovel, which counts as the same bed; the kit is for one "
                        + "already growing without a bed.";

                case InventoryItemType.IrrigationSystemKit:
                    return
                        "A watering system laid across part of the farm.\n\n"
                        + "USED FOR\n"
                        + "The single most effective answer to drought, covering an area "
                        + "rather than one crop. Do not rely on it in a typhoon - adding "
                        + "water during a storm makes things worse, not better.";

                case InventoryItemType.WaterStorageTankKit:
                    return
                        "A tank that stores water for use when there is none falling.\n\n"
                        + "USED FOR\n"
                        + "Carrying an area of the farm through a dry spell. Second only to "
                        + "irrigation against drought, and it does no harm in a storm.";

                case InventoryItemType.ShadeNetKit:
                    return
                        "A net stretched above the crops to cut direct sun.\n\n"
                        + "USED FOR\n"
                        + "Lowering heat stress during a drought. Especially worth it over "
                        + "strawberry, tomato, cacao and mangosteen, which all prefer "
                        + "cooler conditions than Davao normally offers. Young cacao needs "
                        + "one overhead at any time until it starts to fruit.";

                case InventoryItemType.WindbreakKit:
                    return
                        "A barrier raised along the windward edge of the farm.\n\n"
                        + "USED FOR\n"
                        + "The strongest typhoon defence there is, protecting an area "
                        + "rather than one plant. Put it up before the storm season, not "
                        + "during it. It does nothing for drought.";

                case InventoryItemType.DrainageCanalKit:
                    return
                        "A channel dug to carry storm water off the field.\n\n"
                        + "USED FOR\n"
                        + "Moving heavy rain away before it drowns the roots. As valuable "
                        + "as a windbreak during a typhoon. Avoid it in a drought - it "
                        + "drains away water the crops still need.";

                case InventoryItemType.GreenhouseKit:
                    return
                        "An enclosed growing structure covering part of the farm.\n\n"
                        + "USED FOR\n"
                        + "Sheltering crops from both extremes. Strong against a typhoon "
                        + "and of some help in a drought, which makes it the most flexible "
                        + "structure in the shop - and the most expensive.";

                default:
                    return string.Empty;
            }
        }

        private static string MaterialDescription(PlantingMaterialInfo material)
        {
            return CropText(material.Crop)
                   + "\n\nHOW TO PLANT\n" + PlantingMaterialCatalog.DescribeRoute(material)
                   + "\n\nIN REAL FARMS\n" + material.RealWorld;
        }

        private static string CropText(FarmCropType crop)
        {
            switch (crop)
            {
                case FarmCropType.Coconut:
                    return
                        "The tall palm behind most Davao farms. It takes the longest of "
                        + "any crop to reach its first harvest, but once it does it keeps "
                        + "producing for years.\n\n"
                        + "CARE\n"
                        + "Mulch, organic compost and pruning. Watch for coconut brontispa "
                        + "on the leaves and phytophthora in soil that stays wet.\n\n"
                        + "WATCH OUT\n"
                        + "The strongest crop you can plant against a typhoon - wind barely "
                        + "moves it. Its real cost is patience: over six months before the "
                        + "first nut.\n\n"
                        + "HARVEST\n"
                        + "First nuts around day 192, then every 45 days. Up to 6 nuts a "
                        + "pick on a healthy, unstressed tree.";

                case FarmCropType.Banana:
                    return
                        "A fast, heavy producer. Fruits in about three months and gives "
                        + "more fruit per harvest than almost anything else on the farm.\n\n"
                        + "CARE\n"
                        + "Mulch, organic compost, pruning and a support stake. Watch for "
                        + "bugtok and Panama disease.\n\n"
                        + "WATCH OUT\n"
                        + "The most wind-vulnerable crop in the game. A typhoon does over "
                        + "three times the damage it does to a coconut, so stake it before "
                        + "the storm season. Panama disease lives in the soil and cannot be "
                        + "sprayed away - infected plants have to be pulled.\n\n"
                        + "HARVEST\n"
                        + "First bunch around day 96, then every 45 days. 12 to 30 bananas "
                        + "per bunch depending on health and stress.";

                case FarmCropType.Durian:
                    return
                        "The king of fruits, and the slowest of the tree crops to pay off. "
                        + "Few fruits per harvest, but each one is worth the most.\n\n"
                        + "CARE\n"
                        + "Mulch, organic compost, pruning and a support stake. Watch for "
                        + "fruit borer, mealy bug, anthracnose and phytophthora.\n\n"
                        + "WATCH OUT\n"
                        + "Tall and heavy with fruit, so wind is a real threat - stake it. "
                        + "Phytophthora takes hold in soil that never dries, so keep the "
                        + "drainage good.\n\n"
                        + "HARVEST\n"
                        + "First fruit around day 170, then every 60 days. 1 to 5 durian a "
                        + "pick.";

                case FarmCropType.Pomelo:
                    return
                        "A citrus tree that fruits steadily once established, and tolerates "
                        + "a wider temperature range than most.\n\n"
                        + "CARE\n"
                        + "Mulch, organic compost, pruning and a support stake. Watch for "
                        + "fruit fly, citrus fruit borer, aphids and anthracnose.\n\n"
                        + "WATCH OUT\n"
                        + "Three of its four problems attack the fruit itself rather than "
                        + "the leaves, so damage often shows only at harvest. Fruit bags "
                        + "are the cheapest defence.\n\n"
                        + "HARVEST\n"
                        + "First fruit around day 128, then every 46 days. 2 to 8 pomelo a "
                        + "pick.";

                case FarmCropType.Cacao:
                    return
                        "A shade-loving tree grown for its pods. Produces more often than "
                        + "any other tree crop - a new pick roughly every month.\n\n"
                        + "CARE\n"
                        + "Mulch, organic compost and pruning. Watch for black pod rot, pod "
                        + "borer, termites and mealy bug.\n\n"
                        + "WATCH OUT\n"
                        + "Young cacao needs shade: keep a Shade Net Kit over it until it "
                        + "starts to fruit, or it carries extra stress. It prefers cooler "
                        + "shade than open sun and suffers above 30 degrees. Black pod rot "
                        + "spreads in long wet spells, and termites need bait stations "
                        + "rather than spray.\n\n"
                        + "HARVEST\n"
                        + "First pods around day 112, then every 32 days. 2 to 14 pods a "
                        + "pick - the highest count of any tree crop here.";

                case FarmCropType.Pineapple:
                    return
                        "A low ground crop that needs no staking and no pruning. The "
                        + "quickest of the long-season crops to first fruit.\n\n"
                        + "CARE\n"
                        + "Mulch and organic compost, on the raised bed it is planted in - "
                        + "it accepts no other care. Watch for mealy bug, core rot, "
                        + "dieback, root rot and fruit borer.\n\n"
                        + "WATCH OUT\n"
                        + "Three of its five problems are rots that start in waterlogged "
                        + "soil. A raised bed and good drainage prevent more than any spray "
                        + "cures. Wind is not a concern - it sits too low to catch it.\n\n"
                        + "HARVEST\n"
                        + "First fruit around day 96, then every 54 days. Only 1 to 3 "
                        + "pineapples a pick.";

                case FarmCropType.Mangosteen:
                    return
                        "A slow, shade-loving tree that gives a large bundle of fruit when "
                        + "it finally produces.\n\n"
                        + "CARE\n"
                        + "Mulch, organic compost and pruning. Watch for mealy bug, mites "
                        + "and anthracnose.\n\n"
                        + "WATCH OUT\n"
                        + "The longest wait of any crop - half a year before the first "
                        + "fruit. It also dislikes heat and dry spells, wanting steady "
                        + "moisture and shade below 30 degrees.\n\n"
                        + "HARVEST\n"
                        + "First fruit around day 182, then every 58 days. A bundle of 6 to "
                        + "20 fruits at a time.";

                case FarmCropType.Mango:
                    return
                        "A hardy tree that handles heat better than anything else on the "
                        + "farm, and gives a large bundle per harvest.\n\n"
                        + "CARE\n"
                        + "Mulch, organic compost, pruning and a support stake. Watch for "
                        + "anthracnose, black spot, twig borer and flower beetle.\n\n"
                        + "WATCH OUT\n"
                        + "Copes with temperatures up to 40 degrees, so drought is less of "
                        + "a threat than for most crops. Its weakness is wet weather: "
                        + "anthracnose and black spot both spread in long rain.\n\n"
                        + "HARVEST\n"
                        + "First fruit around day 140, then every 56 days. A bundle of 8 to "
                        + "30 mangoes at a time. A grafted seedling reaches it sooner than "
                        + "a liso seed.";

                case FarmCropType.Corn:
                    return
                        "The fastest crop in the game. Ready in six weeks and cropping "
                        + "again a month later, which makes it the usual choice for early "
                        + "money.\n\n"
                        + "CARE\n"
                        + "Only mulch and organic compost - corn accepts no stake, trellis "
                        + "or raised bed. Watch for fall armyworm, corn borer, earworm and "
                        + "two ear rots.\n\n"
                        + "WATCH OUT\n"
                        + "Five different pests, more than any other crop except tomato, "
                        + "and the fewest tools to defend it with. Fall armyworm in "
                        + "particular can strip a field fast, so check it often.\n\n"
                        + "HARVEST\n"
                        + "First ears around day 42, then every 28 days. 1 to 4 ears a "
                        + "pick.";

                case FarmCropType.Eggplant:
                    return
                        "A quick vegetable that keeps producing on a short cycle once it "
                        + "starts.\n\n"
                        + "CARE\n"
                        + "Mulch, organic compost, pruning and a support stake, on the "
                        + "raised bed it is planted in - it accepts every kind of care. "
                        + "Watch for aphids, lady beetle, fruit fly and bacterial wilt.\n\n"
                        + "WATCH OUT\n"
                        + "Bacterial wilt lives in the soil, so no spray removes it. "
                        + "Improve drainage and pull infected plants before it spreads.\n\n"
                        + "HARVEST\n"
                        + "First fruit around day 62, then every 18 days. 2 to 6 eggplants "
                        + "a pick.";

                case FarmCropType.Squash:
                    return
                        "A sprawling ground vine. Few fruits at a time, but each one is "
                        + "large.\n\n"
                        + "CARE\n"
                        + "Mulch, organic compost and a trellis, on the raised bed it is "
                        + "planted in. It is the only crop that takes a trellis. Watch for "
                        + "aphids, lady beetle and mosaic virus.\n\n"
                        + "WATCH OUT\n"
                        + "Mosaic virus has no cure once a plant is infected - the plant "
                        + "must be removed. Since aphids spread it, trapping aphids early "
                        + "is the real defence.\n\n"
                        + "HARVEST\n"
                        + "First fruit around day 55, then every 24 days. 1 to 3 squash a "
                        + "pick.";

                case FarmCropType.Strawberry:
                    return
                        "A small, fast-cycling crop with the shortest gap between harvests "
                        + "of anything on the farm.\n\n"
                        + "CARE\n"
                        + "Organic compost and pruning, on a raised bed covered with mulch "
                        + "before the runner goes in. Watch for aphids and leaf spot - only "
                        + "two problems, the fewest of any crop.\n\n"
                        + "WATCH OUT\n"
                        + "The hardest crop to grow well in Davao. Strawberry is happiest "
                        + "between 15 and 24 degrees and starts suffering above 32, while "
                        + "Davao sits near 30 degrees all year. Expect it to carry heat "
                        + "stress even on a good day, and expect drought to hit it hard.\n\n"
                        + "HARVEST\n"
                        + "First fruit around day 50, then every 16 days. 3 to 10 "
                        + "strawberries a pick.";

                case FarmCropType.Tomato:
                    return
                        "The biggest producer on the farm by count, on a short cycle - but "
                        + "also the crop with the most ways to fail.\n\n"
                        + "CARE\n"
                        + "Mulch, organic compost, pruning and a support stake, on the "
                        + "raised bed it is planted in. Watch for aphids, lady beetle, fruit "
                        + "worm, anthracnose and bacterial wilt.\n\n"
                        + "WATCH OUT\n"
                        + "Five problems, tied with corn for the most, and it prefers "
                        + "cooler weather than Davao usually gives - comfortable to 28 "
                        + "degrees, struggling past 35. Bacterial wilt is soil-borne and "
                        + "cannot be sprayed away.\n\n"
                        + "HARVEST\n"
                        + "First fruit around day 60, then every 20 days. A bundle of 8 to "
                        + "40 tomatoes at a time - the largest harvest in the game.";

                default:
                    return string.Empty;
            }
        }
    }
}
