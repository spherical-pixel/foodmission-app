using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using eu.foodmission.platform;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class RecipeServiceTests
    {
        private TestStoreService _storeService;
        private RecipeService _service;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-token",
                tokenType = "Bearer",
            });
            _service = new RecipeService(_storeService);
            FoodProductFlow.UseDirectClientOverride = () => false;
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = null;
        }

        [Test]
        public async Task CreateRecipeAsync_OnNullRequest_ReturnsTitleRequiredError()
        {
            var (result, error) = await _service.CreateRecipeAsync(null);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.IsTrue(error.message.Contains("Title is required"));
        }

        [Test]
        public async Task UpdateRecipeAsync_OnEmptyId_ReturnsIdRequiredError()
        {
            var (result, error) = await _service.UpdateRecipeAsync("", new CreateRecipeRequest { title = "X" });
            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.IsTrue(error.message.Contains("Recipe id is required"));
        }

        [Test]
        public async Task DeleteRecipeAsync_OnEmptyId_ReturnsIdRequiredError()
        {
            var (success, error) = await _service.DeleteRecipeAsync("");
            Assert.IsFalse(success);
            Assert.IsNotNull(error);
            Assert.IsTrue(error.message.Contains("Recipe id is required"));
        }

        [Test]
        public async Task GetRecommendationsAsync_OnNetworkFailure_ReturnsErrorAndNullResult()
        {
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex(".*GetRecommendationsAsync.*"));
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex(".*GetRecommendationsAsync.*"));
            // No real backend in test env — expect a network error.
            var (result, error) = await _service.GetRecommendationsAsync();
            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }
    }
}
