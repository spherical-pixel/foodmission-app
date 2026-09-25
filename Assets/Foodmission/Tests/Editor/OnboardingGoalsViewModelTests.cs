using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class OnboardingGoalsViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<IAuthService> _authServiceMock;
        private Mock<IDimensionService> _dimensionServiceMock;
        private OnboardingGoalsViewModel _vm;
        private List<string> _dispatchedActions;
        private string _lastNavigatedAction;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _authServiceMock = new Mock<IAuthService>();
            _dimensionServiceMock = new Mock<IDimensionService>();
            _dispatchedActions = _storeService.DispatchedActionTypes;
            _lastNavigatedAction = null;

            _authServiceMock
                .Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .ReturnsAsync((true, null));

            _dimensionServiceMock
                .Setup(d => d.IsLoaded)
                .Returns(true);

            _vm = new OnboardingGoalsViewModel(_storeService, _authServiceMock.Object, _dimensionServiceMock.Object);
            _vm.NavigationRequested += (action, args) => _lastNavigatedAction = action;
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Initialize_SetsStepCountSevenAndStartsAtStepZero()
        {
            _vm.Initialize();

            Assert.AreEqual(7, _vm.StepCount);
            Assert.AreEqual(0, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.IsFirstStep);
            Assert.IsFalse(_vm.IsLastStep);
            Assert.IsFalse(_vm.CanGoPrevious);
            Assert.IsTrue(_vm.CanGoNext);
        }

        [Test]
        public void Initialize_WithNoPriorGoals_SelectsAll26TopicsByDefault()
        {
            _vm.Initialize();

            Assert.AreEqual(26, _vm.SelectedTopicCodes.Count);
            foreach (var code in DimensionGoalsCatalog.AllTopicCodes)
            {
                Assert.IsTrue(_vm.IsTopicSelected(code));
            }
        }

        [Test]
        public void Initialize_WithPriorGoalsInAppState_SelectsOnlyPriorGoals()
        {
            _storeService.SetAppState(new AppState
            {
                userGoals = new[] { TopicCode.ReducingMeatConsumption, TopicCode.WaterUse }
            });

            _vm.Initialize();

            Assert.AreEqual(2, _vm.SelectedTopicCodes.Count);
            Assert.IsTrue(_vm.IsTopicSelected(TopicCode.ReducingMeatConsumption));
            Assert.IsTrue(_vm.IsTopicSelected(TopicCode.WaterUse));
            Assert.IsFalse(_vm.IsTopicSelected(TopicCode.Sugar));
        }

        [Test]
        public void SetTopicSelected_AndToggleTopic_ModifySelection()
        {
            _vm.Initialize();
            Assert.IsTrue(_vm.IsTopicSelected(TopicCode.ReducingMeatConsumption));

            _vm.SetTopicSelected(TopicCode.ReducingMeatConsumption, false);
            Assert.IsFalse(_vm.IsTopicSelected(TopicCode.ReducingMeatConsumption));

            _vm.ToggleTopic(TopicCode.ReducingMeatConsumption);
            Assert.IsTrue(_vm.IsTopicSelected(TopicCode.ReducingMeatConsumption));
        }

        [Test]
        public async Task StepProgression_Steps0to5_CanAdvanceEvenWithZeroSelections()
        {
            _vm.Initialize();
            // Deselect all
            foreach (var code in DimensionGoalsCatalog.AllTopicCodes)
            {
                _vm.SetTopicSelected(code, false);
            }
            Assert.AreEqual(0, _vm.SelectedTopicCodes.Count);

            // Step 0 can advance
            Assert.IsTrue(_vm.CanGoNext);
            await _vm.GoNextAsync();

            // Steps 1 to 5 can advance even with 0 selected
            for (int i = 1; i <= 5; i++)
            {
                Assert.AreEqual(i, _vm.CurrentStepIndex);
                Assert.IsTrue(_vm.CanGoNext);
                await _vm.GoNextAsync();
            }

            // Step 6 (final step): with 0 selected overall, CanGoNext is false
            Assert.AreEqual(6, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.IsLastStep);
            Assert.IsFalse(_vm.CanGoNext);

            // Select 1 topic: CanGoNext becomes true
            _vm.SetTopicSelected(TopicCode.Protein, true);
            Assert.IsTrue(_vm.CanGoNext);
        }

        [Test]
        public async Task CompleteFlow_OnboardingMode_DispatchesReduxUpdatesProfileAndNavigatesToSurvey()
        {
            _vm.Initialize();
            _vm.FromEditProfile = false;

            // Advance through all steps to step 6
            for (int i = 0; i < 6; i++)
            {
                await _vm.GoNextAsync();
            }
            Assert.AreEqual(6, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.IsLastStep);
            Assert.IsTrue(_vm.CanGoNext);

            // Complete flow
            await _vm.GoNextAsync();

            // Verify Redux action dispatched
            Assert.IsTrue(_dispatchedActions.Contains(AppActions.setUserGoals.type));
            var state = _storeService.GetAppState();
            Assert.AreEqual(26, state.userGoals.Length);

            // Verify API called
            _authServiceMock.Verify(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(
                req => req.preferences != null && req.preferences.goals.Length == 26
            )), Times.Once);

            // Verify navigation to survey
            Assert.AreEqual(Actions.onboardinggoals_to_onboardingsurvey, _lastNavigatedAction);
        }

        [Test]
        public async Task CompleteFlow_FromEditProfile_NavigatesToEditProfile()
        {
            _vm.Initialize();
            _vm.FromEditProfile = true;

            for (int i = 0; i < 6; i++)
            {
                await _vm.GoNextAsync();
            }

            await _vm.GoNextAsync();

            Assert.AreEqual(Actions.go_to_editprofile, _lastNavigatedAction);
        }

        [Test]
        public async Task CompleteFlow_WhenFromHome_NavigatesToGoToHome()
        {
            _vm.Initialize();
            _vm.FromHome = true;

            for (int i = 0; i < 6; i++)
            {
                await _vm.GoNextAsync();
            }

            await _vm.GoNextAsync();

            Assert.AreEqual(Actions.go_to_home, _lastNavigatedAction);
        }

        [Test]
        public async Task CompleteFlow_WhenApiFails_SetsErrorDetailAndDoesNotNavigate()
        {
            _authServiceMock
                .Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .ReturnsAsync((false, new ApiErrorResponse { statusCode = 500, message = "Server error" }));

            _vm.Initialize();
            for (int i = 0; i < 6; i++)
            {
                await _vm.GoNextAsync();
            }

            await _vm.GoNextAsync();

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual("Server error", _vm.ErrorDetail.message);
            Assert.IsNull(_lastNavigatedAction);
        }

        [Test]
        public void GetTopicDisplayName_WhenDimensionServiceHasTopic_ReturnsBackendLocalizedName()
        {
            _dimensionServiceMock
                .Setup(d => d.GetTopic(TopicCode.ReducingMeatConsumption))
                .Returns(new Topic { code = TopicCode.ReducingMeatConsumption, name = "Reducción del consumo de carne" });

            string name = _vm.GetTopicDisplayName(TopicCode.ReducingMeatConsumption);

            Assert.AreEqual("Reducción del consumo de carne", name);
        }

        [Test]
        public void GetTopicDisplayName_WhenDimensionServiceReturnsNull_FallsBackToTopicCode()
        {
            _dimensionServiceMock
                .Setup(d => d.GetTopic(It.IsAny<string>()))
                .Returns((Topic)null);

            string name = _vm.GetTopicDisplayName(TopicCode.ReducingMeatConsumption);

            Assert.AreEqual(TopicCode.ReducingMeatConsumption, name);
        }

        [Test]
        public async Task EnsureDimensionsLoadedAsync_WhenNotLoaded_CallsPreloadAndInvokesEvent()
        {
            _dimensionServiceMock.Setup(d => d.IsLoaded).Returns(false);
            _dimensionServiceMock
                .Setup(d => d.PreloadAsync(null, false))
                .ReturnsAsync((new[] { new Dimension { code = "DIET_CHANGES" } }, (ApiErrorResponse)null));

            bool eventFired = false;
            _vm.OnDimensionsLoaded += () => eventFired = true;

            await _vm.EnsureDimensionsLoadedAsync();

            _dimensionServiceMock.Verify(d => d.PreloadAsync(null, false), Times.Once);
            Assert.IsTrue(eventFired);
        }
    }
}
