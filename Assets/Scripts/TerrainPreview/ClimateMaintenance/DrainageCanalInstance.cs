using UnityEngine;

namespace AgriDabao3D
{
    [DisallowMultipleComponent]
    public sealed class DrainageCanalInstance : ClimateMitigationWorldObject
    {
        private void Reset()
        {
            ApplyRecommendedConfiguration(ClimateWorldMitigationType.DrainageCanal);
        }

        private void OnValidate()
        {
            mitigationType = ClimateWorldMitigationType.DrainageCanal;
            ValidateConfiguration();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawRadiusGizmo(new Color(0.25f, 0.55f, 1f, 1f));
        }
#endif
    }
}
