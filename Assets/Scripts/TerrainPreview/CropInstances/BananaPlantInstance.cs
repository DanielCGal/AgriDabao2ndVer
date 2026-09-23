using System;
using UnityEngine;

namespace AgriDabao3D
{
    public enum BananaStage
    {
        Seedling,
        Vegetative,
        PreFruiting,
        Fruiting,
        Old
    }

    [System.Serializable]
    public class BananaBulbHarvest
    {
        public int bananaCount;
        public float producedGameDay;
    }

    public class BananaPlantInstance : MonoBehaviour
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

        [Header("Live Plant Stats")]
        [Range(0f, 1f)] public float moisture = 0.72f;
        [Range(0f, 100f)] public float health = 72f;
        [Range(0f, 100f)] public float stress = 12f;
        [Range(0f, 100f)] public float averageHealth = 72f;

        [Header("Climate")]
        public float currentTemperatureC = 27f;

        [Range(0f, 1f)]
        public float temperatureSuitability = 1f;

        [Header("Growth")]
        public BananaStage stage = BananaStage.Seedling;
        public float plantedGameDay;
        public float lastWateredGameDay;
        public int availableBananaCount;
        public System.Collections.Generic.List<BananaBulbHarvest> storedBulbs = new System.Collections.Generic.List<BananaBulbHarvest>();
        public float nextProductionGameDay;

        private bool initialized;
        private BananaStage lastLoggedStage;

        /// <summary>Banana's own storm and drought ratings, on the tropical catalogue's 0-14 scale.</summary>
        // Banana is the most typhoon-prone crop on the farm, and 13 puts it above
        // every catalogued crop - past mangosteen and strawberry at 12. That is
        // not an arbitrary nudge: blow-down is the defining typhoon loss in
        // Philippine banana growing, because the plant carries huge leaves on a
        // water-filled pseudostem over shallow roots, so it catches the wind like
        // a sail and snaps rather than bending.
        //
        // It used to be 6 - the same as coconut, which survives the same storm.
        // A game about climate adaptation in Davao cannot tell a player those two
        // choices carry equal risk, because banana is exactly the crop a Davao
        // farmer stands to lose.
        private const float TyphoonStressPerDay = 13f;

        /// <summary>
        /// Shallow roots and a heavy, constant water demand, so a dry spell bites
        /// sooner than it does on the tree crops - but not as hard as on cacao or
        /// mangosteen, which sit at 13 and 14.
        /// </summary>
        private const float DroughtStressPerDay = 9f;

        /// <summary>Bounds of the moisture band <see cref="GetWaterScore"/> treats as ideal.</summary>
        private const float IdealMoistureMin = 0.60f;
        private const float IdealMoistureMax = 0.90f;

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
            plantedSoil = soil;
            districtName = district;
            visualSoilType = soil.GetVisualSoilType();
            cropId = Guid.NewGuid().ToString("N");

            drainage = SoilGameplayRules.GetDrainage(soil);
            fertility = SoilGameplayRules.GetFertility(soil);
            soilSuitability = CalculateSoilSuitability(soil);

            plantedGameDay = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : 0f;
            lastWateredGameDay = plantedGameDay;

            nextProductionGameDay = plantedGameDay + 96f; // first fruiting around 96 days

            health = 52f + soilSuitability * 28f;
            averageHealth = health;
            moisture = 0.75f;
            stress = 6f;

            if (WeatherSystem.Instance != null)
            {
                currentTemperatureC =
                    WeatherSystem.Instance.currentTemperatureC;

                temperatureSuitability =
                    CropClimateRules.GetTemperatureSuitability(
                        FarmCropType.Banana,
                        currentTemperatureC
                    );
            }

            stage = GetStage(GetAgeYears());
            lastLoggedStage = stage;
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
            BananaStage previousStage = stage;
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

            float dryRate = Mathf.Lerp(0.055f, 0.105f, 1f - drainage);
            dryRate *= 1.20f; // banana dries faster

            if (stage == BananaStage.Seedling) dryRate *= 1.15f;
            if (stage == BananaStage.Old) dryRate *= 1.05f;

            float weatherDryMultiplier = 1f;
            float weatherMoistureAdd = 0f;
            float directHealthPenalty = 0f;
            float temperatureStress01 = 0f;

            if (WeatherSystem.Instance != null)
            {
                weatherDryMultiplier = WeatherSystem.Instance.GetDryingMultiplier();
                weatherMoistureAdd = WeatherSystem.Instance.GetMoistureAdditionPerDay();
            }

            if (WeatherSystem.Instance != null)
            {
                currentTemperatureC =
                    WeatherSystem.Instance.currentTemperatureC;

                temperatureSuitability =
                    CropClimateRules.GetTemperatureSuitability(
                        FarmCropType.Banana,
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

                    // The same curve coconut and the eleven catalogued crops use.
                    // Banana used to take a flat -8 applied further down instead,
                    // which happened to equal what this produces at its new
                    // constant - but it was a separate number that ignored the
                    // constant entirely, so retuning the stress above would have
                    // silently left the health damage behind.
                    directHealthPenalty += Mathf.Lerp(2f, 8f, TyphoonStressPerDay / 12f);
                }

                if (WeatherSystem.Instance.currentEvent == WeatherEventType.Rain &&
                    moisture < IdealMoistureMin)
                {
                    weatherStressPerDay -= 2f;
                }
            }

            // Waterlogging against banana's own moisture ceiling. This used to come
            // from a shared fixed 0.85 threshold inside the weather system, which
            // fired while banana was still inside the band it treats as ideal.
            bool waterlogged =
                moisture > IdealMoistureMax + 0.06f &&
                drainage < 0.55f;

            if (waterlogged)
                weatherStressPerDay += 8f;

            float waterScore = GetWaterScore();
            float drainageFit = 1f - Mathf.Clamp01(Mathf.Abs(drainage - 0.68f) / 0.68f);

            float targetStress01 =
                (1f - waterScore) * 0.35f +
                (1f - fertility) * 0.20f +
                (1f - soilSuitability) * 0.15f +
                (1f - drainageFit) * 0.15f +
                temperatureStress01 * 0.30f +
                weatherStressPerDay / WeatherStressFullScalePerDay;

            float ageModifier = stage switch
            {
                BananaStage.Seedling => -6f,
                BananaStage.Vegetative => 4f,
                BananaStage.PreFruiting => 8f,
                BananaStage.Fruiting => 10f,
                BananaStage.Old => -12f,
                _ => 0f
            };

            float temperatureHealthPenalty =
                temperatureStress01 * 28f;

            float targetHealth =
                52f +
                Mathf.Lerp(-18f, 18f, soilSuitability) +
                Mathf.Lerp(-22f, 20f, waterScore) +
                Mathf.Lerp(-20f, 20f, fertility) +
                ageModifier -
                directHealthPenalty -
                temperatureHealthPenalty;

            // Smooth this simulation's own baselines toward their targets, fade
            // what other systems contributed, then publish the sum.
            simulatedHealth = Mathf.Clamp(
                Mathf.MoveTowards(simulatedHealth, targetHealth, 24f * deltaGameDays),
                0f, 100f
            );

            simulatedStress = Mathf.Clamp(
                Mathf.MoveTowards(simulatedStress, targetStress01 * 100f, 40f * deltaGameDays),
                0f, 100f
            );

            float externalFade = Mathf.Exp(-deltaGameDays / ExternalEffectTimeConstantDays);
            externalHealth *= externalFade;
            externalStress *= externalFade;

            health = Mathf.Clamp(simulatedHealth + externalHealth, 0f, 100f);
            stress = Mathf.Clamp(simulatedStress + externalStress, 0f, 100f);

            float avgBlend = 1f - Mathf.Exp(-deltaGameDays * 0.40f);
            averageHealth = Mathf.Lerp(averageHealth, health, avgBlend);

            // Re-read the stage after simulating, matching coconut and the tropical
            // crops. Without this the transition was never reported for banana.
            stage = GetStage(GetAgeYears());

            if (stage != previousStage)
                LogStageChange(previousStage, stage);

            TryProduceHarvest();
        }

        private void LogStageChange(BananaStage previousStage, BananaStage currentStage)
        {
            if (currentStage == lastLoggedStage)
                return;

            Debug.Log(
                $"[Banana] Stage changed | District={districtName} | " +
                $"From={previousStage} | To={currentStage} | " +
                $"AgeYears={GetAgeYears():F2}"
            );

            lastLoggedStage = currentStage;
        }

        public void Water(float amount = 0.45f)
        {
            moisture = Mathf.Clamp01(moisture + amount);
            lastWateredGameDay = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : lastWateredGameDay;
            stress = Mathf.Max(0f, stress - 10f);
        }

        public bool CanHarvest()
        {
            return stage >= BananaStage.PreFruiting && storedBulbs.Count > 0;
        }

        public System.Collections.Generic.List<BananaBulbHarvest> HarvestAllBulbs()
        {
            if (!CanHarvest())
                return null;

            var harvestedBulbs = new System.Collections.Generic.List<BananaBulbHarvest>(storedBulbs);

            storedBulbs.Clear();
            availableBananaCount = 0;
            stress = Mathf.Clamp(stress + 6f, 0f, 100f);

            return harvestedBulbs;
        }

        public void ForceProduceHarvestForDev()
        {
            if (!initialized)
                return;

            if (GameTimeSystem.Instance != null)
            {
                float current = GameTimeSystem.Instance.TotalGameDays;
                float neededAgeDays = GameTimeSystem.Instance.DaysPerYear * 0.52f;

                if ((current - plantedGameDay) < neededAgeDays)
                    plantedGameDay = current - neededAgeDays - 1f;

                stage = GetStage(GetAgeYears());
            }

            float avgHealthFactor = Mathf.Clamp01(averageHealth / 100f);
            float stressFactor = 1f - Mathf.Clamp01(stress / 100f);

            int produced = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(12f, 30f, avgHealthFactor * stressFactor)),
                12,
                30
            );

            if (storedBulbs == null)
                storedBulbs = new System.Collections.Generic.List<BananaBulbHarvest>();

            storedBulbs.Add(new BananaBulbHarvest
            {
                bananaCount = produced,
                producedGameDay = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : 0f
            });

            availableBananaCount += produced;

            Debug.Log($"[DevTools] Banana forced harvest ready | BundleBananas={produced} | StoredBulbs={storedBulbs.Count}");
        }

        public float GetAgeYears()
        {
            if (GameTimeSystem.Instance == null)
                return 0f;

            float livedDays = GameTimeSystem.Instance.TotalGameDays - plantedGameDay;
            return livedDays / Mathf.Max(1, GameTimeSystem.Instance.DaysPerYear);
        }

        private BananaStage GetStage(float ageYears)
        {
            if (ageYears < 0.08f) return BananaStage.Seedling;
            if (ageYears < 0.25f) return BananaStage.Vegetative;
            if (ageYears < 0.50f) return BananaStage.PreFruiting;
            if (ageYears < 1.20f) return BananaStage.Fruiting;
            return BananaStage.Old;
        }

        private void TryProduceHarvest()
        {
            if (GameTimeSystem.Instance == null)
                return;

            if (stage < BananaStage.PreFruiting)
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

                produced = Mathf.Clamp(produced, 12, 30);

                int bulbCountBefore = storedBulbs.Count;
                int bananaCountBefore = availableBananaCount;

                BananaBulbHarvest bulb = new BananaBulbHarvest
                {
                    bananaCount = produced,
                    producedGameDay = nextProductionGameDay
                };

                storedBulbs.Add(bulb);
                availableBananaCount += produced;

                Debug.Log(
                    $"[BananaPlant] Harvest cycle triggered | District={districtName} | " +
                    $"Stage={stage} | CycleDay={nextProductionGameDay:F1} | CurrentDay={GetCurrentAbsoluteDay()} | " +
                    $"MaturityFactor={maturityFactor:F2} | AvgHealth={averageHealth:F1} | AvgHealthFactor={avgHealthFactor:F2} | " +
                    $"Stress={stress:F1} | StressFactor={stressFactor:F2} | RandomFactor={randomFactor:F2} | " +
                    $"AddedBulbBananas={produced} | BulbsBefore={bulbCountBefore} | BulbsAfter={storedBulbs.Count} | " +
                    $"AvailableBananasBefore={bananaCountBefore} | AvailableBananasAfter={availableBananaCount}"
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
                BananaStage.PreFruiting => 0.55f,
                BananaStage.Fruiting => 1.00f,
                BananaStage.Old => 0.75f,
                _ => 0f
            };

            avgHealthFactor = Mathf.Clamp01(averageHealth / 100f);
            stressFactor = 1f - Mathf.Clamp01(stress / 100f);
            randomFactor = UnityEngine.Random.Range(0.90f, 1.10f);

            float quality =
                maturityFactor *
                Mathf.Lerp(0.70f, 1.00f, avgHealthFactor) *
                Mathf.Lerp(0.65f, 1.00f, stressFactor) *
                randomFactor;

            float rawBananas = Mathf.Lerp(12f, 30f, Mathf.Clamp01(quality));
            return Mathf.Clamp(Mathf.RoundToInt(rawBananas), 12, 30);
        }

        private int GetCurrentAbsoluteDay()
        {
            if (GameTimeSystem.Instance == null)
                return 0;

            return Mathf.FloorToInt(GameTimeSystem.Instance.TotalGameDays) + 1;
        }

        private float GetWaterScore()
        {
            if (moisture >= 0.60f && moisture <= 0.90f)
                return 1f;

            if (moisture < 0.60f)
                return Mathf.Clamp01(moisture / 0.60f);

            return 1f - Mathf.InverseLerp(0f, 0.10f, moisture - 0.90f);
        }

        private float CalculateSoilSuitability(SoilSample s)
        {
            float typeScore = s.GetVisualSoilType() switch
            {
                "Loamy" => 0.95f,
                "Silty" => 0.85f,
                "Clayey" => 0.65f,
                "Sandy" => 0.60f,
                "Organic-rich" => 0.80f,
                "Rocky" => 0.25f,
                _ => 0.60f
            };

            float phScore = 1f - Mathf.Clamp01(Mathf.Abs(s.phh2o - 6.3f) / 2.0f);
            float drainageScore = SoilGameplayRules.GetDrainage(s);
            float compactionScore = 1f - SoilGameplayRules.GetCompactionPenalty(s);
            float fertilityScore = SoilGameplayRules.GetFertility(s);

            return Mathf.Clamp01(
                typeScore * 0.30f +
                phScore * 0.15f +
                drainageScore * 0.25f +
                fertilityScore * 0.20f +
                compactionScore * 0.10f
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
                BananaStage.Seedling => 0.08f,
                BananaStage.Vegetative => 0.25f,
                BananaStage.PreFruiting => 0.50f,
                BananaStage.Fruiting => 1.20f,
                BananaStage.Old => GetAgeYears(),
                _ => GetAgeYears()
            };

            if (stage == BananaStage.Old)
                return;

            float currentDay = GameTimeSystem.Instance.TotalGameDays;
            float targetAgeDays = targetAgeYears * GameTimeSystem.Instance.DaysPerYear;

            plantedGameDay = currentDay - targetAgeDays - 1f;
            stage = GetStage(GetAgeYears());

            GrowthStageVisualController visuals = GetComponent<GrowthStageVisualController>();
            if (visuals != null)
                visuals.ForceRefresh();

            Debug.Log($"[DevTools] Banana forced to next stage: {stage}");
        }

    }
}
