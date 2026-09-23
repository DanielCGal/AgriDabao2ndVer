using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AgriDabao3D
{
    /// <summary>
    /// The planting half of the farming system: preparing ground, planting the
    /// sixteen planting materials, and transplanting seedlings from the Seedling
    /// Tent.
    ///
    /// The route is decided by the planting material, not the crop
    /// (<see cref="PlantingMaterialCatalog"/>). Every crop needs the same ground
    /// steps whichever way it arrives: the shovel tills bare ground, and tapping
    /// the tilled patch again turns it into a planting hole (tree crops, banana,
    /// coconut), a raised bed (tomato, eggplant, squash, pineapple, strawberry) or
    /// a furrow (corn). Strawberry also needs its bed covered with a Mulch Bag.
    /// </summary>
    public partial class FarmingInteractionSystem
    {
        [Header("Prepared Ground Visuals")]
        [Tooltip("Loose soil left by tilling. Falls back to the Dig Spot Prefab.")]
        public GameObject tilledGroundPrefab;

        [Tooltip("A planting hole with its dug-out soil beside it.")]
        public GameObject plantingHolePrefab;

        [Tooltip("The loose soil on top of a raised bed. The mound itself is the " +
                 "terrain, lifted the same way the Raised Bed Kit lifts it.")]
        public GameObject raisedBedPrefab;

        [Tooltip("An open furrow between two ridges of soil.")]
        public GameObject furrowPrefab;

        [Tooltip("The flat layer of mulch over a raised bed. Falls back to the Mulch " +
                 "Visual Prefab.")]
        public GameObject bedMulchPrefab;

        [Tooltip("Scale of the mulch laid over a raised bed, as a multiple of its " +
                 "prefab's own scale.")]
        public float bedMulchScale = 1f;

        [Tooltip("Radius of the mound a raised bed lifts, in metres. Matches the Raised Bed Kit.")]
        public float raisedBedRadius = 3.2f;

        [Tooltip("Height of that mound, in metres. Matches the Raised Bed Kit.")]
        public float raisedBedHeight = 0.6f;

        [Header("Seedling Tent")]
        public GameObject seedlingTentPrefab;
        public GameObject seedlingBagEmptyPrefab;
        public GameObject seedlingBagFilledPrefab;

        [Tooltip("Ground cannot be tilled closer than this to the Seedling Tent, in " +
                 "metres, so a raised bed's mound never lifts the tent.")]
        public float minDistanceFromSeedlingTent = 8f;

        [Header("Seedlings (in the tent, and just after transplanting)")]
        public GameObject cacaoSeedlingPrefab;
        public GameObject durianSeedlingPrefab;
        public GameObject mangosteenSeedlingPrefab;
        public GameObject pomeloSeedlingPrefab;
        public GameObject bananaPlantletPrefab;
        public GameObject mangoSeedlingPrefab;
        public GameObject tomatoSeedlingPrefab;
        public GameObject eggplantSeedlingPrefab;
        public GameObject squashSeedlingPrefab;

        [Header("Planting Materials (just after planting directly)")]
        public GameObject coconutSeednutPrefab;
        public GameObject bananaSuckerPrefab;
        public GameObject pineappleSuckerPrefab;
        public GameObject strawberryRunnerPrefab;
        public GameObject cornSeedlingPrefab;

        private Terrain ActiveTerrain =>
            terrainGenerator != null && terrainGenerator.targetTerrain != null
                ? terrainGenerator.targetTerrain
                : TemporaryTerrainGenerator.ResolveActiveTerrain();

        private static float CurrentGameDay =>
            GameTimeSystem.Instance != null ? GameTimeSystem.Instance.TotalGameDays : 0f;

        // ------------------------------------------------------------ tap routing

        /// <summary>
        /// Taps that belong to planting: the Seedling Tent, a transplant in
        /// progress, and prepared ground. Returns true when the tap was used.
        /// </summary>
        private bool TryHandlePlantingTap(RaycastHit hit, InventoryItemType selected)
        {
            if (hit.collider.GetComponentInParent<SeedlingTentInstance>() != null)
            {
                SeedlingTentUIBuilder.OpenPanel();
                return true;
            }

            DigSpot spot = hit.collider.GetComponentInParent<DigSpot>();

            NurserySystem nursery = NurserySystem.Instance;
            if (nursery != null && nursery.IsTransplanting && spot != null)
            {
                // The shovel still turns tilled ground into a hole, bed or furrow
                // while a seedling is being carried. Every tap used to go to the
                // transplant, so the seedling had to be put back first.
                if (selected == InventoryItemType.Shovel && spot.plotKind == PreparedPlotKind.Tilled)
                    return HandlePreparedGroundTap(spot, selected);

                return TryTransplantInto(spot);
            }

            return spot != null && HandlePreparedGroundTap(spot, selected);
        }

        private bool HandlePreparedGroundTap(DigSpot spot, InventoryItemType selected)
        {
            bool inReach = IsWithinReach(spot.transform.position);

            if (selected == InventoryItemType.Shovel)
            {
                if (!inReach)
                    return ReportOutOfReach();

                if (spot.plotKind == PreparedPlotKind.Tilled)
                    ShowPrepareChoice(spot);
                else
                    ConfirmFillIn(spot);

                return true;
            }

            if (selected == InventoryItemType.MulchBag)
                return inReach ? TryMulchBed(spot) : ReportOutOfReach();

            if (selected == InventoryItemType.RaisedBedKit)
            {
                ShowFarmingMessage(
                    "Raised beds for planting are built with the shovel: tap tilled ground " +
                    "with it and choose Raised Bed. The Raised Bed Kit is for a crop that " +
                    "is already growing without one.");
                return true;
            }

            InventoryItemType material = PlantingMaterialCatalog.UpgradeLegacy(selected);
            if (PlantingMaterialCatalog.TryGet(material, out PlantingMaterialInfo info))
                return inReach ? TryPlantMaterial(spot, selected, info) : ReportOutOfReach();

            ShowFarmingMessage(DescribePreparedGround(spot));
            return true;
        }

        /// <summary>A planting material tapped on bare ground: say what to do instead.</summary>
        private bool ReportBareGroundPlanting(InventoryItemType selected)
        {
            InventoryItemType material = PlantingMaterialCatalog.UpgradeLegacy(selected);
            if (!PlantingMaterialCatalog.TryGet(material, out PlantingMaterialInfo info))
                return false;

            if (!info.FieldReady)
            {
                ShowFarmingMessage(
                    info.Name + " starts in the Seedling Tent. Open the tent, fill a seedling " +
                    "bag with soil and sow it there; transplant it when it is ready.");
                return true;
            }

            ShowFarmingMessage(
                "You cannot plant straight into bare ground. Till it with the shovel, then " +
                "tap the tilled ground with the shovel again and choose " +
                PrepareVerb(info.Plot) + ".");
            return true;
        }

        private static string PrepareVerb(PreparedPlotKind kind)
        {
            switch (kind)
            {
                case PreparedPlotKind.Hole: return "Planting Hole";
                case PreparedPlotKind.RaisedBed: return "Raised Bed";
                case PreparedPlotKind.Furrow: return "Furrow";
                default: return "the right preparation";
            }
        }

        private string DescribePreparedGround(DigSpot spot)
        {
            string soil = spot.savedSoil != null
                ? "\nDistrict: " + spot.savedDistrict + "\nSoil Type: " + spot.savedSoil.GetVisualSoilType()
                : string.Empty;

            switch (spot.plotKind)
            {
                case PreparedPlotKind.Tilled:
                    return "Tilled ground." + soil +
                           "\nTap it with the shovel to dig a planting hole, build a raised bed " +
                           "or open a furrow.";
                case PreparedPlotKind.Hole:
                    return "A planting hole." + soil +
                           "\nFor cacao, durian, mangosteen, pomelo, banana, mango and coconut. " +
                           "Plant from your hotbar, or transplant a seedling from the Seedling Tent.";
                case PreparedPlotKind.RaisedBed:
                    return (spot.mulched ? "A mulched raised bed." : "A raised bed.") + soil +
                           "\nFor tomato, eggplant, squash, pineapple and strawberry." +
                           (spot.mulched ? string.Empty : " Strawberry needs the bed mulched first.");
                case PreparedPlotKind.Furrow:
                    return "An open furrow." + soil + "\nFor corn seed.";
                default:
                    return "Prepared ground." + soil;
            }
        }

        // -------------------------------------------------------------- tilling

        private bool TryTillGround(RaycastHit hit)
        {
            Terrain terrain = terrainGenerator.targetTerrain;
            TerrainCollider terrainCollider = terrain != null ? terrain.GetComponent<TerrainCollider>() : null;

            // The shovel only works the generated terrain.
            if (terrainCollider == null || hit.collider != terrainCollider)
                return false;

            if (!PlayerInventory.Instance.HasItem(InventoryItemType.Shovel, 1))
            {
                ShowFarmingMessage("You do not own a Shovel.");
                return true;
            }

            Vector3 groundPosition = hit.point;
            groundPosition.y = terrain.SampleHeight(groundPosition) + terrain.transform.position.y;

            // The tap ray runs the length of the map, so without a reach limit the
            // shovel works wherever the player happens to be looking.
            if (!IsWithinReach(groundPosition))
            {
                ShowFarmingMessage("That ground is too far away. Move closer to till it.");
                return true;
            }

            if (IsTooCloseToSeedlingTent(groundPosition))
            {
                ShowFarmingMessage("That is too close to the Seedling Tent. Till ground a few steps further away.");
                return true;
            }

            if (HasNearbyCrop(groundPosition))
            {
                ShowFarmingMessage("You cannot till too close to an existing crop.");
                return true;
            }

            if (HasNearbyDigSpot(groundPosition))
            {
                ShowFarmingMessage("There is already tilled or prepared ground nearby.");
                return true;
            }

            Vector3 terrainNormal = GetTerrainNormal(terrain, groundPosition);
            DigSpot spot = CreatePreparedGround(groundPosition, PlayerFacingYaw(), terrainNormal);

            // Soil is sampled now, while the ground is being worked.
            GetSoilForPlanting(groundPosition, out SoilSample soil, out string district);
            spot.Initialize(soil, district, groundPosition + Vector3.up * plantHeightOffset, terrainNormal);
            spot.plotKind = PreparedPlotKind.Tilled;
            spot.preparedGameDay = CurrentGameDay;

            ApplyPreparedGroundVisual(spot, terrain);
            GameAudioManager.Instance.PlayTill();

            ShowFarmingMessage(
                "Ground tilled.\n" +
                $"District: {district}\n" +
                $"Soil Type: {soil.GetVisualSoilType()}\n" +
                "Tap it again with the shovel to dig a planting hole, build a raised bed or open a furrow.");

            ReportFarmAction("Tilling");
            return true;
        }

        private bool IsTooCloseToSeedlingTent(Vector3 position)
        {
            SeedlingTentInstance tent = NurserySystem.Instance != null ? NurserySystem.Instance.Tent : null;
            if (tent == null)
                tent = Object.FindFirstObjectByType<SeedlingTentInstance>();

            if (tent == null)
                return false;

            Vector3 offset = tent.transform.position - position;
            offset.y = 0f;
            return offset.sqrMagnitude < minDistanceFromSeedlingTent * minDistanceFromSeedlingTent;
        }

        /// <summary>The direction the player is looking, flattened, so a furrow runs away from them.</summary>
        private float PlayerFacingYaw()
        {
            Transform view = playerCamera != null ? playerCamera.transform : null;
            return view != null ? view.eulerAngles.y : 0f;
        }

        /// <summary>
        /// The prepared-ground object itself: an invisible tap target carrying the
        /// soil record, with the model for its current preparation as a child.
        /// </summary>
        private DigSpot CreatePreparedGround(Vector3 groundPosition, float yaw, Vector3 terrainNormal)
        {
            GameObject root = new GameObject("PreparedGround_Runtime");
            root.transform.SetPositionAndRotation(
                groundPosition,
                Quaternion.FromToRotation(Vector3.up, terrainNormal) * Quaternion.Euler(0f, yaw, 0f));

            DigSpot spot = root.AddComponent<DigSpot>();

            // A trigger, so the player walks over prepared ground instead of into
            // it; taps still find it because the tap ray includes triggers.
            BoxCollider target = root.AddComponent<BoxCollider>();
            target.isTrigger = true;

            return spot;
        }

        /// <summary>Swaps in the model for the ground's current preparation and fits the tap target to it.</summary>
        private void ApplyPreparedGroundVisual(DigSpot spot, Terrain terrain)
        {
            if (spot == null)
                return;

            if (spot.visual != null)
                Destroy(spot.visual);

            GameObject prefab = spot.plotKind switch
            {
                PreparedPlotKind.Hole => plantingHolePrefab,
                PreparedPlotKind.RaisedBed => raisedBedPrefab != null ? raisedBedPrefab : tilledGroundPrefab,
                PreparedPlotKind.Furrow => furrowPrefab,
                _ => tilledGroundPrefab
            };

            if (prefab == null)
                prefab = digSpotPrefab;

            if (prefab != null)
            {
                GameObject visual = Instantiate(prefab, spot.transform);
                visual.name = prefab.name + "_Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = prefab.transform.localRotation;
                visual.transform.localScale = prefab.transform.localScale;
                StripPlantingVisual(visual);
                SnapObjectBottomToTerrain(visual, terrain, digSpotGroundOffset);
                DistanceCullable.Attach(visual);
                spot.visual = visual;
            }

            GameObject mulchPrefab = bedMulchPrefab != null ? bedMulchPrefab : mulchVisualPrefab;

            if (spot.mulched)
            {
                if (spot.mulchVisual == null && mulchPrefab != null)
                {
                    GameObject mulch = Instantiate(mulchPrefab, spot.transform);
                    mulch.name = "BedMulch_Visual";
                    mulch.transform.localPosition = Vector3.zero;
                    mulch.transform.localRotation = mulchPrefab.transform.localRotation;
                    mulch.transform.localScale = mulchPrefab.transform.localScale * Mathf.Max(0.05f, bedMulchScale);
                    StripPlantingVisual(mulch);
                    DistanceCullable.Attach(mulch);
                    spot.mulchVisual = mulch;
                }

                if (spot.mulchVisual != null)
                    SnapObjectBottomToTerrain(spot.mulchVisual, terrain, digSpotGroundOffset + 0.02f);
            }
            else if (spot.mulchVisual != null)
            {
                Destroy(spot.mulchVisual);
                spot.mulchVisual = null;
            }

            FitPreparedGroundTarget(spot);
        }

        private static void FitPreparedGroundTarget(DigSpot spot)
        {
            BoxCollider target = spot.GetComponent<BoxCollider>();
            if (target == null)
                return;

            Vector3 size = spot.plotKind switch
            {
                PreparedPlotKind.RaisedBed => new Vector3(2.6f, 0.9f, 2.6f),
                PreparedPlotKind.Furrow => new Vector3(1.4f, 0.7f, 2.8f),
                _ => new Vector3(1.9f, 0.7f, 1.9f)
            };

            target.size = size;
            target.center = new Vector3(0f, size.y * 0.35f, 0f);
        }

        /// <summary>
        /// Removes everything on a visual that would make it act like something
        /// else: a dug-spot component (which would steal taps from the real one),
        /// colliders and physics.
        /// </summary>
        private static void StripPlantingVisual(GameObject visual)
        {
            foreach (DigSpot copy in visual.GetComponentsInChildren<DigSpot>(true))
                DestroyImmediate(copy);
            foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true))
                DestroyImmediate(body);
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(collider);
        }

        // --------------------------------------------------- hole / bed / furrow

        private void ShowPrepareChoice(DigSpot spot)
        {
            List<FarmChoicePopup.Choice> choices = new List<FarmChoicePopup.Choice>
            {
                // Short enough for two lines under each plank; tapping the finished
                // ground lists every crop it takes.
                new FarmChoicePopup.Choice("Planting Hole",
                    "Fruit trees, banana, coconut",
                    () => PrepareGround(spot, PreparedPlotKind.Hole)),
                new FarmChoicePopup.Choice("Raised Bed",
                    "Vegetables, pineapple, strawberry",
                    () => PrepareGround(spot, PreparedPlotKind.RaisedBed)),
                new FarmChoicePopup.Choice("Furrow",
                    "Corn",
                    () => PrepareGround(spot, PreparedPlotKind.Furrow))
            };

            FarmChoicePopup.Instance.Show("Prepare this tilled ground for planting:", choices);
        }

        private void PrepareGround(DigSpot spot, PreparedPlotKind kind)
        {
            // The popup waits for the player, so the ground may be gone or changed.
            if (spot == null || spot.plotKind != PreparedPlotKind.Tilled)
                return;

            if (!IsWithinReach(spot.transform.position))
            {
                ReportOutOfReach();
                return;
            }

            if (PlayerInventory.Instance == null ||
                !PlayerInventory.Instance.HasItem(InventoryItemType.Shovel, 1))
            {
                ShowFarmingMessage("You do not own a Shovel.");
                return;
            }

            Terrain terrain = ActiveTerrain;

            if (kind == PreparedPlotKind.RaisedBed)
            {
                if (terrain == null || terrain.terrainData == null)
                {
                    ShowFarmingMessage("The ground is not ready yet. Try again in a moment.");
                    return;
                }

                // The heightmap can only lift its own vertices, so the bed's peak is
                // the vertex nearest the tilled patch; the patch moves onto it (by
                // at most half a vertex spacing) so it sits on top of its own bed.
                Vector3 peak = TerrainModificationService.SnapToHeightmapVertex(terrain, spot.transform.position);
                if (string.IsNullOrWhiteSpace(spot.bedPatchId))
                    spot.bedPatchId = Guid.NewGuid().ToString("N");

                TerrainModificationService.ApplyRaisedBed(terrain, peak, raisedBedRadius, raisedBedHeight, spot.bedPatchId);
                SeatOnRaisedBed(spot, terrain, peak);
            }

            spot.plotKind = kind;
            spot.preparedGameDay = CurrentGameDay;
            ApplyPreparedGroundVisual(spot, terrain);
            GameAudioManager.Instance.PlayDig();

            string done = kind switch
            {
                PreparedPlotKind.Hole => "Planting hole dug.",
                PreparedPlotKind.RaisedBed => "Raised bed built. Its roots will sit above standing water.",
                PreparedPlotKind.Furrow => "Furrow opened.",
                _ => "Ground prepared."
            };

            ShowFarmingMessage(done + "\n" + DescribePreparedGround(spot));
            ReportFarmAction("Preparing ground");
        }

        private void SeatOnRaisedBed(DigSpot spot, Terrain terrain, Vector3 peak)
        {
            peak.y = terrain.SampleHeight(peak) + terrain.transform.position.y;
            float yaw = spot.transform.eulerAngles.y;
            spot.transform.SetPositionAndRotation(peak, Quaternion.Euler(0f, yaw, 0f));
            spot.plantingPosition = peak + Vector3.up * plantHeightOffset;
            spot.terrainNormal = Vector3.up;
        }

        private void ConfirmFillIn(DigSpot spot)
        {
            string question = "Fill in this " + spot.DisplayName + " and return it to bare ground?" +
                              (spot.mulched ? "\nThe mulch on it will be lost." : string.Empty);

            if (FarmConfirmPopup.Instance != null)
                FarmConfirmPopup.Instance.Show(question, () => FillIn(spot));
            else
                FillIn(spot);
        }

        private void FillIn(DigSpot spot)
        {
            if (spot == null || spot.occupied)
                return;

            if (spot.plotKind == PreparedPlotKind.RaisedBed && !string.IsNullOrWhiteSpace(spot.bedPatchId))
            {
                Terrain terrain = ActiveTerrain;
                TerrainModificationService.RemoveRaisedBed(
                    terrain, spot.transform.position, raisedBedRadius, raisedBedHeight, spot.bedPatchId);
            }

            Destroy(spot.gameObject);
            GameAudioManager.Instance.PlayDig();
            ShowFarmingMessage("The ground was filled back in.");
        }

        private bool TryMulchBed(DigSpot spot)
        {
            if (spot.plotKind != PreparedPlotKind.RaisedBed)
            {
                ShowFarmingMessage(
                    "Mulch covers a raised bed before planting, or goes around a crop already growing.");
                return true;
            }

            if (spot.mulched)
            {
                ShowFarmingMessage("This bed is already mulched.");
                return true;
            }

            if (!PlayerInventory.Instance.HasItem(InventoryItemType.MulchBag, 1))
            {
                ShowFarmingMessage("You do not have a Mulch Bag.");
                return true;
            }

            if (!PlayerInventory.Instance.ConsumeItem(InventoryItemType.MulchBag, 1))
                return true;

            spot.mulched = true;
            ApplyPreparedGroundVisual(spot, ActiveTerrain);
            GameAudioManager.Instance.PlayPlaceMitigation();

            ShowFarmingMessage(
                "Bed mulched. Strawberry runners can be planted through it now, and any crop " +
                "planted here keeps the mulch.");
            ReportFarmAction("Mulching");
            return true;
        }

        // -------------------------------------------------------------- planting

        /// <summary>
        /// Why this material cannot go into this ground, or null when it can.
        /// <paramref name="name"/> is what the player is holding, when that is not
        /// the material itself - a seedling carried from the tent.
        /// </summary>
        private static string CheckGroundFor(DigSpot spot, PlantingMaterialInfo info, string name = null)
        {
            if (!spot.IsPrepared)
            {
                return "Prepare this ground first: tap it with the shovel and choose " +
                       PrepareVerb(info.Plot) + ".";
            }

            if (spot.plotKind != info.Plot)
            {
                return (name ?? info.Name) + " goes in a " + PlantingMaterialCatalog.PlotName(info.Plot) +
                       ", not a " + spot.DisplayName + ". Till new ground and choose " +
                       PrepareVerb(info.Plot) + ", or fill this one in with the shovel.";
            }

            if (info.NeedsMulchedBed && !spot.mulched)
            {
                return "Strawberry runners are planted through mulch. Cover this raised bed " +
                       "with a Mulch Bag first.";
            }

            return null;
        }

        private bool TryPlantMaterial(DigSpot spot, InventoryItemType heldItem, PlantingMaterialInfo info)
        {
            if (!info.FieldReady)
            {
                ShowFarmingMessage(
                    info.Name + " is raised in the Seedling Tent first. Sow it in a seedling bag " +
                    "there, then transplant the seedling into this " + spot.DisplayName + " when it is ready.");
                return true;
            }

            string problem = CheckGroundFor(spot, info);
            if (problem != null)
            {
                ShowFarmingMessage(problem);
                return true;
            }

            if (!PlayerInventory.Instance.HasItem(heldItem, 1))
            {
                ShowFarmingMessage("You do not have a " + info.Name + ".");
                return true;
            }

            float startingAge = PlantingMaterialCatalog.StartingAgeDays(info, 0f);

            GameObject root = PlantOnPreparedGround(
                spot, info, startingAge, 0f,
                () => PlayerInventory.Instance.ConsumeItem(heldItem, 1));

            if (root != null)
                GameAudioManager.Instance.PlayPlantSeed();

            return true;
        }

        private bool TryTransplantInto(DigSpot spot)
        {
            NurserySystem nursery = NurserySystem.Instance;
            if (nursery == null || !nursery.TryGetTransplant(out PlantingMaterialInfo info, out float daysInNursery))
            {
                nursery?.CancelTransplant();
                return false;
            }

            if (!IsWithinReach(spot.transform.position))
                return ReportOutOfReach();

            string problem = CheckGroundFor(spot, info, PlantingMaterialCatalog.SeedlingName(info));
            if (problem != null)
            {
                ShowFarmingMessage(problem);
                return true;
            }

            float startingAge = PlantingMaterialCatalog.StartingAgeDays(info, daysInNursery);
            GameObject root = PlantOnPreparedGround(spot, info, startingAge, daysInNursery, () => true);
            if (root == null)
                return true;

            nursery.FinishTransplant();
            GameAudioManager.Instance.PlayTransplant();

            CropRuntimeAdapter.TryCreate(root, out CropRuntimeAdapter crop);
            NurserySystem.RecordNurseryAction(
                "TransplantSeedling", info.Item.ToString(), info.Crop.ToString(),
                root.transform.position, crop != null ? crop.CropId : null,
                PlantingMaterialCatalog.SeedlingName(info) + " was transplanted after " +
                PlantingMaterialCatalog.FormatDays(daysInNursery) + " in the Seedling Tent.");

            return true;
        }

        /// <summary>
        /// Plants into prepared ground and returns the new crop, or null when it
        /// could not be planted (a message has already been shown). The material is
        /// only taken once the crop exists, and the crop is removed again if taking
        /// it fails, so nothing is lost or duplicated either way.
        /// </summary>
        private GameObject PlantOnPreparedGround(
            DigSpot spot,
            PlantingMaterialInfo info,
            float startingAgeDays,
            float nurseryDays,
            Func<bool> takeMaterial)
        {
            if (!spot.TryReserve())
            {
                ShowFarmingMessage("This planting spot is already being used.");
                return null;
            }

            GameObject root = CreateCrop(info, spot.plantingPosition, spot.GetSoilCopy(), spot.savedDistrict, nurseryDays);
            if (root == null)
            {
                spot.Release();
                return null;
            }

            if (HasOtherCropNearby(root))
            {
                Destroy(root);
                spot.Release();
                ShowFarmingMessage("A crop is already too close to this planting spot.");
                return null;
            }

            if (!takeMaterial())
            {
                Destroy(root);
                spot.Release();
                return null;
            }

            ApplyStartingAge(root, startingAgeDays);
            AdoptPreparedGround(root, spot);

            // The prepared ground becomes the crop's; its model goes with it.
            spot.Consume();

            GrowthStageVisualController visuals = root.GetComponent<GrowthStageVisualController>();
            if (visuals != null)
                visuals.ForceRefresh();

            if (soilAwareTerrain == null)
                soilAwareTerrain = Object.FindFirstObjectByType<SoilAwareTerrainGenerator>();
            if (soilAwareTerrain != null)
                soilAwareTerrain.ShowFullCropInfo(root);

            ReportFarmAction("Planting");
            return root;
        }

        /// <summary>
        /// A last spacing check between crop bases, measured on the ground plane.
        ///
        /// Tilling already keeps new ground <see cref="minDistanceBetweenTrees"/>
        /// away from every crop and every other patch, and every crop now starts on
        /// such a patch. A raised bed then moves its patch onto the nearest
        /// heightmap vertex - up to about two metres - so this allows for that
        /// shift rather than refusing a bed the player was allowed to build.
        /// </summary>
        private bool HasOtherCropNearby(GameObject newCrop)
        {
            Vector3 position = newCrop.transform.position;
            float minimum = Mathf.Max(0f, minDistanceBetweenTrees - 2f);
            float minSquared = minimum * minimum;

            foreach (CropRuntimeAdapter crop in CropRuntimeAdapter.FindAll())
            {
                if (crop == null || crop.Root == null || crop.Root == newCrop)
                    continue;

                Vector3 offset = crop.Transform.position - position;
                offset.y = 0f;
                if (offset.sqrMagnitude < minSquared)
                    return true;
            }

            return false;
        }

        private static void ApplyStartingAge(GameObject root, float ageDays)
        {
            if (ageDays <= 0f || root == null)
                return;

            CoconutTreeInstance coconut = root.GetComponent<CoconutTreeInstance>();
            if (coconut != null)
            {
                coconut.ApplyStartingAge(ageDays);
                return;
            }

            BananaPlantInstance banana = root.GetComponent<BananaPlantInstance>();
            if (banana != null)
            {
                banana.ApplyStartingAge(ageDays);
                return;
            }

            TropicalCropPlantInstance tropical = root.GetComponent<TropicalCropPlantInstance>();
            if (tropical != null)
                tropical.ApplyStartingAge(ageDays);
        }

        private void AdoptPreparedGround(GameObject root, DigSpot spot)
        {
            bool raisedBed = spot.plotKind == PreparedPlotKind.RaisedBed;
            if (!raisedBed && !spot.mulched)
                return;

            if (!CropRuntimeAdapter.TryCreate(root, out CropRuntimeAdapter crop))
                return;

            CropClimateMaintenanceState state = crop.GetOrAddMaintenanceState();
            state.AdoptPreparedGround(raisedBed, spot.bedPatchId, spot.mulched, ActiveTerrain);
        }

        /// <summary>
        /// Builds a crop of the material's kind at the planting position. Returns
        /// null (with a warning) when the crop's models are not assigned.
        /// </summary>
        private GameObject CreateCrop(
            PlantingMaterialInfo info,
            Vector3 plantingPosition,
            SoilSample soil,
            string district,
            float nurseryDays)
        {
            float today = CurrentGameDay;
            string materialName = info.Item.ToString();

            if (info.Crop == FarmCropType.Coconut)
            {
                PlantGrowthVisualSet visualSet = CropVisualSet(FarmCropType.Coconut, info.Item);
                if (!HasAnyVisualPrefab(visualSet))
                {
                    Debug.LogWarning("FarmingInteractionSystem: Coconut growth prefabs are not assigned.");
                    return null;
                }

                GameObject root = CreatePlantRoot("CoconutTree", plantingPosition);
                CoconutTreeInstance crop = root.AddComponent<CoconutTreeInstance>();
                crop.plantingMaterial = materialName;
                crop.nurseryDays = nurseryDays;
                crop.fieldPlantedGameDay = today;

                EnsurePestDiseaseComponent(root);
                AddGrowthVisuals(root, visualSet);
                EnsureCollider(root);

                crop.Initialize(soil, district);
                crop.cropName = CropNaming.NextName("Coconut");
                return root;
            }

            if (info.Crop == FarmCropType.Banana)
            {
                PlantGrowthVisualSet visualSet = CropVisualSet(FarmCropType.Banana, info.Item);
                if (!HasAnyVisualPrefab(visualSet))
                {
                    Debug.LogWarning("FarmingInteractionSystem: Banana growth prefabs are not assigned.");
                    return null;
                }

                GameObject root = CreatePlantRoot("BananaPlant", plantingPosition);
                BananaPlantInstance crop = root.AddComponent<BananaPlantInstance>();
                crop.plantingMaterial = materialName;
                crop.nurseryDays = nurseryDays;
                crop.fieldPlantedGameDay = today;

                EnsurePestDiseaseComponent(root);
                AddGrowthVisuals(root, visualSet);
                EnsureCollider(root);

                crop.Initialize(soil, district);
                crop.cropName = CropNaming.NextName("Banana");
                return root;
            }

            if (!TryGetTropicalKind(info.Crop, out TropicalCropKind kind))
            {
                Debug.LogWarning("FarmingInteractionSystem: no crop kind for " + info.Crop + ".");
                return null;
            }

            PlantGrowthVisualSet tropicalVisuals = CropVisualSet(info.Crop, info.Item);
            if (!HasAnyVisualPrefab(tropicalVisuals))
            {
                Debug.LogWarning(
                    $"FarmingInteractionSystem: {TropicalCropCatalog.GetDisplayName(kind)} growth prefabs are not assigned.");
                return null;
            }

            Vector3 spawnPosition = plantingPosition;
            spawnPosition.y += GetTropicalGroundOffset(kind);

            GameObject tropicalRoot = CreatePlantRoot($"{TropicalCropCatalog.GetDisplayName(kind)}Plant", spawnPosition);
            TropicalCropPlantInstance instance = tropicalRoot.AddComponent<TropicalCropPlantInstance>();
            instance.cropKind = kind;
            instance.plantingMaterial = materialName;
            instance.nurseryDays = nurseryDays;
            instance.fieldPlantedGameDay = today;

            EnsurePestDiseaseComponent(tropicalRoot);
            AddGrowthVisuals(tropicalRoot, tropicalVisuals);
            EnsureCollider(tropicalRoot);

            instance.Initialize(soil, district);
            instance.cropName = CropNaming.NextName(instance.CropDisplayName);
            return tropicalRoot;
        }

        private static bool TryGetTropicalKind(FarmCropType crop, out TropicalCropKind kind)
        {
            // The two enums share their names for every crop except coconut and
            // banana, which have classes of their own.
            return Enum.TryParse(crop.ToString(), out kind) &&
                   crop != FarmCropType.Coconut &&
                   crop != FarmCropType.Banana;
        }

        /// <summary>
        /// A crop's full set of stage models, plus the planting-material model it
        /// shows for its first days in the field.
        /// </summary>
        private PlantGrowthVisualSet CropVisualSet(FarmCropType crop, InventoryItemType material)
        {
            PlantGrowthVisualSet set;

            if (crop == FarmCropType.Coconut)
            {
                set = MakeVisualSet(coconutSproutPrefab, coconutSecondStagePrefab, coconutTreePrefab,
                    coconutSproutYOffset, coconutSecondStageYOffset);
            }
            else if (crop == FarmCropType.Banana)
            {
                set = MakeVisualSet(bananaSproutPrefab, bananaSecondStagePrefab, bananaTreePrefab,
                    bananaSproutYOffset, bananaSecondStageYOffset);
            }
            else if (TryGetTropicalKind(crop, out TropicalCropKind kind))
            {
                set = MakeVisualSet(
                    GetTropicalSproutPrefab(kind),
                    GetTropicalSecondStagePrefab(kind),
                    GetTropicalTreePrefab(kind),
                    GetTropicalSproutYOffset(kind),
                    GetTropicalSecondStageYOffset(kind));
            }
            else
            {
                set = new PlantGrowthVisualSet();
            }

            set.plantedPrefab = GetPlantedMaterialPrefab(crop, material);
            set.plantedBuryFraction = GetPlantedBuryFraction(material);
            return set;
        }

        /// <summary>
        /// The model a crop shows for its first days in the field: the seednut,
        /// sucker or runner itself, or the seedling as it looked in its bag.
        /// </summary>
        public GameObject GetPlantedMaterialPrefab(FarmCropType crop, InventoryItemType material)
        {
            switch (material)
            {
                case InventoryItemType.CoconutSeednut: return coconutSeednutPrefab;
                case InventoryItemType.BananaSucker: return bananaSuckerPrefab;
                case InventoryItemType.PineappleSucker: return pineappleSuckerPrefab;
                case InventoryItemType.StrawberryRunner: return strawberryRunnerPrefab;
                case InventoryItemType.CornSeed: return cornSeedlingPrefab;
            }

            return GetSeedlingPrefab(crop);
        }

        /// <summary>The seedling model shown in a Seedling Tent bag, per crop.</summary>
        public GameObject GetSeedlingPrefab(FarmCropType crop)
        {
            switch (crop)
            {
                case FarmCropType.Cacao: return cacaoSeedlingPrefab;
                case FarmCropType.Durian: return durianSeedlingPrefab;
                case FarmCropType.Mangosteen: return mangosteenSeedlingPrefab;
                case FarmCropType.Pomelo: return pomeloSeedlingPrefab;
                case FarmCropType.Banana: return bananaPlantletPrefab;
                case FarmCropType.Mango: return mangoSeedlingPrefab;
                case FarmCropType.Tomato: return tomatoSeedlingPrefab;
                case FarmCropType.Eggplant: return eggplantSeedlingPrefab;
                case FarmCropType.Squash: return squashSeedlingPrefab;
                case FarmCropType.Corn: return cornSeedlingPrefab;
                default: return null;
            }
        }

        /// <summary>
        /// How much of each planting-material model goes under the soil. Every one
        /// of these models includes its roots, which make up the bottom quarter to
        /// two-fifths of it, so they are sunk until the roots are hidden.
        /// </summary>
        private static float GetPlantedBuryFraction(InventoryItemType material)
        {
            switch (material)
            {
                // Roots and the lower half of the nut, leaving part of it showing
                // as the notes describe.
                case InventoryItemType.CoconutSeednut: return 0.42f;
                case InventoryItemType.BananaSucker: return 0.2f;
                case InventoryItemType.PineappleSucker: return 0.25f;
                case InventoryItemType.StrawberryRunner: return 0.32f;
                case InventoryItemType.CornSeed: return 0.3f;
                default: return SeedlingRootShare;
            }
        }

        /// <summary>Share of a seedling model's height taken up by its roots and seed.</summary>
        public const float SeedlingRootShare = 0.38f;

        /// <summary>Restores a crop's planting-material look and protection after a farm load.</summary>
        private void RestorePlantingDetails(GameObject root, CropSaveDto save)
        {
            CropPersistenceMapper.RestoreProtection(root, save, fruitBagPrefab);
        }

        // ------------------------------------------------------ saving and loading

        public List<PreparedPlotSaveDto> CapturePreparedGround()
        {
            List<PreparedPlotSaveDto> result = new List<PreparedPlotSaveDto>();

            foreach (DigSpot spot in Object.FindObjectsByType<DigSpot>(FindObjectsSortMode.None))
            {
                // Ground already planted is gone; ground being planted this very
                // frame belongs to its crop.
                if (spot == null || spot.occupied || spot.plotKind == PreparedPlotKind.None)
                    continue;

                result.Add(new PreparedPlotSaveDto
                {
                    plotId = spot.plotId,
                    plotKind = spot.plotKind.ToString(),
                    mulched = spot.mulched,
                    bedPatchId = spot.bedPatchId,
                    preparedGameDay = spot.preparedGameDay,
                    position = new SerializableVector3(spot.transform.position),
                    rotation = new SerializableQuaternion(spot.transform.rotation),
                    terrainNormal = new SerializableVector3(spot.terrainNormal),
                    soil = spot.GetSoilCopy(),
                    districtName = spot.savedDistrict
                });
            }

            return result;
        }

        public void RestorePreparedGround(List<PreparedPlotSaveDto> saved)
        {
            if (saved == null)
                return;

            Terrain terrain = ActiveTerrain;

            foreach (PreparedPlotSaveDto save in saved)
            {
                if (save == null ||
                    !Enum.TryParse(save.plotKind, out PreparedPlotKind kind) ||
                    kind == PreparedPlotKind.None)
                {
                    continue;
                }

                Vector3 position = save.position.ToVector3();
                Vector3 normal = save.terrainNormal.ToVector3();
                if (normal.sqrMagnitude < 0.001f)
                    normal = Vector3.up;

                // The heightmap is rebuilt bare on every load, so the ground is
                // sampled afresh rather than trusting the saved height.
                if (terrain != null && terrain.terrainData != null)
                    position.y = terrain.SampleHeight(position) + terrain.transform.position.y;

                float yaw = save.rotation.ToQuaternion().eulerAngles.y;
                DigSpot spot = CreatePreparedGround(position, yaw, normal);
                spot.plotId = save.plotId;
                spot.Initialize(save.soil, save.districtName, position + Vector3.up * plantHeightOffset, normal);
                spot.plotKind = kind;
                spot.mulched = save.mulched && kind == PreparedPlotKind.RaisedBed;
                spot.bedPatchId = save.bedPatchId;
                spot.preparedGameDay = save.preparedGameDay;

                if (kind == PreparedPlotKind.RaisedBed && terrain != null && terrain.terrainData != null)
                {
                    if (string.IsNullOrWhiteSpace(spot.bedPatchId))
                        spot.bedPatchId = Guid.NewGuid().ToString("N");

                    Vector3 peak = TerrainModificationService.SnapToHeightmapVertex(terrain, position);
                    TerrainModificationService.ApplyRaisedBed(terrain, peak, raisedBedRadius, raisedBedHeight, spot.bedPatchId);
                    SeatOnRaisedBed(spot, terrain, peak);
                }

                ApplyPreparedGroundVisual(spot, terrain);
            }
        }
    }
}
