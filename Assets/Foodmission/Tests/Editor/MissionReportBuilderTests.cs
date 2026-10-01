using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionReportBuilderTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 1, 15, 30, 0, DateTimeKind.Local);

        private static List<PendingReportItem> Build(string code, params MissionStepAnswer[] answers) =>
            MissionReportBuilder.Build(code, MissionInteractionCatalog.Get(code).Steps, answers, "r1", null, Now);

        private static Dictionary<string, object> Meta(PendingReportItem item) => (Dictionary<string, object>)item.Event.metadata;

        [Test]
        public void Allowed_WithoutStart_IsLastSevenDaysEndingToday()
        {
            var days = MissionReportDays.Allowed(null, Now);

            Assert.AreEqual(7, days.Count);
            Assert.AreEqual(Now.Date.AddDays(-6), days.First());
            Assert.AreEqual(Now.Date, days.Last());
        }

        [Test]
        public void Allowed_StartsAtMissionStart_WhenMoreRecent()
        {
            var days = MissionReportDays.Allowed(Now.AddDays(-2), Now);

            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-2), Now.Date.AddDays(-1), Now.Date }, days);
        }

        [Test]
        public void Allowed_StartInFuture_ReturnsOnlyToday()
        {
            CollectionAssert.AreEqual(new[] { Now.Date }, MissionReportDays.Allowed(Now.AddDays(3), Now));
        }

        [Test]
        public void TimestampFor_UsesMealHour_AndTodayLaterThanNow_ClampsToNow()
        {
            Assert.AreEqual(Now.Date.AddDays(-1).AddHours(8), MissionReportDays.TimestampFor(Now.Date.AddDays(-1), "BREAKFAST", Now));
            Assert.AreEqual(Now, MissionReportDays.TimestampFor(Now.Date, "DINNER", Now));
        }

        [Test]
        public void Build_Count_SendsNEventsWithDistinctIdsAndStableKeys()
        {
            var items = Build("M.B2.1", new MissionStepAnswer { Count = 3 });

            Assert.AreEqual(3, items.Count);
            Assert.IsTrue(items.All(i => i.Event != null && i.Event.eventType == ClientEventTypes.ShoppingOriginChecked));
            CollectionAssert.AreEqual(new[] { "manual-r1-0", "manual-r1-1", "manual-r1-2" }, items.Select(i => Meta(i)["productId"]).ToArray());
            CollectionAssert.AreEqual(new[] { "mission-report:r1:0", "mission-report:r1:1", "mission-report:r1:2" }, items.Select(i => i.Event.idempotencyKey).ToArray());
            Assert.AreEqual("M.B2.1", Meta(items[0])["missionCode"]);
            Assert.AreEqual(MissionReportBuilder.ReportedVia, Meta(items[0])["reportedVia"]);
        }

        [Test]
        public void Build_Count_IsClampedToMax_AndZeroSendsNothing()
        {
            Assert.AreEqual(5, Build("M.B2.1", new MissionStepAnswer { Count = 99 }).Count);
            Assert.AreEqual(0, Build("M.B2.1", new MissionStepAnswer { Count = 0 }).Count);
        }

        [Test]
        public void Build_FixedMetadata_IsSent()
        {
            var items = Build("M.A3.1", new MissionStepAnswer { Count = 1 });

            Assert.AreEqual("GREEN_SCORE", Meta(items[0])["comparedWith"]);
        }

        [Test]
        public void Build_OptionPicker_SendsOptionEventsWithOptionMetadata()
        {
            var answer = new MissionStepAnswer();
            answer.OptionCounts[0] = 1;
            answer.OptionCounts[2] = 1;

            var items = Build("M.I1.5", answer);

            Assert.AreEqual(2, items.Count);
            Assert.IsTrue(items.All(i => i.Event.eventType == ClientEventTypes.NutritionProteinVarietyLogged));
            CollectionAssert.AreEqual(new[] { "LEGUMES", "EGGS" }, items.Select(i => Meta(i)["proteinSource"]).ToArray());
        }

        [Test]
        public void Build_DayPicker_SendsOnlyToday()
        {
            // Backend (v0.3.1) has no occurredAt: past days can't be dated, so they are dropped
            var answer = new MissionStepAnswer();
            answer.Days.Add(Now.Date.AddDays(-1));
            answer.Days.Add(Now.Date);

            var items = Build("M.A5.4", answer);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(ClientEventTypes.FoodWasteFifoOrganized, items[0].Event.eventType);
        }

        [Test]
        public void Build_DropsDaysOutsideAllowedWindow()
        {
            var answer = new MissionStepAnswer();
            answer.Days.Add(Now.Date.AddDays(-10));
            answer.Days.Add(Now.Date.AddDays(1));
            answer.Days.Add(Now.Date.AddDays(-3));

            var items = MissionReportBuilder.Build("M.A5.4", MissionInteractionCatalog.Get("M.A5.4").Steps, new[] { answer }, "r1", Now.AddDays(-3), Now);

            Assert.AreEqual(0, items.Count);
        }

        [Test]
        public void Build_IgnoresMealReportSteps()
        {
            Assert.AreEqual(0, Build("M.A1.3", new MissionStepAnswer { Count = 3 }).Count);
        }

        [Test]
        public void Build_IndexOffset_ShiftsKeysAndIds()
        {
            var items = MissionReportBuilder.Build("M.B2.1", MissionInteractionCatalog.Get("M.B2.1").Steps,
                new[] { new MissionStepAnswer { Count = 2 } }, "r1", null, Now, 5);

            CollectionAssert.AreEqual(new[] { "mission-report:r1:5", "mission-report:r1:6" }, items.Select(i => i.Event.idempotencyKey).ToArray());
            Assert.AreEqual("manual-r1-6", Meta(items[1])["productId"]);
        }

        [Test]
        public void Build_MultipleSteps_KeepsGlobalIndexes()
        {
            var items = Build("M.A3.3",
                new MissionStepAnswer { Count = 1 },
                new MissionStepAnswer { Count = 1 },
                new MissionStepAnswer { Count = 1 });

            CollectionAssert.AreEqual(new[] { "mission-report:r1:0", "mission-report:r1:1", "mission-report:r1:2" }, items.Select(i => i.Event.idempotencyKey).ToArray());
            Assert.AreEqual("manual-r1-2", Meta(items[2])["productId"]);
        }

        [Test]
        public void Build_YesNo_SendsOnlyWhenConfirmed()
        {
            Assert.AreEqual(1, Build("M.I3.2", new MissionStepAnswer { Confirmed = true }).Count);
            Assert.AreEqual(0, Build("M.I3.2", new MissionStepAnswer { Confirmed = false }).Count);
        }
    }
}
