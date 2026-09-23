using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    /// <summary>
    /// Central, additive audio engine for the game: looping background music,
    /// looping weather/scene ambience (both with crossfade), and one-shot SFX.
    /// SFX clips are Inspector fields the developer drags their own files into.
    ///
    /// Persistent singleton (survives scene loads); also lazily self-creates so
    /// any Play* call is safe even in a scene that forgot to add the component.
    /// It touches no gameplay logic.
    ///
    /// This component does NOT know which music belongs to which scene - because
    /// it persists, only the very first instance loaded would ever run its own
    /// Start(). Instead, each scene that wants its own track adds a small,
    /// non-persistent <see cref="SceneMusicPlayer"/> (or, for the Farm scene,
    /// <see cref="FarmAudioController"/>), which reruns every time that scene
    /// loads and simply calls Instance.PlayMusic(...) with its own clip.
    /// </summary>
    public class GameAudioManager : MonoBehaviour
    {
        private static GameAudioManager instance;

        public static bool HasInstance => instance != null;

        public static GameAudioManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                instance = Object.FindFirstObjectByType<GameAudioManager>();
                if (instance != null)
                    return instance;

                GameObject go = new GameObject("GameAudioManager (auto)");
                instance = go.AddComponent<GameAudioManager>();
                return instance;
            }
        }

        [Header("SFX Clips")]
        public AudioClip buttonClickSfx;
        public AudioClip digSfx;
        public AudioClip plantSeedSfx;
        public AudioClip spraySfx;
        public AudioClip placeMitigationSfx;
        public AudioClip rewardSfx;

        // Volumes now live in GameSettings (user-adjustable + persisted) and are
        // read live at playback / applied via the GameSettings.Changed event.

        [Header("Transition")]
        [Tooltip("Seconds to fade the old track out while the new one fades in.")]
        public float crossfadeSeconds = 1.5f;

        [Header("Button Click SFX")]
        [Tooltip("Automatically play buttonClickSfx on every UI Button click in the scene.")]
        public bool autoPlayButtonClicks = true;
        public float buttonScanInterval = 0.3f;

        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource ambienceA;
        private AudioSource ambienceB;
        private AudioSource sfxSource;

        private bool musicUsingA = true;
        private bool ambienceUsingA = true;
        private AudioClip currentMusicClip;
        private AudioClip currentAmbienceClip;
        private AudioSource activeMusic;
        private AudioSource activeAmbience;

        private Coroutine musicFade;
        private Coroutine ambienceFade;

        private readonly HashSet<Button> hookedButtons = new HashSet<Button>();
        private float nextButtonScan;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                // Destroy only this duplicate component, not the whole GameObject:
                // a scene may also carry a sibling SceneMusicPlayer/FarmAudioController
                // on the same object, and that sibling must survive to run its own
                // Start() and tell the surviving (persistent) instance what to play.
                Destroy(this);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            musicA = CreateSource("MusicA", true);
            musicB = CreateSource("MusicB", true);
            ambienceA = CreateSource("AmbienceA", true);
            ambienceB = CreateSource("AmbienceB", true);
            sfxSource = CreateSource("Sfx", false);

            GameSettings.Changed += ApplyLiveVolumes;
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= ApplyLiveVolumes;
            if (instance == this)
                instance = null;
        }

        private void ApplyLiveVolumes()
        {
            if (activeMusic != null)
                activeMusic.volume = GameSettings.MusicVolume;
            if (activeAmbience != null)
                activeAmbience.volume = GameSettings.AmbienceVolume;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            GameObject go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);

            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f; // 2D, non-positional
            return src;
        }

        private void Update()
        {
            if (!autoPlayButtonClicks)
                return;

            if (Time.unscaledTime < nextButtonScan)
                return;

            nextButtonScan = Time.unscaledTime + buttonScanInterval;
            HookNewButtons();
        }

        private void HookNewButtons()
        {
            Button[] buttons = Object.FindObjectsByType<Button>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < buttons.Length; i++)
            {
                Button b = buttons[i];
                if (b == null || hookedButtons.Contains(b))
                    continue;

                b.onClick.AddListener(PlayButtonClick);
                hookedButtons.Add(b);
            }

            hookedButtons.RemoveWhere(b => b == null);
        }

        // ---------------- SFX ----------------

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null)
                return;

            sfxSource.PlayOneShot(clip, GameSettings.SfxVolume);
        }

        public void PlayButtonClick() => PlaySfx(buttonClickSfx);
        public void PlayDig() => PlaySfx(digSfx);
        public void PlayPlantSeed() => PlaySfx(plantSeedSfx);
        public void PlaySpray() => PlaySfx(spraySfx);
        public void PlayPlaceMitigation() => PlaySfx(placeMitigationSfx);
        public void PlayReward() => PlaySfx(rewardSfx);

        // ---------------- Music / ambience (crossfaded) ----------------

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || clip == currentMusicClip)
                return;

            currentMusicClip = clip;

            AudioSource incoming = musicUsingA ? musicB : musicA;
            AudioSource outgoing = musicUsingA ? musicA : musicB;
            musicUsingA = !musicUsingA;
            activeMusic = incoming;

            if (musicFade != null)
                StopCoroutine(musicFade);
            musicFade = StartCoroutine(Crossfade(outgoing, incoming, clip, isMusic: true));
        }

        public void PlayAmbience(AudioClip clip)
        {
            if (clip == currentAmbienceClip)
                return; // a null clip is allowed - fades to silence

            currentAmbienceClip = clip;

            AudioSource incoming = ambienceUsingA ? ambienceB : ambienceA;
            AudioSource outgoing = ambienceUsingA ? ambienceA : ambienceB;
            ambienceUsingA = !ambienceUsingA;
            activeAmbience = incoming;

            if (ambienceFade != null)
                StopCoroutine(ambienceFade);
            ambienceFade = StartCoroutine(Crossfade(outgoing, incoming, clip, isMusic: false));
        }

        private IEnumerator Crossfade(
            AudioSource outgoing, AudioSource incoming, AudioClip clip, bool isMusic)
        {
            float duration = Mathf.Max(0.01f, crossfadeSeconds);

            if (clip != null)
            {
                incoming.clip = clip;
                incoming.volume = 0f;
                incoming.Play();
            }

            float startOutVolume = outgoing.isPlaying ? outgoing.volume : 0f;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                // Read the target volume live so a settings change mid-fade is respected.
                float targetVolume = isMusic ? GameSettings.MusicVolume : GameSettings.AmbienceVolume;
                if (clip != null)
                    incoming.volume = targetVolume * k;
                outgoing.volume = startOutVolume * (1f - k);
                yield return null;
            }

            if (clip != null)
                incoming.volume = isMusic ? GameSettings.MusicVolume : GameSettings.AmbienceVolume;

            outgoing.Stop();
            outgoing.clip = null;
            outgoing.volume = 0f;
        }
    }
}
