using System;
using System.Collections.Generic;

namespace AgriDabao3D
{
    public static class DailyTaskCatalog
    {
        public const int ExpectedCount = 204;

        private static List<DailyTaskTemplate> cached;

        public static IReadOnlyList<DailyTaskTemplate> GetAll()
        {
            if (cached == null)
                cached = Build();
            return cached;
        }

        private static List<DailyTaskTemplate> Build()
        {
            List<DailyTaskTemplate> values = new List<DailyTaskTemplate>();
            AddRoutineTasks(values);          // 1-65
            AddGeneralTasks(values);          // 66-80
            AddDroughtTasks(values);          // 81-105
            AddTyphoonTasks(values);          // 106-130
            AddPestDiseaseTasks(values);      // 131-177
            AddAdvancedTasks(values);         // 178-200
            AddNurseryTasks(values);          // 201-204

            // Ids are positions in this list, and a saved task keeps its id, so
            // new templates only ever go on the end.
            if (values.Count != ExpectedCount)
            {
                throw new InvalidOperationException(
                    "DailyTaskCatalog must contain exactly " + ExpectedCount +
                    " templates, but contains " + values.Count + ".");
            }

            for (int i = 0; i < values.Count; i++)
                values[i].id = i + 1;
            return values;
        }

        private static void AddRoutineTasks(List<DailyTaskTemplate> list)
        {
            AddRoutineCrop(list, "Coconut",
                ("Mulch", "MulchBag"),
                ("OrganicCompost", "OrganicCompostBag"),
                ("Prune", "PruningShears"));
            AddRoutineCrop(list, "Banana",
                ("Mulch", "MulchBag"),
                ("SupportStake", "SupportStakeKit"),
                ("Prune", "PruningShears"));
            AddRoutineCrop(list, "Durian",
                ("Mulch", "MulchBag"),
                ("SupportStake", "SupportStakeKit"),
                ("Prune", "PruningShears"));
            AddRoutineCrop(list, "Pomelo",
                ("Mulch", "MulchBag"),
                ("SupportStake", "SupportStakeKit"),
                ("Prune", "PruningShears"));
            AddRoutineCrop(list, "Cacao",
                ("Mulch", "MulchBag"),
                ("OrganicCompost", "OrganicCompostBag"),
                ("Prune", "PruningShears"));
            AddRoutineCrop(list, "Pineapple",
                ("Mulch", "MulchBag"),
                ("OrganicCompost", "OrganicCompostBag"),
                ("RaisedBed", "RaisedBedKit"));
            AddRoutineCrop(list, "Mangosteen",
                ("Mulch", "MulchBag"),
                ("OrganicCompost", "OrganicCompostBag"),
                ("Prune", "PruningShears"));
            AddRoutineCrop(list, "Mango",
                ("Mulch", "MulchBag"),
                ("SupportStake", "SupportStakeKit"),
                ("Prune", "PruningShears"));
            AddRoutineCrop(list, "Corn",
                ("Mulch", "MulchBag"),
                ("OrganicCompost", "OrganicCompostBag"),
                ("Plant", "CornSeed"));
            AddRoutineCrop(list, "Eggplant",
                ("Mulch", "MulchBag"),
                ("SupportStake", "SupportStakeKit"),
                ("RaisedBed", "RaisedBedKit"));
            AddRoutineCrop(list, "Squash",
                ("Mulch", "MulchBag"),
                ("Trellis", "TrellisKit"),
                ("RaisedBed", "RaisedBedKit"));
            AddRoutineCrop(list, "Strawberry",
                ("Mulch", "MulchBag"),
                ("Prune", "PruningShears"),
                ("RaisedBed", "RaisedBedKit"));
            AddRoutineCrop(list, "Tomato",
                ("Mulch", "MulchBag"),
                ("SupportStake", "SupportStakeKit"),
                ("Prune", "PruningShears"));
        }

        private static void AddRoutineCrop(
            List<DailyTaskTemplate> list,
            string crop,
            (string action, string item) first,
            (string action, string item) second,
            (string action, string item) third)
        {
            list.Add(Task(
                "Water " + crop,
                "Water one " + crop + " crop that currently needs water.",
                DailyTaskKind.WaterCrop,
                DailyTaskDifficulty.Easy,
                crop: crop,
                amount: 1,
                threshold: 0.60f));

            list.Add(Task(
                "Harvest " + crop,
                "Harvest one harvest-ready " + crop + " crop.",
                DailyTaskKind.HarvestCrop,
                DailyTaskDifficulty.Easy,
                crop: crop,
                amount: 1));

            AddRoutineAction(list, crop, first.action, first.item);
            AddRoutineAction(list, crop, second.action, second.item);
            AddRoutineAction(list, crop, third.action, third.item);
        }

        private static void AddRoutineAction(
            List<DailyTaskTemplate> list,
            string crop,
            string action,
            string item)
        {
            if (string.Equals(action, "Plant", StringComparison.Ordinal))
            {
                list.Add(Task(
                    "Plant " + crop,
                    "Plant one " + crop + " in prepared ground.",
                    DailyTaskKind.PlantCrop,
                    DailyTaskDifficulty.Easy,
                    crop: crop,
                    item: item,
                    amount: 1));
                return;
            }

            list.Add(Task(
                ActionLabel(action) + " " + crop,
                ActionDescription(action, crop),
                DailyTaskKind.ApplyMaintenance,
                action == "RaisedBed" || action == "Trellis"
                    ? DailyTaskDifficulty.Medium
                    : DailyTaskDifficulty.Easy,
                crop: crop,
                item: item,
                maintenance: action,
                amount: 1));
        }

        private static void AddGeneralTasks(List<DailyTaskTemplate> list)
        {
            list.Add(Task("Prepare a planting spot",
                "Till ground with the shovel, then dig a planting hole, build a raised bed or open a furrow in it.",
                DailyTaskKind.DigPlantingSpot, DailyTaskDifficulty.Easy,
                item: "Shovel", amount: 1));
            list.Add(Task("Plant any crop",
                "Plant any planting material in prepared ground, or transplant a ready seedling from the Seedling Tent.",
                DailyTaskKind.PlantAnyCrop, DailyTaskDifficulty.Easy, amount: 1));
            list.Add(Task("Plant a fruit crop",
                "Plant one fruit-tree or tropical-fruit crop in prepared ground.",
                DailyTaskKind.PlantAnyCrop, DailyTaskDifficulty.Easy,
                item: "CoconutSeednut|BananaPlantlet|BananaSucker|DurianSeed|PomeloSeed|CacaoSeed|PineappleSucker|MangosteenSeed|MangoGraftedSeedling|MangoLiso",
                amount: 1));
            list.Add(Task("Plant a vegetable crop",
                "Plant one corn, eggplant, squash, strawberry or tomato crop in prepared ground.",
                DailyTaskKind.PlantAnyCrop, DailyTaskDifficulty.Easy,
                item: "CornSeed|EggplantSeed|SquashSeed|SquashSeedling|StrawberryRunner|TomatoSeed",
                amount: 1));
            list.Add(Task("Plant two crops",
                "Plant two crops in prepared ground during the current game day.",
                DailyTaskKind.PlantAnyCrop, DailyTaskDifficulty.Medium, amount: 2));
            list.Add(Task("Collect three crops",
                "Collect at least three harvested crop items.",
                DailyTaskKind.CollectHarvest, DailyTaskDifficulty.Easy, amount: 3));
            list.Add(Task("Sell a harvested crop",
                "Sell at least one harvested crop through the shipping bin.",
                DailyTaskKind.SellCropQuantity, DailyTaskDifficulty.Easy, amount: 1));
            list.Add(Task("Sell five crops",
                "Sell at least five harvested crop items through the shipping bin.",
                DailyTaskKind.SellCropQuantity, DailyTaskDifficulty.Medium, amount: 5));
            list.Add(Task("Help the driest crop",
                "Raise the driest crop's moisture to at least 60%.",
                DailyTaskKind.RaiseCropMoisture, DailyTaskDifficulty.Medium,
                amount: 1, threshold: 0.60f));
            list.Add(Task("Water a dry crop",
                "Water any crop whose moisture is below 40%.",
                DailyTaskKind.WaterCrop, DailyTaskDifficulty.Easy,
                amount: 1, threshold: 0.55f,
                eligibility: 0.40f));
            list.Add(Task("Complete a harvest",
                "Harvest any crop that is currently ready.",
                DailyTaskKind.HarvestCrop, DailyTaskDifficulty.Easy, amount: 1));
            list.Add(Task("Improve low fertility",
                "Apply organic compost to one crop with low fertility.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Medium,
                item: "OrganicCompostBag", maintenance: "OrganicCompost", amount: 1,
                eligibility: 0.55f));
            list.Add(Task("Protect the soil",
                "Apply mulch to one crop that does not already have mulch.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Easy,
                item: "MulchBag", maintenance: "Mulch", amount: 1));
            list.Add(Task("Reduce crop stress",
                "Prune one compatible crop with high stress.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Medium,
                item: "PruningShears", maintenance: "Prune", amount: 1,
                eligibility: 45f));
            list.Add(Task("Support a vulnerable crop",
                "Install a support stake or trellis on one compatible crop.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Medium,
                item: "SupportStakeKit|TrellisKit", maintenance: "SupportStakeOrTrellis", amount: 1));
        }

        private static void AddDroughtTasks(List<DailyTaskTemplate> list)
        {
            string[] crops =
            {
                "Coconut", "Banana", "Durian", "Pomelo", "Cacao",
                "Pineapple", "Mangosteen", "Mango", "Corn", "Eggplant",
                "Squash", "Strawberry", "Tomato"
            };
            foreach (string crop in crops)
            {
                list.Add(Task(
                    "Drought care: " + crop,
                    "Water one drought-stressed " + crop + " crop.",
                    DailyTaskKind.WaterCrop,
                    DailyTaskDifficulty.Medium,
                    crop: crop,
                    weather: "ExtremeDrought",
                    amount: 1,
                    threshold: 0.65f));
            }

            list.Add(Task("Water the two driest crops",
                "During the drought, water the two driest crops on the farm.",
                DailyTaskKind.WaterCrop, DailyTaskDifficulty.Hard,
                weather: "ExtremeDrought", amount: 2, threshold: 0.60f));
            list.Add(Task("Mulch the driest crop",
                "Apply mulch to the driest compatible crop during the drought.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Medium,
                item: "MulchBag", maintenance: "Mulch",
                weather: "ExtremeDrought", amount: 1));
            list.Add(Task("Mulch two drought-stressed crops",
                "Apply mulch to two drought-stressed compatible crops.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Hard,
                item: "MulchBag", maintenance: "Mulch",
                weather: "ExtremeDrought", amount: 2));
            list.Add(Task("Compost a drought-stressed crop",
                "Apply organic compost to a drought-stressed crop with low fertility.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Medium,
                item: "OrganicCompostBag", maintenance: "OrganicCompost",
                weather: "ExtremeDrought", amount: 1,
                eligibility: 0.60f));
            list.Add(WorldTask("Install irrigation",
                "Install an irrigation system near the active crop area.",
                "IrrigationSystemKit", "IrrigationSystem",
                "ExtremeDrought", DailyTaskDifficulty.Hard));
            list.Add(WorldTask("Store drought water",
                "Install a water-storage tank for drought preparation.",
                "WaterStorageTankKit", "WaterStorageTank",
                "ExtremeDrought", DailyTaskDifficulty.Hard));
            list.Add(WorldTask("Install shade protection",
                "Install a shade net near drought-sensitive crops.",
                "ShadeNetKit", "ShadeNet",
                "ExtremeDrought", DailyTaskDifficulty.Hard));
            list.Add(WorldTask("Install a greenhouse",
                "Install a greenhouse to protect drought-sensitive crops.",
                "GreenhouseKit", "Greenhouse",
                "ExtremeDrought", DailyTaskDifficulty.Hard));
            list.Add(Task("Mulch a young crop",
                "Apply mulch to one seedling or vegetative crop during the drought.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Medium,
                item: "MulchBag", maintenance: "Mulch",
                weather: "ExtremeDrought", amount: 1,
                stage: "Seedling|Vegetative|Young|Immature"));
            list.Add(Task("Mulch a fruiting crop",
                "Apply mulch to one fruiting crop during the drought.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Medium,
                item: "MulchBag", maintenance: "Mulch",
                weather: "ExtremeDrought", amount: 1,
                stage: "Fruiting"));
            list.Add(Task("Emergency drought watering",
                "Water up to three crops whose moisture is below 25%.",
                DailyTaskKind.WaterCrop, DailyTaskDifficulty.Hard,
                weather: "ExtremeDrought", amount: 3, threshold: 0.55f,
                eligibility: 0.25f));
            list.Add(Task("Restore safe moisture",
                "Raise one crop's moisture to at least 60% during the drought.",
                DailyTaskKind.RaiseCropMoisture, DailyTaskDifficulty.Medium,
                weather: "ExtremeDrought", amount: 1, threshold: 0.60f));
        }

        private static void AddTyphoonTasks(List<DailyTaskTemplate> list)
        {
            list.Add(WorldTask("Build a drainage canal",
                "Install one drainage canal near the crop area.",
                "DrainageCanalKit", "DrainageCanal",
                "Typhoon", DailyTaskDifficulty.Hard));
            list.Add(WorldTask("Install a windbreak",
                "Install one windbreak near exposed crops.",
                "WindbreakKit", "Windbreak",
                "Typhoon", DailyTaskDifficulty.Hard));
            list.Add(WorldTask("Protect crops with a greenhouse",
                "Install one greenhouse to protect vulnerable crops.",
                "GreenhouseKit", "Greenhouse",
                "Typhoon", DailyTaskDifficulty.Hard));

            string[] raisedBedCrops =
                { "Pineapple", "Tomato", "Strawberry", "Squash", "Eggplant" };
            foreach (string crop in raisedBedCrops)
            {
                list.Add(Task(
                    "Raise " + crop + " above floodwater",
                    "Install a raised bed under one vulnerable " + crop + " crop.",
                    DailyTaskKind.ApplyMaintenance,
                    DailyTaskDifficulty.Hard,
                    crop: crop,
                    item: "RaisedBedKit",
                    maintenance: "RaisedBed",
                    weather: "Typhoon",
                    amount: 1));
            }

            string[] stakeCrops =
                { "Banana", "Durian", "Pomelo", "Mango", "Tomato", "Eggplant" };
            foreach (string crop in stakeCrops)
            {
                list.Add(Task(
                    "Stake " + crop + " for wind",
                    "Install a support stake on one " + crop + " crop.",
                    DailyTaskKind.ApplyMaintenance,
                    DailyTaskDifficulty.Medium,
                    crop: crop,
                    item: "SupportStakeKit",
                    maintenance: "SupportStake",
                    weather: "Typhoon",
                    amount: 1));
            }

            list.Add(Task("Trellis squash for the storm",
                "Install a trellis on one squash crop.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Medium,
                crop: "Squash", item: "TrellisKit", maintenance: "Trellis",
                weather: "Typhoon", amount: 1));

            string[] pruneCrops =
            {
                "Banana", "Coconut", "Durian", "Pomelo",
                "Cacao", "Mangosteen", "Mango", "Tomato", "Eggplant"
            };
            foreach (string crop in pruneCrops)
            {
                list.Add(Task(
                    "Prune " + crop + " before stronger winds",
                    "Prune one " + crop + " crop during the typhoon.",
                    DailyTaskKind.ApplyMaintenance,
                    DailyTaskDifficulty.Medium,
                    crop: crop,
                    item: "PruningShears",
                    maintenance: "Prune",
                    weather: "Typhoon",
                    amount: 1));
            }

            list.Add(Task("Drain a waterlogged crop",
                "Install a drainage kit on one waterlogged or infected crop.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Hard,
                item: "DrainageKit", maintenance: "DrainageImprovement",
                weather: "Typhoon", amount: 1));
        }

        private static void AddPestDiseaseTasks(List<DailyTaskTemplate> list)
        {
            AddPest(list, "Tomato", "Aphids", "AphidTrap|NeemSoap");
            AddPest(list, "Tomato", "FruitWorm", "BtBioSpray|PheromoneTrap|FruitBag");
            AddPest(list, "Tomato", "SpottedLadyBeetle", "NeemSoap|Sanitation");
            AddPest(list, "Tomato", "BacterialWilt", "Disinfectant|DrainageImprovement|RemoveInfectedPlant");
            AddPest(list, "Tomato", "Anthracnose", "CopperFungicide|Sanitation|FruitBag");

            AddPest(list, "Squash", "SpottedLadyBeetle", "NeemSoap|Sanitation");
            AddPest(list, "Squash", "Aphids", "AphidTrap|NeemSoap");
            AddPest(list, "Squash", "MosaicVirus", "AphidTrap|NeemSoap|RemoveInfectedPlant");

            AddPest(list, "Eggplant", "SpottedLadyBeetle", "NeemSoap|Sanitation");
            AddPest(list, "Eggplant", "Aphids", "AphidTrap|NeemSoap");
            AddPest(list, "Eggplant", "FruitFly", "PheromoneTrap|FruitBag|Sanitation");
            AddPest(list, "Eggplant", "BacterialWilt", "Disinfectant|DrainageImprovement|RemoveInfectedPlant");

            AddPest(list, "Strawberry", "Aphids", "AphidTrap|NeemSoap");
            AddPest(list, "Strawberry", "StrawberryLeafSpot", "CopperFungicide|Sanitation");

            AddPest(list, "Durian", "FruitBorer", "BtBioSpray|PheromoneTrap|FruitBag");
            AddPest(list, "Durian", "MealyBug", "NeemSoap|Sanitation");
            AddPest(list, "Durian", "Phytophthora", "CopperFungicide|DrainageImprovement|Sanitation");
            AddPest(list, "Durian", "Anthracnose", "CopperFungicide|Sanitation|FruitBag");

            AddPest(list, "Cacao", "CacaoTermite", "TermiteBait|Sanitation");
            AddPest(list, "Cacao", "MealyBug", "NeemSoap|Sanitation");
            AddPest(list, "Cacao", "CacaoPodBorer", "BtBioSpray|PheromoneTrap|FruitBag");
            AddPest(list, "Cacao", "CacaoBlackPodRot", "CopperFungicide|Sanitation|FruitBag");

            AddPest(list, "Pineapple", "FruitBorer", "BtBioSpray|PheromoneTrap|FruitBag");
            AddPest(list, "Pineapple", "MealyBug", "NeemSoap|Sanitation");
            AddPest(list, "Pineapple", "PineappleCoreRot", "CopperFungicide|DrainageImprovement|Sanitation");
            AddPest(list, "Pineapple", "PineappleDieback", "DrainageImprovement|Sanitation");
            AddPest(list, "Pineapple", "RootRot", "CopperFungicide|DrainageImprovement|Sanitation");

            AddPest(list, "Pomelo", "CitrusFruitBorer", "BtBioSpray|PheromoneTrap|FruitBag");
            AddPest(list, "Pomelo", "Aphids", "AphidTrap|NeemSoap");
            AddPest(list, "Pomelo", "FruitFly", "PheromoneTrap|FruitBag|Sanitation");
            AddPest(list, "Pomelo", "Anthracnose", "CopperFungicide|Sanitation|FruitBag");

            AddPest(list, "Mango", "FlowerBeetle", "NeemSoap|Sanitation");
            AddPest(list, "Mango", "TwigBorer", "BtBioSpray|Sanitation");
            AddPest(list, "Mango", "MangoBlackSpot", "CopperFungicide|Sanitation|FruitBag");
            AddPest(list, "Mango", "Anthracnose", "CopperFungicide|Sanitation|FruitBag");

            AddPest(list, "Banana", "PanamaDisease", "Disinfectant|DrainageImprovement|RemoveInfectedPlant");
            AddPest(list, "Banana", "BugtokDisease", "FruitBag|Sanitation|Disinfectant");

            AddPest(list, "Coconut", "Brontispa", "NeemSoap|Sanitation");
            AddPest(list, "Coconut", "Phytophthora", "CopperFungicide|DrainageImprovement|Sanitation");

            AddPest(list, "Mangosteen", "Mites", "NeemSoap|Sanitation");
            AddPest(list, "Mangosteen", "MealyBug", "NeemSoap|Sanitation");
            AddPest(list, "Mangosteen", "Anthracnose", "CopperFungicide|Sanitation|FruitBag");

            AddPest(list, "Corn", "CornBorer", "BtBioSpray|PheromoneTrap|Sanitation");
            AddPest(list, "Corn", "CornEarworm", "BtBioSpray|PheromoneTrap");
            AddPest(list, "Corn", "FallArmyworm", "BtBioSpray|PheromoneTrap|Sanitation");
            AddPest(list, "Corn", "Gibberella", "CopperFungicide|Sanitation");
            AddPest(list, "Corn", "FusariumEarKernelRot", "CopperFungicide|Sanitation|BtBioSpray");
        }

        private static void AddPest(
            List<DailyTaskTemplate> list,
            string crop,
            string condition,
            string mitigations)
        {
            list.Add(Task(
                "Control " + FriendlyCondition(condition) + " on " + crop,
                "Apply a valid mitigation to reduce " +
                FriendlyCondition(condition) + " severity on " + crop +
                ". Valid options: " + mitigations.Replace("|", ", ") + ".",
                DailyTaskKind.MitigateCondition,
                DailyTaskDifficulty.Hard,
                crop: crop,
                item: mitigations,
                condition: condition,
                amount: 1,
                reduction: 5f,
                activeCondition: true));
        }

        private static void AddAdvancedTasks(List<DailyTaskTemplate> list)
        {
            list.Add(Task("Reduce an active pest",
                "Reduce the severity of any active pest by at least 10 percentage points.",
                DailyTaskKind.ReduceAnyPestSeverity, DailyTaskDifficulty.Hard,
                reduction: 10f, activeCondition: true));
            list.Add(Task("Reduce an active disease",
                "Reduce the severity of any active disease by at least 10 percentage points.",
                DailyTaskKind.ReduceAnyDiseaseSeverity, DailyTaskDifficulty.Hard,
                reduction: 10f, activeCondition: true));
            list.Add(Task("Treat two affected crops",
                "Successfully mitigate active conditions on two affected crops.",
                DailyTaskKind.MitigateCondition, DailyTaskDifficulty.Hard,
                amount: 2, reduction: 1f, activeCondition: true));
            list.Add(Task("Protect crops with an aphid trap",
                "Place an aphid trap near an active aphid outbreak.",
                DailyTaskKind.PlacePestTrap, DailyTaskDifficulty.Medium,
                item: "AphidTrap", condition: "Aphids", amount: 1,
                activeCondition: true));
            list.Add(Task("Clean a full aphid trap",
                "Clean one full aphid trap so it becomes active again.",
                DailyTaskKind.CleanTrap, DailyTaskDifficulty.Medium,
                item: "AphidTrap", amount: 1));
            list.Add(Task("Place a pheromone trap",
                "Place a pheromone trap near a compatible active pest.",
                DailyTaskKind.PlacePestTrap, DailyTaskDifficulty.Medium,
                item: "PheromoneTrap", amount: 1, activeCondition: true));
            list.Add(Task("Clean an area trap",
                "Clean one full pheromone or bait trap.",
                DailyTaskKind.CleanTrap, DailyTaskDifficulty.Medium,
                item: "PheromoneTrap|TermiteBaitStation", amount: 1));
            list.Add(Task("Protect cacao from termites",
                "Place a termite bait station near cacao affected by cacao termite.",
                DailyTaskKind.PlacePestTrap, DailyTaskDifficulty.Hard,
                crop: "Cacao", item: "TermiteBaitStation",
                condition: "CacaoTermite", amount: 1, activeCondition: true));
            list.Add(Task("Bag an affected fruit",
                "Install a fruit bag on one compatible affected fruiting crop.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Medium,
                item: "FruitBag", maintenance: "FruitBag",
                amount: 1, activeCondition: true));
            list.Add(Task("Apply copper fungicide twice",
                "Apply copper fungicide to two compatible fungal-disease cases.",
                DailyTaskKind.MitigateCondition, DailyTaskDifficulty.Hard,
                item: "CopperFungicide", amount: 2,
                reduction: 1f, activeCondition: true));
            list.Add(Task("Sanitize an infected crop",
                "Sanitize one infected crop with a compatible condition.",
                DailyTaskKind.MitigateCondition, DailyTaskDifficulty.Medium,
                item: "Sanitation", amount: 1,
                reduction: 1f, activeCondition: true));
            list.Add(Task("Sanitize two infected crops",
                "Sanitize two infected crops with compatible conditions.",
                DailyTaskKind.MitigateCondition, DailyTaskDifficulty.Hard,
                item: "Sanitation", amount: 2,
                reduction: 1f, activeCondition: true));
            list.Add(Task("Improve infected-crop drainage",
                "Improve drainage on one waterlogged or infected crop.",
                DailyTaskKind.ApplyMaintenance, DailyTaskDifficulty.Hard,
                item: "DrainageKit", maintenance: "DrainageImprovement",
                amount: 1, activeCondition: true));
            list.Add(Task("Disinfect an affected crop",
                "Apply disinfectant to one compatible bacterial or soil-borne disease.",
                DailyTaskKind.MitigateCondition, DailyTaskDifficulty.Hard,
                item: "Disinfectant", amount: 1,
                reduction: 1f, activeCondition: true));
            list.Add(Task("Remove mosaic-infected squash",
                "Remove one severely infected squash plant with mosaic virus.",
                DailyTaskKind.RemoveInfectedPlant, DailyTaskDifficulty.Hard,
                crop: "Squash", condition: "MosaicVirus",
                threshold: 55f, activeCondition: true));
            list.Add(Task("Remove wilted tomato",
                "Remove one severely infected tomato plant with bacterial wilt.",
                DailyTaskKind.RemoveInfectedPlant, DailyTaskDifficulty.Hard,
                crop: "Tomato", condition: "BacterialWilt",
                threshold: 55f, activeCondition: true));
            list.Add(Task("Remove wilted eggplant",
                "Remove one severely infected eggplant with bacterial wilt.",
                DailyTaskKind.RemoveInfectedPlant, DailyTaskDifficulty.Hard,
                crop: "Eggplant", condition: "BacterialWilt",
                threshold: 55f, activeCondition: true));
            list.Add(Task("Remove Panama-diseased banana",
                "Remove one severely infected banana plant with Panama disease.",
                DailyTaskKind.RemoveInfectedPlant, DailyTaskDifficulty.Hard,
                crop: "Banana", condition: "PanamaDisease",
                threshold: 55f, activeCondition: true));
            list.Add(Task("Treat the most severe condition",
                "Apply a valid mitigation to the crop with the highest active severity.",
                DailyTaskKind.MitigateCondition, DailyTaskDifficulty.Hard,
                amount: 1, reduction: 5f, activeCondition: true));
            list.Add(Task("Place a protective area trap",
                "Place a compatible area trap near an infected crop.",
                DailyTaskKind.PlacePestTrap, DailyTaskDifficulty.Hard,
                item: "PheromoneTrap|TermiteBaitStation|AphidTrap",
                amount: 1, activeCondition: true));
            list.Add(Task("Reduce total farm severity",
                "Reduce the farm's combined pest-and-disease severity by at least 20 percentage points.",
                DailyTaskKind.ReduceFarmConditionSeverity, DailyTaskDifficulty.Hard,
                reduction: 20f, activeCondition: true));
            list.Add(Task("Control an urgent outbreak",
                "Reduce one urgent active pest or disease condition below 25% severity.",
                DailyTaskKind.MitigateCondition, DailyTaskDifficulty.Hard,
                threshold: 25f, reduction: 1f, activeCondition: true));
            list.Add(Task("Stabilize affected crops",
                "Lower average stress among affected crops by at least 5 points.",
                DailyTaskKind.LowerAverageStress, DailyTaskDifficulty.Hard,
                reduction: 5f, activeCondition: true));
        }

        /// <summary>
        /// The planting system's own jobs: working ground, and the Seedling Tent.
        /// A seed sown today cannot be transplanted before the day ends, so sowing
        /// and transplanting are separate tasks rather than one "raise a seedling".
        /// </summary>
        private static void AddNurseryTasks(List<DailyTaskTemplate> list)
        {
            list.Add(Task("Sow a seedling bag",
                "In the Seedling Tent, fill a bag with soil and sow a seed, a banana plantlet or a grafted mango seedling in it.",
                DailyTaskKind.SowSeedlingBag, DailyTaskDifficulty.Easy, amount: 1));
            list.Add(Task("Sow two seedling bags",
                "Sow two seedling bags in the Seedling Tent during the current game day.",
                DailyTaskKind.SowSeedlingBag, DailyTaskDifficulty.Medium, amount: 2));
            list.Add(Task("Transplant a seedling",
                "Move one ready seedling from the Seedling Tent into prepared ground.",
                DailyTaskKind.TransplantSeedling, DailyTaskDifficulty.Medium, amount: 1));
            list.Add(Task("Till new ground",
                "Till one new patch of ground with the shovel.",
                DailyTaskKind.TillGround, DailyTaskDifficulty.Easy,
                item: "Shovel", amount: 1));
        }

        private static DailyTaskTemplate WorldTask(
            string title,
            string description,
            string item,
            string action,
            string weather,
            DailyTaskDifficulty difficulty)
        {
            return Task(
                title, description,
                DailyTaskKind.PlaceWorldMitigation,
                difficulty,
                item: item,
                maintenance: action,
                weather: weather,
                amount: 1);
        }

        private static DailyTaskTemplate Task(
            string title,
            string description,
            DailyTaskKind kind,
            DailyTaskDifficulty difficulty,
            string crop = "",
            string item = "",
            string maintenance = "",
            string weather = "",
            string condition = "",
            int amount = 1,
            int value = 0,
            float threshold = 0f,
            float reduction = 0f,
            bool activeCondition = false,
            string stage = "",
            float eligibility = 0f)
        {
            return new DailyTaskTemplate
            {
                title = title,
                description = description,
                kind = kind,
                difficulty = difficulty,
                cropType = crop,
                itemType = item,
                maintenanceAction = maintenance,
                requiredWeather = weather,
                conditionType = condition,
                requiredStage = stage,
                eligibilityThreshold = eligibility,
                targetAmount = Math.Max(1, amount),
                targetValue = Math.Max(0, value),
                targetThreshold = threshold,
                requiredReduction = reduction,
                requiresActiveCondition = activeCondition
            };
        }

        private static string ActionLabel(string action)
        {
            switch (action)
            {
                case "Mulch": return "Mulch";
                case "OrganicCompost": return "Compost";
                case "Prune": return "Prune";
                case "SupportStake": return "Stake";
                case "Trellis": return "Trellis";
                case "RaisedBed": return "Raise";
                default: return action;
            }
        }

        private static string ActionDescription(string action, string crop)
        {
            switch (action)
            {
                case "Mulch":
                    return "Apply mulch to one " + crop + " crop.";
                case "OrganicCompost":
                    return "Apply organic compost to one " + crop + " crop.";
                case "Prune":
                    return "Prune one compatible " + crop + " crop.";
                case "SupportStake":
                    return "Install a support stake on one " + crop + " crop.";
                case "Trellis":
                    return "Install a trellis on one " + crop + " crop.";
                case "RaisedBed":
                    return "Install a raised bed under one " + crop + " crop.";
                default:
                    return action + " one " + crop + " crop.";
            }
        }

        private static string FriendlyCondition(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "condition";
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current) &&
                    !char.IsUpper(value[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(current);
            }
            return result.ToString();
        }
    }
}
