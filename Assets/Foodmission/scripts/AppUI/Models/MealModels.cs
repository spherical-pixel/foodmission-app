using System;

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
        public float calories;
        public float proteins;
        public MealNutritionalInfo nutritionalInfo;
        public float sustainabilityScore;
        public float price;
        public string barcode;
        public string[] mealCategories;
        public string mealCourse;
        public string[] dietaryPreferences;
        public string userId;
        public string createdAt;
        public string updatedAt;
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

    // Outgoing request models — not deserialized by JsonUtility, plain C# classes
    public class CreateMealRequest
    {
        public string name;
        public string recipeId;
        public float? calories;
        public float? proteins;
        public MealNutritionalInfo nutritionalInfo;
        public float? sustainabilityScore;
        public float? price;
        public string barcode;
        public string[] mealCategories;
        public string mealCourse;
        public string[] dietaryPreferences;
    }

    public class UpdateMealRequest
    {
        public string name;
        public string recipeId;
        public float? calories;
        public float? proteins;
        public MealNutritionalInfo nutritionalInfo;
        public float? sustainabilityScore;
        public float? price;
        public string barcode;
        public string[] mealCategories;
        public string mealCourse;
        public string[] dietaryPreferences;
    }
}
