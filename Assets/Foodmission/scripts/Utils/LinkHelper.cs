using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace eu.foodmission.platform
{
    public struct ExtractedLink
    {
        public string Title; // Texto visible (ej: "Organización Mundial de la Salud", "Ver artículo (DOI) ↗", etc.)
        public string Url;   // URL a abrir
    }

    public class SourceInfo
    {
        public string CitationText { get; set; }
        public List<ExtractedLink> Links { get; set; } = new List<ExtractedLink>();
        public bool HasLinks => Links != null && Links.Count > 0;
        public bool IsEmpty => string.IsNullOrWhiteSpace(CitationText) && !HasLinks;
    }

    public static class LinkHelper
    {
        private static readonly Regex MarkdownLinkRegex = new Regex(@"\[([^\]]+)\]\((https?://[^\)]+)\)", RegexOptions.Compiled);
        private static readonly Regex RawUrlRegex = new Regex(@"(https?://[^\s\)]+)", RegexOptions.Compiled);

        public static List<ExtractedLink> ExtractLinks(string text)
        {
            var result = new List<ExtractedLink>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            // 1. Buscar primero enlaces Markdown [Título](URL)
            var mdMatches = MarkdownLinkRegex.Matches(text);
            var handledUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Match match in mdMatches)
            {
                string title = match.Groups[1].Value;
                string url = match.Groups[2].Value.TrimEnd('.', ',', ';', ')');
                result.Add(new ExtractedLink { Title = title, Url = url });
                handledUrls.Add(url);
            }

            // 2. Buscar URLs directas que no formen parte de un enlace Markdown
            var rawMatches = RawUrlRegex.Matches(text);
            foreach (Match match in rawMatches)
            {
                string url = match.Value.TrimEnd('.', ',', ';', ')');
                if (!handledUrls.Contains(url))
                {
                    string title = GetDomainOrShortUrl(url);
                    result.Add(new ExtractedLink { Title = title, Url = url });
                    handledUrls.Add(url);
                }
            }

            return result;
        }

        public static SourceInfo ParseSource(string rawSource)
        {
            if (string.IsNullOrWhiteSpace(rawSource))
                return null;

            var info = new SourceInfo { CitationText = rawSource.Trim() };

            // 1. Comprobar si contiene enlace Markdown [Título](URL)
            var mdMatch = MarkdownLinkRegex.Match(rawSource);
            if (mdMatch.Success)
            {
                string title = mdMatch.Groups[1].Value;
                string url = mdMatch.Groups[2].Value.TrimEnd('.', ',', ';', ')');

                info.Links.Add(new ExtractedLink { Title = $"{title} ↗", Url = url });
                info.CitationText = rawSource.Replace(mdMatch.Value, title).Trim().TrimEnd(' ', '-', '—', ':');
                return info;
            }

            // 2. Comprobar si contiene URL directa
            var rawMatch = RawUrlRegex.Match(rawSource);
            if (rawMatch.Success)
            {
                string url = rawMatch.Value.TrimEnd('.', ',', ';', ')');
                string domain = "Enlace ↗";

                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    domain = $"{uri.Host.Replace("www.", "")} ↗";
                }

                info.Links.Add(new ExtractedLink { Title = domain, Url = url });
                info.CitationText = rawSource.Replace(rawMatch.Value, "").Trim().TrimEnd(' ', '-', '—', ':');
            }

            return info;
        }

        private static string GetDomainOrShortUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Host.Replace("www.", "");
            }
            return "Fuente";
        }
    }
}