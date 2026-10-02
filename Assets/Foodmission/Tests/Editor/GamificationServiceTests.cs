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
    }
}
