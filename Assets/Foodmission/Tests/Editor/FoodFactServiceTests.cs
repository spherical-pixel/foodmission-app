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
        private TestLocalStorageService _localStorageService;
        private FoodFactService _service;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _localStorageService = new TestLocalStorageService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-jwt-token",
                tokenType = "Bearer",
                lang = "es"
            });
            _service = new FoodFactService(_storeService, _localStorageService);
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

        [Test]
        public async Task MarkAsReadAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.MarkAsReadAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.MarkAsReadAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task MarkAsReadAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*MarkAsReadAsync.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*MarkAsReadAsync.*"));

            var (result, error) = await _service.MarkAsReadAsync("FF1.1.1");

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetUserProgressListAsync_WhenUnauthenticated_ReturnsOnlyAnError()
        {
            // The read list is per user and lives in the backend: a device-wide copy leaked one account's reads into another
            _storeService.SetAppState(new AppState { accessToken = null });

            var (result, error) = await _service.GetUserProgressListAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetUserProgressListAsync_OnFailure_NeverReturnsReadsKeptOnTheDevice()
        {
            _storeService.SetAppState(new AppState { accessToken = null });
            _localStorageService.SetValue("fm_read_food_facts", new System.Collections.Generic.List<string> { "FACT_OTHER_ACCOUNT" });
            var service = new FoodFactService(_storeService, _localStorageService);

            var (result, error) = await service.GetUserProgressListAsync();

            Assert.IsTrue(result == null || !System.Array.Exists(result, p => p.foodFactCode == "FACT_OTHER_ACCOUNT"));
            Assert.IsNull(_localStorageService.GetValue<System.Collections.Generic.List<string>>("fm_read_food_facts"), "legacy device-wide list is removed");
        }

        [Test]
        public async Task GetUserProgressListAsync_WithoutServerList_UsesOnlyTheSignedInUsersReads()
        {
            // The backend has no GET /food-facts/progress yet: reads recorded on this device stand in, per user
            _storeService.SetAppState(new AppState { accessToken = "test-jwt-token", tokenType = "Bearer", userId = "u2" });
            _localStorageService.SetValue(FoodFactService.ReadFactsKey("u1"), new System.Collections.Generic.List<string> { "FACT_OF_U1" });
            _localStorageService.SetValue(FoodFactService.ReadFactsKey("u2"), new System.Collections.Generic.List<string> { "FACT_OF_U2" });

            var (result, _) = await _service.GetUserProgressListAsync();

            Assert.IsTrue(System.Array.Exists(result, p => p.foodFactCode == "FACT_OF_U2"));
            Assert.IsFalse(System.Array.Exists(result, p => p.foodFactCode == "FACT_OF_U1"));
        }
    }
}
