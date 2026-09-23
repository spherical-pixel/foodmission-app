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

        public static string GetDisplayNameForFlag(string flag)
        {
            return flag switch
            {
                ClientEventTypes.MealMeatConsumed => "🥩 Con carne",
                ClientEventTypes.MealMeatFree => "🌱 Sin carne (vegetariana)",
                ClientEventTypes.MealVegan => "🥗 100% vegetal (vegana)",
                ClientEventTypes.MealLegumeConsumed => "🫘 Legumbres",
                ClientEventTypes.MealAlternativeStaple => "🌾 Cereal alternativo",
                ClientEventTypes.MealAncientGrain => "🌾 Grano ancestral",
                ClientEventTypes.MealSustainablePlate => "🍽️ Plato sostenible",
                _ => flag
            };
        }
    }
}
