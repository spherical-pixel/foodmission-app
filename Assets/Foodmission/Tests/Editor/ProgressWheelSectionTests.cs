using System.Collections.Generic;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProgressWheelSectionTests
    {
        [TestCase(4, 4, false, true, ProgressWheelSectionState.Grid)]
        [TestCase(4, 0, false, true, ProgressWheelSectionState.AllHidden)]
        [TestCase(0, 0, true, true, ProgressWheelSectionState.Loading)]
        [TestCase(0, 0, false, false, ProgressWheelSectionState.SurveyCta)]
        [TestCase(0, 0, true, false, ProgressWheelSectionState.Loading)]
        [TestCase(0, 0, false, true, ProgressWheelSectionState.Hidden)]
        [TestCase(4, 2, true, true, ProgressWheelSectionState.Grid)]
        public void ResolveState(int total, int visible, bool loading, bool surveyComplete, ProgressWheelSectionState expected)
        {
            Assert.AreEqual(expected, ProgressWheelSection.ResolveState(total, visible, loading, surveyComplete));
        }

        [Test]
        public void Build_FiltersHidden_AndForcesSingleColumnOnLargeScale()
        {
            var state = new AppState
            {
                scale = "large",
                userSegment = "BEGINNER",
                progressWheels = new[] { new ProgressWheel { kind = "CO2_REDUCTION" }, new ProgressWheel { kind = "WATER_SAVINGS" } },
                hiddenProgressWheels = new[] { "WATER_SAVINGS" },
            };

            ProgressWheelSectionModel model = ProgressWheelSection.Build(state, isLoading: false);

            Assert.AreEqual(ProgressWheelSectionState.Grid, model.State);
            Assert.AreEqual(1, model.Visible.Count);
            Assert.AreEqual(2, model.All.Count);
            Assert.IsTrue(model.ForceSingleColumn);
            Assert.AreEqual("BEGINNER", model.Segment);
        }

        [Test]
        public void Signature_ChangesWithWheelValues()
        {
            var a = new AppState { progressWheels = new[] { new ProgressWheel { kind = "CO2_REDUCTION", percentComplete = 10f } } };
            AppState b = a.Copy();
            b.progressWheels[0].percentComplete = 11f;

            Assert.AreNotEqual(ProgressWheelSection.Signature(a), ProgressWheelSection.Signature(b));
        }

        [Test]
        public void Signature_UnchangedForUnrelatedState()
        {
            var a = new AppState { progressWheels = new[] { new ProgressWheel { kind = "CO2_REDUCTION", percentComplete = 10f } } };
            AppState b = a.Copy();
            b.userXp = 999;
            b.userBadges = new[] { "CHEF" };

            Assert.AreEqual(ProgressWheelSection.Signature(a), ProgressWheelSection.Signature(b));
        }

        [Test]
        public void Signature_ChangesWithHiddenSegmentLangAndScale()
        {
            var a = new AppState();
            string baseSig = ProgressWheelSection.Signature(a);

            Assert.AreNotEqual(baseSig, ProgressWheelSection.Signature(new AppState { hiddenProgressWheels = new[] { "X" } }));
            Assert.AreNotEqual(baseSig, ProgressWheelSection.Signature(new AppState { userSegment = "ADVANCED" }));
            Assert.AreNotEqual(baseSig, ProgressWheelSection.Signature(new AppState { lang = "de" }));
            Assert.AreNotEqual(baseSig, ProgressWheelSection.Signature(new AppState { scale = "large" }));
        }

        [Test]
        public void LockedKind_OnlyWhenOneEnabled()
        {
            Assert.AreEqual("CO2_REDUCTION", ProgressWheelSection.LockedKind(new[] { "CO2_REDUCTION" }));
            Assert.IsNull(ProgressWheelSection.LockedKind(new[] { "CO2_REDUCTION", "WATER_SAVINGS" }));
            Assert.IsNull(ProgressWheelSection.LockedKind(new string[0]));
        }
    }
}
