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
        public bool? isPublic;
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
        public string CountryCode { get; set; }
        public string Emoji { get; set; }
        public string LocalizationKey { get; set; }

        public string GetLocalizedName()
        {
            return UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UI", LocalizationKey);
        }
    }

    public static class RecipeCatalogs
    {
        public static readonly System.Collections.Generic.List<RecipeCatalogItem> Categories = new()
        {
            new RecipeCatalogItem { Code = "Beef", Emoji = "🥩", LocalizationKey = "RECIPES_CAT_BEEF"},
            new RecipeCatalogItem { Code = "Chicken", Emoji = "🍗", LocalizationKey = "RECIPES_CAT_CHICKEN"},
            new RecipeCatalogItem { Code = "Dessert", Emoji = "🍰", LocalizationKey = "RECIPES_CAT_DESSERT"},
            new RecipeCatalogItem { Code = "Lamb", Emoji = "🍖", LocalizationKey = "RECIPES_CAT_LAMB"},
            new RecipeCatalogItem { Code = "Miscellaneous", Emoji = "🍱", LocalizationKey = "RECIPES_CAT_MISC"},
            new RecipeCatalogItem { Code = "Pasta", Emoji = "🍝", LocalizationKey = "RECIPES_CAT_PASTA"},
            new RecipeCatalogItem { Code = "Pork", Emoji = "🥓", LocalizationKey = "RECIPES_CAT_PORK"},
            new RecipeCatalogItem { Code = "Seafood", Emoji = "🐟", LocalizationKey = "RECIPES_CAT_SEAFOOD"},
            new RecipeCatalogItem { Code = "Side", Emoji = "🍟", LocalizationKey = "RECIPES_CAT_SIDE"},
            new RecipeCatalogItem { Code = "Starter", Emoji = "🥗", LocalizationKey = "RECIPES_CAT_STARTER"},
            new RecipeCatalogItem { Code = "Vegan", Emoji = "🌱", LocalizationKey = "RECIPES_CAT_VEGAN"},
            new RecipeCatalogItem { Code = "Vegetarian", Emoji = "🥦", LocalizationKey = "RECIPES_CAT_VEGETARIAN"},
            new RecipeCatalogItem { Code = "Breakfast", Emoji = "🍳", LocalizationKey = "RECIPES_CAT_BREAKFAST"},
            new RecipeCatalogItem { Code = "Goat", Emoji = "🐐", LocalizationKey = "RECIPES_CAT_GOAT"}
        };

        public static readonly System.Collections.Generic.List<RecipeCatalogItem> Cuisines = new()
        {
            new RecipeCatalogItem { Code = "Algerian", CountryCode = "DZ", Emoji = "🇩🇿", LocalizationKey = "RECIPES_CUISINE_ALGERIAN"},
            new RecipeCatalogItem { Code = "American", CountryCode = "US", Emoji = "🇺🇸", LocalizationKey = "RECIPES_CUISINE_AMERICAN"},
            new RecipeCatalogItem { Code = "Argentinian", CountryCode = "AR", Emoji = "🇦🇷", LocalizationKey = "RECIPES_CUISINE_ARGENTINIAN"},
            new RecipeCatalogItem { Code = "Australian", CountryCode = "AU", Emoji = "🇦🇺", LocalizationKey = "RECIPES_CUISINE_AUSTRALIAN"},
            new RecipeCatalogItem { Code = "British", CountryCode = "GB", Emoji = "🇬🇧", LocalizationKey = "RECIPES_CUISINE_BRITISH"},
            new RecipeCatalogItem { Code = "Canadian", CountryCode = "CA", Emoji = "🇨🇦", LocalizationKey = "RECIPES_CUISINE_CANADIAN"},
            new RecipeCatalogItem { Code = "Chinese", CountryCode = "CN", Emoji = "🇨🇳", LocalizationKey = "RECIPES_CUISINE_CHINESE"},
            new RecipeCatalogItem { Code = "Croatian", CountryCode = "HR", Emoji = "🇭🇷", LocalizationKey = "RECIPES_CUISINE_CROATIAN"},
            new RecipeCatalogItem { Code = "Dutch", CountryCode = "NL", Emoji = "🇳🇱", LocalizationKey = "RECIPES_CUISINE_DUTCH"},
            new RecipeCatalogItem { Code = "Egyptian", CountryCode = "EG", Emoji = "🇪🇬", LocalizationKey = "RECIPES_CUISINE_EGYPTIAN"},
            new RecipeCatalogItem { Code = "Filipino", CountryCode = "PH", Emoji = "🇵🇭", LocalizationKey = "RECIPES_CUISINE_FILIPINO"},
            new RecipeCatalogItem { Code = "French", CountryCode = "FR", Emoji = "🇫🇷", LocalizationKey = "RECIPES_CUISINE_FRENCH"},
            new RecipeCatalogItem { Code = "Greek", CountryCode = "GR", Emoji = "🇬🇷", LocalizationKey = "RECIPES_CUISINE_GREEK"},
            new RecipeCatalogItem { Code = "Indian", CountryCode = "IN", Emoji = "🇮🇳", LocalizationKey = "RECIPES_CUISINE_INDIAN"},
            new RecipeCatalogItem { Code = "Irish", CountryCode = "IE", Emoji = "🇮🇪", LocalizationKey = "RECIPES_CUISINE_IRISH"},
            new RecipeCatalogItem { Code = "Italian", CountryCode = "IT", Emoji = "🇮🇹", LocalizationKey = "RECIPES_CUISINE_ITALIAN"},
            new RecipeCatalogItem { Code = "Jamaican", CountryCode = "JM", Emoji = "🇯🇲", LocalizationKey = "RECIPES_CUISINE_JAMAICAN"},
            new RecipeCatalogItem { Code = "Kenyan", CountryCode = "KE", Emoji = "🇰🇪", LocalizationKey = "RECIPES_CUISINE_KENYAN"},
            new RecipeCatalogItem { Code = "Malaysian", CountryCode = "MY", Emoji = "🇲🇾", LocalizationKey = "RECIPES_CUISINE_MALAYSIAN"},
            new RecipeCatalogItem { Code = "Mexican", CountryCode = "MX", Emoji = "🇲🇽", LocalizationKey = "RECIPES_CUISINE_MEXICAN"},
            new RecipeCatalogItem { Code = "Moroccan", CountryCode = "MA", Emoji = "🇲🇦", LocalizationKey = "RECIPES_CUISINE_MOROCCAN"},
            new RecipeCatalogItem { Code = "Norwegian", CountryCode = "NO", Emoji = "🇳🇴", LocalizationKey = "RECIPES_CUISINE_NORWEGIAN"},
            new RecipeCatalogItem { Code = "Polish", CountryCode = "PL", Emoji = "🇵🇱", LocalizationKey = "RECIPES_CUISINE_POLISH"},
            new RecipeCatalogItem { Code = "Portuguese", CountryCode = "PT", Emoji = "🇵🇹", LocalizationKey = "RECIPES_CUISINE_PORTUGUESE"},
            new RecipeCatalogItem { Code = "Saudi Arabian", CountryCode = "SA", Emoji = "🇸🇦", LocalizationKey = "RECIPES_CUISINE_SAUDI_ARABIAN"},
            new RecipeCatalogItem { Code = "Slovakian", CountryCode = "SK", Emoji = "🇸🇰", LocalizationKey = "RECIPES_CUISINE_SLOVAKIAN"},
            new RecipeCatalogItem { Code = "Spanish", CountryCode = "ES", Emoji = "🇪🇸", LocalizationKey = "RECIPES_CUISINE_SPANISH"},
            new RecipeCatalogItem { Code = "Syrian", CountryCode = "SY", Emoji = "🇸🇾", LocalizationKey = "RECIPES_CUISINE_SYRIAN"},
            new RecipeCatalogItem { Code = "Thai", CountryCode = "TH", Emoji = "🇹🇭", LocalizationKey = "RECIPES_CUISINE_THAI"},
            new RecipeCatalogItem { Code = "Tunisian", CountryCode = "TN", Emoji = "🇹🇳", LocalizationKey = "RECIPES_CUISINE_TUNISIAN"},
            new RecipeCatalogItem { Code = "Turkish", CountryCode = "TR", Emoji = "🇹🇷", LocalizationKey = "RECIPES_CUISINE_TURKISH"},
            new RecipeCatalogItem { Code = "Ukrainian", CountryCode = "UA", Emoji = "🇺🇦", LocalizationKey = "RECIPES_CUISINE_UKRAINIAN"},
            new RecipeCatalogItem { Code = "Uruguayan", CountryCode = "UY", Emoji = "🇺🇾", LocalizationKey = "RECIPES_CUISINE_URUGUAYAN"},
            new RecipeCatalogItem { Code = "Venezulan", CountryCode = "VE", Emoji = "🇻🇪", LocalizationKey = "RECIPES_CUISINE_VENEZUELAN"},
            new RecipeCatalogItem { Code = "Vietnamese", CountryCode = "VN", Emoji = "🇻🇳", LocalizationKey = "RECIPES_CUISINE_VIETNAMESE"}
        };

        public static string GetCategoryEmoji(string category)
        {
            if (string.IsNullOrEmpty(category)) return "🍲";
            var item = Categories.Find(c => c.Code.Equals(category, System.StringComparison.OrdinalIgnoreCase));
            return item?.Emoji ?? "🍲";
        }

        public static string GetLocalizedCategoryName(string category)
        {
            if (string.IsNullOrEmpty(category)) return "";
            var item = Categories.Find(c => c.Code.Equals(category, System.StringComparison.OrdinalIgnoreCase));
            return item?.GetLocalizedName() ?? category;
        }

        public static string GetCuisineEmoji(string cuisine)
        {
            if (string.IsNullOrEmpty(cuisine)) return "🌍";
            var item = Cuisines.Find(c => c.Code.Equals(cuisine, System.StringComparison.OrdinalIgnoreCase));
            return item?.Emoji ?? "🌍";
        }

        public static string GetLocalizedCuisineName(string cuisine)
        {
            if (string.IsNullOrEmpty(cuisine)) return "";
            var item = Cuisines.Find(c => c.Code.Equals(cuisine, System.StringComparison.OrdinalIgnoreCase));
            return item?.GetLocalizedName() ?? cuisine;
        }

        public static RecipeCatalogItem GetCuisineByCountryCode(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode)) return null;
            var code = countryCode.Trim();
            if (code.Equals("UK", System.StringComparison.OrdinalIgnoreCase)) code = "GB";
            return Cuisines.Find(c => !string.IsNullOrEmpty(c.CountryCode) && c.CountryCode.Equals(code, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
