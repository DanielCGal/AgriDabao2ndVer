using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
namespace AgriDabao3D
{
    public class FarmPersistenceManager : MonoBehaviour
    {
        public static FarmPersistenceManager Instance { get; private set; }
        public const int CurrentSchemaVersion = 3;
        public const string CurrentGeneratorVersion = "davao-terrain-v1";
        [Header("Scene References")]
        public FarmingInteractionSystem farmingSystem;
        public SoilAwareTerrainGenerator soilAwareTerrain;
        public TemporaryTerrainGenerator terrainGenerator;
        public Transform player;
        public ShippingBinSpawner shippingBinSpawner;
        public bool IsWorldReady { get; private set; }
        public bool IsSaving { get; private set; }
        public string LastStatus { get; private set; }
        private SoilSample sampleA;
        private SoilSample sampleB;
        private Vector2 sampleAPosition;
        private Vector2 sampleBPosition;
        private void Awake()
        {
            Instance = this;
            FarmTaskRuntimeBootstrap.EnsureInstalled();
        }
        private void Start()
        {
            ResolveReferences();
            ClimateMaintenanceRuntimeBootstrap.EnsureInstalled();
            FarmTaskRuntimeBootstrap.EnsureInstalled();
        }
        public void NotifyWorldReady(SoilSample loadedSampleA, SoilSample loadedSampleB,
            Vector2 loadedSampleAPosition, Vector2 loadedSampleBPosition)
        {
            sampleA = loadedSampleA;
            sampleB = loadedSampleB;
            sampleAPosition = loadedSampleAPosition;
            sampleBPosition = loadedSampleBPosition;
            ClimateMaintenanceRuntimeBootstrap.EnsureInstalled();
            FarmTaskRuntimeBootstrap.EnsureInstalled();
            if (FarmLoadContext.IsRestoring) RestoreGameplayState(FarmLoadContext.PendingSnapshot);
            IsWorldReady = true;
            if (FarmLoadContext.IsRestoring)
            {
                FarmLoadContext.Clear();
                LastStatus = "Farm loaded, including daily tasks, the AI Adviser task, recorded actions, climate events, and mitigation objects.";
            }
            else LastStatus = "Farm ready. Use Save Farm when you want to store it.";
        }
        /// <param name="allowDuringTutorial">
        /// Set by the marketplace and trade flows, which must sync the farm before
        /// the server will validate an offer against it. Refusing those saves left
        /// the trade session unprepared, and because the failure only surfaced as a
        /// status string the player just saw items refusing to go into the box.
        /// </param>
        public IEnumerator SaveFarm(bool initialSave = false, bool allowDuringTutorial = false)
        {
            if (IsSaving || !IsWorldReady) yield break;
            // Step 14 has the player open the save panel to look at it, and
            // Antonio says he will handle the saving himself before he goes - which
            // he does, from Finish(), after clearing this flag. A save landing
            // mid-tour would also write a half-finished tutorial as the farm's
            // permanent state.
            if (TutorialState.IsRunning && !allowDuringTutorial)
            {
                LastStatus = "Antonio will save the farm for you before he leaves.";
                yield break;
            }
            if (AuthSession.Instance == null || !AuthSession.Instance.IsAuthenticated)
            {
                LastStatus = "You must be logged in before saving.";
                yield break;
            }
            IsSaving = true;
            LastStatus = initialSave ? "Creating first farm save..." : "Saving farm...";
            FarmSnapshotDto snapshot = CaptureSnapshot();
            long expectedRevision = AuthSession.Instance.LoadedFarm != null ? AuthSession.Instance.LoadedFarm.revision : 0;
            FarmSaveRequestDto request = new FarmSaveRequestDto
            {
                expectedRevision = expectedRevision,
                schemaVersion = CurrentSchemaVersion,
                generatorVersion = CurrentGeneratorVersion,
                snapshot = snapshot
            };
            string error = null;
            long errorCode = 0;
            FarmSaveResponseDto saved = null;
            yield return ApiClient.Instance.PutJson<FarmSaveRequestDto, FarmSaveResponseDto>(
                "/api/farms/me", request, AuthSession.Instance.AccessToken,
                response => saved = response,
                (message, code) => { error = message; errorCode = code; });
            IsSaving = false;
            if (saved != null)
            {
                AuthSession.Instance.ApplySavedFarm(saved);
                LastStatus = "Farm saved successfully.";
                yield break;
            }
            LastStatus = errorCode == 409
                ? "This farm was changed on another device. Log in again before overwriting it."
                : "Farm save failed: " + error;
        }
        public FarmSnapshotDto CaptureSnapshot()
        {
            ResolveReferences();
            FarmSnapshotDto snapshot = new FarmSnapshotDto();
            Rect area = SelectedAreaState.NormalizedRect;
            snapshot.area.x = area.x;
            snapshot.area.y = area.y;
            snapshot.area.width = area.width;
            snapshot.area.height = area.height;
            snapshot.area.districtName = SelectedAreaState.SelectedDistrictName;
            snapshot.area.terrainSeed = CalculateTerrainSeed(area);
            snapshot.soil.sampleALatitude = sampleAPosition.x;
            snapshot.soil.sampleALongitude = sampleAPosition.y;
            snapshot.soil.sampleA = CloneSoil(sampleA);
            snapshot.soil.sampleBLatitude = sampleBPosition.x;
            snapshot.soil.sampleBLongitude = sampleBPosition.y;
            snapshot.soil.sampleB = CloneSoil(sampleB);
            if (player != null)
            {
                snapshot.player.position = new SerializableVector3(player.position);
                snapshot.player.rotation = new SerializableQuaternion(player.rotation);
            }
            if (PlayerInventory.Instance != null) snapshot.inventory = PlayerInventory.Instance.CaptureSaveData();
            if (GameTimeSystem.Instance != null)
            {
                snapshot.gameTime.totalGameDays = GameTimeSystem.Instance.TotalGameDays;
                snapshot.gameTime.daysPerMonth = GameTimeSystem.Instance.daysPerMonth;
                snapshot.gameTime.monthsPerYear = GameTimeSystem.Instance.monthsPerYear;
            }
            if (WeatherSystem.Instance != null)
            {
                WeatherSystem weather = WeatherSystem.Instance;
                snapshot.weather.currentEvent = weather.currentEvent.ToString();
                snapshot.weather.currentTemperatureC = weather.currentTemperatureC;
                snapshot.weather.currentHumidity = weather.currentHumidity;
                snapshot.weather.currentRainIntensity = weather.currentRainIntensity;
                snapshot.weather.remainingEventDays = weather.remainingEventDays;
                snapshot.weather.dailyLowTemperatureC = weather.dailyLowTemperatureC;
                snapshot.weather.dailyHighTemperatureC = weather.dailyHighTemperatureC;
            }
            if (ClimateEventTracker.Instance != null) snapshot.climateEvent = ClimateEventTracker.Instance.CaptureSaveData();
            if (DailyTaskSystem.Instance != null) snapshot.dailyTasks = DailyTaskSystem.Instance.CaptureSaveData();
            if (AIAdvisorTaskSystem.Instance != null) snapshot.aiAdvisorTask = AIAdvisorTaskSystem.Instance.CaptureSaveData();
            CaptureCrops(snapshot.crops);
            CaptureWorldObjects(snapshot.worldObjects);
            snapshot.tutorial = TutorialState.Capture();
            return snapshot;
        }
        private void RestoreGameplayState(FarmSnapshotDto snapshot)
        {
            if (snapshot == null) return;
            // Restored first: PlayerInventory and the tutorial director both read
            // this while coming up, and a farm saved before the tutorial existed
            // must be marked complete before either of them looks.
            TutorialState.Restore(snapshot.tutorial);
            ResolveReferences();
            ClimateMaintenanceRuntimeBootstrap.EnsureInstalled();
            FarmTaskRuntimeBootstrap.EnsureInstalled();
            if (GameTimeSystem.Instance != null) GameTimeSystem.Instance.RestoreTime(snapshot.gameTime);
            if (WeatherSystem.Instance != null) WeatherSystem.Instance.RestoreWeather(snapshot.weather);
            if (PlayerInventory.Instance != null) PlayerInventory.Instance.RestoreSaveData(snapshot.inventory);
            if (player != null)
            {
                player.position = snapshot.player.position.ToVector3();
                player.rotation = snapshot.player.rotation.ToQuaternion();
            }
            if (farmingSystem != null && snapshot.crops != null)
                foreach (CropSaveDto crop in snapshot.crops) farmingSystem.RestoreCropFromSave(crop);
            bool shippingBinWasRestored = false;
            Terrain terrain = TemporaryTerrainGenerator.ResolveActiveTerrain();
            if (snapshot.worldObjects != null)
            {
                foreach (WorldObjectSaveDto worldObject in snapshot.worldObjects)
                {
                    if (worldObject == null) continue;
                    if (string.Equals(worldObject.objectType, "ShippingBin", StringComparison.OrdinalIgnoreCase))
                    {
                        if (shippingBinSpawner != null)
                        {
                            GameObject restoredBin = shippingBinSpawner.RestoreBin(
                                worldObject.position.ToVector3(), worldObject.rotation.ToQuaternion());
                            shippingBinWasRestored = restoredBin != null;
                        }
                        continue;
                    }
                    if (ClimateMitigationWorldObject.IsClimateSave(worldObject))
                    {
                        ClimateMitigationWorldObject.RestoreFromSave(worldObject, terrain);
                        continue;
                    }
                    if (farmingSystem != null) farmingSystem.RestoreWorldObjectFromSave(worldObject);
                }
            }
            if (!shippingBinWasRestored && shippingBinSpawner != null) shippingBinSpawner.SpawnBinNearPlayerNow();
            if (ClimateEventTracker.Instance != null) ClimateEventTracker.Instance.RestoreSaveData(snapshot.climateEvent);
            if (DailyTaskSystem.Instance != null) DailyTaskSystem.Instance.RestoreSaveData(snapshot.dailyTasks);
            if (AIAdvisorTaskSystem.Instance != null) AIAdvisorTaskSystem.Instance.RestoreSaveData(snapshot.aiAdvisorTask);
            FarmTaskActionObserver.Instance?.ResetBaseline();
        }
        private static void CaptureCrops(List<CropSaveDto> destination)
        {
            foreach (CoconutTreeInstance crop in Object.FindObjectsByType<CoconutTreeInstance>(FindObjectsSortMode.None))
                if (crop != null) destination.Add(CropPersistenceMapper.Capture(crop));
            foreach (BananaPlantInstance crop in Object.FindObjectsByType<BananaPlantInstance>(FindObjectsSortMode.None))
                if (crop != null) destination.Add(CropPersistenceMapper.Capture(crop));
            foreach (TropicalCropPlantInstance crop in Object.FindObjectsByType<TropicalCropPlantInstance>(FindObjectsSortMode.None))
                if (crop != null) destination.Add(CropPersistenceMapper.Capture(crop));
        }
        private void CaptureWorldObjects(List<WorldObjectSaveDto> destination)
        {
            if (shippingBinSpawner != null && shippingBinSpawner.SpawnedBin != null)
            {
                Transform bin = shippingBinSpawner.SpawnedBin.transform;
                destination.Add(new WorldObjectSaveDto
                {
                    objectType = "ShippingBin",
                    position = new SerializableVector3(bin.position),
                    rotation = new SerializableQuaternion(bin.rotation)
                });
            }
            foreach (AphidTrapInstance trap in Object.FindObjectsByType<AphidTrapInstance>(FindObjectsSortMode.None))
            {
                if (trap == null) continue;
                destination.Add(new WorldObjectSaveDto
                {
                    objectType = "AphidTrap",
                    position = new SerializableVector3(trap.transform.position),
                    rotation = new SerializableQuaternion(trap.transform.rotation),
                    currentLoad = trap.deadAphidLoadPercent,
                    capacity = trap.capacityPercent,
                    radius = trap.radius
                });
            }
            foreach (AreaMitigationTrapInstance trap in Object.FindObjectsByType<AreaMitigationTrapInstance>(FindObjectsSortMode.None))
            {
                if (trap == null) continue;
                destination.Add(new WorldObjectSaveDto
                {
                    objectType = "AreaMitigationTrap",
                    position = new SerializableVector3(trap.transform.position),
                    rotation = new SerializableQuaternion(trap.transform.rotation),
                    currentLoad = trap.capturedLoadPercent,
                    capacity = trap.capacityPercent,
                    radius = trap.radius,
                    mitigation = trap.mitigation.ToString()
                });
            }
            foreach (ClimateMitigationWorldObject item in Object.FindObjectsByType<ClimateMitigationWorldObject>(FindObjectsSortMode.None))
                if (item != null) destination.Add(item.CaptureSaveData());
        }
        private void ResolveReferences()
        {
            if (farmingSystem == null) farmingSystem = Object.FindFirstObjectByType<FarmingInteractionSystem>();
            if (soilAwareTerrain == null) soilAwareTerrain = Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();
            if (terrainGenerator == null) terrainGenerator = Object.FindFirstObjectByType<TemporaryTerrainGenerator>();
            if (shippingBinSpawner == null) shippingBinSpawner = Object.FindFirstObjectByType<ShippingBinSpawner>();
            if (player == null)
            {
                FirstPersonTerrainController controller = Object.FindFirstObjectByType<FirstPersonTerrainController>();
                if (controller != null) player = controller.transform;
            }
        }
        public static int CalculateTerrainSeed(Rect rect)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Mathf.RoundToInt(rect.x * 100000f);
                hash = hash * 31 + Mathf.RoundToInt(rect.y * 100000f);
                hash = hash * 31 + Mathf.RoundToInt(rect.width * 100000f);
                hash = hash * 31 + Mathf.RoundToInt(rect.height * 100000f);
                return hash;
            }
        }
        private static SoilSample CloneSoil(SoilSample source)
        {
            if (source == null) return null;
            return new SoilSample
            {
                sand = source.sand,
                silt = source.silt,
                clay = source.clay,
                phh2o = source.phh2o,
                soc = source.soc,
                cfvo = source.cfvo,
                bdod = source.bdod,
                nitrogen = source.nitrogen
            };
        }
    }
}
