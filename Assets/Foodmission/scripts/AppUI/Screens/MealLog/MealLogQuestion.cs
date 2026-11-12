using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Question shown above the food search for each type of meal. Each type has its own sentence because the
    /// backend label doesn't fit one template ("What did you have for drinks?", "para el cena", "na kolacja").
    /// </summary>
    public static class MealLogQuestion
    {
        private static readonly Dictionary<string, string> s_Keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "BREAKFAST", "MEAL_LOG_Q_BREAKFAST" },
            { "LUNCH", "MEAL_LOG_Q_LUNCH" },
            { "DINNER", "MEAL_LOG_Q_DINNER" },
            { "SNACK", "MEAL_LOG_Q_SNACK" },
            { "DRINKS", "MEAL_LOG_Q_DRINKS" },
            { "OTHER", "MEAL_LOG_Q_OTHER" }
        };

        /// <summary>Localization key for the type of meal, or null for a type the app doesn't know.</summary>
        public static string KeyFor(string typeOfMealCode)
        {
            if (string.IsNullOrEmpty(typeOfMealCode))
            {
                return null;
            }
            return s_Keys.TryGetValue(typeOfMealCode, out string key) ? key : null;
        }
    }
}
