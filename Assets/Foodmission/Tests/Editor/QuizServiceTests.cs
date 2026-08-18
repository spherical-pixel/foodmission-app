using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuizServiceTests
    {
        private TestStoreService _storeService;
        private QuizService _service;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-jwt-token",
                tokenType = "Bearer",
                lang = "es"
            });
            _service = new QuizService(_storeService);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        [Test]
        public async Task GetQuizAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetQuizAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetQuizAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetQuizProgressAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetQuizProgressAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetQuizProgressAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task SubmitQuizAnswerAsync_OnEmptyCodeOrId_ReturnsError()
        {
            var (result, error) = await _service.SubmitQuizAnswerAsync("", "A");
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.IsTrue(error.message.Contains("code or id is required"));

            var (resultNull, errorNull) = await _service.SubmitQuizAnswerAsync(null, "A");
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
            Assert.IsTrue(errorNull.message.Contains("code or id is required"));
        }

        [Test]
        public async Task SubmitQuizAnswerAsync_OnEmptySelectedLabel_ReturnsError()
        {
            var (result, error) = await _service.SubmitQuizAnswerAsync("Q1.1.1", "");
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.IsTrue(error.message.Contains("Selected option label is required"));

            var (resultNull, errorNull) = await _service.SubmitQuizAnswerAsync("Q1.1.1", null);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
            Assert.IsTrue(errorNull.message.Contains("Selected option label is required"));
        }

        [Test]
        public async Task GetQuizzesAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*GetQuizzesAsync.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*GetQuizzesAsync.*"));

            var (result, error) = await _service.GetQuizzesAsync(new QuizFilterParams
            {
                dimensionCode = "DIET_CHANGES",
                level = QuizLevel.Beginner
            });

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetUserProgressListAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*GetUserProgressListAsync.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*GetUserProgressListAsync.*"));

            var (result, error) = await _service.GetUserProgressListAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }
    }
}
