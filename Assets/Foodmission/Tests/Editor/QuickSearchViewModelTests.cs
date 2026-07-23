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
        public async Task SearchFoodsAsync_EmptyQuery_ReturnsEmptyList()
        {
            var vm = CreateViewModel();
            var results = await vm.SearchFoodsAsync("");

            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
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
                .Setup(x => x.CreateListAsync("Mi Lista"))
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

            _shoppingListServiceMock.Verify(x => x.CreateListAsync("Mi Lista"), Times.Once);
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
    }
}
