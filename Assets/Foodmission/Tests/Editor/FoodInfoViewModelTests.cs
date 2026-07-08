using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using UnityEngine;
using UnityEngine.TestTools;
using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodInfoViewModelTests
    {
        private Mock<IGenericFoodService> _genericFoodServiceMock;
        private Mock<IFoodProductService> _foodProductServiceMock;
        private TestStoreService _storeService;

        [SetUp]
        public void SetUp()
        {
            _genericFoodServiceMock = new Mock<IGenericFoodService>();
            _foodProductServiceMock = new Mock<IFoodProductService>();
            _storeService = new TestStoreService();
        }

        private FoodInfoViewModel CreateViewModel()
        {
            return new FoodInfoViewModel(_storeService, _genericFoodServiceMock.Object, _foodProductServiceMock.Object);
        }

        [Test]
        public async Task LoadAsync_Product_FetchesDetailAndMapsFields()
        {
            var detail = new FoodProductDetail
            {
                id = Guid.NewGuid().ToString(),
                name = "Test Product",
                brands = "TestBrand",
                nutritionGrade = "a",
                novaGroup = 2,
                imageFrontUrl = "https://example.com/image.jpg",
                ingredientsText = "Water, sugar",
                allergens = new[] { "en:milk" }
            };

            _foodProductServiceMock
                .Setup(x => x.GetFoodProductDetailAsync(detail.id))
                .ReturnsAsync((detail, null));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Product, detail.id, "pantry");

            Assert.AreEqual("Test Product", vm.FoodName);
            Assert.AreEqual("TestBrand", vm.FoodSubtitle);
            Assert.AreEqual("a", vm.NutritionGrade);
            Assert.AreEqual(2, vm.NovaGroup);
            Assert.AreEqual("Water, sugar", vm.Ingredients);
            Assert.AreEqual("en:milk", vm.Allergens);
            Assert.IsTrue(vm.ShowActionButton);
            Assert.IsFalse(vm.IsLoading);
        }

        [Test]
        public async Task LoadAsync_Generic_FetchesDetailAndMapsFields()
        {
            var detail = new GenericFoodDetail
            {
                id = Guid.NewGuid().ToString(),
                foodName = "Test Food",
                foodGroup = "Fruits",
                energyKcal = 50f,
                proteins = 1f,
                fat = 0.5f,
                carbohydrates = 10f
            };

            _genericFoodServiceMock
                .Setup(x => x.GetGenericFoodDetailAsync(detail.id))
                .ReturnsAsync((detail, null));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Generic, detail.id, "shoppingList");

            Assert.AreEqual("Test Food", vm.FoodName);
            Assert.AreEqual("Fruits", vm.FoodSubtitle);
            Assert.AreEqual("", vm.NutritionGrade);
            Assert.AreEqual(0, vm.NovaGroup);
            Assert.IsTrue(vm.ShowActionButton);
            Assert.IsFalse(vm.IsLoading);
        }

        [Test]
        public async Task LoadAsync_ProductWithError_SetsErrorDetail()
        {
            string foodId = Guid.NewGuid().ToString();
            var error = new ApiErrorResponse { message = "Not found" };

            _foodProductServiceMock
                .Setup(x => x.GetFoodProductDetailAsync(foodId))
                .ReturnsAsync((null, error));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Product, foodId, "pantry");

            Assert.IsNotNull(vm.ErrorDetail);
            Assert.IsFalse(vm.IsLoading);
        }

        [Test]
        public async Task LoadAsync_GenericWithError_SetsErrorDetail()
        {
            string foodId = Guid.NewGuid().ToString();
            var error = new ApiErrorResponse { message = "Not found" };

            _genericFoodServiceMock
                .Setup(x => x.GetGenericFoodDetailAsync(foodId))
                .ReturnsAsync((null, error));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Generic, foodId, "pantry");

            Assert.IsNotNull(vm.ErrorDetail);
            Assert.IsFalse(vm.IsLoading);
        }

        [Test]
        public async Task LoadAsync_InvalidUuid_DoesNotFetch()
        {
            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Product, "not-a-uuid", "pantry");

            Assert.IsFalse(vm.IsLoading);
            _foodProductServiceMock.Verify(x => x.GetFoodProductDetailAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task LoadAsync_NoneContext_HidesActionButton()
        {
            string foodId = Guid.NewGuid().ToString();
            var detail = new FoodProductDetail { id = foodId, name = "Test" };

            _foodProductServiceMock
                .Setup(x => x.GetFoodProductDetailAsync(foodId))
                .ReturnsAsync((detail, null));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Product, foodId, "none");

            Assert.IsFalse(vm.ShowActionButton);
        }

        [Test]
        public async Task LoadAsync_PantryContext_SetsActionButtonText()
        {
            string foodId = Guid.NewGuid().ToString();
            var detail = new FoodProductDetail { id = foodId, name = "Test" };

            _foodProductServiceMock
                .Setup(x => x.GetFoodProductDetailAsync(foodId))
                .ReturnsAsync((detail, null));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Product, foodId, "pantry");

            Assert.IsTrue(vm.ShowActionButton);
        }

        [Test]
        public async Task BuildMacroCards_Generic_Has4Cards()
        {
            var detail = new GenericFoodDetail
            {
                id = Guid.NewGuid().ToString(),
                foodName = "Test",
                foodGroup = "Fruits",
                energyKcal = 50f,
                proteins = 1f,
                fat = 0.5f,
                carbohydrates = 10f
            };

            _genericFoodServiceMock
                .Setup(x => x.GetGenericFoodDetailAsync(detail.id))
                .ReturnsAsync((detail, null));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Generic, detail.id, "none");

            Assert.IsNotNull(vm.MacroCards);
            Assert.AreEqual(4, vm.MacroCards.Count);
        }

        [Test]
        public async Task BuildNutritionGroups_Generic_Has4Groups()
        {
            var detail = new GenericFoodDetail
            {
                id = Guid.NewGuid().ToString(),
                foodName = "Test",
                foodGroup = "Fruits",
                energyKcal = 50f,
                proteins = 1f,
                fat = 0.5f,
                carbohydrates = 10f,
                vitaminC = 10f,
                iron = 2f
            };

            _genericFoodServiceMock
                .Setup(x => x.GetGenericFoodDetailAsync(detail.id))
                .ReturnsAsync((detail, null));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Generic, detail.id, "none");

            Assert.IsNotNull(vm.NutritionDetail);
            Assert.AreEqual(4, vm.NutritionDetail.Count);
        }

        [Test]
        public async Task BuildTrafficLights_NullNutrientLevels_ReturnsEmpty()
        {
            var detail = new FoodProductDetail
            {
                id = Guid.NewGuid().ToString(),
                name = "Test",
                nutrientLevels = null
            };

            _foodProductServiceMock
                .Setup(x => x.GetFoodProductDetailAsync(detail.id))
                .ReturnsAsync((detail, null));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Product, detail.id, "none");

            Assert.IsNotNull(vm.TrafficLights);
            Assert.AreEqual(0, vm.TrafficLights.Count);
        }

        [Test]
        public async Task EcoScoreBadge_HiddenWhenNull()
        {
            var detail = new FoodProductDetail
            {
                id = Guid.NewGuid().ToString(),
                name = "Test",
                ecoscoreGrade = null
            };

            _foodProductServiceMock
                .Setup(x => x.GetFoodProductDetailAsync(detail.id))
                .ReturnsAsync((detail, null));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Product, detail.id, "none");

            Assert.AreEqual("", vm.EcoScoreGrade);
        }

        [Test]
        public async Task ActionClick_DispatchesAddRequestedAction()
        {
            string foodId = Guid.NewGuid().ToString();
            var detail = new FoodProductDetail { id = foodId, name = "Test" };

            _foodProductServiceMock
                .Setup(x => x.GetFoodProductDetailAsync(foodId))
                .ReturnsAsync((detail, null));

            var vm = CreateViewModel();
            await vm.LoadAsync(FoodInfoType.Product, foodId, "pantry");

            vm.OnActionButtonClicked();

            Assert.Contains("app/foodInfo/addRequested", _storeService.DispatchedActionTypes);
        }
    }
}
