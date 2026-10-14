using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>What a meal mission asks, shown under the check-in's meals when its flag labels don't say it.</summary>
    public static class MealStepHints
    {
        // Keyed by the meal step's prompt, not the flag: M.I6.2 and M.A6.4 share the fibre flag but ask different things
        private static readonly Dictionary<string, string> s_Keys = new(StringComparer.Ordinal)
        {
            { "MISSION_Q_" + ClientEventTypes.FoodWasteHalfPlateSaved, "MISSION_HINT_HALF_PLATE_SAVED" },
            { "MISSION_Q_" + ClientEventTypes.NutritionHighFibreMeal, "MISSION_HINT_HIGH_FIBRE_MEAL" },
            { "MISSION_Q_NUTRIENT_DENSE_MEALS", "MISSION_HINT_NUTRIENT_DENSE_MEALS" },
        };

        /// <summary>UI.csv key of the hint, null when the step isn't a meal step or needs none.</summary>
        public static string KeyFor(MissionReportStep step)
        {
            if (step == null || step.Type != MissionStepType.MealReport || step.PromptKey == null)
            {
                return null;
            }
            return s_Keys.TryGetValue(step.PromptKey, out string key) ? key : null;
        }
    }
}
