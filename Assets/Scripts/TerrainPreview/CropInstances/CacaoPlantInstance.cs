using UnityEngine;

namespace AgriDabao3D
{
    public class CacaoPlantInstance : TropicalCropPlantInstance
    {
        private void Awake()
        {
            cropKind = TropicalCropKind.Cacao;
        }

        private void Reset()
        {
            cropKind = TropicalCropKind.Cacao;
        }
    }
}
