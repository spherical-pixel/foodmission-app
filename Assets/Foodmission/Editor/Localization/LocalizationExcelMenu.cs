using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

using eu.foodmission.platform.EditorTools;

namespace eu.foodmission.platform.Editor
{
    /// <summary>
    /// Shares the string tables with partners as one Excel file (Teams) and applies their edits back.
    /// Sheet 1 = instructions, then one sheet per table. Import only changes existing translations.
    /// </summary>
    public static class LocalizationExcelMenu
    {
        private static readonly string[] Tables = { "UI", "AppUI" };
        // Same column order as UI.csv.
        private static readonly string[] LocaleOrder = { "en", "nl", "de", "el", "it", "no", "pl", "sl", "es" };
        private const string UiCsvPath = "Assets/Foodmission/localization/CSV/UI.csv";
        private const string DefaultFileName = "FOODMISSION_localization.xlsx";

        [MenuItem("Foodmission/Localization/Export Excel…")]
        public static void Export()
        {
            string path = EditorUtility.SaveFilePanel("Export localization to Excel", "", DefaultFileName, "xlsx");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (ExportTo(path))
            {
                EditorUtility.RevealInFinder(path);
            }
        }

        public static bool ExportTo(string path)
        {
            try
            {
                var sheets = new List<XlsxSheet> { InstructionsSheet() };
                foreach (string tableName in Tables)
                {
                    StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                    if (collection == null)
                    {
                        Debug.LogWarning($"[LocalizationExcelMenu] String table collection '{tableName}' not found; skipped.");
                        continue;
                    }
                    sheets.Add(TableSheet(collection));
                }

                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    XlsxWriter.Write(stream, sheets);
                }

                int rows = sheets.Skip(1).Sum(s => Math.Max(0, s.Rows.Count - 1));
                Debug.Log($"[LocalizationExcelMenu] Exported {rows} texts in {sheets.Count - 1} sheets to {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationExcelMenu] Export failed: {ex}");
                EditorUtility.DisplayDialog("Export failed", ex.Message, "OK");
                return false;
            }
        }

        [MenuItem("Foodmission/Localization/Import Excel…")]
        public static void Import()
        {
            string path = EditorUtility.OpenFilePanel("Import localization from Excel", "", "xlsx");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string summary = ImportFrom(path);
            if (summary != null)
            {
                EditorUtility.DisplayDialog("Localization import", Truncate(summary, 1500), "OK");
            }
        }

        /// <summary>Applies the edited Excel to the string tables and UI.csv; returns the summary, or null on failure.</summary>
        public static string ImportFrom(string path)
        {
            try
            {
                List<XlsxSheet> sheets;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    sheets = XlsxReader.Read(stream);
                }

                var report = new StringBuilder();
                var plans = new List<LocalizationImportPlan>();
                foreach (string tableName in Tables)
                {
                    StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
                    XlsxSheet sheet = sheets.FirstOrDefault(s => string.Equals(s.Name, tableName, StringComparison.OrdinalIgnoreCase));
                    if (collection == null || sheet == null)
                    {
                        report.AppendLine($"• {tableName}: sheet or table not found, skipped.");
                        continue;
                    }

                    LocalizationImportPlan plan = LocalizationImportPlanner.Plan(tableName, sheet.Rows, CurrentValues(collection));
                    plans.Add(plan);
                    if (plan.Error != null)
                    {
                        report.AppendLine($"• {tableName}: {plan.Error}");
                        continue;
                    }
                    Apply(collection, plan);
                }

                int csvChanges = PatchUiCsv(plans.FirstOrDefault(p => p.Table == "UI" && p.Error == null), report);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string summary = Summary(plans, csvChanges, report);
                Debug.Log($"[LocalizationExcelMenu] Import from {path}\n{summary}");
                return summary;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationExcelMenu] Import failed: {ex}");
                EditorUtility.DisplayDialog("Import failed", ex.Message, "OK");
                return null;
            }
        }

        // ── Export ──────────────────────────────────────────────────────────

        private static IEnumerable<Locale> OrderedLocales()
        {
            List<Locale> locales = LocalizationEditorSettings.GetLocales().ToList();
            return locales.OrderBy(l =>
            {
                int index = Array.IndexOf(LocaleOrder, l.Identifier.Code);
                return index < 0 ? int.MaxValue : index;
            });
        }

        private static XlsxSheet TableSheet(StringTableCollection collection)
        {
            List<Locale> locales = OrderedLocales().ToList();
            var tables = locales.Select(l => collection.GetTable(l.Identifier) as StringTable).ToList();

            var rows = new List<string[]>();
            var header = new List<string> { "Key" };
            header.AddRange(locales.Select(l => $"{LanguageName(l)} ({l.Identifier.Code})"));
            rows.Add(header.ToArray());

            foreach (SharedTableData.SharedTableEntry entry in collection.SharedData.Entries)
            {
                var row = new List<string> { entry.Key };
                foreach (StringTable table in tables)
                {
                    row.Add(table?.GetEntry(entry.Id)?.Value ?? "");
                }
                rows.Add(row.ToArray());
            }

            var widths = new List<double> { 38 };
            widths.AddRange(locales.Select(_ => 48.0));
            return new XlsxSheet(collection.TableCollectionName, rows)
            {
                Protected = true,
                LockedColumns = new[] { 0 },
                FreezeHeader = true,
                ColumnWidths = widths.ToArray(),
            };
        }

        private static string LanguageName(Locale locale)
        {
            // LocaleName is usually "English (en)"; keep just the language part.
            string name = locale.LocaleName ?? locale.Identifier.Code;
            int paren = name.IndexOf(" (", StringComparison.Ordinal);
            return paren > 0 ? name.Substring(0, paren) : name;
        }

        private static XlsxSheet InstructionsSheet()
        {
            var lines = new[]
            {
                "FOODMISSION app – texts for translation review",
                "",
                "Please review the texts of your language. This file is imported directly into the app, so following these rules avoids broken screens. New texts will be added to this file as new features are built.",
                "",
                "How to edit this file",
                "1. Each tab after this one (UI, AppUI) has one row per text shown in the app. Column A (\"Key\") identifies the text: do not edit, add, delete or reorder rows, and do not rename the tabs or the header row.",
                "2. Edit only the column of your language. English (column B) is the reference text.",
                "3. Keep placeholders exactly as they are: anything in curly braces, such as {0}, {1}, {0:F1} or {global.SERVINGS}, is replaced by a number, a unit or a name in the app. You may move them within the sentence to fit your grammar (\"Step {0} of {1}\"), but do not translate, change or remove them, and do not use { or } as normal punctuation. Rows with broken placeholders are not imported.",
                "4. Keep formatting tags such as <b>…</b> where the English text has them, always with their closing tag. Do not add web tags such as <br>, <strong> or <p>.",
                "5. Keep the emojis at the start of some texts (🍽, ⏱, 🌱…): they are icons in the app.",
                "6. Line breaks inside a cell are kept (Alt+Enter on Windows, Control+Option+Return on Mac). Never split one text across several rows.",
                "7. An empty cell never deletes the current translation. If you are unsure about a term, keep the current text and add a comment on the cell. To ask for a new text or to remove one, add a comment or contact the app team.",
                "8. Texts are shown on mobile screens and buttons, tabs and labels have little space: choose the shortest natural wording. If a translation is much longer than the English, please look for a shorter one.",
                "9. If a text starts with =, + or -, type an apostrophe first ('- 10%) so Excel does not treat it as a formula.",
                "10. Several partners can edit this file at the same time in Teams; there is no need to download it.",
                "",
                "If you have questions about where a text appears in the app, add a comment on the cell or contact the app team. Thank you for your help!",
                "",
                $"Exported {DateTime.Now:yyyy-MM-dd HH:mm} from the FOODMISSION app project.",
            };
            return new XlsxSheet("Instructions", lines.Select(l => new[] { l }).ToList())
            {
                ColumnWidths = new[] { 140.0 },
            };
        }

        // ── Import ──────────────────────────────────────────────────────────

        private static Dictionary<string, Dictionary<string, string>> CurrentValues(StringTableCollection collection)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            var tables = collection.StringTables.Where(t => t != null).ToList();
            foreach (SharedTableData.SharedTableEntry entry in collection.SharedData.Entries)
            {
                var values = new Dictionary<string, string>();
                foreach (StringTable table in tables)
                {
                    values[table.LocaleIdentifier.Code] = table.GetEntry(entry.Id)?.Value;
                }
                result[entry.Key] = values;
            }
            return result;
        }

        private static void Apply(StringTableCollection collection, LocalizationImportPlan plan)
        {
            var touched = new HashSet<StringTable>();
            foreach (LocalizationChange change in plan.Changes)
            {
                var table = collection.GetTable(new LocaleIdentifier(change.Locale)) as StringTable;
                if (table == null)
                {
                    Debug.LogWarning($"[LocalizationExcelMenu] {plan.Table}: no table for locale '{change.Locale}', '{change.Key}' skipped.");
                    continue;
                }
                StringTableEntry entry = table.GetEntry(change.Key);
                if (entry == null)
                {
                    table.AddEntry(change.Key, change.Value);
                }
                else
                {
                    entry.Value = change.Value;
                }
                touched.Add(table);
            }
            foreach (StringTable table in touched)
            {
                EditorUtility.SetDirty(table);
            }
        }

        /// <summary>Keeps UI.csv (used by tests and as the import source of new keys) in sync, rewriting only changed cells.</summary>
        private static int PatchUiCsv(LocalizationImportPlan plan, StringBuilder report)
        {
            if (plan == null || plan.Changes.Count == 0 || !File.Exists(UiCsvPath))
            {
                return 0;
            }

            byte[] original = File.ReadAllBytes(UiCsvPath);
            LocalizationCsvFile csv = LocalizationCsvFile.Parse(Encoding.UTF8.GetString(original));
            int changed = 0;
            foreach (LocalizationChange change in plan.Changes)
            {
                if (csv.TrySetValue(change.Key, change.Locale, change.Value))
                {
                    changed++;
                }
                else
                {
                    report.AppendLine($"• UI.csv has no cell for {change.Key} ({change.Locale}); only the Unity table was updated.");
                }
            }
            if (changed > 0)
            {
                File.WriteAllBytes(UiCsvPath, Encoding.UTF8.GetBytes(csv.Serialize()));
            }
            return changed;
        }

        private static string Summary(List<LocalizationImportPlan> plans, int csvChanges, StringBuilder report)
        {
            var sb = new StringBuilder();
            foreach (LocalizationImportPlan plan in plans.Where(p => p.Error == null))
            {
                sb.AppendLine($"{plan.Table}: {plan.Changes.Count} translation(s) updated");
                foreach (var group in plan.Changes.GroupBy(c => c.Locale).OrderBy(g => Array.IndexOf(LocaleOrder, g.Key)))
                {
                    sb.AppendLine($"   {group.Key}: {group.Count()}");
                }
                if (plan.UnknownKeys.Count > 0)
                {
                    sb.AppendLine($"   Unknown keys (ignored): {string.Join(", ", plan.UnknownKeys)}");
                }
                foreach (LocalizationCellIssue issue in plan.PlaceholderErrors)
                {
                    sb.AppendLine($"   Placeholders differ from English, NOT imported: {issue.Key} ({issue.Locale})");
                }
            }
            if (csvChanges > 0)
            {
                sb.AppendLine($"UI.csv: {csvChanges} cell(s) updated");
            }
            if (report.Length > 0)
            {
                sb.Append(report);
            }
            sb.AppendLine();
            sb.AppendLine("Review the diff before committing. For a text-only hotfix also run Foodmission > Export Localization > To JSON.");
            return sb.ToString();
        }

        private static string Truncate(string text, int max)
        {
            return text.Length <= max ? text : text.Substring(0, max) + "\n… (full report in the Console)";
        }
    }
}
