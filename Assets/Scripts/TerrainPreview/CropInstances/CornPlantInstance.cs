using UnityEngine;

namespace AgriDabao3D
{
    public class CornPlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Corn;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Corn;
        }
    }
}
