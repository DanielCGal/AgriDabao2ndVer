using UnityEngine;

namespace AgriDabao3D
{
    public class StrawberryPlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Strawberry;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Strawberry;
        }
    }
}
