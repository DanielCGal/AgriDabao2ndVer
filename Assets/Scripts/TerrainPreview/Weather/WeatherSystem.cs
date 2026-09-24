using System;
using UnityEngine;

namespace AgriDabao3D
{
    public enum WeatherEventType
    {
        Clear,
        Rain,
        Typhoon,
        ExtremeDrought
    }

    [Serializable]
    public class MonthlyWeatherProfile
    {
        public string monthName;

        public float avgLowTempC;
        public float avgHighTempC;
        public float baseHumidity;

        public float rainfallMm;
        public int rainyDays;
        public float sunshineHours;

        [Range(0f, 1f)]
        public float baseRainChance;

        [Range(0f, 1f)]
        public float typhoonChance;

        [Range(0f, 1f)]
        public float droughtChance;
    }

    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        [Header("Current Weather")]
        public WeatherEventType currentEvent =
            WeatherEventType.Clear;

        public float currentTemperatureC = 27f;
        public float currentHumidity = 78f;
        public float currentRainIntensity;
        public int remainingEventDays;

        [Header("Current Daily Range")]
        public float dailyLowTemperatureC;
        public float dailyHighTemperatureC;

        [Header("Monthly Profiles")]
        public MonthlyWeatherProfile[] monthlyProfiles =
            new MonthlyWeatherProfile[12];

        public event Action OnWeatherChanged;

        private MonthlyWeatherProfile currentProfile;
        private bool initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            BuildDefaultProfiles();
        }

        private void Start()
        {
            if (!FarmLoadContext.IsRestoring)
                EnsureInitialized();
        }

        private void Update()
        {
            if (FarmLoadContext.IsRestoring)
                return;

            RefreshCurrentConditions();
        }

        public void EnsureInitialized()
        {
            if (initialized ||
                GameTimeSystem.Instance == null)
            {
                return;
            }

            RefreshMonthlyProfile();
            RollNewDailyWeather();
            RollDailyTemperatureRange();
            UpdateCurrentTemperatureAndHumidity();

            initialized = true;
            OnWeatherChanged?.Invoke();
        }

        public void RefreshCurrentConditions()
        {
            if (GameTimeSystem.Instance == null)
                return;

            if (!initialized)
            {
                EnsureInitialized();
            }

            if (!initialized)
                return;

            UpdateCurrentTemperatureAndHumidity();
        }

        private void BuildDefaultProfiles()
        {
            monthlyProfiles[0] = NewProfile(
                "January", 23.5f, 31f, 78f,
                117f, 13, 6f,
                0.35f, 0f, 0.05f
            );

            monthlyProfiles[1] = NewProfile(
                "February", 23.5f, 31.5f, 77f,
                118f, 12, 6f,
                0.32f, 0f, 0.06f
            );

            monthlyProfiles[2] = NewProfile(
                "March", 24f, 32f, 75f,
                93f, 11, 7f,
                0.18f, 0f, 0.12f
            );

            monthlyProfiles[3] = NewProfile(
                "April", 24.5f, 33f, 74f,
                153f, 11, 8f,
                0.25f, 0.01f, 0.16f
            );

            monthlyProfiles[4] = NewProfile(
                "May", 24.5f, 32f, 82f,
                220f, 15, 7f,
                0.48f, 0.04f, 0.08f
            );

            monthlyProfiles[5] = NewProfile(
                "June", 24f, 31f, 84f,
                202f, 17, 6f,
                0.52f, 0.08f, 0.03f
            );

            monthlyProfiles[6] = NewProfile(
                "July", 23.8f, 31f, 82f,
                165f, 15, 6f,
                0.46f, 0.14f, 0.02f
            );

            monthlyProfiles[7] = NewProfile(
                "August", 23.8f, 31f, 84f,
                192f, 14, 6f,
                0.50f, 0.18f, 0.02f
            );

            monthlyProfiles[8] = NewProfile(
                "September", 24f, 31.5f, 84f,
                195f, 15, 6f,
                0.52f, 0.20f, 0.02f
            );

            monthlyProfiles[9] = NewProfile(
                "October", 24f, 31.5f, 84f,
                177f, 16, 6f,
                0.50f, 0.16f, 0.03f
            );

            monthlyProfiles[10] = NewProfile(
                "November", 24f, 31.5f, 82f,
                147f, 15, 7f,
                0.40f, 0.08f, 0.04f
            );

            monthlyProfiles[11] = NewProfile(
                "December", 23.8f, 31f, 79f,
                114f, 13, 6f,
                0.28f, 0f, 0.05f
            );
        }

        private MonthlyWeatherProfile NewProfile(
            string monthName,
            float lowTemp,
            float highTemp,
            float humidity,
            float rainfall,
            int rainyDays,
            float sunshine,
            float rainChance,
            float typhoonChance,
            float droughtChance)
        {
            return new MonthlyWeatherProfile
            {
                monthName = monthName,
                avgLowTempC = lowTemp,
                avgHighTempC = highTemp,
                baseHumidity = humidity,
                rainfallMm = rainfall,
                rainyDays = rainyDays,
                sunshineHours = sunshine,
                baseRainChance = rainChance,
                typhoonChance = typhoonChance,
                droughtChance = droughtChance
            };
        }

        public void AdvanceWeatherOneDay()
        {
            EnsureInitialized();

            if (!initialized)
                return;

            RefreshMonthlyProfile();

            if (remainingEventDays > 0)
            {
                remainingEventDays--;

                if (remainingEventDays <= 0)
                {
                    currentEvent =
                        WeatherEventType.Clear;

                    currentRainIntensity = 0f;
                }
            }

            if (remainingEventDays <= 0)
                RollNewDailyWeather();

            RollDailyTemperatureRange();
            UpdateCurrentTemperatureAndHumidity();

            OnWeatherChanged?.Invoke();
        }

        private void RefreshMonthlyProfile()
        {
            if (GameTimeSystem.Instance == null)
                return;

            int monthIndex = Mathf.Clamp(
                GameTimeSystem.Instance.CurrentMonthNumber - 1,
                0,
                11
            );

            currentProfile = monthlyProfiles[monthIndex];
        }

        private void RollNewDailyWeather()
        {
            if (currentProfile == null)
                return;

            if (FarmGraceperiod.IsCalmWeatherDay)
            {
                StartWeatherEvent(WeatherEventType.Clear, 0, 0f);
                return;
            }

            if (UnityEngine.Random.value <
                currentProfile.typhoonChance)
            {
                StartWeatherEvent(
                    WeatherEventType.Typhoon,
                    UnityEngine.Random.Range(3, 6),
                    1f
                );

                return;
            }

            if (UnityEngine.Random.value <
                currentProfile.droughtChance)
            {
                StartWeatherEvent(
                    WeatherEventType.ExtremeDrought,
                    UnityEngine.Random.Range(3, 7),
                    0f
                );

                return;
            }

            if (UnityEngine.Random.value <
                currentProfile.baseRainChance)
            {
                StartWeatherEvent(
                    WeatherEventType.Rain,
                    1,
                    UnityEngine.Random.Range(0.35f, 0.75f)
                );

                return;
            }

            StartWeatherEvent(
                WeatherEventType.Clear,
                0,
                0f
            );
        }

        private void StartWeatherEvent(
            WeatherEventType eventType,
            int duration,
            float rainIntensity)
        {
            currentEvent = eventType;
            remainingEventDays = duration;
            currentRainIntensity = rainIntensity;

            Debug.Log(
                $"[Weather] {currentEvent} | " +
                $"Duration={remainingEventDays} days | " +
                $"Month={currentProfile?.monthName}"
            );
        }

        private void RollDailyTemperatureRange()
        {
            if (currentProfile == null)
                return;

            float eventOffset =
                GetEventTemperatureOffset();

            dailyLowTemperatureC =
                currentProfile.avgLowTempC +
                UnityEngine.Random.Range(-0.6f, 0.6f) +
                eventOffset * 0.35f;

            dailyHighTemperatureC =
                currentProfile.avgHighTempC +
                UnityEngine.Random.Range(-1f, 1f) +
                eventOffset;

            dailyHighTemperatureC = Mathf.Max(
                dailyHighTemperatureC,
                dailyLowTemperatureC + 3f
            );
        }

        private float GetEventTemperatureOffset()
        {
            return currentEvent switch
            {
                WeatherEventType.Rain => -1.5f,
                WeatherEventType.Typhoon => -2.5f,
                WeatherEventType.ExtremeDrought => 3f,
                _ => 0f
            };
        }

        private void UpdateCurrentTemperatureAndHumidity()
        {
            if (GameTimeSystem.Instance == null ||
                currentProfile == null)
            {
                return;
            }

            float hour =
                GameTimeSystem.Instance.CurrentHour;

            float warmth =
                Mathf.Cos(
                    (hour - 14f) /
                    24f *
                    Mathf.PI *
                    2f
                ) *
                0.5f +
                0.5f;

            currentTemperatureC = Mathf.Lerp(
                dailyLowTemperatureC,
                dailyHighTemperatureC,
                warmth
            );

            float weatherHumidity = currentEvent switch
            {
                WeatherEventType.Rain =>
                    10f + currentRainIntensity * 6f,

                WeatherEventType.Typhoon => 16f,

                WeatherEventType.ExtremeDrought => -22f,

                _ => 0f
            };

            float nightHumidity =
                Mathf.Lerp(6f, 0f, warmth);

            currentHumidity = Mathf.Clamp(
                currentProfile.baseHumidity +
                weatherHumidity +
                nightHumidity,
                40f,
                98f
            );
        }

        public float GetDryingMultiplier()
        {
            float temperatureMultiplier =
                Mathf.Lerp(
                    0.80f,
                    1.45f,
                    Mathf.InverseLerp(
                        22f,
                        36f,
                        currentTemperatureC
                    )
                );

            return currentEvent switch
            {
                WeatherEventType.Rain =>
                    0.55f * temperatureMultiplier,

                WeatherEventType.Typhoon =>
                    0.25f * temperatureMultiplier,

                WeatherEventType.ExtremeDrought =>
                    1.85f * temperatureMultiplier,

                _ => temperatureMultiplier
            };
        }

        public float GetMoistureAdditionPerDay()
        {
            return currentEvent switch
            {
                WeatherEventType.Rain =>
                    Mathf.Lerp(
                        0.08f,
                        0.18f,
                        currentRainIntensity
                    ),

                WeatherEventType.Typhoon => 0.25f,

                _ => 0f
            };
        }

        public float GetEventStressPerDay()
        {
            return currentEvent switch
            {
                WeatherEventType.ExtremeDrought => 10f,
                WeatherEventType.Typhoon => 7f,
                _ => 0f
            };
        }

        public bool IsWetWeather()
        {
            return currentEvent ==
                       WeatherEventType.Rain ||
                   currentEvent ==
                       WeatherEventType.Typhoon;
        }

        public bool IsDryWeather()
        {
            return currentEvent ==
                       WeatherEventType.ExtremeDrought ||
                   currentHumidity < 65f;
        }

        public void ForceWeatherEvent(
            WeatherEventType eventType,
            int durationDays)
        {
            EnsureInitialized();
            RefreshMonthlyProfile();

            int safeDuration =
                eventType == WeatherEventType.Clear
                    ? 0
                    : Mathf.Max(1, durationDays);

            float forcedRainIntensity =
                eventType switch
                {
                    WeatherEventType.Rain => 0.60f,
                    WeatherEventType.Typhoon => 1f,
                    _ => 0f
                };

            StartWeatherEvent(
                eventType,
                safeDuration,
                forcedRainIntensity
            );

            RollDailyTemperatureRange();
            UpdateCurrentTemperatureAndHumidity();

            Debug.Log(
                $"[Weather] Forced event | " +
                $"Type={currentEvent} | " +
                $"Duration={remainingEventDays} days | " +
                $"Temperature={currentTemperatureC:F1}C | " +
                $"Humidity={currentHumidity:F0}%"
            );

            OnWeatherChanged?.Invoke();
        }


        public void RestoreWeather(WeatherSaveDto save)
        {
            if (save == null)
                return;

            if (!Enum.TryParse(save.currentEvent, out WeatherEventType restoredEvent))
                restoredEvent = WeatherEventType.Clear;

            currentEvent = restoredEvent;
            currentTemperatureC = save.currentTemperatureC;
            currentHumidity = save.currentHumidity;
            currentRainIntensity = save.currentRainIntensity;
            remainingEventDays = Mathf.Max(0, save.remainingEventDays);
            dailyLowTemperatureC = save.dailyLowTemperatureC;
            dailyHighTemperatureC = save.dailyHighTemperatureC;

            RefreshMonthlyProfile();
            initialized = true;
            OnWeatherChanged?.Invoke();
        }

        public string GetWeatherLabel()
        {
            return
                $"Weather: {currentEvent} | " +
                $"{currentTemperatureC:F1}C | " +
                $"Humidity: {currentHumidity:F0}%";
        }
    }
}

