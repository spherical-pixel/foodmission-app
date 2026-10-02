using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace eu.foodmission.platform.EditorTools
{
    /// <summary>
    /// UI.csv kept byte-for-byte: cells are stored raw (quotes included) with each row's own terminator,
    /// so changing one translation only rewrites that cell. Embedded newlines use \r\n like the rest of the file.
    /// </summary>
    public sealed class LocalizationCsvFile
    {
        private static readonly Regex LocaleInHeader = new Regex(@"\(([^)]+)\)\s*$", RegexOptions.Compiled);

        private sealed class Row
        {
            public readonly List<string> Cells = new List<string>();
            public string Terminator = "";
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly Dictionary<string, int> _localeColumns = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _rowByKey = new Dictionary<string, int>();

        public static LocalizationCsvFile Parse(string text)
        {
            var file = new LocalizationCsvFile();
            var row = new Row();
            var cell = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (quoted)
                {
                    cell.Append(ch);
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    continue;
                }

                if (ch == '"')
                {
                    quoted = true;
                    cell.Append(ch);
                }
                else if (ch == ',')
                {
                    row.Cells.Add(cell.ToString());
                    cell.Clear();
                }
                else if (ch == '\r' || ch == '\n')
                {
                    bool crlf = ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n';
                    row.Cells.Add(cell.ToString());
                    cell.Clear();
                    row.Terminator = crlf ? "\r\n" : ch.ToString();
                    if (crlf)
                    {
                        i++;
                    }
                    file._rows.Add(row);
                    row = new Row();
                }
                else
                {
                    cell.Append(ch);
                }
            }

            if (cell.Length > 0 || row.Cells.Count > 0)
            {
                row.Cells.Add(cell.ToString());
                file._rows.Add(row);
            }

            file.Index();
            return file;
        }

        private void Index()
        {
            if (_rows.Count == 0)
            {
                return;
            }
            List<string> header = _rows[0].Cells;
            for (int c = 0; c < header.Count; c++)
            {
                Match match = LocaleInHeader.Match(Unquote(header[c]));
                if (match.Success)
                {
                    _localeColumns[match.Groups[1].Value] = c;
                }
            }
            for (int r = 1; r < _rows.Count; r++)
            {
                if (_rows[r].Cells.Count > 0)
                {
                    string key = Unquote(_rows[r].Cells[0]);
                    if (!string.IsNullOrEmpty(key) && !_rowByKey.ContainsKey(key))
                    {
                        _rowByKey[key] = r;
                    }
                }
            }
        }

        public bool HasKey(string key) => key != null && _rowByKey.ContainsKey(key);

        public string GetValue(string key, string locale)
        {
            if (!TryLocate(key, locale, out Row row, out int column))
            {
                return null;
            }
            return column < row.Cells.Count ? Unquote(row.Cells[column]) : "";
        }

        public bool TrySetValue(string key, string locale, string value)
        {
            if (!TryLocate(key, locale, out Row row, out int column))
            {
                return false;
            }
            while (row.Cells.Count <= column)
            {
                row.Cells.Add("");
            }
            string normalized = (value ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
            row.Cells[column] = "\"" + normalized.Replace("\"", "\"\"") + "\"";
            return true;
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            foreach (Row row in _rows)
            {
                sb.Append(string.Join(",", row.Cells));
                sb.Append(row.Terminator);
            }
            return sb.ToString();
        }

        private bool TryLocate(string key, string locale, out Row row, out int column)
        {
            row = null;
            column = -1;
            if (key == null || locale == null || !_rowByKey.TryGetValue(key, out int r) || !_localeColumns.TryGetValue(locale, out column))
            {
                return false;
            }
            row = _rows[r];
            return true;
        }

        private static string Unquote(string raw)
        {
            if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
            {
                return raw.Substring(1, raw.Length - 2).Replace("\"\"", "\"");
            }
            return raw;
        }
    }
}
