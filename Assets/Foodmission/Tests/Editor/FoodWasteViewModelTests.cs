using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodWasteViewModelTests
    {
        private Mock<IFoodWasteService> _mockFoodWasteService;
        private Mock<ILocalStorageService> _mockLocalStorage;
        private TestStoreService _storeService;
        private FoodWasteViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockFoodWasteService = new Mock<IFoodWasteService>();
            _mockLocalStorage = new Mock<ILocalStorageService>();
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState());

            _vm = new FoodWasteViewModel(_storeService, _mockFoodWasteService.Object, _mockLocalStorage.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        // ── Constructor ──────────────────────────────────────────────────

        [Test]
        public void Constructor_InitializesDefaults()
        {
            Assert.IsNotNull(_vm.Groups);
            Assert.AreEqual(0, _vm.Groups.Count);
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsFalse(_vm.HasMorePages);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.IsNull(_vm.ErrorDetail);
        }

        // ── LoadAsync ────────────────────────────────────────────────────

        [Test]
        public async Task LoadAsync_OnSuccess_PopulatesGroups()
        {
            var response = new PaginatedFoodWasteResponse
            {
                data = new[]
                {
                    new FoodWaste { id = "1", quantity = 1.5f, unit = "kg", wasteReason = "EXPIRED", wastedAt = "2026-05-15T10:00:00Z" },
                },
                total = 1,
                page = 1,
                limit = 20,
                totalPages = 1
            };

            _mockFoodWasteService
                .Setup(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((response, null)));

            await _vm.LoadAsync();

            Assert.AreEqual(1, _vm.Groups.Count);
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorDetail);
            Assert.IsFalse(_vm.HasMorePages);
            _mockLocalStorage.Verify(x => x.SetValue(It.IsAny<string>(), It.IsAny<PaginatedFoodWasteResponse>()), Times.Once);
        }

        [Test]
        public async Task LoadAsync_WhenHasMorePages_SetsHasMorePagesTrue()
        {
            var response = new PaginatedFoodWasteResponse
            {
                data = new[] { new FoodWaste { id = "1", quantity = 1f, unit = "kg", wasteReason = "EXPIRED", wastedAt = "2026-05-15T10:00:00Z" } },
                total = 21,
                page = 1,
                limit = 20,
                totalPages = 2
            };

            _mockFoodWasteService
                .Setup(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((response, null)));

            await _vm.LoadAsync();

            Assert.IsTrue(_vm.HasMorePages);
        }

        [Test]
        public async Task LoadAsync_OnApiError_SetsErrorDetail()
        {
            var error = new ApiErrorResponse { message = "Server error", statusCode = 500 };
            _mockFoodWasteService
                .Setup(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((null, error)));

            _mockLocalStorage
                .Setup(x => x.GetValue<PaginatedFoodWasteResponse>(It.IsAny<string>(), It.IsAny<PaginatedFoodWasteResponse>()))
                .Returns((PaginatedFoodWasteResponse)null);

            await _vm.LoadAsync();

            Assert.AreEqual(error, _vm.ErrorDetail);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task LoadAsync_OnApiError_LoadsFromCache()
        {
            var cachedData = new PaginatedFoodWasteResponse
            {
                data = new[]
                {
                    new FoodWaste { id = "cached-1", quantity = 1.0f, unit = "kg", wasteReason = "EXPIRED", wastedAt = "2026-05-15T10:00:00Z" }
                }
            };

            var error = new ApiErrorResponse { message = "Server error", statusCode = 500 };
            _mockFoodWasteService
                .Setup(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((null, error)));

            _mockLocalStorage
                .Setup(x => x.GetValue<PaginatedFoodWasteResponse>(It.IsAny<string>(), It.IsAny<PaginatedFoodWasteResponse>()))
                .Returns(cachedData);

            await _vm.LoadAsync();

            Assert.AreEqual(error, _vm.ErrorDetail);
            Assert.AreEqual(1, _vm.Groups.Count);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public void LoadAsync_WhenServiceThrows_PropagatesException()
        {
            _mockFoodWasteService
                .Setup(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Network failure"));

            Assert.ThrowsAsync<Exception>(async () => await _vm.LoadAsync());
        }

        [Test]
        public async Task LoadAsync_OnPageOne_ReplacesExistingData()
        {
            var page1 = new PaginatedFoodWasteResponse
            {
                data = new[] { new FoodWaste { id = "1", quantity = 1f, unit = "kg", wasteReason = "EXPIRED", wastedAt = "2026-05-15T10:00:00Z" } },
                total = 2,
                page = 1,
                limit = 20,
                totalPages = 2
            };

            _mockFoodWasteService
                .SetupSequence(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((page1, null)))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((page1, null)));

            await _vm.LoadAsync(1);
            Assert.AreEqual(1, _vm.Groups.Count);

            await _vm.LoadAsync(1);
            Assert.AreEqual(1, _vm.Groups.Count, "Page 1 should replace, not append");
        }

        [Test]
        public async Task LoadAsync_WithFilter_SendsFilterToService()
        {
            _vm.FilterWasteReason = "EXPIRED";

            var response = new PaginatedFoodWasteResponse
            {
                data = Array.Empty<FoodWaste>(),
                total = 0,
                page = 1,
                limit = 20,
                totalPages = 1
            };

            _mockFoodWasteService
                .Setup(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((response, null)));

            await _vm.LoadAsync();

            _mockFoodWasteService.Verify(
                x => x.GetListAsync(1, 20, "EXPIRED", null, null, null), Times.Once);
        }

        // ── LoadNextPageAsync ────────────────────────────────────────────

        [Test]
        public async Task LoadNextPageAsync_WhenHasMorePages_AppendsData()
        {
            var page1 = new PaginatedFoodWasteResponse
            {
                data = new[] { new FoodWaste { id = "1", quantity = 1f, unit = "kg", wasteReason = "EXPIRED", wastedAt = "2026-05-15T10:00:00Z" } },
                total = 2,
                page = 1,
                limit = 1,
                totalPages = 2
            };

            var page2 = new PaginatedFoodWasteResponse
            {
                data = new[] { new FoodWaste { id = "2", quantity = 2f, unit = "kg", wasteReason = "SPOILED", wastedAt = "2026-05-20T10:00:00Z" } },
                total = 2,
                page = 2,
                limit = 1,
                totalPages = 2
            };

            _mockFoodWasteService
                .SetupSequence(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((page1, null)))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((page2, null)));

            await _vm.LoadAsync(1);
            await _vm.LoadNextPageAsync();

            Assert.AreEqual(2, _vm.Groups[0].Items.Count);
            Assert.IsFalse(_vm.HasMorePages);
        }

        [Test]
        public async Task LoadNextPageAsync_WhenNoMorePages_DoesNotLoad()
        {
            var response = new PaginatedFoodWasteResponse
            {
                data = new[] { new FoodWaste { id = "1", quantity = 1f, unit = "kg", wasteReason = "EXPIRED", wastedAt = "2026-05-15T10:00:00Z" } },
                total = 1,
                page = 1,
                limit = 20,
                totalPages = 1
            };

            _mockFoodWasteService
                .Setup(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((response, null)));

            await _vm.LoadAsync(1);
            Assert.IsFalse(_vm.HasMorePages);

            await _vm.LoadNextPageAsync();

            _mockFoodWasteService.Verify(
                x => x.GetListAsync(2, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Test]
        public async Task LoadNextPageAsync_WhenLoading_DoesNotLoad()
        {
            var response = new PaginatedFoodWasteResponse
            {
                data = new[] { new FoodWaste { id = "1", quantity = 1f, unit = "kg", wasteReason = "EXPIRED", wastedAt = "2026-05-15T10:00:00Z" } },
                total = 21,
                page = 1,
                limit = 20,
                totalPages = 2
            };

            _mockFoodWasteService
                .Setup(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((response, null)));

            _vm.IsLoading = true;
            await _vm.LoadNextPageAsync();

            _mockFoodWasteService.Verify(
                x => x.GetListAsync(2, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        // ── DeleteWasteAsync ─────────────────────────────────────────────

        [Test]
        public async Task DeleteWasteAsync_OnSuccess_RemovesFromList()
        {
            var response = new PaginatedFoodWasteResponse
            {
                data = new[]
                {
                    new FoodWaste { id = "1", quantity = 1f, unit = "kg", wasteReason = "EXPIRED", wastedAt = "2026-05-15T10:00:00Z" },
                    new FoodWaste { id = "2", quantity = 2f, unit = "kg", wasteReason = "SPOILED", wastedAt = "2026-05-20T10:00:00Z" }
                },
                total = 2,
                page = 1,
                limit = 20,
                totalPages = 1
            };

            _mockFoodWasteService
                .Setup(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((response, null)));

            _mockFoodWasteService
                .Setup(x => x.DeleteAsync("1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));

            await _vm.LoadAsync();
            Assert.AreEqual(2, _vm.Groups[0].Items.Count);

            await _vm.DeleteWasteAsync("1");

            Assert.AreEqual(1, _vm.Groups[0].Items.Count);
            Assert.AreEqual("2", _vm.Groups[0].Items[0].id);
            Assert.IsNull(_vm.ErrorDetail);
            _mockLocalStorage.Verify(x => x.SetValue(It.IsAny<string>(), It.IsAny<PaginatedFoodWasteResponse>()), Times.Exactly(2));
        }

        [Test]
        public async Task DeleteWasteAsync_OnApiError_SetsErrorDetail()
        {
            var response = new PaginatedFoodWasteResponse
            {
                data = new[] { new FoodWaste { id = "1", quantity = 1f, unit = "kg", wasteReason = "EXPIRED", wastedAt = "2026-05-15T10:00:00Z" } },
                total = 1,
                page = 1,
                limit = 20,
                totalPages = 1
            };

            _mockFoodWasteService
                .Setup(x => x.GetListAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((response, null)));

            var error = new ApiErrorResponse { message = "Delete failed", statusCode = 500 };
            _mockFoodWasteService
                .Setup(x => x.DeleteAsync("1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((false, error)));

            await _vm.LoadAsync();
            Assert.AreEqual(1, _vm.Groups[0].Items.Count);

            await _vm.DeleteWasteAsync("1");

            Assert.AreEqual(error, _vm.ErrorDetail);
            Assert.AreEqual(1, _vm.Groups[0].Items.Count, "Item should not be removed on error");
        }

        [Test]
        public void DeleteWasteAsync_WhenServiceThrows_PropagatesException()
        {
            _mockFoodWasteService
                .Setup(x => x.DeleteAsync(It.IsAny<string>()))
                .Throws(new Exception("Delete failed"));

            Assert.ThrowsAsync<Exception>(async () => await _vm.DeleteWasteAsync("1"));
        }

        // ── LoadStatisticsAsync ──────────────────────────────────────────

        [Test]
        public async Task LoadStatisticsAsync_OnSuccess_ReturnsStatistics()
        {
            var stats = new FoodWasteStatistics
            {
                totalWaste = 3.5f,
                totalCost = 12.50f,
                totalCarbon = 5.2f,
                wasteByReason = new[]
                {
                    new WasteByReason { reason = "EXPIRED", count = 2 }
                }
            };

            _mockFoodWasteService
                .Setup(x => x.GetStatisticsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(FoodWasteStatistics Result, ApiErrorResponse Error)>((stats, null)));

            var result = await _vm.LoadStatisticsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(3.5f, result.totalWaste);
            Assert.AreEqual(12.50f, result.totalCost);
            Assert.AreEqual(1, result.wasteByReason.Length);
        }
    }
}
