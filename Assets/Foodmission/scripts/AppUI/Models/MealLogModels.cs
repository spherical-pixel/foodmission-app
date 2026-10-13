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
        public string[] flags;
        public string[] swaps;
        // DRAFT (staged for review, counts toward nothing) or CONFIRMED
        public string status;
        // Flags the backend derived from the meal items; on a draft, its proposals
        public string[] derivedFlags;
        public string confirmedAt;

        // Client-only markers (never from API)
        public bool isProduct;
        public bool isGenericFood;
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
        [JsonProperty("mealId", NullValueHandling = NullValueHandling.Ignore)]
        public string mealId;

        [JsonProperty("typeOfMeal")]
        public string typeOfMeal;

        [JsonProperty("flags", NullValueHandling = NullValueHandling.Ignore)]
        public string[] flags;

        [JsonProperty("swaps", NullValueHandling = NullValueHandling.Ignore)]
        public string[] swaps;

        [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
        public string timestamp;

        [JsonProperty("mealFromPantry")]
        public bool mealFromPantry;

        [JsonProperty("eatenOut")]
        public bool eatenOut;

        // Stage the log for review: the backend stores it as DRAFT with the flags it derives from the meal
        [JsonProperty("draft", NullValueHandling = NullValueHandling.Ignore)]
        public bool? draft;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }

    public static class MealLogStatus
    {
        public const string Draft = "DRAFT";
        public const string Confirmed = "CONFIRMED";
    }

    /// <summary>
    /// Body of POST /meal-logs/:id/confirm. The lists replace the draft's: send them always (an empty
    /// array too), because a missing list keeps the draft's flags, including proposals the user unmarked.
    /// </summary>
    public class MealLogFactsRequest
    {
        [JsonProperty("flags")]
        public string[] flags = Array.Empty<string>();

        [JsonProperty("swaps")]
        public string[] swaps = Array.Empty<string>();

        public byte[] ToJsonBody()
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }
    }

    public class UpdateMealLogRequest
    {
        [JsonProperty("mealId", NullValueHandling = NullValueHandling.Ignore)]
        public string mealId;

        [JsonProperty("typeOfMeal", NullValueHandling = NullValueHandling.Ignore)]
        public string typeOfMeal;

        [JsonProperty("flags", NullValueHandling = NullValueHandling.Ignore)]
        public string[] flags;

        [JsonProperty("swaps", NullValueHandling = NullValueHandling.Ignore)]
        public string[] swaps;

        [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
        public string timestamp;

        [JsonProperty("mealFromPantry", NullValueHandling = NullValueHandling.Ignore)]
        public bool? mealFromPantry;

        [JsonProperty("eatenOut", NullValueHandling = NullValueHandling.Ignore)]
        public bool? eatenOut;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }

    public class MealLogItem
    {
        public string id;
        public string foodProductId;
        public string genericFoodId;
        public string name;
        public float? quantity;
        public string unit;
        public bool isProduct;
        public bool isGenericFood;
    }


    public class PantryDeduction
    {
        public string PantryItemId;
        public string FoodProductId;
        public string GenericFoodId;
        public string FoodName;
        public float AvailableQuantity;
        public string Unit;
        public float Quantity;
    }
}
