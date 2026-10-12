using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class GamificationServiceTests
    {
        private TestStoreService _storeService;
        private GamificationService _service;

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
            _service = new GamificationService(_storeService);
        }

        [Test]
        public async Task GetWalletBalanceAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetWalletBalanceAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetEarnedRewardsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetEarnedRewardsAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetGamificationProfileAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetGamificationProfileAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task SubmitOnboardingSurveyAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.SubmitOnboardingSurveyAsync(new OnboardingSurveyData());

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void BuildOnboardingSurveyUrl_TargetsGamificationRoute()
        {
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/onboarding-survey",
                GamificationService.BuildOnboardingSurveyUrl("https://api.test"));
        }

        [Test]
        public void BuildProgressWheelsUrl_TargetsGamificationRoute()
        {
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/progress-wheels",
                GamificationService.BuildProgressWheelsUrl("https://api.test"));
        }

        [Test]
        public async Task GetProgressWheelsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetProgressWheelsAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void ParseDimensionLevels_ReadsCodeAndLevel_IgnoringQuests()
        {
            const string json = "[{\"dimensionCode\":\"DIET_CHANGES\",\"dimensionName\":\"Diet\",\"level\":\"INTERMEDIATE\",\"quests\":[{\"id\":\"q\"}]},{\"dimensionCode\":\"PACKAGING\",\"level\":\"BEGINNER\",\"quests\":[]}]";

            DimensionLevelEntry[] levels = GamificationService.ParseDimensionLevels(json);

            Assert.AreEqual(2, levels.Length);
            Assert.AreEqual(DimensionCode.DietChanges, levels[0].dimensionCode);
            Assert.AreEqual("INTERMEDIATE", levels[0].level);
        }

        [Test]
        public void BuildDimensionsUrl_TargetsGamificationRoute()
        {
            Assert.AreEqual("https://api.test/api/v1/users/me/gamification/dimensions", GamificationService.BuildDimensionsUrl("https://api.test"));
        }

        [Test]
        public async Task GetDimensionLevelsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetDimensionLevelsAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void ParseOnboardingSurvey_ReadsQuestionsInOrder_AndDropsInvalidOnes()
        {
            const string json = "{\"questions\":[" +
                "{\"field\":\"weeklyMeatConsumption\",\"text\":\"How many meals…?\",\"options\":[{\"value\":\"ZERO_TO_FOUR\",\"label\":\"0-4\"},{\"value\":\"FIVE_TO_NINE\",\"label\":\"5-9\"}]}," +
                "{\"field\":\"\",\"text\":\"no field\",\"options\":[{\"value\":\"A\",\"label\":\"A\"}]}," +
                "{\"field\":\"usesLeftovers\",\"text\":\"no options\",\"options\":[]}," +
                "{\"field\":\"wholeGrainFrequency\",\"text\":\"Whole grain?\",\"options\":[{\"value\":\"NEVER\",\"label\":\"Never\"},{\"value\":\"\",\"label\":\"broken\"}]}" +
                "]}";

            OnboardingSurveyQuestion[] questions = GamificationService.ParseOnboardingSurvey(json);

            CollectionAssert.AreEqual(new[] { "weeklyMeatConsumption", "wholeGrainFrequency" }, System.Linq.Enumerable.Select(questions, q => q.field));
            Assert.AreEqual("5-9", questions[0].options[1].label);
            Assert.AreEqual(1, questions[1].options.Length, "options without a value are dropped");
        }

        [TestCase("")]
        [TestCase("{}")]
        [TestCase("{\"questions\":null}")]
        public void ParseOnboardingSurvey_EmptyOrMissing_ReturnsEmpty(string json)
        {
            Assert.AreEqual(0, GamificationService.ParseOnboardingSurvey(json).Length);
        }

        [Test]
        public async Task GetOnboardingSurveyAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.ignoreFailingMessages = true;
            var (result, error) = await _service.GetOnboardingSurveyAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }
    }
}
