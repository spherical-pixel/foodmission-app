using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class BarcodeScanTests
    {
        [TestCase("049000006346", "0049000006346")]
        [TestCase(" 049000006346 ", "0049000006346")]
        [TestCase("5449000000996", "5449000000996")]
        [TestCase("96385074", "96385074")]
        [TestCase("ABC123456789", "ABC123456789")]
        [TestCase("", "")]
        [TestCase(null, null)]
        public void Normalize_PadsOnlyTwelveDigitCodes(string input, string expected)
        {
            Assert.AreEqual(expected, BarcodeScan.Normalize(input));
        }

        [Test]
        public void Classify_NamedProduct_IsFound()
        {
            var product = new FoodProduct { id = "550e8400-e29b-41d4-a716-446655440000", name = "Nutella" };

            Assert.AreEqual(BarcodeScanOutcome.Found, BarcodeScan.Classify(product, null));
        }

        [TestCase(null)]
        [TestCase("  ")]
        [TestCase("Unknown Product")]
        [TestCase("unknown product")]
        public void Classify_ProductWithoutName_IsNoProductInfo(string name)
        {
            var product = new FoodProduct { id = "550e8400-e29b-41d4-a716-446655440000", name = name };

            Assert.AreEqual(BarcodeScanOutcome.NoProductInfo, BarcodeScan.Classify(product, null));
        }

        [Test]
        public void Classify_NoProductAndNoError_IsNotFound()
        {
            Assert.AreEqual(BarcodeScanOutcome.NotFound, BarcodeScan.Classify(null, null));
        }

        [TestCase(404, BarcodeScanOutcome.NotFound)]
        [TestCase(0, BarcodeScanOutcome.Busy)]
        [TestCase(408, BarcodeScanOutcome.Busy)]
        [TestCase(429, BarcodeScanOutcome.Busy)]
        [TestCase(500, BarcodeScanOutcome.Busy)]
        [TestCase(503, BarcodeScanOutcome.Busy)]
        [TestCase(400, BarcodeScanOutcome.Failed)]
        [TestCase(403, BarcodeScanOutcome.Failed)]
        public void Classify_Error_ByStatusCode(int statusCode, BarcodeScanOutcome expected)
        {
            var error = new ApiErrorResponse { statusCode = statusCode, message = "x" };

            Assert.AreEqual(expected, BarcodeScan.Classify(null, error));
        }
    }
}
