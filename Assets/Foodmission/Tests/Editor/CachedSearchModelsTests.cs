using System;

using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CachedSearchModelsTests
    {
        [Test]
        public void CachedFoodSearch_Roundtrips_Via_JsonUtility_WithProducts()
        {
            var cached = new CachedFoodSearch
            {
                data = new OpenFoodFactsSearchResponse
                {
                    products = new[]
                    {
                        new OpenFoodFactsProduct
                        {
                            id = "1",
                            barcode = "5449000000996",
                            name = "Coca-Cola",
                            brands = new[] { "Coca-Cola" },
                            quantity = "330ml",
                            imageFrontUrl = "https://example.com/img.jpg"
                        },
                        new OpenFoodFactsProduct
                        {
                            id = "2",
                            barcode = "4902430303039",
                            name = "Green Tea",
                            brands = new[] { "Lipton" },
                            quantity = "500ml"
                        }
                    },
                    totalCount = 2,
                    page = "1",
                    pageSize = 20,
                    totalPages = 1
                },
                cachedAtTicks = DateTime.UtcNow.Ticks
            };

            string json = JsonUtility.ToJson(cached);
            var restored = JsonUtility.FromJson<CachedFoodSearch>(json);

            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.data);
            Assert.IsNotNull(restored.data.products);
            Assert.AreEqual(2, restored.data.products.Length);
            Assert.AreEqual("Coca-Cola", restored.data.products[0].name);
            Assert.AreEqual("5449000000996", restored.data.products[0].barcode);
            Assert.AreEqual("Lipton", restored.data.products[1].brands[0]);
            Assert.AreEqual(2, restored.data.totalCount);
            Assert.AreEqual("1", restored.data.page);
        }

        [Test]
        public void CachedFoodSearch_CachedAtTicks_Roundtrips()
        {
            long ticks = new DateTime(2026, 5, 8, 12, 0, 0, DateTimeKind.Utc).Ticks;

            var cached = new CachedFoodSearch
            {
                data = new OpenFoodFactsSearchResponse
                {
                    products = new[] { new OpenFoodFactsProduct { id = "1", name = "Test" } },
                    totalCount = 1,
                    page = "1",
                    pageSize = 20,
                    totalPages = 1
                },
                cachedAtTicks = ticks
            };

            string json = JsonUtility.ToJson(cached);
            var restored = JsonUtility.FromJson<CachedFoodSearch>(json);

            Assert.AreEqual(ticks, restored.cachedAtTicks);
        }

        [Test]
        public void CachedGenericFoodSearch_Roundtrips_Via_JsonUtility_WithItems()
        {
            var cached = new CachedGenericFoodSearch
            {
                data = new PaginatedGenericFoodResponse
                {
                    items = new[]
                    {
                        new GenericFood { id = "cat-1", foodName = "Whole milk", foodGroup = "Dairy" },
                        new GenericFood { id = "cat-2", foodName = "Chicken breast", foodGroup = "Meat" }
                    },
                    total = 2,
                    page = 1,
                    limit = 20,
                    totalPages = 1
                },
                cachedAtTicks = DateTime.UtcNow.Ticks
            };

            string json = JsonUtility.ToJson(cached);
            var restored = JsonUtility.FromJson<CachedGenericFoodSearch>(json);

            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.data);
            Assert.IsNotNull(restored.data.items);
            Assert.AreEqual(2, restored.data.items.Length);
            Assert.AreEqual("Whole milk", restored.data.items[0].foodName);
            Assert.AreEqual("Meat", restored.data.items[1].foodGroup);
            Assert.AreEqual(2, restored.data.total);
        }

        [Test]
        public void CachedFoodSearch_WhenNoProducts_Roundtrips()
        {
            var cached = new CachedFoodSearch
            {
                data = new OpenFoodFactsSearchResponse
                {
                    products = Array.Empty<OpenFoodFactsProduct>(),
                    totalCount = 0,
                    page = "1"
                },
                cachedAtTicks = 0
            };

            string json = JsonUtility.ToJson(cached);
            var restored = JsonUtility.FromJson<CachedFoodSearch>(json);

            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.data);
            Assert.IsNotNull(restored.data.products);
            Assert.AreEqual(0, restored.data.products.Length);
            Assert.AreEqual(0, restored.cachedAtTicks);
        }

        [Test]
        public void CachedGenericFoodSearch_WhenNoData_Roundtrips()
        {
            var cached = new CachedGenericFoodSearch
            {
                data = new PaginatedGenericFoodResponse
                {
                    items = Array.Empty<GenericFood>(),
                    total = 0
                },
                cachedAtTicks = 0
            };

            string json = JsonUtility.ToJson(cached);
            var restored = JsonUtility.FromJson<CachedGenericFoodSearch>(json);

            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.data);
            Assert.IsNotNull(restored.data.items);
            Assert.AreEqual(0, restored.data.items.Length);
            Assert.AreEqual(0, restored.cachedAtTicks);
        }
    }
}
