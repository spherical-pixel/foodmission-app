using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class DietaryPreferenceSelectionTests
    {
        private const int None = 3;

        private static readonly CatalogItem[] Catalog =
        {
            new CatalogItem { code = "VEGAN", label = "Vegan" },
            new CatalogItem { code = "HALAL", label = "Halal" },
            new CatalogItem { code = "KOSHER", label = "Kosher" }
        };

        [Test]
        public void BuildOptions_AppendsNoneAfterCatalog()
        {
            var options = DietaryPreferenceSelection.BuildOptions(Catalog, "None of the above");

            CollectionAssert.AreEqual(new[] { "Vegan", "Halal", "Kosher", "None of the above" }, options);
            Assert.AreEqual(None, DietaryPreferenceSelection.NoneIndex(Catalog));
        }

        [Test]
        public void BuildOptions_WithoutCatalog_IsEmpty()
        {
            Assert.IsEmpty(DietaryPreferenceSelection.BuildOptions(null, "None of the above"));
        }

        [Test]
        public void Normalize_PickingNone_ClearsTheRest()
        {
            CollectionAssert.AreEqual(new[] { None },
                DietaryPreferenceSelection.Normalize(new[] { 0, 2 }, new[] { 0, 2, None }, None));
        }

        [Test]
        public void Normalize_PickingAnOptionWhileNone_RemovesNone()
        {
            CollectionAssert.AreEqual(new[] { 1 },
                DietaryPreferenceSelection.Normalize(new[] { None }, new[] { None, 1 }, None));
        }

        [Test]
        public void Normalize_DeselectingNone_LeavesEmpty()
        {
            CollectionAssert.IsEmpty(DietaryPreferenceSelection.Normalize(new[] { None }, new int[0], None));
        }

        [Test]
        public void Normalize_WithoutNone_KeepsSelection()
        {
            CollectionAssert.AreEqual(new[] { 0, 2 },
                DietaryPreferenceSelection.Normalize(new[] { 0 }, new[] { 0, 2 }, None));
        }

        [Test]
        public void ToCodes_SkipsNone()
        {
            CollectionAssert.AreEqual(new[] { "VEGAN", "KOSHER" },
                DietaryPreferenceSelection.ToCodes(new[] { 0, 2 }, Catalog));
            CollectionAssert.IsEmpty(DietaryPreferenceSelection.ToCodes(new[] { None }, Catalog));
            CollectionAssert.IsEmpty(DietaryPreferenceSelection.ToCodes(null, Catalog));
        }
    }
}
