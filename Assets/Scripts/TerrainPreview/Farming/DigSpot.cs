using System;
using UnityEngine;

namespace AgriDabao3D
{
    public enum PreparedPlotKind
    {
        None,
        Tilled,
        Hole,
        RaisedBed,
        Furrow
    }

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

        [NonSerialized] public GameObject visual;

        [NonSerialized] public GameObject mulchVisual;

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
