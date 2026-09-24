using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriDabao3D
{
    public static class NurseryRuntimeBootstrap
    {
        private const string RootName = "NurseryRuntime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            EnsureInstalled();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInstalled();
        }

        public static void EnsureInstalled()
        {
            if (!SceneManager.GetActiveScene().name.Equals("TerrainPreview", System.StringComparison.OrdinalIgnoreCase) ||
                Object.FindFirstObjectByType<FarmingInteractionSystem>() == null)
            {
                return;
            }

            GameObject root = GameObject.Find(RootName);
            if (root == null)
                root = new GameObject(RootName);

            if (Object.FindFirstObjectByType<NurserySystem>() == null)
                root.AddComponent<NurserySystem>();

            if (Object.FindFirstObjectByType<SeedlingTentUIBuilder>() == null)
                root.AddComponent<SeedlingTentUIBuilder>();
        }
    }
}
