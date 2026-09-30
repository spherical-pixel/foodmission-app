using System.Globalization;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodWasteFormatterTests
    {
        [Test]
        public void GetDisplayName_Product_ReturnsProductName()
        {
            var waste = new FoodWaste { foodProduct = new FoodProduct { name = "Milk" }, genericFood = new GenericFood { foodName = "Dairy" } };
            Assert.AreEqual("Milk", FoodWasteFormatter.GetDisplayName(waste, "?"));
        }

        [Test]
        public void GetDisplayName_Generic_ReturnsGenericName()
        {
            var waste = new FoodWaste { genericFood = new GenericFood { foodName = "Rice" } };
            Assert.AreEqual("Rice", FoodWasteFormatter.GetDisplayName(waste, "?"));
        }

        [Test]
        public void GetDisplayName_NoFood_ReturnsUnknown()
        {
            Assert.AreEqual("?", FoodWasteFormatter.GetDisplayName(new FoodWaste { foodProductId = "p1" }, "?"));
            Assert.AreEqual("?", FoodWasteFormatter.GetDisplayName(null, "?"));
        }

        [TestCase(WasteReason.Expired, "REASON_EXPIRED")]
        [TestCase(WasteReason.Spoiled, "REASON_SPOILED")]
        [TestCase(WasteReason.Overcooked, "REASON_OVERCOOKED")]
        [TestCase(WasteReason.Unwanted, "REASON_UNWANTED")]
        [TestCase(WasteReason.PortionTooLarge, "REASON_PORTION_LARGE")]
        [TestCase(WasteReason.Other, "REASON_OTHER")]
        [TestCase("WHATEVER", null)]
        [TestCase(null, null)]
        public void GetReasonKey_MapsKnownReasons(string reason, string expected)
        {
            Assert.AreEqual(expected, FoodWasteFormatter.GetReasonKey(reason));
        }

        [Test]
        public void FormatDetail_JoinsQuantityUnitAndReason()
        {
            Assert.AreEqual("200 G · Expired", FoodWasteFormatter.FormatDetail(200f, "G", "Expired"));
        }

        [Test]
        public void FormatDetail_NoReason_OmitsSeparator()
        {
            Assert.AreEqual("1.5 KG", FoodWasteFormatter.FormatDetail(1.5f, "KG", null));
        }

        [Test]
        public void FormatMonth_Spanish_CapitalizesMonth()
        {
            Assert.AreEqual("Septiembre 2026", FoodWasteFormatter.FormatMonth("2026-09", new CultureInfo("es-ES")));
        }

        [Test]
        public void FormatMonth_English()
        {
            Assert.AreEqual("September 2026", FoodWasteFormatter.FormatMonth("2026-09", new CultureInfo("en-US")));
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("not-a-month")]
        public void FormatMonth_Invalid_ReturnsNull(string key)
        {
            Assert.IsNull(FoodWasteFormatter.FormatMonth(key, CultureInfo.InvariantCulture));
        }

        [Test]
        public void FormatShortDate_Invalid_ReturnsEmpty()
        {
            Assert.AreEqual("", FoodWasteFormatter.FormatShortDate("nope", CultureInfo.InvariantCulture));
            Assert.AreEqual("", FoodWasteFormatter.FormatShortDate(null, CultureInfo.InvariantCulture));
        }

        [Test]
        public void FormatShortDate_Valid_ContainsDay()
        {
            StringAssert.Contains("28", FoodWasteFormatter.FormatShortDate("2026-09-28T10:00:00Z", new CultureInfo("en-US")));
        }
    }
}
