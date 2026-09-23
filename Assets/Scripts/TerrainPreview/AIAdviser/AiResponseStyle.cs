namespace AgriDabao3D
{
    /// <summary>
    /// Turns the player's "AI Summarization" setting into a line of prompt.
    ///
    /// Both talking AI features - the adviser chat and the post-event climate
    /// evaluation - carry their own system guidance, written as a tunable field on
    /// their MonoBehaviour, and each of those describes a full-length answer with
    /// bullet lists. Rather than keep two shortened copies of that guidance in
    /// sync, the brief instruction is appended last, where it overrides the format
    /// the guidance above it asked for.
    ///
    /// The two task features deliberately do not use this. They must return strict
    /// JSON for the game to parse, so trimming their wording would break them
    /// rather than make them easier to read.
    /// </summary>
    public static class AiResponseStyle
    {
        /// <summary>
        /// Deliberately about length and shape only. It does not tell the model to
        /// leave anything out, because a shorter answer that quietly drops the
        /// warning the player needed is worse than a long one.
        /// </summary>
        private const string BriefDirective =
            "\n\nBREVITY OVERRIDE - this takes priority over any response format " +
            "described above. Reply in at most two short sentences of plain prose. " +
            "No bullet points, no numbered lists, no headings, no bold, no markdown. " +
            "Keep the same findings and the same recommendation as you would have " +
            "given at full length - state the single most important thing first, " +
            "then the action to take. Do not omit a warning just to stay short.";

        /// <summary>
        /// Returns <paramref name="guidance"/> with the brevity instruction appended
        /// when the player has summarization on, and unchanged when they do not.
        /// </summary>
        public static string Apply(string guidance)
        {
            if (!GameSettings.AiSummarization)
                return guidance;

            return string.IsNullOrEmpty(guidance)
                ? BriefDirective.TrimStart()
                : guidance + BriefDirective;
        }
    }
}
