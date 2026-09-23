using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// The quiet start every new farm gets: no rain, no climate events, no pest or
    /// disease outbreaks for the first two game days.
    ///
    /// A new player is being taught to dig, plant and water. Dropping a typhoon on
    /// them mid-lesson would destroy the one crop they own and make Antonio look
    /// like a liar, since he is at that point still explaining that bad weather is
    /// something that happens *later*.
    ///
    /// Deliberately keyed to the game date rather than to tutorial progress. Tying
    /// it to the tutorial would freeze the weather forever on a farm whose owner
    /// declined the tour or abandoned it half way; keying it to the date means a
    /// player who skips the guide still gets the same two calm days, and normal
    /// weather resumes on day 3 no matter what.
    ///
    /// Climate events need no guard of their own. ClimateEventTracker only starts
    /// tracking when a weather event begins, so holding the weather at Clear
    /// suppresses climate events - and their end-of-event AI evaluation - as a
    /// consequence rather than as a second special case.
    /// </summary>
    public static class FarmGraceperiod
    {
        /// <summary>Game days protected from the start. Day 1 and day 2.</summary>
        public const float CalmDays = 2f;

        /// <summary>True while the farm is still inside its opening calm spell.</summary>
        public static bool IsCalmWeatherDay
        {
            get
            {
                GameTimeSystem time = GameTimeSystem.Instance;
                if (time == null)
                    return false;

                // TotalGameDays counts up from 0 on day 1, so "still inside day 2"
                // means strictly less than 2 whole days elapsed.
                return time.TotalGameDays < CalmDays;
            }
        }
    }
}
