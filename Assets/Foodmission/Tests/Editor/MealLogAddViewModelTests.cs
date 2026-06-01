using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class MealLogAddViewModelTests
    {
        private Mock<IMealLogService> _mockMealLogService;
        private Mock<IMealService> _mockMealService;
        private Mock<IPantryService> _mockPantryService;
        private TestStoreService _storeService;
        private MealLogAddViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockMealLogService = new Mock<IMealLogService>();
            _mockMealService = new Mock<IMealService>();
            _mockPantryService = new Mock<IPantryService>();
            _storeService = new TestStoreService();
            _vm = new MealLogAddViewModel(
                _storeService,
                _mockMealLogService.Object,
                _mockMealService.Object,
                _mockPantryService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesWithDefaults()
        {
            Assert.IsNotNull(_vm.MealSearchResults);
            Assert.AreEqual(0, _vm.MealSearchResults.Count);
            Assert.IsFalse(_vm.IsSearchingMeals);
            Assert.AreEqual("", _vm.MealSearchQuery);
            Assert.AreEqual(0, _vm.SelectedTypeOfMealIndex);
            Assert.IsFalse(_vm.MealFromPantry);
            Assert.IsFalse(_vm.EatenOut);
            Assert.AreEqual("", _vm.SelectedMealName);
            Assert.IsFalse(_vm.HasSelectedMeal);
            Assert.IsFalse(_vm.IsSaving);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public void SelectedTypeOfMeal_WithDefaultIndex_ReturnsBreakfast()
        {
            Assert.AreEqual("BREAKFAST", _vm.SelectedTypeOfMeal);
        }

        [Test]
        public void SelectedTypeOfMeal_WithDinnerIndex_ReturnsDinner()
        {
            _vm.SelectedTypeOfMealIndex = 2;

            Assert.AreEqual("DINNER", _vm.SelectedTypeOfMeal);
        }

        [Test]
        public void SelectMeal_WithMeal_SetsProperties()
        {
            Meal meal = new Meal { id = "meal1", name = "Pasta" };
            _vm.SelectMeal(meal);

            Assert.IsTrue(_vm.HasSelectedMeal);
            Assert.AreEqual("Pasta", _vm.SelectedMealName);
        }

        [Test]
        public void SelectMeal_WithNull_ClearsSelection()
        {
            _vm.SelectMeal(new Meal { id = "m1", name = "Test" });
            _vm.SelectMeal(null);

            Assert.IsFalse(_vm.HasSelectedMeal);
            Assert.AreEqual("", _vm.SelectedMealName);
        }

        [Test]
        public async Task SearchMealsAsync_WithEmptyQuery_ReturnsWithoutCallingService()
        {
            await _vm.SearchMealsAsync("");

            Assert.AreEqual(0, _vm.MealSearchResults.Count);
            _mockMealService.Verify(
                x => x.GetMealsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test]
        public async Task SearchMealsAsync_OnSuccess_FillsResults()
        {
            _mockMealService
                .Setup(x => x.GetMealsAsync("pasta", null, null, null, null, 1, 20))
                .Returns(Task.FromResult<(PaginatedMealResponse Result, ApiErrorResponse Error)>((new PaginatedMealResponse
                {
                    data = new[] { new Meal { id = "m1", name = "Pasta" } }
                }, null)));

            await _vm.SearchMealsAsync("pasta");

            Assert.AreEqual(1, _vm.MealSearchResults.Count);
            Assert.AreEqual("Pasta", _vm.MealSearchResults[0].name);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task CreateAndSelectMealAsync_WithEmptyName_ReturnsFalse()
        {
            bool result = await _vm.CreateAndSelectMealAsync("");

            Assert.IsFalse(result);
        }

        [Test]
        public async Task CreateAndSelectMealAsync_OnSuccess_SelectsMeal()
        {
            Meal created = new Meal { id = "new1", name = "Custom Meal" };
            _mockMealService
                .Setup(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()))
                .Returns(Task.FromResult<(Meal Result, ApiErrorResponse Error)>((created, null)));

            bool result = await _vm.CreateAndSelectMealAsync("Custom Meal");

            Assert.IsTrue(result);
            Assert.IsTrue(_vm.HasSelectedMeal);
            Assert.AreEqual("Custom Meal", _vm.SelectedMealName);
        }

        [Test]
        public async Task CreateAndSelectMealAsync_WithApiError_ReturnsFalse()
        {
            _mockMealService
                .Setup(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()))
                .Returns(Task.FromResult<(Meal Result, ApiErrorResponse Error)>(((Meal)null, new ApiErrorResponse { statusCode = 500, message = "Create failed" })));

            bool result = await _vm.CreateAndSelectMealAsync("Custom Meal");

            Assert.IsFalse(result);
            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task SaveAsync_WithNoMealSelectedAndEmptyQuery_ReturnsFalse()
        {
            bool result = await _vm.SaveAsync();

            Assert.IsFalse(result);
            _mockMealService.Verify(x => x.CreateMealAsync(It.IsAny<CreateMealRequest>()), Times.Never);
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            _vm.SelectMeal(new Meal { id = "m1", name = "Test" });
            _vm.SelectedTypeOfMealIndex = 2;
            _vm.MealFromPantry = true;
            _vm.EatenOut = true;

            _vm.Reset();

            Assert.IsFalse(_vm.HasSelectedMeal);
            Assert.AreEqual("", _vm.MealSearchQuery);
            Assert.AreEqual(0, _vm.SelectedTypeOfMealIndex);
            Assert.IsFalse(_vm.MealFromPantry);
            Assert.IsFalse(_vm.EatenOut);
            Assert.AreEqual("", _vm.ErrorMessage);
        }
    }
}
