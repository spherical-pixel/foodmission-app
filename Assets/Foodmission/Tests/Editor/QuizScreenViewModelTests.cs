using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuizScreenViewModelTests
    {
        private Mock<IQuizService> _mockQuizService;
        private Mock<IAvatarService> _mockAvatarService;
        private TestStoreService _storeService;
        private QuizScreenViewModel _vm;
        private Func<bool> _originalOverride;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockQuizService = new Mock<IQuizService>();
            _mockAvatarService = new Mock<IAvatarService>();
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-token",
                tokenType = "Bearer",
                lang = "es"
            });

            _vm = new QuizScreenViewModel(
                _storeService,
                _mockAvatarService.Object,
                _mockQuizService.Object
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
            Assert.IsNull(_vm.QuizData);
            Assert.IsNull(_vm.QuizProgress);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task LoadQuizDataByCodeOrId_WhenSuccessful_SetsQuizData()
        {
            var quiz = new Quiz
            {
                id = "quiz-1",
                code = "Q.B1.1",
                question = "Test Question"
            };

            _mockQuizService.Setup(s => s.GetQuizAsync("Q.B1.1", null))
                .ReturnsAsync((quiz, null));

            await _vm.LoadQuizDataByCodeOrId("Q.B1.1");

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorDetail);
            Assert.AreSame(quiz, _vm.QuizData);
        }

        [Test]
        public async Task SubmitResponse_WhenProgressContainsReward_DispatchesWalletReward()
        {
            var quiz = new Quiz { id = "quiz-1", code = "Q.B1.1" };
            _vm.QuizData = quiz;

            var expectedReward = new ContentReward { xp = 30, points = 10 };
            var progress = new QuizProgress
            {
                quizId = "quiz-1",
                quizCode = "Q.B1.1",
                completed = true,
                isCorrect = true,
                reward = expectedReward
            };

            _mockQuizService.Setup(s => s.SubmitQuizAnswerAsync("quiz-1", "Option A", null))
                .ReturnsAsync((progress, null));

            var option = new QuizOption { label = "Option A" };
            await _vm.SubmitResponse(option);

            Assert.IsNotNull(_vm.QuizProgress);
            Assert.AreSame(progress, _vm.QuizProgress);
            Assert.Contains("app/addWalletReward", _storeService.DispatchedActionTypes);
            Assert.AreEqual(30, _storeService.GetAppState().userXp);
            Assert.AreEqual(10, _storeService.GetAppState().userPoints);
        }

        [Test]
        public async Task SubmitResponse_WhenNoReward_DoesNotDispatchWalletReward()
        {
            var quiz = new Quiz { id = "quiz-1", code = "Q.B1.1" };
            _vm.QuizData = quiz;

            var progress = new QuizProgress
            {
                quizId = "quiz-1",
                quizCode = "Q.B1.1",
                completed = true,
                isCorrect = true,
                reward = null
            };

            _mockQuizService.Setup(s => s.SubmitQuizAnswerAsync("quiz-1", "Option A", null))
                .ReturnsAsync((progress, null));

            var option = new QuizOption { label = "Option A" };
            await _vm.SubmitResponse(option);

            Assert.IsNotNull(_vm.QuizProgress);
            Assert.IsFalse(_storeService.DispatchedActionTypes.Contains("app/addWalletReward"));
        }
    }
}
