using System;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class GenericFood
    {
        public string id;
        public string name;
        public string foodGroup;
        public string description;
    }

    [Serializable]
    public class PaginatedGenericFoodResponse
    {
        public GenericFood[] data;
        public int total;
        public int page;
        public int pageSize;
        public int totalPages;
    }
}
