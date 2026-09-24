using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MealLogModelsTests
    {
        [Test]
        public void MealLog_Roundtrips_Via_JsonUtility()
        {
            var log = new MealLog
            {
                id = "log-1",
                userId = "user-1",
                mealId = "meal-1",
                typeOfMeal = "LUNCH",
                timestamp = "2026-05-09T12:00:00",
                mealFromPantry = true,
                eatenOut = false,
                meal = new Meal { id = "meal-1", name = "Chicken Salad" }
            };

            string json = JsonUtility.ToJson(log);
            var result = JsonUtility.FromJson<MealLog>(json);

            Assert.AreEqual("log-1", result.id);
            Assert.AreEqual("LUNCH", result.typeOfMeal);
            Assert.IsTrue(result.mealFromPantry);
            Assert.IsFalse(result.eatenOut);
            Assert.IsNotNull(result.meal);
            Assert.AreEqual("Chicken Salad", result.meal.name);
        }

        [Test]
        public void PaginatedMealLogResponse_Deserializes()
        {
            string json = "{\"data\":[" +
                "{\"id\":\"l1\",\"userId\":\"u1\",\"mealId\":\"m1\",\"typeOfMeal\":\"BREAKFAST\"," +
                "\"timestamp\":\"2026-05-09T08:00:00\",\"mealFromPantry\":false,\"eatenOut\":true," +
                "\"createdAt\":\"\",\"updatedAt\":\"\"}," +
                "{\"id\":\"l2\",\"userId\":\"u1\",\"mealId\":\"m2\",\"typeOfMeal\":\"LUNCH\"," +
                "\"timestamp\":\"2026-05-09T13:00:00\",\"mealFromPantry\":true,\"eatenOut\":false," +
                "\"createdAt\":\"\",\"updatedAt\":\"\"}]," +
                "\"total\":2,\"page\":1,\"limit\":20,\"totalPages\":1}";

            var response = JsonUtility.FromJson<PaginatedMealLogResponse>(json);

            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.data.Length);
            Assert.AreEqual("BREAKFAST", response.data[0].typeOfMeal);
            Assert.IsTrue(response.data[1].mealFromPantry);
            Assert.IsTrue(response.data[0].eatenOut);
            Assert.AreEqual(1, response.totalPages);
        }

        [Test]
        public void PaginatedMealLogResponse_EmptyData_Roundtrips()
        {
            var response = new PaginatedMealLogResponse
            {
                data = System.Array.Empty<MealLog>(),
                total = 0,
                page = 1,
                limit = 20,
                totalPages = 0
            };

            string json = JsonUtility.ToJson(response);
            var result = JsonUtility.FromJson<PaginatedMealLogResponse>(json);

            Assert.IsNotNull(result.data);
            Assert.AreEqual(0, result.data.Length);
            Assert.AreEqual(0, result.totalPages);
        }

        [Test]
        public void CreateMealLogRequest_ToJsonBody_Produces_Valid_Json()
        {
            var request = new CreateMealLogRequest
            {
                mealId = "meal-1",
                typeOfMeal = "DINNER",
                mealFromPantry = true,
                eatenOut = false,
                timestamp = "2026-05-09T20:00:00Z"
            };

            byte[] body = request.ToJsonBody();
            string json = System.Text.Encoding.UTF8.GetString(body);

            StringAssert.Contains("\"mealId\":\"meal-1\"", json);
            StringAssert.Contains("\"typeOfMeal\":\"DINNER\"", json);
            StringAssert.Contains("\"mealFromPantry\":true", json);
            StringAssert.Contains("\"eatenOut\":false", json);
            StringAssert.Contains("\"timestamp\":\"2026-05-09T20:00:00Z\"", json);
        }

        [Test]
        public void CreateMealLogRequest_WithoutTimestamp_OmitsField()
        {
            var request = new CreateMealLogRequest
            {
                mealId = "meal-1",
                typeOfMeal = "SNACK",
                mealFromPantry = false,
                eatenOut = true
            };

            byte[] body = request.ToJsonBody();
            string json = System.Text.Encoding.UTF8.GetString(body);

            StringAssert.Contains("\"mealId\":\"meal-1\"", json);
            StringAssert.DoesNotContain("\"timestamp\"", json);
            StringAssert.Contains("\"mealFromPantry\":false", json);
            StringAssert.Contains("\"eatenOut\":true", json);
        }

        [Test]
        public void CreateMealLogRequest_Escapes_Special_Characters()
        {
            var request = new CreateMealLogRequest
            {
                mealId = "meal-\"with-quotes\"",
                typeOfMeal = "OTHER",
                mealFromPantry = false,
                eatenOut = false
            };

            byte[] body = request.ToJsonBody();
            string json = System.Text.Encoding.UTF8.GetString(body);

            StringAssert.Contains("\"meal-\\\"with-quotes\\\"\"", json);
        }

        [Test]
        public void CreateMealLogRequest_QuickLog_WithFlagsAndSwaps_OmitsMealId()
        {
            var request = new CreateMealLogRequest
            {
                typeOfMeal = "DINNER",
                flags = new[] { "MEAL_MEAT_FREE", "MEAL_LEGUME_CONSUMED" },
                swaps = new[] { "SWAP_BEEF_TO_LEGUMES" },
                timestamp = "2026-09-26T10:50:28.037Z"
            };

            byte[] body = request.ToJsonBody();
            string json = System.Text.Encoding.UTF8.GetString(body);

            StringAssert.DoesNotContain("\"mealId\"", json);
            StringAssert.Contains("\"typeOfMeal\":\"DINNER\"", json);
            StringAssert.Contains("\"MEAL_MEAT_FREE\"", json);
            StringAssert.Contains("\"MEAL_LEGUME_CONSUMED\"", json);
            StringAssert.Contains("\"SWAP_BEEF_TO_LEGUMES\"", json);
            StringAssert.Contains("\"timestamp\":\"2026-09-26T10:50:28.037Z\"", json);
        }

        [Test]
        public void MealLog_QuickLog_WithoutMeal_Deserializes()
        {
            string json = "{\"id\":\"quick-1\",\"userId\":\"u1\",\"mealId\":null,\"typeOfMeal\":\"LUNCH\"," +
                          "\"timestamp\":\"2026-09-26T10:50:28.037Z\",\"mealFromPantry\":false,\"eatenOut\":false," +
                          "\"flags\":[\"MEAL_VEGAN\"],\"swaps\":[\"SWAP_BEEF_TO_LEGUMES\"]}";

            var log = Newtonsoft.Json.JsonConvert.DeserializeObject<MealLog>(json);

            Assert.IsNotNull(log);
            Assert.AreEqual("quick-1", log.id);
            Assert.IsNull(log.mealId);
            Assert.IsNull(log.meal);
            Assert.AreEqual("LUNCH", log.typeOfMeal);
            Assert.IsNotNull(log.flags);
            Assert.AreEqual(1, log.flags.Length);
            Assert.AreEqual("MEAL_VEGAN", log.flags[0]);
            Assert.IsNotNull(log.swaps);
            Assert.AreEqual(1, log.swaps.Length);
            Assert.AreEqual("SWAP_BEEF_TO_LEGUMES", log.swaps[0]);
        }

        [Test]
        public void UpdateMealLogRequest_WithFlagsAndSwaps_SerializesCorrectly()
        {
            var request = new UpdateMealLogRequest
            {
                typeOfMeal = "DINNER",
                flags = new[] { "MEAL_MEAT_FREE", "MEAL_LEGUME_CONSUMED" },
                swaps = new[] { "SWAP_BEEF_TO_LEGUMES" }
            };

            byte[] body = request.ToJsonBody();
            string json = System.Text.Encoding.UTF8.GetString(body);

            StringAssert.Contains("\"typeOfMeal\":\"DINNER\"", json);
            StringAssert.Contains("\"flags\":[\"MEAL_MEAT_FREE\",\"MEAL_LEGUME_CONSUMED\"]", json);
            StringAssert.Contains("\"swaps\":[\"SWAP_BEEF_TO_LEGUMES\"]", json);
        }
    }
}
