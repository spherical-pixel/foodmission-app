using System;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionDetailViewModelTests
    {
        private Mock<IMissionService> _missions;
        private Mock<IDimensionService> _dimensions;
        private Mock<IQuestService> _quests;
        private Mock<IMissionFailureService> _failures;
        private TestStoreService _store;
        private MissionDetailViewModel _vm;
        private string _navAction;
        private Argument[] _navArgs;

        [SetUp]
        public void SetUp()
        {
            _navAction = null;
            _navArgs = null;
            _missions = new Mock<IMissionService>();
            _dimensions = new Mock<IDimensionService>();
            _dimensions.Setup(d => d.IsLoaded).Returns(true);
            _quests = new Mock<IQuestService>();
            _failures = new Mock<IMissionFailureService>();
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", userCurrentQuestId = "q1", lang = "es" });
            _quests.Setup(q => q.GetQuestAsync("q1", null)).ReturnsAsync((new Quest
            {
                id = "q1",
                items = new[]
                {
                    new QuestItem { contentType = QuestContentType.Mission, contentCode = "M.B1.4" },
                    new QuestItem { contentType = QuestContentType.Mission, contentCode = "M.A2.1" }
                }
            }, (ApiErrorResponse)null));
            _vm = new MissionDetailViewModel(_store, _missions.Object, _dimensions.Object, _quests.Object, _failures.Object);
            _vm.NavigationRequested += (action, args) => { _navAction = action; _navArgs = args; };
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
        }

        private Task LoadAsync(string code, float progress = 0f, bool completed = false, string status = null)
        {
            _missions.Setup(m => m.GetMissionAsync(code, null)).ReturnsAsync((new Mission { id = "id", code = code, title = "T", level = "BEGINNER" }, (ApiErrorResponse)null));
            _missions.Setup(m => m.GetMissionProgressAsync(code, null)).ReturnsAsync((new MissionProgress { missionCode = code, progress = progress, completed = completed, status = status }, (ApiErrorResponse)null));
            return _vm.LoadMissionAsync(code);
        }

        [Test]
        public async Task Load_CurrentQuestMission_CanActWithCatalogModules()
        {
            await LoadAsync("M.B1.4", 40f);

            Assert.AreSame(MissionInteractionCatalog.Get("M.B1.4"), _vm.Interaction);
            Assert.IsTrue(_vm.IsCurrentQuestMission);
            Assert.IsTrue(_vm.CanAct);
            Assert.AreEqual(40, _vm.ProgressPercent);
            Assert.AreSame(MissionInteractionCatalog.QuickMealLog, _vm.AutoModules[0]);
            Assert.AreEqual("CHALLENGE_LEVEL_BEGINNER", _vm.LevelKey);
        }

        [Test]
        public async Task Load_MissionOutsideCurrentQuest_CannotAct()
        {
            await LoadAsync("M.B2.1");

            Assert.IsFalse(_vm.IsCurrentQuestMission);
            Assert.IsFalse(_vm.CanAct);
            Assert.IsTrue(_vm.ShowsNotCurrentQuest);
            Assert.AreEqual(0, _vm.HelperModules.Count);
        }

        [Test]
        public async Task Load_PendingRule_IsNotAvailable()
        {
            await LoadAsync("M.A2.1");

            Assert.IsTrue(_vm.IsPendingRule);
            Assert.IsFalse(_vm.CanAct);
            Assert.IsFalse(_vm.ShowsNotCurrentQuest);
        }

        [Test]
        public async Task Load_Completed_CannotAct()
        {
            await LoadAsync("M.B1.4", 100f, true);

            Assert.IsTrue(_vm.IsCompleted);
            Assert.IsFalse(_vm.CanAct);
        }

        [Test]
        public async Task OpenCheckIn_NavigatesWithMissionCode()
        {
            await LoadAsync("M.B1.4");

            _vm.OpenCheckIn();

            Assert.AreEqual(Actions.open_mission_checkin, _navAction);
            Assert.AreEqual("code", _navArgs[0].name);
            Assert.AreEqual("M.B1.4", _navArgs[0].value);
        }

        [Test]
        public async Task OpenCheckIn_WhenCannotAct_DoesNothing()
        {
            await LoadAsync("M.B2.1");

            _vm.OpenCheckIn();

            Assert.IsNull(_navAction);
        }

        [Test]
        public async Task OpenModule_NavigatesToModuleAction()
        {
            await LoadAsync("M.B1.4");

            _vm.OpenModule(_vm.AutoModules[0]);

            Assert.AreEqual(Actions.open_quick_meal_log, _navAction);
        }

        [Test]
        public async Task Load_MissionError_SetsErrorDetail()
        {
            var error = new ApiErrorResponse { message = "404" };
            _missions.Setup(m => m.GetMissionAsync("M.B1.4", null)).ReturnsAsync(((Mission)null, error));

            await _vm.LoadMissionAsync("M.B1.4");

            Assert.AreSame(error, _vm.ErrorDetail);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task LoadMissionAsync_ById_FetchesProgressByMissionCode()
        {
            _missions.Setup(m => m.GetMissionAsync("mission-id", null)).ReturnsAsync((new Mission { id = "mission-id", code = "M.B2.1", title = "T", level = "BEGINNER" }, (ApiErrorResponse)null));
            _missions.Setup(m => m.GetMissionProgressAsync(It.IsAny<string>(), null)).ReturnsAsync((new MissionProgress { missionCode = "M.B2.1", progress = 10 }, (ApiErrorResponse)null));

            await _vm.LoadMissionAsync("mission-id");

            _missions.Verify(m => m.GetMissionProgressAsync("M.B2.1", null), Times.Once);
            _missions.Verify(m => m.GetMissionProgressAsync("mission-id", null), Times.Never);
        }

        [Test]
        public async Task Load_FailedMission_ShowsFailedState_AndCanRestart()
        {
            await LoadAsync("M.B1.4", 100f, status: ProgressStatus.Failed);

            Assert.IsTrue(_vm.IsFailed);
            Assert.IsFalse(_vm.IsCompleted);
            Assert.IsFalse(_vm.CanAct);
            Assert.IsFalse(_vm.ShowsNotCurrentQuest);
            Assert.IsTrue(_vm.CanRestartFailed);
            Assert.IsFalse(_vm.CanRestartActive);
            Assert.AreEqual(0, _vm.ProgressPercent);
            _failures.Verify(f => f.Acknowledge(It.Is<MissionProgress>(p => p.missionCode == "M.B1.4")), Times.Once);
        }

        [Test]
        public async Task Load_FailedMission_NotInCurrentQuest_CannotRestart()
        {
            await LoadAsync("M.Z9.9", 0f, status: ProgressStatus.Failed);

            Assert.IsTrue(_vm.IsFailed);
            Assert.IsFalse(_vm.CanRestartFailed);
        }

        [Test]
        public async Task Load_ActiveMission_CanRestartActive()
        {
            await LoadAsync("M.B1.4", 40f, status: ProgressStatus.InProgress);

            Assert.IsTrue(_vm.CanAct);
            Assert.IsTrue(_vm.CanRestartActive);
            Assert.IsFalse(_vm.CanRestartFailed);
        }

        [Test]
        public async Task RestartMission_WhenFailed_RestartsAndUpdatesProgress()
        {
            await LoadAsync("M.B1.4", 0f, status: ProgressStatus.Failed);
            var restarted = new MissionProgress { missionCode = "M.B1.4", status = ProgressStatus.NotStarted };
            _failures.Setup(f => f.RestartAsync(It.IsAny<MissionProgress>())).ReturnsAsync((restarted, (ApiErrorResponse)null));

            await _vm.RestartMissionAsync();

            Assert.AreSame(restarted, _vm.MissionProgress);
            Assert.IsFalse(_vm.IsFailed);
            Assert.IsTrue(_vm.CanAct);
        }

        [Test]
        public async Task RestartMission_WhenActive_GivesUpAndRestarts()
        {
            await LoadAsync("M.B1.4", 40f, status: ProgressStatus.InProgress);
            _failures.Setup(f => f.GiveUpAndRestartAsync("M.B1.4"))
                .ReturnsAsync((new MissionProgress { missionCode = "M.B1.4", status = ProgressStatus.NotStarted }, (ApiErrorResponse)null));

            await _vm.RestartMissionAsync();

            _failures.Verify(f => f.GiveUpAndRestartAsync("M.B1.4"), Times.Once);
            Assert.AreEqual(0, _vm.ProgressPercent);
        }

        [Test]
        public async Task RestartMission_WhenRestartFailsAfterGivingUp_ReloadsFailedState_AndShowsError()
        {
            await LoadAsync("M.B1.4", 40f, status: ProgressStatus.InProgress);
            var error = new ApiErrorResponse { message = "network" };
            _failures.Setup(f => f.GiveUpAndRestartAsync("M.B1.4")).ReturnsAsync(((MissionProgress)null, error));
            _missions.Setup(m => m.GetMissionProgressAsync("M.B1.4", null))
                .ReturnsAsync((new MissionProgress { missionCode = "M.B1.4", status = ProgressStatus.Failed }, (ApiErrorResponse)null));

            await _vm.RestartMissionAsync();

            Assert.AreSame(error, _vm.ErrorDetail);
            Assert.IsTrue(_vm.IsFailed);
            Assert.IsTrue(_vm.CanRestartFailed);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task Load_ActiveMission_WithoutStatus_CannotRestartActive()
        {
            // Old backend (before pr-402): no status, no restart endpoint
            await LoadAsync("M.B1.4", 40f);

            Assert.IsTrue(_vm.CanAct);
            Assert.IsFalse(_vm.CanRestartActive);
        }
    }
}
