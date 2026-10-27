using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CheckInResultLinesTests
    {
        private static CheckInResultLine LineFor(string code, float percent, string status = ProgressStatus.InProgress) =>
            CheckInResultLines.For("Title", MissionInteractionCatalog.Get(code),
                new MissionProgress { missionCode = code, progress = percent, status = status });

        private static void AssertLine(CheckInResultLine line, string key, params object[] args)
        {
            Assert.AreEqual(key, line.Key);
            CollectionAssert.AreEqual(args, line.Args);
        }

        [Test]
        public void SwapCountMission_ShowsNOfTarget() =>
            AssertLine(LineFor("M.I3.3", 75f), "MISSION_CHECKIN_LINE_SWAPS", "Title", 3, 4);

        [Test]
        public void LevelMission_ShowsLevels() =>
            AssertLine(LineFor("M.I1.2", 66.67f), "MISSION_CHECKIN_LINE_LEVELS", "Title", 2, 3);

        [Test]
        public void FlagDayMission_ShowsDays() =>
            AssertLine(LineFor("M.B5.5", 50f), "MISSION_CHECKIN_LINE_DAYS", "Title", 2, 4);

        [Test]
        public void LimitOrRatioMission_ShowsPercent()
        {
            AssertLine(LineFor("M.A6.3", 40f), "MISSION_CHECKIN_LINE_PERCENT", "Title", 40);
            AssertLine(LineFor("M.A1.3", 33.4f), "MISSION_CHECKIN_LINE_PERCENT", "Title", 33);
        }

        [Test]
        public void Completed_And_Failed_Lines()
        {
            AssertLine(LineFor("M.I3.3", 100f, ProgressStatus.Completed), "MISSION_CHECKIN_LINE_DONE", "Title");
            AssertLine(LineFor("M.I3.3", 50f, ProgressStatus.Failed), "MISSION_CHECKIN_LINE_FAILED", "Title");
        }

        [Test]
        public void NullProgress_NoLine()
        {
            Assert.IsNull(CheckInResultLines.For("Title", MissionInteractionCatalog.Get("M.I3.3"), null));
        }
    }
}
