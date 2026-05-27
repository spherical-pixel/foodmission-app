using System;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class GenericFood
    {
        public string id;
        public string foodName;
        public string foodGroup;
    }

    [Serializable]
    public class PaginatedGenericFoodResponse
    {
        public GenericFood[] items;
        public int total;
        public int page;
        public int limit;
        public int totalPages;
    }
}
