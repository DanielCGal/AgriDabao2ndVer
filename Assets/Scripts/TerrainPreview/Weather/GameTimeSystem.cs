using System;
using UnityEngine;
using Object = UnityEngine.Object;
namespace AgriDabao3D
{
    public class GameTimeSystem : MonoBehaviour
    {
        public static GameTimeSystem Instance { get; private set; }
        [Header("Calendar")]
        public float realSecondsPerGameDay = 900f;
        public int daysPerMonth = 16;
        public int monthsPerYear = 12;
        [Header("Starting Time")]
        [Range(0f, 23.99f)]
        public float startHour = 8f;
        [Header("Day / Night")]
        public Light sunLight;
        public float nightSunIntensity = 0.01f;
        public float daySunIntensity = 1.2f;
        public float sunYaw = 170f;
        [Header("Development Time Skip")]
        [Tooltip(
            "Maximum simulation step used by AdvanceDays/AdvanceHours. " +
            "0.25 means skipped time is simulated in 6-hour steps."
        )]
        [Range(0.05f, 1f)]
        public float maximumTimeSkipStepDays = 0.25f;
        [Header("Debug")]
        public bool logNewDayPipeline;
        public float TotalGameDays { get; private set; }
        public float DeltaGameDays { get; private set; }
        public float TimeOfDay01 { get; private set; }
        public int DaysPerYear =>
            Mathf.Max(1, daysPerMonth * monthsPerYear);
        public float RealSecondsPerGameYear =>
            realSecondsPerGameDay * DaysPerYear;
        public float TotalGameYears =>
            TotalGameDays / DaysPerYear;
        public int CurrentYearNumber =>
            Mathf.FloorToInt(TotalGameDays / DaysPerYear) + 1;
        public int CurrentDayInYear =>
            Mathf.FloorToInt(TotalGameDays) % DaysPerYear + 1;
        public int CurrentMonthNumber =>
            ((CurrentDayInYear - 1) /
             Mathf.Max(1, daysPerMonth)) + 1;
        public int CurrentDayInMonth =>
            ((CurrentDayInYear - 1) %
             Mathf.Max(1, daysPerMonth)) + 1;
        public float CurrentHour =>
            TimeOfDay01 * 24f;
        public event Action OnTimeChanged;
        public event Action OnNewGameDay;
        private const float TimeEpsilon = 0.000001f;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Day 1 begins at the requested time.
            TotalGameDays =
                Mathf.Repeat(startHour, 24f) / 24f;
            RefreshTimeOfDay();
            UpdateSun();
        }
        private void Start()
        {
            if (FarmLoadContext.IsRestoring)
                return;
            // Initialization is idempotent, so script Start order
            // no longer affects the first pest/disease evaluation.
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.EnsureInitialized();
            }
            if (PestDiseaseSystem.Instance != null)
            {
                PestDiseaseSystem.Instance.ProcessInitialGameDay();
            }
        }
        private void Update()
        {
            if (FarmLoadContext.IsRestoring)
            {
                DeltaGameDays = 0f;
                return;
            }
            DeltaGameDays =
                Time.deltaTime /
                Mathf.Max(1f, realSecondsPerGameDay);
            AdvanceClock(
                DeltaGameDays,
                simulateSkippedWorld: false
            );
            UpdateSun();
            OnTimeChanged?.Invoke();
        }
        private void AdvanceClock(
            float gameDays,
            bool simulateSkippedWorld)
        {
            if (gameDays <= 0f)
                return;
            float remainingDays = gameDays;
            int safetyCounter = 0;
            while (remainingDays > TimeEpsilon)
            {
                safetyCounter++;
                if (safetyCounter > 100000)
                {
                    Debug.LogError(
                        "GameTimeSystem stopped an unexpectedly " +
                        "large time-advance loop."
                    );
                    break;
                }
                float currentDayFraction =
                    Mathf.Repeat(TotalGameDays, 1f);
                float daysUntilBoundary =
                    1f - currentDayFraction;
                if (daysUntilBoundary <= TimeEpsilon)
                {
                    daysUntilBoundary = 1f;
                }
                float maximumStep =
                    simulateSkippedWorld
                        ? Mathf.Clamp(
                            maximumTimeSkipStepDays,
                            0.05f,
                            1f
                        )
                        : remainingDays;
                float stepDays = Mathf.Min(
                    remainingDays,
                    daysUntilBoundary,
                    maximumStep
                );
                TotalGameDays += stepDays;
                remainingDays -= stepDays;
                RefreshTimeOfDay();
                if (WeatherSystem.Instance != null)
                {
                    WeatherSystem.Instance
                        .RefreshCurrentConditions();
                }
                if (simulateSkippedWorld)
                {
                    SimulateSkippedWorld(stepDays);
                }
                bool reachedNewDay =
                    daysUntilBoundary - stepDays <=
                    TimeEpsilon;
                if (reachedNewDay)
                {
                    ProcessNewGameDay();
                }
            }
        }
        private void ProcessNewGameDay()
        {
            // Required deterministic order:
            // 1. The clock is already on the new day.
            // 2. Roll the new day's weather and temperature.
            // 3. Evaluate pest/disease risk using that weather.
            // 4. Notify any additional daily systems.
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance
                    .AdvanceWeatherOneDay();
            }
            if (PestDiseaseSystem.Instance != null)
            {
                PestDiseaseSystem.Instance
                    .ProcessNewGameDay();
            }
            OnNewGameDay?.Invoke();
            if (logNewDayPipeline)
            {
                string weatherLabel =
                    WeatherSystem.Instance != null
                        ? WeatherSystem.Instance
                            .currentEvent.ToString()
                        : "None";
                Debug.Log(
                    $"[NewDayPipeline] " +
                    $"Year={CurrentYearNumber} | " +
                    $"Month={CurrentMonthNumber} | " +
                    $"Day={CurrentDayInMonth} | " +
                    $"Weather={weatherLabel}"
                );
            }
        }
        public void AdvanceDays(float days)
        {
            if (days <= 0f)
                return;
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.EnsureInitialized();
            }
            if (PestDiseaseSystem.Instance != null)
            {
                PestDiseaseSystem.Instance
                    .ProcessInitialGameDay();
            }
            AdvanceClock(
                days,
                simulateSkippedWorld: true
            );
            UpdateSun();
            OnTimeChanged?.Invoke();
            // Do not misclassify changes caused by skipped simulation as
            // direct player actions after the skip finishes.
            FarmTaskActionObserver.Instance?.ResetBaseline();
            Debug.Log(
                $"[DevTools] Advanced {days:F2} game day(s). " +
                $"Now: Year {CurrentYearNumber}, " +
                $"Month {CurrentMonthNumber}, " +
                $"Day {CurrentDayInMonth}, " +
                $"{GetClockText()}."
            );
        }
        public void AdvanceHours(float hours)
        {
            if (hours <= 0f)
                return;
            AdvanceDays(hours / 24f);
        }
        private void SimulateSkippedWorld(float days)
        {
            if (days <= 0f)
                return;
            // Continuous mitigation is applied before disease damage.
            SimulateContinuousMitigations(days);
            if (PestDiseaseSystem.Instance != null)
            {
                PestDiseaseSystem.Instance
                    .SimulateDiseaseProgress(days);
            }
            SimulateCrops(days);
        }
        private void SimulateContinuousMitigations(
            float days)
        {
            AphidTrapInstance[] aphidTraps =
                Object.FindObjectsByType<
                    AphidTrapInstance>(
                    FindObjectsSortMode.None
                );
            foreach (AphidTrapInstance trap
                     in aphidTraps)
            {
                if (trap != null)
                    trap.SimulateTimeSkip(days);
            }
            AreaMitigationTrapInstance[] areaTraps =
                Object.FindObjectsByType<
                    AreaMitigationTrapInstance>(
                    FindObjectsSortMode.None
                );
            foreach (AreaMitigationTrapInstance trap
                     in areaTraps)
            {
                if (trap != null)
                    trap.SimulateTimeSkip(days);
            }
            CropProtectionState[] protections =
                Object.FindObjectsByType<
                    CropProtectionState>(
                    FindObjectsSortMode.None
                );
            foreach (CropProtectionState protection
                     in protections)
            {
                if (protection != null)
                    protection.SimulateTimeSkip(days);
            }
            CropClimateMaintenanceState[] maintenanceStates =
                Object.FindObjectsByType<
                    CropClimateMaintenanceState>(
                    FindObjectsSortMode.None
                );
            foreach (CropClimateMaintenanceState maintenance
                     in maintenanceStates)
            {
                if (maintenance != null)
                    maintenance.SimulateTimeSkip(days);
            }
            // Climate mitigation objects also have continuous effects and
            // stored resources, so they must advance during task-based skips.
            ClimateMitigationWorldObject[] climateMitigations =
                Object.FindObjectsByType<ClimateMitigationWorldObject>(
                    FindObjectsSortMode.None);
            foreach (ClimateMitigationWorldObject mitigation
                     in climateMitigations)
            {
                if (mitigation != null)
                    mitigation.SimulateTimeSkip(days);
            }
        }
        private void SimulateCrops(float days)
        {
            CoconutTreeInstance[] coconuts =
                Object.FindObjectsByType<
                    CoconutTreeInstance>(
                    FindObjectsSortMode.None
                );
            foreach (CoconutTreeInstance crop
                     in coconuts)
            {
                if (crop != null)
                    crop.SimulateTimeSkip(days);
            }
            BananaPlantInstance[] bananas =
                Object.FindObjectsByType<
                    BananaPlantInstance>(
                    FindObjectsSortMode.None
                );
            foreach (BananaPlantInstance crop
                     in bananas)
            {
                if (crop != null)
                    crop.SimulateTimeSkip(days);
            }
            TropicalCropPlantInstance[] tropicals =
                Object.FindObjectsByType<
                    TropicalCropPlantInstance>(
                    FindObjectsSortMode.None
                );
            foreach (TropicalCropPlantInstance crop
                     in tropicals)
            {
                if (crop != null)
                    crop.SimulateTimeSkip(days);
            }
        }
        private void RefreshTimeOfDay()
        {
            TimeOfDay01 =
                Mathf.Repeat(TotalGameDays, 1f);
        }
        private void UpdateSun()
        {
            if (sunLight == null)
                return;
            // 6 AM = sunrise, 12 PM = highest sun,
            // 6 PM = sunset.
            float sunAngle =
                TimeOfDay01 * 360f - 90f;
            sunLight.transform.rotation =
                Quaternion.Euler(
                    sunAngle,
                    sunYaw,
                    0f
                );
            float daylight = Mathf.Clamp01(
                Mathf.Sin(
                    (TimeOfDay01 - 0.25f) *
                    Mathf.PI *
                    2f
                )
            );
            daylight = Mathf.Pow(
                daylight,
                0.55f
            );
            sunLight.intensity = Mathf.Lerp(
                nightSunIntensity,
                daySunIntensity,
                daylight
            );
        }
        public void RestoreTime(GameTimeSaveDto save)
        {
            if (save == null)
                return;
            daysPerMonth = Mathf.Max(1, save.daysPerMonth);
            monthsPerYear = Mathf.Max(1, save.monthsPerYear);
            TotalGameDays = Mathf.Max(0f, save.totalGameDays);
            DeltaGameDays = 0f;
            RefreshTimeOfDay();
            UpdateSun();
            OnTimeChanged?.Invoke();
        }
        public string GetClockText()
        {
            int totalMinutes =
                Mathf.FloorToInt(
                    CurrentHour * 60f
                );
            int hour24 = totalMinutes / 60;
            int minute = totalMinutes % 60;
            string period =
                hour24 >= 12 ? "PM" : "AM";
            int hour12 = hour24 % 12;
            if (hour12 == 0)
                hour12 = 12;
            return $"{hour12}:{minute:00} {period}";
        }
    }
}
