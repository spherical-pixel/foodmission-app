using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace eu.foodmission.platform.EditorTools
{
    public sealed class LocalizationChange
    {
        public string Key;
        public string Locale;
        public string Value;
    }

    public sealed class LocalizationCellIssue
    {
        public string Key;
        public string Locale;
        public string Value;
    }

    public sealed class LocalizationImportPlan
    {
        public string Table;
        public string Error;
        public readonly List<LocalizationChange> Changes = new List<LocalizationChange>();
        public readonly List<string> UnknownKeys = new List<string>();
        public readonly List<LocalizationCellIssue> PlaceholderErrors = new List<LocalizationCellIssue>();
    }

    /// <summary>
    /// Compares one exported sheet (edited by partners) with the current table values and decides what to apply.
    /// Never creates keys, never clears a translation (empty cells are ignored) and rejects translations whose
    /// {placeholders} differ from the English text.
    /// </summary>
    public static class LocalizationImportPlanner
    {
        public const string ReferenceLocale = "en";
        private static readonly Regex LocaleInHeader = new Regex(@"\(([^)]+)\)\s*$", RegexOptions.Compiled);

        /// <param name="current">key → (locale code → current value).</param>
        public static LocalizationImportPlan Plan(string table, IList<string[]> rows, IReadOnlyDictionary<string, Dictionary<string, string>> current)
        {
            var plan = new LocalizationImportPlan { Table = table };
            if (rows == null || rows.Count == 0)
            {
                plan.Error = $"Sheet '{table}' is empty.";
                return plan;
            }

            string[] header = rows[0];
            int keyColumn = -1;
            var localeColumns = new List<(int Column, string Locale)>();
            for (int c = 0; c < header.Length; c++)
            {
                string title = (header[c] ?? "").Trim();
                if (string.Equals(title, "Key", StringComparison.OrdinalIgnoreCase))
                {
                    keyColumn = c;
                    continue;
                }
                Match match = LocaleInHeader.Match(title);
                if (match.Success)
                {
                    localeColumns.Add((c, match.Groups[1].Value.Trim()));
                }
            }
            if (keyColumn < 0)
            {
                plan.Error = $"Sheet '{table}' has no 'Key' column in its first row.";
                return plan;
            }

            var unknown = new HashSet<string>();
            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r] ?? Array.Empty<string>();
                string key = Cell(row, keyColumn).Trim();
                if (key.Length == 0)
                {
                    continue;
                }
                if (current == null || !current.TryGetValue(key, out Dictionary<string, string> values))
                {
                    if (unknown.Add(key))
                    {
                        plan.UnknownKeys.Add(key);
                    }
                    continue;
                }

                string sheetEnglish = null;
                foreach (var (column, locale) in localeColumns)
                {
                    if (locale == ReferenceLocale)
                    {
                        sheetEnglish = Cell(row, column);
                    }
                }
                values.TryGetValue(ReferenceLocale, out string currentEnglish);
                string reference = string.IsNullOrWhiteSpace(sheetEnglish) ? currentEnglish : sheetEnglish;

                foreach (var (column, locale) in localeColumns)
                {
                    string value = Cell(row, column);
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }
                    values.TryGetValue(locale, out string currentValue);
                    if (Normalize(value) == Normalize(currentValue))
                    {
                        continue;
                    }

                    string placeholderReference = locale == ReferenceLocale ? currentEnglish : reference;
                    if (!string.IsNullOrEmpty(placeholderReference) && !LocalizationPlaceholders.Match(placeholderReference, value))
                    {
                        plan.PlaceholderErrors.Add(new LocalizationCellIssue { Key = key, Locale = locale, Value = value });
                        continue;
                    }

                    string normalized = Normalize(value);
                    bool useCrLf = currentValue != null && currentValue.Contains("\r\n");
                    plan.Changes.Add(new LocalizationChange
                    {
                        Key = key,
                        Locale = locale,
                        Value = useCrLf ? normalized.Replace("\n", "\r\n") : normalized,
                    });
                }
            }
            return plan;
        }

        private static string Cell(string[] row, int column)
        {
            return column < row.Length ? row[column] ?? "" : "";
        }

        private static string Normalize(string value)
        {
            return (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }
}
