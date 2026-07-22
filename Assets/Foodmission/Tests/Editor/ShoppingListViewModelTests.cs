using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ShoppingListViewModelTests
    {
        private Mock<IShoppingListService> _mockShoppingListService;
        private Mock<ILocalStorageService> _mockLocalStorage;
        private Mock<IAuthService> _mockAuthService;
        private TestStoreService _storeService;
        private ShoppingListViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockShoppingListService = new Mock<IShoppingListService>();
            _mockLocalStorage = new Mock<ILocalStorageService>();
            _mockAuthService = new Mock<IAuthService>();
            _storeService = new TestStoreService();

            _vm = new ShoppingListViewModel(
                _storeService,
                _mockShoppingListService.Object,
                _mockLocalStorage.Object,
                _mockAuthService.Object
            );
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public async Task ResolveLastOpenedListAsync_WhenLastOpenedListExists_ReturnsTargetList()
        {
            // Arrange
            string lastOpenedId = "list-123";
            _storeService.SetAppState(new AppState
            {
                userLastShoppingListId = lastOpenedId,
                userShoppingResponsibility = "resp",
                userDietaryPreference = new[] { "pref1" }
            });

            var list1 = new ShoppingList { id = "list-123", title = "My List" };
            var list2 = new ShoppingList { id = "list-456", title = "Another List" };
            _mockShoppingListService
                .Setup(s => s.GetListsAsync())
                .ReturnsAsync((new[] { list1, list2 }, (ApiErrorResponse)null));

            // Act
            var (result, error) = await _vm.ResolveLastOpenedListAsync();

            // Assert
            Assert.IsNull(error);
            Assert.IsNotNull(result);
            Assert.AreEqual("list-123", result.id);
            _mockAuthService.Verify(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Never);
        }

        [Test]
        public async Task ResolveLastOpenedListAsync_WhenLastOpenedListDoesNotExistButOtherListsExist_ReturnsFirstAndUpdatesPreference()
        {
            // Arrange
            _storeService.SetAppState(new AppState
            {
                userLastShoppingListId = "non-existent-id",
                userShoppingResponsibility = "resp",
                userDietaryPreference = new[] { "pref1" }
            });

            var list1 = new ShoppingList { id = "list-456", title = "Another List" };
            _mockShoppingListService
                .Setup(s => s.GetListsAsync())
                .ReturnsAsync((new[] { list1 }, (ApiErrorResponse)null));

            _mockAuthService
                .Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .ReturnsAsync((true, (ApiErrorResponse)null));

            // Act
            var (result, error) = await _vm.ResolveLastOpenedListAsync();

            // Assert
            Assert.IsNull(error);
            Assert.IsNotNull(result);
            Assert.AreEqual("list-456", result.id);

            _mockAuthService.Verify(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r =>
                r.preferences != null &&
                r.preferences.lastShoppingListId == "list-456" &&
                r.preferences.shoppingResponsibility == "resp" &&
                r.preferences.dietaryPreference[0] == "pref1"
            )), Times.Once);
        }

        [Test]
        public async Task ResolveLastOpenedListAsync_WhenNoListsExist_CreatesNewListAndUpdatesPreference()
        {
            // Arrange
            _storeService.SetAppState(new AppState
            {
                userLastShoppingListId = "",
                userShoppingResponsibility = "resp",
                userDietaryPreference = new[] { "pref1" }
            });

            _mockShoppingListService
                .Setup(s => s.GetListsAsync())
                .ReturnsAsync((new ShoppingList[0], (ApiErrorResponse)null));

            var createdList = new ShoppingList { id = "new-list-789", title = "Shopping list" };
            _mockShoppingListService
                .Setup(s => s.CreateListAsync(It.IsAny<string>()))
                .ReturnsAsync((createdList, (ApiErrorResponse)null));

            _mockAuthService
                .Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .ReturnsAsync((true, (ApiErrorResponse)null));

            // Act
            var (result, error) = await _vm.ResolveLastOpenedListAsync();

            // Assert
            Assert.IsNull(error);
            Assert.IsNotNull(result);
            Assert.AreEqual("new-list-789", result.id);

            _mockShoppingListService.Verify(s => s.CreateListAsync(It.IsAny<string>()), Times.Once);
            _mockAuthService.Verify(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r =>
                r.preferences != null &&
                r.preferences.lastShoppingListId == "new-list-789"
            )), Times.Once);
        }

        [Test]
        public async Task ResolveLastOpenedListAsync_WhenGetListsFails_ReturnsNullAndError()
        {
            // Arrange
            var apiError = new ApiErrorResponse { statusCode = 500, error = "SERVER_ERROR", message = "Failure" };
            _mockShoppingListService
                .Setup(s => s.GetListsAsync())
                .ReturnsAsync(((ShoppingList[])null, apiError));

            // Act
            var (result, error) = await _vm.ResolveLastOpenedListAsync();

            // Assert
            Assert.IsNotNull(error);
            Assert.IsNull(result);
            Assert.AreEqual("SERVER_ERROR", error.error);
        }
    }
}
