using System;

using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class PantryModelsTests
    {
        [Test]
        public void PantryItem_With_FoodProductId_Roundtrips_Via_JsonUtility()
        {
            var item = new PantryItem
            {
                id = "pi-1",
                pantryId = "p-1",
                foodProductId = "food-1",
                genericFoodId = "",
                quantity = 2f,
                unit = "KG",
                notes = "organic",
                location = "Fridge",
                expiryDate = "2026-05-30"
            };
            string json = JsonUtility.ToJson(item);
            var result = JsonUtility.FromJson<PantryItem>(json);

            Assert.AreEqual("pi-1", result.id);
            Assert.AreEqual("food-1", result.foodProductId);
            Assert.AreEqual("", result.genericFoodId);
            Assert.AreEqual("Fridge", result.location);
            Assert.AreEqual("2026-05-30", result.expiryDate);
        }

        [Test]
        public void PantryItem_With_GenericFoodId_Roundtrips_Via_JsonUtility()
        {
            var item = new PantryItem
            {
                id = "pi-2",
                pantryId = "p-1",
                foodProductId = "",
                genericFoodId = "cat-1",
                quantity = 500f,
                unit = "G"
            };
            string json = JsonUtility.ToJson(item);
            var result = JsonUtility.FromJson<PantryItem>(json);

            Assert.AreEqual("", result.foodProductId);
            Assert.AreEqual("cat-1", result.genericFoodId);
            Assert.AreEqual(500f, result.quantity, 0.001f);
        }

        [Test]
        public void Pantry_With_Embedded_Items_Deserializes()
        {
            string json = "{\"id\":\"p-1\",\"userId\":\"u-1\",\"items\":[" +
                          "{\"id\":\"pi-1\",\"pantryId\":\"p-1\",\"foodProductId\":\"f-1\",\"genericFoodId\":\"\"," +
                          "\"quantity\":1.0,\"unit\":\"PIECES\",\"notes\":\"\",\"location\":\"\",\"expiryDate\":\"\"}]}";

            var pantry = JsonUtility.FromJson<Pantry>(json);

            Assert.AreEqual("p-1", pantry.id);
            Assert.IsNotNull(pantry.items);
            Assert.AreEqual(1, pantry.items.Length);
            Assert.AreEqual("f-1", pantry.items[0].foodProductId);
        }

        [Test]
        public void PantryItemArrayWrapper_Deserializes_Api_Array_Response()
        {
            string apiJson = "[{\"id\":\"pi-1\",\"pantryId\":\"p-1\",\"foodProductId\":\"f-1\",\"genericFoodId\":\"\"," +
                             "\"quantity\":2.0,\"unit\":\"KG\",\"notes\":\"\",\"location\":\"\",\"expiryDate\":\"\"}]";
            string wrapped = "{\"items\":" + apiJson + "}";

            var wrapper = JsonUtility.FromJson<PantryItemArrayWrapper>(wrapped);

            Assert.IsNotNull(wrapper.items);
            Assert.AreEqual(1, wrapper.items.Length);
            Assert.AreEqual("f-1", wrapper.items[0].foodProductId);
        }

        [TestCase("2026-05-30")]
        [TestCase("2026-05-30T00:00:00.000Z")]
        public void TryParseExpiryDate_CalendarDate_KeepsTheDayInAnyTimeZone(string value)
        {
            // Midnight UTC is the day the user picked; converting to local made it the day before west of UTC
            Assert.IsTrue(PantryItem.TryParseExpiryDate(value, out DateTime date));

            Assert.AreEqual(new DateTime(2026, 5, 30), date);
        }

        [Test]
        public void TryParseExpiryDate_TimestampWithTime_UsesLocalDay()
        {
            var utc = new DateTime(2026, 5, 30, 23, 30, 0, DateTimeKind.Utc);

            Assert.IsTrue(PantryItem.TryParseExpiryDate(utc.ToString("o"), out DateTime date));

            Assert.AreEqual(utc.ToLocalTime().Date, date);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not a date")]
        public void TryParseExpiryDate_Invalid_ReturnsFalse(string value)
        {
            Assert.IsFalse(PantryItem.TryParseExpiryDate(value, out _));
        }

        [Test]
        public void ExpiryDateTime_UsesCalendarDay()
        {
            var item = new PantryItem { expiryDate = "2026-05-30T00:00:00.000Z" };

            Assert.AreEqual(new DateTime(2026, 5, 30), item.ExpiryDateTime);
        }
    }
}
