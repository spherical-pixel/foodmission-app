using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodSearchFlowTests
    {
        private Mock<IGenericFoodService> _mockGenericService;
        private Mock<IFoodProductService> _mockFoodService;
        private Mock<IOpenFoodFactsClientService> _mockOffService;

        [SetUp]
        public void SetUp()
        {
            FoodProductFlow.UseDirectClientOverride = () => false;
            _mockGenericService = new Mock<IGenericFoodService>();
            _mockFoodService = new Mock<IFoodProductService>();
            _mockOffService = new Mock<IOpenFoodFactsClientService>();
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = () => ApiConfig.UseDirectOpenFoodFactsClient;
        }

        private static GenericFood[] Generics(params string[] names)
        {
            return names.Select(n => new GenericFood { id = n, foodName = n }).ToArray();
        }

        private static OpenFoodFactsProduct[] Products(params string[] names)
        {
            return names.Select(n => new OpenFoodFactsProduct { id = n, name = n }).ToArray();
        }

        private void SetupGeneric(int page, int pageSize, GenericFood[] items, int totalPages)
        {
            _mockGenericService
                .Setup(s => s.SearchGenericFoodsAsync("pan", null, page, pageSize))
                .ReturnsAsync((new PaginatedGenericFoodResponse { items = items, page = page, limit = pageSize, totalPages = totalPages }, null));
        }

        private void SetupProducts(int page, int pageSize, OpenFoodFactsProduct[] products, int totalPages)
        {
            _mockFoodService
                .Setup(s => s.SearchOpenFoodFactsAsync("pan", page, pageSize))
                .ReturnsAsync((new OpenFoodFactsSearchResponse { products = products, pageSize = pageSize, totalPages = totalPages }, null));
        }

        private Task<(FoodSearchPage Result, ApiErrorResponse Error)> Search(string query, int page, bool generic, bool products)
        {
            return FoodSearchFlow.SearchAsync(_mockGenericService.Object, _mockFoodService.Object, _mockOffService.Object,
                query, page, generic, products);
        }

        [Test]
        public async Task SearchAsync_EmptyQuery_ReturnsEmptyWithoutCallingServices()
        {
            var (result, error) = await Search("  ", 1, true, true);

            Assert.IsNull(error);
            Assert.AreEqual(0, result.Items.Count);
            Assert.AreEqual(0, result.TotalPages);
            _mockGenericService.VerifyNoOtherCalls();
            _mockFoodService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SearchAsync_GenericOnly_RequestsFullPageAndKeepsBackendOrder()
        {
            SetupGeneric(2, FoodSearchFlow.PageSize, Generics("Zanahoria", "Pan", "Arroz"), 4);

            var (result, error) = await Search("pan", 2, true, false);

            Assert.IsNull(error);
            CollectionAssert.AreEqual(new[] { "Zanahoria", "Pan", "Arroz" },
                result.Items.Cast<GenericFood>().Select(g => g.foodName).ToArray());
            Assert.AreEqual(4, result.TotalPages);
            _mockFoodService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SearchAsync_ProductsOnly_RequestsFullPage()
        {
            SetupProducts(1, FoodSearchFlow.PageSize, Products("P1", "P2"), 3);

            var (result, error) = await Search("pan", 1, false, true);

            Assert.IsNull(error);
            Assert.AreEqual(2, result.Items.Count);
            Assert.AreEqual(3, result.TotalPages);
            _mockGenericService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SearchAsync_Both_RequestsHalfPageEachAndInterleavesStartingWithGeneric()
        {
            int half = FoodSearchFlow.PageSize / 2;
            SetupGeneric(1, half, Generics("G1", "G2", "G3"), 2);
            SetupProducts(1, half, Products("P1", "P2", "P3"), 5);

            var (result, error) = await Search("pan", 1, true, true);

            Assert.IsNull(error);
            CollectionAssert.AreEqual(new[] { "G1", "P1", "G2", "P2", "G3", "P3" }, Names(result));
            Assert.AreEqual(5, result.TotalPages);
        }

        [Test]
        public async Task SearchAsync_Both_OneSourceShorter_AppendsTheRest()
        {
            int half = FoodSearchFlow.PageSize / 2;
            SetupGeneric(3, half, Generics("G1"), 3);
            SetupProducts(3, half, Products("P1", "P2", "P3"), 6);

            var (result, _) = await Search("pan", 3, true, true);

            CollectionAssert.AreEqual(new[] { "G1", "P1", "P2", "P3" }, Names(result));
        }

        [Test]
        public async Task SearchAsync_Both_OneSourceExhausted_ShowsOnlyTheOther()
        {
            int half = FoodSearchFlow.PageSize / 2;
            SetupGeneric(4, half, new GenericFood[0], 3);
            SetupProducts(4, half, Products("P1", "P2"), 6);

            var (result, _) = await Search("pan", 4, true, true);

            CollectionAssert.AreEqual(new[] { "P1", "P2" }, Names(result));
            Assert.AreEqual(6, result.TotalPages);
        }

        [Test]
        public async Task SearchAsync_OneSourceFails_ReturnsTheOtherAndTheError()
        {
            int half = FoodSearchFlow.PageSize / 2;
            SetupGeneric(1, half, Generics("G1", "G2"), 1);
            var offError = new ApiErrorResponse { statusCode = 503, message = "OFF down" };
            _mockFoodService
                .Setup(s => s.SearchOpenFoodFactsAsync("pan", 1, half))
                .ReturnsAsync(((OpenFoodFactsSearchResponse)null, offError));

            var (result, error) = await Search("pan", 1, true, true);

            Assert.AreSame(offError, error);
            CollectionAssert.AreEqual(new[] { "G1", "G2" }, Names(result));
            Assert.AreEqual(1, result.TotalPages);
        }

        [Test]
        public async Task SearchAsync_ProductsWithoutTotalPages_FallsBackToTotalCount()
        {
            _mockFoodService
                .Setup(s => s.SearchOpenFoodFactsAsync("pan", 1, FoodSearchFlow.PageSize))
                .ReturnsAsync((new OpenFoodFactsSearchResponse { products = Products("P1"), totalCount = 25 }, null));

            var (result, _) = await Search("pan", 1, false, true);

            Assert.AreEqual(3, result.TotalPages);
        }

        [Test]
        public async Task SearchAsync_TrimsQuery()
        {
            SetupGeneric(1, FoodSearchFlow.PageSize, Generics("Pan"), 1);

            var (result, _) = await Search("  pan ", 1, true, false);

            Assert.AreEqual(1, result.Items.Count);
        }

        private static string[] Names(FoodSearchPage page)
        {
            return page.Items.Select(i => i is GenericFood g ? g.foodName : ((OpenFoodFactsProduct)i).name).ToArray();
        }
    }
}
