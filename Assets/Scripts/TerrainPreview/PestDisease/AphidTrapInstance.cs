using System.Collections.Generic;
using UnityEngine;

namespace AgriDabao3D
{
    public class AphidTrapInstance : MonoBehaviour
    {
        [Header("Trap Effect")]
        public float radius = 12f;
        public float drainPercentPerCropPerGameDay = 22f;
        public float capacityPercent = 100f;

        [Header("Current Load")]
        [Range(0f, 100f)] public float deadAphidLoadPercent;

        public bool IsFull => deadAphidLoadPercent >= capacityPercent;

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
            if (elapsedGameDays <= 0f || IsFull)
                return;

            DrainNearbyAphids(
                elapsedGameDays
            );
        }

        private void DrainNearbyAphids(float deltaGameDays)
        {
            if (PestDiseaseSystem.Instance == null)
                return;

            List<PestDiseaseAffectedCrop> crops = PestDiseaseSystem.Instance.GetAllAffectedCrops();

            float remainingCapacity = capacityPercent - deadAphidLoadPercent;

            foreach (PestDiseaseAffectedCrop crop in crops)
            {
                if (crop == null || crop.aphidsPercent <= 0f)
                    continue;

                float distance = Vector3.Distance(transform.position, crop.transform.position);

                if (distance > radius)
                    continue;

                float distanceFactor = 1f - Mathf.Clamp01(distance / radius);

                float drain =
                    drainPercentPerCropPerGameDay *
                    distanceFactor *
                    deltaGameDays;

                drain = Mathf.Min(drain, remainingCapacity);

                float removed = crop.ReduceAphids(drain);

                deadAphidLoadPercent += removed;
                remainingCapacity -= removed;

                if (deadAphidLoadPercent >= capacityPercent)
                    break;
            }
        }

        public void CleanTrap()
        {
            deadAphidLoadPercent = 0f;
        }

        public string GetInspectionText()
        {
            return IsFull
                ? $"Aphid Trap: FULL ({deadAphidLoadPercent:F0}%). Clean it to make it usable again."
                : $"Aphid Trap Load: {deadAphidLoadPercent:F0}% / {capacityPercent:F0}%";
        }
    }
}