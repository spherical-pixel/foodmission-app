using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>Day questions inside one check-in (spec 2026-10-27 §5–6). Pure.</summary>
    public static class CheckInDayRules
    {
        /// <summary>
        /// Questions of a day still needed: a mission stops being asked once the counting answers given on earlier days
        /// cover what it still needed when the plan was made. Missions with a limit are always asked.
        /// </summary>
        public static IReadOnlyList<CheckInDayQuestion> Visible(CheckInPlan plan, CheckInAnswers answers, int dayIndex)
        {
            if (plan == null || answers == null || dayIndex < 0 || dayIndex >= plan.MealDays.Count)
            {
                return Array.Empty<CheckInDayQuestion>();
            }

            CheckInMealDay day = plan.MealDays[dayIndex];
            // A chosen meal asking "had meat?" answers the meat day question
            bool meatAskedPerMeal = dayIndex < answers.MealDays.Count && answers.MealDays[dayIndex].Meals.Keys
                .Any(m => day.OpenMealTypes.Contains(m) && day.QuestionsFor(m).Any(q => q.EventType == ClientEventTypes.MealMeatConsumed));
            return day.DayQuestions
                .Where(q => !(q.DerivedFromMeals && meatAskedPerMeal))
                .Where(q => !q.Remaining.HasValue || CountingAnswersBefore(plan, answers, dayIndex, q.MissionCode) < q.Remaining.Value)
                .ToList();
        }

        /// <summary>Removes answers of questions that are no longer shown. True when something was removed.</summary>
        public static bool ClearHidden(CheckInPlan plan, CheckInAnswers answers)
        {
            bool removed = false;
            // Visibility of a day only depends on earlier days, so one pass in order is enough
            for (int i = 0; i < plan.MealDays.Count && i < answers.MealDays.Count; i++)
            {
                var visible = new HashSet<string>(Visible(plan, answers, i).Select(q => q.MissionCode), StringComparer.OrdinalIgnoreCase);
                foreach (string code in answers.MealDays[i].DayAnswers.Keys.Where(c => !visible.Contains(c)).ToList())
                {
                    answers.MealDays[i].DayAnswers.Remove(code);
                    removed = true;
                }
            }
            return removed;
        }

        /// <summary>
        /// Questions left unanswered on a day that would send something, where any log would answer them silently:
        /// all-meals ratios (every meal log of that day counts) and logged-day questions (a logged day without meat is
        /// a meat-free day).
        /// </summary>
        public static IReadOnlyList<CheckInDayQuestion> MissingRequired(CheckInPlan plan, CheckInAnswers answers, int dayIndex)
        {
            if (plan == null || answers == null || dayIndex < 0 || dayIndex >= plan.MealDays.Count || dayIndex >= answers.MealDays.Count)
            {
                return Array.Empty<CheckInDayQuestion>();
            }

            IReadOnlyList<CheckInDayQuestion> visible = Visible(plan, answers, dayIndex);
            CheckInMealDayAnswer answer = answers.MealDays[dayIndex];
            List<CheckInDayQuestion> unanswered = visible
                .Where(q => (q.Step.DayQuestion.AllMeals || q.Step.DayQuestion.Coverage == DayCoverage.LoggedDay) && !answer.DayAnswers.ContainsKey(q.MissionCode))
                .ToList();
            if (unanswered.Count == 0)
            {
                return unanswered;
            }

            CheckInMealDay day = plan.MealDays[dayIndex];
            bool sendsMeal = answer.Meals.Any(m => day.OpenMealTypes.Contains(m.Key) && CheckInBuilder.CanSendMeal(day, m.Key, m.Value));
            bool sendsDayFlag = visible.Any(q => EventOf(q, answer) != null);
            return sendsMeal || sendsDayFlag ? unanswered : new List<CheckInDayQuestion>();
        }

        /// <summary>Event of the chosen option of this question, null when unanswered or the answer sends nothing.</summary>
        public static string EventOf(CheckInDayQuestion question, CheckInMealDayAnswer answer)
        {
            if (!answer.DayAnswers.TryGetValue(question.MissionCode, out int option) || option < 0 || option >= question.Step.Options.Count)
            {
                return null;
            }
            return question.Step.Options[option].EventType;
        }

        private static int CountingAnswersBefore(CheckInPlan plan, CheckInAnswers answers, int dayIndex, string missionCode)
        {
            int count = 0;
            for (int i = 0; i < dayIndex && i < answers.MealDays.Count; i++)
            {
                CheckInDayQuestion question = plan.MealDays[i].DayQuestions
                    .FirstOrDefault(q => string.Equals(q.MissionCode, missionCode, StringComparison.OrdinalIgnoreCase));
                if (question != null && answers.MealDays[i].DayAnswers.TryGetValue(missionCode, out int option) &&
                    question.Step.DayQuestion.CountingOptions.Contains(option))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
