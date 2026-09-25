using System.Linq;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class HomeScreenViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<IAudioService> _mockAudioService;
        private Mock<INotificationService> _mockNotificationService;
        private Mock<ILegalService> _mockLegalService;
        private HomeScreenViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _mockAudioService = new Mock<IAudioService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLegalService = new Mock<ILegalService>();

            _vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                _mockNotificationService.Object,
                _mockLegalService.Object
            );
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
            PlayerPrefs.DeleteKey("last_seen_gamif_ts_test-user");
            PlayerPrefs.DeleteKey("celebrated_gamif_ids_test-user");
        }

        [Test]
        public async Task CheckPendingLegalConsentAsync_ReturnsStatusFromService()
        {
            var expectedStatus = new LegalConsentStatus
            {
                mustAccept = true,
                documents = new[]
                {
                    new PendingLegalConsent
                    {
                        docType = LegalDocType.TermsOfService,
                        documentKey = "TERMS_OF_SERVICE:1.0:es",
                        accepted = false
                    }
                }
            };

            _mockLegalService.Setup(s => s.GetConsentStatusAsync(It.IsAny<string>()))
                .ReturnsAsync((expectedStatus, (ApiErrorResponse)null));

            var result = await _vm.CheckPendingLegalConsentAsync();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.mustAccept);
            Assert.AreEqual(1, result.documents.Length);
            Assert.IsFalse(result.documents[0].accepted);
        }

        [Test]
        public async Task AcceptLegalConsentAsync_ReturnsTrueOnSuccess()
        {
            _mockLegalService.Setup(s => s.AcceptConsentAsync("TERMS_OF_SERVICE:1.0:es"))
                .ReturnsAsync((new AcceptLegalConsentResponse { accepted = true }, (ApiErrorResponse)null));

            bool success = await _vm.AcceptLegalConsentAsync("TERMS_OF_SERVICE:1.0:es");

            Assert.IsTrue(success);
        }

        [Test]
        public async Task GetPilotConsentFormAsync_LoadsFromCatalogService()
        {
            var mockCatalog = new Mock<ICatalogService>();
            var mockPilot = new Mock<IPilotSurveyService>();

            mockCatalog.Setup(c => c.GetConsentFormAsync("de", It.IsAny<string>()))
                .ReturnsAsync((new ConsentFormData { countryCode = "de", content = "# Pilot Consent MD" }, (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userCountry = "de", lang = "de" });

            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                _mockNotificationService.Object,
                _mockLegalService.Object,
                mockPilot.Object,
                mockCatalog.Object
            );

            var (content, error) = await vm.GetPilotConsentFormAsync();
            Assert.IsNull(error);
            Assert.AreEqual("# Pilot Consent MD", content);
        }

        [Test]
        public async Task LoadActiveQuestAsync_WithoutQuestId_SetsHasActiveQuestFalse()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });

            await _vm.LoadActiveQuestAsync();

            Assert.IsFalse(_vm.HasActiveQuest);
            Assert.IsEmpty(_vm.CurrentQuestTitle);
            Assert.IsEmpty(_vm.CurrentQuestActivityStates);
        }

        [Test]
        public async Task LoadActiveQuestAsync_WithValidQuest_LoadsTitleAndActivityStates()
        {
            var mockQuestService = new Mock<IQuestService>();
            var quest = new Quest
            {
                id = "quest-1",
                code = "HEALTHY_BREAKFAST",
                title = "Healthy Breakfast",
                items = new[]
                {
                    new QuestItem { id = "item-1", contentType = QuestContentType.Quiz },
                    new QuestItem { id = "item-2", contentType = QuestContentType.FoodFact },
                    new QuestItem { id = "item-3", contentType = QuestContentType.Mission },
                    new QuestItem { id = "item-4", contentType = QuestContentType.Challenge }
                }
            };
            var progress = new QuestProgress
            {
                questId = "quest-1",
                progress = 50f,
                completed = false
            };

            mockQuestService.Setup(q => q.GetQuestAsync("quest-1", It.IsAny<string>()))
                .ReturnsAsync((quest, (ApiErrorResponse)null));
            mockQuestService.Setup(q => q.GetQuestProgressAsync("quest-1", It.IsAny<string>()))
                .ReturnsAsync((progress, (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userCurrentQuestId = "quest-1" });

            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                _mockNotificationService.Object,
                _mockLegalService.Object,
                questService: mockQuestService.Object
            );

            await vm.LoadActiveQuestAsync();

            Assert.IsTrue(vm.HasActiveQuest);
            Assert.AreEqual("Healthy Breakfast", vm.CurrentQuestTitle);
            Assert.AreEqual("HEALTHY_BREAKFAST", vm.CurrentQuestCode);
            Assert.AreEqual("quest-1", vm.CurrentQuestId);
            Assert.AreEqual(4, vm.CurrentQuestActivityStates.Length);
            // 50% of 4 items = 2 items completed
            Assert.IsTrue(vm.CurrentQuestActivityStates[0]);
            Assert.IsTrue(vm.CurrentQuestActivityStates[1]);
            Assert.IsFalse(vm.CurrentQuestActivityStates[2]);
            Assert.IsFalse(vm.CurrentQuestActivityStates[3]);
        }

        [Test]
        public async Task LoadActiveQuestAsync_WithReadFoodFact_MarksFoodFactActivityStateCompleted()
        {
            var mockQuestService = new Mock<IQuestService>();
            var mockFoodFactService = new Mock<IFoodFactService>();

            var quest = new Quest
            {
                id = "quest-1",
                code = "HEALTHY_BREAKFAST",
                title = "Healthy Breakfast",
                items = new[]
                {
                    new QuestItem { id = "item-1", contentType = QuestContentType.Quiz, contentCode = "Q1" },
                    new QuestItem { id = "item-2", contentType = QuestContentType.FoodFact, contentCode = "FF1.1.1" },
                    new QuestItem { id = "item-3", contentType = QuestContentType.Mission, contentCode = "M1" }
                }
            };
            var progress = new QuestProgress
            {
                questId = "quest-1",
                progress = 0f,
                completed = false
            };

            mockQuestService.Setup(q => q.GetQuestAsync("quest-1", It.IsAny<string>()))
                .ReturnsAsync((quest, (ApiErrorResponse)null));
            mockQuestService.Setup(q => q.GetQuestProgressAsync("quest-1", It.IsAny<string>()))
                .ReturnsAsync((progress, (ApiErrorResponse)null));

            var foodFactProgress = new[]
            {
                new FoodFactProgressResponse
                {
                    foodFactCode = "FF1.1.1",
                    readAt = "2026-09-17T10:00:00Z"
                }
            };
            mockFoodFactService.Setup(s => s.GetUserProgressListAsync())
                .ReturnsAsync((foodFactProgress, (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userCurrentQuestId = "quest-1" });

            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                _mockNotificationService.Object,
                _mockLegalService.Object,
                questService: mockQuestService.Object,
                foodFactService: mockFoodFactService.Object
            );

            await vm.LoadActiveQuestAsync();

            Assert.IsTrue(vm.HasActiveQuest);
            Assert.AreEqual(3, vm.CurrentQuestActivityStates.Length);
            Assert.IsFalse(vm.CurrentQuestActivityStates[0]); // Quiz pending
            Assert.IsTrue(vm.CurrentQuestActivityStates[1]);  // Food fact read -> true!
            Assert.IsFalse(vm.CurrentQuestActivityStates[2]); // Mission pending
        }

        [Test]
        public void OpenCurrentQuest_RequestsNavigationToQuestDetail()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) => requestedAction = action;

            _vm.SetCurrentQuestForTesting("Healthy Breakfast", "HEALTHY_BREAKFAST", "quest-1", new[] { true, false });
            _vm.OpenCurrentQuest();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.open_quest, requestedAction);
        }

        [Test]
        public void NavigateToQuests_RequestsNavigationToQuests()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) => requestedAction = action;

            _vm.NavigateToQuests();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_quests, requestedAction);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenNoUserOrToken_ReturnsNull()
        {
            var mockGamification = new Mock<IGamificationService>();
            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                gamificationService: mockGamification.Object
            );

            _storeService.SetAppState(new AppState { userId = null, accessToken = null });

            var result = await vm.CheckPendingGamificationRewardsAsync();
            Assert.IsNull(result);
            mockGamification.Verify(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_FirstRun_EstablishesCursorAndReturnsNull()
        {
            var mockGamification = new Mock<IGamificationService>();
            var profile = new GamificationProfileResponse
            {
                userId = "test-user",
                recentEvents = new[]
                {
                    new UserEvent { id = "ev-1", eventType = "MISSION_COMPLETED", timestamp = "2026-09-23T10:00:00Z" }
                }
            };
            mockGamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((profile, (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123" });
            PlayerPrefs.DeleteKey("last_seen_gamif_ts_test-user");

            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                gamificationService: mockGamification.Object
            );

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNull(result);
            string savedCursor = PlayerPrefs.GetString("last_seen_gamif_ts_test-user", "");
            Assert.AreEqual("2026-09-23T10:00:00Z", savedCursor);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenMissionCompleted_ReturnsCelebration()
        {
            var mockGamification = new Mock<IGamificationService>();
            var profile = new GamificationProfileResponse
            {
                userId = "test-user",
                recentEvents = new[]
                {
                    new UserEvent
                    {
                        id = "ev-mission-1",
                        eventType = "MISSION_COMPLETED",
                        timestamp = "2026-09-23T10:05:00Z",
                        metadata = JObject.FromObject(new { missionCode = "M.B1.1" })
                    }
                },
                recentWalletEntries = new[]
                {
                    new WalletEntry
                    {
                        id = "w-1",
                        currency = "XP",
                        amount = 50,
                        reason = "Mission M.B1.1 completed"
                    },
                    new WalletEntry
                    {
                        id = "w-2",
                        currency = "POINTS",
                        amount = 10,
                        reason = "Mission M.B1.1 completed"
                    }
                }
            };

            mockGamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((profile, (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123" });
            PlayerPrefs.SetString("last_seen_gamif_ts_test-user", "2026-09-23T10:00:00Z");

            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                gamificationService: mockGamification.Object
            );

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("@UI:MISSION_REWARD_TITLE", result[0].ContextTitle);
            Assert.AreEqual(50, result[0].Reward.xp);
            Assert.AreEqual(10, result[0].Reward.points);
            Assert.AreEqual("M.B1.1", result[0].Code);
            Assert.IsFalse(result[0].IsQuest);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenQuizOrFoodFactEvent_IgnoresAndReturnsNull()
        {
            var mockGamification = new Mock<IGamificationService>();
            var profile = new GamificationProfileResponse
            {
                userId = "test-user",
                recentEvents = new[]
                {
                    new UserEvent
                    {
                        id = "ev-quiz-1",
                        eventType = "QUIZ_ANSWERED",
                        timestamp = "2026-09-23T10:05:00Z",
                        metadata = JObject.FromObject(new { quizCode = "Q.1" })
                    },
                    new UserEvent
                    {
                        id = "ev-fact-1",
                        eventType = "LEARNING_FACT_READ",
                        timestamp = "2026-09-23T10:06:00Z",
                        metadata = JObject.FromObject(new { factCode = "FF.1" })
                    }
                },
                recentWalletEntries = new[]
                {
                    new WalletEntry
                    {
                        id = "w-quiz",
                        currency = "XP",
                        amount = 20,
                        reason = "Quiz Q.1 completed"
                    }
                }
            };

            mockGamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((profile, (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123" });
            PlayerPrefs.SetString("last_seen_gamif_ts_test-user", "2026-09-23T10:00:00Z");

            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                gamificationService: mockGamification.Object
            );

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNull(result, "Quiz and food fact events must be ignored with zero collision!");
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenChainedEvents_OrdersActivitiesBeforeQuest()
        {
            var mockGamification = new Mock<IGamificationService>();
            var profile = new GamificationProfileResponse
            {
                userId = "test-user",
                recentEvents = new[]
                {
                    new UserEvent
                    {
                        id = "ev-quest",
                        eventType = "QUEST_COMPLETED",
                        timestamp = "2026-09-23T10:05:02Z",
                        metadata = JObject.FromObject(new { questCode = "QUEST.1" })
                    },
                    new UserEvent
                    {
                        id = "ev-mission",
                        eventType = "MISSION_COMPLETED",
                        timestamp = "2026-09-23T10:05:00Z",
                        metadata = JObject.FromObject(new { missionCode = "M.1" })
                    },
                    new UserEvent
                    {
                        id = "ev-challenge",
                        eventType = "CHALLENGE_COMPLETED",
                        timestamp = "2026-09-23T10:05:01Z",
                        metadata = JObject.FromObject(new { challengeCode = "CH.1" })
                    }
                }
            };

            mockGamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((profile, (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123" });
            PlayerPrefs.SetString("last_seen_gamif_ts_test-user", "2026-09-23T10:00:00Z");

            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                gamificationService: mockGamification.Object
            );

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            // First 2 must be activities (Mission and Challenge)
            Assert.IsFalse(result[0].IsQuest);
            Assert.IsFalse(result[1].IsQuest);
            // Last must be Quest
            Assert.IsTrue(result[2].IsQuest);
            Assert.AreEqual("@UI:QUEST_REWARD_TITLE", result[2].ContextTitle);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenAlreadyCelebrated_PreventsDuplicate()
        {
            var mockGamification = new Mock<IGamificationService>();
            var profile = new GamificationProfileResponse
            {
                userId = "test-user",
                recentEvents = new[]
                {
                    new UserEvent
                    {
                        id = "ev-mission-dup",
                        eventType = "MISSION_COMPLETED",
                        timestamp = "2026-09-23T10:05:00Z",
                        metadata = JObject.FromObject(new { missionCode = "M.1" })
                    }
                }
            };

            mockGamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((profile, (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123" });
            PlayerPrefs.SetString("last_seen_gamif_ts_test-user", "2026-09-23T10:00:00Z");

            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                gamificationService: mockGamification.Object
            );

            // First call -> celebrates
            var firstResult = await vm.CheckPendingGamificationRewardsAsync();
            Assert.IsNotNull(firstResult);
            Assert.AreEqual(1, firstResult.Count);

            // Second call -> ID is in celebratedIds, must return null (no duplicate!)
            var secondResult = await vm.CheckPendingGamificationRewardsAsync();
            Assert.IsNull(secondResult);
        }

        [Test]
        public void NavigateToQuickMealLog_RequestsQuickMealLogNavigation()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) => requestedAction = action;

            _vm.NavigateToQuickMealLog();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.open_quick_meal_log, requestedAction);
        }

        [Test]
        public void OpenCurrentQuest_WhenActiveQuestPresent_RequestsOpenQuestNavigation()
        {
            _vm.SetCurrentQuestForTesting("Title", "Q.1", "q-1", new[] { true, false });
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) => requestedAction = action;

            _vm.OpenCurrentQuest();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.open_quest, requestedAction);
        }

        [Test]
        public void GetPendingOnboardingType_WhenProfileSkippedAndNotCompleted_ReturnsProfile()
        {
            _storeService.SetAppState(new AppState
            {
                hasCompletedExtendedProfile = false,
                hasSkippedExtendedProfile = true,
                userOnboardingSurvey = new OnboardingSurveyData()
            });

            var result = _vm.GetPendingOnboardingType();

            Assert.AreEqual(PendingOnboardingType.Profile, result);
        }

        [Test]
        public void GetPendingOnboardingType_WhenProfileCompletedAndSurveyNotAnswered_ReturnsSurvey()
        {
            _storeService.SetAppState(new AppState
            {
                hasCompletedExtendedProfile = true,
                hasSkippedExtendedProfile = false,
                userOnboardingSurvey = new OnboardingSurveyData()
            });

            var result = _vm.GetPendingOnboardingType();

            Assert.AreEqual(PendingOnboardingType.Survey, result);
        }

        [Test]
        public void GetPendingOnboardingType_WhenBothCompleted_ReturnsNone()
        {
            _storeService.SetAppState(new AppState
            {
                hasCompletedExtendedProfile = true,
                hasSkippedExtendedProfile = false,
                userOnboardingSurvey = new OnboardingSurveyData
                {
                    weeklyMeatConsumption = "ZERO_TO_FOUR"
                },
                userGoals = new[] { TopicCode.ReducingMeatConsumption }
            });

            var result = _vm.GetPendingOnboardingType();

            Assert.AreEqual(PendingOnboardingType.None, result);
        }

        [Test]
        public void GetPendingOnboardingType_WhenProfileNotSkippedAndNotCompleted_ReturnsNone()
        {
            _storeService.SetAppState(new AppState
            {
                hasCompletedExtendedProfile = false,
                hasSkippedExtendedProfile = false,
                userOnboardingSurvey = new OnboardingSurveyData()
            });

            var result = _vm.GetPendingOnboardingType();

            Assert.AreEqual(PendingOnboardingType.None, result);
        }

        [Test]
        public void GetPendingOnboardingType_WhenProfileAndSurveyDoneButNoGoals_ReturnsGoals()
        {
            _storeService.SetAppState(new AppState
            {
                hasCompletedExtendedProfile = true,
                hasSkippedExtendedProfile = false,
                userOnboardingSurvey = new OnboardingSurveyData
                {
                    weeklyMeatConsumption = "ZERO_TO_FOUR"
                },
                userGoals = null
            });

            var result = _vm.GetPendingOnboardingType();

            Assert.AreEqual(PendingOnboardingType.Goals, result);
        }

        [Test]
        public void GetPendingOnboardingType_WhenProfileAndSurveyDoneAndGoalsEmpty_ReturnsGoals()
        {
            _storeService.SetAppState(new AppState
            {
                hasCompletedExtendedProfile = true,
                hasSkippedExtendedProfile = false,
                userOnboardingSurvey = new OnboardingSurveyData
                {
                    weeklyMeatConsumption = "ZERO_TO_FOUR"
                },
                userGoals = System.Array.Empty<string>()
            });

            var result = _vm.GetPendingOnboardingType();

            Assert.AreEqual(PendingOnboardingType.Goals, result);
        }

        [Test]
        public void NavigateToOnboardingSurvey_RaisesNavigationWithFromHome()
        {
            string requestedAction = null;
            Unity.AppUI.Navigation.Argument[] requestedArgs = null;
            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
                requestedArgs = args;
            };

            _vm.NavigateToOnboardingSurvey();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.onboardingprofile_to_onboarding_survey, requestedAction);
            Assert.IsNotNull(requestedArgs);
            Assert.AreEqual(1, requestedArgs.Length);
            Assert.AreEqual("fromHome", requestedArgs[0].name);
            Assert.AreEqual("true", requestedArgs[0].value?.ToString());
        }

        [Test]
        public void NavigateToOnboardingGoals_RaisesNavigationWithFromHomeTrue()
        {
            string requestedAction = null;
            Unity.AppUI.Navigation.Argument[] requestedArgs = null;
            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
                requestedArgs = args;
            };

            _vm.NavigateToOnboardingGoals();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.editprofile_to_onboardinggoals, requestedAction);
            Assert.IsNotNull(requestedArgs);
            Assert.AreEqual(2, requestedArgs.Length);
            Assert.IsTrue(requestedArgs.Any(a => a.name == "fromHome" && a.value?.ToString() == "true"));
            Assert.IsTrue(requestedArgs.Any(a => a.name == "fromEditProfile" && a.value?.ToString() == "false"));
        }
    }
}
