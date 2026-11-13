using System;
using System.Collections.Generic;
using System.Globalization;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Which daily meal reminders to schedule. Local notifications don't repeat (a repeating one couldn't skip a day),
    /// so the next <see cref="WindowDays"/> days are scheduled and the window is renewed whenever the app opens.
    /// </summary>
    public static class DailyMealReminderPlan
    {
        public const int WindowDays = 7;
        public const string IdPrefix = "daily_meal_reminder_";
        /// <summary>Id of the single reminder scheduled before 0.1.1; cancelled on every sync.</summary>
        public const string LegacyId = "daily_meal_reminder";
        private const string DayFormat = "yyyy-MM-dd";

        /// <summary>
        /// Reminder times (local) from today on: not today when a meal is already logged today, never in the past,
        /// and not on a day with a mission last-day reminder (it fires at the same time).
        /// </summary>
        public static List<DateTime> Times(DateTime nowLocal, TimeSpan preferredTime, bool loggedToday, ICollection<DateTime> missionReminderDays)
        {
            var times = new List<DateTime>(WindowDays);
            for (int i = 0; i < WindowDays; i++)
            {
                DateTime day = nowLocal.Date.AddDays(i);
                DateTime at = day.Add(preferredTime);
                if (at <= nowLocal || (i == 0 && loggedToday) || (missionReminderDays != null && missionReminderDays.Contains(day)))
                {
                    continue;
                }
                times.Add(at);
            }
            return times;
        }

        public static string IdFor(DateTime dayLocal) => IdPrefix + dayLocal.ToString(DayFormat, CultureInfo.InvariantCulture);
    }
}
