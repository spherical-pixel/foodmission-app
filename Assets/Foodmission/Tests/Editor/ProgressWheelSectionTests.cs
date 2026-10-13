using System.Collections.Generic;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProgressWheelSectionTests
    {
        [TestCase(4, 4, false, ProgressWheelSectionState.Grid)]
        [TestCase(4, 0, false, ProgressWheelSectionState.AllHidden)]
        [TestCase(0, 0, true, ProgressWheelSectionState.Loading)]
        [TestCase(0, 0, false, ProgressWheelSectionState.Hidden)]
        [TestCase(4, 2, true, ProgressWheelSectionState.Grid)]
        public void ResolveState(int total, int visible, bool loading, ProgressWheelSectionState expected)
        {
            Assert.AreEqual(expected, ProgressWheelSection.ResolveState(total, visible, loading));
        }

        [Test]
        public void Build_NoWheels_IsHidden_EvenWithoutSurvey()
        {
            Assert.AreEqual(ProgressWheelSectionState.Hidden, ProgressWheelSection.Build(new AppState(), false).State);
        }

        [Test]
        public void Build_SegmentComesFromTheWheels()
        {
            var state = new AppState
            {
                userSegment = "BEGINNER",
                progressWheels = new[] { new ProgressWheel { kind = "CO2_REDUCTION", profile = "INTERMEDIATE" } },
            };

            Assert.AreEqual("INTERMEDIATE", ProgressWheelSection.Build(state, false).Segment);
            Assert.AreEqual("", ProgressWheelSection.Build(new AppState { userSegment = "ADVANCED" }, false).Segment);
        }

        [Test]
        public void Build_FiltersHidden_AndForcesSingleColumnOnLargeScale()
        {
            var state = new AppState
            {
                scale = "large",
                progressWheels = new[]
                {
                    new ProgressWheel { kind = "CO2_REDUCTION", profile = "BEGINNER" },
                    new ProgressWheel { kind = "WATER_SAVINGS", profile = "BEGINNER" },
                },
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
        public void Signature_ChangesWithHiddenLangAndScale()
        {
            var a = new AppState();
            string baseSig = ProgressWheelSection.Signature(a);

            Assert.AreNotEqual(baseSig, ProgressWheelSection.Signature(new AppState { hiddenProgressWheels = new[] { "X" } }));
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

        [Test]
        public void Signature_ChangesWithBackendTexts()
        {
            var a = new AppState { progressWheels = new[] { new ProgressWheel { kind = "NEW_KIND", label = "New", unit = "kg", stageTitle = "One" } } };
            string baseSig = ProgressWheelSection.Signature(a);

            foreach (System.Action<ProgressWheel> change in new System.Action<ProgressWheel>[] { w => w.label = "Other", w => w.unit = "t", w => w.stageTitle = "Two" })
            {
                AppState b = a.Copy();
                change(b.progressWheels[0]);
                Assert.AreNotEqual(baseSig, ProgressWheelSection.Signature(b));
            }
        }

        [Test]
        public void CustomizableKinds_SkipsMissingAndDuplicateKinds()
        {
            var wheels = new[]
            {
                new ProgressWheel { kind = "CO2_REDUCTION" },
                new ProgressWheel { kind = null },
                null,
                new ProgressWheel { kind = "" },
                new ProgressWheel { kind = "CO2_REDUCTION" },
                new ProgressWheel { kind = "WATER_SAVINGS" },
            };

            IReadOnlyList<ProgressWheel> result = ProgressWheelSection.CustomizableWheels(wheels);

            CollectionAssert.AreEqual(new[] { "CO2_REDUCTION", "WATER_SAVINGS" }, System.Linq.Enumerable.Select(result, w => w.kind));
        }
    }
}
