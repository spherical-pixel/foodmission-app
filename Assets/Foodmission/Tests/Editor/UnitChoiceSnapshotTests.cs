using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class UnitChoiceSnapshotTests
    {
        [Test]
        public void IndexOf_IsCaseInsensitive_AndMinusOneWhenMissing()
        {
            var snapshot = UnitChoiceSnapshot.From(new UnitCatalog(null));

            Assert.AreEqual(1, snapshot.IndexOf("g"));
            Assert.AreEqual(-1, snapshot.IndexOf("BOX"));
            Assert.AreEqual(-1, snapshot.IndexOf(null));
        }

        [Test]
        public void CodeAt_OutOfRange_ReturnsDefault()
        {
            var snapshot = UnitChoiceSnapshot.From(new UnitCatalog(null));

            Assert.AreEqual("KG", snapshot.CodeAt(2));
            Assert.AreEqual(UnitCodes.Default, snapshot.CodeAt(-1));
            Assert.AreEqual(UnitCodes.Default, snapshot.CodeAt(99));
        }

        [Test]
        public async Task Snapshot_IsNotAffectedByLaterCatalogReload()
        {
            var catalogService = new Mock<ICatalogService>();
            catalogService
                .Setup(x => x.GetUnitsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogItem[] Result, ApiErrorResponse Error)>((new[]
                {
                    new CatalogItem { code = "CUPS", label = "Tazas" },
                    new CatalogItem { code = "G", label = "Gramos" }
                }, null)));
            var catalog = new UnitCatalog(catalogService.Object);
            var snapshot = UnitChoiceSnapshot.From(catalog);

            await catalog.LoadAsync("es");

            // Index 0 was PIECES when the dropdown was built; it must stay PIECES.
            Assert.AreEqual(UnitCodes.Pieces, snapshot.CodeAt(0));
            Assert.AreEqual(UnitCodes.Pieces, snapshot.Labels[0]);
        }
    }
}
