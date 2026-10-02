using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using UnityEngine;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProgressWheelTextTests
    {
        [Test]
        public void Keys_ForKnownKindsAndSegments()
        {
            Assert.AreEqual("WHEEL_CO2_REDUCTION", ProgressWheelText.NameKey("CO2_REDUCTION"));
            Assert.AreEqual("WHEEL_UNIT_WATER_SAVINGS", ProgressWheelText.UnitKey("WATER_SAVINGS"));
            Assert.AreEqual("WHEEL_STAGE_ADVANCED_5", ProgressWheelText.StageTitleKey("ADVANCED", 5));
            Assert.AreEqual("SEGMENT_INTERMEDIATE", ProgressWheelText.SegmentKey("INTERMEDIATE"));
        }

        [Test]
        public void Keys_UnknownValues_ReturnNull()
        {
            Assert.IsNull(ProgressWheelText.NameKey("DIET_CHANGES"));
            Assert.IsNull(ProgressWheelText.UnitKey(null));
            Assert.IsNull(ProgressWheelText.StageTitleKey("BEGINNER", 6));
            Assert.IsNull(ProgressWheelText.StageTitleKey("BEGINNER", 0));
            Assert.IsNull(ProgressWheelText.StageTitleKey("EXPERT", 1));
            Assert.IsNull(ProgressWheelText.SegmentKey(""));
        }

        [TestCase("CO2_REDUCTION", "fm-wheel-card--co2")]
        [TestCase("ENERGY_REDUCTION", "fm-wheel-card--energy")]
        [TestCase("WATER_SAVINGS", "fm-wheel-card--water")]
        [TestCase("LAND_USE_REDUCTION", "fm-wheel-card--land")]
        [TestCase("SOMETHING_NEW", "fm-wheel-card--default")]
        [TestCase(null, "fm-wheel-card--default")]
        public void ColorClass_PerKind(string kind, string expected)
        {
            Assert.AreEqual(expected, ProgressWheelText.ColorClass(kind));
        }

        [TestCase(-5f, 0f)]
        [TestCase(37.5f, 37.5f)]
        [TestCase(140f, 100f)]
        [TestCase(float.NaN, 0f)]
        public void ClampPercent(float input, float expected)
        {
            Assert.AreEqual(expected, ProgressWheelText.ClampPercent(input));
        }

        [Test]
        public void AllKeys_ContainsEveryStageTitle()
        {
            var keys = ProgressWheelText.AllKeys().ToList();
            Assert.AreEqual(15, keys.Count(k => k.StartsWith("WHEEL_STAGE_")));
            Assert.AreEqual(keys.Count, keys.Distinct().Count());
        }

        [Test]
        public void EveryKey_ExistsInUiCsv_WithAllLanguages()
        {
            Dictionary<string, string[]> rows = ReadUiCsv();
            foreach (string key in ProgressWheelText.AllKeys())
            {
                Assert.IsTrue(rows.ContainsKey(key), $"Missing UI.csv key {key}");
                string[] cells = rows[key];
                for (int i = 2; i < 11; i++)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(cells[i]), $"{key} has an empty translation in column {i}");
                }
            }
        }

        [Test]
        public void NumericFormats_UseCultureAwarePlaceholders()
        {
            Dictionary<string, string[]> rows = ReadUiCsv();
            StringAssert.Contains("{0:0.##}", rows["WHEEL_VALUE"][2]);
            StringAssert.Contains("{1:0.##}", rows["WHEEL_VALUE"][2]);
            StringAssert.Contains("{0:0.##}", rows["WHEEL_TOTAL"][2]);
        }

        // Minimal RFC-4180 reader: quoted cells may contain commas and \r\n.
        private static Dictionary<string, string[]> ReadUiCsv()
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
