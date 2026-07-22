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
        public int? quantity;
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

    [Serializable]
    public class MealItemDetail
    {
        public string id;
        public string mealId;
        public string itemType;
        public string foodProductId;
        public string genericFoodId;
        public int? quantity;
        public string unit;
        public string notes;
        public MealItemFoodProduct foodProduct;
        public MealItemGenericFood genericFood;
    }

    [Serializable]
    public class MealItemFoodProduct
    {
        public string id;
        public string name;
        public string barcode;
    }

    [Serializable]
    public class MealItemGenericFood
    {
        public string id;
        public string foodName;
    }

    [Serializable]
    public class MealItemDetailList
    {
        public MealItemDetail[] data;
    }

    public class CreateMealItemRequest
    {
        [JsonProperty("foodProductId")]
        public string foodProductId;

        [JsonProperty("genericFoodId")]
        public string genericFoodId;

        [JsonProperty("quantity", NullValueHandling = NullValueHandling.Ignore)]
        public int? quantity;

        [JsonProperty("unit", NullValueHandling = NullValueHandling.Ignore)]
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
