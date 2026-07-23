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
        private Mock<IPantryService> _mockPantryService;
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
            _mockPantryService = new Mock<IPantryService>();
            _storeService = new TestStoreService();
            _vm = new ShoppingListDetailViewModel(
                _storeService,
                _mockShoppingListService.Object,
                _mockFoodProductService.Object,
                _mockGenericFoodService.Object,
                _mockLocalStorage.Object,
                _mockOpenFoodFactsClient.Object,
                _mockAuthService.Object,
                _mockPantryService.Object);
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

        private void SetupMockItems(ShoppingListItem[] items)
        {
            _mockShoppingListService
                .Setup(x => x.GetItemsAsync("list1"))
                .Returns(Task.FromResult<(ShoppingListItem[] Result, ApiErrorResponse Error)>((items, null)));
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.foodProductId))
                {
                    _mockFoodProductService
                        .Setup(x => x.GetFoodByIdAsync(item.foodProductId))
                        .Returns(Task.FromResult<(FoodProduct, ApiErrorResponse)>((new FoodProduct { id = item.foodProductId, name = item.foodProductId + "-name" }, null)));
                }
                if (!string.IsNullOrEmpty(item.genericFoodId))
                {
                    _mockGenericFoodService
                        .Setup(x => x.GetGenericFoodByIdAsync(item.genericFoodId))
                        .Returns(Task.FromResult<(GenericFood, ApiErrorResponse)>((new GenericFood { id = item.genericFoodId, foodName = item.genericFoodId + "-name" }, null)));
                }
            }
            _mockLocalStorage
                .Setup(x => x.GetValue<ShoppingListItemPagedResponse>(It.IsAny<string>(), It.IsAny<ShoppingListItemPagedResponse>()))
                .Returns((ShoppingListItemPagedResponse)null);
        }

        [Test]
        public void AutoAddToPantry_LoadsFromAppState()
        {
            var state = _storeService.GetAppState();
            state.userAutoAddToPantry = true;
            _storeService.SetAppState(state);

            _vm.Dispose();
            _vm = new ShoppingListDetailViewModel(
                _storeService,
                _mockShoppingListService.Object,
                _mockFoodProductService.Object,
                _mockGenericFoodService.Object,
                _mockLocalStorage.Object,
                _mockOpenFoodFactsClient.Object,
                _mockAuthService.Object,
                _mockPantryService.Object);

            Assert.IsTrue(_vm.AutoAddToPantry);
        }

        [Test]
        public async Task ToggleItemAsync_WhenAutoAddEnabled_AndChecked_AddsToPantry()
        {
            var item = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 2, unit = "pcs", @checked = false };
            var updated = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 2, unit = "pcs", @checked = true };
            var pantryItem = new PantryItem { id = "p1", foodProductId = "fp1" };

            SetupMockItems(new[] { item });
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "item1", null, null, null, true))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((updated, null)));
            _mockPantryService
                .Setup(x => x.AddItemAsync("fp1", null, 2f, "pcs", null, null, null))
                .Returns(Task.FromResult<(PantryItem, ApiErrorResponse)>((pantryItem, null)));

            _vm.AutoAddToPantry = true;
            await _vm.LoadAsync("list1");

            await _vm.ToggleItemAsync("item1");

            _mockPantryService.Verify(x => x.AddItemAsync("fp1", null, 2f, "pcs", null, null, null), Times.Once);
            _mockShoppingListService.Verify(x => x.UpdateItemAsync("list1", "item1", null, null, null, true), Times.Once);
        }

        [Test]
        public async Task ToggleItemAsync_WhenAutoAddDisabled_DoesNotAddToPantry()
        {
            var item = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = false };
            var updated = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = true };

            SetupMockItems(new[] { item });
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "item1", null, null, null, true))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((updated, null)));

            _vm.AutoAddToPantry = false;
            await _vm.LoadAsync("list1");

            await _vm.ToggleItemAsync("item1");

            _mockPantryService.Verify(x => x.AddItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ToggleItemAsync_WhenUnchecking_DoesNotRemoveFromPantry()
        {
            var item = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = true };
            var updated = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = false };

            SetupMockItems(new[] { item });
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "item1", null, null, null, false))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((updated, null)));

            _vm.AutoAddToPantry = true;
            await _vm.LoadAsync("list1");

            await _vm.ToggleItemAsync("item1");

            _mockPantryService.Verify(x => x.AddItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ToggleItemAsync_AutoAddedItemId_DoesNotDuplicateOnRecheck()
        {
            var item = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = false };
            var checkedItem = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = true };
            var uncheckedItem = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = false };
            var pantryItem = new PantryItem { id = "p1", foodProductId = "fp1" };

            SetupMockItems(new[] { item });
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "item1", null, null, null, true))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((checkedItem, null)));
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "item1", null, null, null, false))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((uncheckedItem, null)));
            _mockPantryService
                .Setup(x => x.AddItemAsync("fp1", null, 1f, "pcs", null, null, null))
                .Returns(Task.FromResult<(PantryItem, ApiErrorResponse)>((pantryItem, null)));

            _vm.AutoAddToPantry = true;
            await _vm.LoadAsync("list1");

            await _vm.ToggleItemAsync("item1"); // check -> add to pantry
            await _vm.ToggleItemAsync("item1"); // uncheck -> no pantry removal
            await _vm.ToggleItemAsync("item1"); // recheck -> skip (idempotency)

            _mockPantryService.Verify(x => x.AddItemAsync("fp1", null, 1f, "pcs", null, null, null), Times.Once);
        }

        [Test]
        public async Task ToggleItemAsync_PantryAddFails_SetsErrorDetail()
        {
            var item = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = false };
            var updated = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = true };

            SetupMockItems(new[] { item });
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "item1", null, null, null, true))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((updated, null)));
            _mockPantryService
                .Setup(x => x.AddItemAsync("fp1", null, 1f, "pcs", null, null, null))
                .Returns(Task.FromResult<(PantryItem, ApiErrorResponse)>((null, new ApiErrorResponse { statusCode = 500, message = "Pantry full" })));

            _vm.AutoAddToPantry = true;
            await _vm.LoadAsync("list1");

            await _vm.ToggleItemAsync("item1");

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual(500, _vm.ErrorDetail.statusCode);
        }

        [Test]
        public async Task ResetListAsync_UnchecksAllCheckedItems()
        {
            var items = new[]
            {
                new ShoppingListItem { id = "c1", foodProductId = "fp1", @checked = true },
                new ShoppingListItem { id = "c2", foodProductId = "fp2", @checked = true },
                new ShoppingListItem { id = "u1", foodProductId = "fp3", @checked = false },
            };

            SetupMockItems(items);
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "c1", null, null, null, false))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((new ShoppingListItem { id = "c1", @checked = false }, null)));
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "c2", null, null, null, false))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((new ShoppingListItem { id = "c2", @checked = false }, null)));

            await _vm.LoadAsync("list1");
            await _vm.ResetListAsync();

            Assert.IsNull(_vm.ErrorDetail);
            _mockShoppingListService.Verify(x => x.UpdateItemAsync("list1", "c1", null, null, null, false), Times.Once);
            _mockShoppingListService.Verify(x => x.UpdateItemAsync("list1", "c2", null, null, null, false), Times.Once);
            foreach (var v in _vm.Items)
            {
                Assert.IsFalse(v.Item.@checked);
            }
        }

        [Test]
        public async Task ResetListAsync_WhenNoCheckedItems_DoesNothing()
        {
            var items = new[]
            {
                new ShoppingListItem { id = "u1", foodProductId = "fp1", @checked = false },
            };

            SetupMockItems(items);

            await _vm.LoadAsync("list1");
            await _vm.ResetListAsync();

            _mockShoppingListService.Verify(x => x.UpdateItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()), Times.Never);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task ResetListAsync_WithApiError_SetsErrorDetail()
        {
            var items = new[]
            {
                new ShoppingListItem { id = "c1", foodProductId = "fp1", @checked = true },
            };

            SetupMockItems(items);
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "c1", null, null, null, false))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((null, new ApiErrorResponse { statusCode = 500, message = "Reset failed" })));
            _mockShoppingListService
                .Setup(x => x.GetItemsAsync("list1"))
                .Returns(Task.FromResult<(ShoppingListItem[] Result, ApiErrorResponse Error)>((items, null)));

            await _vm.LoadAsync("list1");
            await _vm.ResetListAsync();

            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task ResetListAsync_ClearsAutoAddedTracker()
        {
            var item = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = false };
            var checkedItem = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = true };
            var uncheckedItem = new ShoppingListItem { id = "item1", foodProductId = "fp1", quantity = 1, unit = "pcs", @checked = false };
            var pantryItem = new PantryItem { id = "p1", foodProductId = "fp1" };

            SetupMockItems(new[] { item });
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "item1", null, null, null, true))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((checkedItem, null)));
            _mockShoppingListService
                .Setup(x => x.UpdateItemAsync("list1", "item1", null, null, null, false))
                .Returns(Task.FromResult<(ShoppingListItem, ApiErrorResponse)>((uncheckedItem, null)));
            _mockPantryService
                .Setup(x => x.AddItemAsync("fp1", null, 1f, "pcs", null, null, null))
                .Returns(Task.FromResult<(PantryItem, ApiErrorResponse)>((pantryItem, null)));

            _vm.AutoAddToPantry = true;
            await _vm.LoadAsync("list1");

            // Check -> add to pantry
            await _vm.ToggleItemAsync("item1");
            _mockPantryService.Verify(x => x.AddItemAsync("fp1", null, 1f, "pcs", null, null, null), Times.Once);

            // Reset -> clears tracker
            await _vm.ResetListAsync();

            // Re-check -> should add to pantry AGAIN (tracker was cleared)
            await _vm.ToggleItemAsync("item1");
            _mockPantryService.Verify(x => x.AddItemAsync("fp1", null, 1f, "pcs", null, null, null), Times.Exactly(2));
        }

        [Test]
        public async Task SyncAutoAddToPantryAsync_PersistsViaUpdateProfile()
        {
            _mockAuthService
                .Setup(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .Returns(Task.FromResult<(bool success, ApiErrorResponse error)>((true, null)));

            await _vm.SyncAutoAddToPantryAsync(true);

            _mockAuthService.Verify(x => x.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r =>
                r.preferences != null && r.preferences.autoAddToPantry == true)), Times.Once);
            Assert.IsNull(_vm.ErrorDetail);
        }
    }
}
