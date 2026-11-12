using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DailyMealReminderServiceTests
    {
        // Thursday 2026-11-12 at 08:00 local, reminders at 10:00
        private static readonly DateTime Now = new DateTime(2026, 11, 12, 8, 0, 0, DateTimeKind.Local);

        private TestStoreService _store;
        private TestLocalStorageService _storage;
        private Mock<INotificationService> _notifications;
        private Mock<IMealLogService> _mealLogs;
        private DailyMealReminderService _service;
        private MealLog[] _todayLogs;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", notificationPreferredTime = "10:00" });
            _storage = new TestLocalStorageService();
            _notifications = new Mock<INotificationService>();
            _notifications.Setup(n => n.AreNotificationsEnabled()).Returns(true);
            _mealLogs = new Mock<IMealLogService>();
            _todayLogs = Array.Empty<MealLog>();
            _mealLogs.Setup(m => m.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(() => (new PaginatedMealLogResponse { data = _todayLogs }, (ApiErrorResponse)null));

            _service = new DailyMealReminderService(_store, _notifications.Object, _mealLogs.Object, _storage)
            {
                Clock = () => Now,
                Text = key => key
            };
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            _store.Dispose();
        }

        private void VerifyScheduled(string id, Times times)
        {
            _notifications.Verify(n => n.ScheduleLocalNotification(id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
                NotificationChannels.DailyRemindersId, DailyMealReminderService.NotificationAction, null), times);
        }

        [Test]
        public async Task SyncAsync_SchedulesSevenDaysAndCancelsTheLegacyReminder()
        {
            await _service.SyncAsync();

            VerifyScheduled("daily_meal_reminder_2026-11-12", Times.Once());
            VerifyScheduled("daily_meal_reminder_2026-11-18", Times.Once());
            _notifications.Verify(n => n.ScheduleLocalNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(7));
            _notifications.Verify(n => n.CancelNotification(DailyMealReminderPlan.LegacyId), Times.Once);
            Assert.AreEqual(7, _storage.GetValue<List<string>>(DailyMealReminderService.ScheduledIdsKey("u1")).Count);
        }

        [Test]
        public async Task SyncAsync_MealLoggedToday_SkipsToday()
        {
            _todayLogs = new[] { new MealLog { id = "l1", status = MealLogStatus.Confirmed } };

            await _service.SyncAsync();

            VerifyScheduled("daily_meal_reminder_2026-11-12", Times.Never());
            VerifyScheduled("daily_meal_reminder_2026-11-13", Times.Once());
        }

        [Test]
        public async Task SyncAsync_OnlyADraftToday_StillRemindsToday()
        {
            _todayLogs = new[] { new MealLog { id = "d1", status = MealLogStatus.Draft } };

            await _service.SyncAsync();

            VerifyScheduled("daily_meal_reminder_2026-11-12", Times.Once());
        }

        [Test]
        public async Task SyncAsync_DayWithAMissionLastDayReminder_IsSkipped()
        {
            _storage.SetValue(MissionLastDayService.ScheduledIdsKey("u1"), new List<string> { "mission_last_day_2026-11-14" });

            await _service.SyncAsync();

            VerifyScheduled("daily_meal_reminder_2026-11-14", Times.Never());
            VerifyScheduled("daily_meal_reminder_2026-11-15", Times.Once());
        }

        [Test]
        public async Task SyncAsync_NotificationsDisabled_CancelsWhatWasScheduled()
        {
            await _service.SyncAsync();
            _notifications.Setup(n => n.AreNotificationsEnabled()).Returns(false);

            await _service.SyncAsync();

            _notifications.Verify(n => n.CancelNotification("daily_meal_reminder_2026-11-18"), Times.AtLeastOnce);
            Assert.AreEqual(0, _storage.GetValue<List<string>>(DailyMealReminderService.ScheduledIdsKey("u1")).Count);
        }

        [Test]
        public async Task SyncAsync_LogsCantBeRead_StillRemindsToday()
        {
            _mealLogs.Setup(m => m.GetLogsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(((PaginatedMealLogResponse)null, new ApiErrorResponse { statusCode = 500 }));

            await _service.SyncAsync();

            VerifyScheduled("daily_meal_reminder_2026-11-12", Times.Once());
        }

        [Test]
        public async Task MealLogSaved_ForToday_CancelsTodaysReminder()
        {
            await _service.SyncAsync();

            _mealLogs.Raise(m => m.MealLogSaved += null, new MealLog { id = "l1", timestamp = Now.AddMinutes(-30).ToUniversalTime().ToString("o") });

            _notifications.Verify(n => n.CancelNotification("daily_meal_reminder_2026-11-12"), Times.AtLeast(2));
            CollectionAssert.DoesNotContain(_storage.GetValue<List<string>>(DailyMealReminderService.ScheduledIdsKey("u1")), "daily_meal_reminder_2026-11-12");
        }

        [Test]
        public async Task MealLogSaved_ForAnotherDay_KeepsTodaysReminder()
        {
            await _service.SyncAsync();

            _mealLogs.Raise(m => m.MealLogSaved += null, new MealLog { id = "l1", timestamp = Now.AddDays(-2).ToUniversalTime().ToString("o") });

            CollectionAssert.Contains(_storage.GetValue<List<string>>(DailyMealReminderService.ScheduledIdsKey("u1")), "daily_meal_reminder_2026-11-12");
        }

        [Test]
        public void RemindersChanged_Resyncs()
        {
            _notifications.Raise(n => n.RemindersChanged += null);

            VerifyScheduled("daily_meal_reminder_2026-11-12", Times.Once());
        }
    }
}
