using UnityEngine;

namespace AgriDabao3D
{
    [DisallowMultipleComponent]
    public sealed class WaterStorageTankInstance : ClimateMitigationWorldObject
    {
        private void Reset()
        {
            ApplyRecommendedConfiguration(ClimateWorldMitigationType.WaterStorageTank);
        }

        private void OnValidate()
        {
            mitigationType = ClimateWorldMitigationType.WaterStorageTank;
            ValidateConfiguration();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawRadiusGizmo(new Color(0.10f, 0.80f, 0.95f, 1f));
        }
#endif
    }
}
