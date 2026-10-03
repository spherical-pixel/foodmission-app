using System;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionFailureServiceTests
    {
        private static readonly DateTime Start1 = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Start2 = new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc);

        private TestStoreService _store;
        private TestLocalStorageService _storage;
        private Mock<IQuestService> _quests;
        private Mock<IMissionService> _missions;
        private MissionFailureService _service;

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "q1", lang = "es" });
            _storage = new TestLocalStorageService();
            _quests = new Mock<IQuestService>();
            _quests.Setup(q => q.GetQuestAsync("q1", null)).ReturnsAsync((new Quest
            {
                id = "q1",
                items = new[]
                {
                    new QuestItem { contentType = QuestContentType.Mission, contentCode = "M.A1.1" },
                    new QuestItem { contentType = QuestContentType.Mission, contentCode = "M.A1.2" }
                }
            }, (ApiErrorResponse)null));
            _missions = new Mock<IMissionService>();
            _service = new MissionFailureService(_store, _quests.Object, _missions.Object, _storage);
        }

        private void GivenProgress(params MissionProgress[] list)
        {
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync((list, (ApiErrorResponse)null));
        }

        private static MissionProgress Failed(string code, DateTime? startedAt = null) =>
            new MissionProgress { missionCode = code, missionTitle = code, status = ProgressStatus.Failed, startedAt = startedAt ?? Start1 };

        [Test]
        public async Task GetUnacknowledged_ReturnsOnlyFailedMissionsOfCurrentQuest()
        {
            GivenProgress(
                Failed("M.A1.1"),
                new MissionProgress { missionCode = "M.A1.2", status = ProgressStatus.InProgress, progress = 30f },
                Failed("M.Z9.9"));

            var failures = await _service.GetUnacknowledgedFailuresAsync();

            CollectionAssert.AreEqual(new[] { "M.A1.1" }, failures.Select(f => f.missionCode).ToArray());
        }

        [Test]
        public async Task Acknowledge_HidesThatAttempt_ButANewAttemptFailureShowsAgain()
        {
            GivenProgress(Failed("M.A1.1", Start1));
            _service.Acknowledge(Failed("M.A1.1", Start1));
            Assert.IsEmpty(await _service.GetUnacknowledgedFailuresAsync());

            GivenProgress(Failed("M.A1.1", Start2));
            Assert.AreEqual(1, (await _service.GetUnacknowledgedFailuresAsync()).Count);
        }

        [Test]
        public async Task GetUnacknowledged_WithoutCurrentQuest_ReturnsEmpty()
        {
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = null });
            GivenProgress(Failed("M.A1.1"));
            Assert.IsEmpty(await _service.GetUnacknowledgedFailuresAsync());
        }

        [Test]
        public async Task GetUnacknowledged_OnProgressError_ReturnsEmpty()
        {
            _missions.Setup(m => m.GetUserProgressListAsync(null)).ReturnsAsync(((MissionProgress[])null, new ApiErrorResponse { message = "x" }));
            Assert.IsEmpty(await _service.GetUnacknowledgedFailuresAsync());
        }

        [Test]
        public async Task RestartAsync_AcknowledgesAndRestarts()
        {
            var restarted = new MissionProgress { missionCode = "M.A1.1", status = ProgressStatus.NotStarted, startedAt = Start2 };
            _missions.Setup(m => m.RestartMissionProgressAsync("M.A1.1", null)).ReturnsAsync((restarted, (ApiErrorResponse)null));

            var (result, error) = await _service.RestartAsync(Failed("M.A1.1", Start1));

            Assert.IsNull(error);
            Assert.AreSame(restarted, result);
            GivenProgress(Failed("M.A1.1", Start1));
            Assert.IsEmpty(await _service.GetUnacknowledgedFailuresAsync());
        }

        [Test]
        public async Task GiveUpAndRestart_FailsThenRestarts()
        {
            var sequence = new MockSequence();
            _missions.InSequence(sequence).Setup(m => m.FailMissionProgressAsync("M.A1.2", null))
                .ReturnsAsync((Failed("M.A1.2", Start1), (ApiErrorResponse)null));
            _missions.InSequence(sequence).Setup(m => m.RestartMissionProgressAsync("M.A1.2", null))
                .ReturnsAsync((new MissionProgress { missionCode = "M.A1.2", status = ProgressStatus.NotStarted }, (ApiErrorResponse)null));

            var (result, error) = await _service.GiveUpAndRestartAsync("M.A1.2");

            Assert.IsNull(error);
            Assert.AreEqual(ProgressStatus.NotStarted, result.status);
            _missions.Verify(m => m.RestartMissionProgressAsync("M.A1.2", null), Times.Once);
        }

        [Test]
        public async Task GiveUpAndRestart_WhenFailErrors_DoesNotRestart()
        {
            var failError = new ApiErrorResponse { message = "completed" };
            _missions.Setup(m => m.FailMissionProgressAsync("M.A1.2", null)).ReturnsAsync(((MissionProgress)null, failError));

            var (result, error) = await _service.GiveUpAndRestartAsync("M.A1.2");

            Assert.IsNull(result);
            Assert.AreSame(failError, error);
            _missions.Verify(m => m.RestartMissionProgressAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GiveUpAndRestart_WhenRestartErrors_ReturnsErrorAndDoesNotNotifyLater()
        {
            _missions.Setup(m => m.FailMissionProgressAsync("M.A1.2", null)).ReturnsAsync((Failed("M.A1.2", Start1), (ApiErrorResponse)null));
            var restartError = new ApiErrorResponse { message = "network" };
            _missions.Setup(m => m.RestartMissionProgressAsync("M.A1.2", null)).ReturnsAsync(((MissionProgress)null, restartError));

            var (result, error) = await _service.GiveUpAndRestartAsync("M.A1.2");

            Assert.IsNull(result);
            Assert.AreSame(restartError, error);
            // The user chose to give up: Home must not announce this failure as news
            GivenProgress(Failed("M.A1.2", Start1));
            Assert.IsEmpty(await _service.GetUnacknowledgedFailuresAsync());
        }
    }
}
