using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public partial class FarmingInteractionSystem : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        public TemporaryTerrainGenerator terrainGenerator;
        public SoilAwareTerrainGenerator soilAwareTerrain;

        [Header("Original Prefabs")]
        public GameObject coconutTreePrefab;
        public GameObject coconutFruitPrefab;
        public GameObject bananaTreePrefab;
        public GameObject bananaFruitPrefab;

        [Header("New Tropical Crop Prefabs")]
        public GameObject durianTreePrefab;
        public GameObject durianFruitPrefab;
        public GameObject pomeloTreePrefab;
        public GameObject pomeloFruitPrefab;
        public GameObject cacaoTreePrefab;
        public GameObject cacaoFruitPrefab;
        public GameObject pineappleTreePrefab;
        public GameObject pineappleFruitPrefab;
        public GameObject mangosteenTreePrefab;
        public GameObject mangosteenFruitPrefab;
        public GameObject mangoTreePrefab;
        public GameObject mangoFruitPrefab;

        [Header("New Vegetable Crop Prefabs")]
        public GameObject cornTreePrefab;
        public GameObject cornFruitPrefab;

        public GameObject eggplantTreePrefab;
        public GameObject eggplantFruitPrefab;

        public GameObject squashTreePrefab;
        public GameObject squashFruitPrefab;

        public GameObject strawberryTreePrefab;
        public GameObject strawberryFruitPrefab;

        public GameObject tomatoTreePrefab;
        public GameObject tomatoFruitPrefab;



        [Header("Pest Disease Placeable Prefabs")]
        public GameObject aphidTrapPrefab;
        public GameObject pheromoneTrapPrefab;
        public GameObject termiteBaitStationPrefab;

        [Header("Climate Maintenance Crop Visual Prefabs")]
        [Tooltip("Visual spawned as a child at the base of a crop after Mulch is installed.")]
        public GameObject mulchVisualPrefab;
        [Tooltip("Visual spawned as a child beside a compatible crop after a Support Stake is installed.")]
        public GameObject supportStakePrefab;
        [Tooltip("Visual spawned as a child beside a Squash crop after a Trellis is installed.")]
        public GameObject trellisPrefab;

        [Header("Climate Mitigation Placeable Prefabs")]
        public GameObject irrigationSystemPrefab;
        public GameObject waterStorageTankPrefab;
        public GameObject shadeNetPrefab;
        public GameObject windbreakPrefab;
        public GameObject greenhousePrefab;
        public GameObject drainageCanalPrefab;

        [Header("Climate Mitigation Placement Offsets")]
        [Tooltip("Nudge applied after the prefab bottom is snapped to the terrain.\n\n" +
                 "Y raises or lowers the model - use a negative value to sink it into the " +
                 "ground. X and Z slide it sideways and forward, in the object's OWN local " +
                 "space, so the nudge follows the model when it is rotated.\n\n" +
                 "These are live in the Inspector: enter Play mode, place the item, then " +
                 "tweak the numbers and place another to compare.")]
        public Vector3 irrigationSystemPlacementOffset = Vector3.zero;
        public Vector3 waterStorageTankPlacementOffset = Vector3.zero;
        public Vector3 shadeNetPlacementOffset = Vector3.zero;
        public Vector3 windbreakPlacementOffset = Vector3.zero;
        public Vector3 greenhousePlacementOffset = Vector3.zero;
        public Vector3 drainageCanalPlacementOffset = new Vector3(0f, -0.25f, 0f);

        [Header("Optional Crop Protection Visuals")]
        public GameObject fruitBagPrefab;

        [Header("Digging")]
        public GameObject digSpotPrefab;

        [Tooltip("Small vertical adjustment for the dirt prefab.")]
        public float digSpotGroundOffset = 0.02f;

        [Tooltip("Minimum distance allowed between dug planting spots.")]
        public float minDistanceBetweenDigSpots = 6f;

        [Tooltip("How far from the player any farming action may be performed, in " +
                 "metres - digging, planting, watering, spraying, harvesting, " +
                 "collecting, selling, and placing traps or structures. The tap " +
                 "ray itself reaches across the whole map, so without this the " +
                 "player can act on ground and crops they are nowhere near - " +
                 "including by accident, from a small thumb slide that never " +
                 "crosses the look-drag threshold. Roughly a few steps ahead is " +
                 "enough to farm comfortably while still requiring the player to " +
                 "walk to the crop they want to tend. Inspecting is deliberately " +
                 "not limited: reading a crop's status from a distance harms " +
                 "nothing. Set to 0 to disable the limit entirely.")]
        public float maxInteractDistance = 5f;

        [Header("Mobile Tap Targets")]
        [Tooltip("Radius, in metres, of the invisible tap target placed on every crop. " +
                 "The stage prefabs carry colliders auto-fitted to their meshes, and a " +
                 "seedling's mesh is only a few centimetres across, which is far smaller " +
                 "than a fingertip on a phone screen. Crops sit at least " +
                 "minDistanceBetweenTrees apart, so this can be generous without one " +
                 "plant ever stealing taps from its neighbour. Kept modest so that the " +
                 "ring of ground it covers - where a tap reaches the plant rather than " +
                 "the soil, which matters when placing a trap beside a crop - stays " +
                 "small. Set to 0 to disable.")]
        public float cropTapRadius = 0.3f;

        [Tooltip("Height, in metres, of that same tap target, measured up from the " +
                 "crop's base. Tall enough that a seedling can be tapped without " +
                 "aiming at the ground, short enough not to cover a grown tree's canopy.")]
        public float cropTapHeight = 1.6f;

        [Header("Growth Sprout / Seedling Prefabs")]
        public GameObject coconutSproutPrefab;
        public GameObject bananaSproutPrefab;
        public GameObject durianSproutPrefab;
        public GameObject pomeloSproutPrefab;
        public GameObject cacaoSproutPrefab;
        public GameObject pineappleSproutPrefab;
        public GameObject mangosteenSproutPrefab;
        public GameObject mangoSproutPrefab;
        public GameObject cornSproutPrefab;
        public GameObject eggplantSproutPrefab;
        public GameObject squashSproutPrefab;
        public GameObject strawberrySproutPrefab;
        public GameObject tomatoSproutPrefab;

        [Header("Growth 2nd Stage Prefabs")]
        public GameObject coconutSecondStagePrefab;
        public GameObject bananaSecondStagePrefab;
        public GameObject durianSecondStagePrefab;
        public GameObject pomeloSecondStagePrefab;
        public GameObject cacaoSecondStagePrefab;
        public GameObject pineappleSecondStagePrefab;
        public GameObject mangosteenSecondStagePrefab;
        public GameObject mangoSecondStagePrefab;
        public GameObject cornSecondStagePrefab;
        public GameObject eggplantSecondStagePrefab;
        public GameObject squashSecondStagePrefab;
        public GameObject strawberrySecondStagePrefab;
        public GameObject tomatoSecondStagePrefab;

        [Header("Pest Trap Placement")]
        public float aphidTrapGroundOffset = -0.15f;
        public float pheromoneTrapGroundOffset = 0f;
        public float termiteBaitGroundOffset = 0f;
        [Range(0f, 1f)]
        [Tooltip("How much of the termite bait station is sunk into the soil, as a " +
                 "share of its own height. A real bait station is buried with only its " +
                 "lid at the surface, so 0.8 leaves roughly the top fifth showing. " +
                 "Raise it to bury deeper, lower it to expose more.")]
        public float termiteBaitBuryFraction = 0.8f;
        public float minDistanceBetweenPestTraps = 4f;

        [Header("Direct Non-Spray Mitigation")]
        public float fruitBagInitialReduction = 20f;
        public float drainageKitInitialReduction = 25f;
        public float sanitationReduction = 25f;
        public float drainageKitIncrease = 0.25f;

        [Range(1f, 100f)]
        public float infectedPlantRemovalThreshold = 60f;

        [Header("Selling")]
        public float coconutSellPesos = 16.89f;
        public float bananaSellPesos = 51f;
        public float durianSellPesos = 150f;
        public float pomeloSellPesos = 200f;
        public float cacaoSellPesos = 275.27f;
        public float pineappleSellPesos = 50f;
        public float mangosteenSellPesos = 50f;
        public float mangoSellPesos = 118f;
        public float cornSellPesos = 15f;
        public float eggplantSellPesos = 62f;
        public float squashSellPesos = 42f;
        public float strawberrySellPesos = 350f;
        public float tomatoSellPesos = 156f;

        [Header("Planting")]
        public float plantHeightOffset = 0f;
        public float minDistanceBetweenTrees = 6f;
        public float bananaDropHeight = 2.8f;
        public float bananaDropForwardOffset = 0.6f;
        public float tropicalBulkDropHeight = 2.8f;
        public float tropicalBulkDropForwardOffset = 0.8f;

        [Header("Per-Crop Ground Height Offsets")]
        public float durianGroundOffset = 0f;
        public float pomeloGroundOffset = 2f;
        public float cacaoGroundOffset = 0f;
        public float pineappleGroundOffset = -4.5f;
        public float mangosteenGroundOffset = 2f;
        public float mangoGroundOffset = 0f;
        public float cornGroundOffset = 0f;
        public float eggplantGroundOffset = 0f;
        public float squashGroundOffset = 0f;
        public float strawberryGroundOffset = 0f;
        public float tomatoGroundOffset = 0f;

        [Header("Sprout / Seedling Visual Y Offsets")]
        public float coconutSproutYOffset = 0f;
        public float bananaSproutYOffset = 0f;
        public float durianSproutYOffset = 0f;
        public float pomeloSproutYOffset = 0f;
        public float cacaoSproutYOffset = 0f;
        public float pineappleSproutYOffset = 0f;
        public float mangosteenSproutYOffset = 0f;
        public float mangoSproutYOffset = 0f;
        public float cornSproutYOffset = 0f;
        public float eggplantSproutYOffset = 0f;
        public float squashSproutYOffset = 0f;
        public float strawberrySproutYOffset = 0f;
        public float tomatoSproutYOffset = 0f;

        [Header("2nd Stage Visual Y Offsets")]
        public float coconutSecondStageYOffset = 0f;
        public float bananaSecondStageYOffset = 0f;
        public float durianSecondStageYOffset = 0f;
        public float pomeloSecondStageYOffset = 0f;
        public float cacaoSecondStageYOffset = 0f;
        public float pineappleSecondStageYOffset = 0f;
        public float mangosteenSecondStageYOffset = 0f;
        public float mangoSecondStageYOffset = 0f;
        public float cornSecondStageYOffset = 0f;
        public float eggplantSecondStageYOffset = 0f;
        public float squashSecondStageYOffset = 0f;
        public float strawberrySecondStageYOffset = 0f;
        public float tomatoSecondStageYOffset = 0f;

        [Header("Harvest")]
        public float coconutSpawnRadius = 2f;
        public float tropicalIndividualSpawnRadius = 2.2f;

        private Transform playerReference;

        public static event System.Action<string> FarmActionApplied;

        private static void ReportFarmAction(string action)
        {
            try
            {
                FarmActionApplied?.Invoke(action);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        public bool HandleInteractAtScreenPosition(Vector2 screenPosition)
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            if (soilAwareTerrain == null)
                soilAwareTerrain = Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();

            if (terrainGenerator == null || terrainGenerator.targetTerrain == null || playerCamera == null)
                return false;

            if (PlayerInventory.Instance == null)
                return false;

            Ray ray = playerCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 5000f))
                return false;

            InventoryItemType selected = PlayerInventory.Instance.selectedItem;

            if (TryHandlePlantingTap(hit, selected))
                return true;

            AphidTrapInstance clickedTrap = hit.collider.GetComponentInParent<AphidTrapInstance>();
            if (clickedTrap != null)
            {
                if (selected == InventoryItemType.None)
                {
                    if (!IsWithinReach(clickedTrap.transform.position))
                        return ReportOutOfReach();

                    if (FarmConfirmPopup.Instance != null)
                    {
                        FarmConfirmPopup.Instance.Show("Do you want to clean up this Aphid Trap?", () =>
                        {
                            clickedTrap.CleanTrap();
                        });
                    }
                    else
                    {
                        clickedTrap.CleanTrap();
                    }
                }
                else
                {
                    Debug.Log(clickedTrap.GetInspectionText());
                }

                return true;
            }



            AreaMitigationTrapInstance clickedAreaTrap =
    hit.collider.GetComponentInParent
        <AreaMitigationTrapInstance>();

            if (clickedAreaTrap != null)
            {
                if (selected == InventoryItemType.None)
                {
                    if (!IsWithinReach(clickedAreaTrap.transform.position))
                        return ReportOutOfReach();

                    void CleanTrap()
                    {
                        clickedAreaTrap.CleanTrap();

                        ShowFarmingMessage(
                            $"{clickedAreaTrap.displayName} was cleaned."
                        );
                    }

                    if (FarmConfirmPopup.Instance != null)
                    {
                        FarmConfirmPopup.Instance.Show(
                            $"Clean the " +
                            $"{clickedAreaTrap.displayName}?",
                            CleanTrap
                        );
                    }
                    else
                    {
                        CleanTrap();
                    }
                }
                else
                {
                    ShowFarmingMessage(
                        clickedAreaTrap.GetInspectionText()
                    );
                }

                return true;
            }

            ShippingBinSeller bin = hit.collider.GetComponentInParent<ShippingBinSeller>();
            if (bin != null && IsSellableCrop(selected))
            {
                return IsWithinReach(bin.transform.position)
                    ? TrySellSelectedToBin(selected)
                    : ReportOutOfReach();
            }

            CoconutTreeInstance clickedTree =
    hit.collider.GetComponentInParent
        <CoconutTreeInstance>();

            if (clickedTree != null)
            {
                PestDiseaseAffectedCrop disease =
                    clickedTree.GetComponent
                        <PestDiseaseAffectedCrop>();

                if (IsWithinReach(clickedTree.transform.position))
                {
                    if (TryHandleNonSprayCropMitigation(
                            disease,
                            clickedTree.gameObject,
                            selected))
                    {
                        return true;
                    }

                    if (selected == InventoryItemType.SprayerPump)
                        return TrySprayCrop(disease);

                    if (selected == InventoryItemType.WateringCan)
                        return TryWaterTree(clickedTree);

                    if (selected == InventoryItemType.Machete)
                    {
                        if (TryHarvestTree(clickedTree))
                            return true;
                    }
                }
                else if (IsCropActionItem(selected))
                {
                    return ReportOutOfReach();
                }

                ShowTreeInfo(clickedTree);
                return true;
            }

            BananaPlantInstance clickedBanana =
    hit.collider.GetComponentInParent
        <BananaPlantInstance>();

            if (clickedBanana != null)
            {
                PestDiseaseAffectedCrop disease =
                    clickedBanana.GetComponent
                        <PestDiseaseAffectedCrop>();

                if (IsWithinReach(clickedBanana.transform.position))
                {
                    if (TryHandleNonSprayCropMitigation(
                            disease,
                            clickedBanana.gameObject,
                            selected))
                    {
                        return true;
                    }

                    if (selected == InventoryItemType.SprayerPump)
                        return TrySprayCrop(disease);

                    if (selected == InventoryItemType.WateringCan)
                        return TryWaterBanana(clickedBanana);

                    if (selected == InventoryItemType.Machete)
                    {
                        if (TryHarvestBanana(clickedBanana))
                            return true;
                    }
                }
                else if (IsCropActionItem(selected))
                {
                    return ReportOutOfReach();
                }

                ShowBananaInfo(clickedBanana);
                return true;
            }

            TropicalCropPlantInstance clickedTropical =
    hit.collider.GetComponentInParent
        <TropicalCropPlantInstance>();

            if (clickedTropical != null)
            {
                PestDiseaseAffectedCrop disease =
                    clickedTropical.GetComponent
                        <PestDiseaseAffectedCrop>();

                if (IsWithinReach(clickedTropical.transform.position))
                {
                    if (TryHandleNonSprayCropMitigation(
                            disease,
                            clickedTropical.gameObject,
                            selected))
                    {
                        return true;
                    }

                    if (selected == InventoryItemType.SprayerPump)
                        return TrySprayCrop(disease);

                    if (selected == InventoryItemType.WateringCan)
                        return TryWaterTropical(clickedTropical);

                    if (selected == InventoryItemType.Machete)
                    {
                        if (TryHarvestTropical(clickedTropical))
                            return true;
                    }
                }
                else if (IsCropActionItem(selected))
                {
                    return ReportOutOfReach();
                }

                ShowTropicalInfo(clickedTropical);
                return true;
            }

            if (TryCollectAnyHarvest(hit))
                return true;

            if (selected == InventoryItemType.Shovel)
                return TryTillGround(hit);

            if (ReportBareGroundPlanting(selected))
                return true;

            if (selected == InventoryItemType.AphidTrap)
            {
                return IsWithinReach(hit.point)
                    ? TryPlaceAphidTrap(hit)
                    : ReportOutOfReach();
            }

            if (selected == InventoryItemType.PheromoneTrap)
            {
                if (!IsWithinReach(hit.point))
                    return ReportOutOfReach();

                return TryPlaceAreaMitigationTrap(
                    hit,
                    InventoryItemType.PheromoneTrap,
                    pheromoneTrapPrefab,
                    PestDiseaseMitigation.PheromoneTrap,
                    "Pheromone/Bait Trap",
                    pheromoneTrapGroundOffset
                );
            }

            if (selected == InventoryItemType.TermiteBaitStation)
            {
                if (!IsWithinReach(hit.point))
                    return ReportOutOfReach();

                return TryPlaceAreaMitigationTrap(
                    hit,
                    InventoryItemType.TermiteBaitStation,
                    termiteBaitStationPrefab,
                    PestDiseaseMitigation.TermiteBait,
                    "Termite Bait Station",
                    termiteBaitGroundOffset,
                    termiteBaitBuryFraction
                );
            }

            return false;
        }

        private bool TryHandleNonSprayCropMitigation(
    PestDiseaseAffectedCrop disease,
    GameObject cropRoot,
    InventoryItemType selected)
        {
            if (selected == InventoryItemType.FruitBag)
            {
                return TryApplyFruitBag(
                    disease,
                    cropRoot
                );
            }

            if (selected == InventoryItemType.DrainageKit)
            {
                return TryApplyDrainageKit(
                    disease,
                    cropRoot
                );
            }

            if (selected == InventoryItemType.Machete)
            {
                return TryUseMacheteMitigation(
                    disease,
                    cropRoot
                );
            }

            return false;
        }

        private bool TryApplyFruitBag(
            PestDiseaseAffectedCrop disease,
            GameObject cropRoot)
        {
            if (cropRoot == null)
                return true;

            if (disease == null)
            {
                ShowFarmingMessage(
                    "That crop has no pest or disease to treat.");
                return true;
            }

            if (!PlayerInventory.Instance.HasItem(
                    InventoryItemType.FruitBag,
                    1))
            {
                ShowFarmingMessage(
                    "You do not have a Fruit Bag."
                );

                return true;
            }

            bool correctStage =
                disease.CurrentStage ==
                    CropDevelopmentStage.PreFruiting ||
                disease.CurrentStage ==
                    CropDevelopmentStage.Fruiting;

            if (!correctStage)
            {
                ShowFarmingMessage(
                    "Fruit Bags can only be installed during " +
                    "pre-fruiting or fruiting growth."
                );

                return true;
            }

            CropProtectionState protection =
                cropRoot.GetComponent<CropProtectionState>();

            if (protection == null)
            {
                protection =
                    cropRoot.AddComponent<CropProtectionState>();
            }

            if (!protection.InstallFruitBag(fruitBagPrefab))
            {
                ShowFarmingMessage(
                    $"{disease.CropDisplayName} already has " +
                    "Fruit Bag protection."
                );

                return true;
            }

            GameAudioManager.Instance.PlayPlaceMitigation();

            bool consumed =
                PlayerInventory.Instance.ConsumeItem(
                    InventoryItemType.FruitBag,
                    1
                );

            if (!consumed)
            {
                protection.hasFruitBag = false;
                return true;
            }

            float removed = disease.ApplyMitigation(
                PestDiseaseMitigation.FruitBag,
                fruitBagInitialReduction
            );

            ShowFarmingMessage(
                $"Fruit Bag installed on " +
                $"{disease.CropDisplayName}.\n" +
                $"Current severity reduced: {removed:F1}%."
            );

            ShowPestDiseaseStatus(disease);
            return true;
        }

        private bool TryApplyDrainageKit(
            PestDiseaseAffectedCrop disease,
            GameObject cropRoot)
        {
            if (cropRoot == null)
                return true;

            if (disease == null)
            {
                ShowFarmingMessage(
                    "That crop has no pest or disease to treat.");
                return true;
            }

            if (!PlayerInventory.Instance.HasItem(
                    InventoryItemType.DrainageKit,
                    1))
            {
                ShowFarmingMessage(
                    "You do not have a Drainage Kit."
                );

                return true;
            }

            CropProtectionState protection =
                cropRoot.GetComponent<CropProtectionState>();

            if (protection == null)
            {
                protection =
                    cropRoot.AddComponent<CropProtectionState>();
            }

            if (!protection.InstallDrainageKit(
                    drainageKitIncrease))
            {
                ShowFarmingMessage(
                    $"{disease.CropDisplayName} already has " +
                    "improved drainage."
                );

                return true;
            }

            GameAudioManager.Instance.PlayPlaceMitigation();

            bool consumed =
                PlayerInventory.Instance.ConsumeItem(
                    InventoryItemType.DrainageKit,
                    1
                );

            if (!consumed)
            {
                protection.hasDrainageImprovement = false;
                return true;
            }

            float removed = disease.ApplyMitigation(
                PestDiseaseMitigation.DrainageImprovement,
                drainageKitInitialReduction
            );

            ShowFarmingMessage(
                $"Drainage Kit installed on " +
                $"{disease.CropDisplayName}.\n" +
                $"Drainage increased by " +
                $"{drainageKitIncrease * 100f:F0}%.\n" +
                $"Current severity reduced: {removed:F1}%."
            );

            ShowPestDiseaseStatus(disease);
            return true;
        }

        private bool TryUseMacheteMitigation(
    PestDiseaseAffectedCrop disease,
    GameObject cropRoot)
        {
            if (disease == null ||
                cropRoot == null ||
                !disease.HasAnyPestOrDisease())
            {
                return false;
            }

            if (TryGetAdvancedRemovalCondition(
                    disease,
                    out PestDiseaseType removableType,
                    out float removableSeverity))
            {
                string conditionName =
                    GetPestDiseaseDisplayName(
                        disease,
                        removableType
                    );

                void RemovePlant()
                {
                    if (cropRoot == null)
                        return;

                    string cropName =
                        disease != null
                            ? disease.CropDisplayName
                            : cropRoot.name;

                    if (disease != null)
                    {
                        disease.ApplyMitigation(
                            PestDiseaseMitigation
                                .RemoveInfectedPlant,
                            100f
                        );
                    }

                    Destroy(cropRoot);

                    ShowFarmingMessage(
                        $"{cropName} was removed to prevent " +
                        $"{conditionName} from spreading."
                    );
                }

                string confirmation =
                    $"{disease.CropDisplayName} has advanced " +
                    $"{conditionName} at " +
                    $"{removableSeverity:F0}% severity.\n\n" +
                    "Remove this infected plant permanently?";

                if (FarmConfirmPopup.Instance != null)
                {
                    FarmConfirmPopup.Instance.Show(
                        confirmation,
                        RemovePlant
                    );
                }
                else
                {
                    RemovePlant();
                }

                return true;
            }

            float removed = disease.ApplyMitigation(
                PestDiseaseMitigation.Sanitation,
                sanitationReduction
            );

            if (removed <= 0.01f)
            {
                return false;
            }

            ShowFarmingMessage(
                $"{disease.CropDisplayName} was pruned " +
                $"and sanitized.\n" +
                $"Total severity reduced by {removed:F1}%."
            );

            ShowPestDiseaseStatus(disease);
            return true;
        }

        private bool TryGetAdvancedRemovalCondition(
            PestDiseaseAffectedCrop disease,
            out PestDiseaseType conditionType,
            out float severity)
        {
            conditionType = PestDiseaseType.None;
            severity = 0f;

            PestDiseaseType[] removableConditions =
            {
        PestDiseaseType.MosaicVirus,
        PestDiseaseType.PanamaDisease,
        PestDiseaseType.BacterialWilt
    };

            foreach (PestDiseaseType type
                     in removableConditions)
            {
                float currentSeverity =
                    disease.GetConditionSeverity(type);

                if (currentSeverity <
                    infectedPlantRemovalThreshold)
                {
                    continue;
                }

                if (currentSeverity > severity)
                {
                    severity = currentSeverity;
                    conditionType = type;
                }
            }

            return conditionType != PestDiseaseType.None;
        }

        private string GetPestDiseaseDisplayName(
            PestDiseaseAffectedCrop disease,
            PestDiseaseType type)
        {
            if (PestDiseaseSystem.Instance != null &&
                PestDiseaseSystem.Instance.Database != null)
            {
                PestDiseaseRule rule =
                    PestDiseaseSystem.Instance.Database.GetRule(
                        disease.CropType,
                        type
                    );

                if (rule != null &&
                    !string.IsNullOrWhiteSpace(
                        rule.displayName))
                {
                    return rule.displayName;
                }
            }

            return type.ToString();
        }

        private bool TryPlaceAreaMitigationTrap(
    RaycastHit hit,
    InventoryItemType inventoryItem,
    GameObject trapPrefab,
    PestDiseaseMitigation mitigation,
    string displayName,
    float groundOffset,
    float buryFraction = 0f)
        {
            Terrain terrain =
                terrainGenerator.targetTerrain;

            TerrainCollider terrainCollider =
                terrain != null
                    ? terrain.GetComponent<TerrainCollider>()
                    : null;

            if (terrainCollider == null ||
                hit.collider != terrainCollider)
            {
                return false;
            }

            if (!PlayerInventory.Instance.HasItem(
                    inventoryItem,
                    1))
            {
                ShowFarmingMessage(
                    $"You do not have a {displayName}."
                );

                return true;
            }

            if (trapPrefab == null)
            {
                Debug.LogWarning(
                    $"FarmingInteractionSystem: " +
                    $"{displayName} prefab is not assigned."
                );

                ShowFarmingMessage(
                    $"{displayName} prefab is not assigned."
                );

                return true;
            }

            Vector3 spawnPosition = hit.point;

            spawnPosition.y =
                terrain.SampleHeight(spawnPosition) +
                terrain.transform.position.y;

            if (HasNearbyPestTrap(spawnPosition))
            {
                ShowFarmingMessage(
                    "Another pest-control trap is too close."
                );

                return true;
            }

            Vector3 terrainNormal =
                GetTerrainNormal(
                    terrain,
                    spawnPosition
                );

            Quaternion slopeRotation =
                Quaternion.FromToRotation(
                    Vector3.up,
                    terrainNormal
                );

            Quaternion finalRotation =
                slopeRotation *
                trapPrefab.transform.rotation;

            GameObject trap = Instantiate(
                trapPrefab,
                spawnPosition,
                finalRotation
            );

            trap.name =
                $"{displayName}_Runtime";

            DistanceCullable.Attach(trap);
            GameAudioManager.Instance.PlayPlaceMitigation();

            AreaMitigationTrapInstance instance =
                trap.GetComponent
                    <AreaMitigationTrapInstance>();

            bool componentWasAdded = instance == null;

            if (componentWasAdded)
            {
                instance =
                    trap.AddComponent
                        <AreaMitigationTrapInstance>();
            }

            instance.displayName = displayName;
            instance.mitigation = mitigation;

            if (componentWasAdded)
            {
                if (mitigation ==
                    PestDiseaseMitigation.TermiteBait)
                {
                    instance.radius = 10f;
                    instance
                        .reductionPercentPerCropPerGameDay = 20f;
                    instance.capacityPercent = 100f;
                }
                else
                {
                    instance.radius = 14f;
                    instance
                        .reductionPercentPerCropPerGameDay = 18f;
                    instance.capacityPercent = 150f;
                }
            }

            EnsureCollider(trap);
            FreezePlacedObject(trap);

            SnapObjectBottomToTerrain(
                trap,
                terrain,
                groundOffset,
                buryFraction
            );

            PlayerInventory.Instance.ConsumeItem(
                inventoryItem,
                1
            );

            ShowFarmingMessage(
                $"{displayName} placed successfully.\n" +
                $"Effect radius: {instance.radius:F1} meters."
            );

            return true;
        }

        private bool HasNearbyPestTrap(
            Vector3 position)
        {
            Collider[] nearby = Physics.OverlapSphere(
                position,
                minDistanceBetweenPestTraps
            );

            foreach (Collider nearbyCollider in nearby)
            {
                if (nearbyCollider == null)
                    continue;

                if (nearbyCollider.GetComponentInParent
                        <AphidTrapInstance>() != null)
                {
                    return true;
                }

                if (nearbyCollider.GetComponentInParent
                        <AreaMitigationTrapInstance>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void FreezePlacedObject(
            GameObject placedObject)
        {
            if (placedObject == null)
                return;

            Rigidbody[] bodies =
                placedObject.GetComponentsInChildren
                    <Rigidbody>(true);

            foreach (Rigidbody body in bodies)
            {
                if (body == null)
                    continue;

                body.useGravity = false;
                body.isKinematic = true;
            }
        }

        private Vector3 GetTerrainNormal(
    Terrain terrain,
    Vector3 worldPosition)
        {
            if (terrain == null ||
                terrain.terrainData == null)
            {
                return Vector3.up;
            }

            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPosition = terrain.transform.position;

            float normalizedX = Mathf.Clamp01(
                (worldPosition.x - terrainPosition.x) /
                terrainData.size.x
            );

            float normalizedZ = Mathf.Clamp01(
                (worldPosition.z - terrainPosition.z) /
                terrainData.size.z
            );

            return terrainData.GetInterpolatedNormal(
                normalizedX,
                normalizedZ
            );
        }

        private bool HasNearbyDigSpot(Vector3 position)
        {
            Collider[] nearby = Physics.OverlapSphere(
                position,
                minDistanceBetweenDigSpots
            );

            foreach (Collider nearbyCollider in nearby)
            {
                if (nearbyCollider == null)
                    continue;

                DigSpot nearbySpot =
                    nearbyCollider.GetComponentInParent<DigSpot>();

                if (nearbySpot != null)
                    return true;
            }

            return false;
        }

        public bool IsWithinReach(Vector3 position)
        {
            if (maxInteractDistance <= 0f)
                return true;

            Vector3 player = GetPlayerPosition();
            float dx = player.x - position.x;
            float dz = player.z - position.z;

            return (dx * dx + dz * dz) <= maxInteractDistance * maxInteractDistance;
        }

        private static bool IsCropActionItem(InventoryItemType item)
        {
            return item == InventoryItemType.WateringCan ||
                   item == InventoryItemType.SprayerPump ||
                   item == InventoryItemType.Machete ||
                   item == InventoryItemType.FruitBag ||
                   item == InventoryItemType.DrainageKit;
        }

        public bool ReportOutOfReach()
        {
            ShowFarmingMessage("That is too far away. Move closer.");
            return true;
        }

        private Vector3 GetPlayerPosition()
        {
            if (playerReference == null)
            {
                FirstPersonTerrainController player =
                    Object.FindFirstObjectByType<FirstPersonTerrainController>();

                if (player != null)
                    playerReference = player.transform;
            }

            if (playerReference != null)
                return playerReference.position;

            return playerCamera != null
                ? playerCamera.transform.position
                : Vector3.zero;
        }

        private void ShowFarmingMessage(string message)
        {
            Debug.Log("[Farming] " + message);

            if (soilAwareTerrain == null)
            {
                soilAwareTerrain =
                    Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();
            }

            if (soilAwareTerrain != null)
            {
                soilAwareTerrain.ShowMessage(message);
            }
        }

        private bool TrySprayCrop(PestDiseaseAffectedCrop disease)
        {
            if (disease == null)
            {
                Debug.Log("[Sprayer] This crop has no pest/disease component yet.");
                return true;
            }

            if (PlayerInventory.Instance.TryUseSelectedSprayerOnCrop(disease, out string message))
            {
                GameAudioManager.Instance.PlaySpray();
                Debug.Log("[Sprayer] " + message);
                ShowPestDiseaseStatus(disease);
                return true;
            }

            Debug.Log("[Sprayer] " + message);
            ShowPestDiseaseStatus(disease);
            return true;
        }

        private void ShowPestDiseaseStatus(PestDiseaseAffectedCrop disease)
        {
            if (soilAwareTerrain == null)
                soilAwareTerrain = Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();

            if (soilAwareTerrain != null && disease != null)
                soilAwareTerrain.ShowPestDiseaseInfo(disease);
        }

        private bool TryPlaceAphidTrap(RaycastHit hit)
        {
            Terrain terrain = terrainGenerator.targetTerrain;
            TerrainCollider terrainCollider = terrain != null ? terrain.GetComponent<TerrainCollider>() : null;

            if (terrainCollider == null || hit.collider != terrainCollider)
                return false;

            if (aphidTrapPrefab == null)
            {
                Debug.LogWarning("FarmingInteractionSystem: Aphid Trap prefab is not assigned.");
                return true;
            }

            if (!PlayerInventory.Instance.HasItem(InventoryItemType.AphidTrap, 1))
                return true;

            Vector3 spawnPos = GetTerrainSpawnPosition(hit, terrain);

            GameObject trap = Instantiate(
                aphidTrapPrefab,
                spawnPos,
                aphidTrapPrefab.transform.rotation
            );

            if (trap.GetComponent<AphidTrapInstance>() == null)
                trap.AddComponent<AphidTrapInstance>();

            DistanceCullable.Attach(trap);
            GameAudioManager.Instance.PlayPlaceMitigation();

            EnsureCollider(trap);
            SnapObjectBottomToTerrain(trap, terrain, aphidTrapGroundOffset);

            PlayerInventory.Instance.ConsumeItem(InventoryItemType.AphidTrap, 1);

            return true;
        }


        private Vector3 GetTerrainSpawnPosition(RaycastHit hit, Terrain terrain)
        {
            Vector3 spawnPos = hit.point;
            spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y + plantHeightOffset;
            return spawnPos;
        }

        private void GetSoilForPlanting(Vector3 spawnPos, out SoilSample plantedSoil, out string district)
        {
            plantedSoil = new SoilSample
            {
                sand = 45f,
                silt = 28f,
                clay = 27f,
                phh2o = 6.1f,
                soc = 22f,
                cfvo = 6f,
                bdod = 125f,
                nitrogen = 14f
            };

            district = SelectedAreaState.SelectedDistrictName;

            if (soilAwareTerrain != null &&
                soilAwareTerrain.TryGetSoilAtWorldPosition(spawnPos, out SoilSample sampled, out string sampledDistrict))
            {
                plantedSoil = sampled;
                district = sampledDistrict;
            }
        }

        private bool HasNearbyCrop(Vector3 spawnPos)
        {
            Collider[] nearby = Physics.OverlapSphere(
                spawnPos,
                minDistanceBetweenTrees,
                ~0,
                QueryTriggerInteraction.Ignore
            );
            foreach (Collider c in nearby)
            {
                if (c.GetComponentInParent<CoconutTreeInstance>() != null)
                    return true;

                if (c.GetComponentInParent<BananaPlantInstance>() != null)
                    return true;

                if (c.GetComponentInParent<TropicalCropPlantInstance>() != null)
                    return true;
            }

            return false;
        }

        private bool TryWaterTree(CoconutTreeInstance tree)
        {
            if (tree == null)
                return false;

            if (!PlayerInventory.Instance.HasItem(InventoryItemType.WateringCan, 1))
                return true;

            float beforeMoisture = tree.moisture;
            tree.Water(0.40f);
            GameAudioManager.Instance.PlayWater();

            if (ClimateEventTracker.Instance != null)
                ClimateEventTracker.Instance.RecordWaterAction(tree.cropId, "Coconut", beforeMoisture, tree.moisture);

            ShowTreeInfo(tree);
            ReportFarmAction("Watering");
            return true;
        }

        private bool TryWaterBanana(BananaPlantInstance tree)
        {
            if (tree == null)
                return false;

            if (!PlayerInventory.Instance.HasItem(InventoryItemType.WateringCan, 1))
                return true;

            float beforeMoisture = tree.moisture;
            tree.Water(0.45f);
            GameAudioManager.Instance.PlayWater();

            if (ClimateEventTracker.Instance != null)
                ClimateEventTracker.Instance.RecordWaterAction(tree.cropId, "Banana", beforeMoisture, tree.moisture);

            ShowBananaInfo(tree);
            ReportFarmAction("Watering");
            return true;
        }

        private bool TryWaterTropical(TropicalCropPlantInstance plant)
        {
            if (plant == null)
                return false;

            if (!PlayerInventory.Instance.HasItem(InventoryItemType.WateringCan, 1))
                return true;

            float beforeMoisture = plant.moisture;
            plant.Water(TropicalCropCatalog.GetWaterAmount(plant.cropKind));
            GameAudioManager.Instance.PlayWater();

            if (ClimateEventTracker.Instance != null)
                ClimateEventTracker.Instance.RecordWaterAction(plant.cropId, plant.CropDisplayName, beforeMoisture, plant.moisture);

            ShowTropicalInfo(plant);
            ReportFarmAction("Watering");
            return true;
        }

        private bool TryHarvestTree(CoconutTreeInstance tree)
        {
            if (tree == null)
                return false;

            if (!PlayerInventory.Instance.HasItem(InventoryItemType.Machete, 1))
                return true;

            if (!tree.CanHarvest())
            {
                ShowTreeInfo(tree);
                return true;
            }

            if (coconutFruitPrefab == null)
            {
                Debug.LogWarning("FarmingInteractionSystem: Coconut Fruit Prefab is not assigned.");
                return true;
            }

            int harvestCount = tree.Harvest();
            SpawnIndividualHarvests(tree.transform.position, coconutFruitPrefab, InventoryItemType.Coconut, harvestCount, coconutSpawnRadius, 4f);
            ShowTreeInfo(tree);
            ReportFarmAction("Harvesting");
            return true;
        }

        private bool TryHarvestBanana(BananaPlantInstance tree)
        {
            if (tree == null)
                return false;

            if (!PlayerInventory.Instance.HasItem(InventoryItemType.Machete, 1))
                return true;

            if (!tree.CanHarvest())
            {
                ShowBananaInfo(tree);
                return true;
            }

            if (bananaFruitPrefab == null)
            {
                Debug.LogWarning("FarmingInteractionSystem: Banana Fruit Prefab is not assigned.");
                return true;
            }

            var harvestedBulbs = tree.HarvestAllBulbs();
            if (harvestedBulbs == null || harvestedBulbs.Count == 0)
            {
                ShowBananaInfo(tree);
                return true;
            }

            Terrain terrain = terrainGenerator != null ? terrainGenerator.targetTerrain : null;
            Vector3 center = tree.transform.position + tree.transform.forward * bananaDropForwardOffset + Vector3.up * bananaDropHeight;

            for (int i = 0; i < harvestedBulbs.Count; i++)
            {
                BananaBulbHarvest bulb = harvestedBulbs[i];
                Vector3 offset = new Vector3((i - (harvestedBulbs.Count - 1) * 0.5f) * 0.8f, 0f, 0f);
                Vector3 spawnPos = center + offset;

                if (terrain != null)
                {
                    float groundY = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
                    spawnPos.y = Mathf.Max(spawnPos.y, groundY + 1.5f);
                }

                GameObject bananaBunch = Instantiate(bananaFruitPrefab, spawnPos, bananaFruitPrefab.transform.rotation);

                CollectibleBanana collectible = bananaBunch.GetComponent<CollectibleBanana>();
                if (collectible == null)
                    collectible = bananaBunch.AddComponent<CollectibleBanana>();

                collectible.amount = bulb.bananaCount;
                EnsureCollider(bananaBunch);
                DistanceCullable.Attach(bananaBunch);
                TossHarvestObject(bananaBunch, Vector3.up * 1.2f + offset.normalized * 0.8f);
            }

            ShowBananaInfo(tree);
            ReportFarmAction("Harvesting");
            return true;
        }

        private bool TryHarvestTropical(TropicalCropPlantInstance plant)
        {
            if (plant == null)
                return false;

            if (!PlayerInventory.Instance.HasItem(InventoryItemType.Machete, 1))
                return true;

            if (!plant.CanHarvest())
            {
                ShowTropicalInfo(plant);
                return true;
            }

            GameObject fruitPrefab = GetTropicalFruitPrefab(plant.cropKind);
            if (fruitPrefab == null)
            {
                Debug.LogWarning($"FarmingInteractionSystem: {plant.CropDisplayName} fruit prefab is not assigned.");
                return true;
            }

            if (plant.HarvestMode == TropicalHarvestMode.IndividualFruit)
            {
                int harvestCount = plant.HarvestIndividualFruits();
                SpawnIndividualHarvests(plant.transform.position, fruitPrefab, plant.FruitItemType, harvestCount, tropicalIndividualSpawnRadius, 3.2f, plant.cropKind);
            }
            else
            {
                List<TropicalBundleHarvest> bundles = plant.HarvestBundles();
                SpawnBulkHarvests(plant, fruitPrefab, bundles);
            }

            ShowTropicalInfo(plant);
            ReportFarmAction("Harvesting");
            return true;
        }

        private void SpawnIndividualHarvests(Vector3 center, GameObject fruitPrefab, InventoryItemType itemType, int harvestCount, float radius, float height, TropicalCropKind? cropKind = null)
        {
            if (fruitPrefab == null || harvestCount <= 0)
                return;

            Terrain terrain = terrainGenerator != null ? terrainGenerator.targetTerrain : null;

            for (int i = 0; i < harvestCount; i++)
            {
                float angle = (360f / Mathf.Max(1, harvestCount)) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Vector3 spawnPos = center + offset + Vector3.up * height;

                if (terrain != null)
                {
                    float groundY = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
                    spawnPos.y = Mathf.Max(spawnPos.y, groundY + 1.5f);
                }

                GameObject fruit = Instantiate(fruitPrefab, spawnPos, fruitPrefab.transform.rotation);

                if (itemType == InventoryItemType.Coconut)
                {
                    CollectibleCoconut coconut = fruit.GetComponent<CollectibleCoconut>();
                    if (coconut == null)
                        coconut = fruit.AddComponent<CollectibleCoconut>();
                    coconut.amount = 1;
                }
                else
                {
                    CollectibleTropicalCrop collectible = fruit.GetComponent<CollectibleTropicalCrop>();
                    if (collectible == null)
                        collectible = fruit.AddComponent<CollectibleTropicalCrop>();

                    if (cropKind.HasValue)
                        collectible.cropKind = cropKind.Value;

                    collectible.itemType = itemType;
                    collectible.amount = 1;
                }

                EnsureCollider(fruit);
                DistanceCullable.Attach(fruit);
                TossHarvestObject(fruit, (offset.normalized + Vector3.up * 1.2f) * 2.5f);
            }
        }

        private void SpawnBulkHarvests(TropicalCropPlantInstance plant, GameObject fruitPrefab, List<TropicalBundleHarvest> bundles)
        {
            if (plant == null || fruitPrefab == null || bundles == null || bundles.Count == 0)
                return;

            Terrain terrain = terrainGenerator != null ? terrainGenerator.targetTerrain : null;
            Vector3 center = plant.transform.position + plant.transform.forward * tropicalBulkDropForwardOffset + Vector3.up * tropicalBulkDropHeight;

            for (int i = 0; i < bundles.Count; i++)
            {
                TropicalBundleHarvest bundle = bundles[i];
                Vector3 offset = new Vector3((i - (bundles.Count - 1) * 0.5f) * 0.9f, 0f, 0f);
                Vector3 spawnPos = center + offset;

                if (terrain != null)
                {
                    float groundY = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
                    spawnPos.y = Mathf.Max(spawnPos.y, groundY + 1.5f);
                }

                GameObject bulkFruit = Instantiate(fruitPrefab, spawnPos, fruitPrefab.transform.rotation);

                CollectibleTropicalCrop collectible = bulkFruit.GetComponent<CollectibleTropicalCrop>();
                if (collectible == null)
                    collectible = bulkFruit.AddComponent<CollectibleTropicalCrop>();

                collectible.cropKind = plant.cropKind;
                collectible.itemType = plant.FruitItemType;
                collectible.amount = bundle.itemCount;

                EnsureCollider(bulkFruit);
                DistanceCullable.Attach(bulkFruit);
                TossHarvestObject(bulkFruit, Vector3.up * 1.2f + offset.normalized * 0.8f);
            }
        }

        private void TossHarvestObject(GameObject go, Vector3 impulse)
        {
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null)
                return;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(impulse, ForceMode.Impulse);
        }

        private bool TryCollectAnyHarvest(RaycastHit hit)
        {
            CollectibleCoconut coconut = hit.collider.GetComponentInParent<CollectibleCoconut>();
            if (coconut != null)
            {
                if (!IsWithinReach(coconut.transform.position))
                    return ReportOutOfReach();

                PlayerInventory.Instance.AddItem(InventoryItemType.Coconut, coconut.amount);
                Destroy(coconut.gameObject);
                return true;
            }

            CollectibleBanana banana = hit.collider.GetComponentInParent<CollectibleBanana>();
            if (banana != null)
            {
                if (!IsWithinReach(banana.transform.position))
                    return ReportOutOfReach();

                PlayerInventory.Instance.AddItem(InventoryItemType.Banana, banana.amount);
                Destroy(banana.gameObject);
                return true;
            }

            CollectibleTropicalCrop tropical = hit.collider.GetComponentInParent<CollectibleTropicalCrop>();
            if (tropical != null)
            {
                if (!IsWithinReach(tropical.transform.position))
                    return ReportOutOfReach();

                InventoryItemType item = tropical.itemType != InventoryItemType.None
                    ? tropical.itemType
                    : TropicalCropCatalog.GetFruitItem(tropical.cropKind);

                PlayerInventory.Instance.AddItem(item, tropical.amount);
                Destroy(tropical.gameObject);
                return true;
            }

            return false;
        }

        private bool IsSellableCrop(InventoryItemType item)
        {
            return item == InventoryItemType.Coconut ||
                   item == InventoryItemType.Banana ||
                   item == InventoryItemType.Durian ||
                   item == InventoryItemType.Pomelo ||
                   item == InventoryItemType.Cacao ||
                   item == InventoryItemType.Pineapple ||
                   item == InventoryItemType.Mangosteen ||
                   item == InventoryItemType.Mango ||
                   item == InventoryItemType.Corn ||
                   item == InventoryItemType.Eggplant ||
                   item == InventoryItemType.Squash ||
                   item == InventoryItemType.Strawberry ||
                   item == InventoryItemType.Tomato;
        }

        private bool TrySellSelectedToBin(InventoryItemType selected)
        {
            int centavosEach = GetSellPriceCentavos(selected);
            int soldCount = PlayerInventory.Instance.SellAll(selected, centavosEach);
            return soldCount > 0;
        }

        private int GetSellPriceCentavos(InventoryItemType item)
        {
            float pesos = item switch
            {
                InventoryItemType.Coconut => coconutSellPesos,
                InventoryItemType.Banana => bananaSellPesos,
                InventoryItemType.Durian => durianSellPesos,
                InventoryItemType.Pomelo => pomeloSellPesos,
                InventoryItemType.Cacao => cacaoSellPesos,
                InventoryItemType.Pineapple => pineappleSellPesos,
                InventoryItemType.Mangosteen => mangosteenSellPesos,
                InventoryItemType.Mango => mangoSellPesos,
                InventoryItemType.Corn => cornSellPesos,
                InventoryItemType.Eggplant => eggplantSellPesos,
                InventoryItemType.Squash => squashSellPesos,
                InventoryItemType.Strawberry => strawberrySellPesos,
                InventoryItemType.Tomato => tomatoSellPesos,

                _ => 0f
            };

            return PesoPrice.CentavosFromInspector(pesos);
        }

        private GameObject GetTropicalTreePrefab(TropicalCropKind cropKind)
        {
            return cropKind switch
            {
                TropicalCropKind.Durian => durianTreePrefab,
                TropicalCropKind.Pomelo => pomeloTreePrefab,
                TropicalCropKind.Cacao => cacaoTreePrefab,
                TropicalCropKind.Pineapple => pineappleTreePrefab,
                TropicalCropKind.Mangosteen => mangosteenTreePrefab,
                TropicalCropKind.Mango => mangoTreePrefab,

                TropicalCropKind.Corn => cornTreePrefab,
                TropicalCropKind.Eggplant => eggplantTreePrefab,
                TropicalCropKind.Squash => squashTreePrefab,
                TropicalCropKind.Strawberry => strawberryTreePrefab,
                TropicalCropKind.Tomato => tomatoTreePrefab,

                _ => null
            };
        }

        private float GetTropicalGroundOffset(TropicalCropKind cropKind)
        {
            return cropKind switch
            {
                TropicalCropKind.Durian => durianGroundOffset,
                TropicalCropKind.Pomelo => pomeloGroundOffset,
                TropicalCropKind.Cacao => cacaoGroundOffset,
                TropicalCropKind.Pineapple => pineappleGroundOffset,
                TropicalCropKind.Mangosteen => mangosteenGroundOffset,
                TropicalCropKind.Mango => mangoGroundOffset,

                TropicalCropKind.Corn => cornGroundOffset,
                TropicalCropKind.Eggplant => eggplantGroundOffset,
                TropicalCropKind.Squash => squashGroundOffset,
                TropicalCropKind.Strawberry => strawberryGroundOffset,
                TropicalCropKind.Tomato => tomatoGroundOffset,

                _ => 0f
            };
        }


        private GameObject GetTropicalFruitPrefab(TropicalCropKind cropKind)
        {
            return cropKind switch
            {
                TropicalCropKind.Durian => durianFruitPrefab,
                TropicalCropKind.Pomelo => pomeloFruitPrefab,
                TropicalCropKind.Cacao => cacaoFruitPrefab,
                TropicalCropKind.Pineapple => pineappleFruitPrefab,
                TropicalCropKind.Mangosteen => mangosteenFruitPrefab,
                TropicalCropKind.Mango => mangoFruitPrefab,

                TropicalCropKind.Corn => cornFruitPrefab,
                TropicalCropKind.Eggplant => eggplantFruitPrefab,
                TropicalCropKind.Squash => squashFruitPrefab,
                TropicalCropKind.Strawberry => strawberryFruitPrefab,
                TropicalCropKind.Tomato => tomatoFruitPrefab,

                _ => null
            };
        }


        public GameObject RestoreCropFromSave(CropSaveDto save)
        {
            if (save == null)
                return null;

            SoilSample restoredSoil = save.plantedSoil ?? new SoilSample
            {
                sand = 45f,
                silt = 28f,
                clay = 27f,
                phh2o = 6.1f,
                soc = 22f,
                cfvo = 6f,
                bdod = 125f,
                nitrogen = 14f
            };

            GameObject root;

            InventoryItemType savedMaterial =
                PlantingMaterialCatalog.TryParseItem(save.plantingMaterial, out InventoryItemType parsedMaterial)
                    ? PlantingMaterialCatalog.UpgradeLegacy(parsedMaterial)
                    : InventoryItemType.None;

            if (string.Equals(save.cropFamily, "Coconut", System.StringComparison.OrdinalIgnoreCase))
            {
                PlantGrowthVisualSet visualSet = CropVisualSet(FarmCropType.Coconut, savedMaterial);

                root = CreatePlantRoot("CoconutTree", save.position.ToVector3());
                CoconutTreeInstance crop = root.AddComponent<CoconutTreeInstance>();
                EnsurePestDiseaseComponent(root);
                AddGrowthVisuals(root, visualSet);
                EnsureCollider(root);
                crop.Initialize(restoredSoil, save.districtName);
                CropPersistenceMapper.Restore(crop, save);
                RefreshRestoredVisual(root);
                RestorePlantingDetails(root, save);
                return root;
            }

            if (string.Equals(save.cropFamily, "Banana", System.StringComparison.OrdinalIgnoreCase))
            {
                PlantGrowthVisualSet visualSet = CropVisualSet(FarmCropType.Banana, savedMaterial);

                root = CreatePlantRoot("BananaPlant", save.position.ToVector3());
                BananaPlantInstance crop = root.AddComponent<BananaPlantInstance>();
                EnsurePestDiseaseComponent(root);
                AddGrowthVisuals(root, visualSet);
                EnsureCollider(root);
                crop.Initialize(restoredSoil, save.districtName);
                CropPersistenceMapper.Restore(crop, save);
                RefreshRestoredVisual(root);
                RestorePlantingDetails(root, save);
                return root;
            }

            if (!System.Enum.TryParse(save.tropicalCropKind, out TropicalCropKind cropKind))
            {
                Debug.LogWarning("Could not restore crop because its tropical crop kind is invalid: " + save.tropicalCropKind);
                return null;
            }

            PlantGrowthVisualSet tropicalVisuals =
                CropVisualSet(CropClimateRules.GetFarmCropType(cropKind), savedMaterial);

            root = CreatePlantRoot(
                $"{TropicalCropCatalog.GetDisplayName(cropKind)}Plant",
                save.position.ToVector3()
            );

            TropicalCropPlantInstance tropical = root.AddComponent<TropicalCropPlantInstance>();
            tropical.cropKind = cropKind;
            EnsurePestDiseaseComponent(root);
            AddGrowthVisuals(root, tropicalVisuals);
            EnsureCollider(root);
            tropical.Initialize(restoredSoil, save.districtName);
            CropPersistenceMapper.Restore(tropical, save);
            RefreshRestoredVisual(root);
            RestorePlantingDetails(root, save);
            return root;
        }

        public GameObject RestoreWorldObjectFromSave(WorldObjectSaveDto save)
        {
            if (save == null)
                return null;

            if (string.Equals(save.objectType, "AphidTrap", System.StringComparison.OrdinalIgnoreCase))
            {
                if (aphidTrapPrefab == null)
                {
                    Debug.LogWarning("Cannot restore Aphid Trap: prefab is not assigned.");
                    return null;
                }

                GameObject trapObject = Instantiate(
                    aphidTrapPrefab,
                    save.position.ToVector3(),
                    save.rotation.ToQuaternion()
                );

                AphidTrapInstance trap = trapObject.GetComponent<AphidTrapInstance>();
                if (trap == null)
                    trap = trapObject.AddComponent<AphidTrapInstance>();

                trap.deadAphidLoadPercent = Mathf.Max(0f, save.currentLoad);
                trap.capacityPercent = Mathf.Max(1f, save.capacity);
                trap.radius = Mathf.Max(0.1f, save.radius);
                EnsureCollider(trapObject);
                DistanceCullable.Attach(trapObject);
                return trapObject;
            }

            if (string.Equals(save.objectType, "AreaMitigationTrap", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!System.Enum.TryParse(save.mitigation, out PestDiseaseMitigation mitigation))
                    mitigation = PestDiseaseMitigation.PheromoneTrap;

                GameObject prefab = mitigation == PestDiseaseMitigation.TermiteBait
                    ? termiteBaitStationPrefab
                    : pheromoneTrapPrefab;

                if (prefab == null)
                {
                    Debug.LogWarning("Cannot restore mitigation trap: matching prefab is not assigned.");
                    return null;
                }

                GameObject trapObject = Instantiate(
                    prefab,
                    save.position.ToVector3(),
                    save.rotation.ToQuaternion()
                );

                AreaMitigationTrapInstance trap = trapObject.GetComponent<AreaMitigationTrapInstance>();
                if (trap == null)
                    trap = trapObject.AddComponent<AreaMitigationTrapInstance>();

                trap.mitigation = mitigation;
                trap.capturedLoadPercent = Mathf.Max(0f, save.currentLoad);
                trap.capacityPercent = Mathf.Max(1f, save.capacity);
                trap.radius = Mathf.Max(0.1f, save.radius);
                EnsureCollider(trapObject);
                DistanceCullable.Attach(trapObject);
                return trapObject;
            }

            Debug.LogWarning("Unknown saved world object type: " + save.objectType);
            return null;
        }

        private static void RefreshRestoredVisual(GameObject root)
        {
            GrowthStageVisualController visuals = root != null
                ? root.GetComponent<GrowthStageVisualController>()
                : null;

            if (visuals != null)
                visuals.ForceRefresh();
        }

        public void ForceAllPlantsToProduceForDev()
        {
            CoconutTreeInstance[] coconuts = Object.FindObjectsByType<CoconutTreeInstance>(FindObjectsSortMode.None);
            foreach (CoconutTreeInstance c in coconuts)
                if (c != null) c.ForceProduceHarvestForDev();

            BananaPlantInstance[] bananas = Object.FindObjectsByType<BananaPlantInstance>(FindObjectsSortMode.None);
            foreach (BananaPlantInstance b in bananas)
                if (b != null) b.ForceProduceHarvestForDev();

            TropicalCropPlantInstance[] tropicals = Object.FindObjectsByType<TropicalCropPlantInstance>(FindObjectsSortMode.None);
            foreach (TropicalCropPlantInstance t in tropicals)
                if (t != null) t.ForceProduceHarvestForDev();

            Debug.Log($"[DevTools] Harvest forced ready | Coconuts={coconuts.Length} | Bananas={bananas.Length} | TropicalPlants={tropicals.Length}");
        }

        public void ForceAllPlantsNextStageForDev()
        {
            CoconutTreeInstance[] coconuts = Object.FindObjectsByType<CoconutTreeInstance>(FindObjectsSortMode.None);
            foreach (CoconutTreeInstance c in coconuts)
                if (c != null) c.ForceNextStageForDev();

            BananaPlantInstance[] bananas = Object.FindObjectsByType<BananaPlantInstance>(FindObjectsSortMode.None);
            foreach (BananaPlantInstance b in bananas)
                if (b != null) b.ForceNextStageForDev();

            TropicalCropPlantInstance[] tropicals = Object.FindObjectsByType<TropicalCropPlantInstance>(FindObjectsSortMode.None);
            foreach (TropicalCropPlantInstance t in tropicals)
                if (t != null) t.ForceNextStageForDev();

            Debug.Log($"[DevTools] Forced next growth stage | Coconuts={coconuts.Length} | Bananas={bananas.Length} | TropicalPlants={tropicals.Length}");
        }

        private void ShowTreeInfo(CoconutTreeInstance tree)
        {
            if (soilAwareTerrain == null)
                soilAwareTerrain = Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();

            if (soilAwareTerrain != null)
                soilAwareTerrain.ShowTreeInfo(tree);
        }

        private void ShowBananaInfo(BananaPlantInstance tree)
        {
            if (soilAwareTerrain == null)
                soilAwareTerrain = Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();

            if (soilAwareTerrain != null)
                soilAwareTerrain.ShowTreeInfo(tree);
        }

        private void ShowTropicalInfo(TropicalCropPlantInstance plant)
        {
            if (soilAwareTerrain == null)
                soilAwareTerrain = Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();

            if (soilAwareTerrain != null)
                soilAwareTerrain.ShowTreeInfo(plant);
        }

        public GameObject GetCropMaintenanceVisualPrefab(CropMaintenanceActionType action)
        {
            switch (action)
            {
                case CropMaintenanceActionType.Mulch:
                    return mulchVisualPrefab;
                case CropMaintenanceActionType.SupportStake:
                    return supportStakePrefab;
                case CropMaintenanceActionType.Trellis:
                    return trellisPrefab;
                default:
                    return null;
            }
        }

        public GameObject GetClimateMitigationPrefab(ClimateWorldMitigationType type)
        {
            switch (type)
            {
                case ClimateWorldMitigationType.IrrigationSystem:
                    return irrigationSystemPrefab;
                case ClimateWorldMitigationType.WaterStorageTank:
                    return waterStorageTankPrefab;
                case ClimateWorldMitigationType.ShadeNet:
                    return shadeNetPrefab;
                case ClimateWorldMitigationType.Windbreak:
                    return windbreakPrefab;
                case ClimateWorldMitigationType.Greenhouse:
                    return greenhousePrefab;
                case ClimateWorldMitigationType.DrainageCanal:
                    return drainageCanalPrefab;
                default:
                    return null;
            }
        }

        public Vector3 GetClimateMitigationPlacementOffset(ClimateWorldMitigationType type)
        {
            switch (type)
            {
                case ClimateWorldMitigationType.IrrigationSystem:
                    return irrigationSystemPlacementOffset;
                case ClimateWorldMitigationType.WaterStorageTank:
                    return waterStorageTankPlacementOffset;
                case ClimateWorldMitigationType.ShadeNet:
                    return shadeNetPlacementOffset;
                case ClimateWorldMitigationType.Windbreak:
                    return windbreakPlacementOffset;
                case ClimateWorldMitigationType.Greenhouse:
                    return greenhousePlacementOffset;
                case ClimateWorldMitigationType.DrainageCanal:
                    return drainageCanalPlacementOffset;
                default:
                    return Vector3.zero;
            }
        }

        private void EnsureCollider(GameObject go)
        {
            Collider col = go.GetComponentInChildren<Collider>();
            if (col == null)
            {
                BoxCollider fallback = go.AddComponent<BoxCollider>();
                fallback.size = Vector3.one;
            }
        }

        private void EnsurePestDiseaseComponent(GameObject go)
        {
            if (go == null)
                return;

            if (go.GetComponent<PestDiseaseAffectedCrop>() == null)
                go.AddComponent<PestDiseaseAffectedCrop>();
        }

        private void SnapObjectBottomToTerrain(GameObject go, Terrain terrain, float yOffset,
            float buryFraction = 0f)
        {
            if (go == null || terrain == null)
                return;

            Vector3 pos = go.transform.position;
            float groundY = terrain.SampleHeight(pos) + terrain.transform.position.y;

            if (TryGetCombinedRendererBounds(go, out Bounds bounds))
            {
                float moveY = (groundY + yOffset) - bounds.min.y;
                go.transform.position += new Vector3(0f, moveY, 0f);

                if (buryFraction > 0f)
                {
                    float sink = bounds.size.y * Mathf.Clamp01(buryFraction);
                    go.transform.position -= new Vector3(0f, sink, 0f);
                }
            }
            else
            {
                pos.y = groundY + yOffset;
                go.transform.position = pos;
            }
        }

        private bool TryGetCombinedRendererBounds(GameObject go, out Bounds bounds)
        {
            bounds = new Bounds();

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

            if (renderers == null || renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        private PlantGrowthVisualSet MakeVisualSet(
    GameObject sprout,
    GameObject secondStage,
    GameObject adult,
    float sproutYOffset = 0f,
    float secondStageYOffset = 0f,
    float adultYOffset = 0f)
        {
            return new PlantGrowthVisualSet
            {
                sproutPrefab = sprout,
                secondStagePrefab = secondStage,
                adultPrefab = adult,
                sproutYOffset = sproutYOffset,
                secondStageYOffset = secondStageYOffset,
                adultYOffset = adultYOffset
            };
        }

        private bool HasAnyVisualPrefab(PlantGrowthVisualSet set)
        {
            return set != null &&
                   (set.sproutPrefab != null || set.secondStagePrefab != null || set.adultPrefab != null);
        }

        private GameObject CreatePlantRoot(string name, Vector3 position)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;
            root.transform.rotation = Quaternion.identity;
            return root;
        }

        private void AddGrowthVisuals(GameObject root, PlantGrowthVisualSet visualSet)
        {
            GrowthStageVisualController visualController = root.GetComponent<GrowthStageVisualController>();
            if (visualController == null)
                visualController = root.AddComponent<GrowthStageVisualController>();

            visualController.visuals = visualSet;
            visualController.ForceRefresh();

            EnsureCropTapTarget(root);
        }

        private void EnsureCropTapTarget(GameObject root)
        {
            if (root == null || cropTapRadius <= 0f || cropTapHeight <= 0f)
                return;

            CapsuleCollider tapTarget = root.GetComponent<CapsuleCollider>();
            if (tapTarget == null)
                tapTarget = root.AddComponent<CapsuleCollider>();

            tapTarget.isTrigger = true;
            tapTarget.direction = 1;
            tapTarget.radius = cropTapRadius;
            tapTarget.height = cropTapHeight;

            Terrain terrain = TemporaryTerrainGenerator.ResolveActiveTerrain();
            float localGround = terrain != null && terrain.terrainData != null
                ? terrain.SampleHeight(root.transform.position) + terrain.transform.position.y - root.transform.position.y
                : 0f;
            tapTarget.center = new Vector3(0f, localGround + cropTapHeight * 0.5f, 0f);
        }

        private GameObject GetTropicalSproutPrefab(TropicalCropKind cropKind)
        {
            return cropKind switch
            {
                TropicalCropKind.Durian => durianSproutPrefab,
                TropicalCropKind.Pomelo => pomeloSproutPrefab,
                TropicalCropKind.Cacao => cacaoSproutPrefab,
                TropicalCropKind.Pineapple => pineappleSproutPrefab,
                TropicalCropKind.Mangosteen => mangosteenSproutPrefab,
                TropicalCropKind.Mango => mangoSproutPrefab,
                TropicalCropKind.Corn => cornSproutPrefab,
                TropicalCropKind.Eggplant => eggplantSproutPrefab,
                TropicalCropKind.Squash => squashSproutPrefab,
                TropicalCropKind.Strawberry => strawberrySproutPrefab,
                TropicalCropKind.Tomato => tomatoSproutPrefab,
                _ => null
            };
        }

        private float GetTropicalSproutYOffset(TropicalCropKind cropKind)
        {
            return cropKind switch
            {
                TropicalCropKind.Durian => durianSproutYOffset,
                TropicalCropKind.Pomelo => pomeloSproutYOffset,
                TropicalCropKind.Cacao => cacaoSproutYOffset,
                TropicalCropKind.Pineapple => pineappleSproutYOffset,
                TropicalCropKind.Mangosteen => mangosteenSproutYOffset,
                TropicalCropKind.Mango => mangoSproutYOffset,
                TropicalCropKind.Corn => cornSproutYOffset,
                TropicalCropKind.Eggplant => eggplantSproutYOffset,
                TropicalCropKind.Squash => squashSproutYOffset,
                TropicalCropKind.Strawberry => strawberrySproutYOffset,
                TropicalCropKind.Tomato => tomatoSproutYOffset,
                _ => 0f
            };
        }

        private float GetTropicalSecondStageYOffset(TropicalCropKind cropKind)
        {
            return cropKind switch
            {
                TropicalCropKind.Durian => durianSecondStageYOffset,
                TropicalCropKind.Pomelo => pomeloSecondStageYOffset,
                TropicalCropKind.Cacao => cacaoSecondStageYOffset,
                TropicalCropKind.Pineapple => pineappleSecondStageYOffset,
                TropicalCropKind.Mangosteen => mangosteenSecondStageYOffset,
                TropicalCropKind.Mango => mangoSecondStageYOffset,
                TropicalCropKind.Corn => cornSecondStageYOffset,
                TropicalCropKind.Eggplant => eggplantSecondStageYOffset,
                TropicalCropKind.Squash => squashSecondStageYOffset,
                TropicalCropKind.Strawberry => strawberrySecondStageYOffset,
                TropicalCropKind.Tomato => tomatoSecondStageYOffset,
                _ => 0f
            };
        }

        private GameObject GetTropicalSecondStagePrefab(TropicalCropKind cropKind)
        {
            return cropKind switch
            {
                TropicalCropKind.Durian => durianSecondStagePrefab,
                TropicalCropKind.Pomelo => pomeloSecondStagePrefab,
                TropicalCropKind.Cacao => cacaoSecondStagePrefab,
                TropicalCropKind.Pineapple => pineappleSecondStagePrefab,
                TropicalCropKind.Mangosteen => mangosteenSecondStagePrefab,
                TropicalCropKind.Mango => mangoSecondStagePrefab,
                TropicalCropKind.Corn => cornSecondStagePrefab,
                TropicalCropKind.Eggplant => eggplantSecondStagePrefab,
                TropicalCropKind.Squash => squashSecondStagePrefab,
                TropicalCropKind.Strawberry => strawberrySecondStagePrefab,
                TropicalCropKind.Tomato => tomatoSecondStagePrefab,
                _ => null
            };
        }

    }
}


