using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Unity.AppUI.Navigation;
using Unity.AppUI.Navigation.Generated;
using UnityEngine.TestTools;
using UnityEngine;
using System.Linq;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuizzesViewModelTests
    {
        private Mock<IQuizService> _mockQuizService;
        private Mock<IDimensionService> _mockDimensionService;
        private TestStoreService _storeService;
        private QuizzesViewModel _vm;
        private Func<bool> _originalOverride;

        private Dimension[] _mockDimensions;
        private Quiz[] _mockQuizzes;
        private QuizProgress[] _mockProgress;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockQuizService = new Mock<IQuizService>();
            _mockDimensionService = new Mock<IDimensionService>();
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-token",
                tokenType = "Bearer",
                lang = "es"
            });

            _mockDimensions = new[]
            {
                new Dimension
                {
                    id = "dim-1",
                    code = "DIET_CHANGES",
                    name = "Cambios en la dieta",
                    sortOrder = 1,
                    topics = new[]
                    {
                        new Topic { id = "top-1", code = "REDUCING_MEAT_CONSUMPTION", name = "Reducción de carne", dimensionId = "dim-1", sortOrder = 1 },
                        new Topic { id = "top-2", code = "ALTERNATIVE_STAPLE_FOODS", name = "Alimentos básicos alternativos", dimensionId = "dim-1", sortOrder = 2 }
                    }
                },
                new Dimension
                {
                    id = "dim-2",
                    code = "FOOD_WASTE",
                    name = "Desperdicio de comida",
                    sortOrder = 2,
                    topics = new[]
                    {
                        new Topic { id = "top-3", code = "PLATE_WASTE", name = "Restos del plato", dimensionId = "dim-2", sortOrder = 1 }
                    }
                }
            };

            _mockQuizzes = new[]
            {
                new Quiz
                {
                    id = "q-1",
                    code = "Q1.1.2",
                    topicId = "top-1",
                    question = "¿Segunda pregunta?",
                    level = QuizLevel.Beginner
                },
                new Quiz
                {
                    id = "q-2",
                    code = "Q1.1.1",
                    topicId = "top-1",
                    question = "¿Primera pregunta?",
                    level = QuizLevel.Intermediate
                },
                new Quiz
                {
                    id = "q-3",
                    code = "Q1.2.1",
                    topicId = "top-2",
                    question = "¿Pregunta staples?",
                    level = QuizLevel.Advanced
                },
                new Quiz
                {
                    id = "q-4",
                    code = "Q5.1.1",
                    topicId = "top-3",
                    question = "¿Pregunta desperdicio?",
                    level = QuizLevel.Beginner
                }
            };

            _mockProgress = new[]
            {
                new QuizProgress
                {
                    quizId = "q-2",
                    quizCode = "Q1.1.1",
                    completed = true,
                    isCorrect = true
                }
            };

            _mockDimensionService.Setup(d => d.IsLoaded).Returns(true);
            _mockDimensionService.Setup(d => d.GetAllDimensions()).Returns(_mockDimensions);
            _mockDimensionService.Setup(d => d.GetTopicsForDimension("DIET_CHANGES")).Returns(_mockDimensions[0].topics);
            _mockDimensionService.Setup(d => d.GetTopicsForDimension("FOOD_WASTE")).Returns(_mockDimensions[1].topics);

            _mockQuizService.Setup(q => q.GetQuizzesAsync(It.IsAny<QuizFilterParams>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((new PaginatedQuizResponse { data = _mockQuizzes, meta = new PaginationMeta { total = 4 } }, null));

            _mockQuizService.Setup(q => q.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((_mockProgress, null));

            _vm = new QuizzesViewModel(_storeService, _mockQuizService.Object, _mockDimensionService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesWithDefaults()
        {
            Assert.IsFalse(_vm.IsLoading);
            Assert.AreEqual(QuizFilterLevel.All, _vm.SelectedLevel);
            Assert.AreEqual(QuizFilterStatus.All, _vm.SelectedStatus);
            Assert.IsNull(_vm.ErrorDetail);
            Assert.IsNull(_vm.ErrorMessage);
            Assert.AreEqual(0, _vm.DisplayGroups.Count);
        }

        [Test]
        public async Task LoadDataAsync_PopulatesGroups_AndSortsQuizzesByCode()
        {
            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.AreEqual(4, _vm.TotalQuizzesCount);
            Assert.AreEqual(1, _vm.CompletedQuizzesCount);
            Assert.AreEqual(2, _vm.DisplayGroups.Count);

            // First Dimension: DIET_CHANGES
            var dim1Group = _vm.DisplayGroups[0];
            Assert.AreEqual("DIET_CHANGES", dim1Group.Dimension.code);
            Assert.AreEqual(3, dim1Group.TotalCount);
            Assert.AreEqual(1, dim1Group.CompletedCount);
            Assert.AreEqual(2, dim1Group.Topics.Count);

            // First Topic under DIET_CHANGES: REDUCING_MEAT_CONSUMPTION
            var topic1 = dim1Group.Topics[0];
            Assert.AreEqual("REDUCING_MEAT_CONSUMPTION", topic1.Topic.code);
            Assert.AreEqual(2, topic1.Quizzes.Count);

            // Verifies sort order: Q1.1.1 should precede Q1.1.2
            Assert.AreEqual("Q1.1.1", topic1.Quizzes[0].Quiz.code);
            Assert.IsTrue(topic1.Quizzes[0].IsCompleted);
            Assert.AreEqual(true, topic1.Quizzes[0].IsCorrect);

            Assert.AreEqual("Q1.1.2", topic1.Quizzes[1].Quiz.code);
            Assert.IsFalse(topic1.Quizzes[1].IsCompleted);

            // Second Dimension: FOOD_WASTE
            var dim2Group = _vm.DisplayGroups[1];
            Assert.AreEqual("FOOD_WASTE", dim2Group.Dimension.code);
            Assert.AreEqual(1, dim2Group.TotalCount);
            Assert.AreEqual(0, dim2Group.CompletedCount);
        }

        [Test]
        public async Task LoadDataAsync_OnApiError_SetsErrorDetail()
        {
            _mockQuizService.Setup(q => q.GetQuizzesAsync(It.IsAny<QuizFilterParams>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((null, new ApiErrorResponse { message = "Failed to load quizzes" }));

            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual("Failed to load quizzes", _vm.ErrorMessage);
            Assert.AreEqual(0, _vm.DisplayGroups.Count);
        }

        [Test]
        public async Task SetLevelFilter_FiltersQuizzesByLevel()
        {
            await _vm.LoadDataAsync();

            // Filter by BEGINNER
            _vm.SetLevelFilter(QuizLevel.Beginner);
            Assert.AreEqual(QuizLevel.Beginner, _vm.SelectedLevel);
            Assert.AreEqual(2, _vm.TotalQuizzesCount); // Q1.1.2 and Q5.1.1
            Assert.AreEqual(0, _vm.CompletedQuizzesCount);

            // Filter by ADVANCED
            _vm.SetLevelFilter(QuizLevel.Advanced);
            Assert.AreEqual(QuizLevel.Advanced, _vm.SelectedLevel);
            Assert.AreEqual(1, _vm.TotalQuizzesCount); // Q1.2.1
            Assert.AreEqual(1, _vm.DisplayGroups.Count);
            Assert.AreEqual("DIET_CHANGES", _vm.DisplayGroups[0].Dimension.code);

            // Reset to ALL
            _vm.SetLevelFilter(QuizFilterLevel.All);
            Assert.AreEqual(4, _vm.TotalQuizzesCount);
        }

        [Test]
        public async Task SetStatusFilter_FiltersQuizzesByCompletion()
        {
            await _vm.LoadDataAsync();

            // Completed filter
            _vm.SetStatusFilter(QuizFilterStatus.Completed);
            Assert.AreEqual(QuizFilterStatus.Completed, _vm.SelectedStatus);
            Assert.AreEqual(1, _vm.DisplayGroups.Count);
            Assert.AreEqual(1, _vm.DisplayGroups[0].Topics[0].Quizzes.Count);
            Assert.AreEqual("Q1.1.1", _vm.DisplayGroups[0].Topics[0].Quizzes[0].Quiz.code);

            // Pending filter
            _vm.SetStatusFilter(QuizFilterStatus.Pending);
            Assert.AreEqual(QuizFilterStatus.Pending, _vm.SelectedStatus);
            Assert.AreEqual(2, _vm.DisplayGroups.Count);
            Assert.AreEqual(3, _vm.TotalQuizzesCount - _vm.CompletedQuizzesCount);
        }

        [Test]
        public async Task ToggleDimensionExpanded_TogglesExpandedState()
        {
            await _vm.LoadDataAsync();

            Assert.IsFalse(_vm.DisplayGroups[0].IsExpanded);

            _vm.ToggleDimensionExpanded("DIET_CHANGES");
            Assert.IsTrue(_vm.DisplayGroups[0].IsExpanded);

            _vm.ToggleDimensionExpanded("DIET_CHANGES");
            Assert.IsFalse(_vm.DisplayGroups[0].IsExpanded);
        }

        [Test]
        public void OpenQuiz_RaisesNavigationRequested_WithQuizCodeAndId()
        {
            string requestedAction = null;
            Argument[] requestedArgs = null;

            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
                requestedArgs = args;
            };

            var quiz = new Quiz { id = "quiz-123", code = "Q1.1.1" };
            _vm.OpenQuiz(quiz);

            Assert.AreEqual(Actions.open_quiz, requestedAction);
            Assert.IsNotNull(requestedArgs);
            Assert.AreEqual(2, requestedArgs.Length);
            Assert.AreEqual("code", requestedArgs[0].name);
            Assert.AreEqual("Q1.1.1", requestedArgs[0].value);
            Assert.AreEqual("id", requestedArgs[1].name);
            Assert.AreEqual("quiz-123", requestedArgs[1].value);
        }

        [Test]
        public async Task LanguageChange_TriggersReload()
        {
            await _vm.LoadDataAsync();

            _mockQuizService.Invocations.Clear();

            _storeService.store.Dispatch(AppActions.setLanguage.Invoke("en"));

            // Allow async handler to run
            await Task.Yield();

            _mockQuizService.Verify(q => q.GetQuizzesAsync(It.IsAny<QuizFilterParams>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Test]
        public async Task LoadDataAsync_QuizWithIncorrectAnswer_IsNotMarkedCompleted()
        {
            var progressWithIncorrect = new[]
            {
                new QuizProgress
                {
                    quizId = "q-2",
                    quizCode = "Q1.1.1",
                    completed = true,
                    isCorrect = false // Answered but incorrect
                }
            };

            _mockQuizService.Setup(q => q.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((progressWithIncorrect, null));

            await _vm.LoadDataAsync();

            Assert.AreEqual(0, _vm.CompletedQuizzesCount);
            Assert.AreEqual(4, _vm.TotalQuizzesCount);

            var quizItem = _vm.DisplayGroups[0].Topics[0].Quizzes.First(q => q.Quiz.code == "Q1.1.1");
            Assert.IsFalse(quizItem.IsCompleted);
            Assert.AreEqual(false, quizItem.IsCorrect);
        }

        [Test]
        public async Task OpenRandomQuiz_SelectsPendingQuiz_WhenAvailable()
        {
            await _vm.LoadDataAsync();

            string requestedAction = null;
            Argument[] requestedArgs = null;
            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
                requestedArgs = args;
            };

            // Selected level Beginner: pending are Q1.1.2 and Q5.1.1
            _vm.SetLevelFilter(QuizLevel.Beginner);
            _vm.OpenRandomQuiz();

            Assert.AreEqual(Actions.open_quiz, requestedAction);
            Assert.IsNotNull(requestedArgs);
            string chosenCode = requestedArgs.First(a => a.name == "code").value;
            Assert.IsTrue(chosenCode == "Q1.1.2" || chosenCode == "Q5.1.1");
        }

        [Test]
        public async Task OpenRandomQuiz_SelectsAnyQuiz_WhenAllMatchingCompleted()
        {
            // All beginner quizzes completed
            var progressAllBeginner = new[]
            {
                new QuizProgress { quizId = "q-1", quizCode = "Q1.1.2", completed = true, isCorrect = true },
                new QuizProgress { quizId = "q-4", quizCode = "Q5.1.1", completed = true, isCorrect = true }
            };

            _mockQuizService.Setup(q => q.GetUserProgressListAsync(It.IsAny<string>()))
                .ReturnsAsync((progressAllBeginner, null));

            await _vm.LoadDataAsync();

            string requestedAction = null;
            Argument[] requestedArgs = null;
            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
                requestedArgs = args;
            };

            _vm.SetLevelFilter(QuizLevel.Beginner);
            _vm.OpenRandomQuiz();

            Assert.AreEqual(Actions.open_quiz, requestedAction);
            Assert.IsNotNull(requestedArgs);
            string chosenCode = requestedArgs.First(a => a.name == "code").value;
            Assert.IsTrue(chosenCode == "Q1.1.2" || chosenCode == "Q5.1.1");
        }

        [Test]
        public void OpenRandomQuiz_DoesNothing_WhenNoQuizzesLoaded()
        {
            string requestedAction = null;
            _vm.NavigationRequested += (action, args) => requestedAction = action;

            _vm.OpenRandomQuiz();

            Assert.IsNull(requestedAction);
        }
    }
}
