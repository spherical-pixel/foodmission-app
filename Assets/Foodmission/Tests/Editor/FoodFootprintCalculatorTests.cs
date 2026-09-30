using NUnit.Framework;
using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodFootprintCalculatorTests
    {
        private IFoodFootprintCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new FoodFootprintCalculator();
        }

        [Test]
        public void CalculateProductFootprint_WhenProductHasExplicitCarbonFootprint_ReturnsExactConfidence()
        {
            var product = new FoodProduct
            {
                id = "prod-1",
                name = "Yogurt Bio",
                carbonFootprint = 2.4f,
                categories = new[] { "en:meats", "en:beef" }
            };

            var metric = _calculator.CalculateProductFootprint(product);

            Assert.AreEqual(2.4f, metric.CarbonFootprintKg, 0.001f);
            Assert.AreEqual(FootprintConfidence.Exact, metric.Confidence);
            Assert.IsTrue(metric.CompositeImpactScore > 0f);
        }

        [Test]
        public void CalculateProductFootprint_OpenFoodFactsCategoryTags_ClassifiesCorrectly()
        {
            var product = new FoodProduct
            {
                id = "off-1",
                name = "Organic Chickpeas",
                categories = new[] { "en:plant-based-foods-and-beverages", "en:plant-based-foods", "en:legumes", "en:pulses", "en:chickpeas" }
            };

            var metric = _calculator.CalculateProductFootprint(product);

            Assert.AreEqual(FoodFootprintCalculator.LegumesFootprintKg, metric.CarbonFootprintKg);
            Assert.AreEqual(FoodFootprintCalculator.LegumesCompositeScore, metric.CompositeImpactScore);
            Assert.AreEqual(FootprintConfidence.Estimated, metric.Confidence);
            Assert.IsTrue(_calculator.IsProtein(product));
        }

        [Test]
        public void CalculateProductFootprint_IgnoresNameAndDescription()
        {
            var veggieBurger = new FoodProduct
            {
                name = "Hamburguesa de ternera",
                description = "Burger de vacuno",
                categories = new[] { "en:meat-alternatives", "en:meat-analogues", "en:vegetarian-hamburgers" }
            };
            var uncategorized = new FoodProduct { name = "Filete de ternera" };

            Assert.AreEqual(FoodFootprintCalculator.TofuPlantAltFootprintKg, _calculator.CalculateProductFootprint(veggieBurger).CarbonFootprintKg);
            Assert.AreEqual(FoodFootprintCalculator.DefaultFootprintKg, _calculator.CalculateProductFootprint(uncategorized).CarbonFootprintKg);
            Assert.IsFalse(_calculator.IsProtein(uncategorized));
        }

        [Test]
        public void CalculateProductFootprint_OffSpeciesTags_ResolveSpecies()
        {
            var chickenBurger = new FoodProduct { categories = new[] { "en:meats-and-their-products", "en:chicken-and-its-products", "en:chicken-patties" } };
            var ham = new FoodProduct { categories = new[] { "en:meats-and-their-products", "en:prepared-meats", "en:hams" } };
            var hake = new FoodProduct { categories = new[] { "en:seafood", "en:fishes-and-their-products", "en:fishes", "en:hakes" } };
            var beef = new FoodProduct { categories = new[] { "en:meats", "en:beef-and-its-products", "en:beef" } };

            Assert.AreEqual(FoodFootprintCalculator.PoultryFootprintKg, _calculator.CalculateProductFootprint(chickenBurger).CarbonFootprintKg);
            Assert.AreEqual(FoodFootprintCalculator.PoultryFootprintKg, _calculator.CalculateProductFootprint(ham).CarbonFootprintKg);
            Assert.AreEqual(FoodFootprintCalculator.FishFootprintKg, _calculator.CalculateProductFootprint(hake).CarbonFootprintKg);
            Assert.AreEqual(FoodFootprintCalculator.BeefFootprintKg, _calculator.CalculateProductFootprint(beef).CarbonFootprintKg);
        }

        [Test]
        public void CalculateProductFootprint_WhenCategoriesMissing_UsesOpenFoodFactsInfoCategories()
        {
            var product = new FoodProduct
            {
                name = "Pollo 1954",
                categories = new string[0],
                openFoodFactsInfo = new OpenFoodFactsInfoDto { categories = new[] { "en:meats", "en:chicken-and-its-products", "en:poultries" } }
            };

            Assert.AreEqual(FoodFootprintCalculator.PoultryFootprintKg, _calculator.CalculateProductFootprint(product).CarbonFootprintKg);
            Assert.IsTrue(_calculator.IsProtein(product));
        }

        [Test]
        public void ClassifyProduct_UsesOffTaxonomyNotName()
        {
            var veggieBurger = new FoodProduct { name = "Hamburguesa de ternera", categories = new[] { "en:meat-alternatives", "en:vegetarian-hamburgers" } };
            var chicken = new FoodProduct { name = "Filete", categories = new[] { "en:meats", "en:poultries", "en:chickens" } };
            var mince = new FoodProduct { categories = new[] { "en:meats", "en:beef", "en:pork" } };
            var uncategorized = new FoodProduct { name = "Pollo asado" };

            CollectionAssert.AreEqual(new[] { ProteinClass.PlantAlternative }, FoodFootprintCalculator.ClassifyProduct(veggieBurger));
            CollectionAssert.AreEqual(new[] { ProteinClass.Poultry }, FoodFootprintCalculator.ClassifyProduct(chicken));
            CollectionAssert.AreEqual(new[] { ProteinClass.Beef, ProteinClass.Pork }, FoodFootprintCalculator.ClassifyProduct(mince));
            CollectionAssert.IsEmpty(FoodFootprintCalculator.ClassifyProduct(uncategorized));
        }

        [Test]
        public void CalculateProductFootprint_MixedSpecies_AveragesBenchmarks()
        {
            var mince = new FoodProduct { categories = new[] { "en:meats", "en:beef", "en:pork" } };

            var metric = _calculator.CalculateProductFootprint(mince);

            float expected = (FoodFootprintCalculator.BeefFootprintKg + FoodFootprintCalculator.PorkFootprintKg) / 2f;
            Assert.AreEqual(expected, metric.CarbonFootprintKg, 0.001f);
        }

        [Test]
        public void CalculateProductFootprint_GrillingCheese_IsCheeseNotMeatAlternative()
        {
            var halloumi = new FoodProduct { categories = new[] { "en:dairies", "en:cheeses", "en:meat-alternatives", "en:grilling-cheeses" } };

            Assert.AreEqual(FoodFootprintCalculator.CheeseFootprintKg, _calculator.CalculateProductFootprint(halloumi).CarbonFootprintKg);
        }

        [Test]
        public void IsProtein_Product_RejectsGenericPlantBasedFoods()
        {
            var apples = new FoodProduct { categories = new[] { "en:plant-based-foods-and-beverages", "en:plant-based-foods", "en:fruits-and-vegetables-based-foods", "en:fruits", "en:apples" } };
            var bread = new FoodProduct { categories = new[] { "en:plant-based-foods-and-beverages", "en:plant-based-foods", "en:cereals-and-potatoes", "en:breads" } };
            var soyDrink = new FoodProduct { categories = new[] { "en:plant-based-foods", "en:legumes-and-their-products", "en:dairy-substitutes", "en:milk-substitutes", "en:soy-based-drinks" } };
            var greenBeans = new FoodProduct { categories = new[] { "en:plant-based-foods", "en:legumes-and-their-products", "en:legumes", "en:green-beans" } };
            var seaweed = new FoodProduct { categories = new[] { "en:seafood", "en:seaweeds-and-their-products" } };
            var tofu = new FoodProduct { categories = new[] { "en:plant-based-foods", "en:meat-alternatives", "en:tofu" } };

            Assert.IsFalse(_calculator.IsProtein(apples));
            Assert.IsFalse(_calculator.IsProtein(bread));
            Assert.IsFalse(_calculator.IsProtein(soyDrink));
            Assert.IsFalse(_calculator.IsProtein(greenBeans));
            Assert.IsFalse(_calculator.IsProtein(seaweed));
            Assert.IsTrue(_calculator.IsProtein(tofu));
        }

        [Test]
        public void CalculateGenericFoodFootprint_MeatWithLangualSpecies_UsesSpeciesBenchmark()
        {
            var beef = new GenericFood
            {
                id = "gen-beef",
                foodName = "Pollo",
                foodGroupSlug = "meat-and-poultry",
                langualCodes = new[] { "A0714", "B1201", "C0125" }
            };
            var chicken = new GenericFood
            {
                id = "gen-chicken",
                foodName = "Ternera",
                foodGroupSlug = "meat-and-poultry",
                langualCodes = new[] { "A0715", "B1457", "C0267" }
            };

            var beefMetric = _calculator.CalculateGenericFoodFootprint(beef);

            Assert.AreEqual(FoodFootprintCalculator.BeefFootprintKg, beefMetric.CarbonFootprintKg);
            Assert.AreEqual(FootprintConfidence.Estimated, beefMetric.Confidence);
            Assert.IsTrue(beefMetric.ScientificReference.Contains("Clark et al. (PNAS 2022)"));
            Assert.AreEqual(FoodFootprintCalculator.PoultryFootprintKg, _calculator.CalculateGenericFoodFootprint(chicken).CarbonFootprintKg);
        }

        [Test]
        public void CalculateGenericFoodFootprint_MeatWithoutSpeciesCode_UsesUnspecifiedMeatBenchmark()
        {
            var meat = new GenericFood { foodName = "Beef minced raw", foodGroupSlug = "meat-and-poultry" };

            Assert.AreEqual(FoodFootprintCalculator.PoultryFootprintKg, _calculator.CalculateGenericFoodFootprint(meat).CarbonFootprintKg);
        }

        [Test]
        public void CalculateGenericFoodFootprint_Legumes_ReturnsLowestImpactEstimates()
        {
            var lentils = new GenericFood
            {
                id = "gen-lentils",
                foodName = "Lentejas cocidas",
                foodGroup = "Legumbres",
                foodGroupSlug = "legumes",
                legume = true
            };

            var metric = _calculator.CalculateGenericFoodFootprint(lentils);

            Assert.LessOrEqual(metric.CarbonFootprintKg, 2.0f);
            Assert.Less(metric.CompositeImpactScore, 2.5f);
        }

        [Test]
        public void CalculateGenericFoodFootprint_EnglishNevoSlug_CorrectlyCategorizes()
        {
            var englishFish = new GenericFood
            {
                id = "gen-fish-en",
                foodName = "Cod fillet, steamed",
                foodGroup = "Fish, crustacean and shellfish",
                foodGroupSlug = "fish-crustacean-and-shellfish",
                meatOrFish = true
            };

            var metric = _calculator.CalculateGenericFoodFootprint(englishFish);

            Assert.AreEqual(FoodFootprintCalculator.FishFootprintKg, metric.CarbonFootprintKg);
            Assert.AreEqual(FoodFootprintCalculator.FishCompositeScore, metric.CompositeImpactScore);
            Assert.IsTrue(_calculator.IsProtein(englishFish));
        }

        [Test]
        public void IsProtein_GenericFood_UsesFoodGroupNotNameOrContainsFlags()
        {
            var coldCuts = new GenericFood { foodName = "Jamón serrano", foodGroupSlug = "cold-meat-cuts" };
            var cheese = new GenericFood { foodName = "Queso curado", foodGroupSlug = "cheese" };
            var eggs = new GenericFood { foodName = "Huevos frescos", foodGroupSlug = "eggs" };
            var legumes = new GenericFood { foodName = "Lentejas secas", foodGroupSlug = "legumes" };
            var soup = new GenericFood { foodName = "Chicken soup", foodGroupSlug = "soups", meatOrFish = true };
            var sauce = new GenericFood { foodName = "Hummus", foodGroupSlug = "savoury-sauces", legume = true };
            var bread = new GenericFood { foodName = "Bread wholemeal", foodGroupSlug = "bread", proteins = 9.8f };
            var noGroup = new GenericFood { foodName = "Pechuga de pollo" };

            Assert.IsTrue(_calculator.IsProtein(coldCuts));
            Assert.IsTrue(_calculator.IsProtein(cheese));
            Assert.IsTrue(_calculator.IsProtein(eggs));
            Assert.IsTrue(_calculator.IsProtein(legumes));
            Assert.IsFalse(_calculator.IsProtein(soup));
            Assert.IsFalse(_calculator.IsProtein(sauce));
            Assert.IsFalse(_calculator.IsProtein(bread));
            Assert.IsFalse(_calculator.IsProtein(noGroup));
        }

        [Test]
        public void IsProtein_GenericMeatSubstitutes_RequiresProteinThreshold()
        {
            var tofu = new GenericFood { foodName = "Tofu unprepared", foodGroupSlug = "meat-substitutes-and-dairy-substitutes", proteins = 12.4f };
            var oatDrink = new GenericFood { foodName = "Drink oat wo sugar", foodGroupSlug = "meat-substitutes-and-dairy-substitutes", proteins = 0.3f };
            var unknown = new GenericFood { foodName = "Tempeh", foodGroupSlug = "meat-substitutes-and-dairy-substitutes" };

            Assert.IsTrue(_calculator.IsProtein(tofu));
            Assert.IsFalse(_calculator.IsProtein(oatDrink));
            Assert.IsFalse(_calculator.IsProtein(unknown));
        }
    }
}
