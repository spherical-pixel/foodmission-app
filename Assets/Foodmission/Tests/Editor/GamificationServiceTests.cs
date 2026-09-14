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
    }
}
