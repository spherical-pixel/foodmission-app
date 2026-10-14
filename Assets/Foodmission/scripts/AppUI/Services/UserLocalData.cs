using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Device caches that belong to the signed-in user. Cleared on logout so the next account on the device never sees them.
    /// Caches keyed by something else per user (food waste months, shopping list ids) don't need to be listed here.
    /// Caches whose key is built at runtime (e.g. per language) must be registered with <see cref="Track"/> when written.
    /// </summary>
    public static class UserLocalData
    {
        private const string TrackedKeysKey = "user_cache_keys";

        private static readonly string[] Keys =
        {
            "shoppinglists_cache",
            "pantry_cache",
            "recipes_cache_",
            "recipes_cache__rec",
            "recipes_cache__mine"
        };

        // Meal log caches written before keys were tracked (one per app language)
        private static readonly string[] LegacyMealLogLangs = { "nl", "en", "de", "el", "it", "no", "pl", "sl", "es", "ca", "none" };

        public static string MealLogsCacheKey(string lang) => $"meal_logs_cache_{lang}";

        /// <summary>Registers a runtime-built cache key so <see cref="Clear"/> removes it on logout.</summary>
        public static void Track(ILocalStorageService localStorage, string key)
        {
            if (localStorage == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            string[] tracked = localStorage.GetValue<string[]>(TrackedKeysKey) ?? new string[0];
            if (tracked.Contains(key))
            {
                return;
            }

            localStorage.SetValue(TrackedKeysKey, tracked.Append(key).ToArray());
        }

        public static void Clear(ILocalStorageService localStorage)
        {
            if (localStorage == null)
            {
                return;
            }

            var keys = new List<string>(Keys);
            keys.AddRange(LegacyMealLogLangs.Select(MealLogsCacheKey));
            keys.AddRange(localStorage.GetValue<string[]>(TrackedKeysKey) ?? new string[0]);

            foreach (string key in keys.Distinct())
            {
                localStorage.DeleteValue(key);
            }

            localStorage.DeleteValue(TrackedKeysKey);
        }
    }
}
