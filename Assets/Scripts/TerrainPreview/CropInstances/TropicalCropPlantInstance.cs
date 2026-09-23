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

        /// <summary>
        /// Converts "stress per game day" from the weather tables into a share of
        /// the 0-1 stress target. At 40, the most typhoon-sensitive crop settles
        /// near 48% weather stress and the most resistant near 30%, so the per-crop
        /// difference is plainly visible without pinning every plant at 100.
        /// </summary>
        private const float WeatherStressFullScalePerDay = 40f;

        /// <summary>
        /// How long, in game days, an effect applied from outside this simulation
        /// keeps its grip before fading. A steady 3.5/day pest settles around 7
        /// points of health loss; a cured pest or a passed storm releases over
        /// roughly the same span instead of marking the plant permanently.
        /// </summary>
        private const float ExternalEffectTimeConstantDays = 2f;

        /// <summary>
        /// Extra stress per game day on young cacao standing in open sun, on the
        /// same scale as the weather tables. The user's notes call shade critical
        /// for young cacao, and the Shade Net Kit is the game's shade. At 6 it
        /// settles about fifteen points of stress - plainly visible on the crop
        /// board, not enough to kill a watered plant.
        /// </summary>
        private const float YoungCacaoSunStressPerDay = 6f;

        // The stress and health this simulation last produced on its own. health
        // and stress are shared fields - the pest system, mitigation structures
        // and player actions all write to them between ticks - so the gap between
        // these baselines and the public fields is exactly what those other
        // systems contributed. Tracking it separately is what lets the smoothing
        // below converge on its target without erasing their work.
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

        /// <summary>
        /// Makes a freshly planted crop the age it already reached before the
        /// field - days in the Seedling Tent, a grafted seedling's head start, or
        /// a bought seedling's age. The first harvest moves by the same amount,
        /// so the schedule stays measured from the plant's true age.
        /// </summary>
        public void ApplyStartingAge(float ageDays)
        {
            if (ageDays <= 0f)
                return;

            plantedGameDay -= ageDays;
            nextProductionGameDay -= ageDays;
            stage = GetStage();
            lastLoggedStage = stage;
        }

        /// <summary>Young cacao - before it starts to fruit - wants shade overhead.</summary>
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

            // Adopt whatever the fields currently hold the first time this runs.
            // Covers a fresh planting, a farm restored from the backend, and any
            // dev tool that sets the stats directly.
            if (!simulationBaselineReady)
            {
                simulatedStress = stress;
                simulatedHealth = health;
                simulationBaselineReady = true;
            }

            // Anything the public fields have drifted from the baselines was
            // written by another system since the previous tick - pest damage,
            // mitigation relief, watering, a harvest. Measure it now so the
            // smoothing further down can preserve it instead of erasing it.
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

            // Moisture is integrated before the weather stress is measured, so the
            // waterlogging and rain-relief tests below read this tick's moisture.
            // The coconut and banana simulations already worked this way; reading
            // the pre-drying value here made the same storm judge identical soil
            // differently depending on which crop was standing in it.
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

            // Stress per game day from the weather, deliberately NOT scaled by
            // deltaGameDays: it feeds the stress target below and the smoothing
            // converges on that target over time. Scaling it here and adding it
            // after the smoothing - which is what this used to do - meant the pull
            // back to target erased the whole contribution on the very next tick,
            // so a typhoon moved a durian's stress by 0.0003 out of 100 instead of
            // the 18/day its tables call for.
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

            // Waterlogging is judged against this crop's own moisture ceiling
            // rather than a shared constant, and now applies once instead of
            // stacking with a second fixed-threshold check inside the weather
            // system.
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

            // Smooth this simulation's own baselines toward their targets, then
            // let the contributions other systems made fade on their own clock,
            // and publish the sum. Pest damage of 3.5/day used to be wiped out
            // every tick by a 22/day pull toward target; it now holds the plant
            // down for as long as the infestation lasts.
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

        /// <summary>
        /// Developer tools only: force this crop's stress to a value and move its
        /// own baseline with it.
        ///
        /// Writing <c>stress</c> alone would not hold. Each simulation step
        /// recovers whatever another system wrote as
        /// <c>externalStress = stress - simulatedStress</c> and then fades it with a
        /// two-day time constant, so a directly assigned number decays back toward
        /// whatever the soil dictates - which is right for a mulch bag and wrong
        /// for a deliberate test setup. Moving the baseline too leaves no external
        /// offset to fade, so the value stays until the simulation itself drifts
        /// it.
        /// </summary>
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

            // A forced stage has to show that stage, not the planting material.
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
