using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MissionServiceTests
    {
        private TestStoreService _storeService;
        private MissionService _service;
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
            _service = new MissionService(_storeService);
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
        }

        [Test]
        public async Task GetMissionAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetMissionAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetMissionAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetMissionProgressAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetMissionProgressAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetMissionProgressAsync(null);
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
        public async Task UpdateMissionProgressAsync_OnEmptyCodeOrId_ReturnsError()
        {
            var (result, error) = await _service.UpdateMissionProgressAsync("", true, 100f);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Mission code or id is required", error.message);

            var (resultNull, errorNull) = await _service.UpdateMissionProgressAsync(null, true, 100f);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
        }

        [Test]
        public async Task UpdateMissionProgressAsync_WhenUnauthenticated_ReturnsAuthError()
        {
            _storeService.SetAppState(new AppState
            {
                accessToken = null,
                tokenType = null
            });

            var (result, error) = await _service.UpdateMissionProgressAsync("M.A1.1", true, 100f);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Authentication required", error.message);
        }
    }
}
