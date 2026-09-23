using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AgriDabao3D
{
    // These classes are kept so your existing
    // FarmAdvisorContextBuilder continues compiling.

    [Serializable]
    public class AdvisorPestDiseaseData
    {
        public bool hasActivePestOrDisease;

        public float farmAverageAphidsPercent;
        public float farmAverageBacterialWiltPercent;
        public float farmAverageFallArmywormPercent;
        public float farmAverageMitesPercent;

        public int infectedBacterialWiltCropCount;
        public int aphidAffectedCropCount;
        public int armywormAffectedCropCount;
        public int miteAffectedCropCount;

        public List<AdvisorCropPestDetail> cropDetails =
            new List<AdvisorCropPestDetail>();
    }

    [Serializable]
    public class AdvisorCropPestDetail
    {
        public string cropId;
        public string cropType;

        public float health;
        public float stress;
        public float moisturePercent;

        public bool infected;

        public float aphidsPercent;
        public float bacterialWiltPercent;
        public float fallArmywormPercent;
        public float mitesPercent;
    }

    public class PestDiseaseSystem : MonoBehaviour
    {
        public static PestDiseaseSystem Instance
        {
            get;
            private set;
        }

        [Header("Rule Database")]
        [SerializeField]
        private PestDiseaseDatabase database;

        public PestDiseaseDatabase Database =>
            database;

        [Header("Daily Risk")]
        public bool enableDailyRisk = true;

        [Range(0f, 10f)]
        public float globalRiskMultiplier = 1f;

        [Tooltip(
            "Evaluates the current game day when the scene starts."
        )]
        public bool evaluateImmediatelyOnStart = true;

        [Header("New Condition Severity")]
        public Vector2 startingSeverityRange =
            new Vector2(4f, 10f);

        public Vector2 firstTargetSeverityRange =
            new Vector2(20f, 40f);

        public Vector2 repeatedTargetIncreaseRange =
            new Vector2(6f, 16f);

        [Header("Messages")]
        public bool showAppearancePopup = true;

        [Header("Debug")]
        public bool logSuccessfulAppearances = true;
        public bool logEveryRiskCalculation;

        private readonly List<PestDiseaseAffectedCrop>
            registeredCrops =
                new List<PestDiseaseAffectedCrop>();

        private bool initialDayProcessed;

        private void Awake()
        {
            if (Instance != null &&
                Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // Initialization is safe regardless of Unity Start order.
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.EnsureInitialized();
            }

            ProcessInitialGameDay();
        }

        public void ProcessInitialGameDay()
        {
            if (initialDayProcessed)
                return;

            initialDayProcessed = true;

            if (evaluateImmediatelyOnStart &&
                enableDailyRisk)
            {
                EvaluateDailyRisk();
            }
        }

        public void ProcessNewGameDay()
        {
            initialDayProcessed = true;

            if (enableDailyRisk)
            {
                EvaluateDailyRisk();
            }
        }

        // =========================================================
        // CROP REGISTRATION
        // =========================================================

        public void RegisterCrop(
            PestDiseaseAffectedCrop crop)
        {
            if (crop != null &&
                !registeredCrops.Contains(crop))
            {
                registeredCrops.Add(crop);
            }
        }

        public void UnregisterCrop(
            PestDiseaseAffectedCrop crop)
        {
            registeredCrops.Remove(crop);
        }

        public List<PestDiseaseAffectedCrop>
            GetAllAffectedCrops()
        {
            registeredCrops.RemoveAll(
                crop => crop == null
            );

            AddMissingCropComponents<
                CoconutTreeInstance>();

            AddMissingCropComponents<
                BananaPlantInstance>();

            AddMissingCropComponents<
                TropicalCropPlantInstance>();

            registeredCrops.RemoveAll(
                crop => crop == null
            );

            return registeredCrops.ToList();
        }

        private void AddMissingCropComponents<T>()
            where T : Component
        {
            T[] crops =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsSortMode.None
                );

            foreach (T crop in crops)
            {
                if (crop == null)
                    continue;

                PestDiseaseAffectedCrop affected =
                    crop.GetComponent<
                        PestDiseaseAffectedCrop>();

                if (affected == null)
                {
                    affected =
                        crop.gameObject.AddComponent<
                            PestDiseaseAffectedCrop>();
                }

                RegisterCrop(affected);
            }
        }

        // =========================================================
        // DAILY RISK
        // =========================================================

        private void EvaluateDailyRisk()
        {
            if (!enableDailyRisk)
                return;

            // Days 1 and 2 belong to the new farmer, not to the pests. See
            // FarmGraceperiod for why this is keyed to the date rather than to
            // whether the beginner guide is running.
            if (FarmGraceperiod.IsCalmWeatherDay)
                return;

            if (database == null)
            {
                Debug.LogWarning(
                    "PestDiseaseSystem: Database is not assigned."
                );

                return;
            }

            if (GameTimeSystem.Instance == null ||
                WeatherSystem.Instance == null)
            {
                return;
            }

            List<PestDiseaseAffectedCrop> crops =
                GetAllAffectedCrops();

            int currentMonth =
                GameTimeSystem.Instance.CurrentMonthNumber;

            float temperature =
                WeatherSystem.Instance.currentTemperatureC;

            float humidity =
                WeatherSystem.Instance.currentHumidity;

            foreach (PestDiseaseAffectedCrop crop in crops)
            {
                if (crop == null)
                    continue;

                List<PestDiseaseRule> rules =
                    database.GetRulesForCrop(
                        crop.CropType
                    );

                foreach (PestDiseaseRule rule in rules)
                {
                    if (rule == null)
                        continue;

                    EvaluateRuleForCrop(
                        crop,
                        rule,
                        currentMonth,
                        temperature,
                        humidity,
                        crops
                    );
                }
            }
        }

        private void EvaluateRuleForCrop(
            PestDiseaseAffectedCrop crop,
            PestDiseaseRule rule,
            int currentMonth,
            float temperature,
            float humidity,
            List<PestDiseaseAffectedCrop> allCrops)
        {
            float monthFactor =
                GetMonthFactor(
                    rule,
                    currentMonth
                );

            if (monthFactor <= 0f)
                return;

            float stageFactor =
                GetStageFactor(
                    crop,
                    rule
                );

            if (stageFactor <= 0f)
                return;

            float temperatureFactor =
                GetTemperatureFactor(
                    rule,
                    temperature
                );

            if (temperatureFactor <= 0f)
                return;

            float humidityFactor =
                GetHumidityFactor(
                    rule,
                    humidity
                );

            if (humidityFactor <= 0f)
                return;

            float weatherFactor =
                GetWeatherFactor(rule);

            float stressFactor =
                GetStressFactor(
                    crop,
                    rule
                );

            float soilMoistureFactor =
                GetSoilMoistureFactor(
                    crop,
                    rule
                );

            float dependencyFactor =
                GetSpecialDependencyFactor(
                    crop,
                    rule
                );

            float nearbyFactor =
                GetNearbyInfectionFactor(
                    crop,
                    rule,
                    allCrops
                );

            float chance =
                rule.baseDailyAppearanceChance;

            chance *= monthFactor;
            chance *= stageFactor;
            chance *= temperatureFactor;
            chance *= humidityFactor;
            chance *= weatherFactor;
            chance *= stressFactor;
            chance *= soilMoistureFactor;
            chance *= dependencyFactor;
            chance *= nearbyFactor;
            chance *= globalRiskMultiplier;

            chance = Mathf.Clamp(
                chance,
                0f,
                0.95f
            );

            if (logEveryRiskCalculation)
            {
                Debug.Log(
                    $"[Pest Risk] " +
                    $"Crop={crop.CropDisplayName} | " +
                    $"Condition={rule.displayName} | " +
                    $"Chance={chance * 100f:F2}% | " +
                    $"Month={monthFactor:F2} | " +
                    $"Stage={stageFactor:F2} | " +
                    $"Temp={temperatureFactor:F2} | " +
                    $"Humidity={humidityFactor:F2} | " +
                    $"Weather={weatherFactor:F2} | " +
                    $"Stress={stressFactor:F2} | " +
                    $"Soil={soilMoistureFactor:F2} | " +
                    $"Dependency={dependencyFactor:F2} | " +
                    $"Nearby={nearbyFactor:F2}"
                );
            }

            if (UnityEngine.Random.value >= chance)
                return;

            bool wasAlreadyActive =
                crop.HasCondition(rule.type);

            float startingSeverity =
                UnityEngine.Random.Range(
                    startingSeverityRange.x,
                    startingSeverityRange.y
                );

            float targetSeverity;

            if (!wasAlreadyActive)
            {
                targetSeverity =
                    UnityEngine.Random.Range(
                        firstTargetSeverityRange.x,
                        firstTargetSeverityRange.y
                    );
            }
            else
            {
                targetSeverity =
                    crop.GetConditionTarget(rule.type) +
                    UnityEngine.Random.Range(
                        repeatedTargetIncreaseRange.x,
                        repeatedTargetIncreaseRange.y
                    );
            }

            targetSeverity = Mathf.Clamp(
                targetSeverity,
                startingSeverity,
                100f
            );

            crop.StartOrIncreaseCondition(
                rule.type,
                startingSeverity,
                targetSeverity
            );

            if (logSuccessfulAppearances)
            {
                // Month, stage and temperature are recorded alongside the
                // appearance so a log can be checked against the rule that
                // allowed it - host crop, peak months, stage window and
                // temperature range are the four hard gates, and without these
                // fields an appearance cannot be audited after the fact. All
                // four values are already in scope; nothing here is computed for
                // the log's sake.
                Debug.Log(
                    $"[Pest Appeared] " +
                    $"Crop={crop.CropDisplayName} | " +
                    $"Condition={rule.displayName} | " +
                    $"Month={currentMonth} | " +
                    $"Stage={crop.CurrentStage} | " +
                    $"Temp={temperature:F1}C | " +
                    $"Humidity={humidity:F0}% | " +
                    $"Chance={chance * 100f:F2}% | " +
                    $"Target={targetSeverity:F1}%"
                );
            }

            if (!wasAlreadyActive &&
                showAppearancePopup)
            {
                ShowPopup(
                    $"{rule.displayName} appeared on " +
                    $"{crop.CropDisplayName}.\n" +
                    $"Current severity: " +
                    $"{startingSeverity:F1}%."
                );
            }
        }

        private float GetMonthFactor(
            PestDiseaseRule rule,
            int month)
        {
            if (rule.peakMonths == null ||
                rule.peakMonths.Count == 0)
            {
                return 1f;
            }

            if (rule.peakMonths.Contains(month))
                return 1f;

            if (!rule.canAppearOutsidePeakMonths)
                return 0f;

            return Mathf.Clamp01(
                rule.offSeasonRiskMultiplier
            );
        }

        private float GetStageFactor(
    PestDiseaseAffectedCrop crop,
    PestDiseaseRule rule)
        {
            if (crop == null || rule == null)
                return 0f;

            CropDevelopmentStage currentStage =
                crop.CurrentStage;

            if (currentStage == CropDevelopmentStage.Any)
                return 0f;

            CropDevelopmentStage minimumStage =
                rule.minimumStage == CropDevelopmentStage.Any
                    ? CropDevelopmentStage.Seedling
                    : rule.minimumStage;

            CropDevelopmentStage maximumStage =
                rule.maximumStage == CropDevelopmentStage.Any
                    ? CropDevelopmentStage.Old
                    : rule.maximumStage;

            if (minimumStage > maximumStage)
            {
                Debug.LogWarning(
                    $"Invalid stage range for {rule.displayName}: " +
                    $"{rule.minimumStage} to {rule.maximumStage}."
                );

                return 0f;
            }

            return currentStage >= minimumStage &&
                   currentStage <= maximumStage
                ? 1f
                : 0f;
        }

        private float GetTemperatureFactor(
            PestDiseaseRule rule,
            float temperature)
        {
            if (temperature <
                    rule.minimumTemperatureC ||
                temperature >
                    rule.maximumTemperatureC)
            {
                return 0f;
            }

            float middle =
                (rule.minimumTemperatureC +
                 rule.maximumTemperatureC) *
                0.5f;

            float halfRange = Mathf.Max(
                0.1f,
                (rule.maximumTemperatureC -
                 rule.minimumTemperatureC) *
                0.5f
            );

            float distanceFromMiddle =
                Mathf.Abs(
                    temperature - middle
                ) / halfRange;

            return Mathf.Lerp(
                1.25f,
                0.75f,
                Mathf.Clamp01(
                    distanceFromMiddle
                )
            );
        }

        private float GetHumidityFactor(
            PestDiseaseRule rule,
            float humidity)
        {
            if (rule.minimumHumidity <= 0f)
                return 1f;

            if (humidity >= rule.minimumHumidity)
            {
                return Mathf.Lerp(
                    1f,
                    1.4f,
                    Mathf.InverseLerp(
                        rule.minimumHumidity,
                        100f,
                        humidity
                    )
                );
            }

            return Mathf.InverseLerp(
                rule.minimumHumidity - 15f,
                rule.minimumHumidity,
                humidity
            );
        }

        private float GetWeatherFactor(
            PestDiseaseRule rule)
        {
            if (WeatherSystem.Instance == null)
                return 1f;

            float factor = 1f;

            if (rule.prefersWetWeather)
            {
                factor *=
                    WeatherSystem.Instance.IsWetWeather()
                        ? 1.6f
                        : 0.35f;
            }

            if (rule.prefersDryWeather)
            {
                factor *=
                    WeatherSystem.Instance.IsDryWeather()
                        ? 1.6f
                        : 0.45f;
            }

            return factor;
        }

        private float GetStressFactor(
            PestDiseaseAffectedCrop crop,
            PestDiseaseRule rule)
        {
            if (!rule.prefersStressedPlants)
                return 1f;

            return Mathf.Lerp(
                0.70f,
                1.70f,
                Mathf.Clamp01(
                    crop.Stress / 100f
                )
            );
        }

        private float GetSoilMoistureFactor(
            PestDiseaseAffectedCrop crop,
            PestDiseaseRule rule)
        {
            if (!rule.prefersWaterloggedSoil)
                return 1f;

            bool waterlogged =
                crop.Moisture >= 0.85f &&
                crop.Drainage < 0.55f;

            if (waterlogged)
                return 1.8f;

            bool wetSoil =
                crop.Moisture >= 0.72f;

            return wetSoil ? 0.8f : 0.20f;
        }

        private float GetSpecialDependencyFactor(
            PestDiseaseAffectedCrop crop,
            PestDiseaseRule rule)
        {
            // Squash Mosaic Virus becomes much more likely
            // when Aphids are already present.
            if (rule.type ==
                PestDiseaseType.MosaicVirus)
            {
                return crop.HasCondition(
                    PestDiseaseType.Aphids,
                    5f
                )
                    ? 2.2f
                    : 0.20f;
            }

            // Fusarium ear/kernel rot becomes more likely
            // after insect damage.
            if (rule.type ==
                PestDiseaseType.FusariumEarKernelRot)
            {
                bool hasInsectDamage =
                    crop.HasCondition(
                        PestDiseaseType.CornBorer,
                        5f
                    ) ||
                    crop.HasCondition(
                        PestDiseaseType.CornEarworm,
                        5f
                    ) ||
                    crop.HasCondition(
                        PestDiseaseType.FallArmyworm,
                        5f
                    );

                return hasInsectDamage
                    ? 1.8f
                    : 0.60f;
            }

            return 1f;
        }

        private float GetNearbyInfectionFactor(
            PestDiseaseAffectedCrop target,
            PestDiseaseRule rule,
            List<PestDiseaseAffectedCrop> allCrops)
        {
            float factor = 1f;

            foreach (PestDiseaseAffectedCrop source
                     in allCrops)
            {
                if (source == null ||
                    source == target ||
                    !source.HasCondition(
                        rule.type,
                        10f
                    ))
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    source.transform.position,
                    target.transform.position
                );

                if (distance > rule.spreadRadius)
                    continue;

                float distanceFactor =
                    1f -
                    Mathf.Clamp01(
                        distance /
                        Mathf.Max(
                            0.1f,
                            rule.spreadRadius
                        )
                    );

                float severityFactor =
                    source.GetConditionSeverity(
                        rule.type
                    ) / 100f;

                factor +=
                    distanceFactor *
                    severityFactor *
                    (1f +
                     rule.spreadChancePerDay *
                     5f);
            }

            return Mathf.Clamp(
                factor,
                1f,
                3f
            );
        }

        // =========================================================
        // DEVELOPMENT TIME SKIPS
        // =========================================================

        [Obsolete(
            "GameTimeSystem.AdvanceDays now owns weather/day ordering. " +
            "Use SimulateDiseaseProgress for elapsed disease damage only."
        )]
        public void SimulateTimeSkip(
            float skippedGameDays)
        {
            SimulateDiseaseProgress(
                skippedGameDays
            );
        }

        public void SimulateDiseaseProgress(
            float elapsedGameDays)
        {
            if (elapsedGameDays <= 0f)
                return;

            SimulateAllCrops(
                elapsedGameDays
            );
        }

        private void SimulateAllCrops(float days)
        {
            List<PestDiseaseAffectedCrop> crops =
                GetAllAffectedCrops();

            foreach (PestDiseaseAffectedCrop crop
                     in crops)
            {
                if (crop != null)
                {
                    crop.SimulateDisease(days);
                }
            }
        }

        // =========================================================
        // DEVELOPMENT / FORCE EVENTS
        // Keeps existing DevTools buttons working.
        // =========================================================

        public void ForceEvent(
            PestDiseaseType type)
        {
            if (database == null)
            {
                Debug.LogWarning(
                    "Cannot force event: database not assigned."
                );

                return;
            }

            List<PestDiseaseAffectedCrop> crops =
                GetAllAffectedCrops();

            int affectedCount = 0;

            foreach (PestDiseaseAffectedCrop crop
                     in crops)
            {
                PestDiseaseRule rule =
                    database.GetRule(
                        crop.CropType,
                        type
                    );

                // Only valid host crops are affected.
                if (rule == null)
                    continue;

                crop.StartOrIncreaseCondition(
                    type,
                    10f,
                    70f
                );

                affectedCount++;
            }

            string displayName =
                type.ToString();

            ShowPopup(
                $"{displayName} was forced for testing.\n" +
                $"Valid affected crops: {affectedCount}."
            );

            Debug.Log(
                $"[PestDisease] Forced {type} | " +
                $"Valid crops={affectedCount}"
            );
        }

        public void ForceBacterialWilt()
        {
            ForceEvent(
                PestDiseaseType.BacterialWilt
            );
        }

        public void ForceAphids()
        {
            ForceEvent(
                PestDiseaseType.Aphids
            );
        }

        public void ForceArmyworms()
        {
            ForceEvent(
                PestDiseaseType.FallArmyworm
            );
        }

        public void ForceMites()
        {
            ForceEvent(
                PestDiseaseType.Mites
            );
        }

        // =========================================================
        // EXISTING ADVISER COMPATIBILITY
        // =========================================================

        public AdvisorPestDiseaseData
            BuildAdvisorPestData()
        {
            List<PestDiseaseAffectedCrop> crops =
                GetAllAffectedCrops();

            AdvisorPestDiseaseData data =
                new AdvisorPestDiseaseData();

            if (crops.Count == 0)
                return data;

            data.farmAverageAphidsPercent =
                crops.Average(
                    crop => crop.aphidsPercent
                );

            data.farmAverageBacterialWiltPercent =
                crops.Average(
                    crop =>
                        crop.bacterialWiltPercent
                );

            data.farmAverageFallArmywormPercent =
                crops.Average(
                    crop =>
                        crop.armywormsPercent
                );

            data.farmAverageMitesPercent =
                crops.Average(
                    crop => crop.mitesPercent
                );

            data.infectedBacterialWiltCropCount =
                crops.Count(
                    crop =>
                        crop.bacterialWiltPercent >
                        0.1f
                );

            data.aphidAffectedCropCount =
                crops.Count(
                    crop =>
                        crop.aphidsPercent >
                        0.1f
                );

            data.armywormAffectedCropCount =
                crops.Count(
                    crop =>
                        crop.armywormsPercent >
                        0.1f
                );

            data.miteAffectedCropCount =
                crops.Count(
                    crop =>
                        crop.mitesPercent >
                        0.1f
                );

            data.hasActivePestOrDisease =
                crops.Any(
                    crop =>
                        crop.HasAnyPestOrDisease()
                );

            foreach (PestDiseaseAffectedCrop crop
                     in crops)
            {
                data.cropDetails.Add(
                    new AdvisorCropPestDetail
                    {
                        cropId = crop.CropId,
                        cropType =
                            crop.CropDisplayName,

                        health = crop.Health,
                        stress = crop.Stress,

                        moisturePercent =
                            crop.Moisture * 100f,

                        infected =
                            crop.HasAnyPestOrDisease(),

                        aphidsPercent =
                            crop.aphidsPercent,

                        bacterialWiltPercent =
                            crop.bacterialWiltPercent,

                        fallArmywormPercent =
                            crop.armywormsPercent,

                        mitesPercent =
                            crop.mitesPercent
                    }
                );
            }

            return data;
        }

        private void ShowPopup(string message)
        {
            if (PestDiseasePopupUI.Instance != null)
            {
                PestDiseasePopupUI.Instance.Show(
                    message
                );
            }
            else
            {
                Debug.Log(
                    "[PestDisease Popup] " +
                    message
                );
            }
        }
    }
}
