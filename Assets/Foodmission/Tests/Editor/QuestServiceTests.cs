using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class QuestServiceTests
    {
        private TestStoreService _storeService;
        private QuestService _service;

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
            _service = new QuestService(_storeService);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        [Test]
        public async Task GetQuestAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetQuestAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetQuestAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetQuestProgressAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetQuestProgressAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetQuestProgressAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetUserProgressListAsync_WhenUnauthenticated_ReturnsAuthError()
        {
            _storeService.SetAppState(new AppState
            {
                accessToken = null
            });

            var (result, error) = await _service.GetUserProgressListAsync();
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Authentication required", error.message);
        }

        [Test]
        public async Task UpdateQuestProgressAsync_OnEmptyCodeOrId_ReturnsError()
        {
            var (result, error) = await _service.UpdateQuestProgressAsync("", true, 100f);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Quest code or id is required", error.message);

            var (resultNull, errorNull) = await _service.UpdateQuestProgressAsync(null, true, 100f);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
        }
    }
}
