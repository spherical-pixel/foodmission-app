using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MealFactsTests
    {
        private List<QuickMealSection> _sections;
        private List<QuickMealCheckItem> _all;

        [SetUp]
        public void SetUp()
        {
            _sections = MealFacts.BuildStandardSections();
            _all = MealFacts.AllItems(_sections).ToList();
        }

        private QuickMealCheckItem Item(string id) => _all.First(i => i.Id == id);

        [Test]
        public void BuildStandardSections_HasEveryQuickMealLogSectionWithNothingChecked()
        {
            CollectionAssert.AreEqual(
                new[] { "sec_diet", "sec_swaps", "sec_origin", "sec_waste", "sec_nutrition" },
                _sections.Select(s => s.Id).ToArray());
            Assert.IsTrue(_all.All(i => !i.IsChecked));
            Assert.IsTrue(_sections.All(s => !s.IsExpanded));
        }

        [Test]
        public void Toggle_ChecksAndUnchecks()
        {
            MealFacts.Toggle(Item("q_legumes"), _all);
            Assert.IsTrue(Item("q_legumes").IsChecked);

            MealFacts.Toggle(Item("q_legumes"), _all);
            Assert.IsFalse(Item("q_legumes").IsChecked);
        }

        [Test]
        public void Toggle_MeatFree_UnchecksMeatConsumed_AndViceVersa()
        {
            MealFacts.Toggle(Item("q_meat_consumed"), _all);
            MealFacts.Toggle(Item("q_meat_free"), _all);
            Assert.IsFalse(Item("q_meat_consumed").IsChecked);

            MealFacts.Toggle(Item("q_meat_consumed"), _all);
            Assert.IsFalse(Item("q_meat_free").IsChecked);
            Assert.IsTrue(Item("q_meat_consumed").IsChecked);
        }

        [Test]
        public void Toggle_SyncsOtherItemsWithTheSameEventType()
        {
            var twin = new QuickMealCheckItem { Id = "twin", EventType = ClientEventTypes.MealLegumeConsumed };
            var items = new List<QuickMealCheckItem>(_all) { twin };

            MealFacts.Toggle(Item("q_legumes"), items);

            Assert.IsTrue(twin.IsChecked);
        }

        [Test]
        public void SelectSwap_SelectsThenDeselectsSameOption()
        {
            var selector = new QuickMealCheckItem
            {
                Id = "sel",
                QuestionType = DirectQuestionType.SwapSelector,
                SwapOptions = new[] { ClientEventTypes.SwapBeefToLegumes, ClientEventTypes.SwapBeefToChicken }
            };

            MealFacts.SelectSwap(selector, ClientEventTypes.SwapBeefToChicken);
            Assert.IsTrue(selector.IsChecked);
            Assert.AreEqual(ClientEventTypes.SwapBeefToChicken, selector.EventType);

            MealFacts.SelectSwap(selector, ClientEventTypes.SwapBeefToChicken);
            Assert.IsFalse(selector.IsChecked);
            Assert.IsNull(selector.EventType);
        }

        [Test]
        public void Build_SplitsFlagsAndSwaps_Dedups_AndDropsMeatConsumedWithMeatFree()
        {
            Item("q_legumes").IsChecked = true;
            Item("q_meat_free").IsChecked = true;
            Item("q_meat_consumed").IsChecked = true;
            Item($"q_{ClientEventTypes.SwapSnackToFruitNuts.ToLowerInvariant()}").IsChecked = true;
            var dup = new QuickMealCheckItem { Id = "dup", EventType = ClientEventTypes.MealLegumeConsumed, IsChecked = true };
            var logged = new QuickMealCheckItem { Id = "logged", EventType = ClientEventTypes.MealLogged, IsChecked = true };

            var (flags, swaps) = MealFacts.Build(_all.Concat(new[] { dup, logged }));

            CollectionAssert.AreEquivalent(new[] { ClientEventTypes.MealLegumeConsumed, ClientEventTypes.MealMeatFree }, flags);
            CollectionAssert.AreEqual(new[] { ClientEventTypes.SwapSnackToFruitNuts }, swaps);
        }

        [Test]
        public void Build_NothingChecked_ReturnsEmptyArrays()
        {
            var (flags, swaps) = MealFacts.Build(_all);

            Assert.IsEmpty(flags);
            Assert.IsEmpty(swaps);
        }

        [Test]
        public void ApplySelections_ChecksMatchingFlagsAndSwaps()
        {
            MealFacts.ApplySelections(_all,
                new[] { ClientEventTypes.MealLocalProduce },
                new[] { ClientEventTypes.SwapBeefToLegumes });

            Assert.IsTrue(Item("q_local").IsChecked);
            Assert.IsTrue(Item($"q_{ClientEventTypes.SwapBeefToLegumes.ToLowerInvariant()}").IsChecked);
            Assert.AreEqual(2, _all.Count(i => i.IsChecked));
        }

        [Test]
        public void FilterByMissionEvents_KeepsOnlyMissionItems_AndSharesInstances()
        {
            var events = MealFacts.MissionMealEvents(new[] { "M.A1.3", "M.B6.5" });

            var filtered = MealFacts.FilterByMissionEvents(_sections, events);

            CollectionAssert.AreEquivalent(
                new[] { ClientEventTypes.MealLegumeConsumed, ClientEventTypes.SwapSnackToFruitNuts },
                filtered.SelectMany(s => s.Items).Select(i => i.EventType).ToArray());
            Assert.AreSame(Item("q_legumes"), filtered.SelectMany(s => s.Items).First(i => i.Id == "q_legumes"));
            Assert.IsTrue(filtered.All(s => s.IsExpanded));
        }

        [Test]
        public void FilterByMissionEvents_NoEvents_ReturnsAllSections()
        {
            Assert.AreSame(_sections, MealFacts.FilterByMissionEvents(_sections, new HashSet<string>()));
        }
    }
}
