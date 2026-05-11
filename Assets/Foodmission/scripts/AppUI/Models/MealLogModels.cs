using System;
using System.Text;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    [Serializable]
    public class MealLog
    {
        public string id;
        public string userId;
        public string mealId;
        public string typeOfMeal;
        public string timestamp;
        public bool mealFromPantry;
        public bool eatenOut;
        public string createdAt;
        public string updatedAt;
        public Meal meal;
    }

    [Serializable]
    public class PaginatedMealLogResponse
    {
        public MealLog[] data;
        public int total;
        public int page;
        public int limit;
        public int totalPages;
    }

    public class CreateMealLogRequest
    {
        [JsonProperty("mealId")]
        public string mealId;

        [JsonProperty("typeOfMeal")]
        public string typeOfMeal;

        [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
        public string timestamp;

        [JsonProperty("mealFromPantry")]
        public bool mealFromPantry;

        [JsonProperty("eatenOut")]
        public bool eatenOut;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }

    public class PantryDeduction
    {
        public string PantryItemId;
        public string FoodId;
        public string FoodCategoryId;
        public string FoodName;
        public float AvailableQuantity;
        public string Unit;
        public float Quantity;
    }
}
