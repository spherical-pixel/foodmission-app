using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using NUnit.Framework;

using UnityEngine;

using eu.foodmission.platform.EditorTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class LocalizationExcelSyncTests
    {
        // ── Xlsx ────────────────────────────────────────────────────────────

        [Test]
        public void Xlsx_RoundTrip_KeepsSheetsAndTrickyText()
        {
            var sheets = new List<XlsxSheet>
            {
                new XlsxSheet("Instructions", new List<string[]> { new[] { "Edit only your language column." } }),
                new XlsxSheet("UI", new List<string[]>
                {
                    new[] { "Key", "English (en)", "Greek (el)" },
                    new[] { "MULTI", "Line 1\nLine 2", "Γραμμή 1\nΓραμμή 2" },
                    new[] { "ESCAPE", "a & <b> \"q\" 'x'", "" },
                    new[] { "SPACES", "  padded  ", "{0:0.##} / {1}" },
                }) { Protected = true, LockedColumns = new[] { 0 }, FreezeHeader = true },
            };

            var stream = new MemoryStream();
            XlsxWriter.Write(stream, sheets);
            stream.Position = 0;
            List<XlsxSheet> read = XlsxReader.Read(stream);

            CollectionAssert.AreEqual(new[] { "Instructions", "UI" }, read.Select(s => s.Name));
            Assert.AreEqual("Edit only your language column.", read[0].Rows[0][0]);
            XlsxSheet ui = read[1];
            Assert.AreEqual(4, ui.Rows.Count);
            CollectionAssert.AreEqual(new[] { "MULTI", "Line 1\nLine 2", "Γραμμή 1\nΓραμμή 2" }, ui.Rows[1]);
            Assert.AreEqual("a & <b> \"q\" 'x'", ui.Rows[2][1]);
            Assert.AreEqual("", ui.Rows[2].ElementAtOrDefault(2) ?? "");
            Assert.AreEqual("  padded  ", ui.Rows[3][1]);
            Assert.AreEqual("{0:0.##} / {1}", ui.Rows[3][2]);
        }

        [Test]
        public void Xlsx_ProtectedSheet_LocksOnlyKeyColumn()
        {
            var sheet = new XlsxSheet("UI", new List<string[]> { new[] { "Key", "English (en)" }, new[] { "A", "b" } })
            {
                Protected = true,
                LockedColumns = new[] { 0 },
            };
            var stream = new MemoryStream();
            XlsxWriter.Write(stream, new List<XlsxSheet> { sheet });

            string sheetXml = ReadEntry(stream, "xl/worksheets/sheet1.xml");
            string styles = ReadEntry(stream, "xl/styles.xml");

            StringAssert.Contains("<sheetProtection", sheetXml);
            StringAssert.Contains("<protection locked=\"0\"/>", styles);
        }

        [Test]
        public void XlsxReader_ReadsSharedStringsRichTextAndSparseCells()
        {
            // What Excel itself writes after a partner saves: shared strings, rich-text runs, skipped empty cells.
            var stream = new MemoryStream();
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                Add(zip, "xl/workbook.xml",
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                    "<sheets><sheet name=\"UI\" sheetId=\"1\" r:id=\"rId7\"/></sheets></workbook>");
                Add(zip, "xl/_rels/workbook.xml.rels",
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId7\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/data.xml\"/></Relationships>");
                Add(zip, "xl/sharedStrings.xml",
                    "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                    "<si><t>Key</t></si><si><r><t>Hel</t></r><r><t xml:space=\"preserve\">lo </t></r><r><t>world</t></r></si></sst>");
                Add(zip, "xl/worksheets/data.xml",
                    "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
                    "<row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"C1\" t=\"s\"><v>1</v></c></row>" +
                    "<row r=\"3\"><c r=\"B3\" t=\"str\"><v>formula</v></c><c r=\"D3\"><v>42</v></c></row>" +
                    "</sheetData></worksheet>");
            }
            stream.Position = 0;

            XlsxSheet sheet = XlsxReader.Read(stream).Single();

            Assert.AreEqual("UI", sheet.Name);
            CollectionAssert.AreEqual(new[] { "Key", "", "Hello world" }, sheet.Rows[0]);
            Assert.AreEqual(0, sheet.Rows[1].Length);
            CollectionAssert.AreEqual(new[] { "", "formula", "", "42" }, sheet.Rows[2]);
        }

        // ── Placeholders ────────────────────────────────────────────────────

        [TestCase("Total {0:0.##} {1}", "Gesamt {0:0.##} {1}", true)]
        [TestCase("{0} of {1}", "{1} de {0}", true)]
        [TestCase("No placeholders", "Sin marcadores", true)]
        [TestCase("Stage {0}/{1}", "Etapa {0}", false)]
        [TestCase("Stage {0}", "Etapa {0} {2}", false)]
        [TestCase("{0:0}%", "{0}%", false)]
        public void Placeholders_MatchReference(string reference, string translation, bool expected)
        {
            Assert.AreEqual(expected, LocalizationPlaceholders.Match(reference, translation));
        }

        // ── UI.csv ──────────────────────────────────────────────────────────

        [Test]
        public void CsvFile_RealUiCsv_RoundTripsByteForByte()
        {
            byte[] original = File.ReadAllBytes(UiCsvPath);

            LocalizationCsvFile csv = LocalizationCsvFile.Parse(Encoding.UTF8.GetString(original));

            CollectionAssert.AreEqual(original, Encoding.UTF8.GetBytes(csv.Serialize()));
        }

        [Test]
        public void CsvFile_SetValue_ChangesOnlyThatCellAndUsesCrLfInside()
        {
            string text = "Key,Id,English(en),Spanish(es)\r\n\"A\",1,\"One\",\"Uno\"\r\n\"B\",2,\"Two\",\"Dos\"";
            LocalizationCsvFile csv = LocalizationCsvFile.Parse(text);

            Assert.IsTrue(csv.TrySetValue("B", "es", "Dos\n\"dos\""));
            Assert.IsFalse(csv.TrySetValue("MISSING", "es", "x"));
            Assert.IsFalse(csv.TrySetValue("A", "fr", "x"));

            Assert.AreEqual("Key,Id,English(en),Spanish(es)\r\n\"A\",1,\"One\",\"Uno\"\r\n\"B\",2,\"Two\",\"Dos\r\n\"\"dos\"\"\"", csv.Serialize());
            Assert.AreEqual("Dos\r\n\"dos\"", csv.GetValue("B", "es"));
        }

        // ── Import planner ──────────────────────────────────────────────────

        [Test]
        public void Planner_AppliesChangedCellsOnly_AndReportsProblems()
        {
            var current = new Dictionary<string, Dictionary<string, string>>
            {
                ["GREETING"] = new Dictionary<string, string> { ["en"] = "Hello", ["es"] = "Hola", ["de"] = "Hallo" },
                ["MULTI"] = new Dictionary<string, string> { ["en"] = "A\r\nB", ["es"] = "A\r\nB" },
                ["COUNT"] = new Dictionary<string, string> { ["en"] = "{0} items", ["es"] = "{0} elementos" },
            };
            var rows = new List<string[]>
            {
                new[] { "Key", "English (en)", "Spanish (es)", "German (de)", "Notes" },
                new[] { "GREETING", "Hello", "¡Hola!", "", "ignored" },
                new[] { "MULTI", "A\nB", "A\nB" },
                new[] { "COUNT", "{0} items", "elementos" },
                new[] { "NEW_KEY", "New", "Nuevo" },
                new[] { "", "orphan" },
            };

            LocalizationImportPlan plan = LocalizationImportPlanner.Plan("UI", rows, current);

            Assert.AreEqual(1, plan.Changes.Count);
            Assert.AreEqual(("GREETING", "es", "¡Hola!"), (plan.Changes[0].Key, plan.Changes[0].Locale, plan.Changes[0].Value));
            CollectionAssert.AreEqual(new[] { "NEW_KEY" }, plan.UnknownKeys);
            Assert.AreEqual(1, plan.PlaceholderErrors.Count);
            Assert.AreEqual(("COUNT", "es"), (plan.PlaceholderErrors[0].Key, plan.PlaceholderErrors[0].Locale));
        }

        [Test]
        public void Planner_KeepsCrLfConventionOfCurrentValue()
        {
            var current = new Dictionary<string, Dictionary<string, string>>
            {
                ["MULTI"] = new Dictionary<string, string> { ["en"] = "A\r\nB", ["es"] = "A\r\nB" },
            };
            var rows = new List<string[]> { new[] { "Key", "English (en)", "Spanish (es)" }, new[] { "MULTI", "A\nB", "X\nY" } };

            LocalizationImportPlan plan = LocalizationImportPlanner.Plan("UI", rows, current);

            Assert.AreEqual("X\r\nY", plan.Changes.Single().Value);
        }

        [Test]
        public void Planner_MissingKeyColumn_ReportsError()
        {
            var rows = new List<string[]> { new[] { "Clave", "English (en)" } };

            LocalizationImportPlan plan = LocalizationImportPlanner.Plan("UI", rows, new Dictionary<string, Dictionary<string, string>>());

            Assert.IsNotNull(plan.Error);
            Assert.AreEqual(0, plan.Changes.Count);
        }

        private static string UiCsvPath => Path.Combine(Application.dataPath, "Foodmission/localization/CSV/UI.csv");

        private static void Add(ZipArchive zip, string name, string content)
        {
            using (var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static string ReadEntry(MemoryStream xlsx, string name)
        {
            xlsx.Position = 0;
            using (var zip = new ZipArchive(xlsx, ZipArchiveMode.Read, true))
            using (var reader = new StreamReader(zip.GetEntry(name).Open()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
