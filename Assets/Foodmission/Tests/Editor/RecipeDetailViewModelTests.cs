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
        private Mock<IPantryService> _mockPantryService;
        private RecipeDetailViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState { userId = "user-1" });
            _mockRecipeService = new Mock<IRecipeService>();
            _mockShoppingListService = new Mock<IShoppingListService>();
            _mockCatalogService = new Mock<ICatalogService>();
            _mockPantryService = new Mock<IPantryService>();
            _mockPantryService.Setup(s => s.GetItemsAsync())
                .ReturnsAsync((Array.Empty<PantryItem>(), null));
            _viewModel = new RecipeDetailViewModel(_storeService, _mockRecipeService.Object, _mockShoppingListService.Object, _mockCatalogService.Object, _mockPantryService.Object);
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
            string navAction = null;
            _viewModel.NavigationRequested += (action, args) => navAction = action;
            _viewModel.Recipe = new Recipe { id = "r1" };
            _viewModel.LogRecipe(2, true);
            Assert.AreEqual("go_to_meallog", navAction);
        }

        [Test]
        public void Edit_DispatchesNavToEditor()
        {
            string navAction = null;
            _viewModel.NavigationRequested += (action, args) => navAction = action;
            _viewModel.Recipe = new Recipe { id = "r1" };
            _viewModel.Edit();
            Assert.AreEqual("recipes_to_editor", navAction);
        }

        [Test]
        public async Task DeleteAsync_OnSuccess_NavigatesToRecipes()
        {
            string navAction = null;
            _viewModel.NavigationRequested += (action, args) => navAction = action;
            _viewModel.Recipe = new Recipe { id = "r1", userId = "user-1" };
            _mockRecipeService.Setup(s => s.DeleteRecipeAsync("r1"))
                .ReturnsAsync((true, null));
            await _viewModel.DeleteAsync();
            Assert.AreEqual("go_to_recipes", navAction);
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
            _mockShoppingListService.Setup(s => s.GetListsAsync())
                .ReturnsAsync((new[] { new ShoppingList { id = "list-1" } }, null));

            _mockShoppingListService.Setup(s => s.AddItemAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<string>()))
                .ReturnsAsync((new ShoppingListItem { id = "i1" }, null));

            await _viewModel.AddIngredientsToShoppingListAsync(await _viewModel.GetShoppingListCandidatesAsync(), "list-1");

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

            bool success = await _viewModel.AddIngredientsToShoppingListAsync(await _viewModel.GetShoppingListCandidatesAsync());

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

            bool success = await _viewModel.AddIngredientsToShoppingListAsync(await _viewModel.GetShoppingListCandidatesAsync());

            Assert.IsTrue(success);
            _mockShoppingListService.Verify(s => s.CreateListAsync(It.IsAny<string>()), Times.Once);
            _mockShoppingListService.Verify(s => s.AddItemAsync("new-list-id", "fp1", It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task AddIngredientsToShoppingListAsync_WhenItemExists_IncrementsQuantity()
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
                .ReturnsAsync((new[] { new ShoppingList { id = "list-1" } }, null));

            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1"))
                .ReturnsAsync((new[] { new ShoppingListItem { id = "item-1", foodProductId = "fp1", quantity = 2f, unit = "PIECES", @checked = false } }, null));

            _mockShoppingListService.Setup(s => s.UpdateItemAsync("list-1", "item-1", 3f, "PIECES", null, false))
                .ReturnsAsync((new ShoppingListItem { id = "item-1", quantity = 3f }, null));

            bool success = await _viewModel.AddIngredientsToShoppingListAsync(await _viewModel.GetShoppingListCandidatesAsync(), "list-1");

            Assert.IsTrue(success);
            _mockShoppingListService.Verify(s => s.UpdateItemAsync("list-1", "item-1", 3f, "PIECES", null, false), Times.Once);
        }

        // ── Pantry check before the shopping list (issue #57) ───────────────

        [Test]
        public async Task GetShoppingListCandidatesAsync_IngredientNotInPantry_IsSelectedWithRecipeAmount()
        {
            _viewModel.Recipe = new Recipe { id = "r1", ingredients = new[] { new RecipeIngredient { genericFoodId = "gf1", measure = "200 g" } } };

            var candidates = await _viewModel.GetShoppingListCandidatesAsync();

            Assert.AreEqual(1, candidates.Length);
            Assert.IsTrue(candidates[0].Selected);
            Assert.IsFalse(candidates[0].InPantry);
            Assert.AreEqual(200f, candidates[0].Quantity);
            Assert.AreEqual(UnitCodes.G, candidates[0].Unit);
        }

        [Test]
        public async Task GetShoppingListCandidatesAsync_SkipsIngredientsWithoutFoodId()
        {
            _viewModel.Recipe = new Recipe { id = "r1", ingredients = new[] { new RecipeIngredient { name = "Salt" }, new RecipeIngredient { foodProductId = "fp1" } } };

            var candidates = await _viewModel.GetShoppingListCandidatesAsync();

            Assert.AreEqual(1, candidates.Length);
            Assert.AreEqual("fp1", candidates[0].Ingredient.foodProductId);
        }

        [Test]
        public async Task GetShoppingListCandidatesAsync_PantryFailure_OffersEverything()
        {
            _mockPantryService.Setup(s => s.GetItemsAsync())
                .ReturnsAsync(((PantryItem[])null, new ApiErrorResponse { statusCode = 500, message = "boom" }));
            _viewModel.Recipe = new Recipe { id = "r1", ingredients = new[] { new RecipeIngredient { genericFoodId = "gf1" } } };

            var candidates = await _viewModel.GetShoppingListCandidatesAsync();

            Assert.IsTrue(candidates[0].Selected);
            Assert.IsFalse(candidates[0].InPantry);
            Assert.IsNull(_viewModel.ErrorDetail);
        }

        [Test]
        public void BuildCandidate_EnoughInPantry_IsNotSelected()
        {
            var pantry = new[] { new PantryItem { genericFoodId = "gf1", quantity = 0.5f, unit = UnitCodes.Kg } };

            var c = RecipeDetailViewModel.BuildCandidate(new RecipeIngredient { genericFoodId = "gf1", measure = "200 g" }, pantry);

            Assert.IsTrue(c.InPantry);
            Assert.IsFalse(c.Selected);
            Assert.IsFalse(c.PantryShort);
            Assert.AreEqual(0.5f, c.PantryQuantity);
            Assert.AreEqual(UnitCodes.Kg, c.PantryUnit);
        }

        [Test]
        public void BuildCandidate_NotEnoughInPantry_SelectsTheDifference()
        {
            var pantry = new[]
            {
                new PantryItem { foodProductId = "fp1", quantity = 100f, unit = UnitCodes.G },
                new PantryItem { foodProductId = "fp1", quantity = 0.05f, unit = UnitCodes.Kg }
            };

            var c = RecipeDetailViewModel.BuildCandidate(new RecipeIngredient { foodProductId = "fp1", measure = "400 g" }, pantry);

            Assert.IsTrue(c.InPantry);
            Assert.IsTrue(c.PantryShort);
            Assert.IsTrue(c.Selected);
            Assert.AreEqual(250f, c.Quantity, 0.01f);
            Assert.AreEqual(UnitCodes.G, c.Unit);
            Assert.AreEqual(150f, c.PantryQuantity, 0.01f);
        }

        [Test]
        public void BuildCandidate_AmountsNotComparable_HavingItIsEnough()
        {
            var pantry = new[] { new PantryItem { genericFoodId = "gf1", quantity = 1f, unit = UnitCodes.Pieces } };

            var freeText = RecipeDetailViewModel.BuildCandidate(new RecipeIngredient { genericFoodId = "gf1", measure = "2 tbs" }, pantry);
            var otherUnit = RecipeDetailViewModel.BuildCandidate(new RecipeIngredient { genericFoodId = "gf1", measure = "300 g" }, pantry);

            Assert.IsFalse(freeText.Selected);
            Assert.IsFalse(freeText.HasRecipeQuantity);
            Assert.AreEqual(1f, freeText.Quantity);
            Assert.AreEqual(UnitCodes.Pieces, freeText.Unit);
            Assert.IsFalse(otherUnit.Selected);
            Assert.IsFalse(otherUnit.PantryShort);
        }

        [Test]
        public void BuildCandidate_MatchesByGenericFoodWhenProductDiffers()
        {
            var pantry = new[] { new PantryItem { foodProductId = "other", genericFoodId = "gf1", quantity = 1f, unit = UnitCodes.L } };

            var c = RecipeDetailViewModel.BuildCandidate(new RecipeIngredient { foodProductId = "fp1", genericFoodId = "gf1", measure = "500 ml" }, pantry);

            Assert.IsTrue(c.InPantry);
            Assert.IsFalse(c.Selected);
        }

        [Test]
        public async Task AddIngredientsToShoppingListAsync_OnlyAddsSelected_WithTheirAmount()
        {
            _mockShoppingListService.Setup(s => s.GetListsAsync())
                .ReturnsAsync((new[] { new ShoppingList { id = "list-1" } }, null));
            _mockShoppingListService.Setup(s => s.AddItemAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<string>()))
                .ReturnsAsync((new ShoppingListItem { id = "i1" }, null));
            var candidates = new[]
            {
                new RecipeShoppingCandidate { Ingredient = new RecipeIngredient { genericFoodId = "gf1" }, Quantity = 250f, Unit = UnitCodes.G, Selected = true },
                new RecipeShoppingCandidate { Ingredient = new RecipeIngredient { genericFoodId = "gf2" }, Quantity = 1f, Unit = UnitCodes.Pieces, Selected = false }
            };

            bool success = await _viewModel.AddIngredientsToShoppingListAsync(candidates, "list-1");

            Assert.IsTrue(success);
            _mockShoppingListService.Verify(s => s.AddItemAsync("list-1", null, 250f, UnitCodes.G, null, null, "gf1"), Times.Once);
            _mockShoppingListService.Verify(s => s.AddItemAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                "gf2"), Times.Never);
        }

        [Test]
        public void BuildCandidate_FractionalAmount_IsMadeWholeForTheShoppingList()
        {
            var kg = RecipeDetailViewModel.BuildCandidate(new RecipeIngredient { genericFoodId = "gf1", measure = "1.5 kg" }, null);
            var cups = RecipeDetailViewModel.BuildCandidate(new RecipeIngredient { genericFoodId = "gf1", measure = "1/2 cup" }, null);

            Assert.AreEqual(1500f, kg.Quantity);
            Assert.AreEqual(UnitCodes.G, kg.Unit);
            Assert.AreEqual(1f, cups.Quantity);
            Assert.AreEqual(UnitCodes.Cups, cups.Unit);
        }

        [Test]
        public async Task AddIngredientsToShoppingListAsync_SendsWholeQuantities()
        {
            _mockShoppingListService.Setup(s => s.GetListsAsync())
                .ReturnsAsync((new[] { new ShoppingList { id = "list-1" } }, null));
            _mockShoppingListService.Setup(s => s.AddItemAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<string>()))
                .ReturnsAsync((new ShoppingListItem { id = "i1" }, null));
            var candidates = new[] { new RecipeShoppingCandidate { Ingredient = new RecipeIngredient { genericFoodId = "gf1" }, Quantity = 0.25f, Unit = UnitCodes.Cups, Selected = true } };

            await _viewModel.AddIngredientsToShoppingListAsync(candidates, "list-1");

            _mockShoppingListService.Verify(s => s.AddItemAsync("list-1", null, 1f, UnitCodes.Cups, null, null, "gf1"), Times.Once);
        }

        [Test]
        public async Task AddIngredientsToShoppingListAsync_NothingSelected_DoesNothing()
        {
            var candidates = new[] { new RecipeShoppingCandidate { Ingredient = new RecipeIngredient { genericFoodId = "gf1" }, Selected = false } };

            bool success = await _viewModel.AddIngredientsToShoppingListAsync(candidates);

            Assert.IsFalse(success);
            _mockShoppingListService.Verify(s => s.GetListsAsync(), Times.Never);
        }

        [Test]
        public async Task AddIngredientsToShoppingListAsync_ItemOnListInConvertibleUnit_AddsUpConverted()
        {
            _mockShoppingListService.Setup(s => s.GetListsAsync())
                .ReturnsAsync((new[] { new ShoppingList { id = "list-1" } }, null));
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1"))
                .ReturnsAsync((new[] { new ShoppingListItem { id = "item-1", genericFoodId = "gf1", quantity = 1f, unit = UnitCodes.Kg } }, null));
            _mockShoppingListService.Setup(s => s.UpdateItemAsync("list-1", "item-1", It.IsAny<float?>(), It.IsAny<string>(), null, false))
                .ReturnsAsync((new ShoppingListItem { id = "item-1" }, null));
            var candidates = new[] { new RecipeShoppingCandidate { Ingredient = new RecipeIngredient { genericFoodId = "gf1" }, Quantity = 500f, Unit = UnitCodes.G, Selected = true } };

            await _viewModel.AddIngredientsToShoppingListAsync(candidates, "list-1");

            // 1 kg + 500 g = 1.5 kg → sent as 1500 g (the backend only takes whole quantities)
            _mockShoppingListService.Verify(s => s.UpdateItemAsync("list-1", "item-1", 1500f, UnitCodes.G, null, false), Times.Once);
        }

        [Test]
        public async Task AddIngredientsToShoppingListAsync_ItemOnListInOtherKindOfUnit_LeavesIt()
        {
            _mockShoppingListService.Setup(s => s.GetListsAsync())
                .ReturnsAsync((new[] { new ShoppingList { id = "list-1" } }, null));
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1"))
                .ReturnsAsync((new[] { new ShoppingListItem { id = "item-1", genericFoodId = "gf1", quantity = 2f, unit = UnitCodes.Pieces } }, null));
            var candidates = new[] { new RecipeShoppingCandidate { Ingredient = new RecipeIngredient { genericFoodId = "gf1" }, Quantity = 500f, Unit = UnitCodes.G, Selected = true } };

            bool success = await _viewModel.AddIngredientsToShoppingListAsync(candidates, "list-1");

            Assert.IsTrue(success);
            _mockShoppingListService.Verify(s => s.UpdateItemAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()), Times.Never);
            _mockShoppingListService.Verify(s => s.AddItemAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task LoadAsync_OnSuccess_ReportsRecipeViewed()
        {
            var session = new Mock<IChallengeSessionService>();
            var vm = new RecipeDetailViewModel(_storeService, _mockRecipeService.Object, _mockShoppingListService.Object, _mockCatalogService.Object, _mockPantryService.Object, session.Object);
            _mockRecipeService.Setup(s => s.GetRecipeAsync("r-1"))
                .ReturnsAsync((new Recipe { id = "r-1", userId = "user-1", title = "Pasta" }, null));

            await vm.LoadAsync("r-1");

            session.Verify(s => s.ReportAsync(ChallengeCompletionTrigger.RecipeViewed, "r-1"), Times.Once);
            vm.Dispose();
        }

        // ── Rating ──────────────────────────────────────────────────────────

        [Test]
        public async Task LoadAsync_OtherUsersRecipe_LoadsMyRating()
        {
            _mockRecipeService.Setup(s => s.GetRecipeAsync("r1"))
                .ReturnsAsync((new Recipe { id = "r1", userId = "user-other", rating = 3f, ratingCount = 2 }, null));
            _mockRecipeService.Setup(s => s.GetRatingAsync("r1"))
                .ReturnsAsync((new RecipeRating { recipeId = "r1", rating = 4.2f, ratingCount = 12, myRating = 4 }, null));

            await _viewModel.LoadAsync("r1");

            Assert.IsTrue(_viewModel.CanRate);
            Assert.AreEqual(4, _viewModel.MyRating);
            Assert.AreEqual(4.2f, _viewModel.AverageRating, 0.001f);
            Assert.AreEqual(12, _viewModel.RatingCount);
        }

        [Test]
        public async Task LoadAsync_OwnRecipe_CannotRateAndSkipsRatingRequest()
        {
            _mockRecipeService.Setup(s => s.GetRecipeAsync("r1"))
                .ReturnsAsync((new Recipe { id = "r1", userId = "user-1", rating = 3.5f, ratingCount = 4 }, null));

            await _viewModel.LoadAsync("r1");

            Assert.IsFalse(_viewModel.CanRate);
            Assert.AreEqual(3.5f, _viewModel.AverageRating, 0.001f);
            Assert.AreEqual(4, _viewModel.RatingCount);
            _mockRecipeService.Verify(s => s.GetRatingAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task RateAsync_OnSuccess_UpdatesMyRatingAndAggregate()
        {
            await LoadOtherUsersRecipeAsync(myRating: null);
            _mockRecipeService.Setup(s => s.RateAsync("r1", 5))
                .ReturnsAsync((new RecipeRating { recipeId = "r1", rating = 4.5f, ratingCount = 3, myRating = 5 }, null));

            await _viewModel.RateAsync(5);

            Assert.AreEqual(5, _viewModel.MyRating);
            Assert.AreEqual(4.5f, _viewModel.AverageRating, 0.001f);
            Assert.AreEqual(3, _viewModel.RatingCount);
            Assert.IsNull(_viewModel.ErrorDetail);
            Assert.IsFalse(_viewModel.IsRatingBusy);
        }

        [Test]
        public async Task RateAsync_OnError_RestoresPreviousRatingAndSetsError()
        {
            await LoadOtherUsersRecipeAsync(myRating: 2);
            var error = new ApiErrorResponse { statusCode = 500, message = "boom" };
            _mockRecipeService.Setup(s => s.RateAsync("r1", 5))
                .ReturnsAsync(((RecipeRating)null, error));

            await _viewModel.RateAsync(5);

            Assert.AreEqual(2, _viewModel.MyRating);
            Assert.AreSame(error, _viewModel.RatingErrorDetail);
            Assert.IsNull(_viewModel.ErrorDetail);
            Assert.IsFalse(_viewModel.IsRatingBusy);
        }

        [Test]
        public async Task RateAsync_OutOfRangeOrOwner_DoesNothing()
        {
            await LoadOtherUsersRecipeAsync(myRating: null);

            await _viewModel.RateAsync(0);
            await _viewModel.RateAsync(6);

            _mockRecipeService.Verify(s => s.RateAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task RemoveRatingAsync_ClearsMyRatingAndUpdatesAggregate()
        {
            await LoadOtherUsersRecipeAsync(myRating: 4);
            _mockRecipeService.Setup(s => s.RemoveRatingAsync("r1"))
                .ReturnsAsync((new RecipeRating { recipeId = "r1", rating = 3f, ratingCount = 1, myRating = null }, null));

            await _viewModel.RemoveRatingAsync();

            Assert.AreEqual(0, _viewModel.MyRating);
            Assert.AreEqual(3f, _viewModel.AverageRating, 0.001f);
            Assert.AreEqual(1, _viewModel.RatingCount);
        }

        private async Task LoadOtherUsersRecipeAsync(int? myRating)
        {
            _mockRecipeService.Setup(s => s.GetRecipeAsync("r1"))
                .ReturnsAsync((new Recipe { id = "r1", userId = "user-other", rating = 4f, ratingCount = 2 }, null));
            _mockRecipeService.Setup(s => s.GetRatingAsync("r1"))
                .ReturnsAsync((new RecipeRating { recipeId = "r1", rating = 4f, ratingCount = 2, myRating = myRating }, null));
            await _viewModel.LoadAsync("r1");
        }
    }
}
