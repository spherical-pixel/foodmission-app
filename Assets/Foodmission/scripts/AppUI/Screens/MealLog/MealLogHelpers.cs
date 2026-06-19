using System.Collections.Generic;

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

    }
}
