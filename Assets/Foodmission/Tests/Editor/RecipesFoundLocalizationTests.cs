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

        // Plural forms follow the CLDR rules of each language (linguists asked for a singular): 0 takes the plural form, pl/sl have 4 forms
        [TestCase("en", 0, "0 recipes found")]
        [TestCase("en", 1, "1 recipe found")]
        [TestCase("en", 2, "2 recipes found")]
        [TestCase("es", 1, "1 receta encontrada")]
        [TestCase("es", 3, "3 recetas encontradas")]
        [TestCase("el", 1, "Βρέθηκε 1 συνταγή")]
        [TestCase("el", 4, "Βρέθηκαν 4 συνταγές")]
        [TestCase("pl", 1, "Znaleziono 1 przepis")]
        [TestCase("pl", 3, "Znaleziono 3 przepisy")]
        [TestCase("pl", 5, "Znaleziono 5 przepisów")]
        [TestCase("sl", 1, "Najden 1 recept")]
        [TestCase("sl", 2, "Najdena 2 recepta")]
        [TestCase("sl", 3, "Najdeni 3 recepti")]
        [TestCase("sl", 5, "Najdenih 5 receptov")]
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
