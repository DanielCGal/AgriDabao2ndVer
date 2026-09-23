using UnityEngine;

namespace AgriDabao3D
{
    public class PomeloPlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Pomelo;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Pomelo;
        }
    }
}
