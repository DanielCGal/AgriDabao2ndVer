using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AgriDabao3D
{
    public class DailyTaskSystem : MonoBehaviour
    {
        public static DailyTaskSystem Instance { get; private set; }

        [Header("Daily Task Rules")]
        [Range(2, 2)]
        public int tasksPerDay = 2;
        public bool logDebug = true;

        public event Action OnStateChanged;

        public IReadOnlyList<DailyTaskInstance> Tasks => state.tasks;
        public int RewardMoney => state.rewardMoney;
        public bool RewardClaimed => state.rewardClaimed;
        public bool AllCompleted =>
            state.tasks != null &&
            state.tasks.Count == 2 &&
            state.tasks.TrueForAll(task => task != null && task.completed);
        public bool CanCollectReward => AllCompleted && !state.rewardClaimed;
        public bool CanSkipDay => AllCompleted;

        public int DaySkipCount { get; private set; }

        private DailyTaskSystemSaveDto state = new DailyTaskSystemSaveDto();
        private bool restored;
        private float nextStateEvaluation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private IEnumerator Start()
        {
            while (FarmPersistenceManager.Instance != null &&
                   !FarmPersistenceManager.Instance.IsWorldReady)
            {
                yield return null;
            }

            SubscribeToTime();
            if (!restored)
                EnsureTasksForCurrentDay();
        }

        private void OnEnable()
        {
            SubscribeToTime();
        }

        private void OnDisable()
        {
            if (GameTimeSystem.Instance != null)
                GameTimeSystem.Instance.OnNewGameDay -= HandleNewGameDay;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextStateEvaluation)
                return;
            nextStateEvaluation = Time.unscaledTime + 0.5f;

            bool changed = false;
            foreach (DailyTaskInstance task in state.tasks)
            {
                if (task == null || task.completed)
                    continue;
                bool before = task.completed;
                EvaluateTaskFromState(task);
                changed |= before != task.completed;
            }

            if (changed)
                NotifyChanged();
        }

        private void SubscribeToTime()
        {
            if (GameTimeSystem.Instance == null)
                return;
            GameTimeSystem.Instance.OnNewGameDay -= HandleNewGameDay;
            GameTimeSystem.Instance.OnNewGameDay += HandleNewGameDay;
        }

        private void HandleNewGameDay()
        {
            GenerateDailyTasks();
        }

        public void EnsureTasksForCurrentDay()
        {
            int currentDay = GetAbsoluteDay();
            if (state.tasks == null)
                state.tasks = new List<DailyTaskInstance>();

            if (state.assignedAbsoluteDay != currentDay ||
                state.tasks.Count != 2)
            {
                GenerateDailyTasks();
            }
            else
            {
                NotifyChanged();
            }
        }

        public void GenerateDailyTasks()
        {
            state = new DailyTaskSystemSaveDto
            {
                assignedAbsoluteDay = GetAbsoluteDay(),
                rewardClaimed = false,
                skipUsed = false,
                tasks = new List<DailyTaskInstance>()
            };

            if (!TutorialState.Completed)
            {
                state.tasks.Add(CreateInstance(CreateEmergencyDigTask()));
                state.tasks.Add(CreateInstance(CreateEmergencyPlantTask()));
                state.rewardMoney = 300;

                foreach (DailyTaskInstance task in state.tasks)
                    EvaluateTaskFromState(task);

                if (logDebug)
                {
                    Debug.Log("[DailyTasks] Day " + state.assignedAbsoluteDay +
                              " pinned for the beginner guide: " +
                              string.Join(" | ", state.tasks.Select(task => task.title)) +
                              " | Reward=P" + state.rewardMoney);
                }

                NotifyChanged();
                return;
            }

            List<DailyTaskTemplate> eligible =
                DailyTaskCatalog.GetAll()
                    .Where(IsEligible)
                    .ToList();

            if (eligible.Count == 0)
            {
                eligible.Add(CreateEmergencyDigTask());
                if (CanPlantAny(string.Empty))
                    eligible.Add(CreateEmergencyPlantTask());
                else
                    eligible.Add(CreateEmergencyDigTwoTask());
            }

            DailyTaskTemplate first = ChooseTemplate(eligible, null, true);
            if (first != null)
                state.tasks.Add(CreateInstance(first));

            DailyTaskTemplate second = ChooseTemplate(
                eligible,
                first,
                false);
            if (second == null)
            {
                second = CanPlantAny(string.Empty)
                    ? CreateEmergencyPlantTask()
                    : CreateEmergencyDigTwoTask();
            }
            state.tasks.Add(CreateInstance(second));

            while (state.tasks.Count < 2)
            {
                state.tasks.Add(CreateInstance(
                    CanPlantAny(string.Empty)
                        ? CreateEmergencyPlantTask()
                        : CreateEmergencyDigTwoTask()));
            }

            int difficultyScore = state.tasks.Sum(task =>
                ParseDifficulty(task.difficulty) == DailyTaskDifficulty.Hard ? 3 :
                ParseDifficulty(task.difficulty) == DailyTaskDifficulty.Medium ? 2 : 1);

            bool urgent = state.tasks.Any(task =>
                !string.IsNullOrWhiteSpace(task.requiredWeather) ||
                !string.IsNullOrWhiteSpace(task.conditionType) ||
                ParseKind(task.kind) == DailyTaskKind.ReduceFarmConditionSeverity);

            state.rewardMoney = urgent || difficultyScore >= 5
                ? 500
                : difficultyScore >= 4
                    ? 400
                    : 300;

            foreach (DailyTaskInstance task in state.tasks)
                EvaluateTaskFromState(task);

            if (logDebug)
            {
                Debug.Log(
                    "[DailyTasks] Generated day " +
                    state.assignedAbsoluteDay + " tasks: " +
                    string.Join(" | ", state.tasks.Select(task => task.title)) +
                    " | Reward=P" + state.rewardMoney);
            }

            NotifyChanged();
        }

        private DailyTaskTemplate ChooseTemplate(
            List<DailyTaskTemplate> eligible,
            DailyTaskTemplate excluded,
            bool preferUrgent)
        {
            List<DailyTaskTemplate> pool = eligible
                .Where(candidate =>
                    excluded == null ||
                    !SameObjective(candidate, excluded))
                .ToList();

            if (pool.Count == 0)
                return null;

            if (preferUrgent)
            {
                List<DailyTaskTemplate> urgent = pool
                    .Where(candidate =>
                        !string.IsNullOrWhiteSpace(candidate.requiredWeather) ||
                        candidate.requiresActiveCondition)
                    .ToList();
                if (urgent.Count > 0)
                    pool = urgent;
            }

            int totalWeight = 0;
            foreach (DailyTaskTemplate candidate in pool)
            {
                int weight = candidate.difficulty == DailyTaskDifficulty.Hard
                    ? 2
                    : candidate.difficulty == DailyTaskDifficulty.Medium
                        ? 3
                        : 4;
                if (!string.IsNullOrWhiteSpace(candidate.requiredWeather))
                    weight += 5;
                if (candidate.requiresActiveCondition)
                    weight += 6;
                totalWeight += weight;
            }

            int roll = UnityEngine.Random.Range(0, Mathf.Max(1, totalWeight));
            foreach (DailyTaskTemplate candidate in pool)
            {
                int weight = candidate.difficulty == DailyTaskDifficulty.Hard
                    ? 2
                    : candidate.difficulty == DailyTaskDifficulty.Medium
                        ? 3
                        : 4;
                if (!string.IsNullOrWhiteSpace(candidate.requiredWeather))
                    weight += 5;
                if (candidate.requiresActiveCondition)
                    weight += 6;
                if (roll < weight)
                    return candidate;
                roll -= weight;
            }
            return pool[0];
        }

        private static bool SameObjective(
            DailyTaskTemplate first,
            DailyTaskTemplate second)
        {
            if (first == null || second == null)
                return false;
            return first.kind == second.kind &&
                   string.Equals(first.cropType, second.cropType,
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(first.conditionType, second.conditionType,
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(first.maintenanceAction, second.maintenanceAction,
                       StringComparison.OrdinalIgnoreCase);
        }

        private DailyTaskInstance CreateInstance(DailyTaskTemplate template)
        {
            DailyTaskInstance task = new DailyTaskInstance
            {
                instanceId = Guid.NewGuid().ToString("N"),
                templateId = template.id,
                title = template.title,
                description = template.description,
                kind = template.kind.ToString(),
                difficulty = template.difficulty.ToString(),
                cropType = template.cropType,
                itemType = template.itemType,
                maintenanceAction = template.maintenanceAction,
                requiredWeather = template.requiredWeather,
                conditionType = template.conditionType,
                requiredStage = template.requiredStage,
                eligibilityThreshold = template.eligibilityThreshold,
                targetAmount = Mathf.Max(1, template.targetAmount),
                targetValue = Mathf.Max(0, template.targetValue),
                targetThreshold = template.targetThreshold,
                requiredReduction = template.requiredReduction,
                assignedGameDay = GameTimeSystem.Instance != null
                    ? GameTimeSystem.Instance.TotalGameDays
                    : 0f
            };

            List<CropRuntimeAdapter> candidateCrops =
                FindEligibleCrops(template);
            CropRuntimeAdapter selectedTarget = null;
            if (candidateCrops.Count > 0 &&
                (template.targetAmount <= 1 ||
                 !string.IsNullOrWhiteSpace(template.cropType) ||
                 !string.IsNullOrWhiteSpace(template.conditionType)))
            {
                selectedTarget = template.id == 196
                    ? candidateCrops
                        .OrderByDescending(GetHighestOwnedCompatibleConditionSeverity)
                        .First()
                    : candidateCrops[
                        UnityEngine.Random.Range(0, candidateCrops.Count)];
                task.cropId = selectedTarget.CropId;
                if (string.IsNullOrWhiteSpace(task.cropType))
                    task.cropType = selectedTarget.CropDisplayName;

                BindConditionAndOwnedMitigations(
                    task,
                    template,
                    selectedTarget);
            }

            if (template.kind == DailyTaskKind.WaterCrop &&
                candidateCrops.Count > 0)
            {
                task.targetAmount = Mathf.Clamp(
                    task.targetAmount, 1, candidateCrops.Count);
            }

            if (template.kind == DailyTaskKind.MitigateCondition ||
                template.kind == DailyTaskKind.ReduceAnyPestSeverity ||
                template.kind == DailyTaskKind.ReduceAnyDiseaseSeverity)
            {
                task.baselineMetric = GetRelevantSeverity(task);
                task.currentMetric = task.baselineMetric;
            }
            else if (template.kind ==
                     DailyTaskKind.ReduceFarmConditionSeverity)
            {
                task.baselineMetric =
                    FarmTaskContextBuilder.GetTotalConditionSeverity();
                task.currentMetric = task.baselineMetric;
            }
            else if (template.kind == DailyTaskKind.PlaceWorldMitigation)
            {
                task.baselineMetric =
                    CountWorldMitigation(task.maintenanceAction);
            }
            else if (template.kind == DailyTaskKind.RaiseAverageHealth)
            {
                task.baselineMetric = AverageHealth();
                task.currentMetric = task.baselineMetric;
            }
            else if (template.kind == DailyTaskKind.LowerAverageStress)
            {
                task.baselineMetric = AverageStress(true);
                task.currentMetric = task.baselineMetric;
            }

            return task;
        }

        private bool IsEligible(DailyTaskTemplate template)
        {
            if (template == null)
                return false;

            WeatherEventType currentWeather =
                WeatherSystem.Instance != null
                    ? WeatherSystem.Instance.currentEvent
                    : WeatherEventType.Clear;
            if (!string.IsNullOrWhiteSpace(template.requiredWeather) &&
                !string.Equals(
                    template.requiredWeather,
                    currentWeather.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!CanObtainRequiredItems(template))
                return false;

            if (template.requiresActiveCondition &&
                !HasAnyActiveCondition(
                    template.cropType,
                    template.conditionType))
            {
                return false;
            }

            List<CropRuntimeAdapter> crops = FindEligibleCrops(template);

            switch (template.kind)
            {
                case DailyTaskKind.WaterCrop:
                case DailyTaskKind.HarvestCrop:
                case DailyTaskKind.ApplyMaintenance:
                case DailyTaskKind.RaiseCropMoisture:
                case DailyTaskKind.MitigateCondition:
                case DailyTaskKind.RemoveInfectedPlant:
                    return crops.Count >= Mathf.Max(1, template.targetAmount);

                case DailyTaskKind.PlantCrop:
                    return CanPlant(
                        template.cropType,
                        Mathf.Max(1, template.targetAmount));
                case DailyTaskKind.PlantAnyCrop:
                    return CanPlantAny(
                        template.itemType,
                        Mathf.Max(1, template.targetAmount));
                case DailyTaskKind.DigPlantingSpot:
                case DailyTaskKind.TillGround:
                    return HasInventoryItem("Shovel");
                case DailyTaskKind.SowSeedlingBag:
                    return PlantingAvailability.CountFreeBags() >= Mathf.Max(1, template.targetAmount) &&
                           PlantingAvailability.CountNurseryMaterialsHeld() >= Mathf.Max(1, template.targetAmount);
                case DailyTaskKind.TransplantSeedling:
                    return PlantingAvailability.CountTentSeedlingsPlantableToday() >=
                           Mathf.Max(1, template.targetAmount);
                case DailyTaskKind.CollectHarvest:
                    return CropRuntimeAdapter.FindAll()
                        .Sum(crop =>
                            FarmTaskContextBuilder
                                .GetAvailableHarvest(crop)) >=
                           Mathf.Max(1, template.targetAmount);
                case DailyTaskKind.SellCropQuantity:
                    return HasSellableInventory(
                        template.cropType,
                        Mathf.Max(1, template.targetAmount));
                case DailyTaskKind.SellCropValue:
                    return HasSellableInventory(
                        template.cropType, 1);
                case DailyTaskKind.PlaceWorldMitigation:
                    return CropRuntimeAdapter.FindAll().Count > 0;
                case DailyTaskKind.PlacePestTrap:
                    return HasCompatibleActiveCondition(
                        template.cropType,
                        template.conditionType,
                        template.itemType);
                case DailyTaskKind.CleanTrap:
                    return HasFullMatchingTrap(template.itemType);
                case DailyTaskKind.ReduceAnyPestSeverity:
                case DailyTaskKind.ReduceAnyDiseaseSeverity:
                    return crops.Count >= Mathf.Max(1, template.targetAmount);
                case DailyTaskKind.ReduceFarmConditionSeverity:
                    return FarmTaskContextBuilder.GetTotalConditionSeverity() >
                           Mathf.Max(1f, template.requiredReduction);
                case DailyTaskKind.RaiseAverageHealth:
                    return CropRuntimeAdapter.FindAll().Count > 0;
                case DailyTaskKind.LowerAverageStress:
                    return CropRuntimeAdapter.FindAll().Any(crop =>
                    {
                        if (crop == null || crop.Root == null ||
                            crop.Stress <= 5f)
                        {
                            return false;
                        }
                        PestDiseaseAffectedCrop affected =
                            crop.Root.GetComponent<PestDiseaseAffectedCrop>();
                        return affected != null &&
                               affected.HasAnyPestOrDisease() &&
                               HasPracticalCareAction(crop);
                    });
                default:
                    return true;
            }
        }

        private List<CropRuntimeAdapter> FindEligibleCrops(
            DailyTaskTemplate template)
        {
            List<CropRuntimeAdapter> result = new List<CropRuntimeAdapter>();
            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                if (crop == null)
                    continue;
                if (!string.IsNullOrWhiteSpace(template.cropType) &&
                    !string.Equals(
                        crop.CropDisplayName,
                        template.cropType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(template.requiredStage) &&
                    !AlternativeMatches(
                        template.requiredStage,
                        crop.Stage))
                {
                    continue;
                }

                switch (template.kind)
                {
                    case DailyTaskKind.WaterCrop:
                    case DailyTaskKind.RaiseCropMoisture:
                    {
                        float limit = template.eligibilityThreshold > 0f
                            ? template.eligibilityThreshold
                            : template.targetThreshold > 0f
                                ? template.targetThreshold
                                : 0.60f;
                        if (crop.Moisture >= limit)
                            continue;
                        break;
                    }
                    case DailyTaskKind.HarvestCrop:
                        if (!FarmTaskContextBuilder.IsHarvestReady(crop))
                            continue;
                        break;
                    case DailyTaskKind.ApplyMaintenance:
                        if (!CanApplyMaintenance(crop, template))
                            continue;
                        break;
                    case DailyTaskKind.MitigateCondition:
                        if (!CropHasCondition(crop, template.conditionType,
                                template.targetThreshold) ||
                            (!string.IsNullOrWhiteSpace(template.itemType)
                                ? !CropHasCompatibleMitigation(
                                    crop,
                                    template.conditionType,
                                    template.itemType)
                                : !CropHasOwnedCompatibleMitigation(
                                    crop,
                                    template.conditionType,
                                    diseasesOnly: false,
                                    pestsOnly: false)))
                        {
                            continue;
                        }
                        break;
                    case DailyTaskKind.RemoveInfectedPlant:
                        if (!CropHasCondition(crop, template.conditionType,
                                template.targetThreshold))
                            continue;
                        break;
                    case DailyTaskKind.ReduceAnyPestSeverity:
                        if (!CropHasConditionCategory(
                                crop,
                                PestDiseaseCategory.Pest,
                                diseasesOnly: false) ||
                            !CropHasOwnedCompatibleMitigation(
                                crop,
                                string.Empty,
                                diseasesOnly: false,
                                pestsOnly: true))
                        {
                            continue;
                        }
                        break;
                    case DailyTaskKind.ReduceAnyDiseaseSeverity:
                        if (!CropHasConditionCategory(
                                crop,
                                PestDiseaseCategory.Pest,
                                diseasesOnly: true) ||
                            !CropHasOwnedCompatibleMitigation(
                                crop,
                                string.Empty,
                                diseasesOnly: true,
                                pestsOnly: false))
                        {
                            continue;
                        }
                        break;
                }

                result.Add(crop);
            }
            return result;
        }

        private bool CanApplyMaintenance(
            CropRuntimeAdapter crop,
            DailyTaskTemplate template)
        {
            if (crop == null || crop.Root == null)
                return false;

            string action = template.maintenanceAction ?? string.Empty;
            if (action == "OrganicCompost" &&
                template.eligibilityThreshold > 0f &&
                crop.Fertility >= template.eligibilityThreshold)
            {
                return false;
            }
            if (action == "Prune" &&
                template.eligibilityThreshold > 0f &&
                crop.Stress < template.eligibilityThreshold)
            {
                return false;
            }
            if (action == "FruitBag" ||
                action == "DrainageImprovement")
            {
                PestDiseaseAffectedCrop affected =
                    crop.Root.GetComponent<PestDiseaseAffectedCrop>();
                if (affected == null || !affected.HasAnyPestOrDisease() ||
                    !CropHasCompatibleMitigation(crop, string.Empty, action))
                {
                    return false;
                }
                CropProtectionState protection =
                    crop.Root.GetComponent<CropProtectionState>();
                if (action == "FruitBag")
                    return protection == null || !protection.hasFruitBag;
                return protection == null ||
                       !protection.hasDrainageImprovement;
            }

            if (action == "SupportStakeOrTrellis")
            {
                CropClimateMaintenanceState state =
                    crop.Root.GetComponent<CropClimateMaintenanceState>();
                bool canStake =
                    CropMaintenanceCatalog.IsAllowed(
                        crop.CropType,
                        CropMaintenanceActionType.SupportStake) &&
                    (state == null || !state.hasSupportStake);
                bool canTrellis =
                    CropMaintenanceCatalog.IsAllowed(
                        crop.CropType,
                        CropMaintenanceActionType.Trellis) &&
                    (state == null || !state.hasTrellis);
                return canStake || canTrellis;
            }

            if (!Enum.TryParse(action, true,
                    out CropMaintenanceActionType maintenanceAction))
            {
                return false;
            }

            if (!CropMaintenanceCatalog.IsAllowed(
                    crop.CropType,
                    maintenanceAction))
            {
                return false;
            }

            CropClimateMaintenanceState current =
                crop.Root.GetComponent<CropClimateMaintenanceState>();
            float now = GameTimeSystem.Instance != null
                ? GameTimeSystem.Instance.TotalGameDays
                : 0f;
            if (current == null)
                return true;

            switch (maintenanceAction)
            {
                case CropMaintenanceActionType.Mulch:
                    return !current.hasMulch;
                case CropMaintenanceActionType.SupportStake:
                    return !current.hasSupportStake;
                case CropMaintenanceActionType.Trellis:
                    return !current.hasTrellis;
                case CropMaintenanceActionType.RaisedBed:
                    return !current.hasRaisedBed;
                case CropMaintenanceActionType.OrganicCompost:
                    return now - current.lastCompostGameDay >= 12f;
                case CropMaintenanceActionType.Prune:
                    return now - current.lastPrunedGameDay >= 15f;
                default:
                    return false;
            }
        }

        public void RecordAction(ClimateActionRecord record)
        {
            if (record == null || !record.actionSucceeded ||
                state.tasks == null || state.tasks.Count == 0)
            {
                return;
            }

            bool changed = false;
            foreach (DailyTaskInstance task in state.tasks)
            {
                if (task == null || task.completed)
                    continue;
                int beforeAmount = task.progressAmount;
                int beforeValue = task.progressValue;
                bool beforeCompleted = task.completed;
                ApplyActionProgress(task, record);
                EvaluateTaskFromState(task);
                changed |= beforeAmount != task.progressAmount ||
                           beforeValue != task.progressValue ||
                           beforeCompleted != task.completed;
            }

            if (changed)
                NotifyChanged();
        }

        private void ApplyActionProgress(
            DailyTaskInstance task,
            ClimateActionRecord record)
        {
            DailyTaskKind kind = ParseKind(task.kind);
            bool cropMatches =
                string.IsNullOrWhiteSpace(task.cropType) ||
                string.Equals(task.cropType, record.cropType,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(task.cropType, record.itemType,
                    StringComparison.OrdinalIgnoreCase);
            bool cropIdMatches =
                string.IsNullOrWhiteSpace(task.cropId) ||
                string.Equals(task.cropId, record.cropId,
                    StringComparison.Ordinal);
            int quantity = Mathf.Max(1, record.quantity);

            switch (kind)
            {
                case DailyTaskKind.WaterCrop:
                case DailyTaskKind.RaiseCropMoisture:
                    if (record.actionType == "WaterCrop" &&
                        cropMatches && cropIdMatches)
                    {
                        AddUniqueProgress(task,
                            record.cropId, quantity);
                    }
                    break;

                case DailyTaskKind.HarvestCrop:
                    if (record.actionType == "HarvestCrop" &&
                        cropMatches && cropIdMatches)
                    {
                        AddUniqueProgress(task,
                            record.cropId + "|" + record.gameDay,
                            quantity);
                    }
                    break;

                case DailyTaskKind.ApplyMaintenance:
                    if (IsMaintenanceRecord(record) &&
                        cropMatches && cropIdMatches &&
                        MaintenanceMatches(task, record))
                    {
                        AddUniqueProgress(task,
                            record.cropId + "|" +
                            (record.itemType ?? record.actionType),
                            quantity);
                    }
                    break;

                case DailyTaskKind.PlantCrop:
                    if (record.actionType == "PlantCrop" &&
                        cropMatches)
                    {
                        task.progressAmount += quantity;
                    }
                    break;

                case DailyTaskKind.PlantAnyCrop:
                    if (record.actionType == "PlantCrop" &&
                        (string.IsNullOrWhiteSpace(task.itemType) ||
                         AlternativeMatches(task.itemType, record.itemType)))
                    {
                        task.progressAmount += quantity;
                    }
                    break;

                case DailyTaskKind.DigPlantingSpot:
                    if (record.actionType == "DigPlantingSpot")
                        task.progressAmount += quantity;
                    break;

                case DailyTaskKind.TillGround:
                    if (record.actionType == "TillGround")
                        task.progressAmount += quantity;
                    break;

                case DailyTaskKind.SowSeedlingBag:
                    if (record.actionType == "SowSeedlingBag" &&
                        (string.IsNullOrWhiteSpace(task.itemType) ||
                         AlternativeMatches(task.itemType, record.itemType)))
                    {
                        task.progressAmount += quantity;
                    }
                    break;

                case DailyTaskKind.TransplantSeedling:
                    if (record.actionType == "TransplantSeedling" && cropMatches)
                        task.progressAmount += quantity;
                    break;

                case DailyTaskKind.CollectHarvest:
                    if (record.actionType == "CollectHarvest")
                        task.progressAmount += quantity;
                    break;

                case DailyTaskKind.SellCropQuantity:
                    if (record.actionType == "SellCrop" &&
                        cropMatches)
                    {
                        task.progressAmount += quantity;
                    }
                    break;

                case DailyTaskKind.SellCropValue:
                    if (record.actionType == "SellCrop" &&
                        cropMatches)
                    {
                        task.progressValue += Mathf.Max(
                            0, record.moneyDelta);
                    }
                    break;

                case DailyTaskKind.PlaceWorldMitigation:
                    if (record.actionType == "PlaceClimateMitigation" &&
                        AlternativeMatches(
                            task.maintenanceAction,
                            record.itemType))
                    {
                        task.progressAmount += quantity;
                    }
                    break;

                case DailyTaskKind.PlacePestTrap:
                    if ((record.actionType == "PlaceAphidTrap" ||
                         record.actionType == "PlacePestMitigationTrap") &&
                        AlternativeMatches(task.itemType, record.itemType))
                    {
                        task.progressAmount += quantity;
                    }
                    break;

                case DailyTaskKind.CleanTrap:
                    if (record.actionType == "CleanTrap" &&
                        AlternativeMatches(task.itemType, record.itemType))
                    {
                        task.progressAmount += quantity;
                    }
                    break;

                case DailyTaskKind.MitigateCondition:
                case DailyTaskKind.ReduceAnyPestSeverity:
                case DailyTaskKind.ReduceAnyDiseaseSeverity:
                {
                    bool directMitigation =
                        IsMitigationRecord(record) &&
                        cropMatches && cropIdMatches &&
                        (string.IsNullOrWhiteSpace(task.conditionType) ||
                         string.Equals(task.conditionType,
                             record.conditionType,
                             StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(task.itemType) ||
                         AlternativeMatches(task.itemType,
                             record.itemType));
                    bool compatibleTrapPlacement =
                        (record.actionType == "PlaceAphidTrap" ||
                         record.actionType == "PlacePestMitigationTrap") &&
                        AlternativeMatches(task.itemType, record.itemType);

                    if (directMitigation || compatibleTrapPlacement)
                    {
                        AddUniqueProgress(task,
                            (record.cropId ?? string.Empty) + "|" +
                            (record.conditionType ?? record.itemType) + "|" +
                            record.actionType,
                            quantity);
                    }
                    break;
                }

                case DailyTaskKind.ReduceFarmConditionSeverity:
                    if (IsMitigationRecord(record) &&
                        record.beforeSeverity > record.afterSeverity)
                    {
                        task.progressAmount += quantity;
                    }
                    break;

                case DailyTaskKind.RaiseAverageHealth:
                case DailyTaskKind.LowerAverageStress:
                    if (IsFarmCareRecord(record))
                    {
                        task.progressAmount += quantity;
                    }
                    break;

                case DailyTaskKind.RemoveInfectedPlant:
                    if (record.actionType == "RemoveInfectedPlant" &&
                        cropMatches &&
                        (string.IsNullOrWhiteSpace(task.conditionType) ||
                         string.Equals(task.conditionType,
                             record.conditionType,
                             StringComparison.OrdinalIgnoreCase)))
                    {
                        task.progressAmount += quantity;
                    }
                    break;
            }

            if (task.progressAmount >= task.targetAmount &&
                kind != DailyTaskKind.WaterCrop &&
                kind != DailyTaskKind.RaiseCropMoisture &&
                kind != DailyTaskKind.SellCropValue &&
                kind != DailyTaskKind.RaiseAverageHealth &&
                kind != DailyTaskKind.LowerAverageStress &&
                kind != DailyTaskKind.ReduceFarmConditionSeverity &&
                kind != DailyTaskKind.MitigateCondition &&
                kind != DailyTaskKind.ReduceAnyPestSeverity &&
                kind != DailyTaskKind.ReduceAnyDiseaseSeverity)
            {
                task.completed = true;
            }

            if (kind == DailyTaskKind.SellCropValue &&
                task.progressValue >= task.targetValue)
            {
                task.completed = true;
            }
        }

        private void EvaluateTaskFromState(DailyTaskInstance task)
        {
            if (task == null || task.completed)
                return;

            DailyTaskKind kind = ParseKind(task.kind);
            switch (kind)
            {
                case DailyTaskKind.WaterCrop:
                case DailyTaskKind.RaiseCropMoisture:
                    if (!string.IsNullOrWhiteSpace(task.cropId))
                    {
                        CropRuntimeAdapter crop = FindCrop(task.cropId);
                        if (crop != null)
                        {
                            task.currentMetric = crop.Moisture;
                            if (task.progressAmount >= task.targetAmount &&
                                crop.Moisture >= Mathf.Max(
                                    0.50f, task.targetThreshold))
                            {
                                task.completed = true;
                            }
                        }
                    }
                    break;

                case DailyTaskKind.ApplyMaintenance:
                    if (!string.IsNullOrWhiteSpace(task.cropId))
                    {
                        CropRuntimeAdapter crop = FindCrop(task.cropId);
                        if (crop != null &&
                            IsMaintenancePresent(
                                crop, task.maintenanceAction))
                        {
                            task.completed = true;
                        }
                    }
                    break;

                case DailyTaskKind.PlaceWorldMitigation:
                {
                    float current =
                        CountWorldMitigation(task.maintenanceAction);
                    task.currentMetric = current;
                    if (current > task.baselineMetric)
                        task.completed = true;
                    break;
                }

                case DailyTaskKind.MitigateCondition:
                case DailyTaskKind.ReduceAnyPestSeverity:
                case DailyTaskKind.ReduceAnyDiseaseSeverity:
                {
                    float current = GetRelevantSeverity(task);
                    task.currentMetric = current;
                    float reduction = task.baselineMetric - current;
                    bool thresholdMet =
                        task.targetThreshold > 0f &&
                        current <= task.targetThreshold;
                    bool reductionMet =
                        task.requiredReduction > 0f &&
                        reduction >= task.requiredReduction;
                    bool conditionCleared =
                        current <= 0.01f &&
                        task.baselineMetric > 0.01f;
                    if ((thresholdMet || reductionMet ||
                         conditionCleared) &&
                        task.progressAmount >=
                        Mathf.Max(1, task.targetAmount))
                    {
                        task.completed = true;
                    }
                    break;
                }

                case DailyTaskKind.ReduceFarmConditionSeverity:
                {
                    float current =
                        FarmTaskContextBuilder.GetTotalConditionSeverity();
                    task.currentMetric = current;
                    if (task.progressAmount > 0 &&
                        task.baselineMetric - current >=
                        task.requiredReduction)
                    {
                        task.completed = true;
                    }
                    break;
                }

                case DailyTaskKind.RaiseAverageHealth:
                {
                    float current = AverageHealth();
                    task.currentMetric = current;
                    if (task.progressAmount > 0 &&
                        current - task.baselineMetric >=
                        task.requiredReduction)
                    {
                        task.completed = true;
                    }
                    break;
                }

                case DailyTaskKind.LowerAverageStress:
                {
                    float current = AverageStress(true);
                    task.currentMetric = current;
                    if (task.progressAmount > 0 &&
                        task.baselineMetric - current >=
                        task.requiredReduction)
                    {
                        task.completed = true;
                    }
                    break;
                }
            }

            if (task.progressAmount >= task.targetAmount &&
                kind != DailyTaskKind.WaterCrop &&
                kind != DailyTaskKind.RaiseCropMoisture &&
                kind != DailyTaskKind.RaiseAverageHealth &&
                kind != DailyTaskKind.LowerAverageStress &&
                kind != DailyTaskKind.ReduceFarmConditionSeverity &&
                kind != DailyTaskKind.MitigateCondition &&
                kind != DailyTaskKind.ReduceAnyPestSeverity &&
                kind != DailyTaskKind.ReduceAnyDiseaseSeverity)
            {
                task.completed = true;
            }
        }

        public void CollectReward()
        {
            if (!CanCollectReward)
                return;
            if (PlayerInventory.Instance == null)
                return;
            PlayerInventory.Instance.AddMoney(state.rewardMoney);
            GameAudioManager.Instance.PlayReward();
            state.rewardClaimed = true;
            FarmTaskPopupUI.Instance?.Show(
                "Daily Tasks Completed",
                "You collected P" + state.rewardMoney +
                " for completing both daily tasks.");
            NotifyChanged();
        }

        public void SkipToNextDayAtEight()
        {
            if (!CanSkipDay || GameTimeSystem.Instance == null)
                return;

            if (CanCollectReward)
                CollectReward();

            state.skipUsed = true;
            DaySkipCount++;
            float currentFraction =
                Mathf.Repeat(GameTimeSystem.Instance.TotalGameDays, 1f);
            float daysToNextEight =
                (1f - currentFraction) + (8f / 24f);
            GameTimeSystem.Instance.AdvanceDays(daysToNextEight);
            NotifyChanged();
        }

        public DailyTaskSystemSaveDto CaptureSaveData()
        {
            return new DailyTaskSystemSaveDto
            {
                assignedAbsoluteDay = state.assignedAbsoluteDay,
                rewardMoney = state.rewardMoney,
                rewardClaimed = state.rewardClaimed,
                skipUsed = state.skipUsed,
                tasks = state.tasks != null
                    ? new List<DailyTaskInstance>(state.tasks)
                    : new List<DailyTaskInstance>()
            };
        }

        public void RestoreSaveData(DailyTaskSystemSaveDto save)
        {
            restored = true;
            state = save ?? new DailyTaskSystemSaveDto();
            if (state.tasks == null)
                state.tasks = new List<DailyTaskInstance>();

            if (state.tasks.Count != 2 ||
                state.assignedAbsoluteDay != GetAbsoluteDay())
            {
                GenerateDailyTasks();
                return;
            }

            foreach (DailyTaskInstance task in state.tasks)
            {
                if (task.progressKeys == null)
                    task.progressKeys = new List<string>();

                task.itemType = PlantingMaterialCatalog.UpgradeLegacyList(task.itemType);
                EvaluateTaskFromState(task);
            }
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnStateChanged?.Invoke();
        }

        private static int GetAbsoluteDay()
        {
            return GameTimeSystem.Instance != null
                ? Mathf.FloorToInt(GameTimeSystem.Instance.TotalGameDays) + 1
                : 1;
        }

        private static DailyTaskKind ParseKind(string value)
        {
            return Enum.TryParse(value, true, out DailyTaskKind result)
                ? result
                : DailyTaskKind.None;
        }

        private static DailyTaskDifficulty ParseDifficulty(string value)
        {
            return Enum.TryParse(value, true,
                out DailyTaskDifficulty result)
                ? result
                : DailyTaskDifficulty.Easy;
        }

        private static void AddUniqueProgress(
            DailyTaskInstance task,
            string key,
            int amount)
        {
            if (task.progressKeys == null)
                task.progressKeys = new List<string>();
            string normalized = string.IsNullOrWhiteSpace(key)
                ? Guid.NewGuid().ToString("N")
                : key;
            if (task.progressKeys.Contains(normalized))
                return;
            task.progressKeys.Add(normalized);
            task.progressAmount += Mathf.Max(1, amount);
        }

        private static bool IsMaintenanceRecord(
            ClimateActionRecord record)
        {
            return record.actionType == "ApplyCropMaintenance" ||
                   record.actionType == "InstallFruitBag" ||
                   record.actionType == "InstallDrainageKit";
        }

        private static bool IsMitigationRecord(
            ClimateActionRecord record)
        {
            return record.actionType == "MitigatePestDisease" ||
                   record.actionType == "SprayCrop" ||
                   record.actionType == "InstallFruitBag" ||
                   record.actionType == "InstallDrainageKit" ||
                   record.actionType == "SanitizeCrop" ||
                   record.actionType == "RemoveInfectedPlant";
        }

        private static bool IsFarmCareRecord(
            ClimateActionRecord record)
        {
            if (record == null)
                return false;
            return record.actionType == "WaterCrop" ||
                   record.actionType == "ApplyCropMaintenance" ||
                   record.actionType == "InstallFruitBag" ||
                   record.actionType == "InstallDrainageKit" ||
                   record.actionType == "MitigatePestDisease" ||
                   record.actionType == "SprayCrop" ||
                   record.actionType == "SanitizeCrop" ||
                   record.actionType == "PlaceClimateMitigation";
        }

        private static bool MaintenanceMatches(
            DailyTaskInstance task,
            ClimateActionRecord record)
        {
            if (task.maintenanceAction == "SupportStakeOrTrellis")
            {
                return record.itemType == "SupportStake" ||
                       record.itemType == "SupportStakeKit" ||
                       record.itemType == "Trellis" ||
                       record.itemType == "TrellisKit";
            }

            return AlternativeMatches(
                       task.maintenanceAction,
                       record.itemType) ||
                   AlternativeMatches(
                       task.itemType,
                       record.itemType);
        }

        private static bool AlternativeMatches(
            string alternatives,
            string value)
        {
            if (string.IsNullOrWhiteSpace(alternatives))
                return true;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            foreach (string option in alternatives.Split('|'))
            {
                if (string.Equals(
                        NormalizeToken(option),
                        NormalizeToken(value),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string NormalizeToken(string value)
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
                .Replace("Sprayer", string.Empty)
                .Trim();

            if (string.Equals(token, "BtBioInsecticide",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "Insecticide",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "BtBioSpray",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "BtBioSpray";
            }

            if (string.Equals(token, "Drainage",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "DrainageImprovement",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "DrainageImprovement";
            }

            return token;
        }

        private static bool CanObtainRequiredItems(DailyTaskTemplate template)
        {
            if (template == null || string.IsNullOrWhiteSpace(template.itemType))
                return true;

            if (PlayerInventory.Instance == null)
                return false;

            foreach (string option in template.itemType.Split('|'))
            {
                if (!Enum.TryParse(MapRequirementToItem(option), true,
                        out InventoryItemType item))
                {
                    continue;
                }

                int held = PlayerInventory.Instance.GetCount(item);
                if (held < 1)
                    continue;

                int needed = PlayerInventory.IsTool(item)
                    ? 1
                    : Mathf.Max(1, template.targetAmount);

                if (held >= needed)
                    return true;

                if (!ShopUIBuilder.SellsToPlayer(item))
                    continue;

                long cost = ShopUIBuilder.CostFor(item, needed - held);
                if (PlayerInventory.Instance.money >= cost)
                    return true;
            }

            return false;
        }

        private static bool HasRequiredItem(string requirement)
        {
            if (string.IsNullOrWhiteSpace(requirement))
                return true;

            foreach (string option in requirement.Split('|'))
            {
                if (HasInventoryItem(MapRequirementToItem(option)))
                    return true;
            }
            return false;
        }

        private static string MapRequirementToItem(string value)
        {
            switch (NormalizeToken(value))
            {
                case "NeemSoap": return "NeemSoapLiter";
                case "BtBioSpray": return "BtBioInsecticideLiter";
                case "CopperFungicide": return "CopperFungicideLiter";
                case "Disinfectant": return "DisinfectantLiter";
                case "DrainageImprovement": return "DrainageKit";
                case "Sanitation": return "Machete";
                case "RemoveInfectedPlant": return "Machete";
                case "TermiteBait": return "TermiteBaitStation";
                case "SupportStakeOrTrellis": return "SupportStakeKit";
                default: return value;
            }
        }

        private static bool HasInventoryItem(string itemName)
        {
            if (PlayerInventory.Instance == null ||
                string.IsNullOrWhiteSpace(itemName))
            {
                return false;
            }
            return Enum.TryParse(itemName, true,
                       out InventoryItemType item) &&
                   PlayerInventory.Instance.HasItem(item, 1);
        }

        private static bool CanPlant(
            string cropType,
            int requiredAmount = 1)
        {
            return PlantingMaterialCatalog.TryGetCropType(cropType, out FarmCropType crop) &&
                   PlantingAvailability.CountPlantableToday(crop) >= Mathf.Max(1, requiredAmount);
        }

        private static bool CanPlantAny(
            string allowedItems,
            int requiredAmount = 1)
        {
            Func<InventoryItemType, bool> allow = string.IsNullOrWhiteSpace(allowedItems)
                ? null
                : new Func<InventoryItemType, bool>(item => AlternativeMatches(allowedItems, item.ToString()));

            return PlantingAvailability.CountPlantableToday(allow) >= Mathf.Max(1, requiredAmount);
        }

        private static bool HasSellableInventory(
            string cropType,
            int requiredAmount)
        {
            PlayerInventory inventory = PlayerInventory.Instance;
            if (inventory == null)
                return false;

            int required = Mathf.Max(1, requiredAmount);
            if (!string.IsNullOrWhiteSpace(cropType))
            {
                InventoryItemType item =
                    FarmTaskContextBuilder.CropNameToFruitItem(cropType);
                return item != InventoryItemType.None &&
                       inventory.GetCount(item) >= required;
            }

            int total = 0;
            foreach (InventoryItemType item in
                     Enum.GetValues(typeof(InventoryItemType)))
            {
                if (!FarmTaskContextBuilder.IsSellableCropItem(item))
                    continue;
                total += inventory.GetCount(item);
                if (total >= required)
                    return true;
            }
            return false;
        }

        private static bool HasCompatibleActiveCondition(
            string cropType,
            string conditionType,
            string mitigationAlternatives)
        {
            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                if (crop == null ||
                    (!string.IsNullOrWhiteSpace(cropType) &&
                     !string.Equals(crop.CropDisplayName, cropType,
                         StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (CropHasCompatibleMitigation(
                        crop,
                        conditionType,
                        mitigationAlternatives))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CropHasCompatibleMitigation(
            CropRuntimeAdapter crop,
            string conditionType,
            string mitigationAlternatives)
        {
            if (crop == null || crop.Root == null)
                return false;

            PestDiseaseAffectedCrop affected =
                crop.Root.GetComponent<PestDiseaseAffectedCrop>();
            PestDiseaseDatabase database =
                PestDiseaseSystem.Instance != null
                    ? PestDiseaseSystem.Instance.Database
                    : null;
            if (affected == null || database == null ||
                affected.activeConditions == null)
            {
                return false;
            }

            foreach (ActivePestDisease condition in affected.activeConditions)
            {
                if (condition == null || condition.severity <= 0.1f)
                    continue;
                if (!string.IsNullOrWhiteSpace(conditionType) &&
                    !string.Equals(condition.type.ToString(), conditionType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                PestDiseaseRule rule =
                    database.GetRule(crop.CropType, condition.type);
                if (rule == null || rule.mitigations == null)
                    continue;

                foreach (PestDiseaseMitigation mitigation in rule.mitigations)
                {
                    if (AlternativeMatches(
                            mitigationAlternatives,
                            mitigation.ToString()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void BindConditionAndOwnedMitigations(
            DailyTaskInstance task,
            DailyTaskTemplate template,
            CropRuntimeAdapter crop)
        {
            if (task == null || template == null || crop == null ||
                crop.Root == null)
            {
                return;
            }

            DailyTaskKind kind = template.kind;
            if (kind != DailyTaskKind.MitigateCondition &&
                kind != DailyTaskKind.ReduceAnyPestSeverity &&
                kind != DailyTaskKind.ReduceAnyDiseaseSeverity)
            {
                return;
            }

            ActivePestDisease selected = SelectCondition(
                crop,
                template.conditionType,
                kind == DailyTaskKind.ReduceAnyDiseaseSeverity,
                kind == DailyTaskKind.ReduceAnyPestSeverity,
                template.targetThreshold,
                template.id == 196);
            if (selected == null)
                return;

            if (string.IsNullOrWhiteSpace(task.conditionType))
                task.conditionType = selected.type.ToString();

            if (!string.IsNullOrWhiteSpace(task.itemType))
                return;

            string owned = GetOwnedCompatibleMitigations(
                crop,
                selected.type);
            if (string.IsNullOrWhiteSpace(owned))
                return;

            task.itemType = owned;
            task.description += " Owned valid options: " +
                                owned.Replace("|", ", ") + ".";
        }

        private static ActivePestDisease SelectCondition(
            CropRuntimeAdapter crop,
            string conditionType,
            bool diseasesOnly,
            bool pestsOnly,
            float minimumSeverity,
            bool highest)
        {
            if (crop == null || crop.Root == null)
                return null;

            PestDiseaseAffectedCrop affected =
                crop.Root.GetComponent<PestDiseaseAffectedCrop>();
            PestDiseaseDatabase database =
                PestDiseaseSystem.Instance != null
                    ? PestDiseaseSystem.Instance.Database
                    : null;
            if (affected == null || database == null ||
                affected.activeConditions == null)
            {
                return null;
            }

            List<ActivePestDisease> matches =
                new List<ActivePestDisease>();
            foreach (ActivePestDisease condition in
                     affected.activeConditions)
            {
                if (condition == null ||
                    condition.severity <= Mathf.Max(0.1f, minimumSeverity))
                {
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(conditionType) &&
                    !string.Equals(
                        condition.type.ToString(),
                        conditionType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                PestDiseaseRule rule =
                    database.GetRule(crop.CropType, condition.type);
                if (rule == null)
                    continue;
                if (diseasesOnly &&
                    rule.category == PestDiseaseCategory.Pest)
                {
                    continue;
                }
                if (pestsOnly &&
                    rule.category != PestDiseaseCategory.Pest)
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(
                        GetOwnedCompatibleMitigations(
                            crop,
                            condition.type)))
                {
                    continue;
                }
                matches.Add(condition);
            }

            if (matches.Count == 0)
                return null;
            return highest
                ? matches.OrderByDescending(value => value.severity).First()
                : matches[UnityEngine.Random.Range(0, matches.Count)];
        }

        private static bool CropHasOwnedCompatibleMitigation(
            CropRuntimeAdapter crop,
            string conditionType,
            bool diseasesOnly,
            bool pestsOnly)
        {
            return SelectCondition(
                       crop,
                       conditionType,
                       diseasesOnly,
                       pestsOnly,
                       0f,
                       highest: false) != null;
        }

        private static string GetOwnedCompatibleMitigations(
            CropRuntimeAdapter crop,
            PestDiseaseType conditionType)
        {
            PestDiseaseDatabase database =
                PestDiseaseSystem.Instance != null
                    ? PestDiseaseSystem.Instance.Database
                    : null;
            if (crop == null || database == null)
                return string.Empty;

            PestDiseaseRule rule =
                database.GetRule(crop.CropType, conditionType);
            if (rule == null || rule.mitigations == null)
                return string.Empty;

            List<string> owned = new List<string>();
            foreach (PestDiseaseMitigation mitigation in rule.mitigations)
            {
                string name = mitigation.ToString();
                if (HasRequiredItem(name) &&
                    !owned.Contains(name))
                {
                    owned.Add(name);
                }
            }
            return string.Join("|", owned);
        }

        private static float GetHighestOwnedCompatibleConditionSeverity(
            CropRuntimeAdapter crop)
        {
            if (crop == null || crop.Root == null)
                return 0f;
            PestDiseaseAffectedCrop affected =
                crop.Root.GetComponent<PestDiseaseAffectedCrop>();
            if (affected == null || affected.activeConditions == null)
                return 0f;

            float highest = 0f;
            foreach (ActivePestDisease condition in
                     affected.activeConditions)
            {
                if (condition == null ||
                    string.IsNullOrWhiteSpace(
                        GetOwnedCompatibleMitigations(
                            crop,
                            condition.type)))
                {
                    continue;
                }
                highest = Mathf.Max(highest, condition.severity);
            }
            return highest;
        }

        private static bool HasPracticalCareAction(
            CropRuntimeAdapter crop)
        {
            if (crop == null)
                return false;
            if (HasInventoryItem("WateringCan"))
                return true;
            if (CropHasOwnedCompatibleMitigation(
                    crop,
                    string.Empty,
                    diseasesOnly: false,
                    pestsOnly: false))
            {
                return true;
            }
            if (HasInventoryItem("PruningShears") &&
                CropMaintenanceCatalog.IsAllowed(
                    crop.CropType,
                    CropMaintenanceActionType.Prune))
            {
                return true;
            }
            return HasInventoryItem("OrganicCompostBag") &&
                   CropMaintenanceCatalog.IsAllowed(
                       crop.CropType,
                       CropMaintenanceActionType.OrganicCompost);
        }

        private static bool HasAnyActiveCondition(
            string cropType,
            string conditionType)
        {
            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                if (crop == null ||
                    (!string.IsNullOrWhiteSpace(cropType) &&
                     !string.Equals(crop.CropDisplayName, cropType,
                         StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                if (CropHasCondition(crop, conditionType, 0f))
                    return true;
            }
            return false;
        }

        private static bool CropHasCondition(
            CropRuntimeAdapter crop,
            string conditionType,
            float minimumSeverity)
        {
            if (crop == null || crop.Root == null)
                return false;
            PestDiseaseAffectedCrop affected =
                crop.Root.GetComponent<PestDiseaseAffectedCrop>();
            if (affected == null ||
                affected.activeConditions == null)
            {
                return false;
            }

            foreach (ActivePestDisease condition in
                     affected.activeConditions)
            {
                if (condition == null ||
                    condition.severity <= Mathf.Max(0.1f, minimumSeverity))
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(conditionType) ||
                    string.Equals(condition.type.ToString(),
                        conditionType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CropHasConditionCategory(
            CropRuntimeAdapter crop,
            PestDiseaseCategory category,
            bool diseasesOnly)
        {
            if (crop == null || crop.Root == null)
                return false;

            PestDiseaseDatabase database =
                PestDiseaseSystem.Instance != null
                    ? PestDiseaseSystem.Instance.Database
                    : null;
            PestDiseaseAffectedCrop affected =
                crop.Root.GetComponent<PestDiseaseAffectedCrop>();
            if (database == null || affected == null ||
                affected.activeConditions == null)
            {
                return false;
            }

            foreach (ActivePestDisease condition in
                     affected.activeConditions)
            {
                if (condition == null || condition.severity <= 0.1f)
                    continue;
                PestDiseaseRule rule =
                    database.GetRule(crop.CropType, condition.type);
                if (rule == null)
                    continue;
                if (diseasesOnly)
                {
                    if (rule.category != PestDiseaseCategory.Pest)
                        return true;
                }
                else if (rule.category == category)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasConditionCategory(
            PestDiseaseCategory category)
        {
            PestDiseaseDatabase database =
                PestDiseaseSystem.Instance != null
                    ? PestDiseaseSystem.Instance.Database
                    : null;
            if (database == null)
                return false;

            foreach (PestDiseaseAffectedCrop crop in
                     Object.FindObjectsByType<PestDiseaseAffectedCrop>(
                         FindObjectsSortMode.None))
            {
                if (crop == null || crop.activeConditions == null)
                    continue;
                foreach (ActivePestDisease condition in
                         crop.activeConditions)
                {
                    PestDiseaseRule rule = condition != null
                        ? database.GetRule(crop.CropType, condition.type)
                        : null;
                    if (condition != null &&
                        condition.severity > 0.1f &&
                        rule != null &&
                        rule.category == category)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool HasAnyDiseaseCondition()
        {
            return HasConditionCategory(
                       PestDiseaseCategory.FungalDisease) ||
                   HasConditionCategory(
                       PestDiseaseCategory.BacterialDisease) ||
                   HasConditionCategory(
                       PestDiseaseCategory.ViralDisease);
        }

        private static bool HasFullMatchingTrap(string itemType)
        {
            if (AlternativeMatches(itemType, "AphidTrap"))
            {
                if (Object.FindObjectsByType<AphidTrapInstance>(
                        FindObjectsSortMode.None)
                    .Any(trap => trap != null && trap.IsFull))
                {
                    return true;
                }
            }
            if (AlternativeMatches(itemType, "PheromoneTrap") ||
                AlternativeMatches(itemType, "TermiteBaitStation"))
            {
                if (Object.FindObjectsByType<AreaMitigationTrapInstance>(
                        FindObjectsSortMode.None)
                    .Any(trap => trap != null && trap.IsFull))
                {
                    return true;
                }
            }
            return false;
        }

        private static CropRuntimeAdapter FindCrop(string cropId)
        {
            return CropRuntimeAdapter.FindAll().FirstOrDefault(crop =>
                crop != null &&
                string.Equals(crop.CropId, cropId,
                    StringComparison.Ordinal));
        }

        private static bool IsMaintenancePresent(
            CropRuntimeAdapter crop,
            string action)
        {
            if (crop == null || crop.Root == null)
                return false;

            if (action == "FruitBag" ||
                action == "DrainageImprovement")
            {
                CropProtectionState protection =
                    crop.Root.GetComponent<CropProtectionState>();
                return protection != null &&
                       (action == "FruitBag"
                           ? protection.hasFruitBag
                           : protection.hasDrainageImprovement);
            }

            CropClimateMaintenanceState state =
                crop.Root.GetComponent<CropClimateMaintenanceState>();
            if (state == null)
                return false;
            switch (action)
            {
                case "Mulch": return state.hasMulch;
                case "SupportStake": return state.hasSupportStake;
                case "Trellis": return state.hasTrellis;
                case "RaisedBed": return state.hasRaisedBed;
                case "SupportStakeOrTrellis":
                    return state.hasSupportStake || state.hasTrellis;
                case "OrganicCompost":
                    return GameTimeSystem.Instance != null &&
                           GameTimeSystem.Instance.TotalGameDays -
                           state.lastCompostGameDay < 0.5f;
                case "Prune":
                    return GameTimeSystem.Instance != null &&
                           GameTimeSystem.Instance.TotalGameDays -
                           state.lastPrunedGameDay < 0.5f;
                default: return false;
            }
        }

        private static float CountWorldMitigation(string typeName)
        {
            if (!Enum.TryParse(typeName, true,
                    out ClimateWorldMitigationType type))
            {
                return 0f;
            }
            return Object.FindObjectsByType<ClimateMitigationWorldObject>(
                    FindObjectsSortMode.None)
                .Count(item => item != null &&
                               item.mitigationType == type);
        }

        private static float GetRelevantSeverity(
            DailyTaskInstance task)
        {
            float total = 0f;
            PestDiseaseDatabase database =
                PestDiseaseSystem.Instance != null
                    ? PestDiseaseSystem.Instance.Database
                    : null;
            DailyTaskKind kind = ParseKind(task.kind);

            foreach (PestDiseaseAffectedCrop crop in
                     Object.FindObjectsByType<PestDiseaseAffectedCrop>(
                         FindObjectsSortMode.None))
            {
                if (crop == null || crop.activeConditions == null)
                    continue;
                if (!string.IsNullOrWhiteSpace(task.cropType) &&
                    !string.Equals(crop.CropDisplayName,
                        task.cropType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(task.cropId) &&
                    !string.Equals(crop.CropId, task.cropId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (ActivePestDisease condition in crop.activeConditions)
                {
                    if (condition == null)
                        continue;
                    if (!string.IsNullOrWhiteSpace(task.conditionType) &&
                        !string.Equals(condition.type.ToString(),
                            task.conditionType,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    PestDiseaseRule rule = database != null
                        ? database.GetRule(crop.CropType, condition.type)
                        : null;
                    if (kind == DailyTaskKind.ReduceAnyPestSeverity &&
                        (rule == null ||
                         rule.category != PestDiseaseCategory.Pest))
                    {
                        continue;
                    }
                    if (kind == DailyTaskKind.ReduceAnyDiseaseSeverity &&
                        rule != null &&
                        rule.category == PestDiseaseCategory.Pest)
                    {
                        continue;
                    }
                    total += Mathf.Max(0f, condition.severity);
                }
            }
            return total;
        }

        private static float AverageHealth()
        {
            List<CropRuntimeAdapter> crops =
                CropRuntimeAdapter.FindAll();
            return crops.Count == 0
                ? 0f
                : crops.Average(crop => crop.Health);
        }

        private static float AverageStress(bool affectedOnly)
        {
            List<CropRuntimeAdapter> crops =
                CropRuntimeAdapter.FindAll();
            if (affectedOnly)
            {
                crops = crops.Where(crop =>
                {
                    PestDiseaseAffectedCrop affected =
                        crop.Root.GetComponent<PestDiseaseAffectedCrop>();
                    return affected != null &&
                           affected.HasAnyPestOrDisease();
                }).ToList();
            }
            return crops.Count == 0
                ? 0f
                : crops.Average(crop => crop.Stress);
        }

        private static DailyTaskTemplate CreateEmergencyDigTask()
        {
            return new DailyTaskTemplate
            {
                id = 66,
                title = "Prepare a planting spot",
                description =
                    "Till ground with the shovel, then dig a planting hole, build a " +
                    "raised bed or open a furrow in it.",
                kind = DailyTaskKind.DigPlantingSpot,
                difficulty = DailyTaskDifficulty.Easy,
                itemType = "Shovel",
                targetAmount = 1
            };
        }

        private static DailyTaskTemplate CreateEmergencyDigTwoTask()
        {
            return new DailyTaskTemplate
            {
                id = 66,
                title = "Prepare two planting spots",
                description =
                    "Till two patches of ground and prepare each one as a planting " +
                    "hole, a raised bed or a furrow.",
                kind = DailyTaskKind.DigPlantingSpot,
                difficulty = DailyTaskDifficulty.Easy,
                itemType = "Shovel",
                targetAmount = 2
            };
        }

        private static DailyTaskTemplate CreateEmergencyPlantTask()
        {
            return new DailyTaskTemplate
            {
                id = 67,
                title = "Plant any crop",
                description =
                    "Plant any planting material in prepared ground, or transplant a " +
                    "ready seedling from the Seedling Tent.",
                kind = DailyTaskKind.PlantAnyCrop,
                difficulty = DailyTaskDifficulty.Easy,
                targetAmount = 1
            };
        }
    }
}
