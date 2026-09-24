using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public class AreaMitigationTrapInstance : MonoBehaviour
    {
        [Header("Identity")]
        public string displayName = "Mitigation Trap";
        public PestDiseaseMitigation mitigation =
            PestDiseaseMitigation.PheromoneTrap;

        [Header("Area Effect")]
        [Min(0.1f)]
        public float radius = 14f;

        [Min(0f)]
        public float reductionPercentPerCropPerGameDay = 18f;

        [Header("Capacity")]
        [Min(1f)]
        public float capacityPercent = 150f;

        [Min(0f)]
        public float capturedLoadPercent;

        public bool IsFull =>
            capturedLoadPercent >= capacityPercent;

        private void Update()
        {
            if (GameTimeSystem.Instance == null)
                return;

            SimulateTimeSkip(
                GameTimeSystem.Instance.DeltaGameDays
            );
        }

        public void SimulateTimeSkip(
            float elapsedGameDays)
        {
            if (elapsedGameDays <= 0f ||
                PestDiseaseSystem.Instance == null ||
                IsFull)
            {
                return;
            }

            ApplyAreaEffect(
                elapsedGameDays
            );
        }

        private void ApplyAreaEffect(float deltaGameDays)
        {
            List<PestDiseaseAffectedCrop> crops =
                PestDiseaseSystem.Instance.GetAllAffectedCrops();

            foreach (PestDiseaseAffectedCrop crop in crops)
            {
                if (crop == null || IsFull)
                    continue;

                float distance = Vector3.Distance(
                    transform.position,
                    crop.transform.position
                );

                if (distance > radius)
                    continue;

                float distanceFactor =
                    1f - Mathf.Clamp01(distance / radius);

                float requestedReduction =
                    reductionPercentPerCropPerGameDay *
                    distanceFactor *
                    deltaGameDays;

                float remainingCapacity =
                    capacityPercent - capturedLoadPercent;

                requestedReduction = Mathf.Min(
                    requestedReduction,
                    remainingCapacity
                );

                if (requestedReduction <= 0f)
                    break;

                float removed = crop.ApplyMitigation(
                    mitigation,
                    requestedReduction
                );

                capturedLoadPercent = Mathf.Min(
                    capacityPercent,
                    capturedLoadPercent + removed
                );
            }
        }

        public void CleanTrap()
        {
            capturedLoadPercent = 0f;
        }

        public string GetInspectionText()
        {
            if (IsFull)
            {
                return
                    $"{displayName}: FULL\n" +
                    $"Load: {capturedLoadPercent:F0} / " +
                    $"{capacityPercent:F0}\n" +
                    "Clean the trap to reactivate it.";
            }

            return
                $"{displayName}\n" +
                $"Radius: {radius:F1} meters\n" +
                $"Load: {capturedLoadPercent:F0} / " +
                $"{capacityPercent:F0}";
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(
                transform.position,
                radius
            );
        }
#endif
    }
}