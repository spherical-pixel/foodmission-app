using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CheckInSwapRulesTests
    {
        private const string Snack = ClientEventTypes.SwapSnackToFruitNuts;
        private const string Water = ClientEventTypes.SwapSugaryDrinkToWater;
        private const string BeefToLegumes = ClientEventTypes.SwapBeefToLegumes;

        private static (CheckInPlan Plan, CheckInAnswers Answers) Make(CheckInInputs inputs)
        {
            CheckInPlan plan = CheckInPlanner.Plan(inputs);
            return (plan, CheckInAnswers.For(plan));
        }

        private static (CheckInPlan Plan, CheckInAnswers Answers) Make(params CheckInMission[] missions) =>
            Make(CheckInPlannerTests.Inputs(missions));

        private static CheckInSwapOption Option(CheckInPlan plan, CheckInAnswers answers, int day, string swap) =>
            CheckInSwapRules.Visible(plan, answers, day).FirstOrDefault(o => o.Swap == swap);

        [Test]
        public void Counter_MaxIsRemaining_CappedAtFive()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.B6.5", 0f));
            Assert.AreEqual(5, Option(plan, answers, 0, Snack).Max);
            Assert.IsTrue(Option(plan, answers, 0, Snack).IsCounter);

            (plan, answers) = Make(CheckInPlannerTests.MP("M.B6.5", 0.6f));
            Assert.AreEqual(2, Option(plan, answers, 0, Snack).Max);
        }

        [Test]
        public void Counter_LoggedThatDay_LowersCap()
        {
            CheckInInputs inputs = CheckInPlannerTests.Inputs(CheckInPlannerTests.MP("M.B6.5", 0f));
            CheckInMission mission = inputs.Missions[0];
            for (int i = 0; i < 4; i++)
            {
                inputs.LoggedSwaps.Add((mission.StartLocal.Value.AddHours(1 + i), Snack));
            }
            var (plan, answers) = Make(inputs);

            CheckInSwapOption option = Option(plan, answers, 0, Snack);
            Assert.AreEqual(4, option.Logged);
            Assert.AreEqual(1, option.Max);
        }

        [Test]
        public void Chip_LoggedThatDay_IsLocked()
        {
            CheckInInputs inputs = CheckInPlannerTests.Inputs(CheckInPlannerTests.MP("M.I1.1", 0f));
            inputs.LoggedSwaps.Add((inputs.Missions[0].StartLocal.Value.AddHours(3), BeefToLegumes));
            var (plan, answers) = Make(inputs);

            CheckInSwapOption option = Option(plan, answers, 0, BeefToLegumes);
            Assert.IsFalse(option.IsCounter);
            Assert.AreEqual(1, option.Logged);
            Assert.AreEqual(0, option.Max);
            Assert.AreEqual(1, Option(plan, answers, 1, BeefToLegumes).Max, "only that day");
        }

        [Test]
        public void EarlierDays_ConsumeRemaining()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.I3.3", 0.5f));
            answers.MealDays[0].Swaps[Water] = 2;

            Assert.IsEmpty(CheckInSwapRules.Visible(plan, answers, 1));
            Assert.IsNotEmpty(CheckInSwapRules.Visible(plan, answers, 0), "a day's own answer never hides it");
        }

        [Test]
        public void LevelMission_EarlierDayCoversLevel()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.I1.2", 0f));
            answers.MealDays[0].Swaps[BeefToLegumes] = 1;

            Assert.IsNull(Option(plan, answers, 1, BeefToLegumes));
            Assert.IsNotNull(Option(plan, answers, 1, ClientEventTypes.SwapBeefToPork));
            Assert.IsNotNull(Option(plan, answers, 1, ClientEventTypes.SwapChickenToLegumes));
        }

        [Test]
        public void SharedSwap_VisibleWhileAnyMissionNeedsIt()
        {
            CheckInInputs inputs = CheckInPlannerTests.Inputs(CheckInPlannerTests.MP("M.I1.1", 0f), CheckInPlannerTests.MP("M.I1.2", 0.34f));
            inputs.LoggedSwaps.Add((inputs.Missions[0].StartLocal.Value.AddHours(3), BeefToLegumes));
            var (plan, answers) = Make(inputs);

            Assert.IsNotNull(Option(plan, answers, 2, BeefToLegumes), "I1.1 still counts it");
        }

        [Test]
        public void ClearHidden_DropsAnswersOfHiddenOptions_AndClamps()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.I3.3", 0.5f));
            answers.MealDays[1].Swaps[Water] = 1;
            answers.MealDays[1].NoSwaps = true;
            answers.MealDays[0].Swaps[Snack] = 2;

            Assert.IsTrue(CheckInSwapRules.ClearHidden(plan, answers));
            Assert.IsEmpty(answers.MealDays[1].Swaps);
            Assert.IsFalse(answers.MealDays[1].NoSwaps);

            var (plan2, answers2) = Make(CheckInPlannerTests.MP("M.B6.5", 0.6f));
            answers2.MealDays[0].Swaps[Snack] = 5;
            Assert.IsTrue(CheckInSwapRules.ClearHidden(plan2, answers2));
            Assert.AreEqual(2, answers2.MealDays[0].Swaps[Snack]);
            Assert.IsFalse(CheckInSwapRules.ClearHidden(plan2, answers2), "nothing left to change");
        }

        [Test]
        public void IsAnswered_NoOrAnyUnit()
        {
            var answer = new CheckInMealDayAnswer();
            Assert.IsFalse(CheckInSwapRules.IsAnswered(answer));
            answer.Swaps[Snack] = 0;
            Assert.IsFalse(CheckInSwapRules.IsAnswered(answer));
            answer.Swaps[Snack] = 1;
            Assert.IsTrue(CheckInSwapRules.IsAnswered(answer));
            answer.Swaps.Clear();
            answer.NoSwaps = true;
            Assert.IsTrue(CheckInSwapRules.IsAnswered(answer));
        }
    }
}
