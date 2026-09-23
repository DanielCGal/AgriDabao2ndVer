using System.Text.RegularExpressions;

namespace AgriDabao3D
{
    /// <summary>
    /// Takes the Markdown out of a Gemini reply before a text box shows it.
    ///
    /// The AI Adviser and the Climate Resilience Evaluation ask for bullets and
    /// short sections, and Gemini marks those up in Markdown - **bold**, "* "
    /// bullets, "## " headings. The game's text boxes print text exactly as they
    /// get it, so players saw asterisks around words (ISS-15). Every word stays;
    /// only the marks go, and bullets become a plain "- ", which any font has.
    ///
    /// Underscores are left alone on purpose. Crop names such as mangosteen_1 are
    /// written with them, and reading them as Markdown would eat them.
    /// </summary>
    public static class AiText
    {
        /// <summary>A ``` line opening or closing a code block, with its line break.</summary>
        private static readonly Regex CodeFenceLine =
            new Regex(@"^[ \t]*```[^\n]*(\n|$)", RegexOptions.Multiline);

        /// <summary>A line of three or more -, * or _ with nothing else on it: a divider.</summary>
        private static readonly Regex DividerLine =
            new Regex(@"^[ \t]*([-*_])([ \t]*\1){2,}[ \t]*$", RegexOptions.Multiline);

        /// <summary>The #'s in front of a heading.</summary>
        private static readonly Regex HeadingMarks =
            new Regex(@"^[ \t]*#{1,6}[ \t]+", RegexOptions.Multiline);

        /// <summary>A "* " or "+ " bullet; "- " bullets are already plain.</summary>
        private static readonly Regex StarBullet =
            new Regex(@"^([ \t]*)[*+][ \t]+", RegexOptions.Multiline);

        /// <summary>
        /// *words* in italics. The stars must hug the words, so "2 * 3" and
        /// "x*y" are not touched.
        /// </summary>
        private static readonly Regex Italic =
            new Regex(@"(?<![\w*])\*(?=[^\s*])([^*\n]*?[^\s*])\*(?![\w*])");

        /// <summary>`words` in a code span.</summary>
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

            // Bold first, so "* **Water early:** ..." is an ordinary bullet by the
            // time bullets are looked for, and "***word***" is left as an italic.
            result = result.Replace("**", string.Empty);

            result = StarBullet.Replace(result, "$1- ");
            result = Italic.Replace(result, "$1");
            result = InlineCode.Replace(result, "$1");

            // Removed dividers and code fences leave empty lines behind.
            result = ExtraBlankLines.Replace(result, "\n\n");

            return result.Trim();
        }
    }
}
