using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

using UnityEngine.UIElements;

using eu.foodmission.platform.Components;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FMSearchOrCategoryFieldPaginationTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private FMSearchOrCategoryField _field;

        [SetUp]
        public void SetUp()
        {
            FMSearchOrCategoryField.ClearSessionCache();
            _field = new FMSearchOrCategoryField();
            _field.SearchByFoodGroupAsync = (group, page, size) => Task.FromResult(new PaginatedGenericFoodResponse
            {
                items = new[] { new GenericFood { id = "1", foodName = "Bread" } },
                page = page,
                limit = size,
                totalPages = 3
            });
        }

        [TearDown]
        public void TearDown()
        {
            FMSearchOrCategoryField.ClearSessionCache();
        }

        private T GetField<T>(string name)
        {
            return (T)typeof(FMSearchOrCategoryField).GetField(name, Private).GetValue(_field);
        }

        private void SetField(string name, object value)
        {
            typeof(FMSearchOrCategoryField).GetField(name, Private).SetValue(_field, value);
        }

        private object Invoke(string name, params object[] args)
        {
            return typeof(FMSearchOrCategoryField).GetMethod(name, Private).Invoke(_field, args);
        }

        private DisplayStyle PaginationDisplay => GetField<VisualElement>("_paginationContainer").style.display.value;

        [Test]
        public void CategoryWithSeveralPages_ShowsPagination()
        {
            Invoke("ShowItemsForGroup", "Bread", "bread");

            Assert.AreEqual(DisplayStyle.Flex, PaginationDisplay);
        }

        [Test]
        public async Task BackToCategories_HidesPagination()
        {
            Invoke("ShowItemsForGroup", "Bread", "bread");

            await (Task)Invoke("ShowGenericFoodsAsync");

            Assert.AreEqual(DisplayStyle.None, PaginationDisplay);
        }

        [Test]
        public async Task SearchWithSeveralPages_ShowsPaginationAndRequestsThePage()
        {
            var requested = new List<(string query, int page, bool generic, bool products)>();
            _field.SearchFoodsAsync = (query, page, generic, products) =>
            {
                requested.Add((query, page, generic, products));
                return Task.FromResult(new FoodSearchPage
                {
                    Items = new List<object> { new GenericFood { id = "1", foodName = "Pan" } },
                    TotalPages = 4
                });
            };
            SetField("_searchQuery", "pan");

            Invoke("GoToSearchPage", 2);
            await Task.Yield();

            Assert.AreEqual(1, requested.Count);
            Assert.AreEqual(("pan", 2, true, false), requested[0]);
            Assert.AreEqual(4, GetField<int>("_searchTotalPages"));
            Assert.AreEqual(DisplayStyle.Flex, PaginationDisplay);
            Assert.AreEqual(1, GetField<VisualElement>("_searchResultsContainer").childCount);
        }

        [Test]
        public async Task SearchWithOnePage_HidesPaginationLeftFromCategory()
        {
            Invoke("ShowItemsForGroup", "Bread", "bread");
            _field.SearchFoodsAsync = (query, page, generic, products) =>
                Task.FromResult(new FoodSearchPage { Items = new List<object> { new GenericFood { id = "1", foodName = "Pan" } }, TotalPages = 1 });
            SetField("_searchQuery", "pan");

            Invoke("GoToSearchPage", 1);
            await Task.Yield();

            Assert.AreEqual(DisplayStyle.None, PaginationDisplay);
        }
    }
}
