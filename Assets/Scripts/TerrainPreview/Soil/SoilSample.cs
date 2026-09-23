using UnityEngine;

namespace AgriDabao3D
{
    [System.Serializable]
    public class SoilSample
    {
        public float sand;
        public float silt;
        public float clay;
        public float phh2o;
        public float soc;
        public float cfvo;
        public float bdod;
        public float nitrogen;

        public string GetVisualSoilType()
        {
            if (cfvo >= 35f) return "Rocky";
            if (soc >= 40f) return "Organic-rich";
            if (clay >= 40f) return "Clayey";
            if (sand >= 60f) return "Sandy";
            if (silt >= 50f) return "Silty";
            return "Loamy";
        }
    }
}
