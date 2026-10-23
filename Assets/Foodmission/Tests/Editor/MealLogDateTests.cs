using System;
using System.Collections.Generic;
using System.Globalization;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    public class MealLogDateTests
    {
        private static DateTime ParseUtc(string iso)
        {
            return DateTime.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        }

        [Test]
        public void AvailableDays_ReturnsTheLastWeek_OldestFirst()
        {
            var now = new DateTime(2026, 10, 23, 18, 45, 0, DateTimeKind.Local);

            IReadOnlyList<DateTime> days = MealLogDate.AvailableDays(now);

            Assert.AreEqual(MealLogDate.DaysBack + 1, days.Count);
            Assert.AreEqual(new DateTime(2026, 10, 17), days[0]);
            Assert.AreEqual(new DateTime(2026, 10, 23), days[days.Count - 1]);
            foreach (DateTime d in days)
            {
                Assert.AreEqual(TimeSpan.Zero, d.TimeOfDay);
            }
        }

        [Test]
        public void BuildTimestamp_Today_IsNow()
        {
            var nowLocal = new DateTime(2026, 10, 23, 18, 45, 12, DateTimeKind.Local);

            string ts = MealLogDate.BuildTimestamp(nowLocal.Date, "BREAKFAST", nowLocal);

            Assert.AreEqual(nowLocal.ToUniversalTime(), ParseUtc(ts));
            StringAssert.EndsWith("Z", ts);
        }

        [TestCase("BREAKFAST", 8, 0)]
        [TestCase("LUNCH", 13, 30)]
        [TestCase("SNACK", 17, 0)]
        [TestCase("DINNER", 20, 30)]
        [TestCase("DRINKS", 12, 0)]
        [TestCase("OTHER", 12, 0)]
        [TestCase(null, 12, 0)]
        public void BuildTimestamp_PastDay_UsesTheTypicalLocalTimeOfTheMeal(string mealType, int hour, int minute)
        {
            var nowLocal = new DateTime(2026, 10, 23, 9, 0, 0, DateTimeKind.Local);
            var day = new DateTime(2026, 10, 21);

            string ts = MealLogDate.BuildTimestamp(day, mealType, nowLocal);

            DateTime expectedLocal = new DateTime(2026, 10, 21, hour, minute, 0, DateTimeKind.Local);
            Assert.AreEqual(expectedLocal.ToUniversalTime(), ParseUtc(ts));
            StringAssert.EndsWith("Z", ts);
        }

        [Test]
        public void BuildTimestamp_FutureDay_FallsBackToNow()
        {
            var nowLocal = new DateTime(2026, 10, 23, 10, 0, 0, DateTimeKind.Local);

            string ts = MealLogDate.BuildTimestamp(nowLocal.Date.AddDays(1), "DINNER", nowLocal);

            Assert.AreEqual(nowLocal.ToUniversalTime(), ParseUtc(ts));
        }

        [Test]
        public void IsToday_ComparesOnlyTheDate()
        {
            var nowLocal = new DateTime(2026, 10, 23, 23, 59, 0, DateTimeKind.Local);

            Assert.IsTrue(MealLogDate.IsToday(new DateTime(2026, 10, 23, 1, 0, 0), nowLocal));
            Assert.IsFalse(MealLogDate.IsToday(new DateTime(2026, 10, 22), nowLocal));
        }
    }
}
