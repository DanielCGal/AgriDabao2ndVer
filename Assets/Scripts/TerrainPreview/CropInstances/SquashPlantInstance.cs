using UnityEngine;

namespace AgriDabao3D
{
    public class SquashPlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Squash;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Squash;
        }
    }
}
