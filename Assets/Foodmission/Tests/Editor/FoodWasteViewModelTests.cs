using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodWasteViewModelTests
    {
        private Mock<IFoodWasteService> _mockFoodWasteService;
        private Mock<IPantryService> _mockPantryService;
        private Mock<IPantryItemEnricher> _mockEnricher;
        private Mock<ILocalStorageService> _mockLocalStorage;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<IChallengeSessionService> _mockChallengeSession;
        private TestStoreService _storeService;
        private FoodWasteViewModel _vm;
        private Dictionary<string, string> _names;

        private static string Yesterday => DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        private static string Tomorrow => DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
        private static DateTime CurrentMonth => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        [SetUp]
        public void SetUp()
        {
            _mockFoodWasteService = new Mock<IFoodWasteService>();
            _mockPantryService = new Mock<IPantryService>();
            _mockEnricher = new Mock<IPantryItemEnricher>();
            _mockLocalStorage = new Mock<ILocalStorageService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockChallengeSession = new Mock<IChallengeSessionService>();
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState());
            _names = new Dictionary<string, string>();

            _mockEnricher
                .Setup(x => x.EnrichAsync(It.IsAny<PantryItem[]>()))
                .Returns<PantryItem[]>(items => Task.FromResult(items
                    .Select(i => new PantryItemView { Item = i, DisplayName = _names.TryGetValue(i.id, out string n) ? n : i.id })
                    .ToArray()));

            _mockChallengeSession
                .Setup(x => x.ReportAsync(It.IsAny<ChallengeCompletionTrigger>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            SetupHistory(1, Page(1, 1));
            SetupPantry();
            SetupExpired();

            _vm = new FoodWasteViewModel(
                _storeService,
                _mockFoodWasteService.Object,
                _mockPantryService.Object,
                _mockEnricher.Object,
                _mockLocalStorage.Object,
                _mockNotificationService.Object,
                _mockChallengeSession.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static PaginatedFoodWasteResponse Page(int page, int totalPages, params FoodWaste[] data)
        {
            return new PaginatedFoodWasteResponse { data = data, page = page, totalPages = totalPages, limit = 50, total = data.Length };
        }

        private static FoodWaste Waste(string id, string wastedAt, string pantryItemId = null)
        {
            return new FoodWaste { id = id, wastedAt = wastedAt, quantity = 1f, unit = "KG", wasteReason = WasteReason.Expired, pantryItemId = pantryItemId };
        }

        private static (string From, string To) MonthRange(DateTime monthStart)
        {
            return (monthStart.ToUniversalTime().ToString("o"), monthStart.AddMonths(1).AddSeconds(-1).ToUniversalTime().ToString("o"));
        }

        private static string CacheKey(DateTime monthStart)
        {
            return "foodwaste_cache_" + monthStart.ToString("yyyy-MM");
        }

        private PantryItem Item(string id, string name)
        {
            _names[id] = name;
            return new PantryItem { id = id, quantity = 1f, unit = "PIECES" };
        }

        private void SetupHistory(int page, PaginatedFoodWasteResponse response, ApiErrorResponse error = null)
        {
            _mockFoodWasteService
                .Setup(x => x.GetListAsync(page, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)>((error == null ? response : null, error)));
        }

        private void SetupPantry(params PantryItem[] items)
        {
            _mockPantryService
                .Setup(x => x.GetPantryAsync())
                .Returns(Task.FromResult<(Pantry Result, ApiErrorResponse Error)>((new Pantry { id = "pantry", items = items }, null)));
        }

        private void SetupExpired(params ExpiredPantryItem[] items)
        {
            _mockPantryService
                .Setup(x => x.GetExpiredItemsAsync())
                .Returns(Task.FromResult<(ExpiredPantryItem[] Result, ApiErrorResponse Error)>((items, null)));
        }

        // ── Month selection ─────────────────────────────────────────────

        [Test]
        public void Constructor_SelectsCurrentMonth()
        {
            Assert.AreEqual(CurrentMonth, _vm.SelectedMonth);
        }

        [Test]
        public async Task LoadAsync_RequestsSelectedMonthRange()
        {
            await _vm.LoadAsync();

            var (from, to) = MonthRange(CurrentMonth);
            _mockFoodWasteService.Verify(x => x.GetListAsync(1, It.IsAny<int>(), null, null, from, to), Times.Once);
        }

        [Test]
        public async Task LoadAsync_ListsRecordsNewestFirst_UnparseableDatesLast()
        {
            SetupHistory(1, Page(1, 1,
                Waste("bad", "garbage"),
                Waste("early", "2026-09-05T10:00:00Z"),
                Waste("late", "2026-09-20T10:00:00Z")));

            await _vm.LoadAsync();

            CollectionAssert.AreEqual(new[] { "late", "early", "bad" }, _vm.History.Select(w => w.id).ToArray());
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task LoadAsync_FetchesAllPagesOfTheMonth()
        {
            SetupHistory(1, Page(1, 2, Waste("p1", "2026-09-20T10:00:00Z")));
            SetupHistory(2, Page(2, 2, Waste("p2", "2026-09-10T10:00:00Z")));

            await _vm.LoadAsync();

            CollectionAssert.AreEqual(new[] { "p1", "p2" }, _vm.History.Select(w => w.id).ToArray());
            _mockFoodWasteService.Verify(x => x.GetListAsync(3, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task SetSelectedMonthAsync_ReloadsOnlyHistoryForThatMonth()
        {
            await _vm.LoadAsync();
            DateTime previous = CurrentMonth.AddMonths(-1);

            await _vm.SetSelectedMonthAsync(previous.AddDays(10));

            Assert.AreEqual(previous, _vm.SelectedMonth);
            var (from, to) = MonthRange(previous);
            _mockFoodWasteService.Verify(x => x.GetListAsync(1, It.IsAny<int>(), null, null, from, to), Times.Once);
            _mockPantryService.Verify(x => x.GetPantryAsync(), Times.Once);
        }

        [Test]
        public async Task SetSelectedMonthAsync_EmptyMonth_ClearsHistory()
        {
            SetupHistory(1, Page(1, 1, Waste("w1", "2026-09-05T10:00:00Z")));
            await _vm.LoadAsync();
            SetupHistory(1, Page(1, 1));

            await _vm.SetSelectedMonthAsync(CurrentMonth.AddMonths(-1));

            Assert.AreEqual(0, _vm.History.Count);
        }

        [Test]
        public async Task LoadAsync_SavesCachePerMonth()
        {
            SetupHistory(1, Page(1, 1, Waste("w1", "2026-09-05T10:00:00Z")));

            await _vm.LoadAsync();

            _mockLocalStorage.Verify(x => x.SetValue(CacheKey(CurrentMonth), It.IsAny<PaginatedFoodWasteResponse>()), Times.Once);
        }

        [Test]
        public async Task LoadAsync_HistoryError_FallsBackToMonthCache()
        {
            var error = new ApiErrorResponse { message = "offline" };
            SetupHistory(1, null, error);
            _mockLocalStorage
                .Setup(x => x.GetValue<PaginatedFoodWasteResponse>(CacheKey(CurrentMonth), It.IsAny<PaginatedFoodWasteResponse>()))
                .Returns(Page(1, 1, Waste("cached", "2026-09-05T10:00:00Z")));

            await _vm.LoadAsync();

            Assert.AreEqual(1, _vm.History.Count);
            Assert.AreEqual("cached", _vm.History[0].id);
            Assert.AreSame(error, _vm.ErrorDetail);
        }

        [Test]
        public async Task LoadAsync_HistoryErrorWithEmptyCache_EmptyHistoryAndErrorDetail()
        {
            var error = new ApiErrorResponse { message = "offline" };
            SetupHistory(1, null, error);

            await _vm.LoadAsync();

            Assert.AreEqual(0, _vm.History.Count);
            Assert.AreSame(error, _vm.ErrorDetail);
            Assert.IsFalse(_vm.IsLoading);
        }

        // ── Pantry / expired ────────────────────────────────────────────

        [Test]
        public async Task LoadAsync_MapsOnlyPastExpiredItemsPresentInPantry()
        {
            SetupPantry(Item("i1", "Milk"), Item("i2", "Rice"));
            SetupExpired(
                new ExpiredPantryItem { pantryItemId = "i1", expiryDate = Yesterday },
                new ExpiredPantryItem { pantryItemId = "i2", expiryDate = Tomorrow },
                new ExpiredPantryItem { pantryItemId = "ghost", expiryDate = Yesterday },
                new ExpiredPantryItem { pantryItemId = "i1", expiryDate = null });

            await _vm.LoadAsync();

            Assert.AreEqual(1, _vm.ExpiredItems.Count);
            Assert.AreEqual("i1", _vm.ExpiredItems[0].Item.id);
            Assert.AreEqual("Milk", _vm.ExpiredItems[0].DisplayName);
        }

        [Test]
        public async Task LoadAsync_PantryError_NoExpiredNoCandidatesAndErrorDetail()
        {
            var error = new ApiErrorResponse { message = "pantry down" };
            _mockPantryService
                .Setup(x => x.GetPantryAsync())
                .Returns(Task.FromResult<(Pantry Result, ApiErrorResponse Error)>((null, error)));
            SetupExpired(new ExpiredPantryItem { pantryItemId = "i1", expiryDate = Yesterday });

            await _vm.LoadAsync();

            Assert.AreEqual(0, _vm.ExpiredItems.Count);
            Assert.AreEqual(0, (await _vm.SearchCandidatesAsync("a")).Count);
            Assert.AreSame(error, _vm.ErrorDetail);
        }

        [Test]
        public async Task LoadAsync_ExpiredEndpointError_SetsErrorDetail()
        {
            var error = new ApiErrorResponse { message = "expired down" };
            _mockPantryService
                .Setup(x => x.GetExpiredItemsAsync())
                .Returns(Task.FromResult<(ExpiredPantryItem[] Result, ApiErrorResponse Error)>((null, error)));

            await _vm.LoadAsync();

            Assert.AreSame(error, _vm.ErrorDetail);
            Assert.AreEqual(0, _vm.ExpiredItems.Count);
        }

        [Test]
        public async Task LoadAsync_UnexpectedException_ShowsMonthCacheAndError()
        {
            _mockPantryService
                .Setup(x => x.GetPantryAsync())
                .Returns(Task.FromException<(Pantry Result, ApiErrorResponse Error)>(new Exception("boom")));
            _mockLocalStorage
                .Setup(x => x.GetValue<PaginatedFoodWasteResponse>(CacheKey(CurrentMonth), It.IsAny<PaginatedFoodWasteResponse>()))
                .Returns(Page(1, 1, Waste("cached", "2026-09-05T10:00:00Z")));
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex(@"\[FoodWasteViewModel\]"));

            await _vm.LoadAsync();

            Assert.AreEqual(1, _vm.History.Count);
            Assert.AreEqual("cached", _vm.History[0].id);
            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.IsFalse(_vm.IsLoading);
        }

        // ── Search ──────────────────────────────────────────────────────

        [Test]
        public async Task SearchCandidatesAsync_EmptyQuery_ReturnsEmpty()
        {
            SetupPantry(Item("i1", "Milk"));
            await _vm.LoadAsync();

            Assert.AreEqual(0, (await _vm.SearchCandidatesAsync("")).Count);
            Assert.AreEqual(0, (await _vm.SearchCandidatesAsync("   ")).Count);
        }

        [Test]
        public async Task SearchCandidatesAsync_CaseInsensitiveContains_SortedByName()
        {
            SetupPantry(Item("i1", "Whole milk"), Item("i2", "Rice"), Item("i3", "Milk"));
            await _vm.LoadAsync();

            List<PantryItemView> result = await _vm.SearchCandidatesAsync("MILK");

            CollectionAssert.AreEqual(new[] { "Milk", "Whole milk" }, result.Select(v => v.DisplayName).ToArray());
        }

        // ── Delete ──────────────────────────────────────────────────────

        [Test]
        public async Task DeleteWasteAsync_Success_RemovesRecord()
        {
            SetupHistory(1, Page(1, 1, Waste("w1", "2026-09-05T10:00:00Z"), Waste("w2", "2026-09-06T10:00:00Z")));
            _mockFoodWasteService
                .Setup(x => x.DeleteAsync("w1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));
            await _vm.LoadAsync();

            bool ok = await _vm.DeleteWasteAsync("w1");

            Assert.IsTrue(ok);
            CollectionAssert.AreEqual(new[] { "w2" }, _vm.History.Select(w => w.id).ToArray());
        }

        [Test]
        public async Task DeleteWasteAsync_Error_KeepsRecordAndSetsErrorDetail()
        {
            var error = new ApiErrorResponse { message = "nope" };
            SetupHistory(1, Page(1, 1, Waste("w1", "2026-09-05T10:00:00Z")));
            _mockFoodWasteService
                .Setup(x => x.DeleteAsync("w1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((false, error)));
            await _vm.LoadAsync();

            bool ok = await _vm.DeleteWasteAsync("w1");

            Assert.IsFalse(ok);
            Assert.AreEqual(1, _vm.History.Count);
            Assert.AreSame(error, _vm.ErrorDetail);
        }

        // ── Batch waste ─────────────────────────────────────────────────

        [Test]
        public async Task BatchWasteExpiredAsync_NoExpired_ReturnsZeroWithoutCall()
        {
            await _vm.LoadAsync();

            int wasted = await _vm.BatchWasteExpiredAsync();

            Assert.AreEqual(0, wasted);
            _mockPantryService.Verify(x => x.BatchWasteAsync(It.IsAny<BatchWasteRequest>()), Times.Never);
        }

        [Test]
        public async Task BatchWasteExpiredAsync_SendsExpiredIdsReportsAndReloads()
        {
            SetupPantry(Item("i1", "Milk"));
            SetupExpired(new ExpiredPantryItem { pantryItemId = "i1", expiryDate = Yesterday });
            BatchWasteRequest sent = null;
            _mockPantryService
                .Setup(x => x.BatchWasteAsync(It.IsAny<BatchWasteRequest>()))
                .Callback<BatchWasteRequest>(r => sent = r)
                .Returns(Task.FromResult<(BatchWasteResult Result, ApiErrorResponse Error)>((new BatchWasteResult
                {
                    successCount = 1,
                    successes = new[] { Waste("w1", "2026-09-29T10:00:00Z", "i1") }
                }, null)));
            await _vm.LoadAsync();

            int wasted = await _vm.BatchWasteExpiredAsync();

            Assert.AreEqual(1, wasted);
            CollectionAssert.AreEqual(new[] { "i1" }, sent.items.Select(i => i.pantryItemId).ToArray());
            _mockChallengeSession.Verify(x => x.ReportAsync(ChallengeCompletionTrigger.FoodWasteLogged, "w1"), Times.Once);
            _mockNotificationService.Verify(x => x.CancelPantryReminder("i1"), Times.Once);
            _mockPantryService.Verify(x => x.GetPantryAsync(), Times.Exactly(2));
        }

        [Test]
        public async Task BatchWasteExpiredAsync_RequestError_KeepsErrorAfterReload()
        {
            var error = new ApiErrorResponse { message = "down" };
            SetupPantry(Item("i1", "Milk"));
            SetupExpired(new ExpiredPantryItem { pantryItemId = "i1", expiryDate = Yesterday });
            _mockPantryService
                .Setup(x => x.BatchWasteAsync(It.IsAny<BatchWasteRequest>()))
                .Returns(Task.FromResult<(BatchWasteResult Result, ApiErrorResponse Error)>((null, error)));
            await _vm.LoadAsync();

            int wasted = await _vm.BatchWasteExpiredAsync();

            Assert.AreEqual(0, wasted);
            Assert.AreSame(error, _vm.ErrorDetail);
            _mockNotificationService.Verify(x => x.CancelPantryReminder(It.IsAny<string>()), Times.Never);
            _mockChallengeSession.Verify(x => x.ReportAsync(It.IsAny<ChallengeCompletionTrigger>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task BatchWasteExpiredAsync_PartialFailureInBody_SetsErrorDetailFromErrors()
        {
            SetupPantry(Item("i1", "Milk"), Item("i2", "Rice"));
            SetupExpired(
                new ExpiredPantryItem { pantryItemId = "i1", expiryDate = Yesterday },
                new ExpiredPantryItem { pantryItemId = "i2", expiryDate = Yesterday });
            _mockPantryService
                .Setup(x => x.BatchWasteAsync(It.IsAny<BatchWasteRequest>()))
                .Returns(Task.FromResult<(BatchWasteResult Result, ApiErrorResponse Error)>((new BatchWasteResult
                {
                    successCount = 1,
                    errorCount = 1,
                    successes = new[] { Waste("w1", "2026-09-29T10:00:00Z", "i1") },
                    errors = new[] { new BatchWasteErrorItem { pantryItemId = "i2", error = "Pantry item not found" } }
                }, null)));
            await _vm.LoadAsync();

            int wasted = await _vm.BatchWasteExpiredAsync();

            Assert.AreEqual(1, wasted);
            Assert.IsNotNull(_vm.ErrorDetail);
            StringAssert.Contains("Pantry item not found", _vm.ErrorDetail.message);
            _mockNotificationService.Verify(x => x.CancelPantryReminder("i1"), Times.Once);
            _mockNotificationService.Verify(x => x.CancelPantryReminder("i2"), Times.Never);
            _mockChallengeSession.Verify(x => x.ReportAsync(ChallengeCompletionTrigger.FoodWasteLogged, It.IsAny<string>()), Times.Once);
        }

        // ── OnWasteRecorded ─────────────────────────────────────────────

        [Test]
        public async Task OnWasteRecorded_ReloadsData()
        {
            await _vm.LoadAsync();

            _vm.OnWasteRecorded();
            await Task.Yield();

            _mockFoodWasteService.Verify(x => x.GetListAsync(1, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }
    }
}
