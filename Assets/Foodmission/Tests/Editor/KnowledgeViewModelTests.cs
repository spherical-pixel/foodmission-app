using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class KnowledgeViewModelTests
    {
        private TestStoreService _storeService;
        private KnowledgeViewModel _vm;
        private Func<bool> _originalOverride;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-token",
                tokenType = "Bearer",
                lang = "es"
            });

            _vm = new KnowledgeViewModel(_storeService);
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
            _vm?.Dispose();
        }

        [Test]
        public void InitialState_ShouldContainQuizzesAndFoodFactsSections()
        {
            Assert.IsNotNull(_vm.Sections);
            Assert.AreEqual(2, _vm.Sections.Count);

            var quizzesSection = _vm.Sections.FirstOrDefault(s => s.Id == "quizzes");
            Assert.IsNotNull(quizzesSection);
            Assert.AreEqual(Actions.go_to_quizzes, quizzesSection.NavigationAction);
            Assert.IsTrue(quizzesSection.IsEnabled);
            Assert.AreEqual("NAV_QUIZZES", quizzesSection.TitleKey);
            Assert.AreEqual("quiz", quizzesSection.BannerAddress);

            var foodFactsSection = _vm.Sections.FirstOrDefault(s => s.Id == "food_facts");
            Assert.IsNotNull(foodFactsSection);
            Assert.AreEqual(Actions.go_to_food_facts, foodFactsSection.NavigationAction);
            Assert.IsTrue(foodFactsSection.IsEnabled);
            Assert.AreEqual("NAV_FOOD_FACTS", foodFactsSection.TitleKey);
            Assert.AreEqual("foodfacts", foodFactsSection.BannerAddress);
        }

        [Test]
        public void OpenQuizzes_ShouldTriggerNavigationToGoToQuizzes()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
            };

            _vm.OpenQuizzes();

            Assert.AreEqual(Actions.go_to_quizzes, requestedAction);
        }

        [Test]
        public void OpenFoodFacts_ShouldTriggerNavigationToGoToFoodFacts()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
            };

            _vm.OpenFoodFacts();

            Assert.AreEqual(Actions.go_to_food_facts, requestedAction);
        }

        [Test]
        public void OpenSection_WhenValid_ShouldTriggerNavigation()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
            };

            var section = _vm.Sections.First(s => s.Id == "quizzes");
            _vm.OpenSection(section);

            Assert.AreEqual(Actions.go_to_quizzes, requestedAction);
        }

        [Test]
        public void OpenSection_WhenNullOrDisabled_ShouldNotTriggerNavigation()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
            };

            _vm.OpenSection(null);
            Assert.IsNull(requestedAction);

            var disabledSection = new KnowledgeSectionItem
            {
                Id = "disabled",
                NavigationAction = Actions.go_to_quizzes,
                IsEnabled = false
            };

            _vm.OpenSection(disabledSection);
            Assert.IsNull(requestedAction);

            var emptyActionSection = new KnowledgeSectionItem
            {
                Id = "empty_action",
                NavigationAction = null,
                IsEnabled = true
            };

            _vm.OpenSection(emptyActionSection);
            Assert.IsNull(requestedAction);
        }
    }
}
