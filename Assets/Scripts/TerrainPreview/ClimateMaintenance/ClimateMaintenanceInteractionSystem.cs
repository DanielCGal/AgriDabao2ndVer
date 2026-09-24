using UnityEngine;

namespace AgriDabao3D
{
    public class ClimateMaintenanceInteractionSystem : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        public TemporaryTerrainGenerator terrainGenerator;
        public FarmingInteractionSystem farmingSystem;
        public ClimateMaintenanceToastUI toast;

        [Header("Placement")]
        [Min(0.1f)]
        public float minimumWorldObjectSpacing = 4f;

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (terrainGenerator == null)
            {
                terrainGenerator =
                    Object.FindFirstObjectByType<TemporaryTerrainGenerator>();
            }

            if (farmingSystem == null)
            {
                farmingSystem =
                    Object.FindFirstObjectByType<FarmingInteractionSystem>();
            }

            if (toast == null)
            {
                toast =
                    Object.FindFirstObjectByType<ClimateMaintenanceToastUI>();
            }
        }

        public bool HandleInteractAtScreenPosition(Vector2 screenPosition)
        {
            ResolveReferences();

            if (playerCamera == null || PlayerInventory.Instance == null)
            {
                return false;
            }

            Ray ray = playerCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 5000f))
            {
                return false;
            }

            if (hit.collider.GetComponentInParent<SeedlingTentInstance>() != null)
                return false;

            if (hit.collider.GetComponentInParent<DigSpot>() != null)
            {
                bool transplanting = NurserySystem.Instance != null && NurserySystem.Instance.IsTransplanting;
                bool structureKit = ClimateMaintenanceItemCatalog.TryGetWorldMitigation(
                    PlayerInventory.Instance.selectedItem, out _);

                if (transplanting || !structureKit)
                    return false;
            }

            ClimateMitigationWorldObject clickedWorld =
                hit.collider.GetComponentInParent<ClimateMitigationWorldObject>();
            InventoryItemType selected = PlayerInventory.Instance.selectedItem;

            if (clickedWorld != null &&
                !ClimateMaintenanceItemCatalog.IsClimateMaintenanceItem(selected))
            {
                ClimateMitigationWorldObject.LastInspected = clickedWorld;
                Show(clickedWorld.GetInspectionText());
                return true;
            }

            if (ClimateMaintenanceItemCatalog.TryGetCropAction(
                    selected,
                    out CropMaintenanceActionType cropAction))
            {
                return HandleCropAction(hit, selected, cropAction);
            }

            if (ClimateMaintenanceItemCatalog.TryGetWorldMitigation(
                    selected,
                    out ClimateWorldMitigationType worldType))
            {
                return HandleWorldPlacement(
    screenPosition,
    selected,
    worldType);
            }

            return false;
        }

        private bool HandleCropAction(
            RaycastHit hit,
            InventoryItemType item,
            CropMaintenanceActionType action)
        {
            if (!PlayerInventory.Instance.HasItem(item, 1))
            {
                Show("You do not own " +
                     ClimateMaintenanceItemCatalog.FriendlyName(item) + ".");
                return true;
            }

            if (!CropRuntimeAdapter.TryFromCollider(
                    hit.collider,
                    out CropRuntimeAdapter crop))
            {
                RecordAttempt(
                    "Use" + action,
                    item,
                    null,
                    hit.point,
                    false,
                    "No crop was selected.");
                Show(ClimateMaintenanceItemCatalog.FriendlyName(item) +
                     " must be used directly on a crop.");
                return true;
            }

            if (!IsWithinReach(crop.Transform.position))
            {
                Show("That crop is too far away. Move closer.");
                return true;
            }

            float beforeMoisture = crop.Moisture;
            float beforeHealth = crop.Health;
            float beforeStress = crop.Stress;
            float beforeDrainage = crop.Drainage;
            float beforeFertility = crop.Fertility;

            CropClimateMaintenanceState state =
                crop.GetOrAddMaintenanceState();
            Terrain terrain = ResolveTargetTerrain();

            bool applied = state.ApplyAction(
                action,
                terrain,
                out string message);

            if (applied &&
                !ClimateMaintenanceItemCatalog.IsReusableTool(item))
            {
                if (!PlayerInventory.Instance.ConsumeItem(item, 1))
                {
                    message = "The item could not be consumed from inventory.";
                    applied = false;
                }
            }

            RecordAttempt(
                action.ToString(),
                item,
                crop,
                hit.point,
                applied,
                message,
                beforeMoisture,
                beforeHealth,
                beforeStress,
                beforeDrainage,
                beforeFertility);

            if (applied)
                GameAudioManager.Instance.PlayPlaceMitigation();

            Show(message);
            return true;
        }

        private bool HandleWorldPlacement(
    Vector2 screenPosition,
    InventoryItemType item,
    ClimateWorldMitigationType type)
        {
            Terrain terrain = ResolveTargetTerrain();

            if (terrain == null || terrain.terrainData == null)
            {
                string terrainMessage =
                    "The generated terrain is not ready.";

                RecordAttempt(
                    "Place" + type,
                    item,
                    null,
                    playerCamera != null
                        ? playerCamera.transform.position
                        : Vector3.zero,
                    false,
                    terrainMessage);

                Show(terrainMessage);
                return true;
            }

            if (!TryGetTerrainPlacement(
                    screenPosition,
                    terrain,
                    out Vector3 position,
                    out Vector3 terrainNormal))
            {
                string placementMessage =
                    ClimateMaintenanceItemCatalog.FriendlyName(item) +
                    " must be placed on terrain.";

                RecordAttempt(
                    "Place" + type,
                    item,
                    null,
                    position,
                    false,
                    "No generated-terrain hit was found.");

                Show(placementMessage);
                return true;
            }

            if (!PlayerInventory.Instance.HasItem(item, 1))
            {
                Show(
                    "You do not own " +
                    ClimateMaintenanceItemCatalog.FriendlyName(item) +
                    ".");

                return true;
            }

            if (farmingSystem == null)
            {
                farmingSystem =
                    UnityEngine.Object.FindFirstObjectByType
                        <FarmingInteractionSystem>();
            }

            if (!IsWithinReach(position))
            {
                Show("That ground is too far away. Move closer to build here.");
                return true;
            }

            if (farmingSystem == null)
            {
                string missingSystemMessage =
                    "FarmingInteractionSystem was not found in the scene.";

                RecordAttempt(
                    "Place" + type,
                    item,
                    null,
                    position,
                    false,
                    missingSystemMessage);

                Show(missingSystemMessage);
                return true;
            }

            GameObject prefab =
                farmingSystem.GetClimateMitigationPrefab(type);

            if (prefab == null)
            {
                string missingPrefabMessage =
                    ClimateMaintenanceItemCatalog.FriendlyName(item) +
                    " prefab is not assigned in FarmingSystem.";

                RecordAttempt(
                    "Place" + type,
                    item,
                    null,
                    position,
                    false,
                    missingPrefabMessage);

                Show(missingPrefabMessage);
                return true;
            }

            SeedlingTentInstance tent = NurserySystem.Instance != null
                ? NurserySystem.Instance.Tent
                : UnityEngine.Object.FindFirstObjectByType<SeedlingTentInstance>();
            if (tent != null)
            {
                Vector3 fromTent = position - tent.transform.position;
                fromTent.y = 0f;
                if (fromTent.magnitude < minimumWorldObjectSpacing + 3f)
                {
                    string tentMessage = "That is too close to the Seedling Tent.";
                    RecordAttempt("Place" + type, item, null, position, false, tentMessage);
                    Show(tentMessage);
                    return true;
                }
            }

            foreach (ClimateMitigationWorldObject existing in
                     UnityEngine.Object.FindObjectsByType
                         <ClimateMitigationWorldObject>(
                             FindObjectsSortMode.None))
            {
                if (existing == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    existing.transform.position,
                    position);

                if (distance < minimumWorldObjectSpacing)
                {
                    string spacingMessage =
                        "Another climate mitigation object is too close " +
                        "to this position.";

                    RecordAttempt(
                        "Place" + type,
                        item,
                        null,
                        position,
                        false,
                        spacingMessage);

                    Show(spacingMessage);
                    return true;
                }
            }

            Quaternion placementRotation;

            if (type == ClimateWorldMitigationType.DrainageCanal ||
                type == ClimateWorldMitigationType.Windbreak ||
                type == ClimateWorldMitigationType.Greenhouse ||
                type == ClimateWorldMitigationType.ShadeNet)
            {
                placementRotation = Quaternion.Euler(
                    0f,
                    playerCamera.transform.eulerAngles.y,
                    0f);
            }
            else
            {
                placementRotation =
                    Quaternion.FromToRotation(
                        Vector3.up,
                        terrainNormal);
            }

            Quaternion finalRotation =
                placementRotation * prefab.transform.rotation;

            GameObject runtimeObject =
                ClimateMitigationWorldObject.CreateRuntimeObject(
                    type,
                    position,
                    finalRotation,
                    prefab,
                    terrain,
                    true,
                    false);

            if (runtimeObject == null)
            {
                string creationMessage =
                    "Could not create " +
                    ClimateMaintenanceItemCatalog.FriendlyName(item) +
                    ".";

                RecordAttempt(
                    "Place" + type,
                    item,
                    null,
                    position,
                    false,
                    creationMessage);

                Show(creationMessage);
                return true;
            }

            EnsureCollider(runtimeObject);

            if (type == ClimateWorldMitigationType.ShadeNet ||
                type == ClimateWorldMitigationType.Windbreak ||
                type == ClimateWorldMitigationType.Greenhouse)
            {
                MakeCollidersPassThrough(runtimeObject);
            }

            FreezePlacedObject(runtimeObject);

            Vector3 placementOffset =
                farmingSystem.GetClimateMitigationPlacementOffset(type);

            SnapObjectBottomToTerrain(runtimeObject, terrain, placementOffset.y);

            if (placementOffset.x != 0f || placementOffset.z != 0f)
            {
                runtimeObject.transform.position += runtimeObject.transform.TransformVector(
                    new Vector3(placementOffset.x, 0f, placementOffset.z));
            }

            if (!PlayerInventory.Instance.ConsumeItem(item, 1))
            {
                Destroy(runtimeObject);

                Show(
                    "The item could not be consumed from inventory.");

                return true;
            }

            ClimateMitigationWorldObject component =
                runtimeObject.GetComponent
                    <ClimateMitigationWorldObject>();

            string successMessage =
                ClimateMaintenanceItemCatalog.FriendlyName(item) +
                " placed successfully.\nEffect radius: " +
                (component != null
                    ? component.radius
                    : 0f).ToString("F1") +
                " meters.";

            RecordAttempt(
                "Place" + type,
                item,
                null,
                runtimeObject.transform.position,
                true,
                successMessage);

            GameAudioManager.Instance.PlayPlaceMitigation();
            Show(successMessage);
            return true;
        }

        private Terrain ResolveTargetTerrain()
        {
            if (terrainGenerator != null &&
                terrainGenerator.targetTerrain != null)
            {
                return terrainGenerator.targetTerrain;
            }

            TemporaryTerrainGenerator[] generators =
                UnityEngine.Object.FindObjectsByType
                    <TemporaryTerrainGenerator>(
                        FindObjectsSortMode.None);

            foreach (TemporaryTerrainGenerator generator in generators)
            {
                if (generator == null ||
                    generator.targetTerrain == null)
                {
                    continue;
                }

                terrainGenerator = generator;
                return generator.targetTerrain;
            }

            if (Terrain.activeTerrain != null)
            {
                return Terrain.activeTerrain;
            }

            return UnityEngine.Object.FindFirstObjectByType<Terrain>();
        }

        private bool TryGetTerrainPlacement(
            Vector2 screenPosition,
            Terrain terrain,
            out Vector3 position,
            out Vector3 terrainNormal)
        {
            position = Vector3.zero;
            terrainNormal = Vector3.up;

            if (playerCamera == null ||
                terrain == null ||
                terrain.terrainData == null)
            {
                return false;
            }

            Ray ray =
                playerCamera.ScreenPointToRay(screenPosition);

            TerrainCollider terrainCollider =
                terrain.GetComponent<TerrainCollider>();

            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                5000f,
                ~0,
                QueryTriggerInteraction.Ignore);

            bool terrainHitFound = false;
            RaycastHit nearestTerrainHit = default;
            float nearestTerrainDistance = float.MaxValue;

            bool anyHitFound = false;
            RaycastHit nearestAnyHit = default;
            float nearestAnyDistance = float.MaxValue;

            foreach (RaycastHit candidate in hits)
            {
                if (candidate.collider == null)
                {
                    continue;
                }

                if (candidate.distance < nearestAnyDistance)
                {
                    nearestAnyDistance = candidate.distance;
                    nearestAnyHit = candidate;
                    anyHitFound = true;
                }

                bool belongsToTerrain =
                    candidate.collider == terrainCollider ||
                    candidate.collider.GetComponentInParent<Terrain>()
                        == terrain;

                if (belongsToTerrain &&
                    candidate.distance < nearestTerrainDistance)
                {
                    nearestTerrainDistance = candidate.distance;
                    nearestTerrainHit = candidate;
                    terrainHitFound = true;
                }
            }

            Vector3 sampledPoint;

            if (terrainHitFound)
            {
                sampledPoint = nearestTerrainHit.point;
            }
            else if (anyHitFound &&
                     IsPointInsideTerrain(
                         terrain,
                         nearestAnyHit.point))
            {
                sampledPoint = nearestAnyHit.point;
            }
            else
            {
                return false;
            }

            if (!IsPointInsideTerrain(terrain, sampledPoint))
            {
                return false;
            }

            position = sampledPoint;
            position.y =
                terrain.SampleHeight(position) +
                terrain.transform.position.y;

            terrainNormal =
                GetTerrainNormal(terrain, position);

            return true;
        }

        private static bool IsPointInsideTerrain(
            Terrain terrain,
            Vector3 point)
        {
            if (terrain == null ||
                terrain.terrainData == null)
            {
                return false;
            }

            Vector3 terrainPosition =
                terrain.transform.position;

            Vector3 terrainSize =
                terrain.terrainData.size;

            return
                point.x >= terrainPosition.x &&
                point.x <= terrainPosition.x + terrainSize.x &&
                point.z >= terrainPosition.z &&
                point.z <= terrainPosition.z + terrainSize.z;
        }

        private static Vector3 GetTerrainNormal(
            Terrain terrain,
            Vector3 worldPosition)
        {
            if (terrain == null ||
                terrain.terrainData == null)
            {
                return Vector3.up;
            }

            TerrainData data = terrain.terrainData;
            Vector3 terrainPosition =
                terrain.transform.position;

            float normalizedX = Mathf.Clamp01(
                (worldPosition.x - terrainPosition.x) /
                data.size.x);

            float normalizedZ = Mathf.Clamp01(
                (worldPosition.z - terrainPosition.z) /
                data.size.z);

            return data.GetInterpolatedNormal(
                normalizedX,
                normalizedZ);
        }

        private static void EnsureCollider(GameObject go)
        {
            if (go == null || go.GetComponentInChildren<Collider>(true) != null)
            {
                return;
            }

            go.AddComponent<BoxCollider>();
        }

        private static void FreezePlacedObject(GameObject placedObject)
        {
            if (placedObject == null)
            {
                return;
            }

            Rigidbody[] bodies =
                placedObject.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody body in bodies)
            {
                if (body == null)
                {
                    continue;
                }

                body.useGravity = false;
                body.isKinematic = true;
            }
        }

        private static void SnapObjectBottomToTerrain(
            GameObject go,
            Terrain terrain,
            float yOffset)
        {
            if (go == null || terrain == null)
            {
                return;
            }

            Vector3 position = go.transform.position;
            float groundY = terrain.SampleHeight(position) +
                            terrain.transform.position.y;

            Renderer[] renderers =
                go.GetComponentsInChildren<Renderer>(true);
            if (renderers != null && renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }
                }

                float moveY = (groundY + yOffset) - bounds.min.y;
                go.transform.position += new Vector3(0f, moveY, 0f);
            }
            else
            {
                position.y = groundY + yOffset;
                go.transform.position = position;
            }
        }

        private void RecordAttempt(
            string actionType,
            InventoryItemType item,
            CropRuntimeAdapter crop,
            Vector3 position,
            bool applied,
            string details,
            float beforeMoisture = 0f,
            float beforeHealth = 0f,
            float beforeStress = 0f,
            float beforeDrainage = 0f,
            float beforeFertility = 0f)
        {
            ClimateEventTracker tracker = ClimateEventTracker.Instance;
            if (tracker == null || !tracker.IsTrackingEvent)
            {
                return;
            }

            float score;
            if (ClimateMaintenanceItemCatalog.TryGetCropAction(
                    item,
                    out CropMaintenanceActionType cropAction))
            {
                score = crop != null
                    ? CropMaintenanceCatalog.ScoreCropAction(
                        tracker.ActiveEventType,
                        crop.CropType,
                        cropAction)
                    : -2f;
            }
            else if (ClimateMaintenanceItemCatalog.TryGetWorldMitigation(
                         item,
                         out ClimateWorldMitigationType worldAction))
            {
                score = CropMaintenanceCatalog.ScoreWorldAction(
                    tracker.ActiveEventType,
                    worldAction);
            }
            else
            {
                score = 0f;
            }

            if (!applied)
            {
                score = Mathf.Min(score, -1f);
            }

            tracker.RecordAction(new ClimateActionRecord
            {
                actionType = actionType,
                itemType = item.ToString(),
                cropType = crop != null ? crop.CropDisplayName : string.Empty,
                cropId = crop != null ? crop.CropId : string.Empty,
                gameDay = GameTimeSystem.Instance != null
                    ? GameTimeSystem.Instance.TotalGameDays
                    : 0f,
                worldPosition = new SerializableVector3(position),
                beforeMoisture = beforeMoisture,
                afterMoisture = crop != null
                    ? crop.Moisture
                    : beforeMoisture,
                beforeHealth = beforeHealth,
                afterHealth = crop != null
                    ? crop.Health
                    : beforeHealth,
                beforeStress = beforeStress,
                afterStress = crop != null
                    ? crop.Stress
                    : beforeStress,
                beforeDrainage = beforeDrainage,
                afterDrainage = crop != null
                    ? crop.Drainage
                    : beforeDrainage,
                beforeFertility = beforeFertility,
                afterFertility = crop != null
                    ? crop.Fertility
                    : beforeFertility,
                actionSucceeded = applied,
                recommendedForEvent = score > 0f,
                effectivenessScore = score,
                details = details
            });
        }

        private static void MakeCollidersPassThrough(GameObject go)
        {
            if (go == null)
                return;

            Collider[] colliders =
                go.GetComponentsInChildren<Collider>(true);

            foreach (Collider colliderComponent in colliders)
            {
                if (colliderComponent == null)
                    continue;

                colliderComponent.isTrigger = true;
            }
        }

        private bool IsWithinReach(Vector3 position)
        {
            return farmingSystem == null || farmingSystem.IsWithinReach(position);
        }

        private void Show(string message)
        {
            if (toast != null)
            {
                toast.Show(message);
            }
            else
            {
                Debug.Log("[ClimateMaintenance] " + message);
            }
        }
    }
}
