using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class UserLocalDataTests
    {
        [Test]
        public void Clear_RemovesEveryCacheThatBelongsToTheSignedInUser()
        {
            // Another account signing in on the device must not see these (seen 2026-10-07 with food facts and pantry)
            var storage = new TestLocalStorageService();
            foreach (string key in new[] { "shoppinglists_cache", "meal_logs_cache", "pantry_cache", "recipes_cache_" })
            {
                storage.SetValue(key, new List<string> { "previous account" });
            }

            UserLocalData.Clear(storage);

            foreach (string key in new[] { "shoppinglists_cache", "meal_logs_cache", "pantry_cache", "recipes_cache_" })
            {
                Assert.IsNull(storage.GetValue<List<string>>(key), key);
            }
        }

        [Test]
        public void FoodWasteCacheKey_IsPerUser()
        {
            var month = new DateTime(2026, 10, 1);

            Assert.AreNotEqual(FoodWasteViewModel.CacheKeyFor("u1", month), FoodWasteViewModel.CacheKeyFor("u2", month));
        }
    }
}
