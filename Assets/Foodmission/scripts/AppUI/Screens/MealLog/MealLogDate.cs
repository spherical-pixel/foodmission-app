using System;
using System.Collections.Generic;
using System.Globalization;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Day a meal is logged for (issue #65): today or up to <see cref="DaysBack"/> days before. Only the day is chosen;
    /// past days get a typical local time for the meal type so the log lands on that day and keeps a sensible order.
    /// </summary>
    public static class MealLogDate
    {
        public const int DaysBack = 6;

        private static readonly Dictionary<string, TimeSpan> TypicalTimes = new()
        {
            { "BREAKFAST", new TimeSpan(8, 0, 0) },
            { "LUNCH", new TimeSpan(13, 30, 0) },
            { "SNACK", new TimeSpan(17, 0, 0) },
            { "DINNER", new TimeSpan(20, 30, 0) },
        };

        private static readonly TimeSpan DefaultTime = new(12, 0, 0);

        /// <summary>Selectable days, oldest first, ending with today (local dates, midnight).</summary>
        public static IReadOnlyList<DateTime> AvailableDays(DateTime nowLocal)
        {
            var days = new List<DateTime>(DaysBack + 1);
            for (int i = DaysBack; i >= 0; i--)
            {
                days.Add(nowLocal.Date.AddDays(-i));
            }
            return days;
        }

        public static bool IsToday(DateTime day, DateTime nowLocal)
        {
            return day.Date == nowLocal.Date;
        }

        /// <summary>UTC ISO timestamp for a meal of <paramref name="mealType"/> eaten on <paramref name="day"/>.
        /// Today (or a future day, never offered) is "now".</summary>
        public static string BuildTimestamp(DateTime day, string mealType, DateTime nowLocal)
        {
            if (day.Date >= nowLocal.Date)
            {
                return nowLocal.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
            }

            TimeSpan time = mealType != null && TypicalTimes.TryGetValue(mealType, out TimeSpan t) ? t : DefaultTime;
            var local = DateTime.SpecifyKind(day.Date + time, DateTimeKind.Local);
            return local.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        }
    }
}
