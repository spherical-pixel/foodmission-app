using System.IO;

using eu.foodmission.platform.Components;

using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    public class ProductScoreLogosTests
    {
        [TestCase("c", "c")]
        [TestCase(" E ", "e")]
        [TestCase("not-applicable", "not-applicable")]
        [TestCase("unknown", "unknown")]
        [TestCase("", "unknown")]
        [TestCase(null, "unknown")]
        [TestCase("f", "unknown")]
        public void NutriScoreValue_NormalizesTheGrade(string grade, string expected)
        {
            Assert.AreEqual(expected, ProductScoreLogos.NutriScoreValue(grade));
        }

        [TestCase(1, "1")]
        [TestCase(4, "4")]
        [TestCase(0, "unknown")]
        [TestCase(5, "unknown")]
        public void NovaValue_OnlyGroupsOneToFour(int group, string expected)
        {
            Assert.AreEqual(expected, ProductScoreLogos.NovaValue(group));
        }

        [TestCase("a-plus", "a-plus")]
        [TestCase("A+", "a-plus")]
        [TestCase("f", "f")]
        [TestCase("b", "b")]
        [TestCase("not-applicable", "not-applicable")]
        [TestCase("g", "unknown")]
        public void GreenScoreValue_IncludesAPlusAndF(string grade, string expected)
        {
            Assert.AreEqual(expected, ProductScoreLogos.GreenScoreValue(grade));
        }

        [TestCase(ProductScore.NutriScore, "c", "Nutri-Score C")]
        [TestCase(ProductScore.Nova, "3", "NOVA 3")]
        [TestCase(ProductScore.GreenScore, "a-plus", "Green-Score A+")]
        [TestCase(ProductScore.GreenScore, "unknown", null)]
        public void DisplayName_MatchesTheLogo(ProductScore score, string value, string expected)
        {
            Assert.AreEqual(expected, ProductScoreLogos.DisplayName(score, value));
        }

        [TestCase("c", "SCORE_INFO_THIS_PRODUCT")]
        [TestCase("unknown", "SCORE_INFO_UNKNOWN")]
        [TestCase("not-applicable", "SCORE_INFO_NOT_APPLICABLE")]
        public void ValueKey_DependsOnTheValue(string value, string expected)
        {
            Assert.AreEqual(expected, ProductScoreLogos.ValueKey(value));
        }

        [Test]
        public void EveryValue_HasALogoFileAndAUssClass()
        {
            string uss = File.ReadAllText(Path.Combine(Application.dataPath, "Foodmission/AppUI/Foodmission_Global_Styles.uss"));
            var cases = new (ProductScore Score, string File, string[] Values)[]
            {
                (ProductScore.NutriScore, "nutriscore-", new[] { "a", "b", "c", "d", "e", "unknown", "not-applicable" }),
                (ProductScore.Nova, "nova-group-", new[] { "1", "2", "3", "4", "unknown" }),
                (ProductScore.GreenScore, "green-score-", new[] { "a-plus", "a", "b", "c", "d", "e", "f", "unknown", "not-applicable" })
            };
            foreach (var c in cases)
            {
                foreach (string value in c.Values)
                {
                    string file = c.File + value + ".svg";
                    Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, "Foodmission/graphics/scores", file)), $"Missing {file}");
                    string cls = "." + ProductScoreLogos.ClassFor(c.Score, value) + " ";
                    StringAssert.Contains(cls, uss, $"Missing USS class {cls}");
                    StringAssert.Contains("graphics/scores/" + file, uss, $"USS doesn't use {file}");
                }
            }
        }

        [Test]
        public void InfoTexts_ExistInUiCsvForEveryLanguage()
        {
            var csv = UiCsv.Read();
            foreach (string key in new[] { "SCORE_INFO_NUTRISCORE", "SCORE_INFO_NOVA", "SCORE_INFO_GREEN_SCORE", "SCORE_INFO_THIS_PRODUCT", "SCORE_INFO_UNKNOWN", "SCORE_INFO_NOT_APPLICABLE" })
            {
                Assert.IsTrue(csv.TryGetValue(key, out string[] cells), $"Missing UI key {key}");
                for (int i = 2; i <= 10; i++)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(cells[i]), $"{key} has no text in column {i}");
                }
            }
        }
    }
}
