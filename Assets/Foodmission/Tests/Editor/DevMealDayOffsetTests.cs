using System;
using System.Globalization;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    public class DevMealDayOffsetTests
    {
        [TearDown]
        public void TearDown()
        {
            DevMealDayOffset.Days = 0;
        }

        [Test]
        public void ApplyTo_ShiftsTheTimestampByTheOffset_WithoutTouchingTheRequest()
        {
            DevMealDayOffset.Days = 2;
            var request = new CreateMealLogRequest { typeOfMeal = "LUNCH", flags = new[] { "MEAL_MEAT_FREE" }, timestamp = "2026-10-01T12:30:00.0000000Z" };

            CreateMealLogRequest shifted = DevMealDayOffset.ApplyTo(request, DateTime.UtcNow);

            Assert.AreEqual(new DateTime(2026, 10, 3, 12, 30, 0, DateTimeKind.Utc), DateTime.Parse(shifted.timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal));
            CollectionAssert.AreEqual(request.flags, shifted.flags);
            Assert.AreEqual("LUNCH", shifted.typeOfMeal);
            Assert.AreEqual("2026-10-01T12:30:00.0000000Z", request.timestamp, "retries must not shift twice");
        }

        [Test]
        public void ApplyTo_RequestWithoutTimestamp_UsesNowPlusOffset()
        {
            DevMealDayOffset.Days = 1;
            var now = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

            CreateMealLogRequest shifted = DevMealDayOffset.ApplyTo(new CreateMealLogRequest { typeOfMeal = "BREAKFAST" }, now);

            Assert.AreEqual(now.AddDays(1), DateTime.Parse(shifted.timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal));
        }

        [Test]
        public void ApplyTo_ZeroOffset_ReturnsTheSameRequest()
        {
            var request = new CreateMealLogRequest { typeOfMeal = "DINNER" };

            Assert.AreSame(request, DevMealDayOffset.ApplyTo(request, DateTime.UtcNow));
        }

        [Test]
        public void Cycle_GoesFromZeroToMaxAndWraps()
        {
            for (int i = 1; i <= DevMealDayOffset.MaxDays; i++)
            {
                DevMealDayOffset.Cycle();
                Assert.AreEqual(i, DevMealDayOffset.Days);
            }
            DevMealDayOffset.Cycle();
            Assert.AreEqual(0, DevMealDayOffset.Days);
        }
    }
}
