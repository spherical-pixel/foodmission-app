using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    /// <summary>Reads Assets/Foodmission/localization/CSV/UI.csv: key → cells (0 key, 1 id, 2–10 languages).</summary>
    internal static class UiCsv
    {
        // Minimal RFC-4180 reader: quoted cells may contain commas and \r\n.
        public static Dictionary<string, string[]> Read()
        {
            string text = File.ReadAllText(Path.Combine(Application.dataPath, "Foodmission/localization/CSV/UI.csv"));
            var result = new Dictionary<string, string[]>();
            var row = new List<string>();
            var cell = new System.Text.StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else if (c == '"') { quoted = false; }
                    else { cell.Append(c); }
                    continue;
                }
                if (c == '"') { quoted = true; }
                else if (c == ',') { row.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\n')
                {
                    row.Add(cell.ToString().TrimEnd('\r')); cell.Clear();
                    if (row.Count > 0 && row[0].Length > 0) { result[row[0]] = row.ToArray(); }
                    row = new List<string>();
                }
                else { cell.Append(c); }
            }
            row.Add(cell.ToString());
            if (row[0].Length > 0) { result[row[0]] = row.ToArray(); }
            return result;
        }
    }
}
