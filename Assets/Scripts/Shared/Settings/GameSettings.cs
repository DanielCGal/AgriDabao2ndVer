using System;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Central, additive store for user-adjustable settings: the three audio
    /// volumes, the render (culling) distance, and the two accessibility scales
    /// for interface and text size. It is the single source of truth that
    /// <see cref="GameAudioManager"/>, <see cref="DistanceCullingManager"/>,
    /// the grass spawner and <see cref="UIScaleService"/> read from and react to
    /// via <see cref="Changed"/>.
    ///
    /// Locally cached in PlayerPrefs (so it survives app restarts and is available
    /// before login); the per-account copy from the backend is applied via
    /// <see cref="ApplyFromDto"/> right after login and overrides the local cache.
    /// </summary>
    public static class GameSettings
    {
        public const float RenderMin = 8f;
        public const float RenderMax = 60f;

        /// <summary>
        /// Bounds for the interface scale.
        ///
        /// The ceiling is not arbitrary. Scaling the UI up works by shrinking every
        /// canvas's reference resolution, so a larger scale means fewer reference
        /// units fit on screen. At 1.20 the reference height falls to 900, which
        /// still clears the tallest fixed panel in the game (the marketplace board
        /// at 800) and leaves the Settings board's hanging sign on screen. Raising
        /// this further would start pushing panel headings off the top edge on a
        /// 4:3 tablet, so it is a real limit rather than a preference.
        /// </summary>
        public const float UiScaleMin = 0.80f;
        public const float UiScaleMax = 1.20f;

        /// <summary>
        /// Bounds for the extra text scale, applied on top of the interface scale.
        ///
        /// Tightened at the top and loosened at the bottom when the game moved to
        /// Playpen Sans. Many labels sit in rects sized as a multiple of their own
        /// font size, and text scale grows the font without growing the rect, so a
        /// high ceiling eats that headroom - with a handwriting face, whose line
        /// box and advance widths are both larger than the old font's, it runs out
        /// sooner. The floor drops in return, because a wider face is the case
        /// where a player is most likely to want text smaller rather than larger.
        /// </summary>
        public const float TextScaleMin = 0.80f;
        public const float TextScaleMax = 1.15f;

        private const float DefaultMusic = 0.6f;
        private const float DefaultSfx = 0.9f;
        private const float DefaultAmbience = 0.5f;
        private const float DefaultRender = 30f;

        /// <summary>Today's look is 1.0 for both, so an existing player sees no change.</summary>
        private const float DefaultUiScale = 1f;
        private const float DefaultTextScale = 1f;

        /// <summary>
        /// Off by default, which is the adviser's existing full-length answer. A
        /// player who never opens Settings sees exactly what they see today.
        /// </summary>
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

        /// <summary>Multiplier on every canvas, so panels and buttons grow together.</summary>
        public static float UiScale { get; private set; }

        /// <summary>Multiplier on font sizes, applied on top of <see cref="UiScale"/>.</summary>
        public static float TextScale { get; private set; }

        /// <summary>
        /// When on, the farm adviser and the climate evaluation answer in a couple
        /// of sentences instead of a full write-up. It carries the same findings,
        /// just without the bullet lists. The two task features are deliberately
        /// unaffected - they must return strict JSON, so shortening them would
        /// break parsing rather than reading nicer.
        /// </summary>
        public static bool AiSummarization { get; private set; }

        /// <summary>Fired whenever any value changes, so live consumers re-apply.</summary>
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

        /// <summary>Applies the account's saved settings (from the backend). Authoritative.</summary>
        public static void ApplyFromDto(SettingsDto dto)
        {
            if (dto == null)
                return;

            MusicVolume = Mathf.Clamp01(dto.musicVolume);
            SfxVolume = Mathf.Clamp01(dto.sfxVolume);
            AmbienceVolume = Mathf.Clamp01(dto.ambienceVolume);
            RenderDistance = Mathf.Clamp(dto.renderDistance, RenderMin, RenderMax);

            // A server that predates the scale columns omits these, and Unity's
            // deserializer leaves an absent float at 0. Clamping that would silently
            // shrink the player's interface to the minimum on their next login, so a
            // non-positive value is read as "the account has no opinion" and the
            // value already loaded from PlayerPrefs is kept.
            if (dto.uiScale > 0f)
                UiScale = ClampUiScale(dto.uiScale);
            if (dto.textScale > 0f)
                TextScale = ClampTextScale(dto.textScale);

            // A plain bool has no "absent" value the way the scales do, so an older
            // server simply reports false - which is the default anyway.
            AiSummarization = dto.aiSummarization;

            SaveLocal(); // keep the local cache in sync with the account
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
