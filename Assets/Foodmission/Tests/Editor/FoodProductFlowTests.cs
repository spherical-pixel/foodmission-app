using NUnit.Framework;
using Moq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodProductFlowTests
    {
        private Mock<IFoodProductService> _mockFoodService;
        private Mock<IOpenFoodFactsClientService> _mockOffService;

        [SetUp]
        public void SetUp()
        {
            _mockFoodService = new Mock<IFoodProductService>();
            _mockOffService = new Mock<IOpenFoodFactsClientService>();
        }

        [Test]
        public async Task SearchProducts_WhenUseDirectClientFalse_DelegatesToBackend()
        {
            FoodProductFlow.UseDirectClientOverride = () => false;

            var mockResponse = new OpenFoodFactsSearchResponse
            {
                products = new[] { new OpenFoodFactsProduct { id = "1", name = "Backend Product" } }
            };

            _mockFoodService
                .Setup(f => f.SearchOpenFoodFactsAsync("apple", 1, 20))
                .ReturnsAsync((mockResponse, null));

            var (result, error) = await FoodProductFlow.SearchProductsAsync(_mockFoodService.Object, _mockOffService.Object, "apple");

            Assert.IsNull(error);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Backend Product", result[0].name);

            _mockFoodService.Verify(f => f.SearchOpenFoodFactsAsync("apple", 1, 20), Times.Once);
            _mockOffService.Verify(o => o.SearchAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task SearchProducts_WhenUseDirectClientTrue_AndQueryShort_ReturnsEmpty()
        {
            FoodProductFlow.UseDirectClientOverride = () => true;

            var (result, error) = await FoodProductFlow.SearchProductsAsync(_mockFoodService.Object, _mockOffService.Object, "ap");

            Assert.IsNull(error);
            Assert.AreEqual(0, result.Count);

            _mockFoodService.Verify(f => f.SearchOpenFoodFactsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _mockOffService.Verify(o => o.SearchAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task SearchProducts_WhenUseDirectClientTrue_AndQueryValid_QueriesOFFDirectly()
        {
            FoodProductFlow.UseDirectClientOverride = () => true;

            var mockResponse = new OpenFoodFactsSearchResponse
            {
                products = new[] { new OpenFoodFactsProduct { id = "2", name = "OFF Direct Product" } }
            };

            _mockOffService
                .Setup(o => o.SearchAsync("apple", 1))
                .ReturnsAsync((mockResponse, null));

            var (result, error) = await FoodProductFlow.SearchProductsAsync(_mockFoodService.Object, _mockOffService.Object, "apple");

            Assert.IsNull(error);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("OFF Direct Product", result[0].name);

            _mockFoodService.Verify(f => f.SearchOpenFoodFactsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            _mockOffService.Verify(o => o.SearchAsync("apple", 1), Times.Once);
        }

        [Test]
        public async Task ImportByBarcode_WhenUseDirectClientTrue_AndDbHit_ReturnsWithoutCallingOFF()
        {
            FoodProductFlow.UseDirectClientOverride = () => true;

            var dbProduct = new FoodProduct { id = "uuid-1", name = "DB Product", barcode = "12345" };

            _mockFoodService
                .Setup(f => f.FindByBarcodeAsync("12345", false))
                .ReturnsAsync((dbProduct, null));

            var (result, error) = await FoodProductFlow.ImportByBarcodeAsync(_mockFoodService.Object, _mockOffService.Object, "12345");

            Assert.IsNull(error);
            Assert.AreEqual(dbProduct, result);

            _mockFoodService.Verify(f => f.FindByBarcodeAsync("12345", false), Times.Once);
            _mockOffService.Verify(o => o.GetByBarcodeAsync(It.IsAny<string>()), Times.Never);
            _mockFoodService.Verify(f => f.CreateAsync(It.IsAny<CreateFoodProductRequest>()), Times.Never);
        }

        [Test]
        public async Task ImportByBarcode_WhenUseDirectClientTrue_AndDbMiss_QueriesOFFAndCreates()
        {
            FoodProductFlow.UseDirectClientOverride = () => true;

            var offProduct = new OpenFoodFactsProduct
            {
                id = "12345",
                barcode = "12345",
                name = "OFF Product",
                brands = new[] { "BrandA" }
            };

            var createdProduct = new FoodProduct { id = "uuid-created", name = "OFF Product", barcode = "12345" };

            _mockFoodService
                .Setup(f => f.FindByBarcodeAsync("12345", false))
                .ReturnsAsync(((FoodProduct)null, null)); // DB Miss

            _mockOffService
                .Setup(o => o.GetByBarcodeAsync("12345"))
                .ReturnsAsync((offProduct, null));

            _mockFoodService
                .Setup(f => f.CreateAsync(It.IsAny<CreateFoodProductRequest>()))
                .ReturnsAsync((createdProduct, null));

            var (result, error) = await FoodProductFlow.ImportByBarcodeAsync(_mockFoodService.Object, _mockOffService.Object, "12345");

            Assert.IsNull(error);
            Assert.AreEqual(createdProduct, result);

            _mockFoodService.Verify(f => f.FindByBarcodeAsync("12345", false), Times.Once);
            _mockOffService.Verify(o => o.GetByBarcodeAsync("12345"), Times.Once);
            _mockFoodService.Verify(f => f.CreateAsync(It.Is<CreateFoodProductRequest>(r => r.Name == "OFF Product" && r.Brands == "BrandA")), Times.Once);
        }

        [Test]
        public async Task ImportByBarcode_WhenUseDirectClientFalse_AndAlreadyInDb_ReturnsExistingWithoutImporting()
        {
            FoodProductFlow.UseDirectClientOverride = () => false;

            var existingProduct = new FoodProduct { id = "550e8400-e29b-41d4-a716-446655440000", name = "Existing DB Product", barcode = "12345" };

            _mockFoodService
                .Setup(f => f.SearchFoodsByBarcodeAsync("12345"))
                .ReturnsAsync((new PaginatedFoodProductResponse { data = new[] { existingProduct } }, null));

            var (result, error) = await FoodProductFlow.ImportByBarcodeAsync(_mockFoodService.Object, _mockOffService.Object, "12345");

            Assert.IsNull(error);
            Assert.AreEqual(existingProduct, result);

            _mockFoodService.Verify(f => f.SearchFoodsByBarcodeAsync("12345"), Times.Once);
            _mockFoodService.Verify(f => f.ImportFromBarcodeAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ImportByBarcode_WhenUseDirectClientFalse_AndNotInDb_ImportsFromBackendProxy()
        {
            FoodProductFlow.UseDirectClientOverride = () => false;

            var importedProduct = new FoodProduct { id = "550e8400-e29b-41d4-a716-446655440000", name = "Proxy Imported Product", barcode = "12345" };

            _mockFoodService
                .Setup(f => f.SearchFoodsByBarcodeAsync("12345"))
                .ReturnsAsync(((PaginatedFoodProductResponse)null, null));

            _mockFoodService
                .Setup(f => f.ImportFromBarcodeAsync("12345"))
                .ReturnsAsync((importedProduct, null));

            var (result, error) = await FoodProductFlow.ImportByBarcodeAsync(_mockFoodService.Object, _mockOffService.Object, "12345");

            Assert.IsNull(error);
            Assert.AreEqual(importedProduct, result);

            _mockFoodService.Verify(f => f.ImportFromBarcodeAsync("12345"), Times.Once);
        }
    }
}
