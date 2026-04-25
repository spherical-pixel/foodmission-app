using System;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class FoodCategory
    {
        public string id;
        public string name;
        public string foodGroup;
        public string description;
    }

    [Serializable]
    public class PaginatedFoodCategoryResponse
    {
        public FoodCategory[] data;
        public int total;
        public int page;
        public int pageSize;
        public int totalPages;
    }
}
