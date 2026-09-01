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

        [Test]
        public void GenericFood_DietFlags_Roundtrips_Via_JsonUtility()
        {
            var genericFood = new GenericFood
            {
                id = "lentils-1",
                foodName = "Brown Lentils",
                foodGroup = "Legumes",
                foodGroupSlug = "legumes",
                vegan = true,
                vegetarian = true,
                meatOrFish = false,
                legume = true
            };
            string json = JsonUtility.ToJson(genericFood);
            var result = JsonUtility.FromJson<GenericFood>(json);

            Assert.IsTrue(result.vegan);
            Assert.IsTrue(result.vegetarian);
            Assert.IsFalse(result.meatOrFish);
            Assert.IsTrue(result.legume);
        }

        [Test]
        public void GenericFoodDetail_DietFlags_Deserializes_Correctly()
        {
            string json = "{\"id\":\"salmon-1\",\"foodName\":\"Atlantic Salmon\",\"foodGroup\":\"Fish\",\"foodGroupSlug\":\"fish\"," +
                          "\"vegan\":false,\"vegetarian\":false,\"meatOrFish\":true,\"legume\":false,\"proteins\":20.4}";

            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<GenericFoodDetail>(json);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.vegan);
            Assert.IsFalse(result.vegetarian);
            Assert.IsTrue(result.meatOrFish);
            Assert.IsFalse(result.legume);
            Assert.AreEqual(20.4f, result.proteins);
        }
    }
}
