using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class CountryModelsTests
    {
        // ── CountryUtils.CountryCodeToFlag ──────────────────────────────────

        [Test]
        public void CountryCodeToFlag_WithValidCode_ReturnsEmoji()
        {
            Assert.AreEqual("\U0001F1EA\U0001F1F8", CountryUtils.CountryCodeToFlag("ES")); // 🇪🇸
            Assert.AreEqual("\U0001F1E6\U0001F1F9", CountryUtils.CountryCodeToFlag("AT")); // 🇦🇹
            Assert.AreEqual("\U0001F1FA\U0001F1F8", CountryUtils.CountryCodeToFlag("US")); // 🇺🇸
        }

        [Test]
        public void CountryCodeToFlag_WithInvalidInput_ReturnsEmpty()
        {
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag(null));
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag(""));
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag("X"));
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag("ABC"));
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag("12"));
            Assert.AreEqual("", CountryUtils.CountryCodeToFlag("es")); // lowercase not supported
        }
    }
}
