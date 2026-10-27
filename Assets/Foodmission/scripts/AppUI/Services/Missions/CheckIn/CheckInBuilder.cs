using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>Check-in answers → grouped quick meal logs + events (spec §3.9). Pure.</summary>
    public static class CheckInBuilder
    {
        /// <summary>Meal type of the day log (spec 2026-10-27): outside the main-meal rules.</summary>
        public const string OtherMealType = "OTHER";

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
                CheckInMealDayAnswer dayAnswer = answers.MealDays[i];
                IReadOnlyList<CheckInDayQuestion> dayQuestions = CheckInDayRules.Visible(plan, answers, i);
                // "Yes" to an all-meals question is true of every meal of that day
                List<string> allMealsFlags = dayQuestions
                    .Where(q => q.Step.DayQuestion.AllMeals)
                    .Select(q => CheckInDayRules.EventOf(q, dayAnswer))
                    .Where(e => e != null)
                    .ToList();
                foreach (string mealType in CheckInMealTypes.All)
                {
                    if (!day.OpenMealTypes.Contains(mealType) || !dayAnswer.Meals.TryGetValue(mealType, out HashSet<string> checkedEvents))
                    {
                        continue;
                    }

                    TryBuildMeal(day, mealType, checkedEvents, out List<string> flags, out List<string> swaps);
                    flags.AddRange(allMealsFlags.Where(f => !flags.Contains(f)));
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

                // Every day answer of the day in one OTHER meal log: no food, only flags and swaps, never claiming which meal it was
                List<CheckInDayQuestion> answered = dayQuestions.Where(q => CheckInDayRules.EventOf(q, dayAnswer) != null).ToList();
                List<(string Swap, int Units)> swapUnits = CheckInSwapRules.Visible(plan, answers, i)
                    .Select(o => (o.Swap, Units: dayAnswer.Swaps.TryGetValue(o.Swap, out int units) ? Math.Min(units, o.Max) : 0))
                    .Where(s => s.Units > 0)
                    .ToList();
                if (answered.Count > 0 || swapUnits.Count > 0)
                {
                    DateTime? latestStart = answered.Select(q => q.StartLocal)
                        .Concat(swapUnits.Count > 0 ? day.SwapQuestion.Missions.Select(m => m.StartLocal) : Enumerable.Empty<DateTime?>())
                        .Where(start => start.HasValue).DefaultIfEmpty(null).Max();
                    string timestamp = MissionReportDays.EventTimestampFor(day.Day, latestStart, nowLocal).ToUniversalTime().ToString("o");
                    items.Add(new PendingReportItem(new CreateMealLogRequest
                    {
                        typeOfMeal = OtherMealType,
                        flags = answered.Count > 0 ? answered.Select(q => CheckInDayRules.EventOf(q, dayAnswer)).Distinct().ToArray() : null,
                        swaps = swapUnits.Count > 0 ? swapUnits.Select(s => s.Swap).ToArray() : null,
                        timestamp = timestamp
                    }));
                    // A meal log carries a swap once (backend @ArrayUnique): each further unit is its own OTHER log
                    foreach (var (swap, units) in swapUnits)
                    {
                        for (int extra = 1; extra < units; extra++)
                        {
                            items.Add(new PendingReportItem(new CreateMealLogRequest
                            {
                                typeOfMeal = OtherMealType,
                                swaps = new[] { swap },
                                timestamp = timestamp
                            }));
                        }
                    }
                }
            }

            for (int i = 0; i < plan.DayEvents.Count && i < answers.DayEvents.Count; i++)
            {
                CheckInDayEvent dayEvent = plan.DayEvents[i];
                var answer = new MissionStepAnswer();
                answer.Days.AddRange(answers.DayEvents[i].Where(d => dayEvent.OpenDays.Contains(d.Date)).Select(d => d.Date));
                items.AddRange(MissionReportBuilder.Build(dayEvent.MissionCode, new[] { dayEvent.Step }, new[] { answer }, reportId, dayEvent.StartLocal, nowLocal, items.Count));
            }

            for (int i = 0; i < plan.MissionSteps.Count && i < answers.MissionSteps.Count; i++)
            {
                CheckInMissionStep missionStep = plan.MissionSteps[i];
                items.AddRange(MissionReportBuilder.Build(missionStep.MissionCode, new[] { missionStep.Step }, new[] { answers.MissionSteps[i] }, reportId, missionStep.StartLocal, nowLocal, items.Count));
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

            // A swap that ends in meat (beef → chicken) means the meal had meat, whatever else was checked
            if (swaps.Any(MeatSwaps.EndsInMeat))
            {
                flags.Remove(ClientEventTypes.MealMeatFree);
                flags.Remove(ClientEventTypes.MealVegan);
                if (!flags.Contains(ClientEventTypes.MealMeatConsumed) && questions.Any(q => q.EventType == ClientEventTypes.MealMeatConsumed))
                {
                    flags.Add(ClientEventTypes.MealMeatConsumed);
                }
            }
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
