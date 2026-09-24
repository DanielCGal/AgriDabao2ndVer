using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AgriDabao3D
{
    public class AIAdvisorTaskSystem : MonoBehaviour
    {
        public static AIAdvisorTaskSystem Instance { get; private set; }

        public AIAdvisorTaskGeminiClient geminiClient;
        public event Action OnStateChanged;

        public bool HasActiveTask => state.active;
        public bool IsBusy => busy;
        public string DisplayText => BuildDisplayText();
        public int RewardMoney => state.rewardMoney;
        public AIAdvisorTaskSaveDto CurrentState => state;

        private AIAdvisorTaskSaveDto state =
            new AIAdvisorTaskSaveDto();
        private bool busy;

        private static readonly HashSet<string> AllowedObjectiveTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "SellCropValue",
                "HarvestQuantity",
                "WaterCrops",
                "ImproveMoisture",
                "ReduceCondition",
                "ReduceFarmSeverity",
                "InstallMitigation",
                "ApplyMaintenance",
                "PlantQuantity",
                "ImproveHealth",
                "ReduceStress"
            };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureClient();
        }

        public void RequestTask()
        {
            if (busy || state.active)
                return;
            StartCoroutine(RequestTaskRoutine());
        }

        private IEnumerator RequestTaskRoutine()
        {
            busy = true;
            NotifyChanged();
            EnsureClient();

            FarmTaskContextPayload context =
                FarmTaskContextBuilder.Capture();
            string json = JsonUtility.ToJson(context, true);
            AIAdvisorTaskGenerationEnvelope generated = null;
            string error = null;

            if (geminiClient != null)
            {
                yield return geminiClient.GenerateTask(
                    json,
                    value => generated = value,
                    message => error = message);
            }
            else
            {
                error = "AI task client was not found.";
            }

            if (generated == null ||
                !IsGeneratedTaskUsable(generated, context))
            {
                generated = BuildFallbackTask(context);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Debug.LogWarning(
                        "[AIAdvisorTask] Gemini generation fallback: " +
                        error);
                }
            }

            BeginTask(generated);
            busy = false;
            NotifyChanged();
        }

        private void BeginTask(
            AIAdvisorTaskGenerationEnvelope generated)
        {
            state = new AIAdvisorTaskSaveDto
            {
                active = true,
                checking = false,
                taskId = Guid.NewGuid().ToString("N"),
                dialogue = PolishDialogue(generated.dialogue),
                taskText = PolishTaskText(generated.taskText),
                rewardMoney = Mathf.Clamp(
                    generated.rewardMoney, 800, 2500),
                startedGameDay =
                    GameTimeSystem.Instance != null
                        ? GameTimeSystem.Instance.TotalGameDays
                        : 0f,
                objective =
                    generated.objective ??
                    new AIAdvisorTaskObjective(),
                beforeSnapshots =
                    FarmTaskContextBuilder.CaptureCropSnapshots(),
                actionRecords =
                    new List<ClimateActionRecord>(),
                lastVerdict = string.Empty,
                lastTips = string.Empty
            };

            NormalizeObjective(state.objective);
            state.objective.baselineMetric =
                CaptureObjectiveMetric(state.objective);

            FarmTaskPopupUI.Instance?.Show(
                "AI Adviser Task",
                "A new multi-day task has been assigned.");
        }

        public void SkipTask()
        {
            if (busy || !state.active)
                return;

            string oldTask = state.taskText;
            state = new AIAdvisorTaskSaveDto();
            FarmTaskPopupUI.Instance?.Show(
                "AI Adviser Task Skipped",
                string.IsNullOrWhiteSpace(oldTask)
                    ? "The active task was abandoned."
                    : "You abandoned: " + oldTask);
            NotifyChanged();
        }

        public void CheckTask()
        {
            if (busy || !state.active)
                return;
            StartCoroutine(CheckTaskRoutine());
        }

        private IEnumerator CheckTaskRoutine()
        {
            busy = true;
            state.checking = true;
            NotifyChanged();
            EnsureClient();

            AIAdvisorTaskCheckPayload payload =
                new AIAdvisorTaskCheckPayload
                {
                    task = CaptureSaveData(),
                    currentFarm =
                        FarmTaskContextBuilder.Capture()
                };
            string json = JsonUtility.ToJson(payload, true);

            AIAdvisorTaskCheckEnvelope verdict = null;
            string error = null;
            if (geminiClient != null)
            {
                yield return geminiClient.CheckTask(
                    json,
                    value => verdict = value,
                    message => error = message);
            }
            else
            {
                error = "AI task client was not found.";
            }

            AIAdvisorTaskCheckEnvelope localVerdict =
                EvaluateLocally();
            if (verdict == null)
            {
                verdict = localVerdict;
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Debug.LogWarning(
                        "[AIAdvisorTask] Gemini check fallback: " +
                        error);
                }
            }
            else if (verdict.completed &&
                     (localVerdict == null ||
                      !localVerdict.completed))
            {
                verdict.completed = false;
                verdict.reason =
                    "The AI review was positive, but the saved measurable " +
                    "progress has not reached the required target yet. " +
                    (localVerdict != null
                        ? localVerdict.reason
                        : string.Empty);
                verdict.progressSummary =
                    localVerdict != null
                        ? localVerdict.progressSummary
                        : verdict.progressSummary;
                verdict.tips =
                    localVerdict != null
                        ? localVerdict.tips
                        : verdict.tips;
            }

            ApplyVerdict(verdict);
            busy = false;
            if (state != null)
                state.checking = false;
            NotifyChanged();
        }

        private void ApplyVerdict(
            AIAdvisorTaskCheckEnvelope verdict)
        {
            if (verdict == null)
                return;

            verdict.reason = PolishVerdictText(verdict.reason);
            verdict.progressSummary = PolishVerdictText(verdict.progressSummary);

            if (verdict.tips != null)
            {
                for (int i = 0; i < verdict.tips.Count; i++)
                    verdict.tips[i] = PolishVerdictText(verdict.tips[i]);
            }

            string tips = verdict.tips != null &&
                          verdict.tips.Count > 0
                ? string.Join("\n- ", verdict.tips)
                : "Continue working on the exact measurable target.";

            if (!verdict.completed)
            {
                state.lastVerdict = verdict.reason;
                state.lastTips = tips;
                string body =
                    "The task is not complete yet.\n\n" +
                    verdict.reason;
                if (!string.IsNullOrWhiteSpace(
                        verdict.progressSummary))
                {
                    body += "\n\nProgress:\n" +
                            verdict.progressSummary;
                }
                body += "\n\nTips:\n- " + tips;
                FarmTaskPopupUI.Instance?.Show(
                    "AI Adviser Check", body);
                return;
            }

            int reward = Mathf.Clamp(
                state.rewardMoney, 800, 2500);
            if (PlayerInventory.Instance != null)
                PlayerInventory.Instance.AddMoney(reward);
            GameAudioManager.Instance.PlayReward();

            string completedTask = state.taskText;
            string reason = verdict.reason;
            state = new AIAdvisorTaskSaveDto();

            FarmTaskPopupUI.Instance?.Show(
                "AI Adviser Task Completed",
                "Task completed: " + completedTask +
                "\n\n" + reason +
                "\n\nYou received P" + reward + ".");
        }

        public void RecordAction(ClimateActionRecord record)
        {
            if (!state.active ||
                record == null ||
                !record.actionSucceeded)
            {
                return;
            }

            if (state.actionRecords == null)
                state.actionRecords =
                    new List<ClimateActionRecord>();
            state.actionRecords.Add(record);
            NotifyChanged();
        }

        public AIAdvisorTaskSaveDto CaptureSaveData()
        {
            return new AIAdvisorTaskSaveDto
            {
                active = state.active,
                checking = false,
                taskId = state.taskId,
                dialogue = state.dialogue,
                taskText = state.taskText,
                rewardMoney = state.rewardMoney,
                startedGameDay = state.startedGameDay,
                objective = state.objective,
                beforeSnapshots = state.beforeSnapshots != null
                    ? new List<CropSnapshot>(
                        state.beforeSnapshots)
                    : new List<CropSnapshot>(),
                actionRecords = state.actionRecords != null
                    ? new List<ClimateActionRecord>(
                        state.actionRecords)
                    : new List<ClimateActionRecord>(),
                lastVerdict = state.lastVerdict,
                lastTips = state.lastTips
            };
        }

        public void RestoreSaveData(
            AIAdvisorTaskSaveDto save)
        {
            state = save ?? new AIAdvisorTaskSaveDto();
            state.checking = false;
            if (state.objective == null)
                state.objective =
                    new AIAdvisorTaskObjective();
            state.objective.itemType =
                PlantingMaterialCatalog.UpgradeLegacyList(state.objective.itemType);
            if (state.beforeSnapshots == null)
                state.beforeSnapshots =
                    new List<CropSnapshot>();
            if (state.actionRecords == null)
                state.actionRecords =
                    new List<ClimateActionRecord>();
            busy = false;
            NotifyChanged();
        }

        private void EnsureClient()
        {
            if (geminiClient == null)
            {
                geminiClient =
                    UnityEngine.Object.FindFirstObjectByType<
                        AIAdvisorTaskGeminiClient>();
            }

            if (geminiClient == null)
            {
                geminiClient =
                    gameObject.AddComponent<
                        AIAdvisorTaskGeminiClient>();
            }
        }

        private bool IsGeneratedTaskUsable(
            AIAdvisorTaskGenerationEnvelope task,
            FarmTaskContextPayload context)
        {
            if (task == null ||
                task.objective == null ||
                context == null ||
                string.IsNullOrWhiteSpace(task.taskText) ||
                string.IsNullOrWhiteSpace(task.objective.type) ||
                !AllowedObjectiveTypes.Contains(
                    task.objective.type))
            {
                return false;
            }

            NormalizeObjective(task.objective);
            AIAdvisorTaskObjective objective = task.objective;
            string cropType = objective.cropType;
            string cropId = objective.cropId;
            string condition = objective.conditionType;

            List<FarmTaskCropContext> relevantCrops =
                (context.crops ?? new List<FarmTaskCropContext>())
                    .Where(crop =>
                        crop != null &&
                        (string.IsNullOrWhiteSpace(cropType) ||
                         string.Equals(crop.cropType, cropType,
                             StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(cropId) ||
                         string.Equals(crop.cropId, cropId,
                             StringComparison.Ordinal)))
                    .ToList();

            bool allowsMissingCurrentCrop =
                objective.type.Equals(
                    "PlantQuantity",
                    StringComparison.OrdinalIgnoreCase) ||
                objective.type.Equals(
                    "SellCropValue",
                    StringComparison.OrdinalIgnoreCase);
            if ((!string.IsNullOrWhiteSpace(cropType) ||
                 !string.IsNullOrWhiteSpace(cropId)) &&
                relevantCrops.Count == 0 &&
                !allowsMissingCurrentCrop)
            {
                return false;
            }

            switch (objective.type)
            {
                case "ReduceCondition":
                {
                    List<FarmTaskConditionContext> matches =
                        relevantCrops
                            .SelectMany(crop =>
                                crop.conditions ??
                                new List<FarmTaskConditionContext>())
                            .Where(value =>
                                value != null &&
                                (string.IsNullOrWhiteSpace(condition) ||
                                 string.Equals(value.type, condition,
                                     StringComparison.OrdinalIgnoreCase)))
                            .ToList();
                    if (matches.Count == 0)
                        return false;
                    float severity = matches.Max(value => value.severity);
                    bool thresholdRequiresWork =
                        objective.targetThreshold > 0f &&
                        objective.targetThreshold < severity - 0.1f;
                    bool reductionIsPossible =
                        objective.requiredReduction > 0f &&
                        objective.requiredReduction <= severity + 0.1f;
                    return thresholdRequiresWork || reductionIsPossible;
                }

                case "ReduceFarmSeverity":
                {
                    float total =
                        FarmTaskContextBuilder
                            .GetTotalConditionSeverity();
                    if (total <= 0.1f)
                        return false;
                    bool thresholdRequiresWork =
                        objective.targetThreshold > 0f &&
                        objective.targetThreshold < total - 0.1f;
                    bool reductionIsPossible =
                        objective.requiredReduction > 0f &&
                        objective.requiredReduction <= total + 0.1f;
                    return thresholdRequiresWork || reductionIsPossible;
                }

                case "WaterCrops":
                {
                    int target = Mathf.Max(1, objective.targetAmount);
                    if (relevantCrops.Count < target)
                        return false;
                    float threshold = objective.targetThreshold > 0f
                        ? objective.targetThreshold / 100f
                        : 0.60f;
                    return relevantCrops.Count(crop =>
                        crop.moisture < threshold) >= target;
                }

                case "ImproveMoisture":
                {
                    if (relevantCrops.Count == 0)
                        return false;
                    float current = relevantCrops.Average(crop =>
                        crop.moisture * 100f);
                    return objective.targetThreshold >
                           current + 0.1f;
                }

                case "ImproveHealth":
                {
                    if (relevantCrops.Count == 0)
                        return false;
                    float current = relevantCrops.Average(crop =>
                        crop.health);
                    return objective.requiredReduction > 0f &&
                           current + objective.requiredReduction <=
                           100.1f;
                }

                case "ReduceStress":
                {
                    if (relevantCrops.Count == 0)
                        return false;
                    float current = relevantCrops.Average(crop =>
                        crop.stress);
                    return objective.requiredReduction > 0f &&
                           current >= objective.requiredReduction;
                }

                case "SellCropValue":
                {
                    List<FarmTaskInventoryContext> items =
                        context.inventory ??
                        new List<FarmTaskInventoryContext>();
                    if (!string.IsNullOrWhiteSpace(cropType))
                    {
                        return relevantCrops.Count > 0 ||
                               items.Any(item =>
                                   string.Equals(
                                       item.itemType,
                                       cropType,
                                       StringComparison.OrdinalIgnoreCase) &&
                                   item.amount > 0);
                    }
                    return relevantCrops.Count > 0 ||
                           items.Any(item =>
                               FarmTaskContextBuilder
                                   .CropNameToFruitItem(
                                       item.itemType) !=
                               InventoryItemType.None &&
                               item.amount > 0);
                }

                case "HarvestQuantity":
                case "InstallMitigation":
                case "ApplyMaintenance":
                    return relevantCrops.Count > 0;

                case "PlantQuantity":
                    return objective.targetAmount > 0 &&
                           CanObtainSeedFor(cropType, objective.itemType);

                default:
                    return relevantCrops.Count > 0;
            }
        }

        private static bool CanObtainSeedFor(string cropType, string itemType)
        {
            List<InventoryItemType> options = new List<InventoryItemType>();

            if (PlantingMaterialCatalog.TryParseItem(itemType, out InventoryItemType named) &&
                PlantingMaterialCatalog.IsPlantingMaterial(PlantingMaterialCatalog.UpgradeLegacy(named)))
            {
                options.Add(PlantingMaterialCatalog.UpgradeLegacy(named));
            }
            else
            {
                options.AddRange(FarmTaskContextBuilder.CropNameToPlantingMaterials(cropType));
            }

            if (options.Count == 0)
                return true;

            foreach (InventoryItemType option in options)
            {
                if (PlayerInventory.Instance != null && PlayerInventory.Instance.GetCount(option) > 0)
                    return true;

                if (DistrictCropPools.IsAvailableToPlayer(option))
                    return true;
            }

            return PlantingMaterialCatalog.TryGetCropType(cropType, out FarmCropType crop) &&
                   NurserySystem.Instance != null &&
                   NurserySystem.Instance.CountReady(crop) > 0;
        }

        private static void NormalizeObjective(
            AIAdvisorTaskObjective objective)
        {
            if (objective == null)
                return;
            objective.type =
                CleanText(objective.type);
            objective.cropType =
                CleanText(objective.cropType);
            objective.cropId =
                CleanText(objective.cropId);
            objective.conditionType =
                CleanText(objective.conditionType);
            objective.itemType =
                PlantingMaterialCatalog.UpgradeLegacyList(
                    CleanText(objective.itemType));
            objective.targetAmount =
                Mathf.Clamp(objective.targetAmount, 0, 1000);
            objective.targetValue =
                Mathf.Clamp(objective.targetValue, 0, 100000);
            objective.targetThreshold =
                Mathf.Clamp(objective.targetThreshold, 0f, 100f);
            objective.requiredReduction =
                Mathf.Clamp(objective.requiredReduction, 0f, 10000f);

            switch (objective.type)
            {
                case "SellCropValue":
                    if (objective.targetValue <= 0)
                        objective.targetValue = 1000;
                    break;
                case "HarvestQuantity":
                case "WaterCrops":
                case "InstallMitigation":
                case "ApplyMaintenance":
                case "PlantQuantity":
                    if (objective.targetAmount <= 0)
                        objective.targetAmount = 2;
                    break;
                case "ImproveMoisture":
                    if (objective.targetThreshold <= 0f)
                        objective.targetThreshold = 65f;
                    break;
                case "ReduceCondition":
                case "ReduceFarmSeverity":
                    if (objective.requiredReduction <= 0f &&
                        objective.targetThreshold <= 0f)
                    {
                        objective.requiredReduction = 20f;
                    }
                    break;
                case "ImproveHealth":
                case "ReduceStress":
                    if (objective.requiredReduction <= 0f)
                        objective.requiredReduction = 8f;
                    break;
            }
        }

        private static AIAdvisorTaskGenerationEnvelope
            BuildFallbackTask(
                FarmTaskContextPayload context)
        {
            FarmTaskCropContext mostAffected =
                context.crops
                    .Where(crop =>
                        crop.conditions != null &&
                        crop.conditions.Count > 0)
                    .OrderByDescending(crop =>
                        crop.conditions.Sum(condition =>
                            condition.severity))
                    .FirstOrDefault();

            if (mostAffected != null)
            {
                FarmTaskConditionContext condition =
                    mostAffected.conditions
                        .OrderByDescending(value =>
                            value.severity)
                        .First();
                float target = Mathf.Max(
                    10f, condition.severity - 25f);
                return Envelope(
                    "I found an active outbreak that needs a sustained response. " +
                    "Use only treatments that match the crop's current condition, " +
                    "then check the crop again after the mitigation has taken effect.",
                    "reduce " + condition.type + " on " +
                    mostAffected.cropType + " to " +
                    target.ToString("F0") + "% severity or lower",
                    1400,
                    "ReduceCondition",
                    mostAffected.cropType,
                    mostAffected.cropId,
                    condition.type,
                    string.Empty,
                    0, 0, target, 20f);
            }

            if (string.Equals(
                    context.weather,
                    WeatherEventType.ExtremeDrought.ToString(),
                    StringComparison.OrdinalIgnoreCase) &&
                context.crops != null &&
                context.crops.Count > 0)
            {
                int dryCount = context.crops.Count(crop =>
                    crop.moisture < 0.55f);
                if (dryCount > 0)
                {
                    int count = Mathf.Clamp(
                        dryCount,
                        1, Mathf.Min(4, context.crops.Count));
                    return Envelope(
                        "The drought is drawing moisture from your crops faster than usual. " +
                        "Focus on the driest plants and restore safe moisture before their stress rises further.",
                        "water " + count +
                        " drought-stressed crops and raise each selected crop to at least 60% moisture",
                        1300,
                        "WaterCrops",
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        "WateringCan",
                        count, 0, 60f, 0f);
                }

                return Envelope(
                    "Your crops are currently holding moisture, but the drought can continue for several days. " +
                    "Build lasting protection now instead of waiting for the farm to become critically dry.",
                    "install two suitable drought protections such as irrigation, water storage, mulch, shade netting, compost, or a greenhouse",
                    1500,
                    "InstallMitigation",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "DroughtProtection",
                    2, 0, 0f, 0f);
            }

            if (string.Equals(
                    context.weather,
                    WeatherEventType.Typhoon.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return Envelope(
                    "The typhoon can raise stress and waterlogging risk across the farm. " +
                    "Install protection that continues helping the crops while the storm lasts.",
                    "install two suitable typhoon protections such as a windbreak, drainage canal, raised bed, stake, trellis, pruning, or greenhouse",
                    1500,
                    "InstallMitigation",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    "TyphoonProtection",
                    2, 0, 0f, 0f);
            }

            FarmTaskInventoryContext valuableInventory =
                context.inventory
                    .Where(item =>
                        FarmTaskContextBuilder
                            .CropNameToFruitItem(item.itemType) !=
                        InventoryItemType.None)
                    .OrderByDescending(item => item.amount)
                    .FirstOrDefault();

            if (valuableInventory != null &&
                valuableInventory.amount >= 20)
            {
                return Envelope(
                    "Your storage contains a large amount of harvested produce. " +
                    "Turn part of that stock into working capital without emptying the entire farm.",
                    "sell at least P1000 worth of " +
                    valuableInventory.itemType +
                    " through the shipping bin",
                    1200,
                    "SellCropValue",
                    valuableInventory.itemType,
                    string.Empty,
                    string.Empty,
                    valuableInventory.itemType,
                    0, 1000, 0f, 0f);
            }

            int availableHarvest = context.crops
                .Where(crop => crop.harvestReady)
                .Sum(crop => Mathf.Max(0, crop.availableHarvest));
            if (availableHarvest > 0)
            {
                int target = Mathf.Clamp(
                    Mathf.Max(3, availableHarvest),
                    3, 20);
                return Envelope(
                    "Several crops are ready to contribute to the farm's income. " +
                    "Complete the harvest carefully and build a useful stock of produce.",
                    "collect at least " + target +
                    " harvested crop items",
                    1000,
                    "HarvestQuantity",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    target, 0, 0f, 0f);
            }

            return Envelope(
                "The farm needs a stronger production base before larger objectives become practical. " +
                "Expand carefully and keep the new crops manageable.",
                "plant three crops and keep the new plantings established",
                1000,
                "PlantQuantity",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                3, 0, 0f, 0f);
        }

        private static AIAdvisorTaskGenerationEnvelope Envelope(
            string dialogue,
            string task,
            int reward,
            string type,
            string cropType,
            string cropId,
            string conditionType,
            string itemType,
            int targetAmount,
            int targetValue,
            float targetThreshold,
            float requiredReduction)
        {
            return new AIAdvisorTaskGenerationEnvelope
            {
                dialogue = dialogue,
                taskText = task,
                rewardMoney = reward,
                objective = new AIAdvisorTaskObjective
                {
                    type = type,
                    cropType = cropType,
                    cropId = cropId,
                    conditionType = conditionType,
                    itemType = itemType,
                    targetAmount = targetAmount,
                    targetValue = targetValue,
                    targetThreshold = targetThreshold,
                    requiredReduction = requiredReduction
                }
            };
        }

        private AIAdvisorTaskCheckEnvelope EvaluateLocally()
        {
            AIAdvisorTaskObjective objective =
                state.objective ??
                new AIAdvisorTaskObjective();
            string type = objective.type ?? string.Empty;
            int amount = 0;
            int value = 0;
            float reduction = 0f;
            int relevantActions = 0;
            HashSet<string> unique =
                new HashSet<string>(StringComparer.Ordinal);

            foreach (ClimateActionRecord action in
                     state.actionRecords ??
                     new List<ClimateActionRecord>())
            {
                if (action == null ||
                    !action.actionSucceeded)
                {
                    continue;
                }

                bool cropMatches =
                    string.IsNullOrWhiteSpace(
                        objective.cropType) ||
                    string.Equals(
                        objective.cropType,
                        action.cropType,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        objective.cropType,
                        action.itemType,
                        StringComparison.OrdinalIgnoreCase);
                bool cropIdMatches =
                    string.IsNullOrWhiteSpace(
                        objective.cropId) ||
                    string.Equals(
                        objective.cropId,
                        action.cropId,
                        StringComparison.Ordinal);
                bool conditionMatches =
                    string.IsNullOrWhiteSpace(
                        objective.conditionType) ||
                    string.Equals(
                        objective.conditionType,
                        action.conditionType,
                        StringComparison.OrdinalIgnoreCase);

                switch (type)
                {
                    case "SellCropValue":
                        if (action.actionType == "SellCrop" &&
                            cropMatches)
                        {
                            value += Mathf.Max(
                                0, action.moneyDelta);
                            relevantActions++;
                        }
                        break;
                    case "HarvestQuantity":
                        if (action.actionType == "HarvestCrop" &&
                            cropMatches && cropIdMatches)
                        {
                            amount += Mathf.Max(
                                1, action.quantity);
                            relevantActions++;
                        }
                        break;
                    case "WaterCrops":
                        if (action.actionType == "WaterCrop" &&
                            cropMatches && cropIdMatches)
                        {
                            unique.Add(
                                string.IsNullOrWhiteSpace(
                                    action.cropId)
                                    ? Guid.NewGuid().ToString("N")
                                    : action.cropId);
                            relevantActions++;
                        }
                        break;
                    case "ImproveMoisture":
                        if (action.actionType == "WaterCrop" &&
                            cropMatches && cropIdMatches)
                        {
                            relevantActions++;
                        }
                        break;
                    case "ReduceCondition":
                        if (cropMatches && cropIdMatches &&
                            conditionMatches &&
                            action.beforeSeverity >
                            action.afterSeverity)
                        {
                            reduction +=
                                action.beforeSeverity -
                                action.afterSeverity;
                            relevantActions++;
                        }
                        break;
                    case "ReduceFarmSeverity":
                        if (action.beforeSeverity >
                            action.afterSeverity)
                        {
                            reduction +=
                                action.beforeSeverity -
                                action.afterSeverity;
                            relevantActions++;
                        }
                        break;
                    case "InstallMitigation":
                        if ((action.actionType ==
                                 "PlaceClimateMitigation" ||
                             action.actionType ==
                                 "ApplyCropMaintenance" ||
                             action.actionType ==
                                 "InstallDrainageKit" ||
                             action.actionType ==
                                 "InstallFruitBag" ||
                             action.actionType ==
                                 "PlaceAphidTrap" ||
                             action.actionType ==
                                 "PlacePestMitigationTrap") &&
                            ActionItemMatches(
                                objective.itemType,
                                action,
                                allowTyphoonProtection: true))
                        {
                            string key =
                                (action.actionType ?? string.Empty) + "|" +
                                (action.itemType ?? string.Empty) + "|" +
                                (action.cropId ?? string.Empty) + "|" +
                                action.worldPosition.x.ToString("F2") + "|" +
                                action.worldPosition.y.ToString("F2") + "|" +
                                action.worldPosition.z.ToString("F2") + "|" +
                                action.gameDay.ToString("F4");
                            if (unique.Add(key))
                                amount += Mathf.Max(1, action.quantity);
                            relevantActions++;
                        }
                        break;
                    case "ApplyMaintenance":
                        if ((action.actionType ==
                                 "ApplyCropMaintenance" ||
                             action.actionType ==
                                 "InstallDrainageKit" ||
                             action.actionType ==
                                 "InstallFruitBag") &&
                            cropMatches && cropIdMatches &&
                            ActionItemMatches(
                                objective.itemType,
                                action,
                                allowTyphoonProtection: false))
                        {
                            amount += Mathf.Max(
                                1, action.quantity);
                            relevantActions++;
                        }
                        break;
                    case "PlantQuantity":
                        if (action.actionType == "PlantCrop" &&
                            cropMatches && cropIdMatches &&
                            (ActionItemMatches(
                                 objective.itemType,
                                 action,
                                 allowTyphoonProtection: false) ||
                             SameCropMaterial(objective.itemType, action.itemType)))
                        {
                            amount += Mathf.Max(
                                1, action.quantity);
                            relevantActions++;
                        }
                        break;
                    case "ImproveHealth":
                    case "ReduceStress":
                        if (cropMatches && cropIdMatches &&
                            IsFarmCareAction(action))
                        {
                            relevantActions++;
                        }
                        break;
                }
            }

            float currentMetric =
                CaptureObjectiveMetric(objective);
            bool completed = false;
            string progress;

            switch (type)
            {
                case "SellCropValue":
                    completed = value >= objective.targetValue;
                    progress = "P" + value + " of P" +
                               objective.targetValue + " sold.";
                    break;
                case "HarvestQuantity":
                    completed = amount >= objective.targetAmount;
                    progress = amount + " of " +
                               objective.targetAmount +
                               " harvest items collected.";
                    break;
                case "WaterCrops":
                    amount = unique.Count;
                    completed = amount >= objective.targetAmount;
                    if (completed &&
                        objective.targetThreshold > 0f)
                    {
                        completed = WateredCropsMeetMoisture(
                            objective,
                            unique,
                            objective.targetAmount);
                    }
                    progress = amount + " of " +
                               objective.targetAmount +
                               " crops watered.";
                    break;
                case "ImproveMoisture":
                    completed =
                        relevantActions > 0 &&
                        currentMetric >=
                        objective.targetThreshold;
                    progress =
                        "Current moisture metric: " +
                        currentMetric.ToString("F1") +
                        "%. Recorded watering actions: " +
                        relevantActions + ".";
                    break;
                case "ReduceCondition":
                case "ReduceFarmSeverity":
                    completed =
                        relevantActions > 0 &&
                        ((objective.targetThreshold > 0f &&
                          currentMetric <=
                          objective.targetThreshold) ||
                         reduction >=
                         objective.requiredReduction ||
                         objective.baselineMetric -
                         currentMetric >=
                         objective.requiredReduction);
                    progress =
                        "Severity reduced by " +
                        Mathf.Max(
                            reduction,
                            objective.baselineMetric -
                            currentMetric).ToString("F1") +
                        " points.";
                    break;
                case "InstallMitigation":
                case "ApplyMaintenance":
                case "PlantQuantity":
                    completed = amount >=
                                objective.targetAmount;
                    progress = amount + " of " +
                               objective.targetAmount +
                               " required actions completed.";
                    break;
                case "ImproveHealth":
                    completed =
                        relevantActions > 0 &&
                        currentMetric -
                        objective.baselineMetric >=
                        objective.requiredReduction;
                    progress = "Average health changed by " +
                               (currentMetric -
                                objective.baselineMetric)
                               .ToString("F1") +
                               " points after " +
                               relevantActions +
                               " recorded care action(s).";
                    break;
                case "ReduceStress":
                    completed =
                        relevantActions > 0 &&
                        objective.baselineMetric -
                        currentMetric >=
                        objective.requiredReduction;
                    progress = "Average stress reduced by " +
                               (objective.baselineMetric -
                                currentMetric)
                               .ToString("F1") +
                               " points after " +
                               relevantActions +
                               " recorded care action(s).";
                    break;
                default:
                    progress =
                        "The objective could not be evaluated locally.";
                    break;
            }

            return new AIAdvisorTaskCheckEnvelope
            {
                completed = completed,
                reason = completed
                    ? "The recorded actions and current farm state meet the measurable task requirement."
                    : "The recorded actions or current farm state do not yet meet the full measurable target.",
                progressSummary = progress,
                tips = completed
                    ? new List<string>()
                    : new List<string>
                    {
                        "Continue the exact action named in the task and avoid unrelated work.",
                        "Check the target crop, quantity, value, severity, moisture, health, or stress before checking again."
                    }
            };
        }

        private static bool SameCropMaterial(string requirement, string plantedItem)
        {
            if (string.IsNullOrWhiteSpace(requirement) ||
                !PlantingMaterialCatalog.TryGet(plantedItem, out PlantingMaterialInfo planted))
            {
                return false;
            }

            foreach (string option in requirement.Split('|'))
            {
                if (PlantingMaterialCatalog.TryGet(option.Trim(), out PlantingMaterialInfo wanted) &&
                    wanted.Crop == planted.Crop)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ActionItemMatches(
            string requirement,
            ClimateActionRecord action,
            bool allowTyphoonProtection)
        {
            if (string.IsNullOrWhiteSpace(requirement))
                return true;
            if (string.Equals(requirement,
                    "DroughtProtection",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (action == null)
                    return false;

                string actionType = action.actionType ?? string.Empty;
                string normalizedItem = NormalizeItem(
                    !string.IsNullOrWhiteSpace(action.itemType)
                        ? action.itemType
                        : action.taskSource);
                string[] allowedItems =
                {
                    "IrrigationSystem",
                    "WaterStorageTank",
                    "Mulch",
                    "ShadeNet",
                    "OrganicCompost",
                    "Greenhouse"
                };
                bool allowedItem = allowedItems.Any(item =>
                    string.Equals(
                        NormalizeItem(item),
                        normalizedItem,
                        StringComparison.OrdinalIgnoreCase));
                bool allowedAction =
                    actionType == "PlaceClimateMitigation" ||
                    actionType == "ApplyCropMaintenance";
                return allowedAction && allowedItem;
            }

            if (allowTyphoonProtection &&
                string.Equals(requirement,
                    "TyphoonProtection",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (action == null)
                    return false;

                string actionType = action.actionType ?? string.Empty;
                string normalizedItem = NormalizeItem(
                    !string.IsNullOrWhiteSpace(action.itemType)
                        ? action.itemType
                        : action.taskSource);

                string[] allowedItems =
                {
                    "Windbreak",
                    "DrainageCanal",
                    "DrainageImprovement",
                    "Greenhouse",
                    "RaisedBed",
                    "SupportStake",
                    "Trellis",
                    "PruningShears"
                };

                bool allowedItem = allowedItems.Any(item =>
                    string.Equals(
                        NormalizeItem(item),
                        normalizedItem,
                        StringComparison.OrdinalIgnoreCase));

                bool allowedAction =
                    actionType == "PlaceClimateMitigation" ||
                    actionType == "ApplyCropMaintenance" ||
                    actionType == "InstallDrainageKit";

                return allowedAction && allowedItem;
            }

            string[] values =
            {
                action != null ? action.itemType : string.Empty,
                action != null ? action.taskSource : string.Empty
            };
            foreach (string option in requirement.Split('|'))
            {
                string expected = NormalizeItem(option);
                foreach (string value in values)
                {
                    if (string.Equals(
                            expected,
                            NormalizeItem(value),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static string NormalizeItem(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            string token = value
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .Replace("Liter", string.Empty)
                .Replace("Kit", string.Empty)
                .Replace("Station", string.Empty)
                .Trim();
            if (string.Equals(token, "BtBioInsecticide",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "Insecticide",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "BtBioSpray";
            }
            if (string.Equals(token, "Drainage",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "DrainageImprovement";
            }
            return token;
        }

        private static bool IsFarmCareAction(
            ClimateActionRecord action)
        {
            if (action == null)
                return false;
            return action.actionType == "WaterCrop" ||
                   action.actionType == "ApplyCropMaintenance" ||
                   action.actionType == "InstallDrainageKit" ||
                   action.actionType == "InstallFruitBag" ||
                   action.actionType == "MitigatePestDisease" ||
                   action.actionType == "SprayCrop" ||
                   action.actionType == "SanitizeCrop" ||
                   action.actionType == "PlaceClimateMitigation";
        }

        private static bool WateredCropsMeetMoisture(
            AIAdvisorTaskObjective objective,
            HashSet<string> wateredCropIds,
            int requiredCount)
        {
            float threshold =
                objective.targetThreshold / 100f;
            int count = CropRuntimeAdapter.FindAll()
                .Count(crop =>
                    wateredCropIds.Contains(crop.CropId) &&
                    (string.IsNullOrWhiteSpace(
                         objective.cropType) ||
                     string.Equals(
                         crop.CropDisplayName,
                         objective.cropType,
                         StringComparison.OrdinalIgnoreCase)) &&
                    crop.Moisture >= threshold);
            return count >= requiredCount;
        }

        private float CaptureObjectiveMetric(
            AIAdvisorTaskObjective objective)
        {
            if (objective == null)
                return 0f;

            switch (objective.type)
            {
                case "ImproveMoisture":
                    return AverageCropMetric(
                        objective,
                        crop => crop.Moisture * 100f);
                case "ReduceCondition":
                    return GetConditionMetric(objective);
                case "ReduceFarmSeverity":
                    return FarmTaskContextBuilder
                        .GetTotalConditionSeverity();
                case "ImproveHealth":
                    return AverageCropMetric(
                        objective,
                        crop => crop.Health);
                case "ReduceStress":
                    return AverageCropMetric(
                        objective,
                        crop => crop.Stress);
                default:
                    return 0f;
            }
        }

        private static float AverageCropMetric(
            AIAdvisorTaskObjective objective,
            Func<CropRuntimeAdapter, float> selector)
        {
            List<CropRuntimeAdapter> crops =
                CropRuntimeAdapter.FindAll()
                    .Where(crop =>
                        (string.IsNullOrWhiteSpace(
                             objective.cropType) ||
                         string.Equals(
                             crop.CropDisplayName,
                             objective.cropType,
                             StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(
                             objective.cropId) ||
                         string.Equals(
                             crop.CropId,
                             objective.cropId,
                             StringComparison.Ordinal)))
                    .ToList();
            return crops.Count == 0
                ? 0f
                : crops.Average(selector);
        }

        private static float GetConditionMetric(
            AIAdvisorTaskObjective objective)
        {
            if (!string.IsNullOrWhiteSpace(
                    objective.conditionType))
            {
                if (!string.IsNullOrWhiteSpace(
                        objective.cropId))
                {
                    return FarmTaskContextBuilder
                        .GetConditionSeverity(
                            objective.cropId,
                            objective.conditionType);
                }

                float total = 0f;
                foreach (PestDiseaseAffectedCrop crop in
                         UnityEngine.Object
                             .FindObjectsByType<
                                 PestDiseaseAffectedCrop>(
                                 FindObjectsSortMode.None))
                {
                    if (crop == null ||
                        (!string.IsNullOrWhiteSpace(
                             objective.cropType) &&
                         !string.Equals(
                             crop.CropDisplayName,
                             objective.cropType,
                             StringComparison.OrdinalIgnoreCase)) ||
                        !Enum.TryParse(
                            objective.conditionType,
                            true,
                            out PestDiseaseType type))
                    {
                        continue;
                    }
                    total += crop.GetConditionSeverity(type);
                }
                return total;
            }
            return FarmTaskContextBuilder
                .GetTotalConditionSeverity();
        }

        private string BuildDisplayText()
        {
            if (!state.active)
            {
                return
                    "No AI Adviser task is active.\n\n" +
                    "Press \"Give me a task!\" and the adviser " +
                    "will examine the current farm, crops, weather, " +
                    "inventory, pests, diseases, and mitigation setup.";
            }

            return
                (string.IsNullOrWhiteSpace(state.dialogue)
                    ? "I reviewed the current farm."
                    : state.dialogue) +
                "\n\nNow... Do " + state.taskText +
                "\n\nLet's say, I'll give you P" +
                state.rewardMoney +
                " if you finish this task." +
                "\n\nRecorded actions: " +
                (state.actionRecords != null
                    ? state.actionRecords.Count
                    : 0);
        }

        private static string CleanText(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private static readonly System.Text.RegularExpressions.Regex EchoedOpening =
            new System.Text.RegularExpressions.Regex(
                @"^\s*(now\s*(\.{2,}|,|:)?\s*)?do\s+|^\s*now\s*(\.{2,}|,|:)\s*",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static readonly System.Text.RegularExpressions.Regex PesoAmount =
            new System.Text.RegularExpressions.Regex(
                @"\bP\s?\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static readonly System.Text.RegularExpressions.Regex PromiseWording =
            new System.Text.RegularExpressions.Regex(
                @"\bgive\s+you\b|\bif\s+you\s+(finish|complete)\b|\bas\s+a\s+reward\b|\byou'?ll\s+(get|earn)\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static string StripBoardWording(string taskText)
        {
            if (string.IsNullOrWhiteSpace(taskText))
                return string.Empty;

            string text = EchoedOpening.Replace(taskText.Trim(), string.Empty, 1);
            text = StripRewardPromise(text);

            if (text.Length > 0 && char.IsUpper(text[0]))
                text = char.ToLowerInvariant(text[0]) + text.Substring(1);

            return text;
        }

        private static string StripRewardPromise(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string[] sentences = System.Text.RegularExpressions.Regex.Split(text, @"(?<=[.!?])\s+");
            List<string> kept = new List<string>(sentences.Length);

            foreach (string sentence in sentences)
            {
                if (PesoAmount.IsMatch(sentence) && PromiseWording.IsMatch(sentence))
                    continue;

                if (!string.IsNullOrWhiteSpace(sentence))
                    kept.Add(sentence.Trim());
            }

            return string.Join(" ", kept).Trim();
        }

        private static readonly System.Text.RegularExpressions.Regex IdParenthetical =
            new System.Text.RegularExpressions.Regex(
                @"\s*[\(\[]\s*(?:crop\s*)?id\s*[:=]?\s*[^)\]]*[\)\]]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static readonly System.Text.RegularExpressions.Regex CropGuid =
            new System.Text.RegularExpressions.Regex(
                @"\b[0-9a-fA-F]{8}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{12}\b");

        private static string PolishVerdictText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            string polished = IdParenthetical.Replace(text, string.Empty);
            polished = CropNaming.Humanize(polished);
            polished = CropGuid.Replace(polished, string.Empty);

            polished = System.Text.RegularExpressions.Regex.Replace(polished, "[ \t]{2,}", " ");
            polished = System.Text.RegularExpressions.Regex.Replace(polished, @"\s+([.,;:!?])", "$1");

            return polished.Trim();
        }

        private static string PolishDialogue(string dialogue)
        {
            return CropNaming.Humanize(StripRewardPromise(CleanText(dialogue)));
        }

        private static string PolishTaskText(string taskText)
        {
            return CropNaming.Humanize(StripBoardWording(CleanText(taskText)));
        }

        private void NotifyChanged()
        {
            OnStateChanged?.Invoke();
        }
    }
}
