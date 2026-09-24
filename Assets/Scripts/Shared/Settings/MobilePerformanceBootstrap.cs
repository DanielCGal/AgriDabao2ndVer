using UnityEngine;

namespace AgriDabao3D
{
    public static class MobilePerformanceBootstrap
    {
        private const int TargetFrameRate = 60;

        private const float ShadowDistance = 25f;

        private const int ShadowCascades = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;

            QualitySettings.shadowDistance = ShadowDistance;
            QualitySettings.shadowCascades = ShadowCascades;

            Debug.Log(
                $"[Performance] Frame cap {TargetFrameRate} fps | " +
                $"shadow distance {ShadowDistance}m | cascades {ShadowCascades}");
        }
    }
}
