using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodFactScreenViewModelTests
    {
        private Mock<IFoodFactService> _mockFoodFactService;
        private TestStoreService _storeService;
        private FoodFactScreenViewModel _vm;
        private Func<bool> _originalOverride;

        [SetUp]
        public void SetUp()
        {
            _originalOverride = FoodProductFlow.UseDirectClientOverride;
            FoodProductFlow.UseDirectClientOverride = () => false;

            _mockFoodFactService = new Mock<IFoodFactService>();
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState
            {
                accessToken = "test-token",
                tokenType = "Bearer",
                lang = "es"
            });

            _vm = new FoodFactScreenViewModel(
                _storeService,
                _mockFoodFactService.Object
            );
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = _originalOverride;
            _vm?.Dispose();
        }

        [Test]
        public void InitialState_ShouldHaveDefaultValues()
        {
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.FoodFactData);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task LoadFoodFactDataByCodeOrId_WhenSuccessful_SetsFoodFactData()
        {
            var expectedFact = new FoodFact
            {
                id = "fact-1",
                code = "FF1.1.1",
                topicId = "topic-1",
                body = "Test body",
                source = "Test source",
                level = FoodFactLevel.Beginner
            };

            _mockFoodFactService.Setup(s => s.GetFoodFactAsync("FF1.1.1", null))
                .ReturnsAsync((expectedFact, null));

            await _vm.LoadFoodFactDataByCodeOrId("FF1.1.1");

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorDetail);
            Assert.IsNotNull(_vm.FoodFactData);
            Assert.AreEqual("FF1.1.1", _vm.FoodFactData.code);
            Assert.AreEqual("Test body", _vm.FoodFactData.body);
        }

        [Test]
        public async Task LoadFoodFactDataByCodeOrId_WhenError_SetsErrorDetail()
        {
            var error = new ApiErrorResponse
            {
                message = "Not found",
                statusCode = 404
            };

            _mockFoodFactService.Setup(s => s.GetFoodFactAsync("INVALID", null))
                .ReturnsAsync((null, error));

            await _vm.LoadFoodFactDataByCodeOrId("INVALID");

            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual(404, _vm.ErrorDetail.statusCode);
            Assert.IsNull(_vm.FoodFactData);
        }

        [Test]
        public async Task LoadFoodFactDataByCodeOrId_WhenEmpty_DoesNothing()
        {
            await _vm.LoadFoodFactDataByCodeOrId("");
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.FoodFactData);

            await _vm.LoadFoodFactDataByCodeOrId(null);
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.FoodFactData);
        }

        [Test]
        public async Task MarkAsReadAsync_WhenFoodFactDataNull_ReturnsNull()
        {
            var result = await _vm.MarkAsReadAsync();
            Assert.IsNull(result);
            Assert.IsNull(_vm.EarnedReward);
        }

        [Test]
        public async Task MarkAsReadAsync_WhenServiceReturnsReward_DispatchesWalletRewardAndSetsEarnedReward()
        {
            var fact = new FoodFact { id = "fact-1", code = "FF.B1.1" };
            _vm.FoodFactData = fact;

            var expectedReward = new ContentReward { xp = 20, points = 5 };
            var progressResponse = new FoodFactProgressResponse
            {
                foodFactCode = "FF.B1.1",
                reward = expectedReward
            };

            _mockFoodFactService.Setup(s => s.MarkAsReadAsync("FF.B1.1"))
                .ReturnsAsync((progressResponse, null));

            var result = await _vm.MarkAsReadAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(20, result.xp);
            Assert.AreEqual(5, result.points);
            Assert.AreSame(expectedReward, _vm.EarnedReward);
            Assert.Contains("app/addWalletReward", _storeService.DispatchedActionTypes);
            Assert.AreEqual(20, _storeService.GetAppState().userXp);
            Assert.AreEqual(5, _storeService.GetAppState().userPoints);
        }

        [Test]
        public async Task MarkAsReadAsync_WhenServiceReturnsNoReward_DoesNotDispatch()
        {
            var fact = new FoodFact { id = "fact-1", code = "FF.B1.1" };
            _vm.FoodFactData = fact;

            var progressResponse = new FoodFactProgressResponse
            {
                foodFactCode = "FF.B1.1",
                reward = null
            };

            _mockFoodFactService.Setup(s => s.MarkAsReadAsync("FF.B1.1"))
                .ReturnsAsync((progressResponse, null));

            var result = await _vm.MarkAsReadAsync();

            Assert.IsNull(result);
            Assert.IsNull(_vm.EarnedReward);
            Assert.IsFalse(_storeService.DispatchedActionTypes.Contains("app/addWalletReward"));
        }
    }
}
