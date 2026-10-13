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
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-2)));
            inputs.LoggedMealTypes[Now.Date.AddDays(-1)] = new HashSet<string> { "BREAKFAST", "LUNCH", "DINNER" };

            var plan = CheckInPlanner.Plan(inputs);

            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-2), Now.Date }, plan.MealDays.Select(d => d.Day).ToArray());
        }

        [Test]
        public void MealDay_OpenTypes_AreTheUnloggedOnes()
        {
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-1)));
            inputs.LoggedMealTypes[Now.Date.AddDays(-1)] = new HashSet<string> { "LUNCH" };

            var day = CheckInPlanner.Plan(inputs).MealDays.First();

            CollectionAssert.AreEqual(new[] { "BREAKFAST", "SNACK", "DINNER" }, day.OpenMealTypes);
            Assert.IsTrue(day.HasAnyLog);
        }

        [Test]
        public void MealQuestions_AreTheUnionOfActiveMealMissions_PerDayWindow()
        {
            // A1.3 legumes since 2 days ago; B1.4 protein swaps since today
            var plan = CheckInPlanner.Plan(Inputs(M("M.A1.3", Now.Date.AddDays(-2)), M("M.B1.4", Now.Date)));

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
            var plan = CheckInPlanner.Plan(Inputs(M("M.A1.1", Now.Date), M("M.B1.3", Now.Date)));

            Assert.AreEqual(1, plan.MealDays.Single().Questions.Count(q => q.EventType == ClientEventTypes.MealMeatConsumed));
        }

        [TestCase("M.A1.1")]
        [TestCase("M.B1.3")]
        public void MeatCountingMissions_AskMeatFreeAndMeat(string code)
        {
            // Only "had meat?" left users unsure what an unchecked meal meant
            var day = CheckInPlanner.Plan(Inputs(M(code, Now.Date))).MealDays.Single();

            CollectionAssert.AreEquivalent(new[] { ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed },
                day.Questions.Select(q => q.EventType).ToArray());
        }

        [Test]
        public void BreakfastOnlyFlag_AppliesOnlyToBreakfast_UnlessAnotherMissionNeedsItAnywhere()
        {
            var breakfastOnly = CheckInPlanner.Plan(Inputs(M("M.A6.2", Now.Date))).MealDays.Single();
            var q = breakfastOnly.Questions.Single();
            Assert.AreEqual("BREAKFAST", q.OnlyMealType);
            Assert.AreEqual(0, breakfastOnly.QuestionsFor("LUNCH").Count);
            Assert.AreEqual(1, breakfastOnly.QuestionsFor("BREAKFAST").Count);
            Assert.AreEqual("EVENTS_HIGH_FIBRE", q.LabelKey);
            CollectionAssert.AreEqual(new[] { "BREAKFAST" }, breakfastOnly.OpenMealTypes.ToArray(), "meals with nothing to ask aren't offered");
        }

        [TestCase("M.I6.2", 1)]
        [TestCase("M.A6.4", 4)]
        public void MainMealFlags_AreAskedForMainMealsOnly(string code, int flags)
        {
            // Rules count breakfast, lunch and dinner only: a flagged snack would be wasted
            var day = CheckInPlanner.Plan(Inputs(M(code, Now.Date))).MealDays.Single();

            Assert.AreEqual(flags, day.Questions.Count);
            foreach (CheckInMealQuestion q in day.Questions)
            {
                Assert.IsNull(q.OnlyMealType);
                CollectionAssert.AreEquivalent(CheckInMealTypes.Main, q.MealTypes.ToArray());
            }
            Assert.AreEqual(0, day.QuestionsFor("SNACK").Count);
        }

        [Test]
        public void MainMealFlag_SharedWithAnAnyMealMission_IsAskedForEveryMeal()
        {
            // M.I6.4 (healthy fat, any meal) and M.A6.4 (main meals) ask the same flag
            var day = CheckInPlanner.Plan(Inputs(M("M.A6.4", Now.Date), M("M.I6.4", Now.Date))).MealDays.Single();

            var fat = day.Questions.Single(q => q.EventType == ClientEventTypes.NutritionHealthyFatChosen);
            CollectionAssert.AreEquivalent(CheckInMealTypes.All, fat.MealTypes.ToArray());
            Assert.AreEqual(1, day.QuestionsFor("SNACK").Count);
        }

        [Test]
        public void MealDays_StartDay_OffersOnlyMealsDatedAfterTheStart()
        {
            // Rules ignore events before startedAt: a 13:00 lunch can't count for a mission started at 14:00
            var plan = CheckInPlanner.Plan(Inputs(M("M.A1.3", Now.Date.AddDays(-1).AddHours(14))));

            CheckInMealDay startDay = plan.MealDays.Single(d => d.Day == Now.Date.AddDays(-1));
            CollectionAssert.AreEqual(new[] { "SNACK", "DINNER" }, startDay.OpenMealTypes.ToArray());
            Assert.AreEqual(0, startDay.QuestionsFor("LUNCH").Count);
            Assert.AreEqual(1, startDay.QuestionsFor("DINNER").Count);
        }

        [Test]
        public void MealDays_StartDay_KeepsQuestionsOfMissionsStartedEarlier()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.A1.3", Now.Date.AddDays(-1).AddHours(14)), M("M.B1.4", Now.Date.AddDays(-3))));

            CheckInMealDay day = plan.MealDays.Single(d => d.Day == Now.Date.AddDays(-1));
            Assert.AreEqual(CheckInMealTypes.All.Length, day.OpenMealTypes.Count);
            Assert.IsFalse(day.QuestionsFor("LUNCH").Any(q => q.EventType == ClientEventTypes.MealLegumeConsumed), "A1.3 had not started at lunch");
            Assert.IsTrue(day.QuestionsFor("LUNCH").All(q => q.IsSwap));
            Assert.IsTrue(day.QuestionsFor("DINNER").Any(q => q.EventType == ClientEventTypes.MealLegumeConsumed));
        }

        [Test]
        public void MealDays_StartedAfterTheLastMeal_OffersNothingThatDay()
        {
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-1).AddHours(21)));

            var plan = CheckInPlanner.Plan(inputs);

            Assert.IsFalse(plan.MealDays.Any(d => d.Day == Now.Date.AddDays(-1)));
            Assert.AreEqual(0, plan.PendingPastDays(Now));
        }

        [Test]
        public void MissionsPastTheirDeadline_AreNotOffered()
        {
            // Ended at 15:00 today; the backend fails it within the hour, and nothing reported now would count
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-7).AddHours(15)), M("M.A5.4", Now.Date.AddDays(-7).AddHours(15)));
            inputs.NowLocal = Now.Date.AddHours(18);

            Assert.IsTrue(CheckInPlanner.Plan(inputs).IsEmpty);

            inputs.NowLocal = Now.Date.AddHours(14);
            Assert.IsFalse(CheckInPlanner.Plan(inputs).IsEmpty, "still inside the window");
        }

        [Test]
        public void DayEvents_ExcludeCoveredDays_AndRespectMissionStart()
        {
            var inputs = Inputs(M("M.A5.4", Now.AddDays(-3)));
            inputs.CoveredEventDays[ClientEventTypes.FoodWasteFifoOrganized] = new HashSet<DateTime> { Now.Date.AddDays(-2) };

            var plan = CheckInPlanner.Plan(inputs);

            var de = plan.DayEvents.Single();
            Assert.AreEqual("M.A5.4", de.MissionCode);
            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-3), Now.Date.AddDays(-1), Now.Date }, de.OpenDays);
            Assert.AreEqual(Now.AddDays(-3), de.StartLocal);
            Assert.AreEqual(0, plan.MealDays.Count);

            inputs.CoveredEventDays[ClientEventTypes.FoodWasteFifoOrganized].UnionWith(new[] { Now.Date.AddDays(-3), Now.Date.AddDays(-1), Now.Date });
            Assert.AreEqual(0, CheckInPlanner.Plan(inputs).DayEvents.Count, "every day already recorded");
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
            var plan = CheckInPlanner.Plan(Inputs(M("M.Z9.9"), M("M.Z9.9")));

            Assert.IsTrue(plan.IsEmpty);
            Assert.AreEqual(0, plan.StepCount);
        }

        [Test]
        public void PendingPastDays_CountsPastDaysWithoutAnyLog_OrWithOpenDayEvents()
        {
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-3)), M("M.A5.4", Now.AddDays(-1)));
            inputs.LoggedMealTypes[Now.Date.AddDays(-3)] = new HashSet<string> { "LUNCH" }; // partially logged: not "missing"
            inputs.CoveredEventDays[ClientEventTypes.FoodWasteFifoOrganized] = new HashSet<DateTime>();

            var plan = CheckInPlanner.Plan(inputs);

            // -2 and -1 have no meals; FIFO is open on -1 too. Today never counts.
            Assert.AreEqual(2, plan.PendingPastDays(Now));
        }

        [Test]
        public void MealDays_Today_OffersMealTypesFromTwoHoursBeforeTheirTypicalHour()
        {
            var inputs = Inputs(M("M.A1.3", Now.Date.AddDays(-1)));
            inputs.NowLocal = Now.Date.AddHours(10);

            var plan = CheckInPlanner.Plan(inputs);

            CheckInMealDay today = plan.MealDays.Single(d => d.Day == Now.Date);
            CollectionAssert.AreEqual(new[] { "BREAKFAST" }, today.OpenMealTypes.ToArray());
            CheckInMealDay yesterday = plan.MealDays.Single(d => d.Day == Now.Date.AddDays(-1));
            Assert.AreEqual(CheckInMealTypes.All.Length, yesterday.OpenMealTypes.Count, "past days offer every meal type");

            inputs.NowLocal = Now.Date.AddHours(11);
            CollectionAssert.AreEqual(new[] { "BREAKFAST", "LUNCH" }, CheckInPlanner.Plan(inputs).MealDays.Single(d => d.Day == Now.Date).OpenMealTypes.ToArray());
        }

        [Test]
        public void MealDays_TodayWithEveryOfferedMealLogged_IsSkipped()
        {
            var inputs = Inputs(M("M.A1.3"));
            inputs.NowLocal = Now.Date.AddHours(10);
            inputs.LoggedMealTypes[Now.Date] = new HashSet<string> { "BREAKFAST" };

            Assert.IsFalse(CheckInPlanner.Plan(inputs).MealDays.Any(d => d.Day == Now.Date));
        }

        private static void Record(CheckInInputs inputs, DateTime day, params string[] colours)
        {
            if (!inputs.RecordedValues.TryGetValue(ClientEventTypes.NutritionRainbowColoursLogged, out var byDay))
            {
                byDay = new Dictionary<DateTime, HashSet<string>>();
                inputs.RecordedValues[ClientEventTypes.NutritionRainbowColoursLogged] = byDay;
            }
            byDay[day] = new HashSet<string>(colours);
        }

        [Test]
        public void DayChecklist_OpenDaysAreWindowDays_WithRecordedValues()
        {
            var inputs = Inputs(M("M.A6.1", Now.Date.AddDays(-2)));
            Record(inputs, Now.Date.AddDays(-1), "GREEN", "RED");

            var step = CheckInPlanner.Plan(inputs).MissionSteps.Single();

            Assert.AreEqual(MissionStepType.DayChecklist, step.Step.Type);
            CollectionAssert.AreEqual(new[] { Now.Date.AddDays(-2), Now.Date.AddDays(-1), Now.Date }, step.OpenDays.ToArray());
            CollectionAssert.AreEquivalent(new[] { "GREEN", "RED" }, step.RecordedValuesFor(Now.Date.AddDays(-1)).ToArray());
            Assert.AreEqual(0, step.RecordedValuesFor(Now.Date).Count);
            Assert.AreEqual(Now.Date.AddDays(-2), step.StartLocal);
        }

        [Test]
        public void DayChecklist_DaysAlreadyCounting_AreNotOpen()
        {
            var inputs = Inputs(M("M.A6.1", Now.Date.AddDays(-1)));
            Record(inputs, Now.Date.AddDays(-1), "GREEN", "RED", "ORANGE", "YELLOW", "PURPLE");

            var step = CheckInPlanner.Plan(inputs).MissionSteps.Single();

            CollectionAssert.AreEqual(new[] { Now.Date }, step.OpenDays.ToArray());
        }

        [Test]
        public void DayChecklist_NoOpenDay_StepIsSkipped()
        {
            var inputs = Inputs(M("M.A6.1", Now.Date));
            Record(inputs, Now.Date, "GREEN", "RED", "ORANGE", "YELLOW", "PURPLE", "WHITE");

            Assert.IsTrue(CheckInPlanner.Plan(inputs).IsEmpty);
        }

        [Test]
        public void PendingPastDays_CountsDayChecklistDaysWithNothingRecorded()
        {
            var inputs = Inputs(M("M.A6.1", Now.Date.AddDays(-2)));
            Record(inputs, Now.Date.AddDays(-1), "GREEN");

            // -2 has nothing; -1 has something (not "untold"); today doesn't count as past
            Assert.AreEqual(1, CheckInPlanner.Plan(inputs).PendingPastDays(Now));
        }

        [Test]
        public void ProductChecklist_IsMissionStep_AndAnswersStartWithOneEmptyProduct()
        {
            var plan = CheckInPlanner.Plan(Inputs(M("M.A2.1", Now.Date)));

            Assert.AreEqual(MissionStepType.ProductChecklist, plan.MissionSteps.Single().Step.Type);
            Assert.AreEqual(0, plan.MissionSteps.Single().OpenDays.Count);
            var answers = CheckInAnswers.For(plan);
            Assert.AreEqual(1, answers.MissionSteps.Single().Products.Count);
            Assert.AreEqual(0, answers.MissionSteps.Single().Products[0].Count);
        }
    }
}
