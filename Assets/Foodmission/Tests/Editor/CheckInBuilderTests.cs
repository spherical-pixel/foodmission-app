using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CheckInBuilderTests
    {
        private static readonly DateTime Now = CheckInPlannerTests.Now;

        [Test]
        public void Build_GroupsAllMissionFlagsAndSwapsInOneMealLog()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.B1.2", Now.Date.AddDays(-1)), CheckInPlannerTests.M("M.B1.4", Now.Date.AddDays(-1))));
            var answers = CheckInAnswers.For(plan);
            int yesterday = plan.MealDays.FindIndex(d => d.Day == Now.Date.AddDays(-1));
            answers.MealDays[yesterday].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.MealMeatFree, ClientEventTypes.SwapBeefToLegumes };

            var items = CheckInBuilder.Build(plan, answers, "r1", Now);

            Assert.AreEqual(1, items.Count);
            var log = items[0].MealLog;
            Assert.AreEqual("LUNCH", log.typeOfMeal);
            CollectionAssert.AreEqual(new[] { ClientEventTypes.MealMeatFree }, log.flags);
            CollectionAssert.AreEqual(new[] { ClientEventTypes.SwapBeefToLegumes }, log.swaps);
            Assert.AreEqual(Now.Date.AddDays(-1).AddHours(13).ToUniversalTime().ToString("o"), log.timestamp);
        }

        [Test]
        public void Build_OneLogPerChosenMeal_AndSkipsMealsWithNothingChecked()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.A1.3", Now.Date)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.MealLegumeConsumed };
            answers.MealDays[0].Meals["DINNER"] = new HashSet<string> { ClientEventTypes.MealLegumeConsumed };
            answers.MealDays[0].Meals["BREAKFAST"] = new HashSet<string>();

            var items = CheckInBuilder.Build(plan, answers, "r1", Now);

            CollectionAssert.AreEqual(new[] { "LUNCH", "DINNER" }, items.Select(i => i.MealLog.typeOfMeal).ToArray());
        }

        [Test]
        public void Build_DropsQuestionsNotApplicableToTheMealOrNotInPlan()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.A6.2", Now.Date)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.NutritionHighFibreMeal };
            answers.MealDays[0].Meals["BREAKFAST"] = new HashSet<string> { ClientEventTypes.NutritionHighFibreMeal, ClientEventTypes.MealVegan };

            var items = CheckInBuilder.Build(plan, answers, "r1", Now);

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("BREAKFAST", items[0].MealLog.typeOfMeal);
            CollectionAssert.AreEqual(new[] { ClientEventTypes.NutritionHighFibreMeal }, items[0].MealLog.flags);
        }

        [Test]
        public void Build_MeatFreeWinsOverMeatConsumedInTheSameMeal()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.B1.1", Now.Date)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["DINNER"] = new HashSet<string> { ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed };

            CollectionAssert.AreEqual(new[] { ClientEventTypes.MealMeatFree }, CheckInBuilder.Build(plan, answers, "r1", Now)[0].MealLog.flags);
        }

        [Test]
        public void Build_DayEventsAndMissionSteps_UseRunningIndexes()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(
                CheckInPlannerTests.M("M.A1.3", Now.Date), CheckInPlannerTests.M("M.A5.4", Now.AddDays(-1)), CheckInPlannerTests.M("M.B2.1")));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[plan.MealDays.Count - 1].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.MealLegumeConsumed };
            answers.DayEvents[0].Add(Now.Date);
            answers.MissionSteps[0].Count = 2;

            var items = CheckInBuilder.Build(plan, answers, "r1", Now);

            Assert.AreEqual(4, items.Count);
            Assert.IsNotNull(items[0].MealLog);
            Assert.AreEqual(ClientEventTypes.FoodWasteFifoOrganized, items[1].Event.eventType);
            CollectionAssert.AreEqual(new[] { "mission-report:r1:1", "mission-report:r1:2", "mission-report:r1:3" },
                items.Skip(1).Select(i => i.Event.idempotencyKey).ToArray());
        }

        [Test]
        public void Build_DayEvent_IgnoresDaysThatAreNotOpen()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.A5.4", Now.AddDays(-2))));
            var answers = CheckInAnswers.For(plan);
            answers.DayEvents[0].Add(Now.Date.AddDays(-3));
            answers.DayEvents[0].Add(Now.Date.AddDays(1));
            Assert.AreEqual(0, CheckInBuilder.Build(plan, answers, "r1", Now).Count);

            answers.DayEvents[0].Add(Now.Date.AddDays(-1));
            Assert.AreEqual(1, CheckInBuilder.Build(plan, answers, "r1", Now).Count);
        }

        [Test]
        public void Build_DayEvent_OnMissionStartDay_IsDatedAtTheStart()
        {
            // Started at 18:00, after the noon a past day is dated at
            DateTime start = Now.AddDays(-2);
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.A5.4", start)));
            var answers = CheckInAnswers.For(plan);
            answers.DayEvents[0].Add(start.Date);

            var item = CheckInBuilder.Build(plan, answers, "r1", Now).Single();

            Assert.AreEqual(start.ToUniversalTime().ToString("o"), item.Event.createdAt);
        }

        [Test]
        public void Build_SwapOnlyMeal_IsSentWithSwapsOnly()
        {
            // Next to a per-meal mission (A1.3) the protein swaps stay per meal
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.A1.3", Now.Date), CheckInPlannerTests.M("M.B1.4", Now.Date)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.SwapBeefToChicken };

            var log = CheckInBuilder.Build(plan, answers, "r1", Now).Single().MealLog;

            Assert.IsNull(log.flags, "no implied flags: backend v0.3.1 accepts swaps-only meal logs");
            CollectionAssert.AreEqual(new[] { ClientEventTypes.SwapBeefToChicken }, log.swaps);
        }

        [Test]
        public void Build_MealLogsKeepDayAndMealOrder_BeforeEvents()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.B5.1", Now.Date), CheckInPlannerTests.M("M.B6.3", Now.Date), CheckInPlannerTests.M("M.B2.1")));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.FoodWasteHalfPlateSaved };
            answers.MealDays[0].Meals["DINNER"] = new HashSet<string> { ClientEventTypes.SwapSugaryDrinkToWater };
            answers.MissionSteps[0].Count = 1;

            var items = CheckInBuilder.Build(plan, answers, "r1", Now);

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual("LUNCH", items[0].MealLog.typeOfMeal);
            Assert.AreEqual("DINNER", items[1].MealLog.typeOfMeal);
            Assert.IsNotNull(items[2].Event);
        }

        [Test]
        public void Build_ChosenMealWithMeatQuestionUnchecked_IsSentAsMeatFree()
        {
            // The user saw "had meat?" for this meal and left it unchecked: the meal had no meat
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.B1.3", Now.Date)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string>();

            var log = CheckInBuilder.Build(plan, answers, "r1", Now).Single().MealLog;

            CollectionAssert.AreEqual(new[] { ClientEventTypes.MealMeatFree }, log.flags);
        }

        [Test]
        public void Build_ChosenMealWithoutMeatQuestionAndNothingChecked_IsNotSent()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.A1.3", Now.Date)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string>();

            Assert.AreEqual(0, CheckInBuilder.Build(plan, answers, "r1", Now).Count);
            Assert.IsFalse(CheckInBuilder.CanSendMeal(plan.MealDays[0], "LUNCH", answers.MealDays[0].Meals["LUNCH"]));
            Assert.IsTrue(CheckInBuilder.CanSendMeal(plan.MealDays[0], "LUNCH", new HashSet<string> { ClientEventTypes.MealLegumeConsumed }));
        }

        [Test]
        public void Build_DayChecklist_UsesMissionStart()
        {
            DateTime now = CheckInPlannerTests.Now;
            DateTime start = now.Date.AddHours(14);
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.A6.1", start)));
            var answers = CheckInAnswers.For(plan);
            answers.MissionSteps[0].DayValues[now.Date] = new HashSet<int> { 0 };

            var items = CheckInBuilder.Build(plan, answers, "r1", now);

            Assert.AreEqual(start.ToUniversalTime().ToString("o"), items.Single().Event.createdAt);
        }

        [Test]
        public void Build_SwapThatEndsInMeat_NeverInfersMeatFree()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.B1.3", Now.Date), CheckInPlannerTests.M("M.B1.5", Now.Date)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.SwapBeefToChicken, ClientEventTypes.MealMeatFree };

            var log = CheckInBuilder.Build(plan, answers, "r1", Now).Single().MealLog;

            CollectionAssert.DoesNotContain(log.flags, ClientEventTypes.MealMeatFree);
            CollectionAssert.Contains(log.flags, ClientEventTypes.MealMeatConsumed);
        }

        // ── Day questions (spec 2026-10-27) ───────────────────

        private static (CheckInPlan Plan, CheckInAnswers Answers) DayPlan(params CheckInMission[] missions)
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(missions));
            return (plan, CheckInAnswers.For(plan));
        }

        [Test]
        public void Build_DayAnswers_OneOtherLogPerDay()
        {
            var (plan, answers) = DayPlan(CheckInPlannerTests.MP("M.B6.2", 0f), CheckInPlannerTests.MP("M.B5.5", 0f));
            int day = plan.MealDays.FindIndex(d => d.Day == Now.Date.AddDays(-2));
            answers.MealDays[day].DayAnswers["M.B6.2"] = 0;
            answers.MealDays[day].DayAnswers["M.B5.5"] = 0;

            var log = CheckInBuilder.Build(plan, answers, "r1", Now).Single().MealLog;

            Assert.AreEqual("OTHER", log.typeOfMeal);
            CollectionAssert.AreEquivalent(new[] { ClientEventTypes.NutritionFruitVegServingAdded, ClientEventTypes.FoodWasteHalfPlateSaved }, log.flags);
            Assert.IsNull(log.swaps);
            Assert.AreEqual(Now.Date.AddDays(-2).AddHours(12).ToUniversalTime().ToString("o"), log.timestamp);
        }

        [Test]
        public void Build_DayAnswersAllNo_NoLog()
        {
            var (plan, answers) = DayPlan(CheckInPlannerTests.MP("M.B6.2", 0f));
            answers.MealDays[0].DayAnswers["M.B6.2"] = 1;

            Assert.IsEmpty(CheckInBuilder.Build(plan, answers, "r1", Now));
        }

        [Test]
        public void Build_MeatFreeDay_SendsMealMeatFree()
        {
            var (plan, answers) = DayPlan(CheckInPlannerTests.MP("M.I1.3", 0f));
            answers.MealDays[0].DayAnswers["M.I1.3"] = 1;

            CollectionAssert.AreEqual(new[] { ClientEventTypes.MealMeatFree }, CheckInBuilder.Build(plan, answers, "r1", Now).Single().MealLog.flags);
        }

        [Test]
        public void Build_DayLogOnStartDay_NotBeforeStart()
        {
            DateTime start = Now.Date.AddDays(-3).AddHours(15);
            var (plan, answers) = DayPlan(CheckInPlannerTests.MP("M.B6.2", 0f, start));
            answers.MealDays[0].DayAnswers["M.B6.2"] = 0;

            Assert.AreEqual(start.ToUniversalTime().ToString("o"), CheckInBuilder.Build(plan, answers, "r1", Now).Single().MealLog.timestamp);
        }

        [Test]
        public void Build_TodayMorning_DatedNow()
        {
            DateTime morning = Now.Date.AddHours(9.5);
            var inputs = CheckInPlannerTests.Inputs(CheckInPlannerTests.MP("M.B6.2", 0f));
            inputs.NowLocal = morning;
            var plan = CheckInPlanner.Plan(inputs);
            var answers = CheckInAnswers.For(plan);
            int today = plan.MealDays.FindIndex(d => d.Day == Now.Date);
            answers.MealDays[today].DayAnswers["M.B6.2"] = 0;

            Assert.AreEqual(morning.ToUniversalTime().ToString("o"), CheckInBuilder.Build(plan, answers, "r1", morning).Single().MealLog.timestamp);
        }

        [Test]
        public void Build_AllMealsYes_FlagsEveryMealOfThatDay()
        {
            var (plan, answers) = DayPlan(CheckInPlannerTests.MP("M.A6.3", 0f), CheckInPlannerTests.MP("M.A1.3", 0f));
            int day = plan.MealDays.FindIndex(d => d.Day == Now.Date.AddDays(-1));
            answers.MealDays[day].DayAnswers["M.A6.3"] = 0;
            answers.MealDays[day].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.MealLegumeConsumed };

            var logs = CheckInBuilder.Build(plan, answers, "r1", Now).Select(i => i.MealLog).ToList();

            CollectionAssert.AreEqual(new[] { "LUNCH", "OTHER" }, logs.Select(l => l.typeOfMeal).ToArray());
            Assert.IsTrue(logs.All(l => l.flags.Contains(ClientEventTypes.NutritionAddedSugarAvoided)));
            CollectionAssert.Contains(logs[0].flags, ClientEventTypes.MealLegumeConsumed);
        }

        [Test]
        public void Build_MealLevelQuestionsNeverUseOther()
        {
            var (plan, answers) = DayPlan(CheckInPlannerTests.MP("M.B1.3", 0f));
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.MealMeatConsumed };

            var items = CheckInBuilder.Build(plan, answers, "r1", Now);

            Assert.IsTrue(items.All(i => i.MealLog.typeOfMeal != "OTHER"));
        }

        [Test]
        public void Build_AnswerOfQuestionNotAskedThatDay_IsIgnored()
        {
            var (plan, answers) = DayPlan(CheckInPlannerTests.MP("M.B6.2", 0f));
            answers.MealDays[0].DayAnswers["M.Z9.9"] = 0;

            Assert.IsEmpty(CheckInBuilder.Build(plan, answers, "r1", Now));
        }

        // ── Day swaps (spec 2026-10-27 swaps §7) ──────────────

        private static List<CreateMealLogRequest> OtherLogs(List<PendingReportItem> items) =>
            items.Select(i => i.MealLog).Where(l => l != null && l.typeOfMeal == CheckInBuilder.OtherMealType).ToList();

        [Test]
        public void DaySwaps_GoInTheDayOtherLog()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.MP("M.I1.1", 0f), CheckInPlannerTests.MP("M.B6.2", 0f)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Swaps[ClientEventTypes.SwapBeefToLegumes] = 1;
            answers.MealDays[0].DayAnswers["M.B6.2"] = 0;

            CreateMealLogRequest log = OtherLogs(CheckInBuilder.Build(plan, answers, "r1", Now)).Single();

            CollectionAssert.AreEqual(new[] { ClientEventTypes.NutritionFruitVegServingAdded }, log.flags);
            CollectionAssert.AreEqual(new[] { ClientEventTypes.SwapBeefToLegumes }, log.swaps);
        }

        [Test]
        public void DaySwaps_Only_SendFlagsNull()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.MP("M.I1.1", 0f)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[1].Swaps[ClientEventTypes.SwapPorkToLegumes] = 1;

            List<PendingReportItem> items = CheckInBuilder.Build(plan, answers, "r1", Now);

            CreateMealLogRequest log = items.Single().MealLog;
            Assert.AreEqual(CheckInBuilder.OtherMealType, log.typeOfMeal);
            Assert.IsNull(log.flags);
            CollectionAssert.AreEqual(new[] { ClientEventTypes.SwapPorkToLegumes }, log.swaps);
            Assert.AreEqual(MissionReportDays.EventTimestampFor(plan.MealDays[1].Day, plan.MealDays[1].SwapQuestion.Missions[0].StartLocal, Now)
                .ToUniversalTime().ToString("o"), log.timestamp);
        }

        [Test]
        public void Counter_ExtraUnits_AreExtraOtherLogs()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.MP("M.B6.5", 0f)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Swaps[ClientEventTypes.SwapSnackToFruitNuts] = 3;

            List<CreateMealLogRequest> logs = OtherLogs(CheckInBuilder.Build(plan, answers, "r1", Now));

            Assert.AreEqual(3, logs.Count);
            Assert.IsTrue(logs.All(l => l.swaps.SequenceEqual(new[] { ClientEventTypes.SwapSnackToFruitNuts }) && l.flags == null));
            Assert.AreEqual(1, logs.Select(l => l.timestamp).Distinct().Count());
        }

        [Test]
        public void NoSwaps_SendsNothing()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.MP("M.B6.5", 0f)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].NoSwaps = true;

            Assert.IsEmpty(CheckInBuilder.Build(plan, answers, "r1", Now));
        }

        [Test]
        public void HiddenSwapAnswers_AreNotSent()
        {
            // I3.3 needs 2: two waters on day 0 hide day 1's question
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.MP("M.I3.3", 0.5f)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Swaps[ClientEventTypes.SwapSugaryDrinkToWater] = 2;
            answers.MealDays[1].Swaps[ClientEventTypes.SwapSugaryCerealToOats] = 1;

            List<CreateMealLogRequest> logs = OtherLogs(CheckInBuilder.Build(plan, answers, "r1", Now));

            Assert.AreEqual(2, logs.Count, "day 0: the day log + one extra water");
            Assert.IsFalse(logs.Any(l => l.swaps.Contains(ClientEventTypes.SwapSugaryCerealToOats)));
        }
    }
}
