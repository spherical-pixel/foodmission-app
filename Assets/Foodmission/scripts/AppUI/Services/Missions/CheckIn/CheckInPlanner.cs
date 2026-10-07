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
            public string EventType;
            public string OnlyMealType;
            public int Order;
        }

        public static CheckInPlan Plan(CheckInInputs inputs)
        {
            var mealByDay = new SortedDictionary<DateTime, List<MealContribution>>();
            var dayEvents = new List<(string Code, MissionReportStep Step, SortedSet<DateTime> Days, DateTime? Start)>();
            var missionSteps = new List<CheckInMissionStep>();
            int order = 0;

            foreach (CheckInMission mission in inputs.Missions)
            {
                if (mission == null || !mission.Interaction.CanReport)
                {
                    continue;
                }

                IReadOnlyList<DateTime> window = MissionReportDays.Allowed(mission.StartLocal, inputs.NowLocal);
                foreach (MissionReportStep step in mission.Interaction.Steps)
                {
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
                                    list.Add(new MealContribution { EventType = eventType, OnlyMealType = step.FixedMealType, Order = eventOrder });
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
                                    dayEvents[existing] = (dayEvents[existing].Code, step, dayEvents[existing].Days, mission.StartLocal);
                                }
                            }
                            else
                            {
                                dayEvents.Add((mission.Code, step, new SortedSet<DateTime>(window), mission.StartLocal));
                            }
                            break;

                        default:
                            missionSteps.Add(new CheckInMissionStep(mission.Code, mission.Title, step));
                            break;
                    }
                }
            }

            var mealDays = new List<CheckInMealDay>();
            foreach (KeyValuePair<DateTime, List<MealContribution>> entry in mealByDay)
            {
                inputs.LoggedMealTypes.TryGetValue(entry.Key, out HashSet<string> logged);
                logged ??= new HashSet<string>();
                if (CheckInMealTypes.Main.All(logged.Contains))
                {
                    continue;
                }

                List<CheckInMealQuestion> questions = entry.Value
                    .GroupBy(c => c.EventType)
                    .Select(g => new
                    {
                        EventType = g.Key,
                        Order = g.Min(c => c.Order),
                        // Restricted only if every contributing mission restricts it to the same meal type
                        Only = g.All(c => c.OnlyMealType != null) && g.Select(c => c.OnlyMealType).Distinct().Count() == 1 ? g.First().OnlyMealType : null
                    })
                    .OrderBy(q => q.EventType.StartsWith("SWAP_", StringComparison.Ordinal) ? 1 : 0)
                    .ThenBy(q => q.Order)
                    .Select(q => new CheckInMealQuestion(q.EventType, q.Only))
                    .ToList();

                bool isToday = entry.Key == inputs.NowLocal.Date;
                string[] open = CheckInMealTypes.All
                    .Where(t => !logged.Contains(t) && (!isToday || MissionReportDays.IsOfferedToday(t, inputs.NowLocal)))
                    .ToArray();
                if (open.Length == 0)
                {
                    continue;
                }
                mealDays.Add(new CheckInMealDay(entry.Key, logged.Count > 0, open, questions));
            }

            var openDayEvents = new List<CheckInDayEvent>();
            foreach (var (code, step, days, start) in dayEvents)
            {
                inputs.CoveredEventDays.TryGetValue(step.EventType, out HashSet<DateTime> covered);
                List<DateTime> open = days.Where(d => covered == null || !covered.Contains(d)).ToList();
                if (open.Count > 0)
                {
                    openDayEvents.Add(new CheckInDayEvent(code, step, open, start));
                }
            }

            return new CheckInPlan(mealDays, openDayEvents, missionSteps);
        }
    }
}
