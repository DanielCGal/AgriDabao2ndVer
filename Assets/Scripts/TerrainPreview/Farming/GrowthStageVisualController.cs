using UnityEngine;

namespace AgriDabao3D
{
    public enum PlantVisualStage
    {
        Sprout,
        SecondStage,
        Adult
    }

    [System.Serializable]
    public class PlantGrowthVisualSet
    {
        public GameObject sproutPrefab;
        public GameObject secondStagePrefab;
        public GameObject adultPrefab;

        [Header("Visual Height Offsets")]
        public float sproutYOffset = 0f;
        public float secondStageYOffset = 0f;
        public float adultYOffset = 0f;

        public GameObject GetPrefab(PlantVisualStage stage)
        {
            GameObject fallback =
                adultPrefab != null ? adultPrefab :
                secondStagePrefab != null ? secondStagePrefab :
                sproutPrefab;

            return stage switch
            {
                PlantVisualStage.Sprout => sproutPrefab != null ? sproutPrefab : fallback,
                PlantVisualStage.SecondStage => secondStagePrefab != null ? secondStagePrefab : fallback,
                PlantVisualStage.Adult => adultPrefab != null ? adultPrefab : fallback,
                _ => fallback
            };
        }

        public Vector3 GetLocalOffset(PlantVisualStage stage)
        {
            float y = stage switch
            {
                PlantVisualStage.Sprout => sproutYOffset,
                PlantVisualStage.SecondStage => secondStageYOffset,
                PlantVisualStage.Adult => adultYOffset,
                _ => 0f
            };

            return new Vector3(0f, y, 0f);
        }
    }

    public class GrowthStageVisualController : MonoBehaviour
    {
        [Header("Visual Prefabs")]
        public PlantGrowthVisualSet visuals = new PlantGrowthVisualSet();

        [Header("Safety")]
        public bool removeLogicComponentsFromVisuals = true;

        private GameObject currentVisual;
        private PlantVisualStage currentVisualStage = (PlantVisualStage)(-1);
        private float nextCheckTime;

        private void Start()
        {
            ForceRefresh();
        }

        private void LateUpdate()
        {
            if (Time.time < nextCheckTime)
                return;

            nextCheckTime = Time.time + 0.25f;
            ForceRefresh();
        }

        public void ForceRefresh()
        {
            PlantVisualStage targetStage = GetTargetVisualStage();

            if (currentVisual != null && targetStage == currentVisualStage)
            {
                currentVisual.transform.localPosition = visuals != null
                    ? visuals.GetLocalOffset(targetStage)
                    : Vector3.zero;

                return;
            }

            ApplyVisual(targetStage);
        }

        public PlantVisualStage GetTargetVisualStage()
        {
            CoconutTreeInstance coconut = GetComponent<CoconutTreeInstance>();
            if (coconut != null)
            {
                return coconut.stage switch
                {
                    CoconutStage.Seedling => PlantVisualStage.Sprout,
                    CoconutStage.Young => PlantVisualStage.Sprout,
                    CoconutStage.Immature => PlantVisualStage.SecondStage,
                    CoconutStage.Mature => PlantVisualStage.Adult,
                    CoconutStage.Old => PlantVisualStage.Adult,
                    _ => PlantVisualStage.Sprout
                };
            }

            BananaPlantInstance banana = GetComponent<BananaPlantInstance>();
            if (banana != null)
            {
                return banana.stage switch
                {
                    BananaStage.Seedling => PlantVisualStage.Sprout,
                    BananaStage.Vegetative => PlantVisualStage.Sprout,
                    BananaStage.PreFruiting => PlantVisualStage.SecondStage,
                    BananaStage.Fruiting => PlantVisualStage.Adult,
                    BananaStage.Old => PlantVisualStage.Adult,
                    _ => PlantVisualStage.Sprout
                };
            }

            TropicalCropPlantInstance tropical = GetComponent<TropicalCropPlantInstance>();
            if (tropical != null)
            {
                return tropical.stage switch
                {
                    TropicalCropStage.Seedling => PlantVisualStage.Sprout,
                    TropicalCropStage.Vegetative => PlantVisualStage.Sprout,
                    TropicalCropStage.PreFruiting => PlantVisualStage.SecondStage,
                    TropicalCropStage.Fruiting => PlantVisualStage.Adult,
                    TropicalCropStage.Old => PlantVisualStage.Adult,
                    _ => PlantVisualStage.Sprout
                };
            }

            return PlantVisualStage.Sprout;
        }

        private void ApplyVisual(PlantVisualStage stage)
        {
            GameObject prefab = visuals != null ? visuals.GetPrefab(stage) : null;
            if (prefab == null)
                return;

            if (currentVisual != null)
                Destroy(currentVisual);

            currentVisual = Instantiate(prefab, transform);
            currentVisual.name = prefab.name + "_Visual";
            currentVisual.transform.localPosition = visuals != null ? visuals.GetLocalOffset(stage) : Vector3.zero;
            currentVisual.transform.localRotation = prefab.transform.localRotation;
            currentVisual.transform.localScale = prefab.transform.localScale;

            if (removeLogicComponentsFromVisuals)
                RemoveDuplicateLogicComponents(currentVisual);

            DistanceCullable.Attach(currentVisual);

            currentVisualStage = stage;
        }

        private void RemoveDuplicateLogicComponents(GameObject visualRoot)
        {
            RemoveComponentsInChildren<CoconutTreeInstance>(visualRoot);
            RemoveComponentsInChildren<BananaPlantInstance>(visualRoot);
            RemoveComponentsInChildren<TropicalCropPlantInstance>(visualRoot);
            RemoveComponentsInChildren<PestDiseaseAffectedCrop>(visualRoot);
        }

        private void RemoveComponentsInChildren<T>(GameObject visualRoot) where T : Component
        {
            T[] components = visualRoot.GetComponentsInChildren<T>(true);

            foreach (T component in components)
            {
                if (component == null)
                    continue;

                if (component.gameObject == gameObject)
                    continue;

                Destroy(component);
            }
        }
    }
}
