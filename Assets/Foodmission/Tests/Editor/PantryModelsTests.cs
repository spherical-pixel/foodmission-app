using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class PantryModelsTests
    {
        [Test]
        public void PantryItem_With_FoodId_Roundtrips_Via_JsonUtility()
        {
            var item = new PantryItem
            {
                id = "pi-1",
                pantryId = "p-1",
                foodId = "food-1",
                foodCategoryId = "",
                quantity = 2f,
                unit = "KG",
                notes = "organic",
                location = "Fridge",
                expiryDate = "2026-05-30"
            };
            string json = JsonUtility.ToJson(item);
            var result = JsonUtility.FromJson<PantryItem>(json);

            Assert.AreEqual("pi-1", result.id);
            Assert.AreEqual("food-1", result.foodId);
            Assert.AreEqual("", result.foodCategoryId);
            Assert.AreEqual("Fridge", result.location);
            Assert.AreEqual("2026-05-30", result.expiryDate);
        }

        [Test]
        public void PantryItem_With_CategoryId_Roundtrips_Via_JsonUtility()
        {
            var item = new PantryItem
            {
                id = "pi-2",
                pantryId = "p-1",
                foodId = "",
                foodCategoryId = "cat-1",
                quantity = 500f,
                unit = "G"
            };
            string json = JsonUtility.ToJson(item);
            var result = JsonUtility.FromJson<PantryItem>(json);

            Assert.AreEqual("", result.foodId);
            Assert.AreEqual("cat-1", result.foodCategoryId);
            Assert.AreEqual(500f, result.quantity, 0.001f);
        }

        [Test]
        public void Pantry_With_Embedded_Items_Deserializes()
        {
            string json = "{\"id\":\"p-1\",\"userId\":\"u-1\",\"items\":[" +
                          "{\"id\":\"pi-1\",\"pantryId\":\"p-1\",\"foodId\":\"f-1\",\"foodCategoryId\":\"\"," +
                          "\"quantity\":1.0,\"unit\":\"PIECES\",\"notes\":\"\",\"location\":\"\",\"expiryDate\":\"\"}]}";

            var pantry = JsonUtility.FromJson<Pantry>(json);

            Assert.AreEqual("p-1", pantry.id);
            Assert.IsNotNull(pantry.items);
            Assert.AreEqual(1, pantry.items.Length);
            Assert.AreEqual("f-1", pantry.items[0].foodId);
        }

        [Test]
        public void PantryItemArrayWrapper_Deserializes_Api_Array_Response()
        {
            string apiJson = "[{\"id\":\"pi-1\",\"pantryId\":\"p-1\",\"foodId\":\"f-1\",\"foodCategoryId\":\"\"," +
                             "\"quantity\":2.0,\"unit\":\"KG\",\"notes\":\"\",\"location\":\"\",\"expiryDate\":\"\"}]";
            string wrapped = "{\"items\":" + apiJson + "}";

            var wrapper = JsonUtility.FromJson<PantryItemArrayWrapper>(wrapped);

            Assert.IsNotNull(wrapper.items);
            Assert.AreEqual(1, wrapper.items.Length);
            Assert.AreEqual("f-1", wrapper.items[0].foodId);
        }
    }
}
