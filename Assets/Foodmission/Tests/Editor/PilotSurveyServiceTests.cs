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
        private Mock<IAuthService> _auth;
        private PilotSurveyService _service;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                userId = "test-user-123",
                userCountry = "de",
                lang = "de",
                accessToken = "token",
                userAutoAddToPantry = true
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

            _auth = new Mock<IAuthService>();
            _auth.Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>())).ReturnsAsync((true, (ApiErrorResponse)null));

            _service = new PilotSurveyService(_mockSurveyService.Object, _storeService, _localStorageService, _auth.Object);
        }

        private static string DaysAgo(int days)
        {
            return DateTime.UtcNow.Date.AddDays(-days).ToString("yyyy-MM-dd");
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
            // Empty/null means "the current user's country" (the fixture user is in "de").
            Assert.IsTrue(_service.IsPilotCountry(""));
            Assert.IsTrue(_service.IsPilotCountry(null));

            _storeService.SetAppState(new AppState { userId = "test-user-123", userCountry = "" });
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
            state.activeDatesInCycle = new List<string> { DaysAgo(1), DaysAgo(0) };
            state.cycleStartDate = DaysAgo(1);
            _localStorageService.SetValue<string>("pilot_cycle_state_test-user-123", Newtonsoft.Json.JsonConvert.SerializeObject(state));

            var survey = await _service.GetPendingPilotSurveyAsync();
            Assert.IsNotNull(survey);
            Assert.AreEqual("second-use", survey.slug);
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_WhenPostponed_ReturnsNullUntilTomorrow()
        {
            DateTime now = new DateTime(2026, 10, 10, 20, 0, 0, DateTimeKind.Local);
            _service.NowLocal = () => now;
            await _service.AcceptPilotConsentAsync();
            var state = _service.GetCurrentCycleState();
            state.activeDatesInCycle = new List<string> { DaysAgo(1), DaysAgo(0) };
            _localStorageService.SetValue<string>("pilot_cycle_state_test-user-123", Newtonsoft.Json.JsonConvert.SerializeObject(state));

            _service.PostponeSurvey("second-use");
            Assert.IsNull(await _service.GetPendingPilotSurveyAsync(), "later today");

            var restarted = new PilotSurveyService(_mockSurveyService.Object, _storeService, _localStorageService, _auth.Object) { NowLocal = () => now };
            Assert.IsNull(await restarted.GetPendingPilotSurveyAsync(), "survives an app restart");

            restarted.NowLocal = () => now.AddDays(1).Date.AddHours(8);
            Assert.AreEqual("second-use", (await restarted.GetPendingPilotSurveyAsync())?.slug, "offered again tomorrow");
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_WhenSkipped_EvaluatesNextRule()
        {
            await _service.AcceptPilotConsentAsync();

            var state = _service.GetCurrentCycleState();
            state.activeDatesInCycle = new List<string> { DaysAgo(2), DaysAgo(1), DaysAgo(0) }; // 3 days
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

        [Test]
        public void CycleSync_KeepsAutoAddToPantry()
        {
            _service.RecordDailyUsage(); // creates the default cycle and records today: one or more PATCHes

            _auth.Verify(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r => r.preferences != null && r.preferences.pilotSurveyCycleState != null)), Times.AtLeastOnce);
            _auth.Verify(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r => r.preferences == null || !r.preferences.autoAddToPantry)), Times.Never);
        }

        [Test]
        public void CycleSync_Failed_IsRetriedOnTheNextHomeEntry()
        {
            _auth.Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>())).ReturnsAsync((false, new ApiErrorResponse { message = "offline" }));
            _service.RecordDailyUsage();
            Assert.AreEqual("true", _localStorageService.GetValue<string>(PilotSurveyService.PendingSyncKeyFor("test-user-123"), ""));

            int calls = 0;
            _auth.Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .Callback(() => calls++)
                .ReturnsAsync((true, (ApiErrorResponse)null));
            _service.OnHomeEntered(); // same day: no new usage, but the pending sync is sent

            Assert.AreEqual(1, calls);
            Assert.AreEqual("", _localStorageService.GetValue<string>(PilotSurveyService.PendingSyncKeyFor("test-user-123"), ""));
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_DoesNotRecordUsage()
        {
            await _service.AcceptPilotConsentAsync();

            await _service.GetPendingPilotSurveyAsync();

            Assert.AreEqual(0, _service.GetActiveDaysCountInCurrentCycle());
        }

        [Test]
        public async Task OnHomeEntered_RecordsToday()
        {
            await _service.AcceptPilotConsentAsync();

            _service.OnHomeEntered();

            Assert.AreEqual(1, _service.GetActiveDaysCountInCurrentCycle());
        }

        [Test]
        public void OnHomeEntered_WithoutPilotConsent_DoesNotCountTheDay()
        {
            // Days before consent would make several surveys due at once when the user finally accepts
            _service.OnHomeEntered();

            Assert.AreEqual(0, _service.GetActiveDaysCountInCurrentCycle());
        }

        [Test]
        public void Merge_SameCycle_UnionsDaysAndSurveys()
        {
            var local = new PilotSurveyCycleState
            {
                currentCycle = 1,
                cycleStartDate = "2026-10-03",
                activeDatesInCycle = new List<string> { "2026-10-03", "2026-10-05" },
                completedSlugsInCycle = new List<string> { "second-use" }
            };
            var server = new PilotSurveyCycleState
            {
                currentCycle = 1,
                cycleStartDate = "2026-10-01",
                activeDatesInCycle = new List<string> { "2026-10-01", "2026-10-05" },
                completedSlugsInCycle = new List<string> { "third-use" },
                skippedSlugsInCycle = new List<string> { "fourth-use" },
                postponedUntil = new List<PostponedSurvey> { new PostponedSurvey { slug = "fifth-use", day = "2026-10-06" } }
            };

            PilotSurveyCycleState merged = PilotSurveyService.Merge(local, server);

            Assert.AreEqual("2026-10-01", merged.cycleStartDate);
            CollectionAssert.AreEqual(new[] { "2026-10-01", "2026-10-03", "2026-10-05" }, merged.activeDatesInCycle);
            CollectionAssert.AreEquivalent(new[] { "second-use", "third-use" }, merged.completedSlugsInCycle);
            CollectionAssert.AreEquivalent(new[] { "fourth-use" }, merged.skippedSlugsInCycle);
            Assert.AreEqual("2026-10-06", merged.postponedUntil.Find(p => p.slug == "fifth-use")?.day);
        }

        [Test]
        public void Merge_HigherCycleWins()
        {
            var local = new PilotSurveyCycleState { currentCycle = 1, completedSlugsInCycle = new List<string> { "second-use" } };
            var server = new PilotSurveyCycleState { currentCycle = 2, cycleStartDate = "2026-11-01" };

            Assert.AreEqual(2, PilotSurveyService.Merge(local, server).currentCycle);
            Assert.AreEqual(0, PilotSurveyService.Merge(local, server).completedSlugsInCycle.Count);
            Assert.AreEqual(2, PilotSurveyService.Merge(server, local).currentCycle);
        }

        [Test]
        public async Task GetPendingPilotSurveyAsync_SurveyAnsweredOnAnotherDevice_IsNotOfferedAgain()
        {
            await _service.AcceptPilotConsentAsync();
            var local = _service.GetCurrentCycleState();
            local.activeDatesInCycle = new List<string> { DaysAgo(1), DaysAgo(0) };
            _localStorageService.SetValue<string>("pilot_cycle_state_test-user-123", Newtonsoft.Json.JsonConvert.SerializeObject(local));
            // Login on this device restored the server copy, where second-use was answered on another device
            var server = local.Copy();
            server.completedSlugsInCycle.Add("second-use");
            _storeService.store.Dispatch(AppActions.setPilotCycleState.Invoke(server));

            var survey = await _service.GetPendingPilotSurveyAsync();

            Assert.AreNotEqual("second-use", survey?.slug);
        }
    }
}
