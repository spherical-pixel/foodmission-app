using System;
using System.Text;
using Newtonsoft.Json;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class MealNutritionalInfo
    {
        public float carbs;
        public float fats;
        public float sugar;
    }

    [Serializable]
    public class Meal
    {
        public string id;
        public string name;
        public string recipeId;
        public bool isRecipe;
        public float? calories;
        public float? proteins;
        public MealNutritionalInfo nutritionalInfo;
        public float? sustainabilityScore;
        public float? price;
        public string barcode;
        public string[] mealCategories;
        public string mealCourse;
        public string[] dietaryPreferences;
        public string userId;
        public string createdAt;
        public string updatedAt;
        public MealItemDetail[] items;
    }

    [Serializable]
    public class PaginatedMealResponse
    {
        public Meal[] data;
        public int total;
        public int page;
        public int limit;
        public int totalPages;
    }

    public class CreateMealRequest
    {
        [JsonProperty("name")]
        public string name;

        [JsonProperty("recipeId")]
        public string recipeId;

        [JsonProperty("calories")]
        public float? calories;

        [JsonProperty("proteins")]
        public float? proteins;

        [JsonProperty("nutritionalInfo")]
        public MealNutritionalInfo nutritionalInfo;

        [JsonProperty("sustainabilityScore")]
        public float? sustainabilityScore;

        [JsonProperty("price")]
        public float? price;

        [JsonProperty("barcode")]
        public string barcode;

        [JsonProperty("mealCategories")]
        public string[] mealCategories;

        [JsonProperty("mealCourse")]
        public string mealCourse;

        [JsonProperty("dietaryPreferences")]
        public string[] dietaryPreferences;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }

    public class UpdateMealRequest
    {
        [JsonProperty("name")]
        public string name;

        [JsonProperty("recipeId")]
        public string recipeId;

        [JsonProperty("calories")]
        public float? calories;

        [JsonProperty("proteins")]
        public float? proteins;

        [JsonProperty("nutritionalInfo")]
        public MealNutritionalInfo nutritionalInfo;

        [JsonProperty("sustainabilityScore")]
        public float? sustainabilityScore;

        [JsonProperty("price")]
        public float? price;

        [JsonProperty("barcode")]
        public string barcode;

        [JsonProperty("mealCategories")]
        public string[] mealCategories;

        [JsonProperty("mealCourse")]
        public string mealCourse;

        [JsonProperty("dietaryPreferences")]
        public string[] dietaryPreferences;

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
