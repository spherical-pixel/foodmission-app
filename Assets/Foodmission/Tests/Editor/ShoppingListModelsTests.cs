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
            var list = new ShoppingList { id = "list-1", name = "Weekly Shop", description = "For Monday", userGroupId = "" };
            string json = JsonUtility.ToJson(list);
            var result = JsonUtility.FromJson<ShoppingList>(json);

            Assert.AreEqual("list-1", result.id);
            Assert.AreEqual("Weekly Shop", result.name);
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
        public void ShoppingListArrayWrapper_Deserializes_Api_Array_Response()
        {
            // API returns a raw JSON array — wrap it before passing to JsonUtility
            string apiJson = "[{\"id\":\"1\",\"name\":\"My List\",\"description\":\"\",\"userGroupId\":\"\"}," +
                             "{\"id\":\"2\",\"name\":\"Weekend\",\"description\":\"\",\"userGroupId\":\"\"}]";
            string wrapped = "{\"items\":" + apiJson + "}";

            var wrapper = JsonUtility.FromJson<ShoppingListArrayWrapper>(wrapped);

            Assert.IsNotNull(wrapper.items);
            Assert.AreEqual(2, wrapper.items.Length);
            Assert.AreEqual("My List", wrapper.items[0].name);
            Assert.AreEqual("Weekend", wrapper.items[1].name);
        }

        [Test]
        public void ShoppingListItemArrayWrapper_Deserializes_Api_Array_Response()
        {
            string apiJson = "[{\"id\":\"i1\",\"shoppingListId\":\"l1\",\"foodId\":\"f1\"," +
                             "\"quantity\":1.0,\"unit\":\"PIECES\",\"notes\":\"\",\"checked\":false}]";
            string wrapped = "{\"items\":" + apiJson + "}";

            var wrapper = JsonUtility.FromJson<ShoppingListItemArrayWrapper>(wrapped);

            Assert.IsNotNull(wrapper.items);
            Assert.AreEqual(1, wrapper.items.Length);
            Assert.AreEqual("f1", wrapper.items[0].foodId);
        }
    }
}
