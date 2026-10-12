using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DimensionLevelsTests
    {
        [Test]
        public void GetLevel_StoredLevel_ElseBeginner_SegmentIgnored()
        {
            var state = new AppState
            {
                userSegment = "ADVANCED",
                dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.DietChanges, "INTERMEDIATE") }
            };

            Assert.AreEqual("INTERMEDIATE", DimensionLevels.GetLevel(state, DimensionCode.DietChanges));
            Assert.AreEqual("BEGINNER", DimensionLevels.GetLevel(state, DimensionCode.FoodWaste));
            Assert.AreEqual("BEGINNER", DimensionLevels.GetLevel(null, DimensionCode.FoodWaste));
        }

        [Test]
        public void GetLevel_InvalidStoredLevel_IsIgnored()
        {
            var state = new AppState { dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.DietChanges, "EXPERT"), null } };

            Assert.AreEqual("BEGINNER", DimensionLevels.GetLevel(state, DimensionCode.DietChanges));
        }

        [Test]
        public void Resolve_ReturnsSixEntriesInCatalogueOrder()
        {
            DimensionLevelEntry[] resolved = DimensionLevels.Resolve(new AppState
            {
                dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.Packaging, "ADVANCED") }
            });

            CollectionAssert.AreEqual(DimensionCode.All, resolved.Select(e => e.dimensionCode).ToArray());
            Assert.AreEqual("ADVANCED", resolved.First(e => e.dimensionCode == DimensionCode.Packaging).level);
            Assert.AreEqual(5, resolved.Count(e => e.level == "BEGINNER"));
        }

        [Test]
        public void Sanitize_KeepsKnownCodesAndValidLevels_InCatalogueOrder()
        {
            DimensionLevelEntry[] entries = DimensionLevels.Sanitize(new[]
            {
                new DimensionLevelEntry(DimensionCode.FoodWaste, "advanced"),
                new DimensionLevelEntry("UNKNOWN", "ADVANCED"),
                new DimensionLevelEntry(DimensionCode.DietChanges, "EXPERT"),
                null,
                new DimensionLevelEntry(DimensionCode.DietChanges, "INTERMEDIATE")
            });

            CollectionAssert.AreEqual(new[] { DimensionCode.DietChanges, DimensionCode.FoodWaste }, entries.Select(e => e.dimensionCode).ToArray());
            Assert.AreEqual("INTERMEDIATE", entries[0].level);
            Assert.AreEqual("ADVANCED", entries[1].level);
            Assert.AreEqual(0, DimensionLevels.Sanitize(null).Length);
        }

        [Test]
        public async Task RefreshAsync_Success_StoresBackendLevels()
        {
            var store = new TestStoreService();
            var gamification = new Mock<IGamificationService>();
            gamification.Setup(g => g.GetDimensionLevelsAsync()).ReturnsAsync((new[] { new DimensionLevelEntry(DimensionCode.Packaging, "INTERMEDIATE") }, (ApiErrorResponse)null));

            Assert.IsTrue(await DimensionLevels.RefreshAsync(gamification.Object, store));

            Assert.AreEqual("INTERMEDIATE", DimensionLevels.GetLevel(store.GetAppState(), DimensionCode.Packaging));
            store.Dispose();
        }

        [Test]
        public async Task RefreshAsync_ErrorOrNullService_KeepsStoredLevels()
        {
            var store = new TestStoreService();
            store.SetAppState(new AppState { dimensionLevels = new[] { new DimensionLevelEntry(DimensionCode.Packaging, "ADVANCED") } });
            var gamification = new Mock<IGamificationService>();
            gamification.Setup(g => g.GetDimensionLevelsAsync()).ReturnsAsync(((DimensionLevelEntry[])null, new ApiErrorResponse { statusCode = 500 }));
            LogAssert.Expect(LogType.Warning, new Regex("Could not refresh levels"));

            Assert.IsFalse(await DimensionLevels.RefreshAsync(gamification.Object, store));
            Assert.IsFalse(await DimensionLevels.RefreshAsync(null, store));

            Assert.AreEqual("ADVANCED", DimensionLevels.GetLevel(store.GetAppState(), DimensionCode.Packaging));
            store.Dispose();
        }
    }
}
