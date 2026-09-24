using UnityEngine;

namespace AgriDabao3D
{
    public class FarmAudioController : MonoBehaviour
    {
        [Header("Day / Night Music")]
        public AudioClip dayMusic;
        public AudioClip nightMusic;
        [Range(0f, 24f)] public float dayStartHour = 6f;
        [Range(0f, 24f)] public float nightStartHour = 18f;

        [Header("Weather Ambience")]
        public AudioClip clearAmbience;
        public AudioClip rainAmbience;
        public AudioClip typhoonAmbience;
        public AudioClip droughtAmbience;

        private bool subscribed;
        private bool hasDayState;
        private bool lastWasDay;

        private void Start()
        {
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem.Instance.OnWeatherChanged += HandleWeatherChanged;
                subscribed = true;
            }

            ApplyDayNight(force: true);
            HandleWeatherChanged();
        }

        private void OnDestroy()
        {
            if (subscribed && WeatherSystem.Instance != null)
                WeatherSystem.Instance.OnWeatherChanged -= HandleWeatherChanged;

            if (GameAudioManager.HasInstance)
                GameAudioManager.Instance.PlayAmbience(null);
        }

        private void Update()
        {
            ApplyDayNight(force: false);
        }

        private void ApplyDayNight(bool force)
        {
            if (GameTimeSystem.Instance == null)
                return;

            float hour = GameTimeSystem.Instance.TimeOfDay01 * 24f;
            bool isDay = hour >= dayStartHour && hour < nightStartHour;

            if (force || !hasDayState || lastWasDay != isDay)
            {
                hasDayState = true;
                lastWasDay = isDay;

                AudioClip clip = isDay ? dayMusic : nightMusic;
                if (clip != null)
                    GameAudioManager.Instance.PlayMusic(clip);
            }
        }

        private void HandleWeatherChanged()
        {
            WeatherEventType weather = WeatherSystem.Instance != null
                ? WeatherSystem.Instance.currentEvent
                : WeatherEventType.Clear;

            AudioClip clip = weather switch
            {
                WeatherEventType.Rain => rainAmbience,
                WeatherEventType.Typhoon => typhoonAmbience,
                WeatherEventType.ExtremeDrought => droughtAmbience,
                _ => clearAmbience
            };

            GameAudioManager.Instance.PlayAmbience(clip);
        }
    }
}
