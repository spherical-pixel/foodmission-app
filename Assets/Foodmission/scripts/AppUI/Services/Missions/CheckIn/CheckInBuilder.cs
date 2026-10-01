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

                    if (!TryBuildMeal(day, mealType, checkedEvents, out List<string> flags, out List<string> swaps))
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

        /// <summary>Whether a chosen meal would produce a meal log (otherwise the screen tells the user to mark something).</summary>
        public static bool CanSendMeal(CheckInMealDay day, string mealType, ISet<string> checkedEvents) =>
            TryBuildMeal(day, mealType, checkedEvents, out _, out _);

        private static bool TryBuildMeal(CheckInMealDay day, string mealType, ISet<string> checkedEvents, out List<string> flags, out List<string> swaps)
        {
            IReadOnlyList<CheckInMealQuestion> questions = day.QuestionsFor(mealType);
            List<CheckInMealQuestion> applicable = questions.Where(q => checkedEvents != null && checkedEvents.Contains(q.EventType)).ToList();
            flags = applicable.Where(q => !q.IsSwap).Select(q => q.EventType).ToList();
            swaps = applicable.Where(q => q.IsSwap).Select(q => q.EventType).ToList();

            if (flags.Contains(ClientEventTypes.MealMeatFree) || flags.Contains(ClientEventTypes.MealVegan))
            {
                flags.Remove(ClientEventTypes.MealMeatConsumed);
            }
            // The user was asked "had meat?" for this meal and left it unchecked: the meal was meat-free
            if (!flags.Contains(ClientEventTypes.MealMeatConsumed) && !flags.Contains(ClientEventTypes.MealMeatFree) &&
                !flags.Contains(ClientEventTypes.MealVegan) && questions.Any(q => q.EventType == ClientEventTypes.MealMeatConsumed))
            {
                flags.Add(ClientEventTypes.MealMeatFree);
            }

            return flags.Count > 0 || swaps.Count > 0;
        }
    }
}
