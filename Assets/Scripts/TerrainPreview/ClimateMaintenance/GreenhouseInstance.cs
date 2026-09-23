using UnityEngine;

namespace AgriDabao3D
{
    [DisallowMultipleComponent]
    public sealed class GreenhouseInstance : ClimateMitigationWorldObject
    {
        private void Reset()
        {
            ApplyRecommendedConfiguration(ClimateWorldMitigationType.Greenhouse);
        }

        private void OnValidate()
        {
            mitigationType = ClimateWorldMitigationType.Greenhouse;
            ValidateConfiguration();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawRadiusGizmo(new Color(0.70f, 0.95f, 1f, 1f));
        }
#endif
    }
}
