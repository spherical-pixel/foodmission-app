using System;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class FoodItem
    {
        public string id;
        public string name;
        public string barcode;
        public string description;
        public string imageUrl;
        public string imageFrontUrl;
    }

    [Serializable]
    public class PaginatedFoodResponse
    {
        public FoodItem[] data;
        public int total;
        public int page;
        public int pageSize;
        public int totalPages;
    }

    [Serializable]
    public class NutritionalInfo
    {
        public float energyKcal;
        public float energyKj;
        public float fat;
        public float saturatedFat;
        public float carbohydrates;
        public float sugars;
        public float proteins;
        public float salt;
        public float sodium;
    }

    // Deserialized via Newtonsoft.Json — JsonUtility cannot handle string[] inside nested objects
    [Serializable]
    public class OpenFoodFactsProduct
    {
        public string id;
        public string barcode;
        public string name;
        public string genericName;
        public string[] brands;
        public string quantity;
        public string ingredients;
        public string[] allergens;
        public string nutritionGrade;
        public string ecoscoreGrade;
        public string imageFrontUrl;
        public NutritionalInfo nutritionalInfo;
    }

    [Serializable]
    public class OpenFoodFactsSearchResponse
    {
        public OpenFoodFactsProduct[] products;
        public int totalCount;
        public string page;     // API returns this as string, not int
        public int pageSize;
        public int totalPages;
    }
}
