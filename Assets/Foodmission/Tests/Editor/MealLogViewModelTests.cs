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
        private Mock<ILocalStorageService> _mockLocalStorage;
        private TestStoreService _storeService;
        private MealLogViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockMealLogService = new Mock<IMealLogService>();
            _mockLocalStorage = new Mock<ILocalStorageService>();
            _storeService = new TestStoreService();
            _vm = new MealLogViewModel(_storeService, _mockMealLogService.Object, _mockLocalStorage.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesDefaults()
        {
            Assert.IsNotNull(_vm.Groups);
            Assert.AreEqual(0, _vm.Groups.Count);
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsFalse(_vm.HasMorePages);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.AreEqual("", _vm.FilterDateFrom);
            Assert.AreEqual("", _vm.FilterDateTo);
            Assert.AreEqual("", _vm.FilterTypeOfMeal);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task LoadAsync_InitialLoad_ClearsGroupsAndLoadsPage1()
        {
            var response = new PaginatedMealLogResponse
            {
                data = new[]
                {
                    new MealLog { id = "1", typeOfMeal = "BREAKFAST", meal = new Meal { name = "Toast" } },
                    new MealLog { id = "2", typeOfMeal = "BREAKFAST", meal = new Meal { name = "Juice" } },
                    new MealLog { id = "3", typeOfMeal = "LUNCH", meal = new Meal { name = "Salad" } }
                },
                total = 3,
                page = 1,
                limit = 20,
                totalPages = 1
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 20, null, null, null))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((response, null)));

            await _vm.LoadAsync(1);

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorDetail);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.IsFalse(_vm.HasMorePages);

            Assert.AreEqual(2, _vm.Groups.Count);
            var breakfastGroup = _vm.Groups.First(g => g.TypeOfMeal == "BREAKFAST");
            Assert.AreEqual(2, breakfastGroup.Logs.Count);

            _mockLocalStorage.Verify(x => x.SetValue(It.IsAny<string>(), It.IsAny<PaginatedMealLogResponse>()), Times.Once);
        }

        [Test]
        public async Task LoadAsync_WithDataOnPage2_AppendsToExistingLogs()
        {
            var page1 = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST", meal = new Meal { name = "Toast" } } },
                total = 2, page = 1, limit = 1, totalPages = 2
            };
            var page2 = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "2", typeOfMeal = "LUNCH", meal = new Meal { name = "Salad" } } },
                total = 2, page = 2, limit = 1, totalPages = 2
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 20, null, null, null))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((page1, null)));
            _mockMealLogService
                .Setup(x => x.GetLogsAsync(2, 20, null, null, null))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((page2, null)));

            await _vm.LoadAsync(1);
            Assert.AreEqual(1, _vm.Groups.SelectMany(g => g.Logs).Count());

            await _vm.LoadAsync(2);
            Assert.AreEqual(2, _vm.Groups.SelectMany(g => g.Logs).Count());
        }

        [Test]
        public async Task LoadAsync_WhenLastPage_SetsHasMorePagesFalse()
        {
            var response = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST" } },
                total = 1, page = 1, limit = 20, totalPages = 1
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((response, null)));

            await _vm.LoadAsync(1);

            Assert.IsFalse(_vm.HasMorePages);
        }

        [Test]
        public async Task LoadAsync_WhenMultiplePages_SetsHasMorePagesTrue()
        {
            var response = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST" } },
                total = 25, page = 1, limit = 20, totalPages = 2
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((response, null)));

            await _vm.LoadAsync(1);

            Assert.IsTrue(_vm.HasMorePages);
        }

        [Test]
        public async Task LoadAsync_WithApiError_SetsErrorDetailAndLoadsFromCache()
        {
            var apiError = new ApiErrorResponse { statusCode = 500, message = "Server error" };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((null, apiError)));

            await _vm.LoadAsync(1);

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual(500, _vm.ErrorDetail.statusCode);
            Assert.IsFalse(_vm.IsLoading);
            _mockLocalStorage.Verify(x => x.GetValue<PaginatedMealLogResponse>(It.IsAny<string>(), It.IsAny<PaginatedMealLogResponse>()), Times.AtLeastOnce);
        }

        [Test]
        public void LoadAsync_WithException_PropagatesToCaller()
        {
            _mockMealLogService
                .Setup(x => x.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Network failure"));

            Assert.ThrowsAsync<Exception>(async () => await _vm.LoadAsync(1));
        }

        [Test]
        public async Task LoadNextPageAsync_WhenHasMorePages_LoadsNextPage()
        {
            var page1 = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST" } },
                total = 25, page = 1, limit = 20, totalPages = 2
            };
            var page2 = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "2", typeOfMeal = "LUNCH" } },
                total = 25, page = 2, limit = 20, totalPages = 2
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 20, null, null, null))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((page1, null)));
            _mockMealLogService
                .Setup(x => x.GetLogsAsync(2, 20, null, null, null))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((page2, null)));

            await _vm.LoadAsync(1);
            Assert.IsTrue(_vm.HasMorePages);

            await _vm.LoadNextPageAsync();

            _mockMealLogService.Verify(x => x.GetLogsAsync(2, 20, null, null, null), Times.Once);
            Assert.AreEqual(2, _vm.Groups.SelectMany(g => g.Logs).Count());
        }

        [Test]
        public async Task LoadNextPageAsync_WhenHasMorePagesFalse_DoesNothing()
        {
            var response = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST" } },
                total = 1, page = 1, limit = 20, totalPages = 1
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((response, null)));

            await _vm.LoadAsync(1);
            Assert.IsFalse(_vm.HasMorePages);

            await _vm.LoadNextPageAsync();

            _mockMealLogService.Verify(x => x.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task DeleteLogAsync_Success_RemovesFromList()
        {
            var mealLog = new MealLog { id = "1", typeOfMeal = "BREAKFAST", meal = new Meal { name = "Toast" } };
            var response = new PaginatedMealLogResponse
            {
                data = new[] { mealLog },
                total = 1, page = 1, limit = 20, totalPages = 1
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((response, null)));

            _mockMealLogService
                .Setup(x => x.DeleteLogAsync("1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));

            await _vm.LoadAsync(1);
            Assert.AreEqual(1, _vm.Groups.SelectMany(g => g.Logs).Count());

            await _vm.DeleteLogAsync("1");

            Assert.IsNull(_vm.ErrorDetail);
            Assert.AreEqual(0, _vm.Groups.SelectMany(g => g.Logs).Count());
            _mockMealLogService.Verify(x => x.DeleteLogAsync("1"), Times.Once);
        }

        [Test]
        public async Task DeleteLogAsync_WithApiError_SetsErrorDetail()
        {
            var mealLog = new MealLog { id = "1", typeOfMeal = "BREAKFAST" };
            var response = new PaginatedMealLogResponse
            {
                data = new[] { mealLog },
                total = 1, page = 1, limit = 20, totalPages = 1
            };
            var apiError = new ApiErrorResponse { statusCode = 500, message = "Delete failed" };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((response, null)));

            _mockMealLogService
                .Setup(x => x.DeleteLogAsync("1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((false, apiError)));

            await _vm.LoadAsync(1);
            Assert.AreEqual(1, _vm.Groups.SelectMany(g => g.Logs).Count());

            await _vm.DeleteLogAsync("1");

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual("Delete failed", _vm.ErrorDetail.message);
            Assert.AreEqual(1, _vm.Groups.SelectMany(g => g.Logs).Count());
        }

        [Test]
        public async Task FilterTypeOfMeal_AppliedOnLoad()
        {
            var response = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "1", typeOfMeal = "LUNCH" } },
                total = 1, page = 1, limit = 20, totalPages = 1
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 20, "LUNCH", null, null))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((response, null)));

            _vm.FilterTypeOfMeal = "LUNCH";
            await _vm.LoadAsync(1);

            Assert.AreEqual(1, _vm.Groups.SelectMany(g => g.Logs).Count());
            Assert.AreEqual("LUNCH", _vm.Groups.First().TypeOfMeal);
        }

        [Test]
        public async Task FilterDateFrom_AppliedOnLoad()
        {
            var response = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST", timestamp = "2026-05-01" } },
                total = 1, page = 1, limit = 20, totalPages = 1
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 20, null, "2026-05-01", null))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((response, null)));

            _vm.FilterDateFrom = "2026-05-01";
            await _vm.LoadAsync(1);

            Assert.IsNull(_vm.ErrorDetail);
            _mockMealLogService.Verify(x => x.GetLogsAsync(1, 20, null, "2026-05-01", null), Times.Once);
        }

        [Test]
        public async Task LoadInitialThenReload_ClearsPreviousGroups()
        {
            var page1 = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "1", typeOfMeal = "BREAKFAST" } },
                total = 2, page = 1, limit = 1, totalPages = 2
            };
            var page2 = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "2", typeOfMeal = "LUNCH" } },
                total = 2, page = 2, limit = 1, totalPages = 2
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 20, null, null, null))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((page1, null)));
            _mockMealLogService
                .Setup(x => x.GetLogsAsync(2, 20, null, null, null))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((page2, null)));

            await _vm.LoadAsync(1);
            await _vm.LoadAsync(2);

            Assert.AreEqual(2, _vm.Groups.SelectMany(g => g.Logs).Count());

            var reloadPage = new PaginatedMealLogResponse
            {
                data = new[] { new MealLog { id = "3", typeOfMeal = "DINNER" } },
                total = 1, page = 1, limit = 20, totalPages = 1
            };

            _mockMealLogService
                .Setup(x => x.GetLogsAsync(1, 20, null, null, null))
                .Returns(Task.FromResult<(PaginatedMealLogResponse Result, ApiErrorResponse Error)>((reloadPage, null)));

            await _vm.LoadAsync(1);

            Assert.AreEqual(1, _vm.Groups.SelectMany(g => g.Logs).Count());
            Assert.AreEqual("DINNER", _vm.Groups.First().TypeOfMeal);
        }
    }
}
