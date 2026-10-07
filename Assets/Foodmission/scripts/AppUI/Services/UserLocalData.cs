namespace eu.foodmission.platform
{
    /// <summary>
    /// Device caches that belong to the signed-in user. Cleared on logout so the next account on the device never sees them.
    /// Caches keyed by something else per user (food waste months, shopping list ids) don't need to be listed here.
    /// </summary>
    public static class UserLocalData
    {
        private static readonly string[] Keys =
        {
            "shoppinglists_cache",
            "meal_logs_cache",
            "pantry_cache",
            "recipes_cache_"
        };

        public static void Clear(ILocalStorageService localStorage)
        {
            if (localStorage == null)
            {
                return;
            }

            foreach (string key in Keys)
            {
                localStorage.DeleteValue(key);
            }
        }
    }
}
