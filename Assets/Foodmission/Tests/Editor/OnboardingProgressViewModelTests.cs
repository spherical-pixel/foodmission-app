using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class OnboardingProgressViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<IAuthService> _authServiceMock;
        private OnboardingRun _run;
        private OnboardingProgressViewModel _vm;
        private List<(string Action, Argument[] Args)> _navigations;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _authServiceMock = new Mock<IAuthService>();
            _authServiceMock
                .Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .ReturnsAsync((true, null));
            _run = new OnboardingRun();
            _run.Start();
            _navigations = new List<(string, Argument[])>();

            _vm = new OnboardingProgressViewModel(_storeService, _run, _authServiceMock.Object);
            _vm.NavigationRequested += (action, args) => _navigations.Add((action, args));
            _vm.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        private static string ArgValue(Argument[] args, string name)
        {
            return args?.FirstOrDefault(a => a.name == name)?.value?.ToString();
        }

        [Test]
        public void Refresh_FreshUser_ProfileCurrent()
        {
            _vm.Refresh();

            Assert.AreEqual(OnboardingStep.Profile, _vm.CurrentStep);
            Assert.AreEqual(OnboardingStepState.Current, _vm.StepStates[(int)OnboardingStep.Profile]);
            Assert.IsTrue(_vm.CanGoNext);
            Assert.IsEmpty(_navigations);
        }

        [TestCase(OnboardingStep.Profile, Actions.register_to_onboarding)]
        [TestCase(OnboardingStep.Goals, Actions.go_to_onboarding_goals)]
        [TestCase(OnboardingStep.Survey, Actions.onboardingprofile_to_onboarding_survey)]
        [TestCase(OnboardingStep.Avatar, Actions.go_to_avatar_editor)]
        public async Task Continue_EachStep_RaisesItsAction(OnboardingStep step, string expectedAction)
        {
            for (int i = 0; i < (int)step; i++)
            {
                _run.MarkSkipped((OnboardingStep)i);
            }
            _vm.Refresh();

            await _vm.GoNextAsync();

            Assert.AreEqual(1, _navigations.Count);
            Assert.AreEqual(expectedAction, _navigations[0].Action);
            if (step == OnboardingStep.Goals)
            {
                Assert.AreEqual("false", ArgValue(_navigations[0].Args, "fromHome"));
                Assert.AreEqual("false", ArgValue(_navigations[0].Args, "fromEditProfile"));
            }
            if (step == OnboardingStep.Avatar)
            {
                Assert.AreEqual("true", ArgValue(_navigations[0].Args, "fromOnboarding"));
            }
        }

        [Test]
        public async Task SkipCurrent_Profile_MarksProfileSkippedAndGoalsCurrent()
        {
            _vm.Refresh();

            await _vm.SkipCurrentAsync();

            _authServiceMock.Verify(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r =>
                r.preferences != null && !string.IsNullOrEmpty(r.preferences.onboardingProfileSkippedAt))), Times.Once);
            Assert.Contains("app/setSkippedExtendedProfile", _storeService.DispatchedActionTypes);
            Assert.AreEqual(OnboardingStep.Goals, _vm.CurrentStep);
            Assert.AreEqual(OnboardingStepState.Skipped, _vm.StepStates[(int)OnboardingStep.Profile]);
            Assert.IsEmpty(_navigations);
        }

        [Test]
        public async Task SkipCurrent_Goals_NoBackendCall()
        {
            _run.MarkDone(OnboardingStep.Profile);
            _vm.Refresh();

            await _vm.SkipCurrentAsync();

            _authServiceMock.Verify(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Never);
            Assert.AreEqual(OnboardingStep.Survey, _vm.CurrentStep);
        }

        [Test]
        public async Task SkipCurrent_Avatar_EndsRunAndGoesHome()
        {
            _run.MarkDone(OnboardingStep.Profile);
            _run.MarkDone(OnboardingStep.Goals);
            _run.MarkDone(OnboardingStep.Survey);
            _vm.Refresh();

            await _vm.SkipCurrentAsync();

            Assert.IsFalse(_run.IsActive);
            Assert.AreEqual(Actions.go_to_home, _navigations.Single().Action);
        }

        [Test]
        public void Refresh_NothingLeft_EndsRunAndGoesHome()
        {
            _run.MarkDone(OnboardingStep.Profile);
            _run.MarkDone(OnboardingStep.Goals);
            _run.MarkDone(OnboardingStep.Survey);
            _run.MarkSkipped(OnboardingStep.Avatar);

            _vm.Refresh();

            Assert.IsNull(_vm.CurrentStep);
            Assert.IsFalse(_run.IsActive);
            Assert.AreEqual(Actions.go_to_home, _navigations.Single().Action);
        }

        [Test]
        public async Task Close_ProfilePending_MarksProfileSkipped_EndsRun_GoesHome()
        {
            _vm.Refresh();

            await _vm.CloseAsync();

            _authServiceMock.Verify(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Once);
            Assert.IsFalse(_run.IsActive);
            Assert.AreEqual(Actions.go_to_home, _navigations.Single().Action);
        }

        [Test]
        public async Task Close_ProfileDone_DoesNotMarkProfileSkipped()
        {
            _run.MarkDone(OnboardingStep.Profile);
            _vm.Refresh();

            await _vm.CloseAsync();

            _authServiceMock.Verify(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Never);
            Assert.AreEqual(Actions.go_to_home, _navigations.Single().Action);
        }

        [Test]
        public async Task SkipCurrent_WhileSubmitting_Ignored()
        {
            var pending = new TaskCompletionSource<(bool, ApiErrorResponse)>();
            _authServiceMock
                .Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .Returns(pending.Task);
            _vm.Refresh();

            Task first = _vm.SkipCurrentAsync();
            Task second = _vm.SkipCurrentAsync();
            pending.SetResult((true, null));
            await Task.WhenAll(first, second);

            _authServiceMock.Verify(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Once);
            Assert.AreEqual(OnboardingStep.Goals, _vm.CurrentStep);
        }

        [Test]
        public async Task SkipCurrent_DoubleTapOnGoals_SkipsOnlyGoals()
        {
            _run.MarkDone(OnboardingStep.Profile);
            _vm.Refresh();

            await _vm.SkipCurrentAsync();
            await _vm.SkipCurrentAsync();

            Assert.AreEqual(OnboardingStep.Survey, _vm.CurrentStep);
            Assert.AreEqual(OnboardingStepState.Current, _vm.StepStates[(int)OnboardingStep.Survey]);
        }
    }
}
