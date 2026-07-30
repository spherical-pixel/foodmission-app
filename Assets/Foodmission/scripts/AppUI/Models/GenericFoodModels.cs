using System;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class FoodGroupItem
    {
        public string slug;
        public string name;
    }

    [Serializable]
    public class GenericFood
    {
        public string id;
        public string foodName;
        public string foodGroup;
        public string foodGroupSlug;
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

    [Serializable]
    public class GenericFoodDetail
    {
        public string id;
        public string foodName;
        public string foodGroup;
        public string foodGroupSlug;
        public string synonym;
        public string quantity;
        public string containsTracesOf;
        public string isFortifiedWith;

        public float? energyKj;
        public float? energyKcal;
        public float? water;

        public float? proteins;
        public float? proteinsPlant;
        public float? proteinsAnimal;
        public float? carbohydrates;
        public float? sugars;
        public float? addedSugars;
        public float? starch;
        public float? fiber;

        public float? fat;
        public float? saturatedFat;
        public float? monoUnsaturatedFat;
        public float? polyUnsaturatedFat;
        public float? omega3Fat;
        public float? omega6Fat;
        public float? transFat;

        public float? sodium;
        public float? potassium;
        public float? calcium;
        public float? phosphorus;
        public float? magnesium;
        public float? iron;
        public float? zinc;

        public float? vitaminARae;
        public float? vitaminD;
        public float? vitaminE;
        public float? vitaminK;
        public float? vitaminC;
        public float? thiamin;
        public float? riboflavin;
        public float? vitaminB6;
        public float? vitaminB12;
        public float? folateTotal;
    }
}
