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
    }
}
