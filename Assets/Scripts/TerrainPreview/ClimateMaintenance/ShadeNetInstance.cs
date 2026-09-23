using UnityEngine;

namespace AgriDabao3D
{
    [DisallowMultipleComponent]
    public sealed class ShadeNetInstance : ClimateMitigationWorldObject
    {
        private void Reset()
        {
            ApplyRecommendedConfiguration(ClimateWorldMitigationType.ShadeNet);
        }

        private void OnValidate()
        {
            mitigationType = ClimateWorldMitigationType.ShadeNet;
            ValidateConfiguration();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            DrawRadiusGizmo(new Color(0.25f, 0.90f, 0.55f, 1f));
        }
#endif
    }
}
