using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AgriDabao3D
{
    public class ClimateMitigationWorldObject : MonoBehaviour
    {
        [Header("Identity")]
        public ClimateWorldMitigationType mitigationType;
        public string displayName = "Climate Mitigation";

        [Header("Area Effect")]
        [Min(0.1f)]
        public float radius = 14f;
        [Range(0.1f, 2f)]
        public float effectiveness = 1f;

        [Header("Stored Resource")]
        [Min(0f)]
        public float storedResource;

        /// <summary>
        /// The structure whose readout was last shown, so the developer tools can
        /// act on "the one you just clicked" rather than needing a picker of their
        /// own. Mirrors how the crop editor follows the inspected crop.
        /// </summary>
        public static ClimateMitigationWorldObject LastInspected { get; set; }

        /// <summary>Developer tools only: top the tank up without waiting for rain.</summary>
        public void DevFillResource()
        {
            storedResource = resourceCapacity;
        }
        [Min(1f)]
        public float resourceCapacity = 300f;

        [Header("Terrain Modification")]
        public string terrainPatchId;
        [Min(0.1f)]
        public float terrainLength = 16f;
        [Min(0.1f)]
        public float terrainWidth = 2f;
        [Min(0.01f)]
        public float terrainDelta = 0.45f;

        private float pendingGameDays;
        private Terrain terrain;

        protected virtual void Awake()
        {
            TemporaryTerrainGenerator generator =
                Object.FindFirstObjectByType<TemporaryTerrainGenerator>();
            terrain = generator != null ? generator.targetTerrain : null;
        }

        public void Initialize(
            ClimateWorldMitigationType type,
            Terrain targetTerrain,
            bool applyTerrain = true)
        {
            mitigationType = type;
            terrain = targetTerrain;
            ValidateConfiguration();

            if (string.IsNullOrWhiteSpace(displayName) ||
                displayName == "Climate Mitigation")
            {
                displayName = GetDefaultDisplayName(type);
            }

            if (string.IsNullOrWhiteSpace(terrainPatchId))
            {
                terrainPatchId = Guid.NewGuid().ToString("N");
            }

            if (applyTerrain &&
                type == ClimateWorldMitigationType.DrainageCanal &&
                terrain != null)
            {
                TerrainModificationService.ApplyDrainageCanal(
                    terrain,
                    transform.position,
                    transform.rotation,
                    terrainLength,
                    terrainWidth,
                    terrainDelta,
                    terrainPatchId);
            }
        }

        protected void ApplyRecommendedConfiguration(
            ClimateWorldMitigationType type)
        {
            mitigationType = type;
            effectiveness = 1f;

            switch (type)
            {
                case ClimateWorldMitigationType.IrrigationSystem:
                    displayName = "Irrigation System";
                    radius = 18f;
                    resourceCapacity = 300f;
                    break;
                case ClimateWorldMitigationType.WaterStorageTank:
                    displayName = "Water Storage Tank";
                    radius = 24f;
                    resourceCapacity = 500f;
                    break;
                case ClimateWorldMitigationType.ShadeNet:
                    displayName = "Shade Net";
                    radius = 12f;
                    resourceCapacity = 300f;
                    break;
                case ClimateWorldMitigationType.Windbreak:
                    displayName = "Windbreak";
                    radius = 20f;
                    resourceCapacity = 300f;
                    break;
                case ClimateWorldMitigationType.Greenhouse:
                    displayName = "Greenhouse";
                    radius = 10f;
                    resourceCapacity = 300f;
                    break;
                case ClimateWorldMitigationType.DrainageCanal:
                    displayName = "Drainage Canal";
                    radius = 16f;
                    resourceCapacity = 300f;
                    terrainLength = 16f;
                    terrainWidth = 2f;
                    terrainDelta = 0.45f;
                    break;
                default:
                    displayName = "Climate Mitigation";
                    radius = 14f;
                    resourceCapacity = 300f;
                    break;
            }

            ValidateConfiguration();
        }

        protected void ValidateConfiguration()
        {
            radius = Mathf.Max(0.1f, radius);
            effectiveness = Mathf.Clamp(effectiveness, 0.1f, 2f);
            storedResource = Mathf.Max(0f, storedResource);
            resourceCapacity = Mathf.Max(1f, resourceCapacity);
            storedResource = Mathf.Min(storedResource, resourceCapacity);
            terrainLength = Mathf.Max(0.1f, terrainLength);
            terrainWidth = Mathf.Max(0.1f, terrainWidth);
            terrainDelta = Mathf.Max(0.01f, terrainDelta);
        }

        protected void DrawRadiusGizmo(Color color)
        {
            ValidateConfiguration();
            Color previous = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position, radius);
            Gizmos.color = previous;
        }

        private void Update()
        {
            if (FarmLoadContext.IsRestoring ||
                GameTimeSystem.Instance == null)
            {
                return;
            }

            pendingGameDays += GameTimeSystem.Instance.DeltaGameDays;
            if (pendingGameDays < 0.02f)
            {
                return;
            }

            float elapsed = pendingGameDays;
            pendingGameDays = 0f;
            SimulateTimeSkip(elapsed);
        }

        public void SimulateTimeSkip(float elapsedGameDays)
        {
            if (elapsedGameDays <= 0f)
            {
                return;
            }

            WeatherEventType weather = WeatherSystem.Instance != null
                ? WeatherSystem.Instance.currentEvent
                : WeatherEventType.Clear;

            if (mitigationType == ClimateWorldMitigationType.WaterStorageTank)
            {
                if (weather == WeatherEventType.Rain)
                {
                    storedResource = Mathf.Min(
                        resourceCapacity,
                        storedResource + 45f * elapsedGameDays);
                }

                if (weather == WeatherEventType.Typhoon)
                {
                    storedResource = Mathf.Min(
                        resourceCapacity,
                        storedResource + 85f * elapsedGameDays);
                }

                return;
            }

            List<CropRuntimeAdapter> crops = CropRuntimeAdapter.FindAll();
            foreach (CropRuntimeAdapter crop in crops)
            {
                if (crop == null ||
                    Vector3.Distance(
                        transform.position,
                        crop.Transform.position) > radius)
                {
                    continue;
                }

                switch (mitigationType)
                {
                    case ClimateWorldMitigationType.IrrigationSystem:
                        ApplyIrrigation(crop, elapsedGameDays, weather);
                        break;
                    case ClimateWorldMitigationType.ShadeNet:
                        if (weather == WeatherEventType.ExtremeDrought)
                        {
                            crop.AddMoisture(
                                0.012f * effectiveness * elapsedGameDays);
                            crop.AddStress(
                                -1.4f * effectiveness * elapsedGameDays);
                        }
                        break;
                    case ClimateWorldMitigationType.Windbreak:
                        if (weather == WeatherEventType.Typhoon)
                        {
                            crop.AddStress(
                                -2.1f * effectiveness * elapsedGameDays);
                            crop.AddHealth(
                                0.35f * effectiveness * elapsedGameDays);
                        }
                        break;
                    case ClimateWorldMitigationType.Greenhouse:
                        if (weather == WeatherEventType.Typhoon)
                        {
                            crop.AddStress(
                                -1.8f * effectiveness * elapsedGameDays);
                            crop.AddHealth(
                                0.30f * effectiveness * elapsedGameDays);
                        }
                        else if (weather == WeatherEventType.ExtremeDrought)
                        {
                            crop.AddMoisture(
                                0.006f * effectiveness * elapsedGameDays);
                        }
                        break;
                    case ClimateWorldMitigationType.DrainageCanal:
                        if (weather == WeatherEventType.Typhoon ||
                            weather == WeatherEventType.Rain)
                        {
                            crop.AddMoisture(
                                -0.010f * effectiveness * elapsedGameDays);
                            crop.AddStress(
                                -1.9f * effectiveness * elapsedGameDays);
                            crop.AddHealth(
                                0.25f * effectiveness * elapsedGameDays);
                        }
                        break;
                }
            }
        }

        private void ApplyIrrigation(
            CropRuntimeAdapter crop,
            float elapsedGameDays,
            WeatherEventType weather)
        {
            if (weather != WeatherEventType.ExtremeDrought &&
                crop.Moisture >= 0.55f)
            {
                return;
            }

            float requested = 9f * elapsedGameDays;
            ClimateMitigationWorldObject tank = FindNearestTank();
            float supplyFactor = 0.35f;

            if (tank != null && tank.storedResource > 0f)
            {
                float used = Mathf.Min(requested, tank.storedResource);
                tank.storedResource -= used;
                supplyFactor = requested <= 0f
                    ? 1f
                    : Mathf.Clamp01(used / requested);
            }

            crop.AddMoisture(
                0.035f * effectiveness * supplyFactor * elapsedGameDays);
            crop.AddStress(
                -1.2f * effectiveness * supplyFactor * elapsedGameDays);
        }

        private ClimateMitigationWorldObject FindNearestTank()
        {
            ClimateMitigationWorldObject best = null;
            float bestDistance = float.MaxValue;

            foreach (ClimateMitigationWorldObject item in
                     Object.FindObjectsByType<ClimateMitigationWorldObject>(
                         FindObjectsSortMode.None))
            {
                if (item == null ||
                    item == this ||
                    item.mitigationType !=
                    ClimateWorldMitigationType.WaterStorageTank)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    transform.position,
                    item.transform.position);
                if (distance <= item.radius && distance < bestDistance)
                {
                    best = item;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public WorldObjectSaveDto CaptureSaveData()
        {
            return new WorldObjectSaveDto
            {
                objectType = "ClimateMitigation",
                climateMitigationType = mitigationType.ToString(),
                position = new SerializableVector3(transform.position),
                rotation = new SerializableQuaternion(transform.rotation),
                radius = radius,
                effectiveness = effectiveness,
                storedResource = storedResource,
                resourceCapacity = resourceCapacity,
                terrainPatchId = terrainPatchId,
                terrainLength = terrainLength,
                terrainWidth = terrainWidth,
                terrainDelta = terrainDelta
            };
        }

        public static bool IsClimateSave(WorldObjectSaveDto save)
        {
            return save != null &&
                   string.Equals(
                       save.objectType,
                       "ClimateMitigation",
                       StringComparison.OrdinalIgnoreCase);
        }

        public static GameObject RestoreFromSave(
            WorldObjectSaveDto save,
            Terrain terrain)
        {
            if (!IsClimateSave(save))
            {
                return null;
            }

            if (!Enum.TryParse(
                    save.climateMitigationType,
                    out ClimateWorldMitigationType type))
            {
                return null;
            }

            FarmingInteractionSystem farmingSystem =
                Object.FindFirstObjectByType<FarmingInteractionSystem>();
            GameObject prefab = farmingSystem != null
                ? farmingSystem.GetClimateMitigationPrefab(type)
                : null;

            GameObject go = CreateRuntimeObject(
                type,
                save.position.ToVector3(),
                save.rotation.ToQuaternion(),
                prefab,
                terrain,
                false,
                true);

            if (go == null)
            {
                return null;
            }

            ClimateMitigationWorldObject item =
                go.GetComponent<ClimateMitigationWorldObject>();
            item.radius = save.radius > 0f
                ? save.radius
                : item.radius;
            item.effectiveness = save.effectiveness > 0f
                ? save.effectiveness
                : item.effectiveness;
            item.storedResource = Mathf.Max(0f, save.storedResource);
            item.resourceCapacity = save.resourceCapacity > 0f
                ? save.resourceCapacity
                : item.resourceCapacity;
            item.terrainPatchId = save.terrainPatchId;
            item.terrainLength = save.terrainLength > 0f
                ? save.terrainLength
                : item.terrainLength;
            item.terrainWidth = save.terrainWidth > 0f
                ? save.terrainWidth
                : item.terrainWidth;
            item.terrainDelta = save.terrainDelta > 0f
                ? save.terrainDelta
                : item.terrainDelta;
            item.ValidateConfiguration();

            if (type == ClimateWorldMitigationType.DrainageCanal &&
                terrain != null)
            {
                if (string.IsNullOrWhiteSpace(item.terrainPatchId))
                {
                    item.terrainPatchId = Guid.NewGuid().ToString("N");
                }

                TerrainModificationService.ApplyDrainageCanal(
                    terrain,
                    item.transform.position,
                    item.transform.rotation,
                    item.terrainLength,
                    item.terrainWidth,
                    item.terrainDelta,
                    item.terrainPatchId);
            }

            return go;
        }

        public static GameObject CreateRuntimeObject(
            ClimateWorldMitigationType type,
            Vector3 position,
            Quaternion rotation,
            GameObject prefab,
            Terrain terrain,
            bool applyTerrain,
            bool allowFallback)
        {
            GameObject go;
            bool usedFallback = prefab == null;

            if (prefab != null)
            {
                go = Instantiate(prefab, position, rotation);
            }
            else if (allowFallback)
            {
                Debug.LogWarning(
                    "[ClimateMaintenance] No assigned prefab for " +
                    type + ". A fallback primitive was created so the saved " +
                    "farm can still load.");
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetPositionAndRotation(position, rotation);
                go.transform.localScale =
                    type == ClimateWorldMitigationType.DrainageCanal
                        ? new Vector3(2f, 0.25f, 16f)
                        : new Vector3(2f, 2f, 2f);
            }
            else
            {
                return null;
            }

            go.name = type + "_Runtime";

            ClimateMitigationWorldObject component =
                go.GetComponent<ClimateMitigationWorldObject>();
            bool componentWasAdded = component == null;
            if (componentWasAdded)
            {
                component = AddCorrectComponent(go, type);
            }

            if (component == null)
            {
                Object.Destroy(go);
                return null;
            }

            if (componentWasAdded || usedFallback)
            {
                component.ApplyRecommendedConfiguration(type);
            }

            component.Initialize(type, terrain, applyTerrain);

            Rigidbody[] bodies =
                go.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody body in bodies)
            {
                if (body == null)
                {
                    continue;
                }

                body.isKinematic = true;
                body.useGravity = false;
            }

            DistanceCullable.Attach(go);

            return go;
        }

        private static ClimateMitigationWorldObject AddCorrectComponent(
            GameObject go,
            ClimateWorldMitigationType type)
        {
            switch (type)
            {
                case ClimateWorldMitigationType.IrrigationSystem:
                    return go.AddComponent<IrrigationSystemInstance>();
                case ClimateWorldMitigationType.WaterStorageTank:
                    return go.AddComponent<WaterStorageTankInstance>();
                case ClimateWorldMitigationType.ShadeNet:
                    return go.AddComponent<ShadeNetInstance>();
                case ClimateWorldMitigationType.Windbreak:
                    return go.AddComponent<WindbreakInstance>();
                case ClimateWorldMitigationType.Greenhouse:
                    return go.AddComponent<GreenhouseInstance>();
                case ClimateWorldMitigationType.DrainageCanal:
                    return go.AddComponent<DrainageCanalInstance>();
                default:
                    return go.AddComponent<ClimateMitigationWorldObject>();
            }
        }

        public string GetInspectionText()
        {
            string resource =
                mitigationType == ClimateWorldMitigationType.WaterStorageTank
                    ? "Water: " + storedResource.ToString("F0") +
                      "/" + resourceCapacity.ToString("F0")
                    : string.Empty;

            return displayName +
                   "Radius: " + radius.ToString("F1") + " m" +
                   resource;
        }

        private static string GetDefaultDisplayName(
            ClimateWorldMitigationType type)
        {
            switch (type)
            {
                case ClimateWorldMitigationType.IrrigationSystem:
                    return "Irrigation System";
                case ClimateWorldMitigationType.WaterStorageTank:
                    return "Water Storage Tank";
                case ClimateWorldMitigationType.ShadeNet:
                    return "Shade Net";
                case ClimateWorldMitigationType.Windbreak:
                    return "Windbreak";
                case ClimateWorldMitigationType.Greenhouse:
                    return "Greenhouse";
                case ClimateWorldMitigationType.DrainageCanal:
                    return "Drainage Canal";
                default:
                    return type.ToString();
            }
        }
    }
}
