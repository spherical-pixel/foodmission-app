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
            Assert.AreEqual(3, _vm.Questions.Count);
            Assert.IsTrue(_vm.Questions.All(q => !q.IsChecked));
        }

        [Test]
        public async Task LoadActiveQuestQuestionsAsync_WhenActiveQuestExists_SetsQuestInfoAndLoadsStandardSections()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "q-active-1" });

            var mockQuest = new Quest
            {
                id = "q-active-1",
                code = "QUEST.LEGUMES",
                title = "Aventura de las Legumbres"
            };

            _mockQuestService.Setup(s => s.GetQuestAsync("q-active-1", It.IsAny<string>()))
                .ReturnsAsync((mockQuest, null));

            await _vm.LoadActiveQuestQuestionsAsync();

            Assert.AreEqual("Aventura de las Legumbres", _vm.ActiveQuestTitle);
            Assert.AreEqual("QUEST.LEGUMES", _vm.ActiveQuestCode);
            Assert.IsNotNull(_vm.Sections);
            Assert.AreEqual(3, _vm.Sections.Count);
            Assert.IsTrue(_vm.Sections.Any(s => s.Id == "sec_diet"));
            Assert.IsTrue(_vm.Sections.Any(s => s.Id == "sec_swaps"));
            Assert.IsTrue(_vm.Sections.Any(s => s.Id == "sec_nutrition"));
            Assert.IsFalse(_vm.Sections.Any(s => s.Id == "sec_waste"));
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
        public void ToggleQuestion_WhenMeatFreeSelected_UnchecksMeatConsumed()
        {
            var meatConsumedItem = new QuickMealCheckItem
            {
                Id = "q_meat_consumed",
                EventType = ClientEventTypes.MealMeatConsumed,
                IsChecked = true
            };
            var meatFreeItem = new QuickMealCheckItem
            {
                Id = "q_meat_free",
                EventType = ClientEventTypes.MealMeatFree,
                IsChecked = false
            };

            _vm.Sections = new System.Collections.Generic.List<QuickMealSection>
            {
                new QuickMealSection
                {
                    Id = "sec_diet",
                    Items = new System.Collections.Generic.List<QuickMealCheckItem> { meatConsumedItem, meatFreeItem }
                }
            };

            _vm.ToggleQuestion("q_meat_free");

            Assert.IsTrue(meatFreeItem.IsChecked);
            Assert.IsFalse(meatConsumedItem.IsChecked);
        }

        [Test]
        public void ToggleQuestion_WhenMeatConsumedSelected_UnchecksMeatFree()
        {
            var meatConsumedItem = new QuickMealCheckItem
            {
                Id = "q_meat_consumed",
                EventType = ClientEventTypes.MealMeatConsumed,
                IsChecked = false
            };
            var meatFreeItem = new QuickMealCheckItem
            {
                Id = "q_meat_free",
                EventType = ClientEventTypes.MealMeatFree,
                IsChecked = true
            };

            _vm.Sections = new System.Collections.Generic.List<QuickMealSection>
            {
                new QuickMealSection
                {
                    Id = "sec_diet",
                    Items = new System.Collections.Generic.List<QuickMealCheckItem> { meatConsumedItem, meatFreeItem }
                }
            };

            _vm.ToggleQuestion("q_meat_consumed");

            Assert.IsTrue(meatConsumedItem.IsChecked);
            Assert.IsFalse(meatFreeItem.IsChecked);
        }

        [Test]
        public void ToggleQuestion_WhenMeatConsumedSelected_UnchecksVegan()
        {
            var meatConsumedItem = new QuickMealCheckItem
            {
                Id = "q_meat_consumed",
                EventType = ClientEventTypes.MealMeatConsumed,
                IsChecked = false
            };
            var veganItem = new QuickMealCheckItem
            {
                Id = "q_vegan",
                EventType = ClientEventTypes.MealVegan,
                IsChecked = true
            };

            _vm.Sections = new System.Collections.Generic.List<QuickMealSection>
            {
                new QuickMealSection
                {
                    Id = "sec_diet",
                    Items = new System.Collections.Generic.List<QuickMealCheckItem> { meatConsumedItem, veganItem }
                }
            };

            _vm.ToggleQuestion("q_meat_consumed");

            Assert.IsTrue(meatConsumedItem.IsChecked);
            Assert.IsFalse(veganItem.IsChecked);
        }

        [Test]
        public void ToggleQuestion_WhenVeganSelected_UnchecksMeatConsumed()
        {
            var meatConsumedItem = new QuickMealCheckItem
            {
                Id = "q_meat_consumed",
                EventType = ClientEventTypes.MealMeatConsumed,
                IsChecked = true
            };
            var veganItem = new QuickMealCheckItem
            {
                Id = "q_vegan",
                EventType = ClientEventTypes.MealVegan,
                IsChecked = false
            };

            _vm.Sections = new System.Collections.Generic.List<QuickMealSection>
            {
                new QuickMealSection
                {
                    Id = "sec_diet",
                    Items = new System.Collections.Generic.List<QuickMealCheckItem> { meatConsumedItem, veganItem }
                }
            };

            _vm.ToggleQuestion("q_vegan");

            Assert.IsTrue(veganItem.IsChecked);
            Assert.IsFalse(meatConsumedItem.IsChecked);
        }

        [Test]
        public void ToggleQuestion_WhenPlantBasedInQuestionsSelected_UnchecksMeatConsumedInSections()
        {
            var plantBasedQuestion = new QuickMealCheckItem
            {
                Id = "q_plant_based",
                EventType = ClientEventTypes.MealMeatFree,
                IsChecked = false
            };
            var meatFreeSectionItem = new QuickMealCheckItem
            {
                Id = "q_meat_free",
                EventType = ClientEventTypes.MealMeatFree,
                IsChecked = false
            };
            var meatConsumedSectionItem = new QuickMealCheckItem
            {
                Id = "q_meat_consumed",
                EventType = ClientEventTypes.MealMeatConsumed,
                IsChecked = true
            };

            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem> { plantBasedQuestion };
            _vm.Sections = new System.Collections.Generic.List<QuickMealSection>
            {
                new QuickMealSection
                {
                    Id = "sec_diet",
                    Items = new System.Collections.Generic.List<QuickMealCheckItem> { meatFreeSectionItem, meatConsumedSectionItem }
                }
            };

            _vm.ToggleQuestion("q_plant_based");

            Assert.IsTrue(plantBasedQuestion.IsChecked);
            Assert.IsTrue(meatFreeSectionItem.IsChecked);
            Assert.IsFalse(meatConsumedSectionItem.IsChecked);
        }

        [Test]
        public void ToggleQuestion_WhenUncheckingMeatFree_DoesNotRecheckMeatConsumed()
        {
            var meatConsumedItem = new QuickMealCheckItem
            {
                Id = "q_meat_consumed",
                EventType = ClientEventTypes.MealMeatConsumed,
                IsChecked = false
            };
            var meatFreeItem = new QuickMealCheckItem
            {
                Id = "q_meat_free",
                EventType = ClientEventTypes.MealMeatFree,
                IsChecked = true
            };

            _vm.Sections = new System.Collections.Generic.List<QuickMealSection>
            {
                new QuickMealSection
                {
                    Id = "sec_diet",
                    Items = new System.Collections.Generic.List<QuickMealCheckItem> { meatConsumedItem, meatFreeItem }
                }
            };

            _vm.ToggleQuestion("q_meat_free");

            Assert.IsFalse(meatFreeItem.IsChecked);
            Assert.IsFalse(meatConsumedItem.IsChecked);
        }

        [Test]
        public async Task SubmitQuickMealLogAsync_SendsFlagsAndSwapsWithoutEmittingSeparateEvents()
        {
            _vm.SelectedMealType = "LUNCH";
            _vm.MealName = "Lentejas caseras";
            _vm.ActiveQuestCode = "QUEST.LEGUMES";
            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem>
            {
                new QuickMealCheckItem { Id = "q1", Prompt = "Q1", EventType = ClientEventTypes.MealLegumeConsumed, IsChecked = true, ActivityCode = "M1" },
                new QuickMealCheckItem { Id = "q2", Prompt = "Q2", EventType = ClientEventTypes.MealMeatFree, IsChecked = false, ActivityCode = "M2" }
            };

            _mockMealService.Setup(s => s.CreateMealAsync(It.IsAny<CreateMealRequest>()))
                .ReturnsAsync((new Meal { id = "created-meal-1", name = "Lentejas caseras" }, null));
            _mockMealLogService.Setup(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog { id = "created-log-1" }, null));

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsTrue(success);
            Assert.IsTrue(_vm.SubmitSuccess);
            Assert.AreEqual("@UI:QUICK_MEAL_LOG_SUCCESS", _vm.SuccessMessage);

            _mockMealLogService.Verify(s => s.CreateAsync(It.Is<CreateMealLogRequest>(r =>
                r.typeOfMeal == "LUNCH" &&
                r.mealId == null &&
                r.flags != null &&
                r.flags.Contains(ClientEventTypes.MealLegumeConsumed))), Times.Once);
            _mockEventService.Verify(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()), Times.Never);
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
        public async Task SubmitQuickMealLogAsync_WithSwapSelection_SendsSwapInMealLogRequest()
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

            _mockMealLogService.Setup(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog { id = "ml-1" }, null));

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsTrue(success);
            _mockMealLogService.Verify(s => s.CreateAsync(It.Is<CreateMealLogRequest>(r =>
                r.typeOfMeal == "LUNCH" &&
                r.swaps != null &&
                r.swaps.Contains("SWAP_BEEF_TO_CHICKEN") &&
                r.flags != null &&
                r.flags.Length == 0 &&
                !r.flags.Contains(ClientEventTypes.MealMeatFree))), Times.Once);
        }

        [Test]
        public async Task SubmitQuickMealLogAsync_WhenNothingChecked_DoesNotSendRequestAndSetsErrorMessage()
        {
            _vm.SelectedMealType = "SNACK";
            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q1",
                    EventType = ClientEventTypes.MealMeatFree,
                    IsChecked = false
                },
                new QuickMealCheckItem
                {
                    Id = "q2",
                    EventType = ClientEventTypes.MealSustainablePlate,
                    IsChecked = false
                }
            };

            string toastRequested = null;
            _vm.ShowToastRequest += msg => toastRequested = msg;

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsFalse(success);
            Assert.IsFalse(_vm.SubmitSuccess);
            Assert.AreEqual("@UI:QUICK_MEAL_LOG_EMPTY_SELECTION", _vm.ErrorMessage);
            Assert.AreEqual("@UI:QUICK_MEAL_LOG_EMPTY_SELECTION", toastRequested);
            _mockMealLogService.Verify(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()), Times.Never);
        }

        [Test]
        public async Task SubmitQuickMealLogAsync_WithFlags_SendsFlagsAndExcludesMeatConsumed()
        {
            _vm.SelectedMealType = "DINNER";
            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q1",
                    EventType = ClientEventTypes.MealMeatFree,
                    IsChecked = true
                },
                new QuickMealCheckItem
                {
                    Id = "q2",
                    EventType = ClientEventTypes.MealLegumeConsumed,
                    IsChecked = true
                },
                new QuickMealCheckItem
                {
                    Id = "q3",
                    EventType = ClientEventTypes.MealMeatConsumed,
                    IsChecked = true
                }
            };

            _mockMealLogService.Setup(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog { id = "ml-2" }, null));

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsTrue(success);
            _mockMealLogService.Verify(s => s.CreateAsync(It.Is<CreateMealLogRequest>(r =>
                r.flags != null &&
                r.flags.Contains(ClientEventTypes.MealMeatFree) &&
                r.flags.Contains(ClientEventTypes.MealLegumeConsumed) &&
                !r.flags.Contains(ClientEventTypes.MealMeatConsumed))), Times.Once);
        }

        [Test]
        public async Task SubmitQuickMealLogAsync_WhenBackendReturnsError_SetsErrorDetailAndReturnsFalse()
        {
            _vm.SelectedMealType = "LUNCH";
            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q1",
                    EventType = ClientEventTypes.MealMeatFree,
                    IsChecked = true
                }
            };

            var expectedError = new ApiErrorResponse
            {
                statusCode = 400,
                message = "Invalid meal log data",
                traceId = "trace-400"
            };

            _mockMealLogService.Setup(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync(((MealLog)null, expectedError));

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsFalse(success);
            Assert.IsFalse(_vm.SubmitSuccess);
            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual(400, _vm.ErrorDetail.statusCode);
            Assert.AreEqual("Invalid meal log data", _vm.ErrorDetail.message);
        }

        [Test]
        public async Task SubmitQuickMealLogAsync_WhenBackendThrowsException_SetsErrorDetailAndReturnsFalse()
        {
            _vm.SelectedMealType = "LUNCH";
            _vm.Questions = new System.Collections.Generic.List<QuickMealCheckItem>
            {
                new QuickMealCheckItem
                {
                    Id = "q1",
                    EventType = ClientEventTypes.MealMeatFree,
                    IsChecked = true
                }
            };

            _mockMealLogService.Setup(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ThrowsAsync(new System.Exception("Connection refused"));

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsFalse(success);
            Assert.IsFalse(_vm.SubmitSuccess);
            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual(500, _vm.ErrorDetail.statusCode);
            Assert.AreEqual("Connection refused", _vm.ErrorDetail.message);
        }

        [Test]
        public async Task LoadCatalogDataAsync_PopulatesMealTypeLabels()
        {
            await _vm.LoadCatalogDataAsync();

            Assert.IsNotNull(_vm.TypeOfMealOptions);
            Assert.AreEqual(4, _vm.TypeOfMealOptions.Length);
            Assert.AreEqual("BREAKFAST", _vm.TypeOfMealOptions[0].code);

            Assert.IsNotNull(_vm.MealTypeLabels);
            Assert.AreEqual(4, _vm.MealTypeLabels.Count);
            Assert.IsTrue(_vm.MealTypeLabels.ContainsKey("BREAKFAST"));
            Assert.IsTrue(_vm.MealTypeLabels["BREAKFAST"].Contains("Desayuno"));
            Assert.IsTrue(_vm.MealTypeLabels.ContainsKey("LUNCH"));
            Assert.IsTrue(_vm.MealTypeLabels["LUNCH"].Contains("Almuerzo"));
        }

        [Test]
        public async Task LoadActiveQuestQuestionsAsync_PopulatesSectionsProperly()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });

            await _vm.LoadActiveQuestQuestionsAsync();

            Assert.IsNotNull(_vm.Sections);
            Assert.AreEqual(3, _vm.Sections.Count);
            Assert.IsTrue(_vm.Sections.Any(s => s.Id == "sec_diet"));
            Assert.IsTrue(_vm.Sections.Any(s => s.Id == "sec_swaps"));
            Assert.IsTrue(_vm.Sections.Any(s => s.Id == "sec_nutrition"));
            Assert.IsFalse(_vm.Sections.Any(s => s.Id == "sec_waste"));

            var dietSec = _vm.Sections.First(s => s.Id == "sec_diet");
            Assert.IsTrue(dietSec.IsExpanded);
            Assert.AreEqual(0, dietSec.SelectedCount);
        }

        [Test]
        public async Task ToggleSection_TogglesIsExpandedState()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });
            await _vm.LoadActiveQuestQuestionsAsync();

            var dietSec = _vm.Sections.First(s => s.Id == "sec_diet");
            Assert.IsTrue(dietSec.IsExpanded);

            _vm.ToggleSection("sec_diet");
            Assert.IsFalse(dietSec.IsExpanded);

            _vm.ToggleSection("sec_diet");
            Assert.IsTrue(dietSec.IsExpanded);
        }

        [Test]
        public async Task ToggleQuestion_InSections_UpdatesSelectedCountAndSubmitsFlags()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });
            await _vm.LoadActiveQuestQuestionsAsync();

            var dietSec = _vm.Sections.First(s => s.Id == "sec_diet");
            Assert.AreEqual(0, dietSec.SelectedCount);

            // Toggle item in diet
            _vm.ToggleQuestion("q_meat_free");
            Assert.AreEqual(1, dietSec.SelectedCount);

            // Toggle item in nutrition
            var nutritionSec = _vm.Sections.First(s => s.Id == "sec_nutrition");
            _vm.ToggleQuestion("q_fruit_veg");
            Assert.AreEqual(1, nutritionSec.SelectedCount);

            _mockMealLogService.Setup(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog { id = "ml-sec-1" }, null));

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsTrue(success);
            // Habit flag goes to meal log, nutrition flag is excluded from meal log flags
            _mockMealLogService.Verify(s => s.CreateAsync(It.Is<CreateMealLogRequest>(r =>
                r.flags != null &&
                r.flags.Contains(ClientEventTypes.MealMeatFree) &&
                !r.flags.Contains(ClientEventTypes.NutritionFruitVegServingAdded))), Times.Once);

            // Nutrition flag is emitted directly as a client event
            _mockEventService.Verify(e => e.RecordClientEventAsync(It.Is<CreateClientEventRequest>(req =>
                req.eventType == ClientEventTypes.NutritionFruitVegServingAdded)), Times.Once);
        }

        [Test]
        public async Task SubmitQuickMealLogAsync_WithOnlyNutritionFlag_EmitsDirectEventAndSucceedsWithoutMealLogCall()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });
            await _vm.LoadActiveQuestQuestionsAsync();

            var nutritionSec = _vm.Sections.First(s => s.Id == "sec_nutrition");
            _vm.ToggleQuestion("q_wholegrain");
            Assert.AreEqual(1, nutritionSec.SelectedCount);

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsTrue(success);
            Assert.IsTrue(_vm.SubmitSuccess);
            Assert.AreEqual("@UI:QUICK_MEAL_LOG_SUCCESS", _vm.SuccessMessage);

            // No meal flags present, so meal log service should not be called with empty flags
            _mockMealLogService.Verify(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()), Times.Never);

            // Event emitted directly
            _mockEventService.Verify(e => e.RecordClientEventAsync(It.Is<CreateClientEventRequest>(req =>
                req.eventType == ClientEventTypes.NutritionWholegrainChosen)), Times.Once);
        }

        [Test]
        public async Task SubmitQuickMealLogAsync_WhenNutritionDirectEmitFailsAndNoMealFlags_ReturnsFalseAndSetsErrorDetail()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });
            await _vm.LoadActiveQuestQuestionsAsync();

            var nutritionSec = _vm.Sections.First(s => s.Id == "sec_nutrition");
            _vm.ToggleQuestion("q_salt_free");

            var expectedErr = new ApiErrorResponse { statusCode = 500, message = "Event failed" };
            _mockEventService.Setup(e => e.RecordClientEventAsync(It.IsAny<CreateClientEventRequest>()))
                .ReturnsAsync(((UserEvent)null, expectedErr));

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsFalse(success);
            Assert.IsFalse(_vm.SubmitSuccess);
            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual(500, _vm.ErrorDetail.statusCode);
            Assert.AreEqual("Event failed", _vm.ErrorDetail.message);
        }

        [Test]
        public async Task ToggleQuestion_InSwapSection_AllowsMultipleSwapsAndSubmitsSwaps()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });
            await _vm.LoadActiveQuestQuestionsAsync();

            var swapSec = _vm.Sections.First(s => s.Id == "sec_swaps");
            Assert.AreEqual(0, swapSec.SelectedCount);
            Assert.AreEqual(11, swapSec.Items.Count);

            string swap1Id = $"q_{ClientEventTypes.SwapBeefToLegumes.ToLowerInvariant()}";
            string swap2Id = $"q_{ClientEventTypes.SwapSugaryDrinkToWater.ToLowerInvariant()}";

            _vm.ToggleQuestion(swap1Id);
            Assert.AreEqual(1, swapSec.SelectedCount);

            _vm.ToggleQuestion(swap2Id);
            Assert.AreEqual(2, swapSec.SelectedCount);

            // Also check a meal flag since backend requires at least one flag
            var dietSec = _vm.Sections.First(s => s.Id == "sec_diet");
            _vm.ToggleQuestion("q_sustainable_plate");
            Assert.AreEqual(1, dietSec.SelectedCount);

            _mockMealLogService.Setup(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()))
                .ReturnsAsync((new MealLog { id = "ml-swap-1" }, null));

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsTrue(success);
            _mockMealLogService.Verify(s => s.CreateAsync(It.Is<CreateMealLogRequest>(r =>
                r.flags != null &&
                r.flags.Contains(ClientEventTypes.MealSustainablePlate) &&
                r.swaps != null &&
                r.swaps.Length == 2 &&
                r.swaps.Contains(ClientEventTypes.SwapBeefToLegumes) &&
                r.swaps.Contains(ClientEventTypes.SwapSugaryDrinkToWater))), Times.Once);
        }

        [Test]
        public async Task SubmitQuickMealLogAsync_WithOnlySwapsAndNoFlags_DoesNotSendRequestAndSetsErrorMessage()
        {
            _storeService.SetAppState(new AppState { userCurrentQuestId = "" });
            await _vm.LoadActiveQuestQuestionsAsync();

            var swapSec = _vm.Sections.First(s => s.Id == "sec_swaps");
            string swap1Id = $"q_{ClientEventTypes.SwapBeefToLegumes.ToLowerInvariant()}";
            _vm.ToggleQuestion(swap1Id);
            Assert.AreEqual(1, swapSec.SelectedCount);

            string toastRequested = null;
            _vm.ShowToastRequest += msg => toastRequested = msg;

            bool success = await _vm.SubmitQuickMealLogAsync();

            Assert.IsFalse(success);
            Assert.IsFalse(_vm.SubmitSuccess);
            Assert.AreEqual("@UI:QUICK_MEAL_LOG_EMPTY_SELECTION", _vm.ErrorMessage);
            Assert.AreEqual("@UI:QUICK_MEAL_LOG_EMPTY_SELECTION", toastRequested);
            _mockMealLogService.Verify(s => s.CreateAsync(It.IsAny<CreateMealLogRequest>()), Times.Never);
        }

        [Test]
        public void NavigateToHome_RaisesNavigationRequestedWithGoToHome()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) => requestedAction = action;

            _vm.NavigateToHome();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.go_to_home, requestedAction);
        }
    }
}

