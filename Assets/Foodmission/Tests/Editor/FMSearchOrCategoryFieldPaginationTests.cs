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

        private List<string> RenderedCategoryTitles()
        {
            var titles = new List<string>();
            foreach (VisualElement child in GetField<VisualElement>("_categoryContainer").Children())
            {
                if (child is Unity.AppUI.UI.Button button)
                {
                    titles.Add(button.title);
                }
            }
            return titles;
        }

        [Test]
        public void OrderForDisplay_KeepsBackendOrderAndMovesAlcoholicBeveragesLast()
        {
            var groups = new List<FoodGroupItem>
            {
                new FoodGroupItem { slug = "vegetables", name = "Verduras" },
                new FoodGroupItem { slug = "alcoholic-beverages", name = "Bebidas alcohólicas" },
                new FoodGroupItem { slug = "bread", name = "Pan" },
                new FoodGroupItem { slug = "non-alcoholic-beverages", name = "Bebidas sin alcohol" }
            };

            List<FoodGroupItem> ordered = FMSearchOrCategoryField.OrderForDisplay(groups);

            CollectionAssert.AreEqual(
                new[] { "vegetables", "bread", "non-alcoholic-beverages", "alcoholic-beverages" },
                ordered.ConvertAll(g => g.slug));
        }

        [Test]
        public void OrderForDisplay_WithoutAlcoholicBeverages_KeepsOrder()
        {
            var groups = new List<FoodGroupItem>
            {
                new FoodGroupItem { slug = "soups", name = "Soups" },
                new FoodGroupItem { slug = "bread", name = "Bread" }
            };

            List<FoodGroupItem> ordered = FMSearchOrCategoryField.OrderForDisplay(groups);

            CollectionAssert.AreEqual(new[] { "soups", "bread" }, ordered.ConvertAll(g => g.slug));
        }

        [Test]
        public async Task ShowCategories_RendersBackendGroupsWithAlcoholicBeveragesLast()
        {
            _field.GetFoodGroupsAsync = () => Task.FromResult(new List<FoodGroupItem>
            {
                new FoodGroupItem { slug = "alcoholic-beverages", name = "Alcoholic beverages" },
                new FoodGroupItem { slug = "soups", name = "Soups" },
                new FoodGroupItem { slug = "bread", name = "Bread" }
            });

            await (Task)Invoke("ShowGenericFoodsAsync");

            List<string> titles = RenderedCategoryTitles();
            Assert.AreEqual(3, titles.Count);
            StringAssert.EndsWith("Soups", titles[0]);
            StringAssert.EndsWith("Bread", titles[1]);
            StringAssert.EndsWith("Alcoholic beverages", titles[2]);
        }

        [Test]
        public async Task ShowCategories_WithoutBackendGroups_ShowsDefaultsWithAlcoholicBeveragesLast()
        {
            await (Task)Invoke("ShowGenericFoodsAsync");

            List<string> titles = RenderedCategoryTitles();
            StringAssert.EndsWith("Bread", titles[0]);
            StringAssert.EndsWith("Alcoholic beverages", titles[titles.Count - 1]);
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
                    TotalPages = 4,
                    HasNextPage = true
                });
            };
            SetField("_searchQuery", "pan");

            Invoke("GoToSearchPage", 2);
            await Task.Yield();

            Assert.AreEqual(1, requested.Count);
            Assert.AreEqual(("pan", 2, true, false), requested[0]);
            Assert.IsTrue(GetField<bool>("_searchHasPages"));
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

        [Test]
        public async Task SearchWithUnknownTotal_ShowsPaginationWithNextEnabled()
        {
            _field.SearchFoodsAsync = (query, page, generic, products) =>
                Task.FromResult(new FoodSearchPage
                {
                    Items = new List<object> { new OpenFoodFactsProduct { id = "1", name = "Pan" } },
                    TotalPages = 0,
                    HasNextPage = true
                });
            SetField("_searchQuery", "pan");

            Invoke("GoToSearchPage", 1);
            await Task.Yield();

            var pagination = GetField<VisualElement>("_paginationContainer");
            Assert.AreEqual(DisplayStyle.Flex, PaginationDisplay);
            Assert.IsFalse(pagination[0].enabledSelf, "Previous on page 1");
            Assert.IsTrue(pagination[2].enabledSelf, "Next while more results exist");
        }

        [Test]
        public async Task SearchLastPageWithUnknownTotal_DisablesNext()
        {
            _field.SearchFoodsAsync = (query, page, generic, products) =>
                Task.FromResult(new FoodSearchPage
                {
                    Items = new List<object> { new OpenFoodFactsProduct { id = "1", name = "Pan" } },
                    TotalPages = 0,
                    HasNextPage = false
                });
            SetField("_searchQuery", "pan");

            Invoke("GoToSearchPage", 3);
            await Task.Yield();

            var pagination = GetField<VisualElement>("_paginationContainer");
            Assert.AreEqual(DisplayStyle.Flex, PaginationDisplay);
            Assert.IsTrue(pagination[0].enabledSelf);
            Assert.IsFalse(pagination[2].enabledSelf);
        }
    }
}
