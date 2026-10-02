using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace eu.foodmission.platform.EditorTools
{
    /// <summary>One worksheet: rows of cell texts (row 0 is the header on data sheets).</summary>
    public sealed class XlsxSheet
    {
        public string Name;
        public List<string[]> Rows;
        /// <summary>Protects the sheet (no password); only cells outside <see cref="LockedColumns"/> stay editable.</summary>
        public bool Protected;
        public int[] LockedColumns = Array.Empty<int>();
        public bool FreezeHeader;
        public double[] ColumnWidths = Array.Empty<double>();

        public XlsxSheet(string name, List<string[]> rows)
        {
            Name = name;
            Rows = rows ?? new List<string[]>();
        }
    }

    /// <summary>Minimal .xlsx (Office Open XML) writer: inline strings, wrap text, frozen header, sheet protection.</summary>
    public static class XlsxWriter
    {
        private const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        // cellXfs indexes in styles.xml
        private const int StyleLocked = 1;
        private const int StyleEditable = 2;
        private const int StyleHeader = 3;

        public static void Write(Stream output, IList<XlsxSheet> sheets)
        {
            using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                AddEntry(zip, "[Content_Types].xml", ContentTypes(sheets.Count));
                AddEntry(zip, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");
                AddEntry(zip, "xl/workbook.xml", Workbook(sheets));
                AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRels(sheets.Count));
                AddEntry(zip, "xl/styles.xml", Styles());
                for (int i = 0; i < sheets.Count; i++)
                {
                    AddEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", Worksheet(sheets[i]));
                }
            }
        }

        private static void AddEntry(ZipArchive zip, string name, string content)
        {
            using (var writer = new StreamWriter(zip.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static string ContentTypes(int sheetCount)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            for (int i = 1; i <= sheetCount; i++)
            {
                sb.Append($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            }
            sb.Append("</Types>");
            return sb.ToString();
        }

        private static string Workbook(IList<XlsxSheet> sheets)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append($"<workbook xmlns=\"{MainNs}\" xmlns:r=\"{RelNs}\"><sheets>");
            for (int i = 0; i < sheets.Count; i++)
            {
                sb.Append($"<sheet name=\"{Escape(sheets[i].Name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
            }
            sb.Append("</sheets></workbook>");
            return sb.ToString();
        }

        private static string WorkbookRels(int sheetCount)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (int i = 1; i <= sheetCount; i++)
            {
                sb.Append($"<Relationship Id=\"rId{i}\" Type=\"{RelNs}/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>");
            }
            sb.Append($"<Relationship Id=\"rId{sheetCount + 1}\" Type=\"{RelNs}/styles\" Target=\"styles.xml\"/>");
            sb.Append("</Relationships>");
            return sb.ToString();
        }

        private static string Styles()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                   $"<styleSheet xmlns=\"{MainNs}\">" +
                   "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                   "<fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
                   "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFE2EFDA\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
                   "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                   "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                   "<cellXfs count=\"4\">" +
                   "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                   "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment wrapText=\"1\" vertical=\"top\"/></xf>" +
                   "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\" applyProtection=\"1\"><alignment wrapText=\"1\" vertical=\"top\"/><protection locked=\"0\"/></xf>" +
                   "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyAlignment=\"1\"><alignment wrapText=\"1\" vertical=\"top\"/></xf>" +
                   "</cellXfs>" +
                   "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                   "</styleSheet>";
        }

        private static string Worksheet(XlsxSheet sheet)
        {
            int columnCount = sheet.Rows.Count == 0 ? 1 : Math.Max(1, sheet.Rows.Max(r => r?.Length ?? 0));
            var locked = new HashSet<int>(sheet.LockedColumns ?? Array.Empty<int>());

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append($"<worksheet xmlns=\"{MainNs}\" xmlns:r=\"{RelNs}\">");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\">");
            if (sheet.FreezeHeader)
            {
                sb.Append("<pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/>");
            }
            sb.Append("</sheetView></sheetViews>");
            sb.Append("<sheetFormatPr defaultRowHeight=\"15\"/>");

            sb.Append("<cols>");
            for (int c = 0; c < columnCount; c++)
            {
                double width = c < sheet.ColumnWidths.Length ? sheet.ColumnWidths[c] : 40;
                int style = CellStyle(sheet, locked, c, isHeader: false);
                sb.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" customWidth=\"1\" style=\"{style}\"/>");
            }
            sb.Append("</cols>");

            sb.Append("<sheetData>");
            for (int r = 0; r < sheet.Rows.Count; r++)
            {
                string[] row = sheet.Rows[r] ?? Array.Empty<string>();
                bool isHeader = sheet.FreezeHeader && r == 0;
                sb.Append($"<row r=\"{r + 1}\">");
                for (int c = 0; c < columnCount; c++)
                {
                    string value = c < row.Length ? row[c] : null;
                    int style = CellStyle(sheet, locked, c, isHeader);
                    string reference = ColumnName(c) + (r + 1);
                    if (string.IsNullOrEmpty(value))
                    {
                        // Empty editable cells still need the unlocked style so partners can type into them.
                        sb.Append($"<c r=\"{reference}\" s=\"{style}\"/>");
                        continue;
                    }
                    sb.Append($"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Escape(NormalizeNewlines(value))}</t></is></c>");
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData>");

            if (sheet.Protected)
            {
                sb.Append("<sheetProtection sheet=\"1\" objects=\"1\" scenarios=\"1\" formatColumns=\"0\" formatRows=\"0\" autoFilter=\"0\" sort=\"0\"/>");
            }
            sb.Append("</worksheet>");
            return sb.ToString();
        }

        private static int CellStyle(XlsxSheet sheet, HashSet<int> locked, int column, bool isHeader)
        {
            if (isHeader)
            {
                return StyleHeader;
            }
            if (!sheet.Protected)
            {
                return StyleLocked;
            }
            return locked.Contains(column) ? StyleLocked : StyleEditable;
        }

        public static string ColumnName(int index)
        {
            string name = "";
            int n = index + 1;
            while (n > 0)
            {
                int rem = (n - 1) % 26;
                name = (char)('A' + rem) + name;
                n = (n - 1) / 26;
            }
            return name;
        }

        private static string NormalizeNewlines(string value)
        {
            return value.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        private static string Escape(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    case '>': sb.Append("&gt;"); break;
                    case '"': sb.Append("&quot;"); break;
                    default:
                        // XML 1.0 forbids most control characters.
                        if (ch >= 0x20 || ch == '\n' || ch == '\t')
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>Minimal .xlsx reader: sheet names, shared/inline/rich strings and plain values, sparse cells.</summary>
    public static class XlsxReader
    {
        private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly XNamespace PackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";

        public static List<XlsxSheet> Read(Stream input)
        {
            using (var zip = new ZipArchive(input, ZipArchiveMode.Read, true))
            {
                XDocument workbook = Load(zip, "xl/workbook.xml") ?? throw new InvalidDataException("Not an .xlsx file: xl/workbook.xml missing.");
                XDocument rels = Load(zip, "xl/_rels/workbook.xml.rels");
                var targets = new Dictionary<string, string>();
                if (rels != null)
                {
                    foreach (XElement relationship in rels.Root.Elements(PackageRel + "Relationship"))
                    {
                        targets[(string)relationship.Attribute("Id")] = (string)relationship.Attribute("Target");
                    }
                }

                List<string> shared = ReadSharedStrings(zip);
                var result = new List<XlsxSheet>();
                foreach (XElement sheet in workbook.Root.Element(Main + "sheets")?.Elements(Main + "sheet") ?? Enumerable.Empty<XElement>())
                {
                    string relId = (string)sheet.Attribute(Rel + "id");
                    if (relId == null || !targets.TryGetValue(relId, out string target))
                    {
                        continue;
                    }
                    string path = target.StartsWith("/", StringComparison.Ordinal) ? target.TrimStart('/') : "xl/" + target;
                    XDocument data = Load(zip, path);
                    result.Add(new XlsxSheet((string)sheet.Attribute("name"), data == null ? new List<string[]>() : ReadRows(data, shared)));
                }
                return result;
            }
        }

        private static XDocument Load(ZipArchive zip, string path)
        {
            ZipArchiveEntry entry = zip.GetEntry(path);
            if (entry == null)
            {
                return null;
            }
            using (Stream stream = entry.Open())
            {
                return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            }
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            XDocument doc = Load(zip, "xl/sharedStrings.xml");
            if (doc == null)
            {
                return new List<string>();
            }
            return doc.Root.Elements(Main + "si").Select(TextOf).ToList();
        }

        /// <summary>Concatenates the &lt;t&gt; runs of a string item, skipping phonetic (rPh) runs.</summary>
        private static string TextOf(XElement container)
        {
            var sb = new StringBuilder();
            foreach (XElement t in container.Descendants(Main + "t"))
            {
                if (t.Ancestors(Main + "rPh").Any())
                {
                    continue;
                }
                sb.Append(t.Value);
            }
            return sb.ToString();
        }

        private static List<string[]> ReadRows(XDocument sheet, List<string> shared)
        {
            var rows = new List<string[]>();
            XElement sheetData = sheet.Root.Element(Main + "sheetData");
            if (sheetData == null)
            {
                return rows;
            }

            int nextRow = 1;
            foreach (XElement row in sheetData.Elements(Main + "row"))
            {
                int rowNumber = int.TryParse((string)row.Attribute("r"), out int r) ? r : nextRow;
                while (rows.Count < rowNumber - 1)
                {
                    rows.Add(Array.Empty<string>());
                }
                nextRow = rowNumber + 1;

                var cells = new SortedDictionary<int, string>();
                int nextColumn = 0;
                foreach (XElement cell in row.Elements(Main + "c"))
                {
                    int column = ColumnIndex((string)cell.Attribute("r")) ?? nextColumn;
                    nextColumn = column + 1;
                    cells[column] = CellValue(cell, shared);
                }

                int length = 0;
                foreach (var pair in cells)
                {
                    if (!string.IsNullOrEmpty(pair.Value))
                    {
                        length = pair.Key + 1;
                    }
                }
                var values = new string[length];
                for (int c = 0; c < length; c++)
                {
                    values[c] = cells.TryGetValue(c, out string v) ? v ?? "" : "";
                }
                rows.Add(values);
            }
            return rows;
        }

        private static string CellValue(XElement cell, List<string> shared)
        {
            string type = (string)cell.Attribute("t");
            if (type == "inlineStr")
            {
                XElement inline = cell.Element(Main + "is");
                return inline == null ? "" : TextOf(inline);
            }
            string raw = cell.Element(Main + "v")?.Value ?? "";
            if (type == "s")
            {
                return int.TryParse(raw, out int index) && index >= 0 && index < shared.Count ? shared[index] : "";
            }
            return raw;
        }

        /// <summary>"C12" → 2; null when the reference is missing or malformed.</summary>
        public static int? ColumnIndex(string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return null;
            }
            int index = 0;
            int letters = 0;
            foreach (char ch in reference)
            {
                if (ch >= 'A' && ch <= 'Z')
                {
                    index = index * 26 + (ch - 'A' + 1);
                    letters++;
                }
                else
                {
                    break;
                }
            }
            return letters == 0 ? (int?)null : index - 1;
        }
    }
}
