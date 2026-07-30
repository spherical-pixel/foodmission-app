using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using eu.foodmission.platform;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class RecipeBookViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<IRecipeService> _mockRecipeService;
        private Mock<ICatalogService> _mockCatalogService;
        private TestLocalStorageService _localStorage;
        private RecipeBookViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _mockRecipeService = new Mock<IRecipeService>();
            _mockCatalogService = new Mock<ICatalogService>();
            _mockCatalogService.Setup(c => c.GetMealCategoriesAsync(It.IsAny<string>()))
                .ReturnsAsync((new CatalogItem[0], null));
            _mockCatalogService.Setup(c => c.GetMealCoursesAsync(It.IsAny<string>()))
                .ReturnsAsync((new CatalogItem[0], null));
            _localStorage = new TestLocalStorageService();
            _viewModel = new RecipeBookViewModel(_storeService, _mockRecipeService.Object, _mockCatalogService.Object, _localStorage);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        [Test]
        public async Task LoadAsync_OnSuccess_PopulatesRecipes()
        {
            _mockRecipeService.Setup(s => s.GetRecipesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new PaginatedRecipeResponse
                {
                    data = new[] { new Recipe { id = "r1", title = "Pasta" } },
                    total = 1,
                    page = 1,
                    limit = 20,
                    totalPages = 1
                }, null));
            _mockRecipeService.Setup(s => s.GetRecommendationsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new MultipleRecommendationResponse { data = new RecommendationResponse[0] }, null));

            await _viewModel.LoadAsync();

            Assert.AreEqual(1, _viewModel.Recipes.Count);
            Assert.AreEqual("Pasta", _viewModel.Recipes[0].Item.title);
            Assert.IsFalse(_viewModel.HasMore);
        }

        [Test]
        public async Task LoadAsync_OnError_FallsBackToCache()
        {
            var cached = new List<RecipeView> { new() { DisplayTitle = "Cached", Item = new Recipe { id = "c1" } } };
            _localStorage.SetValue("recipes_cache_", cached);

            _mockRecipeService.Setup(s => s.GetRecipesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((null, new ApiErrorResponse { message = "network" }));
            _mockRecipeService.Setup(s => s.GetRecommendationsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((null, new ApiErrorResponse { message = "network" }));

            await _viewModel.LoadAsync();

            Assert.AreEqual(1, _viewModel.Recipes.Count);
            Assert.AreEqual("Cached", _viewModel.Recipes[0].DisplayTitle);
            Assert.IsNotNull(_viewModel.ErrorDetail);
        }


    }
}
