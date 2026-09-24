using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AgriDabao3D
{
    public class FarmTaskActionObserver : MonoBehaviour
    {
        public static FarmTaskActionObserver Instance { get; private set; }

        [Range(0.1f, 2f)]
        public float scanInterval = 0.25f;

        private sealed class CropObservation
        {
            public string cropId;
            public string cropType;
            public string plantingMaterial;
            public Vector3 position;
            public float moisture;
            public float health;
            public float stress;
            public float fertility;
            public float drainage;
            public int harvestAvailable;
            public bool hasMulch;
            public bool hasSupportStake;
            public bool hasTrellis;
            public bool hasRaisedBed;
            public float lastCompostGameDay;
            public float lastPrunedGameDay;
            public bool hasFruitBag;
            public bool hasDrainageImprovement;
            public Dictionary<string, float> conditions =
                new Dictionary<string, float>(
                    StringComparer.OrdinalIgnoreCase);
        }

        private sealed class TrapObservation
        {
            public string itemType;
            public float load;
            public float capacity;
        }

        private struct GroundObservation
        {
            public PreparedPlotKind kind;
            public bool mulched;
        }

        private readonly Dictionary<string, CropObservation> crops =
            new Dictionary<string, CropObservation>(
                StringComparer.Ordinal);
        private readonly Dictionary<int, GroundObservation> digSpots =
            new Dictionary<int, GroundObservation>();
        private readonly Dictionary<int, TrapObservation> traps =
            new Dictionary<int, TrapObservation>();
        private readonly HashSet<int> worldMitigations =
            new HashSet<int>();
        private readonly Dictionary<InventoryItemType, int> inventory =
            new Dictionary<InventoryItemType, int>();

        private int money;
        private float nextScan;
        private bool initialized;
        private string lastKnownTreatment = "ObservedMitigation";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DisableLegacyObservers();
        }

        private void Start()
        {
            DisableLegacyObservers();
            ResetBaseline();
        }

        private void Update()
        {
            DisableLegacyObservers();
            if (FarmLoadContext.IsRestoring ||
                Time.unscaledTime < nextScan)
            {
                return;
            }

            nextScan = Time.unscaledTime +
                       Mathf.Max(0.1f, scanInterval);

            if (!initialized)
            {
                ResetBaseline();
                return;
            }

            ScanCrops();
            ScanDigSpots();
            ScanTraps();
            ScanWorldMitigations();
            ScanInventoryAndSales();
            RefreshLastKnownTreatment();
        }

        public void ResetBaseline()
        {
            DisableLegacyObservers();
            FarmTaskActionHub.ClearRecent();
            crops.Clear();
            digSpots.Clear();
            traps.Clear();
            worldMitigations.Clear();
            inventory.Clear();

            foreach (CropRuntimeAdapter crop in
                     CropRuntimeAdapter.FindAll())
            {
                CropObservation observation = Observe(crop);
                if (observation != null)
                    crops[observation.cropId] = observation;
            }

            foreach (DigSpot spot in
                     Object.FindObjectsByType<DigSpot>(
                         FindObjectsSortMode.None))
            {
                if (spot != null)
                    digSpots[spot.GetInstanceID()] = ObserveGround(spot);
            }

            foreach (AphidTrapInstance trap in
                     Object.FindObjectsByType<AphidTrapInstance>(
                         FindObjectsSortMode.None))
            {
                if (trap == null)
                    continue;
                traps[trap.GetInstanceID()] =
                    new TrapObservation
                    {
                        itemType = "AphidTrap",
                        load = trap.deadAphidLoadPercent,
                        capacity = trap.capacityPercent
                    };
            }

            foreach (AreaMitigationTrapInstance trap in
                     Object.FindObjectsByType<
                         AreaMitigationTrapInstance>(
                         FindObjectsSortMode.None))
            {
                if (trap == null)
                    continue;
                traps[trap.GetInstanceID()] =
                    new TrapObservation
                    {
                        itemType =
                            trap.mitigation ==
                            PestDiseaseMitigation.TermiteBait
                                ? "TermiteBaitStation"
                                : "PheromoneTrap",
                        load = trap.capturedLoadPercent,
                        capacity = trap.capacityPercent
                    };
            }

            foreach (ClimateMitigationWorldObject item in
                     Object.FindObjectsByType<
                         ClimateMitigationWorldObject>(
                         FindObjectsSortMode.None))
            {
                if (item != null)
                    worldMitigations.Add(
                        item.GetInstanceID());
            }

            PlayerInventory playerInventory =
                PlayerInventory.Instance;
            if (playerInventory != null)
            {
                foreach (InventoryItemType item in
                         Enum.GetValues(
                             typeof(InventoryItemType)))
                {
                    if (item == InventoryItemType.None)
                        continue;
                    inventory[item] =
                        playerInventory.GetCount(item);
                }
                money = playerInventory.money;
            }

            RefreshLastKnownTreatment();
            initialized = true;
        }

        private void ScanCrops()
        {
            Dictionary<string, CropObservation> current =
                new Dictionary<string, CropObservation>(
                    StringComparer.Ordinal);

            foreach (CropRuntimeAdapter crop in
                     CropRuntimeAdapter.FindAll())
            {
                CropObservation now = Observe(crop);
                if (now == null)
                    continue;
                current[now.cropId] = now;

                if (!crops.TryGetValue(
                        now.cropId, out CropObservation before))
                {
                    Route(new ClimateActionRecord
                    {
                        actionType = "PlantCrop",
                        itemType = now.plantingMaterial,
                        cropType = now.cropType,
                        cropId = now.cropId,
                        gameDay = CurrentGameDay(),
                        worldPosition =
                            new SerializableVector3(now.position),
                        quantity = 1,
                        actionSucceeded = true,
                        effectivenessScore = 0f,
                        details = now.cropType +
                                  " was planted."
                    });
                    continue;
                }



                if (now.harvestAvailable <
                    before.harvestAvailable)
                {
                    int harvested = Mathf.Max(
                        1,
                        before.harvestAvailable -
                        now.harvestAvailable);
                    Route(new ClimateActionRecord
                    {
                        actionType = "HarvestCrop",
                        itemType = now.cropType,
                        cropType = now.cropType,
                        cropId = now.cropId,
                        gameDay = CurrentGameDay(),
                        worldPosition =
                            new SerializableVector3(now.position),
                        quantity = harvested,
                        actionSucceeded = true,
                        effectivenessScore = 0f,
                        details = harvested + " " +
                                  now.cropType +
                                  " harvest item(s) were removed " +
                                  "from the crop."
                    });
                }

                ScanMaintenanceChanges(before, now);
                ScanConditionChanges(before, now);
            }

            foreach (KeyValuePair<string, CropObservation> pair
                     in crops)
            {
                if (current.ContainsKey(pair.Key))
                    continue;

                CropObservation removed = pair.Value;
                KeyValuePair<string, float> severe =
                    removed.conditions
                        .OrderByDescending(value => value.Value)
                        .FirstOrDefault();
                string actionType =
                    severe.Value >= 0.1f
                        ? "RemoveInfectedPlant"
                        : "RemoveCrop";
                Route(new ClimateActionRecord
                {
                    actionType = actionType,
                    itemType = actionType == "RemoveInfectedPlant"
                        ? "RemoveInfectedPlant"
                        : "Machete",
                    taskSource = "Machete",
                    cropType = removed.cropType,
                    cropId = removed.cropId,
                    conditionType = severe.Key,
                    beforeSeverity = severe.Value,
                    afterSeverity = 0f,
                    gameDay = CurrentGameDay(),
                    worldPosition =
                        new SerializableVector3(
                            removed.position),
                    quantity = 1,
                    actionSucceeded = true,
                    effectivenessScore = 0f,
                    details = removed.cropType +
                              " was removed from the farm."
                });
            }

            crops.Clear();
            foreach (KeyValuePair<string, CropObservation> pair
                     in current)
            {
                crops[pair.Key] = pair.Value;
            }
        }

        private void ScanMaintenanceChanges(
            CropObservation before,
            CropObservation now)
        {
            if (now.hasMulch && !before.hasMulch)
                RecordMaintenance(now, "Mulch",
                    "MulchBag");
            if (now.hasSupportStake &&
                !before.hasSupportStake)
            {
                RecordMaintenance(now, "SupportStake",
                    "SupportStakeKit");
            }
            if (now.hasTrellis && !before.hasTrellis)
                RecordMaintenance(now, "Trellis",
                    "TrellisKit");
            if (now.hasRaisedBed && !before.hasRaisedBed)
                RecordMaintenance(now, "RaisedBed",
                    "RaisedBedKit");

            if (now.lastCompostGameDay >
                before.lastCompostGameDay + 0.001f)
            {
                RecordMaintenance(now, "OrganicCompost",
                    "OrganicCompostBag");
            }
            if (now.lastPrunedGameDay >
                before.lastPrunedGameDay + 0.001f)
            {
                RecordMaintenance(now, "Prune",
                    "PruningShears");
            }

            if (now.hasFruitBag && !before.hasFruitBag)
            {
                Route(CropAction(
                    "InstallFruitBag",
                    "FruitBag",
                    before, now,
                    "Fruit bag protection was installed."));
            }

            if (now.hasDrainageImprovement &&
                !before.hasDrainageImprovement)
            {
                Route(CropAction(
                    "InstallDrainageKit",
                    "DrainageKit",
                    before, now,
                    "Crop drainage protection was installed."));
            }
        }

        private void RecordMaintenance(
            CropObservation crop,
            string action,
            string item)
        {
            Route(new ClimateActionRecord
            {
                actionType = "ApplyCropMaintenance",
                itemType = action,
                taskSource = item,
                cropType = crop.cropType,
                cropId = crop.cropId,
                gameDay = CurrentGameDay(),
                worldPosition =
                    new SerializableVector3(crop.position),
                afterMoisture = crop.moisture,
                afterHealth = crop.health,
                afterStress = crop.stress,
                afterDrainage = crop.drainage,
                afterFertility = crop.fertility,
                quantity = 1,
                actionSucceeded = true,
                effectivenessScore = 0f,
                details = action + " was applied using " +
                          item + "."
            });
        }

        private void ScanConditionChanges(
            CropObservation before,
            CropObservation now)
        {
            HashSet<string> names =
                new HashSet<string>(
                    before.conditions.Keys,
                    StringComparer.OrdinalIgnoreCase);
            names.UnionWith(now.conditions.Keys);

            foreach (string condition in names)
            {
                float oldSeverity =
                    before.conditions.TryGetValue(
                        condition, out float oldValue)
                        ? oldValue
                        : 0f;
                float newSeverity =
                    now.conditions.TryGetValue(
                        condition, out float newValue)
                        ? newValue
                        : 0f;

                if (oldSeverity - newSeverity < 0.25f)
                    continue;

                string item = InferCurrentTreatment();
                Route(new ClimateActionRecord
                {
                    actionType = "MitigatePestDisease",
                    itemType = item,
                    cropType = now.cropType,
                    cropId = now.cropId,
                    conditionType = condition,
                    beforeSeverity = oldSeverity,
                    afterSeverity = newSeverity,
                    gameDay = CurrentGameDay(),
                    worldPosition =
                        new SerializableVector3(now.position),
                    beforeMoisture = before.moisture,
                    afterMoisture = now.moisture,
                    beforeHealth = before.health,
                    afterHealth = now.health,
                    beforeStress = before.stress,
                    afterStress = now.stress,
                    quantity = 1,
                    actionSucceeded = true,
                    effectivenessScore =
                        oldSeverity - newSeverity,
                    details = condition +
                              " severity decreased by " +
                              (oldSeverity - newSeverity)
                              .ToString("F1") +
                              " percentage points."
                });
            }
        }

        private void ScanDigSpots()
        {
            Dictionary<int, GroundObservation> current =
                new Dictionary<int, GroundObservation>();

            foreach (DigSpot spot in
                     Object.FindObjectsByType<DigSpot>(
                         FindObjectsSortMode.None))
            {
                if (spot == null)
                    continue;

                int id = spot.GetInstanceID();
                GroundObservation now = ObserveGround(spot);
                current[id] = now;

                bool known = digSpots.TryGetValue(id, out GroundObservation before);

                if (!known && now.kind == PreparedPlotKind.Tilled)
                {
                    RouteGround("TillGround", "Shovel", spot,
                        "New ground was tilled.");
                }
                else if (spot.IsPrepared &&
                         (!known || before.kind != now.kind))
                {
                    RouteGround("DigPlantingSpot", now.kind.ToString(), spot,
                        "A " + spot.DisplayName + " was prepared for planting.");
                }

                if (now.mulched && (!known || !before.mulched))
                {
                    RouteGround("MulchBed", "MulchBag", spot,
                        "A raised bed was covered with mulch.");
                }
            }

            digSpots.Clear();
            foreach (KeyValuePair<int, GroundObservation> pair in current)
                digSpots[pair.Key] = pair.Value;
        }

        private static GroundObservation ObserveGround(DigSpot spot)
        {
            return new GroundObservation
            {
                kind = spot.plotKind,
                mulched = spot.mulched
            };
        }

        private static void RouteGround(
            string action,
            string item,
            DigSpot spot,
            string details)
        {
            Route(new ClimateActionRecord
            {
                actionType = action,
                itemType = item,
                taskSource = "Shovel",
                gameDay = CurrentGameDay(),
                worldPosition =
                    new SerializableVector3(
                        spot.transform.position),
                quantity = 1,
                actionSucceeded = true,
                effectivenessScore = 0f,
                details = details
            });
        }

        private void ScanTraps()
        {
            Dictionary<int, TrapObservation> current =
                new Dictionary<int, TrapObservation>();

            foreach (AphidTrapInstance trap in
                     Object.FindObjectsByType<AphidTrapInstance>(
                         FindObjectsSortMode.None))
            {
                if (trap == null)
                    continue;
                int id = trap.GetInstanceID();
                TrapObservation now = new TrapObservation
                {
                    itemType = "AphidTrap",
                    load = trap.deadAphidLoadPercent,
                    capacity = trap.capacityPercent
                };
                current[id] = now;
                HandleTrapObservation(
                    id, now, trap.transform.position,
                    "PlaceAphidTrap");
            }

            foreach (AreaMitigationTrapInstance trap in
                     Object.FindObjectsByType<
                         AreaMitigationTrapInstance>(
                         FindObjectsSortMode.None))
            {
                if (trap == null)
                    continue;
                int id = trap.GetInstanceID();
                string item =
                    trap.mitigation ==
                    PestDiseaseMitigation.TermiteBait
                        ? "TermiteBaitStation"
                        : "PheromoneTrap";
                TrapObservation now = new TrapObservation
                {
                    itemType = item,
                    load = trap.capturedLoadPercent,
                    capacity = trap.capacityPercent
                };
                current[id] = now;
                HandleTrapObservation(
                    id, now, trap.transform.position,
                    "PlacePestMitigationTrap");
            }

            traps.Clear();
            foreach (KeyValuePair<int, TrapObservation> pair
                     in current)
            {
                traps[pair.Key] = pair.Value;
            }
        }

        private void HandleTrapObservation(
            int id,
            TrapObservation now,
            Vector3 position,
            string placementAction)
        {
            if (!traps.TryGetValue(
                    id, out TrapObservation before))
            {
                Route(new ClimateActionRecord
                {
                    actionType = placementAction,
                    itemType = now.itemType,
                    gameDay = CurrentGameDay(),
                    worldPosition =
                        new SerializableVector3(position),
                    quantity = 1,
                    actionSucceeded = true,
                    effectivenessScore = 0f,
                    details = now.itemType +
                              " was placed."
                });
                return;
            }

            bool wasFull =
                before.capacity > 0f &&
                before.load >= before.capacity - 0.01f;
            bool cleaned =
                before.load - now.load >=
                Mathf.Max(5f, before.capacity * 0.50f);
            if (cleaned && (wasFull || now.load <= 0.01f))
            {
                Route(new ClimateActionRecord
                {
                    actionType = "CleanTrap",
                    itemType = now.itemType,
                    gameDay = CurrentGameDay(),
                    worldPosition =
                        new SerializableVector3(position),
                    beforeSeverity = before.load,
                    afterSeverity = now.load,
                    quantity = 1,
                    actionSucceeded = true,
                    effectivenessScore = 0f,
                    details = now.itemType +
                              " was cleaned."
                });
            }
        }

        private void ScanWorldMitigations()
        {
            HashSet<int> current = new HashSet<int>();
            foreach (ClimateMitigationWorldObject item in
                     Object.FindObjectsByType<
                         ClimateMitigationWorldObject>(
                         FindObjectsSortMode.None))
            {
                if (item == null)
                    continue;
                int id = item.GetInstanceID();
                current.Add(id);
                if (worldMitigations.Contains(id))
                    continue;

                Route(new ClimateActionRecord
                {
                    actionType = "PlaceClimateMitigation",
                    itemType =
                        item.mitigationType.ToString(),
                    gameDay = CurrentGameDay(),
                    worldPosition =
                        new SerializableVector3(
                            item.transform.position),
                    quantity = 1,
                    actionSucceeded = true,
                    effectivenessScore = 0f,
                    details = item.displayName +
                              " was installed."
                });
            }

            worldMitigations.Clear();
            foreach (int id in current)
                worldMitigations.Add(id);
        }

        private void ScanInventoryAndSales()
        {
            PlayerInventory playerInventory =
                PlayerInventory.Instance;
            if (playerInventory == null)
                return;

            Dictionary<InventoryItemType, int> current =
                new Dictionary<InventoryItemType, int>();
            List<KeyValuePair<InventoryItemType, int>> decreases =
                new List<KeyValuePair<InventoryItemType, int>>();
            List<KeyValuePair<InventoryItemType, int>> increases =
                new List<KeyValuePair<InventoryItemType, int>>();

            foreach (InventoryItemType item in
                     Enum.GetValues(typeof(InventoryItemType)))
            {
                if (item == InventoryItemType.None)
                    continue;
                int value = playerInventory.GetCount(item);
                current[item] = value;
                int previous =
                    inventory.TryGetValue(item, out int old)
                        ? old
                        : value;
                int delta = value - previous;
                if (delta > 0)
                    increases.Add(
                        new KeyValuePair<
                            InventoryItemType, int>(
                            item, delta));
                else if (delta < 0)
                    decreases.Add(
                        new KeyValuePair<
                            InventoryItemType, int>(
                            item, -delta));
            }

            int moneyDelta = playerInventory.money - money;

            foreach (KeyValuePair<InventoryItemType, int> gain
                     in increases)
            {
                if (FarmTaskContextBuilder.IsSellableCropItem(
                        gain.Key))
                {
                    Route(new ClimateActionRecord
                    {
                        actionType = "CollectHarvest",
                        itemType = gain.Key.ToString(),
                        cropType = gain.Key.ToString(),
                        gameDay = CurrentGameDay(),
                        quantity = gain.Value,
                        actionSucceeded = true,
                        effectivenessScore = 0f,
                        details = gain.Value + " " +
                                  gain.Key +
                                  " item(s) were collected."
                    });
                }
                else
                {
                    Route(new ClimateActionRecord
                    {
                        actionType = "InventoryItemGained",
                        itemType = gain.Key.ToString(),
                        gameDay = CurrentGameDay(),
                        quantity = gain.Value,
                        actionSucceeded = true,
                        effectivenessScore = 0f,
                        details = gain.Key +
                                  " inventory increased by " +
                                  gain.Value + "."
                    });
                }
            }

            List<KeyValuePair<InventoryItemType, int>>
                soldCropDecreases = decreases
                    .Where(value =>
                        FarmTaskContextBuilder
                            .IsSellableCropItem(value.Key))
                    .ToList();

            if (moneyDelta > 0 &&
                soldCropDecreases.Count > 0)
            {
                int remainingMoney = moneyDelta;
                for (int i = 0;
                     i < soldCropDecreases.Count;
                     i++)
                {
                    KeyValuePair<InventoryItemType, int> sold =
                        soldCropDecreases[i];
                    int allocated =
                        i == soldCropDecreases.Count - 1
                            ? remainingMoney
                            : Mathf.RoundToInt(
                                moneyDelta *
                                (sold.Value /
                                 (float)soldCropDecreases.Sum(
                                     value => value.Value)));
                    remainingMoney -= allocated;
                    Route(new ClimateActionRecord
                    {
                        actionType = "SellCrop",
                        itemType = sold.Key.ToString(),
                        cropType = sold.Key.ToString(),
                        gameDay = CurrentGameDay(),
                        quantity = sold.Value,
                        moneyDelta = Mathf.Max(0, allocated),
                        actionSucceeded = true,
                        effectivenessScore = 0f,
                        details = sold.Value + " " +
                                  sold.Key +
                                  " item(s) were sold for P" +
                                  Mathf.Max(0, allocated) + "."
                    });
                }
            }

            inventory.Clear();
            foreach (KeyValuePair<InventoryItemType, int> pair
                     in current)
            {
                inventory[pair.Key] = pair.Value;
            }
            money = playerInventory.money;
        }

        private static CropObservation Observe(
            CropRuntimeAdapter crop)
        {
            if (crop == null || crop.Root == null)
                return null;

            CropObservation result =
                new CropObservation
                {
                    cropId = crop.CropId,
                    cropType = crop.CropDisplayName,
                    plantingMaterial = crop.PlantingMaterial,
                    position = crop.Transform.position,
                    moisture = crop.Moisture,
                    health = crop.Health,
                    stress = crop.Stress,
                    fertility = crop.Fertility,
                    drainage = crop.Drainage,
                    harvestAvailable =
                        FarmTaskContextBuilder
                            .GetAvailableHarvest(crop)
                };

            CropClimateMaintenanceState maintenance =
                crop.Root.GetComponent<
                    CropClimateMaintenanceState>();
            if (maintenance != null)
            {
                result.hasMulch =
                    maintenance.hasMulch;
                result.hasSupportStake =
                    maintenance.hasSupportStake;
                result.hasTrellis =
                    maintenance.hasTrellis;
                result.hasRaisedBed =
                    maintenance.hasRaisedBed;
                result.lastCompostGameDay =
                    maintenance.lastCompostGameDay;
                result.lastPrunedGameDay =
                    maintenance.lastPrunedGameDay;
            }

            CropProtectionState protection =
                crop.Root.GetComponent<CropProtectionState>();
            if (protection != null)
            {
                result.hasFruitBag =
                    protection.hasFruitBag;
                result.hasDrainageImprovement =
                    protection.hasDrainageImprovement;
            }

            PestDiseaseAffectedCrop affected =
                crop.Root.GetComponent<
                    PestDiseaseAffectedCrop>();
            if (affected != null &&
                affected.activeConditions != null)
            {
                foreach (ActivePestDisease condition in
                         affected.activeConditions)
                {
                    if (condition == null)
                        continue;
                    result.conditions[
                        condition.type.ToString()] =
                        condition.severity;
                }
            }

            return result;
        }

        private static ClimateActionRecord CropAction(
            string action,
            string item,
            CropObservation before,
            CropObservation after,
            string details)
        {
            return new ClimateActionRecord
            {
                actionType = action,
                itemType = item,
                cropType = after.cropType,
                cropId = after.cropId,
                gameDay = CurrentGameDay(),
                worldPosition =
                    new SerializableVector3(after.position),
                beforeMoisture = before.moisture,
                afterMoisture = after.moisture,
                beforeHealth = before.health,
                afterHealth = after.health,
                beforeStress = before.stress,
                afterStress = after.stress,
                beforeDrainage = before.drainage,
                afterDrainage = after.drainage,
                beforeFertility = before.fertility,
                afterFertility = after.fertility,
                quantity = 1,
                actionSucceeded = true,
                effectivenessScore = 0f,
                details = details
            };
        }

        private string InferCurrentTreatment()
        {
            string current = ReadSelectedTreatment();
            if (!string.IsNullOrWhiteSpace(current))
            {
                lastKnownTreatment = current;
                return current;
            }

            if (!string.IsNullOrWhiteSpace(lastKnownTreatment))
                return lastKnownTreatment;

            PlayerInventory playerInventory = PlayerInventory.Instance;
            return playerInventory != null &&
                   playerInventory.selectedItem != InventoryItemType.None
                ? playerInventory.selectedItem.ToString()
                : "ObservedMitigation";
        }

        private void RefreshLastKnownTreatment()
        {
            string current = ReadSelectedTreatment();
            if (!string.IsNullOrWhiteSpace(current))
                lastKnownTreatment = current;
        }

        private static string ReadSelectedTreatment()
        {
            PlayerInventory playerInventory = PlayerInventory.Instance;
            if (playerInventory == null)
                return null;

            InventoryItemType selected = playerInventory.selectedItem;
            if (selected == InventoryItemType.Machete)
                return "Sanitation";
            if (selected == InventoryItemType.FruitBag)
                return "FruitBag";
            if (selected == InventoryItemType.DrainageKit)
                return "DrainageImprovement";
            if (selected != InventoryItemType.SprayerPump)
                return null;

            InventorySlotData slot = playerInventory.GetSelectedSlot();
            if (slot == null)
                return null;

            switch (slot.sprayerLiquid)
            {
                case SprayerLiquidType.NeemSoap:
                    return "NeemSoap";
                case SprayerLiquidType.BtBioInsecticide:
                    return "BtBioSpray";
                case SprayerLiquidType.CopperFungicide:
                    return "CopperFungicide";
                case SprayerLiquidType.Disinfectant:
                    return "Disinfectant";
                case SprayerLiquidType.Insecticide:
                    return "Insecticide";
                default:
                    return null;
            }
        }

        private static float CurrentGameDay()
        {
            return GameTimeSystem.Instance != null
                ? GameTimeSystem.Instance.TotalGameDays
                : 0f;
        }

        private static void Route(
            ClimateActionRecord record)
        {
            if (record == null)
                return;
            record.recordOrigin = "Observer";
            if (ClimateEventTracker.Instance != null)
                ClimateEventTracker.Instance.RecordObservedAction(record);
            else
                FarmTaskActionHub.Record(record);
        }

        private static void DisableLegacyObservers()
        {
            foreach (ClimateActionAutoObserver legacyObserver in
                     Object.FindObjectsByType<ClimateActionAutoObserver>(
                         FindObjectsSortMode.None))
            {
                if (legacyObserver != null && legacyObserver.enabled)
                    legacyObserver.enabled = false;
            }
        }
    }
}
