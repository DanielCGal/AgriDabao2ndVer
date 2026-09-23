using UnityEngine;

namespace AgriDabao3D
{
    public class DigSpot : MonoBehaviour
    {
        [Header("Saved Soil Information")]
        public SoilSample savedSoil;
        public string savedDistrict;

        [Header("Planting Information")]
        public Vector3 plantingPosition;
        public Vector3 terrainNormal = Vector3.up;

        [Header("State")]
        public bool occupied;

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
