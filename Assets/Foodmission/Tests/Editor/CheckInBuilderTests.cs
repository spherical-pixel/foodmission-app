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
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.B1.2", Now.AddDays(-1)), CheckInPlannerTests.M("M.B1.4", Now.AddDays(-1))));
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
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.A1.3", Now)));
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
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.A6.2", Now)));
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
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.B1.1", Now)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["DINNER"] = new HashSet<string> { ClientEventTypes.MealMeatFree, ClientEventTypes.MealMeatConsumed };

            CollectionAssert.AreEqual(new[] { ClientEventTypes.MealMeatFree }, CheckInBuilder.Build(plan, answers, "r1", Now)[0].MealLog.flags);
        }

        [Test]
        public void Build_DayEventsAndMissionSteps_UseRunningIndexes()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(
                CheckInPlannerTests.M("M.A1.3", Now), CheckInPlannerTests.M("M.A5.4", Now.AddDays(-1)), CheckInPlannerTests.M("M.B2.1")));
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
            answers.DayEvents[0].Add(Now.Date.AddDays(-1));
            answers.DayEvents[0].Add(Now.Date.AddDays(-2));
            Assert.AreEqual(0, CheckInBuilder.Build(plan, answers, "r1", Now).Count);

            answers.DayEvents[0].Add(Now.Date);
            Assert.AreEqual(1, CheckInBuilder.Build(plan, answers, "r1", Now).Count);
        }

        [Test]
        public void Build_SwapOnlyMeal_GetsTheImpliedFlag()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.B1.4", Now)));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.SwapBeefToLegumes };

            var log = CheckInBuilder.Build(plan, answers, "r1", Now).Single().MealLog;

            CollectionAssert.AreEqual(new[] { ClientEventTypes.MealLegumeConsumed }, log.flags);
            CollectionAssert.AreEqual(new[] { ClientEventTypes.SwapBeefToLegumes }, log.swaps);
        }

        [Test]
        public void Build_SwapOnlyMealWithoutImpliedFlag_IsSentLast()
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.B6.3", Now), CheckInPlannerTests.M("M.B2.1")));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.SwapSugaryDrinkToWater };
            answers.MissionSteps[0].Count = 1;

            var items = CheckInBuilder.Build(plan, answers, "r1", Now);

            Assert.AreEqual(2, items.Count);
            Assert.IsNotNull(items[0].Event, "the event goes first so it is recorded even if the swaps-only log is rejected");
            Assert.IsNull(items[1].MealLog.flags);
            CollectionAssert.AreEqual(new[] { ClientEventTypes.SwapSugaryDrinkToWater }, items[1].MealLog.swaps);
        }

        [Test]
        public void Build_MealWithFoodWasteFlag_IsSentLast()
        {
            // Backend v0.3.0 rejects FOOD_WASTE_* meal-log flags (assumed flags): they must not block the rest
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(CheckInPlannerTests.M("M.B5.1", Now), CheckInPlannerTests.M("M.A1.3", Now), CheckInPlannerTests.M("M.B2.1")));
            var answers = CheckInAnswers.For(plan);
            answers.MealDays[0].Meals["LUNCH"] = new HashSet<string> { ClientEventTypes.FoodWasteHalfPlateSaved };
            answers.MealDays[0].Meals["DINNER"] = new HashSet<string> { ClientEventTypes.MealLegumeConsumed };
            answers.MissionSteps[0].Count = 1;

            var items = CheckInBuilder.Build(plan, answers, "r1", Now);

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual("DINNER", items[0].MealLog.typeOfMeal);
            Assert.IsNotNull(items[1].Event);
            Assert.AreEqual("LUNCH", items[2].MealLog.typeOfMeal);
        }
    }
}
