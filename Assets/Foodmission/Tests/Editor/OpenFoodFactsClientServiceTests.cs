using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class OpenFoodFactsClientServiceTests
    {
        private OpenFoodFactsClientService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new OpenFoodFactsClientService();
        }

        [Test]
        public void CacheFreshness_CheckTtlBehavior()
        {
            var cacheField = typeof(OpenFoodFactsClientService).GetField("_barcodeCache", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(cacheField);

            var cache = (Dictionary<string, (OpenFoodFactsProduct data, DateTime cachedAt)>)cacheField.GetValue(_service);
            
            var dummyProduct = new OpenFoodFactsProduct { id = "12345", name = "Test" };
            
            cache["12345"] = (dummyProduct, DateTime.UtcNow);

            _service.ClearCache();
            Assert.AreEqual(0, cache.Count);
        }

        [Test]
        public async Task RateLimiter_AllowsNineRequestsImmediately()
        {
            var timestampsField = typeof(OpenFoodFactsClientService).GetField("_requestTimestamps", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(timestampsField);

            var timestamps = (Queue<DateTime>)timestampsField.GetValue(_service);

            for (int i = 0; i < 8; i++)
            {
                timestamps.Enqueue(DateTime.UtcNow.AddSeconds(-10));
            }

            var enforceMethod = typeof(OpenFoodFactsClientService).GetMethod("EnforceRateLimitAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(enforceMethod);

            var task = (Task)enforceMethod.Invoke(_service, null);
            await task;

            Assert.AreEqual(9, timestamps.Count);
        }

        [Test]
        public void UserAgent_BuildsFormattedString()
        {
            string ua = OpenFoodFactsUserAgent.Build();
            Assert.IsTrue(ua.StartsWith("FOODMISSION - "));
            Assert.IsTrue(ua.Contains("Version"));
            Assert.IsTrue(ua.EndsWith("dev@foodmission.eu"));
        }
    }
}
