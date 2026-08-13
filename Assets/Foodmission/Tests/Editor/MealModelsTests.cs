using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MealModelsTests
    {
        [Test]
        public void Meal_Roundtrips_Via_JsonUtility()
        {
            var meal = new Meal
            {
                id = "meal-1",
                name = "Grilled chicken salad",
                mealCourse = "MAIN_DISH",
                calories = 520f,
                proteins = 42f,
                userId = "user-1",
                createdAt = "2026-04-27T10:00:00Z",
                updatedAt = "2026-04-27T10:00:00Z"
            };

            string json = JsonUtility.ToJson(meal);
            Meal result = JsonUtility.FromJson<Meal>(json);

            Assert.AreEqual("meal-1", result.id);
            Assert.AreEqual("Grilled chicken salad", result.name);
            Assert.AreEqual("MAIN_DISH", result.mealCourse);
            Assert.AreEqual(520f, result.calories, 0.001f);
            Assert.AreEqual(42f, result.proteins, 0.001f);
        }

        [Test]
        public void Meal_With_Categories_And_Preferences_Roundtrips()
        {
            var meal = new Meal
            {
                id = "meal-2",
                name = "Vegan bowl",
                mealCategories = new[] { "PLANT_PROTEIN", "VEGGIES_FRUIT" },
                dietaryPreferences = new[] { "VEGAN", "GLUTEN_FREE" },
                userId = "user-1",
                createdAt = "2026-04-27T10:00:00Z",
                updatedAt = "2026-04-27T10:00:00Z"
            };

            string json = JsonUtility.ToJson(meal);
            Meal result = JsonUtility.FromJson<Meal>(json);

            Assert.IsNotNull(result.mealCategories);
            Assert.AreEqual(2, result.mealCategories.Length);
            Assert.AreEqual("PLANT_PROTEIN", result.mealCategories[0]);
            Assert.IsNotNull(result.dietaryPreferences);
            Assert.AreEqual("VEGAN", result.dietaryPreferences[0]);
        }

        [Test]
        public void Meal_With_NutritionalInfo_Roundtrips()
        {
            var meal = new Meal
            {
                id = "meal-3",
                name = "Pasta",
                nutritionalInfo = new MealNutritionalInfo { carbs = 40f, fats = 20f, sugar = 5f },
                userId = "user-1",
                createdAt = "2026-04-27T10:00:00Z",
                updatedAt = "2026-04-27T10:00:00Z"
            };

            string json = JsonUtility.ToJson(meal);
            Meal result = JsonUtility.FromJson<Meal>(json);

            Assert.IsNotNull(result.nutritionalInfo);
            Assert.AreEqual(40f, result.nutritionalInfo.carbs, 0.001f);
            Assert.AreEqual(20f, result.nutritionalInfo.fats, 0.001f);
            Assert.AreEqual(5f, result.nutritionalInfo.sugar, 0.001f);
        }

        [Test]
        public void PaginatedMealResponse_Deserializes_From_Api_Json()
        {
            string json = "{\"data\":[" +
                "{\"id\":\"m1\",\"name\":\"Salad\",\"mealCourse\":\"SIDE_SNACK\"," +
                "\"calories\":0,\"proteins\":0,\"sustainabilityScore\":0,\"price\":0," +
                "\"userId\":\"u1\",\"createdAt\":\"2026-04-27T00:00:00Z\",\"updatedAt\":\"2026-04-27T00:00:00Z\"}" +
                "],\"total\":1,\"page\":1,\"limit\":20,\"totalPages\":1}";

            PaginatedMealResponse result = JsonUtility.FromJson<PaginatedMealResponse>(json);

            Assert.IsNotNull(result.data);
            Assert.AreEqual(1, result.data.Length);
            Assert.AreEqual("m1", result.data[0].id);
            Assert.AreEqual("Salad", result.data[0].name);
            Assert.AreEqual(1, result.total);
            Assert.AreEqual(1, result.totalPages);
        }

        [Test]
        public void Meal_Deserializes_Null_Numeric_Fields_Via_NewtonsoftJson()
        {
            string json = "{\"id\":\"m2\",\"name\":\"Null Meal\"," +
                "\"calories\":null,\"proteins\":null,\"sustainabilityScore\":null,\"price\":null," +
                "\"userId\":\"u1\",\"createdAt\":\"2026-04-27T00:00:00Z\",\"updatedAt\":\"2026-04-27T00:00:00Z\"}";

            Meal result = Newtonsoft.Json.JsonConvert.DeserializeObject<Meal>(json);

            Assert.IsNotNull(result);
            Assert.AreEqual("m2", result.id);
            Assert.IsNull(result.calories);
            Assert.IsNull(result.proteins);
            Assert.IsNull(result.sustainabilityScore);
            Assert.IsNull(result.price);
        }
    }
}
