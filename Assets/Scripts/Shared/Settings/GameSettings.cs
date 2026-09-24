using System;
using UnityEngine;

namespace AgriDabao3D
{
    public static class GameSettings
    {
        public const float RenderMin = 8f;
        public const float RenderMax = 60f;

        public const float UiScaleMin = 0.80f;
        public const float UiScaleMax = 1.20f;

        public const float TextScaleMin = 0.80f;
        public const float TextScaleMax = 1.15f;

        private const float DefaultMusic = 0.6f;
        private const float DefaultSfx = 0.9f;
        private const float DefaultAmbience = 0.5f;
        private const float DefaultRender = 30f;

        private const float DefaultUiScale = 1f;
        private const float DefaultTextScale = 1f;

        private const bool DefaultAiSummarization = false;

        private const string KeyMusic = "settings.musicVolume";
        private const string KeySfx = "settings.sfxVolume";
        private const string KeyAmbience = "settings.ambienceVolume";
        private const string KeyRender = "settings.renderDistance";
        private const string KeyUiScale = "settings.uiScale";
        private const string KeyTextScale = "settings.textScale";
        private const string KeyAiSummarization = "settings.aiSummarization";

        public static float MusicVolume { get; private set; }
        public static float SfxVolume { get; private set; }
        public static float AmbienceVolume { get; private set; }
        public static float RenderDistance { get; private set; }

        public static float UiScale { get; private set; }

        public static float TextScale { get; private set; }

        public static bool AiSummarization { get; private set; }

        public static event Action Changed;

        static GameSettings()
        {
            LoadLocal();
        }

        private static void LoadLocal()
        {
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusic, DefaultMusic));
            SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeySfx, DefaultSfx));
            AmbienceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyAmbience, DefaultAmbience));
            RenderDistance = Mathf.Clamp(PlayerPrefs.GetFloat(KeyRender, DefaultRender), RenderMin, RenderMax);
            UiScale = ClampUiScale(PlayerPrefs.GetFloat(KeyUiScale, DefaultUiScale));
            TextScale = ClampTextScale(PlayerPrefs.GetFloat(KeyTextScale, DefaultTextScale));
            AiSummarization = PlayerPrefs.GetInt(
                KeyAiSummarization, DefaultAiSummarization ? 1 : 0) != 0;
        }

        public static void SaveLocal()
        {
            PlayerPrefs.SetFloat(KeyMusic, MusicVolume);
            PlayerPrefs.SetFloat(KeySfx, SfxVolume);
            PlayerPrefs.SetFloat(KeyAmbience, AmbienceVolume);
            PlayerPrefs.SetFloat(KeyRender, RenderDistance);
            PlayerPrefs.SetFloat(KeyUiScale, UiScale);
            PlayerPrefs.SetFloat(KeyTextScale, TextScale);
            PlayerPrefs.SetInt(KeyAiSummarization, AiSummarization ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            Changed?.Invoke();
        }

        public static void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            Changed?.Invoke();
        }

        public static void SetAmbienceVolume(float value)
        {
            AmbienceVolume = Mathf.Clamp01(value);
            Changed?.Invoke();
        }

        public static void SetRenderDistance(float value)
        {
            RenderDistance = Mathf.Clamp(value, RenderMin, RenderMax);
            Changed?.Invoke();
        }

        public static void SetUiScale(float value)
        {
            UiScale = ClampUiScale(value);
            Changed?.Invoke();
        }

        public static void SetTextScale(float value)
        {
            TextScale = ClampTextScale(value);
            Changed?.Invoke();
        }

        public static void SetAiSummarization(bool value)
        {
            AiSummarization = value;
            Changed?.Invoke();
        }

        public static float ClampUiScale(float value)
        {
            return Mathf.Clamp(value, UiScaleMin, UiScaleMax);
        }

        public static float ClampTextScale(float value)
        {
            return Mathf.Clamp(value, TextScaleMin, TextScaleMax);
        }

        public static void ApplyFromDto(SettingsDto dto)
        {
            if (dto == null)
                return;

            MusicVolume = Mathf.Clamp01(dto.musicVolume);
            SfxVolume = Mathf.Clamp01(dto.sfxVolume);
            AmbienceVolume = Mathf.Clamp01(dto.ambienceVolume);
            RenderDistance = Mathf.Clamp(dto.renderDistance, RenderMin, RenderMax);

            if (dto.uiScale > 0f)
                UiScale = ClampUiScale(dto.uiScale);
            if (dto.textScale > 0f)
                TextScale = ClampTextScale(dto.textScale);

            AiSummarization = dto.aiSummarization;

            SaveLocal();
            Changed?.Invoke();
        }

        public static SettingsDto ToDto()
        {
            return new SettingsDto
            {
                musicVolume = MusicVolume,
                sfxVolume = SfxVolume,
                ambienceVolume = AmbienceVolume,
                renderDistance = RenderDistance,
                uiScale = UiScale,
                textScale = TextScale,
                aiSummarization = AiSummarization
            };
        }
    }
}
