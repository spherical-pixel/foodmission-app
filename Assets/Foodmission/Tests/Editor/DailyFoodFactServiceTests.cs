using System;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DailyFoodFactServiceTests
    {
        private TestStoreService _store;
        private TestLocalStorageService _storage;
        private Mock<IQuestService> _quests;
        private Mock<IFoodFactService> _facts;
        private DailyFoodFactService _service;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _now = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Local);
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "q1" });
            _storage = new TestLocalStorageService();
            _quests = new Mock<IQuestService>();
            _facts = new Mock<IFoodFactService>();
            SetQuest();
            SetRead();
            _service = new DailyFoodFactService(_store, _quests.Object, _facts.Object, _storage)
            {
                NowLocal = () => _now
            };
        }

        private void SetQuest(params QuestItem[] items)
        {
            _quests.Setup(q => q.GetQuestAsync("q1", null))
                .ReturnsAsync((new Quest { id = "q1", dimensionId = "dim-1", items = items }, (ApiErrorResponse)null));
        }

        private void SetRead(params string[] codes)
        {
            var progress = Array.ConvertAll(codes, c => new FoodFactProgressResponse { foodFactCode = c });
            _facts.Setup(f => f.GetUserProgressListAsync()).ReturnsAsync((progress, (ApiErrorResponse)null));
        }

        private static QuestItem Fact(string code, int order) =>
            new QuestItem { contentType = QuestContentType.FoodFact, contentCode = code, sortOrder = order };

        private static QuestItem Mission(string code, int order) =>
            new QuestItem { contentType = QuestContentType.Mission, contentCode = code, sortOrder = order };

        [Test]
        public async Task NoActiveQuest_ReturnsNull()
        {
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "" });
            SetQuest(Fact("FF1", 1));

            Assert.IsNull(await _service.GetFactToShowAsync());
            _quests.Verify(q => q.GetQuestAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ReturnsFirstUnreadQuestFactBySortOrder()
        {
            SetQuest(Mission("M.A1.1", 0), Fact("FF3", 3), Fact("FF1", 1), Fact("FF2", 2));
            SetRead("FF1");

            Assert.AreEqual("FF2", await _service.GetFactToShowAsync());
        }

        [Test]
        public async Task ReadByIdCountsAsRead()
        {
            SetQuest(Fact("FF1", 1), Fact("FF2", 2));
            _facts.Setup(f => f.GetUserProgressListAsync())
                .ReturnsAsync((new[] { new FoodFactProgressResponse { foodFactId = "FF1" } }, (ApiErrorResponse)null));

            Assert.AreEqual("FF2", await _service.GetFactToShowAsync());
        }

        [Test]
        public async Task OncePerDay()
        {
            SetQuest(Fact("FF1", 1), Fact("FF2", 2));

            Assert.AreEqual("FF1", await _service.GetFactToShowAsync());
            Assert.IsNull(await _service.GetFactToShowAsync(), "Second Home entry on the same day");

            _now = _now.AddDays(1);
            Assert.AreEqual("FF1", await _service.GetFactToShowAsync(), "Still unread the next day");
        }

        [Test]
        public async Task QuestWithoutFacts_ReturnsNull()
        {
            SetQuest(Mission("M.A1.1", 0));

            Assert.IsNull(await _service.GetFactToShowAsync());
        }

        [Test]
        public async Task AllQuestFactsRead_ReturnsNullAndDoesNotUseTheDay()
        {
            SetQuest(Fact("FF1", 1), Fact("FF2", 2));
            SetRead("FF1", "FF2");

            Assert.IsNull(await _service.GetFactToShowAsync());
            Assert.IsFalse(_storage.HasValue(DailyFoodFactService.StorageKey("u1")));
        }

        [Test]
        public async Task ProgressError_ReturnsNull()
        {
            SetQuest(Fact("FF1", 1));
            _facts.Setup(f => f.GetUserProgressListAsync())
                .ReturnsAsync(((FoodFactProgressResponse[])null, new ApiErrorResponse { message = "boom" }));

            Assert.IsNull(await _service.GetFactToShowAsync());
            Assert.IsFalse(_storage.HasValue(DailyFoodFactService.StorageKey("u1")));
        }

        [Test]
        public async Task QuestError_ReturnsNull()
        {
            _quests.Setup(q => q.GetQuestAsync("q1", null))
                .ReturnsAsync(((Quest)null, new ApiErrorResponse { message = "boom" }));

            Assert.IsNull(await _service.GetFactToShowAsync());
        }

        [Test]
        public async Task Reset_AllowsAnotherFactToday()
        {
            SetQuest(Fact("FF1", 1));
            Assert.AreEqual("FF1", await _service.GetFactToShowAsync());

            _service.Reset();

            Assert.AreEqual("FF1", await _service.GetFactToShowAsync());
        }

        [Test]
        public void ShiftStoredDates_MovesTheDayBack()
        {
            Assert.AreEqual("2026-10-07", DailyFoodFactService.ShiftStoredDates("2026-10-10", 3));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not a date")]
        public void ShiftStoredDates_LeavesEmptyOrCorruptUnchanged(string value)
        {
            Assert.AreEqual(value, DailyFoodFactService.ShiftStoredDates(value, 3));
        }
    }
}
