using System.Collections.Generic;
using UnityEngine;
namespace AgriDabao3D
{
    public class ClimateEventTracker : MonoBehaviour
    {
        [Header("AI Evaluation References")]
        public ClimateResilienceGeminiClient geminiClient;
        public ClimateEvaluationPopupBuilder popupBuilder;
        public static ClimateEventTracker Instance { get; private set; }
        [Header("Debug")]
        public bool logDebug = true;
        public bool IsTrackingEvent { get; private set; }
        public WeatherEventType ActiveEventType { get; private set; }
        public int ActiveDurationDays { get; private set; }
        public float EventStartGameDay { get; private set; }
        private readonly List<CropSnapshot> beforeSnapshots = new List<CropSnapshot>();
        private readonly List<ClimateActionRecord> actionRecords = new List<ClimateActionRecord>();
        private bool isSubmittingEvaluation;
        private void Awake()
        {
            EnsureReferences();
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        private void Update()
        {
            if (FarmLoadContext.IsRestoring) return;
            if (WeatherSystem.Instance == null || GameTimeSystem.Instance == null) return;
            WeatherEventType current = WeatherSystem.Instance.currentEvent;
            bool shouldTrack = current == WeatherEventType.Typhoon || current == WeatherEventType.ExtremeDrought;
            if (!IsTrackingEvent && shouldTrack)
                StartTracking(current, WeatherSystem.Instance.remainingEventDays);
            else if (IsTrackingEvent && !isSubmittingEvaluation && current != ActiveEventType)
                FinishTracking();
        }
        public void RecordWaterAction(string cropId, string cropType, float beforeMoisture, float afterMoisture)
        {
            if (string.IsNullOrWhiteSpace(cropId)) return;

            WeatherEventType eventType = IsTrackingEvent
                ? ActiveEventType
                : WeatherSystem.Instance != null
                    ? WeatherSystem.Instance.currentEvent
                    : WeatherEventType.Clear;

            float score = eventType == WeatherEventType.ExtremeDrought
                ? 8f
                : eventType == WeatherEventType.Typhoon
                    ? -5f
                    : 0f;

            string details = eventType == WeatherEventType.ExtremeDrought
                ? "Watering directly reduced drought stress."
                : eventType == WeatherEventType.Typhoon
                    ? "Watering during a typhoon may increase waterlogging risk."
                    : "The crop was watered during normal farm conditions.";

            RecordAction(new ClimateActionRecord
            {
                actionType = "WaterCrop",
                itemType = InventoryItemType.WateringCan.ToString(),
                cropType = cropType,
                cropId = cropId,
                gameDay = GameTimeSystem.Instance != null
                    ? GameTimeSystem.Instance.TotalGameDays
                    : 0f,
                beforeMoisture = beforeMoisture,
                afterMoisture = afterMoisture,
                quantity = 1,
                actionSucceeded = true,
                recommendedForEvent = score > 0f,
                effectivenessScore = score,
                details = details
            });
        }
        public void RecordAction(ClimateActionRecord record)
        {
            RecordInternal(record, "Direct");
        }

        public void RecordObservedAction(ClimateActionRecord record)
        {
            RecordInternal(record, "Observer");
        }

        private void RecordInternal(
            ClimateActionRecord record,
            string origin)
        {
            if (record == null) return;
            if (string.IsNullOrWhiteSpace(record.recordOrigin))
                record.recordOrigin = origin;

            int duplicateIndex =
                IsTrackingEvent && record.actionSucceeded
                    ? FindDuplicateEventRecord(record)
                    : -1;
            ClimateActionRecord previousDuplicate =
                duplicateIndex >= 0
                    ? actionRecords[duplicateIndex]
                    : null;

            if (previousDuplicate != null &&
                ShouldPreferIncomingRecord(previousDuplicate, record))
            {
                MergeMissingTreatmentDetails(record, previousDuplicate);
            }

            bool deferTaskRouting =
                ShouldDeferDirectTreatmentToObserver(record);
            if (!deferTaskRouting)
                FarmTaskActionHub.Record(record);

            if (!IsTrackingEvent || !record.actionSucceeded) return;

            if (duplicateIndex >= 0)
            {
                if (ShouldPreferIncomingRecord(previousDuplicate, record))
                {
                    actionRecords[duplicateIndex] = record;
                    if (logDebug)
                    {
                        Debug.Log(
                            "[ClimateTracker] Replaced low-detail duplicate with " +
                            "observer record | Action=" + record.actionType +
                            " | Crop=" + record.cropType +
                            " | Condition=" + record.conditionType);
                    }
                }
                return;
            }

            actionRecords.Add(record);
            if (logDebug)
            {
                Debug.Log("[ClimateTracker] Action recorded | Event=" + ActiveEventType +
                          " | Origin=" + record.recordOrigin +
                          " | Action=" + record.actionType +
                          " | Item=" + record.itemType +
                          " | Crop=" + record.cropType +
                          " | Score=" + record.effectivenessScore.ToString("F1") +
                          " | Success=" + record.actionSucceeded);
            }
        }

        private static bool ShouldDeferDirectTreatmentToObserver(
            ClimateActionRecord record)
        {
            if (record == null ||
                !string.Equals(record.recordOrigin, "Direct",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string action = (record.actionType ?? string.Empty)
                .ToLowerInvariant();
            bool isSprayerAction = action.Contains("spray");
            bool lacksCondition =
                string.IsNullOrWhiteSpace(record.conditionType);
            bool lacksSeverityDelta =
                record.beforeSeverity <= record.afterSeverity + 0.01f;
            return isSprayerAction && lacksCondition && lacksSeverityDelta;
        }

        private int FindDuplicateEventRecord(ClimateActionRecord record)
        {
            if (record == null || actionRecords.Count == 0)
                return -1;

            float minimumDay = record.gameDay - 0.0025f;
            for (int i = actionRecords.Count - 1; i >= 0; i--)
            {
                ClimateActionRecord previous = actionRecords[i];
                if (previous == null) continue;
                if (previous.gameDay < minimumDay) break;
                if (string.Equals(previous.recordOrigin, record.recordOrigin,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!SameOrEmpty(previous.cropId, record.cropId)) continue;
                if (!SameOrEmpty(previous.cropType, record.cropType)) continue;
                if (!SameOrEmpty(previous.conditionType, record.conditionType)) continue;

                string previousAction = NormalizeAction(previous);
                string currentAction = NormalizeAction(record);
                if (string.Equals(previousAction, currentAction,
                        System.StringComparison.Ordinal))
                {
                    return i;
                }

                if (IsTreatmentAction(previousAction) &&
                    IsTreatmentAction(currentAction) &&
                    (IsLowDetailTreatment(previous) ||
                     IsLowDetailTreatment(record)))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool ShouldPreferIncomingRecord(
            ClimateActionRecord previous,
            ClimateActionRecord incoming)
        {
            if (previous == null || incoming == null)
                return false;

            int previousDetail = TreatmentDetailScore(previous);
            int incomingDetail = TreatmentDetailScore(incoming);
            return incomingDetail > previousDetail;
        }

        private static int TreatmentDetailScore(
            ClimateActionRecord record)
        {
            if (record == null) return 0;
            int score = 0;
            if (!string.IsNullOrWhiteSpace(record.conditionType)) score += 4;
            if (record.beforeSeverity > record.afterSeverity + 0.01f) score += 4;
            if (!string.IsNullOrWhiteSpace(record.itemType) &&
                !string.Equals(record.itemType, "ObservedMitigation",
                    System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(record.itemType, "SprayerPump",
                    System.StringComparison.OrdinalIgnoreCase)) score += 2;
            if (string.Equals(record.recordOrigin, "Observer",
                    System.StringComparison.OrdinalIgnoreCase)) score += 1;
            return score;
        }

        private static bool IsLowDetailTreatment(
            ClimateActionRecord record)
        {
            return record != null &&
                   string.IsNullOrWhiteSpace(record.conditionType) &&
                   record.beforeSeverity <= record.afterSeverity + 0.01f;
        }

        private static bool IsTreatmentAction(string normalizedAction)
        {
            return !string.IsNullOrWhiteSpace(normalizedAction) &&
                   normalizedAction.StartsWith("treatment:",
                       System.StringComparison.Ordinal);
        }

        private static void MergeMissingTreatmentDetails(
            ClimateActionRecord preferred,
            ClimateActionRecord fallback)
        {
            if (preferred == null || fallback == null) return;
            if (string.IsNullOrWhiteSpace(preferred.itemType) ||
                string.Equals(preferred.itemType, "ObservedMitigation",
                    System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(preferred.itemType, "SprayerPump",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                preferred.itemType = fallback.itemType;
            }
            if (string.IsNullOrWhiteSpace(preferred.taskSource))
                preferred.taskSource = fallback.taskSource;
            if (string.IsNullOrWhiteSpace(preferred.details))
                preferred.details = fallback.details;
        }

        private static bool SameOrEmpty(string first, string second)
        {
            return string.IsNullOrWhiteSpace(first) ||
                   string.IsNullOrWhiteSpace(second) ||
                   string.Equals(first, second,
                       System.StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeAction(ClimateActionRecord record)
        {
            string value =
                (record.actionType ?? string.Empty) + "|" +
                (record.itemType ?? string.Empty) + "|" +
                (record.taskSource ?? string.Empty);
            value = value.ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);

            if (value.Contains("mulch")) return "maintenance:mulch";
            if (value.Contains("compost")) return "maintenance:compost";
            if (value.Contains("supportstake") || value.Contains("stake"))
                return "maintenance:supportstake";
            if (value.Contains("trellis")) return "maintenance:trellis";
            if (value.Contains("raisedbed")) return "maintenance:raisedbed";
            if (value.Contains("prun")) return "maintenance:prune";
            if (value.Contains("irrigation")) return "climate:irrigation";
            if (value.Contains("waterstoragetank")) return "climate:tank";
            if (value.Contains("shadenet")) return "climate:shade";
            if (value.Contains("windbreak")) return "climate:windbreak";
            if (value.Contains("greenhouse")) return "climate:greenhouse";
            if (value.Contains("drainagecanal")) return "climate:canal";
            if (value.Contains("aphidtrap")) return "trap:aphid";
            if (value.Contains("pheromone")) return "trap:pheromone";
            if (value.Contains("termitebait")) return "trap:termite";
            if (value.Contains("fruitbag")) return "protection:fruitbag";
            if (value.Contains("drainagekit") ||
                value.Contains("drainageimprovement"))
                return "protection:drainage";
            if (value.Contains("spray") || value.Contains("mitigate") ||
                !string.IsNullOrWhiteSpace(record.conditionType))
            {
                if (value.Contains("neem")) return "treatment:neem";
                if (value.Contains("btbio")) return "treatment:bt";
                if (value.Contains("copper")) return "treatment:copper";
                if (value.Contains("disinfect")) return "treatment:disinfect";
                if (value.Contains("sanitation") || value.Contains("machete"))
                    return "treatment:sanitation";
                return "treatment:" + (record.itemType ?? string.Empty).ToLowerInvariant();
            }
            if (value.Contains("water")) return "water";
            if (value.Contains("plantcrop")) return "plant";
            if (value.Contains("harvest")) return "harvest";
            if (value.Contains("dig")) return "dig";
            if (value.Contains("sell")) return "sell";
            if (value.Contains("cleantrap")) return "cleantrap";
            if (value.Contains("remove")) return "remove";
            return value;
        }
        private void EnsureReferences()
        {
            if (geminiClient == null) geminiClient = Object.FindFirstObjectByType<ClimateResilienceGeminiClient>();
            if (popupBuilder == null) popupBuilder = Object.FindFirstObjectByType<ClimateEvaluationPopupBuilder>();
        }
        private void StartTracking(WeatherEventType eventType, int durationDays)
        {
            IsTrackingEvent = true;
            ActiveEventType = eventType;
            ActiveDurationDays = Mathf.Max(1, durationDays);
            EventStartGameDay = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : 0f;
            beforeSnapshots.Clear();
            actionRecords.Clear();
            beforeSnapshots.AddRange(CaptureAllCropSnapshots());
            if (logDebug)
                Debug.Log("[ClimateTracker] Event started | Type=" + eventType + " | Duration=" + ActiveDurationDays + " | Crops=" + beforeSnapshots.Count);
            EnsureReferences();
            if (popupBuilder != null) popupBuilder.ShowEventStarted(eventType.ToString(), ActiveDurationDays);
        }
        private void FinishTracking()
        {
            if (isSubmittingEvaluation) return;
            isSubmittingEvaluation = true;
            ClimateEventEvaluationPayload payload = new ClimateEventEvaluationPayload
            {
                eventType = ActiveEventType.ToString(),
                durationDays = ActiveDurationDays,
                startGameDay = EventStartGameDay,
                endGameDay = GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : EventStartGameDay
            };
            payload.beforeCrops.AddRange(beforeSnapshots);
            payload.afterCrops.AddRange(CaptureAllCropSnapshots());
            payload.actions.AddRange(actionRecords);
            payload.precomputedScore = ComputeScore(payload);
            string json = JsonUtility.ToJson(payload, true);
            if (logDebug) Debug.Log("[ClimateTracker] Evaluation payload:\n" + json);
            EnsureReferences();
            if (geminiClient != null)
            {
                StartCoroutine(geminiClient.EvaluateClimateEvent(
                    json,
                    reply =>
                    {
                        if (popupBuilder != null) popupBuilder.ShowEventEnded(payload.eventType, payload.precomputedScore, reply);
                        ResetTrackingState();
                    },
                    error =>
                    {
                        string fallback = "The climate event has ended.\n\nAI evaluation could not be generated.\n\n" +
                                          "Mitigation Score: " + Mathf.RoundToInt(payload.precomputedScore) + "%\n\n" +
                                          BuildLocalSummary(payload);
                        if (popupBuilder != null) popupBuilder.ShowEventEnded(payload.eventType, payload.precomputedScore, fallback);
                        ResetTrackingState();
                    }));
            }
            else
            {
                string fallback = "The climate event has ended.\n\nMitigation Score: " + Mathf.RoundToInt(payload.precomputedScore) + "%\n\n" + BuildLocalSummary(payload);
                if (popupBuilder != null) popupBuilder.ShowEventEnded(payload.eventType, payload.precomputedScore, fallback);
                ResetTrackingState();
            }
        }
        private string BuildLocalSummary(ClimateEventEvaluationPayload payload)
        {
            int successful = 0;
            int recommended = 0;
            int counterproductive = 0;
            foreach (ClimateActionRecord action in payload.actions)
            {
                if (action.actionSucceeded) successful++;
                if (action.effectivenessScore > 0f) recommended++;
                if (action.effectivenessScore < 0f) counterproductive++;
            }
            return "Recorded actions: " + payload.actions.Count +
                   "\nSuccessful actions: " + successful +
                   "\nRecommended actions: " + recommended +
                   "\nCounterproductive or unnecessary actions: " + counterproductive + ".";
        }
        private void ResetTrackingState()
        {
            IsTrackingEvent = false;
            ActiveEventType = WeatherEventType.Clear;
            ActiveDurationDays = 0;
            EventStartGameDay = 0f;
            beforeSnapshots.Clear();
            actionRecords.Clear();
            isSubmittingEvaluation = false;
            if (logDebug) Debug.Log("[ClimateTracker] Tracking state reset.");
        }
        private List<CropSnapshot> CaptureAllCropSnapshots()
        {
            List<CropSnapshot> result = new List<CropSnapshot>();
            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                CropClimateMaintenanceState maintenance = crop.Root.GetComponent<CropClimateMaintenanceState>();
                result.Add(new CropSnapshot
                {
                    cropId = crop.CropId,
                    cropType = crop.CropDisplayName,
                    district = crop.District,
                    stage = crop.Stage,
                    health = crop.Health,
                    averageHealth = crop.AverageHealth,
                    stress = crop.Stress,
                    moisture = crop.Moisture,
                    fertility = crop.Fertility,
                    drainage = crop.Drainage,
                    soilSuitability = crop.SoilSuitability,
                    maintenanceState = maintenance != null ? maintenance.GetInspectionText() : "None"
                });
            }
            return result;
        }
        private float ComputeScore(ClimateEventEvaluationPayload payload)
        {
            float score = 45f;
            foreach (ClimateActionRecord action in payload.actions)
                score += Mathf.Clamp(action.effectivenessScore, -8f, 10f);
            float totalHealthDelta = 0f;
            float totalStressDelta = 0f;
            float totalMoistureDelta = 0f;
            int compared = 0;
            foreach (CropSnapshot before in payload.beforeCrops)
            {
                CropSnapshot after = payload.afterCrops.Find(x => x.cropId == before.cropId);
                if (after == null) continue;
                totalHealthDelta += after.health - before.health;
                totalStressDelta += after.stress - before.stress;
                totalMoistureDelta += after.moisture - before.moisture;
                compared++;
            }
            if (compared > 0)
            {
                float avgHealthDelta = totalHealthDelta / compared;
                float avgStressDelta = totalStressDelta / compared;
                float avgMoistureDelta = totalMoistureDelta / compared;
                score += Mathf.Clamp(avgHealthDelta * 0.7f, -18f, 18f);
                score -= Mathf.Clamp(avgStressDelta * 0.45f, -18f, 18f);
                if (payload.eventType == WeatherEventType.ExtremeDrought.ToString())
                    score += Mathf.Clamp(avgMoistureDelta * 35f, -15f, 15f);
                else if (payload.eventType == WeatherEventType.Typhoon.ToString())
                    score -= Mathf.Clamp(avgMoistureDelta * 25f, -12f, 12f);
            }
            if (payload.actions.Count == 0) score -= 15f;
            return Mathf.Clamp(score, 0f, 100f);
        }
        public ClimateEventSaveDto CaptureSaveData()
        {
            return new ClimateEventSaveDto
            {
                isTrackingEvent = IsTrackingEvent,
                activeEventType = ActiveEventType.ToString(),
                activeDurationDays = ActiveDurationDays,
                eventStartGameDay = EventStartGameDay,
                beforeSnapshots = new List<CropSnapshot>(beforeSnapshots),
                actionRecords = new List<ClimateActionRecord>(actionRecords)
            };
        }
        public void RestoreSaveData(ClimateEventSaveDto save)
        {
            beforeSnapshots.Clear();
            actionRecords.Clear();
            isSubmittingEvaluation = false;
            if (save == null)
            {
                IsTrackingEvent = false;
                ActiveEventType = WeatherEventType.Clear;
                ActiveDurationDays = 0;
                EventStartGameDay = 0f;
                return;
            }
            IsTrackingEvent = save.isTrackingEvent;
            ActiveDurationDays = Mathf.Max(0, save.activeDurationDays);
            EventStartGameDay = Mathf.Max(0f, save.eventStartGameDay);
            if (!System.Enum.TryParse(save.activeEventType, out WeatherEventType restoredType)) restoredType = WeatherEventType.Clear;
            ActiveEventType = restoredType;
            if (save.beforeSnapshots != null) beforeSnapshots.AddRange(save.beforeSnapshots);
            if (save.actionRecords != null) actionRecords.AddRange(save.actionRecords);
            if (logDebug && IsTrackingEvent)
                Debug.Log("[ClimateTracker] Restored active event " + ActiveEventType + " with " + actionRecords.Count + " recorded actions.");
        }
    }
}


