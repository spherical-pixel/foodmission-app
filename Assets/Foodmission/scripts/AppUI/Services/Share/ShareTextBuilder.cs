using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Composes the shared text: body, source and footer separated by blank lines. Empty parts are skipped.
    /// </summary>
    public static class ShareTextBuilder
    {
        public static string Build(string body, string source, string footer)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return "";
            }

            var parts = new List<string> { body.Trim() };
            string formattedSource = FormatSource(source);
            if (formattedSource.Length > 0)
            {
                parts.Add(formattedSource);
            }
            if (!string.IsNullOrWhiteSpace(footer))
            {
                parts.Add(footer.Trim());
            }
            return string.Join("\n\n", parts);
        }

        /// <summary>
        /// Plain-text source: citation followed by its URLs, so markdown links do not reach other apps.
        /// </summary>
        public static string FormatSource(string rawSource)
        {
            if (string.IsNullOrWhiteSpace(rawSource))
            {
                return "";
            }

            SourceInfo info = LinkHelper.ParseSource(rawSource);
            if (info == null || info.IsEmpty)
            {
                return rawSource.Trim();
            }

            IEnumerable<string> pieces = new[] { info.CitationText?.Trim() }
                .Concat(info.Links.Select(l => l.Url))
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" ", pieces);
        }
    }
}
