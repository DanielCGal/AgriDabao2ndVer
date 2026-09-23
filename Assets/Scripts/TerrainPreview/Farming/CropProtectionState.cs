using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Stores persistent non-spray protection applied to one crop.
    /// </summary>
    public class CropProtectionState : MonoBehaviour
    {
        [Header("Installed Protection")]
        public bool hasFruitBag;
        public bool hasDrainageImprovement;

        [Header("Continuous Protection Per Game Day")]
        public float fruitBagReductionPerGameDay = 5f;
        public float drainageReductionPerGameDay = 4f;

        [Header("Optional Visual")]
        public GameObject fruitBagVisual;

        private PestDiseaseAffectedCrop disease;

        private void Awake()
        {
            disease =
                GetComponent<PestDiseaseAffectedCrop>();
        }

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
            if (elapsedGameDays <= 0f)
                return;

            if (disease == null)
            {
                disease =
                    GetComponent<PestDiseaseAffectedCrop>();
            }

            if (disease == null)
                return;

            if (hasFruitBag)
            {
                disease.ApplyMitigation(
                    PestDiseaseMitigation.FruitBag,
                    fruitBagReductionPerGameDay *
                    elapsedGameDays
                );
            }

            if (hasDrainageImprovement)
            {
                disease.ApplyMitigation(
                    PestDiseaseMitigation.DrainageImprovement,
                    drainageReductionPerGameDay *
                    elapsedGameDays
                );
            }
        }

        public bool InstallFruitBag(
            GameObject visualPrefab)
        {
            if (hasFruitBag)
                return false;

            hasFruitBag = true;

            if (visualPrefab != null)
            {
                fruitBagVisual = Instantiate(
                    visualPrefab,
                    transform
                );

                fruitBagVisual.name =
                    "FruitBagProtection_Runtime";

                fruitBagVisual.transform.localPosition =
                    visualPrefab.transform.localPosition;

                fruitBagVisual.transform.localRotation =
                    visualPrefab.transform.localRotation;

                fruitBagVisual.transform.localScale =
                    visualPrefab.transform.localScale;

                DistanceCullable.Attach(fruitBagVisual);
            }

            return true;
        }

        public bool InstallDrainageKit(
            float drainageIncrease)
        {
            if (hasDrainageImprovement)
                return false;

            hasDrainageImprovement = true;

            drainageIncrease =
                Mathf.Max(0f, drainageIncrease);

            CoconutTreeInstance coconut =
                GetComponent<CoconutTreeInstance>();

            if (coconut != null)
            {
                coconut.drainage = Mathf.Clamp01(
                    coconut.drainage + drainageIncrease
                );
            }

            BananaPlantInstance banana =
                GetComponent<BananaPlantInstance>();

            if (banana != null)
            {
                banana.drainage = Mathf.Clamp01(
                    banana.drainage + drainageIncrease
                );
            }

            TropicalCropPlantInstance tropical =
                GetComponent<TropicalCropPlantInstance>();

            if (tropical != null)
            {
                tropical.drainage = Mathf.Clamp01(
                    tropical.drainage + drainageIncrease
                );
            }

            return true;
        }
    }
}