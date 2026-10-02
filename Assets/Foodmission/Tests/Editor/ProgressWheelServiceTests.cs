using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProgressWheelServiceTests
    {
        private TestStoreService _store;
        private Mock<IGamificationService> _gamification;
        private Mock<IAuthService> _auth;
        private ProgressWheelService _service;

        private static readonly OnboardingSurveyData CompleteSurvey = new OnboardingSurveyData
        {
            weeklyMeatConsumption = "ZERO_TO_FOUR",
            weeklyBeefConsumption = "NEVER",
            weeklyFoodWaste = "ZERO",
            weeklyUpfConsumption = "ZERO_TO_THREE",
            weeklyReusableOrRefill = "TEN_PLUS",
        };

        private static ProgressWheel Wheel(string kind) => new ProgressWheel { kind = kind, stage = 1 };

        [SetUp]
        public void SetUp()
        {
            _store = new TestStoreService();
            _store.SetAppState(new AppState { userId = "u1", accessToken = "t", tokenType = "Bearer" });
            _gamification = new Mock<IGamificationService>();
            _auth = new Mock<IAuthService>();
            _auth.Setup(a => a.SyncSettingsAsync()).Returns(Task.CompletedTask);
            _service = new ProgressWheelService(_store, _gamification.Object, _auth.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _store?.Dispose();
        }

        [Test]
        public async Task Refresh_StoresWheels()
        {
            _gamification.Setup(g => g.GetProgressWheelsAsync())
                .ReturnsAsync((new[] { Wheel("CO2_REDUCTION"), Wheel("WATER_SAVINGS") }, (ApiErrorResponse)null));

            await _service.RefreshAsync();

            Assert.AreEqual(2, _store.GetAppState().progressWheels.Length);
            Assert.IsFalse(_service.IsLoading);
        }

        [Test]
        public async Task Refresh_Error_KeepsCache()
        {
            _store.store.Dispatch(AppActions.setProgressWheels.Invoke(new[] { Wheel("CO2_REDUCTION") }));
            _gamification.Setup(g => g.GetProgressWheelsAsync())
                .ReturnsAsync(((ProgressWheel[])null, new ApiErrorResponse { statusCode = 500 }));

            await _service.RefreshAsync();

            Assert.AreEqual(1, _store.GetAppState().progressWheels.Length);
        }

        [Test]
        public async Task Refresh_EmptyWithCompleteSurvey_RecoversOnce()
        {
            _store.store.Dispatch(AppActions.setOnboardingSurvey.Invoke(CompleteSurvey));
            // After the re-submission the backend has a segment, so the next GET returns the wheels.
            _gamification.SetupSequence(g => g.GetProgressWheelsAsync())
                .ReturnsAsync((new ProgressWheel[0], (ApiErrorResponse)null))
                .ReturnsAsync((new[] { Wheel("CO2_REDUCTION") }, (ApiErrorResponse)null));
            _gamification.Setup(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()))
                .ReturnsAsync((new OnboardingSurveyResult { segment = "BEGINNER", progressWheels = new[] { Wheel("CO2_REDUCTION") } }, (ApiErrorResponse)null));

            await _service.RefreshAsync();
            await _service.RefreshAsync();

            _gamification.Verify(g => g.SubmitOnboardingSurveyAsync(It.Is<OnboardingSurveyData>(d => d.weeklyMeatConsumption == "ZERO_TO_FOUR")), Times.Once);
            Assert.AreEqual("BEGINNER", _store.GetAppState().userSegment);
            Assert.AreEqual(1, _store.GetAppState().progressWheels.Length);
        }

        [Test]
        public async Task Refresh_EmptyWithIncompleteSurvey_DoesNotSubmit()
        {
            _gamification.Setup(g => g.GetProgressWheelsAsync())
                .ReturnsAsync((new ProgressWheel[0], (ApiErrorResponse)null));

            await _service.RefreshAsync();

            _gamification.Verify(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()), Times.Never);
            Assert.AreEqual(0, _store.GetAppState().progressWheels.Length);
        }

        [Test]
        public async Task Refresh_RecoveryFails_NotRetriedInSession()
        {
            _store.store.Dispatch(AppActions.setOnboardingSurvey.Invoke(CompleteSurvey));
            _gamification.Setup(g => g.GetProgressWheelsAsync())
                .ReturnsAsync((new ProgressWheel[0], (ApiErrorResponse)null));
            _gamification.Setup(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()))
                .ReturnsAsync(((OnboardingSurveyResult)null, new ApiErrorResponse { statusCode = 500 }));

            await _service.RefreshAsync();
            await _service.RefreshAsync();

            _gamification.Verify(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()), Times.Once);
        }

        [Test]
        public async Task Refresh_RecoveryFlagIsPerUser()
        {
            _store.store.Dispatch(AppActions.setOnboardingSurvey.Invoke(CompleteSurvey));
            _gamification.Setup(g => g.GetProgressWheelsAsync())
                .ReturnsAsync((new ProgressWheel[0], (ApiErrorResponse)null));
            _gamification.Setup(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()))
                .ReturnsAsync(((OnboardingSurveyResult)null, new ApiErrorResponse { statusCode = 500 }));

            await _service.RefreshAsync();
            AppState other = _store.GetAppState().Copy();
            other.userId = "u2";
            _store.SetAppState(other);
            await _service.RefreshAsync();

            _gamification.Verify(g => g.SubmitOnboardingSurveyAsync(It.IsAny<OnboardingSurveyData>()), Times.Exactly(2));
        }

        [Test]
        public async Task Refresh_NoToken_DoesNothing()
        {
            _store.SetAppState(new AppState { userId = "u1", accessToken = "" });

            await _service.RefreshAsync();

            _gamification.Verify(g => g.GetProgressWheelsAsync(), Times.Never);
        }

        [Test]
        public async Task Refresh_ConcurrentCalls_ShareOneRequest()
        {
            var pending = new TaskCompletionSource<(ProgressWheel[], ApiErrorResponse)>();
            _gamification.Setup(g => g.GetProgressWheelsAsync()).Returns(pending.Task);

            Task first = _service.RefreshAsync();
            Task second = _service.RefreshAsync();
            Assert.IsTrue(_service.IsLoading);
            pending.SetResult((new[] { Wheel("CO2_REDUCTION") }, null));
            await Task.WhenAll(first, second);

            _gamification.Verify(g => g.GetProgressWheelsAsync(), Times.Once);
            Assert.IsFalse(_service.IsLoading);
        }

        [Test]
        public async Task SetHidden_UpdatesStoreAndSyncsSettings()
        {
            await _service.SetHiddenAsync(new[] { "WATER_SAVINGS", "WATER_SAVINGS", "" });

            CollectionAssert.AreEqual(new[] { "WATER_SAVINGS" }, _store.GetAppState().hiddenProgressWheels);
            _auth.Verify(a => a.SyncSettingsAsync(), Times.Once);
        }

        [Test]
        public void GetVisible_FiltersHiddenAndKeepsOrder()
        {
            var wheels = new[] { Wheel("CO2_REDUCTION"), Wheel("ENERGY_REDUCTION"), Wheel("WATER_SAVINGS"), Wheel("LAND_USE_REDUCTION") };

            IReadOnlyList<ProgressWheel> visible = ProgressWheelService.GetVisible(wheels, new[] { "ENERGY_REDUCTION" });

            CollectionAssert.AreEqual(new[] { "CO2_REDUCTION", "WATER_SAVINGS", "LAND_USE_REDUCTION" }, System.Linq.Enumerable.Select(visible, w => w.kind));
            Assert.AreEqual(0, ProgressWheelService.GetVisible(null, null).Count);
        }
    }
}
