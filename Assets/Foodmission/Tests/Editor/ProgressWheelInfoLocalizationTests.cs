using System.Collections.Generic;
using System.Text.RegularExpressions;

using NUnit.Framework;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProgressWheelInfoLocalizationTests
    {
        private const int FirstLanguageColumn = 2;
        private const int LastLanguageColumn = 10;

        private static readonly string[] s_BoldParagraphs =
        {
            "WHEEL_INFO_CO2_REDUCTION_P2",
            "WHEEL_INFO_ENERGY_REDUCTION_P2",
            "WHEEL_INFO_ENERGY_REDUCTION_P3",
            "WHEEL_INFO_WATER_SAVINGS_P2",
            "WHEEL_INFO_LAND_USE_REDUCTION_P2",
        };

        private Dictionary<string, string[]> _rows;

        [OneTimeSetUp]
        public void LoadCsv()
        {
            _rows = UiCsv.Read();
        }

        [Test]
        public void EveryKey_ExistsInUiCsv_WithAllLanguages()
        {
            foreach (string key in ProgressWheelInfoCatalog.AllKeys())
            {
                Assert.IsTrue(_rows.ContainsKey(key), $"Missing UI.csv key {key}");
                string[] cells = _rows[key];
                for (int i = FirstLanguageColumn; i <= LastLanguageColumn; i++)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(cells[i]), $"{key} has an empty translation in column {i}");
                }
            }
        }

        [Test]
        public void ValueKeys_UseCultureAwarePlaceholder()
        {
            for (int i = FirstLanguageColumn; i <= LastLanguageColumn; i++)
            {
                StringAssert.Contains("{0:0.##}", _rows[ProgressWheelInfoCatalog.ValueKey][i]);
                StringAssert.Contains("{1}", _rows[ProgressWheelInfoCatalog.ValueKey][i]);
                foreach (WheelAmountUnit unit in (WheelAmountUnit[])System.Enum.GetValues(typeof(WheelAmountUnit)))
                {
                    StringAssert.Contains("{0:0.##}", _rows[ProgressWheelInfoCatalog.AmountKey(unit)][i], $"{unit} column {i}");
                }
            }
        }

        [Test]
        public void ParagraphKeys_BoldTagsBalanced()
        {
            foreach (string kind in ProgressWheelText.KnownKinds)
            {
                for (int n = 1; n <= ProgressWheelInfoCatalog.ParagraphCount(kind); n++)
                {
                    string key = ProgressWheelInfoCatalog.ParagraphKey(kind, n);
                    bool mustBeBold = System.Array.IndexOf(s_BoldParagraphs, key) >= 0;
                    for (int i = FirstLanguageColumn; i <= LastLanguageColumn; i++)
                    {
                        string text = _rows[key][i];
                        int open = Regex.Matches(text, "<b>").Count;
                        int close = Regex.Matches(text, "</b>").Count;
                        Assert.AreEqual(open, close, $"{key} column {i} has unbalanced <b> tags");
                        if (mustBeBold)
                        {
                            Assert.GreaterOrEqual(open, 1, $"{key} column {i} lost its bold unit");
                        }
                    }
                }
            }
        }
    }
}
