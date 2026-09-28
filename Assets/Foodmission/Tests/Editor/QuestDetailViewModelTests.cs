using System;
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
    public class QuestDetailViewModelTests
    {
        private Mock<IQuestService> _mockQuestService;
        private Mock<IDimensionService> _mockDimensionService;
        private Mock<IQuizService> _mockQuizService;
        private Mock<IMissionService> _mockMissionService;
        private Mock<IChallengeService> _mockChallengeService;
        private Mock<IFoodFactService> _mockFoodFactService;
        private TestStoreService _storeService;
        private QuestDetailViewModel _vm;
        private Func<bool> _originalOverride;

        private Dimension _mockDimension;
        private Quest _mockQuest;
        private QuestProgress _mockProgress;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockQuestService = new Mock<IQuestService>();
            _mockDimensionService = new Mock<IDimensionService>();
            _mockQuizService = new Mock<IQuizService>();
            _mockMissionService = new Mock<IMissionService>();
            _mockChallengeService = new Mock<IChallengeService>();
            _mockFoodFactService = new Mock<IFoodFactService>();

            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-token",
                tokenType = "Bearer",
                lang = "es"
            });

            _mockDimension = new Dimension
            {
                id = "dim-1",
                code = "DIET_CHANGES",
                name = "Cambios en la dieta"
            };

            _mockQuest = new Quest
            {
                id = "q-100",
                code = "QUEST.DIET.1",
                dimensionId = "dim-1",
                level = QuestLevel.Beginner,
                title = "Aventura de la Dieta Saludable",
                description = "Completa los pasos para mejorar tu dieta diaria.",
                available = true,
                items = new[]
                {
                    new QuestItem
                    {
                        id = "it-1",
                        contentType = QuestContentType.Quiz,
                        contentCode = "QUIZ_DIET_1",
                        label = "Quiz sobre verduras",
                        sortOrder = 1
                    },
                    new QuestItem
                    {
                        id = "it-2",
                        contentType = QuestContentType.FoodFact,
                        contentCode = "FACT_VEG_1",
                        label = "Dato sobre la fibra",
                        sortOrder = 2
                    },
                    new QuestItem
                    {
                        id = "it-3",
                        contentType = QuestContentType.Mission,
                        contentCode = "MISSION_MEAL_1",
                        label = "Registra un almuerzo saludable",
                        sortOrder = 3
                    },
                    new QuestItem
                    {
                        id = "it-4",
                        contentType = "CHALLENGE",
                        contentCode = "CHALLENGE_DAY_1",
                        label = "Reto del día sin carne",
                        sortOrder = 4
                    }
                }
            };

            _mockProgress = new QuestProgress
            {
                questId = "q-100",
                userId = "user-1",
                completed = false,
                progress = 25f
            };

            _mockDimensionService.Setup(d => d.IsLoaded).Returns(true);
            _mockDimensionService.Setup(d => d.GetDimension("dim-1")).Returns(_mockDimension);
            _mockFoodFactService.Setup(s => s.GetUserProgressListAsync())
                .ReturnsAsync((Array.Empty<FoodFactProgressResponse>(), null));

            _vm = new QuestDetailViewModel(
                _storeService,
                _mockQuestService.Object,
                _mockDimensionService.Object,
                _mockQuizService.Object,
                _mockMissionService.Object,
                _mockChallengeService.Object,
                foodFactService: _mockFoodFactService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
            _vm?.Dispose();
        }

        [Test]
        public void Constructor_InitializesDefaultState()
        {
            Assert.IsFalse(_vm.IsLoading);
            Assert.AreEqual(string.Empty, _vm.QuestCode);
            Assert.AreEqual(string.Empty, _vm.QuestTitle);
            Assert.AreEqual(string.Empty, _vm.QuestDescription);
            Assert.AreEqual(QuestLevel.Beginner, _vm.QuestLevel);
            Assert.AreEqual(0f, _vm.ProgressPercent);
            Assert.IsFalse(_vm.IsCompleted);
            Assert.AreEqual(0, _vm.TotalActivitiesCount);
            Assert.AreEqual(0, _vm.CompletedActivitiesCount);
            Assert.IsEmpty(_vm.Activities);
        }

        [Test]
        public async Task LoadQuestAsync_PopulatesQuestAndActivities_CalculatesProgress()
        {
            _mockQuestService.Setup(s => s.GetQuestAsync("QUEST.DIET.1", It.IsAny<string>()))
                .ReturnsAsync((_mockQuest, null));
            _mockQuestService.Setup(s => s.GetQuestProgressAsync("QUEST.DIET.1", It.IsAny<string>()))
                .ReturnsAsync((_mockProgress, null));

            var completedQuizProgress = new[]
            {
                new QuizProgress
                {
                    quizCode = "QUIZ_DIET_1",
                    completed = true,
                    isCorrect = true
                }
            };

            _mockQuizService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((completedQuizProgress, null));
            _mockMissionService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((Array.Empty<MissionProgress>(), null));
            _mockChallengeService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((Array.Empty<ChallengeProgress>(), null));

            await _vm.LoadQuestAsync("QUEST.DIET.1");

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorMessage);
            Assert.AreEqual("QUEST.DIET.1", _vm.QuestCode);
            Assert.AreEqual("Aventura de la Dieta Saludable", _vm.QuestTitle);
            Assert.AreEqual("DIET_CHANGES", _vm.DimensionCode);
            Assert.AreEqual("Cambios en la dieta", _vm.DimensionName);
            Assert.AreEqual(4, _vm.TotalActivitiesCount);
            Assert.AreEqual(1, _vm.CompletedActivitiesCount);
            Assert.AreEqual(25f, _vm.ProgressPercent);
            Assert.IsFalse(_vm.IsCompleted);

            Assert.AreEqual(4, _vm.Activities.Count);
            Assert.IsTrue(_vm.Activities[0].IsCompleted);
            // Assert.AreEqual("Quiz", _vm.Activities[0].TypeLabel);
            Assert.IsFalse(_vm.Activities[1].IsCompleted);
            // Assert.AreEqual("Dato curioso", _vm.Activities[1].TypeLabel);
            Assert.IsFalse(_vm.Activities[2].IsCompleted);
            // Assert.AreEqual("Misión", _vm.Activities[2].TypeLabel);
            Assert.IsFalse(_vm.Activities[3].IsCompleted);
            // Assert.AreEqual("Desafío", _vm.Activities[3].TypeLabel);
        }

        [Test]
        public async Task LoadQuestAsync_WhenQuestCompleted_MarksAllActivitiesCompleted()
        {
            var completedProgress = new QuestProgress
            {
                questId = "q-100",
                questCode = "QUEST.DIET.1",
                completed = true,
                progress = 100f
            };

            _mockQuestService.Setup(s => s.GetQuestAsync("QUEST.DIET.1", It.IsAny<string>()))
                .ReturnsAsync((_mockQuest, null));
            _mockQuestService.Setup(s => s.GetQuestProgressAsync("QUEST.DIET.1", It.IsAny<string>()))
                .ReturnsAsync((completedProgress, null));
            _mockQuizService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((Array.Empty<QuizProgress>(), null));
            _mockMissionService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((Array.Empty<MissionProgress>(), null));
            _mockChallengeService.Setup(s => s.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((Array.Empty<ChallengeProgress>(), null));

            await _vm.LoadQuestAsync("QUEST.DIET.1");

            Assert.IsTrue(_vm.IsCompleted);
            Assert.AreEqual(100f, _vm.ProgressPercent);
            Assert.AreEqual(4, _vm.CompletedActivitiesCount);
            Assert.IsTrue(_vm.Activities.All(a => a.IsCompleted));
        }

        [Test]
        public async Task LoadQuestAsync_WhenServiceReturnsError_SetsErrorDetail()
        {
            _mockQuestService.Setup(s => s.GetQuestAsync("QUEST.ERROR", It.IsAny<string>()))
                .ReturnsAsync((null, new ApiErrorResponse { message = "Quest not found", statusCode = 404 }));

            await _vm.LoadQuestAsync("QUEST.ERROR");

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual("Quest not found", _vm.ErrorMessage);
            Assert.AreEqual(404, _vm.ErrorDetail.statusCode);
        }

        [Test]
        public void OpenActivity_WhenQuiz_RequestsOpenQuizNavigation()
        {
            string requestedRoute = null;
            Argument[] capturedArgs = null;

            _vm.NavigationRequested += (route, args) =>
            {
                requestedRoute = route;
                capturedArgs = args;
            };

            var quizActivity = new QuestActivityDisplayItem
            {
                Item = new QuestItem { contentType = QuestContentType.Quiz, contentCode = "QUIZ_1" }
            };

            _vm.OpenActivity(quizActivity);

            Assert.AreEqual(Actions.open_quiz, requestedRoute);
            Assert.IsNotNull(capturedArgs);
            Assert.AreEqual("code", capturedArgs[0].name);
            Assert.AreEqual("QUIZ_1", capturedArgs[0].value);
        }

        [Test]
        public void OpenActivity_WhenFoodFact_RequestsOpenFoodFactNavigation()
        {
            string requestedRoute = null;
            Argument[] capturedArgs = null;

            _vm.NavigationRequested += (route, args) =>
            {
                requestedRoute = route;
                capturedArgs = args;
            };

            var factActivity = new QuestActivityDisplayItem
            {
                Item = new QuestItem { contentType = QuestContentType.FoodFact, contentCode = "FACT_1" }
            };

            _vm.OpenActivity(factActivity);

            Assert.AreEqual(Actions.open_food_fact, requestedRoute);
            Assert.IsNotNull(capturedArgs);
            Assert.AreEqual("code", capturedArgs[0].name);
            Assert.AreEqual("FACT_1", capturedArgs[0].value);
        }

        [Test]
        public void OpenActivity_WhenMission_RequestsOpenMissionNavigation()
        {
            string requestedRoute = null;
            Argument[] capturedArgs = null;

            _vm.NavigationRequested += (route, args) =>
            {
                requestedRoute = route;
                capturedArgs = args;
            };

            var missionActivity = new QuestActivityDisplayItem
            {
                Item = new QuestItem { contentType = QuestContentType.Mission, contentCode = "MISSION_1" }
            };

            _vm.OpenActivity(missionActivity);

            Assert.AreEqual(Actions.open_mission, requestedRoute);
            Assert.IsNotNull(capturedArgs);
            Assert.AreEqual("code", capturedArgs[0].name);
            Assert.AreEqual("MISSION_1", capturedArgs[0].value);
        }

        [Test]
        public void OpenActivity_WhenChallenge_RequestsOpenChallengeNavigation()
        {
            string requestedRoute = null;
            Argument[] capturedArgs = null;

            _vm.NavigationRequested += (route, args) =>
            {
                requestedRoute = route;
                capturedArgs = args;
            };

            var challengeActivity = new QuestActivityDisplayItem
            {
                Item = new QuestItem { contentType = "CHALLENGE", contentCode = "CHALLENGE_1" }
            };

            _vm.OpenActivity(challengeActivity);

            Assert.AreEqual(Actions.open_challenge, requestedRoute);
            Assert.IsNotNull(capturedArgs);
            Assert.AreEqual("code", capturedArgs[0].name);
            Assert.AreEqual("CHALLENGE_1", capturedArgs[0].value);
        }

        [Test]
        public void TimelineDisplayLabel_FormatsCorrectlyAccordingToMockup()
        {
            // 1. Item with label already containing type prefix
            var item1 = new QuestActivityDisplayItem
            {
                // TypeLabel = "Challenge",
                Title = "Whole Grain Check",
                Item = new QuestItem { contentType = "CHALLENGE", label = "Challenge: Whole Grain Check", contentCode = "CHALLENGE_1" }
            };
            Assert.AreEqual("Challenge: Whole Grain Check", item1.TimelineDisplayLabel);

            // 2. Item with label without type prefix
            var item2 = new QuestActivityDisplayItem
            {
                // TypeLabel = "Mission",
                Title = "Protein Every Day",
                Item = new QuestItem { contentType = QuestContentType.Mission, label = "Protein Every Day", contentCode = "MISSION_1" }
            };
            Assert.AreEqual("Mission: Protein Every Day", item2.TimelineDisplayLabel);

            // 3. Quiz with no custom label (or label equal to contentCode)
            var item3 = new QuestActivityDisplayItem
            {
                // TypeLabel = "Quiz",
                Title = "QUIZ_1",
                Item = new QuestItem { contentType = QuestContentType.Quiz, label = null, contentCode = "QUIZ_1" }
            };
            Assert.AreEqual("Quiz", item3.TimelineDisplayLabel);

            // 4. Food Fact without custom label
            var item4 = new QuestActivityDisplayItem
            {
                // TypeLabel = "Food Facts",
                Title = "FACT_1",
                Item = new QuestItem { contentType = QuestContentType.FoodFact, label = null, contentCode = "FACT_1" }
            };
            Assert.AreEqual("Food Facts", item4.TimelineDisplayLabel);
        }

        [Test]
        public void IsCurrentQuest_MatchesUserCurrentQuestId()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "q-100" });

            _vm.SetQuestForTesting(_mockQuest, _mockProgress);

            Assert.IsTrue(_vm.IsCurrentQuest);

            _storeService.SetAppState(new AppState { userCurrentQuestId = "different-quest" });
            _vm.SetQuestForTesting(_mockQuest, _mockProgress);
            Assert.IsFalse(_vm.IsCurrentQuest);
        }

        [Test]
        public void HasOtherActiveQuest_WhenDifferentQuestActive_ReturnsTrue()
        {
            _vm.SetQuestForTesting(_mockQuest, _mockProgress);

            // 1. When no quest is active
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });
            Assert.IsFalse(_vm.HasOtherActiveQuest);

            // 2. When same quest is active (by ID)
            _storeService.SetAppState(new AppState { userCurrentQuestId = "q-100" });
            Assert.IsFalse(_vm.HasOtherActiveQuest);

            // 3. When same quest is active (by code)
            _storeService.SetAppState(new AppState { userCurrentQuestId = "QUEST.DIET.1" });
            Assert.IsFalse(_vm.HasOtherActiveQuest);

            // 4. When a different quest is active
            _storeService.SetAppState(new AppState { userCurrentQuestId = "other-quest-id" });
            Assert.IsTrue(_vm.HasOtherActiveQuest);
        }

        [Test]
        public async Task StartQuestAsync_UpdatesProfileAndDispatchesAction()
        {
            var mockAuthService = new Mock<IAuthService>();
            mockAuthService.Setup(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r => r.currentQuestId == "q-100")))
                .ReturnsAsync((true, (ApiErrorResponse)null));

            var vm = new QuestDetailViewModel(
                _storeService,
                _mockQuestService.Object,
                _mockDimensionService.Object,
                _mockQuizService.Object,
                _mockMissionService.Object,
                _mockChallengeService.Object,
                authService: mockAuthService.Object
            );

            vm.SetQuestForTesting(_mockQuest, _mockProgress);
            Assert.IsFalse(vm.IsCurrentQuest);

            bool success = await vm.StartQuestAsync();

            Assert.IsTrue(success);
            Assert.IsTrue(vm.IsCurrentQuest);
            mockAuthService.Verify(a => a.UpdateProfileAsync(It.Is<ProfileUpdateRequest>(r => r.currentQuestId == "q-100")), Times.Once);
            Assert.Contains("app/setCurrentQuest", _storeService.DispatchedActionTypes);
            Assert.AreEqual("q-100", _storeService.GetAppState().userCurrentQuestId);
        }

        [Test]
        public void PopulateFromQuest_WhenProgressHasReward_DispatchesWalletRewardAndSetsEarnedReward()
        {
            var expectedReward = new ContentReward { xp = 150, points = 50, badgeId = "MASTER_CHEF" };
            var progress = new QuestProgress
            {
                questId = "q-100",
                completed = true,
                progress = 100f,
                reward = expectedReward
            };

            _vm.SetQuestForTesting(_mockQuest, progress);

            Assert.IsNotNull(_vm.EarnedReward);
            Assert.AreSame(expectedReward, _vm.EarnedReward);
            Assert.Contains("app/addWalletReward", _storeService.DispatchedActionTypes);
            Assert.AreEqual(150, _storeService.GetAppState().userXp);
            Assert.AreEqual(50, _storeService.GetAppState().userPoints);
        }

        [Test]
        public void PopulateFromQuest_WhenProgressHasNoReward_DoesNotDispatchWalletReward()
        {
            var progress = new QuestProgress
            {
                questId = "q-100",
                completed = true,
                progress = 100f,
                reward = null
            };

            _vm.SetQuestForTesting(_mockQuest, progress);

            Assert.IsNull(_vm.EarnedReward);
            Assert.IsFalse(_storeService.DispatchedActionTypes.Contains("app/addWalletReward"));
        }

        [Test]
        public void PopulateFromQuest_WithCompletedFoodFact_MarksItemCompleted()
        {
            var foodFactProgress = new[]
            {
                new FoodFactProgressResponse
                {
                    foodFactCode = "FACT_VEG_1",
                    readAt = "2026-09-17T10:00:00Z"
                }
            };

            _vm.SetQuestForTesting(_mockQuest, _mockProgress, foodFactProgress: foodFactProgress);

            var factActivity = _vm.Activities.FirstOrDefault(a => a.Item.contentCode == "FACT_VEG_1");
            Assert.IsNotNull(factActivity);
            Assert.IsTrue(factActivity.IsCompleted);
            Assert.AreEqual(100f, factActivity.Progress);
        }
    }
}
