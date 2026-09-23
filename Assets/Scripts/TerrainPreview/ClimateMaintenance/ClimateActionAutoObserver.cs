using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AgriDabao3D
{
    /// <summary>
    /// Observes existing game systems that do not directly call ClimateEventTracker.
    /// This lets the evaluation retain planting, crop removal, digging, inventory,
    /// pest-trap, fruit-bag, and drainage actions performed during an active event.
    /// </summary>
    public class ClimateActionAutoObserver : MonoBehaviour
    {
        private sealed class CropObservation
        {
            public string cropId;
            public string cropType;
            public Vector3 position;
        }

        private readonly HashSet<int> seenAphidTraps = new HashSet<int>();
        private readonly HashSet<int> seenAreaTraps = new HashSet<int>();
        private readonly HashSet<int> seenDigSpots = new HashSet<int>();
        private readonly Dictionary<int, bool> fruitBagStates = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> drainageStates = new Dictionary<int, bool>();
        private readonly Dictionary<int, CropObservation> observedCrops = new Dictionary<int, CropObservation>();
        private readonly Dictionary<InventoryItemType, int> inventoryCounts = new Dictionary<InventoryItemType, int>();

        private bool wasTracking;
        private float nextScan;
        private int observedMoney;

        private void Update()
        {
            ClimateEventTracker tracker = ClimateEventTracker.Instance;
            bool tracking = tracker != null && tracker.IsTrackingEvent;
            if (tracking && !wasTracking) SnapshotExisting();
            wasTracking = tracking;
            if (!tracking || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 0.5f;
            Scan(tracker);
        }

        private void SnapshotExisting()
        {
            seenAphidTraps.Clear();
            seenAreaTraps.Clear();
            seenDigSpots.Clear();
            fruitBagStates.Clear();
            drainageStates.Clear();
            observedCrops.Clear();
            inventoryCounts.Clear();

            foreach (AphidTrapInstance trap in Object.FindObjectsByType<AphidTrapInstance>(FindObjectsSortMode.None))
                if (trap != null) seenAphidTraps.Add(trap.GetInstanceID());
            foreach (AreaMitigationTrapInstance trap in Object.FindObjectsByType<AreaMitigationTrapInstance>(FindObjectsSortMode.None))
                if (trap != null) seenAreaTraps.Add(trap.GetInstanceID());
            foreach (DigSpot spot in Object.FindObjectsByType<DigSpot>(FindObjectsSortMode.None))
                if (spot != null) seenDigSpots.Add(spot.GetInstanceID());
            foreach (CropProtectionState state in Object.FindObjectsByType<CropProtectionState>(FindObjectsSortMode.None))
            {
                if (state == null) continue;
                fruitBagStates[state.GetInstanceID()] = state.hasFruitBag;
                drainageStates[state.GetInstanceID()] = state.hasDrainageImprovement;
            }
            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
                observedCrops[crop.Root.GetInstanceID()] = Observe(crop);

            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory != null)
            {
                foreach (InventoryItemType item in Enum.GetValues(typeof(InventoryItemType)))
                {
                    if (item == InventoryItemType.None) continue;
                    inventoryCounts[item] = inventory.GetCount(item);
                }
                observedMoney = inventory.money;
            }
        }

        private void Scan(ClimateEventTracker tracker)
        {
            ScanPestAndProtectionActions(tracker);
            ScanDigging(tracker);
            ScanCropLifecycle(tracker);
            ScanInventoryAndMoney(tracker);
        }

        private void ScanPestAndProtectionActions(ClimateEventTracker tracker)
        {
            foreach (AphidTrapInstance trap in Object.FindObjectsByType<AphidTrapInstance>(FindObjectsSortMode.None))
            {
                if (trap == null || !seenAphidTraps.Add(trap.GetInstanceID())) continue;
                tracker.RecordAction(SimpleWorldAction(
                    "PlaceAphidTrap", "AphidTrap", trap.transform.position, -1f,
                    "A pest trap was placed during the climate event. It was recorded even though it is not a direct climate mitigation."));
            }
            foreach (AreaMitigationTrapInstance trap in Object.FindObjectsByType<AreaMitigationTrapInstance>(FindObjectsSortMode.None))
            {
                if (trap == null || !seenAreaTraps.Add(trap.GetInstanceID())) continue;
                string item = trap.mitigation == PestDiseaseMitigation.TermiteBait
                    ? "TermiteBaitStation"
                    : "PheromoneTrap";
                tracker.RecordAction(SimpleWorldAction(
                    "PlacePestMitigationTrap", item, trap.transform.position, -1f,
                    "A pest-control object was placed during the climate event and retained in the action history."));
            }
            foreach (CropProtectionState state in Object.FindObjectsByType<CropProtectionState>(FindObjectsSortMode.None))
            {
                if (state == null || !CropRuntimeAdapter.TryCreate(state.gameObject, out CropRuntimeAdapter crop)) continue;
                int id = state.GetInstanceID();
                bool previousFruitBag = fruitBagStates.TryGetValue(id, out bool f) && f;
                bool previousDrainage = drainageStates.TryGetValue(id, out bool d) && d;
                if (state.hasFruitBag && !previousFruitBag)
                    tracker.RecordAction(SimpleCropAction(
                        "InstallFruitBag", "FruitBag", crop, 1f,
                        "Fruit bag protection was installed."));
                if (state.hasDrainageImprovement && !previousDrainage)
                {
                    float score = tracker.ActiveEventType == WeatherEventType.Typhoon ? 8f : -1f;
                    tracker.RecordAction(SimpleCropAction(
                        "InstallDrainageKit", "DrainageKit", crop, score,
                        "Crop drainage was improved."));
                }
                fruitBagStates[id] = state.hasFruitBag;
                drainageStates[id] = state.hasDrainageImprovement;
            }
        }

        private void ScanDigging(ClimateEventTracker tracker)
        {
            foreach (DigSpot spot in Object.FindObjectsByType<DigSpot>(FindObjectsSortMode.None))
            {
                if (spot == null || !seenDigSpots.Add(spot.GetInstanceID())) continue;
                tracker.RecordAction(SimpleWorldAction(
                    "DigPlantingSpot", "Shovel", spot.transform.position, 0f,
                    "A planting spot was dug during the climate event."));
            }
        }

        private void ScanCropLifecycle(ClimateEventTracker tracker)
        {
            Dictionary<int, CropObservation> current = new Dictionary<int, CropObservation>();
            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                int key = crop.Root.GetInstanceID();
                CropObservation observation = Observe(crop);
                current[key] = observation;
                if (observedCrops.ContainsKey(key)) continue;
                tracker.RecordAction(new ClimateActionRecord
                {
                    actionType = "PlantCrop",
                    itemType = crop.PlantingMaterial,
                    cropType = observation.cropType,
                    cropId = observation.cropId,
                    gameDay = CurrentGameDay(),
                    worldPosition = new SerializableVector3(observation.position),
                    actionSucceeded = true,
                    recommendedForEvent = false,
                    effectivenessScore = -1f,
                    details = "A new crop was planted while the climate event was active."
                });
            }

            foreach (KeyValuePair<int, CropObservation> previous in observedCrops)
            {
                if (current.ContainsKey(previous.Key)) continue;
                CropObservation removed = previous.Value;
                tracker.RecordAction(new ClimateActionRecord
                {
                    actionType = "RemoveCrop",
                    cropType = removed.cropType,
                    cropId = removed.cropId,
                    gameDay = CurrentGameDay(),
                    worldPosition = new SerializableVector3(removed.position),
                    actionSucceeded = true,
                    recommendedForEvent = false,
                    effectivenessScore = 0f,
                    details = "A crop was removed or harvested out of the world during the climate event."
                });
            }

            observedCrops.Clear();
            foreach (KeyValuePair<int, CropObservation> pair in current)
                observedCrops[pair.Key] = pair.Value;
        }

        private void ScanInventoryAndMoney(ClimateEventTracker tracker)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null) return;

            foreach (InventoryItemType item in Enum.GetValues(typeof(InventoryItemType)))
            {
                if (item == InventoryItemType.None) continue;
                int current = inventory.GetCount(item);
                int previous = inventoryCounts.TryGetValue(item, out int oldValue) ? oldValue : current;
                int delta = current - previous;
                if (delta != 0)
                {
                    tracker.RecordAction(new ClimateActionRecord
                    {
                        actionType = delta > 0 ? "InventoryItemGained" : "InventoryItemUsed",
                        itemType = item.ToString(),
                        gameDay = CurrentGameDay(),
                        actionSucceeded = true,
                        recommendedForEvent = false,
                        effectivenessScore = 0f,
                        details = item + " inventory changed by " + delta + " during the climate event."
                    });
                }
                inventoryCounts[item] = current;
            }

            int moneyDelta = inventory.money - observedMoney;
            if (moneyDelta != 0)
            {
                tracker.RecordAction(new ClimateActionRecord
                {
                    actionType = moneyDelta > 0 ? "MoneyGained" : "MoneySpent",
                    itemType = "Money",
                    gameDay = CurrentGameDay(),
                    actionSucceeded = true,
                    recommendedForEvent = false,
                    effectivenessScore = 0f,
                    details = "Player money changed by P" + moneyDelta + " during the climate event."
                });
                observedMoney = inventory.money;
            }
        }

        private static CropObservation Observe(CropRuntimeAdapter crop)
        {
            return new CropObservation
            {
                cropId = crop.CropId,
                cropType = crop.CropDisplayName,
                position = crop.Transform.position
            };
        }

        private static float CurrentGameDay()
        {
            return GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : 0f;
        }

        private static ClimateActionRecord SimpleWorldAction(
            string action, string item, Vector3 position, float score, string details)
        {
            return new ClimateActionRecord
            {
                actionType = action,
                itemType = item,
                gameDay = CurrentGameDay(),
                worldPosition = new SerializableVector3(position),
                actionSucceeded = true,
                recommendedForEvent = score > 0f,
                effectivenessScore = score,
                details = details
            };
        }

        private static ClimateActionRecord SimpleCropAction(
            string action, string item, CropRuntimeAdapter crop, float score, string details)
        {
            return new ClimateActionRecord
            {
                actionType = action,
                itemType = item,
                cropId = crop.CropId,
                cropType = crop.CropDisplayName,
                gameDay = CurrentGameDay(),
                worldPosition = new SerializableVector3(crop.Transform.position),
                beforeMoisture = crop.Moisture,
                afterMoisture = crop.Moisture,
                beforeHealth = crop.Health,
                afterHealth = crop.Health,
                beforeStress = crop.Stress,
                afterStress = crop.Stress,
                beforeDrainage = crop.Drainage,
                afterDrainage = crop.Drainage,
                beforeFertility = crop.Fertility,
                afterFertility = crop.Fertility,
                actionSucceeded = true,
                recommendedForEvent = score > 0f,
                effectivenessScore = score,
                details = details
            };
        }
    }
}
