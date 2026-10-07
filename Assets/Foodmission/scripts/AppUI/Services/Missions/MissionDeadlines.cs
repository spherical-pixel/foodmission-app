using System;
using System.Collections.Generic;
using System.Linq;

namespace eu.foodmission.platform
{
    /// <summary>
    /// When a mission's reporting window ends and when to remind its last day. Pure.
    /// Every mission rule is a since_start window of 7 days: events after startedAt + 7 days never count,
    /// and the backend fails the mission within the hour. Past days can't be reported once it failed.
    /// </summary>
    public static class MissionDeadlines
    {
        public const int WindowDays = 7;

        public sealed class ActiveMission
        {
            public string Code { get; }
            public string Title { get; }
            public DateTime? StartLocal { get; }

            public ActiveMission(string code, string title, DateTime? startLocal)
            {
                Code = code;
                Title = title;
                StartLocal = startLocal;
            }
        }

        /// <summary>Missions sharing a last-day reminder.</summary>
        public sealed class LastDay
        {
            public DateTime ReminderLocal { get; }
            /// <summary>Earliest deadline of the group: after it, reporting no longer counts.</summary>
            public DateTime DeadlineLocal { get; }
            public IReadOnlyList<ActiveMission> Missions { get; }

            public LastDay(DateTime reminderLocal, DateTime deadlineLocal, IReadOnlyList<ActiveMission> missions)
            {
                ReminderLocal = reminderLocal;
                DeadlineLocal = deadlineLocal;
                Missions = missions;
            }
        }

        public static DateTime DeadlineLocal(DateTime startLocal) => startLocal.AddDays(WindowDays);

        /// <summary>
        /// The preferred time on the deadline day, or on the day before when that time is already past the
        /// deadline (a mission started at 15:00 ends at 15:00: a 20:00 reminder that day would be too late).
        /// </summary>
        public static DateTime ReminderLocal(DateTime startLocal, TimeSpan preferredTime)
        {
            DateTime deadline = DeadlineLocal(startLocal);
            DateTime reminder = deadline.Date.Add(preferredTime);
            return reminder < deadline ? reminder : reminder.AddDays(-1);
        }

        public static IReadOnlyList<LastDay> LastDays(IEnumerable<ActiveMission> missions, TimeSpan preferredTime)
        {
            return (missions ?? Enumerable.Empty<ActiveMission>())
                .Where(m => m?.StartLocal != null)
                .GroupBy(m => ReminderLocal(m.StartLocal.Value, preferredTime))
                .OrderBy(g => g.Key)
                .Select(g => new LastDay(g.Key, g.Min(m => DeadlineLocal(m.StartLocal.Value)), g.OrderBy(m => m.StartLocal).ToList()))
                .ToList();
        }

        /// <summary>The group whose last day is today (from its reminder day until its deadline), or null.</summary>
        public static LastDay Today(IEnumerable<ActiveMission> missions, TimeSpan preferredTime, DateTime nowLocal)
        {
            return LastDays(missions, preferredTime)
                .FirstOrDefault(d => nowLocal.Date >= d.ReminderLocal.Date && nowLocal < d.DeadlineLocal);
        }
    }
}
