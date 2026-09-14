using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ChallengeDetailViewModelTests
    {
        private Mock<IChallengeService> _mockChallengeService;
        private Mock<IEventService> _mockEventService;
        private Mock<IDimensionService> _mockDimensionService;
        private Mock<IActivityEventMapper> _mockActivityEventMapper;
        private TestStoreService _storeService;
        private ChallengeDetailViewModel _vm;
        private Func<bool> _originalOverride;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockChallengeService = new Mock<IChallengeService>();
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

            _vm = new ChallengeDetailViewModel(
                _storeService,
                _mockChallengeService.Object,
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
            Assert.IsNull(_vm.Challenge);
            Assert.IsNull(_vm.ChallengeProgress);
            Assert.AreEqual(0, _vm.SelectedTabIndex);
        }

        [Test]
        public async Task LoadChallengeAsync_WhenSuccessful_SetsChallengeProgressAndMapping()
        {
            var challenge = new Challenge
            {
                id = "ch-1",
                code = "CH.B1.1",
                title = "Which Protein Has the Lowest Footprint?",
                task = "Compare two proteins in the shopping list",
                dimensionId = "dim-1",
                level = "BEGINNER"
            };

            var progress = new ChallengeProgress
            {
                challengeId = "ch-1",
                completed = false,
                progress = 0f
            };

            var mapping = new ActivityMapping
            {
                ActivityCode = "CH.B1.1",
                TargetEventTypes = new[] { ClientEventTypes.LearningFootprintCompared },
                NativeModuleAction = "go_to_shopping_list",
                QuestionType = DirectQuestionType.SingleChoice
            };

            _mockChallengeService.Setup(s => s.GetChallengeAsync("CH.B1.1", It.IsAny<string>()))
                .ReturnsAsync((challenge, null));
            _mockChallengeService.Setup(s => s.GetChallengeProgressAsync("CH.B1.1", It.IsAny<string>()))
                .ReturnsAsync((progress, null));
            _mockActivityEventMapper.Setup(m => m.GetChallengeMapping("CH.B1.1"))
                .Returns(mapping);

            await _vm.LoadChallengeAsync("CH.B1.1");

            Assert.IsNotNull(_vm.Challenge);
            Assert.AreEqual("CH.B1.1", _vm.Challenge.code);
            Assert.IsNotNull(_vm.ChallengeProgress);
            Assert.IsNotNull(_vm.Mapping);
            Assert.AreEqual("go_to_shopping_list", _vm.Mapping.NativeModuleAction);
        }

        [Test]
        public async Task SubmitDirectReportAsync_EmitsClientEventAndSetsSuccess()
        {
            var challenge = new Challenge { id = "ch-1", code = "CH.B1.1" };
            var mapping = new ActivityMapping
            {
                ActivityCode = "CH.B1.1",
                TargetEventTypes = new[] { ClientEventTypes.LearningFootprintCompared },
                QuestionType = DirectQuestionType.SingleChoice
            };

            _vm.Challenge = challenge;
            _vm.Mapping = mapping;

            _mockEventService.Setup(s => s.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()))
                .ReturnsAsync((new UserEvent { id = "ev-1", eventType = ClientEventTypes.LearningFootprintCompared }, null));

            bool success = await _vm.SubmitDirectReportAsync();

            Assert.IsTrue(success);
            Assert.IsTrue(_vm.ReportSuccess);
            _mockEventService.Verify(s => s.RecordClientEventAsync(It.Is<CreateClientEventRequest>(
                r => r.eventType == ClientEventTypes.LearningFootprintCompared
            )), Times.Once);
        }
    }
}
