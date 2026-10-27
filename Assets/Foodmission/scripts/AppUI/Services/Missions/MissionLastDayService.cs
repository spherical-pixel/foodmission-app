using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Missions that need data every day fail at their deadline and past days can't be added afterwards,
    /// so the user is reminded on the last day: a local notification at the preferred time and Nutri on Home.
    /// </summary>
    public class MissionLastDayService : IMissionLastDayService
    {
        public const string NotificationAction = "open_mission_checkin";
        private const string NotificationIdPrefix = "mission_last_day_";
        private const string DayFormat = "yyyy-MM-dd";

        private readonly IStoreService _storeService;
        private readonly IQuestService _questService;
        private readonly IMissionService _missionService;
        private readonly INotificationService _notificationService;
        private readonly ILocalStorageService _localStorage;

        public Func<DateTime> Clock { get; set; } = () => DateTime.Now;
        /// <summary>Localized UI text (key, format args); replaceable in tests.</summary>
        public Func<string, object[], string> Text { get; set; } = (key, args) =>
            LocalizationSettings.StringDatabase.GetLocalizedString("UI", key, args);

        public MissionLastDayService(
            IStoreService storeService,
            IQuestService questService,
            IMissionService missionService,
            INotificationService notificationService,
            ILocalStorageService localStorage)
        {
            _storeService = storeService;
            _questService = questService;
            _missionService = missionService;
            _notificationService = notificationService;
            _localStorage = localStorage;
        }

        public static string ScheduledIdsKey(string userId) => "mission_last_day_ids_" + userId;
        private static string ShownKey(string userId) => "mission_last_day_shown_" + userId;
        private static string SecuredKey(string userId) => "mission_secured_shown_" + userId;

        private static string AttemptOf(MissionDeadlines.ActiveMission mission) =>
            mission.Code + "|" + (mission.StartLocal?.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) ?? "-");

        private string UserId => _storeService?.GetAppState()?.userId;

        public bool ShownToday
        {
            get
            {
                string userId = UserId;
                return !string.IsNullOrEmpty(userId) &&
                       _localStorage?.GetValue<string>(ShownKey(userId)) == Clock().Date.ToString(DayFormat, CultureInfo.InvariantCulture);
            }
        }

        public void MarkShown()
        {
            string userId = UserId;
            if (!string.IsNullOrEmpty(userId))
            {
                _localStorage?.SetValue(ShownKey(userId), Clock().Date.ToString(DayFormat, CultureInfo.InvariantCulture));
            }
        }

        public async Task<MissionDeadlines.LastDay> GetTodayAsync()
        {
            try
            {
                if (ShownToday)
                {
                    return null;
                }
                var (missions, _) = await LoadActiveMissionsAsync();
                return MissionDeadlines.Today(missions, PreferredTime(), Clock());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionLastDayService] GetTodayAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task SyncRemindersAsync()
        {
            string userId = UserId;
            if (_notificationService == null || string.IsNullOrEmpty(userId))
            {
                return;
            }

            try
            {
                DateTime now = Clock();
                var (missions, _) = await LoadActiveMissionsAsync();
                var scheduled = new List<string>();
                foreach (MissionDeadlines.LastDay day in MissionDeadlines.LastDays(missions, PreferredTime()).Where(d => d.ReminderLocal > now))
                {
                    string id = NotificationIdPrefix + day.ReminderLocal.ToString(DayFormat, CultureInfo.InvariantCulture);
                    string body = day.Missions.Count == 1
                        ? Text("MISSION_LAST_DAY_NOTIFICATION_BODY_ONE", new object[] { day.Missions[0].Title })
                        : Text("MISSION_LAST_DAY_NOTIFICATION_BODY_MANY", new object[] { day.Missions.Count });
                    _notificationService.CancelNotification(id);
                    _notificationService.ScheduleLocalNotification(id, Text("MISSION_LAST_DAY_NOTIFICATION_TITLE", null), body,
                        day.ReminderLocal, null, NotificationAction, null);
                    scheduled.Add(id);
                }

                List<string> previous = _localStorage?.GetValue<List<string>>(ScheduledIdsKey(userId)) ?? new List<string>();
                foreach (string stale in previous.Except(scheduled))
                {
                    _notificationService.CancelNotification(stale);
                }
                _localStorage?.SetValue(ScheduledIdsKey(userId), scheduled);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionLastDayService] SyncRemindersAsync failed: {ex.Message}");
            }
        }

        public async Task<MissionDeadlines.ActiveMission> GetSecuredToAnnounceAsync()
        {
            string userId = UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            try
            {
                var (_, secured) = await LoadActiveMissionsAsync();
                List<string> announced = _localStorage?.GetValue<List<string>>(SecuredKey(userId)) ?? new List<string>();
                return secured
                    .Where(m => !announced.Contains(AttemptOf(m)))
                    .OrderBy(m => m.StartLocal ?? DateTime.MaxValue)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionLastDayService] GetSecuredToAnnounceAsync failed: {ex.Message}");
                return null;
            }
        }

        public void MarkSecuredAnnounced(MissionDeadlines.ActiveMission mission)
        {
            string userId = UserId;
            if (mission == null || string.IsNullOrEmpty(userId))
            {
                return;
            }
            List<string> announced = _localStorage?.GetValue<List<string>>(SecuredKey(userId)) ?? new List<string>();
            if (!announced.Contains(AttemptOf(mission)))
            {
                announced.Add(AttemptOf(mission));
                _localStorage?.SetValue(SecuredKey(userId), announced);
            }
        }

        private TimeSpan PreferredTime()
        {
            string value = _storeService?.GetAppState()?.notificationPreferredTime;
            return !string.IsNullOrEmpty(value) && TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan time)
                ? time
                : TimeSpan.FromHours(10);
        }

        /// <summary>
        /// Open missions of the current quest that have a rule (so a deadline) and a start time. Secured ones (nothing
        /// left to report, they only wait for their end) come apart: no last-day reminder for them.
        /// </summary>
        private async Task<(List<MissionDeadlines.ActiveMission> Active, List<MissionDeadlines.ActiveMission> Secured)> LoadActiveMissionsAsync()
        {
            var result = new List<MissionDeadlines.ActiveMission>();
            var secured = new List<MissionDeadlines.ActiveMission>();
            string questId = _storeService?.GetAppState()?.userCurrentQuestId;
            if (string.IsNullOrEmpty(questId) || _questService == null || _missionService == null)
            {
                return (result, secured);
            }

            var (quest, questError) = await _questService.GetQuestAsync(questId);
            if (questError != null || quest?.items == null)
            {
                return (result, secured);
            }
            var (progressList, progressError) = await _missionService.GetUserProgressListAsync();
            if (progressError != null)
            {
                return (result, secured);
            }

            IEnumerable<string> codes = quest.items
                .Where(i => i != null && string.Equals(i.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(i.contentCode))
                .Select(i => i.contentCode)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (string code in codes)
            {
                MissionInteraction interaction = MissionInteractionCatalog.Get(code);
                if (interaction.Status != MissionInteractionStatus.Available)
                {
                    continue;
                }
                MissionProgress progress = progressList?.FirstOrDefault(p => string.Equals(p?.missionCode, code, StringComparison.OrdinalIgnoreCase));
                if (progress?.startedAt == null || MissionProgressState.IsResolved(progress))
                {
                    continue;
                }
                string title = !string.IsNullOrEmpty(progress.missionTitle) ? progress.missionTitle : code;
                var mission = new MissionDeadlines.ActiveMission(code, title, progress.startedAt.Value.ToLocalTime());
                (MissionProgressState.IsSecured(progress, interaction) ? secured : result).Add(mission);
            }
            return (result, secured);
        }
    }
}
