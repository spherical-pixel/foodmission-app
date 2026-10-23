using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodSearchFlowTests
    {
        private const int Chunk = FoodSearchFlow.ChunkSize;

        private Mock<IGenericFoodService> _mockGenericService;
        private Mock<IFoodProductService> _mockFoodService;
        private Mock<IOpenFoodFactsClientService> _mockOffService;
        private List<int> _genericPages;
        private List<int> _productPages;

        [SetUp]
        public void SetUp()
        {
            FoodProductFlow.UseDirectClientOverride = () => false;
            FoodSearchFlow.ClearKnownCounts();
            _mockGenericService = new Mock<IGenericFoodService>();
            _mockFoodService = new Mock<IFoodProductService>();
            _mockOffService = new Mock<IOpenFoodFactsClientService>();
            _genericPages = new List<int>();
            _productPages = new List<int>();
        }

        [TearDown]
        public void TearDown()
        {
            FoodProductFlow.UseDirectClientOverride = () => ApiConfig.UseDirectOpenFoodFactsClient;
            FoodSearchFlow.ClearKnownCounts();
        }

        private static GenericFood[] Generics(params string[] names)
        {
            return names.Select(n => new GenericFood { id = n, foodName = n }).ToArray();
        }

        private static OpenFoodFactsProduct[] Products(params string[] names)
        {
            return names.Select(n => new OpenFoodFactsProduct { id = n, name = n }).ToArray();
        }

        private static string[] Range(string prefix, int from, int to)
        {
            return Enumerable.Range(from, Math.Max(0, to - from)).Select(i => $"{prefix}{i}").ToArray();
        }

        /// <summary>Generic search over `total` foods named G0..G(total-1), any page size.</summary>
        private void SetupGenericTotal(int total)
        {
            _mockGenericService
                .Setup(s => s.SearchGenericFoodsAsync("pan", null, It.IsAny<int>(), It.IsAny<int>()))
                .Returns((string q, string group, int page, int size) =>
                {
                    _genericPages.Add(page);
                    var items = Generics(Range("G", (page - 1) * size, Math.Min(total, page * size)));
                    var response = new PaginatedGenericFoodResponse
                    {
                        items = items, total = total, page = page, limit = size, totalPages = (total + size - 1) / size
                    };
                    return Task.FromResult((response, (ApiErrorResponse)null));
                });
        }

        /// <summary>Backend OFF search over `total` products named P0..P(total-1), reporting hasMore and no real count.</summary>
        private void SetupProductsTotal(int total)
        {
            _mockFoodService
                .Setup(s => s.SearchOpenFoodFactsAsync("pan", It.IsAny<int>(), It.IsAny<int>()))
                .Returns((string q, int page, int size) =>
                {
                    _productPages.Add(page);
                    var products = Products(Range("P", (page - 1) * size, Math.Min(total, page * size)));
                    bool hasMore = total > page * size;
                    var response = new OpenFoodFactsSearchResponse
                    {
                        products = products,
                        pageSize = size,
                        hasMore = hasMore,
                        totalCount = (page - 1) * size + products.Length + (hasMore ? 1 : 0),
                        totalPages = page + (hasMore ? 1 : 0)
                    };
                    return Task.FromResult((response, (ApiErrorResponse)null));
                });
        }

        private void SetupProductsError(ApiErrorResponse error)
        {
            _mockFoodService
                .Setup(s => s.SearchOpenFoodFactsAsync("pan", It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(((OpenFoodFactsSearchResponse)null, error));
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
            Assert.IsFalse(result.HasNextPage);
            _mockGenericService.VerifyNoOtherCalls();
            _mockFoodService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SearchAsync_GenericOnly_RequestsFullPageAndKeepsBackendOrder()
        {
            _mockGenericService
                .Setup(s => s.SearchGenericFoodsAsync("pan", null, 2, FoodSearchFlow.PageSize))
                .ReturnsAsync((new PaginatedGenericFoodResponse { items = Generics("Zanahoria", "Pan", "Arroz"), page = 2, totalPages = 4 }, null));

            var (result, error) = await Search("pan", 2, true, false);

            Assert.IsNull(error);
            CollectionAssert.AreEqual(new[] { "Zanahoria", "Pan", "Arroz" }, Names(result));
            Assert.AreEqual(4, result.TotalPages);
            Assert.IsTrue(result.HasNextPage);
            _mockFoodService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SearchAsync_ProductsOnly_HasMore_TotalUnknownAndNextPage()
        {
            SetupProductsTotal(100);

            var (result, error) = await Search("pan", 2, false, true);

            Assert.IsNull(error);
            CollectionAssert.AreEqual(Range("P", 10, 20), Names(result));
            Assert.AreEqual(0, result.TotalPages);
            Assert.IsTrue(result.HasNextPage);
            CollectionAssert.AreEqual(new[] { 2 }, _productPages);
            _mockGenericService.VerifyNoOtherCalls();
        }

        [Test]
        public async Task SearchAsync_ProductsOnly_LastPage_KnowsTheTotal()
        {
            SetupProductsTotal(23);

            var (result, _) = await Search("pan", 3, false, true);

            CollectionAssert.AreEqual(Range("P", 20, 23), Names(result));
            Assert.AreEqual(3, result.TotalPages);
            Assert.IsFalse(result.HasNextPage);
        }

        [Test]
        public async Task SearchAsync_ProductsOnly_ExactCountWithoutHasMore_UsesTheCount()
        {
            _mockFoodService
                .Setup(s => s.SearchOpenFoodFactsAsync("pan", 1, FoodSearchFlow.PageSize))
                .ReturnsAsync((new OpenFoodFactsSearchResponse { products = Products("P1"), totalCount = 25 }, null));

            var (result, _) = await Search("pan", 1, false, true);

            Assert.AreEqual(3, result.TotalPages);
            Assert.IsTrue(result.HasNextPage);
        }

        [Test]
        public async Task SearchAsync_Both_RequestsOneChunkEachAndInterleavesStartingWithGeneric()
        {
            SetupGenericTotal(148);
            SetupProductsTotal(1000);

            var (result, error) = await Search("pan", 2, true, true);

            Assert.IsNull(error);
            CollectionAssert.AreEqual(new[] { "G5", "P5", "G6", "P6", "G7", "P7", "G8", "P8", "G9", "P9" }, Names(result));
            CollectionAssert.AreEqual(new[] { 2 }, _genericPages);
            CollectionAssert.AreEqual(new[] { 2 }, _productPages);
            Assert.AreEqual(0, result.TotalPages, "OFF doesn't count its matches");
            Assert.IsTrue(result.HasNextPage);
        }

        [Test]
        public async Task SearchAsync_Both_PageWhereGenericsRunOut_FillsWithProducts()
        {
            // 29 generic foods: 6 chunks, the last one with 4
            SetupGenericTotal(29);
            SetupProductsTotal(1000);

            var (result, _) = await Search("pan", 6, true, true);

            CollectionAssert.AreEqual(new[] { "G25", "P25", "G26", "P26", "G27", "P27", "G28", "P28", "P29" }, Names(result));
        }

        [Test]
        public async Task SearchAsync_Both_AfterGenericsRunOut_ProductsFillTheWholePage()
        {
            SetupGenericTotal(29);
            SetupProductsTotal(1000);

            var (result, _) = await Search("pan", 7, true, true);

            // Pages 1-6 showed P0..P29, page 7 continues without gaps or repeats
            CollectionAssert.AreEqual(Range("P", 30, 40), Names(result));
            Assert.IsTrue(result.HasNextPage);
        }

        [Test]
        public async Task SearchAsync_Both_KnownGenericEnd_SkipsGenericRequests()
        {
            SetupGenericTotal(29);
            SetupProductsTotal(1000);
            await Search("pan", 6, true, true);
            _genericPages.Clear();
            _productPages.Clear();

            var (result, _) = await Search("pan", 8, true, true);

            CollectionAssert.AreEqual(Range("P", 40, 50), Names(result));
            CollectionAssert.IsEmpty(_genericPages);
            CollectionAssert.AreEquivalent(new[] { 9, 10 }, _productPages);
        }

        [Test]
        public async Task SearchAsync_Both_ProductsRunOut_GenericsFillTheWholePage()
        {
            SetupGenericTotal(30);
            SetupProductsTotal(3);

            var (result, _) = await Search("pan", 2, true, true);

            CollectionAssert.AreEqual(Range("G", 5, 15), Names(result));
            Assert.AreEqual(4, result.TotalPages, "6 generic chunks + 1 product chunk");
            Assert.IsTrue(result.HasNextPage);
        }

        [Test]
        public async Task SearchAsync_Both_ProductsEndedSeveralChunksBack_FindsTheEnd()
        {
            SetupGenericTotal(50);
            SetupProductsTotal(7);

            var (result, _) = await Search("pan", 4, true, true);

            // Products took chunks 0-1 on pages 1-2; generics: chunks 0-1 there, then 2 per page
            CollectionAssert.AreEqual(Range("G", 20, 30), Names(result));
            Assert.AreEqual(6, result.TotalPages, "10 generic chunks + 2 product chunks");
        }

        [Test]
        public async Task SearchAsync_Both_LastPage_HasNoNextPage()
        {
            SetupGenericTotal(5);
            SetupProductsTotal(3);

            var (result, _) = await Search("pan", 1, true, true);

            CollectionAssert.AreEqual(new[] { "G0", "P0", "G1", "P1", "G2", "P2", "G3", "G4" }, Names(result));
            Assert.AreEqual(1, result.TotalPages);
            Assert.IsFalse(result.HasNextPage);
        }

        [Test]
        public async Task SearchAsync_Both_NoMatches_ReturnsEmpty()
        {
            SetupGenericTotal(0);
            SetupProductsTotal(0);

            var (result, error) = await Search("pan", 1, true, true);

            Assert.IsNull(error);
            Assert.AreEqual(0, result.Items.Count);
            Assert.IsFalse(result.HasNextPage);
        }

        [Test]
        public async Task SearchAsync_Both_OneSourceFails_ShowsTheOtherWithoutError()
        {
            SetupGenericTotal(2);
            SetupProductsError(new ApiErrorResponse { statusCode = 503, message = "OFF down" });

            var (result, error) = await Search("pan", 1, true, true);

            Assert.IsNull(error);
            CollectionAssert.AreEqual(new[] { "G0", "G1" }, Names(result));
        }

        [Test]
        public async Task SearchAsync_Both_FailureAndNothingToShow_ReturnsTheError()
        {
            SetupGenericTotal(0);
            var offError = new ApiErrorResponse { statusCode = 503, message = "OFF down" };
            SetupProductsError(offError);

            var (result, error) = await Search("pan", 1, true, true);

            Assert.AreSame(offError, error);
            Assert.AreEqual(0, result.Items.Count);
        }

        [Test]
        public async Task SearchAsync_TrimsQuery()
        {
            _mockGenericService
                .Setup(s => s.SearchGenericFoodsAsync("pan", null, 1, FoodSearchFlow.PageSize))
                .ReturnsAsync((new PaginatedGenericFoodResponse { items = Generics("Pan"), totalPages = 1 }, null));

            var (result, _) = await Search("  pan ", 1, true, false);

            Assert.AreEqual(1, result.Items.Count);
        }

        [Test]
        public void Plan_BothLast_OneChunkEach()
        {
            Assert.AreEqual(new FoodSearchFlow.ChunkPlan(3, 4, 3, 4), FoodSearchFlow.Plan(3, 10, null));
        }

        [Test]
        public void Plan_GenericsRanOut_TwoProductChunks()
        {
            Assert.AreEqual(new FoodSearchFlow.ChunkPlan(6, 6, 6, 8), FoodSearchFlow.Plan(6, 6, null));
        }

        [Test]
        public void Plan_BothRanOut_IsEmpty()
        {
            Assert.IsTrue(FoodSearchFlow.Plan(4, 3, 4).IsEmpty);
            Assert.IsFalse(FoodSearchFlow.Plan(3, 3, 4).IsEmpty);
        }

        private static string[] Names(FoodSearchPage page)
        {
            return page.Items.Select(i => i is GenericFood g ? g.foodName : ((OpenFoodFactsProduct)i).name).ToArray();
        }
    }
}
