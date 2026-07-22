using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class PantryViewModelTests
    {
        private Mock<IPantryService> _mockPantryService;
        private Mock<IFoodProductService> _mockFoodProductService;
        private Mock<IGenericFoodService> _mockGenericFoodService;
        private Mock<ILocalStorageService> _mockLocalStorage;
        private Mock<IOpenFoodFactsClientService> _mockOpenFoodFactsClient;
        private TestStoreService _storeService;
        private PantryViewModel _vm;
        private System.Func<bool> _originalOverride;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;
            _mockPantryService = new Mock<IPantryService>();
            _mockFoodProductService = new Mock<IFoodProductService>();
            _mockGenericFoodService = new Mock<IGenericFoodService>();
            _mockLocalStorage = new Mock<ILocalStorageService>();
            _mockOpenFoodFactsClient = new Mock<IOpenFoodFactsClientService>();
            _storeService = new TestStoreService();
            _vm = new PantryViewModel(
                _storeService,
                _mockPantryService.Object,
                _mockFoodProductService.Object,
                _mockGenericFoodService.Object,
                _mockLocalStorage.Object,
                _mockOpenFoodFactsClient.Object);
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesWithDefaults()
        {
            Assert.IsNotNull(_vm.Items);
            Assert.AreEqual(0, _vm.Items.Count);
            Assert.IsFalse(_vm.IsLoading);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.AreEqual("", _vm.FilterText);
            Assert.AreEqual(0, _vm.ExpiredItemCount);
            Assert.IsFalse(_vm.HasExpiredItems);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task ApplyFilter_WithEmptyFilter_ReturnsAllItems()
        {
            PantryItem[] items = new[]
            {
                new PantryItem { id = "1", foodProductId = "Apple", quantity = 2, unit = "kg" },
                new PantryItem { id = "2", foodProductId = "Banana", quantity = 1, unit = "kg" },
            };
            _mockPantryService
                .Setup(x => x.GetPantryAsync())
                .Returns(Task.FromResult<(Pantry Result, ApiErrorResponse Error)>((new Pantry { id = "p1", items = items }, null)));
            _mockPantryService
                .Setup(x => x.GetExpiredItemsAsync())
                .Returns(Task.FromResult<(ExpiredPantryItem[] Result, ApiErrorResponse Error)>((Array.Empty<ExpiredPantryItem>(), null)));
            _mockFoodProductService
                .Setup(x => x.GetFoodByIdAsync("Apple"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((new FoodProduct { id = "Apple", name = "Apple" }, null)));
            _mockFoodProductService
                .Setup(x => x.GetFoodByIdAsync("Banana"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((new FoodProduct { id = "Banana", name = "Banana" }, null)));
            _mockLocalStorage
                .Setup(x => x.GetValue<PantryItemArrayWrapper>(It.IsAny<string>(), It.IsAny<PantryItemArrayWrapper>()))
                .Returns((PantryItemArrayWrapper)null);

            await _vm.LoadAsync();

            Assert.AreEqual(2, _vm.Items.Count);
        }

        [Test]
        public async Task ApplyFilter_WithText_FiltersItems()
        {
            PantryItem[] items = new[]
            {
                new PantryItem { id = "1", foodProductId = "Apple", quantity = 2, unit = "kg" },
                new PantryItem { id = "2", foodProductId = "Banana", quantity = 1, unit = "kg" },
            };
            _mockPantryService
                .Setup(x => x.GetPantryAsync())
                .Returns(Task.FromResult<(Pantry Result, ApiErrorResponse Error)>((new Pantry { id = "p1", items = items }, null)));
            _mockPantryService
                .Setup(x => x.GetExpiredItemsAsync())
                .Returns(Task.FromResult<(ExpiredPantryItem[] Result, ApiErrorResponse Error)>((Array.Empty<ExpiredPantryItem>(), null)));
            _mockFoodProductService
                .Setup(x => x.GetFoodByIdAsync("Apple"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((new FoodProduct { id = "Apple", name = "Apple" }, null)));
            _mockFoodProductService
                .Setup(x => x.GetFoodByIdAsync("Banana"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((new FoodProduct { id = "Banana", name = "Banana" }, null)));
            _mockLocalStorage
                .Setup(x => x.GetValue<PantryItemArrayWrapper>(It.IsAny<string>(), It.IsAny<PantryItemArrayWrapper>()))
                .Returns((PantryItemArrayWrapper)null);

            await _vm.LoadAsync();
            _vm.FilterText = "Apple";
            _vm.ApplyFilter();

            Assert.AreEqual(1, _vm.Items.Count);
            Assert.AreEqual("Apple", _vm.Items[0].DisplayName);
        }

        [Test]
        public async Task LoadAsync_WithApiErrorAndNoCache_SetsErrorMessage()
        {
            _mockPantryService
                .Setup(x => x.GetPantryAsync())
                .Returns(Task.FromResult<(Pantry Result, ApiErrorResponse Error)>(((Pantry)null, new ApiErrorResponse { statusCode = 500, message = "Server error" })));
            _mockPantryService
                .Setup(x => x.GetExpiredItemsAsync())
                .Returns(Task.FromResult<(ExpiredPantryItem[] Result, ApiErrorResponse Error)>(((ExpiredPantryItem[])null, null)));
            _mockLocalStorage
                .Setup(x => x.GetValue<PantryItemArrayWrapper>(It.IsAny<string>(), It.IsAny<PantryItemArrayWrapper>()))
                .Returns((PantryItemArrayWrapper)null);

            await _vm.LoadAsync();

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task LoadAsync_WithApiErrorAndCache_UsesCache()
        {
            PantryItem cachedItem = new PantryItem { id = "cached1", foodProductId = "fp1", quantity = 2, unit = "kg" };
            _mockPantryService
                .Setup(x => x.GetPantryAsync())
                .Returns(Task.FromResult<(Pantry Result, ApiErrorResponse Error)>(((Pantry)null, new ApiErrorResponse { statusCode = 500 })));
            _mockPantryService
                .Setup(x => x.GetExpiredItemsAsync())
                .Returns(Task.FromResult<(ExpiredPantryItem[] Result, ApiErrorResponse Error)>(((ExpiredPantryItem[])null, null)));
            _mockLocalStorage
                .Setup(x => x.GetValue<PantryItemArrayWrapper>(It.IsAny<string>(), It.IsAny<PantryItemArrayWrapper>()))
                .Returns(new PantryItemArrayWrapper { items = new[] { cachedItem } });

            await _vm.LoadAsync();

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task LoadAsync_WithExpiredItems_SetsExpiredItemCount()
        {
            _mockPantryService
                .Setup(x => x.GetPantryAsync())
                .Returns(Task.FromResult<(Pantry Result, ApiErrorResponse Error)>((new Pantry { id = "p1", items = Array.Empty<PantryItem>() }, null)));
            _mockPantryService
                .Setup(x => x.GetExpiredItemsAsync())
                .Returns(Task.FromResult<(ExpiredPantryItem[] Result, ApiErrorResponse Error)>((new ExpiredPantryItem[]
                {
                    new ExpiredPantryItem { pantryItemId = "e1", quantity = 1, unit = "kg" }
                }, null)));

            await _vm.LoadAsync();

            Assert.AreEqual(1, _vm.ExpiredItemCount);
            Assert.IsTrue(_vm.HasExpiredItems);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task DeleteItemAsync_OnSuccess_RemovesItemFromList()
        {
            _mockPantryService
                .Setup(x => x.DeleteItemAsync("item1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));

            _vm.Items = new List<PantryItemView>
            {
                new PantryItemView { Item = new PantryItem { id = "item1" }, DisplayName = "Apple" },
                new PantryItemView { Item = new PantryItem { id = "item2" }, DisplayName = "Banana" },
            };

            await _vm.DeleteItemAsync("item1");

            Assert.IsNull(_vm.ErrorDetail);
            _mockPantryService.Verify(x => x.DeleteItemAsync("item1"), Times.Once);
        }

        [Test]
        public async Task DeleteItemAsync_WithApiError_SetsErrorDetail()
        {
            _mockPantryService
                .Setup(x => x.DeleteItemAsync("item1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((false, new ApiErrorResponse { statusCode = 500, message = "Delete failed" })));

            await _vm.DeleteItemAsync("item1");

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual(500, _vm.ErrorDetail.statusCode);
        }

        [Test]
        public async Task SearchFoodsAsync_WithEmptyQuery_ReturnsEmpty()
        {
            List<OpenFoodFactsProduct> result = await _vm.SearchFoodsAsync("");

            Assert.AreEqual(0, result.Count);
            _mockFoodProductService.Verify(
                x => x.SearchOpenFoodFactsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test]
        public async Task SearchFoodsAsync_WithCachedResult_UsesCache()
        {
            _mockLocalStorage
                .Setup(x => x.GetValue<CachedFoodSearch>(It.IsAny<string>(), It.IsAny<CachedFoodSearch>()))
                .Returns(new CachedFoodSearch
                {
                    data = new OpenFoodFactsSearchResponse
                    {
                        products = new OpenFoodFactsProduct[]
                        {
                            new OpenFoodFactsProduct { barcode = "123", name = "CachedProduct" }
                        }
                    },
                    cachedAtTicks = DateTime.UtcNow.Ticks
                });

            List<OpenFoodFactsProduct> result = await _vm.SearchFoodsAsync("test");

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("CachedProduct", result[0].name);
            _mockFoodProductService.Verify(
                x => x.SearchOpenFoodFactsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test]
        public async Task GetGenericFoodsAsync_OnSuccess_ReturnsItems()
        {
            _mockGenericFoodService
                .Setup(x => x.SearchGenericFoodsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult<(PaginatedGenericFoodResponse Result, ApiErrorResponse Error)>((new PaginatedGenericFoodResponse
                {
                    items = new GenericFood[]
                    {
                        new GenericFood { id = "g1", foodName = "Rice", foodGroup = "Grains" }
                    }
                }, null)));

            List<GenericFood> result = await _vm.GetGenericFoodsAsync();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Rice", result[0].foodName);
        }

        [Test]
        public async Task GetGenericFoodsAsync_WithApiError_ReturnsEmpty()
        {
            _mockGenericFoodService
                .Setup(x => x.SearchGenericFoodsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult<(PaginatedGenericFoodResponse Result, ApiErrorResponse Error)>(((PaginatedGenericFoodResponse)null, new ApiErrorResponse { statusCode = 500 })));

            List<GenericFood> result = await _vm.GetGenericFoodsAsync();

            Assert.AreEqual(0, result.Count);
            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task ImportByBarcodeAsync_OnSuccess_ReturnsFoodProduct()
        {
            FoodProduct expected = new FoodProduct { id = "fp1", name = "Product", barcode = "123" };
            _mockFoodProductService
                .Setup(x => x.FindByBarcodeAsync("123", false))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>(((FoodProduct)null, null)));
            _mockFoodProductService
                .Setup(x => x.ImportFromBarcodeAsync("123"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((expected, null)));

            var (result, error) = await _vm.ImportByBarcodeAsync("123");

            Assert.IsNull(error);
            Assert.IsNotNull(result);
            Assert.AreEqual("fp1", result.id);
        }

        [Test]
        public async Task ImportByBarcodeAsync_LocalNotFound_ImportsFromOff()
        {
            FoodProduct imported = new FoodProduct { id = "fp3", name = "Imported", barcode = "123" };
            _mockFoodProductService
                .Setup(x => x.FindByBarcodeAsync("123", false))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>(((FoodProduct)null, null)));
            _mockFoodProductService
                .Setup(x => x.ImportFromBarcodeAsync("123"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((imported, null)));

            var (result, error) = await _vm.ImportByBarcodeAsync("123");

            Assert.IsNull(error);
            Assert.IsNotNull(result);
            Assert.AreEqual("fp3", result.id);
        }

        [Test]
        public async Task ImportByBarcodeAsync_LocalNotFound_ImportFails_FindsWithOff()
        {
            FoodProduct fallback = new FoodProduct { id = "fp2", name = "Existing", barcode = "123" };
            _mockFoodProductService
                .Setup(x => x.FindByBarcodeAsync("123", false))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>(((FoodProduct)null, null)));
            _mockFoodProductService
                .Setup(x => x.ImportFromBarcodeAsync("123"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>(((FoodProduct)null, new ApiErrorResponse { statusCode = 400 })));
            _mockFoodProductService
                .Setup(x => x.FindByBarcodeAsync("123", true))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((fallback, null)));

            var (result, error) = await _vm.ImportByBarcodeAsync("123");

            Assert.IsNull(error);
            Assert.IsNotNull(result);
            Assert.AreEqual("fp2", result.id);
        }
    }
}
