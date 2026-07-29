using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using eu.foodmission.platform;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class RecipeDetailViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<IRecipeService> _mockRecipeService;
        private Mock<IShoppingListService> _mockShoppingListService;
        private Mock<ICatalogService> _mockCatalogService;
        private RecipeDetailViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState { userId = "user-1" });
            _mockRecipeService = new Mock<IRecipeService>();
            _mockShoppingListService = new Mock<IShoppingListService>();
            _mockCatalogService = new Mock<ICatalogService>();
            _viewModel = new RecipeDetailViewModel(_storeService, _mockRecipeService.Object, _mockShoppingListService.Object, _mockCatalogService.Object);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        [Test]
        public async Task LoadAsync_OnSuccess_SetsRecipeAndIsOwner()
        {
            _mockRecipeService.Setup(s => s.GetRecipeAsync("r1"))
                .ReturnsAsync((new Recipe { id = "r1", userId = "user-1", title = "Pasta" }, null));
            await _viewModel.LoadAsync("r1");
            Assert.AreEqual("Pasta", _viewModel.Recipe.title);
            Assert.IsTrue(_viewModel.IsOwner);
        }

        [Test]
        public async Task LoadAsync_OnOtherUser_SetsIsOwnerFalse()
        {
            _mockRecipeService.Setup(s => s.GetRecipeAsync("r1"))
                .ReturnsAsync((new Recipe { id = "r1", userId = "user-other" }, null));
            await _viewModel.LoadAsync("r1");
            Assert.IsFalse(_viewModel.IsOwner);
        }

        [Test]
        public async Task LoadAsync_OnError_SetsErrorDetail()
        {
            _mockRecipeService.Setup(s => s.GetRecipeAsync("r1"))
                .ReturnsAsync((null, new ApiErrorResponse { message = "not found" }));
            await _viewModel.LoadAsync("r1");
            Assert.IsNotNull(_viewModel.ErrorDetail);
            Assert.IsNull(_viewModel.Recipe);
        }

        [Test]
        public void LogRecipe_DispatchesNavWithRecipeIdAndMealOptions()
        {
            _viewModel.Recipe = new Recipe { id = "r1" };
            _viewModel.LogRecipe(2, true);
            Assert.Contains("go_to_meallog", _storeService.DispatchedActionTypes);
        }

        [Test]
        public void Edit_DispatchesNavToEditor()
        {
            _viewModel.Recipe = new Recipe { id = "r1" };
            _viewModel.Edit();
            Assert.Contains("recipes_to_editor", _storeService.DispatchedActionTypes);
        }

        [Test]
        public async Task DeleteAsync_OnSuccess_NavigatesToRecipes()
        {
            _viewModel.Recipe = new Recipe { id = "r1", userId = "user-1" };
            _mockRecipeService.Setup(s => s.DeleteRecipeAsync("r1"))
                .ReturnsAsync((true, null));
            await _viewModel.DeleteAsync();
            Assert.Contains("go_to_recipes", _storeService.DispatchedActionTypes);
        }

        [Test]
        public async Task AddIngredientsToShoppingListAsync_CallsServicePerIngredient()
        {
            _viewModel.Recipe = new Recipe
            {
                id = "r1",
                ingredients = new[]
                {
                    new RecipeIngredient { foodProductId = "fp1" },
                    new RecipeIngredient { genericFoodId = "gf1" }
                }
            };
            _mockShoppingListService.Setup(s => s.AddItemAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<string>()))
                .ReturnsAsync((new ShoppingListItem { id = "i1" }, null));

            await _viewModel.AddIngredientsToShoppingListAsync("list-1");

            _mockShoppingListService.Verify(s => s.AddItemAsync(
                "list-1", It.IsAny<string>(), It.IsAny<float>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public async Task AddIngredientsToShoppingListAsync_WhenNoListIdProvided_FetchesOrCreatesList()
        {
            _viewModel.Recipe = new Recipe
            {
                id = "r1",
                ingredients = new[]
                {
                    new RecipeIngredient { foodProductId = "fp1" }
                }
            };

            _mockShoppingListService.Setup(s => s.GetListsAsync())
                .ReturnsAsync((new[] { new ShoppingList { id = "auto-list-1" } }, null));

            _mockShoppingListService.Setup(s => s.AddItemAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<string>()))
                .ReturnsAsync((new ShoppingListItem { id = "i1" }, null));

            bool success = await _viewModel.AddIngredientsToShoppingListAsync();

            Assert.IsTrue(success);
            _mockShoppingListService.Verify(s => s.AddItemAsync(
                "auto-list-1", "fp1", It.IsAny<float>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task AddIngredientsToShoppingListAsync_WhenListReturns404_CreatesNewListAndRetries()
        {
            _viewModel.Recipe = new Recipe
            {
                id = "r1",
                ingredients = new[]
                {
                    new RecipeIngredient { foodProductId = "fp1" }
                }
            };

            _storeService.SetAppState(new AppState { userId = "user-1", userLastShoppingListId = "deleted-list-id" });

            _mockShoppingListService.Setup(s => s.GetListsAsync())
                .ReturnsAsync((Array.Empty<ShoppingList>(), null));

            _mockShoppingListService.Setup(s => s.CreateListAsync(It.IsAny<string>()))
                .ReturnsAsync((new ShoppingList { id = "new-list-id" }, null));

            _mockShoppingListService.Setup(s => s.AddItemAsync("deleted-list-id", It.IsAny<string>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>()))
                .ReturnsAsync((null, new ApiErrorResponse { statusCode = 404, message = "Shopping list not found" }));

            _mockShoppingListService.Setup(s => s.AddItemAsync("new-list-id", It.IsAny<string>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>()))
                .ReturnsAsync((new ShoppingListItem { id = "i1" }, null));

            bool success = await _viewModel.AddIngredientsToShoppingListAsync();

            Assert.IsTrue(success);
            _mockShoppingListService.Verify(s => s.CreateListAsync(It.IsAny<string>()), Times.Once);
            _mockShoppingListService.Verify(s => s.AddItemAsync("new-list-id", "fp1", It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>()), Times.Once);
        }
    }
}
