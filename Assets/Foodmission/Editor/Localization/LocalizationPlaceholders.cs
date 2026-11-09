using System.Linq;
using System.Text.RegularExpressions;

namespace eu.foodmission.platform.EditorTools
{
    /// <summary>
    /// Format placeholders such as {0}, {1} or {0:0.##} must survive translation unchanged (order may differ).
    /// Only which ones appear is compared, not how often: a plural repeats {0} once per form, and languages
    /// like Slovenian or Polish have more forms than English.
    /// </summary>
    public static class LocalizationPlaceholders
    {
        private static readonly Regex Placeholder = new Regex(@"\{[^{}]*\}", RegexOptions.Compiled);

        public static bool Match(string reference, string translation)
        {
            string[] expected = Extract(reference);
            string[] actual = Extract(translation);
            return expected.SequenceEqual(actual);
        }

        public static string[] Extract(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new string[0];
            }
            return Placeholder.Matches(text).Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Value)
                .Distinct()
                .OrderBy(v => v, System.StringComparer.Ordinal)
                .ToArray();
        }
    }
}
