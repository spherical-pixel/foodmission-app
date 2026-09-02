using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodFactServiceTests
    {
        private TestStoreService _storeService;
        private FoodFactService _service;

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
            _service = new FoodFactService(_storeService);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        [Test]
        public async Task GetFoodFactAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetFoodFactAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetFoodFactAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetFoodFactByCodeAsync_OnEmptyCode_ReturnsNull()
        {
            var (result, error) = await _service.GetFoodFactByCodeAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetFoodFactByCodeAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetFoodFactsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*GetFoodFactsAsync.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*GetFoodFactsAsync.*"));

            var (result, error) = await _service.GetFoodFactsAsync(new FoodFactFilterParams
            {
                dimensionCode = "DIET_CHANGES",
                level = FoodFactLevel.Beginner
            });

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetFoodFactAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*GetFoodFactAsync.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*GetFoodFactAsync.*"));

            var (result, error) = await _service.GetFoodFactAsync("FF1.1.1");

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetFoodFactByCodeAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*GetFoodFactByCodeAsync.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*GetFoodFactByCodeAsync.*"));

            var (result, error) = await _service.GetFoodFactByCodeAsync("FF1.1.1");

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }
    }
}
