using UnityEngine;

namespace AgriDabao3D
{
    public enum PlantVisualStage
    {
        Sprout,
        SecondStage,
        Adult,

        Planted
    }

    [System.Serializable]
    public class PlantGrowthVisualSet
    {
        public GameObject sproutPrefab;
        public GameObject secondStagePrefab;
        public GameObject adultPrefab;

        [Tooltip("The planting material as it was put into the ground. Optional; " +
                 "without it the crop starts on its sprout model as before.")]
        public GameObject plantedPrefab;

        [Header("Visual Height Offsets")]
        public float sproutYOffset = 0f;
        public float secondStageYOffset = 0f;
        public float adultYOffset = 0f;

        [Tooltip("Share of the planted model's own height sunk into the soil. A " +
                 "seednut is planted with part of it showing; a seedling sits on " +
                 "the surface.")]
        [Range(0f, 0.9f)]
        public float plantedBuryFraction = 0.05f;

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
                PlantVisualStage.Planted => plantedPrefab != null
                    ? plantedPrefab
                    : sproutPrefab != null ? sproutPrefab : fallback,
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
                PlantVisualStage.Planted => sproutYOffset,
                _ => 0f
            };

            return new Vector3(0f, y, 0f);
        }
    }

    public class GrowthStageVisualController : MonoBehaviour
    {
        public const float PlantedVisualDays = 3f;

        [Header("Visual Prefabs")]
        public PlantGrowthVisualSet visuals = new PlantGrowthVisualSet();

        [Header("Safety")]
        public bool removeLogicComponentsFromVisuals = true;

        private GameObject currentVisual;
        private PlantVisualStage currentVisualStage = (PlantVisualStage)(-1);
        private float nextCheckTime;
        private float nextGroundCheckTime;
        private float lastGroundY = float.NaN;

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
                if (targetStage == PlantVisualStage.Planted)
                {
                    if (Time.time >= nextGroundCheckTime)
                    {
                        nextGroundCheckTime = Time.time + 2f;
                        SeatPlantedVisual();
                    }
                }
                else
                {
                    currentVisual.transform.localPosition = visuals != null
                        ? visuals.GetLocalOffset(targetStage)
                        : Vector3.zero;
                }

                KeepTapTargetOnGround();
                return;
            }

            ApplyVisual(targetStage);
            KeepTapTargetOnGround();
        }

        public PlantVisualStage GetTargetVisualStage()
        {
            CoconutTreeInstance coconut = GetComponent<CoconutTreeInstance>();
            if (coconut != null)
            {
                if (ShowsPlantedMaterial(coconut.fieldPlantedGameDay))
                    return PlantVisualStage.Planted;

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
                if (ShowsPlantedMaterial(banana.fieldPlantedGameDay))
                    return PlantVisualStage.Planted;

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
                if (ShowsPlantedMaterial(tropical.fieldPlantedGameDay))
                    return PlantVisualStage.Planted;

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

        private bool ShowsPlantedMaterial(float fieldPlantedGameDay)
        {
            if (visuals == null || visuals.plantedPrefab == null || fieldPlantedGameDay < 0f)
                return false;

            float now = GameTimeSystem.Instance != null
                ? GameTimeSystem.Instance.TotalGameDays
                : fieldPlantedGameDay;

            return now - fieldPlantedGameDay < PlantedVisualDays;
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

            currentVisualStage = stage;

            if (stage == PlantVisualStage.Planted)
            {
                SeatPlantedVisual();
                nextGroundCheckTime = Time.time + 2f;
            }

            DistanceCullable.Attach(currentVisual);
        }

        private void SeatPlantedVisual()
        {
            if (currentVisual == null || !TryGetGroundY(out float groundY))
                return;

            Renderer[] renderers = currentVisual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float bury = visuals != null ? Mathf.Clamp01(visuals.plantedBuryFraction) : 0f;
            float target = groundY - bounds.size.y * bury;
            currentVisual.transform.position += Vector3.up * (target - bounds.min.y);
        }

        private void KeepTapTargetOnGround()
        {
            CapsuleCollider tapTarget = GetComponent<CapsuleCollider>();
            if (tapTarget == null || !tapTarget.isTrigger)
                return;

            if (!TryGetGroundY(out float groundY))
                return;

            if (!float.IsNaN(lastGroundY) && Mathf.Abs(groundY - lastGroundY) < 0.01f)
                return;

            lastGroundY = groundY;

            float localGround = groundY - transform.position.y;
            tapTarget.center = new Vector3(0f, localGround + tapTarget.height * 0.5f, 0f);
        }

        private bool TryGetGroundY(out float groundY)
        {
            groundY = 0f;
            Terrain terrain = TemporaryTerrainGenerator.ResolveActiveTerrain();
            if (terrain == null || terrain.terrainData == null)
                return false;

            groundY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
            return true;
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
