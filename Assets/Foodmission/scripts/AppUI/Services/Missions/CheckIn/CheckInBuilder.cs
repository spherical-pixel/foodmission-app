using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>Check-in answers → grouped quick meal logs + events (spec §3.9). Pure.</summary>
    public static class CheckInBuilder
    {
        public static List<PendingReportItem> Build(CheckInPlan plan, CheckInAnswers answers, string reportId, DateTime nowLocal)
        {
            var items = new List<PendingReportItem>();
            if (plan == null || answers == null)
            {
                return items;
            }

            for (int i = 0; i < plan.MealDays.Count && i < answers.MealDays.Count; i++)
            {
                CheckInMealDay day = plan.MealDays[i];
                foreach (string mealType in CheckInMealTypes.All)
                {
                    if (!day.OpenMealTypes.Contains(mealType) || !answers.MealDays[i].Meals.TryGetValue(mealType, out HashSet<string> checkedEvents))
                    {
                        continue;
                    }

                    List<CheckInMealQuestion> applicable = day.QuestionsFor(mealType).Where(q => checkedEvents.Contains(q.EventType)).ToList();
                    List<string> flags = applicable.Where(q => !q.IsSwap).Select(q => q.EventType).ToList();
                    List<string> swaps = applicable.Where(q => q.IsSwap).Select(q => q.EventType).ToList();
                    if (flags.Contains(ClientEventTypes.MealMeatFree) || flags.Contains(ClientEventTypes.MealVegan))
                    {
                        flags.Remove(ClientEventTypes.MealMeatConsumed);
                    }
                    if (flags.Count == 0 && swaps.Count == 0)
                    {
                        continue;
                    }

                    DateTime moment = MissionReportDays.TimestampFor(day.Day, mealType, nowLocal);
                    items.Add(new PendingReportItem(new CreateMealLogRequest
                    {
                        typeOfMeal = mealType,
                        flags = flags.Count > 0 ? flags.ToArray() : null,
                        swaps = swaps.Count > 0 ? swaps.ToArray() : null,
                        timestamp = moment.ToUniversalTime().ToString("o")
                    }));
                }
            }

            for (int i = 0; i < plan.DayEvents.Count && i < answers.DayEvents.Count; i++)
            {
                CheckInDayEvent dayEvent = plan.DayEvents[i];
                var answer = new MissionStepAnswer();
                answer.Days.AddRange(answers.DayEvents[i].Where(d => dayEvent.OpenDays.Contains(d.Date)).Select(d => d.Date));
                items.AddRange(MissionReportBuilder.Build(dayEvent.MissionCode, new[] { dayEvent.Step }, new[] { answer }, reportId, null, nowLocal, items.Count));
            }

            for (int i = 0; i < plan.MissionSteps.Count && i < answers.MissionSteps.Count; i++)
            {
                CheckInMissionStep missionStep = plan.MissionSteps[i];
                items.AddRange(MissionReportBuilder.Build(missionStep.MissionCode, new[] { missionStep.Step }, new[] { answers.MissionSteps[i] }, reportId, null, nowLocal, items.Count));
            }

            return items;
        }
    }
}
