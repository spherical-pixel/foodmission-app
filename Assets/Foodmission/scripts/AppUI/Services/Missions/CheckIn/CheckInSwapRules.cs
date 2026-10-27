using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>One chip or counter of a day's swap question.</summary>
    public sealed class CheckInSwapOption
    {
        public string Swap { get; }
        public bool IsCounter { get; }
        /// <summary>Times already logged that day (checked and locked).</summary>
        public int Logged { get; }
        /// <summary>New units allowed: 0 or 1 for a chip, up to the counter cap for a counter.</summary>
        public int Max { get; }

        public CheckInSwapOption(string swap, bool isCounter, int logged, int max)
        {
            Swap = swap;
            IsCounter = isCounter;
            Logged = logged;
            Max = max;
        }
    }

    /// <summary>The day swap question inside one check-in (spec 2026-10-27 swaps §5–6). Pure.</summary>
    public static class CheckInSwapRules
    {
        /// <summary>
        /// Chips and counters of a day's swap question: a swap is offered while a mission behind the question still needs
        /// it after the answers of earlier days, and shown locked when it was already logged that day. Empty = no question.
        /// </summary>
        public static IReadOnlyList<CheckInSwapOption> Visible(CheckInPlan plan, CheckInAnswers answers, int dayIndex)
        {
            if (plan == null || answers == null || dayIndex < 0 || dayIndex >= plan.MealDays.Count)
            {
                return Array.Empty<CheckInSwapOption>();
            }
            CheckInSwapQuestion question = plan.MealDays[dayIndex].SwapQuestion;
            if (question == null)
            {
                return Array.Empty<CheckInSwapOption>();
            }

            // Visibility of a day only depends on earlier days
            var needs = question.Missions.ToDictionary(m => m.Code, m => Needs(plan, answers, dayIndex, m), StringComparer.OrdinalIgnoreCase);
            var options = new List<CheckInSwapOption>();
            foreach (string swap in question.Swaps)
            {
                int logged = question.LoggedCount(swap);
                List<int> lefts = question.Missions
                    .Where(m => m.Swaps.Contains(swap))
                    .Select(m => needs[m.Code](swap))
                    .Where(left => left > 0)
                    .ToList();
                if (lefts.Count == 0 && logged == 0)
                {
                    continue;
                }

                bool isCounter = MissionSwaps.IsRepeatable(swap);
                int max = lefts.Count == 0 ? 0
                    : isCounter ? Math.Max(0, Math.Min(MissionSwaps.MaxPerDay - logged, lefts.Max()))
                    : logged > 0 ? 0 : 1;
                options.Add(new CheckInSwapOption(swap, isCounter, logged, max));
            }
            // Only locked chips left: nothing to ask that day
            return options.Any(o => o.Max > 0) ? options : (IReadOnlyList<CheckInSwapOption>)Array.Empty<CheckInSwapOption>();
        }

        /// <summary>Removes answers of hidden questions or options and clamps units to their max. True when something changed.</summary>
        public static bool ClearHidden(CheckInPlan plan, CheckInAnswers answers)
        {
            bool changed = false;
            for (int i = 0; i < plan.MealDays.Count && i < answers.MealDays.Count; i++)
            {
                CheckInMealDayAnswer answer = answers.MealDays[i];
                IReadOnlyList<CheckInSwapOption> options = Visible(plan, answers, i);
                if (options.Count == 0)
                {
                    if (answer.Swaps.Count > 0 || answer.NoSwaps)
                    {
                        answer.Swaps.Clear();
                        answer.NoSwaps = false;
                        changed = true;
                    }
                    continue;
                }
                foreach (string swap in answer.Swaps.Keys.ToList())
                {
                    CheckInSwapOption option = options.FirstOrDefault(o => o.Swap == swap);
                    int units = Math.Min(answer.Swaps[swap], option?.Max ?? 0);
                    if (units <= 0)
                    {
                        answer.Swaps.Remove(swap);
                        changed = true;
                    }
                    else if (units != answer.Swaps[swap])
                    {
                        answer.Swaps[swap] = units;
                        changed = true;
                    }
                }
            }
            return changed;
        }

        /// <summary>The day's swap question was answered: some swap, or "No".</summary>
        public static bool IsAnswered(CheckInMealDayAnswer answer) =>
            answer != null && (answer.NoSwaps || answer.Swaps.Values.Any(units => units > 0));

        /// <summary>For one mission: how many more of a swap it still takes after earlier days (0 = not needed).</summary>
        private static Func<string, int> Needs(CheckInPlan plan, CheckInAnswers answers, int dayIndex, CheckInSwapMission mission)
        {
            var told = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < dayIndex && i < answers.MealDays.Count; i++)
            {
                CheckInSwapQuestion earlier = plan.MealDays[i].SwapQuestion;
                if (earlier == null || !earlier.Missions.Any(m => string.Equals(m.Code, mission.Code, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                foreach (KeyValuePair<string, int> pair in answers.MealDays[i].Swaps.Where(p => p.Value > 0 && mission.Swaps.Contains(p.Key)))
                {
                    told[pair.Key] = told.TryGetValue(pair.Key, out int units) ? units + pair.Value : pair.Value;
                }
            }

            if (mission.Spec.IsLevels)
            {
                var missing = new HashSet<int>(mission.MissingLevels.Where(level => !mission.Spec.Levels[level].Any(told.ContainsKey)));
                return swap => missing.Any(level => mission.Spec.Levels[level].Contains(swap)) ? 1 : 0;
            }
            int left = Math.Max(0, (mission.Remaining ?? 0) - told.Values.Sum());
            return _ => left;
        }
    }
}
