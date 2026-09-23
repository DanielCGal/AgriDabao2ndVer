using AgriDabao3D;
using UnityEngine;

public static class SoilGameplayRules
{
    public static float GetDrainage(SoilSample s)
    {
        float drainage = 0.5f;
        drainage += s.sand * 0.004f;
        drainage -= s.clay * 0.004f;
        drainage -= Mathf.InverseLerp(80f, 160f, s.bdod) * 0.2f;
        return Mathf.Clamp01(drainage);
    }

    public static float GetFertility(SoilSample s)
    {
        float nitrogen = Mathf.InverseLerp(0f, 50f, s.nitrogen);
        float carbon = Mathf.InverseLerp(0f, 60f, s.soc);
        float ph = 1f - Mathf.Abs(s.phh2o - 6.5f) / 2.5f;

        return Mathf.Clamp01(nitrogen * 0.45f + carbon * 0.35f + ph * 0.20f);
    }

    public static float GetCompactionPenalty(SoilSample s)
    {
        return Mathf.Clamp01(Mathf.InverseLerp(90f, 170f, s.bdod));
    }
}
