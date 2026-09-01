using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class PilotSurveyViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<ISurveyService> _mockSurveyService;
        private Mock<IPilotSurveyService> _mockPilotSurveyService;
        private PilotSurveyViewModel _viewModel;

        private SurveyDto CreateSampleSurvey()
        {
            return new SurveyDto
            {
                id = "survey-101",
                slug = "second-use",
                title = "segundo uso",
                description = "Encuesta: segundo uso",
                questions = new[]
                {
                    new QuestionDto
                    {
                        id = "q1",
                        key = "second_use_0",
                        text = "Pregunta 1",
                        order = 0,
                        answers = new[]
                        {
                            new AnswerOptionDto { value = 1, label = "Totalmente en desacuerdo" },
                            new AnswerOptionDto { value = 5, label = "Totalmente de acuerdo" }
                        }
                    },
                    new QuestionDto
                    {
                        id = "q2",
                        key = "second_use_1",
                        text = "Pregunta 2",
                        order = 1,
                        answers = new[]
                        {
                            new AnswerOptionDto { value = 1, label = "Totalmente en desacuerdo" },
                            new AnswerOptionDto { value = 5, label = "Totalmente de acuerdo" }
                        }
                    }
                }
            };
        }

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                userId = "user-1",
                userCountry = "de",
                lang = "es"
            });

            _mockSurveyService = new Mock<ISurveyService>();
            _mockPilotSurveyService = new Mock<IPilotSurveyService>();

            _viewModel = new PilotSurveyViewModel(_storeService, _mockSurveyService.Object, _mockPilotSurveyService.Object);
        }

        [Test]
        public void SetSurvey_InitializesQuestionsAndUnselectedAnswers()
        {
            var survey = CreateSampleSurvey();
            _viewModel.SetSurvey(survey);

            Assert.AreEqual(2, _viewModel.StepCount);
            Assert.AreEqual("survey-101", _viewModel.SurveyId);
            Assert.AreEqual("second-use", _viewModel.SurveySlug);
            Assert.AreEqual("segundo uso", _viewModel.SurveyTitle);
            Assert.AreEqual(2, _viewModel.Questions.Length);
            Assert.AreEqual(-1, _viewModel.GetAnswer(0));
            Assert.AreEqual(-1, _viewModel.GetAnswer(1));
            Assert.IsTrue(_viewModel.CanGoNext, "Steps are non-mandatory, so user can proceed without answering");
        }

        [Test]
        public void SetAnswer_UpdatesValueCorrectly()
        {
            var survey = CreateSampleSurvey();
            _viewModel.SetSurvey(survey);

            _viewModel.SetAnswer(0, 4);

            Assert.AreEqual(4, _viewModel.GetAnswer(0));
            Assert.IsTrue(_viewModel.CanGoNext);
        }

        [Test]
        public async Task Navigation_GoNextAndGoPrevious_ChangesStepIndexEvenIfUnanswered()
        {
            var survey = CreateSampleSurvey();
            _viewModel.SetSurvey(survey);

            // Navigate without answering step 0
            await _viewModel.GoNextAsync();

            Assert.AreEqual(1, _viewModel.CurrentStepIndex);
            Assert.IsTrue(_viewModel.CanGoPrevious);
            Assert.IsTrue(_viewModel.CanGoNext);

            await _viewModel.GoPreviousAsync();
            Assert.AreEqual(0, _viewModel.CurrentStepIndex);
        }

        [Test]
        public async Task FlowCompletion_SubmitsAnswersAndMarksCompleted()
        {
            var survey = CreateSampleSurvey();
            _viewModel.SetSurvey(survey);

            _mockSurveyService.Setup(s => s.SubmitSurveyResponseAsync("survey-101", It.IsAny<SubmitSurveyResponseDto>()))
                .ReturnsAsync((new SurveyResponseDto { id = "resp-1" }, null));

            _viewModel.SetAnswer(0, 4);
            await _viewModel.GoNextAsync();

            _viewModel.SetAnswer(1, 5);
            await _viewModel.GoNextAsync(); // Flow completes

            _mockSurveyService.Verify(s => s.SubmitSurveyResponseAsync("survey-101", It.Is<SubmitSurveyResponseDto>(dto =>
                dto.responses.Length == 2 &&
                dto.responses[0].questionId == "q1" && dto.responses[0].value == 4 &&
                dto.responses[1].questionId == "q2" && dto.responses[1].value == 5
            )), Times.Once);

            _mockPilotSurveyService.Verify(p => p.MarkSurveyCompletedAsync("second-use", "survey-101"), Times.Once);
        }

        [Test]
        public async Task FlowCompletion_WhenPartiallyAnswered_SubmitsOnlyAnsweredQuestions()
        {
            var survey = CreateSampleSurvey();
            _viewModel.SetSurvey(survey);

            _mockSurveyService.Setup(s => s.SubmitSurveyResponseAsync("survey-101", It.IsAny<SubmitSurveyResponseDto>()))
                .ReturnsAsync((new SurveyResponseDto { id = "resp-1" }, null));

            // Skip step 0, answer only step 1
            await _viewModel.GoNextAsync();

            _viewModel.SetAnswer(1, 3);
            await _viewModel.GoNextAsync(); // Flow completes

            _mockSurveyService.Verify(s => s.SubmitSurveyResponseAsync("survey-101", It.Is<SubmitSurveyResponseDto>(dto =>
                dto.responses.Length == 1 &&
                dto.responses[0].questionId == "q2" && dto.responses[0].value == 3
            )), Times.Once);

            _mockPilotSurveyService.Verify(p => p.MarkSurveyCompletedAsync("second-use", "survey-101"), Times.Once);
        }

        [Test]
        public void PostponeFlow_CallsPilotServiceAndRequestsNavigation()
        {
            var survey = CreateSampleSurvey();
            _viewModel.SetSurvey(survey);

            _viewModel.PostponeFlow();

            _mockPilotSurveyService.Verify(p => p.PostponeSurvey("second-use"), Times.Once);
        }

        [Test]
        public void SkipFlow_CallsPilotServiceAndRequestsNavigation()
        {
            var survey = CreateSampleSurvey();
            _viewModel.SetSurvey(survey);

            _viewModel.SkipFlow();

            _mockPilotSurveyService.Verify(p => p.SkipSurvey("second-use"), Times.Once);
        }

        [Test]
        public void GetStepNutriMessage_ReturnsQuestionTextForCurrentStep()
        {
            var survey = CreateSampleSurvey();
            _viewModel.SetSurvey(survey);

            Assert.AreEqual("Pregunta 1", _viewModel.GetStepNutriMessage(0));
            Assert.AreEqual("Pregunta 2", _viewModel.GetStepNutriMessage(1));
            Assert.AreEqual("", _viewModel.GetStepNutriMessage(2));
        }
    }
}
