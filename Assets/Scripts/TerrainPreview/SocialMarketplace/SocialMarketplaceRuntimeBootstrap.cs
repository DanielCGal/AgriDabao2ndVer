using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriDabao3D
{
    public static class SocialMarketplaceRuntimeBootstrap
    {
        private const string RuntimeObjectName = "SocialMarketplace_Runtime";
        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            if (subscribed) return;
            subscribed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "TerrainPreview") return;
            if (GameObject.Find(RuntimeObjectName) != null) return;

            GameObject go = new GameObject(RuntimeObjectName);
            go.AddComponent<SocialMarketplaceApi>();
            go.AddComponent<SocialMarketplaceController>();
            go.AddComponent<SocialMarketplaceUIBuilder>();
        }
    }
}
