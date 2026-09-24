using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public class TropicalCropPlantInstance : MonoBehaviour
    {
        [Header("Crop")]
        public TropicalCropKind cropKind = TropicalCropKind.Durian;

        [Header("Soil Snapshot")]
        public string districtName;
        public string visualSoilType;
        public SoilSample plantedSoil;
        public string cropId;
        public string cropName;

        [Header("Derived Soil Scores")]
        [Range(0f, 1f)] public float soilSuitability;
        [Range(0f, 1f)] public float drainage;
        [Range(0f, 1f)] public float fertility;

        [Header("Live Plant Stats")]
        [Range(0f, 1f)] public float moisture = 0.65f;
        [Range(0f, 100f)] public float health = 70f;
        [Range(0f, 100f)] public float stress = 8f;
        [Range(0f, 100f)] public float averageHealth = 70f;

        [Header("Climate")]
        public float currentTemperatureC = 27f;

        [Range(0f, 1f)]
        public float temperatureSuitability = 1f;

        [Header("Growth")]
        public TropicalCropStage stage = TropicalCropStage.Seedling;
        public float plantedGameDay;
        public float lastWateredGameDay;
        public int availableHarvestCount;
        public List<TropicalBundleHarvest> storedBundles = new List<TropicalBundleHarvest>();
        public float nextProductionGameDay;

        [Header("Planting")]
        [Tooltip("The planting material this crop grew from, saved by item name.")]
        public string plantingMaterial;

        [Tooltip("Game days it spent in the Seedling Tent before transplanting.")]
        public float nurseryDays;

        [Tooltip("Game day it went into the field; -1 for crops planted before this was recorded.")]
        public float fieldPlantedGameDay = -1f;

        public string CropDisplayName => TropicalCropCatalog.GetDisplayName(cropKind);
        public TropicalHarvestMode HarvestMode => TropicalCropCatalog.GetHarvestMode(cropKind);
        public InventoryItemType FruitItemType => TropicalCropCatalog.GetFruitItem(cropKind);
        public InventoryItemType SeedItemType => TropicalCropCatalog.GetSeedItem(cropKind);

        private bool initialized;
        private TropicalCropStage lastLoggedStage;

        private const float WeatherStressFullScalePerDay = 40f;

        private const float ExternalEffectTimeConstantDays = 2f;

        private const float YoungCacaoSunStressPerDay = 6f;

        private float simulatedStress;
        private float simulatedHealth;
        private bool simulationBaselineReady;

        public void Initialize(SoilSample soil, string district)
        {
            cropId = Guid.NewGuid().ToString("N");

            plantedSoil = soil;
            districtName = district;
            visualSoilType = soil != null ? soil.GetVisualSoilType() : "Unknown";

            drainage = soil != null ? SoilGameplayRules.GetDrainage(soil) : 0.60f;
            fertility = soil != null ? SoilGameplayRules.GetFertility(soil) : 0.50f;
            soilSuitability = soil != null ? TropicalCropCatalog.CalculateSoilSuitability(cropKind, soil) : 0.50f;

            plantedGameDay = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : 0f;
            lastWateredGameDay = plantedGameDay;
            nextProductionGameDay = plantedGameDay + TropicalCropCatalog.GetFirstHarvestDelayDays(cropKind);

            float idealMin = TropicalCropCatalog.GetIdealMoistureMin(cropKind);
            float idealMax = TropicalCropCatalog.GetIdealMoistureMax(cropKind);
            moisture = Mathf.Lerp(idealMin, idealMax, 0.55f);

            health = 50f + soilSuitability * 30f;
            averageHealth = health;
            stress = 6f;

            if (WeatherSystem.Instance != null)
            {
                currentTemperatureC =
                    WeatherSystem.Instance.currentTemperatureC;

                temperatureSuitability =
                    CropClimateRules.GetTemperatureSuitability(
                        CropClimateRules.GetFarmCropType(cropKind),
                        currentTemperatureC
                    );
            }

            stage = GetStage();
            lastLoggedStage = stage;
            initialized = true;
        }

        public void ApplyStartingAge(float ageDays)
        {
            if (ageDays <= 0f)
                return;

            plantedGameDay -= ageDays;
            nextProductionGameDay -= ageDays;
            stage = GetStage();
            lastLoggedStage = stage;
        }

        public bool NeedsShade =>
            cropKind == TropicalCropKind.Cacao &&
            stage < TropicalCropStage.PreFruiting;

        public bool IsShaded => ClimateMitigationWorldObject.IsShadeOver(transform.position);

        private void Start()
        {
            if (!initialized && plantedSoil != null)
                Initialize(plantedSoil, districtName);
        }

        private void Update()
        {
            if (!initialized || GameTimeSystem.Instance == null)
                return;

            Simulate(GameTimeSystem.Instance.DeltaGameDays);
        }

        public void SimulateTimeSkip(float skippedGameDays)
        {
            if (!initialized || skippedGameDays <= 0f)
                return;

            Simulate(skippedGameDays);
        }

        private void Simulate(float deltaGameDays)
        {
            TropicalCropStage previousStage = stage;
            stage = GetStage();

            if (!simulationBaselineReady)
            {
                simulatedStress = stress;
                simulatedHealth = health;
                simulationBaselineReady = true;
            }

            float externalStress = stress - simulatedStress;
            float externalHealth = health - simulatedHealth;

            float dryRate = TropicalCropCatalog.GetBaseDryRate(cropKind);
            dryRate *= Mathf.Lerp(1.15f, 0.85f, drainage);

            if (stage == TropicalCropStage.Seedling)
                dryRate *= 1.15f;

            if (stage == TropicalCropStage.Old)
                dryRate *= 1.05f;

            float weatherDryMultiplier = 1f;
            float weatherMoistureAdd = 0f;
            float directHealthPenalty = 0f;
            float temperatureStress01 = 0f;

            if (WeatherSystem.Instance != null)
            {
                weatherDryMultiplier = WeatherSystem.Instance.GetDryingMultiplier();
                weatherMoistureAdd = WeatherSystem.Instance.GetMoistureAdditionPerDay();
            }

            moisture = Mathf.Clamp01(
                moisture -
                dryRate * weatherDryMultiplier * deltaGameDays +
                weatherMoistureAdd * deltaGameDays
            );

            if (WeatherSystem.Instance != null)
            {
                currentTemperatureC =
                    WeatherSystem.Instance.currentTemperatureC;

                FarmCropType farmCropType =
                    CropClimateRules.GetFarmCropType(cropKind);

                temperatureSuitability =
                    CropClimateRules.GetTemperatureSuitability(
                        farmCropType,
                        currentTemperatureC
                    );

                temperatureStress01 =
                    1f - temperatureSuitability;
            }
            else
            {
                temperatureSuitability = 1f;
                temperatureStress01 = 0f;
            }

            float waterScore = GetWaterScore();

            float idealDrainage = TropicalCropCatalog.GetIdealDrainage(cropKind);
            float drainageFit = 1f - Mathf.Clamp01(Mathf.Abs(drainage - idealDrainage) / 0.70f);

            float weatherStressPerDay = 0f;

            if (WeatherSystem.Instance != null)
            {
                weatherStressPerDay = WeatherSystem.Instance.GetEventStressPerDay();

                if (WeatherSystem.Instance.currentEvent == WeatherEventType.ExtremeDrought)
                    weatherStressPerDay += TropicalCropCatalog.GetDroughtStress(cropKind);

                if (WeatherSystem.Instance.currentEvent == WeatherEventType.Typhoon)
                {
                    weatherStressPerDay += TropicalCropCatalog.GetTyphoonStress(cropKind);
                    directHealthPenalty += Mathf.Lerp(2f, 8f, TropicalCropCatalog.GetTyphoonStress(cropKind) / 12f);
                }

                if (WeatherSystem.Instance.currentEvent == WeatherEventType.Rain &&
                    moisture < TropicalCropCatalog.GetIdealMoistureMin(cropKind))
                {
                    weatherStressPerDay -= 2f;
                }
            }

            bool waterlogged =
                moisture > TropicalCropCatalog.GetIdealMoistureMax(cropKind) + 0.06f &&
                drainage < 0.55f;

            if (waterlogged)
                weatherStressPerDay += 8f;

            if (NeedsShade && !IsShaded)
                weatherStressPerDay += YoungCacaoSunStressPerDay;

            float targetStress01 =
                (1f - waterScore) * 0.35f +
                (1f - fertility) * 0.18f +
                (1f - soilSuitability) * 0.15f +
                (1f - drainageFit) * 0.15f +
                temperatureStress01 * 0.30f +
                weatherStressPerDay / WeatherStressFullScalePerDay;

            float ageModifier = stage switch
            {
                TropicalCropStage.Seedling => -6f,
                TropicalCropStage.Vegetative => 4f,
                TropicalCropStage.PreFruiting => 8f,
                TropicalCropStage.Fruiting => 10f,
                TropicalCropStage.Old => -10f,
                _ => 0f
            };

            float temperatureHealthPenalty =
                temperatureStress01 * 30f;

            float targetHealth =
                50f +
                Mathf.Lerp(-20f, 22f, soilSuitability) +
                Mathf.Lerp(-22f, 20f, waterScore) +
                Mathf.Lerp(-16f, 18f, fertility) +
                Mathf.Lerp(-12f, 10f, drainageFit) +
                ageModifier -
                directHealthPenalty -
                temperatureHealthPenalty;

            simulatedHealth = Mathf.Clamp(
                Mathf.MoveTowards(simulatedHealth, targetHealth, 22f * deltaGameDays),
                0f,
                100f
            );

            simulatedStress = Mathf.Clamp(
                Mathf.MoveTowards(simulatedStress, targetStress01 * 100f, 36f * deltaGameDays),
                0f,
                100f
            );

            float externalFade = Mathf.Exp(-deltaGameDays / ExternalEffectTimeConstantDays);
            externalHealth *= externalFade;
            externalStress *= externalFade;

            health = Mathf.Clamp(simulatedHealth + externalHealth, 0f, 100f);
            stress = Mathf.Clamp(simulatedStress + externalStress, 0f, 100f);

            float avgBlend = 1f - Mathf.Exp(-deltaGameDays * 0.35f);
            averageHealth = Mathf.Lerp(averageHealth, health, avgBlend);

            stage = GetStage();

            if (stage != previousStage)
                LogStageChange(previousStage, stage);

            TryProduceHarvest();
        }

        private void LogStageChange(TropicalCropStage previousStage, TropicalCropStage currentStage)
        {
            if (currentStage == lastLoggedStage)
                return;

            Debug.Log(
                $"[{CropDisplayName}] Stage changed | District={districtName} | " +
                $"From={previousStage} | To={currentStage} | AgeYears={GetAgeYears():F2} | " +
                $"Day={GetCurrentAbsoluteDay()}"
            );

            lastLoggedStage = currentStage;
        }

        public void Water(float amount)
        {
            moisture = Mathf.Clamp01(moisture + amount);
            lastWateredGameDay = GameTimeSystem.Instance != null
                ? GameTimeSystem.Instance.TotalGameDays
                : lastWateredGameDay;

            stress = Mathf.Max(0f, stress - 9f);
        }

        public bool CanHarvest()
        {
            if (stage < TropicalCropStage.PreFruiting)
                return false;

            if (HarvestMode == TropicalHarvestMode.IndividualFruit)
                return availableHarvestCount > 0;

            return storedBundles != null && storedBundles.Count > 0;
        }

        public int HarvestIndividualFruits()
        {
            if (HarvestMode != TropicalHarvestMode.IndividualFruit || !CanHarvest())
                return 0;

            int amount = availableHarvestCount;
            availableHarvestCount = 0;
            stress = Mathf.Clamp(stress + 5f, 0f, 100f);

            return amount;
        }

        public List<TropicalBundleHarvest> HarvestBundles()
        {
            if (HarvestMode != TropicalHarvestMode.BulkBundle || !CanHarvest())
                return null;

            List<TropicalBundleHarvest> result = new List<TropicalBundleHarvest>(storedBundles);
            storedBundles.Clear();
            availableHarvestCount = 0;
            stress = Mathf.Clamp(stress + 5f, 0f, 100f);

            return result;
        }

        public void ForceProduceHarvestForDev()
        {
            if (!initialized)
            {
                SoilSample fallback = plantedSoil ?? new SoilSample
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

                Initialize(
                    fallback,
                    string.IsNullOrWhiteSpace(districtName)
                        ? SelectedAreaState.SelectedDistrictName
                        : districtName
                );
            }

            if (GameTimeSystem.Instance != null)
            {
                float current = GameTimeSystem.Instance.TotalGameDays;
                float firstHarvest = TropicalCropCatalog.GetFirstHarvestDelayDays(cropKind);

                if (current - plantedGameDay < firstHarvest)
                    plantedGameDay = current - firstHarvest - 1f;

                stage = GetStage();
                fieldPlantedGameDay = -1f;
                nextProductionGameDay = Mathf.Min(nextProductionGameDay, current);
            }

            ProduceOneCycle(GameTimeSystem.Instance != null
                ? GameTimeSystem.Instance.TotalGameDays
                : nextProductionGameDay);
        }

        public float GetAgeYears()
        {
            if (GameTimeSystem.Instance == null)
                return 0f;

            float livedDays = GameTimeSystem.Instance.TotalGameDays - plantedGameDay;
            return livedDays / Mathf.Max(1, GameTimeSystem.Instance.DaysPerYear);
        }

        private TropicalCropStage GetStage()
        {
            float current = GameTimeSystem.Instance != null
                ? GameTimeSystem.Instance.TotalGameDays
                : plantedGameDay;

            float livedDays = Mathf.Max(0f, current - plantedGameDay);
            return TropicalCropCatalog.GetStageForAgeDays(cropKind, livedDays);
        }

        private void TryProduceHarvest()
        {
            if (GameTimeSystem.Instance == null)
                return;

            if (stage < TropicalCropStage.PreFruiting)
                return;

            while (GameTimeSystem.Instance.TotalGameDays >= nextProductionGameDay)
            {
                ProduceOneCycle(nextProductionGameDay);
                nextProductionGameDay += TropicalCropCatalog.GetProductionIntervalDays(cropKind);
            }
        }

        private void ProduceOneCycle(float producedGameDay)
        {
            if (HarvestMode == TropicalHarvestMode.IndividualFruit)
            {
                int produced = TropicalCropCatalog.CalculateIndividualYield(
                    cropKind,
                    stage,
                    averageHealth,
                    stress
                );

                int before = availableHarvestCount;
                availableHarvestCount += produced;

                Debug.Log(
                    $"[{CropDisplayName}] Harvest cycle | Mode=IndividualFruit | District={districtName} | " +
                    $"Stage={stage} | Added={produced} | Before={before} | After={availableHarvestCount} | " +
                    $"Health={averageHealth:F1} | Stress={stress:F1}"
                );
            }
            else
            {
                int produced = TropicalCropCatalog.CalculateBulkYield(
                    cropKind,
                    stage,
                    averageHealth,
                    stress
                );

                if (storedBundles == null)
                    storedBundles = new List<TropicalBundleHarvest>();

                storedBundles.Add(new TropicalBundleHarvest
                {
                    itemCount = produced,
                    producedGameDay = producedGameDay
                });

                availableHarvestCount += produced;

                Debug.Log(
                    $"[{CropDisplayName}] Harvest cycle | Mode=BulkBundle | District={districtName} | " +
                    $"Stage={stage} | BundleItems={produced} | StoredBundles={storedBundles.Count} | " +
                    $"AvailableItems={availableHarvestCount} | Health={averageHealth:F1} | Stress={stress:F1}"
                );
            }
        }

        private float GetWaterScore()
        {
            float min = TropicalCropCatalog.GetIdealMoistureMin(cropKind);
            float max = TropicalCropCatalog.GetIdealMoistureMax(cropKind);

            if (moisture >= min && moisture <= max)
                return 1f;

            if (moisture < min)
                return Mathf.Clamp01(moisture / Mathf.Max(0.01f, min));

            return 1f - Mathf.InverseLerp(0f, 0.15f, moisture - max);
        }

        private int GetCurrentAbsoluteDay()
        {
            if (GameTimeSystem.Instance == null)
                return 0;

            return Mathf.FloorToInt(GameTimeSystem.Instance.TotalGameDays) + 1;
        }

        public void DevSetStressBaseline(float value)
        {
            stress = Mathf.Clamp(value, 0f, 100f);
            simulatedStress = stress;
        }

        public string GetInspectionText()
        {
            string shade = NeedsShade
                ? (IsShaded
                    ? "\nShade: under a Shade Net"
                    : "\nShade: none - young cacao needs a Shade Net nearby")
                : string.Empty;

            return
                $"Crop Name: {cropName}\n" +
                CropPlantingText.PlantedFromLine(plantingMaterial, nurseryDays) +
                $"Tree Stage: {stage}\n" +
                $"Health: {health:F1}/100\n" +
                $"Avg Health: {averageHealth:F1}/100\n" +
                $"Stress: {stress:F1}/100\n" +
                $"Water: {(moisture * 100f):F0}%\n" +
                $"Drainage: {(drainage * 100f):F0}%\n" +
                $"Fertility: {(fertility * 100f):F0}%\n" +
                $"Soil Suitability: {(soilSuitability * 100f):F0}%" +
                shade +
                CropPlantingText.RealWorldLine(plantingMaterial, CropClimateRules.GetFarmCropType(cropKind));
        }

        public void ForceNextStageForDev()
        {
            if (!initialized)
            {
                SoilSample fallback = plantedSoil ?? new SoilSample
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

                Initialize(
                    fallback,
                    string.IsNullOrWhiteSpace(districtName)
                        ? SelectedAreaState.SelectedDistrictName
                        : districtName
                );
            }

            if (GameTimeSystem.Instance == null)
                return;

            float firstHarvest = TropicalCropCatalog.GetFirstHarvestDelayDays(cropKind);

            float targetLivedDays = stage switch
            {
                TropicalCropStage.Seedling => firstHarvest * 0.22f,
                TropicalCropStage.Vegetative => firstHarvest * 0.62f,
                TropicalCropStage.PreFruiting => firstHarvest,
                TropicalCropStage.Fruiting => GetOldAgeDaysForDev(cropKind),
                TropicalCropStage.Old => Mathf.Max(0f, GameTimeSystem.Instance.TotalGameDays - plantedGameDay),
                _ => Mathf.Max(0f, GameTimeSystem.Instance.TotalGameDays - plantedGameDay)
            };

            if (stage == TropicalCropStage.Old)
                return;

            float currentDay = GameTimeSystem.Instance.TotalGameDays;
            plantedGameDay = currentDay - targetLivedDays - 1f;

            stage = GetStage();

            fieldPlantedGameDay = -1f;

            GrowthStageVisualController visuals = GetComponent<GrowthStageVisualController>();
            if (visuals != null)
                visuals.ForceRefresh();

            Debug.Log($"[DevTools] {CropDisplayName} forced to next stage: {stage}");
        }

        private float GetOldAgeDaysForDev(TropicalCropKind crop)
        {
            return crop switch
            {
                TropicalCropKind.Pineapple => 520f,
                TropicalCropKind.Cacao => 850f,
                TropicalCropKind.Pomelo => 980f,
                TropicalCropKind.Mango => 1150f,
                TropicalCropKind.Durian => 1250f,
                TropicalCropKind.Mangosteen => 1300f,
                TropicalCropKind.Corn => 220f,
                TropicalCropKind.Eggplant => 260f,
                TropicalCropKind.Squash => 240f,
                TropicalCropKind.Strawberry => 300f,
                TropicalCropKind.Tomato => 280f,
                _ => 900f
            };
        }
    }
}
