using UnityEngine;

namespace AgriDabao3D
{
    public class MangoPlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Mango;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Mango;
        }
    }
}
