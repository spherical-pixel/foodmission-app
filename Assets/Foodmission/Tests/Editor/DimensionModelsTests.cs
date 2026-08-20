using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DimensionModelsTests
    {
        [Test]
        public void DimensionCode_Constants_ShouldMatchExpectedValues()
        {
            Assert.AreEqual("DIET_CHANGES", DimensionCode.DietChanges);
            Assert.AreEqual("PRODUCT_CHOICES", DimensionCode.ProductChoices);
            Assert.AreEqual("PRODUCTION_METHODS", DimensionCode.ProductionMethods);
            Assert.AreEqual("PACKAGING", DimensionCode.Packaging);
            Assert.AreEqual("FOOD_WASTE", DimensionCode.FoodWaste);
            Assert.AreEqual("NUTRITION_VALUES", DimensionCode.NutritionValues);
            Assert.AreEqual(6, DimensionCode.All.Length);
        }

        [Test]
        public void TopicCode_Constants_ShouldMatchExpectedValues()
        {
            Assert.AreEqual("REDUCING_MEAT_CONSUMPTION", TopicCode.ReducingMeatConsumption);
            Assert.AreEqual("INCREASING_OTHER_PROTEIN_SOURCES", TopicCode.IncreasingOtherProteinSources);
            Assert.AreEqual("ALTERNATIVE_STAPLE_FOODS", TopicCode.AlternativeStapleFoods);
            Assert.AreEqual("LAND_USE", TopicCode.LandUse);
            Assert.AreEqual("WATER_USE", TopicCode.WaterUse);
            Assert.AreEqual("ENERGY_CONSUMPTION", TopicCode.EnergyConsumption);
            Assert.AreEqual("CARBON_FOOTPRINT", TopicCode.CarbonFootprint);
            Assert.AreEqual("TRAVEL_DISTANCES", TopicCode.TravelDistances);
            Assert.AreEqual("LEVEL_OF_PROCESSING", TopicCode.LevelOfProcessing);
            Assert.AreEqual("COUNTRY_OF_ORIGIN", TopicCode.CountryOfOrigin);
            Assert.AreEqual("FARMING_PRODUCTION_METHODS", TopicCode.FarmingProductionMethods);
            Assert.AreEqual("BREEDING_METHODS", TopicCode.BreedingMethods);
            Assert.AreEqual("FAIR_TRADE_LABOUR", TopicCode.FairTradeLabour);
            Assert.AreEqual("SUSTAINABILITY_OF_PACKAGING_MATERIALS", TopicCode.SustainabilityOfPackagingMaterials);
            Assert.AreEqual("CAPACITY_TO_REUSE_PACKAGING", TopicCode.CapacityToReusePackaging);
            Assert.AreEqual("PLATE_WASTE", TopicCode.PlateWaste);
            Assert.AreEqual("LEFTOVERS_WASTE", TopicCode.LeftoversWaste);
            Assert.AreEqual("EXPIRED_FOOD", TopicCode.ExpiredFood);
            Assert.AreEqual("OVERCONSUMPTION", TopicCode.Overconsumption);
            Assert.AreEqual("PROTEIN", TopicCode.Protein);
            Assert.AreEqual("FAT", TopicCode.Fat);
            Assert.AreEqual("SUGAR", TopicCode.Sugar);
            Assert.AreEqual("SALT", TopicCode.Salt);
            Assert.AreEqual("FIBER", TopicCode.Fiber);
            Assert.AreEqual("VITAMINS", TopicCode.Vitamins);
            Assert.AreEqual("ENERGY_VALUE_CALORIES", TopicCode.EnergyValueCalories);
            Assert.AreEqual(26, TopicCode.All.Length);
        }

        [Test]
        public void Topic_Deserialization_ShouldPopulateAllFields()
        {
            string json = @"{
                ""id"": ""topic-uuid-1"",
                ""code"": ""REDUCING_MEAT_CONSUMPTION"",
                ""name"": ""Reducing meat consumption"",
                ""dimensionId"": ""dim-uuid-1"",
                ""sortOrder"": 1
            }";

            var topic = JsonConvert.DeserializeObject<Topic>(json);

            Assert.IsNotNull(topic);
            Assert.AreEqual("topic-uuid-1", topic.id);
            Assert.AreEqual("REDUCING_MEAT_CONSUMPTION", topic.code);
            Assert.AreEqual("Reducing meat consumption", topic.name);
            Assert.AreEqual("dim-uuid-1", topic.dimensionId);
            Assert.AreEqual(1, topic.sortOrder);
        }

        [Test]
        public void Dimension_Deserialization_ShouldPopulateAllFieldsAndTopics()
        {
            string json = @"{
                ""id"": ""dim-uuid-1"",
                ""code"": ""DIET_CHANGES"",
                ""name"": ""Diet changes towards a more sustainable system"",
                ""sortOrder"": 1,
                ""topics"": [
                    {
                        ""id"": ""topic-1"",
                        ""code"": ""REDUCING_MEAT_CONSUMPTION"",
                        ""name"": ""Reducing meat consumption"",
                        ""dimensionId"": ""dim-uuid-1"",
                        ""sortOrder"": 1
                    },
                    {
                        ""id"": ""topic-2"",
                        ""code"": ""INCREASING_OTHER_PROTEIN_SOURCES"",
                        ""name"": ""Increasing other protein sources"",
                        ""dimensionId"": ""dim-uuid-1"",
                        ""sortOrder"": 2
                    }
                ]
            }";

            var dimension = JsonConvert.DeserializeObject<Dimension>(json);

            Assert.IsNotNull(dimension);
            Assert.AreEqual("dim-uuid-1", dimension.id);
            Assert.AreEqual("DIET_CHANGES", dimension.code);
            Assert.AreEqual("Diet changes towards a more sustainable system", dimension.name);
            Assert.AreEqual(1, dimension.sortOrder);
            Assert.IsNotNull(dimension.topics);
            Assert.AreEqual(2, dimension.topics.Length);
            Assert.AreEqual("topic-1", dimension.topics[0].id);
            Assert.AreEqual("REDUCING_MEAT_CONSUMPTION", dimension.topics[0].code);
            Assert.AreEqual("topic-2", dimension.topics[1].id);
            Assert.AreEqual("INCREASING_OTHER_PROTEIN_SOURCES", dimension.topics[1].code);
        }

        [Test]
        public void DimensionArray_Deserialization_ShouldHandleMultipleDimensions()
        {
            string json = @"[
                {
                    ""id"": ""dim-1"",
                    ""code"": ""FOOD_WASTE"",
                    ""name"": ""Food waste"",
                    ""sortOrder"": 5,
                    ""topics"": [
                        { ""id"": ""top-1"", ""code"": ""PLATE_WASTE"", ""name"": ""Plate waste"", ""dimensionId"": ""dim-1"", ""sortOrder"": 1 }
                    ]
                },
                {
                    ""id"": ""dim-2"",
                    ""code"": ""PACKAGING"",
                    ""name"": ""Packaging"",
                    ""sortOrder"": 4,
                    ""topics"": []
                }
            ]";

            var dimensions = JsonConvert.DeserializeObject<Dimension[]>(json);

            Assert.IsNotNull(dimensions);
            Assert.AreEqual(2, dimensions.Length);
            Assert.AreEqual("FOOD_WASTE", dimensions[0].code);
            Assert.AreEqual(1, dimensions[0].topics.Length);
            Assert.AreEqual("PACKAGING", dimensions[1].code);
            Assert.AreEqual(0, dimensions[1].topics.Length);
        }
    }
}
