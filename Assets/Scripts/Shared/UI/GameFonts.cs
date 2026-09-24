using UnityEngine;

namespace AgriDabao3D
{
    public static class GameFonts
    {
        private const string FontFolder = "Fonts";
        private const string PreferredFont = "Fonts/PlaypenSans";
        private const string FontNamePrefix = "PlaypenSans";

        private const string BuiltinFont = "LegacyRuntime.ttf";

        private static Font primary;
        private static bool resolved;

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
