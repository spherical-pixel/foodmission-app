using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodModelsTests
    {
        [Test]
        public void FoodItem_Roundtrips_Via_JsonUtility()
        {
            var food = new FoodItem { id = "abc-123", name = "Apple", barcode = "012345", description = "A fruit" };
            string json = JsonUtility.ToJson(food);
            var result = JsonUtility.FromJson<FoodItem>(json);

            Assert.AreEqual("abc-123", result.id);
            Assert.AreEqual("Apple", result.name);
            Assert.AreEqual("012345", result.barcode);
            Assert.AreEqual("A fruit", result.description);
        }

        [Test]
        public void PaginatedFoodResponse_Deserializes_Data_Array()
        {
            string json = "{\"data\":[{\"id\":\"1\",\"name\":\"Apple\",\"barcode\":\"\",\"description\":\"\"}," +
                          "{\"id\":\"2\",\"name\":\"Banana\",\"barcode\":\"\",\"description\":\"\"}]," +
                          "\"total\":2,\"page\":1,\"pageSize\":20,\"totalPages\":1}";

            var result = JsonUtility.FromJson<PaginatedFoodResponse>(json);

            Assert.IsNotNull(result.data);
            Assert.AreEqual(2, result.data.Length);
            Assert.AreEqual("Apple", result.data[0].name);
            Assert.AreEqual("Banana", result.data[1].name);
            Assert.AreEqual(2, result.total);
        }

        [Test]
        public void NutritionalInfo_Roundtrips_Via_JsonUtility()
        {
            var info = new NutritionalInfo { energyKcal = 42f, proteins = 0.3f, carbohydrates = 4f };
            string json = JsonUtility.ToJson(info);
            var result = JsonUtility.FromJson<NutritionalInfo>(json);

            Assert.AreEqual(42f, result.energyKcal, 0.01f);
            Assert.AreEqual(0.3f, result.proteins, 0.001f);
        }
    }
}
