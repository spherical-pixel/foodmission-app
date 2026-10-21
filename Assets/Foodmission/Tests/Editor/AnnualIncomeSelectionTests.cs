using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class AnnualIncomeSelectionTests
    {
        private static readonly CatalogItem[] Catalog =
        {
            new CatalogItem { code = "BELOW_10000", label = "< 10k" },
            new CatalogItem { code = "ABOVE_100000", label = "> 100k" }
        };

        [Test]
        public void BuildOptions_AppendsPreferNotToSayAfterCatalog()
        {
            var options = AnnualIncomeSelection.BuildOptions(Catalog, "Prefer not to say");

            CollectionAssert.AreEqual(new[] { "< 10k", "> 100k", "Prefer not to say" }, options);
            Assert.AreEqual(2, AnnualIncomeSelection.PreferNotToSayIndex(Catalog));
        }

        [Test]
        public void BuildOptions_EmptyCatalog_HasNoOptions()
        {
            Assert.AreEqual(0, AnnualIncomeSelection.BuildOptions(null, "Prefer not to say").Count);
            Assert.AreEqual(0, AnnualIncomeSelection.BuildOptions(new CatalogItem[0], "Prefer not to say").Count);
        }

        [Test]
        public void ToCode_ReturnsCodeOnlyForCatalogRanges()
        {
            Assert.AreEqual("ABOVE_100000", AnnualIncomeSelection.ToCode(1, Catalog));
            Assert.IsNull(AnnualIncomeSelection.ToCode(2, Catalog));
            Assert.IsNull(AnnualIncomeSelection.ToCode(-1, Catalog));
            Assert.IsNull(AnnualIncomeSelection.ToCode(0, null));
        }

        [Test]
        public void IsPreferNotToSay_OnlyForIndexAfterCatalog()
        {
            Assert.IsTrue(AnnualIncomeSelection.IsPreferNotToSay(2, Catalog));
            Assert.IsFalse(AnnualIncomeSelection.IsPreferNotToSay(1, Catalog));
            Assert.IsFalse(AnnualIncomeSelection.IsPreferNotToSay(-1, Catalog));
            Assert.IsFalse(AnnualIncomeSelection.IsPreferNotToSay(0, new CatalogItem[0]));
        }
    }
}
