using System.Linq;

using NUnit.Framework;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProgressWheelInfoCatalogTests
    {
        private static WheelScenario Scenario(string code)
        {
            return ProgressWheelInfoCatalog.Scenarios.First(s => s.Code == code);
        }

        private static float Value(string code, string kind)
        {
            Assert.IsTrue(Scenario(code).TryGetValue(kind, out float value), $"{code} has no {kind} value");
            return value;
        }

        [Test]
        public void Scenarios_AreTheTwentyFromTheDoc_InOrder()
        {
            string[] expected =
            {
                "VEGAN_DAY", "VEGETARIAN_DAY", "VEGAN_MEAL", "VEGETARIAN_MEAL",
                "BEEF_TO_PORK_100G", "BEEF_TO_CHICKEN_100G", "BEEF_TO_LEGUMES_100G",
                "PORK_TO_CHICKEN_100G", "PORK_TO_LEGUMES_100G", "CHICKEN_TO_LEGUMES_100G",
                "HALF_PLATE_SAVED", "FULL_PLATE_SAVED", "EXPIRED_PRODUCT_CONSUMED",
                "SNACK_TO_FRUIT_OR_NUTS_30G", "SUGARY_CEREAL_TO_OATS_FRUIT_30G", "SUGARY_DRINK_TO_WATER_250ML",
                "READY_MEAL_TO_HOME_COOKED_400G", "PROCESSED_MEAT_TO_LEGUMES_50G",
                "REUSABLE_CONTAINER_USE", "REFILL_PRODUCT",
            };
            CollectionAssert.AreEqual(expected, ProgressWheelInfoCatalog.Scenarios.Select(s => s.Code).ToArray());
        }

        [Test]
        public void EveryScenario_HasAValueForEveryKind()
        {
            foreach (WheelScenario scenario in ProgressWheelInfoCatalog.Scenarios)
            {
                foreach (string kind in ProgressWheelText.KnownKinds)
                {
                    Assert.IsTrue(scenario.TryGetValue(kind, out float value), $"{scenario.Code} has no {kind} value");
                    Assert.Greater(value, 0f, $"{scenario.Code} {kind} must be positive");
                }
            }
        }

        [Test]
        public void SpotValues_MatchTheDoc()
        {
            Assert.AreEqual(4.5f, Value("VEGAN_DAY", "CO2_REDUCTION"), 1e-4f);
            Assert.AreEqual(44f, Value("BEEF_TO_CHICKEN_100G", "WATER_SAVINGS"), 1e-4f);
            Assert.AreEqual(0.01f, Value("SUGARY_DRINK_TO_WATER_250ML", "LAND_USE_REDUCTION"), 1e-4f);
            Assert.AreEqual(0.5f, Value("REFILL_PRODUCT", "ENERGY_REDUCTION"), 1e-4f);

            Assert.AreEqual(250f, Scenario("SUGARY_DRINK_TO_WATER_250ML").Amount);
            Assert.AreEqual(WheelAmountUnit.Millilitres, Scenario("SUGARY_DRINK_TO_WATER_250ML").Unit);
            Assert.AreEqual(1f, Scenario("REUSABLE_CONTAINER_USE").Amount);
            Assert.AreEqual(WheelAmountUnit.Use, Scenario("REUSABLE_CONTAINER_USE").Unit);
            Assert.AreEqual(1f, Scenario("REFILL_PRODUCT").Amount);
            Assert.AreEqual(WheelAmountUnit.Refill, Scenario("REFILL_PRODUCT").Unit);
            Assert.AreEqual(50f, Scenario("HALF_PLATE_SAVED").Amount);
            Assert.AreEqual(WheelAmountUnit.Grams, Scenario("HALF_PLATE_SAVED").Unit);
        }

        [Test]
        public void UnknownKind_HasNoValue()
        {
            Assert.IsFalse(Scenario("VEGAN_MEAL").TryGetValue("X", out _));
            Assert.IsFalse(Scenario("VEGAN_MEAL").TryGetValue(null, out _));
        }

        [Test]
        public void ParagraphCount_PerKind()
        {
            Assert.AreEqual(3, ProgressWheelInfoCatalog.ParagraphCount("CO2_REDUCTION"));
            Assert.AreEqual(3, ProgressWheelInfoCatalog.ParagraphCount("ENERGY_REDUCTION"));
            Assert.AreEqual(2, ProgressWheelInfoCatalog.ParagraphCount("WATER_SAVINGS"));
            Assert.AreEqual(2, ProgressWheelInfoCatalog.ParagraphCount("LAND_USE_REDUCTION"));
            Assert.AreEqual(0, ProgressWheelInfoCatalog.ParagraphCount("X"));
        }

        [Test]
        public void Keys_PerKind_AndUnknownIsNull()
        {
            Assert.AreEqual("WHEEL_INFO_WATER_SAVINGS_P2", ProgressWheelInfoCatalog.ParagraphKey("WATER_SAVINGS", 2));
            Assert.AreEqual("WHEEL_INFO_CO2_REDUCTION_TITLE", ProgressWheelInfoCatalog.TitleKey("CO2_REDUCTION"));
            Assert.AreEqual("WHEEL_INFO_LAND_USE_REDUCTION_SUBTITLE", ProgressWheelInfoCatalog.SubtitleKey("LAND_USE_REDUCTION"));
            Assert.AreEqual("WHEEL_INFO_ENERGY_REDUCTION_VALUE_HEADER", ProgressWheelInfoCatalog.ValueHeaderKey("ENERGY_REDUCTION"));
            Assert.AreEqual("WHEEL_INFO_SCENARIO_REFILL_PRODUCT", ProgressWheelInfoCatalog.ScenarioKey("REFILL_PRODUCT"));
            Assert.AreEqual("WHEEL_INFO_AMOUNT_ML", ProgressWheelInfoCatalog.AmountKey(WheelAmountUnit.Millilitres));
            Assert.IsNull(ProgressWheelInfoCatalog.TitleKey("X"));
            Assert.IsNull(ProgressWheelInfoCatalog.ParagraphKey("WATER_SAVINGS", 3));
            Assert.IsNull(ProgressWheelInfoCatalog.ParagraphKey("WATER_SAVINGS", 0));
        }

        [Test]
        public void Sources_FourteenNonEmpty_WithUrl()
        {
            Assert.AreEqual(14, ProgressWheelInfoCatalog.Sources.Count);
            foreach (string source in ProgressWheelInfoCatalog.Sources)
            {
                StringAssert.Contains("http", source);
                StringAssert.DoesNotContain("from 2026http", source);
            }
        }

        [Test]
        public void AllKeys_Distinct_AndCount()
        {
            var keys = ProgressWheelInfoCatalog.AllKeys().ToList();
            Assert.AreEqual(keys.Count, keys.Distinct().Count());
            Assert.AreEqual(54, keys.Count);
            Assert.IsFalse(keys.Any(string.IsNullOrEmpty));
        }
    }
}
