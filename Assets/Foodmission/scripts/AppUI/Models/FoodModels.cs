using System;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class FoodProduct
    {
        public string id;
        public string name;
        public string barcode;
        public string description;
        public string imageUrl;
        public string imageFrontUrl;
        public string nutriscoreGrade;
        public int? nutriscoreScore;
        public int? novaGroup;
        public string ecoscoreGrade;
    }

    [Serializable]
    public class PaginatedFoodProductResponse
    {
        public FoodProduct[] data;
        public int total;
        public int page;
        public int limit;
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

        // Extended fields for direct integration
        public string imageUrl;
        public string[] categories;
        public string[] labels;
        public string servingSize;
        public string[] traces;
        public string origins;
        public string manufacturingPlaces;
        public int? novaGroup;
        public int? nutriscoreScore;
        public float? carbonFootprint;
        public string nutritionDataPer;
        public string imageNutritionUrl;
        public string imageIngredientsUrl;
        public string[] countries;
        public string[] stores;
        public float? completeness;
        public DateTime? createdAt;
        public DateTime? lastModified;
        public string rawNutriments;
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

    [Serializable]
    public class FoodProductDetail
    {
        public string id;
        public string name;
        public string description;
        public string barcode;
        public string brands;
        public string[] categories;
        public string[] labels;
        public string quantity;
        public string servingSize;
        public string ingredientsText;
        public string[] allergens;
        public string[] traces;
        public string[] countries;
        public string origins;
        public string manufacturingPlaces;
        public string imageUrl;
        public string imageFrontUrl;

        public object nutrimentsRaw;

        public string nutritionGrade;
        public int? novaGroup;
        public string ecoscoreGrade;
        public float? carbonFootprint;

        public object nutrientLevels;

        public bool? isVegan;
        public bool? isVegetarian;
        public bool? isPalmOilFree;
    }
}
