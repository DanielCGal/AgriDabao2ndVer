using System.Collections.Generic;
using AgriDabao3D;
using UnityEditor;
using UnityEngine;

namespace AgriDabao3D.EditorTools
{
    public static class ClimateMaintenanceAssignmentValidator
    {
        [MenuItem("Tools/AgriDabao/Climate Maintenance/Validate Inspector Assignments")]
        private static void ValidateAssignments()
        {
            List<string> problems = new List<string>();

            FarmingInteractionSystem farming =
                Object.FindFirstObjectByType<FarmingInteractionSystem>();
            InventoryUIBuilder inventory =
                Object.FindFirstObjectByType<InventoryUIBuilder>();

            if (farming == null)
            {
                problems.Add("FarmingInteractionSystem was not found in the open scene.");
            }
            else
            {
                CheckPrefab(
                    problems,
                    "Mulch Visual",
                    farming.mulchVisualPrefab);
                CheckPrefab(
                    problems,
                    "Support Stake",
                    farming.supportStakePrefab);
                CheckPrefab(
                    problems,
                    "Trellis",
                    farming.trellisPrefab);

                CheckTypedPrefab<IrrigationSystemInstance>(
                    problems,
                    "Irrigation System",
                    farming.irrigationSystemPrefab);
                CheckTypedPrefab<WaterStorageTankInstance>(
                    problems,
                    "Water Storage Tank",
                    farming.waterStorageTankPrefab);
                CheckTypedPrefab<ShadeNetInstance>(
                    problems,
                    "Shade Net",
                    farming.shadeNetPrefab);
                CheckTypedPrefab<WindbreakInstance>(
                    problems,
                    "Windbreak",
                    farming.windbreakPrefab);
                CheckTypedPrefab<GreenhouseInstance>(
                    problems,
                    "Greenhouse",
                    farming.greenhousePrefab);
                CheckTypedPrefab<DrainageCanalInstance>(
                    problems,
                    "Drainage Canal",
                    farming.drainageCanalPrefab);
            }

            if (inventory == null)
            {
                problems.Add("InventoryUIBuilder was not found in the open scene.");
            }
            else
            {
                CheckSprite(problems, "Mulch Bag", inventory.mulchBagSprite);
                CheckSprite(problems, "Organic Compost Bag", inventory.organicCompostBagSprite);
                CheckSprite(problems, "Pruning Shears", inventory.pruningShearsSprite);
                CheckSprite(problems, "Support Stake Kit", inventory.supportStakeKitSprite);
                CheckSprite(problems, "Trellis Kit", inventory.trellisKitSprite);
                CheckSprite(problems, "Raised Bed Kit", inventory.raisedBedKitSprite);
                CheckSprite(problems, "Irrigation System Kit", inventory.irrigationSystemKitSprite);
                CheckSprite(problems, "Water Storage Tank Kit", inventory.waterStorageTankKitSprite);
                CheckSprite(problems, "Shade Net Kit", inventory.shadeNetKitSprite);
                CheckSprite(problems, "Windbreak Kit", inventory.windbreakKitSprite);
                CheckSprite(problems, "Greenhouse Kit", inventory.greenhouseKitSprite);
                CheckSprite(problems, "Drainage Canal Kit", inventory.drainageCanalKitSprite);
            }

            if (problems.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Climate Maintenance Validation",
                    "All custom prefab and inventory sprite assignments are complete.",
                    "OK");
                Debug.Log("[ClimateMaintenance] Inspector assignment validation passed.");
                return;
            }

            string report = string.Join("\n- ", problems);
            EditorUtility.DisplayDialog(
                "Climate Maintenance Validation",
                "The following assignments need attention:\n\n- " + report,
                "OK");
            Debug.LogWarning(
                "[ClimateMaintenance] Inspector assignment problems:\n- " + report);
        }

        private static void CheckPrefab(
            List<string> problems,
            string label,
            GameObject prefab)
        {
            if (prefab == null)
            {
                problems.Add(label + " prefab is not assigned in FarmingSystem.");
            }
        }

        private static void CheckTypedPrefab<T>(
            List<string> problems,
            string label,
            GameObject prefab)
            where T : ClimateMitigationWorldObject
        {
            if (prefab == null)
            {
                problems.Add(label + " prefab is not assigned in FarmingSystem.");
                return;
            }

            ClimateMitigationWorldObject[] components =
                prefab.GetComponents<ClimateMitigationWorldObject>();
            if (components.Length > 1)
            {
                problems.Add(
                    label + " prefab has more than one climate mitigation component. " +
                    "Keep only " + typeof(T).Name + ".");
                return;
            }

            if (prefab.GetComponent<T>() == null)
            {
                problems.Add(
                    label + " prefab must have the " +
                    typeof(T).Name + " component on its root.");
            }
        }

        private static void CheckSprite(
            List<string> problems,
            string label,
            Sprite sprite)
        {
            if (sprite == null)
            {
                problems.Add(label + " sprite is not assigned in InventoryUI.");
            }
        }
    }
}
