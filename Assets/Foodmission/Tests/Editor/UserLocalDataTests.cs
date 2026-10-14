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
            var keys = new[]
            {
                "shoppinglists_cache", "pantry_cache",
                "recipes_cache_", "recipes_cache__rec", "recipes_cache__mine"
            };
            var storage = new TestLocalStorageService();
            foreach (string key in keys)
            {
                storage.SetValue(key, new List<string> { "previous account" });
            }

            UserLocalData.Clear(storage);

            foreach (string key in keys)
            {
                Assert.IsNull(storage.GetValue<List<string>>(key), key);
            }
        }

        [Test]
        public void Clear_RemovesMealLogCachesOfEveryLanguage()
        {
            // The meal log cache is per language; only clearing the base key leaked the previous user's logs
            var storage = new TestLocalStorageService();
            foreach (string lang in new[] { "en", "es", "nl", "none" })
            {
                storage.SetValue(UserLocalData.MealLogsCacheKey(lang), new List<string> { "previous account" });
            }

            UserLocalData.Clear(storage);

            foreach (string lang in new[] { "en", "es", "nl", "none" })
            {
                Assert.IsNull(storage.GetValue<List<string>>(UserLocalData.MealLogsCacheKey(lang)), lang);
            }
        }

        [Test]
        public void Clear_RemovesTrackedKeys()
        {
            var storage = new TestLocalStorageService();
            storage.SetValue("some_runtime_cache_fr", new List<string> { "previous account" });
            UserLocalData.Track(storage, "some_runtime_cache_fr");

            UserLocalData.Clear(storage);

            Assert.IsNull(storage.GetValue<List<string>>("some_runtime_cache_fr"));
        }

        [Test]
        public void Track_SameKeyTwice_StoresItOnce()
        {
            var storage = new TestLocalStorageService();

            UserLocalData.Track(storage, "k");
            UserLocalData.Track(storage, "k");

            CollectionAssert.AreEqual(new[] { "k" }, storage.GetValue<string[]>("user_cache_keys"));
        }

        [Test]
        public void FoodWasteCacheKey_IsPerUser()
        {
            var month = new DateTime(2026, 10, 1);

            Assert.AreNotEqual(FoodWasteViewModel.CacheKeyFor("u1", month), FoodWasteViewModel.CacheKeyFor("u2", month));
        }
    }
}
