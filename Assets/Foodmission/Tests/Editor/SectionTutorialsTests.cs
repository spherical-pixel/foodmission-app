using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using Unity.AppUI.Navigation.Generated;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SectionTutorialsTests
    {
        [Test]
        public void All_HasTheTwelveIds()
        {
            CollectionAssert.AreEquivalent(new[]
            {
                "home", "meal_log", "quick_meal_log", "quick_search", "shopping_list", "pantry",
                "quests", "missions", "challenges", "knowledge", "recipes", "food_waste"
            }, SectionTutorials.All);
        }

        [Test]
        public void IdForDestination_MapsCoveredDestinations()
        {
            Assert.AreEqual("home", SectionTutorials.IdForDestination(Destinations.home));
            Assert.AreEqual("meal_log", SectionTutorials.IdForDestination(Destinations.meallog));
            Assert.AreEqual("quick_meal_log", SectionTutorials.IdForDestination(Destinations.quick_meallog));
            Assert.AreEqual("quick_search", SectionTutorials.IdForDestination("QUICK_SEARCH"));
            Assert.AreEqual("shopping_list", SectionTutorials.IdForDestination(Destinations.shoppinglist));
            // The lists screen opens the last list at once: users land on the detail, so both share the tutorial
            Assert.AreEqual("shopping_list", SectionTutorials.IdForDestination(Destinations.shoppinglistdetail));
            Assert.AreEqual("pantry", SectionTutorials.IdForDestination(Destinations.pantry));
            Assert.AreEqual("quests", SectionTutorials.IdForDestination(Destinations.quests));
            Assert.AreEqual("missions", SectionTutorials.IdForDestination(Destinations.missions));
            Assert.AreEqual("challenges", SectionTutorials.IdForDestination(Destinations.challenges));
            Assert.AreEqual("knowledge", SectionTutorials.IdForDestination(Destinations.knowledge));
            Assert.AreEqual("recipes", SectionTutorials.IdForDestination("RecipeBook"));
            Assert.AreEqual("food_waste", SectionTutorials.IdForDestination(Destinations.foodwaste));
        }

        [Test]
        public void IdForDestination_Uncovered_ReturnsNull()
        {
            Assert.IsNull(SectionTutorials.IdForDestination(Destinations.badges));
            Assert.IsNull(SectionTutorials.IdForDestination(null));
        }

        [Test]
        public void MessageKeys_Home_HasFiveSteps()
        {
            CollectionAssert.AreEqual(new[] { "TUTORIAL_HOME_1", "TUTORIAL_HOME_2", "TUTORIAL_HOME_3", "TUTORIAL_HOME_4", "TUTORIAL_HOME_5" },
                SectionTutorials.MessageKeys(SectionTutorials.Home));
        }

        [Test]
        public void MessageKeys_EveryId_HasKeys()
        {
            foreach (string id in SectionTutorials.All)
            {
                CollectionAssert.IsNotEmpty(SectionTutorials.MessageKeys(id), id);
            }
            CollectionAssert.AreEqual(new[] { "TUTORIAL_MEAL_LOG" }, SectionTutorials.MessageKeys(SectionTutorials.MealLog));
            CollectionAssert.AreEqual(new[] { "TUTORIAL_QUICK_MEAL_LOG" }, SectionTutorials.MessageKeys(SectionTutorials.QuickMealLog));
            CollectionAssert.IsEmpty(SectionTutorials.MessageKeys("unknown"));
        }

        [Test]
        public void AllTutorialKeys_ExistInUiCsv()
        {
            string path = Path.Combine(Application.dataPath, "Foodmission/localization/CSV/UI.csv");
            var keys = new HashSet<string>();
            foreach (string line in File.ReadAllLines(path))
            {
                if (line.StartsWith("\""))
                {
                    int end = line.IndexOf('"', 1);
                    if (end > 1)
                    {
                        keys.Add(line.Substring(1, end - 1));
                    }
                }
            }

            var expected = new List<string>
            {
                SectionTutorials.NextKey, SectionTutorials.SkipKey, SectionTutorials.StartKey,
                SectionTutorials.GotItKey, SectionTutorials.HelpKey
            };
            foreach (string id in SectionTutorials.All)
            {
                expected.AddRange(SectionTutorials.MessageKeys(id));
            }
            foreach (string key in expected)
            {
                Assert.IsTrue(keys.Contains(key), $"Missing UI key {key}");
            }
        }
    }
}
