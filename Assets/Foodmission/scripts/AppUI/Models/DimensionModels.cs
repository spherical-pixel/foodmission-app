using System;

namespace eu.foodmission.platform
{
    [Serializable]
    public class Topic
    {
        public string id;
        public string code;
        public string name;
        public string dimensionId;
        public int sortOrder;
    }

    [Serializable]
    public class Dimension
    {
        public string id;
        public string code;
        public string name;
        public int sortOrder;
        public Topic[] topics;
    }

    public static class DimensionCode
    {
        public const string DietChanges = "DIET_CHANGES";
        public const string ProductChoices = "PRODUCT_CHOICES";
        public const string ProductionMethods = "PRODUCTION_METHODS";
        public const string Packaging = "PACKAGING";
        public const string FoodWaste = "FOOD_WASTE";
        public const string NutritionValues = "NUTRITION_VALUES";

        public static readonly string[] All =
        {
            DietChanges,
            ProductChoices,
            ProductionMethods,
            Packaging,
            FoodWaste,
            NutritionValues
        };
    }

    public static class TopicCode
    {
        // DIET_CHANGES
        public const string ReducingMeatConsumption = "REDUCING_MEAT_CONSUMPTION";
        public const string IncreasingOtherProteinSources = "INCREASING_OTHER_PROTEIN_SOURCES";
        public const string AlternativeStapleFoods = "ALTERNATIVE_STAPLE_FOODS";

        // PRODUCT_CHOICES
        public const string LandUse = "LAND_USE";
        public const string WaterUse = "WATER_USE";
        public const string EnergyConsumption = "ENERGY_CONSUMPTION";
        public const string CarbonFootprint = "CARBON_FOOTPRINT";
        public const string TravelDistances = "TRAVEL_DISTANCES";

        // PRODUCTION_METHODS
        public const string LevelOfProcessing = "LEVEL_OF_PROCESSING";
        public const string CountryOfOrigin = "COUNTRY_OF_ORIGIN";
        public const string FarmingProductionMethods = "FARMING_PRODUCTION_METHODS";
        public const string BreedingMethods = "BREEDING_METHODS";
        public const string FairTradeLabour = "FAIR_TRADE_LABOUR";

        // PACKAGING
        public const string SustainabilityOfPackagingMaterials = "SUSTAINABILITY_OF_PACKAGING_MATERIALS";
        public const string CapacityToReusePackaging = "CAPACITY_TO_REUSE_PACKAGING";

        // FOOD_WASTE
        public const string PlateWaste = "PLATE_WASTE";
        public const string LeftoversWaste = "LEFTOVERS_WASTE";
        public const string ExpiredFood = "EXPIRED_FOOD";
        public const string Overconsumption = "OVERCONSUMPTION";

        // NUTRITION_VALUES
        public const string Protein = "PROTEIN";
        public const string Fat = "FAT";
        public const string Sugar = "SUGAR";
        public const string Salt = "SALT";
        public const string Fiber = "FIBER";
        public const string Vitamins = "VITAMINS";
        public const string EnergyValueCalories = "ENERGY_VALUE_CALORIES";

        public static readonly string[] All =
        {
            ReducingMeatConsumption,
            IncreasingOtherProteinSources,
            AlternativeStapleFoods,
            LandUse,
            WaterUse,
            EnergyConsumption,
            CarbonFootprint,
            TravelDistances,
            LevelOfProcessing,
            CountryOfOrigin,
            FarmingProductionMethods,
            BreedingMethods,
            FairTradeLabour,
            SustainabilityOfPackagingMaterials,
            CapacityToReusePackaging,
            PlateWaste,
            LeftoversWaste,
            ExpiredFood,
            Overconsumption,
            Protein,
            Fat,
            Sugar,
            Salt,
            Fiber,
            Vitamins,
            EnergyValueCalories
        };
    }
}
