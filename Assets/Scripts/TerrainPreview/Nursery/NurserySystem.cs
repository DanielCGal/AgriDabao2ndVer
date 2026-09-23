using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AgriDabao3D
{
    public enum SeedlingBagStatus
    {
        /// <summary>No soil in the bag yet.</summary>
        Empty,

        /// <summary>Filled with soil, waiting for something to be sown.</summary>
        Filled,

        /// <summary>A seed germinating in the seed tray, before it is pricked into this bag.</summary>
        Germinating,

        /// <summary>Germinated and waiting for the player to prick it into the bag.</summary>
        NeedsPricking,

        /// <summary>Growing (or hardening) toward transplant size.</summary>
        Growing,

        /// <summary>Ready to transplant.</summary>
        Ready
    }

    /// <summary>One seedling bag on the Seedling Tent's racks.</summary>
    [Serializable]
    public class SeedlingBag
    {
        public int slot;
        public bool filled;
        public string material;
        public float sownGameDay;
        public float prickedGameDay = -1f;

        public bool IsSown => filled && !string.IsNullOrEmpty(material);

        public void Clear()
        {
            filled = false;
            material = null;
            sownGameDay = 0f;
            prickedGameDay = -1f;
        }
    }

    /// <summary>
    /// The farm's Seedling Tent: eight seedling bags on two racks, where nursery
    /// materials are raised before they go to the field.
    ///
    /// A bag is filled with soil, then sown. Durian and pomelo seeds germinate in
    /// a seed tray first and wait for the player to prick them into their bag,
    /// as the user's notes describe. When a seedling is ready the player picks it
    /// in the tent panel and walks it to prepared ground: the next tap on a
    /// matching hole or bed transplants it (see FarmingInteractionSystem).
    ///
    /// Time is the game clock's, measured from the day each bag was sown, so days
    /// skipped from the objectives book or the developer tools count too.
    ///
    /// Created at runtime by <see cref="NurseryRuntimeBootstrap"/>, like the other
    /// farm systems, so the scene needs no wiring beyond the prefabs on the
    /// farming system.
    /// </summary>
    public class NurserySystem : MonoBehaviour
    {
        public const int BagCount = 8;

        public static NurserySystem Instance { get; private set; }

        /// <summary>Raised whenever a bag, the transplant state or the tent changes.</summary>
        public event Action Changed;

        private readonly SeedlingBag[] bags = new SeedlingBag[BagCount];
        private SeedlingTentInstance tent;
        private bool tentHandled;
        private int transplantSlot = -1;
        private float nextStatusCheck;
        private SeedlingBagStatus[] lastStatus = new SeedlingBagStatus[BagCount];

        public SeedlingTentInstance Tent => tent;

        public bool IsTransplanting =>
            transplantSlot >= 0 && GetStatus(transplantSlot) == SeedlingBagStatus.Ready;

        public int TransplantSlot => IsTransplanting ? transplantSlot : -1;

        private static float Now =>
            GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            for (int i = 0; i < BagCount; i++)
                bags[i] = new SeedlingBag { slot = i };
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            // A new farm gets its tent once the world exists. A loaded farm gets
            // it from RestoreSaveData, which runs before the world is marked ready.
            if (!tentHandled && !FarmLoadContext.IsRestoring &&
                FarmPersistenceManager.Instance != null &&
                FarmPersistenceManager.Instance.IsWorldReady)
            {
                tentHandled = true;
                if (tent == null)
                    PlaceTentNearPlayer();
            }

            // Growth is continuous, but the tent only needs redrawing when a bag
            // moves from one state to the next, or its seedling visibly grows.
            if (Time.unscaledTime < nextStatusCheck)
                return;

            nextStatusCheck = Time.unscaledTime + 1f;

            bool statusChanged = false;
            for (int i = 0; i < BagCount; i++)
            {
                SeedlingBagStatus status = GetStatus(i);
                if (status != lastStatus[i])
                {
                    lastStatus[i] = status;
                    statusChanged = true;
                }
            }

            if (transplantSlot >= 0 && !IsTransplanting)
            {
                transplantSlot = -1;
                statusChanged = true;
            }

            if (statusChanged)
                NotifyChanged();
            else if (tent != null)
                tent.RefreshGrowth(this);
        }

        // ------------------------------------------------------------- reading

        public SeedlingBag GetBag(int slot)
        {
            return IsValidSlot(slot) ? bags[slot] : null;
        }

        public static bool IsValidSlot(int slot)
        {
            return slot >= 0 && slot < BagCount;
        }

        public bool TryGetMaterial(int slot, out PlantingMaterialInfo info)
        {
            info = null;
            SeedlingBag bag = GetBag(slot);
            return bag != null && bag.IsSown && PlantingMaterialCatalog.TryGet(bag.material, out info);
        }

        public SeedlingBagStatus GetStatus(int slot)
        {
            SeedlingBag bag = GetBag(slot);
            if (bag == null || !bag.filled)
                return SeedlingBagStatus.Empty;

            if (!TryGetMaterial(slot, out PlantingMaterialInfo info))
                return SeedlingBagStatus.Filled;

            float now = Now;

            if (info.PrickAfterDays > 0f && bag.prickedGameDay < 0f)
            {
                return now - bag.sownGameDay >= info.PrickAfterDays
                    ? SeedlingBagStatus.NeedsPricking
                    : SeedlingBagStatus.Germinating;
            }

            return now >= ReadyDay(bag, info) ? SeedlingBagStatus.Ready : SeedlingBagStatus.Growing;
        }

        private static float ReadyDay(SeedlingBag bag, PlantingMaterialInfo info)
        {
            return info.PrickAfterDays > 0f
                ? bag.prickedGameDay + Mathf.Max(0f, info.NurseryDays - info.PrickAfterDays)
                : bag.sownGameDay + info.NurseryDays;
        }

        /// <summary>Game days until the bag's next step: germination, or ready to transplant.</summary>
        public float DaysUntilNextStep(int slot)
        {
            SeedlingBag bag = GetBag(slot);
            if (!TryGetMaterial(slot, out PlantingMaterialInfo info))
                return 0f;

            switch (GetStatus(slot))
            {
                case SeedlingBagStatus.Germinating:
                    return Mathf.Max(0f, bag.sownGameDay + info.PrickAfterDays - Now);
                case SeedlingBagStatus.Growing:
                    return Mathf.Max(0f, ReadyDay(bag, info) - Now);
                default:
                    return 0f;
            }
        }

        /// <summary>How far the seedling is toward transplant size, 0 to 1, for the models and the panel.</summary>
        public float GrowthProgress(int slot)
        {
            SeedlingBag bag = GetBag(slot);
            if (!TryGetMaterial(slot, out PlantingMaterialInfo info) || info.NurseryDays <= 0f)
                return 0f;

            SeedlingBagStatus status = GetStatus(slot);
            if (status == SeedlingBagStatus.Ready)
                return 1f;

            float grown;
            if (info.PrickAfterDays > 0f)
            {
                grown = bag.prickedGameDay < 0f
                    ? Mathf.Min(Now - bag.sownGameDay, info.PrickAfterDays)
                    : info.PrickAfterDays + (Now - bag.prickedGameDay);
            }
            else
            {
                grown = Now - bag.sownGameDay;
            }

            return Mathf.Clamp01(grown / info.NurseryDays);
        }

        /// <summary>Days a seedling has spent in its bag, counted from sowing.</summary>
        public float DaysInNursery(int slot)
        {
            SeedlingBag bag = GetBag(slot);
            return bag != null && bag.IsSown ? Mathf.Max(0f, Now - bag.sownGameDay) : 0f;
        }

        public int CountBags(SeedlingBagStatus status)
        {
            int count = 0;
            for (int i = 0; i < BagCount; i++)
            {
                if (GetStatus(i) == status)
                    count++;
            }
            return count;
        }

        /// <summary>Ready seedlings of a crop, for the daily tasks and the adviser.</summary>
        public int CountReady(FarmCropType? crop = null)
        {
            int count = 0;
            for (int i = 0; i < BagCount; i++)
            {
                if (GetStatus(i) != SeedlingBagStatus.Ready)
                    continue;
                if (crop.HasValue && (!TryGetMaterial(i, out PlantingMaterialInfo info) || info.Crop != crop.Value))
                    continue;
                count++;
            }
            return count;
        }

        // ------------------------------------------------------------- actions

        public bool FillBag(int slot, out string message)
        {
            SeedlingBag bag = GetBag(slot);
            if (bag == null)
            {
                message = "That bag does not exist.";
                return false;
            }

            if (bag.filled)
            {
                message = "This bag already has soil in it.";
                return false;
            }

            bag.filled = true;
            bag.material = null;
            bag.prickedGameDay = -1f;

            GameAudioManager.Instance.PlayFillBag();
            RecordNurseryAction("FillSeedlingBag", "SeedlingBag", null, TentPosition, null,
                "A seedling bag was filled with soil.");

            message = "Bag filled with loose, well-drained soil. It is ready to sow.";
            NotifyChanged();
            return true;
        }

        public bool Sow(int slot, InventoryItemType item, out string message)
        {
            SeedlingBag bag = GetBag(slot);
            if (bag == null)
            {
                message = "That bag does not exist.";
                return false;
            }

            if (!bag.filled)
            {
                message = "Fill the bag with soil first.";
                return false;
            }

            if (bag.IsSown)
            {
                message = "Something is already growing in this bag.";
                return false;
            }

            if (!PlantingMaterialCatalog.TryGet(item, out PlantingMaterialInfo info) || !info.SowInBag)
            {
                message = PlantingMaterialCatalog.NameOf(item) + " does not go in a seedling bag.";
                return false;
            }

            if (PlayerInventory.Instance == null || !PlayerInventory.Instance.ConsumeItem(item, 1))
            {
                message = "You do not have a " + info.Name + ".";
                return false;
            }

            bag.material = info.Item.ToString();
            bag.sownGameDay = Now;
            bag.prickedGameDay = -1f;

            GameAudioManager.Instance.PlayPlantSeed();
            RecordNurseryAction("SowSeedlingBag", bag.material, info.Crop.ToString(), TentPosition, null,
                info.Name + " was sown in a seedling bag.");

            message = SowMessage(info);
            NotifyChanged();
            return true;
        }

        private static string SowMessage(PlantingMaterialInfo info)
        {
            if (info.PrickAfterDays > 0f)
            {
                return info.Name + " sown in the seed tray. It germinates in about " +
                       PlantingMaterialCatalog.FormatDays(info.PrickAfterDays) +
                       "; come back then to prick it into this bag.";
            }

            bool plant = info.Item == InventoryItemType.BananaPlantlet ||
                         info.Item == InventoryItemType.MangoGraftedSeedling;

            return info.Name + (plant ? " set in the bag to harden. " : " sown and watered. ") +
                   "Ready to transplant in about " + PlantingMaterialCatalog.FormatDays(info.NurseryDays) + ".";
        }

        public bool PrickOut(int slot, out string message)
        {
            if (GetStatus(slot) != SeedlingBagStatus.NeedsPricking ||
                !TryGetMaterial(slot, out PlantingMaterialInfo info))
            {
                message = "There is nothing to prick out in this bag yet.";
                return false;
            }

            SeedlingBag bag = bags[slot];
            bag.prickedGameDay = Now;

            GameAudioManager.Instance.PlayTransplant();
            string seedling = PlantingMaterialCatalog.SeedlingName(info);
            RecordNurseryAction("PrickOutSeedling", bag.material, info.Crop.ToString(), TentPosition, null,
                seedling + " was pricked into its bag.");

            message = seedling + " pricked into its bag. Ready to transplant in about " +
                      PlantingMaterialCatalog.FormatDays(info.NurseryDays - info.PrickAfterDays) + ".";
            NotifyChanged();
            return true;
        }

        /// <summary>Throws out whatever is in a bag, soil included. The material is lost.</summary>
        public bool EmptyBag(int slot, out string message)
        {
            SeedlingBag bag = GetBag(slot);
            if (bag == null || !bag.filled)
            {
                message = "This bag is already empty.";
                return false;
            }

            if (slot == transplantSlot)
                transplantSlot = -1;

            bag.Clear();
            message = "The bag was emptied.";
            NotifyChanged();
            return true;
        }

        public bool BeginTransplant(int slot, out string message)
        {
            if (GetStatus(slot) != SeedlingBagStatus.Ready || !TryGetMaterial(slot, out PlantingMaterialInfo info))
            {
                message = "This seedling is not ready to transplant yet.";
                return false;
            }

            transplantSlot = slot;
            message = "Carrying the " + PlantingMaterialCatalog.SeedlingName(info) + ". Tap a prepared " +
                      PlantingMaterialCatalog.PlotName(info.Plot) +
                      (info.NeedsMulchedBed ? " (mulched)" : string.Empty) + " to transplant it.";
            NotifyChanged();
            return true;
        }

        public void CancelTransplant()
        {
            if (transplantSlot < 0)
                return;

            transplantSlot = -1;
            NotifyChanged();
        }

        /// <summary>What is being carried to the field, without taking it out of its bag yet.</summary>
        public bool TryGetTransplant(out PlantingMaterialInfo info, out float daysInNursery)
        {
            info = null;
            daysInNursery = 0f;

            if (!IsTransplanting || !TryGetMaterial(transplantSlot, out info))
                return false;

            daysInNursery = DaysInNursery(transplantSlot);
            return true;
        }

        /// <summary>
        /// Called once the seedling is in the ground. The soil goes with the
        /// seedling, so the bag is left empty and has to be refilled to use again.
        /// </summary>
        public void FinishTransplant()
        {
            if (!IsValidSlot(transplantSlot))
                return;

            bags[transplantSlot].Clear();
            transplantSlot = -1;
            NotifyChanged();
        }

        /// <summary>Developer tools: every sown bag skips straight to ready.</summary>
        public int DevMakeAllReady()
        {
            int changed = 0;
            float now = Now;

            for (int i = 0; i < BagCount; i++)
            {
                if (!TryGetMaterial(i, out PlantingMaterialInfo info))
                    continue;

                SeedlingBag bag = bags[i];
                bag.sownGameDay = Mathf.Min(bag.sownGameDay, now - info.NurseryDays - 0.01f);
                if (info.PrickAfterDays > 0f)
                    bag.prickedGameDay = now - Mathf.Max(0f, info.NurseryDays - info.PrickAfterDays) - 0.01f;
                changed++;
            }

            NotifyChanged();
            return changed;
        }

        private void NotifyChanged()
        {
            for (int i = 0; i < BagCount; i++)
                lastStatus[i] = GetStatus(i);

            if (tent != null)
                tent.Refresh(this);

            try
            {
                Changed?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // ---------------------------------------------------------------- tent

        private Vector3 TentPosition => tent != null ? tent.transform.position : Vector3.zero;

        private void PlaceTentNearPlayer()
        {
            FarmingInteractionSystem farming = Object.FindFirstObjectByType<FarmingInteractionSystem>();
            Terrain terrain = TemporaryTerrainGenerator.ResolveActiveTerrain();
            FirstPersonTerrainController player = Object.FindFirstObjectByType<FirstPersonTerrainController>();

            if (farming == null || farming.seedlingTentPrefab == null || terrain == null || player == null)
            {
                Debug.LogWarning("[Nursery] Seedling Tent not placed: prefab, terrain or player missing. " +
                                 "The tent panel still works from its HUD button.");
                return;
            }

            Transform playerTransform = player.transform;
            Vector3 forward = Flat(playerTransform.forward, Vector3.forward);
            Vector3 right = Flat(playerTransform.right, Vector3.right);

            // To the player's left, on the other side from the shipping bin, and
            // far enough out that the two never touch. Other spots are tried in
            // turn so a farm loaded from an older save does not put the tent on
            // top of its crops.
            Vector3[] offsets =
            {
                forward * 14f - right * 9f,
                forward * 16f + right * 14f,
                -forward * 14f - right * 10f,
                -forward * 14f + right * 10f,
                forward * 24f,
                -right * 20f,
                right * 22f,
                -forward * 22f
            };

            Vector3 chosen = playerTransform.position + offsets[0];
            foreach (Vector3 offset in offsets)
            {
                Vector3 candidate = playerTransform.position + offset;
                if (IsClearForTent(candidate))
                {
                    chosen = candidate;
                    break;
                }
            }

            chosen.y = terrain.SampleHeight(chosen) + terrain.transform.position.y;

            Vector3 toPlayer = playerTransform.position - chosen;
            toPlayer.y = 0f;
            Quaternion facing = toPlayer.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(toPlayer.normalized, Vector3.up)
                : Quaternion.identity;

            SpawnTent(farming, chosen, facing, terrain);
            Debug.Log("[Nursery] Seedling Tent placed at " + chosen + ".");
        }

        private static Vector3 Flat(Vector3 value, Vector3 fallback)
        {
            value.y = 0f;
            return value.sqrMagnitude > 0.001f ? value.normalized : fallback;
        }

        private static bool IsClearForTent(Vector3 position)
        {
            foreach (Collider collider in Physics.OverlapSphere(position, 8f, ~0, QueryTriggerInteraction.Collide))
            {
                if (collider == null || collider is TerrainCollider)
                    continue;

                if (collider.GetComponentInParent<DigSpot>() != null ||
                    collider.GetComponentInParent<ClimateMitigationWorldObject>() != null ||
                    collider.GetComponentInParent<ShippingBinSeller>() != null ||
                    collider.GetComponentInParent<AphidTrapInstance>() != null ||
                    collider.GetComponentInParent<AreaMitigationTrapInstance>() != null ||
                    CropRuntimeAdapter.TryFromCollider(collider, out _))
                {
                    return false;
                }
            }

            return true;
        }

        private void SpawnTent(FarmingInteractionSystem farming, Vector3 position, Quaternion rotation, Terrain terrain)
        {
            if (tent != null)
                Destroy(tent.gameObject);

            GameObject go = Instantiate(farming.seedlingTentPrefab, position, rotation);
            go.name = "SeedlingTent_Runtime";

            tent = go.GetComponent<SeedlingTentInstance>();
            if (tent == null)
                tent = go.AddComponent<SeedlingTentInstance>();

            tent.SeatOnTerrain(terrain);
            tent.Refresh(this);
        }

        // ------------------------------------------------------ saving/loading

        public NurserySaveDto CaptureSaveData()
        {
            NurserySaveDto save = new NurserySaveDto
            {
                tentPlaced = tent != null
            };

            if (tent != null)
            {
                save.tentPosition = new SerializableVector3(tent.transform.position);
                save.tentRotation = new SerializableQuaternion(tent.transform.rotation);
            }

            foreach (SeedlingBag bag in bags)
            {
                if (bag == null || !bag.filled)
                    continue;

                save.bags.Add(new SeedlingBagSaveDto
                {
                    slot = bag.slot,
                    filled = bag.filled,
                    material = bag.material,
                    sownGameDay = bag.sownGameDay,
                    prickedGameDay = bag.prickedGameDay
                });
            }

            return save;
        }

        public void RestoreSaveData(NurserySaveDto save)
        {
            transplantSlot = -1;
            for (int i = 0; i < BagCount; i++)
                bags[i].Clear();

            if (save != null && save.bags != null)
            {
                foreach (SeedlingBagSaveDto saved in save.bags)
                {
                    if (saved == null || !IsValidSlot(saved.slot))
                        continue;

                    SeedlingBag bag = bags[saved.slot];
                    bag.filled = saved.filled;

                    // Anything no longer sown in bags is dropped rather than left to
                    // sit in a bag that can never finish.
                    string material = PlantingMaterialCatalog.UpgradeLegacyName(saved.material);
                    bag.material = PlantingMaterialCatalog.TryGet(material, out PlantingMaterialInfo info) && info.SowInBag
                        ? info.Item.ToString()
                        : null;
                    bag.sownGameDay = saved.sownGameDay;
                    bag.prickedGameDay = bag.material != null ? saved.prickedGameDay : -1f;
                }
            }

            FarmingInteractionSystem farming = Object.FindFirstObjectByType<FarmingInteractionSystem>();
            Terrain terrain = TemporaryTerrainGenerator.ResolveActiveTerrain();

            if (save != null && save.tentPlaced && farming != null && farming.seedlingTentPrefab != null)
            {
                SpawnTent(farming, save.tentPosition.ToVector3(), save.tentRotation.ToQuaternion(), terrain);
                tentHandled = true;
            }
            else
            {
                // A farm saved before the Seedling Tent existed gets one now, near
                // wherever the player was standing.
                tentHandled = false;
            }

            NotifyChanged();
        }

        // ------------------------------------------------------------ records

        /// <summary>
        /// Reports a nursery action to the daily tasks, the adviser's task and the
        /// beginner guide. It deliberately skips the climate event tracker: sowing
        /// a bag is not a response to a typhoon or a drought, and counting it there
        /// would pad the event's evaluation with unrelated work.
        /// </summary>
        public static void RecordNurseryAction(
            string actionType,
            string itemType,
            string cropType,
            Vector3 position,
            string cropId,
            string details)
        {
            FarmTaskActionHub.Record(new ClimateActionRecord
            {
                actionType = actionType,
                itemType = itemType,
                cropType = cropType,
                cropId = cropId,
                gameDay = Now,
                worldPosition = new SerializableVector3(position),
                quantity = 1,
                actionSucceeded = true,
                effectivenessScore = 0f,
                details = details,
                recordOrigin = "Direct"
            });
        }
    }
}
