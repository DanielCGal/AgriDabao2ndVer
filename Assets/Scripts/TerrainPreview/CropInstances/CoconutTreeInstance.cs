using System;
using UnityEngine;


namespace AgriDabao3D
{
    public enum CoconutStage
    {
        Seedling,
        Young,
        Immature,
        Mature,
        Old
    }

    public class CoconutTreeInstance : MonoBehaviour
    {
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

        [Header("Live Tree Stats")]
        [Range(0f, 1f)] public float moisture = 0.60f;
        [Range(0f, 100f)] public float health = 75f;
        [Range(0f, 100f)] public float stress = 10f;
        [Range(0f, 100f)] public float averageHealth = 75f;

        [Header("Climate")]
        public float currentTemperatureC = 27f;

        [Range(0f, 1f)]
        public float temperatureSuitability = 1f;

        [Header("Growth")]
        public CoconutStage stage = CoconutStage.Seedling;
        public float plantedGameDay;
        public float lastWateredGameDay;
        public int availableHarvestCount;
        public float nextProductionGameDay;


        private bool initialized;
        private CoconutStage lastLoggedStage;
        private bool hasLoggedImmature;

        /// <summary>
        /// Coconut's own storm and drought ratings, on the same 0-14 scale the
        /// tropical crop catalogue uses. This simulation previously had no typhoon
        /// or drought branch at all, which left coconut the one crop of the
        /// thirteen a storm could not touch. The values sit at the resistant end
        /// deliberately - a deep-rooted palm rides out weather that flattens
        /// strawberry - but resistant is not immune.
        /// </summary>
        // Coconut is the most storm-hardy crop on the farm, and these numbers say
        // so: 4 is below every one of the eleven catalogued crops, the lowest of
        // which is pineapple at 5. A palm bends with the wind and sheds it
        // through its fronds instead of catching it, which is why coconut stands
        // through storms that flatten everything around it - and it is the
        // difference the game should be teaching about planting near the coast.
        //
        // It used to be 6, identical to banana, which put the most typhoon-proof
        // crop and the most typhoon-prone one on the same footing.
        private const float TyphoonStressPerDay = 4f;

        /// <summary>Deep roots reach water long after shallow-rooted crops wilt.</summary>
        private const float DroughtStressPerDay = 5f;

        /// <summary>Upper end of the moisture band <see cref="GetWaterScore"/> treats as ideal.</summary>
        private const float IdealMoistureMax = 0.80f;

        /// <summary>See TropicalCropPlantInstance for the reasoning behind both constants.</summary>
        private const float WeatherStressFullScalePerDay = 40f;
        private const float ExternalEffectTimeConstantDays = 2f;

        // Baselines this simulation owns; the gap between these and the public
        // fields is what the pest system, mitigation structures and player actions
        // contributed since the previous tick.
        private float simulatedStress;
        private float simulatedHealth;
        private bool simulationBaselineReady;

        public void Initialize(SoilSample soil, string district)
        {

            cropId = Guid.NewGuid().ToString("N");

            plantedSoil = soil;
            districtName = district;
            visualSoilType = soil.GetVisualSoilType();

            drainage = SoilGameplayRules.GetDrainage(soil);
            fertility = SoilGameplayRules.GetFertility(soil);
            soilSuitability = CalculateSoilSuitability(soil);

            plantedGameDay = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : 0f;
            lastWateredGameDay = plantedGameDay;
            nextProductionGameDay = plantedGameDay + 192f;

            health = 55f + soilSuitability * 25f;
            averageHealth = health;
            moisture = 0.65f;
            stress = 5f;

            if (WeatherSystem.Instance != null)
            {
                currentTemperatureC =
                    WeatherSystem.Instance.currentTemperatureC;

                temperatureSuitability =
                    CropClimateRules.GetTemperatureSuitability(
                        FarmCropType.Coconut,
                        currentTemperatureC
                    );
            }

            stage = GetStage(GetAgeYears());
            lastLoggedStage = stage;
            hasLoggedImmature = stage >= CoconutStage.Immature;

            initialized = true;
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
            CoconutStage previousStage = stage;
            stage = GetStage(GetAgeYears());

            // Adopt whatever the fields currently hold on the first tick, covering
            // a fresh planting, a restored farm, and the dev tools.
            if (!simulationBaselineReady)
            {
                simulatedStress = stress;
                simulatedHealth = health;
                simulationBaselineReady = true;
            }

            float externalStress = stress - simulatedStress;
            float externalHealth = health - simulatedHealth;

            float dryRate = Mathf.Lerp(0.040f, 0.085f, 1f - drainage);
            if (stage == CoconutStage.Seedling) dryRate *= 1.15f;
            if (stage == CoconutStage.Old) dryRate *= 0.90f;

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
                moisture
                - (dryRate * weatherDryMultiplier * deltaGameDays)
                + (weatherMoistureAdd * deltaGameDays)
            );

            // Weather stress per game day. Not scaled by deltaGameDays - it feeds
            // the stress target below, which the smoothing converges on. Adding it
            // after the smoothing, as this used to, let the pull back to target
            // erase it every tick.
            float weatherStressPerDay = 0f;

            if (WeatherSystem.Instance != null)
            {
                weatherStressPerDay = WeatherSystem.Instance.GetEventStressPerDay();

                if (WeatherSystem.Instance.currentEvent == WeatherEventType.ExtremeDrought)
                    weatherStressPerDay += DroughtStressPerDay;

                if (WeatherSystem.Instance.currentEvent == WeatherEventType.Typhoon)
                {
                    weatherStressPerDay += TyphoonStressPerDay;

                    // Matches the curve the tropical crops use, so coconut sits on
                    // the same scale as the other twelve rather than outside it.
                    directHealthPenalty += Mathf.Lerp(2f, 8f, TyphoonStressPerDay / 12f);
                }

                if (WeatherSystem.Instance.currentEvent == WeatherEventType.Rain &&
                    moisture < 0.45f)
                {
                    weatherStressPerDay -= 2f;
                }
            }

            // Waterlogging against coconut's own moisture ceiling. This used to
            // come from a shared fixed 0.85 threshold inside the weather system.
            bool waterlogged =
                moisture > IdealMoistureMax + 0.06f &&
                drainage < 0.55f;

            if (waterlogged)
                weatherStressPerDay += 8f;

            if (WeatherSystem.Instance != null)
            {
                currentTemperatureC =
                    WeatherSystem.Instance.currentTemperatureC;

                temperatureSuitability =
                    CropClimateRules.GetTemperatureSuitability(
                        FarmCropType.Coconut,
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
            float drainageFit = 1f - Mathf.Clamp01(Mathf.Abs(drainage - 0.72f) / 0.72f);

            float targetStress01 =
                (1f - waterScore) * 0.45f +
                (1f - fertility) * 0.18f +
                (1f - soilSuitability) * 0.17f +
                (1f - drainageFit) * 0.10f +
                temperatureStress01 * 0.25f +
                weatherStressPerDay / WeatherStressFullScalePerDay;

            float ageModifier = stage switch
            {
                CoconutStage.Seedling => -8f,
                CoconutStage.Young => 2f,
                CoconutStage.Immature => 6f,
                CoconutStage.Mature => 10f,
                CoconutStage.Old => -6f,
                _ => 0f
            };

            float temperatureHealthPenalty =
                 temperatureStress01 * 25f;

            float targetHealth =
                50f +
                Mathf.Lerp(-20f, 20f, soilSuitability) +
                Mathf.Lerp(-20f, 20f, waterScore) +
                Mathf.Lerp(-15f, 15f, fertility) +
                ageModifier -
                directHealthPenalty -
                temperatureHealthPenalty;

            // Smooth this simulation's own baselines toward their targets, fade
            // what other systems contributed, then publish the sum.
            simulatedHealth = Mathf.Clamp(
                Mathf.MoveTowards(simulatedHealth, targetHealth, 20f * deltaGameDays),
                0f,
                100f
            );

            simulatedStress = Mathf.Clamp(
                Mathf.MoveTowards(simulatedStress, targetStress01 * 100f, 35f * deltaGameDays),
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

            stage = GetStage(GetAgeYears());
            LogStageChanges(previousStage, stage);

            TryProduceHarvest();
        }

        private void LogStageChanges(CoconutStage previousStage, CoconutStage currentStage)
        {
            if (currentStage != previousStage)
            {
                Debug.Log(
                    $"[CoconutTree] Stage changed | District={districtName} | " +
                    $"From={previousStage} | To={currentStage} | " +
                    $"AgeYears={GetAgeYears():F2} | Day={GetCurrentAbsoluteDay()}"
                );
            }

            if (!hasLoggedImmature && currentStage >= CoconutStage.Immature)
            {
                hasLoggedImmature = true;

                Debug.Log(
                    $"[CoconutTree] Tree reached IMMATURE stage | District={districtName} | " +
                    $"Stage={currentStage} | AgeYears={GetAgeYears():F2} | Day={GetCurrentAbsoluteDay()} | " +
                    $"NextHarvestCycleDay={nextProductionGameDay:F1}"
                );
            }

            lastLoggedStage = currentStage;
        }

        public void Water(float amount = 0.40f)
        {
            moisture = Mathf.Clamp01(moisture + amount);
            lastWateredGameDay = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : lastWateredGameDay;
            stress = Mathf.Max(0f, stress - 8f);
        }

        public bool CanHarvest()
        {
            return stage >= CoconutStage.Immature && availableHarvestCount > 0;
        }

        public int Harvest()
        {
            if (!CanHarvest())
                return 0;

            int amount = availableHarvestCount;
            availableHarvestCount = 0;
            stress = Mathf.Clamp(stress + 6f, 0f, 100f);
            return amount;
        }

        public void ForceProduceHarvestForDev()
        {
            if (!initialized)
                return;

            if (GameTimeSystem.Instance != null)
            {
                float current = GameTimeSystem.Instance.TotalGameDays;
                float neededAgeDays = GameTimeSystem.Instance.DaysPerYear * 1.65f;

                if ((current - plantedGameDay) < neededAgeDays)
                    plantedGameDay = current - neededAgeDays - 1f;

                stage = GetStage(GetAgeYears());
            }

            float maturityFactor = stage switch
            {
                CoconutStage.Immature => 0.45f,
                CoconutStage.Mature => 1.00f,
                CoconutStage.Old => 0.70f,
                _ => 0.65f
            };

            float avgHealthFactor = Mathf.Clamp01(averageHealth / 100f);
            float stressFactor = 1f - Mathf.Clamp01(stress / 100f);

            int produced = Mathf.Clamp(
                Mathf.RoundToInt(6f * maturityFactor * avgHealthFactor * stressFactor),
                1,
                6
            );

            availableHarvestCount = Mathf.Max(availableHarvestCount, produced);

            Debug.Log($"[DevTools] Coconut forced harvest ready | Amount={availableHarvestCount}");
        }

        public float GetAgeYears()
        {
            if (GameTimeSystem.Instance == null)
                return 0f;

            float livedDays = GameTimeSystem.Instance.TotalGameDays - plantedGameDay;
            return livedDays / Mathf.Max(1, GameTimeSystem.Instance.DaysPerYear);
        }

        private CoconutStage GetStage(float ageYears)
        {
            if (ageYears < 0.35f) return CoconutStage.Seedling;
            if (ageYears < 1.00f) return CoconutStage.Young;
            if (ageYears < 1.60f) return CoconutStage.Immature;
            if (ageYears < 8.00f) return CoconutStage.Mature;
            return CoconutStage.Old;
        }

        private void TryProduceHarvest()
        {
            if (GameTimeSystem.Instance == null)
                return;

            if (stage < CoconutStage.Immature)
                return;

            while (GameTimeSystem.Instance.TotalGameDays >= nextProductionGameDay)
            {
                float maturityFactor;
                float avgHealthFactor;
                float stressFactor;
                float randomFactor;

                int produced = CalculateHarvestForCurrentCycle(
                    out maturityFactor,
                    out avgHealthFactor,
                    out stressFactor,
                    out randomFactor
                );

                int before = availableHarvestCount;
                availableHarvestCount += produced;

                Debug.Log(
                    $"[CoconutTree] Harvest cycle triggered | District={districtName} | " +
                    $"Stage={stage} | CycleDay={nextProductionGameDay:F1} | CurrentDay={GetCurrentAbsoluteDay()} | " +
                    $"MaturityFactor={maturityFactor:F2} | AvgHealth={averageHealth:F1} | AvgHealthFactor={avgHealthFactor:F2} | " +
                    $"Stress={stress:F1} | StressFactor={stressFactor:F2} | RandomFactor={randomFactor:F2} | " +
                    $"Added={produced} | AvailableHarvestBefore={before} | AvailableHarvestAfter={availableHarvestCount}"
                );

                nextProductionGameDay += 45f;
            }
        }

        private int CalculateHarvestForCurrentCycle(
            out float maturityFactor,
            out float avgHealthFactor,
            out float stressFactor,
            out float randomFactor)
        {
            maturityFactor = stage switch
            {
                CoconutStage.Immature => 0.45f,
                CoconutStage.Mature => 1.00f,
                CoconutStage.Old => 0.70f,
                _ => 0f
            };

            avgHealthFactor = Mathf.Clamp01(averageHealth / 100f);
            stressFactor = 1f - Mathf.Clamp01(stress / 100f);
            randomFactor = UnityEngine.Random.Range(0.90f, 1.10f);

            float rawHarvest =
                6f *
                maturityFactor *
                avgHealthFactor *
                stressFactor *
                randomFactor;

            int result = Mathf.Clamp(Mathf.RoundToInt(rawHarvest), 0, 6);
            return result;
        }

        private int GetCurrentAbsoluteDay()
        {
            if (GameTimeSystem.Instance == null)
                return 0;

            return Mathf.FloorToInt(GameTimeSystem.Instance.TotalGameDays) + 1;
        }

        private float GetWaterScore()
        {
            if (moisture >= 0.45f && moisture <= 0.80f)
                return 1f;

            if (moisture < 0.45f)
                return 1f - Mathf.InverseLerp(0f, 0.45f, 0.45f - moisture);

            return 1f - Mathf.InverseLerp(0f, 0.20f, moisture - 0.80f);
        }

        private float CalculateSoilSuitability(SoilSample s)
        {
            float typeScore = s.GetVisualSoilType() switch
            {
                "Sandy" => 0.95f,
                "Loamy" => 0.90f,
                "Silty" => 0.70f,
                "Clayey" => 0.55f,
                "Organic-rich" => 0.75f,
                "Rocky" => 0.35f,
                _ => 0.65f
            };

            float phScore = 1f - Mathf.Clamp01(Mathf.Abs(s.phh2o - 6.2f) / 2.2f);
            float drainageScore = SoilGameplayRules.GetDrainage(s);
            float compactionScore = 1f - SoilGameplayRules.GetCompactionPenalty(s);

            return Mathf.Clamp01(
                typeScore * 0.40f +
                phScore * 0.20f +
                drainageScore * 0.25f +
                compactionScore * 0.15f
            );
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
            return
                $"Crop Name: {cropName}\n" +
                $"Tree Stage: {stage}\n" +
                $"Health: {health:F1}/100\n" +
                $"Avg Health: {averageHealth:F1}/100\n" +
                $"Stress: {stress:F1}/100\n" +
                $"Water: {(moisture * 100f):F0}%\n" +
                $"Drainage: {(drainage * 100f):F0}%\n" +
                $"Fertility: {(fertility * 100f):F0}%\n" +
                $"Soil Suitability: {(soilSuitability * 100f):F0}%";
        }

        public void ForceNextStageForDev()
        {
            if (!initialized || GameTimeSystem.Instance == null)
                return;

            float targetAgeYears = stage switch
            {
                CoconutStage.Seedling => 0.35f,
                CoconutStage.Young => 1.00f,
                CoconutStage.Immature => 1.60f,
                CoconutStage.Mature => 8.00f,
                CoconutStage.Old => GetAgeYears(),
                _ => GetAgeYears()
            };

            if (stage == CoconutStage.Old)
                return;

            float currentDay = GameTimeSystem.Instance.TotalGameDays;
            float targetAgeDays = targetAgeYears * GameTimeSystem.Instance.DaysPerYear;

            plantedGameDay = currentDay - targetAgeDays - 1f;
            stage = GetStage(GetAgeYears());

            GrowthStageVisualController visuals = GetComponent<GrowthStageVisualController>();
            if (visuals != null)
                visuals.ForceRefresh();

            Debug.Log($"[DevTools] Coconut forced to next stage: {stage}");
        }
    }
}