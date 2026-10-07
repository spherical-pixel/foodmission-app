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

        /// <summary>How long before its typical hour a meal of today can already be reported.</summary>
        public const int TodayLeadHours = 2;

        public static int TypicalHour(string mealType) => mealType switch
        {
            "BREAKFAST" => 8,
            "LUNCH" => 13,
            "SNACK" => 17,
            "DINNER" => 20,
            _ => 13
        };

        /// <summary>Today, a meal is offered from <see cref="TodayLeadHours"/> before its typical hour (breakfast always).</summary>
        public static bool IsOfferedToday(string mealType, DateTime nowLocal) =>
            mealType == "BREAKFAST" || nowLocal.TimeOfDay >= TimeSpan.FromHours(TypicalHour(mealType) - TodayLeadHours);

        /// <summary>Local moment used for a reported meal: a typical hour of the meal type, clamped to now.</summary>
        public static DateTime TimestampFor(DateTime day, string mealType, DateTime nowLocal)
        {
            DateTime moment = day.Date.AddHours(TypicalHour(mealType));
            return moment > nowLocal ? nowLocal : moment;
        }

        /// <summary>
        /// Local moment a day event is dated at: noon (same UTC date as the local day in Europe), never before the
        /// mission start (rules ignore earlier events) nor after now.
        /// </summary>
        public static DateTime EventTimestampFor(DateTime day, DateTime? missionStartLocal, DateTime nowLocal)
        {
            DateTime moment = day.Date.AddHours(12);
            if (missionStartLocal.HasValue && missionStartLocal.Value.Date == day.Date && moment < missionStartLocal.Value)
            {
                moment = missionStartLocal.Value;
            }
            return moment > nowLocal ? nowLocal : moment;
        }
    }
}
