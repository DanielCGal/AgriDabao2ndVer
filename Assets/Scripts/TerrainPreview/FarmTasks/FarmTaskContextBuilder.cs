using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AgriDabao3D
{
    public static class FarmTaskContextBuilder
    {
        private static readonly HashSet<InventoryItemType> SellableCrops =
            new HashSet<InventoryItemType>
            {
                InventoryItemType.Coconut,
                InventoryItemType.Banana,
                InventoryItemType.Durian,
                InventoryItemType.Pomelo,
                InventoryItemType.Cacao,
                InventoryItemType.Pineapple,
                InventoryItemType.Mangosteen,
                InventoryItemType.Mango,
                InventoryItemType.Corn,
                InventoryItemType.Eggplant,
                InventoryItemType.Squash,
                InventoryItemType.Strawberry,
                InventoryItemType.Tomato
            };

        public static FarmTaskContextPayload Capture()
        {
            FarmTaskContextPayload payload = new FarmTaskContextPayload();
            GameTimeSystem time = GameTimeSystem.Instance;
            WeatherSystem weather = WeatherSystem.Instance;
            PlayerInventory inventory = PlayerInventory.Instance;

            payload.district = SelectedAreaState.SelectedDistrictName;
            if (time != null)
            {
                payload.gameDay = time.TotalGameDays;
                payload.year = time.CurrentYearNumber;
                payload.month = time.CurrentMonthNumber;
                payload.day = time.CurrentDayInMonth;
                payload.time = time.GetClockText();
            }

            if (weather != null)
            {
                payload.weather = weather.currentEvent.ToString();
                payload.temperatureC = weather.currentTemperatureC;
                payload.humidity = weather.currentHumidity;
                payload.rainIntensity = weather.currentRainIntensity;
                payload.remainingWeatherDays = weather.remainingEventDays;
            }
            else
            {
                payload.weather = WeatherEventType.Clear.ToString();
            }

            if (inventory != null)
            {
                payload.money = inventory.money;
                foreach (InventoryItemType item in Enum.GetValues(typeof(InventoryItemType)))
                {
                    if (item == InventoryItemType.None)
                        continue;
                    int amount = inventory.GetCount(item);
                    if (amount <= 0)
                        continue;
                    payload.inventory.Add(new FarmTaskInventoryContext
                    {
                        itemType = item.ToString(),
                        amount = amount
                    });
                }
            }

            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                if (crop == null || crop.Root == null)
                    continue;

                FarmTaskCropContext cropContext = new FarmTaskCropContext
                {
                    cropId = crop.CropId,
                    cropName = crop.CropName,
                    cropType = crop.CropDisplayName,
                    district = crop.District,
                    stage = crop.Stage,
                    health = crop.Health,
                    averageHealth = crop.AverageHealth,
                    stress = crop.Stress,
                    moisture = crop.Moisture,
                    fertility = crop.Fertility,
                    drainage = crop.Drainage,
                    soilSuitability = crop.SoilSuitability,
                    harvestReady = IsHarvestReady(crop),
                    availableHarvest = GetAvailableHarvest(crop)
                };

                CropClimateMaintenanceState maintenance =
                    crop.Root.GetComponent<CropClimateMaintenanceState>();
                cropContext.maintenanceState = maintenance != null
                    ? maintenance.GetInspectionText()
                    : "None";
                cropContext.plantingMaterial = crop.PlantingMaterial;

                PestDiseaseAffectedCrop affected =
                    crop.Root.GetComponent<PestDiseaseAffectedCrop>();
                if (affected != null && affected.activeConditions != null)
                {
                    foreach (ActivePestDisease condition in affected.activeConditions)
                    {
                        if (condition == null || condition.severity <= 0.01f)
                            continue;
                        FarmTaskConditionContext conditionContext =
                            new FarmTaskConditionContext
                            {
                                type = condition.type.ToString(),
                                severity = condition.severity,
                                targetSeverity = condition.targetSeverity
                            };

                        PestDiseaseDatabase database =
                            PestDiseaseSystem.Instance != null
                                ? PestDiseaseSystem.Instance.Database
                                : null;
                        PestDiseaseRule rule = database != null
                            ? database.GetRule(crop.CropType, condition.type)
                            : null;
                        if (rule != null && rule.mitigations != null)
                        {
                            foreach (PestDiseaseMitigation mitigation in rule.mitigations)
                                conditionContext.validMitigations.Add(mitigation.ToString());
                        }
                        cropContext.conditions.Add(conditionContext);
                    }
                }

                payload.crops.Add(cropContext);
            }

            foreach (AphidTrapInstance trap in
                     Object.FindObjectsByType<AphidTrapInstance>(
                         FindObjectsSortMode.None))
            {
                if (trap == null)
                    continue;
                payload.worldObjects.Add(new FarmTaskWorldObjectContext
                {
                    objectType = "AphidTrap",
                    mitigationType = PestDiseaseMitigation.AphidTrap.ToString(),
                    radius = trap.radius,
                    currentLoad = trap.deadAphidLoadPercent,
                    capacity = trap.capacityPercent
                });
            }

            foreach (AreaMitigationTrapInstance trap in
                     Object.FindObjectsByType<AreaMitigationTrapInstance>(
                         FindObjectsSortMode.None))
            {
                if (trap == null)
                    continue;
                payload.worldObjects.Add(new FarmTaskWorldObjectContext
                {
                    objectType = "AreaMitigationTrap",
                    mitigationType = trap.mitigation.ToString(),
                    radius = trap.radius,
                    currentLoad = trap.capturedLoadPercent,
                    capacity = trap.capacityPercent
                });
            }

            foreach (ClimateMitigationWorldObject item in
                     Object.FindObjectsByType<ClimateMitigationWorldObject>(
                         FindObjectsSortMode.None))
            {
                if (item == null)
                    continue;
                payload.worldObjects.Add(new FarmTaskWorldObjectContext
                {
                    objectType = "ClimateMitigation",
                    mitigationType = item.mitigationType.ToString(),
                    radius = item.radius,
                    storedResource = item.storedResource,
                    resourceCapacity = item.resourceCapacity
                });
            }

            NurserySystem nursery = NurserySystem.Instance;
            if (nursery != null)
            {
                for (int slot = 0; slot < NurserySystem.BagCount; slot++)
                {
                    SeedlingBagStatus status = nursery.GetStatus(slot);
                    if (status == SeedlingBagStatus.Empty)
                        continue;

                    bool sown = nursery.TryGetMaterial(slot, out PlantingMaterialInfo info);
                    payload.seedlingTent.Add(new FarmTaskNurseryBagContext
                    {
                        bag = slot + 1,
                        status = status.ToString(),
                        plantingMaterial = sown ? info.Item.ToString() : null,
                        cropType = sown ? info.Crop.ToString() : null,
                        daysUntilNextStep = nursery.DaysUntilNextStep(slot)
                    });
                }
            }

            foreach (DigSpot spot in Object.FindObjectsByType<DigSpot>(FindObjectsSortMode.None))
            {
                if (spot == null || spot.occupied)
                    continue;

                payload.preparedGround.Add(new FarmTaskPreparedGroundContext
                {
                    preparation = spot.plotKind.ToString(),
                    mulched = spot.mulched
                });
            }

            return payload;
        }

        public static List<CropSnapshot> CaptureCropSnapshots()
        {
            List<CropSnapshot> result = new List<CropSnapshot>();
            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                if (crop == null || crop.Root == null)
                    continue;
                CropClimateMaintenanceState maintenance =
                    crop.Root.GetComponent<CropClimateMaintenanceState>();
                result.Add(new CropSnapshot
                {
                    cropId = crop.CropId,
                    cropType = crop.CropDisplayName,
                    district = crop.District,
                    stage = crop.Stage,
                    health = crop.Health,
                    averageHealth = crop.AverageHealth,
                    stress = crop.Stress,
                    moisture = crop.Moisture,
                    fertility = crop.Fertility,
                    drainage = crop.Drainage,
                    soilSuitability = crop.SoilSuitability,
                    maintenanceState = maintenance != null
                        ? maintenance.GetInspectionText()
                        : "None"
                });
            }
            return result;
        }

        public static bool IsHarvestReady(CropRuntimeAdapter crop)
        {
            if (crop == null || crop.Root == null)
                return false;
            CoconutTreeInstance coconut =
                crop.Root.GetComponent<CoconutTreeInstance>();
            if (coconut != null)
                return coconut.CanHarvest();
            BananaPlantInstance banana =
                crop.Root.GetComponent<BananaPlantInstance>();
            if (banana != null)
                return banana.CanHarvest();
            TropicalCropPlantInstance tropical =
                crop.Root.GetComponent<TropicalCropPlantInstance>();
            return tropical != null && tropical.CanHarvest();
        }

        public static int GetAvailableHarvest(CropRuntimeAdapter crop)
        {
            if (crop == null || crop.Root == null)
                return 0;
            CoconutTreeInstance coconut =
                crop.Root.GetComponent<CoconutTreeInstance>();
            if (coconut != null)
                return Mathf.Max(0, coconut.availableHarvestCount);
            BananaPlantInstance banana =
                crop.Root.GetComponent<BananaPlantInstance>();
            if (banana != null)
                return Mathf.Max(0, banana.availableBananaCount);
            TropicalCropPlantInstance tropical =
                crop.Root.GetComponent<TropicalCropPlantInstance>();
            if (tropical != null)
                return Mathf.Max(0, tropical.availableHarvestCount);
            return 0;
        }

        public static bool IsSellableCropItem(InventoryItemType item)
        {
            return SellableCrops.Contains(item);
        }

        public static InventoryItemType CropNameToFruitItem(string cropType)
        {
            if (string.IsNullOrWhiteSpace(cropType))
                return InventoryItemType.None;
            return Enum.TryParse(cropType, true, out InventoryItemType item) &&
                   IsSellableCropItem(item)
                ? item
                : InventoryItemType.None;
        }

        public static List<InventoryItemType> CropNameToPlantingMaterials(string cropType)
        {
            return PlantingMaterialCatalog.TryGetCropType(cropType, out FarmCropType crop)
                ? PlantingMaterialCatalog.MaterialsFor(crop)
                : new List<InventoryItemType>();
        }

        public static float GetTotalConditionSeverity()
        {
            float total = 0f;
            foreach (PestDiseaseAffectedCrop crop in
                     Object.FindObjectsByType<PestDiseaseAffectedCrop>(
                         FindObjectsSortMode.None))
            {
                if (crop == null || crop.activeConditions == null)
                    continue;
                foreach (ActivePestDisease condition in crop.activeConditions)
                {
                    if (condition != null)
                        total += Mathf.Max(0f, condition.severity);
                }
            }
            return total;
        }

        public static float GetConditionSeverity(
            string cropId,
            string conditionType)
        {
            if (string.IsNullOrWhiteSpace(conditionType) ||
                !Enum.TryParse(conditionType, true, out PestDiseaseType type))
            {
                return 0f;
            }

            foreach (PestDiseaseAffectedCrop crop in
                     Object.FindObjectsByType<PestDiseaseAffectedCrop>(
                         FindObjectsSortMode.None))
            {
                if (crop == null)
                    continue;
                if (!string.IsNullOrWhiteSpace(cropId) &&
                    !string.Equals(crop.CropId, cropId, StringComparison.Ordinal))
                {
                    continue;
                }
                return crop.GetConditionSeverity(type);
            }
            return 0f;
        }
    }
}
