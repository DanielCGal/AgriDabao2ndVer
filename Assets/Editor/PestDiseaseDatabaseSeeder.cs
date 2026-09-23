using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AgriDabao3D
{
    public static class PestDiseaseDatabaseSeeder
    {
        private static readonly int[] AllMonths =
        {
            1, 2, 3, 4, 5, 6,
            7, 8, 9, 10, 11, 12
        };

        [MenuItem(
            "Tools/AgriDabao/Populate Selected Pest Disease Database"
        )]
        private static void PopulateSelectedDatabase()
        {
            PestDiseaseDatabase database =
                Selection.activeObject as PestDiseaseDatabase;

            if (database == null)
            {
                EditorUtility.DisplayDialog(
                    "No database selected",
                    "Select DavaoPestDiseaseDatabase in the Project window first.",
                    "OK"
                );

                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Populate pest database?",
                "This will remove the current rules and replace them with all 47 configured rules.",
                "Populate",
                "Cancel"
            );

            if (!confirmed)
                return;

            Undo.RecordObject(
                database,
                "Populate Pest Disease Database"
            );

            if (database.rules == null)
            {
                database.rules =
                    new List<PestDiseaseRule>();
            }

            database.rules.Clear();

            AddTomatoRules(database);
            AddSquashRules(database);
            AddEggplantRules(database);
            AddStrawberryRules(database);
            AddDurianRules(database);
            AddCacaoRules(database);
            AddPineappleRules(database);
            AddPomeloRules(database);
            AddMangoRules(database);
            AddBananaRules(database);
            AddCoconutRules(database);
            AddMangosteenRules(database);
            AddCornRules(database);

            ValidateGeneratedRules(database);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Database populated",
                $"Created {database.rules.Count} rules.",
                "OK"
            );

            Debug.Log(
                $"[PestDatabaseSeeder] Populated " +
                $"{database.name} with " +
                $"{database.rules.Count} rules."
            );
        }

        [MenuItem(
            "Tools/AgriDabao/Populate Selected Pest Disease Database",
            true
        )]
        private static bool ValidatePopulateSelectedDatabase()
        {
            return Selection.activeObject
                is PestDiseaseDatabase;
        }

        private static void ValidateGeneratedRules(
            PestDiseaseDatabase database)
        {
            if (database.rules.Count != 47)
            {
                throw new System.InvalidOperationException(
                    $"Expected 47 pest/disease rules, but generated {database.rules.Count}."
                );
            }

            foreach (PestDiseaseRule rule in database.rules)
            {
                if (rule == null)
                {
                    throw new System.InvalidOperationException(
                        "The generated database contains a null pest/disease rule."
                    );
                }

                CropDevelopmentStage minimum =
                    rule.minimumStage == CropDevelopmentStage.Any
                        ? CropDevelopmentStage.Seedling
                        : rule.minimumStage;

                CropDevelopmentStage maximum =
                    rule.maximumStage == CropDevelopmentStage.Any
                        ? CropDevelopmentStage.Old
                        : rule.maximumStage;

                if (minimum > maximum)
                {
                    throw new System.InvalidOperationException(
                        $"Invalid crop-stage range for {rule.affectedCrop} / " +
                        $"{rule.displayName}: {rule.minimumStage} to {rule.maximumStage}."
                    );
                }
            }
        }

        private static void AddTomatoRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Tomato,
                PestDiseaseType.Aphids,
                "Aphids",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.PreFruiting,
                new[] { 3, 4, 5, 9, 10, 11 },
                20f,
                30f,
                0f,
                0.06f,
                wet: false,
                dry: true,
                waterlogged: false,
                stressed: true,
                PestDiseaseMitigation.AphidTrap,
                PestDiseaseMitigation.NeemSoap
            );

            AddRule(
                database,
                FarmCropType.Tomato,
                PestDiseaseType.FruitWorm,
                "Tomato Fruit Worm",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 12, 1, 2, 3 },
                24f,
                32f,
                0f,
                0.05f,
                false,
                false,
                false,
                true,
                PestDiseaseMitigation.BtBioSpray,
                PestDiseaseMitigation.PheromoneTrap,
                PestDiseaseMitigation.FruitBag
            );

            AddRule(
                database,
                FarmCropType.Tomato,
                PestDiseaseType.SpottedLadyBeetle,
                "28-Spotted Lady Beetle",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 8, 9, 10, 11 },
                20f,
                35f,
                75f,
                0.05f,
                true,
                false,
                false,
                false,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Tomato,
                PestDiseaseType.BacterialWilt,
                "Bacterial Wilt",
                PestDiseaseCategory.BacterialDisease,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Old,
                new[] { 3, 4, 5 },
                29f,
                35f,
                70f,
                0.035f,
                true,
                false,
                true,
                true,
                PestDiseaseMitigation.Disinfectant,
                PestDiseaseMitigation.DrainageImprovement,
                PestDiseaseMitigation.RemoveInfectedPlant
            );

            AddRule(
                database,
                FarmCropType.Tomato,
                PestDiseaseType.Anthracnose,
                "Anthracnose",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 6, 7, 8, 9, 10, 11 },
                20f,
                35f,
                80f,
                0.04f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.Sanitation,
                PestDiseaseMitigation.FruitBag
            );
        }

        private static void AddSquashRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Squash,
                PestDiseaseType.SpottedLadyBeetle,
                "28-Spotted Lady Beetle",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 8, 9, 10, 11 },
                20f,
                35f,
                75f,
                0.05f,
                true,
                false,
                false,
                false,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Squash,
                PestDiseaseType.Aphids,
                "Aphids",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.PreFruiting,
                new[] { 3, 4, 5, 9, 10, 11 },
                20f,
                30f,
                0f,
                0.06f,
                false,
                true,
                false,
                true,
                PestDiseaseMitigation.AphidTrap,
                PestDiseaseMitigation.NeemSoap
            );

            AddRule(
                database,
                FarmCropType.Squash,
                PestDiseaseType.MosaicVirus,
                "Squash Mosaic Virus",
                PestDiseaseCategory.ViralDisease,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 1, 2, 3, 4, 5, 9, 10, 11 },
                25f,
                32f,
                0f,
                0.03f,
                false,
                false,
                false,
                true,
                PestDiseaseMitigation.AphidTrap,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.RemoveInfectedPlant
            );
        }

        private static void AddEggplantRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Eggplant,
                PestDiseaseType.SpottedLadyBeetle,
                "28-Spotted Lady Beetle",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 8, 9, 10, 11 },
                20f,
                35f,
                75f,
                0.05f,
                true,
                false,
                false,
                false,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Eggplant,
                PestDiseaseType.Aphids,
                "Aphids",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.PreFruiting,
                new[] { 3, 4, 5, 9, 10, 11 },
                20f,
                30f,
                0f,
                0.06f,
                false,
                true,
                false,
                true,
                PestDiseaseMitigation.AphidTrap,
                PestDiseaseMitigation.NeemSoap
            );

            AddRule(
                database,
                FarmCropType.Eggplant,
                PestDiseaseType.FruitFly,
                "Fruit Fly",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                25f,
                32f,
                75f,
                0.05f,
                true,
                false,
                false,
                false,
                PestDiseaseMitigation.PheromoneTrap,
                PestDiseaseMitigation.FruitBag,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Eggplant,
                PestDiseaseType.BacterialWilt,
                "Bacterial Wilt",
                PestDiseaseCategory.BacterialDisease,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Old,
                new[] { 3, 4, 5 },
                25f,
                35f,
                65f,
                0.035f,
                true,
                false,
                true,
                true,
                PestDiseaseMitigation.Disinfectant,
                PestDiseaseMitigation.DrainageImprovement,
                PestDiseaseMitigation.RemoveInfectedPlant
            );
        }

        private static void AddStrawberryRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Strawberry,
                PestDiseaseType.Aphids,
                "Aphids",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 3, 4, 5, 9, 10, 11 },
                15f,
                25f,
                0f,
                0.05f,
                false,
                true,
                false,
                true,
                PestDiseaseMitigation.AphidTrap,
                PestDiseaseMitigation.NeemSoap
            );

            AddRule(
                database,
                FarmCropType.Strawberry,
                PestDiseaseType.StrawberryLeafSpot,
                "Strawberry Leaf Spot",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                15f,
                25f,
                80f,
                0.04f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.Sanitation
            );
        }

        private static void AddDurianRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Durian,
                PestDiseaseType.FruitBorer,
                "Durian Fruit Borer",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 6, 7, 8, 9, 11, 12, 1 },
                25f,
                32f,
                0f,
                0.04f,
                false,
                false,
                false,
                false,
                PestDiseaseMitigation.BtBioSpray,
                PestDiseaseMitigation.PheromoneTrap,
                PestDiseaseMitigation.FruitBag
            );

            AddRule(
                database,
                FarmCropType.Durian,
                PestDiseaseType.MealyBug,
                "Mealy Bug",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 1, 2, 3, 4, 5, 6 },
                25f,
                30f,
                0f,
                0.05f,
                false,
                true,
                false,
                true,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Durian,
                PestDiseaseType.Phytophthora,
                "Phytophthora",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Seedling,
                CropDevelopmentStage.Old,
                new[] { 5, 6, 7, 8, 9, 10 },
                25f,
                30f,
                80f,
                0.035f,
                true,
                false,
                true,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.DrainageImprovement,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Durian,
                PestDiseaseType.Anthracnose,
                "Anthracnose",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 6, 7, 8, 9, 10, 11 },
                20f,
                35f,
                75f,
                0.04f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.Sanitation,
                PestDiseaseMitigation.FruitBag
            );
        }

        private static void AddCacaoRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Cacao,
                PestDiseaseType.CacaoTermite,
                "Cacao Termite",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Seedling,
                CropDevelopmentStage.Vegetative,
                new[] { 6, 7, 8, 9, 10, 11, 12, 1 },
                10f,
                40f,
                0f,
                0.04f,
                false,
                false,
                true,
                true,
                PestDiseaseMitigation.TermiteBait,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Cacao,
                PestDiseaseType.MealyBug,
                "Mealy Bug",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 1, 2, 3, 4, 5, 6 },
                25f,
                30f,
                0f,
                0.05f,
                false,
                true,
                false,
                true,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Cacao,
                PestDiseaseType.CacaoPodBorer,
                "Cacao Pod Borer",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                25f,
                32f,
                0f,
                0.04f,
                false,
                false,
                false,
                false,
                PestDiseaseMitigation.BtBioSpray,
                PestDiseaseMitigation.PheromoneTrap,
                PestDiseaseMitigation.FruitBag
            );

            AddRule(
                database,
                FarmCropType.Cacao,
                PestDiseaseType.CacaoBlackPodRot,
                "Cacao Black Pod Rot",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                20f,
                35f,
                80f,
                0.04f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.Sanitation,
                PestDiseaseMitigation.FruitBag
            );
        }

        private static void AddPineappleRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Pineapple,
                PestDiseaseType.FruitBorer,
                "Pineapple Fruit Borer",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                AllMonths,
                25f,
                32f,
                0f,
                0.04f,
                false,
                false,
                false,
                false,
                PestDiseaseMitigation.BtBioSpray,
                PestDiseaseMitigation.PheromoneTrap,
                PestDiseaseMitigation.FruitBag
            );

            AddRule(
                database,
                FarmCropType.Pineapple,
                PestDiseaseType.MealyBug,
                "Mealy Bug",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 1, 2, 3, 4, 5, 6 },
                20f,
                35f,
                0f,
                0.05f,
                false,
                true,
                false,
                true,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Pineapple,
                PestDiseaseType.PineappleCoreRot,
                "Pineapple Core Rot",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.PreFruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                10f,
                40f,
                75f,
                0.035f,
                true,
                false,
                true,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.DrainageImprovement,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Pineapple,
                PestDiseaseType.PineappleDieback,
                "Pineapple Dieback",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 6, 7, 8, 9, 10 },
                10f,
                40f,
                80f,
                0.03f,
                true,
                false,
                true,
                true,
                PestDiseaseMitigation.DrainageImprovement,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Pineapple,
                PestDiseaseType.RootRot,
                "Pineapple Root Rot",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Seedling,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                10f,
                40f,
                75f,
                0.035f,
                true,
                false,
                true,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.DrainageImprovement,
                PestDiseaseMitigation.Sanitation
            );
        }

        private static void AddPomeloRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Pomelo,
                PestDiseaseType.CitrusFruitBorer,
                "Citrus Fruit Borer",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.PreFruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 8, 9, 10, 11, 12, 1, 2 },
                25f,
                32f,
                0f,
                0.04f,
                false,
                false,
                false,
                false,
                PestDiseaseMitigation.BtBioSpray,
                PestDiseaseMitigation.PheromoneTrap,
                PestDiseaseMitigation.FruitBag
            );

            AddRule(
                database,
                FarmCropType.Pomelo,
                PestDiseaseType.Aphids,
                "Aphids",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.PreFruiting,
                new[] { 3, 4, 5, 9, 10, 11 },
                20f,
                30f,
                0f,
                0.05f,
                false,
                true,
                false,
                true,
                PestDiseaseMitigation.AphidTrap,
                PestDiseaseMitigation.NeemSoap
            );

            AddRule(
                database,
                FarmCropType.Pomelo,
                PestDiseaseType.FruitFly,
                "Fruit Fly",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                25f,
                32f,
                0f,
                0.05f,
                false,
                false,
                false,
                false,
                PestDiseaseMitigation.PheromoneTrap,
                PestDiseaseMitigation.FruitBag,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Pomelo,
                PestDiseaseType.Anthracnose,
                "Anthracnose",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 6, 7, 8, 9, 10, 11 },
                20f,
                35f,
                75f,
                0.04f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.Sanitation,
                PestDiseaseMitigation.FruitBag
            );
        }

        private static void AddMangoRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Mango,
                PestDiseaseType.FlowerBeetle,
                "Mango Flower Beetle",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.PreFruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 12, 1, 2, 3, 4, 5 },
                25f,
                32f,
                0f,
                0.04f,
                false,
                false,
                false,
                false,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Mango,
                PestDiseaseType.TwigBorer,
                "Mango Twig Borer",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.PreFruiting,
                new[] { 7, 8, 9, 10 },
                10f,
                40f,
                0f,
                0.04f,
                false,
                false,
                false,
                true,
                PestDiseaseMitigation.BtBioSpray,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Mango,
                PestDiseaseType.MangoBlackSpot,
                "Mango Black Spot",
                PestDiseaseCategory.BacterialDisease,
                CropDevelopmentStage.PreFruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                25f,
                30f,
                75f,
                0.035f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.Sanitation,
                PestDiseaseMitigation.FruitBag
            );

            AddRule(
                database,
                FarmCropType.Mango,
                PestDiseaseType.Anthracnose,
                "Anthracnose",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 6, 7, 8, 9, 10, 11 },
                20f,
                35f,
                80f,
                0.04f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.Sanitation,
                PestDiseaseMitigation.FruitBag
            );
        }

        private static void AddBananaRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Banana,
                PestDiseaseType.PanamaDisease,
                "Panama Disease",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Seedling,
                CropDevelopmentStage.Old,
                new[] { 5, 6, 7, 8, 9, 10 },
                10f,
                40f,
                0f,
                0.025f,
                true,
                false,
                true,
                true,
                PestDiseaseMitigation.Disinfectant,
                PestDiseaseMitigation.DrainageImprovement,
                PestDiseaseMitigation.RemoveInfectedPlant
            );

            PestDiseaseRule panamaRule =
                database.rules[database.rules.Count - 1];

            panamaRule.canAppearOutsidePeakMonths = true;
            panamaRule.offSeasonRiskMultiplier = 0.35f;

            AddRule(
                database,
                FarmCropType.Banana,
                PestDiseaseType.BugtokDisease,
                "Banana Bugtok",
                PestDiseaseCategory.BacterialDisease,
                CropDevelopmentStage.PreFruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                10f,
                40f,
                80f,
                0.03f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.FruitBag,
                PestDiseaseMitigation.Sanitation,
                PestDiseaseMitigation.Disinfectant
            );
        }

        private static void AddCoconutRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Coconut,
                PestDiseaseType.Brontispa,
                "Coconut Brontispa",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Seedling,
                CropDevelopmentStage.Vegetative,
                new[] { 1, 2, 3, 4 },
                20f,
                35f,
                0f,
                0.05f,
                false,
                true,
                false,
                true,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Coconut,
                PestDiseaseType.Phytophthora,
                "Coconut Phytophthora",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Seedling,
                CropDevelopmentStage.Old,
                new[] { 5, 6, 7, 8, 9, 10 },
                25f,
                30f,
                80f,
                0.03f,
                true,
                false,
                true,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.DrainageImprovement,
                PestDiseaseMitigation.Sanitation
            );
        }

        private static void AddMangosteenRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Mangosteen,
                PestDiseaseType.Mites,
                "Mites",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 1, 2, 3, 4 },
                25f,
                35f,
                0f,
                0.05f,
                false,
                true,
                false,
                true,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Mangosteen,
                PestDiseaseType.MealyBug,
                "Mealy Bug",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.Fruiting,
                new[] { 1, 2, 3, 4, 5, 6 },
                20f,
                35f,
                0f,
                0.05f,
                false,
                true,
                false,
                true,
                PestDiseaseMitigation.NeemSoap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Mangosteen,
                PestDiseaseType.Anthracnose,
                "Anthracnose",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 6, 7, 8, 9, 10, 11 },
                20f,
                35f,
                75f,
                0.04f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.Sanitation,
                PestDiseaseMitigation.FruitBag
            );
        }

        private static void AddCornRules(
            PestDiseaseDatabase database)
        {
            AddRule(
                database,
                FarmCropType.Corn,
                PestDiseaseType.CornBorer,
                "Corn Borer",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Vegetative,
                CropDevelopmentStage.PreFruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                25f,
                30f,
                0f,
                0.06f,
                false,
                false,
                false,
                true,
                PestDiseaseMitigation.BtBioSpray,
                PestDiseaseMitigation.PheromoneTrap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Corn,
                PestDiseaseType.CornEarworm,
                "Corn Earworm",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.PreFruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                25f,
                32f,
                0f,
                0.05f,
                false,
                false,
                false,
                true,
                PestDiseaseMitigation.BtBioSpray,
                PestDiseaseMitigation.PheromoneTrap
            );

            AddRule(
                database,
                FarmCropType.Corn,
                PestDiseaseType.FallArmyworm,
                "Fall Armyworm",
                PestDiseaseCategory.Pest,
                CropDevelopmentStage.Seedling,
                CropDevelopmentStage.Vegetative,
                new[] { 5, 6, 7, 8, 9, 10 },
                25f,
                30f,
                0f,
                0.07f,
                false,
                false,
                false,
                true,
                PestDiseaseMitigation.BtBioSpray,
                PestDiseaseMitigation.PheromoneTrap,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Corn,
                PestDiseaseType.Gibberella,
                "Gibberella Ear Rot",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.PreFruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                24f,
                30f,
                80f,
                0.035f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.Sanitation
            );

            AddRule(
                database,
                FarmCropType.Corn,
                PestDiseaseType.FusariumEarKernelRot,
                "Fusarium Ear/Kernel Rot",
                PestDiseaseCategory.FungalDisease,
                CropDevelopmentStage.Fruiting,
                CropDevelopmentStage.Fruiting,
                new[] { 5, 6, 7, 8, 9, 10 },
                25f,
                32f,
                75f,
                0.035f,
                true,
                false,
                false,
                true,
                PestDiseaseMitigation.CopperFungicide,
                PestDiseaseMitigation.Sanitation,
                PestDiseaseMitigation.BtBioSpray
            );
        }

        private static void AddRule(
            PestDiseaseDatabase database,
            FarmCropType crop,
            PestDiseaseType type,
            string displayName,
            PestDiseaseCategory category,
            CropDevelopmentStage minimumStage,
            CropDevelopmentStage maximumStage,
            int[] peakMonths,
            float minimumTemperature,
            float maximumTemperature,
            float minimumHumidity,
            float dailyChance,
            bool wet,
            bool dry,
            bool waterlogged,
            bool stressed,
            params PestDiseaseMitigation[] mitigations)
        {
            float severityRise =
                category == PestDiseaseCategory.Pest
                    ? 7f
                    : 6f;

            float healthDamage =
                category switch
                {
                    PestDiseaseCategory.Pest => 1.5f,
                    PestDiseaseCategory.ViralDisease => 4f,
                    PestDiseaseCategory.BacterialDisease => 3.5f,
                    PestDiseaseCategory.FungalDisease => 3f,
                    _ => 2f
                };

            float stressAdded =
                category == PestDiseaseCategory.Pest
                    ? 3f
                    : 5f;

            float spreadChance =
                category switch
                {
                    PestDiseaseCategory.Pest => 0.05f,
                    PestDiseaseCategory.ViralDisease => 0.04f,
                    PestDiseaseCategory.BacterialDisease => 0.04f,
                    PestDiseaseCategory.FungalDisease => 0.035f,
                    _ => 0.03f
                };

            PestDiseaseRule rule =
                new PestDiseaseRule
                {
                    type = type,
                    displayName = displayName,
                    category = category,
                    affectedCrop = crop,

                    peakMonths =
                        new List<int>(peakMonths),

                    canAppearOutsidePeakMonths = false,
                    offSeasonRiskMultiplier = 0.15f,

                    minimumStage = minimumStage,
                    maximumStage = maximumStage,

                    minimumTemperatureC =
                        minimumTemperature,

                    maximumTemperatureC =
                        maximumTemperature,

                    minimumHumidity =
                        minimumHumidity,

                    prefersWetWeather = wet,
                    prefersDryWeather = dry,

                    prefersWaterloggedSoil =
                        waterlogged,

                    prefersStressedPlants =
                        stressed,

                    baseDailyAppearanceChance =
                        dailyChance,

                    severityRisePerGameDay =
                        severityRise,

                    healthDamagePerGameDay =
                        healthDamage,

                    stressAddedPerGameDay =
                        stressAdded,

                    spreadRadius = 8f,

                    spreadChancePerDay =
                        spreadChance,

                    mitigations =
                        new List<PestDiseaseMitigation>(
                            mitigations
                        )
                };

            database.rules.Add(rule);
        }
    }
}