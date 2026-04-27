using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ShoppingListModelsTests
    {
        [Test]
        public void ShoppingList_Roundtrips_Via_JsonUtility()
        {
            var list = new ShoppingList { id = "list-1", title = "Weekly Shop", description = "For Monday", userGroupId = "" };
            string json = JsonUtility.ToJson(list);
            var result = JsonUtility.FromJson<ShoppingList>(json);

            Assert.AreEqual("list-1", result.id);
            Assert.AreEqual("Weekly Shop", result.title);
        }

        [Test]
        public void ShoppingListItem_Checked_Field_Serializes_Correctly()
        {
            var item = new ShoppingListItem
            {
                id = "item-1",
                foodId = "food-1",
                quantity = 2.5f,
                unit = "KG",
                @checked = true
            };
            string json = JsonUtility.ToJson(item);

            // "checked" must appear in JSON (not "@checked")
            StringAssert.Contains("\"checked\":true", json);

            var result = JsonUtility.FromJson<ShoppingListItem>(json);
            Assert.IsTrue(result.@checked);
            Assert.AreEqual(2.5f, result.quantity, 0.001f);
            Assert.AreEqual("KG", result.unit);
        }

        [Test]
        public void ShoppingListPagedResponse_Deserializes_Api_Response()
        {
            // API returns {"data":[...]} paged envelope
            string apiJson = "{\"data\":[{\"id\":\"1\",\"title\":\"My List\",\"description\":\"\",\"userGroupId\":\"\"}," +
                             "{\"id\":\"2\",\"title\":\"Weekend\",\"description\":\"\",\"userGroupId\":\"\"}]}";

            var response = JsonUtility.FromJson<ShoppingListPagedResponse>(apiJson);

            Assert.IsNotNull(response.data);
            Assert.AreEqual(2, response.data.Length);
            Assert.AreEqual("My List", response.data[0].title);
            Assert.AreEqual("Weekend", response.data[1].title);
        }

        [Test]
        public void ShoppingListItemPagedResponse_Deserializes_Api_Response()
        {
            // API returns {"data":[...]} paged envelope with embedded food object
            string apiJson = "{\"data\":[{\"id\":\"i1\",\"shoppingListId\":\"l1\",\"foodId\":\"f1\"," +
                             "\"quantity\":1.0,\"unit\":\"PIECES\",\"notes\":\"\",\"checked\":false," +
                             "\"food\":{\"id\":\"f1\",\"name\":\"Leche\",\"barcode\":\"123\"}}]}";

            var response = JsonUtility.FromJson<ShoppingListItemPagedResponse>(apiJson);

            Assert.IsNotNull(response.data);
            Assert.AreEqual(1, response.data.Length);
            Assert.AreEqual("f1", response.data[0].foodId);
            Assert.AreEqual("Leche", response.data[0].food.name);
        }
    }
}
