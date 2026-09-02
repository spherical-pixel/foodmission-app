using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ChallengeServiceTests
    {
        private TestStoreService _storeService;
        private ChallengeService _service;
        private Func<bool> _originalOverride;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-jwt-token",
                tokenType = "Bearer",
                lang = "es"
            });
            _service = new ChallengeService(_storeService);
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
        }

        [Test]
        public async Task GetChallengeAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetChallengeAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetChallengeAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetChallengeProgressAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetChallengeProgressAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetChallengeProgressAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetUserProgressListAsync_WhenUnauthenticated_ReturnsAuthError()
        {
            _storeService.SetAppState(new AppState
            {
                accessToken = null,
                tokenType = null
            });

            var (result, error) = await _service.GetUserProgressListAsync();
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Authentication required", error.message);
        }

        [Test]
        public async Task UpdateChallengeProgressAsync_OnEmptyCodeOrId_ReturnsError()
        {
            var (result, error) = await _service.UpdateChallengeProgressAsync("", true, 100f);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Challenge code or id is required", error.message);

            var (resultNull, errorNull) = await _service.UpdateChallengeProgressAsync(null, true, 100f);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
        }

        [Test]
        public async Task UpdateChallengeProgressAsync_WhenUnauthenticated_ReturnsAuthError()
        {
            _storeService.SetAppState(new AppState
            {
                accessToken = null,
                tokenType = null
            });

            var (result, error) = await _service.UpdateChallengeProgressAsync("CH.A1.1", true, 100f);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Authentication required", error.message);
        }
    }
}
