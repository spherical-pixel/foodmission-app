using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class LegalServiceTests
    {
        private TestStoreService _storeService;
        private LegalService _service;

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
            _service = new LegalService(_storeService);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        [Test]
        public async Task GetLatestDocumentAsync_OnEmptyDocType_ReturnsError()
        {
            var (result, error) = await _service.GetLatestDocumentAsync("");
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Document type is required", error.message);

            var (resultNull, errorNull) = await _service.GetLatestDocumentAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
        }

        [Test]
        public async Task AcceptConsentAsync_OnEmptyDocumentKey_ReturnsError()
        {
            var (result, error) = await _service.AcceptConsentAsync("");
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Document key is required", error.message);

            var (resultNull, errorNull) = await _service.AcceptConsentAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNotNull(errorNull);
        }

        [Test]
        public async Task GetConsentStatusAsync_WhenUnauthenticated_ReturnsAuthError()
        {
            _storeService.SetAppState(new AppState
            {
                accessToken = null
            });

            var (result, error) = await _service.GetConsentStatusAsync();
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.AreEqual("Authentication required", error.message);
        }
    }
}
