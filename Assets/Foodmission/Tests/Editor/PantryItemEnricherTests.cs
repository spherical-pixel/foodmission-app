using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class PantryItemEnricherTests
    {
        private Mock<IFoodProductService> _mockFoodProductService;
        private Mock<IGenericFoodService> _mockGenericFoodService;
        private PantryItemEnricher _enricher;

        [SetUp]
        public void SetUp()
        {
            _mockFoodProductService = new Mock<IFoodProductService>();
            _mockGenericFoodService = new Mock<IGenericFoodService>();
            _enricher = new PantryItemEnricher(_mockFoodProductService.Object, _mockGenericFoodService.Object);
        }

        [Test]
        public async Task EnrichAsync_ProductItem_UsesProductName()
        {
            _mockFoodProductService
                .Setup(x => x.GetFoodByIdAsync("p1"))
                .Returns(Task.FromResult<(FoodProduct Result, ApiErrorResponse Error)>((new FoodProduct { name = "Milk" }, null)));

            PantryItemView view = await _enricher.EnrichAsync(new PantryItem { id = "i1", foodProductId = "p1" });

            Assert.AreEqual("Milk", view.DisplayName);
            Assert.AreEqual("i1", view.Item.id);
        }

        [Test]
        public async Task EnrichAsync_GenericItem_UsesGenericFoodName()
        {
            _mockGenericFoodService
                .Setup(x => x.GetGenericFoodByIdAsync("g1"))
                .Returns(Task.FromResult<(GenericFood Result, ApiErrorResponse Error)>((new GenericFood { foodName = "Rice" }, null)));

            PantryItemView view = await _enricher.EnrichAsync(new PantryItem { id = "i2", genericFoodId = "g1" });

            Assert.AreEqual("Rice", view.DisplayName);
        }

        [Test]
        public async Task EnrichAsync_NoFoodReference_DoesNotCallServices()
        {
            PantryItemView view = await _enricher.EnrichAsync(new PantryItem { id = "i3" });

            Assert.IsNotNull(view);
            Assert.AreEqual("i3", view.Item.id);
            _mockFoodProductService.Verify(x => x.GetFoodByIdAsync(It.IsAny<string>()), Times.Never);
            _mockGenericFoodService.Verify(x => x.GetGenericFoodByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task EnrichAsync_Array_PreservesOrder()
        {
            PantryItemView[] views = await _enricher.EnrichAsync(new[] { new PantryItem { id = "a" }, new PantryItem { id = "b" } });

            Assert.AreEqual(2, views.Length);
            Assert.AreEqual("a", views[0].Item.id);
            Assert.AreEqual("b", views[1].Item.id);
        }

        [Test]
        public async Task EnrichAsync_NullArray_ReturnsEmpty()
        {
            PantryItemView[] views = await _enricher.EnrichAsync((PantryItem[])null);

            Assert.AreEqual(0, views.Length);
        }
    }
}
