using System;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MealLogViewModelTests
    {
        private Mock<IMealLogService> _mockMealLogService;
        private Mock<IMealService> _mockMealService;
        private Mock<IRecipeService> _mockRecipeService;
        private Mock<IFoodProductService> _mockFoodProductService;
        private Mock<IGenericFoodService> _mockGenericFoodService;
        private Mock<IMealItemService> _mockMealItemService;
        private Mock<ICatalogService> _mockCatalogService;
        private Mock<ILocalStorageService> _mockLocalStorage;
        private TestStoreService _storeService;
        private MealLogViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockMealLogService = new Mock<IMealLogService>();
            _mockMealService = new Mock<IMealService>();
            _mockRecipeService = new Mock<IRecipeService>();
            _mockFoodProductService = new Mock<IFoodProductService>();
            _mockGenericFoodService = new Mock<IGenericFoodService>();
            _mockMealItemService = new Mock<IMealItemService>();
            _mockCatalogService = new Mock<ICatalogService>();
            _mockLocalStorage = new Mock<ILocalStorageService>();
            _storeService = new TestStoreService();
            _vm = new MealLogViewModel(
                _storeService,
                _mockMealLogService.Object,
                _mockMealService.Object,
                _mockRecipeService.Object,
                _mockFoodProductService.Object,
                _mockGenericFoodService.Object,
                _mockMealItemService.Object,
                _mockCatalogService.Object,
                _mockLocalStorage.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.DisposeSearchCts();
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesDefaults()
        {
            Assert.IsNotNull(_vm.Groups);
            Assert.AreEqual(0, _vm.Groups.Count);
            Assert.AreEqual(MealLogStep.SelectingTypeOfMeal, _vm.CurrentStep);
            Assert.IsEmpty(_vm.TypeOfMealOptions);
            Assert.AreEqual(-1, _vm.SelectedTypeOfMealIndex);
            Assert.IsFalse(_vm.MealFromPantry);
            Assert.IsFalse(_vm.EatenOut);
            Assert.IsEmpty(_vm.PresetResults);
            Assert.IsEmpty(_vm.SelectedItems);
            Assert.IsEmpty(_vm.MealContainerName);
            Assert.IsNull(_vm.SelectedMealPreset);
            Assert.IsFalse(_vm.IsSaving);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task InitializeAsync_LoadsTypeOfMealOptions()
        {
            var typeOfMeals = new[]
            {
                new CatalogItem { code = "BREAKFAST", label = "Breakfast" },
                new CatalogItem { code = "LUNCH", label = "Lunch" },
            };

            _mockCatalogService.Setup(x => x.GetTypeOfMealsAsync())
                .ReturnsAsync((typeOfMeals, null));

            await _vm.InitializeAsync();

            Assert.AreEqual(2, _vm.TypeOfMealOptions.Length);
            Assert.AreEqual("BREAKFAST", _vm.TypeOfMealOptions[0].code);
        }

        [Test]
        public void SelectTypeOfMeal_AdvancesToSourceStep()
        {
            _vm.TypeOfMealOptions = new[]
            {
                new CatalogItem { code = "BREAKFAST", label = "Breakfast" },
                new CatalogItem { code = "LUNCH", label = "Lunch" },
            };

            _vm.SelectTypeOfMeal(0);

            Assert.AreEqual(0, _vm.SelectedTypeOfMealIndex);
            Assert.AreEqual(MealLogStep.SelectingSource, _vm.CurrentStep);
        }

        [Test]
        public void SetSource_AdvancesToDishesStep()
        {
            _vm.SetSource(true, false);

            Assert.IsTrue(_vm.MealFromPantry);
            Assert.IsFalse(_vm.EatenOut);
            Assert.AreEqual(MealLogStep.SelectingDishes, _vm.CurrentStep);
        }

        [Test]
        public void GoBack_FromSource_ReturnsToTypeStep()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "BREAKFAST", label = "Breakfast" } };
            _vm.SelectTypeOfMeal(0);
            Assert.AreEqual(MealLogStep.SelectingSource, _vm.CurrentStep);

            _vm.GoBack();
            Assert.AreEqual(MealLogStep.SelectingTypeOfMeal, _vm.CurrentStep);
        }

        [Test]
        public void GoBack_FromDishes_ReturnsToSourceStep()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "BREAKFAST", label = "Breakfast" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(true, false);
            Assert.AreEqual(MealLogStep.SelectingDishes, _vm.CurrentStep);

            _vm.GoBack();
            Assert.AreEqual(MealLogStep.SelectingSource, _vm.CurrentStep);
        }

        [Test]
        public void ResetToStep1_ClearsStateAndReturnsToTypeStep()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "BREAKFAST", label = "Breakfast" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(true, false);

            _vm.ResetToStep1();

            Assert.AreEqual(MealLogStep.SelectingTypeOfMeal, _vm.CurrentStep);
            Assert.AreEqual(-1, _vm.SelectedTypeOfMealIndex);
            Assert.IsFalse(_vm.MealFromPantry);
            Assert.IsEmpty(_vm.PresetResults);
            Assert.IsEmpty(_vm.SelectedItems);
            Assert.IsEmpty(_vm.MealContainerName);
            Assert.IsNull(_vm.SelectedMealPreset);
        }

        // ========= Preset tests =========

        [Test]
        public void SelectMealPreset_SetsContainerAndClearsResults()
        {
            var meal = new Meal { id = "m1", name = "Pasta" };
            _vm.SelectMealPreset(meal);

            Assert.AreSame(meal, _vm.SelectedMealPreset);
            Assert.AreEqual("Pasta", _vm.MealContainerName);
            Assert.IsEmpty(_vm.PresetResults);
        }

        [Test]
        public void ClearMealPreset_ResetsContainer()
        {
            _vm.SelectedMealPreset = new Meal { id = "m1", name = "Pasta" };
            _vm.MealContainerName = "Pasta";

            _vm.ClearMealPreset();

            Assert.IsNull(_vm.SelectedMealPreset);
            Assert.IsEmpty(_vm.MealContainerName);
        }

        // ========= Item selection tests =========

        [Test]
        public void AddProductItem_ImportsAndAddsToSelection()
        {
            var product = new OpenFoodFactsProduct
            {
                barcode = "123456",
                name = "Test Product",
            };
            var foodProduct = new FoodProduct { id = Guid.NewGuid().ToString(), name = "Imported Product" };

            _mockFoodProductService
                .Setup(x => x.ImportFromBarcodeAsync("123456"))
                .ReturnsAsync((foodProduct, null));

            _vm.AddProductItem(product, 2f, "PIECES").GetAwaiter().GetResult();

            Assert.AreEqual(1, _vm.SelectedItems.Count);
            Assert.AreEqual(foodProduct.id, _vm.SelectedItems[0].foodProductId);
            Assert.AreEqual("Test Product", _vm.SelectedItems[0].name);
            Assert.AreEqual(2f, _vm.SelectedItems[0].quantity);
            Assert.AreEqual("PIECES", _vm.SelectedItems[0].unit);
            Assert.IsTrue(_vm.SelectedItems[0].isProduct);
        }

        [Test]
        public void AddProductItem_ImportFails_FallsBackToFindByBarcode()
        {
            var product = new OpenFoodFactsProduct
            {
                barcode = "123456",
                name = "Test Product",
            };
            var apiError = new ApiErrorResponse { statusCode = 400, message = "Already exists" };
            var existingFood = new FoodProduct { id = Guid.NewGuid().ToString(), name = "Existing Product" };

            _mockFoodProductService
                .Setup(x => x.ImportFromBarcodeAsync("123456"))
                .ReturnsAsync(((FoodProduct)null, apiError));
            _mockFoodProductService
                .Setup(x => x.FindByBarcodeAsync("123456"))
                .ReturnsAsync((existingFood, null));

            _vm.AddProductItem(product, 1f, "G").GetAwaiter().GetResult();

            Assert.AreEqual(1, _vm.SelectedItems.Count);
            Assert.AreEqual(existingFood.id, _vm.SelectedItems[0].foodProductId);
        }

        [Test]
        public void AddProductItem_ImportAndFindFail_DoesNotAdd()
        {
            var product = new OpenFoodFactsProduct
            {
                barcode = "123456",
                name = "Test Product",
            };
            var apiError = new ApiErrorResponse { statusCode = 500, message = "Server error" };

            _mockFoodProductService
                .Setup(x => x.ImportFromBarcodeAsync("123456"))
                .ReturnsAsync(((FoodProduct)null, apiError));

            _vm.AddProductItem(product, 1f, "PIECES").GetAwaiter().GetResult();

            Assert.IsEmpty(_vm.SelectedItems);
            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task AddGenericFoodItem_WithValidId_AddsToSelection()
        {
            var food = new GenericFood
            {
                id = Guid.NewGuid().ToString(),
                foodName = "Test Food",
            };

            await _vm.AddGenericFoodItem(food, 3f, "KG");

            Assert.AreEqual(1, _vm.SelectedItems.Count);
            Assert.AreEqual(food.id, _vm.SelectedItems[0].genericFoodId);
            Assert.AreEqual("Test Food", _vm.SelectedItems[0].name);
            Assert.AreEqual(3f, _vm.SelectedItems[0].quantity);
            Assert.AreEqual("KG", _vm.SelectedItems[0].unit);
            Assert.IsTrue(_vm.SelectedItems[0].isGenericFood);
        }

        [Test]
        public async Task AddGenericFoodItem_WithInvalidId_ShowsError()
        {
            var food = new GenericFood
            {
                id = "not-a-uuid",
                foodName = "Bad Food",
            };

            await _vm.AddGenericFoodItem(food, 1f, "PIECES");

            Assert.IsEmpty(_vm.SelectedItems);
            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public void RemoveItem_RemovesFromSelection()
        {
            var item = new MealLogItem { foodProductId = "uuid-1", name = "Item" };
            _vm.SelectedItems = new System.Collections.Generic.List<MealLogItem> { item };

            _vm.RemoveItem(item);

            Assert.IsEmpty(_vm.SelectedItems);
        }

        [Test]
        public void GetEmojiForTypeOfMeal_ReturnsCorrectEmoji()
        {
            Assert.AreEqual("\U0001F305", MealLogViewModel.GetEmojiForTypeOfMeal("BREAKFAST"));
            Assert.AreEqual("\U0001F37D\uFE0F", MealLogViewModel.GetEmojiForTypeOfMeal("UNKNOWN"));
        }

        // ========= Save tests =========

        [Test]
        public async Task SaveAsync_WithNoType_ReturnsFalse()
        {
            bool result = await _vm.SaveAsync();
            Assert.IsFalse(result);
        }

        [Test]
        public async Task SaveAsync_WithPresetAndItems_CreatesMealItemsAndOneLog()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "DINNER", label = "Dinner" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(false, true);

            _vm.SelectMealPreset(new Meal { id = "existing-meal", name = "Mi cena" });

            var prodItem = new MealLogItem
            {
                foodProductId = Guid.NewGuid().ToString(),
                name = "Arroz",
                quantity = 2f,
                unit = "G",
                isProduct = true,
            };
            var genItem = new MealLogItem
            {
                genericFoodId = Guid.NewGuid().ToString(),
                name = "Tomate",
                quantity = 1f,
                unit = "PIECES",
                isGenericFood = true,
            };
            _vm.SelectedItems = new System.Collections.Generic.List<MealLogItem> { prodItem, genItem };

            _mockMealItemService
                .Setup(x => x.CreateAsync("existing-meal", It.Is<CreateMealItemRequest>(r => r.foodProductId == prodItem.foodProductId)))
                .ReturnsAsync((new MealItem { id = "mi-1" }, null));
            _mockMealItemService
                .Setup(x => x.CreateAsync("existing-meal", It.Is<CreateMealItemRequest>(r => r.genericFoodId == genItem.genericFoodId)))
                .ReturnsAsync((new MealItem { id = "mi-2" }, null));

            _mockMealLogService
                .Setup(x => x.CreateAsync(It.Is<CreateMealLogRequest>(r => r.mealId == "existing-meal" && r.typeOfMeal == "DINNER")))
                .ReturnsAsync((new MealLog { id = "log-1" }, null));

            bool result = await _vm.SaveAsync();

            Assert.IsTrue(result);
            _mockMealItemService.Verify(x => x.CreateAsync("existing-meal", It.IsAny<CreateMealItemRequest>()), Times.Exactly(2));
            _mockMealLogService.Verify(x => x.CreateAsync(It.IsAny<CreateMealLogRequest>()), Times.Once);
            _mockMealService.Verify(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()), Times.Never);
        }

        [Test]
        public async Task SaveAsync_WithoutPreset_CreatesMealThenItemsThenLog()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "LUNCH", label = "Lunch" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(true, false);
            _vm.MealContainerName = "My lunch";

            var prodItem = new MealLogItem
            {
                foodProductId = Guid.NewGuid().ToString(),
                name = "Pasta",
                quantity = 1f,
                unit = "PIECES",
                isProduct = true,
            };
            _vm.SelectedItems = new System.Collections.Generic.List<MealLogItem> { prodItem };

            var createdMeal = new Meal { id = "new-meal", name = "My lunch" };
            _mockMealService
                .Setup(x => x.CreateMealAsync(It.Is<CreateMealRequest>(r => r.name == "My lunch")))
                .ReturnsAsync((createdMeal, null));

            _mockMealItemService
                .Setup(x => x.CreateAsync("new-meal", It.Is<CreateMealItemRequest>(r => r.foodProductId == prodItem.foodProductId)))
                .ReturnsAsync((new MealItem { id = "mi-1" }, null));

            _mockMealLogService
                .Setup(x => x.CreateAsync(It.Is<CreateMealLogRequest>(r => r.mealId == "new-meal")))
                .ReturnsAsync((new MealLog { id = "log-1" }, null));

            bool result = await _vm.SaveAsync();

            Assert.IsTrue(result);
            _mockMealService.Verify(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()), Times.Once);
            _mockMealItemService.Verify(x => x.CreateAsync("new-meal", It.IsAny<CreateMealItemRequest>()), Times.Once);
            _mockMealLogService.Verify(x => x.CreateAsync(It.IsAny<CreateMealLogRequest>()), Times.Once);
        }

        [Test]
        public async Task SaveAsync_WithMealItemError_ReturnsFalse()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "DINNER", label = "Dinner" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(true, false);

            _vm.SelectMealPreset(new Meal { id = "existing-meal", name = "Dinner" });

            var item = new MealLogItem
            {
                foodProductId = Guid.NewGuid().ToString(),
                name = "Pasta",
                quantity = 1f,
                unit = "PIECES",
                isProduct = true,
            };
            _vm.SelectedItems = new System.Collections.Generic.List<MealLogItem> { item };

            var apiError = new ApiErrorResponse { statusCode = 500, message = "MealItem error" };
            _mockMealItemService
                .Setup(x => x.CreateAsync("existing-meal", It.IsAny<CreateMealItemRequest>()))
                .ReturnsAsync(((MealItem)null, apiError));

            bool result = await _vm.SaveAsync();

            Assert.IsFalse(result);
            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task SaveAsync_WithApiError_ReturnsFalse()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "BREAKFAST", label = "Breakfast" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(false, false);
            _vm.MealContainerName = "My meal";

            _mockMealService
                .Setup(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()))
                .ReturnsAsync((new Meal { id = "new-meal" }, null));

            var apiError = new ApiErrorResponse { statusCode = 500, message = "Server error" };
            _mockMealLogService
                .Setup(x => x.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync(((MealLog)null, apiError));

            bool result = await _vm.SaveAsync();

            Assert.IsFalse(result);
            Assert.IsNotNull(_vm.ErrorDetail);
        }

        // ========= Load / Delete tests =========

        [Test]
        public async Task LoadTodayAsync_ParsesAndGroupsLogs()
        {
            var response = new PaginatedMealLogResponse
            {
                data = new[]
                {
                    new MealLog { id = "1", typeOfMeal = "BREAKFAST", meal = new Meal { name = "Toast" } },
                    new MealLog { id = "2", typeOfMeal = "BREAKFAST", meal = new Meal { name = "Juice" } },
                    new MealLog { id = "3", typeOfMeal = "LUNCH", meal = new Meal { name = "Salad" } }
                },
                total = 3, page = 1, limit = 20, totalPages = 1
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 50, null, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((response, null));

            await _vm.LoadTodayAsync();

            Assert.IsTrue(_vm.TodayLoaded);
            Assert.AreEqual(2, _vm.Groups.Count);
            Assert.AreEqual(2, _vm.Groups.First(g => g.TypeOfMeal == "BREAKFAST").Logs.Count);
        }

        [Test]
        public async Task DeleteLogAsync_Success_RemovesFromGroups()
        {
            var response = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST", meal = new Meal { name = "Toast" } } },
                total = 1, page = 1, limit = 20, totalPages = 1
            };
            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 50, null, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((response, null));

            await _vm.LoadTodayAsync();
            Assert.AreEqual(1, _vm.Groups.SelectMany(g => g.Logs).Count());

            _mockMealLogService
                .Setup(x => x.DeleteLogAsync("1"))
                .ReturnsAsync((true, null));

            await _vm.DeleteLogAsync("1");

            Assert.IsNull(_vm.ErrorDetail);
            Assert.AreEqual(0, _vm.Groups.SelectMany(g => g.Logs).Count());
        }

        [Test]
        public async Task DeleteLogAsync_WithApiError_SetsErrorDetail()
        {
            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 50, null, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((new PaginatedMealLogResponse { data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST" } }, total = 1, page = 1, limit = 20, totalPages = 1 }, null));

            await _vm.LoadTodayAsync();

            var apiError = new ApiErrorResponse { statusCode = 500, message = "Delete failed" };
            _mockMealLogService
                .Setup(x => x.DeleteLogAsync("1"))
                .ReturnsAsync((false, apiError));

            await _vm.DeleteLogAsync("1");

            Assert.IsNotNull(_vm.ErrorDetail);
        }
    }
}
