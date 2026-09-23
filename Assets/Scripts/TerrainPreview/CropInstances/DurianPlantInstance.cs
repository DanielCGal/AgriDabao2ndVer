using UnityEngine;

namespace AgriDabao3D
{
    public class DurianPlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Durian;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Durian;
        }
    }
}
