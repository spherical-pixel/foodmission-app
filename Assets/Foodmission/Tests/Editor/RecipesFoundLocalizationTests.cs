using System.Globalization;

using NUnit.Framework;

using UnityEditor;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class RecipesFoundLocalizationTests
    {
        private const string Key = "RECIPES_FOUND_N";

        // English uses plural forms (linguists asked for a singular); the other languages keep "({0})" until translated
        [TestCase("en", 0, "0 recipes found")]
        [TestCase("en", 1, "1 recipe found")]
        [TestCase("en", 2, "2 recipes found")]
        [TestCase("es", 1, "Recetas encontradas (1)")]
        public void RecipesFound_FormatsTheCount(string code, int count, string expected)
        {
            StringTableEntry entry = LoadEntry(code);

            string text = LocalizationSettings.StringDatabase.SmartFormatter.Format(new CultureInfo(code), entry.Value, count);

            Assert.AreEqual(expected, text);
        }

        [TestCase("en")]
        [TestCase("nl")]
        [TestCase("de")]
        [TestCase("el")]
        [TestCase("it")]
        [TestCase("no")]
        [TestCase("pl")]
        [TestCase("sl")]
        [TestCase("es")]
        public void RecipesFound_IsSmartInEveryLanguage(string code)
        {
            Assert.IsTrue(LoadEntry(code).IsSmart, $"{Key} must be Smart in UI_{code}");
        }

        private static StringTableEntry LoadEntry(string code)
        {
            var table = AssetDatabase.LoadAssetAtPath<StringTable>($"Assets/Foodmission/localization/UI_{code}.asset");
            Assert.IsNotNull(table, $"UI_{code} table not found");
            StringTableEntry entry = table.GetEntry(Key);
            Assert.IsNotNull(entry, $"{Key} missing in UI_{code}");
            return entry;
        }
    }
}
