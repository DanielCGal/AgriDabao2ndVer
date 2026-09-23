using UnityEngine;

namespace AgriDabao3D
{
    [DisallowMultipleComponent]
    public sealed class IrrigationSystemInstance : ClimateMitigationWorldObject
    {
        private void Reset()
        {
            ApplyRecommendedConfiguration(ClimateWorldMitigationType.IrrigationSystem);
        }

        private void OnValidate()
        {
            mitigationType = ClimateWorldMitigationType.IrrigationSystem;
            ValidateConfiguration();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawRadiusGizmo(new Color(0.20f, 0.65f, 1f, 1f));
        }
#endif
    }
}
