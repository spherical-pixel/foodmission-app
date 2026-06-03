using System;
using System.Text;

using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    [Serializable]
    public class MealItem
    {
        public string id;
        public string mealId;
        public string foodProductId;
        public string genericFoodId;
        public int quantity;
        public string unit;
        public string notes;
    }

    [Serializable]
    public class PaginatedMealItemResponse
    {
        public MealItem[] data;
        public int total;
        public int page;
        public int limit;
        public int totalPages;
    }

    public class CreateMealItemRequest
    {
        [JsonProperty("foodProductId")]
        public string foodProductId;

        [JsonProperty("genericFoodId")]
        public string genericFoodId;

        [JsonProperty("quantity")]
        public int quantity = 1;

        [JsonProperty("unit")]
        public string unit;

        [JsonProperty("notes")]
        public string notes;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
