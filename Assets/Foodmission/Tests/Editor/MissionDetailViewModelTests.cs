using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionDetailViewModelTests
    {
        private Mock<IMissionService> _mockMissionService;
        private Mock<IEventService> _mockEventService;
        private Mock<IDimensionService> _mockDimensionService;
        private Mock<IActivityEventMapper> _mockActivityEventMapper;
        private TestStoreService _storeService;
        private MissionDetailViewModel _vm;
        private Func<bool> _originalOverride;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockMissionService = new Mock<IMissionService>();
            _mockEventService = new Mock<IEventService>();
            _mockDimensionService = new Mock<IDimensionService>();
            _mockActivityEventMapper = new Mock<IActivityEventMapper>();
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-token",
                tokenType = "Bearer",
                lang = "es"
            });

            _vm = new MissionDetailViewModel(
                _storeService,
                _mockMissionService.Object,
                _mockDimensionService.Object,
                _mockEventService.Object,
                _mockActivityEventMapper.Object
            );
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
            _vm?.Dispose();
        }

        [Test]
        public void InitialState_ShouldHaveDefaultValues()
        {
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.Mission);
            Assert.IsNull(_vm.MissionProgress);
            Assert.AreEqual(0, _vm.SelectedTabIndex);
            Assert.AreEqual(1, _vm.SelectedCount);
        }

        [Test]
        public async Task LoadMissionAsync_WhenSuccessful_SetsMissionProgressAndMapping()
        {
            var mission = new Mission
            {
                id = "m-1",
                code = "M.A1.1",
                title = "Stay in the Green Zone",
                goal = "Remain below 4 meat meals per week",
                dimensionId = "dim-1",
                level = "ADVANCED"
            };

            var progress = new MissionProgress
            {
                missionId = "m-1",
                completed = false,
                progress = 50f
            };

            var mapping = new ActivityMapping
            {
                ActivityCode = "M.A1.1",
                TargetEventTypes = new[] { ClientEventTypes.MealMeatConsumed },
                NativeModuleAction = "go_to_meallog",
                QuestionType = DirectQuestionType.CountStepper
            };

            _mockMissionService.Setup(s => s.GetMissionAsync("M.A1.1", It.IsAny<string>()))
                .ReturnsAsync((mission, null));
            _mockMissionService.Setup(s => s.GetMissionProgressAsync("M.A1.1", It.IsAny<string>()))
                .ReturnsAsync((progress, null));
            _mockActivityEventMapper.Setup(m => m.GetMissionMapping("M.A1.1"))
                .Returns(mapping);

            await _vm.LoadMissionAsync("M.A1.1");

            Assert.IsNotNull(_vm.Mission);
            Assert.AreEqual("M.A1.1", _vm.Mission.code);
            Assert.IsNotNull(_vm.MissionProgress);
            Assert.AreEqual(50f, _vm.MissionProgress.progress);
            Assert.IsNotNull(_vm.Mapping);
            Assert.AreEqual("go_to_meallog", _vm.Mapping.NativeModuleAction);
        }

        [Test]
        public async Task SubmitDirectReportAsync_EmitsClientEventAndSetsSuccess()
        {
            var mission = new Mission { id = "m-1", code = "M.A1.1" };
            var mapping = new ActivityMapping
            {
                ActivityCode = "M.A1.1",
                TargetEventTypes = new[] { ClientEventTypes.MealMeatConsumed },
                QuestionType = DirectQuestionType.CountStepper
            };

            _vm.Mission = mission;
            _vm.Mapping = mapping;
            _vm.SelectedCount = 2;

            _mockEventService.Setup(s => s.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()))
                .ReturnsAsync((new UserEvent { id = "ev-1", eventType = ClientEventTypes.MealMeatConsumed }, null));

            bool success = await _vm.SubmitDirectReportAsync();

            Assert.IsTrue(success);
            Assert.IsTrue(_vm.ReportSuccess);
            _mockEventService.Verify(s => s.RecordClientEventAsync(It.Is<CreateClientEventRequest>(
                r => r.eventType == ClientEventTypes.MealMeatConsumed
            )), Times.Once);
        }

        [Test]
        public void Stepper_IncrementAndDecrement_ClampsCorrectly()
        {
            _vm.SelectedCount = 1;
            _vm.Mapping = new ActivityMapping { MaxCount = 5, DefaultCount = 1 };

            _vm.IncrementCount();
            Assert.AreEqual(2, _vm.SelectedCount);

            _vm.DecrementCount();
            Assert.AreEqual(1, _vm.SelectedCount);

            _vm.DecrementCount();
            Assert.AreEqual(0, _vm.SelectedCount);

            _vm.DecrementCount(); // Below zero clamp
            Assert.AreEqual(0, _vm.SelectedCount);
        }
    }
}
