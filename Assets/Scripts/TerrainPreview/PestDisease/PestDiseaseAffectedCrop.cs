using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AgriDabao3D
{
    public class PestDiseaseAffectedCrop : MonoBehaviour
    {
        [Header("Active Pest and Disease Conditions")]
        public List<ActivePestDisease> activeConditions =
            new List<ActivePestDisease>();

        private CoconutTreeInstance coconut;
        private BananaPlantInstance banana;
        private TropicalCropPlantInstance tropical;

        private void Awake()
        {
            CacheCrop();

            if (activeConditions == null)
            {
                activeConditions =
                    new List<ActivePestDisease>();
            }
        }

        private void Start()
        {
            if (PestDiseaseSystem.Instance != null)
            {
                PestDiseaseSystem.Instance.RegisterCrop(this);
            }
        }

        private void OnDestroy()
        {
            if (PestDiseaseSystem.Instance != null)
            {
                PestDiseaseSystem.Instance.UnregisterCrop(this);
            }
        }

        private void Update()
        {
            if (GameTimeSystem.Instance == null)
                return;

            SimulateDisease(
                GameTimeSystem.Instance.DeltaGameDays
            );
        }

        private void CacheCrop()
        {
            coconut =
                GetComponent<CoconutTreeInstance>();

            banana =
                GetComponent<BananaPlantInstance>();

            tropical =
                GetComponent<TropicalCropPlantInstance>();
        }

        public FarmCropType CropType
        {
            get
            {
                if (coconut != null)
                    return FarmCropType.Coconut;

                if (banana != null)
                    return FarmCropType.Banana;

                if (tropical != null)
                {
                    return CropClimateRules.GetFarmCropType(
                        tropical.cropKind
                    );
                }

                return FarmCropType.Tomato;
            }
        }

        public string CropDisplayName
        {
            get
            {
                if (coconut != null)
                    return "Coconut";

                if (banana != null)
                    return "Banana";

                if (tropical != null)
                    return tropical.CropDisplayName;

                return gameObject.name;
            }
        }

        public string CropId
        {
            get
            {
                if (coconut != null)
                    return coconut.cropId;

                if (banana != null)
                    return banana.cropId;

                if (tropical != null)
                    return tropical.cropId;

                return gameObject.GetInstanceID().ToString();
            }
        }

        public float Health
        {
            get
            {
                if (coconut != null)
                    return coconut.health;

                if (banana != null)
                    return banana.health;

                if (tropical != null)
                    return tropical.health;

                return 0f;
            }
        }

        public float Stress
        {
            get
            {
                if (coconut != null)
                    return coconut.stress;

                if (banana != null)
                    return banana.stress;

                if (tropical != null)
                    return tropical.stress;

                return 0f;
            }
        }

        public float Moisture
        {
            get
            {
                if (coconut != null)
                    return coconut.moisture;

                if (banana != null)
                    return banana.moisture;

                if (tropical != null)
                    return tropical.moisture;

                return 0f;
            }
        }

        public float Drainage
        {
            get
            {
                if (coconut != null)
                    return coconut.drainage;

                if (banana != null)
                    return banana.drainage;

                if (tropical != null)
                    return tropical.drainage;

                return 0.5f;
            }
        }

        public CropDevelopmentStage CurrentStage
        {
            get
            {
                if (coconut != null)
                {
                    return coconut.stage switch
                    {
                        CoconutStage.Seedling =>
                            CropDevelopmentStage.Seedling,

                        CoconutStage.Young =>
                            CropDevelopmentStage.Vegetative,

                        CoconutStage.Immature =>
                            CropDevelopmentStage.PreFruiting,

                        CoconutStage.Mature =>
                            CropDevelopmentStage.Fruiting,

                        CoconutStage.Old =>
                            CropDevelopmentStage.Old,

                        _ => CropDevelopmentStage.Any
                    };
                }

                if (banana != null)
                {
                    return banana.stage switch
                    {
                        BananaStage.Seedling =>
                            CropDevelopmentStage.Seedling,

                        BananaStage.Vegetative =>
                            CropDevelopmentStage.Vegetative,

                        BananaStage.PreFruiting =>
                            CropDevelopmentStage.PreFruiting,

                        BananaStage.Fruiting =>
                            CropDevelopmentStage.Fruiting,

                        BananaStage.Old =>
                            CropDevelopmentStage.Old,

                        _ => CropDevelopmentStage.Any
                    };
                }

                if (tropical != null)
                {
                    return tropical.stage switch
                    {
                        TropicalCropStage.Seedling =>
                            CropDevelopmentStage.Seedling,

                        TropicalCropStage.Vegetative =>
                            CropDevelopmentStage.Vegetative,

                        TropicalCropStage.PreFruiting =>
                            CropDevelopmentStage.PreFruiting,

                        TropicalCropStage.Fruiting =>
                            CropDevelopmentStage.Fruiting,

                        TropicalCropStage.Old =>
                            CropDevelopmentStage.Old,

                        _ => CropDevelopmentStage.Any
                    };
                }

                return CropDevelopmentStage.Any;
            }
        }

        public ActivePestDisease GetCondition(
            PestDiseaseType type)
        {
            if (activeConditions == null)
                return null;

            return activeConditions.Find(
                condition =>
                    condition != null &&
                    condition.type == type
            );
        }

        public bool HasCondition(
            PestDiseaseType type,
            float minimumSeverity = 0.1f)
        {
            ActivePestDisease condition =
                GetCondition(type);

            return condition != null &&
                   condition.severity >= minimumSeverity;
        }

        public float GetConditionSeverity(
            PestDiseaseType type)
        {
            ActivePestDisease condition =
                GetCondition(type);

            return condition != null
                ? condition.severity
                : 0f;
        }

        public float GetConditionTarget(
            PestDiseaseType type)
        {
            ActivePestDisease condition =
                GetCondition(type);

            return condition != null
                ? condition.targetSeverity
                : 0f;
        }

        public void StartOrIncreaseCondition(
            PestDiseaseType type,
            float startingSeverity,
            float targetSeverity)
        {
            if (type == PestDiseaseType.None)
                return;

            if (activeConditions == null)
            {
                activeConditions =
                    new List<ActivePestDisease>();
            }

            ActivePestDisease existing =
                GetCondition(type);

            startingSeverity = Mathf.Clamp(
                startingSeverity,
                0f,
                100f
            );

            targetSeverity = Mathf.Clamp(
                targetSeverity,
                startingSeverity,
                100f
            );

            if (existing == null)
            {
                activeConditions.Add(
                    new ActivePestDisease
                    {
                        type = type,
                        severity = startingSeverity,
                        targetSeverity = targetSeverity,
                        startedGameDay =
                            GameTimeSystem.Instance != null
                                ? GameTimeSystem.Instance.TotalGameDays
                                : 0f
                    }
                );

                return;
            }

            existing.severity = Mathf.Max(
                existing.severity,
                startingSeverity
            );

            existing.targetSeverity = Mathf.Max(
                existing.targetSeverity,
                targetSeverity
            );
        }

        public void IncreaseConditionTarget(
            PestDiseaseType type,
            float amount,
            float minimumStartingSeverity = 5f)
        {
            ActivePestDisease condition =
                GetCondition(type);

            if (condition == null)
            {
                StartOrIncreaseCondition(
                    type,
                    minimumStartingSeverity,
                    minimumStartingSeverity + amount
                );

                return;
            }

            condition.targetSeverity = Mathf.Clamp(
                condition.targetSeverity + amount,
                condition.severity,
                100f
            );
        }

        public float ReduceCondition(
            PestDiseaseType type,
            float amount)
        {
            ActivePestDisease condition =
                GetCondition(type);

            if (condition == null || amount <= 0f)
                return 0f;

            float before = condition.severity;

            condition.severity = Mathf.Max(
                0f,
                condition.severity - amount
            );

            condition.targetSeverity = Mathf.Max(
                condition.severity,
                condition.targetSeverity - amount
            );

            float removed = before - condition.severity;

            if (condition.severity <= 0.01f &&
                condition.targetSeverity <= 0.01f)
            {
                activeConditions.Remove(condition);
            }

            return removed;
        }

        public float ApplyMitigation(
            PestDiseaseMitigation mitigation,
            float amount)
        {
            if (mitigation == PestDiseaseMitigation.None ||
                amount <= 0f)
            {
                return 0f;
            }

            if (activeConditions == null ||
                activeConditions.Count == 0)
            {
                return 0f;
            }


            if (PestDiseaseSystem.Instance == null ||
                PestDiseaseSystem.Instance.Database == null)
            {
                return 0f;
            }

            float totalRemoved = 0f;

            for (int i = activeConditions.Count - 1;
                 i >= 0;
                 i--)
            {
                ActivePestDisease condition =
                    activeConditions[i];

                if (condition == null)
                    continue;

                PestDiseaseRule rule =
                    PestDiseaseSystem.Instance.Database.GetRule(
                        CropType,
                        condition.type
                    );

                if (rule == null ||
                    rule.mitigations == null ||
                    !rule.mitigations.Contains(mitigation))
                {
                    continue;
                }

                float before = condition.severity;

                condition.severity = Mathf.Max(
                    0f,
                    condition.severity - amount
                );

                condition.targetSeverity = Mathf.Max(
                    condition.severity,
                    condition.targetSeverity - amount
                );

                totalRemoved +=
                    before - condition.severity;

                if (condition.severity <= 0.01f &&
                    condition.targetSeverity <= 0.01f)
                {
                    activeConditions.RemoveAt(i);
                }
            }

            return totalRemoved;
        }

        public void SimulateDisease(float deltaGameDays)
        {
            if (deltaGameDays <= 0f ||
                activeConditions == null ||
                activeConditions.Count == 0)
            {
                return;
            }

            float totalStressDamage = 0f;
            float totalHealthDamage = 0f;

            for (int i = activeConditions.Count - 1;
                 i >= 0;
                 i--)
            {
                ActivePestDisease condition =
                    activeConditions[i];

                if (condition == null)
                {
                    activeConditions.RemoveAt(i);
                    continue;
                }

                PestDiseaseRule rule = null;

                if (PestDiseaseSystem.Instance != null &&
                    PestDiseaseSystem.Instance.Database != null)
                {
                    rule =
                        PestDiseaseSystem.Instance.Database.GetRule(
                            CropType,
                            condition.type
                        );
                }

                float risePerDay =
                    rule != null
                        ? rule.severityRisePerGameDay
                        : 6f;

                condition.severity = Mathf.MoveTowards(
                    condition.severity,
                    condition.targetSeverity,
                    risePerDay * deltaGameDays
                );

                float pressure = Mathf.Pow(
                    Mathf.Clamp01(
                        condition.severity / 100f
                    ),
                    1.35f
                );

                float healthDamagePerDay =
                    rule != null
                        ? rule.healthDamagePerGameDay
                        : 2f;

                float stressDamagePerDay =
                    rule != null
                        ? rule.stressAddedPerGameDay
                        : 3f;

                totalHealthDamage +=
                    pressure *
                    healthDamagePerDay *
                    deltaGameDays;

                totalStressDamage +=
                    pressure *
                    stressDamagePerDay *
                    deltaGameDays;

                if (condition.severity <= 0.01f &&
                    condition.targetSeverity <= 0.01f)
                {
                    activeConditions.RemoveAt(i);
                }
            }

            AddStress(totalStressDamage);
            AddHealth(-totalHealthDamage);
        }

        private void AddStress(float amount)
        {
            if (Mathf.Approximately(amount, 0f))
                return;

            if (coconut != null)
            {
                coconut.stress = Mathf.Clamp(
                    coconut.stress + amount,
                    0f,
                    100f
                );
            }

            if (banana != null)
            {
                banana.stress = Mathf.Clamp(
                    banana.stress + amount,
                    0f,
                    100f
                );
            }

            if (tropical != null)
            {
                tropical.stress = Mathf.Clamp(
                    tropical.stress + amount,
                    0f,
                    100f
                );
            }
        }

        private void AddHealth(float amount)
        {
            if (Mathf.Approximately(amount, 0f))
                return;

            if (coconut != null)
            {
                coconut.health = Mathf.Clamp(
                    coconut.health + amount,
                    0f,
                    100f
                );

                coconut.averageHealth = Mathf.Clamp(
                    coconut.averageHealth +
                    amount * 0.5f,
                    0f,
                    100f
                );
            }

            if (banana != null)
            {
                banana.health = Mathf.Clamp(
                    banana.health + amount,
                    0f,
                    100f
                );

                banana.averageHealth = Mathf.Clamp(
                    banana.averageHealth +
                    amount * 0.5f,
                    0f,
                    100f
                );
            }

            if (tropical != null)
            {
                tropical.health = Mathf.Clamp(
                    tropical.health + amount,
                    0f,
                    100f
                );

                tropical.averageHealth = Mathf.Clamp(
                    tropical.averageHealth +
                    amount * 0.5f,
                    0f,
                    100f
                );
            }
        }

        public float aphidsPercent
        {
            get => GetConditionSeverity(
                PestDiseaseType.Aphids
            );

            set => SetConditionSeverity(
                PestDiseaseType.Aphids,
                value
            );
        }

        public float bacterialWiltPercent
        {
            get => GetConditionSeverity(
                PestDiseaseType.BacterialWilt
            );

            set => SetConditionSeverity(
                PestDiseaseType.BacterialWilt,
                value
            );
        }

        public float armywormsPercent
        {
            get => GetConditionSeverity(
                PestDiseaseType.FallArmyworm
            );

            set => SetConditionSeverity(
                PestDiseaseType.FallArmyworm,
                value
            );
        }

        public float mitesPercent
        {
            get => GetConditionSeverity(
                PestDiseaseType.Mites
            );

            set => SetConditionSeverity(
                PestDiseaseType.Mites,
                value
            );
        }

        public float targetAphidsPercent
        {
            get => GetConditionTarget(
                PestDiseaseType.Aphids
            );

            set => SetConditionTarget(
                PestDiseaseType.Aphids,
                value
            );
        }

        public float targetArmywormsPercent
        {
            get => GetConditionTarget(
                PestDiseaseType.FallArmyworm
            );

            set => SetConditionTarget(
                PestDiseaseType.FallArmyworm,
                value
            );
        }

        public float targetMitesPercent
        {
            get => GetConditionTarget(
                PestDiseaseType.Mites
            );

            set => SetConditionTarget(
                PestDiseaseType.Mites,
                value
            );
        }

        private void SetConditionSeverity(
            PestDiseaseType type,
            float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);

            ActivePestDisease condition =
                GetCondition(type);

            if (condition == null)
            {
                if (value <= 0f)
                    return;

                StartOrIncreaseCondition(
                    type,
                    value,
                    value
                );

                return;
            }

            condition.severity = value;
            condition.targetSeverity = Mathf.Max(
                condition.targetSeverity,
                value
            );
        }

        private void SetConditionTarget(
            PestDiseaseType type,
            float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);

            ActivePestDisease condition =
                GetCondition(type);

            if (condition == null)
            {
                if (value <= 0f)
                    return;

                StartOrIncreaseCondition(
                    type,
                    0f,
                    value
                );

                return;
            }

            condition.targetSeverity = value;
        }

        public void InfectBacterialWilt(
            float startingPercent = 10f)
        {
            StartOrIncreaseCondition(
                PestDiseaseType.BacterialWilt,
                startingPercent,
                Mathf.Max(
                    startingPercent,
                    30f
                )
            );
        }

        public float ReduceAphids(float amount)
        {
            return ReduceCondition(
                PestDiseaseType.Aphids,
                amount
            );
        }

        public float ApplyDisinfectant(float amount)
        {
            return ReduceCondition(
                PestDiseaseType.BacterialWilt,
                amount
            );
        }

        public float ApplyInsecticide(float amount)
        {
            float removed = 0f;

            removed += ReduceCondition(
                PestDiseaseType.FallArmyworm,
                amount
            );

            removed += ReduceCondition(
                PestDiseaseType.Mites,
                amount
            );

            return removed;
        }

        public bool HasAnyPestOrDisease()
        {
            if (activeConditions == null)
                return false;

            return activeConditions.Exists(
                condition =>
                    condition != null &&
                    condition.severity > 0.1f
            );
        }

        public string GetPestInspectionText()
        {
            StringBuilder builder =
                new StringBuilder();

            builder.Append(
                "\n\nPest / Disease Status:\n"
            );

            if (!HasAnyPestOrDisease())
            {
                builder.Append("None");
                return builder.ToString();
            }

            foreach (ActivePestDisease condition
                     in activeConditions)
            {
                if (condition == null ||
                    condition.severity <= 0.1f)
                {
                    continue;
                }

                string displayName =
                    condition.type.ToString();

                if (PestDiseaseSystem.Instance != null &&
                    PestDiseaseSystem.Instance.Database != null)
                {
                    PestDiseaseRule rule =
                        PestDiseaseSystem.Instance.Database.GetRule(
                            CropType,
                            condition.type
                        );

                    if (rule != null &&
                        !string.IsNullOrWhiteSpace(
                            rule.displayName
                        ))
                    {
                        displayName = rule.displayName;
                    }
                }

                builder.AppendLine(
                    $"{displayName}: " +
                    $"{condition.severity:F1}% " +
                    $"(Target {condition.targetSeverity:F1}%)"
                );
            }

            return builder.ToString().TrimEnd();
        }

        public static string GetInspectionTextFor(
            GameObject cropObject)
        {
            if (cropObject == null)
                return "";

            PestDiseaseAffectedCrop disease =
                cropObject.GetComponent<
                    PestDiseaseAffectedCrop>();

            return disease != null
                ? disease.GetPestInspectionText()
                : "\n\nPest / Disease Status:\nNone";
        }
    }
}
