using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AgriDabao3D
{
    [System.Serializable]
    public class AdvisorTimeData
    {
        public int year;
        public int month;
        public int day;
    }

    [System.Serializable]
    public class AdvisorWeatherData
    {

        public string weatherEvent;
        public float temperatureC;

    }

    [System.Serializable]
    public class AdvisorSoilData
    {
        public string district;
        public string soilType;
        public float sand;
        public float silt;
        public float clay;
        public float phh2o;
        public float soc;
        public float cfvo;
        public float bdod;
        public float nitrogen;
    }

    [System.Serializable]
    public class AdvisorCropGroupData
    {
        public string cropType;
        public int count;
        public string averageStage;
        public float averageAgeYears;
        public float averageHealth;
        public float averageStress;
        public float averageWaterPercent;
        public float averageDrainagePercent;
        public float averageFertilityPercent;
        public float averageSuitabilityPercent;
    }

    /// <summary>One bag in the Seedling Tent. The tent is not near any crop, so it is listed on its own.</summary>
    [System.Serializable]
    public class AdvisorSeedlingBagData
    {
        public int bag;
        public string status;
        public string plantingMaterial;
        public float daysUntilNextStep;
    }

    [System.Serializable]
    public class AdvisorContextData
    {
        public string simulationMode;
        public AdvisorTimeData time;
        public AdvisorWeatherData weather;
        public AdvisorSoilData currentSoil;
        public List<AdvisorCropGroupData> cropGroups;
        public AdvisorPestDiseaseData pestDisease;
        public List<AdvisorSeedlingBagData> seedlingTent;
    }

    public class FarmAdvisorContextBuilder : MonoBehaviour
    {
        public SoilAwareTerrainGenerator soilAwareTerrain;
        public Transform playerTransform;

        private void Awake()
        {
            if (soilAwareTerrain == null)
                soilAwareTerrain = Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();

            if (playerTransform == null)
            {
                FirstPersonTerrainController player = Object.FindFirstObjectByType<FirstPersonTerrainController>();
                if (player != null)
                    playerTransform = player.transform;
            }
        }

        public AdvisorContextData BuildContext()
        {
            AdvisorContextData data = new AdvisorContextData();
            data.simulationMode = "semi_simulation";
            data.time = BuildTime();
            data.weather = BuildWeather();
            data.currentSoil = BuildSoil();
            data.cropGroups = BuildCropGroups();
            data.pestDisease = PestDiseaseSystem.Instance != null
            ? PestDiseaseSystem.Instance.BuildAdvisorPestData()
            : new AdvisorPestDiseaseData();
            data.seedlingTent = BuildSeedlingTent();
            return data;
        }

        /// <summary>
        /// The Seedling Tent's bags, wherever the player stands. Crops are only
        /// described within 25 m of the player, and the tent is usually further
        /// away than that, so without this the adviser could not see a single
        /// seedling the player is raising.
        /// </summary>
        private static List<AdvisorSeedlingBagData> BuildSeedlingTent()
        {
            List<AdvisorSeedlingBagData> bags = new List<AdvisorSeedlingBagData>();
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null)
                return bags;

            for (int slot = 0; slot < NurserySystem.BagCount; slot++)
            {
                SeedlingBagStatus status = nursery.GetStatus(slot);
                if (status == SeedlingBagStatus.Empty)
                    continue;

                bool sown = nursery.TryGetMaterial(slot, out PlantingMaterialInfo info);
                bags.Add(new AdvisorSeedlingBagData
                {
                    bag = slot + 1,
                    status = status.ToString(),
                    plantingMaterial = sown ? info.Name : string.Empty,
                    daysUntilNextStep = nursery.DaysUntilNextStep(slot)
                });
            }

            return bags;
        }

        public string BuildContextJson()
        {
            AdvisorContextData data = BuildContext();
            return JsonUtility.ToJson(data, true);
        }

        private AdvisorTimeData BuildTime()
        {
            AdvisorTimeData t = new AdvisorTimeData();

            if (GameTimeSystem.Instance != null)
            {
                t.year = GameTimeSystem.Instance.CurrentYearNumber;
                t.month = GameTimeSystem.Instance.CurrentMonthNumber;
                t.day = GameTimeSystem.Instance.CurrentDayInMonth;
            }

            return t;
        }

        private AdvisorWeatherData BuildWeather()
        {
            AdvisorWeatherData w = new AdvisorWeatherData();

            if (WeatherSystem.Instance != null)
            {
                w.weatherEvent = WeatherSystem.Instance.currentEvent.ToString();
                w.temperatureC = WeatherSystem.Instance.currentTemperatureC;
            }
            else
            {
                w.weatherEvent = "Unknown";
                w.temperatureC = 0f;
            }

            return w;
        }

        private AdvisorSoilData BuildSoil()
        {
            AdvisorSoilData s = new AdvisorSoilData();

            if (soilAwareTerrain != null && playerTransform != null &&
                soilAwareTerrain.TryGetSoilAtWorldPosition(playerTransform.position, out SoilSample sample, out string district))
            {
                s.district = district;
                s.soilType = sample.GetVisualSoilType();
                s.sand = sample.sand;
                s.silt = sample.silt;
                s.clay = sample.clay;
                s.phh2o = sample.phh2o;
                s.soc = sample.soc;
                s.cfvo = sample.cfvo;
                s.bdod = sample.bdod;
                s.nitrogen = sample.nitrogen;
            }

            return s;
        }

        private const float NearbyCropRadius = 25f;

        private List<AdvisorCropGroupData> BuildCropGroups()
        {
            List<AdvisorCropGroupData> groups = new List<AdvisorCropGroupData>();

            if (playerTransform == null)
                return groups;

            Vector3 playerPos = playerTransform.position;

            CoconutTreeInstance[] allCoconuts = Object.FindObjectsByType<CoconutTreeInstance>(FindObjectsSortMode.None);
            CoconutTreeInstance[] coconuts = allCoconuts
                .Where(x => x != null && Vector3.Distance(x.transform.position, playerPos) <= NearbyCropRadius)
                .ToArray();

            if (coconuts.Length > 0)
            {
                groups.Add(new AdvisorCropGroupData
                {
                    cropType = "Coconut",
                    count = coconuts.Length,
                    averageStage = GetMostCommonCoconutStage(coconuts),
                    averageAgeYears = coconuts.Average(x => x.GetAgeYears()),
                    averageHealth = coconuts.Average(x => x.health),
                    averageStress = coconuts.Average(x => x.stress),
                    averageWaterPercent = coconuts.Average(x => x.moisture * 100f),
                    averageDrainagePercent = coconuts.Average(x => x.drainage * 100f),
                    averageFertilityPercent = coconuts.Average(x => x.fertility * 100f),
                    averageSuitabilityPercent = coconuts.Average(x => x.soilSuitability * 100f)
                });
            }

            BananaPlantInstance[] allBananas = Object.FindObjectsByType<BananaPlantInstance>(FindObjectsSortMode.None);
            BananaPlantInstance[] bananas = allBananas
                .Where(x => x != null && Vector3.Distance(x.transform.position, playerPos) <= NearbyCropRadius)
                .ToArray();

            if (bananas.Length > 0)
            {
                groups.Add(new AdvisorCropGroupData
                {
                    cropType = "Banana",
                    count = bananas.Length,
                    averageStage = GetMostCommonBananaStage(bananas),
                    averageAgeYears = bananas.Average(x => x.GetAgeYears()),
                    averageHealth = bananas.Average(x => x.health),
                    averageStress = bananas.Average(x => x.stress),
                    averageWaterPercent = bananas.Average(x => x.moisture * 100f),
                    averageDrainagePercent = bananas.Average(x => x.drainage * 100f),
                    averageFertilityPercent = bananas.Average(x => x.fertility * 100f),
                    averageSuitabilityPercent = bananas.Average(x => x.soilSuitability * 100f)
                });
            }

            TropicalCropPlantInstance[] allTropicals =
            Object.FindObjectsByType<TropicalCropPlantInstance>(FindObjectsSortMode.None);

            TropicalCropPlantInstance[] tropicals = allTropicals
                .Where(x => x != null && Vector3.Distance(x.transform.position, playerPos) <= NearbyCropRadius)
                .ToArray();

            var tropicalGroups = tropicals.GroupBy(x => x.cropKind);

            foreach (var cropGroup in tropicalGroups)
            {
                TropicalCropPlantInstance[] plants = cropGroup.ToArray();

                groups.Add(new AdvisorCropGroupData
                {
                    cropType = TropicalCropCatalog.GetDisplayName(cropGroup.Key),
                    count = plants.Length,
                    averageStage = plants.GroupBy(x => x.stage).OrderByDescending(g => g.Count()).First().Key.ToString(),
                    averageAgeYears = plants.Average(x => x.GetAgeYears()),
                    averageHealth = plants.Average(x => x.health),
                    averageStress = plants.Average(x => x.stress),
                    averageWaterPercent = plants.Average(x => x.moisture * 100f),
                    averageDrainagePercent = plants.Average(x => x.drainage * 100f),
                    averageFertilityPercent = plants.Average(x => x.fertility * 100f),
                    averageSuitabilityPercent = plants.Average(x => x.soilSuitability * 100f)
                });
            }

            return groups;
        }

        private string GetMostCommonCoconutStage(CoconutTreeInstance[] trees)
        {
            return trees.GroupBy(x => x.stage)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key
                        .ToString();
        }

        private string GetMostCommonBananaStage(BananaPlantInstance[] trees)
        {
            return trees.GroupBy(x => x.stage)
                        .OrderByDescending(g => g.Count())
                        .First()
                        .Key
                        .ToString();
        }
    }
}
