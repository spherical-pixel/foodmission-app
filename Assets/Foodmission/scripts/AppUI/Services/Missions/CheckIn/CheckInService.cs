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
                string questId = _storeService?.GetAppState()?.userCurrentQuestId;
                if (string.IsNullOrEmpty(questId))
                {
                    return (CheckInPlan.Empty, null);
                }

                var (quest, questError) = await _questService.GetQuestAsync(questId);
                if (questError != null)
                {
                    return (null, questError);
                }

                List<string> codes = (quest?.items ?? Array.Empty<QuestItem>())
                    .Where(i => i != null && string.Equals(i.contentType, QuestContentType.Mission, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(i.contentCode))
                    .Select(i => i.contentCode)
                    .Where(c => onlyMissionCode == null || string.Equals(c, onlyMissionCode, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (codes.Count == 0)
                {
                    return (CheckInPlan.Empty, null);
                }

                var (progressList, progressError) = await _missionService.GetUserProgressListAsync();
                if (progressError != null)
                {
                    return (null, progressError);
                }

                DateTime now = Clock();
                var inputs = new CheckInInputs { NowLocal = now };
                foreach (string code in codes)
                {
                    MissionInteraction interaction = MissionInteractionCatalog.Get(code);
                    MissionProgress progress = progressList?.FirstOrDefault(p => string.Equals(p?.missionCode, code, StringComparison.OrdinalIgnoreCase));
                    if (!interaction.CanReport || progress?.completed == true || (progress?.progress ?? 0f) >= 100f)
                    {
                        continue;
                    }
                    string title = !string.IsNullOrEmpty(progress?.missionTitle) ? progress.missionTitle : code;
                    inputs.Missions.Add(new CheckInMission(code, title, interaction, progress?.startedAt?.ToLocalTime()));
                }

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

                if (inputs.Missions.Any(m => HasStep(m, MissionStepType.DayPicker)))
                {
                    var (profile, profileError) = await _gamificationService.GetGamificationProfileAsync(EventsLimit, 1);
                    if (profileError != null)
                    {
                        return (null, profileError);
                    }
                    foreach (UserEvent userEvent in profile?.recentEvents ?? Array.Empty<UserEvent>())
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
