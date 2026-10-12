using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class OnboardingSurveyViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<IGamificationService> _gamification;
        private Mock<IAuthService> _auth;

        private static OnboardingSurveyQuestion Q(string field, params string[] values) => new OnboardingSurveyQuestion
        {
            field = field,
            text = field + "?",
            options = values.Select(v => new OnboardingSurveyOption { value = v, label = v.ToLowerInvariant() }).ToArray()
        };

        private static readonly OnboardingSurveyQuestion[] ThreeQuestions =
        {
            Q("weeklyMeatConsumption", "ZERO_TO_FOUR", "FIVE_TO_NINE"),
            Q("usesLeftovers", "NEVER", "OFTEN"),
            Q("wholeGrainFrequency", "NEVER", "DAILY")
        };

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState { lang = "es", accessToken = "t" });
            _gamification = new Mock<IGamificationService>();
            _gamification.Setup(g => g.GetOnboardingSurveyAsync()).ReturnsAsync((ThreeQuestions, (ApiErrorResponse)null));
            _gamification.Setup(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()))
                .ReturnsAsync((new OnboardingSurveyResult { segment = "BEGINNER", progressWheels = new ProgressWheel[0] }, (ApiErrorResponse)null));
            _auth = new Mock<IAuthService>();
            _auth.Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>())).ReturnsAsync((true, (ApiErrorResponse)null));
        }

        [TearDown]
        public void TearDown()
        {
            _storeService?.Dispose();
        }

        private OnboardingSurveyViewModel CreateVm()
        {
            var vm = new OnboardingSurveyViewModel(_storeService, _gamification.Object, _auth.Object);
            vm.Initialize();
            return vm;
        }

        private static async Task GoToEndAsync(OnboardingSurveyViewModel vm)
        {
            while (!vm.IsLastStep)
            {
                await vm.GoNextAsync();
            }
            await vm.GoNextAsync();
        }

        [Test]
        public void Initialize_OneStepPerBackendQuestionPlusWelcome()
        {
            var vm = CreateVm();

            Assert.AreEqual(4, vm.StepCount);
            Assert.AreEqual(3, vm.Questions.Count);
            Assert.IsNull(vm.QuestionForStep(0));
            Assert.AreEqual("usesLeftovers", vm.QuestionForStep(2).field);
            Assert.IsTrue(vm.CanGoNext);
            vm.Dispose();
        }

        [Test]
        public async Task LoadQuestionsAsync_WhileRequestPending_IsLoading()
        {
            var pending = new TaskCompletionSource<(OnboardingSurveyQuestion[] Result, ApiErrorResponse Error)>();
            _gamification.Setup(g => g.GetOnboardingSurveyAsync()).Returns(pending.Task);
            var vm = new OnboardingSurveyViewModel(_storeService, _gamification.Object, _auth.Object);

            var load = vm.LoadQuestionsAsync();
            Assert.IsTrue(vm.IsLoading);
            pending.SetResult((ThreeQuestions, null));
            await load;

            Assert.IsFalse(vm.IsLoading);
            vm.Dispose();
        }

        [Test]
        public async Task QuestionSteps_CanBeSkipped()
        {
            var vm = CreateVm();
            await vm.GoNextAsync();

            Assert.AreEqual(1, vm.CurrentStepIndex);
            Assert.IsTrue(vm.CanGoNext, "every question is optional");
            vm.Dispose();
        }

        [Test]
        public void LoadFails_ShowsErrorAndNeverAllowsSubmittingAnUnseenSurvey()
        {
            var error = new ApiErrorResponse { statusCode = 500, message = "down" };
            _gamification.Setup(g => g.GetOnboardingSurveyAsync()).ReturnsAsync(((OnboardingSurveyQuestion[])null, error));

            var vm = CreateVm();

            Assert.AreSame(error, vm.ErrorDetail);
            Assert.AreEqual(1, vm.StepCount);
            Assert.IsFalse(vm.CanGoNext);
            _gamification.Verify(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()), Times.Never);
            vm.Dispose();
        }

        [Test]
        public async Task LoadFails_ThenRetry_LoadsTheQuestions()
        {
            _gamification.Setup(g => g.GetOnboardingSurveyAsync())
                .ReturnsAsync(((OnboardingSurveyQuestion[])null, new ApiErrorResponse { statusCode = 500 }));
            var vm = CreateVm();
            Assert.IsFalse(vm.HasQuestions, "nothing loaded: the error dialog offers a retry");

            _gamification.Setup(g => g.GetOnboardingSurveyAsync()).ReturnsAsync((ThreeQuestions, (ApiErrorResponse)null));
            await vm.LoadQuestionsAsync();

            Assert.IsTrue(vm.HasQuestions);
            Assert.AreEqual(4, vm.StepCount);
            Assert.IsTrue(vm.CanGoNext);
            vm.Dispose();
        }

        [Test]
        public async Task OverlappingLoads_TheLatestRequestWins()
        {
            var older = new TaskCompletionSource<(OnboardingSurveyQuestion[] Result, ApiErrorResponse Error)>();
            var newer = new TaskCompletionSource<(OnboardingSurveyQuestion[] Result, ApiErrorResponse Error)>();
            _gamification.SetupSequence(g => g.GetOnboardingSurveyAsync()).Returns(older.Task).Returns(newer.Task);
            var vm = new OnboardingSurveyViewModel(_storeService, _gamification.Object, _auth.Object);

            var first = vm.LoadQuestionsAsync();
            var second = vm.LoadQuestionsAsync();
            newer.SetResult((ThreeQuestions, null));
            await second;
            older.SetResult((new[] { Q("usesLeftovers", "NEVER") }, null));
            await first;

            Assert.AreEqual(3, vm.Questions.Count, "a slower response for the previous language must not replace the new one");
            Assert.AreEqual(4, vm.StepCount);
            Assert.IsFalse(vm.IsLoading);
            vm.Dispose();
        }

        [Test]
        public async Task Reload_WithFewerQuestions_KeepsTheCurrentStepInRange()
        {
            var vm = CreateVm();
            await vm.GoToStepAsync(3);
            _gamification.Setup(g => g.GetOnboardingSurveyAsync())
                .ReturnsAsync((new[] { Q("usesLeftovers", "NEVER") }, (ApiErrorResponse)null));

            await vm.LoadQuestionsAsync();

            Assert.AreEqual(2, vm.StepCount);
            Assert.AreEqual(1, vm.CurrentStepIndex);
            Assert.IsTrue(vm.IsLastStep);
            vm.Dispose();
        }

        [Test]
        public async Task LanguageChange_ReloadsQuestions_AndKeepsAnswers()
        {
            var vm = CreateVm();
            vm.SetAnswer("usesLeftovers", "OFTEN");
            _gamification.Invocations.Clear();

            // Dispatch, not SetAppState: TestStoreService.SetAppState rebuilds the store and drops the VM's subscription
            _storeService.store.Dispatch(AppActions.setLanguage.Invoke("en"));
            await Task.Yield();

            _gamification.Verify(g => g.GetOnboardingSurveyAsync(), Times.AtLeastOnce);
            Assert.AreEqual("OFTEN", vm.GetAnswer("usesLeftovers"));
            vm.Dispose();
        }

        [Test]
        public async Task Complete_SubmitsOnlyAnsweredFields_StoresSegment_AndGoesToLevelsProposal()
        {
            var vm = CreateVm();
            vm.SetAnswer("weeklyMeatConsumption", "FIVE_TO_NINE");
            vm.SetAnswer("wholeGrainFrequency", "DAILY");
            string action = null;
            Unity.AppUI.Navigation.Argument[] args = null;
            vm.NavigationRequested += (a, ar) => { action = a; args = ar; };

            await GoToEndAsync(vm);

            _gamification.Verify(g => g.SubmitOnboardingSurveyAsync(It.Is<OnboardingSurveyData>(d =>
                d.Count == 2 && d.Get("weeklyMeatConsumption") == "FIVE_TO_NINE" && d.Get("wholeGrainFrequency") == "DAILY")), Times.Once);
            _auth.Verify(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Never);
            Assert.AreEqual("BEGINNER", _storeService.GetAppState().userSegment);
            Assert.AreEqual("DAILY", _storeService.GetAppState().userOnboardingSurvey.Get("wholeGrainFrequency"));
            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_dimension_levels, action);
            Assert.IsTrue(args.Any(a => a.name == "mode" && (string)a.value == "proposal"));
            Assert.IsTrue(args.Any(a => a.name == "fromHome" && (string)a.value == "false"));
            vm.Dispose();
        }

        [Test]
        public async Task Complete_RefreshesBackendLevelsBeforeTheProposal()
        {
            _gamification.Setup(g => g.GetDimensionLevelsAsync()).ReturnsAsync((new[] { new DimensionLevelEntry(DimensionCode.FoodWaste, "ADVANCED") }, (ApiErrorResponse)null));
            var vm = CreateVm();
            string levelWhenNavigating = null;
            vm.NavigationRequested += (a, _) => levelWhenNavigating = DimensionLevels.GetLevel(_storeService.GetAppState(), DimensionCode.FoodWaste);

            await GoToEndAsync(vm);

            Assert.AreEqual("ADVANCED", levelWhenNavigating);
            vm.Dispose();
        }

        [Test]
        public async Task Complete_NothingAnswered_StillSubmits()
        {
            var vm = CreateVm();

            await GoToEndAsync(vm);

            _gamification.Verify(g => g.SubmitOnboardingSurveyAsync(It.Is<OnboardingSurveyData>(d => d.Count == 0)), Times.Once);
            vm.Dispose();
        }

        [Test]
        public async Task Complete_AlreadySubmitted_FallsBackToPatch()
        {
            _gamification.Setup(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()))
                .ReturnsAsync(((OnboardingSurveyResult)null, new ApiErrorResponse { statusCode = 409 }));
            var vm = CreateVm();
            vm.FromHome = true;
            vm.SetAnswer("usesLeftovers", "OFTEN");
            string action = null;
            Unity.AppUI.Navigation.Argument[] args = null;
            vm.NavigationRequested += (a, ar) => { action = a; args = ar; };

            await GoToEndAsync(vm);

            _auth.Verify(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r => r.preferences.onboardingSurvey.Get("usesLeftovers") == "OFTEN")), Times.Once);
            Assert.IsNull(vm.ErrorDetail);
            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_dimension_levels, action);
            Assert.IsTrue(args.Any(a => a.name == "fromHome" && (string)a.value == "true"));
            vm.Dispose();
        }

        [Test]
        public async Task Complete_SubmitFails_SetsErrorAndStays()
        {
            var error = new ApiErrorResponse { statusCode = 500, message = "Server error occurred" };
            _gamification.Setup(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()))
                .ReturnsAsync(((OnboardingSurveyResult)null, error));
            var vm = CreateVm();
            string action = null;
            vm.NavigationRequested += (a, _) => action = a;

            await GoToEndAsync(vm);

            Assert.AreSame(error, vm.ErrorDetail);
            Assert.IsNull(action);
            _auth.Verify(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Never);
            vm.Dispose();
        }

        [Test]
        public async Task Complete_PatchFails_SetsErrorDetail()
        {
            _gamification.Setup(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()))
                .ReturnsAsync(((OnboardingSurveyResult)null, new ApiErrorResponse { statusCode = 409 }));
            var error = new ApiErrorResponse { statusCode = 500, message = "Server error occurred" };
            _auth.Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>())).ReturnsAsync((false, error));
            var vm = CreateVm();

            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, "[OnboardingSurveyViewModel] Failed to sync survey data with server via PATCH");
            await GoToEndAsync(vm);

            Assert.AreEqual("Server error occurred", vm.ErrorDetail.message);
            vm.Dispose();
        }

        [Test]
        public void Close_GoesHomeWithoutSaving()
        {
            var vm = CreateVm();
            string requestedAction = null;
            vm.NavigationRequested += (action, args) => requestedAction = action;
            _storeService.DispatchedActionTypes.Clear();

            vm.Close();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_home, requestedAction);
            CollectionAssert.DoesNotContain(_storeService.DispatchedActionTypes, "app/setOnboardingSurvey");
            vm.Dispose();
        }
    }
}
