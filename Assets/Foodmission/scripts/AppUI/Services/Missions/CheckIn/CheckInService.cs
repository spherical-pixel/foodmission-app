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

        private readonly IStoreService _storeService;
        private readonly IQuestService _questService;
        private readonly IMissionService _missionService;
        private readonly IMealLogService _mealLogService;
        private readonly IGamificationService _gamificationService;

        public Func<DateTime> Clock { get; set; } = () => DateTime.Now;

        public CheckInService(
            IStoreService storeService,
            IQuestService questService,
            IMissionService missionService,
            IMealLogService mealLogService,
            IGamificationService gamificationService)
        {
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
                var (missions, _, missionsError) = await LoadActiveMissionsAsync(_storeService, _questService, _missionService, onlyMissionCode);
                if (missionsError != null)
                {
                    return (null, missionsError);
                }

                DateTime now = Clock();
                var inputs = new CheckInInputs { NowLocal = now };
                inputs.Missions.AddRange(missions);

                if (inputs.Missions.Count == 0)
                {
                    return (CheckInPlan.Empty, null);
                }

                if (inputs.Missions.Any(m => HasStep(m, MissionStepType.MealReport)))
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
                        if (log == null || string.IsNullOrEmpty(log.typeOfMeal) || !TryParseLocalDate(log.timestamp, out DateTime day))
                        {
                            continue;
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
                if (daySteps.Count > 0)
                {
                    string[] eventTypes = daySteps.Select(s => s.EventType).Distinct(StringComparer.Ordinal).ToArray();
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
                active.Add(new CheckInMission(code, title, interaction, progress?.startedAt?.ToLocalTime()));
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

        private static bool TryParseLocalDate(string timestamp, out DateTime day)
        {
            day = default;
            if (string.IsNullOrEmpty(timestamp) ||
                !DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime utc))
            {
                return false;
            }
            day = utc.ToLocalTime().Date;
            return true;
        }
    }
}
