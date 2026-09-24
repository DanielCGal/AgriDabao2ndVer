using System;
using System.Collections.Generic;
using UnityEngine;
namespace AgriDabao3D
{
    [Serializable]
    public class FarmSnapshotDto
    {
        public AreaSaveDto area = new AreaSaveDto();
        public SoilRegionSaveDto soil = new SoilRegionSaveDto();
        public PlayerSaveDto player = new PlayerSaveDto();
        public InventorySaveDto inventory = new InventorySaveDto();
        public GameTimeSaveDto gameTime = new GameTimeSaveDto();
        public WeatherSaveDto weather = new WeatherSaveDto();
        public ClimateEventSaveDto climateEvent = new ClimateEventSaveDto();
        public DailyTaskSystemSaveDto dailyTasks = new DailyTaskSystemSaveDto();
        public AIAdvisorTaskSaveDto aiAdvisorTask = new AIAdvisorTaskSaveDto();
        public List<CropSaveDto> crops = new List<CropSaveDto>();
        public List<WorldObjectSaveDto> worldObjects = new List<WorldObjectSaveDto>();
        public TutorialSaveDto tutorial = new TutorialSaveDto();

        public NurserySaveDto nursery = new NurserySaveDto();
        public List<PreparedPlotSaveDto> preparedPlots = new List<PreparedPlotSaveDto>();
    }

    [Serializable]
    public class NurserySaveDto
    {
        public bool tentPlaced;
        public SerializableVector3 tentPosition;
        public SerializableQuaternion tentRotation;
        public List<SeedlingBagSaveDto> bags = new List<SeedlingBagSaveDto>();
    }

    [Serializable]
    public class SeedlingBagSaveDto
    {
        public int slot;
        public bool filled;
        public string material;
        public float sownGameDay;
        public float prickedGameDay = -1f;
    }

    [Serializable]
    public class PreparedPlotSaveDto
    {
        public string plotId;
        public string plotKind;
        public bool mulched;
        public string bedPatchId;
        public float preparedGameDay;
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public SerializableVector3 terrainNormal;
        public SoilSample soil;
        public string districtName;
    }

    [Serializable]
    public class TutorialSaveDto
    {
        public bool completed;
        public bool offered;
        public int currentStep = -1;
        public List<string> startingSeeds = new List<string>();
    }
    [Serializable]
    public class AreaSaveDto
    {
        public float x;
        public float y;
        public float width;
        public float height;
        public string districtName;
        public int terrainSeed;
    }
    [Serializable]
    public class SoilRegionSaveDto
    {
        public float sampleALatitude;
        public float sampleALongitude;
        public SoilSample sampleA;
        public float sampleBLatitude;
        public float sampleBLongitude;
        public SoilSample sampleB;
    }
    [Serializable]
    public class PlayerSaveDto
    {
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
    }
    [Serializable]
    public class InventorySaveDto
    {
        public int selectedSlotIndex;
        public int money;
        public List<InventorySlotSaveDto> slots = new List<InventorySlotSaveDto>();
    }
    [Serializable]
    public class InventorySlotSaveDto
    {
        public string itemType;
        public int amount;
        public int liquidMl;
        public string sprayerLiquid;
    }
    [Serializable]
    public class GameTimeSaveDto
    {
        public float totalGameDays;
        public int daysPerMonth;
        public int monthsPerYear;
    }
    [Serializable]
    public class WeatherSaveDto
    {
        public string currentEvent;
        public float currentTemperatureC;
        public float currentHumidity;
        public float currentRainIntensity;
        public int remainingEventDays;
        public float dailyLowTemperatureC;
        public float dailyHighTemperatureC;
    }
    [Serializable]
    public class ClimateEventSaveDto
    {
        public bool isTrackingEvent;
        public string activeEventType;
        public int activeDurationDays;
        public float eventStartGameDay;
        public List<CropSnapshot> beforeSnapshots = new List<CropSnapshot>();
        public List<ClimateActionRecord> actionRecords = new List<ClimateActionRecord>();
    }
    [Serializable]
    public class CropClimateMaintenanceSaveDto
    {
        public bool hasMulch;
        public bool hasSupportStake;
        public bool hasTrellis;
        public bool hasRaisedBed;
        public float lastCompostGameDay = -999f;
        public float lastPrunedGameDay = -999f;
        public string raisedBedPatchId;
        public float raisedBedRootOffset;
    }
    [Serializable]
    public class CropSaveDto
    {
        public string cropFamily;
        public string cropId;
        public string cropName;
        public string tropicalCropKind;
        public string districtName;
        public string visualSoilType;
        public SoilSample plantedSoil;
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public float soilSuitability;
        public float drainage;
        public float fertility;
        public float moisture;
        public float health;
        public float stress;
        public float averageHealth;
        public float currentTemperatureC;
        public float temperatureSuitability;
        public string stage;
        public float plantedGameDay;
        public float lastWateredGameDay;
        public float nextProductionGameDay;
        public int availableHarvestCount;
        public CropClimateMaintenanceSaveDto maintenance = new CropClimateMaintenanceSaveDto();
        public List<BananaBulbHarvestSaveDto> bananaBulbs = new List<BananaBulbHarvestSaveDto>();
        public List<TropicalBundleHarvestSaveDto> tropicalBundles = new List<TropicalBundleHarvestSaveDto>();
        public List<PestConditionSaveDto> activeConditions = new List<PestConditionSaveDto>();

        public string plantingMaterial;
        public float nurseryDays;
        public float fieldPlantedGameDay = -1f;

        public bool hasFruitBag;
        public bool hasDrainageImprovement;
    }
    [Serializable]
    public class BananaBulbHarvestSaveDto
    {
        public int bananaCount;
        public float producedGameDay;
    }
    [Serializable]
    public class TropicalBundleHarvestSaveDto
    {
        public int itemCount;
        public float producedGameDay;
    }
    [Serializable]
    public class PestConditionSaveDto
    {
        public string type;
        public float severity;
        public float targetSeverity;
        public float startedGameDay;
    }
    [Serializable]
    public class WorldObjectSaveDto
    {
        public string objectType;
        public SerializableVector3 position;
        public SerializableQuaternion rotation;
        public float currentLoad;
        public float capacity;
        public float radius;
        public string mitigation;
        public string climateMitigationType;
        public float effectiveness;
        public float storedResource;
        public float resourceCapacity;
        public string terrainPatchId;
        public float terrainLength;
        public float terrainWidth;
        public float terrainDelta;
    }
    [Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;
        public SerializableVector3(Vector3 value) { x = value.x; y = value.y; z = value.z; }
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }
    [Serializable]
    public struct SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public SerializableQuaternion(Quaternion value) { x = value.x; y = value.y; z = value.z; w = value.w; }
        public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
    }
    public static class FarmLoadContext
    {
        public static FarmSnapshotDto PendingSnapshot;
        public static bool IsRestoring => PendingSnapshot != null;
        public static void Clear() { PendingSnapshot = null; }
    }
}
