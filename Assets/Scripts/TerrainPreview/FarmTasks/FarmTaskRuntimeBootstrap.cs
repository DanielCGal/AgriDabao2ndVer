using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriDabao3D
{
    public static class FarmTaskRuntimeBootstrap
    {
        private const string RootName = "FarmTaskSystem_Runtime";

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
            if (Object.FindFirstObjectByType<GameTimeSystem>() == null &&
                Object.FindFirstObjectByType<FarmingInteractionSystem>() == null &&
                FarmPersistenceManager.Instance == null)
            {
                return;
            }

            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
            }

            AIAdvisorTaskGeminiClient aiClient =
                Object.FindFirstObjectByType<AIAdvisorTaskGeminiClient>();
            if (aiClient == null)
                aiClient = root.AddComponent<AIAdvisorTaskGeminiClient>();

            DailyTaskSystem daily =
                Object.FindFirstObjectByType<DailyTaskSystem>();
            if (daily == null)
                daily = root.AddComponent<DailyTaskSystem>();

            AIAdvisorTaskSystem ai =
                Object.FindFirstObjectByType<AIAdvisorTaskSystem>();
            if (ai == null)
                ai = root.AddComponent<AIAdvisorTaskSystem>();
            ai.geminiClient = aiClient;

            if (Object.FindFirstObjectByType<FarmTaskActionObserver>() == null)
                root.AddComponent<FarmTaskActionObserver>();

            foreach (ClimateActionAutoObserver legacyObserver in
                     Object.FindObjectsByType<ClimateActionAutoObserver>(
                         FindObjectsSortMode.None))
            {
                if (legacyObserver != null)
                    legacyObserver.enabled = false;
            }

            if (Object.FindFirstObjectByType<FarmTaskPopupUI>() == null)
                root.AddComponent<FarmTaskPopupUI>();
            if (Object.FindFirstObjectByType<FarmTaskUIBuilder>() == null)
                root.AddComponent<FarmTaskUIBuilder>();
        }
    }
}
