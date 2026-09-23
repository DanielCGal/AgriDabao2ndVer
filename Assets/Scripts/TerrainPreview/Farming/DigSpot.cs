using System;
using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// What a patch of prepared ground currently is.
    ///
    /// The shovel tills bare ground first; tapping the tilled patch again turns it
    /// into the preparation the crop needs. Values are saved by name, so new ones
    /// can go anywhere, but keep None first as the default.
    /// </summary>
    public enum PreparedPlotKind
    {
        None,
        Tilled,
        Hole,
        RaisedBed,
        Furrow
    }

    /// <summary>
    /// A patch of prepared ground: tilled, then dug into a planting hole, built
    /// into a raised bed or opened into a furrow. It carries the soil sampled when
    /// it was dug, so the crop planted here gets the soil of this spot.
    ///
    /// Kept under its old name so the DigSpot prefab and every system that already
    /// looks for dug spots - the tutorial, the daily tasks, the task observer -
    /// keep working.
    /// </summary>
    public class DigSpot : MonoBehaviour
    {
        [Header("Saved Soil Information")]
        public SoilSample savedSoil;
        public string savedDistrict;

        [Header("Planting Information")]
        public Vector3 plantingPosition;
        public Vector3 terrainNormal = Vector3.up;

        [Header("Prepared Ground")]
        public PreparedPlotKind plotKind = PreparedPlotKind.Tilled;

        [Tooltip("A raised bed covered with a Mulch Bag, which strawberry runners need.")]
        public bool mulched;

        [Tooltip("Stable id used by the farm save.")]
        public string plotId;

        [Tooltip("Terrain patch a raised bed lifted, so a reload can lift the same ground again.")]
        public string bedPatchId;

        [Tooltip("Game day the ground was last prepared.")]
        public float preparedGameDay;

        [Header("State")]
        public bool occupied;

        /// <summary>The model currently shown for this preparation.</summary>
        [NonSerialized] public GameObject visual;

        /// <summary>The mulch laid over a raised bed.</summary>
        [NonSerialized] public GameObject mulchVisual;

        /// <summary>Ready to take a crop: a hole, a bed or a furrow, not just tilled ground.</summary>
        public bool IsPrepared =>
            plotKind == PreparedPlotKind.Hole ||
            plotKind == PreparedPlotKind.RaisedBed ||
            plotKind == PreparedPlotKind.Furrow;

        public string DisplayName => PlantingMaterialCatalog.PlotName(plotKind);

        public void Initialize(
            SoilSample soil,
            string district,
            Vector3 savedPlantingPosition,
            Vector3 savedTerrainNormal)
        {
            savedSoil = CopySoilSample(soil);
            savedDistrict = district;

            plantingPosition = savedPlantingPosition;

            terrainNormal = savedTerrainNormal.sqrMagnitude > 0.001f
                ? savedTerrainNormal.normalized
                : Vector3.up;

            if (string.IsNullOrWhiteSpace(plotId))
                plotId = Guid.NewGuid().ToString("N");

            occupied = false;
        }

        public bool TryReserve()
        {
            if (occupied)
                return false;

            occupied = true;
            return true;
        }

        public void Release()
        {
            occupied = false;
        }

        public SoilSample GetSoilCopy()
        {
            return CopySoilSample(savedSoil);
        }

        public void Consume()
        {
            Destroy(gameObject);
        }

        private SoilSample CopySoilSample(SoilSample source)
        {
            if (source == null)
            {
                return new SoilSample
                {
                    sand = 45f,
                    silt = 28f,
                    clay = 27f,
                    phh2o = 6.1f,
                    soc = 22f,
                    cfvo = 6f,
                    bdod = 125f,
                    nitrogen = 14f
                };
            }

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
