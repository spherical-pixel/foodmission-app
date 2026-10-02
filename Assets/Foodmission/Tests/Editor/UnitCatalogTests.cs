using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class UnitCatalogTests
    {
        private Mock<ICatalogService> _mockCatalogService;
        private UnitCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _mockCatalogService = new Mock<ICatalogService>();
            _catalog = new UnitCatalog(_mockCatalogService.Object);
        }

        private void SetupUnits(params CatalogItem[] items)
        {
            _mockCatalogService
                .Setup(x => x.GetUnitsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogItem[] Result, ApiErrorResponse Error)>((items, null)));
        }

        private static readonly CatalogItem[] SpanishUnits =
        {
            new CatalogItem { code = "PIECES", label = "Unidades" },
            new CatalogItem { code = "G", label = "Gramos" },
            new CatalogItem { code = "KG", label = "Kilogramos" },
            new CatalogItem { code = "ML", label = "Mililitros" },
            new CatalogItem { code = "L", label = "Litros" },
            new CatalogItem { code = "CUPS", label = "Tazas" }
        };

        [Test]
        public void BeforeLoad_ExposesBaseCodesAsCodesAndLabels()
        {
            CollectionAssert.AreEqual(UnitCodes.All, _catalog.Codes.ToArray());
            CollectionAssert.AreEqual(UnitCodes.All, _catalog.Labels.ToArray());
            Assert.AreEqual("G", _catalog.GetLabel("G"));
        }

        [Test]
        public async Task LoadAsync_Success_UsesCatalogLabels()
        {
            SetupUnits(SpanishUnits);

            await _catalog.LoadAsync("es");

            Assert.AreEqual("Gramos", _catalog.GetLabel("G"));
            Assert.AreEqual("Gramos", _catalog.GetLabel("g"));
            Assert.AreEqual(6, _catalog.Codes.Count);
        }

        [Test]
        public async Task LoadAsync_SameLanguageTwice_CallsApiOnce()
        {
            SetupUnits(SpanishUnits);

            await _catalog.LoadAsync("es");
            await _catalog.LoadAsync("es");

            _mockCatalogService.Verify(x => x.GetUnitsAsync("es"), Times.Once);
        }

        [Test]
        public async Task LoadAsync_ApiError_KeepsDefaults()
        {
            _mockCatalogService
                .Setup(x => x.GetUnitsAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogItem[] Result, ApiErrorResponse Error)>((null, new ApiErrorResponse { message = "down" })));

            await _catalog.LoadAsync("es");

            CollectionAssert.AreEqual(UnitCodes.All, _catalog.Codes.ToArray());
            Assert.AreEqual("KG", _catalog.GetLabel("KG"));
        }

        [Test]
        public async Task LoadAsync_Exception_KeepsDefaultsAndLogsError()
        {
            _mockCatalogService
                .Setup(x => x.GetUnitsAsync(It.IsAny<string>()))
                .Returns(Task.FromException<(CatalogItem[] Result, ApiErrorResponse Error)>(new System.Exception("boom")));
            LogAssert.Expect(LogType.Error, new Regex(@"\[UnitCatalog\]"));

            await _catalog.LoadAsync("es");

            CollectionAssert.AreEqual(UnitCodes.All, _catalog.Codes.ToArray());
        }

        [TestCase("g")]
        [TestCase("ml")]
        [TestCase("G")]
        public void GetLabel_BeforeLoad_ReturnsInputUnchanged(string code)
        {
            Assert.AreEqual(code, _catalog.GetLabel(code));
        }

        [Test]
        public void GetLabel_UnknownOrEmpty()
        {
            Assert.AreEqual("BOX", _catalog.GetLabel("BOX"));
            Assert.AreEqual("", _catalog.GetLabel(null));
            Assert.AreEqual("", _catalog.GetLabel(""));
        }

        [TestCase("G", "G")]
        [TestCase("kg", "KG")]
        [TestCase("grams", "G")]
        [TestCase("litre", "L")]
        [TestCase("pcs", "PIECES")]
        [TestCase("cup", "CUPS")]
        [TestCase("  ml ", "ML")]
        [TestCase("handful", null)]
        [TestCase("", null)]
        [TestCase(null, null)]
        public void ResolveCode_ByCodeOrAlias(string raw, string expected)
        {
            Assert.AreEqual(expected, _catalog.ResolveCode(raw));
        }

        [Test]
        public async Task ResolveCode_ByLocalizedLabel()
        {
            SetupUnits(SpanishUnits);
            await _catalog.LoadAsync("es");

            Assert.AreEqual("G", _catalog.ResolveCode("gramos"));
        }

        [TestCase("200 g", 200f, "G")]
        [TestCase("200g", 200f, "G")]
        [TestCase("1.5 kg", 1.5f, "KG")]
        [TestCase("1,5 kg", 1.5f, "KG")]
        [TestCase("2", 2f, "PIECES")]
        [TestCase("3 handfuls", 3f, "PIECES")]
        public void TryParseMeasure_Valid(string measure, float qty, string unit)
        {
            Assert.IsTrue(_catalog.TryParseMeasure(measure, out float q, out string u));
            Assert.AreEqual(qty, q, 0.0001f);
            Assert.AreEqual(unit, u);
        }

        [Test]
        public void TryParseMeasure_DecimalWithoutLeadingZero()
        {
            Assert.IsTrue(_catalog.TryParseMeasure(".5 cup", out float q, out string u));
            Assert.AreEqual(0.5f, q, 0.0001f);
            Assert.AreEqual("CUPS", u);
        }

        [TestCase("1/2 cup", 0.5f, "CUPS")]
        [TestCase("3/4kg", 0.75f, "KG")]
        [TestCase("1 1/2 cups", 1.5f, "CUPS")]
        [TestCase("2 1/4 l", 2.25f, "L")]
        [TestCase("½ cup", 0.5f, "CUPS")]
        [TestCase("1½ cups", 1.5f, "CUPS")]
        [TestCase("1 ½ cups", 1.5f, "CUPS")]
        [TestCase("¾ kg", 0.75f, "KG")]
        [TestCase("1/2 tsp", 0.5f, "PIECES")]
        [TestCase("½", 0.5f, "PIECES")]
        public void TryParseMeasure_Fractions(string measure, float qty, string unit)
        {
            Assert.IsTrue(_catalog.TryParseMeasure(measure, out float q, out string u));
            Assert.AreEqual(qty, q, 0.0001f);
            Assert.AreEqual(unit, u);
        }

        [Test]
        public void TryParseMeasure_ZeroDenominator_ReturnsFalse()
        {
            Assert.IsFalse(_catalog.TryParseMeasure("1/0 cup", out float q, out string u));
            Assert.AreEqual(1f, q);
            Assert.AreEqual(UnitCodes.Default, u);
        }

        [Test]
        public void ResolveMeasure_Fraction_UsesParsedQuantity()
        {
            var (q, u) = _catalog.ResolveMeasure("1/2 tsp", null, "G");
            Assert.AreEqual(0.5f, q, 0.0001f);
            Assert.AreEqual("G", u);
        }

        [Test]
        public void ResolveMeasure_StoredQuantityAndUnit_WinOverText()
        {
            var (q, u) = _catalog.ResolveMeasure("200 g", 3f, "KG");
            Assert.AreEqual(3f, q);
            Assert.AreEqual("KG", u);
        }

        [Test]
        public void ResolveMeasure_UnknownUnitText_KeepsStoredUnit()
        {
            var (q, u) = _catalog.ResolveMeasure("2 pinch", null, "G");
            Assert.AreEqual(2f, q);
            Assert.AreEqual("G", u);
        }

        [Test]
        public void ResolveMeasure_UnknownUnitText_NoStoredUnit_FallsBackToDefault()
        {
            var (q, u) = _catalog.ResolveMeasure("2 pinch", null, null);
            Assert.AreEqual(2f, q);
            Assert.AreEqual(UnitCodes.Default, u);
        }

        [Test]
        public void ResolveMeasure_RecognisedUnitText_UsesIt()
        {
            var (q, u) = _catalog.ResolveMeasure("1,5 kg", null, "G");
            Assert.AreEqual(1.5f, q, 0.0001f);
            Assert.AreEqual("KG", u);
        }

        [Test]
        public void ResolveMeasure_NoNumber_UsesStoredValuesOrDefaults()
        {
            var (q, u) = _catalog.ResolveMeasure("a pinch", 4f, null);
            Assert.AreEqual(4f, q);
            Assert.AreEqual(UnitCodes.Default, u);

            var (q2, u2) = _catalog.ResolveMeasure(null, null, "ML");
            Assert.AreEqual(1f, q2);
            Assert.AreEqual("ML", u2);
        }

        [TestCase("a pinch of salt")]
        [TestCase("")]
        [TestCase(null)]
        public void TryParseMeasure_NoLeadingNumber_ReturnsFalseWithDefaults(string measure)
        {
            Assert.IsFalse(_catalog.TryParseMeasure(measure, out float q, out string u));
            Assert.AreEqual(1f, q);
            Assert.AreEqual(UnitCodes.Default, u);
        }

        [Test]
        public void Current_WithoutApp_ReturnsUsableFallback()
        {
            Assert.IsNotNull(UnitCatalog.Current);
            Assert.AreEqual("G", UnitCatalog.Current.ResolveCode("g"));
        }
    }
}
