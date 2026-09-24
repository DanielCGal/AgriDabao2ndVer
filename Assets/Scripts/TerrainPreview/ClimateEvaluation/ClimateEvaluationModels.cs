using System;
using System.Collections.Generic;
namespace AgriDabao3D
{
    [Serializable]
    public class ClimateActionRecord
    {
        public string actionType;
        public string itemType;
        public string cropType;
        public string cropId;
        public float gameDay;
        public SerializableVector3 worldPosition;
        public float beforeMoisture;
        public float afterMoisture;
        public float beforeHealth;
        public float afterHealth;
        public float beforeStress;
        public float afterStress;
        public float beforeDrainage;
        public float afterDrainage;
        public float beforeFertility;
        public float afterFertility;
        public bool actionSucceeded;
        public bool recommendedForEvent;
        public float effectivenessScore;
        public string details;
        public int quantity;
        public int moneyDelta;
        public string conditionType;
        public float beforeSeverity;
        public float afterSeverity;
        public string taskSource;
        public string recordOrigin;
    }
    [Serializable]
    public class CropSnapshot
    {
        public string cropId;
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
        public string maintenanceState;
    }
    [Serializable]
    public class ClimateEventEvaluationPayload
    {
        public string evaluationType = "ClimateResilience";
        public string eventType;
        public int durationDays;
        public float startGameDay;
        public float endGameDay;
        public List<CropSnapshot> beforeCrops = new List<CropSnapshot>();
        public List<CropSnapshot> afterCrops = new List<CropSnapshot>();
        public List<ClimateActionRecord> actions = new List<ClimateActionRecord>();
        public float precomputedScore;
    }
}

