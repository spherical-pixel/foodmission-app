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
        public void Build_DayPicker_SendsOneEventPerDay_DatedAtLocalNoon()
        {
            var answer = new MissionStepAnswer();
            answer.Days.Add(Now.Date.AddDays(-1));
            answer.Days.Add(Now.Date);

            var items = Build("M.A5.4", answer);

            Assert.AreEqual(2, items.Count);
            Assert.IsTrue(items.All(i => i.Event.eventType == ClientEventTypes.FoodWasteFifoOrganized));
            CollectionAssert.AreEqual(
                new[] { Now.Date.AddDays(-1).AddHours(12).ToUniversalTime().ToString("o"), Now.Date.AddHours(12).ToUniversalTime().ToString("o") },
                items.Select(i => i.Event.createdAt).ToArray());
        }

        [Test]
        public void Build_DayPicker_NeverDatesBeforeMissionStartOrAfterNow()
        {
            // Backend rules ignore events before the mission's startedAt
            DateTime start = Now.Date.AddDays(-2).AddHours(14);
            DateTime morning = Now.Date.AddHours(10);
            var answer = new MissionStepAnswer();
            answer.Days.Add(start.Date);
            answer.Days.Add(morning.Date);

            var items = MissionReportBuilder.Build("M.A5.4", MissionInteractionCatalog.Get("M.A5.4").Steps, new[] { answer }, "r1", start, morning);

            CollectionAssert.AreEqual(
                new[] { start.ToUniversalTime().ToString("o"), morning.ToUniversalTime().ToString("o") },
                items.Select(i => i.Event.createdAt).ToArray());
        }

        [Test]
        public void Build_CountSteps_AreNotBackdated()
        {
            var items = Build("M.B2.1", new MissionStepAnswer { Count = 2 });

            Assert.IsTrue(items.All(i => i.Event.createdAt == null));
            StringAssert.DoesNotContain("createdAt", System.Text.Encoding.UTF8.GetString(items[0].Event.ToJsonBody()));
        }

        [Test]
        public void Build_DropsDaysOutsideAllowedWindow()
        {
            var answer = new MissionStepAnswer();
            answer.Days.Add(Now.Date.AddDays(-10));
            answer.Days.Add(Now.Date.AddDays(1));
            answer.Days.Add(Now.Date.AddDays(-4));
            answer.Days.Add(Now.Date.AddDays(-3));

            var items = MissionReportBuilder.Build("M.A5.4", MissionInteractionCatalog.Get("M.A5.4").Steps, new[] { answer }, "r1", Now.AddDays(-3), Now);

            Assert.AreEqual(1, items.Count, "only the mission start day is inside the window");
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

        private static MissionStepAnswer Products(params int[][] products)
        {
            var answer = new MissionStepAnswer();
            foreach (int[] p in products)
            {
                answer.Products.Add(new HashSet<int>(p));
            }
            return answer;
        }

        [Test]
        public void ProductChecklist_SharesOneProductIdPerProduct_AndSkipsIncomplete()
        {
            // M.A2.1 options: 0 origin, 1 certification, 2 production method, 3 seasonal; origin required, min 2
            var items = Build("M.A2.1", Products(new[] { 0, 1 }, new[] { 1, 2 }, new[] { 0, 3 }));

            CollectionAssert.AreEqual(
                new[] { ClientEventTypes.ShoppingOriginChecked, ClientEventTypes.ShoppingCertificationChosen, ClientEventTypes.ShoppingOriginChecked, ClientEventTypes.ShoppingSeasonalChosen },
                items.Select(i => i.Event.eventType).ToArray(), "product 2 has no origin: not sent");
            CollectionAssert.AreEqual(new[] { "manual-r1-p0", "manual-r1-p0", "manual-r1-p2", "manual-r1-p2" }, items.Select(i => Meta(i)[MissionReportBuilder.ProductIdField]).ToArray());
            CollectionAssert.AreEqual(new[] { "mission-report:r1:0", "mission-report:r1:1", "mission-report:r1:2", "mission-report:r1:3" }, items.Select(i => i.Event.idempotencyKey).ToArray());
            Assert.IsNull(items[0].Event.createdAt, "product facts are not back-dated");
        }

        [Test]
        public void ProductChecklist_ArrayMode_SendsOneEventWithLabels()
        {
            var items = Build("M.A2.2", Products(new[] { 3, 0 }));

            var item = items.Single();
            Assert.AreEqual(ClientEventTypes.ShoppingDoubleLabelChosen, item.Event.eventType);
            CollectionAssert.AreEqual(new[] { "EU_ORGANIC", "MSC" }, (string[])Meta(item)["labels"]);
            Assert.AreEqual("manual-r1-p0", Meta(item)[MissionReportBuilder.ProductIdField]);
        }

        [Test]
        public void ProductChecklist_MoreProductsThanMax_SendsOnlyMax()
        {
            var items = Build("M.A2.2", Products(new[] { 0, 1 }, new[] { 2, 3 }));

            Assert.AreEqual(1, items.Count);
        }

        [Test]
        public void DayChecklist_SendsNewColoursPerDayAtNoon()
        {
            var answer = new MissionStepAnswer();
            answer.DayValues[Now.Date.AddDays(-1)] = new HashSet<int> { 1, 0 };
            answer.DayValues[Now.Date.AddDays(-2)] = new HashSet<int>();
            answer.DayValues[Now.Date.AddDays(-9)] = new HashSet<int> { 2 };

            var items = MissionReportBuilder.Build("M.A6.1", MissionInteractionCatalog.Get("M.A6.1").Steps, new[] { answer }, "r1", null, Now);

            var item = items.Single();
            Assert.AreEqual(ClientEventTypes.NutritionRainbowColoursLogged, item.Event.eventType);
            CollectionAssert.AreEqual(new[] { "GREEN", "RED" }, (string[])Meta(item)["colours"]);
            Assert.AreEqual(Now.Date.AddDays(-1).AddHours(12).ToUniversalTime().ToString("o"), item.Event.createdAt);
            Assert.IsFalse(Meta(item).ContainsKey(MissionReportBuilder.ProductIdField));
        }

        [Test]
        public void DayChecklist_StartDay_DatedAtStart()
        {
            DateTime start = Now.Date.AddHours(14);
            var answer = new MissionStepAnswer();
            answer.DayValues[Now.Date] = new HashSet<int> { 0 };
            DateTime now = Now.Date.AddHours(15);

            var items = MissionReportBuilder.Build("M.A6.1", MissionInteractionCatalog.Get("M.A6.1").Steps, new[] { answer }, "r1", start, now);

            Assert.AreEqual(start.ToUniversalTime().ToString("o"), items.Single().Event.createdAt);
        }
    }
}
