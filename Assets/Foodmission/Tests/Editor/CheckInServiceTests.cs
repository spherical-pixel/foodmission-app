using System;
using System.Collections.Generic;
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
        private TestLocalStorageService _storage;
        private string _dateFrom;
        private string _dateTo;
        private string[] _requestedTypes;
        private UserEvent[] _recentEvents;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "q1" });
            _quests = new Mock<IQuestService>();
            _missions = new Mock<IMissionService>();
            _mealLogs = new Mock<IMealLogService>();
            _gamification = new Mock<IGamificationService>();

            SetQuest("M.A1.3", "M.A5.4", "M.B2.1", "M.Z9.9");
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((new[]
            {
                new MissionProgress { missionCode = "M.A1.3", missionTitle = "Legumes", progress = 20, startedAt = Now.Date.AddDays(-1).ToUniversalTime() },
                new MissionProgress { missionCode = "M.A5.4", missionTitle = "FIFO", progress = 0, startedAt = Now.AddDays(-2).ToUniversalTime() },
                new MissionProgress { missionCode = "M.B2.1", missionTitle = "Origin", progress = 100, completed = true }
            }, (ApiErrorResponse)null));
            _mealLogs.Setup(m => m.GetLogsAsync(1, CheckInService.MealLogsLimit, null, It.IsAny<string>(), It.IsAny<string>()))
                .Callback<int, int, string, string, string>((_, _, _, from, to) => { _dateFrom = from; _dateTo = to; })
                .ReturnsAsync((new PaginatedMealLogResponse { data = new[]
                {
                    new MealLog { typeOfMeal = "LUNCH", timestamp = Now.Date.AddDays(-1).AddHours(13).ToUniversalTime().ToString("o") }
                } }, (ApiErrorResponse)null));
            _requestedTypes = null;
            _recentEvents = new[]
            {
                new UserEvent { eventType = ClientEventTypes.FoodWasteFifoOrganized, metadata = JObject.Parse("{\"dayBucket\":\"" + Now.Date.AddDays(-2).ToString("yyyy-MM-dd") + "\"}") }
            };
            _gamification.Setup(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), CheckInService.EventsLimit))
                .Callback<IReadOnlyCollection<string>, int>((types, _) => _requestedTypes = types.ToArray())
                .ReturnsAsync(() => (_recentEvents, (ApiErrorResponse)null));

            _storage = new TestLocalStorageService();
            _service = new CheckInService(_store, _quests.Object, _missions.Object, _mealLogs.Object, _gamification.Object, _storage) { Clock = () => Now };
        }

        private void StartMissionToo(string code, DateTime startedLocal)
        {
            // Since backend #424 only started missions are active: give the mission a started row
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((new[]
            {
                new MissionProgress { missionCode = "M.A1.3", missionTitle = "Legumes", progress = 20, startedAt = Now.Date.AddDays(-1).ToUniversalTime() },
                new MissionProgress { missionCode = "M.A5.4", missionTitle = "FIFO", progress = 0, startedAt = Now.AddDays(-2).ToUniversalTime() },
                new MissionProgress { missionCode = "M.B2.1", missionTitle = "Origin", progress = 100, completed = true },
                new MissionProgress { missionCode = code, missionTitle = code, progress = 0, status = ProgressStatus.InProgress, startedAt = startedLocal.ToUniversalTime() }
            }, (ApiErrorResponse)null));
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
            // A5.4 since 2 days ago, -2 already recorded
            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-1), Now.Date }, plan.DayEvents.Single().OpenDays);
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
            _gamification.Setup(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), CheckInService.EventsLimit)).ReturnsAsync(((UserEvent[])null, apiError));

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
                new MissionProgress { missionCode = "M.A1.3", missionTitle = "Legumes", progress = 20, status = ProgressStatus.Failed, startedAt = Now.Date.AddDays(-1).ToUniversalTime() },
                new MissionProgress { missionCode = "M.A5.4", missionTitle = "FIFO", progress = 0, startedAt = Now.AddDays(-2).ToUniversalTime() }
            }, (ApiErrorResponse)null));

            var (plan, error) = await _service.LoadPlanAsync();

            Assert.IsNull(error);
            Assert.AreEqual(0, plan.MealDays.Count, "the failed meal mission asks nothing");
            Assert.AreEqual(1, plan.DayEvents.Count);
        }

        [Test]
        public async Task LoadPlan_AsksRecentEventsOnlyForDayStepTypes()
        {
            SetQuest("M.A5.4", "M.A6.1", "M.B2.1");
            StartMissionToo("M.A6.1", Now.Date.AddDays(-3));

            await _service.LoadPlanAsync();

            CollectionAssert.AreEquivalent(new[] { ClientEventTypes.FoodWasteFifoOrganized, ClientEventTypes.NutritionRainbowColoursLogged }, _requestedTypes);
        }

        [Test]
        public async Task LoadPlan_NoDayStep_SkipsRecentEvents()
        {
            SetQuest("M.A1.3");

            await _service.LoadPlanAsync();

            _gamification.Verify(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task LoadPlan_RecordedValues_UpperCasedAndNonArraysIgnored()
        {
            SetQuest("M.A6.1");
            StartMissionToo("M.A6.1", Now.Date.AddDays(-3));
            string day = Now.Date.AddDays(-1).ToString("yyyy-MM-dd");
            _recentEvents = new[]
            {
                new UserEvent { eventType = ClientEventTypes.NutritionRainbowColoursLogged, metadata = JObject.Parse("{\"dayBucket\":\"" + day + "\",\"colours\":[\"green\",\"Red\"]}") },
                new UserEvent { eventType = ClientEventTypes.NutritionRainbowColoursLogged, metadata = JObject.Parse("{\"dayBucket\":\"" + day + "\",\"colours\":\"ORANGE\"}") }
            };

            var (plan, _) = await _service.LoadPlanAsync();

            var step = plan.MissionSteps.Single();
            CollectionAssert.AreEquivalent(new[] { "GREEN", "RED" }, step.RecordedValuesFor(Now.Date.AddDays(-1)).ToArray());
        }

        [Test]
        public async Task LoadPlan_BackendWithoutEventTypeFilter_FallsBackToUnfilteredEvents()
        {
            // Backends before #419 reject the eventTypes query parameter with a 400
            _gamification.Setup(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), CheckInService.EventsLimit))
                .ReturnsAsync(((UserEvent[])null, new ApiErrorResponse { statusCode = 400, message = "property eventTypes should not exist" }));
            _gamification.Setup(g => g.GetGamificationProfileAsync(CheckInService.EventsLimit, 1)).ReturnsAsync((new GamificationProfileResponse
            {
                recentEvents = new[]
                {
                    new UserEvent { eventType = ClientEventTypes.FoodWasteFifoOrganized, metadata = JObject.Parse("{\"dayBucket\":\"" + Now.Date.AddDays(-2).ToString("yyyy-MM-dd") + "\"}") },
                    new UserEvent { eventType = ClientEventTypes.MealLogged, metadata = JObject.Parse("{\"dayBucket\":\"" + Now.Date.AddDays(-1).ToString("yyyy-MM-dd") + "\"}") }
                }
            }, (ApiErrorResponse)null));

            var (plan, error) = await _service.LoadPlanAsync();

            Assert.IsNull(error);
            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-1), Now.Date }, plan.DayEvents.Single().OpenDays);
        }

        [Test]
        public async Task LoadActiveMissions_SkipsResolvedAndUnknownMissions_AndKeepsProgress()
        {
            var (missions, progress, error) = await CheckInService.LoadActiveMissionsAsync(_store, _quests.Object, _missions.Object);

            Assert.IsNull(error);
            CollectionAssert.AreEquivalent(new[] { "M.A1.3", "M.A5.4" }, missions.Select(m => m.Code).ToArray());
            Assert.AreEqual(20, progress["M.A1.3"].progress);
        }

        [Test]
        public async Task LoadActiveMissions_SkipsMissionsNotStarted()
        {
            SetQuest("M.A1.3", "M.A5.4", "M.B2.1");
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((new[]
            {
                new MissionProgress { missionId = "id-a13", missionCode = "M.A1.3", progress = 0, status = ProgressStatus.InProgress, startedAt = Now.AddDays(-1).ToUniversalTime() },
                new MissionProgress { missionId = "id-a54", missionCode = "M.A5.4", progress = 0, status = ProgressStatus.NotStarted, startedAt = null }
                // M.B2.1 has no row: not started
            }, (ApiErrorResponse)null));

            var (missions, _, error) = await CheckInService.LoadActiveMissionsAsync(_store, _quests.Object, _missions.Object);

            Assert.IsNull(error);
            CollectionAssert.AreEqual(new[] { "M.A1.3" }, missions.Select(m => m.Code).ToArray());
        }

        [Test]
        public async Task LoadPlan_CountsRealProductReports_IgnoringCheckInProducts()
        {
            SetQuest("M.B2.5");
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((new[]
            {
                new MissionProgress { missionCode = "M.B2.5", missionTitle = "Packaging", progress = 0, startedAt = Now.AddDays(-2).ToUniversalTime() }
            }, (ApiErrorResponse)null));
            string at = Now.AddDays(-1).ToUniversalTime().ToString("o");
            _gamification.Setup(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), CheckInService.EventsLimit))
                .ReturnsAsync(() => (_recentEvents, (ApiErrorResponse)null));
            _recentEvents = new[]
            {
                new UserEvent { eventType = ClientEventTypes.ShoppingPackagingInfoChecked, timestamp = at, metadata = JObject.Parse("{\"productId\":\"p-1\"}") },
                new UserEvent { eventType = ClientEventTypes.ShoppingPackagingInfoChecked, timestamp = at, metadata = JObject.Parse("{\"productId\":\"manual-r1-0\"}") }
            };

            var (plan, error) = await _service.LoadPlanAsync();

            Assert.IsNull(error);
            Assert.AreEqual(1, plan.MissionSteps.Single(s => s.MissionCode == "M.B2.5").FromProducts);
        }

        [Test]
        public async Task LoadPlan_ProductEventsFailure_KeepsThePlan()
        {
            // "Already counted from products" is only a hint: failing to load it must not block the check-in
            SetQuest("M.B2.5");
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((new[]
            {
                new MissionProgress { missionCode = "M.B2.5", missionTitle = "Packaging", progress = 0, startedAt = Now.AddDays(-2).ToUniversalTime() }
            }, (ApiErrorResponse)null));
            _gamification.Setup(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), CheckInService.EventsLimit))
                .ReturnsAsync(((UserEvent[])null, new ApiErrorResponse { statusCode = 500, message = "down" }));

            var (plan, error) = await _service.LoadPlanAsync();

            Assert.IsNull(error);
            Assert.AreEqual(0, plan.MissionSteps.Single(s => s.MissionCode == "M.B2.5").FromProducts);
        }

        [Test]
        public async Task LoadPlan_DayEvents_AreLoadedApartFromProductEvents()
        {
            SetQuest("M.A5.4", "M.B2.5");
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((new[]
            {
                new MissionProgress { missionCode = "M.A5.4", missionTitle = "FIFO", progress = 0, startedAt = Now.AddDays(-2).ToUniversalTime() },
                new MissionProgress { missionCode = "M.B2.5", missionTitle = "Packaging", progress = 0, startedAt = Now.AddDays(-2).ToUniversalTime() }
            }, (ApiErrorResponse)null));
            var calls = new List<string[]>();
            _gamification.Setup(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), CheckInService.EventsLimit))
                .Callback<IReadOnlyCollection<string>, int>((types, _) => calls.Add(types.ToArray()))
                .ReturnsAsync(() => (_recentEvents, (ApiErrorResponse)null));

            await _service.LoadPlanAsync();

            Assert.IsTrue(calls.Any(c => c.SequenceEqual(new[] { ClientEventTypes.FoodWasteFifoOrganized })), "day types alone, as before");
        }

        // ── Day questions (spec 2026-10-27) ───────────────────

        private static readonly DateTime DayMissionStart = Now.Date.AddDays(-3);

        private void OnlyDayMission(string code, float progressPercent, DateTime? startLocal = null)
        {
            SetQuest(code);
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((new[]
            {
                new MissionProgress { missionCode = code, missionTitle = code, progress = progressPercent, status = ProgressStatus.InProgress, startedAt = (startLocal ?? DayMissionStart).ToUniversalTime() }
            }, (ApiErrorResponse)null));
        }

        [Test]
        public async Task RememberDayAnswers_ForgottenWhenTheMissionRestarts()
        {
            OnlyDayMission("M.B6.2", 0f);
            _service.RememberDayAnswers(new[] { ("M.B6.2", (DateTime?)DayMissionStart, Now.Date.AddDays(-1)) });

            // Restarted: a new attempt with its own start asks every day of its window again
            OnlyDayMission("M.B6.2", 0f, Now.Date.AddDays(-2).AddHours(10));
            var (plan, _) = await _service.LoadPlanAsync();

            CollectionAssert.Contains(plan.MealDays.Where(d => d.DayQuestions.Count > 0).Select(d => d.Day).ToArray(), Now.Date.AddDays(-1));
        }

        [Test]
        public async Task LoadPlan_DayQuestionMission_LoadsMealLogsAndFlagEvents()
        {
            OnlyDayMission("M.B6.2", 0f);
            _recentEvents = new[]
            {
                new UserEvent { eventType = ClientEventTypes.NutritionFruitVegServingAdded, metadata = JObject.Parse("{\"dayBucket\":\"" + Now.Date.AddDays(-2).ToString("yyyy-MM-dd") + "\"}") }
            };

            var (plan, error) = await _service.LoadPlanAsync();

            Assert.IsNull(error);
            CollectionAssert.Contains(_requestedTypes, ClientEventTypes.NutritionFruitVegServingAdded);
            _mealLogs.Verify(m => m.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            CollectionAssert.DoesNotContain(plan.MealDays.Where(d => d.DayQuestions.Count > 0).Select(d => d.Day).ToArray(), Now.Date.AddDays(-2));
        }

        [Test]
        public async Task RememberDayAnswers_SkipsThoseDaysNextTime_PerUser()
        {
            OnlyDayMission("M.B6.2", 0f);
            _service.RememberDayAnswers(new[] { ("M.B6.2", (DateTime?)DayMissionStart, Now.Date.AddDays(-2)) });

            var (plan, _) = await _service.LoadPlanAsync();
            CollectionAssert.DoesNotContain(plan.MealDays.Where(d => d.DayQuestions.Count > 0).Select(d => d.Day).ToArray(), Now.Date.AddDays(-2));

            _store.SetAppState(new AppState { userId = "u2", userCurrentQuestId = "q1" });
            var (other, _) = await _service.LoadPlanAsync();
            CollectionAssert.Contains(other.MealDays.Where(d => d.DayQuestions.Count > 0).Select(d => d.Day).ToArray(), Now.Date.AddDays(-2));
        }

        [Test]
        public async Task LoadPlan_PassesProgressToPlanner()
        {
            OnlyDayMission("M.B5.5", 50f);

            var (plan, _) = await _service.LoadPlanAsync();

            Assert.IsTrue(plan.MealDays.SelectMany(d => d.DayQuestions).Any());
            Assert.IsTrue(plan.MealDays.SelectMany(d => d.DayQuestions).All(q => q.Remaining == 2));
        }

        [Test]
        public async Task LoadPlan_DayQuestionEvents_UseFallbackOn400()
        {
            OnlyDayMission("M.B6.2", 0f);
            _gamification.Setup(g => g.GetRecentEventsAsync(It.IsAny<IReadOnlyCollection<string>>(), CheckInService.EventsLimit))
                .ReturnsAsync(((UserEvent[])null, new ApiErrorResponse { statusCode = 400, message = "property eventTypes should not exist" }));
            _gamification.Setup(g => g.GetGamificationProfileAsync(CheckInService.EventsLimit, 1)).ReturnsAsync((new GamificationProfileResponse
            {
                recentEvents = new[]
                {
                    new UserEvent { eventType = ClientEventTypes.NutritionFruitVegServingAdded, metadata = JObject.Parse("{\"dayBucket\":\"" + Now.Date.AddDays(-1).ToString("yyyy-MM-dd") + "\"}") }
                }
            }, (ApiErrorResponse)null));

            var (plan, error) = await _service.LoadPlanAsync();

            Assert.IsNull(error);
            CollectionAssert.DoesNotContain(plan.MealDays.Where(d => d.DayQuestions.Count > 0).Select(d => d.Day).ToArray(), Now.Date.AddDays(-1));
            Assert.AreEqual(3, plan.MealDays.Count(d => d.DayQuestions.Count > 0));
        }
    }
}
