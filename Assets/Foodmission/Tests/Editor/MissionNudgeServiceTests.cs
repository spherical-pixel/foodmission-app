using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionNudgeServiceTests
    {
        private TestStoreService _store;
        private TestLocalStorageService _storage;
        private Mock<IQuestService> _quests;
        private Mock<IMissionService> _missions;
        private Mock<ICheckInService> _checkIn;
        private MissionNudgeService _service;
        private DateTime _now;
        private int _pendingDays;

        [SetUp]
        public void SetUp()
        {
            _now = new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);
            _pendingDays = 0;
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "q1" });
            _storage = new TestLocalStorageService();
            _quests = new Mock<IQuestService>();
            _missions = new Mock<IMissionService>();
            _checkIn = new Mock<ICheckInService>();
            _checkIn.Setup(c => c.LoadPlanAsync(null)).ReturnsAsync(() => (PlanWithPendingDays(_pendingDays), (ApiErrorResponse)null));
            SetQuest("M.B2.1", "M.B3.2", "M.A2.1");
            SetProgress();
            _service = new MissionNudgeService(_store, _quests.Object, _missions.Object, _storage, _checkIn.Object)
            {
                UtcNow = () => _now,
                NowLocal = () => _now.ToLocalTime()
            };
        }

        /// <summary>A plan whose FIFO day event has <paramref name="days"/> open past days.</summary>
        private CheckInPlan PlanWithPendingDays(int days)
        {
            var open = new List<DateTime>();
            for (int i = 1; i <= days; i++)
            {
                open.Add(_now.ToLocalTime().Date.AddDays(-i));
            }
            var dayEvents = new List<CheckInDayEvent>();
            if (open.Count > 0)
            {
                dayEvents.Add(new CheckInDayEvent("M.A5.4", MissionInteractionCatalog.Get("M.A5.4").Steps[0], open));
            }
            return new CheckInPlan(new List<CheckInMealDay>(), dayEvents, new List<CheckInMissionStep>());
        }

        /// <summary>Lets the service see the current quest <paramref name="days"/> days ago, with nothing pending then.</summary>
        private async Task SeeQuestDaysAgo(int days)
        {
            DateTime now = _now;
            int pending = _pendingDays;
            _now = now.AddDays(-days);
            _pendingDays = 0;
            await _service.GetNudgeAsync();
            _now = now;
            _pendingDays = pending;
        }

        private void SetQuest(params string[] missionCodes)
        {
            var items = Array.ConvertAll(missionCodes, c => new QuestItem { contentType = QuestContentType.Mission, contentCode = c });
            _quests.Setup(q => q.GetQuestAsync("q1", null)).ReturnsAsync((new Quest { id = "q1", items = items }, (ApiErrorResponse)null));
        }

        private void SetProgress(params MissionProgress[] progress)
        {
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((progress, (ApiErrorResponse)null));
        }

        private static MissionProgress P(string code, float progress, DateTime? startedAt = null, bool completed = false) =>
            new MissionProgress { missionCode = code, missionTitle = "T " + code, progress = progress, startedAt = startedAt, completed = completed };

        [Test]
        public async Task GetNudge_MissingDaysAtThreshold_WinsOverStalledMission()
        {
            _pendingDays = 3;
            SetProgress(P("M.B2.1", 0, _now.AddDays(-5)));
            await SeeQuestDaysAgo(5);

            var nudge = await _service.GetNudgeAsync();

            Assert.AreEqual(MissionNudgeKind.MissingDays, nudge.Kind);
            Assert.AreEqual(3, nudge.MissingDays);
        }

        [Test]
        public async Task GetNudge_OneMissingDay_FallsBackToStalledMission()
        {
            _pendingDays = 1;
            SetProgress(P("M.B2.1", 0, _now.AddDays(-3)));
            await SeeQuestDaysAgo(5);

            var nudge = await _service.GetNudgeAsync();

            Assert.AreEqual(MissionNudgeKind.StalledMission, nudge.Kind);
            Assert.AreEqual("M.B2.1", nudge.MissionCode);
        }

        [Test]
        public async Task GetNudge_MissingDaysCooldown_ThenStalledCanStillShow()
        {
            _pendingDays = 2;
            SetProgress(P("M.B2.1", 0, _now.AddDays(-3)), P("M.B3.2", 100, completed: true));
            await SeeQuestDaysAgo(5);
            Assert.AreEqual(MissionNudgeKind.MissingDays, (await _service.GetNudgeAsync()).Kind);

            _now = _now.AddHours(1);
            Assert.AreEqual(MissionNudgeKind.StalledMission, (await _service.GetNudgeAsync()).Kind);

            _now = _now.AddHours(1);
            Assert.IsNull(await _service.GetNudgeAsync(), "both on cooldown");
        }

        [Test]
        public async Task GetNudge_FirstSightWithoutStart_DoesNotNudgeUntilTwoDaysPass()
        {
            Assert.IsNull(await _service.GetNudgeAsync());

            _now = _now.AddDays(2).AddMinutes(1);
            Assert.IsNotNull(await _service.GetNudgeAsync());
        }

        [Test]
        public async Task GetNudge_ProgressChanged_ResetsTheStallClock()
        {
            SetProgress(P("M.B2.1", 0, _now.AddDays(-1)), P("M.B3.2", 100, completed: true));
            Assert.IsNull(await _service.GetNudgeAsync());

            _now = _now.AddDays(1.5);
            SetProgress(P("M.B2.1", 40, _now.AddDays(-2.5)), P("M.B3.2", 100, completed: true));
            Assert.IsNull(await _service.GetNudgeAsync());

            _now = _now.AddDays(1);
            Assert.IsNull(await _service.GetNudgeAsync(), "only 1 day since the change");

            _now = _now.AddDays(1.1);
            Assert.AreEqual("M.B2.1", (await _service.GetNudgeAsync()).MissionCode);
        }

        [Test]
        public async Task GetNudge_FirstSightWithProgress_CountsFromNowNotFromStart()
        {
            // Progress may have moved minutes ago: startedAt says nothing about the last change
            SetQuest("M.B2.1");
            SetProgress(P("M.B2.1", 42.8f, _now.AddDays(-5)));

            Assert.IsNull(await _service.GetNudgeAsync());

            _now = _now.AddDays(2).AddMinutes(1);
            Assert.AreEqual("M.B2.1", (await _service.GetNudgeAsync())?.MissionCode, "still nudged once it really stalls");
        }

        [Test]
        public async Task GetNudge_PicksTheLongestStalled()
        {
            SetProgress(P("M.B2.1", 0, _now.AddDays(-3)), P("M.B3.2", 0, _now.AddDays(-5)));

            Assert.AreEqual("M.B3.2", (await _service.GetNudgeAsync()).MissionCode);
        }

        [Test]
        public async Task GetNudge_IgnoresCompletedAndPendingRuleMissions()
        {
            SetQuest("M.B2.1", "M.A2.1");
            SetProgress(P("M.B2.1", 100, _now.AddDays(-5), completed: true), P("M.A2.1", 0, _now.AddDays(-5)));

            Assert.IsNull(await _service.GetNudgeAsync());
        }

        [Test]
        public async Task GetNudge_QuickMealLogMission_ReturnsAutoModule()
        {
            SetQuest("M.B1.4");
            SetProgress(P("M.B1.4", 0, _now.AddDays(-3)));

            Assert.AreSame(MissionInteractionCatalog.QuickMealLog, (await _service.GetNudgeAsync()).AutoModule);
        }

        [Test]
        public async Task GetNudge_NoCurrentQuest_ReturnsNull()
        {
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "" });

            Assert.IsNull(await _service.GetNudgeAsync());
        }

        [Test]
        public async Task GetNudge_WhenProgressCallFails_ReturnsNull()
        {
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync(((MissionProgress[])null, new ApiErrorResponse { message = "x" }));

            Assert.IsNull(await _service.GetNudgeAsync());
        }

        [Test]
        public async Task GetNudge_WhenCheckInFails_StillChecksStalledMissions()
        {
            _checkIn.Setup(c => c.LoadPlanAsync(null)).ReturnsAsync(((CheckInPlan)null, new ApiErrorResponse { message = "x" }));
            SetProgress(P("M.B2.1", 0, _now.AddDays(-3)));

            Assert.AreEqual(MissionNudgeKind.StalledMission, (await _service.GetNudgeAsync()).Kind);
        }

        [Test]
        public async Task GetNudge_WhenQuestCallThrows_ReturnsNullAndLogs()
        {
            _quests.Setup(q => q.GetQuestAsync("q1", null)).ThrowsAsync(new Exception("boom"));

            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("MissionNudgeService"));
            Assert.IsNull(await _service.GetNudgeAsync());
        }

        [Test]
        public async Task Reset_ForgetsTheUserState()
        {
            SetProgress(P("M.B2.1", 0, _now.AddDays(-3)), P("M.B3.2", 100, completed: true));
            Assert.IsNotNull(await _service.GetNudgeAsync());

            _service.Reset();

            Assert.IsNotNull(await _service.GetNudgeAsync(), "cooldown forgotten after reset");
        }

        [Test]
        public async Task GetNudge_MissingDays_IgnoresDaysBeforeTheQuestWasFirstSeen()
        {
            // startedAt is null until the first progress, so the plan covers 7 days; a user who just picked
            // the quest must not hear about days before that
            _pendingDays = 6;

            var first = await _service.GetNudgeAsync();
            Assert.IsTrue(first == null || first.Kind != MissionNudgeKind.MissingDays);

            _now = _now.AddDays(3);
            _pendingDays = 6;
            var later = await _service.GetNudgeAsync();
            Assert.AreEqual(MissionNudgeKind.MissingDays, later.Kind);
            Assert.AreEqual(3, later.MissingDays, "only the days since the quest was first seen");
        }

        [Test]
        public async Task GetNudge_IsEvaluatedAtMostEvery30Minutes()
        {
            await _service.GetNudgeAsync();
            _now = _now.AddMinutes(10);
            await _service.GetNudgeAsync();
            _checkIn.Verify(c => c.LoadPlanAsync(null), Times.Once);
            _missions.Verify(m => m.GetUserProgressListAsync(null), Times.Once);

            _now = _now.AddMinutes(25);
            await _service.GetNudgeAsync();
            _checkIn.Verify(c => c.LoadPlanAsync(null), Times.Exactly(2));
        }
    }
}
