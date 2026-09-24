using System.Collections.Generic;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    public static class MealLogHelpers
    {
        private static readonly Dictionary<string, string> TypeEmojis = new()
        {
            { "BREAKFAST", "🌅" },
            { "LUNCH", "☀️" },
            { "DINNER", "🌙" },
            { "SNACK", "🍿" },
            { "DRINKS", "🥤" },
            { "OTHER", "🍽️" },
        };

        public static string GetEmojiForTypeOfMeal(string type)
        {
            return TypeEmojis.TryGetValue(type, out string emoji) ? emoji : "🍽️";
        }

        public static string GetFlagLocalizationKey(string flag) => flag switch
        {
            ClientEventTypes.MealMeatFree => "EVENTS_MEAT_FREE",
            ClientEventTypes.MealLegumeConsumed => "EVENTS_LEGUMES_CONSUMED",
            ClientEventTypes.MealVegan => "EVENTS_VEGAN_MEAL",
            ClientEventTypes.MealSustainablePlate => "EVENTS_SUSTAINABLE_PLATE",
            ClientEventTypes.MealAncientGrain => "EVENTS_ANCIENT_GRAIN",
            ClientEventTypes.MealAlternativeStaple => "EVENTS_ALTERNATIVE_STAPLE",
            ClientEventTypes.MealMeatConsumed => "EVENTS_MEAT_CONSUMED",
            ClientEventTypes.NutritionFruitVegServingAdded => "EVENTS_FRUIT_VEG_SERVING",
            ClientEventTypes.NutritionWholegrainChosen => "EVENTS_WHOLEGRAIN",
            ClientEventTypes.NutritionHighFibreMeal => "EVENTS_HIGH_FIBRE",
            ClientEventTypes.NutritionSaltFreeTable => "EVENTS_SALT_FREE",
            ClientEventTypes.NutritionHealthyFatChosen => "EVENTS_HEALTHY_FAT",
            ClientEventTypes.NutritionAddedSugarAvoided => "EVENTS_ADDED_SUGAR_AVOIDED",
            _ => null
        };

        public static string GetFlagEmoji(string flag) => flag switch
        {
            ClientEventTypes.MealMeatFree => "🥗",
            ClientEventTypes.MealLegumeConsumed => "🫘",
            ClientEventTypes.MealVegan => "🌿",
            ClientEventTypes.MealSustainablePlate => "🍽️",
            ClientEventTypes.MealAncientGrain => "🌾",
            ClientEventTypes.MealAlternativeStaple => "🥔",
            ClientEventTypes.MealMeatConsumed => "🥩",
            ClientEventTypes.NutritionFruitVegServingAdded => "🥦",
            ClientEventTypes.NutritionWholegrainChosen => "🍞",
            ClientEventTypes.NutritionHighFibreMeal => "🌾",
            ClientEventTypes.NutritionSaltFreeTable => "🧂",
            ClientEventTypes.NutritionHealthyFatChosen => "🥑",
            ClientEventTypes.NutritionAddedSugarAvoided => "🍬",
            _ => ""
        };

        public static string GetFlagLocalizationTag(string flag)
        {
            string key = GetFlagLocalizationKey(flag);
            return !string.IsNullOrEmpty(key) ? $"@UI:{key}" : null;
        }

        public static string GetDisplayNameForFlag(string flag)
        {
            string key = GetFlagLocalizationKey(flag);
            string emoji = GetFlagEmoji(flag);

            if (!string.IsNullOrEmpty(key))
            {
                string localized = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
                return string.IsNullOrEmpty(emoji) ? localized : $"{emoji} {localized}";
            }

            return flag;
        }



        public static bool IsQuickMeal(MealLog log)
        {
            if (log == null) return false;
            bool hasFlagsOrSwaps = (log.flags != null && log.flags.Length > 0) ||
                                   (log.swaps != null && log.swaps.Length > 0);
            bool hasNoMealItems = log.meal == null || log.meal.items == null || log.meal.items.Length == 0;
            return hasFlagsOrSwaps || (hasNoMealItems && string.IsNullOrEmpty(log.mealId));
        }
    }
}
