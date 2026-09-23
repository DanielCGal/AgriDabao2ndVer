using UnityEngine;

namespace AgriDabao3D
{
    public class MangosteenPlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Mangosteen;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Mangosteen;
        }
    }
}
