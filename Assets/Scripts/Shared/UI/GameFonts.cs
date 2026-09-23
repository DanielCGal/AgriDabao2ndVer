using UnityEngine;

namespace AgriDabao3D
{
    /// <summary>
    /// The one place the game decides what its text is drawn in.
    ///
    /// Every label in AgriDabaw is built in code, and each builder used to ask
    /// Unity for the built-in LegacyRuntime.ttf directly - fifty-three separate
    /// calls across twenty-five files. Routing them all through here means the
    /// typeface is chosen once, and a future change is one edit rather than a
    /// sweep that is easy to do incompletely.
    ///
    /// The font is loaded from Resources rather than referenced from a scene,
    /// because none of the UI exists as a prefab to hold a reference. If it is
    /// missing the game falls back to the built-in font and keeps running - a
    /// missing font asset should look wrong, not crash a farm.
    /// </summary>
    public static class GameFonts
    {
        /// <summary>
        /// Where the typeface lives. The shipped file is Google's variable build,
        /// renamed from PlaypenSans[wght].ttf - the square brackets in the upstream
        /// name are awkward in a Resources path, and Unity renders the font's
        /// default instance (weight 400) either way, since the legacy Text
        /// component cannot address variable axes.
        ///
        /// An exact miss falls through to a scan of the same folder, so swapping in
        /// a static build later needs no code change.
        /// </summary>
        private const string FontFolder = "Fonts";
        private const string PreferredFont = "Fonts/PlaypenSans";
        private const string FontNamePrefix = "PlaypenSans";

        private const string BuiltinFont = "LegacyRuntime.ttf";

        private static Font primary;
        private static bool resolved;

        /// <summary>The typeface every label in the game is drawn in.</summary>
        public static Font Primary
        {
            get
            {
                if (!resolved)
                {
                    primary = Resolve();
                    resolved = true;
                }

                return primary;
            }
        }

        private static Font Resolve()
        {
            Font exact = Resources.Load<Font>(PreferredFont);
            if (exact != null)
                return exact;

            foreach (Font candidate in Resources.LoadAll<Font>(FontFolder))
            {
                if (candidate != null &&
                    candidate.name.StartsWith(FontNamePrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            Debug.LogWarning(
                "[GameFonts] No " + FontNamePrefix + " font found under Resources/" + FontFolder +
                ". Falling back to " + BuiltinFont + "; the game is fully playable, " +
                "it is just drawn in the old typeface.");

            return Resources.GetBuiltinResource<Font>(BuiltinFont);
        }
    }
}
