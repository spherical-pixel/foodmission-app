using System;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Optional metadata attached to a catalog entry.
    /// Currently used by regions to carry the parent country code.
    /// </summary>
    [Serializable]
    public class CatalogMeta
    {
        public string countryCode;
    }

    /// <summary>
    /// A single catalog entry with a code and display label.
    /// Used for genders, activity levels, dietary preferences, etc.
    /// </summary>
    [Serializable]
    public class CatalogItem
    {
        public string code;
        public string label;
        public CatalogMeta meta;
    }

    /// <summary>
    /// Dropdown catalog values for gamification onboarding baselines returned in GET /api/v1/catalog/startup.
    /// </summary>
    [Serializable]
    public class CatalogOnboardingStartupData
    {
        public CatalogItem[] weeklyMeatRanges;
        public CatalogItem[] weeklyBeefFrequencies;
        public CatalogItem[] weeklyFoodWasteRanges;
        public CatalogItem[] weeklyUpfRanges;
        public CatalogItem[] weeklyReusableRanges;
        public CatalogItem[] userSegments;
        public CatalogItem[] motivations;
    }

    /// <summary>
    /// Container for all catalog data returned by GET /api/v1/catalog/startup.
    /// Each field maps to an array of CatalogItem or nested onboarding baselines object.
    /// </summary>
    [Serializable]
    public class CatalogData
    {
        public CatalogItem[] genders;
        public CatalogItem[] activityLevels;
        public CatalogItem[] dietaryPreferences;
        public CatalogItem[] shoppingResponsibilities;
        public CatalogItem[] educationLevels;
        public CatalogItem[] annualIncomeLevels;

        public CatalogOnboardingStartupData onboarding;
        public CatalogItem[] progressIndicatorKinds;
        public CatalogItem[] progressPrecisions;
        public CatalogItem[] walletCurrencies;
    }

    /// <summary>
    /// Top-level response wrapper for the catalog startup endpoint.
    /// </summary>
    [Serializable]
    public class StartupResponse
    {
        public CatalogData data;
    }

    /// <summary>
    /// Response wrapper for individual catalog list endpoints (type-of-meals,
    /// meal-categories, meal-courses, etc.) that return { data: [CatalogItem] }.
    /// </summary>
    [Serializable]
    public class CatalogListResponse
    {
        public CatalogItem[] data;
    }

    /// <summary>
    /// Paginated response wrapper for endpoints like GET /catalog/countries and
    /// GET /catalog/regions that return { data, total, page, limit, totalPages }.
    /// </summary>
    [Serializable]
    public class PaginatedCatalogResponse
    {
        public CatalogItem[] data;
        public int total;
        public int page;
        public int limit;
        public int totalPages;
    }
}