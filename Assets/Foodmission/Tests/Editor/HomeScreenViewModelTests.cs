using System.Linq;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;

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
            PlayerPrefs.DeleteKey("celebrated_quest_ids_test-user");
            PlayerPrefs.DeleteKey("celebrated_badges_test-user");
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
            // Challenge events are no longer tracked via gamification events (now handled directly via PATCH progress)
            Assert.AreEqual(2, result.Count);
            // First is Mission
            Assert.IsFalse(result[0].IsQuest);
            Assert.AreEqual("@UI:MISSION_REWARD_TITLE", result[0].ContextTitle);
            // Second is Quest
            Assert.IsTrue(result[1].IsQuest);
            Assert.AreEqual("@UI:QUEST_REWARD_TITLE", result[1].ContextTitle);
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
        public void GetPendingOnboardingType_WhenProfileAndGoalsDoneAndSurveyNotAnswered_ReturnsSurvey()
        {
            _storeService.SetAppState(new AppState
            {
                hasCompletedExtendedProfile = true,
                hasSkippedExtendedProfile = false,
                userOnboardingSurvey = new OnboardingSurveyData(),
                userGoals = new[] { TopicCode.ReducingMeatConsumption }
            });

            var result = _vm.GetPendingOnboardingType();

            Assert.AreEqual(PendingOnboardingType.Survey, result);
        }

        [Test]
        public void GetPendingOnboardingType_WhenGoalsAndSurveyPending_ReturnsGoalsFirst()
        {
            _storeService.SetAppState(new AppState
            {
                hasCompletedExtendedProfile = true,
                userOnboardingSurvey = new OnboardingSurveyData(),
                userGoals = new string[0]
            });

            Assert.AreEqual(PendingOnboardingType.Goals, _vm.GetPendingOnboardingType());
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
        public void GetPendingOnboardingType_WhenProfileNotCompleted_ReturnsProfile()
        {
            _storeService.SetAppState(new AppState
            {
                hasCompletedExtendedProfile = false,
                hasSkippedExtendedProfile = false,
                userOnboardingSurvey = new OnboardingSurveyData()
            });

            var result = _vm.GetPendingOnboardingType();

            Assert.AreEqual(PendingOnboardingType.Profile, result);
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

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_onboarding_goals, requestedAction);
            Assert.IsNotNull(requestedArgs);
            Assert.AreEqual(2, requestedArgs.Length);
            Assert.IsTrue(requestedArgs.Any(a => a.name == "fromHome" && a.value?.ToString() == "true"));
            Assert.IsTrue(requestedArgs.Any(a => a.name == "fromEditProfile" && a.value?.ToString() == "false"));
        }

        [Test]
        public void NavigateToFoodComparison_RaisesNavigationToGoToFoodComparison_WithArgs()
        {
            string requestedAction = null;
            Unity.AppUI.Navigation.Argument[] requestedArgs = null;
            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
                requestedArgs = args;
            };

            _vm.NavigateToFoodComparison(mode: "sample", source: "sample");

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_food_comparison, requestedAction);
            Assert.IsNotNull(requestedArgs);
            Assert.IsTrue(requestedArgs.Any(a => a.name == "challengeCode" && a.value?.ToString() == "CH.B1.1"));
            Assert.IsTrue(requestedArgs.Any(a => a.name == "mode" && a.value?.ToString() == "sample"));
            Assert.IsTrue(requestedArgs.Any(a => a.name == "source" && a.value?.ToString() == "sample"));
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenQuestCompleted_ResolvesNextUnlockedQuest()
        {
            var mockGamification = new Mock<IGamificationService>();
            var mockQuest = new Mock<IQuestService>();
            var questProgression = new QuestProgressionService();

            var q1 = new Quest { id = "q1", code = "QUEST.HEALTH.BEGINNER.1", title = "Health Quest 1", dimensionId = "HEALTH", level = "BEGINNER" };
            var q2 = new Quest { id = "q2", code = "QUEST.HEALTH.BEGINNER.2", title = "Health Quest 2", dimensionId = "HEALTH", level = "BEGINNER" };
            var allQuests = new[] { q1, q2 };

            mockQuest.Setup(q => q.GetQuestsAsync(null, null, null))
                .ReturnsAsync((allQuests, (ApiErrorResponse)null));

            var profile = new GamificationProfileResponse
            {
                userId = "test-user",
                recentEvents = new[]
                {
                    new UserEvent
                    {
                        id = "ev-quest-1",
                        eventType = "QUEST_COMPLETED",
                        timestamp = "2026-09-23T10:05:00Z",
                        metadata = JObject.FromObject(new { questCode = "QUEST.HEALTH.BEGINNER.1" })
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
                questService: mockQuest.Object,
                gamificationService: mockGamification.Object,
                questProgressionService: questProgression
            );

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].IsQuest);
            Assert.IsNotNull(result[0].UnlockedQuest);
            Assert.AreEqual("QUEST.HEALTH.BEGINNER.2", result[0].UnlockedQuest.code);
            Assert.AreEqual("Health Quest 2", result[0].UnlockedQuest.title);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenLastQuestCompleted_UnlockedQuestIsNull()
        {
            var mockGamification = new Mock<IGamificationService>();
            var mockQuest = new Mock<IQuestService>();
            var questProgression = new QuestProgressionService();

            var q1 = new Quest { id = "q1", code = "QUEST.HEALTH.ADVANCED.10", title = "Last Health Quest", dimensionId = "HEALTH", level = "ADVANCED" };
            var allQuests = new[] { q1 };

            mockQuest.Setup(q => q.GetQuestsAsync(null, null, null))
                .ReturnsAsync((allQuests, (ApiErrorResponse)null));

            var profile = new GamificationProfileResponse
            {
                userId = "test-user",
                recentEvents = new[]
                {
                    new UserEvent
                    {
                        id = "ev-quest-last",
                        eventType = "QUEST_COMPLETED",
                        timestamp = "2026-09-23T10:05:00Z",
                        metadata = JObject.FromObject(new { questCode = "QUEST.HEALTH.ADVANCED.10" })
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
                questService: mockQuest.Object,
                gamificationService: mockGamification.Object,
                questProgressionService: questProgression
            );

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].IsQuest);
            Assert.IsNull(result[0].UnlockedQuest);
        }

        [Test]
        public async Task CheckMissionNudgeAsync_ReturnsServiceNudge()
        {
            var nudge = MissionNudge.ForStalledMission("M.B1.4", "Swap", MissionInteractionCatalog.QuickMealLog);
            var service = new Mock<IMissionNudgeService>();
            service.Setup(s => s.GetNudgeAsync()).ReturnsAsync(nudge);
            var vm = new HomeScreenViewModel(_storeService, _mockAudioService.Object, _mockNotificationService.Object, _mockLegalService.Object,
                missionNudgeService: service.Object);

            Assert.AreSame(nudge, await vm.CheckMissionNudgeAsync());
            vm.Dispose();
        }

        [Test]
        public void OpenCheckIn_And_OpenMissionModule_Navigate()
        {
            string action = null;
            Argument[] args = null;
            _vm.NavigationRequested += (a, ar) => { action = a; args = ar; };

            _vm.OpenCheckIn("M.B1.4");
            Assert.AreEqual(Actions.open_mission_checkin, action);
            Assert.AreEqual("M.B1.4", args[0].value);

            _vm.OpenCheckIn();
            Assert.AreEqual(Actions.open_mission_checkin, action);
            Assert.AreEqual(0, args.Length, "all-missions mode has no code");

            _vm.OpenMissionModule(MissionInteractionCatalog.QuickMealLog);
            Assert.AreEqual(Actions.open_quick_meal_log, action);
        }

        // ── Completed quests that were never celebrated (2026-10-01) ─────────────────

        private static UserEvent QuestCompletedEvent(string id, string questId, string timestamp) => new UserEvent
        {
            id = id,
            eventType = "QUEST_COMPLETED",
            timestamp = timestamp,
            metadata = JObject.FromObject(new { questId, questCode = "QUEST.A.BEGINNER.1" })
        };

        private static Quest[] TwoQuests() => new[]
        {
            new Quest { id = "q1", code = "QUEST.A.BEGINNER.1", level = QuestLevel.Beginner, dimensionId = "A" },
            new Quest { id = "q2", code = "QUEST.A.BEGINNER.2", level = QuestLevel.Beginner, dimensionId = "A" }
        };

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenProcessingFails_DoesNotLoseTheEventsForTheNextCheck()
        {
            var mockGamification = new Mock<IGamificationService>();
            var mockQuests = new Mock<IQuestService>();
            mockGamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((new GamificationProfileResponse
            {
                userId = "test-user",
                recentEvents = new[]
                {
                    new UserEvent { id = "ev-session", eventType = "APP_SESSION_OPENED", timestamp = "2026-10-01T16:32:54.495Z" },
                    QuestCompletedEvent("ev-quest", "q1", "2026-10-01T16:06:05.533Z")
                }
            }, (ApiErrorResponse)null));
            mockQuests.SetupSequence(q => q.GetQuestsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new System.Exception("network"))
                .ReturnsAsync((TwoQuests(), (ApiErrorResponse)null));
            mockQuests.Setup(q => q.GetQuestProgressAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(((QuestProgress)null, (ApiErrorResponse)null));
            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123" });
            PlayerPrefs.SetString("last_seen_gamif_ts_test-user", "2026-10-01T16:05:04.760Z");
            var vm = new HomeScreenViewModel(_storeService, _mockAudioService.Object, questService: mockQuests.Object, gamificationService: mockGamification.Object);

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("network"));
            Assert.IsNull(await vm.CheckPendingGamificationRewardsAsync());
            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].IsQuest);
            Assert.AreEqual("q2", result[0].UnlockedQuest?.id);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_QuestCompletedForCurrentQuest_ClearsCurrentQuest()
        {
            var mockGamification = new Mock<IGamificationService>();
            var mockQuests = new Mock<IQuestService>();
            var mockAuth = new Mock<IAuthService>();
            mockGamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((new GamificationProfileResponse
            {
                userId = "test-user",
                recentEvents = new[] { QuestCompletedEvent("ev-quest", "q1", "2026-10-01T16:06:05.533Z") }
            }, (ApiErrorResponse)null));
            mockQuests.Setup(q => q.GetQuestsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((TwoQuests(), (ApiErrorResponse)null));
            mockQuests.Setup(q => q.GetQuestProgressAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(((QuestProgress)null, (ApiErrorResponse)null));
            mockAuth.Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>())).ReturnsAsync((true, (ApiErrorResponse)null));
            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123", userCurrentQuestId = "q1" });
            PlayerPrefs.SetString("last_seen_gamif_ts_test-user", "2026-10-01T16:05:04.760Z");
            var vm = new HomeScreenViewModel(_storeService, _mockAudioService.Object, questService: mockQuests.Object, gamificationService: mockGamification.Object, authService: mockAuth.Object);

            await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsTrue(string.IsNullOrEmpty(_storeService.GetAppState().userCurrentQuestId));
            mockAuth.Verify(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r => r.clearCurrentQuest)), Times.Once);
        }

        [Test]
        public async Task CheckCompletedActiveQuestAsync_ActiveQuestAlreadyCompleted_CelebratesClearsAndOffersNext()
        {
            var mockQuests = new Mock<IQuestService>();
            var mockAuth = new Mock<IAuthService>();
            var mockGamification = new Mock<IGamificationService>();
            mockQuests.Setup(q => q.GetQuestProgressAsync("q1", It.IsAny<string>())).ReturnsAsync((new QuestProgress
            {
                questId = "q1",
                questCode = "QUEST.A.BEGINNER.1",
                completed = true,
                progress = 100,
                reward = new ContentReward { xp = 30, points = 30 }
            }, (ApiErrorResponse)null));
            mockQuests.Setup(q => q.GetQuestsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((TwoQuests(), (ApiErrorResponse)null));
            mockAuth.Setup(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>())).ReturnsAsync((true, (ApiErrorResponse)null));
            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123", userCurrentQuestId = "q1" });
            var vm = new HomeScreenViewModel(_storeService, _mockAudioService.Object, questService: mockQuests.Object, gamificationService: mockGamification.Object, authService: mockAuth.Object);

            PendingRewardCelebration celebration = await vm.CheckCompletedActiveQuestAsync();

            Assert.IsNotNull(celebration);
            Assert.IsTrue(celebration.IsQuest);
            Assert.AreEqual(30, celebration.Reward.xp);
            Assert.AreEqual("q2", celebration.UnlockedQuest?.id);
            Assert.IsTrue(string.IsNullOrEmpty(_storeService.GetAppState().userCurrentQuestId));
            mockAuth.Verify(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r => r.clearCurrentQuest)), Times.Once);

            // The QUEST_COMPLETED event showing up later must not celebrate the same quest again
            mockGamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync((new GamificationProfileResponse
            {
                userId = "test-user",
                recentEvents = new[] { QuestCompletedEvent("ev-quest", "q1", "2026-10-01T16:06:05.533Z") }
            }, (ApiErrorResponse)null));
            PlayerPrefs.SetString("last_seen_gamif_ts_test-user", "2026-10-01T16:05:04.760Z");
            Assert.IsNull(await vm.CheckPendingGamificationRewardsAsync());
        }

        [Test]
        public async Task CheckCompletedActiveQuestAsync_ActiveQuestInProgress_DoesNothing()
        {
            var mockQuests = new Mock<IQuestService>();
            var mockAuth = new Mock<IAuthService>();
            mockQuests.Setup(q => q.GetQuestProgressAsync("q1", It.IsAny<string>())).ReturnsAsync((new QuestProgress { questId = "q1", completed = false, progress = 50 }, (ApiErrorResponse)null));
            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123", userCurrentQuestId = "q1" });
            var vm = new HomeScreenViewModel(_storeService, _mockAudioService.Object, questService: mockQuests.Object, authService: mockAuth.Object);

            Assert.IsNull(await vm.CheckCompletedActiveQuestAsync());
            Assert.AreEqual("q1", _storeService.GetAppState().userCurrentQuestId);
            mockAuth.Verify(a => a.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Never);
        }

        [Test]
        public void ProfileUpdateRequest_ClearCurrentQuest_SendsExplicitNull()
        {
            string json = new ProfileUpdateRequest { clearCurrentQuest = true }.ToJson();

            Assert.IsTrue(JObject.Parse(json).TryGetValue("currentQuestId", out JToken value));
            Assert.AreEqual(JTokenType.Null, value.Type);
            Assert.IsFalse(json.Contains("clearCurrentQuest"));
        }

        private static UserBadgesResponse BadgesResponse(params UserBadge[] badges)
        {
            return new UserBadgesResponse { badges = badges, earnedCount = badges.Length, totalCount = 10 };
        }

        private HomeScreenViewModel CreateVmWithBadges(Mock<IGamificationService> gamification, Mock<IBadgeService> badges)
        {
            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123" });
            PlayerPrefs.SetString("last_seen_gamif_ts_test-user", "2026-09-23T10:00:00Z");
            return new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                gamificationService: gamification.Object,
                badgeService: badges.Object
            );
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenNewBadge_ReturnsBadgeCelebrationWithWalletReward()
        {
            var gamification = new Mock<IGamificationService>();
            gamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new GamificationProfileResponse
                {
                    userId = "test-user",
                    badges = new[] { "FIRST_STEP", "CHEF" },
                    recentEvents = new UserEvent[0],
                    recentWalletEntries = new[]
                    {
                        new WalletEntry { id = "w-1", currency = "XP", amount = 30, reason = "Badge CHEF completed" },
                        new WalletEntry { id = "w-2", currency = "POINTS", amount = 5, reason = "Badge CHEF completed" },
                        new WalletEntry { id = "w-3", currency = "XP", amount = 99, reason = "Mission M.B1.1 completed" }
                    }
                }, (ApiErrorResponse)null));

            var badges = new Mock<IBadgeService>();
            badges.Setup(b => b.GetMyBadgesAsync())
                .ReturnsAsync((BadgesResponse(new UserBadge { code = "CHEF", name = "Chef", earned = true }), (ApiErrorResponse)null));

            PlayerPrefs.SetString("celebrated_badges_test-user", "FIRST_STEP");
            var vm = CreateVmWithBadges(gamification, badges);

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].IsBadge);
            Assert.AreEqual("@UI:BADGE_EARNED_TITLE", result[0].ContextTitle);
            Assert.AreEqual("CHEF", result[0].Code);
            Assert.AreEqual("CHEF", result[0].Reward.badgeId);
            Assert.AreEqual("Chef", result[0].Reward.badgeName);
            Assert.AreEqual(30, result[0].Reward.xp);
            Assert.AreEqual(5, result[0].Reward.points);
            CollectionAssert.AreEqual(new[] { "FIRST_STEP", "CHEF" }, _storeService.GetAppState().userBadges);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenNewBadgeWithoutWallet_HasNoXpFallback()
        {
            var gamification = new Mock<IGamificationService>();
            gamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new GamificationProfileResponse
                {
                    userId = "test-user",
                    badges = new[] { "STYLISH" },
                    recentEvents = new UserEvent[0],
                    recentWalletEntries = new WalletEntry[0]
                }, (ApiErrorResponse)null));

            var badges = new Mock<IBadgeService>();
            badges.Setup(b => b.GetMyBadgesAsync())
                .ReturnsAsync((BadgesResponse(new UserBadge { code = "STYLISH", name = "Stylish", earned = true }), (ApiErrorResponse)null));

            PlayerPrefs.SetString("celebrated_badges_test-user", "");
            var vm = CreateVmWithBadges(gamification, badges);

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.AreEqual(1, result.Count);
            Assert.IsNull(result[0].Reward.xp);
            Assert.IsNull(result[0].Reward.points);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenMissionAndBadge_OrdersBadgeLast()
        {
            var gamification = new Mock<IGamificationService>();
            gamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new GamificationProfileResponse
                {
                    userId = "test-user",
                    badges = new[] { "MISSIONARY" },
                    recentEvents = new[]
                    {
                        new UserEvent
                        {
                            id = "ev-mission-9",
                            eventType = "MISSION_COMPLETED",
                            timestamp = "2026-09-23T10:05:00Z",
                            metadata = JObject.FromObject(new { missionCode = "M.B1.9" })
                        }
                    },
                    recentWalletEntries = new[]
                    {
                        new WalletEntry { id = "w-1", currency = "XP", amount = 50, reason = "Mission M.B1.9 completed" }
                    }
                }, (ApiErrorResponse)null));

            var badges = new Mock<IBadgeService>();
            badges.Setup(b => b.GetMyBadgesAsync())
                .ReturnsAsync((BadgesResponse(new UserBadge { code = "MISSIONARY", name = "Missionary", earned = true }), (ApiErrorResponse)null));

            PlayerPrefs.SetString("celebrated_badges_test-user", "");
            var vm = CreateVmWithBadges(gamification, badges);

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("@UI:MISSION_REWARD_TITLE", result[0].ContextTitle);
            Assert.IsTrue(result[1].IsBadge);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenBadgeAlreadyCelebrated_ReturnsNull()
        {
            var gamification = new Mock<IGamificationService>();
            gamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new GamificationProfileResponse
                {
                    userId = "test-user",
                    badges = new[] { "CHEF" },
                    recentEvents = new UserEvent[0],
                    recentWalletEntries = new WalletEntry[0]
                }, (ApiErrorResponse)null));

            var badges = new Mock<IBadgeService>();
            PlayerPrefs.SetString("celebrated_badges_test-user", "CHEF");
            var vm = CreateVmWithBadges(gamification, badges);

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNull(result);
            badges.Verify(b => b.GetMyBadgesAsync(), Times.Never);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_WhenBadgeDetailsFail_DoesNotMarkCelebrated()
        {
            var gamification = new Mock<IGamificationService>();
            gamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new GamificationProfileResponse
                {
                    userId = "test-user",
                    badges = new[] { "CHEF" },
                    recentEvents = new UserEvent[0],
                    recentWalletEntries = new WalletEntry[0]
                }, (ApiErrorResponse)null));

            var badges = new Mock<IBadgeService>();
            badges.Setup(b => b.GetMyBadgesAsync())
                .ReturnsAsync(((UserBadgesResponse)null, new ApiErrorResponse { message = "boom" }));

            PlayerPrefs.SetString("celebrated_badges_test-user", "");
            var vm = CreateVmWithBadges(gamification, badges);

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNull(result);
            Assert.AreEqual("", PlayerPrefs.GetString("celebrated_badges_test-user", "missing"));
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_FirstRun_CelebratesOnlyRecentBadge()
        {
            var gamification = new Mock<IGamificationService>();
            gamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new GamificationProfileResponse
                {
                    userId = "test-user",
                    badges = new[] { "CHEF", "FIRST_STEP" },
                    recentEvents = new UserEvent[0],
                    recentWalletEntries = new WalletEntry[0]
                }, (ApiErrorResponse)null));

            var badges = new Mock<IBadgeService>();
            badges.Setup(b => b.GetMyBadgesAsync())
                .ReturnsAsync((BadgesResponse(
                    new UserBadge { code = "CHEF", name = "Chef", earned = true, earnedAt = System.DateTime.UtcNow.AddDays(-5) },
                    new UserBadge { code = "FIRST_STEP", name = "First Step", earned = true, earnedAt = System.DateTime.UtcNow.AddMinutes(-1) }
                ), (ApiErrorResponse)null));

            var vm = CreateVmWithBadges(gamification, badges);

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("FIRST_STEP", result[0].Code);
            badges.Verify(b => b.GetMyBadgesAsync(), Times.Once);
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_FirstGamificationRun_StillReturnsRecentBadge()
        {
            // No last_seen_gamif_ts yet: the mission/quest cursor is established and returns early,
            // but badge detection is independent of that cursor.
            var gamification = new Mock<IGamificationService>();
            gamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((new GamificationProfileResponse
                {
                    userId = "test-user",
                    badges = new[] { "FIRST_STEP" },
                    recentEvents = new UserEvent[0],
                    recentWalletEntries = new WalletEntry[0]
                }, (ApiErrorResponse)null));

            var badges = new Mock<IBadgeService>();
            badges.Setup(b => b.GetMyBadgesAsync())
                .ReturnsAsync((BadgesResponse(
                    new UserBadge { code = "FIRST_STEP", name = "First Step", earned = true, earnedAt = System.DateTime.UtcNow.AddMinutes(-1) }
                ), (ApiErrorResponse)null));

            _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123" });
            PlayerPrefs.DeleteKey("last_seen_gamif_ts_test-user");
            var vm = new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                gamificationService: gamification.Object,
                badgeService: badges.Object
            );

            var result = await vm.CheckPendingGamificationRewardsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual("FIRST_STEP", result[0].Code);
        }
        [Test]
        public void ProgressWheels_StoreChangeRaisesEvent_AndRefreshCallsService()
        {
            var wheels = new Mock<IProgressWheelService>();
            wheels.Setup(w => w.RefreshAsync()).Returns(System.Threading.Tasks.Task.CompletedTask);
            var vm = new HomeScreenViewModel(_storeService, _mockAudioService.Object, _mockNotificationService.Object, _mockLegalService.Object,
                progressWheelService: wheels.Object);
            int raised = 0;
            vm.ProgressWheelsChanged += () => raised++;

            _storeService.store.Dispatch(AppActions.setProgressWheels.Invoke(new[] { new ProgressWheel { kind = "CO2_REDUCTION" } }));
            _storeService.store.Dispatch(AppActions.setWalletBalance.Invoke(new AppActions.WalletPayload(5, 5)));
            vm.RefreshProgressWheels();

            Assert.AreEqual(1, raised);
            Assert.AreEqual(1, vm.GetProgressWheelSection().Visible.Count);
            wheels.Verify(w => w.RefreshAsync(), Times.Once);
            vm.Dispose();
        }

        [Test]
        public void ProgressWheels_LoadingChangeRaisesEvent_AndDisposeUnhooks()
        {
            var wheels = new Mock<IProgressWheelService>();
            var vm = new HomeScreenViewModel(_storeService, _mockAudioService.Object, _mockNotificationService.Object, _mockLegalService.Object,
                progressWheelService: wheels.Object);
            int raised = 0;
            vm.ProgressWheelsChanged += () => raised++;

            wheels.Raise(w => w.LoadingChanged += null);
            vm.Dispose();
            wheels.Raise(w => w.LoadingChanged += null);

            Assert.AreEqual(1, raised);
        }

        private HomeScreenViewModel CreateWithFailures(IMissionFailureService failures)
        {
            return new HomeScreenViewModel(
                _storeService,
                _mockAudioService.Object,
                _mockNotificationService.Object,
                _mockLegalService.Object,
                missionFailureService: failures);
        }

        [Test]
        public async Task CheckFailedMissions_ReturnsServiceFailures()
        {
            var failures = new Mock<IMissionFailureService>();
            var failed = new MissionProgress { missionCode = "M.A1.1", status = ProgressStatus.Failed };
            failures.Setup(f => f.GetUnacknowledgedFailuresAsync()).ReturnsAsync(new[] { failed });
            var vm = CreateWithFailures(failures.Object);

            var result = await vm.CheckFailedMissionsAsync();

            CollectionAssert.AreEqual(new[] { failed }, result);
            vm.Dispose();
        }

        [Test]
        public async Task RestartFailedMission_RestartsThroughService_AndReturnsError()
        {
            var failures = new Mock<IMissionFailureService>();
            var failed = new MissionProgress { missionCode = "M.A1.1", status = ProgressStatus.Failed };
            var error = new ApiErrorResponse { message = "x" };
            failures.Setup(f => f.RestartAsync(failed)).ReturnsAsync(((MissionProgress)null, error));
            var vm = CreateWithFailures(failures.Object);

            Assert.AreSame(error, await vm.RestartFailedMissionAsync(failed));
            vm.Dispose();
        }

        [Test]
        public void AcknowledgeFailedMission_AcknowledgesThroughService()
        {
            var failures = new Mock<IMissionFailureService>();
            var failed = new MissionProgress { missionCode = "M.A1.1", status = ProgressStatus.Failed };
            var vm = CreateWithFailures(failures.Object);

            vm.AcknowledgeFailedMission(failed);

            failures.Verify(f => f.Acknowledge(failed), Times.Once);
            vm.Dispose();
        }

        [Test]
        public async Task CheckPendingGamificationRewardsAsync_NextQuestAboveLevel_IsNotOffered()
        {
            bool originalDevUnlocks = DevUnlocks.All;
            DevUnlocks.All = false;
            try
            {
                var mockGamification = new Mock<IGamificationService>();
                var mockQuest = new Mock<IQuestService>();
                var mockDimensions = new Mock<IDimensionService>();
                mockDimensions.Setup(d => d.GetDimension("HEALTH")).Returns(new Dimension { id = "HEALTH", code = DimensionCode.NutritionValues, name = "Nutrition" });

                // Existing user: completed an ADVANCED quest although their level is BEGINNER
                var q1 = new Quest { id = "q1", code = "QUEST.HEALTH.ADVANCED.1", title = "A1", dimensionId = "HEALTH", level = "ADVANCED" };
                var q2 = new Quest { id = "q2", code = "QUEST.HEALTH.ADVANCED.2", title = "A2", dimensionId = "HEALTH", level = "ADVANCED" };
                mockQuest.Setup(q => q.GetQuestsAsync(null, null, null)).ReturnsAsync((new[] { q1, q2 }, (ApiErrorResponse)null));

                var profile = new GamificationProfileResponse
                {
                    userId = "test-user",
                    recentEvents = new[]
                    {
                        new UserEvent
                        {
                            id = "ev-quest-adv",
                            eventType = "QUEST_COMPLETED",
                            timestamp = "2026-09-23T10:05:00Z",
                            metadata = JObject.FromObject(new { questCode = "QUEST.HEALTH.ADVANCED.1" })
                        }
                    }
                };
                mockGamification.Setup(g => g.GetGamificationProfileAsync(It.IsAny<int>(), It.IsAny<int>()))
                    .ReturnsAsync((profile, (ApiErrorResponse)null));

                _storeService.SetAppState(new AppState { userId = "test-user", accessToken = "token-123", userSegment = "BEGINNER" });
                PlayerPrefs.SetString("last_seen_gamif_ts_test-user", "2026-09-23T10:00:00Z");

                var vm = new HomeScreenViewModel(
                    _storeService,
                    _mockAudioService.Object,
                    questService: mockQuest.Object,
                    gamificationService: mockGamification.Object,
                    questProgressionService: new QuestProgressionService { UnlockAllQuests = false },
                    dimensionService: mockDimensions.Object
                );

                var result = await vm.CheckPendingGamificationRewardsAsync();

                Assert.AreEqual(1, result.Count);
                Assert.IsNull(result[0].UnlockedQuest, "A next quest above the user's level must not be offered");
            }
            finally
            {
                DevUnlocks.All = originalDevUnlocks;
            }
        }
    }
}
