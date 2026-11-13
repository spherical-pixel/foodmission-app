using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>Daily "log your meals" reminder at the preferred time, skipped on days with a meal logged.</summary>
    public interface IDailyMealReminderService
    {
        /// <summary>Schedules the next days' reminders and cancels the ones that no longer apply. Never throws.</summary>
        Task SyncAsync();
    }

    /// <summary>
    /// Keeps <see cref="DailyMealReminderPlan.WindowDays"/> days of reminders scheduled. Synced on Home entry, on app resume,
    /// when notifications are enabled or the preferred time changes; today's reminder is cancelled as soon as a meal of today is saved.
    /// </summary>
    public class DailyMealReminderService : IDailyMealReminderService, IDisposable
    {
        public const string NotificationAction = "go_to_meal_log";

        private readonly IStoreService _storeService;
        private readonly INotificationService _notificationService;
        private readonly IMealLogService _mealLogService;
        private readonly ILocalStorageService _localStorage;

        private bool _syncing;
        private bool _syncAgain;

        public Func<DateTime> Clock { get; set; } = () => DateTime.Now;
        /// <summary>Localized UI text; replaceable in tests.</summary>
        public Func<string, string> Text { get; set; } = key => LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);

        public DailyMealReminderService(
            IStoreService storeService,
            INotificationService notificationService,
            IMealLogService mealLogService,
            ILocalStorageService localStorage)
        {
            _storeService = storeService;
            _notificationService = notificationService;
            _mealLogService = mealLogService;
            _localStorage = localStorage;

            if (_notificationService != null)
            {
                _notificationService.RemindersChanged += OnRemindersChanged;
            }
            if (_mealLogService != null)
            {
                _mealLogService.MealLogSaved += OnMealLogSaved;
            }
        }

        public static string ScheduledIdsKey(string userId) => "daily_meal_reminder_ids_" + userId;

        private string UserId => _storeService?.GetAppState()?.userId;

        public async Task SyncAsync()
        {
            // One sync at a time: enabling notifications and changing the time can both ask for one
            if (_syncing)
            {
                _syncAgain = true;
                return;
            }

            _syncing = true;
            try
            {
                do
                {
                    _syncAgain = false;
                    await SyncOnceAsync();
                }
                while (_syncAgain);
            }
            finally
            {
                _syncing = false;
            }
        }

        private async Task SyncOnceAsync()
        {
            string userId = UserId;
            if (_notificationService == null || string.IsNullOrEmpty(userId))
            {
                return;
            }

            try
            {
                List<string> previous = _localStorage?.GetValue<List<string>>(ScheduledIdsKey(userId)) ?? new List<string>();
                _notificationService.CancelNotification(DailyMealReminderPlan.LegacyId);

                var scheduled = new List<string>();
                if (_notificationService.AreNotificationsEnabled())
                {
                    DateTime now = Clock();
                    bool loggedToday = await HasMealLoggedTodayAsync(now);
                    HashSet<DateTime> missionDays = MissionLastDayService.ReminderDays(_localStorage, userId);
                    string title = Text("NOTIFICATION_DAILY_MEAL_REMINDER_TITLE");
                    string body = Text("NOTIFICATION_DAILY_MEAL_REMINDER_BODY");

                    foreach (DateTime at in DailyMealReminderPlan.Times(now, PreferredTime(), loggedToday, missionDays))
                    {
                        string id = DailyMealReminderPlan.IdFor(at);
                        _notificationService.CancelNotification(id);
                        _notificationService.ScheduleLocalNotification(id, title, body, at, NotificationChannels.DailyRemindersId, NotificationAction, null);
                        scheduled.Add(id);
                    }
                }

                foreach (string stale in previous.Except(scheduled))
                {
                    _notificationService.CancelNotification(stale);
                }
                _localStorage?.SetValue(ScheduledIdsKey(userId), scheduled);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DailyMealReminderService] SyncAsync failed: {ex.Message}");
            }
        }

        /// <summary>Any non-draft meal log from today's midnight on. False when the logs can't be read (better one reminder too many).</summary>
        private async Task<bool> HasMealLoggedTodayAsync(DateTime nowLocal)
        {
            if (_mealLogService == null)
            {
                return false;
            }
            string from = nowLocal.Date.ToUniversalTime().ToString("o");
            string to = nowLocal.ToUniversalTime().ToString("o");
            var (logs, error) = await _mealLogService.GetLogsAsync(1, 10, null, from, to);
            if (error != null || logs?.data == null)
            {
                return false;
            }
            return logs.data.Any(log => log != null && log.status != MealLogStatus.Draft);
        }

        private void OnMealLogSaved(MealLog log)
        {
            string userId = UserId;
            if (log == null || string.IsNullOrEmpty(userId) || !TryParseLocal(log.timestamp, out DateTime atLocal))
            {
                return;
            }

            DateTime today = Clock().Date;
            if (atLocal.Date != today)
            {
                return;
            }

            string id = DailyMealReminderPlan.IdFor(today);
            _notificationService?.CancelNotification(id);
            List<string> scheduled = _localStorage?.GetValue<List<string>>(ScheduledIdsKey(userId));
            if (scheduled != null && scheduled.Remove(id))
            {
                _localStorage.SetValue(ScheduledIdsKey(userId), scheduled);
            }
        }

        private void OnRemindersChanged()
        {
            _ = SyncAsync();
        }

        private TimeSpan PreferredTime()
        {
            string value = _storeService?.GetAppState()?.notificationPreferredTime;
            return !string.IsNullOrEmpty(value) && TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan time)
                ? time
                : TimeSpan.FromHours(10);
        }

        private static bool TryParseLocal(string timestamp, out DateTime local)
        {
            local = default;
            if (string.IsNullOrEmpty(timestamp) ||
                !DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                return false;
            }
            local = parsed.Kind == DateTimeKind.Unspecified ? parsed : parsed.ToLocalTime();
            return true;
        }

        public void Dispose()
        {
            if (_notificationService != null)
            {
                _notificationService.RemindersChanged -= OnRemindersChanged;
            }
            if (_mealLogService != null)
            {
                _mealLogService.MealLogSaved -= OnMealLogSaved;
            }
        }
    }
}
