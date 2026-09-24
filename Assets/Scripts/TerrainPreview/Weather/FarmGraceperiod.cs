using UnityEngine;

namespace AgriDabao3D
{
    public static class FarmGraceperiod
    {
        public const float CalmDays = 2f;

        public static bool IsCalmWeatherDay
        {
            get
            {
                GameTimeSystem time = GameTimeSystem.Instance;
                if (time == null)
                    return false;

                return time.TotalGameDays < CalmDays;
            }
        }
    }
}
