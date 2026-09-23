using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Plays this scene's background music through the persistent
    /// <see cref="GameAudioManager"/>. Add one of these (not GameAudioManager
    /// itself) to any scene that needs its own distinct track - Main Menu,
    /// Area Selection, etc.
    ///
    /// Unlike GameAudioManager, this component is deliberately NOT a singleton
    /// and NOT DontDestroyOnLoad, so it is destroyed and freshly recreated with
    /// every scene load. That means its Start() reliably fires each time this
    /// scene becomes active, correctly switching the music even though the
    /// audio engine itself persists across scenes.
    /// </summary>
    public class SceneMusicPlayer : MonoBehaviour
    {
        [Tooltip("The looping track for this scene.")]
        public AudioClip music;

        private void Start()
        {
            if (music != null)
                GameAudioManager.Instance.PlayMusic(music);
        }
    }
}
