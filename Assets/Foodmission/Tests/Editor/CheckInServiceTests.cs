using System;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CheckInServiceTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Local);

        private TestStoreService _store;
        private Mock<IQuestService> _quests;
        private Mock<IMissionService> _missions;
        private Mock<IMealLogService> _mealLogs;
        private Mock<IGamificationService> _gamification;
        private CheckInService _service;
        private string _dateFrom;
        private string _dateTo;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "q1" });
            _quests = new Mock<IQuestService>();
            _missions = new Mock<IMissionService>();
            _mealLogs = new Mock<IMealLogService>();
            _gamification = new Mock<IGamificationService>();

            SetQuest("M.A1.3", "M.A5.4", "M.B2.1", "M.A2.1");
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((new[]
            {
                new MissionProgress { missionCode = "M.A1.3", missionTitle = "Legumes", progress = 20, startedAt = Now.AddDays(-1).ToUniversalTime() },
                new MissionProgress { missionCode = "M.A5.4", missionTitle = "FIFO", progress = 0, startedAt = Now.AddDays(-2).ToUniversalTime() },
                new MissionProgress { missionCode = "M.B2.1", missionTitle = "Origin", progress = 100, completed = true }
            }, (ApiErrorResponse)null));
            _mealLogs.Setup(m => m.GetLogsAsync(1, CheckInService.MealLogsLimit, null, It.IsAny<string>(), It.IsAny<string>()))
                .Callback<int, int, string, string, string>((_, _, _, from, to) => { _dateFrom = from; _dateTo = to; })
                .ReturnsAsync((new PaginatedMealLogResponse { data = new[]
                {
                    new MealLog { typeOfMeal = "LUNCH", timestamp = Now.Date.AddDays(-1).AddHours(13).ToUniversalTime().ToString("o") }
                } }, (ApiErrorResponse)null));
            _gamification.Setup(g => g.GetGamificationProfileAsync(CheckInService.EventsLimit, 1)).ReturnsAsync((new GamificationProfileResponse
            {
                recentEvents = new[]
                {
                    new UserEvent { eventType = ClientEventTypes.FoodWasteFifoOrganized, metadata = JObject.Parse("{\"dayBucket\":\"" + Now.Date.AddDays(-2).ToString("yyyy-MM-dd") + "\"}") }
                }
            }, (ApiErrorResponse)null));

            _service = new CheckInService(_store, _quests.Object, _missions.Object, _mealLogs.Object, _gamification.Object) { Clock = () => Now };
        }

        private void SetQuest(params string[] codes)
        {
            var items = codes.Select(c => new QuestItem { contentType = QuestContentType.Mission, contentCode = c }).ToArray();
            _quests.Setup(q => q.GetQuestAsync("q1", null)).ReturnsAsync((new Quest { id = "q1", items = items }, (ApiErrorResponse)null));
        }

        [Test]
        public async Task LoadPlan_BuildsPlanFromQuestProgressMealLogsAndEvents()
        {
            var (plan, error) = await _service.LoadPlanAsync();

            Assert.IsNull(error);
            // A1.3 since yesterday: yesterday (lunch logged → other types open) and today
            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-1), Now.Date }, plan.MealDays.Select(d => d.Day).ToArray());
            Assert.IsFalse(plan.MealDays[0].OpenMealTypes.Contains("LUNCH"));
            // A5.4: today only (no occurredAt in backend (v0.3.1))
            CollectionAssert.AreEqual(new[] { Now.Date }, plan.DayEvents.Single().OpenDays);
            // B2.1 completed, A2.1 pending rule → no mission steps
            Assert.AreEqual(0, plan.MissionSteps.Count);
        }

        [Test]
        public async Task LoadPlan_RequestsMealLogsWithIsoInstantRange()
        {
            await _service.LoadPlanAsync();

            Assert.AreEqual(Now.Date.AddDays(-6).ToUniversalTime().ToString("o"), _dateFrom);
            Assert.AreEqual(Now.ToUniversalTime().ToString("o"), _dateTo);
        }

        [Test]
        public async Task LoadPlan_SingleMission_OnlyThatMission()
        {
            var (plan, _) = await _service.LoadPlanAsync("M.A5.4");

            Assert.AreEqual(0, plan.MealDays.Count);
            Assert.AreEqual(1, plan.DayEvents.Count);
            _mealLogs.Verify(m => m.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never, "no meal missions → no meal-log call");
        }

        [Test]
        public async Task LoadPlan_NoCurrentQuest_ReturnsEmpty()
        {
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "" });

            var (plan, error) = await _service.LoadPlanAsync();

            Assert.IsNull(error);
            Assert.IsTrue(plan.IsEmpty);
        }

        [Test]
        public async Task LoadPlan_ApiError_IsReturned()
        {
            var apiError = new ApiErrorResponse { message = "x" };
            _gamification.Setup(g => g.GetGamificationProfileAsync(CheckInService.EventsLimit, 1)).ReturnsAsync(((GamificationProfileResponse)null, apiError));

            var (plan, error) = await _service.LoadPlanAsync();

            Assert.IsNull(plan);
            Assert.AreSame(apiError, error);
        }

        [Test]
        public void ReadDayBucket_AcceptsStringOrDateToken()
        {
            Assert.AreEqual(new DateTime(2026, 10, 8), CheckInService.ReadDayBucket(JObject.Parse("{\"dayBucket\":\"2026-10-08\"}")));
            var dateToken = new JObject { ["dayBucket"] = new JValue(new DateTime(2026, 10, 8)) };
            Assert.AreEqual(new DateTime(2026, 10, 8), CheckInService.ReadDayBucket(dateToken));
            Assert.IsNull(CheckInService.ReadDayBucket(new JObject()));
            Assert.IsNull(CheckInService.ReadDayBucket(null));
        }

        [Test]
        public async Task LoadPlan_SkipsFailedMissions()
        {
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((new[]
            {
                new MissionProgress { missionCode = "M.A1.3", missionTitle = "Legumes", progress = 20, status = ProgressStatus.Failed, startedAt = Now.AddDays(-1).ToUniversalTime() },
                new MissionProgress { missionCode = "M.A5.4", missionTitle = "FIFO", progress = 0, startedAt = Now.AddDays(-2).ToUniversalTime() }
            }, (ApiErrorResponse)null));

            var (plan, error) = await _service.LoadPlanAsync();

            Assert.IsNull(error);
            Assert.AreEqual(0, plan.MealDays.Count, "the failed meal mission asks nothing");
            Assert.AreEqual(1, plan.DayEvents.Count);
        }
    }
}
