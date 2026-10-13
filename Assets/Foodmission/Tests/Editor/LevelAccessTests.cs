using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class LevelAccessTests
    {
        private bool _originalDevUnlocks;

        [SetUp]
        public void SetUp()
        {
            _originalDevUnlocks = DevUnlocks.All;
            DevUnlocks.All = false;
        }

        [TearDown]
        public void TearDown()
        {
            DevUnlocks.All = _originalDevUnlocks;
        }

        [TestCase("BEGINNER", "BEGINNER", false)]
        [TestCase("INTERMEDIATE", "BEGINNER", true)]
        [TestCase("ADVANCED", "INTERMEDIATE", true)]
        [TestCase("BEGINNER", "ADVANCED", false)]
        [TestCase("intermediate", "INTERMEDIATE", false)]
        public void IsLockedByLevel_ComparesRanks(string item, string user, bool expected)
        {
            Assert.AreEqual(expected, LevelAccess.IsLockedByLevel(item, user, started: false));
        }

        [Test]
        public void IsLockedByLevel_StartedContent_IsNeverLocked()
        {
            Assert.IsFalse(LevelAccess.IsLockedByLevel(ContentLevel.Advanced, ContentLevel.Beginner, started: true));
        }

        [TestCase(null, "BEGINNER")]
        [TestCase("", "BEGINNER")]
        [TestCase("EXPERT", "BEGINNER")]
        [TestCase("ADVANCED", null)]
        [TestCase("ADVANCED", "UNKNOWN")]
        public void IsLockedByLevel_UnknownValues_FailOpen(string item, string user)
        {
            Assert.IsFalse(LevelAccess.IsLockedByLevel(item, user, started: false));
        }

        [Test]
        public void LevelGate_LocksAboveUserLevel_AndDescribesTheLock()
        {
            var dims = new Mock<IDimensionService>();
            dims.Setup(d => d.GetDimension("dim-1")).Returns(new Dimension { id = "dim-1", code = DimensionCode.FoodWaste, name = "Food waste" });
            var state = new AppState
            {
                dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.FoodWaste, ContentLevel.Intermediate) }
            };
            var gate = new LevelGate(state, dims.Object);

            Dimension dim = gate.DimensionById("dim-1");
            LevelLock levelLock = gate.GetLock(dim, ContentLevel.Advanced, started: false);

            Assert.IsNotNull(levelLock);
            Assert.AreEqual(ContentLevel.Advanced, levelLock.ItemLevel);
            Assert.AreEqual(ContentLevel.Intermediate, levelLock.UserLevel);
            Assert.AreEqual(DimensionCode.FoodWaste, levelLock.DimensionCode);
            Assert.AreEqual("Food waste", levelLock.DimensionName);
            Assert.IsNull(gate.GetLock(dim, ContentLevel.Intermediate, started: false));
        }

        [Test]
        public void LevelGate_UnknownDimension_NeverLocks()
        {
            var dims = new Mock<IDimensionService>();
            var gate = new LevelGate(new AppState(), dims.Object);

            Assert.IsNull(gate.DimensionById("missing"));
            Assert.IsNull(gate.GetLock(null, ContentLevel.Advanced, started: false));
            Assert.IsNull(new LevelGate(new AppState(), null).GetLock(gate.DimensionForTopic("t"), ContentLevel.Advanced, false));
        }

        [Test]
        public void LevelGate_DevUnlocks_DisablesLocks()
        {
            DevUnlocks.All = true;
            var gate = new LevelGate(new AppState(), null);
            var dim = new Dimension { id = "d", code = DimensionCode.Packaging, name = "Packaging" };

            Assert.IsNull(gate.GetLock(dim, ContentLevel.Advanced, started: false));
        }

        // ── Previous levels hidden in the lists (2026-10-12) ─────────────────

        [TestCase("BEGINNER", "ADVANCED", true)]
        [TestCase("INTERMEDIATE", "ADVANCED", true)]
        [TestCase("BEGINNER", "INTERMEDIATE", true)]
        [TestCase("ADVANCED", "ADVANCED", false)]
        [TestCase("ADVANCED", "BEGINNER", false)]
        [TestCase("beginner", "INTERMEDIATE", true)]
        public void IsBelowUserLevel_ComparesRanks(string item, string user, bool expected)
        {
            Assert.AreEqual(expected, LevelAccess.IsBelowUserLevel(item, user, inProgress: false));
        }

        [Test]
        public void IsBelowUserLevel_InProgressContent_IsNeverHidden()
        {
            Assert.IsFalse(LevelAccess.IsBelowUserLevel(ContentLevel.Beginner, ContentLevel.Advanced, inProgress: true));
        }

        [TestCase(null, "ADVANCED")]
        [TestCase("BEGINNER", null)]
        [TestCase("EXPERT", "ADVANCED")]
        public void IsBelowUserLevel_UnknownValues_AreShown(string item, string user)
        {
            Assert.IsFalse(LevelAccess.IsBelowUserLevel(item, user, inProgress: false));
        }

        [Test]
        public void LevelGate_IsPreviousLevel_UsesTheUserLevelOfTheDimension()
        {
            var state = new AppState { dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.FoodWaste, ContentLevel.Advanced) } };
            var gate = new LevelGate(state, null);
            var dim = new Dimension { id = "d", code = DimensionCode.FoodWaste, name = "Food waste" };

            Assert.IsTrue(gate.IsPreviousLevel(dim, ContentLevel.Intermediate, inProgress: false));
            Assert.IsFalse(gate.IsPreviousLevel(dim, ContentLevel.Advanced, inProgress: false));
            Assert.IsFalse(gate.IsPreviousLevel(null, ContentLevel.Beginner, inProgress: false), "unknown dimension: shown");
        }

        [Test]
        public void LevelGate_DevUnlocks_HidesNothing()
        {
            DevUnlocks.All = true;
            var state = new AppState { dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.FoodWaste, ContentLevel.Advanced) } };
            var dim = new Dimension { id = "d", code = DimensionCode.FoodWaste, name = "Food waste" };

            Assert.IsFalse(new LevelGate(state, null).IsPreviousLevel(dim, ContentLevel.Beginner, inProgress: false));
        }

        [Test]
        public void PreviousLevels_Visible_HidesThemUnlessShown_AndCountsThem()
        {
            var items = new[] { "b1", "a1", "b2" };
            System.Func<string, bool> isPrevious = s => s.StartsWith("b");

            var hidden = PreviousLevels.Visible(items, isPrevious, showPrevious: false, out int count);
            var shown = PreviousLevels.Visible(items, isPrevious, showPrevious: true, out int countWhenShown);

            CollectionAssert.AreEqual(new[] { "a1" }, hidden);
            Assert.AreEqual(2, count);
            CollectionAssert.AreEqual(items, shown);
            Assert.AreEqual(2, countWhenShown, "the row stays to hide them again");
        }

        [Test]
        public void PreviousLevels_ToggleSet_FlipsTheDimension()
        {
            var shown = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            Assert.IsTrue(PreviousLevels.Toggle(shown, "DIET_CHANGES"));
            Assert.IsTrue(PreviousLevels.IsShown(shown, new Dimension { id = "x", code = "diet_changes" }));
            Assert.IsFalse(PreviousLevels.Toggle(shown, "DIET_CHANGES"));
            Assert.IsFalse(PreviousLevels.IsShown(shown, new Dimension { id = "x", code = "DIET_CHANGES" }));
        }
    }
}
