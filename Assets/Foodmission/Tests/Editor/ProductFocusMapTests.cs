using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ProductFocusMapTests
    {
        [TestCase(ProductFocus.Sugar, "NUTR_SUGARS", true)]
        [TestCase(ProductFocus.Sugar, "TL_SUGARS", true)]
        [TestCase(ProductFocus.Sugar, "NUTR_ADDED_SUGARS", true)]
        [TestCase(ProductFocus.Sugar, "NUTR_SALT", false)]
        [TestCase(ProductFocus.Salt, "NUTR_SODIUM", true)]
        [TestCase(ProductFocus.Salt, "TL_SALT", true)]
        [TestCase(ProductFocus.Fat, "NUTR_SATURATED_FAT", true)]
        [TestCase(ProductFocus.Fat, "TL_SATURATED_FAT", true)]
        [TestCase(ProductFocus.Fibre, "NUTR_FIBER", true)]
        [TestCase(ProductFocus.Fibre, "NUTR_CARBOHYDRATES", true)]
        [TestCase(ProductFocus.Protein, "NUTR_PROTEINS_PLANT", true)]
        [TestCase(ProductFocus.Nutrients, "NUTR_VITAMIN_C", true)]
        [TestCase(ProductFocus.Nutrients, "TL_SALT", false)]
        [TestCase(ProductFocus.Nova, "NOVA", true)]
        [TestCase(ProductFocus.Scores, "NUTRISCORE", true)]
        [TestCase(ProductFocus.Scores, "ECOSCORE", true)]
        [TestCase(ProductFocus.Ingredients, "INGREDIENTS", true)]
        [TestCase(ProductFocus.Origin, "META_ORIGINS", true)]
        [TestCase(ProductFocus.Origin, "META_COUNTRIES", true)]
        [TestCase(ProductFocus.Origin, "META_MANUFACTURING", true)]
        [TestCase(ProductFocus.Origin, "META_LABELS", false)]
        [TestCase(ProductFocus.Labels, "META_LABELS", true)]
        [TestCase(ProductFocus.None, "NUTR_SUGARS", false)]
        [TestCase(ProductFocus.Sugar, null, false)]
        public void Matches(ProductFocus focus, string key, bool expected)
        {
            Assert.AreEqual(expected, ProductFocusMap.Matches(focus, key));
        }

        [Test]
        public void Matches_CombinedFlags()
        {
            Assert.IsTrue(ProductFocusMap.Matches(ProductFocus.Nova | ProductFocus.Ingredients, "INGREDIENTS"));
            Assert.IsTrue(ProductFocusMap.Matches(ProductFocus.Nova | ProductFocus.Ingredients, "NOVA"));
        }

        [Test]
        public void HasAny_IsTrueForNone_AndWhenAKeyMatches()
        {
            Assert.IsTrue(ProductFocusMap.HasAny(ProductFocus.None, new string[0]));
            Assert.IsTrue(ProductFocusMap.HasAny(ProductFocus.Sugar, new[] { "NUTR_FAT", "NUTR_SUGARS" }));
            Assert.IsFalse(ProductFocusMap.HasAny(ProductFocus.Origin, new[] { "NUTR_FAT", "META_BRANDS" }));
            Assert.IsFalse(ProductFocusMap.HasAny(ProductFocus.Origin, null));
        }
    }
}
