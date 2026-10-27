using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    /// <summary>A mission that already has what it needs and only waits for its window to end (2026-10-27).</summary>
    [TestFixture]
    public class MissionSecuredTests
    {
        private static MissionProgress P(float progress, string status = ProgressStatus.InProgress) =>
            new MissionProgress { progress = progress, status = status, startedAt = System.DateTime.UtcNow.AddDays(-3) };

        [Test]
        public void DayMissionAtFullProgress_IsSecured()
        {
            Assert.IsTrue(MissionProgressState.IsSecured(P(100f), MissionInteractionCatalog.Get("M.I1.3")));
        }

        [Test]
        public void DayMissionBelowFullProgress_IsNotSecured()
        {
            Assert.IsFalse(MissionProgressState.IsSecured(P(50f), MissionInteractionCatalog.Get("M.I1.3")));
        }

        [Test]
        public void ResolvedMission_IsNotSecured()
        {
            Assert.IsFalse(MissionProgressState.IsSecured(P(100f, ProgressStatus.Completed), MissionInteractionCatalog.Get("M.I1.3")));
            Assert.IsFalse(MissionProgressState.IsSecured(P(100f, ProgressStatus.Failed), MissionInteractionCatalog.Get("M.I1.3")));
        }

        [TestCase("M.A6.3")]
        [TestCase("M.I6.3")]
        [TestCase("M.B1.3")]
        [TestCase("M.A1.1")]
        public void MissionsWithALimit_AreNeverSecured(string code)
        {
            Assert.IsFalse(MissionProgressState.IsSecured(P(100f), MissionInteractionCatalog.Get(code)));
        }
    }
}
