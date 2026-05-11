using System;

namespace eu.foodmission.platform
{
    [Serializable]
    public class CachedFoodSearch
    {
        public OpenFoodFactsSearchResponse data;
        public long cachedAtTicks;
    }

    [Serializable]
    public class CachedCategorySearch
    {
        public PaginatedFoodCategoryResponse data;
        public long cachedAtTicks;
    }
}
