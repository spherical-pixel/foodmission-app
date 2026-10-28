using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>Decides what the check-in asks (spec §3.9). Pure.</summary>
    public static class CheckInPlanner
    {
        private sealed class MealContribution
        {
            public string Code;
            public string Title;
            public string EventType;
            public IReadOnlyList<string> MealTypes;
            public int Order;
            public DateTime? Start;
            public string HintKey;
        }

        public static CheckInPlan Plan(CheckInInputs inputs)
        {
            var mealByDay = new SortedDictionary<DateTime, List<MealContribution>>();
            var dayEvents = new List<(string Code, string Title, MissionReportStep Step, SortedSet<DateTime> Days, DateTime? Start)>();
            var dayQuestions = new List<(CheckInMission Mission, MissionReportStep Step, IReadOnlyList<DateTime> Window, int? Remaining)>();
            var missionSteps = new List<CheckInMissionStep>();
            var swapMissions = new List<(CheckInSwapMission Mission, IReadOnlyList<DateTime> Window)>();
            var asked = new List<CheckInMissionRef>();
            var secured = new List<CheckInMissionRef>();
            int order = 0;

            var askable = new List<CheckInMission>();
            foreach (CheckInMission mission in inputs.Missions)
            {
                if (!IsAskable(mission, inputs.NowLocal))
                {
                    continue;
                }

                if (MissionProgressState.IsSecured(mission.Interaction, mission.Progress * 100f))
                {
                    secured.Add(new CheckInMissionRef(mission.Code, mission.Title, mission.StartLocal));
                    continue;
                }
                askable.Add(mission);
            }
            // Swaps join the meal block when another mission of the quest asks per meal anyway, even one this check-in
            // doesn't cover (spec 2026-10-27 swaps §4)
            bool mealsAsked = askable
                .Concat(inputs.QuestMissions.Where(m => IsAskable(m, inputs.NowLocal) && !MissionProgressState.IsSecured(m.Interaction, m.Progress * 100f)))
                .Any(m => m.Interaction.Steps.Any(s => s.Type == MissionStepType.MealReport));

            foreach (CheckInMission mission in askable)
            {
                IReadOnlyList<DateTime> window = MissionReportDays.Allowed(mission.StartLocal, inputs.NowLocal);
                foreach (MissionReportStep catalogStep in mission.Interaction.Steps)
                {
                    MissionReportStep step = catalogStep.Type == MissionStepType.DaySwaps && mealsAsked ? MissionSwaps.AsMealReport(catalogStep) : catalogStep;
                    switch (step.Type)
                    {
                        case MissionStepType.MealReport:
                            IEnumerable<string> events = step.EventType != null
                                ? new[] { step.EventType }
                                : step.Options.Select(o => o.EventType);
                            foreach (string eventType in events)
                            {
                                int eventOrder = order++;
                                foreach (DateTime day in window)
                                {
                                    if (!mealByDay.TryGetValue(day, out List<MealContribution> list))
                                    {
                                        list = new List<MealContribution>();
                                        mealByDay[day] = list;
                                    }
                                    list.Add(new MealContribution
                                    {
                                        Code = mission.Code, Title = mission.Title, EventType = eventType, MealTypes = step.MealTypes, Order = eventOrder, Start = mission.StartLocal,
                                        HintKey = MealStepHints.KeyFor(step)
                                    });
                                }
                            }
                            break;

                        case MissionStepType.DayPicker:
                            var existing = dayEvents.FindIndex(d => d.Step.EventType == step.EventType);
                            if (existing >= 0)
                            {
                                dayEvents[existing].Days.UnionWith(window);
                                // Latest start: an event dated on that day must count for every mission sharing it
                                DateTime? start = dayEvents[existing].Start;
                                if (mission.StartLocal.HasValue && (!start.HasValue || mission.StartLocal.Value > start.Value))
                                {
                                    dayEvents[existing] = (dayEvents[existing].Code, dayEvents[existing].Title, step, dayEvents[existing].Days, mission.StartLocal);
                                }
                            }
                            else
                            {
                                dayEvents.Add((mission.Code, mission.Title, step, new SortedSet<DateTime>(window), mission.StartLocal));
                            }
                            break;

                        case MissionStepType.DayQuestion:
                            DayQuestionSpec spec = step.DayQuestion;
                            if (spec == null)
                            {
                                break;
                            }
                            int? remaining = null;
                            if (!spec.HasLimit)
                            {
                                // Missions that resolve at window end reach progress 1 before they complete
                                remaining = Math.Max(0, step.MaxCount - (int)Math.Floor(mission.Progress * step.MaxCount + 0.0001f));
                                if (remaining == 0)
                                {
                                    break;
                                }
                            }
                            dayQuestions.Add((mission, step, window, remaining));
                            break;

                        case MissionStepType.DaySwaps:
                            CheckInSwapMission swapMission = SwapMissionFor(mission, step, inputs);
                            if (swapMission != null)
                            {
                                swapMissions.Add((swapMission, window));
                            }
                            break;

                        case MissionStepType.DayChecklist:
                            inputs.RecordedValues.TryGetValue(step.EventType, out Dictionary<DateTime, HashSet<string>> recordedByDay);
                            var recorded = new Dictionary<DateTime, HashSet<string>>();
                            foreach (DateTime day in window)
                            {
                                if (recordedByDay != null && recordedByDay.TryGetValue(day, out HashSet<string> values) && values.Count > 0)
                                {
                                    recorded[day] = values;
                                }
                            }
                            List<DateTime> openDays = window
                                .Where(d => !ChecklistRule.Counts(step.Checklist, ChecklistRule.IndexesOf(step.Checklist, recorded.TryGetValue(d, out HashSet<string> v) ? v : null)))
                                .ToList();
                            if (openDays.Count > 0)
                            {
                                missionSteps.Add(new CheckInMissionStep(mission.Code, mission.Title, step, mission.StartLocal, openDays, recorded));
                            }
                            break;

                        default:
                            missionSteps.Add(new CheckInMissionStep(mission.Code, mission.Title, step, mission.StartLocal,
                                fromProducts: FromProducts(inputs, step, mission.StartLocal)));
                            break;
                    }
                }
            }

            var allDays = new SortedSet<DateTime>(mealByDay.Keys);
            allDays.UnionWith(dayQuestions.SelectMany(q => q.Window));
            allDays.UnionWith(swapMissions.SelectMany(m => m.Window));
            var mealDays = new List<CheckInMealDay>();
            foreach (DateTime dayKey in allDays)
            {
                var entry = new KeyValuePair<DateTime, List<MealContribution>>(dayKey,
                    mealByDay.TryGetValue(dayKey, out List<MealContribution> contributions) ? contributions : new List<MealContribution>());
                inputs.LoggedMealTypes.TryGetValue(entry.Key, out HashSet<string> logged);
                logged ??= new HashSet<string>();
                // A fully logged day keeps only its day questions
                bool mealsClosed = CheckInMealTypes.Main.All(logged.Contains);

                List<CheckInMealQuestion> questions = entry.Value
                    .GroupBy(c => c.EventType)
                    .Select(g => new
                    {
                        EventType = g.Key,
                        Order = g.Min(c => c.Order),
                        // Restricted only if every contributing mission restricts it to the same meal type
                        Only = g.All(c => c.MealTypes.Count == 1) && g.Select(c => c.MealTypes[0]).Distinct().Count() == 1 ? g.First().MealTypes[0] : null,
                        MealTypes = CheckInMealTypes.All.Where(t => g.Any(c => Counts(c, entry.Key, t, inputs.NowLocal))).ToArray()
                    })
                    .Where(q => q.MealTypes.Length > 0)
                    .OrderBy(q => q.EventType.StartsWith("SWAP_", StringComparison.Ordinal) ? 1 : 0)
                    .ThenBy(q => q.Order)
                    .Select(q => new CheckInMealQuestion(q.EventType, q.Only, q.MealTypes))
                    .ToList();

                bool isToday = entry.Key == inputs.NowLocal.Date;
                string[] open = mealsClosed
                    ? Array.Empty<string>()
                    : CheckInMealTypes.All
                        .Where(t => !logged.Contains(t) && (!isToday || MissionReportDays.IsOfferedToday(t, inputs.NowLocal)) && questions.Any(q => q.AppliesTo(t)))
                        .ToArray();
                List<CheckInDayQuestion> dayAsked = DayQuestionsFor(dayQuestions, entry.Key, inputs, logged, open, questions);
                CheckInSwapQuestion swapQuestion = SwapQuestionFor(swapMissions, entry.Key, inputs);
                if (open.Length == 0 && dayAsked.Count == 0 && swapQuestion == null)
                {
                    continue;
                }
                if (open.Length == 0)
                {
                    questions = new List<CheckInMealQuestion>();
                }
                // Only missions still asked on one of the open meals that day
                string[] hints = entry.Value
                    .Where(c => c.HintKey != null && open.Any(t => Counts(c, entry.Key, t, inputs.NowLocal)))
                    .OrderBy(c => c.Order)
                    .Select(c => c.HintKey)
                    .Distinct()
                    .ToArray();
                // A day with a meal log was told about (Home nudge): only empty days are waiting
                var waiting = new List<CheckInMissionRef>();
                if (logged.Count == 0)
                {
                    waiting.AddRange(dayAsked.Select(q => new CheckInMissionRef(q.MissionCode, q.MissionTitle, q.StartLocal)));
                    waiting.AddRange(entry.Value
                        .Where(c => open.Any(t => Counts(c, entry.Key, t, inputs.NowLocal)))
                        .OrderBy(c => c.Order)
                        .Select(c => new CheckInMissionRef(c.Code, c.Title, c.Start)));
                }
                waiting = waiting.GroupBy(m => m.Code, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
                AddAsked(asked, entry.Value.Where(c => open.Any(t => Counts(c, entry.Key, t, inputs.NowLocal))).OrderBy(c => c.Order)
                    .Select(c => new CheckInMissionRef(c.Code, c.Title, c.Start)));
                AddAsked(asked, dayAsked.Select(q => new CheckInMissionRef(q.MissionCode, q.MissionTitle, q.StartLocal)));
                AddAsked(asked, swapQuestion?.Missions.Select(m => new CheckInMissionRef(m.Code, m.Title, m.StartLocal)));
                // Swaps are optional: a day with only the swap question is neither pending nor waiting
                mealDays.Add(new CheckInMealDay(entry.Key, logged.Count > 0, open, questions, hints, dayAsked, waiting, swapQuestion));
            }

            var openDayEvents = new List<CheckInDayEvent>();
            foreach (var (code, title, step, days, start) in dayEvents)
            {
                inputs.CoveredEventDays.TryGetValue(step.EventType, out HashSet<DateTime> covered);
                List<DateTime> open = days.Where(d => covered == null || !covered.Contains(d)).ToList();
                if (open.Count > 0)
                {
                    openDayEvents.Add(new CheckInDayEvent(code, step, open, start, title));
                }
            }

            AddAsked(asked, openDayEvents.Select(e => new CheckInMissionRef(e.MissionCode, e.MissionTitle, e.StartLocal)));
            AddAsked(asked, missionSteps.Select(m => new CheckInMissionRef(m.MissionCode, m.MissionTitle, m.StartLocal)));
            return new CheckInPlan(mealDays, openDayEvents, missionSteps, secured, asked);
        }

        /// <summary>Reportable and before its deadline (past it nothing counts any more; the backend fails it within the hour).</summary>
        private static bool IsAskable(CheckInMission mission, DateTime nowLocal) =>
            mission != null && mission.Interaction.CanReport &&
            !(mission.StartLocal.HasValue && nowLocal >= MissionDeadlines.DeadlineLocal(mission.StartLocal.Value));

        private static void AddAsked(List<CheckInMissionRef> asked, IEnumerable<CheckInMissionRef> missions)
        {
            foreach (CheckInMissionRef mission in missions ?? Enumerable.Empty<CheckInMissionRef>())
            {
                if (!asked.Any(m => string.Equals(m.Code, mission.Code, StringComparison.OrdinalIgnoreCase)))
                {
                    asked.Add(mission);
                }
            }
        }

        /// <summary>Day questions still open on this day (spec 2026-10-27 §6), in mission order.</summary>
        private static List<CheckInDayQuestion> DayQuestionsFor(
            List<(CheckInMission Mission, MissionReportStep Step, IReadOnlyList<DateTime> Window, int? Remaining)> dayQuestions,
            DateTime day, CheckInInputs inputs, HashSet<string> logged, string[] openMeals, List<CheckInMealQuestion> mealQuestions)
        {
            var asked = new List<CheckInDayQuestion>();
            foreach (var (mission, step, window, remaining) in dayQuestions)
            {
                if (!window.Contains(day) ||
                    (inputs.DecidedDays.TryGetValue(mission.Code, out HashSet<DateTime> decided) && decided.Contains(day)))
                {
                    continue;
                }

                DayQuestionSpec spec = step.DayQuestion;
                if (JoinsMealBlock(step, openMeals, mealQuestions))
                {
                    continue;
                }
                bool open;
                if (spec.Coverage == DayCoverage.FlagDay)
                {
                    open = !spec.CountingOptions
                        .Select(i => step.Options[i].EventType)
                        .Any(e => inputs.CoveredEventDays.TryGetValue(e, out HashSet<DateTime> covered) && covered.Contains(day));
                }
                else
                {
                    // A logged day already counts (or can't any more); all-meals answers are still needed for new meals
                    open = logged.Count == 0 || (spec.AllMeals && openMeals.Length > 0);
                }
                // "Had meat?" asked per meal answers the day question too
                bool derivedFromMeals = !spec.HasLimit && openMeals.Length > 0 &&
                    step.Options.Any(o => o.EventType == ClientEventTypes.MealMeatConsumed) &&
                    mealQuestions.Any(q => q.EventType == ClientEventTypes.MealMeatConsumed);
                if (open)
                {
                    asked.Add(new CheckInDayQuestion(mission.Code, mission.Title, step, mission.StartLocal, remaining, derivedFromMeals));
                }
            }
            return asked;
        }

        /// <summary>What a swap mission still needs (spec 2026-10-27 swaps §6); null when nothing.</summary>
        private static CheckInSwapMission SwapMissionFor(CheckInMission mission, MissionReportStep step, CheckInInputs inputs)
        {
            SwapQuestionSpec spec = step.DaySwaps;
            if (spec == null)
            {
                return null;
            }
            List<string> swaps = step.Options.Select(o => o.EventType).Where(e => e != null).ToList();
            if (spec.IsLevels)
            {
                // Progress only says how many levels are done: the swaps logged since the start say which
                var counted = new HashSet<string>(inputs.LoggedSwaps
                    .Where(l => !mission.StartLocal.HasValue || l.AtLocal >= mission.StartLocal.Value)
                    .Select(l => l.Swap), StringComparer.Ordinal);
                List<int> missing = Enumerable.Range(0, spec.Levels.Count).Where(i => !spec.Levels[i].Any(counted.Contains)).ToList();
                return missing.Count == 0 ? null : new CheckInSwapMission(mission.Code, mission.Title, mission.StartLocal, spec, swaps, null, missing);
            }

            int remaining = Math.Max(0, spec.Target - (int)Math.Floor(mission.Progress * spec.Target + 0.0001f));
            return remaining == 0 ? null : new CheckInSwapMission(mission.Code, mission.Title, mission.StartLocal, spec, swaps, remaining);
        }

        /// <summary>The swap question of a day, shared by the swap missions still asked that day; null when none.</summary>
        private static CheckInSwapQuestion SwapQuestionFor(List<(CheckInSwapMission Mission, IReadOnlyList<DateTime> Window)> swapMissions,
            DateTime day, CheckInInputs inputs)
        {
            List<CheckInSwapMission> missions = swapMissions
                .Where(m => m.Window.Contains(day) &&
                            !(inputs.DecidedDays.TryGetValue(m.Mission.Code, out HashSet<DateTime> decided) && decided.Contains(day)))
                .Select(m => m.Mission)
                .ToList();
            if (missions.Count == 0)
            {
                return null;
            }

            List<string> swaps = missions.SelectMany(m => m.Swaps).Distinct().OrderBy(MissionSwaps.OrderOf).ToList();
            var logged = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var (atLocal, swap) in inputs.LoggedSwaps)
            {
                if (atLocal.Date == day && swaps.Contains(swap))
                {
                    logged[swap] = logged.TryGetValue(swap, out int count) ? count + 1 : 1;
                }
            }
            return new CheckInSwapQuestion(missions, swaps, logged);
        }

        /// <summary>
        /// A day question whose flags are already asked per meal that day would ask the same thing twice: it is answered
        /// on the meals instead, with its missing flags added to them (e.g. M.B5.5 next to the plate missions B5.1 / B5.2).
        /// </summary>
        private static bool JoinsMealBlock(MissionReportStep step, string[] openMeals, List<CheckInMealQuestion> mealQuestions)
        {
            DayQuestionSpec spec = step.DayQuestion;
            if (spec.Coverage != DayCoverage.FlagDay || spec.HasLimit || openMeals.Length == 0)
            {
                return false;
            }
            List<string> flags = spec.CountingOptions.Select(i => step.Options[i].EventType).ToList();
            CheckInMealQuestion shared = mealQuestions.FirstOrDefault(q => flags.Contains(q.EventType));
            if (shared == null)
            {
                return false;
            }
            foreach (string flag in flags.Where(f => mealQuestions.All(q => q.EventType != f)))
            {
                mealQuestions.Add(new CheckInMealQuestion(flag, shared.OnlyMealType, shared.MealTypes));
            }
            return true;
        }

        /// <summary>
        /// Whether a meal of this type on this day would count for the contributing mission: right meal type, and
        /// dated (<see cref="MissionReportDays.TimestampFor"/>) at or after the mission start, since rules ignore earlier events.
        /// </summary>
        /// <summary>
        /// Foods reported from the product detail since the mission started that count for this step: matching its fixed
        /// metadata and, for checklists, satisfying the rule with what was reported for that food.
        /// </summary>
        private static int FromProducts(CheckInInputs inputs, MissionReportStep step, DateTime? startLocal)
        {
            var types = new HashSet<string>(ProductMissionPlanner.EventTypesOf(step), StringComparer.Ordinal);
            IEnumerable<IGrouping<string, CheckInProductReport>> byFood = inputs.ProductReports
                .Where(r => types.Contains(r.EventType) && (!startLocal.HasValue || r.AtLocal >= startLocal.Value) && MatchesFixedMetadata(step, r))
                .GroupBy(r => r.Id, StringComparer.Ordinal);
            if (step.Type != MissionStepType.ProductChecklist)
            {
                return byFood.Count();
            }

            ChecklistSpec spec = step.Checklist;
            return byFood.Count(food =>
            {
                IEnumerable<int> chosen = spec.IsArray
                    ? ChecklistRule.IndexesOf(spec, food.SelectMany(r => r.Values))
                    : food.Select(r => spec.Options.ToList().FindIndex(o => o.EventType == r.EventType)).Where(i => i >= 0);
                return ChecklistRule.Counts(spec, chosen.Distinct().ToList());
            });
        }

        private static bool MatchesFixedMetadata(MissionReportStep step, CheckInProductReport report) =>
            step.FixedMetadata.All(kv => report.Metadata.TryGetValue(kv.Key, out string value) && string.Equals(value, kv.Value?.ToString(), StringComparison.Ordinal));

        private static bool Counts(MealContribution contribution, DateTime day, string mealType, DateTime nowLocal) =>
            (contribution.MealTypes.Count == 0 || contribution.MealTypes.Contains(mealType)) &&
            (!contribution.Start.HasValue || MissionReportDays.TimestampFor(day, mealType, nowLocal) >= contribution.Start.Value);
    }
}
