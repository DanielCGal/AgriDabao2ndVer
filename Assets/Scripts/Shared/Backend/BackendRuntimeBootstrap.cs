using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriDabao3D
{
    public static class BackendRuntimeBootstrap
    {
        private const string BootstrapName = "AgriDabao_BackendBootstrap";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBootstrap()
        {
            if (GameObject.Find(BootstrapName) != null)
                return;

            GameObject bootstrap = new GameObject(BootstrapName);
            Object.DontDestroyOnLoad(bootstrap);
            bootstrap.AddComponent<ApiConfiguration>();
            bootstrap.AddComponent<ApiClient>();
            bootstrap.AddComponent<AuthSession>();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu" && Object.FindFirstObjectByType<AuthUIBuilder>() == null)
            {
                new GameObject("AuthUIBuilder_Runtime").AddComponent<AuthUIBuilder>();
            }

            if (scene.name == "TerrainPreview")
            {
                FarmPersistenceManager manager = Object.FindFirstObjectByType<FarmPersistenceManager>();
                GameObject persistence = manager != null
                    ? manager.gameObject
                    : new GameObject("FarmPersistence_Runtime");

                if (manager == null)
                    persistence.AddComponent<FarmPersistenceManager>();

                if (Object.FindFirstObjectByType<SaveFarmButtonBuilder>() == null)
                    persistence.AddComponent<SaveFarmButtonBuilder>();

                if (Object.FindFirstObjectByType<PauseMenuBuilder>() == null)
                    persistence.AddComponent<PauseMenuBuilder>();
            }
        }
    }
}
