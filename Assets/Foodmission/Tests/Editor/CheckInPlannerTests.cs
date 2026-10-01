using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CheckInPlannerTests
    {
        internal static readonly DateTime Now = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Local);

        internal static CheckInMission M(string code, DateTime? start = null) =>
            new CheckInMission(code, "T " + code, MissionInteractionCatalog.Get(code), start);

        internal static CheckInInputs Inputs(params CheckInMission[] missions)
        {
            var inputs = new CheckInInputs { NowLocal = Now };
            inputs.Missions.AddRange(missions);
            return inputs;
        }

        [Test]
        public void MealDays_CoverMissionWindow_AndSkipFullyLoggedDays()
        {
            var inputs = Inputs(M("M.A1.3", Now.AddDays(-2)));
            inputs.LoggedMealTypes[Now.Date.AddDays(-1)] = new HashSet<string> { "BREAKFAST", "LUNCH", "DINNER" };

            var plan = CheckInPlanner.Plan(inputs);

            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-2), Now.Date }, plan.MealDays.Select(d => d.Day).ToArray());
        }

        [Test]
        public void MealDay_OpenTypes_AreTheUnloggedOnes()
        {
            var inputs = Inputs(M("M.A1.3", Now.AddDays(-1)));
            inputs.LoggedMealTypes[Now.Date.AddDays(-1)] = new HashSet<string> { "LUNCH" };

            var day = CheckInPlanner.Plan(inputs).MealDays.First();

            CollectionAssert.AreEqual(new[] { "BREAKFAST", "SNACK", "DINNER" }, day.OpenMealTypes);
            Assert.IsTrue(day.HasAnyLog);
        }

        [Test]
        public void MealQuestions_AreTheUnionOfActiveMealMissions_PerDayWindow()
        {
            // A1.3 legumes since 2 days ago; B1.4 protein swaps since today
            var plan = CheckInPlanner.Plan(Inputs(M("M.A1.3", Now.AddDays(-2)), M("M.B1.4", Now)));

            var older = plan.MealDays.Single(d => d.Day == Now.Date.AddDays(-2));
            CollectionAssert.AreEqual(new[] { ClientEventTypes.MealLegumeConsumed }, older.Questions.Select(q => q.EventType).ToArray());

            var today = plan.MealDays.Single(d => d.Day == Now.Date);
            Assert.AreEqual(1 + 6, today.Questions.Count, "legumes + 6 protein swaps");
            Assert.IsTrue(today.Questions.Where(q => q.IsSwap).All(q => q.LabelKey == q.EventType));
            Assert.AreEqual(ClientEventTypes.MealLegumeConsumed, today.Questions[0].EventType, "flags before swaps");
        }

        [Test]
        public void MealQuestions_AreDeduplicated()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.A1.1", Now), M("M.B1.3", Now)));

            Assert.AreEqual(1, plan.MealDays.Single().Questions.Count(q => q.EventType == ClientEventTypes.MealMeatConsumed));
        }

        [Test]
        public void BreakfastOnlyFlag_AppliesOnlyToBreakfast_UnlessAnotherMissionNeedsItAnywhere()
        {
            var breakfastOnly = CheckInPlanner.Plan(Inputs(M("M.A6.2", Now))).MealDays.Single();
            var q = breakfastOnly.Questions.Single();
            Assert.AreEqual("BREAKFAST", q.OnlyMealType);
            Assert.AreEqual(0, breakfastOnly.QuestionsFor("LUNCH").Count);
            Assert.AreEqual(1, breakfastOnly.QuestionsFor("BREAKFAST").Count);
            Assert.AreEqual("EVENTS_HIGH_FIBRE", q.LabelKey);
        }

        [Test]
        public void DayEvents_ExcludeCoveredDays_AndRespectMissionStart()
        {
            var inputs = Inputs(M("M.A5.4", Now.AddDays(-3)));
            inputs.CoveredEventDays[ClientEventTypes.FoodWasteFifoOrganized] = new HashSet<DateTime> { Now.Date.AddDays(-2) };

            var plan = CheckInPlanner.Plan(inputs);

            var de = plan.DayEvents.Single();
            Assert.AreEqual("M.A5.4", de.MissionCode);
            CollectionAssert.AreEqual(new[] { Now.Date }, de.OpenDays, "no occurredAt in backend v0.3.0: today only");
            Assert.AreEqual(0, plan.MealDays.Count);

            inputs.CoveredEventDays[ClientEventTypes.FoodWasteFifoOrganized].Add(Now.Date);
            Assert.AreEqual(0, CheckInPlanner.Plan(inputs).DayEvents.Count, "today already recorded");
        }

        [Test]
        public void NonMealEventSteps_AreMissionSteps()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.A3.3"), M("M.A5.5")));

            CollectionAssert.AreEqual(new[] { "M.A3.3", "M.A3.3", "M.A3.3", "M.A5.5", "M.A5.5" }, plan.MissionSteps.Select(s => s.MissionCode).ToArray());
            Assert.AreEqual(5, plan.StepCount);
        }

        [Test]
        public void PendingRuleAndUnknownMissions_AreIgnored_AndEmptyPlanIsEmpty()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.A2.1"), M("M.Z9.9")));

            Assert.IsTrue(plan.IsEmpty);
            Assert.AreEqual(0, plan.StepCount);
        }

        [Test]
        public void PendingPastDays_CountsPastDaysWithoutAnyLog_OrWithOpenDayEvents()
        {
            var inputs = Inputs(M("M.A1.3", Now.AddDays(-3)), M("M.A5.4", Now.AddDays(-1)));
            inputs.LoggedMealTypes[Now.Date.AddDays(-3)] = new HashSet<string> { "LUNCH" }; // partially logged: not "missing"
            inputs.CoveredEventDays[ClientEventTypes.FoodWasteFifoOrganized] = new HashSet<DateTime>();

            var plan = CheckInPlanner.Plan(inputs);

            // -2 and -1 have no meals. FIFO is today-only. Today never counts.
            Assert.AreEqual(2, plan.PendingPastDays(Now));
        }
    }
}
