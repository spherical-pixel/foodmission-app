using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class GoalContentFilterTests
    {
        private static readonly Dimension Diet = new Dimension { id = "dim-1", code = DimensionCode.DietChanges };
        private static readonly Dimension Waste = new Dimension { id = "dim-5", code = DimensionCode.FoodWaste };

        [Test]
        public void FromGoals_NoGoals_ShowsEverything()
        {
            foreach (string[] goals in new[] { null, new string[0] })
            {
                var filter = GoalContentFilter.FromGoals(goals);

                Assert.IsTrue(filter.ShowsAll);
                Assert.IsTrue(filter.ShouldShow(Diet, false));
                Assert.IsFalse(filter.HasHiddenContent);
            }
        }

        [Test]
        public void FromGoals_UnknownTopicsOnly_ShowsEverything()
        {
            var filter = GoalContentFilter.FromGoals(new[] { "NOT_A_TOPIC" });

            Assert.IsTrue(filter.ShowsAll);
            Assert.IsTrue(filter.ShouldShow(Diet, false));
        }

        [Test]
        public void ShouldShow_DimensionWantedWhenAnyOfItsTopicsIsAGoal()
        {
            var filter = GoalContentFilter.FromGoals(new[] { TopicCode.PlateWaste.ToLowerInvariant() });

            Assert.IsTrue(filter.ShouldShow(Waste, false));
            Assert.IsFalse(filter.ShouldShow(Diet, false));
            Assert.IsTrue(filter.HasHiddenContent);
            Assert.AreEqual(1, filter.HiddenDimensionCount);
        }

        [Test]
        public void ShouldShow_StartedContentAndUnknownDimension_AlwaysShown()
        {
            var filter = GoalContentFilter.FromGoals(new[] { TopicCode.PlateWaste });

            Assert.IsTrue(filter.ShouldShow(Diet, true));
            Assert.IsTrue(filter.ShouldShow(null, false));
            Assert.IsFalse(filter.HasHiddenContent);
        }

        [Test]
        public void EditGoalsArguments_CarryReturnAction()
        {
            var args = GoalContentFilter.EditGoalsArguments("go_to_quizzes");

            Assert.AreEqual(2, args.Length);
            Assert.AreEqual("returnTo", args[1].name);
            Assert.AreEqual("go_to_quizzes", args[1].value.ToString());
        }
    }
}
