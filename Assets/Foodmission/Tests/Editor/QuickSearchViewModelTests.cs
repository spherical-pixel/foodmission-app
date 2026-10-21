using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using UnityEngine;
using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuickSearchViewModelTests
    {
        private Mock<IFoodProductService> _foodProductServiceMock;
        private Mock<IGenericFoodService> _genericFoodServiceMock;
        private Mock<IPantryService> _pantryServiceMock;
        private Mock<IShoppingListService> _shoppingListServiceMock;
        private Mock<ILocalStorageService> _localStorageMock;
        private Mock<IOpenFoodFactsClientService> _offClientMock;
        private Mock<ICatalogService> _catalogServiceMock;
        private TestStoreService _storeService;

        [SetUp]
        public void SetUp()
        {
            _foodProductServiceMock = new Mock<IFoodProductService>();
            _genericFoodServiceMock = new Mock<IGenericFoodService>();
            _pantryServiceMock = new Mock<IPantryService>();
            _shoppingListServiceMock = new Mock<IShoppingListService>();
            _localStorageMock = new Mock<ILocalStorageService>();
            _offClientMock = new Mock<IOpenFoodFactsClientService>();
            _catalogServiceMock = new Mock<ICatalogService>();
            _storeService = new TestStoreService();

            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        private QuickSearchViewModel CreateViewModel()
        {
            return new QuickSearchViewModel(
                _storeService,
                _foodProductServiceMock.Object,
                _genericFoodServiceMock.Object,
                _pantryServiceMock.Object,
                _shoppingListServiceMock.Object,
                _localStorageMock.Object,
                _offClientMock.Object,
                _catalogServiceMock.Object);
        }

        [Test]
        public async Task SearchFoodsPageAsync_EmptyQuery_ReturnsEmptyPage()
        {
            var vm = CreateViewModel();
            var results = await vm.SearchFoodsPageAsync("", 1, true, true);

            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Items.Count);
        }

        [Test]
        public async Task AddToPantryDirectAsync_GenericFood_CallsPantryService()
        {
            var genericId = Guid.NewGuid().ToString();
            var addedItem = new PantryItem { id = Guid.NewGuid().ToString(), genericFoodId = genericId };

            _pantryServiceMock
                .Setup(x => x.AddItemAsync(null, genericId, 1f, "PIECES", null, null, null))
                .ReturnsAsync((addedItem, null));

            var vm = CreateViewModel();
            var request = new AddToContextRequestedAction
            {
                FoodType = FoodInfoType.Generic,
                FoodId = genericId,
                EntryContext = "pantry"
            };

            await vm.AddToPantryDirectAsync(request);

            _pantryServiceMock.Verify(x => x.AddItemAsync(null, genericId, 1f, "PIECES", null, null, null), Times.Once);
            Assert.IsFalse(string.IsNullOrEmpty(vm.StatusMessage));
        }

        [Test]
        public async Task AddToShoppingListDirectAsync_NoExistingLists_CreatesDefaultListAndAddsItem()
        {
            var genericId = Guid.NewGuid().ToString();
            var newList = new ShoppingList { id = "list-1", title = "Mi Lista" };
            var newItem = new ShoppingListItem { id = "item-1", genericFoodId = genericId };

            _shoppingListServiceMock
                .Setup(x => x.GetListsAsync())
                .ReturnsAsync((new ShoppingList[0], null));

            _shoppingListServiceMock
                .Setup(x => x.CreateListAsync(It.IsAny<string>()))
                .ReturnsAsync((newList, null));

            _shoppingListServiceMock
                .Setup(x => x.AddItemAsync("list-1", null, 1f, "PIECES", null, false, genericId))
                .ReturnsAsync((newItem, null));

            var vm = CreateViewModel();
            var request = new AddToContextRequestedAction
            {
                FoodType = FoodInfoType.Generic,
                FoodId = genericId,
                EntryContext = "shoppingList"
            };

            await vm.AddToShoppingListDirectAsync(request);

            _shoppingListServiceMock.Verify(x => x.CreateListAsync(It.IsAny<string>()), Times.Once);
            _shoppingListServiceMock.Verify(x => x.AddItemAsync("list-1", null, 1f, "PIECES", null, false, genericId), Times.Once);
            Assert.IsFalse(string.IsNullOrEmpty(vm.StatusMessage));
        }

        [Test]
        public void CheckPendingFoodInfoAddRequest_MealLogContext_SetsPendingMealLogAdd()
        {
            var state = _storeService.GetAppState();
            state.foodInfoAddRequest = new AddToContextRequestedAction
            {
                FoodType = FoodInfoType.Generic,
                FoodId = "g1",
                EntryContext = "mealLog"
            };

            var vm = CreateViewModel();
            vm.CheckPendingFoodInfoAddRequest();

            Assert.IsNotNull(vm.PendingMealLogAdd);
            Assert.AreEqual("g1", vm.PendingMealLogAdd.FoodId);
            Assert.AreEqual(FoodInfoType.Generic, vm.PendingMealLogAdd.FoodType);
        }

        [Test]
        public void CheckPendingFoodInfoAddRequest_MealLogOpenFoodFactsProduct_ImportsAndPassesBackendId()
        {
            var productId = Guid.NewGuid().ToString();
            _foodProductServiceMock
                .Setup(x => x.SearchFoodsByBarcodeAsync("8410000000001"))
                .ReturnsAsync((new PaginatedFoodProductResponse { data = new FoodProduct[0] }, null));
            _foodProductServiceMock
                .Setup(x => x.ImportFromBarcodeAsync("8410000000001"))
                .ReturnsAsync((new FoodProduct { id = productId, barcode = "8410000000001" }, null));

            var state = _storeService.GetAppState();
            state.foodInfoAddRequest = new AddToContextRequestedAction
            {
                FoodType = FoodInfoType.Product,
                FoodId = "8410000000001",
                EntryContext = "mealLog",
                FoodData = "{\"barcode\":\"8410000000001\",\"name\":\"Galletas\"}"
            };

            var vm = CreateViewModel();
            vm.CheckPendingFoodInfoAddRequest();

            Assert.IsNotNull(vm.PendingMealLogAdd);
            Assert.AreEqual(productId, vm.PendingMealLogAdd.FoodId);
            Assert.AreEqual(FoodInfoType.Product, vm.PendingMealLogAdd.FoodType);
        }

        [Test]
        public void CheckPendingFoodInfoAddRequest_MealLogProductWithBackendId_DoesNotImport()
        {
            var productId = Guid.NewGuid().ToString();
            var state = _storeService.GetAppState();
            state.foodInfoAddRequest = new AddToContextRequestedAction
            {
                FoodType = FoodInfoType.Product,
                FoodId = productId,
                EntryContext = "mealLog"
            };

            var vm = CreateViewModel();
            vm.CheckPendingFoodInfoAddRequest();

            Assert.IsNotNull(vm.PendingMealLogAdd);
            Assert.AreEqual(productId, vm.PendingMealLogAdd.FoodId);
            _foodProductServiceMock.Verify(x => x.ImportFromBarcodeAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void CheckPendingFoodInfoAddRequest_MealLogImportFails_ShowsErrorAndDoesNotOpenMealLog()
        {
            var importError = new ApiErrorResponse { statusCode = 404, message = "Product not found" };
            _foodProductServiceMock
                .Setup(x => x.SearchFoodsByBarcodeAsync("8410000000001"))
                .ReturnsAsync((new PaginatedFoodProductResponse { data = new FoodProduct[0] }, null));
            _foodProductServiceMock
                .Setup(x => x.ImportFromBarcodeAsync("8410000000001"))
                .ReturnsAsync((null, importError));

            var state = _storeService.GetAppState();
            state.foodInfoAddRequest = new AddToContextRequestedAction
            {
                FoodType = FoodInfoType.Product,
                FoodId = "8410000000001",
                EntryContext = "mealLog"
            };

            var vm = CreateViewModel();
            vm.CheckPendingFoodInfoAddRequest();

            Assert.IsNull(vm.PendingMealLogAdd);
            Assert.IsNotNull(vm.ErrorDetail);
        }
    }
}
