using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine.Localization.Settings;

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
        public async Task LoadQuiz_WaitsForTheTopicCatalog_SoTheBannerResolvesItsTopic()
        {
            var dimensions = new Mock<IDimensionService>();
            var catalog = new TaskCompletionSource<(Dimension[], ApiErrorResponse)>();
            dimensions.Setup(d => d.PreloadAsync(null, false)).Returns(catalog.Task);
            _mockQuizService.Setup(s => s.GetQuizAsync("Q.B1.1", null))
                .ReturnsAsync((new Quiz { code = "Q.B1.1", topicId = "t1" }, (ApiErrorResponse)null));
            var vm = new QuizScreenViewModel(_storeService, _mockAvatarService.Object, _mockQuizService.Object, dimensionService: dimensions.Object);

            Task load = vm.LoadQuizDataByCodeOrId("Q.B1.1");
            Assert.IsNull(vm.QuizData);

            catalog.SetResult((new Dimension[0], null));
            await load;

            Assert.AreEqual("Q.B1.1", vm.QuizData.code);
            vm.Dispose();
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
        public async Task LoadQuizDataByCodeOrId_WhileRequestPending_IsLoading()
        {
            // QuizScreen shows the blocking loading overlay only for this initial load.
            var pending = new TaskCompletionSource<(Quiz, ApiErrorResponse)>();
            _mockQuizService.Setup(s => s.GetQuizAsync("Q.B1.1", null)).Returns(pending.Task);

            Task load = _vm.LoadQuizDataByCodeOrId("Q.B1.1");
            Assert.IsTrue(_vm.IsLoading);

            pending.SetResult((new Quiz { id = "quiz-1" }, null));
            await load;
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task SubmitResponse_WhileRequestPending_IsNotLoading()
        {
            // Answering must not trigger the blocking overlay (it would break the answer animation).
            _vm.QuizData = new Quiz { id = "quiz-1", code = "Q.B1.1" };
            var pending = new TaskCompletionSource<(QuizProgress, ApiErrorResponse)>();
            _mockQuizService.Setup(s => s.SubmitQuizAnswerAsync("quiz-1", "Option A", null)).Returns(pending.Task);

            Task submit = _vm.SubmitResponse(new QuizOption { label = "Option A" });
            Assert.IsFalse(_vm.IsLoading);

            pending.SetResult((new QuizProgress { quizId = "quiz-1", completed = true }, null));
            await submit;
            Assert.IsFalse(_vm.IsLoading);
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

        private static string Ui(string key) => LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);

        [Test]
        public async Task ShareAsync_WithLoadedQuiz_SharesExplanationSourceAndFooter()
        {
            var share = new Mock<IShareService>();
            ShareContent sent = null;
            share.Setup(s => s.ShareAsync(It.IsAny<ShareContent>()))
                .Callback<ShareContent>(c => sent = c)
                .ReturnsAsync(true);
            var vm = new QuizScreenViewModel(_storeService, _mockAvatarService.Object, _mockQuizService.Object, share.Object);
            vm.QuizData = new Quiz { id = "q1", explanation = "Explanation", source = "Estudio en [Nature Food](https://doi.org/x)." };

            bool result = await vm.ShareAsync();

            Assert.IsTrue(result);
            Assert.AreEqual(ShareTextBuilder.Build("Explanation", "Estudio en [Nature Food](https://doi.org/x).", Ui("SHARE_FOOTER")), sent.Text);
            StringAssert.DoesNotContain("](", sent.Text);
            Assert.AreEqual(Ui("SHARE_SUBJECT_QUIZ"), sent.Subject);
            Assert.IsNull(sent.Image);
            vm.Dispose();
        }

        [Test]
        public async Task ShareAsync_WithoutQuizOrExplanation_DoesNotShare()
        {
            var share = new Mock<IShareService>();
            var vm = new QuizScreenViewModel(_storeService, _mockAvatarService.Object, _mockQuizService.Object, share.Object);

            Assert.IsFalse(await vm.ShareAsync());
            vm.QuizData = new Quiz { id = "q1", explanation = null };
            Assert.IsFalse(await vm.ShareAsync());
            share.Verify(s => s.ShareAsync(It.IsAny<ShareContent>()), Times.Never);
            vm.Dispose();
        }
    }
}
