using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionLastDayServiceTests
    {
        // Missions started Monday 2026-10-05 at 15:00 local: they end Monday 2026-10-12 at 15:00
        private static readonly DateTime Start = new DateTime(2026, 10, 5, 15, 0, 0, DateTimeKind.Local);
        private static readonly DateTime LastDay = Start.Date.AddDays(7);

        private TestStoreService _store;
        private TestLocalStorageService _storage;
        private Mock<IQuestService> _quests;
        private Mock<IMissionService> _missions;
        private Mock<INotificationService> _notifications;
        private MissionLastDayService _service;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "q1", notificationPreferredTime = "10:00" });
            _storage = new TestLocalStorageService();
            _quests = new Mock<IQuestService>();
            _missions = new Mock<IMissionService>();
            _notifications = new Mock<INotificationService>();

            var items = new[] { "M.A1.1", "M.B1.3", "M.A2.1", "M.A1.3" }
                .Select(c => new QuestItem { contentType = QuestContentType.Mission, contentCode = c }).ToArray();
            _quests.Setup(q => q.GetQuestAsync("q1", null)).ReturnsAsync((new Quest { id = "q1", items = items }, (ApiErrorResponse)null));
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((new[]
            {
                new MissionProgress { missionCode = "M.A1.1", missionTitle = "Green zone", startedAt = Start.ToUniversalTime() },
                new MissionProgress { missionCode = "M.B1.3", missionTitle = "One fewer", startedAt = Start.ToUniversalTime() },
                // No rule: nothing ends
                new MissionProgress { missionCode = "M.A2.1", missionTitle = "Pending rule", startedAt = Start.ToUniversalTime() },
                new MissionProgress { missionCode = "M.A1.3", missionTitle = "Done", completed = true, startedAt = Start.ToUniversalTime() }
            }, (ApiErrorResponse)null));

            _now = LastDay.AddHours(9);
            _service = new MissionLastDayService(_store, _quests.Object, _missions.Object, _notifications.Object, _storage)
            {
                Clock = () => _now,
                Text = (key, args) => args == null || args.Length == 0 ? key : key + ":" + string.Join(",", args)
            };
        }

        [Test]
        public async Task GetTodayAsync_OnTheLastDay_ReturnsTheOpenMissionsWithARule()
        {
            MissionDeadlines.LastDay today = await _service.GetTodayAsync();

            CollectionAssert.AreEqual(new[] { "M.A1.1", "M.B1.3" }, today.Missions.Select(m => m.Code).ToArray());
        }

        [Test]
        public async Task GetTodayAsync_OncePerDay()
        {
            _service.MarkShown();

            Assert.IsNull(await _service.GetTodayAsync());
            Assert.IsTrue(_service.ShownToday);

            _now = _now.AddDays(-1);
            Assert.IsFalse(_service.ShownToday, "another day");
        }

        [Test]
        public async Task GetTodayAsync_BeforeTheLastDay_ReturnsNull()
        {
            _now = LastDay.AddDays(-2).AddHours(12);

            Assert.IsNull(await _service.GetTodayAsync());
        }

        [Test]
        public async Task SyncRemindersAsync_SchedulesOneReminderPerLastDay_AtThePreferredTime()
        {
            _now = Start.AddDays(1);

            await _service.SyncRemindersAsync();

            _notifications.Verify(n => n.ScheduleLocalNotification(
                "mission_last_day_2026-10-12",
                "MISSION_LAST_DAY_NOTIFICATION_TITLE",
                "MISSION_LAST_DAY_NOTIFICATION_BODY_MANY:2",
                LastDay.AddHours(10),
                null,
                "open_mission_checkin",
                null), Times.Once);
        }

        [Test]
        public async Task SyncRemindersAsync_CancelsRemindersThatNoLongerApply()
        {
            _storage.SetValue(MissionLastDayService.ScheduledIdsKey("u1"), new List<string> { "mission_last_day_2026-10-01" });
            _now = Start.AddDays(1);

            await _service.SyncRemindersAsync();

            _notifications.Verify(n => n.CancelNotification("mission_last_day_2026-10-01"), Times.Once);
            CollectionAssert.AreEqual(new[] { "mission_last_day_2026-10-12" }, _storage.GetValue<List<string>>(MissionLastDayService.ScheduledIdsKey("u1")));
        }

        [Test]
        public async Task SyncRemindersAsync_SkipsRemindersAlreadyPast()
        {
            _now = LastDay.AddHours(11);

            await _service.SyncRemindersAsync();

            _notifications.Verify(n => n.ScheduleLocalNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
