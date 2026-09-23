using System;
using UnityEngine;
namespace AgriDabao3D
{
    public static class CropPersistenceMapper
    {
        public static CropSaveDto Capture(CoconutTreeInstance crop)
        {
            CropSaveDto save = CaptureCommon(
                "Coconut",
                crop.cropId,
                crop.cropName,
                crop.districtName,
                crop.visualSoilType,
                crop.plantedSoil,
                crop.transform,
                crop.soilSuitability,
                crop.drainage,
                crop.fertility,
                crop.moisture,
                crop.health,
                crop.stress,
                crop.averageHealth,
                crop.currentTemperatureC,
                crop.temperatureSuitability,
                crop.stage.ToString(),
                crop.plantedGameDay,
                crop.lastWateredGameDay,
                crop.nextProductionGameDay,
                crop.availableHarvestCount);
            CaptureDiseases(crop.gameObject, save);
            CaptureMaintenance(crop.gameObject, save);
            return save;
        }
        public static CropSaveDto Capture(BananaPlantInstance crop)
        {
            CropSaveDto save = CaptureCommon(
                "Banana",
                crop.cropId,
                crop.cropName,
                crop.districtName,
                crop.visualSoilType,
                crop.plantedSoil,
                crop.transform,
                crop.soilSuitability,
                crop.drainage,
                crop.fertility,
                crop.moisture,
                crop.health,
                crop.stress,
                crop.averageHealth,
                crop.currentTemperatureC,
                crop.temperatureSuitability,
                crop.stage.ToString(),
                crop.plantedGameDay,
                crop.lastWateredGameDay,
                crop.nextProductionGameDay,
                crop.availableBananaCount);
            if (crop.storedBulbs != null)
            {
                foreach (BananaBulbHarvest bulb in crop.storedBulbs)
                {
                    save.bananaBulbs.Add(new BananaBulbHarvestSaveDto
                    {
                        bananaCount = bulb.bananaCount,
                        producedGameDay = bulb.producedGameDay
                    });
                }
            }
            CaptureDiseases(crop.gameObject, save);
            CaptureMaintenance(crop.gameObject, save);
            return save;
        }
        public static CropSaveDto Capture(TropicalCropPlantInstance crop)
        {
            CropSaveDto save = CaptureCommon(
                "Tropical",
                crop.cropId,
                crop.cropName,
                crop.districtName,
                crop.visualSoilType,
                crop.plantedSoil,
                crop.transform,
                crop.soilSuitability,
                crop.drainage,
                crop.fertility,
                crop.moisture,
                crop.health,
                crop.stress,
                crop.averageHealth,
                crop.currentTemperatureC,
                crop.temperatureSuitability,
                crop.stage.ToString(),
                crop.plantedGameDay,
                crop.lastWateredGameDay,
                crop.nextProductionGameDay,
                crop.availableHarvestCount);
            save.tropicalCropKind = crop.cropKind.ToString();
            if (crop.storedBundles != null)
            {
                foreach (TropicalBundleHarvest bundle in crop.storedBundles)
                {
                    save.tropicalBundles.Add(new TropicalBundleHarvestSaveDto
                    {
                        itemCount = bundle.itemCount,
                        producedGameDay = bundle.producedGameDay
                    });
                }
            }
            CaptureDiseases(crop.gameObject, save);
            CaptureMaintenance(crop.gameObject, save);
            return save;
        }
        private static CropSaveDto CaptureCommon(
            string family,
            string cropId,
            string cropName,
            string districtName,
            string visualSoilType,
            SoilSample plantedSoil,
            Transform transform,
            float soilSuitability,
            float drainage,
            float fertility,
            float moisture,
            float health,
            float stress,
            float averageHealth,
            float currentTemperatureC,
            float temperatureSuitability,
            string stage,
            float plantedGameDay,
            float lastWateredGameDay,
            float nextProductionGameDay,
            int availableHarvestCount)
        {
            return new CropSaveDto
            {
                cropFamily = family,
                cropId = cropId,
                cropName = cropName,
                districtName = districtName,
                visualSoilType = visualSoilType,
                plantedSoil = CloneSoil(plantedSoil),
                position = new SerializableVector3(transform.position),
                rotation = new SerializableQuaternion(transform.rotation),
                soilSuitability = soilSuitability,
                drainage = drainage,
                fertility = fertility,
                moisture = moisture,
                health = health,
                stress = stress,
                averageHealth = averageHealth,
                currentTemperatureC = currentTemperatureC,
                temperatureSuitability = temperatureSuitability,
                stage = stage,
                plantedGameDay = plantedGameDay,
                lastWateredGameDay = lastWateredGameDay,
                nextProductionGameDay = nextProductionGameDay,
                availableHarvestCount = availableHarvestCount
            };
        }
        private static void CaptureDiseases(GameObject cropObject, CropSaveDto save)
        {
            PestDiseaseAffectedCrop affected = cropObject.GetComponent<PestDiseaseAffectedCrop>();
            if (affected == null || affected.activeConditions == null)
                return;
            foreach (ActivePestDisease condition in affected.activeConditions)
            {
                if (condition == null) continue;
                save.activeConditions.Add(new PestConditionSaveDto
                {
                    type = condition.type.ToString(),
                    severity = condition.severity,
                    targetSeverity = condition.targetSeverity,
                    startedGameDay = condition.startedGameDay
                });
            }
        }
        public static void Restore(CoconutTreeInstance crop, CropSaveDto save)
        {
            if (crop == null || save == null) return;
            RestoreCommon(crop.transform, save);
            crop.cropId = save.cropId;
            crop.cropName = save.cropName;
            crop.districtName = save.districtName;
            crop.visualSoilType = save.visualSoilType;
            crop.plantedSoil = CloneSoil(save.plantedSoil);
            crop.soilSuitability = save.soilSuitability;
            crop.drainage = save.drainage;
            crop.fertility = save.fertility;
            crop.moisture = save.moisture;
            crop.health = save.health;
            crop.stress = save.stress;
            crop.averageHealth = save.averageHealth;
            crop.currentTemperatureC = save.currentTemperatureC;
            crop.temperatureSuitability = save.temperatureSuitability;
            crop.plantedGameDay = save.plantedGameDay;
            crop.lastWateredGameDay = save.lastWateredGameDay;
            crop.nextProductionGameDay = save.nextProductionGameDay;
            crop.availableHarvestCount = save.availableHarvestCount;
            if (string.IsNullOrEmpty(crop.cropName)) crop.cropName = CropNaming.NextName("Coconut");
            if (Enum.TryParse(save.stage, out CoconutStage stage))
                crop.stage = stage;
            RestoreDiseases(crop.gameObject, save);
            RestoreMaintenance(crop.gameObject, save);
        }
        public static void Restore(BananaPlantInstance crop, CropSaveDto save)
        {
            if (crop == null || save == null) return;
            RestoreCommon(crop.transform, save);
            crop.cropId = save.cropId;
            crop.cropName = save.cropName;
            crop.districtName = save.districtName;
            crop.visualSoilType = save.visualSoilType;
            crop.plantedSoil = CloneSoil(save.plantedSoil);
            crop.soilSuitability = save.soilSuitability;
            crop.drainage = save.drainage;
            crop.fertility = save.fertility;
            crop.moisture = save.moisture;
            crop.health = save.health;
            crop.stress = save.stress;
            crop.averageHealth = save.averageHealth;
            crop.currentTemperatureC = save.currentTemperatureC;
            crop.temperatureSuitability = save.temperatureSuitability;
            crop.plantedGameDay = save.plantedGameDay;
            crop.lastWateredGameDay = save.lastWateredGameDay;
            crop.nextProductionGameDay = save.nextProductionGameDay;
            crop.availableBananaCount = save.availableHarvestCount;
            if (string.IsNullOrEmpty(crop.cropName)) crop.cropName = CropNaming.NextName("Banana");
            if (Enum.TryParse(save.stage, out BananaStage stage))
                crop.stage = stage;
            crop.storedBulbs.Clear();
            if (save.bananaBulbs != null)
            {
                foreach (BananaBulbHarvestSaveDto bulb in save.bananaBulbs)
                {
                    crop.storedBulbs.Add(new BananaBulbHarvest
                    {
                        bananaCount = bulb.bananaCount,
                        producedGameDay = bulb.producedGameDay
                    });
                }
            }
            RestoreDiseases(crop.gameObject, save);
            RestoreMaintenance(crop.gameObject, save);
        }
        public static void Restore(TropicalCropPlantInstance crop, CropSaveDto save)
        {
            if (crop == null || save == null) return;
            RestoreCommon(crop.transform, save);
            crop.cropId = save.cropId;
            crop.cropName = save.cropName;
            crop.districtName = save.districtName;
            crop.visualSoilType = save.visualSoilType;
            crop.plantedSoil = CloneSoil(save.plantedSoil);
            crop.soilSuitability = save.soilSuitability;
            crop.drainage = save.drainage;
            crop.fertility = save.fertility;
            crop.moisture = save.moisture;
            crop.health = save.health;
            crop.stress = save.stress;
            crop.averageHealth = save.averageHealth;
            crop.currentTemperatureC = save.currentTemperatureC;
            crop.temperatureSuitability = save.temperatureSuitability;
            crop.plantedGameDay = save.plantedGameDay;
            crop.lastWateredGameDay = save.lastWateredGameDay;
            crop.nextProductionGameDay = save.nextProductionGameDay;
            crop.availableHarvestCount = save.availableHarvestCount;
            if (Enum.TryParse(save.tropicalCropKind, out TropicalCropKind kind))
                crop.cropKind = kind;
            if (string.IsNullOrEmpty(crop.cropName)) crop.cropName = CropNaming.NextName(crop.CropDisplayName);
            if (Enum.TryParse(save.stage, out TropicalCropStage stage))
                crop.stage = stage;
            crop.storedBundles.Clear();
            if (save.tropicalBundles != null)
            {
                foreach (TropicalBundleHarvestSaveDto bundle in save.tropicalBundles)
                {
                    crop.storedBundles.Add(new TropicalBundleHarvest
                    {
                        itemCount = bundle.itemCount,
                        producedGameDay = bundle.producedGameDay
                    });
                }
            }
            RestoreDiseases(crop.gameObject, save);
            RestoreMaintenance(crop.gameObject, save);
        }
        private static void RestoreCommon(Transform transform, CropSaveDto save)
        {
            transform.position = save.position.ToVector3();
            transform.rotation = save.rotation.ToQuaternion();
        }
        public static void RestoreDiseases(GameObject cropObject, CropSaveDto save)
        {
            PestDiseaseAffectedCrop affected = cropObject.GetComponent<PestDiseaseAffectedCrop>();
            if (affected == null)
                affected = cropObject.AddComponent<PestDiseaseAffectedCrop>();
            affected.activeConditions.Clear();
            foreach (PestConditionSaveDto condition in save.activeConditions)
            {
                if (!Enum.TryParse(condition.type, out PestDiseaseType type))
                    continue;
                affected.activeConditions.Add(new ActivePestDisease
                {
                    type = type,
                    severity = condition.severity,
                    targetSeverity = condition.targetSeverity,
                    startedGameDay = condition.startedGameDay
                });
            }
        }
        private static void CaptureMaintenance(GameObject cropObject, CropSaveDto save)
        {
            CropClimateMaintenanceState state = cropObject.GetComponent<CropClimateMaintenanceState>();
            if (state != null)
                save.maintenance = state.CaptureSaveData();
        }

        private static void RestoreMaintenance(GameObject cropObject, CropSaveDto save)
        {
            if (save == null || save.maintenance == null)
                return;
            CropRuntimeAdapter.TryCreate(cropObject, out CropRuntimeAdapter adapter);
            CropClimateMaintenanceState state = cropObject.GetComponent<CropClimateMaintenanceState>();
            if (state == null) state = cropObject.AddComponent<CropClimateMaintenanceState>();
            if (adapter != null) state.Bind(adapter);
            Terrain terrain = TemporaryTerrainGenerator.ResolveActiveTerrain();
            state.RestoreSaveData(save.maintenance, terrain);
        }

        private static SoilSample CloneSoil(SoilSample source)
        {
            if (source == null) return null;
            return new SoilSample
            {
                sand = source.sand,
                silt = source.silt,
                clay = source.clay,
                phh2o = source.phh2o,
                soc = source.soc,
                cfvo = source.cfvo,
                bdod = source.bdod,
                nitrogen = source.nitrogen
            };
        }
    }
}
