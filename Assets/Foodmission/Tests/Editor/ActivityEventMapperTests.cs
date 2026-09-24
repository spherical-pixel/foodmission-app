using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ActivityEventMapperTests
    {
        private IActivityEventMapper _mapper;

        [SetUp]
        public void SetUp()
        {
            _mapper = new ActivityEventMapper();
        }

        [Test]
        public void GetMissionMapping_MeatCountMission_ReturnsMeatEventsAndPrompt()
        {
            // M.B1.1: Learn to Log Your Food, M.A1.1: Stay in the Green Zone
            var mapping = _mapper.GetMissionMapping("M.A1.1");

            Assert.IsNotNull(mapping);
            Assert.AreEqual("M.A1.1", mapping.ActivityCode);
            Assert.Contains(ClientEventTypes.MealMeatFree, mapping.TargetEventTypes);
            Assert.IsNotEmpty(mapping.DirectQuestionPrompt);
            Assert.AreEqual("go_to_meallog", mapping.NativeModuleAction);
        }

        [Test]
        public void GetMissionMapping_SwapMission_ReturnsSwapSelectorAndSwapEvents()
        {
            // M.B1.4: First Sustainable Swap
            var mapping = _mapper.GetMissionMapping("M.B1.4");

            Assert.IsNotNull(mapping);
            Assert.AreEqual("M.B1.4", mapping.ActivityCode);
            Assert.AreEqual(DirectQuestionType.SwapSelector, mapping.QuestionType);
            Assert.IsNotNull(mapping.SwapOptions);
            Assert.Greater(mapping.SwapOptions.Length, 0);
            Assert.Contains(ClientEventTypes.SwapBeefToLegumes, mapping.TargetEventTypes);
        }

        [Test]
        public void GetMissionMapping_FoodWasteMission_ReturnsFoodWasteEventsAndAction()
        {
            // M.B5.1: Smart Serving Starter (Half plate saved)
            var mapping = _mapper.GetMissionMapping("M.B5.1");

            Assert.IsNotNull(mapping);
            Assert.Contains(ClientEventTypes.FoodWasteHalfPlateSaved, mapping.TargetEventTypes);
            Assert.AreEqual("go_to_foodwaste", mapping.NativeModuleAction);
        }

        [Test]
        public void GetMissionMapping_OriginMission_ReturnsShoppingEvents()
        {
            // M.B2.1: Origin Tracker
            var mapping = _mapper.GetMissionMapping("M.B2.1");

            Assert.IsNotNull(mapping);
            Assert.Contains(ClientEventTypes.ShoppingOriginChecked, mapping.TargetEventTypes);
        }

        [Test]
        public void GetChallengeMapping_FootprintChallenge_ReturnsLearningEvents()
        {
            // CH.B1.1: Which Protein Has the Lowest Footprint?
            var mapping = _mapper.GetChallengeMapping("CH.B1.1");

            Assert.IsNotNull(mapping);
            Assert.Contains(ClientEventTypes.LearningFootprintCompared, mapping.TargetEventTypes);
            Assert.AreEqual("go_to_shopping_list", mapping.NativeModuleAction);
        }

        [Test]
        public void GetChallengeMapping_FindNonMeatProteins_ReturnsProteinVarietyEventsAndCountStepper()
        {
            // CH.B1.2: Find Three Non-Meat Proteins in fridge/pantry
            var mapping = _mapper.GetChallengeMapping("CH.B1.2");

            Assert.IsNotNull(mapping);
            Assert.AreEqual(ClientEventTypes.NutritionProteinVarietyLogged, mapping.TargetEventTypes[0]);
            Assert.AreEqual(DirectQuestionType.CountStepper, mapping.QuestionType);
            Assert.AreEqual(3, mapping.DefaultCount);
            Assert.AreEqual("go_to_pantry", mapping.NativeModuleAction);
        }

        [Test]
        public void GetMissionMapping_UnknownCode_ReturnsSensibleDefault()
        {
            var mapping = _mapper.GetMissionMapping("UNKNOWN.CODE");

            Assert.IsNotNull(mapping);
            Assert.IsNotNull(mapping.TargetEventTypes);
            Assert.Greater(mapping.TargetEventTypes.Length, 0);
        }

        [Test]
        public void GetSwapDisplayName_ReturnsHumanReadableLabel()
        {
            string label = ActivityEventMapper.GetSwapDisplayName(ClientEventTypes.SwapBeefToLegumes);
            Assert.IsNotEmpty(label);
            Assert.IsTrue(label.Contains("legumbres") || label.Contains("legumes", System.StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void GetSwapLocalizationKey_ReturnsExpectedKeys()
        {
            Assert.AreEqual("SWAP_BEEF_TO_LEGUMES", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapBeefToLegumes));
            Assert.AreEqual("SWAP_BEEF_TO_CHICKEN", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapBeefToChicken));
            Assert.AreEqual("SWAP_BEEF_TO_PORK", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapBeefToPork));
            Assert.AreEqual("SWAP_PORK_TO_LEGUMES", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapPorkToLegumes));
            Assert.AreEqual("SWAP_PORK_TO_CHICKEN", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapPorkToChicken));
            Assert.AreEqual("SWAP_CHICKEN_TO_LEGUMES", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapChickenToLegumes));
            Assert.AreEqual("SWAP_SUGARY_DRINK_TO_WATER", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapSugaryDrinkToWater));
            Assert.AreEqual("SWAP_SNACK_TO_FRUIT_NUTS", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapSnackToFruitNuts));
            Assert.AreEqual("SWAP_SUGARY_CEREAL_TO_OATS", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapSugaryCerealToOats));
            Assert.AreEqual("SWAP_READY_MEAL_TO_HOMECOOKED", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapReadyMealToHomecooked));
            Assert.AreEqual("SWAP_PROCESSED_MEAT_TO_LEGUMES", ActivityEventMapper.GetSwapLocalizationKey(ClientEventTypes.SwapProcessedMeatToLegumes));
            Assert.IsNull(ActivityEventMapper.GetSwapLocalizationKey("UNKNOWN_EVENT"));
        }

        [Test]
        public void GetSwapLocalizationTag_ReturnsExpectedTags()
        {
            Assert.AreEqual("@UI:SWAP_BEEF_TO_LEGUMES", ActivityEventMapper.GetSwapLocalizationTag(ClientEventTypes.SwapBeefToLegumes));
            Assert.AreEqual("@UI:SWAP_PROCESSED_MEAT_TO_LEGUMES", ActivityEventMapper.GetSwapLocalizationTag(ClientEventTypes.SwapProcessedMeatToLegumes));
            Assert.IsNull(ActivityEventMapper.GetSwapLocalizationTag("UNKNOWN_EVENT"));
        }

        [Test]
        public void GetSwapDisplayName_AllSwapsReturnNonEmptyText()
        {
            var allSwaps = new[]
            {
                ClientEventTypes.SwapBeefToLegumes,
                ClientEventTypes.SwapBeefToChicken,
                ClientEventTypes.SwapBeefToPork,
                ClientEventTypes.SwapPorkToLegumes,
                ClientEventTypes.SwapPorkToChicken,
                ClientEventTypes.SwapChickenToLegumes,
                ClientEventTypes.SwapSugaryDrinkToWater,
                ClientEventTypes.SwapSnackToFruitNuts,
                ClientEventTypes.SwapSugaryCerealToOats,
                ClientEventTypes.SwapReadyMealToHomecooked,
                ClientEventTypes.SwapProcessedMeatToLegumes
            };

            foreach (var swap in allSwaps)
            {
                string text = ActivityEventMapper.GetSwapDisplayName(swap);
                Assert.IsNotEmpty(text);
                Assert.AreNotEqual(swap, text);
            }
        }
    }
}
