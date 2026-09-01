using System;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace eu.foodmission.platform
{
    public static class ClientEventTypes
    {
        // App Session
        public const string AppSessionOpened = "APP_SESSION_OPENED";
        public const string AppSessionEnded = "APP_SESSION_ENDED";

        // Meal & Diet Patterns
        public const string MealMeatConsumed = "MEAL_MEAT_CONSUMED";
        public const string MealMeatFree = "MEAL_MEAT_FREE";
        public const string MealLegumeConsumed = "MEAL_LEGUME_CONSUMED";
        public const string MealVegan = "MEAL_VEGAN";
        public const string MealVegetarianDay = "MEAL_VEGETARIAN_DAY";
        public const string MealVeganDay = "MEAL_VEGAN_DAY";
        public const string MealAlternativeStaple = "MEAL_ALTERNATIVE_STAPLE";
        public const string MealAncientGrain = "MEAL_ANCIENT_GRAIN";
        public const string MealSustainablePlate = "MEAL_SUSTAINABLE_PLATE";

        // Substitutions & Swaps
        public const string SwapBeefToPork = "SWAP_BEEF_TO_PORK";
        public const string SwapBeefToChicken = "SWAP_BEEF_TO_CHICKEN";
        public const string SwapBeefToLegumes = "SWAP_BEEF_TO_LEGUMES";
        public const string SwapPorkToChicken = "SWAP_PORK_TO_CHICKEN";
        public const string SwapPorkToLegumes = "SWAP_PORK_TO_LEGUMES";
        public const string SwapChickenToLegumes = "SWAP_CHICKEN_TO_LEGUMES";
        public const string SwapSugaryDrinkToWater = "SWAP_SUGARY_DRINK_TO_WATER";
        public const string SwapSnackToFruitNuts = "SWAP_SNACK_TO_FRUIT_NUTS";
        public const string SwapSugaryCerealToOats = "SWAP_SUGARY_CEREAL_TO_OATS";
        public const string SwapReadyMealToHomecooked = "SWAP_READY_MEAL_TO_HOMECOOKED";
        public const string SwapProcessedMeatToLegumes = "SWAP_PROCESSED_MEAT_TO_LEGUMES";

        // Product Origin & Shopping
        public const string ShoppingOriginChecked = "SHOPPING_ORIGIN_CHECKED";
        public const string ShoppingLocalChosen = "SHOPPING_LOCAL_CHOSEN";
        public const string ShoppingSeasonalChosen = "SHOPPING_SEASONAL_CHOSEN";
        public const string ShoppingCertificationChosen = "SHOPPING_CERTIFICATION_CHOSEN";
        public const string ShoppingPackagingInfoChecked = "SHOPPING_PACKAGING_INFO_CHECKED";
        public const string ShoppingMulticriteriaPurchase = "SHOPPING_MULTICRITERIA_PURCHASE";

        // Food Processing & Scores
        public const string ProcessingNovaChecked = "PROCESSING_NOVA_CHECKED";
        public const string ProcessingIngredientsReviewed = "PROCESSING_INGREDIENTS_REVIEWED";
        public const string ProcessingGreenscoreChecked = "PROCESSING_GREENSCORE_CHECKED";
        public const string ProcessingIndicatorsCompared = "PROCESSING_INDICATORS_COMPARED";
        public const string ProcessingProductionMethodChecked = "PROCESSING_PRODUCTION_METHOD_CHECKED";

        // Packaging & Circularity
        public const string PackagingMaterialObserved = "PACKAGING_MATERIAL_OBSERVED";
        public const string PackagingRecyclingLabelRead = "PACKAGING_RECYCLING_LABEL_READ";
        public const string PackagingReusableSpotChosen = "PACKAGING_REUSABLE_SPOT_CHOSEN";
        public const string PackagingRecyclabilityEvaluated = "PACKAGING_RECYCLABILITY_EVALUATED";
        public const string PackagingComparisonMade = "PACKAGING_COMPARISON_MADE";
        public const string PackagingSmartObserved = "PACKAGING_SMART_OBSERVED";

        // Food Waste Prevention
        public const string FoodWasteHalfPlateSaved = "FOOD_WASTE_HALF_PLATE_SAVED";
        public const string FoodWasteFullPlateSaved = "FOOD_WASTE_FULL_PLATE_SAVED";
        public const string FoodWasteExpiredConsumed = "FOOD_WASTE_EXPIRED_CONSUMED";
        public const string FoodWasteStorageInstructionsRead = "FOOD_WASTE_STORAGE_INSTRUCTIONS_READ";
        public const string FoodWasteMealPlanned = "FOOD_WASTE_MEAL_PLANNED";
        public const string FoodWasteFridgePantryChecked = "FOOD_WASTE_FRIDGE_PANTRY_CHECKED";
        public const string FoodWasteFifoOrganized = "FOOD_WASTE_FIFO_ORGANIZED";
        public const string FoodWasteLogged = "FOOD_WASTE_LOGGED";

        // Nutrition & Health
        public const string NutritionProteinIncluded = "NUTRITION_PROTEIN_INCLUDED";
        public const string NutritionFruitVegServingAdded = "NUTRITION_FRUIT_VEG_SERVING_ADDED";
        public const string NutritionWholegrainChosen = "NUTRITION_WHOLEGRAIN_CHOSEN";
        public const string NutritionHighFibreMeal = "NUTRITION_HIGH_FIBRE_MEAL";
        public const string NutritionSaltFreeTable = "NUTRITION_SALT_FREE_TABLE";
        public const string NutritionHealthyFatChosen = "NUTRITION_HEALTHY_FAT_CHOSEN";
        public const string NutritionProteinVarietyLogged = "NUTRITION_PROTEIN_VARIETY_LOGGED";
        public const string NutritionRainbowColoursLogged = "NUTRITION_RAINBOW_COLOURS_LOGGED";
        public const string NutritionAddedSugarAvoided = "NUTRITION_ADDED_SUGAR_AVOIDED";
        public const string NutritionPlantDiversityCount = "NUTRITION_PLANT_DIVERSITY_COUNT";

        // Learning
        public const string LearningFootprintCompared = "LEARNING_FOOTPRINT_COMPARED";
        public const string LearningRecipeExplored = "LEARNING_RECIPE_EXPLORED";
        public const string LearningRecipeShared = "LEARNING_RECIPE_SHARED";

        public static readonly string[] All = {
            AppSessionOpened, AppSessionEnded,
            MealMeatConsumed, MealMeatFree, MealLegumeConsumed, MealVegan, MealVegetarianDay, MealVeganDay, MealAlternativeStaple, MealAncientGrain, MealSustainablePlate,
            SwapBeefToPork, SwapBeefToChicken, SwapBeefToLegumes, SwapPorkToChicken, SwapPorkToLegumes, SwapChickenToLegumes, SwapSugaryDrinkToWater, SwapSnackToFruitNuts, SwapSugaryCerealToOats, SwapReadyMealToHomecooked, SwapProcessedMeatToLegumes,
            ShoppingOriginChecked, ShoppingLocalChosen, ShoppingSeasonalChosen, ShoppingCertificationChosen, ShoppingPackagingInfoChecked, ShoppingMulticriteriaPurchase,
            ProcessingNovaChecked, ProcessingIngredientsReviewed, ProcessingGreenscoreChecked, ProcessingIndicatorsCompared, ProcessingProductionMethodChecked,
            PackagingMaterialObserved, PackagingRecyclingLabelRead, PackagingReusableSpotChosen, PackagingRecyclabilityEvaluated, PackagingComparisonMade, PackagingSmartObserved,
            FoodWasteHalfPlateSaved, FoodWasteFullPlateSaved, FoodWasteExpiredConsumed, FoodWasteStorageInstructionsRead, FoodWasteMealPlanned, FoodWasteFridgePantryChecked, FoodWasteFifoOrganized, FoodWasteLogged,
            NutritionProteinIncluded, NutritionFruitVegServingAdded, NutritionWholegrainChosen, NutritionHighFibreMeal, NutritionSaltFreeTable, NutritionHealthyFatChosen, NutritionProteinVarietyLogged, NutritionRainbowColoursLogged, NutritionAddedSugarAvoided, NutritionPlantDiversityCount,
            LearningFootprintCompared, LearningRecipeExplored, LearningRecipeShared
        };
    }

    [Serializable]
    public class ClientEventMetadata
    {
        public string sessionId;
        public string platform;
        public string appVersion;
        public int durationSeconds;
    }

    [Serializable]
    public class CreateClientEventRequest
    {
        public string eventType;
        public object metadata;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string idempotencyKey;

        public byte[] ToJsonBody()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return Encoding.UTF8.GetBytes(json);
        }
    }

    [Serializable]
    public class UserEvent
    {
        public string id;
        public string userId;
        public string eventType;
        public string source;
        public string timestamp;
        public string groupId;
    }
}
