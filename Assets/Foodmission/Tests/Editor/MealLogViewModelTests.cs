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
        private Mock<IOpenFoodFactsClientService> _mockOpenFoodFactsClient;
        private Mock<IPantryService> _mockPantryService;
        private TestStoreService _storeService;
        private MealLogViewModel _vm;
        private System.Func<bool> _originalOverride;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;
            _mockMealLogService = new Mock<IMealLogService>();
            _mockMealService = new Mock<IMealService>();
            _mockRecipeService = new Mock<IRecipeService>();
            _mockFoodProductService = new Mock<IFoodProductService>();
            _mockGenericFoodService = new Mock<IGenericFoodService>();
            _mockMealItemService = new Mock<IMealItemService>();
            _mockCatalogService = new Mock<ICatalogService>();
            _mockLocalStorage = new Mock<ILocalStorageService>();
            _mockOpenFoodFactsClient = new Mock<IOpenFoodFactsClientService>();
            _mockPantryService = new Mock<IPantryService>();
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
                _mockLocalStorage.Object,
                _mockOpenFoodFactsClient.Object,
                _mockPantryService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
            _vm?.DisposeSearchCts();
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesDefaults()
        {
            Assert.IsNotNull(_vm.LastTenLogs);
            Assert.AreEqual(0, _vm.LastTenLogs.Count);
            Assert.AreEqual(MealLogStep.SelectingTypeOfMeal, _vm.CurrentStep);
            Assert.IsEmpty(_vm.TypeOfMealOptions);
            Assert.AreEqual(-1, _vm.SelectedTypeOfMealIndex);
            Assert.IsFalse(_vm.MealFromPantry);
            Assert.IsFalse(_vm.EatenOut);
            Assert.IsEmpty(_vm.PresetResults);
            Assert.IsEmpty(_vm.SelectedItems);
            Assert.IsEmpty(_vm.MealContainerName);
            Assert.IsFalse(_vm.SaveAsPreset);
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

            _mockCatalogService.Setup(x => x.GetTypeOfMealsAsync(It.IsAny<string>()))
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
            Assert.IsFalse(_vm.SaveAsPreset);
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
                .Setup(x => x.FindByBarcodeAsync("123456", true))
                .ReturnsAsync(((FoodProduct)null, (ApiErrorResponse)null));
            _mockFoodProductService
                .Setup(x => x.ImportFromBarcodeAsync("123456"))
                .ReturnsAsync((foodProduct, null));

            _vm.AddProductItem(product, null, null).GetAwaiter().GetResult();

            Assert.AreEqual(1, _vm.SelectedItems.Count);
            Assert.AreEqual(foodProduct.id, _vm.SelectedItems[0].foodProductId);
            Assert.AreEqual("Test Product", _vm.SelectedItems[0].name);
            Assert.IsNull(_vm.SelectedItems[0].quantity);
            Assert.IsNull(_vm.SelectedItems[0].unit);
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
            var existingFood = new FoodProduct { id = Guid.NewGuid().ToString(), name = "Existing Product", barcode = "123456" };

            int callCount = 0;
            _mockFoodProductService
                .Setup(x => x.SearchFoodsByBarcodeAsync("123456"))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount == 1)
                        return (new PaginatedFoodProductResponse { data = new FoodProduct[0] }, (ApiErrorResponse)null);
                    else
                        return (new PaginatedFoodProductResponse { data = new[] { existingFood } }, (ApiErrorResponse)null);
                });
            _mockFoodProductService
                .Setup(x => x.ImportFromBarcodeAsync("123456"))
                .ReturnsAsync(((FoodProduct)null, apiError));

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
                .Setup(x => x.FindByBarcodeAsync("123456", true))
                .ReturnsAsync(((FoodProduct)null, (ApiErrorResponse)null));
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
            Assert.AreEqual("🌅", MealLogHelpers.GetEmojiForTypeOfMeal("BREAKFAST"));
            Assert.AreEqual("🍽️", MealLogHelpers.GetEmojiForTypeOfMeal("UNKNOWN"));
        }

        // ========= Save tests =========

        [Test]
        public async Task SaveAsync_WithNoType_ReturnsFalse()
        {
            bool result = await _vm.SaveAsync();
            Assert.IsFalse(result);
        }

        [Test]
        public async Task SaveAsync_WithPresetAndItems_DifferentName_CreatesNewMeal()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "DINNER", label = "Dinner" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(false, true);

            _vm.SelectMealPreset(new Meal { id = "existing-meal", name = "Mi cena" });
            _vm.MealContainerName = "Mi cena modificada";

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

            var createdMeal = new Meal { id = "new-meal", name = "Mi cena modificada" };
            _mockMealService
                .Setup(x => x.CreateMealAsync(It.Is<CreateMealRequest>(r => r.name == "Mi cena modificada")))
                .ReturnsAsync((createdMeal, null));

            _mockMealItemService
                .Setup(x => x.CreateAsync("new-meal", It.Is<CreateMealItemRequest>(r => r.foodProductId == prodItem.foodProductId)))
                .ReturnsAsync((new MealItem { id = "mi-1" }, null));
            _mockMealItemService
                .Setup(x => x.CreateAsync("new-meal", It.Is<CreateMealItemRequest>(r => r.genericFoodId == genItem.genericFoodId)))
                .ReturnsAsync((new MealItem { id = "mi-2" }, null));

            _mockMealLogService
                .Setup(x => x.CreateAsync(It.Is<CreateMealLogRequest>(r => r.mealId == "new-meal" && r.typeOfMeal == "DINNER")))
                .ReturnsAsync((new MealLog { id = "log-1" }, null));

            bool result = await _vm.SaveAsync();

            Assert.IsTrue(result);
            _mockMealService.Verify(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()), Times.Once);
            _mockMealItemService.Verify(x => x.CreateAsync("new-meal", It.IsAny<CreateMealItemRequest>()), Times.Exactly(2));
            _mockMealLogService.Verify(x => x.CreateAsync(It.IsAny<CreateMealLogRequest>()), Times.Once);
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
            _vm.MealContainerName = "New dinner";

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
            _mockMealService
                .Setup(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()))
                .ReturnsAsync((new Meal { id = "existing-meal" }, null));
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
            _vm.SelectedItems = new System.Collections.Generic.List<MealLogItem> { new MealLogItem { name = "Item 1" } };

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
        public async Task LoadTodayAsync_ParsesAndReturnsLastTen()
        {
            var response = new PaginatedMealLogResponse
            {
                data = new[]
                {
                    new MealLog { id = "1", typeOfMeal = "BREAKFAST", timestamp = "2026-06-04T09:00:00Z", meal = new Meal { name = "Toast" } },
                    new MealLog { id = "2", typeOfMeal = "BREAKFAST", timestamp = "2026-06-04T08:00:00Z", meal = new Meal { name = "Juice" } },
                    new MealLog { id = "3", typeOfMeal = "LUNCH", timestamp = "2026-06-04T13:00:00Z", meal = new Meal { name = "Salad" } }
                },
                total = 3,
                page = 1,
                limit = 20,
                totalPages = 1
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 50, null, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((response, null));

            await _vm.LoadTodayAsync();

            //Assert.IsTrue(_vm.TodayLoaded);
            Assert.AreEqual(3, _vm.LastTenLogs.Count);
            Assert.AreEqual("Salad", _vm.LastTenLogs[0].meal.name);
            Assert.AreEqual("BREAKFAST", _vm.LastTenLogs[2].typeOfMeal);
        }

        [Test]
        public async Task DeleteLogAsync_Success_RemovesFromLogs()
        {
            var response = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST", timestamp = "2026-06-04T09:00:00Z", meal = new Meal { name = "Toast" } } },
                total = 1,
                page = 1,
                limit = 20,
                totalPages = 1
            };
            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 50, null, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((response, null));

            await _vm.LoadTodayAsync();
            Assert.AreEqual(1, _vm.LastTenLogs.Count);

            _mockMealLogService
                .Setup(x => x.DeleteLogAsync("1"))
                .ReturnsAsync((true, null));

            await _vm.DeleteLogAsync("1");

            Assert.IsNull(_vm.ErrorDetail);
            Assert.AreEqual(0, _vm.LastTenLogs.Count);
        }

        [Test]
        public async Task DeleteLogAsync_WithApiError_SetsErrorDetail()
        {
            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 50, null, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((new PaginatedMealLogResponse { data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST", timestamp = "2026-06-04T09:00:00Z" } }, total = 1, page = 1, limit = 20, totalPages = 1 }, null));

            await _vm.LoadTodayAsync();

            var apiError = new ApiErrorResponse { statusCode = 500, message = "Delete failed" };
            _mockMealLogService
                .Setup(x => x.DeleteLogAsync("1"))
                .ReturnsAsync((false, apiError));

            await _vm.DeleteLogAsync("1");

            Assert.IsNotNull(_vm.ErrorDetail);
        }

        // ========= HasModifications tests =========

        [Test]
        public async Task HasModifications_WithNoChanges_ReturnsFalse()
        {
            var detail = new MealItemDetail
            {
                id = "i1",
                foodProductId = "fp-1",
                quantity = 2,
                unit = "G",
                itemType = "food_product",
                foodProduct = new MealItemFoodProduct { id = "fp-1", name = "Item1" },
            };
            _mockMealItemService
                .Setup(x => x.GetByMealIdAsync("existing-meal"))
                .ReturnsAsync((new[] { detail }, null));

            _vm.SelectMealPreset(new Meal { id = "existing-meal", name = "Test meal" });

            Assert.IsFalse(_vm.HasModifications());
        }

        [Test]
        public async Task HasModifications_WithDifferentQuantity_ReturnsTrue()
        {
            var detail = new MealItemDetail
            {
                id = "i1",
                foodProductId = "fp-1",
                quantity = 2,
                unit = "G",
                itemType = "food_product",
                foodProduct = new MealItemFoodProduct { id = "fp-1", name = "Item1" },
            };
            _mockMealItemService
                .Setup(x => x.GetByMealIdAsync("existing-meal"))
                .ReturnsAsync((new[] { detail }, null));

            _vm.SelectMealPreset(new Meal { id = "existing-meal", name = "Test meal" });
            _vm.SelectedItems[0].quantity = 5f;

            Assert.IsTrue(_vm.HasModifications());
        }

        [Test]
        public async Task HasModifications_WithRemovedItem_ReturnsTrue()
        {
            var detail = new MealItemDetail
            {
                id = "i1",
                foodProductId = "fp-1",
                quantity = 2,
                unit = "G",
                itemType = "food_product",
                foodProduct = new MealItemFoodProduct { id = "fp-1", name = "Item1" },
            };
            _mockMealItemService
                .Setup(x => x.GetByMealIdAsync("existing-meal"))
                .ReturnsAsync((new[] { detail }, null));

            _vm.SelectMealPreset(new Meal { id = "existing-meal", name = "Test meal" });
            _vm.SelectedItems = new System.Collections.Generic.List<MealLogItem>();

            Assert.IsTrue(_vm.HasModifications());
        }

        // ========= SaveAsync preset scenarios =========

        [Test]
        public async Task SaveAsync_WithPresetNoModifications_LogsDirectly()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "LUNCH", label = "Lunch" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(true, false);

            var detail = new MealItemDetail
            {
                id = "i1",
                foodProductId = "fp-1",
                quantity = 2,
                unit = "G",
                itemType = "food_product",
                foodProduct = new MealItemFoodProduct { id = "fp-1", name = "Item1" },
            };
            _mockMealItemService
                .Setup(x => x.GetByMealIdAsync("existing-meal"))
                .ReturnsAsync((new[] { detail }, null));

            _vm.SelectMealPreset(new Meal { id = "existing-meal", name = "Test meal" });

            _mockMealLogService
                .Setup(x => x.CreateAsync(It.Is<CreateMealLogRequest>(r => r.mealId == "existing-meal")))
                .ReturnsAsync((new MealLog { id = "log-1" }, null));

            bool result = await _vm.SaveAsync();

            Assert.IsTrue(result);
            _mockMealService.Verify(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()), Times.Never);
            _mockMealItemService.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<CreateMealItemRequest>()), Times.Never);
            _mockMealLogService.Verify(x => x.CreateAsync(It.IsAny<CreateMealLogRequest>()), Times.Once);
        }

        [Test]
        public async Task SaveAsync_WithPresetModificationsAndSameName_FiresEvent()
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
            _vm.SelectedItems = new System.Collections.Generic.List<MealLogItem> { prodItem };

            string capturedName = null;
            _vm.OnConfirmUpdateRequired += name => capturedName = name;

            bool result = await _vm.SaveAsync();

            Assert.IsFalse(result);
            Assert.AreEqual("Mi cena", capturedName);
            _mockMealService.Verify(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()), Times.Never);
        }

        [Test]
        public async Task SaveAsync_WithRecipeAndName_CreatesMealWithRecipeId()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "DINNER", label = "Dinner" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(true, false);

            _mockRecipeService.Setup(r => r.GetRecipeAsync("recipe-1"))
                .ReturnsAsync((new Recipe { id = "recipe-1", ingredients = new[] { new RecipeIngredient { foodProductId = "fp-1", name = "Ing 1" } } }, null));

            _vm.MealContainerName = "Recipe dinner";
            await _vm.SelectMealPreset(new Meal { id = "recipe-1", name = "My Recipe", recipeId = "recipe-1", isRecipe = true });

            var createdMeal = new Meal { id = "new-meal", name = "Recipe dinner" };
            _mockMealService
                .Setup(x => x.CreateMealAsync(It.Is<CreateMealRequest>(r => r.name == "Recipe dinner" && r.recipeId == "recipe-1")))
                .ReturnsAsync((createdMeal, null));

            _mockMealLogService
                .Setup(x => x.CreateAsync(It.Is<CreateMealLogRequest>(r => r.mealId == "new-meal")))
                .ReturnsAsync((new MealLog { id = "log-1" }, null));

            bool result = await _vm.SaveAsync();

            Assert.IsTrue(result);
            _mockMealService.Verify(x => x.CreateMealAsync(It.Is<CreateMealRequest>(r => r.recipeId == "recipe-1")), Times.Once);
        }

        [Test]
        public async Task SaveAsync_WithNoPresetAndEmptyName_ShowsError()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "LUNCH", label = "Lunch" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(true, false);

            bool result = await _vm.SaveAsync();

            Assert.IsFalse(result);
            Assert.IsNotEmpty(_vm.ErrorMessage);
        }

        [Test]
        public async Task ConfirmUpdateAndSaveAsync_UpdatesExistingMeal()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "DINNER", label = "Dinner" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(false, true);

            var detail = new MealItemDetail
            {
                id = "item-1",
                foodProductId = "fp-1",
                quantity = 2,
                unit = "G",
                itemType = "food_product",
                foodProduct = new MealItemFoodProduct { id = "fp-1", name = "Arroz" },
            };
            _mockMealItemService
                .Setup(x => x.GetByMealIdAsync("existing-meal"))
                .ReturnsAsync((new[] { detail }, null));

            _vm.SelectMealPreset(new Meal { id = "existing-meal", name = "Mi cena" });

            MealLogItem prodItem = _vm.SelectedItems[0];
            prodItem.quantity = 3f;

            _mockMealItemService
                .Setup(x => x.CreateAsync("existing-meal", It.IsAny<CreateMealItemRequest>()))
                .ReturnsAsync((new MealItem { id = "new-item" }, null));
            _mockMealItemService
                .Setup(x => x.UpdateAsync("existing-meal", "item-1", It.Is<CreateMealItemRequest>(r => r.quantity == 3)))
                .ReturnsAsync((new MealItem { id = "item-1" }, null));

            _mockMealLogService
                .Setup(x => x.CreateAsync(It.Is<CreateMealLogRequest>(r => r.mealId == "existing-meal")))
                .ReturnsAsync((new MealLog { id = "log-1" }, null));

            bool result = await _vm.ConfirmUpdateAndSaveAsync();

            Assert.IsTrue(result);
            _mockMealItemService.Verify(x => x.UpdateAsync("existing-meal", "item-1", It.Is<CreateMealItemRequest>(r => r.quantity == 3)), Times.Once);
            _mockMealLogService.Verify(x => x.CreateAsync(It.IsAny<CreateMealLogRequest>()), Times.Once);
        }

        [Test]
        public async Task SaveAsync_MealFromPantryFalse_DoesNotTouchPantry()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "LUNCH", label = "Lunch" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(fromPantry: false, eatenOut: false);
            _vm.SelectedItems.Add(new MealLogItem { isProduct = true, foodProductId = "fp-1", quantity = 2f, unit = "PIECES" });

            _mockMealService.Setup(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()))
                .ReturnsAsync((new Meal { id = "meal-1" }, null));
            _mockMealItemService.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<CreateMealItemRequest>()))
                .ReturnsAsync((new MealItem { id = "mi-1" }, null));
            _mockMealLogService.Setup(x => x.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog { id = "log-1" }, null));

            bool result = await _vm.SaveAsync();

            Assert.IsTrue(result);
            _mockPantryService.Verify(x => x.GetItemsAsync(), Times.Never);
        }

        [Test]
        public async Task SaveAsync_MealFromPantryTrue_DeductsQuantityWhenUnitsMatch()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "LUNCH", label = "Lunch" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(fromPantry: true, eatenOut: false);
            _vm.SelectedItems.Add(new MealLogItem { isProduct = true, foodProductId = "fp-1", quantity = 2f, unit = "PIECES" });

            var pantryItem = new PantryItem
            {
                id = "pantry-1",
                foodProductId = "fp-1",
                quantity = 5f,
                unit = "PIECES",
                expiryDate = "2026-08-01"
            };

            _mockPantryService.Setup(x => x.GetItemsAsync())
                .ReturnsAsync((new[] { pantryItem }, null));
            _mockPantryService.Setup(x => x.UpdateItemAsync("pantry-1", 3f, "PIECES", null, null, "2026-08-01", null, null))
                .ReturnsAsync((pantryItem, null));

            _mockMealService.Setup(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()))
                .ReturnsAsync((new Meal { id = "meal-1" }, null));
            _mockMealItemService.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<CreateMealItemRequest>()))
                .ReturnsAsync((new MealItem { id = "mi-1" }, null));
            _mockMealLogService.Setup(x => x.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog { id = "log-1" }, null));

            bool result = await _vm.SaveAsync();

            Assert.IsTrue(result);
            _mockPantryService.Verify(x => x.GetItemsAsync(), Times.Once);
            _mockPantryService.Verify(x => x.UpdateItemAsync("pantry-1", 3f, "PIECES", null, null, "2026-08-01", null, null), Times.Once);
        }

        [Test]
        public async Task SaveAsync_MealFromPantryTrue_DeletesEarliestExpiringOnUnitMismatch()
        {
            _vm.TypeOfMealOptions = new[] { new CatalogItem { code = "LUNCH", label = "Lunch" } };
            _vm.SelectTypeOfMeal(0);
            _vm.SetSource(fromPantry: true, eatenOut: false);
            _vm.SelectedItems.Add(new MealLogItem { isGenericFood = true, genericFoodId = "gf-1", quantity = 200f, unit = "G" });

            var item1 = new PantryItem { id = "pantry-1", genericFoodId = "gf-1", quantity = 1f, unit = "PIECES", expiryDate = "2026-08-10" };
            var item2 = new PantryItem { id = "pantry-2", genericFoodId = "gf-1", quantity = 1f, unit = "PIECES", expiryDate = "2026-07-25" };

            _mockPantryService.Setup(x => x.GetItemsAsync())
                .ReturnsAsync((new[] { item1, item2 }, null));
            _mockPantryService.Setup(x => x.DeleteItemAsync("pantry-2"))
                .ReturnsAsync((true, null));

            _mockMealService.Setup(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()))
                .ReturnsAsync((new Meal { id = "meal-1" }, null));
            _mockMealItemService.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<CreateMealItemRequest>()))
                .ReturnsAsync((new MealItem { id = "mi-1" }, null));
            _mockMealLogService.Setup(x => x.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog { id = "log-1" }, null));

            bool result = await _vm.SaveAsync();

            Assert.IsTrue(result);
            _mockPantryService.Verify(x => x.DeleteItemAsync("pantry-2"), Times.Once);
            _mockPantryService.Verify(x => x.DeleteItemAsync("pantry-1"), Times.Never);
        }

        [Test]
        public async Task LoadRecipePresetAsync_OnSuccess_CallsSelectMealPresetWithConstructedMeal()
        {
            _mockRecipeService.Setup(s => s.GetRecipeAsync("r1"))
                .ReturnsAsync((new Recipe
                {
                    id = "r1",
                    title = "Pasta",
                    ingredients = new[] { new RecipeIngredient { name = "Pasta", genericFoodId = "gf1", measure = "200g" } }
                }, null));

            await _vm.LoadRecipePresetAsync("r1");

            Assert.IsNotNull(_vm.SelectedMealPreset);
            Assert.AreEqual("Pasta", _vm.SelectedMealPreset.name);
            Assert.IsTrue(_vm.SelectedMealPreset.isRecipe);
            Assert.AreEqual("r1", _vm.SelectedMealPreset.recipeId);
        }
    }
}
