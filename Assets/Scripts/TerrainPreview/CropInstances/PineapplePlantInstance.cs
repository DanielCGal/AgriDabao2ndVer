using UnityEngine;

namespace AgriDabao3D
{
    public class PineapplePlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Pineapple;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Pineapple;
        }
    }
}
