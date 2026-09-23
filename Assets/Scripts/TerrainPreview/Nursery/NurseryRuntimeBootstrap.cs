using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriDabao3D
{
    /// <summary>
    /// Creates the Seedling Tent's runtime systems in the farm scene, the same
    /// way the climate-maintenance and farm-task systems are created, so the
    /// scene needs no extra objects. Safe to call any number of times.
    /// </summary>
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

            // The nursery before its panel: the panel subscribes to it on Start.
            if (Object.FindFirstObjectByType<NurserySystem>() == null)
                root.AddComponent<NurserySystem>();

            if (Object.FindFirstObjectByType<SeedlingTentUIBuilder>() == null)
                root.AddComponent<SeedlingTentUIBuilder>();
        }
    }
}
