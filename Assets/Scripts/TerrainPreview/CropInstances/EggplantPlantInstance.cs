using UnityEngine;

namespace AgriDabao3D
{
    public class EggplantPlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Eggplant;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Eggplant;
        }
    }
}
