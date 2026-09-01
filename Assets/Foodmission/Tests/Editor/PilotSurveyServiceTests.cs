using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class PilotSurveyServiceTests
    {
        private TestStoreService _storeService;
        private TestLocalStorageService _localStorageService;
        private Mock<ISurveyService> _mockSurveyService;
        private PilotSurveyService _service;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                userId = "test-user-123",
                userCountry = "de",
                lang = "de"
            });
            _localStorageService = new TestLocalStorageService();
            _mockSurveyService = new Mock<ISurveyService>();

            // Setup default survey mocks
            _mockSurveyService.Setup(s => s.GetSurveyBySlugAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string slug, string lang) => (
                    new SurveyDto
                    {
                        id = $"id-{slug}",
                        slug = slug,
                        title = $"Title for {slug}",
                        questions = new[]
                        {
                            new QuestionDto { id = "q1", text = "Question 1", order = 0 }
                        }
                    }, null));

            _service = new PilotSurveyService(_mockSurveyService.Object, _storeService, _localStorageService);
        }

        [Test]
        public void IsPilotCountry_ValidatesPilotCountryCodesCorrectly()
        {
            Assert.IsTrue(_service.IsPilotCountry("de"));
            Assert.IsTrue(_service.IsPilotCountry("DE"));
            Assert.IsTrue(_service.IsPilotCountry("gr"));
            Assert.IsTrue(_service.IsPilotCountry("it"));
            Assert.IsTrue(_service.IsPilotCountry("nl"));
            Assert.IsTrue(_service.IsPilotCountry("no"));
            Assert.IsTrue(_service.IsPilotCountry("si"));

            Assert.IsFalse(_service.IsPilotCountry("es"));
            Assert.IsFalse(_service.IsPilotCountry("fr"));
            Assert.IsFalse(_service.IsPilotCountry("us"));
            Assert.IsFalse(_service.IsPilotCountry(""));
            Assert.IsFalse(_service.IsPilotCountry(null));
        }

        [Test]
        public async Task HasAcceptedPilotConsentAsync_WhenAccepted_ReturnsTrue()
        {
            Assert.IsFalse(await _service.HasAcceptedPilotConsentAsync());

            await _service.AcceptPilotConsentAsync();

            Assert.IsTrue(await _service.HasAcceptedPilotConsentAsync());
        }

        [Test]
        public void RecordDailyUsage_AddsDateOnceAndDoesNotDuplicate()
        {
            _service.RecordDailyUsage();
            Assert.AreEqual(1, _service.GetActiveDaysCountInCurrentCycle());

            // Second call on the same day does not duplicate
            _service.RecordDailyUsage();
            Assert.AreEqual(1, _service.GetActiveDaysCountInCurrentCycle());
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_WhenNonPilotCountry_ReturnsNull()
        {
            _storeService.SetAppState(new AppState { userId = "user1", userCountry = "es" });
            await _service.AcceptPilotConsentAsync();

            var result = await _service.GetPendingPilotSurveyAsync();
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_WhenConsentNotAccepted_ReturnsNull()
        {
            _storeService.SetAppState(new AppState { userId = "user1", userCountry = "de" });

            var result = await _service.GetPendingPilotSurveyAsync();
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_OnDay1_ReturnsNull()
        {
            await _service.AcceptPilotConsentAsync();
            _service.RecordDailyUsage(); // 1 active day

            var result = await _service.GetPendingPilotSurveyAsync();
            Assert.IsNull(result, "Surveys start from active day 2");
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_OnDay2_ReturnsSecondUseSurvey()
        {
            await _service.AcceptPilotConsentAsync();

            // Simulate 2 active dates in cycle state
            var state = _service.GetCurrentCycleState();
            state.activeDatesInCycle = new List<string> { "2026-08-30", "2026-08-31" };
            state.cycleStartDate = "2026-08-30";
            _localStorageService.SetValue<string>("pilot_cycle_state_test-user-123", Newtonsoft.Json.JsonConvert.SerializeObject(state));

            var survey = await _service.GetPendingPilotSurveyAsync();
            Assert.IsNotNull(survey);
            Assert.AreEqual("second-use", survey.slug);
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_WhenPostponed_ReturnsNullForCurrentSession()
        {
            await _service.AcceptPilotConsentAsync();

            var state = _service.GetCurrentCycleState();
            state.activeDatesInCycle = new List<string> { "2026-08-30", "2026-08-31" };
            _localStorageService.SetValue<string>("pilot_cycle_state_test-user-123", Newtonsoft.Json.JsonConvert.SerializeObject(state));

            _service.PostponeSurvey("second-use");

            var survey = await _service.GetPendingPilotSurveyAsync();
            Assert.IsNull(survey);
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_WhenSkipped_EvaluatesNextRule()
        {
            await _service.AcceptPilotConsentAsync();

            var state = _service.GetCurrentCycleState();
            state.activeDatesInCycle = new List<string> { "2026-08-30", "2026-08-31", "2026-09-01" }; // 3 days
            _localStorageService.SetValue<string>("pilot_cycle_state_test-user-123", Newtonsoft.Json.JsonConvert.SerializeObject(state));

            _service.SkipSurvey("second-use");

            var survey = await _service.GetPendingPilotSurveyAsync();
            Assert.IsNotNull(survey);
            Assert.AreEqual("third-use", survey.slug);
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_1MonthRule_RequiresBothDaysAndDaysSinceStart()
        {
            await _service.AcceptPilotConsentAsync();

            var state = _service.GetCurrentCycleState();
            // 8 active days, but only 10 days since cycle start
            state.cycleStartDate = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-dd");
            state.activeDatesInCycle = new List<string> { "d1", "d2", "d3", "d4", "d5", "d6", "d7", "d8" };
            state.completedSlugsInCycle = new List<string> { "second-use", "third-use", "fourth-use", "fifth-use", "sixth-use", "seventh" };
            _localStorageService.SetValue<string>("pilot_cycle_state_test-user-123", Newtonsoft.Json.JsonConvert.SerializeObject(state));

            var surveyNotYet = await _service.GetPendingPilotSurveyAsync();
            Assert.IsNull(surveyNotYet, "Requires at least 30 days since cycle start");

            // Now simulate 31 days since cycle start
            state.cycleStartDate = DateTime.UtcNow.AddDays(-31).ToString("yyyy-MM-dd");
            _localStorageService.SetValue<string>("pilot_cycle_state_test-user-123", Newtonsoft.Json.JsonConvert.SerializeObject(state));

            var surveyReady = await _service.GetPendingPilotSurveyAsync();
            Assert.IsNotNull(surveyReady);
            Assert.AreEqual("after-1-mt-and-at-least-8th-use", surveyReady.slug);
        }

        [Test]
        public async Task MarkSurveyCompletedAsync_OnEndSurvey_AdvancesToNextCycle()
        {
            await _service.AcceptPilotConsentAsync();

            var state = _service.GetCurrentCycleState();
            Assert.AreEqual(1, state.currentCycle);

            await _service.MarkSurveyCompletedAsync("end", "id-end");

            var nextState = _service.GetCurrentCycleState();
            Assert.AreEqual(2, nextState.currentCycle, "Cycle should advance to 2");
            Assert.AreEqual(1, nextState.activeDatesInCycle.Count, "New cycle starts today");
            Assert.AreEqual(0, nextState.completedSlugsInCycle.Count, "Completed list is cleared for new cycle");
        }

        [Test]
        public void GetCurrentCycleState_WhenLocalStorageEmpty_RestoresFromAppState()
        {
            var serverCycleState = new PilotSurveyCycleState
            {
                currentCycle = 3,
                cycleStartDate = "2026-07-01",
                activeDatesInCycle = new List<string> { "2026-07-01", "2026-07-02", "2026-07-03" },
                completedSlugsInCycle = new List<string> { "second-use", "third-use" }
            };

            _storeService.SetAppState(new AppState
            {
                userId = "test-user-restore",
                userCountry = "de",
                pilotSurveyCycleState = serverCycleState
            });

            var restoredService = new PilotSurveyService(_mockSurveyService.Object, _storeService, _localStorageService);
            var cycle = restoredService.GetCurrentCycleState();

            Assert.IsNotNull(cycle);
            Assert.AreEqual(3, cycle.currentCycle);
            Assert.AreEqual(3, cycle.activeDatesInCycle.Count);
            Assert.IsTrue(cycle.completedSlugsInCycle.Contains("second-use"));
        }

        [Test]
        public async Task HasAcceptedPilotConsentAsync_WhenRestoredFromAppState_ReturnsTrue()
        {
            _storeService.SetAppState(new AppState
            {
                userId = "test-user-consent",
                userCountry = "de",
                pilotConsentAccepted = true
            });

            var restoredService = new PilotSurveyService(_mockSurveyService.Object, _storeService, _localStorageService);
            bool accepted = await restoredService.HasAcceptedPilotConsentAsync();

            Assert.IsTrue(accepted);
        }

        [Test]
        public void SaveCycleState_DispatchesReduxAction()
        {
            _service.RecordDailyUsage();
            Assert.IsTrue(_storeService.DispatchedActionTypes.Contains("app/setPilotCycleState"));
        }
    }
}
