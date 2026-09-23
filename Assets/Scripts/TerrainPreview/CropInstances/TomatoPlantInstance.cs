using UnityEngine;

namespace AgriDabao3D
{
    public class TomatoPlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Tomato;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Tomato;
        }
    }
}
