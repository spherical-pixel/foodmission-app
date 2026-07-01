using System;

namespace eu.foodmission.platform
{
    [Serializable]
    public class Recipe
    {
        public string id;
        public string title;
        public string description;
        public string instructions;
        public int prepTime;
        public int cookTime;
        public int servings;
        public string difficulty;
        public string[] tags;
        public float sustainabilityScore;
        public float price;
        public float rating;
        public int ratingCount;
        public string imageUrl;
        public string cuisineType;
        public string category;
        public string[] dietaryLabels;
        public string createdAt;
        public string updatedAt;
        public RecipeIngredient[] ingredients;
    }

    [Serializable]
    public class RecipeIngredient
    {
        public string id;
        public string recipeId;
        public string name;
        public string measure;
        public int order;
        public string itemType;
        public string foodProductId;
        public string genericFoodId;
    }

    [Serializable]
    public class PaginatedRecipeResponse
    {
        public Recipe[] data;
        public int total;
        public int page;
        public int limit;
        public int totalPages;
    }
}
