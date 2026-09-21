using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using eu.foodmission.platform;
using Unity.AppUI.Navigation.Generated;
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
        public async Task LoadAsync_ForYou_WhenPantryEmpty_SetsIsPantryEmptyTrue()
        {
            _mockRecipeService.Setup(s => s.GetRecommendationsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new MultipleRecommendationResponse
                {
                    data = new RecommendationResponse[0],
                    totalPantryItems = 0,
                    expiringItemsCount = 0
                }, null));

            await _viewModel.LoadAsync();

            Assert.IsTrue(_viewModel.IsPantryEmpty);
            Assert.AreEqual(0, _viewModel.Recommendations.Count);
            Assert.AreEqual(0, _viewModel.TotalPantryItems);
        }

        [Test]
        public async Task LoadAsync_ForYou_WhenRecommendationsReturned_PopulatesRecommendations()
        {
            _mockRecipeService.Setup(s => s.GetRecommendationsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new MultipleRecommendationResponse
                {
                    data = new[]
                    {
                        new RecommendationResponse
                        {
                            recipe = new Recipe { id = "rec-1", title = "Pantry Pasta" },
                            matchCount = 3,
                            totalIngredients = 5,
                            expiringMatchCount = 1,
                            matchedIngredients = new[]
                            {
                                new MatchedIngredient { ingredientName = "Tomato", isExpiringSoon = true }
                            }
                        }
                    },
                    totalPantryItems = 4,
                    expiringItemsCount = 1
                }, null));

            await _viewModel.LoadAsync();

            Assert.IsFalse(_viewModel.IsPantryEmpty);
            Assert.AreEqual(1, _viewModel.Recommendations.Count);
            Assert.AreEqual("Pantry Pasta", _viewModel.Recommendations[0].DisplayTitle);
            Assert.AreEqual(1, _viewModel.Recommendations[0].ExpiringMatchCount);
            Assert.AreEqual(1, _viewModel.ExpiringItemsCount);
            Assert.AreEqual(4, _viewModel.TotalPantryItems);
        }

        [Test]
        public async Task LoadAsync_Explore_OnSuccess_PopulatesRecipes()
        {
            await _viewModel.SetTabAsync(RecipeBookTab.Explore);

            _mockRecipeService.Setup(s => s.GetRecipesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string[]>(),
                It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new PaginatedRecipeResponse
                {
                    data = new[] { new Recipe { id = "r1", title = "Pasta" } },
                    total = 1,
                    page = 1,
                    limit = 20,
                    totalPages = 1
                }, null));

            await _viewModel.LoadAsync();

            Assert.AreEqual(1, _viewModel.Recipes.Count);
            Assert.AreEqual("Pasta", _viewModel.Recipes[0].Item.title);
            Assert.IsFalse(_viewModel.HasMore);
        }

        [Test]
        public async Task LoadAsync_Explore_OnError_FallsBackToCache()
        {
            await _viewModel.SetTabAsync(RecipeBookTab.Explore);

            var cached = new List<RecipeView> { new() { DisplayTitle = "Cached", Item = new Recipe { id = "c1" } } };
            _localStorage.SetValue("recipes_cache_all_all_", cached);

            _mockRecipeService.Setup(s => s.GetRecipesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string[]>(),
                It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((null, new ApiErrorResponse { message = "network" }));

            await _viewModel.LoadAsync();

            Assert.AreEqual(1, _viewModel.Recipes.Count);
            Assert.AreEqual("Cached", _viewModel.Recipes[0].DisplayTitle);
            Assert.IsNotNull(_viewModel.ErrorDetail);
        }

        [Test]
        public async Task SetTabAsync_ChangesCurrentTabAndLoadsData()
        {
            _mockRecipeService.Setup(s => s.GetMyRecipesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new PaginatedRecipeResponse
                {
                    data = new[] { new Recipe { id = "my-1", title = "My Secret Recipe" } },
                    total = 1,
                    page = 1,
                    limit = 20,
                    totalPages = 1
                }, null));

            await _viewModel.SetTabAsync(RecipeBookTab.MyRecipes);

            Assert.AreEqual(RecipeBookTab.MyRecipes, _viewModel.CurrentTab);
            Assert.AreEqual(1, _viewModel.MyRecipes.Count);
            Assert.AreEqual("My Secret Recipe", _viewModel.MyRecipes[0].DisplayTitle);
        }

        [Test]
        public async Task SetDifficultyAsync_FiltersAndReloadsExplore()
        {
            await _viewModel.SetTabAsync(RecipeBookTab.Explore);

            string requestedDifficulty = null;
            _mockRecipeService.Setup(s => s.GetRecipesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string[]>(),
                It.IsAny<int>(), It.IsAny<int>()))
                .Callback<string, string, string, string, string[], string[], int, int>(
                    (s, cat, cui, diff, dl, t, p, l) => requestedDifficulty = diff)
                .ReturnsAsync((new PaginatedRecipeResponse { data = new Recipe[0], total = 0 }, null));

            await _viewModel.SetDifficultyAsync("easy");

            Assert.AreEqual("easy", _viewModel.SelectedDifficulty);
            Assert.AreEqual("easy", requestedDifficulty);
        }

        [Test]
        public async Task SetCategoryAsync_FiltersAndReloadsExplore()
        {
            await _viewModel.SetTabAsync(RecipeBookTab.Explore);

            string requestedCategory = null;
            _mockRecipeService.Setup(s => s.GetRecipesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string[]>(),
                It.IsAny<int>(), It.IsAny<int>()))
                .Callback<string, string, string, string, string[], string[], int, int>(
                    (s, cat, cui, diff, dl, t, p, l) => requestedCategory = cat)
                .ReturnsAsync((new PaginatedRecipeResponse { data = new Recipe[0], total = 0 }, null));

            await _viewModel.SetCategoryAsync("pasta");

            Assert.AreEqual("pasta", _viewModel.SelectedCategory);
            Assert.AreEqual("pasta", requestedCategory);
        }

        [Test]
        public void Navigation_DispatchesExpectedActions()
        {
            string lastAction = null;
            string lastArg = null;
            _viewModel.NavigationRequested += (action, args) =>
            {
                lastAction = action;
                if (args != null && args.Length > 0)
                    lastArg = args[0].value as string;
            };

            _viewModel.OpenCreateRecipe();
            Assert.AreEqual(Actions.recipes_to_editor, lastAction);

            _viewModel.GoToPantry();
            Assert.AreEqual(Actions.go_to_pantry, lastAction);

            _viewModel.OpenRecipe("r-123");
            Assert.AreEqual(Actions.recipes_to_detail, lastAction);
            Assert.AreEqual("r-123", lastArg);
        }

        [Test]
        public async Task SetCuisineAsync_UpdatesSelectedCuisineAndFetches()
        {
            await _viewModel.SetTabAsync(RecipeBookTab.Explore);

            string requestedCuisine = null;
            _mockRecipeService.Setup(s => s.GetRecipesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string[]>(),
                It.IsAny<int>(), It.IsAny<int>()))
                .Callback<string, string, string, string, string[], string[], int, int>(
                    (s, cat, cui, diff, dl, t, p, l) => requestedCuisine = cui)
                .ReturnsAsync((new PaginatedRecipeResponse { data = new Recipe[0], total = 0 }, null));

            await _viewModel.SetCuisineAsync("Italian");

            Assert.AreEqual("Italian", _viewModel.SelectedCuisine);
            Assert.AreEqual("Italian", requestedCuisine);
        }

        [Test]
        public void InitDefaultCuisine_WhenUserCountryMatches_SetsDefaultCuisine()
        {
            var storeWithSpain = new TestStoreService();
            storeWithSpain.GetAppState().userCountry = "ES";

            var vm = new RecipeBookViewModel(storeWithSpain, _mockRecipeService.Object, _mockCatalogService.Object, _localStorage);

            Assert.AreEqual("Spanish", vm.SelectedCuisine);
        }

        [Test]
        public void InitDefaultCuisine_WhenUserCountryNotMatched_DefaultsToAll()
        {
            var storeWithGermany = new TestStoreService();
            storeWithGermany.GetAppState().userCountry = "DE";

            var vm = new RecipeBookViewModel(storeWithGermany, _mockRecipeService.Object, _mockCatalogService.Object, _localStorage);

            Assert.AreEqual("all", vm.SelectedCuisine);
        }

        [Test]
        public async Task ClearFiltersAsync_ResetsCuisineToAll()
        {
            await _viewModel.SetCuisineAsync("Mexican");
            Assert.AreEqual("Mexican", _viewModel.SelectedCuisine);

            await _viewModel.ClearFiltersAsync();
            Assert.AreEqual("all", _viewModel.SelectedCuisine);
        }
    }
}
