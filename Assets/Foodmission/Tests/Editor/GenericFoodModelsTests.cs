using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class GenericFoodModelsTests
    {
        [Test]
        public void GenericFood_Roundtrips_Via_JsonUtility()
        {
            var genericFood = new GenericFood
            {
                id = "cat-1",
                foodName = "Whole milk",
                foodGroup = "Dairy"
            };
            string json = JsonUtility.ToJson(genericFood);
            var result = JsonUtility.FromJson<GenericFood>(json);

            Assert.AreEqual("cat-1", result.id);
            Assert.AreEqual("Whole milk", result.foodName);
            Assert.AreEqual("Dairy", result.foodGroup);
        }

        [Test]
        public void PaginatedGenericFoodResponse_Deserializes_Items_Array()
        {
            string json = "{\"items\":[{\"id\":\"1\",\"foodName\":\"Apple\",\"foodGroup\":\"Fruits\"}," +
                          "{\"id\":\"2\",\"foodName\":\"Chicken\",\"foodGroup\":\"Meat\"}]," +
                          "\"total\":2,\"page\":1,\"limit\":20,\"totalPages\":1}";

            var result = JsonUtility.FromJson<PaginatedGenericFoodResponse>(json);

            Assert.IsNotNull(result.items);
            Assert.AreEqual(2, result.items.Length);
            Assert.AreEqual("Fruits", result.items[0].foodGroup);
            Assert.AreEqual("Chicken", result.items[1].foodName);
        }
    }
}
