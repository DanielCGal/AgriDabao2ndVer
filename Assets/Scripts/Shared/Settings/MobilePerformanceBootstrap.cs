using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// Applies the device-side performance settings once, before the first scene loads.
    ///
    /// These are deliberately set in code rather than left to the Quality Settings
    /// asset. The values there apply per quality tier, and a phone can silently land
    /// on a different tier than the one that was tuned, so the frame cap - the setting
    /// that most affects how the game feels over a long session - could quietly go
    /// missing on the exact devices that need it most.
    ///
    /// NOTE: because this runs at start-up it overrides the shadow values in
    /// Project Settings > Quality. Change them here, not there.
    /// </summary>
    public static class MobilePerformanceBootstrap
    {
        /// <summary>
        /// Frames per second to aim for.
        ///
        /// Without a cap the GPU renders as fast as it physically can. The phone then
        /// heats up, the chip throttles itself to cool down, and the frame rate drops
        /// for the rest of the session - which reads to a player as "the game got
        /// laggy after a while". Capping lets the device finish its work and idle
        /// until the next frame instead.
        ///
        /// 60 suits a farming game on a mid-range phone. If a device still throttles
        /// after a long session, 30 gives it far more headroom and stays perfectly
        /// smooth for this kind of game.
        /// </summary>
        private const int TargetFrameRate = 60;

        /// <summary>
        /// How far from the camera objects still cast real-time shadows, in metres.
        ///
        /// Everything inside this radius has its geometry submitted a second time to
        /// draw the shadow map, so the cost scales with how much of the farm it
        /// covers. 40 reached well past the crops the player is actually working on;
        /// 25 keeps shadows where they read clearly and stops paying for the rest.
        /// </summary>
        private const float ShadowDistance = 25f;

        /// <summary>
        /// Cascades split the shadow distance into bands of differing quality. Each
        /// band is another map to render and sample, which is an expensive trade on
        /// mobile for a view this close to the ground. One band is enough at 25m.
        /// </summary>
        private const int ShadowCascades = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // targetFrameRate is ignored while VSync is driving the frame rate, so the
            // cap below only takes effect with VSync off.
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
