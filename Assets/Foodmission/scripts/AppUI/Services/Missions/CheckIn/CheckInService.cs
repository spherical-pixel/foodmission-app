using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>Loads quest, progress, meal logs and recent events, then delegates to CheckInPlanner (spec §3.9).</summary>
    public class CheckInService : ICheckInService
    {
        public const int EventsLimit = 100;
        public const int MealLogsLimit = 100;
        private const string DecidedDaysKeyPrefix = "checkin_day_answers_";
        /// <summary>Older answers can't be in any 7-day window any more.</summary>
        private const int DecidedDaysKept = 14;

        private readonly IStoreService _storeService;
        private readonly IQuestService _questService;
        private readonly IMissionService _missionService;
        private readonly IMealLogService _mealLogService;
        private readonly IGamificationService _gamificationService;
        private readonly ILocalStorageService _storage;

        public Func<DateTime> Clock { get; set; } = () => DateTime.Now;

        public CheckInService(
            IStoreService storeService,
            IQuestService questService,
            IMissionService missionService,
            IMealLogService mealLogService,
            IGamificationService gamificationService,
            ILocalStorageService storage)
        {
            _storage = storage;
            _storeService = storeService;
            _questService = questService;
            _missionService = missionService;
            _mealLogService = mealLogService;
            _gamificationService = gamificationService;
        }

        public async Task<(CheckInPlan Plan, ApiErrorResponse Error)> LoadPlanAsync(string onlyMissionCode = null)
        {
            try
            {
                // Every active mission of the quest: the others still decide whether swaps go per meal
                var (missions, _, missionsError) = await LoadActiveMissionsAsync(_storeService, _questService, _missionService);
                if (missionsError != null)
                {
                    return (null, missionsError);
                }

                DateTime now = Clock();
                var inputs = new CheckInInputs { NowLocal = now };
                foreach (CheckInMission mission in missions)
                {
                    bool covered = onlyMissionCode == null || string.Equals(mission.Code, onlyMissionCode, StringComparison.OrdinalIgnoreCase);
                    (covered ? inputs.Missions : inputs.QuestMissions).Add(mission);
                }
                // Only answers of each mission's current attempt
                var attempts = new HashSet<string>(inputs.Missions.Select(m => Attempt(m.Code, m.StartLocal)), StringComparer.OrdinalIgnoreCase);
                foreach (var (code, start, day) in ReadDecidedDays())
                {
                    if (!attempts.Contains(Attempt(code, start)))
                    {
                        continue;
                    }
                    if (!inputs.DecidedDays.TryGetValue(code, out HashSet<DateTime> days))
                    {
                        days = new HashSet<DateTime>();
                        inputs.DecidedDays[code] = days;
                    }
                    days.Add(day);
                }

                if (inputs.Missions.Count == 0)
                {
                    return (CheckInPlan.Empty, null);
                }

                // Day questions need the logged days too (a logged day is already decided); day swaps, the swaps already logged
                if (inputs.Missions.Any(m => HasStep(m, MissionStepType.MealReport) || HasStep(m, MissionStepType.DayQuestion) ||
                    HasStep(m, MissionStepType.DaySwaps)))
                {
                    string from = now.Date.AddDays(-(MissionReportDays.WindowDays - 1)).ToUniversalTime().ToString("o");
                    string to = now.ToUniversalTime().ToString("o");
                    var (logs, logsError) = await _mealLogService.GetLogsAsync(1, MealLogsLimit, null, from, to);
                    if (logsError != null)
                    {
                        return (null, logsError);
                    }
                    foreach (MealLog log in logs?.data ?? Array.Empty<MealLog>())
                    {
                        if (log == null || string.IsNullOrEmpty(log.typeOfMeal) || !TryParseLocal(log.timestamp, out DateTime atLocal))
                        {
                            continue;
                        }
                        DateTime day = atLocal.Date;
                        foreach (string swap in log.swaps ?? Array.Empty<string>())
                        {
                            if (!string.IsNullOrEmpty(swap))
                            {
                                inputs.LoggedSwaps.Add((atLocal, swap));
                            }
                        }
                        if (!inputs.LoggedMealTypes.TryGetValue(day, out HashSet<string> types))
                        {
                            types = new HashSet<string>();
                            inputs.LoggedMealTypes[day] = types;
                        }
                        types.Add(log.typeOfMeal);
                    }
                }

                List<MissionReportStep> daySteps = inputs.Missions
                    .SelectMany(m => m.Interaction.Steps)
                    .Where(s => (s.Type == MissionStepType.DayPicker || s.Type == MissionStepType.DayChecklist) && !string.IsNullOrEmpty(s.EventType))
                    .ToList();
                // Day questions decided by a flag: the days that flag was already recorded
                string[] dayQuestionTypes = inputs.Missions
                    .SelectMany(m => m.Interaction.Steps)
                    .Where(s => s.Type == MissionStepType.DayQuestion && s.DayQuestion?.Coverage == DayCoverage.FlagDay)
                    .SelectMany(s => s.DayQuestion.CountingOptions.Select(i => s.Options[i].EventType))
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToArray();
                // Product steps: foods already reported from the product detail (real ids) are counted in the check-in
                string[] productTypes = inputs.Missions
                    .SelectMany(m => m.Interaction.Steps)
                    .SelectMany(ProductMissionPlanner.EventTypesOf)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                if (productTypes.Length > 0)
                {
                    // Own call, failures ignored: it only feeds a hint, and its (many) events must not push day events out of the limit
                    var (productEvents, productError) = await LoadRecentEventsAsync(_gamificationService, productTypes, EventsLimit);
                    if (productError != null)
                    {
                        Debug.LogWarning($"[CheckInService] Product reports not loaded: {productError.message}");
                    }
                    else
                    {
                        RecordProductReports(inputs, productEvents, productTypes);
                    }
                }
                if (daySteps.Count > 0 || dayQuestionTypes.Length > 0)
                {
                    string[] eventTypes = daySteps.Select(s => s.EventType).Concat(dayQuestionTypes).Distinct(StringComparer.Ordinal).ToArray();
                    var (events, eventsError) = await LoadRecentEventsAsync(_gamificationService, eventTypes, EventsLimit);
                    if (eventsError != null)
                    {
                        return (null, eventsError);
                    }
                    var arrayFields = daySteps.Where(s => s.Type == MissionStepType.DayChecklist)
                        .GroupBy(s => s.EventType)
                        .ToDictionary(g => g.Key, g => g.First().Checklist.ArrayField, StringComparer.Ordinal);
                    foreach (UserEvent userEvent in events ?? Array.Empty<UserEvent>())
                    {
                        DateTime? day = ReadDayBucket(userEvent?.metadata);
                        if (userEvent == null || string.IsNullOrEmpty(userEvent.eventType) || !day.HasValue)
                        {
                            continue;
                        }
                        if (!inputs.CoveredEventDays.TryGetValue(userEvent.eventType, out HashSet<DateTime> days))
                        {
                            days = new HashSet<DateTime>();
                            inputs.CoveredEventDays[userEvent.eventType] = days;
                        }
                        days.Add(day.Value);

                        if (arrayFields.TryGetValue(userEvent.eventType, out string arrayField) && userEvent.metadata[arrayField] is JArray values)
                        {
                            if (!inputs.RecordedValues.TryGetValue(userEvent.eventType, out Dictionary<DateTime, HashSet<string>> byDay))
                            {
                                byDay = new Dictionary<DateTime, HashSet<string>>();
                                inputs.RecordedValues[userEvent.eventType] = byDay;
                            }
                            if (!byDay.TryGetValue(day.Value, out HashSet<string> recorded))
                            {
                                recorded = new HashSet<string>(StringComparer.Ordinal);
                                byDay[day.Value] = recorded;
                            }
                            recorded.UnionWith(values.Where(t => t.Type == JTokenType.String).Select(t => ((string)t).ToUpperInvariant()));
                        }
                    }
                }

                return (CheckInPlanner.Plan(inputs), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CheckInService] LoadPlanAsync failed: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public void RememberDayAnswers(IEnumerable<(string MissionCode, DateTime? StartLocal, DateTime Day)> answered)
        {
            string key = DecidedDaysKey();
            if (_storage == null || key == null || answered == null)
            {
                return;
            }

            try
            {
                DateTime oldest = Clock().Date.AddDays(-DecidedDaysKept);
                var entries = new HashSet<string>(ReadDecidedDays().Where(d => d.Day >= oldest).Select(d => Entry(d.MissionCode, d.Start, d.Day)), StringComparer.OrdinalIgnoreCase);
                entries.UnionWith(answered.Where(a => !string.IsNullOrEmpty(a.MissionCode)).Select(a => Entry(a.MissionCode, a.StartLocal, a.Day)));
                _storage.SetValue(key, string.Join(";", entries));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CheckInService] RememberDayAnswers failed: {ex.Message}");
            }
        }

        // Per user: several accounts can share a device
        private string DecidedDaysKey()
        {
            string userId = _storeService?.GetAppState()?.userId;
            return string.IsNullOrEmpty(userId) ? null : DecidedDaysKeyPrefix + userId;
        }

        // Entry: CODE|attempt start (UTC, "-" when unknown)|day
        private static string StartKey(DateTime? startLocal) =>
            startLocal.HasValue ? startLocal.Value.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) : "-";

        private static string Attempt(string missionCode, DateTime? startLocal) => missionCode + "|" + StartKey(startLocal);

        private static string Attempt(string missionCode, string startKey) => missionCode + "|" + startKey;

        private static string Entry(string missionCode, string startKey, DateTime day) =>
            Attempt(missionCode, startKey) + "|" + day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static string Entry(string missionCode, DateTime? startLocal, DateTime day) => Entry(missionCode, StartKey(startLocal), day);

        private List<(string MissionCode, string Start, DateTime Day)> ReadDecidedDays()
        {
            var result = new List<(string, string, DateTime)>();
            string key = DecidedDaysKey();
            string stored = key == null ? null : _storage?.GetValue<string>(key, null);
            if (string.IsNullOrEmpty(stored))
            {
                return result;
            }
            foreach (string entry in stored.Split(';'))
            {
                string[] parts = entry.Split('|');
                if (parts.Length == 3 && DateTime.TryParseExact(parts[2], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime day))
                {
                    result.Add((parts[0], parts[1], day));
                }
            }
            return result;
        }

        /// <summary>
        /// Missions of the active quest the user can still report: known to the catalog and not resolved. Shared with the
        /// product detail (ProductMissionService) so both ask about the same missions.
        /// </summary>
        public static async Task<(List<CheckInMission> Missions, Dictionary<string, MissionProgress> Progress, ApiErrorResponse Error)>
            LoadActiveMissionsAsync(IStoreService store, IQuestService quests, IMissionService missions, string onlyMissionCode = null)
        {
            var active = new List<CheckInMission>();
            var progressByCode = new Dictionary<string, MissionProgress>(StringComparer.OrdinalIgnoreCase);
            string questId = store?.GetAppState()?.userCurrentQuestId;
            if (string.IsNullOrEmpty(questId))
            {
                return (active, progressByCode, null);
            }

            var (quest, questError) = await quests.GetQuestAsync(questId);
            if (questError != null)
            {
                return (null, null, questError);
            }

            List<string> codes = (quest?.items ?? Array.Empty<QuestItem>())
                .Where(i => i != null && string.Equals(i.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(i.contentCode))
                .Select(i => i.contentCode)
                .Where(c => onlyMissionCode == null || string.Equals(c, onlyMissionCode, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (codes.Count == 0)
            {
                return (active, progressByCode, null);
            }

            var (progressList, progressError) = await missions.GetUserProgressListAsync();
            if (progressError != null)
            {
                return (null, null, progressError);
            }

            foreach (string code in codes)
            {
                MissionInteraction interaction = MissionInteractionCatalog.Get(code);
                MissionProgress progress = progressList?.FirstOrDefault(p => string.Equals(p?.missionCode, code, StringComparison.OrdinalIgnoreCase));
                // Since backend #424 the user starts each mission; rules ignore missions that are not started
                if (!interaction.CanReport || !MissionProgressState.IsStarted(progress) || MissionProgressState.IsResolved(progress))
                {
                    continue;
                }
                string title = !string.IsNullOrEmpty(progress?.missionTitle) ? progress.missionTitle : code;
                // Backend progress is a percentage
                active.Add(new CheckInMission(code, title, interaction, progress?.startedAt?.ToLocalTime(), (progress?.progress ?? 0f) / 100f));
                if (progress != null)
                {
                    progressByCode[code] = progress;
                }
            }
            return (active, progressByCode, null);
        }

        /// <summary>
        /// Recent events of these types. Backends without the eventTypes filter (#419) answer 400 to the unknown query
        /// parameter: then the latest events are fetched unfiltered and filtered here.
        /// </summary>
        public static async Task<(UserEvent[] Events, ApiErrorResponse Error)> LoadRecentEventsAsync(
            IGamificationService gamification, string[] eventTypes, int limit)
        {
            var (events, error) = await gamification.GetRecentEventsAsync(eventTypes, limit);
            if (error == null || error.statusCode != 400)
            {
                return (events, error);
            }

            Debug.LogWarning("[CheckInService] eventTypes filter not supported by the backend, loading unfiltered recent events");
            var (profile, profileError) = await gamification.GetGamificationProfileAsync(limit, 1);
            if (profileError != null)
            {
                return (null, profileError);
            }
            var wanted = new HashSet<string>(eventTypes, StringComparer.Ordinal);
            return ((profile?.recentEvents ?? Array.Empty<UserEvent>()).Where(e => e != null && wanted.Contains(e.eventType)).ToArray(), null);
        }

        /// <summary>Server dayBucket (UTC date "yyyy-MM-dd"); Newtonsoft may hand it over as a Date token.</summary>
        public static DateTime? ReadDayBucket(JObject metadata)
        {
            JToken token = metadata?["dayBucket"];
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }
            if (token.Type == JTokenType.Date)
            {
                return ((DateTime)token).Date;
            }
            return DateTime.TryParseExact(token.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime day)
                ? day
                : (DateTime?)null;
        }

        private static void RecordProductReports(CheckInInputs inputs, UserEvent[] events, string[] productTypes)
        {
            var wanted = new HashSet<string>(productTypes, StringComparer.Ordinal);
            foreach (UserEvent userEvent in events ?? Array.Empty<UserEvent>())
            {
                if (userEvent?.metadata == null || !wanted.Contains(userEvent.eventType) || !TryParseLocalTime(userEvent.timestamp, out DateTime at))
                {
                    continue;
                }
                string id = ReadString(userEvent.metadata, ProductMissionPlanner.ProductIdField) ?? ReadString(userEvent.metadata, ProductMissionPlanner.GenericFoodIdField);
                // Check-in products are anonymous (manual-…): only real foods come from the product detail
                if (string.IsNullOrEmpty(id) || id.StartsWith("manual-", StringComparison.Ordinal))
                {
                    continue;
                }
                var values = new List<string>();
                var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (JProperty property in userEvent.metadata.Properties())
                {
                    if (property.Value is JArray array)
                    {
                        values.AddRange(array.Where(t => t.Type == JTokenType.String).Select(t => ((string)t).ToUpperInvariant()));
                    }
                    else if (property.Value.Type == JTokenType.String)
                    {
                        metadata[property.Name] = (string)property.Value;
                    }
                }
                inputs.ProductReports.Add(new CheckInProductReport(userEvent.eventType, id, at, values, metadata));
            }
        }

        private static string ReadString(JObject metadata, string field) =>
            metadata[field]?.Type == JTokenType.String ? (string)metadata[field] : null;

        private static bool TryParseLocalTime(string timestamp, out DateTime local)
        {
            local = default;
            if (string.IsNullOrEmpty(timestamp) ||
                !DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime utc))
            {
                return false;
            }
            local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
            return true;
        }

        private static bool HasStep(CheckInMission mission, MissionStepType type) =>
            mission.Interaction.Steps.Any(s => s.Type == type);

        private static bool TryParseLocal(string timestamp, out DateTime local)
        {
            local = default;
            if (string.IsNullOrEmpty(timestamp) ||
                !DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime utc))
            {
                return false;
            }
            local = utc.ToLocalTime();
            return true;
        }
    }
}
