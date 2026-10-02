namespace eu.foodmission.platform
{
    public enum RecipeOriginFilter
    {
        All,
        TheMealDb,
        Community,
    }

    /// <summary>Recipe origin codes (backend RecipeOrigin) and the TheMealDB source link.</summary>
    public static class RecipeOriginLinks
    {
        public const string TheMealDbOrigin = "THEMEALDB";
        public const string CommunityOrigin = "USER";
        private const string TheMealDbMealUrl = "https://www.themealdb.com/meal/";

        /// <summary>Value for GET /recipes?origin=; null means no filter.</summary>
        public static string ToQueryValue(RecipeOriginFilter filter)
        {
            switch (filter)
            {
                case RecipeOriginFilter.TheMealDb:
                    return TheMealDbOrigin;
                case RecipeOriginFilter.Community:
                    return CommunityOrigin;
                default:
                    return null;
            }
        }

        /// <summary>Original page of an imported recipe, or null when it has no TheMealDB id.</summary>
        public static string SourceUrl(Recipe recipe)
        {
            if (recipe == null || recipe.origin != TheMealDbOrigin || string.IsNullOrWhiteSpace(recipe.externalId))
            {
                return null;
            }
            return TheMealDbMealUrl + System.Uri.EscapeDataString(recipe.externalId.Trim());
        }
    }
}
