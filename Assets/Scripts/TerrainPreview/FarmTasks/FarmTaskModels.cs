using System;
using System.Collections.Generic;

namespace AgriDabao3D
{
    public enum DailyTaskKind
    {
        None,
        WaterCrop,
        HarvestCrop,
        ApplyMaintenance,
        PlantCrop,
        PlantAnyCrop,
        DigPlantingSpot,
        CollectHarvest,
        SellCropQuantity,
        SellCropValue,
        PlaceWorldMitigation,
        PlacePestTrap,
        CleanTrap,
        MitigateCondition,
        ReduceAnyPestSeverity,
        ReduceAnyDiseaseSeverity,
        ReduceFarmConditionSeverity,
        RemoveInfectedPlant,
        RaiseCropMoisture,
        RaiseAverageHealth,
        LowerAverageStress,

        TillGround,
        SowSeedlingBag,
        TransplantSeedling
    }

    public enum DailyTaskDifficulty
    {
        Easy = 1,
        Medium = 2,
        Hard = 3
    }

    [Serializable]
    public class DailyTaskTemplate
    {
        public int id;
        public string title;
        public string description;
        public DailyTaskKind kind;
        public DailyTaskDifficulty difficulty;
        public string cropType;
        public string itemType;
        public string maintenanceAction;
        public string requiredWeather;
        public string conditionType;
        public string requiredStage;
        public float eligibilityThreshold;
        public int targetAmount = 1;
        public int targetValue;
        public float targetThreshold;
        public float requiredReduction;
        public bool requiresActiveCondition;
    }

    [Serializable]
    public class DailyTaskInstance
    {
        public string instanceId;
        public int templateId;
        public string title;
        public string description;
        public string kind;
        public string difficulty;
        public string cropType;
        public string cropId;
        public string itemType;
        public string maintenanceAction;
        public string requiredWeather;
        public string conditionType;
        public string requiredStage;
        public float eligibilityThreshold;
        public int targetAmount = 1;
        public int targetValue;
        public float targetThreshold;
        public float requiredReduction;
        public int progressAmount;
        public int progressValue;
        public List<string> progressKeys = new List<string>();
        public float baselineMetric;
        public float currentMetric;
        public bool completed;
        public float assignedGameDay;
    }

    [Serializable]
    public class DailyTaskSystemSaveDto
    {
        public int assignedAbsoluteDay;
        public int rewardMoney;
        public bool rewardClaimed;
        public bool skipUsed;
        public List<DailyTaskInstance> tasks = new List<DailyTaskInstance>();
    }

    [Serializable]
    public class AIAdvisorTaskObjective
    {
        public string type;
        public string cropType;
        public string cropId;
        public string conditionType;
        public string itemType;
        public int targetAmount;
        public int targetValue;
        public float targetThreshold;
        public float requiredReduction;
        public float baselineMetric;
    }

    [Serializable]
    public class AIAdvisorTaskSaveDto
    {
        public bool active;
        public bool checking;
        public string taskId;
        public string dialogue;
        public string taskText;
        public int rewardMoney;
        public float startedGameDay;
        public AIAdvisorTaskObjective objective = new AIAdvisorTaskObjective();
        public List<CropSnapshot> beforeSnapshots = new List<CropSnapshot>();
        public List<ClimateActionRecord> actionRecords = new List<ClimateActionRecord>();
        public string lastVerdict;
        public string lastTips;
    }

    [Serializable]
    public class FarmTaskCropContext
    {
        public string cropId;
        public string cropName;
        public string cropType;
        public string district;
        public string stage;
        public float health;
        public float averageHealth;
        public float stress;
        public float moisture;
        public float fertility;
        public float drainage;
        public float soilSuitability;
        public bool harvestReady;
        public int availableHarvest;
        public string maintenanceState;
        public string plantingMaterial;
        public List<FarmTaskConditionContext> conditions = new List<FarmTaskConditionContext>();
    }

    [Serializable]
    public class FarmTaskNurseryBagContext
    {
        public int bag;
        public string status;
        public string plantingMaterial;
        public string cropType;
        public float daysUntilNextStep;
    }

    [Serializable]
    public class FarmTaskPreparedGroundContext
    {
        public string preparation;
        public bool mulched;
    }

    [Serializable]
    public class FarmTaskConditionContext
    {
        public string type;
        public float severity;
        public float targetSeverity;
        public List<string> validMitigations = new List<string>();
    }

    [Serializable]
    public class FarmTaskInventoryContext
    {
        public string itemType;
        public int amount;
    }

    [Serializable]
    public class FarmTaskWorldObjectContext
    {
        public string objectType;
        public string mitigationType;
        public float radius;
        public float currentLoad;
        public float capacity;
        public float storedResource;
        public float resourceCapacity;
    }

    [Serializable]
    public class FarmTaskContextPayload
    {
        public string district;
        public float gameDay;
        public int year;
        public int month;
        public int day;
        public string time;
        public string weather;
        public float temperatureC;
        public float humidity;
        public float rainIntensity;
        public int remainingWeatherDays;
        public int money;
        public List<FarmTaskInventoryContext> inventory = new List<FarmTaskInventoryContext>();
        public List<FarmTaskCropContext> crops = new List<FarmTaskCropContext>();
        public List<FarmTaskWorldObjectContext> worldObjects = new List<FarmTaskWorldObjectContext>();
        public List<FarmTaskNurseryBagContext> seedlingTent = new List<FarmTaskNurseryBagContext>();
        public List<FarmTaskPreparedGroundContext> preparedGround = new List<FarmTaskPreparedGroundContext>();
    }

    [Serializable]
    public class AIAdvisorTaskGenerationEnvelope
    {
        public string dialogue;
        public string taskText;
        public int rewardMoney;
        public AIAdvisorTaskObjective objective = new AIAdvisorTaskObjective();
    }

    [Serializable]
    public class AIAdvisorTaskCheckEnvelope
    {
        public bool completed;
        public string reason;
        public string progressSummary;
        public List<string> tips = new List<string>();
    }

    [Serializable]
    public class AIAdvisorTaskCheckPayload
    {
        public AIAdvisorTaskSaveDto task;
        public FarmTaskContextPayload currentFarm;
    }
}
