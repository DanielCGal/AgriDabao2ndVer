using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriDabao3D
{
    public static class ClimateMaintenanceRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            EnsureInstalled();
        }

        public static void EnsureInstalled()
        {
            if (!SceneManager.GetActiveScene().name.Equals(
                    "TerrainPreview",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            GameObject root = GameObject.Find("ClimateMaintenanceRuntime");
            if (root == null)
            {
                root = new GameObject("ClimateMaintenanceRuntime");
            }

            ClimateMaintenanceToastUI toast = root.GetComponent<ClimateMaintenanceToastUI>();
            if (toast == null)
            {
                toast = root.AddComponent<ClimateMaintenanceToastUI>();
            }

            ClimateMaintenanceInteractionSystem interaction =
                root.GetComponent<ClimateMaintenanceInteractionSystem>();
            if (interaction == null)
            {
                interaction = root.AddComponent<ClimateMaintenanceInteractionSystem>();
            }

            if (root.GetComponent<ClimateActionAutoObserver>() == null)
            {
                root.AddComponent<ClimateActionAutoObserver>();
            }

            interaction.toast = toast;
            interaction.farmingSystem =
                Object.FindFirstObjectByType<FarmingInteractionSystem>();
            interaction.terrainGenerator =
                Object.FindFirstObjectByType<TemporaryTerrainGenerator>();
            interaction.playerCamera = Camera.main;

            FirstPersonTerrainController controller =
                Object.FindFirstObjectByType<FirstPersonTerrainController>();
            if (controller != null)
            {
                controller.climateMaintenanceSystem = interaction;
            }

            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                crop.GetOrAddMaintenanceState();
            }
        }
    }
}
