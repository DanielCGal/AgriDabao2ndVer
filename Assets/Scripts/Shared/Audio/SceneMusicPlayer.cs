using UnityEngine;

namespace AgriDabao3D
{
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
