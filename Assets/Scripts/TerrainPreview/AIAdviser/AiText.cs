using System.Text.RegularExpressions;

namespace AgriDabao3D
{
    public static class AiText
    {
        private static readonly Regex CodeFenceLine =
            new Regex(@"^[ \t]*```[^\n]*(\n|$)", RegexOptions.Multiline);

        private static readonly Regex DividerLine =
            new Regex(@"^[ \t]*([-*_])([ \t]*\1){2,}[ \t]*$", RegexOptions.Multiline);

        private static readonly Regex HeadingMarks =
            new Regex(@"^[ \t]*#{1,6}[ \t]+", RegexOptions.Multiline);

        private static readonly Regex StarBullet =
            new Regex(@"^([ \t]*)[*+][ \t]+", RegexOptions.Multiline);

        private static readonly Regex Italic =
            new Regex(@"(?<![\w*])\*(?=[^\s*])([^*\n]*?[^\s*])\*(?![\w*])");

        private static readonly Regex InlineCode =
            new Regex(@"`([^`\n]+)`");

        private static readonly Regex ExtraBlankLines =
            new Regex(@"\n[ \t]*\n([ \t]*\n)+");

        public static string StripMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string result = text.Replace("\r\n", "\n");

            result = CodeFenceLine.Replace(result, string.Empty);
            result = DividerLine.Replace(result, string.Empty);
            result = HeadingMarks.Replace(result, string.Empty);

            result = result.Replace("**", string.Empty);

            result = StarBullet.Replace(result, "$1- ");
            result = Italic.Replace(result, "$1");
            result = InlineCode.Replace(result, "$1");

            result = ExtraBlankLines.Replace(result, "\n\n");

            return result.Trim();
        }
    }
}
