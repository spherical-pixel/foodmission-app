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
    }
}
