using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodCategoryModelsTests
    {
        [Test]
        public void FoodCategory_Roundtrips_Via_JsonUtility()
        {
            var category = new FoodCategory
            {
                id = "cat-1",
                name = "Whole milk",
                foodGroup = "Dairy",
                description = "Full fat milk"
            };
            string json = JsonUtility.ToJson(category);
            var result = JsonUtility.FromJson<FoodCategory>(json);

            Assert.AreEqual("cat-1", result.id);
            Assert.AreEqual("Whole milk", result.name);
            Assert.AreEqual("Dairy", result.foodGroup);
        }

        [Test]
        public void PaginatedFoodCategoryResponse_Deserializes_Data_Array()
        {
            string json = "{\"data\":[{\"id\":\"1\",\"name\":\"Apple\",\"foodGroup\":\"Fruits\",\"description\":\"\"}," +
                          "{\"id\":\"2\",\"name\":\"Chicken\",\"foodGroup\":\"Meat\",\"description\":\"\"}]," +
                          "\"total\":2,\"page\":1,\"pageSize\":20,\"totalPages\":1}";

            var result = JsonUtility.FromJson<PaginatedFoodCategoryResponse>(json);

            Assert.IsNotNull(result.data);
            Assert.AreEqual(2, result.data.Length);
            Assert.AreEqual("Fruits", result.data[0].foodGroup);
            Assert.AreEqual("Chicken", result.data[1].name);
        }
    }
}
