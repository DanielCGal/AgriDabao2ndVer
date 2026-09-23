using System;
using UnityEngine;

namespace AgriDabao3D
{
    public class CropClimateMaintenanceState : MonoBehaviour
    {
        [Header("Persistent Crop Maintenance")]
        public bool hasMulch;
        public bool hasSupportStake;
        public bool hasTrellis;
        public bool hasRaisedBed;
        public float lastCompostGameDay = -999f;
        public float lastPrunedGameDay = -999f;
        public string raisedBedPatchId;
        public float raisedBedRootOffset;

        // One mulch prefab reused at three sizes. The pile reads correctly on a
        // full-grown crop but buries a sprout completely, swallowing the tap that
        // opens the crop information panel.
        //
        // These are MULTIPLIERS on the prefab's own scale, not absolute values.
        // The prefab root sits at 1 with the mesh scaled up inside it, so writing
        // an absolute number here would be multiplied by that mesh scale and
        // produce an enormous pile. Full size therefore means "exactly what the
        // prefab already was".
        private const float MulchScaleFull = 1f;
        private const float MulchScaleSprout = 0.4f;
        private const float MulchScalePineapple = 0.2f;
        private const float MulchScaleLowCrop = 0.5f;
        private const float MulchScaleTinySprout = 0.05f;

        private CropRuntimeAdapter crop;
        private GameObject mulchVisual;
        private GameObject supportVisual;
        private GameObject trellisVisual;
        private GrowthStageVisualController growthVisuals;
        private PlantVisualStage mulchStageApplied = (PlantVisualStage)(-1);
        private float nextMulchScaleCheck;
        private Vector3 mulchBaseScale = Vector3.one;

        public void Bind(CropRuntimeAdapter adapter)
        {
            crop = adapter;
        }

        private void Awake()
        {
            CropRuntimeAdapter.TryCreate(gameObject, out crop);
        }

        private void Update()
        {
            if (FarmLoadContext.IsRestoring || GameTimeSystem.Instance == null) return;
            if (crop == null && !CropRuntimeAdapter.TryCreate(gameObject, out crop)) return;
            SimulateTimeSkip(GameTimeSystem.Instance.DeltaGameDays);

            if (Time.time >= nextMulchScaleCheck)
            {
                nextMulchScaleCheck = Time.time + 0.25f;
                RefreshMulchScale();
            }
        }

        public void SimulateTimeSkip(float elapsedGameDays)
        {
            if (elapsedGameDays <= 0f || crop == null) return;
            WeatherEventType weather = WeatherSystem.Instance != null
                ? WeatherSystem.Instance.currentEvent
                : WeatherEventType.Clear;

            if (hasMulch)
            {
                float moistureSupport = weather == WeatherEventType.ExtremeDrought ? 0.018f : 0.004f;
                crop.AddMoisture(moistureSupport * elapsedGameDays);
                if (weather == WeatherEventType.ExtremeDrought) crop.AddStress(-0.8f * elapsedGameDays);
            }

            if (weather == WeatherEventType.Typhoon)
            {
                if (hasSupportStake) { crop.AddStress(-1.8f * elapsedGameDays); crop.AddHealth(0.30f * elapsedGameDays); }
                if (hasTrellis) { crop.AddStress(-1.5f * elapsedGameDays); crop.AddHealth(0.25f * elapsedGameDays); }
                if (hasRaisedBed) { crop.AddStress(-1.2f * elapsedGameDays); crop.AddHealth(0.20f * elapsedGameDays); }
            }
        }

        public bool ApplyAction(CropMaintenanceActionType action, Terrain terrain, out string message)
        {
            if (crop == null && !CropRuntimeAdapter.TryCreate(gameObject, out crop))
            {
                message = "Crop component was not found.";
                return false;
            }
            if (!CropMaintenanceCatalog.IsAllowed(crop.CropType, action))
            {
                message = action + " is not part of the recommended maintenance for " + crop.CropDisplayName + ".";
                return false;
            }

            float now = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : 0f;
            switch (action)
            {
                case CropMaintenanceActionType.Mulch:
                    if (hasMulch) { message = "Mulch is already installed on this crop."; return false; }
                    hasMulch = true;
                    crop.AddMoisture(0.08f);
                    crop.AddStress(-4f);
                    mulchVisual = CreateVisual(action, mulchVisual);
                    message = "Mulch installed. Moisture loss is reduced, especially during drought.";
                    return true;

                case CropMaintenanceActionType.OrganicCompost:
                    if (now - lastCompostGameDay < 12f) { message = "This crop was composted recently."; return false; }
                    lastCompostGameDay = now;
                    crop.AddFertility(0.14f);
                    crop.AddHealth(4f);
                    crop.AddStress(-3f);
                    message = "Organic compost applied. Fertility and crop recovery improved.";
                    return true;

                case CropMaintenanceActionType.Prune:
                    if (now - lastPrunedGameDay < 15f) { message = "This crop was pruned recently."; return false; }
                    lastPrunedGameDay = now;
                    crop.AddHealth(5f);
                    crop.AddStress(-7f);
                    message = "Pruning completed. Damaged growth was removed and airflow improved.";
                    return true;

                case CropMaintenanceActionType.SupportStake:
                    if (hasSupportStake) { message = "A support stake is already installed."; return false; }
                    hasSupportStake = true;
                    crop.AddStress(-3f);
                    supportVisual = CreateVisual(action, supportVisual);
                    message = "Support stake installed. The crop is less vulnerable to lodging and strong wind.";
                    return true;

                case CropMaintenanceActionType.Trellis:
                    if (hasTrellis) { message = "A trellis is already installed."; return false; }
                    hasTrellis = true;
                    crop.AddStress(-3f);
                    trellisVisual = CreateVisual(action, trellisVisual);
                    message = "Trellis installed. Vine support and airflow improved.";
                    return true;

                case CropMaintenanceActionType.RaisedBed:
                    if (hasRaisedBed) { message = "This crop already has a raised bed."; return false; }
                    if (terrain == null) { message = "The target terrain was not found."; return false; }
                    hasRaisedBed = true;
                    crop.AddDrainage(0.18f);
                    crop.AddStress(-4f);
                    if (string.IsNullOrWhiteSpace(raisedBedPatchId))
                        raisedBedPatchId = Guid.NewGuid().ToString("N");
                    ApplyRaisedBedAndSnapCrop(terrain, false);
                    message = "Raised bed created. Terrain height and crop drainage were improved.";
                    return true;
            }

            message = "No maintenance action was applied.";
            return false;
        }

        private GameObject CreateVisual(
    CropMaintenanceActionType action,
    GameObject current)
        {
            if (current != null)
            {
                return current;
            }

            FarmingInteractionSystem farmingSystem =
                UnityEngine.Object.FindFirstObjectByType<FarmingInteractionSystem>();

            GameObject prefab = farmingSystem != null
                ? farmingSystem.GetCropMaintenanceVisualPrefab(action)
                : null;

            if (prefab == null)
            {
                Debug.LogWarning(
                    "[ClimateMaintenance] No crop visual prefab is assigned for " +
                    action + " in FarmingSystem.");

                return null;
            }

            // Spawn using the crop as the parent, but do not reuse the
            // prefab root's saved scene position.
            GameObject visual = Instantiate(prefab, transform, false);
            visual.name = "ClimateMaintenance_" + action;

            DistanceCullable.Attach(visual);

            Vector3 localOffset = action switch
            {
                CropMaintenanceActionType.SupportStake =>
                    new Vector3(1.0f, 0f, 0f),

                CropMaintenanceActionType.Trellis =>
                    new Vector3(1.4f, 0f, 0f),

                _ => Vector3.zero
            };

            visual.transform.localPosition = localOffset;

            Rigidbody[] rigidbodies =
                visual.GetComponentsInChildren<Rigidbody>(true);

            foreach (Rigidbody body in rigidbodies)
            {
                if (body == null)
                {
                    continue;
                }

                body.useGravity = false;
                body.isKinematic = true;
            }

            if (action == CropMaintenanceActionType.Mulch)
            {
                mulchBaseScale = visual.transform.localScale;
                ApplyMulchScaleTo(visual);
            }

            SnapVisualBottomToTerrain(visual);

            return visual;
        }

        /// <summary>
        /// Mulch is a single prefab reused at five sizes rather than five separate
        /// prefabs, so the pile can follow the crop through its growth stages
        /// without ever being destroyed and respawned. Each size is chosen so the
        /// crop stays visible and tappable above the pile.
        /// </summary>
        public static float GetMulchScaleMultiplier(FarmCropType cropType, PlantVisualStage stage)
        {
            // Pineapple reuses one model for its sprout and second stage, and that
            // model sits low enough that even the sprout size hides it. It stays
            // small through both and only reaches full size on the adult prefab.
            if (cropType == FarmCropType.Pineapple)
            {
                return stage == PlantVisualStage.Adult
                    ? MulchScaleFull
                    : MulchScalePineapple;
            }

            // Squash, strawberry and tomato stay low and spreading at every stage, so
            // the full-size pile buries them even once grown. Their sprouts are
            // barely taller than the pile itself and need smaller still.
            if (cropType == FarmCropType.Squash ||
                cropType == FarmCropType.Strawberry ||
                cropType == FarmCropType.Tomato)
            {
                return stage == PlantVisualStage.Sprout
                    ? MulchScaleTinySprout
                    : MulchScaleLowCrop;
            }

            if (stage == PlantVisualStage.Sprout)
            {
                return MulchScaleSprout;
            }

            return MulchScaleFull;
        }

        private PlantVisualStage ResolveVisualStage()
        {
            if (growthVisuals == null)
            {
                growthVisuals = GetComponent<GrowthStageVisualController>();
            }

            // A crop with no stage controller never swaps prefabs either, so the
            // full-size pile remains correct for it.
            return growthVisuals != null
                ? growthVisuals.GetTargetVisualStage()
                : PlantVisualStage.Adult;
        }

        private void ApplyMulchScaleTo(GameObject visual)
        {
            if (visual == null || crop == null)
            {
                return;
            }

            PlantVisualStage stage = ResolveVisualStage();
            float multiplier = GetMulchScaleMultiplier(crop.CropType, stage);

            // Re-centre before scaling so repeated calls cannot drift the pile off
            // the crop. The caller re-seats it on the terrain afterwards.
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = mulchBaseScale * multiplier;

            mulchStageApplied = stage;
        }

        /// <summary>
        /// Keeps an installed mulch pile matched to the crop stage, so a sprout
        /// growing into its second-stage prefab grows its mulch with it.
        /// </summary>
        private void RefreshMulchScale()
        {
            if (mulchVisual == null || crop == null)
            {
                return;
            }

            if (ResolveVisualStage() == mulchStageApplied)
            {
                return;
            }

            ApplyMulchScaleTo(mulchVisual);
            SnapVisualBottomToTerrain(mulchVisual);
        }

        private void SnapVisualBottomToTerrain(GameObject visual)
        {
            if (visual == null)
            {
                return;
            }

            Terrain terrain = Terrain.activeTerrain;

            if (terrain == null)
            {
                terrain =
                    UnityEngine.Object.FindFirstObjectByType<Terrain>();
            }

            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            Vector3 visualPosition = visual.transform.position;

            float groundY =
                terrain.SampleHeight(visualPosition) +
                terrain.transform.position.y;

            Renderer[] renderers =
                visual.GetComponentsInChildren<Renderer>(true);

            bool hasBounds = false;
            Bounds combinedBounds = default;

            foreach (Renderer rendererComponent in renderers)
            {
                if (rendererComponent == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = rendererComponent.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(rendererComponent.bounds);
                }
            }

            if (hasBounds)
            {
                float requiredMovement =
                    groundY + 0.02f - combinedBounds.min.y;

                visual.transform.position +=
                    Vector3.up * requiredMovement;
            }
            else
            {
                visualPosition.y = groundY + 0.02f;
                visual.transform.position = visualPosition;
            }
        }

        private void ApplyRaisedBedAndSnapCrop(Terrain terrain, bool restoring)
        {
            if (terrain == null || crop == null) return;

            // Move the crop onto the vertex that will become the mound's peak before
            // raising anything. The heightmap can only lift vertices, so the peak
            // cannot land between them - without this the crop sits on the slope of
            // its own bed rather than on top.
            Vector3 position = TerrainModificationService.SnapToHeightmapVertex(
                terrain, crop.Transform.position);

            float baseGround = terrain.SampleHeight(position) + terrain.transform.position.y;
            if (restoring)
                position.y = baseGround + raisedBedRootOffset;
            else
                raisedBedRootOffset = position.y - baseGround;

            TerrainModificationService.ApplyRaisedBed(
    terrain,
    position,
    3.2f,
    0.60f,
    raisedBedPatchId
);
            float raisedGround = terrain.SampleHeight(position) + terrain.transform.position.y;
            position.y = raisedGround + raisedBedRootOffset;
            crop.Transform.position = position;
        }

        public CropClimateMaintenanceSaveDto CaptureSaveData()
        {
            return new CropClimateMaintenanceSaveDto
            {
                hasMulch = hasMulch,
                hasSupportStake = hasSupportStake,
                hasTrellis = hasTrellis,
                hasRaisedBed = hasRaisedBed,
                lastCompostGameDay = lastCompostGameDay,
                lastPrunedGameDay = lastPrunedGameDay,
                raisedBedPatchId = raisedBedPatchId,
                raisedBedRootOffset = raisedBedRootOffset
            };
        }

        public void RestoreSaveData(CropClimateMaintenanceSaveDto save, Terrain terrain)
        {
            if (save == null) return;
            if (crop == null) CropRuntimeAdapter.TryCreate(gameObject, out crop);
            hasMulch = save.hasMulch;
            hasSupportStake = save.hasSupportStake;
            hasTrellis = save.hasTrellis;
            hasRaisedBed = save.hasRaisedBed;
            lastCompostGameDay = save.lastCompostGameDay;
            lastPrunedGameDay = save.lastPrunedGameDay;
            raisedBedPatchId = save.raisedBedPatchId;
            raisedBedRootOffset = save.raisedBedRootOffset;

            if (hasMulch) mulchVisual = CreateVisual(CropMaintenanceActionType.Mulch, mulchVisual);
            if (hasSupportStake) supportVisual = CreateVisual(CropMaintenanceActionType.SupportStake, supportVisual);
            if (hasTrellis) trellisVisual = CreateVisual(CropMaintenanceActionType.Trellis, trellisVisual);
            if (hasRaisedBed && terrain != null && crop != null)
            {
                if (string.IsNullOrWhiteSpace(raisedBedPatchId)) raisedBedPatchId = Guid.NewGuid().ToString("N");
                ApplyRaisedBedAndSnapCrop(terrain, true);
            }
        }

        public string GetInspectionText()
        {
            return "Maintenance: " +
                   "Mulch=" + hasMulch +
                   ", Support=" + hasSupportStake +
                   ", Trellis=" + hasTrellis +
                   ", RaisedBed=" + hasRaisedBed;
        }
    }
}