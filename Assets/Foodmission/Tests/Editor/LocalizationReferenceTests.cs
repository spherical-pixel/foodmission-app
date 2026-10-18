using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using NUnit.Framework;

using Unity.AppUI.Navigation;

using UnityEditor;
using UnityEngine.Localization.Tables;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Every localized text the app references must exist in its string table, in every locale:
    /// a missing key shows the raw "@UI:KEY" (or an empty string) on screen.
    /// </summary>
    [TestFixture]
    public class LocalizationReferenceTests
    {
        private const string NavGraphPath = "Assets/Foodmission/AppUI/Foodmission_NavigationGraph.asset";

        // "@UI:KEY" anywhere in UXML attributes, nav labels or C# string literals
        private static readonly Regex k_TableReference = new Regex(@"^@(\w+):([A-Za-z0-9_.&\-]+)$");

        // GetLocalizedString("UI", "KEY") / new LocalizedString("UI", "KEY"); "PREFIX_" + x is built at runtime and skipped
        private static readonly Regex k_CodeLookup = new Regex(@"(?:GetLocalizedString(?:Async)?|LocalizedString)\(\s*""(\w+)""\s*,\s*""([^""]+)""(?!\s*\+)");
        private static readonly Regex k_CodeReference = new Regex(@"""@(\w+):([A-Za-z0-9_.&\-]+)""(?!\s*\+)");

        // A named placeholder ("{global.PET_NAME}", "{count}") only resolves when the entry is marked Smart
        private static readonly Regex k_NamedPlaceholder = new Regex(@"\{[A-Za-z]");

        private Dictionary<string, SharedTableData> _tablesByName;

        [OneTimeSetUp]
        public void LoadTables()
        {
            _tablesByName = StaticAssetScan.SharedTables().ToDictionary(t => t.TableCollectionName);
        }

        [Test]
        public void UxmlLocalizedStrings_PointAtExistingEntries()
        {
            Dictionary<string, SharedTableData> tablesByGuid = _tablesByName.Values
                .ToDictionary(t => "GUID:" + t.TableCollectionNameGuid.ToString("N"));
            var problems = new List<string>();
            foreach ((string path, XDocument doc) in StaticAssetScan.UxmlDocuments())
            {
                foreach (XElement localized in doc.Descendants().Where(e => e.Name.LocalName.EndsWith("LocalizedString")))
                {
                    string table = (string)localized.Attribute("table");
                    string entry = (string)localized.Attribute("entry");
                    string where = $"{path}:{StaticAssetScan.Line(localized)}";
                    if (table == null || entry == null)
                    {
                        problems.Add($"{where} LocalizedString without table/entry");
                        continue;
                    }
                    SharedTableData shared = tablesByGuid.TryGetValue(table, out SharedTableData byGuid) ? byGuid
                        : _tablesByName.TryGetValue(table, out SharedTableData byName) ? byName : null;
                    if (shared == null)
                    {
                        problems.Add($"{where} unknown table '{table}'");
                        continue;
                    }
                    Match id = Regex.Match(entry, @"^Id\((\d+)\)$");
                    SharedTableData.SharedTableEntry found = id.Success ? shared.GetEntry(long.Parse(id.Groups[1].Value)) : shared.GetEntry(entry);
                    if (found == null)
                    {
                        problems.Add($"{where} entry '{entry}' not in table {shared.TableCollectionName}");
                    }
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "UXML LocalizedString(s) without an entry");
        }

        [Test]
        public void UxmlAndNavigationReferences_ExistInTheirTable()
        {
            var problems = new List<string>();
            foreach ((string path, XDocument doc) in StaticAssetScan.UxmlDocuments())
            {
                foreach (XAttribute attribute in doc.Descendants().SelectMany(e => e.Attributes()))
                {
                    CheckReference(attribute.Value, $"{path}:{StaticAssetScan.Line(attribute.Parent)}", problems);
                }
            }

            var graph = AssetDatabase.LoadAssetAtPath<NavGraphViewAsset>(NavGraphPath);
            Assert.IsNotNull(graph, NavGraphPath);
            foreach (NavDestination destination in graph.nodes.OfType<NavDestination>())
            {
                CheckReference(destination.label ?? "", $"{NavGraphPath} destination {destination.name}", problems);
            }
            StaticAssetScan.AssertNoProblems(problems, "@Table:KEY reference(s) to missing entries");
        }

        [Test]
        public void CodeReferences_ExistInTheirTable()
        {
            var problems = new List<string>();
            foreach (string path in StaticAssetScan.FilesUnderRoot(".cs").Where(p => !p.Contains("/Tests/")))
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string trimmed = lines[i].TrimStart();
                    if (trimmed.StartsWith("//") || trimmed.StartsWith("*"))
                    {
                        continue;
                    }
                    string where = $"{path}:{i + 1}";
                    foreach (Match m in k_CodeLookup.Matches(lines[i]))
                    {
                        CheckKey(m.Groups[1].Value, m.Groups[2].Value, where, problems);
                    }
                    foreach (Match m in k_CodeReference.Matches(lines[i]))
                    {
                        CheckKey(m.Groups[1].Value, m.Groups[2].Value, where, problems);
                    }
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "localization key(s) used in code but missing from the table");
        }

        [Test]
        public void UiEntries_HaveATranslationInEveryLocale()
        {
            SharedTableData shared = StaticAssetScan.SharedTable(StaticAssetScan.UiTable);
            var problems = new List<string>();
            foreach (StringTable table in StaticAssetScan.StringTables(shared))
            {
                foreach (SharedTableData.SharedTableEntry entry in shared.Entries)
                {
                    StringTableEntry localized = table.GetEntry(entry.Id);
                    if (localized == null || string.IsNullOrWhiteSpace(localized.Value))
                    {
                        problems.Add($"{table.LocaleIdentifier.Code}: {entry.Key}");
                    }
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "UI entries without text");
        }

        [Test]
        public void UiEntriesWithNamedPlaceholders_AreSmart()
        {
            SharedTableData shared = StaticAssetScan.SharedTable(StaticAssetScan.UiTable);
            var problems = new List<string>();
            foreach (StringTable table in StaticAssetScan.StringTables(shared))
            {
                foreach (StringTableEntry entry in table.Values)
                {
                    if (!entry.IsSmart && entry.Value != null && k_NamedPlaceholder.IsMatch(entry.Value))
                    {
                        problems.Add($"{table.LocaleIdentifier.Code}: {entry.Key} = \"{entry.Value}\"");
                    }
                }
            }
            StaticAssetScan.AssertNoProblems(problems, "entries with {named} placeholders not marked Smart (shown raw)");
        }

        private void CheckReference(string value, string where, List<string> problems)
        {
            Match m = k_TableReference.Match(value.Trim());
            if (m.Success)
            {
                CheckKey(m.Groups[1].Value, m.Groups[2].Value, where, problems);
            }
        }

        private void CheckKey(string tableName, string key, string where, List<string> problems)
        {
            if (!_tablesByName.TryGetValue(tableName, out SharedTableData shared))
            {
                problems.Add($"{where} unknown table '{tableName}' (key {key})");
                return;
            }
            if (shared.GetEntry(key) == null)
            {
                problems.Add($"{where} {tableName}:{key}");
            }
        }
    }
}
