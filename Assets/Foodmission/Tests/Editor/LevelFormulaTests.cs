using NUnit.Framework;
using eu.foodmission.platform.Utils;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class LevelFormulaTests
    {
        [Test]
        public void GetLevel_ReturnsExpectedLevels()
        {
            Assert.AreEqual(1, LevelFormula.GetLevel(0));
            Assert.AreEqual(1, LevelFormula.GetLevel(10));
            Assert.AreEqual(1, LevelFormula.GetLevel(19));
            Assert.AreEqual(2, LevelFormula.GetLevel(20));
            Assert.AreEqual(2, LevelFormula.GetLevel(49));
            Assert.AreEqual(3, LevelFormula.GetLevel(50));
            Assert.AreEqual(3, LevelFormula.GetLevel(89));
            Assert.AreEqual(4, LevelFormula.GetLevel(90));
        }

        [Test]
        public void GetTotalXpForLevel_ReturnsAccurateStartPoints()
        {
            Assert.AreEqual(0, LevelFormula.GetTotalXpForLevel(1));
            Assert.AreEqual(20, LevelFormula.GetTotalXpForLevel(2));
            Assert.AreEqual(50, LevelFormula.GetTotalXpForLevel(3));
            Assert.AreEqual(90, LevelFormula.GetTotalXpForLevel(4));
        }

        [Test]
        public void GetXpRequiredForLevel_MatchesFormula()
        {
            Assert.AreEqual(20, LevelFormula.GetXpRequiredForLevel(1));
            Assert.AreEqual(30, LevelFormula.GetXpRequiredForLevel(2));
            Assert.AreEqual(40, LevelFormula.GetXpRequiredForLevel(3));
            Assert.AreEqual(50, LevelFormula.GetXpRequiredForLevel(4));
        }

        [Test]
        public void GetProgressInfo_ReturnsAccurateProgress()
        {
            var infoL1 = LevelFormula.GetProgressInfo(10);
            Assert.AreEqual(1, infoL1.Level);
            Assert.AreEqual(10, infoL1.CurrentXpInLevel);
            Assert.AreEqual(20, infoL1.XpNeededInLevel);
            Assert.AreEqual(0.5f, infoL1.NormalizedProgress, 0.001f);

            var infoL2 = LevelFormula.GetProgressInfo(35);
            Assert.AreEqual(2, infoL2.Level);
            Assert.AreEqual(15, infoL2.CurrentXpInLevel);
            Assert.AreEqual(30, infoL2.XpNeededInLevel);
            Assert.AreEqual(0.5f, infoL2.NormalizedProgress, 0.001f);
        }

        [Test]
        public void CalculateAnimationSteps_WhenSameLevel_ReturnsSingleNonLevelUpStep()
        {
            var steps = LevelFormula.CalculateAnimationSteps(10, 15);
            Assert.AreEqual(1, steps.Count);
            Assert.AreEqual(1, steps[0].Level);
            Assert.AreEqual(0.5f, steps[0].StartProgress, 0.001f);
            Assert.AreEqual(0.75f, steps[0].EndProgress, 0.001f);
            Assert.IsFalse(steps[0].IsLevelUp);
        }

        [Test]
        public void CalculateAnimationSteps_WhenSingleLevelUpWithRemainder_ReturnsTwoSteps()
        {
            var steps = LevelFormula.CalculateAnimationSteps(10, 35);
            Assert.AreEqual(2, steps.Count);

            // Step 1: Finish Level 1
            Assert.AreEqual(1, steps[0].Level);
            Assert.AreEqual(0.5f, steps[0].StartProgress, 0.001f);
            Assert.AreEqual(1.0f, steps[0].EndProgress, 0.001f);
            Assert.IsTrue(steps[0].IsLevelUp);

            // Step 2: Animate partially in Level 2
            Assert.AreEqual(2, steps[1].Level);
            Assert.AreEqual(0.0f, steps[1].StartProgress, 0.001f);
            Assert.AreEqual(0.5f, steps[1].EndProgress, 0.001f);
            Assert.IsFalse(steps[1].IsLevelUp);
        }

        [Test]
        public void CalculateAnimationSteps_WhenExactLevelUp_ReturnsSingleLevelUpStep()
        {
            var steps = LevelFormula.CalculateAnimationSteps(10, 20);
            Assert.AreEqual(1, steps.Count);
            Assert.AreEqual(1, steps[0].Level);
            Assert.AreEqual(0.5f, steps[0].StartProgress, 0.001f);
            Assert.AreEqual(1.0f, steps[0].EndProgress, 0.001f);
            Assert.IsTrue(steps[0].IsLevelUp);
        }

        [Test]
        public void CalculateAnimationSteps_WhenMultiLevelUp_ReturnsAllIntermediateSteps()
        {
            // Level 1: 0..20, Level 2: 20..50, Level 3: 50..90
            // startXp = 10 (L1 at 50%), endXp = 70 (L3 at 20/40 = 50%)
            var steps = LevelFormula.CalculateAnimationSteps(10, 70);
            Assert.AreEqual(3, steps.Count);

            // Step 1: Complete L1
            Assert.AreEqual(1, steps[0].Level);
            Assert.AreEqual(0.5f, steps[0].StartProgress, 0.001f);
            Assert.AreEqual(1.0f, steps[0].EndProgress, 0.001f);
            Assert.IsTrue(steps[0].IsLevelUp);

            // Step 2: Full L2
            Assert.AreEqual(2, steps[1].Level);
            Assert.AreEqual(0.0f, steps[1].StartProgress, 0.001f);
            Assert.AreEqual(1.0f, steps[1].EndProgress, 0.001f);
            Assert.IsTrue(steps[1].IsLevelUp);

            // Step 3: Partial L3
            Assert.AreEqual(3, steps[2].Level);
            Assert.AreEqual(0.0f, steps[2].StartProgress, 0.001f);
            Assert.AreEqual(0.5f, steps[2].EndProgress, 0.001f);
            Assert.IsFalse(steps[2].IsLevelUp);
        }

        [Test]
        public void CalculateAnimationSteps_WhenEndLessThanOrEqualToStart_ReturnsEmpty()
        {
            var stepsEqual = LevelFormula.CalculateAnimationSteps(50, 50);
            Assert.AreEqual(0, stepsEqual.Count);

            var stepsLess = LevelFormula.CalculateAnimationSteps(50, 30);
            Assert.AreEqual(0, stepsLess.Count);
        }
    }
}
