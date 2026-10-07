using System;
using System.Collections.Generic;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    /// <summary>Dev time travel: locally stored dates move back with the backend (advance-day.sh).</summary>
    [TestFixture]
    public class DevTimeShiftTests
    {
        private static readonly DateTime Seen = new DateTime(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Nudged = new DateTime(2026, 10, 4, 18, 30, 0, DateTimeKind.Utc);

        // ── MissionNudgeService ──────────────────────────────

        [Test]
        public void NudgeState_ShiftsEveryDateAndKeepsProgress()
        {
            string json = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "__quest_seen__q1", new { FirstSeenAtUtc = Seen, LastProgress = 0f, LastChangeAtUtc = Seen, LastNudgeAtUtc = (DateTime?)null } },
                { "M.B1.3", new { FirstSeenAtUtc = Seen, LastProgress = 42f, LastChangeAtUtc = Seen, LastNudgeAtUtc = (DateTime?)Nudged } },
            });

            JObject shifted = JObject.Parse(MissionNudgeService.ShiftStoredDates(json, 3));

            Assert.AreEqual(Seen.AddDays(-3), shifted["__quest_seen__q1"]["FirstSeenAtUtc"].ToObject<DateTime>().ToUniversalTime());
            Assert.AreEqual(JTokenType.Null, shifted["__quest_seen__q1"]["LastNudgeAtUtc"].Type);
            Assert.AreEqual(Seen.AddDays(-3), shifted["M.B1.3"]["LastChangeAtUtc"].ToObject<DateTime>().ToUniversalTime());
            Assert.AreEqual(Nudged.AddDays(-3), shifted["M.B1.3"]["LastNudgeAtUtc"].ToObject<DateTime>().ToUniversalTime());
            Assert.AreEqual(42f, shifted["M.B1.3"]["LastProgress"].ToObject<float>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{not json")]
        public void NudgeState_EmptyOrCorrupt_IsReturnedUnchanged(string json)
        {
            Assert.AreEqual(json, MissionNudgeService.ShiftStoredDates(json, 3));
        }

        // ── MissionFailureService ────────────────────────────

        [Test]
        public void FailureAcks_ShiftStartedAtTicks()
        {
            long ticks = Seen.Ticks;
            string json = JsonConvert.SerializeObject(new List<string> { $"M.B1.3|{ticks}", "M.A1.1|0", "broken" });

            List<string> shifted = JsonConvert.DeserializeObject<List<string>>(MissionFailureService.ShiftStoredDates(json, 2));

            CollectionAssert.AreEqual(new[] { $"M.B1.3|{Seen.AddDays(-2).Ticks}", "M.A1.1|0", "broken" }, shifted);
        }

        [Test]
        public void FailureAcks_AfterShift_TheMovedAttemptIsStillAcknowledged()
        {
            var store = new TestStoreService();
            store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "q1" });
            var storage = new TestLocalStorageService();
            var service = new MissionFailureService(store, null, null, storage);
            service.Acknowledge(new MissionProgress { missionCode = "M.B1.3", startedAt = Seen, status = ProgressStatus.Failed });

            string key = MissionFailureService.StorageKey("u1");
            storage.SetValue(key, MissionFailureService.ShiftStoredDates(storage.GetValue<string>(key), 5));

            // The backend moved startedAt back by the same 5 days: acknowledging it again adds nothing
            service.Acknowledge(new MissionProgress { missionCode = "M.B1.3", startedAt = Seen.AddDays(-5), status = ProgressStatus.Failed });

            Assert.AreEqual(1, JsonConvert.DeserializeObject<List<string>>(storage.GetValue<string>(key)).Count);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("[oops")]
        public void FailureAcks_EmptyOrCorrupt_IsReturnedUnchanged(string json)
        {
            Assert.AreEqual(json, MissionFailureService.ShiftStoredDates(json, 2));
        }

        // ── PilotSurveyService ───────────────────────────────

        [Test]
        public void PilotCycle_ShiftsStartAndActiveDays_KeepsTheRest()
        {
            string json = JsonConvert.SerializeObject(new PilotSurveyCycleState
            {
                currentCycle = 2,
                cycleStartDate = "2026-10-01",
                activeDatesInCycle = new List<string> { "2026-10-01", "2026-10-05" },
                completedSlugsInCycle = new List<string> { "pilot-a" },
                skippedSlugsInCycle = new List<string>()
            });

            var shifted = JsonConvert.DeserializeObject<PilotSurveyCycleState>(PilotSurveyService.ShiftStoredDates(json, 7));

            Assert.AreEqual("2026-09-24", shifted.cycleStartDate);
            CollectionAssert.AreEqual(new[] { "2026-09-24", "2026-09-28" }, shifted.activeDatesInCycle);
            Assert.AreEqual(2, shifted.currentCycle);
            CollectionAssert.AreEqual(new[] { "pilot-a" }, shifted.completedSlugsInCycle);
        }

        [Test]
        public void PilotCycle_ShiftsPostponedDays()
        {
            string json = JsonConvert.SerializeObject(new PilotSurveyCycleState
            {
                cycleStartDate = "2026-10-01",
                postponedUntil = new List<PostponedSurvey> { new PostponedSurvey { slug = "third-use", day = "2026-10-08" } }
            });

            var shifted = JsonConvert.DeserializeObject<PilotSurveyCycleState>(PilotSurveyService.ShiftStoredDates(json, 7));

            Assert.AreEqual("2026-10-01", shifted.postponedUntil[0].day);
        }

        [Test]
        public void PilotCycle_EmptyStartDate_StaysEmpty()
        {
            string json = JsonConvert.SerializeObject(new PilotSurveyCycleState { cycleStartDate = "" });

            var shifted = JsonConvert.DeserializeObject<PilotSurveyCycleState>(PilotSurveyService.ShiftStoredDates(json, 7));

            Assert.AreEqual("", shifted.cycleStartDate);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{nope")]
        public void PilotCycle_EmptyOrCorrupt_IsReturnedUnchanged(string json)
        {
            Assert.AreEqual(json, PilotSurveyService.ShiftStoredDates(json, 7));
        }

        // ── Celebration cursor (Home) ────────────────────────

        [Test]
        public void CelebrationCursor_ShiftsIsoTimestamp()
        {
            string ts = Seen.ToString("o", CultureInfo.InvariantCulture);

            string shifted = HomeScreenViewModel.ShiftCelebrationCursor(ts, 4);

            Assert.AreEqual(Seen.AddDays(-4), DateTime.Parse(shifted, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("yesterday")]
        public void CelebrationCursor_EmptyOrInvalid_IsReturnedUnchanged(string ts)
        {
            Assert.AreEqual(ts, HomeScreenViewModel.ShiftCelebrationCursor(ts, 4));
        }
    }
}
