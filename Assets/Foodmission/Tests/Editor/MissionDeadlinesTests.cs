using System;
using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionDeadlinesTests
    {
        // Monday 15:00 local
        private static readonly DateTime Start = new DateTime(2026, 10, 5, 15, 0, 0, DateTimeKind.Local);
        private static readonly TimeSpan Ten = TimeSpan.FromHours(10);
        private static readonly TimeSpan Eight = TimeSpan.FromHours(20);

        [Test]
        public void Deadline_IsSevenDaysAfterTheStart()
        {
            // Every mission rule is a 7-day since_start window
            Assert.AreEqual(Start.AddDays(7), MissionDeadlines.DeadlineLocal(Start));
        }

        [Test]
        public void Reminder_IsOnTheDeadlineDay_WhenThePreferredTimeComesFirst()
        {
            Assert.AreEqual(Start.Date.AddDays(7).AddHours(10), MissionDeadlines.ReminderLocal(Start, Ten));
        }

        [Test]
        public void Reminder_IsTheDayBefore_WhenThePreferredTimeIsAfterTheDeadline()
        {
            // Ends next Monday 15:00: a Monday 20:00 reminder would be too late
            Assert.AreEqual(Start.Date.AddDays(6).AddHours(20), MissionDeadlines.ReminderLocal(Start, Eight));
        }

        [Test]
        public void LastDays_GroupMissionsByReminder_AndSkipThoseWithoutStart()
        {
            var missions = new[]
            {
                new MissionDeadlines.ActiveMission("M.A1.1", "Green zone", Start),
                new MissionDeadlines.ActiveMission("M.B1.3", "One fewer", Start.AddHours(1)),
                new MissionDeadlines.ActiveMission("M.A5.4", "FIFO", Start.AddDays(2)),
                new MissionDeadlines.ActiveMission("M.X", "Not started", null)
            };

            var days = MissionDeadlines.LastDays(missions, Ten);

            Assert.AreEqual(2, days.Count);
            CollectionAssert.AreEqual(new[] { "M.A1.1", "M.B1.3" }, days[0].Missions.Select(m => m.Code).ToArray());
            Assert.AreEqual(Start.Date.AddDays(7).AddHours(10), days[0].ReminderLocal);
            CollectionAssert.AreEqual(new[] { "M.A5.4" }, days[1].Missions.Select(m => m.Code).ToArray());
        }

        [Test]
        public void Today_IsTheGroupRemindedToday_WhileItsMissionsCanStillCount()
        {
            var missions = new[] { new MissionDeadlines.ActiveMission("M.A1.1", "Green zone", Start) };
            DateTime lastDay = Start.Date.AddDays(7);

            Assert.IsNotNull(MissionDeadlines.Today(missions, Ten, lastDay.AddHours(9)), "before the reminder hour it is still the last day");
            Assert.IsNull(MissionDeadlines.Today(missions, Ten, lastDay.AddHours(16)), "past the deadline nothing counts any more");
            Assert.IsNull(MissionDeadlines.Today(missions, Ten, lastDay.AddDays(-1).AddHours(12)));
        }
    }
}
