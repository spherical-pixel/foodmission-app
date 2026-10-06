using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DimensionLevelsTests
    {
        [Test]
        public void GetLevel_PrefersStoredLevel_ThenSegment_ThenBeginner()
        {
            var state = new AppState
            {
                userSegment = "INTERMEDIATE",
                dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.DietChanges, "ADVANCED") }
            };

            Assert.AreEqual("ADVANCED", DimensionLevels.GetLevel(state, DimensionCode.DietChanges));
            Assert.AreEqual("INTERMEDIATE", DimensionLevels.GetLevel(state, DimensionCode.FoodWaste));
            Assert.AreEqual("BEGINNER", DimensionLevels.GetLevel(new AppState(), DimensionCode.FoodWaste));
        }

        [Test]
        public void GetLevel_InvalidStoredLevel_IsIgnored()
        {
            var state = new AppState
            {
                userSegment = "",
                dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.DietChanges, "EXPERT"), null }
            };

            Assert.AreEqual("BEGINNER", DimensionLevels.GetLevel(state, DimensionCode.DietChanges));
        }

        [Test]
        public void Resolve_ReturnsSixEntriesInCatalogueOrder()
        {
            DimensionLevelEntry[] resolved = DimensionLevels.Resolve(new AppState { userSegment = "ADVANCED" });

            CollectionAssert.AreEqual(DimensionCode.All, resolved.Select(e => e.dimensionCode).ToArray());
            Assert.IsTrue(resolved.All(e => e.level == "ADVANCED"));
        }

        [TestCase("INTERMEDIATE", "INTERMEDIATE")]
        [TestCase("advanced", "ADVANCED")]
        [TestCase("", "BEGINNER")]
        [TestCase(null, "BEGINNER")]
        [TestCase("FAMILY", "BEGINNER")]
        public void Propose_UsesSegmentForEveryDimension(string segment, string expected)
        {
            DimensionLevelEntry[] proposal = DimensionLevels.Propose(segment);

            Assert.AreEqual(DimensionCode.All.Length, proposal.Length);
            Assert.IsTrue(proposal.All(e => e.level == expected));
        }

        [Test]
        public void FromMap_KeepsOnlyKnownDimensionsAndValidLevels()
        {
            var map = new Dictionary<string, string>
            {
                { DimensionCode.DietChanges, "intermediate" },
                { "UNKNOWN_DIMENSION", "ADVANCED" },
                { DimensionCode.FoodWaste, "EXPERT" }
            };

            DimensionLevelEntry[] entries = DimensionLevels.FromMap(map);

            Assert.AreEqual(1, entries.Length);
            Assert.AreEqual(DimensionCode.DietChanges, entries[0].dimensionCode);
            Assert.AreEqual("INTERMEDIATE", entries[0].level);
            Assert.AreEqual(0, DimensionLevels.FromMap(null).Length);
        }

        [Test]
        public void ToMap_RoundTripsWithFromMap()
        {
            DimensionLevelEntry[] proposal = DimensionLevels.Propose("ADVANCED");

            Dictionary<string, string> map = DimensionLevels.ToMap(proposal);

            Assert.AreEqual(6, map.Count);
            Assert.AreEqual("ADVANCED", map[DimensionCode.Packaging]);
            Assert.AreEqual(6, DimensionLevels.FromMap(map).Length);
        }
    }
}
