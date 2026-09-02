using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DimensionServiceTests
    {
        private TestStoreService _storeService;
        private DimensionService _service;

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
            _service = new DimensionService(_storeService);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
            _service?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public async Task GetDimensionAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetDimensionAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetDimensionAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public async Task GetTopicAsync_OnEmptyCodeOrId_ReturnsNull()
        {
            var (result, error) = await _service.GetTopicAsync("");
            Assert.IsNull(result);
            Assert.IsNull(error);

            var (resultNull, errorNull) = await _service.GetTopicAsync(null);
            Assert.IsNull(resultNull);
            Assert.IsNull(errorNull);
        }

        [Test]
        public void SynchronousLookups_OnEmptyState_ReturnEmptyOrNull()
        {
            Assert.IsFalse(_service.IsLoaded);
            Assert.AreEqual(0, _service.GetAllDimensions().Count);
            Assert.AreEqual(0, _service.GetAllTopics().Count);
            Assert.IsNull(_service.GetDimension("DIET_CHANGES"));
            Assert.IsNull(_service.GetDimension(""));
            Assert.IsNull(_service.GetTopic("REDUCING_MEAT_CONSUMPTION"));
            Assert.IsNull(_service.GetTopic(""));
            Assert.IsNull(_service.GetDimensionForTopic("REDUCING_MEAT_CONSUMPTION"));
            Assert.IsNull(_service.GetDimensionForTopic(""));
            Assert.AreEqual(0, _service.GetTopicsForDimension("DIET_CHANGES").Count);
            Assert.AreEqual(0, _service.GetTopicsForDimension("").Count);
        }

        [Test]
        public async Task GetDimensionsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*GetDimensionsAsync.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*GetDimensionsAsync.*"));

            var (result, error) = await _service.GetDimensionsAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetDimensionAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*GetDimensionAsync.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*GetDimensionAsync.*"));

            var (result, error) = await _service.GetDimensionAsync("DIET_CHANGES");

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetTopicsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*GetDimensionsAsync.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*GetDimensionsAsync.*"));

            var (result, error) = await _service.GetTopicsAsync();

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public async Task GetTopicAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            LogAssert.Expect(LogType.Error, new Regex(".*GetDimensionsAsync.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*GetDimensionsAsync.*"));

            var (result, error) = await _service.GetTopicAsync("REDUCING_MEAT_CONSUMPTION");

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void InvalidateCache_ClearsLoadedMemoryAndIndexes()
        {
            _service.InvalidateCache();

            Assert.IsFalse(_service.IsLoaded);
            Assert.IsNull(_service.LoadedLanguage);
            Assert.AreEqual(0, _service.GetAllDimensions().Count);
            Assert.AreEqual(0, _service.GetAllTopics().Count);
        }

        [Test]
        public void GetTopicSprite_OnEmptyOrNull_ReturnsDefaultOrNull()
        {
            var sprite = _service.GetTopicSprite(null);
            Assert.IsNull(sprite);

            var spriteEmpty = _service.GetTopicSprite("");
            Assert.IsNull(spriteEmpty);
        }

        [Test]
        public void GetDimensionSprite_OnEmptyOrNull_ReturnsDefaultOrNull()
        {
            var sprite = _service.GetDimensionSprite(null);
            Assert.IsNull(sprite);

            var spriteEmpty = _service.GetDimensionSprite("");
            Assert.IsNull(spriteEmpty);
        }

        [Test]
        public void GetSpriteAddresses_ResolvesCorrectAddressablesKeys()
        {
            Assert.AreEqual("dimensions/default", _service.GetDefaultSpriteAddress());
            Assert.AreEqual("dimensions/default", _service.GetDimensionSpriteAddress(null));
            Assert.AreEqual("dimensions/default", _service.GetDimensionSpriteAddress(""));
            Assert.AreEqual("dimensions/diet_changes", _service.GetDimensionSpriteAddress("DIET_CHANGES"));
            Assert.AreEqual("dimensions/food_waste", _service.GetDimensionSpriteAddress("food_waste"));

            Assert.AreEqual("dimensions/default", _service.GetTopicSpriteAddress(null));
            Assert.AreEqual("dimensions/default", _service.GetTopicSpriteAddress(""));
            Assert.AreEqual("topics/reducing_meat_consumption", _service.GetTopicSpriteAddress("REDUCING_MEAT_CONSUMPTION"));
            Assert.AreEqual("topics/plate_waste", _service.GetTopicSpriteAddress("plate_waste"));
        }

        [Test]
        public void ClearSpriteCache_ClearsWithoutError()
        {
            Assert.DoesNotThrow(() => _service.ClearSpriteCache());
        }
    }
}
