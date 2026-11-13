using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    public class DailyMealReminderPlanTests
    {
        private static readonly TimeSpan Ten = TimeSpan.FromHours(10);

        [Test]
        public void Times_BeforePreferredTime_SchedulesTodayAndTheNextSixDays()
        {
            var now = new DateTime(2026, 11, 12, 8, 0, 0);

            List<DateTime> times = DailyMealReminderPlan.Times(now, Ten, false, null);

            Assert.AreEqual(7, times.Count);
            Assert.AreEqual(new DateTime(2026, 11, 12, 10, 0, 0), times.First());
            Assert.AreEqual(new DateTime(2026, 11, 18, 10, 0, 0), times.Last());
        }

        [Test]
        public void Times_AfterPreferredTime_SkipsToday()
        {
            var now = new DateTime(2026, 11, 12, 10, 0, 0);

            List<DateTime> times = DailyMealReminderPlan.Times(now, Ten, false, null);

            Assert.AreEqual(6, times.Count);
            Assert.AreEqual(new DateTime(2026, 11, 13, 10, 0, 0), times.First());
        }

        [Test]
        public void Times_MealLoggedToday_SkipsTodayOnly()
        {
            var now = new DateTime(2026, 11, 12, 8, 0, 0);

            List<DateTime> times = DailyMealReminderPlan.Times(now, Ten, true, null);

            Assert.AreEqual(6, times.Count);
            Assert.AreEqual(new DateTime(2026, 11, 13, 10, 0, 0), times.First());
        }

        [Test]
        public void Times_MissionLastDayReminder_SkipsThatDay()
        {
            var now = new DateTime(2026, 11, 12, 8, 0, 0);
            var missionDays = new HashSet<DateTime> { new DateTime(2026, 11, 14) };

            List<DateTime> times = DailyMealReminderPlan.Times(now, Ten, false, missionDays);

            Assert.AreEqual(6, times.Count);
            CollectionAssert.DoesNotContain(times, new DateTime(2026, 11, 14, 10, 0, 0));
        }

        [Test]
        public void IdFor_UsesTheLocalDay()
        {
            Assert.AreEqual("daily_meal_reminder_2026-11-12", DailyMealReminderPlan.IdFor(new DateTime(2026, 11, 12, 10, 0, 0)));
        }
    }
}
