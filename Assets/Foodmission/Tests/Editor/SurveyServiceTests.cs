using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SurveyServiceTests
    {
        private TestStoreService _storeService;
        private SurveyService _service;

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
            _service = new SurveyService(_storeService);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        [Test]
        public async Task GetSurveyBySlugAsync_OnEmptySlug_ReturnsNull()
        {
            var (result, error) = await _service.GetSurveyBySlugAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetSurveyBySlugAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetSurveyByIdAsync_OnEmptyId_ReturnsNull()
        {
            var (result, error) = await _service.GetSurveyByIdAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetSurveyByIdAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task SubmitSurveyResponseAsync_OnEmptyIdOrNullDto_ReturnsError()
        {
            var (res1, err1) = await _service.SubmitSurveyResponseAsync("", new SubmitSurveyResponseDto());
            Assert.IsNull(res1);
            Assert.IsNotNull(err1);

            var (res2, err2) = await _service.SubmitSurveyResponseAsync("survey-1", null);
            Assert.IsNull(res2);
            Assert.IsNotNull(err2);
        }

        [Test]
        public async Task GetUserSurveyResponseAsync_OnEmptyId_ReturnsNull()
        {
            var (res, err) = await _service.GetUserSurveyResponseAsync("");
            Assert.IsNull(res);
            Assert.IsNull(err);
        }

        [Test]
        public async Task GetUserSurveyResponsesForSurveyAsync_OnEmptyId_ReturnsNull()
        {
            var (res, err) = await _service.GetUserSurveyResponsesForSurveyAsync("");
            Assert.IsNull(res);
            Assert.IsNull(err);
        }
    }
}
