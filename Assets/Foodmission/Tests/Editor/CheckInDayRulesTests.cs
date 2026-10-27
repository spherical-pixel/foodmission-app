using System;
using System.Linq;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CheckInDayRulesTests
    {
        private const int Yes = 0;
        private const int No = 1;

        private static (CheckInPlan Plan, CheckInAnswers Answers) Make(params CheckInMission[] missions)
        {
            var plan = CheckInPlanner.Plan(CheckInPlannerTests.Inputs(missions));
            return (plan, CheckInAnswers.For(plan));
        }

        private static bool Asks(CheckInPlan plan, CheckInAnswers answers, int day, string code) =>
            CheckInDayRules.Visible(plan, answers, day).Any(q => q.MissionCode == code);

        [Test]
        public void Visible_HidesQuestionOnceEarlierDaysCoverRemaining()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.I1.3", 0f));
            answers.MealDays[0].DayAnswers["M.I1.3"] = No;
            answers.MealDays[1].DayAnswers["M.I1.3"] = No;

            Assert.IsFalse(Asks(plan, answers, 2, "M.I1.3"));
            Assert.IsTrue(Asks(plan, answers, 1, "M.I1.3"), "a day's own answer never hides it");
        }

        [Test]
        public void Visible_NonCountingAnswersDoNotPrune()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.I1.3", 0f));
            answers.MealDays[0].DayAnswers["M.I1.3"] = Yes;
            answers.MealDays[1].DayAnswers["M.I1.3"] = No;

            Assert.IsTrue(Asks(plan, answers, 2, "M.I1.3"));
        }

        [Test]
        public void Visible_LimitQuestion_AlwaysVisible()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.A6.3", 0f));
            for (int i = 0; i < 3; i++)
            {
                answers.MealDays[i].DayAnswers["M.A6.3"] = Yes;
            }

            Assert.IsTrue(Asks(plan, answers, 3, "M.A6.3"));
        }

        [Test]
        public void ClearHidden_RemovesAnswersOfQuestionsNoLongerNeeded()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.I1.3", 0f));
            answers.MealDays[2].DayAnswers["M.I1.3"] = No;
            answers.MealDays[0].DayAnswers["M.I1.3"] = No;
            answers.MealDays[1].DayAnswers["M.I1.3"] = No;

            Assert.IsTrue(CheckInDayRules.ClearHidden(plan, answers));
            Assert.IsFalse(answers.MealDays[2].DayAnswers.ContainsKey("M.I1.3"));
            Assert.IsFalse(CheckInDayRules.ClearHidden(plan, answers));
        }

        [Test]
        public void Visible_DerivedMeatQuestion_HiddenOnceAMealWithMeatChipIsChosen()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.I1.3", 0f), CheckInPlannerTests.MP("M.B1.3", 0f));
            Assert.IsTrue(Asks(plan, answers, 0, "M.I1.3"));

            answers.MealDays[0].Meals["LUNCH"] = new System.Collections.Generic.HashSet<string>();

            Assert.IsFalse(Asks(plan, answers, 0, "M.I1.3"));
        }

        [Test]
        public void MissingRequired_UnansweredMeatDay_RequiredWhenDaySendsSomething()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.I1.3", 0f), CheckInPlannerTests.MP("M.B6.2", 0f));
            Assert.IsEmpty(CheckInDayRules.MissingRequired(plan, answers, 0));

            answers.MealDays[0].DayAnswers["M.B6.2"] = Yes;

            Assert.AreEqual("M.I1.3", CheckInDayRules.MissingRequired(plan, answers, 0).Single().MissionCode);
        }

        [Test]
        public void MissingRequired_OnlyWhenDaySendsSomething()
        {
            var (plan, answers) = Make(CheckInPlannerTests.MP("M.A6.3", 0f), CheckInPlannerTests.MP("M.B6.2", 0f));

            Assert.IsEmpty(CheckInDayRules.MissingRequired(plan, answers, 0));

            answers.MealDays[0].DayAnswers["M.B6.2"] = Yes;
            Assert.AreEqual("M.A6.3", CheckInDayRules.MissingRequired(plan, answers, 0).Single().MissionCode);

            answers.MealDays[0].DayAnswers["M.A6.3"] = No;
            Assert.IsEmpty(CheckInDayRules.MissingRequired(plan, answers, 0));
        }
    }
}
