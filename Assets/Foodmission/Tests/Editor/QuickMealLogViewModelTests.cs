using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuickMealLogViewModelTests
    {
        private Mock<IQuestService> _mockQuestService;
        private Mock<IEventService> _mockEventService;
        private Mock<IActivityEventMapper> _mockActivityEventMapper;
        private Mock<ICatalogService> _mockCatalogService;
        private Mock<IMealLogService> _mockMealLogService;
        private Mock<IMealService> _mockMealService;
        private TestStoreService _storeService;
        private QuickMealLogViewModel _vm;
        private Func<bool> _originalOverride;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockQuestService = new Mock<IQuestService>();
            _mockEventService = new Mock<IEventService>();
            _mockActivityEventMapper = new Mock<IActivityEventMapper>();
            _mockCatalogService = new Mock<ICatalogService>();
            _mockMealLogService = new Mock<IMealLogService>();
            _mockMealService = new Mock<IMealService>();

            _mockEventService.Setup(e => e.CurrentSessionId).Returns("test-session-123");

            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-token",
                tokenType = "Bearer",
                lang = "es"
            });

            _mockCatalogService.Setup(c => c.GetTypeOfMealsAsync(It.IsAny<string>()))
                .ReturnsAsync((new CatalogItem[]
                {
                    new CatalogItem { code = "BREAKFAST", label = "Desayuno" },
                    new CatalogItem { code = "LUNCH", label = "Almuerzo" },
                    new CatalogItem { code = "DINNER", label = "Cena" },
                    new CatalogItem { code = "SNACK", label = "Tentempié" }
                }, null));

            _vm = new QuickMealLogViewModel(
                _storeService,
                _mockQuestService.Object,
                _mockEventService.Object,
                _mockActivityEventMapper.Object,
                _mockCatalogService.Object,
                _mockMealLogService.Object,
                _mockMealService.Object
            );
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
            _vm?.Dispose();
        }

        [Test]
        public void Constructor_InitializesDefaultValues()
        {
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsFalse(_vm.IsSubmitting);
            Assert.IsFalse(_vm.SubmitSuccess);
            Assert.IsNotEmpty(_vm.SelectedMealType);
            Assert.IsEmpty(_vm.MealName);
        }

        [Test]
        public async Task LoadActiveQuestQuestionsAsync_WhenNoActiveQuest_LoadsDefaultQuestions()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });

            await _vm.LoadActiveQuestQuestionsAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNotNull(_vm.Questions);
            Assert.AreEqual(4, _vm.Questions.Count);
            Assert.IsTrue(_vm.Questions.All(q => !q.IsChecked));
        }

        [Test]
        public async Task LoadActiveQuestQuestionsAsync_WhenActiveQuestExists_BuildsDynamicQuestions()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "q-active-1" });

            var mockQuest = new Quest
            {
                id = "q-active-1",
                code = "QUEST.LEGUMES",
                title = "Aventura de las Legumbres",
                items = new[]
                {
                    new QuestItem
                    {
                        id = "it-m1",
                        contentType = QuestContentType.Mission,
                        contentCode = "MISSION.LEGUME.1",
                        label = "Come legumbres 3 veces"
                    },
                    new QuestItem
                    {
                        id = "it-c1",
                        contentType = "CHALLENGE",
                        contentCode = "CHALLENGE.PLANT.1",
                        label = "Día sin carne"
                    }
                }
            };

            _mockQuestService.Setup(s => s.GetQuestAsync("q-active-1", It.IsAny<string>()))
                .ReturnsAsync((mockQuest, null));

            _mockActivityEventMapper.Setup(m => m.GetMissionMapping("MISSION.LEGUME.1"))
                .Returns(new ActivityMapping
                {
                    ActivityCode = "MISSION.LEGUME.1",
                    DirectQuestionPrompt = "¿Esta comida contenía legumbres?",
                    TargetEventTypes = new[] { "MEAL_LEGUMES_CONSUMED" }
                });

            _mockActivityEventMapper.Setup(m => m.GetChallengeMapping("CHALLENGE.PLANT.1"))
                .Returns(new ActivityMapping
                {
                    ActivityCode = "CHALLENGE.PLANT.1",
                    DirectQuestionPrompt = "¿Fue una comida 100% vegetariana?",
                    TargetEventTypes = new[] { "MEAL_VEGETARIAN" }
                });

            await _vm.LoadActiveQuestQuestionsAsync();

            Assert.AreEqual("Aventura de las Legumbres", _vm.ActiveQuestTitle);
            Assert.AreEqual("QUEST.LEGUMES", _vm.ActiveQuestCode);
            Assert.AreEqual(2, _vm.Questions.Count);
            Assert.AreEqual("¿Esta comida contenía legumbres?", _vm.Questions[0].Prompt);
            Assert.AreEqual("MEAL_LEGUMES_CONSUMED", _vm.Questions[0].EventType);
            Assert.AreEqual("¿Fue una comida 100% vegetariana?", _vm.Questions[1].Prompt);
            Assert.AreEqual("MEAL_VEGETARIAN", _vm.Questions[1].EventType);
        }

        [Test]
        public async Task LoadActiveQuestQuestionsAsync_WhenMappingHasNoPrompt_FetchesFromMissionService()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "q-active-2" });

            var mockQuest = new Quest
            {
                id = "q-active-2",
                code = "QUEST.CUSTOM",
                title = "Quest Personalizada",
                items = new[]
                {
                    new QuestItem
                    {
                        id = "it-m99",
                        contentType = QuestContentType.Mission,
                        contentCode = "MISSION.CUSTOM.99"
                    }
                }
            };

            var mockMissionService = new Mock<IMissionService>();
            mockMissionService.Setup(s => s.GetMissionAsync("MISSION.CUSTOM.99", It.IsAny<string>()))
                .ReturnsAsync((new Mission { code = "MISSION.CUSTOM.99", title = "Beber 2L de agua" }, null));

            var vm = new QuickMealLogViewModel(
                _storeService,
                _mockQuestService.Object,
                _mockEventService.Object,
                _mockActivityEventMapper.Object,
                _mockCatalogService.Object,
                _mockMealLogService.Object,
                _mockMealService.Object,
                mockMissionService.Object
            );

            _mockQuestService.Setup(s => s.GetQuestAsync("q-active-2", It.IsAny<string>()))
                .ReturnsAsync((mockQuest, null));

            _mockActivityEventMapper.Setup(m => m.GetMissionMapping("MISSION.CUSTOM.99"))
                .Returns((ActivityMapping)null);

            await vm.LoadActiveQuestQuestionsAsync();

            Assert.AreEqual(1, vm.Questions.Count);
        }

        [Test]
        public void ToggleQuestion_TogglesCheckedState()
        {
            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem>
            {
                new QuickMealCheckItem { Id = "q1", Prompt = "Q1", IsChecked = false }
            };

            _vm.ToggleQuestion("q1");
            Assert.IsTrue(_vm.Questions[0].IsChecked);

            _vm.ToggleQuestion("q1");
            Assert.IsFalse(_vm.Questions[0].IsChecked);
        }

        [Test]
        public async Task SubmitQuickMealLogAsync_EmitsEventsAndSucceeds()
        {
            _vm.SelectedMealType = "LUNCH";
            _vm.MealName = "Lentejas caseras";
            _vm.ActiveQuestCode = "QUEST.LEGUMES";
            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem>
            {
                new QuickMealCheckItem { Id = "q1", Prompt = "Q1", EventType = "MEAL_LEGUMES", IsChecked = true, ActivityCode = "M1" },
                new QuickMealCheckItem { Id = "q2", Prompt = "Q2", EventType = "MEAL_VEG", IsChecked = false, ActivityCode = "M2" }
            };

            _mockMealService.Setup(s => s.CreateMealAsync(It.IsAny<CreateMealRequest>()))
                .ReturnsAsync((new Meal { id = "created-meal-1", name = "Lentejas caseras" }, null));
            _mockMealLogService.Setup(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog { id = "created-log-1" }, null));

            _mockEventService.Setup(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()))
                .ReturnsAsync((new UserEvent { id = "ev-1", eventType = "MEAL_LEGUMES" }, null));

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsTrue(success);
            Assert.IsTrue(_vm.SubmitSuccess);
            Assert.AreEqual("¡Comida y progresos registrados con éxito!", _vm.SuccessMessage);

            _mockMealService.Verify(s => s.CreateMealAsync(It.Is<CreateMealRequest>(r => r.name == "Lentejas caseras" && r.mealCourse == "MAIN_DISH")), Times.Once);
            _mockEventService.Verify(e => e.RecordClientEventAsync(It.Is<CreateClientEventRequest>(r => r.eventType == "MEAL_LEGUMES")), Times.Once);
        }

        [Test]
        public void ToggleQuestion_WhenSwapSelector_InitializesSelectedSwapOptionAndEventType()
        {
            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q-swap",
                    Prompt = "¿Qué intercambio has realizado?",
                    QuestionType = DirectQuestionType.SwapSelector,
                    SwapOptions = new[] { "SWAP_BEEF_TO_LEGUMES", "SWAP_BEEF_TO_CHICKEN" },
                    IsChecked = false
                }
            };

            _vm.ToggleQuestion("q-swap");
            Assert.IsTrue(_vm.Questions[0].IsChecked);
            Assert.AreEqual("SWAP_BEEF_TO_LEGUMES", _vm.Questions[0].SelectedSwapOption);
            Assert.AreEqual("SWAP_BEEF_TO_LEGUMES", _vm.Questions[0].EventType);
        }

        [Test]
        public void SelectSwapForQuestion_UpdatesSelectedSwapAndEventType()
        {
            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q-swap",
                    Prompt = "¿Qué intercambio has realizado?",
                    QuestionType = DirectQuestionType.SwapSelector,
                    SwapOptions = new[] { "SWAP_BEEF_TO_LEGUMES", "SWAP_BEEF_TO_CHICKEN" },
                    IsChecked = false,
                    SelectedSwapOption = null,
                    EventType = null
                }
            };

            _vm.SelectSwapForQuestion("q-swap", "SWAP_BEEF_TO_CHICKEN");
            Assert.IsTrue(_vm.Questions[0].IsChecked);
            Assert.AreEqual("SWAP_BEEF_TO_CHICKEN", _vm.Questions[0].SelectedSwapOption);
            Assert.AreEqual("SWAP_BEEF_TO_CHICKEN", _vm.Questions[0].EventType);

            // Clicking the same selected chip deselects it
            _vm.SelectSwapForQuestion("q-swap", "SWAP_BEEF_TO_CHICKEN");
            Assert.IsFalse(_vm.Questions[0].IsChecked);
            Assert.IsNull(_vm.Questions[0].SelectedSwapOption);
            Assert.IsNull(_vm.Questions[0].EventType);
        }

        [Test]
        public async Task SubmitQuickMealLogAsync_WithSwapSelection_EmitsSelectedSwapEvent()
        {
            _vm.SelectedMealType = "LUNCH";
            _vm.MealName = "Pollo a la plancha";
            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q-swap",
                    Prompt = "¿Qué intercambio has realizado?",
                    QuestionType = DirectQuestionType.SwapSelector,
                    SwapOptions = new[] { "SWAP_BEEF_TO_LEGUMES", "SWAP_BEEF_TO_CHICKEN" },
                    IsChecked = true,
                    SelectedSwapOption = "SWAP_BEEF_TO_CHICKEN",
                    EventType = "SWAP_BEEF_TO_CHICKEN"
                }
            };

            _mockMealService.Setup(s => s.CreateMealAsync(It.IsAny<CreateMealRequest>()))
                .ReturnsAsync((new Meal { id = "m-1", name = "Pollo a la plancha" }, null));
            _mockMealLogService.Setup(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog { id = "ml-1" }, null));
            _mockEventService.Setup(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()))
                .ReturnsAsync((new UserEvent { id = "ev-swap", eventType = "SWAP_BEEF_TO_CHICKEN" }, null));

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsTrue(success);
            _mockEventService.Verify(e => e.RecordClientEventAsync(It.Is<CreateClientEventRequest>(r => r.eventType == "SWAP_BEEF_TO_CHICKEN")), Times.Once);
        }

        [Test]
        public async Task LoadCatalogDataAsync_PopulatesMealTypeLabels()
        {
            await _vm.LoadCatalogDataAsync();

            Assert.IsNotNull(_vm.MealTypeLabels);
            Assert.AreEqual(4, _vm.MealTypeLabels.Count);
            Assert.IsTrue(_vm.MealTypeLabels.ContainsKey("BREAKFAST"));
            Assert.IsTrue(_vm.MealTypeLabels["BREAKFAST"].Contains("Desayuno"));
            Assert.IsTrue(_vm.MealTypeLabels.ContainsKey("LUNCH"));
            Assert.IsTrue(_vm.MealTypeLabels["LUNCH"].Contains("Almuerzo"));
        }
    }
}
