using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    public class MealLogQuestionTests
    {
        [TestCase("BREAKFAST", "MEAL_LOG_Q_BREAKFAST")]
        [TestCase("LUNCH", "MEAL_LOG_Q_LUNCH")]
        [TestCase("DINNER", "MEAL_LOG_Q_DINNER")]
        [TestCase("SNACK", "MEAL_LOG_Q_SNACK")]
        [TestCase("DRINKS", "MEAL_LOG_Q_DRINKS")]
        [TestCase("OTHER", "MEAL_LOG_Q_OTHER")]
        [TestCase("drinks", "MEAL_LOG_Q_DRINKS")]
        public void KeyFor_KnownTypeOfMeal_ReturnsItsOwnQuestion(string code, string expected)
        {
            Assert.AreEqual(expected, MealLogQuestion.KeyFor(code));
        }

        [TestCase("BRUNCH")]
        [TestCase("")]
        [TestCase(null)]
        public void KeyFor_UnknownTypeOfMeal_ReturnsNull(string code)
        {
            // The screen falls back to txtWHAT_DID_YOU_HAVE_FOR with the backend label
            Assert.IsNull(MealLogQuestion.KeyFor(code));
        }

        [Test]
        public void Questions_ExistInUiCsvForEveryLanguage()
        {
            var csv = UiCsv.Read();
            foreach (string code in new[] { "BREAKFAST", "LUNCH", "DINNER", "SNACK", "DRINKS", "OTHER" })
            {
                string key = MealLogQuestion.KeyFor(code);
                Assert.IsTrue(csv.TryGetValue(key, out string[] cells), $"Missing UI key {key}");
                for (int i = 2; i <= 10; i++)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(cells[i]), $"{key} has no text in column {i}");
                }
            }
        }
    }
}
