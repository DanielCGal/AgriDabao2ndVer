namespace AgriDabao3D
{
    public static class AiResponseStyle
    {
        private const string BriefDirective =
            "\n\nBREVITY OVERRIDE - this takes priority over any response format " +
            "described above. Reply in at most two short sentences of plain prose. " +
            "No bullet points, no numbered lists, no headings, no bold, no markdown. " +
            "Keep the same findings and the same recommendation as you would have " +
            "given at full length - state the single most important thing first, " +
            "then the action to take. Do not omit a warning just to stay short.";

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
