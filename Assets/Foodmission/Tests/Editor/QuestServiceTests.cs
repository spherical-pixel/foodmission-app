using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuestServiceTests
    {
        private TestStoreService _storeService;
        private QuestService _service;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-jwt-token",
                tokenType = "Bearer",
                lang = "es"
            });
            _service = new QuestService(_storeService);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        [Test]
        public async Task GetQuestAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetQuestAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetQuestAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetQuestAsync_ConcurrentCalls_ShareOneRequest()
        {
            // Home asks for the current quest from several prompts at once; the backend allows 5 requests/s per route
            int calls = 0;
            var pending = new TaskCompletionSource<(Quest, ApiErrorResponse)>();
            _service.Fetch = (_, _) => { calls++; return pending.Task; };

            var first = _service.GetQuestAsync("q1");
            var second = _service.GetQuestAsync("q1");
            pending.SetResult((new Quest { id = "q1" }, null));

            Assert.AreEqual("q1", (await first).Result.id);
            Assert.AreEqual("q1", (await second).Result.id);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public async Task GetQuestAsync_ReusesAResultForAWhile_PerLanguage()
        {
            int calls = 0;
            DateTime now = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
            _service.Clock = () => now;
            _service.Fetch = (id, lang) => { calls++; return Task.FromResult((new Quest { id = id, title = lang }, (ApiErrorResponse)null)); };

            await _service.GetQuestAsync("q1");
            await _service.GetQuestAsync("q1");
            Assert.AreEqual(1, calls);

            Assert.AreEqual("en", (await _service.GetQuestAsync("q1", "en")).Result.title);
            Assert.AreEqual(2, calls, "another language is another quest text");

            now = now.Add(QuestService.CacheDuration);
            await _service.GetQuestAsync("q1");
            Assert.AreEqual(3, calls, "expired");
        }

        [Test]
        public async Task GetQuestAsync_DoesNotKeepErrors()
        {
            int calls = 0;
            _service.Fetch = (_, _) => { calls++; return Task.FromResult(((Quest)null, new ApiErrorResponse { statusCode = 429 })); };

            await _service.GetQuestAsync("q1");
            var (result, error) = await _service.GetQuestAsync("q1");

            Assert.AreEqual(2, calls);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetQuestProgressAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetQuestProgressAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetQuestProgressAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetUserProgressListAsync_WhenUnauthenticated_ReturnsAuthError()
        {
            _storeService.SetAppState(new AppState
            {
                accessToken = null
            });

            var (result, error) = await _service.GetUserProgressListAsync();
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Authentication required", error.message);
        }

        [Test]
        public async Task UpdateQuestProgressAsync_OnEmptyCodeOrId_ReturnsError()
        {
            var (result, error) = await _service.UpdateQuestProgressAsync("", true, 100f);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Quest code or id is required", error.message);

            var (resultNull, errorNull) = await _service.UpdateQuestProgressAsync(null, true, 100f);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
        }
    }
}
