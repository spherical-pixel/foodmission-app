using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodWasteAddViewModelTests
    {
        private Mock<IFoodWasteService> _mockFoodWasteService;
        private Mock<IPantryService> _mockPantryService;
        private TestStoreService _storeService;
        private FoodWasteAddViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockFoodWasteService = new Mock<IFoodWasteService>();
            _mockPantryService = new Mock<IPantryService>();
            _storeService = new TestStoreService();
            _vm = new FoodWasteAddViewModel(
                _storeService,
                _mockFoodWasteService.Object,
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
            Assert.IsNotNull(_vm.PantryItemOptions);
            Assert.AreEqual(0, _vm.PantryItemOptions.Count);
            Assert.AreEqual(-1, _vm.SelectedPantryIndex);
            Assert.AreEqual(WasteReason.Expired, _vm.WasteReason);
            Assert.AreEqual(DetectionMethod.Manual, _vm.DetectionMethod);
            Assert.AreEqual(0f, _vm.Quantity);
            Assert.AreEqual(0f, _vm.MaxQuantity);
            Assert.AreEqual("", _vm.CostEstimate);
            Assert.AreEqual("", _vm.Notes);
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsFalse(_vm.IsSaving);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.AreEqual("", _vm.SelectedFoodName);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public void OnPantryItemSelected_WithValidIndex_SetsQuantityAndMaxQuantity()
        {
            _vm.OnPantryItemSelected(0);

            Assert.AreEqual(0, _vm.SelectedPantryIndex);
        }

        [Test]
        public void OnPantryItemSelected_WithNegativeIndex_Resets()
        {
            _vm.OnPantryItemSelected(-1);

            Assert.AreEqual(-1, _vm.SelectedPantryIndex);
            Assert.AreEqual(0f, _vm.MaxQuantity);
            Assert.AreEqual(0f, _vm.Quantity);
            Assert.AreEqual("", _vm.SelectedFoodName);
        }

        [Test]
        public async Task SaveAsync_WithNoSelection_ReturnsFalse()
        {
            bool result = await _vm.SaveAsync();

            Assert.IsFalse(result);
        }

        [Test]
        public async Task SaveAsync_WithZeroQuantity_ReturnsFalse()
        {
            _mockPantryService
                .Setup(x => x.GetItemsAsync())
                .Returns(Task.FromResult<(PantryItem[] Result, ApiErrorResponse Error)>((new PantryItem[]
                {
                    new PantryItem { id = "pi1", foodProductId = "fp1", quantity = 2, unit = "kg" }
                }, null)));

            await _vm.LoadPantryItemsAsync();

            _vm.OnPantryItemSelected(0);
            _vm.Quantity = 0;

            bool result = await _vm.SaveAsync();

            Assert.IsFalse(result);
        }

        [Test]
        public async Task SaveAsync_OnSuccess_ReturnsTrue()
        {
            _mockPantryService
                .Setup(x => x.GetItemsAsync())
                .Returns(Task.FromResult<(PantryItem[] Result, ApiErrorResponse Error)>((new PantryItem[]
                {
                    new PantryItem { id = "pi1", foodProductId = "fp1", quantity = 2, unit = "kg" }
                }, null)));

            await _vm.LoadPantryItemsAsync();

            _vm.OnPantryItemSelected(0);

            _mockFoodWasteService
                .Setup(x => x.CreateAsync(It.IsAny<CreateFoodWasteRequest>()))
                .Returns(Task.FromResult<(FoodWaste Result, ApiErrorResponse Error)>((new FoodWaste { id = "fw1" }, null)));

            bool result = await _vm.SaveAsync();

            Assert.IsTrue(result);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task SaveAsync_WithApiError_ReturnsFalse()
        {
            _mockPantryService
                .Setup(x => x.GetItemsAsync())
                .Returns(Task.FromResult<(PantryItem[] Result, ApiErrorResponse Error)>((new PantryItem[]
                {
                    new PantryItem { id = "pi1", foodProductId = "fp1", quantity = 2, unit = "kg" }
                }, null)));

            await _vm.LoadPantryItemsAsync();

            _vm.OnPantryItemSelected(0);
            _vm.Quantity = 1;

            _mockFoodWasteService
                .Setup(x => x.CreateAsync(It.IsAny<CreateFoodWasteRequest>()))
                .Returns(Task.FromResult<(FoodWaste Result, ApiErrorResponse Error)>(((FoodWaste)null, new ApiErrorResponse { statusCode = 500, message = "Save failed" })));

            bool result = await _vm.SaveAsync();

            Assert.IsFalse(result);
            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual("Save failed", _vm.ErrorDetail.message);
        }

        [Test]
        public void Reset_ClearsAllState()
        {
            _vm.SelectedPantryIndex = 0;
            _vm.Quantity = 2;
            _vm.CostEstimate = "10";
            _vm.Notes = "test note";
            _vm.SelectedFoodName = "Apple";

            _vm.Reset();

            Assert.AreEqual(-1, _vm.SelectedPantryIndex);
            Assert.AreEqual(WasteReason.Expired, _vm.WasteReason);
            Assert.AreEqual(DetectionMethod.Manual, _vm.DetectionMethod);
            Assert.AreEqual(0f, _vm.Quantity);
            Assert.AreEqual(0f, _vm.MaxQuantity);
            Assert.AreEqual("", _vm.CostEstimate);
            Assert.AreEqual("", _vm.Notes);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.AreEqual("", _vm.SelectedFoodName);
        }

        [Test]
        public async Task LoadPantryItemsAsync_OnSuccess_UpdatesOptions()
        {
            _mockPantryService
                .Setup(x => x.GetItemsAsync())
                .Returns(Task.FromResult<(PantryItem[] Result, ApiErrorResponse Error)>((new PantryItem[]
                {
                    new PantryItem { id = "pi1", foodProductId = "Milk", quantity = 1, unit = "L" }
                }, null)));

            await _vm.LoadPantryItemsAsync();

            Assert.AreEqual(1, _vm.PantryItemOptions.Count);
            Assert.IsFalse(_vm.IsLoading);
        }
    }
}
