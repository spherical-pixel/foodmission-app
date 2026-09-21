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

    public enum RecipeBookTab
    {
        ForYou = 0,
        Explore = 1,
        MyRecipes = 2
    }

    // UI-only DTO (not serialized) — like PantryItemView / ShoppingListItemView
    public class RecipeView
    {
        public Recipe Item;
        public string DisplayTitle;
        public string PlaceholderEmoji;     // "📚" fallback
        public bool HasImage => !string.IsNullOrEmpty(Item?.imageUrl);
        public int MatchCount;
        public int TotalIngredients;
        public int ExpiringMatchCount;
        public bool IsRecommendation;
        public string[] ExpiringIngredientNames;
    }

    public class RecipeCatalogItem
    {
        public string Code { get; set; }
        public string Emoji { get; set; }
        public string NameEn { get; set; }
        public string NameEs { get; set; }

        public string GetLocalizedName(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                lang = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale?.Identifier.Code ?? "en";
            }

            if (lang.StartsWith("es", System.StringComparison.OrdinalIgnoreCase))
                return NameEs;
            return NameEn;
        }
    }

    public static class RecipeCatalogs
    {
        public static readonly System.Collections.Generic.List<RecipeCatalogItem> Categories = new()
        {
            new RecipeCatalogItem { Code = "Pasta", Emoji = "🍝", NameEn = "Pasta", NameEs = "Pasta" },
            new RecipeCatalogItem { Code = "Rice", Emoji = "🍚", NameEn = "Rice & Grains", NameEs = "Arroces y Cereales" },
            new RecipeCatalogItem { Code = "Salad", Emoji = "🥗", NameEn = "Salads", NameEs = "Ensaladas" },
            new RecipeCatalogItem { Code = "Soup", Emoji = "🍲", NameEn = "Soups & Stews", NameEs = "Sopas y Guisos" },
            new RecipeCatalogItem { Code = "Vegetarian", Emoji = "🥦", NameEn = "Vegetables", NameEs = "Verduras" },
            new RecipeCatalogItem { Code = "Legumes", Emoji = "🧆", NameEn = "Legumes", NameEs = "Legumbres" },
            new RecipeCatalogItem { Code = "Chicken", Emoji = "🍗", NameEn = "Poultry", NameEs = "Aves y Pollo" },
            new RecipeCatalogItem { Code = "Seafood", Emoji = "🐟", NameEn = "Fish & Seafood", NameEs = "Pescados y Mariscos" },
            new RecipeCatalogItem { Code = "Meat", Emoji = "🥩", NameEn = "Meat", NameEs = "Carnes" },
            new RecipeCatalogItem { Code = "Dessert", Emoji = "🍰", NameEn = "Desserts", NameEs = "Postres" },
            new RecipeCatalogItem { Code = "Breakfast", Emoji = "🍳", NameEn = "Breakfast", NameEs = "Desayunos" }
        };

        public static readonly System.Collections.Generic.List<RecipeCatalogItem> Cuisines = new()
        {
            new RecipeCatalogItem { Code = "Mediterranean", Emoji = "🫒", NameEn = "Mediterranean", NameEs = "Mediterránea" },
            new RecipeCatalogItem { Code = "Spanish", Emoji = "🥘", NameEn = "Spanish", NameEs = "Española" },
            new RecipeCatalogItem { Code = "Italian", Emoji = "🍕", NameEn = "Italian", NameEs = "Italiana" },
            new RecipeCatalogItem { Code = "Mexican", Emoji = "🌮", NameEn = "Mexican", NameEs = "Mexicana" },
            new RecipeCatalogItem { Code = "Asian", Emoji = "🥢", NameEn = "Asian", NameEs = "Asiática" },
            new RecipeCatalogItem { Code = "Middle Eastern", Emoji = "🧆", NameEn = "Middle Eastern", NameEs = "Oriente Medio" },
            new RecipeCatalogItem { Code = "American", Emoji = "🍔", NameEn = "American", NameEs = "Americana" },
            new RecipeCatalogItem { Code = "Nordic", Emoji = "🫐", NameEn = "Nordic", NameEs = "Nórdica" }
        };

        public static string GetCategoryEmoji(string category)
        {
            if (string.IsNullOrEmpty(category)) return "🍲";
            var item = Categories.Find(c => c.Code.Equals(category, System.StringComparison.OrdinalIgnoreCase)
                                         || c.NameEn.Equals(category, System.StringComparison.OrdinalIgnoreCase));
            return item?.Emoji ?? "🍲";
        }
    }
}
