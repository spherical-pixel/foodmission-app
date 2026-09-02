using Newtonsoft.Json;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodFactModelsTests
    {
        [Test]
        public void FoodFactLevel_Constants_ShouldMatchExpectedValues()
        {
            Assert.AreEqual("BEGINNER", FoodFactLevel.Beginner);
            Assert.AreEqual("INTERMEDIATE", FoodFactLevel.Intermediate);
            Assert.AreEqual("ADVANCED", FoodFactLevel.Advanced);
            Assert.AreEqual(3, FoodFactLevel.All.Length);
        }

        [Test]
        public void FoodFact_Deserialization_ShouldPopulateAllFields()
        {
            string json = @"{
                ""id"": ""fact-uuid-123"",
                ""code"": ""FF1.1.1"",
                ""topicId"": ""topic-uuid-456"",
                ""body"": ""Eating less red meat reduces environmental impact."",
                ""source"": ""Mazac et al. (2022)"",
                ""level"": ""BEGINNER"",
                ""health"": false,
                ""foodChoice"": true,
                ""foodWaste"": false,
                ""available"": true
            }";

            var fact = JsonConvert.DeserializeObject<FoodFact>(json);

            Assert.IsNotNull(fact);
            Assert.AreEqual("fact-uuid-123", fact.id);
            Assert.AreEqual("FF1.1.1", fact.code);
            Assert.AreEqual("topic-uuid-456", fact.topicId);
            Assert.AreEqual("Eating less red meat reduces environmental impact.", fact.body);
            Assert.AreEqual("Mazac et al. (2022)", fact.source);
            Assert.AreEqual("BEGINNER", fact.level);
            Assert.IsFalse(fact.health);
            Assert.IsTrue(fact.foodChoice);
            Assert.IsFalse(fact.foodWaste);
            Assert.IsTrue(fact.available);
        }

        [Test]
        public void PaginatedFoodFactResponse_Deserialization_ShouldParseDataAndMeta()
        {
            string json = @"{
                ""data"": [
                    { ""id"": ""f1"", ""code"": ""FF1.1.1"", ""body"": ""Fact 1"", ""level"": ""BEGINNER"" },
                    { ""id"": ""f2"", ""code"": ""FF1.1.2"", ""body"": ""Fact 2"", ""level"": ""INTERMEDIATE"" }
                ],
                ""meta"": {
                    ""page"": 1,
                    ""limit"": 10,
                    ""total"": 25,
                    ""totalPages"": 3,
                    ""hasNext"": true,
                    ""hasPrevious"": false
                }
            }";

            var response = JsonConvert.DeserializeObject<PaginatedFoodFactResponse>(json);
            Assert.IsNotNull(response);
            Assert.IsNotNull(response.data);
            Assert.AreEqual(2, response.data.Length);
            Assert.AreEqual("f1", response.data[0].id);
            Assert.AreEqual("f2", response.data[1].id);

            Assert.IsNotNull(response.meta);
            Assert.AreEqual(1, response.meta.page);
            Assert.AreEqual(10, response.meta.limit);
            Assert.AreEqual(25, response.meta.total);
            Assert.AreEqual(3, response.meta.totalPages);
            Assert.IsTrue(response.meta.hasNext);
            Assert.IsFalse(response.meta.hasPrevious);
        }

        [Test]
        public void FoodFactFilterParams_ShouldAssignAndReadProperties()
        {
            var filters = new FoodFactFilterParams
            {
                dimensionCode = "DIET_CHANGES",
                topicCode = "REDUCING_MEAT_CONSUMPTION",
                level = FoodFactLevel.Beginner,
                health = true,
                foodChoice = true,
                foodWaste = false,
                search = "meat"
            };

            Assert.AreEqual("DIET_CHANGES", filters.dimensionCode);
            Assert.AreEqual("REDUCING_MEAT_CONSUMPTION", filters.topicCode);
            Assert.AreEqual("BEGINNER", filters.level);
            Assert.IsTrue(filters.health.Value);
            Assert.IsTrue(filters.foodChoice.Value);
            Assert.IsFalse(filters.foodWaste.Value);
            Assert.AreEqual("meat", filters.search);
        }
    }
}
