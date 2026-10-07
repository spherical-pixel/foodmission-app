using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionServiceTests
    {
        private TestStoreService _storeService;
        private MissionService _service;
        private Func<bool> _originalOverride;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-jwt-token",
                tokenType = "Bearer",
                lang = "es"
            });
            _service = new MissionService(_storeService);
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
        }

        [Test]
        public async Task GetUserProgressListAsync_ConcurrentCalls_ShareOneRequest()
        {
            int calls = 0;
            var pending = new TaskCompletionSource<(MissionProgress[], ApiErrorResponse)>();
            _service.FetchProgressList = _ => { calls++; return pending.Task; };

            var first = _service.GetUserProgressListAsync();
            var second = _service.GetUserProgressListAsync();
            pending.SetResult((new[] { new MissionProgress { missionCode = "M.A1.1" } }, null));

            Assert.AreEqual("M.A1.1", (await first).Result[0].missionCode);
            Assert.AreEqual("M.A1.1", (await second).Result[0].missionCode);
            Assert.AreEqual(1, calls);
        }

        [Test]
        public async Task GetUserProgressListAsync_ReusesAResultForAFewSeconds()
        {
            // Home's prompts ask one after another; the backend allows 5 requests/s per route
            int calls = 0;
            DateTime now = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
            _service.Clock = () => now;
            _service.FetchProgressList = _ => { calls++; return Task.FromResult((new MissionProgress[0], (ApiErrorResponse)null)); };

            await _service.GetUserProgressListAsync();
            await _service.GetUserProgressListAsync();
            Assert.AreEqual(1, calls);

            now = now.Add(MissionService.ProgressReuseWindow);
            await _service.GetUserProgressListAsync();
            Assert.AreEqual(2, calls);
        }

        [Test]
        public async Task GetUserProgressListAsync_AfterTheUserRecordsSomething_FetchesAgain()
        {
            int calls = 0;
            _service.Clock = () => new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
            _service.FetchProgressList = _ => { calls++; return Task.FromResult((new MissionProgress[0], (ApiErrorResponse)null)); };

            await _service.GetUserProgressListAsync();
            UserProgressWrites.Record();
            await _service.GetUserProgressListAsync();

            Assert.AreEqual(2, calls);
        }

        [Test]
        public async Task GetUserProgressListAsync_DoesNotKeepErrors()
        {
            int calls = 0;
            _service.FetchProgressList = _ => { calls++; return Task.FromResult(((MissionProgress[])null, new ApiErrorResponse { statusCode = 429 })); };

            await _service.GetUserProgressListAsync();
            await _service.GetUserProgressListAsync();

            Assert.AreEqual(2, calls);
        }

        [Test]
        public async Task GetMissionAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetMissionAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetMissionAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetMissionProgressAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetMissionProgressAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetMissionProgressAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetUserProgressListAsync_WhenUnauthenticated_ReturnsAuthError()
        {
            _storeService.SetAppState(new AppState
            {
                accessToken = null,
                tokenType = null
            });

            var (result, error) = await _service.GetUserProgressListAsync();
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Authentication required", error.message);
        }

        [Test]
        public async Task UpdateMissionProgressAsync_OnEmptyCodeOrId_ReturnsError()
        {
            var (result, error) = await _service.UpdateMissionProgressAsync("", true, 100f);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Mission code or id is required", error.message);

            var (resultNull, errorNull) = await _service.UpdateMissionProgressAsync(null, true, 100f);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
        }

        [Test]
        public async Task UpdateMissionProgressAsync_WhenUnauthenticated_ReturnsAuthError()
        {
            _storeService.SetAppState(new AppState
            {
                accessToken = null,
                tokenType = null
            });

            var (result, error) = await _service.UpdateMissionProgressAsync("M.A1.1", true, 100f);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Authentication required", error.message);
        }

        [Test]
        public void FillMissionCodes_SetsCodeFromMissionId_WhenBackendOmitsIt()
        {
            // GET /missions/progress returns missionId but no missionCode
            var list = new[]
            {
                new MissionProgress { missionId = "id-1", startedAt = new DateTime(2026, 10, 2, 6, 50, 0, DateTimeKind.Utc) },
                new MissionProgress { missionId = "id-2", missionCode = "M.KEEP" },
                new MissionProgress { missionId = "id-unknown" },
                null
            };

            int missing = MissionService.FillMissionCodes(list, id => id == "id-1" ? "M.B1.1" : id == "id-2" ? "M.OTHER" : null);

            Assert.AreEqual("M.B1.1", list[0].missionCode);
            Assert.AreEqual("M.KEEP", list[1].missionCode);
            Assert.IsNull(list[2].missionCode);
            Assert.AreEqual(1, missing);
        }

        [Test]
        public void BuildProgressUrl_ByCodeAndByUuid()
        {
            Assert.AreEqual("https://x/api/v1/missions/by-code/M.A1.1/progress/restart?lang=es",
                MissionService.BuildProgressUrl("https://x", "M.A1.1", "es", "/restart"));
            string uuid = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
            Assert.AreEqual($"https://x/api/v1/missions/{uuid}/progress?lang=en",
                MissionService.BuildProgressUrl("https://x", uuid, "en"));
        }

        [Test]
        public async Task RestartMissionProgressAsync_OnEmptyCode_ReturnsError()
        {
            var (result, error) = await _service.RestartMissionProgressAsync("");
            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task RestartMissionProgressAsync_WhenUnauthenticated_ReturnsAuthError()
        {
            _storeService.SetAppState(new AppState { accessToken = null, tokenType = null });
            var (result, error) = await _service.RestartMissionProgressAsync("M.A1.1");
            Assert.IsNull(result);
            Assert.AreEqual("Authentication required", error.message);
        }

        [Test]
        public async Task FailMissionProgressAsync_OnEmptyCode_ReturnsError()
        {
            var (result, error) = await _service.FailMissionProgressAsync(null);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task FailMissionProgressAsync_WhenUnauthenticated_ReturnsAuthError()
        {
            _storeService.SetAppState(new AppState { accessToken = null, tokenType = null });
            var (result, error) = await _service.FailMissionProgressAsync("M.A1.1");
            Assert.IsNull(result);
            Assert.AreEqual("Authentication required", error.message);
        }
    }
}
