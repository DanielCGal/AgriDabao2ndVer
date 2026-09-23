using UnityEngine;

namespace AgriDabao3D
{
    [DisallowMultipleComponent]
    public sealed class WindbreakInstance : ClimateMitigationWorldObject
    {
        private void Reset()
        {
            ApplyRecommendedConfiguration(ClimateWorldMitigationType.Windbreak);
        }

        private void OnValidate()
        {
            mitigationType = ClimateWorldMitigationType.Windbreak;
            ValidateConfiguration();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawRadiusGizmo(new Color(0.95f, 0.80f, 0.20f, 1f));
        }
#endif
    }
}
