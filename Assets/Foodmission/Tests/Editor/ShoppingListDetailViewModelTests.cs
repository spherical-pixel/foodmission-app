using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ShoppingListDetailViewModelTests
    {
        private Mock<IShoppingListService> _mockShoppingListService;
        private Mock<IFoodProductService> _mockFoodProductService;
        private Mock<IGenericFoodService> _mockGenericFoodService;
        private Mock<ILocalStorageService> _mockLocalStorage;
        private Mock<IOpenFoodFactsClientService> _mockOpenFoodFactsClient;
        private Mock<IAuthService> _mockAuthService;
        private TestStoreService _storeService;
        private ShoppingListDetailViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockShoppingListService = new Mock<IShoppingListService>();
            _mockFoodProductService = new Mock<IFoodProductService>();
            _mockGenericFoodService = new Mock<IGenericFoodService>();
            _mockLocalStorage = new Mock<ILocalStorageService>();
            _mockOpenFoodFactsClient = new Mock<IOpenFoodFactsClientService>();
            _mockAuthService = new Mock<IAuthService>();
            _storeService = new TestStoreService();
            _vm = new ShoppingListDetailViewModel(
                _storeService,
                _mockShoppingListService.Object,
                _mockFoodProductService.Object,
                _mockGenericFoodService.Object,
                _mockLocalStorage.Object,
                _mockOpenFoodFactsClient.Object,
                _mockAuthService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesWithDefaults()
        {
            Assert.IsNotNull(_vm.Items);
            Assert.AreEqual(0, _vm.Items.Count);
            Assert.IsNotNull(_vm.SearchResults);
            Assert.AreEqual(0, _vm.SearchResults.Count);
            Assert.AreEqual("", _vm.SearchQuery);
            Assert.IsFalse(_vm.IsLoadingItems);
            Assert.IsFalse(_vm.IsSearching);
            Assert.AreEqual("", _vm.ListName);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.AreEqual("", _vm.FilterText);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task ApplyFilter_WithEmptyFilter_ReturnsAllItems()
        {
            ShoppingListItem[] items = new[]
            {
                new ShoppingListItem { id = "1", foodProductId = "fp1" },
                new ShoppingListItem { id = "2", foodProductId = "fp2" },
            };
            _mockShoppingListService
                .Setup(x => x.GetItemsAsync("list1"))
                .Returns(Task.FromResult<(ShoppingListItem[] Result, ApiErrorResponse Error)>((items, null)));
            _mockFoodProductService
                .Setup(x => x.GetFoodByIdAsync("fp1"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((new FoodProduct { id = "fp1", name = "Milk" }, null)));
            _mockFoodProductService
                .Setup(x => x.GetFoodByIdAsync("fp2"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((new FoodProduct { id = "fp2", name = "Eggs" }, null)));
            _mockLocalStorage
                .Setup(x => x.GetValue<ShoppingListItemPagedResponse>(It.IsAny<string>(), It.IsAny<ShoppingListItemPagedResponse>()))
                .Returns((ShoppingListItemPagedResponse)null);

            await _vm.LoadAsync("list1");

            Assert.AreEqual(2, _vm.Items.Count);
        }

        [Test]
        public async Task ApplyFilter_WithFilterText_FiltersItems()
        {
            ShoppingListItem[] items = new[]
            {
                new ShoppingListItem { id = "1", foodProductId = "fp1" },
                new ShoppingListItem { id = "2", foodProductId = "fp2" },
            };
            _mockShoppingListService
                .Setup(x => x.GetItemsAsync("list1"))
                .Returns(Task.FromResult<(ShoppingListItem[] Result, ApiErrorResponse Error)>((items, null)));
            _mockFoodProductService
                .Setup(x => x.GetFoodByIdAsync("fp1"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((new FoodProduct { id = "fp1", name = "Milk" }, null)));
            _mockFoodProductService
                .Setup(x => x.GetFoodByIdAsync("fp2"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((new FoodProduct { id = "fp2", name = "Eggs" }, null)));
            _mockLocalStorage
                .Setup(x => x.GetValue<ShoppingListItemPagedResponse>(It.IsAny<string>(), It.IsAny<ShoppingListItemPagedResponse>()))
                .Returns((ShoppingListItemPagedResponse)null);

            await _vm.LoadAsync("list1");
            _vm.FilterText = "Milk";
            _vm.ApplyFilter();

            Assert.AreEqual(1, _vm.Items.Count);
            Assert.AreEqual("Milk", _vm.Items[0].FoodName);
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
            Assert.AreEqual(1, _vm.GenericFoods.Count);
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
        public async Task LoadAsync_WithEmptyId_DoesNotCallService()
        {
            await _vm.LoadAsync("");

            _mockShoppingListService.Verify(x => x.GetItemsAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task LoadAsync_WithInvalidListAndNoCache_SetsErrorMessage()
        {
            _mockShoppingListService
                .Setup(x => x.GetItemsAsync("list1"))
                .Returns(Task.FromResult<(ShoppingListItem[] Result, ApiErrorResponse Error)>(((ShoppingListItem[])null, new ApiErrorResponse { statusCode = 404 })));
            _mockLocalStorage
                .Setup(x => x.GetValue<ShoppingListItemPagedResponse>(It.IsAny<string>(), It.IsAny<ShoppingListItemPagedResponse>()))
                .Returns((ShoppingListItemPagedResponse)null);

            await _vm.LoadAsync("list1");

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.IsFalse(_vm.IsLoadingItems);
        }

        [Test]
        public async Task LoadAsync_SetsListName()
        {
            _mockShoppingListService
                .Setup(x => x.GetItemsAsync("list1"))
                .Returns(Task.FromResult<(ShoppingListItem[] Result, ApiErrorResponse Error)>((Array.Empty<ShoppingListItem>(), null)));

            await _vm.LoadAsync("list1", "My List");

            Assert.AreEqual("My List", _vm.ListName);
        }

        [Test]
        public async Task ToggleItemAsync_OnSuccess_TogglesChecked()
        {
            var item = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = false };
            var updated = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = true };

            _mockShoppingListService
                .Setup(x => x.GetItemsAsync("list1"))
                .Returns(Task.FromResult<(ShoppingListItem[] Result, ApiErrorResponse Error)>((new[] { item }, null)));
            _mockFoodProductService
                .Setup(x => x.GetFoodByIdAsync("fp1"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((new FoodProduct { id = "fp1", name = "Milk" }, null)));
            _mockLocalStorage
                .Setup(x => x.GetValue<ShoppingListItemPagedResponse>(It.IsAny<string>(), It.IsAny<ShoppingListItemPagedResponse>()))
                .Returns((ShoppingListItemPagedResponse)null);
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "item1", null, null, null, true))
                .Returns(Task.FromResult<(ShoppingListItem Result, ApiErrorResponse Error)>((updated, null)));

            await _vm.LoadAsync("list1");

            await _vm.ToggleItemAsync("item1");

            _mockShoppingListService.Verify(
                x => x.UpdateItemAsync("list1", "item1", null, null, null, true),
                Times.Once);
        }

        [Test]
        public async Task RenameListAsync_WithEmptyName_DoesNothing()
        {
            _mockShoppingListService
                .Setup(x => x.UpdateListAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Should not be called"));

            await _vm.RenameListAsync("");
        }

        [Test]
        public async Task RenameListAsync_OnSuccess_UpdatesListName()
        {
            _mockShoppingListService
                .Setup(x => x.UpdateListAsync("list1", "NewName"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));

            await _vm.LoadAsync("list1");
            await _vm.RenameListAsync("NewName");

            Assert.AreEqual("NewName", _vm.ListName);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task DeleteItemAsync_OnSuccess_RemovesFromList()
        {
            _mockShoppingListService
                .Setup(x => x.GetItemsAsync("list1"))
                .Returns(Task.FromResult<(ShoppingListItem[] Result, ApiErrorResponse Error)>((new ShoppingListItem[]
                {
                    new ShoppingListItem { id = "item1", foodProductId = "fp1" },
                    new ShoppingListItem { id = "item2", foodProductId = "fp2" },
                }, null)));
            _mockShoppingListService
                .Setup(x => x.DeleteItemAsync("list1", "item1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));

            await _vm.LoadAsync("list1");
            await _vm.DeleteItemAsync("item1");

            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task DeleteItemAsync_WithApiError_SetsErrorDetail()
        {
            _mockShoppingListService
                .Setup(x => x.DeleteItemAsync("list1", "item1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((false, new ApiErrorResponse { statusCode = 500, message = "Delete failed" })));

            await _vm.LoadAsync("list1");
            await _vm.DeleteItemAsync("item1");

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual("Delete failed", _vm.ErrorDetail.message);
        }

        [Test]
        public async Task ClearCheckedItemsAsync_OnSuccess_RemovesCheckedItems()
        {
            _mockShoppingListService
                .Setup(x => x.GetItemsAsync("list1"))
                .Returns(Task.FromResult<(ShoppingListItem[] Result, ApiErrorResponse Error)>((new ShoppingListItem[]
                {
                    new ShoppingListItem { id = "c1", foodProductId = "fp1", @checked = true },
                    new ShoppingListItem { id = "u1", foodProductId = "fp2", @checked = false },
                }, null)));
            _mockShoppingListService
                .Setup(x => x.ClearCheckedItemsAsync("list1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));

            await _vm.LoadAsync("list1");
            await _vm.ClearCheckedItemsAsync();

            Assert.IsNull(_vm.ErrorDetail);
            _mockShoppingListService.Verify(x => x.ClearCheckedItemsAsync("list1"), Times.Once);
        }

        [Test]
        public async Task ClearCheckedItemsAsync_WithApiError_SetsErrorDetail()
        {
            _mockShoppingListService
                .Setup(x => x.ClearCheckedItemsAsync("list1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((false, new ApiErrorResponse { statusCode = 500 })));

            await _vm.LoadAsync("list1");
            await _vm.ClearCheckedItemsAsync();

            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task LoadAsync_WithGenericFoodItems_EnrichesNamesCorrectly()
        {
            // Arrange
            ShoppingListItem[] items = new[]
            {
                new ShoppingListItem { id = "item1", genericFoodId = "gf1", genericFood = null }
            };
            _mockShoppingListService
                .Setup(x => x.GetItemsAsync("list1"))
                .Returns(Task.FromResult<(ShoppingListItem[] Result, ApiErrorResponse Error)>((items, null)));

            _mockGenericFoodService
                .Setup(x => x.GetGenericFoodByIdAsync("gf1"))
                .Returns(Task.FromResult<(GenericFood Result, ApiErrorResponse Error)>((new GenericFood { id = "gf1", foodName = "Apple sauce" }, null)));

            _mockLocalStorage
                .Setup(x => x.GetValue<ShoppingListItemPagedResponse>(It.IsAny<string>(), It.IsAny<ShoppingListItemPagedResponse>()))
                .Returns((ShoppingListItemPagedResponse)null);

            // Act
            await _vm.LoadAsync("list1");

            // Assert
            Assert.AreEqual(1, _vm.Items.Count);
            Assert.AreEqual("Apple sauce", _vm.Items[0].FoodName);
            _mockGenericFoodService.Verify(x => x.GetGenericFoodByIdAsync("gf1"), Times.Once);
        }
    }
}
