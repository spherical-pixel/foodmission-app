using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>UI.csv label of every meal-log flag. Shared by the check-in and Quick Meal Log.</summary>
    public static class MealFlagLabels
    {
        private static readonly Dictionary<string, string> s_Keys = new(StringComparer.Ordinal)
        {
            { ClientEventTypes.MealMeatConsumed, "EVENTS_MEAT_CONSUMED" },
            { ClientEventTypes.MealMeatFree, "EVENTS_MEAT_FREE" },
            { ClientEventTypes.MealVegan, "EVENTS_VEGAN_MEAL" },
            { ClientEventTypes.MealLegumeConsumed, "EVENTS_LEGUMES_CONSUMED" },
            { ClientEventTypes.MealAlternativeStaple, "EVENTS_ALTERNATIVE_STAPLE" },
            { ClientEventTypes.MealAncientGrain, "EVENTS_ANCIENT_GRAIN" },
            { ClientEventTypes.MealSustainablePlate, "EVENTS_SUSTAINABLE_PLATE" },
            { ClientEventTypes.MealSeasonalProduce, "EVENTS_SEASONAL_PRODUCE" },
            { ClientEventTypes.MealLocalProduce, "EVENTS_LOCAL_PRODUCE" },
            { ClientEventTypes.MealCertifiedProduct, "EVENTS_CERTIFIED_PRODUCT" },
            { ClientEventTypes.FoodWasteHalfPlateSaved, "EVENTS_HALF_PLATE_SAVED" },
            { ClientEventTypes.FoodWasteFullPlateSaved, "EVENTS_FULL_PLATE_SAVED" },
            { ClientEventTypes.FoodWasteExpiredConsumed, "EVENTS_EXPIRED_CONSUMED" },
            { ClientEventTypes.NutritionProteinIncluded, "EVENTS_PROTEIN_INCLUDED" },
            { ClientEventTypes.NutritionFruitVegServingAdded, "EVENTS_FRUIT_VEG_SERVING" },
            { ClientEventTypes.NutritionWholegrainChosen, "EVENTS_WHOLEGRAIN" },
            { ClientEventTypes.NutritionHighFibreMeal, "EVENTS_HIGH_FIBRE" },
            { ClientEventTypes.NutritionSaltFreeTable, "EVENTS_SALT_FREE" },
            { ClientEventTypes.NutritionHealthyFatChosen, "EVENTS_HEALTHY_FAT" },
            { ClientEventTypes.NutritionRainbowColoursLogged, "EVENTS_RAINBOW_COLOURS" },
            { ClientEventTypes.NutritionAddedSugarAvoided, "EVENTS_ADDED_SUGAR_AVOIDED" },
        };

        public static IReadOnlyCollection<string> Flags => s_Keys.Keys;

        public static string KeyFor(string eventType)
        {
            if (string.IsNullOrEmpty(eventType))
            {
                return null;
            }
            if (eventType.StartsWith("SWAP_", StringComparison.Ordinal))
            {
                return eventType;
            }
            return s_Keys.TryGetValue(eventType, out string key) ? key : null;
        }
    }
}
