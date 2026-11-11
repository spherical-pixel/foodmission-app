using System.Globalization;

using NUnit.Framework;

using UnityEditor;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionCheckInSummaryLocalizationTests
    {
        private const string Key = "MISSION_CHECKIN_SUMMARY_MEALS";

        // Plural forms follow the CLDR rules of each language: 0 takes the plural form, pl/sl have 4 forms
        [TestCase("en", 0, "Mon: 0 meals")]
        [TestCase("en", 1, "Mon: 1 meal")]
        [TestCase("en", 2, "Mon: 2 meals")]
        [TestCase("el", 0, "Mon: 0 γεύματα")]
        [TestCase("el", 1, "Mon: 1 γεύμα")]
        [TestCase("el", 3, "Mon: 3 γεύματα")]
        [TestCase("es", 1, "Mon: 1 comida")]
        [TestCase("es", 4, "Mon: 4 comidas")]
        [TestCase("pl", 1, "Mon: 1 posiłek")]
        [TestCase("pl", 3, "Mon: 3 posiłki")]
        [TestCase("pl", 5, "Mon: 5 posiłków")]
        [TestCase("pl", 0, "Mon: 0 posiłków")]
        [TestCase("sl", 1, "Mon: 1 obrok")]
        [TestCase("sl", 2, "Mon: 2 obroka")]
        [TestCase("sl", 3, "Mon: 3 obroki")]
        [TestCase("sl", 5, "Mon: 5 obrokov")]
        [TestCase("sl", 0, "Mon: 0 obrokov")]
        public void SummaryMeals_UsesPluralFormOfEachLanguage(string code, int count, string expected)
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>($"Assets/Foodmission/localization/UI_{code}.asset");
            Assert.IsNotNull(table, $"UI_{code} table not found");
            StringTableEntry entry = table.GetEntry(Key);
            Assert.IsNotNull(entry, $"{Key} missing in UI_{code}");
            Assert.IsTrue(entry.IsSmart, $"{Key} must be Smart in UI_{code}");

            string text = LocalizationSettings.StringDatabase.SmartFormatter.Format(new CultureInfo(code), entry.Value, "Mon", count);

            Assert.AreEqual(expected, text);
        }

        [TestCase("nl")]
        [TestCase("de")]
        [TestCase("it")]
        [TestCase("no")]
        public void SummaryMeals_IsSmartInEveryOtherLanguage(string code)
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>($"Assets/Foodmission/localization/UI_{code}.asset");
            Assert.IsNotNull(table, $"UI_{code} table not found");
            Assert.IsTrue(table.GetEntry(Key).IsSmart, $"{Key} must be Smart in UI_{code}");
        }
    }
}
