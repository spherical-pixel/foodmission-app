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
        public int? prepTime;
        public int? cookTime;
        public int? servings;
        public string difficulty;
        public string[] tags;
        public float? sustainabilityScore;
        public float? price;
        public float? rating;
        public int? ratingCount;
        public string imageUrl;
        public string cuisineType;
        public string category;
        public string[] dietaryLabels;
        public string userId;
        public string createdAt;
        public string updatedAt;
        public string videoUrl;
        public string[] allergens;
        public RecipeNutritionalInfo nutritionalInfo;
        public RecipeIngredient[] ingredients;
    }

    [Serializable]
    public class RecipeNutritionalInfo
    {
        public float? fat;
        public float? carbs;
        public float? fiber;
        public float? protein;
        public float? energyKcal;
    }

    [Serializable]
    public class RecipeIngredient
    {
        public string id;
        public string recipeId;
        public string name;
        public string measure;
        public int? order;
        public string itemType;
        public string foodProductId;
        public string genericFoodId;
    }

    [Serializable]
    public class PaginatedRecipeResponse
    {
        public Recipe[] data;
        public int? total;
        public int? page;
        public int? limit;
        public int? totalPages;
    }

    [Serializable]
    public class CreateRecipeIngredientRequest
    {
        public string name;                  // required
        public string measure;
        public int? order;
        public string foodProductId;         // mutually exclusive w/ genericFoodId
        public string genericFoodId;
    }

    [Serializable]
    public class CreateRecipeRequest
    {
        public string title;                 // required (becomes optional in PATCH context)
        public string description;
        public string instructions;
        public string difficulty;
        public string cuisineType;
        public string category;
        public int? prepTime;
        public int? cookTime;
        public int? servings;
        public string[] tags;
        public string[] dietaryLabels;
        public string[] allergens;
        public RecipeNutritionalInfo nutritionalInfo;
        public float? sustainabilityScore;
        public float? price;
        public string externalId;
        public string imageUrl;
        public string videoUrl;
        public bool? isPublic;
        public CreateRecipeIngredientRequest[] ingredients;
    }

    [Serializable]
    public class MatchedIngredient
    {
        public string ingredientName;
        public string pantryItemName;
        public bool isExpiringSoon;
        public int? daysUntilExpiry;
    }

    [Serializable]
    public class RecommendationResponse
    {
        public string recipeId;
        public Recipe recipe;                // reuse existing Recipe model
        public int? matchCount;
        public int? totalIngredients;
        public int? expiringMatchCount;
        public MatchedIngredient[] matchedIngredients;
    }

    [Serializable]
    public class MultipleRecommendationResponse
    {
        public RecommendationResponse[] data;
        public int? expiringItemsCount;
        public int? totalPantryItems;
        public int? total;
        public int? offset;
        public int? limit;
        public int? page;
        public int? totalPages;
    }

    // UI-only DTO (not serialized) — like PantryItemView / ShoppingListItemView
    public class RecipeView
    {
        public Recipe Item;
        public string DisplayTitle;
        public string PlaceholderEmoji;     // "📚" fallback
        public bool HasImage => !string.IsNullOrEmpty(Item?.imageUrl);
    }
}
