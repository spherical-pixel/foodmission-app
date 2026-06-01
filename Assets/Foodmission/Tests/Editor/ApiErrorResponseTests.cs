using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class ApiErrorResponseTests
    {
        [Test]
        public void TryParse_WithValidNestJsError_ReturnsParsed()
        {
            string json = "{\"statusCode\":400,\"message\":\"Validation failed\",\"error\":\"Bad Request\",\"traceId\":\"abc-123\",\"path\":\"/api/v1/users\"}";

            var result = ApiErrorResponse.TryParse(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.statusCode);
            Assert.AreEqual("Validation failed", result.message);
            Assert.AreEqual("Bad Request", result.error);
            Assert.AreEqual("abc-123", result.traceId);
            Assert.AreEqual("/api/v1/users", result.path);
        }

        [Test]
        public void TryParse_WithNullJson_ReturnsNull()
        {
            var result = ApiErrorResponse.TryParse(null);
            Assert.IsNull(result);
        }

        [Test]
        public void TryParse_WithEmptyJson_ReturnsNull()
        {
            var result = ApiErrorResponse.TryParse("");
            Assert.IsNull(result);
        }

        [Test]
        public void TryParse_WithWhitespaceJson_ReturnsNull()
        {
            var result = ApiErrorResponse.TryParse("   ");
            Assert.IsNull(result);
        }

        [Test]
        public void TryParse_WithMalformedJson_ReturnsNull()
        {
            var result = ApiErrorResponse.TryParse("{invalid json}");
            Assert.IsNull(result);
        }

        [Test]
        public void TryParse_WithPartialData_ReturnsPartial()
        {
            string json = "{\"statusCode\":500,\"message\":\"Internal error\"}";

            var result = ApiErrorResponse.TryParse(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(500, result.statusCode);
            Assert.AreEqual("Internal error", result.message);
            Assert.IsNull(result.error);
            Assert.IsNull(result.traceId);
            Assert.IsNull(result.path);
        }

        [Test]
        public void TryParse_WithExtraFields_IgnoresUnknown()
        {
            string json = "{\"statusCode\":400,\"message\":\"test\",\"error\":\"Bad\",\"traceId\":\"abc\",\"path\":\"/api\",\"extra\":\"ignored\"}";

            var result = ApiErrorResponse.TryParse(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.statusCode);
            Assert.AreEqual("test", result.message);
            Assert.AreEqual("Bad", result.error);
            Assert.AreEqual("abc", result.traceId);
            Assert.AreEqual("/api", result.path);
        }

        [Test]
        public void TryParse_RoundTripViaJsonUtility_MaintainsFields()
        {
            var original = new ApiErrorResponse
            {
                statusCode = 201,
                message = "Created",
                error = "Created",
                traceId = "trace-xyz",
                path = "/api/v1/items"
            };

            string json = JsonUtility.ToJson(original);
            var result = ApiErrorResponse.TryParse(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(original.statusCode, result.statusCode);
            Assert.AreEqual(original.message, result.message);
            Assert.AreEqual(original.error, result.error);
            Assert.AreEqual(original.traceId, result.traceId);
            Assert.AreEqual(original.path, result.path);
        }
    }
}
