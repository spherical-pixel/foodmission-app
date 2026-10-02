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
        public void BuildRecipeUrl_IncludesEscapedIdAndLang()
        {
            string url = RecipeService.BuildRecipeUrl("https://api.test", "abc 1", "es");
            Assert.AreEqual("https://api.test/api/v1/recipes/abc%201?lang=es", url);
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

        [Test]
        public void BuildRatingUrl_EscapesId()
        {
            Assert.AreEqual("https://api.test/api/v1/recipes/abc%201/rating", RecipeService.BuildRatingUrl("https://api.test", "abc 1"));
        }

        [Test]
        public void RecipeRating_Deserializes_WithAndWithoutMyRating()
        {
            var mine = Newtonsoft.Json.JsonConvert.DeserializeObject<RecipeRating>("{\"recipeId\":\"r1\",\"rating\":4.25,\"ratingCount\":8,\"myRating\":5}");
            var none = Newtonsoft.Json.JsonConvert.DeserializeObject<RecipeRating>("{\"recipeId\":\"r1\",\"rating\":0,\"ratingCount\":0,\"myRating\":null}");

            Assert.AreEqual(4.25f, mine.rating, 0.0001f);
            Assert.AreEqual(8, mine.ratingCount);
            Assert.AreEqual(5, mine.myRating);
            Assert.IsNull(none.myRating);
        }

        [Test]
        public async Task RateAsync_InvalidInput_ReturnsErrorWithoutRequest()
        {
            var (result, error) = await _service.RateAsync("", 3);
            Assert.IsNull(result);
            Assert.IsNotNull(error);

            (result, error) = await _service.RateAsync("r1", 6);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void BuildRecipesListUrl_AddsOriginOnlyWhenSet()
        {
            string withOrigin = RecipeService.BuildRecipesListUrl("https://api.test", 1, 20, "es", null, null, null, null, null, null, "USER");
            string without = RecipeService.BuildRecipesListUrl("https://api.test", 2, 20, "es", "pasta", null, null, null, null, null, null);

            Assert.AreEqual("https://api.test/api/v1/recipes?page=1&limit=20&lang=es&origin=USER", withOrigin);
            Assert.AreEqual("https://api.test/api/v1/recipes?page=2&limit=20&lang=es&search=pasta", without);
        }
    }
}
