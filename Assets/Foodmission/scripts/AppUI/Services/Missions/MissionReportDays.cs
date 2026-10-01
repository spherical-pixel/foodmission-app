using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    /// <summary>Days a manual report may target (spec §3.5): [max(missionStart, today − 6) … today], never the future.</summary>
    public static class MissionReportDays
    {
        public const int WindowDays = 7;

        public static IReadOnlyList<DateTime> Allowed(DateTime? missionStartLocal, DateTime nowLocal)
        {
            DateTime today = nowLocal.Date;
            DateTime first = today.AddDays(-(WindowDays - 1));
            if (missionStartLocal.HasValue && missionStartLocal.Value.Date > first)
            {
                first = missionStartLocal.Value.Date;
            }
            if (first > today)
            {
                first = today;
            }

            var days = new List<DateTime>();
            for (DateTime day = first; day <= today; day = day.AddDays(1))
            {
                days.Add(day);
            }
            return days;
        }

        /// <summary>Local moment used for a reported meal: a typical hour of the meal type, clamped to now.</summary>
        public static DateTime TimestampFor(DateTime day, string mealType, DateTime nowLocal)
        {
            int hour = mealType switch
            {
                "BREAKFAST" => 8,
                "LUNCH" => 13,
                "SNACK" => 17,
                "DINNER" => 20,
                _ => 13
            };

            DateTime moment = day.Date.AddHours(hour);
            return moment > nowLocal ? nowLocal : moment;
        }
    }
}
