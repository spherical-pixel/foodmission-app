using System;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    public static class FoodFactLevel
    {
        public const string Beginner = "BEGINNER";
        public const string Intermediate = "INTERMEDIATE";
        public const string Advanced = "ADVANCED";

        public static readonly string[] All = { Beginner, Intermediate, Advanced };
    }

    [Serializable]
    public class FoodFact
    {
        public string id;
        public string code;
        public string topicId;
        public string body;
        public string source;
        public string level;
        public bool health;
        public bool foodChoice;
        public bool foodWaste;
        public bool available;
    }

    [Serializable]
    public class PaginatedFoodFactResponse
    {
        public FoodFact[] data;
        public PaginationMeta meta;
    }

    public class FoodFactFilterParams
    {
        public string dimensionCode;
        public string topicCode;
        public string level;
        public bool? health;
        public bool? foodChoice;
        public bool? foodWaste;
        public string search;
    }
}
